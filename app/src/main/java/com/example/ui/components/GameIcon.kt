package com.example.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.RoundRect
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.rotate
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.graphics.drawscope.withTransform
import androidx.compose.ui.text.TextMeasurer
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.drawText
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.rememberTextMeasurer
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.sp
import com.example.model.GameRegistry
import com.example.ui.theme.Clay
import com.example.ui.theme.AppFamily
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

/**
 * Ícono propio de cada juego, dibujado por código con el sello de arcilla (relleno plano, borde tinta grueso,
 * sombra dura hacia abajo y un brillo), en vez de emojis: los emojis cambian según el fabricante del teléfono,
 * no combinan con el estilo y no dicen nada de la mecánica (ui-ux-pro-max: "no emoji as icons"). Cada ícono
 * sale de lo que se hace en el juego:
 * - Secuencia Lumínica: tablero de 4 fichas con una encendida.
 * - Parejas Ocultas: una carta boca abajo y otra dada vuelta.
 * - Ruta del Tesoro: cristales estelares sobre una roca lunar (los tesoros espaciales del juego).
 * - Tinta o Palabra: gota de tinta y tarjeta con palabra.
 * - Cambio de Chip: ficha con una nave que apunta y dos flechas de cambio (como la nave del juego).
 * - Detective de Series: lupa sobre una serie que crece.
 * - Anagramas: dos fichas de letras.
 * - Cálculo Sereno: + − × =.
 * - Comparación: círculo grande > círculo chico.
 * - Piloto Estelar: la nave volando por una ruta de balizas que serpentea, con una señal que atrapar.
 * Se dibuja pensado para ir sobre el planeta del color del dominio, pero se lee también sobre fondo claro.
 * Id desconocido: cae al emoji de [com.example.model.GameDefinition.iconEmoji].
 */
@Composable
fun GameIcon(gameId: String, size: Dp, modifier: Modifier = Modifier) {
  if (gameId !in DrawnIcons) {
    Box(modifier.size(size), contentAlignment = Alignment.Center) {
      Text(GameRegistry.getById(gameId)?.iconEmoji ?: "🧠", fontSize = (size.value * 0.62f).sp)
    }
    return
  }
  val measurer = rememberTextMeasurer()
  Canvas(modifier.size(size)) {
    val k = this.size.minDimension / 100f
    val dx = (this.size.width - 100f * k) / 2f
    val dy = (this.size.height - 100f * k) / 2f
    withTransform({
      translate(dx, dy)
      scale(k, k, Offset.Zero)
    }) {
      drawGameIcon(gameId, measurer)
    }
  }
}

private val DrawnIcons = setOf(
  "piloto", "radar", "satelites", "freno", "aterrizaje", "acoplamiento", "trafico", "bitacora", "rumbo", "correo",
  "secuencia", "parejas", "rutatesoro", "stroop", "cambiochip", "series", "anagramas", "calculo", "comparacion", "meteoros"
)

// Todo en un lienzo de 100x100 unidades.
private const val Line = 5f       // borde tinta
private const val Drop = 4.5f     // sombra dura
private val Ink = Clay.Ink
private val Cream = Color(0xFFFFFBF2)

