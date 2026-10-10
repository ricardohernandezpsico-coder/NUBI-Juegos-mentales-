package com.example.ui.screens

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Timer
import androidx.compose.material.icons.filled.TimerOff
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import com.example.data.AreaProgress
import com.example.data.AreaSuggestion
import com.example.data.AreaStatus
import com.example.data.DiscoveryKind
import com.example.ui.components.AREA_BODY_FRACTION
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.AreaBar
import com.example.ui.components.LockTabSwipe
import com.example.ui.components.NubiBubble
import com.example.ui.components.areaDescription
import com.example.ui.components.areaPlanetRes
import com.example.data.PlayMode
import com.example.data.Skill
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
import kotlin.math.abs

private const val DAY_MS = 86_400_000L
private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = Color(0xFFC7D0FF)
private val TileClay = Color(0xFF28306E)
private val CardCream = Color(0xFFFFF8EC)
private val CardMuted = Color(0xFF514B78) // sobre crema: 7:1
private val CardTrack = Color(0xFFE2DAC8)
private val ModeRow = Color(0xFFF6EEDE)
private val ModeRowOn = Color(0xFFFFF0C8)

// Tamaños pensados para adultos mayores (28-sep, pedido de Ricardo): ningún texto secundario bajo 14 sp.
private val Small = 14.sp
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
  val playsLast14: Int = 0
)

/**
 * Pestaña Juegos (29-sep, 3.ª versión, pedido de Ricardo tras probar el deslizar entre pestañas: en Juegos también se
 * deslizaba para cambiar de área y los dos gestos confundían). Arriba "Juegos" con tu perfil y opciones; debajo LAS 4
 * ÁREAS A LA VEZ ([AreaGrid]: planeta con su ícono, nombre, la barra de avance de Hoy y la etapa; sello sol = jugada hoy).
 * Tocar un área abre su VENTANA ([AreaWindow]): tapa toda la pantalla, las pestañas no se deslizan, X arriba a la
 * derecha (o Atrás) y Nubi científica pregunta con cuál entrenar; debajo, sus juegos. Tocar una casilla abre la FICHA
 * ([GameSheet]): avance, marca, constancia, dificultad y Jugar. Al volver de un juego se abre en la misma ventana y
 * casilla (`NeuroVidaViewModel.libraryFocus`). Maquetas aprobadas: `docs/previews/juegos-nubi.png` y
 * `navegacion-nubi.png`. Razonamiento de la dificultad: `docs/dificultad-y-avance.md`.
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
  val open = focus.domain?.takeIf { focus.open }

  if (open == null) {
    val suggestion = remember(history, statuses, now, lang) {
      suggestArea(history.map { it.gameId to it.timestamp }, statuses, now, lang)
    }
    Column(modifier = modifier.fillMaxSize()) {
      TabTopBar(viewModel) {}
      SuggestionHeader(suggestion)
      AreaGrid(
        statuses = statuses,
        playedToday = playedToday,
        suggested = suggestion.key,
        lang = lang,
        onArea = { key -> DomainType.fromStored(key)?.let { viewModel.setLibraryFocus(it, null) } },
        modifier = Modifier.weight(1f)
      )
    }
  } else {
    LockTabSwipe()
    BackHandler { viewModel.closeLibraryArea() }
    val games = remember(open) {
      GameRegistry.allGames.filter { it.domain == open }.sortedBy { StarMeasures.defForGame(it.id) == null }
    }
    AreaWindow(
      domain = open,
      title = getDomainName(open, lang),
      status = statuses.firstOrNull { it.area == open.name },
      cards = games.map { g ->
        cardData(g, getGameTitle(g.id, lang, g.title), lastPlayed[g.id], now, progress[g.id], measures,
          history.count { it.gameId == g.id && now - it.timestamp <= 14 * DAY_MS })
      },
      focusGameId = focus.gameId,
      onClose = { viewModel.closeLibraryArea() },
      onGame = { g ->
        viewModel.setLibraryFocus(open, g.id)
        sheetFor = g
      },
      modifier = modifier
    )
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
 * Casilla de un juego en la lista (arcilla, tocable): planeta con el anillo de tu avance y ✓ si se jugó hoy; el nombre;
 * debajo la etapa y el % (o "Sin medir aún"); a la derecha tu marca si es juego estrella. [highlighted] = la última
 * que se abrió (al volver de jugar se ve dónde estabas).
 */
