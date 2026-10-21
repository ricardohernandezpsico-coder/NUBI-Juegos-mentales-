package com.example.ui.screens

import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
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
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.clickable
import androidx.compose.ui.draw.clip
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
import com.example.data.AchievementStats
import com.example.data.Achievements
import com.example.data.AreaProgress
import com.example.data.AreaStatus
import com.example.data.Discovery
import com.example.data.DiscoveryKind
import com.example.data.DiscoveryNudge
import com.example.data.FamilyMedalState
import com.example.data.LeagueRow
import com.example.data.Skill
import com.example.data.StarMeasures
import com.example.data.WeekSummary
import com.example.data.Week
import com.example.data.goalText
import com.example.data.measureText
import com.example.model.GameRegistry
import com.example.ui.components.FamilyMedal
import com.example.ui.components.Glyph
import com.example.ui.components.KitBar
import com.example.ui.components.KitCloseButton
import com.example.ui.components.KitGlyph
import com.example.ui.components.OnBoardText
import com.example.ui.components.ShieldImage
import com.example.ui.components.NubiBubble
import com.example.ui.components.SlabSize
import com.example.ui.components.SpaceSectionTitle
import com.example.ui.components.WeekLampRendered
import com.example.ui.components.areaDescription
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayCard
import com.example.ui.theme.ClayPill

private val OnNight = androidx.compose.ui.graphics.Color(0xFFEAF0FF)
private val OnNightDim = com.example.ui.theme.NubiTokens.OnNightDim

/**
 * Las secciones de Avance (Tarea 125; `auditoria-fable/spec-avance-juegos.md` §1): tu semana, tus áreas, lo que descubriste, tus medallas, tu liga y las últimas partidas. Cada una es una función suelta con sus datos ya armados (los arma `ProgressScreen`
 * con lógica pura de `data/`), para poder fotografiarlas y probarlas aparte. Los textos secundarios van a 16 sp; el único número grande de la pantalla es el de la semana.
 */

/** Tu semana: las 7 farolas del kit con «hoy», «3 de 4 días» en grande, la racha con su línea de perdón y las áreas de la semana. */
@Composable
internal fun AvanceWeek(summary: WeekSummary, lamps: Week.Lamps, streakNote: String, nubiLine: String, anchor: NubiAnchor? = null) {
  Column(Modifier.testTag("avance_week")) {
    SpaceSectionTitle("Tu semana")
    Spacer(Modifier.height(10.dp))
    Row(Modifier.semantics { contentDescription = Week.lampsSpoken(lamps.daysPlayed) }.testTag("avance_lamps"), horizontalArrangement = Arrangement.spacedBy(2.dp)) {
      lamps.played.forEachIndexed { i, on -> WeekLampRendered(on = on, today = i == lamps.todayIndex) }
    }
    Spacer(Modifier.height(10.dp))
    if (summary.empty) {
      OnBoardText(summary.big, color = OnNight, fontSize = 28.sp, fontWeight = FontWeight.Bold, lineHeight = 32.sp, modifier = Modifier.testTag("avance_week_big"))
      OnBoardText(summary.areasLine, color = OnNightDim, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 4.dp).testTag("avance_week_invite"))
      NubiWeekComment(nubiLine, anchor)
      return@Column
    }
    OnBoardText(summary.big, color = OnNight, fontSize = 34.sp, fontWeight = FontWeight.Bold, lineHeight = 38.sp, modifier = Modifier.testTag("avance_week_big"))
    OnBoardText(summary.rest.trim(), color = OnNightDim, fontSize = 16.sp, lineHeight = 21.sp)
    if (summary.streakPill != null) {
      Spacer(Modifier.height(10.dp))
      ClayPill(summary.streakPill, Modifier.testTag("avance_streak_pill"), color = Clay.Cream, leading = { com.example.ui.components.KitLlama(lit = true, size = 20.dp) })       // crema: una gota sol no se vería sobre una pastilla sol
      // Si Nubi ya comenta el descanso («Descansaste el viernes y seguiste: eso es constancia.»), la línea suelta con lo mismo sobra (Tarea 143, B2)
      if (!nubiLine.startsWith("Descansaste")) OnBoardText(streakNote, color = OnNightDim, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 4.dp).testTag("streak_note"))
    }
    OnBoardText(summary.areasLine, color = OnNightDim, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 8.dp).testTag("avance_areas_week"))
    NubiWeekComment(nubiLine, anchor)
  }
}

