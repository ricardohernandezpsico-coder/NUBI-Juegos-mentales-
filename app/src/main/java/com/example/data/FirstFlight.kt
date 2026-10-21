package com.example.data

import android.content.Context
import com.example.model.AgeBand
import com.example.model.DomainType
import org.json.JSONArray
import org.json.JSONObject
import kotlin.math.roundToInt

/**
 * «Primer vuelo con Nubi»: el inicio nuevo (`docs/diseno-inicio.md`). Lógica pura (sin pantallas) con pruebas: los pasos, a dónde
 * lleva «Continuar» y «Atrás», cuánto falta, el punto de partida con sus 4 áreas y lo que se guarda en disco para retomar.
 *
 * Es UN solo recorrido con dos formas ([FlightMode]): [FlightMode.FULL] (la primera vez: preguntas + 4 juegos) y [FlightMode.GAMES]
 * («Hacer la evaluación» desde Avance: solo la parte de juegos, sin preguntas).
 */

/**
 * Si cuesta distinguir algunos colores. Solo se guarda (hoy no cambia nada en los juegos) y se elige en Ajustes: desde la Tarea 143 (R4, decisión de Ricardo del 19-oct) el Primer vuelo ya NO lo pregunta, porque una pregunta que no cambia nada
 * es una promesa a medias (y un paso menos para una persona de 60+). El paso `COLOR` se quitó de [FlightStep]; un recorrido guardado con él se sigue leyendo ([FlightStore.decode]).
 */
enum class ColorVision(val label: String) {
  NORMAL("No, veo bien los colores"),
  DIFICULTAD("Sí, a veces me cuesta"),
  NO_SE("No lo sé");

  companion object {
    val DEFAULT = NO_SE
    fun fromStored(name: String?): ColorVision? = entries.firstOrNull { it.name == name }
  }
}

enum class FlightMode { FULL, GAMES }

/**
 * Los pasos del recorrido. [game] = a qué juego (0..3 de [BaselinePlan.steps]) pertenece el paso de juego o de tarjeta.
 * Desde la Tarea 132 («Juega primero», A2) el primer juego va ANTES de la edad y las metas: antes del primer juego solo el nombre (opcional).
 * El orden del enum solo decide el sentido del deslizamiento; lo guardado se lee por NOMBRE (`FlightStore`), así que un recorrido guardado con el orden viejo sigue leyéndose.
 */
enum class FlightStep(val game: Int? = null) {
  HOLA, NOMBRE,
  JUEGO_1(0), TARJETA_1(0),
  EDAD, METAS, DATO,
  JUEGO_2(1), TARJETA_2(1),
  JUEGO_3(2), TARJETA_3(2),
  DIAS,
  JUEGO_4(3), TARJETA_4(3),
  PUNTO, AVISO, CAMINO;

  val isGame: Boolean get() = game != null && name.startsWith("JUEGO")
  val isCard: Boolean get() = game != null && name.startsWith("TARJETA")

  companion object {
    fun gameStep(index: Int): FlightStep = entries.first { it.game == index && it.isGame }
    fun cardStep(index: Int): FlightStep = entries.first { it.game == index && it.isCard }
  }
}

/** Todo lo que el recorrido sabe en un momento: dónde va, lo que respondió y lo que midió. Se guarda entero en disco ([FlightStore]). */
data class FlightState(
  val mode: FlightMode = FlightMode.FULL,
  val step: FlightStep = FirstFlight.firstStep(mode),
  val name: String = "",
  val age: AgeBand? = null,
  val goals: Set<DomainType> = emptySet(),
  val days: Int? = null,
  /** Hora del aviso diario; [NO_REMINDER] = sin aviso; null = todavía no respondió. */
  val hour: Int? = null,
  /** gameId -> rating 0..1 de la versión corta (lo que sembrará el punto de partida). */
  val measured: Map<String, Float> = emptyMap(),
  /** gameId -> el «primer dato» en palabras ([FirstData]); un juego sin datos suficientes no tiene frase. */
  val phrases: Map<String, String> = emptyMap(),
  /** Juegos que se dejaron para después («Terminar después»): su área se estima. */
  val skipped: Set<String> = emptySet(),
  /** El punto de partida ya se guardó (así «Atrás» y volver a abrir no lo repiten). */
  val applied: Boolean = false
) {
  fun gameId(index: Int): String = BaselinePlan.steps[index].gameId

  /** El juego [index] ya no hay que jugarlo: se midió o se dejó para después. */
  fun isGameDone(index: Int): Boolean = gameId(index) in measured || gameId(index) in skipped

  /** Cuántos juegos se midieron de verdad (los saltados no cuentan). */
  val playedCount: Int get() = BaselinePlan.steps.count { it.gameId in measured }

  val allMeasured: Boolean get() = playedCount == BaselinePlan.steps.size
}

