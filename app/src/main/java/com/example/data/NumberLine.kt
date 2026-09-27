package com.example.data

import kotlin.math.abs

/**
 * Lectura de "tu línea" en Aterrizaje Lunar: en qué tramo de la regla te alejas más del blanco, y un truco para ese
 * tramo. Recibe, por aterrizaje, dónde estaba el blanco y dónde se posó la nave (0..1 del largo de la regla).
 *
 * Por qué así y no "agrandas los números chicos / achicas los grandes" (lo que decía antes):
 * - Ubicar un número en una regla con dos extremos NO mide solo el sentido numérico: en adultos se resuelve sobre todo
 *   como un juicio de proporción, apoyándose en puntos de referencia (los extremos, la mitad, los cuartos) (Barth y
 *   Paladino, 2011; seguimiento ocular: Sullivan et al., 2011), y la relación de la tarea con el rendimiento en
 *   matemáticas se explica en buena parte por habilidades visoespaciales y visomotoras (Simms et al., 2016).
 * - El error con signo cerca de los extremos está sesgado por construcción: cerca del 0 solo se puede errar hacia la
 *   derecha, y cerca del final hacia la izquierda. Con pocos aterrizajes y ruido, "agrandar los chicos y achicar los
 *   grandes" aparece aunque no haya ningún sesgo real (y la curva logarítmica de Siegler y Opfer, 2003, se describió en
 *   niños, no en adultos).
 * Por eso se usa la distancia al blanco SIN signo por tramo (descriptivo, sin esa trampa) y se da un consejo concreto:
 * medir desde la referencia más cercana, la estrategia de quienes mejor estiman.
 */
enum class NumberLineReading { SIN_DATOS, PAREJA, INICIO, CENTRO, FINAL }

object NumberLine {
  /** Aterrizajes mínimos en un tramo para compararlo. */
  const val MIN_PER_ZONE = 2
  /** Para nombrar el tramo más difícil: al menos 3 puntos (% de la regla) más lejos que el más fácil... */
  const val MIN_GAP = 0.03f
  /** ...y al menos 1,5 veces su distancia. */
  const val MIN_RATIO = 1.5f

  /** Tramo de la regla: 0 = primer tercio, 1 = centro, 2 = último tercio. */
  fun zoneOf(fraction: Float): Int = when {
    fraction < 1f / 3f -> 0
    fraction > 2f / 3f -> 2
    else -> 1
  }

  /** Distancia media al blanco (sin signo, fracción de la regla) en cada tramo; null si hay pocos aterrizajes ahí. */
  fun zoneErrors(trueFractions: List<Float>, givenFractions: List<Float>): List<Float?> {
    val n = minOf(trueFractions.size, givenFractions.size)
    return (0..2).map { zone ->
      val errs = (0 until n).filter { zoneOf(trueFractions[it]) == zone }.map { abs(givenFractions[it] - trueFractions[it]) }
      if (errs.size >= MIN_PER_ZONE) errs.average().toFloat() else null
    }
  }

  fun reading(trueFractions: List<Float>, givenFractions: List<Float>): NumberLineReading {
    val errs = zoneErrors(trueFractions, givenFractions)
    val rated = (0..2).filter { errs[it] != null }
    if (rated.size < 2) return NumberLineReading.SIN_DATOS
    val worst = rated.maxBy { errs[it]!! }
    val best = rated.minBy { errs[it]!! }
    val w = errs[worst]!!
    val b = errs[best]!!
    if (w - b < MIN_GAP || w < MIN_RATIO * b) return NumberLineReading.PAREJA
    return when (worst) {
      0 -> NumberLineReading.INICIO
      1 -> NumberLineReading.CENTRO
      else -> NumberLineReading.FINAL
    }
  }

  fun message(reading: NumberLineReading): String = when (reading) {
    NumberLineReading.SIN_DATOS -> "Cada punto es un aterrizaje: la marca es el blanco y el punto, dónde te posaste."
    NumberLineReading.PAREJA -> "Te posas igual de cerca del blanco en toda la regla."
    NumberLineReading.INICIO ->
      "Donde más te alejas del blanco: el primer tercio de la regla. Truco: mide desde el 0 y desde la mitad; el cuarto queda justo entre los dos."
    NumberLineReading.CENTRO ->
      "Donde más te alejas del blanco: el centro de la regla. Truco: ubica primero la mitad exacta y corrige desde ahí."
    NumberLineReading.FINAL ->
      "Donde más te alejas del blanco: el último tercio de la regla. Truco: mide hacia atrás desde el final, no solo desde el 0."
  }
}
