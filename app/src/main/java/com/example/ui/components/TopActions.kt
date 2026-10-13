package com.example.ui.components

import androidx.annotation.DrawableRes
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.offset
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.MutableIntState
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.BlendMode
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.role
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.R
import com.example.ui.theme.Clay
import kotlin.math.PI
import kotlin.math.hypot
import kotlin.math.sin

/**
 * Cuántas ventanas tapan ahora las pestañas (la de un área en Juegos, el detalle de un área en Hoy, la ficha de un
 * juego...). Mientras haya alguna, el dedo NO cambia de pestaña (29-sep, Ricardo: en Juegos dos gestos hacia los
 * costados confundían). Lo lee el HorizontalPager de MainActivity.
 */
val LocalTabSwipeLock = staticCompositionLocalOf<MutableIntState> { mutableIntStateOf(0) }

/** Ponerlo dentro de una ventana: mientras esté en pantalla, las pestañas no se deslizan. */
@Composable
fun LockTabSwipe() {
  val lock = LocalTabSwipeLock.current
  DisposableEffect(lock) {
    lock.intValue++
    onDispose { lock.intValue-- }
  }
}

/**
 * Arriba a la derecha en las 3 pestañas (aprobado por Ricardo, `docs/previews/navegacion-nubi.png`): la inicial en
 * arcilla celeste = tu perfil, y el engranaje = opciones (Ajustes). Botones de 44 dp con nombre para el lector.
 */
@Composable
fun TopActions(
  name: String,
  onProfile: () -> Unit,
  onSettings: () -> Unit,
  modifier: Modifier = Modifier,
  /** Lo que dice el lector de pantalla del botón del perfil (Hoy le suma la liga). */
  profileLabel: String = "Tu perfil",
  /** Una insignia chica en la esquina de abajo a la derecha del perfil (Hoy: el escudo de tu liga). Es decorativa: la liga va en [profileLabel]. */
  profileBadge: (@Composable () -> Unit)? = null
) {
  Row(modifier, verticalAlignment = Alignment.CenterVertically) {
    Box {
      ClayCircle(Clay.Sky, profileLabel, onProfile, Modifier.testTag("btn_top_profile")) {
        val initial = name.trim().firstOrNull()?.uppercase()
        if (initial != null) Text(initial, color = Clay.Ink, fontSize = 20.sp, fontWeight = FontWeight.Bold)
        else Icon(Icons.Default.Person, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(24.dp))
      }
      if (profileBadge != null) Box(Modifier.align(Alignment.BottomEnd).offset(x = 6.dp, y = 6.dp).clearAndSetSemantics { }) { profileBadge() }
    }
    Spacer(Modifier.width(8.dp))
    ClayCircle(Clay.Cream, "Opciones", onSettings, Modifier.testTag("btn_top_settings")) {
      Icon(Icons.Default.Settings, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(26.dp))
    }
  }
}

@Composable
private fun ClayCircle(color: Color, label: String, onClick: () -> Unit, modifier: Modifier, content: @Composable () -> Unit) {
  Box(
    modifier
      .size(44.dp)
      .drawBehind { drawCircle(Clay.Ink, size.minDimension / 2f, center.copy(y = center.y + 3.dp.toPx())) }
      .clip(CircleShape)
      .background(color)
      .border(2.5.dp, Clay.Ink, CircleShape)
      .clickable(onClick = onClick)
      .semantics { contentDescription = label; role = Role.Button },
    contentAlignment = Alignment.Center
  ) { content() }
}

/**
 * Planeta de cada área (esfera de color con su ícono blanco; Atención = diana), horneado por
 * `tools/previews/nubi_recursos.py` en drawable-nodpi/area_*.webp. La esfera mide [AREA_BODY_FRACTION] del ancho.
 */
const val AREA_BODY_FRACTION = 0.6f

@DrawableRes
fun areaPlanetRes(area: String): Int = when (area) {
  "MEMORIA" -> R.drawable.area_memoria
  "ATENCION" -> R.drawable.area_atencion
  "RAZONAMIENTO" -> R.drawable.area_razonamiento
  else -> R.drawable.area_lenguaje
}
