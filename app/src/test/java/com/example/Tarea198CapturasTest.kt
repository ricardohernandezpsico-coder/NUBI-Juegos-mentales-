package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.SessionGame
import com.example.data.SessionSummary
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.ui.components.BoardBackground
import com.example.ui.screens.SessionSummaryScreen
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
 * La lámina «hoy | propuesta» de la Tarea 198 (`docs/previews/tarea-198.png`): «¡Ruta de hoy completa!» con los datos de Ricardo (Bodega · Freno · Aterrizaje, racha de 1 día; Aterrizaje no sumó puntos) sobre el cielo de la app, a 393 × 873 con letra 1,0, 1,3 y 2,0, a 360 × 780 y con Nubi en lo
 * alto de su saltito. No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea198CapturasTest*"`). El prefijo (`hoy` = el código de antes, `prop` = el nuevo) sale de T198_PREFIX; los PNG van a `src/test/screenshots/t198/` y `tools/previews/tarea_198.py` arma la hoja.
 * La llamada a la pantalla está en UNA línea marcada (`// LLAMADA`): para grabar «hoy» con el código de antes (sin las farolas de la semana) se cambia solo esa línea.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea198CapturasTest {
  @get:Rule val rule = createComposeRule()

  private val prefix = System.getenv("T198_PREFIX") ?: "prop"

  private val summary = SessionSummary(
    games = listOf(SessionGame("bodega", "MEMORIA", played = true, gained = 9), SessionGame("freno", "ATENCION", played = true, gained = 12), SessionGame("aterrizaje", "RAZONAMIENTO", played = true, gained = 0)),
    areas = listOf(PlaceTestData.points("MEMORIA", 330, 9), PlaceTestData.points("ATENCION", 410, 12), PlaceTestData.points("RAZONAMIENTO", 90, 0))
  )

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun shot(name: String, widthDp: Int, heightDp: Int, scale: Float, advanceMs: Long) {
    rule.mainClock.autoAdvance = false
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale)) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            SessionSummaryScreen(summary, 1, Week.Lamps(listOf(false, false, false, false, true, false, false), 4), AppLanguage.SPANISH, {})   // LLAMADA
          }
        }
      }
    }
    rule.mainClock.advanceTimeBy(advanceMs)
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t198/${prefix}_$name.png")
  }

  @Test fun a_393_10() = shot("393_10", 393, 873, 1f, 1_200)
  @Test fun b_393_13() = shot("393_13", 393, 873, 1.3f, 1_200)
  @Test fun c_393_20() = shot("393_20", 393, 873, 2f, 1_200)
  @Test fun d_360_10() = shot("360_10", 360, 780, 1f, 1_200)
  @Test fun e_393_10_salto() = shot("393_10_salto", 393, 873, 1f, 225)                  // Nubi en lo alto de su primer salto (a la cuarta parte de los 900 ms)
}
