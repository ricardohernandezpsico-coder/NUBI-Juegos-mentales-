package com.example.ui.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Whatshot
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.AreaProgress
import com.example.data.SessionSummary
import com.example.data.Skill
import com.example.model.AppLanguage
import com.example.model.GameRegistry
import com.example.ui.components.AreaBar
import com.example.ui.components.MiniPlanet
import com.example.ui.components.NubiPose
import com.example.ui.components.NubiWithHalo
import com.example.ui.components.areaDescription
import com.example.ui.components.domainOf
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton

private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = Color(0xFFC7D0FF)

/**
 * Resumen de la sesión del día (29-sep, pedido de Ricardo): al terminar los 3 juegos, Nubi celebra y dice qué se
 * trabajó, el puntaje de cada juego y cuánto se movió cada área desde que empezó la sesión (en la barra de avance de
 * Hoy y en palabras: "Hoy avanzó de 46 a 52"). Texto suelto sobre el cielo, sin recuadros.
 */
@Composable
fun SessionSummaryScreen(
  summary: SessionSummary,
  streak: Int,
  lang: AppLanguage,
  onClose: () -> Unit,
  modifier: Modifier = Modifier
) {
  val names = summary.areas.map { domainOf(it.area)?.displayName ?: it.area }
  Column(modifier.fillMaxSize().testTag("session_summary")) {
    Column(
      Modifier.weight(1f).verticalScroll(rememberScrollState()).padding(horizontal = 24.dp),
      horizontalAlignment = Alignment.CenterHorizontally
    ) {
      Spacer(Modifier.height(8.dp))
      NubiWithHalo(size = 130.dp, pose = NubiPose.CELEBRA)
      Text("¡Sesión de hoy completa!", color = Color.White, fontSize = 28.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center)
      Text(
        "Trabajaste " + joinNames(names.map { it.lowercase() }),
        color = OnNightDim, fontSize = 16.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 4.dp)
      )
      if (streak > 0) Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(top = 10.dp)) {
        Icon(Icons.Default.Whatshot, contentDescription = null, tint = Clay.Sun, modifier = Modifier.size(20.dp))
        Spacer(Modifier.width(6.dp))
        Text(if (streak == 1) "Racha: 1 día" else "Racha: $streak días", color = Clay.Sun, fontSize = 16.sp, fontWeight = FontWeight.Bold)
      }

      Text(
        "TUS JUEGOS", color = Clay.Sun, fontSize = 14.sp, fontWeight = FontWeight.Bold,
        modifier = Modifier.fillMaxWidth().padding(top = 22.dp, bottom = 4.dp)
      )
      summary.games.forEach { g ->
        val def = GameRegistry.getById(g.gameId)
        Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.fillMaxWidth().padding(vertical = 6.dp)) {
          MiniPlanet(g.gameId, 36.dp, done = g.score != null)
          Spacer(Modifier.width(12.dp))
          Column(Modifier.weight(1f)) {
            Text(def?.let { getGameTitle(it.id, lang, it.title) } ?: g.gameId, color = Color.White, fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
            Text(domainOf(g.area)?.displayName ?: "", color = OnNightDim, fontSize = 14.sp)
          }
          Column(horizontalAlignment = Alignment.End) {
            Text(g.score?.let { "$it" } ?: "—", color = Color.White, fontSize = 22.sp, fontWeight = FontWeight.Bold)
            Text("puntaje de 100", color = OnNightDim, fontSize = 14.sp)
          }
        }
      }

      Text(
        "TU AVANCE", color = Clay.Sun, fontSize = 14.sp, fontWeight = FontWeight.Bold,
        modifier = Modifier.fillMaxWidth().padding(top = 18.dp, bottom = 4.dp)
      )
      summary.areas.forEachIndexed { i, a ->
        Column(Modifier.fillMaxWidth().padding(vertical = 8.dp).semantics { contentDescription = areaDescription(names[i], a) }) {
          Row(verticalAlignment = Alignment.Bottom) {
            Text(names[i], color = Color.White, fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
            Spacer(Modifier.weight(1f))
            Text(a.value?.let { Skill.stageName(it) } ?: "Sin medir", color = OnNightDim, fontSize = 14.sp)
          }
          AreaBar(a.value, a.change, height = 10.dp, modifier = Modifier.padding(vertical = 8.dp))
          Text(
            if (a.value == null) "Se mide al jugarlo a tu medida" else AreaProgress.changeLine(a, "Hoy"),
            color = OnNightDim, fontSize = 14.sp
          )
        }
      }
      Spacer(Modifier.height(8.dp))
    }
    Column(Modifier.fillMaxWidth().padding(start = 24.dp, end = 24.dp, bottom = 12.dp, top = 8.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
      Text(
        "Nubi te espera mañana para seguir",
        color = OnNightDim, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.fillMaxWidth()
      )
      ClayButton(text = "Volver a Hoy", onClick = onClose, modifier = Modifier.testTag("btn_summary_close"))
    }
  }
}

/** "memoria", "memoria y atención", "memoria, atención y velocidad". */
private fun joinNames(names: List<String>): String = when (names.size) {
  0 -> "hoy"
  1 -> names[0]
  else -> names.dropLast(1).joinToString(", ") + " y " + names.last()
}
