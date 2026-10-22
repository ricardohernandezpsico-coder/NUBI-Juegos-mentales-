package com.example.ui.screens

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyListState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.AreaPoints
import com.example.data.GamePoints
import com.example.data.LevelLines
import com.example.data.MeasurePoint
import com.example.data.StarMeasures
import com.example.model.GameRegistry
import com.example.ui.components.GameEmblem
import com.example.ui.components.OnBoardText
import com.example.ui.components.SpaceSectionTitle
import com.example.ui.components.StageScale
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayCard

private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = com.example.ui.theme.NubiTokens.OnNightDim
private val ChartLine = Color(0xFF7B6BD6)

/**
 * La PÁGINA de un área (Tarea 159; `auditoria-fable/avance-b-puntos.md` §4): lo que se abre al tocar una fila de Avance. De arriba abajo: «‹ Avance» y el nombre del área; los puntos y la etapa; la barra de las 5 etapas con sus nombres (o en una frase con letra grande);
 * lo que falta y lo que sumaste esta semana; los puntos de las últimas 8 semanas (con menos de 4 semanas, una frase que dice cuándo aparece el gráfico); una tarjeta por juego del área con sus puntos, su nivel en palabras («Ya repites 6 luces seguidas»), su medida y cuándo se jugó;
 * «Qué pones en juego» y la nota honesta. SIN «Jugar»: Avance mira, Hoy juega. Atrás (o «‹ Avance») vuelve a la lista de áreas, donde estaba. Todo a 16 sp o más, con sus unidades y sin palabras sueltas.
 * [lastPlayed] = cuándo se jugó cada juego (de `game_history`, no del registro de avance, que solo anota cuando el avance cambia); [measures] = las medidas de los juegos estrella.
 */
@Composable
internal fun AvanceAreaPage(
  points: AreaPoints,
  weekly: List<Int>,
  areaName: String,
  lastPlayed: Map<String, Long>,
  measures: List<MeasurePoint>,
  lang: com.example.model.AppLanguage,
  now: Long,
  onBack: () -> Unit,
  modifier: Modifier = Modifier,
  listState: LazyListState = androidx.compose.foundation.lazy.rememberLazyListState(),
  /** Si la persona juega ese juego con reloj (la ficha lo recuerda por juego): Freno de Emergencia solo dice su plazo CON reloj. */
  timedFor: (String) -> Boolean = { false }
) {
  val key = points.area.lowercase()
  Column(modifier.fillMaxSize().testTag("avance_area_page_$key")) {
    BackRow(onBack)
    LazyColumn(
      state = listState,
      modifier = Modifier.weight(1f).padding(horizontal = 22.dp),
      contentPadding = PaddingValues(top = 4.dp, bottom = 28.dp),
      verticalArrangement = Arrangement.spacedBy(18.dp)
    ) {
      item { PageHeader(points, areaName) }
      item {
        Column {
          StageScale(if (points.measured) points.fill else null, onCard = false, change = points.change, barTag = "page_bar", namesTag = "page_stages", labelSp = 16)
          OnBoardText(AreaPoints.toNextSentence(points, areaName), color = OnNight, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 10.dp).testTag("page_to_next"))
        }
      }
      item { WeeksSection(weekly, points, areaName) }
      item {
        Column {
          SpaceSectionTitle("Tus juegos de $areaName", hint = AreaPoints.gamesHint(points.maxPerGame))
          Spacer(Modifier.height(8.dp))
          Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
            points.perGame.forEach { g -> GameCard(g, lastPlayed[g.gameId], measures, lang, now, timedFor(g.gameId)) }
          }
        }
      }
      item {
        Column {
          SpaceSectionTitle("Qué pones en juego en $areaName")
          OnBoardText(whatYouPlay(points.area), color = OnNight, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 8.dp).testTag("page_what"))
        }
      }
      item { OnBoardText(HONEST_NOTE, color = OnNightDim, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("page_note")) }
    }
  }
}

