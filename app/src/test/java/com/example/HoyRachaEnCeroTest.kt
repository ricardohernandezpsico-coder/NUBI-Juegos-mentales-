package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.assertTextEquals
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.NBSP
import com.example.data.Streak
import com.example.data.Week
import com.example.ui.components.BoardBackground
import com.example.ui.components.HoyWeek
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File

/**
 * La racha en cero en Hoy (Tarea 186, revisión 5a; Opus, 28-oct): ya no dice «0 días» con la llama apagada y la regla del descanso debajo. Con una mejor racha de antes dice «Tu racha empieza hoy» y, debajo, «Tu mejor racha: N días.»; sin ninguna mejor, «Tu racha empieza con tu próxima partida»
 * y ninguna línea; con racha, todo como siempre («N días» y la línea del descanso o el día perdonado).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class HoyRachaEnCeroTest {
  @get:Rule val rule = createComposeRule()

  private val lamps = Week.Lamps(List(7) { it < 2 }, todayIndex = 3)
  private val today = 1000L
  private fun days(vararg offsets: Int): Set<Long> = offsets.map { today + it }.toSet()

  /** Lo que arma `HomeScreen` para la semana: el texto junto a la llama y la línea de debajo, a partir de la racha y la mejor. */
  private fun show(played: Set<Long>, best: Int, fontScale: Float = 1f) {
    val info = Streak.compute(played, today)
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale)) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp)) {
            BoardBackground()
            HoyWeek(lamps, info.current, Streak.noteFor(info, best), streakText = Streak.counterText(info.current, best))
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun count(tag: String) = rule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().size

  @Test
  fun con_la_racha_en_cero_y_una_mejor_de_antes_dice_que_empieza_hoy_y_cual_fue_la_mejor() {
    show(days(-5, -6, -7), best = 12)
    rule.onNodeWithTag("streak_pill").assertIsDisplayed().assertTextEquals("Tu racha empieza hoy")
    rule.onNodeWithTag("streak_note").assertIsDisplayed().assertTextEquals("Tu mejor racha: 12${NBSP}días.")
    assertEquals("nunca «0 días»", 0, rule.onAllNodes(hasText("0 días", substring = true)).fetchSemanticsNodes().size)
    assertEquals("la regla del descanso solo va cuando hay una racha que explicar", 0, rule.onAllNodes(hasText("descanso", substring = true)).fetchSemanticsNodes().size)
  }

  @Test
  fun sin_ninguna_mejor_dice_que_empieza_con_la_proxima_partida_y_no_lleva_linea_debajo() {
    show(emptySet(), best = 0)
    rule.onNodeWithTag("streak_pill").assertIsDisplayed().assertTextEquals("Tu racha empieza con tu próxima partida")
    assertEquals("sin línea debajo", 0, count("streak_note"))
    assertEquals(0, rule.onAllNodes(hasText("0 días", substring = true)).fetchSemanticsNodes().size)
  }

  @Test
  fun con_racha_todo_sigue_como_siempre() {
    show(days(0, -1, -2), best = 0)
    rule.onNodeWithTag("streak_pill").assertTextEquals("3 días")
    rule.onNodeWithTag("streak_note").assertTextEquals(Streak.REST_LINE)
  }

  @Test
  fun con_un_dia_perdonado_dice_el_dia_que_descanso() {
    show(days(0, -1, -3), best = 0)
    rule.onNodeWithTag("streak_pill").assertTextEquals("3 días")
    rule.onNodeWithTag("streak_note").assertTextEquals("Descansaste el ${Streak.weekdayName(today - 2)}: tu racha sigue.")
  }

  private fun checkFit(fontScale: Float) {
    show(emptySet(), best = 0, fontScale = fontScale)
    val pill = rule.onNodeWithTag("streak_pill").fetchSemanticsNode().boundsInRoot
    assertTrue("la frase más larga se sale del ancho con letra $fontScale: $pill", pill.left >= 0f && pill.right <= 393f * rule.density.density + 1f)
  }

  @Test fun la_frase_mas_larga_cabe_en_el_ancho_con_letra_1_3() = checkFit(1.3f)

  @Test fun la_frase_mas_larga_cabe_en_el_ancho_con_letra_2_0() = checkFit(2f)

  @Test
  fun Hoy_arma_el_texto_y_la_linea_con_la_racha_y_la_mejor_y_se_los_pasa_a_la_semana() {
    val src = File("src/main/java/com/example/ui/screens/HomeScreen.kt").readText()
    assertTrue("Hoy calcula el texto y la línea con la mejor racha", src.contains("Streak.counterText(streakInfo.current, bestStreak)") && src.contains("Streak.noteFor(streakInfo, bestStreak)"))
    assertTrue("y se los pasa a la semana", src.contains("streakText = ui.streakText"))
  }
}
