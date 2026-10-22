package com.example.data

import kotlin.math.roundToInt

/** El avance de un juego en un momento (0..1, el de [Skill.progress]); se anota cada vez que una partida lo mueve. */
data class ProgressPoint(val gameId: String, val time: Long, val progress: Float)

/**
 * El REGISTRO del avance de cada juego (SharedPreferences `progress_log`, solo se agrega). Con él se calculan los puntos de cada área, lo que cambió esta semana, la mejor marca y las semanas del gráfico ([AreaPoints], Tarea 159); antes también
 * armaba «avanzó de 37 a 47» con promedios (`AreaStatus`, retirado: el avance ahora se dice en puntos de 0 a 1000). Lógica pura con pruebas.
 */
object AreaProgress {
  // --- Registro (SharedPreferences `progress_log`, solo se agrega): "juego|ms|avance×1000" por línea. ---
  fun encode(points: List<ProgressPoint>): String =
    points.joinToString("\n") { "${it.gameId}|${it.time}|${(it.progress * 1000f).roundToInt()}" }

  fun decode(text: String?): List<ProgressPoint> = text.orEmpty().lines().mapNotNull { line ->
    val p = line.split('|')
    if (p.size != 3) return@mapNotNull null
    val t = p[1].toLongOrNull(); val v = p[2].toIntOrNull()
    if (t == null || v == null) null else ProgressPoint(p[0], t, v / 1000f)
  }

  /** Cuántos días se guardan: un año (Tarea 159: antes 60), para que «tu mejor marca» de un área no se pierda a los dos meses y el gráfico de 8 semanas tenga de dónde leer. Pesa unas 25 líneas por mes de juego. */
  const val KEEP_DAYS = 365

  /**
   * Agrega un punto si el avance cambió respecto del último de ese juego. Guarda como mucho [keepDays] días (más
   * el último punto anterior de cada juego, para saber dónde estaba).
   */
  fun append(points: List<ProgressPoint>, p: ProgressPoint, keepDays: Int = KEEP_DAYS): List<ProgressPoint> {
    val last = points.filter { it.gameId == p.gameId }.maxByOrNull { it.time }
    val next = if (last != null && kotlin.math.abs(last.progress - p.progress) < 1e-4f) points else points + p
    val cut = p.time - keepDays * 86_400_000L
    val keepOld = next.filter { it.time < cut }.groupBy { it.gameId }.values.map { l -> l.maxBy { it.time } }
    return keepOld + next.filter { it.time >= cut }
  }
}
