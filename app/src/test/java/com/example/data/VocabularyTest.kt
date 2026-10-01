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
  fun `las estrellas encendidas van de media en media`() {
    assertEquals(5f, Vocabulary.litStars(100), 0f)
    assertEquals(5f, Vocabulary.litStars(96), 0f)
    assertEquals(4.5f, Vocabulary.litStars(90), 0f)
    assertEquals(4f, Vocabulary.litStars(78), 0f)
    assertEquals(3f, Vocabulary.litStars(55), 0f)
    assertEquals(1.5f, Vocabulary.litStars(30), 0f)
    assertEquals(0f, Vocabulary.litStars(0), 0f)
  }

  @Test
  fun `la frase de tu vocabulario dice hasta donde reconoces casi todas`() {
    assertEquals(
      "Reconoces casi todas hasta las poco frecuentes; las raras, la mitad.",
      Vocabulary.phrase(listOf(100, 96, 90, 78, 55, 30))
    )
    assertEquals("Reconoces casi todas las palabras, hasta las raras.", Vocabulary.phrase(listOf(100, 100, 98, 95, 90, 88)))
    assertEquals("Reconoces casi todas las que viste.", Vocabulary.phrase(listOf(100, 99, -1, -1, -1, -1)))
    assertEquals("De las comunes reconoces la mitad.", Vocabulary.phrase(listOf(50, 45, -1, -1, -1, -1)))
    assertEquals("Reconoces casi todas hasta las comunes; las poco frecuentes, pocas.", Vocabulary.phrase(listOf(95, 90, 25, 20, -1, -1)))
    assertTrue(Vocabulary.phrase(List(6) { -1 }).startsWith("Juega un poco más"))
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
    assertEquals("comunes 0,7 s · raras 1,1 s", Vocabulary.recognitionLine(700, 1100))
    assertNull(Vocabulary.recognitionLine(700, -1))
    assertNull(Vocabulary.recognitionLine(null, 1100))
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
