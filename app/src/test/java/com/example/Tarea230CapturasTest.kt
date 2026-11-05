package com.example

import android.app.Application
import androidx.activity.ComponentActivity
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
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
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.AreaPoints
import com.example.data.GamePoints
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.data.ProgressPoint
import com.example.data.TimeTrialResult
import com.example.data.Week
import com.example.data.local.GameResultEntity
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.games.GameResultScreen
import com.example.model.AgeBand
import com.example.model.AgeRange
import com.example.model.AppLanguage
import com.example.model.DomainType
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.UserSettings
import com.example.ui.components.BoardBackground
import com.example.ui.components.GameLoadingScreen
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.AvanceAreaPage
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.TuScreen
import com.example.ui.screens.cardData
import com.example.ui.screens.sheetTimeTrialOf
import com.example.ui.theme.NeuroVidaTheme
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
 * La lámina de la Tarea 230 (`docs/previews/tarea-230.png`, `tools/previews/tarea_230.py`): «Etapa» sale de la app. Cinco escenas, cada una a 393 dp con letra 1,0 y a 360 dp con letra 2,0: la página de un área de Avance, la ficha de un juego, la pantalla de carga, Tú (con «N partidas»
 * que ahora suma las del Contrarreloj) y un final de Bodega con «Nivel más alto». Usa SOLO piezas cuya firma no cambió, así que corre igual sobre el código de antes (para la mitad «antes» de la lámina) y sobre el de ahora. No corre en las pruebas de siempre: solo al grabar
 * (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea230CapturasTest*"`); los PNG van a `src/test/screenshots/t230/` (fuera de git). La ficha se captura SIN su `Dialog` (un `Dialog` ignora la densidad de la prueba y la captura «a 2,0» saldría a 1,0).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea230CapturasTest {
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

  private val day = 86_400_000L
  private val now: Long = Calendar.getInstance().apply { clear(); set(2026, Calendar.NOVEMBER, 4, 12, 0) }.timeInMillis

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

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t230/$name.png") }

  // ---------------------------------------------------------------- la página de un área de Avance

  private val memoria = listOf("parejas", "secuencia", "bodega", "correo")

  private fun logMemoria(): List<ProgressPoint> {
    val l = mutableListOf<ProgressPoint>()
    for (w in 8 downTo 0) {
      val t = Week.weekStart(now, w) + day
      l += ProgressPoint("secuencia", t, 0.30f + (8 - w) * 0.03f)
      if (w <= 6) l += ProgressPoint("parejas", t + 3_600_000L, 0.20f + (6 - w) * 0.04f)
      if (w <= 3) l += ProgressPoint("bodega", t + 7_200_000L, 0.20f + (3 - w) * 0.05f)
    }
    return l
  }

  private fun measuresOf(key: String, vararg values: Float) = values.mapIndexed { i, v -> MeasurePoint(now - (values.size - i) * day, key, v, 0.5f, true) }

  private fun avance(name: String, width: Int, height: Int, scale: Float) {
    val log = logMemoria()
    val p = AreaPoints.compute("MEMORIA", memoria, log, now)
    frame(width, height, scale) {
      Column(Modifier.fillMaxSize().padding(horizontal = 22.dp)) {
        AvanceAreaPage(
          p, AreaPoints.weekly(memoria, log, now), "Memoria", mapOf("secuencia" to now, "parejas" to now - 2 * day, "bodega" to now - 3 * day), measuresOf("trail", 4f, 5f, 7f, 5f),
          AppLanguage.SPANISH, now, {}, Modifier.height((height - 40).dp)
        )
      }
    }
    save(name)
  }

  @Test fun avance_393_10() = avance("avance_393_10", 393, 2300, 1f)
  @Test fun avance_360_20() = avance("avance_360_20", 360, 4300, 2f)

  // ---------------------------------------------------------------- la ficha de un juego

  private fun ficha(name: String, width: Int, height: Int, scale: Float) {
    val g = GameRegistry.getById("parejas")!!
    val data = cardData(g, g.title, 0.72f, GamePoints("parejas", 180, 250, 0.72f, true))
    frame(width, height, scale) {
      Box(Modifier.fillMaxSize().padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.TopCenter) {
        Box(Modifier.fillMaxWidth()) {
          GameSheetContent(
            data, AgeBand.ADULT, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = true, onChooseTimed = {},
            timeTrial = sheetTimeTrialOf(g.id, data.title, null, "2026-11-04"), onTimeTrial = {}
          )
        }
      }
    }
    save(name)
  }

  @Test fun ficha_393_10() = ficha("ficha_393_10", 393, 900, 1f)
  @Test fun ficha_360_20() = ficha("ficha_360_20", 360, 2400, 2f)

  // ---------------------------------------------------------------- la pantalla de carga

  private fun carga(name: String, width: Int, height: Int, scale: Float) {
    frame(width, height, scale) { GameLoadingScreen(GameRegistry.getById("secuencia")!!, level = 3, timed = true, withSky = false) }
    save(name)
  }

  @Test fun carga_393_10() = carga("carga_393_10", 393, 873, 1f)
  @Test fun carga_360_20() = carga("carga_360_20", 360, 780, 2f)

  // ---------------------------------------------------------------- Tú: «N partidas» con las del Contrarreloj

  private fun tu(name: String, width: Int, height: Int, scale: Float) {
    runBlocking {
      val db = NeuroVidaDatabase.getDatabase(app)
      db.userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity())
      // 10 partidas de siempre y 5 del Contrarreloj, de los últimos 12 días: Tú dice «10 partidas» antes de la Tarea 230 y «15 partidas» después
      val games = listOf("radar", "parejas", "stroop", "calculo", "bodega")
      repeat(10) { i -> db.gameResultDao().insert(GameResultEntity("r$i", games[i % games.size], 80, 8, 10, false, 2, now - (i + 1) * day)) }
      repeat(5) { i -> db.timeTrialResultDao().insert(TimeTrialResult(id = "t$i", gameId = "stroop", day = "2026-10-${24 + i}", trackVersion = 2, hits = 30, errors = 2, level = 3, timestamp = now - (i * 2 + 1) * day - 1_000L).toEntity()) }
    }
    app.getSharedPreferences("profile_extra", android.content.Context.MODE_PRIVATE).edit().putString("age_range", AgeRange.R45_54.name).commit()
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(30_000, "El perfil no se cargó") { vm.userSettings.value.ageBand != null }
    TestSupport.awaitUntil(30_000, "La referencia no se cargó") { vm.estimatedReference.value != null }
    TestSupport.awaitUntil(30_000, "El historial no se cargó") { vm.gameHistory.value.size == 10 && vm.timeTrialResults.value.size == 5 }
    frame(width, height, scale) { Box(Modifier.fillMaxSize().padding(top = 24.dp)) { TuScreen(vm) } }
    save(name)
  }

  @Test fun tu_393_10() = tu("tu_393_10", 393, 1300, 1f)
  @Test fun tu_360_20() = tu("tu_360_20", 360, 2600, 2f)

  // ---------------------------------------------------------------- el final de Bodega: «Nivel más alto»

  private val bodega = GamePlayResult(
    gameId = "bodega", score = 74, correctAnswers = 9, totalTrials = 12, timed = false, level = 6, endRating = 0.5f, timestamp = 1_000_000L,
    bodGroup = 3, bodBestStreak = 4, bodBiggest = 5, bodBest = 6, bodNewRecord = false, bodMs = 4500
  )

  private fun final(name: String, width: Int, height: Int, scale: Float) {
    val measures = listOf(58f, 66f, 83f).mapIndexed { i, v -> MeasurePoint(1_000L * (i + 1), "bodega", v, 0.5f, false) }
    frame(width, height, scale) {
      GameResultScreen(
        result = bodega, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
        starMeasures = measures, onPlayAgain = {}, onContinue = {}
      )
    }
    rule.onNodeWithTag("detail_toggle").performClick()
    rule.waitForIdle()
    save(name)
  }

  @Test fun final_bodega_393_10() = final("final_bodega_393_10", 393, 2100, 1f)
  @Test fun final_bodega_360_20() = final("final_bodega_360_20", 360, 4200, 2f)
}
