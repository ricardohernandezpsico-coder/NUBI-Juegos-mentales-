package com.example.data

import com.example.model.GamePlayResult
import org.junit.Assert.assertEquals
import org.junit.Test
import java.time.LocalDateTime
import java.time.ZoneId

/** La cuenta regresiva: 3 pasos la primera partida del día y la primera vez de un juego; 2 pasos en las demás (Tarea 127). */
class CountdownStepsTest {
  private val zone = ZoneId.of("America/Santiago")
  private fun at(y: Int, m: Int, d: Int, h: Int) = LocalDateTime.of(y, m, d, h, 0).atZone(zone).toInstant().toEpochMilli()
  private fun played(id: String, ms: Long) = GamePlayResult(gameId = id, score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 3, timestamp = ms)

  private val now = at(2026, 10, 18, 11)

  @Test
  fun `la primera vez de un juego son 3 pasos aunque ya se haya jugado otro hoy`() {
    val history = listOf(played("stroop", at(2026, 10, 18, 9)))
    assertEquals(3, CountdownSteps.steps("radar", history, now, zone))
  }

  @Test
  fun `la primera partida del dia son 3 pasos aunque ya se jugara ese juego otro dia`() {
    val history = listOf(played("radar", at(2026, 10, 17, 20)), played("stroop", at(2026, 10, 16, 8)))
    assertEquals(3, CountdownSteps.steps("radar", history, now, zone))
    assertEquals(3, CountdownSteps.steps("stroop", history, now, zone))
  }

  @Test
  fun `las demas son 2 pasos`() {
    val history = listOf(played("radar", at(2026, 10, 17, 20)), played("stroop", at(2026, 10, 18, 9)))
    assertEquals(2, CountdownSteps.steps("radar", history, now, zone))
    assertEquals(2, CountdownSteps.steps("stroop", history, now, zone))
  }

  @Test
  fun `sin historial son 3 pasos`() {
    assertEquals(3, CountdownSteps.steps("radar", emptyList(), now, zone))
  }

  @Test
  fun `el dia es el del calendario de la persona y no pasa a la medianoche UTC`() {
    // 23:30 del 17 (hora de Santiago) ya es el 18 en UTC: la partida de las 23:30 del 17 NO es «de hoy» (18, 11:00 de Santiago)
    val history = listOf(played("radar", at(2026, 10, 17, 23)), played("stroop", at(2026, 10, 17, 23)))
    assertEquals(3, CountdownSteps.steps("radar", history, now, zone))
  }
}
