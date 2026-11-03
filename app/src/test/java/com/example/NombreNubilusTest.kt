package com.example

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsNode
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.performScrollToNode
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.NBSP
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.ContactoSection
import com.example.ui.screens.FuentesPanel
import com.example.ui.screens.PreguntasPanel
import com.example.ui.screens.SettingsScreen
import com.example.ui.screens.TuScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.NeuroVidaViewModel
import org.junit.After
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
 * El nombre «Nubilus» (Tarea 217, 2-nov; `auditoria-fable/nombre-app.md` §7.3 y §7.4): «Nubilus» es tres letras más largo que «Nubi», y ahí donde el nombre va en un título, un botón o una pregunta, que cabe o pasa a dos renglones POR PALABRAS, dentro de su losa, sin partir ninguna palabra ni salirse
 * del ancho. Cada pantalla se mira a 360 dp (lo más angosto) con letra 1,0, 1,3 y 2,0: Tú («Quién hace Nubilus» y el botón «En qué se basa Nubilus»), Más ajustes («Acerca de Nubilus»), Contacto («Escribir a Nubilus»), el panel «En qué se basa Nubilus» (con «Cómo está hecho Nubilus») y las preguntas
 * frecuentes. Una composición por prueba.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w360dp-h780dp-xxhdpi", sdk = [36])
class NombreNubilusTest {
  @get:Rule val rule = createComposeRule()

  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before fun setUp() { TestSupport.resetDatabase(); TestSupport.fastFrames() }
  @After fun tearDown() { TestSupport.release(*viewModels.toTypedArray()); viewModels.clear(); TestSupport.resetDatabase() }

  private fun vm() = NeuroVidaViewModel(ApplicationProvider.getApplicationContext<Application>()).also { viewModels.add(it) }

