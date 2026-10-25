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
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.Achievements
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
import com.example.ui.screens.FirstFlightScreen
import com.example.ui.screens.FlightActions
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.AllGamesScreen
import com.example.ui.screens.HomeScreen
import com.example.ui.screens.LicensesScreen
import com.example.ui.screens.TuScreen
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

  /** Lo que mide la barra de pestañas a 393 dp de ancho (los discos con su glifo y el rótulo, ~88 dp; Tarea 156, antes 115): la franja que cada pestaña deja libre (`LocalBarInset`). */
  private val BAR_DP = 88

  private enum class Scene {
    HOY, TODOS, FICHA, AVANCE, TU, AJUSTES, LICENCIAS, BARRA, BARRA_AVANCE, BARRA_TU, VUELO_1, VUELO_JUEGO, VUELO_EDAD, VUELO_2, VUELO_PUNTO, VUELO_3, RESULTADO, RESULTADO_RECORD, RESULTADO_CON, MEDALLA_PIEZA, MEDALLA_SIN, CARGA
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

  /** El marco de 393 dp con la escena puesta: [go] cambia de escena y alto, [shot] captura el marco (con la letra del sistema que se pida: 1,0 o 1,3). */
  private inner class Rig(val vm: NeuroVidaViewModel?, first: Scene) {
    var scene by mutableStateOf(first)
    var frameH by mutableStateOf(873)
    var sheetOpen by mutableStateOf(false)
    var fontScale by mutableStateOf(1f)
    var barPx by mutableIntStateOf(0)                    // lo que mide la barra de pestañas (`MainActivity` lo mide igual): con la letra al 130 % es más alta que los 88 dp de la letra normal

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
                    CompositionLocalProvider(LocalBarInset provides barInset) { Box(top) { HomeScreen(viewModel = vm!!) } }
                    if (!sheetOpen) NavBar(AppTab.HOY) { barPx = it }
                  }
                  Scene.TODOS -> Box(top) { AllGamesScreen(viewModel = vm!!) }                          // «Todos los juegos»: ocupa toda la pantalla y esconde la barra (Tarea 158)
                  Scene.FICHA -> Box(Modifier.fillMaxSize().background(Color(0xFF050823)).padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
                    val g = GameRegistry.getById("radar")!!
                    val now = System.currentTimeMillis()
                    val glance = listOf(260f, 240f, 250f, 210f, 190f, 170f).let { v -> v.mapIndexed { i, x -> MeasurePoint(now - (v.size - i) * day, "glance", x, 0.5f, true) } }
                    val data = cardData(g, g.title, now - day, now, 0.55f, glance, 6)
                    GameSheetContent(data, AgeBand.ADULT, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = true, onChooseTimed = {})
                  }
                  Scene.AVANCE -> Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) {
                    CompositionLocalProvider(LocalBarInset provides barInset) { Box(top) { ProgressScreen(viewModel = vm!!) } }
                    if (!sheetOpen) NavBar(AppTab.PROGRESO) { barPx = it }                    // con la lámina de un área abierta la barra se esconde (como en la app)
                  }
                  Scene.TU -> Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) {                      // «Tú»: la tercera pestaña (Tarea 158; antes el Perfil era un panel)
                    CompositionLocalProvider(LocalBarInset provides barInset) { Box(top) { TuScreen(viewModel = vm!!) } }
                    NavBar(AppTab.TU) { barPx = it }
                  }
                  Scene.AJUSTES -> Box(top) { SettingsPanel(viewModel = vm!!, onClose = {}) }
                  Scene.LICENCIAS -> Box(top) { LicensesScreen(onClose = {}) }
                  Scene.BARRA -> Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) { NavBar(AppTab.HOY) }
                  Scene.BARRA_AVANCE -> Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) { NavBar(AppTab.PROGRESO) }
                  Scene.BARRA_TU -> Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) { NavBar(AppTab.TU) }
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
                  Scene.CARGA -> CompositionLocalProvider(LocalReduceMotion provides true) {
                    com.example.ui.components.GameLoadingScreen(GameRegistry.getById("secuencia")!!, level = 3, timed = true, withSky = false)       // «Etapa Hábil · Con reloj» (Tarea 150, B1.2: antes «Reto»)
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
    r.shot("hoy-letra20", 873, 2f)                                                  // Tarea 165: con letra 2,0 Hoy se desliza (la tarjeta no se corta)
  }

  /** La isla de Tú sin piezas y con las diez («Tu isla crece», Tarea 136; desde la Tarea 165 la isla y su revelación viven en Tú): se fijan con el mismo gancho que los botones «[Debug]» de Ajustes. */
  @Test
  fun tu_isla_con_0_y_con_10_piezas() {
    val r = Rig(viewModel(), Scene.TU)
    r.settle()
    composeTestRule.runOnIdle { r.vm!!.debugSetIslaPieces(emptyList()) }
    r.settle(); r.shot("tu-isla-0", 873)
    composeTestRule.runOnIdle { r.vm!!.debugSetIslaPieces(IslaPieza.entries) }
    r.settle(); r.shot("tu-isla-10", 873)
    r.shot("tu-isla-10-letra13", 873, 1.3f)
  }

  // ---------------------------------------------------------------- Todos los juegos

  /** La ventana con los 17 juegos, su buscador y los chips de área (Tarea 158): sin filtro, con «Memoria» y con letra 1,3. */
  @Test
  fun todos_los_juegos() {
    val r = Rig(viewModel(), Scene.TODOS)
    composeTestRule.runOnIdle { r.vm!!.openAllGames() }
    r.settle(); r.shot("todos-los-juegos", 873)
    r.shot("todos-los-juegos-letra13", 873, 1.3f)
    composeTestRule.runOnIdle { r.vm!!.setAllGamesArea("MEMORIA") }
    r.settle(); r.shot("todos-los-juegos-memoria", 873, 1f)
  }

  /** La ficha de un juego (Tarea 140): plegada por defecto, desplegada con «Listo» y las dos con la letra del sistema al 130 %. */
  @Test
  fun ficha_plegada_y_desplegada() {
    val r = Rig(null, Scene.FICHA)
    r.settle(); r.shot("ficha", 873)
    composeTestRule.onNodeWithTag("sheet_change").performClick()
    r.settle(); r.shot("ficha-desplegada", 873)
    r.go(Scene.BARRA, 873)                                            // sale de la ficha: al volver nace plegada otra vez
    r.go(Scene.FICHA, 873, 1.3f); r.shot("ficha-letra13", 873)
    composeTestRule.onNodeWithTag("sheet_change").performClick()
    r.settle(); r.shot("ficha-desplegada-letra13", 873)
  }

  /** Juegos recién instalada la app (sin ninguna partida): la toma que faltaba (Tarea 147, P8): con todo en «Inicio». */
  /** «Tú» recién instalada la app (sin partidas): la isla sin piezas, el nombre y las medallas por ganar. */
  @Test
  fun tu_sin_partidas() {
    val r = Rig(NeuroVidaViewModel(app).also { viewModels.add(it) }, Scene.TU)
    r.settle(); r.shot("tu-vacio", 873)
    r.shot("tu-vacio-letra13", 873, 1.3f)
  }

  // ---------------------------------------------------------------- Avance, Tú y ajustes

  @Test
  fun avance() {
    val r = Rig(viewModel(), Scene.AVANCE)
    r.settle(); r.shot("avance", 873)
    r.shot("avance-letra13", 873, 1.3f)
    r.shot("avance-completa", 3900, 1f)
  }

  /** Tocar un área en Avance abre su PÁGINA (Tarea 159: antes la lámina de la Tarea 155): puntos y etapa, lo que falta, las semanas, una tarjeta por juego, «Qué pones en juego» y la nota. La barra de pestañas sigue visible. */
  @Test
  fun avance_pagina_de_un_area() {
    val r = Rig(viewModel(), Scene.AVANCE)
    r.settle()
    composeTestRule.onNodeWithTag("progress_area_memoria").performClick()
    r.settle(); r.shot("avance-pagina", 873)
    r.shot("avance-pagina-letra13", 873, 1.3f)
    r.shot("avance-pagina-completa", 3000, 1f)
  }

  /** Avance recién instalada la app (sin ninguna partida): Nubi con UNA línea y nada más (Tarea 147, P8), no las secciones vacías de siempre. */
  @Test
  fun avance_sin_partidas() {
    val r = Rig(NeuroVidaViewModel(app).also { viewModels.add(it) }, Scene.AVANCE)
    r.settle(); r.shot("avance-vacio", 873)
    r.shot("avance-vacio-letra13", 873, 1.3f)
  }

  /** Tú (Tarea 158): tu isla con sus piezas, tu nombre con «Cambiar», Racha · Partidas · Medallas, las medallas y, al final, la losa «Ajustes». */
  @Test
  fun tu() {
    val r = Rig(viewModel(), Scene.TU)
    r.settle(); r.shot("tu", 873)
    r.shot("tu-letra13", 873, 1.3f)
    r.shot("tu-completa", 2100, 1f)
    // Tarea 153: «Cambiar» abre el nombre como campo con su rótulo «Tu nombre» (el teclado no se dibuja en la prueba)
    composeTestRule.onNodeWithTag("btn_change_name").performScrollTo().performClick()
    r.settle(); r.shot("tu-editando", 873, 1f)
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
    r.shot("barra-letra13", 873, 1.3f)
    r.go(Scene.BARRA_AVANCE, 873); r.shot("barra-avance", 873)                   // cada pestaña elegida (Tarea 158: la lámina mide los íconos de las tres)
    r.shot("barra-avance-letra13", 873, 1.3f)
    r.go(Scene.BARRA_TU, 873); r.shot("barra-tu", 873)
    r.shot("barra-tu-letra13", 873, 1.3f)
    composeTestRule.runOnIdle { r.fontScale = 1f }
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

  /** La pantalla de carga de un juego con reloj (Tarea 150, B1.2): «Etapa Hábil · Con reloj», no «Etapa Hábil · Reto». */
  @Test
  fun carga_de_un_juego() {
    val r = Rig(null, Scene.CARGA)
    r.settle(); r.shot("carga-juego", 873)
  }
}
