package com.example.data

import java.util.Locale
import kotlin.math.abs
import kotlin.math.roundToInt

/**
 * La medida propia de un juego estrella en una partida ([key] = [MeasureDef.key]). [rating] = rating del juego al
 * terminar (0..1; -1 = partida anterior al 28-sep, sin dato) y [timed] = con reloj (null = sin dato): la evolución
 * compara solo partidas parecidas ([StarMeasures.comparable]). [mode] = el modo en que se jugó (Tarea 159, Ricardo, 22-oct: comparar con otras personas solo en Desafío, `Features.COMPARE`): hoy NO se usa para nada que se vea;
 * se guarda para que el día que haya servidor no falten datos. null = partida anterior a esta tarea, sin dato.
 */
data class MeasurePoint(val timestamp: Long, val key: String, val value: Float, val rating: Float = -1f, val timed: Boolean? = null, val mode: PlayMode? = null)

/**
 * Cómo se muestra una medida: "Tu vistazo en Rescate relámpago" · 84 ms. [suffix] va pegado al número ("18%") y [unit] después
 * ("de casa"). [lowerIsBetter]: menos es mejor (ms, distancia); en el gráfico "mejor" va siempre hacia arriba.
 */
data class MeasureDef(
  val key: String,
  val gameId: String,
  val title: String,
  val suffix: String,
  val unit: String,
  val lowerIsBetter: Boolean,
  val decimals: Int = 0,
  /** Nombre corto para las invitaciones: "tu vistazo". */
  val short: String = "",
  /** La medida depende del nivel jugado (tipo de regla, tramos, paradas...): se compara solo a nivel parecido. */
  val levelDependent: Boolean = false
) {
  fun format(v: Float): String {
    val n = if (decimals == 0) v.roundToInt().toString() else String.format(Locale.ROOT, "%.${decimals}f", v).replace('.', ',')
    return n + suffix
  }
}

enum class DiscoveryKind { RECORD, IMPROVING, STEADY }

/** Una de «Tus mejores marcas» de Avance (Tarea 159): la mejor marca de [def] ([value], en su unidad interna) y cuándo se logró ([timestamp]). */
data class BestMark(val def: MeasureDef, val value: Float, val timestamp: Long)

/** El "descubrimiento del día" de Hoy: una medida con sus últimas partidas (hasta [MAX_POINTS]). */
data class Discovery(val def: MeasureDef, val values: List<Float>, val kind: DiscoveryKind) {
  val caption: String
    get() = when (kind) {
      DiscoveryKind.RECORD -> "Tu mejor marca hasta ahora."
      DiscoveryKind.IMPROVING -> "Vas mejor que en tus primeras partidas de esta serie."
      DiscoveryKind.STEADY -> "Se mantiene parecida: la constancia también cuenta."
    }
}

/**
 * Medidas propias de los juegos estrella guardadas por partida (28-sep, pedido de Ricardo: lo más valioso para la
 * persona es la información del final de cada juego; ahora se guarda para ver su evolución). Lógica pura con pruebas.
 * Regla: no se dice nada con menos de [MIN_POINTS] partidas de un juego.
 */
object StarMeasures {
  const val MIN_POINTS = 3
  const val MAX_POINTS = 6
  private const val DAY_MS = 86_400_000L

