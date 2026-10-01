package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class VocabularyTest {
  private val seen = listOf(10, 10, 10, 10, 10, 3)
  private val hits = listOf(10, 9, 8, 7, 5, 3)

  @Test
  fun `las bandas restan las falsas alarmas y piden 6 palabras vistas`() {
    // 20 inventadas vistas y 2 tocadas: 10% de falsas alarmas (igual que MeteorContract.BandPercents en Unity).
    assertEquals(listOf(90, 80, 70, 60, 40, -1), Vocabulary.bandPercents(seen, hits, listOf(10, 6, 4), listOf(0, 1, 1)))
    assertEquals(100, Vocabulary.bandPercents(seen, hits, listOf(10, 6, 4), listOf(0, 0, 0))[0])
    assertEquals(0, Vocabulary.bandPercents(listOf(10, 0, 0, 0, 0, 0), listOf(1, 0, 0, 0, 0, 0), listOf(10, 0, 0), listOf(5, 0, 0))[0])
    assertEquals(List(6) { -1 }, Vocabulary.bandPercents(null, null, null, null))
  }

  @Test
  fun `los tres grupos juntan bandas, restan falsas alarmas y piden 6 palabras vistas`() {
    // comunes 19/20, intermedias 15/20, raras 8/13 (5 de 10 + 3 de 3); sin inventadas tocadas
    assertEquals(listOf(95, 75, 62), Vocabulary.groupPercents(seen, hits, listOf(10, 6, 4), listOf(0, 0, 0)))
    // 20 inventadas, 2 tocadas: -10 puntos a cada grupo
    assertEquals(listOf(85, 65, 52), Vocabulary.groupPercents(seen, hits, listOf(10, 6, 4), listOf(1, 1, 0)))
    // un grupo con menos de 6 vistas queda sin medir
    assertEquals(listOf(100, -1, -1), Vocabulary.groupPercents(listOf(3, 3, 2, 2, 1, 1), listOf(3, 3, 2, 2, 1, 1), null, null))
    assertEquals(listOf(-1, -1, -1), Vocabulary.groupPercents(null, null, null, null))
  }

  @Test
  fun `la cifra grande sale de la marca de las bandas 3 a 6`() {
    assertEquals(6, Vocabulary.outOfTen(63.25f))
    assertEquals(8, Vocabulary.outOfTen(84f))
    assertEquals(10, Vocabulary.outOfTen(100f))
    assertEquals(0, Vocabulary.outOfTen(2f))
    assertNull(Vocabulary.outOfTen(null))
  }

  @Test
  fun `la frase de tu vocabulario usa rangos y junta grupos iguales`() {
    assertEquals(
      "Reconoces casi todas las comunes, la mayoría de las intermedias y menos de la mitad de las raras.",
      Vocabulary.phraseFor(listOf(98, 84, 43))
    )
    assertEquals("Reconoces casi todas las comunes e intermedias y la mitad de las raras.", Vocabulary.phraseFor(listOf(98, 93, 50)))
    assertEquals("Reconoces casi todas las comunes, intermedias y raras.", Vocabulary.phraseFor(listOf(98, 95, 92)))
    assertEquals("Reconoces más de la mitad de las comunes.", Vocabulary.phraseFor(listOf(60, -1, -1)))
    assertEquals("Reconoces casi todas las comunes y pocas de las intermedias.", Vocabulary.phraseFor(listOf(95, 10, -1)))
    assertTrue(Vocabulary.phraseFor(listOf(-1, -1, -1)).startsWith("Juega un poco más"))
  }

  @Test
  fun `la marca es el promedio ponderado de las bandas 3 a 6`() {
    val p = listOf(100, 96, 90, 78, 55, 30)
    val s = listOf(10, 10, 10, 10, 10, 10)
    // (90 + 78 + 55 + 30) / 4
    assertEquals(63.25f, Vocabulary.mark(p, s)!!, 1e-3f)
    // pondera por las palabras vistas
    assertEquals((90f * 20 + 30f * 10) / 30f, Vocabulary.mark(listOf(-1, -1, 90, -1, -1, 30), listOf(0, 0, 20, 0, 0, 10))!!, 1e-3f)
    assertNull(Vocabulary.mark(listOf(100, 96, -1, -1, -1, -1), s))
  }

  @Test
  fun `el tiempo de reconocimiento necesita las dos medianas`() {
    assertEquals("Las comunes, en 0,7 s. Las raras, en 1,1 s.", Vocabulary.recognitionSentence(700, 1100))
    assertNull(Vocabulary.recognitionSentence(700, -1))
    assertNull(Vocabulary.recognitionSentence(null, 1100))
  }

  @Test
  fun `tu filtro cuenta las inventadas y marca solo el tipo que mas enganio`() {
    assertEquals("Te engañaron 5 de 21 palabras inventadas:", Vocabulary.filterHeadline(listOf(7, 8, 6), listOf(0, 1, 4)))
    assertEquals("No te engañó ninguna de 21 palabras inventadas:", Vocabulary.filterHeadline(listOf(7, 7, 7), listOf(0, 0, 0)))
    assertNull(Vocabulary.filterHeadline(listOf(0, 0, 0), listOf(0, 0, 0)))
    assertEquals(2, Vocabulary.filterHot(listOf(6, 8, 7), listOf(0, 1, 4)))
    assertEquals(0, Vocabulary.filterHot(listOf(6, 8, 7), listOf(5, 1, 4)))
    assertEquals(-1, Vocabulary.filterHot(listOf(6, 8, 7), listOf(1, 1, 1)))   // ninguno llega a la mitad
    assertEquals(-1, Vocabulary.filterHot(listOf(3, 3, 3), listOf(3, 3, 3)))   // pocas vistas
    assertEquals(-1, Vocabulary.filterHot(null, null))
  }

  @Test
  fun `el consejo del filtro solo sale si las traspuestas enganan mas y hay datos de cada tipo`() {
    assertNotNull(Vocabulary.filterAdvice(listOf(6, 8, 7), listOf(0, 1, 4)))
    // las traspuestas no engañan más que las otras
    assertNull(Vocabulary.filterAdvice(listOf(6, 8, 7), listOf(3, 1, 1)))
    // empate: no hay nada que aconsejar
    assertNull(Vocabulary.filterAdvice(listOf(6, 8, 8), listOf(0, 2, 2)))
    // pocas vistas de un tipo
    assertNull(Vocabulary.filterAdvice(listOf(6, 4, 7), listOf(0, 1, 4)))
    assertNull(Vocabulary.filterAdvice(null, null))
    assertTrue(Vocabulary.filterAdvice(listOf(6, 8, 7), listOf(0, 1, 4))!!.contains("centro"))
  }
}