private fun DrawScope.drawGameIcon(id: String, measurer: TextMeasurer) {
  when (id) {
    "secuencia" -> {
      val tiles = listOf(Offset(13f, 13f), Offset(53f, 13f), Offset(13f, 53f), Offset(53f, 53f))
      tiles.forEachIndexed { i, o ->
        clay(roundRect(o.x, o.y, 34f, 34f, 9f), if (i == 1) Clay.Sun else Cream, gloss = i == 1)
      }
      sparkle(Offset(84f, 12f), 11f, Color.White)
    }
    "parejas" -> {
      rotate(-12f, Offset(36f, 52f)) {
        clay(roundRect(14f, 20f, 42f, 60f, 8f), Clay.Grape)
        clay(diamond(Offset(35f, 50f), 10f, 14f), Cream, border = 3.5f, shadow = false)
      }
      rotate(10f, Offset(64f, 48f)) {
        clay(roundRect(42f, 16f, 42f, 60f, 8f), Cream)
        clay(star(Offset(63f, 46f), 15f, 7f, 5), Clay.Coral, border = 4f, shadow = false)
      }
    }
    "rutatesoro" -> {
      // Cristales estelares sobre una roca lunar (como los tesoros del juego).
      clay(poly(20f, 76f, 16f, 50f, 24f, 38f, 36f, 50f, 38f, 78f), Clay.Coral)
      clay(poly(62f, 78f, 63f, 52f, 74f, 40f, 86f, 52f, 80f, 76f), Clay.Sky)
      clay(poly(38f, 80f, 38f, 34f, 50f, 12f, 62f, 34f, 62f, 80f), Clay.Grape, gloss = true)
      drawLine(Ink.copy(alpha = 0.45f), Offset(50f, 16f), Offset(50f, 78f), 3f, StrokeCap.Round)
      clay(Path().apply { addOval(Rect(12f, 70f, 88f, 92f)) }, Color(0xFF8E86C8))
      sparkle(Offset(82f, 20f), 10f, Color.White)
    }
    "stroop" -> {
      clay(roundRect(34f, 50f, 54f, 34f, 9f), Cream)
      drawLine(Clay.Sky, Offset(44f, 62f), Offset(78f, 62f), 7f, StrokeCap.Round)
      drawLine(Clay.Coral, Offset(44f, 73f), Offset(66f, 73f), 7f, StrokeCap.Round)
      clay(inkDrop(Offset(30f, 44f), 20f), Clay.Coral, gloss = true)
    }
    "cambiochip" -> {
      clay(roundRect(24f, 24f, 52f, 52f, 12f), Cream)
      // Nave que apunta arriba: llama, aletas, casco con punta y ventanilla.
      clay(poly(44.4f, 66.8f, 55.6f, 66.8f, 50.0f, 79.4f), Clay.Sun, border = 3.5f, shadow = false)
      clay(poly(41.6f, 50.0f, 31.8f, 64.0f, 31.8f, 69.6f, 41.6f, 64.0f), Clay.Coral, border = 3.5f, shadow = false)
      clay(poly(58.4f, 50.0f, 68.2f, 64.0f, 68.2f, 69.6f, 58.4f, 64.0f), Clay.Coral, border = 3.5f, shadow = false)
      clay(poly(40.2f, 66.8f, 40.2f, 41.6f, 50.0f, 22.0f, 59.8f, 41.6f, 59.8f, 66.8f), Cream, border = 3.5f, shadow = false)
      clay(poly(43.7f, 34.6f, 50.0f, 22.0f, 56.3f, 34.6f), Clay.Coral, border = 3.5f, shadow = false)
      clay(circle(Offset(50f, 47.2f), 5.0f), Clay.Sky, border = 2.5f, shadow = false)
      curvedArrow(Offset(50f, 50f), 44f, 200f, 70f, Clay.Sky)
      curvedArrow(Offset(50f, 50f), 44f, 20f, 70f, Clay.Sky)
    }
    "piloto" -> {
      // Ruta de balizas que serpentea (dos bordes de puntos) hacia arriba.
      for (k in 0..5) {
        val y = 90f - k * 15f
        val cx = 50f + 9f * sin(k * 0.95f)
        val a = 0.95f - k * 0.12f
        drawCircle(Clay.Sky.copy(alpha = a), 3.2f, Offset(cx - 25f, y))
        drawCircle(Clay.Sky.copy(alpha = a), 3.2f, Offset(cx + 25f, y))
      }
      // La nave, algo inclinada (alabeo), como en el juego.
      withTransform({
        translate(-4f, 6f)
        rotate(-12f, Offset(50f, 50f))
        scale(0.82f, 0.82f, Offset(50f, 50f))
      }) {
        clay(poly(44.4f, 66.8f, 55.6f, 66.8f, 50.0f, 79.4f), Clay.Sun, border = 3.5f, shadow = false)
        clay(poly(41.6f, 50.0f, 31.8f, 64.0f, 31.8f, 69.6f, 41.6f, 64.0f), Clay.Coral, border = 3.5f, shadow = false)
        clay(poly(58.4f, 50.0f, 68.2f, 64.0f, 68.2f, 69.6f, 58.4f, 64.0f), Clay.Coral, border = 3.5f, shadow = false)
        clay(poly(40.2f, 66.8f, 40.2f, 41.6f, 50.0f, 22.0f, 59.8f, 41.6f, 59.8f, 66.8f), Cream, border = 3.5f)
        clay(poly(43.7f, 34.6f, 50.0f, 22.0f, 56.3f, 34.6f), Clay.Coral, border = 3.5f, shadow = false)
        clay(circle(Offset(50f, 47.2f), 5.0f), Clay.Sky, border = 2.5f, shadow = false)
      }
      // La señal que hay que atrapar.
      clay(star(Offset(79f, 21f), 16f, 8f, 5), Clay.Sun, border = 3.5f)
    }
    "radar" -> {
      // Pantalla de radar de arcilla: aro, vidrio, anillos, el haz que barre y el astronauta (punto sol).
      clay(circle(Offset(50f, 50f), 40f), Color(0xFF2A3590), gloss = true)
      drawCircle(Color(0xFF0C1648), 31f, Offset(50f, 50f))
      drawCircle(Ink, 31f, Offset(50f, 50f), style = Stroke(3f))
      drawCircle(Clay.Sky.copy(alpha = 0.35f), 20f, Offset(50f, 50f), style = Stroke(1.6f))
      drawCircle(Clay.Sky.copy(alpha = 0.35f), 10f, Offset(50f, 50f), style = Stroke(1.6f))
      drawArc(
        Clay.Lime.copy(alpha = 0.35f), startAngle = -150f, sweepAngle = 60f, useCenter = true,
        topLeft = Offset(21f, 21f), size = androidx.compose.ui.geometry.Size(58f, 58f)
      )
      drawLine(Clay.Lime, Offset(50f, 50f), Offset(50f, 21f), 3f, StrokeCap.Round)
      drawCircle(Cream, 4f, Offset(50f, 50f))
      clay(circle(Offset(68f, 36f), 7f), Clay.Sun, border = 3f, shadow = false)
    }
    "satelites" -> {
      // Tres satélites iguales; el del centro lleva la señal (aro crema) y deja su estela de puntos.
      for (k in 0..3) drawCircle(Cream.copy(alpha = 0.4f + k * 0.15f), 3f, Offset(12f + k * 7f, 90f - k * 7f))
      satellite(Offset(78f, 20f), 0.8f)
      satellite(Offset(80f, 80f), 0.8f)
      drawCircle(Cream, 27f, Offset(48f, 52f), style = Stroke(4.5f))
      satellite(Offset(48f, 52f), 1.35f)
    }
    "freno" -> {
      // Señal de ALTO de arcilla (octágono coral con aro crema) y un cohete que se quedó en su lugar.
      val oct = FloatArray(16).also { a ->
        for (i in 0 until 8) {
          val ang = (PI / 8 + i * PI / 4).toFloat()
          a[i * 2] = 60f + 30f * cos(ang)
          a[i * 2 + 1] = 42f + 30f * sin(ang)
        }
      }
      val inner = FloatArray(16).also { a ->
        for (i in 0 until 8) {
          val ang = (PI / 8 + i * PI / 4).toFloat()
          a[i * 2] = 60f + 23f * cos(ang)
          a[i * 2 + 1] = 42f + 23f * sin(ang)
        }
      }
      clay(poly(*oct), Clay.Coral, gloss = true)
      drawPath(poly(*inner), Cream, style = Stroke(3.5f))
      drawLine(Cream, Offset(47f, 42f), Offset(73f, 42f), 7f, StrokeCap.Round)
      withTransform({
        translate(-25f, 17f)
        scale(0.8f, 0.8f, Offset(50f, 50f))
      }) {
        clay(poly(44.4f, 66.8f, 55.6f, 66.8f, 50.0f, 79.4f), Clay.Sun, border = 3.5f, shadow = false)
        clay(poly(41.6f, 50.0f, 31.8f, 64.0f, 31.8f, 69.6f, 41.6f, 64.0f), Clay.Sky, border = 3.5f, shadow = false)
        clay(poly(58.4f, 50.0f, 68.2f, 64.0f, 68.2f, 69.6f, 58.4f, 64.0f), Clay.Sky, border = 3.5f, shadow = false)
        clay(poly(40.2f, 66.8f, 40.2f, 41.6f, 50.0f, 22.0f, 59.8f, 41.6f, 59.8f, 66.8f), Cream, border = 3.5f)
        clay(poly(43.7f, 34.6f, 50.0f, 22.0f, 56.3f, 34.6f), Clay.Sky, border = 3.5f, shadow = false)
        clay(circle(Offset(50f, 47.2f), 5.0f), Clay.Sky, border = 2.5f, shadow = false)
      }
    }
    "meteoros" -> {
      // Un meteoro de arcilla con su placa de palabra, la estela detrás y una estrella que se rescata.
      drawLine(Clay.Sky.copy(alpha = 0.45f), Offset(14f, 12f), Offset(46f, 44f), 9f, StrokeCap.Round)
      drawLine(Clay.Sky.copy(alpha = 0.25f), Offset(8f, 26f), Offset(38f, 54f), 6f, StrokeCap.Round)
      clay(circle(Offset(58f, 58f), 30f), Clay.Grape, gloss = true)
      clay(circle(Offset(76f, 72f), 5f), Color(0xFF8F7AE6), border = 2.5f, shadow = false)
      clay(circle(Offset(40f, 76f), 4f), Color(0xFF8F7AE6), border = 2.5f, shadow = false)
      clay(roundRect(34f, 50f, 48f, 19f, 6f), Cream, border = 3f, shadow = false)
      drawLine(Ink.copy(alpha = 0.55f), Offset(40f, 59.5f), Offset(76f, 59.5f), 3.2f, StrokeCap.Round)
      sparkle(Offset(86f, 14f), 11f, Clay.Sun)
    }
    "aterrizaje" -> {
      // Regla sobre la luna con sus extremos, la bandera en el blanco y el módulo lunar bajando con su haz.
      clay(roundRect(10f, 74f, 80f, 8f, 4f), Cream)
      clay(roundRect(9f, 68f, 5f, 20f, 2.5f), Ink, border = 0f, shadow = false)
      clay(roundRect(86f, 68f, 5f, 20f, 2.5f), Ink, border = 0f, shadow = false)
      for (k in 0..3) drawCircle(Clay.Lime.copy(alpha = 0.45f + k * 0.15f), 2.4f, Offset(38f, 50f + k * 6f))
      clay(roundRect(64.5f, 44f, 4f, 31f, 2f), Cream, border = 3f, shadow = false)
      clay(poly(68f, 44f, 86f, 48f, 68f, 56f), Clay.Coral, border = 3f, shadow = false)
      // Módulo: patas, base dorada, cabina con ventanilla.
      drawLine(Ink, Offset(28f, 40f), Offset(22f, 50f), 3.5f, StrokeCap.Round)
      drawLine(Ink, Offset(48f, 40f), Offset(54f, 50f), 3.5f, StrokeCap.Round)
      clay(poly(24f, 42f, 52f, 42f, 48f, 34f, 28f, 34f), Clay.Sun, border = 3f)
      clay(roundRect(27f, 14f, 22f, 20f, 7f), Cream, border = 3.5f)
      clay(circle(Offset(38f, 24f), 5f), Clay.Sky, border = 2.5f, shadow = false)
    }
    "acoplamiento" -> {
      // Pieza en L celeste, girada, con su flecha de giro; abajo, su hueco en el puerto (tinta con borde crema).
      withTransform({ rotate(-35f, Offset(30f, 33f)) }) {
        clay(poly(15f, 10f, 30f, 10f, 30f, 41f, 45f, 41f, 45f, 56f, 15f, 56f), Clay.Sky, gloss = true)
      }
      val hole = poly(54f, 44f, 68f, 44f, 68f, 73f, 82f, 73f, 82f, 87f, 54f, 87f)
      drawPath(hole, Ink.copy(alpha = 0.85f))
      drawPath(hole, Cream, style = Stroke(3f, join = StrokeJoin.Round))
      curvedArrow(Offset(30f, 33f), 30f, 190f, 95f, Clay.Sun)
    }
    "bitacora" -> {
      // Bitácora abierta y, encima, un hallazgo que llega con destello (la estela de la sonda desde la derecha).
      clay(poly(16f, 42f, 50f, 50f, 50f, 86f, 16f, 78f), Cream)
      clay(poly(50f, 50f, 84f, 42f, 84f, 78f, 50f, 86f), Cream, gloss = true)
      for (k in 0 until 3) {
        drawLine(Ink.copy(alpha = 0.3f), Offset(23f, 54f + 8f * k), Offset(44f, 58f + 8f * k), 2.5f, StrokeCap.Round)
        drawLine(Ink.copy(alpha = 0.3f), Offset(56f, 58f + 8f * k), Offset(77f, 54f + 8f * k), 2.5f, StrokeCap.Round)
      }
      drawCircle(Clay.Sky.copy(alpha = 0.55f), 3f, Offset(88f, 14f))
      drawCircle(Clay.Sky.copy(alpha = 0.75f), 3.5f, Offset(78f, 18f))
      drawCircle(Clay.Sky, 4f, Offset(67f, 22f))
      clay(star(Offset(48f, 25f), 15f, 7f, 5, round = true), Clay.Sun, border = 4f)
      sparkle(Offset(26f, 20f), 8f, Color.White)
    }
    "rumbo" -> {
      // Tu base (casa), la ruta de ida en puntos lima hasta un cristal, y la flecha sol que vuelve a casa.
      val trail = listOf(Offset(29f, 50f), Offset(30f, 40f), Offset(31f, 30f), Offset(40f, 27f), Offset(50f, 26f), Offset(60f, 25f))
      trail.forEach { o ->
        drawCircle(Ink, 4.2f, o)
        drawCircle(Clay.Lime, 2.6f, o)
      }
      val back = Path().apply {
        moveTo(74f, 38f)
        quadraticBezierTo(72f, 66f, 49f, 71f)
        // Punta de la flecha (dirección de llegada: hacia la izquierda y un poco abajo).
        moveTo(57.8f, 78.2f)
        lineTo(47f, 71.4f)
        lineTo(53.8f, 60.6f)
      }
      clayStroke(back, Clay.Sun, 6f)
      clay(circle(Offset(27f, 72f), 17f), Cream, gloss = true)
      clay(circle(Offset(27f, 72f), 7f), Clay.Coral, border = 3.5f, shadow = false)
      clay(diamond(Offset(76f, 22f), 9f, 13f), Clay.Sky, gloss = true)
      sparkle(Offset(90f, 40f), 7f, Color.White)
    }
    "correo" -> {
      // Un sobre que vuela (estela de puntos sol) hacia un planeta coral con anillo.
      listOf(Offset(14f, 80f), Offset(22f, 72f), Offset(30f, 64f)).forEachIndexed { i, o ->
        drawCircle(Ink, 3.8f + i * 0.4f, o)
        drawCircle(Clay.Sun, 2.3f + i * 0.4f, o)
      }
      clay(circle(Offset(72f, 26f), 15f), Clay.Coral, gloss = true)
      drawPath(Path().apply { addOval(Rect(50f, 21f, 94f, 31f)) }, Ink, style = Stroke(4f))
      drawPath(Path().apply { addArc(Rect(50f, 21f, 94f, 31f), 0f, 180f) }, Clay.Sun, style = Stroke(2.2f))
      rotate(-14f, Offset(50f, 60f)) {
        clay(roundRect(28f, 44f, 44f, 30f, 6f), Cream, gloss = true)
        drawPath(Path().apply { moveTo(31f, 47f); lineTo(50f, 62f); lineTo(69f, 47f) }, Ink, style = Stroke(3f, cap = StrokeCap.Round, join = StrokeJoin.Round))
        clay(circle(Offset(50f, 60f), 5f), Clay.Coral, border = 2.5f, shadow = false)
      }
    }
    "trafico" -> {
      // Estación de carga arriba; la ruta baja en curva hasta un desvío (disco con flecha) y se abre hacia dos planetas.
      val trunk = Path().apply { moveTo(50f, 34f); cubicTo(50f, 44f, 26f, 40f, 30f, 50f); cubicTo(33f, 57f, 50f, 52f, 50f, 60f) }
      val left = Path().apply { moveTo(50f, 60f); cubicTo(42f, 68f, 20f, 66f, 18f, 82f) }
      val right = Path().apply { moveTo(50f, 60f); cubicTo(58f, 68f, 80f, 66f, 82f, 82f) }
      for (route in listOf(trunk, left, right)) drawPath(route, Ink, style = Stroke(10f, cap = StrokeCap.Round))
      drawPath(trunk, Clay.Sky, style = Stroke(5f, cap = StrokeCap.Round))
      drawPath(left, Color(0xFF4B4F9A), style = Stroke(5f, cap = StrokeCap.Round))
      drawPath(right, Clay.Sky, style = Stroke(5f, cap = StrokeCap.Round))
      clay(Path().apply { addArc(Rect(30f, 6f, 70f, 46f), 180f, 180f); close() }, Cream, gloss = true)
      clay(roundRect(20f, 24f, 60f, 12f, 6f), Color(0xFF2A3590))
      clay(roundRect(43f, 25f, 14f, 15f, 5f), Clay.Sun, border = 3f, shadow = false)
      clay(circle(Offset(18f, 82f), 12f), Clay.Coral, border = 3.5f)
      clay(circle(Offset(82f, 82f), 12f), Clay.Sun, border = 3.5f, gloss = true)
      clay(circle(Offset(50f, 60f), 10f), Color(0xFF2A3590), border = 3.5f)
      // Flecha del desvío hacia la derecha-abajo (la ruta activa).
      drawLine(Cream, Offset(46f, 56f), Offset(54f, 64f), 3f, StrokeCap.Round)
      drawLine(Cream, Offset(54f, 64f), Offset(54f, 58f), 3f, StrokeCap.Round)
      drawLine(Cream, Offset(54f, 64f), Offset(48f, 64f), 3f, StrokeCap.Round)
    }
    "series" -> {
      // Lupa de detective sobre una serie que crece (tres puntos cada vez más grandes).
      rotate(45f, Offset(40f, 40f)) { clay(roundRect(64f, 31.5f, 34f, 17f, 8.5f), Clay.Sun) }
      clay(circle(Offset(40f, 40f), 31f), Cream, gloss = true)
      clay(circle(Offset(23f, 47f), 5f), Clay.Sky, border = 3.5f, shadow = false)
      clay(circle(Offset(38f, 43f), 7f), Clay.Coral, border = 3.5f, shadow = false)
      clay(circle(Offset(56f, 37f), 9.5f), Clay.Sun, border = 3.5f, shadow = false)
    }
    "anagramas" -> {
      rotate(-10f, Offset(33f, 50f)) {
        clay(roundRect(12f, 28f, 40f, 44f, 10f), Cream)
        letter(measurer, "A", Offset(32f, 50f))
      }
      rotate(9f, Offset(67f, 50f)) {
        clay(roundRect(48f, 28f, 40f, 44f, 10f), Clay.Sun)
        letter(measurer, "Z", Offset(68f, 50f))
      }
    }
    "calculo" -> {
      val bar = 9f
      // + (arriba izquierda)
      clay(plus(Offset(28f, 28f), 34f, bar), Clay.Coral)
      // − (arriba derecha)
      clay(roundRect(56f, 28f - bar / 2f, 34f, bar, bar / 2f), Cream)
      // × (abajo izquierda)
      rotate(45f, Offset(28f, 72f)) { clay(plus(Offset(28f, 72f), 34f, bar), Clay.Sun) }
      // = (abajo derecha)
      clay(roundRect(56f, 64f - bar / 2f, 34f, bar, bar / 2f), Cream)
      clay(roundRect(56f, 80f - bar / 2f, 34f, bar, bar / 2f), Cream)
    }
    "comparacion" -> {
      clay(circle(Offset(22f, 50f), 20f), Cream, gloss = true)
      clay(circle(Offset(87f, 50f), 10f), Cream)
      chevron(Offset(58f, 50f), 13f, Clay.Sun)
    }
  }
}

