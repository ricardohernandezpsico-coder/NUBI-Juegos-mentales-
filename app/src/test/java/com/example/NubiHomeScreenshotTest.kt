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
import com.example.data.AreaProgress
import com.example.data.AreaStatus
import com.example.data.MeasurePoint
import com.example.model.AppLanguage
import com.example.model.DomainType
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.NubiHome
import com.example.ui.screens.AreaDetail
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/** Hoy con Nubi al centro y el detalle de un área, con datos de ejemplo (para ver el dibujo real sin teléfono). */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class NubiHomeScreenshotTest {

  @get:Rule val composeTestRule = createComposeRule()

  private val day = 86_400_000L
  private val now = 20_000 * day
  private val names = DomainType.values().associate { it.name to it.displayName }
  private val statuses = listOf(
    AreaStatus("MEMORIA", 0.52f, 0.06f, listOf(0.40f, 0.43f, 0.46f, 0.52f)),
    AreaStatus("ATENCION", 0.62f, 0.04f, listOf(0.50f, 0.56f, 0.58f, 0.62f)),
    AreaStatus("RAZONAMIENTO", 0.43f, 0f, listOf(0.43f, 0.43f, 0.43f, 0.43f)),
    AreaStatus("LENGUAJE", 0.12f, 0.03f, listOf(0.06f, 0.08f, 0.09f, 0.12f))
  )

  @Test
  fun hoy_con_nubi() {
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(680.dp).background(Color(0xFF050823))) {
          NubiHome(statuses, names, AreaProgress.nubiLine(statuses, names), onArea = {})
        }
      }
    }
    composeTestRule.mainClock.advanceTimeBy(1_000)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-nubi.png")
  }

  @Test
  fun detalle_de_memoria() {
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          AreaDetail(
            areaKey = AREA_ORDER[0], statuses = statuses, names = names, history = emptyList(),
            measures = listOf(MeasurePoint(now - day, "homing", 18f)), now = now, lang = AppLanguage.SPANISH,
            onArea = {}, onPlay = {}, onClose = {}
          )
        }
      }
    }
    composeTestRule.mainClock.advanceTimeBy(1_000)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-nubi-detalle.png")
  }
}
