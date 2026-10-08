package com.example.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.model.DomainType
import com.example.model.GameRegistry

/**
 * Datos de la pantalla de Progreso basados en el rating del DDA común (0..1 por juego, ver docs/DDA-comun.md). Es un indicador interno de avance, no una medida clínica validada.
 * (8-oct: se quitaron la campana con el percentil y el radar del «perfil cognitivo»: no se muestran percentiles ni comparaciones con una referencia supuesta.)
 */

/** Rating 0..1 por juego; null = todavía sin dato. */
typealias GameLevels = Map<String, Float?>

fun domainScores(levels: GameLevels): Map<DomainType, Float?> =
  DomainType.values().associateWith { d ->
    val vals = GameRegistry.allGames.filter { it.domain == d }.mapNotNull { levels[it.id] }
    if (vals.isEmpty()) null else vals.average().toFloat()
  }

/** Índice general 0..100: promedio de los juegos con dato; null si no hay ninguno. */
fun overallIndex(levels: GameLevels): Int? {
  val vals = levels.values.filterNotNull()
  return if (vals.isEmpty()) null else (vals.average() * 100).toInt().coerceIn(0, 100)
}

/** La etapa del avance (Inicio…Maestro, data/Skill.kt): los mismos nombres en toda la app. */
fun levelWord(v: Float): String = com.example.data.Skill.stageName(v)

/** Cada área con su etapa («Aprendiz», «Avanzado»…): solo la palabra de la etapa, sin percentiles. */
@Composable
fun DomainLegend(levels: GameLevels, modifier: Modifier = Modifier) {
  val scores = domainScores(levels)
  Column(modifier = modifier, verticalArrangement = Arrangement.spacedBy(6.dp)) {
    DomainType.values().forEach { d ->
      val v = scores[d]
      Row(verticalAlignment = Alignment.CenterVertically) {
        Box(Modifier.size(10.dp).clip(CircleShape).background(d.color))
        Spacer(Modifier.width(8.dp))
        Text(d.displayName, style = MaterialTheme.typography.labelLarge, modifier = Modifier.weight(1f))
        Text(
          text = if (v == null) "Sin medir" else levelWord(v),
          style = MaterialTheme.typography.labelMedium,
          color = if (v == null) MaterialTheme.colorScheme.onSurfaceVariant else d.color,
          fontWeight = FontWeight.SemiBold
        )
      }
    }
  }
}