/** Satélite de arcilla: cuerpo sol y dos paneles celestes, inclinado. */
private fun DrawScope.satellite(c: Offset, k: Float) {
  withTransform({
    rotate(-25f, c)
    scale(k, k, c)
  }) {
    clay(roundRect(c.x - 19f, c.y - 5f, 12f, 10f, 2.5f), Clay.Sky, border = 3f, shadow = false)
    clay(roundRect(c.x + 7f, c.y - 5f, 12f, 10f, 2.5f), Clay.Sky, border = 3f, shadow = false)
    clay(roundRect(c.x - 6.5f, c.y - 6.5f, 13f, 13f, 3.5f), Clay.Sun, border = 3f)
  }
}

// ---------- Primitivas de arcilla ----------

/** Figura de arcilla: sombra dura tinta hacia abajo, relleno, borde tinta y (opcional) brillo arriba a la izquierda. */
private fun DrawScope.clay(
  path: Path,
  fill: Color,
  gloss: Boolean = false,
  border: Float = Line,
  shadow: Boolean = true
) {
  if (shadow) translate(0f, Drop) { drawPath(path, Ink) }
  drawPath(path, fill)
  drawPath(path, Ink, style = Stroke(border, join = StrokeJoin.Round, cap = StrokeCap.Round))
  if (gloss) {
    val b = path.getBounds()
    drawOval(
      Color.White.copy(alpha = 0.55f),
      topLeft = Offset(b.left + b.width * 0.18f, b.top + b.height * 0.14f),
      size = androidx.compose.ui.geometry.Size(b.width * 0.34f, b.height * 0.16f)
    )
  }
}

