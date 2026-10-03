package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.dp
import com.example.data.CoachTone
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.model.AgeBand
import com.example.model.DomainType
import com.example.ui.components.CosmosBackground
import com.example.ui.screens.FirstFlightScreen
import com.example.ui.screens.FlightActions
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * Las pantallas del inicio nuevo («Primer vuelo con Nubi») con datos de ejemplo, para verlas sin teléfono. Las capturas van a
 * `app/src/test/screenshots/inicio-real-*.png` y se copian a `docs/previews/`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class FirstFlightScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private val measured = mapOf("secuencia" to 0.62f, "freno" to 0.45f, "aterrizaje" to 0.30f, "meteoros" to 0.78f)

  private fun shot(name: String, state: FlightState, todayGames: List<String> = emptyList(), advanceMs: Long = 0) {
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent { Frame { FirstFlightScreen(state, todayGames, FlightActions.None) } }
    composeTestRule.mainClock.advanceTimeBy(advanceMs + 600)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/inicio-real-$name.png")
  }

  @Composable
  private fun Frame(content: @Composable () -> Unit) {
    NeuroVidaTheme {
      Box(Modifier.width(412.dp).height(915.dp)) {
        CosmosBackground()
        content()
      }
    }
  }

  @Test
  fun bienvenida() = shot("bienvenida", FlightState(FlightMode.FULL, FlightStep.HOLA))

  @Test
  fun una_pregunta() = shot(
    "pregunta",
    FlightState(FlightMode.FULL, FlightStep.METAS, name = "Ana", age = AgeBand.ADULT, goals = setOf(DomainType.MEMORIA, DomainType.RAZONAMIENTO))
  )

  @Test
  fun pregunta_de_animo() = shot("animo", FlightState(FlightMode.FULL, FlightStep.ANIMO, tone = CoachTone.CLARO))

  @Test
  fun antes_de_un_juego() = shot("juego", FlightState(FlightMode.FULL, FlightStep.JUEGO_2, measured = mapOf("secuencia" to 0.62f)))

  @Test
  fun tarjeta_puso_a_prueba() = shot(
    "tarjeta",
    FlightState(FlightMode.FULL, FlightStep.TARJETA_1, measured = mapOf("secuencia" to 0.62f), phrases = mapOf("secuencia" to "Repetiste bien un rastro de 5 luces."))
  )

  @Test
  fun punto_de_partida_con_la_primera_estrella() = shot(
    "punto",
    FlightState(FlightMode.FULL, FlightStep.PUNTO, name = "Ana", age = AgeBand.ADULT, goals = setOf(DomainType.RAZONAMIENTO), measured = measured),
    advanceMs = 2_000
  )

  @Test
  fun aviso() = shot("aviso", FlightState(FlightMode.FULL, FlightStep.AVISO, hour = 19))

  @Test
  fun camino_de_hoy() = shot("camino", FlightState(FlightMode.FULL, FlightStep.CAMINO), todayGames = listOf("rumbo", "trafico", "intrusa"))
}
