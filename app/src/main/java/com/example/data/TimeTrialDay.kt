package com.example.data

/** Cómo va el Contrarreloj de hoy: todavía no se jugó, se jugó sin marca nueva (se puede repetir) o hoy se batió el récord (el día está hecho). */
enum class TimeTrialState { PENDIENTE, JUGADO, MARCA_NUEVA }

/**
 * Una partida de Contrarreloj guardada (una fila de `time_trial_results`): el juego, el día («yyyy-MM-dd», hora local), la versión de la pista con que se jugó, los aciertos y los errores (SIN restar: la MARCA es [mark], aciertos menos errores desde la pista versión 2, Tarea 178), el paso más alto que alcanzó la pista,
 * los segundos de la partida y cuándo terminó. NO es una partida de siempre: no entra en `game_results`, así que no mueve el nivel, los puntos ni lo «medido» de la persona (sí cuenta como día jugado para la racha y las farolas de la semana).
 */
data class TimeTrialResult(
  val id: String = java.util.UUID.randomUUID().toString(),
  val gameId: String,
  val day: String,
  val trackVersion: Int,
  val hits: Int,
  val errors: Int,
  val level: Int,
  val seconds: Int = TimeTrialDay.SECONDS,
  val timestamp: Long = System.currentTimeMillis()
) {
  /** La MARCA de la partida («34 aciertos»): lo que se compara con el récord, las últimas 5 y la semana. */
  val mark: Int get() = TimeTrialDay.markOf(trackVersion, hits, errors)
}

/**
 * El Contrarreloj de hoy, fase 1: local, sin servidor (Tarea 177, 26-oct; `docs/contrarreloj.md`; diseño de Fable en `auditoria-fable/contrarreloj.md`, mecánica A «La escalera de 2 minutos», elegida por Ricardo). Un juego al día, EL MISMO PARA TODAS LAS PERSONAS (rotación fija de la fecha, como Wordle), en
 * una pista fija de 2 minutos que sube un paso cada N aciertos y es la misma ese día para todos; la marca es «N aciertos en 2 minutos», contando los aciertos MENOS los errores (Tarea 178), y se compara solo con tu récord, tus últimas 5 partidas y la mejor de la semana. Reemplaza al «Desafío de hoy» de la Tarea 167 (`DailyChallenge`, retirado). Lógica pura con pruebas (`TimeTrialDayTest`).
 *
 * Es gratis y se repite sin límite; no cuenta para el futuro tope de partidas libres ([COUNTS_FOR_FREE_CAP]). Desde la Tarea 181 (fase 1b) los cuatro juegos también lo tienen en su ficha («Contrarreloj», con la pista de hoy): gratis y sin tope, sin ningún candado (todavía no hay cobro). No compara con otras personas (`Features.COMPARE` sigue apagado).
 * El estado del día se DERIVA de lo guardado ([stateOf]): no hay preferencia pasajera.
 */
object TimeTrialDay {
  /** Lo que dura la partida (s) y cómo se dice. */
  const val SECONDS = 120
  const val MINUTES_TEXT = "2 minutos"

  /** El Contrarreloj de hoy NO cuenta para el futuro tope de partidas libres del día (Plus, `docs/modelo-negocio.md`); hoy no hay tope y esto no se lee: queda dicho para quien lo construya. */
  const val COUNTS_FOR_FREE_CAP = false

  /**
   * La pista de un juego, como la define Unity (`Games/Shared/TimeTrial.cs`; `TimeTrialUnityTest` compara las dos tablas): su VERSIÓN (si cambia, el récord empieza de nuevo), cada cuántos aciertos sube un paso y cuántos pasos tiene.
   */
  data class Track(val version: Int, val stepEvery: Int, val maxLevel: Int)

  /** Los juegos de la primera tanda en el orden de la rotación del día (el mismo orden de `TimeTrial.GameIds` en Unity). */
  val GAMES: List<String> = listOf("stroop", "disparate", "radar", "acoplamiento")

  val TRACKS: Map<String, Track> = mapOf(
    "stroop" to Track(version = 2, stepEvery = 8, maxLevel = 5),
    "disparate" to Track(version = 2, stepEvery = 3, maxLevel = 12),
    "radar" to Track(version = 2, stepEvery = 3, maxLevel = 12),
    "acoplamiento" to Track(version = 2, stepEvery = 2, maxLevel = 12)
  )

