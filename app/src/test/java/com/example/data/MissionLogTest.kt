package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class MissionLogTest {
  private val today = "2026-09-28"
  private val t0 = 1_000_000_000L

  @Test
  fun `el dia pasa de transmision a espera, informe y al dia`() {
    var s = MissionState()
    assertEquals(MissionStep.TRANSMISION, MissionLog.step(s, today, t0, dailyDone = false))
    s = MissionLog.onEncoded(s, today, t0, seed = 42, level = 3, items = 4, learnedMask = 0b1011)
    assertEquals(MissionStep.ESPERA, MissionLog.step(s, today, t0 + 60_000, dailyDone = false))
    assertEquals(9, MissionLog.minutesLeft(s, t0 + 60_000))
    // Se abre al terminar los juegos de la sesión, o pasados 10 minutos.
    assertEquals(MissionStep.INFORME, MissionLog.step(s, today, t0 + 60_000, dailyDone = true))
    assertEquals(MissionStep.INFORME, MissionLog.step(s, today, t0 + MissionLog.MIN_DELAY_MS, dailyDone = false))
    s = MissionLog.onReported(s, recalled = 3)
    assertEquals(MissionStep.AL_DIA, MissionLog.step(s, today, t0 + MissionLog.MIN_DELAY_MS, dailyDone = true))
    assertEquals(3, s.archivedTotal)
    assertEquals(1, s.missions)
    // Al día siguiente llega otra transmisión.
    assertEquals(MissionStep.TRANSMISION, MissionLog.step(s, "2026-09-29", t0 + 86_400_000L, dailyDone = false))
  }

  @Test
  fun `un informe pendiente de ayer se puede hacer y bloquea la nueva transmision`() {
    val s = MissionLog.onEncoded(MissionState(), "2026-09-27", t0, 7, 2, 3, 0b111)
    assertEquals(MissionStep.INFORME, MissionLog.step(s, today, t0 + 20 * 3_600_000L, dailyDone = false))
  }

  @Test
  fun `retencion es lo recordado de lo aprendido`() {
    assertEquals(75, MissionLog.retentionPct(0b1111, 0b10111))  // el 5.º no se aprendió: no cuenta
    assertNull(MissionLog.retentionPct(0, 0b11))
    assertEquals(0, MissionLog.retentionPct(0b11, 0))
  }

  @Test
  fun `la demora se dice en palabras`() {
    assertEquals("a los 50 segundos", MissionLog.delayLabel(50))
    assertEquals("a los 3 minutos", MissionLog.delayLabel(170))
    assertEquals("a los 14 minutos", MissionLog.delayLabel(14 * 60))
    assertEquals("a las 5 horas", MissionLog.delayLabel(5 * 3600))
  }
}
