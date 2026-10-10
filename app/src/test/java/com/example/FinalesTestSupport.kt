package com.example

import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.junit4.ComposeContentTestRule
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.test.onFirst
import androidx.compose.ui.text.TextLayoutResult
import com.example.data.MeasurePoint
import com.example.games.FinalModels
import com.example.model.GamePlayResult
import org.junit.Assert.assertTrue

/**
 * La regla de Opus (10-oct) para los finales con sentido: la línea que va bajo la cifra grande (`headlineUnit`) ocupa 2 líneas como máximo en un teléfono de 393 dp de ancho.
 * Se mide el texto que de verdad se dibuja (su `TextLayoutResult`), no su largo.
 */
internal fun ComposeContentTestRule.assertUnitLineAtMostTwoLines(result: GamePlayResult, measures: List<MeasurePoint>) {
  val unit = FinalModels.forGame(result, measures)?.headlineUnit?.takeIf { it.isNotBlank() } ?: return
  val node = onAllNodesWithText(unit).onFirst().fetchSemanticsNode()
  val layouts = mutableListOf<TextLayoutResult>()
  node.config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(layouts)
  val lines = layouts.firstOrNull()?.lineCount ?: error("no se pudo medir la línea «$unit»")
  assertTrue("«$unit» ocupa $lines líneas a 393 dp (máximo 2)", lines <= 2)
}
