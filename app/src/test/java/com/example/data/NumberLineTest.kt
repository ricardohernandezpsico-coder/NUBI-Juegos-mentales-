package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class NumberLineTest {
  @Test
  fun `sin aterrizajes suficientes no hay lectura`() {
    assertEquals(NumberLineReading.SIN_DATOS, NumberLine.reading(emptyList(), emptyList()))
    // Un aterrizaje por tramo no alcanza para comparar tramos.
    assertEquals(NumberLineReading.SIN_DATOS, NumberLine.reading(listOf(0.1f, 0.5f, 0.9f), listOf(0.2f, 0.5f, 0.8f)))
  }

  @Test
  fun `aterrizajes parejos no nombran un tramo`() {
    val t = listOf(0.1f, 0.2f, 0.45f, 0.55f, 0.8f, 0.9f)
    assertEquals(NumberLineReading.PAREJA, NumberLine.reading(t, t))
    // Todos a 2 puntos del blanco, a un lado o al otro: parejo (el signo no importa).
    assertEquals(NumberLineReading.PAREJA, NumberLine.reading(t, listOf(0.12f, 0.18f, 0.47f, 0.53f, 0.78f, 0.92f)))
  }

  @Test
  fun `nombra el tramo donde mas se aleja del blanco`() {
    val t = listOf(0.1f, 0.2f, 0.45f, 0.55f, 0.8f, 0.9f)
    assertEquals(NumberLineReading.CENTRO, NumberLine.reading(t, listOf(0.11f, 0.19f, 0.37f, 0.63f, 0.81f, 0.89f)))
    assertEquals(NumberLineReading.FINAL, NumberLine.reading(t, listOf(0.11f, 0.19f, 0.46f, 0.54f, 0.72f, 0.98f)))
    assertEquals(NumberLineReading.INICIO, NumberLine.reading(t, listOf(0.18f, 0.12f, 0.46f, 0.54f, 0.81f, 0.89f)))
  }

  @Test
  fun `el error por tramo es sin signo`() {
    val errs = NumberLine.zoneErrors(listOf(0.1f, 0.2f, 0.9f), listOf(0.15f, 0.15f, 0.9f))
    assertEquals(0.05f, errs[0]!!, 1e-5f)
    assertNull(errs[1])
    assertNull(errs[2])
  }
}
