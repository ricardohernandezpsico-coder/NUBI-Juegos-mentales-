package com.example.ui.screens

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Comparison
import com.example.data.ReferenciaFuentes
import com.example.ui.components.OnBoardText
import com.example.ui.theme.AppFamily
import com.example.ui.theme.NubiTokens
import com.example.ui.theme.TitleFamily

/** «De dónde sale la referencia», como panel con flecha encima de la pestaña (como «En qué se basa Nubi»). */
@Composable
fun ReferenciaPanel(onClose: () -> Unit, modifier: Modifier = Modifier) {
  BackHandler(onBack = onClose)
  Column(modifier = modifier.fillMaxSize()) {
    PanelHeader(ReferenciaFuentes.TITLE, onClose)
    ReferenciaScreen(modifier = Modifier.weight(1f))
  }
}

/**
 * «De dónde sale la referencia» (Tarea 209; `auditoria-fable/claridad-plus-y-ayuda.md` §1.1.1), con el molde de «En qué se basa Nubi»: un primer párrafo que dice primero «No son otras personas: Nubi no junta datos de nadie» y qué es la referencia, un segundo párrafo (en `OnNightDim`) que dice con honestidad cómo se calcularon los números,
 * y, por grupo, una losa por estudio con «Autor, año · N personas», una línea en español y «Ver el estudio ›» (abre el DOI en el navegador, sin permiso de internet; [StudyRow]). El último grupo no tiene losas, solo una línea. Nunca el número de robots. [openLink] devuelve false si no hay navegador.
 */
@Composable
fun ReferenciaScreen(modifier: Modifier = Modifier, openLink: ((String) -> Boolean)? = null) {
  val context = LocalContext.current
  var noBrowser by remember { mutableStateOf(false) }
  val open: (String) -> Unit = { url -> noBrowser = !(openLink?.invoke(url) ?: openInBrowser(context, url)) }
  Column(
    modifier = modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 20.dp).testTag("referencia_screen"),
    verticalArrangement = Arrangement.spacedBy(14.dp)
  ) {
    Spacer(Modifier.height(4.dp))
    OnBoardText(ReferenciaFuentes.INTRO, color = NubiTokens.OnNight, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("referencia_intro"))
    OnBoardText(ReferenciaFuentes.INTRO_2, color = NubiTokens.OnNight, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("referencia_intro_2"))
    OnBoardText(ReferenciaFuentes.ABOUT, color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("referencia_about"))
    if (noBrowser) OnBoardText("No se encontró un navegador para abrir el estudio.", color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.testTag("referencia_no_browser"))

    for (group in ReferenciaFuentes.groups) {
      Column(Modifier.fillMaxWidth().testTag("referencia_grupo_${group.key}"), verticalArrangement = Arrangement.spacedBy(8.dp)) {
        OnBoardText(group.title, color = NubiTokens.OnNight, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, modifier = Modifier.padding(top = 8.dp).semantics { heading() }.testTag("referencia_titulo_${group.key}"))
        group.studies.forEach { StudyRow(it, open) }
        group.line?.let { OnBoardText(it, color = NubiTokens.OnNight, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("referencia_linea_${group.key}")) }
      }
    }

    Spacer(Modifier.height(90.dp))
  }
}

/**
 * La fila «De dónde sale la referencia ›» (Tarea 209): UNA línea de 16 sp en Fredoka, tocable entera y de 48 dp o más, con el rol de botón; el lector dice «De dónde sale la referencia, botón» (sin el «›»). El color lo pone quien la usa: `SunOnNight` sobre el noche de la página,
 * `PanelAccent` sobre crema (la hoja del candado y las preguntas). La usan la página «Contrarreloj», la hoja del candado y la pregunta «¿Con qué me compara el Contrarreloj?».
 */
@Composable
internal fun SourceLinkRow(onClick: () -> Unit, color: Color, tag: String, modifier: Modifier = Modifier, label: String = Comparison.SOURCE_LINK) {
  Box(
    modifier.fillMaxWidth().heightIn(min = 48.dp)
      .clickable(role = Role.Button, onClickLabel = "Abrir de dónde sale la referencia", onClick = onClick)
      .semantics(mergeDescendants = true) { contentDescription = label.removeSuffix(" ›") }
      .testTag(tag),
    contentAlignment = Alignment.CenterStart
  ) {
    Text(label, color = color, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, fontFamily = AppFamily, lineHeight = 22.sp, modifier = Modifier.padding(vertical = 13.dp))
  }
}
