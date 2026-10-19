package com.example.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.Image
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.size
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.graphics.TransformOrigin
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.layout.boundsInWindow
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.platform.LocalDensity
import com.example.data.IslaPoint
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.withTransform
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.stateDescription
import androidx.compose.ui.unit.dp
import com.example.R
import com.example.data.IslaHalo
import com.example.data.IslaLayout
import com.example.data.IslaCapa
import com.example.data.IslaLugar
import com.example.data.IslaPieza
import com.example.data.RouteLamps

/** Carga y lee `res/raw/isla.json` una sola vez (son 4 KB). */
@Composable
fun rememberIslaLayout(): IslaLayout {
  val context = LocalContext.current
  return remember { IslaLayout.parse(context.resources.openRawResource(R.raw.isla).bufferedReader().use { it.readText() }) }
}

private val ShadowInk = Color(0xFF2B2451)

/** En reposo la sombra de Nubi está en el punto medio de su flotación: 0,925 del tamaño y 0,83 de la opacidad del punto más bajo (`docs/diseno-isla.md` §6; con movimiento, fase 3, va de 0,85 y 0,3 a 1,0 y 0,46). */
private const val SHADOW_REST_SIZE = 0.925f
private const val SHADOW_REST_ALPHA = 0.83f

/**
 * La isla de plasticina de Hoy (`docs/diseno-isla.md` §6). Se arma con capas renderizadas una sola vez en Blender, apiladas con la misma cámara y la misma luz (`tools/isla/`). Viven en `drawable-nodpi` a 2×: en una carpeta de densidad Android las reescala al decodificar (un teléfono de 420 dpi las achicaba a 0,66 y se veían borrosas).
 * Capas, de abajo arriba: `isla_base` (losa, base de tierra, ruta y farolas) → la sombra de Nubi sobre la losa → `isla_edificios` → los halos de la torre y de las farolas → el brillo del lugar tocado → Nubi (`hola`, o `mira_<lugar>` mientras mira un edificio; una imagen aparte, en su caja del JSON).
 * Lo que se dibuja encima (sombra, halos y brillo) son degradados vectoriales sobre las coordenadas de `isla.json`.
 *
 * Mide lo que le deja su espacio: alto = el del contenedor, y el ancho sale de la proporción de la imagen (hasta el ancho de la pantalla más 28 dp de sangrado, con tope de 480 dp). Es DECORATIVA para el lector de pantalla:
 * las imágenes no tienen descripción y el grupo lleva [description]; los edificios son botones con el nombre del lugar solo si hay [onPlace] (abre la lámina de ese lugar; ver `ui/screens/HoyPlace.kt`).
 * [doneAreas] = áreas de las paradas de hoy ya cumplidas (el edificio dice «hecho hoy»).
 *
 * La lámina del lugar (Tarea 85) mueve a Nubi y la sombra; todo lo que cambia cuadro a cuadro llega como lambda y se lee al DIBUJAR, así que no recompone la isla:
 * [lookKey] = el lugar al que mira Nubi (null = de frente) y [lookAmount] cuánto (0 → 1: fundido a `mira_<lugar>`, corrimiento y ladeo; con [lookMoves] en false, solo el fundido); [nubiAlpha] = 0 mientras Nubi vuela por encima (la dibuja la capa de la lámina);
 * [shadowAlpha] apaga la sombra mientras Nubi no está; [glowKey]/[glowAlpha] = el brillo sol suave del edificio tocado; [onDrawnRect] avisa dónde quedó la isla en la ventana (para que la capa de la lámina sepa dónde está Nubi).
 *
 * Las piezas por medallas (Tarea 136, «Tu isla crece»; `data/IslaPiezas.kt`): [pieces] = las que la persona ya ganó, cada una una capa transparente con el MISMO recorte que la base (`isla_deco_<id>`: trae su propia posición). Las de SUELO se dibujan después de la base y de la sombra de Nubi y ANTES de los edificios
 * (el Hangar y la Biblioteca las tapan donde corresponde); las de AIRE (banderines), encima de los edificios. [pieceReveal] = cuánto lleva revelada cada una (0 → 1; 1 = puesta; alfa y escala 0,9 → 1 desde su ancla; se lee al dibujar). No cambian el tamaño de la isla ni los toques: son decorativas
 * y el lector de pantalla las nombra en [description].
 *
 * Las farolas de la ruta (Tarea 88): la farola de cada lugar de [doneAreas] (parada hecha hoy) está ENCENDIDA: un disco sol en su tope, un halo cálido de 2× el de las apagadas y un leve resplandor en la losa a su pie. [lampProgress] = cuánto lleva encendida
 * la farola de un lugar (0 → 1, con un pulso suave; 1 = encendida y quieta), [sparks] = el progreso de las chispas de la ruta completa (1 = no se ven) y [nubiLiftDp]/[nubiTiltDeg] = el saltito de Nubi. Las farolas de lugares que no están en la ruta de hoy, o que aún no se hacen, quedan como siempre.
 */
