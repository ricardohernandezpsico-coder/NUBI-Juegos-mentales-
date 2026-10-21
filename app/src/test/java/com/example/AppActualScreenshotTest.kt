package com.example

import android.app.Application
import androidx.compose.foundation.background
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
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.Achievements
import com.example.data.AreaStatus
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.data.IslaPieza
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.data.local.DailySessionEntity
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.games.GameResultScreen
import com.example.model.AgeBand
import com.example.model.AppLanguage
import com.example.model.DomainType
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.UserSettings
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.AchievementOverlay
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.LocalTabSwipeLock
import com.example.ui.components.NeuroNavBar
import com.example.ui.screens.AreaDetail
import com.example.ui.screens.FirstFlightScreen
import com.example.ui.screens.FlightActions
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.GamesLibraryScreen
import com.example.ui.screens.HomeScreen
import com.example.ui.screens.LicensesScreen
import com.example.ui.screens.ProfileScreen
import com.example.ui.screens.ProgressScreen
import com.example.ui.screens.SettingsPanel
import com.example.ui.screens.cardData
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
 * La app ACTUAL con las pantallas REALES y unos 20 días de uso de ejemplo (Tarea 121, parte 2; ampliada en la Tarea 141, 19-oct, para reflejar las Tareas 121-140): Hoy (con la isla de 0 y de 10 piezas, la ruta y la lámina de un área abierta), Juegos (la grilla, la ventana
 * de un área y la ficha del juego plegada y desplegada), Avance (arriba, con Nubi, entera y el detalle de un área), el perfil, Ajustes (con las farolas), el resultado de una partida (con y sin récord), la celebración de una medalla con pieza de la isla y el Primer vuelo
 * (Hola, juego 1, edad, metas, punto de partida y camino). Las pantallas principales también con la letra del sistema al 130 % (`-letra13`).
 * No corre en las pruebas de siempre: solo al grabar capturas (`./gradlew :app:recordRoborazziDebug --tests "*AppActualScreenshotTest*"`), y deja los PNG en `src/test/screenshots/app-actual/`; `tools/previews/app_actual.py` los lleva a `docs/previews/app-actual/` y arma `lamina.png`.
 * El marco es de 393 dp de ancho (1080 px) y alto variable: las pantallas que se deslizan se capturan enteras en una sola imagen alta. Cada prueba arma su marco y su base (la memoria de una sola prueba no alcanza para todas las escenas juntas).
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
  private fun androidx.compose.foundation.layout.BoxScope.NavBar(tab: AppTab, onHeight: (Int) -> Unit = {}) {
    NeuroNavBar(current = tab, onSelect = {}, modifier = Modifier.align(Alignment.BottomCenter).onSizeChanged { if (it.height > 0) onHeight(it.height) })
  }

  /** Lo que mide la barra de pestañas a 393 dp de ancho (la bandeja con los cubos y las etiquetas, ~115 dp): la franja que cada pestaña deja libre (`LocalBarInset`). */
  private val BAR_DP = 115

  private enum class Scene {
    HOY, JUEGOS, FICHA, AVANCE, AREA_DETALLE, PERFIL, AJUSTES, LICENCIAS, BARRA, VUELO_1, VUELO_JUEGO, VUELO_EDAD, VUELO_2, VUELO_PUNTO, VUELO_3, RESULTADO, RESULTADO_RECORD, RESULTADO_CON, MEDALLA_PIEZA, MEDALLA_SIN
  }

  private val rastro = GamePlayResult(
    gameId = "secuencia", score = 78, correctAnswers = 14, totalTrials = 19, timed = false, level = 9, endRating = 0.52f, timestamp = System.currentTimeMillis(),
    rasBestLen = listOf(5, 4, 3, 2), rasRounds = listOf(7, 4, 5, 3), rasHits = listOf(7, 3, 2, 2), rasModesSeen = 15, rasNewModes = 8
  )

  /** Constelaciones: la misma partida sin récord y con récord (el final con sentido dice «¡Récord!» y suena el `Record`). */
  private fun constelaciones(record: Boolean) = GamePlayResult(
    gameId = "parejas", score = 74, correctAnswers = 7, totalTrials = 9, timed = false, level = 8, endRating = 0.5f, timestamp = System.currentTimeMillis(),
    conGroup = 4, conGroups = 6, conMemGroups = 4, conBestStreak = 5, conBest = 6, conNewRecord = record
  )

  private val statuses = listOf(
    AreaStatus("MEMORIA", 0.52f, 0.06f, listOf(0.40f, 0.43f, 0.46f, 0.52f)),
    AreaStatus("ATENCION", 0.62f, 0.04f, listOf(0.50f, 0.56f, 0.58f, 0.62f)),
    AreaStatus("RAZONAMIENTO", 0.43f, 0f, listOf(0.43f, 0.43f, 0.43f, 0.43f)),
    AreaStatus("LENGUAJE", 0.12f, 0.03f, listOf(0.06f, 0.08f, 0.09f, 0.12f))
  )

  /** El marco de 393 dp con la escena puesta: [go] cambia de escena y alto, [shot] captura el marco (con la letra del sistema que se pida: 1,0 o 1,3). */
  private inner class Rig(val vm: NeuroVidaViewModel?, first: Scene) {
    var scene by mutableStateOf(first)
    var frameH by mutableStateOf(873)
    var sheetOpen by mutableStateOf(false)
    var fontScale by mutableStateOf(1f)
    var barPx by mutableIntStateOf(0)                    // lo que mide la barra de pestañas (`MainActivity` lo mide igual): con la letra al 130 % es más alta que los 115 dp de la letra normal

    init {
      composeTestRule.mainClock.autoAdvance = false
      composeTestRule.setContent {
        val base = LocalDensity.current
        CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale)) {
          NeuroVidaTheme {
            Box(Modifier.width(393.dp).height(frameH.dp).testTag("frame")) {
              BoardBackground()                       // un solo cielo, también en el Primer vuelo (Tarea 147, P2)
              CompositionLocalProvider(LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
                // el margen de las barras del sistema: 24 dp arriba y 48 abajo (y la barra de pestañas en las tres pantallas principales)
                val top = Modifier.padding(top = 24.dp)
                val barInset = with(LocalDensity.current) { if (barPx > 0) barPx.toDp() else BAR_DP.dp }
                when (scene) {
                  Scene.HOY -> Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) {                    // 48 dp de la barra de navegación del sistema; la barra de pestañas es una capa fija (Tarea 123) y la página deja su franja libre con LocalBarInset
                    CompositionLocalProvider(LocalBarInset provides barInset) { Box(top) { HomeScreen(viewModel = vm!!, onNavigateToGames = {}) } }
                    if (!sheetOpen) NavBar(AppTab.HOY) { barPx = it }
                  }
                  Scene.JUEGOS -> Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) {
                    CompositionLocalProvider(LocalBarInset provides barInset) { Box(top) { GamesLibraryScreen(viewModel = vm!!) } }
                    if (!sheetOpen) NavBar(AppTab.JUEGOS) { barPx = it }                       // con la ventana de un área abierta la barra se esconde (como en la app)
                  }
                  Scene.FICHA -> Box(Modifier.fillMaxSize().background(Color(0xFF050823)).padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
                    val g = GameRegistry.getById("radar")!!
                    val now = System.currentTimeMillis()
                    val glance = listOf(260f, 240f, 250f, 210f, 190f, 170f).let { v -> v.mapIndexed { i, x -> MeasurePoint(now - (v.size - i) * day, "glance", x, 0.5f, true) } }
                    val data = cardData(g, g.title, now - day, now, 0.55f, glance, 6)
                    GameSheetContent(data, AgeBand.ADULT, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = true, onChooseTimed = {})
                  }
                  Scene.AVANCE -> Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) {
                    CompositionLocalProvider(LocalBarInset provides barInset) { Box(top) { ProgressScreen(viewModel = vm!!) } }
                    NavBar(AppTab.PROGRESO) { barPx = it }
                  }
                  Scene.AREA_DETALLE -> Box(top) {
                    AreaDetail(
                      areaKey = AREA_ORDER[0], statuses = statuses, names = DomainType.values().associate { it.name to it.displayName }, history = vm!!.gameHistory.value, measures = vm.starMeasures.value,
                      now = System.currentTimeMillis(), lang = AppLanguage.SPANISH, onArea = {}, onPlay = {}, onGame = {}, onClose = {}
                    )
                  }
                  Scene.PERFIL -> Box(top) { ProfileScreen(viewModel = vm!!, onClose = {}) }
                  Scene.AJUSTES -> Box(top) { SettingsPanel(viewModel = vm!!, onClose = {}) }
                  Scene.LICENCIAS -> Box(top) { LicensesScreen(onClose = {}) }
                  Scene.BARRA -> Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) { NavBar(AppTab.HOY) }
                  Scene.VUELO_1 -> FirstFlightScreen(FlightState(FlightMode.FULL, FlightStep.HOLA), emptyList(), FlightActions.None)
                  Scene.VUELO_JUEGO -> FirstFlightScreen(FlightState(FlightMode.FULL, FlightStep.JUEGO_1, name = "Ana"), emptyList(), FlightActions.None)
                  Scene.VUELO_EDAD -> FirstFlightScreen(
                    FlightState(FlightMode.FULL, FlightStep.EDAD, name = "Ana", measured = mapOf("secuencia" to 0.62f), phrases = mapOf("secuencia" to "Repetiste bien un rastro de 5 luces.")), emptyList(), FlightActions.None
                  )
                  Scene.VUELO_2 -> FirstFlightScreen(
                    FlightState(FlightMode.FULL, FlightStep.METAS, name = "Ana", age = AgeBand.ADULT, goals = setOf(DomainType.MEMORIA, DomainType.RAZONAMIENTO)), emptyList(), FlightActions.None
                  )
                  Scene.VUELO_PUNTO -> FirstFlightScreen(
                    FlightState(
                      FlightMode.FULL, FlightStep.PUNTO, name = "Ana", age = AgeBand.ADULT, goals = setOf(DomainType.RAZONAMIENTO),
                      measured = mapOf("secuencia" to 0.62f, "bodega" to 0.48f, "calculo" to 0.55f, "intrusa" to 0.40f)
                    ), emptyList(), FlightActions.None
                  )
                  Scene.VUELO_3 -> FirstFlightScreen(FlightState(FlightMode.FULL, FlightStep.CAMINO), listOf("bodega", "calculo", "intrusa"), FlightActions.None)
                  Scene.RESULTADO -> GameResultScreen(
                    result = rastro, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
                    starMeasures = emptyList(), onPlayAgain = {}, onContinue = {}, nextGameId = "intrusa", playsBefore = 0
                  )
                  Scene.RESULTADO_CON -> GameResultScreen(
                    result = constelaciones(record = false), didLevelUp = false, isDailyFlow = false, dailyCompletedCount = 0, dailyTotalCount = 3,
                    starMeasures = emptyList(), onPlayAgain = {}, onContinue = {}
                  )
                  Scene.RESULTADO_RECORD -> GameResultScreen(
                    result = constelaciones(record = true), didLevelUp = false, isDailyFlow = false, dailyCompletedCount = 0, dailyTotalCount = 3,
                    starMeasures = emptyList(), onPlayAgain = {}, onContinue = {}, soundEnabled = false
                  )
                  Scene.MEDALLA_PIEZA, Scene.MEDALLA_SIN -> CompositionLocalProvider(LocalReduceMotion provides true) {
                    val def = Achievements.byId(if (scene == Scene.MEDALLA_PIEZA) "racha_7" else "primer_paso")!!
                    AchievementOverlay(def = def, unlockedCount = 6, totalCount = Achievements.all.size, streak = 7, onDismiss = {}, delayMs = 0L, soundEnabled = false, pieceSound = {})
                  }
                }
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

    fun go(s: Scene, height: Int, scale: Float = 1f) {
      composeTestRule.runOnIdle { scene = s; frameH = height; fontScale = scale }
      settle()
    }

    /** Captura el marco con la letra del sistema pedida ([scale] 1,0 por defecto; 1,3 para las `-letra13`). */
    fun shot(name: String, height: Int, scale: Float = fontScale) {
      composeTestRule.runOnIdle { frameH = height; fontScale = scale }
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

  // ---------------------------------------------------------------- Hoy

  @Test
  fun hoy() {
    val r = Rig(viewModel(), Scene.HOY)
    r.settle(); r.shot("hoy", 873)
    r.shot("hoy-letra13", 873, 1.3f)
    r.go(Scene.HOY, 873)
    // la lámina de un área: se abre al tocar un edificio de la isla (con la lámina abierta la barra de pestañas se retira)
    composeTestRule.runOnIdle { r.sheetOpen = true }
    composeTestRule.onNodeWithTag("isla_observatorio").performClick()
    r.settle()
    r.shot("hoy-lamina", 873)
    r.shot("hoy-lamina-letra13", 873, 1.3f)
  }

  /** La isla sin piezas y con las diez («Tu isla crece», Tarea 136): se fijan con el mismo gancho que los botones «[Debug]» de Ajustes. */
  @Test
  fun hoy_isla_con_0_y_con_10_piezas() {
    val r = Rig(viewModel(), Scene.HOY)
    r.settle()
    composeTestRule.runOnIdle { r.vm!!.debugSetIslaPieces(emptyList()) }
    r.settle(); r.shot("hoy-isla-0", 873)
    composeTestRule.runOnIdle { r.vm!!.debugSetIslaPieces(IslaPieza.entries) }
    r.settle(); r.shot("hoy-isla-10", 873)
    r.shot("hoy-isla-10-letra13", 873, 1.3f)
  }

  // ---------------------------------------------------------------- Juegos

  @Test
  fun juegos() {
    val r = Rig(viewModel(), Scene.JUEGOS)
    r.settle(); r.shot("juegos", 873)
    r.shot("juegos-letra13", 873, 1.3f)
    r.go(Scene.JUEGOS, 873)
    // las cuatro áreas y, al tocar una, sus juegos
    for (area in listOf("memoria", "atencion", "razonamiento", "lenguaje")) {
      composeTestRule.runOnIdle { r.sheetOpen = true }
      composeTestRule.onNodeWithTag("area_$area").performClick()
      r.settle()
      r.shot("juegos-$area", 873)
      if (area == "memoria") r.shot("juegos-memoria-letra13", 873, 1.3f)
      composeTestRule.runOnIdle { r.vm!!.closeLibraryArea(); r.sheetOpen = false }
      r.go(Scene.JUEGOS, 873)
    }
  }

  /** La ficha de un juego (Tarea 140): plegada por defecto, desplegada con «Listo» y las dos con la letra del sistema al 130 %. */
  @Test
  fun ficha_plegada_y_desplegada() {
    val r = Rig(null, Scene.FICHA)
    r.settle(); r.shot("juegos-ficha", 873)
    composeTestRule.onNodeWithTag("sheet_change").performClick()
    r.settle(); r.shot("juegos-ficha-desplegada", 873)
    r.go(Scene.BARRA, 873)                                            // sale de la ficha: al volver nace plegada otra vez
    r.go(Scene.FICHA, 873, 1.3f); r.shot("juegos-ficha-letra13", 873)
    composeTestRule.onNodeWithTag("sheet_change").performClick()
    r.settle(); r.shot("juegos-ficha-desplegada-letra13", 873)
  }

  /** Juegos recién instalada la app (sin ninguna partida): la toma que faltaba (Tarea 147, P8): con todo en «Inicio». */
  @Test
  fun juegos_sin_partidas() {
    val r = Rig(NeuroVidaViewModel(app).also { viewModels.add(it) }, Scene.JUEGOS)
    r.settle(); r.shot("juegos-vacio", 873)
    r.shot("juegos-vacio-letra13", 873, 1.3f)
  }

  // ---------------------------------------------------------------- Avance, perfil y ajustes

  @Test
  fun avance() {
    val r = Rig(viewModel(), Scene.AVANCE)
    r.settle(); r.shot("avance", 873)
    r.shot("avance-letra13", 873, 1.3f)
    r.shot("avance-completa", 3900, 1f)
  }

  @Test
  fun avance_detalle_de_un_area() {
    val r = Rig(viewModel(), Scene.AREA_DETALLE)
    r.settle(); r.shot("avance-area", 873)
    r.shot("avance-area-letra13", 873, 1.3f)
  }

  /** Avance recién instalada la app (sin ninguna partida): Nubi con UNA línea y nada más (Tarea 147, P8), no las secciones vacías de siempre. */
  @Test
  fun avance_sin_partidas() {
    val r = Rig(NeuroVidaViewModel(app).also { viewModels.add(it) }, Scene.AVANCE)
    r.settle(); r.shot("avance-vacio", 873)
    r.shot("avance-vacio-letra13", 873, 1.3f)
  }

  @Test
  fun perfil() {
    val r = Rig(viewModel(), Scene.PERFIL)
    r.settle(); r.shot("perfil", 873)
    r.shot("perfil-letra13", 873, 1.3f)
    r.shot("perfil-completa", 2100, 1f)
  }

  @Test
  fun ajustes_y_licencias() {
    val r = Rig(viewModel(), Scene.AJUSTES)
    r.settle(); r.shot("ajustes", 873)
    r.shot("ajustes-letra13", 873, 1.3f)
    r.shot("ajustes-completa", 2600, 1f)
    r.go(Scene.LICENCIAS, 873); r.shot("licencias", 873)
    r.shot("licencias-completa", 2200)
  }

  // ---------------------------------------------------------------- Primer vuelo, barra, resultado y medalla

  @Test
  fun primer_vuelo_y_barra() {
    val r = Rig(null, Scene.BARRA)
    r.settle(); r.shot("barra", 873)
    r.go(Scene.VUELO_1, 873); r.shot("primer-vuelo-1", 873)                     // Hola
    r.go(Scene.VUELO_JUEGO, 873); r.shot("primer-vuelo-juego", 873)             // antes del juego 1
    r.go(Scene.VUELO_EDAD, 873); r.shot("primer-vuelo-edad", 873)               // la edad
    r.go(Scene.VUELO_2, 873); r.shot("primer-vuelo-2", 873)                     // las metas
    r.go(Scene.VUELO_PUNTO, 873); r.settle(); r.shot("primer-vuelo-punto", 873) // el punto de partida
    r.go(Scene.VUELO_3, 873); r.shot("primer-vuelo-3", 873)                     // el camino de hoy
    r.go(Scene.VUELO_1, 873, 1.3f); r.shot("primer-vuelo-1-letra13", 873)
  }

  @Test
  fun resultado_con_y_sin_record() {
    val r = Rig(null, Scene.RESULTADO)
    r.settle(); r.shot("resultado", 873)
    r.shot("resultado-letra13", 873, 1.3f)
    r.shot("resultado-completa", 1600, 1f)
    r.go(Scene.RESULTADO_CON, 873); r.shot("resultado-sin-record", 873)
    r.shot("resultado-sin-record-completa", 1700)
    r.go(Scene.RESULTADO_RECORD, 873); r.shot("resultado-con-record", 873)
    r.shot("resultado-con-record-completa", 1700)
  }

  /** La celebración de una medalla: con pieza de la isla («Y en tu isla aparece: …», Tarea 136) y sin pieza. */
  @Test
  fun medalla_con_y_sin_pieza() {
    val r = Rig(null, Scene.MEDALLA_PIEZA)
    r.settle(); r.shot("medalla-con-pieza", 873)
    r.go(Scene.MEDALLA_SIN, 873); r.shot("medalla-sin-pieza", 873)
  }
}