@Composable
internal fun GameTile(data: GameCardData, highlighted: Boolean, onClick: () -> Unit) {
  val g = data.game
  val ring = com.example.ui.components.domainTextColor(g.domain.name)
  ClayCard(
    color = if (highlighted) Color(0xFF343D86) else TileClay,
    radius = 22.dp, depth = 4.dp, contentPadding = 0.dp, onClick = onClick,
    modifier = Modifier.fillMaxWidth().testTag("tile_${g.id}")
  ) {
    Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(horizontal = 14.dp, vertical = 12.dp)) {
      Box(
        Modifier.size(66.dp).drawBehind {
          val st = 4.dp.toPx()
          drawCircle(Color.White.copy(alpha = 0.16f), size.minDimension / 2f - st / 2, style = Stroke(st))
          data.progress?.let {
            drawArc(ring, -90f, 360f * it.coerceIn(0.03f, 1f), false, Offset(st / 2, st / 2),
              androidx.compose.ui.geometry.Size(size.width - st, size.height - st), style = Stroke(st, cap = StrokeCap.Round))
          }
        },
        contentAlignment = Alignment.Center
      ) { com.example.ui.components.MiniPlanet(g.id, 52.dp, done = data.playedToday) }
      Spacer(Modifier.width(14.dp))
      Column(Modifier.weight(1f)) {
        Text(data.title, color = Color.White, fontSize = 19.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, maxLines = 2, overflow = TextOverflow.Ellipsis)
        Text(
          if (data.progress != null) "${Skill.stageName(data.progress)} · ${Skill.percent(data.progress)}%" else "Sin medir aún",
          color = if (data.progress != null) ring else OnNightDim, fontSize = Body, fontWeight = FontWeight.Medium,
          modifier = Modifier.padding(top = 2.dp)
        )
      }
      if (data.measureValue != null) {
        Spacer(Modifier.width(8.dp))
        Column(horizontalAlignment = Alignment.End, modifier = Modifier.widthIn(max = 120.dp)) {
          Text(data.measureTitle ?: "", color = OnNightDim, fontSize = Small, textAlign = TextAlign.End, maxLines = 1, overflow = TextOverflow.Ellipsis)
          Text(data.measureValue, color = Clay.Sun, fontSize = Body, fontWeight = FontWeight.Bold, textAlign = TextAlign.End, maxLines = 2)
        }
      }
    }
  }
}

