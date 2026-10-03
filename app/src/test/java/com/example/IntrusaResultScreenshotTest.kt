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
import com.example.data.AtlasState
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

/** El final de La estrella intrusa con datos de ejemplo: tu red de significados, las trampas, ¿qué las une? y tu atlas (sin recuadro). */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class IntrusaResultScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  @Test
  fun final_de_la_estrella_intrusa() {
    val today = java.time.LocalDate.now().toEpochDay().toInt()
    val result = GamePlayResult(
      gameId = "intrusa", score = 72, correctAnswers = 14, totalTrials = 19, timed = true, level = 6,
      // por tipo: amplia 4/4, vecina 3/4, uso 2/3 (la más baja con ≥ 3 rondas), material/lugar/parte 2/2 (solo 2: no se muestra), trampas 3/3 + 2/3... → 5 de 6
      intrSeenType = listOf(4, 4, 3, 2, 4, 2), intrHitsType = listOf(4, 3, 2, 2, 2, 1),
      intrRtMs = 1800, intrBestStreak = 6, intrBonusSeen = 5, intrBonusHits = 3,
      intrNewPlates = listOf("fruta", "pez", "vuela")
    )
    val atlas = AtlasState(
      plates = (listOf("animal", "mamífero", "ave", "insecto", "reptil", "verdura", "bebida", "herramienta", "mueble", "prenda", "calzado") +
        listOf("fruta", "pez", "vuela")).associateWith { 100 }.mapValues { (k, _) -> if (k in listOf("fruta", "pez", "vuela")) today else today - 5 },
      review = setOf("sirve para cortar")
    )
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          GameResultScreen(
            result = result, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
            onPlayAgain = {}, onContinue = {}, atlas = atlas
          )
        }
      }
    }
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/intrusa-final.png")
    composeTestRule.onAllNodes(hasScrollAction()).onFirst().performTouchInput { swipeUp(durationMillis = 100) }
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/intrusa-final-2.png")
  }
}
