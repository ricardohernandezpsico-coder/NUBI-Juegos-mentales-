package com.example.data

import com.example.model.GameDefinition
import com.example.model.GamePlayResult
import java.util.TimeZone
import kotlin.math.roundToInt

/**
 * Una ficha de «Tus juegos aquí» en la lámina de un lugar: el juego y cuándo se jugó. [daysAgo] = días desde su última partida (null = nunca se jugó); la lámina solo dice eso ([whenShort], [spoken]) y NO la cifra de la medida del juego
 * (esa vive en la ficha del juego, con su explicación: Tarea 143). [detail] = el dato de siempre («Jugado ayer», «Sin jugar aún», su marca…), que ya no se dibuja pero se conserva para las pruebas y los lectores de la lógica.
 */
data class PlaceGameRow(val gameId: String, val title: String, val detail: String, val daysAgo: Int? = null) {
  /** «hoy», «ayer», «hace 3 días» o «nuevo» (nunca se jugó): la línea corta de la ficha. */
  val whenShort: String get() = PlaceSheets.whenShort(daysAgo)

  /** Lo mismo para el lector de pantalla: «jugado hoy», «jugado ayer», «jugado hace 3 días» o «sin jugar aún». */
  val spoken: String get() = PlaceSheets.whenSpoken(daysAgo)
}

/**
 * Todo lo que dice la lámina que se abre al tocar un edificio de la isla (Tarea 85; boceto «A» de Hoy, aprobado por Ricardo el 12-oct): el AVANCE de esa área y sus juegos. Es solo texto y números (ya calculados), así que la lámina se prueba y se captura con datos fijos.
 * [area] = `MEMORIA`…; [key] = el lugar en `isla.json` (`observatorio`…); [stageIndex]/[stageName]/[points] son null si el área aún no se mide.
 */
data class PlaceSheetUi(
  val area: String,
  val key: String,
  val name: String,
  /** «el Observatorio», «la Antena»: para «Jugar en el Observatorio». */
  val article: String,
  val areaName: String,
  val stageIndex: Int?,
  val stageName: String?,
  val points: Int?,
  /** Cuánto de la barra se llena, 0..1. */
  val fill: Float,
  val changeLine: String,
  val toNextLine: String?,
  /** El avance al final de las últimas 4 semanas (la última = hoy), de 0 a 100; null = sin dato. */
  val weeks: List<Int?>,
  /** Cuánto cambió el avance esta semana (de -1 a 1, en la escala 0..1 de la barra): la barra del kit lo dibuja como un tramo más claro. */
  val change: Float = 0f,
  val games: List<PlaceGameRow>,
  val chosenGameId: String?,
  /** «Nubi elige Bodega de carga: aún no la juegas». */
  val chosenLine: String?,
  /** Lo que dice Nubi en su globo, dentro de la lámina (Tarea 147), en primera persona: lo que avanzaste esta semana y lo que te falta. */
  val bubble: String
) {
  val measured: Boolean get() = stageIndex != null
}

/** Arma [PlaceSheetUi] con la lógica que ya tenía el detalle de área de Hoy (`AreaProgress`, `Skill`, `StarMeasures`): nada nuevo que medir. */
object PlaceSheets {
  private const val DAY_MS = 86_400_000L
  private fun dayIndex(ts: Long): Long = (ts + TimeZone.getDefault().getOffset(ts)) / DAY_MS

  /** «Jugado hoy», «Jugado ayer» o «Hace N días que no lo juegas» (la leyenda de Nubi al pie de la lámina). */
  fun playedAgo(now: Long, last: Long): String = when (val days = (dayIndex(now) - dayIndex(last)).toInt()) {
    0 -> "Jugado hoy"
    1 -> "Jugado ayer"
    else -> "Hace $days días que no lo juegas"
  }

  /** La línea corta de una ficha de juego en la lámina: «hoy», «ayer», «hace N días»; «nuevo» si nunca se jugó ([daysAgo] nulo). */
  fun whenShort(daysAgo: Int?): String = when (daysAgo) {
    null -> "nuevo"
    0 -> "hoy"
    1 -> "ayer"
    else -> "hace $daysAgo días"
  }

