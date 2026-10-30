@file:OptIn(androidx.compose.foundation.ExperimentalFoundationApi::class)

package com.example.ui.components

import androidx.compose.foundation.relocation.BringIntoViewRequester
import androidx.compose.foundation.relocation.bringIntoViewRequester
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.compositionLocalOf
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.composed
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.layout.boundsInRoot
import androidx.compose.ui.layout.onGloballyPositioned

/** Los tres lugares que la guía de Nubi en Hoy puede iluminar (Tarea 193, `HoyGuide`). */
enum class GuideTarget { TIME_TRIAL, ALL_GAMES, TAB_BAR }

/**
 * Dónde están, en coordenadas de la raíz de Compose, los elementos reales que la guía ilumina. Cada elemento se publica solo (`Modifier.guideTarget`) y se mide en cada cambio de tamaño o de lugar (letra del sistema, el enlace del punto de partida, deslizar Hoy);
 * así el hueco del velo siempre cae sobre el elemento verdadero y no sobre una copia. Un elemento que no está en pantalla no tiene rectángulo.
 */
class GuideTargets {
  val rects = mutableStateMapOf<GuideTarget, Rect>()
  internal val requesters = mutableMapOf<GuideTarget, BringIntoViewRequester>()

  /** Trae el elemento a la vista si Hoy se desliza (letra 2,0): la guía lo pide antes de cada paso y recién después calcula el hueco. */
  suspend fun bringIntoView(target: GuideTarget) {
    requesters[target]?.bringIntoView()
  }
}

/** Sin guía (pantallas sueltas, pruebas): publicar un elemento no hace nada. */
val LocalGuideTargets = compositionLocalOf<GuideTargets?> { null }

/** Publica este elemento como un lugar que la guía puede iluminar; lo único que agrega es medirlo y poder traerlo a la vista: no cambia nada de lo que se ve. */
fun Modifier.guideTarget(target: GuideTarget): Modifier = composed {
  val targets = LocalGuideTargets.current
  if (targets == null) {
    this
  } else {
    val requester = remember { BringIntoViewRequester() }
    DisposableEffect(targets, target) {
      targets.requesters[target] = requester
      onDispose { targets.requesters.remove(target); targets.rects.remove(target) }
    }
    this.bringIntoViewRequester(requester).onGloballyPositioned { targets.rects[target] = it.boundsInRoot() }
  }
}
