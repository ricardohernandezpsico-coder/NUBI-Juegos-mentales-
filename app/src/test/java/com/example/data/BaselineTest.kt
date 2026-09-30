package com.example.data

import com.example.model.AgeBand
import com.example.model.DomainType
import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class BaselineTest {
  @Test
  fun `el pico de Secuencia se lleva a 0 a 1`() {
    assertEquals(0f, ratingFromSequencePeak(1), 1e-4f)
    assertEquals(8f / 15f, ratingFromSequencePeak(9), 1e-4f)
    assertEquals(1f, ratingFromSequencePeak(16), 1e-4f)
    assertEquals(1f, ratingFromSequencePeak(30), 1e-4f)
  }

  @Test
  fun `los dominios no medidos toman el promedio de los medidos`() {
    val b = buildBaseline(mapOf("secuencia" to 0.6f, "stroop" to 0.4f, "comparacion" to 0.5f), timestamp = 1L)
    assertEquals(setOf(DomainType.MEMORIA, DomainType.ATENCION), b.measured.keys)
    assertEquals(0.6f, b.domains[DomainType.MEMORIA]!!, 1e-4f)
    // Atención tiene dos pasos (stroop 0.4 y comparación 0.5): vale el promedio, 0.45.
    assertEquals(0.45f, b.domains[DomainType.ATENCION]!!, 1e-4f)
    // Razonamiento y Lenguaje no se miden: promedio de lo medido (0.6 y 0.45).
    assertEquals(0.525f, b.domains[DomainType.RAZONAMIENTO]!!, 1e-4f)
    assertEquals(DomainType.values().size, b.domains.size)
  }

  @Test
  fun `sin mediciones queda la referencia`() {
    val b = buildBaseline(emptyMap(), timestamp = 1L)
    assertTrue(b.measured.isEmpty())
    assertEquals(Percentile.PROVISIONAL_MEAN, b.domains[DomainType.MEMORIA]!!, 1e-4f)
  }

  @Test
  fun `cada juego toma el rating de su dominio`() {
    val b = buildBaseline(mapOf("secuencia" to 0.8f, "stroop" to 0.2f, "comparacion" to 0.5f), timestamp = 1L)
    val seeds = seedRatings(b)
    assertEquals(GameRegistry.allGames.size, seeds.size)
    assertEquals(0.8f, seeds["parejas"]!!, 1e-4f)   // memoria
    assertEquals(0.35f, seeds["cambiochip"]!!, 1e-4f) // atención = promedio de sus 2 pasos (stroop 0.2 y comparación 0.5)
    assertEquals(0.35f, seeds["radar"]!!, 1e-4f)      // radar ahora también es Atención
    assertEquals(0.575f, seeds["series"]!!, 1e-4f)    // estimado = promedio de lo medido (0.8 y 0.35)
  }

  @Test
  fun `el punto de partida estimado se mueve poco con edad y educacion`() {
    val base = priorRating(AgeBand.ADULT, Education.NO_DICE)
    assertEquals(Percentile.PROVISIONAL_MEAN, base, 1e-4f)
    assertTrue(priorRating(AgeBand.SENIOR, Education.BASICA) < base)
    assertTrue(priorRating(AgeBand.ADULT, Education.POSTGRADO) > base)
    assertTrue(priorRating(AgeBand.ADULT, Education.POSTGRADO) - base <= 0.05f + 1e-4f)
  }

  @Test
  fun `nivel desde rating`() {
    assertEquals(1, levelFromRating(0f))
    assertEquals(3, levelFromRating(0.45f))
    assertEquals(5, levelFromRating(1f))
  }

  @Test
  fun `el camino prioriza metas y luego lo mas bajo`() {
    val levels = DomainType.values().associateWith { 0.5f } + (DomainType.RAZONAMIENTO to 0.1f) + (DomainType.MEMORIA to 0.9f)
    val order = rankDomainsForSession(setOf(DomainType.MEMORIA), levels)
    assertEquals(DomainType.MEMORIA, order[0])
    assertEquals(DomainType.RAZONAMIENTO, order[1])
    assertEquals(DomainType.values().size, order.size)
  }

  @Test
  fun `ida y vuelta del mapa y de las metas`() {
    val b = buildBaseline(mapOf("secuencia" to 0.61f, "stroop" to 0.42f), timestamp = 1_700_000_000_000)
    assertEquals(b, decodeBaseline(encodeBaseline(b)))
    val goals = setOf(DomainType.LENGUAJE, DomainType.ATENCION)
    assertEquals(goals, decodeGoals(encodeGoals(goals)))
    assertNull(decodeBaseline("basura"))
    assertTrue(decodeGoals(null).isEmpty())
  }

  @Test
  fun `un punto de partida guardado con 6 areas se lee con 4 y se promedia`() {
    // Velocidad (0.8) pasa a Atención (0.4): 0.6. Cálculo (0.3) pasa a Razonamiento (0.5): 0.4.
    val raw = "5|MEMORIA=0.5,ATENCION=0.4,VELOCIDAD=0.8|MEMORIA=0.5,ATENCION=0.4,VELOCIDAD=0.8,RAZONAMIENTO=0.5,CALCULO=0.3,LENGUAJE=0.45"
    val b = decodeBaseline(raw)!!
    assertEquals(0.6f, b.domains[DomainType.ATENCION]!!, 1e-4f)
    assertEquals(0.4f, b.domains[DomainType.RAZONAMIENTO]!!, 1e-4f)
    assertEquals(0.6f, b.measured[DomainType.ATENCION]!!, 1e-4f)
    assertEquals(4, b.domains.size)
  }

  @Test
  fun `las metas guardadas con areas viejas se leen con las nuevas`() {
    assertEquals(setOf(DomainType.ATENCION, DomainType.RAZONAMIENTO, DomainType.MEMORIA), decodeGoals("VELOCIDAD,CALCULO,MEMORIA,INVENTADA"))
  }

  @Test
  fun `los nombres guardados de las areas`() {
    assertEquals(DomainType.ATENCION, DomainType.fromStored("VELOCIDAD"))
    assertEquals(DomainType.RAZONAMIENTO, DomainType.fromStored("CALCULO"))
    assertEquals(DomainType.LENGUAJE, DomainType.fromStored("LENGUAJE"))
    assertNull(DomainType.fromStored("OTRA"))
    assertNull(DomainType.fromStored(null))
    assertEquals(4, DomainType.entries.size)
  }
}
