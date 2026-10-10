package com.example.data

import java.util.Locale
import kotlin.math.abs
import kotlin.math.roundToInt

/**
 * La medida propia de un juego estrella en una partida ([key] = [MeasureDef.key]). [rating] = rating del juego al
 * terminar (0..1; -1 = partida anterior al 28-sep, sin dato) y [timed] = con reloj (null = sin dato): la evolución
 * compara solo partidas parecidas ([StarMeasures.comparable]).
 */
data class MeasurePoint(val timestamp: Long, val key: String, val value: Float, val rating: Float = -1f, val timed: Boolean? = null)

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
  /** Forma corta con unidad, para debajo de un planeta: "{v} ms", "a {v}". */
  val compactPattern: String = "{v}",
  /** La medida depende del nivel jugado (tipo de regla, tramos, paradas...): se compara solo a nivel parecido. */
  val levelDependent: Boolean = false
) {
  /** La medida en pocas letras y con su unidad (la biblioteca de juegos): "212 ms", "5 a la vez", "a 18%". */
  fun compact(v: Float): String = compactPattern.replace("{v}", format(v))

  fun format(v: Float): String {
    val n = if (decimals == 0) v.roundToInt().toString() else String.format(Locale.ROOT, "%.${decimals}f", v).replace('.', ',')
    return n + suffix
  }
}

enum class DiscoveryKind { RECORD, IMPROVING, STEADY }

/** El "descubrimiento del día" de Hoy: una medida con sus últimas partidas (hasta [MAX_POINTS]). */
data class Discovery(val def: MeasureDef, val values: List<Float>, val kind: DiscoveryKind) {
  val caption: String
    get() = when (kind) {
      DiscoveryKind.RECORD -> "Tu mejor marca hasta ahora."
      DiscoveryKind.IMPROVING -> "Vas mejor que en tus primeras partidas de esta serie."
      DiscoveryKind.STEADY -> "Se mantiene parecida: la constancia también cuenta."
    }
}

/** Qué falta para tener un descubrimiento: jugar [def] [remaining] veces más. */
data class DiscoveryNudge(val def: MeasureDef, val remaining: Int) {
  val text: String
    get() = if (remaining >= StarMeasures.MIN_POINTS) "Juega ${gameName(def.gameId)} para descubrir ${def.short}."
    else "Juega ${gameName(def.gameId)} ${if (remaining == 1) "1 vez" else "$remaining veces"} más y verás cómo evoluciona ${def.short}."
}

