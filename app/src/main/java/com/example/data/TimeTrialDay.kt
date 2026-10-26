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
 * Es gratis y se repite sin límite; no cuenta para el futuro tope de partidas libres ([COUNTS_FOR_FREE_CAP]; el Contrarreloj libre en cualquier juego será Plus en la fase 1b). No compara con otras personas (`Features.COMPARE` sigue apagado).
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

  /** El rótulo del botón de Hoy. Una palabra por concepto: nunca «Reto», «Desafío» ni «Con reloj» para esto. */
  const val LABEL = "Contrarreloj de hoy"

  /** «38 aciertos» / «1 acierto»: una marca (la de [TimeTrialResult.mark]). */
  fun hitsText(hits: Int): String = if (hits == 1) "1 acierto" else "$hits aciertos"

  /** «Tu récord: 41 aciertos.» (la línea plana bajo el botón ancho de Hoy). */
  fun recordLine(record: Int): String = "Tu récord: ${hitsText(record)}."

  /** «Marca nueva: 43 aciertos en Tinta o Palabra.» (el recuadro plano de Hoy: una sola línea). */
  fun newMarkLine(hits: Int, title: String): String = "Marca nueva: ${hitsText(hits)} en $title."

  /** «hoy 38 aciertos». */
  fun todayText(hits: Int): String = "hoy ${hitsText(hits)}"

  /**
   * La segunda línea del botón de Hoy. Ancho (la ruta completa): «Tinta o Palabra · 2 minutos» o, jugado, «Tinta o Palabra · hoy 38 aciertos». Con letra del sistema de 1,3 o más ([large]) solo lo que cambia («Tinta o Palabra», «hoy 38 aciertos»): la losa no crece de más.
   * En media fila (la ruta sigue) no hay segunda línea salvo «otra vez» (jugado). Null = sin segunda línea.
   */
  fun secondLine(title: String, state: TimeTrialState, todayHits: Int?, wide: Boolean, large: Boolean): String? = when {
    state == TimeTrialState.PENDIENTE -> if (!wide) null else if (large) title else "$title · $MINUTES_TEXT"
    !wide -> "otra vez"
    large -> todayText(todayHits ?: 0)
    else -> "$title · ${todayText(todayHits ?: 0)}"
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
