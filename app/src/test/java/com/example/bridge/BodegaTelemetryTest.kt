package com.example.bridge

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** El resultado de «Bodega de carga» tal como lo manda Unity (JsonUtility: -1 = sin dato): objetos al primer intento y encontrados, el grupo de etapa, la racha, la bodega más grande, el récord y el ritmo. */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class BodegaTelemetryTest {
  private fun json(extra: String, game: String = "bodega") =
    """{"user_id":"u1","game_id":"$game","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":4500,"level":3,"timed":false,"end_rating":0.5,"peak_level":4,"mode_trials":8,"mode_hits":6,$extra}}"""

  @Test
  fun `lee el grupo, la racha, la bodega mas grande, el record y el ritmo`() {
    val r = NativeReceiver.parse(json(""""bod_group":3,"bod_best_streak":5,"bod_biggest":4,"bod_best":6,"bod_ms":4500,"bod_new":1"""))
    assertNotNull(r)
    r!!
    assertEquals("bodega", r.gameId)
    assertEquals(10, r.totalTrials)
    assertEquals(7, r.correctAnswers)
    assertEquals(3, r.bodGroup)
    assertEquals(5, r.bodBestStreak)
    assertEquals(4, r.bodBiggest)
    assertEquals(6, r.bodBest)
    assertEquals(4500, r.bodMs)
    assertEquals(true, r.bodNewRecord)
  }

  @Test
  fun `cero en la racha y en la bodega mas grande se conserva y lo que Unity manda como sin dato queda en null`() {
    val r = NativeReceiver.parse(json(""""bod_group":1,"bod_best_streak":0,"bod_biggest":0,"bod_best":0,"bod_ms":-1,"bod_new":0"""))!!
    assertEquals(0, r.bodBestStreak)
    assertEquals(0, r.bodBiggest)
    assertEquals(0, r.bodBest)
    assertNull(r.bodMs)
    assertEquals(false, r.bodNewRecord)
  }

  @Test
  fun `una version vieja sin los campos nuevos no rompe nada`() {
    val r = NativeReceiver.parse(json(""""campo_nuevo_desconocido":3"""))!!
    assertNull(r.bodGroup)
    assertNull(r.bodBest)
    assertNull(r.bodNewRecord)
  }

  @Test
  fun `otros juegos no traen los campos de este`() {
    val r = NativeReceiver.parse(json(""""bod_group":3,"bod_best":6,"bod_new":1""", game = "radar"))!!
    assertNull(r.bodGroup)
    assertNull(r.bodBest)
    assertNull(r.bodNewRecord)
  }

  @Test
  fun `el juego lleva tutorial guiado a quien nunca lo jugo`() {
    assertTrue("bodega" in UnityGameLauncher.TUTORIAL_GAMES)
    assertTrue(UnityGameLauncher.shouldShowTutorial("bodega", emptyList()))
  }

  @Test
  fun `Bitacora de Mision sigue retirada y Bodega ocupa su lugar en Memoria`() {
    assertTrue(com.example.model.GameRegistry.isRetired("bitacora"))
    assertFalse(com.example.model.GameRegistry.isRetired("bodega"))
    assertEquals("MEMORIA", com.example.model.GameRegistry.getById("bodega")!!.domain.name)
    assertEquals("Bodega de carga", com.example.model.GameRegistry.getById("bodega")!!.title)
  }
}
