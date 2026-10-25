package com.example.data

import com.example.PlaceTestData
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** Las filas de juegos de cada área («Todos los juegos»; antes la lámina del lugar, Tarea 85, que se borró en la Tarea 163): cuándo se jugó cada uno, las paradas de la ruta de hoy y el chip de la sugerencia de Nubi. */
class PlaceSheetsTest {
  private val now = 1_800_000_000_000L
  private val day = 86_400_000L
  private fun games(area: String) = GameRegistry.allGames.filter { it.domain.name == area }
  private fun result(id: String, t: Long) = GamePlayResult(gameId = id, score = 70, correctAnswers = 8, totalTrials = 10, timed = false, level = 1, timestamp = t)

  private fun build(area: String, history: List<GamePlayResult> = emptyList(), suggestion: Suggestion.Pick? = null) =
    PlaceSheets.build(area, games(area), { it.title }, history, emptyList(), now, suggestion)

  @Test
  fun cada_area_lista_todos_sus_juegos_y_su_nombre() {
    val p = build("ATENCION")
    assertEquals("Atención", p.areaName)
    assertEquals("ATENCION", p.area)
    assertEquals(games("ATENCION").map { it.id }, p.games.map { it.gameId })
    assertTrue(p.games.size >= 4)
  }

  @Test
  fun el_chip_solo_marca_la_sugerencia_de_nubi_y_solo_en_su_area() {
    val g = games("MEMORIA")
    val history = g.filterIndexed { i, _ -> i != 2 }.mapIndexed { i, d -> result(d.id, now - (i + 1) * day) }
    val p = build("MEMORIA", history)
    assertNull("sin sugerencia de Nubi en esta área no hay chip (Tarea 150, B2: Nubi sugiere UNA sola cosa)", p.suggestedGameId)
    assertEquals("Sin jugar aún", p.games[2].detail)
    assertEquals("Jugado ayer", p.games[0].detail)
    // la sugerencia de Nubi es de esta área: ese juego, con su chip
    assertEquals(g[0].id, build("MEMORIA", history, Suggestion.Pick(g[0].id, "MEMORIA", Suggestion.Why.ROUTE)).suggestedGameId)
    // la sugerencia es de OTRA área: no hay chip aquí
    assertNull(build("MEMORIA", history, Suggestion.Pick("radar", "ATENCION", Suggestion.Why.ROUTE)).suggestedGameId)
  }

  @Test
  fun la_fila_corta_dice_hoy_ayer_o_hace_n_dias() {
    assertEquals("Jugado hoy", PlaceSheets.playedAgoShort(now, now))
    assertEquals("Jugado ayer", PlaceSheets.playedAgoShort(now, now - day))
    assertEquals("Hace 2 días", PlaceSheets.playedAgoShort(now, now - 2 * day))
    assertEquals("Hace 22 días", PlaceSheets.playedAgoShort(now, now - 22 * day))
    val g = games("MEMORIA")
    val history = g.mapIndexed { i, d -> result(d.id, now - (if (i == 0) 22 else 3) * day) }
    val p = build("MEMORIA", history)
    assertEquals("Hace 22 días", p.games[0].detail)
    assertTrue("ninguna fila repite «que no lo juegas»", p.games.none { it.detail.contains("que no lo juegas") })
  }

  @Test
  fun la_ficha_de_cada_juego_dice_cuando_se_jugo_sin_cifras() {
    // Tarea 143: cada fila dice solo «hoy», «ayer», «hace N días» o «nuevo» (y el lector de pantalla, «jugado …» / «sin jugar aún»)
    assertEquals("hoy", PlaceSheets.whenShort(0)); assertEquals("ayer", PlaceSheets.whenShort(1))
    assertEquals("hace 3 días", PlaceSheets.whenShort(3)); assertEquals("hace 22 días", PlaceSheets.whenShort(22)); assertEquals("nuevo", PlaceSheets.whenShort(null))
    assertEquals("jugado hoy", PlaceSheets.whenSpoken(0)); assertEquals("jugado ayer", PlaceSheets.whenSpoken(1))
    assertEquals("jugado hace 3 días", PlaceSheets.whenSpoken(3)); assertEquals("sin jugar aún", PlaceSheets.whenSpoken(null))
    val g = games("MEMORIA")
    val history = listOf(result(g[0].id, now), result(g[1].id, now - day), result(g[3].id, now - 3 * day))      // el 3.º (g[2]) nunca se jugó
    val p = build("MEMORIA", history)
    assertEquals(listOf<Int?>(0, 1, null, 3), p.games.map { it.daysAgo })
    assertEquals(listOf("hoy", "ayer", "nuevo", "hace 3 días"), p.games.map { it.whenShort })
    assertEquals(listOf("jugado hoy", "jugado ayer", "sin jugar aún", "jugado hace 3 días"), p.games.map { it.spoken })
    assertTrue("ninguna ficha lleva una cifra", p.games.none { row -> row.whenShort.any { it.isDigit() && row.daysAgo !in 2..99 } })
  }

  // ---- Las paradas de la ruta de hoy (Tarea 155)

  private fun withRoute(gameIds: List<String>, completed: Int): PlaceSheetUi =
    PlaceSheets.build("MEMORIA", games("MEMORIA"), { it.title }, emptyList(), emptyList(), now, PlaceTestData.suggestion, routeStatesOf(gameIds, completed))

  @Test
  fun las_filas_de_la_ruta_dicen_hoy_en_tu_ruta_y_hecho_hoy_y_van_primero() {
    val memoria = withRoute(listOf("bodega", "correo", "calculo"), completed = 1)
    assertEquals("la parada pendiente va primero, luego la hecha y después el resto", listOf("correo", "bodega"), memoria.games.take(2).map { it.gameId })
    assertEquals(RouteState.PENDING, memoria.games[0].routeState)
    assertEquals(RouteState.DONE, memoria.games[1].routeState)
    assertEquals("hoy, en tu ruta", memoria.games[0].whenShort)
    assertEquals("hecho hoy", memoria.games[1].whenShort)
    assertTrue("el lector de pantalla lo dice completo", memoria.games[0].spoken.contains("ruta") && memoria.games[1].spoken.contains("hecho hoy"))
  }

  @Test
  fun un_juego_que_no_es_parada_de_hoy_dice_cuando_se_jugo() {
    val rows = withRoute(listOf("bodega", "correo", "calculo"), completed = 1).games
    val other = rows.first { it.routeState == RouteState.NONE }
    assertEquals(PlaceSheets.whenShort(other.daysAgo), other.whenShort)
    assertTrue("las paradas van antes que los demás", rows.indexOfFirst { it.routeState == RouteState.NONE } > rows.indexOfLast { it.routeState != RouteState.NONE })
  }
}
