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
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performScrollTo
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
 * Los cuadros «hoy» de la lámina de la Tarea 199 (`docs/previews/tarea-199.png`): Avance y el final del Contrarreloj COMO ERAN, sin la puerta ni el atajo. Usa solo lo que existe antes de la tarea (`ProgressScreen`, `TimeTrialResultScreen` con sus tres parámetros de siempre), así que se graba con el código de antes
 * (los archivos de la tarea apartados): `T199_PREFIX=hoy ./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea199HoyCapturasTest*"`. No corre en las pruebas de siempre.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea199HoyCapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private val prefix = System.getenv("T199_PREFIX") ?: "hoy"
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
  private fun r(mark: Int, d: Int, errors: Int = 2) =
    TimeTrialResult(id = "r-$d-$mark", gameId = "stroop", day = "2026-10-%02d".format(d), trackVersion = 2, hits = mark + errors, errors = errors, level = 4, seconds = 120, timestamp = at(d))

  @Test fun a_avance_de_antes() {
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
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(30_000) { vm.gameHistory.value.size >= n }
    val today = TimeTrialDay.gameFor(vm.todayKey())
    runBlocking {
      listOf(31, 35, 39, 36, 44, 38, 41, 37, 42, 43).forEachIndexed { i, m ->
        repo.recordTimeTrial(TimeTrialResult(id = "avance-$i", gameId = today, day = vm.todayKey(), trackVersion = TimeTrialDay.trackVersion(today), hits = m + 2, errors = 2, level = 4, seconds = 120, timestamp = real - (10 - i) * day))
      }
    }
    TestSupport.awaitUntil(30_000) { vm.timeTrialResults.value.size >= 10 }
    vm.setTab(AppTab.PROGRESO)
    rule.mainClock.autoAdvance = true
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp)) { BoardBackground(); ProgressScreen(viewModel = vm) } }
      }
    }
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t199/${prefix}_avance_plus.png")
  }

  @Test fun b_final_de_antes() {
    val ten = listOf(r(31, 8), r(35, 10), r(39, 13), r(36, 15), r(44, 17), r(38, 19, 3), r(41, 22, 3), r(37, 26, 3), r(42, 27, 3), r(43, 28, 3))
    val now = at(28, 18)
    val outcome = TimeTrialStats.outcome(ten, ten.last(), now)
    rule.mainClock.autoAdvance = true
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp)) { BoardBackground(); TimeTrialResultScreen(outcome = outcome, onAgain = {}, onDone = {}, soundEnabled = false) } }  // LLAMADA
      }
    }
    rule.waitForIdle()
    rule.onNodeWithTag("btn_timetrial_again").performScrollTo()
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t199/${prefix}_final_393_10.png")
  }
}
