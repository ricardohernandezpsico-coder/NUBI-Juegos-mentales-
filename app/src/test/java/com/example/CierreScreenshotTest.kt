package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.FirstFlightScreen
import com.example.ui.screens.FlightActions
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
 * La lámina «hoy | propuesta» de la Tarea 194b (`docs/previews/tarea-194b.png`): el cierre del Primer vuelo («Paso 6 de 6», «Tu primer vuelo fue tu ruta de hoy») con letra 1,0, 1,3 y 2,0, a 393 × 873 dp. No corre en las pruebas de siempre: solo al grabar
 * (`./gradlew.bat :app:recordRoborazziDebug --tests "*CierreScreenshotTest*"`). El prefijo (`hoy` = el código de antes, `prop` = el nuevo) sale de la variable de entorno T194_PREFIX; los PNG van a `src/test/screenshots/cierre/` y `tools/previews/tarea_194b.py` arma la hoja.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class CierreScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private val prefix = System.getenv("T194_PREFIX") ?: "prop"

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun cierre(name: String, fontScale: Float) {
    composeTestRule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.fillMaxSize()) {
            BoardBackground()
            FirstFlightScreen(FlightState(FlightMode.FULL, FlightStep.CAMINO), listOf("bodega", "calculo", "intrusa"), FlightActions.None)
          }
        }
      }
    }
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/cierre/${prefix}_$name.png")
  }

  @Test fun cierre_10() = cierre("10", 1f)
  @Test fun cierre_13() = cierre("13", 1.3f)
  @Test fun cierre_20() = cierre("20", 2f)
}
