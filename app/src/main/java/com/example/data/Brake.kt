package com.example.data

/**
 * «Tu freno» (Freno de Emergencia), Tarea 57 (9-oct): la medida honesta. Una partida trae 6 a 20 altos y el consenso (Verbruggen et al., 2019) pide 50 o más para estimar el tiempo de frenado (SSRT) de una persona: el de UNA partida
 * es ruido. Por eso la pantalla final NO muestra milisegundos sueltos sino el PROMEDIO de las últimas [WINDOW] estimaciones válidas (con la de hoy), ubicado en un velocímetro de tres zonas con nombre. Cada partida sigue estimando
 * el SSRT igual y lo guarda en `star_measures` (clave [KEY], en ms, interno); de ahí sale el promedio. Lógica pura con pruebas.
 *
 * Los cortes son una decisión de diseño, NO normas: ágil < 230 ms, firme 230-300 ms, pausado > 300 ms (los rangos típicos de adultos jóvenes rondan los 200-250 ms; en mayores es más lento). Sirven para ver el cambio propio entre
 * partidas, no para compararse con nadie.
 */
enum class BrakeZone(val label: String) { AGIL("ágil"), FIRME("firme"), PAUSADO("pausado") }

/** El promedio de las últimas estimaciones válidas ([count], de 1 a [Brake.WINDOW]) y lo que de él se muestra. */
data class BrakeReading(val averageMs: Float, val count: Int) {
  val zone: BrakeZone get() = Brake.zone(averageMs)

  /** Con 1 o 2 estimaciones todavía es una primera lectura; con 3 o más ya es un promedio. */
  val isFirstReading: Boolean get() = count < Brake.MIN_FOR_AVERAGE

  /** «Tu freno, promedio de tus últimas 4 partidas: zona firme» o «Primera lectura: zona firme. Se afina con más partidas». */
  val headline: String
    get() = if (isFirstReading) "Primera lectura: zona ${zone.label}. Se afina con más partidas"
    else "Tu freno, promedio de tus últimas $count partidas: zona ${zone.label}"

  /** Para lectores de pantalla: la zona dicha con palabras y de qué viene (sin milisegundos). */
  val spoken: String
    get() = if (isFirstReading) "Tu freno: primera lectura, zona ${zone.label}"
    else "Tu freno: zona ${zone.label}, promedio de tus últimas $count partidas"
}

object Brake {
  /** La clave de la medida en `star_measures` (ver [StarMeasures.defs]). */
  const val KEY = "brake"

  /** Cuántas estimaciones válidas entran en el promedio. */
  const val WINDOW = 5

  /** Desde cuántas estimaciones se habla de «promedio de tus últimas N partidas». */
  const val MIN_FOR_AVERAGE = 3

  /** Cortes de las zonas, en ms: «ágil» por debajo de [AGIL_BELOW_MS], «pausado» por encima de [PAUSADO_ABOVE_MS], «firme» entre los dos (los dos incluidos). */
  const val AGIL_BELOW_MS = 230f
  const val PAUSADO_ABOVE_MS = 300f

  /** El rango en que Unity da una estimación (`BrakeContract.Ssrt` la recorta a 50-900 ms); lo demás no es una estimación válida. */
  private const val MIN_VALID_MS = 50f
  private const val MAX_VALID_MS = 900f

  fun isValid(ms: Float?): Boolean = ms != null && !ms.isNaN() && ms in MIN_VALID_MS..MAX_VALID_MS

  fun zone(ms: Float): BrakeZone = when {
    ms < AGIL_BELOW_MS -> BrakeZone.AGIL
    ms <= PAUSADO_ABOVE_MS -> BrakeZone.FIRME
    else -> BrakeZone.PAUSADO
  }

  /** El promedio de las últimas [WINDOW] estimaciones VÁLIDAS de [valuesInOrder] (de la más vieja a la más nueva); null si no hay ninguna. */
  fun average(valuesInOrder: List<Float>): BrakeReading? {
    val last = valuesInOrder.filter { isValid(it) }.takeLast(WINDOW)
    if (last.isEmpty()) return null
    return BrakeReading(last.average().toFloat(), last.size)
  }

  /**
   * Lo que muestra la pantalla final: el historial de la medida ([history], puntos de `star_measures`; solo cuentan los de clave [KEY]) más la estimación de ESTA partida ([thisMs], null si no se pudo estimar). El punto de esta
   * misma partida ([thisTimestamp]) se descarta del historial por si ya se guardó, para que no cuente dos veces.
   */
  fun reading(history: List<MeasurePoint>, thisMs: Int?, thisTimestamp: Long): BrakeReading? {
    val before = history.filter { it.key == KEY && it.timestamp != thisTimestamp }.sortedBy { it.timestamp }.map { it.value }
    val today = thisMs?.toFloat()?.takeIf { isValid(it) }
    return average(if (today != null) before + today else before)
  }

  /** La zona del PROMEDIO de las últimas partidas de un historial, para Juegos y Hoy («zona firme»); null si todavía no hay ninguna estimación válida. */
  fun reading(history: List<MeasurePoint>): BrakeReading? = average(history.filter { it.key == KEY }.sortedBy { it.timestamp }.map { it.value })

  /** Dónde va la aguja: 0 = el extremo lento (izquierda), 1 = el extremo rápido (derecha). Cada zona ocupa un tercio del arco (pausado 450→300 ms, firme 300→230, ágil 230→150), así los nombres se leen parejos. */
  fun gaugePosition(ms: Float): Float {
    val v = ms.coerceIn(150f, 450f)
    return when {
      v > PAUSADO_ABOVE_MS -> (450f - v) / (450f - PAUSADO_ABOVE_MS) / 3f
      v >= AGIL_BELOW_MS -> 1f / 3f + (PAUSADO_ABOVE_MS - v) / (PAUSADO_ABOVE_MS - AGIL_BELOW_MS) / 3f
      else -> 2f / 3f + (AGIL_BELOW_MS - v) / (AGIL_BELOW_MS - 150f) / 3f
    }
  }
}
