package com.example.ui.screens

import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.PlaceGameRow
import com.example.data.PlaceSheetUi
import com.example.data.PlaceSheets
import com.example.ui.theme.Clay

// ------------------------------------------------------------------ Lo que «Todos los juegos» conserva de la lámina del lugar (Tarea 165)
//
// Hasta la Tarea 162 este archivo era la LÁMINA del lugar: la que se abría al tocar un edificio de la isla de Hoy, con el velo, Nubi volando, el globo y el avance del área (Tareas 85, 147, 155, 158 y 159). La prueba de Ricardo con 5 personas (22-oct) la contradijo: la gente
// no leía los edificios como áreas y prefería «Todos los juegos». Desde la Tarea 165 («Hoy, una sola tarjeta») Hoy no tiene isla grande ni edificios que se toquen y la lámina se BORRÓ (no se apagó con un interruptor). Lo que queda es lo que la lista comparte: el color de
// cada área, la fila de un juego y el armado de las filas de cada área.

/** El color de cada área (los del boceto): el punto junto al nombre del área en «Todos los juegos». */
internal fun placeColor(area: String): Color = when (area) {
  "MEMORIA" -> Color(0xFF4F86D9)
  "ATENCION" -> Color(0xFFE8913A)
  "RAZONAMIENTO" -> Color(0xFF8A6FD8)
  else -> Color(0xFF3E9C5C)
}

/** El texto secundario de una fila de juego sobre la losa crema. */
internal val SheetMuted = Color(0xFF4A4470)

/**
 * Una fila de juego («Todos los juegos»): losa crema del kit con su estado presionado (sol si es la [marked]: el último que se jugó desde la lista, para encontrarlo al volver); a la izquierda el emblema (48 dp, con el sello «jugado hoy»), a su derecha el nombre (16 sp Bold) y cuándo se jugó (16 sp);
 * el sugerido por Nubi lleva el chip sol «Nubi sugiere» pegado al borde derecho. Si la palabra más larga del nombre no cabe junto al chip (letra grande), el chip baja DENTRO de la columna de texto, bajo el «cuándo» (la fila crece). Se lee «Satélites, sin jugar aún, Nubi lo sugiere».
 */
@Composable
internal fun PlaceGameRowItem(row: PlaceGameRow, suggested: Boolean, onClick: () -> Unit, marked: Boolean = false) {
  val density = LocalDensity.current
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  com.example.ui.components.ClaySlab(
    color = if (marked) com.example.ui.components.SlabColor.SUN else com.example.ui.components.SlabColor.CREAM,
    size = com.example.ui.components.SlabSize.SMALL,
    onClick = onClick,
    role = Role.Button,
    onClickLabel = "Ver su ficha",
    contentAlignment = Alignment.CenterStart,
    modifier = Modifier
      .fillMaxWidth()
      .heightIn(min = 60.dp)
      .semantics(mergeDescendants = true) { contentDescription = row.title + ", " + row.spoken + (if (suggested) ", Nubi lo sugiere" else "") + (if (marked) ", el último que jugaste" else "") }
      .testTag("place_game_${row.gameId}")
  ) {
    BoxWithConstraints(Modifier.fillMaxWidth()) {
      val bold = base.merge(androidx.compose.ui.text.TextStyle(fontSize = 16.sp, fontWeight = FontWeight.Bold))
      val longestWordPx = row.title.split(' ').maxOfOrNull { measurer.measure(it, bold, softWrap = false, maxLines = 1).size.width } ?: 0
      val chipPx = measurer.measure(com.example.data.Suggestion.CHIP, bold, softWrap = false, maxLines = 1).size.width + with(density) { 20.dp.toPx() }
      val sidePx = with(density) { (20.dp + 48.dp + 10.dp + 8.dp).toPx() }                      // padding de la fila + emblema + aire + aire del chip
      val chipBeside = suggested && constraints.maxWidth - sidePx - chipPx >= longestWordPx
      Row(Modifier.fillMaxWidth().padding(horizontal = 10.dp, vertical = 6.dp), verticalAlignment = Alignment.CenterVertically) {
        com.example.ui.components.GameEmblem(row.gameId, 48.dp, done = row.daysAgo == 0)
        Spacer(Modifier.width(10.dp))
        Column(Modifier.weight(1f)) {
          Text(row.title, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 20.sp, modifier = Modifier.testTag("place_game_name_${row.gameId}"))
          Text(row.whenShort, color = SheetMuted, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.testTag("place_game_when_${row.gameId}"))
          if (suggested && !chipBeside) com.example.ui.theme.ClayPill(com.example.data.Suggestion.CHIP, Modifier.padding(top = 4.dp).testTag("place_game_chip"), color = Clay.Sun, compact = true)
        }
        if (suggested && chipBeside) {
          Spacer(Modifier.width(8.dp))
          com.example.ui.theme.ClayPill(com.example.data.Suggestion.CHIP, Modifier.testTag("place_game_chip"), color = Clay.Sun, compact = true)
        }
      }
    }
  }
}

/**
 * Las filas de CADA área para «Todos los juegos»: se arma con LA misma lógica ([PlaceSheets.build]) y los mismos datos —los juegos del área (con «hoy, en tu ruta» / «hecho hoy» en las paradas de la ruta [routeGameIds]/[routeCompleted]) y LA sugerencia de Nubi ([suggestion])—.
 * La clave del mapa es el área (`MEMORIA`…).
 */
@Composable
internal fun rememberAreaPlaces(
  history: List<com.example.model.GamePlayResult>,
  measures: List<com.example.data.MeasurePoint>,
  now: Long,
  lang: com.example.model.AppLanguage,
  suggestion: com.example.data.Suggestion.Pick?,
  routeGameIds: List<String>,
  routeCompleted: Int
): Map<String, PlaceSheetUi> = remember(history, measures, now, lang, suggestion, routeGameIds, routeCompleted) {
  val routes = com.example.data.routeStatesOf(routeGameIds, routeCompleted)
  com.example.ui.components.AREA_ORDER.associateWith { key ->
    val domain = com.example.ui.components.domainOf(key)
    PlaceSheets.build(
      area = key, games = com.example.model.GameRegistry.allGames.filter { it.domain == domain }, titleOf = { g -> com.example.ui.i18n.getGameTitle(g.id, lang, g.title) },
      history = history, measures = measures, now = now, suggestion = suggestion, routes = routes
    )
  }
}