object FirstFlight {
  const val NO_REMINDER = -1
  const val DEFAULT_DAYS = 4

  private val full = listOf(
    FlightStep.HOLA, FlightStep.NOMBRE,
    FlightStep.JUEGO_1, FlightStep.TARJETA_1, FlightStep.EDAD, FlightStep.METAS, FlightStep.DATO,
    FlightStep.JUEGO_2, FlightStep.TARJETA_2,
    FlightStep.JUEGO_3, FlightStep.TARJETA_3, FlightStep.DIAS,
    FlightStep.JUEGO_4, FlightStep.TARJETA_4, FlightStep.PUNTO, FlightStep.AVISO, FlightStep.CAMINO
  )
  private val gamesOnly = listOf(
    FlightStep.JUEGO_1, FlightStep.TARJETA_1, FlightStep.JUEGO_2, FlightStep.TARJETA_2,
    FlightStep.JUEGO_3, FlightStep.TARJETA_3, FlightStep.JUEGO_4, FlightStep.TARJETA_4, FlightStep.PUNTO
  )

  fun steps(mode: FlightMode): List<FlightStep> = if (mode == FlightMode.FULL) full else gamesOnly
  fun firstStep(mode: FlightMode): FlightStep = steps(mode).first()

  // ------------------------------------------------------------------ a dónde lleva «Continuar» y «Atrás»

  /**
   * El paso que sigue. Un juego que ya se midió no se vuelve a jugar (de su paso de juego se pasa a su tarjeta) y uno que se
   * dejó para después se salta junto con su tarjeta. **La edad se pregunta UNA sola vez:** [FlightStep.EDAD] se salta cuando ya se sabe
   * (quien viene de un recorrido guardado con el orden viejo ya la respondió; en adelante se cambia en Ajustes). null = era el último paso.
   */
  fun next(s: FlightState): FlightState? {
    val list = steps(s.mode)
    var i = list.indexOf(s.step) + 1
    while (i < list.size) {
      val step = list[i]
      val g = step.game
      when {
        step == FlightStep.EDAD && s.age != null -> i++ // la edad ya se sabe: no se vuelve a preguntar
        g == null -> return s.copy(step = step)
        s.gameId(g) in s.skipped -> i++ // ni el juego ni su tarjeta
        step.isGame && s.gameId(g) in s.measured -> i++ // ya se jugó: directo a la tarjeta
        else -> return s.copy(step = step)
      }
    }
    return null
  }

  /**
   * El paso anterior. «Atrás» vuelve UN paso y nunca cae en el medio de un juego: se salta el paso de juego (para repetirlo
   * habría que tocar «Jugar» otra vez), las tarjetas de los juegos que se dejaron para después y la pregunta de la edad si ya se respondió. null = ya era el primero.
   */
  fun back(s: FlightState): FlightState? {
    val list = steps(s.mode)
    var i = list.indexOf(s.step) - 1
    while (i >= 0) {
      val step = list[i]
      val g = step.game
      val hidden = (step == FlightStep.EDAD && s.age != null) || (g != null && (step.isGame || s.gameId(g) in s.skipped))
      if (!hidden) return s.copy(step = step)
      i--
    }
    return null
  }

