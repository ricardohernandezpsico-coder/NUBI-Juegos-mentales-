package com.example.ui.components

import androidx.compose.material3.LocalTextStyle
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.drawText
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextDecoration
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp

/**
 * La orilla navy de los textos claros que van DIRECTO sobre el fondo (Tarea 87): como los luceros del fondo son fijos detrás de las tres pestañas, uno puede quedar detrás de un texto (al desplazar una pantalla o al deslizar entre pestañas).
 * Una sombra desenfocada NO alcanza (medido: contra una bolita de plasticina deja la letra a 1,0-1,9:1 aunque sea de 7 dp y casi opaca; la bolita es tan clara como el texto): hace falta una orilla DENSA. Por eso cada letra lleva un contorno
 * del azul del fondo, de [Outline] de ancho (la mitad hacia afuera), pintado DETRÁS con el mismo trazado del texto. Sobre el fondo liso no se nota (es del mismo azul) y sobre una bolita deja cada letra con su borde oscuro: ≥ 4,5:1.
 * En crema o sobre tarjetas NO se usa: ahí va `Text` normal.
 */
object OnBoardHalo {
  /** Ancho del contorno (1,75 dp hacia afuera de cada trazo). */
  val Outline: Dp = 3.5.dp
  /** El azul marino del tablero. */
  val Tone: Color = BoardNavy
}

/**
 * `Text` con la orilla navy de [OnBoardHalo]: el estilo común de todo texto claro que va directo sobre el fondo (saludo de Hoy, títulos y cifras de Juegos y Avance, etiquetas de la barra). Mismos parámetros que `Text`.
 * El contorno se dibuja con el mismo `TextLayoutResult` del texto (`drawBehind`): el texto sigue siendo UN solo nodo, con su medida y su semántica de siempre.
 */
@Composable
fun OnBoardText(
  text: String,
  modifier: Modifier = Modifier,
  color: Color = Color.Unspecified,
  fontSize: TextUnit = TextUnit.Unspecified,
  fontStyle: FontStyle? = null,
  fontWeight: FontWeight? = null,
  fontFamily: FontFamily? = null,
  letterSpacing: TextUnit = TextUnit.Unspecified,
  textDecoration: TextDecoration? = null,
  textAlign: TextAlign? = null,
  lineHeight: TextUnit = TextUnit.Unspecified,
  overflow: TextOverflow = TextOverflow.Clip,
  softWrap: Boolean = true,
  maxLines: Int = Int.MAX_VALUE,
  minLines: Int = 1,
  onTextLayout: ((TextLayoutResult) -> Unit)? = null,
  style: TextStyle = LocalTextStyle.current
) {
  var layout by remember { mutableStateOf<TextLayoutResult?>(null) }
  val strokePx = with(LocalDensity.current) { OnBoardHalo.Outline.toPx() }
  Text(
    text = text,
    modifier = modifier.drawBehind {
      layout?.let { drawText(it, color = OnBoardHalo.Tone, drawStyle = Stroke(width = strokePx, join = StrokeJoin.Round)) }
    },
    color = color, fontSize = fontSize, fontStyle = fontStyle, fontWeight = fontWeight, fontFamily = fontFamily, letterSpacing = letterSpacing,
    textDecoration = textDecoration, textAlign = textAlign, lineHeight = lineHeight, overflow = overflow, softWrap = softWrap, maxLines = maxLines, minLines = minLines,
    onTextLayout = { layout = it; onTextLayout?.invoke(it) }, style = style
  )
}
