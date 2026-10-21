package com.example.ui.screens

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectHorizontalDragGestures
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.wrapContentHeight
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.positionInWindow
import com.example.ui.components.Glyph
import com.example.ui.components.KitGlyph
import com.example.ui.components.SlabSize
import com.example.ui.components.OnBoardText
import com.example.ui.components.PieceFx
import com.example.ui.components.RouteFx
import com.example.ui.components.RouteMotion
import com.example.data.IslaPieza
import com.example.data.IslaPiezas
import com.example.data.IslaPiezasStore
import com.example.data.PiezasPlan
import com.example.data.PrefsIslaPiezasStore
import com.example.data.PrefsRouteCelebrationStore
import com.example.data.RouteCelebrationStore
import com.example.data.RouteLamps
import com.example.data.RoutePlan
import com.example.ui.components.rememberIslaLayout
import com.example.ui.components.rememberReduceMotion
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.layout.Layout
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.data.AreaProgress
import com.example.data.AreaStatus
import com.example.data.MeasurePoint
import com.example.data.Skill
import com.example.data.StarMeasures
import com.example.data.IslaLugar
import com.example.data.Streak
import com.example.data.Week
import com.example.model.DomainType
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.AreaBar
import com.example.ui.components.AreaBarColor
import com.example.ui.components.LeagueShield
import com.example.ui.components.LockTabSwipe
import com.example.ui.components.MiniPlanet
import com.example.ui.components.NubiBubble
import com.example.ui.components.Island
import com.example.ui.components.TopActions
import com.example.ui.components.NubiClay
import com.example.ui.components.areaDescription
import com.example.ui.components.domainOf
import com.example.ui.components.overallIndex
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayCard
import com.example.viewmodel.NeuroVidaViewModel
import com.example.viewmodel.TopPanel
import java.util.TimeZone

private const val DAY_MS = 86_400_000L
private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = com.example.ui.theme.NubiTokens.OnNightDim
private val OnNightSoft = Color(0xFF9AA3D6)

private fun dayIndex(ts: Long): Long = (ts + TimeZone.getDefault().getOffset(ts)) / DAY_MS

/**
 * Inicio ("Hoy") con LA ISLA protagonista (Tarea 82, fase 1 de `docs/diseno-isla.md`; distribución «A» de la Tarea 84, aprobada por Ricardo el 12-oct): saludo con tu perfil y la meta de la semana, los farolitos y la racha, la isla de plasticina con
 * Nubi (toma TODO lo que sobra), «Tu ruta de hoy» con tres piedritas (una por juego; tocarlas abre la ficha del juego) y UN solo botón grande. Regla que manda: en 393 × 873 dp con letra normal las tres piedritas y el botón se ven SIN deslizar;
 * con letra grande o pantalla chica la isla se queda en su mínimo y lo demás se desliza, sin cortarse ni superponerse. El globo de Nubi ya no está: lo que decía (partida en pausa, camino cumplido, Primer vuelo) va debajo del botón.
 *

 * Qué pasó con lo que Hoy tenía antes: las cuatro barras de área (con su etapa y el cambio de la semana) ya no están sueltas; cada una vive en el DETALLE de su área ([AreaDetail]: etapa, cuánto avanzó, las últimas semanas, sus juegos y «¿Le damos un empujón?»), que se abre TOCANDO
 * el edificio de esa área en la isla (Observatorio = Memoria, Antena = Atención, Hangar = Razonamiento, Biblioteca = Lenguaje; hasta la fase 2, que lo cambia a «Juegos con ese lugar elegido»). La liga y la racha pasaron al lado de los farolitos; el perfil y las opciones siguen arriba a la derecha.
 */
