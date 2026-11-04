package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Week
import com.example.ui.components.BoardBackground
import com.example.ui.components.HoyWeek
import com.example.ui.components.LocalReduceMotion
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
 * Los cinco cuadros del destello de la llama de hoy para la lámina de la Tarea 223 (`docs/previews/tarea-223.png`, `tools/previews/tarea_223.py`); solo graba (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea223DestelloCapturasTest*"`)
 * y los deja en `app/src/test/screenshots/t223/despues_glint_<ms>.png` (fuera de git). Va aparte de `Tarea223CapturasTest` porque esa se grabó también con el código de ANTES, que no tiene destello.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class Tarea223DestelloCapturasTest {
  @get:Rule val rule = createComposeRule()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  // ---------------------------------------------------------------- el destello de la llama de hoy: cinco cuadros (solo «después»: el «antes» no tiene destello)

  /** La llama de hoy pasa de apagada a encendida con Hoy a la vista (lo que pasa al volver de la primera partida del día); el reloj de la prueba se detiene y se avanza a mano para tomar los cuadros a los 0, 150, 300, 450 y 600 ms. */
  @Test fun glint_cuadros() {
    var lit by mutableStateOf(false)
    rule.mainClock.autoAdvance = false
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density * 1.35f, 1f), LocalReduceMotion provides false) {
        NeuroVidaTheme {
          Box(Modifier.width(300.dp).height(96.dp).background(Color(0xFF050823))) {
            BoardBackground()
            Box(Modifier.fillMaxSize().padding(top = 4.dp)) { HoyWeek(Week.Lamps(listOf(true, true, false, false, false, false, lit), todayIndex = 6), 3, glintVisible = true) }
          }
        }
      }
    }
    rule.mainClock.advanceTimeBy(100)                                            // Hoy a la vista con hoy apagado: la llama «se arma»
    rule.runOnIdle { lit = true }; androidx.compose.runtime.snapshots.Snapshot.sendApplyNotifications()     // con el reloj detenido, un cambio escrito desde el hilo de la prueba no llega a la composición
    rule.mainClock.advanceTimeByFrame()                                          // el cuadro en que se enciende: nace en el 0,8 del destello
    var elapsed = 0L
    for (ms in listOf(0L, 150L, 300L, 450L, 600L)) {
      rule.mainClock.advanceTimeBy(ms - elapsed); elapsed = ms
      save("glint_$ms")
    }
  }

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t223/despues_$name.png") }
}
