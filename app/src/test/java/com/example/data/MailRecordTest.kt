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
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * «La estación de correo»: la telemetría de Unity llega completa a la app (`NativeReceiver.parse`), la medida «tu memoria para lo pendiente» se guarda solo si hubo encargos y el récord (cartas bien puestas en un día perfecto)
 * queda en las preferencias del respaldo y nunca baja.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class MailRecordTest {

  private lateinit var app: Application
  private lateinit var repo: NeuroVidaRepository

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    app.getSharedPreferences("correo_record", android.content.Context.MODE_PRIVATE).edit().clear().commit()
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

  private fun game(best: Int, ok: Int = 8, all: Int = 11, game: String = "correo") = NativeReceiver.parse(
    """{"user_id":"u1","game_id":"$game","session_metrics":{"correct_trials":$ok,"total_trials":$all,"calculated_score":70,"average_response_time_ms":0,"level":3,"timed":false,"end_rating":0.5,"peak_level":5,"mail_ev_hits":4,"mail_ev_total":5,"mail_time_hits":2,"mail_time_total":4,"mail_cancels":2,"mail_commissions":1,"mail_early":1,"mail_peeks":3,"mail_peeks_good":2,"mail_right":40,"mail_sorted":44,"mail_best_combo":9,"mail_days_perfect":1,"mail_group":3,"mail_best":$best,"mail_new":1}}"""
  )!!

  private fun saved() = app.getSharedPreferences("correo_record", android.content.Context.MODE_PRIVATE).getInt("best", -1)

  @Test
  fun `la telemetria de la estacion llega entera a la app`() {
    val r = game(best = 24)
    assertEquals(4, r.mailEvHits)
    assertEquals(5, r.mailEvTotal)
    assertEquals(2, r.mailTimeHits)
    assertEquals(4, r.mailTimeTotal)
    assertEquals(2, r.mailCancels)
    assertEquals(1, r.mailCommissions)
    assertEquals(1, r.mailEarly)
    assertEquals(3, r.mailPeeks)
    assertEquals(2, r.mailPeeksGood)
    assertEquals(40, r.mailRight)
    assertEquals(44, r.mailSorted)
    assertEquals(9, r.mailBestCombo)
    assertEquals(1, r.mailDaysPerfect)
    assertEquals(3, r.mailGroup)
    assertEquals(24, r.mailBest)
    assertEquals(true, r.mailNewRecord)
  }

  @Test
  fun `una partida de otro juego o de una version vieja no trae datos de la estacion`() {
    val other = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"calculo","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":4500,"level":3,"timed":false,"mail_group":2}}"""
    )!!
    assertNull(other.mailGroup)
    val old = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"correo","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":0,"level":3,"timed":true,"mail_commissions":2,"mail_peeks":5}}"""
    )!!
    assertNull("sin el grupo de etapas no es la estación nueva", old.mailGroup)
  }

  // Cada prueba hace UNA sola llamada a recordGameResult (varias seguidas dejan trabajo de fondo que a veces rompe la base de la prueba siguiente). Que el récord nunca baja lo prueba `MailTest.mergeRecord`.

  @Test
  fun `el record y la medida de una partida quedan guardados`() = runBlocking {
    repo.recordGameResult(game(best = 24), countsForDailySession = false)
    assertEquals(24, repo.correoRecord.value)
    assertEquals(24, saved())
    val m = repo.starMeasures.value.lastOrNull { it.key == "estacion" }
    assertNotNull(m)
    assertEquals(100f * 8 / 11, m!!.value, 1e-3f)
  }

  @Test
  fun `una partida sin encargos no guarda medida y el record queda como estaba`() = runBlocking {
    repo.recordGameResult(game(best = 0, ok = 0, all = 0), countsForDailySession = false)
    assertNull(repo.starMeasures.value.lastOrNull { it.key == "estacion" })
    assertEquals(0, repo.correoRecord.value)
  }

  @Test
  fun `una partida de otro juego no toca el record`() = runBlocking {
    val other = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"calculo","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":4500,"level":3,"timed":false,"mail_best":9}}"""
    )!!
    repo.recordGameResult(other, countsForDailySession = false)
    assertEquals(0, repo.correoRecord.value)
    assertEquals(-1, saved())
  }

  @Test
  fun `la medida tiene su nombre, su juego y se lee con su clave`() {
    val def = StarMeasures.def("estacion")!!
    assertEquals("correo", def.gameId)
    assertTrue(def.title.contains("Correo Estelar"))
    assertEquals(def, StarMeasures.defForGame("correo"))
  }
}
