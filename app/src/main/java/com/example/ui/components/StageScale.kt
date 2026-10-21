package com.example.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.height
import androidx.compose.material3.LocalTextStyle
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.rememberTextMeasurer
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Skill
import com.example.ui.theme.Clay
import com.example.ui.theme.NubiTokens

/**
 * La escala de las 5 etapas (Inicio · Aprendiz · Hábil · Experto · Maestro): UN solo dibujo para la lámina de un lugar de Hoy, el detalle de un área en Avance y la ficha de un juego (Tarea 143, ítems 1, 8 y 12 de la revisión de Fable del 19-oct;
 * antes eran tres: una barra segmentada lila, una línea de puntos con triángulo y segmentos con bolita sol y halo). Arriba, la barra del kit ([KitBar]: canal y churro, con la bolita de dónde estabas hace una semana si hubo [change]) con cuatro marcas que la parten en
 * sus 5 etapas; debajo, los nombres a 14 sp (la escala es un rótulo de gráfico), el de tu etapa en negrita. [onCard] = sobre una tarjeta crema (tinta) o sobre el cielo (blanco). Sin medir ([value] nulo) solo se ve el canal.
 *
 * Una palabra NUNCA se parte («Aprendi / z»): si los cinco nombres no caben cada uno en su casilla (pantalla angosta) o la letra del sistema pasa de 1,15, en vez de las cinco columnas va UNA frase que sí se puede partir entre palabras:
 * «Etapas: Inicio · Aprendiz · Hábil · Experto · Maestro. Vas en Hábil».
 */
@Composable
fun StageScale(
  value: Float?,
  onCard: Boolean,
  modifier: Modifier = Modifier,
  change: Float = 0f,
  barTag: String = "stage_bar",
  namesTag: String = "stage_names",
  barModifier: Modifier = Modifier,
  /** false = solo la barra, sin los nombres de las 5 etapas (la lámina del lugar los cede cuando no cabe: Tarea 147). */
  showNames: Boolean = true
) {
  val current = value?.let { Skill.stage(it) }
  val on = if (onCard) Clay.Ink else Color.White
  val off = if (onCard) Clay.InkMuted else NubiTokens.OnNightDim
  val tick = if (onCard) Clay.Ink.copy(alpha = 0.32f) else Color.White.copy(alpha = 0.32f)
  Column(modifier.fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(4.dp)) {
    Box(barModifier.fillMaxWidth().testTag(barTag)) {
      KitBar(value, change, onCard = onCard)
      Canvas(Modifier.matchParentSize()) {
        val inset = 5.dp.toPx()
        for (k in 1..4) {
          val x = size.width * k / 5f
          drawLine(tick, Offset(x, inset), Offset(x, size.height - inset), 1.5.dp.toPx())
        }
      }
    }
    if (!showNames) return@Column
    val measurer = rememberTextMeasurer()
    val base = LocalTextStyle.current.merge(TextStyle(fontSize = 14.sp))
    val fontScale = LocalDensity.current.fontScale
    BoxWithConstraints(Modifier.fillMaxWidth().testTag(namesTag)) {
      val cellPx = constraints.maxWidth / Skill.STAGES.size
      val fits = fontScale <= 1.15f && Skill.STAGES.withIndex().all { (i, name) ->
        val style = base.merge(TextStyle(fontWeight = if (i == current) FontWeight.Bold else FontWeight.Normal))
        measurer.measure(name, style, softWrap = false, maxLines = 1).size.width <= cellPx * 0.96f
      }
      if (fits) {
        Row(Modifier.fillMaxWidth()) {
          Skill.STAGES.forEachIndexed { i, name ->
            val here = i == current
            ScaleText(
              onCard, name, color = if (here) on else off, fontWeight = if (here) FontWeight.Bold else FontWeight.Normal,
              textAlign = TextAlign.Center, softWrap = false, maxLines = 1, modifier = Modifier.weight(1f)
            )
          }
        }
      } else {
        ScaleText(onCard, "Etapas: " + Skill.STAGES.joinToString(" · ") + (current?.let { ". Vas en ${Skill.STAGES[it]}" } ?: ""), color = off, lineHeight = 19.sp)
      }
    }
  }
}

