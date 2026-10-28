package com.example.ui.screens

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.data.AchievementFamily
import com.example.data.glueNumbers
import com.example.data.AchievementStats
import com.example.data.Achievements
import com.example.data.FamilyMedalState
import com.example.data.LeagueRow
import com.example.data.goalText
import com.example.ui.components.FamilyMedal
import com.example.ui.components.Glyph
import com.example.ui.components.KitCloseButton
import com.example.ui.components.KitGlyph
import com.example.ui.components.OnBoardText
import com.example.ui.components.ShieldImage
import com.example.ui.components.SlabSize
import com.example.ui.components.SpaceSectionTitle
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayCard

private val OnNight = androidx.compose.ui.graphics.Color(0xFFEAF0FF)
private val OnNightDim = com.example.ui.theme.NubiTokens.OnNightDim

/**
 * Lo que queda de las secciones de Avance (Tarea 159): las casillas de medallas y el detalle de una familia (las usa «Tú»; Avance ya no las lleva) y la fila de la liga (oculta con `Features.LEAGUES`). Las filas por área, «Tus mejores marcas» y la línea de la semana viven en `AvanceAreas.kt`;
 * la página de un área, en `AvanceAreaPage.kt`. Los textos secundarios van a 16 sp.
 */

/** Cuántas medallas caben por fila sin partir la palabra más larga («Constancia»): con letra grande, menos columnas. */
@Composable
private fun medalColumns(maxWidth: androidx.compose.ui.unit.Dp): Int {
  val needed = 104.dp * LocalDensity.current.fontScale
  return (3 downTo 1).firstOrNull { maxWidth / it - 8.dp >= needed } ?: 1
}

/** Las casillas de las familias (las usa también «Tú»). */
@Composable
internal fun FamilyMedalCards(states: List<FamilyMedalState>, onTap: (AchievementFamily) -> Unit) {
  BoxWithConstraints(Modifier.fillMaxWidth()) {
    val cols = medalColumns(maxWidth)
    Column {
      states.chunked(cols).forEach { row ->
        Row(Modifier.fillMaxWidth().height(IntrinsicSize.Min).padding(bottom = 4.dp)) {
          row.forEach { st ->
            ClayCard(
              modifier = Modifier.weight(1f).fillMaxHeight().padding(horizontal = 2.dp).semantics(mergeDescendants = true) { contentDescription = st.spoken }.testTag("medal_${st.family.name.lowercase()}"),
              size = SlabSize.SMALL, onClick = { onTap(st.family) }, contentPadding = 0.dp
            ) {
              Column(Modifier.fillMaxWidth().padding(horizontal = 4.dp, vertical = 8.dp), horizontalAlignment = Alignment.CenterHorizontally) {
                FamilyMedal(st, 56.dp)
                Text(st.family.title, color = if (st.unlocked) Clay.Ink else Clay.InkMuted, fontSize = 16.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center, lineHeight = 20.sp, modifier = Modifier.padding(top = 4.dp))
                st.captionLines.forEach { Text(it.glueNumbers(), color = Clay.InkMuted, fontSize = 16.sp, textAlign = TextAlign.Center, lineHeight = 20.sp) }
              }
            }
          }
          repeat(cols - row.size) { Spacer(Modifier.weight(1f)) }
        }
      }
    }
  }
}

/** El detalle de una familia de medallas: la medalla grande con su número, qué es, y cada nivel (con su ✓ y cuándo, o cuánto llevas hacia el siguiente). */
@Composable
internal fun FamilyMedalDialog(state: FamilyMedalState, unlocks: Map<String, Long>, stats: AchievementStats, onDismiss: () -> Unit) {
  Dialog(onDismissRequest = onDismiss) {
    ClayCard(Modifier.fillMaxWidth().padding(8.dp).testTag("medal_dialog"), color = Clay.Cream, size = SlabSize.LARGE, contentPadding = 22.dp) {
      Column(Modifier.fillMaxWidth(), horizontalAlignment = Alignment.CenterHorizontally) {
        FamilyMedal(state, 120.dp)
        Text(state.family.title, color = Clay.Ink, fontSize = 24.sp, fontWeight = FontWeight.Bold, fontFamily = com.example.ui.theme.TitleFamily, modifier = Modifier.padding(top = 8.dp))
        Text(state.family.blurb, color = Clay.InkMuted, fontSize = 16.sp, textAlign = TextAlign.Center, lineHeight = 21.sp)
        Spacer(Modifier.height(10.dp))
        Achievements.levelsOf(state.family).forEach { def ->
          val at = unlocks[def.id]
          Row(Modifier.fillMaxWidth().heightIn(min = 36.dp).padding(vertical = 2.dp), verticalAlignment = Alignment.CenterVertically) {
            Box(Modifier.size(24.dp), contentAlignment = Alignment.Center) { if (at != null) KitGlyph(Glyph.CHECK, size = 22.dp) }
            Spacer(Modifier.width(8.dp))
            Column(Modifier.weight(1f)) {
              Text(def.goalText(), color = Clay.Ink, fontSize = 16.sp, fontWeight = if (at != null) FontWeight.Bold else FontWeight.Normal)
              if (at != null) Text("Conseguida el ${java.text.SimpleDateFormat("d 'de' MMMM yyyy", java.util.Locale("es")).format(java.util.Date(at))}", color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 20.sp)
              else if (def.id == state.next?.id) state.progress?.let { (cur, goal) -> Text("Llevas $cur de $goal", color = Clay.InkMuted, fontSize = 16.sp) }
            }
          }
        }
        Spacer(Modifier.height(10.dp))
        KitCloseButton(onDismiss, Modifier.testTag("medal_dialog_close"))
      }
    }
  }
}

/** Tu liga en UNA fila: el escudo chico, «Liga Plata · 1.240 trofeos», cuánto falta para la siguiente y Compartir (el escudo grande queda en el Perfil y en la celebración de ascenso). */
@Composable
internal fun AvanceLeague(row: LeagueRow, onShare: () -> Unit) {
  Column {
    SpaceSectionTitle("Tu liga")
    Spacer(Modifier.height(10.dp))
    // Con letra normal en un teléfono angosto el título no deja sitio al botón sin partirse («37 / trofeos»): Compartir baja a su propia línea.
    BoxWithConstraints(Modifier.fillMaxWidth()) {
    val large = LocalDensity.current.fontScale > 1.15f || maxWidth < 420.dp
    Column {
    Row(Modifier.fillMaxWidth().testTag("avance_league"), verticalAlignment = Alignment.CenterVertically) {
      ShieldImage(row.tier, 40.dp)
      Spacer(Modifier.width(12.dp))
      Column(Modifier.weight(1f)) {
        OnBoardText(row.title, color = OnNight, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp)
        if (row.toNext != null) OnBoardText(row.toNext, color = OnNightDim, fontSize = 16.sp, lineHeight = 21.sp)
        else if (row.tier.ordinal == 0) OnBoardText("Cada partida suma", color = OnNightDim, fontSize = 16.sp, lineHeight = 21.sp)
      }
      if (!large) {
        Spacer(Modifier.width(8.dp))
        ClayButton(text = "Compartir", color = Clay.Cream, glyph = Glyph.SHARE, wide = false, onClick = onShare, modifier = Modifier.testTag("btn_share_league"))
      }
    }
    if (large) ClayButton(text = "Compartir", color = Clay.Cream, glyph = Glyph.SHARE, wide = false, onClick = onShare, modifier = Modifier.padding(top = 8.dp).testTag("btn_share_league"))
    }
    }
  }
}
