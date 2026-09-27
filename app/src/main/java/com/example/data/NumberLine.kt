package com.example.data

/**
 * Lectura de "tu línea" en Aterrizaje Lunar: si hay un sesgo al ubicar números en la regla. Un patrón muy estudiado
 * (Siegler y Opfer, 2003) es "agrandar" los números chicos (ponerlos más a la derecha de donde van) y "achicar" los
 * grandes: la línea mental se comprime hacia el final. Recibe, por aterrizaje, dónde estaba el blanco y dónde se posó
 * la nave (0..1 del largo de la regla).
 */
enum class NumberLineBias { SIN_DATOS, PAREJA, AGRANDA_CHICOS, ACHICA_GRANDES, COMPRIME }

object NumberLine {
  /** Diferencia media (con signo) que ya cuenta como sesgo: 3% del largo de la regla. */
  const val BIAS_THRESHOLD = 0.03f

  fun bias(trueFractions: List<Float>, givenFractions: List<Float>): NumberLineBias {
    val n = minOf(trueFractions.size, givenFractions.size)
    val low = (0 until n).filter { trueFractions[it] < 1f / 3f }.map { givenFractions[it] - trueFractions[it] }
    val high = (0 until n).filter { trueFractions[it] > 2f / 3f }.map { givenFractions[it] - trueFractions[it] }
    if (low.size < 2 && high.size < 2) return NumberLineBias.SIN_DATOS
    val lowUp = low.size >= 2 && low.average() > BIAS_THRESHOLD
    val highDown = high.size >= 2 && high.average() < -BIAS_THRESHOLD
    return when {
      lowUp && highDown -> NumberLineBias.COMPRIME
      lowUp -> NumberLineBias.AGRANDA_CHICOS
      highDown -> NumberLineBias.ACHICA_GRANDES
      else -> NumberLineBias.PAREJA
    }
  }

  fun message(bias: NumberLineBias): String = when (bias) {
    NumberLineBias.SIN_DATOS -> "Cada punto es un aterrizaje: la marca es el blanco y el punto, dónde te posaste."
    NumberLineBias.PAREJA -> "Tu línea es pareja: aciertas igual con números chicos y grandes."
    NumberLineBias.AGRANDA_CHICOS -> "Tiendes a poner los números chicos más a la derecha de donde van. Es muy común: la mente los \"agranda\"."
    NumberLineBias.ACHICA_GRANDES -> "Tiendes a poner los números grandes más a la izquierda de donde van."
    NumberLineBias.COMPRIME -> "Agrandas los números chicos y achicas los grandes: tu línea se aprieta hacia el final. Es un patrón muy estudiado y se corrige con práctica."
  }
}
