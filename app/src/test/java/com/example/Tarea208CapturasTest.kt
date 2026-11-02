package com.example

import androidx.activity.ComponentActivity
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performSemanticsAction
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Comparison
import com.example.data.EstimatedReference
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialStats
import com.example.games.TimeTrialResultScreen
import com.example.model.AgeRange
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.ContrarrelojPage
import com.example.ui.theme.NeuroVidaTheme
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
 * La lámina de la Tarea 208 (`docs/previews/tarea-208.png`, `tools/previews/tarea_208.py`): Rescate relámpago cuenta por CÁPSULA (cada cápsula bien marcada suma 1 acierto y cada cápsula marcada que no estaba resta 1 error; la escalera sigue por rondas logradas).
 * El final del Contrarreloj de Rescate con la marca nueva y el detalle abierto (la frase «En Rescate relámpago, cada cápsula bien marcada es un acierto…» en 16 sp y, al pie, el récord de la pista 2 como «pista anterior») con letra 1,0 y 2,0 (el detalle es parte de la pantalla, no un `Dialog`:
 * Roborazzi mide la letra de verdad), y la página de Rescate con la escalera de cinco peldaños y la campana de 0 a 45 en 18 a 29 y en 75 años o más, con una marca de la pista 2 como «pista anterior». La escalera a 2,0 sale del EMULADOR.
 * No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea208CapturasTest*"`); los PNG van a `src/test/screenshots/t208/`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea208CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun at(d: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, d, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(hits: Int, errors: Int, d: Int, version: Int = TimeTrialDay.trackVersion("radar"), level: Int = 4) =
    TimeTrialResult(id = "r-radar-$d-$hits-$version", gameId = "radar", day = "2026-10-%02d".format(d), trackVersion = version, hits = hits, errors = errors, level = level, seconds = 120, timestamp = at(d))

  private val now = at(28, 18)

  /** La referencia estimada REAL del APK, como la ve la tienda. */
  private val estWorld: Comparison.Source by lazy {
    val dir = java.io.File("src/main/assets/${EstimatedReference.ASSET_DIR}")
    Comparison.ComposedSource(null, EstimatedReference.of(dir.listFiles { f -> f.extension == "json" }.orEmpty().sortedBy { it.name }.map { it.readText(Charsets.UTF_8) }))
  }

  // ---------------------------------------------------------------- el final del Contrarreloj de Rescate

  /** Una partida de 34 aciertos y 3 errores (marca 31) con marca nueva: el récord de la pista 3 era 28; y un récord de la pista 2 (12, con la regla de rondas) como pista anterior. */
  private val finalOutcome: TimeTrialStats.Outcome get() {
    val old = r(12, 0, 20, version = 2)
    val history = listOf(r(27, 2, 22), r(30, 2, 24), r(28, 1, 25), r(26, 1, 26))
    val today = r(34, 3, 27, level = 5)
    return TimeTrialStats.outcome(listOf(old) + history + today, today)
  }

  private fun finalScreen(name: String, scale: Float, toTag: String, widthDp: Int = 393, heightDp: Int = 873) {
    rule.mainClock.autoAdvance = true
    val outcome = finalOutcome
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823))) { TimeTrialResultScreen(outcome = outcome, onAgain = {}, onDone = {}, soundEnabled = false) } }
      }
    }
    rule.waitForIdle()
    rule.onNodeWithTag("detail_toggle").performScrollTo().performClick()
    rule.waitForIdle()
    rule.onNodeWithTag(toTag, useUnmergedTree = true).performScrollTo()
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t208/ahora_$name.png")
  }

  @Test fun a_final_393_10() = finalScreen("final_393_10", 1f, "timetrial_detail_5")                 // con 1,0 todo el detalle cabe: la frase de Rescate y, al pie, la pista anterior
  @Test fun b_final_393_20() = finalScreen("final_393_20", 2f, "timetrial_detail_2")                 // con 2,0 hay que deslizar: primero la frase,
  @Test fun b2_final_393_20_pie() = finalScreen("final_393_20_pie", 2f, "timetrial_detail_5")       // y después el pie (la pista anterior)

  // ---------------------------------------------------------------- la página de Rescate

  private fun scrollTo(tag: String, marginDp: Int = 12) {
    val y = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
    rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, y - marginDp * 3f) }     // xxhdpi: 3 px por dp
    rule.waitForIdle()
  }

  private fun page(name: String, range: AgeRange, mark: Int, to: String) {
    rule.mainClock.autoAdvance = true
    // 3 partidas de la pista 3 (la última, de esta semana, con la marca pedida) y una marca de la pista 2 como pista anterior
    val results = listOf(r(14, 0, 5, version = 2), r(mark - 6, 2, 14), r(mark - 3, 1, 21), r(mark + 2, 2, 27))
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(873.dp)) {
            BoardBackground()
            ContrarrelojPage(results = results, selected = "radar", onSelect = {}, now = now, compare = estWorld, ageRange = range)
          }
        }
      }
    }
    rule.waitForIdle()
    scrollTo(to)
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t208/ahora_$name.png")
  }

  @Test fun c_escalera_18_29_393_10() = page("ladder_18_29_393_10", AgeRange.R18_29, 26, "contrarreloj_ladder")
  @Test fun d_campana_18_29_393_10() = page("bell_18_29_393_10", AgeRange.R18_29, 26, "contrarreloj_bell")
  @Test fun e_escalera_75_393_10() = page("ladder_75_393_10", AgeRange.R75_PLUS, 19, "contrarreloj_ladder")
  @Test fun f_campana_75_393_10() = page("bell_75_393_10", AgeRange.R75_PLUS, 19, "contrarreloj_bell")
  @Test fun g_pista_anterior_393_10() = page("older_393_10", AgeRange.R18_29, 26, "contrarreloj_notes")
}