@Composable
fun HomeScreen(
  viewModel: NeuroVidaViewModel,
  onNavigateToGames: () -> Unit,
  modifier: Modifier = Modifier
) {
  val dailySession by viewModel.dailySession.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val ranks by viewModel.gameRanks.collectAsState()
  val progress by viewModel.gameProgress.collectAsState()
  val progressLog by viewModel.progressLog.collectAsState()
  val pausedGameId by viewModel.pausedGameId.collectAsState()
  val baseline by viewModel.baseline.collectAsState()
  val measures by viewModel.starMeasures.collectAsState()
  val settings by viewModel.userSettings.collectAsState()
  val unlocks by viewModel.achievementUnlocks.collectAsState()
  val achievementQueue by viewModel.achievementQueue.collectAsState()
  val debugPieces by viewModel.debugIslaPieces.collectAsState()
  // Reloj de la semana de cada área: cada 30 s.
  var clock by remember { mutableStateOf(System.currentTimeMillis()) }
  LaunchedEffect(Unit) {
    while (true) {
      kotlinx.coroutines.delay(30_000L)
      clock = System.currentTimeMillis()
    }
  }
  val lang = LocalAppLanguage.current

  var sheetFor by remember { mutableStateOf<com.example.model.GameDefinition?>(null) }
  val skill by viewModel.skill.collectAsState()
  val lastPlayed = remember(history) { history.groupBy { it.gameId }.mapValues { (_, l) -> l.maxOf { it.timestamp } } }

  val avg = if (ranks.isEmpty()) 0 else ranks.sumOf { it.rating } / ranks.size
  val tier = RankTier.fromRating(avg)

  val names = remember { DomainType.values().associate { it.name to it.displayName } }
  val statuses = remember(progress, progressLog, clock) {
    AREA_ORDER.map { key ->
      AreaProgress.status(key, GameRegistry.allGames.filter { it.domain.name == key }.map { it.id }, progress, progressLog, clock)
    }
  }

  // UNA sola sugerencia de Nubi para toda la app (Tarea 150, B2): lo que sigue en la ruta de hoy y, terminada, el juego que más tiempo lleva sin jugarse.
  val suggestion = remember(dailySession, lastPlayed, clock) { com.example.data.Suggestion.next(dailySession.gameIds, dailySession.completedCount, lastPlayed, clock, Streak::dayOf) }
  // La lámina de cada lugar: el avance de su área y sus juegos (la lógica del antiguo detalle de área).
  val places = remember(statuses, history, measures, clock, lang, suggestion) {
    AREA_ORDER.mapIndexed { i, key ->
      val domain = domainOf(key)
      key to com.example.data.PlaceSheets.build(
        area = key, status = statuses.getOrNull(i) ?: AreaStatus(key, null, 0f, emptyList()),
        games = GameRegistry.allGames.filter { it.domain == domain }, titleOf = { g -> getGameTitle(g.id, lang, g.title) },
        history = history, measures = measures, now = clock, suggestion = suggestion
      )
    }.toMap()
  }
  val completed = dailySession.completedCount
  // La racha que perdona (Tarea 124): con el día de descanso, si lo hubo esta semana.
  val streakInfo = remember(history, clock) { Streak.fromTimestamps(history.map { it.timestamp }, clock) }
  val ui = HomeUi(
    name = settings.name,
    now = clock,
    tier = tier,
    streak = streakInfo.current,
    streakNote = Streak.forgivenLine(streakInfo) ?: Streak.REST_LINE,
    weeklyGoal = settings.weeklyGoal,
    lamps = Week.lampsFor(history.map { it.timestamp }, clock),
    stops = homeStops(dailySession.gameIds, completed, lang),
    pausedTitle = pausedGameId?.let { GameRegistry.getById(it) }?.let { getGameTitle(it.id, lang, it.title) },
    flightDay = dailySession.gameIds.isNotEmpty() && completed >= dailySession.gameIds.size && dailySession.scores.isEmpty(),
    hasBaseline = baseline != null,
    baselineInvite = com.example.data.BaselinePlan.showHomeInvite(baseline != null, history.size, history.minOfOrNull { it.timestamp }, clock),
    places = places,
    islandPieces = debugPieces ?: IslaPiezas.earned(unlocks.keys)          // «Tu isla crece»: todo se deriva de las medallas (solo el botón [Debug] de Ajustes lo cambia)
  )
  val appContext = androidx.compose.ui.platform.LocalContext.current
  val routeStore = remember { PrefsRouteCelebrationStore(appContext) }
  val pieceStore = remember { PrefsIslaPiezasStore(appContext) }
  HomeContent(
    ui = ui,
    onProfile = { viewModel.openTopPanel(TopPanel.PERFIL) },
    onSettings = { viewModel.openTopPanel(TopPanel.AJUSTES) },
    onPlaceGame = { id -> viewModel.playFromHome(id, com.example.data.PlayMode.A_TU_MEDIDA) },
    onPlaceGameCard = { id -> GameRegistry.getById(id)?.let { sheetFor = it } },
    onStop = { stop -> GameRegistry.getById(stop.gameId)?.let { sheetFor = it } },
    onPlay = { viewModel.startDailySession() },
    onResume = { viewModel.resumePausedGame() },
    onBaseline = { viewModel.startBaseline() },
    modifier = modifier,
    routeStore = routeStore,
    pieceStore = pieceStore,
    holdPieces = achievementQueue.isNotEmpty()          // la celebración de la medalla está encima: la pieza nueva espera a que se cierre para entrar
  )

  // La ficha del juego que se tocó (una piedrita de «Tu ruta de hoy» o una fila de la lámina de un lugar): la misma de Juegos (avance, marca, constancia, cómo jugar y Jugar). Al volver de la partida se queda en Hoy.
  sheetFor?.let { g ->
    GameSheetHost(
      game = g, viewModel = viewModel,
      onDismiss = { sheetFor = null },
      onPlay = { m, timed ->
        sheetFor = null
        viewModel.playFromHome(g.id, m, timed)
      }
    )
  }
}

// ------------------------------------------------------------------ Hoy con la isla

private val HoyCream = Color(0xFFF6EBD3)
private val HoySoft = Color(0xFFD6D1F0)

/** Lo que Hoy muestra, ya calculado (así `HomeContent` no depende del ViewModel y se prueba y se captura con datos fijos). */
internal data class HomeUi(
  val name: String,
  val now: Long,
  val tier: RankTier,
  val streak: Int,
  /** Debajo de la racha: «Descansaste el martes: tu racha sigue.» si hubo un día de descanso perdonado en los últimos 7 días; si no, «Un día de descanso por semana no la corta.» (null: sin línea). */
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
  /** La lámina de cada lugar (por área: `MEMORIA`…) que se abre al tocar su edificio. */
  val places: Map<String, com.example.data.PlaceSheetUi> = emptyMap(),
  /** Las piezas de la isla que dan las medallas ganadas («Tu isla crece»; `data/IslaPiezas.kt`), en el orden de dibujo. */
  val islandPieces: List<IslaPieza> = emptyList()
) {
  val completed: Int get() = stops.count { it.state == StopState.DONE }
}

internal enum class StopState { DONE, NEXT, LATER }

/** Una parada del camino de hoy: el juego, su lugar en la isla (el de su área) y si ya se hizo, es la que sigue o viene después. */
internal data class HomeStop(val gameId: String, val title: String, val area: String, val place: String, val state: StopState)

