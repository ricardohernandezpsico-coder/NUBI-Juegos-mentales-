package com.example.ui.components

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import com.example.R

/** El índigo casi negro del tablero «Espacio profundo» (`#0E0D33`, el material del render): se ve mientras decodifica la imagen y si falla. */
val BoardNavy = Color(0xFF0E0D33)

/**
 * El fondo de la app, «Espacio profundo» (variante E; Ricardo la eligió el 17-oct al pedir un fondo «como si estuviésemos en el espacio», `docs/diseno-isla.md`): un tablero de plasticina índigo casi negro con la nebulosa apagada pintada en el material y las esquinas y el pie
 * más oscuros (viñeta del 25 %), renderizado una vez en Blender (`tools/isla/`, `--lamina E`, 720 × 1200 px) y recortado al centro para llenar cualquier pantalla. Antes fue la lámina B (azul marino `#23255A`, 11-oct). Son DOS capas apiladas:
 * - `fondo_lamina`: la lámina sola (mediana `#151539`: arriba `#181739`, medio `#151439`, pie `#0F0E2E`);
 * - `fondo_luceros`: las bolitas de plasticina con su sombra de contacto, encima: 55 colocados (~46 visibles), en tres profundidades (de 0,9 a 2,2 % del ancho; crema, crema apagado y crema hundido). Van FIJAS detrás de las tres pestañas, siempre con opacidad 1: sin fundido por pestaña.
 *   Al deslizar entre pestañas o desplazar una pantalla el fondo no se mueve, así que un lucero puede acabar detrás de un texto: los textos claros que van directo sobre el fondo usan `OnBoardText` (un halo navy suave que conserva su contraste).
 *   Solo hay una zona sin luceros, arriba (`ZONAS_HOY` en `tools/isla/isla.py`): bajo el saludo de Hoy y los títulos de Juegos y Avance, que siempre caen en el mismo lugar; lo vigila `BoardBackgroundContrastTest`.
 * Quieto: sin paralaje ni titileo. Contraste medido del saludo sobre la lámina: crema 13,7:1 y sol 11,1:1; todos los textos claros pasan de 4,5:1 arriba, en medio y al pie (`BoardBackgroundContrastTest`).
 * Va SIEMPRE detrás de todo el contenido de las pantallas: las tarjetas y los textos tapan a los luceros, nunca al revés.
 */
@Composable
fun BoardBackground(modifier: Modifier = Modifier) {
  Box(modifier.fillMaxSize().background(BoardNavy)) {
    Image(painterResource(R.drawable.fondo_lamina), contentDescription = null, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.Crop)
    Image(painterResource(R.drawable.fondo_luceros), contentDescription = null, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.Crop)
  }
}
