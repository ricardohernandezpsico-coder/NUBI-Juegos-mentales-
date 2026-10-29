package com.example.ui.components

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.AnimationVector1D
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.role
import androidx.compose.ui.semantics.stateDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.FinalesConSentido
import com.example.ui.theme.AppFamily
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayCard
import com.example.ui.theme.ClayPill
import com.example.ui.theme.NubiTokens

/** Un número del antiguo «Tu avance» (tres cuadros). Desde el final «B» (Tarea 146) ya no se dibujan: los dos números van dentro de la frase; el modelo trae la lista vacía. */
data class ResultChip(val label: String, val value: String, val highlight: Boolean = false)

/**
 * Lo que se ve en un final CON SENTIDO, en su versión «B» (Tarea 146; `auditoria-fable/spec-final-B.md`): UNA cifra y su unidad, UNA frase de avance solo contigo (con los dos números adentro; nunca percentiles ni otras personas), los avisos cuando pasan y el truco.
 * Después van los botones (los pone la pantalla: [ResultFrame.actions]), «Ver el detalle de tu partida» (el desglose que cada juego ya mostraba: nada se borra), «¿Por qué importa?» con su fuente solo en las 3 primeras partidas de cada juego
 * (desde la cuarta vive en el detalle y en la ficha del juego) y la nota de siempre («Medida de esta partida. No es un diagnóstico.»).
 * Todo texto suelto va con la orilla navy de [OnBoardText]; nada se dice solo con color y nada va bajo 16 sp.
 */
data class MeaningfulResultModel(
  val title: String,
  val boxTitle: String,
  val headline: String,
  val headlineUnit: String,
  val dataLine: String?,
  val summaryLine: String?,
  val extraLine: String? = null,
  val progressTitle: String,
  /** Vacía desde el final «B»: los dos números van dentro de [progressPhrase]. */
  val chips: List<ResultChip>,
  /** La frase de avance con los números adentro; vacía si hoy no hay medida (la unidad ya dice por qué). */
  val progressPhrase: String,
  val trickTitle: String,
  val trick: String,
  val whyTitle: String,
  val why: String,
  val source: String,
  val note: String,
  /** Lo que lee en voz alta un lector de pantalla: el juego, la cifra con su unidad, la frase de avance, los avisos y el truco. */
  val spoken: String = "",
  /**
   * El sello del récord (Tarea 127): UNA sola vez por pantalla, arriba, con la cifra cuando la hay («¡Récord nuevo: 31 cápsulas!»); null si no hay récord. Nunca en la primera partida de un juego (no hay nada que superar). Si hay sello, la frase de avance no lo repite.
   */
  val recordSeal: String? = null,
  /** Lo que esta partida anuncia y es nuevo («Modo nuevo: En marcha», «¡Despegó tu cohete n.º 3!»): sale como aviso (una píldora) bajo la frase de avance. */
  val notices: List<String> = emptyList()
)

/** Un aviso del final (una píldora): lima si celebra o anuncia algo nuevo, crema si solo informa. [pop] = el rebote de «¡Subiste de etapa!» (con «quitar animaciones», ya quieto). */
class ResultNotice(val text: String, val lime: Boolean = text.startsWith("¡"), val pop: Animatable<Float, AnimationVector1D>? = null)

/**
 * Lo que la pantalla de resultado le entrega al final: el puntaje (va primero en el detalle), los avisos que no son del juego (la etapa que subió y la nota del modo), si «¿Por qué importa?» va a la vista (las 3 primeras partidas del juego)
 * y los botones, que van entre el truco y «Ver el detalle». Fuera de la pantalla de resultado, todo vacío.
 */
class ResultFrame(
  val score: String? = null,
  val levelNotice: ResultNotice? = null,
  val modeNotice: ResultNotice? = null,
  val showWhyInline: Boolean = true,
  val actions: (@Composable ColumnScope.() -> Unit)? = null
)

val LocalResultFrame = androidx.compose.runtime.compositionLocalOf { ResultFrame() }

object ResultScore {
  /** El puntaje de la partida («74 puntos · 14 de 19 aciertos»): se sigue guardando, pero va dentro de «Ver el detalle de tu partida». */
  fun line(r: com.example.model.GamePlayResult): String = "${r.score} ${if (r.score == 1) "punto" else "puntos"}" + if (r.totalTrials > 0) " · ${r.correctAnswers} de ${r.totalTrials} aciertos" else ""
}

/** Cuántos avisos caben bajo la frase de avance; los demás pasan al detalle como una línea. */
const val MAX_NOTICES = 2

