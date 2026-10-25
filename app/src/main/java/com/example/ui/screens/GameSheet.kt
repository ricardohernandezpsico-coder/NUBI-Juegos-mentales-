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
import androidx.compose.ui.semantics.heading
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
import com.example.data.AreaPoints
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
import com.example.data.GamePoints
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

/**
 * La ficha del juego y todo lo que necesita (Tarea 158, 21-oct): estaba en `GamesLibraryScreen.kt`, que se fue con la pestaña Juegos («La isla es el mapa»). La ficha ([GameSheet], [GameSheetContent], [GameSheetHost]) la abren las filas de la lámina de un lugar en Hoy,
 * las de la lámina de un área en Avance, las piedritas de «Tu ruta de hoy», «Tus medidas» y las filas de «Todos los juegos» (`AllGamesScreen.kt`).
 */
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
  /** Tus puntos en este juego (null = sin medir aún): «180 de 250 puntos», con su etapa (Tarea 159). */
  val points: GamePoints?,
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
 * La ficha de [game] con sus datos ya leídos del ViewModel: la usan Hoy (la piedrita de «Tu ruta de hoy» y las filas de la lámina de un lugar) y Avance (las filas del detalle de un área), igual que Juegos. Tocar el nombre de un juego abre SIEMPRE esta ficha;
 * solo los botones grandes («Empezar la ruta», «Jugar en el …», «Jugar <juego>» del detalle de un área y el «Jugar · …» de la propia ficha) juegan directo (Tarea 87).
 */
