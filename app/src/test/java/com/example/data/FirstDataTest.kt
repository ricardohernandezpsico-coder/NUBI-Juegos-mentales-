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
  fun `Aterrizaje dice la distancia media al lugar justo en porcentaje de la regla`() {
    assertEquals("Tus aterrizajes quedaron, en promedio, a 4,2 % del largo de la regla del lugar justo.",
      FirstData.phrase(result("aterrizaje") { copy(numlineErrorPct = 4.2f) }))
    assertEquals("Tus aterrizajes quedaron, en promedio, a 0,0 % del largo de la regla del lugar justo.", FirstData.landing(0f))
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
    assertNull(FirstData.phrase(result("secuencia")))
  }
}
