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
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
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
import androidx.compose.runtime.saveable.rememberSaveable
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
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.data.AreaProgress
import com.example.data.AreaStatus
import com.example.data.MeasurePoint
import com.example.data.Skill
import com.example.data.StarMeasures
import com.example.model.DomainType
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.AreaBar
import com.example.ui.components.AreaBarColor
import com.example.ui.components.CosmosScroll
import com.example.ui.components.LeagueShield
import com.example.ui.components.MiniPlanet
import com.example.ui.components.NubiBubble
import com.example.ui.components.NubiHome
import com.example.ui.components.NubiPose
import com.example.ui.components.NubiWithHalo
import com.example.ui.components.areaDescription
import com.example.ui.components.domainOf
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
 * Inicio ("Hoy") con NUBI al centro (29-sep, aprobado por Ricardo; maqueta `docs/previews/nubi-hoy.png`): Nubi con su
 * halo, su frase de la semana en palabras y tres áreas a cada lado con su barra de avance (0-100) y el cambio de la
 * semana dibujado en la barra (ver [AreaProgress]). Tocar un área abre su detalle ([AreaDetail]). Debajo, la acción
 * de hoy (sesión, partida en pausa, Bitácora, punto de partida).
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
  val progress by viewModel.gameProgress.collectAsState()
  val progressLog by viewModel.progressLog.collectAsState()
  val weeklyChallenges by viewModel.weeklyChallengeProgress.collectAsState()
  val pausedGameId by viewModel.pausedGameId.collectAsState()
  val baseline by viewModel.baseline.collectAsState()
  val mission by viewModel.mission.collectAsState()
  val measures by viewModel.starMeasures.collectAsState()
  // Reloj de la línea de la Bitácora ("el informe se abre en N min") y de la semana de cada área: cada 30 s.
  var clock by remember { mutableStateOf(System.currentTimeMillis()) }
  LaunchedEffect(Unit) {
    CosmosScroll.offset = 0f
    while (true) {
      kotlinx.coroutines.delay(30_000L)
      clock = System.currentTimeMillis()
    }
  }
  val missionStep = remember(mission, dailySession, clock) { viewModel.missionStep(clock) }
  val lang = LocalAppLanguage.current

  var showChallenges by remember { mutableStateOf(false) }
  var openArea by rememberSaveable { mutableStateOf<String?>(null) }

  val avg = if (ranks.isEmpty()) 0 else ranks.sumOf { it.rating } / ranks.size
  val tier = RankTier.fromRating(avg)
  val index = overallIndex(levels)

  val names = remember { DomainType.values().associate { it.name to it.displayName } }
  val statuses = remember(progress, progressLog, clock) {
    AREA_ORDER.map { key ->
      AreaProgress.status(key, GameRegistry.allGames.filter { it.domain.name == key }.map { it.id }, progress, progressLog, clock)
    }
  }

  openArea?.let { key ->
    BackHandler { openArea = null }
    AreaDetail(
      areaKey = key,
      statuses = statuses,
      names = names,
      history = history,
      measures = measures,
      now = clock,
      lang = lang,
      onArea = { openArea = it },
      onPlay = { id ->
        openArea = null
        viewModel.launchGame(id)
      },
      onClose = { openArea = null },
      modifier = modifier
    )
    return
  }

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

    NubiHome(
      statuses = statuses,
      names = names,
      line = AreaProgress.nubiLine(statuses, names),
      onArea = { openArea = it },
      modifier = Modifier
        .weight(1f)
        .heightIn(min = 260.dp)
        .fillMaxWidth()
        .padding(top = 10.dp)
        .testTag("home_nubi")
    )

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

/** Lo que dice Nubi arriba del detalle de un área, en palabras. */
private fun detailLine(name: String, s: AreaStatus): String = when {
  s.value == null -> "$name aún sin medir: juega y lo verás"
  s.change > 0f -> "Esta semana $name avanzó"
  s.change < 0f -> "$name bajó un poco: se recupera jugando"
  else -> "$name se mantiene esta semana"
}

