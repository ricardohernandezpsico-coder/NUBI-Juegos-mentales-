package com.example.ui.components

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.offset
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.ui.theme.AppFamily
import com.example.ui.theme.Clay

/** La sombra dura del logotipo: tinta más oscura que el cielo, para que se vea el relieve. */
private val LogoShadow = Color(0xFF080522)

/**
 * El logotipo «Nubilus» de arriba de Hoy (Tarea 223; la variante L1 de la lámina de la Tarea 222, que eligió Ricardo: «cuando entras al Instagram, arriba dice Instagram con sus letras»): el nombre de la app en Fredoka Bold de 30 dp, con las
 * letras un poco separadas, el relleno [fill] (el crema de Hoy), un contorno grueso de tinta (5 dp, 2,5 dp hacia afuera) y una sombra dura de 4 dp en tinta oscura, como todo lo de plasticina.
 *
 * Es una MARCA, no un texto: NO crece con la letra grande del sistema (los 30 dp, el interlineado y el espaciado se piden en dp y se pasan a sp con la escala del teléfono, así que se compensan solos, también con la escala no lineal de
 * Android 14). El lector de pantalla lo dice una vez, «Nubilus», como encabezado (`home_logo`); las cuatro capas de dibujo no se leen.
 */
@Composable
fun NubilusLogo(fill: Color, modifier: Modifier = Modifier) {
  val density = LocalDensity.current
  val style = with(density) { TextStyle(fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 30.dp.toSp(), lineHeight = 36.dp.toSp(), letterSpacing = 1.2.dp.toSp()) }
  val strokePx = with(density) { 5.dp.toPx() }
  val text = "Nubilus"
  Box(modifier.testTag("home_logo").clearAndSetSemantics { contentDescription = text; heading() }) {
    Text(text, style = style.copy(color = LogoShadow, drawStyle = Stroke(strokePx, join = StrokeJoin.Round)), modifier = Modifier.offset(4.dp, 4.dp))
    Text(text, style = style.copy(color = LogoShadow), modifier = Modifier.offset(4.dp, 4.dp))
    Text(text, style = style.copy(color = Clay.Ink, drawStyle = Stroke(strokePx, join = StrokeJoin.Round)))
    Text(text, style = style.copy(color = fill))
  }
}
