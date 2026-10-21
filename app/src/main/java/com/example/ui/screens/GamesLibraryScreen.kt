package com.example.ui.screens

import com.example.ui.components.ClaySlab
import com.example.ui.components.Glyph
import com.example.ui.components.KitGlyph
import com.example.ui.components.SlabColor
import com.example.ui.components.SlabSize
import androidx.activity.compose.BackHandler
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.EnterTransition
import androidx.compose.animation.ExitTransition
import androidx.compose.animation.core.MutableTransitionState
import androidx.compose.animation.core.tween
import androidx.compose.animation.expandVertically
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.shrinkVertically
import androidx.compose.animation.slideInVertically
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.focusable
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.PathMeasure
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.layout
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.disabled
import androidx.compose.ui.semantics.liveRegion
import androidx.compose.ui.semantics.selected
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.stateDescription
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.rememberTextMeasurer
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import com.example.data.AreaProgress
import com.example.data.Suggestion
import com.example.data.AreaStatus
import com.example.data.DiscoveryKind
import com.example.data.FinalesConSentido
import com.example.ui.components.OnBoardText
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.AreaBar
import com.example.ui.components.LockTabSwipe
import com.example.ui.components.NubiBubble
import com.example.ui.components.BuildingIcon
import com.example.ui.components.BuildingOnPedestal
import com.example.ui.components.GameEmblem
import com.example.ui.components.KitBar
import com.example.ui.components.NubiMotion
import com.example.data.LibraryOrder
import com.example.ui.components.areaDescription
import com.example.data.PlayMode
import com.example.data.RetoChoice
import com.example.ui.components.KitButton
import com.example.data.Skill
import com.example.data.measureText
import com.example.data.StarMeasures
import com.example.model.AgeBand
import com.example.model.DomainType
import com.example.model.GameDefinition
import com.example.model.GameRegistry
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getDomainName
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayCard
import com.example.viewmodel.NeuroVidaViewModel
import kotlinx.coroutines.delay
import kotlin.math.abs

private const val DAY_MS = 86_400_000L
private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = com.example.ui.theme.NubiTokens.OnNightDim
private val TileClay = Color(0xFF28306E)
private val CardCream = Color(0xFFFFF8EC)
private val CardMuted = Color(0xFF514B78) // sobre crema: 7:1
private val CardTrack = Color(0xFFE2DAC8)
private val ModeRow = Color(0xFFF6EEDE)
private val ModeRowOn = Color(0xFFFFF0C8)

// Tamaños pensados para adultos mayores (28-sep, pedido de Ricardo): ningún texto secundario bajo 14 sp.
private val Small = 16.sp
private val Body = 16.sp

private fun dayIndex(ts: Long): Long = (ts + java.util.TimeZone.getDefault().getOffset(ts)) / DAY_MS

/** Color del área oscurecido para texto sobre crema (contraste ≥ 4,5). */
private fun onCream(domain: DomainType): Color = lerp(domain.color, Clay.Ink, 0.45f)

/** Todo lo que muestran la casilla y la ficha de un juego (armado desde el ViewModel; separado para la foto de prueba). */
data class GameCardData(
  val game: GameDefinition,
  val title: String,
  val lastPlayed: String,
  val playedToday: Boolean,
  /** Tu avance 0..1 (null = sin medir aún). */
  val progress: Float?,
  /** Tu marca de juego estrella: nombre ("Tu brújula"), valor ("a 18% de casa"), etiqueta y últimas partidas. */
  val measureTitle: String? = null,
  val measureValue: String? = null,
  val measureTag: String? = null,
  val measureSeries: List<Float> = emptyList(),
  val lowerIsBetter: Boolean = false,
  /** Partidas de este juego en los últimos 14 días. */
  val playsLast14: Int = 0,
  /** Su número de parada (1 a 3) si es de la ruta de hoy, y si ya se hizo en la ruta (Tarea 125). */
  val routeStop: Int? = null,
  val routeDone: Boolean = false
)

/**
 * Pestaña Juegos (29-sep, 3.ª versión, pedido de Ricardo tras probar el deslizar entre pestañas: en Juegos también se deslizaba para cambiar de área y los dos gestos confundían). Arriba "Juegos" con tu perfil y opciones; debajo LAS 4
 * ÁREAS A LA VEZ ([AreaGrid]: planeta con su ícono, nombre, la barra de avance de Hoy y la etapa; sello sol = jugada hoy).
 * Tocar un área abre LA MISMA LÁMINA que Hoy y que Avance (Tarea 155, «un área, una sola vista»; `auditoria-fable/spec-area-unica.md`): el avance del área y sus juegos en filas, con «Jugar <el que sugiere Nubi>» fijo abajo; sube sobre la pestaña con su velo,
 * las pestañas no se deslizan, X, Atrás o el velo la cierran y Nubi vuela hasta ella desde su sitio en «¿Qué jugamos hoy?». Tocar una fila abre la FICHA ([GameSheet]): avance, marca, constancia, dificultad y Jugar; Atrás vuelve a la lámina. Al volver de un juego lanzado desde aquí
 * la lámina queda abierta como se dejó (`NeuroVidaViewModel.libraryFocus`). La primera vez, el globo de Nubi dice que los juegos de cada lugar se ven aquí, como en Hoy. Razonamiento de la dificultad: `docs/dificultad-y-avance.md`.
 */
