package com.example.bridge

import com.example.data.ratingFromSequencePeak
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * Secuencia Lumínica y Parejas Ocultas ya están en el DDA común (3-oct): su resultado trae `end_rating` como el de
 * los demás juegos. Las versiones viejas de Unity no lo mandaban: Secuencia caía al nivel más alto alcanzado y
 * Parejas quedaba sin rating.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class MemoryTelemetryTest {
  private fun sequence(extra: String) =
    """{"user_id":"u1","game_id":"secuencia","session_metrics":{"correct_rounds":9,"total_rounds":12,"calculated_score":72,"average_response_time_ms":640.0,"final_span_length":6,"level":3,"timed":false$extra}}"""

  private fun cards(extra: String) =
    """{"user_id":"u1","game_id":"parejas","session_metrics":{"matched_pairs":30,"attempts":41,"calculated_score":74,"level":3,"timed":true$extra}}"""

  @Test
  fun `Secuencia usa el end_rating del motor comun`() {
    val r = NativeReceiver.parse(sequence(""","peak_level":9,"end_rating":0.41,"mode_trials":10,"mode_hits":8"""))
    assertNotNull(r)
    r!!
    assertEquals("secuencia", r.gameId)
    assertEquals(72, r.score)
    assertEquals(9, r.correctAnswers)
    assertEquals(12, r.totalTrials)
    assertEquals(0.41f, r.endRating!!, 1e-5f)       // no el del nivel más alto
    assertEquals(10, r.modeTrials)
    assertEquals(8, r.modeHits)
  }

  @Test
  fun `Secuencia sin end_rating (Unity viejo) cae al nivel mas alto alcanzado`() {
    val r = NativeReceiver.parse(sequence(""","peak_level":9"""))!!
    assertEquals(ratingFromSequencePeak(9), r.endRating!!, 1e-5f)
    assertEquals(0, r.modeTrials)
  }

  @Test
  fun `Secuencia sin end_rating ni nivel no inventa un rating`() {
    assertNull(NativeReceiver.parse(sequence(""))!!.endRating)
  }

  @Test
  fun `un end_rating de cero se respeta y no cae al nivel mas alto`() {
    val r = NativeReceiver.parse(sequence(""","peak_level":9,"end_rating":0.0"""))!!
    assertEquals(0f, r.endRating!!, 1e-6f)
  }

  @Test
  fun `Parejas lee el end_rating y los ensayos del modo`() {
    val r = NativeReceiver.parse(cards(""","end_rating":0.62,"peak_level":7,"mode_trials":39,"mode_hits":30"""))
    assertNotNull(r)
    r!!
    assertEquals("parejas", r.gameId)
    assertEquals(30, r.correctAnswers)
    assertEquals(41, r.totalTrials)
    assertEquals(0.62f, r.endRating!!, 1e-5f)
    assertEquals(39, r.modeTrials)
    assertEquals(30, r.modeHits)
  }

  @Test
  fun `Parejas sin end_rating (Unity viejo) no trae rating`() {
    val r = NativeReceiver.parse(cards(""))!!
    assertNull(r.endRating)
    assertEquals(0, r.modeTrials)
  }
}
