package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertThrows
import org.junit.Test

class AreaSuggestionTest {
  private val day = 86_400_000L
  private val now = 100 * day
  private val areas = listOf("Memoria", "Atención", "Razonamiento", "Lenguaje")
  private val dayOf = { t: Long -> t / day }

  private fun pick(last: Map<String, Long?>, value: Map<String, Float?> = emptyMap()) =
    AreaSuggestion.pick(areas, last, value, now, dayOf)

  @Test
  fun `primero el area que nunca se ha jugado, la primera en el orden`() {
    val r = pick(mapOf("Memoria" to now, "Atención" to now - day, "Razonamiento" to null, "Lenguaje" to null))
    assertEquals("Razonamiento", r.area)
    assertEquals("Aún no lo juegas", r.reason)
  }

  @Test
  fun `si todas se jugaron, la que lleva mas dias sin jugarse`() {
    val r = pick(
      mapOf("Memoria" to now - 1 * day, "Atención" to now - 3 * day, "Razonamiento" to now - 4 * day, "Lenguaje" to now - 2 * day)
    )
    assertEquals("Razonamiento", r.area)
    assertEquals("Hace 4 días que no lo juegas", r.reason)
  }

  @Test
  fun `con menos de 2 dias de ausencia se sugiere la de menor avance`() {
    val r = pick(
      mapOf("Memoria" to now, "Atención" to now, "Razonamiento" to now - day, "Lenguaje" to now),
      mapOf("Memoria" to 0.5f, "Atención" to 0.6f, "Razonamiento" to 0.4f, "Lenguaje" to 0.1f)
    )
    assertEquals("Lenguaje", r.area)
    assertEquals("Es el área que más puede crecer", r.reason)
  }

  @Test
  fun `un area jugada pero sin medir cuenta como la de menor avance`() {
    val r = pick(
      mapOf("Memoria" to now, "Atención" to now, "Razonamiento" to now, "Lenguaje" to now),
      mapOf("Memoria" to 0.5f, "Atención" to 0.6f, "Razonamiento" to null, "Lenguaje" to 0.2f)
    )
    assertEquals("Razonamiento", r.area)
  }

  @Test
  fun `el empate de dias se resuelve por el orden de las areas`() {
    val r = pick(mapOf("Memoria" to now - 3 * day, "Atención" to now - 3 * day, "Razonamiento" to now, "Lenguaje" to now))
    assertEquals("Memoria", r.area)
  }

  @Test
  fun `el empate de avance se resuelve por el orden de las areas`() {
    val r = pick(
      mapOf("Memoria" to now, "Atención" to now, "Razonamiento" to now, "Lenguaje" to now),
      mapOf("Memoria" to 0.3f, "Atención" to 0.3f, "Razonamiento" to 0.9f, "Lenguaje" to 0.9f)
    )
    assertEquals("Memoria", r.area)
  }

  @Test
  fun `sin areas es un error de uso`() {
    assertThrows(IllegalArgumentException::class.java) { AreaSuggestion.pick(emptyList(), emptyMap(), emptyMap(), now, dayOf) }
  }
}
