package com.example.games

import androidx.activity.compose.BackHandler
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.spring
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialStats
import com.example.model.GameRegistry
import com.example.ui.components.DetailSection
import com.example.ui.components.DetailText
import com.example.ui.components.Glyph
import com.example.ui.components.MeaningfulResultModel
import com.example.ui.components.OnBoardText
import com.example.ui.components.ResultHeadline
import com.example.ui.components.ResultNote
import com.example.ui.components.ResultPhrase
import com.example.ui.components.rememberReduceMotion
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.AppFamily
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.NubiTokens
import kotlinx.coroutines.delay

/** El alto de la barra más larga de «Tus últimas partidas» y el de la más corta (una partida con 0 aciertos sigue viéndose). */
internal val TimeTrialBarMax = 96.dp
internal val TimeTrialBarMin = 8.dp

/**
 * Lo que dice el final del Contrarreloj, ya armado (así la pantalla y las pruebas leen lo mismo): el nombre del juego, la marca con su unidad, el sello si hubo marca nueva, la frase solo contigo, el paso de la pista, las últimas 5 partidas con la mejor de la semana y la nota.
 */
internal data class TimeTrialFinalUi(
  val title: String,
  val headline: String,
  val unit: String,
  val seal: String?,
  val phrase: String,
  val step: String,
  /** Las marcas de las últimas partidas con la de hoy al final; vacío la primera vez (no se dibuja: no hay con qué comparar). */
  val last: List<Int>,
  val weekLine: String?,
  val note: String,
  val detail: List<String>,
  val spoken: String
)

internal fun timeTrialFinalUi(o: TimeTrialStats.Outcome, title: String): TimeTrialFinalUi = TimeTrialFinalUi(
  title = title,
  headline = TimeTrialDay.hitsText(o.result.mark),
  unit = TimeTrialStats.scoreLine(o.result),
  seal = if (o.isRecord) TimeTrialStats.SEAL else null,
  phrase = TimeTrialStats.phrase(o),
  step = TimeTrialStats.stepLine(o.result.level, o.maxLevel),
  last = if (o.isFirst) emptyList() else o.last,
  weekLine = TimeTrialStats.weekLine(o),
  note = TimeTrialStats.NOTE,
  detail = TimeTrialStats.detailLines(o),
  spoken = TimeTrialStats.spoken(title, o)
)

/**
 * El final del Contrarreloj (Tarea 177; `auditoria-fable/contrarreloj.md` §4): la misma forma del final «B», de arriba abajo: la placa del juego («Atención · Contrarreloj de hoy» si es el juego del día; «Atención · Contrarreloj» si se jugó desde la ficha de otro: Tarea 181), el sello «¡Tu mejor marca hasta hoy!» solo con marca nueva, la cifra («38 aciertos») y su unidad
 * («en 2 minutos de Contrarreloj»), UNA frase solo contigo (con marca nueva, sin ella o la primera vez), «Llegaste al paso 4 de 5 de la pista.», «Tus últimas partidas» (cinco barras con su cifra encima, la de hoy en sol y marcada «hoy») y «La mejor de esta semana», «Otra vez» (sol: el
 * Contrarreloj se juega para repetirse), «Listo», «Ver el detalle de tu partida» y la nota («Es el resultado de un juego, comparado solo contigo. No es una medida de capacidad.»). Sin «¿Por qué importa?»: es el resultado de un juego, no de una medida. Nunca percentiles ni otras personas.
 */
