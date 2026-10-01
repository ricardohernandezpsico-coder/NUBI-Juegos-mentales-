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
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.unit.dp
import com.example.ui.screens.LicensesScreen
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/** Ajustes → "Licencias y créditos" (SPALEX, tipografías y Unity), para verla sin teléfono. */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class LicensesScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  @Test
  fun licencias() {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          LicensesScreen(onClose = {})
        }
      }
    }
    composeTestRule.onNodeWithTag("licenses_content").assertIsDisplayed()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/licencias.png")
  }
}
