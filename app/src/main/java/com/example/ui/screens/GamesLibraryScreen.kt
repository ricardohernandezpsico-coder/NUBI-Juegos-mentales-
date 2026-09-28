package com.example.ui.screens

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectHorizontalDragGestures
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.pager.HorizontalPager
import androidx.compose.foundation.pager.rememberPagerState
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.PlayArrow
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
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.data.DiscoveryKind
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
private val CardCream = Color(0xFFFFF8EC)
private val CardMuted = Color(0xFF5A5482) // sobre crema: 6:1
private val CardTrack = Color(0xFFE2DAC8)
private val ModeRow = Color(0xFFF6EEDE)
private val ModeRowOn = Color(0xFFFFF0C8)

private fun dayIndex(ts: Long): Long = (ts + java.util.TimeZone.getDefault().getOffset(ts)) / DAY_MS

/** Color del área oscurecido para texto sobre crema (contraste ≥ 4,5). */
private fun onCream(domain: DomainType): Color = lerp(domain.color, Clay.Ink, 0.45f)

/** Todo lo que muestra la carta de un juego (armado desde el ViewModel; separado para la foto de prueba). */
data class GameCardData(
  val game: GameDefinition,
  val title: String,
  val position: String,
  val lastPlayed: String,
  /** Tu avance 0..1 (null = sin medir aún). */
  val progress: Float?,
  /** Tu marca de juego estrella: nombre ("Tu brújula"), valor ("a 18% de casa"), etiqueta y últimas partidas. */
  val measureTitle: String? = null,
  val measureValue: String? = null,
  val measureTag: String? = null,
  val measureSeries: List<Float> = emptyList(),
  val lowerIsBetter: Boolean = false,
  /** Constancia 0..5: partidas de este juego en los últimos 14 días (1 punto cada 2). */
  val constancy: Int = 0
)

