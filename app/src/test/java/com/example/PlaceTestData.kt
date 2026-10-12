package com.example

import com.example.data.AreaStatus
import com.example.data.PlaceSheetUi
import com.example.data.PlaceSheets
import com.example.model.GameRegistry

/** Datos fijos para probar y capturar la lámina de cada lugar: Memoria avanzó, Atención se mantiene, Razonamiento recién empieza y Lenguaje aún no se mide. */
object PlaceTestData {
  val statuses = mapOf(
    "MEMORIA" to AreaStatus("MEMORIA", 0.22f, 0.04f, listOf(0.12f, 0.15f, 0.18f, 0.22f)),
    "ATENCION" to AreaStatus("ATENCION", 0.31f, 0f, listOf(0.26f, 0.29f, 0.31f, 0.31f)),
    "RAZONAMIENTO" to AreaStatus("RAZONAMIENTO", 0.09f, 0.03f, listOf(null, 0.03f, 0.06f, 0.09f)),
    "LENGUAJE" to AreaStatus("LENGUAJE", null, 0f, listOf(null, null, null, null))
  )

  fun places(now: Long): Map<String, PlaceSheetUi> = statuses.mapValues { (area, status) ->
    PlaceSheets.build(
      area = area, status = status, games = GameRegistry.allGames.filter { it.domain.name == area }, titleOf = { it.title },
      history = emptyList(), measures = emptyList(), now = now
    )
  }
}