/**
 * Ficha del juego (ventana superpuesta, crema): el juego, tu avance con su etapa, tu marca, tu constancia, cómo
 * quieres jugar (4 modos con sus aciertos esperados; Experto se abre al superar un Desafío), «Sin reloj / Contra el reloj» (se recuerda por juego: data/RetoChoice.kt) y Jugar.
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
  Dialog(onDismissRequest = onDismiss, properties = DialogProperties(usePlatformDefaultWidth = false)) {
    // 10-oct (Tarea 73): en el Motorola, en los juegos con medida, gráfico y reloj, la ficha quedaba más alta que la pantalla y «Jugar» caía bajo la barra de navegación. Ahora la ventana ocupa toda la
    // pantalla pero la tarjeta vive DENTRO de las barras del sistema (estado y navegación) con un margen, mide como máximo lo que queda (su contenido hace scroll por dentro) y «Jugar» va fijo abajo.
    // Un toque fuera de la tarjeta la cierra (como antes); los toques sobre la tarjeta no.
    Box(
      Modifier
        .fillMaxSize()
        .clickable(interactionSource = remember { MutableInteractionSource() }, indication = null, onClick = onDismiss)
        .windowInsetsPadding(WindowInsets.safeDrawing)
        .padding(horizontal = 14.dp, vertical = 12.dp),
      contentAlignment = Alignment.Center
    ) {
      Box(Modifier.fillMaxWidth().pointerInput(Unit) { detectTapGestures { } }) {
        GameSheetContent(data, age, expertOpen, choice, onChoose = { choice = it }, onPlay = { onPlay(choice, withClock) }, onClose = onDismiss, timed = withClock, onChooseTimed = { withClock = it })
      }
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
  ClayCard(color = CardCream, radius = 28.dp, contentPadding = 0.dp, modifier = Modifier.fillMaxWidth().testTag("game_sheet")) {
    // Lo que hace scroll (todo menos «Jugar»): ocupa lo que haga falta y, si no cabe, se corta al alto disponible de la tarjeta.
    Column(Modifier.weight(1f, fill = false).verticalScroll(rememberScrollState()).padding(start = 20.dp, end = 20.dp, top = 16.dp, bottom = 4.dp)) {
      // El juego
      Row(verticalAlignment = Alignment.CenterVertically) {
        com.example.ui.components.MiniPlanet(g.id, 58.dp)
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
          Text(data.title, color = Clay.Ink, fontSize = 22.sp, fontWeight = FontWeight.Bold, lineHeight = 25.sp)
          Text(g.subtitle, color = CardMuted, fontSize = Small, lineHeight = 18.sp)
        }
        Box(
          Modifier.size(44.dp).clip(CircleShape).clickable(onClick = onClose).semantics { contentDescription = "Cerrar" },
          contentAlignment = Alignment.Center
        ) { Icon(Icons.Default.Close, contentDescription = null, tint = CardMuted, modifier = Modifier.size(26.dp)) }
      }

      // Tu avance
      HorizontalLine()
      if (data.progress != null) {
        Row(verticalAlignment = Alignment.CenterVertically) {
          Text("${Skill.percent(data.progress)}%", color = Clay.Ink, fontSize = 34.sp, fontWeight = FontWeight.Bold)
          Spacer(Modifier.width(14.dp))
          Column {
            Text(Skill.stageName(data.progress), color = onCream(g.domain), fontSize = 19.sp, fontWeight = FontWeight.Bold)
            val next = Skill.toNextStage(data.progress)
            Text(
              if (next != null) "a ${next.first} ${if (next.first == 1) "punto" else "puntos"} de ${next.second}" else "La etapa más alta",
              color = CardMuted, fontSize = Small
            )
          }
        }
        StageLine(data.progress, g.domain)
      } else {
        Text("Sin medir aún", color = Clay.Ink, fontSize = 20.sp, fontWeight = FontWeight.Bold)
        Text("Juega a tu medida una vez y verás dónde estás.", color = CardMuted, fontSize = Small)
      }

      // Tu marca y constancia, en pocas palabras
      if (data.measureTitle != null && data.measureValue != null) {
        HorizontalLine()
        Row(verticalAlignment = Alignment.CenterVertically) {
          Column(Modifier.weight(1f)) {
            Text(data.measureTitle, color = CardMuted, fontSize = Small)
            Text(data.measureValue, color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold)
            if (data.measureTag != null) Tag(data.measureTag, Clay.Lime, Modifier.padding(top = 4.dp))
          }
          if (data.measureSeries.size >= 2) {
            Column(horizontalAlignment = Alignment.End) {
              CreamSparkline(data.measureSeries, data.lowerIsBetter, g.domain.color, Modifier.size(width = 112.dp, height = 44.dp))
              Text("mejor hacia arriba", color = CardMuted, fontSize = Small)
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
        color = CardMuted, fontSize = Small, modifier = Modifier.padding(top = 10.dp)
      )

      // Cómo quieres jugar: 4 filas cortas; la explicación solo del elegido
      HorizontalLine()
      Text("¿Cómo quieres jugar?", color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(bottom = 8.dp))
      PlayMode.entries.forEach { m ->
        val open = Skill.isOpen(m, g.id, if (expertOpen) setOf(g.id) else emptySet())
        val on = m == choice
        val shape = RoundedCornerShape(16.dp)
        Row(
          verticalAlignment = Alignment.CenterVertically,
          modifier = Modifier
            .fillMaxWidth()
            .padding(bottom = 8.dp)
            .heightIn(min = 52.dp)
            .clip(shape)
            .background(if (on) ModeRowOn else ModeRow)
            .border(if (on) 3.dp else 1.5.dp, Clay.Ink, shape)
            .clickable(enabled = open) { onChoose(m) }
            .padding(horizontal = 12.dp, vertical = 8.dp)
            .testTag("mode_${m.name}")
        ) {
          ModeBars(m.ordinal + 1, if (on) Clay.Sun else g.domain.color)
          Spacer(Modifier.width(12.dp))
          Text(m.label, color = if (open) Clay.Ink else CardMuted, fontSize = 17.sp, fontWeight = FontWeight.Bold, modifier = Modifier.weight(1f))
          if (!open) {
            Text("Supera un Desafío", color = CardMuted, fontSize = Small)
            Spacer(Modifier.width(6.dp))
            Icon(Icons.Default.Lock, contentDescription = "Bloqueado", tint = CardMuted, modifier = Modifier.size(20.dp))
          } else {
            Text("${Skill.hitsText(Skill.expectedHits(g.id, m, age))} aciertos", color = Clay.Ink, fontSize = 15.sp, fontWeight = FontWeight.Medium)
            if (on) {
              Spacer(Modifier.width(8.dp))
              Box(
                Modifier.size(24.dp).clip(CircleShape).background(Clay.Lime).border(2.dp, Clay.Ink, CircleShape),
                contentAlignment = Alignment.Center
              ) { Icon(Icons.Default.Check, contentDescription = "Elegido", tint = Clay.Ink, modifier = Modifier.size(16.dp)) }
            }
          }
        }
      }
      Text(choice.what, color = CardMuted, fontSize = Small, lineHeight = 19.sp)

      // ¿Con o sin reloj? Se recuerda por juego; el camino diario de Hoy va siempre sin reloj.
      if (com.example.data.RetoChoice.supports(g.id)) {
        HorizontalLine()
        Text("¿Con reloj?", color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(bottom = 8.dp))
        listOf(false to com.example.data.RetoChoice.UNTIMED_LABEL, true to com.example.data.RetoChoice.timedLabel(g.id)).forEach { (value, label) ->
          val on = timed == value
          val shape = RoundedCornerShape(16.dp)
          Row(
            verticalAlignment = Alignment.CenterVertically,
            modifier = Modifier
              .fillMaxWidth()
              .padding(bottom = 8.dp)
              .heightIn(min = 52.dp)
              .clip(shape)
              .background(if (on) ModeRowOn else ModeRow)
              .border(if (on) 3.dp else 1.5.dp, Clay.Ink, shape)
              .clickable { onChooseTimed(value) }
              .semantics { contentDescription = label + if (on) ", elegido" else "" }
              .padding(horizontal = 12.dp, vertical = 8.dp)
              .testTag(if (value) "reto_con" else "reto_sin")
          ) {
            Icon(if (value) Icons.Filled.Timer else Icons.Filled.TimerOff, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(26.dp))
            Spacer(Modifier.width(12.dp))
            Text(label, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, modifier = Modifier.weight(1f))
            if (on) {
              Box(
                Modifier.size(24.dp).clip(CircleShape).background(Clay.Lime).border(2.dp, Clay.Ink, CircleShape),
                contentAlignment = Alignment.Center
              ) { Icon(Icons.Default.Check, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(16.dp)) }
            }
          }
        }
        Text(com.example.data.RetoChoice.help(timed), color = CardMuted, fontSize = Small, lineHeight = 19.sp)
      }

    }
    // «Jugar»: FUERA del scroll, fijo abajo en la tarjeta, siempre a la vista.
    ClayCard(color = Clay.Sun, radius = 22.dp, depth = 4.dp, contentPadding = 0.dp, onClick = onPlay, modifier = Modifier.fillMaxWidth().padding(start = 20.dp, end = 20.dp, top = 10.dp, bottom = 12.dp).testTag("sheet_play")) {
      Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.Center, modifier = Modifier.fillMaxWidth().height(54.dp)) {
        Icon(Icons.Default.PlayArrow, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(24.dp))
        Spacer(Modifier.width(6.dp))
        Text(if (choice == PlayMode.A_TU_MEDIDA) "Jugar a tu medida" else "Jugar en ${choice.label}", color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold)
      }
    }
  }
}

/** La marca con su unidad: "84 ms", "a 18% de casa", "96° por segundo". */
internal fun measureText(def: com.example.data.MeasureDef, v: Float): String =
  (if (def.compactPattern.startsWith("a ")) "a " else "") + "${def.format(v)} ${def.unit}".trim()

