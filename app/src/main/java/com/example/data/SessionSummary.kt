package com.example.data

import com.example.model.GamePlayResult
import com.example.model.GameRegistry

/** Un juego de la sesión de hoy con su puntaje (0-100; null si no quedó registrado). */
data class SessionGame(val gameId: String, val area: String, val score: Int?)

/**
 * Resumen de la sesión del día (29-sep, pedido de Ricardo: "al finalizar debería aparecer una lámina que señale qué se
 * trabajó, qué puntaje y cuánto avance"). [areas] = las áreas trabajadas, con su avance desde que empezó la sesión
 * (lógica de [AreaProgress]: solo cuenta lo que se movió en los juegos ya medidos).
 */
data class SessionSummary(val games: List<SessionGame>, val areas: List<AreaStatus>) {
  companion object {
    /** [gameIds] = los 3 de la sesión; [history] = todas las partidas; [progress] = avance de hoy por juego medido. */
    fun build(
      gameIds: List<String>,
      history: List<GamePlayResult>,
      progress: Map<String, Float>,
      points: List<ProgressPoint>,
      now: Long
    ): SessionSummary {
      val dayAgo = now - 86_400_000L
      val latest = gameIds.associateWith { id -> history.filter { it.gameId == id && it.timestamp >= dayAgo }.maxByOrNull { it.timestamp } }
      val games = gameIds.mapNotNull { id ->
        GameRegistry.getById(id)?.let { SessionGame(id, it.domain.name, latest[id]?.score) }
      }
      val start = latest.values.filterNotNull().minOfOrNull { it.timestamp } ?: now
      val areas = games.map { it.area }.distinct().map { area ->
        val ids = GameRegistry.allGames.filter { it.domain.name == area }.map { it.id }
        AreaProgress.status(area, ids, progress, points, now, since = start - 1)
      }
      return SessionSummary(games, areas)
    }
  }
}
