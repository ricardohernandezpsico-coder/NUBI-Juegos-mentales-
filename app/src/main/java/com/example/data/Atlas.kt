package com.example.data

/**
 * El atlas de "La estrella intrusa" (juego estrella de Lenguaje; ver docs/diseno-estrella-intrusa.md): cada regla que se resuelve
 * bien gana su lámina (la figura de estrellas de esa regla, con su nombre). Unity manda en cada partida las láminas nuevas, las
 * reglas falladas (por repasar) y las repasadas; aquí se guardan y se convierten en lo que la persona lee. Lógica pura con pruebas.
 *
 * Las medidas de la partida salen de UNA sola fuente (aciertos y rondas por tipo): la barra "Trampas" (5 de 8) y "Las trampas te
 * engañaron 3 de 8" son la misma cuenta dicha de dos maneras, y nunca se muestran números que no cuadren.
 */
data class AtlasState(
  /** Regla → día (días desde 1970) en que se ganó su lámina. */
  val plates: Map<String, Int> = emptyMap(),
  /** Reglas cuya lámina se ganó «con nombre propio» (se acertó «¿Qué las une?»). */
  val named: Set<String> = emptySet(),
  /** Reglas falladas que esperan un repaso. */
  val review: Set<String> = emptySet()
)

object Atlas {
  /** Rondas mínimas para dar la medida de una categoría, y trampas mínimas para hablar de ellas (igual que en Unity). */
  const val MIN_CATEGORY_ROUNDS = 3
  const val MIN_TRAP_ROUNDS = 4
  /** Cuántas miniaturas de láminas ganadas hoy se muestran como máximo. */
  const val MAX_THUMBS = 3

  val CATEGORY_NAMES = listOf("Tipo de cosa", "Para qué sirve", "Dónde está o de qué es", "Trampas")

  // ------------------------------------------------------------------ guardado

  fun decodePlates(text: String?): Map<String, Int> {
    val out = LinkedHashMap<String, Int>()
    for (part in text.orEmpty().split(";")) {
      val cut = part.lastIndexOf(':')
      if (cut <= 0) continue
      val day = part.substring(cut + 1).toIntOrNull() ?: continue
      out[part.substring(0, cut)] = day
    }
    return out
  }

  fun encodePlates(plates: Map<String, Int>): String = plates.entries.joinToString(";") { "${it.key}:${it.value}" }

  fun decodeSet(text: String?): Set<String> = text.orEmpty().split(";").map { it.trim() }.filter { it.isNotEmpty() }.toSet()

  fun encodeSet(set: Set<String>): String = set.sorted().joinToString(";")

  /** Lo que mandó Unity de una partida como lista (claves separadas por ';'). */
  fun keys(csv: String?): List<String> = csv.orEmpty().split(";").map { it.trim() }.filter { it.isNotEmpty() }.distinct()

  /**
   * Suma una partida al atlas: las láminas nuevas (con el día de hoy), las «con nombre propio», las reglas falladas (se agregan a
   * las que esperan repaso; una regla ya ganada también puede quedar por repasar) y las repasadas bien (salen de la lista).
   */
  fun record(
    state: AtlasState, newPlates: List<String>, named: List<String>, reviewNew: List<String>, reviewDone: List<String>, today: Int
  ): AtlasState {
    val plates = LinkedHashMap(state.plates)
    for (k in newPlates) if (k !in plates) plates[k] = today
    return AtlasState(
      plates = plates,
      named = state.named + named.filter { it in plates },
      review = (state.review + reviewNew) - reviewDone.toSet()
    )
  }

  // ------------------------------------------------------------------ lo que se lee

  /** Cuántas láminas se ganaron hoy. */
  fun todayCount(state: AtlasState, today: Int): Int = state.plates.values.count { it == today }

  /** Las reglas de las láminas ganadas hoy, las más recientes primero y hasta [MAX_THUMBS]. */
  fun todayPlates(state: AtlasState, today: Int, max: Int = MAX_THUMBS): List<String> =
    state.plates.entries.filter { it.value == today }.map { it.key }.asReversed().take(max)

  /** "14 láminas · 2 nuevas hoy · 1 por repasar" (sin las partes que valen cero); null si el atlas está vacío. */
  fun summary(state: AtlasState, today: Int): String? {
    val total = state.plates.size
    if (total == 0 && state.review.isEmpty()) return null
    val parts = mutableListOf<String>()
    parts += if (total == 1) "1 lámina" else "$total láminas"
    val fresh = todayCount(state, today)
    if (fresh > 0) parts += if (fresh == 1) "1 nueva hoy" else "$fresh nuevas hoy"
    val pending = state.review.size
    if (pending > 0) parts += "$pending por repasar"
    return parts.joinToString(" · ")
  }

  // ------------------------------------------------------------------ medidas de la partida