/** El área que Nubi sugiere hoy: su clave, su nombre visible y el motivo ("Hace 4 días que no lo juegas"). */
internal data class AreaSuggestionUi(val key: String, val name: String, val reason: String)

/** Arma la sugerencia desde el historial ([plays] = juego + momento) y el avance de cada área. */
internal fun suggestArea(plays: List<Pair<String, Long>>, statuses: List<AreaStatus>, now: Long, lang: com.example.model.AppLanguage): AreaSuggestionUi {
  val names = AREA_ORDER.associateWith { key -> getDomainName(DomainType.fromStored(key) ?: DomainType.MEMORIA, lang) }
  val lastByKey = AREA_ORDER.associateWith { key ->
    plays.filter { (gameId, _) -> GameRegistry.getById(gameId)?.domain?.name == key }.maxOfOrNull { it.second }
  }
  val r = AreaSuggestion.pick(
    areas = AREA_ORDER.map { names.getValue(it) },
    lastPlayedByArea = AREA_ORDER.associate { names.getValue(it) to lastByKey[it] },
    valueByArea = AREA_ORDER.associate { key -> names.getValue(key) to statuses.firstOrNull { it.area == key }?.value },
    now = now,
    dayOf = ::dayIndex
  )
  val key = names.entries.first { it.value == r.area }.key
  return AreaSuggestionUi(key, r.area, r.reason)
}

