package com.example.data

import com.example.model.AgeRange

/**
 * La LIGA DE LA SEMANA (Tarea 229; `docs/liga-de-la-semana.md` §2 a §4; decisiones de Ricardo del 4-nov): UNA división por persona, que sale SOLO del Contrarreloj, se sube de a una por semana y es GRATIS. LÓGICA PURA con pruebas (`LeagueTest`): no sabe de pantallas ni de dónde se guarda
 * (`NeuroVidaRepository` guarda el [State] en las preferencias `league_weeks`, con [encode] y [decode]).
 *
 * **Tu semana** = el promedio, redondeado hacia ABAJO como el percentil (nunca dice más de lo cierto), de lo que supera la mejor marca de la semana de cada uno de los cuatro juegos de la referencia estimada para la franja de la persona. Cuenta un juego solo si tiene comparación
 * (`Comparison.gate` en SHOW: hay fuente, la persona tiene franja y el juego ya tiene 3 partidas guardadas) y solo con la pista de hoy (`TimeTrialDay.ofGame`); un juego no jugado no cuenta ni baja el promedio. Sin ningún juego que cuente, la semana queda SIN % y, al cerrarse, baja una división (decisión de Ricardo).
 * **Al cerrar la semana** se sube una si el % llega a la línea de la división de arriba, se queda si llega a la de la suya y baja una si no (nunca de En la plataforma, y nunca más de una por semana). Lo cerrado se guarda y NUNCA se recalcula.
 */
object League {
  /** Las preferencias donde vive el estado (van en el respaldo; «Borrar datos» las limpia) y la llave. */
  const val PREFS = "league_weeks"
  const val KEY = "state"

  /** Una semana cerrada: cuándo empezó, su % (null = sin %), la división con que empezó y la con que terminó. */
  data class Closed(val weekStart: Long, val percent: Int?, val from: Comparison.Division, val to: Comparison.Division) {
    /** Qué pasó al cerrarse: subió, se quedó o bajó. */
    val move: Move get() = when { to.ordinal > from.ordinal -> Move.UP; to.ordinal < from.ordinal -> Move.DOWN; else -> Move.STAY }
  }

  enum class Move { UP, STAY, DOWN }

  /**
   * Todo lo guardado: la semana en que empezó la liga ([startWeek], el lunes a las 00:00 de la semana en que se abrió por primera vez la versión que la trae; null = todavía no se abrió), las semanas cerradas de la más vieja a la más nueva ([weeks]) y la semana cuya hoja del lunes
   * ya se vio ([seenWeek]).
   */
  data class State(val startWeek: Long? = null, val weeks: List<Closed> = emptyList(), val seenWeek: Long? = null) {
    /** La división de ahora: la «terminó» de la última semana cerrada; sin ninguna, En la plataforma (todos parten ahí, también quienes ya tenían partidas: el pasado no se reconstruye). */
    val division: Comparison.Division get() = weeks.lastOrNull()?.to ?: Comparison.Division.PLATFORM

    /** true = todavía no se cerró ninguna semana: esta es la primera. */
    val firstWeek: Boolean get() = weeks.isEmpty()

    /** La hoja del lunes que falta ver: la de la última semana cerrada, una sola vez (si se cerraron varias, solo la última). */
    val pendingSheet: Closed? get() = weeks.lastOrNull()?.takeIf { it.weekStart != seenWeek }
  }

  // ------------------------------------------------------------------ guardar y leer (texto plano: nada que migrar, y sin depender de org.json)

  /** «v1|<inicio o ->|<hoja vista o ->|<inicio>,<%>,<desde>,<hasta>;…» (el % sin dato se escribe «-»). */
  fun encode(state: State): String = "v1|${state.startWeek ?: "-"}|${state.seenWeek ?: "-"}|" +
    state.weeks.joinToString(";") { "${it.weekStart},${it.percent ?: "-"},${it.from.name},${it.to.name}" }

