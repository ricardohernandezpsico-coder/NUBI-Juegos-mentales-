package com.example.data

import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** La lámina del lugar (Tarea 85) con los datos del área: avance, cómo cambió, las 4 semanas, los juegos y el que elige Nubi. */
class PlaceSheetsTest {
  private val now = 1_800_000_000_000L
  private val day = 86_400_000L
  private fun games(area: String) = GameRegistry.allGames.filter { it.domain.name == area }
  private fun result(id: String, t: Long) = GamePlayResult(gameId = id, score = 70, correctAnswers = 8, totalTrials = 10, timed = false, level = 1, timestamp = t)

  private fun build(area: String, status: AreaStatus, history: List<GamePlayResult> = emptyList()) =
    PlaceSheets.build(area, status, games(area), { it.title }, history, emptyList(), now)

  @Test
  fun un_area_medida_dice_su_etapa_sus_puntos_y_como_cambio() {
    val p = build("MEMORIA", AreaStatus("MEMORIA", 0.22f, 0.04f, listOf(0.12f, 0.15f, 0.18f, 0.22f)))
    assertEquals("observatorio", p.key)
    assertEquals("Observatorio", p.name)
    assertEquals("el Observatorio", p.article)
    assertEquals("Memoria", p.areaName)
    assertEquals(1, p.stageIndex)
    assertEquals("Aprendiz", p.stageName)
    assertEquals(22, p.points)
    assertEquals(0.22f, p.fill, 1e-6f)
    assertEquals("Esta semana avanzó de 18 a 22", p.changeLine)
    assertEquals("Te faltan 18 para Hábil", p.toNextLine)
    assertEquals(listOf<Int?>(12, 15, 18, 22), p.weeks)
    assertTrue(p.measured)
    assertEquals("el globo de Nubi (Tarea 147): primera persona, lo que avanzaste y lo que te falta", "Esta semana avanzaste de 18 a 22. Te faltan 18 para Hábil.", p.bubble)
    assertEquals(0.04f, p.change, 1e-6f)
  }

  @Test
  fun un_area_sin_medir_lo_dice_sin_inventar_numeros() {
    val p = build("LENGUAJE", AreaStatus("LENGUAJE", null, 0f, listOf(null, null, null, null)))
    assertFalse(p.measured)
    assertNull(p.stageName)
    assertNull(p.points)
    assertNull(p.toNextLine)
    assertEquals(0f, p.fill, 0f)
    assertEquals(listOf<Int?>(null, null, null, null), p.weeks)
    assertEquals("Aún sin medir: tu primera partida lo muestra", p.changeLine)
    assertEquals("Aún no hay avance aquí: juega uno y lo verás.", p.bubble)
  }

  @Test
  fun el_globo_de_nubi_dice_lo_que_cambio_y_lo_que_falta_en_cinco_casos() {
    fun bubble(value: Float?, change: Float) = PlaceSheets.bubble(AreaStatus("MEMORIA", value, change, listOf(null, null, null, value)))
    assertEquals("avanzó", "Esta semana avanzaste de 37 a 47. Te faltan 13 para Experto.", bubble(0.47f, 0.10f))
    assertEquals("bajó", "Esta semana bajaste un poco, de 40 a 38: es normal que varíe. Te faltan 2 para Hábil.", bubble(0.38f, -0.02f))
    assertEquals("se mantuvo", "Esta semana te mantuviste en 47. Te faltan 13 para Experto.", bubble(0.47f, 0f))
    assertEquals("en Maestro: la primera frase sola", "Esta semana te mantuviste en 91.", bubble(0.91f, 0f))
    assertEquals("sin medir", "Aún no hay avance aquí: juega uno y lo verás.", bubble(null, 0f))
    for (t in listOf(bubble(0.47f, 0.10f), bubble(0.38f, -0.02f), bubble(0.47f, 0f), bubble(null, 0f))) {
      assertEquals(t, null, com.example.NoLossGuard.hit(t, com.example.NoLossGuard.literals))
      assertEquals(t, null, com.example.NoLossGuard.hit(t, com.example.NoLossGuard.reminderRoots))
    }
  }

  @Test
  fun las_semanas_sin_dato_quedan_en_null() {
    val p = build("RAZONAMIENTO", AreaStatus("RAZONAMIENTO", 0.09f, 0.03f, listOf(null, 0.03f, 0.06f, 0.09f)))
    assertEquals(listOf<Int?>(null, 3, 6, 9), p.weeks)
    assertEquals("Inicio", p.stageName)
  }

  @Test
  fun la_lamina_lista_todos_los_juegos_del_area_y_nada_mas() {
    val p = build("ATENCION", AreaStatus("ATENCION", 0.31f, 0f, listOf(0.26f, 0.29f, 0.31f, 0.31f)))
    assertEquals(games("ATENCION").map { it.id }, p.games.map { it.gameId })
    assertTrue(p.games.size >= 4)
    assertEquals("Esta semana se mantuvo en 31", p.changeLine)
  }

  @Test
  fun nubi_elige_el_juego_que_lleva_mas_tiempo_sin_jugarse_y_dice_por_que() {
    val g = games("MEMORIA")
    // todos jugados menos el tercero: lo elige Nubi («aún no la juegas»)
    val history = g.filterIndexed { i, _ -> i != 2 }.mapIndexed { i, d -> result(d.id, now - (i + 1) * day) }
    val p = build("MEMORIA", AreaStatus("MEMORIA", 0.22f, 0f, listOf(null, null, null, 0.22f)), history)
    assertEquals(g[2].id, p.chosenGameId)
    assertEquals("Nubi elige ${g[2].title}: aún no la juegas", p.chosenLine)
    assertEquals("Sin jugar aún", p.games[2].detail)
    assertEquals("Jugado ayer", p.games[0].detail)
  }

  @Test
  fun si_todos_se_jugaron_elige_el_mas_olvidado_con_los_dias() {
    val g = games("MEMORIA")
    val history = g.mapIndexed { i, d -> result(d.id, now - (if (i == 1) 9 else 1) * day) }
    val p = build("MEMORIA", AreaStatus("MEMORIA", 0.22f, 0f, listOf(null, null, null, 0.22f)), history)
    assertEquals(g[1].id, p.chosenGameId)
    assertEquals("Nubi elige ${g[1].title}: hace 9 días que no lo juegas", p.chosenLine)
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
    val p = build("MEMORIA", AreaStatus("MEMORIA", 0.22f, 0f, listOf(null, null, null, 0.22f)), history)
    assertEquals("Hace 22 días", p.games[0].detail)
    assertEquals("Nubi elige ${g[0].title}: hace 22 días que no lo juegas", p.chosenLine)
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
    val p = build("MEMORIA", AreaStatus("MEMORIA", 0.22f, 0f, listOf(null, null, null, 0.22f)), history)
    assertEquals(listOf<Int?>(0, 1, null, 3), p.games.map { it.daysAgo })
    assertEquals(listOf("hoy", "ayer", "nuevo", "hace 3 días"), p.games.map { it.whenShort })
    assertEquals(listOf("jugado hoy", "jugado ayer", "sin jugar aún", "jugado hace 3 días"), p.games.map { it.spoken })
    assertTrue("ninguna ficha lleva una cifra", p.games.none { row -> row.whenShort.any { it.isDigit() && row.daysAgo !in 2..99 } })
  }
}
