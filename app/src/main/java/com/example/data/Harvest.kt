package com.example.data

/**
 * Lectura de las medidas de "Cosecha de palabras" (juego estrella de Lenguaje, fluidez verbal con letras fijas; ver
 * docs/diseno-cosecha-de-palabras.md y docs/medidas-juegos-estrella.md). Unity manda las palabras de las 3 cosechas (sin las
 * ocultas), cuántas de las comunes se encontraron, el % de palabras en racimo, las palabras de los primeros y de los últimos
 * 20 s, la palabra estrella y las comunes que faltaron; aquí se convierten en lo que la persona lee. Lógica pura con pruebas.
 *
 * "Tu manera de buscar" retoma, como DESCRIPCIÓN, el análisis de agrupar y saltar de Troyer, Moscovitch y Winocur (1997): ese
 * estudio se validó con fluidez por categoría o por letra inicial, no con juegos de letras fijas, así que no hay normas ni
 * comparaciones con otras personas, y la pantalla lo dice ("cómo buscaste en esta partida").
 */
object Harvest {
  /** Palabras mínimas para decir cómo se buscó (Unity ya manda -1 con menos). */
  const val MIN_WORDS = 10

  /** Desde este % de racimos se dice que se exprime cada idea; hasta este % de racimos, que se salta rápido. */
  const val MANY_CLUSTERS = 70
  const val MANY_JUMPS = 30

  /** "34": las palabras de la cosecha, o null si no hay dato. */
  fun words(value: Int?): Int? = value?.takeIf { it >= 0 }

  /** "palabra" / "palabras" según la cifra. */
  fun wordsLabel(n: Int): String = if (n == 1) "palabra" else "palabras"

  /** "de las comunes, 21 de 48" (null si no hay comunes medidas). */
  fun commonLine(found: Int?, total: Int?): String? {
    if (found == null || total == null || total <= 0) return null
    return "de las comunes, ${found.coerceIn(0, total)} de $total"
  }

  /** La marca para ver su evolución: % de las comunes encontradas (más alto = mejor). null sin medida. */
  fun mark(found: Int?, total: Int?): Float? =
    if (found == null || total == null || total <= 0) null else 100f * found.coerceIn(0, total) / total

  /** El % de racimos, o null con menos de 10 palabras (Unity manda -1). */
  fun clusterPct(value: Int?): Int? = value?.takeIf { it in 0..100 }

  /** "Racimos 60 %" y "Saltos 40 %". */
  fun clusterLabel(pct: Int): String = "Racimos $pct %"
  fun jumpLabel(pct: Int): String = "Saltos ${100 - pct} %"

  /** La frase de "tu manera de buscar", con el consejo del diseño. */
  fun searchLine(clusterPct: Int?): String? {
    val p = clusterPct(clusterPct) ?: return null
    return when {
      p >= MANY_CLUSTERS -> "Exprimes bien cada idea (casa, casas, caso). Prueba también saltar a otra letra inicial."
      p <= MANY_JUMPS -> "Saltas rápido entre ideas. Cuando una funciona, busca sus parientes: el plural, otra terminación."
      else -> "Combinas bien: exprimes una idea y saltas a otra cuando se agota."
    }
  }

  /** Cómo cambió el ritmo entre el comienzo y el final de las cosechas. */
  enum class Rhythm { STRONG_START, STEADY, LATE_RISE }

  /** Primeros 20 s contra últimos 20 s (promedios de las cosechas). null si no se encontró nada. */
  fun rhythm(first: Int?, last: Int?): Rhythm? {
    if (first == null || last == null || first < 0 || last < 0 || first + last == 0) return null
    return when {
      first >= 2 && last <= first * 0.6f -> Rhythm.STRONG_START
      last >= first + 2 && last > first * 1.3f -> Rhythm.LATE_RISE
      else -> Rhythm.STEADY
    }
  }

  fun rhythmLine(first: Int?, last: Int?): String? = when (rhythm(first, last)) {
    Rhythm.STRONG_START -> "Arrancas fuerte y bajas al final: es lo normal."
    Rhythm.LATE_RISE -> "Te soltaste al final: cada idea te llevó a la siguiente."
    Rhythm.STEADY -> "Mantienes el ritmo de principio a fin."
    null -> null
  }

  /** El consejo cuando se baja al final. */
  fun rhythmTip(first: Int?, last: Int?): String? =
    if (rhythm(first, last) == Rhythm.STRONG_START) "Si te atascas, cambia de idea: otra letra o otra terminación." else null

  /** Fracción (0 a 1) de la barra de [value] respecto del mayor de los dos (siempre algo visible si hay datos). */
  fun barFraction(value: Int, other: Int): Float {
    val max = maxOf(value, other)
    return if (max <= 0) 0f else (value.toFloat() / max).coerceIn(0.05f, 1f)
  }

  /** "También podías: saciar, acosar, rosca, ácaros y orcas." (hasta 5; null si no faltó ninguna). */
  fun missedLine(words: List<String>?): String? {
    val w = words.orEmpty().filter { it.isNotBlank() }.take(5)
    if (w.isEmpty()) return null
    val list = if (w.size == 1) w[0] else w.dropLast(1).joinToString(", ") + " y " + w.last()
    return "También podías: $list."
  }

  /** "La palabra estrella" cuando se encontró; si no, "tu palabra más larga" (o null sin ninguna). */
  fun starTitle(star: String?, best: String?): Pair<String, String>? = when {
    !star.isNullOrBlank() -> "Tu palabra estrella" to star.uppercase()
    !best.isNullOrBlank() -> "Tu palabra más larga" to best.uppercase()
    else -> null
  }

  /** "34 palabras, de las comunes 21 de 48" para lectores de pantalla. */
  fun spoken(words: Int, found: Int?, total: Int?): String =
    "$words ${wordsLabel(words)}" + (commonLine(found, total)?.let { ", $it" } ?: "")
}
