package com.example.data

import com.example.model.GamePlayResult
import java.time.Instant
import java.time.ZoneId

/**
 * Los pasos de la cuenta regresiva de un juego (Tarea 127, `auditoria-fable/pulido-juegos.md` §1.4): 3 («3 · 2 · 1 · ¡Ya!», 3,1 s) en la PRIMERA partida del día y en la primera vez que se juega un juego; 2 («2 · 1 · ¡Ya!», 2,4 s) en las demás.
 * En la ruta de tres juegos del día eran unos 9 s de cuenta; ahora, 3,1 + 2,4 + 2,4. La app lo decide (ya sabe el historial) y se lo manda a Unity en la config (`countdown_steps`); con «quitar animaciones» rige la misma regla.
 */
object CountdownSteps {
  const val FULL = 3
  const val SHORT = 2

  /**
   * @param history las partidas ya guardadas (todas las de la persona; `timestamp` en milisegundos).
   * @param nowMs ahora, en milisegundos. @param zone la zona horaria de la persona: «el día» es el de su calendario.
   */
  fun steps(gameId: String, history: List<GamePlayResult>, nowMs: Long, zone: ZoneId = ZoneId.systemDefault()): Int {
    val firstTimeOfGame = history.none { it.gameId == gameId }
    if (firstTimeOfGame) return FULL
    val today = Instant.ofEpochMilli(nowMs).atZone(zone).toLocalDate()
    val firstOfTheDay = history.none { Instant.ofEpochMilli(it.timestamp).atZone(zone).toLocalDate() == today }
    return if (firstOfTheDay) FULL else SHORT
  }
}