@Composable
internal fun GameSheetHost(game: GameDefinition, viewModel: NeuroVidaViewModel, onDismiss: () -> Unit, onPlay: (PlayMode, Boolean) -> Unit, onTutorial: () -> Unit = {}) {
  val history by viewModel.gameHistory.collectAsState()
  val settings by viewModel.userSettings.collectAsState()
  val measures by viewModel.starMeasures.collectAsState()
  val progressLog by viewModel.progressLog.collectAsState()
  val skill by viewModel.skill.collectAsState()
  val lang = LocalAppLanguage.current
  val now = remember(game.id) { System.currentTimeMillis() }
  // Los puntos del juego salen de los de su área, para que la ficha y la página del área digan lo mismo (el redondeo se reparte entre los juegos del área)
  val gamePoints = remember(game.id, progressLog, now) { AreaPoints.compute(game.domain.name, AreaPoints.gamesOf(game.domain.name), progressLog, now).perGame.firstOrNull { it.gameId == game.id } }
  val data = cardData(
    game, getGameTitle(game.id, lang, game.title), history.filter { it.gameId == game.id }.maxOfOrNull { it.timestamp }, now, null, measures,
    history.count { it.gameId == game.id && now - it.timestamp <= 14 * DAY_MS }, points = gamePoints
  )
  GameSheet(
    data = data,
    age = settings.ageBand,
    expertOpen = game.id in skill.expertOpen,
    selected = PlayMode.A_TU_MEDIDA,
    timed = viewModel.retoFor(game.id),
    onDismiss = onDismiss,
    onPlay = onPlay,
    onTutorial = onTutorial
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
  playsLast14: Int,
  /** Los puntos del juego ya repartidos con el resto de su área ([AreaPoints.perGame]); sin ellos se calculan a solas desde [progress]. */
  points: GamePoints? = null
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
    points = points ?: progress?.let { GamePoints.of(g.id, it) },
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
 * Ficha del juego (ventana superpuesta, crema): desde la Tarea 171 (25-oct) es una PORTADA (ver [GameSheetContent]; antes fue la propuesta «A · Compacta» de la Tarea 74 y la ficha con UNA decisión visible de la Tarea 140). Vive aquí lo que no es de la ficha misma: la ventana, la elección
 * («A tu medida» por defecto) y el reloj (se recuerda por juego: data/RetoChoice.kt), «Más opciones» y «Sobre este juego» abiertos o no (para volver de la lámina con la ficha como estaba) y los toques de fuera y Atrás, que cierran primero la lámina y luego la ficha.
 * La tarjeta va CENTRADA en el espacio libre entre las barras del sistema (Tarea 140: Ricardo la quiso más arriba, no pegada abajo; sube desde abajo y con «quitar animaciones» aparece de una vez), dentro de ellas, y mide como máximo el alto que queda (el contenido hace scroll por dentro solo si no cabe).
 */
@Composable
internal fun GameSheet(
  data: GameCardData,
  age: AgeBand?,
  expertOpen: Boolean,
  selected: PlayMode,
  timed: Boolean,
  onDismiss: () -> Unit,
  onPlay: (PlayMode, Boolean) -> Unit,
  /** «Cómo se juega», en «Sobre este juego»: abre el tutorial del juego otra vez (Tarea 171). */
  onTutorial: () -> Unit = {}
) {
  var choice by remember(data.game.id) { mutableStateOf(selected) }
  var withClock by remember(data.game.id) { mutableStateOf(timed) }
  // «Sobre este juego» abierto (la lámina ocupa el lugar de la ficha) y «Más opciones» desplegadas: viven aquí para que volver de la lámina deje la ficha como estaba
  var about by rememberSaveable(data.game.id) { mutableStateOf(false) }
  val options = rememberSaveable(data.game.id) { mutableStateOf(false) }
  val close: () -> Unit = { if (about) about = false else onDismiss() }              // Atrás y un toque fuera cierran la lámina primero y la ficha después
  val reduceMotion = com.example.ui.components.rememberReduceMotion()
  // `decorFitsSystemWindows = false` (Tarea 81): desde Android 15 la ventana de un diálogo ya va borde a borde, pero con el valor por defecto (true) los insets de las barras le llegan al contenido en CERO y `safeDrawing` no sirve de nada:
  // en el Motorola (Android 16, navegación de 3 botones) la barra se dibujaba ENCIMA de «Jugar». Con false la ventana es borde a borde a propósito y Compose recibe los insets reales de las barras.
  Dialog(onDismissRequest = close, properties = DialogProperties(usePlatformDefaultWidth = false, decorFitsSystemWindows = false)) {
    // Con `usePlatformDefaultWidth = false` Compose mide el contenido para TODA la pantalla, pero Android 15+ ajusta el marco de la ventana del diálogo DENTRO de las barras (`fitInsetsTypes`): la tarjeta se desbordaba y la barra tapaba «Jugar».
    // Se hace la ventana borde a borde de verdad para que `safeDrawing` reciba los insets reales (barra de 3 botones o de gestos) y la tarjeta quede dentro de ellos.
    val dialogWindow = (androidx.compose.ui.platform.LocalView.current.parent as? androidx.compose.ui.window.DialogWindowProvider)?.window
    SideEffect { dialogWindow?.let(::makeDialogWindowEdgeToEdge) }
    // La ventana ocupa toda la pantalla pero la tarjeta vive DENTRO de las barras del sistema (estado y navegación) con un margen (Tarea 73: en el Motorola «Jugar» caía bajo la barra de navegación).
    // Un toque fuera de la tarjeta la cierra (como antes); los toques sobre la tarjeta no.
    Box(
      Modifier
        .fillMaxSize()
        .clickable(interactionSource = remember { MutableInteractionSource() }, indication = null, onClick = close)
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
          if (about) GameAboutContent(data, onClose = { about = false }, onHowToPlay = onTutorial)
          else GameSheetContent(
            data, age, expertOpen, choice, onChoose = { choice = it }, onPlay = { onPlay(choice, withClock) }, onClose = onDismiss, timed = withClock, onChooseTimed = { withClock = it },
            onPlayDesafio = { onPlay(PlayMode.DESAFIO, withClock) }, onAbout = { about = true }, options = options
          )
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

/** La portada de la ficha (Tarea 171, 25-oct): una ventanilla noche de 150 dp con la escena real del juego; el emblema (a la izquierda) y «Sobre este juego» (a la derecha) asoman bajo su borde y el nombre va debajo, a todo el ancho. */
private val CoverHeight = 150.dp
private val EmblemSize = 56.dp
private val EmblemDrop = 28.dp             // cuánto asoma el emblema bajo la ventanilla: la mitad
private val CoverInset = 10.dp            // el aire entre la ventanilla y el borde de la tarjeta
private val SideInset = 18.dp             // el margen del texto y de los botones

/** El tamaño del nombre del juego: 24 sp, o menos (hasta 18) si UNA de sus palabras no cabe en [room]: nunca se parte una palabra («Constelacione/s»). */
@Composable
private fun titleSizeFor(title: String, room: Dp): Int {
  val measurer = rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  val density = androidx.compose.ui.platform.LocalDensity.current
  val roomPx = with(density) { room.roundToPx() }
  val longest = title.split(' ').maxByOrNull { it.length } ?: title
  return listOf(24, 22, 20, 18).firstOrNull { sp ->
    measurer.measure(longest, base.merge(TextStyle(fontSize = sp.sp, fontWeight = FontWeight.Bold)), softWrap = false, maxLines = 1).size.width <= roomPx
  } ?: 18
}

/**
 * La ficha del juego como la PORTADA de una tienda (Tarea 171; `auditoria-fable/ficha-juego.md`, opción 1, la que Ricardo eligió el 25-oct tras mostrarla a 3 personas: nadie la leía, todas tocaban «Jugar» o «Desafío», el texto cuesta con poca visión y se veía apagada).
 * De arriba abajo: la ventanilla noche con la escena real del juego ([GameCover]; sobre ella la X, y en su borde de abajo ASOMAN el emblema, a la izquierda, y «Sobre este juego», a la derecha), el nombre y UNA línea, «Etapa Maestro · 230 de 250 puntos» con su barra (sin las 5 etiquetas), «Jugar» en sol con lo elegido en su segunda línea
 * («a tu medida · sin reloj»), y una fila con «Desafío» (lila, con el cometa de Hoy) y «Más opciones», que despliega debajo las casillas de siempre: «¿Cómo quieres jugar?» (Suave · A tu medida · Desafío · Al límite, con su candado hasta superar un Desafío) y «¿Con reloj?» (no en un juego sin Reto).
 * Ya no están, en la ficha: la marca con su gráfico y su chip, la constancia, las 5 etiquetas de las etapas, «Te faltan N puntos», «Qué pones en juego» (pasó a «Sobre este juego») ni «Cambiar cómo jugar». Un solo sol a la vez: el de «Jugar». Cabe sin deslizar con letra 1,3 mientras «Más opciones» está plegado; desplegado, la tarjeta se desliza sola.
 * La tarjeta entera hace scroll si no cabe (con letra 2,0 en un teléfono común): «Jugar» ya no va fijo abajo porque está en la primera pantalla.
 */
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
  onChooseTimed: (Boolean) -> Unit = {},
  /** «Desafío»: juega ya en Desafío, con el reloj que esté elegido. */
  onPlayDesafio: () -> Unit = {},
  /** «Sobre este juego»: abre la lámina con «Qué pones en juego», su fuente y «Cómo se juega». */
  onAbout: () -> Unit = {},
  /** «Más opciones» desplegadas. La ventana la sube para que sobreviva a abrir «Sobre este juego» y volver; sola, vive mientras la ficha está abierta (por juego). */
  options: MutableState<Boolean> = rememberSaveable(data.game.id) { mutableStateOf(false) }
) {
  val g = data.game
  val density = androidx.compose.ui.platform.LocalDensity.current
  // Con letra muy grande (> 1,3) las casillas van de a UNA por fila y los dos botones de abajo se apilan: así ninguna palabra se parte ni se corta.
  val largeText = density.fontScale > 1.3f
  val supportsReto = RetoChoice.supports(g.id)
  var opciones by options
  // Dónde cae el foco tras plegar o desplegar (solo cuando la persona toca el botón: una rotación con las opciones abiertas no le mueve el foco): 0 = nada, 1 = la casilla elegida, 2 = «Más opciones»
  val focusChosen = remember { FocusRequester() }
  val focusChange = remember { FocusRequester() }
  var focusMove by remember { mutableIntStateOf(0) }
  val scroll = rememberScrollState()
  val reduceMotion = com.example.ui.components.rememberReduceMotion()
  LaunchedEffect(focusMove) {
    if (focusMove != 0) {
      withFrameNanos { }                                  // espera a que las casillas recién compuestas estén puestas en pantalla antes de pedirles el foco
      // Al desplegar la ficha se desliza hasta el final: en un teléfono común «¿Con reloj?» quedaba bajo el pliegue y nadie se enteraba de que estaba (con «quitar animaciones», de un salto)
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
    Column(Modifier.weight(1f, fill = false).verticalScroll(scroll).padding(bottom = 10.dp)) {
      // La ventanilla con la escena del juego y la X arriba a la derecha; en su borde de abajo ASOMAN, el emblema a la izquierda y «Sobre este juego» a la derecha: así la escena queda libre arriba y al centro
      Box(Modifier.fillMaxWidth().padding(start = CoverInset, end = CoverInset, top = CoverInset)) {
        com.example.ui.components.GameCover(g.id, Modifier.testTag("sheet_cover"))
        com.example.ui.components.KitCloseButton(onClose, Modifier.align(Alignment.TopEnd).padding(top = 8.dp, end = 8.dp).testTag("sheet_close"))
        GameEmblem(g.id, EmblemSize, Modifier.align(Alignment.BottomStart).offset(x = 8.dp, y = EmblemDrop).testTag("sheet_emblem"))
        ClaySlab(
          SlabColor.CREAM, Modifier.align(Alignment.BottomEnd).padding(start = EmblemSize + 24.dp, end = 8.dp).offset(y = EmblemDrop).testTag("sheet_about").semantics { contentDescription = "Sobre este juego: qué pones en juego y su fuente" },
          SlabSize.SMALL, onClick = onAbout, contentAlignment = Alignment.CenterStart, onClickLabel = "Abrir"
        ) {
          Row(Modifier.heightIn(min = 48.dp).padding(horizontal = 12.dp, vertical = 4.dp), verticalAlignment = Alignment.CenterVertically) {
            KitGlyph(Glyph.INFO, size = 24.dp)
            Spacer(Modifier.width(8.dp))
            Text("Sobre este juego", color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 20.sp, modifier = Modifier.weight(1f, fill = false))
          }
        }
      }

      // El nombre y UNA línea, a todo el ancho y debajo de lo que asoma
      BoxWithConstraints(Modifier.fillMaxWidth().padding(top = EmblemDrop + 6.dp)) {
        val titleSp = titleSizeFor(data.title, maxWidth - SideInset * 2)
        Column(Modifier.padding(horizontal = SideInset)) {
          Text(data.title, color = Clay.Ink, fontSize = titleSp.sp, fontWeight = FontWeight.Bold, lineHeight = (titleSp + 4).sp, modifier = Modifier.semantics { heading() }.testTag("sheet_title"))
          Text(g.subtitle, color = CardMuted, fontSize = Small, lineHeight = 20.sp, modifier = Modifier.testTag("sheet_subtitle"))
        }
      }

      Column(Modifier.fillMaxWidth().padding(horizontal = SideInset)) {
        // Tu avance, en UNA línea con su barra (sin las 5 etiquetas de las etapas: la línea ya dice la etapa y Avance tiene la escala): «Etapa Maestro · 230 de 250 puntos» en 20 con el color de su área (Tarea 159: los puntos del juego)
        if (data.points != null && data.points.measured) {
          Text(data.points.stageLine, color = onCream(g.domain), fontSize = 20.sp, fontWeight = FontWeight.Bold, lineHeight = 24.sp, modifier = Modifier.padding(top = 10.dp).testTag("sheet_stage"))
          com.example.ui.components.StageScale(data.points.fill, onCard = true, showNames = false, modifier = Modifier.padding(top = 6.dp))
        } else {
          Text("Aún sin medir", color = Clay.Ink, fontSize = 20.sp, fontWeight = FontWeight.Bold, lineHeight = 24.sp, modifier = Modifier.padding(top = 10.dp).testTag("sheet_stage"))
          Text("Juega a tu medida una vez y verás dónde estás.", color = CardMuted, fontSize = Small, lineHeight = 20.sp)
        }

        // «Jugar»: el único sol de la ficha, con lo elegido dicho en su segunda línea («a tu medida · sin reloj»). Lo elegido se cambia en «Más opciones».
        val spoken = RetoChoice.playSpoken(choice, timed, supportsReto)
        KitButton(
          text = "Jugar", secondLine = RetoChoice.playDetail(choice, timed, supportsReto), secondLineSize = 16.sp, onClick = onPlay, glyph = Glyph.PLAY, fontSize = 20.sp,
          modifier = Modifier.padding(top = 12.dp).testTag("sheet_play").semantics { contentDescription = "Jugar, $spoken" }
        )

        // «Desafío» (lila, con el cometa de Hoy) y «Más opciones» (crema), lado a lado; apilados con letra muy grande o si una palabra no cabe en su media fila
        val measurer = rememberTextMeasurer()
        val base = androidx.compose.material3.LocalTextStyle.current
        BoxWithConstraints(Modifier.fillMaxWidth().padding(top = 8.dp)) {
          val half = (maxWidth - 8.dp) / 2
          val room = with(density) { (half - 24.dp - 34.dp).roundToPx() }              // lo que le queda al texto: 12 + 12 de aire y el ícono (24) con su espacio (10)
          val style = base.merge(TextStyle(fontSize = 17.sp, fontWeight = FontWeight.Bold))
          val sideBySide = !largeText && listOf("Desafío", "opciones").all { measurer.measure(it, style, softWrap = false, maxLines = 1).size.width <= room }
          val desafio: @Composable (Modifier, Boolean) -> Unit = { m, stretch ->
            KitButton(
              text = "Desafío", onClick = onPlayDesafio, color = SlabColor.LILAC, glyph = Glyph.COMET, minHeight = 56.dp, centerContent = stretch,
              modifier = m.testTag("sheet_desafio").semantics { contentDescription = "Desafío. ${PlayMode.DESAFIO.short}" }
            )
          }
          val more: @Composable (Modifier, Boolean) -> Unit = { m, stretch ->
            KitButton(
              text = if (opciones) "Menos opciones" else "Más opciones", onClick = { opciones = !opciones; focusMove = if (opciones) 1 else 2 }, color = SlabColor.CREAM, glyph = if (opciones) Glyph.UP else Glyph.DOWN,
              minHeight = 56.dp, centerContent = stretch, onClickLabel = if (opciones) "Ocultar opciones" else "Mostrar opciones",
              modifier = m.focusRequester(focusChange).focusable().testTag("sheet_change").semantics {
                contentDescription = (if (opciones) "Menos opciones" else "Más opciones") + ". Ahora: $spoken"
                stateDescription = if (opciones) "opciones visibles" else "opciones ocultas"
              }
            )
          }
          if (sideBySide) {
            Row(Modifier.fillMaxWidth().height(IntrinsicSize.Min), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
              desafio(Modifier.weight(1f).fillMaxHeight(), true)
              more(Modifier.weight(1f).fillMaxHeight(), true)
            }
          } else {
            Column(Modifier.fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(8.dp)) {
              desafio(Modifier, false)
              more(Modifier, false)
            }
          }
        }

        // Las dos secciones solo se componen con «Más opciones» desplegadas, y entran y salen cambiando de ALTURA con suavidad (la tarjeta, centrada en la ventana, crece hacia arriba y hacia abajo):
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
            if (supportsReto) {
              HorizontalLine()
              Text("¿Con reloj?", color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(bottom = 8.dp))
              val duration = RetoChoice.seconds(g.id)?.let { RetoChoice.durationText(it) } ?: "con tiempo"
              // Lado a lado mientras CADA palabra quepa en su media fila («segundos» no: en un teléfono angosto o con letra 1,3 se partía en «segund / os»); si no, de a UNA por fila
              BoxWithConstraints(Modifier.fillMaxWidth()) {
                val cellText = with(density) { (((maxWidth - 8.dp) / 2) - 80.dp).roundToPx() }                    // la media fila menos 10 + 10 de aire, el ícono (24) con su espacio (8) y la insignia ✓ (6 + 22)
                val style = base.merge(TextStyle(fontSize = 16.sp, fontWeight = FontWeight.Bold))
                val side = !largeText && listOf("Sin reloj", "a tu ritmo", "Con reloj", duration).flatMap { it.split(' ') }.all { measurer.measure(it, style, softWrap = false, maxLines = 1).size.width <= cellText }
              Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                listOf(false to ("Sin reloj" to "a tu ritmo"), true to ("Con reloj" to duration)).chunked(if (side) 2 else 1).forEach { group ->
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
      }
    }
  }
}

/**
 * «Sobre este juego» (Tarea 171): la lámina que abre el botón de la portada, en el lugar de la ficha y con la X para volver a ella. Lleva lo que la ficha tenía de «leer»: la frase del juego, «Qué pones en juego» con su fuente (la regla de Ricardo: la explicación y su fuente van juntas y siempre a un toque; nadie tiene que leerlas para jugar)
 * y, fijo abajo, «Cómo se juega», que abre el tutorial del juego otra vez aunque la persona ya lo haya jugado. El final de partida sigue mostrando «¿Por qué importa?» en las 3 primeras partidas de cada juego.
 */
@Composable
internal fun GameAboutContent(data: GameCardData, onClose: () -> Unit = {}, onHowToPlay: () -> Unit = {}) {
  val g = data.game
  val scroll = rememberScrollState()
  ClayCard(color = CardCream, size = SlabSize.LARGE, contentPadding = 0.dp, modifier = Modifier.fillMaxWidth().testTag("game_about")) {
    Column(Modifier.weight(1f, fill = false).verticalScroll(scroll).padding(start = SideInset, end = SideInset, top = 14.dp, bottom = 6.dp)) {
      Row(verticalAlignment = Alignment.CenterVertically) {
        GameEmblem(g.id, 48.dp)
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
          Text("Sobre este juego", color = CardMuted, fontSize = Small, lineHeight = 20.sp, modifier = Modifier.testTag("about_caption"))
          Text(data.title, color = Clay.Ink, fontSize = 21.sp, fontWeight = FontWeight.Bold, lineHeight = 25.sp, modifier = Modifier.semantics { heading() }.testTag("about_title"))
        }
        Spacer(Modifier.width(8.dp))
        com.example.ui.components.KitCloseButton(onClose, Modifier.testTag("about_close"), label = "Cerrar y volver a la ficha")
      }
      HorizontalLine()
      Text(g.subtitle, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.testTag("about_subtitle"))
      FinalesConSentido.sheetFor(g.id)?.let { (whyText, whySource) ->
        Column(Modifier.padding(top = 12.dp).testTag("sheet_why")) {
          Text("Qué pones en juego", color = Clay.InkMuted, fontSize = 16.sp, fontWeight = FontWeight.Bold)
          Text(whyText, color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 4.dp))
          Text(whySource, color = Clay.InkSoft, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 6.dp))
        }
      }
    }
    Box(Modifier.fillMaxWidth().padding(horizontal = SideInset).height(1.dp).background(CardTrack))
    KitButton(
      text = "Cómo se juega", onClick = onHowToPlay, color = SlabColor.CREAM, glyph = Glyph.PLAY, fontSize = 18.sp,
      modifier = Modifier.padding(start = SideInset, end = SideInset, top = 10.dp, bottom = 8.dp).testTag("about_how")
    )
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