@Composable
fun Island(
  description: String,
  modifier: Modifier = Modifier,
  layout: IslaLayout = rememberIslaLayout(),
  doneAreas: Set<String> = emptySet(),
  onPlace: ((area: String) -> Unit)? = null,
  lookKey: String? = null,
  lookAmount: () -> Float = { 0f },
  lookMoves: Boolean = true,
  nubiAlpha: () -> Float = { 1f },
  shadowAlpha: () -> Float = { 1f },
  glowKey: String? = null,
  glowAlpha: () -> Float = { 0f },
  onDrawnRect: ((Rect) -> Unit)? = null,
  lampProgress: (placeKey: String) -> Float = { 1f },
  sparks: () -> Float = { 1f },
  nubiLiftDp: () -> Float = { 0f },
  nubiTiltDeg: () -> Float = { 0f },
  pieces: List<IslaPieza> = emptyList(),
  pieceReveal: (id: String) -> Float = { 1f }
) {
  BoxWithConstraints(modifier, contentAlignment = Alignment.Center) {
    val width = minOf(maxHeight * layout.aspect, maxWidth + 28.dp, 480.dp)
    val height = width / layout.aspect
    val widthPx = with(LocalDensity.current) { width.toPx() }
    val look = PlaceMotion.look(lookKey)
    val miraBox = lookKey?.let { layout.miraBoxes[it] }
    // (lugar, farola, tope) de cada parada hecha hoy: esas farolas están encendidas.
    val lit = remember(layout, doneAreas) { doneAreas.mapNotNull { a -> RouteLamps.placeKey(a)?.let { k -> layout.lampIndex(k)?.let { i -> LitLamp(k, i, layout.lamps[i]) } } }.sortedBy { it.index } }
    Box(
      Modifier
        .requiredSize(width, height)
        .onGloballyPositioned { onDrawnRect?.invoke(it.boundsInWindow()) }
        .semantics { contentDescription = description }
        .testTag("isla")
    ) {
      Image(painterResource(R.drawable.isla_base), contentDescription = null, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.FillBounds)
      Canvas(Modifier.fillMaxSize()) {
        drawNubiShadow(layout, shiftX = if (lookMoves) look.shift * size.width * lookAmount() else 0f, strength = shadowAlpha() * (1f - 0.25f * (nubiLiftDp() / RouteMotion.JUMP_DP).coerceIn(0f, 1f)))
        drawLitFootGlows(layout, lit, lampProgress)      // debajo de los edificios: el del Hangar tapa el resplandor de las farolas de adelante
      }
      pieces.filter { it.capa == IslaCapa.SUELO }.forEach { IslaPieceImage(it, layout, pieceReveal) }
      Image(painterResource(R.drawable.isla_edificios), contentDescription = null, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.FillBounds)
      pieces.filter { it.capa == IslaCapa.AIRE }.forEach { IslaPieceImage(it, layout, pieceReveal) }
      Canvas(Modifier.fillMaxSize()) {
        drawHalo(layout.antennaHalo)
        drawHalo(layout.lampHalos, skip = lit.map { it.index }.toSet())
        drawLitLamps(layout, lit, lampProgress)
        drawSparks(layout, lit, sparks())
        glowKey?.let { drawPlaceGlow(layout.place(it).center, glowAlpha()) }
      }
      // Nubi de frente; si mira un lugar, se funde con su imagen de mirada y las dos se corren y se ladean juntas.
      Image(
        painterResource(R.drawable.isla_nubi_hola), contentDescription = null,
        modifier = Modifier
          .offset(x = width * layout.nubiTopLeft.x, y = height * layout.nubiTopLeft.y)
          .size(width * (layout.nubiBottomRight.x - layout.nubiTopLeft.x), height * (layout.nubiBottomRight.y - layout.nubiTopLeft.y))
          .graphicsLayer {
            val amount = lookAmount()
            alpha = (1f - if (miraBox != null) PlaceMotion.lookFade(amount) else 0f) * nubiAlpha()
            translationX = if (lookMoves) look.shift * widthPx * amount else 0f
            translationY = -nubiLiftDp() * density
            rotationZ = (if (lookMoves) look.tiltDeg * amount else 0f) + nubiTiltDeg()
          },
        contentScale = ContentScale.FillBounds
      )
      if (miraBox != null && lookKey != null) {
        Image(
          painterResource(miraDrawable(lookKey)), contentDescription = null,
          modifier = Modifier
            .offset(x = width * miraBox.topLeft.x, y = height * miraBox.topLeft.y)
            .size(width * miraBox.width, height * miraBox.height)
            .graphicsLayer {
              val amount = lookAmount()
              alpha = PlaceMotion.lookFade(amount) * nubiAlpha()
              translationX = if (lookMoves) look.shift * widthPx * amount else 0f
              translationY = -nubiLiftDp() * density
              rotationZ = (if (lookMoves) look.tiltDeg * amount else 0f) + nubiTiltDeg()
            },
          contentScale = ContentScale.FillBounds
        )
      }
      if (onPlace != null) {
        // Un toque centrado en cada edificio (del tamaño del edificio, 48 dp como mínimo), en el orden del rombo (Observatorio, Antena, Biblioteca, Hangar).
        val hit = maxOf(48.dp, width * 0.2f)
        layout.places.forEach { p ->
          val spoken = IslaLugar.spoken(p.area).orEmpty()
          val done = p.area in doneAreas
          Box(
            Modifier
              .offset(x = width * p.center.x - hit / 2, y = height * p.center.y - hit / 2)
              .size(hit)
              .semantics(mergeDescendants = true) {
                contentDescription = spoken
                if (done) stateDescription = "hecho hoy"
              }
              .clickable(role = Role.Button, onClickLabel = "Ver $spoken") { onPlace(p.area) }
              .testTag("isla_${p.key}")
          )
        }
      }
    }
  }
}

