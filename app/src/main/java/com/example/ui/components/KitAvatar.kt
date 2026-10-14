package com.example.ui.components

import androidx.annotation.DrawableRes
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.example.R
import com.example.data.AvatarIds

/** El dibujo de plasticina de cada avatar (`AvatarIds`): Nubi sale del render `nubi_3d.webp`; los otros siete, de `isla.py --kit --kit-solo avatares` (lienzo de 48 dp con su sombra de contacto). Un id desconocido cae al de Nubi. */
@DrawableRes
internal fun avatarRes(id: String): Int = when (AvatarIds.resolve(id)) {
  AvatarIds.COHETE -> R.drawable.kit_av_cohete
  AvatarIds.FAROL -> R.drawable.kit_av_farol
  AvatarIds.ISLA -> R.drawable.kit_av_isla
  AvatarIds.ENGRANAJE -> R.drawable.kit_av_engranaje
  AvatarIds.PLANETA -> R.drawable.kit_av_planeta
  AvatarIds.LUCERO -> R.drawable.kit_av_lucero
  AvatarIds.GOTA -> R.drawable.kit_av_gota
  else -> R.drawable.nubi_3d
}

/**
 * Un avatar dentro de una caja de [size] × [size] (48 dp = el tamaño de un disco: se pone como contenido de `ClayDisc`). Es decorativo: quien lo usa dice cuál es (`AvatarIds.LABELS`). Nubi es más ancho que alto: va a 80 % de la caja.
 */
@Composable
fun KitAvatar(id: String, modifier: Modifier = Modifier, size: Dp = 48.dp) {
  val resolved = AvatarIds.resolve(id)
  Box(modifier.size(size), contentAlignment = Alignment.Center) {
    Image(
      painterResource(avatarRes(resolved)), contentDescription = null,
      modifier = Modifier.size(if (resolved == AvatarIds.NUBI) size * 0.8f else size),
      contentScale = ContentScale.Fit
    )
  }
}
