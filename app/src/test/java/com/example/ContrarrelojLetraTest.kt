package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsNode
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Comparison
import com.example.data.EstimatedReference
import com.example.data.NBSP
import com.example.data.TimeTrialResult
import com.example.model.AgeRange
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.ContrarrelojPage
import com.example.ui.screens.PlusLockSheetContent
import com.example.ui.screens.ContrarrelojDoor
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File
import java.util.Calendar

/**
 * La letra grande y las palabras que no se parten (Tarea 205c; auditoría de Fable de la T205): con letra 2,0 a 360 dp, «Te faltan 12 aciertos» pegado con espacios duros medía ~300 dp en un renglón de ~246 y Android lo cortaba por letras («acier / tos»; se vio en el emulador, no solo
 * en la prueba). Desde la 205c los nombres de división y «Te faltan N aciertos» solo se pegan con letra de 1,3 o menos (`glueNames(fontScale)`), y la cifra con su unidad («12 aciertos») siempre. Estas pruebas miran el RENGLÓN que Android armó de verdad (`TextLayoutResult`): ningún renglón termina dentro de una palabra,
 * a 393 y 360 dp con letra 1,0, 1,3 y 2,0 y con «Te faltan» de una y de dos cifras; y la hoja del candado (sin su `Dialog`, que trae su propia densidad) con las tres letras.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class ContrarrelojLetraTest {
  @get:Rule val rule = createComposeRule()

  @Before fun setUp() { TestSupport.fastFrames() }

  private val estimated: Comparison.Source by lazy {
    val dir = File("src/main/assets/${EstimatedReference.ASSET_DIR}")
    Comparison.ComposedSource(null, EstimatedReference.of(dir.listFiles { f -> f.extension == "json" }.orEmpty().sortedBy { it.name }.map { it.readText(Charsets.UTF_8) }))
  }

  private fun at(day: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, day, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(mark: Int, day: Int = 27, game: String = "stroop") =
    TimeTrialResult(id = "r-$game-$day-$mark", gameId = game, day = "2026-10-%02d".format(day), trackVersion = 2, hits = mark + 2, errors = 2, level = 4, seconds = 120, timestamp = at(day))
  private val now = at(28, 18)

  /** Los lugares donde un renglón terminó DENTRO de una palabra: entre dos letras (ni espacio ni salto), que es como Android corta lo que no tiene dónde partir. Los espacios duros no cuentan como espacio: pegan. */
  private fun cutsIn(node: SemanticsNode, out: MutableList<String>) {
    val results = mutableListOf<TextLayoutResult>()
    node.config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(results)
    val layout = results.firstOrNull()
    if (layout != null) {
      val text = layout.layoutInput.text.text
      for (line in 0 until layout.lineCount - 1) {
        val end = layout.getLineEnd(line)
        if (end in 1 until text.length) {
          val before = text[end - 1]; val after = text[end]
          if (before != ' ' && before != '\n' && after != ' ' && after != '\n') {
            val tag = node.config.getOrNull(SemanticsProperties.TestTag) ?: "?"
            out += "«${text.substring(maxOf(0, end - 14), end)}|${text.substring(end, minOf(text.length, end + 14))}» en $tag".replace(NBSP, "·")
          }
        }
      }
    }
    node.children.forEach { cutsIn(it, out) }
  }

  private fun cutsOnScreen(): List<String> = mutableListOf<String>().also { cutsIn(rule.onRoot(useUnmergedTree = true).fetchSemanticsNode(), it) }

  private fun textOf(tag: String): String =
    rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Text)?.joinToString("") { it.text }.orEmpty().replace(NBSP, " ")

  // ---------------------------------------------------------------- la puerta de Avance (16 sp en negrita; 20 dp de margen a cada lado en Avance)

  @Test
  fun la_puerta_no_parte_ninguna_palabra_por_dentro_con_letra_1_0_1_3_y_2_0_a_393_y_360_con_Te_faltan_de_una_y_de_dos_cifras() {
    var width by mutableIntStateOf(393)
    var scale by mutableFloatStateOf(1f)
    var line by mutableStateOf("")
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(width.dp).padding(horizontal = 20.dp)) { ContrarrelojDoor(record = 12, plus = false, onClick = {}, freeLine = line) } }
      }
    }
    fun free(game: String, mark: Int) = Comparison.freeSentence(Comparison.place(estimated, game, AgeRange.R45_54, mark))!!
    val lines = listOf(free("radar", 1) to 12, free("radar", 9) to 4, free("stroop", 36) to 15, free("stroop", 46) to 5, free("stroop", 58) to 5, free("stroop", 53) to 4)
    assertTrue("las frases traen su «Te faltan N»: ${lines.map { it.first }}", lines.all { (s, n) -> "Te faltan $n aciertos" in s || "Te falta $n aciertos" in s })
    val problems = mutableListOf<String>()
    var checked = 0
    for ((sentence, _) in lines) for (w in listOf(393, 360)) for (s in listOf(1f, 1.3f, 2f)) {
      width = w; scale = s; line = sentence
      rule.waitForIdle()
      cutsOnScreen().forEach { problems += "a $w dp con letra $s, «$sentence»: $it" }
      checked += 1
    }
    assertEquals("se miró cada combinación", lines.size * 6, checked)
    assertTrue("palabras cortadas por dentro:\n" + problems.joinToString("\n"), problems.isEmpty())
  }

  // ---------------------------------------------------------------- la página con la referencia (la escalera y todo lo demás)

  @Test
  fun la_pagina_con_la_referencia_no_parte_ninguna_palabra_por_dentro_con_letra_1_0_1_3_y_2_0_a_393_y_360() {
    var width by mutableIntStateOf(393)
    var scale by mutableFloatStateOf(1f)
    var results by mutableStateOf(listOf<TimeTrialResult>())
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(width.dp).height(873.dp)) {
            BoardBackground()
            ContrarrelojPage(results = results, selected = "stroop", onSelect = {}, onBack = {}, now = now, lang = com.example.model.AppLanguage.SPANISH, compare = estimated, ageRange = AgeRange.R45_54)
          }
        }
      }
    }
    val problems = mutableListOf<String>()
    var checked = 0
    for (mark in listOf(36, 46, 58, 70)) for (w in listOf(393, 360)) for (s in listOf(1f, 1.3f, 2f)) {
      results = listOf(r(mark)); width = w; scale = s
      rule.waitForIdle()
      if (mark < 63) assertTrue("con la marca $mark la escalera dice «Te faltan N aciertos»: ${textOf("contrarreloj_ladder_need")}", "Te falta" in textOf("contrarreloj_ladder_need"))
      cutsOnScreen().forEach { problems += "marca $mark a $w dp con letra $s: $it" }
      checked += 1
    }
    assertEquals(4 * 6, checked)
    assertTrue("palabras cortadas por dentro:\n" + problems.joinToString("\n"), problems.isEmpty())
  }

  // ---------------------------------------------------------------- la hoja del candado, sin su Dialog

  @Test
  fun la_hoja_del_candado_con_letra_1_0_1_3_y_2_0_se_lee_entera_y_Cerrar_se_alcanza_sin_partir_palabras() {
    var scale by mutableFloatStateOf(1f)
    var closed by mutableIntStateOf(0)
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(873.dp).padding(vertical = 24.dp)) {     // el alto de una ventana de diálogo en un teléfono de 873 dp: la hoja puede crecer hasta ahí y de ahí en adelante se desliza
            PlusLockSheetContent(onClose = { closed += 1 }, kind = Comparison.Kind.ESTIMATED)
          }
        }
      }
    }
    val report = mutableListOf<String>()
    for (s in listOf(1f, 1.3f, 2f)) {
      scale = s
      rule.waitForIdle()
      assertEquals("el título", com.example.data.PlusAccess.SHEET_TITLE, textOf("plus_sheet_title"))
      assertEquals("el encabezado", com.example.data.PlusAccess.sheetLead(Comparison.Kind.ESTIMATED), textOf("plus_sheet_lead"))
      val cuts = cutsOnScreen()
      assertTrue("palabras cortadas por dentro con letra $s:\n" + cuts.joinToString("\n"), cuts.isEmpty())
      val visibleWithoutScrolling = runCatching { rule.onNodeWithTag("btn_plus_sheet_ok", useUnmergedTree = true).assertIsDisplayed() }.isSuccess
      rule.onNodeWithTag("btn_plus_sheet_ok", useUnmergedTree = true).performScrollTo().assertIsDisplayed()          // si la hoja no cabe, se desliza hasta el botón
      report += "letra $s: «Cerrar» " + if (visibleWithoutScrolling) "a la vista sin deslizar" else "hay que deslizar (la hoja se desliza)"
    }
    println("T205c hoja del candado a 393 × 873 dp, sin Dialog: " + report.joinToString("; "))
    rule.onNodeWithTag("btn_plus_sheet_ok", useUnmergedTree = true).performClick()
    rule.waitForIdle()
    assertEquals("«Cerrar» cierra la hoja", 1, closed)
  }
}
