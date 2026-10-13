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

/** El azul marino del tablero (`#23255A` en el render): se ve mientras decodifica la imagen y si falla. */
val BoardNavy = Color(0xFF23255A)

/**
 * El fondo de la app (la «lámina B» que Ricardo eligió el 11-oct, `docs/diseno-isla.md`): un tablero de plasticina azul marino con la nebulosa pintada en el material, renderizado una vez en Blender (`tools/isla/`, 720 × 1200 px)
 * y recortado al centro para llenar cualquier pantalla. Son DOS capas apiladas que dan el B aprobado:
 * - `fondo_lamina`: la lámina sola;
 * - `fondo_luceros`: las bolitas de plasticina con su sombra de contacto, encima. Desde la Tarea 87 (Ricardo encontró Hoy «muy vacío») son ~80 y van FIJAS detrás de las tres pestañas, siempre con opacidad 1: sin fundido por pestaña.
 *   Al deslizar entre pestañas o desplazar una pantalla el fondo no se mueve, así que un lucero puede acabar detrás de un texto: los textos claros que van directo sobre el fondo usan `OnBoardText` (un halo navy suave que conserva su contraste).
 *   Solo hay una zona sin luceros, arriba (`ZONAS_HOY` en `tools/isla/isla.py`): bajo el saludo de Hoy y los títulos de Juegos y Avance, que siempre caen en el mismo lugar; lo vigila `BoardBackgroundContrastTest`.
 * Quieto: sin paralaje ni titileo. Contraste medido del saludo sobre la lámina: crema 8,3:1 y sol 6,7:1 (`BoardBackgroundContrastTest`).
 */
@Composable
fun BoardBackground(modifier: Modifier = Modifier) {
  Box(modifier.fillMaxSize().background(BoardNavy)) {
    Image(painterResource(R.drawable.fondo_lamina), contentDescription = null, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.Crop)
    Image(painterResource(R.drawable.fondo_luceros), contentDescription = null, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.Crop)
  }
}