  /** «Terminar después» en un juego: ese y los que quedan se dejan para después (su área se estima); lo ya medido se conserva. */
  fun skipRemainingGames(s: FlightState): FlightState {
    val from = s.step.game ?: 0
    val ids = (from until BaselinePlan.steps.size).map { s.gameId(it) }.filter { it !in s.measured }
    return s.copy(skipped = s.skipped + ids)
  }

  /** Llegó el resultado del juego [gameId]: se anota su medida y su primer dato, y se pasa a su tarjeta. */
  fun withResult(s: FlightState, gameId: String, rating: Float, phrase: String?): FlightState {
    val index = BaselinePlan.steps.indexOfFirst { it.gameId == gameId }
    if (index < 0) return s
    return s.copy(
      measured = s.measured + (gameId to rating.coerceIn(0f, 1f)),
      phrases = if (phrase != null) s.phrases + (gameId to phrase) else s.phrases - gameId,
      skipped = s.skipped - gameId,
      step = FlightStep.cardStep(index)
    )
  }

  // ------------------------------------------------------------------ cuánto falta

  /** Segundos que se estima que lleva cada paso (los juegos incluyen su tutorial y la versión corta). Es un cálculo a ojo, no una medición. */
  private fun seconds(step: FlightStep): Int = when {
    step.isGame -> 55
    step.isCard -> 8
    else -> when (step) {
      FlightStep.HOLA -> 5
      FlightStep.NOMBRE -> 8
      FlightStep.EDAD, FlightStep.DIAS, FlightStep.METAS -> 6
      FlightStep.DATO -> 5
      FlightStep.PUNTO -> 12
      FlightStep.AVISO -> 8
      else -> 5
    }
  }

  /** Cuántos minutos faltan (redondeado, mínimo 1) desde el paso actual, sin contar lo que ya se jugó o se dejó para después. */
  fun minutesLeft(s: FlightState): Int {
    val list = steps(s.mode)
    val from = list.indexOf(s.step).coerceAtLeast(0)
    var total = 0
    for (i in from until list.size) {
      val step = list[i]
      val g = step.game
      if (g != null) {
        val id = s.gameId(g)
        if (id in s.skipped || (step.isGame && id in s.measured)) continue
      }
      total += seconds(step)
    }
    return (total / 60f).roundToInt().coerceAtLeast(1)
  }

  /** «Faltan unos 5 minutos» / «Falta 1 minuto». Desde la Tarea 132 ya no se muestra (ver [stationText]); queda para las pruebas. */
  fun minutesLeftText(minutes: Int): String = if (minutes <= 1) "Falta 1 minuto" else "Faltan unos $minutes minutos"

  /** Cuánto de la barra va lleno (0..1): el paso actual sobre el total de pasos de ese recorrido. */
  fun progress(s: FlightState): Float {
    val list = steps(s.mode)
    return (list.indexOf(s.step) + 1).toFloat() / list.size
  }

  // ------------------------------------------------------------------ las farolas de etapa (Tarea 132, A4)

  /**
   * Una farola de etapa de la cabecera del Primer vuelo (en lugar de la barra y de «Faltan unos 5 minutos», una promesa de tiempo que en una persona de 60+ no se cumple). [lit] = esa etapa ya se dio (la primera, desde el inicio:
   * abrir Nubi es el primer paso); [current] = donde está la persona (lleva el aro lima de «hoy», como la semana).
   */
  data class Station(val label: String, val lit: Boolean, val current: Boolean)

  private const val FIRST_STATION = "Empezar"
  private const val LAST_STATION = "Tu punto de partida"
  private const val GAMES_LAST_STATION = "Listo"

