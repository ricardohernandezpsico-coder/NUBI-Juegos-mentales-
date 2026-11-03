package com.example.ui.screens

import androidx.compose.foundation.ScrollState
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.material3.LocalTextStyle
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.input.nestedscroll.NestedScrollConnection
import androidx.compose.ui.input.nestedscroll.NestedScrollSource
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.CollectionInfo
import androidx.compose.ui.semantics.collectionInfo
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.rememberTextMeasurer
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.HistoryCard
import com.example.data.HistoryRow
import com.example.ui.components.GameEmblem
import com.example.ui.components.SlabSize
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayCard
import com.example.ui.theme.TitleFamily

/** El título de la tarjeta (las mismas palabras de la sección que salió de Avance en la Tarea 159 y del gráfico de la ficha). */
internal const val HISTORY_TITLE = "Tus últimas partidas"

/**
 * «Tus últimas partidas» en Hoy (Tarea 221, la opción A que eligió Ricardo; `auditoria-fable/pestana-juegos-historial.md` §5 y §10): una losa crema como la de la ruta, después del Contrarreloj, con el título «Tus últimas partidas» (18 sp Rubik Bold, encabezado) y una línea fina debajo,
 * y las filas como las de la ruta: el emblema (36 dp), el nombre (17 sp, negrita) y debajo «cuándo · modo · marca» (16 sp en tinta suave, a dos renglones por palabras si hace falta); 52 dp como mínimo y una línea fina de tinta al 12 % entre filas. **No se toca**: ninguna fila lleva `onClick` ni `Role.Button`;
 * el juego se abre desde la pestaña Juegos. Cada fila va fusionada para el lector de pantalla («Carga exacta, hoy, en tu ruta, 70 % de las cargas, sin pista.»).
 *
 * **El alto se mide EN FILAS** ([HistoryCard]): con 4 o más partidas la tarjeta muestra tres filas enteras y la MITAD de la cuarta (la señal de «hay más») y las filas se deslizan POR DENTRO (una `LazyColumn` de alto fijo, con el aire del final); con 1 a 3 tiene su alto natural y no se desliza.
 * Cada fila se mide con el texto real, al ancho real y con la letra del sistema (`rememberTextMeasurer`, como `HoyStops`). Cómo conviven este deslizado y el de Hoy: [hoyFirstConnection].
 */
@Composable
internal fun HistoryCard(rows: List<HistoryRow>, modifier: Modifier = Modifier) {
  if (rows.isEmpty()) return
  val measurer = rememberTextMeasurer()
  val base = LocalTextStyle.current
  val density = LocalDensity.current
  ClayCard(modifier = modifier.fillMaxWidth().testTag("home_history"), color = Clay.Cream, size = SlabSize.LARGE, contentPadding = 0.dp) {
    BoxWithConstraints(Modifier.fillMaxWidth().padding(horizontal = 16.dp)) {
      val nameStyle = base.merge(TextStyle(fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp))
      val detailStyle = base.merge(TextStyle(fontSize = 16.sp, lineHeight = 20.sp))
      val textPx = with(density) { (maxWidth - HistoryEmblem - HistoryEmblemGap).roundToPx() }
      // lo que mide cada fila con SU texto, al ancho real: el nombre y la segunda línea (una o varias líneas)
      val heights = remember(rows, textPx, density.density, density.fontScale) {
        rows.map { r ->
          val c = Constraints(maxWidth = textPx)
          val px = measurer.measure(r.title, nameStyle, constraints = c).size.height + measurer.measure(r.shownDetail, detailStyle, constraints = c).size.height
          HistoryCard.rowHeight(with(density) { px.toDp().value })
        }
      }
      val scrolls = HistoryCard.scrolls(rows.size)
      val windowDp = HistoryCard.windowHeight(heights).dp
      Column(Modifier.fillMaxWidth()) {
        Spacer(Modifier.height(HistoryCard.TITLE_TOP_DP.dp))
        Text(
          HISTORY_TITLE, color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 22.sp,
          modifier = Modifier.fillMaxWidth().semantics { heading() }.testTag("home_history_title")
        )
        Spacer(Modifier.height((HistoryCard.TITLE_GAP_DP - HistoryCard.DIVIDER_DP).dp))
        HistoryLine()
        if (scrolls) {
          // la lista publica su cuenta (TalkBack dice «en lista, 10 elementos»): la `LazyColumn` sola dice «-1» (no sabe cuántas hay), así que se la damos
          LazyColumn(Modifier.fillMaxWidth().height(windowDp).semantics { collectionInfo = CollectionInfo(rowCount = rows.size, columnCount = 1) }.testTag("home_history_list"), contentPadding = PaddingValues(bottom = HistoryCard.END_AIR_DP.dp)) {
            itemsIndexed(rows, key = { _, r -> r.timestamp.toString() + r.gameId + r.timeTrial }) { i, r ->
              Column(Modifier.fillMaxWidth()) {
                if (i > 0) HistoryLine()
                HistoryRowItem(r, i + 1)
              }
            }
          }
        } else {
          Column(Modifier.fillMaxWidth().height(windowDp).testTag("home_history_list")) {
            rows.forEachIndexed { i, r ->
              if (i > 0) HistoryLine()
              HistoryRowItem(r, i + 1)
            }
          }
        }
      }
    }
  }
}