/**
 * Pestaña Juegos (28-sep, Ricardo eligió "un área a la vez" + cartas: `docs/previews/juegos-dificultad-1.png`):
 * "¿Qué quieres trabajar hoy?" con el área en grande (flechas o deslizar sobre el nombre) y sus juegos como cartas
 * que se deslizan. Cada carta dice tu avance (leído a 8 de 10, con su etapa), tu marca y tu constancia; abajo el modo
 * ("A tu medida · cambiar", abre [ModeSheet]) y "Jugar". Razonamiento en `docs/dificultad-y-avance.md`.
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
  val skill by viewModel.skill.collectAsState()
  val lang = LocalAppLanguage.current
  val now = remember(history) { System.currentTimeMillis() }
  val age = userSettings.ageBand

  val lastPlayed = remember(history) { history.groupBy { it.gameId }.mapValues { (_, l) -> l.maxOf { it.timestamp } } }
  // Se abre en el área (y el juego) que sugiere "Para ti" (zona quieta, juego sin probar, olvidado).
  val suggestion = remember(history.isEmpty()) {
    val plays = history.mapNotNull { r -> GameRegistry.getById(r.gameId)?.let { com.example.data.PlanetPlay(it.domain.name, r.timestamp) } }
    val planet = com.example.data.Planet.build(plays, now, ::dayIndex)
    val games = GameRegistry.allGames.filter { it.id != "bitacora" }.map { com.example.data.LibraryGame(it.id, it.domain.name, StarMeasures.defForGame(it.id) != null) }
    com.example.data.Library.picks(planet, games, lastPlayed, now, ::dayIndex) { it }.firstOrNull()?.gameId
  }
  val domains = DomainType.values().toList()
  var domainIndex by rememberSaveable {
    mutableStateOf(domains.indexOf(suggestion?.let { GameRegistry.getById(it)?.domain } ?: DomainType.MEMORIA).coerceAtLeast(0))
  }
  val domain = domains[domainIndex]
  val games = remember(domain) {
    GameRegistry.allGames.filter { it.domain == domain }.sortedBy { StarMeasures.defForGame(it.id) == null }
  }
  val modes = remember { mutableStateMapOf<String, PlayMode>() }
  var sheetFor by remember { mutableStateOf<GameDefinition?>(null) }

  Column(modifier = modifier.fillMaxSize()) {
    val (areaProgress, explored) = Skill.area(games.map { it.id }, progress)
    AreaHeader(
      domain = domain,
      title = getDomainName(domain, lang),
      progress = areaProgress,
      explored = explored,
      total = games.size,
      onPrev = { domainIndex = (domainIndex + domains.size - 1) % domains.size },
      onNext = { domainIndex = (domainIndex + 1) % domains.size }
    )
    key(domain) {
      val start = games.indexOfFirst { it.id == suggestion }.coerceAtLeast(0)
      val pager = rememberPagerState(initialPage = start) { games.size }
      HorizontalPager(
        state = pager,
        contentPadding = PaddingValues(horizontal = 30.dp),
        pageSpacing = 12.dp,
        modifier = Modifier.weight(1f).testTag("game_cards")
      ) { page ->
        val g = games[page]
        val data = cardData(g, getGameTitle(g.id, lang, g.title), "${page + 1} de ${games.size}", lastPlayed[g.id], now,
          progress[g.id], measures, history.count { it.gameId == g.id && now - it.timestamp <= 14 * DAY_MS })
        GameCard(
          data = data,
          mode = modes[g.id] ?: PlayMode.A_TU_MEDIDA,
          onMode = { sheetFor = g },
          onPlay = { viewModel.launchGame(g.id, mode = modes[g.id] ?: PlayMode.A_TU_MEDIDA) }
        )
      }
      DeckDots(count = games.size, current = pager.currentPage)
    }
  }

  sheetFor?.let { g ->
    ModeSheet(
      game = g,
      title = getGameTitle(g.id, lang, g.title),
      progress = progress[g.id],
      age = age,
      expertOpen = g.id in skill.expertOpen,
      selected = modes[g.id] ?: PlayMode.A_TU_MEDIDA,
      onDismiss = { sheetFor = null },
      onPlay = { m ->
        modes[g.id] = m
        sheetFor = null
        viewModel.launchGame(g.id, mode = m)
      }
    )
  }
}

/** Arma la carta de un juego: cuándo se jugó, avance, marca con su etiqueta y constancia. */
internal fun cardData(
  g: GameDefinition,
  title: String,
  position: String,
  last: Long?,
  now: Long,
  progress: Float?,
  measures: List<com.example.data.MeasurePoint>,
  playsLast14: Int
): GameCardData {
  val lastText = when {
    last == null -> "Aún no lo juegas"
    else -> when (val d = (dayIndex(now) - dayIndex(last)).toInt()) {
      0 -> "Jugaste hoy"
      1 -> "Jugaste ayer"
      else -> "Jugaste hace $d días"
    }
  }
  val def = StarMeasures.defForGame(g.id)
  // Solo partidas comparables con la última (mismo reloj; nivel parecido si la marca depende del nivel).
  val series = def?.let { d -> StarMeasures.comparable(measures.filter { it.key == d.key }) }.orEmpty()
  val kind = if (def != null && series.size >= StarMeasures.MIN_POINTS) StarMeasures.discover(series, now)?.kind else null
  return GameCardData(
    game = g,
    title = title,
    position = position,
    lastPlayed = lastText,
    progress = progress,
    measureTitle = def?.short?.replaceFirstChar { it.uppercase() },
    measureValue = series.lastOrNull()?.let { p -> def?.let { measureText(it, p.value) } },
    measureTag = when (kind) {
      DiscoveryKind.RECORD -> "Tu récord"
      DiscoveryKind.IMPROVING -> "Mejorando"
      DiscoveryKind.STEADY -> "Se mantiene"
      null -> null
    },
    measureSeries = series.takeLast(StarMeasures.MAX_POINTS).map { it.value },
    lowerIsBetter = def?.lowerIsBetter ?: false,
    constancy = ((playsLast14 + 1) / 2).coerceIn(0, 5)
  )
}

/** La marca con su unidad: "84 ms", "a 18% de casa", "96° por segundo". */
internal fun measureText(def: com.example.data.MeasureDef, v: Float): String =
  (if (def.compactPattern.startsWith("a ")) "a " else "") + "${def.format(v)} ${def.unit}".trim()

