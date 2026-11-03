package com.example.data

import com.example.model.AppLanguage
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.ui.i18n.getGameTitle
import kotlin.math.max

/**
 * Una fila de «Tus últimas partidas» en Hoy (Tarea 221; `auditoria-fable/pestana-juegos-historial.md` §5): el juego, cuándo se jugó, con qué modo y, si la partida trajo una medida válida, su marca. La fila INFORMA, no se toca (el juego se abre desde la pestaña Juegos).
 * [mode] es «en tu ruta», «suave», «a tu medida», «en Desafío», «al límite» (la forma del botón «Jugar · en Desafío · con reloj»: [RetoChoice.modeWords]) o «Contrarreloj»; la línea va en minúscula salvo los nombres propios; null en una partida anterior a que se guardara el modo (no se inventa). [mark] es la frase de la medida de ese juego con su unidad, la misma de su final de partida
 * («70 % de las cargas, sin pista»), o «38 aciertos» en el Contrarreloj; null si la partida no trajo una medida válida (o es tan vieja que ya no está entre las medidas guardadas).
 */
data class HistoryRow(
  val gameId: String,
  val title: String,
  val whenText: String,
  val mode: String?,
  val mark: String?,
  val timestamp: Long,
  val timeTrial: Boolean = false
) {
  /** La segunda línea de la fila: «cuándo · modo · marca» (lo que falta se omite). Pasa a dos renglones por palabras. */
  val detail: String get() = listOfNotNull(whenText, mode, mark).joinToString(" · ")

  /** La segunda línea tal como se DIBUJA: cada número queda pegado a su unidad con un espacio duro («38 aciertos», «hace 2 días», «70 %»; [glueNumbers], el mismo de «Te quedan 2 partidas» en Hoy), para que el renglón nunca los separe. El «·» queda pegado a la palabra de antes (un renglón no empieza con el separador) y «a la vez» (Satélites: «3,2 satélites a la vez») va pegado entre sí (Tarea 221c: nunca queda «vez» sola en un renglón). El lector lee [spoken], con espacios normales. */
  val shownDetail: String get() = detail.glueNumbers().replace(" · ", "$NBSP· ").replace("a la vez", "a${NBSP}la${NBSP}vez")

  /** Lo que dice el lector de pantalla de la fila entera, sin «botón» (no lo es): «Carga exacta, hoy, en tu ruta, 70 % de las cargas, sin pista.» La pantalla y el lector dicen lo mismo: el modo ya viene en minúscula (salvo el nombre propio «Contrarreloj»). */
  val spoken: String get() = (listOf(title, whenText) + listOfNotNull(mode, mark)).joinToString(", ") + "."

  companion object {
    /** El modo de una partida de la ruta de hoy: «Tu ruta de hoy» es el nombre, nunca «camino», «sesión» ni «diaria». */
    const val ROUTE_LABEL = "en tu ruta"
  }
}

/** Cuántas partidas lista el historial: diez son dos o tres días de uso (3 de ruta + 1 Contrarreloj por día). */
const val HISTORY_MAX = 10

/**
 * Las últimas [max] partidas para «Tus últimas partidas», las más recientes primero (Tarea 221). Mezcla las de siempre ([history]: `game_results`) con las del Contrarreloj ([timeTrials]: `time_trial_results`) por su fecha; no lista los juegos retirados (no tienen emblema ni ficha).
 * La marca de una partida de siempre sale de [measures] (`star_measures`) cruzada por juego y fecha EXACTA (`MeasurePoint.timestamp == GamePlayResult.timestamp`, la misma con que se guardó): Freno de Emergencia no lleva marca (su medida es un promedio, no una partida); Tinta o Palabra, en palabras.
 * Sin partidas, la lista queda vacía y Hoy no dibuja la tarjeta.
 */
fun historyRows(
  history: List<GamePlayResult>,
  timeTrials: List<TimeTrialResult>,
  measures: List<MeasurePoint>,
  now: Long,
  lang: AppLanguage = AppLanguage.SPANISH,
  max: Int = HISTORY_MAX
): List<HistoryRow> {
  val points = HashMap<Pair<String, Long>, MeasurePoint>(measures.size * 2)
  for (p in measures) points[p.key to p.timestamp] = p
  fun listed(gameId: String) = GameRegistry.getById(gameId)?.takeIf { !GameRegistry.isRetired(gameId) }
  fun whenOf(ts: Long): String = MeasurePhrases.ago((Streak.dayOf(now) - Streak.dayOf(ts)).toInt().coerceAtLeast(0))

  val rows = ArrayList<HistoryRow>(history.size + timeTrials.size)
  for (r in history) {
    val def = listed(r.gameId) ?: continue
    val measure = StarMeasures.defForGame(r.gameId)
    val point = measure?.let { points[it.key to r.timestamp] }
    val mark = if (measure == null || point == null) null else markOf(measure, point)
    val mode = when {
      r.route -> HistoryRow.ROUTE_LABEL
      r.modeKnown -> RetoChoice.modeWords(r.playMode)
      else -> null
    }
    rows += HistoryRow(r.gameId, getGameTitle(r.gameId, lang, def.title), whenOf(r.timestamp), mode, mark, r.timestamp)
  }
  for (t in timeTrials) {
    val def = listed(t.gameId) ?: continue
    rows += HistoryRow(t.gameId, getGameTitle(t.gameId, lang, def.title), whenOf(t.timestamp), TimeTrialDay.BUTTON_LABEL, TimeTrialDay.hitsText(t.mark), t.timestamp, timeTrial = true)
  }
  return rows.sortedByDescending { it.timestamp }.take(max)
}

