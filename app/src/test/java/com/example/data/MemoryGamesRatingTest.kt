package com.example.data

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.bridge.NativeReceiver
import com.example.data.local.NeuroVidaDatabase
import com.example.model.AgeBand
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * Parejas Ocultas y Secuencia Lumínica ya guardan y retoman el rating del DDA común (3-oct), como los demás juegos:
 * antes estaban en `OWN_ENGINE_GAMES` y no se sembraban ni se retomaban igual. Usa la base de datos de verdad
 * (Robolectric), sin Unity.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class MemoryGamesRatingTest {
  private lateinit var app: Application
  private lateinit var repo: NeuroVidaRepository

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    repo = NeuroVidaRepository(app)
    awaitDefaults() // la base de datos se llena sola al crear el repositorio: esperar para no competir con ella
  }

  @After
  fun tearDown() {
    TestSupport.resetDatabase()
  }

  private fun sequence(endRating: Double) = NativeReceiver.parse(
    """{"user_id":"u1","game_id":"secuencia","session_metrics":{"correct_rounds":9,"total_rounds":12,"calculated_score":72,"average_response_time_ms":640.0,"final_span_length":6,"level":3,"timed":false,"peak_level":9,"end_rating":$endRating,"mode_trials":10,"mode_hits":8}}"""
  )!!

  private fun cards(endRating: Double) = NativeReceiver.parse(
    """{"user_id":"u1","game_id":"parejas","session_metrics":{"matched_pairs":30,"attempts":41,"calculated_score":74,"level":3,"timed":true,"end_rating":$endRating,"peak_level":7,"mode_trials":39,"mode_hits":30}}"""
  )!!

  /** Lo que lee el lanzador para arrancar el juego (`UnityGameLauncher`: `gameDdaRating`). */
  private fun launchRating(game: String): Float {
    val end = System.currentTimeMillis() + 5_000
    while (System.currentTimeMillis() < end) {
      val r = repo.gameDdaRating.value[game] ?: -1f
      if (r >= 0f) return r
      Thread.sleep(20)
    }
    return repo.gameDdaRating.value[game] ?: -1f
  }

  private fun awaitDefaults() = TestSupport.awaitUntil(message = "La base de datos no se inicializó") {
    runBlocking { NeuroVidaDatabase.getDatabase(app).gameProgressDao().getAllProgressSync().isNotEmpty() }
  }

  private fun savedRating(game: String): Float = runBlocking {
    NeuroVidaDatabase.getDatabase(app).gameProgressDao().getProgressForGameSync(game)?.ddaRating ?: -1f
  }

  @Test
  fun `el rating de Secuencia se guarda, se suaviza y se retoma`() = runBlocking {
    assertEquals(-1f, savedRating("secuencia"), 1e-6f)
    repo.recordGameResult(sequence(0.5), countsForDailySession = false)
    assertEquals(0.5f, savedRating("secuencia"), 1e-5f)              // primera partida: el rating tal cual
    assertEquals(0.5f, launchRating("secuencia"), 1e-5f)            // y el lanzador lo manda a Unity
    repo.recordGameResult(sequence(0.9), countsForDailySession = false)
    assertEquals(0.6f * 0.9f + 0.4f * 0.5f, savedRating("secuencia"), 1e-5f)  // 60 % partida / 40 % anterior
  }

  @Test
  fun `el rating de Parejas se guarda, se suaviza y se retoma`() = runBlocking {
    assertEquals(-1f, savedRating("parejas"), 1e-6f)
    repo.recordGameResult(cards(0.3), countsForDailySession = false)
    assertEquals(0.3f, savedRating("parejas"), 1e-5f)
    assertEquals(0.3f, launchRating("parejas"), 1e-5f)
    repo.recordGameResult(cards(0.8), countsForDailySession = false)
    assertEquals(0.6f * 0.8f + 0.4f * 0.3f, savedRating("parejas"), 1e-5f)
  }

  @Test
  fun `una partida de Parejas de una version vieja sin end_rating no toca el rating`() = runBlocking {
    val old = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"parejas","session_metrics":{"matched_pairs":30,"attempts":41,"calculated_score":74,"level":3,"timed":true}}"""
    )!!
    repo.recordGameResult(old, countsForDailySession = false)
    assertEquals(-1f, savedRating("parejas"), 1e-6f)
  }

  @Test
  fun `el punto de partida estimado ya siembra el rating de los dos juegos`() = runBlocking {
    repo.applyPriorIfNeeded(AgeBand.ADULT, null)
    assertTrue("secuencia=${savedRating("secuencia")}", savedRating("secuencia") >= 0f)
    assertTrue("parejas=${savedRating("parejas")} stroop=${savedRating("stroop")}", savedRating("parejas") >= 0f)
    assertEquals(savedRating("stroop"), savedRating("parejas"), 1e-6f) // igual que cualquier juego del motor común
  }

  @Test
  fun `Secuencia ya no tiene objetivo propio y sus modos piden rondas`() {
    assertEquals(0.80f, Skill.trainingTarget("secuencia", AgeBand.ADULT), 1e-6f)
    assertEquals(0.85f, Skill.trainingTarget("secuencia", AgeBand.SENIOR), 1e-6f)
    assertTrue(Skill.passed("secuencia", PlayMode.DESAFIO, AgeBand.ADULT, hits = 5, trials = 6))
    assertFalse(Skill.passed("secuencia", PlayMode.DESAFIO, AgeBand.ADULT, hits = 4, trials = 6))
    assertTrue(Skill.passed("parejas", PlayMode.DESAFIO, AgeBand.ADULT, hits = 30, trials = 36))
  }
}