/** "¿Qué quieres trabajar hoy?": el área en grande con su planeta y avance; flechas o deslizar para cambiarla. */
@Composable
internal fun AreaHeader(
  domain: DomainType,
  title: String,
  progress: Float?,
  explored: Int,
  total: Int,
  onPrev: () -> Unit,
  onNext: () -> Unit
) {
  Column(modifier = Modifier.fillMaxWidth().padding(top = 12.dp)) {
    Text(
      "¿Qué quieres trabajar hoy?",
      color = OnNight, fontSize = 24.sp, fontWeight = FontWeight.Bold,
      modifier = Modifier.padding(horizontal = 20.dp)
    )
    var drag by remember { mutableStateOf(0f) }
    Row(
      verticalAlignment = Alignment.CenterVertically,
      modifier = Modifier
        .fillMaxWidth()
        .padding(horizontal = 12.dp, vertical = 10.dp)
        .pointerInput(domain) {
          detectHorizontalDragGestures(
            onDragStart = { drag = 0f },
            onDragEnd = { if (drag < -60f) onNext() else if (drag > 60f) onPrev(); drag = 0f },
            onHorizontalDrag = { _, d -> drag += d }
          )
        }
        .testTag("area_header")
    ) {
      ArrowButton(left = true, onClick = onPrev)
      Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.weight(1f).padding(horizontal = 10.dp)) {
        Box(
          modifier = Modifier.size(56.dp).drawBehind {
            drawCircle(Brush.radialGradient(listOf(domain.color.copy(alpha = 0.5f), Color.Transparent)), size.minDimension / 2f)
            val stroke = 3.dp.toPx()
            drawCircle(Color.White.copy(alpha = 0.18f), size.minDimension / 2f - stroke, style = Stroke(stroke))
            if (progress != null) drawArc(
              com.example.ui.components.domainTextColor(domain.name), -90f, 360f * progress.coerceIn(0.03f, 1f), false,
              Offset(stroke, stroke), androidx.compose.ui.geometry.Size(size.width - 2 * stroke, size.height - 2 * stroke),
              style = Stroke(stroke, cap = StrokeCap.Round)
            )
          },
          contentAlignment = Alignment.Center
        ) {
          Box(Modifier.size(38.dp).clip(CircleShape).background(domain.color).border(3.dp, Clay.Ink, CircleShape))
        }
        Spacer(Modifier.width(12.dp))
        Column {
          Text(title, color = Color.White, fontSize = 26.sp, fontWeight = FontWeight.Bold, lineHeight = 28.sp)
          if (progress != null) Text("${Skill.percent(progress)}% de avance", color = OnNightDim, fontSize = 13.sp, fontWeight = FontWeight.Bold)
          Text(
            if (explored == total) (if (total == 1) "1 juego" else "$total juegos") else "$explored de $total juegos explorados",
            color = OnNightDim, fontSize = 13.sp
          )
        }
      }
      ArrowButton(left = false, onClick = onNext)
    }
    // En cuál de las 6 áreas estás: rayitas de colores.
    Row(horizontalArrangement = Arrangement.Center, modifier = Modifier.fillMaxWidth().padding(bottom = 10.dp)) {
      DomainType.values().forEach { d ->
        Box(
          Modifier
            .padding(horizontal = 3.dp)
            .size(width = if (d == domain) 26.dp else 16.dp, height = 5.dp)
            .clip(RoundedCornerShape(3.dp))
            .background(if (d == domain) d.color else d.color.copy(alpha = 0.35f))
        )
      }
    }
  }
}

@Composable
private fun ArrowButton(left: Boolean, onClick: () -> Unit) {
  Box(
    modifier = Modifier
      .size(44.dp)
      .drawBehind { drawCircle(Clay.Ink, size.minDimension / 2f, center.copy(y = center.y + 3.dp.toPx())) }
      .clip(CircleShape)
      .background(Color(0xFF28306E))
      .border(2.dp, Clay.Ink, CircleShape)
      .clickable(onClick = onClick)
      .semantics { contentDescription = if (left) "Área anterior" else "Área siguiente" },
    contentAlignment = Alignment.Center
  ) {
    Icon(
      if (left) Icons.AutoMirrored.Filled.KeyboardArrowLeft else Icons.AutoMirrored.Filled.KeyboardArrowRight,
      contentDescription = null, tint = Color.White, modifier = Modifier.size(28.dp)
    )
  }
}

