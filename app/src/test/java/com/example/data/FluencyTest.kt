package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class FluencyTest {
  private fun round(valid: Int, cluster: Float, switches: Int, quarters: List<Int> = emptyList(), category: String = "animales") =
    FluencyRound(category, valid, 0, 0, cluster, switches, quarters, emptyList(), emptyList())

  @Test
  fun `lee las rondas que llegan de Unity`() {
    val r = Fluency.rounds(
      categories = listOf("animales", "letra_p"),
      valid = listOf(20, 12), repeats = listOf(1, 0), unknown = listOf(2, 1),
      cluster = listOf(2f, 0.8f), switches = listOf(6, 7),
      quarters = listOf(8, 6, 3, 3, 4, 3, 3, 2),
      tops = listOf("felinos:5;del mar:4;de la casa:2", ""),
      words = listOf("perro|gato|león", "pato|pala")
    )
    assertEquals(2, r.size)
    assertEquals("Animales", r[0].title)
    assertEquals("Palabras con P", r[1].title)
    assertTrue(r[1].isLetter)
    assertEquals(listOf("felinos" to 5, "del mar" to 4, "de la casa" to 2), r[0].constellations)
    assertEquals(listOf(4, 3, 3, 2), r[1].quarters)
    assertEquals(listOf("perro", "gato", "león"), r[0].words)
    assertTrue(r[1].constellations.isEmpty())
  }

  @Test
  fun `estilo solo con palabras suficientes`() {
    assertEquals(FluencyStyle.SIN_DATOS, Fluency.style(round(6, 0.2f, 5)))
    // Como la prueba de Ricardo por abecedario: 33 palabras, agrupa 0,22, 26 saltos.
    assertEquals(FluencyStyle.SALTA_MUCHO, Fluency.style(round(33, 0.22f, 26)))
    // Como su prueba "pensando": 20 palabras, agrupa 2, 5 saltos.
    assertEquals(FluencyStyle.AGRUPA_MUCHO, Fluency.style(round(20, 2f, 5)))
    assertEquals(FluencyStyle.EQUILIBRIO, Fluency.style(round(18, 1f, 8)))
    assertNull(Fluency.styleMessage(FluencyStyle.SIN_DATOS))
  }

  @Test
  fun `ritmo y coleccion`() {
    assertTrue(Fluency.fastStart(round(20, 1f, 8, listOf(9, 5, 3, 3))))
    assertFalse(Fluency.fastStart(round(20, 1f, 8, listOf(5, 5, 5, 5))))
    assertFalse(Fluency.fastStart(round(5, 1f, 2, listOf(3, 1, 1, 0))))

    val r = FluencyRound("animales", 3, 0, 0, 1f, 1, emptyList(), emptyList(), listOf("Perro", "gato", "león"))
    val (set, added) = Fluency.merge(setOf("animales:perro"), listOf(r))
    assertEquals(2, added)
    assertEquals(3, set.size)
    assertTrue("animales:león" in set)
  }
}
