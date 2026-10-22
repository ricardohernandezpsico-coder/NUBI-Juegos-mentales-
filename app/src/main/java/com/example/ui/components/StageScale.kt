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
  showNames: Boolean = true,
  /** El tamaño de los nombres de las etapas: 14 sp en las láminas (rótulo de gráfico); 16 sp en la página de un área (Tarea 159: nada bajo 16 sp). */
  labelSp: Int = 14
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
    val base = LocalTextStyle.current.merge(TextStyle(fontSize = labelSp.sp))
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
              textAlign = TextAlign.Center, softWrap = false, maxLines = 1, modifier = Modifier.weight(1f), size = labelSp.sp
            )
          }
        }
      } else {
        ScaleText(onCard, "Etapas: " + Skill.STAGES.joinToString(" · ") + (current?.let { ". Vas en ${Skill.STAGES[it]}" } ?: ""), color = off, lineHeight = (labelSp + 5).sp, size = labelSp.sp)
      }
    }
  }
}

/** Un rótulo de la escala: sobre una tarjeta, `Text`; sobre el cielo, `OnBoardText` (con el contorno que lo separa de los luceros del fondo). [size] = 14 sp (las láminas) o 16 sp (la página de un área). */
@Composable
private fun ScaleText(
  onCard: Boolean, text: String, color: Color, modifier: Modifier = Modifier, fontWeight: FontWeight? = null, textAlign: TextAlign? = null,
  softWrap: Boolean = true, maxLines: Int = Int.MAX_VALUE, lineHeight: androidx.compose.ui.unit.TextUnit = androidx.compose.ui.unit.TextUnit.Unspecified,
  size: androidx.compose.ui.unit.TextUnit = 14.sp
) {
  if (onCard) Text(text, modifier, color = color, fontSize = size, fontWeight = fontWeight, textAlign = textAlign, softWrap = softWrap, maxLines = maxLines, lineHeight = lineHeight)
  else OnBoardText(text, modifier, color = color, fontSize = size, fontWeight = fontWeight, textAlign = textAlign, softWrap = softWrap, maxLines = maxLines, lineHeight = lineHeight)
}
