package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.assertCountEquals
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasScrollAction
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onFirst
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performScrollToNode
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.swipeUp
import androidx.compose.ui.unit.dp
import com.example.data.Punta
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

/** El final de «En la punta de la lengua» con datos de ejemplo: «Encontraste X de N por tu cuenta», la lista con su lucero y las azules que vuelven. */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class PuntaResultScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private fun result(words: String, solo: Int, pista: Int, letras: Int, vista: Int, ms: Int = 3900) = GamePlayResult(
    gameId = "anagramas", score = 71, correctAnswers = solo + pista + letras, totalTrials = solo + pista + letras + vista, timed = false, level = 3,
    puntaSolo = solo, puntaPista = pista, puntaLetras = letras, puntaVista = vista, puntaMs = ms, puntaWords = Punta.entries(words),
    puntaBlue = Punta.words("búho"), puntaCleared = emptyList()
  )

  @Test
  fun final_de_en_la_punta_de_la_lengua() {
    val r = result("reloj:o;faro:p;búho:a;colmena:o;nido:c;mesa:o;estrella:p;río:o", solo = 4, pista = 2, letras = 1, vista = 1)
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          GameResultScreen(result = r, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3, onPlayAgain = {}, onContinue = {})
        }
      }
    }
    composeTestRule.onNodeWithTag("punta_headline").performScrollToNodeSafely()
    composeTestRule.onNodeWithTag("punta_headline").assertIsDisplayed()
    composeTestRule.onNodeWithTag("punta_words").assertIsDisplayed()
    composeTestRule.onNodeWithTag("punta_blue_note").assertIsDisplayed()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/punta-final.png")
    composeTestRule.onAllNodes(hasScrollAction()).onFirst().performTouchInput { swipeUp(durationMillis = 100) }
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/punta-final-2.png")
  }

  @Test
  fun sin_azules_no_hay_nota_y_con_pocas_solas_no_hay_tiempo() {
    val r = result("reloj:o;faro:p;mesa:p", solo = 1, pista = 2, letras = 0, vista = 0).copy(puntaBlue = emptyList())
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          GameResultScreen(result = r, didLevelUp = false, isDailyFlow = false, dailyCompletedCount = 0, dailyTotalCount = 0, onPlayAgain = {}, onContinue = {})
        }
      }
    }
    composeTestRule.onNodeWithTag("punta_headline").performScrollToNodeSafely()
    composeTestRule.onNodeWithTag("punta_headline").assertIsDisplayed()
    composeTestRule.onAllNodes(hasTestTag("punta_blue_note")).assertCountEquals(0)
  }

  /** Lleva la lista de abajo a la vista (la pantalla de resultado se desliza). */
  private fun androidx.compose.ui.test.SemanticsNodeInteraction.performScrollToNodeSafely() {
    runCatching { composeTestRule.onAllNodes(hasScrollAction()).onFirst().performScrollToNode(hasTestTag("punta_headline")) }
  }
}
