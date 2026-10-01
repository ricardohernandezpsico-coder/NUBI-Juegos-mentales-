package com.example.bridge

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** El resultado de Lluvia de meteoros tal como lo manda Unity (JsonUtility: arreglos, -1 = sin dato, texto separado por comas). */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class MeteorosTelemetryTest {
  private fun json(extra: String) =
    """{"user_id":"u1","game_id":"meteoros","session_metrics":{"correct_trials":41,"total_trials":50,"calculated_score":78,"average_response_time_ms":2400,"level":6,"timed":true,"end_rating":0.5,"peak_level":7,"mode_trials":44,"mode_hits":36,$extra}}"""

  @Test
  fun `lee las medidas de vocabulario, reconocimiento, filtro y coleccion`() {
    val r = NativeReceiver.parse(
      json(
        """"lex_band_seen":[8,8,7,7,6,6],"lex_band_hits":[8,8,7,6,4,2],"lex_fa_seen":[6,8,7],"lex_fa_hits":[0,1,4],""" +
          """"lex_rt_common_ms":700,"lex_rt_rare_ms":1100,"lex_rare_words":"efímero,umbral,efímero,inefable""""
      )
    )
    assertNotNull(r)
    r!!
    assertEquals("meteoros", r.gameId)
    assertEquals(78, r.score)
    assertEquals(listOf(8, 8, 7, 7, 6, 6), r.lexBandSeen)
    assertEquals(listOf(8, 8, 7, 6, 4, 2), r.lexBandHits)
    assertEquals(listOf(6, 8, 7), r.lexFaSeen)
    assertEquals(listOf(0, 1, 4), r.lexFaHits)
    assertEquals(700, r.lexRtCommonMs)
    assertEquals(1100, r.lexRtRareMs)
    // sin repetir y en orden de hallazgo
    assertEquals(listOf("efímero", "umbral", "inefable"), r.lexRareWords)
  }

  @Test
  fun `lo que Unity manda como sin dato queda en null`() {
    val r = NativeReceiver.parse(
      json(""""lex_rare_words":"","lex_band_seen":[8,8,7,7,6,6],"lex_band_hits":[8,8,7,6,4,2],"lex_rt_common_ms":-1,"lex_rt_rare_ms":-1""")
    )!!
    assertNull(r.lexRtCommonMs)
    assertNull(r.lexRtRareMs)
    assertNull(r.lexRareWords)
    assertNull(r.lexFaSeen)
  }

  @Test
  fun `una lista con el largo equivocado no se toma`() {
    val r = NativeReceiver.parse(json(""""lex_band_seen":[1,2,3],"lex_band_hits":[1,2,3],"lex_fa_seen":[1,2],"lex_fa_hits":[0,1]"""))!!
    assertNull(r.lexBandSeen)
    assertNull(r.lexFaSeen)
  }

  @Test
  fun `otros juegos no traen los campos del vocabulario`() {
    val r = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"calculo","session_metrics":{"correct_trials":8,"total_trials":10,"calculated_score":70,"average_response_time_ms":1500,"level":3,"timed":true}}"""
    )!!
    assertNull(r.lexBandSeen)
    assertNull(r.lexRareWords)
  }
}