  /** Lee lo guardado; cualquier texto que no se entienda (vacío, de otra versión, roto) da una liga sin empezar: nunca revienta la app. */
  fun decode(text: String?): State = runCatching {
    val parts = text.orEmpty().split("|")
    if (parts.size < 4 || parts[0] != "v1") return@runCatching State()
    val weeks = parts[3].split(";").filter { it.isNotBlank() }.map { w ->
      val f = w.split(",")
      Closed(f[0].toLong(), f[1].takeIf { it != "-" }?.toInt(), Comparison.Division.valueOf(f[2]), Comparison.Division.valueOf(f[3]))
    }
    State(parts[1].takeIf { it != "-" }?.toLong(), weeks, parts[2].takeIf { it != "-" }?.toLong())
  }.getOrDefault(State())

  // ------------------------------------------------------------------ tu semana

  /** Los cuatro juegos de la liga: los del Contrarreloj. */
  val GAMES: List<String> = TimeTrialDay.GAMES

  /** ¿Hay liga para esta persona? Hay con qué comparar (la referencia cubre al menos uno de los cuatro juegos) y tiene una franja de edad con la que compararse. Sin eso no hay liga: ni losa, ni línea en Hoy, ni fila en Tú, y las semanas esperan. */
  fun available(source: Comparison.Source?, ageRange: AgeRange?): Boolean = source != null && Comparison.rangeFor(ageRange, source) != null && GAMES.any { source.covers(it) }

  /** Por qué un juego cuenta o no esta semana: [COUNTS] (tiene comparación y marca), [NO_PLAYS] (sin partidas esta semana), [EARLY] (jugó, pero tiene menos de 3 partidas en total: todavía no se compara) o [NO_COMPARE] (sin franja de edad o sin referencia para el juego). */
  enum class Status { COUNTS, NO_PLAYS, EARLY, NO_COMPARE }

  /** Un juego en una semana: su mejor marca, lo que supera de la referencia (solo si [Status.COUNTS]) y su estado. */
  data class GameWeek(val game: String, val best: Int?, val percent: Int?, val status: Status)

  /**
   * Cómo le fue a [game] en la semana que empezó [weeksAgo] semanas atrás (0 = esta): la mejor marca de ESA semana con la pista de hoy y, si la comparación lo permite, su percentil contra la referencia de la franja (la misma fuente y la misma tabla que la cifra de la página:
   * `Comparison.percentile` sobre los tramos sin suavizar, hacia abajo, tope 99). La regla de «desde la tercera partida» cuenta las partidas guardadas HASTA el fin de esa semana, así una semana vieja que se cierra tarde no se juzga con partidas de después.
   */
  fun gameWeek(source: Comparison.Source?, ageRange: AgeRange?, results: List<TimeTrialResult>, game: String, now: Long, weeksAgo: Int): GameWeek {
    val best = Comparison.bestOfWeek(TimeTrialDay.ofGame(results, game), now, weeksAgo)
    val until = if (weeksAgo == 0) Long.MAX_VALUE else Week.weekStart(now, weeksAgo - 1)
    val gate = Comparison.gate(source, ageRange, game, if (weeksAgo == 0) results else results.filter { it.timestamp < until })
    if (best == null) return GameWeek(game, null, null, Status.NO_PLAYS)
    if (gate == Comparison.Gate.NONE || gate == Comparison.Gate.NEEDS_AGE) return GameWeek(game, best, null, Status.NO_COMPARE)
    if (gate == Comparison.Gate.EARLY) return GameWeek(game, best, null, Status.EARLY)
    val range = Comparison.rangeFor(ageRange, source!!)!!
    return GameWeek(game, best, Comparison.percentile(best, source.distribution(game, listOf(range), weeksAgo)), Status.COUNTS)
  }

  /** Los cuatro juegos de la liga en esa semana, en el orden de [GAMES]. */
  fun weekGames(source: Comparison.Source?, ageRange: AgeRange?, results: List<TimeTrialResult>, now: Long, weeksAgo: Int = 0): List<GameWeek> =
    GAMES.map { gameWeek(source, ageRange, results, it, now, weeksAgo) }

