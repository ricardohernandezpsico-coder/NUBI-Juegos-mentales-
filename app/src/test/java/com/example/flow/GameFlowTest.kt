package com.example.flow

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.bridge.GameSessionStore
import com.example.data.PlayMode
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toDomain
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

  // ---- «Cómo se juega» de la ficha (Tarea 171)

  @Test
  fun `Como se juega, en Sobre este juego, abre el juego con su tutorial aunque ya se haya jugado y al volver deja Todos los juegos como estaba`() {
    val vm = newViewModel()
    vm.openAllGames()
    vm.setAllGamesQuery("pil")
    vm.playTutorialFromHome("piloto")
    val session = vm.activeGame.value!!
    assertEquals("piloto", session.gameDef.id)
    assertTrue("el tutorial forzado (show_tutorial)", session.tutorial)
    assertFalse(session.isDailyFlow)
    assertEquals(PlayMode.A_TU_MEDIDA, session.mode)
    assertNull(vm.lastResult.value)
    vm.closeGameOrResult()
    assertEquals(AppTab.HOY, vm.currentTab.value)
    assertEquals("pil", vm.allGames.value?.query)
    assertEquals("piloto", vm.allGames.value?.mark)
    // jugar a secas NO lleva el tutorial: solo «Cómo se juega» lo pide
    vm.playFromHome("piloto", PlayMode.A_TU_MEDIDA)
    assertFalse(vm.activeGame.value!!.tutorial)
  }

  // ---- El Contrarreloj de hoy (Tarea 177; antes, el Desafío de hoy de la Tarea 167)

  /** Lo que Unity devuelve de un Contrarreloj: la marca (`tt_hits`), los errores, el paso, los segundos y la versión de la pista; el nivel de la persona sale sin tocar (`endRating` 0,5) y `modeTrials` en 0. */
  private fun timeTrialResult(gameId: String, hits: Int, errors: Int = 4, peak: Int = 2) =
    GamePlayResult(
      gameId = gameId, score = 50, correctAnswers = hits, totalTrials = hits + errors, timed = true, level = 2, endRating = 0.5f, modeTrials = 0, modeHits = 0,
      ttHits = hits, ttErrors = errors, ttPeakLevel = peak, ttSeconds = 120, ttTrackVersion = 2
    )

  private fun savedTimeTrials(): List<com.example.data.TimeTrialResult> =
    runBlocking { NeuroVidaDatabase.getDatabase(app).timeTrialResultDao().getAllSync().map { it.toDomain() } }

  @Test
  fun `el Contrarreloj de hoy lanza el juego de la fecha con su pista, sin ser la ruta, y vuelve a Hoy`() {
    val vm = newViewModel()
    vm.startTimeTrial()
    val day = vm.todayKey()
    val session = vm.activeGame.value!!
    assertEquals("el juego sale de la fecha", com.example.data.TimeTrialDay.gameFor(day), session.gameDef.id)
    assertEquals("la pista de ese día", day, session.timeTrialDay)
    assertTrue("siempre contra el reloj", session.timed)
    assertEquals("sin modos: la dificultad adaptativa está apagada", PlayMode.A_TU_MEDIDA, session.mode)
    assertFalse("no es la ruta: no avanza el camino de hoy", session.isDailyFlow)
    vm.closeGameOrResult()
    assertEquals("al terminar se vuelve a Hoy", AppTab.HOY, vm.currentTab.value)
    assertNull("sin Todos los juegos abierto", vm.allGames.value)
  }

  @Test
  fun `una partida de Contrarreloj se guarda en su tabla, no mueve el avance ni la ruta, y abre su final`() {
    val vm = newViewModel()
    TestSupport.awaitUntil(message = "El camino de hoy no se cargó") { vm.dailySession.value.gameIds.isNotEmpty() }
    val before = vm.dailySession.value
    vm.startTimeTrial()
    val id = vm.activeGame.value!!.gameDef.id
    vm.onUnityLaunched("LT1")
    vm.onUnityResult(timeTrialResult(id, hits = 38), "LT1")
    TestSupport.awaitUntil(message = "El Contrarreloj no se guardó") { savedTimeTrials().size == 1 }
    val saved = savedTimeTrials().single()
    assertEquals("Unity manda los aciertos y los errores SIN restar", listOf<Any>(id, 38, 4, 2, 120, 2), listOf(saved.gameId, saved.hits, saved.errors, saved.level, saved.seconds, saved.trackVersion))
    assertEquals("la marca es aciertos menos errores", 34, saved.mark)
    assertEquals("con el día de la pista", vm.todayKey(), saved.day)
    assertEquals("NO es una partida de siempre: no entra en game_results", 0, savedResults())
    assertEquals("no mueve la ruta de hoy", before.completedCount, vm.dailySession.value.completedCount)
    TestSupport.awaitUntil(message = "No se abrió el final del Contrarreloj") { vm.lastTimeTrial.value != null }
    val o = vm.lastTimeTrial.value!!
    assertTrue("la primera partida de un juego es «primera»: no hay récord que superar", o.isFirst && !o.isRecord)
    assertEquals(38, o.result.hits)
    assertEquals("la marca del final es la neta", 34, o.result.mark)
    assertTrue("y hay un final abierto (la barra y las pestañas se esconden como con cualquier final)", vm.lastResult.value?.first?.isTimeTrial == true)
    assertNull("la sesión terminó", vm.activeGame.value)
  }

  @Test
  fun `el estado de Hoy se deriva de lo guardado, la marca nueva se reconoce y repetir no la pierde`() {
    val vm = newViewModel()
    val day = vm.todayKey()
    val id = com.example.data.TimeTrialDay.gameFor(day)
    fun play(hits: Int, launch: String) {
      vm.startTimeTrial()
      vm.onUnityLaunched(launch)
      vm.onUnityResult(timeTrialResult(id, hits), launch)
    }
    assertEquals(com.example.data.TimeTrialState.PENDIENTE, com.example.data.TimeTrialDay.stateOf(vm.timeTrialResults.value, day, id))
    play(30, "LS1")
    TestSupport.awaitUntil(message = "La primera partida no se guardó") { vm.timeTrialResults.value.size == 1 }
    assertEquals("la primera vez se juega pero no es marca nueva", com.example.data.TimeTrialState.JUGADO, com.example.data.TimeTrialDay.stateOf(vm.timeTrialResults.value, day, id))
    Thread.sleep(5)
    play(41, "LS2")
    TestSupport.awaitUntil(message = "La segunda partida no se guardó") { vm.timeTrialResults.value.size == 2 }
    TestSupport.awaitUntil(message = "No pasó a marca nueva") { com.example.data.TimeTrialDay.stateOf(vm.timeTrialResults.value, day, id) == com.example.data.TimeTrialState.MARCA_NUEVA }
    TestSupport.awaitUntil(message = "El final no se abrió") { vm.lastTimeTrial.value?.result?.hits == 41 }
    assertTrue("el final lo dice", vm.lastTimeTrial.value!!.isRecord)
    assertEquals("el récord de antes es la marca neta: 30 aciertos menos 4 errores", 26, vm.lastTimeTrial.value!!.previousRecord)
    Thread.sleep(5)
    play(35, "LS3")
    TestSupport.awaitUntil(message = "La tercera partida no se guardó") { vm.timeTrialResults.value.size == 3 }
    assertEquals("la marca nueva no se pierde al repetir sin superarla", com.example.data.TimeTrialState.MARCA_NUEVA, com.example.data.TimeTrialDay.stateOf(vm.timeTrialResults.value, day, id))
    assertEquals("el récord es la marca neta: 41 aciertos menos 4 errores", 37, com.example.data.TimeTrialStats.record(vm.timeTrialResults.value, id))
    assertEquals("y sigue siendo el mismo juego todo el día", id, com.example.data.TimeTrialDay.gameFor(vm.todayKey()))
  }

  @Test
  fun `un Contrarreloj cuenta como dia jugado para la racha y las farolas pero no para el avance ni lo medido`() {
    val vm = newViewModel()
    val measuredBefore = vm.skill.value.measured
    assertEquals("sin partidas no hay racha", 0, vm.currentStreak.value)
    vm.startTimeTrial()
    val id = vm.activeGame.value!!.gameDef.id
    vm.onUnityLaunched("LR1")
    vm.onUnityResult(timeTrialResult(id, hits = 20), "LR1")
    TestSupport.awaitUntil(message = "El Contrarreloj no se guardó") { vm.timeTrialResults.value.size == 1 }
    TestSupport.awaitUntil(message = "No llegó la actividad") { vm.activityTimestamps.value.size == 1 }
    TestSupport.awaitUntil(message = "El Contrarreloj no sumó a la racha") { vm.currentStreak.value >= 1 }
    assertEquals("no mueve lo medido", measuredBefore, vm.skill.value.measured)
    assertTrue("ni suma una partida de siempre al historial", vm.gameHistory.value.isEmpty())
  }

  @Test
  fun `una partida de Contrarreloj en pausa se retoma como Contrarreloj y una de siempre no la reemplaza`() {
    val vm = newViewModel()
    vm.startTimeTrial()
    val id = vm.activeGame.value!!.gameDef.id
    val day = vm.activeGame.value!!.timeTrialDay
    vm.onUnityLaunched("LP1")
    vm.onReturnedFromGame("LP1", null, paused = true)
    assertEquals("Hoy ofrece seguir la partida", id, vm.pausedGameId.value)
    vm.resumePausedGame()
    val resumed = vm.activeGame.value!!
    assertEquals("se retoma con la MISMA partida de Unity", "LP1", resumed.resumeLaunchId)
    assertEquals("y sigue siendo un Contrarreloj del mismo día", day, resumed.timeTrialDay)
    // una partida de siempre del mismo juego NO retoma la del Contrarreloj: empieza una nueva
    vm.onReturnedFromGame("LP1", null, paused = true)
    vm.launchGame(id)
    assertNull(vm.activeGame.value!!.resumeLaunchId)
    assertNull(vm.activeGame.value!!.timeTrialDay)
  }

  @Test
  fun `si Android cierra la app durante un Contrarreloj, el ViewModel nuevo lo reconoce y abre su final`() {
    val first = newViewModel()
    first.startTimeTrial()
    val id = first.activeGame.value!!.gameDef.id
    val day = first.activeGame.value!!.timeTrialDay!!
    first.onUnityLaunched("LK1")
    // Android cierra la app: el ViewModel de antes se pierde y lo anotado en disco queda
    val inFlight = GameSessionStore.inFlight("LK1")!!
    assertEquals("la partida en curso recuerda el día de su pista", day, inFlight.timeTrialDay)
    val second = newViewModel()
    second.onUnityResult(timeTrialResult(id, hits = 27), "LK1")
    TestSupport.awaitUntil(message = "El Contrarreloj no se guardó") { savedTimeTrials().size == 1 }
    assertEquals("con el día de la pista de la sesión", day, savedTimeTrials().single().day)
    TestSupport.awaitUntil(message = "No se abrió el final") { second.lastTimeTrial.value != null }
  }

  @Test
  fun `Otra vez repite el mismo juego y la misma pista aunque el dia haya cambiado`() {
    val vm = newViewModel()
    vm.startTimeTrial("radar", "2026-10-26")
    val s = vm.activeGame.value!!
    assertEquals("radar", s.gameDef.id)
    assertEquals("2026-10-26", s.timeTrialDay)
  }

  /** Tarea 181: el botón «Contrarreloj» de la ficha lanza el de ESE juego, sea o no el del día, con la pista de hoy (la misma semilla que usaría Hoy), y se guarda igual que desde Hoy. */
  @Test
  fun `desde la ficha se lanza el Contrarreloj de cualquiera de los cuatro con la pista de hoy y se guarda igual`() {
    val vm = newViewModel()
    val day = vm.todayKey()
    val ofDay = com.example.data.TimeTrialDay.gameFor(day)
    for ((i, game) in com.example.data.TimeTrialDay.GAMES.withIndex()) {
      vm.openAllGames()
      vm.startTimeTrial(game)
      val s = vm.activeGame.value!!
      assertEquals(game, s.gameDef.id)
      assertEquals("$game: la pista es la de hoy (la fecha local), no la de otro día", day, s.timeTrialDay)
      assertTrue("$game: va con el reloj del Contrarreloj", s.timed)
      assertEquals("$game: sin modo de dificultad (el Contrarreloj no los tiene)", PlayMode.A_TU_MEDIDA, s.mode)
      assertEquals("$game: la semilla que va a Unity es la fecha de hoy como número", com.example.data.TimeTrialDay.trackSeed(day), s.timeTrialDay?.let { com.example.data.TimeTrialDay.trackSeed(it) })
      vm.onUnityLaunched("LF$i")
      vm.onUnityResult(timeTrialResult(game, hits = 20 + i), "LF$i")
      TestSupport.awaitUntil(message = "El Contrarreloj de $game no se guardó") { vm.timeTrialResults.value.count { it.gameId == game } == 1 }
      val saved = vm.timeTrialResults.value.single { it.gameId == game }
      assertEquals("$game: se guarda en el día de la pista", day, saved.day)
      TestSupport.awaitUntil(message = "No se abrió el final de $game") { vm.lastTimeTrial.value?.result?.gameId == game }
      assertEquals("cuenta como la partida de ese juego, de «hoy» solo si es el del día", game == ofDay, com.example.data.TimeTrialDay.isOfDay(game, day))
      vm.closeGameOrResult()
    }
    assertTrue("ninguno de los cuatro sumó una partida de siempre", vm.gameHistory.value.isEmpty())
  }

  @Test
  fun `el Contrarreloj lanzado desde la ficha vuelve a Todos los juegos con el juego marcado y desde Hoy vuelve a Hoy`() {
    val vm = newViewModel()
    vm.openAllGames()
    vm.setAllGamesArea("ATENCION")
    vm.startTimeTrial("stroop")
    vm.onUnityLaunched("LV1")
    vm.onUnityResult(timeTrialResult("stroop", hits = 25), "LV1")
    TestSupport.awaitUntil(message = "No se abrió el final") { vm.lastTimeTrial.value != null }
    vm.closeGameOrResult()
    assertEquals("«Listo» vuelve a Todos los juegos como estaba, con el juego marcado", AllGamesState(area = "ATENCION", query = "", mark = "stroop"), vm.allGames.value)
    assertNull(vm.lastResult.value)
    // lanzado desde Hoy (sin Todos los juegos abierta): vuelve a Hoy, sin nada abierto
    vm.closeAllGames()
    vm.startTimeTrial()
    val today = vm.activeGame.value!!.gameDef.id
    vm.onUnityLaunched("LV2")
    vm.onUnityResult(timeTrialResult(today, hits = 25), "LV2")
    TestSupport.awaitUntil(message = "No se abrió el final de Hoy") { vm.lastTimeTrial.value?.result?.gameId == today }
    vm.closeGameOrResult()
    assertNull("desde Hoy no queda Todos los juegos abierta", vm.allGames.value)
    assertEquals(AppTab.HOY, vm.currentTab.value)
  }

  @Test
  fun `lo jugado desde la ficha del juego del dia lo ve Hoy como jugado o marca nueva y el de otro juego no lo toca`() {
    val vm = newViewModel()
    val day = vm.todayKey()
    val ofDay = com.example.data.TimeTrialDay.gameFor(day)
    val other = com.example.data.TimeTrialDay.GAMES.first { it != ofDay }
    fun play(game: String, hits: Int, launch: String) {
      vm.openAllGames()
      vm.startTimeTrial(game)
      vm.onUnityLaunched(launch)
      vm.onUnityResult(timeTrialResult(game, hits), launch)
    }
    assertEquals(com.example.data.TimeTrialState.PENDIENTE, com.example.data.TimeTrialDay.stateOf(vm.timeTrialResults.value, day, ofDay))
    play(other, 30, "LH1")
    TestSupport.awaitUntil(message = "No se guardó el del otro juego") { vm.timeTrialResults.value.size == 1 }
    assertEquals("jugar otro juego desde su ficha no cambia el de Hoy", com.example.data.TimeTrialState.PENDIENTE, com.example.data.TimeTrialDay.stateOf(vm.timeTrialResults.value, day, ofDay))
    play(ofDay, 30, "LH2")
    TestSupport.awaitUntil(message = "No se guardó el del día") { vm.timeTrialResults.value.size == 2 }
    assertEquals("el juego del día, jugado desde su ficha, Hoy lo ve jugado", com.example.data.TimeTrialState.JUGADO, com.example.data.TimeTrialDay.stateOf(vm.timeTrialResults.value, day, ofDay))
    Thread.sleep(5)
    play(ofDay, 41, "LH3")
    TestSupport.awaitUntil(message = "No se guardó la segunda") { vm.timeTrialResults.value.size == 3 }
    assertEquals("y con una marca mejor, marca nueva", com.example.data.TimeTrialState.MARCA_NUEVA, com.example.data.TimeTrialDay.stateOf(vm.timeTrialResults.value, day, ofDay))
    assertEquals("el récord de la ficha sale de la misma tabla", 37, com.example.data.TimeTrialStats.record(vm.timeTrialResults.value, ofDay))
    assertEquals("y el de otro juego, del suyo", 26, com.example.data.TimeTrialStats.record(vm.timeTrialResults.value, other))
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
