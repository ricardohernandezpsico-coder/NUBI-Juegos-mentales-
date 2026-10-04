package com.example.data

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.bridge.NativeReceiver
import com.example.bridge.UnityGameLauncher
import com.example.data.local.NeuroVidaDatabase
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** Las palabras azules de «En la punta de la lengua»: se guardan al terminar la partida (en cualquier modo) y se mandan a Unity en la siguiente. */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class PuntaPendingTest {
  private lateinit var app: Application
  private lateinit var repo: NeuroVidaRepository

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    app.getSharedPreferences("punta_words", android.content.Context.MODE_PRIVATE).edit().clear().commit()
    repo = NeuroVidaRepository(app)
    TestSupport.awaitUntil(message = "La base de datos no se inicializó") {
      runBlocking { NeuroVidaDatabase.getDatabase(app).gameProgressDao().getAllProgressSync().isNotEmpty() }
    }
  }

  @After
  fun tearDown() {
    TestSupport.resetDatabase()
  }

  private fun game(blue: String, cleared: String = "", solo: Int = 4, words: String = "reloj:o") = NativeReceiver.parse(
    """{"user_id":"u1","game_id":"anagramas","session_metrics":{"correct_trials":7,"total_trials":8,"calculated_score":71,"average_response_time_ms":3100,"level":3,"timed":false,"end_rating":0.5,"peak_level":4,"mode_trials":6,"mode_hits":5,"punta_solo":$solo,"punta_pista":1,"punta_letras":0,"punta_vista":2,"punta_ms":3000,"punta_words":"$words","punta_blue":"$blue","punta_cleared":"$cleared"}}"""
  )!!

  private fun saved() = app.getSharedPreferences("punta_words", android.content.Context.MODE_PRIVATE).getString("pending", "")

  @Test
  fun `las azules de una partida quedan guardadas y listas para mandar a Unity`() = runBlocking {
    assertTrue(repo.puntaPending.value.isEmpty())
    repo.recordGameResult(game(blue = "búho;faro"), countsForDailySession = false)
    assertEquals(listOf("búho", "faro"), repo.puntaPending.value)
    assertEquals("búho;faro", saved())          // en las preferencias que SÍ van en el respaldo (punta_words)
  }

  @Test
  fun `la partida siguiente suma las nuevas y quita las que se encontraron`() = runBlocking {
    repo.recordGameResult(game(blue = "búho;faro"), countsForDailySession = false)
    repo.recordGameResult(game(blue = "nido", cleared = "faro"), countsForDailySession = false)
    assertEquals(listOf("búho", "nido"), repo.puntaPending.value)
    assertEquals("búho;nido", saved())
  }

  @Test
  fun `una partida de otro juego no toca las azules`() = runBlocking {
    repo.recordGameResult(game(blue = "búho"), countsForDailySession = false)
    val other = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"calculo","session_metrics":{"correct_trials":7,"total_trials":8,"calculated_score":71,"average_response_time_ms":3100,"level":3,"timed":false,"punta_blue":"mesa"}}"""
    )!!
    repo.recordGameResult(other, countsForDailySession = false)
    assertEquals(listOf("búho"), repo.puntaPending.value)
  }

  @Test
  fun `una version vieja sin los campos nuevos no guarda nada`() = runBlocking {
    val old = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"anagramas","session_metrics":{"correct_trials":7,"total_trials":8,"calculated_score":71,"average_response_time_ms":3100,"level":3,"timed":false}}"""
    )!!
    repo.recordGameResult(old, countsForDailySession = false)
    assertTrue(repo.puntaPending.value.isEmpty())
  }

  @Test
  fun `el lanzador manda el tutorial a quien nunca jugo y las azules guardadas`() {
    runBlocking { repo.recordGameResult(game(blue = "búho;faro"), countsForDailySession = false) }
    assertEquals("búho;faro", Punta.encode(repo.puntaPending.value))
    assertTrue(UnityGameLauncher.shouldShowTutorial("anagramas", emptyList()))
    assertFalse(UnityGameLauncher.shouldShowTutorial("series", emptyList()))
    assertTrue("anagramas" in UnityGameLauncher.TUTORIAL_GAMES)
  }

  @Test
  fun `la medida de la partida se guarda en las medidas del juego`() = runBlocking {
    repo.recordGameResult(game(blue = "", solo = 4, words = "a:o"), countsForDailySession = false)
    // 4 de (4+1+0+2)=7 solas → 57 %
    val p = repo.starMeasures.value.lastOrNull { it.key == "punta" }
    assertNotNull(p)
    assertEquals(100f * 4 / 7, p!!.value, 1e-3f)
  }
}
