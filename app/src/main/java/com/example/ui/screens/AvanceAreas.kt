package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
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
import com.example.ui.components.StageScale
import com.example.ui.components.WeekLampRendered
import com.example.ui.components.areaDescription
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayCard

private val OnNight = androidx.compose.ui.graphics.Color(0xFFEAF0FF)
private val OnNightDim = com.example.ui.theme.NubiTokens.OnNightDim

/**
 * Avance con PUNTOS por área (Tarea 159; Ricardo, 22-oct: «con puntos por área»; `auditoria-fable/avance-b-puntos.md` §3): el título, UNA línea de la semana, cuatro filas de área a todo el ancho con sus puntos y «Tus mejores marcas». Lo largo de la semana
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

/**
 * Tus áreas: cuatro filas a todo el ancho (la fila entera se toca y abre la página del área; alto de 110 dp o más). Cada una: el nombre con el punto del color del área, «Etapa Hábil» debajo, a la derecha los puntos grandes («470») con «de 1000 puntos», la barra de las 5 etapas
 * (lo ganado esta semana, más claro) y la frase de la semana («Esta semana sumaste 12 puntos.» / «Esta semana te mantuviste.» / «Esta semana bajaste 8 puntos. Tu mejor marca sigue en 520 puntos.»). Con letra de 1,3 o más los puntos bajan bajo el nombre.
 */
@Composable
internal fun AvanceAreaRows(areas: List<AreaPoints>, names: Map<String, String>, onArea: (String) -> Unit) {
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  val density = LocalDensity.current
  Column {
    SpaceSectionTitle("Tus áreas", hint = "Toca un área para ver sus juegos y tus marcas")
    Spacer(Modifier.height(8.dp))
    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
      areas.forEach { p ->
        val key = p.area.lowercase()
        val name = names[p.area] ?: p.area
        ClayCard(
          modifier = Modifier.fillMaxWidth().heightIn(min = 110.dp).semantics(mergeDescendants = true) { contentDescription = areaDescription(name, p) }.testTag("progress_area_$key"),
          onClick = { onArea(p.area) }, contentPadding = 0.dp
        ) {
          androidx.compose.foundation.layout.BoxWithConstraints(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 12.dp)) {
            // Los puntos van al lado del nombre solo si caben los dos ENTEROS (nombre a 20 sp, cifra de 34 sp y «de 1000 puntos»); si no, bajan bajo el nombre: ninguna palabra se parte (Razonamiento a 1,3)
            fun px(text: String, sp: Int, bold: Boolean) = measurer.measure(text, base.merge(androidx.compose.ui.text.TextStyle(fontSize = sp.sp, fontWeight = if (bold) FontWeight.Bold else FontWeight.Normal)), softWrap = false, maxLines = 1).size.width
            val dotPx = with(density) { 20.dp.toPx() }
            val pointsPx = maxOf(px(p.total.toString(), 34, true), px("de ${AreaPoints.SCALE} puntos", 16, false))
            val stacked = px(name, 20, true) + dotPx + with(density) { 8.dp.toPx() } + pointsPx > constraints.maxWidth
            // el nombre baja de 20 a 16 sp si ni solo cabe en su línea (letra muy grande en un teléfono angosto)
            val nameSp = (20 downTo 16).firstOrNull { px(name, it, true) + dotPx <= constraints.maxWidth } ?: 16
            Column(Modifier.fillMaxWidth()) {
            @Composable
            fun NameAndStage(m: Modifier) = Column(m) {
              Row(verticalAlignment = Alignment.CenterVertically) {
                AreaDot(p.area)
                Spacer(Modifier.width(8.dp))
                Text(name, color = Clay.Ink, fontSize = nameSp.sp, fontWeight = FontWeight.Bold, lineHeight = (nameSp + 4).sp, modifier = Modifier.testTag("area_name_$key"))
              }
              Text(if (p.measured) "Etapa ${p.stageName}" else "Aún sin medir", color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.testTag("area_stage_$key"))
            }
            @Composable
            fun Points(m: Modifier, end: Boolean) = Column(m, horizontalAlignment = if (end) Alignment.End else Alignment.Start) {
              Text(p.total.toString(), color = Clay.Ink, fontSize = 34.sp, fontWeight = FontWeight.Bold, lineHeight = 36.sp, modifier = Modifier.testTag("area_points_$key"))
              Text("de ${AreaPoints.SCALE} puntos", color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 20.sp)
            }
            if (stacked) { NameAndStage(Modifier); Points(Modifier.padding(top = 4.dp), end = false) }
            else Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.Top) { NameAndStage(Modifier.weight(1f)); Spacer(Modifier.width(8.dp)); Points(Modifier, end = true) }
            StageScale(if (p.measured) p.fill else null, onCard = true, modifier = Modifier.padding(top = 8.dp), change = p.change, barTag = "area_bar_$key", showNames = false)
            Text(AreaPoints.weekLine(p, name), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 6.dp).testTag("area_line_$key"))
            }
          }
        }
      }
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
    SpaceSectionTitle("Tus mejores marcas", hint = "Cada una con la unidad de su juego, comparada solo contigo")
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
