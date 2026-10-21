package com.example.ui.components

import androidx.compose.animation.core.CubicBezierEasing
import androidx.compose.ui.graphics.Color

/**
 * Los tiempos y las curvas de la lámina del lugar de Hoy (Tarea 85; boceto «A», artboard `Main.dc.html`): Nubi mira hacia el edificio tocado, a los ~650 ms cae el velo, sube la lámina y Nubi vuela en arco hasta posarse arriba a la izquierda de ella.
 * Todo es matemática pura (se prueba sin pantalla); lo anima `ui/screens/HoyPlace.kt`. Con «quitar animaciones» nada de esto se usa: la lámina y Nubi aparecen en su sitio con un fundido corto.
 */
object PlaceMotion {
  /** Fundido entre Nubi `hola` y `mira_<lugar>`. */
  const val LOOK_FADE_MS = 180
  /** Lo que tarda Nubi en correrse y ladearse hacia el edificio. */
  const val LOOK_SHIFT_MS = 260
  /** Cuánto se queda mirando antes de que caiga el velo. */
  const val LOOK_HOLD_MS = 650
  /** El velo cae como un cambio de estado (`NubiMotion`, Tarea 123: 360 → 200). */
  const val VEIL_MS = NubiMotion.STATE_MS
  /**
   * El velo que cae sobre la página cuando se abre la lámina de un lugar (Tarea 122): casi negro (`#070620`) al 45 %. Sobre el cielo «Espacio profundo» (`#151539`) el tinte de antes (`#211C45` al 50 %, MÁS CLARO que el cielo) lo habría
   * aclarado; este lo oscurece un poco y apaga la isla, la ruta y el saludo (la crema de la isla baja de 0,84 a ~0,23 de luminancia, igual que antes). `PlaceMotionTest` mide las dos cosas. El tiempo (360 ms) no cambia: la escala de movimiento entera se rehace aparte.
   */
  val VEIL = Color(0x73070620)
  /** La lámina es un cambio de lugar (`NubiMotion`, Tarea 123: 460 → 400)… */
  const val SHEET_MS = NubiMotion.SHEET_MS
  /** …y sale AL TOQUE, junto con el velo (antes esperaba 180 ms). */
  const val SHEET_DELAY_MS = 0
  const val FLIGHT_MS = 820
  const val BACK_FLIGHT_MS = 620
  /** El globo aparece cuando Nubi está por posarse (640 ms después de empezar el vuelo). */
  const val GLOBE_DELAY_MS = 640
  const val GLOBE_MS = 320
  const val SHADOW_MS = 320
  const val GLOW_MS = 320
  /** Fundido de la losa sol de la pestaña elegida en la barra inferior (Tarea 86 el cubo; Tarea 156 la losa). */
  const val TAB_FADE_MS = NubiMotion.STATE_FAST_MS
  /** Con «quitar animaciones»: un fundido corto y nada más. */
  const val REDUCED_FADE_MS = NubiMotion.REDUCED_FADE_MS

  /** `cubic-bezier(.3,.7,.3,1)` del boceto: arranca rápido y se posa suave. */
  val FlightEasing = CubicBezierEasing(0.3f, 0.7f, 0.3f, 1f)
  /** La curva «enfatizada» de la escala única (antes `cubic-bezier(.2,.8,.2,1)` del boceto): arranca rápido y frena suave. */
  val SheetEasing = NubiMotion.Emphasized

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

  // Vuelo de ida (dp hacia arriba negativo, grados): el arco sale de combinar el desplazamiento con un salto vertical. En el boceto `vuelo` sube ~92 px a un tercio del camino y se balancea de -10° a +6°.
  val OUT_ARC_DP = listOf(0f to 0f, 0.35f to -92f, 0.70f to -34f, 0.88f to 6f, 1f to 0f)
  val OUT_TILT_DEG = listOf(0f to 0f, 0.35f to -10f, 0.70f to 6f, 0.88f to -2f, 1f to 0f)
  // Vuelo de vuelta (`vuelta` en el boceto).
  val BACK_ARC_DP = listOf(0f to 0f, 0.40f to -70f, 1f to 0f)
  val BACK_TILT_DEG = listOf(0f to 0f, 0.40f to 8f, 1f to 0f)

  /** Valor en [p] (0..1) de una lista de cuadros clave (progreso → valor), suavizado entre cuadro y cuadro. */
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

  /** Dónde está Nubi en pleno vuelo: [position] 0 = en la isla, 1 = posada en la lámina; [outbound] = de ida (arco alto y balanceo) o de vuelta. */
  data class Flight(val easedPosition: Float, val arcDp: Float, val tiltDeg: Float)

  fun flight(position: Float, outbound: Boolean): Flight {
    val f = position.coerceIn(0f, 1f)
    val time = if (outbound) f else 1f - f   // el tiempo del cuadro clave: la ida va de 0 a 1 y la vuelta de 1 a 0
    return Flight(
      easedPosition = FlightEasing.transform(f),
      arcDp = sample(if (outbound) OUT_ARC_DP else BACK_ARC_DP, time),
      tiltDeg = sample(if (outbound) OUT_TILT_DEG else BACK_TILT_DEG, time)
    )
  }

  /** Mezcla lineal. */
  fun lerp(a: Float, b: Float, t: Float): Float = a + (b - a) * t
}
