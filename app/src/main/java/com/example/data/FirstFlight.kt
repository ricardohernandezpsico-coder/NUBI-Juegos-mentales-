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

/** Si cuesta distinguir algunos colores. Por ahora solo se guarda (los juegos lo usarán después: paletas seguras). */
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

/** Los pasos del recorrido. [game] = a qué juego (0..3 de [BaselinePlan.steps]) pertenece el paso de juego o de tarjeta. */
enum class FlightStep(val game: Int? = null) {
  HOLA, NOMBRE, EDAD,
  JUEGO_1(0), TARJETA_1(0),
  METAS, DATO,
  JUEGO_2(1), TARJETA_2(1),
  ENFOQUE, COLOR,
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
  /** Qué le sirve más ver primero al terminar un juego (`result_focus`). */
  val focus: ResultFocus? = null,
  val color: ColorVision? = null,
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
    FlightStep.HOLA, FlightStep.NOMBRE, FlightStep.EDAD,
    FlightStep.JUEGO_1, FlightStep.TARJETA_1, FlightStep.METAS, FlightStep.DATO,
    FlightStep.JUEGO_2, FlightStep.TARJETA_2, FlightStep.ENFOQUE, FlightStep.COLOR,
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
   * dejó para después se salta junto con su tarjeta. null = era el último paso.
   */
  fun next(s: FlightState): FlightState? {
    val list = steps(s.mode)
    var i = list.indexOf(s.step) + 1
    while (i < list.size) {
      val step = list[i]
      val g = step.game
      when {
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
   * habría que tocar «Jugar» otra vez) y las tarjetas de los juegos que se dejaron para después. null = ya era el primero.
   */
  fun back(s: FlightState): FlightState? {
    val list = steps(s.mode)
    var i = list.indexOf(s.step) - 1
    while (i >= 0) {
      val step = list[i]
      val g = step.game
      val hidden = g != null && (step.isGame || s.gameId(g) in s.skipped)
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
      FlightStep.EDAD, FlightStep.ENFOQUE, FlightStep.COLOR, FlightStep.DIAS, FlightStep.METAS -> 6
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

  /** «Faltan unos 5 minutos» / «Falta 1 minuto». */
  fun minutesLeftText(minutes: Int): String = if (minutes <= 1) "Falta 1 minuto" else "Faltan unos $minutes minutos"

  /** Cuánto de la barra va lleno (0..1): el paso actual sobre el total de pasos de ese recorrido. */
  fun progress(s: FlightState): Float {
    val list = steps(s.mode)
    return (list.indexOf(s.step) + 1).toFloat() / list.size
  }

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
      Skill.progress(step.gameId, baseline.domains[d] ?: Percentile.PROVISIONAL_MEAN, age)
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
    s.focus?.let { put("focus", it.name) }
    s.color?.let { put("color", it.name) }
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
      val step = FlightStep.valueOf(o.getString("step"))
      if (step !in FirstFlight.steps(mode)) return null
      val measured = o.optJSONObject("measured")?.let { m -> m.keys().asSequence().associateWith { m.getDouble(it).toFloat() } }.orEmpty()
      val phrases = o.optJSONObject("phrases")?.let { m -> m.keys().asSequence().associateWith { m.getString(it) } }.orEmpty()
      val skipped = o.optJSONArray("skipped")?.let { a -> (0 until a.length()).map { a.getString(it) }.toSet() }.orEmpty()
      FlightState(
        mode = mode,
        step = step,
        name = o.optString("name"),
        age = AgeBand.entries.firstOrNull { it.name == o.optString("age") },
        goals = decodeGoals(o.optString("goals")),
        focus = ResultFocus.fromStored(o.optString("focus")),
        color = ColorVision.fromStored(o.optString("color")),
        days = if (o.has("days")) o.getInt("days") else null,
        hour = if (o.has("hour")) o.getInt("hour") else null,
        measured = measured,
        phrases = phrases,
        skipped = skipped,
        applied = o.optBoolean("applied")
      )
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
