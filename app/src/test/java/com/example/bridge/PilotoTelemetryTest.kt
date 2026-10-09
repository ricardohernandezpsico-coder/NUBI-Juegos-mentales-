package com.example.bridge

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * El resultado de «Piloto Estelar: la ruta de las balizas» tal como lo manda Unity (`pil_*`): todo medido con las dos tareas a la vez; ya no existe `multitask_cost` (regla permanente 1 de Ricardo, 9-oct).
 * -1 = sin dato (por ejemplo, la proporción de «tus señales a los mandos» con menos de 8 señales de la misión).
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class PilotoTelemetryTest {
  private fun json(extra: String) =
    """{"user_id":"u1","game_id":"piloto","session_metrics":{"correct_trials":41,"total_trials":58,"calculated_score":74,"average_response_time_ms":0,"level":3,"timed":true,"end_rating":0.55,"peak_level":7,"mode_trials":40,"mode_hits":33$extra}}"""

  @Test
  fun `lee la ruta, las senales, los toques equivocados y la medida`() {
    val r = NativeReceiver.parse(
      json(""","pil_lane_pct":82,"pil_hits":15,"pil_targets":18,"pil_false":3,"pil_signal_pct":67,"pil_signal_level":6,"pil_drive_level":5,"pil_best_streak":9,"pil_points":1250,"pil_hyper":2""")
    )
    assertNotNull(r)
    r!!
    assertEquals("piloto", r.gameId)
    assertEquals(74, r.score)
    assertEquals(82, r.pilLanePct)
    assertEquals(15, r.pilHits)
    assertEquals(18, r.pilTargets)
    assertEquals(3, r.pilFalse)
    assertEquals(67, r.pilSignalPct)
    assertEquals(6, r.pilSignalLevel)
    assertEquals(9, r.pilBestStreak)
    assertEquals(1250, r.pilPoints)
    assertEquals(2, r.pilHyper)
    // y lo de siempre se conserva
    assertEquals(0.55f, r.endRating!!, 1e-5f)
    assertEquals(40, r.modeTrials)
    assertEquals(33, r.modeHits)
  }

  @Test
  fun `con menos de ocho senales de la mision no hay proporcion pero si lo demas`() {
    val r = NativeReceiver.parse(json(""","pil_lane_pct":90,"pil_hits":4,"pil_targets":5,"pil_false":0,"pil_signal_pct":-1,"pil_signal_level":4"""))!!
    assertNull(r.pilSignalPct)
    assertEquals(90, r.pilLanePct)
    assertEquals(4, r.pilHits)
    assertEquals(0, r.pilFalse)          // cero toques equivocados es un dato valido
  }

  @Test
  fun `una partida sin los campos nuevos no inventa nada`() {
    val r = NativeReceiver.parse(json(""))!!
    assertNull(r.pilLanePct)
    assertNull(r.pilSignalPct)
    assertNull(r.pilSignalLevel)
    assertNull(r.pilPoints)
  }
}