/** Nubi comenta tu semana (Tarea 131, A6): Nubi de 56 dp a la izquierda y su globo con UNA frase (`nubiWeekLine`), como la ventana de un área en Juegos pero más chico: el comentario acompaña, no manda. */
@Composable
private fun NubiWeekComment(line: String, anchor: NubiAnchor? = null) {
  if (line.isBlank()) return                                                   // sin frase no hay globo (la hoja «hoy» de la Tarea 131 lo usa)
  Row(Modifier.fillMaxWidth().padding(top = 14.dp).testTag("avance_nubi"), verticalAlignment = Alignment.CenterVertically) {
    com.example.ui.components.NubiClay(com.example.ui.components.NubiSize.S, modifier = Modifier.padding(end = 6.dp).let { if (anchor != null) it.markNubi(anchor) else it })       // la lámina de un área vuela desde esta Nubi (Tarea 155)
    NubiBubble(text = line, tailLeft = true, modifier = Modifier.weight(1f).testTag("avance_nubi_line"))
  }
}

/** Tus áreas: 4 tarjetas crema tocables con su nombre, su etapa, el canal con el churro, la línea del cambio y, si no hay medida, la invitación. */
@Composable
internal fun AvanceAreas(statuses: List<AreaStatus>, names: Map<String, String>, onArea: (String) -> Unit) {
  Column {
    SpaceSectionTitle("Tus áreas", hint = "Toca un área para ver su detalle")
    Spacer(Modifier.height(8.dp))
    Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
      statuses.forEach { s ->
        val name = names[s.area] ?: s.area
        ClayCard(
          modifier = Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = areaDescription(name, s) }.testTag("progress_area_${s.area.lowercase()}"),
          onClick = { onArea(s.area) }, contentPadding = 0.dp
        ) {
          Column(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 12.dp)) {
            Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
              Text(name, color = Clay.Ink, fontSize = 19.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.weight(1f))
              Spacer(Modifier.width(8.dp))
              Text(s.value?.let { Skill.stageName(it) } ?: "Aún sin medir", color = Clay.InkMuted, fontSize = 16.sp)
            }
            KitBar(s.value, s.change, Modifier.padding(top = 8.dp, bottom = 2.dp), onCard = true)
            val line = if (s.value == null) "Juega un juego de $name y aquí verás tu avance" else AreaProgress.cardLine(s)
            if (line != null) Text(line, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 6.dp).testTag("area_line_${s.area.lowercase()}"))
          }
        }
      }
    }
  }
}

/** Lo que dice una tarjeta de «Lo que descubriste»: el valor en palabras (Freno y Tinta o Palabra, en zonas y palabras, nunca en milisegundos). */
internal fun discoveryValue(d: Discovery, measures: List<com.example.data.MeasurePoint>): String =
  StarMeasures.zoneText(d.def, measures) ?: measureText(d.def, d.values.last())

/**
 * Lo que descubriste: de una a tres tarjetas con tus marcas («Tu vistazo», «4 de 4 cápsulas», «Tu récord»), comparadas solo contigo; tocar una abre la ficha de su juego. Sin marcas todavía, una sola tarjeta: la invitación a jugar el juego al que le faltan menos partidas.
 */
