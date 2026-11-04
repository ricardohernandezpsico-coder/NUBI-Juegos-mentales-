package com.example.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.ui.theme.AppFamily
import com.example.ui.theme.Clay
import com.example.viewmodel.NeuroVidaViewModel

/** Lo que se toca del botón «Tú» (48 dp, el mínimo) y lo que se ve (un círculo de 44 dp con 3 dp de sombra: 1 dp de aire arriba y la sombra termina justo en el borde de abajo del toque). */
val TU_BUTTON_TOUCH = 48.dp
val TU_BUTTON_DISC = 44.dp
/** El margen derecho del botón (igual en las cuatro pestañas) y lo que las cabeceras dejan libre a la derecha para no pasar por debajo de él. */
val TU_BUTTON_END = 16.dp
val TU_BUTTON_RESERVE = TU_BUTTON_TOUCH + TU_BUTTON_END
/**
 * La cabecera de cada pestaña (Tarea 225b): 48 dp de alto con el botón arriba a la derecha DENTRO de ella (se va hacia arriba con la página, como el logotipo, y nunca queda encima de otra cosa) y, antes de lo que sigue, [TU_HEADER_GAP] de aire: la sombra del botón termina a los 48 dp,
 * así que lo de abajo empieza a 56 dp o más (la Tarea 225 lo dejaba a 46 dp en Juegos y la «R» tocaba el buscador).
 */
val TU_HEADER_HEIGHT = TU_BUTTON_TOUCH
val TU_HEADER_GAP = 8.dp

/** El crema del botón: el mismo de las letras de Hoy y del logotipo. */
private val TuCream = Color(0xFFF6EBD3)
private val TuShadow = Clay.Ink.copy(alpha = 0.8f)

/**
 * El botón «Tú» (Tarea 225, 4-nov, decisión de Ricardo: «Tú» sale de la barra y pasa a un botón redondo arriba a la derecha): un círculo de plasticina crema con borde de tinta de 2,5 dp y sombra dura de 3 dp, que se ve de 44 dp y se toca en 48 dp. Adentro, la INICIAL
 * del nombre en mayúscula (Fredoka Bold de 20 sp con tope de escala 1,3); sin nombre guardado, la persona de línea que tenía la barra ([NavIcon.PERSONA]). Para el lector es «Tú: tu perfil y ajustes», un botón; al tocarlo da el tic del kit ([rememberKitTap]) y abre Tú a
 * pantalla completa. Hundido (presionado) baja 3 dp sobre su sombra, como todo lo de plasticina. Va dentro de la cabecera de cada pestaña ([TuHeaderRow], Tarea 225b: antes lo ponía `MainActivity` encima de todas y quedaba sobre lo que se deslizaba), siempre en el mismo lugar.
 * [name] es el nombre guardado (puede venir vacío o con espacios).
 */
@Composable
fun TuButton(name: String, onClick: () -> Unit, modifier: Modifier = Modifier) {
  val tap = rememberKitTap()
  val source = remember { MutableInteractionSource() }
  val pressed by source.collectIsPressedAsState()
  val initial = tuInitial(name)
  Box(
    modifier
      .size(TU_BUTTON_TOUCH)
      .clickable(interactionSource = source, indication = null, role = Role.Button, onClickLabel = null) { tap(); onClick() }
      .semantics { contentDescription = TU_BUTTON_LABEL }
      .testTag("tu_button"),
    contentAlignment = Alignment.TopCenter
  ) {
    val lift = if (pressed) 3.dp else 0.dp                                                          // hundido: el disco baja sobre su sombra
    Box(Modifier.size(TU_BUTTON_DISC).offset(y = 1.dp + lift), contentAlignment = Alignment.Center) {
      Canvas(Modifier.size(TU_BUTTON_DISC)) {
        val r = size.minDimension / 2f
        val border = 2.5.dp.toPx()
        if (!pressed) drawCircle(TuShadow, r, Offset(r, r + 3.dp.toPx()))                           // la sombra dura de 3 dp
        drawCircle(TuCream, r, Offset(r, r))
        drawCircle(Clay.Ink, r - border / 2f, Offset(r, r), style = Stroke(border))
      }
      if (initial != null) {
        CapScale(1.3f) { Text(initial.toString(), color = Clay.Ink, fontSize = 20.sp, lineHeight = 24.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily) }
      } else {
        NavIconImage(NavIcon.PERSONA, size = 26.dp)
      }
    }
  }
}

/** Lo que dice el lector de pantalla del botón. */
const val TU_BUTTON_LABEL = "Tú: tu perfil y ajustes"

/** La inicial del nombre en mayúscula, o null si no hay nombre (o no empieza con una letra o un número): entonces va la persona. */
fun tuInitial(name: String): Char? = name.trim().firstOrNull()?.takeIf { it.isLetterOrDigit() }?.uppercaseChar()

/**
 * La fila de arriba de una pestaña (Tarea 225b): una caja de al menos [TU_HEADER_HEIGHT] con el título a la izquierda (lo dibuja [title], que se alinea solo y deja libres [TU_BUTTON_RESERVE] a la derecha) y el botón «Tú» ([tuButton], que recibe su lugar: arriba a la derecha con [TU_BUTTON_END] de margen)
 * y, debajo, [bottomGap] de aire. Si el título es más alto que 48 dp (letra grande) la fila crece y el botón queda arriba, en el mismo lugar. Sin botón ([tuButton] vacío, pantallas sueltas y pruebas) la fila mide igual.
 */
@Composable
fun TuHeaderRow(tuButton: @Composable (Modifier) -> Unit, modifier: Modifier = Modifier, bottomGap: androidx.compose.ui.unit.Dp = TU_HEADER_GAP, title: @Composable BoxScope.() -> Unit) {
  Box(modifier.fillMaxWidth().heightIn(min = TU_HEADER_HEIGHT).padding(bottom = bottomGap)) {
    title()
    tuButton(Modifier.align(Alignment.TopEnd).padding(end = TU_BUTTON_END))
  }
}

/** El botón «Tú» de una pestaña: con el nombre guardado y abriendo Tú ([NeuroVidaViewModel.openTu]); [modifier] trae su lugar (y, en Hoy, lo que lo publica a la guía). */
@Composable
fun TuHeaderButton(viewModel: NeuroVidaViewModel, modifier: Modifier = Modifier) {
  val settings by viewModel.userSettings.collectAsState()
  TuButton(name = settings.name, onClick = viewModel::openTu, modifier = modifier)
}
