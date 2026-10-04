package com.example.data

import java.util.Locale

/**
 * «En la punta de la lengua» (juego de Lenguaje que reemplaza a Anagramas, id `anagramas`; ver docs/diseno-punta-de-la-lengua.md): la lectura de lo que
 * manda Unity y las palabras azules que vuelven. Lógica pura con pruebas.
 *
 * Cada palabra de la partida termina con un lucero: dorado (la encontraste sola), plateado (con 1 o 2 ayudas), cobre (con las letras justas) o azul (te la
 * mostró Nubi). La medida es «cuántas encontraste por tu cuenta»; los luceros azules se GUARDAN (son progreso: van en el respaldo) y la app los manda a
 * Unity para que la palabra vuelva en otra partida.
 */
enum class PuntaTier(val code: Char, val label: String) {
  SOLO('o', "sola"),
  PISTA('p', "con pista"),
  LETRAS('c', "con las letras"),
  VISTA('a', "te la mostró Nubi");

  companion object {
    fun fromCode(code: Char): PuntaTier? = entries.firstOrNull { it.code == code }
  }
}

/** Una palabra de la partida con el lucero que ganó. */
data class PuntaEntry(val word: String, val tier: PuntaTier)

object Punta {
  /** Palabras azules que esperan volver, como máximo (las más viejas se sueltan primero). */
  const val MAX_PENDING = 20
  /** Palabras encontradas solas para mostrar el tiempo («no sacar conclusiones de pocos casos»). */
  const val MIN_ALONE_FOR_SPEED = 3
  /** Tiempo mínimo y máximo creíble hasta «¡La tengo!», en ms (fuera de eso no se muestra). */
  private const val MAX_SPEED_MS = 120_000

  // ------------------------------------------------------------------ lo que manda Unity

  /** Palabras separadas por «;» («búho;faro»): sin vacías ni repetidas, en orden. */
  fun words(csv: String?): List<String> = csv.orEmpty().split(";").map { it.trim() }.filter { it.isNotEmpty() }.distinct()

  /** «reloj:o;búho:a» → cada palabra con su lucero, en el orden jugado. Lo que no se entiende se salta. */
  fun entries(csv: String?): List<PuntaEntry> = csv.orEmpty().split(";").mapNotNull { part ->
    val cut = part.lastIndexOf(':')
    if (cut <= 0 || cut != part.length - 2) return@mapNotNull null
    val tier = PuntaTier.fromCode(part[cut + 1]) ?: return@mapNotNull null
    val word = part.substring(0, cut).trim()
    if (word.isEmpty()) null else PuntaEntry(word, tier)
  }

  // ------------------------------------------------------------------ las azules que vuelven

  fun encode(words: List<String>): String = words.joinToString(";")

  /**
   * Las que esperan volver después de una partida: las de antes más las que Nubi mostró hoy, menos las que hoy se encontraron solas o con 1-2 ayudas. Sin
   * repetidas (una mostrada otra vez sube al final), y como máximo [MAX_PENDING]: si hay más, se sueltan las más viejas.
   */
  fun mergePending(pending: List<String>, blue: List<String>, cleared: List<String>): List<String> {
    val out = ArrayList<String>()
    for (w in pending + blue) {
      out.remove(w)
      out.add(w)
    }
    out.removeAll(cleared.toSet())
    return if (out.size > MAX_PENDING) out.takeLast(MAX_PENDING) else out
  }

  // ------------------------------------------------------------------ lo que se lee

  fun count(entries: List<PuntaEntry>, tier: PuntaTier): Int = entries.count { it.tier == tier }

  /** Palabras jugadas: las cuatro cuentas juntas (cada palabra termina en un solo lucero). */
  fun total(solo: Int?, pista: Int?, letras: Int?, vista: Int?): Int = (solo ?: 0) + (pista ?: 0) + (letras ?: 0) + (vista ?: 0)

  /** «Encontraste 5 de 8 por tu cuenta»; null sin palabras. */
  fun headline(solo: Int?, total: Int?): String? {
    if (solo == null || total == null || total <= 0) return null
    return "Encontraste ${solo.coerceIn(0, total)} de $total por tu cuenta"
  }

  /** La marca para ver su evolución: % de palabras encontradas solas (más alto = mejor). null sin medida. */
  fun mark(solo: Int?, total: Int?): Float? {
    if (solo == null || total == null || total <= 0) return null
    return 100f * solo.coerceIn(0, total) / total
  }

  /** «Con ayuda» = pista o las letras (la ayuda que se pidió, sin las que Nubi mostró). */
  fun helped(pista: Int?, letras: Int?): Int = (pista ?: 0) + (letras ?: 0)

  /** El desglose en una línea, sin las partes que valen cero: «5 solas · 2 con ayuda · 1 mostrada». null sin palabras. */
  fun breakdown(solo: Int?, pista: Int?, letras: Int?, vista: Int?): String? {
    val parts = mutableListOf<String>()
    val s = solo ?: 0
    val h = helped(pista, letras)
    val v = vista ?: 0
    if (s + h + v <= 0) return null
    if (s > 0) parts += if (s == 1) "1 sola" else "$s solas"
    if (h > 0) parts += if (h == 1) "1 con ayuda" else "$h con ayuda"
    if (v > 0) parts += if (v == 1) "1 mostrada" else "$v mostradas"
    return parts.joinToString(" · ")
  }

  /** «Tardaste 4,2 s en decir ¡La tengo! en las que salieron solas»; solo con [MIN_ALONE_FOR_SPEED] solas o más. */
  fun speedLine(ms: Int?, solo: Int?): String? {
    if (ms == null || ms <= 0 || ms > MAX_SPEED_MS || solo == null || solo < MIN_ALONE_FOR_SPEED) return null
    return "Tardaste " + String.format(Locale("es"), "%.1f", ms / 1000f).replace('.', ',') + " s en decir «¡La tengo!» en las que salieron solas"
  }

  /** Si alguna necesitó ayuda o la mostró Nubi: normalizar que pasa, con el truco de la escalera. null si todas salieron solas. */
  fun message(solo: Int?, total: Int?): String? {
    if (solo == null || total == null || total <= 0 || solo >= total) return null
    return "Que algunas palabras se escondan es normal: pasa a todo el mundo. Truco: si una no sale, piensa en cómo empieza o en otra parecida: suele destrabarla."
  }

  /** Nota para quien ve las azules: vuelven. null sin azules. */
  fun blueNote(vista: Int?): String? = if ((vista ?: 0) > 0) "Las azules vuelven en otra partida." else null

  /** Para lectores de pantalla. */
  fun spoken(solo: Int?, total: Int?): String = headline(solo, total) ?: "Sin medida"
}
