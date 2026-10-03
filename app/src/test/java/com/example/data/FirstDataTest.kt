package com.example.data

import com.example.model.GamePlayResult
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class FirstDataTest {
  private fun result(id: String, block: GamePlayResult.() -> GamePlayResult = { this }) =
    GamePlayResult(gameId = id, score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 1).block()

  @Test
  fun `Freno dice cuantos altos se frenaron a tiempo`() {
    assertEquals("Frenaste a tiempo 6 de 8 veces.", FirstData.phrase(result("freno") { copy(stopsOk = 6, stopsTotal = 8) }))
    assertEquals("Frenaste a tiempo 0 de 8 veces.", FirstData.phrase(result("freno") { copy(stopsOk = 0, stopsTotal = 8) }))
  }

  @Test
  fun `Freno no dice nada con pocos altos o sin datos`() {
    assertNull(FirstData.phrase(result("freno") { copy(stopsOk = 2, stopsTotal = 3) }))
    assertNull(FirstData.phrase(result("freno")))
  }

  @Test
  fun `Freno nunca dice mas frenados que altos`() {
    assertEquals("Frenaste a tiempo 8 de 8 veces.", FirstData.brake(12, 8))
  }

  @Test
  fun `Aterrizaje dice la distancia media al lugar justo, redondeada y sin decimales`() {
    assertEquals("En promedio, aterrizaste a un 4 % de distancia del lugar justo.",
      FirstData.phrase(result("aterrizaje") { copy(numlineErrorPct = 4.2f) }))
    assertEquals("En promedio, aterrizaste a un 5 % de distancia del lugar justo.", FirstData.landing(4.5f))
    assertEquals("En promedio, aterrizaste a un 0 % de distancia del lugar justo.", FirstData.landing(0f))
    assertEquals("En promedio, aterrizaste a un 0 % de distancia del lugar justo.", FirstData.landing(0.4f))
  }

  @Test
  fun `Rastro de luz dice el rastro mas largo repetido bien`() {
    assertEquals("Repetiste bien un rastro de 5 luces.", FirstData.phrase(result("secuencia") { copy(rasBestLen = listOf(5, 0, 0, 0)) }))
    assertEquals("Repetiste bien un rastro de 1 luz.", FirstData.trail(listOf(1, 0, 0, 0)))
    // El mejor largo de los otros modos no cuenta: solo el rastro simple (familia 0).
    assertNull(FirstData.phrase(result("secuencia") { copy(rasBestLen = listOf(0, 4, 3, 2)) }))
    assertNull(FirstData.phrase(result("secuencia")))
  }

  @Test
  fun `Aterrizaje no dice nada sin medida`() {
    assertNull(FirstData.phrase(result("aterrizaje")))
    assertNull(FirstData.landing(-1f))
  }

  @Test
  fun `Meteoros suma las palabras reales de todas las bandas`() {
    val r = result("meteoros") { copy(lexBandSeen = listOf(4, 3, 2, 1, 1, 0), lexBandHits = listOf(4, 3, 1, 1, 0, 0)) }
    assertEquals("Reconociste 9 de 11 palabras reales.", FirstData.phrase(r))
  }

  @Test
  fun `Meteoros no dice nada con menos de 6 palabras reales vistas`() {
    assertNull(FirstData.phrase(result("meteoros") { copy(lexBandSeen = listOf(3, 2, 0, 0, 0, 0), lexBandHits = listOf(3, 2, 0, 0, 0, 0)) }))
    assertNull(FirstData.phrase(result("meteoros")))
  }

  @Test
  fun `los demas juegos no tienen primer dato propio`() {
    assertNull(FirstData.phrase(result("stroop")))
    assertNull(FirstData.phrase(result("parejas")))
  }
}
