package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/** Las medidas de Tinta o Palabra («Dos orillas») en palabras, y su consejo («Truco para la próxima» de su final). */
class DosOrillasTest {
  @Test
  fun `la diferencia se dice en segundos con coma, y casi nada bajo 0,05 s`() {
    assertEquals("+0,4 s", DosOrillas.cost(400))
    assertEquals("+0,3 s", DosOrillas.cost(349))
    assertEquals("+1,2 s", DosOrillas.cost(1210))
    assertEquals("+0,1 s", DosOrillas.cost(50))
    assertEquals("casi nada", DosOrillas.cost(49))
    assertEquals("casi nada", DosOrillas.cost(0))
  }

  @Test
  fun `los textos no prometen ni diagnostican`() {
    val all = listOf(DosOrillas.INTERFERENCE_TITLE, DosOrillas.INTERFERENCE_LINE, DosOrillas.SWITCH_TITLE, DosOrillas.SWITCH_LINE) +
      DosOrillas.tips(900, 900)
    all.forEach { t ->
      val l = t.lowercase()
      listOf("control ejecutivo", "concentración", "cerebro", "neurona", "diagnóstico", "mejora", "déficit").forEach { assertFalse(t, l.contains(it)) }
    }
    assertEquals("Cuánto te frenó la palabra", DosOrillas.INTERFERENCE_TITLE)
    assertEquals("Cambiar de orilla te costó", DosOrillas.SWITCH_TITLE)
  }

  @Test
  fun `el consejo solo sale con una diferencia grande y con datos`() {
    assertTrue(DosOrillas.tips(null, null).isEmpty())
    assertTrue(DosOrillas.tips(299, 299).isEmpty())
    assertEquals(1, DosOrillas.tips(300, null).size)
    assertEquals(1, DosOrillas.tips(null, 300).size)
    assertEquals(2, DosOrillas.tips(500, 500).size)
  }

  @Test
  fun `el consejo trae su marca de truco y no repite la medida`() {
    val tips = DosOrillas.tips(450, 100).map { ResultAdvice.tipOf(it) }
    assertEquals(1, tips.size)
    assertTrue(tips[0], tips[0]!!.startsWith("Mira primero de qué orilla llega"))
    assertFalse(DosOrillas.INTERFERENCE_LINE.contains("Truco"))
    // sin medidas (menos de 3 aciertos de cada tipo) o con diferencia chica no hay consejo
    assertTrue(DosOrillas.tips(null, null).isEmpty())
    assertTrue(DosOrillas.tips(100, 0).isEmpty())
  }
}
