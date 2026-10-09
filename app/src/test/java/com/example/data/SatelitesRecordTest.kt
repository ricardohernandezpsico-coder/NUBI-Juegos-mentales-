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
 * «Satélites: enciende tu planeta»: la telemetría de Unity llega completa a la app (`NativeReceiver.parse`), «tu seguimiento» se guarda como medida y el récord de luces queda en las preferencias del respaldo y nunca baja.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class SatelitesRecordTest {

  private lateinit var app: Application
  private lateinit var repo: NeuroVidaRepository

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    app.getSharedPreferences("satelites_record", android.content.Context.MODE_PRIVATE).edit().clear().commit()
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

  private fun game(best: Int, lights: Int = 17, game: String = "satelites") = NativeReceiver.parse(
    """{"user_id":"u1","game_id":"$game","session_metrics":{"correct_trials":5,"total_trials":8,"calculated_score":71,"average_response_time_ms":0,"level":3,"timed":false,"end_rating":0.5,"peak_level":6,"tracking_capacity":2.6,"tracking_targets":3.5,"tracking_speed":1.6,"sat_lights":$lights,"sat_perfect":5,"sat_best_streak":3,"sat_best":$best,"sat_new":1}}"""
  )!!

  private fun saved() = app.getSharedPreferences("satelites_record", android.content.Context.MODE_PRIVATE).getInt("best", -1)

  @Test
  fun `la telemetria de los satelites llega entera a la app`() {
    val r = game(best = 24)
    assertEquals(17, r.satLights)
    assertEquals(5, r.satPerfect)
    assertEquals(3, r.satBestStreak)
    assertEquals(24, r.satBest)
    assertEquals(true, r.satNewRecord)
    assertEquals(2.6f, r.trackingCapacity!!, 1e-4f)
    assertEquals(3.5f, r.trackingTargets!!, 1e-4f)
    assertEquals(1.6f, r.trackingSpeed!!, 1e-4f)
  }

  @Test
  fun `una partida de otro juego o de la version vieja no trae datos de luces`() {
    val other = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"calculo","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":4500,"level":3,"timed":false,"sat_lights":9,"sat_best":9}}"""
    )!!
    assertNull(other.satLights)
    assertNull(other.satBest)
    val old = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"satelites","session_metrics":{"correct_trials":4,"total_trials":6,"calculated_score":60,"average_response_time_ms":0,"level":3,"timed":true,"tracking_capacity":2.1,"tracking_targets":3.0}}"""
    )!!
    assertNull("sin las luces no es el juego nuevo", old.satLights)
    assertNull(old.satBest)
    assertEquals(2.1f, old.trackingCapacity!!, 1e-4f)
  }

  // Cada prueba hace UNA sola llamada a recordGameResult (varias seguidas dejan trabajo de fondo que a veces rompe la base de la prueba siguiente). Que el récord nunca baja lo prueba `SatelitesTest.el record nunca baja ni es negativo`.

  @Test
  fun `el record y la medida de una partida quedan guardados`() = runBlocking {
    repo.recordGameResult(game(best = 24), countsForDailySession = false)
    assertEquals(24, repo.satelitesRecord.value)
    assertEquals(24, saved())
    val m = repo.starMeasures.value.lastOrNull { it.key == "tracking" }
    assertNotNull(m)
    assertEquals(2.6f, m!!.value, 1e-3f)
  }

  @Test
  fun `una partida de otro juego no toca el record`() = runBlocking {
    val other = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"calculo","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":4500,"level":3,"timed":false,"sat_best":9}}"""
    )!!
    repo.recordGameResult(other, countsForDailySession = false)
    assertEquals(0, repo.satelitesRecord.value)
    assertEquals(-1, saved())
  }
}
