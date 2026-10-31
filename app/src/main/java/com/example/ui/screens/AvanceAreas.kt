package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.AreaPoints
import com.example.data.BestMark
import com.example.data.MeasurePhrases
import com.example.data.Week
import com.example.model.DomainType
import com.example.ui.components.GameEmblem
import com.example.ui.components.OnBoardText
import com.example.ui.components.SpaceSectionTitle
import com.example.ui.components.WeekLampRendered
import com.example.ui.theme.Clay
import com.example.ui.theme.TitleFamily
import com.example.ui.theme.ClayCard

private val OnNight = androidx.compose.ui.graphics.Color(0xFFEAF0FF)
private val OnNightDim = com.example.ui.theme.NubiTokens.OnNightDim

/**
 * Avance con PUNTOS por área (Tarea 159; Ricardo, 22-oct: «con puntos por área»; `auditoria-fable/avance-b-puntos.md` §3): el título, UNA línea de la semana, las cuatro áreas con sus puntos (desde la Tarea 174, una cuadrícula de 2 × 2: [AvanceAreaGrid]) y «Tus mejores marcas». Lo largo de la semana
 * (farolas grandes, racha, globo de Nubi) vive en Hoy y no se repite aquí; los detalles de cada área, en su página ([AvanceAreaPage]). Cada número lleva su unidad y su «de cuánto»; nada de flechas, signos más o menos ni abreviaturas.
 */

/** Tu semana en una línea: las 7 farolas chicas con «hoy» y «2 de 4 días de tu meta». Si el texto no cabe al lado de las farolas (frase larga o letra grande), baja a su propia línea: nunca se parte en dos. [empty] = todavía ninguna partida. */
@Composable
internal fun AvanceWeekLine(lamps: Week.Lamps, goal: Int, empty: Boolean, modifier: Modifier = Modifier) {
  val text = if (empty) "Tu semana empieza hoy" else Week.caption(lamps.daysPlayed, goal)
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  val style = androidx.compose.material3.LocalTextStyle.current.merge(androidx.compose.ui.text.TextStyle(fontSize = 16.sp, fontWeight = FontWeight.SemiBold))
  val density = LocalDensity.current
  @Composable
  fun Lamps() {
    Row(Modifier.semantics { contentDescription = Week.lampsSpoken(lamps.daysPlayed) }.testTag("avance_lamps"), horizontalArrangement = Arrangement.spacedBy(0.dp)) {
      lamps.played.forEachIndexed { i, on -> WeekLampRendered(on = on, today = i == lamps.todayIndex, size = 26.dp) }
    }
  }
  @Composable
  fun Caption(m: Modifier) = OnBoardText(text, color = OnNight, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, lineHeight = 21.sp, modifier = m.testTag("avance_week_line"))
  androidx.compose.foundation.layout.BoxWithConstraints(modifier.fillMaxWidth().testTag("avance_week")) {
    val textWidth = with(density) { measurer.measure(text, style, softWrap = false, maxLines = 1).size.width.toDp() }
    val beside = 26.dp * 7 + 10.dp + textWidth + 4.dp <= maxWidth
    if (beside) Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) { Lamps(); Spacer(Modifier.width(10.dp)); Caption(Modifier.weight(1f)) }
    else Column(Modifier.fillMaxWidth()) { Lamps(); Caption(Modifier.padding(top = 2.dp)) }
  }
}

/** El margen de adentro de un cuadro de área a cada lado: lo justo para que «Razonamiento» (Rubik Bold a 16 sp, unos 151 dp con letra 1,3) entre entero en un cuadro de unos 172 dp. */
private val CellPad = 8.dp
private val AreaTrack = androidx.compose.ui.graphics.Color(0xFFE2DAC8)

/**
 * Tus áreas (Tarea 174, 25-oct; Ricardo, sobre un boceto de Opus, después de que 5 personas dijeran que la lista era «monótona» y de descartar los edificios de la isla y el radar de Fable): las cuatro áreas en una CUADRÍCULA de 2 × 2. Cada cuadro es una losa crema de arcilla que se toca y abre la página
 * del área ([AvanceAreaPage], que no cambia) y lleva SOLO tres cosas, siempre en el mismo lugar: el NOMBRE (Rubik Bold, [TitleFamily], con un solo tamaño para los cuatro: el mayor, de 20 a 16 sp, al que el nombre más largo cabe entero), los PUNTOS («730» grande y debajo «de 1000 puntos»; un área
 * sin medir dice «Aún sin medir», como el resto de la app) y una BARRA del color del área sobre su riel (de 0 a 1000 puntos; con el contorno de tinta, para que se vea sin depender del color). Con letra mayor que 1,3, o si «Razonamiento» no cabe entero a 16 sp en media fila, va un cuadro por fila. La etapa, lo que falta,
 * la frase de la semana y «tu mejor marca sigue en N puntos» ya no están aquí: viven en la página del área. Nada se distingue solo por color: el nombre identifica cada área.
 */
