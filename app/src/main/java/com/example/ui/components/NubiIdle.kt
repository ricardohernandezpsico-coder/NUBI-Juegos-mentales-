package com.example.ui.components

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.Stable
import androidx.compose.runtime.State
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.runtime.remember
import androidx.compose.runtime.withFrameMillis
import androidx.compose.ui.platform.InfiniteAnimationPolicy
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlin.coroutines.coroutineContext
import kotlin.math.PI
import kotlin.math.sin
import kotlin.random.Random

/**
 * El reposo de Nubi (Tarea 148, P1 de `auditoria-fable/propuestas-diseno-visual.md` §2.3): **flota** y **parpadea** aunque no pase nada, para que sea alguien que está ahí y no una ilustración pegada.
 * - Flota: sube y baja ±[FLOAT_DP] dp en un seno de [FLOAT_MS] ms (el período que ya fijaba `docs/diseno-isla.md` §5); su sombra se achica y se aclara en el punto alto.
 * - Parpadea: un fundido de [BLINK_MS] ms a la imagen «descansa» (ojos cerrados) y de vuelta, con una espera al azar de [MIN_WAIT_MS] a [MAX_WAIT_MS] ms entre uno y otro, y un parpadeo doble cada [DOUBLE_EVERY] veces. Nunca mientras mira un lugar, salta o vuela (quien dibuja pasa `active = false`).
 * Es decorativo e igual en toda partida, buena o mala: nada reacciona al desempeño (regla de patentes). Con «quitar animaciones», Nubi queda quieta y con los ojos abiertos, como antes. Lógica pura aquí; `NubiIdleTest` la prueba.
 */
object NubiIdle {
  const val FLOAT_DP = 6f
  const val FLOAT_MS = 4500L
  const val BLINK_MS = 120
  const val MIN_WAIT_MS = 4000L
  const val MAX_WAIT_MS = 7000L
  const val DOUBLE_EVERY = 5
  /** Entre los dos parpadeos de uno doble. */
  const val DOUBLE_GAP_MS = 160L
  /** Lo que tarda el vaivén en apagarse (o en volver) cuando Nubi salta o mira un lugar: sin tirón. */
  const val CALM_MS = 250L

  /** SOLO PRUEBAS: una semilla fija para que el primer parpadeo caiga siempre a la misma hora (las capturas y `NubiIdleScreenshotTest`); en la app es null (al azar de verdad). */
  internal var seedForTests: Long? = null
  /** SOLO PRUEBAS: el último estado que armó `rememberNubiIdle` (las pruebas leen cuánto flota y cuánto están cerrados los ojos sin mirar píxeles). */
  internal var lastForTests: NubiIdleState? = null

  /** El desplazamiento vertical (dp, positivo = arriba) a los [timeMs] ms: un seno de ±[FLOAT_DP] con período [FLOAT_MS]. */
  fun floatDp(timeMs: Long): Float = FLOAT_DP * sin(2.0 * PI * (timeMs % FLOAT_MS) / FLOAT_MS).toFloat()

  /** Cuánto espera antes del parpadeo número [blinkCount] (1, 2, 3…): entre [MIN_WAIT_MS] y [MAX_WAIT_MS]. */
  fun waitMs(random: Random): Long = MIN_WAIT_MS + (random.nextFloat() * (MAX_WAIT_MS - MIN_WAIT_MS)).toLong()

  /** ¿El parpadeo número [blinkCount] (1, 2, 3…) es doble? Uno de cada [DOUBLE_EVERY]. */
  fun isDouble(blinkCount: Int): Boolean = blinkCount > 0 && blinkCount % DOUBLE_EVERY == 0

  /** De 0 (en el punto más bajo) a 1 (en el más alto): para la sombra. */
  fun height01(floatDp: Float): Float = ((floatDp + FLOAT_DP) / (2f * FLOAT_DP)).coerceIn(0f, 1f)

