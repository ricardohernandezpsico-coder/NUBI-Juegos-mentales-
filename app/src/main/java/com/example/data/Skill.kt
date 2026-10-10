package com.example.data

import com.example.model.AgeBand
import kotlin.math.ceil
import kotlin.math.exp
import kotlin.math.ln
import kotlin.math.roundToInt

/**
 * Cómo quiere jugar la persona hoy (28-sep, aprobado por Ricardo; razonamiento completo en
 * `docs/dificultad-y-avance.md`). Cada modo se define por los aciertos que se esperan, no por niveles.
 */
enum class PlayMode(val label: String, val what: String) {
  SUAVE("Suave", "Un paso más fácil. Para entrar en calor o un día cansado."),
  A_TU_MEDIDA("A tu medida", "Justo en tu nivel. Aquí se mide tu avance."),
  DESAFIO("Desafío", "Un paso más difícil. Si lo superas, tu avance sube."),
  EXPERTO("Experto", "Dos pasos más. Para probar tus límites.")
}

/**
 * La escalera de un juego: [levels] niveles (la misma para todas las edades), [ownTarget] = aciertos que busca el
 * juego si no usa el de la edad (hoy ningún juego: el último fue Ruta del Tesoro, 70%, retirada el 4-oct) y [slope] =
 * cuánto caen los aciertos por nivel (logística). La pendiente es un SUPUESTO inicial: 1 por nivel en escaleras de
 * 9 a 16 niveles y 2 en las de 5 a 7, donde cada nivel es un salto mayor; se calibra con datos.
 */
data class Ladder(val levels: Int, val ownTarget: Float? = null, val slope: Float = if (levels <= 7) 2f else 1f)

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

  val STAGES = listOf("Inicio", "Aprendiz", "Hábil", "Experto", "Maestro")

  val ladders: Map<String, Ladder> = mapOf(
    "stroop" to Ladder(5), "anagramas" to Ladder(5), "calculo" to Ladder(5), "engranajes" to Ladder(12), "bodega" to Ladder(12),
    "piloto" to Ladder(9),
    "radar" to Ladder(12), "freno" to Ladder(12), "aterrizaje" to Ladder(12), "acoplamiento" to Ladder(12),
    "satelites" to Ladder(12), "meteoros" to Ladder(12), "disparate" to Ladder(12), "cosecha" to Ladder(10), "intrusa" to Ladder(12),
    "correo" to Ladder(10), "parejas" to Ladder(18),
    "secuencia" to Ladder(16)
  )

  fun ladder(gameId: String): Ladder = ladders[gameId] ?: Ladder(10)

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

  /** "8 de 10", "8 a 9 de 10". */
  fun hitsText(p: Float): String {
    val tenths = (p * 20).roundToInt() / 2f
    val lo = tenths.toInt()
    return if (tenths - lo > 0.01f) "$lo a ${lo + 1} de 10" else "$lo de 10"
  }

  /** Rating guardado (medido con los aciertos de entrenamiento) → avance 0..1 leído a 8 de 10. */
  fun progress(gameId: String, rating: Float, age: AgeBand?): Float {
    val l = ladder(gameId)
    val shiftLevels = (logit(trainingTarget(gameId, age)) - logit(READ_AT)) / l.slope
    return (rating + shiftLevels / l.levels).coerceIn(0f, 1f)
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
  fun nextRating(previous: Float, sessionEnd: Float, mode: PlayMode, passed: Boolean): Float {
    if (!counts(mode, passed)) return previous
    if (previous < 0f) return sessionEnd.coerceIn(0f, 1f)
    val blended = blendDdaRating(previous, sessionEnd)
    return if (mode == PlayMode.A_TU_MEDIDA) blended.coerceAtLeast(previous - MAX_DROP) else maxOf(previous, blended)
  }

  /** Etapa 0..4 (Inicio..Maestro): quintos del avance. */
  fun stage(progress: Float): Int = (progress.coerceIn(0f, 1f) * 5f).toInt().coerceIn(0, 4)

  fun stageName(progress: Float): String = STAGES[stage(progress)]

  /** Puntos que faltan para la etapa siguiente ("a 5 puntos de Experto"); null en Maestro. */
  fun toNextStage(progress: Float): Pair<Int, String>? {
    val s = stage(progress)
    if (s >= 4) return null
    val pts = ceil(((s + 1) * 0.2f - progress) * 100f - 1e-3f).toInt().coerceAtLeast(1)
    return pts to STAGES[s + 1]
  }

  /** Avance en porcentaje entero para mostrar. */
  fun percent(progress: Float): Int = (progress.coerceIn(0f, 1f) * 100f).toInt()

  /** Avance de un área: promedio de sus juegos medidos (null si ninguno) y cuántos de [games] se exploraron. */
  fun area(games: List<String>, measured: Map<String, Float>): Pair<Float?, Int> {
    val vals = games.mapNotNull { measured[it] }
    return (if (vals.isEmpty()) null else vals.average().toFloat()) to vals.size
  }
}

/**
 * Lo que se recuerda del avance (SharedPreferences `skill`, solo se agrega): qué juegos ya se MIDIERON (una partida
 * que cuenta: sin eso la carta dice "Sin medir aún", aunque exista un punto de partida estimado), en cuáles se abrió
 * Experto (un Desafío superado) y cuándo se logró cada etapa (quedan con su fecha aunque el avance baje).
 */
data class SkillState(
  val measured: Set<String> = emptySet(),
  val expertOpen: Set<String> = emptySet(),
  val stageDates: Map<String, Map<Int, Long>> = emptyMap()
) {
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
  }.joinToString("\n")

  companion object {
    fun decode(text: String?): SkillState {
      val m = mutableSetOf<String>(); val x = mutableSetOf<String>(); val d = mutableMapOf<String, MutableMap<Int, Long>>()
      text.orEmpty().lines().forEach { line ->
        val p = line.split('|')
        when {
          p.size == 2 && p[0] == "m" -> m += p[1]
          p.size == 2 && p[0] == "x" -> x += p[1]
          p.size == 4 && p[0] == "s" -> {
            val s = p[2].toIntOrNull(); val t = p[3].toLongOrNull()
            if (s != null && t != null) d.getOrPut(p[1]) { mutableMapOf() }[s] = t
          }
        }
      }
      return SkillState(m, x, d)
    }
  }
}
