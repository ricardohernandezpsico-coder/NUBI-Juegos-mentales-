package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.dp
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.model.AgeBand
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.screens.AreaHeader
import com.example.ui.screens.GameCard
import com.example.ui.screens.ModeSheetContent
import com.example.ui.screens.cardData
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/** La pestaña Juegos (área a la vez + cartas) y la ventana de modos, con datos de ejemplo, para verlas sin teléfono. */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class LibraryScreenshotTest {

  @get:Rule val composeTestRule = createComposeRule()

  private val day = 86_400_000L
  private val now = 400 * day + 3_600_000L

  @Test
  fun library() {
    val rumbo = GameRegistry.getById("rumbo")!!
    val points = listOf(31f, 27f, 29f, 24f, 22f, 18f).mapIndexed { i, v -> MeasurePoint(now - (6 - i) * day, "homing", v) }
    val data = cardData(rumbo, rumbo.title, "2 de 6", now - day, now, 0.45f, points, playsLast14 = 6)
    composeTestRule.setContent {
      NeuroVidaTheme {
        Column(Modifier.width(412.dp).background(Color(0xFF0A1040))) {
          AreaHeader(DomainType.MEMORIA, "Memoria", 0.51f, explored = 4, total = 6, onPrev = {}, onNext = {})
          Box(Modifier.padding(horizontal = 30.dp)) {
            GameCard(data, PlayMode.A_TU_MEDIDA, onMode = {}, onPlay = {})
          }
        }
      }
    }
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/library.png")
  }

  @Test
  fun libraryUnmeasured() {
    val correo = GameRegistry.getById("correo")!!
    val data = cardData(correo, correo.title, "3 de 6", null, now, null, emptyList(), playsLast14 = 0)
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).background(Color(0xFF0A1040)).padding(horizontal = 30.dp, vertical = 12.dp)) {
          GameCard(data, PlayMode.A_TU_MEDIDA, onMode = {}, onPlay = {})
        }
      }
    }
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/library-sin-medir.png")
  }

  @Test
  fun modes() {
    val rumbo = GameRegistry.getById("rumbo")!!
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).background(Color(0xFF0A1040)).padding(16.dp)) {
          ModeSheetContent(rumbo, rumbo.title, 0.45f, AgeBand.SENIOR, expertOpen = false, choice = PlayMode.DESAFIO, onChoose = {}, onPlay = {})
        }
      }
    }
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/library-modos.png")
  }
}
