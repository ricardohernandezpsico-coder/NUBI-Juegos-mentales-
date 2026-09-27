package com.example.bridge

import com.example.model.GamePlayResult
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow

/** Resultado de una partida y el id de lanzamiento con que se abrió (para saber a qué sesión de la app pertenece). */
data class FinishedGame(val result: GamePlayResult, val launchId: String?)

/**
 * Canal por donde llega a la UI (ViewModel) el resultado de una partida jugada en Unity.
 *
 * [NativeReceiver.handleFinished] lo publica; el `NeuroVidaViewModel` lo recoge, lo guarda en Room y muestra
 * la pantalla de resultado de la app. Si nadie está escuchando (la app se cerró mientras Unity estaba al frente),
 * [publish] devuelve false y el receptor lo deja pendiente en [GameSessionStore] (o lo guarda directo).
 */
object UnityResultBus {
  private val _results = MutableSharedFlow<FinishedGame>(
    replay = 0,
    extraBufferCapacity = 8,
    onBufferOverflow = BufferOverflow.DROP_OLDEST
  )
  val results: SharedFlow<FinishedGame> = _results.asSharedFlow()

  /** true si hay al menos un suscriptor que recibirá el resultado. */
  fun publish(game: FinishedGame): Boolean {
    val hasSubscribers = _results.subscriptionCount.value > 0
    if (hasSubscribers) _results.tryEmit(game)
    return hasSubscribers
  }
}
