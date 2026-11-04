package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.testTag
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.nestedscroll.nestedScroll
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Comparison
import com.example.data.IslaLugar
import com.example.data.League
import com.example.data.Streak
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialState
import com.example.data.TimeTrialStats
import com.example.data.Week
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.ClayClockIcon
import com.example.ui.components.DivisionEmblem
import com.example.ui.components.DivisionEmblemSmall
import com.example.ui.components.GameEmblem
import com.example.ui.components.Glyph
import com.example.ui.components.GuideTarget
import com.example.ui.components.guideTarget
import com.example.ui.components.KitButton
import com.example.ui.components.KitGlyph
import com.example.ui.components.NubiClay
import com.example.ui.components.LockedRow
import com.example.ui.components.OnBoardText
import com.example.ui.components.SlabColor
import com.example.ui.components.SlabSize
import com.example.data.PlusAccess
import com.example.data.glueGames
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayCard
import com.example.viewmodel.NeuroVidaViewModel

/**
 * Inicio («Hoy»): el saludo, la semana (las farolas, la llama y la frase de la racha) y UNA tarjeta crema con todo lo de la ruta de hoy (Tarea 165, 24-oct; `auditoria-fable/hoy-reestructura.md`, opción 3, elegida por Ricardo después de ver los letreros de la isla de la Tarea 163, que NO se instalaron:
 * «esa pantalla se ve muy sobrecargada, demasiada sobrecarga de estímulos, de botones»): «Tu ruta de hoy», «1 de 3 · unos 4 minutos», los tres juegos con su estado en palabras (no se tocan: informan) y el botón sol ADENTRO, con Nubi posada en su esquina; debajo, «Todos los juegos».
 * Desde la Tarea 167 (25-oct; `auditoria-fable/hoy-desafio-y-dia-completo.md`, opción 1 «Dos botones, un solo sol a la vez», elegida por Ricardo) hay un segundo botón; desde la Tarea 177 (26-oct; `auditoria-fable/contrarreloj.md`) es **«Contrarreloj de hoy»** (el mismo juego para todas las personas, 2 minutos contra el
 * reloj, con el reloj del kit; reemplaza al «Desafío de hoy»): lila junto a «Todos los juegos» mientras la ruta sigue y sol a todo el ancho con la ruta completa. **Un solo sol a la vez**: el sol está en la acción del momento (la ruta, después el Contrarreloj); jugado sin marca nueva queda en crema («otra vez») y con la marca nueva
 * ningún sol: el día está hecho. Con la ruta completa la tarjeta se cierra sola (tres «Hecho», «3 de 3 · ruta completa» en negrita, sin botón ni rótulo) y ya NO hay isla en Hoy: la isla vive en Tú. Con letra del sistema de 1,3 todo cabe en 393 × 873 dp sin deslizar.
 */
@Composable
fun HomeScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier
) {
  val dailySession by viewModel.dailySession.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val ranks by viewModel.gameRanks.collectAsState()
  val pausedGameId by viewModel.pausedGameId.collectAsState()
  val baseline by viewModel.baseline.collectAsState()
  val settings by viewModel.userSettings.collectAsState()
  val skill by viewModel.skill.collectAsState()
  val timeTrialResults by viewModel.timeTrialResults.collectAsState()
  val plus by viewModel.plus.collectAsState()
  val startupStage by viewModel.startupStage.collectAsState()
  var lockSheet by remember { mutableStateOf(false) }
  val activity by viewModel.activityTimestamps.collectAsState()          // cuándo se jugó: las partidas de siempre Y el Contrarreloj (cuenta como día jugado, no como puntos)
  // Reloj de Hoy (la fecha, la semana y la racha): cada 30 s.
  var clock by remember { mutableStateOf(System.currentTimeMillis()) }
  LaunchedEffect(Unit) {
    while (true) {
      kotlinx.coroutines.delay(30_000L)
      clock = System.currentTimeMillis()
      viewModel.dropStalePause()                    // una partida del Contrarreloj en pausa vale solo su día (Tarea 218)
      viewModel.closeLeagueWeeks()                  // y la semana de la liga se cierra sola a la medianoche del domingo (Tarea 229)
    }
  }
  val lang = LocalAppLanguage.current
  val avg = if (ranks.isEmpty()) 0 else ranks.sumOf { it.rating } / ranks.size
  val tier = RankTier.fromRating(avg)
  val completed = dailySession.completedCount
  // La racha que perdona (Tarea 124): con el día de descanso, si lo hubo esta semana.
  val streakInfo = remember(activity, clock) { Streak.fromTimestamps(activity, clock) }
  // Con la racha en 0, Hoy dice cuál fue la mejor (Tarea 186, 5a): la más larga de siempre solo se calcula entonces
  val bestStreak = remember(activity, clock) { if (streakInfo.current > 0) 0 else Streak.longest(activity.map(Streak::dayOf)) }
  val measures by viewModel.starMeasures.collectAsState()
  val historyList = remember(history, timeTrialResults, measures, clock, lang) { com.example.data.historyRows(history, timeTrialResults, measures, clock, lang) }
  // La liga de la semana (Tarea 229): con qué se compara esta persona es lo mismo que en la pestaña; sin franja de edad o sin referencia no hay liga ni línea
  val league by viewModel.league.collectAsState()
  val ageRange by viewModel.ageRange.collectAsState()
  val compareWorld by viewModel.compareWorld.collectAsState()
  val estimatedReference by viewModel.estimatedReference.collectAsState()
  val leagueLive = remember(league, ageRange, compareWorld, estimatedReference, timeTrialResults, clock) {
    val source = Comparison.sourceFor(com.example.BuildConfig.DEBUG, compareWorld, estimatedReference)
    if (League.available(source, ageRange)) League.live(league, League.weekGames(source, ageRange, timeTrialResults, clock)) else null
  }
  val ui = HomeUi(
    name = settings.name,
    now = clock,
    tier = tier,
    streak = streakInfo.current,
    streakText = Streak.counterText(streakInfo.current, bestStreak),
    streakNote = Streak.noteFor(streakInfo, bestStreak),
    weeklyGoal = settings.weeklyGoal,
    lamps = Week.lampsFor(activity, clock),
    stops = homeStops(dailySession.gameIds, completed, lang),
    pausedTitle = pausedGameId?.let { GameRegistry.getById(it) }?.let { getGameTitle(it.id, lang, it.title) },
    flightDay = dailySession.isFlightDay,
    hasBaseline = baseline != null,
    baselineInvite = com.example.data.BaselinePlan.showHomeInvite(baseline != null, history.size, history.minOfOrNull { it.timestamp }, clock),
    timeTrial = timeTrialOf(timeTrialResults, viewModel.todayKey(), lang, plus),
    league = leagueLive,
    history = historyList,
    onScreen = startupStage == com.example.viewmodel.StartupStage.READY && com.example.ui.components.LocalTabActive.current
  )
  HomeContent(
    ui = ui,
    onPlay = { viewModel.startDailySession() },
    onResume = { viewModel.resumePausedGame() },
    onBaseline = { viewModel.startBaseline() },
    onTimeTrial = { viewModel.startTimeTrial() },
    onLocked = { lockSheet = true },
    tuButton = { m -> com.example.ui.components.TuHeaderButton(viewModel, m.guideTarget(GuideTarget.TU_BUTTON)) },       // el botón «Tú» va en la fila del logotipo y la guía lo ilumina en su paso 3 (Tarea 225b)
    modifier = modifier
  )
  if (lockSheet) LockSheetHost(viewModel, onDismiss = { lockSheet = false })
}

