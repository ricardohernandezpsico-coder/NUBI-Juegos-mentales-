package com.example.ui.components

import androidx.annotation.DrawableRes
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.Image
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.withTransform
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.R
import com.example.data.AreaStatus
import com.example.data.Skill
import com.example.ui.theme.Clay
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

/** La relación ancho / alto de `nubi_3d.webp` (720 × 476 px): Nubi de plasticina de frente, recortada a su caja (render de `isla.py --solo-nubi`, ver `tools/isla/icono.py`). */
const val NUBI_ASPECT = 720f / 476f

/** La tinta de la sombra de Nubi: casi la del cielo de la app, para que se vea (más oscura que el fondo) y no ensucie. */
private val NubiShadowInk = Color(0xFF0B0930)

/**
 * La Nubi de plasticina (Tarea 92, fase 4 de `docs/diseno-isla.md`): el render 3D de frente, de [width] de ancho (alto = ancho / [NUBI_ASPECT]), con una sombra elíptica suave debajo, como en la isla, en vez del halo de antes (la Nubi lila se separa bien del cielo sin él).
 * Nubi flota apenas sobre su sombra. Con [celebrate] da el saltito de la Tarea 88 (dos saltos de 12 dp con un giro de ±5°, 900 ms, una sola vez al aparecer; la sombra se achica y aclara mientras sube): es movimiento decorativo, nunca una cara nueva y nunca según el desempeño;
 * con «quitar animaciones» no salta. Mide [width] × ([width] / [NUBI_ASPECT] + el aire de la sombra, 0,1 × [width]).
 */
@Composable
fun NubiClay(width: Dp, modifier: Modifier = Modifier, celebrate: Boolean = false) {
  val reduceMotion = rememberReduceMotion()
  val hop = remember { Animatable(0f) }
  LaunchedEffect(celebrate, reduceMotion) {
    if (celebrate && !reduceMotion) {
      hop.snapTo(0f)
      hop.animateTo(1f, tween(RouteMotion.JUMP_MS, easing = LinearEasing))
    }
  }
  val p = hop.value
  val lift = RouteMotion.jumpLiftDp(p)
  val tilt = RouteMotion.jumpTiltDeg(p)
  val body = width / NUBI_ASPECT
  Box(modifier.size(width, body + width * NUBI_SHADOW_ROOM), contentAlignment = Alignment.TopCenter) {
    Canvas(Modifier.fillMaxSize()) { drawNubiGroundShadow(body.toPx(), strength = 1f - 0.3f * (lift / RouteMotion.JUMP_DP).coerceIn(0f, 1f)) }
    Image(
      painter = painterResource(R.drawable.nubi_3d),
      contentDescription = "Nubi",
      modifier = Modifier.size(width, body).graphicsLayer { translationY = -lift * density; rotationZ = tilt }
    )
  }
}

/** Cuánto aire (fracción del ancho) deja [NubiClay] debajo del cuerpo para su sombra. */
private const val NUBI_SHADOW_ROOM = 0.1f

/** La sombra elíptica bajo Nubi: 0,8 de su ancho, 0,17 de alto sobre eso, con borde suave (degradado radial), a ras de la base del cuerpo ([bodyHeightPx]). */
private fun DrawScope.drawNubiGroundShadow(bodyHeightPx: Float, strength: Float) {
  val rx = size.width * 0.4f * (0.85f + 0.15f * strength)
  val ry = rx * 0.2f
  val c = Offset(size.width / 2f, bodyHeightPx + size.width * NUBI_SHADOW_ROOM * 0.42f)
  withTransform({ translate(c.x, c.y); scale(1f, ry / rx, Offset.Zero) }) {
    drawCircle(
      Brush.radialGradient(0f to NubiShadowInk.copy(alpha = 0.55f * strength), 0.62f to NubiShadowInk.copy(alpha = 0.45f * strength), 1f to Color.Transparent, center = Offset.Zero, radius = rx),
      radius = rx, center = Offset.Zero
    )
  }
}

// ------------------------------------------------------------------ Piezas de Hoy y del detalle de un área (la composición vieja «Nubi al centro con 4 áreas» salió con la isla, Tarea 82)

private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = com.example.ui.theme.NubiTokens.OnNightDim
/** Un solo color para las barras (el lavanda de Nubi): el NOMBRE dice qué área es, no el color. */
val AreaBarColor = Color(0xFFC4BAFF)
/** El mismo lavanda, más oscuro, para la barra sobre una tarjeta crema. */
private val AreaBarOnCard = Color(0xFF7B6BD6)
/** Lo que bajó en la semana: lila suave, nunca rojo (sin culpa). */
private val AreaDownColor = Color(0xFFB2A8E8)
private val BubbleLabel = Color(0xFF605890)

