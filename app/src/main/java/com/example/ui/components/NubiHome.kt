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
import com.example.data.AreaPoints
import com.example.data.Skill
import com.example.ui.theme.Clay
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

/** La relación ancho / alto de `nubi_3d.webp` y de `nubi_3d_descansa.webp` (1080 × 713 px, Tarea 148): Nubi de plasticina de frente, con los ojos abiertos y con los ojos cerrados, recortadas a la MISMA caja (render de `isla.py --solo-nubi`, armado por `tools/isla/nubi_reposo.py`). */
const val NUBI_ASPECT = 1080f / 713f

/** La tinta de la sombra de Nubi: la de la isla (`Island.kt`), la misma sombra en toda la app (Tarea 148, P3: antes `NubiClay` tenía una propia, más chica y más oscura). */
internal val NubiShadowInk = Color(0xFF2B2451)

/**
 * UNA sola sombra para Nubi (Tarea 148, P3): una elipse de borde difuso con la tinta y la opacidad de la isla, la que aprobó Ricardo. La isla la dibuja sobre su losa ([drawNubiShadowEllipse] con sus medidas de `isla.json`) y `NubiClay`, con las mismas, a ras del cuerpo.
 * [center] = el centro; [rx]/[ry] = los radios del núcleo; [soft] = media franja de desenfoque a cada lado del borde; [alpha] = la opacidad en el núcleo.
 */
internal fun DrawScope.drawNubiShadowEllipse(center: Offset, rx: Float, ry: Float, soft: Float, alpha: Float) {
  if (alpha <= 0f || rx <= 0f) return
  val outer = rx + soft
  withTransform({ translate(center.x, center.y); scale(1f, ry / rx, Offset.Zero) }) {
    drawCircle(
      Brush.radialGradient(0f to NubiShadowInk.copy(alpha = alpha), (rx - soft).coerceAtLeast(0f) / outer to NubiShadowInk.copy(alpha = alpha * 0.9f), 1f to Color.Transparent, center = Offset.Zero, radius = outer),
      radius = outer, center = Offset.Zero
    )
  }
}

/**
 * La Nubi de plasticina (Tarea 92, fase 4 de `docs/diseno-isla.md`): el render 3D de frente, de [width] de ancho (alto = ancho / [NUBI_ASPECT]), con una sombra elíptica suave debajo, como en la isla, en vez del halo de antes (la Nubi lila se separa bien del cielo sin él).
 * Nubi flota apenas sobre su sombra. Con [celebrate] da el saltito de la Tarea 88 (dos saltos de 12 dp con un giro de ±5°, 900 ms, una sola vez al aparecer; la sombra se achica y aclara mientras sube): es movimiento decorativo, nunca una cara nueva y nunca según el desempeño;
 * con «quitar animaciones» no salta. Mide [width] × ([width] / [NUBI_ASPECT] + el aire de la sombra, 0,1 × [width]).
 */
/**
 * Los TRES tamaños de Nubi (Tarea 147, P3: antes nueve, de 56 a 210 dp): [S] acompaña una frase (Avance, el resumen de la sesión, los avisos); [M] presenta una pantalla (Juegos, la ventana y el detalle de un área, el Primer vuelo); [L] es la protagonista
 * (el primer paso del Primer vuelo, las celebraciones). La lámina del lugar usa la S (72 dp), la misma imagen que vuela.
 */
enum class NubiSize(val width: Dp) { S(72.dp), M(120.dp), L(200.dp) }

/** Nubi en uno de los tres tamaños fijos ([NubiSize]). */
@Composable
fun NubiClay(size: NubiSize, modifier: Modifier = Modifier, celebrate: Boolean = false) = NubiClay(size.width, modifier, celebrate)

/** [asleep] = dormida (Tarea 165, la Nubi de la tarjeta de Hoy de noche): los ojos cerrados todo el tiempo (la misma imagen `descansa`), sin parpadear; sigue flotando apenas. Nunca según el desempeño. */
@Composable
fun NubiClay(width: Dp, modifier: Modifier = Modifier, celebrate: Boolean = false, asleep: Boolean = false) {
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
  // Nubi viva (Tarea 148, P1): flota y parpadea en reposo (nunca mientras salta); con «quitar animaciones», quieta y con los ojos abiertos
  val idle = rememberNubiIdle { !hop.isRunning }
  Box(modifier.size(width, body + width * NUBI_SHADOW_ROOM), contentAlignment = Alignment.TopCenter) {
    Canvas(Modifier.fillMaxSize()) {
      val h = NubiIdle.height01(idle.floatDp)
      drawNubiGroundShadow(body.toPx(), strength = (1f - 0.3f * (lift / RouteMotion.JUMP_DP).coerceIn(0f, 1f)) * NubiIdle.shadowAlpha(h), sizeScale = NubiIdle.shadowSize(h))
    }
    Box(Modifier.size(width, body).graphicsLayer { translationY = -(lift + idle.floatDp) * density; rotationZ = tilt }) {
      Image(painter = painterResource(R.drawable.nubi_3d), contentDescription = "Nubi", modifier = Modifier.fillMaxSize().testTag("nubi_ojos_abiertos"))
      Image(painter = painterResource(R.drawable.nubi_3d_descansa), contentDescription = null, modifier = Modifier.fillMaxSize().graphicsLayer { alpha = if (asleep) 1f else idle.eyesClosed }.testTag("nubi_ojos_cerrados"))
    }
  }
}

