package com.example.ui.components

import androidx.annotation.DrawableRes
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.FilterQuality
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.res.imageResource
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.example.R
import com.example.data.Comparison
import com.example.ui.theme.NubiTokens

/**
 * El emblema de plasticina de una división del Contrarreloj (D3, Tarea 212; diseño de Fable en `auditoria-fable/emblemas-divisiones.md`, dirección A «Los cinco lugares» que eligió Ricardo el 2-nov): un solo objeto grande por división, el lugar que dice su nombre —la plataforma de Freno con el cohete parado
 * (En la plataforma), el cohete despegando con sus nubes (Despegando), el planeta con anillo de Constelaciones (En órbita), la luna llena con cráteres (En la Luna) y tres luceros redondos (Entre estrellas)—, todos de Blender con la misma luz y material del kit (`tools/juegos/emblemas_divisiones.py`).
 *
 * Va sobre una ventanilla noche ([NubiTokens.NightWindow], esquina de [size] × 0,23): sobre la losa crema las piezas de plasticina tienen un contraste plano de 1,1 a 2,6:1 (la luna y los luceros se sostienen solo por la sombra); sobre la ventanilla, de 6,5 a 18,7:1 para las cinco y comparten silueta. El objeto ocupa el 86 % del lado.
 * Tamaños: [DivisionEmblemHero] en la cifra y [DivisionEmblemLadder] en la escalera; siempre en dp, no escala con la letra. Nada se anima.
 *
 * **Es decorativo** (`contentDescription = null`): la división ya se dice en palabras en la cifra (`Comparison.heroSpoken`), en cada fila de la escalera (`rowSpoken`) y en la fila «Tú», así que lo que lee el lector de pantalla no cambia. Las pruebas lo encuentran con la marca que le pase quien lo usa en [modifier].
 */
@Composable
fun DivisionEmblem(division: Comparison.Division, size: Dp, modifier: Modifier = Modifier) {
  Box(
    modifier.size(size).clip(RoundedCornerShape(size * CORNER)).background(NubiTokens.NightWindow),
    contentAlignment = Alignment.Center
  ) {
    Image(
      bitmap = ImageBitmap.imageResource(drawableFor(division)), contentDescription = null, filterQuality = FilterQuality.High,
      modifier = Modifier.size(size * OBJECT)
    )
  }
}

/** La imagen de cada división (`res/drawable-nodpi/division_*.webp`: los renders de `tools/juegos/emblemas_divisiones.py`, reducidos por `emblemas_divisiones_app.py`). */
@DrawableRes
fun drawableFor(division: Comparison.Division): Int = when (division) {
  Comparison.Division.PLATFORM -> R.drawable.division_plataforma
  Comparison.Division.TAKEOFF -> R.drawable.division_despegando
  Comparison.Division.ORBIT -> R.drawable.division_orbita
  Comparison.Division.MOON -> R.drawable.division_luna
  Comparison.Division.STARS -> R.drawable.division_estrellas
}

/** El emblema de la cifra (junto al nombre de la división). */
val DivisionEmblemHero = 52.dp
/** El emblema de cada fila de la escalera. */
val DivisionEmblemLadder = 28.dp

private const val CORNER = 0.23f
private const val OBJECT = 0.86f