/** Las tres paradas de hoy en orden: las [completed] primeras están hechas, la siguiente es «la que sigue». */
internal fun homeStops(gameIds: List<String>, completed: Int, lang: com.example.model.AppLanguage): List<HomeStop> =
  gameIds.take(3).mapIndexedNotNull { i, id ->
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

/** «Domingo 11 oct.» (con el mes abreviado: a 16 sp, «Miércoles 21 de octubre · 2 de 4 días de tu meta» no cabía en una línea a 393 dp y le quitaba alto a la isla; Tarea 153). */
internal fun dateText(now: Long): String =
  java.text.SimpleDateFormat("EEEE d MMM", java.util.Locale("es")).format(java.util.Date(now)).replaceFirstChar { it.uppercase() }

/**
 * El nombre que dice el lector de pantalla para la isla, con el estado de la ruta de hoy (Tarea 88): «Tu isla: hoy la ruta pasa por el Observatorio, la Antena y el Hangar» (nada hecho), «Tu isla: hecho el Observatorio; faltan la Antena y el Hangar»
 * (algo hecho) o «Tu isla: ruta completa, hechos …». Las farolas y su animación son decorativas: lo que importa para el lector es esto.
 */
internal fun islandDescription(ui: HomeUi): String =
  RouteLamps.description(ui.stops.mapNotNull { s -> IslaLugar.withArticle(s.area)?.let { it to (s.state == StopState.DONE) } }) + IslaPiezas.description(ui.islandPieces)

/**
 * El contenido de Hoy sin ViewModel (Tarea 84, distribución «A» aprobada por Ricardo el 12-oct): saludo con tu perfil y opciones, la fecha con la meta de la semana, los siete farolitos con tu racha, LA ISLA (la protagonista: toma todo lo que sobra),
 * «Tu ruta de hoy» con tres piedritas (una por juego; tocarlas abre la ficha del juego) y UN solo botón grande. La isla ocupa lo que SOBRA entre los demás elementos,
 * con un mínimo de [ISLAND_MIN_HEIGHT] ([HoyColumn]): si lo demás no cabe (letra grande, pantalla chica) la isla se queda en su mínimo y toda la pantalla se desliza. Con letra grande (≥ 1,3) las tres piedritas pasan a una lista de filas, porque en tres columnas no caben las palabras.
 * [onStop] = se tocó la piedrita de un juego del camino; [onPlaceGame] = se pidió jugar un juego desde la lámina de un lugar (Tarea 85: tocar un edificio abre esa lámina con el avance del área; ver `HoyPlace.kt`).
 * [fx] = el estado de esa lámina (se pasa solo en pruebas).
 */
@Composable
internal fun HomeContent(
  ui: HomeUi,
  onProfile: () -> Unit,
  onSettings: () -> Unit,
  /** El botón grande «Jugar en el …» de la lámina: juega directo el juego que elige Nubi. */
  onPlaceGame: (gameId: String) -> Unit,
  /** Tocar el nombre (la fila) de un juego en la lámina: abre la ficha de ese juego encima de ella (Tarea 87). */
  onPlaceGameCard: (gameId: String) -> Unit,
  onStop: (HomeStop) -> Unit,
  onPlay: () -> Unit,
  onResume: () -> Unit,
  onBaseline: () -> Unit,
  modifier: Modifier = Modifier,
  fx: PlaceFx = remember { PlaceFx() },
  /** Dónde se guarda lo celebrado hoy (las farolas que ya pulsaron y la ruta completa). Con null (pruebas de dibujo) no hay celebración: las farolas de las paradas hechas están encendidas y quietas. */
  routeStore: RouteCelebrationStore? = null,
  /** Avisa qué se va a animar (pruebas): se llama una vez por plan no vacío, justo antes de animar. */
  onRoutePlan: (RoutePlan) -> Unit = {},
  /** Dónde se guardan las piezas de la isla que Hoy ya mostró (pasajero). Con null (pruebas de dibujo) no hay revelación: las piezas están puestas. */
  pieceStore: IslaPiezasStore? = null,
  /** Hay una celebración de medalla encima de Hoy: las piezas nuevas esperan (sin aparecer) hasta que se cierre. */
  holdPieces: Boolean = false,
  /** Avisa qué piezas se van a revelar (pruebas): se llama una vez por plan no vacío, justo antes de animar. */
  onPiecePlan: (List<IslaPieza>) -> Unit = {}
) {
  val scope = rememberCoroutineScope()
  val reduceMotion = rememberReduceMotion()
  val confirm = com.example.ui.components.rememberKitConfirm()       // el «listo» háptico al completar la ruta (KitHaptics)
  val layout = rememberIslaLayout()
  // Las farolas de la ruta (Tarea 88): qué se enciende ahora por primera vez hoy y si la ruta quedó completa. El plan sale de lo celebrado; se guarda al empezar a animar, así cada farola pulsa UNA vez por parada y por día.
  val doneKeys = ui.stops.filter { it.state == StopState.DONE }.mapNotNull { RouteLamps.placeKey(it.area) }
  val allDone = ui.stops.isNotEmpty() && ui.stops.all { it.state == StopState.DONE }
  val day = RouteLamps.dayKey(ui.now)
  val routeFx = remember(day, doneKeys, allDone, routeStore, reduceMotion) {
    RouteFx(plan = if (routeStore == null) RoutePlan.NONE else RouteLamps.plan(doneKeys, allDone, routeStore.load(), day), reduceMotion = reduceMotion)
  }
  LaunchedEffect(routeFx) {
    if (routeStore != null && !routeFx.plan.isEmpty) {
      routeStore.save(RouteLamps.consume(routeFx.plan, routeStore.load(), day))
      onRoutePlan(routeFx.plan)
      if (routeFx.plan.complete) confirm()
      routeFx.run()
    }
  }
  // Las piezas de la isla («Tu isla crece»): la primera vez con esta versión (o en un teléfono nuevo) lo que ya se tiene se siembra en silencio; una pieza nueva entra UNA vez (de a una, sin sonido ni vibración: ya los dio la celebración de la medalla)
  // y se anota al empezar. Con «quitar animaciones» nace puesta y se anota igual (sin segunda vez).
  val pieceFx = remember(ui.islandPieces, pieceStore, reduceMotion) {
    PieceFx(plan = if (pieceStore == null) PiezasPlan.NONE else IslaPiezas.plan(ui.islandPieces, pieceStore.load()), reduceMotion = reduceMotion)
  }
  LaunchedEffect(pieceFx, holdPieces) {
    if (pieceStore != null && !holdPieces) {
      val seen = pieceStore.load()
      val next = IslaPiezas.consume(ui.islandPieces, seen)
      if (next != seen) pieceStore.save(next)
      if (!pieceFx.plan.isEmpty) {
        onPiecePlan(pieceFx.plan.reveal)
        pieceFx.run()
      }
    }
  }
  var rootOffset by remember { mutableStateOf(Offset.Zero) }
  var islandWindow by remember { mutableStateOf(Rect.Zero) }
  // La barra de pestañas es una capa fija (`LocalBarInset`, Tarea 123): la página de Hoy ya no cambia de alto al abrir o cerrar la lámina (antes se congelaba el alto y se soltaba 450 ms después de cerrar), y la lámina cubre la barra.
  val barInset = com.example.ui.components.LocalBarInset.current
  if (fx.active) {
    LockTabSwipe()                                            // las pestañas no se deslizan con la lámina abierta (y la barra se retira)
    BackHandler { fx.close(scope, reduceMotion) }             // Atrás la cierra
  }
  BoxWithConstraints(modifier.fillMaxSize().onGloballyPositioned { rootOffset = it.positionInWindow() }) {
    val viewportHeight = maxHeight - barInset                  // lo que ve la isla: el alto de la página sin la franja de la barra
    Box(Modifier.fillMaxSize().padding(bottom = barInset).verticalScroll(rememberScrollState(), enabled = !fx.active)) {
      HoyColumn(
        viewportHeight = viewportHeight,
        top = {
          Column(Modifier.fillMaxWidth()) {
            // Saludo; arriba a la derecha tu perfil (con el escudo de tu liga) y las opciones, como en las otras pestañas. Todo el texto va a la IZQUIERDA y desde 28 dp bajo la barra: la zona del fondo sin luceros (`ZONAS_HOY` en `tools/isla/isla.py`;
            // la vigila `HoyIslaScreenshotTest`). Con 24 dp, en una pantalla de 411 × 914 dp, el lucero de arriba a la izquierda rozaba la caja del saludo.
            Row(Modifier.fillMaxWidth().padding(start = 20.dp, end = 16.dp, top = 28.dp), verticalAlignment = Alignment.CenterVertically) {
              OnBoardText(
                if (ui.name.isBlank()) greetingWord(ui.now) else "${greetingWord(ui.now)}, ${ui.name.trim()}",
                color = HoyCream, fontSize = 22.sp, fontWeight = FontWeight.Bold, lineHeight = 27.sp, modifier = Modifier.weight(1f).testTag("home_greeting")
              )
              Spacer(Modifier.width(4.dp))
              TopActions(
                name = ui.name, onProfile = onProfile, onSettings = onSettings,
                profileLabel = if (com.example.Features.LEAGUES) "Tu perfil, liga ${ui.tier.tierName}" else "Tu perfil",
                profileBadge = if (com.example.Features.LEAGUES) ({ LeagueShield(tier = ui.tier, size = 20.dp) }) else null           // sin ligas en la v1 (Tarea 149): el avatar va solo con su inicial
              )
            }
            // Una sola línea con la fecha y la meta de la semana.
            OnBoardText(
              "${dateText(ui.now)} · ${Week.caption(ui.lamps.daysPlayed, ui.weeklyGoal)}",
              color = HoySoft, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.padding(start = 20.dp, end = 16.dp, top = 2.dp).testTag("home_date")
            )
            com.example.ui.components.HoyWeek(ui.lamps, ui.streak, ui.streakNote)
          }
        },
        island = {
          Island(
            description = islandDescription(ui),
            layout = layout,
            doneAreas = ui.stops.filter { it.state == StopState.DONE }.map { it.area }.toSet(),
            onPlace = { area ->
              val key = IslaLugar.nameOfArea(area)?.lowercase()
              if (key != null && ui.places.containsKey(area) && !fx.active) {
                fx.open(scope, key, reduceMotion)
              }
            },
            lookKey = fx.key ?: routeFx.lookKey,
            lookAmount = { if (fx.key != null) fx.look.value else routeFx.look.value },
            lookMoves = if (fx.key != null) fx.moves else routeFx.moves,
            lampProgress = { routeFx.pulseProgress(it) },
            sparks = { routeFx.sparks.value },
            nubiLiftDp = { RouteMotion.jumpLiftDp(routeFx.jump.value) },
            nubiTiltDeg = { RouteMotion.jumpTiltDeg(routeFx.jump.value) },
            pieces = ui.islandPieces,
            pieceReveal = { pieceFx.progress(it) },
            nubiAlpha = { if (fx.flight.value > 0f) 0f else 1f },
            shadowAlpha = { fx.shadow.value },
            glowKey = fx.key,
            glowAlpha = { fx.glow.value },
            onDrawnRect = { islandWindow = it },
            modifier = Modifier.fillMaxWidth().testTag("home_isla")
          )
        },
        bottom = {
          Column(Modifier.fillMaxWidth()) {
            // Con letra grande (> 1,15) lo principal va primero (Tarea 143, ítem 3): el botón «Seguir la ruta» antes de la lista de paradas, que es la explicación y puede quedar bajo el pliegue
            if (LocalDensity.current.fontScale > 1.15f) {
              HoyAction(ui, onPlay = onPlay, onResume = onResume, onBaseline = onBaseline)
              HoyRoute(ui, onStop)
            } else {
              HoyRoute(ui, onStop)
              HoyAction(ui, onPlay = onPlay, onResume = onResume, onBaseline = onBaseline)
            }
          }
        }
      )
    }

    // La lámina del lugar tocado (velo, lámina, Nubi volando y su globo) va encima de todo Hoy.
    val density = LocalDensity.current
    PlaceLayer(
      fx = fx,
      place = fx.key?.let { k -> ui.places.values.firstOrNull { it.key == k } },
      layout = layout,
      island = Rect(islandWindow.left - rootOffset.x, islandWindow.top - rootOffset.y, islandWindow.right - rootOffset.x, islandWindow.bottom - rootOffset.y),
      onClose = { fx.close(scope, reduceMotion) },
      onPlay = onPlaceGame,
      onGame = onPlaceGameCard
    )
  }
}

/**
 * Las tres partes de Hoy una debajo de otra: lo de arriba, la isla y lo de abajo. Lo de arriba y lo de abajo miden lo que necesiten y la isla toma TODO lo que sobra de la pantalla ([viewportHeight]), con un mínimo de [ISLAND_MIN_HEIGHT]:
 * si lo demás no cabe, la isla se queda en su mínimo y el total pasa de la pantalla (la columna de afuera se desliza). Se mide así, y no con un `weight`, porque un hijo con `weight` recibe un alto exacto: un mínimo no puede agrandarlo
 * y la isla se dibujaba más grande que su caja, encima de lo de abajo.
 */
@Composable
private fun HoyColumn(viewportHeight: Dp, top: @Composable () -> Unit, island: @Composable () -> Unit, bottom: @Composable () -> Unit) {
  val density = LocalDensity.current
  val viewportPx = with(density) { viewportHeight.roundToPx() }
  val minIslandPx = with(density) { ISLAND_MIN_HEIGHT.roundToPx() }
  Layout(content = { top(); island(); bottom() }, modifier = Modifier.fillMaxWidth()) { measurables, constraints ->
    val loose = Constraints(constraints.minWidth, constraints.maxWidth, 0, Constraints.Infinity)
    val t = measurables[0].measure(loose)
    val b = measurables[2].measure(loose)
    val islandHeight = maxOf(minIslandPx, viewportPx - t.height - b.height)
    val i = measurables[1].measure(Constraints(constraints.minWidth, constraints.maxWidth, islandHeight, islandHeight))
    layout(constraints.maxWidth, t.height + i.height + b.height) {
      t.place(0, 0)
      i.place(0, t.height)
      b.place(0, t.height + i.height)
    }
  }
}

/** Alto mínimo de la isla en Hoy: debajo de esto se ve chica, y la pantalla se desliza antes de achicarla más. A 393 × 873 dp con letra normal la isla pasa de 400 dp. */
internal val ISLAND_MIN_HEIGHT = 220.dp

/** Lo que dice la derecha de «Tu ruta de hoy»: cuántos juegos van y cuánto falta («0 de 3 · unos 6 minutos»). */
internal fun routeMeta(ui: HomeUi): String {
  val total = ui.stops.size
  val pending = ui.stops.count { it.state != StopState.DONE }
  return if (pending == 0) "$total de $total · completa" else "${total - pending} de $total · ${com.example.data.DailyPath.minutesText(pending)}"
}

/** «Tu ruta de hoy» y, debajo, una piedrita por juego del camino. Tocar una abre la ficha del juego. */
@Composable
private fun HoyRoute(ui: HomeUi, onStop: (HomeStop) -> Unit) {
  if (ui.stops.isEmpty()) return
  Column(Modifier.fillMaxWidth().padding(start = 16.dp, end = 16.dp, top = 4.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
    val bigFont = LocalDensity.current.fontScale > 1.15f
    if (bigFont) {
      // Con letra grande el título y su meta no caben lado a lado: uno debajo del otro.
      Column {
        OnBoardText("Tu ruta de hoy", color = HoyCream, fontSize = 17.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.testTag("home_route_title"))
        OnBoardText(routeMeta(ui), color = HoySoft, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.testTag("home_route_meta"))
      }
    } else {
      Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.Bottom, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        OnBoardText("Tu ruta de hoy", color = HoyCream, fontSize = 17.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.testTag("home_route_title"))
        OnBoardText(routeMeta(ui), color = HoySoft, fontSize = 16.sp, lineHeight = 20.sp, textAlign = TextAlign.End, modifier = Modifier.weight(1f).testTag("home_route_meta"))
      }
    }
    HoyStops(ui, onStop, bigFont)
  }
}

/** Cómo van los nombres en las piedritas: de lo más cómodo a lo más justo (nombre en 15 o 14 sp, relleno interno de 9 o 6 dp). Si ni la última cabe, las paradas pasan a filas. */
internal enum class StoneFit(val titleSp: Int, val padding: Dp) {
  NORMAL(15, 9.dp), TIGHT_PADDING(15, 6.dp), SMALL_TEXT(14, 6.dp);

  companion object {
    /** La primera medida en la que la palabra MÁS LARGA de cada nombre cabe entera en la piedrita; `null` si en ninguna (entonces: filas). [widestWordPx] = ancho de la palabra más larga de todos los nombres a [titleSp] sp. */
    fun pick(stoneWidthPx: Float, density: Float, widestWordPx: (titleSp: Int) -> Float): StoneFit? =
      entries.firstOrNull { f -> widestWordPx(f.titleSp) + 2f * f.padding.value * density + 1f * density <= stoneWidthPx }
  }
}

/**
 * Las tres paradas del camino: piedritas en una fila o, con letra grande o si ni a 14 sp cabe la palabra más larga de un nombre, una fila por juego. Nunca se parte una palabra a la mitad (Tarea 87): antes de achicar nada se mide el nombre
 * («Constelaciones» a 411 dp) y se elige [StoneFit]; el mínimo de texto es 14 sp.
 */
@Composable
private fun HoyStops(ui: HomeUi, onStop: (HomeStop) -> Unit, bigFont: Boolean) {
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  val density = LocalDensity.current
  BoxWithConstraints(Modifier.fillMaxWidth()) {
    val stoneWidthPx = with(density) { ((maxWidth - 16.dp) / 3).toPx() }          // tres piedritas y dos separaciones de 8 dp
    val titles = ui.stops.map { it.title }
    val fit = if (bigFont) null else StoneFit.pick(stoneWidthPx, density.density) { sp ->
      val style = base.merge(androidx.compose.ui.text.TextStyle(fontSize = sp.sp, fontWeight = FontWeight.SemiBold, lineHeight = 17.sp))
      titles.maxOf { t -> t.split(' ').maxOf { w -> measurer.measure(w, style = style, softWrap = false, maxLines = 1).size.width } }.toFloat()
    }
    if (fit == null) {
      // Con letra grande (o si la palabra no cabe ni a 14 sp) no caben las palabras en tres columnas: una fila por juego.
      Column(verticalArrangement = Arrangement.spacedBy(2.dp)) { ui.stops.forEachIndexed { i, s -> HoyStopRow(s, i + 1, onClick = { onStop(s) }) } }
    } else {
      Row(Modifier.fillMaxWidth().height(IntrinsicSize.Max), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        ui.stops.forEachIndexed { i, s -> HoyStone(s, i + 1, fit, onClick = { onStop(s) }, modifier = Modifier.weight(1f).fillMaxHeight()) }
      }
    }
  }
}

private val HoyStoneMuted = Color(0xFF4A4470)

/** Lo que dice el lector de pantalla de una parada: «Bodega de carga, Observatorio, hecho / es la que sigue / después». */
private fun stopDescription(stop: HomeStop): String =
  "${stop.title}, ${stop.place}, " + when (stop.state) { StopState.DONE -> "hecho"; StopState.NEXT -> "es la que sigue"; StopState.LATER -> "después" }

/** El número de una parada (o el visto bueno si ya está hecha: forma, no solo color). */
@Composable
private fun StopNumber(stop: HomeStop, number: Int) {
  val scale = LocalDensity.current.fontScale.coerceIn(1f, 1.6f)   // el círculo crece con la letra: con letra 2,0 el número no cabría en 24 dp
  Box(Modifier.size(24.dp * scale).clip(RoundedCornerShape(50)).background(Color(0xFFFFF8EC)).border(2.dp, Clay.Ink, RoundedCornerShape(50)), contentAlignment = Alignment.Center) {
    if (stop.state == StopState.DONE) com.example.ui.components.KitGlyph(com.example.ui.components.Glyph.CHECK, size = 15.dp * scale)
    else Text("$number", color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold)
  }
}

/**
 * Una piedrita de plasticina: su número (o el visto bueno), el lugar en la isla y el nombre del juego. La que sigue va en sol y dice «Sigue» en lugar del lugar; las hechas, en lima con el visto y «Hecho»; el resto, en crema con su lugar.
 * Mide 88 dp como mínimo (84 + la sombra dura) y las tres miden lo mismo.
 */
@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun HoyStone(stop: HomeStop, number: Int, fit: StoneFit, onClick: () -> Unit, modifier: Modifier = Modifier) {
  val color = when (stop.state) { StopState.DONE -> Clay.Lime; StopState.NEXT -> Clay.Sun; StopState.LATER -> HoyCream }
  val tag = when (stop.state) { StopState.DONE -> "Hecho"; StopState.NEXT -> "Sigue"; StopState.LATER -> "Después" }      // (Tarea 143, ítem 18) antes la tercera decía el lugar y las otras dos el estado; el lugar ya se ve en la isla
  ClayCard(
    modifier = modifier.heightIn(min = 90.dp).semantics(mergeDescendants = true) { contentDescription = stopDescription(stop) }.testTag("home_stop_$number"),   // 84 dp de losa + 6 de arranque de sombra
    color = color, size = SlabSize.MEDIUM, onClick = onClick, contentPadding = 0.dp
  ) {
    Column(Modifier.fillMaxWidth().padding(horizontal = fit.padding, vertical = 8.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
      // El número y el lugar van en una línea; si el lugar («Biblioteca») no cabe al lado (pantallas de ~360 dp), baja a la línea de abajo en vez de partirse a media palabra.
      FlowRow(horizontalArrangement = Arrangement.spacedBy(6.dp), verticalArrangement = Arrangement.spacedBy(2.dp)) {
        StopNumber(stop, number)
        Box(Modifier.heightIn(min = 24.dp), contentAlignment = Alignment.CenterStart) {
          Text(tag, color = if (stop.state == StopState.LATER) HoyStoneMuted else Clay.Ink, fontSize = 16.sp, fontWeight = if (stop.state == StopState.LATER) FontWeight.Normal else FontWeight.Bold, lineHeight = 20.sp)
        }
      }
      Text(stop.title, color = Clay.Ink, fontSize = fit.titleSp.sp, fontWeight = FontWeight.SemiBold, lineHeight = 17.sp, modifier = Modifier.testTag("home_stop_title_$number"))
    }
  }
}

/** La parada como fila (letra grande): su número (o el visto bueno), el juego, su lugar y si está hecha, es la que sigue o viene después. La que sigue va en sol; las hechas, en lima con visto. */
@Composable
private fun HoyStopRow(stop: HomeStop, number: Int, onClick: () -> Unit) {
  val color = when (stop.state) { StopState.DONE -> Clay.Lime; StopState.NEXT -> Clay.Sun; StopState.LATER -> HoyCream }
  val status = when (stop.state) { StopState.DONE -> "Hecho"; StopState.NEXT -> "Sigue"; StopState.LATER -> "Después" }
  ClayCard(
    modifier = Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = stopDescription(stop) }.testTag("home_stop_$number"),
    color = color, size = SlabSize.SMALL, onClick = onClick, contentPadding = 0.dp
  ) {
    Row(Modifier.fillMaxWidth().heightIn(min = 44.dp).padding(horizontal = 10.dp, vertical = 5.dp), verticalAlignment = Alignment.CenterVertically) {
      StopNumber(stop, number)
      Spacer(Modifier.width(10.dp))
      Column(Modifier.weight(1f)) {
        Text(stop.title, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 19.sp)
        Text(stop.place, color = Clay.Ink, fontSize = 16.sp, lineHeight = 20.sp)
      }
      Spacer(Modifier.width(8.dp))
      Text(status, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 20.sp, textAlign = TextAlign.End, modifier = Modifier.widthIn(max = 112.dp))
    }
  }
}

/** Lo que Hoy dice debajo del botón cuando hay algo que decir: la partida en pausa, la ruta ya cumplida o el Primer vuelo. Antes lo decía el globo de Nubi. */
internal fun captionLine(ui: HomeUi): String? {
  val pending = ui.stops.count { it.state != StopState.DONE }
  return when {
    ui.pausedTitle != null -> "Tienes una partida en pausa: ${ui.pausedTitle}."
    ui.flightDay -> "Tu primer vuelo fue tu ruta de hoy. Mañana empieza tu ruta."
    ui.stops.isNotEmpty() && pending == 0 -> "Ya pasaste por la isla hoy. Mañana sigo aquí."
    else -> null
  }
}

/** UN solo botón grande: seguir la partida en pausa, empezar o seguir la ruta de hoy, o «Ruta completa» cuando ya no queda nada; debajo, lo que haya que decir ([captionLine]) y, si todavía no hizo la evaluación, la invitación a hacerla. */
@Composable
private fun HoyAction(ui: HomeUi, onPlay: () -> Unit, onResume: () -> Unit, onBaseline: () -> Unit) {
  val pending = ui.stops.count { it.state != StopState.DONE }
  Column(Modifier.fillMaxWidth().padding(start = 16.dp, end = 16.dp, top = 2.dp, bottom = 2.dp)) {
    when {
      ui.pausedTitle != null ->
        ClayButton(text = "Seguir tu partida", onClick = onResume, glyph = Glyph.PLAY, fontSize = 18.sp, modifier = Modifier.testTag("btn_resume_paused"))
      pending > 0 -> {
        val label = if (ui.completed == 0) "Empezar la ruta · ${ui.stops.size} juegos" else "Seguir la ruta · $pending ${if (pending == 1) "juego" else "juegos"}"
        ClayButton(text = label, onClick = onPlay, glyph = Glyph.PLAY, fontSize = 18.sp, modifier = Modifier.testTag("btn_home_play"))
      }
      ui.stops.isNotEmpty() ->
        ClayCard(modifier = Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = "Ruta completa" }.testTag("home_route_done"), color = Clay.Lime, contentPadding = 0.dp) {
          Row(Modifier.fillMaxWidth().heightIn(min = 56.dp).padding(horizontal = 8.dp, vertical = 6.dp), horizontalArrangement = Arrangement.Center, verticalAlignment = Alignment.CenterVertically) {
            KitGlyph(Glyph.CHECK)
            Spacer(Modifier.width(8.dp))
            Text("Ruta completa", color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold)
          }
        }
    }
    captionLine(ui)?.let { line ->
      OnBoardText(
        line, color = HoySoft, fontSize = 16.sp, lineHeight = 21.sp, textAlign = TextAlign.Center,
        modifier = Modifier.fillMaxWidth().padding(top = 4.dp).testTag(if (ui.flightDay) "home_flight_day" else "home_caption")
      )
    }
    if (ui.baselineInvite) {
      // Un solo sol en Hoy (Tarea 147, P7): el enlace sigue siendo un enlace pero en crema tenue (`OnNightDim`) con su chevrón, para que no compita con el botón grande, que lleva el único sol
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
  }
}


/**
 * Detalle de un área (vista "C" aprobada por Ricardo): Nubi explica, la barra grande con las 5 etapas nombradas, de
 * dónde a dónde avanzó en la semana y cuánto falta para la siguiente, las últimas 4 semanas y sus juegos. Abajo, en
 * vez de "Jugar Memoria", la pregunta de Nubi va en su globo (Tarea 143) y abajo el botón "Jugar <juego>" (Nubi elige el juego del área
 * sin jugar o el más olvidado y dice por qué). Flechas (o deslizar sobre Nubi) para pasar a otra área.
 */
@Composable
internal fun AreaDetail(
  areaKey: String,
  statuses: List<AreaStatus>,
  names: Map<String, String>,
  history: List<GamePlayResult>,
  measures: List<MeasurePoint>,
  now: Long,
  lang: com.example.model.AppLanguage,
  onArea: (String) -> Unit,
  /** «Jugar <juego>»: juega directo el juego que Nubi elige. */
  onPlay: (String) -> Unit,
  /** Tocar el nombre (la fila) de un juego: abre su ficha (Tarea 87). */
  onGame: (String) -> Unit,
  onClose: () -> Unit,
  modifier: Modifier = Modifier,
  /** A dónde vuelve la flecha: «Avance» (de donde se abre hoy); antes la cabecera decía «Hoy» aunque se abriera desde Avance (Tarea 143, A1). */
  backLabel: String = "Avance",
  /** LA sugerencia de Nubi de toda la app ([com.example.data.Suggestion.next]): en su área, Nubi dice por qué; en las otras, el botón propone el juego que más tiempo lleva sin jugarse, sin «Te sugiero». */
  suggestion: com.example.data.Suggestion.Pick? = null
) {
  val idx = AREA_ORDER.indexOf(areaKey).coerceAtLeast(0)
  val s = statuses.getOrNull(idx) ?: AreaStatus(areaKey, null, 0f, emptyList())
  val name = names[areaKey] ?: areaKey
  val domain = domainOf(areaKey) ?: return
  val games = GameRegistry.allGames.filter { it.domain == domain }
  val lastPlayed = games.associate { g -> g.id to history.filter { it.gameId == g.id }.maxOfOrNull { it.timestamp } }
  val pick = com.example.data.Suggestion.forArea(areaKey, suggestion, games, lastPlayed.mapNotNull { (id, t) -> t?.let { id to it } }.toMap(), now, Streak::dayOf)
  val suggested = pick?.let { p -> games.firstOrNull { it.id == p.gameId } }
  val suggestedHere = com.example.data.Suggestion.suggestedHere(areaKey, suggestion)
  fun go(step: Int) = onArea(AREA_ORDER[(idx + step + AREA_ORDER.size) % AREA_ORDER.size])

  Column(modifier.fillMaxSize().testTag("area_detail")) {
    Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(start = 12.dp, top = 4.dp)) {
      com.example.ui.components.KitBackButton(onClose, Modifier.testTag("area_back"), label = "Volver a $backLabel")
      Spacer(Modifier.width(10.dp))
      OnBoardText(backLabel, color = OnNight, fontSize = 17.sp, fontWeight = FontWeight.SemiBold)
    }
    Column(
      Modifier.weight(1f).verticalScroll(rememberScrollState()).padding(horizontal = 20.dp),
      horizontalAlignment = Alignment.CenterHorizontally
    ) {
      // Maqueta B de la Propuesta 3 (Tarea 147): primero el ÁREA (su nombre y su etapa a la izquierda, las flechas ‹ › a la derecha: antes Nubi iba entre las flechas, como si se pasara de Nubi en Nubi) y debajo Nubi M con su globo al lado.
      // Una voz: lo que dice Nubi va en su globo, en primera persona (la pregunta y, si hay juego elegido, por qué lo eligió; antes el porqué iba suelto como «Nubi lo eligió: …»).
      var drag by remember(areaKey) { mutableStateOf(0f) }
      val globeText = com.example.data.Suggestion.detailGlobe(suggested?.let { getGameTitle(it.id, lang, it.title) }, if (suggestedHere) pick else null)      // DOS frases como mucho y sin repetir la línea de avance de abajo (Opus, 21-oct)
      Row(
        verticalAlignment = Alignment.CenterVertically,
        modifier = Modifier.fillMaxWidth().padding(top = 4.dp).pointerInput(areaKey) {
          detectHorizontalDragGestures(
            onDragEnd = { if (drag > 60.dp.toPx()) go(-1) else if (drag < -60.dp.toPx()) go(1); drag = 0f },
            onDragCancel = { drag = 0f }
          ) { _, dx -> drag += dx }
        }
      ) {
        Column(Modifier.weight(1f)) {
          OnBoardText(name, color = Color.White, fontSize = 28.sp, fontWeight = FontWeight.Bold, modifier = Modifier.testTag("area_name"))
          OnBoardText(
            s.value?.let { Skill.stageHeader(Skill.stageName(it), s.points ?: 0) } ?: "Aún sin medir",         // el mismo encabezado que la lámina y la ficha (Tarea 150, B1.6)
            color = OnNightDim, fontSize = 16.sp
          )
        }
        AreaArrow(left = true, onClick = { go(-1) })
        Spacer(Modifier.width(8.dp))
        AreaArrow(left = false, onClick = { go(1) })
      }
      Row(Modifier.fillMaxWidth().padding(top = 8.dp), verticalAlignment = Alignment.CenterVertically) {
        NubiClay(com.example.ui.components.NubiSize.M)
        Spacer(Modifier.width(6.dp))
        NubiBubble(
          text = globeText,
          tailLeft = true, modifier = Modifier.weight(1f).testTag("area_nubi_globe")
        )
      }
      Spacer(Modifier.height(4.dp))
      com.example.ui.components.StageScale(s.value, onCard = false, change = s.change, barModifier = Modifier.semantics { contentDescription = areaDescription(name, s) })
      Spacer(Modifier.height(8.dp))
      OnBoardText(AreaProgress.changeLine(s), color = Color.White, fontSize = 17.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center)
      AreaProgress.toNextLine(s)?.let { OnBoardText(it, color = OnNightDim, fontSize = 16.sp) }
      if (s.value != null && s.weeks.count { it != null } >= 2) WeeksChart(s.weeks)
      Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.padding(vertical = 12.dp)) {
        AREA_ORDER.forEachIndexed { i, _ ->
          Box(
            Modifier.size(if (i == idx) 10.dp else 8.dp).clip(RoundedCornerShape(50))
              .background(if (i == idx) Color.White else Color.White.copy(alpha = 0.25f))
          )
        }
      }
      OnBoardText(
        "Tus juegos de ${name.lowercase()}", color = Clay.Sun, fontSize = 16.sp, fontWeight = FontWeight.Bold,
        modifier = Modifier.fillMaxWidth().padding(top = 4.dp, bottom = 2.dp)
      )
      games.forEach { g ->
        val m = StarMeasures.latest(measures, g.id)
        val last = lastPlayed[g.id]
        val detail = when {
          m != null -> StarMeasures.zoneText(m.first, measures)?.let { "$it · promedio reciente" } ?: "${m.first.format(m.second)} ${m.first.unit} · tu última"
          last == null -> "Sin jugar aún"
          else -> playedAgo(now, last)
        }
        // Cada juego es una tarjeta de plasticina tocable (Tarea 90): tocarla abre su ficha.
        ClayCard(Modifier.fillMaxWidth().padding(vertical = 2.dp).testTag("area_game_${g.id}"), onClick = { onGame(g.id) }, contentPadding = 0.dp) {
          Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.fillMaxWidth().heightIn(min = 52.dp).padding(horizontal = 14.dp, vertical = 6.dp)) {
            com.example.ui.components.GameEmblem(g.id, 40.dp)
            Spacer(Modifier.width(12.dp))
            Column {
              Text(getGameTitle(g.id, lang, g.title), color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold)
              Text(detail, color = Clay.InkMuted, fontSize = 16.sp)
            }
          }
        }
      }
      Spacer(Modifier.height(8.dp))
    }
    if (suggested != null) {
      // La invitación: el botón «Jugar <juego>» (la pregunta ya la hizo Nubi en su globo) y por qué lo eligió, sin repetir lo que ya dice la tarjeta del juego.
      Column(Modifier.fillMaxWidth().padding(start = 20.dp, end = 20.dp, top = 8.dp, bottom = 10.dp), horizontalAlignment = Alignment.CenterHorizontally) {
        val title = getGameTitle(suggested.id, lang, suggested.title)
        ClayButton(text = "Jugar $title", onClick = { onPlay(suggested.id) }, leading = { com.example.ui.components.GameEmblem(suggested.id, 32.dp) }, modifier = Modifier.testTag("area_play"))
      }
    }
  }
}