@Composable
fun MeaningfulResult(model: MeaningfulResultModel, modifier: Modifier = Modifier, detail: (@Composable ColumnScope.() -> Unit)? = null) {
  val frame = LocalResultFrame.current
  // el orden de los avisos: la etapa, lo nuevo del juego (un modo, un cohete) y la nota del modo
  val notices = buildList {
    frame.levelNotice?.let { add(it) }
    model.notices.forEach { add(ResultNotice(it, lime = true)) }
    frame.modeNotice?.let { add(it) }
  }
  Column(modifier = modifier.fillMaxWidth().testTag("meaningful_result"), horizontalAlignment = Alignment.Start) {
    ResultHeadline(model)
    ResultPhrase(model)
    ResultNotices(notices.take(MAX_NOTICES))
    NextTrick(model)
    frame.actions?.invoke(this)
    DetailSection {
      if (frame.score != null) DetailText(frame.score, "detail_score", strong = true)
      model.dataLine?.let { DetailText(it, "detail_data", strong = true) }
      if (model.title.isNotBlank()) DetailText(model.title, "detail_title", strong = true)
      model.summaryLine?.let { DetailText(it, "detail_summary") }
      model.extraLine?.let { DetailText(it, "detail_extra") }
      notices.drop(MAX_NOTICES).forEach { DetailText(it.text, "detail_notice") }
      detail?.invoke(this)
      if (!frame.showWhyInline) WhyItMatters(model, topPadding = 16.dp)
    }
    if (frame.showWhyInline) WhyItMatters(model)
    ResultNote(model)
  }
}

// ---------------------------------------------------------------- la cifra, la frase y los avisos

/** La cifra suelta («5 luces») y su unidad («seguidas, bien repetidas»), alineadas a la izquierda; sin tarjeta. */
@Composable
fun ResultHeadline(model: MeaningfulResultModel, modifier: Modifier = Modifier) {
  Column(modifier.fillMaxWidth()) {
    OnBoardText(
      text = model.headline, color = Clay.Cream, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 34.sp, lineHeight = 38.sp,
      modifier = Modifier.padding(top = 24.dp).testTag("result_headline")
    )
    if (model.headlineUnit.isNotBlank()) {
      OnBoardText(
        text = model.headlineUnit, color = NubiTokens.OnNight, fontFamily = AppFamily, fontWeight = FontWeight.Normal, fontSize = 17.sp, lineHeight = 24.sp,
        modifier = Modifier.padding(top = 4.dp).testTag("result_unit")
      )
    }
  }
}

/** Tu avance en UNA frase, solo contigo, con los dos números adentro («Mejor que tu promedio (4 luces). Tu mejor: 6 luces.»). Sin medida hoy no se dibuja. */
@Composable
fun ResultPhrase(model: MeaningfulResultModel, modifier: Modifier = Modifier) {
  if (model.progressPhrase.isBlank()) return
  OnBoardText(
    text = model.progressPhrase, color = NubiTokens.OnNightDim, fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = 16.sp, lineHeight = 22.sp,
    modifier = modifier.fillMaxWidth().padding(top = 10.dp).testTag("progress_phrase")
  )
}

/** Los avisos que pasaron en esta partida: píldoras de plasticina, lima si celebran o anuncian algo nuevo y crema si solo informan. */
@OptIn(ExperimentalLayoutApi::class)
@Composable
fun ResultNotices(notices: List<ResultNotice>, modifier: Modifier = Modifier) {
  if (notices.isEmpty()) return
  FlowRow(
    modifier.fillMaxWidth().padding(top = 12.dp).testTag("result_notices"),
    horizontalArrangement = Arrangement.spacedBy(8.dp), verticalArrangement = Arrangement.spacedBy(8.dp)
  ) {
    for (n in notices) {
      ClayPill(text = n.text, color = if (n.lime) Clay.Lime else Clay.Cream, modifier = Modifier.scale(n.pop?.value ?: 1f).testTag("result_notice"))
    }
  }
}

/** El truco para la próxima: un recuadro lima, el único recuadro del final. */
@Composable
fun NextTrick(model: MeaningfulResultModel, modifier: Modifier = Modifier) {
  ClayCard(
    modifier = modifier.fillMaxWidth().padding(top = 20.dp).testTag("next_trick").semantics(mergeDescendants = true) { contentDescription = model.trickTitle + ": " + model.trick },
    color = Clay.Lime,
    size = SlabSize.MEDIUM,
    contentPadding = 14.dp
  ) {
    Column(modifier = Modifier.fillMaxWidth()) {
      Text(model.trickTitle, color = Clay.Ink, fontFamily = com.example.ui.theme.TitleFamily, fontWeight = FontWeight.Bold, fontSize = 16.sp)
      Text(model.trick, color = Clay.Ink, fontWeight = FontWeight.SemiBold, fontSize = 16.sp, modifier = Modifier.padding(top = 4.dp))
    }
  }
}

// ---------------------------------------------------------------- el detalle, «¿Por qué importa?» y la nota

