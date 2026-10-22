package com.example.data

import com.example.PlaceTestData
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** La lámina del lugar (Tarea 85) con los datos del área: puntos (Tarea 159), cómo cambió, los juegos y el que elige Nubi. */
class PlaceSheetsTest {
  private val now = 1_800_000_000_000L
  private val day = 86_400_000L
  private fun games(area: String) = GameRegistry.allGames.filter { it.domain.name == area }
  private fun result(id: String, t: Long) = GamePlayResult(gameId = id, score = 70, correctAnswers = 8, totalTrials = 10, timed = false, level = 1, timestamp = t)

  private fun build(area: String, points: AreaPoints, history: List<GamePlayResult> = emptyList(), suggestion: Suggestion.Pick? = null) =
    PlaceSheets.build(area, points, games(area), { it.title }, history, emptyList(), now, suggestion)

  @Test
  fun un_area_medida_dice_su_etapa_sus_puntos_y_como_cambio() {
    val p = build("MEMORIA", PlaceTestData.points("MEMORIA", 220, 40))
    assertEquals("observatorio", p.key)
    assertEquals("Observatorio", p.name)
    assertEquals("el Observatorio", p.article)
    assertEquals("Memoria", p.areaName)
    assertEquals(1, p.stageIndex)
    assertEquals("Aprendiz", p.stageName)
    assertEquals(220, p.points)
    assertEquals(0.22f, p.fill, 1e-6f)
    assertEquals("la lámina dice lo mismo que Avance: etapa y puntos de la escala de 0 a 1000 (Tarea 159)", "Etapa Aprendiz · 220 de 1000 puntos", p.stageLine)
    assertTrue(p.measured)
    assertEquals("el globo de Nubi (Tarea 147): lo que falta y lo que sumó, en puntos", "Te faltan 180 puntos para Hábil. Esta semana sumaste 40 puntos.", p.bubble)
    assertEquals(0.04f, p.change, 1e-6f)
  }

  @Test
  fun un_area_sin_medir_lo_dice_sin_inventar_numeros() {
    val p = build("LENGUAJE", PlaceTestData.points("LENGUAJE", 0, 0, measured = false))
    assertFalse(p.measured)
    assertNull(p.stageName)
    assertNull(p.points)
    assertEquals(0f, p.fill, 0f)
    assertEquals("Aún sin medir", p.stageLine)
    assertEquals("Aún no hay avance aquí: juega uno y lo verás.", p.bubble)
  }

  @Test
  fun el_globo_de_nubi_dice_lo_que_falta_y_lo_que_cambio_en_cinco_casos() {
    fun bubble(total: Int, delta: Int, measured: Boolean = true) = PlaceSheets.bubble(PlaceTestData.points("MEMORIA", total, delta, measured), "Memoria")
    assertEquals("sumó", "Te faltan 130 puntos para Experto. Esta semana sumaste 100 puntos.", bubble(470, 100))
    assertEquals("bajó", "Te faltan 20 puntos para Hábil. Esta semana bajaste 20 puntos.", bubble(380, -20))
    assertEquals("se mantuvo", "Te faltan 130 puntos para Experto. Esta semana te mantuviste.", bubble(470, 0))
    assertEquals("en Maestro: no falta nada", "Llegaste al último tramo. Esta semana te mantuviste.", bubble(910, 0))
    assertEquals("sin medir", "Aún no hay avance aquí: juega uno y lo verás.", bubble(0, 0, measured = false))
    for (t in listOf(bubble(470, 100), bubble(380, -20), bubble(470, 0), bubble(0, 0, measured = false))) {
      assertEquals(t, null, com.example.NoLossGuard.hit(t, com.example.NoLossGuard.literals))
      assertEquals(t, null, com.example.NoLossGuard.hit(t, com.example.NoLossGuard.reminderRoots))
    }
  }

  @Test
  fun un_area_que_recien_empieza_esta_en_Inicio() {
    val p = build("RAZONAMIENTO", PlaceTestData.points("RAZONAMIENTO", 90, 30))
    assertEquals("Inicio", p.stageName)
    assertEquals("Etapa Inicio · 90 de 1000 puntos", p.stageLine)
  }

  @Test
  fun la_lamina_lista_todos_los_juegos_del_area_y_nada_mas() {
    val p = build("ATENCION", PlaceTestData.points("ATENCION", 310, 0))
    assertEquals(games("ATENCION").map { it.id }, p.games.map { it.gameId })
    assertTrue(p.games.size >= 4)
    assertEquals("Etapa Aprendiz · 310 de 1000 puntos", p.stageLine)
  }