private fun playedAgo(now: Long, last: Long): String = when (val days = (dayIndex(now) - dayIndex(last)).toInt()) {
  0 -> "Jugado hoy"
  1 -> "Jugado ayer"
  else -> "Hace $days días que no lo juegas"
}

@Composable
private fun AreaArrow(left: Boolean, onClick: () -> Unit) {
  com.example.ui.components.ClayDisc(com.example.ui.components.DiscStyle.CREAM, onClick, if (left) "Área anterior" else "Área siguiente") {
    com.example.ui.components.KitGlyph(if (left) Glyph.LEFT else Glyph.RIGHT, size = 28.dp)
  }
}

/** Las últimas 4 semanas en la misma escala 0-100: una línea lavanda, la de esta semana en sol, con su número. */
@Composable
private fun WeeksChart(weeks: List<Float?>) {
  val vals = weeks.map { it?.let { v -> (v * 100f) } }
  val known = vals.filterNotNull()
  val lo = (known.minOrNull() ?: 0f) - 4f
  val hi = (known.maxOrNull() ?: 0f) + 4f
  Column(Modifier.fillMaxWidth().padding(top = 16.dp)) {
    Row(Modifier.fillMaxWidth()) {
      vals.forEachIndexed { i, v ->
        OnBoardText(
          v?.let { "${it.toInt()}" } ?: "", fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.weight(1f),
          color = if (i == vals.lastIndex) Color.White else OnNightSoft, fontWeight = if (i == vals.lastIndex) FontWeight.Bold else FontWeight.Normal
        )
      }
    }
    Canvas(Modifier.fillMaxWidth().height(56.dp).padding(vertical = 6.dp)) {
      fun p(i: Int, v: Float) = Offset(size.width * (i + 0.5f) / vals.size, size.height * (1f - (v - lo) / (hi - lo)))
      val pts = vals.mapIndexedNotNull { i, v -> v?.let { p(i, it) } }
      val path = Path().apply { pts.forEachIndexed { i, o -> if (i == 0) moveTo(o.x, o.y) else lineTo(o.x, o.y) } }
      drawPath(path, AreaBarColor, style = Stroke(2.5.dp.toPx(), cap = StrokeCap.Round, join = StrokeJoin.Round))
      pts.forEachIndexed { i, o ->
        val last = i == pts.lastIndex
        drawCircle(Clay.Ink, (if (last) 6 else 5).dp.toPx(), o)
        drawCircle(if (last) Clay.Sun else AreaBarColor, (if (last) 4.5f else 3.5f).dp.toPx(), o)
      }
    }
    Row(Modifier.fillMaxWidth()) {
      listOf("hace 3 sem.", "hace 2 sem.", "sem. pasada", "esta sem.").forEachIndexed { i, t ->
        OnBoardText(t, fontSize = 14.sp, textAlign = TextAlign.Center, color = if (i == 3) Color.White else OnNightSoft, modifier = Modifier.weight(1f))
      }
    }
  }
}
