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
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.stateDescription
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Estudio
import com.example.data.Fuente
import com.example.data.Fuentes
import com.example.data.FuentesMas
import com.example.data.HealthNotice
import com.example.data.Mecanismo
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.components.GameEmblem
import com.example.ui.components.Glyph
import com.example.ui.components.KitButton
import com.example.ui.components.OnBoardText
import com.example.ui.components.SlabColor
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
 *
 * Tarea 186 (28-oct): bajo la losa de cada juego, una fila «Más estudios (N)» PLEGADA que al tocarla despliega, en el mismo lugar, sus estudios extra (cada uno en su fila tocable: «Autor, año», su línea y «Ver el estudio ›»); entre los juegos y «Las palabras», la sección
 * «Cómo está hecha Nubi» (un grupo por decisión de diseño: a la vista, su título y su frase de lo que hace Nubi; las fuentes van plegadas en «Estudios (N)», igual que «Más estudios»: «lo simple primero», Opus, 28-oct; «La dificultad se acomoda a ti» absorbe lo que era «Cómo se ajusta la dificultad»); y «Lo que no decimos» con cuatro revisiones. Los datos están en `FuentesMas`.
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

    // Por área: una losa por juego, y bajo cada una «Más estudios (N)», plegado
    for (area in DomainType.entries) {
      AreaHeading(area)
      for (game in GameRegistry.allGames.filter { it.domain == area && !GameRegistry.isRetired(it.id) }) {
        val source = Fuentes.byGame[game.id] ?: continue
        Column(Modifier.fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(6.dp)) {
          SourceTile(game.id, game.title, source, open)
          FuentesMas.byGame[game.id]?.takeIf { it.isNotEmpty() }?.let { StudiesFold("mas_${game.id}", FuentesMas.moreLabel(it.size), "Más estudios de ${game.title}, ${it.size}", it, open, startPadding = 14.dp) }
        }
      }
    }

    // Cómo está hecha Nubi: un grupo por decisión de diseño
    SectionTitle(FuentesMas.MECHANISMS_TITLE, "fuentes_mecanismos")
    OnBoardText(FuentesMas.MECHANISMS_TEXT, color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("fuentes_mecanismos_texto"))
    FuentesMas.mechanisms.forEach { MechanismGroup(it, open) }

    // Las palabras
    SectionTitle(Fuentes.WORDS_TITLE, "fuentes_palabras")
    OnBoardText(Fuentes.WORDS_TEXT, color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 22.sp)

    // Lo que no decimos
    SectionTitle(Fuentes.NO_DECIR_TITLE, "fuentes_no_decimos")
    OnBoardText(Fuentes.NO_DECIR_TEXT, color = NubiTokens.OnNight, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("fuentes_no_decir_texto"))
    FuentesMas.noDecir.forEach { StudyRow(it, open) }
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

/**
 * La fila de un estudio (Tarea 186): «Autor, año», su línea «qué sustenta» y «Ver el estudio ›»; tocarla abre el DOI en el navegador. Se usa en «Más estudios», en «Cómo está hecha Nubi» y en «Lo que no decimos». TalkBack la lee como una frase y dice qué hace al tocar.
 */
@Composable
private fun StudyRow(study: Estudio, open: (String) -> Unit, modifier: Modifier = Modifier) {
  ClayCard(
    modifier = modifier.fillMaxWidth().testTag("estudio_${study.doi}").semantics { contentDescription = "${study.cite}. ${study.line} Abre el estudio en el navegador." },
    color = Clay.Cream, onClick = { open(study.url) }, contentPadding = 14.dp
  ) {
    Text(study.cite, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp)
    Text(study.line, color = Clay.Ink, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 2.dp))
    Text(Fuentes.SEE_STUDY, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 4.dp))
  }
}

/**
 * Una fila plegable de estudios (Tarea 186): un botón lila de 48 dp, PLEGADO por defecto, que al tocarlo despliega en el mismo lugar sus estudios, cada uno en su [StudyRow]. Es «Más estudios (N)» bajo la losa de un juego y «Estudios (N)» bajo cada grupo de «Cómo está hecha Nubi». La flecha del botón dice si está abierto;
 * el lector de pantalla oye el estado («estudios ocultos» / «estudios visibles»). Lo abierto se recuerda al girar la pantalla. [tag] es la etiqueta del botón (`mas_<juego>` / `estudios_<grupo>`).
 */
@Composable
private fun StudiesFold(tag: String, label: String, spoken: String, studies: List<Estudio>, open: (String) -> Unit, startPadding: Dp) {
  var expanded by rememberSaveable(tag) { mutableStateOf(false) }
  Column(Modifier.fillMaxWidth().padding(start = startPadding), verticalArrangement = Arrangement.spacedBy(8.dp)) {
    KitButton(
      text = label, onClick = { expanded = !expanded }, color = SlabColor.LILAC, glyph = if (expanded) Glyph.UP else Glyph.DOWN, fontSize = 16.sp, wide = false, minHeight = 48.dp,
      onClickLabel = if (expanded) "Ocultar los estudios" else "Mostrar los estudios",
      modifier = Modifier.testTag(tag).semantics {
        contentDescription = spoken
        stateDescription = if (expanded) "estudios visibles" else "estudios ocultos"
      }
    )
    if (expanded) studies.forEach { StudyRow(it, open) }
  }
}

/** Un grupo de «Cómo está hecha Nubi»: su título, UNA frase en simple de lo que hace Nubi y debajo «Estudios (N)», plegado, con sus fuentes en el mismo formato de fila (Opus, 28-oct: a la vista solo lo simple). */
@Composable
private fun MechanismGroup(group: Mecanismo, open: (String) -> Unit) {
  Column(Modifier.fillMaxWidth().testTag("mecanismo_${group.key}"), verticalArrangement = Arrangement.spacedBy(8.dp)) {
    OnBoardText(group.title, color = NubiTokens.OnNight, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, modifier = Modifier.padding(top = 8.dp).semantics { heading() }.testTag("mecanismo_titulo_${group.key}"))
    OnBoardText(group.text, color = NubiTokens.OnNight, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("mecanismo_texto_${group.key}"))
    StudiesFold("estudios_${group.key}", FuentesMas.studiesLabel(group.studies.size), "Estudios de ${group.title}, ${group.studies.size}", group.studies, open, startPadding = 0.dp)
  }
}
