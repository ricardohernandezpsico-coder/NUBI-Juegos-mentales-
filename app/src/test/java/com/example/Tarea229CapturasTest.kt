package com.example

import android.app.Application
import androidx.activity.ComponentActivity
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.positionInRoot
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
import com.example.data.League
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.AgeRange
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.model.UserSettings
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NeuroNavBar
import com.example.ui.screens.ContrarrelojPage
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeUi
import com.example.ui.screens.LeagueSheetContent
import com.example.ui.screens.TuScreen
import com.example.ui.screens.homeStops
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
 * La lámina de la Tarea 229 (`docs/previews/tarea-229.png`, `tools/previews/tarea_229.py`): la liga de la semana. La losa de la liga en sus seis estados (sube, se queda, baja, sin %, en la más alta y primera semana), la escalera de las diez divisiones con la persona abajo, al medio y arriba, los cuatro cuadros de
 * «Tus juegos de esta semana» con y sin Plus, la línea de Hoy, la hoja del lunes en sus casos y la fila de Tú. Todo con la referencia estimada REAL del APK. No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea229CapturasTest*"`);
 * los PNG van a `src/test/screenshots/t229/` (fuera de git). Cuidado: la hoja del lunes se captura SIN su `Dialog` (un `Dialog` ignora la densidad de la prueba y la captura «a 2,0» saldría a 1,0).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea229CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

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

  /** Un miércoles (4 de noviembre de 2026) al mediodía: la semana empezó el lunes 2. */
  private val now: Long = Calendar.getInstance().apply { clear(); set(2026, Calendar.NOVEMBER, 4, 12, 0) }.timeInMillis
  private fun wk(ago: Int, hours: Long = 5) = Week.weekStart(now, ago) + hours * 3_600_000L

  private val estWorld: Comparison.Source by lazy {
    val dir = java.io.File("src/main/assets/${EstimatedReference.ASSET_DIR}")
    Comparison.ComposedSource(null, EstimatedReference.of(dir.listFiles { f -> f.extension == "json" }.orEmpty().sortedBy { it.name }.map { it.readText(Charsets.UTF_8) }))
  }

  /** La marca con que [game] llega al [pct] % de la referencia de 45 a 54 años. */
  private fun markFor(game: String, pct: Int): Int = Comparison.firstMarkReaching(pct, estWorld.distribution(game, listOf(AgeRange.R45_54), 0)) ?: 0

  /** Tres partidas de cada juego de semanas atrás (así cuentan) y, si [pct] no es null, la de esta semana de cada uno con la marca de ese %. */
  private fun resultsFor(pct: Int?, games: List<String> = TimeTrialDay.GAMES): List<TimeTrialResult> = games.flatMap { g ->
    fun res(id: String, mark: Int, at: Long) = TimeTrialResult(id = "$g-$id", gameId = g, day = "2026-10-20", trackVersion = TimeTrialDay.trackVersion(g), hits = mark, errors = 0, level = 3, seconds = 120, timestamp = at)
    listOf(res("a", 8, wk(5)), res("b", 10, wk(5, 30)), res("c", 12, wk(4))) + (if (pct != null) listOf(res("w", markFor(g, pct), wk(0, 20))) else emptyList())
  }

  private fun leagueIn(d: Comparison.Division) = if (d == Comparison.Division.PLATFORM) League.State(startWeek = Week.weekStart(now))
    else League.State(startWeek = Week.weekStart(now, 3), weeks = listOf(League.Closed(Week.weekStart(now, 1), d.from + 5, d.down ?: d, d)))

  private var fontState by mutableStateOf(1f)

  private fun frame(widthDp: Int, heightDp: Int, scale: Float = 1f, content: @androidx.compose.runtime.Composable () -> Unit) {
    rule.mainClock.autoAdvance = true
    fontState = scale
    org.robolectric.RuntimeEnvironment.setQualifiers("w${widthDp}dp-h${heightDp}dp-xxhdpi")          // la captura mide lo que mide la pantalla: un marco alto necesita una pantalla alta
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontState), LocalReduceMotion provides true, LocalBarInset provides 88.dp) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823))) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t229/$name.png") }

  private fun scrollTo(tag: String, marginDp: Int = 12) {
    val y = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
    rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, y - marginDp * 3f) }     // xxhdpi: 3 px por dp
    rule.waitForIdle()
  }

  // ---------------------------------------------------------------- la pestaña

  private fun page(name: String, league: League.State, pct: Int?, plus: Boolean = true, widthDp: Int = 393, heightDp: Int = 873, scale: Float = 1f, scrollTag: String? = null, games: List<String> = TimeTrialDay.GAMES) {
    frame(widthDp, heightDp, scale) {
      Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) {
        Box(Modifier.padding(top = 24.dp)) {
          ContrarrelojPage(results = resultsFor(pct, games), selected = "stroop", onSelect = {}, now = now, compare = estWorld, ageRange = AgeRange.R45_54, plus = plus, today = "stroop", league = league)
        }
      }
    }
    if (scrollTag != null) scrollTo(scrollTag, 8)
    save(name)
  }

  // la losa de la liga, en sus seis estados (393 dp con letra 1,0)
  @Test fun liga_sube_393_10() = page("liga_sube_393_10", leagueIn(Comparison.Division.TAKEOFF), 25)
  @Test fun liga_queda_393_10() = page("liga_queda_393_10", leagueIn(Comparison.Division.TAKEOFF), 14)
  @Test fun liga_baja_393_10() = page("liga_baja_393_10", leagueIn(Comparison.Division.TAKEOFF), 4)
  @Test fun liga_sin_porcentaje_393_10() = page("liga_sin_porcentaje_393_10", leagueIn(Comparison.Division.TAKEOFF), null)
  @Test fun liga_en_la_mas_alta_393_10() = page("liga_en_la_mas_alta_393_10", leagueIn(Comparison.Division.GALAXY), 95)
  @Test fun liga_primera_semana_393_10() = page("liga_primera_semana_393_10", leagueIn(Comparison.Division.PLATFORM), null)

  // a 360 dp con letra 1,3 y a 393 y 360 con letra 2,0 (más altos: la losa crece)
  @Test fun liga_sube_360_13() = page("liga_sube_360_13", leagueIn(Comparison.Division.TAKEOFF), 25, widthDp = 360, heightDp = 780, scale = 1.3f)
  @Test fun liga_baja_360_13() = page("liga_baja_360_13", leagueIn(Comparison.Division.TAKEOFF), 4, widthDp = 360, heightDp = 780, scale = 1.3f)
  @Test fun liga_queda_en_la_luna_360_13() = page("liga_queda_en_la_luna_360_13", leagueIn(Comparison.Division.MOON), 43, widthDp = 360, heightDp = 780, scale = 1.3f)
  @Test fun liga_sube_393_20() = page("liga_sube_393_20", leagueIn(Comparison.Division.TAKEOFF), 25, heightDp = 1500, scale = 2f)
  @Test fun liga_sin_porcentaje_360_20() = page("liga_sin_porcentaje_360_20", leagueIn(Comparison.Division.CLOUDS), null, widthDp = 360, heightDp = 1500, scale = 2f)
  @Test fun liga_en_la_mas_alta_360_20() = page("liga_en_la_mas_alta_360_20", leagueIn(Comparison.Division.GALAXY), 95, widthDp = 360, heightDp = 1500, scale = 2f)

  // la escalera de las diez divisiones: la persona abajo, al medio y arriba
  @Test fun escalera_abajo_393_10() = page("escalera_abajo_393_10", leagueIn(Comparison.Division.PLATFORM), null, heightDp = 1100, scrollTag = "contrarreloj_league_ladder")
  @Test fun escalera_medio_393_10() = page("escalera_medio_393_10", leagueIn(Comparison.Division.MOON), 43, heightDp = 1100, scrollTag = "contrarreloj_league_ladder")
  @Test fun escalera_arriba_393_10() = page("escalera_arriba_393_10", leagueIn(Comparison.Division.GALAXY), 95, heightDp = 1100, scrollTag = "contrarreloj_league_ladder")
  @Test fun escalera_medio_360_13() = page("escalera_medio_360_13", leagueIn(Comparison.Division.CLOUDS), 25, widthDp = 360, heightDp = 1300, scale = 1.3f, scrollTag = "contrarreloj_league_ladder")
  @Test fun escalera_abajo_360_20() = page("escalera_abajo_360_20", leagueIn(Comparison.Division.PLATFORM), null, widthDp = 360, heightDp = 1700, scale = 2f, scrollTag = "contrarreloj_league_ladder")

  // los cuatro cuadros de «Tus juegos de esta semana», con y sin Plus; una semana con tres juegos y uno sin jugar; y uno que todavía no cuenta
  @Test fun cuadros_con_plus_393_10() = page("cuadros_con_plus_393_10", leagueIn(Comparison.Division.CLOUDS), 25, heightDp = 700, scrollTag = "contrarreloj_selector", games = listOf("stroop", "disparate", "acoplamiento"))
  @Test fun cuadros_sin_plus_393_10() = page("cuadros_sin_plus_393_10", leagueIn(Comparison.Division.CLOUDS), 25, plus = false, heightDp = 700, scrollTag = "contrarreloj_selector", games = listOf("stroop", "disparate", "acoplamiento"))
  @Test fun cuadros_sin_plus_360_13() = page("cuadros_sin_plus_360_13", leagueIn(Comparison.Division.CLOUDS), 25, plus = false, widthDp = 360, heightDp = 900, scale = 1.3f, scrollTag = "contrarreloj_selector", games = listOf("stroop", "disparate", "acoplamiento"))
  @Test fun cuadros_con_plus_360_20() = page("cuadros_con_plus_360_20", leagueIn(Comparison.Division.CLOUDS), 25, widthDp = 360, heightDp = 1500, scale = 2f, scrollTag = "contrarreloj_selector", games = listOf("stroop", "disparate", "acoplamiento"))

  // la página entera sin Plus (la parte gratis y las dos losas grises)
  @Test fun pagina_sin_plus_393_10() = page("pagina_sin_plus_393_10", leagueIn(Comparison.Division.TAKEOFF), 25, plus = false, heightDp = 2200)

  // ---------------------------------------------------------------- Hoy

  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, record = 41)
  private fun live(d: Comparison.Division, course: League.Course, target: Comparison.Division = d, percent: Int? = if (course == League.Course.NONE) null else d.from + 5) = League.Live(d, percent, course, target, d == Comparison.Division.PLATFORM)

  private fun home(name: String, league: League.Live?, t: HomeTimeTrial = stroop, width: Int = 393, height: Int = 873, scale: Float = 1f) {
    val ui = HomeUi(
      name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps, stops = homeStops(listOf("bodega", "calculo", "intrusa"), 1, AppLanguage.SPANISH),
      pausedTitle = null, flightDay = false, hasBaseline = true, timeTrial = t, league = league
    )
    frame(width, height, scale) {
      Box(Modifier.fillMaxSize()) {
        HomeContent(ui = ui, onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {}, onLocked = {}, modifier = Modifier.padding(top = 24.dp, bottom = 100.dp))
        Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = AppTab.HOY, onSelect = {}) }
      }
    }
    save(name)
  }

  @Test fun hoy_sube_393_10() = home("hoy_sube_393_10", live(Comparison.Division.TAKEOFF, League.Course.UP, Comparison.Division.CLOUDS))
  @Test fun hoy_queda_393_10() = home("hoy_queda_393_10", live(Comparison.Division.TAKEOFF, League.Course.STAY))
  @Test fun hoy_baja_393_10() = home("hoy_baja_393_10", live(Comparison.Division.TAKEOFF, League.Course.DOWN, Comparison.Division.PLATFORM))
  @Test fun hoy_sin_porcentaje_393_10() = home("hoy_sin_porcentaje_393_10", live(Comparison.Division.TAKEOFF, League.Course.NONE))
  @Test fun hoy_plataforma_393_10() = home("hoy_plataforma_393_10", live(Comparison.Division.PLATFORM, League.Course.NONE))
  @Test fun hoy_la_mas_alta_393_10() = home("hoy_la_mas_alta_393_10", live(Comparison.Division.GALAXY, League.Course.TOP))
  @Test fun hoy_sin_liga_393_10() = home("hoy_sin_liga_393_10", null)
  @Test fun hoy_larga_360_13() = home("hoy_larga_360_13", live(Comparison.Division.TAKEOFF, League.Course.UP, Comparison.Division.CLOUDS), width = 360, height = 780, scale = 1.3f)
  @Test fun hoy_larga_360_20() = home("hoy_larga_360_20", live(Comparison.Division.ASTEROIDS, League.Course.UP, Comparison.Division.JUPITER), width = 360, height = 1500, scale = 2f)
  @Test fun hoy_sin_partidas_393_10() = home("hoy_sin_partidas_393_10", live(Comparison.Division.MARS, League.Course.STAY), stroop.copy(state = TimeTrialState.JUGADO, todayHits = 38, gamesLeft = 0, tomorrowTitle = "¿Verdad o disparate?"))

  // ---------------------------------------------------------------- la hoja del lunes (sin su Dialog)

  private fun sheet(name: String, closed: League.Closed, width: Int = 393, height: Int = 620, scale: Float = 1f) {
    frame(width, height, scale) {
      Box(Modifier.fillMaxSize().padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) { LeagueSheetContent(League.sheet(closed), onClose = {}) }
    }
    save(name)
  }

  @Test fun hoja_sube_393_10() = sheet("hoja_sube_393_10", League.Closed(0L, 51, Comparison.Division.TAKEOFF, Comparison.Division.CLOUDS))
  @Test fun hoja_queda_393_10() = sheet("hoja_queda_393_10", League.Closed(0L, 12, Comparison.Division.TAKEOFF, Comparison.Division.TAKEOFF))
  @Test fun hoja_baja_393_10() = sheet("hoja_baja_393_10", League.Closed(0L, 4, Comparison.Division.TAKEOFF, Comparison.Division.PLATFORM))
  @Test fun hoja_sin_porcentaje_393_10() = sheet("hoja_sin_porcentaje_393_10", League.Closed(0L, null, Comparison.Division.CLOUDS, Comparison.Division.TAKEOFF))
  @Test fun hoja_plataforma_sin_porcentaje_393_10() = sheet("hoja_plataforma_sin_porcentaje_393_10", League.Closed(0L, null, Comparison.Division.PLATFORM, Comparison.Division.PLATFORM))
  @Test fun hoja_la_mas_alta_393_10() = sheet("hoja_la_mas_alta_393_10", League.Closed(0L, 95, Comparison.Division.GALAXY, Comparison.Division.GALAXY))
  @Test fun hoja_sube_360_13() = sheet("hoja_sube_360_13", League.Closed(0L, 51, Comparison.Division.TAKEOFF, Comparison.Division.CLOUDS), 360, 760, 1.3f)
  @Test fun hoja_sube_393_20() = sheet("hoja_sube_393_20", League.Closed(0L, 51, Comparison.Division.TAKEOFF, Comparison.Division.CLOUDS), 393, 1100, 2f)
  @Test fun hoja_baja_360_20() = sheet("hoja_baja_360_20", League.Closed(0L, 4, Comparison.Division.ASTEROIDS, Comparison.Division.MARS), 360, 1100, 2f)

  // ---------------------------------------------------------------- Tú

  private fun tu(name: String, division: Comparison.Division, width: Int = 393, height: Int = 873, scale: Float = 1f) {
    runBlocking { NeuroVidaDatabase.getDatabase(app).userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity()) }
    app.getSharedPreferences("profile_extra", android.content.Context.MODE_PRIVATE).edit().putString("age_range", AgeRange.R45_54.name).commit()
    app.getSharedPreferences(League.PREFS, android.content.Context.MODE_PRIVATE).edit().putString(League.KEY, League.encode(leagueIn(division))).commit()
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(30_000, "El perfil no se cargó") { vm.userSettings.value.ageBand != null }
    TestSupport.awaitUntil(30_000, "La referencia no se cargó") { vm.estimatedReference.value != null }
    frame(width, height, scale) { Box(Modifier.fillMaxSize().padding(top = 24.dp)) { TuScreen(vm) } }
    save(name)
  }

  @Test fun tu_despegando_393_10() = tu("tu_despegando_393_10", Comparison.Division.TAKEOFF)
  @Test fun tu_en_la_plataforma_393_10() = tu("tu_en_la_plataforma_393_10", Comparison.Division.PLATFORM)
  @Test fun tu_entre_asteroides_360_13() = tu("tu_entre_asteroides_360_13", Comparison.Division.ASTEROIDS, 360, 780, 1.3f)
  @Test fun tu_en_otra_galaxia_360_20() = tu("tu_en_otra_galaxia_360_20", Comparison.Division.GALAXY, 360, 1400, 2f)
}