// ------------------------------------------------------------------ Hoy con una sola tarjeta

private val HoyCream = Color(0xFFF6EBD3)
private val HoySoft = Color(0xFFD6D1F0)
private val HoyMuted = Color(0xFF4A4470)

/** Lo que Hoy muestra, ya calculado (así `HomeContent` no depende del ViewModel y se prueba y se captura con datos fijos). */
internal data class HomeUi(
  val name: String,
  val now: Long,
  val tier: RankTier,
  val streak: Int,
  /** Junto a la llama: «N días»; con la racha en 0, «Tu racha empieza hoy» (hubo una mejor antes) o «Tu racha empieza con tu próxima partida» (Tarea 186; null: «N días»). */
  val streakText: String? = null,
  /** Debajo de la racha: «Descansaste el martes: tu racha sigue.» si hubo un día de descanso perdonado en los últimos 7 días; si no, «Un día de descanso por semana no la corta.»; con la racha en 0, «Tu mejor racha: 12 días.» (null: sin línea). */
  val streakNote: String? = null,
  val weeklyGoal: Int,
  val lamps: Week.Lamps,
  val stops: List<HomeStop>,
  /** Título del juego en pausa, si hay uno (el botón único pasa a «Seguir tu partida»). */
  val pausedTitle: String?,
  /** El camino de hoy quedó cumplido sin jugarlo: fue el Primer vuelo. */
  val flightDay: Boolean,
  val hasBaseline: Boolean,
  /** Si Hoy invita a «Encuentra tu punto de partida»: por omisión, mientras no lo haya; Hoy la apaga pasadas 5 partidas o 7 días (Tarea 152, B6.2). */
  val baselineInvite: Boolean = !hasBaseline,
  /** El Contrarreloj de hoy (Tarea 177): el juego de la fecha (el mismo para todas las personas) y cómo va; null solo si el juego del día no está en el catálogo (no pasa). */
  val timeTrial: HomeTimeTrial? = null,
  /** La liga de la semana (Tarea 229): la división de ahora y cómo va la semana, para la línea bajo la losa del Contrarreloj; null = sin liga (sin franja de edad o sin referencia): no hay línea. */
  val league: League.Live? = null,
  /** «Tus últimas partidas» (Tarea 221): las últimas partidas, las más recientes primero (`historyRows`); vacía = Hoy no dibuja la tarjeta. */
  val history: List<com.example.data.HistoryRow> = emptyList(),
  /** Hoy está a la vista y el arranque terminó (los datos ya están al día): hasta entonces la llama de hoy no puede «destellar», porque la semana sale vacía un instante, ni destellar con Hoy escondida en otra pestaña (Tareas 223 y 224, `rememberTodayGlint`). */
  val onScreen: Boolean = true
) {
  val completed: Int get() = stops.count { it.state == StopState.DONE }
}

internal enum class StopState { DONE, NEXT, LATER }

/**
 * Lo que ocupa el logotipo arriba de Hoy (Tarea 223): 6 dp de aire, 40 dp de caja del logotipo y 2 dp hasta el saludo; desde la Tarea 225b es una FILA de 48 dp ([HOY_HEADER], la cabecera de las pestañas) con el logotipo a la izquierda, centrado en su caja de 40 dp
 * (4 dp de aire arriba y abajo: [HOY_LOGO_TOP]), y el botón «Tú» a la derecha, también centrado a 24 dp, DENTRO de la fila: se va hacia arriba con Hoy y nunca queda sobre la tarjeta. El saludo sigue empezando 48 dp bajo la barra ([HOY_GREETING_TOP] = 0).
 * Antes de la Tarea 223 el saludo empezaba 28 dp bajo la barra: todo Hoy baja [HOY_LOGO_SHIFT].
 */
internal val HOY_HEADER = com.example.ui.components.TU_HEADER_HEIGHT
internal val HOY_LOGO_BOX = 40.dp
internal val HOY_LOGO_TOP = (HOY_HEADER - HOY_LOGO_BOX) / 2
internal val HOY_GREETING_TOP = 0.dp
internal val HOY_LOGO_SHIFT = HOY_HEADER + HOY_GREETING_TOP - 28.dp

/** El espacio bajo la última fila cuando la tarjeta no tiene botón y solo dice una línea (Tarea 194). */
internal val CAPTION_ONLY_GAP = 6.dp

/** Lo más que muestra la tarjeta: la ruta normal trae 3 juegos y el día del Primer vuelo, hasta los 4 del vuelo (Tarea 193). */
internal const val MAX_STOPS = 4

