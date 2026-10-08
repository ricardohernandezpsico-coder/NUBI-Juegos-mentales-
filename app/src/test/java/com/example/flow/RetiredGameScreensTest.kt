package com.example.flow

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.data.SkillState
import com.example.data.computeAchievementStats
import com.example.data.local.DailySessionEntity
import com.example.data.local.GameProgressEntity
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toDomain
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.UserSettings
import com.example.ui.screens.GamesLibraryScreen
import com.example.ui.screens.HomeScreen
import com.example.ui.screens.ProgressScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.NeuroVidaViewModel
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
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

/** Hoy, Juegos y Avance con datos viejos de un juego retirado (`cambiochip`): se abren sin errores y no lo nombran. Ver [RetiredGameTest] para el resto. */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w412dp-h915dp", sdk = [36])
class RetiredGameScreensTest {
  @get:Rule val composeTestRule = createComposeRule()

  private lateinit var app: Application
  private val old = "cambiochip"
  private val day = 24L * 60 * 60 * 1000

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
  }

  @After
  fun tearDown() {
    TestSupport.resetDatabase()
  }

  private fun db() = NeuroVidaDatabase.getDatabase(app)

  private fun seedOldData() = runBlocking {
    val now = System.currentTimeMillis()
    db().userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ana").toEntity())
    db().gameProgressDao().insertAll(GameRegistry.allGames.map { GameProgressEntity(gameId = it.id, currentLevel = 1) })
    db().gameProgressDao().insertOrUpdate(GameProgressEntity(gameId = old, currentLevel = 4, eloRating = 900, ddaRating = 0.7f, totalGamesPlayed = 3))
    for (i in 0..2) {
      db().gameResultDao().insert(
        GamePlayResult(gameId = old, score = 80, correctAnswers = 10, totalTrials = 12, timed = true, level = 4, timestamp = now - (i + 1) * day, endRating = 0.7f).toEntity()
      )
    }
    val today = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())
    db().dailySessionDao().insertOrUpdate(DailySessionEntity(dateKey = today, gameIdsRaw = "$old,calculo,series", completedCount = 0, scoresRaw = ""))
    app.getSharedPreferences("skill", android.content.Context.MODE_PRIVATE).edit()
      .putString("state", SkillState(measured = setOf(old, "calculo")).encode()).commit()
  }

  @Test
  fun `Hoy, Juegos y Avance se abren sin errores con datos viejos y no nombran el juego`() {
    seedOldData()
    val vm = NeuroVidaViewModel(app)
    TestSupport.awaitUntil { vm.gameHistory.value.isNotEmpty() && vm.dailySession.value.gameIds.none { it == old } }
    composeTestRule.mainClock.autoAdvance = false
    val screen = androidx.compose.runtime.mutableIntStateOf(0)
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp)) {
          androidx.compose.runtime.CompositionLocalProvider(com.example.ui.components.LocalTabSwipeLock provides androidx.compose.runtime.remember { androidx.compose.runtime.mutableIntStateOf(0) }) {
            when (screen.intValue) {
              0 -> HomeScreen(viewModel = vm, onNavigateToGames = {})
              1 -> GamesLibraryScreen(viewModel = vm)
              else -> ProgressScreen(viewModel = vm)
            }
          }
        }
      }
    }
    for (i in 0..2) {
      screen.intValue = i
      composeTestRule.mainClock.advanceTimeBy(500)
      composeTestRule.waitForIdle()
      assertEquals("«Cambio de Chip» no debe verse en la pantalla $i", 0, composeTestRule.onAllNodesWithText("Cambio de Chip", substring = true).fetchSemanticsNodes().size)
    }
  }
}
