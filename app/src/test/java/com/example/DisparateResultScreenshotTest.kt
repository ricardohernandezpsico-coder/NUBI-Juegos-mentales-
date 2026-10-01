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

/** El final de ¿Verdad o disparate? con datos de ejemplo (cifra grande de palabras por minuto, qué te frena, precisión y racha). */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class DisparateResultScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  @Test
  fun final_de_disparate() {
    val result = GamePlayResult(
      gameId = "disparate", score = 86, correctAnswers = 27, totalTrials = 30, timed = true, level = 8,
      svWpm = 142,
      svRtType = listOf(900, 1600, 1900, 1400, 1300, -1).let { listOf(it[0], it[3], it[2], it[1], -1, it[4]) },
      svHitsType = listOf(5, 5, 5, 4, 4, 4),
      svSeenType = listOf(5, 5, 5, 5, 5, 5),
      svEvidentHits = 9, svEvidentSeen = 10, svSubtleHits = 6, svSubtleSeen = 8,
      svBestStreak = 14
    )
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          GameResultScreen(
            result = result, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
            onPlayAgain = {}, onContinue = {}
          )
        }
      }
    }
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/disparate-final.png")
    composeTestRule.onAllNodes(hasScrollAction()).onFirst().performTouchInput { swipeUp(durationMillis = 100) }
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/disparate-final-2.png")
  }
}
