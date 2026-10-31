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
 * La lámina corta de la Tarea 202b (`docs/previews/tarea-202b.png`): la cabecera y la nota de la página «Contrarreloj» con y sin la comparación (con ella ya no dicen «solo contigo») y «Todavía somos pocos» con «Mientras tanto» y la fila «Todas las edades» (el mismo número esta semana; con «muy pocos»,
 * «Mientras tanto» usa las últimas 4 semanas, lo dice, y la fila dice «todavía somos pocos»). No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea202bCapturasTest*"`); los PNG van a `src/test/screenshots/t202b/` y `tools/previews/tarea_202b.py` arma la hoja.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea202bCapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

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

  private fun scrollTo(tag: String, marginDp: Int = 12) {
    val y = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
    rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, y - marginDp * 3f) }     // xxhdpi: 3 px por dp
    rule.waitForIdle()
  }

  private fun page(name: String, heightDp: Int, compare: Comparison.Source?, to: String? = null) {
    rule.mainClock.autoAdvance = true
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, 1f), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(heightDp.dp)) {
            BoardBackground()
            ContrarrelojPage(results = ten + others, selected = "stroop", onSelect = {}, onBack = {}, now = now, compare = compare, ageRange = AgeRange.R45_54)
          }
        }
      }
    }
    rule.waitForIdle()
    if (to != null) scrollTo(to)
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t202b/ahora_$name.png")
  }

  private val many = SampleComparison(few = false)
  private val few = SampleComparison(few = true)
  private val veryFew = SampleComparison(SampleComparison.World.VERY_FEW)

  @Test fun a_cabecera_sin_comparacion() = page("header_sin", 190, null)
  @Test fun b_cabecera_con_comparacion() = page("header_con", 190, many)
  @Test fun c_nota_sin_comparacion() = page("note_sin", 300, null, to = "contrarreloj_older")
  @Test fun d_nota_con_comparacion() = page("note_con", 300, many, to = "contrarreloj_older")
  @Test fun e_pocos_lugar() = page("few_place", 700, few, to = "contrarreloj_place")
  @Test fun f_pocos_otras_edades() = page("few_others", 620, few, to = "contrarreloj_others")
  @Test fun g_muy_pocos_lugar() = page("veryfew_place", 700, veryFew, to = "contrarreloj_place")
  @Test fun h_muy_pocos_otras_edades() = page("veryfew_others", 620, veryFew, to = "contrarreloj_others")
}
