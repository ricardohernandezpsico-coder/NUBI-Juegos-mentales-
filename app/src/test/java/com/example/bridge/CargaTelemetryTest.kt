package com.example.bridge

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** El resultado de «Carga exacta» tal como lo manda Unity (JsonUtility: -1 = sin dato). */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class CargaTelemetryTest {
  private fun json(extra: String, game: String = "calculo") =
    """{"user_id":"u1","game_id":"$game","session_metrics":{"correct_trials":7,"total_trials":8,"calculated_score":71,"average_response_time_ms":14200,"level":3,"timed":false,"end_rating":0.5,"peak_level":4,"mode_trials":6,"mode_hits":5,$extra}}"""

  @Test
  fun `lee las cargas sin pista, con pista, los caminos cortos y el tiempo`() {
    val r = NativeReceiver.parse(json(""""carga_alone":5,"carga_hinted":2,"carga_short":3,"carga_ms":14200"""))
    assertNotNull(r)
    r!!
    assertEquals("calculo", r.gameId)
    assertEquals(8, r.totalTrials)
    assertEquals(5, r.cargaAlone)
    assertEquals(2, r.cargaHinted)
    assertEquals(3, r.cargaShort)
    assertEquals(14200, r.cargaMs)
  }

  @Test
  fun `lo que Unity manda como sin dato queda en null y cero se conserva`() {
    val r = NativeReceiver.parse(json(""""carga_alone":0,"carga_hinted":0,"carga_short":0,"carga_ms":-1"""))!!
    assertEquals(0, r.cargaAlone)
    assertEquals(0, r.cargaShort)
    assertNull(r.cargaMs)
  }

  @Test
  fun `una version vieja sin los campos nuevos no rompe nada`() {
    val r = NativeReceiver.parse(json(""""campo_nuevo_desconocido":3"""))!!
    assertNull(r.cargaAlone)
    assertNull(r.cargaHinted)
  }

  @Test
  fun `otros juegos no traen los campos de este`() {
    val r = NativeReceiver.parse(json(""""carga_alone":5,"carga_ms":9000""", game = "rutatesoro"))!!
    assertNull(r.cargaAlone)
    assertNull(r.cargaMs)
  }
}
