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
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onFirst
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollToNode
import androidx.compose.ui.test.performSemanticsAction
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.Comparison
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialTrials
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
 * La lámina «antes (T203b) | ahora» de la Tarea 205 (`docs/previews/tarea-205.png`): la página «Contrarreloj», la puerta de Avance y la hoja del candado con la REFERENCIA ESTIMADA que ahora trae el APK (la tienda también compara): la frase larga una sola vez bajo el rótulo, «referencia estimada»
 * en cada losa y nunca «personas», sin «Todas las edades», la hoja sin promesas y con «Cerrar»; y el eje y la escalera de cada juego (Rescate relámpago de a 1 acierto). «antes» sale de las capturas de la Tarea 203b (`t203b/`); «ahora», de esta prueba. No corre en las pruebas de siempre: solo al grabar
 * (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea205CapturasTest*"`); los PNG van a `src/test/screenshots/t205/` y `tools/previews/tarea_205.py` arma la hoja. Mismos datos de la Tarea 203b, con la referencia REAL de `assets/referencia/` (la que lee la app).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea205CapturasTest {
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
  private fun r(mark: Int, d: Int, errors: Int = 2, level: Int = 4, game: String = "stroop", version: Int = 2) =
    TimeTrialResult(id = "r-$game-$d-$mark", gameId = game, day = "2026-10-%02d".format(d), trackVersion = version, hits = mark + errors, errors = errors, level = level, seconds = 120, timestamp = at(d))

  private fun paceTrials(hits: List<Int>, errors: List<Int>, levels: List<Int> = listOf(1, 2, 4, 4)): TimeTrialTrials {
    val t = mutableListOf<Int>(); val ok = mutableListOf<Int>(); val lv = mutableListOf<Int>()
    for (h in 0..3) {
      val n = hits[h] + errors[h]
      for (k in 0 until n) { t += h * 30_000 + 500 + k * (28_000 / maxOf(n, 1)); ok += if (k < hits[h]) 1 else 0; lv += levels[h] }
    }
    return TimeTrialTrials.of(t, ok, lv)!!
  }

  /** La referencia estimada REAL del APK, como la ve la tienda. */
  private val estWorld: Comparison.Source by lazy {
    val dir = java.io.File("src/main/assets/${com.example.data.EstimatedReference.ASSET_DIR}")
    Comparison.ComposedSource(null, com.example.data.EstimatedReference.of(dir.listFiles { f -> f.extension == "json" }.orEmpty().sortedBy { it.name }.map { it.readText(Charsets.UTF_8) }))
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

  private fun page(
    name: String, widthDp: Int, heightDp: Int, scale: Float, world: Comparison.Source? = estWorld, results: List<TimeTrialResult> = ten + others,
    to: String? = null, margin: Int = 12, range: AgeRange? = AgeRange.R45_54, selected: String = "stroop"
  ) {
    rule.mainClock.autoAdvance = true
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            ContrarrelojPage(results = results, selected = selected, onSelect = {}, onBack = {}, now = now, compare = world, ageRange = range)
          }
        }
      }
    }
    rule.waitForIdle()
    if (to != null) scrollTo(to, margin)
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t205/ahora_$name.png")
  }

  private val septemberMarks = listOf(r(34, 1), r(36, -5), r(38, -12))      // las tres semanas anteriores a octubre, para tener siete semanas con marca
  private val radarMarks = listOf(r(9, 5, game = "radar"), r(10, 12, game = "radar"), r(11, 19, game = "radar"), r(12, 22, game = "radar"), r(13, 26, game = "radar"), r(14, 27, game = "radar"))
  private val acopMarks = listOf(r(14, 12, game = "acoplamiento"), r(18, 19, game = "acoplamiento"), r(22, 24, game = "acoplamiento"), r(24, 27, game = "acoplamiento"))

  @Test fun a_arriba_393_10() = page("top_393_10", 393, 873, 1f)
  @Test fun b_arriba_393_20() = page("top_393_20", 393, 873, 2f)
  @Test fun c_arriba_360_10() = page("top_360_10", 360, 780, 1f)
  @Test fun d_arriba_360_20() = page("top_360_20", 360, 780, 2f)
  @Test fun e_escalera_393_10() = page("ladder_393_10", 393, 873, 1f, to = "contrarreloj_ladder")
  @Test fun f_campana_393_10() = page("bell_393_10", 393, 873, 1f, to = "contrarreloj_bell")
  @Test fun g_campana_360_10() = page("bell_360_10", 360, 780, 1f, to = "contrarreloj_bell")
  @Test fun h_otras_edades_393_10() = page("others_393_10", 393, 873, 1f, to = "contrarreloj_others")
  @Test fun i_semanas_393_10() = page("weeks_393_10", 393, 873, 1f, to = "contrarreloj_percweeks")
  @Test fun j_rescate_arriba_393_10() = page("radar_top_393_10", 393, 873, 1f, results = radarMarks, selected = "radar")
  @Test fun k_rescate_escalera_393_10() = page("radar_ladder_393_10", 393, 873, 1f, results = radarMarks, selected = "radar", to = "contrarreloj_ladder")
  @Test fun l_rescate_campana_393_10() = page("radar_bell_393_10", 393, 873, 1f, results = radarMarks, selected = "radar", to = "contrarreloj_bell")
  @Test fun m_acoplamiento_campana_393_10() = page("acop_bell_393_10", 393, 873, 1f, results = acopMarks, selected = "acoplamiento", to = "contrarreloj_bell")
  @Test fun n_siete_semanas_360_20() = page("weeks7_360_20", 360, 780, 2f, results = ten + others + septemberMarks, to = "contrarreloj_percweeks")
  @Test fun o_escalera_pie_360_10() = page("ladder_foot_360_10", 360, 780, 1f, to = "contrarreloj_ladder_need", margin = 330)
  @Test fun p_sin_comparacion_393_10() = page("nocompare_393_10", 393, 873, 1f, world = null)

  // ---------------------------------------------------------------- Avance con la puerta y su frase gratis (con la pantalla real y su ViewModel)

  private fun seedAvance(): NeuroVidaViewModel {
    val repo = (app as NeuroVidaApplication).repository
    val real = System.currentTimeMillis()
    val n = runBlocking {
      val db = NeuroVidaDatabase.getDatabase(app)
      db.userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity())
      var count = 0
      for ((k, id) in listOf("secuencia", "parejas", "bodega", "radar", "stroop", "calculo", "anagramas").withIndex()) for (round in 0 until 4) {
        repo.recordGameResult(
          GamePlayResult(gameId = id, score = 70, correctAnswers = 8, totalTrials = 12, timed = false, level = 2, timestamp = real - (28 - 7 * round) * day + k * 3_600_000L, endRating = 0.25f + 0.08f * round + 0.01f * k, modeTrials = 10, modeHits = 8),
          countsForDailySession = false
        )
        count += 1
      }
      count
    }
    val model = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(30_000) { model.gameHistory.value.size >= n }
    // las marcas del juego de HOY (el de la puerta): las de antes, de la semana pasada hacia atrás (44 es el récord), y UNA de este momento sembrada para que la puerta diga la misma meta que la página de Tinta o Palabra: «Te faltan 7 aciertos para En la Luna.»
    // (el juego del día cambia con la fecha real y la muestra corre la silueta por juego: por eso la marca se calcula con la escalera de ESE juego)
    val today = TimeTrialDay.gameFor(model.todayKey())
    // el peldaño de arriba de la escalera de ESE juego (en Rescate relámpago «En la Luna» no tiene peldaño propio: el percentil 75 y el 90 se alcanzan con el mismo acierto)
    val topRung = Comparison.thresholds(estWorld.distribution(today, listOf(AgeRange.R45_54), 0)).last()
    val doorMark = (topRung.from - 7).coerceAtLeast(0)
    runBlocking {
      listOf(31, 35, 39, 36, 44, 38, 41, 37, 42).forEachIndexed { i, m ->
        repo.recordTimeTrial(TimeTrialResult(id = "avance-$i", gameId = today, day = model.todayKey(), trackVersion = TimeTrialDay.trackVersion(today), hits = m + 2, errors = 2, level = 4, seconds = 120, timestamp = real - (17 - i) * day))
      }
      repo.recordTimeTrial(TimeTrialResult(id = "avance-now", gameId = today, day = model.todayKey(), trackVersion = TimeTrialDay.trackVersion(today), hits = doorMark + 2, errors = 2, level = 4, seconds = 120, timestamp = real))
    }
    TestSupport.awaitUntil(30_000) { model.timeTrialResults.value.size >= 10 }
    model.setAgeRange(AgeRange.R45_54)              // sin franja elegida no hay con quién compararse (la tienda tampoco)
    TestSupport.awaitUntil(30_000) { model.ageRange.value == AgeRange.R45_54 }
    model.setTab(AppTab.PROGRESO)
    return model
  }

  private fun avance(name: String, plus: Boolean, scale: Float = 1f, sheet: Boolean = false, widthDp: Int = 393, heightDp: Int = 873) {
    val vm = seedAvance()
    vm.setPlus(plus)
    rule.mainClock.autoAdvance = true
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true, com.example.ui.components.LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp)) { BoardBackground(); ProgressScreen(viewModel = vm) } }
      }
    }
    rule.waitForIdle()
    if (sheet) {
      // con letra 2,0 la puerta queda fuera de la ventana: se baja la lista hasta ella antes de tocarla
      rule.onAllNodes(androidx.compose.ui.test.hasScrollAction()).onFirst().performScrollToNode(hasTestTag("avance_contrarreloj_door"))
      rule.waitForIdle()
      rule.onNode(hasTestTag("avance_contrarreloj_door")).performClick(); rule.waitForIdle()
    }
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t205/ahora_$name.png")
  }

  @Test fun u_avance_sin_plus_393_10() = avance("avance_sinplus_393_10", plus = false)
  @Test fun v_hoja_del_candado_393_10() = avance("sheet_393_10", plus = false, sheet = true)
  @Test fun w_avance_sin_plus_393_20() = avance("avance_sinplus_393_20", plus = false, scale = 2f)
  @Test fun x_hoja_del_candado_393_20() = avance("sheet_393_20", plus = false, scale = 2f, sheet = true)
  @Test fun y_avance_sin_plus_360_10() = avance("avance_sinplus_360_10", plus = false, widthDp = 360, heightDp = 780)
  @Test fun z_hoja_del_candado_360_10() = avance("sheet_360_10", plus = false, sheet = true, widthDp = 360, heightDp = 780)

  // ---------------------------------------------------------------- «[Debug] Contrarreloj de invitado»: el final que solo muestra la marca

  @Test fun za_invitado_393_10() {
    rule.mainClock.autoAdvance = true
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp)) { BoardBackground(); com.example.games.GuestTrialScreen(trial = com.example.games.GuestTrial("stroop", 36, 2), onAgain = {}, onDone = {}) } }
      }
    }
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t205/ahora_guest_393_10.png")
  }
}
