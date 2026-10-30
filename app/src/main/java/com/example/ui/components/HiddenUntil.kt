package com.example.ui.components

import androidx.compose.ui.Modifier
import androidx.compose.ui.input.pointer.PointerEventPass
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.semantics.clearAndSetSemantics

/**
 * Mientras [visible] sea falso: sin lector de pantalla y sin que nada reciba el toque (el contenido todavía no se ve, o hay algo encima que lo tapa). Lo usan el arranque en frío (`StartupGate`: el contenido se compone invisible mientras llegan los datos, Tarea 192)
 * y la guía de Nubi en Hoy (Tarea 193: mientras el velo está abierto, lo de abajo queda fuera de TalkBack y sin toques). Con [visible] verdadero no cambia nada.
 */
fun Modifier.hiddenUntil(visible: Boolean): Modifier =
  if (visible) this
  else clearAndSetSemantics { }.pointerInput(Unit) {
    awaitPointerEventScope { while (true) awaitPointerEvent(PointerEventPass.Initial).changes.forEach { it.consume() } }
  }
