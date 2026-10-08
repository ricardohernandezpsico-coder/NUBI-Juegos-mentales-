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

/** El récord de «Constelaciones» (la racha de memoria más larga): se guarda al terminar la partida (en cualquier modo), nunca baja y va en las preferencias del respaldo; la medida «memoria de lugar» se guarda solo si hubo oportunidades. */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class ConstelacionesRecordTest {

  private lateinit var app: Application
  private lateinit var repo: NeuroVidaRepository

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    app.getSharedPreferences("constelaciones_record", android.content.Context.MODE_PRIVATE).edit().clear().commit()
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

  private fun game(best: Int, hits: Int = 7, opps: Int = 10, game: String = "parejas") = NativeReceiver.parse(
    """{"user_id":"u1","game_id":"$game","session_metrics":{"matched_pairs":14,"attempts":38,"calculated_score":70,"level":3,"timed":false,"end_rating":0.5,"peak_level":9,"mode_trials":22,"mode_hits":15,"con_hits":$hits,"con_opps":$opps,"con_groups":14,"con_mem_groups":5,"con_best_streak":$best,"con_useless":6,"con_turns":38,"con_group":3,"con_best":$best,"con_new":1}}"""
  )!!

  private fun saved() = app.getSharedPreferences("constelaciones_record", android.content.Context.MODE_PRIVATE).getInt("best", -1)

  @Test
  fun `empieza sin record`() {
    assertEquals(0, repo.constelacionesRecord.value)
  }

  // Cada prueba hace UNA sola llamada a recordGameResult (varias seguidas dejan trabajo de fondo que a veces rompe la base de la prueba siguiente). Que el récord nunca baja lo prueba `ConstelacionesTest`.

  @Test
  fun `el record de una partida queda guardado en las preferencias del respaldo y la medida en las medidas`() = runBlocking {
    repo.recordGameResult(game(best = 5), countsForDailySession = false)
    assertEquals(5, repo.constelacionesRecord.value)
    assertEquals(5, saved())
    val p = repo.starMeasures.value.lastOrNull { it.key == "place" }
    assertNotNull(p)
    assertEquals(70f, p!!.value, 1e-3f)      // 7 de 10 veces fue directo a una pareja ya vista
  }

  @Test
  fun `una partida sin oportunidades no guarda medida pero si el record`() = runBlocking {
    repo.recordGameResult(game(best = 0, hits = 0, opps = 0), countsForDailySession = false)
    assertNull(repo.starMeasures.value.lastOrNull { it.key == "place" })
    assertEquals(0, repo.constelacionesRecord.value)
  }

  @Test
  fun `una partida de otro juego no toca el record`() = runBlocking {
    val other = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"calculo","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":4500,"level":3,"timed":false,"con_best":9}}"""
    )!!
    repo.recordGameResult(other, countsForDailySession = false)
    assertEquals(0, repo.constelacionesRecord.value)
    assertEquals(-1, saved())
  }

  @Test
  fun `una partida de una version vieja sin el record no lo toca`() = runBlocking {
    val old = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"parejas","session_metrics":{"matched_pairs":30,"attempts":41,"calculated_score":74,"level":3,"timed":true}}"""
    )!!
    repo.recordGameResult(old, countsForDailySession = false)
    assertEquals(0, repo.constelacionesRecord.value)
    assertNull(repo.starMeasures.value.lastOrNull { it.key == "place" })
  }

  @Test
  fun `la medida tiene su nombre y su juego`() {
    val def = StarMeasures.def("place")!!
    assertEquals("parejas", def.gameId)
    assertEquals("Constelaciones", StarMeasures.gameNames["parejas"])
    assertEquals(def, StarMeasures.defForGame("parejas"))
  }
}
