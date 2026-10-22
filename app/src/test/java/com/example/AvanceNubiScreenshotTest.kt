package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.AreaStatus
import com.example.data.Streak
import com.example.data.Week
import com.example.data.nubiWeekLine
import com.example.data.weekSummary
import com.example.model.DomainType
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.AvanceAreas
import com.example.ui.screens.AvanceWeek
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
 * La hoja «hoy | propuesta» de Nubi comentando «Tu semana» en Avance (Tarea 131, A6, `docs/previews/avance-nubi.png`): el bloque REAL (`AvanceWeek` + `AvanceAreas`) a 393 × 873 dp, con la letra del sistema en 1,0 y en 1,3.
 * «Hoy» = sin frase (`nubiLine` vacío: no hay globo); «propuesta» = la frase de `nubiWeekLine` con Nubi de 56 dp. No corre en las pruebas de siempre: solo al grabar capturas
 * (`AVANCE_NUBI_PREFIX=hoy|prop ./gradlew.bat :app:recordRoborazziDebug --tests "*AvanceNubiScreenshotTest*"`), y deja los PNG en `src/test/screenshots/avance-nubi/`; `tools/previews/avance_nubi.py` arma la hoja.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class AvanceNubiScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private val statuses = listOf(
    AreaStatus("MEMORIA", 0.52f, 0.06f, listOf(0.40f, 0.45f, 0.46f, 0.52f)),
    AreaStatus("ATENCION", 0.62f, -0.02f, listOf(0.60f, 0.64f, 0.64f, 0.62f)),
    AreaStatus("RAZONAMIENTO", 0.43f, 0f, listOf(0.43f, 0.43f, 0.43f, 0.43f)),
    AreaStatus("LENGUAJE", null, 0f, listOf(null, null, null, null))
  )
  private val names = DomainType.values().associate { it.name to it.displayName }

  private fun shot(name: String, fontScale: Float) {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    val prefix = System.getenv("AVANCE_NUBI_PREFIX") ?: "prop"
    // lun, mar y jue jugados; hoy es el jueves: lleva 3 de 4 → «Te falta 1 día para tu meta. Cualquier ruta cuenta.»
    val lamps = Week.Lamps(listOf(true, true, false, true, false, false, false), todayIndex = 3)
    val summary = weekSummary(lamps, goal = 4, streak = 3, areas = 2, noGamesAtAll = false)
    val line = if (prefix == "hoy") "" else nubiWeekLine(lamps, 4, 2, noGamesAtAll = false)
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(873.dp).testTag("frame")) {
            BoardBackground()
            Column(Modifier.fillMaxSize().padding(horizontal = 22.dp, vertical = 24.dp)) {
              AvanceWeek(summary, lamps, line)
              Box(Modifier.height(26.dp))
              AvanceAreas(statuses, names) {}
            }
          }
        }
      }
    }
    repeat(3) { composeTestRule.mainClock.advanceTimeBy(600); composeTestRule.waitForIdle() }
    composeTestRule.onNodeWithTag("frame").captureRoboImage(filePath = "src/test/screenshots/avance-nubi/$prefix-$name.png")
  }

  @Test fun a_letra_1_0() = shot("393-1_0", 1f)

  @Test fun b_letra_1_3() = shot("393-1_3", 1.3f)
}