  /** Desde esta versión de la pista la marca es aciertos menos errores (con piso en 0); las partidas de la versión 1 (solo aciertos) quedan en el detalle y no se comparan. Tarea 178. */
  const val NET_FROM_VERSION = 2

  /**
   * La MARCA de una partida: aciertos menos errores, con piso en 0 (nunca negativa) desde la pista versión 2; antes, solo los aciertos. Es la misma fórmula que usa Unity (`TimeTrial.Mark`): una prueba en cada lado la vigila con los mismos casos.
   */
  fun markOf(trackVersion: Int, hits: Int, errors: Int): Int = if (trackVersion < NET_FROM_VERSION) hits else (hits - errors).coerceAtLeast(0)

  fun supports(gameId: String): Boolean = gameId in TRACKS

  /** La versión de la pista del juego (la que se manda y la que se compara al guardar un récord). */
  fun trackVersion(gameId: String): Int = TRACKS[gameId]?.version ?: 1

  /** Cuántos pasos tiene la pista del juego («Llegaste al paso 4 de 5»). */
  fun maxLevel(gameId: String): Int = TRACKS[gameId]?.maxLevel ?: 1

  /**
   * El juego del Contrarreloj de [dateKey] («yyyy-MM-dd»): los cuatro de la primera tanda rotan por la FECHA (los días desde 1970 de ese día, módulo 4), así que es el mismo para todas las personas y cada uno vuelve cada cuatro días. No depende de la ruta, de lo medido ni de la persona.
   */
  fun gameFor(dateKey: String): String = GAMES[Math.floorMod(epochDayOf(dateKey), GAMES.size.toLong()).toInt()]

  /** La FECHA como número (aaaammdd, p. ej. 20261026): es la `track_seed` que se manda a Unity; con la versión de la pista y el id del juego fija la semilla de la pista del día. */
  fun trackSeed(dateKey: String): Int = dateKey.filter { it.isDigit() }.toIntOrNull() ?: 0

  /** La partida que Unity devolvió, ya en la forma que se guarda: la marca (`tt_hits`), los errores, el paso más alto de la pista, los segundos y la versión de la pista con que se jugó (sin ella, la de hoy). [day] = el día de la pista. */
  fun resultOf(raw: com.example.model.GamePlayResult, day: String): TimeTrialResult = TimeTrialResult(
    gameId = raw.gameId, day = day, trackVersion = raw.ttTrackVersion ?: trackVersion(raw.gameId), hits = raw.ttHits ?: 0,
    errors = raw.ttErrors ?: (raw.totalTrials - raw.correctAnswers).coerceAtLeast(0), level = raw.ttPeakLevel ?: 1, seconds = raw.ttSeconds ?: SECONDS, timestamp = raw.timestamp
  )

  /** Los días desde 1970 de un «yyyy-MM-dd». */
  fun epochDayOf(dateKey: String): Long = java.time.LocalDate.parse(dateKey).toEpochDay()

  /** Las partidas de [gameId] con la versión de pista de HOY, de la más vieja a la más nueva: las únicas que cuentan para el récord, las últimas 5 y la semana (si cambia la versión, el récord empieza de nuevo). */
  fun ofGame(results: List<TimeTrialResult>, gameId: String): List<TimeTrialResult> =
    results.filter { it.gameId == gameId && it.trackVersion == trackVersion(gameId) }.sortedBy { it.timestamp }

  /**
   * Cómo va el Contrarreloj de [dateKey] con lo guardado: sin partidas de [gameId] ese día, pendiente; con ellas, «marca nueva» si alguna superó el récord que había JUSTO ANTES de jugarla (la primera partida de un juego nunca es marca nueva: no hay nada que superar) y «jugado» si no.
   */
  fun stateOf(results: List<TimeTrialResult>, dateKey: String, gameId: String): TimeTrialState {
    val mine = ofGame(results, gameId)
    val today = mine.filter { it.day == dateKey }
    if (today.isEmpty()) return TimeTrialState.PENDIENTE
    return if (today.any { isNewRecord(mine, it) }) TimeTrialState.MARCA_NUEVA else TimeTrialState.JUGADO
  }

  /** ¿[r] superó el récord que había justo antes? (partidas anteriores por hora, del mismo juego y la misma pista; la primera nunca). */
  fun isNewRecord(mine: List<TimeTrialResult>, r: TimeTrialResult): Boolean {
    val before = mine.filter { it.timestamp < r.timestamp }
    return before.isNotEmpty() && r.mark > before.maxOf { it.mark }
  }

