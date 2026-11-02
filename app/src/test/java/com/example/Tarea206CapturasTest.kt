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
import java.util.Calendar

/**
 * La lámina de la Tarea 206 (`docs/previews/tarea-206.png`, `tools/previews/tarea_206.py`): la página «Contrarreloj» de Tinta o Palabra y de Acoplamiento, franja de 30 a 44 años, con la escala calibrada y los 20 tramos: la campana con su eje («95 o más», «38 o más») y la escalera de CINCO peldaños,
 * con letra 1,0 y 2,0, y la campana a 360 dp (el ancho más angosto, donde los rótulos del eje podrían chocar). La marca es la de P1 en su primera semana (58 aciertos en Tinta, 24 en Acoplamiento). Es la página, no un `Dialog`, así que Roborazzi mide la letra de verdad.
 * No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea206CapturasTest*"`); los PNG van a `src/test/screenshots/t206/`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea206CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun at(d: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, d, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(mark: Int, d: Int, game: String, errors: Int = 2) =
    TimeTrialResult(id = "r-$game-$d-$mark", gameId = game, day = "2026-10-%02d".format(d), trackVersion = 2, hits = mark + errors, errors = errors, level = 4, seconds = 120, timestamp = at(d))

  /** La referencia estimada REAL del APK, como la ve la tienda. */
  private val estWorld: Comparison.Source by lazy {
    val dir = java.io.File("src/main/assets/${EstimatedReference.ASSET_DIR}")
    Comparison.ComposedSource(null, EstimatedReference.of(dir.listFiles { f -> f.extension == "json" }.orEmpty().sortedBy { it.name }.map { it.readText(Charsets.UTF_8) }))
  }

  private val now = at(28, 18)

  /** Baja la página hasta que [tag] quede [marginDp] dp bajo el borde de arriba (la página es un solo desplazable). */
  private fun scrollTo(tag: String, marginDp: Int = 12) {
    val y = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
    rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, y - marginDp * 3f) }     // xxhdpi: 3 px por dp
    rule.waitForIdle()
  }

  private fun page(name: String, game: String, mark: Int, widthDp: Int, heightDp: Int, scale: Float, to: String) {
    rule.mainClock.autoAdvance = true
    val results = listOf(r(mark, 27, game))
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            ContrarrelojPage(results = results, selected = game, onSelect = {}, now = now, compare = estWorld, ageRange = AgeRange.R30_44)
          }
        }
      }
    }
    rule.waitForIdle()
    scrollTo(to)
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t206/ahora_$name.png")
  }

  @Test fun a_tinta_escalera_393_10() = page("stroop_ladder_393_10", "stroop", 58, 393, 873, 1f, "contrarreloj_ladder")
  @Test fun b_tinta_campana_393_10() = page("stroop_bell_393_10", "stroop", 58, 393, 873, 1f, "contrarreloj_bell")
  @Test fun c_tinta_escalera_393_20() = page("stroop_ladder_393_20", "stroop", 58, 393, 873, 2f, "contrarreloj_ladder")
  @Test fun d_tinta_campana_393_20() = page("stroop_bell_393_20", "stroop", 58, 393, 873, 2f, "contrarreloj_bell")
  @Test fun e_tinta_campana_360_10() = page("stroop_bell_360_10", "stroop", 58, 360, 780, 1f, "contrarreloj_bell")

  @Test fun f_acoplamiento_escalera_393_10() = page("acop_ladder_393_10", "acoplamiento", 24, 393, 873, 1f, "contrarreloj_ladder")
  @Test fun g_acoplamiento_campana_393_10() = page("acop_bell_393_10", "acoplamiento", 24, 393, 873, 1f, "contrarreloj_bell")
  @Test fun h_acoplamiento_escalera_393_20() = page("acop_ladder_393_20", "acoplamiento", 24, 393, 873, 2f, "contrarreloj_ladder")
  @Test fun i_acoplamiento_campana_393_20() = page("acop_bell_393_20", "acoplamiento", 24, 393, 873, 2f, "contrarreloj_bell")
  @Test fun j_acoplamiento_campana_360_10() = page("acop_bell_360_10", "acoplamiento", 24, 360, 780, 1f, "contrarreloj_bell")
}