/** Un rótulo de la escala: sobre una tarjeta, `Text`; sobre el cielo, `OnBoardText` (con el contorno que lo separa de los luceros del fondo). 14 sp siempre. */
@Composable
private fun ScaleText(
  onCard: Boolean, text: String, color: Color, modifier: Modifier = Modifier, fontWeight: FontWeight? = null, textAlign: TextAlign? = null,
  softWrap: Boolean = true, maxLines: Int = Int.MAX_VALUE, lineHeight: androidx.compose.ui.unit.TextUnit = androidx.compose.ui.unit.TextUnit.Unspecified
) {
  if (onCard) Text(text, modifier, color = color, fontSize = 14.sp, fontWeight = fontWeight, textAlign = textAlign, softWrap = softWrap, maxLines = maxLines, lineHeight = lineHeight)
  else OnBoardText(text, modifier, color = color, fontSize = 14.sp, fontWeight = fontWeight, textAlign = textAlign, softWrap = softWrap, maxLines = maxLines, lineHeight = lineHeight)
}

/**
 * Las últimas 4 semanas del avance de un área sobre la lámina crema (Tarea 155; antes `WeeksChart` del detalle de Avance, sobre el cielo): los números arriba, una línea lavanda con sus puntos (el de esta semana en sol, con borde de tinta) y los rótulos
 * «hace 3 sem. · hace 2 sem. · sem. pasada · esta sem.» abajo. Todo a 14 sp (rótulos de un gráfico). [weeks] = de 0 a 100, la última = esta semana; un null deja un hueco. Mide ~100 dp.
 */
@Composable
fun WeeksChartOnCard(weeks: List<Int?>, modifier: Modifier = Modifier) {
  val known = weeks.filterNotNull()
  val lo = (known.minOrNull() ?: 0) - 4f
  val hi = (known.maxOrNull() ?: 0) + 4f
  val line = Color(0xFF7B6BD6)
  Column(modifier.fillMaxWidth().testTag("place_chart")) {
    Row(Modifier.fillMaxWidth()) {
      weeks.forEachIndexed { i, v ->
        Text(
          v?.toString() ?: "", fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.weight(1f),
          color = if (i == weeks.lastIndex) Clay.Ink else Clay.InkMuted, fontWeight = if (i == weeks.lastIndex) FontWeight.Bold else FontWeight.Normal
        )
      }
    }
    Canvas(Modifier.fillMaxWidth().height(56.dp).padding(vertical = 6.dp)) {
      fun p(i: Int, v: Int) = Offset(size.width * (i + 0.5f) / weeks.size, size.height * (1f - (v - lo) / (hi - lo)))
      val pts = weeks.mapIndexedNotNull { i, v -> v?.let { p(i, it) } }
      val path = androidx.compose.ui.graphics.Path().apply { pts.forEachIndexed { i, o -> if (i == 0) moveTo(o.x, o.y) else lineTo(o.x, o.y) } }
      drawPath(path, line, style = androidx.compose.ui.graphics.drawscope.Stroke(2.5.dp.toPx(), cap = androidx.compose.ui.graphics.StrokeCap.Round, join = androidx.compose.ui.graphics.StrokeJoin.Round))
      pts.forEachIndexed { i, o ->
        val last = i == pts.lastIndex
        drawCircle(Clay.Ink, (if (last) 6 else 5).dp.toPx(), o)
        drawCircle(if (last) Clay.Sun else line, (if (last) 4.5f else 3.5f).dp.toPx(), o)
      }
    }
    Row(Modifier.fillMaxWidth()) {
      listOf("hace 3 sem.", "hace 2 sem.", "sem. pasada", "esta sem.").forEachIndexed { i, label ->
        Text(label, fontSize = 14.sp, textAlign = TextAlign.Center, color = if (i == 3) Clay.Ink else Clay.InkMuted, modifier = Modifier.weight(1f))          // con letra grande pasa a dos líneas («hace 3 / sem.»), nunca se pisa ni se parte una palabra
      }
    }
  }
}
