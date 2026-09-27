package com.example.data

/**
 * Lectura del final de "Correo Estelar" (Unity: `Games/Correo/MailContract.cs`): "tu memoria para lo pendiente"
 * (memoria prospectiva), separando los encargos por LUGAR (un planeta que pasa avisa) de los encargos por HORA (la radio:
 * nada avisa, hay que acordarse solo y mirar el reloj). Con pocos datos no se dice nada (misma regla que el resto de
 * las medidas de los juegos estrella).
 */
object Mail {
  /** Encargos de cada tipo que hacen falta para comparar lugar y hora. */
  const val MIN_TO_COMPARE = 3
  /** Diferencia (proporción) desde la que se dice que un tipo se pasó más que el otro. */
  const val COMPARE_GAP = 0.25f
  /** Horas de radio que hacen falta para hablar de cómo se usó el reloj. */
  const val MIN_RADIO_FOR_CLOCK = 2

  fun rate(hits: Int, total: Int): Float? = if (total <= 0) null else hits.toFloat() / total

  /** Planetas tocados por error, dicho sin culpa ("de color parecido" si los hubo). */
  fun commissionMessage(commissions: Int, lures: Int): String? {
    if (commissions <= 0) return null
    val base = if (commissions == 1) "Tocaste 1 planeta que no era del encargo" else "Tocaste $commissions planetas que no eran del encargo"
    return when {
      lures == commissions -> "$base: todos de color parecido. Mirar un instante más antes de tocar ayuda."
      lures > 0 -> "$base ($lures de color parecido)."
      else -> "$base."
    }
  }

  /**
   * Cómo se usó el reloj tapado. Lo que más ayuda en los encargos por hora es mirar poco al principio y más a medida
   * que se acerca la hora (el patrón clásico de las tareas de memoria prospectiva por tiempo).
   */
  fun clockMessage(checks: Int, late: Int, radioTotal: Int): String? {
    if (radioTotal < MIN_RADIO_FOR_CLOCK || checks < 0) return null
    return when {
      checks == 0 -> "No miraste el reloj. Para los encargos por hora, mirarlo cuando se acerca la hora ayuda mucho."
      late * 2 >= checks -> "Miraste el reloj $checks ${veces(checks)}, sobre todo justo antes de la hora: es la estrategia que más ayuda."
      checks > 3 * radioTotal -> "Miraste el reloj $checks veces, repartidas en todo el vuelo. Basta con mirarlo más cuando se acerca la hora."
      else -> "Miraste el reloj $checks ${veces(checks)}. Truco: mira poco al principio y más a medida que se acerca la hora."
    }
  }

  private fun veces(n: Int) = if (n == 1) "vez" else "veces"

  /**
   * Lugar contra hora, con un consejo para el día a día. Solo con [MIN_TO_COMPARE] encargos de cada tipo y
   * [COMPARE_GAP] de diferencia.
   */
  fun compareMessage(eventHits: Int, eventTotal: Int, radioHits: Int, radioTotal: Int): String? {
    if (eventTotal < MIN_TO_COMPARE || radioTotal < MIN_TO_COMPARE) return null
    val ev = eventHits.toFloat() / eventTotal
    val ti = radioHits.toFloat() / radioTotal
    return when {
      ev - ti >= COMPARE_GAP ->
        "Los encargos por hora se te pasaron más que los de lugar. Es lo esperable: la hora no avisa. En tu día, conviértelos en encargos de lugar: por ejemplo, deja el remedio junto al cepillo de dientes."
      ti - ev >= COMPARE_GAP ->
        "Los encargos por lugar se te pasaron más que los de hora. Truco: antes de salir, imagina el momento y dilo por dentro: «cuando vea el planeta, lo toco»."
      else -> null
    }
  }
}
