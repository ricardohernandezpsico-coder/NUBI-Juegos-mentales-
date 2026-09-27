package com.example.ui.components

import android.provider.Settings
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.PathEffect
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.PlanetState
import com.example.data.PlanetZone
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.theme.Clay
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.hypot
import kotlin.math.min
import kotlin.math.roundToInt
import kotlin.math.sin

/** Una partida de la sesión de hoy: orbita el planeta y, al jugarla, "aterriza" en la zona de su dominio. */
data class PlanetShip(val gameId: String, val domain: String, val landed: Boolean)

/** Nombre y color de un dominio a partir de su clave ([DomainType.name]). */
fun domainOf(key: String): DomainType? = DomainType.values().firstOrNull { it.name == key }

/** Color del dominio aclarado para texto sobre el cielo (contraste ≥ 4.5:1 con la noche). */
fun domainTextColor(key: String): Color = lerp(domainOf(key)?.color ?: Color.White, Color.White, 0.45f)

private val Ocean = Color(0xFF3A4696)

private data class ShipPos(val ship: PlanetShip, val pos: Offset, val behind: Boolean)

// Ángulo de cada zona en el planeta (grados, 0 = arriba, sentido horario); Velocidad va al centro.
private val ZoneAngle = mapOf(
  "MEMORIA" to -35f, "ATENCION" to 35f, "RAZONAMIENTO" to 105f, "LENGUAJE" to 180f, "CALCULO" to 250f
)

private fun zoneCenter(domain: String, c: Offset, r: Float, spin: Float): Offset {
  val a = ZoneAngle[domain] ?: return c
  val t = ((a + spin - 90f) * PI / 180f).toFloat()
  return Offset(c.x + cos(t) * r * 0.58f, c.y + sin(t) * r * 0.58f)
}

private fun zoneRadius(z: PlanetZone, r: Float): Float = when {
  z.plays == 0 -> r * 0.12f
  z.domain !in ZoneAngle -> r * (0.14f + 0.12f * z.growth)
  else -> r * (0.16f + 0.14f * z.growth)
}

/**
 * "Tu planeta" (pestaña Hoy): un planeta de arcilla con una zona por dominio que crece con cada partida (cristales en
 * Memoria, faros en Atención, torres en Razonamiento, árboles en Lenguaje, domos en Cálculo, antenas en Velocidad).
 * Gira despacio; las 3 partidas del día lo orbitan y al jugarlas aterrizan en su zona con un ✓. Tocar una zona la abre.
 * Respeta "quitar animaciones" (queda quieto).
 */
@Composable
fun HomePlanet(
  state: PlanetState,
  ships: List<PlanetShip>,
  onZone: (String) -> Unit,
  modifier: Modifier = Modifier
) {
  val context = LocalContext.current
  val reduceMotion = remember {
    Settings.Global.getFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f) == 0f
  }
  val anim = rememberInfiniteTransition(label = "planet")
  val spinAnim by anim.animateFloat(0f, 360f, infiniteRepeatable(tween(150_000, easing = LinearEasing)), label = "spin")
  val orbitAnim by anim.animateFloat(0f, 360f, infiniteRepeatable(tween(48_000, easing = LinearEasing)), label = "orbit")
  val pulseAnim by anim.animateFloat(0f, 1f, infiniteRepeatable(tween(1600), RepeatMode.Reverse), label = "pulse")
  val spin = if (reduceMotion) 0f else spinAnim
  val orbit = if (reduceMotion) 30f else orbitAnim
  val pulse = if (reduceMotion) 0.5f else pulseAnim
  val grew = state.playedToday.lastOrNull()

  BoxWithConstraints(
    modifier = modifier.semantics { contentDescription = "Tu planeta. Toca una zona para ver sus juegos." }
  ) {
    val density = LocalDensity.current
    val w = with(density) { maxWidth.toPx() }
    val h = with(density) { maxHeight.toPx() }
    val top = with(density) { 26.dp.toPx() } // espacio para "¡Creció ...!"
    val r = min(w * 0.33f, (h - top) * 0.40f).coerceAtLeast(1f)
    val c = Offset(w / 2f, top + (h - top) / 2f)
    val rx = min(r * 1.5f, w / 2f - with(density) { 24.dp.toPx() })
    val ry = r * 0.42f
    val shipSize = 34.dp
    val shipPx = with(density) { shipSize.toPx() }

    val currentSpin by rememberUpdatedState(spin)
    val currentState by rememberUpdatedState(state)
    val tapModifier = Modifier.pointerInput(c, r) {
      detectTapGestures { p ->
        val zones = currentState.zones
        val hit = zones.minByOrNull { (zoneCenter(it.domain, c, r, currentSpin) - p).getDistance() / zoneRadius(it, r) }
        if (hit != null) {
          val d = (zoneCenter(hit.domain, c, r, currentSpin) - p).getDistance()
          if (d <= zoneRadius(hit, r) * 1.5f || hypot(p.x - c.x, p.y - c.y) <= r) onZone(hit.domain)
        }
      }
    }

    // Posición de cada nave: en órbita (repartidas cada 120°) o posada en su zona.
    val positions = ships.mapIndexed { i, s ->
      if (s.landed) {
        val same = ships.take(i).count { it.landed && it.domain == s.domain }
        val zc = zoneCenter(s.domain, c, r, spin)
        ShipPos(s, zc + Offset(same * shipPx * 0.6f, -shipPx * 0.15f), false)
      } else {
        val t = ((orbit + i * 120f) * PI / 180f).toFloat()
        ShipPos(s, Offset(c.x + cos(t) * rx, c.y + sin(t) * ry), sin(t) < 0f)
      }
    }

    // Detrás: resplandor y mitad lejana de la órbita.
    Canvas(Modifier.fillMaxSize()) {
      drawCircle(Brush.radialGradient(listOf(Clay.Sky.copy(alpha = 0.22f), Color.Transparent), c, r * 1.7f), r * 1.7f, c)
      drawArc(
        Color.White.copy(alpha = 0.22f), 180f, 180f, false, Offset(c.x - rx, c.y - ry), Size(rx * 2, ry * 2),
        style = Stroke(2.dp.toPx())
      )
    }
    positions.filter { it.behind }.forEach { ShipAt(it.ship, it.pos, shipSize, dim = true) }
    // El planeta y la mitad cercana de la órbita.
    Canvas(Modifier.fillMaxSize().then(tapModifier)) {
      drawPlanet(state, c, r, spin, pulse)
      drawArc(
        Color.White.copy(alpha = 0.32f), 0f, 180f, false, Offset(c.x - rx, c.y - ry), Size(rx * 2, ry * 2),
        style = Stroke(2.dp.toPx())
      )
    }
    positions.filter { !it.behind }.forEach { ShipAt(it.ship, it.pos, shipSize, dim = false) }

    if (grew != null) {
      Text(
        text = "¡Creció ${domainOf(grew)?.displayName ?: ""}!",
        color = Clay.Sun,
        fontSize = 17.sp,
        fontWeight = FontWeight.Bold,
        modifier = Modifier.align(Alignment.TopCenter)
      )
    }
  }
}

