package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasScrollAction
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onFirst
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.test.swipeUp
import androidx.compose.ui.unit.dp
import com.example.data.MeasurePoint
import com.example.games.GameResultScreen
import com.example.model.GamePlayResult
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assert.assertEquals
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * Los finales con sentido del grupo 1 de la Etapa 3 (Memoria; `docs/finales-con-sentido.md`): Constelaciones, Rastro de luz, Bodega de carga y La estación de correo. Cada uno, en el orden de la plantilla de Aterrizaje: lo que hiciste con UN dato tuyo,
 * tu avance (hoy, tu promedio y tu mejor, con su frase), el truco, el botón «Ver el detalle de tu partida» (cerrado) y, abajo y en chico, «¿Por qué importa?». Capturas `*-final-sentido*.png`; la del detalle abierto, `*-final-sentido-detalle.png`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class Grupo1FinalesScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private fun points(key: String, vararg values: Float) = values.mapIndexed { i, v -> MeasurePoint(1_000L * (i + 1), key, v, 0.5f, false) }

  private val constelaciones = GamePlayResult(
    gameId = "parejas", score = 74, correctAnswers = 7, totalTrials = 9, timed = false, level = 8, endRating = 0.5f, timestamp = 1_000_000L,
    conGroup = 4, conGroups = 6, conMemGroups = 4, conBestStreak = 5, conBest = 6, conNewRecord = false
  )
  private val rastro = GamePlayResult(
    gameId = "secuencia", score = 74, correctAnswers = 14, totalTrials = 19, timed = false, level = 9, endRating = 0.5f, timestamp = 1_000_000L,
    rasBestLen = listOf(5, 4, 3, 2), rasRounds = listOf(7, 4, 5, 3), rasHits = listOf(7, 3, 2, 2), rasModesSeen = 15, rasNewModes = 8
  )
  private val bodega = GamePlayResult(
    gameId = "bodega", score = 74, correctAnswers = 9, totalTrials = 12, timed = false, level = 6, endRating = 0.5f, timestamp = 1_000_000L,
    bodGroup = 3, bodBestStreak = 4, bodBiggest = 5, bodBest = 6, bodNewRecord = false, bodMs = 4500
  )
  private val correo = GamePlayResult(
    gameId = "correo", score = 74, correctAnswers = 8, totalTrials = 11, timed = false, level = 5, endRating = 0.5f, timestamp = 1_000_000L,
    mailGroup = 2, mailEvHits = 4, mailEvTotal = 4, mailTimeHits = 1, mailTimeTotal = 2, mailCommissions = 0, mailPeeks = 3, mailPeeksGood = 1, mailRight = 6, mailBest = 9, mailNewRecord = false
  )

  private fun show(result: GamePlayResult, measures: List<MeasurePoint>) {
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          GameResultScreen(
            result = result, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 2, dailyTotalCount = 3,
            starMeasures = measures, onPlayAgain = {}, onContinue = {}
          )
        }
      }
    }
  }

  /** Las pantallas de arriba hacia abajo: la primera tal cual y una más por cada deslizada. */
  private fun capture(shot: String, pages: Int = 3) {
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/$shot-1.png")
    for (i in 2..pages) {
      composeTestRule.onAllNodes(hasScrollAction()).onFirst().performTouchInput { swipeUp(durationMillis = 100) }
      composeTestRule.waitForIdle()
      composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/$shot-$i.png")
    }
  }

  @Test
  fun final_de_constelaciones_con_sentido() {
    show(constelaciones, points("place", 60f, 70f, 80f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("constelaciones-final-sentido")
  }

  @Test
  fun final_de_rastro_de_luz_con_sentido() {
    show(rastro, points("trail", 4f, 5f, 4f, 5f, 4f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("rastro-final-sentido")
  }

  @Test
  fun final_de_bodega_con_sentido() {
    show(bodega, points("bodega", 58f, 66f, 83f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("bodega-final-sentido")
  }

  @Test
  fun final_de_correo_con_sentido() {
    show(correo, points("estacion", 55f, 64f, 70f))
    composeTestRule.onNodeWithTag("meaningful_result").assertExists()
    capture("correo-final-sentido")
  }

  @Test
  fun el_detalle_de_la_partida_esta_cerrado_y_se_abre_con_un_boton_de_56_dp_o_mas() {
    show(rastro, points("trail", 4f, 5f, 4f, 5f, 4f))
    // cerrado por defecto: lo que mostraba el juego antes («Por modo», las barras) no está a la vista
    composeTestRule.onNodeWithTag("detail_content").assertDoesNotExist()
    composeTestRule.onNodeWithText("Por modo").assertDoesNotExist()
    composeTestRule.onNodeWithText("Ver el detalle de tu partida").performScrollTo().assertIsDisplayed()
    val bounds = composeTestRule.onNodeWithTag("detail_toggle").fetchSemanticsNode().boundsInRoot
    val dp = composeTestRule.density.run { bounds.height.toDp().value }
    assert(dp >= 56f) { "el botón mide $dp dp y debe medir 56 o más" }
    // abierto: nada se borró, el detalle de antes está ahí
    composeTestRule.onNodeWithTag("detail_toggle").performClick()
    composeTestRule.waitForIdle()
    composeTestRule.onNodeWithTag("detail_content").assertExists()
    composeTestRule.onNodeWithText("Por modo").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithText("Ocultar el detalle de tu partida").assertExists()
    capture("rastro-final-sentido-detalle", pages = 4)
    // y se vuelve a cerrar
    composeTestRule.onNodeWithTag("detail_toggle").performScrollTo().performClick()
    composeTestRule.waitForIdle()
    composeTestRule.onNodeWithTag("detail_content").assertDoesNotExist()
  }

  private fun assertNoRepeatedHits(result: GamePlayResult, measures: List<MeasurePoint>) {
    // «7/9 aciertos» repetía lo que el recuadro principal ya dice; en los finales con sentido la fila común deja solo etapa y modo
    show(result, measures)
    assertEquals(result.gameId, 0, composeTestRule.onAllNodes(hasText("aciertos")).fetchSemanticsNodes().size)
    assertEquals(result.gameId, 1, composeTestRule.onAllNodes(hasText("etapa")).fetchSemanticsNodes().size)
    assertEquals(result.gameId, 1, composeTestRule.onAllNodes(hasText("modo")).fetchSemanticsNodes().size)
  }

  @Test
  fun la_fila_comun_de_constelaciones_ya_no_repite_los_aciertos() = assertNoRepeatedHits(constelaciones, points("place", 60f))

  @Test
  fun la_fila_comun_de_bodega_ya_no_repite_los_aciertos() = assertNoRepeatedHits(bodega, points("bodega", 60f))

  @Test
  fun la_fila_comun_de_correo_ya_no_repite_los_aciertos() = assertNoRepeatedHits(correo, points("estacion", 60f))

  @Test
  fun la_fila_comun_de_rastro_ya_no_repite_los_aciertos() = assertNoRepeatedHits(rastro, points("trail", 4f))
}
