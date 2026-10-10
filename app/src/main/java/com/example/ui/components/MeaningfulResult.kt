package com.example.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.KeyboardArrowDown
import androidx.compose.material.icons.filled.KeyboardArrowUp
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
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

/** Un número del «Tu avance»: «Hoy», «Tu promedio» o «Tu mejor», con su valor ya escrito («11 %» o «—»). El de hoy va resaltado. */
data class ResultChip(val label: String, val value: String, val highlight: Boolean = false)

/**
 * Lo que se ve en un final CON SENTIDO (docs/diseno-aterrizaje.md §6): la plantilla que el primer juego que la usa (Aterrizaje Lunar) deja lista para los demás, si Ricardo lo confirma. Cuatro piezas, en este orden de importancia:
 * 1. **Lo que hiciste** (recuadro principal): un título, la cifra en grande con su unidad, UN dato tuyo (dónde te costó más) y una línea chica con lo que pasó en la partida;
 * 2. **Tu avance**, solo contigo (nunca percentiles ni otras personas): una fila de tres números y una frase;
 * 3. **Un truco** para la próxima (recuadro chico);
 * 4. **¿Por qué importa?**, abajo, en chico y sin recuadro: una frase, su fuente y la nota de siempre («Medida de esta partida. No es un diagnóstico.»).
 * Si el juego trae [detail] (los 17 juegos de la Etapa 3: `docs/finales-con-sentido.md`), el detalle que antes se veía (barras, listas, desgloses) NO se borra: va detrás de «Ver el detalle de tu partida», un botón de 56 dp o más cerrado por defecto,
 * entre el truco y «¿Por qué importa?». El [MeaningfulResultModel.title] y el [MeaningfulResultModel.boxTitle] pueden ir en blanco (no se dibujan).
 * Todo el texto va desde 14 sp, nada se dice solo con color y cada pieza se lee de corrido con un lector de pantalla.
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
  val chips: List<ResultChip>,
  val progressPhrase: String,
  val trickTitle: String,
  val trick: String,
  val whyTitle: String,
  val why: String,
  val source: String,
  val note: String,
  /** Lo que lee en voz alta un lector de pantalla del recuadro principal. */
  val spoken: String = ""
)

private val BoxFill = Color(0xFF22205A)
private val ChipFill = Color(0xFF2C2A6E)
private val TrickFill = Color(0xFF1F3A35)
private val Soft = Color(0xFFD6D1F2)
private val Muted = Color(0xFFABA5D2)
private val Faint = Color(0xFF8F8AC0)

@Composable
fun MeaningfulResult(model: MeaningfulResultModel, modifier: Modifier = Modifier, detail: (@Composable ColumnScope.() -> Unit)? = null) {
  Column(modifier = modifier.fillMaxWidth().testTag("meaningful_result"), horizontalAlignment = Alignment.CenterHorizontally) {
    WhatYouDid(model)
    YourProgress(model)
    NextTrick(model)
    if (detail != null) DetailSection(detail)
    WhyItMatters(model)
  }
}

/** «Ver el detalle de tu partida»: un botón de 56 dp o más, cerrado por defecto, que abre el detalle que cada juego ya mostraba (nada se borra). */
@Composable
fun DetailSection(content: @Composable ColumnScope.() -> Unit) {
  var open by rememberSaveable { mutableStateOf(false) }
  Column(Modifier.fillMaxWidth().padding(top = 18.dp), horizontalAlignment = Alignment.CenterHorizontally) {
    val label = if (open) FinalesConSentido.DETAIL_CLOSE else FinalesConSentido.DETAIL_OPEN
    ClayCard(
      modifier = Modifier.fillMaxWidth().testTag("detail_toggle").semantics(mergeDescendants = true) { role = Role.Button; contentDescription = label; stateDescription = if (open) "abierto" else "cerrado" },
      color = ChipFill,
      radius = 18.dp,
      depth = 4.dp,
      contentPadding = 0.dp,
      onClick = { open = !open }
    ) {
      Row(Modifier.fillMaxWidth().heightIn(min = 56.dp).padding(horizontal = 16.dp), verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.Center) {
        Text(label, color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 16.sp, textAlign = TextAlign.Center, modifier = Modifier.weight(1f, fill = false))
        Spacer(Modifier.width(8.dp))
        Icon(if (open) Icons.Default.KeyboardArrowUp else Icons.Default.KeyboardArrowDown, contentDescription = null, tint = Color.White, modifier = Modifier.size(26.dp))
      }
    }
    if (open) Column(Modifier.fillMaxWidth().padding(top = 10.dp).testTag("detail_content"), horizontalAlignment = Alignment.CenterHorizontally, content = content)
  }
}

