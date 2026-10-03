package com.example.bridge

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** El resultado de La estrella intrusa tal como lo manda Unity (JsonUtility: -1 o "" = sin dato, reglas separadas por «;»). */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class IntrusaTelemetryTest {
  private fun json(extra: String) =
    """{"user_id":"u1","game_id":"intrusa","session_metrics":{"correct_trials":7,"total_trials":9,"calculated_score":71,"average_response_time_ms":1800,"level":5,"timed":true,"end_rating":0.5,"peak_level":6,"mode_trials":9,"mode_hits":7,$extra}}"""

  @Test
  fun `lee las rondas por tipo, la rapidez, el bonus y las reglas del atlas`() {
    val r = NativeReceiver.parse(
      json(
        """"intr_seen_type":[2,2,2,1,1,1],"intr_hits_type":[2,2,1,1,1,0],"intr_rt_ms":1800,"intr_best_streak":4,"intr_bonus_seen":5,"intr_bonus_hits":3,""" +
          """"intr_new_plates":"fruta;sirve para cortar","intr_review_new":"pez","intr_review_done":"vuela;vuela","intr_named":"fruta""""
      )
    )
    assertNotNull(r)
    r!!
    assertEquals("intrusa", r.gameId)
    assertEquals(71, r.score)
    assertEquals(listOf(2, 2, 2, 1, 1, 1), r.intrSeenType)
    assertEquals(listOf(2, 2, 1, 1, 1, 0), r.intrHitsType)
    assertEquals(1800, r.intrRtMs)
    assertEquals(4, r.intrBestStreak)
    assertEquals(5, r.intrBonusSeen)
    assertEquals(3, r.intrBonusHits)
    assertEquals(listOf("fruta", "sirve para cortar"), r.intrNewPlates)
    assertEquals(listOf("pez"), r.intrReviewNew)
    assertEquals(listOf("vuela"), r.intrReviewDone)             // sin repetir
    assertEquals(listOf("fruta"), r.intrNamed)
  }

  @Test
  fun `lo que Unity manda como sin dato queda en null o vacio`() {
    val r = NativeReceiver.parse(
      json(""""intr_seen_type":[1,1,1,1,1,1],"intr_hits_type":[1,1,1,1,1,1],"intr_rt_ms":-1,"intr_best_streak":-1,"intr_bonus_seen":-1,"intr_bonus_hits":-1""")
    )!!
    assertNull(r.intrRtMs)
    assertNull(r.intrBestStreak)
    assertNull(r.intrBonusSeen)
    assertEquals(emptyList<String>(), r.intrNewPlates)
  }

  @Test
  fun `una lista de tipos con otro largo no se toma`() {
    val r = NativeReceiver.parse(json(""""intr_seen_type":[1,2,3],"intr_hits_type":[1,2,3]"""))!!
    assertNull(r.intrSeenType)
    assertNull(r.intrHitsType)
  }

  @Test
  fun `otros juegos no traen los campos de la estrella intrusa`() {
    val r = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"cosecha","session_metrics":{"correct_trials":8,"total_trials":10,"calculated_score":70,"average_response_time_ms":1500,"level":3,"timed":true,"intr_rt_ms":900,"intr_new_plates":"fruta"}}"""
    )!!
    assertNull(r.intrRtMs)
    assertNull(r.intrNewPlates)
    assertNull(r.intrSeenType)
  }
}