  /** La sombra en el punto alto: 0,85 de su ancho y 0,7 de su opacidad (como la sombra de la isla con movimiento, `diseno-isla.md` §6). */
  fun shadowSize(height01: Float): Float = 1f - 0.15f * height01
  fun shadowAlpha(height01: Float): Float = 1f - 0.3f * height01
}

/** Una operación de una animación INFINITA: en las pruebas de Compose el `InfiniteAnimationPolicy` la cancela (con el reloj en automático) para que `waitForIdle` no se quede esperando un reposo que nunca termina. */
private suspend fun <R> infinite(block: suspend () -> R): R = coroutineContext[InfiniteAnimationPolicy]?.onInfiniteOperation(block) ?: block()

/** Lo que dibuja quien muestra a Nubi: cuánto sube ([floatDp], dp) y cuánto están cerrados los ojos ([eyesClosed], 0 a 1). Se leen al DIBUJAR (en `graphicsLayer`), así que no recomponen nada. */
@Stable
class NubiIdleState internal constructor(private val float: State<Float>, private val closed: State<Float>) {
  val floatDp: Float get() = float.value
  val eyesClosed: Float get() = closed.value
}

/**
 * El reposo de Nubi para quien la dibuja. [active] se pregunta cuadro a cuadro: false (mira un lugar o salta) la deja quieta —sube y baja se apaga suave en [NubiIdle.CALM_MS] ms, sin tirón— y sin parpadear (uno ya empezado termina); con «quitar animaciones» ([rememberReduceMotion]) queda quieta y con los ojos abiertos siempre.
 * [active] se lee al dibujar, no al componer: una lambda que mira la animación del salto (`nubiLiftDp()`) no recompone nada.
 */
@Composable
fun rememberNubiIdle(active: () -> Boolean = { true }): NubiIdleState {
  val reduce = rememberReduceMotion()
  val isActive = rememberUpdatedState(active)
  val float = remember { mutableFloatStateOf(0f) }
  val closed = remember { mutableFloatStateOf(0f) }
  LaunchedEffect(reduce) {
    if (reduce) {
      float.floatValue = 0f; closed.floatValue = 0f
      return@LaunchedEffect
    }
    val random = Random(NubiIdle.seedForTests ?: System.nanoTime())
    launch {
      var weight = 0f; var last = -1L        // cuánto del vaivén se ve: 1 en reposo, 0 mientras salta o mira un lugar
      while (true) infinite {
        withFrameMillis { t ->
          val dt = if (last < 0L) 0L else t - last; last = t
          val target = if (isActive.value()) 1f else 0f
          val step = dt / NubiIdle.CALM_MS.toFloat()
          weight = if (weight < target) minOf(target, weight + step) else maxOf(target, weight - step)
          float.floatValue = NubiIdle.floatDp(t) * weight
        }
      }
    }
    suspend fun blink() {
      // un fundido de BLINK_MS ms hacia «descansa» y otro de vuelta, a pasos por cuadro (la imagen cerrada se funde por alfa)
      var start = -1L
      while (true) {
        val done = infinite { withFrameMillis { t -> if (start < 0) start = t; val p = ((t - start).toFloat() / NubiIdle.BLINK_MS).coerceIn(0f, 1f); closed.floatValue = p; p >= 1f } }
        if (done) break
      }
      start = -1L
      while (true) {
        val done = infinite { withFrameMillis { t -> if (start < 0) start = t; val p = ((t - start).toFloat() / NubiIdle.BLINK_MS).coerceIn(0f, 1f); closed.floatValue = 1f - p; p >= 1f } }
        if (done) break
      }
    }
    var n = 0
    while (true) {
      infinite { delay(NubiIdle.waitMs(random)) }
      while (!isActive.value()) infinite { delay(100) }          // nunca parpadea mientras mira un lugar o salta: espera a que termine
      n++
      blink()
      if (NubiIdle.isDouble(n)) { delay(NubiIdle.DOUBLE_GAP_MS); blink() }
    }
  }
  return remember { NubiIdleState(float, closed).also { NubiIdle.lastForTests = it } }
}
