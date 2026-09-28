package com.example.ui.screens

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
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
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Whatshot
import androidx.compose.material3.Icon
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
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.data.Discovery
import com.example.data.DiscoveryKind
import com.example.data.DiscoveryNudge
import com.example.data.MeasurePoint
import com.example.data.Planet
import com.example.data.PlanetPlay
import com.example.data.PlanetState
import com.example.data.StarMeasures
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.CosmosScroll
import com.example.ui.components.HomePlanet
import com.example.ui.components.LeagueShield
import com.example.ui.components.MiniPlanet
import com.example.ui.components.PlanetShip
import com.example.ui.components.domainOf
import com.example.ui.components.domainTextColor
import com.example.ui.components.levelWord
import com.example.ui.components.overallIndex
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayCard
import com.example.viewmodel.NeuroVidaViewModel
import java.util.TimeZone

private const val DAY_MS = 86_400_000L
private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = Color(0xFFC7D0FF)
private val OnNightSoft = Color(0xFF9AA3D6)

private fun dayIndex(ts: Long): Long = (ts + TimeZone.getDefault().getOffset(ts)) / DAY_MS

/**
 * Inicio ("Hoy") como TU PLANETA (28-sep, elegido por Ricardo entre 6 propuestas; maqueta en
 * `docs/previews/inicio-planeta.png`): cada partida hace crecer la zona de su dominio (ver [Planet]), las 3 partidas
 * del día orbitan y aterrizan al jugarlas, y debajo va el DESCUBRIMIENTO DEL DÍA: una medida de un juego estrella con
 * su evolución (ver [StarMeasures]). Tocar una zona abre su ventana (juegos, medidas, partidas por semana).
 */