/** Una pieza por medallas: su capa del recorte común (`FillBounds` sobre la misma caja que la base: la capa trae su posición). Aparece con alfa y escala 0,9 → 1 desde su ancla, no desde el centro de la isla. */
@Composable
private fun IslaPieceImage(piece: IslaPieza, layout: IslaLayout, reveal: (String) -> Float) {
  val origin = layout.deco[piece.id]?.anchor?.let { TransformOrigin(it.x, it.y) } ?: TransformOrigin.Center
  Image(
    painterResource(islaPiezaDrawable(piece)), contentDescription = null,
    modifier = Modifier.fillMaxSize().testTag("isla_pieza_${piece.id}").graphicsLayer {
      val p = reveal(piece.id)
      alpha = PieceFx.alpha(p)
      scaleX = PieceFx.scale(p)
      scaleY = PieceFx.scale(p)
      transformOrigin = origin
    },
    contentScale = ContentScale.FillBounds
  )
}

/** La capa de la pieza (mismo recorte que `isla_base`). */
@androidx.annotation.DrawableRes
internal fun islaPiezaDrawable(piece: IslaPieza): Int = when (piece) {
  IslaPieza.C1 -> R.drawable.isla_deco_c1
  IslaPieza.C2 -> R.drawable.isla_deco_c2
  IslaPieza.C3 -> R.drawable.isla_deco_c3
  IslaPieza.C4 -> R.drawable.isla_deco_c4
  IslaPieza.V1 -> R.drawable.isla_deco_v1
  IslaPieza.V2 -> R.drawable.isla_deco_v2
  IslaPieza.V3 -> R.drawable.isla_deco_v3
  IslaPieza.VO1 -> R.drawable.isla_deco_vo1
  IslaPieza.VO2 -> R.drawable.isla_deco_vo2
  IslaPieza.VO3 -> R.drawable.isla_deco_vo3
}

