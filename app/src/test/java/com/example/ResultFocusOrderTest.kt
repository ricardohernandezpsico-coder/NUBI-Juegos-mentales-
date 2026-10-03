package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithTag
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.unit.dp
import com.example.data.ResultFocus
import com.example.games.GameResultScreen
import com.example.model.GamePlayResult
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/** `result_focus`: en la pantalla de resultado el bloque que va primero cambia (avance / consejo) y nada se repite ni se oculta. */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class ResultFocusOrderTest {
  @get:Rule val composeTestRule = createComposeRule()

  private val landing = GamePlayResult(
    gameId = "aterrizaje", score = 74, correctAnswers = 6, totalTrials = 8, timed = false, level = 3, numlineErrorPct = 8f,
    numlineTrue = listOf(0.1f, 0.2f, 0.5f, 0.5f, 0.9f, 0.9f), numlineGiven = listOf(0.3f, 0.4f, 0.5f, 0.52f, 0.9f, 0.91f)
  )

  private fun show(focus: ResultFocus, result: GamePlayResult = landing) {
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          GameResultScreen(
            result = result, didLevelUp = false, isDailyFlow = false, dailyCompletedCount = 0,
            onPlayAgain = {}, onContinue = {}, resultFocus = focus
          )
        }
      }
    }
    composeTestRule.mainClock.advanceTimeBy(300)
  }

  private fun yOfTag(tag: String) = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
  private fun yOfText(text: String) = composeTestRule.onNodeWithText(text, substring = true).fetchSemanticsNode().positionInRoot.y
  private fun countText(text: String) = composeTestRule.onAllNodesWithText(text, substring = true).fetchSemanticsNodes().size

  @Test
  fun avance_primero_pone_la_medida_antes_del_consejo() {
    show(ResultFocus.AVANCE)
    assertTrue("«Tu línea» (la medida) va antes del consejo", yOfText("Tu línea") < yOfTag("result_advice"))
  }

  @Test
  fun consejo_primero_pone_el_consejo_antes_de_la_medida() {
    show(ResultFocus.CONSEJO)
    assertTrue("el consejo va antes de «Tu línea»", yOfTag("result_advice") < yOfText("Tu línea"))
  }

  private fun checkOnce(focus: ResultFocus) {
    show(focus)
    assertEquals("el consejo debe salir una sola vez ($focus)", 1, countText("Mide desde el 0"))
    assertEquals("la medida no se oculta ($focus)", 1, countText("Donde más te alejas del blanco: el primer tercio de la regla."))
    assertEquals(1, composeTestRule.onAllNodesWithTag("result_advice").fetchSemanticsNodes().size)
  }

  @Test
  fun avance_el_consejo_sale_una_sola_vez_y_la_medida_no_lo_repite() = checkOnce(ResultFocus.AVANCE)

  @Test
  fun consejo_el_consejo_sale_una_sola_vez_y_la_medida_no_lo_repite() = checkOnce(ResultFocus.CONSEJO)

  @Test
  fun un_juego_sin_consejo_se_ve_igual_con_las_dos_opciones() {
    val brake = GamePlayResult(gameId = "freno", score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 2)
    show(ResultFocus.CONSEJO, brake)
    assertEquals(0, composeTestRule.onAllNodesWithTag("result_advice").fetchSemanticsNodes().size)
  }
}