@Composable
fun HomeScreen(
  viewModel: NeuroVidaViewModel,
  onNavigateToGames: () -> Unit,
  modifier: Modifier = Modifier
) {
  val streak by viewModel.currentStreak.collectAsState()
  val dailySession by viewModel.dailySession.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val ranks by viewModel.gameRanks.collectAsState()
  val levels by viewModel.gameLevelsForProgress.collectAsState()
  val weeklyChallenges by viewModel.weeklyChallengeProgress.collectAsState()
  val pausedGameId by viewModel.pausedGameId.collectAsState()
  val baseline by viewModel.baseline.collectAsState()
  val mission by viewModel.mission.collectAsState()
  val measures by viewModel.starMeasures.collectAsState()
  // Reloj de la línea de la Bitácora ("el informe se abre en N min") y del planeta: se refresca cada 30 s.
  var clock by remember { mutableStateOf(System.currentTimeMillis()) }
  LaunchedEffect(Unit) {
    CosmosScroll.offset = 0f // el camino de antes movía las estrellas al deslizar; el planeta no
    while (true) {
      kotlinx.coroutines.delay(30_000L)
      clock = System.currentTimeMillis()
    }
  }
  val missionStep = remember(mission, dailySession, clock) { viewModel.missionStep(clock) }
  val lang = LocalAppLanguage.current

  var showChallenges by remember { mutableStateOf(false) }
  var openZone by remember { mutableStateOf<String?>(null) }

  val avg = if (ranks.isEmpty()) 0 else ranks.sumOf { it.rating } / ranks.size
  val tier = RankTier.fromRating(avg)
  val index = overallIndex(levels)

  val plays = remember(history) {
    history.mapNotNull { r -> GameRegistry.getById(r.gameId)?.let { PlanetPlay(it.domain.name, r.timestamp) } }
  }
  val planet = remember(plays, clock) { Planet.build(plays, clock, ::dayIndex) }
  val ships = remember(dailySession) {
    dailySession.gameIds.mapIndexedNotNull { i, id ->
      GameRegistry.getById(id)?.let { PlanetShip(id, it.domain.name, landed = i < dailySession.completedCount) }
    }
  }
  val discovery = remember(measures, clock) { StarMeasures.discover(measures, clock) }
  val nudge = remember(measures) { StarMeasures.nudge(measures) }

  Column(modifier = modifier.fillMaxSize()) {
    // Cabecera fija: liga, nivel y racha en una sola línea (sin recuadros)
    Row(
      modifier = Modifier
        .fillMaxWidth()
        .padding(start = 20.dp, end = 20.dp, top = 8.dp),
      verticalAlignment = Alignment.CenterVertically
    ) {
      LeagueShield(tier = tier, size = 26.dp)
      Spacer(Modifier.width(8.dp))
      Text(tier.tierName, color = OnNight, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
      Text("  ·  ", color = OnNightDim, fontSize = 15.sp)
      Text(index?.let { "$it nivel" } ?: "sin nivel", color = OnNight, fontSize = 15.sp)
      Text("  ·  ", color = OnNightDim, fontSize = 15.sp)
      Icon(Icons.Default.Whatshot, contentDescription = null, tint = Clay.Sun, modifier = Modifier.size(18.dp))
      Text("$streak", color = Clay.Sun, fontSize = 15.sp, fontWeight = FontWeight.Bold, modifier = Modifier.testTag("streak_pill"))
      Spacer(Modifier.weight(1f))
      Text(
        text = "Desafíos ${weeklyChallenges.count { it.isComplete }}/${weeklyChallenges.size}",
        color = Clay.Sun,
        fontSize = 15.sp,
        fontWeight = FontWeight.SemiBold,
        modifier = Modifier
          .clip(RoundedCornerShape(10.dp))
          .clickable { showChallenges = true }
          .padding(6.dp)
      )
    }
    Text(
      text = "Tu planeta",
      color = OnNight,
      fontSize = 28.sp,
      fontWeight = FontWeight.Bold,
      modifier = Modifier.padding(start = 20.dp, top = 2.dp)
    )

    HomePlanet(
      state = planet,
      ships = ships,
      onZone = { openZone = it },
      modifier = Modifier
        .weight(1f)
        .heightIn(min = 180.dp)
        .fillMaxWidth()
        .testTag("home_planet")
    )

    ZonesLine(planet, onZone = { openZone = it })
    DiscoveryBlock(discovery, nudge, onPlay = { viewModel.launchGame(it) })

    // Acción de hoy (siempre a mano y con un área de toque normal): seguir la partida en pausa o jugar el siguiente
    // juego del camino. Debajo, si todavía no hizo la evaluación, una invitación.
    TodayAction(
      pausedGameId = pausedGameId,
      nextGameId = dailySession.gameIds.getOrNull(dailySession.completedCount)?.takeIf { dailySession.completedCount < 3 },
      completed = dailySession.completedCount,
      hasBaseline = baseline != null,
      missionStep = missionStep,
      missionMinutesLeft = com.example.data.MissionLog.minutesLeft(mission, clock),
      missionFromOtherDay = mission.dateKey.isNotEmpty() && mission.dateKey != dailySession.dateKey,
      archivedTotal = mission.archivedTotal,
      onTransmit = { viewModel.startMissionTransmission() },
      onReport = { viewModel.startMissionReport() },
      lang = lang,
      onResume = { viewModel.resumePausedGame() },
      onPlay = { viewModel.startDailySession() },
      onBaseline = { viewModel.startBaseline() }
    )
  }

  openZone?.let { key ->
    ZoneDialog(
      domainKey = key,
      planet = planet,
      history = history,
      measures = measures,
      levels = levels,
      now = clock,
      lang = lang,
      onPlay = { id ->
        openZone = null
        viewModel.launchGame(id)
      },
      onDismiss = { openZone = null }
    )
  }

  if (showChallenges) {
    Dialog(onDismissRequest = { showChallenges = false }) {
      ClayCard(modifier = Modifier.fillMaxWidth().padding(8.dp), color = Clay.Grape, radius = 28.dp, contentPadding = 20.dp) {
        Text("Desafíos de la semana", color = Clay.Ink, fontSize = 22.sp, fontWeight = FontWeight.Bold)
        Spacer(Modifier.height(12.dp))
        weeklyChallenges.forEach { wc ->
          Column(modifier = Modifier.padding(vertical = 6.dp)) {
            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
              Text(wc.def.title, color = Clay.Ink, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
              Text(if (wc.isComplete) "Listo" else "${wc.progress}/${wc.def.target}", color = Clay.Ink, fontSize = 14.sp, fontWeight = FontWeight.Bold)
            }
            Box(
              modifier = Modifier
                .padding(top = 4.dp)
                .fillMaxWidth()
                .height(10.dp)
                .clip(RoundedCornerShape(5.dp))
                .background(Color.White.copy(alpha = 0.55f))
                .border(2.dp, Clay.Ink, RoundedCornerShape(5.dp))
            ) {
              Box(
                modifier = Modifier
                  .fillMaxWidth((wc.progress.toFloat() / wc.def.target).coerceIn(0.04f, 1f))
                  .height(10.dp)
                  .background(if (wc.isComplete) Clay.Lime else Clay.Sun)
              )
            }
          }
        }
      }
    }
  }
}

/**
 * Una línea bajo el planeta (texto suelto, sin recuadros): lo jugado hoy; o qué zona creció esta semana y cuál está
 * quieta (o aún sin explorar). Tocar un nombre abre su zona.
 */
@Composable
internal fun ZonesLine(planet: PlanetState, onZone: (String) -> Unit) {
  Row(
    modifier = Modifier.fillMaxWidth().padding(start = 20.dp, end = 20.dp, top = 4.dp, bottom = 6.dp),
    verticalAlignment = Alignment.Bottom
  ) {
    if (planet.playedToday.isNotEmpty()) {
      Column {
        Text("Hoy entrenaste", color = OnNightDim, fontSize = 14.sp)
        Text(
          planet.playedToday.mapNotNull { domainOf(it)?.displayName }.joinToString(" · "),
          color = Color.White, fontSize = 17.sp, fontWeight = FontWeight.Bold
        )
      }
      return@Row
    }
    val grew = planet.grewThisWeek
    val quiet = planet.quiet
    if (grew == null && quiet == null) {
      Text("Cada partida hace crecer su zona del planeta", color = OnNightDim, fontSize = 15.sp)
      return@Row
    }
    if (grew != null) {
      Column(Modifier.clip(RoundedCornerShape(10.dp)).clickable { onZone(grew) }.padding(2.dp)) {
        Text("Esta semana creció", color = OnNightDim, fontSize = 14.sp)
        Text(domainOf(grew)?.displayName ?: "", color = domainTextColor(grew), fontSize = 17.sp, fontWeight = FontWeight.Bold)
      }
    }
    Spacer(Modifier.weight(1f))
    if (quiet != null) {
      Column(
        horizontalAlignment = Alignment.End,
        modifier = Modifier.clip(RoundedCornerShape(10.dp)).clickable { onZone(quiet) }.padding(2.dp)
      ) {
        Text(
          if (planet.quietDays < 0) "Aún sin explorar" else "Quieta hace ${planet.quietDays} días",
          color = OnNightDim, fontSize = 14.sp
        )
        Text(domainOf(quiet)?.displayName ?: "", color = domainTextColor(quiet), fontSize = 17.sp, fontWeight = FontWeight.Bold)
      }
    }
  }
}

/**
 * El descubrimiento del día: la medida de un juego estrella con sus últimas partidas (récord, mejora o "se mantiene",
 * ver [StarMeasures.discover]). Sin 3 partidas de ningún juego estrella, una invitación a jugar uno (se puede tocar).
 */
@Composable
internal fun DiscoveryBlock(discovery: Discovery?, nudge: DiscoveryNudge, onPlay: (String) -> Unit) {
  Column(modifier = Modifier.fillMaxWidth().padding(horizontal = 20.dp)) {
    Box(Modifier.fillMaxWidth().height(1.dp).background(Color.White.copy(alpha = 0.14f)))
    Text(
      "DESCUBRIMIENTO DEL DÍA", color = Clay.Sun, fontSize = 14.sp, fontWeight = FontWeight.Bold,
      letterSpacing = 1.sp, modifier = Modifier.padding(top = 8.dp, bottom = 4.dp)
    )
    if (discovery == null) {
      Row(
        verticalAlignment = Alignment.CenterVertically,
        modifier = Modifier
          .fillMaxWidth()
          .clip(RoundedCornerShape(12.dp))
          .clickable { onPlay(nudge.def.gameId) }
          .padding(vertical = 4.dp)
          .testTag("discovery_nudge")
      ) {
        MiniPlanet(nudge.def.gameId, 32.dp)
        Spacer(Modifier.width(12.dp))
        Text(nudge.text, color = OnNight, fontSize = 14.sp, fontWeight = FontWeight.SemiBold)
      }
      return
    }
    val def = discovery.def
    Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.testTag("discovery")) {
      Column(Modifier.weight(1f)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
          MiniPlanet(def.gameId, 22.dp)
          Spacer(Modifier.width(8.dp))
          Text(def.title, color = OnNightDim, fontSize = 15.sp)
        }
        Row(verticalAlignment = Alignment.Bottom) {
          Text(def.format(discovery.values.last()), color = Color.White, fontSize = 34.sp, fontWeight = FontWeight.Bold)
          Spacer(Modifier.width(6.dp))
          Text(def.unit, color = OnNightDim, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(bottom = 6.dp))
          if (discovery.kind == DiscoveryKind.RECORD) {
            Spacer(Modifier.width(8.dp))
            Text(
              "Récord",
              color = Clay.Ink, fontSize = 14.sp, fontWeight = FontWeight.Bold,
              modifier = Modifier
                .padding(bottom = 9.dp)
                .clip(RoundedCornerShape(50))
                .background(Clay.Sun)
                .border(2.dp, Clay.Ink, RoundedCornerShape(50))
                .padding(horizontal = 8.dp, vertical = 1.dp)
            )
          }
        }
        Text(discovery.caption, color = OnNightDim, fontSize = 15.sp)
      }
      Spacer(Modifier.width(10.dp))
      Sparkline(discovery.values, def.lowerIsBetter, def::format)
    }
  }
}

