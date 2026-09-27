package com.example.data

import android.content.Context
import java.util.Locale
import java.util.TimeZone

/**
 * Una palabra del diccionario nuri de la persona (Primer Contacto): cuándo la descifró, cuándo la vio por última vez
 * (días locales desde 1970) y cuántos repasos lleva bien.
 */
data class NuriWord(val id: Int, val learnedDay: Long, val lastSeenDay: Long, val reviews: Int = 0)

/** Lo que Unity necesita al empezar: el diccionario y qué palabras de otros días repasar (con los días que pasaron). */
data class ContactLaunch(val known: List<Int>, val review: List<Int>, val reviewDays: List<Int>)

/**
 * Lógica pura de "Primer Contacto" del lado de la app (Unity: `Games/Contacto/ContactContract.cs`):
 * - El DICCIONARIO de cada persona crece día a día: las palabras descifradas entran; al empezar cada partida se repasan
 *   hasta 4 de otros días (las que hace más que no se ven); la que se olvida sale y vuelve a aprenderse. Es recuerdo con
 *   demora (días, no minutos) y práctica espaciada.
 * - Las lecturas del final: escenas por palabra, descarte de lo que ya tiene nombre y el repaso. Con pocos datos no se
 *   dice nada (misma regla que el resto de las medidas de los juegos estrella).
 */
object Contact {
  const val WORD_COUNT = 36
  const val MAX_REVIEW = 4
  /** Lo mínimo para descifrar: oír la palabra 3 veces (la primera es adivinar; después, dos aciertos seguidos). */
  const val MIN_HEARINGS = 3
  /** Escenas con palabra nueva y algo ya nombrado al lado: desde cuántas se habla del descarte. */
  const val MIN_EXCLUSION_CHANCES = 5

  /** Mismas palabras y significados que `ContactContract.Words` / `Meanings` (mismo número = misma palabra). */
  val WORDS = listOf(
    "ZOBA", "KITU", "FEDI", "NUPA", "LIRO", "GAMU", "TEBI", "SUKE", "MOVA", "DAKO", "PIFO", "NELI",
    "RA", "KI", "LU", "ZO", "PE",
    "KORU", "BIMA", "FULE", "ZENO", "TAMI", "REKU", "LOPA", "GISE", "BUKO", "MEZU", "PAVI", "NOKI", "SIRU", "DEFO",
    "ZUMI", "FOGI",
    "TUN", "NAS", "BEL"
  )
  val MEANINGS = listOf(
    "planeta", "cohete", "cometa", "estrella", "luna", "ovni", "satélite", "sol", "casco", "asteroide", "telescopio", "cristal",
    "coral", "amarillo", "celeste", "lila", "verde",
    "llave", "campana", "pluma", "concha", "reloj de arena", "brújula", "farol", "corona", "bellota", "libro", "copa", "gema",
    "hongo", "ancla", "estrella de mar", "paraguas",
    "uno", "dos", "tres"
  )

  /** Día local (días desde 1970 en la zona horaria del teléfono): cambia a medianoche, no a las 0 h UTC. */
  fun today(nowMs: Long = System.currentTimeMillis()): Long = (nowMs + TimeZone.getDefault().getOffset(nowMs)) / 86_400_000L

  /** Qué repasar hoy: palabras que no se vieron hoy, las que hace más que no se ven primero; hasta [MAX_REVIEW]. */
  fun reviewPick(dict: Map<Int, NuriWord>, today: Long): List<NuriWord> =
    dict.values.filter { it.lastSeenDay < today }
      .sortedWith(compareBy<NuriWord> { it.lastSeenDay }.thenBy { it.id })
      .take(MAX_REVIEW)

  fun launch(dict: Map<Int, NuriWord>, today: Long): ContactLaunch {
    val review = reviewPick(dict, today)
    return ContactLaunch(
      known = dict.keys.sorted(),
      review = review.map { it.id },
      reviewDays = review.map { (today - it.lastSeenDay).toInt().coerceAtLeast(1) }
    )
  }

