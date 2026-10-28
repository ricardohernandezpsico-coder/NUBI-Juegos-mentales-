package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.Layout
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.AchievementFamily
import com.example.data.FamilyMedalState
import com.example.data.TuTile
import com.example.ui.components.AreaSquares
import com.example.ui.components.FamilyMedal
import com.example.ui.components.Glyph
import com.example.ui.components.KitGlyph
import com.example.ui.components.KitLlama
import com.example.ui.theme.Clay

/** El crema del cuerpo de las losas del kit (la de las medallas, #FFEBD2), liso: `Clay.Cream` es solo la clave blanca que `ClayCard` traduce a la losa de arcilla, y aquí no hay losa. */
private val FlatCream = androidx.compose.ui.graphics.Color(0xFFFFEBD2)

/** Lo que mide el ícono de cada cuadro (dp): el del kit, arriba al centro. */
private val ICON_SIZE = 40.dp

/** El ícono de plasticina de un cuadro: solo piezas del kit que la persona ya ve (la gota de la racha de Hoy, el «jugar» de las partidas, la medalla de Constancia y las cuatro casillas de las áreas de «Todos los juegos»). Decorativo: el cuadro lo dice con palabras. */
@Composable
private fun TileIcon(key: String, size: Dp, streakLit: Boolean, medal: FamilyMedalState?) {
  Box(Modifier.testTag("tu_icono_$key")) {
    when (key) {
      "racha" -> KitLlama(lit = streakLit, size = size)
      "partidas" -> KitGlyph(Glyph.PLAY, size = size)
      "medallas" -> if (medal != null) FamilyMedal(medal, size) else Spacer(Modifier.size(size))
      else -> AreaSquares(size = size * 0.9f)
    }
  }
}

/**
 * Los cuatro cuadros de Tú (Tarea 185; Fable, propuesta B «Lo tuyo en cuadros»; Ricardo eligió el 28-oct la variante «Centrada»: el ícono de plasticina del kit arriba al centro, la cifra grande, su unidad en una frase y la línea de apoyo, todo centrado, y el contenido
 * centrado también en altura) en una rejilla de 2 × 2 (de una columna con letra muy grande, para que ninguna palabra se parta), todos de la MISMA altura: la del más alto. Son informativos, no se tocan, así que van PLANOS (crema lisa, sin borde ni sombra dura): lo tocable es
 * arcilla y lo informativo es plano. TalkBack lee cada cuadro como una frase ([TuTile.spoken]).
 */
@Composable
fun TuCuadros(tiles: List<TuTile>, streakLit: Boolean, medal: FamilyMedalState?, modifier: Modifier = Modifier) {
  val columns = if (LocalDensity.current.fontScale > 1.45f) 1 else 2          // dos columnas; una sola con letra muy grande: así ninguna palabra se parte
  EqualHeightGrid(columns = columns, gap = 12.dp, modifier = modifier.fillMaxWidth().testTag("tu_cuadros")) {
    tiles.forEach { tile ->
      Box(
        Modifier.background(FlatCream, RoundedCornerShape(22.dp)).testTag("tu_cuadro_${tile.key}").semantics(mergeDescendants = true) { contentDescription = tile.spoken },
        contentAlignment = Alignment.Center                                      // el contenido también al centro de la altura: el de medallas, sin línea de apoyo, no queda con un hueco abajo
      ) {
        Box(Modifier.clearAndSetSemantics { }) { TileContent(tile, streakLit, medal) }
      }
    }
  }
}

@Composable
private fun TileContent(tile: TuTile, streakLit: Boolean, medal: FamilyMedalState?) {
  Column(Modifier.fillMaxWidth().padding(horizontal = 10.dp, vertical = 14.dp), horizontalAlignment = Alignment.CenterHorizontally) {
    TileIcon(tile.key, ICON_SIZE, streakLit, medal)
    Text(tile.figure, color = Clay.Ink, fontSize = 34.sp, lineHeight = 40.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 4.dp))
    Text(tile.unit, color = Clay.Ink, fontSize = 16.sp, lineHeight = 21.sp, textAlign = TextAlign.Center)
    tile.support?.let { Text(it, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 6.dp)) }
  }
}

/** La medalla que va en el cuadro «Medallas»: la de Constancia (la primera de las tres familias). */
fun medalForTile(medals: List<FamilyMedalState>): FamilyMedalState? = medals.firstOrNull { it.family == AchievementFamily.CONSTANCIA } ?: medals.firstOrNull()

/** Una rejilla de [columns] columnas donde TODOS los hijos miden lo mismo de alto: lo del más alto (con el ancho que les toca). */
@Composable
private fun EqualHeightGrid(columns: Int, gap: Dp, modifier: Modifier = Modifier, content: @Composable () -> Unit) {
  Layout(content = content, modifier = modifier) { measurables, constraints ->
    val gapPx = gap.roundToPx()
    val cellWidth = ((constraints.maxWidth - gapPx * (columns - 1)) / columns).coerceAtLeast(0)
    val cellHeight = measurables.maxOfOrNull { it.minIntrinsicHeight(cellWidth) } ?: 0
    val placeables = measurables.map { it.measure(Constraints.fixed(cellWidth, cellHeight)) }
    val rows = (placeables.size + columns - 1) / columns
    val height = rows * cellHeight + (rows - 1).coerceAtLeast(0) * gapPx
    layout(constraints.maxWidth, height) {
      placeables.forEachIndexed { i, p -> p.placeRelative((i % columns) * (cellWidth + gapPx), (i / columns) * (cellHeight + gapPx)) }
    }
  }
}
