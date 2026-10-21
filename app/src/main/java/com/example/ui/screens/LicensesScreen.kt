package com.example.ui.screens

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.heightIn
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

// Tarea 145 (ítem 10): el texto largo va sobre una LOSA crema (`ClayPanel`, como el aviso de salud de Ajustes) con la letra en tinta: sobre el cielo, los luceros fijos quedaban detrás de las palabras y leían como puntuación («Mandera, P.•, Keuleers»).
private val Title = com.example.ui.theme.Clay.Ink
private val Body = com.example.ui.theme.Clay.InkMuted

/**
 * "Licencias y créditos" (Ajustes, 1-oct): lo que hay que citar de lo que no es nuestro. Texto con títulos sobre UNA losa crema (Tarea 145: antes suelto sobre el cielo, con los luceros detrás de las palabras); los enlaces van como texto (no abren el navegador). Letra de 15 sp o más.
 * - SPALEX (datos de las palabras de Lluvia de meteoros, Cosecha de palabras y En la punta de la lengua): CC BY 4.0, con su atribución y el cambio que se le hizo en cada juego.
 * - EsPal (Duchon et al., 2013): la frecuencia de uso que trae SPALEX y que En la punta de la lengua usa para el nivel de cada palabra. Se CITA por cortesía (decisión de Ricardo, Tarea 80); su licencia propia NO está confirmada y no se escribe ninguna (consulta a los autores o al abogado pendiente antes de publicar).
 * - Hunspell es_ES de LibreOffice (diccionario que sirvió para armar y filtrar esas palabras; solo al generar): GPL 3+, LGPL 3+ o MPL 1.1+, a elección (LICENSE.md del repositorio LibreOffice/dictionaries, carpeta es).
 * - Tipografías (Fredoka, Nunito, Atkinson Hyperlegible y Fraunces): SIL Open Font License 1.1, con los avisos de copyright de cada una y el texto completo de la licencia (desplegable; vive en `res/raw/ofl.txt`, no en un string).
 * - Unity (el motor de los juegos).
 */
@Composable
fun LicensesScreen(onClose: () -> Unit, modifier: Modifier = Modifier) {
  BackHandler(onBack = onClose)
  Column(modifier.fillMaxSize()) {
    PanelHeader("Licencias y créditos", onClose)
    LicensesContent(Modifier.weight(1f))
  }
}

