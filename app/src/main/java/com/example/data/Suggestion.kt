package com.example.data

import com.example.model.GameDefinition
import com.example.model.GameRegistry

/**
 * «Nubi sugiere» (Tarea 150, B2 de la auditoría de coherencia del 20-oct): UNA sola sugerencia para toda la app. Antes eran tres reglas distintas (Juegos: un ÁREA por «la que más puede crecer»; Hoy: la ruta, por otra; la lámina y el detalle de un área: un JUEGO, el más olvidado) con cuatro redacciones.
 * Lógica pura, con pruebas (`SuggestionTest`).
 *
 * - **Mientras la ruta de hoy tenga paradas pendientes**, Nubi sugiere SIEMPRE la que sigue ([Why.ROUTE]), en Juegos (su área), en la lámina y en el detalle de esa área.
 * - **Terminada la ruta** (o sin ruta), el juego que más tiempo lleva sin jugarse: uno que nunca se jugó gana ([Why.NEVER]; entre varios, el primero del catálogo) y, si no, el que lleva más días ([Why.DAYS]; ante empate, el primero del catálogo). En Juegos, su área.
 * - **La razón va en palabras de tiempo** («hace 4 días que no lo juegas», «sigue en tu ruta de hoy»): nunca «es el área que más puede crecer», que significaba «la más baja» y nadie lo entendía.
 * - En el área de una sugerencia, el chip «Nubi sugiere» marca ESE juego; en las demás áreas no hay chip: Nubi sugiere una sola cosa.
 */
object Suggestion {
  enum class Why { ROUTE, NEVER, DAYS }

  /** [area] = el nombre del área (`MEMORIA`…); [days] = los días sin jugarse, solo con [Why.DAYS]. */
  data class Pick(val gameId: String, val area: String, val why: Why, val days: Int = 0)

  /** El rótulo del chip (lámina, ventana de Juegos): una sola redacción. */
  const val CHIP = "Nubi sugiere"

  /** Los juegos que se ofrecen: los del catálogo que la app lanza (sin los retirados), en el orden del catálogo. */
  private fun live(games: List<GameDefinition>): List<GameDefinition> = games.filter { !GameRegistry.isRetired(it.id) }

  /**
   * La sugerencia de hoy. [routeGameIds] y [routeDone] = la ruta de hoy y cuántos van (`DailySessionState`); [lastPlayed] = por juego, la marca de tiempo de su última partida (sin entrada = nunca);
   * [dayOf] = el número de día de una marca de tiempo (en la zona horaria de la persona).
   */
  fun next(
    routeGameIds: List<String>,
    routeDone: Int,
    lastPlayed: Map<String, Long>,
    now: Long,
    dayOf: (Long) -> Long,
    games: List<GameDefinition> = GameRegistry.allGames
  ): Pick? {
    val offered = live(games)
    routeGameIds.getOrNull(routeDone)?.let { id ->
      offered.firstOrNull { it.id == id }?.let { return Pick(it.id, it.domain.name, Why.ROUTE) }
    }
    return longestAway(offered, lastPlayed, now, dayOf)
  }

  /** El juego de [candidates] que más tiempo lleva sin jugarse (el que nunca se jugó gana). null si no hay candidatos. */
  fun longestAway(candidates: List<GameDefinition>, lastPlayed: Map<String, Long>, now: Long, dayOf: (Long) -> Long): Pick? {
    val offered = live(candidates)
    offered.firstOrNull { it.id !in lastPlayed }?.let { return Pick(it.id, it.domain.name, Why.NEVER) }
    val today = dayOf(now)
    val away = offered.associateWith { today - dayOf(lastPlayed.getValue(it.id)) }
    val g = offered.maxByOrNull { away.getValue(it) } ?: return null          // ante empate, el primero del catálogo
    return Pick(g.id, g.domain.name, Why.DAYS, away.getValue(g).toInt().coerceAtLeast(0))
  }

  /** ¿Es LA sugerencia de Nubi de esta [area]? (lleva el chip «Nubi sugiere» en «Todos los juegos»). */
  fun suggestedHere(area: String, global: Pick?): Boolean = global != null && global.area == area

  /** La razón de un juego sugerido, en palabras de tiempo: «sigue en tu ruta de hoy», «aún no lo has jugado», «hace 4 días que no lo juegas». */
  fun reason(p: Pick): String = when (p.why) {
    Why.ROUTE -> "sigue en tu ruta de hoy"
    Why.NEVER -> "aún no lo has jugado"
    Why.DAYS -> when {
      p.days <= 0 -> "para variar un poco"
      p.days == 1 -> "hace 1 día que no lo juegas"
      else -> "hace ${p.days} días que no lo juegas"
    }
  }

  /**
   * La razón de la sugerencia cuando se habla del ÁREA (el globo de Juegos: «Te sugiero Atención: …»): nombra el juego, porque la sugerencia es ese juego y su área solo dice dónde está.
   * «sigue Tinta o Palabra en tu ruta de hoy», «aún no has jugado Constelaciones», «hace 4 días que no juegas Constelaciones».
   */
  fun reasonForArea(p: Pick, gameTitle: String): String = when (p.why) {
    Why.ROUTE -> "sigue $gameTitle en tu ruta de hoy"
    Why.NEVER -> "aún no has jugado $gameTitle"
    Why.DAYS -> when {
      p.days <= 0 -> "para variar un poco"
      p.days == 1 -> "hace 1 día que no juegas $gameTitle"
      else -> "hace ${p.days} días que no juegas $gameTitle"
    }
  }
}
