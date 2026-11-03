package com.example

import androidx.activity.ComponentActivity
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
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
import com.example.data.GuideStep
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.GuideTargets
import com.example.ui.components.HoyGuideOverlay
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalGuideTargets
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NeuroNavBar
import com.example.ui.components.hiddenUntil
import com.example.ui.screens.AllGamesContent
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeUi
import com.example.ui.screens.homeStops
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
 * La lámina de la Tarea 220 (`docs/previews/tarea-220.png`, `tools/previews/tarea_220.py`): la barra de cinco pestañas (Hoy · Juegos · Contrarreloj · Avance · Tú) a 393 y 360 dp con letra 1,0 y 1,3, la pestaña Juegos, Hoy sin la losa «Todos los juegos» (ruta a medias y ruta completa)
 * y el paso 2 de la guía del primer ingreso (la columna Juegos de la barra). Lo del emulador (letra 2,0 a 393 y 360 dp) son capturas del `medium_phone`.
 * No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea220CapturasTest*"`); las deja en `app/src/test/screenshots/t220/` (fuera de git).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea220CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun at(d: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, d, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private var fontState by mutableStateOf(1f)

  private fun frame(widthDp: Int, heightDp: Int, scale: Float = 1f, barInset: Int = 88, targets: GuideTargets? = null, content: @androidx.compose.runtime.Composable () -> Unit) {
    rule.mainClock.autoAdvance = true
    fontState = scale
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontState), LocalReduceMotion provides true, LocalBarInset provides barInset.dp, LocalGuideTargets provides targets) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823))) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t220/$name.png") }

  // ---------------------------------------------------------------- la barra de cinco

  private fun bar(widthDp: Int, scale: Float, name: String, current: AppTab = AppTab.JUEGOS) {
    frame(widthDp, 120, scale) { Box(Modifier.fillMaxSize()) { Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = current, onSelect = {}) } } }
    save(name)
  }

  @Test fun barra_393_10() = bar(393, 1f, "barra_393_10")
  @Test fun barra_393_13() = bar(393, 1.3f, "barra_393_13")
  @Test fun barra_360_10() = bar(360, 1f, "barra_360_10")
  @Test fun barra_360_13() = bar(360, 1.3f, "barra_360_13")
  @Test fun barra_hoy_360_13() = bar(360, 1.3f, "barra_hoy_360_13", AppTab.HOY)

  // ---------------------------------------------------------------- la pestaña Juegos

  @Test fun juegos_393_10() {
    val now = 400 * 86_400_000L + 3_600_000L
    frame(393, 873) {
      Box(Modifier.fillMaxSize()) {
        AllGamesContent(places = PlaceTestData.places(now), area = null, query = "", mark = null, onQueryChange = {}, onArea = {}, onGame = {}, modifier = Modifier.padding(top = 24.dp))
        Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = AppTab.JUEGOS, onSelect = {}) }
      }
    }
    save("juegos_393_10")
  }

  // ---------------------------------------------------------------- Hoy sin la losa «Todos los juegos»

  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, record = 41)

  private fun ui(completed: Int) = HomeUi(
    name = "Ricardo", now = at(26, 16), tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(listOf("bodega", "calculo", "intrusa"), completed, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true, timeTrial = stroop
  )

  private fun hoy(completed: Int, name: String, width: Int = 393, height: Int = 873, scale: Float = 1f) {
    frame(width, height, scale) {
      Box(Modifier.fillMaxSize()) {
        HomeContent(ui = ui(completed), onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {}, onLocked = {}, modifier = Modifier.padding(top = 24.dp, bottom = 100.dp))
        Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = AppTab.HOY, onSelect = {}) }
      }
    }
    save(name)
  }

  @Test fun hoy_ruta_a_medias_393_10() = hoy(1, "hoy_ruta_a_medias_393_10")
  @Test fun hoy_ruta_completa_393_10() = hoy(3, "hoy_ruta_completa_393_10")
  @Test fun hoy_ruta_a_medias_393_13() = hoy(1, "hoy_ruta_a_medias_393_13", scale = 1.3f)
  @Test fun hoy_ruta_a_medias_360_10() = hoy(1, "hoy_ruta_a_medias_360_10", 360, 780)

  // ---------------------------------------------------------------- la guía, paso 2: la columna Juegos

  private fun guia(step: GuideStep, name: String, scale: Float = 1f) {
    val targets = GuideTargets()
    frame(393, 873, scale, targets = targets) {
      Box(Modifier.fillMaxSize()) {
        Box(Modifier.fillMaxSize().hiddenUntil(false)) {
          HomeContent(ui = ui(1), onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {}, modifier = Modifier.padding(top = 24.dp, bottom = 100.dp))
          Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = AppTab.HOY, onSelect = {}) }
        }
        HoyGuideOverlay(step = step, targets = targets, onNext = {}, onSkip = {})
      }
    }
    save(name)
  }

  @Test fun guia_paso1_393_10() = guia(GuideStep.CONTRARRELOJ, "guia_paso1_393_10")
  @Test fun guia_paso2_393_10() = guia(GuideStep.JUEGOS, "guia_paso2_393_10")
  @Test fun guia_paso3_393_10() = guia(GuideStep.BARRA, "guia_paso3_393_10")
  @Test fun guia_paso2_393_13() = guia(GuideStep.JUEGOS, "guia_paso2_393_13", 1.3f)
}
