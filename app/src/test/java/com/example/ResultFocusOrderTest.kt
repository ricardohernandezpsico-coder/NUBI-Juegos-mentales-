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

  // En la punta de la lengua: la medida («Tu cielo de palabras») y un consejo (alguna palabra necesitó ayuda o la mostró Nubi). Aterrizaje Lunar ya no usa este bloque: su truco viene en su propio final (`MeaningfulResult`).
  private val punta = GamePlayResult(
    gameId = "anagramas", score = 74, correctAnswers = 6, totalTrials = 8, timed = false, level = 3,
    puntaSolo = 5, puntaPista = 2, puntaLetras = 0, puntaVista = 1
  )

  private fun show(focus: ResultFocus, result: GamePlayResult = punta) {
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
    assertTrue("«Tu cielo de palabras» (la medida) va antes del consejo", yOfText("Tu cielo de palabras") < yOfTag("result_advice"))
  }

  @Test
  fun consejo_primero_pone_el_consejo_antes_de_la_medida() {
    show(ResultFocus.CONSEJO)
    assertTrue("el consejo va antes de «Tu cielo de palabras»", yOfTag("result_advice") < yOfText("Tu cielo de palabras"))
  }

  private fun checkOnce(focus: ResultFocus) {
    show(focus)
    assertEquals("el consejo debe salir una sola vez ($focus)", 1, countText("Si una no sale, piensa en cómo empieza"))
    assertEquals("la medida no se oculta ($focus)", 1, countText("Tu cielo de palabras"))
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
