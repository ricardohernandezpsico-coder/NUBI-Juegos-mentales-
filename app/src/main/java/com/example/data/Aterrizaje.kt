package com.example.data

import kotlin.math.roundToInt

/**
 * «Aterrizaje Lunar: la misma tarea, más grande, con más vida y un final con sentido» (id `aterrizaje`, renovado el 10-oct; Unity: `Games/Aterrizaje/LandingContract.cs`; diseño en docs/diseno-aterrizaje.md): la lectura de lo que manda Unity para la pantalla final (§6) y lo que se guarda. Lógica pura con pruebas.
 *
 * El final ordena lo que importa: lo que hiciste («11 % de la regla» de distancia promedio al lugar justo y el dato tuyo, dónde te costó más), tu avance SOLO contigo (hoy, tu promedio y tu mejor, de las partidas ANTERIORES de este juego: nunca percentiles ni otras personas), un truco para la próxima y, abajo y en chico, por qué importa con su fuente.
 * Las cúpulas de tu base (una cada 5 aterrizajes justos) y su récord (preferencias `aterrizaje_record`, que SÍ van en el respaldo) son el premio, no una medida.
 */
object Aterrizaje {
  /** Cada cuántos aterrizajes justos se arma una cúpula (la misma regla que `LandingContract.DomeEvery` en Unity). */
  const val DOME_EVERY = 5

  /** Cuántas partidas anteriores entran en «Tu promedio» y «Tu mejor» (las últimas). */
  const val PREVIOUS_MAX = 10

  const val BOX_TITLE = "Tu distancia promedio al lugar justo"
  const val PROGRESS_TITLE = "Tu avance"
  const val TRICK_TITLE = "Truco para la próxima"
  const val WHY_TITLE = "¿Por qué importa?"
  const val WHY = "Pusiste en juego la estimación en proporción. Ubicar números a ojo se relaciona con cómo valoramos montos, tiempos y riesgos al decidir."
  const val SOURCE = "Fuente: Schley y Peters, 2014"
  const val NOTE = "Medida de esta partida. No es un diagnóstico."

  /** El error medio (en % del largo de la regla): redondeado y nunca menos de 1. null sin dato. */
  fun estimate(errorPct: Float?): Int? = errorPct?.takeIf { it >= 0f && !it.isNaN() }?.let { maxOf(1, it.roundToInt()) }

  /** «11 % de la regla»; «—» sin dato. */
  fun headline(estimate: Int?): String = if (estimate == null) "—" else "$estimate % de la regla"

  /** La línea simple bajo la cifra, con la unidad puesta en algo que se imagina: «Como quedar a 11 en una regla de 0 a 100.»; vacía sin dato. */
  fun unitLine(estimate: Int?): String = if (estimate == null) "" else "Como quedar a $estimate en una regla de 0 a 100."

  /** «12 aterrizajes justos de 15» (en singular: «1 aterrizaje justo de 8»). */
  fun title(hits: Int, total: Int): String = (if (hits == 1) "1 aterrizaje justo" else "$hits aterrizajes justos") + " de $total"

  /** «2 dianas lunares · racha mayor ×4 · 1 cúpula» (en singular cuando corresponde; la racha solo se nombra desde 2). null si no vino ningún dato. */
  fun summaryLine(bulls: Int?, bestStreak: Int?, domes: Int?): String? {
    if (bulls == null && bestStreak == null && domes == null) return null
    val b = bulls ?: 0
    val d = domes ?: 0
    return (if (b == 1) "1 diana lunar" else "$b dianas lunares") + " · racha mayor " + (if ((bestStreak ?: 0) >= 2) "×$bestStreak" else "—") + " · " + (if (d == 1) "1 cúpula" else "$d cúpulas")
  }

  /** Tu avance contigo: los tres números («N %») y la frase. */
  data class Progress(val today: Int?, val average: Int?, val best: Int?, val phrase: String)

  /**
   * Las partidas ANTERIORES de este juego con las que se compara hoy: las medidas guardadas (`numline`, el error medio en %) sin la de esta partida (la misma marca de tiempo), las que se pueden comparar con ella (el mismo reloj y un nivel parecido: `StarMeasures.comparable`) y las últimas [PREVIOUS_MAX].
   * Funciona igual si la partida de hoy todavía no se guardó (se suma a mano) o ya sí.
   */
  fun previousErrors(points: List<MeasurePoint>, today: MeasurePoint): List<Float> {
    val series = points.filter { it.key == today.key && it.timestamp != today.timestamp } + today
    return StarMeasures.comparable(series).filter { it.timestamp != today.timestamp }.takeLast(PREVIOUS_MAX).map { it.value }
  }

  /**
   * «Hoy / Tu promedio / Tu mejor» y su frase (§6.3). El promedio y el mejor salen de las partidas ANTERIORES; «Tu mejor» incluye hoy. Sin partidas anteriores: «Juega otra vez para ver tu avance.»; mejor que tu mejor anterior: «¡Tu partida más precisa hasta ahora!»;
   * más cerca que el promedio (por más de 0,5): «Hoy quedaste más cerca que tu promedio.»; más lejos (por más de 0,5): «Un poco más lejos que tu promedio: es normal que varíe.»; si no: «Igual que tu promedio: vas parejo.»
   */
  fun progress(todayPct: Float?, previous: List<Float>): Progress {
    val today = estimate(todayPct)
    if (todayPct == null || previous.isEmpty()) return Progress(today, null, null, "Juega otra vez para ver tu avance.")
    val mean = previous.average().toFloat()
    val prevBest = previous.min()
    val phrase = when {
      todayPct < prevBest -> "¡Tu partida más precisa hasta ahora!"
      todayPct < mean - 0.5f -> "Hoy quedaste más cerca que tu promedio."
      todayPct > mean + 0.5f -> "Un poco más lejos que tu promedio: es normal que varíe."
      else -> "Igual que tu promedio: vas parejo."
    }
    return Progress(today, estimate(mean), estimate(minOf(prevBest, todayPct)), phrase)
  }

  /** «N %»; «—» sin dato. */
  fun chipValue(value: Int?): String = if (value == null) "—" else "$value %"

  // ------------------------------------------------------------------ las cúpulas de tu base (preferencias `aterrizaje_record`, en el respaldo)

  /** Lo que se suma partida a partida: las cúpulas de toda la vida y las de tu mejor partida. */
  data class Totals(val domes: Int = 0, val best: Int = 0)

  /** Los totales después de una partida: nunca bajan ni son negativos (un dato que no vino suma 0). */
  fun addTotals(saved: Totals, domes: Int?): Totals {
    val d = (domes ?: 0).coerceAtLeast(0)
    return Totals(saved.domes.coerceAtLeast(0) + d, maxOf(saved.best.coerceAtLeast(0), d))
  }

  /** «Tu base lunar: 12 cúpulas» (en singular: «1 cúpula»); null si todavía no hay ninguna. */
  fun baseLine(totals: Totals?): String? = when {
    totals == null || totals.domes <= 0 -> null
    totals.domes == 1 -> "Tu base lunar: 1 cúpula"
    else -> "Tu base lunar: ${totals.domes} cúpulas"
  }

  /** Para lectores de pantalla. */
  fun spoken(title: String, headline: String, dataLine: String?, progressPhrase: String): String =
    listOfNotNull(title, "$BOX_TITLE, $headline", dataLine, progressPhrase).joinToString(". ")
}
