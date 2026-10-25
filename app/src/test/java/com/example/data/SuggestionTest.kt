package com.example.data

import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Nubi sugiere» (Tarea 150, B2): UNA sola sugerencia para Juegos, Hoy y Avance. */
class SuggestionTest {
  private val day = 86_400_000L
  private val dayOf: (Long) -> Long = { it / day }
  private val now = 400 * day + 3_600_000L
  private val all = GameRegistry.allGames
  private fun gamesOf(area: String) = all.filter { it.domain.name == area }
  private fun playedAll(daysAgo: Long) = all.associate { it.id to now - daysAgo * day }

  @Test
  fun mientras_la_ruta_tiene_paradas_pendientes_sugiere_siempre_la_que_sigue() {
    val route = listOf("calculo", "secuencia", "stroop")
    val p = Suggestion.next(route, 0, playedAll(5), now, dayOf)!!
    assertEquals("calculo", p.gameId); assertEquals("RAZONAMIENTO", p.area); assertEquals(Suggestion.Why.ROUTE, p.why)
    assertEquals("con una hecha, la que sigue", "secuencia", Suggestion.next(route, 1, playedAll(5), now, dayOf)!!.gameId)
    assertEquals("stroop", Suggestion.next(route, 2, playedAll(5), now, dayOf)!!.gameId)
    assertEquals("sigue en tu ruta de hoy", Suggestion.reason(p))
  }

  @Test
  fun terminada_la_ruta_sugiere_el_juego_que_mas_tiempo_lleva_sin_jugarse() {
    val route = listOf("calculo", "secuencia", "stroop")
    val last = playedAll(1).toMutableMap().apply { put("radar", now - 9 * day); put("bodega", now - 4 * day) }
    val p = Suggestion.next(route, 3, last, now, dayOf)!!
    assertEquals("radar", p.gameId); assertEquals("ATENCION", p.area); assertEquals(Suggestion.Why.DAYS, p.why); assertEquals(9, p.days)
    assertEquals("hace 9 días que no lo juegas", Suggestion.reason(p))
    assertEquals("hace 9 días que no juegas ${GameRegistry.getById("radar")!!.title}", Suggestion.reasonForArea(p, GameRegistry.getById("radar")!!.title))
    // sin ruta (nada guardado todavía) pasa lo mismo
    assertEquals("radar", Suggestion.next(emptyList(), 0, last, now, dayOf)!!.gameId)
  }

  @Test
  fun un_juego_nunca_jugado_gana_y_entre_varios_el_primero_del_catalogo() {
    val last = all.drop(2).associate { it.id to now - day }              // los dos primeros nunca se jugaron
    val p = Suggestion.next(emptyList(), 0, last, now, dayOf)!!
    assertEquals(all[0].id, p.gameId); assertEquals(Suggestion.Why.NEVER, p.why)
    assertEquals("aún no lo has jugado", Suggestion.reason(p))
    assertEquals("aún no has jugado X", Suggestion.reasonForArea(p, "X"))
  }

  @Test
  fun ante_un_empate_de_dias_gana_el_primero_del_catalogo() {
    val p = Suggestion.next(emptyList(), 0, playedAll(3), now, dayOf)!!
    assertEquals(all[0].id, p.gameId); assertEquals(3, p.days)
  }

  @Test
  fun la_razon_se_dice_en_palabras_de_tiempo_y_nunca_como_el_area_que_mas_puede_crecer() {
    for (days in 0..9) {
      for (p in listOf(Suggestion.Pick("radar", "ATENCION", Suggestion.Why.DAYS, days), Suggestion.Pick("radar", "ATENCION", Suggestion.Why.ROUTE), Suggestion.Pick("radar", "ATENCION", Suggestion.Why.NEVER))) {
        for (t in listOf(Suggestion.reason(p), Suggestion.reasonForArea(p, "Rescate relámpago"))) {
          assertTrue(t, !t.contains("crecer") && !t.contains("puede"))
          assertEquals(t, null, com.example.NoLossGuard.hit(t, com.example.NoLossGuard.literals))
        }
      }
    }
    assertEquals("hace 1 día que no lo juegas", Suggestion.reason(Suggestion.Pick("radar", "ATENCION", Suggestion.Why.DAYS, 1)))
  }

  @Test
  fun solo_el_area_de_la_sugerencia_lleva_el_chip() {
    val route = listOf("calculo", "secuencia", "stroop")
    val last = playedAll(2).toMutableMap().apply { put("anagramas", now - 8 * day) }
    val global = Suggestion.next(route, 0, last, now, dayOf)                       // sigue «calculo» (Razonamiento)
    assertTrue(Suggestion.suggestedHere("RAZONAMIENTO", global))
    assertTrue("en otra área no hay chip: Nubi sugiere UNA sola cosa", !Suggestion.suggestedHere("LENGUAJE", global))
    assertTrue(!Suggestion.suggestedHere("MEMORIA", null))
  }

  @Test
  fun los_juegos_retirados_nunca_se_sugieren_ni_aunque_esten_en_la_ruta_guardada() {
    val retired = GameRegistry.retiredDomains.keys.first()
    val p = Suggestion.next(listOf(retired), 0, playedAll(2), now, dayOf)
    assertNotNull(p)
    assertTrue(p!!.gameId != retired)
  }

  @Test
  fun el_chip_dice_siempre_lo_mismo() {
    assertEquals("Nubi sugiere", Suggestion.CHIP)
  }
}