/**
 * El Contrarreloj de hoy tal como lo muestra Hoy: el juego del día (con su nombre visible), cómo va (pendiente, jugado sin marca nueva —«otra vez»— o con marca nueva), la mejor marca de HOY y el récord de la persona en ese juego (los dos null si no hay).
 */
internal data class HomeTimeTrial(
  val gameId: String, val title: String, val state: TimeTrialState, val todayHits: Int? = null, val record: Int? = null,
  /** Las partidas gratis que quedan hoy (Tarea 218, la regla de partidas): null = sin tope (con Nubilus Plus); 0 = «Hoy ya jugaste tus 3 partidas.». */
  val gamesLeft: Int? = null,
  /** El juego del Contrarreloj de mañana, para decir «Mañana: Tinta o Palabra.» cuando no quedan partidas. */
  val tomorrowTitle: String? = null
)

/** El Contrarreloj de hoy con lo guardado: el juego sale de la FECHA ([TimeTrialDay.gameFor]); el estado, de las partidas de hoy ([TimeTrialDay.stateOf]); el récord, de todas las de ese juego con la pista de hoy ([TimeTrialStats.record]). */
internal fun timeTrialOf(results: List<com.example.data.TimeTrialResult>, dateKey: String, lang: com.example.model.AppLanguage, plus: Boolean = true): HomeTimeTrial? {
  val gameId = TimeTrialDay.gameFor(dateKey)
  val def = GameRegistry.getById(gameId) ?: return null
  val state = TimeTrialDay.stateOf(results, dateKey, gameId)
  // la regla de partidas (Tarea 218): sin Plus quedan [FREE_GAMES_PER_DAY] menos las de hoy; sin ninguna, Hoy dice el juego de mañana
  val left = if (plus) null else TimeTrialDay.gamesLeft(results, dateKey, gameId)
  val tomorrow = if (left == 0) GameRegistry.getById(TimeTrialDay.gameFor(TimeTrialDay.nextDay(dateKey)))?.let { getGameTitle(it.id, lang, it.title) } else null
  return HomeTimeTrial(
    gameId = gameId, title = getGameTitle(gameId, lang, def.title), state = state,
    todayHits = TimeTrialDay.ofGame(results, gameId).filter { it.day == dateKey }.maxOfOrNull { it.mark }, record = TimeTrialStats.record(results, gameId),
    gamesLeft = left, tomorrowTitle = tomorrow
  )
}

/** Una parada del camino de hoy: el juego, su lugar en la isla (el de su área) y si ya se hizo, es la que sigue o viene después. */
internal data class HomeStop(val gameId: String, val title: String, val area: String, val place: String, val state: StopState)

/** Las paradas de hoy en orden (tres juegos; cuatro el día del Primer vuelo, que muestra los que se jugaron en el vuelo): las [completed] primeras están hechas, la siguiente es «la que sigue». */
internal fun homeStops(gameIds: List<String>, completed: Int, lang: com.example.model.AppLanguage): List<HomeStop> =
  gameIds.take(MAX_STOPS).mapIndexedNotNull { i, id ->
    GameRegistry.getById(id)?.let { def ->
      HomeStop(
        gameId = id, title = getGameTitle(id, lang, def.title), area = def.domain.name, place = IslaLugar.nameOfArea(def.domain.name).orEmpty(),
        state = when { i < completed -> StopState.DONE; i == completed -> StopState.NEXT; else -> StopState.LATER }
      )
    }
  }

/** «Buenos días», «Buenas tardes» o «Buenas noches» según la hora (sin culpa: no habla de lo que falta). */
internal fun greetingWord(now: Long): String {
  val h = java.util.Calendar.getInstance().apply { timeInMillis = now }.get(java.util.Calendar.HOUR_OF_DAY)
  return when { h < 12 -> "Buenos días"; h < 20 -> "Buenas tardes"; else -> "Buenas noches" }
}

/** «Domingo 11 oct.» (con el mes abreviado: a 16 sp, «Miércoles 21 de octubre · 2 de 4 días de tu meta» no cabía en una línea a 393 dp; Tarea 153). */
internal fun dateText(now: Long): String =
  java.text.SimpleDateFormat("EEEE d MMM", java.util.Locale("es")).format(java.util.Date(now)).replaceFirstChar { it.uppercase() }

/** Lo que dice la derecha de «Tu ruta de hoy»: cuántos juegos van y cuánto falta («0 de 3 · unos 6 minutos»). */
internal fun routeMeta(ui: HomeUi): String {
  val total = ui.stops.size
  val pending = ui.stops.count { it.state != StopState.DONE }
  return if (pending == 0) "$total de $total · ruta completa" else "${total - pending} de $total · ${com.example.data.DailyPath.minutesText(pending)}"
}

/**
 * El contenido de Hoy sin ViewModel (Tareas 165, 167, 177 y 220): saludo, la fecha con la meta de la semana, los siete farolitos con tu racha, UNA tarjeta con la ruta de hoy y, debajo, el Contrarreloj de hoy (y la invitación al punto de partida, si toca) y, al final, «Tus últimas partidas» (Tarea 221, [HistoryCard]: sin partidas no hay tarjeta; Hoy y la lista de la tarjeta se deslizan con [hoyFirstConnection]). «Todos los juegos» ya no está aquí: es la pestaña Juegos (Tarea 220). Si algo no cabe (letra del sistema muy grande),
 * toda la pantalla se desliza; con 1,3 cabe entera en 393 × 873 dp.
 */
