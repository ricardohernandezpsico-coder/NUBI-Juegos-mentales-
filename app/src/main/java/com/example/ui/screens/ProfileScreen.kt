package com.example.ui.screens

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
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
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Whatshot
import androidx.compose.material3.Icon
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
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.style.TextAlign
import com.example.data.AchievementDef
import com.example.data.AchievementStats
import com.example.data.Achievements
import com.example.model.RankTier
import com.example.ui.components.AchievementMedal
import com.example.ui.components.medalColor
import com.example.ui.components.DomainLegend
import com.example.ui.components.LeagueShield
import com.example.ui.components.SpaceSectionTitle
import com.example.ui.components.levelWord
import com.example.ui.components.overallIndex
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.viewmodel.NeuroVidaViewModel

/**
 * Cabecera de los paneles que se abren desde arriba a la derecha (perfil y opciones): flecha para volver y el título.
 */
@Composable
fun PanelHeader(title: String, onBack: () -> Unit) {
  Row(
    modifier = Modifier
      .fillMaxWidth()
      .padding(horizontal = 12.dp, vertical = 8.dp),
    verticalAlignment = Alignment.CenterVertically
  ) {
    Box(
      modifier = Modifier
        .size(44.dp)
        .clip(CircleShape)
        .background(Clay.Cream)
        .border(Clay.Border, Clay.Ink, CircleShape)
        .clickable(onClick = onBack)
        .testTag("btn_panel_back"),
      contentAlignment = Alignment.Center
    ) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Volver", tint = Clay.Ink) }
    Spacer(Modifier.width(12.dp))
    Text(title, color = Color(0xFFEAF0FF), fontSize = 22.sp, fontWeight = FontWeight.Bold)
  }
}

/** Las opciones (engranaje de arriba a la derecha), con su flecha de regreso. */
@Composable
fun SettingsPanel(viewModel: NeuroVidaViewModel, onClose: () -> Unit, modifier: Modifier = Modifier) {
  var showLicenses by remember { mutableStateOf(false) }
  if (showLicenses) {
    // "Licencias y créditos" (1-oct): su propia cabecera; Atrás vuelve a Opciones.
    LicensesScreen(onClose = { showLicenses = false }, modifier = modifier)
    return
  }
  BackHandler(onBack = onClose)
  Column(modifier = modifier.fillMaxSize()) {
    PanelHeader("Opciones", onClose)
    SettingsScreen(viewModel = viewModel, modifier = Modifier.weight(1f), onOpenLicenses = { showLicenses = true })
  }
}

/**
 * Perfil (se abre con tu inicial, arriba a la derecha, desde el 29-sep): quién eres (escudo, nombre, liga), tres cifras
 * y tus logros. Las áreas y el punto de partida pasaron a Avance; los ajustes, al engranaje.
 */
@Composable
fun ProfileScreen(viewModel: NeuroVidaViewModel, onClose: () -> Unit, modifier: Modifier = Modifier) {
  BackHandler(onBack = onClose)
  val userSettings by viewModel.userSettings.collectAsState()
  val streak by viewModel.currentStreak.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val ranks by viewModel.gameRanks.collectAsState()
  val levels by viewModel.gameLevelsForProgress.collectAsState()
  val unlocks by viewModel.achievementUnlocks.collectAsState()
  val achStats by viewModel.achievementStats.collectAsState()
  var achievementDetail by remember { mutableStateOf<AchievementDef?>(null) }

  val avg = if (ranks.isEmpty()) 0 else ranks.sumOf { it.rating } / ranks.size
  val tier = RankTier.fromRating(avg)
  val index = overallIndex(levels)
  val best = history.maxOfOrNull { it.score }

  val prog = if (tier == RankTier.MAESTRO) ((avg - tier.minRating) % 250) / 250f else (avg - tier.minRating) / 250f

  Column(modifier.fillMaxSize()) {
  PanelHeader("Tu perfil", onClose)
  LazyColumn(
    modifier = Modifier
      .weight(1f)
      .padding(horizontal = 22.dp),
    contentPadding = PaddingValues(top = 4.dp, bottom = 28.dp),
    verticalArrangement = Arrangement.spacedBy(22.dp)
  ) {
    // Tu escudo y tu nombre, sin recuadros
    item {
      Column(modifier = Modifier.fillMaxWidth(), horizontalAlignment = Alignment.CenterHorizontally) {
        Box(contentAlignment = Alignment.BottomEnd) {
          LeagueShield(tier = tier, size = 160.dp, pips = ((prog * 5).toInt() + 1).coerceIn(1, 5), glow = true)
          Box(
            modifier = Modifier
              .size(54.dp)
              .clip(CircleShape)
              .background(Clay.Sky)
              .border(Clay.Border, Clay.Ink, CircleShape),
            contentAlignment = Alignment.Center
          ) {
            // La misma inicial del botón de arriba a la derecha (sin emojis como íconos).
            Text(userSettings.name.trim().firstOrNull()?.uppercase() ?: "", color = Clay.Ink, fontSize = 26.sp, fontWeight = FontWeight.Bold)
          }
        }
        Text(userSettings.name.ifBlank { "Tu perfil" }, color = Color(0xFFEAF0FF), fontSize = 30.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 6.dp))
        Text(
          text = "Liga ${tier.tierName}" + (index?.let { "  ·  nivel ${levelWord(it / 100f).lowercase()}" } ?: ""),
          color = Color(0xFFB4BFEA),
          fontSize = 15.sp
        )
      }
    }

    // Tres cifras como texto grande, separadas por lineas finas
    item {
      Row(modifier = Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        StatText("Racha", "$streak", Clay.Sun, Modifier.weight(1f), fire = true)
        Box(Modifier.width(1.dp).height(44.dp).background(Color.White.copy(alpha = 0.14f)))
        StatText("Partidas", "${history.size}", Clay.Sky, Modifier.weight(1f))
        Box(Modifier.width(1.dp).height(44.dp).background(Color.White.copy(alpha = 0.14f)))
        StatText("Mejor", best?.toString() ?: "–", Clay.Lime, Modifier.weight(1f))
      }
    }

    // Logros: medallas en filas de 4; las bloqueadas muestran cuánto falta.
    item {
      Column {
        SpaceSectionTitle("Logros", hint = "${unlocks.keys.count { Achievements.byId(it) != null }} de ${Achievements.all.size} conseguidos")
        Spacer(Modifier.height(14.dp))
        Achievements.all.chunked(4).forEach { row ->
          Row(modifier = Modifier.fillMaxWidth().padding(bottom = 14.dp)) {
            row.forEach { def ->
              val got = def.id in unlocks
              Column(
                modifier = Modifier
                  .weight(1f)
                  .clip(RoundedCornerShape(16.dp))
                  .clickable { achievementDetail = def }
                  .padding(vertical = 4.dp)
                  .testTag("achievement_${def.id}"),
                horizontalAlignment = Alignment.CenterHorizontally
              ) {
                AchievementMedal(def = def, unlocked = got, size = 64.dp)
                Text(
                  text = def.title,
                  color = if (got) Color(0xFFEAF0FF) else Color(0xFFB4BFEA).copy(alpha = 0.7f),
                  fontSize = 14.sp,
                  fontWeight = FontWeight.SemiBold,
                  textAlign = TextAlign.Center,
                  maxLines = 2,
                  lineHeight = 14.sp
                )
                if (!got) {
                  val (cur, goal) = def.progress(achStats)
                  Text("$cur/$goal", color = Clay.Sun.copy(alpha = 0.8f), fontSize = 14.sp)
                }
              }
            }
            repeat(4 - row.size) { Spacer(Modifier.weight(1f)) }
          }
        }
      }
    }

  }
  }

  achievementDetail?.let { def ->
    AchievementDetailDialog(def, unlocks[def.id], achStats) { achievementDetail = null }
  }
}

