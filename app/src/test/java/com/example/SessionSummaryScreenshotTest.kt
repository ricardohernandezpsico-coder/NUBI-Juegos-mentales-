package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.dp
import com.example.data.AreaStatus
import com.example.data.SessionGame
import com.example.data.SessionSummary
import com.example.model.AppLanguage
import com.example.ui.screens.OnboardingScreen
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

/** El resumen de la sesión del día y dos páginas de la bienvenida, con datos de ejemplo (para verlas sin teléfono). */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class SessionSummaryScreenshotTest {

  @get:Rule val composeTestRule = createComposeRule()

  @Test
  fun resumen_de_la_sesion() {
    val summary = SessionSummary(
      games = listOf(SessionGame("radar", "VELOCIDAD", 82), SessionGame("stroop", "ATENCION", 74), SessionGame("parejas", "MEMORIA", 68)),
      areas = listOf(
        AreaStatus("VELOCIDAD", 0.52f, 0.06f, emptyList()),
        AreaStatus("ATENCION", 0.40f, 0f, emptyList()),
        AreaStatus("MEMORIA", null, 0f, emptyList())
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

  @Test
  fun bienvenida_y_metas() {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          OnboardingScreen(onFinish = { _, _, _, _, _, _, _ -> })
        }
      }
    }
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/bienvenida.png")
    composeTestRule.onNodeWithTag("btn_onboarding_start").performClick()
    composeTestRule.waitForIdle()
    composeTestRule.onNodeWithTag("btn_onboarding_name_next").performClick()
    composeTestRule.waitForIdle()
    composeTestRule.onNodeWithTag("age_band_adult").performClick()
    composeTestRule.waitForIdle()
    composeTestRule.onNodeWithTag("education_media").performClick()
    composeTestRule.waitForIdle()
    composeTestRule.onNodeWithTag("goal_memoria").performClick()
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/bienvenida-metas.png")
  }
}
