package com.example.ui.components

import androidx.annotation.DrawableRes
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.MutableIntState
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.BlendMode
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.role
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.R
import com.example.ui.theme.Clay
import kotlin.math.PI
import kotlin.math.hypot
import kotlin.math.sin

/**
 * Cuántas ventanas tapan ahora las pestañas (la de un área en Juegos, el detalle de un área en Hoy, la ficha de un
 * juego...). Mientras haya alguna, el dedo NO cambia de pestaña (29-sep, Ricardo: en Juegos dos gestos hacia los
 * costados confundían). Lo lee el HorizontalPager de MainActivity.
 */
val LocalTabSwipeLock = staticCompositionLocalOf<MutableIntState> { mutableIntStateOf(0) }

/** Ponerlo dentro de una ventana: mientras esté en pantalla, las pestañas no se deslizan. */
@Composable
fun LockTabSwipe() {
  val lock = LocalTabSwipeLock.current
  DisposableEffect(lock) {
    lock.intValue++
    onDispose { lock.intValue-- }
  }
}

/**
 * Arriba a la derecha en las 3 pestañas (aprobado por Ricardo, `docs/previews/navegacion-nubi.png`): la inicial en
 * arcilla celeste = tu perfil, y el engranaje = opciones (Ajustes). Botones de 44 dp con nombre para el lector.
 */
@Composable
fun TopActions(name: String, onProfile: () -> Unit, onSettings: () -> Unit, modifier: Modifier = Modifier) {
  Row(modifier, verticalAlignment = Alignment.CenterVertically) {
    ClayCircle(Clay.Sky, "Tu perfil", onProfile, Modifier.testTag("btn_top_profile")) {
      val initial = name.trim().firstOrNull()?.uppercase()
      if (initial != null) Text(initial, color = Clay.Ink, fontSize = 20.sp, fontWeight = FontWeight.Bold)
      else Icon(Icons.Default.Person, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(24.dp))
    }
    Spacer(Modifier.width(8.dp))
    ClayCircle(Clay.Cream, "Opciones", onSettings, Modifier.testTag("btn_top_settings")) {
      Icon(Icons.Default.Settings, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(26.dp))
    }
  }
}

@Composable
private fun ClayCircle(color: Color, label: String, onClick: () -> Unit, modifier: Modifier, content: @Composable () -> Unit) {
  Box(
    modifier
      .size(44.dp)
      .drawBehind { drawCircle(Clay.Ink, size.minDimension / 2f, center.copy(y = center.y + 3.dp.toPx())) }
      .clip(CircleShape)
      .background(color)
      .border(2.5.dp, Clay.Ink, CircleShape)
      .clickable(onClick = onClick)
      .semantics { contentDescription = label; role = Role.Button },
    contentAlignment = Alignment.Center
  ) { content() }
}

/**
 * Cerebro de perfil para la pestaña Avance (dibujado aquí: los íconos de Material no tienen uno). Lóbulos en
 * nubecitas, cerebelo y tronco, con surcos ondulados calados (se ve el fondo por ellos).
 */
@Composable
fun BrainIcon(color: Color, size: Dp, modifier: Modifier = Modifier) {
  Canvas(modifier.size(size).graphicsLayer { compositingStrategy = CompositingStrategy.Offscreen }) { drawBrain(color) }
}

private fun DrawScope.drawBrain(color: Color) {
  val s = size.minDimension / 2f * 0.98f
  val c = center
  fun p(x: Float, y: Float) = Offset(c.x + x * s, c.y + y * s)
  listOf(
    Triple(-0.58f, 0.02f, 0.4f), Triple(-0.3f, -0.34f, 0.44f), Triple(0.12f, -0.42f, 0.44f), Triple(0.52f, -0.16f, 0.42f),
    Triple(0.5f, 0.2f, 0.34f), Triple(-0.02f, 0.18f, 0.46f), Triple(-0.44f, 0.3f, 0.3f)
  ).forEach { (x, y, r) -> drawCircle(color, r * s, p(x, y)) }
  drawCircle(color, 0.23f * s, p(0.49f, 0.54f))                              // cerebelo
  drawRoundRect(color, p(0.02f, 0.4f), androidx.compose.ui.geometry.Size(0.24f * s, 0.52f * s),
    androidx.compose.ui.geometry.CornerRadius(0.1f * s))                     // tronco
  val w = 0.13f * s
  fun wave(x0: Float, y0: Float, x1: Float, y1: Float, amp: Float, n: Int) {
    val path = Path()
    val nx = -(y1 - y0); val ny = x1 - x0; val l = hypot(nx, ny).takeIf { it > 0f } ?: 1f
    for (i in 0..16) {
      val t = i / 16f
      val o = sin(t * PI.toFloat() * n) * amp
      val pt = p(x0 + (x1 - x0) * t + nx / l * o, y0 + (y1 - y0) * t + ny / l * o)
      if (i == 0) path.moveTo(pt.x, pt.y) else path.lineTo(pt.x, pt.y)
    }
    drawPath(path, Color.Black, style = Stroke(w, cap = StrokeCap.Round, join = StrokeJoin.Round), blendMode = BlendMode.Clear)
  }
  wave(-0.5f, 0.2f, 0.34f, 0.02f, 0.07f, 2)      // cisura lateral
  wave(0.1f, -0.74f, -0.06f, 0.02f, 0.08f, 3)    // surco central
  wave(-0.74f, -0.14f, -0.36f, -0.2f, 0.05f, 1)
  wave(0.38f, -0.6f, 0.62f, -0.24f, 0.05f, 1)
  wave(-0.36f, -0.66f, -0.2f, -0.36f, 0.04f, 1)
}

/**
 * Planeta de cada área (esfera de color con su ícono blanco; Atención = diana), horneado por
 * `tools/previews/nubi_recursos.py` en drawable-nodpi/area_*.webp. La esfera mide [AREA_BODY_FRACTION] del ancho.
 */
const val AREA_BODY_FRACTION = 0.6f

@DrawableRes
fun areaPlanetRes(area: String): Int = when (area) {
  "MEMORIA" -> R.drawable.area_memoria
  "ATENCION" -> R.drawable.area_atencion
  "RAZONAMIENTO" -> R.drawable.area_razonamiento
  else -> R.drawable.area_lenguaje
}
