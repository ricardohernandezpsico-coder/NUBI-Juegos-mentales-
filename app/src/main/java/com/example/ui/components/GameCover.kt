package com.example.ui.components

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.example.R

/**
 * Las portadas de los 17 juegos (Tarea 171, 25-oct; `tools/previews/portadas.py`): un recorte de una captura REAL del juego corriendo en Unity (1080 × 480 px, en webp, sin el marcador ni avisos dentro del recorte: a este tamaño no se leerían), el objeto del juego en su cielo
 * de noche, la «ventanilla noche» del kit (regla R: lo calibrado sobre oscuro va en su ventanilla, dentro de la crema). Se regeneran con ese script cuando cambia el arte de un juego, como las láminas.
 */
private val COVERS: Map<String, Int> = mapOf(
  "parejas" to R.drawable.portada_parejas, "secuencia" to R.drawable.portada_secuencia, "bodega" to R.drawable.portada_bodega, "correo" to R.drawable.portada_correo,
  "stroop" to R.drawable.portada_stroop, "piloto" to R.drawable.portada_piloto, "freno" to R.drawable.portada_freno, "satelites" to R.drawable.portada_satelites, "radar" to R.drawable.portada_radar,
  "calculo" to R.drawable.portada_calculo, "engranajes" to R.drawable.portada_engranajes, "acoplamiento" to R.drawable.portada_acoplamiento, "aterrizaje" to R.drawable.portada_aterrizaje,
  "anagramas" to R.drawable.portada_anagramas, "meteoros" to R.drawable.portada_meteoros, "disparate" to R.drawable.portada_disparate, "intrusa" to R.drawable.portada_intrusa
)

/** La portada de un juego, o null si es uno retirado (la ventanilla queda con el cielo solo). */
internal fun coverRes(gameId: String): Int? = COVERS[gameId]

/** Los ids de los juegos que tienen portada (los 17 de la app). */
internal val COVER_GAME_IDS: Set<String> get() = COVERS.keys

/** Los tres tonos del cielo de la app (`#07071F`, `#100F3A`, `#1C1B52`): lo que se ve de la ventanilla si la imagen no está. */
private val CoverSky = Brush.verticalGradient(listOf(Color(0xFF07071F), Color(0xFF100F3A), Color(0xFF1C1B52)))
private val CoverRim = Color(0xFF3A3875)

/**
 * El alto de la ventanilla, ADAPTATIVO (Tarea 172; eran 150 dp en la Tarea 171 y los objetos de varios juegos no cabían enteros): 200 dp con letra hasta 1,3 y 180 dp con letra mayor, para que la ficha plegada a 2,0 siga cabiendo (771 dp de los 777 que deja la ventana de un teléfono de 393 × 873).
 * Es el mismo umbral con que la ficha apila sus botones y baja el nombre (`fontScale > 1,3`). UNA imagen de 1200 × 626 px (la proporción de 345 × 180 dp) sirve a las dos: a 180 dp se ve entera y a 200 dp se recorta un 5 % a cada lado; `tools/previews/portadas.py` pone los objetos dentro de lo que se ve de las dos.
 */
internal fun coverHeight(fontScale: Float): Dp = if (fontScale > 1.3f) 180.dp else 200.dp

/**
 * La ventanilla de la ficha de un juego: [coverHeight] de alto, a todo el ancho que le dé quien la pone, con la esquina redondeada y un borde fino; la imagen se recorta por los lados si el teléfono es angosto (la escena va al centro). No es tocable: informa (lo informativo es plano).
 * Decorativa para el lector de pantalla: el nombre del juego ya está dicho en el título.
 */
@Composable
internal fun GameCover(gameId: String, modifier: Modifier = Modifier) {
  val shape = RoundedCornerShape(22.dp)
  val height = coverHeight(LocalDensity.current.fontScale)
  val res = coverRes(gameId)
  Box(modifier.fillMaxWidth().height(height).clip(shape).background(CoverSky).border(1.5.dp, CoverRim, shape)) {
    if (res != null) Image(painterResource(res), contentDescription = null, contentScale = ContentScale.Crop, alignment = Alignment.Center, modifier = Modifier.matchParentSize())
  }
}
