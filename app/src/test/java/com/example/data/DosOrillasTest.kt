package com.example.data

import com.example.model.GamePlayResult
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/** Las medidas de Tinta o Palabra («Dos orillas») en palabras, y su consejo para «¿qué te sirve más ver primero?». */
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
  fun `el consejo entra a ResultAdvice sin repetir la medida`() {
    val r = GamePlayResult(gameId = "stroop", score = 70, correctAnswers = 11, totalTrials = 16, timed = false, level = 3, interferenceMs = 450, switchCostMs = 100)
    val tips = ResultAdvice.tips(r)
    assertEquals(1, tips.size)
    assertTrue(tips[0], tips[0].startsWith("Mira primero de qué orilla llega"))
    assertFalse(DosOrillas.INTERFERENCE_LINE.contains("Truco"))
    // sin medidas (menos de 3 aciertos de cada tipo) no hay bloque de consejo
    assertTrue(ResultAdvice.tips(r.copy(interferenceMs = null, switchCostMs = null)).isEmpty())
    assertTrue(ResultAdvice.tips(r.copy(interferenceMs = 100, switchCostMs = 0)).isEmpty())
  }
}
