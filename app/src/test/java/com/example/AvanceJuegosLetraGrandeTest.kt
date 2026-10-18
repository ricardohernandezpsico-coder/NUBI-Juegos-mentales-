package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.AchievementFamily
import com.example.data.Achievements
import com.example.data.AreaStatus
import com.example.data.MeasurePoint
import com.example.data.StarMeasures
import com.example.data.Week
import com.example.data.computeAchievementStats
import com.example.data.familyState
import com.example.data.leagueRow
import com.example.data.weekSummary
import com.example.model.AppLanguage
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.screens.AreaGrid
import com.example.ui.screens.AreaWindow
import com.example.ui.screens.AvanceAreas
import com.example.ui.screens.AvanceDiscoveries
import com.example.ui.screens.AvanceLeague
import com.example.ui.screens.AvanceMedals
import com.example.ui.screens.AvanceWeek
import com.example.ui.screens.cardData
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * Avance y Juegos con letra grande (Tarea 125): con la letra del sistema al 130 % ningún nombre se corta ni se parte una palabra, y todo lo tocable sigue en su sitio. Fotografía las pantallas para mirarlas (`src/test/screenshots`, los `-letra13.png`)
 * y comprueba que lo importante está a la vista; lo que se parte o se corta se ve en la foto.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class AvanceJuegosLetraGrandeTest {
  @get:Rule val rule = createComposeRule()

  private val day = 86_400_000L
  private val now = 400 * day + 3_600_000L
  private val statuses = listOf(
    AreaStatus("MEMORIA", 0.52f, 0.06f, listOf(0.40f, 0.45f, 0.46f, 0.52f)),
    AreaStatus("ATENCION", 0.62f, -0.02f, listOf(0.60f, 0.64f, 0.64f, 0.62f)),
    AreaStatus("RAZONAMIENTO", 0.43f, 0f, listOf(0.43f, 0.43f, 0.43f, 0.43f)),
    AreaStatus("LENGUAJE", null, 0f, listOf(null, null, null, null))
  )
  private val names = DomainType.values().associate { it.name to it.displayName }

  private fun big(content: @androidx.compose.runtime.Composable () -> Unit) = rule.setContent {
    CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, fontScale = 1.3f)) { NeuroVidaTheme { content() } }
  }

  @Test
  fun grilla_de_juegos() {
    big {
      Column(Modifier.width(393.dp).height(873.dp).background(Color(0xFF050823))) {
        AreaGrid(statuses, playedToday = setOf("MEMORIA"), suggested = "RAZONAMIENTO", lang = AppLanguage.SPANISH, onArea = {}, modifier = Modifier.weight(1f))
      }
    }
    rule.onNodeWithTag("area_razonamiento").assertIsDisplayed()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/juegos-areas-letra13.png")
  }

  @Test
  fun ventana_de_un_area() {
    val progress = mapOf("secuencia" to 0.75f, "parejas" to 0.40f, "correo" to 0.33f)
    val games = GameRegistry.allGames.filter { it.domain == DomainType.MEMORIA }
    val cards = games.mapIndexed { i, g -> cardData(g, g.title, if (g.id == "correo") null else now - (1 + i) * day, now, progress[g.id], emptyList(), 3).copy(routeStop = if (i == 0) 1 else null, routeDone = i == 0) }
    big {
      Box(Modifier.width(393.dp).height(873.dp).background(Color(0xFF050823))) {
        AreaWindow(
          DomainType.MEMORIA, "Memoria", statuses[0], cards, focusGameId = null, onClose = {}, onGame = {},
          sections = listOf("En tu ruta de hoy" to cards.take(1), "Hace días que no juegas" to cards.drop(1).take(2), "Nuevo para ti" to cards.drop(3))
        )
      }
    }
    rule.onNodeWithTag("area_window").assertIsDisplayed()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/library-letra13.png")
  }

  @Test
  fun avance() {
    val history = listOf("secuencia", "stroop", "calculo").mapIndexed { i, id ->
      com.example.model.GamePlayResult(gameId = id, score = 70, correctAnswers = 5, totalTrials = 10, timed = false, level = 1, timestamp = now - i * day)
    }
    val stats = computeAchievementStats(history, emptyMap())
    val unlocked = Achievements.unlocked(stats)
    val medals = AchievementFamily.entries.map { Achievements.familyState(it, unlocked, stats) }
    val lamps = Week.Lamps(listOf(true, true, false, true, false, false, false), todayIndex = 3)
    val summary = weekSummary(lamps, goal = 4, streak = 3, areas = 2, noGamesAtAll = false)
    val points = listOf(31f, 27f, 29f, 24f, 22f, 18f).mapIndexed { i, v -> MeasurePoint(now - (6 - i) * day, "numline", v) } +
      listOf(210f, 200f, 190f).mapIndexed { i, v -> MeasurePoint(now - (3 - i) * day, "glance", v) }
    val cards = StarMeasures.cards(points, now)
    big {
      Column(Modifier.width(393.dp).background(Color(0xFF0B0C33)).verticalScroll(rememberScrollState()).padding(horizontal = 22.dp)) {
        AvanceWeek(summary, lamps, "Descansaste el martes: tu racha sigue.")
        AvanceAreas(statuses, names) {}
        AvanceDiscoveries(cards, null, points, onGame = {}, onPlay = {})
        AvanceMedals(medals) {}
        AvanceLeague(leagueRow(1240)) {}
      }
    }
    rule.onNodeWithTag("avance_week").assertIsDisplayed()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/avance-letra13.png")
  }
}
