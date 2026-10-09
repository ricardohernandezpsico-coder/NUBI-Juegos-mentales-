package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.hasScrollAction
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onFirst
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.test.swipeUp
import androidx.compose.ui.unit.dp
import com.example.data.MeasurePoint
import com.example.games.GameResultScreen
import com.example.model.GamePlayResult
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/** El final de Freno de Emergencia con «Tu freno» por zonas y como promedio (Tarea 57): con un historial de partidas (promedio de las últimas 5) y como primera lectura. */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class BrakeResultScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private fun result(brakeMs: Int?) = GamePlayResult(
    gameId = "freno", score = 82, correctAnswers = 30, totalTrials = 36, timed = true, level = 4,
    brakeMs = brakeMs, stopsOk = 5, stopsTotal = 10, brakeBestSsdMs = 400, timestamp = 1_000_000L
  )

  private fun show(result: GamePlayResult, history: List<MeasurePoint>, shot: String) {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          GameResultScreen(
            result = result, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
            starMeasures = history, onPlayAgain = {}, onContinue = {}
          )
        }
      }
    }
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/$shot.png")
    composeTestRule.onAllNodes(hasScrollAction()).onFirst().performTouchInput { swipeUp(durationMillis = 100) }
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/$shot-2.png")
  }

  @Test
  fun final_de_freno_con_promedio_de_varias_partidas() {
    val history = listOf(MeasurePoint(100L, "brake", 275f), MeasurePoint(200L, "brake", 262f), MeasurePoint(300L, "brake", 281f), MeasurePoint(400L, "brake", 255f))
    show(result(brakeMs = 270), history, "freno-final")
  }

  @Test
  fun final_de_freno_primera_lectura_agil() {
    show(result(brakeMs = 205), emptyList(), "freno-final-primera")
  }
}