/** 1) Lo que hiciste: el título y el recuadro principal. */
@Composable
fun WhatYouDid(model: MeaningfulResultModel, modifier: Modifier = Modifier) {
  Column(modifier = modifier.fillMaxWidth(), horizontalAlignment = Alignment.CenterHorizontally) {
    if (model.title.isNotBlank()) {
      Text(
        text = model.title,
        color = Color.White,
        fontFamily = AppFamily,
        fontWeight = FontWeight.Bold,
        fontSize = 24.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 16.dp)
      )
      Spacer(Modifier.height(12.dp))
    }
    ClayCard(
      modifier = Modifier.fillMaxWidth().testTag("meaningful_box").semantics(mergeDescendants = true) { if (model.spoken.isNotEmpty()) contentDescription = model.spoken },
      color = BoxFill,
      radius = 22.dp,
      depth = 5.dp,
      contentPadding = 16.dp
    ) {
      Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.fillMaxWidth()) {
        if (model.boxTitle.isNotBlank()) Text(model.boxTitle, color = Soft, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 15.sp, textAlign = TextAlign.Center)
        Text(model.headline, color = Clay.Sun, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 34.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 2.dp))
        Text(model.headlineUnit, color = Color.White, fontWeight = FontWeight.SemiBold, fontSize = 16.sp, textAlign = TextAlign.Center)
        model.dataLine?.let {
          Text(it, color = Clay.Sky, fontWeight = FontWeight.Bold, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 8.dp))
        }
        model.summaryLine?.let {
          Text(it, color = Muted, fontWeight = FontWeight.SemiBold, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 6.dp))
        }
        model.extraLine?.let {
          Text(it, color = Muted, fontWeight = FontWeight.SemiBold, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 2.dp))
        }
      }
    }
  }
}

/** 2) Tu avance, solo contigo: tres números en una fila y su frase. */
@Composable
fun YourProgress(model: MeaningfulResultModel, modifier: Modifier = Modifier) {
  Column(modifier = modifier.fillMaxWidth().padding(top = 18.dp), horizontalAlignment = Alignment.CenterHorizontally) {
    Text(model.progressTitle, color = Soft, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 15.sp)
    Spacer(Modifier.height(8.dp))
    Row(horizontalArrangement = Arrangement.spacedBy(10.dp), modifier = Modifier.fillMaxWidth().testTag("progress_chips")) {
      for (chip in model.chips) {
        val spoken = chip.label + ": " + chip.value
        ClayCard(
          modifier = Modifier.weight(1f).semantics(mergeDescendants = true) { contentDescription = spoken },
          color = if (chip.highlight) Clay.Sun else ChipFill,
          radius = 14.dp,
          depth = 3.dp,
          contentPadding = 8.dp
        ) {
          Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.fillMaxWidth()) {
            Text(chip.label, color = if (chip.highlight) Clay.Ink else Soft, fontWeight = FontWeight.Bold, fontSize = 14.sp, textAlign = TextAlign.Center)
            Text(chip.value, color = if (chip.highlight) Clay.Ink else Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 17.sp, textAlign = TextAlign.Center)
          }
        }
      }
    }
    Text(
      text = model.progressPhrase,
      color = Color(0xFFF4F2FF),
      fontWeight = FontWeight.SemiBold,
      fontSize = 15.sp,
      textAlign = TextAlign.Center,
      modifier = Modifier.padding(top = 10.dp, start = 16.dp, end = 16.dp).testTag("progress_phrase")
    )
  }
}

/** 3) El truco para la próxima: un recuadro chico. */
@Composable
fun NextTrick(model: MeaningfulResultModel, modifier: Modifier = Modifier) {
  ClayCard(
    modifier = modifier.fillMaxWidth().padding(top = 18.dp).testTag("next_trick").semantics(mergeDescendants = true) { contentDescription = model.trickTitle + ": " + model.trick },
    color = TrickFill,
    radius = 18.dp,
    depth = 4.dp,
    contentPadding = 14.dp
  ) {
    Column(modifier = Modifier.fillMaxWidth()) {
      Text(model.trickTitle, color = Clay.Lime, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 15.sp)
      Text(model.trick, color = Color(0xFFF4F2FF), fontWeight = FontWeight.SemiBold, fontSize = 14.sp, modifier = Modifier.padding(top = 4.dp))
    }
  }
}

/** 4) ¿Por qué importa?: abajo, en chico y sin recuadro (una frase, su fuente y la nota de siempre). */
@Composable
fun WhyItMatters(model: MeaningfulResultModel, modifier: Modifier = Modifier) {
  Column(
    modifier = modifier.fillMaxWidth().padding(top = 22.dp, start = 12.dp, end = 12.dp).testTag("why_it_matters"),
    horizontalAlignment = Alignment.CenterHorizontally
  ) {
    Box(Modifier.fillMaxWidth().height(1.dp).background(Faint.copy(alpha = 0.35f)))
    Text(model.whyTitle, color = Muted, fontWeight = FontWeight.Bold, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 8.dp))
    Text(model.why, color = Muted, fontWeight = FontWeight.SemiBold, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 4.dp))
    Text(model.source, color = Faint, fontWeight = FontWeight.SemiBold, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 6.dp))
    Text(model.note, color = Faint, fontWeight = FontWeight.SemiBold, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 2.dp))
  }
}
