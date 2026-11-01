package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.SemanticsMatcher
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Comparison
import com.example.data.NBSP
import com.example.data.PlusAccess
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
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

/**
 * La puerta de Avance sin Plus con la frase gratis nueva (con el lugar de la división) y el candado largo (Tarea 209): con letra 1,0 / 1,3 / 2,0 a 393 y a 360 dp ninguna palabra se parte, nada se sale del margen de la losa y se MIDE cuánto crece
 * (`println`, que la Tarea 209 anota en `docs/contrarreloj.md` §18). Una sola puerta por prueba (un solo `setContent`); el ancho y la letra se cambian después.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class ContrarrelojPuertaLetraTest {
  @get:Rule val rule = createComposeRule()

  private var widthState by mutableStateOf(393)
  private var fontState by mutableStateOf(1f)
  private val density get() = rule.density.density

  /** La frase gratis más larga que puede decir la puerta (la tercera división, con la referencia estimada y «te faltan»). */
  private val free = "Esta semana estás En la plataforma, la primera de cinco divisiones de la referencia estimada para tu edad. Te faltan 10 aciertos para Despegando."

  @Before fun setUp() { TestSupport.fastFrames() }

  private fun mount(widthDp: Int, fontScale: Float, freeLine: String?, lock: String) {
    widthState = widthDp; fontState = fontScale
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontState), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(widthState.dp).height(1200.dp)) {
            BoardBackground()
            Column(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp)) { ContrarrelojDoor(record = 26, plus = false, onClick = {}, freeLine = freeLine, lockLabel = lock) }
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun heightDp() = rule.onNodeWithTag("avance_contrarreloj_door").fetchSemanticsNode().size.height / density

  private fun assertNoWordSplit(label: String) {
    for (n in rule.onAllNodes(SemanticsMatcher.keyIsDefined(SemanticsActions.GetTextLayoutResult), useUnmergedTree = true).fetchSemanticsNodes()) {
      val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
      n.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
      val l = results.firstOrNull() ?: continue
      val text = l.layoutInput.text.text
      for (line in 0 until l.lineCount - 1) {
        val end = l.getLineEnd(line, visibleEnd = false)
        assertTrue("$label: «$text» parte una palabra a la mitad en el renglón ${line + 1}", end >= text.length || text[end - 1] == ' ' || text[end] == ' ' || text[end - 1] == '\n' || text[end - 1] == NBSP[0])
      }
    }
  }

  private fun check(widthDp: Int, scale: Float) {
    mount(widthDp, scale, free, PlusAccess.LOCK_LABEL_COMPARED)
    val label = "a $widthDp dp con letra $scale"
    assertNoWordSplit(label)
    val door = rule.onNodeWithTag("avance_contrarreloj_door").fetchSemanticsNode()
    val left = door.positionInRoot.x / density; val right = (door.positionInRoot.x + door.size.width) / density
    assertTrue("$label: la losa se sale del margen de 16 dp (x %.1f a %.1f)".format(left, right), left >= 15.5f && right <= widthDp - 15.5f)
    for (tag in listOf("avance_contrarreloj_title", "avance_contrarreloj_line", "avance_contrarreloj_free", "avance_contrarreloj_lock_label")) {
      val n = rule.onNode(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNode()
      assertTrue("$label: «$tag» se sale de la losa", (n.positionInRoot.x + n.size.width) / density <= right + 0.5f && n.positionInRoot.x / density >= left - 0.5f)
    }
    val h = heightDp()
    println("T209 puerta (frase gratis y candado largo) %d dp, letra %.1f: %.0f dp de alto".format(widthDp, scale, h))
    assertTrue("$label: la puerta mide %.0f dp (tope 800)".format(h), h <= 800f)
    assertEquals("el lector dice el candado largo (Tarea 209)", true, rule.onNodeWithTag("avance_contrarreloj_door").fetchSemanticsNode().config.getOrNull(SemanticsProperties.ContentDescription)?.joinToString(". ")?.replace(NBSP, " ")?.endsWith(PlusAccess.LOCK_LABEL_COMPARED) == true)
  }

  @Test fun la_puerta_con_el_candado_largo_cabe_a_393_con_letra_1_0() = check(393, 1f)
  @Test fun la_puerta_con_el_candado_largo_cabe_a_393_con_letra_1_3() = check(393, 1.3f)
  @Test fun la_puerta_con_el_candado_largo_cabe_a_393_con_letra_2_0() = check(393, 2f)
  @Test fun la_puerta_con_el_candado_largo_cabe_a_360_con_letra_1_0() = check(360, 1f)
  @Test fun la_puerta_con_el_candado_largo_cabe_a_360_con_letra_2_0() = check(360, 2f)

  /** Lo que medía la puerta ANTES de la Tarea 209 (la frase gratis sin el lugar de la división y «Con Nubi Plus»), con las mismas letras y anchos: solo se anota, para decir cuánto creció. */
  @Test
  fun anota_cuanto_media_la_puerta_de_antes() {
    val before = "Esta semana estás En la plataforma en la referencia estimada para tu edad. Te faltan 10 aciertos para Despegando."
    for ((w, f) in listOf(393 to 1f, 393 to 1.3f, 393 to 2f, 360 to 1f, 360 to 2f)) {
      widthState = w; fontState = f
      if (w == 393 && f == 1f) mount(w, f, before, PlusAccess.LOCK_LABEL) else { rule.waitForIdle() }
      if (!(w == 393 && f == 1f)) { widthState = w; fontState = f; rule.waitForIdle() }
      println("T209 puerta de ANTES %d dp, letra %.1f: %.0f dp de alto".format(w, f, heightDp()))
    }
  }

  @Test
  fun sin_comparacion_la_puerta_queda_como_era_con_su_candado_corto() {
    mount(393, 1f, null, PlusAccess.lockLabel(Comparison.Kind.NONE))
    assertEquals("Con Nubi Plus", PlusAccess.LOCK_LABEL)
    val h = heightDp()
    println("T209 puerta sin comparación, 393 dp, letra 1,0: %.0f dp de alto".format(h))
    assertTrue("sin frase gratis ni candado largo la puerta es baja (%.0f dp)".format(h), h <= 150f)
  }
}
