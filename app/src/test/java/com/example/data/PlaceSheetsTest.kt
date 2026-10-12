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
    assertEquals("En el Observatorio vas en Aprendiz. Aquí tienes tu avance y sus juegos.", p.bubble)
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
    assertEquals("En la Biblioteca aún no hay avance: juega uno y lo verás.", p.bubble)
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
    assertEquals("Hace 9 días que no lo juegas", p.games[1].detail)
  }

  @Test
  fun los_rotulos_de_las_semanas_son_los_del_boceto() {
    assertEquals(listOf("Hace 3 sem.", "Hace 2 sem.", "Sem. pasada", "Esta semana"), PlaceSheets.WEEK_LABELS)
  }
}
