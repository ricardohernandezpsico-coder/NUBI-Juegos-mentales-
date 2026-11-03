package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.assertIsSelected
import androidx.compose.ui.test.assertIsNotSelected
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NeuroNavBar
import com.example.ui.components.navColumnWidths
import com.example.ui.components.navTextScaleCap
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La barra inferior del kit (Tarea 156, opción C de Fable): cuatro discos crema de 48 dp con un ícono vectorial de línea de tinta (isla, cronómetro, barras, persona; Tarea 215: Hoy · Contrarreloj · Avance · Tú), su rótulo de 16 sp debajo y la pestaña elegida sobre una losa sol, con el rótulo dentro, en tinta y negrita.
 * Se prueba que cada pestaña sea `Role.Tab` con `selected` y su nombre, que ocupe toda su columna (de ancho desigual, medido con el rótulo en NEGRITA, y todo el alto de la barra: ≥ 48 dp en los dos ejes), que elegir otra pestaña no mueva ninguna columna, que la losa sol mida el ancho de su columna menos 8 dp,
 * que la barra y sus rótulos queden DENTRO de la pantalla y sobre el inset de navegación (48 dp, como con 3 botones), que a 393 dp mida ~89 dp (95 con letra 1,3, y de ahí no sube: los rótulos tienen tope de letra 1,3), que los rótulos sean de 16 sp o más y de UNA línea con letra 1,0, 1,3 y 2,0
 * a 360 y 393 dp, que SOLO la elegida lleve la losa, y que con «quitar animaciones» la elegida cambie de golpe; y se capturan las cuatro pestañas.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class NeuroNavBarTest {
  @get:Rule val rule = createComposeRule()

  private var selected: AppTab? = null
  private var shown by mutableStateOf(AppTab.HOY)

  // Una sola composición por prueba (Compose no deja `setContent` dos veces): `show` de nuevo solo cambia el ancho, el alto, la letra y la pestaña.
  private var contentSet = false
  private var widthState by mutableIntStateOf(393)
  private var heightState by mutableIntStateOf(873)
  private var scaleState by mutableFloatStateOf(1f)
  private var reduceState by mutableStateOf(false)

  private fun show(current: AppTab, widthDp: Int = 393, heightDp: Int = 873, fontScale: Float = 1f, reduce: Boolean = false) {
    shown = current
    widthState = widthDp; heightState = heightDp; scaleState = fontScale; reduceState = reduce
    if (!contentSet) {
      contentSet = true
      rule.setContent {
        val base = LocalDensity.current
        CompositionLocalProvider(LocalDensity provides Density(base.density, scaleState), LocalReduceMotion provides reduceState) {
          NeuroVidaTheme {
            Box(Modifier.width(widthState.dp).height(heightState.dp)) {
              BoardBackground()
              // 48 dp de la barra de navegación del teléfono (3 botones) bajo la nuestra
              Column(Modifier.align(Alignment.BottomCenter).padding(bottom = 48.dp)) { NeuroNavBar(current = shown, onSelect = { selected = it }) }
            }
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun bounds(tag: String): Rect = rule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot

  private val tabs = listOf("nav_item_hoy" to "Hoy", "nav_item_juegos" to "Juegos", "nav_item_contrarreloj" to "Contrarreloj", "nav_item_progreso" to "Avance", "nav_item_tu" to "Tú")

  private fun layoutOf(label: String): TextLayoutResult {
    val results = mutableListOf<TextLayoutResult>()
    rule.onNodeWithText(label, useUnmergedTree = true).fetchSemanticsNode().config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
    assertTrue("«$label» tiene su diseño de texto", results.isNotEmpty())
    return results.first()
  }

  // ---- Semántica

  @Test
  fun cada_pestana_es_Role_Tab_con_su_nombre_y_solo_la_elegida_esta_selected() {
    show(AppTab.PROGRESO)
    for ((tag, name) in tabs) {
      val node = rule.onNodeWithTag(tag).fetchSemanticsNode()
      assertEquals("$tag es una pestaña", Role.Tab, node.config[SemanticsProperties.Role])
      assertEquals("$tag se llama $name", listOf(name), node.config[SemanticsProperties.ContentDescription])
    }
    rule.onNodeWithTag("nav_item_progreso").assertIsSelected()
    rule.onNodeWithTag("nav_item_hoy").assertIsNotSelected()
    rule.onNodeWithTag("nav_item_juegos").assertIsNotSelected()
    rule.onNodeWithTag("nav_item_contrarreloj").assertIsNotSelected()
    rule.onNodeWithTag("nav_item_tu").assertIsNotSelected()
  }

  @Test
  fun tocar_una_pestana_la_elige() {
    show(AppTab.HOY)
    rule.onNodeWithTag("nav_item_tu").performClick()
    assertEquals(AppTab.TU, selected)
    rule.onNodeWithTag("nav_item_progreso").performClick()
    assertEquals(AppTab.PROGRESO, selected)
    rule.onNodeWithTag("nav_item_contrarreloj").performClick()
    assertEquals(AppTab.CONTRARRELOJ, selected)
    rule.onNodeWithTag("nav_item_juegos").performClick()
    assertEquals(AppTab.JUEGOS, selected)
  }

  @Test
  fun las_cinco_etiquetas_son_Hoy_Juegos_Contrarreloj_Avance_y_Tu_en_ese_orden() {
    show(AppTab.TU)
    for (t in listOf("Hoy", "Juegos", "Contrarreloj", "Avance", "Tú")) rule.onNodeWithText(t, useUnmergedTree = true).assertIsDisplayed()
    assertEquals(listOf(AppTab.HOY, AppTab.JUEGOS, AppTab.CONTRARRELOJ, AppTab.PROGRESO, AppTab.TU), AppTab.entries)
    val lefts = tabs.map { bounds(it.first).left }
    assertEquals("las columnas van de izquierda a derecha en el orden de las pestañas", lefts.sorted(), lefts)
  }

  // ---- La elegida: forma (losa sol) y peso, nunca solo color

  @Test
  fun solo_la_elegida_lleva_la_losa_sol() {
    show(AppTab.PROGRESO, reduce = true)
    rule.onNodeWithTag("nav_slab_progreso", useUnmergedTree = true).assertExists()
    rule.onNodeWithTag("nav_slab_hoy", useUnmergedTree = true).assertDoesNotExist()
    rule.onNodeWithTag("nav_slab_tu", useUnmergedTree = true).assertDoesNotExist()
  }

  @Test
  fun la_elegida_va_en_negrita_y_las_otras_no() {
    show(AppTab.TU)
    assertTrue("«Tú» elegida en negrita", (layoutOf("Tú").layoutInput.style.fontWeight?.weight ?: 400) >= 700)
    assertTrue("«Hoy» sin elegir no es negrita", (layoutOf("Hoy").layoutInput.style.fontWeight?.weight ?: 400) < 700)
    assertTrue("«Avance» sin elegir no es negrita", (layoutOf("Avance").layoutInput.style.fontWeight?.weight ?: 400) < 700)
  }

  @Test
  fun con_quitar_animaciones_la_losa_cambia_de_golpe() {
    show(AppTab.HOY, reduce = true)
    rule.onNodeWithTag("nav_slab_hoy", useUnmergedTree = true).assertExists()
    shown = AppTab.TU
    rule.waitForIdle()
    rule.onNodeWithTag("nav_slab_hoy", useUnmergedTree = true).assertDoesNotExist()
    rule.onNodeWithTag("nav_slab_tu", useUnmergedTree = true).assertExists()
  }

  @Test
  fun la_losa_se_funde_y_termina_en_la_nueva_elegida() {
    show(AppTab.HOY)
    shown = AppTab.CONTRARRELOJ
    rule.waitForIdle()
    rule.onNodeWithTag("nav_slab_hoy", useUnmergedTree = true).assertDoesNotExist()
    rule.onNodeWithTag("nav_slab_contrarreloj", useUnmergedTree = true).assertExists()
  }

  // ---- Medidas: dentro de la pantalla, sobre el inset, toque amplio

  private fun assertFitsAboveNavigation(widthDp: Int, heightDp: Int) {
    val density = rule.density.density
    val root = rule.onRoot().fetchSemanticsNode().boundsInRoot
    val bar = bounds("bottom_nav_bar")
    assertTrue("la barra queda dentro de la pantalla: $bar", bar.left >= -1f && bar.right <= widthDp * density + 1f && bar.top >= 0f)
    assertTrue("y sobre los 48 dp de la barra del teléfono: ${bar.bottom} contra ${root.bottom - 48 * density}", bar.bottom <= root.bottom - 48 * density + 1f)
    for ((tag, _) in tabs) {
      val b = bounds(tag)
      assertTrue("$tag mide al menos 48 dp de ancho: ${b.width / density}", b.width >= 48 * density)
      assertTrue("$tag mide al menos 48 dp de alto: ${b.height / density}", b.height >= 48 * density)
      assertTrue("$tag cabe en la barra: $b contra $bar", b.top >= bar.top - 1f && b.bottom <= bar.bottom + 1f && b.left >= -1f && b.right <= widthDp * density + 1f)
    }
    for ((_, t) in tabs) {
      val label = rule.onNodeWithText(t, useUnmergedTree = true).fetchSemanticsNode().boundsInRoot
      assertTrue("el rótulo «$t» queda dentro de la barra: $label contra $bar", label.bottom <= bar.bottom + 1f && label.top >= bar.top)
    }
  }

  @Test fun la_barra_cabe_sobre_el_inset_a_393() { show(AppTab.HOY); assertFitsAboveNavigation(393, 873) }
  @Test fun la_barra_cabe_sobre_el_inset_a_360() { show(AppTab.PROGRESO, 360, 780); assertFitsAboveNavigation(360, 780) }
  @Test fun la_barra_cabe_sobre_el_inset_a_411() { show(AppTab.TU, 411, 914); assertFitsAboveNavigation(411, 914) }
  @Test fun la_barra_cabe_con_letra_1_3() { show(AppTab.HOY, fontScale = 1.3f); assertFitsAboveNavigation(393, 873) }
  @Test fun la_barra_cabe_con_letra_2_0_y_crece_en_vez_de_cortar() { show(AppTab.HOY, fontScale = 2f); assertFitsAboveNavigation(393, 873) }

  // ---- Las columnas de ancho desigual (Tarea 215)

  /** El ancho de cada rótulo en negrita con letra 1,0 según Fable (`auditoria-fable/pestana-juegos-historial.md` §4.3, Fredoka Bold a 16 sp): Hoy 28,8 · Juegos 51,5 · Contrarreloj 91,8 · Avance 56,2 · Tú 19,5 dp. */
  private val fableLabels = listOf(28.8f, 51.5f, 91.8f, 56.2f, 19.5f)

  @Test
  fun el_reparto_de_ancho_suma_la_pantalla_respeta_los_48_dp_y_da_a_cada_rotulo_su_ancho_mas_aire() {
    for (total in listOf(360f, 393f, 411f)) {
      val w = navColumnWidths(fableLabels, total)
      assertEquals("las cinco columnas suman el ancho de la pantalla ($total)", total, w.sum(), 0.01f)
      assertTrue("todas miden 48 dp o más: $w", w.all { it >= 48f })
      assertTrue("y cada una cabe su rótulo con 12 dp de aire: $w", w.indices.all { w[it] >= fableLabels[it] + 12f })
      assertTrue("la columna más ancha es la de Contrarreloj", w[2] == w.max())
      val gain = w.indices.map { w[it] - maxOf(fableLabels[it] + 12f, 48f) }
      assertTrue("el sobrante se reparte parejo: todas ganan lo mismo (%s)".format(gain), gain.all { Math.abs(it - gain[0]) < 0.01f })
    }
  }

  @Test
  fun si_no_hubiera_ni_para_los_minimos_se_achican_todas_parejas_en_vez_de_salirse() {
    val w = navColumnWidths(listOf(200f, 200f, 200f, 200f, 200f), 360f)
    assertEquals(360f, w.sum(), 0.01f)
    assertTrue("en la misma proporción", w.all { Math.abs(it - 72f) < 0.01f })
  }

  @Test
  fun a_360_y_a_393_las_columnas_son_las_que_dice_la_cuenta_y_cada_rotulo_va_centrado_en_la_suya() {
    for (width in listOf(360, 393)) {
      show(AppTab.HOY, width, 873)
      val density = rule.density.density
      val cols = tabs.map { bounds(it.first) }
      var left = 0f
      for ((i, pair) in tabs.withIndex()) {
        val col = cols[i]
        assertEquals("$width dp: ${pair.first} empieza donde termina la anterior", left, col.left / density, 1f)
        assertTrue("$width dp: ${pair.first} mide al menos 48 dp: ${col.width / density}", col.width / density >= 47.5f)
        val label = rule.onNodeWithText(pair.second, useUnmergedTree = true).fetchSemanticsNode().boundsInRoot
        assertEquals("$width dp: «${pair.second}» centrada en su columna", col.center.x, label.center.x, 1.5f * density)
        left += col.width / density
      }
      assertEquals("las cinco llenan el ancho", width.toFloat(), left, 1f)
      assertTrue("Contrarreloj es la más ancha y Tú la más angosta: ${cols.map { it.width / density }}", cols[2].width == cols.maxOf { it.width } && cols[4].width == cols.minOf { it.width })
    }
  }

  @Test
  fun elegir_otra_pestana_no_mueve_ninguna_columna() {
    for (width in listOf(360, 393)) for (scale in listOf(1f, 1.3f, 2f)) {
      show(AppTab.HOY, width, 873, fontScale = scale)
      val before = tabs.map { bounds(it.first) }
      for (t in AppTab.entries) {
        shown = t
        rule.waitForIdle()
        val now = tabs.map { bounds(it.first) }
        // (a lo ancho, nada: ni un píxel; a lo alto, a lo sumo 1 px de redondeo de la línea del rótulo en negrita, como antes de la Tarea 215)
        for (i in tabs.indices) {
          assertEquals("a $width dp con letra $scale, la columna ${tabs[i].second} no se corre al elegir $t", before[i].left, now[i].left, 0f)
          assertEquals("a $width dp con letra $scale, la columna ${tabs[i].second} no cambia de ancho al elegir $t", before[i].right, now[i].right, 0f)
          assertEquals("y tampoco de alto (${tabs[i].second}, $t): como mucho lo que mide una línea de más del rótulo en negrita", before[i].height, now[i].height, 2.5f * rule.density.density)
        }
      }
    }
  }

  @Test
  fun la_losa_sol_mide_el_ancho_de_su_columna_menos_8_dp() {
    for (width in listOf(360, 393)) {
      for (t in AppTab.entries) {
        show(t, width, 873, reduce = true)
        val density = rule.density.density
        val slab = rule.onNodeWithTag("nav_slab_body_${t.name.lowercase()}", useUnmergedTree = true).fetchSemanticsNode().boundsInRoot
        val col = bounds("nav_item_${t.name.lowercase()}")
        // (la losa lleva 6 dp de sombra abajo, pero el ancho es el del cuerpo: el contenedor la centra en su columna)
        assertEquals("a $width dp la losa de $t: columna ${col.width / density} menos 8", (col.width / density) - 8f, slab.width / density, 1.5f)
        assertEquals("y centrada en su columna", col.center.x, slab.center.x, 1.5f * density)
      }
    }
  }

  @Test
  fun a_393_dp_la_barra_mide_alrededor_de_89_dp_y_a_letra_1_3_alrededor_de_95() {
    show(AppTab.HOY)
    val h = bounds("bottom_nav_bar").height / rule.density.density
    assertTrue("alto $h dp (89 en el diseño; antes eran 115)", h in 86f..92f)
  }

  @Test
  fun con_letra_1_3_la_barra_crece_solo_lo_que_crece_el_rotulo() {
    show(AppTab.HOY, fontScale = 1.3f)
    val h = bounds("bottom_nav_bar").height / rule.density.density
    assertTrue("alto $h dp con letra 1,3 (95 en el diseño con el tope en 1,3; 92 si los cinco rótulos piden bajarlo a 1,2)", h in 91.5f..98f)
  }

  // ---- Rótulos: 16 sp o más y una sola línea, hasta con letra 2,0

  private fun rotulosDe16SpYUnaLinea(scale: Float, widthDp: Int = 393) {
    show(AppTab.PROGRESO, widthDp, if (widthDp == 360) 780 else 873, fontScale = scale)
    // Tarea 220: el tope es el MAYOR de 1,3 · 1,2 · 1,1 · 1,0 con que las cinco columnas quepan: a 360 dp queda en 1,1; a 393 dp, en 1,3 con las medidas de Fable (388 dp) y en 1,2 con las de Compose en un Pixel 8 (394,3 dp con 1,3 contra 393: 1,3 dp de más)
    val capMin = if (widthDp == 360) 1.1f else 1.2f
    val capMax = if (widthDp == 360) 1.1f else 1.3f
    for ((tag, t) in tabs) {
      val layout = layoutOf(t)
      assertTrue("«$t» a letra $scale mide ≥ 16 sp: ${layout.layoutInput.style.fontSize}", layout.layoutInput.style.fontSize.value >= 16f)
      assertTrue("«$t» a $widthDp dp con letra $scale tiene tope de letra entre $capMin y $capMax: la escala con que se dibuja es ${layout.layoutInput.density.fontScale}", layout.layoutInput.density.fontScale <= minOf(scale, capMax) + 1e-3f && (scale < capMin || layout.layoutInput.density.fontScale >= capMin - 1e-3f))
      assertEquals("«$t» a $widthDp dp con letra $scale es de una sola línea", 1, layout.lineCount)
      assertTrue("«$t» a letra $scale entera: no se corta a lo alto", !layout.didOverflowHeight)
      // (`didOverflowWidth` no sirve aquí: con softWrap apagado el párrafo mide el ancho disponible y el texto es más angosto)
      assertTrue("«$t» a $widthDp dp con letra $scale cabe en su columna: ${layout.size.width} px contra ${bounds(tag).width}", layout.size.width <= bounds(tag).width + 1f)
    }
  }

  @Test fun los_rotulos_son_de_16_sp_o_mas_y_de_una_linea_con_letra_1_0() = rotulosDe16SpYUnaLinea(1f)
  @Test fun los_rotulos_son_de_16_sp_o_mas_y_de_una_linea_con_letra_1_3() = rotulosDe16SpYUnaLinea(1.3f)
  @Test fun los_rotulos_son_de_16_sp_o_mas_y_de_una_linea_con_letra_2_0_topada() = rotulosDe16SpYUnaLinea(2f)
  @Test fun a_360_los_rotulos_son_de_una_linea_con_letra_1_0() = rotulosDe16SpYUnaLinea(1f, 360)
  @Test fun a_360_los_rotulos_son_de_una_linea_con_letra_1_3() = rotulosDe16SpYUnaLinea(1.3f, 360)
  @Test fun a_360_los_rotulos_son_de_una_linea_con_letra_2_0_topada() = rotulosDe16SpYUnaLinea(2f, 360)

  /**
   * Tarea 220 (`auditoria-fable/pestana-juegos-historial.md` §4.3): con CINCO pestañas, a 360 y 393 dp y con letra 1,0, 1,3 y 2,0, ninguna columna baja de 48 dp (el toque) y ningún rótulo pasa de su columna ni se parte, y las cinco llenan el ancho:
   * ni el aire de 12 dp ni el mínimo de 48 se tocan; lo que cede es el tope de la letra de los rótulos.
   */
  @Test
  fun con_cinco_pestanas_ninguna_columna_baja_de_48_dp_y_ningun_rotulo_pasa_de_su_columna_a_360_y_393_con_letra_1_0_1_3_y_2_0() {
    for (width in listOf(360, 393)) for (scale in listOf(1f, 1.3f, 2f)) {
      show(AppTab.HOY, width, if (width == 360) 780 else 873, fontScale = scale)
      val d = rule.density.density
      var sum = 0f
      for ((tag, label) in tabs) {
        val col = bounds(tag)
        sum += col.width / d
        assertTrue("$width dp, letra $scale: la columna «$label» mide ${col.width / d} dp (≥ 48 para el toque)", col.width / d >= 47.5f)
        assertTrue("$width dp, letra $scale: la columna «$label» mide ${col.height / d} dp de alto (≥ 48)", col.height / d >= 47.5f)
        val layout = layoutOf(label)
        assertEquals("$width dp, letra $scale: «$label» en una sola línea", 1, layout.lineCount)
        assertTrue("$width dp, letra $scale: «$label» (${layout.size.width / d} dp de texto en ${layout.layoutInput.density.fontScale}) no pasa de su columna (${col.width / d} dp)", layout.size.width <= col.width - 12f * d + 1f)
      }
      assertEquals("$width dp, letra $scale: las cinco llenan el ancho", width.toFloat(), sum, 1f)
    }
  }

  /** La regla pura del tope dinámico: el mayor de 1,3 · 1,2 · 1,1 · 1,0 con que las cinco quepan (a 393 queda en 1,3; a 360, en 1,1), nunca por debajo de 1,0. */
  @Test
  fun el_tope_de_la_letra_de_los_rotulos_es_el_mayor_de_1_3_1_2_1_1_1_0_con_que_las_cinco_quepan() {
    val base = fableLabels
    fun at(scale: Float) = base.map { it * scale }
    assertEquals("393 dp: caben con 1,3 (388 dp)", 1.3f, navTextScaleCap(2f, 393f, ::at), 0f)
    assertEquals("411 dp: también", 1.3f, navTextScaleCap(2f, 411f, ::at), 0f)
    assertEquals("360 dp con 1,3: no cabe (388), ni con 1,2 (368,8); con 1,1 sí (347,5)", 1.1f, navTextScaleCap(1.3f, 360f, ::at), 0f)
    assertEquals("360 dp con 2,0: igual", 1.1f, navTextScaleCap(2f, 360f, ::at), 0f)
    assertEquals("360 dp con letra normal: cabe sobrado y no hay nada que topar", 1.3f, navTextScaleCap(1f, 360f, ::at), 0f)
    assertEquals("un teléfono de 350 dp: con 1,1 ya no (347,5 + aire); queda 1,0", 1.0f, navTextScaleCap(2f, 340f, ::at), 0f)
    assertEquals("y si ni con 1,0 cupieran (331,5 > 320) devuelve 1,0, nunca menos", 1.0f, navTextScaleCap(2f, 320f, ::at), 0f)
    for (total in listOf(320f, 340f, 360f, 380f, 393f, 411f, 480f)) for (fontScale in listOf(1f, 1.15f, 1.3f, 2f)) {
      val cap = navTextScaleCap(fontScale, total, ::at)
      assertTrue("tope $cap a $total dp con letra $fontScale: entre 1,0 y 1,3", cap in 1.0f..1.3f)
      val need = at(minOf(fontScale, cap)).sumOf { maxOf(it + 12f, 48f).toDouble() }
      assertTrue("a $total dp con letra $fontScale y tope $cap las cinco necesitan %.1f dp".format(need), need <= total || cap == 1.0f)
    }
  }

  @Test
  fun con_letra_2_0_la_barra_no_crece_mas_que_con_1_3_porque_los_rotulos_estan_topados() {
    show(AppTab.PROGRESO, fontScale = 1.3f)
    val h13 = bounds("bottom_nav_bar").height / rule.density.density
    show(AppTab.PROGRESO, fontScale = 2f)
    val h20 = bounds("bottom_nav_bar").height / rule.density.density
    assertEquals("la barra mide lo mismo con 1,3 que con 2,0 (%.1f y %.1f dp)".format(h13, h20), h13, h20, 1f)
    assertTrue("y más que a letra normal (88 dp)", h20 > 90f)
  }

  @Test
  fun con_letra_2_0_el_rotulo_se_dibuja_a_19_2_o_20_8_sp_y_no_a_32() {
    show(AppTab.PROGRESO, fontScale = 2f)
    val layout = layoutOf("Contrarreloj")
    val drawn = layout.layoutInput.style.fontSize.value * layout.layoutInput.density.fontScale
    assertTrue("16 sp × 1,2 o × 1,3 según lo que midan los cinco rótulos a 393 dp: se dibuja a $drawn sp", drawn in 19.1f..20.9f)
  }

  // ---- Capturas de las cinco pestañas

  @Test fun captura_juegos_393() { show(AppTab.JUEGOS); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-juegos-393.png") }
  @Test fun captura_juegos_360() { show(AppTab.JUEGOS, 360, 780); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-juegos-360.png") }
  @Test fun captura_juegos_360_letra_1_3() { show(AppTab.JUEGOS, 360, 780, fontScale = 1.3f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-juegos-360-letra13.png") }
  @Test fun captura_juegos_393_letra_1_3() { show(AppTab.JUEGOS, fontScale = 1.3f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-juegos-393-letra13.png") }
  @Test fun captura_hoy_393() { show(AppTab.HOY); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-393.png") }
  @Test fun captura_contrarreloj_393() { show(AppTab.CONTRARRELOJ); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-contrarreloj-393.png") }
  @Test fun captura_avance_393() { show(AppTab.PROGRESO); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-avance-393.png") }
  @Test fun captura_tu_393() { show(AppTab.TU); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-tu-393.png") }
  @Test fun captura_hoy_360() { show(AppTab.HOY, 360, 780); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-360.png") }
  @Test fun captura_contrarreloj_360() { show(AppTab.CONTRARRELOJ, 360, 780); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-contrarreloj-360.png") }
  @Test fun captura_hoy_360_letra_1_3() { show(AppTab.HOY, 360, 780, fontScale = 1.3f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-360-letra13.png") }
  @Test fun captura_contrarreloj_360_letra_1_3() { show(AppTab.CONTRARRELOJ, 360, 780, fontScale = 1.3f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-contrarreloj-360-letra13.png") }
  @Test fun captura_avance_360() { show(AppTab.PROGRESO, 360, 780); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-avance-360.png") }
  @Test fun captura_tu_360() { show(AppTab.TU, 360, 780); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-tu-360.png") }
  @Test fun captura_hoy_411() { show(AppTab.HOY, 411, 914); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-411.png") }
  @Test fun captura_hoy_letra_1_3() { show(AppTab.HOY, fontScale = 1.3f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-letra13.png") }
  @Test fun captura_hoy_letra_2_0() { show(AppTab.HOY, fontScale = 2f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-letra20.png") }
  @Test fun captura_avance_letra_2_0() { show(AppTab.PROGRESO, fontScale = 2f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-avance-letra20.png") }
  @Test fun captura_tu_letra_2_0() { show(AppTab.TU, fontScale = 2f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-tu-letra20.png") }
}
