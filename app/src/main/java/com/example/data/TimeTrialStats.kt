package com.example.data

/**
 * Lo que el Contrarreloj dice de una partida y de la persona, SOLO contra sí misma (Tarea 177; `docs/contrarreloj.md`): su récord, sus últimas 5 partidas y la mejor de la semana, todo del mismo juego y la misma versión de la pista. Nunca percentiles ni otras personas (`Features.COMPARE` sigue apagado).
 * Lógica pura sobre las partidas guardadas ([TimeTrialResult]); con pruebas (`TimeTrialStatsTest`).
 */
object TimeTrialStats {
  /** Cuántas partidas se dibujan en «Tus últimas partidas». */
  const val LAST_N = 5

  /** El récord del juego (la mayor marca con la pista de hoy); null = nunca jugó. */
  fun record(results: List<TimeTrialResult>, gameId: String): Int? = TimeTrialDay.ofGame(results, gameId).maxOfOrNull { it.hits }

  /** Las últimas [n] partidas del juego, de la más vieja a la más nueva. */
  fun last(results: List<TimeTrialResult>, gameId: String, n: Int = LAST_N): List<TimeTrialResult> = TimeTrialDay.ofGame(results, gameId).takeLast(n)

  /** La mejor marca del juego desde el lunes (hora local) de la semana de [now]; null = ninguna esta semana. */
  fun bestOfWeek(results: List<TimeTrialResult>, gameId: String, now: Long): Int? {
    val from = Week.weekStart(now)
    return TimeTrialDay.ofGame(results, gameId).filter { it.timestamp >= from && it.timestamp <= now }.maxOfOrNull { it.hits }
  }

  /**
   * Todo lo que el final del Contrarreloj necesita saber de UNA partida (ya guardada en [all]): el récord que había justo antes y cuándo se hizo, si esta partida lo superó (nunca la primera de un juego: no hay nada que superar), las últimas 5 (la de hoy al final) y la mejor de la semana.
   */
  data class Outcome(
    val result: TimeTrialResult,
    /** El récord de antes de esta partida (null = es la primera partida de este juego con esta pista). */
    val previousRecord: Int?,
    /** Cuándo se hizo ese récord (la primera vez que se alcanzó). */
    val previousRecordAt: Long?,
    val isRecord: Boolean,
    val isFirst: Boolean,
    /** Las marcas de las últimas partidas con esta incluida al final (hasta [LAST_N]). */
    val last: List<Int>,
    val bestOfWeek: Int?,
    /** ¿La mejor de la semana es esta misma partida (o la iguala)? */
    val weekBestIsToday: Boolean,
    val maxLevel: Int,
    /** El récord de una pista ANTERIOR del mismo juego (versión, aciertos), si la hay: queda en el detalle («tu récord de la pista 1»). */
    val olderTrackRecord: Pair<Int, Int>? = null
  )

  fun outcome(all: List<TimeTrialResult>, result: TimeTrialResult, now: Long = result.timestamp): Outcome {
    val mine = TimeTrialDay.ofGame(all, result.gameId)
    val before = mine.filter { it.timestamp < result.timestamp }
    val prev = before.maxOfOrNull { it.hits }
    val prevAt = prev?.let { p -> before.filter { it.hits == p }.minOf { it.timestamp } }
    val lastMarks = mine.filter { it.timestamp <= result.timestamp }.takeLast(LAST_N).map { it.hits }
    val week = bestOfWeek(all, result.gameId, now)
    val older = all.filter { it.gameId == result.gameId && it.trackVersion < TimeTrialDay.trackVersion(result.gameId) }
      .groupBy { it.trackVersion }.mapValues { (_, rs) -> rs.maxOf { it.hits } }.maxByOrNull { it.key }?.let { it.key to it.value }
    return Outcome(
      result = result, previousRecord = prev, previousRecordAt = prevAt,
      isRecord = prev != null && result.hits > prev, isFirst = prev == null,
      last = lastMarks, bestOfWeek = week, weekBestIsToday = week != null && result.hits >= week && result.timestamp >= Week.weekStart(now),
      maxLevel = TimeTrialDay.maxLevel(result.gameId), olderTrackRecord = older
    )
  }

