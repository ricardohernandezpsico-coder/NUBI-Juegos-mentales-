package com.example

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.UserSettings
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.LocalTabSwipeLock
import com.example.ui.components.NeuroNavBar
import com.example.ui.screens.FirstFlightScreen
import com.example.ui.screens.FlightActions
import com.example.ui.screens.FuentesPanel
import com.example.ui.screens.SettingsPanel
import com.example.ui.screens.TuScreen
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
import java.io.File
import kotlin.math.sin

/**
 * Las capturas de la lámina de la Tarea 185 («La pestaña Tú»): Tú con las dos variantes de cuadros (V1 «Centrado» y V2 «Ícono a la izquierda», letra 1,0 y 1,3, arriba y hasta Contacto), Ajustes en cuatro losas (recordatorio encendido y apagado), «En qué se basa Nubi»
 * (al principio y al final, letra 1,0 y 1,3), el cierre del Primer vuelo con Nubi y las medidas de cabida. Solo corre al grabar capturas; deja los PNG en `src/test/screenshots/tarea-185/propuesta/` y las medidas en `medidas.txt`; los de «hoy» se grabaron con el código de antes
 * de la tarea (`git stash` no hizo falta: se grabaron antes de tocar nada) en `tarea-185/hoy/`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h4200dp-440dpi", sdk = [36])
class Tarea185CapturasTest {
  @get:Rule val rule = createComposeRule()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()
  private val day = 24L * 60 * 60 * 1000
  private val out = "src/test/screenshots/tarea-185/propuesta"
  private val measures = mutableListOf<String>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
  }

  @After fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray()); viewModels.clear(); TestSupport.resetDatabase()
    if (measures.isNotEmpty()) File("$out/medidas.txt").apply { parentFile.mkdirs(); appendText(measures.joinToString("\n") + "\n") }
  }

  /** Unos 20 días de uso de ejemplo (como `AppActualScreenshotTest`): dos o tres partidas por día en rotación por los 17 juegos, con dos días sin jugar. */
  private fun seed() = runBlocking {
    val db = NeuroVidaDatabase.getDatabase(app)
    val repo = (app as NeuroVidaApplication).repository
    val now = System.currentTimeMillis()
    db.userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity())
    val games = GameRegistry.allGames.filter { !GameRegistry.isRetired(it.id) }.map { it.id }
    var k = 0
    for (d in 20 downTo 1) {
      if (d == 9 || d == 4) continue
      val plays = if (d % 3 == 0) 3 else 2
      for (p in 0 until plays) {
        val id = games[k++ % games.size]
        val trend = (20 - d) / 20f
        val score = (58 + trend * 24 + 6 * sin(k * 1.7)).toInt().coerceIn(40, 96)
        val rating = (0.30f + trend * 0.28f + 0.04f * sin(k * 2.3f).toFloat()).coerceIn(0.1f, 0.9f)
        repo.recordGameResult(
          GamePlayResult(
            gameId = id, score = score, correctAnswers = 8 + (score / 20), totalTrials = 12, timed = p % 2 == 0, level = 2 + (20 - d) / 8, timestamp = now - d * day + (9 + p * 4) * 3_600_000L,
            endRating = rating, modeTrials = 10, modeHits = 7 + p
          ),
          countsForDailySession = false
        )
      }
    }
  }

  private fun viewModel(): NeuroVidaViewModel {
    seed()
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(30_000) { vm.gameHistory.value.size >= 40 }
    return vm
  }

  private fun settle() { repeat(3) { rule.mainClock.advanceTimeBy(600); rule.waitForIdle() } }

  private var frameH by mutableStateOf(873)
  private var fontScale by mutableStateOf(1f)
  private var scene by mutableStateOf("tu")

  /** El marco de 393 dp con la escena puesta y las barras del sistema (24 dp arriba, 48 abajo) y la de pestañas (88 dp, la fija de la app). */
  private fun setUp(vm: NeuroVidaViewModel?, first: String) {
    scene = first
    rule.mainClock.autoAdvance = false
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(frameH.dp).testTag("frame")) {
            BoardBackground()
            CompositionLocalProvider(LocalTabSwipeLock provides remember { androidx.compose.runtime.mutableIntStateOf(0) }) {
              val top = Modifier.padding(top = 24.dp)
              when (scene) {
                "tu" -> Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) {
                  CompositionLocalProvider(LocalBarInset provides 88.dp) { Box(top) { TuScreen(viewModel = vm!!) } }
                  NeuroNavBar(current = AppTab.TU, onSelect = {}, modifier = Modifier.align(Alignment.BottomCenter))
                }
                "ajustes" -> Box(top) { SettingsPanel(viewModel = vm!!, onClose = {}) }
                "fuentes" -> Box(top) { FuentesPanel(onClose = {}) }
                "cierre" -> FirstFlightScreen(FlightState(FlightMode.FULL, FlightStep.CAMINO), listOf("bodega", "calculo", "intrusa"), FlightActions.None)
              }
            }
          }
        }
      }
    }
  }

  private fun shot(name: String, height: Int = 873, scale: Float = 1f) {
    rule.runOnIdle { frameH = height; fontScale = scale }
    settle()
    rule.onNodeWithTag("frame").captureRoboImage(filePath = "$out/$name.png")
  }

  private fun bottomOf(tag: String): Float { val n = rule.onNodeWithTag(tag).fetchSemanticsNode(); return n.boundsInRoot.bottom / rule.density.density }
  private fun heightOf(tag: String): Float { val n = rule.onNodeWithTag(tag).fetchSemanticsNode(); return n.boundsInRoot.height / rule.density.density }

  @Test
  fun tu_con_los_cuadros_centrados() {
    val vm = viewModel()
    setUp(vm, "tu")
    settle()
    shot("tu-1_0", 873, 1f)
    shot("tu-1_3", 873, 1.3f)
    shot("tu-1_3-alta", 1000, 1.3f)
    shot("tu-completa", 2000, 1f)
    measures += "Tú con letra 1,0: cuadros ${"%.0f".format(heightOf("tu_cuadros"))} dp de alto; «Cómo juegas» termina a ${"%.0f".format(bottomOf("card_como_juegas"))} dp; «Contacto» termina a ${"%.0f".format(bottomOf("tu_contacto"))} dp (la barra de pestañas empieza a 785)"
    rule.runOnIdle { fontScale = 1.3f; frameH = 2800 }
    settle()
    measures += "Tú con letra 1,3: cuadros ${"%.0f".format(heightOf("tu_cuadros"))} dp; «Contacto» termina a ${"%.0f".format(bottomOf("tu_contacto"))} dp"
  }

  @Test
  fun ajustes_en_cuatro_losas_con_el_recordatorio_encendido_y_apagado() {
    val vm = viewModel()
    setUp(vm, "ajustes")
    settle()
    shot("ajustes-1_0", 873)
    shot("ajustes-1_3", 873, 1.3f)
    shot("ajustes-completa-encendido", 2400)
    measures += "Ajustes (cuatro losas) con letra 1,0: Juego termina a ${"%.0f".format(bottomOf("card_juego"))} dp, Recordatorio a ${"%.0f".format(bottomOf("card_notifications_workmanager"))} dp, Tus datos a ${"%.0f".format(bottomOf("card_tus_datos"))} dp y Acerca de Nubi a ${"%.0f".format(bottomOf("card_acerca"))} dp (antes, con nueve bloques, unos 1 900 dp)"
    vm.setReminderEnabled(false)
    TestSupport.awaitUntil { !vm.userSettings.value.notificationsEnabled }
    shot("ajustes-completa-apagado", 2400)
    rule.runOnIdle { fontScale = 1.3f; frameH = 2600 }
    settle()
    measures += "Ajustes con letra 1,3: Acerca de Nubi termina a ${"%.0f".format(bottomOf("card_acerca"))} dp"
  }

  /** Lo que mide una pantalla que se desliza: lo que se ve más lo que falta por deslizar (dp), del rango de su lista. */
  private fun contentLength(tag: String): Float {
    val n = rule.onNodeWithTag(tag).fetchSemanticsNode()
    val range = n.config[androidx.compose.ui.semantics.SemanticsProperties.VerticalScrollAxisRange]
    return (n.boundsInRoot.height + range.maxValue()) / rule.density.density
  }

  @Test
  fun en_que_se_basa_nubi_al_principio_y_al_final() {
    setUp(null, "fuentes")
    settle()
    shot("fuentes-inicio-1_0", 873)
    measures += "«En qué se basa Nubi» con letra 1,0 mide ${"%.0f".format(contentLength("fuentes_screen"))} dp de largo (Fable calculó 3 141)"
    // (el final de la lista y el medio se capturan en el emulador, deslizando de verdad)
  }

  @Test
  fun en_que_se_basa_nubi_con_letra_grande() {
    setUp(null, "fuentes")
    settle()
    shot("fuentes-inicio-1_3", 873, 1.3f)
    measures += "«En qué se basa Nubi» con letra 1,3 mide ${"%.0f".format(contentLength("fuentes_screen"))} dp de largo (Fable calculó 4 394)"
  }

  @Test
  fun el_cierre_del_primer_vuelo_con_nubi() {
    setUp(null, "cierre")
    settle()
    shot("cierre", 873)
    shot("cierre-1_3", 873, 1.3f)
  }
}
