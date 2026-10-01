package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class ReadingTest {
  // corta 0,9 s · complemento 1,0 · negación 1,8 · pausa 1,5 · todos/algunos sin dato · comparación 1,3
  private val rt = listOf(900, 1000, 1800, 1500, -1, 1300)

  @Test
  fun `las palabras por minuto y la marca solo existen con medida`() {
    assertEquals(142, Reading.wpm(142))
    assertNull(Reading.wpm(-1))
    assertNull(Reading.wpm(null))
    assertEquals(142f, Reading.mark(142)!!, 0f)
    assertNull(Reading.mark(-1))
    assertNull(Reading.mark(0))
  }

  @Test
  fun `una fila por tipo con tiempo y la mas lenta marcada`() {
    val rows = Reading.typeRows(rt)
    assertEquals(listOf(0, 1, 2, 3, 5), rows.map { it.index })           // el tipo sin dato no sale
    assertEquals(listOf(false, false, true, false, false), rows.map { it.slowest })
    assertEquals("Negaciones", rows.first { it.slowest }.label)
  }

  @Test
  fun `con una sola fila no se marca ninguna como la mas lenta`() {
    val rows = Reading.typeRows(listOf(900, -1, -1, -1, -1, -1))
    assertEquals(1, rows.size)
    assertFalse(rows[0].slowest)
    assertTrue(Reading.typeRows(null).isEmpty())
    assertTrue(Reading.typeRows(listOf(-1, -1, -1, -1, -1, -1)).isEmpty())
  }

  @Test
  fun `la frase de lo que frena compara con las frases simples y da el truco por tipo`() {
    val rows = Reading.typeRows(rt)
    assertEquals("Las negaciones te toman 0,9 s más que las frases simples. Es normal.", Reading.slowText(rows))
    assertEquals("Truco: lee la frase sin el «no» y después dala vuelta.", Reading.tip(rows))
    // pausa
    val pause = Reading.typeRows(listOf(900, 1000, 1200, 2000, 1500, 1300))
    assertEquals("Las frases con pausa te toman 1,1 s más que las frases simples. Es normal.", Reading.slowText(pause))
    assertEquals("Truco: fíjate solo en lo que va después de la segunda coma.", Reading.tip(pause))
    // todos / algunos
    val quant = Reading.typeRows(listOf(900, 1000, 1200, 1500, 2100, 1300))
    assertTrue(Reading.tip(quant)!!.contains("un solo ejemplo"))
    // comparación
    val comp = Reading.typeRows(listOf(900, 1000, 1200, 1500, 1400, 2200))
    assertTrue(Reading.tip(comp)!!.contains("una al lado de la otra"))
  }

  @Test
  fun `no se aconseja nada si lo mas lento ya es una frase simple o la diferencia es chica`() {
    val simple = Reading.typeRows(listOf(2000, 1000, 1200, 1100, 1000, 1000))
    assertNull(Reading.slowText(simple))
    assertNull(Reading.tip(simple))
    val close = Reading.typeRows(listOf(900, 1000, 1100, 1050, 1000, 1000))     // 1,1 s − 0,9 s = 0,2 s
    assertNull(Reading.slowText(close))
    assertNull(Reading.tip(close))
    assertTrue(Reading.balanced(close))
    assertFalse(Reading.balanced(Reading.typeRows(rt)))
  }

  @Test
  fun `sin frases cortas se compara con las de complemento`() {
    val rows = Reading.typeRows(listOf(-1, 1000, 1900, -1, -1, -1))
    assertEquals("Las negaciones te toman 0,9 s más que las frases simples. Es normal.", Reading.slowText(rows))
    // sin ninguna simple no hay con qué comparar
    assertNull(Reading.slowText(Reading.typeRows(listOf(-1, -1, 1900, 1200, -1, -1))))
  }

  @Test
  fun `la precision con y sin disparates sutiles`() {
    assertEquals("27 de 30 · disparates sutiles 6 de 8", Reading.precisionLine(listOf(5, 5, 5, 4, 4, 4), listOf(5, 5, 5, 5, 5, 5), 6, 8))
    assertEquals("27 de 30", Reading.precisionLine(listOf(5, 5, 5, 4, 4, 4), listOf(5, 5, 5, 5, 5, 5), 0, 0))
    assertEquals("27 de 30", Reading.precisionLine(listOf(5, 5, 5, 4, 4, 4), listOf(5, 5, 5, 5, 5, 5), null, null))
    assertNull(Reading.precisionLine(null, null, null, null))
    assertNull(Reading.precisionLine(listOf(0), listOf(0), 0, 0))
  }

  @Test
  fun `la mejor racha en palabras`() {
    assertEquals("14 seguidas", Reading.streakLine(14))
    assertEquals("1 seguida", Reading.streakLine(1))
    assertNull(Reading.streakLine(0))
    assertNull(Reading.streakLine(null))
  }

  @Test
  fun `las barras van de lo mas corto a lo mas largo respecto del tipo mas lento`() {
    val rows = Reading.typeRows(rt)
    assertEquals(1f, Reading.barFraction(1800, rows), 1e-4f)
    assertEquals(0.5f, Reading.barFraction(900, rows), 1e-4f)
    assertNotNull(Reading.seconds(900))
    assertEquals("0,9 s", Reading.seconds(900))
    assertEquals("1,2 s", Reading.seconds(1234))
  }

  @Test
  fun `no existe ningun perfil de sesgo en la lectura`() {
    // regla de patentes (US 11,839,472): la lectura solo habla de tiempos y aciertos por tipo de frase
    val names = Reading::class.java.declaredMethods.map { it.name.lowercase() }
    assertTrue(names.none { "bias" in it || "sesgo" in it || "impuls" in it || "conserv" in it })
  }
}
