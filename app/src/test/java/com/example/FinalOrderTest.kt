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
 * El orden de la pantalla de resultado (`docs/finales-con-sentido.md`): los 18 juegos usan el final con sentido, que tiene UN solo orden (lo que hiciste, tu avance, el truco, ¿por qué importa?)
 * y trae su truco dentro. Desde el 10-oct ya no hay ajuste «Lo que avancé / Un consejo para la próxima» que lo mueva ni bloque aparte de consejo: el truco sale una sola vez, dentro del final.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class FinalOrderTest {
  @get:Rule val composeTestRule = createComposeRule()

  // En la punta de la lengua: la medida («Tu cielo de palabras») y un truco. Aterrizaje Lunar y los otros 16 también usan el final con sentido.
  private val punta = GamePlayResult(
    gameId = "anagramas", score = 74, correctAnswers = 6, totalTrials = 8, timed = false, level = 3,
    puntaSolo = 5, puntaPista = 2, puntaLetras = 0, puntaVista = 1
  )

  private fun show(result: GamePlayResult = punta) {
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          GameResultScreen(result = result, didLevelUp = false, isDailyFlow = false, dailyCompletedCount = 0, onPlayAgain = {}, onContinue = {})
        }
      }
    }
    composeTestRule.mainClock.advanceTimeBy(300)
  }

  private fun yOfTag(tag: String) = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
  private fun countTag(tag: String) = composeTestRule.onAllNodesWithTag(tag).fetchSemanticsNodes().size
  private fun countText(text: String) = composeTestRule.onAllNodesWithText(text, substring = true).fetchSemanticsNodes().size

  @Test
  fun el_final_va_en_su_orden_lo_que_hiciste_tu_avance_y_el_truco() {
    show()
    val box = yOfTag("meaningful_box")
    val chips = yOfTag("progress_chips")
    val trick = yOfTag("next_trick")
    assertTrue("lo que hiciste va antes de tu avance", box < chips)
    assertTrue("tu avance va antes del truco", chips < trick)
    assertEquals("la medida no se oculta", 1, countText("Tu cielo de palabras"))
  }

  @Test
  fun el_truco_sale_una_sola_vez_dentro_del_final_y_no_hay_bloque_aparte_de_consejo() {
    show()
    assertEquals("el título del bloque viejo ya no sale", 0, countText("Un consejo para la próxima"))
    assertEquals("el truco sale una sola vez, dentro del final", 1, countTag("next_trick"))
    assertEquals(1, countText("Truco para la próxima"))
  }

  @Test
  fun un_juego_sin_truco_condicional_tambien_trae_su_truco_general_una_sola_vez() {
    val brake = GamePlayResult(gameId = "freno", score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 2, brakeMs = 245, stopsOk = 6, stopsTotal = 8)
    show(brake)
    assertEquals(0, countText("Un consejo para la próxima"))
    assertEquals(1, countTag("next_trick"))
  }
}
