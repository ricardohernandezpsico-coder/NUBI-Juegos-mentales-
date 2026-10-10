package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Acoplamiento: muelle de acoplamiento»: lo que dice la pantalla final (módulos, anillos, récord y totales) y el récord que nunca baja. */
class AcoplamientoTest {

  @Test
  fun `los modulos acoplados se dicen en singular y plural`() {
    assertEquals("24 módulos acoplados", Acoplamiento.dockedLine(24))
    assertEquals("1 módulo acoplado", Acoplamiento.dockedLine(1))
    assertEquals("Ningún módulo acoplado", Acoplamiento.dockedLine(0))
    assertNull(Acoplamiento.dockedLine(null))
    assertNull(Acoplamiento.dockedLine(-1))
  }

  @Test
  fun `los anillos completos solo se dicen si hubo alguno`() {
    assertEquals("Anillos completos: 3", Acoplamiento.ringsLine(3))
    assertEquals("Anillos completos: 1", Acoplamiento.ringsLine(1))
    assertNull(Acoplamiento.ringsLine(0))
    assertNull(Acoplamiento.ringsLine(null))
  }

  @Test
  fun `el record dice record nuevo solo cuando la partida lo supero`() {
    assertEquals("Tu récord: 31 módulos en una partida", Acoplamiento.recordLine(31, false))
    assertEquals("Tu récord: 31 módulos en una partida", Acoplamiento.recordLine(31, null))
    assertEquals("¡Récord nuevo: 31 módulos!", Acoplamiento.recordLine(31, true))
    assertEquals("¡Récord nuevo: 1 módulo!", Acoplamiento.recordLine(1, true))
    assertEquals("Tu récord: 1 módulo en una partida", Acoplamiento.recordLine(1, false))
    assertNull(Acoplamiento.recordLine(0, true))
    assertNull(Acoplamiento.recordLine(null, null))
  }

  @Test
  fun `el record nunca baja ni es negativo`() {
    assertEquals(24, Acoplamiento.mergeRecord(0, 24))
    assertEquals(30, Acoplamiento.mergeRecord(30, 24))
    assertEquals(30, Acoplamiento.mergeRecord(30, null))
    assertEquals(0, Acoplamiento.mergeRecord(-3, -5))
  }

  @Test
  fun `los totales suman partida a partida y nunca bajan`() {
    val one = Acoplamiento.addTotals(Acoplamiento.Totals(), 17, 2)
    assertEquals(Acoplamiento.Totals(17, 2), one)
    assertEquals(Acoplamiento.Totals(40, 5), Acoplamiento.addTotals(one, 23, 3))
    assertEquals("un dato que no vino suma 0", Acoplamiento.Totals(17, 2), Acoplamiento.addTotals(one, null, null))
    assertEquals("nada negativo", Acoplamiento.Totals(17, 2), Acoplamiento.addTotals(one, -4, -1))
    assertEquals(Acoplamiento.Totals(0, 0), Acoplamiento.addTotals(Acoplamiento.Totals(-5, -1), 0, 0))
  }

  @Test
  fun `la linea del total`() {
    assertEquals("Has acoplado 342 módulos · 12 anillos", Acoplamiento.totalLine(Acoplamiento.Totals(342, 12)))
    assertEquals("Has acoplado 1 módulo", Acoplamiento.totalLine(Acoplamiento.Totals(1, 0)))
    assertEquals("Has acoplado 9 módulos", Acoplamiento.totalLine(Acoplamiento.Totals(9, 0)))
    assertEquals("Has acoplado 8 módulos · 1 anillo", Acoplamiento.totalLine(Acoplamiento.Totals(8, 1)))
    assertNull(Acoplamiento.totalLine(Acoplamiento.Totals()))
    assertNull(Acoplamiento.totalLine(null))
  }

  @Test
  fun `el texto para lectores de pantalla junta lo que se ve`() {
    assertEquals("17 módulos acoplados. Anillos completos: 2. ¡Récord nuevo: 17 módulos!", Acoplamiento.spoken(17, 2, 17, true))
    assertEquals("5 módulos acoplados", Acoplamiento.spoken(5, 0, null, null))
  }

  @Test
  fun `los casilleros por anillo son los de Unity`() {
    assertEquals(8, Acoplamiento.SLOTS)
  }

  @Test
  fun `ningun texto usa palabras vedadas`() {
    val all = listOf(
      Acoplamiento.dockedLine(5), Acoplamiento.dockedLine(0), Acoplamiento.ringsLine(2), Acoplamiento.recordLine(9, true), Acoplamiento.recordLine(9, false),
      Acoplamiento.totalLine(Acoplamiento.Totals(30, 3)), Acoplamiento.spoken(3, 1, 3, true)
    ).filterNotNull()
    for (text in all) for (bad in listOf("cognitiv", "entrenamiento", "cerebro", "combustible")) {
      assertTrue("«$text» dice «$bad»", !text.lowercase().contains(bad))
    }
  }
}
