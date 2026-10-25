package com.example

import com.example.data.AreaPoints
import com.example.data.GamePoints
import com.example.data.PlaceSheetUi
import com.example.data.PlaceSheets
import com.example.data.Suggestion
import com.example.model.GameRegistry

/** Datos fijos para probar y capturar la lámina de cada lugar: Memoria sumó, Atención se mantiene, Razonamiento recién empieza y Lenguaje aún no se mide (puntos de 0 a 1000, Tarea 159). */
object PlaceTestData {
  /**
   * Los puntos de un área para pruebas y capturas, sin pasar por el registro de avance: [total] puntos, [delta] sumados (o restados) esta semana y, si [measured], un juego medido que aporta lo que cabe de [total].
   * Los demás juegos del área valen 0.
   */
  fun points(area: String, total: Int, delta: Int = 0, measured: Boolean = true, best: Int = total): AreaPoints {
    val ids = AreaPoints.gamesOf(area)
    val max = AreaPoints.SCALE / ids.size
    val per = ids.mapIndexed { i, id ->
      val pts = if (i == 0) total.coerceAtMost(max) else 0
      GamePoints(id, pts, max, pts / max.toFloat(), measured = measured && i == 0)
    }
    return AreaPoints(area, total, per, delta, best)
  }

  val areaPoints = mapOf(
    "MEMORIA" to points("MEMORIA", 220, 40),
    "ATENCION" to points("ATENCION", 310, 0),
    "RAZONAMIENTO" to points("RAZONAMIENTO", 90, 30),
    "LENGUAJE" to points("LENGUAJE", 0, 0, measured = false)
  )

  /** LA sugerencia de Nubi de estos datos (Tarea 150, B2): lo que sigue en la ruta de hoy, un juego del Observatorio; en las demás áreas no hay chip. */
  val suggestion = Suggestion.Pick(GameRegistry.allGames.first { it.domain.name == "MEMORIA" }.id, "MEMORIA", Suggestion.Why.ROUTE)

  fun places(now: Long, suggestion: Suggestion.Pick? = this.suggestion): Map<String, PlaceSheetUi> = areaPoints.mapValues { (area, pts) ->
    PlaceSheets.build(
      area = area, games = GameRegistry.allGames.filter { it.domain.name == area }, titleOf = { it.title },
      history = emptyList(), measures = emptyList(), now = now, suggestion = suggestion
    )
  }
}
