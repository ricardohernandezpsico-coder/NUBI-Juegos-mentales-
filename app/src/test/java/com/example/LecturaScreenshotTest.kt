package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.hasScrollAction
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onFirst
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.test.swipeUp
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Achievements
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.data.GuideStep
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.model.AgeBand
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.games.GameResultScreen
import com.example.model.GamePlayResult
import com.example.ui.components.AchievementOverlay
import com.example.ui.components.BoardBackground
import com.example.ui.components.GuideTargets
import com.example.ui.components.HoyGuideOverlay
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalGuideTargets
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NeuroNavBar
import com.example.ui.components.hiddenUntil
import com.example.ui.screens.FirstFlightScreen
import com.example.ui.screens.FlightActions
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
 * La lámina «hoy | propuesta» de la Tarea 194 (`docs/previews/tarea-194.png`): tres arreglos de lectura que pidió Ricardo tras probar la T193b en el teléfono: el amarillo del texto del Primer vuelo («¿Sabías que…?» y las preguntas), la línea de la tarjeta del día del vuelo más arriba y el texto del globo
 * de la guía más oscuro y firme. No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*LecturaScreenshotTest*"`). El prefijo (`hoy` = el código de antes, `prop` = el nuevo) sale de la variable de entorno T194_PREFIX; los PNG van a `src/test/screenshots/lectura/`
 * y `tools/previews/tarea_194.py` arma la hoja.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class LecturaScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private val prefix = System.getenv("T194_PREFIX") ?: "prop"

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun out(name: String) = "src/test/screenshots/lectura/${prefix}_$name.png"

  /** Una página del Primer vuelo, a 393 × 873 dp con «quitar animaciones» (todo en su estado final). */
  private fun flightPage(name: String, state: FlightState) {
    composeTestRule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.fillMaxSize()) { BoardBackground(); FirstFlightScreen(state, emptyList(), FlightActions.None) } }
      }
    }
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = out(name))
  }

  @Test fun dato() = flightPage("dato", FlightState(FlightMode.FULL, FlightStep.DATO, name = "Ana", age = AgeBand.ADULT))

  @Test fun pregunta() = flightPage("pregunta", FlightState(FlightMode.FULL, FlightStep.EDAD, name = "Ana", measured = mapOf("secuencia" to 0.62f), phrases = mapOf("secuencia" to "Repetiste bien un rastro de 5 luces.")))

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 30, 16, 30, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(false, false, false, false, true, false, false), todayIndex = 4)
  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, todayHits = null, record = null)
  private val flight = listOf("secuencia", "freno", "aterrizaje", "meteoros")

  private fun ui() = HomeUi(
    name = "Ana", now = now, tier = RankTier.fromRating(0), streak = 1, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(flight, flight.size, AppLanguage.SPANISH), pausedTitle = null, flightDay = true, hasBaseline = true, timeTrial = stroop
  )

  /** Hoy con su barra (y la guía en [step], si no es null) como los aloja `NeuroVidaApp`; las barras del sistema simuladas por margen. */
  private fun hoy(name: String, fontScale: Float, step: GuideStep? = null) {
    val targets = GuideTargets()
    composeTestRule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides true, LocalGuideTargets provides targets) {
        NeuroVidaTheme {
          Box(Modifier.fillMaxSize()) {
            BoardBackground()
            Box(Modifier.fillMaxSize().padding(top = 24.dp, bottom = 24.dp).hiddenUntil(step == null)) {
              CompositionLocalProvider(LocalBarInset provides (if (fontScale >= 1.3f) 95.dp else 88.dp)) {
                HomeContent(ui = ui(), onAllGames = {}, onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {})
              }
              Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = AppTab.HOY, onSelect = {}) }
            }
            if (step != null) HoyGuideOverlay(step = step, targets = targets, onNext = {}, onSkip = {})
          }
        }
      }
    }
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = out(name))
  }

  @Test fun tarjeta_10() = hoy("tarjeta_10", 1f)
  @Test fun paso1_10() = hoy("paso1_10", 1f, GuideStep.CONTRARRELOJ)
  @Test fun paso1_13() = hoy("paso1_13", 1.3f, GuideStep.CONTRARRELOJ)

  /** La medalla única del vuelo («Medalla nueva» y «Primer paso»: las dos letras amarillas de la celebración). */
  @Test
  fun medalla() {
    val def = Achievements.byId("primer_paso")!!
    composeTestRule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.fillMaxSize()) { BoardBackground(); AchievementOverlay(def = def, unlockedCount = 3, totalCount = Achievements.all.size, streak = 1, onDismiss = {}, delayMs = 0L, soundEnabled = false) } }
      }
    }
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = out("medalla"))
  }

  /** Un final de partida con «Tu colección» (Lluvia de meteoros con palabras raras): el título amarillo del detalle y el resaltado de la cifra. */
  @Test
  fun coleccion() {
    val result = GamePlayResult(
      gameId = "meteoros", score = 78, correctAnswers = 54, totalTrials = 62, timed = true, level = 6,
      lexBandSeen = listOf(10, 10, 10, 10, 10, 10), lexBandHits = listOf(10, 10, 10, 10, 9, 7), lexFaSeen = listOf(7, 8, 6), lexFaHits = listOf(0, 1, 4),
      lexRtCommonMs = 700, lexRtRareMs = 1100, lexRareWords = listOf("efímero", "umbral", "inefable", "cántaro", "alba")
    )
    composeTestRule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.fillMaxSize()) { BoardBackground(); GameResultScreen(result = result, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3, onPlayAgain = {}, onContinue = {}) } }
      }
    }
    composeTestRule.waitForIdle()
    repeat(2) { composeTestRule.onAllNodes(hasScrollAction()).onFirst().performTouchInput { swipeUp(durationMillis = 100) }; composeTestRule.waitForIdle() }
    composeTestRule.onNodeWithText("Ver el detalle de tu partida").performClick()                 // el detalle va plegado: «Tu colección» está adentro
    composeTestRule.waitForIdle()
    composeTestRule.onNodeWithText("Tu colección").performScrollTo()
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = out("coleccion"))
  }
}