/**
 * Encabezado "Nubi te sugiere" (30-sep, maqueta `docs/previews/cuatro-areas.png` Juegos · 2): Nubi científica a la
 * izquierda, la pregunta y, debajo, qué área sugiere y por qué.
 */
@Composable
internal fun SuggestionHeader(s: AreaSuggestionUi) {
  Row(
    verticalAlignment = Alignment.CenterVertically,
    modifier = Modifier.fillMaxWidth().padding(start = 8.dp, end = 16.dp).testTag("suggestion_header")
  ) {
    Image(
      painterResource(com.example.R.drawable.nubi_cientifica), contentDescription = "Nubi con bata de científica",
      modifier = Modifier.size(104.dp)
    )
    Column(Modifier.weight(1f)) {
      Text("¿Qué entrenamos hoy?", color = Color.White, fontSize = 24.sp, fontWeight = FontWeight.Bold, lineHeight = 28.sp)
      Text(
        "Te sugiero ${s.name}: ${s.reason.replaceFirstChar { it.lowercase() }}",
        color = OnNightDim, fontSize = 15.sp, lineHeight = 20.sp, modifier = Modifier.padding(top = 2.dp).testTag("suggestion_text")
      )
    }
  }
}

/**
 * Las 4 áreas a la vez, en 2 x 2 que llena la pantalla hasta la barra de pestañas (aprobado por Ricardo,
 * `docs/previews/cuatro-areas.png` Juegos · 2): el planeta del área (el sugerido, más grande, con un aro de luz y el
 * rótulo "Sugerida"; sello sol con ✓ = jugada hoy), el nombre, su subtítulo, la barra de avance de Hoy y "Hábil · 7
 * juegos". Sin flechas ni deslizar: el único gesto hacia los costados de esta pantalla es cambiar de pestaña. Si la letra
 * del sistema es muy grande, la rejilla se desplaza.
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
    val rowHeight = maxOf(maxHeight / 2, 262.dp)
    Column(Modifier.fillMaxWidth().verticalScroll(rememberScrollState()).padding(horizontal = 12.dp)) {
      statuses.chunked(2).forEach { row ->
        Row(Modifier.fillMaxWidth().height(rowHeight)) {
          row.forEach { s ->
            val domain = DomainType.fromStored(s.area) ?: DomainType.MEMORIA
            val name = getDomainName(domain, lang)
            val count = GameRegistry.allGames.count { it.domain == domain }
            val isSuggested = s.area == suggested
            val planet = if (isSuggested) 140.dp else 104.dp
            // La esfera mide AREA_BODY_FRACTION de la imagen: el aro se dibuja alrededor de la esfera, no de la imagen.
            val ringRadius = planet * AREA_BODY_FRACTION / 2 + 12.dp
            Column(
              horizontalAlignment = Alignment.CenterHorizontally,
              verticalArrangement = Arrangement.Center,
              modifier = Modifier
                .weight(1f)
                .fillMaxHeight()
                .clip(RoundedCornerShape(24.dp))
                .clickable { onArea(s.area) }
                .semantics(mergeDescendants = true) {
                  contentDescription = areaDescription(name, s) + (if (isSuggested) ". Sugerida por Nubi" else "") + ". Abrir sus juegos"
                }
                .testTag("area_${s.area.lowercase()}")
            ) {
              Box(
                Modifier.size(150.dp).drawBehind {
                  if (isSuggested) {
                    val ring = ringRadius.toPx()
                    drawCircle(Brush.radialGradient(listOf(Clay.Sun.copy(alpha = 0.14f), Color.Transparent), center, ring * 1.4f), ring * 1.4f, center)
                    drawCircle(Clay.Sun, ring, center, style = Stroke(3.dp.toPx()))
                  } else {
                    // resplandor suave del color del área detrás del planeta
                    drawCircle(Brush.radialGradient(listOf(domain.color.copy(alpha = 0.38f), Color.Transparent), center, size.minDimension / 2f))
                  }
                },
                contentAlignment = Alignment.Center
              ) {
                Image(painterResource(areaPlanetRes(s.area)), contentDescription = null, modifier = Modifier.size(planet))
                if (s.area in playedToday) {
                  TodaySeal(Modifier.align(Alignment.Center).offset(x = planet * 0.22f, y = -planet * 0.22f))
                }
                if (isSuggested) SuggestedTag(Modifier.align(Alignment.Center).offset(y = -ringRadius))
              }
              Text(name, color = Color.White, fontSize = 22.sp, fontWeight = FontWeight.Bold, maxLines = 1)
              Text(domain.tagline, color = OnNightDim, fontSize = 15.sp, maxLines = 1)
              AreaBar(s.value, s.change, modifier = Modifier.width(140.dp).padding(vertical = 8.dp), height = 9.dp)
              Text(
                (s.value?.let { Skill.stageName(it) } ?: "Sin medir") + " · " + if (count == 1) "1 juego" else "$count juegos",
                color = OnNightDim, fontSize = 15.sp, maxLines = 1
              )
            }
          }
        }
      }
    }
  }
}

/** Rótulo "Sugerida" (píldora sol con letra tinta): un rótulo, no una tarjeta. */
@Composable
private fun SuggestedTag(modifier: Modifier = Modifier) {
  Box(
    modifier
      .height(26.dp)
      .clip(RoundedCornerShape(13.dp))
      .background(Clay.Sun)
      .padding(horizontal = 12.dp),
    contentAlignment = Alignment.Center
  ) { Text("Sugerida", color = Clay.Ink, fontSize = 14.sp, fontWeight = FontWeight.Bold) }
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
  ) { Icon(Icons.Default.Check, contentDescription = "Jugada hoy", tint = Clay.Ink, modifier = Modifier.size(16.dp)) }
}

