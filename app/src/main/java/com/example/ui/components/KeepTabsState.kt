package com.example.ui.components

import androidx.compose.runtime.Composable
import androidx.compose.runtime.saveable.rememberSaveableStateHolder

/**
 * Compone [content] (el pager de las tres pestañas) solo mientras [show], y al esconderlo GUARDA su estado para devolverlo igual cuando vuelva (Tarea 209b, 1-nov).
 *
 * `MainActivity` saca el pager de la composición mientras hay un juego, un resultado, el resumen del día, «Todos los juegos» o un panel de arriba (Ajustes, «En qué se basa Nubi», «Preguntas frecuentes», «De dónde sale la referencia»): esas pantallas van sobre el cielo transparente y no deben dejar
 * pasar toques a la pestaña de atrás. Pero una página que sale de la composición pierde su `rememberScrollState()` y su `rememberLazyListState()`: al volver de «De dónde sale la referencia» la página Contrarreloj arrancaba arriba, aunque la fila que lo abre está a mitad de página, y lo mismo pasaba
 * con Tú al volver de «En qué se basa Nubi». Un `SaveableStateHolder` guarda lo que las páginas recuerdan con `rememberSaveable` (los deslizados lo hacen) cuando salen de la composición y lo restaura al volver a entrar. Lo que vive en el ViewModel (la pestaña, la página abierta de un área) no cambia.
 */
@Composable
fun KeepTabsState(show: Boolean, content: @Composable () -> Unit) {
  val holder = rememberSaveableStateHolder()
  if (show) holder.SaveableStateProvider(TABS_STATE_KEY, content)
}

private const val TABS_STATE_KEY = "tabs"
