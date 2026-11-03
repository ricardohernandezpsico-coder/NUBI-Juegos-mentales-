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
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.model.AgeRange
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NeuroNavBar
import com.example.ui.screens.ContrarrelojPage
import com.example.ui.screens.PlusLockSheetContent
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
 * La lámina de la Tarea 215b (`docs/previews/tarea-215b.png`, `tools/previews/tarea_215b.py`): lo que Fable pidió corregir en la auditoría de la 215. La hoja del candado con la referencia a 393 dp con letra 1,0 (y a 360 con 2,0), la losa gratis en sus tres casos (la mejor de la semana ES el récord, la mejor es otra y el juego sin marcas)
 * y la pestaña sin Plus antes de la tercera partida (el lugar todavía no aparece). No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea215bCapturasTest*"`); los PNG van a `src/test/screenshots/t215b/` (fuera de git).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea215bCapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun at(d: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, d, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(mark: Int, d: Int, errors: Int = 2, game: String = "stroop") =
    TimeTrialResult(id = "r-$game-$d-$mark", gameId = game, day = "2026-10-%02d".format(d), trackVersion = TimeTrialDay.trackVersion(game), hits = mark + errors, errors = errors, level = 4, seconds = 120, timestamp = at(d))

  private val now = at(28, 18)

  /** Tres partidas esta semana (26, 27 y 28 de octubre) y marcas de semanas anteriores; el récord es el 52 de hoy: la mejor de la semana ES el récord. */
  private val recordIsBest = listOf(r(31, 8), r(35, 10), r(39, 13), r(36, 15), r(44, 17), r(38, 19, 3), r(41, 22, 3), r(37, 26, 3), r(42, 27, 3), r(52, 28, 3))

  /** El récord (60) es del 17 de octubre y la mejor de esta semana es otra (48). */
  private val recordIsOld = listOf(r(31, 8), r(35, 10), r(39, 13), r(36, 15), r(60, 17), r(38, 19, 3), r(41, 22, 3), r(37, 26, 3), r(42, 27, 3), r(48, 28, 3))

  /** La referencia estimada REAL del APK, como la ve la tienda. */
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
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontState), LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823))) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t215b/$name.png") }

  // ---------------------------------------------------------------- la hoja del candado

  private fun sheet(kind: Comparison.Kind, widthDp: Int, heightDp: Int, scale: Float) {
    frame(widthDp, heightDp, scale) { Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) { PlusLockSheetContent(onClose = {}, kind = kind, onSource = {}) } }
  }

  @Test fun hoja_referencia_393_10() { sheet(Comparison.Kind.ESTIMATED, 393, 620, 1f); save("hoja_referencia_393_10") }
  @Test fun hoja_sin_comparacion_393_10() { sheet(Comparison.Kind.NONE, 393, 620, 1f); save("hoja_sin_comparacion_393_10") }
  @Test fun hoja_referencia_360_20() { sheet(Comparison.Kind.ESTIMATED, 360, 780, 2f); save("hoja_referencia_360_20") }

  // ---------------------------------------------------------------- la pestaña

  private fun tab(results: List<TimeTrialResult>, selected: String = "stroop", plus: Boolean = false, widthDp: Int = 393, heightDp: Int = 873, scale: Float = 1f, today: String = "stroop") {
    frame(widthDp, heightDp, scale) {
      Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) {
        val inset = with(LocalDensity.current) { if (barPx > 0) barPx.toDp() else 88.dp }
        CompositionLocalProvider(LocalBarInset provides inset) {
          Box(Modifier.padding(top = 24.dp)) { ContrarrelojPage(results = results, selected = selected, onSelect = {}, now = now, compare = estWorld, ageRange = AgeRange.R45_54, plus = plus, today = today) }
        }
        Box(Modifier.align(Alignment.BottomCenter).onSizeChanged { if (it.height > 0) barPx = it.height }) { NeuroNavBar(current = AppTab.CONTRARRELOJ, onSelect = {}) }
      }
    }
  }

  @Test fun gratis_record_es_la_mejor_393_10() { tab(recordIsBest); save("gratis_record_es_la_mejor_393_10") }
  @Test fun gratis_mejor_distinta_393_10() { tab(recordIsOld); save("gratis_mejor_distinta_393_10") }
  @Test fun gratis_sin_marcas_393_10() { tab(recordIsBest, selected = "radar", today = "radar"); save("gratis_sin_marcas_393_10") }
  @Test fun antes_de_la_tercera_393_10() { tab(listOf(r(31, 27), r(35, 28))); save("antes_de_la_tercera_393_10") }
}
