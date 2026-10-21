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
 * Cuántas ventanas tapan ahora las pestañas (la lámina de un área, en Hoy, Juegos y Avance; la ficha de un
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
 * plasticina celeste = tu perfil, y el engranaje de plasticina = opciones (Ajustes). Discos de 48 dp (kit de la Tarea 90) con nombre para el lector.
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
      ClayDisc(DiscStyle.SKY, onProfile, profileLabel, Modifier.testTag("btn_top_profile")) {
        val initial = name.trim().firstOrNull()?.uppercase()
        if (initial != null) Text(initial, color = Clay.Ink, fontSize = 20.sp, fontWeight = FontWeight.Bold)
        else KitGlyph(Glyph.PERSON)
      }
      if (profileBadge != null) Box(Modifier.align(Alignment.BottomEnd).offset(x = 6.dp, y = 6.dp).clearAndSetSemantics { }) { profileBadge() }
    }
    Spacer(Modifier.width(8.dp))
    ClayDisc(DiscStyle.CREAM, onSettings, "Opciones", Modifier.testTag("btn_top_settings")) { KitGlyph(Glyph.GEAR, size = 28.dp) }
  }
}
