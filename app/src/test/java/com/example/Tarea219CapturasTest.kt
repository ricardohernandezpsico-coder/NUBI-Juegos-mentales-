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
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.PlayMode
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.model.AgeBand
import com.example.model.AppLanguage
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NeuroNavBar
import com.example.ui.components.SlabColor
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeUi
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
 * La lámina de la Tarea 219 (`docs/previews/tarea-219.png`, `tools/previews/tarea_219.py`): Hoy reordenada (ruta · «Todos los juegos» · Contrarreloj en celeste) en sus estados, con y sin Nubilus Plus, y la ficha con el botón celeste. Lo del emulador (letra 2,0 a 393 y 360 dp) son capturas del `medium_phone`.
 * No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea219CapturasTest*"`); las deja en `app/src/test/screenshots/t219/` (fuera de git).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea219CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private val today = "2026-10-26"                                       // ese día el Contrarreloj de hoy es Tinta o Palabra
  private fun at(d: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, d, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(mark: Int, d: Int, errors: Int = 2, game: String = "stroop") =
    TimeTrialResult(id = "r-$game-$d-$mark", gameId = game, day = "2026-10-%02d".format(d), trackVersion = TimeTrialDay.trackVersion(game), hits = mark + errors, errors = errors, level = 4, seconds = 120, timestamp = at(d))

  private fun results(n: Int) = listOf(r(31, 8), r(35, 10), r(39, 13), r(36, 15), r(44, 17), r(38, 19, 3), r(41, 22, 3)) + (1..n).map { r(36 + it, 26, 3) }

  private var fontState by mutableStateOf(1f)

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

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t219/$name.png") }

  // ---------------------------------------------------------------- Hoy

  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  private fun home(t: HomeTimeTrial, completed: Int, name: String, width: Int = 393, height: Int = 873, scale: Float = 1f) {
    val ui = HomeUi(name = "Ricardo", now = at(26, 16), tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps, stops = homeStops(listOf("bodega", "calculo", "intrusa"), completed, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true, timeTrial = t)
    frame(width, height, scale) {
      Box(Modifier.fillMaxSize()) {
        HomeContent(ui = ui, onAllGames = {}, onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {}, onLocked = {}, modifier = Modifier.padding(top = 24.dp, bottom = 100.dp))
        Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = AppTab.HOY, onSelect = {}) }
      }
    }
    save(name)
  }

  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, record = 41)
  private val jugado = stroop.copy(state = TimeTrialState.JUGADO, todayHits = 38)
  private val marca = stroop.copy(state = TimeTrialState.MARCA_NUEVA, todayHits = 43, record = 43)
  private val verdad = "¿Verdad o disparate?"

  // con Plus
  @Test fun hoy_ruta_a_medias_393_10() = home(stroop, 1, "hoy_ruta_a_medias_393_10")
  @Test fun hoy_ruta_completa_393_10() = home(stroop, 3, "hoy_ruta_completa_393_10")
  @Test fun hoy_jugado_393_10() = home(jugado, 3, "hoy_jugado_393_10")
  @Test fun hoy_marca_nueva_393_10() = home(marca, 3, "hoy_marca_nueva_393_10")

  // sin Plus
  @Test fun hoy_sin_plus_ruta_a_medias_393_10() = home(stroop.copy(gamesLeft = 3), 1, "hoy_sin_plus_ruta_a_medias_393_10")
  @Test fun hoy_sin_plus_jugado_393_10() = home(jugado.copy(gamesLeft = 2), 3, "hoy_sin_plus_jugado_393_10")
  @Test fun hoy_sin_partidas_393_10() = home(jugado.copy(gamesLeft = 0, tomorrowTitle = verdad), 3, "hoy_sin_partidas_393_10")
  @Test fun hoy_marca_nueva_sin_partidas_393_10() = home(marca.copy(gamesLeft = 0, tomorrowTitle = verdad), 3, "hoy_marca_nueva_sin_partidas_393_10")

  // con letra 1,3 y a 360 dp
  @Test fun hoy_ruta_a_medias_393_13() = home(stroop, 1, "hoy_ruta_a_medias_393_13", scale = 1.3f)
  @Test fun hoy_ruta_a_medias_360_10() = home(stroop, 1, "hoy_ruta_a_medias_360_10", 360, 780)
  @Test fun hoy_sin_partidas_360_10() = home(jugado.copy(gamesLeft = 0, tomorrowTitle = verdad), 3, "hoy_sin_partidas_360_10", 360, 780)

  // ---------------------------------------------------------------- la ficha (el botón del Contrarreloj pasa de lila a celeste)

  private fun sheet(gameId: String, plus: Boolean, n: Int, name: String) {
    frame(393, 620) {
      val g = GameRegistry.getById(gameId)!!
      Box(Modifier.fillMaxSize().padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
        Box(Modifier.fillMaxWidth()) {
          GameSheetContent(cardData(g, g.title, 0.55f), AgeBand.ADULT, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, onClose = {}, timeTrial = sheetTimeTrialOf(gameId, g.title, 41, today, SlabColor.SKY, plus = plus, results = results(n)), onTimeTrial = {})
        }
      }
    }
    save(name)
  }

  @Test fun ficha_hoy_con_partidas_393_10() = sheet("stroop", false, 1, "ficha_hoy_con_partidas_393_10")
  @Test fun ficha_con_plus_393_10() = sheet("disparate", true, 0, "ficha_con_plus_393_10")
  @Test fun ficha_gris_393_10() = sheet("disparate", false, 0, "ficha_gris_393_10")
}