/**
 * Detalle de un área (vista "C" aprobada por Ricardo): Nubi explica, la barra grande con las 5 etapas nombradas, de
 * dónde a dónde avanzó en la semana y cuánto falta para la siguiente, las últimas 4 semanas y sus juegos. Abajo, en
 * vez de "Jugar Memoria", la pregunta de Nubi con el estilo del botón y "¡Sí, vamos!" (Nubi elige el juego del área
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
  onPlay: (String) -> Unit,
  onClose: () -> Unit,
  modifier: Modifier = Modifier
) {
  val idx = AREA_ORDER.indexOf(areaKey).coerceAtLeast(0)
  val s = statuses.getOrNull(idx) ?: AreaStatus(areaKey, null, 0f, emptyList())
  val name = names[areaKey] ?: areaKey
  val domain = domainOf(areaKey) ?: return
  val games = GameRegistry.allGames.filter { it.domain == domain }
  val lastPlayed = games.associate { g -> g.id to history.filter { it.gameId == g.id }.maxOfOrNull { it.timestamp } }
  val suggested = games.minByOrNull { lastPlayed[it.id] ?: Long.MIN_VALUE }
  fun go(step: Int) = onArea(AREA_ORDER[(idx + step + AREA_ORDER.size) % AREA_ORDER.size])

  Column(modifier.fillMaxSize().testTag("area_detail")) {
    Row(
      verticalAlignment = Alignment.CenterVertically,
      modifier = Modifier
        .padding(start = 8.dp, top = 4.dp)
        .clip(RoundedCornerShape(12.dp))
        .clickable(onClick = onClose)
        .padding(horizontal = 8.dp, vertical = 8.dp)
        .testTag("area_back")
    ) {
      Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = null, tint = OnNight, modifier = Modifier.size(22.dp))
      Spacer(Modifier.width(8.dp))
      Text("Hoy", color = OnNight, fontSize = 17.sp, fontWeight = FontWeight.SemiBold)
    }
    Column(
      Modifier.weight(1f).verticalScroll(rememberScrollState()).padding(horizontal = 20.dp),
      horizontalAlignment = Alignment.CenterHorizontally
    ) {
      NubiBubble("Nubi", detailLine(name, s))
      var drag by remember(areaKey) { mutableStateOf(0f) }
      Row(
        verticalAlignment = Alignment.CenterVertically,
        modifier = Modifier.fillMaxWidth().pointerInput(areaKey) {
          detectHorizontalDragGestures(
            onDragEnd = { if (drag > 60.dp.toPx()) go(-1) else if (drag < -60.dp.toPx()) go(1); drag = 0f },
            onDragCancel = { drag = 0f }
          ) { _, dx -> drag += dx }
        }
      ) {
        AreaArrow(left = true, onClick = { go(-1) })
        Spacer(Modifier.weight(1f))
        NubiWithHalo(size = 118.dp, pose = NubiPose.MIRA)
        Spacer(Modifier.weight(1f))
        AreaArrow(left = false, onClick = { go(1) })
      }
      Text(name, color = Color.White, fontSize = 28.sp, fontWeight = FontWeight.Bold)
      Text(
        s.value?.let { "Etapa ${Skill.stageName(it)} · ${s.points} de 100" } ?: "Aún sin medir",
        color = OnNightDim, fontSize = 16.sp
      )
      Spacer(Modifier.height(12.dp))
      AreaBar(s.value, s.change, height = 14.dp, modifier = Modifier.semantics { contentDescription = areaDescription(name, s) })
      Row(Modifier.fillMaxWidth().padding(top = 6.dp)) {
        Skill.STAGES.forEachIndexed { i, st ->
          val here = s.value != null && Skill.stage(s.value) == i
          Text(
            st, fontSize = 13.sp, textAlign = TextAlign.Center, maxLines = 1,
            color = if (here) Color.White else OnNightSoft, fontWeight = if (here) FontWeight.Bold else FontWeight.Normal,
            modifier = Modifier.weight(1f)
          )
        }
      }
      Spacer(Modifier.height(14.dp))
      Text(AreaProgress.changeLine(s), color = Color.White, fontSize = 17.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center)
      AreaProgress.toNextLine(s)?.let { Text(it, color = OnNightDim, fontSize = 15.sp) }
      if (s.value != null && s.weeks.count { it != null } >= 2) WeeksChart(s.weeks)
      Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.padding(vertical = 12.dp)) {
        AREA_ORDER.forEachIndexed { i, _ ->
          Box(
            Modifier.size(if (i == idx) 10.dp else 8.dp).clip(RoundedCornerShape(50))
              .background(if (i == idx) Color.White else Color.White.copy(alpha = 0.25f))
          )
        }
      }
      Text(
        "Tus juegos de ${name.lowercase()}", color = Clay.Sun, fontSize = 14.sp, fontWeight = FontWeight.Bold,
        modifier = Modifier.fillMaxWidth().padding(top = 4.dp, bottom = 2.dp)
      )
      games.forEach { g ->
        val m = StarMeasures.latest(measures, g.id)
        val last = lastPlayed[g.id]
        val detail = when {
          m != null -> "${m.first.format(m.second)} ${m.first.unit} · tu última"
          last == null -> "Sin jugar aún"
          else -> playedAgo(now, last)
        }
        Row(
          verticalAlignment = Alignment.CenterVertically,
          modifier = Modifier.fillMaxWidth().clip(RoundedCornerShape(12.dp)).clickable { onPlay(g.id) }.padding(vertical = 6.dp)
        ) {
          MiniPlanet(g.id, 34.dp)
          Spacer(Modifier.width(12.dp))
          Column {
            Text(getGameTitle(g.id, lang, g.title), color = Color.White, fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
            Text(detail, color = OnNightDim, fontSize = 14.sp)
          }
        }
      }
      Spacer(Modifier.height(8.dp))
    }
    if (suggested != null) {
      // La invitación: la pregunta con el mismo estilo que el botón, para que se lean juntas.
      Column(Modifier.fillMaxWidth().padding(start = 20.dp, end = 20.dp, top = 8.dp, bottom = 10.dp), horizontalAlignment = Alignment.CenterHorizontally) {
        Text(
          "¿Le damos un empujón a ${AreaProgress.your(name)}?", color = Clay.Sun, fontSize = 20.sp,
          fontWeight = FontWeight.Bold, textAlign = TextAlign.Center, modifier = Modifier.padding(bottom = 8.dp)
        )
        ClayButton(text = "¡Sí, vamos!", onClick = { onPlay(suggested.id) }, modifier = Modifier.testTag("area_play"))
        val title = getGameTitle(suggested.id, lang, suggested.title)
        val last = lastPlayed[suggested.id]
        Text(
          when {
            last == null -> "Nubi eligió $title: aún no la juegas"
            else -> "Nubi eligió $title: ${playedAgo(now, last).replaceFirstChar { it.lowercase() }}"
          },
          color = OnNightDim, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 8.dp)
        )
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
  Box(
    Modifier.size(48.dp).clip(RoundedCornerShape(50)).background(Color.White.copy(alpha = 0.12f)).clickable(onClick = onClick),
    contentAlignment = Alignment.Center
  ) {
    Icon(
      if (left) Icons.AutoMirrored.Filled.KeyboardArrowLeft else Icons.AutoMirrored.Filled.KeyboardArrowRight,
      contentDescription = if (left) "Área anterior" else "Área siguiente", tint = Color.White, modifier = Modifier.size(28.dp)
    )
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
        Text(
          v?.let { "${it.toInt()}" } ?: "", fontSize = 13.sp, textAlign = TextAlign.Center, modifier = Modifier.weight(1f),
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
      listOf("hace 3", "hace 2", "pasada", "esta").forEachIndexed { i, t ->
        Text(t, fontSize = 13.sp, textAlign = TextAlign.Center, color = if (i == 3) Color.White else OnNightSoft, modifier = Modifier.weight(1f))
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
