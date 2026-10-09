package com.example.data

/**
 * «Piloto Estelar: la ruta de las balizas» (id `piloto`, renovado el 9-oct; Unity: `Games/Piloto/PilotContract.cs`; diseño en docs/diseno-piloto.md): la lectura de lo que manda Unity para la pantalla final. Lógica pura con pruebas.
 *
 * TODO se mide con las dos tareas a la vez (pilotar y atrapar las señales de la misión): no hay una medida de una tarea sola ni un «costo de multitarea» (regla permanente 1 de Ricardo, 9-oct). La medida propia es
 * «Tus señales a los mandos» = (señales de la misión atrapadas − toques equivocados) / señales de la misión, y solo se muestra con 8 o más señales de la misión (`pil_signal_pct` llega en −1 si no). Va a StarMeasures con la clave `mandos`
 * (los puntos viejos de `multitask` quedan guardados sin leerse). Va la nota común «No es un diagnóstico».
 */
object Piloto {
  /** Señales de la misión que hacen falta para mostrar «Tus señales a los mandos». */
  const val MIN_TARGETS = 8

  const val MAX_LEVEL = 9

  /** El valor de la medida para StarMeasures (0..100); null si Unity no la mandó (menos de 8 señales de la misión). */
  fun mark(signalPct: Int?): Float? = signalPct?.takeIf { it in 0..100 }?.toFloat()

  /** «Tus señales a los mandos: 71 %»; null sin medida. */
  fun measureLine(signalPct: Int?): String? = signalPct?.takeIf { it in 0..100 }?.let { "Tus señales a los mandos: $it %" }

  /** Cómo leer la medida, en palabras de todos los días. */
  const val MEASURE_EXPLANATION =
    "Cuántas señales de tu misión atrapaste, descontando los toques equivocados, mientras pilotabas. Todo se mide con las dos cosas a la vez. Con práctica suele subir."

  /** Cuando hubo menos de 8 señales de la misión no se dice una proporción (con pocos casos no significa nada). */
  const val TOO_FEW_LINE = "Faltaron señales de tu misión para medir tus señales a los mandos (se necesitan 8)."

  /** «En la ruta: 82 %»; null sin dato. */
  fun laneLine(lanePct: Int?): String? = lanePct?.takeIf { it in 0..100 }?.let { "En la ruta: $it %" }

  /** «Señales de tu misión: 14 de 18»; null sin dato. */
  fun missionLine(hits: Int?, targets: Int?): String? {
    if (hits == null || targets == null || targets < 0 || hits < 0) return null
    return "Señales de tu misión: ${hits.coerceAtMost(targets)} de $targets"
  }

  /** «Toques equivocados: 2»; null sin dato. */
  fun wrongLine(falseAlarms: Int?): String? = falseAlarms?.takeIf { it >= 0 }?.let { "Toques equivocados: $it" }

  /** «Tu nivel de señales: 6 de 9»; null sin dato. */
  fun levelLine(level: Int?): String? = level?.takeIf { it in 1..MAX_LEVEL }?.let { "Tu nivel de señales: $it de $MAX_LEVEL" }

  /** «Racha mayor ×7» (solo desde 2); null sin dato. */
  fun streakLine(best: Int?): String? = best?.takeIf { it >= 2 }?.let { "Racha mayor ×$it" }

  /** Los puntos de la partida («1.250 puntos», con el punto de miles como se escribe en español); null sin dato. */
  fun pointsLine(points: Int?): String? = points?.takeIf { it >= 0 }?.let {
    java.text.NumberFormat.getIntegerInstance(java.util.Locale("es", "CL")).format(it) + (if (it == 1) " punto" else " puntos")
  }

  /**
   * Un consejo concreto cuando se puede (nunca una conclusión de pocos casos): primero los toques equivocados, después la ruta, después las señales que se fueron. null si no hay nada que decir.
   * Hace falta una partida con señales de la misión resueltas ([MIN_TARGETS]) para aconsejar sobre ellas.
   */
  fun advice(hits: Int?, targets: Int?, falseAlarms: Int?, lanePct: Int?): String? {
    if (hits == null || targets == null || falseAlarms == null || lanePct == null || targets < MIN_TARGETS) return null
    return when {
      falseAlarms >= maxOf(3, targets / 4) -> "Tocaste varias señales que no eran de tu misión. Antes de tocar, mira el detalle: punto, anillo o franja."
      lanePct < 70 -> "La nave se salió bastante de la ruta. Mira un poco más lejos que la nave: las balizas de adelante te dicen hacia dónde ir."
      hits * 100 < targets * 60 -> "Se te fueron varias señales de tu misión. Cuando cambie el sector, mira la tarjeta de misión de arriba."
      else -> null
    }
  }

  /** Para lectores de pantalla. */
  fun spoken(signalPct: Int?, lanePct: Int?, hits: Int?, targets: Int?): String =
    listOfNotNull(measureLine(signalPct), laneLine(lanePct), missionLine(hits, targets)).joinToString(". ")
}
