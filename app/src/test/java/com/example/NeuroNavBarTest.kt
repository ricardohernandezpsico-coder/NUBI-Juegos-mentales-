package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.assertIsSelected
import androidx.compose.ui.test.assertIsNotSelected
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
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
 * La barra inferior de plasticina (Tarea 86): una bandeja con tres cubos (isla, cohete, farol) y su etiqueta debajo. Se prueba que cada pestaña sea `Role.Tab` con `selected` y su nombre, que ocupe toda su columna (≥ 56 dp),
 * que la barra y sus etiquetas queden DENTRO de la pantalla y sobre el inset de navegación (48 dp, como con 3 botones), que a 393 dp mida ~113-120 dp con sus etiquetas, que con letra 2,0 no se corte nada, y se capturan las tres pestañas a 393, 360, 411 y con letra 2,0.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class NeuroNavBarTest {
  @get:Rule val rule = createComposeRule()

  private var selected: AppTab? = null

  private fun show(current: AppTab, widthDp: Int = 393, heightDp: Int = 873, fontScale: Float = 1f, reduce: Boolean = false) {
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides reduce) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            // 48 dp de la barra de navegación del teléfono (3 botones) bajo la nuestra
            Column(Modifier.align(Alignment.BottomCenter).padding(bottom = 48.dp)) { NeuroNavBar(current = current, onSelect = { selected = it }) }
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun bounds(tag: String): Rect = rule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot

  // ---- Semántica

  @Test
  fun cada_pestana_es_Role_Tab_con_su_nombre_y_solo_la_elegida_esta_selected() {
    show(AppTab.JUEGOS)
    for ((tag, name) in listOf("nav_item_hoy" to "Hoy", "nav_item_juegos" to "Juegos", "nav_item_progreso" to "Avance")) {
      val node = rule.onNodeWithTag(tag).fetchSemanticsNode()
      assertEquals("$tag es una pestaña", Role.Tab, node.config[SemanticsProperties.Role])
      assertEquals("$tag se llama $name", listOf(name), node.config[SemanticsProperties.ContentDescription])
    }
    rule.onNodeWithTag("nav_item_juegos").assertIsSelected()
    rule.onNodeWithTag("nav_item_hoy").assertIsNotSelected()
    rule.onNodeWithTag("nav_item_progreso").assertIsNotSelected()
  }

  @Test
  fun tocar_una_pestana_la_elige() {
    show(AppTab.HOY)
    rule.onNodeWithTag("nav_item_progreso").performClick()
    assertEquals(AppTab.PROGRESO, selected)
    rule.onNodeWithTag("nav_item_juegos").performClick()
    assertEquals(AppTab.JUEGOS, selected)
  }

  @Test
  fun sin_el_cerebro_las_tres_etiquetas_son_Hoy_Juegos_y_Avance() {
    show(AppTab.PROGRESO)
    for (t in listOf("Hoy", "Juegos", "Avance")) rule.onNodeWithText(t, useUnmergedTree = true).assertIsDisplayed()
  }

  // ---- Medidas: dentro de la pantalla, sobre el inset, toque amplio

  private fun assertFitsAboveNavigation(widthDp: Int, heightDp: Int) {
    val density = rule.density.density
    val root = rule.onRoot().fetchSemanticsNode().boundsInRoot
    val bar = bounds("bottom_nav_bar")
    assertTrue("la barra queda dentro de la pantalla: $bar", bar.left >= -1f && bar.right <= widthDp * density + 1f && bar.top >= 0f)
    assertTrue("y sobre los 48 dp de la barra del teléfono: ${bar.bottom} contra ${root.bottom - 48 * density}", bar.bottom <= root.bottom - 48 * density + 1f)
    for (tag in listOf("nav_item_hoy", "nav_item_juegos", "nav_item_progreso")) {
      val b = bounds(tag)
      assertTrue("$tag mide al menos 56 dp de ancho: ${b.width / density}", b.width >= 56 * density)
      assertTrue("$tag cabe en la barra: $b contra $bar", b.top >= bar.top - 1f && b.bottom <= bar.bottom + 1f && b.left >= -1f && b.right <= widthDp * density + 1f)
    }
    for (t in listOf("Hoy", "Juegos", "Avance")) {
      val label = rule.onNodeWithText(t, useUnmergedTree = true).fetchSemanticsNode().boundsInRoot
      assertTrue("la etiqueta «$t» queda dentro de la barra: $label contra $bar", label.bottom <= bar.bottom + 1f && label.top >= bar.top)
    }
  }

  @Test fun la_barra_cabe_sobre_el_inset_a_393() { show(AppTab.HOY); assertFitsAboveNavigation(393, 873) }
  @Test fun la_barra_cabe_sobre_el_inset_a_360() { show(AppTab.JUEGOS, 360, 780); assertFitsAboveNavigation(360, 780) }
  @Test fun la_barra_cabe_sobre_el_inset_a_411() { show(AppTab.PROGRESO, 411, 914); assertFitsAboveNavigation(411, 914) }
  @Test fun la_barra_cabe_con_letra_1_3() { show(AppTab.HOY, fontScale = 1.3f); assertFitsAboveNavigation(393, 873) }
  @Test fun la_barra_cabe_con_letra_2_0_y_crece_en_vez_de_cortar() { show(AppTab.HOY, fontScale = 2f); assertFitsAboveNavigation(393, 873) }

  @Test
  fun cada_etiqueta_va_centrada_bajo_su_cubo() {
    show(AppTab.HOY)
    val density = rule.density.density
    // los cubos del compuesto aprobado están en x = 181, 393 y 605 px de 786 (90,5; 196,5 y 302,5 dp a 393 dp de ancho)
    for ((t, cubeDp) in listOf("Hoy" to 90.5f, "Juegos" to 196.5f, "Avance" to 302.5f)) {
      val label = rule.onNodeWithText(t, useUnmergedTree = true).fetchSemanticsNode().boundsInRoot
      assertEquals("«$t» centrada bajo su cubo", cubeDp, label.center.x / density, 1.5f)
    }
  }

  @Test
  fun a_393_dp_la_barra_con_etiquetas_mide_entre_113_y_120_dp() {
    show(AppTab.HOY)
    val h = bounds("bottom_nav_bar").height / rule.density.density
    assertTrue("alto $h dp (con las etiquetas, 113-117 en el diseño)", h in 113f..120f)
  }

  @Test
  fun con_letra_2_0_la_etiqueta_no_se_corta_a_lo_alto() {
    show(AppTab.JUEGOS, fontScale = 2f)
    for (t in listOf("Hoy", "Juegos", "Avance")) {
      val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
      rule.onNodeWithText(t, useUnmergedTree = true).fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsActions.GetTextLayoutResult].action?.invoke(results)
      assertTrue("«$t» entera", results.isNotEmpty() && !results.first().didOverflowHeight)
    }
    val normal = 117f
    assertTrue("a letra 2,0 la barra es más alta que a letra normal", bounds("bottom_nav_bar").height / rule.density.density > normal)
  }

  @Test
  fun con_quitar_animaciones_la_barra_se_arma_igual() {
    show(AppTab.PROGRESO, reduce = true)
  }

  // ---- Capturas de las tres pestañas

  @Test fun captura_hoy_393() { show(AppTab.HOY); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-393.png") }
  @Test fun captura_juegos_393() { show(AppTab.JUEGOS); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-juegos-393.png") }
  @Test fun captura_avance_393() { show(AppTab.PROGRESO); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-avance-393.png") }
  @Test fun captura_hoy_360() { show(AppTab.HOY, 360, 780); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-360.png") }
  @Test fun captura_juegos_360() { show(AppTab.JUEGOS, 360, 780); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-juegos-360.png") }
  @Test fun captura_avance_360() { show(AppTab.PROGRESO, 360, 780); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-avance-360.png") }
  @Test fun captura_hoy_411() { show(AppTab.HOY, 411, 914); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-411.png") }
  @Test fun captura_hoy_letra_2_0() { show(AppTab.HOY, fontScale = 2f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-hoy-letra20.png") }
  @Test fun captura_juegos_letra_2_0() { show(AppTab.JUEGOS, fontScale = 2f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-juegos-letra20.png") }
  @Test fun captura_avance_letra_2_0() { show(AppTab.PROGRESO, fontScale = 2f); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/barra-avance-letra20.png") }
}
