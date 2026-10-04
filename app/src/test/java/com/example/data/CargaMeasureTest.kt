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

/** La medida de «Carga exacta» (cargas sin pista, en %) se guarda con las demás medidas de los juegos estrella. */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class CargaMeasureTest {
  private lateinit var app: Application
  private lateinit var repo: NeuroVidaRepository

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    repo = NeuroVidaRepository(app)
    TestSupport.awaitUntil(message = "La base de datos no se inicializó") {
      runBlocking { NeuroVidaDatabase.getDatabase(app).gameProgressDao().getAllProgressSync().isNotEmpty() }
    }
  }

  @After
  fun tearDown() {
    TestSupport.resetDatabase()
  }

  @Test
  fun `la medida de la partida se guarda en las medidas del juego`() = runBlocking {
    val r = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"calculo","session_metrics":{"correct_trials":7,"total_trials":8,"calculated_score":71,"average_response_time_ms":14200,"level":3,"timed":false,"end_rating":0.5,"peak_level":4,"mode_trials":6,"mode_hits":5,"carga_alone":5,"carga_hinted":2,"carga_short":3,"carga_ms":14200}}"""
    )!!
    repo.recordGameResult(r, countsForDailySession = false)
    val p = repo.starMeasures.value.lastOrNull { it.key == "carga" }
    assertNotNull(p)
    assertEquals(100f * 5 / 8, p!!.value, 1e-3f)
  }
}