/** La carta de un juego (arcilla crema): cuándo jugaste, el juego, tu avance, tu marca, tu constancia, modo y Jugar. */
@Composable
internal fun GameCard(data: GameCardData, mode: PlayMode, onMode: () -> Unit, onPlay: () -> Unit) {
  val g = data.game
  ClayCard(color = CardCream, radius = 28.dp, contentPadding = 0.dp, modifier = Modifier.fillMaxWidth().testTag("card_${g.id}")) {
    Column(Modifier.fillMaxWidth().verticalScroll(rememberScrollState()).padding(horizontal = 22.dp, vertical = 18.dp)) {
      Row(Modifier.fillMaxWidth()) {
        Text(data.lastPlayed, color = CardMuted, fontSize = 12.sp, modifier = Modifier.weight(1f))
        Text(data.position, color = CardMuted, fontSize = 12.sp, fontWeight = FontWeight.Bold)
      }
      Box(
        Modifier.fillMaxWidth().padding(top = 6.dp).height(100.dp).drawBehind {
          drawCircle(Brush.radialGradient(listOf(g.domain.color.copy(alpha = 0.35f), Color.Transparent), center, 60.dp.toPx()), 60.dp.toPx())
        },
        contentAlignment = Alignment.Center
      ) { com.example.ui.components.MiniPlanet(g.id, 84.dp) }
      Text(data.title, color = Clay.Ink, fontSize = 23.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center, modifier = Modifier.fillMaxWidth())
      Text(g.subtitle, color = CardMuted, fontSize = 13.sp, textAlign = TextAlign.Center, modifier = Modifier.fillMaxWidth().padding(top = 2.dp))

      // Tu avance
      Text("Tu avance", color = CardMuted, fontSize = 12.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 16.dp))
      if (data.progress != null) {
        Row(verticalAlignment = Alignment.CenterVertically) {
          Text("${Skill.percent(data.progress)}%", color = Clay.Ink, fontSize = 32.sp, fontWeight = FontWeight.Bold)
          Spacer(Modifier.width(14.dp))
          Column {
            Text(Skill.stageName(data.progress), color = onCream(g.domain), fontSize = 16.sp, fontWeight = FontWeight.Bold)
            val next = Skill.toNextStage(data.progress)
            Text(
              if (next != null) "a ${next.first} ${if (next.first == 1) "punto" else "puntos"} de ${next.second}" else "La etapa más alta",
              color = CardMuted, fontSize = 12.sp
            )
          }
        }
        StageLine(data.progress, g.domain)
      } else {
        Text("Sin medir aún", color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 2.dp))
        Text("Juega a tu medida una vez y verás dónde estás.", color = CardMuted, fontSize = 12.sp)
      }

      // Tu marca (juegos estrella)
      if (data.measureTitle != null) {
        HorizontalLine()
        Row(verticalAlignment = Alignment.CenterVertically) {
          Column(Modifier.weight(1f)) {
            Text(data.measureTitle, color = CardMuted, fontSize = 12.sp, fontWeight = FontWeight.Bold)
            Text(data.measureValue ?: "Juega para descubrirla", color = Clay.Ink, fontSize = if (data.measureValue != null) 17.sp else 13.sp, fontWeight = FontWeight.Bold)
            if (data.measureTag != null) Tag(data.measureTag, Clay.Lime, Modifier.padding(top = 4.dp))
          }
          if (data.measureSeries.size >= 2) {
            Column(horizontalAlignment = Alignment.End) {
              CreamSparkline(data.measureSeries, data.lowerIsBetter, g.domain.color, Modifier.size(width = 110.dp, height = 40.dp))
              Text("mejor hacia arriba", color = CardMuted, fontSize = 10.sp)
            }
          }
        }
      }

      // Constancia
      HorizontalLine()
      Row(verticalAlignment = Alignment.CenterVertically) {
        Text("Tu constancia", color = Clay.Ink, fontSize = 13.sp, modifier = Modifier.weight(1f))
        Row(Modifier.semantics { contentDescription = "Constancia ${data.constancy} de 5" }) {
          repeat(5) { i ->
            Box(
              Modifier.padding(start = 4.dp).size(12.dp).clip(CircleShape)
                .background(if (i < data.constancy) g.domain.color else CardTrack)
                .border(if (i < data.constancy) 1.dp else 0.dp, Clay.Ink, CircleShape)
            )
          }
        }
      }
      Text("partidas de las últimas 2 semanas", color = CardMuted, fontSize = 11.sp)

      // Modo + Jugar
      Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.fillMaxWidth().padding(top = 16.dp)) {
        ClayCard(color = Color(0xFFF0E6D4), radius = 20.dp, depth = 4.dp, contentPadding = 0.dp, onClick = onMode, modifier = Modifier.weight(1f).testTag("mode_button")) {
          Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.height(48.dp).padding(horizontal = 12.dp)) {
            ModeBars(mode.ordinal + 1, g.domain.color)
            Spacer(Modifier.width(10.dp))
            Column {
              Text(mode.label, color = Clay.Ink, fontSize = 13.sp, fontWeight = FontWeight.Bold, lineHeight = 15.sp)
              Text("cambiar", color = CardMuted, fontSize = 11.sp, lineHeight = 13.sp)
            }
          }
        }
        Spacer(Modifier.width(10.dp))
        ClayCard(color = Clay.Sun, radius = 20.dp, depth = 4.dp, contentPadding = 0.dp, onClick = onPlay, modifier = Modifier.weight(1f).testTag("play_button")) {
          Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.Center, modifier = Modifier.fillMaxWidth().height(48.dp)) {
            Icon(Icons.Default.PlayArrow, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(22.dp))
            Spacer(Modifier.width(6.dp))
            Text("Jugar", color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold)
          }
        }
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
          fontSize = if (i == current) 11.sp else 10.sp, fontWeight = if (i == current) FontWeight.Bold else FontWeight.Normal
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
    text, color = Clay.Ink, fontSize = 11.sp, fontWeight = FontWeight.Bold,
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