  /** Lo que dice el lector de pantalla de esa línea: «jugado hoy», «jugado ayer», «jugado hace N días», «sin jugar aún». */
  fun whenSpoken(daysAgo: Int?): String = when (daysAgo) {
    null -> "sin jugar aún"
    0 -> "jugado hoy"
    1 -> "jugado ayer"
    else -> "jugado hace $daysAgo días"
  }

  /** El dato corto de una fila de juego: «Jugado hoy», «Jugado ayer» o «Hace N días» (la fila es angosta y la frase larga se partía; la leyenda de abajo conserva la larga). */
  fun playedAgoShort(now: Long, last: Long): String = when (val days = (dayIndex(now) - dayIndex(last)).toInt()) {
    0 -> "Jugado hoy"
    1 -> "Jugado ayer"
    else -> "Hace $days días"
  }

  /**
   * [status] = el avance del área; [games] = sus juegos; [titleOf] = el nombre visible de un juego (con idioma); [history] = las partidas guardadas; [measures] = las marcas de los juegos estrella.
   * El juego que elige Nubi es el que lleva más tiempo sin jugarse (uno nunca jugado gana).
   */
  fun build(
    area: String,
    status: AreaStatus,
    games: List<GameDefinition>,
    titleOf: (GameDefinition) -> String,
    history: List<GamePlayResult>,
    measures: List<MeasurePoint>,
    now: Long
  ): PlaceSheetUi {
    val name = IslaLugar.nameOfArea(area).orEmpty()
    val article = IslaLugar.withArticle(area).orEmpty()
    val value = status.value
    val lastPlayed = games.associate { g -> g.id to history.filter { it.gameId == g.id }.maxOfOrNull { it.timestamp } }
    val rows = games.map { g ->
      val m = StarMeasures.latest(measures, g.id)
      val last = lastPlayed[g.id]
      val detail = when {
        m != null -> StarMeasures.zoneText(m.first, measures)?.let { "$it · promedio reciente" } ?: "${m.first.format(m.second)} ${m.first.unit} · tu última"
        last == null -> "Sin jugar aún"
        else -> playedAgoShort(now, last)
      }
      PlaceGameRow(g.id, titleOf(g), detail, daysAgo = last?.let { (dayIndex(now) - dayIndex(it)).toInt() })
    }
    val chosen = games.minByOrNull { lastPlayed[it.id] ?: Long.MIN_VALUE }
    val chosenLine = chosen?.let { g ->
      val last = lastPlayed[g.id]
      "Nubi elige ${titleOf(g)}: " + if (last == null) "aún no la juegas" else playedAgo(now, last).replaceFirstChar { it.lowercase() }
    }
    val stageName = value?.let { Skill.stageName(it) }
    return PlaceSheetUi(
      area = area, key = name.lowercase(), name = name, article = article, areaName = IslaLugar.areaName(area).orEmpty(),
      stageIndex = value?.let { Skill.stage(it) }, stageName = stageName, points = status.points, fill = value?.coerceIn(0f, 1f) ?: 0f,
      changeLine = AreaProgress.changeLine(status), toNextLine = AreaProgress.toNextLine(status),
      weeks = status.weeks.map { w -> w?.let { (it.coerceIn(0f, 1f) * 100f).roundToInt() } }, change = status.change,
      games = rows, chosenGameId = chosen?.id, chosenLine = chosenLine,
      bubble = bubble(status)
    )
  }

  /**
   * Lo que dice Nubi en su globo (Tarea 147; `spec-lamina-lugar.md` §3), en primera persona y hablándole a la persona: «Esta semana avanzaste de 37 a 47. Te faltan 13 para Experto.» / «bajaste un poco, de 40 a 38: es normal que varíe» / «te mantuviste en 47»;
   * en Maestro, la primera frase sola; sin medir, «Aún no hay avance aquí: juega uno y lo verás.».
   */
  fun bubble(s: AreaStatus): String {
    val now = s.points ?: return "Aún no hay avance aquí: juega uno y lo verás."
    val before = s.pointsBefore ?: now
    val first = when {
      s.change > 0f && before != now -> "Esta semana avanzaste de $before a $now."
      s.change < 0f && before != now -> "Esta semana bajaste un poco, de $before a $now: es normal que varíe."
      else -> "Esta semana te mantuviste en $now."
    }
    val next = AreaProgress.toNextLine(s)
    return if (next == null) first else "$first $next."
  }
}