/**
 * La ventana de un área (aprobada por Ricardo con Nubi científica, `docs/previews/juegos-nubi.png`): tapa toda la
 * pantalla (la barra de pestañas se esconde y el dedo no cambia de pestaña), X arriba a la derecha, Nubi con bata y
 * lentes pregunta con cuál entrenar y debajo van los juegos del área. [focusGameId] = la última casilla abierta.
 */
@Composable
internal fun AreaWindow(
  domain: DomainType,
  title: String,
  status: AreaStatus?,
  cards: List<GameCardData>,
  focusGameId: String?,
  onClose: () -> Unit,
  onGame: (GameDefinition) -> Unit,
  modifier: Modifier = Modifier
) {
  Column(modifier.fillMaxSize().testTag("area_window")) {
    Row(
      verticalAlignment = Alignment.CenterVertically,
      modifier = Modifier.fillMaxWidth().padding(start = 8.dp, end = 16.dp, top = 6.dp)
    ) {
      Image(painterResource(areaPlanetRes(domain.name)), contentDescription = null, modifier = Modifier.size(64.dp))
      Column(Modifier.weight(1f)) {
        Text(title, color = Color.White, fontSize = 26.sp, fontWeight = FontWeight.Bold, lineHeight = 28.sp)
        Text(
          (status?.value?.let { Skill.stageName(it) } ?: "Sin medir") + " · " + if (cards.size == 1) "1 juego" else "${cards.size} juegos",
          color = OnNightDim, fontSize = 15.sp
        )
      }
      Box(
        Modifier
          .size(44.dp)
          .drawBehind { drawCircle(Clay.Ink, size.minDimension / 2f, center.copy(y = center.y + 3.dp.toPx())) }
          .clip(CircleShape)
          .background(Clay.Cream)
          .border(2.5.dp, Clay.Ink, CircleShape)
          .clickable(onClick = onClose)
          .semantics { contentDescription = "Cerrar $title" }
          .testTag("btn_area_close"),
        contentAlignment = Alignment.Center
      ) { Icon(Icons.Default.Close, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(26.dp)) }
    }
    val start = cards.indexOfFirst { it.game.id == focusGameId }.let { if (it < 0) 0 else it + 1 }
    val list = rememberLazyListState(initialFirstVisibleItemIndex = start)
    LazyColumn(
      state = list,
      contentPadding = PaddingValues(start = 16.dp, end = 16.dp, top = 4.dp, bottom = 24.dp),
      verticalArrangement = Arrangement.spacedBy(12.dp),
      modifier = Modifier.weight(1f).testTag("game_list")
    ) {
      item(key = "nubi") {
        Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.fillMaxWidth().padding(bottom = 4.dp)) {
          Image(
            painterResource(com.example.R.drawable.nubi_cientifica), contentDescription = "Nubi con bata de científica",
            modifier = Modifier.size(150.dp)
          )
          NubiBubble(
            title = "Nubi",
            text = "¿Con cuál entrenamos ${AreaProgress.your(title)}?",
            tailLeft = true,
            modifier = Modifier.weight(1f)
          )
        }
      }
      itemsIndexed(cards, key = { _, c -> c.game.id }) { _, data ->
        GameTile(data, highlighted = data.game.id == focusGameId) { onGame(data.game) }
      }
    }
  }
}

