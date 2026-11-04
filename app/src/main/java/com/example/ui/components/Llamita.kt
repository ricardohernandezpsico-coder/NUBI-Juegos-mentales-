package com.example.ui.components

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.size
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.State
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Fill
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.scale
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.example.ui.theme.Clay
import kotlin.math.PI
import kotlin.math.sin

/**
 * LA LLAMITA (Tarea 223; la eligió Ricardo en la lámina de la Tarea 222, `docs/previews/tarea-222.png`: «me quedo con las llamitas y el logotipo»): UN solo símbolo para «un día jugado» y para la racha, en lugar de la farola del kit (los siete días de la semana)
 * y de la gota (la racha). Se dibuja en código, sin Blender ni imágenes, con las mismas formas, colores y proporciones del prototipo de la 222:
 *
 * - **Encendida ([lit]):** una llama de tres lenguas naranja con el centro amarillo claro, el contorno de tinta y la sombra dura de la plasticina, y un halo cálido detrás.
 * - **Apagada:** a 0,65 de la altura, solo el contorno gris lila (sin relleno que compita), así que se distingue de la encendida por TAMAÑO y por FORMA (relleno y halo), nunca solo por color.
 * - **[today]:** el aro lima de «hoy» de siempre (la misma forma que tenía la farola); no se mueve ni se agranda con el destello.
 *
 * [box] es el lado de la caja cuadrada (36 dp en Hoy); la llama encendida mide el 80 % de la caja y la apagada el 52 %, apoyadas en el mismo suelo (el 90 % de la caja), y los trazos se achican con la caja (de 1,5 a 3 dp).
 * [onCream] es para las losas crema (la meta de Ajustes y los cuadros de Tú): ahí la llama apagada va en tinta suave (#514B78, 6,9:1 sobre el crema) y no en el gris lila del cielo, que sobre crema no llegaba a 3:1.
 * [halo] = false apaga el halo cálido (la meta de Ajustes ya tiene el suyo, el de la elegida). [glint] da el avance del destello (0 a 1; 1 = en reposo) y se LEE al dibujar, sin recomponer: la llama crece de 0,8 a 1,1 y vuelve a 1 desde su base,
 * y el halo late una vez; solo lo usa la llama de hoy en Hoy ([rememberTodayGlint]). Decorativa: el texto de al lado (o la frase de la semana) dice lo mismo.
 */
@Composable
fun Llamita(
  lit: Boolean,
  modifier: Modifier = Modifier,
  box: Dp = HOY_LAMP_BOX,
  today: Boolean = false,
  onCream: Boolean = false,
  halo: Boolean = true,
  glint: () -> Float = { 1f }
) {
  Canvas(modifier.size(box)) { drawLlamita(lit, today, onCream, halo, glint()) }
}

internal val FlameOrange = Color(0xFFFF8A1F)       // naranja fuego
private val FlameCore = Color(0xFFFFE680)         // el centro, amarillo claro
private val FlameHalo = Color(0xFFFF9D3F)         // el halo cálido
private val AshFill = Color(0xFF4A4682)           // gris lila de ceniza (sobre el cielo)
internal val AshLine = Color(0xFFA29FD0)           // su contorno
private val Shadow = Clay.Ink.copy(alpha = 0.8f)

/** La altura de la llama encendida y de la apagada, en fracción de la caja, y dónde apoyan (el 90 %): la apagada mide 0,65 de la encendida. */
internal const val LIT_HEIGHT = 0.80f
internal const val OFF_HEIGHT = 0.52f
private const val FLOOR = 0.90f