/** Evolución breve: una línea con las últimas partidas, "mejor" siempre hacia arriba; la última en sol. */
@Composable
private fun Sparkline(values: List<Float>, lowerIsBetter: Boolean, format: (Float) -> String) {
  Column(horizontalAlignment = Alignment.End, modifier = Modifier.width(118.dp)) {
    Text("últimas ${values.size}", color = OnNightSoft, fontSize = 14.sp)
    Canvas(Modifier.fillMaxWidth().height(46.dp).padding(vertical = 5.dp)) {
      val lo = values.minOrNull() ?: 0f
      val hi = values.maxOrNull() ?: 0f
      val span = (hi - lo).takeIf { it > 1e-6f } ?: 1f
      fun y(v: Float): Float {
        val k = (v - lo) / span
        return if (lowerIsBetter) k * size.height else (1f - k) * size.height
      }
      val pts = values.mapIndexed { i, v -> Offset(size.width * i / (values.size - 1).coerceAtLeast(1), y(v)) }
      drawLine(Color.White.copy(alpha = 0.16f), Offset(0f, size.height + 4.dp.toPx()), Offset(size.width, size.height + 4.dp.toPx()), 1.dp.toPx())
      val path = Path().apply { pts.forEachIndexed { i, p -> if (i == 0) moveTo(p.x, p.y) else lineTo(p.x, p.y) } }
      drawPath(path, Clay.Sky, style = Stroke(3.dp.toPx(), cap = StrokeCap.Round, join = StrokeJoin.Round))
      pts.forEachIndexed { i, p ->
        val last = i == pts.lastIndex
        val rr = (if (last) 5 else 3).dp.toPx()
        drawCircle(Clay.Ink, rr + 1.5.dp.toPx(), p)
        drawCircle(if (last) Clay.Sun else Clay.Sky, rr, p)
      }
    }
    Row(Modifier.fillMaxWidth()) {
      Text(format(values.first()), color = OnNightSoft, fontSize = 14.sp)
      Spacer(Modifier.weight(1f))
      Text(format(values.last()), color = Clay.Sun, fontSize = 14.sp, fontWeight = FontWeight.Bold)
    }
  }
}

