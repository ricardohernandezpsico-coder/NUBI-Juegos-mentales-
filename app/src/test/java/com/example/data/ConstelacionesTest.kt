package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Constelaciones»: la lectura pura de lo que manda Unity (docs/diseno-constelaciones.md §5 y §7): la «memoria de lugar», las líneas de la pantalla final, el consejo, el récord que nunca baja y la escalera nueva. */
class ConstelacionesTest {
  @Test
  fun `la medida es el porcentaje de veces que fuiste directo a una pareja ya vista y se acota`() {
    assertEquals(70f, Constelaciones.mark(7, 10)!!, 1e-3f)
    assertEquals(100f, Constelaciones.mark(12, 10)!!, 1e-3f)
    assertEquals(0f, Constelaciones.mark(-3, 10)!!, 1e-3f)
    assertEquals(70, Constelaciones.percent(7, 10))
    assertEquals(33, Constelaciones.percent(1, 3))
  }

  @Test
  fun `sin oportunidades no hay medida y la partida no se guarda como medida`() {
    assertNull(Constelaciones.mark(0, 0))
    assertNull(Constelaciones.mark(null, 10))
    assertNull(Constelaciones.mark(3, null))
    assertNull(Constelaciones.percent(0, 0))
    assertEquals("Memoria de lugar: —", Constelaciones.headline(0, 0))
  }

  @Test
  fun `el titular y la frase de detalle dicen lo que se midio`() {
    assertEquals("Memoria de lugar: 70 %", Constelaciones.headline(7, 10))
    assertEquals("7 de 10 veces fuiste directo a una pareja que ya habías visto", Constelaciones.detailLine(7, 10))
    assertEquals("10 de 10 veces fuiste directo a una pareja que ya habías visto", Constelaciones.detailLine(14, 10))
    assertEquals("No hubo parejas vistas que recordar", Constelaciones.detailLine(0, 0))
    assertEquals(Constelaciones.detailLine(3, 4), Constelaciones.spoken(3, 4))
  }

  @Test
  fun `las lineas de la pantalla final`() {
    assertEquals("Parejas de memoria: 4 de 6", Constelaciones.memoryGroupsLine(4, 6))
    assertEquals("Parejas de memoria: 6 de 6", Constelaciones.memoryGroupsLine(9, 6))
    assertNull(Constelaciones.memoryGroupsLine(null, 6))
    assertNull(Constelaciones.memoryGroupsLine(2, 0))
    assertEquals("Racha de memoria más larga: 5", Constelaciones.streakLine(5))
    assertNull(Constelaciones.streakLine(0))
    assertNull(Constelaciones.streakLine(null))
  }

  @Test
  fun `la etapa mas alta es de 6 grupos`() {
    assertEquals("Etapa más alta: 1 de 6 (parejas)", Constelaciones.groupLine(1))
    assertEquals("Etapa más alta: 4 de 6 (tríos)", Constelaciones.groupLine(4))
    assertEquals("Etapa más alta: 6 de 6 (cielo grande)", Constelaciones.groupLine(6))
    assertNull(Constelaciones.groupLine(0))
    assertNull(Constelaciones.groupLine(7))
    assertNull(Constelaciones.groupLine(null))
  }

  @Test
  fun `el record dice nueva mejor racha solo cuando Unity avisa que se supero`() {
    assertEquals("Tu mejor racha: 6", Constelaciones.recordLine(6, false))
    assertEquals("Tu mejor racha: 6", Constelaciones.recordLine(6, null))
    assertEquals("¡Nueva mejor racha! 7", Constelaciones.recordLine(7, true))
    assertNull(Constelaciones.recordLine(0, true))
    assertNull(Constelaciones.recordLine(null, null))
  }

  @Test
  fun `el consejo solo sale si alguna vez se te escapo una pareja ya vista`() {
    assertEquals("Truco: da vuelta primero una luz nueva.", Constelaciones.tip(6, 10))
    assertNull(Constelaciones.tip(10, 10))
    assertNull(Constelaciones.tip(0, 0))
    assertNull(Constelaciones.tip(null, null))
  }

  @Test
  fun `el record nunca baja ni es negativo`() {
    assertEquals(8, Constelaciones.mergeRecord(8, 5))
    assertEquals(9, Constelaciones.mergeRecord(8, 9))
    assertEquals(8, Constelaciones.mergeRecord(8, null))
    assertEquals(0, Constelaciones.mergeRecord(-4, -2))
  }

  @Test
  fun `el rating guardado de la escalera vieja de 10 pasa a una etapa equivalente en la de 18`() {
    assertEquals(-1f, Constelaciones.translateOldRating(-1f), 0f)      // sin dato se queda sin dato
    assertEquals(0f, Constelaciones.translateOldRating(0f), 1e-6f)
    // nivel viejo 10 (rating 0,9) = etapa 9 de la nueva (rating 8/18)
    assertEquals(8f / 18f, Constelaciones.translateOldRating(0.9f), 1e-3f)
    assertEquals(1f, Constelaciones.translateOldRating(2.5f), 1e-6f)
    for (r in listOf(0.1f, 0.3f, 0.5f, 0.7f, 0.9f)) assertTrue(Constelaciones.translateOldRating(r) < r)
    assertEquals(40f / 81f, Constelaciones.OLD_TO_NEW_RATIO, 1e-6f)
  }

  @Test
  fun `la escalera de Constelaciones tiene 18 etapas y el juego esta en Memoria con su nombre nuevo`() {
    assertEquals(18, Skill.ladder("parejas").levels)
    val g = com.example.model.GameRegistry.getById("parejas")
    assertNotNull(g)
    assertEquals("Constelaciones", g!!.title)
    assertEquals("MEMORIA", g.domain.name)
    assertFalse(com.example.model.GameRegistry.isRetired("parejas"))
    assertTrue("parejas" in com.example.bridge.UnityGameLauncher.TUTORIAL_GAMES)
    assertTrue(com.example.bridge.UnityGameLauncher.shouldShowTutorial("parejas", emptyList()))
    assertEquals("Constelaciones", StarMeasures.gameNames["parejas"])
    assertEquals("parejas", StarMeasures.def("place")!!.gameId)
  }
}