  /** Una categoría de medida: su nombre, rondas y aciertos, y si es la de menor acierto (se marca con TEXTO, nunca solo con color). */
  data class CategoryRow(val label: String, val seen: Int, val hits: Int, val lowest: Boolean) {
    val percent: Int get() = if (seen <= 0) 0 else Math.round(100f * hits / seen)
  }

  /** Suma una lista de 6 tipos en las 4 categorías de medida. */
  private fun sumByCategory(perType: List<Int>): IntArray {
    val out = IntArray(4)
    perType.forEachIndexed { i, v ->
      val c = when (i) { 0, 1 -> 0; 2 -> 1; 3 -> 2; else -> 3 }
      out[c] += v
    }
    return out
  }

  /**
   * Las categorías con al menos 3 rondas, con la de menor acierto marcada (solo si hay dos o más y no todas empatan). Vacía si no
   * hay datos o no llegan a 6 tipos.
   */
  fun categoryRows(seenType: List<Int>?, hitsType: List<Int>?): List<CategoryRow> {
    if (seenType == null || hitsType == null || seenType.size != 6 || hitsType.size != 6) return emptyList()
    val seen = sumByCategory(seenType)
    val hits = sumByCategory(hitsType)
    val valid = (0 until 4).filter { seen[it] >= MIN_CATEGORY_ROUNDS }
    if (valid.isEmpty()) return emptyList()
    val rate = valid.associateWith { hits[it].toFloat() / seen[it] }
    val low = rate.values.minOrNull() ?: 0f
    val high = rate.values.maxOrNull() ?: 0f
    val markLowest = valid.size >= 2 && high - low > 1e-6f
    val lowestKey = if (markLowest) valid.minByOrNull { rate.getValue(it) } else null
    return valid.map { CategoryRow(CATEGORY_NAMES[it], seen[it], hits[it], it == lowestKey) }
  }

  /** Aciertos y rondas de las trampas (tipos 5 y 6) de la partida. null si no hay datos. */
  fun trapStats(seenType: List<Int>?, hitsType: List<Int>?): Pair<Int, Int>? {
    if (seenType == null || hitsType == null || seenType.size != 6 || hitsType.size != 6) return null
    val seen = seenType[4] + seenType[5]
    val hits = (hitsType[4] + hitsType[5]).coerceAtMost(seen)
    return if (seen <= 0) null else hits to seen
  }

  /** "Las trampas te engañaron 3 de 8" (la misma cuenta que la barra «Trampas»; solo con 4 trampas o más y si engañaron al menos una vez). */
  fun trapFooledLine(seenType: List<Int>?, hitsType: List<Int>?): String? {
    val (hits, seen) = trapStats(seenType, hitsType) ?: return null
    if (seen < MIN_TRAP_ROUNDS) return null
    val fooled = seen - hits
    return if (fooled <= 0) null else "Las trampas te engañaron $fooled de $seen"
  }

  /** La lectura de las trampas, con el truco del diseño. */
  fun trapReading(seenType: List<Int>?, hitsType: List<Int>?): String? {
    val (hits, seen) = trapStats(seenType, hitsType) ?: return null
    if (seen < MIN_TRAP_ROUNDS) return null
    return if (seen - hits > 0)
      "Es normal: el cerebro une lo que suele ir junto. Truco: antes de tocar, pregúntate qué TIPO de cosa es cada una."
    else "Resististe todas: separas bien lo que va junto de lo que es del mismo tipo."
  }

  /** "Nombraste 3 de 5" de «¿Qué las une?» (solo si se jugó el bonus). */
  fun bonusLine(seen: Int?, hits: Int?): String? {
    if (seen == null || hits == null || seen <= 0) return null
    return "Nombraste ${hits.coerceIn(0, seen)} de $seen"
  }

  /** "6 de 8 bien": la cifra grande de la medida (aciertos de rondas). */
  fun totals(seenType: List<Int>?, hitsType: List<Int>?): Pair<Int, Int>? {
    if (seenType == null || hitsType == null || seenType.size != 6 || hitsType.size != 6) return null
    val seen = seenType.sum()
    return if (seen <= 0) null else hitsType.sum().coerceIn(0, seen) to seen
  }

  /** La marca para ver su evolución: % de aciertos de la partida (más alto = mejor). null sin medida. */
  fun mark(seenType: List<Int>?, hitsType: List<Int>?): Float? {
    val (hits, seen) = totals(seenType, hitsType) ?: return null
    return 100f * hits / seen
  }

  /** Mediana del tiempo hasta el toque, dicha en segundos ("1,8 s"); null sin dato. */
  fun speedLine(ms: Int?): String? {
    if (ms == null || ms <= 0) return null
    return "Mediana al tocar: " + String.format(java.util.Locale("es"), "%.1f", ms / 1000f).replace('.', ',') + " s"
  }

  /** Para lectores de pantalla. */
  fun spoken(seenType: List<Int>?, hitsType: List<Int>?): String {
    val (hits, seen) = totals(seenType, hitsType) ?: return "Sin medida"
    return "$hits de $seen rondas bien"
  }
}