@Composable
private fun ShipAt(ship: PlanetShip, pos: Offset, size: Dp, dim: Boolean) {
  val half = with(LocalDensity.current) { size.toPx() / 2f }
  MiniPlanet(
    gameId = ship.gameId,
    size = size,
    done = ship.landed,
    modifier = Modifier
      .offset { IntOffset((pos.x - half).roundToInt(), (pos.y - half).roundToInt()) }
      .alpha(if (dim) 0.7f else 1f)
  )
}

/** Un juego como planetita de arcilla (color de su dominio + ícono); [done] = ✓ lima de "ya jugado hoy". */
@Composable
fun MiniPlanet(gameId: String, size: Dp, modifier: Modifier = Modifier, done: Boolean = false) {
  val color = GameRegistry.getById(gameId)?.domain?.color ?: Clay.Grape
  Box(modifier.size(size), contentAlignment = Alignment.Center) {
    Box(
      Modifier
        .size(size)
        .drawBehind {
          val rr = this.size.minDimension / 2f
          drawCircle(Clay.Ink, rr, Offset(center.x, center.y + 2.5.dp.toPx()))
          drawCircle(color, rr, center)
          drawCircle(Clay.Ink, rr - 1.dp.toPx(), center, style = Stroke(2.dp.toPx()))
        }
    )
    GameIcon(gameId, size * 0.82f)
    if (done) {
      Box(
        Modifier
          .align(Alignment.TopEnd)
          .offset(x = 4.dp, y = (-4).dp)
          .size(size * 0.45f)
          .clip(CircleShape)
          .background(Clay.Lime)
          .border(2.dp, Clay.Ink, CircleShape),
        contentAlignment = Alignment.Center
      ) { Icon(Icons.Default.Check, contentDescription = "Jugado hoy", tint = Clay.Ink, modifier = Modifier.size(size * 0.32f)) }
    }
  }
}

// ------------------------------------------------------------------ dibujo del planeta

private fun DrawScope.drawPlanet(state: PlanetState, c: Offset, r: Float, spin: Float, pulse: Float) {
  val ink = Clay.Ink
  drawCircle(ink, r, Offset(c.x, c.y + 7.dp.toPx()))
  drawCircle(Ocean, r, c)
  drawCircle(Color.White.copy(alpha = 0.06f), r * 0.8f, Offset(c.x - r * 0.15f, c.y - r * 0.15f))
  // Zonas: primero los resplandores de las jugadas hoy, después cada zona con sus construcciones.
  state.zones.filter { it.playedToday }.forEach { z ->
    val zc = zoneCenter(z.domain, c, r, spin)
    val zr = zoneRadius(z, r) * (1.9f + 0.25f * pulse)
    drawCircle(Brush.radialGradient(listOf(Clay.Sun.copy(alpha = 0.55f), Color.Transparent), zc, zr), zr, zc)
  }
  state.zones.forEach { z -> drawZone(z, zoneCenter(z.domain, c, r, spin), zoneRadius(z, r)) }
  drawCircle(ink, r - 1.75.dp.toPx(), c, style = Stroke(3.5.dp.toPx()))
  drawOval(Color.White.copy(alpha = 0.16f), Offset(c.x - r * 0.72f, c.y - r * 0.84f), Size(r * 0.36f, r * 0.2f))
}

