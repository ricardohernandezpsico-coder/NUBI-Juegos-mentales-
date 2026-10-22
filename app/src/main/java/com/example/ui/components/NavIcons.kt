package com.example.ui.components

import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.size
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.graphics.vector.PathParser
import androidx.compose.ui.graphics.vector.rememberVectorPainter
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.example.ui.theme.Clay

/**
 * Los íconos de la barra de pestañas y de la losa «Todos los juegos» (Tarea 158, 21-oct): vectores dibujados en Compose, NO imágenes ampliadas (la Tarea 156 usaba glifos de churro de 24 dp ampliados a 32 y a Ricardo «no se reconocía el cohete ni la farola»;
 * la lámina de estilos de la Tarea 157, `docs/previews/tarea-157-iconos.png`, midió por qué y Ricardo eligió la línea fina de tinta, con la isla en dos tonos). El dibujo vive en una rejilla de 48 × 48; el trazo es de 3 unidades y la tinta es [Clay.Ink].
 * Cada pestaña es lo que ya conoce quien usa un teléfono: la isla con su banderín (Hoy: es una miniatura de lo que se ve al abrirla), tres barras que suben (Avance: «cómo voy») y una persona (Tú). Las cuatro casillas (2 × 2) son el dibujo universal de «todos».
 * Sin estrellas con puntas, medialunas ni cruces, sin caras; los huecos interiores quedan abiertos (3 dp o más) y no llevan sombra: el disco que los sostiene ya la tiene.
 */
/** La arena de la isla (el `tan` de `tools/isla/isla.py`): los costados del bloque de Hoy. */
private val Sand = Color(0xFFDDB782)

internal enum class NavIcon(private val shapes: List<Shape>) {
  /**
   * Hoy: la isla isométrica de Hoy en miniatura, un BLOQUE (Opus, 21-oct: la primera versión, una losa plana, se leía como un hoyo de golf): la cara de arriba en rombo lima, los costados de arena que le dan el grosor, el mástil con su banderín coral y una ola corta debajo.
   * Entre las variantes (con y sin ola, con costados más o menos gruesos) se eligió la de costados de 8 unidades y ola corta: la que mejor se lee como isla y separa más sus tres formas tras un desenfoque (parecido 0,45 contra 0,65 de la losa plana).
   */
  ISLA(
    listOf(
      Shape("M6 20.5 L6 28.5 L24 37.5 L42 28.5 L42 20.5 L24 29.5 Z", fill = Sand, stroke = 3f),
      Shape("M6 20.5 L24 11.5 L42 20.5 L24 29.5 Z", fill = Clay.Lime, stroke = 3f),
      Shape("M31 19.5 L31 3.5", stroke = 3f),
      Shape("M31 4.5 L41 8.5 L31 12.5 Z", fill = Clay.Coral, stroke = 2.5f),
      Shape("M14 42 Q16.5 40 19 42 Q21.5 44 24 42 Q26.5 40 29 42 Q31.5 44 34 42", stroke = 3f)
    )
  ),

  /** Avance: tres barras que suben, de izquierda a derecha. */
  BARRAS(listOf(Shape(rrect(8f, 27f, 8f, 13f), stroke = 3f), Shape(rrect(20f, 19f, 8f, 21f), stroke = 3f), Shape(rrect(32f, 10f, 8f, 30f), stroke = 3f))),

  /** Tú: una persona (cabeza y hombros). */
  PERSONA(listOf(Shape(circle(24f, 16f, 7.5f), stroke = 3f), Shape("M10 40 C10 31 16 27 24 27 C32 27 38 31 38 40", stroke = 3f))),

  /** «Todos los juegos»: cuatro casillas en dos filas de dos. */
  CASILLAS(listOf(Shape(rrect(8f, 8f, 13f, 13f, 3.5f), stroke = 3f), Shape(rrect(27f, 8f, 13f, 13f, 3.5f), stroke = 3f), Shape(rrect(8f, 27f, 13f, 13f, 3.5f), stroke = 3f), Shape(rrect(27f, 27f, 13f, 13f, 3.5f), stroke = 3f)));

  /** El dibujo como vector (una caja de [BOX_DP] dp: se dibuja del tamaño que se pida con [NavIconImage]). */
  val vector: ImageVector by lazy {
    val b = ImageVector.Builder("nav_${name.lowercase()}", BOX_DP.dp, BOX_DP.dp, 48f, 48f)
    for (s in shapes) {
      b.addPath(
        PathParser().parsePathString(s.d).toNodes(), fill = s.fill?.let { SolidColor(it) }, stroke = SolidColor(Clay.Ink),
        strokeLineWidth = s.stroke, strokeLineCap = StrokeCap.Round, strokeLineJoin = StrokeJoin.Round
      )
    }
    b.build()
  }

