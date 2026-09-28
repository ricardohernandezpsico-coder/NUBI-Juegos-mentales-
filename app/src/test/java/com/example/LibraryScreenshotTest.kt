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
import com.example.ui.screens.GameTile
import com.example.ui.screens.GameSheetContent
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
    val points = listOf(31f, 27f, 29f, 24f, 22f, 18f).mapIndexed { i, v -> MeasurePoint(now - (6 - i) * day, "homing", v) }
    val progress = mapOf("bitacora" to 0.62f, "rumbo" to 0.45f, "secuencia" to 0.75f, "parejas" to 0.40f, "rutatesoro" to 0.33f)
    val games = GameRegistry.allGames.filter { it.domain == DomainType.MEMORIA }
      .sortedBy { com.example.data.StarMeasures.defForGame(it.id) == null }
    composeTestRule.setContent {
      NeuroVidaTheme {
        Column(Modifier.width(412.dp).background(Color(0xFF0A1040))) {
          AreaHeader(DomainType.MEMORIA, "Memoria", 0.51f, explored = 5, total = 6, onPrev = {}, onNext = {})
          Column(Modifier.padding(horizontal = 16.dp), verticalArrangement = androidx.compose.foundation.layout.Arrangement.spacedBy(12.dp)) {
            games.forEach { g ->
              val last = if (g.id == "correo") null else now - (if (g.id == "rumbo") 0 else 2) * day
              GameTile(cardData(g, g.title, last, now, progress[g.id], if (g.id == "rumbo") points else emptyList(), 4), highlighted = g.id == "rumbo") {}
            }
          }
        }
      }
    }
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/library.png")
  }

  @Test
  fun sheet() {
    val rumbo = GameRegistry.getById("rumbo")!!
    val points = listOf(31f, 27f, 29f, 24f, 22f, 18f).mapIndexed { i, v -> MeasurePoint(now - (6 - i) * day, "homing", v) }
    val data = cardData(rumbo, rumbo.title, now - day, now, 0.45f, points, playsLast14 = 6)
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).background(Color(0xFF0A1040)).padding(14.dp)) {
          GameSheetContent(data, AgeBand.SENIOR, expertOpen = false, choice = PlayMode.DESAFIO, onChoose = {}, onPlay = {})
        }
      }
    }
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/library-ficha.png")
  }
}
