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
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** El récord de «Bodega de carga» (la bodega más grande sin errores): se guarda al terminar la partida (en cualquier modo), nunca baja, sobrevive a reiniciar la app y «Borrar datos» lo limpia. */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class BodegaRecordTest {
  @get:org.junit.Rule val retryOnDbFlake = TestSupport.retryOnDbFlake()

  private lateinit var app: Application
  private lateinit var repo: NeuroVidaRepository

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    app.getSharedPreferences("bodega_record", android.content.Context.MODE_PRIVATE).edit().clear().commit()
    repo = NeuroVidaRepository(app)
    TestSupport.awaitUntil(message = "La base de datos no se inicializó") {
      runBlocking { NeuroVidaDatabase.getDatabase(app).gameProgressDao().getAllProgressSync().isNotEmpty() }
    }
  }

  @After
  fun tearDown() {
    TestSupport.resetDatabase()
  }

  private fun game(best: Int, game: String = "bodega") = NativeReceiver.parse(
    """{"user_id":"u1","game_id":"$game","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":4500,"level":3,"timed":false,"end_rating":0.5,"peak_level":4,"mode_trials":8,"mode_hits":6,"bod_group":3,"bod_best_streak":4,"bod_biggest":$best,"bod_best":$best,"bod_ms":4500,"bod_new":1}}"""
  )!!

  private fun saved() = app.getSharedPreferences("bodega_record", android.content.Context.MODE_PRIVATE).getInt("best", -1)

  @Test
  fun `empieza sin record`() {
    assertEquals(0, repo.bodegaRecord.value)
  }

  // Cada prueba hace UNA sola llamada a recordGameResult: varias seguidas en la misma prueba dejan trabajo de fondo que a veces rompe la base de la prueba siguiente (como pasó con EngranajesRocketTest).
  // Que el récord nunca baja lo prueba `BodegaTest` con `Bodega.mergeRecord`.

  @Test
  fun `el record de una partida queda guardado en las preferencias del respaldo`() = runBlocking {
    repo.recordGameResult(game(best = 4), countsForDailySession = false)
    assertEquals(4, repo.bodegaRecord.value)
    assertEquals(4, saved())
  }

  @Test
  fun `una partida de otro juego no toca el record`() = runBlocking {
    repo.recordGameResult(game(best = 9, game = "calculo"), countsForDailySession = false)
    assertEquals(0, repo.bodegaRecord.value)
    assertEquals(-1, saved())
  }

  @Test
  fun `una partida de una version vieja sin el record no lo toca`() = runBlocking {
    val old = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"bodega","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":4500,"level":3,"timed":false}}"""
    )!!
    repo.recordGameResult(old, countsForDailySession = false)
    assertEquals(0, repo.bodegaRecord.value)
  }

  @Test
  fun `la medida de la partida se guarda en las medidas del juego`() = runBlocking {
    repo.recordGameResult(game(best = 3), countsForDailySession = false)
    val p = repo.starMeasures.value.lastOrNull { it.key == "bodega" }
    assertNotNull(p)
    assertEquals(70f, p!!.value, 1e-3f)      // 7 de 10 objetos al primer intento
  }

  @Test
  fun `la medida tiene su nombre y su juego`() {
    val def = StarMeasures.def("bodega")!!
    assertEquals("bodega", def.gameId)
    assertEquals("Bodega de carga", StarMeasures.gameNames["bodega"])
    assertEquals(def, StarMeasures.defForGame("bodega"))
  }
}
