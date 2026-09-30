package com.example.data

import kotlin.math.ln
import kotlin.math.min

/** Una partida para el planeta de Hoy: su dominio ([com.example.model.DomainType.name]) y cuándo se jugó. */
data class PlanetPlay(val domain: String, val timestamp: Long)

/**
 * Una zona del planeta (una por dominio). [growth] 0..1 = cuánto creció (nunca baja: jugar menos no la achica, solo
 * la deja quieta); [structures] = cuántas construcciones tiene (cristales, faros, árboles...).
 */
data class PlanetZone(
  val domain: String,
  val plays: Int,
  val growth: Float,
  val structures: Int,
  val lastPlayed: Long?,
  val playedToday: Boolean,
  val weekPlays: Int
)

/**
 * El planeta de Hoy: las 4 zonas, la que más creció esta semana, y la que está quieta ([quietDays] días sin jugar, o
 * -1 = aún sin explorar), para la línea bajo el planeta.
 */
data class PlanetState(
  val zones: List<PlanetZone>,
  val grewThisWeek: String?,
  val quiet: String?,
  val quietDays: Int,
  val playedToday: List<String>
)

/**
 * "Tu planeta" (pestaña Hoy, 28-sep, elegido por Ricardo): cada partida hace crecer la zona de su dominio. Lógica pura
 * con pruebas; [dayOf] da el día local de un instante (inyectado para probar sin zona horaria).
 */
object Planet {
  val DOMAINS = listOf("MEMORIA", "ATENCION", "RAZONAMIENTO", "LENGUAJE")

  /** Partidas con las que una zona queda "llena" (después sigue sumando construcciones, no tamaño). */
  const val FULL_AT = 60

  /** Días sin jugar un dominio para nombrarlo "quieto". */
  const val QUIET_DAYS = 4

  /** Crecimiento 0..1: rápido al principio (se nota desde la primera partida) y cada vez más lento. */
  fun growth(plays: Int): Float = if (plays <= 0) 0f else min(1f, (ln(1.0 + plays) / ln(1.0 + FULL_AT)).toFloat())

  fun structures(plays: Int): Int = when {
    plays <= 0 -> 0
    plays < 3 -> 1
    plays < 10 -> 2
    plays < 25 -> 3
    plays < 50 -> 4
    else -> 5
  }

  fun build(plays: List<PlanetPlay>, now: Long, dayOf: (Long) -> Long): PlanetState {
    val today = dayOf(now)
    val zones = DOMAINS.map { d ->
      val mine = plays.filter { it.domain == d }
      val last = mine.maxOfOrNull { it.timestamp }
      PlanetZone(
        domain = d,
        plays = mine.size,
        growth = growth(mine.size),
        structures = structures(mine.size),
        lastPlayed = last,
        playedToday = last != null && dayOf(last) == today,
        weekPlays = mine.count { today - dayOf(it.timestamp) in 0..6 }
      )
    }
    val grew = zones.filter { it.weekPlays > 0 }
      .maxWithOrNull(compareBy<PlanetZone>({ it.weekPlays }, { it.lastPlayed ?: 0L }))?.domain
    // Quieta: primero una zona aún sin explorar (la invitación más concreta); si no, la que lleva más días sin jugar.
    val unexplored = zones.firstOrNull { it.plays == 0 }
    val (quiet, quietDays) = if (unexplored != null && zones.any { it.plays > 0 }) {
      unexplored.domain to -1
    } else {
      val oldest = zones.filter { it.lastPlayed != null }.minByOrNull { it.lastPlayed!! }
      val days = oldest?.let { (today - dayOf(it.lastPlayed!!)).toInt() } ?: 0
      if (oldest != null && days >= QUIET_DAYS) oldest.domain to days else null to 0
    }
    val playedToday = plays.filter { dayOf(it.timestamp) == today }.sortedBy { it.timestamp }.map { it.domain }.distinct()
    return PlanetState(zones, grew, quiet, quietDays, playedToday)
  }

  /** Partidas por semana de un dominio en las últimas [weeks] semanas (la última = los 7 días hasta hoy). */
  fun weeklyCounts(timestamps: List<Long>, now: Long, dayOf: (Long) -> Long, weeks: Int = 4): List<Int> {
    val today = dayOf(now)
    return (weeks - 1 downTo 0).map { w ->
      timestamps.count { val ago = today - dayOf(it); ago in (w * 7L)..(w * 7L + 6) }
    }
  }
}
