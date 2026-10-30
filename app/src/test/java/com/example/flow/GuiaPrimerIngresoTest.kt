package com.example.flow

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.NeuroVidaApplication
import com.example.TestSupport
import com.example.data.Achievements
import com.example.data.BaselinePlan
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.data.FlightStore
import com.example.data.local.DailySessionEntity
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toDomain
import com.example.model.AgeBand
import com.example.model.GamePlayResult
import com.example.viewmodel.AppTab
import com.example.viewmodel.NeuroVidaViewModel
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale

/**
 * Lo que decidió Ricardo el 30-oct para el cierre del Primer vuelo (Tarea 193): SOLO la primera medalla se celebra (las otras se ganan igual y quedan en Tú › Medallas) y la tarjeta de ese día muestra los juegos que de verdad se jugaron en el vuelo, con «Hecho», en el orden del vuelo.
 * Usan el ViewModel y la base de verdad (Robolectric). La guía en sí (cuándo sale, el velo, los botones) está en `HoyGuideOverlayTest`.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class GuiaPrimerIngresoTest {
  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
  }

  @After fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private val flightGames = BaselinePlan.steps.map { it.gameId }                    // el orden del vuelo: Rastro de luz, Freno de emergencia, Aterrizaje lunar y Lluvia de meteoros
  private fun today() = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())
  private fun daysAgo(n: Int) = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Calendar.getInstance().apply { add(Calendar.DAY_OF_YEAR, -n) }.time)
  private val db get() = NeuroVidaDatabase.getDatabase(app)
  private val repository get() = (app as NeuroVidaApplication).repository

  /** Un ViewModel que arranca con el recorrido guardado en [flight] (como si la app se abriera en el cierre del vuelo). */
  private fun viewModelWithFlight(flight: FlightState): NeuroVidaViewModel {
    FlightStore.save(app, flight)
    return NeuroVidaViewModel(app).also { viewModels.add(it) }
  }

  private fun fullFlight(measured: Map<String, Float> = flightGames.associateWith { 0.6f }, applied: Boolean = true) =
    FlightState(FlightMode.FULL, FlightStep.CAMINO, name = "Ana", age = AgeBand.ADULT, measured = measured, applied = applied)

  private fun savedToday(): DailySessionEntity? = runBlocking { db.dailySessionDao().getDailySessionSync(today()) }

  // ---------------------------------------------------------------- PARTE 2: solo la primera medalla

  @Test
  fun en_el_Primer_vuelo_completo_se_celebra_solo_Primer_paso() {
    val vm = viewModelWithFlight(fullFlight())
    vm.flightAchievements = listOf("primer_paso", "dia_completo", "dominios")
    vm.finishFlight()
    assertEquals(listOf("primer_paso"), vm.achievementQueue.value)
    assertNull("el vuelo terminó", vm.flight.value)
    assertEquals(AppTab.HOY, vm.currentTab.value)
    assertNull("el ViewModel no abre la guía solo: la abre quien la aloja cuando la cola se vacía", vm.guideStep.value)
    vm.dismissAchievement()
    assertTrue("cerrada la única medalla, la cola queda vacía (y Hoy queda libre para la guía)", vm.achievementQueue.value.isEmpty())
  }

  @Test
  fun si_Primer_paso_no_esta_entre_las_nuevas_no_se_celebra_ninguna() {
    val vm = viewModelWithFlight(fullFlight())
    vm.flightAchievements = listOf("dia_completo", "dominios")
    vm.finishFlight()
    assertTrue(vm.achievementQueue.value.isEmpty())
  }

  @Test
  fun repetir_la_evaluacion_queda_como_estaba_y_celebra_todas() {
    val vm = viewModelWithFlight(FlightState(FlightMode.GAMES, FlightStep.JUEGO_1, measured = flightGames.associateWith { 0.5f }, applied = true))
    vm.flightAchievements = listOf("primer_paso", "dia_completo", "dominios")
    vm.finishFlight()
    assertEquals(listOf("primer_paso", "dia_completo", "dominios"), vm.achievementQueue.value)
    assertNull("«Repetir la evaluación» nunca da el día por cumplido, así que no hay día de vuelo ni guía", savedToday()?.toDomain()?.takeIf { it.isFlightDay })
  }

  @Test
  fun las_medallas_sin_celebrar_quedan_ganadas_en_Tu_y_no_saltan_mas_tarde() {
    // los 4 juegos del vuelo se guardan como partidas de verdad (una por área): se ganan Primer paso, Día completo y Mente completa
    val repo = repository
    val newOnes = mutableListOf<String>()
    runBlocking {
      for ((i, game) in flightGames.withIndex()) {
        newOnes += repo.recordGameResult(GamePlayResult(gameId = game, score = 80, correctAnswers = 8, totalTrials = 10, timed = false, level = 2, timestamp = System.currentTimeMillis() + i), countsForDailySession = false).newAchievements
      }
    }
    assertTrue("el vuelo gana las tres: $newOnes", newOnes.containsAll(listOf("primer_paso", "dia_completo", "dominios")))
    val vm = viewModelWithFlight(fullFlight())
    vm.flightAchievements = newOnes
    vm.finishFlight()
    assertEquals("solo una celebración", listOf("primer_paso"), vm.achievementQueue.value)
    val unlocked = repo.achievementUnlocks.value.keys
    assertTrue("las tres medallas están ganadas y guardadas aunque solo se celebre una: $unlocked", unlocked.containsAll(listOf("primer_paso", "dia_completo", "dominios")))
    assertTrue("Tú › Medallas cuenta lo ganado: ${Achievements.medalCount(unlocked)} medallas", Achievements.medalCount(unlocked) >= 3)
    vm.dismissAchievement()
    // otra partida el mismo día: no vuelve a celebrar lo que ya se ganó sin celebrar
    val later = runBlocking { repo.recordGameResult(GamePlayResult(gameId = "radar", score = 80, correctAnswers = 8, totalTrials = 10, timed = false, level = 2, timestamp = System.currentTimeMillis() + 10)) }
    assertFalse("Día completo no salta más tarde", "dia_completo" in later.newAchievements)
    assertFalse("Mente completa tampoco", "dominios" in later.newAchievements)
    assertTrue("ni queda una celebración pendiente", vm.achievementQueue.value.isEmpty())
  }

  // ---------------------------------------------------------------- PARTE 3: la tarjeta del día del vuelo con los juegos de verdad

  @Test
  fun el_dia_del_vuelo_la_tarjeta_muestra_los_4_juegos_jugados_en_el_orden_del_vuelo_con_Hecho() {
    runBlocking { repository.completeTodayWithFlight(flightGames) }
    val s = repository.dailySession.value
    assertEquals("los 4 juegos del vuelo, en su orden (no la cola que armó DailyPath)", flightGames, s.gameIds)
    assertEquals("los 4 con «Hecho»", 4, s.completedCount)
    assertTrue(s.scores.isEmpty())
    assertTrue("«4 de 4 · ruta completa» y la línea del vuelo salen de isFlightDay, que sigue siendo verdadero", s.isFlightDay)
    // en Room, no solo en memoria: sobrevive a cerrar la app
    val saved = savedToday()!!.toDomain()
    assertEquals(flightGames, saved.gameIds); assertEquals(4, saved.completedCount); assertTrue(saved.isFlightDay)
  }

  @Test
  fun con_Terminar_despues_los_juegos_sin_jugar_no_aparecen() {
    val played = listOf(flightGames[0], flightGames[2])                      // se dejaron para después Freno de emergencia y Lluvia de meteoros
    runBlocking { repository.completeTodayWithFlight(played) }
    val s = repository.dailySession.value
    assertEquals(played, s.gameIds)
    assertEquals(2, s.completedCount)
    assertTrue(s.isFlightDay)
    assertEquals(played, savedToday()!!.toDomain().gameIds)
  }

  @Test
  fun si_no_se_jugo_ninguno_el_dia_no_se_da_por_cumplido() {
    val before = repository.dailySession.value
    runBlocking { repository.completeTodayWithFlight(emptyList()) }
    val after = repository.dailySession.value
    assertEquals("la ruta de hoy queda como estaba, para jugarla", before.gameIds, after.gameIds)
    assertEquals(0, after.completedCount)
    assertFalse(after.isFlightDay)
  }

  @Test
  fun un_camino_que_ya_tiene_puntajes_no_se_toca() {
    runBlocking { db.dailySessionDao().insertOrUpdate(DailySessionEntity(today(), "bodega,calculo,intrusa", completedCount = 1, scoresRaw = "85")) }
    runBlocking { repository.completeTodayWithFlight(flightGames) }
    val saved = savedToday()!!.toDomain()
    assertEquals(listOf("bodega", "calculo", "intrusa"), saved.gameIds)
    assertEquals(1, saved.completedCount)
    assertEquals(listOf(85), saved.scores)
  }

  @Test
  fun finishFlight_da_el_dia_por_cumplido_con_los_juegos_medidos_en_el_orden_del_vuelo() {
    // se midieron 3 de los 4 (Aterrizaje se dejó para después); el mapa llega en otro orden: la tarjeta sigue el orden del vuelo
    val measured = linkedMapOf(flightGames[3] to 0.7f, flightGames[0] to 0.5f, flightGames[1] to 0.4f)
    val vm = viewModelWithFlight(fullFlight(measured = measured))
    vm.finishFlight()
    TestSupport.awaitUntil(message = "la tarjeta del día del vuelo no se guardó") { vm.dailySession.value.isFlightDay && vm.dailySession.value.gameIds.size == 3 }
    assertEquals(listOf(flightGames[0], flightGames[1], flightGames[3]), vm.dailySession.value.gameIds)
    assertEquals(3, vm.dailySession.value.completedCount)
    assertEquals(listOf(flightGames[0], flightGames[1], flightGames[3]), savedToday()!!.toDomain().gameIds)
  }

  @Test
  fun repetir_la_evaluacion_no_toca_la_tarjeta_de_hoy() {
    val vm = viewModelWithFlight(FlightState(FlightMode.GAMES, FlightStep.JUEGO_1, measured = flightGames.associateWith { 0.5f }, applied = true))
    TestSupport.awaitUntil(message = "el arranque no quedó listo") { vm.startupStage.value == com.example.viewmodel.StartupStage.READY }
    val before = vm.dailySession.value
    vm.finishFlight()
    val end = System.currentTimeMillis() + 300
    while (System.currentTimeMillis() < end) { org.robolectric.Shadows.shadowOf(android.os.Looper.getMainLooper()).idle(); Thread.sleep(20) }
    assertEquals(before.gameIds, vm.dailySession.value.gameIds)
    assertEquals(0, vm.dailySession.value.completedCount)
    assertFalse(vm.dailySession.value.isFlightDay)
  }

  @Test
  fun manana_la_ruta_normal_arranca_bien_aunque_ayer_fueran_los_4_del_vuelo() {
    // ayer: el día del vuelo, con los 4 juegos hechos y sin puntajes
    runBlocking { db.dailySessionDao().insertOrUpdate(DailySessionEntity(daysAgo(1), flightGames.joinToString(","), completedCount = 4, scoresRaw = "")) }
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(message = "la ruta de hoy no se armó") { vm.startupStage.value == com.example.viewmodel.StartupStage.READY }
    val s = vm.dailySession.value
    assertEquals("hoy hay una ruta normal de 3 juegos", 3, s.gameIds.size)
    assertEquals("sin empezar", 0, s.completedCount)
    assertFalse("hoy ya no es el día del vuelo: sin guía y con el botón «Empezar la ruta»", s.isFlightDay)
    assertEquals("la ruta nueva existe en Room", 3, savedToday()!!.toDomain().gameIds.size)
  }
}
