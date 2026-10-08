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
import androidx.compose.ui.geometry.Size
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
 * - Rastro de luz (id «secuencia»): tres luceros de cristal unidos por el rastro de la chispa.
 * - Constelaciones (id parejas): dos luces encendidas unidas por una línea dorada y dos dormidas.
 * - Tinta o Palabra: gota de tinta y tarjeta con palabra.
 * - Anagramas: dos fichas de letras.
 * - Carga exacta (id «calculo»): un reactor de arcilla con su aro de luces y tres celdas de energía debajo.
 * - Engranajes: dos engranajes de arcilla que encajan (grande lila, chico sol) y la punta REDONDEADA de un cohete que asoma arriba a la derecha (nada de estrellas con puntas).
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
  "piloto", "radar", "satelites", "freno", "aterrizaje", "acoplamiento", "rumbo", "correo",
  "secuencia", "parejas", "stroop", "anagramas", "calculo", "engranajes", "bodega", "meteoros", "disparate", "cosecha", "intrusa"
)

// Todo en un lienzo de 100x100 unidades.
private const val Line = 5f       // borde tinta
private const val Drop = 4.5f     // sombra dura
private val Ink = Clay.Ink
private val Cream = Color(0xFFFFFBF2)

private fun DrawScope.drawGameIcon(id: String, measurer: TextMeasurer) {
  when (id) {
    "secuencia" -> {
      // Rastro de luz: tres luceros de cristal (redondos, sin puntas) unidos por el rastro de la chispa, que lleva su núcleo de luz.
      val a = Offset(24f, 70f)
      val b = Offset(50f, 30f)
      val c = Offset(78f, 64f)
      val trail = Path().apply {
        moveTo(a.x, a.y)
        quadraticBezierTo(22f, 38f, b.x, b.y)
        quadraticBezierTo(82f, 24f, c.x, c.y)
      }
      drawPath(trail, Cream.copy(alpha = 0.9f), style = Stroke(4f, cap = StrokeCap.Round))
      clay(circle(a, 14f), Clay.Sky, gloss = true)
      clay(circle(b, 14f), Clay.Grape, gloss = true)
      clay(circle(c, 14f), Clay.Sun, gloss = true)
      drawCircle(Color.White, 4.6f, Offset(71f, 33f))
    }
    "parejas" -> {
      // Dos luces encendidas (una pareja encontrada de memoria) unidas por una línea dorada, y dos dormidas.
      val a = Offset(28f, 66f)
      val b = Offset(70f, 30f)
      drawLine(Clay.Sun, a, b, 5f, StrokeCap.Round)
      clay(circle(Offset(72f, 72f), 12f), Clay.Grape)
      clay(circle(Offset(26f, 28f), 12f), Clay.Grape)
      clay(circle(a, 14f), Clay.Sky, gloss = true)
      clay(circle(b, 14f), Clay.Sky, gloss = true)
      drawCircle(Color.White, 4.2f, Offset(a.x - 4f, a.y - 4f))
      drawCircle(Color.White, 4.2f, Offset(b.x - 4f, b.y - 4f))
    }
    "stroop" -> {
      clay(roundRect(34f, 50f, 54f, 34f, 9f), Cream)
      drawLine(Clay.Sky, Offset(44f, 62f), Offset(78f, 62f), 7f, StrokeCap.Round)
      drawLine(Clay.Coral, Offset(44f, 73f), Offset(66f, 73f), 7f, StrokeCap.Round)
      clay(inkDrop(Offset(30f, 44f), 20f), Clay.Coral, gloss = true)
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
      clay(hexagon(Offset(79f, 21f), 15f), Clay.Sun, border = 3.5f)
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
    "disparate" -> {
      // Una antena de arcilla: base, mástil, plato con su borde celeste, luz sol en la punta y las ondas que salen.
      drawArc(Clay.Sky.copy(alpha = 0.55f), 205f, 130f, false, Offset(24f, 2f), Size(52f, 52f), style = Stroke(5f, cap = StrokeCap.Round))
      drawArc(Clay.Sky.copy(alpha = 0.3f), 210f, 120f, false, Offset(12f, -10f), Size(76f, 76f), style = Stroke(4.5f, cap = StrokeCap.Round))
      clay(roundRect(16f, 78f, 68f, 12f, 6f), Clay.Grape)
      clay(roundRect(44f, 44f, 12f, 36f, 4f), Cream, border = 3f, shadow = false)
      clay(poly(22f, 30f, 78f, 30f, 68f, 50f, 32f, 50f), Cream)
      clay(roundRect(28f, 26f, 44f, 9f, 4.5f), Clay.Sky, border = 2.5f, shadow = false)
      clay(circle(Offset(50f, 16f), 6f), Clay.Sun, border = 2.5f, shadow = false)
    }
    "cosecha" -> {
      // Un planeta de tierra con un brote, y dos letras-luna girando en su órbita.
      drawArc(Clay.Sky.copy(alpha = 0.55f), 0f, 360f, false, Offset(6f, 46f), Size(88f, 40f), style = Stroke(3.5f))
      clay(circle(Offset(50f, 64f), 22f), Color(0xFF9E6642), gloss = true)
      clay(roundRect(47.5f, 28f, 5f, 22f, 2.5f), Clay.Lime, border = 2.5f, shadow = false)
      clay(circle(Offset(40f, 33f), 7.5f), Clay.Lime, border = 2.5f, shadow = false)
      clay(circle(Offset(60f, 33f), 7.5f), Clay.Lime, border = 2.5f, shadow = false)
      clay(circle(Offset(13f, 60f), 8.5f), Cream)
      clay(circle(Offset(87f, 72f), 8.5f), Clay.Grape)
    }
    "intrusa" -> {
      // Cuatro luceros redondos unidos por una línea de luz (sin cerrar) y un quinto que se suelta como lucero fugaz con su estela.
      val pts = listOf(Offset(16f, 70f), Offset(32f, 40f), Offset(58f, 56f), Offset(50f, 86f))
      for (i in 0 until pts.size - 1) drawLine(Cream.copy(alpha = 0.8f), pts[i], pts[i + 1], 3.2f, StrokeCap.Round)
      pts.forEach { clay(circle(it, 8f), Clay.Sun, border = 3f, shadow = false) }
      drawLine(Clay.Coral.copy(alpha = 0.3f), Offset(96f, 6f), Offset(70f, 26f), 7f, StrokeCap.Round)
      drawLine(Clay.Coral.copy(alpha = 0.55f), Offset(90f, 10f), Offset(70f, 26f), 4.5f, StrokeCap.Round)
      clay(circle(Offset(66f, 30f), 9.5f), Clay.Coral, border = 3.5f)
      sparkle(Offset(80f, 70f), 7f, Color.White)
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
    "anagramas" -> {
      // En la punta de la lengua: un lucero REDONDO (nunca una estrella con puntas) que emite ondas de señal, como la transmisión de Nubi.
      val c = Offset(38f, 62f)
      val waves = listOf(Pair(30f, 0.95f), Pair(44f, 0.7f), Pair(58f, 0.45f))
      for ((r, alpha) in waves) {
        drawArc(Clay.Sky.copy(alpha = alpha), -88f, 70f, false, Offset(c.x - r, c.y - r), Size(2f * r, 2f * r), style = Stroke(6.5f, cap = StrokeCap.Round))
      }
      clay(circle(c, 17f), Clay.Sun, gloss = true)
    }
    "engranajes" -> {
      // Engranajes: la punta redondeada de un cohete (cuerpo crema, ventanilla celeste) arriba a la derecha y, delante, dos engranajes que encajan: el grande lila y el chico sol.
      val nose = Path().apply {
        moveTo(58f, 48f)
        cubicTo(58f, 28f, 66f, 14f, 74f, 8f)
        cubicTo(82f, 14f, 90f, 28f, 90f, 48f)
        close()
      }
      clay(nose, Cream)
      clay(circle(Offset(74f, 32f), 6f), Clay.Sky, border = 3.5f, shadow = false)
      val big = Offset(36f, 62f)
      val small = Offset(70f, 74f)
      clay(gearPath(big, 27f, 19f, 10, 20f), Clay.Grape, gloss = true)
      clay(circle(big, 7f), Cream, border = 3.5f, shadow = false)
      clay(gearPath(small, 18f, 11f, 7, 20f), Clay.Sun, gloss = true)
      clay(circle(small, 4.5f), Cream, border = 3f, shadow = false)
    }
    "bodega" -> {
      // Bodega de carga: el módulo redondo (lila) con su esclusa dorada arriba, cinco escotillas crema alrededor y el robot en el centro (un disco crema con su visor).
      val c = Offset(50f, 50f)
      clay(circle(c, 41f), Clay.Grape, gloss = true)
      for (i in 0 until 6) {
        val a = (-Math.PI / 2.0 + i * Math.PI / 3.0).toFloat()
        val p = Offset(c.x + cos(a) * 28f, c.y + sin(a) * 28f)
        if (i == 0) clay(circle(p, 9f), Clay.Sun, border = 3.5f, shadow = false)
        else clay(circle(p, 7.5f), Cream, border = 3.5f, shadow = false)
      }
      clay(circle(c, 12f), Cream, border = 3.5f, shadow = false)
      drawCircle(Clay.Sky, 4.2f, c)
    }
    "calculo" -> {
      // Carga exacta: el reactor (aro con luces y un núcleo dorado) y tres celdas de energía debajo.
      val c = Offset(50f, 36f)
      clay(circle(c, 29f), Clay.Grape, gloss = true)
      for (i in 0 until 8) {
        val a = i * (Math.PI / 4.0).toFloat()
        drawCircle(Cream, 2.6f, Offset(c.x + cos(a) * 22.5f, c.y + sin(a) * 22.5f))
      }
      clay(circle(c, 15f), Clay.Sun, border = 3.5f, shadow = false)
      clay(roundRect(9f, 68f, 24f, 24f, 7f), Clay.Sky)
      clay(roundRect(38f, 68f, 24f, 24f, 7f), Clay.Coral)
      clay(roundRect(67f, 68f, 24f, 24f, 7f), Cream)
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

/** Engranaje de [n] dientes de punta [tip] y raíz [root], con el primer diente a [startDeg] grados (0 = derecha, sentido horario). Los dientes son trapecios redondos: no hay puntas. */
private fun gearPath(c: Offset, tip: Float, root: Float, n: Int, startDeg: Float) = Path().apply {
  val pitch = (2.0 * PI / n).toFloat()
  val a0 = startDeg * PI.toFloat() / 180f
  var first = true
  for (k in 0 until n) {
    val t = a0 + k * pitch
    for ((da, r) in listOf(-0.32f to root, -0.17f to tip, 0.17f to tip, 0.32f to root)) {
      val a = t + da * pitch
      val x = c.x + cos(a) * r
      val y = c.y + sin(a) * r
      if (first) { moveTo(x, y); first = false } else lineTo(x, y)
    }
  }
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

/** Hexágono regular de radio [r] (con la punta hacia arriba). */
private fun hexagon(c: Offset, r: Float) = Path().apply {
  for (i in 0 until 6) {
    val a = (-90f + i * 60f) * PI.toFloat() / 180f
    val x = c.x + cos(a) * r
    val y = c.y + sin(a) * r
    if (i == 0) moveTo(x, y) else lineTo(x, y)
  }
  close()
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

/** Brillo REDONDO (sin borde): el "brillo" de lo encendido, un núcleo con su halo (4-oct: ya no es un destello de 4 puntas). */
private fun DrawScope.sparkle(c: Offset, r: Float, color: Color) {
  drawCircle(color.copy(alpha = color.alpha * 0.35f), r, c)
  drawCircle(color, r * 0.5f, c)
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
