package com.example.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material3.Icon
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.theme.Clay

// El planeta de Hoy ("Tu planeta") se reemplazó el 29-sep por Nubi al centro (NubiHome.kt); quedan acá las piezas
// que usan otras pantallas.

/** Nombre y color de un dominio a partir de su clave ([DomainType.name]). */
fun domainOf(key: String): DomainType? = DomainType.values().firstOrNull { it.name == key }

/** Color del dominio aclarado para texto sobre el cielo (contraste ≥ 4.5:1 con la noche). */
fun domainTextColor(key: String): Color = lerp(domainOf(key)?.color ?: Color.White, Color.White, 0.45f)

/** Un juego como planetita de arcilla (color de su dominio + ícono); [done] = ✓ lima de "ya jugado hoy". */
@Composable
fun MiniPlanet(gameId: String, size: Dp, modifier: Modifier = Modifier, done: Boolean = false) {
  val color = GameRegistry.getById(gameId)?.domain?.color ?: Clay.Grape
  Box(modifier.size(size), contentAlignment = Alignment.Center) {
    Box(
      Modifier
        .size(size)
        .drawBehind {
          val rr = this.size.minDimension / 2f
          drawCircle(Clay.Ink, rr, Offset(center.x, center.y + 2.5.dp.toPx()))
          drawCircle(color, rr, center)
          drawCircle(Clay.Ink, rr - 1.dp.toPx(), center, style = Stroke(2.dp.toPx()))
        }
    )
    GameIcon(gameId, size * 0.82f)
    if (done) {
      Box(
        Modifier
          .align(Alignment.TopEnd)
          .offset(x = 4.dp, y = (-4).dp)
          .size(size * 0.45f)
          .clip(CircleShape)
          .background(Clay.Lime)
          .border(2.dp, Clay.Ink, CircleShape),
        contentAlignment = Alignment.Center
      ) { Icon(Icons.Default.Check, contentDescription = "Jugado hoy", tint = Clay.Ink, modifier = Modifier.size(size * 0.32f)) }
    }
  }
}