  private fun mount(scale: Float, content: @androidx.compose.runtime.Composable () -> Unit) {
    rule.mainClock.autoAdvance = true
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true, LocalBarInset provides 88.dp) {
        NeuroVidaTheme { Box(Modifier.width(360.dp).height(4200.dp)) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  /** Los renglones que terminaron DENTRO de una palabra (entre dos letras, sin espacio ni salto: así corta Android lo que no tiene dónde partir). Los espacios duros pegan. */
  private fun cutsIn(node: SemanticsNode, out: MutableList<String>, only: (String) -> Boolean = { "Nubilus" in it }) {
    val results = mutableListOf<TextLayoutResult>()
    node.config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(results)
    val layout = results.firstOrNull()
    // solo los textos que llevan el nombre: lo que ya estaba (las horas del recordatorio, por ejemplo) no es de esta prueba
    if (layout != null && only(layout.layoutInput.text.text)) {
      val text = layout.layoutInput.text.text
      for (line in 0 until layout.lineCount - 1) {
        val end = layout.getLineEnd(line)
        if (end in 1 until text.length) {
          val before = text[end - 1]; val after = text[end]
          if (before != ' ' && before != '\n' && after != ' ' && after != '\n') out += "«${text.substring(maxOf(0, end - 14), end)}|${text.substring(end, minOf(text.length, end + 14))}»".replace(NBSP, "·")
        }
      }
    }
    node.children.forEach { cutsIn(it, out, only) }
  }

  private fun cuts(only: (String) -> Boolean = { "Nubilus" in it }): List<String> = mutableListOf<String>().also { cutsIn(rule.onRoot(useUnmergedTree = true).fetchSemanticsNode(), it, only) }

  private fun textNodes(text: String) = rule.onAllNodes(hasText(text, substring = false), useUnmergedTree = true).fetchSemanticsNodes()

  private fun lines(text: String): Int {
    val results = mutableListOf<TextLayoutResult>()
    textNodes(text).first().config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(results)
    return results.first().lineCount
  }

  private fun bounds(text: String) = textNodes(text).first().boundsInRoot

  /** Que el texto exista, quepa en el ancho de la pantalla y no parta ninguna palabra; devuelve cuántos renglones usa. */
  private fun checkTitle(text: String): Int {
    assertTrue("«$text» está en la pantalla", textNodes(text).isNotEmpty())
    val b = bounds(text)
    assertTrue("«$text» no se sale del ancho (${b.left} a ${b.right} de 360 dp con la densidad de la prueba)", b.left >= 0f && b.right <= rule.onRoot().fetchSemanticsNode().boundsInRoot.right + 1f)
    return lines(text)
  }

  // ---------------------------------------------------------------- Tú: «Quién hace Nubilus» y «En qué se basa Nubilus»

  private fun tu(scale: Float) {
    val v = vm()
    mount(scale) { TuScreen(viewModel = v) }
    rule.onNodeWithTag("tu_lista").performScrollToNode(hasTestTag("tu_quien_hace")); rule.waitForIdle()        // Tú es una lista perezosa: cada bloque se compone al llegar a él
    val who = checkTitle("Quién hace Nubilus")
    val cutsWho = cuts()
    rule.onNodeWithTag("tu_lista").performScrollToNode(hasTestTag("btn_open_fuentes")); rule.waitForIdle()
    val basis = checkTitle("En qué se basa Nubilus")
    assertTrue("«Quién hace Nubilus» cabe en una línea con letra hasta 1,3 y a 2,0 pasa a dos por palabras (renglones: $who)", if (scale <= 1.3f) who == 1 else who in 1..2)
    assertTrue("el botón «En qué se basa Nubilus»: a lo sumo tres renglones ($basis)", basis in 1..3)
    assertEquals("ninguna palabra con el nombre partida en Tú a ${scale}", emptyList<String>(), cutsWho + cuts())
  }

  @Test fun tu_a_360_con_letra_1_0() = tu(1f)
  @Test fun tu_a_360_con_letra_1_3() = tu(1.3f)
  @Test fun tu_a_360_con_letra_2_0() = tu(2f)

  // ---------------------------------------------------------------- Más ajustes: «Acerca de Nubilus»

  private fun ajustes(scale: Float) {
    val v = vm()
    mount(scale) { SettingsScreen(viewModel = v) }
    val rows = checkTitle("Acerca de Nubilus")
    assertTrue("«Acerca de Nubilus» cabe en una línea con 1,0 y 1,3 y a 2,0 pasa a dos por palabras (renglones: $rows)", if (scale <= 1.3f) rows == 1 else rows in 1..2)
    val card = rule.onNodeWithTag("card_acerca").fetchSemanticsNode().boundsInRoot
    val title = bounds("Acerca de Nubilus")
    assertTrue("el título queda dentro de su losa: $title contra $card", title.left >= card.left && title.right <= card.right && title.top >= card.top && title.bottom <= card.bottom)
    assertEquals("ninguna palabra partida en Más ajustes a ${scale}", emptyList<String>(), cuts())
  }

  @Test fun ajustes_a_360_con_letra_1_0() = ajustes(1f)
  @Test fun ajustes_a_360_con_letra_1_3() = ajustes(1.3f)
  @Test fun ajustes_a_360_con_letra_2_0() = ajustes(2f)

  // ---------------------------------------------------------------- las horas del recordatorio (hallazgo de esta prueba: a 360 dp con letra grande «atardecer» se partía por dentro)

  private fun horas(scale: Float) {
    val v = vm()
    mount(scale) { SettingsScreen(viewModel = v) }
    val labels = listOf("Mañana", "Tarde", "Al atardecer", "Noche")
    assertEquals("ninguna hora parte una palabra a ${scale}", emptyList<String>(), cuts { t -> labels.any { it in t } })
    val manana = rule.onNodeWithTag("btn_time_mañana").fetchSemanticsNode().boundsInRoot
    val tarde = rule.onNodeWithTag("btn_time_tarde").fetchSemanticsNode().boundsInRoot
    if (scale <= 1.15f) assertEquals("con letra normal las horas siguen en una cuadrícula de 2 × 2", manana.top, tarde.top, 1f)
    else assertTrue("con letra grande cada hora va en su fila (${manana.top} contra ${tarde.top})", tarde.top > manana.bottom - 1f)
  }

  @Test fun horas_a_360_con_letra_1_0() = horas(1f)
  @Test fun horas_a_360_con_letra_1_15() = horas(1.15f)          // un paso real de Android y el último con dos columnas: queda justo bajo el umbral (Tarea 217c; Fable: unos 8 dp de aire)
  @Test fun horas_a_360_con_letra_1_3() = horas(1.3f)
  @Test fun horas_a_360_con_letra_2_0() = horas(2f)

  // ---------------------------------------------------------------- Contacto: «Escribir a Nubilus»

  private fun contacto(scale: Float) {
    mount(scale) { ContactoSection(onSendReport = {}, email = null) }
    val rows = checkTitle("Escribir a Nubilus")
    assertTrue("«Escribir a Nubilus» cabe en una línea con 1,0 y 1,3 y a 2,0 pasa a dos por palabras (renglones: $rows)", if (scale <= 1.3f) rows == 1 else rows in 1..2)
    val button = rule.onNodeWithTag("btn_write_to_nubi").fetchSemanticsNode().boundsInRoot
    assertTrue("el botón mide 48 dp o más (${button.height} px a ${scale})", button.height >= 48f * rule.density.density * 0.99f)
    assertTrue("y el título queda dentro de él", bounds("Escribir a Nubilus").let { it.left >= button.left && it.right <= button.right && it.top >= button.top && it.bottom <= button.bottom })
    assertEquals("ninguna palabra partida en Contacto a ${scale}", emptyList<String>(), cuts())
  }

  @Test fun contacto_a_360_con_letra_1_0() = contacto(1f)
  @Test fun contacto_a_360_con_letra_1_3() = contacto(1.3f)
  @Test fun contacto_a_360_con_letra_2_0() = contacto(2f)

  // ---------------------------------------------------------------- el panel «En qué se basa Nubilus» y «Cómo está hecho Nubilus»

  private fun panelFuentes(scale: Float) {
    mount(scale) { FuentesPanel(onClose = {}) }
    val title = checkTitle("En qué se basa Nubilus")
    assertTrue("el título del panel: dos o tres renglones a lo sumo ($title)", title in 1..3)
    checkTitle("Cómo está hecho Nubilus")
    assertEquals("ninguna palabra partida en el panel a ${scale}", emptyList<String>(), cuts())
  }

  @Test fun panel_de_fuentes_a_360_con_letra_1_0() = panelFuentes(1f)
  @Test fun panel_de_fuentes_a_360_con_letra_1_3() = panelFuentes(1.3f)
  @Test fun panel_de_fuentes_a_360_con_letra_2_0() = panelFuentes(2f)

  // ---------------------------------------------------------------- las preguntas frecuentes

  private fun preguntas(scale: Float) {
    mount(scale) { PreguntasPanel(onClose = {}) }
    for (q in listOf("¿Quién hace Nubilus?", "¿Nubilus necesita internet o una cuenta?")) checkTitle(q)
    assertEquals("ninguna palabra partida en las preguntas a ${scale}", emptyList<String>(), cuts())
  }

  @Test fun preguntas_a_360_con_letra_1_0() = preguntas(1f)
  @Test fun preguntas_a_360_con_letra_1_3() = preguntas(1.3f)
  @Test fun preguntas_a_360_con_letra_2_0() = preguntas(2f)

  // ---------------------------------------------------------------- el informe de errores y el asunto del correo

  @Test fun el_informe_de_errores_y_el_asunto_del_correo_llevan_el_nombre_nuevo() {
    val report = com.example.diag.ErrorLog.report(ApplicationProvider.getApplicationContext<Application>())
    assertTrue("la cabecera del informe empieza con el nombre nuevo: «${report.lineSequence().first()}»", report.startsWith("Nubilus "))
    assertEquals("Nubilus", com.example.data.NubiContact.SUBJECT)
  }
}