/** El texto de las licencias, desplazable (separado para verlo en una captura sin la cabecera). */
@Composable
fun LicensesContent(modifier: Modifier = Modifier) {
  Column(
    modifier
      .fillMaxWidth()
      .verticalScroll(rememberScrollState())
      .padding(horizontal = 20.dp, vertical = 8.dp)
      .testTag("licenses_content")
  ) {
    com.example.ui.components.ClayPanel(Modifier.fillMaxWidth().testTag("licenses_panel"), padding = 18.dp) {
    Text(
      "Nubi usa trabajo de otras personas. Aquí está de quién es y bajo qué licencia.",
      color = Body, fontSize = 16.sp, lineHeight = 22.sp
    )

    Section("Palabras de Lluvia de meteoros, Cosecha de palabras y En la punta de la lengua: SPALEX")
    Para(
      "Aguasvivas, J. A., Carreiras, M., Brysbaert, M., Mandera, P., Keuleers, E. y Duñabeitia, J. A. (2018). " +
        "SPALEX: A Spanish Lexical Decision Database From a Massive Online Data Collection. Frontiers in Psychology, 9, 2156. " +
        "doi:10.3389/fpsyg.2018.02156"
    )
    Para("Datos bajo licencia CC BY 4.0 (creativecommons.org/licenses/by/4.0). Fuente: figshare.com/projects/SPALEX/29722")
    Para(
      "Cambios: en Lluvia de meteoros y en Cosecha de palabras se usaron para ordenar las palabras en bandas de dificultad. " +
        "En En la punta de la lengua solo entran palabras que figuran en SPALEX, y su frecuencia de uso (que SPALEX incluye) decide en qué nivel va cada una."
    )
    Para(
      "Esa frecuencia de uso es la de EsPal y llega a la app a través de SPALEX: Duchon, A., Perea, M., Sebastián-Gallés, N., Martí, A. y Carreiras, M. (2013). " +
        "EsPal: One-stop shopping for Spanish word properties. Behavior Research Methods, 45, 1246-1258."
    )

    Section("Diccionario: Hunspell es_ES de LibreOffice")
    Para(
      "En Cosecha de palabras, las palabras de cada ronda salen de SPALEX (arriba) y sus formas verbales y plurales se generaron con este diccionario. " +
        "En Lluvia de meteoros y En la punta de la lengua el mismo diccionario sirvió para comprobar qué es una palabra de verdad."
    )
    Para(
      "Hunspell es_ES, diccionario iniciado por Santiago Bosio (github.com/LibreOffice/dictionaries). Se distribuye con tres licencias a elección: " +
        "GNU GPL 3 o posterior, GNU LGPL 3 o posterior, o MPL 1.1 o posterior. En la app solo van listas de palabras sueltas, no el diccionario."
    )

    Section("Tipografías")
    Para("Fredoka, de Milena Brandão y Hafontia; Nunito, de Vernon Adams y Jacques Le Bailly; y Atkinson Hyperlegible, del Braille Institute of America, usada para las palabras de Lluvia de meteoros y las letras de Cosecha de palabras; y Fraunces SemiBold Italic, de Undercase Type (Phaedra Charles y Flora Lucini), usada para el nombre de cada figura en La estrella intrusa.")
    Para("Las cuatro bajo la licencia SIL Open Font License 1.1 (openfontlicense.org). Avisos de copyright:")
    Para("Fredoka: Copyright 2016 The Fredoka Project Authors (github.com/hafontia/Fredoka-One).")
    Para("Nunito: Copyright 2014 The Nunito Project Authors (github.com/googlefonts/nunito).")
    Para("Atkinson Hyperlegible: Copyright 2020 Braille Institute of America, Inc.")
    Para("Fraunces: Copyright 2018 The Fraunces Project Authors (github.com/undercasetype/Fraunces).")
    OflText()

    Section("Hecho con Unity")
    Para("Los juegos de Nubi están hechos con Unity, de Unity Technologies (unity.com).")

    Section(com.example.data.HealthNotice.TITLE)
    Para(com.example.data.HealthNotice.TEXT)
    }

    Spacer(Modifier.height(90.dp))
  }
}

/** El texto completo de la SIL OFL 1.1, desplegable: se lee del recurso `res/raw/ofl.txt` (no es un string gigante). */
@Composable
private fun OflText() {
  val context = LocalContext.current
  var open by remember { mutableStateOf(false) }
  val text = remember(open) {
    if (!open) "" else runCatching { context.resources.openRawResource(com.example.R.raw.ofl).bufferedReader().use { it.readText() } }.getOrDefault("")
  }
  com.example.ui.theme.ClayButton(
    text = if (open) "Ocultar el texto completo de la licencia" else "Ver el texto completo de la licencia",
    onClick = { open = !open }, fontSize = 16.sp,                       // sol: sobre la losa crema un botón crema no se vería
    modifier = Modifier.padding(top = 6.dp, bottom = 8.dp).testTag("btn_ofl_text")
  )
  if (open) {
    Text(text, color = Body, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(bottom = 8.dp).testTag("ofl_text"))
  }
}

@Composable
private fun Section(text: String) {
  Text(
    text, color = Title, fontSize = 19.sp, fontWeight = FontWeight.Bold,
    modifier = Modifier.padding(top = 22.dp, bottom = 6.dp)
  )
}

@Composable
private fun Para(text: String) {
  Text(text, color = Body, fontSize = 15.sp, lineHeight = 21.sp, modifier = Modifier.padding(bottom = 8.dp))
}
