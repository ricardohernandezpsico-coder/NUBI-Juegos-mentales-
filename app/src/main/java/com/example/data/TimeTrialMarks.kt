package com.example.data

/**
 * Lo que dice la PÁGINA «Contrarreloj» de Avance (Tarea 199; `auditoria-fable/estadisticas-contrarreloj.md` §2.1 y §11), SOLO contigo y SIN servidor: sale entero de las partidas guardadas ([TimeTrialResult], tabla `time_trial_results`) y de [TimeTrialStats]; no hay tabla ni migración nuevas.
 * Todo del MISMO juego y la MISMA versión de la pista ([TimeTrialDay.ofGame]): las marcas de otra pista no se comparan (solo el récord de la pista anterior queda aparte, en «Pistas anteriores»). Lógica pura, con pruebas (`TimeTrialMarksTest`).
 */
object TimeTrialMarks {
  /** Cuántas marcas se dibujan (con letra 2,0, [HALF]: las cifras no caben a 16 sp sin tocarse). */
  const val LAST_N = 10
  const val HALF = 5

  const val TITLE = "Contrarreloj"
  const val SUBTITLE = "Tus marcas, comparadas solo contigo."
  const val RECORD_TITLE = "Tu récord"
  const val WEEKS_TITLE = "Esta semana y la pasada"
  const val SPLIT_TITLE = "Aciertos y errores"
  const val OLDER_TITLE = "Pistas anteriores"
  const val FIRST_MARK = "Tu primera marca. Desde la segunda, se dibujan aquí."
  const val NO_MARKS_DOOR = "Todavía sin marcas"
  const val NO_MARK_TILE = "sin marca aún"
  const val OLDER_NOTE = "Las marcas de otra pista no se comparan."

  /** Las partidas de una semana: cuántas y la mejor marca (null = ninguna). */
  data class WeekSummary(val games: Int, val best: Int?)

  /**
   * Todo lo de la página para UN juego. [marks] = las últimas [LAST_N] partidas de la pista de hoy, de la más vieja a la más nueva; [total] = cuántas hay en total con esa pista; [record] = la partida del récord (la primera vez que se alcanzó esa marca);
   * [recordIndex] = su lugar dentro de [marks] (null si ya no está entre las últimas). Los dos promedios solo existen con 10 partidas o más (cinco contra las cinco de antes). [recordHits]/[recordErrors] son SIN restar (la marca ya es la resta); [recentHits]/[recentErrors] = el promedio por partida de las últimas 5 (o de las que haya).
   */
  data class Page(
    val gameId: String,
    val marks: List<TimeTrialResult>,
    val total: Int,
    val record: TimeTrialResult?,
    val recordIndex: Int?,
    val thisWeek: WeekSummary,
    val lastWeek: WeekSummary,
    val lastFiveAverage: Int?,
    val previousFiveAverage: Int?,
    val recentHits: Int?,
    val recentErrors: Int?,
    val recentCount: Int,
    /** El récord de una pista ANTERIOR del mismo juego: (versión de la pista, marca con la regla de ESA pista). */
    val olderTrack: Pair<Int, Int>?,
    val maxLevel: Int
  ) {
    val hasMarks: Boolean get() = total > 0
    val recordMark: Int? get() = record?.mark
  }

  /** La página de [gameId] con las partidas de [all] vistas desde [now]. */
  fun page(all: List<TimeTrialResult>, gameId: String, now: Long): Page {
    val mine = TimeTrialDay.ofGame(all, gameId)
    val shown = mine.takeLast(LAST_N)
    val best = mine.maxOfOrNull { it.mark }
    val record = best?.let { b -> mine.filter { it.mark == b }.minByOrNull { it.timestamp } }
    val recordIndex = record?.let { r -> shown.indexOfFirst { it.id == r.id }.takeIf { it >= 0 } }
    val thisStart = Week.weekStart(now)
    val lastStart = Week.weekStart(now, 1)
    fun summary(from: Long, to: Long): WeekSummary {
      val inWeek = mine.filter { it.timestamp in from until to }
      return WeekSummary(inWeek.size, inWeek.maxOfOrNull { it.mark })
    }
    val last5 = mine.takeLast(HALF)
    val averages = if (mine.size >= 2 * HALF) average(last5.map { it.mark }) to average(mine.dropLast(HALF).takeLast(HALF).map { it.mark }) else null to null
    val older = all.filter { it.gameId == gameId && it.trackVersion < TimeTrialDay.trackVersion(gameId) }
      .groupBy { it.trackVersion }.mapValues { (_, rs) -> rs.maxOf { it.mark } }.maxByOrNull { it.key }?.let { it.key to it.value }
    return Page(
      gameId = gameId, marks = shown, total = mine.size, record = record, recordIndex = recordIndex,
      thisWeek = summary(thisStart, Long.MAX_VALUE), lastWeek = summary(lastStart, thisStart),
      lastFiveAverage = averages.first, previousFiveAverage = averages.second,
      recentHits = last5.takeIf { it.isNotEmpty() }?.let { average(it.map { r -> r.hits }) },
      recentErrors = last5.takeIf { it.isNotEmpty() }?.let { average(it.map { r -> r.errors }) },
      recentCount = last5.size, olderTrack = older, maxLevel = TimeTrialDay.maxLevel(gameId)
    )
  }

  /** El promedio entero (redondeado al más cercano: 40,2 → 40). */
  private fun average(values: List<Int>): Int = Math.round(values.sum().toDouble() / values.size).toInt()

  // ------------------------------------------------------------------ los textos