@Composable
fun GamesLibraryScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier
) {
  val history by viewModel.gameHistory.collectAsState()
  val userSettings by viewModel.userSettings.collectAsState()
  val measures by viewModel.starMeasures.collectAsState()
  val progress by viewModel.gameProgress.collectAsState()
  val progressLog by viewModel.progressLog.collectAsState()
  val skill by viewModel.skill.collectAsState()
  val focus by viewModel.libraryFocus.collectAsState()
  val daily by viewModel.dailySession.collectAsState()
  val lang = LocalAppLanguage.current
  val now = remember(history) { System.currentTimeMillis() }

  val lastPlayed = remember(history) { history.groupBy { it.gameId }.mapValues { (_, l) -> l.maxOf { it.timestamp } } }
  val statuses = remember(progress, progressLog, now) {
    AREA_ORDER.map { key ->
      AreaProgress.status(key, GameRegistry.allGames.filter { it.domain.name == key }.map { it.id }, progress, progressLog, now)
    }
  }
  val playedToday = remember(history, now) {
    history.filter { dayIndex(it.timestamp) == dayIndex(now) }.mapNotNull { GameRegistry.getById(it.gameId)?.domain?.name }.toSet()
  }
  val modes = remember { mutableStateMapOf<String, PlayMode>() }
  var sheetFor by remember { mutableStateOf<GameDefinition?>(null) }
  val scope = androidx.compose.runtime.rememberCoroutineScope()
  val reduceMotion = com.example.ui.components.rememberReduceMotion()

  // LA sugerencia de Nubi de toda la app (Suggestion.next): el encabezado la dice y la lámina de su área la marca y la juega
  val suggestion = remember(daily, lastPlayed, now, lang) { suggestArea(daily, lastPlayed, now, lang) }
  val pick = remember(daily, lastPlayed, now) { Suggestion.next(daily.gameIds, daily.completedCount, lastPlayed, now, com.example.data.Streak::dayOf) }
  val places = rememberAreaPlaces(statuses, history, measures, now, lang, pick, daily.gameIds, daily.completedCount)
  val fx = remember { PlaceFx(looks = false) }
  val anchor = remember { NubiAnchor() }
  // La primera vez que se abre una lámina en Juegos, el globo de Nubi lo cuenta (una sola vez; se anota al abrir)
  var introNow by remember { mutableStateOf(false) }
  val shownPlaces = if (introNow) places.mapValues { (_, p) -> p.copy(bubble = com.example.data.PlaceSheets.AREA_SHEET_INTRO) } else places

  fun placeKeyOf(domain: DomainType): String? = com.example.data.IslaLugar.nameOfArea(domain.name)?.lowercase()
  fun openArea(areaKey: String) {
    val domain = DomainType.fromStored(areaKey) ?: return
    val place = placeKeyOf(domain) ?: return
    viewModel.setLibraryFocus(domain, null)
    introNow = !viewModel.areaSheetIntroSeen
    viewModel.markAreaSheetIntroSeen()
    fx.open(scope, place, reduceMotion)
  }
  fun closeArea() {
    fx.close(scope, reduceMotion)
    viewModel.closeLibraryArea()
  }
  // Al volver de una partida lanzada desde aquí la lámina queda abierta como se dejó (sin animar); si el foco se cierra por otro lado (cambiar de pestaña), la lámina se va de golpe
  androidx.compose.runtime.LaunchedEffect(focus.open, focus.domain) {
    val domain = focus.domain
    if (focus.open && domain != null && !fx.active) placeKeyOf(domain)?.let { fx.snapOpen(scope, it) }
    else if (!focus.open && fx.active && !fx.closing) fx.reset(scope)
  }

  AreaSheetScaffold(
    fx = fx, places = shownPlaces, anchor = anchor, showChart = false,
    onClose = ::closeArea,
    onPlay = { id -> viewModel.playFromLibrary(id, PlayMode.A_TU_MEDIDA, viewModel.retoFor(id)) },
    onGame = { id -> GameRegistry.getById(id)?.let { g -> viewModel.setLibraryFocus(g.domain, g.id); sheetFor = g } },
    modifier = modifier
  ) {
    Column(modifier = Modifier.fillMaxSize().padding(bottom = com.example.ui.components.LocalBarInset.current)) {      // la franja de la barra de pestañas queda libre; la lámina de un área, encima, la cubre
      TabTopBar(viewModel) {}
      SuggestionHeader(suggestion, anchor)
      AreaGrid(
        statuses = statuses,
        playedToday = playedToday,
        suggested = suggestion.key,
        lang = lang,
        onArea = ::openArea,
        modifier = Modifier.weight(1f)
      )
    }
  }

  sheetFor?.let { g ->
    val data = cardData(g, getGameTitle(g.id, lang, g.title), lastPlayed[g.id], now, progress[g.id], measures,
      history.count { it.gameId == g.id && now - it.timestamp <= 14 * DAY_MS })
    GameSheet(
      data = data,
      age = userSettings.ageBand,
      expertOpen = g.id in skill.expertOpen,
      selected = modes[g.id] ?: PlayMode.A_TU_MEDIDA,
      timed = viewModel.retoFor(g.id),        // la elección de ese juego (o el ajuste viejo de Ajustes, si todavía no eligió); se lee al abrir la ficha
      onDismiss = { sheetFor = null },
      onPlay = { m, timed ->
        modes[g.id] = m
        sheetFor = null
        viewModel.playFromLibrary(g.id, m, timed)
      }
    )
  }
}

/**
 * La ficha de [game] con sus datos ya leídos del ViewModel: la usan Hoy (la piedrita de «Tu ruta de hoy» y las filas de la lámina de un lugar) y Avance (las filas del detalle de un área), igual que Juegos. Tocar el nombre de un juego abre SIEMPRE esta ficha;
 * solo los botones grandes («Empezar la ruta», «Jugar en el …», «Jugar <juego>» del detalle de un área y el «Jugar · …» de la propia ficha) juegan directo (Tarea 87).
 */
@Composable
internal fun GameSheetHost(game: GameDefinition, viewModel: NeuroVidaViewModel, onDismiss: () -> Unit, onPlay: (PlayMode, Boolean) -> Unit) {
  val history by viewModel.gameHistory.collectAsState()
  val settings by viewModel.userSettings.collectAsState()
  val measures by viewModel.starMeasures.collectAsState()
  val progress by viewModel.gameProgress.collectAsState()
  val skill by viewModel.skill.collectAsState()
  val lang = LocalAppLanguage.current
  val now = remember(game.id) { System.currentTimeMillis() }
  val data = cardData(
    game, getGameTitle(game.id, lang, game.title), history.filter { it.gameId == game.id }.maxOfOrNull { it.timestamp }, now, progress[game.id], measures,
    history.count { it.gameId == game.id && now - it.timestamp <= 14 * DAY_MS }
  )
  GameSheet(
    data = data,
    age = settings.ageBand,
    expertOpen = game.id in skill.expertOpen,
    selected = PlayMode.A_TU_MEDIDA,
    timed = viewModel.retoFor(game.id),
    onDismiss = onDismiss,
    onPlay = onPlay
  )
}

