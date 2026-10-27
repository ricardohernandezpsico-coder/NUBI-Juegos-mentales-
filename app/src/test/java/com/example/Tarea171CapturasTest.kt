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
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.model.AgeBand
import com.example.model.GameRegistry
import com.example.ui.screens.GameAboutContent
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

/**
 * Las capturas de la ficha-portada (Tarea 171, `docs/previews/tarea-171.png`): Piloto Estelar (con Reto) y Correo Estelar (sin Reto), con letra 1,0 y 1,3, plegada y con «Más opciones»; «Sobre este juego»; y las 17 portadas dentro de su ventanilla. A 393 × 873 dp menos las barras
 * del sistema (24 arriba, 48 abajo) y con el margen de la ventana, como en la ventana real; datos fijos. No corre en las pruebas de siempre: solo al grabar capturas
 * (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea171CapturasTest*"`) y deja los PNG en `src/test/screenshots/tarea-171/`; `tools/previews/tarea_171.py` arma la lámina.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class Tarea171CapturasTest {
  @get:Rule val rule = createComposeRule()

  @Before fun solo_al_grabar() { Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true") }

  private val day = 86_400_000L
  private val now = 400 * day + 3_600_000L
  private val shots = "src/test/screenshots/tarea-171"

  private fun series(key: String, vararg values: Float) = values.mapIndexed { i, v -> MeasurePoint(now - (values.size - i) * day, key, v, 0.5f, true) }

  private fun piloto(): GameCardData {
    val g = GameRegistry.getById("piloto")!!
    return cardData(g, g.title, 0.92f)
  }

  private fun correo(): GameCardData {
    val g = GameRegistry.getById("correo")!!
    return cardData(g, g.title, 0.48f)
  }

  /** La ficha de [data] en el marco de la ventana real (393 × 825 dp: el alto que dejan las barras del sistema) con la letra [fontScale]; [unfold] la despliega («Más opciones»); [about] muestra «Sobre este juego» en su lugar. */
  private fun shot(data: GameCardData, name: String, fontScale: Float = 1f, timed: Boolean = true, unfold: Boolean = false, about: Boolean = false, mode: PlayMode = PlayMode.A_TU_MEDIDA) {
    rule.mainClock.autoAdvance = false
    rule.setContent {
      CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, fontScale)) {
        NeuroVidaTheme {
          var choice by remember { mutableStateOf(mode) }
          var withClock by remember { mutableStateOf(timed) }
          Box(Modifier.width(393.dp).height(873.dp - 72.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
            Box(Modifier.fillMaxWidth()) {
              if (about) GameAboutContent(data)
              else GameSheetContent(data, AgeBand.ADULT, expertOpen = false, choice = choice, onChoose = { choice = it }, onPlay = {}, timed = withClock, onChooseTimed = { withClock = it })
            }
          }
        }
      }
    }
    repeat(3) { rule.mainClock.advanceTimeBy(600); rule.waitForIdle() }
    if (unfold) {
      rule.onNodeWithTag("sheet_change").performClick()
      repeat(4) { rule.mainClock.advanceTimeBy(600); rule.waitForIdle() }
    }
    rule.onNodeWithTag("frame").captureRoboImage(filePath = "$shots/$name.png")
  }

  @Test fun piloto_plegada_100() = shot(piloto(), "piloto-1_0")
  @Test fun piloto_plegada_130() = shot(piloto(), "piloto-1_3", fontScale = 1.3f)
  @Test fun correo_plegada_100() = shot(correo(), "correo-1_0", timed = false)
  @Test fun correo_plegada_130() = shot(correo(), "correo-1_3", fontScale = 1.3f, timed = false)
  @Test fun piloto_mas_opciones_100() = shot(piloto(), "piloto-mas-1_0", unfold = true)
  @Test fun piloto_mas_opciones_130() = shot(piloto(), "piloto-mas-1_3", fontScale = 1.3f, unfold = true)
  @Test fun correo_mas_opciones_100() = shot(correo(), "correo-mas-1_0", timed = false, unfold = true)
  @Test fun piloto_sobre_este_juego_100() = shot(piloto(), "piloto-sobre-1_0", about = true)
  @Test fun piloto_sobre_este_juego_130() = shot(piloto(), "piloto-sobre-1_3", fontScale = 1.3f, about = true)
  @Test fun piloto_plegada_200() = shot(piloto(), "piloto-2_0", fontScale = 2f)
  @Test fun correo_plegada_200() = shot(correo(), "correo-2_0", fontScale = 2f, timed = false)

  /** Las 17 fichas plegadas (una por juego, a 1,0): para ver que cada portada se lee dentro de su ventanilla con el emblema asomando. Un solo `setContent`: el juego cambia por estado. */
  @Test
  fun las_17_fichas_plegadas() {
    var data by mutableStateOf(correo())
    rule.mainClock.autoAdvance = false
    rule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(873.dp - 72.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
          Box(Modifier.fillMaxWidth()) { GameSheetContent(data, AgeBand.ADULT, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = false, onChooseTimed = {}) }
        }
      }
    }
    for (g in GameRegistry.allGames) {
      data = cardData(g, g.title, 0.5f)
      repeat(2) { rule.mainClock.advanceTimeBy(600); rule.waitForIdle() }
      rule.onNodeWithTag("frame").captureRoboImage(filePath = "$shots/juego-${g.id}.png")
    }
  }
}
