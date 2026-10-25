package com.example.data

/**
 * Lo que aporta un juego a los puntos de su área (Tarea 159; `auditoria-fable/avance-b-puntos.md` §2). [progress] = su último avance (0..1, el de [Skill.progress]; 0 si nunca se midió); [points] = sus puntos, ya redondeados de modo que la suma
 * de los juegos del área dé EXACTAMENTE el total del área; [max] = lo máximo que puede aportar (1000 ÷ cuántos juegos tiene el área: 250, o 200 en Atención); [measured] = ya hay una partida que contó para su avance.
 * La etapa del juego usa los mismos tramos de la escala, en proporción: cinco tramos iguales de [max].
 */
data class GamePoints(val gameId: String, val points: Int, val max: Int, val progress: Float, val measured: Boolean) {
  /** Etapa 0..4 (Inicio..Maestro) del juego: quintos de [max]. */
  val stage: Int get() = if (max <= 0) 0 else (points * 5 / max).coerceIn(0, 4)
  val stageName: String get() = Skill.STAGES[stage]

  /** Cuántos puntos del juego faltan para la etapa que sigue (null en Maestro). */
  val toNext: Int? get() = if (stage >= 4) null else (stage + 1) * max / 5 - points
  val nextName: String? get() = if (stage >= 4) null else Skill.STAGES[stage + 1]

  /** Cuánto de la barra se llena, 0..1. */
  val fill: Float get() = if (max <= 0) 0f else (points.toFloat() / max).coerceIn(0f, 1f)

  /** «Etapa Experto · 180 de 250 puntos»: el encabezado de la ficha del juego (la misma forma que la lámina del área: «Etapa Hábil · 470 de 1000 puntos»). */
  val stageLine: String get() = "Etapa $stageName · $points de $max puntos"

  /** «Te faltan 20 puntos para Maestro» (null en Maestro). */
  val toNextLine: String? get() = toNext?.let { n -> if (n == 1) "Te falta 1 punto para $nextName" else "Te faltan $n puntos para $nextName" }

  companion object {
    /**
     * Los puntos de un juego mirado a solas, sin repartir el redondeo con el resto del área (lo usan las pruebas y las capturas; en la app, la ficha recibe los puntos de [AreaPoints.compute], que cuadran con los del área): [progress] (0..1) × 1000 ÷ N, redondeado hacia arriba desde la mitad.
     */
    fun of(gameId: String, progress: Float): GamePoints {
      val area = com.example.model.GameRegistry.getById(gameId)?.domain?.name
      val n = area?.let { AreaPoints.gamesOf(it).size }?.takeIf { it > 0 } ?: 1
      val milli = Math.round(progress.coerceIn(0f, 1f) * 1000f)
      return GamePoints(gameId, ((2 * milli + n) / (2 * n)).coerceAtMost(AreaPoints.SCALE / n), AreaPoints.SCALE / n, milli / 1000f, measured = true)
    }
  }
}

/**
 * Los puntos de un área en un momento (Tarea 159; Ricardo, 22-oct: «puntos por área»). Cada área va de 0 a [SCALE] (1000) y se suma: cada juego aporta hasta 1000 ÷ N (N = los juegos del área) según el escalón de su escalera donde se aciertan 8 de 10
 * ([ProgressPoint.progress]); un juego sin partidas vale 0, así que probar uno nuevo solo puede sumar. La etapa es el tramo fijo de [STAGE_SPAN] (200) donde cae el total: Inicio 0-199 · Aprendiz 200-399 · Hábil 400-599 · Experto 600-799 · Maestro 800-1000.
 * Nunca sale del porcentaje de aciertos ni del tiempo de juego, y no baja por dejar de jugar. [weekDelta] = el cambio desde el lunes a las 00:00 ([Week.weekStart]); [best] = el máximo del área que quedó anotado (la «mejor marca»).
 * Lógica pura con pruebas (`AreaPointsTest`); toda la app lee ESTA escala (la lámina de Hoy, Avance y la ficha de un juego).
 */
