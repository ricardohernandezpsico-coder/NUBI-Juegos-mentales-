package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.SemanticsMatcher
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.NBSP
import com.example.data.Preguntas
import com.example.data.ReferenciaFuentes
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.FuentesPanel
import com.example.ui.screens.PreguntasPanel
import com.example.ui.screens.PreguntasScreen
import com.example.ui.screens.ReferenciaScreen
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * «Preguntas frecuentes» y «De dónde sale la referencia» (Tarea 209): las losas plegadas (la pregunta a la vista, la respuesta al tocar, «plegado» y «desplegado» para el lector), el enlace de la pregunta del Contrarreloj, las dos del cobro apagadas, los diez estudios con su enlace al DOI, y con letra 1,0 / 1,3 / 2,0 a 393 y 360 dp
 * nada fuera del margen, ninguna palabra partida a la mitad y toda losa de 48 dp o más. Una sola pantalla por prueba (un solo `setContent`); el ancho y la letra se cambian después.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class PreguntasYReferenciaScreenTest {
  @get:Rule val rule = createComposeRule()

  private var widthState by mutableStateOf(393)
  private var fontState by mutableStateOf(1f)
  private var expanded by mutableStateOf(setOf<String>())
  private var sourceCalls = 0
  private var billing by mutableStateOf(false)
  private val opened = mutableListOf<String>()
  private val density get() = rule.density.density

  @Before fun setUp() { TestSupport.fastFrames() }

  private fun mount(widthDp: Int = 393, fontScale: Float = 1f, content: @androidx.compose.runtime.Composable () -> Unit) {
    widthState = widthDp; fontState = fontScale
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontState), LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(widthState.dp).height(873.dp)) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  private fun mountPreguntas(widthDp: Int = 393, fontScale: Float = 1f) = mount(widthDp, fontScale) {
    PreguntasScreen(
      expanded = expanded, onToggle = { k -> expanded = if (k in expanded) expanded - k else expanded + k }, onOpenReferencia = { sourceCalls++ },
      billingOn = billing, openLink = { url -> opened += url; true }
    )
  }

  private fun exists(tag: String) = rule.onAllNodes(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNodes().isNotEmpty()
  private fun node(tag: String) = rule.onNode(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNode()
  private fun textOf(tag: String): String = node(tag).config.getOrNull(SemanticsProperties.Text)?.joinToString("") { it.text }.orEmpty().replace(NBSP, " ")
  private fun state(tag: String): String? = node(tag).config.getOrNull(SemanticsProperties.StateDescription)
  private fun heightDp(tag: String) = node(tag).size.height / density
  // posición y tamaño SIN recortar (boundsInRoot recorta lo que queda fuera de la vista de un deslizable y devuelve 0)
  private fun top(tag: String) = node(tag).positionInRoot.y / density
  private fun left(tag: String) = node(tag).positionInRoot.x / density
  private fun right(tag: String) = (node(tag).positionInRoot.x + node(tag).size.width) / density

  /** NUNCA una palabra partida a la mitad: el renglón de cada texto termina en un espacio o al final. */
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

  // ------------------------------------------------------------------ Preguntas frecuentes

  @Test
  fun las_seis_preguntas_salen_plegadas_con_su_lead_y_sin_ninguna_respuesta_a_la_vista() {
    mountPreguntas()
    assertEquals("Respuestas cortas. Si falta una, escríbenos desde Contacto.", textOf("preguntas_lead"))
    assertEquals(Preguntas.general.map { it.key }, Preguntas.visible().map { it.key })
    for (p in Preguntas.general) {
      assertTrue("falta la losa de «${p.question}»", exists("pregunta_${p.key}"))
      assertEquals("«${p.question}» a la vista", p.question, textOf("pregunta_${p.key}_q"))
      assertFalse("nada se abre solo: «${p.key}» sin respuesta", exists("pregunta_${p.key}_a"))
      assertEquals("plegado", state("pregunta_${p.key}"))
      assertTrue("la losa mide 48 dp o más (%.1f)".format(heightDp("pregunta_${p.key}")), heightDp("pregunta_${p.key}") >= 48f)
    }
    assertFalse("las del cobro, apagadas", exists("pregunta_renueva") || exists("pregunta_cancelar"))
  }

  @Test
  fun tocar_una_pregunta_despliega_su_respuesta_en_la_misma_losa_y_tocarla_de_nuevo_la_pliega() {
    mountPreguntas()
    val closedHeight = heightDp("pregunta_telefono")
    rule.onNodeWithTag("pregunta_telefono").performScrollTo().performClick(); rule.waitForIdle()
    assertEquals(Preguntas.general.first { it.key == "telefono" }.answer, textOf("pregunta_telefono_a"))
    assertEquals("desplegado", state("pregunta_telefono"))
    assertTrue("la losa creció (de %.0f a %.0f dp)".format(closedHeight, heightDp("pregunta_telefono")), heightDp("pregunta_telefono") > closedHeight + 40f)
    assertEquals("las demás siguen plegadas", "plegado", state("pregunta_racha"))
    rule.onNodeWithTag("pregunta_telefono").performClick(); rule.waitForIdle()
    assertFalse(exists("pregunta_telefono_a")); assertEquals("plegado", state("pregunta_telefono"))
  }

  @Test
  fun la_pregunta_del_Contrarreloj_y_la_de_14_a_17_traen_la_fila_de_donde_sale_la_referencia_y_las_demas_no() {
    expanded = Preguntas.general.map { it.key }.toSet()
    mountPreguntas()
    assertEquals(listOf("pregunta_referencia_source", "pregunta_edad_source"), Preguntas.general.map { "pregunta_${it.key}_source" }.filter { exists(it) })
    rule.onNodeWithTag("pregunta_referencia_source").performScrollTo()
    assertTrue("la fila mide 48 dp o más (%.1f)".format(heightDp("pregunta_referencia_source")), heightDp("pregunta_referencia_source") >= 47.5f)
    assertEquals(Role.Button, rule.onNode(hasTestTag("pregunta_referencia_source")).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Role))
    assertEquals("De dónde sale la referencia", rule.onNode(hasTestTag("pregunta_referencia_source")).fetchSemanticsNode().config[SemanticsProperties.ContentDescription].joinToString(". "))
    rule.onNodeWithTag("pregunta_referencia_source").performClick(); rule.waitForIdle()
    assertEquals("tocarla abre «De dónde sale la referencia»", 1, sourceCalls)
    assertEquals("y la losa sigue desplegada", "desplegado", state("pregunta_referencia"))
  }

  @Test
  fun con_la_bandera_del_cobro_encendida_sus_dos_preguntas_van_arriba_y_la_de_cancelar_trae_su_boton_de_48_dp() {
    billing = true; expanded = setOf("cancelar")
    mountPreguntas()
    val tags = rule.onAllNodes(SemanticsMatcher("una pregunta") { it.config.getOrNull(SemanticsProperties.TestTag)?.let { t -> t.startsWith("pregunta_") && !t.endsWith("_q") && !t.endsWith("_a") && !t.endsWith("_source") && !t.endsWith("_subscriptions") } == true }, useUnmergedTree = true)
      .fetchSemanticsNodes().mapNotNull { it.config.getOrNull(SemanticsProperties.TestTag) }
    assertEquals("las del cobro arriba y las seis de siempre debajo", listOf("pregunta_renueva", "pregunta_cancelar") + Preguntas.general.map { "pregunta_${it.key}" }, tags)
    rule.onNodeWithTag("pregunta_cancelar_subscriptions").performScrollTo()
    assertTrue("el botón mide 48 dp o más (%.1f)".format(heightDp("pregunta_cancelar_subscriptions")), heightDp("pregunta_cancelar_subscriptions") >= 47.5f)
    rule.onNodeWithTag("pregunta_cancelar_subscriptions").performClick(); rule.waitForIdle()
    assertEquals(listOf("https://play.google.com/store/account/subscriptions?package=com.nubibraingames.app"), opened)
  }

  private fun assertPreguntasFit(widthDp: Int, scale: Float) {
    expanded = Preguntas.general.map { it.key }.toSet()
    mountPreguntas(widthDp, scale)
    val label = "a $widthDp dp con letra $scale"
    assertNoWordSplit(label)
    for (p in Preguntas.general) {
      val tag = "pregunta_${p.key}"
      assertTrue("$label: «$tag» se sale del margen de 20 dp (x %.1f a %.1f)".format(left(tag), right(tag)), left(tag) >= 19.5f && right(tag) <= widthDp - 19.5f)
      assertTrue("$label: «$tag» mide 48 dp o más", heightDp(tag) >= 48f)
      assertTrue("$label: el texto de «$tag» se ve entero (la pregunta cabe en su losa)", right("${tag}_q") <= right(tag) + 0.5f && right("${tag}_a") <= right(tag) + 0.5f)
    }
    // de arriba abajo, sin pisarse: cada losa empieza donde (o después de donde) termina la anterior
    val ys = Preguntas.general.map { top("pregunta_${it.key}") to top("pregunta_${it.key}") + heightDp("pregunta_${it.key}") }
    for (i in 1 until ys.size) assertTrue("$label: las losas ${i} y ${i + 1} se tapan", ys[i].first >= ys[i - 1].second - 0.5f)
  }

  @Test fun las_preguntas_caben_a_393_con_letra_1_0() = assertPreguntasFit(393, 1f)
  @Test fun las_preguntas_caben_a_393_con_letra_1_3() = assertPreguntasFit(393, 1.3f)
  @Test fun las_preguntas_caben_a_393_con_letra_2_0() = assertPreguntasFit(393, 2f)
  @Test fun las_preguntas_caben_a_360_con_letra_1_0() = assertPreguntasFit(360, 1f)
  @Test fun las_preguntas_caben_a_360_con_letra_2_0() = assertPreguntasFit(360, 2f)

  @Test
  fun el_panel_abre_la_hoja_de_la_referencia_dentro_de_si_mismo_y_Atras_vuelve_con_lo_que_estaba_abierto() {
    mount { PreguntasPanel(onClose = {}) }
    rule.onNodeWithTag("pregunta_referencia").performScrollTo().performClick(); rule.waitForIdle()
    rule.onNodeWithTag("pregunta_referencia_source").performScrollTo().performClick(); rule.waitForIdle()
    assertTrue("se ve «De dónde sale la referencia»", exists("referencia_screen"))
    assertFalse("y ya no las preguntas", exists("preguntas_screen"))
    rule.onNodeWithTag("btn_panel_back").performClick(); rule.waitForIdle()
    assertTrue("Atrás vuelve a las preguntas", exists("preguntas_screen"))
    assertEquals("con la pregunta todavía desplegada", "desplegado", state("pregunta_referencia"))
    assertFalse(exists("referencia_screen"))
  }

  // ------------------------------------------------------------------ De dónde sale la referencia

  @Test
  fun la_hoja_dice_primero_que_no_son_otras_personas_y_trae_los_once_estudios_en_cuatro_grupos() {
    mount { ReferenciaScreen(openLink = { url -> opened += url; true }) }
    assertEquals(ReferenciaFuentes.INTRO, textOf("referencia_intro"))
    assertEquals("el segundo párrafo (Tarea 213: el de antes, sin su primera frase)", ReferenciaFuentes.INTRO_2, textOf("referencia_intro_2"))
    assertEquals(ReferenciaFuentes.ABOUT, textOf("referencia_about"))
    assertEquals("la línea de 14 a 17 años va justo después (Tarea 216)", ReferenciaFuentes.TEEN_LINE, textOf("referencia_teen").replace(com.example.data.NBSP, " "))
    assertTrue("y antes de los grupos", top("referencia_about") < top("referencia_teen") && top("referencia_teen") < top("referencia_grupo_rapidez"))
    for (g in ReferenciaFuentes.groups) assertTrue("falta el grupo «${g.title}»", exists("referencia_grupo_${g.key}"))
    assertEquals("la línea del último grupo", ReferenciaFuentes.SHARED_LINE, textOf("referencia_linea_otros"))
    for (e in ReferenciaFuentes.studies) assertTrue("falta el estudio ${e.cite}", exists("estudio_${e.doi}"))
    rule.onNodeWithTag("estudio_10.1037/0882-7974.21.1.62").performScrollTo().performClick(); rule.waitForIdle()
    assertEquals("tocar un estudio abre su DOI en el navegador", listOf("https://doi.org/10.1037/0882-7974.21.1.62"), opened)
  }

  private fun assertReferenciaFits(widthDp: Int, scale: Float) {
    mount(widthDp, scale) { ReferenciaScreen(openLink = { true }) }
    val label = "a $widthDp dp con letra $scale"
    assertNoWordSplit(label)
    for (tag in listOf("referencia_intro", "referencia_intro_2", "referencia_about", "referencia_teen") + ReferenciaFuentes.studies.map { "estudio_${it.doi}" })
      assertTrue("$label: «$tag» se sale del margen de 20 dp (x %.1f a %.1f)".format(left(tag), right(tag)), left(tag) >= 19.5f && right(tag) <= widthDp - 19.5f)
    for (e in ReferenciaFuentes.studies) assertTrue("$label: la losa de ${e.cite} mide 48 dp o más", heightDp("estudio_${e.doi}") >= 48f)
  }

  @Test fun la_hoja_de_la_referencia_cabe_a_393_con_letra_1_0() = assertReferenciaFits(393, 1f)
  @Test fun la_hoja_de_la_referencia_cabe_a_393_con_letra_2_0() = assertReferenciaFits(393, 2f)
  @Test fun la_hoja_de_la_referencia_cabe_a_360_con_letra_1_0() = assertReferenciaFits(360, 1f)
  @Test fun la_hoja_de_la_referencia_cabe_a_360_con_letra_2_0() = assertReferenciaFits(360, 2f)

  @Test
  fun el_titulo_del_panel_de_la_referencia_no_parte_palabras_con_letra_2_0_a_360_dp() {
    mount(360, 2f) { com.example.ui.screens.ReferenciaPanel(onClose = {}) }
    assertNoWordSplit("el panel entero a 360 dp con letra 2,0")
    assertNotNull(node("btn_panel_back"))
  }

  // ------------------------------------------------------------------ «En qué se basa Nubilus» suma la fila de la referencia

  @Test
  fun en_que_se_basa_nubi_trae_la_fila_de_la_referencia_y_abre_la_hoja_dentro_del_panel() {
    mount { FuentesPanel(onClose = {}) }
    rule.onNodeWithTag("fuentes_referencia_row").performScrollTo()
    assertEquals(Role.Button, rule.onNode(hasTestTag("fuentes_referencia_row")).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Role))
    assertEquals("De dónde sale la referencia del Contrarreloj", rule.onNode(hasTestTag("fuentes_referencia_row")).fetchSemanticsNode().config[SemanticsProperties.ContentDescription].joinToString(". "))
    assertTrue("la fila mide 48 dp o más", heightDp("fuentes_referencia_row") >= 48f)
    // va entre «Cómo está hecho Nubilus» y «Las palabras»
    assertTrue("antes de «Las palabras»", top("fuentes_referencia_row") < top("fuentes_palabras"))
    assertTrue("y después de «Cómo está hecho Nubilus»", top("fuentes_referencia_row") > top("fuentes_mecanismos"))
    rule.onNodeWithTag("fuentes_referencia_row").performClick(); rule.waitForIdle()
    assertTrue(exists("referencia_screen")); assertFalse(exists("fuentes_screen"))
    rule.onNodeWithTag("btn_panel_back").performClick(); rule.waitForIdle()
    assertTrue("Atrás vuelve a «En qué se basa Nubilus»", exists("fuentes_screen")); assertFalse(exists("referencia_screen"))
  }
}