@Composable
internal fun HomeContent(
  ui: HomeUi,
  onPlay: () -> Unit,
  onResume: () -> Unit,
  onBaseline: () -> Unit,
  /** «Contrarreloj de hoy»: lanza el juego de la fecha en «La escalera de 2 minutos» (Tarea 177). */
  onTimeTrial: () -> Unit = {},
  /** La fila gris con candado de Hoy sin partidas (Tarea 218): abre la hoja del candado. */
  onLocked: () -> Unit = {},
  /** El botón «Tú» de la fila del logotipo (Tarea 225b): recibe su lugar (arriba a la derecha). Vacío en las pruebas y capturas sueltas: la fila mide igual. */
  tuButton: @Composable (Modifier) -> Unit = {},
  modifier: Modifier = Modifier
) {
  // La barra de pestañas es una capa fija (`LocalBarInset`, Tarea 123): Hoy deja esa franja abajo.
  val barInset = com.example.ui.components.LocalBarInset.current
  BoxWithConstraints(modifier.fillMaxSize()) {
    // Hoy se desliza con UN estado propio: «Tus últimas partidas» (Tarea 221) tiene su propia lista por dentro y `hoyFirstConnection` decide quién se mueve primero (hacia arriba el dedo, Hoy; hacia abajo, la lista)
    val scrollState = rememberScrollState()
    Column(Modifier.fillMaxSize().padding(bottom = barInset).nestedScroll(remember(scrollState) { hoyFirstConnection(scrollState) }).verticalScroll(scrollState).testTag("home_scroll")) {
      // El logotipo «Nubilus» (Tarea 223, L1) y el botón «Tú» (Tarea 225b): UNA fila de 48 dp arriba, SOLO en Hoy; el logotipo en su caja de 40 dp centrada, el botón a la derecha, los dos con el centro a 24 dp; la fila se va con Hoy (antes el saludo empezaba 28 dp bajo la barra: Hoy baja 20 dp). Marca, no texto: no crece con la letra del sistema.
      com.example.ui.components.TuHeaderRow(tuButton, bottomGap = 0.dp, modifier = Modifier.testTag("home_header")) {
        Box(Modifier.align(Alignment.CenterStart).padding(start = 20.dp).height(HOY_LOGO_BOX), contentAlignment = Alignment.CenterStart) { com.example.ui.components.NubilusLogo(fill = HoyCream) }
      }
      // Saludo. Todo el texto va a la IZQUIERDA, en la zona del fondo sin luceros (`ZONAS_HOY` en `tools/isla/isla.py`; la vigila `BoardBackgroundContrastTest`).
      Row(Modifier.fillMaxWidth().padding(start = 20.dp, end = 16.dp, top = HOY_GREETING_TOP), verticalAlignment = Alignment.CenterVertically) {
        OnBoardText(
          if (ui.name.isBlank()) greetingWord(ui.now) else "${greetingWord(ui.now)}, ${ui.name.trim()}",
          color = HoyCream, fontSize = 22.sp, fontWeight = FontWeight.Bold, fontFamily = com.example.ui.theme.TitleFamily, lineHeight = 27.sp, modifier = Modifier.weight(1f).testTag("home_greeting")
        )
      }
      // Una sola línea con la fecha y la meta de la semana.
      OnBoardText(
        "${dateText(ui.now)} · ${Week.caption(ui.lamps.daysPlayed, ui.weeklyGoal)}",
        color = HoySoft, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.padding(start = 20.dp, end = 16.dp, top = 2.dp).testTag("home_date")
      )
      com.example.ui.components.HoyWeek(ui.lamps, ui.streak, ui.streakNote, streakText = ui.streakText, glintVisible = ui.onScreen)
      Spacer(Modifier.height(10.dp))
      if (ui.stops.isNotEmpty() || ui.pausedTitle != null) HoyCard(ui, onPlay = onPlay, onResume = onResume, modifier = Modifier.padding(horizontal = 16.dp))
      HoyBelow(ui, onTimeTrial = onTimeTrial, onLocked = onLocked, onBaseline = onBaseline)
    }
  }
}

/**
 * LA tarjeta de Hoy: una losa crema con «Tu ruta de hoy» y su meta («1 de 3 · unos 4 minutos») a la izquierda y Nubi posada en su esquina de arriba a la derecha (`hola` de día; de noche duerme, con los ojos cerrados); debajo, los tres juegos de la ruta, cada uno con su emblema, su nombre y su estado en
 * palabras con su marca («Hecho» con el visto sobre lima, «Sigue» con un punto de tinta, «Después» con un aro vacío): NO se tocan —informan; un juego concreto se abre desde la pestaña Juegos—, y el botón sol ADENTRO («Empezar la ruta · 3 juegos», «Seguir la ruta · 2 juegos» o «Seguir tu partida»),
 * el único sol mientras la ruta sigue. Con la ruta COMPLETA la tarjeta se cierra sola (Tarea 167): las tres filas en «Hecho», la meta en negrita («3 de 3 · ruta completa») y Nubi saludando, sin botón ni rótulo; el sol pasa al Contrarreloj de hoy, debajo ([HoyBelow]). Las filas se leen una a una
 * («Bodega de carga, hecho»); el botón es el único toque de la tarjeta.
 */
@Composable
private fun HoyCard(ui: HomeUi, onPlay: () -> Unit, onResume: () -> Unit, modifier: Modifier = Modifier) {
  val pending = ui.stops.count { it.state != StopState.DONE }
  val paused = ui.pausedTitle != null
  val complete = ui.stops.isNotEmpty() && pending == 0 && !paused
  val caption = captionLine(ui)
  ClayCard(modifier = modifier.fillMaxWidth().testTag("home_card"), color = Clay.Cream, size = SlabSize.LARGE, contentPadding = 0.dp) {
    Column(Modifier.fillMaxWidth().padding(start = 16.dp, end = 16.dp, top = 12.dp, bottom = 14.dp)) {
      if (ui.stops.isNotEmpty()) {
        Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
          Column(Modifier.weight(1f)) {
            Text("Tu ruta de hoy", color = Clay.Ink, fontSize = 22.sp, fontWeight = FontWeight.Bold, fontFamily = com.example.ui.theme.TitleFamily, lineHeight = 27.sp, modifier = Modifier.testTag("home_route_title"))
            Text(
              routeMeta(ui), color = if (complete) Clay.Ink else Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp,
              fontWeight = if (complete) FontWeight.Bold else FontWeight.Normal, modifier = Modifier.testTag("home_route_meta")
            )
          }
          Spacer(Modifier.width(8.dp))
          NubiClay(width = 84.dp, modifier = Modifier.testTag("home_nubi"))
        }
        HoyStops(ui)
      }
      // Con botón (la ruta sigue o hay una partida en pausa): 10 dp antes del botón. Sin botón, solo la línea («Tu primer vuelo fue tu ruta de hoy.», Tarea 194): pegada al ritmo de las filas, un solo espacio chico bajo la última (antes 10 + 8 = 18 dp, y se veía bajada).
      val hasButton = paused || pending > 0
      if (hasButton) Spacer(Modifier.height(10.dp))
      when {
        paused -> ClayButton(text = "Seguir tu partida", onClick = onResume, glyph = Glyph.PLAY, fontSize = 18.sp, modifier = Modifier.testTag("btn_resume_paused"))
        pending > 0 -> {
          val label = if (ui.completed == 0) "Empezar la ruta · ${ui.stops.size} juegos" else "Seguir la ruta · $pending ${if (pending == 1) "juego" else "juegos"}"
          ClayButton(text = label, onClick = onPlay, glyph = Glyph.PLAY, fontSize = 18.sp, modifier = Modifier.testTag("btn_home_play"))
        }
      }
      caption?.let { line ->
        Text(
          line, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, textAlign = TextAlign.Center,
          modifier = Modifier.fillMaxWidth().padding(top = if (hasButton) 8.dp else CAPTION_ONLY_GAP).testTag(if (ui.flightDay) "home_flight_day" else "home_caption")
        )
      }
    }
  }
}

