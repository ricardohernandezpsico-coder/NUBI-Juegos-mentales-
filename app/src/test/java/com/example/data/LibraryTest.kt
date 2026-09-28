package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class LibraryTest {
  private val day = 86_400_000L
  private val now = 300 * day + 3_600_000L
  private val dayOf = { t: Long -> t / day }
  private val names = mapOf("LENGUAJE" to "Lenguaje", "MEMORIA" to "Memoria", "ATENCION" to "Atención")
  private val games = listOf(
    LibraryGame("anagramas", "LENGUAJE", star = false),
    LibraryGame("secuencia", "MEMORIA", star = false),
    LibraryGame("rumbo", "MEMORIA", star = true),
    LibraryGame("radar", "ATENCION", star = true)
  )

  private fun planet(quiet: String?, quietDays: Int) = PlanetState(emptyList(), null, quiet, quietDays, emptyList())

  @Test
  fun `primero la zona quieta, despues las estrellas sin probar y los olvidados, sin repetir`() {
    val last = mapOf("anagramas" to now - 6 * day, "secuencia" to now - 3 * day, "radar" to now)
    val p = Library.picks(planet("LENGUAJE", 6), games, last, now, dayOf) { names[it] ?: it }
    assertEquals("anagramas", p[0].gameId)
    assertEquals("Tu zona de Lenguaje está quieta hace 6 días.", p[0].reason)
    assertEquals("rumbo", p[1].gameId)
    assertTrue(p[1].reason.startsWith("Nuevo para ti"))
    assertEquals("secuencia", p[2].gameId)
    assertEquals("No lo juegas hace 3 días.", p[2].reason)
    assertEquals(p.size, p.map { it.gameId }.distinct().size)
  }

  @Test
  fun `zona sin explorar y, si todo esta al dia, igual sugiere uno`() {
    val p = Library.picks(planet("LENGUAJE", -1), games, emptyMap(), now, dayOf) { names[it] ?: it }
    assertEquals("Aún no exploras tu zona de Lenguaje.", p[0].reason)
    val fresh = games.associate { it.id to now }
    val q = Library.picks(planet(null, 0), games, fresh, now, dayOf) { it }
    assertEquals(1, q.size)
  }
}
