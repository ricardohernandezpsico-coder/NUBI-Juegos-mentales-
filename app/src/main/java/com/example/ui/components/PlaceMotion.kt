package com.example.ui.components

/**
 * Los tiempos de lo que quedó del toque a un edificio de la isla (Tarea 85; boceto «A», artboard `Main.dc.html`; la «mirada» de Nubi se borró en la Tarea 182): Nubi mira hacia el edificio tocado y, a los ~650 ms, se abre «Todos los juegos» en esa área ([IslaLook]). Desde la Tarea 163 no hay lámina del edificio: se fueron el velo, la lámina,
 * el vuelo de Nubi hasta posarse en ella y su globo; quedan los tiempos de la barra de pestañas y los del brillo del edificio. Todo es matemática pura (se prueba sin pantalla).
 */
object PlaceMotion {
  /** Cuánto se queda mirando antes de que se abra «Todos los juegos». */
  const val LOOK_HOLD_MS = 650
  const val GLOW_MS = 320
  /** Fundido de la losa sol de la pestaña elegida en la barra inferior (Tarea 86 el cubo; Tarea 156 la losa). */
  const val TAB_FADE_MS = NubiMotion.STATE_FAST_MS
  /** Con «quitar animaciones»: un fundido corto y nada más. */
  const val REDUCED_FADE_MS = NubiMotion.REDUCED_FADE_MS

  /** Valor en [p] (0..1) de una lista de cuadros clave (progreso → valor), suavizado entre cuadro y cuadro (las farolas de la ruta lo usaban: se retiraron en la Tarea 168). */
  fun sample(keys: List<Pair<Float, Float>>, p: Float): Float {
    val x = p.coerceIn(0f, 1f)
    for (i in 1 until keys.size) {
      val (p0, v0) = keys[i - 1]
      val (p1, v1) = keys[i]
      if (x <= p1) {
        val t = if (p1 == p0) 1f else (x - p0) / (p1 - p0)
        val s = t * t * (3f - 2f * t)
        return v0 + (v1 - v0) * s
      }
    }
    return keys.last().second
  }
}