  /** «el 17 de octubre, en 2 minutos de Contrarreloj» (con el año si no es el de [now]). */
  fun recordWhen(record: TimeTrialResult, now: Long): String = "el ${TuStats.since(record.timestamp, now)}, ${TimeTrialStats.UNIT}"

  /** «Tus últimas 5 marcas promedian 40 aciertos; las 5 anteriores, 37.» Null si no hay 10 partidas. */
  fun averagesSentence(page: Page): String? {
    val a = page.lastFiveAverage ?: return null
    val b = page.previousFiveAverage ?: return null
    return "Tus últimas $HALF marcas promedian ${TimeTrialDay.hitsText(a)}; las $HALF anteriores, $b."
  }

  /** «Tus últimas 10 marcas» / «Tus últimas 5 marcas» (según las que se dibujan); con una sola, «Tus últimas marcas» (debajo dice que la primera ya está y cuándo se dibujan). */
  fun barsTitle(shown: Int): String = if (shown <= 1) "Tus últimas marcas" else "Tus últimas $shown marcas"

  /** «3 partidas» / «1 partida» / «ninguna partida». */
  fun gamesText(n: Int): String = when (n) { 0 -> "ninguna partida"; 1 -> "1 partida"; else -> "$n partidas" }

  /** «En tus últimas 5, por partida» (con menos de 5: «En tus últimas 3, por partida»). Null con una sola partida: sería lo mismo que el récord. */
  fun recentTitle(count: Int): String? = if (count < 2) null else "En tus últimas $count, por partida"

  /** «1 error» / «3 errores» / «0 errores». */
  fun errorsText(n: Int): String = if (n == 1) "1 error" else "$n errores"

  /**
   * Lo que se dice en el eje bajo las barras: a la izquierda la fecha de la PRIMERA barra que se ve (con letra 2,0, la primera de las cinco que quedan: nunca la de las diez) y a la derecha «hoy» si la última marca es de hoy; si no, su fecha. Con una sola fecha (todo el mismo día) queda solo el de la izquierda.
   * Sin marcas no hay eje.
   */
  fun axisLabels(shown: List<TimeTrialResult>, now: Long): Pair<String, String?>? {
    if (shown.isEmpty()) return null
    fun label(r: TimeTrialResult): String = if (Streak.dayOf(r.timestamp) == Streak.dayOf(now)) "hoy" else TuStats.since(r.timestamp, now)
    val left = label(shown.first())
    val right = label(shown.last())
    return left to right.takeIf { it != left }
  }

  /**
   * Lo que lee el lector de pantalla de las barras (Fable §3.2): «Tus últimas 10 marcas: 31, 35, 39, 36, 44 aciertos, tu récord, 38, 41, 37, 42 y 43 aciertos, la de hoy.» La unidad va tras el récord y tras la última; si la última no es de hoy, dice «la de ayer» o «la de hace 3 días».
   * Con una sola: «Tu primera marca: 31 aciertos.»
   */
  fun barsSpoken(shown: List<TimeTrialResult>, recordIndex: Int?, now: Long): String {
    if (shown.isEmpty()) return ""
    if (shown.size == 1) return "Tu primera marca: ${TimeTrialDay.hitsText(shown[0].mark)}."
    val lastIndex = shown.lastIndex
    val parts = shown.mapIndexed { i, r ->
      val n = r.mark
      val tail = StringBuilder()
      if (i == recordIndex) tail.append(", tu récord")
      if (i == lastIndex) tail.append(", la ${TimeTrialStats.daysAgoText(now, r.timestamp)}")
      if (tail.isEmpty()) "$n" else TimeTrialDay.hitsText(n) + tail
    }
    return "${barsTitle(shown.size)}: ${parts.dropLast(1).joinToString(", ")} y ${parts.last()}."
  }

  /** Una fila «rótulo … cifra» de «Esta semana y la pasada», dicha de corrido: «Jugaste, 3 partidas». */
  fun rowSpoken(label: String, value: String): String = "$label, $value"

  /** Las filas de una semana: «Jugaste» y, si hubo partidas, «La mejor marca». */
  fun weekRows(w: WeekSummary): List<Pair<String, String>> = buildList {
    add("Jugaste" to gamesText(w.games))
    w.best?.let { add("La mejor marca" to TimeTrialDay.hitsText(it)) }
  }

  /** «Tu récord de la pista 1: 38 aciertos. Las marcas de otra pista no se comparan.» */
  fun olderSentence(older: Pair<Int, Int>): String = "Tu récord de la pista ${older.first}: ${TimeTrialDay.hitsText(older.second)}. $OLDER_NOTE"

  /** Lo que dice cada juego en el selector: su récord («44 aciertos») o «sin marca aún». */
  fun tileValue(record: Int?): String = record?.let { TimeTrialDay.hitsText(it) } ?: NO_MARK_TILE

  /** Para el lector de pantalla: «Tinta o Palabra, récord 44 aciertos, elegido». */
  fun tileSpoken(title: String, record: Int?, selected: Boolean): String =
    title + ", " + (record?.let { "récord ${TimeTrialDay.hitsText(it)}" } ?: NO_MARK_TILE) + if (selected) ", elegido" else ""

  /** La segunda línea de la puerta de Avance: «Tu récord de hoy: 44 aciertos» (el juego del día) o «Todavía sin marcas». */
  fun doorLine(record: Int?): String = record?.let { "Tu récord de hoy: ${TimeTrialDay.hitsText(it)}" } ?: NO_MARKS_DOOR

  /** Lo que lee el lector de pantalla de la puerta: el título y la línea de abajo (y el candado, sin Plus). */
  fun doorSpoken(record: Int?, plus: Boolean): String = "Tus marcas del Contrarreloj. ${doorLine(record)}" + if (plus) "." else ". Con Nubi Plus."
}
