package com.example.data

import java.util.Locale
import kotlin.math.roundToInt

/**
 * Las medidas propias de los juegos estrella dichas en frases completas, con su unidad y sin abreviaturas (Tarea 159; Ricardo, 22-oct: «sin palabras sueltas»; Opus: segundos y no milisegundos, «grados por segundo» escrito completo). Las medidas internas
 * (milisegundos, grados) NO cambian: solo lo que se ve. [value] es la frase de una marca («5 luces seguidas», «0,16 segundos de destello»); [short] repite la unidad más corto para «Tu mejor marca: …» cuando ya se dijo de qué se trata («7 luces»).
 * Freno de Emergencia se dice SIEMPRE en zonas (Tarea 57: nunca en milisegundos) y Tinta o Palabra en palabras («casi nada», «0,4 segundos»), como en sus finales.
 */
object MeasurePhrases {
  /** Segundos con coma decimal fija y hasta 2 decimales sin ceros de sobra: 160 ms → «0,16», 200 → «0,2», 1000 → «1». */
  fun seconds(ms: Float): String {
    val cs = (ms.roundToInt() + 5) / 10                                           // centésimas, en enteros: 855 ms → 86, sin errores de flotante
    val t = "${cs / 100}." + "%02d".format(Locale.ROOT, cs % 100)
    return t.trimEnd('0').trimEnd('.').replace('.', ',')
  }

  /** «1 segundo» / «0,16 segundos». */
  fun secondsWord(ms: Float): String = seconds(ms).let { n -> if (n == "1") "1 segundo" else "$n segundos" }

  private fun num(def: MeasureDef, v: Float): String =
    if (def.decimals == 0) v.roundToInt().toString() else String.format(Locale.ROOT, "%.${def.decimals}f", v).replace('.', ',')

  private fun plural(n: String, one: String, many: String): String = if (n == "1") "1 $one" else "$n $many"

  /** La marca [v] de [def] con su unidad, en una frase corta que se entiende sola: «5 luces seguidas», «0,16 segundos de destello», «70% de las veces, directo a una pareja ya vista». */
  fun value(def: MeasureDef, v: Float): String = when (def.key) {
    "glance" -> "${secondsWord(v)} de destello"
    "brake" -> "${secondsWord(v)} para frenar"
    "tracking" -> plural(num(def, v), "satélite a la vez", "satélites a la vez")
    "numline" -> "a ${num(def, v)}% del blanco"
    "rotation" -> plural(num(def, v), "grado por segundo", "grados por segundo")
    "mandos" -> "${num(def, v)}% de las señales de tu misión, a los mandos"
    "vocab" -> "${num(def, v)}% de las palabras menos comunes, reconocidas"
    "wpm" -> plural(num(def, v), "palabra por minuto", "palabras por minuto")
    "atlas" -> "${num(def, v)}% de aciertos"
    "punta" -> "${num(def, v)}% de las palabras, sin ayuda"
    "carga" -> "${num(def, v)}% de las cargas, sin pista"
    "taller" -> "${num(def, v)}% de las máquinas, arregladas"
    "bodega" -> "${num(def, v)}% de los objetos, al primer intento"
    "place" -> "${num(def, v)}% de las veces, directo a una pareja ya vista"
    "trail" -> plural(num(def, v), "luz seguida", "luces seguidas")
    "stroop" -> if (v < DosOrillas.NEGLIGIBLE_MS) "la palabra casi no te frena" else "la palabra te frena ${secondsWord(v)}"
    "estacion" -> "${num(def, v)}% de los encargos, cumplidos"
    else -> measureText(def, v)
  }

  /** Lo mismo más corto, para repetir la unidad tras haberla dicho una vez: «7 luces», «0,16 segundos», «80% de las veces». */
  fun short(def: MeasureDef, v: Float): String = when (def.key) {
    "glance", "brake" -> secondsWord(v)
    "tracking" -> value(def, v)
    "numline" -> value(def, v)
    "rotation" -> value(def, v)
    "mandos" -> "${num(def, v)}% de las señales"
    "vocab" -> "${num(def, v)}% de las palabras"
    "wpm" -> value(def, v)
    "atlas" -> value(def, v)
    "punta" -> "${num(def, v)}% de las palabras"
    "carga" -> "${num(def, v)}% de las cargas"
    "taller" -> "${num(def, v)}% de las máquinas"
    "bodega" -> "${num(def, v)}% de los objetos"
    "place" -> "${num(def, v)}% de las veces"
    "trail" -> plural(num(def, v), "luz", "luces")
    "stroop" -> if (v < DosOrillas.NEGLIGIBLE_MS) "casi nada" else secondsWord(v)
    "estacion" -> "${num(def, v)}% de los encargos"
    else -> measureText(def, v)
  }

  /** El rótulo de la mejor marca en «Tus mejores marcas»: lo que se mejoró, en femenino o masculino como pida el nombre. */
  fun recordLabel(def: MeasureDef): String = when (def.key) {
    "glance" -> "Tu vistazo más rápido"
    "brake" -> "Tu freno más ágil"
    "tracking" -> "Tu seguimiento más amplio"
    "numline" -> "Tu aterrizaje más cerca del blanco"
    "rotation" -> "Tu giro mental más rápido"
    "mandos" -> "Tus mejores señales a los mandos"
    "vocab" -> "Tu mejor vocabulario"
    "wpm" -> "Tu lectura más rápida"
    "atlas" -> "Tu mejor red de significados"
    "punta" -> "Tus mejores palabras por tu cuenta"
    "carga" -> "Tus mejores cargas sin pista"
    "taller" -> "Tus mejores máquinas arregladas"
    "bodega" -> "Tus mejores objetos al primer intento"
    "place" -> "Tu mejor memoria de lugar"
    "trail" -> "Tu rastro más largo"
    "stroop" -> "Lo que menos te frenó la palabra"
    "estacion" -> "Tu mejor memoria para lo pendiente"
    else -> def.title
  }

  /** «hoy», «ayer», «hace 6 días», «hace 2 semanas», «hace 3 meses»: cuándo pasó, en palabras. */
  fun ago(days: Int): String = when {
    days <= 0 -> "hoy"
    days == 1 -> "ayer"
    days < 14 -> "hace $days días"
    days < 60 -> "hace ${days / 7} semanas"
    else -> "hace ${days / 30} meses"
  }

  /**
   * La frase de la medida de un juego para su tarjeta en la página del área (Fable §4: «Hoy: 5 luces seguidas · Tu mejor marca: 7 luces»; Opus, C: «En tu última partida: …» cuando la última no fue hoy). [last] = el valor de la última partida comparable; [best] = la mejor marca de las comparables
   * (null con menos de [StarMeasures.MIN_POINTS] partidas: no se dice nada de «mejor» con tan pocas); [today] = la última partida fue hoy. Si la última ES la mejor, «Es tu mejor marca.» en vez de repetirla.
   */
  fun sentence(def: MeasureDef, last: Float, best: Float?, today: Boolean): String {
    val lead = if (today) "Hoy" else "En tu última partida"
    val first = "$lead: ${value(def, last)}."
    if (best == null) return first
    val tied = if (def.lowerIsBetter) best >= last else best <= last
    return if (tied) "$first Es tu mejor marca." else "$first Tu mejor marca: ${short(def, best)}."
  }
}
