package com.example

import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performSemanticsAction
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Comparison
import com.example.data.SampleComparison
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialTrials
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
 * La lámina de la Tarea 202 (`docs/previews/tarea-202.png`): la página «Contrarreloj» con la comparación con personas de tu edad (datos de muestra, solo en depuración): «Tu lugar esta semana» a 1,0 y 2,0, las 7 semanas y otras edades, «Todavía somos pocos» y la página entera.
 * No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea202CapturasTest*"`). Los PNG van a `src/test/screenshots/t202/` y `tools/previews/tarea_202.py` arma la hoja.
 * Mismos datos de la Tarea 199 (las diez marcas 31, 35, 39, 36, 44, 38, 41, 37, 42 y 43 hoy) con el ritmo de la Tarea 201 en la partida de hoy, y la persona de 45 a 54 años de la lámina de Fable (128 personas esta semana, 43 aciertos supera a 63 de cada 100).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea202CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private val prefix = System.getenv("T202_PREFIX") ?: "ahora"

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun at(day: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, day, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(mark: Int, day: Int, errors: Int = 2, level: Int = 4, game: String = "stroop", version: Int = 2) =
    TimeTrialResult(id = "r-$game-$day-$mark", gameId = game, day = "2026-10-%02d".format(day), trackVersion = version, hits = mark + errors, errors = errors, level = level, seconds = 120, timestamp = at(day))

  private fun paceTrials(hits: List<Int>, errors: List<Int>, levels: List<Int> = listOf(1, 2, 4, 4)): TimeTrialTrials {
    val t = mutableListOf<Int>(); val ok = mutableListOf<Int>(); val lv = mutableListOf<Int>()
    for (h in 0..3) {
      val n = hits[h] + errors[h]
      for (k in 0 until n) { t += h * 30_000 + 500 + k * (28_000 / maxOf(n, 1)); ok += if (k < hits[h]) 1 else 0; lv += levels[h] }
    }
    return TimeTrialTrials.of(t, ok, lv)!!
  }

  private val now = at(28, 18)
  private val lastGame = r(43, 28, 3).copy(trials = paceTrials(listOf(11, 14, 12, 9), listOf(0, 0, 1, 2)))
  private val ten = listOf(r(31, 8), r(35, 10), r(39, 13), r(36, 15), r(44, 17), r(38, 19, 3), r(41, 22, 3), r(37, 26, 3), r(42, 27, 3), lastGame)
  private val others = listOf(r(31, 21, game = "disparate"), r(27, 23, game = "radar"), r(19, 24, game = "acoplamiento"), r(38, 12, errors = 0, version = 1))

  /** Baja la página hasta que [tag] quede [marginDp] dp bajo el borde de arriba (la página es un solo desplazable). */
  private fun scrollTo(tag: String, marginDp: Int = 12) {
    val y = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
    rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, y - marginDp * 3f) }     // xxhdpi: 3 px por dp
    rule.waitForIdle()
  }

  private fun page(name: String, widthDp: Int, heightDp: Int, scale: Float, few: Boolean = false, results: List<TimeTrialResult> = ten + others, to: String? = null, beyondDp: Int = 0, range: AgeRange? = AgeRange.R45_54) {
    rule.mainClock.autoAdvance = true
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            ContrarrelojPage(results = results, selected = "stroop", onSelect = {}, onBack = {}, now = now, compare = SampleComparison(few), ageRange = range)
          }
        }
      }
    }
    rule.waitForIdle()
    if (to != null) scrollTo(to)
    if (beyondDp > 0) { rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, beyondDp * 3f) }; rule.waitForIdle() }
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t202/${prefix}_$name.png")
  }

  @Test fun a_seccion_393_10() = page("section_393_10", 393, 873, 1f, to = "contrarreloj_section_compare")
  @Test fun b_lugar_393_10_campana() = page("place_393_10_bell", 393, 873, 1f, to = "contrarreloj_place", beyondDp = 190)
  @Test fun c_semanas_393_10() = page("weeks_393_10", 393, 873, 1f, to = "contrarreloj_percweeks")
  @Test fun d_otras_edades_393_10() = page("others_393_10", 393, 873, 1f, to = "contrarreloj_others")
  @Test fun e_lugar_393_20() = page("place_393_20", 393, 873, 2f, to = "contrarreloj_place")
  @Test fun f_campana_393_20() = page("place_393_20_bell", 393, 873, 2f, to = "contrarreloj_place", beyondDp = 560)
  @Test fun g_semanas_393_20() = page("weeks_393_20", 393, 873, 2f, to = "contrarreloj_percweeks")
  @Test fun h_otras_393_20() = page("others_393_20", 393, 873, 2f, to = "contrarreloj_others", beyondDp = 0)
  @Test fun i_pocos_393_10() = page("few_393_10", 393, 873, 1f, few = true, to = "contrarreloj_section_compare")
  @Test fun j_pocos_393_20() = page("few_393_20", 393, 873, 2f, few = true, to = "contrarreloj_place")
  @Test fun k_lugar_360_10() = page("place_360_10", 360, 780, 1f, to = "contrarreloj_place")
  @Test fun l_pagina_entera_393_10() = page("full_393_10", 393, 3800, 1f)
  @Test fun m_sin_marca_esta_semana() = page("nomark_393_10", 393, 873, 1f, results = ten.dropLast(3) + others, to = "contrarreloj_section_compare")
  @Test fun n_pocos_pagina_entera() = page("full_few_393_10", 393, 3300, 1f, few = true)
}
