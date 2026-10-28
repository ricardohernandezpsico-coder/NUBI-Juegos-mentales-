package com.example.ui.screens

import android.content.ActivityNotFoundException
import android.content.Context
import android.content.Intent
import android.net.Uri
import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
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
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Estudio
import com.example.data.Fuente
import com.example.data.Fuentes
import com.example.data.HealthNotice
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.components.GameEmblem
import com.example.ui.components.OnBoardText
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayCard
import com.example.ui.theme.NubiTokens
import com.example.ui.theme.TitleFamily

/** Abre [url] en el navegador (`ACTION_VIEW`): la app no tiene permiso de internet ni lo necesita, es el navegador el que se conecta. false si el teléfono no tiene ninguno. */
internal fun openInBrowser(context: Context, url: String): Boolean = try {
  context.startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(url)).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK))
  true
} catch (_: ActivityNotFoundException) {
  false
}

/** «En qué se basa Nubi», como panel con flecha encima de Tú (como Ajustes). */
@Composable
fun FuentesPanel(onClose: () -> Unit, modifier: Modifier = Modifier) {
  BackHandler(onBack = onClose)
  Column(modifier = modifier.fillMaxSize()) {
    PanelHeader("En qué se basa Nubi", onClose)
    FuentesScreen(modifier = Modifier.weight(1f))
  }
}

/**
 * «En qué se basa Nubi» (Tarea 185; Fable, `auditoria-fable/tu-pestana.md` §3, y la decisión de Ricardo del 27-oct sobre el nombre: no «La ciencia de Nubi», que sugiere que Nubi está probada). De qué estudio sale la tarea de cada juego: por área, una losa por juego con su emblema,
 * su nombre, UNA línea de «qué tomamos» y «Autor, año · Ver el estudio ›»; tocar la losa abre el estudio en el navegador (`https://doi.org/…`). Después, cómo se ajusta la dificultad (tres estudios), las palabras (que viven en «Licencias y créditos») y «Lo que no decimos», con el aviso
 * de salud. Solo autor y año y nuestra línea en español: ni el título en inglés ni la revista (la guardia de vocabulario prohíbe «cognitiv…» en cualquier texto del código y varios los traen; la página del DOI los trae a un toque). [openLink] devuelve false si no hay navegador.
 */
@Composable
fun FuentesScreen(modifier: Modifier = Modifier, openLink: ((String) -> Boolean)? = null) {
  val context = LocalContext.current
  var noBrowser by remember { mutableStateOf(false) }
  val open: (String) -> Unit = { url -> noBrowser = !(openLink?.invoke(url) ?: openInBrowser(context, url)) }
  Column(
    modifier = modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 20.dp).testTag("fuentes_screen"),
    verticalArrangement = Arrangement.spacedBy(14.dp)
  ) {
    Spacer(Modifier.height(4.dp))
    OnBoardText(Fuentes.INTRO, color = NubiTokens.OnNight, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("fuentes_intro"))
    OnBoardText(Fuentes.PROMISE, color = NubiTokens.OnNight, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, lineHeight = 22.sp, modifier = Modifier.testTag("fuentes_promise"))
    if (noBrowser) OnBoardText("No se encontró un navegador para abrir el estudio.", color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.testTag("fuentes_no_browser"))

    // Por área: una losa por juego
    for (area in DomainType.entries) {
      AreaHeading(area)
      for (game in GameRegistry.allGames.filter { it.domain == area && !GameRegistry.isRetired(it.id) }) {
        val source = Fuentes.byGame[game.id] ?: continue
        SourceTile(game.id, game.title, source, open)
      }
    }

    // Cómo se ajusta la dificultad
    SectionTitle(Fuentes.METHOD_TITLE, "fuentes_metodo")
    OnBoardText(Fuentes.METHOD_TEXT, color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 22.sp)
    Fuentes.method.forEach { StudyTile(it, open) }

    // Las palabras
    SectionTitle(Fuentes.WORDS_TITLE, "fuentes_palabras")
    OnBoardText(Fuentes.WORDS_TEXT, color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 22.sp)

    // Lo que no decimos
    SectionTitle(Fuentes.NO_DECIR_TITLE, "fuentes_no_decimos")
    OnBoardText(Fuentes.NO_DECIR_TEXT, color = NubiTokens.OnNight, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("fuentes_no_decir_texto"))
    StudyTile(Fuentes.simons, open)
    OnBoardText(HealthNotice.TEXT, color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("fuentes_aviso_salud"))

    Spacer(Modifier.height(90.dp))
  }
}

@Composable
private fun SectionTitle(text: String, tag: String) {
  OnBoardText(
    text, color = NubiTokens.OnNight, fontSize = 20.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily,
    modifier = Modifier.padding(top = 10.dp).semantics { heading() }.testTag(tag)
  )
}

/** El título de un área con su punto de color (el de Avance y de «Todos los juegos»). */
@Composable
private fun AreaHeading(area: DomainType) {
  Row(Modifier.padding(top = 10.dp).semantics(mergeDescendants = true) { heading() }.testTag("fuentes_area_${area.name.lowercase()}"), verticalAlignment = Alignment.CenterVertically) {
    Box(Modifier.size(12.dp).background(area.color, CircleShape))
    Spacer(Modifier.width(10.dp))
    OnBoardText(area.displayName, color = NubiTokens.OnNight, fontSize = 20.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily)
  }
}

/** La losa de un juego: emblema, nombre, «qué tomamos» y la cita con su enlace. TalkBack la lee como una frase y dice qué hace al tocar. */
@Composable
private fun SourceTile(gameId: String, title: String, source: Fuente, open: (String) -> Unit) {
  ClayCard(
    modifier = Modifier.fillMaxWidth().testTag("fuente_$gameId").semantics { contentDescription = "$title. ${source.taken} ${source.cite}. Abre el estudio en el navegador." },
    color = Clay.Cream, onClick = { open(source.url) }, contentPadding = 14.dp
  ) {
    Row(verticalAlignment = Alignment.CenterVertically) {
      GameEmblem(gameId, 40.dp)
      Spacer(Modifier.width(12.dp))
      Column(Modifier.weight(1f)) {
        Text(title, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp)
        Text(source.taken, color = Clay.Ink, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 2.dp))
        Text("${source.cite} · ${Fuentes.SEE_STUDY}", color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 4.dp))
      }
    }
  }
}

/** Una losa chica de un estudio suelto («Levitt, 1971 · Ver el estudio ›»). */
@Composable
private fun StudyTile(study: Estudio, open: (String) -> Unit) {
  ClayCard(
    modifier = Modifier.fillMaxWidth().testTag("estudio_${study.doi}").semantics { contentDescription = "${study.cite}. Abre el estudio en el navegador." },
    color = Clay.Cream, onClick = { open(study.url) }, contentPadding = 14.dp
  ) {
    Text("${study.cite} · ${Fuentes.SEE_STUDY}", color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, lineHeight = 21.sp, textAlign = TextAlign.Center, modifier = Modifier.fillMaxWidth())
  }
}