  /**
   * En qué etapa del recorrido va [s]: 0 «Empezar» (hola y nombre), 1 a 4 las áreas (su juego, su tarjeta y la pregunta que va con ella) y 5 «Tu punto de partida» (el punto de partida, el aviso y el camino). En la evaluación repetida: 0 a 3 los juegos y 4 «Listo».
   * **La etapa que se muestra es la primera que aún no se dio** (Tarea 133, a pedido de Opus): una etapa cuyo juego se dejó «para después» ya se pasó, así que se salta. Con los 4 juegos para después, las pantallas que siguen (la edad, las metas, el color, los días…) dicen
   * «Paso 6 de 6 · Tu punto de partida» y la etapa nunca salta de la 4 a la 6 mientras queda una por dar.
   */
  fun stationIndex(s: FlightState): Int {
    if (s.mode == FlightMode.GAMES) return s.step.game ?: 4
    var i = when (s.step) {
      FlightStep.HOLA, FlightStep.NOMBRE -> 0
      FlightStep.JUEGO_1, FlightStep.TARJETA_1, FlightStep.EDAD, FlightStep.METAS, FlightStep.DATO -> 1
      FlightStep.JUEGO_2, FlightStep.TARJETA_2 -> 2
      FlightStep.JUEGO_3, FlightStep.TARJETA_3, FlightStep.DIAS -> 3
      FlightStep.JUEGO_4, FlightStep.TARJETA_4 -> 4
      FlightStep.PUNTO, FlightStep.AVISO, FlightStep.CAMINO -> 5
    }
    while (i in 1..4 && s.gameId(i - 1) in s.skipped) i++      // un juego dejado para después: su etapa ya se pasó
    return i
  }

  /**
   * Las farolas: **Empezar · Memoria · Atención · Razonamiento · Lenguaje · Tu punto de partida** (el recorrido completo) o las 4 áreas + **Listo** (la evaluación repetida). Una farola de área se enciende cuando su juego se dio (se jugó, o se dejó
   * «para después»: la etapa se pasó y nada se castiga); la última, cuando el punto de partida se guardó. Como todo sale de lo ya medido, **volver atrás nunca apaga una farola**.
   */
  fun stations(s: FlightState): List<Station> {
    val current = stationIndex(s)
    val areas = BaselinePlan.steps.map { it.domain.displayName }
    val labels = if (s.mode == FlightMode.GAMES) areas + GAMES_LAST_STATION else listOf(FIRST_STATION) + areas + LAST_STATION
    val offset = if (s.mode == FlightMode.GAMES) 0 else 1
    return labels.mapIndexed { i, label ->
      val lit = when {
        s.mode == FlightMode.FULL && i == 0 -> true
        i == labels.lastIndex -> s.applied
        else -> s.isGameDone(i - offset)
      }
      Station(label, lit, current = i == current)
    }
  }

  /**
   * El texto bajo las farolas: «Empezamos juntos» (hola y nombre; Tarea 145: antes «Ya diste el primer paso», que afirmaba algo que la persona todavía no había hecho), «Paso 2 de 6 · Memoria» (un juego o su tarjeta), «Paso 2 de 6 · una pregunta» (la edad, las metas, los días, el aviso) o «Paso 6 de 6»; en la evaluación repetida, «Juego 2 de 4 · Atención» y «Listo · tu punto de partida».
   * Nunca dice cuánto falta en minutos.
   */
  fun stationText(s: FlightState): String {
    val i = stationIndex(s)
    val st = stations(s)
    return if (s.mode == FlightMode.GAMES) {
      if (i >= BaselinePlan.steps.size) "Listo · tu punto de partida" else "Juego ${i + 1} de ${BaselinePlan.steps.size} · ${st[i].label}"
    } else if (i == 0) "Empezamos juntos"                           // la voz de Nubi, corta; no afirma nada que la persona no haya hecho (Tarea 145, antes «Ya diste el primer paso»)
    else {
      val base = "Paso ${i + 1} de ${st.size}"                                                 // el mismo número que las farolas: «de 6» (Opus, 21-oct: «de 15» asustaba a una persona de 60+ y no coincidía con las farolas)
      when {
        s.step.game != null -> "$base · ${st[i].label}"                                         // un juego o su tarjeta llevan el nombre del área
        s.step in QUESTION_STEPS -> "$base · una pregunta"                                      // la edad, las metas, los días y el aviso se dicen preguntas (antes todas llevaban «Paso 2 de 6» a secas)
        else -> base
      }
    }
  }

  /** Las pantallas que preguntan algo: la edad, las metas, los días por semana y el aviso. */
  private val QUESTION_STEPS = setOf(FlightStep.EDAD, FlightStep.METAS, FlightStep.DIAS, FlightStep.AVISO)

