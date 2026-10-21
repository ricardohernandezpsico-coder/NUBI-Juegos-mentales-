package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.games.FinalModels
import com.example.games.GameResultScreen
import com.example.model.GamePlayResult
import com.example.ui.components.ResultHeadline
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * Que el final «B» (Tarea 146; `auditoria-fable/spec-final-B.md` §1 y §5) entre en la pantalla de un teléfono, en los 17 juegos, desde la cuarta partida (sin «¿Por qué importa?» a la vista): a letra 1,0 «Jugar de nuevo» termina por encima del borde
 * de abajo; a letra 1,3 el botón principal EMPIEZA por encima del borde (se ve sin deslizar). Y que en el botón «Siguiente: …» ninguna palabra se parta, ni con el nombre más largo.
 * Se prueba en 393 × 873 dp y en 360 × 780 dp. Cada prueba arma UNA pantalla y le cambia la partida (un solo `setContent`).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class FinalFitTest {
  @get:Rule val rule = createComposeRule()

  private var current by mutableStateOf<GamePlayResult?>(null)
  private var nextGame by mutableStateOf<String?>(null)
  private var moreToday by mutableStateOf(false)

  private fun mount(widthDp: Int, heightDp: Int, fontScale: Float) {
    rule.setContent {
      val d = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(d.density, fontScale)) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823))) {
            current?.let { r ->
              GameResultScreen(
                result = r, didLevelUp = false, isDailyFlow = moreToday, dailyCompletedCount = if (moreToday) 1 else 0, dailyTotalCount = 3,
                starMeasures = FinalesFixtures.history(r.gameId), onPlayAgain = {}, onContinue = {},
                nextGameId = nextGame, playsBefore = 3
              )
            }
          }
        }
      }
    }
  }

  private fun show(r: GamePlayResult) { current = r; rule.waitForIdle() }

  private fun bottomDp(tag: String): Float {
    val n = rule.onNodeWithTag(tag).fetchSemanticsNode()
    return (n.positionInRoot.y + n.size.height) / rule.density.density
  }

  private fun topDp(tag: String): Float = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y / rule.density.density

  private fun everyGameEndsAboveTheBottom(widthDp: Int, heightDp: Int) {
    mount(widthDp, heightDp, 1f)
    for (r in FinalesFixtures.all) {
      show(r)
      val bottom = bottomDp("btn_result_replay")
      assertTrue("${r.gameId} a ${widthDp}x$heightDp con letra 1,0: «Jugar de nuevo» termina en $bottom dp (límite $heightDp)", bottom <= heightDp)
    }
  }

  private fun everyGameShowsTheMainButtonWithBigLetters(widthDp: Int, heightDp: Int) {
    mount(widthDp, heightDp, 1.3f)
    for (r in FinalesFixtures.all) {
      show(r)
      val top = topDp("btn_result_continue")
      assertTrue("${r.gameId} a ${widthDp}x$heightDp con letra 1,3: el botón principal empieza en $top dp (límite $heightDp)", top <= heightDp)
    }
  }

  @Test fun los_17_juegos_terminan_encima_del_borde_en_393_por_873_con_letra_1_0() = everyGameEndsAboveTheBottom(393, 873)
  @Test fun los_17_juegos_terminan_encima_del_borde_en_360_por_780_con_letra_1_0() = everyGameEndsAboveTheBottom(360, 780)
  @Test fun los_17_juegos_muestran_el_boton_principal_en_393_por_873_con_letra_1_3() = everyGameShowsTheMainButtonWithBigLetters(393, 873)
  @Test fun los_17_juegos_muestran_el_boton_principal_en_360_por_780_con_letra_1_3() = everyGameShowsTheMainButtonWithBigLetters(360, 780)

  // ------------------------------------------------------------------ el botón «Siguiente: …» nunca parte una palabra

  /** Con el nombre de CADA uno de los 17 juegos (el más largo es «En la punta de la lengua»; «La estrella intrusa» y «Freno de Emergencia» le siguen): «Siguiente: {nombre} · 2 de 3» nunca parte una palabra a la mitad. */
  private fun noWordSplit(widthDp: Int, fontScale: Float) {
    moreToday = true; nextGame = "calculo"
    mount(widthDp, 873, fontScale)
    for (g in com.example.model.GameRegistry.allGames) {
      nextGame = g.id
      show(FinalesFixtures.byGame.getValue("secuencia"))
      val text = "Siguiente: ${g.title} · 2 de 3"
      val node = rule.onNode(hasText(text), useUnmergedTree = true).fetchSemanticsNode()
      val layouts = mutableListOf<TextLayoutResult>()
      node.config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(layouts)
      val layout = layouts.firstOrNull() ?: error("no se pudo medir «$text»")
      for (line in 0 until layout.lineCount) {
        val end = layout.getLineEnd(line)
        val atWordEdge = end >= text.length || text[end - 1].isWhitespace() || text[end].isWhitespace()
        assertTrue("«$text» a $widthDp dp con letra $fontScale parte una palabra en la línea ${line + 1} (corte en $end)", atWordEdge)
      }
      assertTrue("«$text» a $widthDp dp con letra $fontScale ocupa ${layout.lineCount} líneas (máximo 4)", layout.lineCount <= 4)
    }
  }

  @Test fun el_boton_siguiente_no_parte_palabras_en_393_con_letra_1_0() = noWordSplit(393, 1f)
  @Test fun el_boton_siguiente_no_parte_palabras_en_393_con_letra_1_3() = noWordSplit(393, 1.3f)
  @Test fun el_boton_siguiente_no_parte_palabras_en_360_con_letra_1_3() = noWordSplit(360, 1.3f)
  @Test fun el_boton_siguiente_no_parte_palabras_en_360_con_letra_2_0() = noWordSplit(360, 2f)

  // ------------------------------------------------------------------ la unidad bajo la cifra: dos líneas como máximo, también «sin medida»

  private fun unitLines(model: com.example.ui.components.MeaningfulResultModel, tagIndex: Int): Int {
    val nodes = rule.onAllNodes(hasTestTag("result_unit")).fetchSemanticsNodes()
    val layouts = mutableListOf<TextLayoutResult>()
    nodes[tagIndex].config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(layouts)
    return layouts.firstOrNull()?.lineCount ?: error("no se pudo medir «${model.headlineUnit}»")
  }

  @Test
  fun las_unidades_de_los_17_con_medida_y_las_sin_medida_ocupan_dos_lineas_como_maximo_a_393_dp() {
    val withMeasure = FinalesFixtures.all.mapNotNull { FinalModels.forGame(it, emptyList()) }
    val without = listOf(
      FinalesFixtures.byGame.getValue("parejas").copy(correctAnswers = 0, totalTrials = 0),
      FinalesFixtures.byGame.getValue("correo").copy(correctAnswers = 0, totalTrials = 0),
      FinalesFixtures.byGame.getValue("stroop").copy(interferenceMs = null),
      FinalesFixtures.byGame.getValue("piloto").copy(pilSignalPct = null),
      FinalesFixtures.byGame.getValue("freno").copy(brakeMs = null),
      FinalesFixtures.byGame.getValue("satelites").copy(trackingCapacity = null),
      FinalesFixtures.byGame.getValue("radar").copy(glanceMs = null),
      FinalesFixtures.byGame.getValue("acoplamiento").copy(rotationSpeedDps = null, rotationCurveMs = null),
      FinalesFixtures.byGame.getValue("meteoros").copy(lexBandSeen = listOf(2, 2, 2, 2, 2, 2), lexBandHits = listOf(2, 2, 2, 2, 2, 2)),
      FinalesFixtures.byGame.getValue("disparate").copy(svWpm = null),
      FinalesFixtures.byGame.getValue("secuencia").copy(rasBestLen = listOf(0, 0, 0, 0), rasRounds = listOf(2, 0, 0, 0), rasHits = listOf(0, 0, 0, 0))
    ).mapNotNull { FinalModels.forGame(it, emptyList()) }
    val models = withMeasure + without
    assertEquals(16 + 11, models.size)
    rule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).background(Color(0xFF050823))) {
          Column(Modifier.padding(horizontal = 24.dp)) { models.forEach { ResultHeadline(it) } }
        }
      }
    }
    rule.waitForIdle()
    models.forEachIndexed { i, m ->
      val lines = unitLines(m, i)
      assertTrue("«${m.headlineUnit}» ocupa $lines líneas a 345 dp de texto (máximo 2)", lines <= 2)
    }
  }
}
