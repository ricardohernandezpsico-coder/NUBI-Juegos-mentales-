package com.example.bridge

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** El resultado de «Engranajes» tal como lo manda Unity (JsonUtility: -1 = sin dato) y la config que la app le manda con el cohete. */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class EngranajesTelemetryTest {
  private fun json(extra: String, game: String = "engranajes") =
    """{"user_id":"u1","game_id":"$game","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":7500,"level":3,"timed":false,"end_rating":0.5,"peak_level":4,"mode_trials":8,"mode_hits":6,$extra}}"""

  @Test
  fun `lee la etapa, las luces, los cohetes, los despegues y el ritmo`() {
    val r = NativeReceiver.parse(json(""""engr_etapa":3,"engr_lights":4,"engr_orbit":2,"engr_launches":1,"engr_ms":7500"""))
    assertNotNull(r)
    r!!
    assertEquals("engranajes", r.gameId)
    assertEquals(10, r.totalTrials)
    assertEquals(7, r.correctAnswers)
    assertEquals(3, r.engrEtapa)
    assertEquals(4, r.engrLights)
    assertEquals(2, r.engrOrbit)
    assertEquals(1, r.engrLaunches)
    assertEquals(7500, r.engrMs)
  }

  @Test
  fun `cero luces y cero cohetes se conservan y lo que Unity manda como sin dato queda en null`() {
    val r = NativeReceiver.parse(json(""""engr_etapa":1,"engr_lights":0,"engr_orbit":0,"engr_launches":0,"engr_ms":-1"""))!!
    assertEquals(0, r.engrLights)
    assertEquals(0, r.engrOrbit)
    assertEquals(0, r.engrLaunches)
    assertNull(r.engrMs)
  }

  @Test
  fun `una version vieja sin los campos nuevos no rompe nada`() {
    val r = NativeReceiver.parse(json(""""campo_nuevo_desconocido":3"""))!!
    assertNull(r.engrEtapa)
    assertNull(r.engrLights)
  }

  @Test
  fun `otros juegos no traen los campos de este`() {
    val r = NativeReceiver.parse(json(""""engr_etapa":3,"engr_lights":4,"engr_orbit":2""", game = "radar"))!!
    assertNull(r.engrEtapa)
    assertNull(r.engrLights)
  }

  @Test
  fun `el juego lleva tutorial guiado a quien nunca lo jugo`() {
    assertTrue("engranajes" in UnityGameLauncher.TUTORIAL_GAMES)
    assertTrue(UnityGameLauncher.shouldShowTutorial("engranajes", emptyList()))
  }
}
