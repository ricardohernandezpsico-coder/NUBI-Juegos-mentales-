package com.example.data

import com.example.model.GameDefinition
import com.example.model.GamePlayResult
import java.util.TimeZone
import kotlin.math.roundToInt

/**
 * Si un juego es una parada de la ruta de hoy (Tarea 155, «un área, una sola vista»): la lámina del área es la misma en Hoy y en Avance (Juegos se fue en la Tarea 158), y en ella la fila de ese juego dice «hoy, en tu ruta» ([PENDING]) o «hecho hoy» ([DONE]); así no se pierde lo que decían las secciones
 * «En tu ruta de hoy» de la antigua ventana de Juegos. [NONE] = no es una parada de hoy.
 */
enum class RouteState { DONE, PENDING, NONE }

/** Las paradas de la ruta de hoy: las [completed] primeras de [gameIds] están hechas y el resto pendientes; los demás juegos no están en la ruta. */
fun routeStatesOf(gameIds: List<String>, completed: Int): Map<String, RouteState> =
  gameIds.mapIndexed { i, id -> id to if (i < completed) RouteState.DONE else RouteState.PENDING }.toMap()

/**
 * Una ficha de «Tus juegos aquí» en la lámina de un lugar: el juego y cuándo se jugó. [daysAgo] = días desde su última partida (null = nunca se jugó); la lámina solo dice eso ([whenShort], [spoken]) y NO la cifra de la medida del juego
 * (esa vive en la ficha del juego, con su explicación: Tarea 143). [detail] = el dato de siempre («Jugado ayer», «Sin jugar aún», su marca…), que ya no se dibuja pero se conserva para las pruebas y los lectores de la lógica.
 */
data class PlaceGameRow(val gameId: String, val title: String, val detail: String, val daysAgo: Int? = null, val routeState: RouteState = RouteState.NONE) {
  /** «hoy, en tu ruta» o «hecho hoy» si es una parada de la ruta de hoy; si no, «hoy», «ayer», «hace 3 días» o «nuevo» (nunca se jugó): la línea corta de la ficha. */
  val whenShort: String get() = PlaceSheets.whenShort(daysAgo, routeState)

  /** Lo mismo para el lector de pantalla: «pendiente en tu ruta de hoy», «hecho hoy en tu ruta», «jugado hoy», «jugado ayer», «jugado hace 3 días» o «sin jugar aún». */
  val spoken: String get() = PlaceSheets.whenSpoken(daysAgo, routeState)
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
  /** El juego del botón «Jugar …» de la lámina: la sugerencia de Nubi si es de esta área; si no, el que más tiempo lleva sin jugarse en ella ([Suggestion.forArea]). */
  val chosenGameId: String?,
  /** El juego que lleva el chip «Nubi sugiere»: LA sugerencia de Nubi, solo en el área donde está (Tarea 150, B2); null en las demás. */
  val suggestedGameId: String? = null,
  /** Lo que dice Nubi en su globo, dentro de la lámina (Tarea 147): lo que avanzó el área esta semana (la misma frase que el detalle, [AreaProgress.changeLine]) y lo que le falta. */
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

  /** La línea corta de una ficha de juego en la lámina: «hecho hoy» / «hoy, en tu ruta» si es una parada de hoy ([route]); si no, «hoy», «ayer», «hace N días»; «nuevo» si nunca se jugó ([daysAgo] nulo). */
  fun whenShort(daysAgo: Int?, route: RouteState = RouteState.NONE): String = when {
    route == RouteState.DONE -> "hecho hoy"
    route == RouteState.PENDING -> "hoy, en tu ruta"
    daysAgo == null -> "nuevo"
    daysAgo == 0 -> "hoy"
    daysAgo == 1 -> "ayer"
    else -> "hace $daysAgo días"
  }

  /** Lo que dice el lector de pantalla de esa línea: «hecho hoy en tu ruta», «pendiente en tu ruta de hoy», «jugado hoy», «jugado ayer», «jugado hace N días», «sin jugar aún». */
  fun whenSpoken(daysAgo: Int?, route: RouteState = RouteState.NONE): String = when {
    route == RouteState.DONE -> "hecho hoy en tu ruta"
    route == RouteState.PENDING -> "pendiente en tu ruta de hoy"
    daysAgo == null -> "sin jugar aún"
    daysAgo == 0 -> "jugado hoy"
    daysAgo == 1 -> "jugado ayer"
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
   * [suggestion] = LA sugerencia de Nubi de toda la app ([Suggestion.next]): en su área marca ese juego y el botón lo juega; en las otras áreas el botón propone el que más tiempo lleva sin jugarse, sin chip.
   */
  fun build(
    area: String,
    status: AreaStatus,
    games: List<GameDefinition>,
    titleOf: (GameDefinition) -> String,
    history: List<GamePlayResult>,
    measures: List<MeasurePoint>,
    now: Long,
    suggestion: Suggestion.Pick? = null,
    /** Las paradas de la ruta de hoy ([routeStatesOf]): sus filas dicen «hoy, en tu ruta» / «hecho hoy» y van primero (Tarea 155). */
    routes: Map<String, RouteState> = emptyMap()
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
      PlaceGameRow(g.id, titleOf(g), detail, daysAgo = last?.let { (dayIndex(now) - dayIndex(it)).toInt() }, routeState = routes[g.id] ?: RouteState.NONE)
    }.sortedBy { when (it.routeState) { RouteState.PENDING -> 0; RouteState.DONE -> 1; RouteState.NONE -> 2 } }          // las paradas de hoy primero (la que sigue, antes que las hechas); el resto, en el orden de siempre
    val pick = Suggestion.forArea(area, suggestion, games, lastPlayed.mapNotNull { (id, t) -> t?.let { id to it } }.toMap(), now) { dayIndex(it) }
    val chosen = pick?.let { p -> games.firstOrNull { it.id == p.gameId } }
    val stageName = value?.let { Skill.stageName(it) }
    return PlaceSheetUi(
      area = area, key = name.lowercase(), name = name, article = article, areaName = IslaLugar.areaName(area).orEmpty(),
      stageIndex = value?.let { Skill.stage(it) }, stageName = stageName, points = status.points, fill = value?.coerceIn(0f, 1f) ?: 0f,
      changeLine = AreaProgress.changeLine(status), toNextLine = AreaProgress.toNextLine(status),
      weeks = status.weeks.map { w -> w?.let { (it.coerceIn(0f, 1f) * 100f).roundToInt() } }, change = status.change,
      games = rows, chosenGameId = chosen?.id, suggestedGameId = if (Suggestion.suggestedHere(area, suggestion)) chosen?.id else null,
      bubble = bubble(status)
    )
  }

  /**
   * Lo que dice Nubi en su globo (Tarea 147; `spec-lamina-lugar.md` §3) con LA frase de siempre ([AreaProgress.changeLine], Tarea 150, B1.8: antes «avanzaste», que en Avance era «avanzó»): «Esta semana avanzó de 37 a 47. Te faltan 13 para Experto.» / «bajó un poco, de 40 a 38: es normal que varíe» / «se mantuvo en 47»;
   * en Maestro, la primera frase sola; sin medir, «Aún no hay avance aquí: juega uno y lo verás.».
   */
  fun bubble(s: AreaStatus): String {
    if (s.points == null) return "Aún no hay avance aquí: juega uno y lo verás."
    val first = AreaProgress.changeLine(s) + "."
    val next = AreaProgress.toNextLine(s)
    return if (next == null) first else "$first $next."
  }
}