@Composable
fun TimeTrialResultScreen(
  outcome: TimeTrialStats.Outcome,
  onAgain: () -> Unit,
  onDone: () -> Unit,
  modifier: Modifier = Modifier,
  /** «Efectos de sonido» de Ajustes: con false, la marca nueva no suena (sí vibra, si la vibración está encendida). */
  soundEnabled: Boolean = true
) {
  val gameDef = GameRegistry.getById(outcome.result.gameId)
  val lang = com.example.ui.i18n.LocalAppLanguage.current
  val title = gameDef?.let { getGameTitle(it.id, lang, it.title) } ?: "Contrarreloj"
  val ui = remember(outcome, title) { timeTrialFinalUi(outcome, title) }
  val context = LocalContext.current
  val reduceMotion = rememberReduceMotion()
  val plaquePop = remember(outcome) { Animatable(if (reduceMotion) 1f else 0.82f) }
  BackHandler(onBack = onDone)
  LaunchedEffect(outcome) {
    // la placa entra con su rebote; la marca nueva suena CUANDO TERMINA ese rebote (como en el final de siempre)
    if (!reduceMotion) plaquePop.animateTo(1f, spring(dampingRatio = 0.55f, stiffness = 380f)) else delay(150L)
    if (outcome.isRecord && soundEnabled) com.example.ui.components.RecordSound.play(context)
  }
  // la cifra y la unidad reutilizan las piezas del final «B»; el resto del modelo no se dibuja aquí
  val model = remember(ui) {
    MeaningfulResultModel(
      title = "", boxTitle = "", headline = ui.headline, headlineUnit = ui.unit, dataLine = null, summaryLine = null, progressTitle = "", chips = emptyList(), progressPhrase = ui.phrase,
      trickTitle = "", trick = "", whyTitle = "", why = "", source = "", note = ui.note, spoken = ui.spoken
    )
  }

  Column(
    modifier = modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 24.dp).padding(top = 20.dp, bottom = 32.dp).testTag("timetrial_result"),
    horizontalAlignment = Alignment.Start
  ) {
    ResultPlaque(outcome.result.gameId, ui.title, (gameDef?.domain?.displayName?.let { "$it · " } ?: "") + TimeTrialDay.nameFor(outcome.result.gameId, outcome.result.day), gameDef?.domain?.color ?: Clay.Sky, plaquePop)
    ui.seal?.let { RecordLine(it) }
    Column(Modifier.semantics(mergeDescendants = true) { contentDescription = ui.spoken }) {
      ResultHeadline(model)
      ResultPhrase(model)
      OnBoardText(
        text = ui.step, color = NubiTokens.OnNightDim, fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = 16.sp, lineHeight = 22.sp,
        modifier = Modifier.fillMaxWidth().padding(top = 8.dp).testTag("timetrial_step")
      )
    }
    if (ui.last.isNotEmpty()) {
      LastGames(ui.last, ui.weekLine)
    }
    Spacer(Modifier.height(24.dp))
    ClayButton(text = TimeTrialStats.AGAIN, onClick = onAgain, glyph = Glyph.CLOCK, modifier = Modifier.testTag("btn_timetrial_again"))
    Spacer(Modifier.height(14.dp))
    ClayButton(text = TimeTrialStats.DONE, onClick = onDone, color = Clay.Cream, glyph = Glyph.CHECK, modifier = Modifier.testTag("btn_timetrial_done"))
    DetailSection {
      ui.detail.forEachIndexed { i, line -> DetailText(line, "timetrial_detail_$i", strong = true) }
    }
    ResultNote(model)
  }
}

/** «Tus últimas partidas»: cinco barras (o las que haya) con su cifra encima, la más nueva a la derecha, en sol y marcada «hoy» (no solo por el color); debajo, «La mejor de esta semana: 41 aciertos.». */
@Composable
private fun LastGames(marks: List<Int>, weekLine: String?) {
  val most = (marks.maxOrNull() ?: 1).coerceAtLeast(1)
  val spoken = TimeTrialStats.LAST_TITLE + ": " + marks.joinToString(", ") { TimeTrialDay.hitsText(it) } + ". La última es la de hoy."
  Column(Modifier.fillMaxWidth().padding(top = 20.dp).testTag("timetrial_last")) {
    OnBoardText(
      text = TimeTrialStats.LAST_TITLE, color = Clay.Cream, fontFamily = com.example.ui.theme.TitleFamily, fontWeight = FontWeight.Bold, fontSize = 18.sp, lineHeight = 24.sp,
      modifier = Modifier.padding(start = 4.dp, bottom = 8.dp)
    )
    Row(
      Modifier.fillMaxWidth().semantics { contentDescription = spoken },
      horizontalArrangement = Arrangement.spacedBy(10.dp), verticalAlignment = Alignment.Bottom
    ) {
      marks.forEachIndexed { i, n ->
        val today = i == marks.lastIndex
        Column(Modifier.weight(1f).testTag("timetrial_bar_$i").clearAndSetSemantics { }, horizontalAlignment = Alignment.CenterHorizontally) {
          // la cifra encima: 16 sp (rótulo de gráfico), la de hoy en sol y en negrita
          Text(n.toString(), color = if (today) com.example.ui.theme.NubiTokens.SunOnNight else NubiTokens.OnNightDim, fontSize = 16.sp, fontWeight = if (today) FontWeight.Bold else FontWeight.SemiBold, fontFamily = AppFamily, textAlign = TextAlign.Center)
          val h = TimeTrialBarMin + (TimeTrialBarMax - TimeTrialBarMin) * (n.toFloat() / most)
          Box(
            Modifier.padding(top = 4.dp).width(36.dp).height(h)
              .clip(RoundedCornerShape(topStart = 8.dp, topEnd = 8.dp)).background(if (today) Clay.Sun else Clay.Grape)
              .border(2.dp, Clay.Ink, RoundedCornerShape(topStart = 8.dp, topEnd = 8.dp))
          )
          // «hoy» bajo la barra de hoy; las otras, en blanco: todas ocupan el mismo alto
          Text(if (today) "hoy" else " ", color = if (today) com.example.ui.theme.NubiTokens.SunOnNight else NubiTokens.OnNightDim, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, fontFamily = AppFamily, modifier = Modifier.padding(top = 2.dp))
        }
      }
      // si hay menos de 5, el resto del ancho queda vacío a la derecha: las barras no se estiran
      repeat(TimeTrialStats.LAST_N - marks.size) { Spacer(Modifier.weight(1f)) }
    }
    weekLine?.let {
      OnBoardText(text = it, color = NubiTokens.OnNightDim, fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.fillMaxWidth().padding(top = 10.dp).testTag("timetrial_week"))
    }
  }
}
