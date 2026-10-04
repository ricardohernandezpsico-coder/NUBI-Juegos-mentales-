package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.dp
import com.example.data.AreaStatus
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.model.AgeBand
import com.example.model.AppLanguage
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.components.NeuroNavBar
import com.example.ui.components.TopActions
import com.example.ui.screens.AreaGrid
import com.example.ui.screens.SuggestionHeader
import com.example.ui.screens.suggestArea
import com.example.ui.screens.AreaWindow
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.cardData
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La pestaña Juegos (las 4 áreas con "Nubi te sugiere", la ventana de un área con Nubi científica y la ficha de un juego) y la barra de abajo,
 * con datos de ejemplo, para verlas sin teléfono.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class LibraryScreenshotTest {

  @get:Rule val composeTestRule = createComposeRule()

  private val day = 86_400_000L
  private val now = 400 * day + 3_600_000L
  private val statuses = listOf(
    AreaStatus("MEMORIA", 0.52f, 0.06f, emptyList()),
    AreaStatus("ATENCION", 0.62f, 0.04f, emptyList()),
    AreaStatus("RAZONAMIENTO", 0.43f, 0f, emptyList()),
    AreaStatus("LENGUAJE", 0.12f, 0.03f, emptyList())
  )

  @Test
  fun areas() {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Column(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          Box(Modifier.fillMaxWidth().padding(16.dp)) {
            TopActions("Ricardo", {}, {}, Modifier.align(androidx.compose.ui.Alignment.CenterEnd))
          }
          // Razonamiento hace 4 días que no se juega: Nubi lo sugiere (las otras se jugaron ayer u hoy).
          val plays = listOf("secuencia" to now, "stroop" to now - day, "calculo" to now - 4 * day, "anagramas" to now - day)
          val suggestion = suggestArea(plays, statuses, now, AppLanguage.SPANISH)
          SuggestionHeader(suggestion)
          AreaGrid(statuses, playedToday = setOf("MEMORIA"), suggested = suggestion.key, lang = AppLanguage.SPANISH, onArea = {}, modifier = Modifier.weight(1f))
          NeuroNavBar(current = AppTab.JUEGOS, onSelect = {})
        }
      }
    }
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/juegos-areas.png")
  }

  @Test
  fun library() {
    val points = listOf(31f, 27f, 29f, 24f, 22f, 18f).mapIndexed { i, v -> MeasurePoint(now - (6 - i) * day, "homing", v) }
    val progress = mapOf("bitacora" to 0.62f, "rumbo" to 0.45f, "secuencia" to 0.75f, "parejas" to 0.40f, "correo" to 0.33f)
    val games = GameRegistry.allGames.filter { it.domain == DomainType.MEMORIA }
      .sortedBy { com.example.data.StarMeasures.defForGame(it.id) == null }
    val cards = games.map { g ->
      val last = if (g.id == "correo") null else now - (if (g.id == "rumbo") 0 else 2) * day
      cardData(g, g.title, last, now, progress[g.id], if (g.id == "rumbo") points else emptyList(), 4)
    }
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          AreaWindow(DomainType.MEMORIA, "Memoria", statuses[0], cards, focusGameId = null, onClose = {}, onGame = {})
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
        Box(Modifier.width(412.dp).background(Color(0xFF050823)).padding(14.dp)) {
          GameSheetContent(data, AgeBand.SENIOR, expertOpen = false, choice = PlayMode.DESAFIO, onChoose = {}, onPlay = {})
        }
      }
    }
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/library-ficha.png")
  }
}
