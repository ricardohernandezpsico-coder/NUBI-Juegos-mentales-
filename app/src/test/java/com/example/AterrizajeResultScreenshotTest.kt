package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.hasScrollAction
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onFirst
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.test.swipeUp
import androidx.compose.ui.unit.dp
import com.example.data.MeasurePoint
import com.example.games.GameResultScreen
import com.example.model.GamePlayResult
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * El final de «Aterrizaje Lunar» renovado (Tarea 70, docs/diseno-aterrizaje.md §6), en el orden del diseño: el título, el recuadro «a X de cada 100 del lugar justo», «Tu avance» (hoy, tu promedio, tu mejor),
 * «Truco para la próxima» y, abajo y en chico, «¿Por qué importa?» con su fuente. Tres casos: la primera partida (sin con qué compararse), mejor que tu promedio y con una lectura clara (te costó el final de la regla).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class AterrizajeResultScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private val trues = listOf(0.1f, 0.2f, 0.45f, 0.55f, 0.8f, 0.9f)
  private val parejo = listOf(0.11f, 0.19f, 0.46f, 0.54f, 0.81f, 0.89f)
  private val finalLejos = listOf(0.11f, 0.19f, 0.46f, 0.54f, 0.72f, 0.98f)

  private fun result(errorPct: Float, given: List<Float>, hits: Int, bulls: Int, domes: Int, streak: Int) = GamePlayResult(
    gameId = "aterrizaje", score = 74, correctAnswers = hits, totalTrials = 15, timed = false, level = 4,
    numlineErrorPct = errorPct, numlineTrue = trues, numlineGiven = given, numlineBullseyes = bulls,
    landDomes = domes, landStreak = streak, endRating = 0.5f, timestamp = 1_000_000L
  )

  private fun earlier(vararg errors: Float) = errors.mapIndexed { i, e -> MeasurePoint(1_000L * (i + 1), "numline", e, 0.5f, false) }

  private fun show(result: GamePlayResult, shot: String, measures: List<MeasurePoint>, totals: com.example.data.Aterrizaje.Totals?) {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          GameResultScreen(
            result = result, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
            starMeasures = measures, aterrizajeTotals = totals, onPlayAgain = {}, onContinue = {}
          )
        }
      }
    }
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/$shot.png")
    composeTestRule.onAllNodes(hasScrollAction()).onFirst().performTouchInput { swipeUp(durationMillis = 100) }
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/$shot-2.png")
  }

  @Test
  fun final_de_aterrizaje_primera_partida() {
    // sin partidas anteriores: «Juega otra vez para ver tu avance.»; lectura pareja y ninguna cúpula todavía
    show(result(errorPct = 11.2f, given = parejo, hits = 9, bulls = 1, domes = 0, streak = 3), "aterrizaje-final-primera", emptyList(), null)
  }

  @Test
  fun final_de_aterrizaje_mejor_que_tu_promedio() {
    show(
      result(errorPct = 7.4f, given = parejo, hits = 13, bulls = 3, domes = 2, streak = 6), "aterrizaje-final-mejor",
      earlier(12f, 10f, 9.5f), com.example.data.Aterrizaje.Totals(domes = 7, best = 2)
    )
  }

  @Test
  fun final_de_aterrizaje_con_lectura_del_final_de_la_regla() {
    show(
      result(errorPct = 10.6f, given = finalLejos, hits = 10, bulls = 0, domes = 0, streak = 2), "aterrizaje-final-lectura",
      earlier(9f, 10f, 11f), com.example.data.Aterrizaje.Totals(domes = 1, best = 1)
    )
  }
}
