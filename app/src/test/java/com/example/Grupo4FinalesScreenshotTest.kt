package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.hasScrollAction
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onFirst
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.test.swipeUp
import androidx.compose.ui.unit.dp
import com.example.data.MeasurePoint
import com.example.data.PuntaEntry
import com.example.data.PuntaTier
import com.example.games.GameResultScreen
import com.example.model.GamePlayResult
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assert.assertEquals
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * Los finales con sentido del grupo 4 de la Etapa 3 (Lenguaje; `docs/finales-con-sentido.md`): En la punta de la lengua, Lluvia de meteoros, ¿Verdad o disparate? y La estrella intrusa (Cosecha de palabras salió de la app el 10-oct, Tarea 80). Cada uno, en el orden de la plantilla: lo que hiciste
 * con UN dato tuyo, tu avance (hoy, tu promedio y tu mejor, con su frase), el truco, el botón «Ver el detalle de tu partida» (cerrado) y, abajo y en chico, «¿Por qué importa?». Capturas `*-final-sentido-*.png`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class Grupo4FinalesScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private fun points(key: String, vararg values: Float) = values.mapIndexed { i, v -> MeasurePoint(1_000L * (i + 1), key, v, 0.5f, false) }

  private fun base(id: String, block: GamePlayResult.() -> GamePlayResult) =
    GamePlayResult(gameId = id, score = 74, correctAnswers = 7, totalTrials = 10, timed = false, level = 5, endRating = 0.5f, timestamp = 1_000_000L).block()

  private val punta = base("anagramas") { copy(puntaSolo = 5, puntaPista = 2, puntaLetras = 0, puntaVista = 1, puntaMs = 6500, puntaWords = listOf(PuntaEntry("luna", PuntaTier.SOLO), PuntaEntry("barco", PuntaTier.PISTA), PuntaEntry("nube", PuntaTier.VISTA))) }
  private val meteoros = base("meteoros") { copy(lexBandSeen = listOf(8, 8, 8, 8, 8, 8), lexBandHits = listOf(8, 8, 7, 6, 5, 3), lexFaSeen = listOf(7, 7, 7), lexFaHits = listOf(0, 1, 2), lexRtCommonMs = 700, lexRtRareMs = 1100, lexRareWords = listOf("alcayata", "badil")) }
  private val disparate = base("disparate") { copy(svSeenType = listOf(8, 8, 8, 8, 8, 8), svRtType = listOf(900, 950, 1300, 1500, 1100, 1000), svWpm = 142, svSubtleHits = 6, svSubtleSeen = 8, svBestStreak = 14) }
  private val intrusa = base("intrusa") { copy(intrSeenType = listOf(3, 3, 2, 2, 2, 2), intrHitsType = listOf(3, 2, 2, 2, 1, 1), intrBonusSeen = 5, intrBonusHits = 3, intrRtMs = 1800) }

  private fun show(result: GamePlayResult, measures: List<MeasurePoint>) {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(873.dp).background(Color(0xFF050823))) {
          GameResultScreen(
            result = result, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
            starMeasures = measures, onPlayAgain = {}, onContinue = {}
          )
        }
      }
    }
    composeTestRule.assertUnitLineAtMostTwoLines(result, measures)
  }

  private fun capture(shot: String, pages: Int = 3) {
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/$shot-1.png")
    for (i in 2..pages) {
      composeTestRule.onAllNodes(hasScrollAction()).onFirst().performTouchInput { swipeUp(durationMillis = 100) }
      composeTestRule.waitForIdle()
      composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/$shot-$i.png")
    }
  }

  @Test
  fun final_de_punta_de_la_lengua_con_sentido() {
    show(punta, points("punta", 50f, 62f, 75f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("punta-final-sentido")
  }

  @Test
  fun final_de_meteoros_con_sentido() {
    show(meteoros, points("vocab", 40f, 50f, 60f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("meteoros-final-sentido")
  }

  @Test
  fun final_de_disparate_con_sentido() {
    show(disparate, points("wpm", 130f, 140f, 150f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("disparate-final-sentido")
  }

  @Test
  fun final_de_intrusa_con_sentido() {
    show(intrusa, points("atlas", 70f, 75f, 80f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("intrusa-final-sentido")
  }

  private fun assertNoRepeatedHits(result: GamePlayResult, measures: List<MeasurePoint>) {
    show(result, measures)
    assertEquals(result.gameId, 0, composeTestRule.onAllNodes(hasText("aciertos")).fetchSemanticsNodes().size)
    assertEquals(result.gameId, 1, composeTestRule.onAllNodes(hasText("etapa")).fetchSemanticsNodes().size)
  }

  @Test
  fun la_fila_comun_de_punta_ya_no_repite_los_aciertos() = assertNoRepeatedHits(punta, points("punta", 50f))

  @Test
  fun la_fila_comun_de_meteoros_ya_no_repite_los_aciertos() = assertNoRepeatedHits(meteoros, points("vocab", 50f))

  @Test
  fun la_fila_comun_de_disparate_ya_no_repite_los_aciertos() = assertNoRepeatedHits(disparate, points("wpm", 140f))

  @Test
  fun la_fila_comun_de_intrusa_ya_no_repite_los_aciertos() = assertNoRepeatedHits(intrusa, points("atlas", 70f))
}
