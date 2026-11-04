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
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performSemanticsAction
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.Comparison
import com.example.data.EstimatedReference
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.AgeRange
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
 * La lámina de la Tarea 207 (`docs/previews/tarea-207.png`, `tools/previews/tarea_207.py`): el lugar aparece desde la tercera partida de cada juego. La PÁGINA «Contrarreloj» de Tinta o Palabra con 1 y con 3 partidas (franja de 30 a 44 años, marca 58, la de P1), la PUERTA de Avance sin Plus con 1 y con 3 partidas
 * (la pantalla real y su ViewModel) y el eje de la campana de Tinta con la regla nueva (el «63» ya no queda bajo la raya de la mitad), a letra 1,0. La de 2,0 sale del EMULADOR (`tools/emulador_ui.py`: la frase es larga y un `Dialog` no recibe la letra de una prueba).
 * No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea207CapturasTest*"`); los PNG van a `src/test/screenshots/t207/`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea207CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()
  private val day = 24L * 60 * 60 * 1000

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

  private fun at(d: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, d, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(mark: Int, d: Int, game: String = "stroop") =
    TimeTrialResult(id = "r-$game-$d-$mark", gameId = game, day = "2026-10-%02d".format(d), trackVersion = 2, hits = mark + 2, errors = 2, level = 4, seconds = 120, timestamp = at(d))

  /** La referencia estimada REAL del APK, como la ve la tienda. */
  private val estWorld: Comparison.Source by lazy {
    val dir = java.io.File("src/main/assets/${EstimatedReference.ASSET_DIR}")
    Comparison.ComposedSource(null, EstimatedReference.of(dir.listFiles { f -> f.extension == "json" }.orEmpty().sortedBy { it.name }.map { it.readText(Charsets.UTF_8) }))
  }

  private val now = at(28, 18)

  /** [n] partidas de Tinta (la última, de esta semana, con la marca 58; las anteriores, más flojas y de semanas pasadas). */
  private fun stroopGames(n: Int) = List(n) { i -> if (i == n - 1) r(58, 27) else r(40 + 3 * i, 12 + 3 * i) }

  private fun scrollTo(tag: String, marginDp: Int = 12) {
    val y = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
    rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, y - marginDp * 3f) }     // xxhdpi: 3 px por dp
    rule.waitForIdle()
  }

  private fun page(name: String, games: Int, widthDp: Int, heightDp: Int, to: String? = null) {
    rule.mainClock.autoAdvance = true
    val results = stroopGames(games)
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            ContrarrelojPage(results = results, selected = "stroop", onSelect = {}, now = now, compare = estWorld, ageRange = AgeRange.R30_44)
          }
        }
      }
    }
    rule.waitForIdle()
    if (to != null) scrollTo(to)
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t207/ahora_$name.png")
  }

  @Test fun a_pagina_1_partida_393_10() = page("page_1_393_10", 1, 393, 873)
  @Test fun b_pagina_3_partidas_393_10() = page("page_3_393_10", 3, 393, 873)
  @Test fun c_eje_393_10() = page("axis_393_10", 3, 393, 873, to = "contrarreloj_bell")
  @Test fun d_eje_360_10() = page("axis_360_10", 3, 360, 780, to = "contrarreloj_bell")

  // ---------------------------------------------------------------- la puerta de Avance, sin Plus (la pantalla real y su ViewModel)

  /** La puerta usa el juego de HOY; [count] marcas suyas de esta semana. */
  private fun seedAvance(count: Int): NeuroVidaViewModel {
    val repo = (app as NeuroVidaApplication).repository
    val real = System.currentTimeMillis()
    val n = runBlocking {
      val db = NeuroVidaDatabase.getDatabase(app)
      db.userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity())
      var c = 0
      for ((k, id) in listOf("secuencia", "parejas", "bodega", "radar", "stroop", "calculo", "anagramas").withIndex()) for (round in 0 until 4) {
        repo.recordGameResult(
          GamePlayResult(gameId = id, score = 70, correctAnswers = 8, totalTrials = 12, timed = false, level = 2, timestamp = real - (28 - 7 * round) * day + k * 3_600_000L, endRating = 0.25f + 0.08f * round + 0.01f * k, modeTrials = 10, modeHits = 8),
          countsForDailySession = false
        )
        c += 1
      }
      c
    }
    val model = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(30_000) { model.gameHistory.value.size >= n }
    val today = TimeTrialDay.gameFor(model.todayKey())
    val topFrom = Comparison.firstMarkReaching(90, estWorld.distribution(today, listOf(AgeRange.R45_54), 0)) ?: 0          // (Tarea 229: el percentil 90 que fue el peldaño de arriba)
    val doorMark = (topFrom - 7).coerceAtLeast(0)
    runBlocking {
      for (i in 0 until count) {
        val mark = if (i == count - 1) doorMark else (doorMark - 3 - 2 * i).coerceAtLeast(0)
        repo.recordTimeTrial(TimeTrialResult(id = "avance-$i", gameId = today, day = model.todayKey(), trackVersion = TimeTrialDay.trackVersion(today), hits = mark + 2, errors = 2, level = 4, seconds = 120, timestamp = real - (count - 1 - i) * 60_000L))
      }
    }
    TestSupport.awaitUntil(30_000) { model.timeTrialResults.value.size >= count }
    model.setAgeRange(AgeRange.R45_54)
    TestSupport.awaitUntil(30_000) { model.ageRange.value == AgeRange.R45_54 }
    TestSupport.awaitUntil(30_000) { model.estimatedReference.value != null }
    model.setTab(AppTab.PROGRESO)
    return model
  }

  private fun door(name: String, count: Int) {
    val vm = seedAvance(count)
    vm.setPlus(false)
    rule.mainClock.autoAdvance = true
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp)) { BoardBackground(); ProgressScreen(viewModel = vm) } }
      }
    }
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t207/ahora_$name.png")
  }

  @Test fun e_puerta_1_partida_393_10() = door("door_1_393_10", 1)
  @Test fun f_puerta_3_partidas_393_10() = door("door_3_393_10", 3)
}
