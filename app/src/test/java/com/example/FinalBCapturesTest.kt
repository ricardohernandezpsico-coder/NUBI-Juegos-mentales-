package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.games.GameResultScreen
import com.example.model.AgeBand
import com.example.model.GameRegistry
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.cardData
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * Las capturas REALES de Roborazzi del final «B» (Tarea 146, spec-final-B §5 y §6): tres juegos (Rastro de luz, una cifra entera; Freno de Emergencia, una zona; En la punta de la lengua, un «N de M») × cuatro tomas (primera partida y con historial,
 * letra 1,0 y 1,3) → `src/test/screenshots/final-b/final-b-<juego>-<estado>-<letra>.png`, y la ficha de Rastro de luz con «Qué pones en juego». Cada pantalla se toma ENTERA (una ventana de 2000 dp de alto: nada se desliza) y `tools/previews/final_b.py`
 * la recorta a su contenido y la pone junto a la de HOY (las mismas tomas hechas con el código del commit anterior).
 * «Primera partida» = `playsBefore` 0 y sin historial; «con historial» = `playsBefore` 5 y 5 partidas comparables (`FinalesFixtures`).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h2000dp-xxhdpi", sdk = [36])
class FinalBCapturesTest {
  @get:Rule val rule = createComposeRule()

  private fun capture(gameId: String, withHistory: Boolean, fontScale: Float, shot: String) {
    val r = FinalesFixtures.byGame.getValue(gameId)
    val history = if (withHistory) FinalesFixtures.history(gameId) else emptyList()
    val plays = if (withHistory) 5 else 0
    rule.setContent {
      val d = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(d.density, fontScale)) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(2000.dp).background(Color(0xFF050823))) {
            GameResultScreen(result = r, didLevelUp = false, isDailyFlow = true, dailyCompletedCount = 1, dailyTotalCount = 3, starMeasures = history, onPlayAgain = {}, onContinue = {}, nextGameId = "calculo", playsBefore = plays)  // NEW-API
          }
        }
      }
    }
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/final-b/$shot.png")
  }

  @Test fun rastro_primera_1_0() = capture("secuencia", false, 1f, "final-b-rastro-primera-1.0")
  @Test fun rastro_historial_1_0() = capture("secuencia", true, 1f, "final-b-rastro-historial-1.0")
  @Test fun rastro_primera_1_3() = capture("secuencia", false, 1.3f, "final-b-rastro-primera-1.3")
  @Test fun rastro_historial_1_3() = capture("secuencia", true, 1.3f, "final-b-rastro-historial-1.3")

  @Test fun freno_primera_1_0() = capture("freno", false, 1f, "final-b-freno-primera-1.0")
  @Test fun freno_historial_1_0() = capture("freno", true, 1f, "final-b-freno-historial-1.0")
  @Test fun freno_primera_1_3() = capture("freno", false, 1.3f, "final-b-freno-primera-1.3")
  @Test fun freno_historial_1_3() = capture("freno", true, 1.3f, "final-b-freno-historial-1.3")

  @Test fun punta_primera_1_0() = capture("anagramas", false, 1f, "final-b-punta-primera-1.0")
  @Test fun punta_historial_1_0() = capture("anagramas", true, 1f, "final-b-punta-historial-1.0")
  @Test fun punta_primera_1_3() = capture("anagramas", false, 1.3f, "final-b-punta-primera-1.3")
  @Test fun punta_historial_1_3() = capture("anagramas", true, 1.3f, "final-b-punta-historial-1.3")

  /** La ficha de Rastro de luz plegada, en un marco de 393 × 801 dp (lo que le queda a la ventana en un teléfono común), con «Qué pones en juego» después de la medida. */
  @Test
  fun ficha_de_rastro() {
    val day = 86_400_000L
    val now = 400 * day + 3_600_000L
    val secuencia = GameRegistry.getById("secuencia")!!
    val series = listOf(4f, 5f, 4f, 5f, 4f, 5f).let { v -> v.mapIndexed { i, x -> MeasurePoint(now - (v.size - i) * day, "trail", x, 0.5f, true) } }
    val data = cardData(secuencia, secuencia.title, 0.55f)
    rule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(801.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
          Box(Modifier.fillMaxWidth()) {
            GameSheetContent(data, AgeBand.SENIOR, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = true, onChooseTimed = {})
          }
        }
      }
    }
    rule.waitForIdle()
    rule.onNodeWithTag("frame").captureRoboImage(filePath = "src/test/screenshots/final-b/final-b-ficha-rastro.png")
  }
}