/** «Ver el detalle de tu partida»: un botón de 56 dp o más, cerrado por defecto, que abre el detalle que cada juego ya mostraba (nada se borra). Va después de «Jugar de nuevo». */
@Composable
fun DetailSection(content: @Composable ColumnScope.() -> Unit) {
  var open by rememberSaveable { mutableStateOf(false) }
  Column(Modifier.fillMaxWidth().padding(top = 14.dp), horizontalAlignment = Alignment.CenterHorizontally) {
    val label = if (open) FinalesConSentido.DETAIL_CLOSE else FinalesConSentido.DETAIL_OPEN
    ClayCard(
      modifier = Modifier.fillMaxWidth().testTag("detail_toggle").semantics(mergeDescendants = true) { role = Role.Button; contentDescription = label; stateDescription = if (open) "abierto" else "cerrado" },
      color = Clay.Cream,
      size = SlabSize.MEDIUM,
      contentPadding = 0.dp,
      onClick = { open = !open }
    ) {
      Row(Modifier.fillMaxWidth().heightIn(min = 56.dp).padding(horizontal = 16.dp), verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.Center) {
        Text(label, color = Clay.Ink, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 16.sp, textAlign = TextAlign.Center, modifier = Modifier.weight(1f, fill = false))
        Spacer(Modifier.width(8.dp))
        KitGlyph(if (open) Glyph.UP else Glyph.DOWN, size = 26.dp)
      }
    }
    if (open) Column(Modifier.fillMaxWidth().padding(top = 14.dp).testTag("detail_content"), horizontalAlignment = Alignment.Start, content = content)
  }
}

/**
 * Una línea del detalle (la parte de LECTURA, spec-final-B §1.3): a la izquierda, 17 sp Nunito Regular `OnNight` con interlineado de 24 sp; [strong] = SemiBold del mismo tamaño; las que empiezan con «¡» (un récord) van en sol. 8 dp entre líneas.
 * Ya no se usan `Soft`, `Muted` ni `Faint` para el cuerpo.
 */
@Composable
fun DetailText(text: String, tag: String, strong: Boolean = false, modifier: Modifier = Modifier) {
  val celebrate = text.startsWith("¡")
  OnBoardText(
    text = text, color = if (celebrate) Clay.Sun else NubiTokens.OnNight, fontFamily = AppFamily,
    fontWeight = if (celebrate || strong) FontWeight.SemiBold else FontWeight.Normal, fontSize = 17.sp, lineHeight = 24.sp,
    modifier = modifier.fillMaxWidth().padding(start = 4.dp, top = 4.dp, bottom = 4.dp).testTag(tag)
  )
}

/** El título de una sección del detalle («Por modo», «Qué te frena»): 18 sp Fredoka Bold en sol, 12 dp antes. */
@Composable
fun DetailTitle(text: String, modifier: Modifier = Modifier) {
  OnBoardText(
    text = text, color = Clay.Sun, fontFamily = com.example.ui.theme.TitleFamily, fontWeight = FontWeight.Bold, fontSize = 18.sp, lineHeight = 24.sp,
    modifier = modifier.fillMaxWidth().padding(start = 4.dp, top = 12.dp, bottom = 4.dp)
  )
}

/**
 * ¿Por qué importa?: un bloque de lectura (título de 20 sp, cuerpo de 17 sp con interlineado 1,45, «Fuente: …» de 16 sp), alineado a la izquierda y con una línea fina arriba. A la vista en las 3 primeras partidas de cada juego; desde la cuarta va dentro del detalle.
 * (En la ficha del juego se llama «Qué pones en juego».)
 */
@Composable
fun WhyItMatters(model: MeaningfulResultModel, modifier: Modifier = Modifier, topPadding: androidx.compose.ui.unit.Dp = 24.dp) {
  Column(modifier.fillMaxWidth().padding(top = topPadding).testTag("why_it_matters")) {
    Box(Modifier.fillMaxWidth().height(1.dp).background(Color(0xFF8F8AC0).copy(alpha = 0.35f)))
    OnBoardText(model.whyTitle, color = NubiTokens.OnNight, fontFamily = com.example.ui.theme.TitleFamily, fontWeight = FontWeight.Bold, fontSize = 20.sp, lineHeight = 26.sp, modifier = Modifier.padding(start = 4.dp, top = 12.dp))
    OnBoardText(model.why, color = NubiTokens.OnNight, fontFamily = AppFamily, fontWeight = FontWeight.Normal, fontSize = 17.sp, lineHeight = 25.sp, modifier = Modifier.padding(start = 4.dp, top = 6.dp))
    OnBoardText(model.source, color = NubiTokens.OnNightDim, fontFamily = AppFamily, fontWeight = FontWeight.Normal, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(start = 4.dp, top = 8.dp))
  }
}

/** La nota de siempre al pie, centrada: «Medida de esta partida. No es un diagnóstico.» (una línea en 412 dp). Va en todos los estados. La de Rastro de luz, «Resultado de esta partida. Puede variar de una a otra. No es un diagnóstico.» (Tarea 186), pasa a dos renglones: es más larga y nada se corta, con letra 1,0 y con letra grande. */
@Composable
fun ResultNote(model: MeaningfulResultModel, modifier: Modifier = Modifier) {
  OnBoardText(
    text = model.note, color = NubiTokens.OnNightDim, fontFamily = AppFamily, fontWeight = FontWeight.Normal, fontSize = 16.sp, textAlign = TextAlign.Center,
    modifier = modifier.fillMaxWidth().padding(top = 20.dp, bottom = 8.dp).testTag("result_note")
  )
}
