package com.example.ui.components

import androidx.compose.animation.core.AnimationSpec
import androidx.compose.animation.core.CubicBezierEasing
import androidx.compose.animation.core.Easing
import androidx.compose.animation.core.snap
import androidx.compose.animation.core.tween

/**
 * La escala de movimiento ÚNICA de la app (Tarea 123; Fable, `auditoria-fable/pulido-app-17-oct.md` §3.3). Material 3 usa tres duraciones y dos curvas; Duolingo y Headspace, lo mismo en espíritu: rápido para lo que responde al dedo,
 * medio para cambiar de lugar y largo solo para celebrar. Todo tiempo nuevo sale de aquí.
 *
 * | Clase | Duración | Curva | Dónde |
 * |---|---|---|---|
 * | Respuesta al dedo | 80 al apretar, 140 al soltar | suave | losas y discos del kit (`ClayKit`): la plasticina real se hunde rápido y recupera su forma un poco más lento |
 * | Cambio de estado | 150-200 | estándar | losa de la barra (150), interruptor (150), velo de la lámina (200) |
 * | Cambio de lugar | 300 pestaña · 400 lámina | enfatizada (arranca rápido, frena suave) | `animateScrollToPage`, la lámina de un lugar (sin espera) y los pasos del Primer vuelo (300) |
 * | Celebración | 600-900 | resorte suave | pulso de farola 600 y saltito 900 (`RouteMotion`) |
 *
 * Con «quitar animaciones» ([rememberReduceMotion]) todo cae a `snap()` y la lámina aparece en su sitio con un fundido de [REDUCED_FADE_MS]. `NubiMotionTest` fija la tabla.
 */
object NubiMotion {
  /** Al apoyar el dedo en la losa. */
  const val PRESS_MS = 80
  /** Al soltarlo: más lento que al apretar. */
  const val RELEASE_MS = 140

  /** Cambio de estado, rápido (losa de la barra, interruptor). */
  const val STATE_FAST_MS = 150
  /** Cambio de estado, normal (velo de la lámina, aparición de un rótulo). */
  const val STATE_MS = 200

  /** Pasar de una pestaña a otra. */
  const val TAB_MS = 300
  /** La lámina de un lugar sube (y baja). Sin espera: sale al toque. */
  const val SHEET_MS = 400

  /** Pulso de una farola de la ruta. */
  const val CELEBRATE_MS = 600
  /** El saltito de Nubi al completar la ruta. */
  const val CELEBRATE_LONG_MS = 900

  /** «Quitar animaciones»: el fundido corto que reemplaza a todo movimiento de lugar. */
  const val REDUCED_FADE_MS = 150

  /** «Enfatizada» de Material 3 (la de salir): arranca rápido y frena suave. */
  val Emphasized: Easing = CubicBezierEasing(0.05f, 0.7f, 0.1f, 1f)
  /** «Estándar» de Material 3. */
  val Standard: Easing = CubicBezierEasing(0.2f, 0f, 0f, 1f)

  /** La animación de [ms] ms con [easing], o un salto (`snap()`) si el sistema pide quitar animaciones. */
  fun <T> spec(reduce: Boolean, ms: Int, easing: Easing = Standard): AnimationSpec<T> = if (reduce) snap() else tween(ms, easing = easing)

  /** La respuesta al dedo: [PRESS_MS] al apretar y [RELEASE_MS] al soltar (un salto con «quitar animaciones»). */
  fun <T> finger(reduce: Boolean, pressed: Boolean): AnimationSpec<T> = if (reduce) snap() else tween(if (pressed) PRESS_MS else RELEASE_MS)
}