private fun roundRect(x: Float, y: Float, w: Float, h: Float, r: Float) =
  Path().apply { addRoundRect(RoundRect(x, y, x + w, y + h, CornerRadius(r))) }

/** Polígono cerrado a partir de pares x, y. */
private fun poly(vararg xy: Float) = Path().apply {
  moveTo(xy[0], xy[1])
  var i = 2
  while (i < xy.size) { lineTo(xy[i], xy[i + 1]); i += 2 }
  close()
}

private fun circle(c: Offset, r: Float) = Path().apply { addOval(Rect(c.x - r, c.y - r, c.x + r, c.y + r)) }

private fun diamond(c: Offset, w: Float, h: Float) = Path().apply {
  moveTo(c.x, c.y - h); lineTo(c.x + w, c.y); lineTo(c.x, c.y + h); lineTo(c.x - w, c.y); close()
}

private fun plus(c: Offset, len: Float, bar: Float) = Path().apply {
  addRoundRect(RoundRect(c.x - len / 2f, c.y - bar / 2f, c.x + len / 2f, c.y + bar / 2f, CornerRadius(bar / 2f)))
  addRoundRect(RoundRect(c.x - bar / 2f, c.y - len / 2f, c.x + bar / 2f, c.y + len / 2f, CornerRadius(bar / 2f)))
}

