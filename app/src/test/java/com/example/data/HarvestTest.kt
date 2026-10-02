package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class HarvestTest {
  @Test
  fun `las palabras y la linea de las comunes`() {
    assertEquals(34, Harvest.words(34))
    assertEquals(0, Harvest.words(0))
    assertNull(Harvest.words(-1))
    assertNull(Harvest.words(null))
    assertEquals("palabra", Harvest.wordsLabel(1))
    assertEquals("palabras", Harvest.wordsLabel(0))
    assertEquals("palabras", Harvest.wordsLabel(34))
    assertEquals("de las comunes, 21 de 48", Harvest.commonLine(21, 48))
    assertEquals("de las comunes, 48 de 48", Harvest.commonLine(60, 48))     // nunca más que las que había
    assertNull(Harvest.commonLine(5, 0))
    assertNull(Harvest.commonLine(null, 48))
  }

  @Test
  fun `la marca es el porcentaje de comunes encontradas`() {
    assertEquals(43.75f, Harvest.mark(21, 48)!!, 1e-3f)
    assertEquals(100f, Harvest.mark(99, 48)!!, 1e-3f)
    assertEquals(0f, Harvest.mark(0, 48)!!, 1e-3f)
    assertNull(Harvest.mark(null, 48))
    assertNull(Harvest.mark(10, null))
    assertNull(Harvest.mark(10, 0))
  }

  @Test
  fun `la manera de buscar con sus etiquetas y su consejo`() {
    assertEquals("Racimos 60 %", Harvest.clusterLabel(60))
    assertEquals("Saltos 40 %", Harvest.jumpLabel(60))
    assertTrue(Harvest.searchLine(80)!!.startsWith("Exprimes bien cada idea"))
    assertTrue(Harvest.searchLine(80)!!.contains("saltar a otra letra inicial"))
    assertTrue(Harvest.searchLine(70)!!.startsWith("Exprimes"))
    assertTrue(Harvest.searchLine(20)!!.startsWith("Saltas rápido"))
    assertTrue(Harvest.searchLine(20)!!.contains("busca sus parientes"))
    assertTrue(Harvest.searchLine(30)!!.startsWith("Saltas"))
    assertTrue(Harvest.searchLine(55)!!.startsWith("Combinas bien"))
    assertNull(Harvest.searchLine(null))
    assertNull(Harvest.searchLine(-1))
  }

  @Test
  fun `el ritmo entre los primeros y los ultimos veinte segundos`() {
    assertEquals(Harvest.Rhythm.STRONG_START, Harvest.rhythm(13, 6))
    assertEquals(Harvest.Rhythm.STEADY, Harvest.rhythm(8, 7))
    assertEquals(Harvest.Rhythm.STEADY, Harvest.rhythm(1, 1))
    assertEquals(Harvest.Rhythm.LATE_RISE, Harvest.rhythm(4, 9))
    assertNull(Harvest.rhythm(0, 0))
    assertNull(Harvest.rhythm(null, 3))
    assertNull(Harvest.rhythm(-1, 3))
    assertEquals("Arrancas fuerte y bajas al final: es lo normal.", Harvest.rhythmLine(13, 6))
    assertEquals("Mantienes el ritmo de principio a fin.", Harvest.rhythmLine(8, 7))
    assertEquals("Te soltaste al final: cada idea te llevó a la siguiente.", Harvest.rhythmLine(4, 9))
    assertNull(Harvest.rhythmLine(0, 0))
  }

  @Test
  fun `el consejo del ritmo solo aparece si se baja al final`() {
    assertEquals("Si te atascas, cambia de idea: otra letra o otra terminación.", Harvest.rhythmTip(13, 6))
    assertNull(Harvest.rhythmTip(8, 7))
    assertNull(Harvest.rhythmTip(4, 9))
    assertNull(Harvest.rhythmTip(null, null))
  }

  @Test
  fun `las barras van respecto de la mayor y siempre se ven`() {
    assertEquals(1f, Harvest.barFraction(13, 6), 1e-4f)
    assertEquals(6f / 13f, Harvest.barFraction(6, 13), 1e-4f)
    assertEquals(0.05f, Harvest.barFraction(0, 10), 1e-4f)
    assertEquals(0f, Harvest.barFraction(0, 0), 1e-4f)
  }

  @Test
  fun `tambien podias`() {
    assertEquals("También podías: saciar, acosar, rosca, ácaros y orcas.", Harvest.missedLine(listOf("saciar", "acosar", "rosca", "ácaros", "orcas")))
    assertEquals("También podías: casa y caso.", Harvest.missedLine(listOf("casa", "caso")))
    assertEquals("También podías: casa.", Harvest.missedLine(listOf("casa")))
    assertEquals("También podías: a, b, c, d y e.", Harvest.missedLine(listOf("a", "b", "c", "d", "e", "f")))     // hasta cinco
    assertNull(Harvest.missedLine(emptyList()))
    assertNull(Harvest.missedLine(null))
    assertNull(Harvest.missedLine(listOf(" ", "")))
  }

  @Test
  fun `la palabra estrella o la mas larga`() {
    assertEquals("Tu palabra estrella" to "ASOCIAR", Harvest.starTitle("asociar", "saciar"))
    assertEquals("Tu palabra más larga" to "SACIAR", Harvest.starTitle("", "saciar"))
    assertEquals("Tu palabra más larga" to "SACIAR", Harvest.starTitle(null, "saciar"))
    assertNull(Harvest.starTitle(null, null))
    assertNull(Harvest.starTitle("", " "))
  }

  @Test
  fun `la descripcion para lectores de pantalla`() {
    assertEquals("34 palabras, de las comunes, 21 de 48", Harvest.spoken(34, 21, 48))
    assertEquals("1 palabra", Harvest.spoken(1, null, null))
  }

  @Test
  fun `no hay perfiles ni comparaciones con otras personas`() {
    // el analisis de agrupar y saltar solo DESCRIBE como se busco: ni normas ni percentiles
    val names = Harvest::class.java.declaredMethods.map { it.name.lowercase() }
    assertTrue(names.none { "percentil" in it || "norma" in it || "perfil" in it || "diagnos" in it })
    assertFalse(Harvest.searchLine(50)!!.contains("normal"))
    assertNotNull(Harvest.searchLine(50))
  }
}
