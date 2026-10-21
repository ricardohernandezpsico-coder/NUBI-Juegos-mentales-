package com.example.data

import kotlin.math.roundToInt

/** El avance de un juego en un momento (0..1, el de [Skill.progress]); se anota cada vez que una partida lo mueve. */
data class ProgressPoint(val gameId: String, val time: Long, val progress: Float)

/**
 * Cómo va un área en Hoy (29-sep, aprobado por Ricardo, `docs/previews/nubi-hoy.png`). [value] = avance del área hoy
 * (0..1, promedio de sus juegos medidos; null = ninguno medido); [change] = cuánto se movió ESTA SEMANA, desde el lunes (Tarea 151, B4.1; puede ser negativo);
 * [weeks] = el avance al final de cada una de las últimas 4 semanas de calendario (el domingo a medianoche; la última = hoy).
 */
data class AreaStatus(
  val area: String,
  val value: Float?,
  val change: Float,
  val weeks: List<Float?>
) {
  val points: Int? get() = value?.let { (it.coerceIn(0f, 1f) * 100f).roundToInt() }
  /** El avance con que empezó la semana (el lunes a las 00:00) en la misma escala 0-100 (para "avanzó de 46 a 52"). */
  val pointsBefore: Int? get() = value?.let { ((it - change).coerceIn(0f, 1f) * 100f).roundToInt() }
}

/**
 * Avance por área y su cambio de la semana (lógica pura con pruebas). Se lee en una sola escala: el avance de 0 a 100
 * de `data/Skill.kt` (cada etapa = 20). El cambio compara, juego por juego, el avance de hoy con el del lunes a las 00:00 ([Week.weekStart]: «esta semana» es la misma en toda la app),
 * y solo en los juegos que ya estaban medidos entonces: explorar un juego nuevo NO se cuenta como subida ni bajada.
 */
object AreaProgress {
  const val WEEK_MS = 7L * 86_400_000L
  /** Un cambio menor a este (en avance 0..1) cuenta como "igual": medio punto de 100. */
  const val SAME = 0.005f

  /** Avance de un juego en [t]: el último punto anotado hasta ese momento (null = aún sin medir entonces). */
  fun at(points: List<ProgressPoint>, gameId: String, t: Long): Float? =
    points.filter { it.gameId == gameId && it.time <= t }.maxByOrNull { it.time }?.progress

  /**
   * [games] = los juegos del área; [current] = el avance de hoy de cada juego medido (lo que muestran las cartas).
   * [since] = desde cuándo se mide el cambio (por omisión, el lunes de esta semana; el resumen de la sesión usa su comienzo).
   * Si un juego medido no tiene historia anotada (se midió antes de que existiera el registro), se toma su avance de
   * hoy como el de siempre: no inventa cambios.
   */
  fun status(
    area: String, games: List<String>, current: Map<String, Float>, points: List<ProgressPoint>, now: Long,
    since: Long = Week.weekStart(now)
  ): AreaStatus {
    val measured = games.filter { it in current }
    val value = if (measured.isEmpty()) null else measured.map { current.getValue(it) }.average().toFloat()
    val then = since
    val deltas = measured.mapNotNull { g ->
      val hist = points.any { it.gameId == g }
      val before = if (hist) at(points, g, then) else current.getValue(g)
      before?.let { current.getValue(g) - it }
    }
    val change = if (deltas.isEmpty()) 0f else deltas.average().toFloat()
    val weeks = (3 downTo 0).map { k ->
      if (k == 0) value else {
        val t = Week.weekStart(now, k - 1)                      // el lunes a las 00:00 que cierra esa semana: «pasada» = el de esta semana, «hace 2» = el de la semana pasada…
        val vals = measured.mapNotNull { g -> if (points.any { it.gameId == g }) at(points, g, t) else current.getValue(g) }
        if (vals.isEmpty()) null else vals.average().toFloat()
      }
    }
    return AreaStatus(area, value, if (kotlin.math.abs(change) < SAME) 0f else change, weeks)
  }

