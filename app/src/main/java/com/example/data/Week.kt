package com.example.data

/**
 * La semana de constancia de Hoy (`docs/diseno-isla.md` §4.1): siete farolitos de lunes a domingo; los días con al menos una partida se encienden. Los hitos y la meta cuentan DÍAS JUGADOS (no puntajes ni rachas seguidas).
 * Lógica pura con pruebas (`WeekTest`).
 */
object Week {
  /** [played] = lunes a domingo (7); [todayIndex] = 0..6 el día de hoy dentro de la semana; [daysPlayed] = cuántos días se jugó. */
  data class Lamps(val played: List<Boolean>, val todayIndex: Int) {
    val daysPlayed: Int get() = played.count { it }
  }

  /**
   * [timestamps] = las partidas guardadas; [dayOf] = el número de día de una marca de tiempo (en la zona horaria de la persona);
   * [mondayOffset] = cuántos días pasaron desde el lunes hasta hoy (lunes = 0, domingo = 6).
   */
  fun lamps(timestamps: List<Long>, now: Long, dayOf: (Long) -> Long, mondayOffset: Int): Lamps {
    val offset = mondayOffset.coerceIn(0, 6)
    val monday = dayOf(now) - offset
    val days = timestamps.map(dayOf).toSet()
    return Lamps(List(7) { (monday + it) in days }, offset)
  }

  /** «3 de 4 días de tu meta»; pasada la meta, «6 días esta semana: meta cumplida» (no «6 de 4»); sin meta, «3 días esta semana». */
  fun caption(daysPlayed: Int, goal: Int): String = when {
    goal > 0 && daysPlayed > goal -> "$daysPlayed días esta semana: meta cumplida"
    goal > 0 -> "$daysPlayed de $goal ${if (goal == 1) "día" else "días"} de tu meta"
    daysPlayed == 1 -> "1 día esta semana"
    else -> "$daysPlayed días esta semana"
  }

  /** Lo que lee el lector de pantalla para el grupo de siete farolitos (una sola descripción, no siete). */
  fun spoken(daysPlayed: Int, goal: Int): String = "Tu semana: " + caption(daysPlayed, goal)
}
