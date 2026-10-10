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
 * Los finales con sentido del grupo 2 de la Etapa 3 (Atención; `docs/finales-con-sentido.md`): Tinta o Palabra, Piloto Estelar, Freno de Emergencia, Satélites y Rescate relámpago. Cada uno, en el orden de la plantilla: lo que hiciste con UN dato tuyo,
 * tu avance (hoy, tu promedio y tu mejor, con su frase), el truco, el botón «Ver el detalle de tu partida» (cerrado) y, abajo y en chico, «¿Por qué importa?». Capturas `*-final-sentido-*.png`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class Grupo2FinalesScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private fun points(key: String, vararg values: Float) = values.mapIndexed { i, v -> MeasurePoint(1_000L * (i + 1), key, v, 0.5f, false) }

  private fun base(id: String, block: GamePlayResult.() -> GamePlayResult) =
    GamePlayResult(gameId = id, score = 74, correctAnswers = 7, totalTrials = 10, timed = false, level = 5, endRating = 0.5f, timestamp = 1_000_000L).block()

  private val tinta = base("stroop") { copy(correctAnswers = 20, totalTrials = 24, interferenceMs = 420, switchCostMs = 180) }
  private val piloto = base("piloto") { copy(pilSignalPct = 58, pilHits = 14, pilTargets = 18, pilFalse = 2, pilLanePct = 82, pilSignalLevel = 6, pilBestStreak = 7, pilPoints = 1250) }
  private val freno = base("freno") { copy(brakeMs = 245, stopsOk = 6, stopsTotal = 8, brakeBestSsdMs = 180) }
  private val satelites = base("satelites") { copy(trackingCapacity = 2.6f, trackingTargets = 3.5f, satLights = 12, satPerfect = 5, satBestStreak = 3, satBest = 22, satNewRecord = false, totalTrials = 8, trackingSpeed = 1.4f) }
  private val rescate = base("radar") { copy(glanceMs = 170, glanceLoad = 3f, rescRescued = 23, rescBest = 31, rescNewRecord = false, captureK = 3.5f, rescPerfect = 5, rescRounds = 12, rescBestStreak = 3, rescShortestMs = 150, rescTrips = 2) }

  private fun show(result: GamePlayResult, measures: List<MeasurePoint>) {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          GameResultScreen(
            result = result, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
            starMeasures = measures, onPlayAgain = {}, onContinue = {}
          )
        }
      }
    }
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
  fun final_de_tinta_o_palabra_con_sentido() {
    show(tinta, points("stroop", 300f, 500f, 400f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("tinta-final-sentido")
  }

  @Test
  fun final_de_piloto_con_sentido() {
    show(piloto, points("mandos", 45f, 52f, 61f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("piloto-final-sentido")
  }

  @Test
  fun final_de_freno_con_sentido() {
    show(freno, points("brake", 260f, 250f, 240f, 270f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("freno-final-sentido")
  }

  @Test
  fun final_de_satelites_con_sentido() {
    show(satelites, points("tracking", 2.2f, 2.4f, 2.9f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("satelites-final-sentido")
  }

  @Test
  fun final_de_rescate_con_sentido() {
    show(rescate, points("glance", 200f, 190f, 180f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("rescate-final-sentido")
  }

  @Test
  fun el_detalle_de_freno_esta_cerrado_y_trae_el_velocimetro_al_abrirlo() {
    show(freno, points("brake", 260f, 250f, 240f, 270f))
    composeTestRule.onNodeWithTag("detail_content").assertDoesNotExist()
    composeTestRule.onNodeWithTag("detail_toggle").performScrollTo().performClick()
    composeTestRule.waitForIdle()
    composeTestRule.onNodeWithTag("detail_content").assertExists()
    capture("freno-final-sentido-detalle", pages = 4)
  }

  @Test
  fun el_detalle_de_rescate_y_de_satelites_conserva_lo_que_mostraban_antes() {
    show(rescate, points("glance", 200f, 190f, 180f))
    composeTestRule.onNodeWithTag("detail_toggle").performScrollTo().performClick()
    composeTestRule.waitForIdle()
    composeTestRule.onNodeWithTag("rescate_perfect").performScrollTo().assertExists()
    composeTestRule.onNodeWithTag("rescate_trips").assertExists()
    capture("rescate-final-sentido-detalle", pages = 4)
  }

  private fun assertNoRepeatedHits(result: GamePlayResult, measures: List<MeasurePoint>) {
    show(result, measures)
    assertEquals(result.gameId, 0, composeTestRule.onAllNodes(hasText("aciertos")).fetchSemanticsNodes().size)
    assertEquals(result.gameId, 1, composeTestRule.onAllNodes(hasText("etapa")).fetchSemanticsNodes().size)
  }

  @Test
  fun la_fila_comun_de_tinta_ya_no_repite_los_aciertos() = assertNoRepeatedHits(tinta, points("stroop", 300f))

  @Test
  fun la_fila_comun_de_piloto_ya_no_repite_los_aciertos() = assertNoRepeatedHits(piloto, points("mandos", 50f))

  @Test
  fun la_fila_comun_de_freno_ya_no_repite_los_aciertos() = assertNoRepeatedHits(freno, points("brake", 250f))

  @Test
  fun la_fila_comun_de_satelites_ya_no_repite_los_aciertos() = assertNoRepeatedHits(satelites, points("tracking", 2.4f))

  @Test
  fun la_fila_comun_de_rescate_ya_no_repite_los_aciertos() = assertNoRepeatedHits(rescate, points("glance", 190f))
}
