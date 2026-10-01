package com.example.data

import java.util.Locale

/**
 * Lectura de las medidas de "¿Verdad o disparate?" (juego estrella de Lenguaje, verificación de frases; ver
 * docs/diseno-verdad-o-disparate.md y docs/medidas-juegos-estrella.md). Unity manda las palabras por minuto, el tiempo medio de
 * los aciertos y las frases vistas/acertadas por tipo, y los disparates evidentes y sutiles; aquí se convierten en lo que la
 * persona lee. Lógica pura con pruebas.
 *
 * Tipos de frase, en el orden de la telemetría: 0 = corta, 1 = con complemento, 2 = con negación, 3 = con pausa («, que …,»),
 * 4 = todos / algunos / ningún, 5 = comparación.
 *
 * NO hay ningún perfil de sesgo (tender a decir verdad o disparate): regla de patentes (US 11,839,472). Solo aciertos y tiempos.
 */
object Reading {
  /** Aciertos mínimos de un tipo para dar su tiempo (Unity ya manda -1 si hay menos). */
  const val MIN_TYPE_HITS = 4

  val TYPE_LABELS = listOf("Frases cortas", "Complemento", "Negaciones", "Con pausa", "Todos / algunos", "Comparaciones")

  /** Cómo se nombra cada tipo en la frase "te toman N s más". */
  private val TYPE_SUBJECTS = listOf(
    "Las frases cortas", "Las frases con complemento", "Las negaciones", "Las frases con pausa",
    "Las frases con «todos», «algunos» o «ningún»", "Las comparaciones"
  )

  /** El truco de lectura de cada tipo (null en los dos más simples: no hay nada que aconsejar). */
  private val TIPS = listOf(
    null,
    null,
    "lee la frase sin el «no» y después dala vuelta.",
    "fíjate solo en lo que va después de la segunda coma.",
    "busca un solo ejemplo que la rompa.",
    "imagina las dos cosas una al lado de la otra."
  )

  data class TypeRow(val index: Int, val label: String, val ms: Int, val slowest: Boolean)

  /** "142": las palabras por minuto, o null si no se pudo medir (Unity manda -1 con menos de 10 aciertos). */
  fun wpm(value: Int?): Int? = value?.takeIf { it > 0 }

  /** La marca para ver su evolución: palabras por minuto (más alto = mejor). null sin medida. */
  fun mark(wpm: Int?): Float? = wpm?.takeIf { it > 0 }?.toFloat()

  /** Una fila por tipo con tiempo válido (≥ 4 aciertos), con la más lenta marcada (solo si hay al menos dos filas). */
  fun typeRows(rtMs: List<Int>?): List<TypeRow> {
    val rows = rtMs.orEmpty().take(6).mapIndexedNotNull { i, ms -> if (ms > 0) TypeRow(i, TYPE_LABELS[i], ms, false) else null }
    if (rows.size < 2) return rows
    val max = rows.maxOf { it.ms }
    var marked = false
    return rows.map { r -> if (!marked && r.ms == max) { marked = true; r.copy(slowest = true) } else r }
  }

  /** "0,9 s" con coma decimal fija (sin las culturas del teléfono). */
  fun seconds(ms: Int): String = String.format(Locale("es"), "%.1f", ms / 1000f) + " s"

  /**
   * Lo que frena, en palabras: "Las negaciones te toman 0,9 s más que las frases simples. Es normal." con el truco aparte.
   * Se compara con las frases simples (cortas, o con complemento si no hay cortas). null si no hay con qué comparar, si lo más
   * lento ya es una frase simple o si la diferencia es menor de 0,3 s (entonces [balanced] dice que van parejas).
   */
  fun slowText(rows: List<TypeRow>): String? {
    val slow = rows.firstOrNull { it.slowest } ?: return null
    if (slow.index <= 1) return null
    val simple = rows.firstOrNull { it.index == 0 } ?: rows.firstOrNull { it.index == 1 } ?: return null
    val diff = slow.ms - simple.ms
    if (diff < 300) return null
    return "${TYPE_SUBJECTS[slow.index]} te toman ${seconds(diff)} más que las frases simples. Es normal."
  }

  /** El truco del tipo más lento, o null si no hay (o la diferencia es chica). */
  fun tip(rows: List<TypeRow>): String? {
    if (slowText(rows) == null) return null
    val slow = rows.firstOrNull { it.slowest } ?: return null
    return TIPS[slow.index]?.let { "Truco: $it" }
  }

  /** Cuando ningún tipo se separa del resto (menos de 0,3 s entre el más lento y el más simple). */
  fun balanced(rows: List<TypeRow>): Boolean {
    if (rows.size < 3) return false
    val slow = rows.firstOrNull { it.slowest } ?: return false
    val fast = rows.minOf { it.ms }
    return slow.ms - fast < 300
  }

  /** "27 de 30" y, si hubo disparates sutiles, "· disparates sutiles 6 de 8". */
  fun precisionLine(hits: List<Int>?, seen: List<Int>?, subtleHits: Int?, subtleSeen: Int?): String? {
    val s = seen.orEmpty().sum()
    if (s <= 0) return null
    val h = hits.orEmpty().sum().coerceAtMost(s)
    val base = "$h de $s"
    return if (subtleSeen != null && subtleSeen > 0 && subtleHits != null) "$base · disparates sutiles ${subtleHits.coerceAtMost(subtleSeen)} de $subtleSeen" else base
  }

  /** "14 seguidas" / "1 seguida". */
  fun streakLine(best: Int?): String? = best?.takeIf { it > 0 }?.let { if (it == 1) "1 seguida" else "$it seguidas" }

  /** Fracción (0 a 1) del ancho de la barra de un tipo respecto del más lento. */
  fun barFraction(ms: Int, rows: List<TypeRow>): Float {
    val max = rows.maxOfOrNull { it.ms } ?: return 0f
    return if (max <= 0) 0f else (ms.toFloat() / max).coerceIn(0.05f, 1f)
  }

  /** "142 palabras por minuto" para lectores de pantalla. */
  fun spoken(wpm: Int): String = "$wpm palabras por minuto leyendo y decidiendo"
}
