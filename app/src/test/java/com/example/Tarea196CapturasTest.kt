package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.ui.components.BoardBackground
import com.example.ui.screens.AllGamesContent
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
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
 * La lámina «hoy | propuesta» de la Tarea 196 (`docs/previews/tarea-196.png`): «Todos los juegos» entera con los chips de área de antes y de ahora, a 393 × 873 dp con letra 1,0 («Todas» y «Lenguaje» elegidas), a 360 × 780 con 1,0 y a 393 con 1,3 y con 2,0. No corre en las pruebas de siempre: solo al grabar
 * (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea196CapturasTest*"`). El prefijo (`hoy` = el código de antes, `prop` = el nuevo) sale de la variable de entorno T196_PREFIX; los PNG van a `src/test/screenshots/t196/` y `tools/previews/tarea_196.py` arma la hoja.
 * Escrita para compilar con el código de antes y con el de ahora (solo usa `AllGamesContent`).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class Tarea196CapturasTest {
  @get:Rule val rule = createComposeRule()

  private val now = 400 * 86_400_000L + 3_600_000L
  private val places = PlaceTestData.places(now)
  private val prefix = System.getenv("T196_PREFIX") ?: "prop"

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun shot(name: String, widthDp: Int, heightDp: Int, scale: Float, area: String?) {
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale)) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            AllGamesContent(places = places, area = area, query = "", mark = null, onQueryChange = {}, onArea = {}, onClose = {}, onGame = {}, modifier = Modifier.padding(top = 24.dp))
          }
        }
      }
    }
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t196/${prefix}_$name.png")
  }

  @Test fun a_393_10_todas() = shot("393_10_todas", 393, 873, 1f, null)
  @Test fun b_393_10_lenguaje() = shot("393_10_lenguaje", 393, 873, 1f, "LENGUAJE")
  @Test fun c_360_10_todas() = shot("360_10_todas", 360, 780, 1f, null)
  @Test fun d_393_13_atencion() = shot("393_13_atencion", 393, 873, 1.3f, "ATENCION")
  @Test fun e_393_20_todas() = shot("393_20_todas", 393, 873, 2f, null)
}
