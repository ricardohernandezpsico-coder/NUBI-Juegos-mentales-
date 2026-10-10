package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.dp
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.model.AgeBand
import com.example.model.GameRegistry
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.cardData
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La ficha del juego en su PEOR caso (Tarea 73, 10-oct): un juego con medida, gráfico de la serie y reloj, y con Experto bloqueado (Rescate relámpago). En el Motorola de Ricardo la ficha quedaba más alta que la pantalla
 * y «Jugar» caía bajo la barra de navegación. Ahora la tarjeta mide como máximo el alto que le deja la ventana (dentro de las barras del sistema), lo demás hace scroll por dentro y «Jugar» va fijo abajo, siempre a la vista.
 * Se prueba en 360 × 780 (el botón entero a la vista) y en 360 × 640 (un teléfono bajo: el botón visible y el resto con scroll).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class GameSheetFitScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private val day = 86_400_000L
  private val now = 400 * day + 3_600_000L

  private fun show(heightDp: Int, shot: String) {
    val radar = GameRegistry.getById("radar")!!
    val points = listOf(260f, 240f, 250f, 210f, 190f, 170f).mapIndexed { i, v -> MeasurePoint(now - (6 - i) * day, "glance", v, 0.5f, true) }
    val data = cardData(radar, radar.title, now - day, now, 0.55f, points, playsLast14 = 6)
    composeTestRule.setContent {
      NeuroVidaTheme {
        // lo que hace la ventana de la ficha: un marco del alto disponible (ya sin las barras del sistema), con su margen, y la tarjeta centrada
        Box(Modifier.width(360.dp).height(heightDp.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
          Box(Modifier.fillMaxWidth()) {
            GameSheetContent(data, AgeBand.SENIOR, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = true, onChooseTimed = {})
          }
        }
      }
    }
    composeTestRule.waitForIdle()
    composeTestRule.onNodeWithTag("frame").captureRoboImage(filePath = "src/test/screenshots/$shot.png")
  }

  private fun assertPlayButtonInsideFrame() {
    composeTestRule.onNodeWithTag("sheet_play").assertIsDisplayed()          // sin tener que hacer scroll
    val frame = composeTestRule.onNodeWithTag("frame").fetchSemanticsNode().boundsInRoot
    val play = composeTestRule.onNodeWithTag("sheet_play").fetchSemanticsNode().boundsInRoot
    val card = composeTestRule.onNodeWithTag("game_sheet").fetchSemanticsNode().boundsInRoot
    assertTrue("el botón «Jugar» (abajo en ${play.bottom}) cabe en el marco (abajo en ${frame.bottom})", play.bottom <= frame.bottom + 0.5f)
    assertTrue("la tarjeta (arriba en ${card.top}, abajo en ${card.bottom}) cabe en el marco (${frame.top} a ${frame.bottom})", card.top >= frame.top - 0.5f && card.bottom <= frame.bottom + 0.5f)
  }

  @Test
  fun ficha_de_rescate_en_360_por_780() {
    show(780, "ficha-peor-caso-360x780")
    assertPlayButtonInsideFrame()
  }

  @Test
  fun ficha_de_rescate_en_360_por_640_con_scroll_y_el_boton_a_la_vista() {
    show(640, "ficha-peor-caso-360x640")
    assertPlayButtonInsideFrame()
    // el resto del contenido sigue ahí, alcanzable con scroll (la última fila del reloj)
    composeTestRule.onNodeWithTag("reto_con").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_play").assertIsDisplayed()
  }
}
