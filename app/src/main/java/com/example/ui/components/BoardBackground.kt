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
 * El fondo de la app (fase 1 de la isla de plasticina, `docs/diseno-isla.md` §14; la «lámina B» que Ricardo eligió el 11-oct): un tablero de plasticina azul marino con la nebulosa pintada en el material y luceros como bolitas de plasticina apoyadas,
 * renderizado una vez en Blender (`tools/isla/`, 720 × 1200 px) y recortado al centro para llenar cualquier pantalla. Es UNA sola capa detrás del pager de Hoy, Juegos y Avance, así que al deslizar entre pestañas no salta ni se mueve.
 * Quieto: no hay paralaje ni titileo (por eso «quitar animaciones» no cambia nada). Contraste medido del saludo sobre él: crema 8,3:1 y sol 6,7:1 (`BoardBackgroundContrastTest`).
 */
@Composable
fun BoardBackground(modifier: Modifier = Modifier) {
  Box(modifier.fillMaxSize().background(BoardNavy)) {
    Image(painterResource(R.drawable.fondo_tablero), contentDescription = null, modifier = Modifier.fillMaxSize(), contentScale = ContentScale.Crop)
  }
}