/**
 * Ventana de una zona (las tarjetas solo van en diálogos): cuánto se jugó, partidas por semana (4 semanas) y cada
 * juego del dominio con su última medida o su nivel. El botón propone el juego sin jugar, o el que lleva más tiempo.
 */
@Composable
private fun ZoneDialog(
  domainKey: String,
  planet: PlanetState,
  history: List<GamePlayResult>,
  measures: List<MeasurePoint>,
  levels: Map<String, Float?>,
  now: Long,
  lang: com.example.model.AppLanguage,
  onPlay: (String) -> Unit,
  onDismiss: () -> Unit
) {
  val domain = domainOf(domainKey) ?: return
  val zone = planet.zones.firstOrNull { it.domain == domainKey }
  val games = GameRegistry.allGames.filter { it.domain == domain }
  val lastPlayed = games.associate { g -> g.id to history.filter { it.gameId == g.id }.maxOfOrNull { it.timestamp } }
  val suggested = games.minByOrNull { lastPlayed[it.id] ?: Long.MIN_VALUE }
  val weekly = Planet.weeklyCounts(history.filter { r -> games.any { it.id == r.gameId } }.map { it.timestamp }, now, ::dayIndex)
  val domainLevel = games.mapNotNull { levels[it.id] }.takeIf { it.isNotEmpty() }?.average()?.toFloat()

  Dialog(onDismissRequest = onDismiss) {
    ClayCard(modifier = Modifier.fillMaxWidth().padding(8.dp), color = Clay.Cream, radius = 28.dp, contentPadding = 20.dp) {
      Column(Modifier.verticalScroll(rememberScrollState())) {
        Row(verticalAlignment = Alignment.CenterVertically) {
          Box(
            Modifier.size(36.dp).clip(RoundedCornerShape(50)).background(domain.color).border(3.dp, Clay.Ink, RoundedCornerShape(50))
          )
          Spacer(Modifier.width(12.dp))
          Column {
            Text("Zona de ${domain.displayName}", color = Clay.Ink, fontSize = 22.sp, fontWeight = FontWeight.Bold)
            val plays = zone?.plays ?: 0
            Text(
              when {
                plays == 0 -> "Aún sin explorar: tu primera partida la hace nacer"
                else -> (domainLevel?.let { "${levelWord(it)} · " } ?: "") +
                  "${if (plays == 1) "1 partida" else "$plays partidas"} · ${zone?.weekPlays ?: 0} esta semana"
              },
              color = Clay.InkSoft, fontSize = 15.sp
            )
          }
        }
        Spacer(Modifier.height(14.dp))
        Text("Partidas por semana", color = Clay.InkSoft, fontSize = 14.sp)
        WeekBars(weekly, domain.color)
        Row(Modifier.fillMaxWidth()) {
          Text("hace 4 semanas", color = Clay.InkSoft, fontSize = 14.sp)
          Spacer(Modifier.weight(1f))
          Text("esta semana", color = Clay.Ink, fontSize = 14.sp, fontWeight = FontWeight.Bold)
        }
        Spacer(Modifier.height(10.dp))
        games.forEach { g ->
          val m = StarMeasures.latest(measures, g.id)
          val last = lastPlayed[g.id]
          val detail = when {
            m != null -> "${m.first.format(m.second)} ${m.first.unit} · tu última"
            last == null -> "Sin jugar aún"
            else -> {
              val days = (dayIndex(now) - dayIndex(last)).toInt()
              when (days) { 0 -> "Jugado hoy"; 1 -> "Jugado ayer"; else -> "Jugado hace $days días" }
            }
          }
          Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.fillMaxWidth().padding(vertical = 6.dp)) {
            MiniPlanet(g.id, 34.dp)
            Spacer(Modifier.width(12.dp))
            Column {
              Text(getGameTitle(g.id, lang, g.title), color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold)
              Text(detail, color = Clay.InkSoft, fontSize = 14.sp)
            }
          }
        }
        if (suggested != null) {
          Spacer(Modifier.height(10.dp))
          ClayButton(
            text = "Jugar ${getGameTitle(suggested.id, lang, suggested.title)}",
            onClick = { onPlay(suggested.id) },
            icon = Icons.Default.PlayArrow,
            modifier = Modifier.testTag("zone_play")
          )
        }
      }
    }
  }
}