/** Lo que dice el lector de pantalla de un juego de la ruta: «Bodega de carga, hecho / es el que sigue / después». */
private fun stopDescription(stop: HomeStop): String =
  "${stop.title}, " + when (stop.state) { StopState.DONE -> "hecho"; StopState.NEXT -> "es el que sigue"; StopState.LATER -> "después" }

/** La palabra de cada estado (la marca sola no basta: acierto y estado nunca solo por forma o color). */
internal fun stopStatusWord(state: StopState): String = when (state) { StopState.DONE -> "Hecho"; StopState.NEXT -> "Sigue"; StopState.LATER -> "Después" }

/** La marca del estado de un juego: «Hecho» = un visto de tinta sobre un disco lima; «Sigue» = un punto crema en un disco de tinta; «Después» = un aro vacío. Crece con la letra del sistema (hasta 1,6). */
@Composable
private fun StopMark(state: StopState) {
  val scale = LocalDensity.current.fontScale.coerceIn(1f, 1.6f)
  val s = 24.dp * scale
  when (state) {
    StopState.DONE -> Box(Modifier.size(s).clip(CircleShape).background(Clay.Lime).border(2.dp, Clay.Ink, CircleShape), contentAlignment = Alignment.Center) { KitGlyph(Glyph.CHECK, size = 15.dp * scale) }
    StopState.NEXT -> Box(Modifier.size(s).clip(CircleShape).background(Clay.Ink), contentAlignment = Alignment.Center) { Box(Modifier.size(s * 0.36f).clip(CircleShape).background(HoyCream)) }
    StopState.LATER -> Box(Modifier.size(s).border(2.dp, HoyMuted, CircleShape))
  }
}

/**
 * Las tres filas de la tarjeta. Si la palabra MÁS LARGA de algún nombre («Constelaciones», «Acoplamiento») no cabe junto a su estado («Después» con su marca), las TRES filas ponen el estado DEBAJO del nombre (con letra 2,0, o 1,3 en un teléfono de 360 dp, el nombre y el estado lado a lado
 * partían las palabras a la mitad): se mide con la letra del sistema, igual que se hacía con las piedritas.
 */
@Composable
private fun HoyStops(ui: HomeUi) {
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  val density = LocalDensity.current
  val scale = density.fontScale.coerceIn(1f, 1.6f)
  BoxWithConstraints(Modifier.fillMaxWidth().padding(top = 4.dp)) {
    val titleStyle = base.merge(androidx.compose.ui.text.TextStyle(fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp))
    val statusStyle = base.merge(androidx.compose.ui.text.TextStyle(fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 20.sp))
    val longestPx = ui.stops.maxOf { s -> s.title.split(' ').maxOf { w -> measurer.measure(w, titleStyle, softWrap = false, maxLines = 1).size.width } }
    val statusPx = ui.stops.maxOf { s -> measurer.measure(stopStatusWord(s.state), statusStyle, softWrap = false, maxLines = 1).size.width } + with(density) { (24.dp * scale + 6.dp).toPx() }
    val availablePx = with(density) { (maxWidth - 36.dp - 10.dp - 8.dp).toPx() }                       // el ancho sin el emblema, su aire y el aire entre el nombre y el estado
    val stacked = longestPx + statusPx > availablePx
    Column(Modifier.fillMaxWidth()) {
      ui.stops.forEachIndexed { i, s ->
        if (i > 0) Box(Modifier.fillMaxWidth().height(1.dp).background(Clay.Ink.copy(alpha = 0.12f)))
        HoyStopRow(s, i + 1, stacked)
      }
    }
  }
}

/**
 * Un juego de la ruta como fila de la tarjeta (48 dp como mínimo): su emblema (36 dp), su nombre y su estado con su marca; el estado va a la derecha o, si no cabe ([HoyStops]), debajo del nombre. NO es un botón (ni `onClick` ni `Role.Button`): informa.
 * El nombre pasa a dos líneas antes que partirse una palabra (`HoyStonesWordsTest`, con letra hasta 2,0); la que sigue va en negrita (el sol es solo del botón de la tarjeta).
 */
@Composable
private fun HoyStopRow(stop: HomeStop, number: Int, stacked: Boolean) {
  @Composable fun Status(modifier: Modifier = Modifier) {
    Row(verticalAlignment = Alignment.CenterVertically, modifier = modifier) {
      StopMark(stop.state)
      Spacer(Modifier.width(6.dp))
      Text(
        stopStatusWord(stop.state), color = if (stop.state == StopState.LATER) HoyMuted else Clay.Ink, fontSize = 16.sp,
        fontWeight = if (stop.state == StopState.LATER) FontWeight.Normal else FontWeight.Bold, lineHeight = 20.sp, softWrap = false, modifier = Modifier.testTag("home_stop_state_$number")
      )
    }
  }
  Row(
    Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = stopDescription(stop) }.testTag("home_stop_$number").heightIn(min = 52.dp).padding(vertical = 4.dp),
    verticalAlignment = Alignment.CenterVertically
  ) {
    GameEmblem(stop.gameId, 36.dp)
    Spacer(Modifier.width(10.dp))
    if (stacked) {
      Column(Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(2.dp)) {
        Text(stop.title, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp, modifier = Modifier.testTag("home_stop_title_$number"))
        Status()
      }
    } else {
      Text(stop.title, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp, modifier = Modifier.weight(1f).testTag("home_stop_title_$number"))
      Spacer(Modifier.width(8.dp))
      Status()
    }
  }
}

