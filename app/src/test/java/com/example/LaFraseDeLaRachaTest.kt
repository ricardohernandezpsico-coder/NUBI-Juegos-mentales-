package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.dp
import com.example.data.Streak
import com.example.data.Week
import com.example.model.DomainType
import com.example.ui.screens.AvanceAreaGrid
import com.example.ui.screens.AvanceWeekLine
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
 * Desde la Tarea 159 Avance ni siquiera lleva la pastilla «Racha: N días» ni el globo de Nubi (la semana es UNA línea: las farolas y «2 de 4 días de tu meta»; la racha vive en Hoy y en Tú). Se prueba que Avance (la línea de la semana, las filas y la página del área) no habla de la racha ni del descanso, y que el código solo arma esas frases en `HomeScreen`
 * (Tú no las lleva: quedan las cifras).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [36])
class LaFraseDeLaRachaTest {
  @get:Rule val rule = createComposeRule()

  @Test
  fun avance_no_dice_nada_de_la_racha_ni_del_descanso() {
    val l = Week.Lamps(List(7) { it < 5 }, todayIndex = 3)
    val names = DomainType.values().associate { it.name to it.displayName }
    val areas = listOf(PlaceTestData.points("MEMORIA", 470, 12), PlaceTestData.points("ATENCION", 310, -8, best = 400), PlaceTestData.points("RAZONAMIENTO", 440, 0), PlaceTestData.points("LENGUAJE", 0, 0, measured = false))
    rule.setContent { NeuroVidaTheme { Box(Modifier.width(393.dp)) { Column { AvanceWeekLine(l, goal = 4, empty = false); AvanceAreaGrid(areas, names) {} } } } }
    rule.onNodeWithTag("avance_week_line").assertIsDisplayed()
    assertEquals("0 de 0: ni la pastilla de la racha", 0, rule.onAllNodes(androidx.compose.ui.test.hasTestTag("avance_streak_pill")).fetchSemanticsNodes().size)
    for (frase in listOf("Descansaste", "descanso", Streak.REST_LINE, "tu racha sigue", "racha", "Racha")) assertEquals("«$frase» no debe verse en Avance", 0, rule.onAllNodes(hasText(frase, substring = true)).fetchSemanticsNodes().size)
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
