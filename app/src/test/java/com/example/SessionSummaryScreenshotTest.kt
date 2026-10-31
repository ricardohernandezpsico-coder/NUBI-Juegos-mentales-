package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.dp
import com.example.data.SessionGame
import com.example.data.SessionSummary
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.ui.components.BoardBackground
import com.example.ui.screens.SessionSummaryScreen
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/** El resumen de la ruta del día, con datos de ejemplo (para verlo sin teléfono): los de Ricardo (Bodega · Freno · Aterrizaje, racha de 1 día; Aterrizaje no sumó puntos), sobre el cielo de la app. Tarea 198: UNA losa crema y la semana de Hoy debajo. */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class SessionSummaryScreenshotTest {

  @get:Rule val composeTestRule = createComposeRule()

  @Before fun setUp() { TestSupport.fastFrames() }

  @Test
  fun resumen_de_la_sesion() {
    val summary = SessionSummary(
      games = listOf(SessionGame("bodega", "MEMORIA", played = true, gained = 9), SessionGame("freno", "ATENCION", played = true, gained = 12), SessionGame("aterrizaje", "RAZONAMIENTO", played = true, gained = 0)),
      areas = listOf(PlaceTestData.points("MEMORIA", 330, 9), PlaceTestData.points("ATENCION", 410, 12), PlaceTestData.points("RAZONAMIENTO", 90, 0))
    )
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(873.dp)) {
          BoardBackground()
          SessionSummaryScreen(summary, streak = 1, lamps = Week.Lamps(listOf(false, false, false, false, true, false, false), 4), lang = AppLanguage.SPANISH, onClose = {})
        }
      }
    }
    composeTestRule.mainClock.advanceTimeBy(1_200)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/resumen-sesion.png")
  }
}
