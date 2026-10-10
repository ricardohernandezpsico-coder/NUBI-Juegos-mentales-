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
 * «Acoplamiento: muelle de acoplamiento»: la telemetría de Unity llega completa a la app (`NativeReceiver.parse`: módulos, anillos, récord y las dos medidas de siempre) y el récord y los totales quedan en las preferencias del respaldo (`acoplamiento_record`) y nunca bajan.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class AcoplamientoRecordTest {

  private lateinit var app: Application
  private lateinit var repo: NeuroVidaRepository

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    app.getSharedPreferences("acoplamiento_record", android.content.Context.MODE_PRIVATE).edit().clear().commit()
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

  private fun game(best: Int, docked: Int = 17, rings: Int = 2, game: String = "acoplamiento") = NativeReceiver.parse(
    """{"user_id":"u1","game_id":"$game","session_metrics":{"correct_trials":$docked,"total_trials":20,"calculated_score":71,"average_response_time_ms":1450,"level":6,"timed":true,"end_rating":0.5,"peak_level":7,"rotation_speed_dps":212,"rotation_curve_ms":[900,1100,1400,1750,2100],"docked":$docked,"rings":$rings,"dock_best":$best,"dock_new":1}}"""
  )!!

  private fun prefs() = app.getSharedPreferences("acoplamiento_record", android.content.Context.MODE_PRIVATE)

  @Test
  fun `la telemetria del acoplamiento llega entera a la app`() {
    val r = game(best = 18)
    assertEquals(17, r.dockDocked)
    assertEquals(2, r.dockRings)
    assertEquals(18, r.dockBest)
    assertEquals(true, r.dockNewRecord)
    assertEquals("las medidas de siempre no se tocan", 212, r.rotationSpeedDps)
    assertEquals(listOf(900, 1100, 1400, 1750, 2100), r.rotationCurveMs)
  }

  @Test
  fun `una partida de la version vieja no trae datos de modulos`() {
    val old = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"acoplamiento","session_metrics":{"correct_trials":10,"total_trials":14,"calculated_score":60,"average_response_time_ms":1500,"level":3,"timed":true,"rotation_speed_dps":190}}"""
    )!!
    assertNull("sin los módulos no es el juego nuevo", old.dockDocked)
    assertNull(old.dockRings)
    assertNull(old.dockBest)
    assertNull(old.dockNewRecord)
    assertEquals(190, old.rotationSpeedDps)
  }

  // Cada prueba hace UNA sola llamada a recordGameResult (varias seguidas dejan trabajo de fondo que a veces rompe la base de la prueba siguiente). Que el récord nunca baja lo prueba `AcoplamientoTest.el record nunca baja ni es negativo`.

  @Test
  fun `el record y los totales de una partida quedan guardados en el respaldo`() = runBlocking {
    repo.recordGameResult(game(best = 18), countsForDailySession = false)
    assertEquals(18, repo.acoplamientoRecord.value)
    assertEquals(18, prefs().getInt("best", -1))
    assertEquals(Acoplamiento.Totals(17, 2), repo.acoplamientoTotals.value)
    assertEquals("los totales van en las mismas preferencias del respaldo", 17, prefs().getInt("total", -1))
    assertEquals(2, prefs().getInt("rings", -1))
    val m = repo.starMeasures.value.lastOrNull { it.key == "rotation" }
    assertNotNull("la medida de siempre se sigue guardando", m)
    assertEquals(212f, m!!.value, 1e-3f)
  }

  @Test
  fun `una partida de otro juego no toca el record ni los totales`() = runBlocking {
    repo.recordGameResult(game(best = 9, game = "calculo"), countsForDailySession = false)
    assertEquals(0, repo.acoplamientoRecord.value)
    assertEquals(-1, prefs().getInt("best", -1))
    assertEquals(Acoplamiento.Totals(), repo.acoplamientoTotals.value)
  }
}