/** La imagen chica de la pieza (264 × 168 px = 88 × 56 dp) para la celebración de la medalla. */
@androidx.annotation.DrawableRes
internal fun islaPiezaChica(piece: IslaPieza): Int = when (piece) {
  IslaPieza.C1 -> R.drawable.kit_pz_deco_c1
  IslaPieza.C2 -> R.drawable.kit_pz_deco_c2
  IslaPieza.C3 -> R.drawable.kit_pz_deco_c3
  IslaPieza.C4 -> R.drawable.kit_pz_deco_c4
  IslaPieza.V1 -> R.drawable.kit_pz_deco_v1
  IslaPieza.V2 -> R.drawable.kit_pz_deco_v2
  IslaPieza.V3 -> R.drawable.kit_pz_deco_v3
  IslaPieza.VO1 -> R.drawable.kit_pz_deco_vo1
  IslaPieza.VO2 -> R.drawable.kit_pz_deco_vo2
  IslaPieza.VO3 -> R.drawable.kit_pz_deco_vo3
}

/** La imagen de Nubi mirando hacia [key] (`observatorio`, `antena`, `biblioteca` o `hangar`). */
@androidx.annotation.DrawableRes
internal fun miraDrawable(key: String): Int = when (key) {
  "antena" -> R.drawable.isla_nubi_mira_antena
  "biblioteca" -> R.drawable.isla_nubi_mira_biblioteca
  "hangar" -> R.drawable.isla_nubi_mira_hangar
  else -> R.drawable.isla_nubi_mira_observatorio
}

/** La sombra de Nubi sobre la losa: una elipse de tinta con el borde muy difuso (degradado radial, no blur). */
private fun DrawScope.drawNubiShadow(layout: IslaLayout, shiftX: Float = 0f, strength: Float = 1f) {
  if (strength <= 0f) return
  val s = layout.shadow
  val c = Offset(size.width * s.center.x + shiftX, size.height * s.center.y)
  val rx = size.width * s.width * SHADOW_REST_SIZE / 2f
  val ry = size.height * s.height * SHADOW_REST_SIZE / 2f
  val soft = size.width * s.blur / 2f                      // media franja de desenfoque a cada lado del borde
  val outer = rx + soft
  val alpha = s.opacity * SHADOW_REST_ALPHA * strength
  withTransform({ translate(c.x, c.y); scale(1f, ry / rx, Offset.Zero) }) {
    drawCircle(
      Brush.radialGradient(
        0f to ShadowInk.copy(alpha = alpha), (rx - soft).coerceAtLeast(0f) / outer to ShadowInk.copy(alpha = alpha * 0.9f), 1f to Color.Transparent,
        center = Offset.Zero, radius = outer
      ),
      radius = outer, center = Offset.Zero
    )
  }
}

/** Halos de luz cálida (degradado radial de [IslaHalo.colorHex], opaco al centro y transparente en el borde): el de la torre y los de las farolas. El radio es una fracción del ancho de la isla. */
private fun DrawScope.drawHalo(halo: IslaHalo, skip: Set<Int> = emptySet()) {
  val color = Color(android.graphics.Color.parseColor(halo.colorHex))
  val r = size.width * halo.radius
  halo.centers.forEachIndexed { i, p ->
    if (i in skip) return@forEachIndexed          // las farolas encendidas dibujan su propio halo, más grande
    val c = Offset(size.width * p.x, size.height * p.y)
    drawCircle(Brush.radialGradient(listOf(color.copy(alpha = halo.opacity), color.copy(alpha = halo.opacity * 0.35f), Color.Transparent), center = c, radius = r), radius = r, center = c)
  }
}

