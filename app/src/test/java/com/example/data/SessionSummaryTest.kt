package com.example.data

import com.example.model.GamePlayResult
import org.junit.Assert.assertEquals
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
    val s = SessionSummary.build(listOf("radar", "calculo", "parejas"), history, points, now)
    assertEquals("quiénes se jugaron en esta ruta (parejas no)", listOf(true, true, false), s.games.map { it.played })
    assertEquals("los puntos que sumó cada juego hoy en su área: radar 0,46 → 0,52 = 92 → 104 de 200; calculo no se movió; parejas no se jugó", listOf(12, 0, 0), s.games.map { it.gained })
    assertEquals(listOf("ATENCION", "RAZONAMIENTO", "MEMORIA"), s.areas.map { it.area })
    // Atención (5 juegos, hasta 200 cada uno): la partida de hoy movió radar de 0,46 a 0,52 → de 92 a 104 puntos.
    assertEquals(12, s.areas[0].weekDelta)
    assertEquals("Hoy sumaste 12 puntos.", AreaPoints.todayLine(s.areas[0], "Atención"))
    // Razonamiento: calculo no se movió en la sesión; Memoria sin medir.
    assertEquals(0, s.areas[1].weekDelta)
    assertEquals("Hoy te mantuviste.", AreaPoints.todayLine(s.areas[1], "Razonamiento"))
    assertEquals(false, s.areas[2].measured)
  }
}
