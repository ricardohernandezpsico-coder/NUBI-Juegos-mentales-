package com.example.data

import com.example.Features
import com.example.model.AgeRange
import kotlin.math.ceil
import kotlin.math.max
import kotlin.math.roundToInt

/**
 * Los DATOS DE MUESTRA de la comparación (Tarea 202): una fuente [Comparison.Source] determinista que se usa SOLO en las builds de depuración y SOLO para ver cómo queda la pantalla ([Comparison.sourceFor]); no son personas: son números inventados a propósito, y la pantalla lo dice
 * con una frase visible («Estos son datos de muestra: solo se ven en la versión de prueba.»). Nunca llegan a la build de la tienda ni se presentan como personas reales (`Features.COMPARE` sigue apagado: el día que haya servidor, esta fuente se reemplaza por sus documentos semanales).
 *
 * Coherente con la Tarea 199 y la lámina de Fable (§7): para una persona de 45 a 54 años que jugó Tinta o Palabra, la semana de hoy tiene **128 personas**, su marca de 43 aciertos supera a **63 de cada 100**, la mitad hizo **39 aciertos o menos** y la cuarta parte más alta **47 o más**.
 * Las otras franjas, semanas y juegos salen de esa misma silueta corrida y escalada con reglas fijas (sin azar): las franjas más jóvenes marcan más; las mayores, menos; cada juego tiene su propio rango. [few] arma el mundo de «pocos»: 23 personas de 45 a 54 años esta semana, y todavía menos
 * en las últimas 4 semanas y en las franjas vecinas, pero sí más de 50 con todas las edades de esta semana; [World.VERY_FEW] baja también a todas las edades de esta semana (45 personas) y solo las últimas 4 semanas llegan a 50.
 */
class SampleComparison(private val world: World) : Comparison.Source {
  /** Los tres mundos de los datos de muestra: «muchos» (lo normal), «pocos» (la franja no llega a 50 pero todas las edades de ESTA semana sí) y «muy pocos» (ni siquiera todas las edades esta semana: solo con las últimas 4 semanas). */
  enum class World {
    /** NO es una muestra: es la referencia estimada (la de la tienda), el valor de fábrica de «[Debug] Comparación» (Tarea 205). Ninguna `SampleComparison` se arma con él. */
    REFERENCE,
    MANY, FEW, VERY_FEW
  }

  constructor(few: Boolean) : this(if (few) World.FEW else World.MANY)

  init { require(world != World.REFERENCE) { "la referencia estimada no es una muestra: EstimatedReference" } }