@Composable
internal fun AvanceAreaGrid(areas: List<AreaPoints>, names: Map<String, String>, onArea: (String) -> Unit) {
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  val density = LocalDensity.current
  Column {
    SpaceSectionTitle("Tus áreas", hint = "Toca un área para ver sus juegos y tus marcas", fontFamily = TitleFamily)
    Spacer(Modifier.height(8.dp))
    androidx.compose.foundation.layout.BoxWithConstraints(Modifier.fillMaxWidth()) {
      val gapPx = with(density) { 8.dp.roundToPx() }
      val padPx = with(density) { CellPad.roundToPx() }
      fun namePx(text: String, sp: Int) = measurer.measure(text, base.merge(androidx.compose.ui.text.TextStyle(fontSize = sp.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily)), softWrap = false, maxLines = 1).size.width
      val nameOf = { p: AreaPoints -> names[p.area] ?: p.area }
      val twoPx = (constraints.maxWidth - gapPx) / 2 - 2 * padPx
      val cols = if (density.fontScale <= 1.3f && areas.all { namePx(nameOf(it), 16) <= twoPx }) 2 else 1
      val innerPx = (constraints.maxWidth - gapPx * (cols - 1)) / cols - 2 * padPx
      val nameSp = (20 downTo 16).firstOrNull { sp -> areas.all { namePx(nameOf(it), sp) <= innerPx } } ?: 16
      Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        areas.chunked(cols).forEach { row ->
          Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            row.forEach { p -> AreaCell(p, nameOf(p), nameSp, { onArea(p.area) }, Modifier.weight(1f)) }
          }
        }
      }
    }
  }
}

/** Un cuadro de área: nombre, puntos y barra (de arriba abajo, siempre en ese orden y a la misma altura en los cuatro). El lector de pantalla lo lee entero: «Memoria, 470 de 1000 puntos». */
@Composable
private fun AreaCell(p: AreaPoints, name: String, nameSp: Int, onClick: () -> Unit, modifier: Modifier) {
  val key = p.area.lowercase()
  val density = LocalDensity.current
  ClayCard(
    modifier = modifier.semantics(mergeDescendants = true) { contentDescription = AreaPoints.cardSpoken(p, name) }.testTag("progress_area_$key"),
    onClick = onClick, contentPadding = 0.dp
  ) {
    Column(Modifier.fillMaxWidth().padding(horizontal = CellPad, vertical = 12.dp)) {
      Text(name, color = Clay.Ink, fontSize = nameSp.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = (nameSp + 4).sp, maxLines = 1, softWrap = false, modifier = Modifier.testTag("area_name_$key"))
      // los puntos: la cifra grande y debajo «de 1000 puntos»; SIEMPRE la misma altura (la de las dos líneas más un respiro), haya cifra o no, para que la barra quede a la misma altura en los cuatro
      Column(Modifier.padding(top = 6.dp).height(with(density) { 36.sp.toDp() + 20.sp.toDp() + 6.dp })) {
        if (p.measured) {
          Text(p.total.toString(), color = Clay.Ink, fontSize = 32.sp, fontWeight = FontWeight.Bold, lineHeight = 36.sp, modifier = Modifier.testTag("area_points_$key"))
          Text("de ${AreaPoints.SCALE} puntos", color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.testTag("area_label_$key"))
        } else {
          Text("Aún sin medir", color = Clay.InkMuted, fontSize = 18.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.testTag("area_unmeasured_$key"))
        }
      }
      Spacer(Modifier.height(8.dp))
      AreaBar(if (p.measured) p.total / AreaPoints.SCALE.toFloat() else 0f, DomainType.fromStored(p.area)?.color ?: Clay.Ink, key)
    }
  }
}

