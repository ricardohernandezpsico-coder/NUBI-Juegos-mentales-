package com.example.ui.components

/**
 * Los tiempos y la mirada de Nubi cuando se toca un edificio de la isla (Tarea 85; boceto «A», artboard `Main.dc.html`): Nubi mira hacia el edificio tocado y, a los ~650 ms, se abre «Todos los juegos» en esa área ([IslaLook]). Desde la Tarea 163 no hay lámina del edificio: se fueron el velo, la lámina,
 * el vuelo de Nubi hasta posarse en ella y su globo; queda la mirada, el brillo del edificio y los tiempos que comparten la barra de pestañas y las farolas de la ruta. Todo es matemática pura (se prueba sin pantalla); lo anima [IslaLook].
 * Con «quitar animaciones» nada de esto se usa: la mirada solo cambia la imagen.
 */
object PlaceMotion {
  /** Fundido entre Nubi `hola` y `mira_<lugar>`. */
  const val LOOK_FADE_MS = 180
  /** Lo que tarda Nubi en correrse y ladearse hacia el edificio. */
  const val LOOK_SHIFT_MS = 260
  /** Cuánto se queda mirando antes de que se abra «Todos los juegos». */
  const val LOOK_HOLD_MS = 650
  const val GLOW_MS = 320
  /** Fundido de la losa sol de la pestaña elegida en la barra inferior (Tarea 86 el cubo; Tarea 156 la losa). */
  const val TAB_FADE_MS = NubiMotion.STATE_FAST_MS
  /** Con «quitar animaciones»: un fundido corto y nada más. */
  const val REDUCED_FADE_MS = NubiMotion.REDUCED_FADE_MS

  /** Cuánto se corre Nubi (fracción del ANCHO de la isla; negativo = a la izquierda) y cuánto se ladea (grados, positivo = a la derecha) al mirar cada lugar. */
  data class Look(val shift: Float, val tiltDeg: Float)

  /** Del boceto (a 421 dp de ancho: 17 y 14 dp): el Observatorio, la Antena y el Hangar quedan a la izquierda de Nubi y la Biblioteca a la derecha. */
  val LOOKS: Map<String, Look> = mapOf(
    "observatorio" to Look(-0.040f, -6f),
    "antena" to Look(-0.040f, -6f),
    "hangar" to Look(-0.033f, -4f),
    "biblioteca" to Look(0.040f, 6f)
  )

  fun look(key: String?): Look = LOOKS[key] ?: Look(0f, 0f)

  /** El fundido de la imagen (`hola` → `mira`) sobre el avance [look] del corrimiento (0 → 1 en [LOOK_SHIFT_MS]): el fundido dura [LOOK_FADE_MS], menos. */
  fun lookFade(look: Float): Float = (look * LOOK_SHIFT_MS / LOOK_FADE_MS).coerceIn(0f, 1f)

  /** Valor en [p] (0..1) de una lista de cuadros clave (progreso → valor), suavizado entre cuadro y cuadro (lo usan las farolas de la ruta: [RouteMotion]). */
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