/**
 * La marca de una partida: la frase de su medida (`MeasurePhrases.value`). **Freno de Emergencia no lleva marca** (Tarea 221b, auditoría de Fable): la app siempre dice el freno como el promedio de las últimas partidas (`Brake.average`, Tarea 57), así que la zona de una partida suelta
 * podía contradecir su propio final; su fila termina en el modo («hace 2 días · a tu medida»).
 */
private fun markOf(def: MeasureDef, point: MeasurePoint): String? =
  if (def.key == Brake.KEY) null else MeasurePhrases.value(def, point.value)

/**
 * El alto de la tarjeta «Tus últimas partidas» (Tarea 221; `auditoria-fable/pestana-juegos-historial.md` §10.1): se mide EN FILAS, nunca en dp fijos, porque las filas no miden lo mismo (52 dp con la marca en una línea, 69 con dos, con letra 1,0) y crecen con la letra.
 * **Tres filas enteras y la MITAD de la cuarta**: así siempre hay una fila cortada mientras falten filas (la señal de «hay más»: forma, no color). Con 1, 2 o 3 partidas la tarjeta tiene su alto natural, sin recorte. La aritmética es pura y está probada; la medida de cada fila
 * (con `rememberTextMeasurer`, al ancho real) la hace la pantalla.
 *
 * `alto = 12 + título + 6` (el título, 18 sp en una línea de 22 sp, y su aire; el 6 lleva la línea fina de debajo) `+ h1 + 1 + h2 + 1 + h3 + 1 + h4 / 2` (tres filas con sus líneas finas de 1 dp y la mitad de la cuarta). La lista termina con [END_AIR_DP] de aire:
 * la décima fila no toca el borde de abajo.
 */
object HistoryCard {
  /** Cuántas filas se ven enteras antes de la cortada. */
  const val FULL_ROWS = 3
  const val MIN_ROW_DP = 52f
  const val EMBLEM_DP = 36f
  /** El aire de arriba y de abajo de una fila (4 + 4). */
  const val ROW_PAD_DP = 8f
  const val DIVIDER_DP = 1f
  const val TITLE_TOP_DP = 12f
  const val TITLE_GAP_DP = 6f
  const val END_AIR_DP = 10f

  /** El alto de una fila cuyo texto (el nombre y su segunda línea) mide [textDp]: 52 dp como mínimo, y el emblema (36 dp, un objeto: no crece con la letra) o el texto más 8 dp de aire. */
  fun rowHeight(textDp: Float): Float = max(MIN_ROW_DP, max(EMBLEM_DP, textDp) + ROW_PAD_DP)

  /** Lo de arriba de la lista: el aire, el título (que mide [titleDp]: una línea de 22 sp con la letra del sistema, o más si el título pasa a dos renglones con letra muy grande) y el aire de abajo con su línea fina. */
  fun headerHeight(titleDp: Float): Float = TITLE_TOP_DP + titleDp + TITLE_GAP_DP

  /** ¿Se desliza por dentro? Solo con más de [FULL_ROWS] filas: con menos no hay cuarta que asomar. */
  fun scrolls(rowCount: Int): Boolean = rowCount > FULL_ROWS

  /** El alto de la ventana de la lista: el natural (con el aire del final) si no se desliza; si no, tres filas con sus líneas y la mitad de la cuarta. */
  fun windowHeight(rowHeights: List<Float>): Float {
    if (rowHeights.isEmpty()) return 0f
    if (!scrolls(rowHeights.size)) return rowHeights.sum() + DIVIDER_DP * (rowHeights.size - 1) + END_AIR_DP
    return rowHeights.take(FULL_ROWS).sum() + DIVIDER_DP * FULL_ROWS + rowHeights[FULL_ROWS] / 2f
  }

  /** El alto de la tarjeta (sin la sombra de la losa): el encabezado y la ventana. */
  fun height(rowHeights: List<Float>, titleDp: Float): Float = if (rowHeights.isEmpty()) 0f else headerHeight(titleDp) + windowHeight(rowHeights)

  /** Cuánto recorre la lista por dentro: todo su contenido (las filas, sus líneas y el aire del final) menos la ventana; 0 si no se desliza. */
  fun innerTravel(rowHeights: List<Float>): Float =
    if (!scrolls(rowHeights.size)) 0f else rowHeights.sum() + DIVIDER_DP * (rowHeights.size - 1) + END_AIR_DP - windowHeight(rowHeights)
}