/** Arma los datos de un juego: cuándo se jugó, avance, marca con su etiqueta y constancia. */
internal fun cardData(
  g: GameDefinition,
  title: String,
  last: Long?,
  now: Long,
  progress: Float?,
  measures: List<com.example.data.MeasurePoint>,
  playsLast14: Int
): GameCardData {
  val daysAgo = last?.let { (dayIndex(now) - dayIndex(it)).toInt() }
  val lastText = when (daysAgo) {
    null -> "Aún no lo juegas"
    0 -> "Jugaste hoy"
    1 -> "Jugaste ayer"
    else -> "Jugaste hace $daysAgo días"
  }
  val def = StarMeasures.defForGame(g.id)
  // Solo partidas comparables con la última (mismo reloj; nivel parecido si la marca depende del nivel).
  val series = def?.let { d -> StarMeasures.comparable(measures.filter { it.key == d.key }) }.orEmpty()
  val kind = if (def != null && series.size >= StarMeasures.MIN_POINTS) StarMeasures.discover(series, now)?.kind else null
  return GameCardData(
    game = g,
    title = title,
    lastPlayed = lastText,
    playedToday = daysAgo == 0,
    progress = progress,
    measureTitle = def?.short?.replaceFirstChar { it.uppercase() },
    measureValue = def?.let { StarMeasures.zoneText(it, measures) }                        // Freno: la zona del promedio, no milisegundos
      ?: series.lastOrNull()?.let { p -> def?.let { measureText(it, p.value) } },
    measureTag = when (kind) {
      DiscoveryKind.RECORD -> "Tu récord"
      DiscoveryKind.IMPROVING -> "Mejorando"
      DiscoveryKind.STEADY -> "Se mantiene"
      null -> null
    },
    measureSeries = series.takeLast(StarMeasures.MAX_POINTS).map { it.value },
    lowerIsBetter = def?.lowerIsBetter ?: false,
    playsLast14 = playsLast14
  )
}

/**
 * Ficha del juego (ventana superpuesta, crema), propuesta «A · Compacta» (Ricardo la eligió el 10-oct, Tarea 74, para los 18 juegos; boceto: https://claude.ai/artifact/AhQyja6q32NChTLp6FfDHQ): el juego, tu avance con su etapa,
 * tu marca en UNA fila (con su gráfico), tu constancia, cómo quieres jugar (4 modos en una cuadrícula de 2 × 2 con sus aciertos esperados; Experto se abre al superar un Desafío), «Sin reloj / Con reloj» (se recuerda por juego:
 * data/RetoChoice.kt) y Jugar. Su meta: que quepa ENTERA en un teléfono común (393 × 873 dp) sin deslizar, con el botón siempre a la vista.
 * La tarjeta va CENTRADA en el espacio libre entre las barras del sistema (Tarea 140: Ricardo la quiso más arriba, no pegada abajo; sube desde abajo y con «quitar animaciones» aparece de una vez), dentro de ellas, mide como máximo el alto que queda (el contenido hace scroll solo si no cabe; al desplegar crece hacia arriba y hacia abajo) y «Jugar» va fijo abajo, fuera del scroll.
 */
@Composable
internal fun GameSheet(
  data: GameCardData,
  age: AgeBand?,
  expertOpen: Boolean,
  selected: PlayMode,
  timed: Boolean,
  onDismiss: () -> Unit,
  onPlay: (PlayMode, Boolean) -> Unit
) {
  var choice by remember(data.game.id) { mutableStateOf(selected) }
  var withClock by remember(data.game.id) { mutableStateOf(timed) }
  val reduceMotion = com.example.ui.components.rememberReduceMotion()
  // `decorFitsSystemWindows = false` (Tarea 81): desde Android 15 la ventana de un diálogo ya va borde a borde, pero con el valor por defecto (true) los insets de las barras le llegan al contenido en CERO y `safeDrawing` no sirve de nada:
  // en el Motorola (Android 16, navegación de 3 botones) la barra se dibujaba ENCIMA de «Jugar». Con false la ventana es borde a borde a propósito y Compose recibe los insets reales de las barras.
  Dialog(onDismissRequest = onDismiss, properties = DialogProperties(usePlatformDefaultWidth = false, decorFitsSystemWindows = false)) {
    // Con `usePlatformDefaultWidth = false` Compose mide el contenido para TODA la pantalla, pero Android 15+ ajusta el marco de la ventana del diálogo DENTRO de las barras (`fitInsetsTypes`): la tarjeta se desbordaba y la barra tapaba «Jugar».
    // Se hace la ventana borde a borde de verdad para que `safeDrawing` reciba los insets reales (barra de 3 botones o de gestos) y la tarjeta quede dentro de ellos.
    val dialogWindow = (androidx.compose.ui.platform.LocalView.current.parent as? androidx.compose.ui.window.DialogWindowProvider)?.window
    SideEffect { dialogWindow?.let(::makeDialogWindowEdgeToEdge) }
    // La ventana ocupa toda la pantalla pero la tarjeta vive DENTRO de las barras del sistema (estado y navegación) con un margen (Tarea 73: en el Motorola «Jugar» caía bajo la barra de navegación).
    // Un toque fuera de la tarjeta la cierra (como antes); los toques sobre la tarjeta no.
    Box(
      Modifier
        .fillMaxSize()
        .clickable(interactionSource = remember { MutableInteractionSource() }, indication = null, onClick = onDismiss)
        .windowInsetsPadding(WindowInsets.safeDrawing)
        .padding(horizontal = 14.dp, vertical = 12.dp),
      contentAlignment = Alignment.Center
    ) {
      val shown = remember { MutableTransitionState(reduceMotion).apply { targetState = true } }
      AnimatedVisibility(
        visibleState = shown,
        enter = if (reduceMotion) EnterTransition.None else slideInVertically(initialOffsetY = { it }) + fadeIn(),
        exit = ExitTransition.None
      ) {
        Box(Modifier.fillMaxWidth().pointerInput(Unit) { detectTapGestures { } }) {
          GameSheetContent(data, age, expertOpen, choice, onChoose = { choice = it }, onPlay = { onPlay(choice, withClock) }, onClose = onDismiss, timed = withClock, onChooseTimed = { withClock = it })
        }
      }
    }
  }
}

