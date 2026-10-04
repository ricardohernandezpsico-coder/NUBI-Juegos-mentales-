package com.example.bridge

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** El resultado de Tinta o Palabra («Dos orillas») tal como lo manda Unity: interferencia y costo de cambio en ms, -1 = sin datos suficientes. */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class StroopTelemetryTest {
  private fun json(game: String, extra: String) =
    """{"user_id":"u1","game_id":"$game","session_metrics":{"correct_trials":13,"total_trials":16,"calculated_score":81,"average_response_time_ms":1100,"level":3,"timed":false,"end_rating":0.5,"peak_level":4,"mode_trials":13,"mode_hits":11$extra}}"""

  @Test
  fun `lee la interferencia y el costo de cambio`() {
    val r = NativeReceiver.parse(json("stroop", ""","interference_ms":420,"switch_cost_ms":250"""))
    assertNotNull(r)
    r!!
    assertEquals("stroop", r.gameId)
    assertEquals(81, r.score)
    assertEquals(420, r.interferenceMs)
    assertEquals(250, r.switchCostMs)
  }

  @Test
  fun `menos uno es sin datos y queda en null, pero cero es un dato valido`() {
    val none = NativeReceiver.parse(json("stroop", ""","interference_ms":-1,"switch_cost_ms":-1"""))!!
    assertNull(none.interferenceMs)
    assertNull(none.switchCostMs)
    val zero = NativeReceiver.parse(json("stroop", ""","interference_ms":0,"switch_cost_ms":-1"""))!!
    assertEquals(0, zero.interferenceMs)
    assertNull(zero.switchCostMs)
  }

  @Test
  fun `una version vieja de Unity sin esos campos no los inventa`() {
    val r = NativeReceiver.parse(json("stroop", ""))!!
    assertNull(r.interferenceMs)
    assertNull(r.switchCostMs)
  }

  @Test
  fun `otros juegos con la telemetria comun no traen estas medidas`() {
    val r = NativeReceiver.parse(json("rutatesoro", ""","interference_ms":420,"switch_cost_ms":250"""))!!
    assertNull(r.interferenceMs)
    assertNull(r.switchCostMs)
  }
}
