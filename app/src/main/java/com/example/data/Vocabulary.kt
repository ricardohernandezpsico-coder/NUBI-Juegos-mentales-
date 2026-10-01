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

  /** Estrellas encendidas (0 a 5, de media en media) para un porcentaje: 100 → 5, 90 → 4,5, 55 → 3 (redondeado a la media). */
  fun litStars(percent: Int): Float = ((percent.coerceIn(0, 100) / 20f) * 2f).roundToInt() / 2f

  private val categories = listOf("comunes" to listOf(0, 1), "poco frecuentes" to listOf(2, 3), "raras" to listOf(4, 5))

  private fun qualifier(p: Int): String = when {
    p >= 80 -> "casi todas"
    p >= 60 -> "la mayoría"
    p >= 35 -> "la mitad"
    p >= 15 -> "pocas"
    else -> "muy pocas"
  }

  /**
   * La frase de "tu vocabulario": agrupa las bandas en comunes (1-2), poco frecuentes (3-4) y raras (5-6) y dice hasta
   * dónde reconoces casi todas. Ej.: "Reconoces casi todas hasta las poco frecuentes; las raras, la mitad."
   */
  fun phrase(percents: List<Int>): String {
    val cats = categories.mapNotNull { (name, bands) ->
      val vals = bands.mapNotNull { percents.getOrNull(it)?.takeIf { v -> v >= 0 } }
      if (vals.isEmpty()) null else name to vals.average().roundToInt()
    }
    if (cats.isEmpty()) return "Juega un poco más para medir tu vocabulario: hacen falta unas cuantas palabras de cada tipo."
    val firstShort = cats.indexOfFirst { it.second < 80 }
    return when {
      firstShort < 0 ->
        if (cats.last().first == "raras") "Reconoces casi todas las palabras, hasta las raras."
        else "Reconoces casi todas las que viste."
      firstShort == 0 -> "De las ${cats[0].first} reconoces ${qualifier(cats[0].second)}."
      else -> "Reconoces casi todas hasta las ${cats[firstShort - 1].first}; las ${cats[firstShort].first}, ${qualifier(cats[firstShort].second)}."
    }
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

  /** "comunes 0,7 s · raras 1,1 s", o null si falta alguna de las dos medianas (-1 = pocas muestras). */
  fun recognitionLine(commonMs: Int?, rareMs: Int?): String? {
    if (commonMs == null || rareMs == null || commonMs <= 0 || rareMs <= 0) return null
    fun s(ms: Int) = String.format(Locale("es"), "%.1f", ms / 1000f)
    return "comunes ${s(commonMs)} s · raras ${s(rareMs)} s"
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