  // ------------------------------------------------------------------ el punto de partida

  /** Una de las 4 áreas en la pantalla «Tu punto de partida». [progress] 0..1 (la misma escala de Avance), [stage] su etapa en palabras. */
  data class AreaRow(
    val domain: DomainType,
    val gameId: String,
    val progress: Float,
    val stage: String,
    val measured: Boolean,
    val strong: Boolean,
    val focus: Boolean
  )

  /**
   * Las 4 áreas con su etapa (Inicio…Maestro, [Skill]). La medida de un juego salta de su rating a la escala de Avance con la edad;
   * un área sin medir (dejada para después) toma el promedio de las medidas y se rotula «estimado». «Tu fuerte hoy» = el área medida
   * más alta (solo con 2 o más medidas) y «donde más vamos a jugar» = la primera del camino de hoy ([rankDomainsForSession]: tus
   * metas y lo más bajo), que no sea la fuerte.
   */
  fun startingPoint(s: FlightState, age: AgeBand?): List<AreaRow> {
    val baseline = buildBaseline(s.measured, timestamp = 0L)
    val progress = DomainType.entries.associateWith { d ->
      val step = BaselinePlan.stepFor(d)
      Skill.progress(step.gameId, baseline.domains[d] ?: Baseline.DEFAULT_LEVEL, age)
    }
    val measured = DomainType.entries.filter { BaselinePlan.stepFor(it).gameId in s.measured }
    val strong = if (measured.size >= 2) measured.maxByOrNull { progress.getValue(it) } else null
    val spread = measured.size >= 2 && measured.maxOf { progress.getValue(it) } - measured.minOf { progress.getValue(it) } > 1e-4f
    val ranked = rankDomainsForSession(s.goals, progress)
    val focus = if (measured.isEmpty()) null else ranked.firstOrNull { it != strong }
    return DomainType.entries.map { d ->
      AreaRow(
        domain = d,
        gameId = BaselinePlan.stepFor(d).gameId,
        progress = progress.getValue(d),
        stage = Skill.stageName(progress.getValue(d)),
        measured = d in measured,
        strong = spread && d == strong,
        focus = d == focus
      )
    }
  }
}

/** Los textos del recorrido que dependen del juego (tarjeta de entrada y tarjeta «Acabas de usar tu…»). Aprobados en la maqueta. */
object FlightCopy {
  class GameCopy(
    /** Una línea de cómo se juega (el tutorial está dentro del juego). */
    val how: String,
    /** «Acabas de usar tu [short]». */
    val short: String,
    /** Para qué sirve esa capacidad en la vida diaria (una línea; sin promesas de salud). */
    val why: String
  )

  val games: Map<String, GameCopy> = mapOf(
    "secuencia" to GameCopy(
      how = "Mira qué luces se encienden y tócalas en el mismo orden.",
      short = "memoria de trabajo",
      why = "La memoria de trabajo sostiene información unos segundos mientras la usas: seguir una receta, recordar un número."
    ),
    "freno" to GameCopy(
      how = "Toca el cohete que se enciende, pero si aparece la señal ¡ALTO!, no toques.",
      short = "control de impulsos",
      why = "Frenar a tiempo una respuesta automática es parte del control de la atención."
    ),
    "aterrizaje" to GameCopy(
      how = "Aterriza la nave en el número que te piden, sobre una regla sin marcas.",
      short = "sentido de los números",
      why = "Ubicar cantidades en una recta es la base del cálculo aproximado del día a día."
    ),
    "meteoros" to GameCopy(
      how = "Toca las palabras que existen. Las inventadas, déjalas caer.",
      short = "vocabulario",
      why = "Reconocer palabras rápido apoya la lectura y la conversación."
    )
  )

  fun of(gameId: String): GameCopy = games.getValue(gameId)

  /** Cuando un juego no trajo datos suficientes para una frase. */
  const val NO_DATA_LINE = "Con esta partida corta no alcanzó para un dato, pero tu medida quedó guardada."
  const val SKIPPED_LINE = "Lo dejaste para después: por ahora lo estimamos."
}