  companion object {
    /** Las 128 personas de 45 a 54 años que jugaron Tinta o Palabra esta semana, por tramo de 5 aciertos (0 a 4, …, 75 o más). */
    val BASE: List<Int> = listOf(1, 2, 3, 5, 8, 12, 18, 23, 15, 11, 10, 8, 6, 3, 2, 1)
    const val BASE_P25 = 31
    const val BASE_P50 = 39
    const val BASE_P75 = 47

    /** Cuánto se corre la silueta por franja, en tramos de 5 aciertos (+2 = 10 aciertos más). */
    private val RANGE_SHIFT = mapOf(
      AgeRange.R13_17 to 3, AgeRange.R18_29 to 2, AgeRange.R30_44 to 1, AgeRange.R45_54 to 0, AgeRange.R55_64 to -1, AgeRange.R65_74 to -2, AgeRange.R75_PLUS to -3
    )

    /** Y por juego: cada Contrarreloj tiene su propio rango de marcas (Tinta o Palabra, la de la lámina; Acoplamiento, la más baja). */
    private val GAME_SHIFT = mapOf("stroop" to 0, "disparate" to -1, "radar" to -2, "acoplamiento" to -3)

    /** Un pequeño vaivén de semana en semana (0 = esta semana, la de la lámina). */
    private val WEEK_SHIFT = intArrayOf(0, 0, -1, 0, 1, 0, -1)

    private val MANY_PEOPLE = mapOf(
      AgeRange.R13_17 to 41, AgeRange.R18_29 to 211, AgeRange.R30_44 to 187, AgeRange.R45_54 to 128, AgeRange.R55_64 to 96, AgeRange.R65_74 to 71, AgeRange.R75_PLUS to 38
    )

    /** El mundo de «pocos» (esta semana): 23 personas de 45 a 54 años; solo la franja de 18 a 29 llega a 50. */
    private val FEW_PEOPLE = mapOf(
      AgeRange.R13_17 to 8, AgeRange.R18_29 to 70, AgeRange.R30_44 to 4, AgeRange.R45_54 to 23, AgeRange.R55_64 to 14, AgeRange.R65_74 to 9, AgeRange.R75_PLUS to 4
    )

    /** El mundo de «muy pocos» (esta semana): 23 personas de 45 a 54 años y solo 45 en todas las edades, así que «Mientras tanto» tiene que usar las últimas 4 semanas (84 personas). */
    private val VERY_FEW_PEOPLE = mapOf(
      AgeRange.R13_17 to 2, AgeRange.R18_29 to 12, AgeRange.R30_44 to 3, AgeRange.R45_54 to 23, AgeRange.R55_64 to 4, AgeRange.R65_74 to 2, AgeRange.R75_PLUS to 1
    )

    /** Corre los tramos [k] lugares a la derecha (a la izquierda con k negativo); lo que se sale por un borde se junta en el último tramo de ese lado. */
    internal fun shift(bins: List<Int>, k: Int): List<Int> {
      val out = IntArray(bins.size)
      bins.forEachIndexed { i, n -> out[(i + k).coerceIn(0, bins.lastIndex)] += n }
      return out.toList()
    }

    /** Reparte [total] personas en la silueta de [weights] (el método del resto mayor: la suma da justo [total] y, si ya sumaba [total], queda igual). */
    internal fun fit(weights: List<Int>, total: Int): List<Int> {
      val sum = weights.sum()
      if (sum == total) return weights
      val exact = weights.map { it.toDouble() * total / sum }
      val floors = exact.map { it.toInt() }.toMutableList()
      var rest = total - floors.sum()
      exact.indices.sortedByDescending { exact[it] - floors[it] }.forEach { i -> if (rest > 0) { floors[i] += 1; rest -= 1 } }
      return floors
    }

    /** Un cuartil estimado desde el histograma (para las clases juntadas: franjas vecinas, todas las edades o varias semanas): la marca de la persona que ocupa ese lugar, repartida pareja dentro de su tramo. */
    internal fun quantile(bins: List<Int>, people: Int, q: Double): Int {
      val need = max(ceil(people * q).toInt(), 1)
      var cum = 0
      for (i in bins.indices) {
        if (cum + bins[i] >= need) return i * Comparison.BIN_WIDTH + ((need - cum - 1).toDouble() / max(bins[i], 1) * Comparison.BIN_WIDTH).toInt().coerceIn(0, Comparison.BIN_WIDTH - 1)
        cum += bins[i]
      }
      return Comparison.TOP_MARK - 1
    }
  }

  /** Cuántas personas de [range] jugaron esa semana: las de la tabla en la semana de hoy y, antes, un vaivén fijo (más o menos 6 %) o, en el mundo de «pocos», poco más de la cuarta parte. */
  private fun peopleOf(range: AgeRange, weeksAgo: Int): Int {
    val base = when (world) { World.MANY, World.REFERENCE -> MANY_PEOPLE; World.FEW -> FEW_PEOPLE; World.VERY_FEW -> VERY_FEW_PEOPLE }.getValue(range)
    if (weeksAgo == 0) return base
    if (world != World.MANY) return max(1, (base * 0.27).roundToInt())
    val factor = 1.0 + 0.03 * ((weeksAgo * 3) % 5 - 2)
    return (base * factor).roundToInt()
  }

  /** UNA franja, UNA semana: la silueta de la lámina corrida por franja, juego y semana y escalada a las personas de esa clase. */
  private fun single(game: String, range: AgeRange, weeksAgo: Int): Comparison.Distribution {
    val k = RANGE_SHIFT.getValue(range) + WEEK_SHIFT[weeksAgo % WEEK_SHIFT.size] + (GAME_SHIFT[game] ?: 0)
    val people = peopleOf(range, weeksAgo)
    val bins = fit(shift(BASE, k), people)
    fun q(base: Int) = (base + Comparison.BIN_WIDTH * k).coerceAtLeast(0)
    return Comparison.Distribution(people, bins, q(BASE_P25), q(BASE_P50), q(BASE_P75))
  }

  override fun distribution(game: String, ranges: List<AgeRange>?, weeksAgo: Int, weeks: Int): Comparison.Distribution {
    val rs = ranges ?: AgeRange.entries.filter { it != AgeRange.R13_17 || Features.COMPARE_MINORS }
    if (rs.size == 1 && weeks == 1) return single(game, rs[0], weeksAgo)
    val parts = rs.flatMap { r -> (weeksAgo until weeksAgo + weeks).map { single(game, r, it) } }
    val bins = List(Comparison.BINS) { i -> parts.sumOf { it.bins[i] } }
    val people = bins.sum()
    return Comparison.Distribution(people, bins, quantile(bins, people, 0.25), quantile(bins, people, 0.5), quantile(bins, people, 0.75))
  }
}
