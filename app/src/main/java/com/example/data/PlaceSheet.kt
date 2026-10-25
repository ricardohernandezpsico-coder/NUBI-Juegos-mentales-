package com.example.data

import com.example.model.GameDefinition
import com.example.model.GamePlayResult
import java.util.TimeZone

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
 * Los juegos de un área tal como los lista «Todos los juegos» (Tarea 163: tocar un edificio de la isla o su letrero abre esa lista ya filtrada; la lámina del edificio, con el avance y el globo de Nubi, se borró; el avance del área vive en Avance).
 * [area] = `MEMORIA`…; [areaName] = «Memoria»; [games] = sus filas (las paradas de la ruta de hoy primero); [suggestedGameId] = el juego que lleva el chip «Nubi sugiere»: LA sugerencia de Nubi, solo en el área donde está (Tarea 150, B2); null en las demás.
 */
data class PlaceSheetUi(
  val area: String,
  val areaName: String,
  val games: List<PlaceGameRow>,
  val suggestedGameId: String? = null
)

/** Arma [PlaceSheetUi] con los juegos del área y las marcas de los juegos estrella: nada nuevo que medir. */
object PlaceSheets {
  private const val DAY_MS = 86_400_000L
  private fun dayIndex(ts: Long): Long = (ts + TimeZone.getDefault().getOffset(ts)) / DAY_MS

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

  /** El dato corto de una fila de juego: «Jugado hoy», «Jugado ayer» o «Hace N días» (la fila es angosta y la frase larga se partía). */
  fun playedAgoShort(now: Long, last: Long): String = when (val days = (dayIndex(now) - dayIndex(last)).toInt()) {
    0 -> "Jugado hoy"
    1 -> "Jugado ayer"
    else -> "Hace $days días"
  }

  /**
   * [games] = sus juegos; [titleOf] = el nombre visible de un juego (con idioma); [history] = las partidas guardadas; [measures] = las marcas de los juegos estrella.
   * [suggestion] = LA sugerencia de Nubi de toda la app ([Suggestion.next]): en su área marca ese juego con el chip; en las otras áreas no hay chip.
   */
  fun build(
    area: String,
    games: List<GameDefinition>,
    titleOf: (GameDefinition) -> String,
    history: List<GamePlayResult>,
    measures: List<MeasurePoint>,
    now: Long,
    suggestion: Suggestion.Pick? = null,
    /** Las paradas de la ruta de hoy ([routeStatesOf]): sus filas dicen «hoy, en tu ruta» / «hecho hoy» y van primero (Tarea 155). */
    routes: Map<String, RouteState> = emptyMap()
  ): PlaceSheetUi {
    val lastPlayed = games.associate { g -> g.id to history.filter { it.gameId == g.id }.maxOfOrNull { it.timestamp } }
    val rows = games.map { g ->
      val m = StarMeasures.latest(measures, g.id)
      val last = lastPlayed[g.id]
      val detail = when {
        m != null -> StarMeasures.zoneText(m.first, measures)?.let { "$it · promedio reciente" } ?: "${MeasurePhrases.value(m.first, m.second)} · tu última"
        last == null -> "Sin jugar aún"
        else -> playedAgoShort(now, last)
      }
      PlaceGameRow(g.id, titleOf(g), detail, daysAgo = last?.let { (dayIndex(now) - dayIndex(it)).toInt() }, routeState = routes[g.id] ?: RouteState.NONE)
    }.sortedBy { when (it.routeState) { RouteState.PENDING -> 0; RouteState.DONE -> 1; RouteState.NONE -> 2 } }          // las paradas de hoy primero (la que sigue, antes que las hechas); el resto, en el orden de siempre
    return PlaceSheetUi(
      area = area, areaName = IslaLugar.areaName(area).orEmpty(), games = rows,
      suggestedGameId = suggestion?.takeIf { Suggestion.suggestedHere(area, it) }?.gameId
    )
  }
}
