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
 * Cómo se muestra una medida: "Tu vistazo en Radar" · 84 ms. [suffix] va pegado al número ("18%") y [unit] después
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
    MeasureDef("glance", "radar", "Tu vistazo en Radar", "", "ms", lowerIsBetter = true, short = "tu vistazo", compactPattern = "{v} ms"),
    MeasureDef("brake", "freno", "Tu freno en Freno de Emergencia", "", "ms", lowerIsBetter = true, short = "tu freno", compactPattern = "{v} ms"),
    MeasureDef("tracking", "satelites", "Tu seguimiento en Satélites", "", "a la vez", lowerIsBetter = false, decimals = 1, short = "tu seguimiento", compactPattern = "{v} a la vez", levelDependent = true),
    MeasureDef("numline", "aterrizaje", "Tu estimación en Aterrizaje Lunar", "%", "del blanco", lowerIsBetter = true, decimals = 1, short = "tu estimación", compactPattern = "a {v}", levelDependent = true),
    MeasureDef("rotation", "acoplamiento", "Tu giro mental en Acoplamiento", "°", "por segundo", lowerIsBetter = false, short = "tu giro mental", compactPattern = "{v}/s", levelDependent = true),
    MeasureDef("load", "trafico", "Tu carga en Tráfico Estelar", "", "cápsulas a la vez", lowerIsBetter = false, short = "tu carga", compactPattern = "{v} a la vez"),
    MeasureDef("multitask", "piloto", "Tu multitarea en Piloto Estelar", "%", "de costo", lowerIsBetter = true, short = "tu multitarea", compactPattern = "{v} costo"),
    MeasureDef("homing", "rumbo", "Tu brújula en Rumbo a Casa", "%", "de casa", lowerIsBetter = true, short = "tu brújula", compactPattern = "a {v}", levelDependent = true),
    MeasureDef("recall", "bitacora", "Tu memoria en la Bitácora", "%", "recordado", lowerIsBetter = false, short = "tu memoria", compactPattern = "{v}", levelDependent = true),
    MeasureDef("vocab", "meteoros", "Tu vocabulario en Lluvia de meteoros", "%", "reconocido (palabras menos comunes)", lowerIsBetter = false, short = "tu vocabulario", compactPattern = "{v}", levelDependent = true),
    MeasureDef("wpm", "disparate", "Tu lectura en ¿Verdad o disparate?", "", "palabras por minuto", lowerIsBetter = false, short = "tu lectura", compactPattern = "{v} ppm", levelDependent = true),
    MeasureDef("harvest", "cosecha", "Tu cosecha en Cosecha de palabras", "%", "de las comunes", lowerIsBetter = false, short = "tu cosecha", compactPattern = "{v}", levelDependent = true),
    MeasureDef("atlas", "intrusa", "Tu red de significados en La estrella intrusa", "%", "de aciertos", lowerIsBetter = false, short = "tu red de significados", compactPattern = "{v}", levelDependent = true),
    MeasureDef("trail", "secuencia", "Tu rastro en Rastro de luz", "", "luces seguidas", lowerIsBetter = false, short = "tu rastro", compactPattern = "{v} luces", levelDependent = true),
    MeasureDef("pending", "correo", "Tu memoria para lo pendiente", "%", "de encargos", lowerIsBetter = false, short = "tu memoria para lo pendiente", compactPattern = "{v}", levelDependent = true)
  )

  val gameNames = mapOf(
    "radar" to "Radar", "freno" to "Freno de Emergencia", "satelites" to "Satélites", "aterrizaje" to "Aterrizaje Lunar",
    "acoplamiento" to "Acoplamiento", "trafico" to "Tráfico Estelar", "piloto" to "Piloto Estelar", "rumbo" to "Rumbo a Casa",
    "bitacora" to "Bitácora de Misión", "correo" to "Correo Estelar", "meteoros" to "Lluvia de meteoros", "disparate" to "¿Verdad o disparate?", "cosecha" to "Cosecha de palabras", "intrusa" to "La estrella intrusa", "secuencia" to "Rastro de luz"
  )

  fun def(key: String) = defs.firstOrNull { it.key == key }
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

  /** Sin descubrimiento: el juego estrella al que le faltan menos partidas (o Radar si todavía no hay ninguna). */
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
