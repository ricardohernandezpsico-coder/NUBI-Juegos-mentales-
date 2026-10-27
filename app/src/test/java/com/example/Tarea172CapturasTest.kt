package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.model.AgeBand
import com.example.model.GameRegistry
import com.example.ui.screens.GameCardData
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.cardData
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assume
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File

/**
 * Las capturas de «Portadas que se entienden» (Tarea 172, `docs/previews/tarea-172.png`): las 17 fichas con su portada nueva (la ventanilla de 180 dp), Piloto (con Reto) y Correo (sin Reto) plegadas con letra 1,0 y 1,3, y lo que MIDE la ficha plegada
 * (alto de la tarjeta y lo que sobra del alto disponible) con las dos letras, que va a `medidas.txt`. A 393 × 873 dp menos las barras del sistema (24 arriba, 48 abajo) y con el margen de la ventana, como en la ventana real; datos fijos.
 * No corre en las pruebas de siempre: solo al grabar capturas (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea172CapturasTest*"`) y deja los PNG en `src/test/screenshots/tarea-172/`; `tools/previews/tarea_172.py` arma la lámina.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class Tarea172CapturasTest {
  @get:Rule val rule = createComposeRule()

  @Before fun solo_al_grabar() { Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true") }

  private val day = 86_400_000L
  private val now = 400 * day + 3_600_000L
  private val shots = "src/test/screenshots/tarea-172"
  private val frameHeightDp = 873f - 72f                                  // lo que dejan las barras del sistema (24 arriba y 48 abajo)

  private fun series(key: String, vararg values: Float) = values.mapIndexed { i, v -> MeasurePoint(now - (values.size - i) * day, key, v, 0.5f, true) }

  private fun piloto(): GameCardData {
    val g = GameRegistry.getById("piloto")!!
    return cardData(g, g.title, 0.92f)
  }

  private fun correo(): GameCardData {
    val g = GameRegistry.getById("correo")!!
    return cardData(g, g.title, 0.48f)
  }

  private fun shot(data: GameCardData, name: String, fontScale: Float = 1f, timed: Boolean = true) {
    rule.mainClock.autoAdvance = false
    rule.setContent {
      CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, fontScale)) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(frameHeightDp.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
            Box(Modifier.fillMaxWidth()) { GameSheetContent(data, AgeBand.ADULT, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = timed, onChooseTimed = {}) }
          }
        }
      }
    }
    repeat(3) { rule.mainClock.advanceTimeBy(600); rule.waitForIdle() }
    rule.onNodeWithTag("frame").captureRoboImage(filePath = "$shots/$name.png")
    val card = rule.onNodeWithTag("game_sheet").fetchSemanticsNode().boundsInRoot
    val dp = rule.density.density
    val cover = rule.onNodeWithTag("sheet_cover").fetchSemanticsNode().boundsInRoot
    val about = rule.onNodeWithTag("sheet_about").fetchSemanticsNode().boundsInRoot
    val line = "$name: la tarjeta mide ${"%.0f".format(card.height / dp)} dp de ${"%.0f".format(frameHeightDp - 24)} que tiene la ventana (le sobran ${"%.0f".format(frameHeightDp - 24 - card.height / dp)} dp); " +
      "la ventanilla mide ${"%.0f".format(cover.height / dp)} dp; «Sobre este juego» mide ${"%.0f".format(about.width / dp)} × ${"%.0f".format(about.height / dp)} dp y entra ${"%.0f".format((cover.bottom - about.top) / dp)} dp por dentro de la ventanilla"
    File("$shots/medidas.txt").apply { parentFile.mkdirs(); appendText(line + "\n") }
  }

  @Test fun piloto_100() = shot(piloto(), "piloto-1_0")
  @Test fun piloto_130() = shot(piloto(), "piloto-1_3", fontScale = 1.3f)
  @Test fun correo_100() = shot(correo(), "correo-1_0", timed = false)
  @Test fun correo_130() = shot(correo(), "correo-1_3", fontScale = 1.3f, timed = false)
  @Test fun piloto_200() = shot(piloto(), "piloto-2_0", fontScale = 2f)
  @Test fun correo_200() = shot(correo(), "correo-2_0", fontScale = 2f, timed = false)

  /** Las 17 fichas plegadas (una por juego, a 1,0). Un solo `setContent`: el juego cambia por estado. */
  @Test
  fun las_17_fichas_plegadas() {
    var data by mutableStateOf(correo())
    rule.mainClock.autoAdvance = false
    rule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(frameHeightDp.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
          Box(Modifier.fillMaxWidth()) { GameSheetContent(data, AgeBand.ADULT, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = false, onChooseTimed = {}) }
        }
      }
    }
    for (g in GameRegistry.allGames) {
      data = cardData(g, g.title, 0.5f)
      repeat(2) { rule.mainClock.advanceTimeBy(600); rule.waitForIdle() }
      rule.onNodeWithTag("frame").captureRoboImage(filePath = "$shots/propuesta-${g.id}.png")
    }
  }
}
