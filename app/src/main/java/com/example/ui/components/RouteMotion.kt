package com.example.ui.components

import kotlin.math.PI
import kotlin.math.abs
import kotlin.math.cos
import kotlin.math.pow
import kotlin.math.sin

/**
 * Los tiempos y las formas del encendido de las farolas de la ruta y de la celebración de la ruta completa (Tarea 88). Todo es matemática pura (se prueba sin pantalla); lo anima `RouteFx` y lo dibuja `Island`.
 * Es movimiento decorativo: premia la constancia (haber hecho la parada), nunca el puntaje, y no cambia la cara de Nubi según el desempeño. Con «quitar animaciones» nada de esto se usa: las farolas aparecen encendidas, sin pulso, mirada, saltito ni chispas.
 */
object RouteMotion {
  /** El halo de una farola crece de 0 a su tamaño con un pulso suave. */
  const val PULSE_MS = NubiMotion.CELEBRATE_MS
  /** Cuánto mira Nubi hacia el lugar de la farola que se enciende (entrar, mirar y volver al frente, todo dentro de 1,5 s). */
  const val LOOK_TOTAL_MS = 1500
  /** Si se encienden varias farolas a la vez (volver de una sesión entera), se encienden una tras otra con esta separación y Nubi no mira a ninguna. */
  const val STAGGER_MS = 250
  /** El saltito alegre de Nubi al completar la ruta: dos saltos en este tiempo. */
  const val JUMP_MS = NubiMotion.CELEBRATE_LONG_MS
  const val JUMP_DP = 12f
  /** El leve giro de cada salto, en grados (a un lado en el primero y al otro en el segundo). */
  const val JUMP_TILT_DEG = 5f
  /** Las chispas de las farolas: cuánto duran, cuántas salen de cada farola y cuánto se alejan (fracción del ancho de la isla). */
  const val SPARKS_MS = 1100
  const val SPARKS_PER_LAMP = 4
  const val SPARK_TRAVEL = 0.05f
  const val SPARK_RADIUS = 0.007f

  /** Cuadros clave del tamaño del halo (progreso → escala): crece, pasa un poco de su tamaño y se asienta. Termina en 1: encendida y quieta. */
  private val PULSE_KEYS = listOf(0f to 0f, 0.5f to 1.12f, 0.78f to 0.97f, 1f to 1f)

  /** La escala del halo y del disco en el progreso [p] (0..1) del encendido: 0 = apagada, 1 = encendida. */
  fun pulseScale(p: Float): Float = PlaceMotion.sample(PULSE_KEYS, p)

  /** La opacidad del encendido en [p]: sube con el pulso y nunca pasa de 1. */
  fun pulseAlpha(p: Float): Float = pulseScale(p).coerceIn(0f, 1f)

  /** Cuánto sube Nubi (dp) en el progreso [p] (0..1) del saltito: dos arcos, 0 al empezar, a la mitad y al terminar. */
  fun jumpLiftDp(p: Float): Float = JUMP_DP * abs(sin(2.0 * PI * p.coerceIn(0f, 1f))).toFloat()

  /** El giro de Nubi (grados) en [p]: a un lado en el primer salto y al otro en el segundo. */
  fun jumpTiltDeg(p: Float): Float = JUMP_TILT_DEG * sin(2.0 * PI * p.coerceIn(0f, 1f)).toFloat()

  /** La opacidad de una chispa en el progreso [p] (0..1): arranca entera y se apaga. En 1 (reposo) no se ve. */
  fun sparkAlpha(p: Float): Float = (1f - p.coerceIn(0f, 1f)).pow(1.3f)

  /** Cuánto se ha alejado una chispa de su farola (fracción de [SPARK_TRAVEL]) en [p]: sale rápido y se frena. */
  fun sparkTravel(p: Float): Float = 1f - (1f - p.coerceIn(0f, 1f)).pow(2)

  /** Los ángulos (grados, 0 = derecha, −90 = arriba) de las chispas de la farola [lampIndex]: un abanico hacia arriba, con una vuelta distinta por farola para que no salgan iguales. */
  fun sparkAngles(lampIndex: Int): List<Float> = (0 until SPARKS_PER_LAMP).map { k -> -90f + (k - (SPARKS_PER_LAMP - 1) / 2f) * 38f + lampIndex * 4f }

  /** El punto (dx, dy normalizados al ancho de la isla) de una chispa de ángulo [angleDeg] en el progreso [p]. */
  fun sparkOffset(angleDeg: Float, p: Float): Pair<Float, Float> {
    val r = SPARK_TRAVEL * sparkTravel(p)
    val a = Math.toRadians(angleDeg.toDouble())
    return (r * cos(a)).toFloat() to (r * sin(a)).toFloat()
  }
}
