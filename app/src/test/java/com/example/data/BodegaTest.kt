package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Bodega de carga»: la lectura pura de lo que manda Unity (docs/diseno-bodega-de-carga.md §7): la medida de «primer intento», las líneas de la pantalla final, el consejo y el récord que nunca baja. */
class BodegaTest {
  @Test
  fun `la medida es el porcentaje de objetos al primer intento y se acota`() {
    assertEquals(70f, Bodega.mark(7, 10)!!, 1e-3f)
    assertEquals(100f, Bodega.mark(12, 10)!!, 1e-3f)
    assertEquals(0f, Bodega.mark(-3, 10)!!, 1e-3f)
    assertNull(Bodega.mark(null, 10))
    assertNull(Bodega.mark(3, 0))
    assertEquals(70, Bodega.percent(7, 10))
    assertNull(Bodega.percent(3, null))
  }

  @Test
  fun `el titular dice cuantos se encontraron al primer intento`() {
    assertEquals("Encontraste 7 de 10 objetos al primer intento", Bodega.headline(7, 10))
    assertEquals("Encontraste 10 de 10 objetos al primer intento", Bodega.headline(14, 10))
    assertNull(Bodega.headline(0, 0))
    assertEquals("Sin medida", Bodega.spoken(null, null))
    assertEquals("Encontraste 3 de 4 objetos al primer intento", Bodega.spoken(3, 4))
  }

  @Test
  fun `la etapa mas alta es de 5 grupos`() {
    assertEquals("Etapa más alta: 1 de 5 (pocos objetos)", Bodega.groupLine(1))
    assertEquals("Etapa más alta: 3 de 5 (cajas que se mueven)", Bodega.groupLine(3))
    assertEquals("Etapa más alta: 4 de 5 (la bodega gira)", Bodega.groupLine(4))
    assertEquals("Etapa más alta: 5 de 5 (todo junto)", Bodega.groupLine(5))
    assertNull(Bodega.groupLine(0))
    assertNull(Bodega.groupLine(6))
    assertNull(Bodega.groupLine(null))
  }

  @Test
  fun `la racha y el ritmo solo se muestran con datos suficientes`() {
    assertEquals("Tu racha más larga: 5 seguidos al primer intento", Bodega.streakLine(5, 10))
    assertNull("una racha de 1 no es una racha", Bodega.streakLine(1, 10))
    assertNull("con pocos objetos no se sacan conclusiones", Bodega.streakLine(3, 2))
    assertEquals("Tu ritmo: 4,5 s por objeto", Bodega.paceLine(4500, 8))
    assertNull(Bodega.paceLine(4500, 2))
    assertNull(Bodega.paceLine(999_999, 8))
    assertNull(Bodega.paceLine(null, 8))
  }

  @Test
  fun `la bodega mas grande de hoy y el record`() {
    assertEquals("Tu bodega más grande hoy: 5 objetos sin errores", Bodega.biggestLine(5))
    assertEquals("Tu bodega más grande hoy: 1 objeto sin errores", Bodega.biggestLine(1))
    assertNull("sin ningún pedido perfecto no se dice nada", Bodega.biggestLine(0))
    assertNull(Bodega.biggestLine(null))
    assertEquals("Tu récord: 6 objetos", Bodega.recordLine(6, false))
    assertEquals("Tu récord: 6 objetos", Bodega.recordLine(6, null))
    assertEquals("¡Nuevo récord! 6 objetos", Bodega.recordLine(6, true))
    assertNull(Bodega.recordLine(0, true))
    assertNull(Bodega.recordLine(null, null))
  }

  @Test
  fun `el record guardado nunca baja ni es negativo`() {
    assertEquals(5, Bodega.mergeRecord(3, 5))
    assertEquals(5, Bodega.mergeRecord(5, 3))
    assertEquals(5, Bodega.mergeRecord(5, null))
    assertEquals(0, Bodega.mergeRecord(-2, -9))
    assertEquals(4, Bodega.mergeRecord(0, 4))
  }

  @Test
  fun `el consejo aparece solo si algun objeto no se encontro a la primera, y no habla de enfermedades`() {
    val tip = Bodega.tip(7, 10)!!
    assertTrue(tip.startsWith("Truco: "))
    assertTrue(tip.contains("escotilla"))
    assertNull(Bodega.tip(10, 10))
    assertNull(Bodega.tip(null, 10))
    assertNull(Bodega.tip(3, 0))
    for (banned in listOf("alzheimer", "demencia", "previene", "diagn")) assertFalse(tip.lowercase().contains(banned))
  }
}
