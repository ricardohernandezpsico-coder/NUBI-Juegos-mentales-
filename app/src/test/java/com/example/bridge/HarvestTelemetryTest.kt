package com.example.bridge

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** El resultado de Cosecha de palabras tal como lo manda Unity (JsonUtility: -1 o "" = sin dato, palabras separadas por comas). */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class HarvestTelemetryTest {
  private fun json(extra: String) =
    """{"user_id":"u1","game_id":"cosecha","session_metrics":{"correct_trials":2,"total_trials":3,"calculated_score":74,"average_response_time_ms":0,"level":5,"timed":true,"end_rating":0.5,"peak_level":6,"mode_trials":3,"mode_hits":2,$extra}}"""

  @Test
  fun `lee las palabras, las comunes, los racimos, el ritmo y la estrella`() {
    val r = NativeReceiver.parse(
      json(
        """"harv_words":34,"harv_common_found":21,"harv_common_total":48,"harv_cluster_pct":60,"harv_first20":13,"harv_last20":6,""" +
          """"harv_star":"asociar","harv_best":"asociar","harv_missed":"saciar,acosar,rosca,ácaros,orcas,saciar","harv_hints":2"""
      )
    )
    assertNotNull(r)
    r!!
    assertEquals("cosecha", r.gameId)
    assertEquals(74, r.score)
    assertEquals(34, r.harvWords)
    assertEquals(21, r.harvCommonFound)
    assertEquals(48, r.harvCommonTotal)
    assertEquals(60, r.harvClusterPct)
    assertEquals(13, r.harvFirst20)
    assertEquals(6, r.harvLast20)
    assertEquals("asociar", r.harvStar)
    assertEquals("asociar", r.harvBest)
    assertEquals(listOf("saciar", "acosar", "rosca", "ácaros", "orcas"), r.harvMissed)      // sin repetir
    assertEquals(2, r.harvHints)
  }

  @Test
  fun `lo que Unity manda como sin dato queda en null`() {
    val r = NativeReceiver.parse(
      json(""""harv_words":4,"harv_common_found":2,"harv_common_total":0,"harv_cluster_pct":-1,"harv_star":"","harv_best":"casa","harv_missed":"","harv_hints":0""")
    )!!
    assertEquals(4, r.harvWords)
    assertNull(r.harvCommonTotal)
    assertNull(r.harvClusterPct)
    assertNull(r.harvStar)
    assertEquals("casa", r.harvBest)
    assertNull(r.harvMissed)
    assertEquals(0, r.harvHints)
  }

  @Test
  fun `el porcentaje de racimos fuera de rango no se toma`() {
    val r = NativeReceiver.parse(json(""""harv_words":12,"harv_cluster_pct":140"""))!!
    assertNull(r.harvClusterPct)
  }

  @Test
  fun `otros juegos no traen los campos de la cosecha`() {
    val r = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"disparate","session_metrics":{"correct_trials":8,"total_trials":10,"calculated_score":70,"average_response_time_ms":1500,"level":3,"timed":true,"harv_words":5}}"""
    )!!
    assertNull(r.harvWords)
    assertNull(r.harvStar)
    assertNull(r.harvMissed)
  }
}
