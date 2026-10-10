package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Aterrizaje Lunar renovado» (10-oct): lo que dice el final con sentido (la estimación «de cada 100», tu avance solo contigo, la base lunar) y las cúpulas que nunca bajan. */
class AterrizajeTest {

  @Test
  fun `la estimacion es de cada 100, redondeada y nunca menos de 1`() {
    assertEquals(11, Aterrizaje.estimate(11.2f))
    assertEquals(12, Aterrizaje.estimate(11.5f))
    assertEquals("un error casi cero igual se dice a 1 de cada 100", 1, Aterrizaje.estimate(0.2f))
    assertEquals(1, Aterrizaje.estimate(0f))
    assertNull(Aterrizaje.estimate(null))
    assertNull(Aterrizaje.estimate(-3f))
    assertNull(Aterrizaje.estimate(Float.NaN))
    assertEquals("a 11 de cada 100", Aterrizaje.headline(11))
    assertEquals("—", Aterrizaje.headline(null))
  }

  @Test
  fun `el titulo cuenta los aterrizajes justos en singular y plural`() {
    assertEquals("12 aterrizajes justos de 15", Aterrizaje.title(12, 15))
    assertEquals("1 aterrizaje justo de 8", Aterrizaje.title(1, 8))
    assertEquals("0 aterrizajes justos de 6", Aterrizaje.title(0, 6))
  }

  @Test
  fun `la linea de lo que paso nombra dianas, racha y cupulas`() {
    assertEquals("2 dianas lunares · racha mayor ×4 · 1 cúpula", Aterrizaje.summaryLine(2, 4, 1))
    assertEquals("1 diana lunar · racha mayor — · 0 cúpulas", Aterrizaje.summaryLine(1, 1, 0))
    assertEquals("la racha se nombra desde 2", "0 dianas lunares · racha mayor ×2 · 3 cúpulas", Aterrizaje.summaryLine(0, 2, 3))
    assertNull("una partida sin esos datos no inventa la línea", Aterrizaje.summaryLine(null, null, null))
  }

  @Test
  fun `la primera partida no tiene con que compararse`() {
    val p = Aterrizaje.progress(11.2f, emptyList())
    assertEquals(11, p.today)
    assertNull(p.average)
    assertNull(p.best)
    assertEquals("Juega otra vez para ver tu avance.", p.phrase)
    assertEquals("—", Aterrizaje.chipValue(p.average))
    assertEquals("11 de 100", Aterrizaje.chipValue(p.today))
  }

  @Test
  fun `mejor que tu mejor anterior es la partida mas precisa`() {
    val p = Aterrizaje.progress(6f, listOf(10f, 12f, 8f))
    assertEquals("¡Tu partida más precisa hasta ahora!", p.phrase)
    assertEquals(6, p.today)
    assertEquals("el promedio sale de las ANTERIORES", 10, p.average)
    assertEquals("tu mejor incluye hoy", 6, p.best)
  }

  @Test
  fun `mas cerca, mas lejos o igual que tu promedio`() {
    val prev = listOf(10f, 12f, 8f)                       // promedio 10, mejor 8
    assertEquals("Hoy quedaste más cerca que tu promedio.", Aterrizaje.progress(9f, prev).phrase)
    assertEquals("Un poco más lejos que tu promedio: es normal que varíe.", Aterrizaje.progress(11f, prev).phrase)
    assertEquals("Igual que tu promedio: vas parejo.", Aterrizaje.progress(10.3f, prev).phrase)
    assertEquals("un tramo de 0,5 alrededor del promedio cuenta como parejo", "Igual que tu promedio: vas parejo.", Aterrizaje.progress(9.6f, prev).phrase)
    // sin mejorar la mejor anterior, tu mejor sigue siendo la de antes
    assertEquals(8, Aterrizaje.progress(11f, prev).best)
  }

  @Test
  fun `la comparacion es solo contigo y nunca habla de otras personas ni percentiles`() {
    val all = listOf(
      Aterrizaje.progress(9f, listOf(10f)).phrase, Aterrizaje.progress(11f, listOf(10f)).phrase, Aterrizaje.progress(10f, listOf(10f)).phrase,
      Aterrizaje.progress(5f, listOf(10f)).phrase, Aterrizaje.progress(5f, emptyList()).phrase,
      Aterrizaje.WHY, Aterrizaje.SOURCE, Aterrizaje.NOTE, Aterrizaje.BOX_TITLE, Aterrizaje.PROGRESS_TITLE, Aterrizaje.TRICK_TITLE
    )
    for (t in all) {
      assertTrue(t, !t.contains("percentil", true) && !t.contains("otras personas", true) && !t.contains("la mayoría", true))
      assertTrue(t, !t.contains("cerebro", true) && !t.contains("cognitiv", true) && !t.contains("entrenamiento", true))
    }
    assertEquals("Fuente: Schley y Peters, 2014", Aterrizaje.SOURCE)
    assertEquals("Medida de esta partida. No es un diagnóstico.", Aterrizaje.NOTE)
  }