/** Orden de las áreas (30-sep, 4 áreas): en Hoy, índices pares a la izquierda y impares a la derecha. */
val AREA_ORDER = listOf("MEMORIA", "ATENCION", "RAZONAMIENTO", "LENGUAJE")

/**
 * Barra de avance de un área (0-100; 4 marcas finas = 5 etapas) con el cambio de la semana dibujado EN la barra, sin
 * números (Ricardo: los "▲ +4" molestaban a la vista): tramo sol al final = lo avanzado; tramo lila tenue con borde
 * punteado = lo que bajó. Forma distinta para cada caso, así no depende solo del color.
 */
@Composable
fun AreaBar(value: Float?, change: Float, modifier: Modifier = Modifier, height: Dp = 8.dp, onCard: Boolean = false) {
  Canvas(modifier.fillMaxWidth().height(height)) {
    val w = size.width; val h = size.height; val rad = CornerRadius(h / 2f, h / 2f)
    // Sobre una tarjeta crema (Tarea 90) la pista es tinta tenue y la barra un lavanda más oscuro (≥ 3:1 sobre la crema); sobre el cielo, como siempre.
    drawRoundRect(if (onCard) Clay.Ink.copy(alpha = 0.14f) else Color.White.copy(alpha = 0.15f), cornerRadius = rad)
    val v = (value ?: 0f).coerceIn(0f, 1f)
    val before = if (change > 0f) (v - change).coerceIn(0f, 1f) else v
    if (value != null && before > 0.005f) drawRoundRect(if (onCard) AreaBarOnCard else AreaBarColor, size = Size(maxOf(h, w * before), h), cornerRadius = rad)
    if (value != null && change > 0f) {
      val x0 = maxOf(0f, w * before - h / 2f); val x1 = w * v
      drawCircle(Brush.radialGradient(listOf(Clay.Sun.copy(alpha = 0.55f), Color.Transparent), Offset(x1, h / 2f), h * 1.6f), h * 1.6f, Offset(x1, h / 2f))
      drawRoundRect(Clay.Sun, topLeft = Offset(x0, 0f), size = Size(maxOf(h, x1 - x0), h), cornerRadius = rad)
      if (onCard) drawRoundRect(Clay.Ink, topLeft = Offset(x0, 0f), size = Size(maxOf(h, x1 - x0), h), cornerRadius = rad, style = Stroke(1.2f * density))     // el tramo sol sobre crema necesita su contorno
      drawCircle(if (onCard) Clay.Ink else Color.White, h * 0.22f, Offset(x1 - h * 0.1f, h / 2f))
    } else if (value != null && change < 0f) {
      val x0 = w * v; val x1 = w * (v - change).coerceIn(0f, 1f)
      drawRoundRect(AreaDownColor.copy(alpha = 0.28f), topLeft = Offset(x0 - h / 2f, 0f), size = Size(x1 - x0 + h / 2f, h), cornerRadius = rad)
      val step = 3f * density
      var x = x0
      while (x <= x1 + 0.1f) {
        drawCircle(AreaDownColor, 0.9f * density, Offset(x, 0f)); drawCircle(AreaDownColor, 0.9f * density, Offset(x, h))
        x += step
      }
    }
    for (k in 1..4) {
      val x = w * k / 5f
      drawLine(Color(0xAA0A0A28), Offset(x, -2f * density), Offset(x, h + 2f * density), 1.4f * density)
    }
  }
}

/** Qué dice la barra, para lectores de pantalla. */
fun areaDescription(name: String, s: AreaStatus): String {
  val v = s.points ?: return "$name: aún sin medir"
  val ch = when {
    s.change > 0f -> "avanzó esta semana"
    s.change < 0f -> "bajó un poco esta semana"
    else -> "igual que la semana pasada"
  }
  return "$name: ${Skill.stageName(s.value!!)}, $v de 100, $ch"
}

/**
 * Globo de texto de Nubi (un diálogo de Nubi: el único recuadro de la pantalla): la losa crema del kit con su cola de plasticina (Tarea 125; `kit_cola_izq`, 16 × 12 dp) abajo: a la izquierda si Nubi está al lado ([tailLeft], como en la ventana de un área
 * de Juegos) o al centro si Nubi está debajo. El texto es de Compose.
 */
@Composable
fun NubiBubble(title: String, text: String, modifier: Modifier = Modifier, tailLeft: Boolean = false) {
  Box(modifier.padding(bottom = 8.dp)) {
    ClaySlab(SlabColor.CREAM, Modifier.fillMaxWidth(), SlabSize.MEDIUM) {
      Column(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 10.dp)) {
        Text(title, color = BubbleLabel, fontSize = 16.sp)
        Text(text, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold)
      }
    }
    BubbleTail(left = true, Modifier.align(if (tailLeft) Alignment.BottomStart else Alignment.BottomCenter).offset(x = if (tailLeft) 26.dp else 0.dp, y = 2.dp))
  }
}
