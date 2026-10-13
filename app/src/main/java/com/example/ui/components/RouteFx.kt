package com.example.ui.components

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.tween
import androidx.compose.runtime.Stable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import com.example.data.RoutePlan
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

/**
 * El estado y las animaciones del encendido de las farolas de la ruta de hoy (Tarea 88): el pulso de cada farola que se enciende ahora por primera vez, la mirada de Nubi hacia su lugar y, al completar la ruta, el saltito de Nubi con las chispas.
 * Cada pieza es un [Animatable] que lee quien dibuja (`Island`): nada recompone cuadro a cuadro. [plan] dice qué animar; todo lo demás ya está encendido y quieto. Con [reduceMotion] («quitar animaciones») las farolas del plan nacen encendidas y [run] no hace nada.
 */
@Stable
internal class RouteFx(val plan: RoutePlan = RoutePlan.NONE, reduceMotion: Boolean = false) {
  val moves: Boolean = !reduceMotion

  /** El progreso del encendido de cada farola del plan (0 = apagada, 1 = encendida y quieta). Las que no están en el plan valen 1. */
  private val pulses: Map<String, Animatable<Float, androidx.compose.animation.core.AnimationVector1D>> = plan.pulse.associateWith { Animatable(if (moves) 0f else 1f) }

  fun pulseProgress(placeKey: String): Float = pulses[placeKey]?.value ?: 1f

  /** El lugar al que mira Nubi mientras se enciende su farola (null = de frente) y cuánto (0 → 1). */
  var lookKey by mutableStateOf<String?>(null)
  val look = Animatable(0f)

  /** El saltito de la ruta completa (0 → 1; en 0 y en 1 Nubi está en su sitio) y las chispas (0 → 1; en 1 no se ven). */
  val jump = Animatable(0f)
  val sparks = Animatable(1f)

  /** Corre lo que dice el [plan]: los pulsos (y la mirada si se enciende una sola farola) y, si la ruta quedó completa, el saltito con las chispas. Termina cuando todo volvió a su sitio. */
  suspend fun run() {
    if (!moves || plan.isEmpty) return
    coroutineScope {
      plan.pulse.forEachIndexed { i, key ->
        launch {
          delay(i * RouteMotion.STAGGER_MS.toLong())
          pulses.getValue(key).animateTo(1f, tween(RouteMotion.PULSE_MS, easing = LinearEasing))
        }
      }
      if (plan.pulse.size == 1) {
        launch {
          lookKey = plan.pulse.single()
          look.animateTo(1f, tween(PlaceMotion.LOOK_SHIFT_MS))
          delay((RouteMotion.LOOK_TOTAL_MS - 2 * PlaceMotion.LOOK_SHIFT_MS).toLong())
          look.animateTo(0f, tween(PlaceMotion.LOOK_SHIFT_MS))
          lookKey = null
        }
      }
    }
    if (plan.complete) {
      delay(120)
      coroutineScope {
        launch { jump.snapTo(0f); jump.animateTo(1f, tween(RouteMotion.JUMP_MS, easing = LinearEasing)) }
        launch { sparks.snapTo(0f); sparks.animateTo(1f, tween(RouteMotion.SPARKS_MS, easing = LinearEasing)) }
      }
    }
  }
}
