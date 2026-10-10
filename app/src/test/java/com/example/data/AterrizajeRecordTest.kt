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
 * «Aterrizaje Lunar renovado»: la telemetría de Unity llega completa a la app (`NativeReceiver.parse`: dianas, cúpulas y racha mayor, además del error medio y la regla de siempre) y las cúpulas
 * de tu base quedan en las preferencias del respaldo (`aterrizaje_record`) sin bajar nunca.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class AterrizajeRecordTest {

  private lateinit var app: Application
  private lateinit var repo: NeuroVidaRepository

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    app.getSharedPreferences("aterrizaje_record", android.content.Context.MODE_PRIVATE).edit().clear().commit()
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

  private fun game(domes: Int, game: String = "aterrizaje") = NativeReceiver.parse(
    """{"user_id":"u1","game_id":"$game","session_metrics":{"correct_trials":12,"total_trials":15,"calculated_score":71,"average_response_time_ms":1450,"level":4,"timed":false,"end_rating":0.5,"peak_level":5,"numline_error_pct":8.4,"numline_true":[0.1,0.2,0.5,0.5,0.9,0.9],"numline_given":[0.12,0.18,0.55,0.45,0.88,0.93],"numline_bullseyes":3,"domes":$domes,"land_streak":5}}"""
  )!!

  private fun prefs() = app.getSharedPreferences("aterrizaje_record", android.content.Context.MODE_PRIVATE)

  @Test
  fun `la telemetria del aterrizaje llega entera a la app`() {
    val r = game(domes = 2)
    assertEquals(3, r.numlineBullseyes)
    assertEquals(2, r.landDomes)
    assertEquals(5, r.landStreak)
    assertEquals("la medida de siempre no se toca", 8.4f, r.numlineErrorPct!!, 1e-4f)
    assertEquals(6, r.numlineTrue!!.size)
    assertEquals(6, r.numlineGiven!!.size)
  }

  @Test
  fun `una partida de la version vieja no trae cupulas ni racha`() {
    val old = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"aterrizaje","session_metrics":{"correct_trials":10,"total_trials":14,"calculated_score":60,"average_response_time_ms":1500,"level":3,"timed":false,"numline_error_pct":12.0}}"""
    )!!
    assertNull("sin el dato no se inventa", old.landDomes)
    assertNull(old.landStreak)
    assertEquals(12f, old.numlineErrorPct!!, 1e-4f)
  }

  // Cada prueba hace UNA sola llamada a recordGameResult (varias seguidas dejan trabajo de fondo que a veces rompe la base de la prueba siguiente). Que las cúpulas nunca bajan lo prueba `AterrizajeTest`.

  @Test
  fun `las cupulas de una partida quedan guardadas en el respaldo`() = runBlocking {
    repo.recordGameResult(game(domes = 2), countsForDailySession = false)
    assertEquals(Aterrizaje.Totals(2, 2), repo.aterrizajeTotals.value)
    assertEquals(2, prefs().getInt("total", -1))
    assertEquals(2, prefs().getInt("best", -1))
    val m = repo.starMeasures.value.lastOrNull { it.key == "numline" }
    assertNotNull("la medida de siempre se sigue guardando", m)
    assertEquals(8.4f, m!!.value, 1e-3f)
  }

  @Test
  fun `una partida de otro juego no toca las cupulas`() = runBlocking {
    repo.recordGameResult(game(domes = 4, game = "calculo"), countsForDailySession = false)
    assertEquals(Aterrizaje.Totals(), repo.aterrizajeTotals.value)
    assertEquals(-1, prefs().getInt("total", -1))
  }
}
