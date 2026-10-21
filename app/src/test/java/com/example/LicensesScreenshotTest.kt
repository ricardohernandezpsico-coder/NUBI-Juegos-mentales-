package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.assertTextContains
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import org.junit.Assert.assertTrue
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.unit.dp
import com.example.ui.screens.LicensesScreen
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/** Ajustes → "Licencias y créditos" (SPALEX, tipografías y Unity), para verla sin teléfono. */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class LicensesScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  @Test
  fun licencias() {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          LicensesScreen(onClose = {})
        }
      }
    }
    composeTestRule.onNodeWithTag("licenses_content").assertIsDisplayed()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/licencias.png")
  }

  /**
   * Los créditos de las palabras (10-oct, Tarea 79): SPALEX (CC BY 4.0) cubre a los TRES juegos que lo usan (Lluvia de meteoros, Cosecha de palabras y En la punta de la lengua), con la cita completa y el cambio hecho en cada uno,
   * y el diccionario Hunspell es_ES va con su licencia real: GPL 3 o posterior, LGPL 3 o posterior o MPL 1.1 o posterior, a elección (LICENSE.md de LibreOffice/dictionaries).
   */
  @Test
  fun los_creditos_de_las_palabras_cubren_a_los_tres_juegos_con_la_licencia_de_cada_fuente() {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          LicensesScreen(onClose = {})
        }
      }
    }
    fun shown(text: String) = composeTestRule.onAllNodesWithText(text, substring = true).fetchSemanticsNodes().isNotEmpty()
    for (t in listOf(
      "Lluvia de meteoros y En la punta de la lengua: SPALEX",
      "Aguasvivas, J. A., Carreiras, M., Brysbaert, M., Mandera, P., Keuleers, E. y Duñabeitia, J. A. (2018)",
      "CC BY 4.0",
      "En En la punta de la lengua solo entran palabras que figuran en SPALEX",
      "Esa frecuencia de uso es la de EsPal y llega a la app a través de SPALEX",
      "Duchon, A., Perea, M., Sebastián-Gallés, N., Martí, A. y Carreiras, M. (2013)",
      "EsPal: One-stop shopping for Spanish word properties. Behavior Research Methods, 45, 1246-1258",
      "Diccionario: Hunspell es_ES de LibreOffice",
      "GNU GPL 3 o posterior, GNU LGPL 3 o posterior, o MPL 1.1 o posterior",
      "no el diccionario"
    )) assertTrue("falta en los créditos: $t", shown(t))
    // de EsPal SOLO se cita: ninguna licencia que no esté confirmada (decisión de Ricardo, Tarea 80)
    val espal = composeTestRule.onAllNodesWithText("Esa frecuencia de uso es la de EsPal", substring = true).fetchSemanticsNodes().single()
      .config[androidx.compose.ui.semantics.SemanticsProperties.Text].joinToString("") { it.text }.lowercase()
    for (claim in listOf("licencia", "cc by", "gpl", "mpl", "libre", "gratis", "permiso")) assertTrue("EsPal no lleva «$claim»", !espal.contains(claim))
    // la licencia vieja, imprecisa, ya no está
    assertTrue(!shown("licencias MPL 1.1, LGPL y GPL"))
  }

  /** El texto completo de la SIL OFL, desplegado (se lee de res/raw/ofl.txt). */
  @Test
  fun licenciaCompletaDeLasTipografias() {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          LicensesScreen(onClose = {})
        }
      }
    }
    composeTestRule.onNodeWithTag("btn_ofl_text").performScrollTo().performClick()
    composeTestRule.onNodeWithTag("ofl_text").assertExists()
    composeTestRule.onNodeWithTag("ofl_text").assertTextContains("SIL OPEN FONT LICENSE", substring = true)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/licencias-ofl.png")
  }
}
