package com.example.data

/**
 * «Satélites: enciende tu planeta» (id `satelites`, renovado el 9-oct; Unity: `Games/Satelites/SatelliteContract.cs`; diseño en docs/diseno-satelites.md): la lectura de lo que manda Unity para la pantalla final y el récord que se guarda.
 * Lógica pura con pruebas.
 *
 * La medida de siempre sigue: «Tu seguimiento» = cuántos satélites se siguieron de verdad a la vez, descontando los aciertos por suerte (`tracking_capacity`; va a StarMeasures). Lo nuevo: cada mensaje entregado enciende una luz en el planeta
 * («Encendiste N luces»), las rondas perfectas (se encontraron todos los que traían mensaje) y la racha mayor de rondas perfectas. El récord es el de luces en una partida (preferencias `satelites_record`, que SÍ van en el respaldo; se manda a
 * Unity en cada partida). No se habla de enfermedades ni se promete nada de salud: va la nota común «No es un diagnóstico».
 */
object Satelites {
  /** Cuántas luces caben a la vista en el planeta de la pantalla final (las demás suman al número, no al dibujo). */
  const val MAX_LIGHT_DOTS = 40

  /** «Encendiste 12 luces» (una sola: «Encendiste 1 luz»; ninguna: «No se encendió ninguna luz»). null sin dato. */
  fun lightsLine(lights: Int?): String? = when {
    lights == null || lights < 0 -> null
    lights == 0 -> "No se encendió ninguna luz"
    lights == 1 -> "Encendiste 1 luz"
    else -> "Encendiste $lights luces"
  }

  /** «Tu seguimiento: 2,6 de 3,5 a la vez» (coma decimal; el segundo número sin decimales si es entero). null si no hubo medida. */
  fun trackingLine(capacity: Float?, targets: Float?): String? {
    if (capacity == null || capacity < 0f) return null
    val cap = String.format(java.util.Locale("es"), "%.1f", capacity)
    val of = if (targets == null || targets <= 0f) "" else " de " + String.format(java.util.Locale("es"), if (targets % 1f == 0f) "%.0f" else "%.1f", targets)
    return "Tu seguimiento: $cap$of a la vez"
  }

  /** «Rondas perfectas: 5 de 8 · racha mayor ×3» (la racha solo desde 2); null sin dato. */
  fun perfectLine(perfect: Int?, rounds: Int?, bestStreak: Int?): String? {
    if (perfect == null || rounds == null || rounds <= 0 || perfect < 0) return null
    val streak = if (bestStreak != null && bestStreak >= 2) " · racha mayor ×$bestStreak" else ""
    return "Rondas perfectas: ${perfect.coerceAtMost(rounds)} de $rounds$streak"
  }

  /** «Tu récord: 22 luces en una partida»; con «¡Récord nuevo!» solo cuando en esta partida se superó el guardado (lo dice Unity: igualarlo no cuenta). null sin récord. */
  fun recordLine(best: Int?, isNew: Boolean?): String? {
    if (best == null || best <= 0) return null
    return if (isNew == true) "¡Récord nuevo: $best luces!" else "Tu récord: $best luces en una partida"
  }

  /** El récord que se guarda: el mayor de lo guardado y lo que trae la partida; nunca baja y nunca es negativo. */
  fun mergeRecord(saved: Int, fromGame: Int?): Int = maxOf(saved.coerceAtLeast(0), (fromGame ?: 0).coerceAtLeast(0))

  /**
   * Cuando se renovó el juego (9-oct) los niveles de Satélites cambiaron: cada uno trae mecánicas nuevas (los anillos que se cruzan de frente desde el nivel 3, las sorpresas desde el 2, la media vuelta desde el 6) y la escalera sigue siendo de 12.
   * El rating guardado (0..1) de quienes ya jugaban se lleva a las tres cuartas partes (`ddaRating × 0,75`, la migración de Room 14 → 15) para que nadie parta en un nivel con mecánicas que aún no vio: el mejor jugador del juego
   * viejo (0,9) empieza en el nivel 9 y el que estaba en el medio (0,5), en el 5. Sin dato (−1) no se toca.
   */
  fun translateOldRating(oldRating: Float): Float = if (oldRating < 0f) oldRating else (oldRating * OLD_TO_NEW_RATIO).coerceIn(0f, 1f)

  const val OLD_TO_NEW_RATIO = 0.75f

  /** Cuántas luces se dibujan de las encendidas. */
  fun dots(lights: Int?): Int = (lights ?: 0).coerceIn(0, MAX_LIGHT_DOTS)

  /** Para lectores de pantalla. */
  fun spoken(lights: Int?, capacity: Float?, targets: Float?): String =
    listOfNotNull(lightsLine(lights), trackingLine(capacity, targets)).joinToString(". ")
}
