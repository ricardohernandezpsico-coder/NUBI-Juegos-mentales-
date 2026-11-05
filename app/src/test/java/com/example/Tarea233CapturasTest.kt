package com.example

import android.app.Application
import androidx.activity.ComponentActivity
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.Carga
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.GamePlayResult
import com.example.model.UserSettings
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
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

/**
 * La lámina de la Tarea 233 (`docs/previews/tarea-233.png`, `tools/previews/tarea_233.py`): Carga exacta de 5 a 8 niveles con los puntos anclados. Por la pantalla REAL de Avance (su ViewModel, la base y las preferencias, no una réplica): la página de Razonamiento de una persona que estaba
 * en el 4,6 de 5 de Carga exacta, antes (el código de la Tarea 230) y ahora (su rating pasó a 4,6 de 8: lo que dejaría la migración de Room), a 393 dp con letra 1,0 y a 360 dp con 2,0; y, solo ahora, la de alguien que ya llegó al nivel 7 (sus puntos están en el tope y su frase dice el nivel nuevo).
 * (La tabla de tres personas de ejemplo está en `Tarea233TablaCapturaTest`: usa el código nuevo y no corre sobre el de antes.) Corre igual sobre el código de antes (el cambio se detecta por reflexión: `Carga.OLD_TO_NEW_RATIO`). No corre en las pruebas de siempre: solo al grabar
 * (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea233CapturasTest*"`); los PNG van a `src/test/screenshots/t233/` (fuera de git).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea233CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()
  private val day = 24L * 60 * 60 * 1000

  /** true sobre el código de ahora (Carga exacta con 8 niveles); false sobre el de antes. */
  private val v2 = Carga::class.java.declaredFields.any { it.name == "OLD_TO_NEW_RATIO" }

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

  private var fontState by mutableStateOf(1f)

  private fun frame(widthDp: Int, heightDp: Int, scale: Float, content: @androidx.compose.runtime.Composable () -> Unit) {
    rule.mainClock.autoAdvance = true
    fontState = scale
    org.robolectric.RuntimeEnvironment.setQualifiers("w${widthDp}dp-h${heightDp}dp-xxhdpi")
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontState), LocalReduceMotion provides true, LocalBarInset provides 88.dp) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823))) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t233/$name.png") }

  /** Lo que Unity manda como `end_rating` de Carga exacta para la persona cuyo rating de la escalera de 5 era [oldRating]: el mismo nivel, sobre 8 niveles en vez de 5. */
  private fun cargaEnd(oldRating: Float) = if (v2) oldRating * 5f / 8f else oldRating

  /** Cuatro partidas de cada juego de Razonamiento a lo largo de cuatro semanas, con Carga exacta terminando siempre en [cargaRating] (la escala del código que corre: `end_rating` de Unity). */
  private fun seed(cargaRating: Float): Int = runBlocking {
    val db = NeuroVidaDatabase.getDatabase(app)
    val repo = (app as NeuroVidaApplication).repository
    val now = System.currentTimeMillis()
    db.userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity())
    val games = listOf("calculo" to cargaRating, "engranajes" to 0.5f, "acoplamiento" to 0.45f, "aterrizaje" to 0.4f)
    var n = 0
    for ((k, g) in games.withIndex()) for (round in 0 until 4) {
      repo.recordGameResult(
        GamePlayResult(
          gameId = g.first, score = 70, correctAnswers = 8, totalTrials = 10, timed = false, level = 2, timestamp = now - (28 - 7 * round) * day + k * 3_600_000L, endRating = g.second, modeTrials = 10, modeHits = 8
        ),
        countsForDailySession = false
      )
      n += 1
    }
    n
  }

  private fun page(name: String, cargaRating: Float, width: Int, height: Int, scale: Float) {
    val n = seed(cargaRating)
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(30_000, "El historial no se cargó") { vm.gameHistory.value.size >= n }
    vm.setTab(AppTab.PROGRESO)
    vm.openAvanceArea("RAZONAMIENTO")
    frame(width, height, scale) { Box(Modifier.fillMaxSize()) { ProgressScreen(viewModel = vm) } }
    save(name)
  }

  // ---------------------------------------------------------------- la misma persona, antes y ahora: 4,6 de 5 (= 4,6 de 8)

  @Test fun razonamiento_4_6_393_10() = page("razonamiento_4_6_393_10", cargaEnd(0.92f), 393, 2300, 1f)
  @Test fun razonamiento_4_6_360_20() = page("razonamiento_4_6_360_20", cargaEnd(0.92f), 360, 4300, 2f)

  // ---------------------------------------------------------------- solo ahora: quien ya llegó al nivel 7 de 8 (rating 6,6 de 8)

  @Test fun razonamiento_nivel7_393_10() { Assume.assumeTrue("solo con la escalera de 8", v2); page("razonamiento_nivel7_393_10", 6.6f / 8f, 393, 2300, 1f) }
  @Test fun razonamiento_nivel7_360_20() { Assume.assumeTrue("solo con la escalera de 8", v2); page("razonamiento_nivel7_360_20", 6.6f / 8f, 360, 4300, 2f) }
}
