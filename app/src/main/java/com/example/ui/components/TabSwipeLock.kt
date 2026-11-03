package com.example.ui.components

import androidx.compose.runtime.MutableIntState
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.staticCompositionLocalOf

/**
 * Cuántas ventanas tapan ahora las pestañas (la página de un área, la ficha de un juego...; la pestaña Juegos NO cuenta: es una pestaña, Tarea 220). Mientras haya alguna, el dedo NO cambia de pestaña (29-sep, Ricardo: dos gestos hacia los costados confundían)
 * y la barra de abajo se retira. Lo lee el HorizontalPager de MainActivity. (Antes vivía en `TopActions.kt`, con los dos botones de arriba a la derecha que se fueron en la Tarea 158.)
 */
val LocalTabSwipeLock = staticCompositionLocalOf<MutableIntState> { mutableIntStateOf(0) }