/** El contorno de la llama en una caja de [w] × [h] px con la punta arriba: tres lenguas (una grande al centro y dos chicas a los lados) y la panza redonda. */
private fun flamePath(w: Float, h: Float): Path {
  fun x(v: Float) = v * w
  fun y(v: Float) = v * h
  return Path().apply {
    moveTo(x(0.50f), y(1.00f))
    cubicTo(x(0.20f), y(1.00f), x(0.02f), y(0.84f), x(0.07f), y(0.60f))                   // panza de la izquierda
    cubicTo(x(0.09f), y(0.48f), x(0.15f), y(0.42f), x(0.20f), y(0.28f))                   // sube a la lengua chica de la izquierda
    cubicTo(x(0.27f), y(0.36f), x(0.30f), y(0.44f), x(0.33f), y(0.50f))                   // y baja al valle
    cubicTo(x(0.33f), y(0.32f), x(0.44f), y(0.16f), x(0.50f), y(0.00f))                   // sube a la lengua grande
    cubicTo(x(0.58f), y(0.16f), x(0.66f), y(0.28f), x(0.67f), y(0.40f))                   // y baja al valle de la derecha
    cubicTo(x(0.72f), y(0.34f), x(0.76f), y(0.26f), x(0.78f), y(0.16f))                   // sube a la lengua de la derecha
    cubicTo(x(0.88f), y(0.30f), x(0.97f), y(0.44f), x(0.93f), y(0.62f))                   // y baja a la panza de la derecha
    cubicTo(x(0.90f), y(0.84f), x(0.80f), y(1.00f), x(0.50f), y(1.00f))                   // cierra por abajo
    close()
  }
}

/** El centro de la llama: una gota más chica, anclada abajo. */
private fun corePath(w: Float, h: Float): Path {
  fun x(v: Float) = v * w
  fun y(v: Float) = v * h
  return Path().apply {
    moveTo(x(0.50f), y(0.46f))
    cubicTo(x(0.60f), y(0.58f), x(0.78f), y(0.70f), x(0.74f), y(0.84f))
    cubicTo(x(0.70f), y(0.98f), x(0.58f), y(1.00f), x(0.50f), y(1.00f))
    cubicTo(x(0.42f), y(1.00f), x(0.30f), y(0.98f), x(0.26f), y(0.84f))
    cubicTo(x(0.22f), y(0.70f), x(0.40f), y(0.58f), x(0.50f), y(0.46f))
    close()
  }
}

/** La escala del destello según su avance [p] (0 a 1): de 0,8 a 1,1 en la primera mitad y de 1,1 a 1 en la segunda, con curva suave en cada tramo. */
internal fun glintScale(p: Float): Float {
  fun smooth(t: Float) = t * t * (3f - 2f * t)
  val q = p.coerceIn(0f, 1f)
  return if (q < 0.5f) 0.8f + 0.3f * smooth(q / 0.5f) else 1.1f - 0.1f * smooth((q - 0.5f) / 0.5f)
}

/** El latido del halo del destello (0 a 1 a 0): una sola vez. */
internal fun glintPulse(p: Float): Float = sin(PI.toFloat() * p.coerceIn(0f, 1f)).coerceAtLeast(0f)

