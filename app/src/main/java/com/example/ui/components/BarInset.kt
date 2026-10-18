package com.example.ui.components

import androidx.compose.runtime.compositionLocalOf
import androidx.compose.ui.unit.dp

/**
 * Lo que ocupa abajo la barra de pestañas (Tarea 123). La barra es una capa FIJA encima de las páginas (`MainActivity`), ya no el `bottomBar` de un `Scaffold`: no se encoge ni se mueve cuando se abre una lámina o una ventana, que la tapan, y las páginas no
 * saltan. Cada pestaña (Hoy, Juegos, Avance) deja esa franja libre en su capa de base con `Modifier.padding(bottom = LocalBarInset.current)` (así el contenido termina sobre la barra, nítido y sin el desvanecido de página que había antes), y las ventanas y láminas
 * ocupan TODO el alto y cubren la barra.
 *
 * Vale 0 donde no hay barra (pruebas de una pantalla suelta, el Primer vuelo): la pantalla usa todo su alto.
 */
val LocalBarInset = compositionLocalOf { 0.dp }
