package com.example.ui.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.testTag
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.data.Comparison
import com.example.data.League
import com.example.data.glueNumbers
import com.example.ui.components.DivisionEmblem
import com.example.ui.components.DivisionEmblemHero
import com.example.ui.components.SlabSize
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayCard
import com.example.ui.theme.TitleFamily
import com.example.viewmodel.NeuroVidaViewModel

/**
 * Quien aloja la HOJA DEL LUNES (Tarea 229; `docs/liga-de-la-semana.md` §5.3): sale UNA vez por semana cerrada, la primera vez que se abre la app después del cierre (si se cerraron varias, habla solo de la última), con Hoy a la vista, el arranque terminado y sin nada encima (ni la guía de Nubi, ni una medalla, ni un final).
 * Lo que pasó lo guarda la liga ([League.State.pendingSheet]); al «Seguir» queda vista y no vuelve. [somethingAbove] es verdadero si Hoy no está a la vista o hay algo encima.
 */
@Composable
internal fun LeagueSheetHost(viewModel: NeuroVidaViewModel, somethingAbove: Boolean) {
  val league by viewModel.league.collectAsState()
  val stage by viewModel.startupStage.collectAsState()
  val guide by viewModel.guideStep.collectAsState()
  val compareWorld by viewModel.compareWorld.collectAsState()
  val estimatedReference by viewModel.estimatedReference.collectAsState()
  val closed = league.pendingSheet
  if (closed == null || somethingAbove || guide != null || stage != com.example.viewmodel.StartupStage.READY) return
  val estimated = remember(compareWorld, estimatedReference) { Comparison.kindOf(Comparison.sourceFor(com.example.BuildConfig.DEBUG, compareWorld, estimatedReference)) != Comparison.Kind.PEOPLE }
  LeagueSheet(League.sheet(closed, estimated), onClose = { viewModel.dismissLeagueSheet(closed.weekStart) })
}

/** La hoja dentro de su ventana; el contenido ([LeagueSheetContent]) se prueba suelto, porque un `Dialog` ignora la densidad de la prueba. */
@Composable
internal fun LeagueSheet(sheet: League.Sheet, onClose: () -> Unit) {
  Dialog(onDismissRequest = onClose) { LeagueSheetContent(sheet, onClose) }
}

/**
 * El CONTENIDO de la hoja del lunes: el emblema de la división con que empieza la semana (52 dp, decorativo), el título (¡Subiste a Sobre las nubes! / Sigues en Despegando / Esta semana estás En la plataforma), la frase de la semana pasada, si el % alcanzaba para más de una división «Alcanzaba para En Marte, pero se sube de a una división por semana.»,
 * lo de esta semana («con el 30 % subes a En órbita; con el 20 %, te quedas Sobre las nubes») y el botón «Seguir». Solo SUBIR celebra, y con palabras (el «¡»): quedarse o bajar no festejan ni dicen «bajaste» ni «perdiste», y una semana sin juego dice que no se jugó, sin culpa. Se desliza: con letra 2,0 puede no caber entera.
 * Lo que lee el lector de pantalla es la hoja entera de un tirón ([League.sheetSpoken]).
 */
@Composable
internal fun LeagueSheetContent(sheet: League.Sheet, onClose: () -> Unit) {
  ClayCard(Modifier.fillMaxWidth().padding(8.dp).testTag("league_sheet"), color = Clay.Cream, size = SlabSize.LARGE, contentPadding = 20.dp) {
    Column(Modifier.fillMaxWidth().verticalScroll(androidx.compose.foundation.rememberScrollState()), verticalArrangement = Arrangement.spacedBy(12.dp), horizontalAlignment = Alignment.CenterHorizontally) {
      DivisionEmblem(sheet.closed.to, DivisionEmblemHero, Modifier.clearAndSetSemantics { testTag = "league_sheet_emblem" })
      // el texto se lee de un tirón; el botón queda aparte (con el texto fusionado, el lector no lo encontraría como botón)
      Column(Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = League.sheetSpoken(sheet) }, verticalArrangement = Arrangement.spacedBy(12.dp)) {
        Text(sheet.title.glueNamesForLetter(), color = Clay.Ink, fontSize = 22.sp, fontWeight = androidx.compose.ui.text.font.FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 28.sp, textAlign = TextAlign.Center, modifier = Modifier.fillMaxWidth().semantics { heading() }.testTag("league_sheet_title"))
        Text(sheet.last.glueNumbers().glueNamesForLetter(), color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.fillMaxWidth().testTag("league_sheet_last"))
        sheet.extra?.let { Text(it.glueNamesForLetter(), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.fillMaxWidth().testTag("league_sheet_extra")) }
        Text(sheet.now.glueNumbers().glueNamesForLetter(), color = Clay.Ink, fontSize = 16.sp, fontWeight = androidx.compose.ui.text.font.FontWeight.SemiBold, lineHeight = 22.sp, modifier = Modifier.fillMaxWidth().testTag("league_sheet_now"))
      }
      Spacer(Modifier.height(4.dp))
      ClayButton(text = League.SHEET_OK, onClick = onClose, modifier = Modifier.testTag("btn_league_sheet_ok"))
    }
  }
}
