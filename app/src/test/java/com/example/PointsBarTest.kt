package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.ui.components.PointsBar
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La barra de puntos (Tarea 230, 4-nov; antes la escala de las 5 etapas, Tarea 143): UN solo dibujo para la página de un área de Avance y la ficha de un juego, que ya no lleva las marcas de las etapas ni los nombres Inicio · Aprendiz · Hábil · Experto · Maestro.
 * Con la letra del sistema normal, al 130 % y al 200 %, y en una pantalla angosta, la barra se ve entera, del ancho que le dan, sin texto que se corte (ya no tiene texto).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class PointsBarTest {
  @get:Rule val rule = createComposeRule()

  // UNA sola composición por prueba (Compose no deja llamar a setContent dos veces): el ancho, la letra, el fondo y el valor son estado y cada `show` los cambia
  private var widthDp by mutableStateOf(393)
  private var scale by mutableStateOf(1f)
  private var card by mutableStateOf(true)
  private var measure by mutableStateOf<Float?>(0.47f)

  @Before
  fun setUp() {
    rule.setContent {
      CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, scale)) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).background(if (card) Color(0xFFFFF8EC) else Color(0xFF0B0C33)).padding(horizontal = 16.dp)) {
            PointsBar(measure, card, change = 0.05f)
          }
        }
      }
    }
  }

  private fun show(widthDp: Int, fontScale: Float, onCard: Boolean, value: Float? = 0.47f) {
    rule.runOnIdle { this.widthDp = widthDp; scale = fontScale; card = onCard; measure = value }
    rule.waitForIdle()
  }

  private fun barWidthPx(): Int = rule.onNodeWithTag("points_bar").fetchSemanticsNode().size.width

  @Test
  fun la_barra_se_ve_entera_con_cualquier_letra_y_en_telefonos_angostos() {
    for (fontScale in listOf(1f, 1.3f, 2f)) for (onCard in listOf(true, false)) for (width in listOf(393, 360, 320)) {
      show(width, fontScale, onCard)
      rule.onNodeWithTag("points_bar").assertIsDisplayed()
      assertTrue("la barra ocupa el ancho que le dan ($width dp, letra $fontScale): ${barWidthPx()} px", barWidthPx() > 0)
    }
  }

  @Test
  fun sin_medir_se_ve_el_canal_y_con_medida_tambien() {
    show(393, 1.3f, onCard = true, value = null)
    rule.onNodeWithTag("points_bar").assertIsDisplayed()
    show(393, 1.3f, onCard = false, value = 0.47f)
    rule.onNodeWithTag("points_bar").assertIsDisplayed()
  }

  @Test
  fun la_barra_es_la_misma_en_la_pagina_del_area_y_en_la_ficha_y_no_dice_etapa() {
    // Tarea 143, ítem 12: tres dibujos distintos pasaron a uno solo; Tarea 230: ese uno ya no lleva etapas
    val read = { path: String -> java.io.File("src/main/java/com/example/ui/$path").readText() }
    assertTrue(read("screens/AvanceAreaPage.kt").contains("PointsBar("))
    assertTrue(read("screens/GameSheet.kt").contains("PointsBar("))
    assertFalse("StageScale.kt ya no existe", java.io.File("src/main/java/com/example/ui/components/StageScale.kt").exists())
    val bar = read("components/PointsBar.kt").lines().filterNot { it.trim().startsWith("*") || it.trim().startsWith("/*") || it.trim().startsWith("//") }.joinToString("\n")
    assertFalse("la barra no dibuja texto", bar.contains("Text("))
    assertFalse("ni nombra etapas", bar.lowercase().contains("etapa"))
  }
}
