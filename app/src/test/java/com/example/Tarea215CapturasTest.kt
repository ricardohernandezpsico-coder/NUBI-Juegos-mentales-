package com.example

import androidx.activity.ComponentActivity
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
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
import androidx.compose.ui.test.performSemanticsAction
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Comparison
import com.example.data.EstimatedReference
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialTrials
import com.example.model.AgeRange
import com.example.ui.components.BoardBackground
import com.example.ui.components.ClayDisc
import com.example.ui.components.DiscStyle
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NavIcon
import com.example.ui.components.NavIconImage
import com.example.ui.components.NeuroNavBar
import com.example.ui.components.SlabColor
import com.example.ui.components.SlabSize
import com.example.ui.components.ClaySlab
import com.example.ui.screens.ContrarrelojPage
import com.example.ui.theme.Clay
import com.example.ui.theme.NeuroVidaTheme
import com.example.ui.theme.NubiTokens
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
 * La lámina de la Tarea 215 (`docs/previews/tarea-215.png`, `tools/previews/tarea_215.py`): la barra de cuatro pestañas ANTES | AHORA a 360 y 393 dp con letra 1,0 y 1,3 («antes» es una réplica de la barra de tres tercios de la Tarea 156, solo para esta lámina), la pestaña «Contrarreloj» sin Plus (lo gratis a color arriba y las dos losas
 * grises abajo) y con Plus (lo gratis, el detalle y todas las marcas), la pestaña sin franja de edad guardada y Avance sin la puerta (ese sale de `AppActualScreenshotTest.avance`). La letra 2,0 se mira en el emulador. No corre en las pruebas de siempre: solo al grabar
 * (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea215CapturasTest*"`); los PNG van a `src/test/screenshots/t215/` (fuera de git).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea215CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
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

  /** Tinta o Palabra, el juego de hoy: diez marcas, la última con su ritmo (64 de marca esta semana: En órbita en la referencia de 45 a 54 años) y una marca de otro juego. */
  private val results = listOf(
    r(31, 8), r(35, 10), r(39, 13), r(36, 15), r(44, 17), r(38, 19, 3), r(41, 22, 3), r(37, 26, 3), r(42, 27, 3),
    r(64, 28, 3).copy(trials = paceTrials(listOf(16, 19, 17, 15), listOf(0, 0, 1, 2))), r(27, 23, game = "radar")
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
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontState), LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823))) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t215/$name.png") }

  // ---------------------------------------------------------------- la barra: antes | ahora

  /** La barra de tres tercios de la Tarea 156 (la de antes de esta tarea), réplica solo para la lámina: columnas parejas, la losa sol fija en 96 dp y el rótulo sin tope de letra. */
  @androidx.compose.runtime.Composable
  private fun OldBar(current: String) {
    Row(Modifier.width(androidx.compose.ui.platform.LocalConfiguration.current.screenWidthDp.dp).background(Color(0xFF29276A))) {
      for ((label, icon) in listOf("Hoy" to NavIcon.ISLA, "Avance" to NavIcon.BARRAS, "Tú" to NavIcon.PERSONA)) {
        val selected = label == current
        Box(Modifier.weight(1f), contentAlignment = Alignment.TopCenter) {
          if (selected) Box(Modifier.matchParentSize(), contentAlignment = Alignment.TopCenter) { ClaySlab(SlabColor.SUN, Modifier.width(96.dp).fillMaxHeight().padding(top = 3.dp), size = SlabSize.SMALL, onClick = null, role = null) {} }
          Column(Modifier.padding(top = 7.dp, bottom = 8.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            ClayDisc(DiscStyle.CREAM, onClick = null, label = label, forcePressed = false) { NavIconImage(icon) }
            Spacer(Modifier.height(4.dp))
            Text(label, color = if (selected) Clay.Ink else NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 21.sp, fontWeight = if (selected) FontWeight.Bold else FontWeight.Medium, textAlign = TextAlign.Center, softWrap = false)
          }
        }
      }
    }
  }

  private fun bar(widthDp: Int, scale: Float, now: Boolean, tab: AppTab) {
    frame(widthDp, 100, scale) {
      Box(Modifier.fillMaxSize(), contentAlignment = Alignment.BottomCenter) { if (now) NeuroNavBar(current = tab, onSelect = {}) else OldBar(if (tab == AppTab.HOY) "Hoy" else "Avance") }
    }
    save("${if (now) "ahora" else "antes"}_barra_${widthDp}_${if (scale == 1f) "10" else "13"}_${tab.name.lowercase()}")
  }

  // (una composición por prueba: cada barra es la suya)
  @Test fun antes_393_10() = bar(393, 1f, false, AppTab.HOY)
  @Test fun ahora_393_10() = bar(393, 1f, true, AppTab.CONTRARRELOJ)
  @Test fun antes_360_10() = bar(360, 1f, false, AppTab.HOY)
  @Test fun ahora_360_10() = bar(360, 1f, true, AppTab.CONTRARRELOJ)
  @Test fun antes_393_13() = bar(393, 1.3f, false, AppTab.HOY)
  @Test fun ahora_393_13() = bar(393, 1.3f, true, AppTab.CONTRARRELOJ)
  @Test fun antes_360_13() = bar(360, 1.3f, false, AppTab.HOY)
  @Test fun ahora_360_13() = bar(360, 1.3f, true, AppTab.CONTRARRELOJ)
  @Test fun barra_ahora_360_hoy_10() { bar(360, 1f, true, AppTab.HOY) }
  @Test fun barra_ahora_360_tu_13() { bar(360, 1.3f, true, AppTab.TU) }

  // ---------------------------------------------------------------- la pestaña

  /** La pestaña como la ve la app: la página con la franja de la barra libre y la barra de verdad abajo, sobre 48 dp de la barra del sistema. */
  private fun tab(widthDp: Int, heightDp: Int, plus: Boolean, range: AgeRange?, scale: Float = 1f) {
    frame(widthDp, heightDp, scale) {
      Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) {
        val inset = with(LocalDensity.current) { if (barPx > 0) barPx.toDp() else 88.dp }
        CompositionLocalProvider(LocalBarInset provides inset) {
          Box(Modifier.padding(top = 24.dp)) {
            ContrarrelojPage(results = results, selected = "stroop", onSelect = {}, now = now, compare = estWorld, ageRange = range, plus = plus, today = "stroop")
          }
        }
        Box(Modifier.align(Alignment.BottomCenter).onSizeChanged { if (it.height > 0) barPx = it.height }) { NeuroNavBar(current = AppTab.CONTRARRELOJ, onSelect = {}) }
      }
    }
  }

  private fun scrollTo(tag: String, marginDp: Int = 12) {
    val y = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
    rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, y - marginDp * 3f) }     // xxhdpi: 3 px por dp
    rule.waitForIdle()
  }

  @Test fun sin_plus_393_10() { tab(393, 873, false, AgeRange.R45_54); save("ahora_sin_plus_393_10") }
  @Test fun sin_plus_abajo_393_10() { tab(393, 873, false, AgeRange.R45_54); scrollTo("contrarreloj_locked_place", 120); save("ahora_sin_plus_abajo_393_10") }
  @Test fun sin_plus_360_13() { tab(360, 780, false, AgeRange.R45_54, 1.3f); save("ahora_sin_plus_360_13") }
  @Test fun con_plus_393_10() { tab(393, 873, true, AgeRange.R45_54); save("ahora_con_plus_393_10") }
  @Test fun con_plus_detalle_393_10() { tab(393, 873, true, AgeRange.R45_54); scrollTo("contrarreloj_section_compare", 12); save("ahora_con_plus_detalle_393_10") }
  @Test fun con_plus_marcas_393_10() { tab(393, 873, true, AgeRange.R45_54); scrollTo("contrarreloj_section_marks", 12); save("ahora_con_plus_marcas_393_10") }
  @Test fun sin_franja_393_10() { tab(393, 873, false, null); save("ahora_sin_franja_393_10") }
  @Test fun menores_393_10() { tab(393, 873, false, AgeRange.R14_17); save("ahora_menores_393_10") }
}
