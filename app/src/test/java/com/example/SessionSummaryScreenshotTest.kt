package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.dp
import com.example.data.AreaPoints
import com.example.data.SessionGame
import com.example.data.SessionSummary
import com.example.model.AppLanguage
import com.example.ui.screens.SessionSummaryScreen
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/** El resumen de la sesión del día, con datos de ejemplo (para verlo sin teléfono). */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class SessionSummaryScreenshotTest {

  @get:Rule val composeTestRule = createComposeRule()

  @Test
  fun resumen_de_la_sesion() {
    val summary = SessionSummary(
      games = listOf(SessionGame("radar", "ATENCION", played = true, gained = 12), SessionGame("calculo", "RAZONAMIENTO", played = true, gained = 0), SessionGame("parejas", "MEMORIA", played = true, gained = 5)),
      areas = listOf(
        PlaceTestData.points("ATENCION", 330, 12),
        PlaceTestData.points("RAZONAMIENTO", 400, 0),
        PlaceTestData.points("MEMORIA", 205, 5)
      )
    )
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          SessionSummaryScreen(summary, streak = 4, lang = AppLanguage.SPANISH, onClose = {})
        }
      }
    }
    composeTestRule.mainClock.advanceTimeBy(1_000)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/resumen-sesion.png")
  }
}
