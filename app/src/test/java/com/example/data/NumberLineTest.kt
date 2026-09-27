package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Test

class NumberLineTest {
  @Test
  fun `sin aterrizajes suficientes no hay lectura`() {
    assertEquals(NumberLineBias.SIN_DATOS, NumberLine.bias(emptyList(), emptyList()))
    assertEquals(NumberLineBias.SIN_DATOS, NumberLine.bias(listOf(0.1f, 0.9f), listOf(0.2f, 0.8f)))
  }

  @Test
  fun `aterrizajes justos dan una linea pareja`() {
    val t = listOf(0.1f, 0.2f, 0.5f, 0.8f, 0.9f)
    assertEquals(NumberLineBias.PAREJA, NumberLine.bias(t, t))
  }

  @Test
  fun `agrandar los chicos y achicar los grandes es comprimir`() {
    val t = listOf(0.1f, 0.2f, 0.8f, 0.9f)
    assertEquals(NumberLineBias.COMPRIME, NumberLine.bias(t, listOf(0.2f, 0.3f, 0.72f, 0.8f)))
    assertEquals(NumberLineBias.AGRANDA_CHICOS, NumberLine.bias(t, listOf(0.2f, 0.3f, 0.8f, 0.9f)))
    assertEquals(NumberLineBias.ACHICA_GRANDES, NumberLine.bias(t, listOf(0.1f, 0.2f, 0.72f, 0.8f)))
  }
}