/**
 * La barra de un área: un riel crema con contorno de tinta y, encima, el relleno del color del área hasta [fill] (0 a 1) con un canto de tinta en su punta; una barra vacía es solo el riel. Mide 16 dp de alto. Con puntos, el relleno mide al menos 12 dp (una rayita se vería como un defecto).
 * [change] (0 a 1, en la misma escala; Tarea 198, el resumen de la ruta) = lo que se sumó HOY: ese tramo del final del relleno va a media tinta («lo que se sumó») y lo de antes a todo color; con 0 (Avance) la barra es la de siempre. Decorativa para el lector de pantalla: el cuadro ya dice los puntos.
 */
@Composable
internal fun AreaBar(fill: Float, color: androidx.compose.ui.graphics.Color, key: String, change: Float = 0f) {
  val shape = androidx.compose.foundation.shape.RoundedCornerShape(8.dp)
  androidx.compose.foundation.layout.BoxWithConstraints(Modifier.fillMaxWidth().height(16.dp).clip(shape).background(AreaTrack).border(2.dp, Clay.Ink, shape).testTag("area_bar_$key")) {
    if (fill > 0f) {
      val w = (maxWidth * fill.coerceIn(0f, 1f)).coerceAtLeast(12.dp)
      val before = (fill - change).coerceIn(0f, fill)
      Box(
        Modifier.width(w).fillMaxHeight().drawBehind {
          if (change > 0f && before < fill) {
            val beforePx = (maxWidth * before).toPx().coerceIn(0f, size.width)
            drawRect(color, size = androidx.compose.ui.geometry.Size(beforePx, size.height))
            drawRect(color.copy(alpha = 0.5f), topLeft = androidx.compose.ui.geometry.Offset(beforePx, 0f), size = androidx.compose.ui.geometry.Size(size.width - beforePx, size.height))
          } else drawRect(color)
          if (w < maxWidth) drawRect(Clay.Ink, topLeft = androidx.compose.ui.geometry.Offset(size.width - 2.dp.toPx(), 0f), size = androidx.compose.ui.geometry.Size(2.dp.toPx(), size.height))
        }.testTag("area_bar_fill_$key")
      )
    }
  }
}

/** El punto del color del área, con el borde de tinta del kit (como el de la lámina de un lugar). */
@Composable
internal fun AreaDot(areaKey: String) {
  val color = DomainType.fromStored(areaKey)?.color ?: Clay.Ink
  Box(Modifier.size(12.dp).clip(CircleShape).background(color).border(2.dp, Clay.Ink, CircleShape))
}

/**
 * Tus mejores marcas: de una a tres, cada una con el emblema de su juego, lo que mejoraste, la marca con su unidad en una frase y cuándo fue («hace 6 días»); comparadas solo contigo, y solo de juegos con 3 partidas o más. Sin ninguna todavía,
 * una frase que dice cuándo aparecen. No se tocan: Avance mira, Hoy juega.
 */
@Composable
internal fun AvanceBestMarks(marks: List<BestMark>, now: Long) {
  Column {
    SpaceSectionTitle("Tus mejores marcas", hint = "Cada una con la unidad de su juego, comparada solo contigo", fontFamily = TitleFamily)
    Spacer(Modifier.height(8.dp))
    if (marks.isEmpty()) {
      OnBoardText("Cuando juegues tres partidas de un mismo juego, aquí aparece tu mejor marca.", color = OnNightDim, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.testTag("best_marks_empty"))
      return@Column
    }
    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
      marks.forEach { m ->
        ClayCard(Modifier.fillMaxWidth().testTag("best_mark_${m.def.key}"), contentPadding = 0.dp) {
          Row(Modifier.fillMaxWidth().padding(horizontal = 14.dp, vertical = 12.dp), verticalAlignment = Alignment.CenterVertically) {
            GameEmblem(m.def.gameId, 48.dp)
            Spacer(Modifier.width(12.dp))
            Column(Modifier.weight(1f)) {
              Text(MeasurePhrases.recordLabel(m.def), color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp)
              Text(MeasurePhrases.value(m.def, m.value), color = Clay.Ink, fontSize = 16.sp, lineHeight = 21.sp)
              Text("Lo lograste " + MeasurePhrases.ago(daysBetween(m.timestamp, now)), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.testTag("best_mark_when_${m.def.key}"))
            }
          }
        }
      }
    }
  }
}

/** Días de calendario entre [then] y [now] (hoy = 0, ayer = 1), en la zona de la persona. */
internal fun daysBetween(then: Long, now: Long): Int {
  fun day(ts: Long) = (ts + java.util.TimeZone.getDefault().getOffset(ts)) / 86_400_000L
  return (day(now) - day(then)).toInt().coerceAtLeast(0)
}
