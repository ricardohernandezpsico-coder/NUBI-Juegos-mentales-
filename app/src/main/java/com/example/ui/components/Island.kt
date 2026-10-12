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
import com.example.data.IslaLugar

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
  onDrawnRect: ((Rect) -> Unit)? = null
) {
  BoxWithConstraints(modifier, contentAlignment = Alignment.Center) {
    val width = minOf(maxHeight * layout.aspect, maxWidth + 28.dp, 480.dp)
    val height = width / layout.aspect
    val widthPx = with(LocalDensity.current) { width.toPx() }
    val look = PlaceMotion.look(lookKey)
    val miraBox = lookKey?.let { layout.miraBoxes[it] }
    Box(
      Modifier
        .requiredSize(width, height)
        .onGloballyPositioned { onDrawnRect?.invoke(it.boundsInWindow()) }
        .semantics { contentDescription = description }
        .testTag("isla")
    ) {
      Image(painterResource(R.drawable.isla_base), contentDescription = null, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.FillBounds)
      Canvas(Modifier.fillMaxSize()) { drawNubiShadow(layout, shiftX = if (lookMoves) look.shift * size.width * lookAmount() else 0f, strength = shadowAlpha()) }
      Image(painterResource(R.drawable.isla_edificios), contentDescription = null, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.FillBounds)
      Canvas(Modifier.fillMaxSize()) {
        drawHalo(layout.antennaHalo)
        drawHalo(layout.lampHalos)
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
            rotationZ = if (lookMoves) look.tiltDeg * amount else 0f
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
              rotationZ = if (lookMoves) look.tiltDeg * amount else 0f
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
private fun DrawScope.drawHalo(halo: IslaHalo) {
  val color = Color(android.graphics.Color.parseColor(halo.colorHex))
  val r = size.width * halo.radius
  halo.centers.forEach { p ->
    val c = Offset(size.width * p.x, size.height * p.y)
    drawCircle(Brush.radialGradient(listOf(color.copy(alpha = halo.opacity), color.copy(alpha = halo.opacity * 0.35f), Color.Transparent), center = c, radius = r), radius = r, center = c)
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
