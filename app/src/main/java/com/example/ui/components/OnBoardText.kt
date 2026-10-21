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
import androidx.compose.ui.graphics.StrokeCap
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
  /**
   * El color del contorno: el de la lámina en su zona más común, MEDIDO sobre el render nuevo (mediana de `fondo_lamina` «Espacio profundo»: 21, 21, 57 = `#151539`), así sobre el fondo liso casi no se nota; la lámina va de ~#0F0E2E (pie) a ~#181739 (arriba).
   * Si cambia la lámina, esto cambia con ella (`BoardBackgroundContrastTest` lo mide): un contorno más claro que el fondo deja cada letra con un aura. Contra una letra clara (luminancia 0,65-0,9) sigue dando 13:1 o más.
   */
  val Tone: Color = Color(0xFF151539)
}

/** Lo mismo con un texto de varios estilos (una palabra en sol dentro de una frase): el contorno se dibuja con el mismo diseño del texto. */
@Composable
fun OnBoardText(
  text: androidx.compose.ui.text.AnnotatedString,
  modifier: Modifier = Modifier,
  color: Color = Color.Unspecified,
  fontSize: TextUnit = TextUnit.Unspecified,
  fontWeight: FontWeight? = null,
  fontFamily: FontFamily? = null,
  textAlign: TextAlign? = null,
  lineHeight: TextUnit = TextUnit.Unspecified
) {
  var layout by remember { mutableStateOf<TextLayoutResult?>(null) }
  val strokePx = with(LocalDensity.current) { OnBoardHalo.Outline.toPx() }
  Text(
    text = text,
    modifier = modifier.drawBehind {
      if (strokePx > 0f) layout?.let { drawText(it, color = OnBoardHalo.Tone, drawStyle = Stroke(width = strokePx, cap = StrokeCap.Round, join = StrokeJoin.Round)) }
    },
    color = color, fontSize = fontSize, fontWeight = fontWeight, fontFamily = fontFamily, textAlign = textAlign, lineHeight = lineHeight,
    onTextLayout = { layout = it }
  )
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
      if (strokePx > 0f) layout?.let { drawText(it, color = OnBoardHalo.Tone, drawStyle = Stroke(width = strokePx, cap = StrokeCap.Round, join = StrokeJoin.Round)) }
    },
    color = color, fontSize = fontSize, fontStyle = fontStyle, fontWeight = fontWeight, fontFamily = fontFamily, letterSpacing = letterSpacing,
    textDecoration = textDecoration, textAlign = textAlign, lineHeight = lineHeight, overflow = overflow, softWrap = softWrap, maxLines = maxLines, minLines = minLines,
    onTextLayout = { layout = it; onTextLayout?.invoke(it) }, style = style
  )
}
