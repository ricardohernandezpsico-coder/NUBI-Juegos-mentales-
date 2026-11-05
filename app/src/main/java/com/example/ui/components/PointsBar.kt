package com.example.ui.components

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag

/**
 * La barra de puntos (Tarea 230, 4-nov; antes `StageScale`, la «escala de las 5 etapas»): lo que queda de ella cuando las etapas Inicio · Aprendiz · Hábil · Experto · Maestro salieron de la app es SOLO la barra del kit ([KitBar]: canal y churro, con la bolita de dónde estabas hace una semana si hubo [change]),
 * sin las cuatro marcas que la partían en cinco ni los nombres de abajo. Es la de la página de un área en Avance (sobre el cielo) y la de la ficha de un juego (sobre la tarjeta crema); [onCard] = sobre una tarjeta crema (tinta) o sobre el cielo (blanco). Sin medir ([value] nulo) solo se ve el canal.
 */
@Composable
fun PointsBar(
  value: Float?,
  onCard: Boolean,
  modifier: Modifier = Modifier,
  change: Float = 0f,
  barTag: String = "points_bar",
  barModifier: Modifier = Modifier
) {
  Box(modifier.fillMaxWidth()) {
    Box(barModifier.fillMaxWidth().testTag(barTag)) {
      KitBar(value, change, onCard = onCard)
    }
  }
}
