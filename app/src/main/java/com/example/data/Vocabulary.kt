package com.example.data

import java.util.Locale
import kotlin.math.roundToInt

/**
 * Lectura de las medidas de "Lluvia de meteoros" (juego estrella de Lenguaje, decisión léxica "ir / no ir"; ver
 * docs/diseno-lluvia-de-meteoros.md y docs/medidas-juegos-estrella.md). Unity manda por banda cuántas palabras reales se
 * vieron y se tocaron, y por tipo de inventada cuántas se vieron y se tocaron (falsas alarmas); aquí se convierten en
 * lo que la persona lee. Lógica pura con pruebas.
 *
 * Bandas: 1 = la conoce casi todo el mundo … 6 = rara (SPALEX; Aguasvivas et al., 2018). Tipos de inventada, en el orden
 * de la telemetría: 0 = obvia, 1 = una letra cambiada, 2 = letras cambiadas de lugar.
 */
object Vocabulary {
  /** Palabras vistas que hacen falta en una banda para decir algo de ella (igual que `MeteorContract.MinBandSeen`). */
  const val MIN_BAND_SEEN = 6
  /** Inventadas vistas de cada tipo para dar el consejo de "tu filtro". */
  const val MIN_DECOYS_FOR_ADVICE = 5
  /** Toques mínimos (comunes y raras) para comparar el tiempo de reconocimiento: Unity manda -1 si hay menos. */
  private const val MIN_BAND_FOR_MARK = 3

  val FILTER_LABELS = listOf("obvias", "una letra cambiada", "letras cambiadas de lugar")

  /**
   * "Tu vocabulario" por banda: % de palabras reconocidas MENOS el % de inventadas tocadas en toda la partida (así no se
   * premia tocar todo). -1 en una banda con menos de [MIN_BAND_SEEN] palabras vistas. Siempre 6 valores.
   */
  fun bandPercents(seen: List<Int>?, hits: List<Int>?, faSeen: List<Int>?, faHits: List<Int>?): List<Int> {
    val decoys = faSeen.orEmpty().sum()
    val tapped = faHits.orEmpty().sum()
    val fa = if (decoys > 0) 100f * tapped / decoys else 0f
    return List(6) { b ->
      val s = seen?.getOrNull(b) ?: 0
      val h = hits?.getOrNull(b) ?: 0
      if (s < MIN_BAND_SEEN) -1 else (100f * h / s - fa).coerceAtLeast(0f).roundToInt()
    }
  }

  val GROUP_LABELS = listOf("Comunes", "Intermedias", "Raras")
  private val groupNames = listOf("comunes", "intermedias", "raras")
  private val groupBands = listOf(listOf(0, 1), listOf(2, 3), listOf(4, 5))

  /**
   * "Tu vocabulario" en tres grupos: comunes (bandas 1-2), intermedias (3-4) y raras (5-6). % reconocido del grupo MENOS el %
   * de inventadas tocadas en la partida. -1 si el grupo tiene menos de [MIN_BAND_SEEN] palabras vistas. Siempre 3 valores.
   */
  fun groupPercents(seen: List<Int>?, hits: List<Int>?, faSeen: List<Int>?, faHits: List<Int>?): List<Int> {
    val decoys = faSeen.orEmpty().sum()
    val tapped = faHits.orEmpty().sum()
    val fa = if (decoys > 0) 100f * tapped / decoys else 0f
    return groupBands.map { bands ->
      val s = bands.sumOf { seen?.getOrNull(it) ?: 0 }
      val h = bands.sumOf { hits?.getOrNull(it) ?: 0 }
      if (s < MIN_BAND_SEEN) -1 else (100f * h / s - fa).coerceAtLeast(0f).roundToInt()
    }
  }

  /** Indice del grupo con menor % reconocido (se marca con TEXTO), solo con dos o mas grupos medidos que no empaten; si no, -1. */
  fun lowestGroup(groups: List<Int>): Int {
    val m = groups.withIndex().filter { it.value >= 0 }
    if (m.size < 2) return -1
    val low = m.minOf { it.value }
    return if (m.maxOf { it.value } == low) -1 else m.first { it.value == low }.index
  }

  private fun qualifier(p: Int): String = when {
    p >= 90 -> "casi todas"
    p >= 70 -> "la mayoría"
    p >= 55 -> "más de la mitad"
    p >= 45 -> "la mitad"
    p >= 25 -> "menos de la mitad"
    else -> "pocas"
  }

