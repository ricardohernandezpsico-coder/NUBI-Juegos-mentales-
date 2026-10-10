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

/**
 * `result_focus` desde que los 18 juegos usan el final con sentido (`docs/finales-con-sentido.md`): el final tiene UN solo orden (lo que hiciste, tu avance, el truco, ¿por qué importa?) y el truco viene dentro de él,
 * así que el ajuste «Lo que avancé / Un consejo para la próxima» ya no cambia lo que se ve: ninguna de las dos opciones saca el bloque viejo «Un consejo para la próxima» ni mueve nada.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class ResultFocusOrderTest {
  @get:Rule val composeTestRule = createComposeRule()

  // En la punta de la lengua: la medida («Tu cielo de palabras») y un truco. Aterrizaje Lunar y los otros 16 también usan el final con sentido.
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
  private fun countTag(tag: String) = composeTestRule.onAllNodesWithTag(tag).fetchSemanticsNodes().size
  private fun countText(text: String) = composeTestRule.onAllNodesWithText(text, substring = true).fetchSemanticsNodes().size

  private fun checkOrder(focus: ResultFocus) {
    show(focus)
    val box = yOfTag("meaningful_box")
    val chips = yOfTag("progress_chips")
    val trick = yOfTag("next_trick")
    assertTrue("lo que hiciste va antes de tu avance ($focus)", box < chips)
    assertTrue("tu avance va antes del truco ($focus)", chips < trick)
    assertEquals("la medida no se oculta ($focus)", 1, countText("Tu cielo de palabras"))
  }

  @Test
  fun avance_el_final_va_en_su_orden_de_siempre() = checkOrder(ResultFocus.AVANCE)

  @Test
  fun consejo_el_final_va_en_su_orden_de_siempre() = checkOrder(ResultFocus.CONSEJO)

  private fun checkNoOldAdvice(focus: ResultFocus) {
    show(focus)
    assertEquals("el bloque viejo de consejo ya no sale ($focus)", 0, countTag("result_advice"))
    assertEquals("el título viejo ya no sale ($focus)", 0, countText("Un consejo para la próxima"))
    assertEquals("el truco sale una sola vez, dentro del final ($focus)", 1, countTag("next_trick"))
  }

  @Test
  fun avance_el_consejo_viejo_ya_no_sale_y_el_truco_va_una_sola_vez() = checkNoOldAdvice(ResultFocus.AVANCE)

  @Test
  fun consejo_el_consejo_viejo_ya_no_sale_y_el_truco_va_una_sola_vez() = checkNoOldAdvice(ResultFocus.CONSEJO)

  @Test
  fun un_juego_sin_consejo_se_ve_igual_con_las_dos_opciones() {
    val brake = GamePlayResult(gameId = "freno", score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 2)
    show(ResultFocus.CONSEJO, brake)
    assertEquals(0, countTag("result_advice"))
  }
}