private fun blob(zc: Offset, zr: Float, seed: Float): Path = Path().apply {
  for (i in 0 until 36) {
    val t = (2 * PI * i / 36).toFloat()
    val x = zc.x + cos(t) * zr * (1f + 0.12f * sin(3 * t + seed))
    val y = zc.y + sin(t) * zr * 0.82f * (1f + 0.12f * cos(2 * t + seed))
    if (i == 0) moveTo(x, y) else lineTo(x, y)
  }
  close()
}

private fun DrawScope.drawZone(z: PlanetZone, zc: Offset, zr: Float) {
  val color = domainOf(z.domain)?.color ?: Clay.Grape
  val seed = (ZoneAngle[z.domain] ?: 0f) * 0.1f
  val shape = blob(zc, zr, seed)
  if (z.plays == 0) {
    // Sin explorar: un contorno punteado tenue (una semilla de zona).
    drawPath(shape, color.copy(alpha = 0.22f))
    drawPath(
      shape, Color.White.copy(alpha = 0.45f),
      style = Stroke(1.5.dp.toPx(), pathEffect = PathEffect.dashPathEffect(floatArrayOf(4.dp.toPx(), 4.dp.toPx())))
    )
    return
  }
  translate(0f, 3.dp.toPx()) { drawPath(shape, Clay.Ink) }
  drawPath(shape, color)
  drawPath(shape, Clay.Ink, style = Stroke(2.dp.toPx(), join = StrokeJoin.Round))
  val n = z.structures
  val base = zc.y + zr * 0.25f
  for (k in 0 until n) {
    val bx = zc.x + (k - (n - 1) / 2f) * zr * (if (n > 3) 0.38f else 0.5f)
    val hh = (9.dp.toPx() + 10.dp.toPx() * z.growth) * (0.85f + 0.3f * (((k * 37 + n * 11) % 10) / 10f))
    drawStructure(z.domain, Offset(bx, base), hh)
  }
}

private fun DrawScope.drawStructure(domain: String, b: Offset, h: Float) {
  val ink = Clay.Ink
  val line = Stroke(1.5.dp.toPx(), join = StrokeJoin.Round)
  fun shape(p: Path, fill: Color) {
    drawPath(p, fill)
    drawPath(p, ink, style = line)
  }
  when (domain) {
    "MEMORIA" -> shape(Path().apply { // cristal
      moveTo(b.x, b.y - h); lineTo(b.x + h * 0.35f, b.y - h * 0.35f); lineTo(b.x, b.y); lineTo(b.x - h * 0.35f, b.y - h * 0.35f); close()
    }, Color(0xFFBEDEFF))
    "ATENCION" -> { // faro
      shape(Path().apply { addRect(Rect(b.x - h * 0.15f, b.y - h * 0.75f, b.x + h * 0.15f, b.y)) }, Color(0xFFFFF8EC))
      shape(Path().apply { addOval(Rect(b.x - h * 0.22f, b.y - h, b.x + h * 0.22f, b.y - h * 0.6f)) }, Clay.Sun)
    }
    "RAZONAMIENTO" -> shape(Path().apply { // torre
      moveTo(b.x - h * 0.25f, b.y); lineTo(b.x - h * 0.15f, b.y - h * 0.65f); lineTo(b.x, b.y - h)
      lineTo(b.x + h * 0.15f, b.y - h * 0.65f); lineTo(b.x + h * 0.25f, b.y); close()
    }, Color(0xFFE1D4FF))
    "LENGUAJE" -> { // árbol
      drawLine(ink, Offset(b.x, b.y), Offset(b.x, b.y - h * 0.35f), 2.5.dp.toPx())
      shape(Path().apply { addOval(Rect(b.x - h * 0.4f, b.y - h, b.x + h * 0.4f, b.y - h * 0.25f)) }, Color(0xFF96F0BE))
    }
    "CALCULO" -> shape(Path().apply { // domo
      arcTo(Rect(b.x - h * 0.45f, b.y - h * 0.8f, b.x + h * 0.45f, b.y + h * 0.1f), 180f, 180f, true); close()
    }, Color(0xFFAAF5E6))
    else -> { // antena (Velocidad)
      drawLine(ink, Offset(b.x, b.y), Offset(b.x, b.y - h * 0.7f), 2.dp.toPx())
      shape(Path().apply { addOval(Rect(b.x - h * 0.2f, b.y - h, b.x + h * 0.2f, b.y - h * 0.6f)) }, Color(0xFFFFAAB9))
    }
  }
}
