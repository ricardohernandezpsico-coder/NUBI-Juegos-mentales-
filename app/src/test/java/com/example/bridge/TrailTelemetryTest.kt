package com.example.bridge

import com.example.data.Trail
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * El resultado de Rastro de luz (id «secuencia») tal como lo manda Unity: la telemetría de siempre (`end_rating`, `peak_level`, `mode_trials`,
 * `mode_hits`) más las listas `ras_*` por familia; y cuándo la app manda el tutorial guiado.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class TrailTelemetryTest {
  private fun json(extra: String) =
    """{"user_id":"u1","game_id":"secuencia","session_metrics":{"correct_rounds":9,"total_rounds":12,"calculated_score":72,"average_response_time_ms":640.0,"final_span_length":6,"level":3,"timed":true,"peak_level":9,"end_rating":0.41,"mode_trials":10,"mode_hits":8$extra}}"""

  @Test
  fun `lee el mejor largo, las rondas, los aciertos y los modos`() {
    val r = NativeReceiver.parse(
      json(""","ras_best_len":[5,3,0,2],"ras_rounds":[5,3,3,1],"ras_hits":[5,2,1,1],"ras_modes_seen":15,"ras_new_modes":6""")
    )
    assertNotNull(r)
    r!!
    assertEquals("secuencia", r.gameId)
    assertEquals(72, r.score)
    assertEquals(listOf(5, 3, 0, 2), r.rasBestLen)
    assertEquals(listOf(5, 3, 3, 1), r.rasRounds)
    assertEquals(listOf(5, 2, 1, 1), r.rasHits)
    assertEquals(15, r.rasModesSeen)
    assertEquals(6, r.rasNewModes)
    // y lo de siempre se conserva
    assertEquals(0.41f, r.endRating!!, 1e-5f)
    assertEquals(10, r.modeTrials)
    assertEquals(8, r.modeHits)
  }

  @Test
  fun `lee las confusiones de modo sin cambiar la medida`() {
    val r = NativeReceiver.parse(
      json(""","ras_best_len":[5,3,0,2],"ras_rounds":[5,3,3,1],"ras_hits":[5,2,1,1],"ras_modes_seen":15,"ras_new_modes":0,"ras_mode_confusions":[0,1,0,1]""")
    )!!
    assertEquals(listOf(0, 1, 0, 1), r.rasModeConfusions)
    assertEquals(listOf(5, 3, 3, 1), r.rasRounds)
    assertNull(NativeReceiver.parse(json(""","ras_mode_confusions":[1,2]"""))!!.rasModeConfusions)
    assertNull(NativeReceiver.parse(json(""))!!.rasModeConfusions)
  }

  @Test
  fun `la medida final sale de la telemetria y marca el modo que mas costo`() {
    val r = NativeReceiver.parse(
      json(""","ras_best_len":[5,3,2,2],"ras_rounds":[5,3,4,3],"ras_hits":[5,3,1,2],"ras_modes_seen":15,"ras_new_modes":0""")
    )!!
    assertEquals(5, Trail.bestTrail(r.rasBestLen))
    val rows = Trail.modeRows(r.rasRounds, r.rasHits)
    assertEquals(listOf("Al revés", "El cielo gira", "En marcha"), rows.map { it.label })
    assertEquals("El cielo gira", rows.single { it.lowest }.label)
    assertNull(Trail.unlockedLine(r.rasNewModes))
  }

  @Test
  fun `una partida de una version vieja de Unity no trae la medida`() {
    val r = NativeReceiver.parse(json(""))!!
    assertNull(r.rasBestLen)
    assertNull(r.rasRounds)
    assertNull(r.rasHits)
    assertNull(r.rasModesSeen)
    assertNull(r.rasNewModes)
    assertEquals(0.41f, r.endRating!!, 1e-5f)
  }

  @Test
  fun `una lista con otro largo no se toma y -1 es sin dato`() {
    val r = NativeReceiver.parse(json(""","ras_best_len":[5,3],"ras_rounds":[1,2,3,4,5],"ras_hits":[1,1,1,1],"ras_modes_seen":-1,"ras_new_modes":-1"""))!!
    assertNull(r.rasBestLen)
    assertNull(r.rasRounds)
    assertEquals(listOf(1, 1, 1, 1), r.rasHits)
    assertNull(r.rasModesSeen)
    assertNull(r.rasNewModes)
  }

  @Test
  fun `otros juegos no traen los campos de Rastro de luz`() {
    val r = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"parejas","session_metrics":{"matched_pairs":30,"attempts":41,"calculated_score":74,"level":3,"timed":true,"ras_best_len":[5,3,0,2]}}"""
    )!!
    assertNull(r.rasBestLen)
    assertNull(r.rasRounds)
  }

  // ---- el tutorial guiado: solo si nunca jugó este juego

  private fun played(gameId: String) = com.example.model.GamePlayResult(gameId = gameId, score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 1)

  @Test
  fun `el tutorial se manda solo a quien nunca jugo Rastro de luz`() {
    assertTrue(UnityGameLauncher.shouldShowTutorial("secuencia", emptyList()))
    assertTrue(UnityGameLauncher.shouldShowTutorial("secuencia", listOf(played("parejas"), played("stroop"))))      // jugó otros, no este
    assertFalse(UnityGameLauncher.shouldShowTutorial("secuencia", listOf(played("parejas"), played("secuencia"))))
  }

  @Test
  fun `los juegos sin tutorial guiado no lo piden aunque no tengan historial`() {
    assertFalse(UnityGameLauncher.shouldShowTutorial("radar", emptyList()))
    assertFalse(UnityGameLauncher.shouldShowTutorial("satelites", emptyList()))
    assertEquals(setOf("secuencia", "freno", "aterrizaje", "meteoros", "stroop", "anagramas", "calculo", "engranajes", "bodega", "parejas", "correo", "cosecha", "disparate", "intrusa"), UnityGameLauncher.TUTORIAL_GAMES)
  }

  @Test
  fun `los seis juegos con tutorial lo piden solo a quien nunca los jugo`() {
    for (id in listOf("secuencia", "freno", "aterrizaje", "meteoros", "stroop", "anagramas")) {
      assertTrue(id, UnityGameLauncher.shouldShowTutorial(id, emptyList()))
      assertTrue(id, UnityGameLauncher.shouldShowTutorial(id, listOf(played("parejas"))))
      assertFalse(id, UnityGameLauncher.shouldShowTutorial(id, listOf(played(id))))
    }
  }
}