/** Lo que Hoy dice dentro de la tarjeta, bajo el botón, cuando hay algo que decir: la partida en pausa o el Primer vuelo. (Con la ruta completa la tarjeta ya no dice nada: el «Mañana…» pasó al bloque del Desafío, [HoyBelow].) */
internal fun captionLine(ui: HomeUi): String? = when {
  ui.pausedTitle != null -> "Tienes una partida en pausa: ${ui.pausedTitle}."
  ui.flightDay -> "Tu primer vuelo fue tu ruta de hoy."                    // UNA frase (Tarea 193): «Mañana empieza tu ruta.» ya lo dijo el cierre del vuelo, y la línea va igual con 1, 2, 3 o 4 filas
  else -> null
}

/** Quién tiene el SOL de Hoy (Tarea 167: «un solo sol a la vez»). Desde la Tarea 219 el sol vive SOLO en la ruta: el Contrarreloj tiene su color propio, el celeste. */
internal enum class Sun { ROUTE, NONE }

/**
 * El sol está en la ruta mientras sigue (o hay una partida en pausa que seguir) y en NADIE cuando la ruta está completa: la tarjeta se cierra sola y la losa celeste del Contrarreloj, por su tamaño y su color, es lo que sigue; ya NO pasa a ser sol (Tarea 219, forma B de `idea-pestana-contrarreloj.md` §2.2).
 * Nunca dos a la vez. Robolectric no deja leer píxeles (`captureToImage` se cuelga): esta función es la que decide el color y la que vigilan las pruebas.
 */
internal fun sunOf(ui: HomeUi): Sun {
  val pending = ui.stops.count { it.state != StopState.DONE }
  return if (ui.pausedTitle != null || pending > 0) Sun.ROUTE else Sun.NONE
}

/** Lo que Hoy dice cuando el día está hecho con la ruta completa y sin Contrarreloj (no pasa: el juego del día siempre existe), y lo que dice la parte de abajo de la tarjeta. */
internal const val ROUTE_TOMORROW = "Mañana te espera una ruta nueva."

/** El nombre del modo para el lector de pantalla y los demás textos («Contrarreloj de hoy»). */
internal val TIME_TRIAL_LABEL = TimeTrialDay.LABEL

/** El rótulo del botón (Tarea 180): solo el nombre del modo, una palabra que nunca se parte; el juego de hoy va en su segunda línea. Una palabra por concepto: nunca «Reto», «Desafío» ni «Con reloj». */
internal val TIME_TRIAL_BUTTON_LABEL = TimeTrialDay.BUTTON_LABEL

/** «Otra vez»: la losa crema que reemplaza a la celeste del Contrarreloj cuando hoy ya se jugó (con o sin marca nueva) y quedan partidas. */
internal const val TIME_TRIAL_AGAIN = TimeTrialStats.AGAIN

/** El reloj de plasticina de la losa del Contrarreloj (Tarea 219: 56 dp, antes 42): mide el CUERPO del reloj; el dibujo trae sus márgenes. */
internal val TimeTrialClockSize = 56.dp

/** Lo que dice el lector de pantalla de la losa: todo junto (los minutos, lo jugado hoy, el récord y las partidas que quedan, que ya no están a la vista en la losa). */
internal fun timeTrialDescription(t: HomeTimeTrial): String =
  TimeTrialDay.description(t.title, t.state, t.todayHits, t.record) + (t.gamesLeft?.takeIf { it > 0 }?.let { ", " + TimeTrialDay.gamesLeftLine(it).trimEnd('.').replaceFirstChar { c -> c.lowercase() } } ?: "")

/** La segunda línea de la losa (Tarea 180): «Hoy: Tinta o Palabra», siempre (lo demás ya no se dice en la losa). */
internal fun timeTrialSecondLine(t: HomeTimeTrial): String = TimeTrialDay.secondLine(t.title)

/** El color de la losa del Contrarreloj: CELESTE mientras falta jugarlo (el color propio del modo) y CREMA («Otra vez») cuando hoy ya se jugó. Nunca sol ni lila (el lila es del Desafío). Robolectric no lee píxeles: las pruebas miran esta función. */
internal fun timeTrialSlabColor(again: Boolean): SlabColor = if (again) SlabColor.CREAM else SlabColor.SKY

/**
 * La losa del Contrarreloj de hoy (Tarea 219, forma B de `idea-pestana-contrarreloj.md` §2.2; antes un botón lila de 64 dp): la losa CELESTE del kit (`SlabColor.SKY`, el color propio del Contrarreloj) a todo el ancho, de 80 dp, con el reloj de plasticina a 56 dp a la izquierda, «Contrarreloj» a 20 sp y, debajo, «Hoy: Tinta o Palabra».
 * Cuando hoy ya se jugó ([again]: con o sin marca nueva) pasa a CREMA y dice «Otra vez» (el celeste es de lo que falta por jugar). Nunca es sol: el único sol de Hoy es el de la ruta.
 */
@Composable
private fun TimeTrialSlab(t: HomeTimeTrial, again: Boolean, onClick: () -> Unit, modifier: Modifier = Modifier) {
  val spoken = timeTrialDescription(t)                                       // la losa entera: el modo, el juego, los 2 minutos, lo jugado hoy («otra vez»), el récord y las partidas que quedan
  KitButton(
    text = if (again) TIME_TRIAL_AGAIN else TIME_TRIAL_BUTTON_LABEL, onClick = onClick, color = timeTrialSlabColor(again),
    leading = { ClayClockIcon(size = TimeTrialClockSize) }, fontSize = 20.sp, minHeight = 80.dp,
    secondLine = timeTrialSecondLine(t), secondLineSize = 16.sp,
    modifier = modifier.semantics(mergeDescendants = true) { contentDescription = spoken }.testTag("btn_timetrial")
  )
}