/** Una farola encendida: el lugar al que pertenece, su índice en `lamps` y su tope. */
private data class LitLamp(val place: String, val index: Int, val top: IslaPoint)

private fun hex(c: String) = Color(android.graphics.Color.parseColor(c))

/** El leve resplandor en la losa a pie de cada farola encendida (una elipse achatada: el suelo está en perspectiva). Va DEBAJO de los edificios. */
private fun DrawScope.drawLitFootGlows(layout: IslaLayout, lit: List<LitLamp>, progress: (String) -> Float) {
  val st = layout.litLamp
  val color = hex(st.footGlowColorHex)
  lit.forEach { l ->
    val p = progress(l.place)
    val a = st.footGlowOpacity * RouteMotion.pulseAlpha(p)
    if (a <= 0f) return@forEach
    val c = Offset(size.width * l.top.x, size.height * (l.top.y + st.footDy))
    val r = size.width * st.footGlowRadius * RouteMotion.pulseScale(p)
    withTransform({ translate(c.x, c.y); scale(1f, 0.55f, Offset.Zero) }) {
      drawCircle(Brush.radialGradient(0f to color.copy(alpha = a), 1f to Color.Transparent, center = Offset.Zero, radius = r), radius = r, center = Offset.Zero)
    }
  }
}

/** El halo cálido (2× el de las apagadas, alfa 0,8) y el disco sol en el tope de cada farola encendida; crecen con el pulso del encendido. */
private fun DrawScope.drawLitLamps(layout: IslaLayout, lit: List<LitLamp>, progress: (String) -> Float) {
  val st = layout.litLamp
  val halo = hex(st.haloColorHex)
  val disc = hex(st.discColorHex)
  lit.forEach { l ->
    val p = progress(l.place)
    val scale = RouteMotion.pulseScale(p)
    if (scale <= 0f) return@forEach
    val alpha = RouteMotion.pulseAlpha(p)
    val c = Offset(size.width * l.top.x, size.height * l.top.y)
    val r = size.width * st.haloRadius * scale
    drawCircle(Brush.radialGradient(listOf(halo.copy(alpha = st.haloOpacity * alpha), halo.copy(alpha = st.haloOpacity * alpha * 0.35f), Color.Transparent), center = c, radius = r), radius = r, center = c)
    drawCircle(disc.copy(alpha = 0.95f * alpha), radius = size.width * st.discRadius * scale.coerceAtMost(1.1f), center = c)
  }
}

/** Las chispas redondas color sol que salen de las farolas encendidas al completar la ruta y se apagan ([progress] 0 → 1; en 1 no se ven). Nada de estrellas con puntas. */
private fun DrawScope.drawSparks(layout: IslaLayout, lit: List<LitLamp>, progress: Float) {
  if (progress >= 1f) return
  val alpha = RouteMotion.sparkAlpha(progress)
  if (alpha <= 0f) return
  val color = hex(layout.litLamp.discColorHex)
  val r = size.width * RouteMotion.SPARK_RADIUS * (1f - 0.4f * progress)
  lit.forEach { l ->
    RouteMotion.sparkAngles(l.index).forEach { angle ->
      val (dx, dy) = RouteMotion.sparkOffset(angle, progress)
      drawCircle(color.copy(alpha = alpha), radius = r, center = Offset(size.width * (l.top.x + dx), size.height * l.top.y + size.width * dy))
    }
  }
}

private val GlowSun = Color(0xFFFFCB45)

/** El brillo sol suave del edificio tocado (del boceto: un círculo de 130 dp a 421 dp de ancho, del 50 % de sol al centro a transparente al 68 %). */
private fun DrawScope.drawPlaceGlow(center: IslaPoint, strength: Float) {
  if (strength <= 0f) return
  val c = Offset(size.width * center.x, size.height * center.y)
  val r = size.width * 0.154f
  drawCircle(Brush.radialGradient(0f to GlowSun.copy(alpha = 0.5f * strength), 0.68f to Color.Transparent, center = c, radius = r), radius = r, center = c)
}
