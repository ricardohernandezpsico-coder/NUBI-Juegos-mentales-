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

/** El final de «Acoplamiento» (Tarea 68): los módulos acoplados, los anillos, el récord y el total, y después «Tu giro mental» y «Tu curva de giro». */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class AcoplamientoResultScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private fun result(docked: Int, rings: Int, speed: Int?) = GamePlayResult(
    gameId = "acoplamiento", score = 71, correctAnswers = docked, totalTrials = docked + 4, timed = true, level = 6,
    rotationSpeedDps = speed, rotationCurveMs = if (speed != null) listOf(900, 1100, 1400, 1750, 2100) else null,
    dockDocked = docked, dockRings = rings, dockBest = docked, dockNewRecord = docked > 0, timestamp = 1_000_000L
  )

  private fun show(result: GamePlayResult, shot: String, totals: com.example.data.Acoplamiento.Totals? = null) {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          GameResultScreen(
            result = result, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
            starMeasures = emptyList(), acoplamientoTotals = totals, onPlayAgain = {}, onContinue = {}
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
  fun final_de_acoplamiento_con_medidas() {
    show(result(docked = 17, rings = 2, speed = 212), "acoplamiento-final", com.example.data.Acoplamiento.Totals(342, 41))
  }

  @Test
  fun final_de_acoplamiento_sin_medida_de_giro() {
    show(result(docked = 5, rings = 0, speed = null), "acoplamiento-final-sin-giro", com.example.data.Acoplamiento.Totals(5, 0))
  }
}
