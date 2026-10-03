package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** La medida de Rastro de luz (docs/diseno-rastro-de-luz.md §6): tu rastro, los modos con 3 rondas o más, «el que más te costó» con TEXTO y la lectura. */
class TrailTest {
  // ---- la cifra grande

  @Test
  fun `tu rastro es el mejor largo del rastro simple`() {
    assertEquals(5, Trail.bestTrail(listOf(5, 3, 4, 2)))
    assertEquals(5f, Trail.mark(listOf(5, 3, 4, 2))!!, 1e-6f)
    assertEquals("5 luces", Trail.lights(5))
    assertEquals("1 luz", Trail.lights(1))
  }

  @Test
  fun `sin rastro simple repetido no hay cifra y se dice con calma`() {
    assertNull(Trail.bestTrail(listOf(0, 3, 0, 0)))
    assertNull(Trail.mark(listOf(0, 3, 0, 0)))
    assertNull(Trail.bestTrail(null))
    assertNull(Trail.bestTrail(emptyList()))
    assertTrue(Trail.noTrailLine(listOf(2, 0, 0, 0)).contains("Esta vez no salió"))
    assertTrue(Trail.noTrailLine(listOf(0, 4, 0, 0)).contains("no tuvo rastro simple"))
    assertFalse(Trail.noTrailLine(listOf(2, 0, 0, 0)).contains("fall"))     // sin culpa
  }

  // ---- por modo

  @Test
  fun `cada modo se muestra solo con tres rondas o mas`() {
    val rows = Trail.modeRows(rounds = listOf(5, 2, 3, 4), hits = listOf(5, 2, 2, 4))
    assertEquals(listOf("El cielo gira", "En marcha"), rows.map { it.label })     // al revés solo tuvo 2 rondas
    assertEquals(listOf(3, 4), rows.map { it.rounds })
    assertEquals(listOf(2, 4), rows.map { it.hits })
  }

  @Test
  fun `el modo con menos aciertos se marca como el que mas costo`() {
    val rows = Trail.modeRows(rounds = listOf(6, 4, 4, 3), hits = listOf(5, 3, 1, 3))
    val low = rows.filter { it.lowest }
    assertEquals(1, low.size)
    assertEquals("El cielo gira", low[0].label)
    assertEquals(25, low[0].percent)
    assertEquals(75, rows.first { it.label == "Al revés" }.percent)
  }

  @Test
  fun `un solo modo o todos iguales no marcan ninguno`() {
    assertTrue(Trail.modeRows(listOf(5, 4, 0, 0), listOf(5, 2, 0, 0)).none { it.lowest })          // un solo modo
    assertTrue(Trail.modeRows(listOf(5, 4, 4, 4), listOf(5, 3, 3, 3)).none { it.lowest })          // empatan
  }

  @Test
  fun `sin datos o con otro largo no hay filas`() {
    assertTrue(Trail.modeRows(null, null).isEmpty())
    assertTrue(Trail.modeRows(listOf(1, 2, 3), listOf(1, 2, 3)).isEmpty())
    assertTrue(Trail.modeRows(listOf(9, 2, 2, 2), listOf(9, 2, 2, 2)).isEmpty())                   // ninguno llega a 3
  }

  @Test
  fun `los aciertos no pasan de las rondas`() {
    val row = Trail.modeRows(listOf(0, 3, 0, 0), listOf(0, 9, 0, 0)).single()
    assertEquals(3, row.hits)
    assertEquals(100, row.percent)
  }

  // ---- lectura en palabras

  @Test
  fun `hay una linea por modo y un truco solo para los tres modos`() {
    for (mode in 1..3) {
      assertNotNull(Trail.modeLine(mode))
      assertNotNull(Trail.trick(mode))
    }
    assertNull(Trail.modeLine(0))
    assertNull(Trail.trick(0))
    assertTrue(Trail.modeLine(1)!!.contains("reordenar en la cabeza"))
  }

  @Test
  fun `la lectura no promete salud ni compara con otras personas`() {
    val all = listOf(Trail.WORKING_MEMORY) + (1..3).flatMap { listOfNotNull(Trail.modeLine(it), Trail.trick(it)) }
    for (t in all) {
      assertFalse(t, t.contains("neurona", ignoreCase = true))
      assertFalse(t, t.contains("mejora", ignoreCase = true))
      assertFalse(t, t.contains("%"))
    }
    assertTrue(Trail.WORKING_MEMORY.contains("memoria de trabajo"))
  }

  @Test
  fun `los modos nuevos de la partida se dicen una sola vez`() {
    assertNull(Trail.unlockedLine(null))
    assertNull(Trail.unlockedLine(0))
    assertNull(Trail.unlockedLine(1))                       // el rastro simple nunca es «nuevo»
    assertEquals("Desbloqueaste un modo nuevo: Al revés", Trail.unlockedLine(2))
    assertEquals("Desbloqueaste modos nuevos: El cielo gira, En marcha", Trail.unlockedLine(4 or 8))
  }

  @Test
  fun `lo dicho a un lector de pantalla`() {
    assertEquals("Tu rastro: 4 luces", Trail.spoken(listOf(4, 0, 0, 0)))
    assertEquals("Sin rastro completo en esta partida", Trail.spoken(listOf(0, 0, 0, 0)))
  }

  // ---- la medida se guarda para ver su evolución

  @Test
  fun `la medida de Rastro de luz esta entre las de los juegos estrella`() {
    val def = StarMeasures.def("trail")!!
    assertEquals("secuencia", def.gameId)
    assertFalse(def.lowerIsBetter)
    assertTrue(def.levelDependent)
    assertEquals("5 luces", def.compact(5f))
    assertEquals("Rastro de luz", StarMeasures.gameNames["secuencia"])
    assertEquals(def, StarMeasures.defForGame("secuencia"))
  }

  @Test
  fun `la evolucion compara solo partidas de un nivel parecido`() {
    // Rastro de luz tiene 16 niveles: una banda de 1/16 del rating
    val pts = listOf(
      MeasurePoint(1, "trail", 4f, rating = 0.20f, timed = true),
      MeasurePoint(2, "trail", 5f, rating = 0.21f, timed = true),
      MeasurePoint(3, "trail", 8f, rating = 0.80f, timed = true),   // otro nivel: no se compara con las de arriba
      MeasurePoint(4, "trail", 5f, rating = 0.22f, timed = true)
    )
    assertEquals(listOf(1L, 2L, 4L), StarMeasures.comparable(pts).map { it.timestamp })
  }
}