/** Cuánto aire (fracción del ancho) deja [NubiClay] debajo del cuerpo para su sombra. */
private const val NUBI_SHADOW_ROOM = 0.1f

/**
 * La sombra de Nubi sobre su suelo: la misma de la isla ([drawNubiShadowEllipse]): una elipse ancha (1,2 de su ancho) y baja, de borde difuso, a ras de la base del cuerpo ([bodyHeightPx]); [strength] = su opacidad (1 = la de la isla, 0,5 × 0,83) y [sizeScale] = su tamaño
 * (0,85 en el punto alto de la flotación y 1 en el bajo: la sombra se achica y se aclara cuando Nubi sube).
 */
private fun DrawScope.drawNubiGroundShadow(bodyHeightPx: Float, strength: Float, sizeScale: Float = 1f) {
  val rx = size.width * 0.6f * sizeScale
  val ry = rx * 0.2f
  val c = Offset(size.width / 2f + size.width * 0.02f, bodyHeightPx + size.width * NUBI_SHADOW_ROOM * 0.42f)
  drawNubiShadowEllipse(c, rx, ry, soft = size.width * 0.045f, alpha = 0.5f * 0.83f * strength)
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

/** Qué dice la barra de un área, para lectores de pantalla: «Memoria: etapa Hábil, 470 de 1000 puntos. Esta semana sumaste 12 puntos» ([AreaPoints.spoken]). */
fun areaDescription(name: String, p: AreaPoints): String = AreaPoints.spoken(p, name)

/** Lo que dice Avance cuando todavía no hay ninguna partida (Tarea 147, P8): en vez de tarjetas vacías, Nubi (S) con UNA línea que dice qué va a pasar. */
const val EMPTY_AVANCE_LINE = "Todavía no hay nada aquí. Después de tu primera ruta, esto se llena."

/**
 * Un estado vacío con Nubi (Tarea 147, P8): Nubi S (72 dp) a la izquierda y una línea a su derecha, sobre el cielo; sin tarjetas ni cifras vacías. La línea va en `OnBoardText` (los luceros quedan detrás).
 */
@Composable
fun NubiEmptyLine(text: String, modifier: Modifier = Modifier) {
  androidx.compose.foundation.layout.Row(modifier.fillMaxWidth().testTag("nubi_empty"), verticalAlignment = Alignment.CenterVertically) {
    NubiClay(width = 72.dp)
    androidx.compose.foundation.layout.Spacer(Modifier.width(10.dp))
    OnBoardText(text, color = com.example.ui.theme.NubiTokens.OnNight, fontSize = 17.sp, fontWeight = FontWeight.SemiBold, lineHeight = 23.sp, modifier = Modifier.weight(1f))
  }
}

/**
 * Globo de texto de Nubi (un diálogo de Nubi: el único recuadro de la pantalla): la losa crema del kit con su cola de plasticina (Tarea 125; `kit_cola_izq`, 16 × 12 dp) abajo: a la izquierda si Nubi está al lado ([tailLeft], como en la ventana de un área
 * de Juegos) o al centro si Nubi está debajo. El texto es de Compose. UNA sola forma de hablar (Tarea 147, P3): Nubi habla siempre en primera persona y siempre en su globo; el rótulo «Nubi» ([title]) solo va cuando habla SIN estar (un aviso, un correo): al lado de
 * Nubi repetiría lo que ya se ve.
 */
@Composable
fun NubiBubble(text: String, modifier: Modifier = Modifier, tailLeft: Boolean = false, title: String? = null) {
  Box(modifier.padding(bottom = 8.dp)) {
    ClaySlab(SlabColor.CREAM, Modifier.fillMaxWidth(), SlabSize.MEDIUM) {
      Column(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 10.dp)) {
        if (title != null) Text(title, color = BubbleLabel, fontSize = 16.sp)
        Text(text, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold)
      }
    }
    BubbleTail(left = true, Modifier.align(if (tailLeft) Alignment.BottomStart else Alignment.BottomCenter).offset(x = if (tailLeft) 26.dp else 0.dp, y = 2.dp))
  }
}
