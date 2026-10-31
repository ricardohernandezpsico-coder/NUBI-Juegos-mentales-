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
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performSemanticsAction
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialTrials
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
 * La lámina «hoy | ahora» de la Tarea 201 (`docs/previews/tarea-201.png`): la página «Contrarreloj» de Avance con el orden nuevo (récord, ritmo, semana, aciertos y errores, pistas), las líneas de separación, la ventanilla noche de los gráficos y la losa «Tu ritmo en la última partida».
 * No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea201CapturasTest*"`). Los PNG van a `src/test/screenshots/t201/` y `tools/previews/tarea_201.py` arma la hoja. «hoy» son las capturas de la Tarea 199 (`prop_*` de `t199/`, copiadas como `hoy_*` antes de tocar la página).
 * Mismos datos de la Tarea 199 (las diez marcas 31, 35, 39, 36, 44 —el récord—, 38, 41, 37, 42 y 43 hoy) y, en la partida de hoy, el ritmo de la lámina de Fable (§2.3): 11, 14, 12 y 9 aciertos por medio minuto, 1 error en el tercero y 2 en el último, en el paso 4 de la pista.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea201CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private val prefix = System.getenv("T201_PREFIX") ?: "ahora"

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun at(day: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, day, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(mark: Int, day: Int, errors: Int = 2, level: Int = 4, game: String = "stroop", version: Int = 2) =
    TimeTrialResult(id = "r-$game-$day-$mark", gameId = game, day = "2026-10-%02d".format(day), trackVersion = version, hits = mark + errors, errors = errors, level = level, seconds = 120, timestamp = at(day))

  /** Por cada medio minuto, [hits] aciertos y luego [errors] errores, en el paso [levels] de la pista. */
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

  /** El peor caso: 18 errores en un medio minuto y 48 aciertos en otro (cifras de dos dígitos en las dos barras de una columna). */
  private val heavy = r(70, 28, 20).copy(trials = paceTrials(listOf(48, 12, 20, 10), listOf(18, 0, 1, 1), levels = listOf(1, 3, 4, 4)))

  private fun page(name: String, widthDp: Int, heightDp: Int, scale: Float, results: List<TimeTrialResult> = ten + others, scrollTo: String? = null, toEnd: Boolean = false, scrollDp: Int = 0) {
    rule.mainClock.autoAdvance = true
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            ContrarrelojPage(results = results, selected = "stroop", onSelect = {}, onBack = {}, now = now)
          }
        }
      }
    }
    rule.waitForIdle()
    if (scrollTo != null) { rule.onNodeWithTag(scrollTo).performScrollTo(); rule.waitForIdle() }
    if (scrollDp > 0) { rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, scrollDp * 3f) }; rule.waitForIdle() }     // xxhdpi: 3 px por dp
    if (toEnd) { rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, 100_000f) }; rule.waitForIdle() }
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t201/${prefix}_$name.png")
  }

  @Test fun a_pagina_393_10_arriba() = page("page_393_10_top", 393, 873, 1f)
  @Test fun b_pagina_393_10_ritmo() = page("page_393_10_rhythm", 393, 873, 1f, scrollTo = "contrarreloj_rhythm")
  @Test fun c_pagina_393_10_semana() = page("page_393_10_weeks", 393, 873, 1f, scrollTo = "contrarreloj_weeks")
  @Test fun d_pagina_393_10_abajo() = page("page_393_10_bottom", 393, 873, 1f, toEnd = true)
  @Test fun e_pagina_393_13_ritmo() = page("page_393_13_rhythm", 393, 873, 1.3f, scrollTo = "contrarreloj_rhythm")
  @Test fun f_pagina_393_20_arriba() = page("page_393_20_top", 393, 873, 2f)
  @Test fun g_pagina_393_20_ritmo() = page("page_393_20_rhythm", 393, 873, 2f, scrollTo = "contrarreloj_rhythm")
  @Test fun h_pagina_360_10_ritmo() = page("page_360_10_rhythm", 360, 780, 1f, scrollTo = "contrarreloj_rhythm")
  @Test fun i_pagina_393_10_sin_ritmo_guardado() = page("page_393_10_wait", 393, 873, 1f, results = ten.dropLast(1) + r(43, 28, 3) + others, scrollTo = "contrarreloj_rhythm")
  @Test fun j_pagina_393_10_peor_caso() = page("page_393_10_heavy", 393, 873, 1f, results = ten.dropLast(1) + heavy + others, scrollTo = "contrarreloj_rhythm")
  @Test fun k_pagina_393_20_peor_caso() = page("page_393_20_heavy", 393, 873, 2f, results = ten.dropLast(1) + heavy + others, scrollTo = "contrarreloj_rhythm")
  @Test fun l_pagina_393_20_marcas() = page("page_393_20_marks", 393, 873, 2f, scrollDp = 820)
}
