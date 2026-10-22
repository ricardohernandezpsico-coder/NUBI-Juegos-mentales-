package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
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
 * La barra inferior del kit (Tarea 156, opción C de Fable): tres discos crema de 48 dp con un ícono vectorial de línea de tinta (isla, barras, persona; Tarea 158: Hoy · Avance · Tú), su rótulo de 16 sp debajo y la pestaña elegida sobre una losa sol, con el rótulo dentro, en tinta y negrita.
 * Se prueba que cada pestaña sea `Role.Tab` con `selected` y su nombre, que ocupe toda su columna (un tercio del ancho y todo el alto de la barra: ≥ 48 dp en los dos ejes), que la barra y sus rótulos queden DENTRO de la pantalla y sobre el inset de navegación
 * (48 dp, como con 3 botones), que a 393 dp mida ~89 dp (95 con letra 1,3), que los rótulos sean de 16 sp o más y de UNA línea hasta con letra 2,0, que SOLO la elegida lleve la losa, y que con «quitar animaciones» la elegida cambie de golpe; y se capturan las tres pestañas.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class NeuroNavBarTest {
  @get:Rule val rule = createComposeRule()

  private var selected: AppTab? = null
  private var shown by mutableStateOf(AppTab.HOY)

  private fun show(current: AppTab, widthDp: Int = 393, heightDp: Int = 873, fontScale: Float = 1f, reduce: Boolean = false) {
    shown = current
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides reduce) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            // 48 dp de la barra de navegación del teléfono (3 botones) bajo la nuestra
            Column(Modifier.align(Alignment.BottomCenter).padding(bottom = 48.dp)) { NeuroNavBar(current = shown, onSelect = { selected = it }) }
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun bounds(tag: String): Rect = rule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot

  private val tabs = listOf("nav_item_hoy" to "Hoy", "nav_item_progreso" to "Avance", "nav_item_tu" to "Tú")

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
    rule.onNodeWithTag("nav_item_tu").assertIsNotSelected()
  }

  @Test
  fun tocar_una_pestana_la_elige() {
    show(AppTab.HOY)
    rule.onNodeWithTag("nav_item_tu").performClick()
    assertEquals(AppTab.TU, selected)
    rule.onNodeWithTag("nav_item_progreso").performClick()
    assertEquals(AppTab.PROGRESO, selected)
  }

  @Test
  fun las_tres_etiquetas_son_Hoy_Avance_y_Tu() {
    show(AppTab.TU)
    for (t in listOf("Hoy", "Avance", "Tú")) rule.onNodeWithText(t, useUnmergedTree = true).assertIsDisplayed()
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
    shown = AppTab.PROGRESO
    rule.waitForIdle()
    rule.onNodeWithTag("nav_slab_hoy", useUnmergedTree = true).assertDoesNotExist()
    rule.onNodeWithTag("nav_slab_progreso", useUnmergedTree = true).assertExists()
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

  @Test
  fun las_tres_columnas_son_tercios_parejos_y_cada_rotulo_va_centrado_en_la_suya() {
    show(AppTab.HOY)
    val density = rule.density.density
    val third = 393f / 3f
    for ((i, pair) in tabs.withIndex()) {
      val col = bounds(pair.first)
      assertEquals("${pair.first} mide un tercio del ancho", third, col.width / density, 1f)
      val label = rule.onNodeWithText(pair.second, useUnmergedTree = true).fetchSemanticsNode().boundsInRoot
      assertEquals("«${pair.second}» centrada en su columna", col.center.x, label.center.x, 1.5f * density)
      assertEquals("la columna $i empieza donde toca", third * i, col.left / density, 1f)
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
    assertTrue("alto $h dp con letra 1,3 (95 en el diseño)", h in 92f..98f)
  }

  // ---- Rótulos: 16 sp o más y una sola línea, hasta con letra 2,0

  private fun rotulosDe16SpYUnaLinea(scale: Float) {
    show(AppTab.PROGRESO, fontScale = scale)
    for ((tag, t) in tabs) {
      val layout = layoutOf(t)
      assertTrue("«$t» a letra $scale mide ≥ 16 sp: ${layout.layoutInput.style.fontSize}", layout.layoutInput.style.fontSize.value >= 16f)
      assertEquals("«$t» a letra $scale es de una sola línea", 1, layout.lineCount)
      assertTrue("«$t» a letra $scale entera: no se corta a lo alto", !layout.didOverflowHeight)
      // (`didOverflowWidth` no sirve aquí: con softWrap apagado el párrafo mide el ancho disponible y el texto es más angosto)
      assertTrue("«$t» a letra $scale cabe en su columna: ${layout.size.width} px contra ${bounds(tag).width}", layout.size.width <= bounds(tag).width + 1f)
    }
  }

  @Test fun los_rotulos_son_de_16_sp_o_mas_y_de_una_linea_con_letra_1_0() = rotulosDe16SpYUnaLinea(1f)
  @Test fun los_rotulos_son_de_16_sp_o_mas_y_de_una_linea_con_letra_1_3() = rotulosDe16SpYUnaLinea(1.3f)
  @Test fun los_rotulos_son_de_16_sp_o_mas_y_de_una_linea_con_letra_2_0() = rotulosDe16SpYUnaLinea(2f)

  @Test
  fun con_letra_2_0_la_barra_es_mas_alta_que_a_letra_normal() {
    show(AppTab.PROGRESO, fontScale = 2f)
    assertTrue("a letra 2,0 la barra crece", bounds("bottom_nav_bar").height / rule.density.density > 100f)
  }

  // ---- Capturas de las tres pestañas

  @Test fun captura_hoy_393() { show(AppTab.HOY); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-393.png") }
  @Test fun captura_avance_393() { show(AppTab.PROGRESO); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-avance-393.png") }
  @Test fun captura_tu_393() { show(AppTab.TU); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-tu-393.png") }
  @Test fun captura_hoy_360() { show(AppTab.HOY, 360, 780); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-360.png") }
  @Test fun captura_avance_360() { show(AppTab.PROGRESO, 360, 780); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-avance-360.png") }
  @Test fun captura_tu_360() { show(AppTab.TU, 360, 780); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-tu-360.png") }
  @Test fun captura_hoy_411() { show(AppTab.HOY, 411, 914); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-411.png") }
  @Test fun captura_hoy_letra_1_3() { show(AppTab.HOY, fontScale = 1.3f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-letra13.png") }
  @Test fun captura_hoy_letra_2_0() { show(AppTab.HOY, fontScale = 2f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-letra20.png") }
  @Test fun captura_avance_letra_2_0() { show(AppTab.PROGRESO, fontScale = 2f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-avance-letra20.png") }
  @Test fun captura_tu_letra_2_0() { show(AppTab.TU, fontScale = 2f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-tu-letra20.png") }
}