/** La nota honesta del final de la página (Fable §4.8): lo que dicen los puntos y lo que no. */
internal const val HONEST_NOTE = "Estos puntos dicen cómo te va en estos juegos, comparados solo contigo. Mejorar aquí es mejorar aquí: no es un diagnóstico."

/** «Qué pones en juego» de cada área, en segunda persona y sin promesas (Fable §4.7): lo que mide cada juego del área. */
internal fun whatYouPlay(areaKey: String): String = when (areaKey) {
  "MEMORIA" -> "Recordar dónde estaba cada cosa, repetir un camino en orden y acordarte de hacer algo a su hora."
  "ATENCION" -> "Ver algo de un vistazo, seguir varias cosas a la vez, no dejarte llevar por la palabra, atender dos tareas juntas y frenar a tiempo."
  "RAZONAMIENTO" -> "Girar una pieza en la mente, armar una cifra con operaciones, ver cómo encajan los engranajes y ubicar un número en una regla."
  else -> "Encontrar la palabra que ya sabes, reconocer palabras, leer y decidir rápido y ver qué une a cuatro palabras."
} + " Cada juego mide una de esas cosas; los puntos suben cuando llegas más lejos en cada uno."

/** «‹ Avance»: vuelve a la lista de áreas. Alto de 48 dp o más, con el botón entero como zona de toque. */
@Composable
private fun BackRow(onBack: () -> Unit) {
  Row(
    Modifier.padding(start = 12.dp, top = 4.dp).heightIn(min = 48.dp).clip(RoundedCornerShape(12.dp)).clickable(role = Role.Button, onClickLabel = "Volver a Avance", onClick = onBack).padding(horizontal = 10.dp).testTag("area_back"),
    verticalAlignment = Alignment.CenterVertically
  ) {
    Canvas(Modifier.size(width = 10.dp, height = 18.dp)) {
      val p = Path().apply { moveTo(size.width * 0.85f, size.height * 0.1f); lineTo(size.width * 0.15f, size.height * 0.5f); lineTo(size.width * 0.85f, size.height * 0.9f) }
      drawPath(p, OnNight, style = Stroke(2.2.dp.toPx(), cap = StrokeCap.Round, join = StrokeJoin.Round))
    }
    Spacer(Modifier.width(8.dp))
    OnBoardText("Avance", color = OnNight, fontSize = 18.sp, fontWeight = FontWeight.SemiBold)
  }
}

/** El nombre del área (con su punto de color) y «470 puntos de 1000 · Etapa Hábil» (con letra grande, en dos líneas). */
@Composable
private fun PageHeader(p: AreaPoints, areaName: String) {
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  Column(Modifier.testTag("page_header")) {
    // el nombre baja de 28 a 20 sp si no cabe entero en su línea (letra 2,0): una palabra nunca se parte
    androidx.compose.foundation.layout.BoxWithConstraints(Modifier.fillMaxWidth()) {
      val room = constraints.maxWidth - with(LocalDensity.current) { 22.dp.toPx() }
      val sp = (28 downTo 20 step 2).firstOrNull { measurer.measure(areaName, base.merge(androidx.compose.ui.text.TextStyle(fontSize = it.sp, fontWeight = FontWeight.Bold)), softWrap = false, maxLines = 1).size.width <= room } ?: 20
      Row(verticalAlignment = Alignment.CenterVertically) {
        AreaDot(p.area)
        Spacer(Modifier.width(10.dp))
        OnBoardText(areaName, color = OnNight, fontSize = sp.sp, fontWeight = FontWeight.Bold, lineHeight = (sp + 4).sp, modifier = Modifier.semantics { heading() }.testTag("page_title"))
      }
    }
    val stage = if (p.measured) "Etapa ${p.stageName}" else "Aún sin medir"
    val total = "${p.total} puntos de ${AreaPoints.SCALE}"
    if (LocalDensity.current.fontScale > 1.15f) {
      OnBoardText(total, color = OnNight, fontSize = 20.sp, fontWeight = FontWeight.Bold, lineHeight = 24.sp, modifier = Modifier.padding(top = 6.dp).testTag("page_points"))
      OnBoardText(stage, color = OnNight, fontSize = 20.sp, fontWeight = FontWeight.Bold, lineHeight = 24.sp, modifier = Modifier.testTag("page_stage"))
    } else {
      OnBoardText("$total · $stage", color = OnNight, fontSize = 20.sp, fontWeight = FontWeight.Bold, lineHeight = 24.sp, modifier = Modifier.padding(top = 6.dp).testTag("page_points"))
    }
  }
}

