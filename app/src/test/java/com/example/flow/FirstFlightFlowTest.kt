package com.example.flow

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.bridge.GameSessionStore
import com.example.data.BaselinePlan
import com.example.data.ColorVision
import com.example.data.FirstFlight
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.data.FlightStore
import com.example.data.local.NeuroVidaDatabase
import com.example.model.AgeBand
import com.example.model.DomainType
import com.example.model.GamePlayResult
import com.example.viewmodel.AppTab
import com.example.viewmodel.NeuroVidaViewModel
import com.example.viewmodel.TopPanel
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
 * «Primer vuelo con Nubi» con el ViewModel y la base de datos de verdad (Robolectric), sin Unity: los resultados de los 4 juegos se simulan.
 * Incluye lo que más duele en el teléfono: que Android cierre la app en medio de un juego o de una pregunta.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class FirstFlightFlowTest {
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

  private fun resultFor(gameId: String): GamePlayResult {
    val base = GamePlayResult(gameId = gameId, score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 1, endRating = 0.6f)
    return when (gameId) {
      "secuencia" -> base.copy(rasBestLen = listOf(5, 0, 0, 0))
      "freno" -> base.copy(stopsOk = 6, stopsTotal = 8, endRating = 0.4f)
      "aterrizaje" -> base.copy(numlineErrorPct = 4.2f, endRating = 0.3f)
      else -> base.copy(lexBandSeen = listOf(4, 3, 2, 1, 1, 0), lexBandHits = listOf(4, 3, 1, 1, 0, 0), endRating = 0.8f)
    }
  }

  private fun savedResults(): Int = runBlocking { NeuroVidaDatabase.getDatabase(app).gameResultDao().getAllResultsSync().size }

  /** «Jugar» el juego [index] y que Unity devuelva su resultado. */
  private fun play(vm: NeuroVidaViewModel, index: Int) {
    val id = BaselinePlan.steps[index].gameId
    vm.flightPlay(index)
    assertEquals(index + 1, vm.activeGame.value!!.assessmentStep)
    vm.onUnityLaunched("L$index")
    vm.onUnityResult(resultFor(id), "L$index")
    TestSupport.awaitUntil(message = "El resultado de $id no llevó a su tarjeta") { vm.flight.value?.step == FlightStep.cardStep(index) }
  }

  /** Hola y el nombre; desde la Tarea 132 («Juega primero») sigue DIRECTO el juego 1: la edad se pregunta después de su tarjeta. */
  private fun answerUntilFirstGame(vm: NeuroVidaViewModel) {
    vm.flightContinue() // HOLA
    vm.flightSetName("Ana")
    vm.flightContinue() // NOMBRE
    assertEquals(FlightStep.JUEGO_1, vm.flight.value!!.step)
    assertNull("la edad todavía no se preguntó", vm.flight.value!!.age)
  }

  /** De la tarjeta del juego 1: «Continuar» pide la edad, se responde y sigue METAS. */
  private fun answerAgeAfterFirstCard(vm: NeuroVidaViewModel) {
    vm.flightContinue() // TARJETA_1 -> EDAD
    assertEquals(FlightStep.EDAD, vm.flight.value!!.step)
    vm.flightSetAge(AgeBand.SENIOR)
    TestSupport.awaitUntil(message = "La edad no quedó en el perfil") { vm.userSettings.value.ageBand == AgeBand.SENIOR }
    vm.flightContinue() // EDAD -> METAS
    assertEquals(FlightStep.METAS, vm.flight.value!!.step)
  }

  @Test
  fun `el recorrido completo con los 4 juegos simulados termina en Hoy con todo guardado`() {
    val vm = newViewModel()
    assertNull("la primera vez no hay nada guardado todavía", vm.flight.value)
    assertNull(vm.userSettings.value.ageBand)
    answerUntilFirstGame(vm)
    // El inicio completo abre SIEMPRE el tutorial guiado y la versión corta, y el juego 1 arranca suave (sin edad conocida todavía: nivel 2).
    vm.flightPlay(0)
    assertTrue(vm.activeGame.value!!.tutorial)
    assertEquals(1, vm.activeGame.value!!.assessmentStep)
    assertEquals("sin edad el arranque es el suave", 2, vm.activeGame.value!!.level)
    assertNull("el juego 1 se lanza sin edad en el perfil (la pantalla la resuelve como adulto)", vm.userSettings.value.ageBand)
    vm.onUnityLaunched("L0")
    vm.onUnityResult(resultFor("secuencia"), "L0")
    TestSupport.awaitUntil { vm.flight.value?.step == FlightStep.TARJETA_1 }
    assertEquals("Repetiste bien un rastro de 5 luces.", vm.flight.value!!.phrases["secuencia"])
    assertNull("el juego del inicio no es una partida de la sesión diaria", vm.lastResult.value)

    answerAgeAfterFirstCard(vm) // TARJETA_1 -> EDAD -> METAS
    assertEquals("la edad respondida después del juego 1 queda en el recorrido", AgeBand.SENIOR, vm.flight.value!!.age)
    vm.flightToggleGoal(DomainType.LENGUAJE)
    vm.flightToggleGoal(DomainType.MEMORIA)
    vm.flightContinue() // METAS -> DATO
    vm.flightContinue() // DATO -> JUEGO_2
    assertEquals(FlightStep.JUEGO_2, vm.flight.value!!.step)
    play(vm, 1)
    assertEquals("Frenaste a tiempo 6 de 8 veces.", vm.flight.value!!.phrases["freno"])
    vm.flightContinue() // TARJETA_2 -> JUEGO_3 (ya no hay pregunta de qué ver primero ni de los colores: Tarea 143, R4)
    assertEquals(FlightStep.JUEGO_3, vm.flight.value!!.step)
    play(vm, 2)
    assertEquals("En promedio, aterrizaste a un 4 % de distancia del lugar justo.", vm.flight.value!!.phrases["aterrizaje"])
    vm.flightContinue() // -> DIAS
    vm.flightSetDays(5)
    vm.flightContinue() // -> JUEGO_4
    play(vm, 3)
    assertEquals("Reconociste 9 de 11 palabras reales.", vm.flight.value!!.phrases["meteoros"])
    vm.flightContinue() // -> PUNTO
    assertEquals(FlightStep.PUNTO, vm.flight.value!!.step)
    assertTrue(vm.flight.value!!.allMeasured)

    vm.flightContinue() // guarda el punto de partida
    TestSupport.awaitUntil(message = "No pasó de «Tu punto de partida» al aviso") { vm.flight.value?.step == FlightStep.AVISO }
    assertNotNull("con los 4 juegos medidos se guarda el mapa", vm.baseline.value)
    assertEquals("Ana", vm.userSettings.value.name)
    assertEquals(5, vm.userSettings.value.weeklyGoal)
    assertEquals(setOf(DomainType.LENGUAJE, DomainType.MEMORIA), vm.goals.value)
    assertEquals("el recorrido ya no toca los colores: queda el valor por defecto (se cambia en Ajustes)", ColorVision.NO_SE, vm.colorVision.value)

    vm.flightConfirmReminder(9)
    TestSupport.awaitUntil { vm.flight.value?.step == FlightStep.CAMINO }
    assertTrue(vm.userSettings.value.notificationsEnabled)
    assertEquals(9, vm.userSettings.value.reminderHour)
    assertEquals(3, vm.dailySession.value.gameIds.size)
    assertEquals("las 4 partidas no cuentan para el camino de hoy", 0, vm.dailySession.value.completedCount)

    vm.flightContinue() // «Empezar mi camino»
    assertNull(vm.flight.value)
    assertNull("no queda nada del recorrido en disco", FlightStore.load(app))
    assertEquals(AgeBand.SENIOR, vm.userSettings.value.ageBand)
    TestSupport.awaitUntil { savedResults() == 4 }
    // El día del Primer vuelo no son 7 juegos: el camino de hoy queda CUMPLIDO con el vuelo, sin puntajes ni partidas duplicadas.
    TestSupport.awaitUntil(message = "El camino de hoy no quedó cumplido con el Primer vuelo") { vm.dailySession.value.completedCount == vm.dailySession.value.gameIds.size }
    assertEquals(3, vm.dailySession.value.gameIds.size)
    assertTrue("un camino cumplido sin puntajes es el del Primer vuelo", vm.dailySession.value.scores.isEmpty())
    assertEquals("no se duplican partidas", 4, savedResults())
    // y con eso el día cuenta para la racha
    assertEquals(1, vm.currentStreak.value)
  }

  @Test
  fun `terminar despues en el juego 2 estima el resto, sigue con las preguntas y no guarda mapa`() {
    val vm = newViewModel()
    answerUntilFirstGame(vm)
    play(vm, 0)
    answerAgeAfterFirstCard(vm); vm.flightToggleGoal(DomainType.ATENCION); vm.flightContinue() // EDAD y METAS
    vm.flightContinue() // DATO -> JUEGO_2
    assertEquals(FlightStep.JUEGO_2, vm.flight.value!!.step)
    vm.flightSkipGames()
    val s = vm.flight.value!!
    assertEquals(setOf("freno", "aterrizaje", "meteoros"), s.skipped)
    assertEquals(setOf("secuencia"), s.measured.keys)
    assertEquals("sigue por la pregunta que falta, sin juegos", FlightStep.DIAS, s.step)
    vm.flightSetDays(3); vm.flightContinue()
    assertEquals(FlightStep.PUNTO, vm.flight.value!!.step)
    vm.flightContinue()
    TestSupport.awaitUntil { vm.flight.value?.step == FlightStep.AVISO }
    // Un solo juego medido: queda el punto de partida estimado y la evaluación se ofrece de nuevo en Avance.
    assertNull(vm.baseline.value)
    assertEquals(3, vm.userSettings.value.weeklyGoal)
    assertEquals(1, savedResults())
  }

  @Test
  fun `si Android cierra la app en medio de una pregunta, vuelve en esa pregunta sin repetir el juego`() {
    val first = newViewModel()
    answerUntilFirstGame(first)
    play(first, 0)
    answerAgeAfterFirstCard(first) // -> METAS
    first.flightToggleGoal(DomainType.RAZONAMIENTO)

    val second = newViewModel() // el proceso murió y volvió
    val s = second.flight.value!!
    assertEquals(FlightStep.METAS, s.step)
    assertEquals(setOf("secuencia"), s.measured.keys)
    assertEquals("Ana", s.name)
    assertEquals(AgeBand.SENIOR, s.age)
    assertEquals(setOf(DomainType.RAZONAMIENTO), s.goals)
    // Atrás vuelve a la tarjeta (no a repetir el juego) y Continuar sigue adelante.
    second.flightBack()
    assertEquals(FlightStep.TARJETA_1, second.flight.value!!.step)
    second.flightContinue()
    second.flightContinue()
    assertEquals(FlightStep.DATO, second.flight.value!!.step)
  }

  @Test
  fun `si Android cierra la app en medio del juego 3, el resultado que vuelve sigue el recorrido`() {
    val first = newViewModel()
    answerUntilFirstGame(first)
    play(first, 0)
    answerAgeAfterFirstCard(first); first.flightToggleGoal(DomainType.MEMORIA); first.flightContinue(); first.flightContinue()
    play(first, 1)
    first.flightContinue() // TARJETA_2 -> JUEGO_3
    assertEquals(FlightStep.JUEGO_3, first.flight.value!!.step)
    first.flightPlay(2)
    first.onUnityLaunched("L2") // Unity al frente; la app muere

    val second = newViewModel()
    assertNull(second.activeGame.value)
    // Vuelve en el juego 3 (no repite los juegos ni las preguntas de antes).
    assertEquals(FlightStep.JUEGO_3, second.flight.value!!.step)
    assertEquals(setOf("secuencia", "freno"), second.flight.value!!.measured.keys)
    // El resultado llega al ViewModel nuevo: lo reconoce por la partida anotada en disco.
    second.onUnityResult(resultFor("aterrizaje"), "L2")
    TestSupport.awaitUntil(message = "El resultado no llevó a la tarjeta del juego 3") { second.flight.value?.step == FlightStep.TARJETA_3 }
    assertEquals(setOf("secuencia", "freno", "aterrizaje"), second.flight.value!!.measured.keys)
    assertNull(GameSessionStore.inFlight("L2"))
    TestSupport.awaitUntil { savedResults() == 3 }
  }

  @Test
  fun `un resultado que llego con la app cerrada se procesa al abrirla y sigue el recorrido`() {
    val first = newViewModel()
    answerUntilFirstGame(first)
    play(first, 0)
    answerAgeAfterFirstCard(first); first.flightToggleGoal(DomainType.MEMORIA); first.flightContinue(); first.flightContinue() // -> JUEGO_2
    assertEquals(FlightStep.JUEGO_2, first.flight.value!!.step)
    first.flightPlay(1)
    first.onUnityLaunched("L1")

    GameSessionStore.savePendingResult(
      "L1",
      """{"user_id":"1","game_id":"freno","session_metrics":{"correct_trials":8,"total_trials":10,"calculated_score":80,"average_response_time_ms":900,"level":2,"timed":false,"end_rating":0.55}}"""
    )
    val second = newViewModel() // al arrancar toma lo pendiente
    TestSupport.awaitUntil(message = "El resultado pendiente no llevó a la tarjeta") { second.flight.value?.step == FlightStep.TARJETA_2 }
    assertEquals(0.55f, second.flight.value!!.measured["freno"]!!, 1e-3f)
    assertEquals(setOf("secuencia", "freno"), second.flight.value!!.measured.keys)
  }

  @Test
  fun `si Android cierra la app en el juego 1, antes de la edad, el resultado sigue a la tarjeta y despues pide la edad`() {
    val first = newViewModel()
    answerUntilFirstGame(first)
    first.flightPlay(0)
    first.onUnityLaunched("L0") // Unity al frente; la app muere sin que nadie haya respondido la edad

    val second = newViewModel()
    assertNull(second.activeGame.value)
    assertEquals(FlightStep.JUEGO_1, second.flight.value!!.step)
    assertNull(second.flight.value!!.age)
    assertNull("el relanzamiento usa el perfil sin edad: adulto", second.userSettings.value.ageBand)
    second.onUnityResult(resultFor("secuencia"), "L0")
    TestSupport.awaitUntil(message = "El resultado no llevó a la tarjeta del juego 1") { second.flight.value?.step == FlightStep.TARJETA_1 }
    assertEquals(setOf("secuencia"), second.flight.value!!.measured.keys)
    second.flightContinue()
    assertEquals("después de la tarjeta se pide la edad", FlightStep.EDAD, second.flight.value!!.step)
    second.flightSetAge(AgeBand.ADULT)
    second.flightContinue()
    assertEquals(FlightStep.METAS, second.flight.value!!.step)
  }

  @Test
  fun `si Android cierra la app en la pregunta de la edad, vuelve ahi con el juego 1 ya medido`() {
    val first = newViewModel()
    answerUntilFirstGame(first)
    play(first, 0)
    first.flightContinue() // TARJETA_1 -> EDAD
    assertEquals(FlightStep.EDAD, first.flight.value!!.step)

    val second = newViewModel() // el proceso murió y volvió
    val s = second.flight.value!!
    assertEquals(FlightStep.EDAD, s.step)
    assertEquals(setOf("secuencia"), s.measured.keys)
    assertNull(s.age)
    // sigue sin pedir otra vez el juego 1 ni el nombre
    second.flightSetAge(AgeBand.UNDER_18)
    second.flightContinue()
    assertEquals(FlightStep.METAS, second.flight.value!!.step)
  }

  @Test
  fun `el punto de partida usa la edad respondida despues del juego 1`() {
    val vm = newViewModel()
    answerUntilFirstGame(vm)
    play(vm, 0)
    answerAgeAfterFirstCard(vm)
    val s = vm.flight.value!!
    assertEquals(AgeBand.SENIOR, s.age)
    // la medida del juego es el rating propio del juego: la edad entra al LEER (Skill.progress), no al medir
    val rows = FirstFlight.startingPoint(s, s.age ?: vm.userSettings.value.ageBand)
    val memoria = rows.first { it.domain == DomainType.MEMORIA }
    assertTrue(memoria.measured)
    assertEquals(com.example.data.Skill.progress("secuencia", s.measured.getValue("secuencia"), AgeBand.SENIOR), memoria.progress, 1e-6f)
  }

  @Test
  fun `salir de Unity sin terminar deja el juego para volver a tocar Jugar`() {
    val vm = newViewModel()
    answerUntilFirstGame(vm)
    vm.flightPlay(0)
    vm.onUnityLaunched("L0")
    vm.onReturnedFromGame("L0", null)
    assertNull(vm.activeGame.value)
    assertEquals(FlightStep.JUEGO_1, vm.flight.value!!.step)
    assertTrue(vm.flight.value!!.measured.isEmpty())
  }

  @Test
  fun `repetir la evaluacion desde Avance son solo los 4 juegos, sin tutorial forzado ni preguntas`() {
    val vm = newViewModel()
    vm.startBaseline()
    val s = vm.flight.value!!
    assertEquals(FlightMode.GAMES, s.mode)
    assertEquals(FlightStep.JUEGO_1, s.step)
    for (i in 0..3) {
      play(vm, i)
      vm.flightContinue()
    }
    assertEquals(FlightStep.PUNTO, vm.flight.value!!.step)
    vm.flightContinue()
    TestSupport.awaitUntil(message = "La evaluación no terminó") { vm.flight.value == null }
    assertNotNull(vm.baseline.value)
    assertEquals(4, vm.baseline.value!!.measured.size)
    assertNull(FlightStore.load(app))
  }

  @Test
  fun `repetir la evaluacion desde Avance no da por cumplido el camino de hoy`() {
    val vm = newViewModel()
    TestSupport.awaitUntil { vm.dailySession.value.gameIds.size == 3 }
    vm.startBaseline()
    for (i in 0..3) {
      play(vm, i)
      vm.flightContinue()
    }
    vm.flightContinue()
    TestSupport.awaitUntil(message = "La evaluación no terminó") { vm.flight.value == null }
    assertEquals("el camino de hoy sigue sin empezar", 0, vm.dailySession.value.completedCount)
  }

  @Test
  fun `terminar despues en la evaluacion repetida la cierra`() {
    val vm = newViewModel()
    vm.startBaseline()
    vm.flightSkipGames()
    assertNull(vm.flight.value)
    assertNull(FlightStore.load(app))
  }

  @Test
  fun `salir del inicio con Ajustes abierto cae en Hoy limpio, sin el panel tapando`() {
    // «Terminar después» en la evaluación repetida, abierta con el panel de Ajustes (o Perfil) encima.
    for (panel in TopPanel.entries) {
      val vm = newViewModel()
      vm.setTab(AppTab.PROGRESO)
      vm.openTopPanel(panel)
      vm.startBaseline()
      vm.flightSkipGames()
      assertNull(vm.flight.value)
      assertEquals(AppTab.HOY, vm.currentTab.value)
      assertNull("el panel $panel debía cerrarse", vm.topPanel.value)
    }
  }

  @Test
  fun `terminar normal con Ajustes abierto tambien cae en Hoy limpio`() {
    val vm = newViewModel()
    vm.openTopPanel(TopPanel.AJUSTES)
    vm.startBaseline()
    for (i in 0..3) {
      play(vm, i)
      vm.flightContinue()
    }
    vm.flightContinue() // «Listo» del punto de partida
    TestSupport.awaitUntil(message = "La evaluación no terminó") { vm.flight.value == null }
    assertEquals(AppTab.HOY, vm.currentTab.value)
    assertNull(vm.topPanel.value)
  }

  @Test
  fun `el inicio completo repetido desde Debug termina en Hoy con el panel cerrado`() {
    val vm = newViewModel()
    vm.openTopPanel(TopPanel.AJUSTES)
    vm.debugRestartOnboarding()
    vm.finishFlight() // cualquier salida del recorrido pasa por finishFlight
    assertNull(vm.flight.value)
    assertEquals(AppTab.HOY, vm.currentTab.value)
    assertNull(vm.topPanel.value)
  }

  @Test
  fun `repetir el inicio de depuracion no borra datos y empieza de nuevo`() {
    val vm = newViewModel()
    vm.onUnityResult(resultFor("calculo"), null) // una partida de antes
    TestSupport.awaitUntil { savedResults() == 1 }
    vm.debugRestartOnboarding()
    assertEquals(FlightStep.HOLA, vm.flight.value!!.step)
    assertEquals(FlightMode.FULL, vm.flight.value!!.mode)
    assertEquals(1, savedResults())
    // El inicio completo no se pisa con la evaluación repetida.
    vm.startBaseline()
    assertEquals(FlightMode.FULL, vm.flight.value!!.mode)
  }

  @Test
  fun `la vision de color se guarda y sobrevive al cierre de la app`() {
    val first = newViewModel()
    assertEquals(ColorVision.NO_SE, first.colorVision.value)
    first.setColorVision(ColorVision.DIFICULTAD)
    val second = newViewModel()
    assertEquals(ColorVision.DIFICULTAD, second.colorVision.value)
  }

  @Test
  fun `lo que quedo guardado de result_focus y coach_tone ya no se lee ni se toca`() {
    // La pregunta «¿qué te sirve más ver primero?» se quitó el 10-oct: lo guardado se queda en el teléfono tal cual, sin leerse y sin migrarse.
    val prefs = app.getSharedPreferences("profile_extra", android.content.Context.MODE_PRIVATE)
    prefs.edit().putString("result_focus", "CONSEJO").putString("coach_tone", "CLARO").commit()
    val vm = newViewModel()
    assertEquals(ColorVision.NO_SE, vm.colorVision.value)
    assertEquals("CONSEJO", prefs.getString("result_focus", null))
    assertEquals("CLARO", prefs.getString("coach_tone", null))
  }

  @Test
  fun `el recorrido guardado se codifica y se lee de vuelta, y lo ilegible se descarta`() {
    val s = FlightState(
      mode = FlightMode.FULL, step = FlightStep.JUEGO_3, name = "Ana", age = AgeBand.SENIOR,
      goals = setOf(DomainType.MEMORIA, DomainType.LENGUAJE), days = 4, hour = FirstFlight.NO_REMINDER,
      measured = mapOf("secuencia" to 0.5f, "freno" to 0.25f), phrases = mapOf("freno" to "Frenaste a tiempo 6 de 8 veces."),
      skipped = setOf("meteoros"), applied = false
    )
    assertEquals(s, FlightStore.decode(FlightStore.encode(s)))
    assertEquals(FlightState(FlightMode.GAMES, FlightStep.JUEGO_1), FlightStore.decode(FlightStore.encode(FlightState(FlightMode.GAMES))))
    assertNull(FlightStore.decode("esto no es json"))
    assertNull(FlightStore.decode(null))
    // Un paso que no es de ese recorrido (otra versión de la app) también se descarta.
    assertNull(FlightStore.decode(FlightStore.encode(s.copy(mode = FlightMode.GAMES, step = FlightStep.JUEGO_1)).replace("JUEGO_1", "NOMBRE")))
  }

  @Test
  fun `un recorrido guardado con el orden viejo se lee igual y no vuelve a preguntar la edad`() {
    // Tarea 132: antes la edad se preguntaba ANTES del juego 1. Lo guardado se lee por NOMBRE de paso, así que el orden nuevo no rompe nada.
    val old = FlightState(mode = FlightMode.FULL, step = FlightStep.METAS, name = "Ana", age = AgeBand.SENIOR, measured = mapOf("secuencia" to 0.5f))
    assertEquals(old, FlightStore.decode(FlightStore.encode(old)))
    assertEquals(FlightStep.EDAD, FlightStore.decode(FlightStore.encode(FlightState(FlightMode.FULL, FlightStep.EDAD, name = "Ana", measured = mapOf("secuencia" to 0.5f))))!!.step)
    FlightStore.save(app, old)
    val vm = newViewModel()
    assertEquals(FlightStep.METAS, vm.flight.value!!.step)
    vm.flightBack()
    assertEquals("la edad ya respondida no se vuelve a preguntar", FlightStep.TARJETA_1, vm.flight.value!!.step)
    vm.flightContinue()
    assertEquals(FlightStep.METAS, vm.flight.value!!.step)
  }

  @Test
  fun `quien quedo guardado en una pregunta que se quito sigue en la que venia, sin reiniciar el recorrido`() {
    // Un recorrido guardado con una versión anterior (con el paso ENFOQUE y la clave focus, o con el paso COLOR de la pregunta de los colores, quitada el 19-oct): no se descarta; sigue donde venía y la respuesta vieja se ignora.
    val s = FlightState(mode = FlightMode.FULL, step = FlightStep.TARJETA_2, name = "Ana", age = AgeBand.SENIOR, measured = mapOf("secuencia" to 0.5f, "freno" to 0.25f))
    val expected = s.copy(step = FlightStep.JUEGO_3)
    val enfoque = FlightStore.encode(s).replace("\"step\":\"TARJETA_2\"", "\"step\":\"ENFOQUE\"").replaceFirst("{", "{\"focus\":\"CONSEJO\",")
    assertTrue("el JSON viejo trae el paso que ya no existe", enfoque.contains("ENFOQUE") && enfoque.contains("focus"))
    assertEquals(expected, FlightStore.decode(enfoque))
    val color = FlightStore.encode(s).replace("\"step\":\"TARJETA_2\"", "\"step\":\"COLOR\"").replaceFirst("{", "{\"color\":\"DIFICULTAD\",")
    assertTrue(color.contains("\"COLOR\"") && color.contains("DIFICULTAD"))
    assertEquals("el paso de los colores sigue en el juego 3 y la respuesta guardada se ignora", expected, FlightStore.decode(color))
    // si ese juego ya se midió pasa directo a su tarjeta; si se dejó para después, al paso que sigue; y la clave «color» guardada se ignora
    fun saved(state: FlightState) = FlightStore.encode(state).replace("\"step\":\"TARJETA_2\"", "\"step\":\"COLOR\"").replaceFirst("{", "{\"color\":\"DIFICULTAD\",")
    assertEquals(FlightStep.TARJETA_3, FlightStore.decode(saved(s.copy(measured = s.measured + ("aterrizaje" to 0.3f))))!!.step)
    assertEquals("con el juego 3 para después sigue en la pregunta de los días", FlightStep.DIAS, FlightStore.decode(saved(s.copy(skipped = setOf("aterrizaje"))))!!.step)
    assertFalse("y lo que se guarda ya no lleva el color", FlightStore.encode(s).contains("color"))
    // y con ese recorrido guardado en el teléfono, al abrir la app sigue en el juego 3
    FlightStore.save(app, expected.copy(step = FlightStep.TARJETA_2))
    val vm = newViewModel()
    assertEquals(FlightStep.TARJETA_2, vm.flight.value!!.step)
    vm.flightContinue()
    assertEquals(FlightStep.JUEGO_3, vm.flight.value!!.step)
  }

  @Test
  fun `borrar datos deja el recorrido sin nada guardado`() {
    val vm = newViewModel()
    answerUntilFirstGame(vm)
    assertNotNull(FlightStore.load(app))
    vm.resetData()
    TestSupport.awaitUntil { FlightStore.load(app) == null && vm.flight.value == null }
  }
}
