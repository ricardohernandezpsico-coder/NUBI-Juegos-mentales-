package com.example.ui.screens

import androidx.compose.material3.minimumInteractiveComponentSize
import androidx.compose.material.icons.filled.Share
import kotlinx.coroutines.launch
import androidx.compose.ui.platform.testTag
import androidx.compose.foundation.clickable
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.activity.compose.BackHandler
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import com.example.data.AreaProgress
import com.example.data.Skill
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.AreaBar
import com.example.ui.components.LockTabSwipe
import com.example.ui.components.areaDescription
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.components.BellCurveCard
import com.example.ui.components.DomainRadarCard
import com.example.ui.components.GameLevelsList
import com.example.ui.components.LeagueHero
import com.example.ui.components.ProgressTrendChart
import com.example.ui.components.SpaceSectionTitle
import com.example.ui.components.SpaceToggle
import com.example.ui.components.overallIndex
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getDomainName
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.TealPrimary
import com.example.viewmodel.NeuroVidaViewModel
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = Color(0xFFB4BFEA)

/**
 * Avance (29-sep; antes la pestaña "Liga"; ícono: un cerebro, `docs/previews/navegacion-nubi.png`). Sin tarjetas, en
 * secciones sueltas: tu liga (escudo y compartir), tus 6 áreas con la barra de Hoy (tocar una abre su detalle), los
 * desafíos de la semana, tu avance en cada juego y tu punto de partida. Lo secundario (tu posición, el mapa de áreas,
 * maestría, tendencia e historial) queda plegado detrás de "Ver más detalles" para no saturar.
 */
