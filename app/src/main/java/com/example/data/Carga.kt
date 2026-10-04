package com.example.data

import java.util.Locale

/**
 * «Carga exacta» (juego de Razonamiento que reemplaza a Cálculo Sereno, id `calculo`; ver docs/diseno-carga-exacta.md): la lectura de lo que manda Unity para la
 * pantalla final. Lógica pura con pruebas.
 *
 * El reactor pide una carga y se juntan celdas de a dos hasta llegar. Cada carga termina de una de tres maneras: lograda SIN pista, lograda con la pista de Nubi, o dejada
 * para otra vez. La medida es «cuántas cargas lograste sin pista»; el tiempo medio (solo de las que salieron sin pista) y los «caminos cortos» (los pasos del camino más
 * corto, sin pista) acompañan.
 */
object Carga {
  /** Cargas logradas sin pista para mostrar el tiempo y los caminos cortos («no sacar conclusiones de pocos casos»). */
  const val MIN_ALONE_FOR_DETAILS = 3
  /** Tiempo máximo creíble por carga, en ms (fuera de eso no se muestra). */
  private const val MAX_MS = 300_000

  /** «Lograste 5 de 8 sin pista»; null sin cargas. */
  fun headline(alone: Int?, total: Int?): String? {
    if (alone == null || total == null || total <= 0) return null
    return "Lograste ${alone.coerceIn(0, total)} de $total sin pista"
  }

  /** La marca para ver su evolución: % de cargas logradas sin pista (más alto = mejor). null sin medida. */
  fun mark(alone: Int?, total: Int?): Float? {
    if (alone == null || total == null || total <= 0) return null
    return 100f * alone.coerceIn(0, total) / total
  }

  /** El desglose en una línea, sin las partes que valen cero: «5 sin pista · 2 con pista · 1 para otra vez». null sin cargas. */
  fun breakdown(alone: Int?, hinted: Int?, total: Int?): String? {
    if (alone == null || total == null || total <= 0) return null
    val a = alone.coerceIn(0, total)
    val h = (hinted ?: 0).coerceIn(0, total - a)
    val left = total - a - h
    val parts = mutableListOf<String>()
    if (a > 0) parts += if (a == 1) "1 sin pista" else "$a sin pista"
    if (h > 0) parts += if (h == 1) "1 con pista" else "$h con pista"
    if (left > 0) parts += if (left == 1) "1 para otra vez" else "$left para otra vez"
    return parts.joinToString(" · ").ifEmpty { null }
  }

  /** «Tardaste 14 s por carga, en promedio, en las que lograste sin pista»; solo con [MIN_ALONE_FOR_DETAILS] o más. */
  fun speedLine(ms: Int?, alone: Int?): String? {
    if (ms == null || ms <= 0 || ms > MAX_MS || alone == null || alone < MIN_ALONE_FOR_DETAILS) return null
    return "Tardaste " + String.format(Locale("es"), "%.0f", ms / 1000f) + " s por carga, en promedio, en las que lograste sin pista"
  }

  /** «Camino corto en 3 de esas 5»: las que lograste con los pasos del camino más corto; solo con [MIN_ALONE_FOR_DETAILS] o más. */
  fun shortLine(short: Int?, alone: Int?): String? {
    if (short == null || short < 0 || alone == null || alone < MIN_ALONE_FOR_DETAILS) return null
    val s = short.coerceIn(0, alone)
    return "Camino corto en $s de esas $alone"
  }

  /**
   * El consejo: cuando alguna carga necesitó pista o quedó para otra vez. Desde el nivel 3 (hay multiplicaciones) el truco es empezar por multiplicar; antes, mirar
   * cuánto falta. null si todas salieron sin pista.
   */
  fun tip(alone: Int?, total: Int?, level: Int): String? {
    if (alone == null || total == null || total <= 0 || alone >= total) return null
    return if (level >= 3) "Truco: mira primero si multiplicar dos celdas te deja cerca de la carga; después ajusta sumando o restando."
    else "Truco: mira cuánto le falta a la celda más grande para llegar a la carga y busca otra celda que lo complete."
  }

  /** Para lectores de pantalla. */
  fun spoken(alone: Int?, total: Int?): String = headline(alone, total) ?: "Sin medida"
}
