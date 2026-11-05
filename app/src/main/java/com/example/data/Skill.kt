package com.example.data

import com.example.model.AgeBand
import kotlin.math.exp
import kotlin.math.ln

/**
 * Cómo quiere jugar la persona hoy (28-sep, aprobado por Ricardo; razonamiento completo en
 * `docs/dificultad-y-avance.md`). Cada modo se define por los aciertos que se esperan, no por niveles.
 */
enum class PlayMode(val label: String, val what: String, val short: String) {
  SUAVE("Suave", "Un paso más fácil. Para entrar en calor o un día cansado.", "Un paso más fácil"),
  A_TU_MEDIDA("A tu medida", "El modo donde se mide tu avance.", "Mide tu avance"),
  DESAFIO("Desafío", "Un paso más difícil. Si lo superas, tu avance sube.", "Un paso más difícil"),
  /** «Al límite» y no «Experto» (Tarea 150, B1.3): «Experto» ya es la 4.ª etapa del avance (Inicio · Aprendiz · Hábil · Experto · Maestro) y dos cosas con el mismo nombre en la misma ficha confundían. */
  EXPERTO("Al límite", "Dos pasos más. Para probar tus límites.", "Dos pasos más")
}

/**
 * La escalera de un juego: [levels] niveles (la misma para todas las edades), [ownTarget] = aciertos que busca el
 * juego si no usa el de la edad (hoy ningún juego: el último fue Ruta del Tesoro, 70%, retirada el 4-oct) y [slope] =
 * cuánto caen los aciertos por nivel (logística). La pendiente es un SUPUESTO inicial: 1 por nivel en escaleras de
 * 9 a 16 niveles y 2 en las de 5 a 7, donde cada nivel es un salto mayor; se calibra con datos.
 */
data class Ladder(
  val levels: Int,
  val ownTarget: Float? = null,
  val slope: Float = if (levels <= 7) 2f else 1f,
  /**
   * Tarea 233 (`docs/alargar-dificultad.md` §B.1): el largo de la escalera de la v1, que ANCLA los puntos. Un juego alargado (hoy Carga exacta, de 5 a 8) lleva dos largos: [levels] es el de la escalera de ahora (el que lee el motor de Unity y con el
   * que se guarda el rating) y [pointsLevels] el de antes: los puntos se leen sobre él ([Skill.progress]) y con tope 1, así que quien estaba en el tope sigue con todos los puntos del juego y los niveles nuevos NO suman. En los demás juegos es igual a [levels].
   * La [slope] de un juego alargado se FIJA A MANO (con 8 niveles la fórmula daría 1 en vez de 2 y movería el corrimiento de los mayores).
   */
  val pointsLevels: Int = levels,
  /** Qué escalera es: 1 la de siempre; 2 desde que el juego se alargó (los ratings guardados con la v1 se llevan a la de ahora: Room `Migration(18, 19)`, `star_measures` por la marca de [SkillState.ladderMarks]). */
  val version: Int = 1
) {
  /** Lo que se multiplica un rating guardado con la escalera VIEJA para llevarlo a la de ahora: 5/8 en Carga exacta, 1 en los demás (el nivel de la persona no cambia). */
  val ratingScale: Float get() = pointsLevels.toFloat() / levels
}

/** Límites de la partida sobre el rating normalizado (0..1): techo en Suave, piso en Desafío y Experto. */
data class ModeBounds(val floor: Float? = null, val ceiling: Float? = null)

/**
 * Dificultad, edad y avance con una sola regla (lógica pura con pruebas):
 * - **Tu avance** (0..1) = el punto de la escalera donde se aciertan 8 de 10, igual para todas las edades. El DDA
 *   guarda el rating medido con los aciertos que busca al entrenar (85% en mayores...); acá
 *   se lleva a 80% con `nivel(p) = θ − logit(p) / s`.
 * - **La edad** cambia los aciertos buscados y, con ellos, dónde parte cada modo.
 * - **Los modos** son "A tu medida" con techo (Suave) o piso (Desafío, Experto), calculados desde los aciertos
 *   esperados de la tabla por edad.
 */