/**
 * El tono claro del CELESTE del Contrarreloj (Tarea 219; el `#4CC9F0` del kit mezclado con la mitad de blanco): el recuadro plano de «Marca nueva» y el de «Hoy ya jugaste tus 3 partidas.». Con él, la tinta (`#1A1240`) mide 12,5:1 y `InkMuted` más de 4,5:1 (`HoyTarjetaTest` los mide).
 * Antes era el lila claro (`#D9CEFF`): el lila queda para el Desafío.
 */
internal val TimeTrialDoneFill = Color(0xFFA5E4F7)

/** El lila claro `#D9CEFF` que tuvo el recuadro de «Marca nueva» hasta la Tarea 219: lo conserva la frase enmarcada de la página «Contrarreloj» (Tarea 203; Ricardo la aprobó así). Si algún día la página se pasa al celeste claro, es una decisión aparte. */
internal val TimeTrialLilacFill = Color(0xFFD9CEFF)

/** El radio de las esquinas de los recuadros planos de Hoy: el de los botones y las tarjetas del kit (22 dp). */
internal val TimeTrialDoneRadius = 22.dp

/** Una línea de un recuadro plano de Hoy: su texto, su etiqueta de prueba (se ve en el árbol sin fusionar) y si lleva el visto o pega «3 partidas» y «de hoy». */
private class NoteLine(val text: String, val tag: String, val check: Boolean = false, val glue: Boolean = false)

/**
 * Un recuadro PLANO (sin borde tinta grueso, sin sombra dura, sin losa: no es un botón, no se toca y no tiene sol) en celeste claro, al ancho de la pantalla y con el radio del kit. Dos usos: hoy hay marca nueva («Marca nueva: 43 aciertos en Tinta o Palabra.», con el visto, la moneda lima; Tarea 169 en el Desafío,
 * Tarea 177 aquí, en lila hasta la 219) y no quedan partidas («Hoy ya jugaste tus 3 partidas. Mañana: ¿Verdad o disparate?», sin visto; Tarea 219: antes era texto suelto sobre el cielo). Si pasan las dos cosas a la vez, van en UN solo recuadro, la marca arriba y, tras una línea fina, lo de mañana (decisión de Opus, 3-nov).
 * El texto va en tinta; con `glue`, «3 partidas» y «de hoy» pegados; una línea sin visto junto a otra con visto se alinea con su texto. El lector dice todas las líneas de un tirón.
 */
@Composable
private fun TimeTrialNote(tag: String, lines: List<NoteLine>, modifier: Modifier = Modifier) {
  val scale = LocalDensity.current.fontScale.coerceIn(1f, 1.6f)
  val anyCheck = lines.any { it.check }
  Column(
    modifier
      .fillMaxWidth()
      .semantics(mergeDescendants = true) { contentDescription = lines.joinToString(" ") { it.text } }
      .testTag(tag)
      .clip(RoundedCornerShape(TimeTrialDoneRadius)).background(TimeTrialDoneFill)
      .padding(horizontal = 14.dp, vertical = 12.dp)
  ) {
    lines.forEachIndexed { i, line ->
      if (i > 0) Box(Modifier.fillMaxWidth().padding(vertical = 8.dp).height(1.dp).background(Clay.Ink.copy(alpha = 0.14f)))
      Row(verticalAlignment = Alignment.CenterVertically) {
        if (line.check) {
          StopMark(StopState.DONE)
          Spacer(Modifier.width(10.dp))
        } else if (anyCheck) Spacer(Modifier.width(24.dp * scale + 10.dp))
        Text(if (line.glue) line.text.glueGames() else line.text, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp, modifier = Modifier.weight(1f).testTag(line.tag))
      }
    }
  }
}

/**
 * La línea de la LIGA bajo la losa del Contrarreloj (Tarea 229; `docs/liga-de-la-semana.md` §5.2): el emblema de la división a 24 dp y, a su lado, «Despegando · vas para subir a Sobre las nubes», en texto suelto sobre el cielo y centrado (reemplaza a «Te quedan N partidas de hoy. Tu récord: …», que quedó
 * en la pestaña y en la ficha). Sin franja de edad o sin referencia no hay liga y la línea no sale. El lector de pantalla lee la línea entera, sin el emblema (decorativo).
 */
@Composable
private fun LeagueLine(live: League.Live) {
  val line = League.hoyLine(live)
  Row(Modifier.fillMaxWidth().padding(top = 6.dp).testTag("home_timetrial_league"), horizontalArrangement = Arrangement.Center, verticalAlignment = Alignment.CenterVertically) {
    DivisionEmblem(live.division, DivisionEmblemSmall, Modifier.clearAndSetSemantics { testTag = "home_league_emblem" })
    Spacer(Modifier.width(8.dp))
    OnBoardText(line.glueNamesForLetter(), color = HoySoft, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.weight(1f, fill = false).testTag("home_timetrial_line"), textAlign = TextAlign.Start)
  }
}

/**
 * El Contrarreloj de hoy bajo la tarjeta de la ruta (Tarea 219; desde la Tarea 220 «Todos los juegos» no está en Hoy y el bloque sube 70 dp; `auditoria-fable/idea-pestana-contrarreloj.md` §2.2, forma B), con los estados de siempre —sin Nubilus Plus / con Plus—:
 *  - **Sin jugar** (la ruta a medias o completa): la losa CELESTE, «Te quedan 3 partidas de hoy.» / «Tu récord: 41 aciertos.» bajo ella, en texto sobre el cielo.
 *  - **Jugado, quedan partidas:** la losa CREMA «Otra vez» con «Hoy: <juego>»; «Te quedan 2 partidas de hoy. Tu récord: 41 aciertos.» / «Tu récord: 41 aciertos.».
 *  - **Marca nueva hoy:** el recuadro plano en celeste claro con el visto («Marca nueva: 43 aciertos en Tinta o Palabra.») y, debajo, «Otra vez» (si quedan partidas; con Plus, siempre) y, sin Plus, «Te quedan 2 partidas de hoy.» (sin el récord: ya está en el recuadro).
 *  - **Sin partidas** (sin Plus): sin losa; el recuadro plano celeste claro «Hoy ya jugaste tus 3 partidas. Mañana: <juego>.» (con la marca nueva, UN solo recuadro: la marca arriba y, tras una línea fina, lo de mañana) y, debajo, UNA fila gris con candado que abre la hoja (Tarea 218).
 */
