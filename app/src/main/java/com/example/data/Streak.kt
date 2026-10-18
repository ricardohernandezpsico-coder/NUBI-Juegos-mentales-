package com.example.data

/**
 * La racha que perdona (Tarea 124; Fable, `auditoria-fable/spec-avance-juegos.md` §1.9). Lógica pura, derivada del historial: no guarda nada y no hay estado nuevo.
 *
 * Los días son números de día locales (`localDay`: días desde 1970 en la zona horaria de la persona), así que sirve para cualquier zona.
 *
 * Reglas:
 * 1. **Ancla:** hoy si hubo partida hoy; si no, ayer (hoy no rompe la racha hasta que termine el día: igual que antes).
 * 2. Se camina hacia atrás desde el ancla. Un día SIN partida se PERDONA si en los 6 días siguientes a él (hacia hoy) no hay otro día perdonado: a lo más **un perdón por cada 7 días corridos**. Un segundo hueco en esa ventana corta la racha.
 * 3. **Decisión:** el día perdonado NO suma. [StreakInfo.current] cuenta solo días jugados, así que «Racha: 5 días» siempre son 5 días con partida (L-M-X-(J perdonado)-V es una racha de 4).
 * 4. Un perdón solo cuenta si de verdad alargó la racha (si después del hueco hay un día jugado); un hueco al final de la racha, sin nada jugado antes, no es un perdón.
 * 5. **Nadie baja:** sigue el mismo camino que la racha estricta hasta el primer hueco y solo puede seguir más lejos, así que es SIEMPRE ≥ la racha estricta.
 *
 * Compatibilidad con lo que venga (`docs/modelo-negocio.md`): una moneda podría comprar un segundo perdón en la misma ventana, y la «reparación» restaurar una racha rota hace ≤ 7 días; ninguna cambia esta función (solo le pasarían días extra como «jugados a efectos de la racha»).
 */
object Streak {
  /** Una racha: [current] días jugados; [forgivenDay] = el hueco perdonado más reciente de los últimos 7 días (para «Descansaste el martes»), o null; [graceAvailable] = no hubo perdón en los últimos 7 días (queda uno disponible). */
  data class StreakInfo(val current: Int, val forgivenDay: Long?, val graceAvailable: Boolean)

  /** Un perdón por cada 7 días corridos. */
  const val WINDOW = 7

  /** Nunca se camina más de esto hacia atrás (diez años): una cota para que una base de datos rara no cuelgue la pantalla. */
  private const val MAX_BACK_DAYS = 3660

  private const val DAY_MS = 24L * 60 * 60 * 1000

  /** El número de día local (días desde 1970 en la zona horaria de la persona) de una marca de tiempo: el mismo que usan los logros. */
  fun dayOf(timestamp: Long): Long = (timestamp + java.util.TimeZone.getDefault().getOffset(timestamp)) / DAY_MS

  /** La racha a partir de las marcas de tiempo de las partidas y la hora de ahora. */
  fun fromTimestamps(timestamps: Collection<Long>, now: Long): StreakInfo = compute(timestamps.map(::dayOf).toSet(), dayOf(now))

  /** La racha que termina HOY (o ayer, si hoy todavía no se jugó) con [playedDays] los días con partida. */
  fun compute(playedDays: Set<Long>, today: Long): StreakInfo {
    if (playedDays.isEmpty()) return StreakInfo(0, null, true)
    var d = if (today in playedDays) today else today - 1
    val oldest = d - MAX_BACK_DAYS
    var count = 0
    var pending: Long? = null              // un hueco perdonado que todavía no se sabe si alargó la racha
    val forgiven = ArrayList<Long>()       // los perdones confirmados, del más reciente al más viejo
    while (d >= oldest) {
      if (d in playedDays) {
        count++
        pending?.let { forgiven += it; pending = null }
      } else {
        val nextGrace = forgiven.lastOrNull()      // el perdón confirmado más cercano a este hueco (el último que se encontró caminando hacia atrás)
        // se perdona si en los 6 días siguientes a este (hacia hoy) no hubo otro perdón, y si el hueco de antes ya se resolvió (dos huecos seguidos cortan)
        if (pending != null || (nextGrace != null && nextGrace - d < WINDOW)) break
        pending = d
      }
      d--
    }
    val recent = forgiven.firstOrNull()?.takeIf { today - it < WINDOW }
    return StreakInfo(count, recent, recent == null)
  }

  /** La racha más larga de siempre (con la misma regla): la que miran los logros `racha_N`. Siempre ≥ la estricta ([longestStrict]). */
  fun longest(playedDays: Collection<Long>): Int {
    if (playedDays.isEmpty()) return 0
    val all = playedDays.toSet()
    var best = 0
    for (anchor in all) {
      val upTo = all.filterTo(HashSet()) { it <= anchor }
      best = maxOf(best, compute(upTo, anchor).current)
    }
    return best
  }

  /** La racha ESTRICTA más larga (días seguidos, sin perdones): la de antes; solo para comparar. */
  fun longestStrict(playedDays: Collection<Long>): Int {
    if (playedDays.isEmpty()) return 0
    val sorted = playedDays.toSortedSet().toList()
    var best = 1
    var run = 1
    for (i in 1 until sorted.size) {
      run = if (sorted[i] == sorted[i - 1] + 1) run + 1 else 1
      if (run > best) best = run
    }
    return best
  }

  private val WEEKDAYS = listOf("jueves", "viernes", "sábado", "domingo", "lunes", "martes", "miércoles")

  /** El nombre del día de la semana de un número de día (el día 0, 1-ene-1970, fue jueves). */
  fun weekdayName(day: Long): String = WEEKDAYS[Math.floorMod(day, 7L).toInt()]

  /** «Descansaste el martes: tu racha sigue.» si hubo un perdón en los últimos 7 días; si no, null. */
  fun forgivenLine(info: StreakInfo): String? = info.forgivenDay?.let { "Descansaste el ${weekdayName(it)}: tu racha sigue." }

  /** La explicación fija que va bajo la racha. */
  const val REST_LINE = "Un día de descanso por semana no la corta."
}
