package com.example.data

/**
 * "Nubi te sugiere" (30-sep): qué área conviene entrenar hoy, para el encabezado de Juegos. Lógica pura.
 *
 * Reglas, en este orden: (a) la primera área (en el orden de [areas]) que nunca se jugó; (b) si no, la que lleva más
 * días sin jugarse, siempre que sean 2 o más; (c) si no, la de menor avance ("la que más puede crecer").
 * [areas] son los nombres visibles; [Suggestion.area] devuelve uno de ellos tal cual. El [Suggestion.reason] NO repite
 * el nombre (el encabezado ya dice "Te sugiero Razonamiento: ..."), con mayúscula inicial.
 */
object AreaSuggestion {
  data class Suggestion(val area: String, val reason: String)

  const val MIN_DAYS_AWAY = 2

  fun pick(
    areas: List<String>,
    lastPlayedByArea: Map<String, Long?>,
    valueByArea: Map<String, Float?>,
    now: Long,
    dayOf: (Long) -> Long
  ): Suggestion {
    require(areas.isNotEmpty()) { "areas vacía" }
    areas.firstOrNull { lastPlayedByArea[it] == null }?.let { return Suggestion(it, "Aún no lo juegas") }
    val today = dayOf(now)
    val away = areas.associateWith { today - dayOf(lastPlayedByArea.getValue(it)!!) }
    val longest = areas.maxByOrNull { away.getValue(it) }!! // ante empate, la primera del orden
    val days = away.getValue(longest)
    if (days >= MIN_DAYS_AWAY) return Suggestion(longest, "Hace $days días que no lo juegas")
    val lowest = areas.minByOrNull { valueByArea[it] ?: 0f }!!
    return Suggestion(lowest, "Es el área que más puede crecer")
  }
}