private fun DrawScope.drawLlamita(lit: Boolean, today: Boolean, onCream: Boolean, haloOn: Boolean, glintP: Float) {
  val s = size.minDimension
  val k = (s / 36.dp.toPx()).coerceIn(0.5f, 1.4f)                                              // los trazos siguen a la caja (de 1,5 a 3 dp)
  val ink = (2.5.dp.toPx() * k).coerceIn(1.5.dp.toPx(), 3.dp.toPx())
  val bottom = s * FLOOR
  fun ring() { if (today) drawCircle(Clay.Lime, s / 2f - 1.5.dp.toPx(), center, style = Stroke(2.5.dp.toPx())) }
  if (lit) {
    val fh = s * LIT_HEIGHT; val fw = fh * 0.82f
    val left = (s - fw) / 2f; val top = bottom - fh
    val sc = glintScale(glintP); val pulse = glintPulse(glintP)
    if (haloOn) scale(sc, sc, pivot = Offset(s / 2f, bottom)) {                                // el halo cálido, detrás de todo (el destello lo hace latir una vez)
      val c = Offset(s / 2f, top + fh * 0.62f); val r = s * 0.62f * (1f + 0.3f * pulse)
      drawCircle(Brush.radialGradient(listOf(FlameHalo.copy(alpha = (0.55f + 0.4f * pulse).coerceAtMost(1f)), FlameHalo.copy(alpha = 0f)), center = c, radius = r), r, c)
    }
    ring()                                                                                     // el aro lima de hoy, sobre el halo (que no lo tiña) y bajo la llama (que con el destello lo roza)
    scale(sc, sc, pivot = Offset(s / 2f, bottom)) {
      translate(left, top) {
        val body = flamePath(fw, fh)
        val drop = (2.dp.toPx() * k).coerceIn(1.dp.toPx(), 2.5.dp.toPx())
        translate(drop, drop) { drawPath(body, Shadow, style = Fill) }                         // la sombra dura de plasticina
        drawPath(body, FlameOrange, style = Fill)
        drawPath(corePath(fw * 0.62f, fh * 0.52f).also { it.translate(Offset(fw * 0.19f, fh * 0.46f)) }, FlameCore, style = Fill)
        drawPath(body, Clay.Ink, style = Stroke(ink, join = StrokeJoin.Round, cap = StrokeCap.Round))
        drawCircle(Color.White.copy(alpha = 0.55f), (1.6.dp.toPx() * k).coerceAtLeast(1.dp.toPx()), Offset(fw * 0.30f, fh * 0.60f))   // el brillo de la arcilla
      }
    }
  } else {
    ring()
    val fh = s * OFF_HEIGHT; val fw = fh * 0.82f
    val left = (s - fw) / 2f; val top = bottom - fh
    val line = if (onCream) Clay.InkMuted else AshLine
    translate(left, top) {
      val body = flamePath(fw, fh)
      drawPath(body, if (onCream) Clay.InkMuted.copy(alpha = 0.14f) else AshFill.copy(alpha = 0.35f), style = Fill)
      drawPath(body, line, style = Stroke((2.dp.toPx() * k).coerceIn(1.2.dp.toPx(), 2.4.dp.toPx()), join = StrokeJoin.Round, cap = StrokeCap.Round))
    }
  }
}

/**
 * El destello de la llama de HOY (Tarea 223): cuando el día pasa de apagado a encendido y Hoy está a la vista (la persona vuelve de su primera partida del día), la llama crece de 0,8 a 1,1 y vuelve a 1 en
 * [NubiMotion.CELEBRATE_MS] (600 ms) y su halo late una vez. Devuelve el avance del destello (0 a 1; 1 = en reposo) para pasárselo a [Llamita] (`glint`).
 *
 * Solo se arma al ver a HOY apagado con Hoy a la vista ([visible]); así NO se repite al girar la pantalla, al cambiar de pestaña ni al abrir la app con el día ya encendido (ahí nunca se vio apagado), y si la primera partida
 * se jugó con Hoy fuera de la pantalla (otra pestaña, el final de la partida encima), el destello espera a que Hoy se vea. Se arma con `rememberSaveable`: lo recuerdan la rotación y el cambio de pestaña (`KeepTabsState`). Con
 * «quitar animaciones» la llama aparece encendida de una vez, sin destello.
 */
@Composable
fun rememberTodayGlint(todayLit: Boolean, visible: Boolean): State<Float> {
  val reduce = rememberReduceMotion()
  var armed by rememberSaveable { mutableStateOf(false) }                                      // vi a hoy apagado y Hoy a la vista: el próximo encendido destella
  var playing by remember { mutableStateOf(false) }
  val fire = playing || (visible && todayLit && armed)
  val progress = remember(fire) { Animatable(if (fire && !reduce) 0f else 1f) }                // nace en 0 en la misma composición que enciende la llama: ni un cuadro encendida a tamaño completo
  LaunchedEffect(todayLit, visible) { if (visible && !todayLit) armed = true }
  LaunchedEffect(fire) {
    if (fire && !playing) {
      playing = true; armed = false
      if (!reduce) progress.animateTo(1f, tween(NubiMotion.CELEBRATE_MS, easing = LinearEasing))
      playing = false
    }
  }
  return progress.asState()
}