  @Test
  fun el_boton_propone_el_juego_que_lleva_mas_tiempo_sin_jugarse_y_el_chip_solo_marca_la_sugerencia_de_nubi() {
    val g = games("MEMORIA")
    // todos jugados menos el tercero: el botón lo propone («aún no la juegas»)
    val history = g.filterIndexed { i, _ -> i != 2 }.mapIndexed { i, d -> result(d.id, now - (i + 1) * day) }
    val st = PlaceTestData.points("MEMORIA", 220, 0)
    val p = build("MEMORIA", st, history)
    assertEquals(g[2].id, p.chosenGameId)
    assertNull("sin sugerencia de Nubi en esta área no hay chip (Tarea 150, B2: Nubi sugiere UNA sola cosa)", p.suggestedGameId)
    assertEquals("Sin jugar aún", p.games[2].detail)
    assertEquals("Jugado ayer", p.games[0].detail)
    // la sugerencia de Nubi es de esta área: ese juego, con su chip
    val q = build("MEMORIA", st, history, Suggestion.Pick(g[0].id, "MEMORIA", Suggestion.Why.ROUTE))
    assertEquals(g[0].id, q.chosenGameId); assertEquals(g[0].id, q.suggestedGameId)
    // la sugerencia es de OTRA área: el botón sigue proponiendo el más olvidado de ésta y no hay chip
    val r = build("MEMORIA", st, history, Suggestion.Pick("radar", "ATENCION", Suggestion.Why.ROUTE))
    assertEquals(g[2].id, r.chosenGameId); assertNull(r.suggestedGameId)
  }

  @Test
  fun si_todos_se_jugaron_elige_el_mas_olvidado_con_los_dias() {
    val g = games("MEMORIA")
    val history = g.mapIndexed { i, d -> result(d.id, now - (if (i == 1) 9 else 1) * day) }
    val p = build("MEMORIA", PlaceTestData.points("MEMORIA", 220, 0), history)
    assertEquals(g[1].id, p.chosenGameId)
    assertEquals("la fila dice el dato corto, sin «que no lo juegas»", "Hace 9 días", p.games[1].detail)
  }

  @Test
  fun la_fila_corta_dice_hoy_ayer_o_hace_n_dias_y_la_leyenda_conserva_la_frase_larga() {
    assertEquals("Jugado hoy", PlaceSheets.playedAgoShort(now, now))
    assertEquals("Jugado ayer", PlaceSheets.playedAgoShort(now, now - day))
    assertEquals("Hace 2 días", PlaceSheets.playedAgoShort(now, now - 2 * day))
    assertEquals("Hace 22 días", PlaceSheets.playedAgoShort(now, now - 22 * day))
    assertEquals("Hace 22 días que no lo juegas", PlaceSheets.playedAgo(now, now - 22 * day))
    val g = games("MEMORIA")
    val history = g.mapIndexed { i, d -> result(d.id, now - (if (i == 0) 22 else 3) * day) }
    val p = build("MEMORIA", PlaceTestData.points("MEMORIA", 220, 0), history)
    assertEquals("Hace 22 días", p.games[0].detail)
    assertEquals(g[0].id, p.chosenGameId)
    assertTrue("ninguna fila repite «que no lo juegas»", p.games.none { it.detail.contains("que no lo juegas") })
  }

  @Test
  fun la_ficha_de_cada_juego_dice_cuando_se_jugo_sin_cifras() {
    // Tarea 143: la lámina muestra una cuadrícula de fichas; cada una dice solo «hoy», «ayer», «hace N días» o «nuevo» (y el lector de pantalla, «jugado …» / «sin jugar aún»)
    assertEquals("hoy", PlaceSheets.whenShort(0)); assertEquals("ayer", PlaceSheets.whenShort(1))
    assertEquals("hace 3 días", PlaceSheets.whenShort(3)); assertEquals("hace 22 días", PlaceSheets.whenShort(22)); assertEquals("nuevo", PlaceSheets.whenShort(null))
    assertEquals("jugado hoy", PlaceSheets.whenSpoken(0)); assertEquals("jugado ayer", PlaceSheets.whenSpoken(1))
    assertEquals("jugado hace 3 días", PlaceSheets.whenSpoken(3)); assertEquals("sin jugar aún", PlaceSheets.whenSpoken(null))
    val g = games("MEMORIA")
    val history = listOf(result(g[0].id, now), result(g[1].id, now - day), result(g[3].id, now - 3 * day))      // el 3.º (g[2]) nunca se jugó
    val p = build("MEMORIA", PlaceTestData.points("MEMORIA", 220, 0), history)
    assertEquals(listOf<Int?>(0, 1, null, 3), p.games.map { it.daysAgo })
    assertEquals(listOf("hoy", "ayer", "nuevo", "hace 3 días"), p.games.map { it.whenShort })
    assertEquals(listOf("jugado hoy", "jugado ayer", "sin jugar aún", "jugado hace 3 días"), p.games.map { it.spoken })
    assertTrue("ninguna ficha lleva una cifra", p.games.none { row -> row.whenShort.any { it.isDigit() && row.daysAgo !in 2..99 } })
  }

  // ---- Las paradas de la ruta de hoy (Tarea 155; desde la Tarea 159 la lámina solo vive en Hoy)

  private fun withRoute(gameIds: List<String>, completed: Int): PlaceSheetUi =
    PlaceSheets.build("MEMORIA", PlaceTestData.points("MEMORIA", 220, 40), games("MEMORIA"), { it.title }, emptyList(), emptyList(), now, PlaceTestData.suggestion, routeStatesOf(gameIds, completed))

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