  /** El % de la semana: el promedio de los percentiles de los juegos que cuentan, redondeado hacia abajo (94, 19 y 42 dan 51, no 52); null si ninguno cuenta. */
  fun weekPercent(games: List<GameWeek>): Int? {
    val counted = games.filter { it.status == Status.COUNTS }.mapNotNull { it.percent }
    return if (counted.isEmpty()) null else counted.sum() / counted.size
  }

  // ------------------------------------------------------------------ subir, quedarse, bajar

  /**
   * La división con que termina una semana con [percent] (null = sin %) para quien empezó en [current]: sube una si el % llega a la línea de la de arriba, se queda si llega a la de la suya y baja una si no. Nunca más de una; En la plataforma no baja y la más alta, con 90 % o más, se queda.
   * Una semana sin % baja una, como cualquiera que no llega a su línea.
   */
  fun next(current: Comparison.Division, percent: Int?): Comparison.Division {
    val up = current.up
    return when {
      percent != null && up != null && percent >= up.from -> up
      percent != null && percent >= current.from -> current
      else -> current.down ?: current
    }
  }

  /** Cómo va la semana si terminara hoy. */
  enum class Course {
    /** Subiría. */ UP,
    /** Se quedaría. */ STAY,
    /** Bajaría. */ DOWN,
    /** Sin %: no hay juego que cuente todavía. */ NONE,
    /** En la más alta y con 90 % o más: se queda. */ TOP
  }

  /** La semana en marcha: la división de ahora, el % de ahora (null = ninguno), cómo va y dónde terminaría si cerrara hoy ([target]). [playedButEarly] = hay marcas pero ningún juego cuenta todavía (les faltan partidas). */
  data class Live(val division: Comparison.Division, val percent: Int?, val course: Course, val target: Comparison.Division, val firstWeek: Boolean, val playedButEarly: Boolean = false)

  fun live(state: State, games: List<GameWeek>): Live {
    val percent = weekPercent(games)
    val d = state.division
    val target = next(d, percent)
    val course = when {
      percent == null -> Course.NONE
      target.ordinal > d.ordinal -> Course.UP
      target == d && d.up == null -> Course.TOP
      target == d -> Course.STAY
      else -> Course.DOWN
    }
    return Live(d, percent, course, target, state.firstWeek, playedButEarly = percent == null && games.any { it.status == Status.EARLY })
  }

  // ------------------------------------------------------------------ empezar y cerrar semanas

  /** La liga empieza la semana en que se abre por primera vez la versión que la trae: esa semana ya cuenta, y nada de antes. */
  fun begin(state: State, now: Long): State = if (state.startWeek != null) state else state.copy(startWeek = Week.weekStart(now))

  /** Las semanas que faltan cerrar, de la más vieja a la más nueva: cada una como (el lunes en que empezó, cuántas semanas atrás de [now]). Desde la última cerrada (o el inicio de la liga) hasta la anterior a la actual. */
  fun pendingWeeks(state: State, now: Long): List<Pair<Long, Int>> {
    val start = state.startWeek ?: return emptyList()
    val last = state.weeks.lastOrNull()?.weekStart
    val out = mutableListOf<Pair<Long, Int>>()
    var ago = 1
    while (ago <= MAX_PENDING) {
      val ws = Week.weekStart(now, ago)
      if (ws < start || (last != null && ws <= last)) break
      out += ws to ago
      ago++
    }
    return out.asReversed()
  }

  /** Tope de semanas que se cierran de una vez (diez años): solo para que un reloj roto no gire sin fin. */
  private const val MAX_PENDING = 520

  /**
   * Cierra, en orden, TODAS las semanas pendientes (si la persona no abrió la app en 3 semanas, se cierran 3: cada una con sus partidas o «sin %»), con [percentOf] = el % de esa semana (el lunes en que empezó y cuántas semanas atrás de [now] fue). Cada cierre guarda el % y las dos divisiones, y no se vuelve a calcular.
   */
  fun close(state: State, now: Long, percentOf: (weekStart: Long, weeksAgo: Int) -> Int?): State {
    var current = state
    for ((ws, ago) in pendingWeeks(state, now)) {
      val percent = percentOf(ws, ago)
      val from = current.division
      current = current.copy(weeks = current.weeks + Closed(ws, percent, from, next(from, percent)))
    }
    return current
  }

