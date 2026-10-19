package com.example.ui.components

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.CubicBezierEasing
import androidx.compose.animation.core.tween
import androidx.compose.runtime.Stable
import com.example.data.PiezasPlan
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

/**
 * La entrada de las piezas nuevas de la isla en Hoy (Tarea 136, «Tu isla crece»): cada pieza del [plan] pasa de 0 a 1 ([progress]) en [NubiMotion.CELEBRATE_MS] (600 ms, un resorte suave: `EaseOutBack`), de a una con [STAGGER_MS] ms de separación
 * (como las farolas, `RouteMotion.STAGGER_MS`). Quien dibuja (`Island`) lee [progress] al DIBUJAR (alfa 0 → 1 y escala 0,9 → 1 desde el ancla de la pieza), así que nada recompone cuadro a cuadro. Las piezas que no están en el plan valen 1 (puestas).
 * **Sin sonido ni vibración**: ya los dio la celebración de la medalla. Con [reduceMotion] («quitar animaciones») las piezas nacen puestas y [run] no hace nada.
 */
@Stable
internal class PieceFx(val plan: PiezasPlan = PiezasPlan.NONE, reduceMotion: Boolean = false) {
  val moves: Boolean = !reduceMotion

  private val pieces: Map<String, Animatable<Float, androidx.compose.animation.core.AnimationVector1D>> = plan.reveal.associate { it.id to Animatable(if (moves) 0f else 1f) }

  /** Cuánto lleva revelada la pieza [id] (0 = sin aparecer, 1 = puesta; el resorte pasa un poco de 1 y vuelve). */
  fun progress(id: String): Float = pieces[id]?.value ?: 1f

  /** Revela las piezas del [plan], una tras otra. Termina cuando la última quedó puesta. */
  suspend fun run() {
    if (!moves || plan.isEmpty) return
    coroutineScope {
      plan.reveal.forEachIndexed { i, piece ->
        launch {
          delay(i * STAGGER_MS)
          pieces.getValue(piece.id).animateTo(1f, tween(NubiMotion.CELEBRATE_MS, easing = EaseOutBackSoft))
        }
      }
    }
  }

  companion object {
    const val STAGGER_MS = 250L

    /** `EaseOutBack` suave: pasa ~4 % de la meta y vuelve (`easeOutBack` de CSS con una sobrepasada chica; es «celebrar», no «dedo»). */
    val EaseOutBackSoft = CubicBezierEasing(0.34f, 1.25f, 0.64f, 1f)

    /** El alfa de una pieza con avance [p]: el resorte pasa de 1, el alfa no. */
    fun alpha(p: Float): Float = p.coerceIn(0f, 1f)

    /** La escala de una pieza con avance [p]: 0,9 → 1 (con el resorte, un poco más al pasar de 1). */
    fun scale(p: Float): Float = 0.9f + 0.1f * p
  }
}
