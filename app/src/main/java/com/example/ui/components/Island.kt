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
 * La isla de plasticina de Hoy (`docs/diseno-isla.md` §6, fase 1: sin movimiento). Se arma con capas renderizadas una sola vez en Blender, apiladas con la misma cámara y la misma luz (`tools/isla/`). Viven en `drawable-nodpi` a 2×: en una carpeta de densidad Android las reescala al decodificar (un teléfono de 420 dpi las achicaba a 0,66 y se veían borrosas).
 * Capas, de abajo arriba: `isla_base` (losa, base de tierra, ruta y farolas) → la sombra de Nubi sobre la losa → `isla_edificios` → los halos de la torre y de las farolas → Nubi `hola` (una imagen aparte, en su caja del JSON).
 * Lo que se dibuja encima (sombra y halos) son degradados vectoriales sobre las coordenadas de `isla.json`.
 *
 * Mide lo que le deja su espacio: alto = el del contenedor, y el ancho sale de la proporción de la imagen (hasta el ancho de la pantalla más 28 dp de sangrado, con tope de 480 dp). Es DECORATIVA para el lector de pantalla:
 * las imágenes no tienen descripción y el grupo lleva [description]; los edificios son botones con el nombre del lugar solo si hay [onPlace] (hoy abren el detalle de su área; en la fase 2, Juegos con ese lugar elegido).
 * [doneAreas] = áreas de las paradas de hoy ya cumplidas (el edificio dice «hecho hoy»).
 */
@Composable
fun Island(
  description: String,
  modifier: Modifier = Modifier,
  layout: IslaLayout = rememberIslaLayout(),
  doneAreas: Set<String> = emptySet(),
  onPlace: ((area: String) -> Unit)? = null
) {
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
      Canvas(Modifier.fillMaxSize()) { drawNubiShadow(layout) }
      Image(painterResource(R.drawable.isla_edificios), contentDescription = null, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.FillBounds)
      Canvas(Modifier.fillMaxSize()) {
        drawHalo(layout.antennaHalo)
        drawHalo(layout.lampHalos)
      }
      Image(
        painterResource(R.drawable.isla_nubi_hola), contentDescription = null,
        modifier = Modifier
          .offset(x = width * layout.nubiTopLeft.x, y = height * layout.nubiTopLeft.y)
          .size(width * (layout.nubiBottomRight.x - layout.nubiTopLeft.x), height * (layout.nubiBottomRight.y - layout.nubiTopLeft.y)),
        contentScale = ContentScale.FillBounds
      )
      if (onPlace != null) {
        // Un toque de 48 dp centrado en cada edificio, en el orden del rombo (Observatorio, Antena, Biblioteca, Hangar).
        layout.places.forEach { p ->
          val spoken = IslaLugar.spoken(p.area).orEmpty()
          val done = p.area in doneAreas
          Box(
            Modifier
              .offset(x = width * p.center.x - 24.dp, y = height * p.center.y - 24.dp)
              .size(48.dp)
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

/** La sombra de Nubi sobre la losa: una elipse de tinta con el borde muy difuso (degradado radial, no blur). */
private fun DrawScope.drawNubiShadow(layout: IslaLayout) {
  val s = layout.shadow
  val c = Offset(size.width * s.center.x, size.height * s.center.y)
  val rx = size.width * s.width * SHADOW_REST_SIZE / 2f
  val ry = size.height * s.height * SHADOW_REST_SIZE / 2f
  val soft = size.width * s.blur / 2f                      // media franja de desenfoque a cada lado del borde
  val outer = rx + soft
  val alpha = s.opacity * SHADOW_REST_ALPHA
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
