package com.example.ui.components

import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.MutableIntState
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.staticCompositionLocalOf

/**
 * Cuántas ventanas tapan ahora las pestañas (la lámina de un lugar o de un área, «Todos los juegos», la ficha de un juego...). Mientras haya alguna, el dedo NO cambia de pestaña (29-sep, Ricardo: dos gestos hacia los costados confundían)
 * y la barra de abajo se retira. Lo lee el HorizontalPager de MainActivity. (Antes vivía en `TopActions.kt`, con los dos botones de arriba a la derecha que se fueron en la Tarea 158.)
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