/** Los puntos de las últimas semanas: el gráfico (con 4 o más semanas) o la frase que dice cuándo aparece; y, si hubo bajadas en el mes, la línea fija que las explica. */
@Composable
private fun WeeksSection(weekly: List<Int>, p: AreaPoints, areaName: String) {
  Column {
    if (weekly.size >= AreaPoints.CHART_MIN_WEEKS) {
      WeeksChart(weekly)
      if (p.belowBest || weekly.zipWithNext().takeLast(4).any { (a, b) -> b < a }) {
        OnBoardText(AreaPoints.UPS_AND_DOWNS, color = OnNightDim, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 8.dp).testTag("page_ups_downs"))
      }
    } else {
      OnBoardText(AreaPoints.weeklyLine(weekly.size, areaName), color = OnNight, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.testTag("page_weeks_empty"))
    }
  }
}

/**
 * «Tus puntos en las últimas 8 semanas»: una línea con un punto por semana (el de esta semana en sol), la cifra con su unidad en el primero y en el último («380 puntos», «470 puntos») y el eje en palabras («hace 7 semanas», «esta semana»). Sobre una tarjeta crema, 16 sp.
 */
@Composable
private fun WeeksChart(values: List<Int>) {
  val n = values.size
  ClayCard(Modifier.fillMaxWidth().testTag("page_chart"), contentPadding = 14.dp) {
    Text("Tus puntos en las últimas $n semanas", color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.semantics { heading() })
    val lo = values.min().toFloat(); val hi = values.max().toFloat()
    val span = maxOf(hi - lo, 20f)
    // El espacio de las dos cifras crece con la letra del sistema (a 2,0 una cifra de 16 sp mide ~42 dp): la línea nunca las pisa
    val fontScale = LocalDensity.current.fontScale
    val labelH = (16f * 1.3f * fontScale).dp
    BoxWithConstraints(Modifier.fillMaxWidth().height(labelH * 2 + 100.dp).padding(top = 6.dp)) {
      Canvas(Modifier.fillMaxSize().padding(top = labelH + 6.dp, bottom = labelH + 6.dp, start = 8.dp, end = 8.dp)) {
        fun pt(i: Int, v: Int) = Offset(size.width * (if (n == 1) 0.5f else i / (n - 1f)), size.height * (1f - (v - lo) / span))
        val path = Path().apply { values.forEachIndexed { i, v -> val o = pt(i, v); if (i == 0) moveTo(o.x, o.y) else lineTo(o.x, o.y) } }
        drawPath(path, ChartLine, style = Stroke(2.5.dp.toPx(), cap = StrokeCap.Round, join = StrokeJoin.Round))
        values.forEachIndexed { i, v ->
          val o = pt(i, v); val last = i == n - 1
          drawCircle(Clay.Ink, (if (last) 6 else 5).dp.toPx(), o)
          drawCircle(if (last) Clay.Sun else ChartLine, (if (last) 4.5f else 3.5f).dp.toPx(), o)
        }
      }
      Text("${values.last()} puntos", color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, modifier = Modifier.align(Alignment.TopEnd).testTag("chart_last"))
      Text("${values.first()} puntos", color = Clay.InkMuted, fontSize = 16.sp, modifier = Modifier.align(Alignment.BottomStart).padding(bottom = 2.dp).testTag("chart_first"))
    }
    val first = if (n - 1 == 1) "la semana pasada" else "hace ${n - 1} semanas"
    if (fontScale > 1.3f) {
      // con letra grande los dos rótulos del eje no caben lado a lado sin partirse: uno bajo el otro
      Text(first, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.testTag("chart_axis_first"))
      Text("esta semana", color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 20.sp, modifier = Modifier.align(Alignment.End).testTag("chart_axis_last"))
    } else Row(Modifier.fillMaxWidth()) {
      Text(first, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.weight(1f).testTag("chart_axis_first"))
      Text("esta semana", color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 20.sp, modifier = Modifier.padding(start = 8.dp).testTag("chart_axis_last"))
    }
  }
}

