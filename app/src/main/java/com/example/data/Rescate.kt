package com.example.data

/**
 * «Rescate relámpago: qué cápsulas viste» (id `radar`, renovado el 9-oct; Unity: `Games/Radar/RadarContract.cs`; diseño en docs/diseno-rescate.md): la lectura de lo que manda Unity para la pantalla final y el récord que se guarda. Lógica pura con pruebas.
 *
 * Se responde QUÉ se vio, nunca DÓNDE: no hay ninguna medida por lugar («Tu radar» y «Tu filtro» se borraron; regla permanente 2 de Ricardo). Las medidas: «Tu vistazo» (el destello más breve con que se rescatan casi todas, en ms reales) y «Tu captura» (cuántas cápsulas
 * se nombran bien de un vistazo, de 4: promedio de aciertos − 2 × elegidas que no estaban en las lluvias; ya no se compara con la referencia de adultos). El premio, que no es una medida, son las cápsulas rescatadas y su récord (preferencias `rescate_record`, que SÍ van
 * en el respaldo; se manda a Unity en cada partida). Va la nota común «No es un diagnóstico».
 */
object Rescate {
  /** El techo de «Tu captura» (una lluvia son 4 cápsulas). */
  const val CAPTURE_MAX = 4

  /** Cuántas cápsulas se dibujan en el final (5 filas de [ROW]); la cantidad exacta va siempre en el número de arriba. */
  const val MAX_DOTS = 40
  const val ROW = 8

  /** Cápsulas que se dibujan: nunca negativas ni más de [MAX_DOTS]. */
  fun dots(rescued: Int?): Int = (rescued ?: 0).coerceIn(0, MAX_DOTS)

  /** «23 cápsulas a salvo» (una sola: «1 cápsula a salvo»; ninguna: «No se rescató ninguna cápsula»). null sin dato. */
  fun rescuedLine(rescued: Int?): String? = when {
    rescued == null || rescued < 0 -> null
    rescued == 0 -> "No se rescató ninguna cápsula"
    rescued == 1 -> "1 cápsula a salvo"
    else -> "$rescued cápsulas a salvo"
  }

  /** «Tu captura: 3,5 de 4» (coma decimal); null si no hubo medida (menos de 2 lluvias). */
  fun captureLine(capture: Float?): String? =
    if (capture == null || capture < 0f) null else "Tu captura: ${String.format(java.util.Locale("es"), "%.1f", capture.coerceAtMost(CAPTURE_MAX.toFloat()))} de $CAPTURE_MAX"

  /** Cómo leer «Tu captura». */
  const val CAPTURE_EXPLANATION =
    "Cuántas cápsulas nombras bien de un vistazo cuando el destello no apura (en las lluvias de cápsulas). Elegir una que no estaba descuenta el doble, así que adivinar no suma."

  /** «Tu vistazo: 280 ms»; null sin dato. */
  fun glanceLine(ms: Int?): String? = if (ms == null || ms <= 0) null else "Tu vistazo: $ms ms"

  /** Cómo leer «Tu vistazo», con cuántas cápsulas a la vez (si Unity lo mandó). */
  fun glanceExplanation(load: Float?): String {
    val n = load?.takeIf { it > 0f }?.let { String.format(java.util.Locale("es"), if (it % 1f == 0f) "%.0f" else "%.1f", it) }
    val with = if (n != null) "con $n cápsulas a la vez, " else ""
    return "El destello más breve con el que, ${with}las rescatas casi todas unas 4 de cada 5 veces. Mientras menos milisegundos, más rápido captas."
  }

  /** «Destello más corto resuelto: 150 ms» (el de una ronda perfecta); null si ninguna lo fue. */
  fun shortestLine(ms: Int?): String? = if (ms == null || ms <= 0) null else "Destello más corto resuelto: $ms ms"

  /** «Rondas perfectas: 5 de 12 · racha mayor ×3» (la racha solo desde 2); null sin dato. */
  fun perfectLine(perfect: Int?, rounds: Int?, bestStreak: Int?): String? {
    if (perfect == null || rounds == null || rounds <= 0 || perfect < 0) return null
    val streak = if (bestStreak != null && bestStreak >= 2) " · racha mayor ×$bestStreak" else ""
    return "Rondas perfectas: ${perfect.coerceAtMost(rounds)} de $rounds$streak"
  }

  /** «Tu récord: 31 cápsulas en una partida»; con «¡Récord nuevo!» solo cuando en esta partida se superó el guardado (lo dice Unity: igualarlo no cuenta). null sin récord. */
  fun recordLine(best: Int?, isNew: Boolean?): String? {
    if (best == null || best <= 0) return null
    return if (isNew == true) "¡Récord nuevo: $best ${if (best == 1) "cápsula" else "cápsulas"}!" else "Tu récord: $best ${if (best == 1) "cápsula" else "cápsulas"} en una partida"
  }

  /** El récord que se guarda: el mayor de lo guardado y lo que trae la partida; nunca baja y nunca es negativo. */
  fun mergeRecord(saved: Int, fromGame: Int?): Int = maxOf(saved.coerceAtLeast(0), (fromGame ?: 0).coerceAtLeast(0))

  /** «Viajes a la estación: 2» (los de esta partida: cada vez que la nave se llena viaja a la estación y vuelve vacía); null si no hubo ninguno o sin dato. */
  fun tripsLine(trips: Int?): String? = if (trips == null || trips <= 0) null else "Viajes a la estación: $trips"

  /** Lo que se suma partida a partida (preferencias `rescate_record`, que van en el respaldo): las cápsulas rescatadas y los viajes a la estación de toda la vida. */
  data class Totals(val rescued: Int = 0, val trips: Int = 0)

  /** Los totales después de una partida: lo guardado más lo de la partida; nunca bajan ni son negativos (un dato que no vino suma 0). */
  fun addTotals(saved: Totals, rescued: Int?, trips: Int?): Totals =
    Totals(saved.rescued.coerceAtLeast(0) + (rescued ?: 0).coerceAtLeast(0), saved.trips.coerceAtLeast(0) + (trips ?: 0).coerceAtLeast(0))

  /** «En total: 342 cápsulas y 12 viajes a la estación» (en singular cuando es uno; sin viajes: solo las cápsulas); null si todavía no hay nada. */
  fun totalLine(totals: Totals?): String? {
    if (totals == null || (totals.rescued <= 0 && totals.trips <= 0)) return null
    val capsules = if (totals.rescued == 1) "1 cápsula" else "${totals.rescued} cápsulas"
    val trips = if (totals.trips == 1) "1 viaje a la estación" else "${totals.trips} viajes a la estación"
    return "En total: " + when {
      totals.trips <= 0 -> capsules
      totals.rescued <= 0 -> trips
      else -> "$capsules y $trips"
    }
  }

  /** Para lectores de pantalla. */
  fun spoken(rescued: Int?, capture: Float?, glanceMs: Int?): String =
    listOfNotNull(rescuedLine(rescued), captureLine(capture), glanceLine(glanceMs)).joinToString(". ")
}
