package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Rescate relámpago: qué cápsulas viste»: lo que dice la pantalla final y el récord que nunca baja. Sin medidas por lugar. */
class RescateTest {

  @Test
  fun `las capsulas a salvo se dicen en singular y en plural`() {
    assertEquals("23 cápsulas a salvo", Rescate.rescuedLine(23))
    assertEquals("1 cápsula a salvo", Rescate.rescuedLine(1))
    assertEquals("No se rescató ninguna cápsula", Rescate.rescuedLine(0))
    assertNull(Rescate.rescuedLine(null))
    assertNull(Rescate.rescuedLine(-1))
  }

  @Test
  fun `tu captura usa coma decimal y no inventa lo que no hubo`() {
    assertEquals("Tu captura: 3,5 de 4", Rescate.captureLine(3.5f))
    assertEquals("Tu captura: 0,0 de 4", Rescate.captureLine(0f))
    assertEquals("Tu captura: 4,0 de 4", Rescate.captureLine(4f))
    assertEquals("Tu captura: 4,0 de 4", Rescate.captureLine(4.7f))       // nunca pasa del techo
    assertNull(Rescate.captureLine(null))
    assertNull(Rescate.captureLine(-1f))
    assertEquals(4, Rescate.CAPTURE_MAX)
  }

  @Test
  fun `tu vistazo se dice en milisegundos reales`() {
    assertEquals("Tu vistazo: 280 ms", Rescate.glanceLine(280))
    assertNull(Rescate.glanceLine(null))
    assertNull(Rescate.glanceLine(0))
    assertNull(Rescate.glanceLine(-1))
    assertEquals("Destello más corto resuelto: 150 ms", Rescate.shortestLine(150))
    assertNull(Rescate.shortestLine(null))
    assertNull(Rescate.shortestLine(-1))
  }

  @Test
  fun `la explicacion del vistazo dice cuantas capsulas a la vez solo si se sabe`() {
    assertTrue(Rescate.glanceExplanation(3.5f).contains("con 3,5 cápsulas a la vez"))
    assertTrue(Rescate.glanceExplanation(4f).contains("con 4 cápsulas a la vez"))
    assertFalse(Rescate.glanceExplanation(null).contains("a la vez"))
    assertFalse(Rescate.glanceExplanation(-1f).contains("a la vez"))
  }

  @Test
  fun `las rondas perfectas y la racha`() {
    assertEquals("Rondas perfectas: 5 de 12 · racha mayor ×3", Rescate.perfectLine(5, 12, 3))
    assertEquals("Rondas perfectas: 3 de 12", Rescate.perfectLine(3, 12, 1))
    assertEquals("Rondas perfectas: 4 de 4", Rescate.perfectLine(9, 4, null))
    assertNull(Rescate.perfectLine(null, 12, 2))
    assertNull(Rescate.perfectLine(2, 0, 2))
  }

  @Test
  fun `el record dice nuevo solo cuando se supero`() {
    assertEquals("Tu récord: 31 cápsulas en una partida", Rescate.recordLine(31, false))
    assertEquals("Tu récord: 31 cápsulas en una partida", Rescate.recordLine(31, null))
    assertEquals("¡Récord nuevo: 31 cápsulas!", Rescate.recordLine(31, true))
    assertEquals("¡Récord nuevo: 1 cápsula!", Rescate.recordLine(1, true))
    assertNull(Rescate.recordLine(0, true))
    assertNull(Rescate.recordLine(null, null))
  }

  @Test
  fun `el record nunca baja ni es negativo`() {
    assertEquals(24, Rescate.mergeRecord(0, 24))
    assertEquals(30, Rescate.mergeRecord(30, 24))
    assertEquals(30, Rescate.mergeRecord(30, null))
    assertEquals(0, Rescate.mergeRecord(-3, -5))
  }

  @Test
  fun `se dibujan hasta cuarenta capsulas en filas de ocho`() {
    assertEquals(0, Rescate.dots(null))
    assertEquals(0, Rescate.dots(-4))
    assertEquals(7, Rescate.dots(7))
    assertEquals(Rescate.MAX_DOTS, Rescate.dots(500))
    assertEquals(0, Rescate.MAX_DOTS % Rescate.ROW)
  }

  @Test
  fun `los viajes a la estacion se dicen solo si hubo`() {
    assertEquals("Viajes a la estación: 3", Rescate.tripsLine(3))
    assertEquals("Viajes a la estación: 1", Rescate.tripsLine(1))
    assertNull(Rescate.tripsLine(0))
    assertNull(Rescate.tripsLine(null))
    assertNull(Rescate.tripsLine(-2))
  }

  @Test
  fun `el en total dice las capsulas y los viajes en singular o plural`() {
    assertEquals("En total: 342 cápsulas y 12 viajes a la estación", Rescate.totalLine(Rescate.Totals(342, 12)))
    assertEquals("En total: 1 cápsula", Rescate.totalLine(Rescate.Totals(1, 0)))
    assertEquals("En total: 11 cápsulas y 1 viaje a la estación", Rescate.totalLine(Rescate.Totals(11, 1)))
    assertEquals("En total: 8 cápsulas", Rescate.totalLine(Rescate.Totals(8, 0)))
    assertNull(Rescate.totalLine(Rescate.Totals(0, 0)))
    assertNull(Rescate.totalLine(null))
  }

  @Test
  fun `los totales se suman partida a partida y nunca bajan`() {
    var t = Rescate.Totals()
    t = Rescate.addTotals(t, 23, 2)
    assertEquals(Rescate.Totals(23, 2), t)
    t = Rescate.addTotals(t, 7, 0)
    assertEquals(Rescate.Totals(30, 2), t)
    assertEquals(t, Rescate.addTotals(t, null, null))            // un dato que no vino suma 0
    assertEquals(t, Rescate.addTotals(t, -5, -1))                // y nada negativo resta
    assertEquals(Rescate.Totals(0, 0), Rescate.addTotals(Rescate.Totals(-3, -1), null, null))
  }

  @Test
  fun `la lectura para lector de pantalla junta lo que hay`() {
    assertEquals("9 cápsulas a salvo. Tu captura: 3,5 de 4. Tu vistazo: 280 ms", Rescate.spoken(9, 3.5f, 280))
    assertEquals("9 cápsulas a salvo", Rescate.spoken(9, null, null))
  }

  @Test
  fun `ningun texto usa las palabras vedadas ni habla de lugares`() {
    val texts = listOf(
      Rescate.rescuedLine(5), Rescate.rescuedLine(1), Rescate.rescuedLine(0), Rescate.captureLine(2f), Rescate.glanceLine(300),
      Rescate.glanceExplanation(3f), Rescate.glanceExplanation(null), Rescate.CAPTURE_EXPLANATION, Rescate.shortestLine(200),
      Rescate.perfectLine(3, 12, 2), Rescate.recordLine(7, true), Rescate.recordLine(7, false), Rescate.spoken(3, 2f, 300),
      Rescate.tripsLine(2), Rescate.totalLine(Rescate.Totals(20, 2)), Rescate.totalLine(Rescate.Totals(20, 0))
    )
    for (t in texts) {
      val low = t!!.lowercase()
      for (bad in listOf("cognitiv", "entrenamiento", "cerebro", "diagnóstico", "tu radar", "tu filtro", "adultos")) assertTrue("«$t» usa «$bad»", !low.contains(bad))
    }
  }
}