object Skill {
  const val READ_AT = 0.80f
  /** Tope de bajada del avance por partida: un mal día se nota, pero no borra semanas. */
  const val MAX_DROP = 0.05f
  const val MIN_TRIALS = 12
  const val MIN_ROUNDS = 6
  /** Juegos de rondas largas: "superado" pide 6 rondas en vez de 12 ensayos. */
  private val LONG_ROUNDS = setOf("satelites", "correo", "secuencia", "anagramas", "calculo", "engranajes", "bodega")


  val ladders: Map<String, Ladder> = mapOf(
    // Carga exacta se alargó de 5 a 8 niveles (Tarea 233): puntos anclados a los 5 de antes, pendiente fija en 2 (la de las escaleras de 5 a 7)
    "stroop" to Ladder(5), "anagramas" to Ladder(5), "calculo" to Ladder(8, slope = 2f, pointsLevels = 5, version = 2), "engranajes" to Ladder(12), "bodega" to Ladder(12),
    "piloto" to Ladder(9),
    "radar" to Ladder(12), "freno" to Ladder(12), "aterrizaje" to Ladder(12), "acoplamiento" to Ladder(12),
    "satelites" to Ladder(12), "meteoros" to Ladder(12), "disparate" to Ladder(12), "intrusa" to Ladder(12),
    "correo" to Ladder(10), "parejas" to Ladder(18),
    "secuencia" to Ladder(16)
  )

  fun ladder(gameId: String): Ladder = ladders[gameId] ?: Ladder(10)

  /** Lo que lleva un rating de la escalera VIEJA a la de ahora (5/8 en Carga exacta, 1 en los demás): para sembrar y para traducir lo guardado. */
  fun ratingScale(gameId: String): Float = ladder(gameId).ratingScale

  /** Las marcas de escalera de una instalación SIN nada guardado (nueva, o después de «Borrar datos»): lo que se juegue desde ahí ya va con la escalera de ahora, así que nada hay que traducir (solo los juegos alargados llevan marca). */
  fun currentLadderMarks(): Map<String, Int> = ladders.filter { it.value.version > 1 }.mapValues { it.value.version }

  fun logit(p: Float): Float = ln(p / (1f - p))
  private fun sigmoid(x: Float): Float = (1.0 / (1.0 + exp(-x.toDouble()))).toFloat()

  /** Aciertos que se esperan en cada modo, por edad (tabla 4.B del documento). Menos de 18 = adultos. */
  fun modeAccuracy(mode: PlayMode, age: AgeBand?): Float {
    val senior = age == AgeBand.SENIOR
    return when (mode) {
      PlayMode.SUAVE -> if (senior) 0.92f else 0.90f
      PlayMode.A_TU_MEDIDA -> if (senior) 0.85f else 0.80f
      PlayMode.DESAFIO -> if (senior) 0.75f else 0.70f
      PlayMode.EXPERTO -> if (senior) 0.65f else 0.60f
    }
  }

  /** Aciertos que busca el juego al entrenar "a tu medida": el suyo propio o el de la edad. */
  fun trainingTarget(gameId: String, age: AgeBand?): Float = ladder(gameId).ownTarget ?: modeAccuracy(PlayMode.A_TU_MEDIDA, age)

  /** Cuánto se corre el modo respecto de "a tu medida", en logit (igual para cualquier juego). */
  private fun modeShift(mode: PlayMode, age: AgeBand?): Float =
    logit(modeAccuracy(PlayMode.A_TU_MEDIDA, age)) - logit(modeAccuracy(mode, age))

  /** Aciertos esperados en ese juego y modo (0..1): la tabla por edad, corrida si el juego busca otros aciertos. */
  fun expectedHits(gameId: String, mode: PlayMode, age: AgeBand?): Float =
    sigmoid(logit(trainingTarget(gameId, age)) - modeShift(mode, age))

  /** El corrimiento de la edad en NIVELES de la escalera: cuántos niveles se corre el punto donde se aciertan 8 de 10 respecto de lo que busca el juego al entrenar. */
  private fun shiftLevels(gameId: String, age: AgeBand?): Float = (logit(trainingTarget(gameId, age)) - logit(READ_AT)) / ladder(gameId).slope