// ------------------------------------------------------------------ guardado en disco

/**
 * El recorrido en curso, en disco (SharedPreferences `first_flight`): cada paso se guarda al completarse. Si Android cierra la app (también
 * en medio de un juego, mientras Unity está al frente: ver [com.example.bridge.GameSessionStore]), al volver sigue en el paso siguiente,
 * sin repetir juegos ni preguntas. Es estado PASAJERO: no va al respaldo (restaurarlo en otro teléfono dejaría un recorrido «fantasma»).
 */
object FlightStore {
  private const val PREFS = "first_flight"
  private const val KEY = "state"

  fun encode(s: FlightState): String = JSONObject().apply {
    put("mode", s.mode.name)
    put("step", s.step.name)
    put("name", s.name)
    s.age?.let { put("age", it.name) }
    put("goals", encodeGoals(s.goals))
    s.days?.let { put("days", it) }
    s.hour?.let { put("hour", it) }
    put("measured", JSONObject().also { o -> s.measured.forEach { (k, v) -> o.put(k, v.toDouble()) } })
    put("phrases", JSONObject().also { o -> s.phrases.forEach { (k, v) -> o.put(k, v) } })
    put("skipped", JSONArray(s.skipped.toList()))
    put("applied", s.applied)
  }.toString()

  /** null = ilegible o de otra versión (se vuelve a empezar). */
  fun decode(raw: String?): FlightState? {
    if (raw.isNullOrEmpty()) return null
    return try {
      val o = JSONObject(raw)
      val mode = FlightMode.valueOf(o.getString("mode"))
      // «ENFOQUE» (la pregunta «¿qué te sirve más ver primero?», quitada el 10-oct) y «COLOR» (la pregunta de los colores, quitada el 19-oct) ya no son pasos: quien quedó guardado ahí sigue en el paso que venía después de la tarjeta del segundo juego
      // (JUEGO_3, o el que siga si ese ya se jugó o se dejó para después). Las claves «focus» y «color» guardadas se ignoran: lo guardado se lee por NOMBRE.
      val rawStep = o.getString("step")
      val legacy = rawStep == "ENFOQUE" || rawStep == "COLOR"
      if (legacy && mode != FlightMode.FULL) return null
      val step = if (legacy) FlightStep.TARJETA_2 else FlightStep.valueOf(rawStep)
      if (step !in FirstFlight.steps(mode)) return null
      val measured = o.optJSONObject("measured")?.let { m -> m.keys().asSequence().associateWith { m.getDouble(it).toFloat() } }.orEmpty()
      val phrases = o.optJSONObject("phrases")?.let { m -> m.keys().asSequence().associateWith { m.getString(it) } }.orEmpty()
      val skipped = o.optJSONArray("skipped")?.let { a -> (0 until a.length()).map { a.getString(it) }.toSet() }.orEmpty()
      val decoded = FlightState(
        mode = mode,
        step = step,
        name = o.optString("name"),
        age = AgeBand.entries.firstOrNull { it.name == o.optString("age") },
        goals = decodeGoals(o.optString("goals")),
        days = if (o.has("days")) o.getInt("days") else null,
        hour = if (o.has("hour")) o.getInt("hour") else null,
        measured = measured,
        phrases = phrases,
        skipped = skipped,
        applied = o.optBoolean("applied")
      )
      if (legacy) FirstFlight.next(decoded) ?: decoded else decoded
    } catch (e: Exception) {
      com.example.diag.ErrorLog.record("DATOS", "No se pudo leer el inicio guardado; se vuelve a empezar.", e)
      null
    }
  }

  private fun prefs(context: Context) = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)

  fun load(context: Context): FlightState? = decode(prefs(context).getString(KEY, null))

  /** [state] null = el recorrido terminó: no queda nada guardado. `commit` (no `apply`): si Android cierra la app justo después, el paso ya está en disco. */
  fun save(context: Context, state: FlightState?) {
    val e = prefs(context).edit()
    if (state == null) e.remove(KEY) else e.putString(KEY, encode(state))
    e.commit()
  }

  fun clear(context: Context) = save(context, null)
}
