package com.example

import android.app.Application
import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onAllNodesWithContentDescription
import androidx.compose.ui.test.onLast
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
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
import com.example.ui.screens.HomeScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.NeuroVidaViewModel
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
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

/**
 * Tarea 87 (B), con el `HomeScreen` y el ViewModel REALES: tocar el nombre de un juego en la lámina de un lugar abre SU FICHA encima de la lámina (no lo juega); cerrar la ficha deja la lámina abierta donde estaba.
 * Solo los botones grandes juegan directo. Qué hace «Jugar» dentro de la ficha (`playFromHome`) ya lo cubre el recorrido de la piedrita de la ruta, que usa la misma ficha.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp", sdk = [36])
class HoyFichaDesdeLaminaTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()
  private val day = 24L * 60 * 60 * 1000

  @Before
  fun setUp() { app = ApplicationProvider.getApplicationContext(); TestSupport.resetDatabase() }

  @After
  fun tearDown() { TestSupport.release(*viewModels.toTypedArray()); viewModels.clear(); TestSupport.resetDatabase() }

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
  fun tocar_una_fila_de_la_lamina_abre_la_ficha_encima_y_Atras_vuelve_a_la_lamina() {
    seed()
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil { vm.gameHistory.value.isNotEmpty() }
    rule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(873.dp)) {
          BoardBackground()
          CompositionLocalProvider(LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
            Box(Modifier.padding(top = 24.dp, bottom = 48.dp)) { HomeScreen(viewModel = vm, onNavigateToGames = {}) }
          }
        }
      }
    }
    rule.waitForIdle()
    rule.onNodeWithTag("isla_observatorio").performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("place_sheet").assertIsDisplayed()
    assertEquals("la ficha todavía no está", 0, rule.onAllNodes(hasTestTag("sheet_play")).fetchSemanticsNodes().size)

    // la fila de Constelaciones (id `parejas`, área Memoria) en la lámina del Observatorio
    val row = "place_game_parejas"
    rule.onNodeWithTag(row).performClick()
    rule.waitForIdle()
    assertTrue("la ficha del juego se abre encima", rule.onAllNodes(hasTestTag("sheet_play")).fetchSemanticsNodes().isNotEmpty())
    assertEquals("la partida NO se lanzó: el ViewModel sigue sin juego activo", null, vm.activeGame.value)

    rule.onAllNodesWithContentDescription("Cerrar").onLast().performClick()           // la X de la ficha (la lámina también tiene la suya: la de la ficha es la última, del diálogo)
    rule.waitForIdle()
    assertEquals("cerrar la ficha", 0, rule.onAllNodes(hasTestTag("sheet_play")).fetchSemanticsNodes().size)
    rule.onNodeWithTag("place_sheet").assertIsDisplayed()
  }
}
