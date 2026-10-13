package com.example.data

import com.example.model.GameDefinition
import com.example.model.GamePlayResult
import java.util.TimeZone
import kotlin.math.roundToInt

/** Una fila de «Tus juegos aquí» en la lámina de un lugar: el juego y lo que se sabe de él («Jugado ayer», «Sin jugar aún», su marca…). */
data class PlaceGameRow(val gameId: String, val title: String, val detail: String)

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
  val games: List<PlaceGameRow>,
  val chosenGameId: String?,
  /** «Nubi elige Bodega de carga: aún no la juegas». */
  val chosenLine: String?,
  /** Lo que dice Nubi en su globo al llegar a la lámina. */
  val bubble: String
) {
  val measured: Boolean get() = stageIndex != null
}

/** Arma [PlaceSheetUi] con la lógica que ya tenía el detalle de área de Hoy (`AreaProgress`, `Skill`, `StarMeasures`): nada nuevo que medir. */
object PlaceSheets {
  val WEEK_LABELS = listOf("Hace 3 sem.", "Hace 2 sem.", "Sem. pasada", "Esta semana")

  private const val DAY_MS = 86_400_000L
  private fun dayIndex(ts: Long): Long = (ts + TimeZone.getDefault().getOffset(ts)) / DAY_MS

  /** «Jugado hoy», «Jugado ayer» o «Hace N días que no lo juegas» (la leyenda de Nubi al pie de la lámina). */
  fun playedAgo(now: Long, last: Long): String = when (val days = (dayIndex(now) - dayIndex(last)).toInt()) {
    0 -> "Jugado hoy"
    1 -> "Jugado ayer"
    else -> "Hace $days días que no lo juegas"
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
      PlaceGameRow(g.id, titleOf(g), detail)
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
      weeks = status.weeks.map { w -> w?.let { (it.coerceIn(0f, 1f) * 100f).roundToInt() } },
      games = rows, chosenGameId = chosen?.id, chosenLine = chosenLine,
      bubble = if (stageName != null) "En $article vas en $stageName. Aquí tienes tu avance y sus juegos." else "En $article aún no hay avance: juega uno y lo verás."
    )
  }
}