/**
 * La tarjeta de un juego del área: el emblema; el nombre; «180 de 250 puntos»; su nivel en palabras («Ya repites 6 luces seguidas»); la medida propia en una frase («Hoy: 5 luces seguidas. Tu mejor marca: 7 luces.»; «En tu última partida: …» si no fue hoy); y cuándo se jugó.
 * Sin partidas: «Todavía sin partidas · aporta hasta 250 puntos». Sin «Jugar» (la página mira, Hoy juega).
 */
@Composable
private fun GameCard(g: GamePoints, last: Long?, measures: List<MeasurePoint>, lang: com.example.model.AppLanguage, now: Long, timed: Boolean) {
  val game = GameRegistry.getById(g.gameId) ?: return
  val title = getGameTitle(game.id, lang, game.title)
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  val large = LocalDensity.current.fontScale > 1.3f
  ClayCard(Modifier.fillMaxWidth().heightIn(min = 100.dp).testTag("page_game_${g.gameId}"), contentPadding = 0.dp) {
    androidx.compose.foundation.layout.BoxWithConstraints(Modifier.fillMaxWidth().padding(horizontal = 14.dp, vertical = 12.dp)) {
      // con letra grande el emblema se achica (56 → 40 dp) para dejarle sitio al texto, y el nombre baja de 18 a 16 sp si su palabra más larga no cabe: ninguna palabra se parte
      val emblem = if (large) 40.dp else 56.dp
      val room = constraints.maxWidth - with(LocalDensity.current) { (emblem + 12.dp).toPx() }
      val longest = title.split(' ').maxByOrNull { it.length } ?: title
      val titleSp = (18 downTo 16).firstOrNull { measurer.measure(longest, base.merge(androidx.compose.ui.text.TextStyle(fontSize = it.sp, fontWeight = FontWeight.Bold)), softWrap = false, maxLines = 1).size.width <= room } ?: 16
    Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.Top) {
      GameEmblem(g.gameId, emblem)
      Spacer(Modifier.width(12.dp))
      Column(Modifier.weight(1f)) {
        Text(title, color = Clay.Ink, fontSize = titleSp.sp, fontWeight = FontWeight.Bold, lineHeight = (titleSp + 4).sp, modifier = Modifier.testTag("page_game_name_${g.gameId}"))
        val measure = StarMeasures.sentence(g.gameId, measures, now)
        when {
          g.measured -> {
            Text("${g.points} de ${g.max} puntos", color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.testTag("page_game_points_${g.gameId}"))
            // si la medida del juego ya dice lo mismo en la misma unidad (luces seguidas, satélites a la vez, el destello), solo va la medida: no conviven dos cifras de lo mismo
            if (measure == null || !LevelLines.overlapsMeasure(g.gameId)) Text(LevelLines.line(g.gameId, g.progress, timed), color = Clay.Ink, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.testTag("page_game_level_${g.gameId}"))
          }
          last == null -> Text("Todavía sin partidas · aporta hasta ${g.max} puntos", color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.testTag("page_game_empty_${g.gameId}"))
          else -> {
            Text("0 de ${g.max} puntos", color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.testTag("page_game_points_${g.gameId}"))
            Text("Aún sin medir: juega a tu medida y empieza a sumar.", color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp)
          }
        }
        measure?.let { Text(it, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.testTag("page_game_measure_${g.gameId}")) }
        if (last != null) {
          val days = daysBetween(last, now)
          Text(when (days) { 0 -> "Jugado hoy"; 1 -> "Jugado ayer"; else -> "Jugado hace $days días" }, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.testTag("page_game_when_${g.gameId}"))
        }
      }
    }
    }
  }
}