/** Las 5 etapas en una línea; la actual con su nombre fuerte y una marquita de "estás aquí". */
@Composable
private fun StageLine(progress: Float, domain: DomainType) {
  val current = Skill.stage(progress)
  Column(Modifier.fillMaxWidth().padding(top = 8.dp)) {
    Canvas(Modifier.fillMaxWidth().height(26.dp).padding(horizontal = 16.dp)) {
      val y = size.height * 0.62f
      val w = size.width
      drawLine(CardTrack, Offset(0f, y), Offset(w, y), 4.dp.toPx(), StrokeCap.Round)
      drawLine(domain.color, Offset(0f, y), Offset(w * progress.coerceIn(0f, 1f), y), 4.dp.toPx(), StrokeCap.Round)
      for (i in 0..4) {
        val x = w * i / 4f
        val r = (if (i == current) 7 else 5).dp.toPx()
        drawCircle(if (i <= current) domain.color else CardTrack, r, Offset(x, y))
        drawCircle(Clay.Ink, r, Offset(x, y), style = Stroke(1.5.dp.toPx()))
      }
      val xm = w * progress.coerceIn(0f, 1f)
      drawPath(Path().apply { moveTo(xm - 5.dp.toPx(), 0f); lineTo(xm + 5.dp.toPx(), 0f); lineTo(xm, 6.dp.toPx()); close() }, Clay.Ink)
    }
    Row(Modifier.fillMaxWidth()) {
      Skill.STAGES.forEachIndexed { i, s ->
        Text(
          s, textAlign = TextAlign.Center, modifier = Modifier.weight(1f),
          color = if (i == current) onCream(domain) else CardMuted,
          fontSize = 14.sp, fontWeight = if (i == current) FontWeight.Bold else FontWeight.Normal
        )
      }
    }
  }
}

@Composable
private fun HorizontalLine() {
  Box(Modifier.fillMaxWidth().padding(vertical = 10.dp).height(1.dp).background(CardTrack))
}

@Composable
private fun Tag(text: String, color: Color, modifier: Modifier = Modifier) {
  Text(
    text, color = Clay.Ink, fontSize = 14.sp, fontWeight = FontWeight.Bold,
    modifier = modifier.clip(RoundedCornerShape(10.dp)).background(color).border(1.5.dp, Clay.Ink, RoundedCornerShape(10.dp))
      .padding(horizontal = 8.dp, vertical = 2.dp)
  )
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

