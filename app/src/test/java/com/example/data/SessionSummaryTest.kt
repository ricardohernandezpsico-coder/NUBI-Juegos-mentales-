package com.example.data

import com.example.model.GamePlayResult
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class SessionSummaryTest {
  private val hour = 3_600_000L
  private val now = 20_000 * 24 * hour

  private fun play(id: String, score: Int, t: Long) = GamePlayResult(gameId = id, score = score, correctAnswers = 0, totalTrials = 0, timed = false, level = 1, timestamp = t)

  @Test
  fun `el resumen dice que se jugo, con que puntaje y cuanto se movio cada area desde el comienzo`() {
    val history = listOf(
      play("radar", 60, now - 30 * hour),      // de ayer: no es de esta sesión
      play("radar", 82, now - 20 * 60_000L),
      play("calculo", 74, now - 12 * 60_000L)
      // parejas no se jugó
    )
    val points = listOf(
      ProgressPoint("radar", now - 5 * 24 * hour, 0.46f), ProgressPoint("radar", now - 20 * 60_000L, 0.52f),
      ProgressPoint("calculo", now - 3 * 24 * hour, 0.40f)
    )
    val progress = mapOf("radar" to 0.52f, "calculo" to 0.40f)
    val s = SessionSummary.build(listOf("radar", "calculo", "parejas"), history, progress, points, now)
    assertEquals(listOf(82, 74, null), s.games.map { it.score })
    assertEquals(listOf("ATENCION", "RAZONAMIENTO", "MEMORIA"), s.areas.map { it.area })
    // Atención: la partida de hoy movió radar de 46 a 52.
    assertEquals(0.06f, s.areas[0].change, 1e-4f)
    assertEquals("Hoy avanzó de 46 a 52", AreaProgress.changeLine(s.areas[0], "Hoy"))
    // Razonamiento: calculo no se movió en la sesión; Memoria sin medir.
    assertEquals(0f, s.areas[1].change, 1e-4f)
    assertNull(s.areas[2].value)
  }
}