  // ------------------------------------------------------------------ los textos del final (Fable §4 y §9.8)

  const val SEAL = "¡Tu mejor marca hasta hoy!"
  const val UNIT = "en 2 minutos de Contrarreloj"
  const val LAST_TITLE = "Tus últimas partidas"
  const val AGAIN = "Otra vez"
  const val DONE = "Listo"
  const val DETAIL = "Ver el detalle de tu partida"
  const val FIRST_PHRASE = "Es tu primera marca en el Contrarreloj. La próxima te comparas contigo."
  const val NOTE = "Es el resultado de un juego, comparado solo contigo. No es una medida de capacidad."

  /** «Llegaste al paso 4 de 5 de la pista.» */
  fun stepLine(level: Int, maxLevel: Int): String = "Llegaste al paso ${level.coerceIn(1, maxLevel)} de $maxLevel de la pista."

  /** «de hoy» / «de ayer» / «de hace 3 días» (días enteros de la hora local). */
  fun daysAgoText(now: Long, at: Long): String {
    val d = (Streak.dayOf(now) - Streak.dayOf(at)).coerceAtLeast(0L)
    return when (d) { 0L -> "de hoy"; 1L -> "de ayer"; else -> "de hace $d días" }
  }

  /**
   * La frase de avance, solo contigo: la primera vez («Es tu primera marca en el Contrarreloj. La próxima te comparas contigo.»), con marca nueva («Antes, tu mejor marca era 41 aciertos.») o sin ella («Tu mejor marca sigue en 41 aciertos, de hace 3 días.»).
   */
  fun phrase(o: Outcome, now: Long = o.result.timestamp): String = when {
    o.isFirst -> FIRST_PHRASE
    o.isRecord -> "Antes, tu mejor marca era ${TimeTrialDay.hitsText(o.previousRecord ?: 0)}."
    else -> "Tu mejor marca sigue en ${TimeTrialDay.hitsText(o.previousRecord ?: 0)}, ${daysAgoText(now, o.previousRecordAt ?: now)}."
  }

  /** «La mejor de esta semana: 41 aciertos.» o, si es la de hoy, «La mejor de esta semana: 43 aciertos, la de hoy.» Null = no hay (la primera vez no se dibuja). */
  fun weekLine(o: Outcome): String? {
    if (o.isFirst) return null
    val best = o.bestOfWeek ?: return null
    return "La mejor de esta semana: ${TimeTrialDay.hitsText(best)}" + if (o.weekBestIsToday) ", la de hoy." else "."
  }

  /** Lo que dice el detalle de la partida (los números que la vista no muestra): aciertos y errores, el paso, la duración y, si la hay, el récord de una pista anterior. */
  fun detailLines(o: Outcome): List<String> = buildList {
    add("Hiciste ${TimeTrialDay.hitsText(o.result.hits)} y ${if (o.result.errors == 1) "1 error" else "${o.result.errors} errores"}.")
    add(stepLine(o.result.level, o.maxLevel))
    add("La partida duró ${o.result.seconds} segundos.")
    o.olderTrackRecord?.let { (version, hits) -> add("Tu récord de la pista $version: ${TimeTrialDay.hitsText(hits)}.") }
  }

  /** Lo que lee en voz alta un lector de pantalla: el juego, la marca con su unidad, el sello, la frase, el paso y la mejor de la semana. */
  fun spoken(title: String, o: Outcome, now: Long = o.result.timestamp): String = buildList {
    add("$title, Contrarreloj de hoy")
    if (o.isRecord) add(SEAL)
    add("${TimeTrialDay.hitsText(o.result.hits)} $UNIT")
    add(phrase(o, now))
    add(stepLine(o.result.level, o.maxLevel))
    weekLine(o)?.let { add(it) }
  }.joinToString(". ").replace("..", ".").replace("!.", "!")
}
