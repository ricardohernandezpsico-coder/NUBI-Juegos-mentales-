package com.example.bridge

import android.app.Activity
import android.view.Choreographer
import com.example.diag.ProbeLog
import java.util.Locale

/**
 * La sonda de la ANIMACIÓN de la pantalla de carga (Tarea 190b, SOLO en depuración): Ricardo vio la luna que orbita el planeta «pegada». Mientras la capa está visible mide, con un `Choreographer.FrameCallback`, el tiempo entre un cuadro y el siguiente DEL HILO DE LA INTERFAZ
 * (el mismo que dibuja la órbita de Compose, en el proceso `:unity`) y escribe UNA línea en `files/sonda-entrada.txt` ([ProbeLog]): cuántos cuadros, mediana, p95, máximo, cuántos pasan de [HITCH_MS], cuánto estuvo visible, si fue en frío o en caliente, los núcleos del teléfono y la frecuencia de la pantalla.
 * Se compara con la línea `[sonda-entrada]` que escribe Unity (hilos de horneo y de sonido): si los cuadros de la carga se alargan justo mientras Unity trabaja, los hilos de trabajo le quitan el tiempo al hilo de la interfaz.
 */
internal class LoadingFrameProbe(private val game: String, private val warm: Boolean, private val activity: Activity) : Choreographer.FrameCallback {
  private val gaps = ArrayList<Float>()
  private var last = 0L
  private var startedAtNs = 0L
  private var running = false

  fun start() {
    running = true
    startedAtNs = System.nanoTime()
    Choreographer.getInstance().postFrameCallback(this)
  }

  override fun doFrame(frameTimeNanos: Long) {
    if (!running) return
    if (last != 0L) gaps.add((frameTimeNanos - last) / 1_000_000f)       // el primer cuadro no tiene anterior
    last = frameTimeNanos
    Choreographer.getInstance().postFrameCallback(this)
  }

  fun stopAndWrite() {
    if (!running) return
    running = false
    val seconds = (System.nanoTime() - startedAtNs) / 1e9
    ProbeLog.append(format(game, warm, gaps, seconds, Runtime.getRuntime().availableProcessors(), refreshRate()))
  }

  @Suppress("DEPRECATION")
  private fun refreshRate(): Float = runCatching { activity.windowManager.defaultDisplay.refreshRate }.getOrDefault(0f)

  companion object {
    const val HITCH_MS = 34f

    /** La línea de la sonda (pura: la prueba la lee sin Android). */
    internal fun format(game: String, warm: Boolean, gapsMs: List<Float>, visibleSeconds: Double, cores: Int, refreshHz: Float): String {
      val sorted = gapsMs.sorted()
      fun pct(p: Double): Float = if (sorted.isEmpty()) 0f else sorted[maxOf(0, minOf(sorted.size - 1, kotlin.math.ceil(p * sorted.size).toInt() - 1))]
      val over = sorted.count { it > HITCH_MS }
      return String.format(
        Locale.US,
        "[sonda-carga] juego=%s %s | animación de la carga (hilo de la interfaz): %d cuadros, mediana %.1f ms, p95 %.1f ms, máx %.1f ms, >%.0f ms: %d | visible %.2f s | núcleos %d, pantalla %.0f Hz",
        game, if (warm) "en caliente" else "en frío", sorted.size, pct(0.5), pct(0.95), sorted.lastOrNull() ?: 0f, HITCH_MS, over, visibleSeconds, cores, refreshHz
      )
    }
  }
}