/** 4 barras de arcilla (partidas por semana); la de esta semana, con borde más grueso. */
@Composable
private fun WeekBars(counts: List<Int>, color: Color) {
  val max = (counts.maxOrNull() ?: 0).coerceAtLeast(1)
  Row(
    modifier = Modifier.fillMaxWidth().height(64.dp).padding(vertical = 6.dp),
    verticalAlignment = Alignment.Bottom,
    horizontalArrangement = Arrangement.spacedBy(10.dp)
  ) {
    counts.forEachIndexed { i, n ->
      Column(Modifier.weight(1f), horizontalAlignment = Alignment.CenterHorizontally) {
        Text("$n", color = Clay.Ink, fontSize = 14.sp, fontWeight = FontWeight.Bold)
        val frac = if (n == 0) 0.06f else 0.15f + 0.85f * n / max
        Box(
          Modifier
            .fillMaxWidth()
            .height(34.dp * frac)
            .clip(RoundedCornerShape(topStart = 6.dp, topEnd = 6.dp))
            .background(if (n == 0) Clay.InkSoft.copy(alpha = 0.25f) else color)
            .border((if (i == counts.lastIndex) 3 else 2).dp, Clay.Ink, RoundedCornerShape(topStart = 6.dp, topEnd = 6.dp))
        )
      }
    }
  }
}

