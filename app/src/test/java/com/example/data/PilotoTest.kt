package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Piloto Estelar: la ruta de las balizas»: lo que dice la pantalla final, la medida «Tus señales a los mandos» y que ya no existe el costo de multitarea. */
class PilotoTest {

  @Test
  fun `la medida solo existe si Unity la manda y esta entre 0 y 100`() {
    assertEquals(71f, Piloto.mark(71)!!, 0f)
    assertEquals(0f, Piloto.mark(0)!!, 0f)
    assertEquals(100f, Piloto.mark(100)!!, 0f)
    assertNull(Piloto.mark(-1))
    assertNull(Piloto.mark(null))
    assertNull(Piloto.mark(140))
    assertEquals("Tus señales a los mandos: 71 %", Piloto.measureLine(71))
    assertNull(Piloto.measureLine(-1))
  }

  @Test
  fun `las lineas del final se dicen con las palabras del diseno`() {
    assertEquals("En la ruta: 82 %", Piloto.laneLine(82))
    assertEquals("Señales de tu misión: 14 de 18", Piloto.missionLine(14, 18))
    assertEquals("Señales de tu misión: 3 de 3", Piloto.missionLine(9, 3))
    assertEquals("Toques equivocados: 2", Piloto.wrongLine(2))
    assertEquals("Toques equivocados: 0", Piloto.wrongLine(0))
    assertEquals("Tu nivel de señales: 6 de 9", Piloto.levelLine(6))
    assertEquals("Racha mayor ×7", Piloto.streakLine(7))
    assertNull(Piloto.streakLine(1))
    assertNull(Piloto.laneLine(null))
    assertNull(Piloto.missionLine(null, 4))
    assertNull(Piloto.levelLine(0))
  }

  @Test
  fun `los puntos usan el punto de miles`() {
    assertEquals("1.250 puntos", Piloto.pointsLine(1250))
    assertEquals("40 puntos", Piloto.pointsLine(40))
    assertEquals("1 punto", Piloto.pointsLine(1))
    assertNull(Piloto.pointsLine(-1))
  }

  @Test
  fun `el consejo es concreto y solo con suficientes senales de la mision`() {
    assertNull(Piloto.advice(5, 7, 4, 40))
    assertTrue(Piloto.advice(9, 12, 4, 90)!!.contains("detalle"))
    assertTrue(Piloto.advice(10, 12, 0, 55)!!.contains("ruta"))
    assertTrue(Piloto.advice(5, 12, 1, 90)!!.contains("tarjeta de misión"))
    assertNull(Piloto.advice(11, 12, 1, 90))
    assertNull(Piloto.advice(null, 12, 1, 90))
  }

  @Test
  fun `ningun texto promete salud ni usa las palabras prohibidas`() {
    val texts = listOf(Piloto.MEASURE_EXPLANATION, Piloto.TOO_FEW_LINE) +
      listOfNotNull(
        Piloto.advice(9, 12, 4, 90), Piloto.advice(10, 12, 0, 55), Piloto.advice(5, 12, 1, 90), Piloto.measureLine(71),
        Piloto.laneLine(80), Piloto.missionLine(5, 8), Piloto.wrongLine(2), Piloto.levelLine(5), Piloto.streakLine(4), Piloto.pointsLine(900)
      )
    for (t in texts) for (banned in listOf("cognitiv", "entrenamiento", "cerebro", "memoria", "demencia", "prevenir"))
      assertFalse("«$t» dice «$banned»", t.lowercase().contains(banned))
    assertNotNull(Piloto.spoken(71, 82, 14, 18))
  }

  @Test
  fun `no hay medida de multitarea ni piloto automatico en el modelo ni en las medidas`() {
    val fields = com.example.model.GamePlayResult::class.java.declaredFields.map { it.name.lowercase() }
    assertFalse(fields.any { it.contains("multitask") })
    assertTrue(StarMeasures.defs.none { it.key == "multitask" })
    val mandos = StarMeasures.defs.single { it.gameId == "piloto" }
    assertEquals("mandos", mandos.key)
    assertFalse(mandos.lowerIsBetter)
  }
}
