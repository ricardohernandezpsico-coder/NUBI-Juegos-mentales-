package com.example.data

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.bridge.NativeReceiver
import com.example.data.local.NeuroVidaDatabase
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * «Rescate relámpago: qué cápsulas viste»: la telemetría de Unity llega completa a la app (`NativeReceiver.parse`, sin medidas por lugar), «tu vistazo» se guarda como medida y el récord de cápsulas queda en las preferencias del respaldo y nunca baja.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class RescateRecordTest {

  private lateinit var app: Application
  private lateinit var repo: NeuroVidaRepository

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    app.getSharedPreferences("rescate_record", android.content.Context.MODE_PRIVATE).edit().clear().commit()
    repo = NeuroVidaRepository(app)
    TestSupport.awaitUntil(message = "La base de datos no se inicializó") {
      runBlocking { NeuroVidaDatabase.getDatabase(app).gameProgressDao().getAllProgressSync().isNotEmpty() }
    }
  }

  @After
  fun tearDown() {
    TestSupport.release(repo)             // cancela su trabajo de fondo ANTES de soltar la base
    TestSupport.resetDatabase()
  }

  private fun game(best: Int, rescued: Int = 23, game: String = "radar", trips: Int = 2) = NativeReceiver.parse(
    """{"user_id":"u1","game_id":"$game","session_metrics":{"correct_trials":9,"total_trials":12,"calculated_score":71,"average_response_time_ms":0,"level":6,"timed":false,"end_rating":0.5,"peak_level":7,"glance_ms":280,"glance_load":3.5,"capture":3.25,"resc_rescued":$rescued,"resc_perfect":5,"resc_rounds":12,"resc_best_streak":3,"resc_shortest_ms":150,"resc_best":$best,"resc_new":1,"resc_trips":$trips}}"""
  )!!

  private fun saved() = app.getSharedPreferences("rescate_record", android.content.Context.MODE_PRIVATE).getInt("best", -1)

  @Test
  fun `la telemetria del rescate llega entera a la app`() {
    val r = game(best = 24)
    assertEquals(23, r.rescRescued)
    assertEquals(5, r.rescPerfect)
    assertEquals(12, r.rescRounds)
    assertEquals(3, r.rescBestStreak)
    assertEquals(150, r.rescShortestMs)
    assertEquals(24, r.rescBest)
    assertEquals(true, r.rescNewRecord)
    assertEquals(2, r.rescTrips)
    assertEquals(280, r.glanceMs)
    assertEquals(3.5f, r.glanceLoad!!, 1e-4f)
    assertEquals(3.25f, r.captureK!!, 1e-4f)
  }

  @Test
  fun `una partida de otro juego o de la version vieja no trae datos de rescate`() {
    val other = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"calculo","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":4500,"level":3,"timed":false,"resc_rescued":9,"resc_best":9}}"""
    )!!
    assertEquals(9, other.rescRescued)            // el campo se lee tal cual; el repositorio solo guarda el récord de «radar»
    val old = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"radar","session_metrics":{"correct_trials":4,"total_trials":6,"calculated_score":60,"average_response_time_ms":0,"level":3,"timed":true,"glance_ms":300,"glance_load":3.0}}"""
    )!!
    assertNull("sin las cápsulas no es el juego nuevo", old.rescRescued)
    assertNull(old.rescBest)
    assertNull(old.rescNewRecord)
    assertNull("sin viajes la versión vieja no inventa ninguno", old.rescTrips)
    assertEquals(300, old.glanceMs)
  }

  // Cada prueba hace UNA sola llamada a recordGameResult (varias seguidas dejan trabajo de fondo que a veces rompe la base de la prueba siguiente). Que el récord nunca baja lo prueba `RescateTest.el record nunca baja ni es negativo`.

  @Test
  fun `el record y la medida de una partida quedan guardados`() = runBlocking {
    repo.recordGameResult(game(best = 24), countsForDailySession = false)
    assertEquals(24, repo.rescateRecord.value)
    assertEquals(24, saved())
    assertEquals(Rescate.Totals(23, 2), repo.rescateTotals.value)
    val prefs = app.getSharedPreferences("rescate_record", android.content.Context.MODE_PRIVATE)
    assertEquals("los totales van en las mismas preferencias del respaldo", 23, prefs.getInt("total", -1))
    assertEquals(2, prefs.getInt("trips", -1))
    val m = repo.starMeasures.value.lastOrNull { it.key == "glance" }
    assertNotNull(m)
    assertEquals(280f, m!!.value, 1e-3f)
  }

  @Test
  fun `una partida de otro juego no toca el record`() = runBlocking {
    val other = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"calculo","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":4500,"level":3,"timed":false,"resc_best":9}}"""
    )!!
    repo.recordGameResult(other, countsForDailySession = false)
    assertEquals(0, repo.rescateRecord.value)
    assertEquals(-1, saved())
    assertEquals(Rescate.Totals(), repo.rescateTotals.value)
  }
}
