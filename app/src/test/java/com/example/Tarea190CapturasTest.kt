package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.dp
import com.example.model.GameRegistry
import com.example.ui.components.GameLoadingScreen
import com.example.ui.components.LocalReduceMotion
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
 * Las capturas de la lámina de la Tarea 190 (entrada al juego sin cuenta regresiva): la pantalla de carga NUEVA (planeta, nombre y etapa; sin «Cómo se juega» ni su frase) de tres juegos y la de un Contrarreloj. Solo corre al grabar capturas; deja los PNG en
 * `src/test/screenshots/tarea-190/propuesta/` (la de «hoy», con la explicación, sale de la captura real de Fable en `auditoria-fable/lamina-cuenta.png`).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class Tarea190CapturasTest {
  @get:Rule val rule = createComposeRule()

  private val out = "src/test/screenshots/tarea-190/propuesta"

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
  }

  private fun shot(name: String, content: @androidx.compose.runtime.Composable () -> Unit) {
    rule.mainClock.autoAdvance = false
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp).testTag("frame")) { content() } }
      }
    }
    rule.mainClock.advanceTimeBy(600)
    rule.onNodeWithTag("frame").captureRoboImage("$out/$name.png")
  }

  @Test fun carga_de_rastro_de_luz() = shot("carga-secuencia") {
    GameLoadingScreen(game = GameRegistry.getById("secuencia")!!, level = 3, timed = true, withSky = true)
  }

  @Test fun carga_de_correo() = shot("carga-correo") {
    GameLoadingScreen(game = GameRegistry.getById("correo")!!, level = 5, timed = null, withSky = true)
  }

  @Test fun carga_de_un_contrarreloj() = shot("carga-contrarreloj") {
    GameLoadingScreen(game = GameRegistry.getById("stroop")!!, level = null, timed = null, withSky = true, timeTrialName = "Contrarreloj de hoy")
  }
}
