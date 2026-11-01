package com.example

import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Comparison
import com.example.games.GuestTrial
import com.example.games.GuestTrialScreen
import com.example.model.AgeRange
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.PlusLockSheetContent
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
 * Las capturas de la Tarea 205c para la lámina `docs/previews/tarea-205c.png` (`tools/previews/tarea_205c.py`): la hoja del candado SIN su `Dialog` a letra 1,0 y 2,0 (la de la Tarea 205 era idéntica a 1,0 porque un `Dialog` trae su propia densidad; con el contenido suelto la letra sí cambia) y el final de invitado con la edad elegida.
 * La puerta y la escalera a 2,0, y la pregunta de la edad del invitado, salen del EMULADOR (`tools/emulador_ui.py`): son lo que ve una persona. No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea205cCapturasTest*"`).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea205cCapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun sheet(name: String, scale: Float, toEnd: Boolean) {
    rule.mainClock.autoAdvance = true
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp).padding(vertical = 24.dp)) { BoardBackground(); PlusLockSheetContent(onClose = {}, kind = Comparison.Kind.ESTIMATED) } }
      }
    }
    rule.waitForIdle()
    if (toEnd) { rule.onNodeWithTag("btn_plus_sheet_ok", useUnmergedTree = true).performScrollTo(); rule.waitForIdle() }
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t205c/ahora_$name.png")
  }

  @Test fun a_hoja_1_0() = sheet("sheet_393_10", 1f, toEnd = false)
  @Test fun b_hoja_2_0_arriba() = sheet("sheet_393_20_top", 2f, toEnd = false)
  @Test fun c_hoja_2_0_cerrar() = sheet("sheet_393_20_end", 2f, toEnd = true)

  @Test fun d_final_de_invitado() {
    rule.mainClock.autoAdvance = true
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp)) { BoardBackground(); GuestTrialScreen(trial = GuestTrial("radar", 16, 2, AgeRange.R65_74), onAgain = {}, onDone = {}) } }
      }
    }
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t205c/ahora_guest_393_10.png")
  }
}
