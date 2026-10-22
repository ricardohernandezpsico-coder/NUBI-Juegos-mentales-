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
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.data.Week
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.UserSettings
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalTabSwipeLock
import com.example.ui.components.NeuroNavBar
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.HomeScreen
import com.example.ui.screens.ProgressScreen
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

/**
 * Las capturas REALES de la lámina de la Tarea 159 («Avance con PUNTOS por área», `docs/previews/tarea-159.png`): la pantalla de Avance y la página de un área (Memoria y Atención, la de 200 por juego) con letra 1,0 y 1,3; los estados vacíos (un área sin partidas, un área con menos de 4 semanas); una semana con bajada;
 * la lámina de Hoy con la línea de puntos; y la ficha de un juego con sus puntos. Datos de ejemplo sembrados con el repositorio real (`recordGameResult`), a 393 dp de ancho. No corre en las pruebas de siempre: solo al grabar capturas
 * (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea159CapturasTest*"`), y deja los PNG en `src/test/screenshots/tarea-159/`; `tools/previews/tarea_159.py` arma la lámina.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h4200dp-440dpi", sdk = [36])
class Tarea159CapturasTest {
  @get:Rule val rule = createComposeRule()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()
  private val day = 24L * 60 * 60 * 1000
  private val barDp = 88

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

  private val memoria = listOf("secuencia", "parejas", "bodega", "correo")
  private val atencion = listOf("radar", "stroop", "freno")

  /**
   * Siembra [weeks] semanas de uso con el repositorio real: cada juego de [games] se juega UNA vez por semana (en martes; esta semana, hoy), con un avance que sube de una semana a otra. [onlyDropIn] = esta semana solo ese juego juega y con una mala partida (el avance baja
   * lo que permite `MAX_DROP`); [thisWeek] = si se juega esta semana.
   */
  private fun seed(weeks: Int, games: List<String>, thisWeek: Boolean = true, onlyDropIn: String? = null): Int = runBlocking {
    val db = NeuroVidaDatabase.getDatabase(app)
    val repo = (app as NeuroVidaApplication).repository
    val now = System.currentTimeMillis()
    db.userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity())
    var n = 0
    for (w in (weeks - 1) downTo 0) for ((k, id) in games.withIndex()) {
      if (w == 0 && !thisWeek) continue
      if (w == 0 && onlyDropIn != null && id != onlyDropIn) continue
      val t = if (w == 0) (now - 3_600_000L * (k + 1)).coerceAtLeast(Week.weekStart(now, 0) + 60_000L) else Week.weekStart(now, w) + day + k * 3_600_000L
      val rating = if (w == 0 && onlyDropIn != null) 0.05f else (0.18f + 0.055f * (weeks - 1 - w) + 0.012f * k).coerceAtMost(0.95f)
      repo.recordGameResult(
        GamePlayResult(
          gameId = id, score = 72, correctAnswers = 9, totalTrials = 12, timed = false, level = 3, timestamp = t, endRating = rating, modeTrials = 10, modeHits = 8, playMode = PlayMode.A_TU_MEDIDA,
          rasBestLen = if (id == "secuencia") listOf(3 + (weeks - 1 - w) / 3, 3, 2) else null, glanceMs = if (id == "radar") 240 - 12 * (weeks - 1 - w) else null
        ),
        countsForDailySession = false
      )
      n += 1
    }
    n
  }

  private fun viewModel(n: Int): NeuroVidaViewModel {
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    if (n > 0) TestSupport.awaitUntil(60_000) { vm.gameHistory.value.size >= n }
    vm.markIslandMapIntroSeen()
    return vm
  }

  private inner class Frame(val vm: NeuroVidaViewModel, first: AppTab) {
    var tab by mutableStateOf(first)
    var height by mutableIntStateOf(873)
    var fontScale by mutableStateOf(1f)
    var barPx by mutableIntStateOf(0)
    var barVisible by mutableStateOf(true)                // con la lámina de un lugar abierta, la barra se retira (como en la app)

    init {
      rule.mainClock.autoAdvance = false
      rule.setContent {
        val base = LocalDensity.current
        CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale)) {
          NeuroVidaTheme {
            Box(Modifier.width(393.dp).height(height.dp).testTag("frame")) {
              BoardBackground()
              CompositionLocalProvider(LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
                val inset = with(LocalDensity.current) { if (barPx > 0) barPx.toDp() else barDp.dp }
                Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) {
                  CompositionLocalProvider(LocalBarInset provides inset) {
                    Box(Modifier.padding(top = 24.dp)) { if (tab == AppTab.HOY) HomeScreen(viewModel = vm) else ProgressScreen(viewModel = vm) }
                  }
                  if (barVisible) NeuroNavBar(current = tab, onSelect = {}, modifier = Modifier.align(Alignment.BottomCenter).onSizeChanged { if (it.height > 0) barPx = it.height })
                }
              }
            }
          }
        }
      }
    }

    fun settle() { repeat(3) { rule.mainClock.advanceTimeBy(600); rule.waitForIdle() } }

    /** Captura la pantalla ENTERA, del alto justo: se mide dónde termina el nodo [bottomTag] (el último de la lista) con un marco alto, y el marco se ajusta a eso más el pie de la lista, la barra de pestañas y los 48 dp de la barra del sistema. */
    fun shotFit(name: String, scale: Float, bottomTag: String) {
      rule.runOnIdle { height = 4100; fontScale = scale }
      settle()
      val d = rule.density.density
      val bottomDp = rule.onNodeWithTag(bottomTag).fetchSemanticsNode().boundsInRoot.bottom / d
      val bar = if (barPx > 0) barPx / d else barDp.toFloat()
      shot(name, kotlin.math.ceil(bottomDp + 28f + bar + 48f + 8f).toInt(), scale)
    }

    fun shot(name: String, h: Int, scale: Float = 1f) {
      rule.runOnIdle { height = h; fontScale = scale }
      settle()
      rule.onNodeWithTag("frame").captureRoboImage(filePath = "src/test/screenshots/tarea-159/$name.png")
    }
  }

  /** Avance entera y la página de un área, con letra 1,0 y 1,3: un teléfono (873 dp) y la pantalla entera (del alto justo). */
  private fun avanceYPagina(f: Frame, area: String) {
    f.settle()
    f.shot("avance", 873)
    f.shot("avance-letra13", 873, 1.3f)
    f.shotFit("avance-completa", 1f, "btn_profile_baseline")
    f.shotFit("avance-completa-letra13", 1.3f, "btn_profile_baseline")
    rule.runOnIdle { f.fontScale = 1f; f.height = 873 }
    f.settle()
    rule.onNodeWithTag("progress_area_$area").performClick()
    f.settle()
    f.shot("pagina-$area", 873)
    f.shot("pagina-$area-letra13", 873, 1.3f)
    f.shotFit("pagina-$area-completa", 1f, "page_note")
    f.shotFit("pagina-$area-completa-letra13", 1.3f, "page_note")
  }

  @Test
  fun avance_y_paginas_de_memoria_y_atencion() {
    val f = Frame(viewModel(seed(9, memoria + atencion + listOf("calculo", "engranajes", "aterrizaje"))), AppTab.PROGRESO)
    avanceYPagina(f, "memoria")
    // la página de Atención (200 puntos por juego)
    rule.runOnIdle { f.fontScale = 1f; f.height = 873; f.vm.closeAvanceArea() }
    f.settle()
    rule.onNodeWithTag("progress_area_atencion").performClick()
    f.settle()
    f.shotFit("pagina-atencion-completa", 1f, "page_note")
    f.shotFit("pagina-atencion-completa-letra13", 1.3f, "page_note")
  }

  @Test
  fun un_area_sin_partidas_y_su_pagina() {
    val f = Frame(viewModel(seed(9, memoria + atencion)), AppTab.PROGRESO)                // Lenguaje y Razonamiento sin jugar
    f.settle()
    rule.onNodeWithTag("progress_area_lenguaje").performClick()
    f.settle()
    f.shotFit("pagina-lenguaje-vacia", 1f, "page_note")
    f.shotFit("pagina-lenguaje-vacia-letra13", 1.3f, "page_note")
  }

  @Test
  fun un_area_con_menos_de_cuatro_semanas() {
    val f = Frame(viewModel(seed(2, memoria)), AppTab.PROGRESO)
    f.settle()
    rule.onNodeWithTag("progress_area_memoria").performClick()
    f.settle()
    f.shotFit("pagina-memoria-empieza", 1f, "page_note")
    f.shotFit("pagina-memoria-empieza-letra13", 1.3f, "page_note")
  }

  @Test
  fun una_semana_con_bajada() {
    val f = Frame(viewModel(seed(9, memoria + atencion, onlyDropIn = "secuencia")), AppTab.PROGRESO)
    f.settle()
    f.shot("avance-bajada", 873)
    rule.onNodeWithTag("progress_area_memoria").performClick()
    f.settle()
    f.shotFit("pagina-memoria-bajada-completa", 1f, "page_note")
  }

  @Test
  fun avance_sin_partidas() {
    val f = Frame(viewModel(0), AppTab.PROGRESO)
    f.settle()
    f.shot("avance-vacio", 873)
  }

  @Test
  fun la_lamina_de_hoy_con_la_linea_de_puntos() {
    val f = Frame(viewModel(seed(9, memoria + atencion + listOf("calculo", "engranajes", "aterrizaje"))), AppTab.HOY)
    f.settle()
    rule.runOnIdle { f.barVisible = false }
    rule.onNodeWithTag("isla_observatorio").performClick()
    f.settle()
    f.shot("hoy-lamina", 873)
    f.shot("hoy-lamina-letra13", 873, 1.3f)
  }

  /** La ficha de un juego con sus puntos («Etapa Hábil · 110 de 200 puntos», «Te faltan 10 puntos para Experto»). */
  @Test
  fun la_ficha_de_un_juego_con_sus_puntos() {
    rule.mainClock.autoAdvance = false
    var scale by mutableStateOf(1f)
    val now = System.currentTimeMillis()
    val g = GameRegistry.getById("radar")!!
    val glance = listOf(260f, 240f, 250f, 210f, 190f, 170f).let { v -> v.mapIndexed { i, x -> MeasurePoint(now - (v.size - i) * day, "glance", x, 0.5f, true) } }
    val data = cardData(g, g.title, now - day, now, 0.55f, glance, 6)
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale)) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(873.dp).testTag("frame").background(Color(0xFF050823)).padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
            GameSheetContent(data, AgeBand.ADULT, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = true, onChooseTimed = {})
          }
        }
      }
    }
    repeat(3) { rule.mainClock.advanceTimeBy(600); rule.waitForIdle() }
    rule.onNodeWithTag("frame").captureRoboImage(filePath = "src/test/screenshots/tarea-159/ficha.png")
    rule.runOnIdle { scale = 1.3f }
    repeat(3) { rule.mainClock.advanceTimeBy(600); rule.waitForIdle() }
    rule.onNodeWithTag("frame").captureRoboImage(filePath = "src/test/screenshots/tarea-159/ficha-letra13.png")
  }
}