  // ------------------------------------------------------------------ textos (Fable §9.8; «Acoplamiento» en lugar de «Carga exacta»)

  /** El nombre del modo con «de hoy» (lo que lee el lector de pantalla y lo que dice el final). Una palabra por concepto: nunca «Reto», «Desafío» ni «Con reloj» para esto. */
  const val LABEL = "Contrarreloj de hoy"

  /** El rótulo del botón de Hoy (Tarea 180): solo el nombre del modo; el juego de hoy va en su segunda línea. */
  const val BUTTON_LABEL = "Contrarreloj"

  /** «38 aciertos» / «1 acierto»: una marca (la de [TimeTrialResult.mark]). */
  fun hitsText(hits: Int): String = if (hits == 1) "1 acierto" else "$hits aciertos"

  /** «Tu récord: 41 aciertos.» (la línea plana bajo el botón ancho de Hoy). */
  fun recordLine(record: Int): String = "Tu récord: ${hitsText(record)}."

  /** «Marca nueva: 43 aciertos en Tinta o Palabra.» (el recuadro plano de Hoy: una sola línea). */
  fun newMarkLine(hits: Int, title: String): String = "Marca nueva: ${hitsText(hits)} en $title."

  /** «hoy 38 aciertos». */
  fun todayText(hits: Int): String = "hoy ${hitsText(hits)}"

  /** La segunda línea del botón de Hoy (Tarea 180): «Hoy: Tinta o Palabra», en todos los estados (lo demás —los minutos, lo jugado, el récord— va en otras partes o en el lector de pantalla). */
  fun secondLine(title: String): String = "Hoy: $title"

  /** ¿[gameId] es el juego del Contrarreloj de [dateKey] (el del día)? Los otros tres de la primera tanda también se juegan en Contrarreloj desde su ficha (Tarea 181), pero no son «de hoy». */
  fun isOfDay(gameId: String, dateKey: String): Boolean = supports(gameId) && runCatching { gameFor(dateKey) }.getOrNull() == gameId           // una fecha que no se lee no es «de hoy» (no rompe la carga)

  /** El nombre del modo para la pantalla de carga, el final y el lector de pantalla (Tarea 181): «Contrarreloj de hoy» SOLO si es el juego del día; si no, «Contrarreloj». */
  fun nameFor(gameId: String, dateKey: String): String = if (isOfDay(gameId, dateKey)) LABEL else BUTTON_LABEL

  /** La segunda línea del botón de la ficha (Tarea 181): «Tu récord: 41 aciertos.» si hay récord de la pista vigente de ESE juego; si no, «2 minutos». */
  fun sheetSecondLine(record: Int?): String = if (record != null) recordLine(record) else MINUTES_TEXT

  /** Lo que dice el lector de pantalla del botón de la ficha: el nombre del modo (con «de hoy» solo si es el juego del día), el juego, los 2 minutos y el récord. */
  fun sheetDescription(gameId: String, dateKey: String, title: String, record: Int?): String = buildString {
    append(nameFor(gameId, dateKey)).append(", ").append(title).append(", ").append(MINUTES_TEXT)
    if (record != null) append(", tu récord ").append(hitsText(record))
  }

  /** El «yyyy-MM-dd» de una `track_seed` (aaaammdd): la pantalla de carga la lee de la config que va a Unity para saber si la pista es la del día. Una semilla que no es una fecha da null. */
  fun dateKeyOfSeed(seed: Int): String? {
    val s = seed.toString()
    if (s.length != 8) return null
    return runCatching { java.time.LocalDate.of(s.substring(0, 4).toInt(), s.substring(4, 6).toInt(), s.substring(6, 8).toInt()).toString() }.getOrNull()
  }

  /** Lo que dice el lector de pantalla del botón: todo junto, también cuando a la vista solo dice «Contrarreloj de hoy» (media fila). */
  fun description(title: String, state: TimeTrialState, todayHits: Int?, record: Int?): String = buildString {
    append(LABEL).append(", ").append(title).append(", ").append(MINUTES_TEXT)
    if (state != TimeTrialState.PENDIENTE && todayHits != null) append(", ").append(todayText(todayHits)).append(", otra vez")
    if (record != null) append(", tu récord ").append(hitsText(record))
  }

  /** Lo que lee el lector de pantalla del recuadro de «Marca nueva». */
  fun newMarkDescription(hits: Int, title: String): String = newMarkLine(hits, title) + " Puedes jugar otra vez."
}
