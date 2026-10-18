package com.example

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.foundation.background
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.ColorVision
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.data.local.DailySessionEntity
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.games.GameResultScreen
import com.example.model.AgeBand
import com.example.model.DomainType
import com.example.model.GamePlayResult
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.model.GameRegistry
import com.example.model.UserSettings
import com.example.ui.components.BoardBackground
import com.example.ui.components.CosmosBackground
import com.example.ui.components.LocalTabSwipeLock
import com.example.ui.components.NeuroNavBar
import com.example.ui.screens.FirstFlightScreen
import com.example.ui.screens.FlightActions
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.LicensesScreen
import com.example.ui.screens.GamesLibraryScreen
import com.example.ui.screens.cardData
import com.example.ui.screens.HomeScreen
import com.example.ui.screens.ProfileScreen
import com.example.ui.screens.ProgressScreen
import com.example.ui.screens.SettingsPanel
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
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import kotlin.math.sin

/**
 * La app ACTUAL con las pantallas REALES y unos 20 días de uso de ejemplo (Tarea 121, parte 2): Hoy (con la isla, la ruta y la lámina de un área abierta), Juegos y Avance completas, perfil, Ajustes, la barra de abajo, el Primer vuelo y el resultado de una partida.
 * No corre en las pruebas de siempre: solo al grabar capturas (`./gradlew :app:recordRoborazziDebug --tests "*AppActualScreenshotTest*"`), y deja los PNG en `src/test/screenshots/app-actual/`; `tools/previews/app_actual.py` los lleva a `docs/previews/app-actual/` y arma `lamina.png`.
 * El marco es de 393 dp de ancho (1080 px) y alto variable: las pantallas que se deslizan se capturan enteras en una sola imagen alta.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h4200dp-440dpi", sdk = [36])
class AppActualScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()
  private val day = 24L * 60 * 60 * 1000

  @Before
  fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
  }

  @After
  fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  /** Unos 20 días de uso: de 2 a 3 partidas por día en rotación por los 17 juegos (con dos días sin jugar), puntajes y rating que suben despacio. */
  private fun seed() = runBlocking {
    val db = NeuroVidaDatabase.getDatabase(app)
    val repo = (app as NeuroVidaApplication).repository
    val now = System.currentTimeMillis()
    db.userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity())
    val games = GameRegistry.allGames.filter { !GameRegistry.isRetired(it.id) }.map { it.id }
    var k = 0
    for (d in 20 downTo 1) {
      if (d == 9 || d == 4) continue                                      // dos días sin jugar
      val plays = if (d % 3 == 0) 3 else 2
      for (p in 0 until plays) {
        val id = games[k++ % games.size]
        val trend = (20 - d) / 20f
        val score = (58 + trend * 24 + 6 * sin(k * 1.7)).toInt().coerceIn(40, 96)
        val rating = (0.30f + trend * 0.28f + 0.04f * sin(k * 2.3f).toFloat()).coerceIn(0.1f, 0.9f)
        val ts = now - d * day + (9 + p * 4) * 3_600_000L
        repo.recordGameResult(
          GamePlayResult(
            gameId = id, score = score, correctAnswers = 8 + (score / 20), totalTrials = 12, timed = p % 2 == 0, level = 2 + (20 - d) / 8, timestamp = ts, endRating = rating,
            modeTrials = 10, modeHits = 7 + p
          ),
          countsForDailySession = false
        )
      }
    }
    val today = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())
    db.dailySessionDao().insertOrUpdate(DailySessionEntity(dateKey = today, gameIdsRaw = "bodega,calculo,intrusa", completedCount = 1, scoresRaw = "78"))
  }

  /** La barra de pestañas como en la app: abajo, sobre la barra de navegación del sistema (48 dp). */
  @androidx.compose.runtime.Composable
  private fun androidx.compose.foundation.layout.BoxScope.NavBar(tab: AppTab) {
    NeuroNavBar(current = tab, onSelect = {}, modifier = Modifier.align(Alignment.BottomCenter).padding(bottom = 48.dp))
  }

  private enum class Scene { HOY, JUEGOS, FICHA, AVANCE, PERFIL, AJUSTES, LICENCIAS, BARRA, VUELO_1, VUELO_2, VUELO_3, RESULTADO }

  private val rastro = GamePlayResult(
    gameId = "secuencia", score = 78, correctAnswers = 14, totalTrials = 19, timed = false, level = 9, endRating = 0.52f, timestamp = System.currentTimeMillis(),
    rasBestLen = listOf(5, 4, 3, 2), rasRounds = listOf(7, 4, 5, 3), rasHits = listOf(7, 3, 2, 2), rasModesSeen = 15, rasNewModes = 8
  )

  /** El marco de 393 dp con la escena puesta: [go] cambia de escena y alto, [shot] captura el marco. Cada prueba arma el suyo (la memoria de la prueba no alcanza para todas las escenas juntas). */
  private inner class Rig(val vm: NeuroVidaViewModel?, first: Scene) {
    var scene by mutableStateOf(first)
    var frameH by mutableStateOf(873)
    var sheetOpen by mutableStateOf(false)

    init {
      composeTestRule.mainClock.autoAdvance = false
      composeTestRule.setContent {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(frameH.dp).testTag("frame")) {
            when (scene) {
              Scene.VUELO_1, Scene.VUELO_2, Scene.VUELO_3 -> CosmosBackground()
              else -> BoardBackground()
            }
            CompositionLocalProvider(LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
              // el margen de las barras del sistema: 24 dp arriba y 48 abajo (y la barra de pestañas en las tres pantallas principales)
              val top = Modifier.padding(top = 24.dp)
              when (scene) {
                Scene.HOY -> Box(Modifier.fillMaxSize()) {
                  Box(top.padding(bottom = 163.dp)) { HomeScreen(viewModel = vm!!, onNavigateToGames = {}) }
                  if (!sheetOpen) NavBar(AppTab.HOY)
                }
                Scene.JUEGOS -> Box(Modifier.fillMaxSize()) {
                  Box(top.padding(bottom = 163.dp)) { GamesLibraryScreen(viewModel = vm!!) }
                  NavBar(AppTab.JUEGOS)
                }
                Scene.FICHA -> Box(Modifier.fillMaxSize().background(Color(0xFF050823)).padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.BottomCenter) {
                  val g = GameRegistry.getById("radar")!!
                  val now = System.currentTimeMillis()
                  val glance = listOf(260f, 240f, 250f, 210f, 190f, 170f).let { v -> v.mapIndexed { i, x -> MeasurePoint(now - (v.size - i) * day, "glance", x, 0.5f, true) } }
                  val data = cardData(g, g.title, now - day, now, 0.55f, glance, 6)
                  GameSheetContent(data, AgeBand.ADULT, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = true, onChooseTimed = {})
                }
                Scene.AVANCE -> Box(Modifier.fillMaxSize()) {
                  Box(top.padding(bottom = 163.dp)) { ProgressScreen(viewModel = vm!!) }
                  NavBar(AppTab.PROGRESO)
                }
                Scene.PERFIL -> Box(top) { ProfileScreen(viewModel = vm!!, onClose = {}) }
                Scene.AJUSTES -> Box(top) { SettingsPanel(viewModel = vm!!, onClose = {}) }
                Scene.LICENCIAS -> Box(top) { LicensesScreen(onClose = {}) }
                Scene.BARRA -> Box(Modifier.fillMaxSize()) { NavBar(AppTab.HOY) }
                Scene.VUELO_1 -> FirstFlightScreen(FlightState(FlightMode.FULL, FlightStep.HOLA), emptyList(), FlightActions.None)
                Scene.VUELO_2 -> FirstFlightScreen(
                  FlightState(FlightMode.FULL, FlightStep.METAS, name = "Ana", age = AgeBand.ADULT, goals = setOf(DomainType.MEMORIA, DomainType.RAZONAMIENTO)), emptyList(), FlightActions.None
                )
                Scene.VUELO_3 -> FirstFlightScreen(FlightState(FlightMode.FULL, FlightStep.CAMINO), listOf("bodega", "calculo", "intrusa"), FlightActions.None)
                Scene.RESULTADO -> GameResultScreen(
                  result = rastro, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
                  starMeasures = emptyList(), onPlayAgain = {}, onContinue = {}
                )
              }
            }
          }
        }
      }
    }

    fun settle() {
      repeat(3) {
        composeTestRule.mainClock.advanceTimeBy(600)
        composeTestRule.waitForIdle()
      }
    }

    fun go(s: Scene, height: Int) {
      composeTestRule.runOnIdle { scene = s; frameH = height }
      settle()
    }

    fun shot(name: String, height: Int) {
      composeTestRule.runOnIdle { frameH = height }
      settle()
      composeTestRule.onNodeWithTag("frame").captureRoboImage(filePath = "src/test/screenshots/app-actual/$name.png")
    }
  }

  private fun viewModel(): NeuroVidaViewModel {
    seed()
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(30_000) { vm.gameHistory.value.size >= 40 }
    return vm
  }

  @Test
  fun hoy_y_juegos() {
    val r = Rig(viewModel(), Scene.HOY)
    r.settle(); r.shot("hoy", 873)
    // la lámina de un área: se abre al tocar un edificio de la isla (con la lámina abierta la barra de pestañas se retira)
    composeTestRule.runOnIdle { r.sheetOpen = true }
    composeTestRule.onNodeWithTag("isla_observatorio").performClick()
    r.settle()
    r.shot("hoy-lamina", 873)
    composeTestRule.runOnIdle { r.sheetOpen = false }
    // Juegos: las cuatro áreas y, al tocar una, sus juegos; la ficha de un juego
    r.go(Scene.JUEGOS, 873); r.shot("juegos", 873)
    for (area in listOf("memoria", "atencion", "razonamiento", "lenguaje")) {
      composeTestRule.onNodeWithTag("area_$area").performClick()
      r.settle()
      r.shot("juegos-$area", 873)
      composeTestRule.runOnIdle { r.vm!!.closeLibraryArea() }
      r.settle()
    }
    r.go(Scene.FICHA, 873); r.shot("juegos-ficha", 873)
  }

  @Test
  fun avance_y_perfil() {
    val r = Rig(viewModel(), Scene.AVANCE)
    r.settle(); r.shot("avance", 873)
    r.shot("avance-completa", 3900)
    r.go(Scene.PERFIL, 873); r.shot("perfil", 873)
    r.shot("perfil-completa", 1700)
  }

  @Test
  fun ajustes_y_licencias() {
    val r = Rig(viewModel(), Scene.AJUSTES)
    r.settle(); r.shot("ajustes", 873)
    r.shot("ajustes-completa", 2600)
    r.go(Scene.LICENCIAS, 873); r.shot("licencias", 873)
    r.shot("licencias-completa", 2200)
  }

  @Test
  fun primer_vuelo_barra_y_resultado() {
    val r = Rig(null, Scene.BARRA)
    r.settle(); r.shot("barra", 873)
    r.go(Scene.VUELO_1, 873); r.shot("primer-vuelo-1", 873)
    r.go(Scene.VUELO_2, 873); r.shot("primer-vuelo-2", 873)
    r.go(Scene.VUELO_3, 873); r.shot("primer-vuelo-3", 873)
    r.go(Scene.RESULTADO, 873); r.shot("resultado", 873)
    r.shot("resultado-completa", 1600)
  }
}