@Composable
private fun TodayAction(
  pausedGameId: String?,
  nextGameId: String?,
  completed: Int,
  hasBaseline: Boolean,
  missionStep: com.example.data.MissionStep,
  missionMinutesLeft: Int,
  missionFromOtherDay: Boolean,
  archivedTotal: Int,
  onTransmit: () -> Unit,
  onReport: () -> Unit,
  lang: com.example.model.AppLanguage,
  onResume: () -> Unit,
  onPlay: () -> Unit,
  onBaseline: () -> Unit
) {
  val paused = pausedGameId?.let { GameRegistry.getById(it) }
  val next = nextGameId?.let { GameRegistry.getById(it) }
  Column(modifier = Modifier.fillMaxWidth().padding(start = 20.dp, end = 20.dp, top = 6.dp, bottom = 8.dp)) {
    MissionLine(missionStep, missionMinutesLeft, missionFromOtherDay, archivedTotal, completed, onTransmit, onReport)
    val (label, def, action, tag) = when {
      paused != null -> Quad("Tienes una partida en pausa", paused, onResume, "btn_resume_paused")
      next != null -> Quad(if (completed == 0) "Tu sesión de hoy · 3 juegos" else "Tu sesión de hoy · juego ${completed + 1} de 3", next, onPlay, "btn_home_play")
      else -> Quad(null, null, onPlay, "")
    }
    if (def != null) {
      Text(label.orEmpty(), color = OnNightDim, fontSize = 15.sp, modifier = Modifier.padding(start = 4.dp, bottom = 6.dp))
      ClayButton(
        text = getGameTitle(def.id, lang, def.title),
        onClick = action,
        icon = Icons.Default.PlayArrow,
        modifier = Modifier.testTag(tag)
      )
    }
    if (!hasBaseline) {
      Text(
        text = "¿Aún sin tu punto de partida? Encuéntralo en 5 minutos",
        color = Clay.Sun,
        fontSize = 15.sp,
        fontWeight = FontWeight.SemiBold,
        modifier = Modifier
          .padding(top = 8.dp)
          .clip(RoundedCornerShape(10.dp))
          .clickable(onClick = onBaseline)
          .padding(horizontal = 4.dp, vertical = 6.dp)
          .testTag("btn_home_baseline")
      )
    }
  }
}

/**
 * Línea de la Bitácora de Misión en Hoy (texto suelto con su ícono, sin recuadro): dice en qué punto va la misión del
 * día y, cuando hay algo que hacer fuera del flujo de la sesión, se puede tocar.
 */
@Composable
private fun MissionLine(
  step: com.example.data.MissionStep,
  minutesLeft: Int,
  fromOtherDay: Boolean,
  archivedTotal: Int,
  completed: Int,
  onTransmit: () -> Unit,
  onReport: () -> Unit
) {
  val (text, action) = when (step) {
    com.example.data.MissionStep.TRANSMISION ->
      if (completed == 0) "Bitácora: la transmisión del día llega al empezar tu sesión" to null
      else "Bitácora: recibir la transmisión del día" to onTransmit
    com.example.data.MissionStep.ESPERA ->
      (if (minutesLeft > 0) "Bitácora: el informe se abre al terminar tu sesión o en $minutesLeft min"
      else "Bitácora: el informe se abre al terminar tu sesión") to null
    com.example.data.MissionStep.INFORME ->
      (if (fromOtherDay) "Bitácora: tienes un informe pendiente" else "Bitácora: tu informe está listo") to onReport
    com.example.data.MissionStep.AL_DIA ->
      (if (archivedTotal == 1) "Bitácora al día · 1 hallazgo archivado" else "Bitácora al día · $archivedTotal hallazgos archivados") to null
  }
  Row(
    verticalAlignment = Alignment.CenterVertically,
    modifier = Modifier
      .fillMaxWidth()
      .padding(bottom = 8.dp)
      .clip(RoundedCornerShape(10.dp))
      .then(if (action != null) Modifier.clickable(onClick = action) else Modifier)
      .padding(horizontal = 4.dp, vertical = 4.dp)
      .testTag("mission_line")
  ) {
    com.example.ui.components.GameIcon("bitacora", 30.dp)
    Spacer(Modifier.width(10.dp))
    Text(
      text = text,
      color = if (action != null) Clay.Sun else OnNightDim,
      fontSize = 15.sp,
      fontWeight = if (action != null) FontWeight.SemiBold else FontWeight.Normal
    )
  }
}

private data class Quad(val label: String?, val def: com.example.model.GameDefinition?, val action: () -> Unit, val tag: String)