data class AreaPoints(
  val area: String,
  val total: Int,
  val perGame: List<GamePoints>,
  val weekDelta: Int,
  val best: Int
) {
  /** Etapa 0..4 (Inicio..Maestro) del área. */
  val stage: Int get() = stageOf(total)
  val stageName: String get() = Skill.STAGES[stage]

  /** Cuántos puntos faltan para la etapa que sigue (null en Maestro) y cómo se llama. */
  val toNext: Int? get() = if (stage >= 4) null else (stage + 1) * STAGE_SPAN - total
  val nextName: String? get() = if (stage >= 4) null else Skill.STAGES[stage + 1]

  /** Cuánto de la barra se llena, 0..1. */
  val fill: Float get() = (total.toFloat() / SCALE).coerceIn(0f, 1f)

  /** Lo que tenías el lunes a las 00:00. */
  val before: Int get() = total - weekDelta

  /** Hay alguna partida que contó para el avance de este área. */
  val measured: Boolean get() = perGame.any { it.measured }

  /** Cuánto aporta cada juego como máximo (250; 200 en Atención). */
  val maxPerGame: Int get() = perGame.firstOrNull()?.max ?: 0

  /** El total quedó por debajo de la mejor marca (la bajada de una partida no borra lo ganado: se dice con la mejor marca al lado). */
  val belowBest: Boolean get() = best > total

  /** Barra para [StageScale]/[KitBar]: el churro de hace una semana y lo de esta semana, en la misma escala 0..1. */
  val change: Float get() = weekDelta.toFloat() / SCALE

  companion object {
    /** El área entera vale hasta 1000 puntos. */
    const val SCALE = 1000

    /** Cada etapa es un tramo de 200 puntos. */
    const val STAGE_SPAN = 200

    /** Cuántas semanas muestra el gráfico del área, y cuántas hacen falta para dibujarlo. */
    const val CHART_WEEKS = 8
    const val CHART_MIN_WEEKS = 4

    fun stageOf(total: Int): Int = (total / STAGE_SPAN).coerceIn(0, 4)

    /** Los juegos de un área (`MEMORIA`…) en el orden del registro (los retirados no están). */
    fun gamesOf(areaKey: String): List<String> = com.example.model.GameRegistry.allGames.filter { it.domain.name == areaKey }.map { it.id }

    /** Los puntos de cada una de las [areaKeys], en ese orden: lo que leen Hoy, «Todos los juegos» y Avance. */
    fun forAreas(areaKeys: List<String>, log: List<ProgressPoint>, now: Long): List<AreaPoints> = areaKeys.map { compute(it, gamesOf(it), log, now) }

    /**
     * Los puntos de [area] en [now]. [games] = los juegos del área (en el orden del registro); [log] = `progress_log` (los puntos de avance de cada juego a lo largo del tiempo); [weekStart] = desde cuándo se mide el cambio (el lunes de esta semana).
     * El avance de un juego en un momento es su último punto anotado hasta entonces (sin ninguno, 0).
     */
    fun compute(area: String, games: List<String>, log: List<ProgressPoint>, now: Long, weekStart: Long = Week.weekStart(now)): AreaPoints {
      val milli = milliAt(games, log, now)
      val total = totalOf(milli.values.sum(), games.size)
      val before = totalOf(milliAt(games, log, weekStart).values.sum(), games.size)
      val max = if (games.isEmpty()) 0 else SCALE / games.size
      val points = distribute(games, milli, total)
      val per = games.map { id ->
        val m = milli.getValue(id)
        GamePoints(id, points.getValue(id), max, m / 1000f, measured = log.any { it.gameId == id && it.time <= now })
      }
      return AreaPoints(area, total, per, total - before, maxOf(total, bestOf(games, log, now)))
    }

    /**
     * Los puntos del área al cierre de cada una de las últimas [weeks] semanas (la última = hoy), de la más vieja a la más nueva: el cierre de una semana es el lunes a las 00:00 que la sigue ([Week.weekStart]); una semana sin partidas repite el valor anterior.
     * Solo cuenta desde la semana de la primera partida del área: con menos de [CHART_MIN_WEEKS] semanas hay menos valores (y el gráfico no se dibuja, `weeklyLine`); sin partidas, la lista queda vacía.
     */
    fun weekly(games: List<String>, log: List<ProgressPoint>, now: Long, weeks: Int = CHART_WEEKS): List<Int> {
      val first = log.filter { it.gameId in games && it.time <= now }.minOfOrNull { it.time } ?: return emptyList()
      var firstBack = 0
      while (firstBack < weeks - 1 && Week.weekStart(now, firstBack) > first) firstBack += 1
      return (firstBack downTo 0).map { back ->
        if (back == 0) totalOf(milliAt(games, log, now).values.sum(), games.size)
        else totalOf(milliAt(games, log, Week.weekStart(now, back - 1)).values.sum(), games.size)
      }
    }

    /** Un avance 0..1 en milésimas enteras (lo que guarda `progress_log`). */
    private fun milli(progress: Float): Int = Math.round(progress.coerceIn(0f, 1f) * 1000f)

    /** El avance de cada juego en [t] en milésimas (0..1000): el último punto anotado hasta ese momento; sin ninguno, 0. Conserva el orden de [games]. */
    private fun milliAt(games: List<String>, log: List<ProgressPoint>, t: Long): Map<String, Int> {
      val last = HashMap<String, ProgressPoint>()
      for (p in log) if (p.time <= t && p.gameId in games) { val prev = last[p.gameId]; if (prev == null || p.time >= prev.time) last[p.gameId] = p }
      return games.associateWith { id -> last[id]?.let { milli(it.progress) } ?: 0 }
    }

    /** El total en puntos entero: la suma de las milésimas ÷ N (cada juego aporta milésimas ÷ N), redondeada hacia arriba desde la mitad. */
    private fun totalOf(sumMilli: Int, n: Int): Int = if (n <= 0) 0 else ((2 * sumMilli + n) / (2 * n)).coerceIn(0, SCALE)

    /** Reparte [total] entre los juegos por el método del mayor resto: cada uno recibe su parte entera y los puntos que sobran van a los de mayor resto (a igual resto, el primero del registro). */
    private fun distribute(games: List<String>, milli: Map<String, Int>, total: Int): Map<String, Int> {
      val n = games.size
      if (n == 0) return emptyMap()
      val floors = games.associateWith { milli.getValue(it) / n }
      var left = total - floors.values.sum()
      val order = games.withIndex().sortedWith(compareByDescending<IndexedValue<String>> { milli.getValue(it.value) % n }.thenBy { it.index }).map { it.value }
      val out = floors.toMutableMap()
      var i = 0
      while (left > 0) { out[order[i % n]] = out.getValue(order[i % n]) + 1; left -= 1; i += 1 }
      return games.associateWith { out.getValue(it) }
    }

    /** El máximo que alcanzó el total del área en toda la historia anotada hasta [now] (tras cada punto de avance del área). */
    private fun bestOf(games: List<String>, log: List<ProgressPoint>, now: Long): Int {
      val running = HashMap<String, Int>()
      var best = 0
      for (p in log.filter { it.gameId in games && it.time <= now }.sortedBy { it.time }) {
        running[p.gameId] = milli(p.progress)
        best = maxOf(best, totalOf(running.values.sum(), games.size))
      }
      return best
    }

    // --- Las frases ---

    /** «1 punto», «12 puntos». */
    fun pointsText(n: Int): String = if (n == 1) "1 punto" else "$n puntos"

    /**
     * La frase de la semana de un área, para su fila de Avance: «Esta semana sumaste 12 puntos» / «Esta semana te mantuviste» / «Esta semana bajaste 8 puntos. Tu mejor marca sigue en 520 puntos.» Sin ninguna partida: invita («Todavía sin puntos: juega un juego de Memoria»).
     * Nunca «perdiste», nunca flechas ni signos de más o menos.
     */
    fun weekLine(p: AreaPoints, areaName: String, period: String = "Esta semana"): String = when {
      !p.measured -> "Todavía sin puntos: juega un juego de $areaName."
      p.weekDelta > 0 -> "$period sumaste ${pointsText(p.weekDelta)}."
      p.weekDelta < 0 -> "$period bajaste ${pointsText(-p.weekDelta)}. Tu mejor marca sigue en ${pointsText(p.best)}."
      else -> "$period te mantuviste."
    }

    /** Lo mismo para el resumen de la ruta de hoy, que mide desde su comienzo: «Hoy sumaste 12 puntos.» (el cambio de [AreaPoints] se calculó «desde» el comienzo de la ruta). */
    fun todayLine(p: AreaPoints, areaName: String): String = weekLine(p, areaName, period = "Hoy")

    /**
     * Lo mismo en la página del área, junto a lo que falta: «Te faltan 130 puntos para Experto. Esta semana sumaste 12 puntos.»; en Maestro, «Llegaste al último tramo. …». Si bajó, con la mejor marca al lado, sin rojo ni flechas («Esta semana bajaste 8 puntos. Tu mejor marca sigue en 520 puntos.»:
     * desde la Tarea 174 la fila de Avance ya no lleva la frase de la semana y lo que decía vive aquí). Sin puntos todavía, una invitación.
     */
    fun toNextSentence(p: AreaPoints, areaName: String): String {
      if (!p.measured) return "Todavía sin puntos en $areaName: cada juego suma desde su primera partida a tu medida."
      val first = p.toNext?.let { if (it == 1) "Te falta 1 punto para ${p.nextName}." else "Te faltan $it puntos para ${p.nextName}." } ?: "Llegaste al último tramo."
      val second = when {
        p.weekDelta > 0 -> "Esta semana sumaste ${pointsText(p.weekDelta)}."
        p.weekDelta < 0 -> "Esta semana bajaste ${pointsText(-p.weekDelta)}. Tu mejor marca sigue en ${pointsText(p.best)}."
        else -> "Esta semana te mantuviste."
      }
      return "$first $second"
    }

    /** «Etapa Hábil · 470 de 1000 puntos» (la lámina de Hoy), o «Aún sin medir» si todavía no hay ninguna partida. */
    fun stageLine(p: AreaPoints): String = if (p.measured) "Etapa ${p.stageName} · ${p.total} de $SCALE puntos" else "Aún sin medir"

    /** Lo que lee el lector de pantalla del CUADRO de un área en Avance (Tarea 174), que solo muestra el nombre, los puntos y la barra: «Memoria, 470 de 1000 puntos»; sin medir, «Memoria, aún sin medir». */
    fun cardSpoken(p: AreaPoints, areaName: String): String = if (p.measured) "$areaName, ${p.total} de $SCALE puntos" else "$areaName, aún sin medir"

    /** «Memoria: etapa Hábil, 470 de 1000 puntos, esta semana sumaste 12 puntos»: lo que lee el lector de pantalla de una fila. */
    fun spoken(p: AreaPoints, areaName: String): String = when {
      !p.measured -> "$areaName: aún sin puntos"
      else -> "$areaName: etapa ${p.stageName}, ${p.total} de $SCALE puntos. " + weekLine(p, areaName).removeSuffix(".")
    }

    /** Lo que dice el gráfico cuando todavía no hay [CHART_MIN_WEEKS] semanas de datos: «Llevas 2 semanas en Memoria. El gráfico aparece cuando tengas 4.» */
    fun weeklyLine(weeksOfData: Int, areaName: String): String = when (weeksOfData) {
      0 -> "Todavía no hay semanas en $areaName. El gráfico aparece cuando tengas $CHART_MIN_WEEKS."
      1 -> "Llevas 1 semana en $areaName. El gráfico aparece cuando tengas $CHART_MIN_WEEKS."
      else -> "Llevas $weeksOfData semanas en $areaName. El gráfico aparece cuando tengas $CHART_MIN_WEEKS."
    }

    /** La ayuda bajo «Tus juegos de Memoria»: «Cada juego aporta hasta 250 puntos. Suben cuando llegas a niveles más altos.» */
    fun gamesHint(maxPerGame: Int): String = "Cada juego aporta hasta $maxPerGame puntos. Suben cuando llegas a niveles más altos."

    /** La línea fija que acompaña a las bajadas del mes (Fable §2.4): los puntos suben y bajan un poco de una partida a otra. */
    const val UPS_AND_DOWNS = "Los puntos suben y bajan un poco de una partida a otra. Lo que cuenta es la etapa."
  }
}
