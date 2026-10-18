package com.example.ui.components

import androidx.compose.animation.core.animateDpAsState
import androidx.compose.animation.core.snap
import androidx.compose.animation.core.tween
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsFocusedAsState
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.selection.toggleable
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.res.imageResource
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.stateDescription
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.ui.theme.AppFamily
import com.example.ui.theme.Clay

/** El ancho de la pista del interruptor (dp) y cuánto recorre la perilla. */
private val SwitchTrackWidth = 56.dp
private val SwitchTrackHeight = 32.dp
private val SwitchThumb = 26.dp
private val SwitchTravel = SwitchTrackWidth - SwitchThumb - 6.dp

/**
 * El interruptor de plasticina (Tarea 91): una pista de losa chica con un disco que se desliza. Encendido = lima con el disco a la derecha y un ✓ de churro tinta a la izquierda; apagado = crema con el disco a la izquierda: se distingue por la posición del disco y el ✓, no solo por el color.
 * Mientras el dedo está encima, la pista y la perilla se ven hundidas. Toca 56 × 48 dp. Para el lector de pantalla es un interruptor (`Role.Switch`) con su estado («Encendido» / «Apagado») y, si se da, su nombre en [label]. Con «quitar animaciones» la perilla cambia de golpe.
 */
@Composable
fun ClaySwitch(checked: Boolean, onCheckedChange: (Boolean) -> Unit, modifier: Modifier = Modifier, label: String? = null) {
  val spec = rememberClayKitSpec()
  val disc = spec.disc(DiscStyle.CREAM.key)
  val thumb = ImageBitmap.imageResource(DiscStyle.CREAM.normal)
  val thumbPressed = ImageBitmap.imageResource(DiscStyle.CREAM.pressed)
  val source = remember { MutableInteractionSource() }
  val isPressed by source.collectIsPressedAsState()
  val reduce = rememberReduceMotion()
  val travel by animateDpAsState(if (checked) SwitchTravel else 0.dp, if (reduce) snap() else tween(NubiMotion.STATE_FAST_MS), label = "interruptor")
  Box(
    modifier
      .size(SwitchTrackWidth, 48.dp)
      .toggleable(value = checked, interactionSource = source, indication = null, role = Role.Switch, onValueChange = onCheckedChange)
      .semantics { if (label != null) contentDescription = label; stateDescription = if (checked) "Encendido" else "Apagado" },
    contentAlignment = Alignment.CenterStart
  ) {
    ClaySlab(if (checked) SlabColor.LIME else SlabColor.CREAM, size = SlabSize.SMALL, forcePressed = isPressed, onClick = null) {
      Box(Modifier.size(SwitchTrackWidth, SwitchTrackHeight)) {
        if (checked) KitGlyph(Glyph.CHECK, Modifier.align(Alignment.CenterStart).offset(x = 8.dp), size = 18.dp)
        Box(
          Modifier.align(Alignment.CenterStart).offset(x = 3.dp + travel).size(SwitchThumb).drawBehind {
            val body = Rect(Offset.Zero, Size(size.width, size.height))
            val k = size.width / (disc.bodyWidth * density)
            drawKitPiece(if (isPressed) thumbPressed else thumb, disc, body, scale = k)
          }
        )
      }
    }
  }
}

/**
 * El campo de texto de plasticina (Tarea 91): una losa crema HUNDIDA (la imagen presionada), texto en tinta de [fontSize] (16 sp o más), cursor tinta y, con el foco puesto, un aro cielo con un filo azul oscuro por dentro (para que se vea también sobre crema).
 * [modifier] va al campo mismo (ahí van el `testTag` y el texto que escribe la prueba); la losa llena el ancho. [placeholder] se ve mientras está vacío.
 */
@Composable
fun ClayTextField(
  value: String,
  onValueChange: (String) -> Unit,
  modifier: Modifier = Modifier,
  placeholder: String? = null,
  fontSize: TextUnit = 18.sp,
  singleLine: Boolean = true,
  keyboardOptions: KeyboardOptions = KeyboardOptions.Default,
  keyboardActions: KeyboardActions = KeyboardActions.Default,
  minHeight: Dp = 56.dp
) {
  val source = remember { MutableInteractionSource() }
  val focused by source.collectIsFocusedAsState()
  ClaySlab(
    SlabColor.CREAM,
    Modifier.fillMaxWidth().drawWithContent {
      drawContent()
      if (focused) {
        val h = size.height - 6.dp.toPx()                                    // sin los 6 dp que la losa reserva abajo para su sombra
        val r = CornerRadius(22.dp.toPx())
        drawRoundRect(Clay.Sky, Offset(1.5.dp.toPx(), 1.5.dp.toPx()), Size(size.width - 3.dp.toPx(), h - 3.dp.toPx()), r, style = Stroke(3.dp.toPx()))
        drawRoundRect(PanelAccent, Offset(3.5.dp.toPx(), 3.5.dp.toPx()), Size(size.width - 7.dp.toPx(), h - 7.dp.toPx()), CornerRadius(20.dp.toPx()), style = Stroke(1.5.dp.toPx()))
      }
    },
    size = SlabSize.MEDIUM, forcePressed = true, onClick = null
  ) {
    Box(Modifier.fillMaxWidth().padding(horizontal = 16.dp)) {
      // El campo mismo ocupa todo el alto de la losa (minHeight), así un toque en cualquier parte de la losa le da el foco; el texto va centrado a lo alto.
      BasicTextField(
        value = value, onValueChange = onValueChange, singleLine = singleLine,
        textStyle = TextStyle(fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = fontSize, color = Clay.Ink),
        cursorBrush = SolidColor(Clay.Ink),
        interactionSource = source,
        keyboardOptions = keyboardOptions, keyboardActions = keyboardActions,
        modifier = modifier.fillMaxWidth().heightIn(min = minHeight),
        decorationBox = { inner ->
          Box(Modifier.fillMaxWidth().heightIn(min = minHeight).padding(vertical = 10.dp), contentAlignment = Alignment.CenterStart) {
            if (value.isEmpty() && placeholder != null) Text(placeholder, color = Clay.InkMuted, fontFamily = AppFamily, fontSize = fontSize)
            inner()
          }
        }
      )
    }
  }
}
