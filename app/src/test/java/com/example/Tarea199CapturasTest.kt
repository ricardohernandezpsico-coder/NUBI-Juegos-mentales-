package com.example

import android.app.Application
import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.performSemanticsAction
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialStats
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.games.TimeTrialResultScreen
import com.example.model.AgeBand
import com.example.model.GamePlayResult
import com.example.model.UserSettings
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.ContrarrelojPage
import com.example.ui.screens.ProgressScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.example.viewmodel.NeuroVidaViewModel
import com.github.takahirom.roborazzi.captureRoboImage
import kotlinx.coroutines.runBlocking
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
 * La lámina «hoy | propuesta» de la Tarea 199 (`docs/previews/tarea-199.png`): la página «Contrarreloj» de Avance (arriba y abajo a 393 × 873 con letra 1,0; arriba con 1,3 y 2,0; a 360; con una sola marca), Avance con la puerta (con y sin Nubi Plus) y la hoja del candado, y el final del Contrarreloj con «Todas tus marcas ›».
 * No corre en las pruebas de siempre: solo al grabar (`T199_PREFIX=prop ./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea199CapturasTest*"`). Los PNG van a `src/test/screenshots/t199/` y `tools/previews/tarea_199.py` arma la hoja. «hoy» (Avance y el final de antes) sale de `Tarea199HoyCapturasTest`, con el código de antes.
 * Datos de la lámina de Fable (§11.3): las diez marcas 31, 35, 39, 36, 44 (el récord, 17 de octubre), 38, 41, 37, 42 y 43 (hoy, miércoles 28).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea199CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private val prefix = System.getenv("T199_PREFIX") ?: "prop"
  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    TestSupport.fastFrames()
  }

  @After fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun at(day: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, day, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(mark: Int, day: Int, errors: Int = 2, level: Int = 4, game: String = "stroop", version: Int = 2) =
    TimeTrialResult(id = "r-$game-$day-$mark", gameId = game, day = "2026-10-%02d".format(day), trackVersion = version, hits = mark + errors, errors = errors, level = level, seconds = 120, timestamp = at(day))

  private val now = at(28, 18)
  private val ten = listOf(r(31, 8), r(35, 10), r(39, 13), r(36, 15), r(44, 17), r(38, 19, 3), r(41, 22, 3), r(37, 26, 3), r(42, 27, 3), r(43, 28, 3))
  private val others = listOf(r(31, 21, game = "disparate"), r(27, 23, game = "radar"), r(19, 24, game = "acoplamiento"), r(38, 12, errors = 0, version = 1))

  private fun page(name: String, widthDp: Int, heightDp: Int, scale: Float, results: List<TimeTrialResult> = ten + others, scrollTo: String? = null, selected: String = "stroop", toEnd: Boolean = false, scrollDp: Int = 0) {
    rule.mainClock.autoAdvance = true
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            ContrarrelojPage(results = results, selected = selected, onSelect = {}, onBack = {}, now = now)
          }
        }
      }
    }
    rule.waitForIdle()
    if (scrollTo != null) { rule.onNodeWithTag(scrollTo).performScrollTo(); rule.waitForIdle() }
    if (scrollDp > 0) { rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, scrollDp * 3f) }; rule.waitForIdle() }     // xxhdpi: 3 px por dp
    if (toEnd) { rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, 100_000f) }; rule.waitForIdle() }
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t199/${prefix}_$name.png")
  }

  @Test fun a_pagina_393_10_arriba() = page("page_393_10_top", 393, 873, 1f)
  @Test fun b_pagina_393_10_abajo() = page("page_393_10_bottom", 393, 873, 1f, toEnd = true)
  @Test fun c_pagina_393_13_arriba() = page("page_393_13_top", 393, 873, 1.3f)
  @Test fun d_pagina_393_20_record() = page("page_393_20_record", 393, 873, 2f, scrollDp = 800)
  @Test fun e_pagina_360_10_arriba() = page("page_360_10_top", 360, 780, 1f)
  @Test fun f_pagina_una_marca() = page("page_single", 393, 873, 1f, results = listOf(r(31, 28)), scrollTo = "contrarreloj_chart_first")
  @Test fun g_pagina_393_20_arriba() = page("page_393_20_top", 393, 873, 2f)
  @Test fun h_pagina_393_13_abajo() = page("page_393_13_bottom", 393, 873, 1.3f, toEnd = true)

  @Test fun i_final_con_atajo() {
    val today = r(43, 28, 3)
    val outcome = TimeTrialStats.outcome(ten + others, today, now)
    rule.mainClock.autoAdvance = true
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp)) { BoardBackground(); TimeTrialResultScreen(outcome = outcome, onAgain = {}, onDone = {}, soundEnabled = false, plus = true, onAllMarks = {}) } }  // LLAMADA
      }
    }
    rule.waitForIdle()
    rule.onNodeWithTag("btn_timetrial_all_marks").performScrollTo()
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t199/${prefix}_final_393_10.png")
  }

  // ---------------------------------------------------------------- Avance con la puerta (con la pantalla real y su ViewModel)

  private val day = 24L * 60 * 60 * 1000

  private fun seedAvance(vm: () -> NeuroVidaViewModel): NeuroVidaViewModel {
    val repo = (app as NeuroVidaApplication).repository
    val real = System.currentTimeMillis()
    val n = runBlocking {
      val db = NeuroVidaDatabase.getDatabase(app)
      db.userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity())
      var n = 0
      for ((k, id) in listOf("secuencia", "parejas", "bodega", "radar", "stroop", "calculo", "anagramas").withIndex()) for (round in 0 until 4) {
        repo.recordGameResult(
          GamePlayResult(gameId = id, score = 70, correctAnswers = 8, totalTrials = 12, timed = false, level = 2, timestamp = real - (28 - 7 * round) * day + k * 3_600_000L, endRating = 0.25f + 0.08f * round + 0.01f * k, modeTrials = 10, modeHits = 8),
          countsForDailySession = false
        )
        n += 1
      }
      n
    }
    val model = vm()
    viewModels.add(model)
    TestSupport.awaitUntil(30_000) { model.gameHistory.value.size >= n }
    // las marcas del juego de HOY (el de la puerta): 44 es el récord
    val today = TimeTrialDay.gameFor(model.todayKey())
    runBlocking {
      listOf(31, 35, 39, 36, 44, 38, 41, 37, 42, 43).forEachIndexed { i, m ->
        repo.recordTimeTrial(TimeTrialResult(id = "avance-$i", gameId = today, day = model.todayKey(), trackVersion = TimeTrialDay.trackVersion(today), hits = m + 2, errors = 2, level = 4, seconds = 120, timestamp = real - (10 - i) * day))
      }
    }
    TestSupport.awaitUntil(30_000) { model.timeTrialResults.value.size >= 10 }
    model.setTab(AppTab.PROGRESO)
    return model
  }

  private fun avance(name: String, plus: Boolean, scale: Float = 1f, sheet: Boolean = false) {
    val vm = seedAvance { NeuroVidaViewModel(app) }
    vm.setPlus(plus)
    rule.mainClock.autoAdvance = true
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true, com.example.ui.components.LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp)) { BoardBackground(); ProgressScreen(viewModel = vm) } }
      }
    }
    rule.waitForIdle()
    if (sheet) { rule.onNode(hasTestTag("avance_contrarreloj_door")).performClick(); rule.waitForIdle() }
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t199/${prefix}_$name.png")
  }

  @Test fun j_avance_con_plus() = avance("avance_plus", plus = true)
  @Test fun k_avance_sin_plus() = avance("avance_sinplus", plus = false)
  @Test fun l_hoja_del_candado() = avance("hoja_candado", plus = false, sheet = true)
}
