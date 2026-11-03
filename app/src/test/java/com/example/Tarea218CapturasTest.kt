package com.example

import androidx.activity.ComponentActivity
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Comparison
import com.example.data.EstimatedReference
import com.example.data.PlayMode
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialState
import com.example.data.TimeTrialStats
import com.example.data.Week
import com.example.games.TimeTrialResultScreen
import com.example.model.AgeBand
import com.example.model.AgeRange
import com.example.model.AppLanguage
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NeuroNavBar
import com.example.ui.components.SlabColor
import com.example.ui.screens.ContrarrelojPage
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeUi
import com.example.ui.screens.PlusLockSheetContent
import com.example.ui.screens.cardData
import com.example.ui.screens.homeStops
import com.example.ui.screens.sheetTimeTrialOf
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assume
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.util.Calendar

/**
 * La lámina de la Tarea 218 (`docs/previews/tarea-218.png`, `tools/previews/tarea_218.py`): la regla de partidas del Contrarreloj. La ficha del juego de hoy con partidas, la de otro juego (gris con candado) y la del juego de hoy sin partidas; la pestaña con partidas y sin ellas; Hoy sin partidas (sola y con
 * marca nueva); el final sin «Otra vez»; y la hoja del candado con sus cuatro líneas. Lo del emulador (letra 2,0 a 393 y 360 dp) son capturas del `medium_phone`. No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea218CapturasTest*"`); los PNG van a `src/test/screenshots/t218/` (fuera de git).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea218CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private val today = "2026-10-26"                                       // ese día el Contrarreloj de hoy es Tinta o Palabra
  private fun at(d: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, d, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(mark: Int, d: Int, errors: Int = 2, game: String = "stroop", day: String = "2026-10-%02d".format(d)) =
    TimeTrialResult(id = "r-$game-$d-$mark", gameId = game, day = day, trackVersion = TimeTrialDay.trackVersion(game), hits = mark + errors, errors = errors, level = 4, seconds = 120, timestamp = at(d))

  /** Marcas de semanas anteriores y [n] partidas de HOY (26 de octubre). */
  private fun results(n: Int) = listOf(r(31, 8), r(35, 10), r(39, 13), r(36, 15), r(44, 17), r(38, 19, 3), r(41, 22, 3)) + (1..n).map { r(36 + it, 26, 3) }

  private val now = at(26, 18)

  private val estWorld: Comparison.Source by lazy {
    val dir = java.io.File("src/main/assets/${EstimatedReference.ASSET_DIR}")
    Comparison.ComposedSource(null, EstimatedReference.of(dir.listFiles { f -> f.extension == "json" }.orEmpty().sortedBy { it.name }.map { it.readText(Charsets.UTF_8) }))
  }

  private var fontState by mutableStateOf(1f)
  private var barPx by mutableIntStateOf(0)

  private fun frame(widthDp: Int, heightDp: Int, scale: Float = 1f, content: @androidx.compose.runtime.Composable () -> Unit) {
    rule.mainClock.autoAdvance = true
    fontState = scale
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontState), LocalReduceMotion provides true, LocalBarInset provides 88.dp) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823))) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t218/$name.png") }

  // ---------------------------------------------------------------- la ficha

  private fun sheet(gameId: String, plus: Boolean, n: Int, name: String) {
    frame(393, 620) {
      val g = GameRegistry.getById(gameId)!!
      Box(Modifier.fillMaxSize().padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
        Box(Modifier.fillMaxWidth()) {
          GameSheetContent(cardData(g, g.title, 0.55f), AgeBand.ADULT, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, onClose = {}, timeTrial = sheetTimeTrialOf(gameId, g.title, 41, today, SlabColor.LILAC, plus = plus, results = results(n)), onTimeTrial = {})
        }
      }
    }
    save(name)
  }

  @Test fun ficha_hoy_con_partidas_393_10() = sheet("stroop", false, 1, "ficha_hoy_con_partidas_393_10")
  @Test fun ficha_gris_393_10() = sheet("disparate", false, 0, "ficha_gris_393_10")
  @Test fun ficha_sin_partidas_393_10() = sheet("stroop", false, 3, "ficha_sin_partidas_393_10")

  // ---------------------------------------------------------------- la pestaña

  private fun tab(gamesLeft: Int?, name: String, scroll: Boolean = false) {
    frame(393, 873) {
      Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) {
        val inset = with(LocalDensity.current) { if (barPx > 0) barPx.toDp() else 88.dp }
        CompositionLocalProvider(LocalBarInset provides inset) {
          Box(Modifier.padding(top = 24.dp)) { ContrarrelojPage(results = results(if (gamesLeft == 0) 3 else 1), selected = "stroop", onSelect = {}, now = now, compare = estWorld, ageRange = AgeRange.R45_54, plus = false, today = "stroop", gamesLeft = gamesLeft) }
        }
        Box(Modifier.align(Alignment.BottomCenter).onSizeChanged { if (it.height > 0) barPx = it.height }) { NeuroNavBar(current = AppTab.CONTRARRELOJ, onSelect = {}) }
      }
    }
    save(name)
  }

  @Test fun pestana_con_partidas_393_10() = tab(2, "pestana_con_partidas_393_10")
  @Test fun pestana_sin_partidas_393_10() = tab(0, "pestana_sin_partidas_393_10")

  // ---------------------------------------------------------------- Hoy

  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  private fun home(t: HomeTimeTrial, name: String) {
    val ui = HomeUi(name = "Ricardo", now = at(26, 16), tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps, stops = homeStops(listOf("bodega", "calculo", "intrusa"), 3, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true, timeTrial = t)
    frame(393, 873) {
      Box(Modifier.fillMaxSize()) {
        HomeContent(ui = ui, onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {}, onLocked = {}, modifier = Modifier.padding(top = 24.dp, bottom = 100.dp))
        Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = AppTab.HOY, onSelect = {}) }
      }
    }
    save(name)
  }

  @Test fun hoy_sin_partidas_393_10() = home(HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.JUGADO, todayHits = 38, record = 41, gamesLeft = 0, tomorrowTitle = "¿Verdad o disparate?"), "hoy_sin_partidas_393_10")
  @Test fun hoy_marca_nueva_sin_partidas_393_10() = home(HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.MARCA_NUEVA, todayHits = 43, record = 43, gamesLeft = 0, tomorrowTitle = "¿Verdad o disparate?"), "hoy_marca_nueva_sin_partidas_393_10")

  // ---------------------------------------------------------------- el final y la hoja

  @Test fun final_sin_partidas_393_10() {
    val all = results(3).filter { it.gameId == "stroop" }
    val outcome = TimeTrialStats.outcome(all, all.last(), now)
    frame(393, 873) { Box(Modifier.fillMaxSize()) { TimeTrialResultScreen(outcome = outcome, onAgain = {}, onDone = {}, soundEnabled = false, plus = false, onAllMarks = null, canAgain = false) } }
    save("final_sin_partidas_393_10")
  }

  @Test fun hoja_393_10() {
    frame(393, 700) { Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) { PlusLockSheetContent(onClose = {}, kind = Comparison.Kind.ESTIMATED, onSource = {}) } }
    save("hoja_393_10")
  }
}