private fun gameName(id: String) = StarMeasures.gameNames[id] ?: id

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
    MeasureDef("glance", "radar", "Tu vistazo en Rescate relámpago", "", "ms", lowerIsBetter = true, short = "tu vistazo", compactPattern = "{v} ms"),
    MeasureDef("brake", "freno", "Tu freno en Freno de Emergencia", "", "ms", lowerIsBetter = true, short = "tu freno", compactPattern = "{v} ms"),
    MeasureDef("tracking", "satelites", "Tu seguimiento en Satélites", "", "a la vez", lowerIsBetter = false, decimals = 1, short = "tu seguimiento", compactPattern = "{v} a la vez", levelDependent = true),
    MeasureDef("numline", "aterrizaje", "Tu estimación en Aterrizaje Lunar", "%", "del blanco", lowerIsBetter = true, decimals = 1, short = "tu estimación", compactPattern = "a {v}", levelDependent = true),
    MeasureDef("rotation", "acoplamiento", "Tu giro mental en Acoplamiento", "°", "por segundo", lowerIsBetter = false, short = "tu giro mental", compactPattern = "{v}/s", levelDependent = true),
    // Clave `mandos` (no `multitask`): el juego se rehízo el 9-oct y se borró el «costo de multitarea» (regla permanente 1 de Ricardo: nada se mide con una tarea sola); los puntos viejos quedan guardados pero ya no se leen.
    MeasureDef("mandos", "piloto", "Tus señales a los mandos en Piloto Estelar", "%", "de las señales de tu misión, a los mandos", lowerIsBetter = false, short = "tus señales a los mandos", compactPattern = "{v}", levelDependent = true),
    MeasureDef("vocab", "meteoros", "Tu vocabulario en Lluvia de meteoros", "%", "reconocido (palabras menos comunes)", lowerIsBetter = false, short = "tu vocabulario", compactPattern = "{v}", levelDependent = true),
    MeasureDef("wpm", "disparate", "Tu lectura en ¿Verdad o disparate?", "", "palabras por minuto", lowerIsBetter = false, short = "tu lectura", compactPattern = "{v} ppm", levelDependent = true),
    MeasureDef("harvest", "cosecha", "Tu cosecha en Cosecha de palabras", "%", "de las comunes", lowerIsBetter = false, short = "tu cosecha", compactPattern = "{v}", levelDependent = true),
    MeasureDef("atlas", "intrusa", "Tu red de significados en La estrella intrusa", "%", "de aciertos", lowerIsBetter = false, short = "tu red de significados", compactPattern = "{v}", levelDependent = true),
    MeasureDef("punta", "anagramas", "Tus palabras por tu cuenta en En la punta de la lengua", "%", "de las palabras, sin ayuda", lowerIsBetter = false, short = "las palabras que encuentras por tu cuenta", compactPattern = "{v}", levelDependent = true),
    MeasureDef("carga", "calculo", "Tus cargas sin pista en Carga exacta", "%", "de las cargas, sin pista", lowerIsBetter = false, short = "las cargas que logras sin pista", compactPattern = "{v}", levelDependent = true),
    // Clave `taller` (no `engranajes`): el juego se rehízo el 5-oct y su medida mide otra cosa; los puntos viejos (clave `engranajes`) quedan guardados pero ya no se leen.
    MeasureDef("taller", "engranajes", "Tus máquinas arregladas en Engranajes", "%", "de las máquinas, arregladas", lowerIsBetter = false, short = "las máquinas que arreglas", compactPattern = "{v}", levelDependent = true),
    MeasureDef("bodega", "bodega", "Tus objetos al primer intento en Bodega de carga", "%", "de los objetos, al primer intento", lowerIsBetter = false, short = "los objetos que encuentras al primer intento", compactPattern = "{v}", levelDependent = true),
    MeasureDef("place", "parejas", "Tu memoria de lugar en Constelaciones", "%", "de las veces, directo a una pareja ya vista", lowerIsBetter = false, short = "tu memoria de lugar", compactPattern = "{v}", levelDependent = true),
    MeasureDef("trail", "secuencia", "Tu rastro en Rastro de luz", "", "luces seguidas", lowerIsBetter = false, short = "tu rastro", compactPattern = "{v} luces", levelDependent = true),
    // Clave `estacion` (no `pending`): el juego se rehízo el 8-oct («La estación de correo») y su medida cuenta otra cosa (encargos por evento, por hora y cancelados); los puntos viejos (clave `pending`) quedan guardados pero ya no se leen.
    MeasureDef("estacion", "correo", "Tu memoria para lo pendiente en Correo Estelar", "%", "de los encargos, cumplidos", lowerIsBetter = false, short = "tu memoria para lo pendiente", compactPattern = "{v}", levelDependent = true)
  )

  val gameNames = mapOf(
    "radar" to "Rescate relámpago", "freno" to "Freno de Emergencia", "satelites" to "Satélites", "aterrizaje" to "Aterrizaje Lunar",
    "acoplamiento" to "Acoplamiento", "piloto" to "Piloto Estelar",
    "correo" to "Correo Estelar", "meteoros" to "Lluvia de meteoros", "disparate" to "¿Verdad o disparate?", "cosecha" to "Cosecha de palabras", "intrusa" to "La estrella intrusa", "secuencia" to "Rastro de luz", "anagramas" to "En la punta de la lengua", "calculo" to "Carga exacta", "engranajes" to "Engranajes", "bodega" to "Bodega de carga", "parejas" to "Constelaciones"
  )

  fun def(key: String) = defs.firstOrNull { it.key == key }

  /**
   * «Tu freno» no se muestra en milisegundos sino en zonas y como promedio de las últimas partidas (Tarea 57, ver [Brake]): para esa medida, el texto de la zona del promedio («zona firme»); para las demás, null (se muestran con su unidad).
   * También null si todavía no hay ninguna estimación válida de freno.
   */
  fun zoneText(def: MeasureDef, points: List<MeasurePoint>): String? {
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

  /** Sin descubrimiento: el juego estrella al que le faltan menos partidas (o Rescate relámpago si todavía no hay ninguna). */
  fun nudge(points: List<MeasurePoint>): DiscoveryNudge {
    val counts = points.groupingBy { it.key }.eachCount()
    val best = counts.filter { it.value < MIN_POINTS && def(it.key) != null }.maxByOrNull { it.value }
    return if (best != null) DiscoveryNudge(def(best.key)!!, MIN_POINTS - best.value) else DiscoveryNudge(defs.first(), MIN_POINTS)
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

  /** Última medida de un juego (para la ventana de cada zona). */
  fun latest(points: List<MeasurePoint>, gameId: String): Pair<MeasureDef, Float>? {
    val d = defForGame(gameId) ?: return null
    val p = points.filter { it.key == d.key }.maxByOrNull { it.timestamp } ?: return null
    return d to p.value
  }

  fun encode(points: List<MeasurePoint>): String = points.joinToString("\n") { p ->
    "${p.timestamp}|${p.key}|${p.value}" + if (p.rating >= 0f || p.timed != null) "|${p.rating}|${p.timed?.let { if (it) 1 else 0 } ?: -1}" else ""
  }

  fun decode(text: String?): List<MeasurePoint> = text.orEmpty().lines().mapNotNull { line ->
    val parts = line.split('|')
    if (parts.size != 3 && parts.size != 5) return@mapNotNull null
    val ts = parts[0].toLongOrNull() ?: return@mapNotNull null
    val v = parts[2].toFloatOrNull() ?: return@mapNotNull null
    if (def(parts[1]) == null || v.isNaN()) return@mapNotNull null
    val rating = if (parts.size == 5) parts[3].toFloatOrNull() ?: -1f else -1f
    val timed = if (parts.size == 5) when (parts[4]) { "1" -> true; "0" -> false; else -> null } else null
    MeasurePoint(ts, parts[1], v, rating, timed)
  }
}