  val defs = listOf(
    MeasureDef("glance", "radar", "Tu vistazo en Rescate relámpago", "", "ms", lowerIsBetter = true, short = "tu vistazo"),
    MeasureDef("brake", "freno", "Tu freno en Freno de Emergencia", "", "ms", lowerIsBetter = true, short = "tu freno"),
    MeasureDef("tracking", "satelites", "Tu seguimiento en Satélites", "", "a la vez", lowerIsBetter = false, decimals = 1, short = "tu seguimiento", levelDependent = true),
    MeasureDef("numline", "aterrizaje", "Tu estimación en Aterrizaje Lunar", "%", "del blanco", lowerIsBetter = true, decimals = 1, short = "tu estimación", levelDependent = true),
    MeasureDef("rotation", "acoplamiento", "Tu giro mental en Acoplamiento", "°", "por segundo", lowerIsBetter = false, short = "tu giro mental", levelDependent = true),
    // Clave `mandos` (no `multitask`): el juego se rehízo el 9-oct y se borró el «costo de multitarea» (regla permanente 1 de Ricardo: nada se mide con una tarea sola); los puntos viejos quedan guardados pero ya no se leen.
    MeasureDef("mandos", "piloto", "Tus señales a los mandos en Piloto Estelar", "%", "de las señales de tu misión, a los mandos", lowerIsBetter = false, short = "tus señales a los mandos", levelDependent = true),
    MeasureDef("vocab", "meteoros", "Tu vocabulario en Lluvia de meteoros", "%", "reconocido (palabras menos comunes)", lowerIsBetter = false, short = "tu vocabulario", levelDependent = true),
    MeasureDef("wpm", "disparate", "Tu lectura en ¿Verdad o disparate?", "", "palabras por minuto", lowerIsBetter = false, short = "tu lectura", levelDependent = true),
    MeasureDef("atlas", "intrusa", "Tu red de significados en La estrella intrusa", "%", "de aciertos", lowerIsBetter = false, short = "tu red de significados", levelDependent = true),
    MeasureDef("punta", "anagramas", "Tus palabras por tu cuenta en En la punta de la lengua", "%", "de las palabras, sin ayuda", lowerIsBetter = false, short = "las palabras que encuentras por tu cuenta", levelDependent = true),
    MeasureDef("carga", "calculo", "Tus cargas sin pista en Carga exacta", "%", "de las cargas, sin pista", lowerIsBetter = false, short = "las cargas que logras sin pista", levelDependent = true),
    // Clave `taller` (no `engranajes`): el juego se rehízo el 5-oct y su medida mide otra cosa; los puntos viejos (clave `engranajes`) quedan guardados pero ya no se leen.
    MeasureDef("taller", "engranajes", "Tus máquinas arregladas en Engranajes", "%", "de las máquinas, arregladas", lowerIsBetter = false, short = "las máquinas que arreglas", levelDependent = true),
    MeasureDef("bodega", "bodega", "Tus objetos al primer intento en Bodega de carga", "%", "de los objetos, al primer intento", lowerIsBetter = false, short = "los objetos que encuentras al primer intento", levelDependent = true),
    MeasureDef("place", "parejas", "Tu memoria de lugar en Constelaciones", "%", "de las veces, directo a una pareja ya vista", lowerIsBetter = false, short = "tu memoria de lugar", levelDependent = true),
    MeasureDef("trail", "secuencia", "Tu rastro en Rastro de luz", "", "luces seguidas", lowerIsBetter = false, short = "tu rastro", levelDependent = true),
    // Clave `estacion` (no `pending`): el juego se rehízo el 8-oct («La estación de correo») y su medida cuenta otra cosa (encargos por evento, por hora y cancelados); los puntos viejos (clave `pending`) quedan guardados pero ya no se leen.
    // Tarea 77 (Etapa 3, finales con sentido): «Cuánto te frenó la palabra» de Tinta o Palabra, que el juego ya calculaba pero no guardaba, para poder mostrar «Tu avance» (hoy, tu promedio y tu mejor). Menos es mejor.
    MeasureDef("stroop", "stroop", "Cuánto te frenó la palabra en Tinta o Palabra", "", "ms", lowerIsBetter = true, short = "cuánto te frena la palabra", levelDependent = true),
    MeasureDef("estacion", "correo", "Tu memoria para lo pendiente en Correo Estelar", "%", "de los encargos, cumplidos", lowerIsBetter = false, short = "tu memoria para lo pendiente", levelDependent = true)
  )

  val gameNames = mapOf(
    "radar" to "Rescate relámpago", "freno" to "Freno de Emergencia", "satelites" to "Satélites", "aterrizaje" to "Aterrizaje Lunar",
    "acoplamiento" to "Acoplamiento", "piloto" to "Piloto Estelar",
    "correo" to "Correo Estelar", "meteoros" to "Lluvia de meteoros", "disparate" to "¿Verdad o disparate?", "intrusa" to "La estrella intrusa", "secuencia" to "Rastro de luz", "anagramas" to "En la punta de la lengua", "calculo" to "Carga exacta", "engranajes" to "Engranajes", "bodega" to "Bodega de carga", "parejas" to "Constelaciones", "stroop" to "Tinta o Palabra"
  )

  fun def(key: String) = defs.firstOrNull { it.key == key }

  /**
   * «Tu freno» no se muestra en milisegundos sino en zonas y como promedio de las últimas partidas (Tarea 57, ver [Brake]): para esa medida, el texto de la zona del promedio («zona firme»); para las demás, null (se muestran con su unidad).
   * También null si todavía no hay ninguna estimación válida de freno.
   */
  fun zoneText(def: MeasureDef, points: List<MeasurePoint>): String? {
    // Tinta o Palabra (Tarea 77): también en palabras, como en su final («casi nada», «+0,4 s»), no en milisegundos
    if (def.key == "stroop") return points.filter { it.key == "stroop" }.maxByOrNull { it.timestamp }?.let { DosOrillas.cost(it.value.roundToInt()) }
    if (def.key != Brake.KEY) return null
    return Brake.reading(points)?.let { "zona ${it.zone.label}" }
  }
  fun defForGame(gameId: String) = defs.firstOrNull { it.gameId == gameId }

