package com.example

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.local.DailySessionEntity
import com.example.data.local.GameProgressEntity
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.UserSettings
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalTabSwipeLock
import com.example.ui.screens.TuScreen
import com.example.ui.screens.HomeScreen
import com.example.ui.screens.ProgressScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.NeuroVidaViewModel
import com.github.takahirom.roborazzi.captureRoboImage
import kotlinx.coroutines.runBlocking
import org.junit.After
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

/**
 * Hoy, Juegos y Avance sobre el fondo en dos capas, con las pantallas REALES y datos de ejemplo: la lámina y los luceros, fijos detrás de las tres pestañas (Tarea 87). Deja `fondo-hoy.png`, `fondo-juegos.png` y `fondo-avance.png` en `src/test/screenshots/` (git-ignorada). Ventana de 393 × 873 dp con las barras del sistema simuladas por margen.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp", sdk = [36])
class FondoPorPestanaScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()
  private val day = 24L * 60 * 60 * 1000

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
  }

  @After
  fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun seed() = runBlocking {
    val db = NeuroVidaDatabase.getDatabase(app)
    val now = System.currentTimeMillis()
    db.userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo").toEntity())
    db.gameProgressDao().insertAll(GameRegistry.allGames.map { GameProgressEntity(gameId = it.id, currentLevel = 2) })
    for ((i, id) in listOf("secuencia", "stroop", "calculo", "anagramas", "parejas").withIndex()) for (j in 0..2) {
      db.gameResultDao().insert(GamePlayResult(gameId = id, score = 70 + i * 4, correctAnswers = 9, totalTrials = 12, timed = true, level = 2, timestamp = now - (j * 2 + i + 1) * day, endRating = 0.5f).toEntity())
    }
    val today = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())
    db.dailySessionDao().insertOrUpdate(DailySessionEntity(dateKey = today, gameIdsRaw = "bodega,calculo,intrusa", completedCount = 1, scoresRaw = ""))
  }

  @Test
  fun hoy_juegos_y_avance_sobre_la_lamina_y_los_luceros() {
    seed()
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil { vm.gameHistory.value.isNotEmpty() }
    composeTestRule.mainClock.autoAdvance = false
    val page = mutableIntStateOf(0)
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(873.dp)) {
          BoardBackground()
          CompositionLocalProvider(LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
            Box(Modifier.padding(top = 24.dp, bottom = 163.dp)) {
              when (page.intValue) {
                0 -> HomeScreen(viewModel = vm)
                1 -> ProgressScreen(viewModel = vm)
                else -> TuScreen(viewModel = vm)
              }
            }
          }
        }
      }
    }
    for ((i, name) in listOf("hoy", "avance", "tu").withIndex()) {
      composeTestRule.runOnIdle { page.intValue = i }
      composeTestRule.mainClock.advanceTimeBy(600)
      composeTestRule.waitForIdle()
      composeTestRule.mainClock.advanceTimeBy(600)
      composeTestRule.waitForIdle()
      composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/fondo-$name.png")
    }
  }
}