/**
 * Deja la ventana de un diálogo BORDE A BORDE (Tarea 81): sin `fitInsetsTypes` el sistema no la recorta dentro de las barras y el contenido recibe los insets reales por `WindowInsets.safeDrawing`.
 * Antes de la API 30 no existe `fitInsetsTypes` y alcanza con `setDecorFitsSystemWindows(false)`.
 */
internal fun makeDialogWindowEdgeToEdge(window: android.view.Window) {
  androidx.core.view.WindowCompat.setDecorFitsSystemWindows(window, false)
  if (android.os.Build.VERSION.SDK_INT >= 30) {
    val attrs = window.attributes
    if (attrs.fitInsetsTypes != 0) {
      attrs.fitInsetsTypes = 0
      window.attributes = attrs
    }
  }
}

@Composable
internal fun GameSheetContent(
  data: GameCardData,
  age: AgeBand?,
  expertOpen: Boolean,
  choice: PlayMode,
  onChoose: (PlayMode) -> Unit,
  onPlay: () -> Unit,
  onClose: () -> Unit = {},
  timed: Boolean = false,
  onChooseTimed: (Boolean) -> Unit = {}
) {
  val g = data.game
  // Con letra muy grande (≥ 1,3) las casillas van de a UNA por fila y el título baja a su propia línea: así ninguna palabra se parte («Constelacione/s», «medid/a») ni se corta.
  val largeText = androidx.compose.ui.platform.LocalDensity.current.fontScale > 1.3f
  val supportsReto = RetoChoice.supports(g.id)
  // Plegada por defecto (Tarea 140): la ficha dice lo elegido y un solo botón lo cambia. La ventana recuerda si está desplegada solo mientras está abierta (por juego), no entre fichas.
  var opciones by rememberSaveable(g.id) { mutableStateOf(false) }
  // Dónde cae el foco tras plegar o desplegar (solo cuando la persona toca el botón: una rotación con las opciones abiertas no le mueve el foco): 0 = nada, 1 = la casilla elegida, 2 = «Cambiar cómo jugar»
  val focusChosen = remember { FocusRequester() }
  val focusChange = remember { FocusRequester() }
  var focusMove by remember { mutableIntStateOf(0) }
  val scroll = rememberScrollState()
  val reduceMotion = com.example.ui.components.rememberReduceMotion()
  LaunchedEffect(focusMove) {
    if (focusMove != 0) {
      withFrameNanos { }                                  // espera a que las casillas recién compuestas estén puestas en pantalla antes de pedirles el foco
      // Al desplegar la ficha se desliza hasta el final: con «Jugar» fijo abajo, en un teléfono común «¿Con reloj?» quedaba bajo el pliegue y nadie se enteraba de que estaba (con «quitar animaciones», de un salto)
      if (focusMove == 1) {
        if (reduceMotion) scroll.scrollTo(scroll.maxValue)
        else { delay(NubiMotion.STATE_MS.toLong()); scroll.animateScrollTo(scroll.maxValue) }           // primero crece la tarjeta; al terminar, se desliza
      }
      when (focusMove) {
        1 -> runCatching { focusChosen.requestFocus() }
        2 -> runCatching { focusChange.requestFocus() }
      }
    }
  }
  ClayCard(color = CardCream, size = SlabSize.LARGE, contentPadding = 0.dp, modifier = Modifier.fillMaxWidth().testTag("game_sheet")) {
    // Lo que hace scroll (todo menos «Jugar»): ocupa lo que haga falta y, si no cabe, se corta al alto disponible de la tarjeta.
    Column(Modifier.weight(1f, fill = false).verticalScroll(scroll).padding(start = 18.dp, end = 18.dp, top = 12.dp, bottom = 6.dp)) {
      // El juego: planeta de 48, título de 21 y subtítulo de 14
      if (largeText) {
        Row(verticalAlignment = Alignment.CenterVertically) {
          GameEmblem(g.id, 48.dp)
          Spacer(Modifier.weight(1f))
          com.example.ui.components.KitCloseButton(onClose, Modifier.testTag("sheet_close"))
        }
        Text(data.title, color = Clay.Ink, fontSize = 21.sp, fontWeight = FontWeight.Bold, lineHeight = 24.sp)
        Text(g.subtitle, color = CardMuted, fontSize = Small, lineHeight = 18.sp)
      } else Row(verticalAlignment = Alignment.CenterVertically) {
        GameEmblem(g.id, 48.dp)
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
          Text(data.title, color = Clay.Ink, fontSize = 21.sp, fontWeight = FontWeight.Bold, lineHeight = 24.sp)
          Text(g.subtitle, color = CardMuted, fontSize = Small, lineHeight = 18.sp)
        }
        com.example.ui.components.KitCloseButton(onClose, Modifier.testTag("sheet_close"))
      }

      // Tu avance: «34%» en 30, la etapa en 17 con el color de su área y cuánto falta en 14
      HorizontalLine()
      if (data.progress != null) {
        // El mismo encabezado de etapa y la misma frase de «cuánto falta» que la lámina y el detalle de un área (Tarea 150, B1.6 y B1.7: antes «55 / de 100 · Hábil · a 5 puntos de Experto»)
        Text(Skill.stageHeader(data.progress), color = onCream(g.domain), fontSize = 20.sp, fontWeight = FontWeight.Bold, lineHeight = 24.sp, modifier = Modifier.testTag("sheet_stage"))
        Text(Skill.toNextLine(data.progress) ?: "La etapa más alta", color = CardMuted, fontSize = Small, modifier = Modifier.testTag("sheet_to_next"))
        com.example.ui.components.StageScale(data.progress, onCard = true, modifier = Modifier.padding(top = 6.dp))
      } else {
        Text("Aún sin medir", color = Clay.Ink, fontSize = 20.sp, fontWeight = FontWeight.Bold)
        Text("Juega a tu medida una vez y verás dónde estás.", color = CardMuted, fontSize = Small)
      }

      // Tu marca, en UNA fila: a la izquierda su nombre (14) y su valor (17, hasta 2 líneas); a la derecha la etiqueta y debajo el gráfico de 76 × 28
      if (data.measureTitle != null && data.measureValue != null) {
        HorizontalLine()
        Row(verticalAlignment = Alignment.CenterVertically) {
          Column(Modifier.weight(1f)) {
            Text(data.measureTitle, color = CardMuted, fontSize = Small)
            Text(data.measureValue, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, maxLines = 2, lineHeight = 21.sp)
          }
          val hasChart = data.measureSeries.size >= 2
          if (data.measureTag != null || hasChart) {
            Column(horizontalAlignment = Alignment.End, modifier = Modifier.padding(start = 10.dp)) {
              if (data.measureTag != null) Tag(data.measureTag, Clay.Lime)
              if (hasChart) {
                CreamSparkline(
                  data.measureSeries, data.lowerIsBetter, g.domain.color,
                  Modifier.padding(top = if (data.measureTag != null) 4.dp else 0.dp).size(width = 76.dp, height = 28.dp)
                    .semantics { contentDescription = "Tus últimas partidas; mejor hacia arriba" }
                )
              }
            }
          }
        }
      }
      Text(
        "${data.lastPlayed} · " + when (data.playsLast14) {
          0 -> "sin partidas en 2 semanas"
          1 -> "1 partida en 2 semanas"
          else -> "${data.playsLast14} partidas en 2 semanas"
        },
        color = CardMuted, fontSize = Small, modifier = Modifier.padding(top = 6.dp)
      )

      // Qué pones en juego (final «B», Tarea 146): lo que antes iba en «¿Por qué importa?» al pie de cada final ahora vive aquí, en presente («Pones en juego…»), con su fuente. El final solo lo muestra en las 3 primeras partidas de cada juego.
      FinalesConSentido.sheetFor(g.id)?.let { (whyText, whySource) ->
        HorizontalLine()
        Column(Modifier.testTag("sheet_why")) {
          Text("Qué pones en juego", color = Clay.InkMuted, fontSize = 16.sp, fontWeight = FontWeight.Bold)
          Text(whyText, color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 4.dp))
          Text(whySource, color = Clay.InkSoft, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 6.dp))
        }
      }

      // Lo elegido, dicho en una línea, y el botón que lo cambia: «Cambiar cómo jugar» (plegada) / «Listo» (desplegada). El mismo botón, en el mismo lugar: las opciones se abren debajo y nada de lo de arriba se mueve.
      HorizontalLine()
      val spoken = RetoChoice.playSpoken(choice, timed, supportsReto)
      Text(
        RetoChoice.summaryLabel(choice, timed, supportsReto, RetoChoice.seconds(g.id)),
        color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, modifier = Modifier.testTag("sheet_summary")
      )
      KitButton(
        text = if (opciones) "Listo" else "Cambiar cómo jugar",
        onClick = { opciones = !opciones; focusMove = if (opciones) 1 else 2 },
        color = SlabColor.CREAM,
        glyph = if (opciones) Glyph.UP else Glyph.DOWN,
        fontSize = 16.sp,
        minHeight = 48.dp,
        onClickLabel = if (opciones) "Ocultar opciones" else "Mostrar opciones",
        modifier = Modifier.padding(top = 8.dp).focusRequester(focusChange).focusable().testTag("sheet_change").semantics {
          contentDescription = if (opciones) "Listo. Ahora: $spoken" else "Cambiar cómo jugar. Ahora: $spoken"
          stateDescription = if (opciones) "opciones visibles" else "opciones ocultas"
        }
      )

      // Las dos secciones solo se componen con las opciones desplegadas, y entran y salen cambiando de ALTURA con suavidad (la tarjeta, centrada en la ventana, crece hacia arriba y hacia abajo):
      // 200 ms (NubiMotion: cambio de estado) y, con «quitar animaciones», de un salto
      AnimatedVisibility(
        visible = opciones,
        enter = if (reduceMotion) EnterTransition.None else expandVertically(tween(NubiMotion.STATE_MS, easing = NubiMotion.Standard)) + fadeIn(tween(NubiMotion.STATE_MS, easing = NubiMotion.Standard)),
        exit = if (reduceMotion) ExitTransition.None else shrinkVertically(tween(NubiMotion.STATE_MS, easing = NubiMotion.Standard)) + fadeOut(tween(NubiMotion.STATE_MS, easing = NubiMotion.Standard))
      ) {
        Column(Modifier.fillMaxWidth()) {
          // Cómo quieres jugar: cuadrícula de 2 × 2 (cada cuadro, al menos 64 de alto); la explicación solo del elegido
          HorizontalLine()
          Text("¿Cómo quieres jugar?", color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(bottom = 8.dp).semantics { liveRegion = LiveRegionMode.Polite })
          PlayMode.entries.chunked(if (largeText) 1 else 2).forEachIndexed { rowIndex, pair ->
            Row(Modifier.fillMaxWidth().height(IntrinsicSize.Min).padding(top = if (rowIndex == 0) 0.dp else 8.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
              pair.forEach { m ->
                val open = Skill.isOpen(m, g.id, if (expertOpen) setOf(g.id) else emptySet())
                ChoiceCell(
                  selected = m == choice,
                  enabled = open,
                  minHeight = 64.dp,
                  description = m.label + (if (open) ", ${m.short}" else ", bloqueado: supera un Desafío"),
                  tag = "mode_${m.name}",
                  onClick = { onChoose(m) },
                  modifier = Modifier.weight(1f).then(if (m == choice) Modifier.focusRequester(focusChosen) else Modifier)
                ) {
                  Row(verticalAlignment = Alignment.CenterVertically) {
                    ModeBars(m.ordinal + 1, if (m == choice) Clay.Sun else g.domain.color, 20.dp)
                    Spacer(Modifier.width(8.dp))
                    // el nombre ocupa lo que queda y pasa a 2 líneas entre palabras («A tu / medida»); la insignia ✓ va al final de ESTA fila (su lugar queda reservado: el cuadro no salta al elegir)
                    Text(m.label, color = if (open) Clay.Ink else CardMuted, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 19.sp, modifier = Modifier.weight(1f))
                    if (open) SelectedMark(m == choice)
                  }
                  if (open) {
                    Text(m.short, color = Clay.Ink, fontSize = Small, modifier = Modifier.padding(top = 2.dp))                  // el nombre y una frase corta; los aciertos esperados son del DDA, no de la ficha (Tarea 151, B4.4)
                  } else {
                    Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(top = 2.dp)) {
                      KitGlyph(Glyph.LOCK, size = 20.dp)
                      Spacer(Modifier.width(2.dp))
                      Text("Supera un Desafío", color = CardMuted, fontSize = Small)
                    }
                  }
                }
              }
            }
          }
          Text(choice.what, color = CardMuted, fontSize = Small, lineHeight = 19.sp, modifier = Modifier.padding(top = 8.dp))

          // ¿Con o sin reloj? Se recuerda por juego; la ruta diaria de Hoy va siempre sin reloj. Dos cuadros lado a lado.
          if (com.example.data.RetoChoice.supports(g.id)) {
            HorizontalLine()
            Text("¿Con reloj?", color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(bottom = 8.dp))
            val duration = com.example.data.RetoChoice.seconds(g.id)?.let { com.example.data.RetoChoice.durationText(it) } ?: "con tiempo"
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
             listOf(false to ("Sin reloj" to "a tu ritmo"), true to ("Con reloj" to duration)).chunked(if (largeText) 1 else 2).forEach { group ->
              Row(Modifier.fillMaxWidth().height(IntrinsicSize.Min), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
              group.forEach { (value, texts) ->
                ChoiceCell(
                  selected = timed == value,
                  enabled = true,
                  minHeight = 56.dp,
                  description = texts.first + ", " + texts.second,
                  tag = if (value) "reto_con" else "reto_sin",
                  onClick = { onChooseTimed(value) },
                  modifier = Modifier.weight(1f)
                ) {
                  Row(verticalAlignment = Alignment.CenterVertically) {
                    KitGlyph(if (value) Glyph.CLOCK else Glyph.CLOCK_OFF)
                    Spacer(Modifier.width(8.dp))
                    Column(Modifier.weight(1f)) {
                      Text(texts.first, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 19.sp)
                      Text(texts.second, color = CardMuted, fontSize = Small)
                    }
                    SelectedMark(timed == value)
                  }
                }
              }
              }
             }
            }
          }
        }
      }
    }
    // «Jugar»: FUERA del scroll, fijo abajo, siempre a la vista: una línea fina arriba y el botón sol de 56 con borde de tinta, sombra dura y su play.
    Box(Modifier.fillMaxWidth().padding(horizontal = 18.dp).height(1.dp).background(CardTrack))
    SheetPlayButton(choice, timed, supportsReto, detailed = opciones, onPlay = onPlay)
  }
}