/** Estrella de [points] puntas; con [round] las puntas quedan romas (estrella de mar). */
private fun star(c: Offset, outer: Float, inner: Float, points: Int, round: Boolean = false) = Path().apply {
  val n = points * 2
  val pts = (0 until n).map { i ->
    val a = (-90f + i * 360f / n) * PI.toFloat() / 180f
    val r = if (i % 2 == 0) outer else inner
    Offset(c.x + cos(a) * r, c.y + sin(a) * r)
  }
  if (!round) {
    moveTo(pts[0].x, pts[0].y); pts.drop(1).forEach { lineTo(it.x, it.y) }; close()
  } else {
    // Curvas que pasan cerca de cada punta: brazos gorditos.
    val mid = { a: Offset, b: Offset -> Offset((a.x + b.x) / 2f, (a.y + b.y) / 2f) }
    val start = mid(pts[n - 1], pts[0])
    moveTo(start.x, start.y)
    for (i in 0 until n) {
      val p = pts[i]
      val next = mid(p, pts[(i + 1) % n])
      quadraticBezierTo(p.x, p.y, next.x, next.y)
    }
    close()
  }
}

/** Gota con la punta hacia arriba; [c] = centro de la parte redonda. */
private fun inkDrop(c: Offset, r: Float) = Path().apply {
  moveTo(c.x, c.y - r * 1.9f)
  cubicTo(c.x + r * 0.35f, c.y - r * 1.2f, c.x + r, c.y - r * 0.55f, c.x + r, c.y)
  cubicTo(c.x + r, c.y + r * 0.56f, c.x + r * 0.56f, c.y + r, c.x, c.y + r)
  cubicTo(c.x - r * 0.56f, c.y + r, c.x - r, c.y + r * 0.56f, c.x - r, c.y)
  cubicTo(c.x - r, c.y - r * 0.55f, c.x - r * 0.35f, c.y - r * 1.2f, c.x, c.y - r * 1.9f)
  close()
}