  /**
   * Rating guardado (medido con los aciertos de entrenamiento) → avance 0..1 leído a 8 de 10, ANCLADO al largo de la escalera de la v1 ([Ladder.pointsLevels]) y con tope 1 (Tarea 233): es lo que va a `progress_log` y a los puntos de cada área y de cada juego.
   * En un juego sin alargar es lo de siempre; en uno alargado, quien pasa los niveles de la v1 se queda en 1,0 y los niveles nuevos no suman ([ladderProgress] es el que ve la escalera entera).
   */
  fun progress(gameId: String, rating: Float, age: AgeBand?): Float {
    val l = ladder(gameId)
    return (rating * (l.levels.toFloat() / l.pointsLevels) + shiftLevels(gameId, age) / l.pointsLevels).coerceIn(0f, 1f)
  }

  /**
   * El avance sobre la escalera ENTERA de ahora (Tarea 233): sin el ancla de los puntos, así que en Carga exacta llega a los niveles 6 a 8. Lo leen [LevelLines.levelOf] (la frase «Tu nivel: …» de Avance) y [bounds];
   * los puntos NO: esos son [progress]. En un juego sin alargar es igual a [progress].
   */
  fun ladderProgress(gameId: String, rating: Float, age: AgeBand?): Float {
    val l = ladder(gameId)
    return (rating + shiftLevels(gameId, age) / l.levels).coerceIn(0f, 1f)
  }

  /** Techo o piso de la partida en rating normalizado. A tu medida: sin límites. */
  fun bounds(gameId: String, rating: Float, mode: PlayMode, age: AgeBand?): ModeBounds {
    if (mode == PlayMode.A_TU_MEDIDA || rating < 0f) return ModeBounds()
    val l = ladder(gameId)
    val b = (rating + modeShift(mode, age) / l.slope / l.levels).coerceIn(0f, 1f)
    return if (mode == PlayMode.SUAVE) ModeBounds(ceiling = b) else ModeBounds(floor = b)
  }

  /** Experto se abre al superar un Desafío en ese juego (decisión de Ricardo, 28-sep). */
  fun isOpen(mode: PlayMode, gameId: String, expertOpen: Set<String>): Boolean =
    mode != PlayMode.EXPERTO || gameId in expertOpen

  /**
   * Desafío o Experto superado: después del calentamiento se acertó al menos lo que se busca a tu medida, con un
   * mínimo de ensayos (rondas en los juegos de rondas largas). Con el piso puesto, eso significa que el nivel de
   * 8 de 10 de la persona está sobre el piso.
   */
  fun passed(gameId: String, mode: PlayMode, age: AgeBand?, hits: Int, trials: Int): Boolean {
    if (mode != PlayMode.DESAFIO && mode != PlayMode.EXPERTO) return false
    val min = if (gameId in LONG_ROUNDS) MIN_ROUNDS else MIN_TRIALS
    return trials >= min && hits.toFloat() / trials >= trainingTarget(gameId, age) - 1e-4f
  }

  /** Si esta partida cuenta para el avance: a tu medida siempre; Desafío o Experto solo superados. */
  fun counts(mode: PlayMode, passed: Boolean): Boolean = mode == PlayMode.A_TU_MEDIDA || passed

  /**
   * Nuevo rating guardado. Solo cambia si la partida cuenta ([counts]): se suaviza 60% partida / 40% anterior
   * ([blendDdaRating]) y no baja más de [MAX_DROP]; un Desafío superado nunca lo baja. Si no cuenta, queda igual.
   */
  fun nextRating(previous: Float, sessionEnd: Float, mode: PlayMode, passed: Boolean, gameId: String? = null): Float {
    if (!counts(mode, passed)) return previous
    if (previous < 0f) return sessionEnd.coerceIn(0f, 1f)
    val blended = blendDdaRating(previous, sessionEnd)
    // El tope de bajada se mide en PUNTOS (Tarea 233): en un juego alargado el rating es una fracción de una escalera más larga y 0,05 de rating pesarían más que 0,05 de avance; se escala igual que el rating.
    val maxDrop = MAX_DROP * (gameId?.let { ratingScale(it) } ?: 1f)
    return if (mode == PlayMode.A_TU_MEDIDA) blended.coerceAtLeast(previous - maxDrop) else maxOf(previous, blended)
  }