/** Detalle de un logro: medalla grande, qué hay que hacer, cuándo se consiguió o cuánto falta. */
@Composable
private fun AchievementDetailDialog(def: AchievementDef, unlockedAt: Long?, stats: AchievementStats, onDismiss: () -> Unit) {
  androidx.compose.ui.window.Dialog(onDismissRequest = onDismiss) {
    com.example.ui.theme.ClayCard(
      modifier = Modifier.fillMaxWidth().padding(8.dp),
      color = Clay.Cream,
      radius = 28.dp,
      contentPadding = 22.dp
    ) {
      Column(modifier = Modifier.fillMaxWidth(), horizontalAlignment = Alignment.CenterHorizontally) {
        Box(
          modifier = Modifier
            .clip(CircleShape)
            .background(Color(0xFF0A0F33))
            .padding(10.dp)
        ) { AchievementMedal(def = def, unlocked = unlockedAt != null, size = 110.dp) }
        Text(def.title, color = Clay.Ink, fontSize = 24.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 10.dp))
        Text(def.description, color = Clay.InkSoft, fontSize = 15.sp, textAlign = TextAlign.Center)
        Spacer(Modifier.height(10.dp))
        if (unlockedAt != null) {
          val f = java.text.SimpleDateFormat("d 'de' MMMM yyyy", java.util.Locale("es"))
          Text("Conseguido el ${f.format(java.util.Date(unlockedAt))}", color = Clay.Ink, fontSize = 14.sp, fontWeight = FontWeight.SemiBold)
        } else {
          val (cur, goal) = def.progress(stats)
          Text("Llevas $cur de $goal", color = Clay.Ink, fontSize = 14.sp, fontWeight = FontWeight.SemiBold)
          Box(
            modifier = Modifier
              .padding(top = 8.dp)
              .fillMaxWidth()
              .height(10.dp)
              .clip(RoundedCornerShape(5.dp))
              .background(Clay.Ink.copy(alpha = 0.12f))
          ) {
            Box(
              modifier = Modifier
                .fillMaxWidth((cur.toFloat() / goal).coerceIn(0.03f, 1f))
                .height(10.dp)
                .background(def.medalColor())
            )
          }
        }
      }
    }
  }
}

@Composable
private fun StatText(label: String, value: String, color: Color, modifier: Modifier, fire: Boolean = false) {
  Column(modifier = modifier, horizontalAlignment = Alignment.CenterHorizontally) {
    Row(verticalAlignment = Alignment.CenterVertically) {
      if (fire) Icon(Icons.Default.Whatshot, contentDescription = null, tint = color, modifier = Modifier.size(24.dp))
      Text(value, color = color, fontSize = 34.sp, fontWeight = FontWeight.Bold)
    }
    Text(label, color = Color(0xFFB4BFEA), fontSize = 15.sp)
  }
}
