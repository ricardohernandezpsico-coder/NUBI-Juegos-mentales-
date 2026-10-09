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

/** El final de «Piloto Estelar: la ruta de las balizas» (Tarea 61): «Tus señales a los mandos» con lo que pasó en el vuelo, y una partida con pocas señales de la misión (sin proporción). */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class PilotoResultScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private fun result(targets: Int, hits: Int, wrong: Int, signalPct: Int?, lane: Int) = GamePlayResult(
    gameId = "piloto", score = 74, correctAnswers = 41, totalTrials = 58, timed = true, level = 5,
    pilLanePct = lane, pilHits = hits, pilTargets = targets, pilFalse = wrong, pilSignalPct = signalPct, pilSignalLevel = 6,
    pilBestStreak = 9, pilPoints = 1250, pilHyper = 2, timestamp = 1_000_000L
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
  fun final_de_piloto_con_la_medida() {
    show(result(targets = 18, hits = 15, wrong = 3, signalPct = 67, lane = 82), "piloto-final")
  }

  @Test
  fun final_de_piloto_con_pocas_senales() {
    show(result(targets = 5, hits = 4, wrong = 0, signalPct = null, lane = 91), "piloto-final-pocas")
  }
}