  /**
   * La frase de "tu vocabulario" con los tres grupos; los vecinos con el mismo calificativo se juntan. Ej. con 98 / 84 / 43:
   * "Reconoces casi todas las comunes, la mayoría de las intermedias y menos de la mitad de las raras."
   */
  fun phraseFor(groups: List<Int>): String {
    val parts = mutableListOf<Pair<String, MutableList<String>>>()
    groups.forEachIndexed { i, p ->
      if (p < 0) return@forEachIndexed
      val q = qualifier(p)
      val last = parts.lastOrNull()
      if (last != null && last.first == q) last.second.add(groupNames[i]) else parts.add(q to mutableListOf(groupNames[i]))
    }
    if (parts.isEmpty()) return "Juega un poco más para medir tu vocabulario: hacen falta unas cuantas palabras de cada tipo."
    val texts = parts.map { (q, names) ->
      val list = if (names.size == 1) names[0] else names.dropLast(1).joinToString(", ") + (if (names.last().startsWith("i")) " e " else " y ") + names.last()
      if (q == "casi todas") "casi todas las $list" else "$q de las $list"
    }
    val joined = if (texts.size == 1) texts[0] else texts.dropLast(1).joinToString(", ") + " y " + texts.last()
    return "Reconoces $joined."
  }

  /**
   * La marca de este juego para ver su evolución: promedio, ponderado por las palabras vistas, del % reconocido en las
   * bandas 3 a 6 (lo que de verdad distingue un vocabulario amplio). null si ninguna de esas bandas tiene datos.
   */
  fun mark(percents: List<Int>, seen: List<Int>?): Float? {
    var sum = 0f
    var weight = 0f
    for (b in (MIN_BAND_FOR_MARK - 1)..5) {
      val p = percents.getOrNull(b) ?: -1
      if (p < 0) continue
      val w = (seen?.getOrNull(b) ?: 0).toFloat()
      sum += p * w
      weight += w
    }
    return if (weight > 0f) sum / weight else null
  }

  /** "Las comunes, en 0,7 s. Las raras, en 1,1 s.", o null si falta alguna de las dos medianas (-1 = pocas muestras). */
  fun recognitionSentence(commonMs: Int?, rareMs: Int?): String? {
    if (commonMs == null || rareMs == null || commonMs <= 0 || rareMs <= 0) return null
    fun s(ms: Int) = String.format(Locale("es"), "%.1f", ms / 1000f)
    return "Las comunes, en ${s(commonMs)} s. Las raras, en ${s(rareMs)} s."
  }

  /** "Te engañaron 5 de 21 palabras inventadas:", o null si no se vio ninguna inventada. */
  fun filterHeadline(faSeen: List<Int>?, faHits: List<Int>?): String? {
    val seen = faSeen.orEmpty().sum()
    if (seen <= 0) return null
    val tapped = faHits.orEmpty().sum()
    return if (tapped == 0) "No te engañó ninguna de $seen palabras inventadas" else "Te engañaron $tapped de $seen palabras inventadas"
  }

  /** Índice del tipo de inventada que más engañó (en rojo coral en la pantalla), o -1: solo si hay 5+ vistas y se tocó la mitad o más. */
  fun filterHot(faSeen: List<Int>?, faHits: List<Int>?): Int {
    var best = -1
    var bestRate = 0f
    for (i in 0 until 3) {
      val s = faSeen?.getOrNull(i) ?: 0
      val h = faHits?.getOrNull(i) ?: 0
      if (s < MIN_DECOYS_FOR_ADVICE || h * 2 < s) continue
      val r = h.toFloat() / s
      if (r > bestRate) { bestRate = r; best = i }
    }
    return best
  }

  /**
   * El consejo de "tu filtro": solo si las letras cambiadas de lugar engañan MÁS que los otros dos tipos y hay al menos
   * [MIN_DECOYS_FOR_ADVICE] inventadas vistas de cada tipo. null en cualquier otro caso.
   */
  fun filterAdvice(faSeen: List<Int>?, faHits: List<Int>?): String? {
    if (faSeen == null || faHits == null || faSeen.size < 3 || faHits.size < 3) return null
    if (faSeen.take(3).any { it < MIN_DECOYS_FOR_ADVICE }) return null
    val rate = List(3) { faHits[it].toFloat() / faSeen[it] }
    if (rate[2] <= maxOf(rate[0], rate[1])) return null
    return "Las letras cambiadas de lugar engañan a casi todos: leemos la palabra entera. Mira el centro de la palabra para no caer."
  }
}