  /** El % de una semana ya terminada, con las partidas y la referencia de hoy (lo que usa el cierre). */
  fun closedPercent(source: Comparison.Source?, ageRange: AgeRange?, results: List<TimeTrialResult>, now: Long, weeksAgo: Int): Int? = weekPercent(weekGames(source, ageRange, results, now, weeksAgo))

  /** Marca la hoja del lunes de [week] como vista. */
  fun seen(state: State, week: Long): State = state.copy(seenWeek = week)

  // ------------------------------------------------------------------ lo que se dice (`docs/liga-de-la-semana.md` §5)

  /** «Tu división:» de la losa. */
  const val YOUR_DIVISION = "Tu división:"

  /** El rótulo de «Tu semana» y el de la escalera. */
  const val WEEK_LABEL = "Tu semana"
  const val LADDER_TITLE = "Las diez divisiones"
  const val GAMES_TITLE = "Tus juegos de esta semana"
  const val GAMES_NOTE = "Tu semana es el promedio de los juegos que jugaste."

  /** «51 %»: la cifra grande (la pantalla pega el % con [glueNumbers]). */
  fun percentText(percent: Int): String = "$percent %"

  /** «En promedio, esta semana superas al 51 % de la referencia estimada para tu edad.» (con personas reales, futuro: «…de las personas de tu edad.»). */
  fun averageSentence(percent: Int, estimated: Boolean = true): String =
    "En promedio, esta semana superas al $percent % de " + if (estimated) "la referencia estimada para tu edad." else "las personas de tu edad."

  /** La frase del lunes de la losa, según cómo va la semana (§5.1 a). */
  fun mondaySentence(live: Live): String {
    val d = live.division
    val up = d.up
    return when {
      live.firstWeek && live.percent == null && !live.playedButEarly -> "Tu primera semana: todos parten ${Comparison.Division.PLATFORM.label}."
      live.course == Course.UP -> "Si la semana terminara hoy, el lunes subirías a ${live.target.label}."
      live.course == Course.STAY -> "Si la semana terminara hoy, te quedarías ${Comparison.placeIn(d)}." + (up?.let { " Para subir a ${it.label}, te falta llegar al ${it.from} %." } ?: "")
      live.course == Course.TOP -> "Si la semana terminara hoy, te quedarías ${Comparison.placeIn(d)}, la división más alta."
      live.course == Course.DOWN -> "Si la semana terminara hoy, bajarías a ${live.target.label}. Para quedarte, te falta llegar al ${d.from} %."
      else -> noPercentSentence(live)
    }
  }

  private fun noPercentSentence(live: Live): String {
    val d = live.division
    val up = d.up
    val start = if (live.playedButEarly) "Todavía no cuenta ningún juego esta semana: cada uno cuenta desde tu tercera partida." else "Todavía no juegas esta semana."
    val end = when {
      d.down == null && up != null -> "puedes subir a ${up.label}."
      up == null -> "puedes quedarte ${Comparison.placeIn(d)}."
      else -> "puedes quedarte ${Comparison.placeIn(d)} o subir."
    }
    return "$start Con cualquiera de los cuatro juegos $end"
  }

  /** «La semana cierra el domingo. Quedan 4 días.» («Queda 1 día.»; el domingo, «La semana cierra hoy a medianoche.»). [daysLeft] = los días que faltan después de hoy (lunes = 6, domingo = 0). */
  fun closesLine(daysLeft: Int): String = when {
    daysLeft <= 0 -> "La semana cierra hoy a medianoche."
    daysLeft == 1 -> "La semana cierra el domingo. Queda 1 día."
    else -> "La semana cierra el domingo. Quedan $daysLeft días."
  }

  /** Cuántos días quedan después de hoy en la semana de [now] (lunes = 6, domingo = 0). */
  fun daysLeft(now: Long): Int {
    val cal = java.util.Calendar.getInstance().apply { timeInMillis = now }
    return 6 - (cal.get(java.util.Calendar.DAY_OF_WEEK) + 5) % 7
  }

