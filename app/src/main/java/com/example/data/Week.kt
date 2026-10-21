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

  /**
   * Los farolitos de la semana de [now] a partir de las marcas de tiempo de las partidas (Tarea 124: antes `weekLamps` de `HomeScreen`): lunes a domingo, los días con partida encendidos. Los días son locales ([Streak.dayOf]).
   */
  fun lampsFor(timestamps: List<Long>, now: Long): Lamps {
    val cal = java.util.Calendar.getInstance().apply { timeInMillis = now }
    val mondayOffset = (cal.get(java.util.Calendar.DAY_OF_WEEK) + 5) % 7   // lunes = 0
    return lamps(timestamps, now, Streak::dayOf, mondayOffset)
  }

  /**
   * El lunes a las 00:00 (hora local) de la semana de [now], [weeksBack] semanas atrás. «Esta semana» EMPIEZA ahí en toda la app (Tarea 151, B4.1: antes las farolas y «Áreas esta semana» contaban desde el lunes y «Subió 10 puntos esta semana» los últimos 7 días).
   */
  fun weekStart(now: Long, weeksBack: Int = 0): Long {
    val cal = java.util.Calendar.getInstance().apply { timeInMillis = now }
    val mondayOffset = (cal.get(java.util.Calendar.DAY_OF_WEEK) + 5) % 7                // lunes = 0
    cal.add(java.util.Calendar.DAY_OF_MONTH, -(mondayOffset + 7 * weeksBack))
    cal.set(java.util.Calendar.HOUR_OF_DAY, 0); cal.set(java.util.Calendar.MINUTE, 0); cal.set(java.util.Calendar.SECOND, 0); cal.set(java.util.Calendar.MILLISECOND, 0)
    return cal.timeInMillis
  }

  /**
   * Cuántas áreas distintas se jugaron esta semana (desde el lunes): «Áreas esta semana: 2 de 4» (Tarea 125, reemplaza al desafío «Variedad»). [entries] = (marca de tiempo, nombre del área) de cada partida; los juegos retirados
   * (sin área) no cuentan. [mondayOffset] = días desde el lunes hasta hoy (lunes = 0).
   */
  fun areasPlayed(entries: List<Pair<Long, String?>>, now: Long, dayOf: (Long) -> Long, mondayOffset: Int): Int {
    val monday = dayOf(now) - mondayOffset.coerceIn(0, 6)
    return entries.filter { dayOf(it.first) in monday..dayOf(now) }.mapNotNull { it.second }.toSet().size
  }

  /** [areasPlayed] con el lunes y los días locales de hoy. */
  fun areasPlayedFor(entries: List<Pair<Long, String?>>, now: Long): Int {
    val cal = java.util.Calendar.getInstance().apply { timeInMillis = now }
    return areasPlayed(entries, now, Streak::dayOf, (cal.get(java.util.Calendar.DAY_OF_WEEK) + 5) % 7)
  }

  /**
   * La frase de la semana partida en lo GRANDE y lo demás (Avance, Tarea 125: «**3 de 4 días** de tu meta»): [caption] es la suma de las dos. «3 de 4 días» + « de tu meta»; pasada la meta, «6 días» + « esta semana: meta cumplida» (no «6 de 4»);
   * sin meta, «3 días» + « esta semana».
   */
  fun captionParts(daysPlayed: Int, goal: Int): Pair<String, String> = when {
    goal > 0 && daysPlayed > goal -> "$daysPlayed días" to " esta semana: meta cumplida"
    goal > 0 -> "$daysPlayed de $goal ${if (goal == 1) "día" else "días"}" to " de tu meta"
    daysPlayed == 1 -> "1 día" to " esta semana"
    else -> "$daysPlayed días" to " esta semana"
  }

  /** «3 de 4 días de tu meta»; pasada la meta, «6 días esta semana: meta cumplida» (no «6 de 4»); sin meta, «3 días esta semana». */
  fun caption(daysPlayed: Int, goal: Int): String = captionParts(daysPlayed, goal).let { it.first + it.second }

  /** Lo que lee el lector de pantalla para el grupo de siete farolitos (una sola descripción, no siete; la meta ya la lee la línea de la fecha). */
  fun lampsSpoken(daysPlayed: Int): String = when (daysPlayed) {
    0 -> "Tu semana: ningún día con partida todavía"
    1 -> "Tu semana: 1 día con partida"
    else -> "Tu semana: $daysPlayed días con partida"
  }
}