@Composable
fun ProgressScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier
) {
  val domainMastery by viewModel.domainMasteryInfo.collectAsState()
  val gameRanks by viewModel.gameRanks.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val levels by viewModel.gameLevelsForProgress.collectAsState()
  val progress by viewModel.gameProgress.collectAsState()
  val progressLog by viewModel.progressLog.collectAsState()
  val measures by viewModel.starMeasures.collectAsState()
  val weeklyChallenges by viewModel.weeklyChallengeProgress.collectAsState()
  val baseline by viewModel.baseline.collectAsState()
  val currentLang = LocalAppLanguage.current
  val dateFormatter = remember { SimpleDateFormat("dd MMM, HH:mm", Locale.getDefault()) }
  var showMore by remember { mutableStateOf(false) }
  var openArea by rememberSaveable { mutableStateOf<String?>(null) }
  val streak by viewModel.currentStreak.collectAsState()
  val context = androidx.compose.ui.platform.LocalContext.current
  val shareScope = androidx.compose.runtime.rememberCoroutineScope()

  val avg = if (gameRanks.isEmpty()) 0 else gameRanks.sumOf { it.rating } / gameRanks.size
  val tier = RankTier.fromRating(avg)
  val prog = if (tier == RankTier.MAESTRO) ((avg - tier.minRating) % 250) / 250f else (avg - tier.minRating) / 250f
  val now = remember(history, progressLog) { System.currentTimeMillis() }
  val names = remember { DomainType.values().associate { it.name to it.displayName } }
  val statuses = remember(progress, progressLog, now) {
    AREA_ORDER.map { key ->
      AreaProgress.status(key, GameRegistry.allGames.filter { it.domain.name == key }.map { it.id }, progress, progressLog, now)
    }
  }

  openArea?.let { key ->
    LockTabSwipe()
    BackHandler { openArea = null }
    AreaDetail(
      areaKey = key, statuses = statuses, names = names, history = history, measures = measures, now = now,
      lang = currentLang, onArea = { openArea = it },
      onPlay = { id -> openArea = null; viewModel.launchGame(id) },
      onClose = { openArea = null }, modifier = modifier
    )
    return
  }

  Column(modifier.fillMaxSize()) {
  TabTopBar(viewModel) { Text("Tu avance", color = OnNight, fontSize = 26.sp, fontWeight = FontWeight.Bold) }
  LazyColumn(
    modifier = Modifier
      .weight(1f)
      .padding(horizontal = 22.dp),
    contentPadding = PaddingValues(top = 8.dp, bottom = 28.dp),
    verticalArrangement = Arrangement.spacedBy(26.dp)
  ) {
    item {
      Column {
        LeagueHero(tier = tier, rating = avg, progress = prog, index = overallIndex(levels))
        Spacer(Modifier.height(14.dp))
        // Compartir la liga general como imagen (misma tarjeta que al ascender).
        com.example.ui.theme.ClayPill(
          text = "Compartir mi liga",
          color = com.example.ui.theme.Clay.Sun,
          icon = androidx.compose.material.icons.Icons.Default.Share,
          modifier = Modifier
            .align(Alignment.CenterHorizontally)
            .minimumInteractiveComponentSize() // área de toque de 48 dp aunque la píldora sea más baja
            .clip(androidx.compose.foundation.shape.RoundedCornerShape(50))
            .clickable {
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
            .testTag("btn_share_league")
        )
      }
    }

    // Tus 6 áreas con la misma barra de Hoy (cambio de la semana en la barra); tocar una abre su detalle.
    item {
      Column {
        SpaceSectionTitle("Tus áreas", hint = "Tu avance de 0 a 100 en cada una; toca una para ver el detalle")
        Spacer(Modifier.height(8.dp))
        statuses.forEach { s ->
          val name = names[s.area] ?: s.area
          Column(
            Modifier
              .fillMaxWidth()
              .clip(RoundedCornerShape(12.dp))
              .clickable { openArea = s.area }
              .padding(vertical = 8.dp)
              .semantics(mergeDescendants = true) { contentDescription = areaDescription(name, s) }
              .testTag("progress_area_${s.area.lowercase()}")
          ) {
            Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
              Text(name, color = OnNight, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
              Text(s.value?.let { Skill.stageName(it) } ?: "Sin medir", color = OnNightDim, fontSize = 14.sp)
            }
            AreaBar(s.value, s.change, modifier = Modifier.padding(top = 8.dp))
          }
        }
      }
    }

    // Desafíos de la semana (antes en la cabecera de Hoy).
    if (weeklyChallenges.isNotEmpty()) item {
      Column {
        SpaceSectionTitle(
          "Desafíos de la semana",
          hint = "${weeklyChallenges.count { it.isComplete }} de ${weeklyChallenges.size} listos"
        )
        Spacer(Modifier.height(8.dp))
        weeklyChallenges.forEach { wc ->
          Column(Modifier.padding(vertical = 6.dp)) {
            Row(Modifier.fillMaxWidth()) {
              Text(wc.def.title, color = OnNight, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
              Text(
                if (wc.isComplete) "Listo" else "${wc.progress} de ${wc.def.target}",
                color = if (wc.isComplete) Clay.Lime else Clay.Sun, fontSize = 14.sp, fontWeight = FontWeight.Bold
              )
            }
            Box(
              Modifier.padding(top = 6.dp).fillMaxWidth().height(8.dp).clip(RoundedCornerShape(4.dp))
                .background(Color.White.copy(alpha = 0.15f))
            ) {
              Box(
                Modifier.fillMaxWidth((wc.progress.toFloat() / wc.def.target).coerceIn(0.03f, 1f)).height(8.dp)
                  .clip(RoundedCornerShape(4.dp)).background(if (wc.isComplete) Clay.Lime else Clay.Sun)
              )
            }
          }
        }
      }
    }

    item {
      Column {
        SpaceSectionTitle("Tu avance por juego", hint = "Tu liga en cada juego y tu etapa")
        Spacer(Modifier.height(14.dp))
        GameLevelsList(levels = levels, ranks = gameRanks.associateBy { it.gameId })
      }
    }

    // Punto de partida (antes en el perfil): si se saltó, invitación a hacerlo; si ya se hizo, repetirlo.
    item {
      Column {
        SpaceSectionTitle(
          "Tu punto de partida",
          hint = if (baseline == null) "3 juegos cortos para que cada juego empiece a tu medida" else "Puedes repetirlo cuando quieras"
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
        text = if (showMore) "Ocultar detalles" else "Ver más detalles",
        onClick = { showMore = !showMore }
      )
    }

    item {
      AnimatedVisibility(visible = showMore) {
        Column(verticalArrangement = Arrangement.spacedBy(26.dp)) {
          BellCurveCard(
            levels = levels,
            scoresWithTime = history.map { it.score to it.timestamp },
            accent = TealPrimary
          )
          DomainRadarCard(levels = levels)
          // Maestría por dominio (XP que solo crece)
          Column {
            SpaceSectionTitle("Maestría por dominio", hint = "Cada partida suma experiencia a su dominio")
            Spacer(Modifier.height(12.dp))
            Column(verticalArrangement = Arrangement.spacedBy(14.dp)) {
              domainMastery.forEach { info ->
                Column {
                  Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                      Box(Modifier.size(10.dp).clip(CircleShape).background(info.domain.color))
                      Spacer(Modifier.width(8.dp))
                      Text(getDomainName(info.domain, currentLang), color = OnNight, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
                    }
                    Text("${info.tier.tierName} · ${info.xpLabel}", color = info.domain.color, fontSize = 14.sp, fontWeight = FontWeight.Bold)
                  }
                  Box(
                    modifier = Modifier
                      .padding(top = 6.dp)
                      .fillMaxWidth()
                      .height(6.dp)
                      .clip(RoundedCornerShape(3.dp))
                      .background(Color.White.copy(alpha = 0.12f))
                  ) {
                    Box(
                      Modifier
                        .fillMaxWidth(info.progressInTier.coerceIn(0.03f, 1f))
                        .height(6.dp)
                        .clip(RoundedCornerShape(3.dp))
                        .background(info.domain.color)
                    )
                  }
                }
              }
            }
          }

          // Tendencia
          ProgressTrendChart(history = history)

          // Historial reciente como filas sueltas
          Column {
            SpaceSectionTitle("Historial reciente")
            Spacer(Modifier.height(10.dp))
            history.take(8).forEach { item ->
              val game = GameRegistry.getById(item.gameId)
              Row(
                modifier = Modifier.fillMaxWidth().padding(vertical = 7.dp),
                verticalAlignment = Alignment.CenterVertically
              ) {
                Box(Modifier.size(10.dp).clip(CircleShape).background(game?.domain?.color ?: TealPrimary))
                Spacer(Modifier.width(10.dp))
                Column(Modifier.weight(1f)) {
                  Text(
                    if (game != null) getGameTitle(game.id, currentLang, game.title) else "Juego",
                    color = OnNight, fontSize = 15.sp, fontWeight = FontWeight.SemiBold
                  )
                  Text(dateFormatter.format(Date(item.timestamp)), color = OnNightDim, fontSize = 14.sp)
                }
                Text("${item.score} pts", color = game?.domain?.color ?: TealPrimary, fontSize = 15.sp, fontWeight = FontWeight.Bold)
              }
            }
          }
        }
      }
    }
  }
  }
}
