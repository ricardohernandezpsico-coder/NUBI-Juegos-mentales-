package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.ui.components.Glyph
import com.example.ui.components.LocalReduceMotion
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Assume
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La lámina de la Tarea 157 («Íconos de la barra que se reconozcan»; `docs/previews/tarea-157-iconos.png`, `tools/previews/tarea_157_iconos.py`): la maqueta de la barra con tres estilos de ícono (`NavIconosPreview.kt`).
 * Lo que SIEMPRE corre: las rutas vectoriales se leen y arman, el dibujo ENTERO de cada ícono vectorial se ve de 28 dp o más, y la maqueta mide lo mismo que la barra real (89 dp; 95 con letra 1,3) y cada pestaña toca ≥ 48 dp.
 * Lo que solo corre al grabar capturas (`./gradlew :app:recordRoborazziDebug --tests "*NavIconosPreviewTest*"`): las tomas de la barra de cada estilo con cada pestaña elegida, a letra 1,0 y 1,3 (393 dp de ancho, 2,75 px por dp, como `AppActualScreenshotTest`), y los mosaicos de cada metáfora; quedan en `src/test/screenshots/nav-iconos/`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class NavIconosPreviewTest {
  @get:Rule val rule = createComposeRule()

  private var style by mutableStateOf(NavStyle.ACTUAL)
  private var tab by mutableStateOf(AppTab.HOY)
  private var fontScale by mutableStateOf(1f)
  private var tile by mutableStateOf<NavIcono?>(null)
  private var tileSelected by mutableStateOf(false)

  private fun setUp() {
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          val t = tile
          if (t == null) Box(Modifier.width(393.dp)) { PreviewNavBar(style, tab) }
          else Box(Modifier.width(131.dp).background(Color(0xFF29276A)).testTag("tile")) {
            val juegos = t == NavIcono.COHETE || t == NavIcono.FICHAS || t == NavIcono.MANDO
            PreviewNavItem(style, if (juegos) "Juegos" else "Avance", Glyph.COHETE, t, "obj_juegos", tileSelected, Modifier.width(131.dp))
          }
        }
      }
    }
    rule.waitForIdle()
  }

  // ---- Siempre corre

  @Test
  fun las_rutas_se_leen_y_los_vectores_se_arman() {
    for (icono in NavIcono.values()) for (s in listOf(NavStyle.LINEA, NavStyle.DOS_TONOS)) {
      val v = icono.vector(s)
      assertTrue("${icono.key} en ${s.key} tiene trazos", v.root.size >= icono.shapes.size)
      for (shape in icono.shapes) assertTrue("la ruta de ${icono.key} se lee: ${shape.d}", androidx.compose.ui.graphics.vector.PathParser().parsePathString(shape.d).toNodes().size >= 2)
    }
  }

  @Test
  fun el_dibujo_entero_de_cada_icono_vectorial_se_ve_de_28_dp_o_mas() {
    for (icono in NavIcono.values()) for (s in listOf(NavStyle.LINEA, NavStyle.DOS_TONOS)) {
      val (w, h) = icono.visibleDp(s)
      assertTrue("${icono.key} en ${s.key} mide ${"%.1f".format(w)} × ${"%.1f".format(h)} dp: el lado mayor debe ser ≥ 28 dp", maxOf(w, h) >= 28f)
      assertTrue("${icono.key} en ${s.key} cabe en el disco de 48 dp (ancho ${"%.1f".format(w)}, alto ${"%.1f".format(h)})", w <= 40f && h <= 40f)
    }
  }

  private fun medidas(scale: Float, alto: ClosedFloatingPointRange<Float>) {
    fontScale = scale
    setUp()
    val d = rule.density.density
    for (s in NavStyle.values()) {
      style = s; tab = AppTab.JUEGOS
      rule.waitForIdle()
      val bar = rule.onNodeWithTag("bottom_nav_bar").fetchSemanticsNode().boundsInRoot
      assertTrue("la maqueta de ${s.key} a letra $scale mide ${bar.height / d} dp (esperado $alto)", bar.height / d in alto)
      for (t in listOf("nav_item_hoy", "nav_item_juegos", "nav_item_progreso")) {
        val b = rule.onNodeWithTag(t).fetchSemanticsNode().boundsInRoot
        assertTrue("$t en ${s.key} toca ≥ 48 dp: ${b.width / d} × ${b.height / d}", b.width >= 48 * d && b.height >= 48 * d)
        assertEquals("$t es un tercio del ancho", 393f / 3f, b.width / d, 1f)
      }
    }
  }

  @Test fun la_maqueta_mide_lo_mismo_que_la_barra_real_a_letra_1_0() = medidas(1f, 86f..92f)
  @Test fun la_maqueta_mide_lo_mismo_que_la_barra_real_a_letra_1_3() = medidas(1.3f, 92f..98f)

  // ---- Solo al grabar capturas

  @Test
  fun captura_lamina() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    setUp()
    val dir = "src/test/screenshots/nav-iconos"
    val tabs = listOf(AppTab.HOY to "hoy", AppTab.JUEGOS to "juegos", AppTab.PROGRESO to "avance")
    for (scale in listOf(1f to "", 1.3f to "-letra13")) {
      rule.runOnIdle { fontScale = scale.first; tile = null }
      for (s in NavStyle.values()) for ((t, name) in tabs) {
        rule.runOnIdle { style = s; tab = t }
        rule.waitForIdle()
        rule.onNodeWithTag("bottom_nav_bar").captureRoboImage(filePath = "$dir/barra-${s.key}-$name${scale.second}.png")
      }
    }
    rule.runOnIdle { fontScale = 1f }
    for (s in listOf(NavStyle.LINEA, NavStyle.DOS_TONOS)) for (icono in NavIcono.values()) for (sel in listOf(false, true)) {
      rule.runOnIdle { style = s; tile = icono; tileSelected = sel }
      rule.waitForIdle()
      rule.onNodeWithTag("tile").captureRoboImage(filePath = "$dir/tile-${s.key}-${icono.key}-${if (sel) "sel" else "off"}.png")
    }
  }
}
