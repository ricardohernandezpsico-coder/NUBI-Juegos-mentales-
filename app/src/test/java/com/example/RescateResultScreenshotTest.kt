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

/** El final de «Rescate relámpago» (Tarea 62): las cápsulas a salvo, «Tu captura: X de 4», «Tu vistazo», las rondas perfectas y el récord; y una partida sin lluvias (sin captura). */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class RescateResultScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private fun result(rescued: Int, capture: Float?) = GamePlayResult(
    gameId = "radar", score = 71, correctAnswers = 9, totalTrials = 12, timed = false, level = 6,
    glanceMs = 280, glanceLoad = 3.5f, captureK = capture,
    rescRescued = rescued, rescPerfect = 5, rescRounds = 12, rescBestStreak = 3, rescShortestMs = 150, rescBest = rescued, rescNewRecord = rescued > 0, timestamp = 1_000_000L
  )

  private fun show(result: GamePlayResult, shot: String) {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          GameResultScreen(
            result = result, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
            starMeasures = emptyList(), onPlayAgain = {}, onContinue = {}
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
  fun final_de_rescate_con_captura() {
    show(result(rescued = 31, capture = 3.25f), "rescate-final")
  }

  @Test
  fun final_de_rescate_sin_lluvias() {
    show(result(rescued = 6, capture = null), "rescate-final-sin-captura")
  }
}