/** En qué carta del área vas: rayitas (no círculos), la actual en sol. */
@Composable
private fun DeckDots(count: Int, current: Int) {
  Row(horizontalArrangement = Arrangement.Center, modifier = Modifier.fillMaxWidth().padding(top = 8.dp, bottom = 12.dp)) {
    repeat(count) { i ->
      Box(
        Modifier.padding(horizontal = 3.dp).size(width = 18.dp, height = 5.dp).clip(RoundedCornerShape(3.dp))
          .background(if (i == current) Clay.Sun else Color(0xFF464E8C))
      )
    }
  }
}

/**
 * "¿Cómo quieres jugar hoy?" (docs/dificultad-y-avance.md): los 4 modos con sus aciertos esperados según la edad;
 * Experto se abre al superar un Desafío en ese juego. El reloj no se elige aquí (queda en Ajustes).
 */
@Composable
internal fun ModeSheet(
  game: GameDefinition,
  title: String,
  progress: Float?,
  age: AgeBand?,
  expertOpen: Boolean,
  selected: PlayMode,
  onDismiss: () -> Unit,
  onPlay: (PlayMode) -> Unit
) {
  var choice by remember(game.id) { mutableStateOf(selected) }
  Dialog(onDismissRequest = onDismiss) {
    ModeSheetContent(game, title, progress, age, expertOpen, choice, onChoose = { choice = it }, onPlay = { onPlay(choice) })
  }
}

