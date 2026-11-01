package com.example

import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.games.GuestTrial
import com.example.games.GuestTrialScreen
import com.example.model.AgeRange
import com.example.ui.components.BoardBackground
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
 * La lámina de la Tarea 208b (`docs/previews/tarea-208b.png`, `tools/previews/tarea_208b.py`): el final del Contrarreloj de INVITADO ahora trae la regla de Rescate relámpago («cada cápsula bien marcada es un acierto y cada cápsula marcada que no estaba, un error.»)
 * justo después de «cómo se cuenta», a 16 sp. Rescate con letra 1,0 y 2,0 (`GuestTrialScreen` no es un `Dialog`: Roborazzi mide la letra de verdad) y, para comparar, el de Tinta o Palabra sin la frase.
 * No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea208bCapturasTest*"`); los PNG van a `src/test/screenshots/t208b/`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea208bCapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun guest(name: String, trial: GuestTrial, scale: Float) {
    rule.mainClock.autoAdvance = true
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp)) { BoardBackground(); GuestTrialScreen(trial = trial, onAgain = {}, onDone = {}) } }
      }
    }
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t208b/ahora_$name.png")
  }

  @Test fun a_rescate_393_10() = guest("rescate_393_10", GuestTrial("radar", 16, 2, AgeRange.R65_74), 1f)
  @Test fun b_rescate_393_20() = guest("rescate_393_20", GuestTrial("radar", 16, 2, AgeRange.R65_74), 2f)
  @Test fun c_tinta_393_10() = guest("tinta_393_10", GuestTrial("stroop", 36, 2, AgeRange.R45_54), 1f)
}
