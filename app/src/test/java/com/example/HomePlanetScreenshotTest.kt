package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.dp
import com.example.data.MeasurePoint
import com.example.data.Planet
import com.example.data.PlanetPlay
import com.example.data.StarMeasures
import com.example.ui.components.HomePlanet
import com.example.ui.components.PlanetShip
import com.example.ui.screens.DiscoveryBlock
import com.example.ui.screens.ZonesLine
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/** "Tu planeta" de Hoy con datos de ejemplo (para ver el dibujo real sin teléfono). */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class HomePlanetScreenshotTest {

  @get:Rule val composeTestRule = createComposeRule()

  private val day = 86_400_000L
  private val now = 20_000 * day + 12 * 3_600_000L
  private val dayOf = { t: Long -> t / day }

  private fun plays(domain: String, n: Int, firstDaysAgo: Int) = (0 until n).map { PlanetPlay(domain, now - (firstDaysAgo + it) * day) }

  private fun render(file: String, playedToday: Boolean, measures: List<MeasurePoint>) {
    val all = plays("MEMORIA", 30, if (playedToday) 0 else 1) + plays("ATENCION", 12, 1) + plays("RAZONAMIENTO", 6, 2) +
      plays("CALCULO", 9, 3) + plays("VELOCIDAD", 18, if (playedToday) 0 else 2) + plays("LENGUAJE", 1, 9)
    val state = Planet.build(all, now, dayOf)
    val ships = listOf(
      PlanetShip("series", "RAZONAMIENTO", landed = playedToday),
      PlanetShip("radar", "VELOCIDAD", landed = playedToday),
      PlanetShip("rumbo", "MEMORIA", landed = false)
    )
    // El planeta gira sin parar: con el reloj automático la prueba nunca quedaría "quieta".
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent {
      NeuroVidaTheme {
        Column(Modifier.width(412.dp).background(Color(0xFF0A1040))) {
          HomePlanet(state, ships, onZone = {}, modifier = Modifier.fillMaxWidth().height(340.dp))
          ZonesLine(state, onZone = {})
          DiscoveryBlock(StarMeasures.discover(measures, now), StarMeasures.nudge(measures), onPlay = {})
        }
      }
    }
    composeTestRule.mainClock.advanceTimeBy(9_000)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/$file")
  }

  @Test
  fun planet_before_playing() {
    val glance = listOf(132f, 121f, 118f, 104f, 96f, 84f).mapIndexed { i, v -> MeasurePoint(now - (5 - i) * day, "glance", v) }
    render("home-planet.png", playedToday = false, measures = glance)
  }

  @Test
  fun planet_after_playing_without_measures() {
    render("home-planet-hoy.png", playedToday = true, measures = listOf(MeasurePoint(now - day, "homing", 30f)))
  }
}