@Composable
internal fun ModeSheetContent(
  game: GameDefinition,
  title: String,
  progress: Float?,
  age: AgeBand?,
  expertOpen: Boolean,
  choice: PlayMode,
  onChoose: (PlayMode) -> Unit,
  onPlay: () -> Unit
) {
  ClayCard(color = CardCream, radius = 28.dp, contentPadding = 0.dp, modifier = Modifier.fillMaxWidth().testTag("mode_sheet")) {
    Column(Modifier.verticalScroll(rememberScrollState()).padding(20.dp)) {
      Row(verticalAlignment = Alignment.CenterVertically) {
        com.example.ui.components.MiniPlanet(game.id, 46.dp)
        Spacer(Modifier.width(12.dp))
        Column {
          Text(title, color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold)
          Text(
            if (progress != null) "Tu nivel: ${Skill.stageName(progress)} · ${Skill.percent(progress)}%" else "Tu nivel: sin medir aún",
            color = CardMuted, fontSize = 12.sp
          )
        }
      }
      Text("¿Cómo quieres jugar hoy?", color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 16.dp, bottom = 8.dp))
      Skill.modesFor(game.id).forEach { m ->
        val open = Skill.isOpen(m, game.id, if (expertOpen) setOf(game.id) else emptySet())
        val on = m == choice
        val shape = RoundedCornerShape(18.dp)
        Row(
          verticalAlignment = Alignment.CenterVertically,
          modifier = Modifier
            .fillMaxWidth()
            .padding(bottom = 8.dp)
            .drawBehind { drawRoundRect(Clay.Ink, Offset(0f, 3.dp.toPx()), size, CornerRadius(18.dp.toPx())) }
            .clip(shape)
            .background(if (on) ModeRowOn else ModeRow)
            .border(if (on) 3.dp else 1.5.dp, Clay.Ink, shape)
            .clickable(enabled = open) { onChoose(m) }
            .padding(horizontal = 12.dp, vertical = 10.dp)
            .testTag("mode_${m.name}")
        ) {
          ModeBars(m.ordinal + 1, if (on) Clay.Sun else game.domain.color)
          Spacer(Modifier.width(12.dp))
          Column(Modifier.weight(1f)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
              Text(m.label, color = if (open) Clay.Ink else CardMuted, fontSize = 16.sp, fontWeight = FontWeight.Bold)
              if (m == PlayMode.A_TU_MEDIDA) Tag("Recomendado", Clay.Lime, Modifier.padding(start = 8.dp))
            }
            Text(
              if (open) m.what else "Se abre cuando superas un Desafío en este juego.",
              color = CardMuted, fontSize = 12.sp, lineHeight = 15.sp
            )
          }
          Spacer(Modifier.width(8.dp))
          if (!open) {
            Icon(Icons.Default.Lock, contentDescription = "Bloqueado", tint = CardMuted, modifier = Modifier.size(22.dp))
          } else {
            Column(horizontalAlignment = Alignment.End) {
              Text(Skill.hitsText(Skill.expectedHits(game.id, m, age)), color = Clay.Ink, fontSize = 13.sp, fontWeight = FontWeight.Bold)
              Text("aciertos", color = CardMuted, fontSize = 11.sp)
              if (on) Box(
                Modifier.padding(top = 4.dp).size(22.dp).clip(CircleShape).background(Clay.Lime).border(2.dp, Clay.Ink, CircleShape),
                contentAlignment = Alignment.Center
              ) { Icon(Icons.Default.Check, contentDescription = "Elegido", tint = Clay.Ink, modifier = Modifier.size(16.dp)) }
            }
          }
        }
      }
      ClayCard(color = Clay.Sun, radius = 22.dp, depth = 4.dp, contentPadding = 0.dp, onClick = onPlay, modifier = Modifier.fillMaxWidth().padding(top = 8.dp).testTag("mode_play")) {
        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.Center, modifier = Modifier.fillMaxWidth().height(50.dp)) {
          Icon(Icons.Default.PlayArrow, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(22.dp))
          Spacer(Modifier.width(6.dp))
          Text(if (choice == PlayMode.A_TU_MEDIDA) "Jugar a tu medida" else "Jugar en ${choice.label}", color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold)
        }
      }
      Text(
        "Cada paso se ajusta a tu edad${age?.let { " (${it.label.lowercase()})" } ?: ""}. Suave, Desafío y Experto nunca bajan tu avance.",
        color = CardMuted, fontSize = 11.sp, lineHeight = 14.sp, modifier = Modifier.padding(top = 10.dp)
      )
    }
  }
}
