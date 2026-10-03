package com.example.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.clipPath
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.FigContour
import com.example.data.FigDetail
import com.example.data.FigPt
import com.example.data.Figure
import com.example.ui.theme.AppFamily
import com.example.ui.theme.Clay

private val Gold = Color(0xFFE9C77B)
private val LineGold = Color(0xFFFFD98A)
private val StarCream = Color(0xFFFFECC8)

/**
 * Una lámina del atlas de "La estrella intrusa" en miniatura: las estrellas de la figura, las líneas de luz, el grabado dorado con su
 * sombreado y los detalles. Sin recuadro: va suelta sobre el cielo de la pantalla final. [name] va debajo, en cursiva como el grabado.
 */
@Composable
fun AtlasThumb(figure: Figure, modifier: Modifier = Modifier, size: androidx.compose.ui.unit.Dp = 92.dp, showName: Boolean = true) {
  Column(modifier, horizontalAlignment = Alignment.CenterHorizontally) {
    Canvas(Modifier.size(size).semantics { contentDescription = "Lámina: ${figure.name}" }) { drawFigure(figure) }
    if (showName) {
      Text(
        figure.name, color = Color(0xFFFFE3A3), fontSize = 15.sp, fontWeight = FontWeight.SemiBold, fontFamily = AppFamily,
        textAlign = TextAlign.Center, modifier = Modifier.padding(top = 2.dp)
      )
    }
  }
}

/** Hasta tres láminas en fila (las ganadas hoy). */
@Composable
fun AtlasThumbRow(figures: List<Figure>, modifier: Modifier = Modifier) {
  Row(modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceEvenly, verticalAlignment = Alignment.Top) {
    figures.forEach { AtlasThumb(it, Modifier.weight(1f, fill = false)) }
  }
}

internal fun DrawScope.drawFigure(f: Figure) {
  val pad = size.minDimension * 0.08f
  val w = size.width - pad * 2
  val h = size.height - pad * 2
  fun p(pt: FigPt) = Offset(pad + pt.x * w, pad + pt.y * h)
  val stroke = size.minDimension / 92f

  // grabado: contorno dorado (alfa 0,55) y sombreado de líneas diagonales recortado al contorno
  val paths = f.contour.map { c ->
    when (c) {
      is FigContour.Smooth -> smoothPath(c.points.map { p(it) })
      is FigContour.Ellipse -> Path().apply {
        addOval(
          androidx.compose.ui.geometry.Rect(
            Offset(pad + (c.cx - c.rx) * w, pad + (c.cy - c.ry) * h), Offset(pad + (c.cx + c.rx) * w, pad + (c.cy + c.ry) * h)
          )
        )
      }
    }
  }
  for (path in paths) {
    clipPath(path) {
      var d = -size.height
      val step = 6f * stroke * 1.4f
      while (d < size.width) {
        drawLine(Gold.copy(alpha = 0.16f), Offset(d, size.height), Offset(d + size.height, 0f), stroke * 0.8f)
        d += step
      }
    }
    drawPath(path, Gold.copy(alpha = 0.6f), style = Stroke(stroke * 1.2f))
  }
  for (d in f.details) {
    when (d) {
      is FigDetail.Eye -> drawCircle(Gold.copy(alpha = 0.9f), stroke * 1.6f, p(d.at))
      is FigDetail.Dot -> drawCircle(Gold.copy(alpha = 0.8f), stroke * 2.2f, p(d.at), style = Stroke(stroke))
      is FigDetail.Line -> drawLine(Gold.copy(alpha = 0.7f), p(d.a), p(d.b), stroke * 1.1f, StrokeCap.Round)
    }
  }

  // las líneas de luz (la chispa ya pasó) y las estrellas: las 4 con palabra, más grandes
  for ((a, b) in f.edges) drawLine(LineGold.copy(alpha = 0.9f), p(f.points[a]), p(f.points[b]), stroke * 1.1f, StrokeCap.Round)
  f.points.forEachIndexed { i, pt ->
    val anchor = i in f.anchors
    val r = (if (anchor) 2.8f else 1.8f) * stroke
    drawCircle(StarCream.copy(alpha = if (anchor) 0.35f else 0.2f), r * 2.6f, p(pt))
    drawCircle(StarCream, r, p(pt))
  }
}

/** Curva cerrada suave (Catmull-Rom) por los puntos. */
private fun smoothPath(pts: List<Offset>): Path {
  val path = Path()
  val n = pts.size
  if (n < 3) return path
  path.moveTo(pts[0].x, pts[0].y)
  for (i in 0 until n) {
    val p0 = pts[(i - 1 + n) % n]
    val p1 = pts[i]
    val p2 = pts[(i + 1) % n]
    val p3 = pts[(i + 2) % n]
    val c1 = Offset(p1.x + (p2.x - p0.x) / 6f, p1.y + (p2.y - p0.y) / 6f)
    val c2 = Offset(p2.x - (p3.x - p1.x) / 6f, p2.y - (p3.y - p1.y) / 6f)
    path.cubicTo(c1.x, c1.y, c2.x, c2.y, p2.x, p2.y)
  }
  path.close()
  return path
}
