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
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

private val Title = Color(0xFFEAF0FF)
private val Body = Color(0xFFC7D0FF)

/**
 * "Licencias y créditos" (Ajustes, 1-oct): lo que hay que citar de lo que no es nuestro. Texto suelto con títulos, sin
 * recuadros; los enlaces van como texto (no abren el navegador). Letra de 14 sp o más.
 * - SPALEX (datos de las palabras de Lluvia de meteoros): CC BY 4.0, con su atribución y el cambio que se le hizo.
 * - Tipografías (Fredoka, Nunito y Atkinson Hyperlegible): SIL Open Font License 1.1.
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
      .padding(horizontal = 24.dp, vertical = 8.dp)
      .testTag("licenses_content")
  ) {
    Text(
      "Nubi usa trabajo de otras personas. Aquí está de quién es y bajo qué licencia.",
      color = Body, fontSize = 16.sp, lineHeight = 22.sp
    )

    Section("Palabras de Lluvia de meteoros: SPALEX")
    Para(
      "Aguasvivas, J. A., Carreiras, M., Brysbaert, M., Mandera, P., Keuleers, E. y Duñabeitia, J. A. (2018). " +
        "SPALEX: A Spanish Lexical Decision Database From a Massive Online Data Collection. Frontiers in Psychology, 9, 2156. " +
        "doi:10.3389/fpsyg.2018.02156"
    )
    Para("Datos bajo licencia CC BY 4.0 (creativecommons.org/licenses/by/4.0). Fuente: figshare.com/projects/SPALEX/29722")
    Para("Cambios: se usaron para ordenar las palabras en bandas de dificultad en Lluvia de meteoros.")

    Section("Palabras de Cosecha de palabras")
    Para(
      "Las palabras de cada ronda salen de SPALEX (arriba) y sus formas verbales y plurales se generaron con el diccionario " +
        "Hunspell es_ES de LibreOffice (licencias MPL 1.1, LGPL y GPL, a elección; documentfoundation.org). En la app solo van " +
        "listas de palabras sueltas, no el diccionario."
    )

    Section("Tipografías")
    Para("Fredoka, de Milena Brandão y Hafontia; Nunito, de Vernon Adams y Jacques Le Bailly; y Atkinson Hyperlegible, del Braille Institute of America, usada para las palabras de Lluvia de meteoros y las letras de Cosecha de palabras; y Fraunces SemiBold Italic, de Undercase Type (Phaedra Charles y Flora Lucini), usada para el nombre de cada figura en La estrella intrusa.")
    Para("Las cuatro bajo la licencia SIL Open Font License 1.1 (openfontlicense.org).")

    Section("Hecho con Unity")
    Para("Los juegos de Nubi están hechos con Unity, de Unity Technologies (unity.com).")

    Spacer(Modifier.height(90.dp))
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
