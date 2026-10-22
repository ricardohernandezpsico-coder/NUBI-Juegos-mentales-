package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.dp
import com.example.data.Streak
import com.example.data.Week
import com.example.data.nubiWeekLine
import com.example.data.weekSummary
import com.example.ui.screens.AvanceWeek
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File

/**
 * La frase de la racha vive SOLO en Hoy (Tarea 158, Opus, 21-oct): «Descansaste el domingo: tu racha sigue.» y «Un día de descanso por semana no la corta.» salían en Hoy (bajo las farolas), en Avance (el globo de Nubi y una línea suelta) y en Tú (bajo las cifras): la repetición que Ricardo quiere sacar.
 * Se prueba que el globo de Nubi de Avance no habla de la racha ni del descanso con ninguna semana posible, que «Tu semana» de Avance conserva la pastilla «Racha: N días» pero no la frase, y que el código solo arma esas frases en `HomeScreen` (Tú no las lleva: quedan las cifras).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [36])
class LaFraseDeLaRachaTest {
  @get:Rule val rule = createComposeRule()

  private fun lamps(days: Int) = Week.Lamps(List(7) { it < days }, todayIndex = 3)

  @Test
  fun el_globo_de_nubi_en_avance_no_dice_nada_de_la_racha_ni_del_descanso_con_ninguna_semana() {
    val lines = mutableSetOf<String>()
    for (days in 0..7) for (goal in 0..7) for (areas in 0..4) for (none in listOf(false, true)) lines += nubiWeekLine(lamps(days), goal, areas, none)
    assertTrue("hay frases", lines.isNotEmpty())
    for (l in lines) {
      val low = l.lowercase()
      assertFalse("«$l» empieza con «Descansaste»", l.startsWith("Descansaste"))
      assertFalse("«$l» habla de la racha", "racha" in low)
      assertFalse("«$l» habla del descanso", "descans" in low)
    }
  }

  @Test
  fun tu_semana_en_avance_conserva_la_pastilla_de_racha_pero_no_la_frase_del_descanso() {
    val l = lamps(5)
    val summary = weekSummary(l, goal = 4, streak = 7, areas = 4, noGamesAtAll = false)
    rule.setContent { NeuroVidaTheme { Box(Modifier.width(393.dp)) { AvanceWeek(summary, l, nubiWeekLine(l, 4, 4, noGamesAtAll = false)) } } }
    rule.onNodeWithTag("avance_streak_pill").assertIsDisplayed()
    assertEquals("la pastilla dice el número", "Racha: 7 días", summary.streakPill)
    for (frase in listOf("Descansaste", "descanso", Streak.REST_LINE, "tu racha sigue")) assertEquals("«$frase» no debe verse en Avance", 0, rule.onAllNodes(hasText(frase, substring = true)).fetchSemanticsNodes().size)
    assertEquals("ya no hay una línea suelta de la racha", 0, rule.onAllNodes(androidx.compose.ui.test.hasTestTag("streak_note")).fetchSemanticsNodes().size)
  }

  @Test
  fun la_frase_solo_se_arma_en_Hoy_y_Tu_no_la_lleva() {
    val usos = mutableListOf<String>()
    File("src/main/java").walkTopDown().filter { it.extension == "kt" }.forEach { f ->
      val rel = f.path.replace('\\', '/').removePrefix("src/main/java/com/example/")
      f.readLines().forEachIndexed { i, raw ->
        val line = raw.trim()
        if (line.startsWith("//") || line.startsWith("*") || line.startsWith("/*")) return@forEachIndexed
        if (line.contains("forgivenLine(") || line.contains("REST_LINE") || line.contains("Un día de descanso por semana")) usos += "$rel:${i + 1}"
      }
    }
    val permitidos = setOf("data/Streak.kt", "ui/screens/HomeScreen.kt")
    val fuera = usos.filter { u -> permitidos.none { u.startsWith("$it:") } }
    assertTrue("La frase de la racha se arma fuera de Hoy:\n" + fuera.joinToString("\n"), fuera.isEmpty())
    assertTrue("Hoy sí la arma", usos.any { it.startsWith("ui/screens/HomeScreen.kt:") })
  }
}
