package com.example.data

import com.example.model.GamePlayResult
import java.util.Locale

/**
 * El «primer dato» en palabras de la versión corta del inicio (Freno de Emergencia, Aterrizaje Lunar y Lluvia de meteoros).
 *
 * Se lee en la app, con lo que la telemetría de cada juego YA trae (altos frenados, error medio de la regla, palabras reales vistas y tocadas): así no
 * hace falta otro campo ni otro viaje desde Unity, y la frase queda en un solo lugar con pruebas. Es una frase corta y literal, sin interpretar:
 * dice lo que pasó, no lo que «eres». Con muy pocos casos no dice nada (devuelve null y la pantalla muestra solo el puntaje).
 *
 *  - Freno: «Frenaste a tiempo 6 de 8 veces.»  (necesita ≥ 4 altos)
 *  - Aterrizaje: «Tus aterrizajes quedaron, en promedio, a 4,2 % del largo de la regla del lugar justo.»  (necesita el error medio)
 *  - Meteoros: «Reconociste 9 de 11 palabras reales.»  (necesita ≥ 6 palabras reales vistas)
 */
object FirstData {
  const val MIN_STOPS = 4
  const val MIN_WORDS = 6

  fun phrase(result: GamePlayResult): String? = when (result.gameId) {
    "freno" -> brake(result.stopsOk, result.stopsTotal)
    "aterrizaje" -> landing(result.numlineErrorPct)
    "meteoros" -> words(result.lexBandHits, result.lexBandSeen)
    else -> null
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
    val n = String.format(Locale.ROOT, "%.1f", pct).replace('.', ',')
    return "Tus aterrizajes quedaron, en promedio, a $n % del largo de la regla del lugar justo."
  }

  fun words(hits: List<Int>?, seen: List<Int>?): String? {
    val total = seen.orEmpty().sum()
    if (total < MIN_WORDS) return null
    val ok = hits.orEmpty().sum().coerceIn(0, total)
    return "Reconociste $ok de $total palabras reales."
  }
}
