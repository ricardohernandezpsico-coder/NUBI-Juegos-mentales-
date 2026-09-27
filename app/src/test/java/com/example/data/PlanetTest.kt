package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class PlanetTest {
  private val day = 86_400_000L
  private val dayOf = { t: Long -> t / day }
  private val now = 100 * day + 3_600_000L

  private fun plays(domain: String, vararg daysAgo: Int) = daysAgo.map { PlanetPlay(domain, now - it * day) }

  @Test
  fun `las zonas crecen rapido al principio, nunca bajan y se llenan`() {
    assertEquals(0f, Planet.growth(0))
    assertTrue(Planet.growth(1) > 0.1f)
    assertTrue(Planet.growth(10) > Planet.growth(3))
    assertEquals(1f, Planet.growth(Planet.FULL_AT))
    assertEquals(1f, Planet.growth(500))
    assertEquals(0, Planet.structures(0))
    assertEquals(1, Planet.structures(1))
    assertEquals(5, Planet.structures(80))
  }

  @Test
  fun `lo que crecio esta semana, lo quieto y lo jugado hoy`() {
    val all = plays("MEMORIA", 0, 1, 2) + plays("ATENCION", 0, 3) + plays("LENGUAJE", 9) +
      plays("RAZONAMIENTO", 1) + plays("CALCULO", 2) + plays("VELOCIDAD", 5)
    val s = Planet.build(all, now, dayOf)
    assertEquals("MEMORIA", s.grewThisWeek)
    assertEquals("LENGUAJE", s.quiet)
    assertEquals(9, s.quietDays)
    assertEquals(setOf("MEMORIA", "ATENCION"), s.playedToday.toSet())
    assertTrue(s.zones.first { it.domain == "MEMORIA" }.playedToday)
    assertEquals(3, s.zones.first { it.domain == "MEMORIA" }.weekPlays)
  }

  @Test
  fun `una zona sin explorar se nombra antes que una quieta, y sin partidas no se nombra nada`() {
    val s = Planet.build(plays("MEMORIA", 0) + plays("ATENCION", 8), now, dayOf)
    assertEquals("RAZONAMIENTO", s.quiet)
    assertEquals(-1, s.quietDays)
    val empty = Planet.build(emptyList(), now, dayOf)
    assertNull(empty.quiet)
    assertNull(empty.grewThisWeek)
    // Todas jugadas hace poco: nada quieto.
    val fresh = Planet.build(Planet.DOMAINS.flatMap { plays(it, 1) }, now, dayOf)
    assertNull(fresh.quiet)
  }

  @Test
  fun `partidas por semana, la ultima es la de hoy`() {
    val ts = listOf(0, 1, 6, 7, 15, 30).map { now - it * day }
    assertEquals(listOf(0, 1, 1, 3), Planet.weeklyCounts(ts, now, dayOf))
  }
}
