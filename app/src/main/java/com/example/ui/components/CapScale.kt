package com.example.ui.components

import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.Density

/**
 * La letra de [content] no pasa de [max] veces el tamaño base (los números y títulos grandes de la página Contrarreloj y los rótulos de la barra de pestañas: con 2,0 se saldrían del margen o de su columna). Antes vivía en `ContrarrelojPage` (Tarea 203);
 * pasó aquí en la Tarea 215 porque la barra de abajo la usa también (desde la Tarea 220 con un tope dinámico: [com.example.ui.components.navTextScaleCap]).
 */
@Composable
internal fun CapScale(max: Float, content: @Composable () -> Unit) {
  val d = LocalDensity.current
  if (d.fontScale <= max) { content(); return }
  CompositionLocalProvider(LocalDensity provides Density(d.density, max)) { content() }
}