/** Destello de 4 puntas (sin borde): el "brillo" de lo encendido. */
private fun DrawScope.sparkle(c: Offset, r: Float, color: Color) {
  val p = Path().apply {
    moveTo(c.x, c.y - r)
    quadraticBezierTo(c.x, c.y, c.x + r, c.y)
    quadraticBezierTo(c.x, c.y, c.x, c.y + r)
    quadraticBezierTo(c.x, c.y, c.x - r, c.y)
    quadraticBezierTo(c.x, c.y, c.x, c.y - r)
    close()
  }
  drawPath(p, color)
}

/** Trazo grueso "de arcilla": sombra tinta, borde tinta y el color encima. */
private fun DrawScope.clayStroke(path: Path, color: Color, width: Float) {
  translate(0f, Drop) { drawPath(path, Ink, style = Stroke(width + Line * 2f, cap = StrokeCap.Round, join = StrokeJoin.Round)) }
  drawPath(path, Ink, style = Stroke(width + Line * 2f, cap = StrokeCap.Round, join = StrokeJoin.Round))
  drawPath(path, color, style = Stroke(width, cap = StrokeCap.Round, join = StrokeJoin.Round))
}

/** Arco de [sweep] grados desde [startDeg] (0 = derecha, sentido horario) con punta de flecha al final. */
private fun DrawScope.curvedArrow(c: Offset, r: Float, startDeg: Float, sweep: Float, color: Color) {
  val p = Path().apply { arcTo(Rect(c.x - r, c.y - r, c.x + r, c.y + r), startDeg, sweep, true) }
  val end = (startDeg + sweep) * PI.toFloat() / 180f
  val tip = Offset(c.x + cos(end) * r, c.y + sin(end) * r)
  // Tangente (sentido horario) y normal, para la punta.
  val t = Offset(-sin(end), cos(end))
  val n = Offset(cos(end), sin(end))
  val head = 9f
  p.moveTo(tip.x - t.x * head + n.x * head, tip.y - t.y * head + n.y * head)
  p.lineTo(tip.x + t.x * 2f, tip.y + t.y * 2f)
  p.lineTo(tip.x - t.x * head - n.x * head, tip.y - t.y * head - n.y * head)
  clayStroke(p, color, 6f)
}

/** Signo ">" de arcilla centrado en [c]. */
private fun DrawScope.chevron(c: Offset, r: Float, color: Color) {
  val p = Path().apply {
    moveTo(c.x - r * 0.6f, c.y - r); lineTo(c.x + r * 0.6f, c.y); lineTo(c.x - r * 0.6f, c.y + r)
  }
  clayStroke(p, color, 7f)
}

/** Letra en Fredoka tinta, centrada en [c] (en unidades del lienzo 100x100). */
private fun DrawScope.letter(measurer: TextMeasurer, text: String, c: Offset) {
  // El lienzo está escalado: se pide la fuente en "unidades" (px antes de escalar).
  val style = TextStyle(fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 34f.toSp(), color = Ink)
  val layout = measurer.measure(text, style)
  drawText(layout, topLeft = Offset(c.x - layout.size.width / 2f, c.y - layout.size.height / 2f))
}
