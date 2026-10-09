package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Satélites: enciende tu planeta»: lo que dice la pantalla final, el récord que nunca baja y cómo se lleva el rating del juego viejo al nuevo. */
class SatelitesTest {

  @Test
  fun `las luces se dicen en singular y en plural`() {
    assertEquals("Encendiste 12 luces", Satelites.lightsLine(12))
    assertEquals("Encendiste 1 luz", Satelites.lightsLine(1))
    assertEquals("No se encendió ninguna luz", Satelites.lightsLine(0))
    assertNull(Satelites.lightsLine(null))
    assertNull(Satelites.lightsLine(-1))
  }

  @Test
  fun `tu seguimiento usa coma decimal y no inventa lo que no hubo`() {
    assertEquals("Tu seguimiento: 2,6 de 3,5 a la vez", Satelites.trackingLine(2.6f, 3.5f))
    assertEquals("Tu seguimiento: 3,0 de 3 a la vez", Satelites.trackingLine(3f, 3f))
    assertEquals("Tu seguimiento: 2,0 a la vez", Satelites.trackingLine(2f, null))
    assertNull(Satelites.trackingLine(null, 3f))
    assertNull(Satelites.trackingLine(-1f, 3f))
  }

  @Test
  fun `las rondas perfectas y la racha`() {
    assertEquals("Rondas perfectas: 5 de 8 · racha mayor ×3", Satelites.perfectLine(5, 8, 3))
    assertEquals("Rondas perfectas: 3 de 8", Satelites.perfectLine(3, 8, 1))
    assertEquals("Rondas perfectas: 8 de 8 · racha mayor ×8", Satelites.perfectLine(8, 8, 8))
    assertEquals("Rondas perfectas: 4 de 4", Satelites.perfectLine(9, 4, null))
    assertNull(Satelites.perfectLine(null, 8, 2))
    assertNull(Satelites.perfectLine(2, 0, 2))
  }

  @Test
  fun `el record dice nuevo solo cuando se supero`() {
    assertEquals("Tu récord: 22 luces en una partida", Satelites.recordLine(22, false))
    assertEquals("Tu récord: 22 luces en una partida", Satelites.recordLine(22, null))
    assertEquals("¡Récord nuevo: 22 luces!", Satelites.recordLine(22, true))
    assertNull(Satelites.recordLine(0, true))
    assertNull(Satelites.recordLine(null, null))
  }

  @Test
  fun `el record nunca baja ni es negativo`() {
    assertEquals(24, Satelites.mergeRecord(0, 24))
    assertEquals(30, Satelites.mergeRecord(30, 24))
    assertEquals(30, Satelites.mergeRecord(30, null))
    assertEquals(0, Satelites.mergeRecord(-3, -5))
  }

  @Test
  fun `el rating del juego viejo se lleva a las tres cuartas partes`() {
    assertEquals(-1f, Satelites.translateOldRating(-1f), 0f)      // sin dato se queda sin dato
    assertEquals(0f, Satelites.translateOldRating(0f), 1e-6f)
    assertEquals(0.675f, Satelites.translateOldRating(0.9f), 1e-4f)
    assertEquals(1f, Satelites.translateOldRating(2.5f), 1e-6f)
    for (r in listOf(0.1f, 0.3f, 0.5f, 0.7f, 0.9f)) assertTrue(Satelites.translateOldRating(r) < r)
    // el mejor del juego viejo (0,9) cae en el nivel 9 de 12 y el del medio (0,5) en el 5: ninguno parte en un nivel que no vio
    assertEquals(9, 1 + (Satelites.translateOldRating(0.9f) * 12f).toInt())
    assertEquals(5, 1 + (Satelites.translateOldRating(0.5f) * 12f).toInt())
  }

  @Test
  fun `se dibujan hasta cuarenta luces`() {
    assertEquals(0, Satelites.dots(null))
    assertEquals(7, Satelites.dots(7))
    assertEquals(Satelites.MAX_LIGHT_DOTS, Satelites.dots(500))
  }

  @Test
  fun `la lectura para lector de pantalla junta las luces y el seguimiento`() {
    assertEquals("Encendiste 9 luces. Tu seguimiento: 2,6 de 3,5 a la vez", Satelites.spoken(9, 2.6f, 3.5f))
    assertEquals("Encendiste 9 luces", Satelites.spoken(9, null, null))
  }

  @Test
  fun `ningun texto usa las palabras vedadas`() {
    val texts = listOf(
      Satelites.lightsLine(5), Satelites.lightsLine(1), Satelites.lightsLine(0), Satelites.trackingLine(2f, 3f),
      Satelites.perfectLine(3, 8, 2), Satelites.recordLine(7, true), Satelites.recordLine(7, false), Satelites.spoken(3, 2f, 3f)
    )
    for (t in texts) for (bad in listOf("cognitiv", "entrenamiento", "cerebro", "diagnóstico")) assertTrue("«$t» usa «$bad»", !t!!.lowercase().contains(bad))
  }
}
