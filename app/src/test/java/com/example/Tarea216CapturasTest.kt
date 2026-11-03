package com.example

import android.app.Application
import androidx.activity.ComponentActivity
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
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
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performSemanticsAction
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.Comparison
import com.example.data.EstimatedReference
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.data.Preguntas
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialTrials
import com.example.model.AgeRange
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NeuroNavBar
import com.example.ui.screens.ContrarrelojPage
import com.example.ui.screens.FirstFlightScreen
import com.example.ui.screens.FlightActions
import com.example.ui.screens.PanelHeader
import com.example.ui.screens.PreguntasScreen
import com.example.ui.screens.ReferenciaPanel
import com.example.ui.screens.SettingsScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.example.viewmodel.NeuroVidaViewModel
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.After
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
 * La lámina de la Tarea 216 (`docs/previews/tarea-216.png`, `tools/previews/tarea_216.py`): la app es desde los 14 años. La pregunta de edad del Primer vuelo con «14 a 17 años», la pestaña Contrarreloj de una persona de 14 a 17 con su comparación (sin y con Nubilus Plus) y «Otras edades» sin fila de 14 a 17, la hoja «De dónde sale la referencia»
 * con la línea nueva y el estudio de Luna, «Preguntas frecuentes» con la respuesta nueva, y «Acerca de Nubilus» (Ajustes) con la línea de edad a 393 dp con letra 1,0 y a 360 dp con letra 2,0. No corre en las pruebas de siempre: solo al grabar
 * (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea216CapturasTest*"`); los PNG van a `src/test/screenshots/t216/` (fuera de git).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea216CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.resetDatabase()
    TestSupport.fastFrames()
  }

  @After fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun at(d: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, d, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(mark: Int, d: Int, errors: Int = 2, game: String = "stroop") =
    TimeTrialResult(id = "r-$game-$d-$mark", gameId = game, day = "2026-10-%02d".format(d), trackVersion = TimeTrialDay.trackVersion(game), hits = mark + errors, errors = errors, level = 4, seconds = 120, timestamp = at(d))

  private val now = at(28, 18)

  private fun paceTrials(hits: List<Int>, errors: List<Int>): TimeTrialTrials {
    val t = mutableListOf<Int>(); val ok = mutableListOf<Int>(); val lv = mutableListOf<Int>()
    for (h in 0..3) {
      val n = hits[h] + errors[h]
      for (k in 0 until n) { t += h * 30_000 + 500 + k * (28_000 / maxOf(n, 1)); ok += if (k < hits[h]) 1 else 0; lv += listOf(1, 2, 4, 4)[h] }
    }
    return TimeTrialTrials.of(t, ok, lv)!!
  }

  /** Una persona de 14 a 17 años que juega Tinta o Palabra: diez marcas, la última de esta semana con 52 (En órbita en la referencia de 18 a 29). */
  private val results = listOf(
    r(31, 8), r(35, 10), r(39, 13), r(36, 15), r(44, 17), r(38, 19, 3), r(41, 22, 3), r(37, 26, 3), r(42, 27, 3),
    r(52, 28, 3).copy(trials = paceTrials(listOf(13, 15, 14, 13), listOf(0, 0, 1, 2))), r(27, 23, game = "radar")
  )

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
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontState), LocalReduceMotion provides true, LocalBarInset provides 88.dp) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823))) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t216/$name.png") }

  private fun scrollPage(tag: String, marginDp: Int = 12) {
    val y = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
    rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, y - marginDp * 3f) }     // xxhdpi: 3 px por dp
    rule.waitForIdle()
  }

  // ---------------------------------------------------------------- la pregunta de edad: la primera opción dice «14 a 17 años»

  @Test fun vuelo_edad_393_10() {
    frame(393, 873) {
      FirstFlightScreen(FlightState(FlightMode.FULL, FlightStep.EDAD, name = "Ana", measured = mapOf("secuencia" to 0.62f), phrases = mapOf("secuencia" to "Repetiste bien un rastro de 5 luces.")), emptyList(), FlightActions.None)
    }
    save("vuelo_edad_393_10")
  }

  // ---------------------------------------------------------------- la pestaña de una persona de 14 a 17 años

  private fun tab(plus: Boolean, widthDp: Int = 393, heightDp: Int = 873, scale: Float = 1f) {
    frame(widthDp, heightDp, scale) {
      Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) {
        val inset = with(LocalDensity.current) { if (barPx > 0) barPx.toDp() else 88.dp }
        CompositionLocalProvider(LocalBarInset provides inset) {
          Box(Modifier.padding(top = 24.dp)) { ContrarrelojPage(results = results, selected = "stroop", onSelect = {}, now = now, compare = estWorld, ageRange = AgeRange.R14_17, plus = plus, today = "stroop") }
        }
        Box(Modifier.align(Alignment.BottomCenter).onSizeChanged { if (it.height > 0) barPx = it.height }) { NeuroNavBar(current = AppTab.CONTRARRELOJ, onSelect = {}) }
      }
    }
  }

  @Test fun pestana_sin_plus_393_10() { tab(plus = false); save("pestana_sin_plus_393_10") }
  @Test fun pestana_con_plus_393_10() { tab(plus = true); save("pestana_con_plus_393_10") }
  @Test fun otras_edades_393_10() { tab(plus = true, heightDp = 873); scrollPage("contrarreloj_others", 12); save("otras_edades_393_10") }
  @Test fun pestana_sin_plus_360_20() { tab(plus = false, widthDp = 360, heightDp = 780, scale = 2f); save("pestana_sin_plus_360_20") }

  // ---------------------------------------------------------------- las hojas

  @Test fun referencia_393_10() { frame(393, 873) { ReferenciaPanel(onClose = {}) }; save("referencia_393_10") }
  @Test fun referencia_luna_393_10() { frame(393, 873) { ReferenciaPanel(onClose = {}) }; rule.onNodeWithTag("estudio_10.1111/j.1467-8624.2004.00745.x").performScrollTo(); rule.waitForIdle(); save("referencia_luna_393_10") }
  @Test fun referencia_360_20() { frame(360, 780, 2f) { ReferenciaPanel(onClose = {}) }; rule.onNodeWithTag("referencia_teen").performScrollTo(); rule.waitForIdle(); save("referencia_360_20") }

  @Test fun preguntas_393_10() {
    frame(393, 873) {
      Column(Modifier.fillMaxSize()) {
        PanelHeader(Preguntas.TITLE, onBack = {})
        PreguntasScreen(expanded = setOf("edad", "referencia"), onToggle = {}, onOpenReferencia = {}, modifier = Modifier.weight(1f))
      }
    }
    save("preguntas_393_10")
  }

  // ---------------------------------------------------------------- «Acerca de Nubilus» con la línea de la edad mínima

  private fun settings(widthDp: Int, heightDp: Int, scale: Float) {
    val app = ApplicationProvider.getApplicationContext<Application>()
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    frame(widthDp, heightDp, scale) { SettingsScreen(viewModel = vm) }
    rule.onNodeWithTag("card_acerca").performScrollTo()
    rule.waitForIdle()
  }

  @Test fun acerca_393_10() { settings(393, 873, 1f); save("acerca_393_10") }
  @Test fun acerca_360_20() { settings(360, 780, 2f); save("acerca_360_20") }
}