  private fun better(def: MeasureDef, a: Float, b: Float) = if (def.lowerIsBetter) a < b else a > b

  /**
   * El descubrimiento del día: primero un récord reciente (la última partida mejor que todas las anteriores, en los
   * últimos 7 días); si no, la medida que más mejoró (últimas partidas contra las primeras de la serie, 5% o más);
   * si no, la última medida jugada, "se mantiene". null = ningún juego estrella tiene todavía [MIN_POINTS] partidas.
   */
  fun discover(points: List<MeasurePoint>, now: Long): Discovery? {
    val series = points.groupBy { it.key }
      .mapNotNull { (k, list) -> def(k)?.let { it to comparable(list) } }
      .filter { it.second.size >= MIN_POINTS }
    if (series.isEmpty()) return null
    fun recent(list: List<MeasurePoint>) = list.takeLast(MAX_POINTS).map { it.value }

    val records = series.filter { (d, list) ->
      val last = list.last()
      now - last.timestamp <= 7 * DAY_MS && list.dropLast(1).all { better(d, last.value, it.value) }
    }
    records.maxByOrNull { it.second.last().timestamp }?.let { (d, list) -> return Discovery(d, recent(list), DiscoveryKind.RECORD) }

    val gains = series.mapNotNull { (d, list) ->
      val v = recent(list)
      val half = v.size / 2
      val first = v.take(half).average().toFloat()
      val last = v.takeLast(half).average().toFloat()
      val base = abs(first).coerceAtLeast(1e-3f)
      val gain = (if (d.lowerIsBetter) first - last else last - first) / base
      if (gain >= 0.05f) Triple(d, v, gain) else null
    }
    gains.maxByOrNull { it.third }?.let { return Discovery(it.first, it.second, DiscoveryKind.IMPROVING) }

    val (d, list) = series.maxByOrNull { it.second.last().timestamp }!!
    return Discovery(d, recent(list), DiscoveryKind.STEADY)
  }

  /**
   * Las partidas de UNA medida que se pueden comparar con la última, en orden: mismo reloj (si se sabe) y, si la
   * medida depende del nivel, rating a menos de un nivel de la escalera del juego. Las partidas viejas sin dato se
   * comparan solo mientras la última tampoco lo tenga (docs/dificultad-y-avance.md, sección 5).
   */
  fun comparable(points: List<MeasurePoint>): List<MeasurePoint> {
    val sorted = points.sortedBy { it.timestamp }
    val last = sorted.lastOrNull() ?: return sorted
    val d = def(last.key)
    val band = 1f / Skill.ladder(d?.gameId ?: "").levels + 1e-4f
    return sorted.filter { p ->
      val sameClock = last.timed == null || p.timed == null || p.timed == last.timed
      val sameLevel = d?.levelDependent != true ||
        (if (last.rating < 0f) p.rating < 0f else p.rating >= 0f && abs(p.rating - last.rating) <= band)
      sameClock && sameLevel
    }
  }

  private fun dayIndex(ts: Long): Long = (ts + java.util.TimeZone.getDefault().getOffset(ts)) / DAY_MS

  /** La mejor de [series] (de [MeasureDef.lowerIsBetter]: la menor; si no, la mayor). Si varias empatan, la más reciente. */
  private fun bestOf(def: MeasureDef, series: List<MeasurePoint>): MeasurePoint? =
    series.sortedBy { it.timestamp }.let { s -> if (def.lowerIsBetter) s.minWithOrNull(compareBy<MeasurePoint> { it.value }.thenByDescending { it.timestamp }) else s.maxWithOrNull(compareBy<MeasurePoint> { it.value }.thenBy { it.timestamp }) }

  /**
   * «Tus mejores marcas» (Tarea 159; `auditoria-fable/avance-b-puntos.md` §3.5): hasta [max] de los juegos estrella con su mejor marca y su fecha, las logradas más recientemente primero. Solo cuentan partidas comparables con la última ([comparable]: mismo reloj; nivel parecido si la medida
   * depende del nivel) y un juego con al menos [MIN_POINTS] de ellas: con menos no se dice nada. «Tu freno» no entra (siempre en zonas, nunca en milisegundos: [Brake]).
   */
  fun bestMarks(points: List<MeasurePoint>, max: Int = 3): List<BestMark> = defs.filter { it.key != Brake.KEY }.mapNotNull { d ->
    val series = comparable(points.filter { it.key == d.key })
    if (series.size < MIN_POINTS) null else bestOf(d, series)?.let { BestMark(d, it.value, it.timestamp) }
  }.sortedByDescending { it.timestamp }.take(max)

