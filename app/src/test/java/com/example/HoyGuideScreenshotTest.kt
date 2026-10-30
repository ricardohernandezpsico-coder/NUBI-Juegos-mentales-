package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Achievements
import com.example.data.GuideStep
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.AchievementOverlay
import com.example.ui.components.BoardBackground
import com.example.ui.components.GuideTargets
import com.example.ui.components.HoyGuideOverlay
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalGuideTargets
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NeuroNavBar
import com.example.ui.components.hiddenUntil
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
 * La lámina «hoy | propuesta» de la Tarea 193 (`docs/previews/tarea-193.png`): Hoy el día del Primer vuelo (la tarjeta con los juegos de la cola de antes y con los 4 del vuelo), los tres pasos de la guía de Nubi y Hoy después, con letra 1,0 y 1,3, y la medalla única. Capturas REALES de los composables de la app
 * (el Hoy de verdad, la barra real y el velo) a 393 × 873 dp. No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*HoyGuideScreenshotTest*"`); deja los PNG en `src/test/screenshots/guia/` y `tools/previews/tarea_193.py` arma la hoja.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class HoyGuideScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 30, 16, 30, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(false, false, false, false, true, false, false), todayIndex = 4)
  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, todayHits = null, record = null)
  private val queue = listOf("bodega", "calculo", "intrusa")                                    // lo que Hoy mostraba antes: la cola que armó DailyPath, no lo jugado
  private val flight = listOf("secuencia", "freno", "aterrizaje", "meteoros")                    // lo que se jugó en el vuelo, en su orden

  private fun ui(ids: List<String>) = HomeUi(
    name = "Ana", now = now, tier = RankTier.fromRating(0), streak = 1, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(ids, ids.size, AppLanguage.SPANISH), pausedTitle = null, flightDay = true, hasBaseline = true, timeTrial = stroop
  )

  /** Hoy con su barra, como los aloja `NeuroVidaApp` (las barras del sistema simuladas por margen: 24 dp arriba y abajo), y la guía encima si [step] no es null. */
  private fun shot(file: String, fontScale: Float, ids: List<String>, step: GuideStep? = null) {
    val targets = GuideTargets()
    composeTestRule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides true, LocalGuideTargets provides targets) {
        NeuroVidaTheme {
          Box(Modifier.fillMaxSize()) {
            BoardBackground()
            Box(Modifier.fillMaxSize().padding(top = 24.dp, bottom = 24.dp).hiddenUntil(step == null)) {
              CompositionLocalProvider(LocalBarInset provides (if (fontScale >= 1.3f) 95.dp else 88.dp)) {
                HomeContent(ui = ui(ids), onAllGames = {}, onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {})
              }
              Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = AppTab.HOY, onSelect = {}) }
            }
            if (step != null) HoyGuideOverlay(step = step, targets = targets, onNext = {}, onSkip = {})
          }
        }
      }
    }
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/guia/$file.png")
  }

  @Test fun hoy_10() = shot("hoy_10", 1f, queue)
  @Test fun hoy_13() = shot("hoy_13", 1.3f, queue)
  @Test fun propuesta_10() = shot("propuesta_10", 1f, flight)
  @Test fun propuesta_13() = shot("propuesta_13", 1.3f, flight)
  @Test fun paso1_10() = shot("paso1_10", 1f, flight, GuideStep.CONTRARRELOJ)
  @Test fun paso2_10() = shot("paso2_10", 1f, flight, GuideStep.TODOS)
  @Test fun paso3_10() = shot("paso3_10", 1f, flight, GuideStep.BARRA)
  @Test fun paso1_13() = shot("paso1_13", 1.3f, flight, GuideStep.CONTRARRELOJ)
  @Test fun paso2_13() = shot("paso2_13", 1.3f, flight, GuideStep.TODOS)
  @Test fun paso3_13() = shot("paso3_13", 1.3f, flight, GuideStep.BARRA)

  /** La medalla única que se celebra al terminar el vuelo: «Primer paso» (con las otras dos ya ganadas: «3 de 15 medallas»). */
  @Test
  fun medalla_unica() {
    val def = Achievements.byId("primer_paso")!!
    composeTestRule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.fillMaxSize()) {
            BoardBackground()
            AchievementOverlay(def = def, unlockedCount = 3, totalCount = Achievements.all.size, streak = 1, onDismiss = {}, delayMs = 0L, soundEnabled = false)
          }
        }
      }
    }
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/guia/medalla_unica.png")
  }
}