/**
 * El botón grande de la ficha, con lo elegido en palabras (Tarea 140): «Jugar · a tu medida · con reloj», «Jugar · suave · sin reloj», «Jugar · en Desafío · con reloj»; en un juego sin Reto, «Jugar · a tu medida».
 * Va en UNA línea mientras la más larga de las posibles quepa en lo que queda del botón (18, 17 o 16 sp: así el botón no cambia de forma al elegir) y en DOS líneas, «Jugar» y debajo «a tu medida · con reloj» a 16 sp, con letra ≥ 1,3, en
 * una ficha de teléfono de 360 dp o menos o si ni a 16 sp cabe en una. Nunca se parte una palabra. El lector de pantalla lo lee con comas: «Jugar, a tu medida, con reloj».
 */
@Composable
private fun SheetPlayButton(choice: PlayMode, timed: Boolean, supportsReto: Boolean, detailed: Boolean, onPlay: () -> Unit) {
  val density = androidx.compose.ui.platform.LocalDensity.current
  val measurer = rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  BoxWithConstraints(Modifier.fillMaxWidth().padding(start = 18.dp, end = 18.dp, top = 10.dp, bottom = 8.dp)) {
    val room = maxWidth - 58.dp                       // lo que le queda al texto dentro del botón: 12 + 12 de aire y el ícono (24) con su espacio (10)
    val oneLine: TextUnit? = remember(maxWidth, density.fontScale, supportsReto) {
      if (density.fontScale >= 1.3f || maxWidth <= 297.dp) null
      else listOf(18, 17, 16).firstOrNull { sp ->
        val style = base.merge(TextStyle(fontSize = sp.sp, fontWeight = FontWeight.Bold))
        PlayMode.entries.all { m ->
          measurer.measure(text = AnnotatedString(RetoChoice.playLabel(m, true, supportsReto)), style = style, softWrap = false, maxLines = 1).size.width <= with(density) { room.roundToPx() }
        }
      }?.sp
    }
    val modifier = Modifier.testTag("sheet_play").semantics { contentDescription = "Jugar, " + RetoChoice.playSpoken(choice, timed, supportsReto) }
    if (!detailed) {
      // Plegada (Tarea 152, C5): la línea de arriba («A tu medida · con reloj (2 min)») ya dice lo elegido; el botón dice «Jugar» a secas. Con las opciones desplegadas lleva lo elegido, que cambia al tocar.
      KitButton(text = "Jugar", onClick = onPlay, glyph = Glyph.PLAY, fontSize = 18.sp, modifier = modifier)
    } else if (oneLine != null) {
      KitButton(text = RetoChoice.playLabel(choice, timed, supportsReto), onClick = onPlay, glyph = Glyph.PLAY, fontSize = oneLine, modifier = modifier)
    } else {
      KitButton(text = "Jugar", secondLine = RetoChoice.playDetail(choice, timed, supportsReto), secondLineSize = 16.sp, onClick = onPlay, glyph = Glyph.PLAY, fontSize = 18.sp, modifier = modifier)
    }
  }
}

