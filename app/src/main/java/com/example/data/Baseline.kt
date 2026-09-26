package com.example.data

import com.example.model.AgeBand
import com.example.model.DomainType
import com.example.model.GameRegistry

/**
 * "Tu punto de partida": la evaluación inicial del onboarding (al estilo del Fit Test de Lumosity o la evaluación
 * de Peak/Elevate). Tres juegos cortos miden memoria, atención y velocidad; con eso cada uno de los 9 juegos
 * empieza a la medida de la persona y el camino diario prioriza sus metas y su dominio más bajo.
 *
 * Criterios (ver CLAUDE.md, "Punto de partida"):
 * - La DIFICULTAD sale del desempeño medido. La edad y el nivel educacional solo se usan (a) para comparar con
 *   gente parecida y (b) como punto de partida estimado si la persona salta la evaluación; el DDA lo corrige en
 *   la primera partida de cada juego.
 * - Nada de "edad cognitiva" ni promesas de salud: es un punto de partida para jugar, no un diagnóstico.
 * Lógica pura (sin Android) para poder probarla.
 */
enum class Education(val label: String) {
  BASICA("Educación básica"),
  MEDIA("Educación media"),
  TECNICA("Técnica o profesional"),
  UNIVERSITARIA("Universitaria"),
  POSTGRADO("Magíster o doctorado"),
  NO_DICE("Prefiero no decir")
}

object BaselinePlan {
  data class Step(val gameId: String, val domain: DomainType, val timed: Boolean, val measures: String)

  /** Los 3 juegos de la evaluación, en orden (ver la elección en CLAUDE.md; se puede cambiar acá). */
  val steps = listOf(
    Step("secuencia", DomainType.MEMORIA, timed = false, measures = "Memoria: repite secuencias de luces cada vez más largas"),
    Step("stroop", DomainType.ATENCION, timed = true, measures = "Atención: responde a la tinta o a la palabra, según la regla"),
    Step("comparacion", DomainType.VELOCIDAD, timed = true, measures = "Velocidad: elige el mayor lo más rápido que puedas")
  )

  /** Nivel (1-5) con que arrancan los juegos de la evaluación: el medio de la escala, algo más suave en mayores. */
  fun startLevel(age: AgeBand?): Int = if (age == AgeBand.SENIOR) 2 else 3
}

/** Resultado de la evaluación: rating 0..1 por dominio; [measured] = los que se midieron (el resto se estima). */
data class Baseline(
  val timestamp: Long,
  val measured: Map<DomainType, Float>,
  val domains: Map<DomainType, Float>
)

/** Secuencia Lumínica informa el nivel más alto alcanzado (1..16+); se lleva a 0..1 como los demás juegos. */
fun ratingFromSequencePeak(peakLevel: Int): Float = ((peakLevel - 1) / 15f).coerceIn(0f, 1f)

/**
 * Punto de partida ESTIMADO cuando se salta la evaluación: la referencia provisional ajustada levemente por edad
 * y nivel educacional (efectos conocidos en las normas de pruebas cognitivas; valores chicos a propósito, porque
 * el DDA corrige enseguida y es preferible empezar algo fácil que frustrar).
 */
fun priorRating(age: AgeBand?, education: Education?): Float {
  var r = Percentile.PROVISIONAL_MEAN
  r += when (age) {
    AgeBand.SENIOR -> -0.10f
    AgeBand.UNDER_18 -> -0.05f
    else -> 0f
  }
  r += when (education) {
    Education.BASICA -> -0.06f
    Education.MEDIA -> -0.03f
    Education.UNIVERSITARIA -> 0.03f
    Education.POSTGRADO -> 0.05f
    else -> 0f
  }
  return r.coerceIn(0.1f, 0.9f)
}

/** Rating 0..1 -> nivel de la app (1-5). */
fun levelFromRating(rating: Float): Int = (1 + (rating.coerceIn(0f, 1f) * 5f).toInt()).coerceIn(1, 5)

/**
 * Arma el mapa con los ratings medidos por juego ([measuredByGame]: id -> 0..1). Los dominios que la evaluación
 * no mide (razonamiento, lenguaje, cálculo) toman el promedio de los medidos, marcados como estimados.
 */
fun buildBaseline(measuredByGame: Map<String, Float>, timestamp: Long = System.currentTimeMillis()): Baseline {
  val measured = BaselinePlan.steps
    .mapNotNull { s -> measuredByGame[s.gameId]?.let { s.domain to it.coerceIn(0f, 1f) } }
    .toMap()
  val mean = if (measured.isEmpty()) Percentile.PROVISIONAL_MEAN else measured.values.average().toFloat()
  return Baseline(timestamp, measured, DomainType.values().associateWith { measured[it] ?: mean })
}

/** Rating inicial de cada uno de los 9 juegos: el de su dominio en el mapa. */
fun seedRatings(baseline: Baseline): Map<String, Float> =
  GameRegistry.allGames.associate { g -> g.id to (baseline.domains[g.domain] ?: Percentile.PROVISIONAL_MEAN) }

/**
 * Orden de dominios para el camino de hoy: primero las metas elegidas (la más baja primero), después el resto de
 * la más baja a la más alta. [domainLevel] = rating o puntaje normalizado 0..1 por dominio (sin dato = 0: se
 * prioriza lo que nunca se jugó).
 */
fun rankDomainsForSession(goals: Set<DomainType>, domainLevel: Map<DomainType, Float>): List<DomainType> {
  val level = { d: DomainType -> domainLevel[d] ?: 0f }
  val (mine, rest) = DomainType.values().partition { it in goals }
  return mine.sortedBy(level) + rest.sortedBy(level)
}

// ------------------------------------------------------------------ guardado (SharedPreferences)

fun encodeGoals(goals: Set<DomainType>): String = goals.joinToString(",") { it.name }

fun decodeGoals(raw: String?): Set<DomainType> =
  raw.orEmpty().split(",").mapNotNull { n -> DomainType.values().firstOrNull { it.name == n.trim() } }.toSet()

/** Una línea: `timestamp|MEMORIA=0.52,ATENCION=0.40|RAZONAMIENTO=0.46,...` (medidos | todos). */
fun encodeBaseline(b: Baseline): String {
  fun map(m: Map<DomainType, Float>) = m.entries.joinToString(",") { "${it.key.name}=${it.value}" }
  return "${b.timestamp}|${map(b.measured)}|${map(b.domains)}"
}

fun decodeBaseline(raw: String?): Baseline? {
  val parts = raw?.split("|") ?: return null
  if (parts.size != 3) return null
  val ts = parts[0].toLongOrNull() ?: return null
  fun map(s: String): Map<DomainType, Float> = s.split(",").mapNotNull { kv ->
    val (k, v) = kv.split("=").takeIf { it.size == 2 } ?: return@mapNotNull null
    val d = DomainType.values().firstOrNull { it.name == k } ?: return@mapNotNull null
    v.toFloatOrNull()?.let { d to it.coerceIn(0f, 1f) }
  }.toMap()
  val domains = map(parts[2])
  if (domains.isEmpty()) return null
  return Baseline(ts, map(parts[1]), domains)
}
