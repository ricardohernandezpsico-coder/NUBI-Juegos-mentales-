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
import com.example.data.Acoplamiento
import com.example.data.MeasurePoint
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
 * Los finales con sentido del grupo 3 de la Etapa 3 (Razonamiento; `docs/finales-con-sentido.md`): Carga exacta, Engranajes y Acoplamiento. Cada uno, en el orden de la plantilla: lo que hiciste con UN dato tuyo, tu avance (hoy, tu promedio y tu mejor,
 * con su frase), el truco, el botón «Ver el detalle de tu partida» (cerrado) y, abajo y en chico, «¿Por qué importa?». Capturas `*-final-sentido-*.png`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class Grupo3FinalesScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private fun points(key: String, vararg values: Float) = values.mapIndexed { i, v -> MeasurePoint(1_000L * (i + 1), key, v, 0.5f, false) }

  private fun base(id: String, block: GamePlayResult.() -> GamePlayResult) =
    GamePlayResult(gameId = id, score = 74, correctAnswers = 7, totalTrials = 10, timed = false, level = 5, endRating = 0.5f, timestamp = 1_000_000L).block()

  private val carga = base("calculo") { copy(correctAnswers = 5, totalTrials = 8, level = 4, cargaAlone = 5, cargaHinted = 2, cargaShort = 3, cargaMs = 14_000) }
  private val engranajes = base("engranajes") { copy(correctAnswers = 7, totalTrials = 10, engrEtapa = 3, engrLaunches = 1, engrOrbit = 3, engrLights = 0, engrMs = 7500) }
  private val acoplamiento = base("acoplamiento") { copy(rotationSpeedDps = 212, rotationCurveMs = listOf(900, 1100, 1400, 1750, 2100), dockDocked = 24, dockRings = 3, dockBest = 31, dockNewRecord = false) }

  private fun show(result: GamePlayResult, measures: List<MeasurePoint>) {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(873.dp).background(Color(0xFF050823))) {
          GameResultScreen(
            result = result, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
            starMeasures = measures, acoplamientoTotals = Acoplamiento.Totals(342, 41), onPlayAgain = {}, onContinue = {}
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
  fun final_de_carga_exacta_con_sentido() {
    show(carga, points("carga", 50f, 75f, 62.5f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("carga-final-sentido")
  }

  @Test
  fun final_de_engranajes_con_sentido() {
    show(engranajes, points("taller", 50f, 60f, 80f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("engranajes-final-sentido")
  }

  @Test
  fun final_de_acoplamiento_con_sentido() {
    show(acoplamiento, points("rotation", 180f, 200f, 260f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("acoplamiento-final-sentido")
  }

  @Test
  fun el_detalle_de_acoplamiento_conserva_la_curva_de_giro_y_lo_acoplado_en_toda_la_vida() {
    show(acoplamiento, points("rotation", 180f, 200f, 260f))
    composeTestRule.onNodeWithTag("detail_content").assertDoesNotExist()
    composeTestRule.onNodeWithTag("detail_toggle").performScrollTo().performClick()
    composeTestRule.waitForIdle()
    composeTestRule.onNodeWithTag("acoplamiento_total").performScrollTo().assertExists()
    composeTestRule.onNodeWithTag("acoplamiento_rings").assertExists()
    capture("acoplamiento-final-sentido-detalle", pages = 4)
  }

  private fun assertNoRepeatedHits(result: GamePlayResult, measures: List<MeasurePoint>) {
    show(result, measures)
    assertEquals(result.gameId, 0, composeTestRule.onAllNodes(hasText("aciertos")).fetchSemanticsNodes().size)
    assertEquals(result.gameId, 1, composeTestRule.onAllNodes(hasText("etapa")).fetchSemanticsNodes().size)
  }

  @Test
  fun la_fila_comun_de_carga_ya_no_repite_los_aciertos() = assertNoRepeatedHits(carga, points("carga", 50f))

  @Test
  fun la_fila_comun_de_engranajes_ya_no_repite_los_aciertos() = assertNoRepeatedHits(engranajes, points("taller", 50f))

  @Test
  fun la_fila_comun_de_acoplamiento_ya_no_repite_los_aciertos() = assertNoRepeatedHits(acoplamiento, points("rotation", 200f))
}