  @Test
  fun `las partidas anteriores salen de las medidas guardadas sin contar la de hoy`() {
    val today = MeasurePoint(timestamp = 500L, key = "numline", value = 7f, rating = 0.5f, timed = true)
    val saved = listOf(
      MeasurePoint(100L, "numline", 12f, 0.5f, true),
      MeasurePoint(200L, "numline", 9f, 0.5f, true),
      MeasurePoint(300L, "numline", 30f, 0.5f, false),      // otro reloj: no se compara
      MeasurePoint(400L, "rotation", 200f, 0.5f, true),     // otra medida
      MeasurePoint(500L, "numline", 7f, 0.5f, true)         // la de hoy, ya guardada: no se cuenta dos veces
    )
    assertEquals(listOf(12f, 9f), Aterrizaje.previousErrors(saved, today))
    // si la de hoy todavía no se guardó, da lo mismo
    assertEquals(listOf(12f, 9f), Aterrizaje.previousErrors(saved.dropLast(1), today))
    // sin nada guardado: ninguna anterior
    assertEquals(emptyList<Float>(), Aterrizaje.previousErrors(emptyList(), today))
  }

  @Test
  fun `solo cuentan las ultimas diez partidas anteriores`() {
    val today = MeasurePoint(timestamp = 10_000L, key = "numline", value = 5f, rating = -1f, timed = null)
    val saved = (1..15).map { MeasurePoint(it * 100L, "numline", it.toFloat(), -1f, null) }
    val prev = Aterrizaje.previousErrors(saved, today)
    assertEquals(Aterrizaje.PREVIOUS_MAX, prev.size)
    assertEquals((6..15).map { it.toFloat() }, prev)
  }

  @Test
  fun `las cupulas de la base nunca bajan ni son negativas`() {
    assertEquals(Aterrizaje.Totals(3, 3), Aterrizaje.addTotals(Aterrizaje.Totals(), 3))
    assertEquals(Aterrizaje.Totals(5, 3), Aterrizaje.addTotals(Aterrizaje.Totals(3, 3), 2))
    assertEquals("el mejor no baja con una partida peor", Aterrizaje.Totals(6, 3), Aterrizaje.addTotals(Aterrizaje.Totals(5, 3), 1))
    assertEquals("un dato que no vino suma 0", Aterrizaje.Totals(4, 2), Aterrizaje.addTotals(Aterrizaje.Totals(4, 2), null))
    assertEquals("nunca baja por un dato negativo", Aterrizaje.Totals(4, 2), Aterrizaje.addTotals(Aterrizaje.Totals(4, 2), -5))
    assertEquals("ni parte de un guardado negativo", Aterrizaje.Totals(1, 1), Aterrizaje.addTotals(Aterrizaje.Totals(-7, -2), 1))
  }

  @Test
  fun `la base lunar solo se nombra si hay cupulas`() {
    assertNull(Aterrizaje.baseLine(null))
    assertNull(Aterrizaje.baseLine(Aterrizaje.Totals()))
    assertEquals("Tu base lunar: 1 cúpula", Aterrizaje.baseLine(Aterrizaje.Totals(1, 1)))
    assertEquals("Tu base lunar: 12 cúpulas", Aterrizaje.baseLine(Aterrizaje.Totals(12, 3)))
  }

  @Test
  fun `el texto para lectores de pantalla junta lo importante`() {
    val spoken = Aterrizaje.spoken("12 aterrizajes justos de 15", "a 11 de cada 100", "Te costó más el final de la regla", "Hoy quedaste más cerca que tu promedio.")
    assertEquals(
      "12 aterrizajes justos de 15. Tu estimación de hoy, a 11 de cada 100 del lugar justo. Te costó más el final de la regla. Hoy quedaste más cerca que tu promedio.",
      spoken
    )
    assertTrue("sin la línea de datos no queda un hueco", !Aterrizaje.spoken("t", "a 5 de cada 100", null, "f").contains(". ."))
  }
}
