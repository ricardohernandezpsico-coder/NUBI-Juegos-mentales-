package com.example.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.Image
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
import androidx.compose.ui.graphics.TransformOrigin
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import com.example.R
import com.example.data.IslaHalo
import com.example.data.IslaLayout
import com.example.data.IslaCapa
import com.example.data.IslaPieza

/** Carga y lee `res/raw/isla.json` una sola vez (son 4 KB). */
@Composable
fun rememberIslaLayout(): IslaLayout {
  val context = LocalContext.current
  return remember { IslaLayout.parse(context.resources.openRawResource(R.raw.isla).bufferedReader().use { it.readText() }) }
}

/** En reposo la sombra de Nubi está en el punto medio de su flotación: 0,925 del tamaño y 0,83 de la opacidad del punto más bajo (`docs/diseno-isla.md` §6; con movimiento, fase 3, va de 0,85 y 0,3 a 1,0 y 0,46). */
private const val SHADOW_REST_SIZE = 0.925f
private const val SHADOW_REST_ALPHA = 0.83f

/**
 * La isla de plasticina (`docs/diseno-isla.md` §6; desde la Tarea 165 vive en Tú, en el Primer vuelo y, una vez al día, dentro de la tarjeta de Hoy cuando la ruta queda completa). Se arma con capas renderizadas una sola vez en Blender, apiladas con la misma cámara y la misma luz (`tools/isla/`). Viven en `drawable-nodpi` a 2×: en una carpeta de densidad Android las reescala al decodificar (un teléfono de 420 dpi las achicaba a 0,66 y se veían borrosas).
 * Capas, de abajo arriba: `isla_base` (losa, base de tierra, ruta y farolas) → la sombra de Nubi sobre la losa → `isla_edificios` → los halos de la torre y de las farolas → Nubi (`hola`).
 * Lo que se dibuja encima (sombra y halos) son degradados vectoriales sobre las coordenadas de `isla.json`.
 *
 * Mide lo que le deja su espacio: alto = el del contenedor, y el ancho sale de la proporción de la imagen (hasta el ancho de la pantalla más 28 dp de sangrado, con tope de 480 dp). Es DECORATIVA para el lector de pantalla:
 * las imágenes no tienen descripción y el grupo lleva [description]. NO es tocable: desde la Tarea 165 los edificios no son botones (la prueba con personas del 22-oct mostró que nadie los leía como áreas).
 *
 * Nubi flota y parpadea (`rememberNubiIdle`) y su sombra la sigue; lo que cambia cuadro a cuadro se lee al DIBUJAR, así que no recompone la isla. (Tarea 182: se borró la «mirada» de Nubi hacia un lugar, de la Tarea 88: nadie la manejaba desde la Tarea 168.)
 *
 * Las piezas por medallas (Tarea 136, «Tu isla crece»; `data/IslaPiezas.kt`): [pieces] = las que la persona ya ganó, cada una una capa transparente con el MISMO recorte que la base (`isla_deco_<id>`: trae su propia posición). Las de SUELO se dibujan después de la base y de la sombra de Nubi y ANTES de los edificios
 * (el Hangar y la Biblioteca las tapan donde corresponde); las de AIRE (banderines), encima de los edificios. [pieceReveal] = cuánto lleva revelada cada una (0 → 1; 1 = puesta; alfa y escala 0,9 → 1 desde su ancla; se lee al dibujar). No cambian el tamaño de la isla ni los toques: son decorativas
 * y el lector de pantalla las nombra en [description].
 */
@Composable
fun Island(
  description: String,
  modifier: Modifier = Modifier,
  layout: IslaLayout = rememberIslaLayout(),
  pieces: List<IslaPieza> = emptyList(),
  pieceReveal: (id: String) -> Float = { 1f }
) {
  // Nubi viva (Tarea 148, P1): flota y parpadea en reposo (quieta y sin parpadear con «quitar animaciones»). Su sombra se achica y se aclara cuando sube (`docs/diseno-isla.md` §6).
  val idle = rememberNubiIdle()
  BoxWithConstraints(modifier, contentAlignment = Alignment.Center) {
    val width = minOf(maxHeight * layout.aspect, maxWidth + 28.dp, 480.dp)
    val height = width / layout.aspect
    Box(
      Modifier
        .requiredSize(width, height)
        .semantics { contentDescription = description }
        .testTag("isla")
    ) {
      Image(painterResource(R.drawable.isla_base), contentDescription = null, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.FillBounds)
      Canvas(Modifier.fillMaxSize()) {
        val h = NubiIdle.height01(idle.floatDp)
        drawNubiShadow(layout, strength = NubiIdle.shadowAlpha(h) / SHADOW_REST_ALPHA, sizeScale = NubiIdle.shadowSize(h))
      }
      pieces.filter { it.capa == IslaCapa.SUELO }.forEach { IslaPieceImage(it, layout, pieceReveal) }
      Image(painterResource(R.drawable.isla_edificios), contentDescription = null, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.FillBounds)
      pieces.filter { it.capa == IslaCapa.AIRE }.forEach { IslaPieceImage(it, layout, pieceReveal) }
      Canvas(Modifier.fillMaxSize()) {
        drawHalo(layout.antennaHalo)
        drawHalo(layout.lampHalos)
      }
      // Nubi de frente
      Image(
        painterResource(R.drawable.isla_nubi_hola), contentDescription = null,
        modifier = Modifier
          .offset(x = width * layout.nubiTopLeft.x, y = height * layout.nubiTopLeft.y)
          .size(width * (layout.nubiBottomRight.x - layout.nubiTopLeft.x), height * (layout.nubiBottomRight.y - layout.nubiTopLeft.y))
          .graphicsLayer { translationY = -idle.floatDp * density }.testTag("isla_nubi"),
        contentScale = ContentScale.FillBounds
      )
      // los ojos cerrados (`descansa`): el mismo cuerpo y la misma caja; se funde encima de la de frente al parpadear
      Image(
        painterResource(R.drawable.isla_nubi_descansa), contentDescription = null,
        modifier = Modifier
          .offset(x = width * layout.nubiTopLeft.x, y = height * layout.nubiTopLeft.y)
          .size(width * (layout.nubiBottomRight.x - layout.nubiTopLeft.x), height * (layout.nubiBottomRight.y - layout.nubiTopLeft.y))
          .graphicsLayer {
            alpha = idle.eyesClosed
            translationY = -idle.floatDp * density
          }.testTag("isla_nubi_ojos_cerrados"),
        contentScale = ContentScale.FillBounds
      )
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

/** La sombra de Nubi sobre la losa: una elipse de tinta con el borde muy difuso (degradado radial, no blur). */
private fun DrawScope.drawNubiShadow(layout: IslaLayout, strength: Float = 1f, sizeScale: Float = SHADOW_REST_SIZE) {
  if (strength <= 0f) return
  val s = layout.shadow
  val c = Offset(size.width * s.center.x, size.height * s.center.y)
  val rx = size.width * s.width * sizeScale / 2f
  val ry = size.height * s.height * sizeScale / 2f
  val soft = size.width * s.blur / 2f                      // media franja de desenfoque a cada lado del borde
  drawNubiShadowEllipse(c, rx, ry, soft, alpha = s.opacity * SHADOW_REST_ALPHA * strength)          // la misma sombra que `NubiClay` (Tarea 148, P3)
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
