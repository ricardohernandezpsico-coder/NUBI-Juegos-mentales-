package com.example.flow

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.bridge.GameSessionStore
import com.example.data.PlayMode
import com.example.data.local.NeuroVidaDatabase
import com.example.model.DomainType
import com.example.model.GamePlayResult
import com.example.viewmodel.AppTab
import com.example.viewmodel.NeuroVidaViewModel
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * Los recorridos del ViewModel donde salieron los errores reales de este proyecto: volver de un juego, la pausa,
 * el camino diario y, sobre todo, que Android cierre la app mientras se juega (el ViewModel "nuevo" tiene que retomar
 * lo que dejó anotado el anterior). Usan el ViewModel y la base de datos de verdad (Robolectric), sin Unity.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class GameFlowTest {
  private lateinit var app: Application

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
  }

  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @After
  fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())      // antes de soltar la base: ver TestSupport.release
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun newViewModel(): NeuroVidaViewModel = NeuroVidaViewModel(app).also { viewModels.add(it) }

  private fun result(game: String = "calculo") =
    GamePlayResult(gameId = game, score = 80, correctAnswers = 8, totalTrials = 10, timed = false, level = 2)

  private fun savedResults(): Int = runBlocking { NeuroVidaDatabase.getDatabase(app).gameResultDao().getAllResultsSync().size }

  @Test
  fun `jugar un juego abre su sesion y no deja un resultado viejo`() {
    val vm = newViewModel()
    vm.launchGame("radar")
    assertEquals("radar", vm.activeGame.value?.gameDef?.id)
    assertFalse(vm.activeGame.value!!.isDailyFlow)
    assertNull(vm.lastResult.value)
  }

  @Test
  fun `la sesion diaria lanza el juego que le toca al camino`() {
    val vm = newViewModel()
    val today = vm.dailySession.value
    vm.startDailySession()
    val session = vm.activeGame.value
    assertNotNull(session)
    assertEquals(today.gameIds[today.completedCount], session!!.gameDef.id)
    assertTrue(session.isDailyFlow)
  }

  @Test
  fun `el juego que sigue en la ruta es el de completedCount y en el ultimo no hay ninguno`() {
    // el botón «Siguiente: …» del final de partida (Tarea 146) sale de aquí: `gameIds[completedCount]`, y null cuando ya se jugaron los 3
    val vm = newViewModel()
    val today = vm.dailySession.value
    assertEquals(today.gameIds.getOrNull(today.completedCount), today.nextGameId)
    val route = com.example.model.DailySessionState("2026-10-20", listOf("bodega", "calculo", "intrusa"))
    assertEquals("bodega", route.nextGameId)
    assertEquals("calculo", route.copy(completedCount = 1).nextGameId)
    assertEquals("intrusa", route.copy(completedCount = 2).nextGameId)
    assertNull("en el último (3 de 3) no hay siguiente: el botón dice «Terminar la ruta de hoy»", route.copy(completedCount = 3).nextGameId)
  }

  @Test
  fun `cambiar de pestana cierra el juego y el resultado`() {
    val vm = newViewModel()
    vm.launchGame("radar")
    vm.setTab(AppTab.PROGRESO)
    assertNull(vm.activeGame.value)
    assertNull(vm.lastResult.value)
    assertEquals(AppTab.PROGRESO, vm.currentTab.value)
  }

  @Test
  fun `salir de Unity con Atras sin terminar cierra la sesion y borra lo anotado`() {
    val vm = newViewModel()
    vm.launchGame("radar")
    vm.onUnityLaunched("L1")
    assertNotNull(GameSessionStore.inFlight("L1"))
    vm.onReturnedFromGame("L1", null)
    assertNull(vm.activeGame.value)
    assertNull(GameSessionStore.inFlight("L1"))
  }

  @Test
  fun `salir desde la pausa deja la partida en pausa y Seguir la retoma con el mismo id`() {
    val vm = newViewModel()
    vm.launchGame("calculo")
    vm.onUnityLaunched("L2")
    vm.onReturnedFromGame("L2", null, paused = true)
    assertNull(vm.activeGame.value)
    assertEquals("calculo", vm.pausedGameId.value)
    vm.resumePausedGame()
    assertEquals("calculo", vm.activeGame.value?.gameDef?.id)
    // Con el mismo id de lanzamiento Unity sigue la partida en vez de empezar otra.
    assertEquals("L2", vm.activeGame.value?.resumeLaunchId)
  }

  @Test
  fun `la pausa sobrevive a que Android cierre la app`() {
    val first = newViewModel()
    first.launchGame("calculo")
    first.onUnityLaunched("L3")
    first.onReturnedFromGame("L3", null, paused = true)
    val second = newViewModel() // la app se abrió de nuevo: ViewModel nuevo
    assertEquals("calculo", second.pausedGameId.value)
    second.resumePausedGame()
    assertEquals("L3", second.activeGame.value?.resumeLaunchId)
  }

  @Test
  fun `abrir otro juego descarta la pausa`() {
    val vm = newViewModel()
    vm.launchGame("calculo")
    vm.onUnityLaunched("L4")
    vm.onReturnedFromGame("L4", null, paused = true)
    vm.launchGame("radar")
    assertEquals("radar", vm.activeGame.value?.gameDef?.id)
    assertNull(vm.activeGame.value?.resumeLaunchId)
    assertNull(vm.pausedGameId.value)
  }

  @Test
  fun `si Android cerro la app durante el juego, el resultado se muestra con el flujo donde iba`() {
    val first = newViewModel()
    first.launchGame("calculo", isDailyFlow = true)
    first.onUnityLaunched("L5")

    val second = newViewModel() // el proceso murió y volvió: ViewModel nuevo, sin sesión viva
    assertNull(second.activeGame.value)
    second.onUnityResult(result(), "L5")

    TestSupport.awaitUntil(message = "El resultado nunca llegó a la pantalla de resultado") { second.lastResult.value != null }
    assertEquals("calculo", second.lastResult.value!!.first.gameId)
    assertTrue("debía seguir siendo un juego del camino diario", second.lastResultDaily.value)
    assertNull(second.activeGame.value)
    assertNull(GameSessionStore.inFlight("L5"))
    TestSupport.awaitUntil(message = "El resultado no quedó guardado") { savedResults() == 1 }
  }

  @Test
  fun `un resultado que llego con la app cerrada se procesa al abrirla`() {
    GameSessionStore.saveInFlight(GameSessionStore.InFlight("L6", "calculo", 1, false, true, 0, 0))
    GameSessionStore.savePendingResult(
      "L6",
      """{"user_id":"1","game_id":"calculo","session_metrics":{"correct_trials":8,"total_trials":10,"calculated_score":80,"average_response_time_ms":900,"level":2,"timed":false}}"""
    )
    val vm = newViewModel() // al arrancar toma lo pendiente
    TestSupport.awaitUntil(message = "El resultado pendiente no se mostró") { vm.lastResult.value != null }
    assertEquals(80, vm.lastResult.value!!.first.score)
    assertTrue(vm.lastResultDaily.value)
    assertNull(GameSessionStore.takePendingResult())
  }

  @Test
  fun `un resultado de un boton de prueba, sin sesion, solo se guarda`() {
    val vm = newViewModel()
    vm.onUnityResult(result("radar"), null)
    TestSupport.awaitUntil(message = "El resultado no quedó guardado") { savedResults() == 1 }
    assertNull(vm.lastResult.value)
    assertNull(vm.activeGame.value)
  }

  @Test
  fun `un resultado ilegible no rompe ni deja la pantalla de carga colgada`() {
    GameSessionStore.saveInFlight(GameSessionStore.InFlight("L7", "calculo", 1, false, true, 0, 0))
    GameSessionStore.savePendingResult("L7", "esto no es un resultado")
    val vm = newViewModel()
    assertNull(vm.lastResult.value)
    assertNull(vm.activeGame.value)
    assertEquals(0, savedResults())
  }

  @Test
  fun `desde Juegos, al cerrar el resultado se vuelve a la misma casilla`() {
    val vm = newViewModel()
    vm.playFromLibrary("freno", PlayMode.DESAFIO)
    assertEquals(DomainType.ATENCION, vm.libraryFocus.value.domain)
    assertEquals("freno", vm.libraryFocus.value.gameId)
    vm.setTab(AppTab.HOY) // como si se hubiera ido a otra pestaña mientras tanto
    vm.playFromLibrary("freno", PlayMode.DESAFIO)
    vm.closeGameOrResult()
    assertEquals(AppTab.JUEGOS, vm.currentTab.value)
    assertEquals("freno", vm.libraryFocus.value.gameId)
  }

  @Test
  fun `desde una piedrita de Hoy, al cerrar el resultado se sigue en Hoy y Juegos no se toca`() {
    val vm = newViewModel()
    // un «volver a Juegos» de una partida anterior lanzada desde Juegos no debe llevarse a Hoy
    vm.playFromLibrary("freno", PlayMode.DESAFIO)
    vm.closeGameOrResult()
    vm.setTab(AppTab.HOY)
    vm.playFromHome("calculo", PlayMode.A_TU_MEDIDA)
    assertEquals("calculo", vm.activeGame.value?.gameDef?.id)
    vm.closeGameOrResult()
    assertEquals(AppTab.HOY, vm.currentTab.value)
    assertFalse("Juegos no abre ningún área", vm.libraryFocus.value.open)
    assertEquals("la casilla de Juegos no cambia", "freno", vm.libraryFocus.value.gameId)
  }

  @Test
  fun `entrar a Juegos despues de jugar desde Juegos muestra las 4 areas`() {
    val vm = newViewModel()
    vm.playFromLibrary("freno", PlayMode.DESAFIO)
    vm.closeGameOrResult()
    assertTrue(vm.libraryFocus.value.open) // la excepción: se vuelve al área
    vm.setTab(AppTab.HOY)
    assertFalse("al salir de Juegos el área se cierra", vm.libraryFocus.value.open)
    vm.setTab(AppTab.JUEGOS) // barra, deslizar o «Ir a Juegos»
    assertFalse("al volver deben verse las 4 áreas", vm.libraryFocus.value.open)
    assertEquals("la casilla se recuerda aunque el área esté cerrada", "freno", vm.libraryFocus.value.gameId)
  }

  // ---- Avance: la misma regla que Juegos (Tarea 155): al terminar se vuelve a la pestaña desde donde se lanzó, con la lámina como se dejó

  @Test
  fun `desde la lamina de un area de Avance, al cerrar el resultado se vuelve a Avance con la lamina abierta`() {
    val vm = newViewModel()
    vm.setTab(AppTab.PROGRESO)
    vm.openAvanceArea("ATENCION")
    vm.playFromAvance("freno", PlayMode.DESAFIO)
    assertEquals("freno", vm.activeGame.value?.gameDef?.id)
    assertEquals("la lámina espera abierta mientras se juega", "ATENCION", vm.avanceArea.value)
    vm.closeGameOrResult()
    assertEquals(AppTab.PROGRESO, vm.currentTab.value)
    assertEquals("ATENCION", vm.avanceArea.value)
    assertFalse("Juegos no abre ningún área", vm.libraryFocus.value.open)
  }

  @Test
  fun `desde Avance sin lamina abierta, al cerrar el resultado se vuelve a Avance sin lamina`() {
    val vm = newViewModel()
    vm.setTab(AppTab.PROGRESO)
    vm.playFromAvance("freno", PlayMode.DESAFIO)          // la ficha de «Lo que descubriste»
    vm.closeGameOrResult()
    assertEquals(AppTab.PROGRESO, vm.currentTab.value)
    assertNull(vm.avanceArea.value)
  }

  @Test
  fun `cerrar la lamina de Avance la olvida y salir o entrar a Avance tampoco la reabre`() {
    val vm = newViewModel()
    vm.setTab(AppTab.PROGRESO)
    vm.openAvanceArea("MEMORIA")
    vm.closeAvanceArea()
    assertNull(vm.avanceArea.value)
    vm.openAvanceArea("MEMORIA")
    vm.setTab(AppTab.HOY)
    assertNull("al salir de Avance la lámina se cierra", vm.avanceArea.value)
    vm.setTab(AppTab.PROGRESO)
    assertNull("y no se reabre al volver", vm.avanceArea.value)
  }

  @Test
  fun `un juego de la ruta de hoy lanzado tras la lamina de Avance no la reabre`() {
    val vm = newViewModel()
    vm.setTab(AppTab.PROGRESO)
    vm.openAvanceArea("ATENCION")
    vm.playFromAvance("freno", PlayMode.DESAFIO)
    vm.launchGame("calculo", isDailyFlow = true)           // «Siguiente: … · 2 de 3» en el final de la partida
    vm.closeGameOrResult()
    assertNull(vm.avanceArea.value)
  }

  @Test
  fun `un juego de Hoy no devuelve a la lamina de Avance aunque antes se jugara desde ella`() {
    val vm = newViewModel()
    vm.setTab(AppTab.PROGRESO)
    vm.openAvanceArea("ATENCION")
    vm.playFromAvance("freno", PlayMode.DESAFIO)
    vm.setTab(AppTab.HOY)                                   // salió sin cerrar el resultado
    vm.playFromHome("calculo", PlayMode.A_TU_MEDIDA)
    vm.closeGameOrResult()
    assertEquals(AppTab.HOY, vm.currentTab.value)
    assertNull(vm.avanceArea.value)
  }

  @Test
  fun `la lamina de Avance tambien se recuerda si Android cierra la app a mitad del juego`() {
    val first = newViewModel()
    first.setTab(AppTab.PROGRESO)
    first.openAvanceArea("MEMORIA")
    first.playFromAvance("calculo", PlayMode.A_TU_MEDIDA)
    val second = newViewModel()
    assertEquals("MEMORIA", second.avanceArea.value)
    second.setTab(AppTab.PROGRESO)                          // entrar a Avance por la barra: se ven las 4 áreas
    assertNull(second.avanceArea.value)
  }

  @Test
  fun `Nubi cuenta una sola vez que los juegos de cada lugar se ven en la lamina de Juegos`() {
    val vm = newViewModel()
    assertFalse("al principio todavía no se contó", vm.areaSheetIntroSeen)
    vm.markAreaSheetIntroSeen()
    assertTrue(vm.areaSheetIntroSeen)
    vm.setLibraryFocus(DomainType.ATENCION, null); vm.closeLibraryArea()
    assertTrue("cerrar y abrir otra lámina no lo borra: una sola vez", vm.areaSheetIntroSeen)
  }

  @Test
  fun `tocar Juegos estando en Juegos con un area abierta la cierra`() {
    val vm = newViewModel()
    vm.setTab(AppTab.JUEGOS)
    vm.setLibraryFocus(DomainType.ATENCION, null)
    assertTrue(vm.libraryFocus.value.open)
    vm.setTab(AppTab.JUEGOS)
    assertFalse(vm.libraryFocus.value.open)
    assertEquals(AppTab.JUEGOS, vm.currentTab.value)
  }

  @Test
  fun `salir de Juegos con un area abierta la cierra y no se reabre al volver`() {
    val vm = newViewModel()
    vm.setTab(AppTab.JUEGOS)
    vm.setLibraryFocus(DomainType.ATENCION, "freno")
    vm.setTab(AppTab.PROGRESO)
    assertFalse(vm.libraryFocus.value.open)
    vm.setTab(AppTab.JUEGOS)
    assertFalse(vm.libraryFocus.value.open)
  }

  @Test
  fun `un juego de Hoy no devuelve a Juegos aunque antes se jugara desde Juegos`() {
    val vm = newViewModel()
    vm.setTab(AppTab.JUEGOS)
    vm.playFromLibrary("freno", PlayMode.DESAFIO)
    vm.setTab(AppTab.HOY) // salió sin cerrar el resultado
    vm.launchGame("freno", isDailyFlow = true)
    vm.closeGameOrResult()
    assertEquals(AppTab.HOY, vm.currentTab.value)
    assertFalse(vm.libraryFocus.value.open)
  }

  @Test
  fun `tras cerrar Android la app a mitad de un juego de Juegos, tocar Juegos muestra las 4 areas`() {
    newViewModel().playFromLibrary("freno", PlayMode.A_TU_MEDIDA)
    val second = newViewModel()
    assertTrue(second.libraryFocus.value.open)
    second.setTab(AppTab.JUEGOS)
    assertFalse(second.libraryFocus.value.open)
  }

  @Test
  fun `la casilla de Juegos tambien se recuerda si Android cierra la app`() {
    val first = newViewModel()
    first.playFromLibrary("freno", PlayMode.A_TU_MEDIDA)
    val second = newViewModel()
    assertEquals("freno", second.libraryFocus.value.gameId)
    assertEquals(DomainType.ATENCION, second.libraryFocus.value.domain)
    assertTrue("la ventana del área debía seguir abierta", second.libraryFocus.value.open)
  }
}
