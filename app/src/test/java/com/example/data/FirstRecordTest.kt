package com.example.data

import com.example.model.GamePlayResult
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

/** La primera partida de un juego no celebra récord (Tarea 127): la marca previa (leída ANTES de guardar) debe ser mayor que 0. */
class FirstRecordTest {
  private fun result(id: String, block: GamePlayResult.() -> GamePlayResult) =
    GamePlayResult(gameId = id, score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 3, endRating = 0.5f, timestamp = 1_000L).block()

  private val none = FirstRecord.Priors()
  private val played = FirstRecord.Priors(rescate = 20, acoplamiento = 10, correo = 8, satelites = 12, bodega = 5, constelaciones = 4)

  @Test
  fun `sin marca previa la primera partida no rompe el record aunque Unity lo diga`() {
    // Unity manda 0 como marca previa: cualquier partida «supera» a 0 y dice récord nuevo
    assertEquals(false, FirstRecord.gate(result("radar") { copy(rescBest = 31, rescNewRecord = true) }, none).rescNewRecord)
    assertEquals(false, FirstRecord.gate(result("acoplamiento") { copy(dockBest = 9, dockNewRecord = true) }, none).dockNewRecord)
    assertEquals(false, FirstRecord.gate(result("correo") { copy(mailBest = 6, mailNewRecord = true) }, none).mailNewRecord)
    assertEquals(false, FirstRecord.gate(result("satelites") { copy(satBest = 12, satNewRecord = true) }, none).satNewRecord)
    assertEquals(false, FirstRecord.gate(result("bodega") { copy(bodBest = 5, bodNewRecord = true) }, none).bodNewRecord)
    assertEquals(false, FirstRecord.gate(result("parejas") { copy(conBest = 4, conNewRecord = true) }, none).conNewRecord)
  }

  @Test
  fun `con marca previa mayor que cero el record se conserva`() {
    assertEquals(true, FirstRecord.gate(result("radar") { copy(rescBest = 31, rescNewRecord = true) }, played).rescNewRecord)
    assertEquals(true, FirstRecord.gate(result("acoplamiento") { copy(dockBest = 9, dockNewRecord = true) }, played).dockNewRecord)
    assertEquals(true, FirstRecord.gate(result("correo") { copy(mailBest = 6, mailNewRecord = true) }, played).mailNewRecord)
    assertEquals(true, FirstRecord.gate(result("satelites") { copy(satBest = 12, satNewRecord = true) }, played).satNewRecord)
    assertEquals(true, FirstRecord.gate(result("bodega") { copy(bodBest = 5, bodNewRecord = true) }, played).bodNewRecord)
    assertEquals(true, FirstRecord.gate(result("parejas") { copy(conBest = 4, conNewRecord = true) }, played).conNewRecord)
    // y lo que no era récord sigue sin serlo
    assertEquals(false, FirstRecord.gate(result("radar") { copy(rescBest = 31, rescNewRecord = false) }, played).rescNewRecord)
  }

  @Test
  fun `lo que no venia de Unity sigue sin venir y los otros juegos no se tocan`() {
    assertNull(FirstRecord.gate(result("radar") { this }, none).rescNewRecord)
    assertNull(FirstRecord.gate(result("correo") { copy(mailBest = 3) }, none).mailNewRecord)
    val other = result("stroop") { this }
    assertEquals(other, FirstRecord.gate(other, none))
    // el récord de un juego no depende de la marca de otro
    val gated = FirstRecord.gate(result("radar") { copy(rescNewRecord = true) }, FirstRecord.Priors(correo = 9))
    assertEquals(false, gated.rescNewRecord)
  }
}
