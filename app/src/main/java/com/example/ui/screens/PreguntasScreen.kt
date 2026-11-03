package com.example.ui.screens

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.ScrollState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.stateDescription
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.Features
import com.example.data.Preguntas
import com.example.data.glueNumbers
import com.example.ui.components.ClaySlab
import com.example.ui.components.Glyph
import com.example.ui.components.KitButton
import com.example.ui.components.KitGlyph
import com.example.ui.components.OnBoardText
import com.example.ui.components.PanelAccent
import com.example.ui.components.SlabColor
import com.example.ui.components.SlabSize
import com.example.ui.theme.Clay
import com.example.ui.theme.NubiTokens

/**
 * «Preguntas frecuentes», como panel con flecha encima de Tú (como «En qué se basa Nubilus»). Desde ahí la pregunta «¿Con qué me compara el Contrarreloj?» abre «De dónde sale la referencia» dentro del mismo panel y Atrás vuelve aquí, con lo que estaba abierto y el deslizado donde estaban.
 * Lo abierto vive aquí y no en cada losa para que sobreviva a esa ida y vuelta (y al girar la pantalla).
 */
@Composable
fun PreguntasPanel(onClose: () -> Unit, modifier: Modifier = Modifier) {
  var showReferencia by rememberSaveable { mutableStateOf(false) }
  var openKeys by rememberSaveable { mutableStateOf("") }          // las preguntas desplegadas, sus claves separadas por comas
  val scroll = rememberScrollState()
  if (showReferencia) {
    ReferenciaPanel(onClose = { showReferencia = false }, modifier = modifier)
    return
  }
  BackHandler(onBack = onClose)
  val expanded = openKeys.split(',').filter { it.isNotEmpty() }.toSet()
  Column(modifier = modifier.fillMaxSize()) {
    PanelHeader(Preguntas.TITLE, onClose)
    PreguntasScreen(
      modifier = Modifier.weight(1f), expanded = expanded, scroll = scroll,
      onToggle = { key -> openKeys = (if (key in expanded) expanded - key else expanded + key).joinToString(",") },
      onOpenReferencia = { showReferencia = true }
    )
  }
}

/**
 * «Preguntas frecuentes» (Tarea 209; `auditoria-fable/claridad-plus-y-ayuda.md` §1.3): arriba «Respuestas cortas. Si falta una, escríbenos desde Contacto.» (16 sp) y, debajo, cada pregunta en una losa crema tocable (la pregunta en Fredoka 17 con el chevron del kit; 48 dp o más) que al tocar despliega su respuesta en la misma losa (16 sp, interlínea 22).
 * El lector de pantalla dice «¿…?, botón, plegado» y «desplegado»; nada se abre solo. La de «¿Con qué me compara el Contrarreloj?» trae bajo su respuesta la fila «De dónde sale la referencia ›» ([onOpenReferencia]). Las dos del cobro solo salen con [billingOn] ([Features.BILLING], apagada hoy), arriba de la lista.
 */
@Composable
fun PreguntasScreen(
  expanded: Set<String>, onToggle: (String) -> Unit, onOpenReferencia: () -> Unit,
  modifier: Modifier = Modifier, scroll: ScrollState = rememberScrollState(), billingOn: Boolean = Features.BILLING, openLink: ((String) -> Boolean)? = null
) {
  val context = LocalContext.current
  Column(
    modifier = modifier.fillMaxSize().verticalScroll(scroll).padding(horizontal = 20.dp).testTag("preguntas_screen"),
    verticalArrangement = Arrangement.spacedBy(10.dp)
  ) {
    OnBoardText(Preguntas.LEAD, color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 4.dp, bottom = 4.dp).testTag("preguntas_lead"))
    for (p in Preguntas.visible(billingOn)) {
      PreguntaTile(
        p, p.key in expanded, { onToggle(p.key) }, onOpenReferencia,
        onOpenSubscriptions = { openLink?.invoke(Preguntas.subscriptionsUrl()) ?: openInBrowser(context, Preguntas.subscriptionsUrl()) }
      )
    }
    Spacer(Modifier.height(90.dp))
  }
}

@Composable
private fun PreguntaTile(p: Preguntas.Pregunta, expanded: Boolean, onToggle: () -> Unit, onOpenReferencia: () -> Unit, onOpenSubscriptions: () -> Unit) {
  ClaySlab(
    SlabColor.CREAM, Modifier.fillMaxWidth().semantics { stateDescription = if (expanded) "desplegado" else "plegado" }.testTag("pregunta_${p.key}"), SlabSize.MEDIUM, onClick = onToggle,
    onClickLabel = if (expanded) "Ocultar la respuesta" else "Mostrar la respuesta"
  ) {
    Column(Modifier.fillMaxWidth().padding(14.dp)) {
      Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        Text(p.question.glueNumbers(), color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.weight(1f).testTag("pregunta_${p.key}_q"))
        Spacer(Modifier.width(10.dp))
        KitGlyph(if (expanded) Glyph.UP else Glyph.DOWN, size = 20.dp)
      }
      if (expanded) {
        Text(p.answer.glueNumbers(), color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 8.dp).testTag("pregunta_${p.key}_a"))
        if (p.sourceLink) SourceLinkRow(onOpenReferencia, PanelAccent, "pregunta_${p.key}_source", Modifier.padding(top = 4.dp))
        if (p.subscriptionsButton) KitButton(
          text = Preguntas.SUBSCRIPTIONS_BUTTON, onClick = onOpenSubscriptions, color = SlabColor.CREAM, fontSize = 16.sp, minHeight = 48.dp,
          modifier = Modifier.padding(top = 10.dp).testTag("pregunta_${p.key}_subscriptions")
        )
      }
    }
  }
}