@Composable
private fun TimeTrialBlock(t: HomeTimeTrial, league: League.Live?, onTimeTrial: () -> Unit, onLocked: () -> Unit) {
  val newMark = t.state == TimeTrialState.MARCA_NUEVA
  val markLine = NoteLine(TimeTrialDay.newMarkLine(t.todayHits ?: t.record ?: 0, t.title), tag = "home_timetrial_mark", check = true)
  val gap = Modifier.padding(top = 8.dp)
  // La guía de Nubi (Tarea 193) ilumina el BLOQUE entero, no un botón: la losa y su línea o, sin partidas, el recuadro y la fila gris (la losa no existe; Tarea 219, pedido de Opus tras la auditoría de la 218)
  Column(Modifier.fillMaxWidth().padding(top = 8.dp).guideTarget(GuideTarget.TIME_TRIAL).testTag("home_timetrial_block")) {
    when {
      t.gamesLeft == 0 -> {
        val tomorrow = NoteLine(t.tomorrowTitle?.let { TimeTrialDay.tomorrowLine(it) } ?: TimeTrialDay.NO_GAMES_LEFT, tag = "home_timetrial_tomorrow", glue = true)
        TimeTrialNote("home_timetrial_none", if (newMark) listOf(markLine, tomorrow) else listOf(tomorrow))
        LockedRow(PlusAccess.LOCKED_GAMES, onLocked, modifier = gap, tag = "home_locked_games")
      }
      newMark -> {
        TimeTrialNote("home_timetrial_done", listOf(markLine))
        TimeTrialSlab(t, again = true, onClick = onTimeTrial, modifier = gap)
      }
      else -> TimeTrialSlab(t, again = t.state == TimeTrialState.JUGADO, onClick = onTimeTrial)
    }
    // la liga de la semana (Tarea 229): «Te quedan N partidas» y el récord ya no se dicen aquí (quedan en la pestaña y en la ficha); sin liga, sin línea
    league?.let { LeagueLine(it) }
  }
}

/**
 * Bajo la tarjeta (Tareas 167, 177, 180, 219 y 220; «un solo sol a la vez», que vive SOLO en la ruta): el Contrarreloj de hoy ([TimeTrialBlock]) en su color propio, el celeste. Ya no hay título «Modos de juego» ni la losa «Todos los juegos» (es la pestaña Juegos).
 * Y, si todavía no hizo la evaluación, la invitación a hacerla.
 */
@Composable
private fun HoyBelow(ui: HomeUi, onTimeTrial: () -> Unit, onLocked: () -> Unit, onBaseline: () -> Unit) {
  val pending = ui.stops.count { it.state != StopState.DONE }
  val routeDone = ui.stops.isNotEmpty() && pending == 0 && ui.pausedTitle == null
  val t = ui.timeTrial
  Column(Modifier.fillMaxWidth().padding(start = 16.dp, end = 16.dp, top = 2.dp, bottom = 4.dp)) {
    if (t == null && routeDone && !ui.flightDay) OnBoardText(ROUTE_TOMORROW, color = HoyCream, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp, modifier = Modifier.fillMaxWidth().padding(top = 8.dp).testTag("home_route_tomorrow"), textAlign = TextAlign.Center)
    if (t != null) TimeTrialBlock(t, ui.league, onTimeTrial, onLocked)
    if (ui.baselineInvite) {
      // El enlace sigue siendo un enlace pero en crema tenue (`OnNightDim`) con su chevrón, para que no compita con el sol de la ruta o del Desafío
      Row(
        Modifier
          .padding(top = 4.dp)
          .clip(RoundedCornerShape(10.dp))
          .clickable(role = androidx.compose.ui.semantics.Role.Button, onClick = onBaseline)
          .heightIn(min = 48.dp)
          .padding(horizontal = 4.dp)
          .testTag("btn_home_baseline"),
        verticalAlignment = Alignment.CenterVertically
      ) {
        OnBoardText(
          text = "Encuentra tu punto de partida",                 // una línea, con su flecha (Opus, 21-oct; antes «… · 4 juegos cortos», que bajaba a 2 líneas)
          color = com.example.ui.theme.NubiTokens.OnNightDim,
          fontSize = 16.sp,
          fontWeight = FontWeight.SemiBold,
          modifier = Modifier.weight(1f, fill = false)
        )
        Spacer(Modifier.width(6.dp))
        androidx.compose.foundation.Canvas(Modifier.size(width = 10.dp, height = 16.dp)) {
          val p = androidx.compose.ui.graphics.Path().apply { moveTo(size.width * 0.15f, size.height * 0.1f); lineTo(size.width * 0.85f, size.height * 0.5f); lineTo(size.width * 0.15f, size.height * 0.9f) }
          drawPath(p, com.example.ui.theme.NubiTokens.OnNightDim, style = androidx.compose.ui.graphics.drawscope.Stroke(2.dp.toPx(), cap = androidx.compose.ui.graphics.StrokeCap.Round, join = androidx.compose.ui.graphics.StrokeJoin.Round))
        }
      }
    }
    // «Tus últimas partidas» (Tarea 221): después del Contrarreloj y de la invitación, antes de la marca del final; sin partidas no hay tarjeta
    if (ui.history.isNotEmpty()) HistoryCard(ui.history, modifier = Modifier.padding(top = 10.dp))
    Spacer(Modifier.testTag("home_end"))                         // marca (sin tamaño) donde termina lo último de Hoy: las pruebas miden con ella cuánto se pasa de la barra
  }
}
