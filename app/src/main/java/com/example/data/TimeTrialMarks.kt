package com.example.data

/**
 * Lo que dice la PÁGINA «Contrarreloj» (hoy la pestaña de al lado de Hoy, Tarea 215; antes una página dentro de Avance, Tarea 199; `auditoria-fable/estadisticas-contrarreloj.md` §2.1 y §11), SOLO contigo y SIN servidor: sale entero de las partidas guardadas ([TimeTrialResult], tabla `time_trial_results`) y de [TimeTrialStats]; no hay tabla ni migración nuevas.
 * Todo del MISMO juego y la MISMA versión de la pista ([TimeTrialDay.ofGame]): las marcas de otra pista no se comparan (solo el récord de la pista anterior queda aparte, en «Pistas anteriores»). Lógica pura, con pruebas (`TimeTrialMarksTest`).
 */
object TimeTrialMarks {
  /** Cuántas marcas se dibujan (con letra 2,0, [HALF]: las cifras no caben a 16 sp sin tocarse). */
  const val LAST_N = 10
  const val HALF = 5

  const val TITLE = "Contrarreloj"

  /** El subtítulo de la pestaña SIN comparación (sin franja de edad, antes de la tercera partida o sin tabla del juego): lo que se ve es solo tuyo. */
  const val SUBTITLE = "Tus marcas, comparadas solo contigo."

  /**
   * El subtítulo CON comparación (Tarea 215, frase de Ricardo): lo gratis de la pestaña ya dice la división de la semana, así que la cabecera lo cuenta en una frase corta, sin decir con qué se compara (eso lo dice «Con qué te comparas ›»).
   * Reemplaza a «Tus marcas, comparadas contigo y con una referencia estimada para tu edad.» (Tarea 205) y a su versión con personas.
   */
  const val SUBTITLE_PLACE = "El Contrarreloj define tu división de la semana."

  fun subtitle(compared: Boolean): String = if (compared) SUBTITLE_PLACE else SUBTITLE
  const val FIRST_MARK = "Tu primera marca. Desde la segunda, se dibujan aquí."
  const val NO_MARK_TILE = "sin marca aún"

  /** El botón de lo gratis (Tarea 215): jugar el juego elegido, 2 minutos. */
  const val PLAY = "Jugar · ${TimeTrialDay.MINUTES_TEXT}"

  /** La pastilla del selector que marca el juego del día. */
  const val TODAY_TAG = "Hoy"

  /** Sin marcas en el juego, lo gratis dice UNA línea y nada más (Tarea 215b, Fable A4 c): ni «récord» ni la semana, que serían tres negaciones seguidas. */
  const val NO_MARKS_FREE = "Todavía no tienes marcas en este juego."

  /** «Tu récord: 44 aciertos.» de lo gratis; sin marca en el juego, [NO_MARKS_FREE]. */
  fun freeRecordLine(record: Int?): String = record?.let { TimeTrialDay.recordLine(it) } ?: NO_MARKS_FREE

  /** Lo que lee el lector de pantalla del botón «Jugar · 2 minutos» (Tarea 215b, Fable A3): dice también el juego, que en la pantalla solo lo dice la losa lima del selector. «Jugar 2 minutos de Acoplamiento». */
  fun playSpoken(gameTitle: String): String = "Jugar ${TimeTrialDay.MINUTES_TEXT} de $gameTitle"