/**
 * Un cuadro de elección de la ficha (un modo de juego o «Sin reloj / Con reloj»): el elegido lleva fondo [ModeRowOn], borde de 3 y una insignia lima con ✓ ([SelectedMark], al final de la fila del nombre; nunca solo color); los demás, [ModeRow] con borde de 1,5.
 * Un cuadro bloqueado no se puede tocar. Semántica: botón de opción, elegido o no, y bloqueado con su motivo en la descripción.
 */
@Composable
private fun ChoiceCell(
  selected: Boolean,
  enabled: Boolean,
  minHeight: Dp,
  description: String,
  tag: String,
  onClick: () -> Unit,
  modifier: Modifier = Modifier,
  content: @Composable ColumnScope.() -> Unit
) {
  Box(modifier.fillMaxHeight()) {
    // Una losa chica de plasticina: la elegida va en lima con su ✓ y su texto en negrita y QUIETA; la imagen presionada solo se ve mientras el dedo está encima (elegido no es presionado). La bloqueada no se toca.
    ClaySlab(
      color = if (selected) SlabColor.LIME else SlabColor.CREAM,
      size = SlabSize.SMALL,
      onClick = if (enabled) onClick else null,
      role = Role.RadioButton,
      contentAlignment = Alignment.CenterStart,
      modifier = Modifier
        .fillMaxWidth()
        .fillMaxHeight()
        .heightIn(min = minHeight + 6.dp)
        .focusable(enabled)                               // un `clickable` solo acepta el foco con teclado o mando, NO en modo táctil (Compose): sin esto el foco que se pide al desplegar la ficha no llegaría a la casilla elegida (ni el lector de pantalla la anunciaría)
        .testTag(tag)                                     // en el MISMO nodo que «elegido» y «bloqueado» (las pruebas leen ahí)
        .semantics(mergeDescendants = true) {
          this.selected = selected
          contentDescription = description
          if (!enabled) disabled()
        }
    ) {
      Column(Modifier.fillMaxWidth().padding(horizontal = 10.dp, vertical = 8.dp), verticalArrangement = Arrangement.Center, content = content)
    }
  }
}