  /** "tu memoria", "tu atención"... para las frases de Nubi. */
  fun your(areaName: String): String = "tu ${areaName.lowercase()}"

  /**
   * Lo que dice Nubi arriba en Hoy: en palabras, sin números sueltos (Ricardo: "¿4 qué?"). [names] = nombre visible
   * de cada área, en el orden de [statuses].
   */
  fun nubiLine(statuses: List<AreaStatus>, names: Map<String, String>): String {
    if (statuses.none { it.value != null }) return "Juega y aquí verás tu avance"
    val up = statuses.filter { it.change > 0f }.sortedByDescending { it.change }.map { names[it.area] ?: it.area }
    return when (up.size) {
      0 -> "Tu avance se mantiene esta semana"
      1 -> "${up[0]} avanzó esta semana"
      2 -> "${up[0]} y ${up[1]} avanzaron"
      else -> "${up.size} áreas avanzaron esta semana"
    }
  }

  /**
   * La línea del cambio: "Esta semana avanzó de 46 a 52" / "bajó un poco, de 40 a 38" / "se mantuvo en 52". [period]
   * = "Esta semana" en el detalle del área, "Hoy" en el resumen de la sesión.
   */
  fun changeLine(s: AreaStatus, period: String = "Esta semana"): String {
    val now = s.points ?: return "Aún sin medir: tu primera partida lo muestra"
    val before = s.pointsBefore ?: now
    return when {
      s.change > 0f && before != now -> "$period avanzó de $before a $now"
      s.change < 0f && before != now -> "$period bajó un poco, de $before a $now: es normal que varíe"
      else -> "$period se mantuvo en $now"
    }
  }

  /**
   * La línea del cambio de la tarjeta de un área en Avance: LA MISMA frase que el detalle y la lámina ([changeLine]: «Esta semana avanzó de 37 a 47», Tarea 150, B1.8; antes «Subió 3 puntos esta semana» / «Igual que la semana pasada»). null si todavía no hay una semana con qué comparar (menos de dos
   * valores en [AreaStatus.weeks]) o el área está sin medir.
   */
  fun cardLine(s: AreaStatus): String? {
    if (s.points == null || s.weeks.count { it != null } < 2) return null
    return changeLine(s)
  }

  /** "Te faltan 8 para Experto" (null en Maestro o sin medir): la misma frase en el área, la lámina y la ficha de un juego ([Skill.toNextLine]). */
  fun toNextLine(s: AreaStatus): String? = s.value?.let { Skill.toNextLine(it) }

  // --- Registro (SharedPreferences `progress_log`, solo se agrega): "juego|ms|avance×1000" por línea. ---
  fun encode(points: List<ProgressPoint>): String =
    points.joinToString("\n") { "${it.gameId}|${it.time}|${(it.progress * 1000f).roundToInt()}" }

  fun decode(text: String?): List<ProgressPoint> = text.orEmpty().lines().mapNotNull { line ->
    val p = line.split('|')
    if (p.size != 3) return@mapNotNull null
    val t = p[1].toLongOrNull(); val v = p[2].toIntOrNull()
    if (t == null || v == null) null else ProgressPoint(p[0], t, v / 1000f)
  }

  /**
   * Agrega un punto si el avance cambió respecto del último de ese juego. Guarda como mucho [keepDays] días (más
   * el último punto anterior de cada juego, para saber dónde estaba).
   */
  fun append(points: List<ProgressPoint>, p: ProgressPoint, keepDays: Int = 60): List<ProgressPoint> {
    val last = points.filter { it.gameId == p.gameId }.maxByOrNull { it.time }
    val next = if (last != null && kotlin.math.abs(last.progress - p.progress) < 1e-4f) points else points + p
    val cut = p.time - keepDays * 86_400_000L
    val keepOld = next.filter { it.time < cut }.groupBy { it.gameId }.values.map { l -> l.maxBy { it.time } }
    return keepOld + next.filter { it.time >= cut }
  }
}
