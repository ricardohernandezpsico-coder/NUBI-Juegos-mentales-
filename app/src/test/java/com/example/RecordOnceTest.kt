package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.assertCountEquals
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithTag
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.dp
import com.example.data.MeasurePoint
import com.example.games.GameResultScreen
import com.example.model.GamePlayResult
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * El récord, UNA vez por pantalla y nunca en la primera partida de un juego (Tarea 127). Lo dice el sello de arriba con la cifra; ni la tarjeta de la medida ni «Tu avance» lo repiten. La primera partida de un juego no celebra récord: no hay nada que superar
 * (sin sello, sin «¡Tu mejor partida hasta ahora!»). Los datos puros están en `FinalModelsTest`; esta prueba mira lo que SE VE.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class RecordOnceTest {
  @get:Rule val composeTestRule = createComposeRule()

  private val rescate = GamePlayResult(
    gameId = "radar", score = 71, correctAnswers = 9, totalTrials = 12, timed = false, level = 6,
    glanceMs = 170, glanceLoad = 3.5f, captureK = 3.25f,
    rescRescued = 31, rescPerfect = 5, rescRounds = 12, rescBestStreak = 3, rescShortestMs = 150, rescBest = 31, rescNewRecord = true, rescTrips = 3, timestamp = 1_000_000L
  )

  private fun show(result: GamePlayResult, measures: List<MeasurePoint>) {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp)) {
          GameResultScreen(
            result = result, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
            starMeasures = measures, rescateTotals = com.example.data.Rescate.Totals(342, 12), soundEnabled = false, onPlayAgain = {}, onContinue = {}
          )
        }
      }
    }
    composeTestRule.waitForIdle()
  }

  @Test
  fun la_primera_partida_no_celebra_record() {
    // la app apaga el récord que Unity dice cuando no había marca previa (FirstRecord.gate, antes de guardar): a la pantalla llega sin récord
    show(com.example.data.FirstRecord.gate(rescate, com.example.data.FirstRecord.Priors()), emptyList())
    composeTestRule.onAllNodesWithTag("result_record").assertCountEquals(0)
    composeTestRule.onAllNodesWithText("récord", substring = true, ignoreCase = true).assertCountEquals(0)
    composeTestRule.onAllNodesWithText("Tu mejor partida hasta ahora", substring = true).assertCountEquals(0)
    composeTestRule.onAllNodesWithText("Juega otra vez para ver tu avance.").assertCountEquals(1)
  }

  @Test
  fun con_marca_previa_el_record_sale_una_sola_vez_con_su_cifra() {
    // dos partidas anteriores de Rescate (el destello más breve: menos es mejor): hoy 170 ms supera a las dos
    val before = listOf(MeasurePoint(100L, "glance", 300f, 0.5f, false), MeasurePoint(200L, "glance", 280f, 0.5f, false))
    show(com.example.data.FirstRecord.gate(rescate, com.example.data.FirstRecord.Priors(rescate = 20)), before)
    composeTestRule.onNodeWithTag("result_record").assertIsDisplayed()
    composeTestRule.onAllNodesWithText("¡Récord nuevo: 31 cápsulas!").assertCountEquals(1)
    composeTestRule.onAllNodesWithText("récord", substring = true, ignoreCase = true).assertCountEquals(1)
    composeTestRule.onAllNodesWithText("Tu mejor partida hasta ahora", substring = true).assertCountEquals(0)
  }
}
