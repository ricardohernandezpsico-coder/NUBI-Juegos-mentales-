package com.example.data

import com.example.model.GamePlayResult
import com.example.model.GameRegistry

/**
 * Un juego de la ruta de hoy en el resumen (Tarea 159, Opus: «dos "puntos" distintos en la misma pantalla es lo que Ricardo no quiere»): [played] = se jugó en esta ruta y [gained] = los PUNTOS DE AVANCE que sumó hoy en su área (0 si no sumó);
 * ya no lleva el puntaje de la partida, que era otra escala.
 */
data class SessionGame(val gameId: String, val area: String, val played: Boolean, val gained: Int)

/**
 * Resumen de la sesión del día (29-sep, pedido de Ricardo: "al finalizar debería aparecer una lámina que señale qué se
 * trabajó y cuánto avance"). [games] = los juegos de la ruta con los puntos que sumó cada uno hoy; [areas] = las áreas trabajadas, con sus puntos ([AreaPoints], Tarea 159) y el cambio desde que empezó la sesión.
 */
data class SessionSummary(val games: List<SessionGame>, val areas: List<AreaPoints>) {
  companion object {
    /** [gameIds] = los 3 de la sesión; [history] = todas las partidas; [points] = el registro del avance de cada juego (`progress_log`). */
    fun build(
      gameIds: List<String>,
      history: List<GamePlayResult>,
      points: List<ProgressPoint>,
      now: Long
    ): SessionSummary {
      val dayAgo = now - 86_400_000L
      val latest = gameIds.associateWith { id -> history.filter { it.gameId == id && it.timestamp >= dayAgo }.maxByOrNull { it.timestamp } }
      val start = latest.values.filterNotNull().minOfOrNull { it.timestamp } ?: now
      val areaKeys = gameIds.mapNotNull { GameRegistry.getById(it)?.domain?.name }.distinct()
      val after = areaKeys.associateWith { AreaPoints.compute(it, AreaPoints.gamesOf(it), points, now, weekStart = start - 1) }
      val before = areaKeys.associateWith { AreaPoints.compute(it, AreaPoints.gamesOf(it), points, start - 1, weekStart = start - 1) }
      fun gameAt(m: Map<String, AreaPoints>, area: String, id: String) = m[area]?.perGame?.firstOrNull { it.gameId == id }?.points ?: 0
      val games = gameIds.mapNotNull { id ->
        GameRegistry.getById(id)?.let { g ->
          val area = g.domain.name
          SessionGame(id, area, played = latest[id] != null, gained = (gameAt(after, area, id) - gameAt(before, area, id)).coerceAtLeast(0))
        }
      }
      return SessionSummary(games, areaKeys.map { after.getValue(it) })
    }
  }
}
