package com.example.data

import com.example.model.GamePlayResult
import kotlin.math.roundToInt

/**
 * El «primer dato» en palabras de la versión corta del inicio (Rastro de luz, Freno de Emergencia, Aterrizaje Lunar y Lluvia de meteoros).
 *
 * Se lee en la app, con lo que la telemetría de cada juego YA trae (rastro repetido, altos frenados, error medio de la regla, palabras reales vistas y tocadas): así no
 * hace falta otro campo ni otro viaje desde Unity, y la frase queda en un solo lugar con pruebas. Es una frase corta y literal, sin interpretar:
 * dice lo que pasó, no lo que «eres». Con muy pocos casos no dice nada (devuelve null y la pantalla muestra solo el puntaje).
 *
 *  - Rastro de luz: «Repetiste bien un rastro de 5 luces.»  (necesita un rastro simple repetido entero)
 *  - Freno: «Frenaste a tiempo 6 de 8 veces.»  (necesita ≥ 4 altos)
 *  - Aterrizaje: «En promedio, aterrizaste a un 4 % de distancia del lugar justo.»  (necesita el error medio; redondeado, sin decimales)
 *  - Meteoros: «Reconociste 9 de 11 palabras reales.»  (necesita ≥ 6 palabras reales vistas)
 */
object FirstData {
  const val MIN_STOPS = 4
  const val MIN_WORDS = 6

  fun phrase(result: GamePlayResult): String? = when (result.gameId) {
    "secuencia" -> trail(result.rasBestLen)
    "freno" -> brake(result.stopsOk, result.stopsTotal)
    "aterrizaje" -> landing(result.numlineErrorPct)
    "meteoros" -> words(result.lexBandHits, result.lexBandSeen)
    else -> null
  }

  fun trail(bestLen: List<Int>?): String? {
    val n = Trail.bestTrail(bestLen) ?: return null
    return "Repetiste bien un rastro de ${Trail.lights(n)}."
  }

  fun brake(stopsOk: Int?, stopsTotal: Int?): String? {
    val total = stopsTotal ?: return null
    val ok = (stopsOk ?: return null).coerceIn(0, total)
    if (total < MIN_STOPS) return null
    return "Frenaste a tiempo $ok de $total ${if (total == 1) "vez" else "veces"}."
  }

  fun landing(errorPct: Float?): String? {
    val pct = errorPct ?: return null
    if (pct < 0f) return null
    return "En promedio, aterrizaste a un ${pct.roundToInt()} % de distancia del lugar justo."
  }

  fun words(hits: List<Int>?, seen: List<Int>?): String? {
    val total = seen.orEmpty().sum()
    if (total < MIN_WORDS) return null
    val ok = hits.orEmpty().sum().coerceIn(0, total)
    return "Reconociste $ok de $total palabras reales."
  }
}
