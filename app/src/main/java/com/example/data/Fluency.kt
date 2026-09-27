package com.example.data

import android.content.Context

/**
 * Lectura del final de Constelación de Palabras (fluidez verbal: decir en un minuto todas las palabras de una categoría
 * o que empiecen con una letra). Se puntúa como Troyer, Moscovitch y Winocur (1997): cuántas palabras, cuánto se
 * AGRUPA (tamaño medio de las constelaciones, contado desde la segunda palabra) y cuánto se SALTA (cambios de grupo).
 * Las dos cosas suman palabras: agrupar aprovecha un grupo; saltar a tiempo evita quedarse buscando en uno agotado.
 *
 * Qué NO se dice: no se compara con normas (la tarea por voz en un teléfono, con esta lista de palabras y con dos rondas,
 * no es la de ningún estudio) y no se saca un patrón de una sola ronda con pocas palabras (ver los mínimos).
 */
data class FluencyRound(
  val category: String,
  val valid: Int,
  val repeats: Int,
  val unknown: Int,
  val meanCluster: Float,
  val switches: Int,
  val quarters: List<Int>,
  /** Constelaciones de 2+ palabras: nombre del grupo y cuántas. */
  val constellations: List<Pair<String, Int>>,
  /** Palabras (de la lista) que sumaron. */
  val words: List<String>
) {
  val isLetter: Boolean get() = category.startsWith("letra_")
  val title: String get() = when {
    isLetter -> "Palabras con ${category.removePrefix("letra_").uppercase()}"
    category == "animales" -> "Animales"
    category == "frutas" -> "Frutas y verduras"
    category == "casa" -> "Cosas de la casa"
    else -> category
  }
}

/** Qué pesa más en la partida. */
enum class FluencyStyle { SIN_DATOS, SALTA_MUCHO, AGRUPA_MUCHO, EQUILIBRIO }

object Fluency {
  /** Palabras mínimas en una ronda para leer su estilo. */
  const val MIN_WORDS = 8

  fun rounds(
    categories: List<String>, valid: List<Int>, repeats: List<Int>, unknown: List<Int>, cluster: List<Float>,
    switches: List<Int>, quarters: List<Int>, tops: List<String>, words: List<String>
  ): List<FluencyRound> {
    val n = listOf(categories.size, valid.size, cluster.size, switches.size).min()
    return (0 until n).map { i ->
      FluencyRound(
        category = categories[i],
        valid = valid[i],
        repeats = repeats.getOrElse(i) { 0 },
        unknown = unknown.getOrElse(i) { 0 },
        meanCluster = cluster[i],
        switches = switches[i],
        quarters = if (quarters.size >= 4 * (i + 1)) quarters.subList(4 * i, 4 * i + 4) else emptyList(),
        constellations = tops.getOrElse(i) { "" }.split(';').mapNotNull { part ->
          val colon = part.lastIndexOf(':')
          if (colon <= 0) null else part.substring(0, colon) to (part.substring(colon + 1).toIntOrNull() ?: return@mapNotNull null)
        }.sortedByDescending { it.second },
        words = words.getOrElse(i) { "" }.split('|').filter { it.isNotBlank() }
      )
    }
  }

  /**
   * Estilo de la ronda de categoría (la de letra agrupa por sonido y se lee aparte): SALTA_MUCHO si casi cada palabra es
   * de otro grupo (agrupa < 0,6 y los saltos son 60%+ de las palabras); AGRUPA_MUCHO si las constelaciones son largas
   * y se salta poco (agrupa ≥ 1,5 y saltos < 35% de las palabras); si no, EQUILIBRIO. Desde [MIN_WORDS] palabras.
   */
  fun style(r: FluencyRound): FluencyStyle {
    if (r.valid < MIN_WORDS || r.meanCluster < 0f) return FluencyStyle.SIN_DATOS
    val switchRate = r.switches / r.valid.toFloat()
    return when {
      r.meanCluster < 0.6f && switchRate >= 0.6f -> FluencyStyle.SALTA_MUCHO
      r.meanCluster >= 1.5f && switchRate < 0.35f -> FluencyStyle.AGRUPA_MUCHO
      else -> FluencyStyle.EQUILIBRIO
    }
  }

  fun styleMessage(s: FluencyStyle): String? = when (s) {
    FluencyStyle.SIN_DATOS -> null
    FluencyStyle.SALTA_MUCHO ->
      "Saltas mucho y agrupas poco: casi cada palabra es de un grupo distinto. Truco: cuando encuentres un grupo (la granja, el mar), quédate ahí hasta que se agote; cada grupo trae varias palabras seguidas."
    FluencyStyle.AGRUPA_MUCHO ->
      "Agrupas mucho: armas constelaciones largas. Truco: cuando un grupo se agote, salta enseguida a otro (del mar a la selva) en vez de seguir buscando ahí."
    FluencyStyle.EQUILIBRIO -> "Agrupas y saltas: las dos cosas suman palabras."
  }

  /** Parte del minuto donde salieron más: "al principio" si el primer cuarto tiene 40%+ de las palabras. */
  fun fastStart(r: FluencyRound): Boolean {
    if (r.quarters.size != 4 || r.valid < MIN_WORDS) return false
    return r.quarters[0] >= 0.4f * r.quarters.sum()
  }

  // ------------------------------------------------------------------ tu cielo de palabras

  /** Palabras nuevas para la colección: cada una se guarda como "categoría:palabra". */
  fun merge(existing: Set<String>, rounds: List<FluencyRound>): Pair<Set<String>, Int> {
    val out = existing.toMutableSet()
    var added = 0
    for (r in rounds) for (w in r.words) if (out.add("${r.category}:${w.lowercase()}")) added++
    return out to added
  }
}

/** La colección "tu cielo de palabras" (SharedPreferences, sin migrar Room). */
class WordSkyStore(context: Context) {
  private val prefs = context.getSharedPreferences("word_sky", Context.MODE_PRIVATE)

  fun load(): Set<String> = prefs.getStringSet("words", emptySet())?.toSet() ?: emptySet()

  /** Agrega las palabras de la partida; devuelve (total, nuevas). */
  fun add(rounds: List<FluencyRound>): Pair<Int, Int> {
    val (merged, added) = Fluency.merge(load(), rounds)
    prefs.edit().putStringSet("words", merged).apply()
    return merged.size to added
  }

  fun clear() = prefs.edit().clear().apply()
}
