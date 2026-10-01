package com.example.bridge

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** El resultado de ¿Verdad o disparate? tal como lo manda Unity (JsonUtility: arreglos, -1 = sin dato, ids separados por comas). */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class DisparateTelemetryTest {
  private fun json(extra: String) =
    """{"user_id":"u1","game_id":"disparate","session_metrics":{"correct_trials":27,"total_trials":30,"calculated_score":86,"average_response_time_ms":1500,"level":8,"timed":true,"end_rating":0.5,"peak_level":9,"mode_trials":28,"mode_hits":24,$extra}}"""

  @Test
  fun `lee las palabras por minuto, los tipos, los disparates y la racha`() {
    val r = NativeReceiver.parse(
      json(
        """"sv_wpm":142,"sv_rt_type":[900,1000,1800,1500,-1,1300],"sv_hits_type":[5,5,5,4,4,4],"sv_seen_type":[5,5,5,5,5,5],""" +
          """"sv_evident_hits":9,"sv_evident_seen":10,"sv_subtle_hits":6,"sv_subtle_seen":8,"sv_best_streak":14,"sv_unclear":"a1b2c3d4,0f9e8d7c,a1b2c3d4""""
      )
    )
    assertNotNull(r)
    r!!
    assertEquals("disparate", r.gameId)
    assertEquals(86, r.score)
    assertEquals(142, r.svWpm)
    assertEquals(listOf(900, 1000, 1800, 1500, -1, 1300), r.svRtType)
    assertEquals(listOf(5, 5, 5, 4, 4, 4), r.svHitsType)
    assertEquals(listOf(5, 5, 5, 5, 5, 5), r.svSeenType)
    assertEquals(9, r.svEvidentHits)
    assertEquals(10, r.svEvidentSeen)
    assertEquals(6, r.svSubtleHits)
    assertEquals(8, r.svSubtleSeen)
    assertEquals(14, r.svBestStreak)
    assertEquals(listOf("a1b2c3d4", "0f9e8d7c"), r.svUnclear)          // sin repetir
  }

  @Test
  fun `lo que Unity manda como sin dato queda en null`() {
    val r = NativeReceiver.parse(json(""""sv_wpm":-1,"sv_unclear":"","sv_seen_type":[3,3,3,3,3,3],"sv_hits_type":[2,2,2,2,2,2]"""))!!
    assertNull(r.svWpm)
    assertNull(r.svUnclear)
    assertNull(r.svRtType)
    assertEquals(listOf(3, 3, 3, 3, 3, 3), r.svSeenType)
  }

  @Test
  fun `una lista con el largo equivocado no se toma`() {
    val r = NativeReceiver.parse(json(""""sv_seen_type":[1,2,3],"sv_hits_type":[1,2],"sv_rt_type":[1]"""))!!
    assertNull(r.svSeenType)
    assertNull(r.svHitsType)
    assertNull(r.svRtType)
  }

  @Test
  fun `otros juegos no traen los campos de la lectura`() {
    val r = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"calculo","session_metrics":{"correct_trials":8,"total_trials":10,"calculated_score":70,"average_response_time_ms":1500,"level":3,"timed":true}}"""
    )!!
    assertNull(r.svWpm)
    assertNull(r.svSeenType)
    assertNull(r.svUnclear)
  }
}
