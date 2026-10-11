package com.example.ui.components

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.pager.PagerState
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import com.example.R

/** El azul marino del tablero (`#23255A` en el render): se ve mientras decodifica la imagen y si falla. */
val BoardNavy = Color(0xFF23255A)

/**
 * Opacidad de la capa de luceros según dónde está el pager: 1 en Hoy (página 0), 0 en Juegos y Avance (páginas 1 y 2) y, entre una y otra, lineal con el desplazamiento del dedo
 * (`currentPage + currentPageOffsetFraction` es la posición continua del pager). Sigue al dedo en vez de animarse, así que «quitar animaciones» se comporta igual.
 */
fun lucerosAlpha(currentPage: Int, currentPageOffsetFraction: Float): Float = (1f - (currentPage + currentPageOffsetFraction)).coerceIn(0f, 1f)

fun lucerosAlpha(pager: PagerState): Float = lucerosAlpha(pager.currentPage, pager.currentPageOffsetFraction)

/** Con opacidad 0 (Juegos y Avance) la capa de luceros no se dibuja en absoluto: ni siquiera se recorre su imagen. */
internal fun drawsLuceros(alpha: Float): Boolean = alpha > 0f

/**
 * Aplica la opacidad de los luceros sin recomponer al deslizar (se lee en la fase de dibujo) y NO dibuja el contenido cuando vale 0.
 * `ModulateAlpha`: la capa es una sola imagen, así que la opacidad se aplica directo, sin pasar por un búfer aparte.
 */
internal fun Modifier.lucerosLayer(alpha: () -> Float): Modifier = this
  .graphicsLayer { this.alpha = alpha().coerceIn(0f, 1f); compositingStrategy = CompositingStrategy.ModulateAlpha }
  .drawWithContent { if (drawsLuceros(alpha())) drawContent() }

/**
 * El fondo de la app (la «lámina B» que Ricardo eligió el 11-oct, `docs/diseno-isla.md`): un tablero de plasticina azul marino con la nebulosa pintada en el material, renderizado una vez en Blender (`tools/isla/`, 720 × 1200 px)
 * y recortado al centro para llenar cualquier pantalla. Son DOS capas apiladas que dan el B aprobado:
 * - `fondo_lamina`: la lámina sola, siempre detrás del pager de Hoy, Juegos y Avance (al deslizar entre pestañas no salta ni se mueve);
 * - `fondo_luceros`: las bolitas de plasticina con su sombra de contacto, encima, con la opacidad que dé [lucerosAlpha] (1 solo en Hoy). En Juegos y Avance el contenido se desliza y un lucero fijo acabaría tarde o temprano bajo un texto;
 *   en Hoy no hay luceros en las zonas donde va texto sobre el fondo (`ZONAS_HOY` en `tools/isla/isla.py`; lo vigila `HoyIslaScreenshotTest`).
 * Con la opacidad en 0 la capa de luceros no se dibuja. Quieto: sin paralaje ni titileo. Contraste medido del saludo sobre la lámina: crema 8,3:1 y sol 6,7:1 (`BoardBackgroundContrastTest`).
 */
@Composable
fun BoardBackground(modifier: Modifier = Modifier, lucerosAlpha: () -> Float = { 1f }) {
  Box(modifier.fillMaxSize().background(BoardNavy)) {
    Image(painterResource(R.drawable.fondo_lamina), contentDescription = null, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.Crop)
    Image(
      painterResource(R.drawable.fondo_luceros), contentDescription = null,
      modifier = Modifier.fillMaxSize().lucerosLayer(lucerosAlpha),
      contentScale = ContentScale.Crop
    )
  }
}
