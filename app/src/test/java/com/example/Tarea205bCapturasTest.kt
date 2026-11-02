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
import com.example.data.EstimatedReference
import com.example.data.TimeTrialResult
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
import java.io.File
import java.util.Calendar

/**
 * La lámina «antes | ahora» de la Tarea 205b (`docs/previews/tarea-205b.png`): la raya de «la mitad» que la barra «Tú» tapaba en la campana (Rescate relámpago con la marca en la mitad; Tinta o Palabra con la mitad ± 1 a 360 dp) y la línea que dice dónde elegir la edad cuando no hay franja guardada.
 * Los «antes» de la campana (Rescate y Tinta) salen de la T205 (7c5ead1f), grabados con el mismo código de prueba y PREFIX = «antes» antes de cambiar la campana; los ya grabados no se tocan. Los «antes» de la página SIN franja salen de esta misma prueba con `compare = null`: la T205 sin franja guardada
 * armaba la página igual que sin comparación (sin sección, subtítulo «solo contigo»). Los PNG van a `src/test/screenshots/t205b/` y `tools/previews/tarea_205b.py` arma la hoja. No corre en las pruebas de siempre: solo al grabar
 * (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea205bCapturasTest*"`).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea205bCapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private companion object { const val PREFIX = "ahora" }

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun at(d: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, d, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(mark: Int, d: Int, errors: Int = 2, game: String = "stroop", version: Int = 2) =
    TimeTrialResult(id = "r-$game-$d-$mark", gameId = game, day = "2026-10-%02d".format(d), trackVersion = version, hits = mark + errors, errors = errors, level = 4, seconds = 120, timestamp = at(d))

  private val now = at(28, 18)

  private val estWorld: Comparison.Source by lazy {
    val dir = File("src/main/assets/${EstimatedReference.ASSET_DIR}")
    Comparison.ComposedSource(null, EstimatedReference.of(dir.listFiles { f -> f.extension == "json" }.orEmpty().sortedBy { it.name }.map { it.readText(Charsets.UTF_8) }))
  }

  private fun half(game: String) = estWorld.distribution(game, listOf(AgeRange.R45_54), 0, 1).p50

  private fun scrollTo(tag: String, marginDp: Int = 12) {
    val y = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
    rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, y - marginDp * 3f) }     // xxhdpi: 3 px por dp
    rule.waitForIdle()
  }

  /** Baja la página una cantidad fija de dp (la misma con y sin el aviso, para que «antes» y «ahora» se comparen en el mismo lugar). */
  private fun scrollBy(dp: Int) {
    rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, dp * 3f) }     // xxhdpi: 3 px por dp
    rule.waitForIdle()
  }

  private fun page(
    name: String, widthDp: Int, heightDp: Int, scale: Float, results: List<TimeTrialResult>, selected: String, range: AgeRange?, to: String? = null, margin: Int = 12,
    compare: Comparison.Source? = estWorld, prefix: String = "ahora", scrollDp: Int = 0
  ) {
    rule.mainClock.autoAdvance = true
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            ContrarrelojPage(results = results, selected = selected, onSelect = {}, now = now, compare = compare, ageRange = range)
          }
        }
      }
    }
    rule.waitForIdle()
    if (to != null) scrollTo(to, margin)
    if (scrollDp > 0) scrollBy(scrollDp)
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t205b/${prefix}_$name.png")
  }

  // la raya de la mitad bajo la barra «Tú»
  @Test fun a_rescate_393() = page("radar_393", 393, 873, 1f, listOf(r(half("radar"), 27, game = "radar")), "radar", AgeRange.R45_54, to = "contrarreloj_bell")
  @Test fun b_rescate_360() = page("radar_360", 360, 780, 1f, listOf(r(half("radar"), 27, game = "radar")), "radar", AgeRange.R45_54, to = "contrarreloj_bell")
  @Test fun c_tinta_mitad_mas_1_360() = page("stroop_360", 360, 780, 1f, listOf(r(half("stroop") + 1, 27)), "stroop", AgeRange.R45_54, to = "contrarreloj_bell")
  // la página sin franja guardada: «antes» = sin comparación (lo que armaba la T205), «ahora» = con la referencia y el aviso
  private val one = listOf(r(43, 27))
  private fun noRange(name: String, w: Int, h: Int, scale: Float, scrollDp: Int, now: Boolean) =
    page(name, w, h, scale, one, "stroop", null, compare = if (now) estWorld else null, prefix = if (now) "ahora" else "antes", scrollDp = scrollDp)

  @Test fun d1_sin_franja_393_10_antes() = noRange("nofranja_393_10", 393, 873, 1f, 0, now = false)
  @Test fun d2_sin_franja_393_10_ahora() = noRange("nofranja_393_10", 393, 873, 1f, 0, now = true)
  @Test fun e1_sin_franja_393_20_antes() = noRange("nofranja_393_20", 393, 873, 2f, 300, now = false)
  @Test fun e2_sin_franja_393_20_ahora() = noRange("nofranja_393_20", 393, 873, 2f, 300, now = true)
  @Test fun f1_sin_franja_360_10_antes() = noRange("nofranja_360_10", 360, 780, 1f, 0, now = false)
  @Test fun f2_sin_franja_360_10_ahora() = noRange("nofranja_360_10", 360, 780, 1f, 0, now = true)
  @Test fun g1_sin_franja_360_20_antes() = noRange("nofranja_360_20", 360, 780, 2f, 400, now = false)
  @Test fun g2_sin_franja_360_20_ahora() = noRange("nofranja_360_20", 360, 780, 2f, 400, now = true)
}
