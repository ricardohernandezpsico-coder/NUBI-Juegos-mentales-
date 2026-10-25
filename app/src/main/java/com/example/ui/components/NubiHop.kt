package com.example.ui.components

import kotlin.math.PI
import kotlin.math.abs
import kotlin.math.sin

/**
 * El saltito alegre de Nubi (Tarea 88; lo da `NubiClay(celebrate = true)`): dos saltos de [JUMP_DP] con un giro leve, en [JUMP_MS]. Todo es matemática pura (se prueba sin pantalla).
 * Es movimiento decorativo: no cambia la cara de Nubi según el desempeño, y con «quitar animaciones» no se usa. (Vivía en `RouteMotion`, junto al encendido de las farolas de la ruta y las chispas, que se retiraron de Hoy en la Tarea 165 y se borraron en la 168.)
 */
object NubiHop {
  /** El saltito: dos saltos en este tiempo. */
  const val JUMP_MS = NubiMotion.CELEBRATE_LONG_MS
  const val JUMP_DP = 12f
  /** El leve giro de cada salto, en grados (a un lado en el primero y al otro en el segundo). */
  const val JUMP_TILT_DEG = 5f

  /** Cuánto sube Nubi (dp) en el progreso [p] (0..1) del saltito: dos arcos, 0 al empezar, a la mitad y al terminar. */
  fun jumpLiftDp(p: Float): Float = JUMP_DP * abs(sin(2.0 * PI * p.coerceIn(0f, 1f))).toFloat()

  /** El giro de Nubi (grados) en [p]: a un lado en el primer salto y al otro en el segundo. */
  fun jumpTiltDeg(p: Float): Float = JUMP_TILT_DEG * sin(2.0 * PI * p.coerceIn(0f, 1f)).toFloat()
}
