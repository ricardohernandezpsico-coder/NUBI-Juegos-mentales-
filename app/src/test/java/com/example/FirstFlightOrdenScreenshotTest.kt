package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.model.AgeBand
import com.example.model.DomainType
import com.example.ui.components.CosmosBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.FirstFlightScreen
import com.example.ui.screens.FlightActions
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assume
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La hoja «hoy | propuesta» del Primer vuelo (Tarea 132, A2 + A4, `docs/previews/primer-vuelo-orden.png`): las pantallas REALES a 412 × 915 dp: Hola · Nombre · Juego 1 (antes de jugar) · Tarjeta 1 · Edad, y un cuadro a 360 dp con la letra al 130 %.
 * «Hoy» es el orden y la cabecera de antes (la edad antes del juego 1, la barra y «Faltan unos 5 minutos»); «propuesta», el juego primero y las seis farolas de etapa. No corre en las pruebas de siempre: solo al grabar capturas
 * (`PRIMER_VUELO_PREFIX=hoy|prop ./gradlew.bat :app:recordRoborazziDebug --tests "*FirstFlightOrdenScreenshotTest*"`), y deja los PNG en `src/test/screenshots/primer-vuelo-orden/`; `tools/previews/primer_vuelo_orden.py` arma la hoja.
 * Para grabar «hoy» se vuelve al código de antes (`git stash` de lo modificado: esta prueba solo usa `FlightState`, `FlightStep` y `FirstFlightScreen`, que existen en los dos).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w412dp-h915dp-440dpi", sdk = [36])
class FirstFlightOrdenScreenshotTest {
  @get:Rule val rule = createComposeRule()

  private val prefix get() = System.getenv("PRIMER_VUELO_PREFIX") ?: "prop"
  private val firstGame = mapOf("secuencia" to 0.62f)
  private val phrase = mapOf("secuencia" to "Repetiste bien un rastro de 5 luces.")

  private fun shot(name: String, state: FlightState, widthDp: Int = 412, heightDp: Int = 915, fontScale: Float = 1f) {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    rule.mainClock.autoAdvance = false
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp).testTag("frame")) { CosmosBackground(); FirstFlightScreen(state, emptyList(), FlightActions.None) } }
      }
    }
    rule.mainClock.advanceTimeBy(1_000)
    rule.onNodeWithTag("frame").captureRoboImage(filePath = "src/test/screenshots/primer-vuelo-orden/$prefix-$name.png")
  }

  /** Antes (hoy) la edad se respondía antes del juego 1; ahora todavía no se sabe. */
  private val ageAtGame1: AgeBand? get() = if (prefix == "hoy") AgeBand.ADULT else null

  @Test fun a_hola() = shot("hola", FlightState(FlightMode.FULL, FlightStep.HOLA))

  @Test fun b_nombre() = shot("nombre", FlightState(FlightMode.FULL, FlightStep.NOMBRE, name = "Ana"))

  @Test fun c_juego1() = shot("juego1", FlightState(FlightMode.FULL, FlightStep.JUEGO_1, name = "Ana", age = ageAtGame1))

  @Test fun d_tarjeta1() = shot("tarjeta1", FlightState(FlightMode.FULL, FlightStep.TARJETA_1, name = "Ana", age = ageAtGame1, measured = firstGame, phrases = phrase))

  @Test fun e_edad() = shot("edad", FlightState(FlightMode.FULL, FlightStep.EDAD, name = "Ana", measured = if (prefix == "hoy") emptyMap() else firstGame, phrases = if (prefix == "hoy") emptyMap() else phrase))

  /** El paso más largo («Paso 6 de 6 · Tu punto de partida») en la pantalla más angosta, con la letra al 130 %. */
  @Test fun f_360_letra_1_3() = shot(
    "360-1_3",
    FlightState(FlightMode.FULL, FlightStep.PUNTO, name = "Ana", age = AgeBand.ADULT, goals = setOf(DomainType.RAZONAMIENTO), measured = mapOf("secuencia" to 0.62f, "freno" to 0.45f, "aterrizaje" to 0.30f, "meteoros" to 0.78f)),
    widthDp = 360, heightDp = 780, fontScale = 1.3f
  )
}
