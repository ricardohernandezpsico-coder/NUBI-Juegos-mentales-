package com.example.bridge

import android.content.Context
import com.example.NeuroVidaApplication

/**
 * El resultado de una partida llega a la app por dos caminos (el Intent de vuelta a la app y el broadcast de
 * respaldo, ver [NativeReceiver]); acá se anota qué lanzamientos ya se procesaron para guardarlo una sola vez.
 * Persistido en SharedPreferences (los últimos [MAX] ids) porque el broadcast puede llegar con la app recién
 * recreada. Solo se usa en el proceso principal.
 *
 * Desde la Tarea 192 la marca de disco ([markHandled]) se escribe DESPUÉS de guardar la partida (antes se escribía al llegar: si el proceso moría entre la marca y el guardado, la partida se perdía). Mientras se guarda, el lanzamiento
 * queda RESERVADO en memoria ([claim]): los dos caminos no guardan a la vez; si el guardado falla, [release] suelta la reserva sin anotarla. Esto es solo el filtro rápido: lo que garantiza que no se guarde dos veces
 * es la base (el `id` de la fila es el del lanzamiento: `NeuroVidaRepository.recordGameResult`).
 */
object UnityResultInbox {
  private const val PREFS = "unity_results"
  private const val KEY = "handled_launch_ids"
  private const val MAX = 30

  private val inFlight = HashSet<String>()

  private fun prefs() = NeuroVidaApplication.instance.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
  private fun handledIds(): List<String> = prefs().getString(KEY, "").orEmpty().split(',').filter { it.isNotEmpty() }

  /** true si es la primera vez que llega este lanzamiento y nadie lo está guardando ya; lo RESERVA en memoria (no lo anota en disco: eso es [markHandled], tras guardar). */
  @Synchronized
  fun claim(launchId: String?): Boolean {
    if (launchId.isNullOrEmpty()) return true // lanzamientos sin id (versión vieja): sin deduplicar
    if (launchId in inFlight || launchId in handledIds()) return false
    inFlight.add(launchId)
    return true
  }

  /** La partida YA está guardada: se anota en disco y se suelta la reserva. */
  @Synchronized
  fun markHandled(launchId: String?) {
    if (launchId.isNullOrEmpty()) return
    inFlight.remove(launchId)
    val ids = handledIds()
    if (launchId in ids) return
    prefs().edit().putString(KEY, (ids + launchId).takeLast(MAX).joinToString(",")).apply()
  }

  /** El guardado falló: se suelta la reserva SIN anotar el lanzamiento, para que otra entrega pueda volver a intentarlo. */
  @Synchronized
  fun release(launchId: String?) {
    if (!launchId.isNullOrEmpty()) inFlight.remove(launchId)
  }

  /** SOLO PARA LAS PRUEBAS: un «reinicio» del proceso (se pierde lo reservado en memoria; lo anotado en disco queda). */
  @Synchronized
  internal fun forgetMemory() = inFlight.clear()
}