  /**
   * La frase de la medida de un juego estrella para su tarjeta en la página de un área («Hoy: 5 luces seguidas. Tu mejor marca: 7 luces.»; ver [MeasurePhrases.sentence]); null si el juego no tiene medida o todavía no hay una partida con ella. Freno de Emergencia dice su zona, nunca milisegundos.
   */
  fun sentence(gameId: String, points: List<MeasurePoint>, now: Long): String? {
    val d = defForGame(gameId) ?: return null
    if (d.key == Brake.KEY) return Brake.reading(points)?.headline?.let { "$it." }
    val series = comparable(points.filter { it.key == d.key })
    val last = series.lastOrNull() ?: return null
    val best = if (series.size >= MIN_POINTS) bestOf(d, series)?.value else null
    return MeasurePhrases.sentence(d, last.value, best, today = dayIndex(last.timestamp) == dayIndex(now))
  }

  /**
   * La última partida con medida de un juego estrella para la fila «Tu última partida, hoy: …» de su tarjeta (Tarea 162): [timestamp] = cuándo fue y [body] = la frase de la medida SIN su comienzo ([MeasurePhrases.body]).
   * [standalone] = la frase es una oración aparte (Freno: «Tu freno, promedio de tus últimas 4 partidas: zona firme.»; el rótulo termina en punto y no en dos puntos).
   */
  data class LastMeasure(val timestamp: Long, val body: String, val standalone: Boolean = false)

  /** Ver [LastMeasure]; null si el juego no tiene medida o todavía no hay ninguna comparable. [withBest] = false no dice «Tu mejor marca: …» (ver [LevelLines.overlapsMeasure]). */
  fun lastMeasure(gameId: String, points: List<MeasurePoint>, withBest: Boolean = true): LastMeasure? {
    val d = defForGame(gameId) ?: return null
    if (d.key == Brake.KEY) {
      val at = points.filter { it.key == d.key }.maxByOrNull { it.timestamp }?.timestamp ?: return null
      return Brake.reading(points)?.headline?.let { LastMeasure(at, "$it.", standalone = true) }
    }
    val series = comparable(points.filter { it.key == d.key })
    val last = series.lastOrNull() ?: return null
    val best = if (series.size >= MIN_POINTS) bestOf(d, series)?.value else null
    return LastMeasure(last.timestamp, MeasurePhrases.body(d, last.value, best, withBest))
  }

  /** Última medida de un juego (para la ventana de cada zona). */
  fun latest(points: List<MeasurePoint>, gameId: String): Pair<MeasureDef, Float>? {
    val d = defForGame(gameId) ?: return null
    val p = points.filter { it.key == d.key }.maxByOrNull { it.timestamp } ?: return null
    return d to p.value
  }

  /** «marca|medida|valor» y, si hay datos del juego, «|rating|reloj» y, desde la Tarea 159, «|modo» (las líneas viejas sin modo se leen igual, con modo nulo). */
  fun encode(points: List<MeasurePoint>): String = points.joinToString("\n") { p ->
    "${p.timestamp}|${p.key}|${p.value}" +
      if (p.rating >= 0f || p.timed != null || p.mode != null) "|${p.rating}|${p.timed?.let { if (it) 1 else 0 } ?: -1}" + (p.mode?.let { "|${it.name}" } ?: "") else ""
  }

  fun decode(text: String?): List<MeasurePoint> = text.orEmpty().lines().mapNotNull { line ->
    val parts = line.split('|')
    if (parts.size != 3 && parts.size != 5 && parts.size != 6) return@mapNotNull null
    val ts = parts[0].toLongOrNull() ?: return@mapNotNull null
    val v = parts[2].toFloatOrNull() ?: return@mapNotNull null
    if (def(parts[1]) == null || v.isNaN()) return@mapNotNull null
    val rating = if (parts.size >= 5) parts[3].toFloatOrNull() ?: -1f else -1f
    val timed = if (parts.size >= 5) when (parts[4]) { "1" -> true; "0" -> false; else -> null } else null
    val mode = if (parts.size == 6) PlayMode.entries.firstOrNull { it.name == parts[5] } else null
    MeasurePoint(ts, parts[1], v, rating, timed, mode)
  }
}

/** La marca con su unidad, en una frase y sin abreviaturas (Tarea 159: antes «84 ms» y «96° por segundo»): «0,08 segundos de destello», «a 18% del blanco», «96 grados por segundo»; ver [MeasurePhrases.value]. */
fun measureText(def: MeasureDef, v: Float): String = MeasurePhrases.value(def, v)