  /**
   * El diccionario después de una partida: lo repasado bien se renueva (visto hoy), lo olvidado sale (vuelve a la
   * lección), y lo descifrado entra (visto hoy).
   */
  fun apply(
    dict: Map<Int, NuriWord>,
    today: Long,
    decoded: List<Int>,
    reviewed: List<Int>,
    reviewOk: List<Boolean>
  ): Map<Int, NuriWord> {
    val out = dict.toMutableMap()
    reviewed.forEachIndexed { i, id ->
      val w = out[id] ?: return@forEachIndexed
      if (reviewOk.getOrNull(i) == true) out[id] = w.copy(lastSeenDay = today, reviews = w.reviews + 1)
      else out.remove(id)
    }
    for (id in decoded) {
      if (id !in 0 until WORD_COUNT) continue
      out[id] = NuriWord(id, learnedDay = today, lastSeenDay = today)
    }
    return out
  }

  fun label(id: Int): String = WORDS.getOrElse(id) { "?" }
  fun meaning(id: Int): String = MEANINGS.getOrElse(id) { "?" }

  /** "KITU = cohete · RA = coral" (lo descifrado en la partida). */
  fun decodedLine(decoded: List<Int>): String = decoded.joinToString(" · ") { "${label(it)} = ${meaning(it)}" }

  private fun oneDecimal(x: Float): String = String.format(Locale.forLanguageTag("es-CL"), "%.1f", x)

  /** Cómo leer las escenas por palabra, con un truco si cuesta. */
  fun hearingsMessage(mean: Float): String {
    val base = "Veces que oíste cada palabra hasta descifrarla: ${oneDecimal(mean)}. Lo mínimo son $MIN_HEARINGS: la primera vez siempre es adivinar."
    return if (mean > 4.5f) "$base Truco: cuando suene una palabra, busca qué cosa estaba también la vez anterior." else base
  }

  /** Descarte (exclusividad mutua): solo con [MIN_EXCLUSION_CHANCES] escenas o más. */
  fun exclusionMessage(ok: Int, total: Int): String? {
    if (total < MIN_EXCLUSION_CHANCES) return null
    val pct = 100 * ok / total
    val head = "Descartaste lo que ya tenía nombre en $ok de $total escenas con una palabra nueva."
    return if (pct >= 70) "$head Es lo que hacen los niños al aprender a hablar: una palabra nueva suele nombrar algo que aún no tiene nombre."
    else "$head Truco: si ya sabes cómo se llama una cosa, la palabra nueva casi seguro es de otra."
  }

  /** El repaso de palabras de otros días. */
  fun reviewMessage(ok: Int, total: Int, maxDays: Int?): String {
    val days = maxDays?.takeIf { it >= 1 }?.let { if (it == 1) " (de ayer)" else " (la más antigua, de hace $it días)" } ?: ""
    return if (ok == total) "Repaso: recordaste las $total palabras de otros días$days."
    else "Repaso: recordaste $ok de $total palabras de otros días$days. Las que se escaparon vuelven a tu lista: se afirman al volver a encontrarlas."
  }
}

/** El diccionario nuri guardado en el teléfono (SharedPreferences `nuri_dictionary`: solo se agrega y se renueva). */
class ContactDictionaryStore(context: Context) {
  private val prefs = context.getSharedPreferences("nuri_dictionary", Context.MODE_PRIVATE)

  fun load(): Map<Int, NuriWord> {
    val out = HashMap<Int, NuriWord>()
    for ((key, value) in prefs.all) {
      val id = key.removePrefix("w").toIntOrNull() ?: continue
      val parts = (value as? String)?.split(",") ?: continue
      if (parts.size < 3) continue
      out[id] = NuriWord(id, parts[0].toLongOrNull() ?: continue, parts[1].toLongOrNull() ?: continue, parts[2].toIntOrNull() ?: 0)
    }
    return out
  }

  fun save(dict: Map<Int, NuriWord>) {
    val e = prefs.edit().clear()
    for (w in dict.values) e.putString("w${w.id}", "${w.learnedDay},${w.lastSeenDay},${w.reviews}")
    e.apply()
  }

  fun clear() = prefs.edit().clear().apply()
}
