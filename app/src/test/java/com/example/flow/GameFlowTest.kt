package com.example.flow

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.bridge.GameSessionStore
import com.example.data.PlayMode
import com.example.data.local.NeuroVidaDatabase
import com.example.model.DomainType
import com.example.model.GamePlayResult
import com.example.viewmodel.AllGamesState
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

  // ---- Hoy (Tarea 158; Tarea 165: sin la lámina de un lugar): al terminar se vuelve a la pestaña desde donde se lanzó, con lo que había abierto («Todos los juegos») como se dejó

  @Test
  fun `desde Todos los juegos, al cerrar el resultado se vuelve con el mismo filtro, la misma busqueda y la fila marcada`() {
    val vm = newViewModel()
    vm.openAllGames()
    vm.setAllGamesArea("ATENCION")
    vm.setAllGamesQuery("fre")
    vm.playFromHome("freno", PlayMode.DESAFIO)
    assertEquals("freno", vm.activeGame.value?.gameDef?.id)
    vm.closeGameOrResult()
    assertEquals(AppTab.HOY, vm.currentTab.value)
    assertEquals(AllGamesState(area = "ATENCION", query = "fre", mark = "freno"), vm.allGames.value)
  }

  @Test
  fun `desde un juego de Hoy sin nada abierto, al cerrar el resultado se sigue en Hoy sin nada abierto`() {
    val vm = newViewModel()
    vm.playFromHome("calculo", PlayMode.A_TU_MEDIDA)
    assertEquals("calculo", vm.activeGame.value?.gameDef?.id)
    vm.closeGameOrResult()
    assertEquals(AppTab.HOY, vm.currentTab.value)
    assertNull(vm.allGames.value)
  }

  // ---- El Desafío de hoy (Tarea 167)

  /** Todos los juegos ya medidos: el Desafío solo sale de los medidos. */
  private fun measureEverything() {
    app.getSharedPreferences("skill", android.content.Context.MODE_PRIVATE).edit()
      .putString("state", com.example.model.GameRegistry.allGames.joinToString("\n") { "m|${it.id}" }).commit()
  }

  private fun challengeResult(gameId: String, passed: Boolean, timed: Boolean) =
    GamePlayResult(gameId = gameId, score = if (passed) 85 else 40, correctAnswers = if (passed) 12 else 4, totalTrials = 12, timed = timed, level = 2, endRating = 0.5f, modeHits = if (passed) 12 else 4, modeTrials = 12)

  @Test
  fun `el Desafio de hoy se elige entre los medidos y fuera de la ruta, lanza ese juego en modo Desafio y vuelve a Hoy`() {
    measureEverything()
    val vm = newViewModel()
    TestSupport.awaitUntil(message = "No se eligió el Desafío de hoy") { vm.refreshChallenge(); vm.challengeToday.value != null }
    val c = vm.challengeToday.value!!
    assertEquals(com.example.data.ChallengeState.PENDIENTE, c.state)
    assertFalse("nunca es un juego de la ruta de hoy", c.gameId in vm.dailySession.value.gameIds)
    vm.startDailyChallenge()
    val session = vm.activeGame.value!!
    assertEquals(c.gameId, session.gameDef.id)
    assertEquals("en modo Desafío", PlayMode.DESAFIO, session.mode)
    assertFalse("no es la ruta: no avanza el camino de hoy", session.isDailyFlow)
    assertEquals("con el reloj que la persona tiene elegido en la ficha de ese juego", vm.retoFor(c.gameId), session.timed)
    vm.closeGameOrResult()
    assertEquals("al terminar se vuelve a Hoy", AppTab.HOY, vm.currentTab.value)
    assertNull("sin Todos los juegos abierto", vm.allGames.value)
  }

  @Test
  fun `una partida de Desafio del juego elegido lo deja jugado y, si se supera, superado y repetir no lo pierde`() {
    measureEverything()
    val vm = newViewModel()
    TestSupport.awaitUntil(message = "No se eligió el Desafío de hoy") { vm.refreshChallenge(); vm.challengeToday.value != null }
    val id = vm.challengeToday.value!!.gameId
    val before = vm.dailySession.value
    fun play(passed: Boolean, launch: String) {
      vm.startDailyChallenge()
      vm.onUnityLaunched(launch)
      vm.onUnityResult(challengeResult(id, passed, vm.activeGame.value!!.timed), launch)
    }
    play(passed = false, launch = "LD1")
    TestSupport.awaitUntil(message = "El Desafío no pasó a jugado") { vm.challengeToday.value?.state == com.example.data.ChallengeState.JUGADO }
    play(passed = true, launch = "LD2")
    TestSupport.awaitUntil(message = "El Desafío no pasó a superado") { vm.challengeToday.value?.state == com.example.data.ChallengeState.SUPERADO }
    play(passed = false, launch = "LD3")
    TestSupport.awaitUntil(message = "Repetir el Desafío no se guardó") { savedResults() == 3 }
    assertEquals("lo superado no se pierde al repetir", com.example.data.ChallengeState.SUPERADO, vm.challengeToday.value?.state)
    assertEquals("el Desafío no avanza la ruta de hoy", before.completedCount, vm.dailySession.value.completedCount)
    assertEquals("y sigue siendo el mismo juego todo el día", id, vm.challengeToday.value?.gameId)
  }

  @Test
  fun `una partida a tu medida del mismo juego no cuenta como el Desafio de hoy`() {
    measureEverything()
    val vm = newViewModel()
    TestSupport.awaitUntil(message = "No se eligió el Desafío de hoy") { vm.refreshChallenge(); vm.challengeToday.value != null }
    val id = vm.challengeToday.value!!.gameId
    vm.playFromHome(id, PlayMode.A_TU_MEDIDA)
    vm.onUnityLaunched("LM1")
    vm.onUnityResult(challengeResult(id, passed = true, timed = false), "LM1")
    TestSupport.awaitUntil(message = "La partida no quedó guardada") { savedResults() == 1 }
    assertEquals(com.example.data.ChallengeState.PENDIENTE, vm.challengeToday.value?.state)
  }

  @Test
  fun `sin ningun juego medido no hay Desafio de hoy y Hoy queda sin la losa`() {
    val vm = newViewModel()
    TestSupport.awaitUntil(message = "El camino de hoy no se cargó") { vm.dailySession.value.gameIds.isNotEmpty() }
    vm.refreshChallenge()
    assertNull(vm.challengeToday.value)
  }

  @Test
  fun `el Desafio de hoy se recuerda aunque Android cierre la app`() {
    measureEverything()
    val first = com.example.data.NeuroVidaRepository(app)           // el repositorio de «antes» y el de «después»: dos arranques de la app
    try {
      TestSupport.awaitUntil(message = "No se eligió el Desafío de hoy") { first.refreshChallenge() != null }
      val id = first.challengeToday.value!!.gameId
      val second = com.example.data.NeuroVidaRepository(app)
      try {
        assertEquals("el juego de hoy se recuerda al abrir la app de nuevo", id, second.challengeToday.value?.gameId)
        assertEquals(com.example.data.ChallengeState.PENDIENTE, second.challengeToday.value?.state)
      } finally { TestSupport.release(second) }
    } finally { TestSupport.release(first) }
  }

  @Test
  fun `cerrar Todos los juegos lo olvida y cambiar de pestana tampoco lo reabre`() {
    val vm = newViewModel()
    vm.openAllGames()
    vm.closeAllGames()
    assertNull(vm.allGames.value)
    vm.openAllGames()
    vm.setTab(AppTab.PROGRESO)
    assertNull("al irse de Hoy «Todos los juegos» se cierra", vm.allGames.value)
    vm.setTab(AppTab.HOY)
    assertNull("y no se reabre al volver", vm.allGames.value)
    vm.openAllGames()
    vm.setTab(AppTab.TU)
    assertNull("lo mismo yendo a Tú", vm.allGames.value)
  }

  @Test
  fun `un juego de la ruta de hoy lanzado tras Todos los juegos no lo reabre`() {
    val vm = newViewModel()
    vm.openAllGames()
    vm.playFromHome("freno", PlayMode.DESAFIO)
    vm.launchGame("calculo", isDailyFlow = true)           // «Siguiente: … · 2 de 3» en el final de la partida
    vm.closeGameOrResult()
    assertNull(vm.allGames.value)
  }

  @Test
  fun `Todos los juegos tambien se recuerda si Android cierra la app a mitad del juego`() {
    val first = newViewModel()
    first.openAllGames()
    first.setAllGamesQuery("tinta")
    first.playFromHome("stroop", PlayMode.A_TU_MEDIDA)
    val second = newViewModel()
    assertEquals(AllGamesState(area = null, query = "tinta", mark = "stroop"), second.allGames.value)
    assertEquals(AppTab.HOY, second.currentTab.value)
    assertEquals("Todos los juegos sigue abierto como estaba (el resultado de Unity lo cierra con «Listo» y no lo borra)", "tinta", second.allGames.value?.query)
    assertEquals("la fila marcada es el juego que se lanzó", "stroop", second.allGames.value?.mark)
  }

  // ---- Avance (Tarea 159): la página de un área no lanza juegos; se abre y se cierra, y cambiar de pestaña la cierra

  @Test
  fun `la pagina de un area de Avance se abre y se cierra y salir o entrar a Avance tampoco la reabre`() {
    val vm = newViewModel()
    vm.setTab(AppTab.PROGRESO)
    vm.openAvanceArea("MEMORIA")
    assertEquals("MEMORIA", vm.avanceArea.value)
    assertEquals("abrir la página no cambia de pestaña (la barra sigue en Avance)", AppTab.PROGRESO, vm.currentTab.value)
    vm.closeAvanceArea()
    assertNull(vm.avanceArea.value)
    vm.openAvanceArea("MEMORIA")
    vm.setTab(AppTab.HOY)
    assertNull("al salir de Avance la página se cierra", vm.avanceArea.value)
    vm.setTab(AppTab.PROGRESO)
    assertNull("y no se reabre al volver", vm.avanceArea.value)
  }

  @Test
  fun `un juego lanzado desde Hoy vuelve a Hoy y no deja ninguna pagina de Avance abierta`() {
    val vm = newViewModel()
    vm.setTab(AppTab.PROGRESO)
    vm.openAvanceArea("ATENCION")
    vm.setTab(AppTab.HOY)                                   // cambiar de pestaña cierra la página
    vm.playFromHome("calculo", PlayMode.A_TU_MEDIDA)
    vm.closeGameOrResult()
    assertEquals(AppTab.HOY, vm.currentTab.value)
    assertNull(vm.avanceArea.value)
  }

  @Test
  fun `un origen avance que quedo guardado de antes ya no devuelve a Avance`() {
    app.getSharedPreferences("library_focus", android.content.Context.MODE_PRIVATE).edit().putString("origin", "avance").putString("origin_game", "freno").putString("avance_area", "ATENCION").commit()
    val vm = newViewModel()
    assertNull("la página de un área no se restaura: no se juega desde ella", vm.avanceArea.value)
    vm.launchGame("freno")
    vm.closeGameOrResult()
    assertNull(vm.avanceArea.value)
  }
}