/** La insignia del elegido: el ✓ de churro tinta de 22 dp, al final de la fila del nombre. Sin ser el elegido deja SU LUGAR vacío (así el nombre no cambia de líneas ni el cuadro de alto al elegir). */
@Composable
private fun SelectedMark(on: Boolean) {
  Spacer(Modifier.width(6.dp))
  Box(Modifier.size(22.dp), contentAlignment = Alignment.Center) { if (on) KitGlyph(Glyph.CHECK, size = 22.dp) }
}

/** El área de LA sugerencia de Nubi (la de [Suggestion.next]): su clave, su nombre visible y el motivo en palabras de tiempo («sigue Tinta o Palabra en tu ruta de hoy», «hace 4 días que no juegas Constelaciones»). */
internal data class AreaSuggestionUi(val key: String, val name: String, val reason: String, val gameId: String? = null)

/** Arma la sugerencia de Nubi con la ruta de hoy ([daily]) y la última partida de cada juego ([lastPlayed]): [Suggestion.next], una sola regla para Juegos, Hoy y Avance (Tarea 150, B2). */
internal fun suggestArea(daily: com.example.model.DailySessionState, lastPlayed: Map<String, Long>, now: Long, lang: com.example.model.AppLanguage): AreaSuggestionUi {
  val pick = Suggestion.next(daily.gameIds, daily.completedCount, lastPlayed, now, com.example.data.Streak::dayOf)
    ?: Suggestion.Pick(GameRegistry.allGames.first().id, GameRegistry.allGames.first().domain.name, Suggestion.Why.NEVER)
  val game = GameRegistry.getById(pick.gameId) ?: GameRegistry.allGames.first()
  val domain = DomainType.fromStored(pick.area) ?: game.domain
  return AreaSuggestionUi(domain.name, getDomainName(domain, lang), Suggestion.reasonForArea(pick, getGameTitle(game.id, lang, game.title)), game.id)
}

/**
 * Encabezado "Nubi te sugiere" (30-sep, maqueta `docs/previews/cuatro-areas.png` Juegos · 2): Nubi científica a la
 * izquierda, la pregunta y, debajo, qué área sugiere y por qué.
 */
@Composable
internal fun SuggestionHeader(s: AreaSuggestionUi, anchor: NubiAnchor? = null) {
  Row(
    verticalAlignment = Alignment.CenterVertically,
    modifier = Modifier.fillMaxWidth().padding(start = 8.dp, end = 16.dp).testTag("suggestion_header")
  ) {
    com.example.ui.components.NubiClay(com.example.ui.components.NubiSize.M, modifier = Modifier.padding(start = 2.dp, end = 6.dp).let { if (anchor != null) it.markNubi(anchor) else it })
    Column(Modifier.weight(1f)) {
      OnBoardText("¿Qué jugamos hoy?", color = Color.White, fontSize = 24.sp, fontWeight = FontWeight.Bold, lineHeight = 28.sp)
      // Una voz (Tarea 147, P3): lo que sugiere Nubi lo dice Nubi, en primera persona y en SU globo; nunca texto suelto atribuido a ella
      NubiBubble(
        text = "Te sugiero ${s.name}: ${s.reason}",
        tailLeft = true, modifier = Modifier.padding(top = 4.dp).testTag("suggestion_text")
      )
    }
  }
}

/**
 * Las 4 áreas a la vez, en 2 x 2 que llena la pantalla hasta la barra de pestañas (aprobado por Ricardo, `docs/previews/cuatro-areas.png` Juegos · 2; Tarea 125): el edificio del área (observatorio, antena, hangar o biblioteca de la isla) sobre su pedestal de
 * plasticina (el sugerido, más grande, con la píldora «Sugerida» y la farola encendida del lugar; sello sol con ✓ = jugada hoy), el nombre, su subtítulo, el canal con el churro del kit y «Hábil · 7 juegos». Sin flechas ni deslizar: el único gesto hacia los costados
 * de esta pantalla es cambiar de pestaña. Si la letra del sistema es muy grande, la rejilla se desplaza y el nombre pasa a dos líneas.
 */