private val HistoryEmblem = 36.dp
private val HistoryEmblemGap = 10.dp

/** La línea fina de tinta al 12 % (la de las filas de la ruta): bajo el título es el borde de la ventana de la lista. */
@Composable
private fun HistoryLine() {
  Spacer(Modifier.fillMaxWidth().height(HistoryCard.DIVIDER_DP.dp).background(Clay.Ink.copy(alpha = 0.12f)))
}

/**
 * Una partida: el emblema, el nombre y «cuándo · modo · marca». Como `HoyStopRow`: 52 dp como mínimo, 4 dp de aire arriba y abajo. NO es un botón. El lector dice la fila entera, fusionada ([HistoryRow.spoken]).
 */
@Composable
private fun HistoryRowItem(row: HistoryRow, number: Int) {
  Row(
    Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = row.spoken }.testTag("home_history_row_$number").heightIn(min = HistoryCard.MIN_ROW_DP.dp).padding(vertical = (HistoryCard.ROW_PAD_DP / 2f).dp),
    verticalAlignment = Alignment.CenterVertically
  ) {
    GameEmblem(row.gameId, HistoryEmblem)
    Spacer(Modifier.width(HistoryEmblemGap))
    Column(Modifier.weight(1f)) {
      Text(row.title, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp, modifier = Modifier.testTag("home_history_name_$number"))
      Text(row.shownDetail, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.testTag("home_history_detail_$number"))
    }
  }
}

/**
 * Cómo conviven los dos deslizados de Hoy (Tarea 221, §10.3): **hacia abajo, Hoy primero; hacia arriba, la lista primero**.
 * - **El dedo sube** (ver lo de abajo): Hoy se desliza hasta su final, que es la tarjeta entera sobre la barra; solo cuando Hoy ya no puede avanzar, el mismo gesto mueve la lista por dentro hasta la última fila.
 * - **El dedo baja** (volver): la lista vuelve a su principio primero (es la hija, va antes por omisión); lo que sobra pasa a Hoy por el camino de siempre.
 * - El impulso (soltar con velocidad) pasa por la misma conexión: los pasos del impulso de la lista se despachan por `onPreScroll` igual que el dedo.
 * La lista nunca se mueve mientras la tarjeta está a medias: para quien mira es UN movimiento continuo (primero baja la página, después bajan las filas).
 *
 * Va en la `Column` de Hoy, ANTES de su `verticalScroll` (`Modifier.nestedScroll(hoyFirstConnection(state)).verticalScroll(state)`): así queda como padre de la lista y recibe cada gesto antes que ella. Es el patrón de las barras que se pliegan (`exitUntilCollapsedScrollBehavior`).
 * El signo del delta (`available.y < 0` = el dedo sube) lo fija `HoyHistoryScrollTest`, no la memoria. NO se apaga `userScrollEnabled` de la lista a mitad de un gesto (cancelaría el toque).
 */
internal fun hoyFirstConnection(state: ScrollState): NestedScrollConnection = object : NestedScrollConnection {
  override fun onPreScroll(available: Offset, source: NestedScrollSource): Offset {
    if (available.y < 0f && state.canScrollForward) {
      val consumed = state.dispatchRawDelta(-available.y)
      return Offset(0f, -consumed)
    }
    return Offset.Zero
  }
}
