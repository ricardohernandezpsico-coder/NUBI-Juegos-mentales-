package com.example.data

import com.example.model.GamePlayResult

/**
 * La primera partida de un juego no rompe ningún récord (Tarea 127): no hay nada que superar. Sin marca previa la app le manda 0 a Unity (`UnityGameLauncher`: `resc_best`, `dock_best`, `mail_best`…), así que Unity dice «récord nuevo» en toda primera partida
 * (cualquier cosa supera a 0). Se arregla AQUÍ, en la app y sin tocar la telemetría: el sello, el sonido y la vibración del récord van solo si la marca previa (leída del repositorio ANTES de guardar la partida) era mayor que 0.
 */
object FirstRecord {
  /** Las marcas guardadas ANTES de esta partida, de los seis juegos que llevan récord (0 = nunca se jugó o nunca se marcó). */
  data class Priors(
    val rescate: Int = 0,
    val acoplamiento: Int = 0,
    val correo: Int = 0,
    val satelites: Int = 0,
    val bodega: Int = 0,
    val constelaciones: Int = 0
  )

  /** El mismo resultado con «récord nuevo» apagado si la marca previa de ese juego era 0 (lo que no venía, queda como venía: null). */
  fun gate(result: GamePlayResult, priors: Priors): GamePlayResult = when (result.gameId) {
    "radar" -> if (priors.rescate > 0) result else result.copy(rescNewRecord = result.rescNewRecord?.let { false })
    "acoplamiento" -> if (priors.acoplamiento > 0) result else result.copy(dockNewRecord = result.dockNewRecord?.let { false })
    "correo" -> if (priors.correo > 0) result else result.copy(mailNewRecord = result.mailNewRecord?.let { false })
    "satelites" -> if (priors.satelites > 0) result else result.copy(satNewRecord = result.satNewRecord?.let { false })
    "bodega" -> if (priors.bodega > 0) result else result.copy(bodNewRecord = result.bodNewRecord?.let { false })
    "parejas" -> if (priors.constelaciones > 0) result else result.copy(conNewRecord = result.conNewRecord?.let { false })
    else -> result
  }
}