@Composable
internal fun AreaGrid(
  statuses: List<AreaStatus>,
  playedToday: Set<String>,
  suggested: String?,
  lang: com.example.model.AppLanguage,
  onArea: (String) -> Unit,
  modifier: Modifier = Modifier
) {
  BoxWithConstraints(modifier.fillMaxWidth().testTag("area_grid")) {
    val fontScale = androidx.compose.ui.platform.LocalDensity.current.fontScale
    // Con letra normal las dos filas llenan la pantalla (aprobado por Ricardo); con letra grande (> 1,15) cada fila mide lo que su contenido, van arriba con 24 dp entre ellas y el aire que sobra queda abajo (Tarea 143, ítem 7)
    val bigLetters = fontScale > 1.15f
    val rowHeight = maxOf(maxHeight / 2, 262.dp * maxOf(1f, fontScale))
    Column(Modifier.fillMaxWidth().verticalScroll(rememberScrollState()).padding(start = 12.dp, end = 12.dp, bottom = 16.dp), verticalArrangement = if (bigLetters) Arrangement.spacedBy(24.dp) else Arrangement.Top) {
      statuses.chunked(2).forEach { row ->
        Row(Modifier.fillMaxWidth().then(if (bigLetters) Modifier.height(IntrinsicSize.Max) else Modifier.height(rowHeight))) {
          row.forEach { s ->
            val domain = DomainType.fromStored(s.area) ?: DomainType.MEMORIA
            val name = getDomainName(domain, lang)
            val count = GameRegistry.allGames.count { it.domain == domain }
            val isSuggested = s.area == suggested
            val pedestal = if (isSuggested) 140.dp else if (fontScale >= 1.3f) 92.dp else 104.dp
            Column(
              horizontalAlignment = Alignment.CenterHorizontally,
              verticalArrangement = Arrangement.Center,
              modifier = Modifier
                .weight(1f)
                .fillMaxHeight()
                .clip(RoundedCornerShape(24.dp))
                .clickable { onArea(s.area) }
                .semantics(mergeDescendants = true) {
                  contentDescription = areaDescription(name, s) + (if (isSuggested) ". Nubi lo sugiere" else "") + ". Abrir sus juegos"
                }
                .testTag("area_${s.area.lowercase()}")
            ) {
              // Todas las cajas miden lo mismo (lo que mide la del sugerido): los nombres y las barras de las cuatro áreas quedan a la misma altura y el sugerido, más grande, crece hacia arriba.
              Box(Modifier.height(120.dp), contentAlignment = Alignment.BottomCenter) {
                BuildingOnPedestal(s.area, pedestal, lamp = isSuggested, overlay = {
                  if (s.area in playedToday) TodaySeal(Modifier.align(Alignment.TopCenter).offset(x = pedestal * 0.16f, y = pedestal * 0.18f))
                })
              }
              OnBoardText(name, color = Color.White, fontSize = 22.sp, fontWeight = FontWeight.Bold, maxLines = 2, textAlign = TextAlign.Center, lineHeight = 26.sp, modifier = Modifier.padding(top = 18.dp))
              OnBoardText(domain.tagline, color = OnNightDim, fontSize = 16.sp, maxLines = 2, textAlign = TextAlign.Center, lineHeight = 20.sp)
              KitBar(s.value, s.change, modifier = Modifier.width(140.dp).padding(vertical = 8.dp), onCard = false)
              OnBoardText(
                (s.value?.let { Skill.stageName(it) } ?: "Aún sin medir") + " · " + if (count == 1) "1 juego" else "$count juegos",
                color = OnNightDim, fontSize = 16.sp, maxLines = 2, textAlign = TextAlign.Center, lineHeight = 20.sp
              )
            }
          }
        }
      }
    }
  }
}

/** Sello sol con ✓: el área (o juego) ya se jugó hoy. La marca es una forma, no solo el color. */
@Composable
private fun TodaySeal(modifier: Modifier = Modifier) {
  Box(
    modifier
      .size(24.dp)
      .drawBehind { drawCircle(Clay.Ink, size.minDimension / 2f, center.copy(y = center.y + 2.dp.toPx())) }
      .clip(CircleShape)
      .background(Clay.Sun)
      .border(2.dp, Clay.Ink, CircleShape),
    contentAlignment = Alignment.Center
  ) { KitGlyph(Glyph.CHECK, Modifier.semantics { contentDescription = "Jugada hoy" }, size = 16.dp) }
}

@Composable
private fun HorizontalLine() {
  Box(Modifier.fillMaxWidth().padding(vertical = 7.dp).height(1.dp).background(CardTrack))
}

@Composable
private fun Tag(text: String, color: Color, modifier: Modifier = Modifier) {
  com.example.ui.theme.ClayPill(text, modifier, color = color, compact = true)
}

/** Últimas partidas de la marca, con "mejor" siempre hacia arriba; la última en sol. */
@Composable
private fun CreamSparkline(values: List<Float>, lowerIsBetter: Boolean, color: Color, modifier: Modifier) {
  Canvas(modifier) {
    val lo = values.min(); val hi = values.max(); val span = (hi - lo).takeIf { abs(it) > 1e-6f } ?: 1f
    val pad = 6.dp.toPx()
    val pts = values.mapIndexed { i, v ->
      val t = (v - lo) / span
      Offset(pad + (size.width - 2 * pad) * i / (values.size - 1), pad + (size.height - 2 * pad) * (if (lowerIsBetter) t else 1f - t))
    }
    for (i in 0 until pts.size - 1) drawLine(color, pts[i], pts[i + 1], 2.5.dp.toPx(), StrokeCap.Round)
    drawCircle(Clay.Sun, 4.dp.toPx(), pts.last())
    drawCircle(Clay.Ink, 4.dp.toPx(), pts.last(), style = Stroke(1.5.dp.toPx()))
  }
}

/** Cuánto cuesta un modo: 4 barras que crecen (se lee por la cantidad, no solo por el color). */
@Composable
private fun ModeBars(n: Int, color: Color, size: Dp = 24.dp) {
  Canvas(Modifier.size(size)) {
    val bw = this.size.width / 7f
    for (i in 0 until 4) {
      val h = this.size.height * (0.35f + 0.65f * i / 3f)
      val x = i * bw * 1.9f
      val on = i < n
      drawRoundRect(if (on) color else CardTrack, Offset(x, this.size.height - h), androidx.compose.ui.geometry.Size(bw, h), CornerRadius(2.dp.toPx()))
      if (on) drawRoundRect(Clay.Ink, Offset(x, this.size.height - h), androidx.compose.ui.geometry.Size(bw, h), CornerRadius(2.dp.toPx()), style = Stroke(1.dp.toPx()))
    }
  }
}