  /**
   * «Esta semana jugaste 3 partidas y la mejor fue de 41 aciertos.» (lo gratis, Tarea 215; reemplaza a la cifra «partidas esta semana» de la Tarea 203; Tarea 215b, Fable M5: con «y … fue», que se lee de corrido, en vez de «; la mejor, …»): las partidas de esta semana de la pista de hoy y su mejor marca.
   * Si esa mejor marca ES el récord ([record]), el número no se repite (ya lo dicen la losa del selector y «Tu récord: 64 aciertos.»; Fable A4 b): «Esta semana jugaste 3 partidas y la mejor fue tu récord.». Con una sola, «Esta semana jugaste 1 partida y la mejor fue de 41 aciertos.» o «…y la mejor fue tu récord.»;
   * sin ninguna, «Esta semana todavía no jugaste.».
   */
  fun weekLine(week: WeekSummary, record: Int? = null): String {
    val best = week.best ?: return "Esta semana todavía no jugaste."
    val played = if (week.games == 1) "1 partida" else "${week.games} partidas"
    // con UNA sola partida «la mejor» sobra: «…1 partida y fue tu récord.» (Tarea 217, Fable M3)
    val which = if (week.games == 1) "fue" else "la mejor fue"
    return if (best == record) "Esta semana jugaste $played y $which tu récord." else "Esta semana jugaste $played y $which de ${TimeTrialDay.hitsText(best)}."
  }
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
    val lastFiveAverage: Int?,
    val previousFiveAverage: Int?,
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
    val inWeek = mine.filter { it.timestamp >= Week.weekStart(now) }
    val last5 = mine.takeLast(HALF)
    val averages = if (mine.size >= 2 * HALF) average(last5.map { it.mark }) to average(mine.dropLast(HALF).takeLast(HALF).map { it.mark }) else null to null
    val older = all.filter { it.gameId == gameId && it.trackVersion < TimeTrialDay.trackVersion(gameId) }
      .groupBy { it.trackVersion }.mapValues { (_, rs) -> rs.maxOf { it.mark } }.maxByOrNull { it.key }?.let { it.key to it.value }
    return Page(
      gameId = gameId, marks = shown, total = mine.size, record = record, recordIndex = recordIndex,
      thisWeek = WeekSummary(inWeek.size, inWeek.maxOfOrNull { it.mark }),
      lastFiveAverage = averages.first, previousFiveAverage = averages.second,
      olderTrack = older, maxLevel = TimeTrialDay.maxLevel(gameId)
    )
  }

  /** El promedio entero (redondeado al más cercano: 40,2 → 40). */
  private fun average(values: List<Int>): Int = Math.round(values.sum().toDouble() / values.size).toInt()

  // ------------------------------------------------------------------ los textos

  /** «Tus últimas 5 marcas promedian 40 aciertos; las 5 anteriores, 37.» Null si no hay 10 partidas. */
  fun averagesSentence(page: Page): String? {
    val a = page.lastFiveAverage ?: return null
    val b = page.previousFiveAverage ?: return null
    return "Tus últimas $HALF marcas promedian ${TimeTrialDay.hitsText(a)}; las $HALF anteriores, $b."
  }

  /** «Tus últimas 10 marcas» / «Tus últimas 5 marcas» (según las que se dibujan); con una sola, «Tus últimas marcas» (debajo dice que la primera ya está y cuándo se dibujan). */
  fun barsTitle(shown: Int): String = if (shown <= 1) "Tus últimas marcas" else "Tus últimas $shown marcas"

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

  /** Una fila «rótulo … cifra» dicha de corrido: «Desde 39 aciertos, En órbita». */
  fun rowSpoken(label: String, value: String): String = "$label, $value"

  /** «Tu récord es del 17 de octubre; llegaste al paso 4 de 5 de la pista.» (bajo las 10 marcas: la fecha y el paso del récord). */
  fun recordLine(record: TimeTrialResult, now: Long, maxLevel: Int): String =
    "Tu récord es del ${TuStats.since(record.timestamp, now)}; llegaste al paso ${record.level.coerceIn(1, maxLevel)} de $maxLevel de la pista."

  /** «Tu récord de la pista 1: 38 aciertos. Las marcas de otra pista no se comparan.» */
  fun olderSentence(older: Pair<Int, Int>): String = "Tu récord de la pista ${older.first}: ${TimeTrialDay.hitsText(older.second)}. $OLDER_NOTE"

  /** Lo que dice cada juego en el selector: su récord («44 aciertos») o «sin marca aún». */
  fun tileValue(record: Int?): String = record?.let { TimeTrialDay.hitsText(it) } ?: NO_MARK_TILE

  /**
   * Para el lector de pantalla: «Tinta o Palabra, récord 44 aciertos, el de hoy, elegido» (Tarea 215: [today] = el juego del día) o, en gris, «Rescate relámpago, récord 12 aciertos, con Nubilus Plus» ([locked]: sin Plus, tocarlo abre la hoja del candado).
   */
  fun tileSpoken(title: String, record: Int?, selected: Boolean, today: Boolean = false, locked: Boolean = false): String =
    title + ", " + (record?.let { "récord ${TimeTrialDay.hitsText(it)}" } ?: NO_MARK_TILE) + (if (today) ", el de hoy" else "") + (if (selected) ", elegido" else "") + (if (locked) ", con Nubilus Plus" else "")

  // ------------------------------------------------------------------ el ritmo de la última partida (Tarea 200; `auditoria-fable/nubi-plus-comparacion.md` §2.3)

  /** Una partida tiene cuatro medios minutos (0 a 30, 30 a 60, 60 a 90 y 90 a 120 s); cada uno dura [HALF_MINUTE_MS]. */
  const val HALF_MINUTES = 4
  const val HALF_MINUTE_MS = 30_000

  /** Los aciertos y los errores de UN medio minuto. */
  data class HalfMinute(val hits: Int, val errors: Int)

  /**
   * El ritmo de UNA partida, contado por medios minutos. [halves] siempre tiene [HALF_MINUTES] elementos (de 0 a 30 s, …, de 90 a 120 s). [bestHalves] = los medios minutos (0 a 3) con MÁS aciertos (más de uno si empatan; vacío si no hubo aciertos); [errorHalves] = los que tuvieron algún error;
   * [bestLine] y [errorsLine] son las dos frases que lee el gráfico y [sentence] las dos juntas (desde la Tarea 203b, sin el paso de la pista). Es la misma partida que dice [result]: sus aciertos y errores son los del final.
   */
  data class Pace(
    val result: TimeTrialResult,
    val halves: List<HalfMinute>,
    val bestHalves: List<Int>,
    val errorHalves: List<Int>,
    val bestLine: String,
    val errorsLine: String
  ) {
    /** «En tu última partida, tu mejor medio minuto fue el de 30 a 60 segundos, con 14 aciertos. Tus 3 errores llegaron en el último minuto.» */
    val sentence: String get() = "$bestLine $errorsLine"
  }

  /**
   * El ritmo de la ÚLTIMA partida de [gameId] (con la pista de hoy) que tenga ritmo completo ([TimeTrialResult.hasPace]); null si ninguna lo tiene (partidas de antes de la versión 17 o de una Unity sin el registro, o ningún juego aún). Solo se cuenta el ritmo: nunca se compara con otras personas.
   */
  fun pace(all: List<TimeTrialResult>, gameId: String): Pace? = TimeTrialDay.ofGame(all, gameId).lastOrNull { it.hasPace }?.let { paceOf(it) }

  /** El ritmo de [result]; null si no tiene ritmo completo. Cada ensayo cae en el medio minuto de su hora (la del final, 120 s, cuenta en el último). */
  fun paceOf(result: TimeTrialResult): Pace? {
    val trials = result.trials?.takeIf { result.hasPace } ?: return null
    val hits = IntArray(HALF_MINUTES)
    val errors = IntArray(HALF_MINUTES)
    for (i in 0 until trials.size) {
      val half = (trials.tMs[i] / HALF_MINUTE_MS).coerceIn(0, HALF_MINUTES - 1)
      if (trials.ok[i]) hits[half]++ else errors[half]++
    }
    val halves = List(HALF_MINUTES) { HalfMinute(hits[it], errors[it]) }
    val top = hits.maxOrNull() ?: 0
    val best = if (top > 0) halves.indices.filter { hits[it] == top } else emptyList()
    val errorHalves = halves.indices.filter { errors[it] > 0 }
    return Pace(result, halves, best, errorHalves, bestLine(halves, best), errorsLine(errorHalves, errors.sum()))
  }

  /** El medio minuto, dicho con los mismos números del eje del gráfico («el de 30 a 60»; la unidad, «segundos», va una vez al final de la frase): así «el segundo» no se lee como «un segundo» (Opus, 31-oct). */
  private fun halfName(i: Int) = "el de ${PACE_AXIS[i]}"
  private val HALF_ORDINALS = listOf("el primer medio minuto", "el segundo medio minuto", "el tercer medio minuto", "el último medio minuto")

  /**
   * La frase de los aciertos: «Tu mejor medio minuto fue el de 30 a 60 segundos, con 14 aciertos.» Con EMPATE en lo más alto lo dice con todos los empatados («Tus mejores medios minutos fueron el de 0 a 30 y el de 30 a 60 segundos, con 14 aciertos cada uno.»; los tres: «el de 0 a 30, el de 60 a 90 y el de 90 a 120 segundos»; los cuatro: «Los cuatro medios
   * minutos fueron parejos, con 12 aciertos cada uno.»), y sin ningún acierto: «Esta partida no tuvo aciertos.» (sin culpa, sin «el mejor» de la nada). [best] = los medios minutos con más aciertos (vacío si no hubo).
   */
  fun bestLine(halves: List<HalfMinute>, best: List<Int>): String {
    if (best.isEmpty()) return "Tu última partida no tuvo aciertos."
    val hits = TimeTrialDay.hitsText(halves[best.first()].hits)
    return when (best.size) {
      1 -> "En tu última partida, tu mejor medio minuto fue ${halfName(best[0])} segundos, con $hits."
      HALF_MINUTES -> "En tu última partida, los cuatro medios minutos fueron parejos, con $hits cada uno."
      2 -> "En tu última partida, tus mejores medios minutos fueron ${halfName(best[0])} y ${halfName(best[1])} segundos, con $hits cada uno."
      else -> "En tu última partida, tus mejores medios minutos fueron ${best.dropLast(1).joinToString(", ") { halfName(it) }} y ${halfName(best.last())} segundos, con $hits cada uno."
    }
  }

  /**
   * La frase de los errores: dónde llegaron (en qué medio minuto). «Tus 3 errores llegaron en el último minuto.»
   *  - Sin errores: «Sin errores.»
   *  - UN error, en singular: «Tu único error llegó en el segundo medio minuto.»
   *  - Cuándo, según los medios minutos con error [errorHalves] (0 a 3): uno solo, «en el primer / segundo / tercer / último medio minuto»; el primero y el segundo, «en el primer minuto»; el tercero y el cuarto, «en el último minuto»; el segundo y el tercero, «en el medio de la partida»;
   *    cualquier otro reparto, «se repartieron durante la partida» (con otro verbo: «Tus 5 errores se repartieron durante la partida, …»).
   * [errors] = cuántos errores hubo en total.
   */
  fun errorsLine(errorHalves: List<Int>, errors: Int): String {
    if (errors == 0 || errorHalves.isEmpty()) return "Sin errores."
    val whenText = when (errorHalves) {
      listOf(0, 1) -> "en el primer minuto"
      listOf(2, 3) -> "en el último minuto"
      listOf(1, 2) -> "en el medio de la partida"
      else -> if (errorHalves.size == 1) "en ${HALF_ORDINALS[errorHalves[0]]}" else null
    }
    val subject = when {
      whenText == null -> "Tus $errors errores se repartieron durante la partida"
      errors == 1 -> "Tu único error llegó $whenText"
      else -> "Tus $errors errores llegaron $whenText"
    }
    return "$subject."
  }

  // ------------------------------------------------------------------ la losa «Tu ritmo en la última partida» (Tarea 201; `auditoria-fable/nubi-plus-comparacion.md` §2.3 y §11)

  const val PACE_TITLE = "Tu ritmo en 2 minutos"

  /** Hay marcas en el juego pero ninguna partida con el ritmo guardado (las de antes de la versión 17): la losa solo dice cuándo aparece. */
  const val PACE_WAIT = "Tu ritmo aparece desde tu próxima partida del Contrarreloj."

  /** La leyenda del gráfico, en palabras: el color nunca dice solo lo que es cada barra. */
  const val PACE_LEGEND = "Barras lila: aciertos. Barras coral: errores."

  /**
   * Lo que dice el eje bajo cada medio minuto (14 sp) y, debajo de los cuatro, la unidad ESCRITA ([PACE_AXIS_UNIT]): la lámina de Fable decía «0 a 30 s», pero «s» por «segundos» no se escribe en ninguna parte de la app (regla «sin palabras sueltas» de Ricardo, `VisibleTextsGuardTest`)
   * y «0 a 30 segundos» no cabe en un cuarto de la ventanilla a 393 dp sin partirse cuatro veces.
   */
  val PACE_AXIS = listOf("0 a 30", "30 a 60", "60 a 90", "90 a 120")
  const val PACE_AXIS_UNIT = "segundos"

  /** Lo que lee el lector de pantalla del gráfico, medio minuto por medio minuto: «De 0 a 30 segundos: 11 aciertos y 0 errores. De 30 a 60 segundos: 14 aciertos y 1 error. …» (las cifras sueltas no se leen aparte). */
  fun paceSpoken(pace: Pace): String = pace.halves.mapIndexed { i, h ->
    "De ${i * HALF_MINUTE_MS / 1000} a ${(i + 1) * HALF_MINUTE_MS / 1000} segundos: ${TimeTrialDay.hitsText(h.hits)} y ${errorsText(h.errors)}."
  }.joinToString(" ")

  /** Los medios minutos cuya cifra de aciertos se resalta: el mejor (o los empatados en lo más alto); ninguno si no hubo aciertos o si los cuatro fueron parejos (no hay «mejor» que destacar). */
  fun paceHighlight(pace: Pace): List<Int> = if (pace.bestHalves.isEmpty() || pace.bestHalves.size == HALF_MINUTES) emptyList() else pace.bestHalves
}