@Composable
internal fun AvanceDiscoveries(
  cards: List<Discovery>, nudge: DiscoveryNudge?, measures: List<com.example.data.MeasurePoint>,
  onGame: (String) -> Unit, onPlay: (String) -> Unit
) {
  Column {
    SpaceSectionTitle("Tus medidas", hint = "Comparadas solo contigo")                // «Tus medidas» (Tarea 150, B1.5; antes «Lo que descubriste» y «Tus marcas»)
    Spacer(Modifier.height(8.dp))
    Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
      cards.forEach { d ->
        ClayCard(Modifier.fillMaxWidth().testTag("discovery_${d.def.key}"), onClick = { onGame(d.def.gameId) }, contentPadding = 0.dp) {
          Column(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 12.dp)) {
            Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
              Text(d.def.short.replaceFirstChar { it.uppercase() }, color = Clay.Ink, fontSize = 19.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.weight(1f))
              Spacer(Modifier.width(8.dp))
              when (d.kind) {
                DiscoveryKind.RECORD -> ClayPill("Tu récord", color = Clay.Lime, compact = true)
                DiscoveryKind.IMPROVING -> ClayPill("Mejorando", color = Clay.Sun, compact = true)
                DiscoveryKind.STEADY -> ClayPill("Se mantiene", color = Clay.Cream, compact = true)
              }
            }
            Text(discoveryValue(d, measures), color = Clay.Ink, fontSize = 24.sp, fontWeight = FontWeight.Bold, lineHeight = 28.sp, modifier = Modifier.padding(top = 4.dp))
            Text(d.caption, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 2.dp))
          }
        }
      }
      if (cards.isEmpty() && nudge != null) {
        val title = StarMeasures.gameNames[nudge.def.gameId] ?: nudge.def.gameId
        val times = if (nudge.remaining == 1) "1 vez" else "${nudge.remaining} veces"
        ClayCard(Modifier.fillMaxWidth().testTag("discovery_nudge"), contentPadding = 0.dp) {
          Column(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 12.dp)) {
            Text("Juega $title $times más y aquí aparece ${nudge.def.short}.", color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, lineHeight = 23.sp)
            Spacer(Modifier.height(10.dp))
            ClayButton(text = "Jugar", onClick = { onPlay(nudge.def.gameId) }, glyph = Glyph.PLAY, wide = false, modifier = Modifier.testTag("btn_discovery_play"))
          }
        }
      }
    }
  }
}

/** Cuántas medallas caben por fila sin partir la palabra más larga («Constancia»): con letra grande, menos columnas. */
@Composable
private fun medalColumns(maxWidth: androidx.compose.ui.unit.Dp): Int {
  val needed = 104.dp * LocalDensity.current.fontScale
  return (3 downTo 1).firstOrNull { maxWidth / it - 8.dp >= needed } ?: 1
}

/**
 * Las medallas en Avance: UNA línea que lleva al Perfil, donde viven (Tarea 152, B6.1: antes las tres tarjetas enteras aquí y en el Perfil, con el mismo título): «9 medallas · ver en tu perfil».
 */
@Composable
internal fun AvanceMedalsLink(count: Int, onClick: () -> Unit) {
  val text = when (count) {
    0 -> "Aún sin medallas · ver en tu perfil"
    1 -> "1 medalla · ver en tu perfil"
    else -> "$count medallas · ver en tu perfil"
  }
  Row(
    Modifier.fillMaxWidth().clip(RoundedCornerShape(10.dp)).clickable(role = androidx.compose.ui.semantics.Role.Button, onClick = onClick).heightIn(min = 48.dp).testTag("avance_medals_link"),
    verticalAlignment = Alignment.CenterVertically
  ) {
    OnBoardText(text, color = OnNight, fontSize = 18.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f, fill = false))
    Spacer(Modifier.width(8.dp))
    androidx.compose.foundation.Canvas(Modifier.size(width = 10.dp, height = 16.dp)) {
      val p = androidx.compose.ui.graphics.Path().apply { moveTo(size.width * 0.15f, size.height * 0.1f); lineTo(size.width * 0.85f, size.height * 0.5f); lineTo(size.width * 0.15f, size.height * 0.9f) }
      drawPath(p, OnNight, style = androidx.compose.ui.graphics.drawscope.Stroke(2.dp.toPx(), cap = androidx.compose.ui.graphics.StrokeCap.Round, join = androidx.compose.ui.graphics.StrokeJoin.Round))
    }
  }
}

/** Las casillas de las familias (las usa también el Perfil). */
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
                st.captionLines.forEach { Text(it, color = Clay.InkMuted, fontSize = 16.sp, textAlign = TextAlign.Center, lineHeight = 20.sp) }
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
        Text(state.family.title, color = Clay.Ink, fontSize = 24.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 8.dp))
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
