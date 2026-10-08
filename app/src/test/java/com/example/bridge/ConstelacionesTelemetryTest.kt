package com.example.bridge

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** El resultado de «Constelaciones» tal como lo manda Unity (JsonUtility: -1 = sin dato): aciertos de memoria y oportunidades, grupos, racha, etapa más alta y el récord. */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class ConstelacionesTelemetryTest {
  private fun json(extra: String, game: String = "parejas") =
    """{"user_id":"u1","game_id":"$game","session_metrics":{"matched_pairs":14,"attempts":38,"calculated_score":70,"level":3,"timed":false,"end_rating":0.5,"peak_level":9,"mode_trials":22,"mode_hits":15$extra}}"""

  @Test
  fun `lee los aciertos de memoria como aciertos y las oportunidades como ensayos`() {
    val r = NativeReceiver.parse(json(""","con_hits":7,"con_opps":10,"con_groups":14,"con_mem_groups":5,"con_best_streak":4,"con_useless":6,"con_turns":38,"con_group":3,"con_best":6,"con_new":1"""))
    assertNotNull(r)
    r!!
    assertEquals("parejas", r.gameId)
    assertEquals(7, r.correctAnswers)
    assertEquals(10, r.totalTrials)
    assertEquals(14, r.conGroups)
    assertEquals(5, r.conMemGroups)
    assertEquals(4, r.conBestStreak)
    assertEquals(6, r.conUseless)
    assertEquals(38, r.conTurns)
    assertEquals(3, r.conGroup)
    assertEquals(6, r.conBest)
    assertEquals(true, r.conNewRecord)
    assertEquals(0.5f, r.endRating!!, 1e-6f)
  }

  @Test
  fun `una partida sin oportunidades se lee con cero de cero`() {
    val r = NativeReceiver.parse(json(""","con_hits":0,"con_opps":0,"con_groups":3,"con_mem_groups":0,"con_best_streak":0,"con_useless":0,"con_turns":9,"con_group":1,"con_best":0,"con_new":0"""))!!
    assertEquals(0, r.correctAnswers)
    assertEquals(0, r.totalTrials)
    assertEquals(0, r.conBestStreak)
    assertEquals(false, r.conNewRecord)
  }

  @Test
  fun `Parejas Ocultas de una version vieja sigue leyendose con parejas e intentos`() {
    val r = NativeReceiver.parse(json(""))!!
    assertEquals(14, r.correctAnswers)
    assertEquals(38, r.totalTrials)
    assertNull(r.conGroup)
    assertNull(r.conBest)
    assertNull(r.conNewRecord)
  }

  @Test
  fun `lo que Unity manda como sin dato queda en null`() {
    val r = NativeReceiver.parse(json(""","con_hits":2,"con_opps":5,"con_groups":-1,"con_mem_groups":-1,"con_best_streak":-1,"con_useless":-1,"con_turns":-1,"con_group":-1,"con_best":-1,"con_new":0"""))!!
    assertNull(r.conGroups)
    assertNull(r.conBestStreak)
    assertNull(r.conGroup)
    assertNull(r.conBest)
    assertNull(r.conNewRecord)
  }

  @Test
  fun `otro juego no trae los campos de este`() {
    val r = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"freno","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":300,"level":3,"timed":false}}"""
    )!!
    assertNull(r.conGroup)
    assertNull(r.conBest)
  }
}