  /** La línea de Hoy, bajo la losa del Contrarreloj, junto al emblema de 24 dp (§5.2): «Despegando · vas para subir a Sobre las nubes». */
  fun hoyLine(live: Live): String {
    val d = live.division
    return "${d.label} · " + when (live.course) {
      Course.UP -> "vas para subir a ${live.target.label}"
      Course.STAY, Course.TOP -> "vas para quedarte"
      Course.DOWN -> "juega para quedarte"
      Course.NONE -> if (d.down == null && d.up != null) "juega para subir" else "juega para quedarte"
    }
  }

  /** «Tu división: Despegando», la fila de Tú (§5.4). */
  fun tuLine(d: Comparison.Division): String = "Tu división: ${d.label}"

  // ---- los cuatro cuadros de «Tus juegos de esta semana» (§5.1 d)

  const val NO_PLAYS = "Sin partidas esta semana"
  const val EARLY = "Cuenta desde tu tercera partida"

  /** La primera línea del cuadro: la mejor marca de la semana («81 aciertos») o que no hubo partidas. */
  fun tileMark(g: GameWeek): String = g.best?.let { TimeTrialDay.hitsText(it) } ?: NO_PLAYS

  /** La segunda línea: «Superas al 94 %», «Cuenta desde tu tercera partida» o nada. */
  fun tilePercent(g: GameWeek): String? = when (g.status) {
    Status.COUNTS -> "Superas al ${g.percent} %"
    Status.EARLY -> EARLY
    else -> null
  }

  /** Lo que dice el lector de pantalla del cuadro, todo junto: «Tinta o Palabra, hoy, 81 aciertos, superas al 94 %». */
  fun tileSpoken(title: String, g: GameWeek, isToday: Boolean, selected: Boolean, locked: Boolean): String = buildString {
    append(title)
    if (isToday) append(", hoy")
    append(", ").append(tileMark(g))
    tilePercent(g)?.let { append(", ").append(it.replaceFirstChar { c -> c.lowercase() }) }
    if (selected) append(", elegido")
    if (locked) append(", con Nubilus Plus")
  }

  // ---- la hoja del lunes (§5.3)

  /** Lo que dice la hoja del lunes: su título, la frase de la semana pasada, lo extra (si el % alcanzaba para más) y lo de esta semana. */
  data class Sheet(val closed: Closed, val title: String, val last: String, val extra: String?, val now: String)

  const val SHEET_OK = "Seguir"

  fun sheet(c: Closed, estimated: Boolean = true): Sheet {
    val to = c.to
    val title = when (c.move) {
      Move.UP -> "¡Subiste a ${to.label}!"
      Move.STAY -> "Sigues ${Comparison.placeIn(to)}"
      Move.DOWN -> "Esta semana estás ${Comparison.placeIn(to)}"
    }
    val last = c.percent?.let { "Tu semana pasada: superaste en promedio al $it % de " + if (estimated) "la referencia estimada para tu edad." else "las personas de tu edad." }
      ?: "Tu semana pasada: no jugaste el Contrarreloj."
    val reach = c.percent?.let { Comparison.division(it) }
    val extra = if (c.move == Move.UP && reach != null && reach.ordinal > to.ordinal) "Alcanzaba para ${reach.label}, pero se sube de a una división por semana." else null
    val up = to.up
    val now = when {
      up == null -> "Esta semana: con el ${to.from} %, te quedas ${Comparison.placeIn(to)}."
      to.down == null -> "Esta semana: con el ${up.from} % subes a ${up.label}."
      else -> "Esta semana: con el ${up.from} % subes a ${up.label}; con el ${to.from} %, te quedas ${Comparison.placeIn(to)}."
    }
    return Sheet(c, title, last, extra, now)
  }

  /** Todo lo que dice la hoja, de un tirón (lo que lee el lector de pantalla). */
  fun sheetSpoken(s: Sheet): String = listOfNotNull(s.title, s.last, s.extra, s.now).joinToString(" ")
}
