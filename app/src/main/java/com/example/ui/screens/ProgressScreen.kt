package com.example.ui.screens

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.activity.compose.BackHandler
import androidx.compose.runtime.saveable.rememberSaveable
import com.example.data.AchievementFamily
import com.example.data.Achievements
import com.example.data.AreaProgress
import com.example.data.PlayMode
import com.example.data.StarMeasures
import com.example.data.Streak
import com.example.data.Week
import com.example.data.areaNameOf
import com.example.data.familyState
import com.example.data.historyLine
import com.example.data.nubiWeekLine
import com.example.data.leagueRow
import com.example.data.weekSummary
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.GameEmblem
import com.example.ui.components.LockTabSwipe
import com.example.ui.components.OnBoardText
import com.example.ui.components.SpaceSectionTitle
import com.example.ui.components.SpaceToggle
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.viewmodel.NeuroVidaViewModel
import kotlinx.coroutines.launch

private val OnNight = Color(0xFFEAF0FF)

/**
 * Avance (Tarea 125; antes la pestaña "Liga"; su ícono en la barra de abajo es el farol de la ruta). Sin tarjetas salvo las que se tocan, en secciones sueltas y en este orden (`auditoria-fable/spec-avance-juegos.md` §1): **tu semana** (el único número
 * grande: «3 de 4 días»), **tus áreas** (canal y churro del kit; tocar una abre su detalle), **lo que descubriste** (tus marcas en palabras, comparadas solo contigo), **tus medallas** (tres familias por conducta), **tu liga** (una fila con el escudo
 * chico y Compartir) y **tu punto de partida**. Lo demás, plegado en «Ver más»: tus últimas partidas, en palabras y sin puntos. Se quitaron el hero de la liga, los desafíos de la semana, el avance por juego, la maestría y la tendencia en puntos.
 */