  /** Qué tan lejos del centro del disco llega lo dibujado (con la mitad del trazo), en dp: tiene que quedar dentro del disco de 48 dp (radio 24) con aire. Se recorren las rutas (rectas y curvas muestreadas): no depende de `PathMeasure`, que en las pruebas de Robolectric no mide. */
  fun reachDp(dp: Float): Float {
    var far = 0f
    fun see(x: Float, y: Float, stroke: Float) { far = maxOf(far, kotlin.math.hypot(x - 24f, y - 24f) + stroke / 2) }
    for (s in shapes) {
      var cx = 0f; var cy = 0f
      for (n in PathParser().parsePathString(s.d).toNodes()) when (n) {
        is androidx.compose.ui.graphics.vector.PathNode.MoveTo -> { cx = n.x; cy = n.y; see(cx, cy, s.stroke) }
        is androidx.compose.ui.graphics.vector.PathNode.LineTo -> { cx = n.x; cy = n.y; see(cx, cy, s.stroke) }
        is androidx.compose.ui.graphics.vector.PathNode.HorizontalTo -> { cx = n.x; see(cx, cy, s.stroke) }
        is androidx.compose.ui.graphics.vector.PathNode.VerticalTo -> { cy = n.y; see(cx, cy, s.stroke) }
        is androidx.compose.ui.graphics.vector.PathNode.QuadTo -> {
          for (i in 1..12) { val t = i / 12f; val u = 1 - t; see(u * u * cx + 2 * u * t * n.x1 + t * t * n.x2, u * u * cy + 2 * u * t * n.y1 + t * t * n.y2, s.stroke) }
          cx = n.x2; cy = n.y2
        }
        is androidx.compose.ui.graphics.vector.PathNode.CurveTo -> {
          for (i in 1..16) { val t = i / 16f; val u = 1 - t; see(u * u * u * cx + 3 * u * u * t * n.x1 + 3 * u * t * t * n.x2 + t * t * t * n.x3, u * u * u * cy + 3 * u * u * t * n.y1 + 3 * u * t * t * n.y2 + t * t * t * n.y3, s.stroke) }
          cx = n.x3; cy = n.y3
        }
        else -> {}
      }
    }
    return far * dp / 48f
  }

  /** Lo que mide el dibujo ENTERO con su trazo, en unidades de la rejilla (ancho, alto): para comprobar que a [dp] dp se ve de 28 dp o más. */
  fun visible(dp: Float): Pair<Float, Float> {
    var l = Float.MAX_VALUE; var t = Float.MAX_VALUE; var r = -Float.MAX_VALUE; var bt = -Float.MAX_VALUE
    for (s in shapes) {
      val bb = PathParser().parsePathString(s.d).toPath().getBounds()
      l = minOf(l, bb.left - s.stroke / 2); t = minOf(t, bb.top - s.stroke / 2); r = maxOf(r, bb.right + s.stroke / 2); bt = maxOf(bt, bb.bottom + s.stroke / 2)
    }
    return ((r - l) * dp / 48f) to ((bt - t) * dp / 48f)
  }

  private data class Shape(val d: String, val fill: Color? = null, val stroke: Float = 3f)

  companion object {
    /** El lado de la caja del ícono dentro del disco crema de 48 dp: 40 dp, con el trazo de 3 unidades = 2,5 dp. El dibujo visible mide de 28 a 35 dp (a 32 dp medía 21 a 25: no se leía). */
    const val BOX_DP = 40f
  }
}

/** El ícono de [icon] dibujado en una caja de [size] × [size] dp (por omisión [NavIcon.BOX_DP]). */
@Composable
internal fun NavIconImage(icon: NavIcon, modifier: Modifier = Modifier, size: Dp = NavIcon.BOX_DP.dp) {
  val painter = rememberVectorPainter(remember(icon) { icon.vector })
  Image(painter, contentDescription = null, modifier = modifier.size(size))
}

private fun circle(cx: Float, cy: Float, r: Float): String {
  val k = 0.5523f * r
  return "M${cx + r} $cy C${cx + r} ${cy + k} ${cx + k} ${cy + r} $cx ${cy + r} C${cx - k} ${cy + r} ${cx - r} ${cy + k} ${cx - r} $cy C${cx - r} ${cy - k} ${cx - k} ${cy - r} $cx ${cy - r} C${cx + k} ${cy - r} ${cx + r} ${cy - k} ${cx + r} $cy Z"
}

private fun rrect(x: Float, y: Float, w: Float, h: Float, r: Float = 2f): String =
  "M${x + r} $y L${x + w - r} $y Q${x + w} $y ${x + w} ${y + r} L${x + w} ${y + h - r} Q${x + w} ${y + h} ${x + w - r} ${y + h} L${x + r} ${y + h} Q$x ${y + h} $x ${y + h - r} L$x ${y + r} Q$x $y ${x + r} $y Z"