  /** Etapa 0..4 (Inicio..Maestro): quintos del avance de un juego (0..1). Los PUNTOS (de cada área y de cada juego) y lo que falta para la etapa que sigue viven en [AreaPoints]/[GamePoints] (Tarea 159: una sola escala en toda la app). */
  fun stage(progress: Float): Int = (progress.coerceIn(0f, 1f) * 5f).toInt().coerceIn(0, 4)


  /** El nivel de [gameId] dicho en palabras, «Ya repites 6 luces seguidas» (Tarea 159; ver [LevelLines]). [level] = 1..N de su escalera ([Ladder.levels]); [timed] = la persona juega con reloj (solo cambia Freno de Emergencia). */
  fun levelLine(gameId: String, level: Int, timed: Boolean = false): String = LevelLines.lineAt(gameId, level, timed)
}

/**
 * Lo que se recuerda del avance (SharedPreferences `skill`, solo se agrega): qué juegos ya se MIDIERON (una partida
 * que cuenta: sin eso la carta dice "Sin medir aún", aunque exista un punto de partida estimado), en cuáles se abrió
 * Experto (un Desafío superado) y cuándo se logró cada etapa (quedan con su fecha aunque el avance baje).
 */
data class SkillState(
  val measured: Set<String> = emptySet(),
  val expertOpen: Set<String> = emptySet(),
  val stageDates: Map<String, Map<Int, Long>> = emptyMap(),
  /**
   * La escalera con que están guardados los ratings de cada juego alargado (Tarea 233; ver [Ladder.version]): sin marca = la v1. La app, al arrancar, traduce una sola vez lo que Room no puede tocar (las medidas de `star_measures`) y anota aquí la versión;
   * vive en esta preferencia, que ya va en el respaldo, y no en una nueva (`BackupRulesTest`).
   */
  val ladderMarks: Map<String, Int> = emptyMap()
) {
  /** La versión de escalera con que están guardadas las medidas de [gameId] (1 si nunca se tradujo). */
  fun ladderMark(gameId: String): Int = ladderMarks[gameId] ?: 1

  /** Después de una partida: [counted] = contó para el avance; [progress] = el avance nuevo. */
  fun after(gameId: String, counted: Boolean, passedChallenge: Boolean, progress: Float, now: Long): SkillState {
    if (!counted) return this
    val dates = stageDates[gameId].orEmpty().toMutableMap()
    for (s in 1..Skill.stage(progress)) dates.putIfAbsent(s, now)
    return copy(
      measured = measured + gameId,
      expertOpen = if (passedChallenge) expertOpen + gameId else expertOpen,
      stageDates = stageDates + (gameId to dates)
    )
  }

  fun encode(): String = buildList {
    measured.forEach { add("m|$it") }
    expertOpen.forEach { add("x|$it") }
    stageDates.forEach { (g, m) -> m.forEach { (s, t) -> add("s|$g|$s|$t") } }
    ladderMarks.forEach { (g, v) -> add("l|$g|$v") }
  }.joinToString("\n")

  companion object {
    fun decode(text: String?): SkillState {
      val m = mutableSetOf<String>(); val x = mutableSetOf<String>(); val d = mutableMapOf<String, MutableMap<Int, Long>>(); val l = mutableMapOf<String, Int>()
      text.orEmpty().lines().forEach { line ->
        val p = line.split('|')
        when {
          p.size == 2 && p[0] == "m" -> m += p[1]
          p.size == 2 && p[0] == "x" -> x += p[1]
          p.size == 3 && p[0] == "l" -> p[2].toIntOrNull()?.let { l[p[1]] = it }
          p.size == 4 && p[0] == "s" -> {
            val s = p[2].toIntOrNull(); val t = p[3].toLongOrNull()
            if (s != null && t != null) d.getOrPut(p[1]) { mutableMapOf() }[s] = t
          }
        }
      }
      return SkillState(m, x, d, l)
    }
  }
}