@Composable
fun ProgressScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier
) {
  val gameRanks by viewModel.gameRanks.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  // Las partidas de juegos retirados (data/Models.kt, GameRegistry.retiredDomains) siguen guardadas pero no se muestran.
  val shownHistory = remember(history) { history.filter { !GameRegistry.isRetired(it.gameId) } }
  val progress by viewModel.gameProgress.collectAsState()
  val progressLog by viewModel.progressLog.collectAsState()
  val measures by viewModel.starMeasures.collectAsState()
  val baseline by viewModel.baseline.collectAsState()
  val settings by viewModel.userSettings.collectAsState()
  val unlocks by viewModel.achievementUnlocks.collectAsState()
  val achStats by viewModel.achievementStats.collectAsState()
  val currentLang = LocalAppLanguage.current
  var showMore by remember { mutableStateOf(false) }
  var openArea by rememberSaveable { mutableStateOf<String?>(null) }
  var sheetFor by remember { mutableStateOf<com.example.model.GameDefinition?>(null) }   // la ficha de un juego tocado en el detalle de un área o en «Lo que descubriste»
  var familyOpen by remember { mutableStateOf<AchievementFamily?>(null) }
  val streak by viewModel.currentStreak.collectAsState()
  val context = androidx.compose.ui.platform.LocalContext.current
  val shareScope = androidx.compose.runtime.rememberCoroutineScope()

  val avg = if (gameRanks.isEmpty()) 0 else gameRanks.sumOf { it.rating } / gameRanks.size
  val tier = RankTier.fromRating(avg)
  val now = remember(history, progressLog) { System.currentTimeMillis() }
  val names = remember { DomainType.values().associate { it.name to it.displayName } }
  val statuses = remember(progress, progressLog, now) {
    AREA_ORDER.map { key ->
      AreaProgress.status(key, GameRegistry.allGames.filter { it.domain.name == key }.map { it.id }, progress, progressLog, now)
    }
  }
  val lamps = remember(history, now) { Week.lampsFor(history.map { it.timestamp }, now) }
  val areasPlayed = remember(history, now) { Week.areasPlayedFor(shownHistory.map { it.timestamp to areaNameOf(it.gameId) }, now) }
  val summary = remember(lamps, settings.weeklyGoal, streak, areasPlayed, shownHistory) { weekSummary(lamps, settings.weeklyGoal, streak, areasPlayed, noGamesAtAll = shownHistory.isEmpty()) }
  val streakInfo = remember(history, now) { Streak.fromTimestamps(history.map { it.timestamp }, now) }
  val streakNote = remember(streakInfo) { Streak.forgivenLine(streakInfo) ?: Streak.REST_LINE }
  val nubiLine = remember(lamps, settings.weeklyGoal, streakInfo, areasPlayed, shownHistory) { nubiWeekLine(lamps, settings.weeklyGoal, streakInfo, areasPlayed, noGamesAtAll = shownHistory.isEmpty()) }
  val cards = remember(measures, now) { StarMeasures.cards(measures, now) }
  val nudge = remember(measures, cards) { if (cards.isEmpty()) StarMeasures.nudge(measures) else null }
  val medals = remember(unlocks, achStats) { AchievementFamily.entries.map { Achievements.familyState(it, unlocks.keys, achStats) } }

  openArea?.let { key ->
    LockTabSwipe()
    BackHandler { openArea = null }
    AreaDetail(
      areaKey = key, statuses = statuses, names = names, history = history, measures = measures, now = now,
      lang = currentLang, onArea = { openArea = it },
      onPlay = { id -> openArea = null; viewModel.launchGame(id) },
      onGame = { id -> GameRegistry.getById(id)?.let { sheetFor = it } },
      onClose = { openArea = null }, modifier = modifier, backLabel = "Avance"
    )
    // La ficha del juego tocado, encima del detalle del área. Jugar desde ella no recuerda casilla de Juegos y, al volver de la partida, la persona sigue en Avance.
    sheetFor?.let { g ->
      GameSheetHost(
        game = g, viewModel = viewModel,
        onDismiss = { sheetFor = null },
        onPlay = { m, timed -> sheetFor = null; openArea = null; viewModel.playFromHome(g.id, m, timed) }
      )
    }
    return
  }

  Column(modifier.fillMaxSize().padding(bottom = com.example.ui.components.LocalBarInset.current)) {      // la franja de la barra de pestañas queda libre; el detalle de un área, arriba, la cubre
  TabTopBar(viewModel) { OnBoardText("Tu avance", color = OnNight, fontSize = 26.sp, fontWeight = FontWeight.Bold) }
  LazyColumn(
    modifier = Modifier
      .weight(1f)
      .padding(horizontal = 22.dp),
    contentPadding = PaddingValues(top = 8.dp, bottom = 28.dp),
    verticalArrangement = Arrangement.spacedBy(26.dp)
  ) {
    item { AvanceWeek(summary, lamps, streakNote, if (shownHistory.isEmpty()) "" else nubiLine) }       // sin partidas habla UNA sola Nubi (la línea de abajo), no dos con lo mismo

    if (shownHistory.isEmpty()) {
      // Sin ninguna partida (Tarea 147, P8): en vez de cuatro tarjetas de áreas vacías y «Lo que descubriste» sin nada, Nubi con UNA línea
      item { com.example.ui.components.NubiEmptyLine(com.example.ui.components.EMPTY_AVANCE_LINE) }
    } else {
      item { AvanceAreas(statuses, names) { openArea = it } }

      item {
        AvanceDiscoveries(
          cards = cards, nudge = nudge, measures = measures,
          onGame = { id -> GameRegistry.getById(id)?.let { sheetFor = it } },
          onPlay = { id -> viewModel.playFromHome(id, PlayMode.A_TU_MEDIDA, viewModel.retoFor(id)) }
        )
      }
    }

    item { AvanceMedals(medals) { familyOpen = it } }

    // «Tu liga» y su Compartir: fuera de la v1 (Tarea 149); se ven solo con `Features.LEAGUES`
    if (com.example.Features.LEAGUES) item {
      AvanceLeague(leagueRow(avg)) {
        // Compartir la liga general como imagen (misma tarjeta que al ascender).
        shareScope.launch {
          try {
            com.example.ui.components.ShareCard.share(
              context,
              com.example.ui.components.ShareCard.Content(
                headline = "Estoy en liga ${tier.tierName}",
                subtitle = "Mi liga general",
                tier = tier,
                stats = com.example.ui.components.ShareCard.Content.trophiesAndStreak(avg, streak)
              ),
              "¡Estoy en la liga ${tier.tierName} de Nubi!"
            )
          } catch (e: Exception) {
            android.util.Log.e("ProgressScreen", "No se pudo compartir la liga", e)
            com.example.diag.ErrorLog.record("COMPARTIR", "No se pudo compartir la liga.", e)
          }
        }
      }
    }

    // Punto de partida (antes en el perfil): si se saltó, invitación a hacerlo; si ya se hizo, repetirlo.
    item {
      Column {
        SpaceSectionTitle(
          "Tu punto de partida",
          hint = if (baseline == null) "4 juegos cortos para que cada juego empiece a tu medida" else "Puedes repetirlo cuando quieras"
        )
        Spacer(Modifier.height(12.dp))
        ClayButton(
          text = if (baseline == null) "Encontrar mi punto de partida" else "Repetir la evaluación",
          onClick = { viewModel.startBaseline() },
          color = if (baseline == null) Clay.Sun else Clay.Cream,
          modifier = Modifier.testTag("btn_profile_baseline")
        )
      }
    }

    item {
      SpaceToggle(
        text = if (showMore) "Ver menos" else "Ver más",
        onClick = { showMore = !showMore }
      )
    }

    item {
      AnimatedVisibility(visible = showMore) {
        // Tus últimas partidas, en el lenguaje de la persona: el día, el juego, cómo se jugó y, si hay medida, su valor en palabras. Sin puntos.
        Column {
          SpaceSectionTitle("Tus últimas partidas")
          Spacer(Modifier.height(10.dp))
          shownHistory.take(8).forEach { item ->
            val game = GameRegistry.getById(item.gameId)
            Row(Modifier.fillMaxWidth().padding(vertical = 6.dp), verticalAlignment = Alignment.CenterVertically) {
              GameEmblem(item.gameId, 40.dp)
              Spacer(Modifier.width(12.dp))
              OnBoardText(
                historyLine(item, measures, if (game != null) getGameTitle(game.id, currentLang, game.title) else "Juego"),
                color = OnNight, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.weight(1f)
              )
            }
          }
        }
      }
    }
  }
  }

  familyOpen?.let { f ->
    medals.firstOrNull { it.family == f }?.let { FamilyMedalDialog(it, unlocks, achStats) { familyOpen = null } }
  }
  // La ficha del juego tocado en «Lo que descubriste» (la del detalle de un área se arma arriba).
  sheetFor?.let { g ->
    GameSheetHost(
      game = g, viewModel = viewModel,
      onDismiss = { sheetFor = null },
      onPlay = { m, timed -> sheetFor = null; viewModel.playFromHome(g.id, m, timed) }
    )
  }
}
