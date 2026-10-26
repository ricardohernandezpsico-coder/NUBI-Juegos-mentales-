package com.example.ui.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material3.LocalContentColor
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.selected
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayCard

/** El azul de la marca, oscurecido para texto e íconos sobre una losa crema (contraste ≥ 4,5:1; el `TealPrimary` de la noche da 2,3:1 sobre crema). */
val PanelAccent = Color(0xFF2B57B8)

/**
 * Un panel grande de plasticina crema (losa de 30 dp; Tarea 90) para Opciones y sus avisos: todo lo de adentro (texto, íconos, divisores, campos) toma los colores de una superficie CLARA (tinta sobre crema), no los de la noche.
 * El contenido va en una columna con [padding] alrededor.
 */
@Composable
fun ClayPanel(modifier: Modifier = Modifier, padding: Dp = 20.dp, verticalSpacing: Dp = 0.dp, content: @Composable ColumnScope.() -> Unit) {
  ClayCard(modifier, Clay.Cream, SlabSize.LARGE, contentPadding = 0.dp) {
    CreamSurface {
      Column(Modifier.fillMaxWidth().padding(padding), verticalArrangement = Arrangement.spacedBy(verticalSpacing), content = content)
    }
  }
}

/** Los colores de una superficie crema para Material: tinta como texto, tinta suave como secundario, el azul oscuro como acento. */
@Composable
fun CreamSurface(content: @Composable () -> Unit) {
  val base = MaterialTheme.colorScheme
  val scheme = base.copy(
    primary = PanelAccent, onPrimary = Color.White,
    surface = Clay.Cream, onSurface = Clay.Ink, onSurfaceVariant = Clay.InkMuted,
    surfaceVariant = Clay.Ink.copy(alpha = 0.10f), outline = Clay.InkMuted, outlineVariant = Clay.Ink.copy(alpha = 0.25f),
    error = Color(0xFFB3261E)
  )
  MaterialTheme(colorScheme = scheme, typography = MaterialTheme.typography, shapes = MaterialTheme.shapes) {
    CompositionLocalProvider(LocalContentColor provides Clay.Ink, content = content)
  }
}

/**
 * Una casilla de elección de plasticina (chips de idioma, de edad, de hora, etc.): losa chica crema; la ELEGIDA va en lima, con su ✓, su texto en negrita y QUIETA (elegido no es presionado: la imagen hundida solo se ve mientras el dedo está encima).
 * Siempre reserva el lugar del ✓ para que elegir no cambie su tamaño. Mide 48 dp de alto como mínimo. Semántica: botón de opción, elegido o no.
 */
@Composable
fun ClayChoice(
  selected: Boolean,
  onClick: () -> Unit,
  modifier: Modifier = Modifier,
  minHeight: Dp = 48.dp,
  /** Botón de opción (una sola) o casilla de verificación (varias). */
  role: Role = Role.RadioButton,
  content: @Composable RowScope.() -> Unit
) {
  ClaySlab(
    color = if (selected) SlabColor.LIME else SlabColor.CREAM,
    size = SlabSize.SMALL,
    onClick = onClick,
    role = role,
    contentAlignment = Alignment.CenterStart,
    modifier = modifier.semantics { this.selected = selected }
  ) {
    Row(Modifier.heightIn(min = minHeight).padding(start = 12.dp, end = 8.dp, top = 6.dp, bottom = 6.dp), verticalAlignment = Alignment.CenterVertically) {
      content()
      Spacer(Modifier.width(4.dp))
      Box(Modifier.size(20.dp), contentAlignment = Alignment.Center) { if (selected) KitGlyph(Glyph.CHECK, size = 20.dp) }
    }
  }
}

/** Una fila de [ClayChoice] que pasa a la línea de abajo cuando no caben (letra grande o pantalla angosta). */
@OptIn(ExperimentalLayoutApi::class)
@Composable
fun ClayChoiceRow(modifier: Modifier = Modifier, content: @Composable () -> Unit) {
  FlowRow(modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp), verticalArrangement = Arrangement.spacedBy(2.dp)) { content() }
}

/**
 * El aviso de confirmación (reemplaza al `AlertDialog` de Material): un panel de plasticina crema con el título, el texto y dos botones: [dismissText] en crema y [confirmText] en [confirmColor] (coral cuando borra algo).
 * Un toque fuera lo cierra. Los dos botones miden 56 dp y, con letra grande, quedan uno debajo del otro.
 */
@Composable
fun ClayDialog(
  title: String,
  text: String,
  confirmText: String,
  onConfirm: () -> Unit,
  dismissText: String,
  onDismiss: () -> Unit,
  confirmColor: Color = Clay.Sun
) {
  Dialog(onDismissRequest = onDismiss) {
    ClayCard(Modifier.fillMaxWidth().padding(8.dp), Clay.Cream, SlabSize.LARGE, contentPadding = 0.dp) {
      Column(Modifier.fillMaxWidth().padding(22.dp)) {
        Text(title, color = Clay.Ink, fontSize = 22.sp, fontWeight = FontWeight.Bold, fontFamily = com.example.ui.theme.TitleFamily)
        Text(text, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 10.dp))
        Column(Modifier.padding(top = 18.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
          ClayButton(confirmText, onConfirm, color = confirmColor, modifier = Modifier.fillMaxWidth())
          ClayButton(dismissText, onDismiss, color = Clay.Cream, modifier = Modifier.fillMaxWidth())
        }
      }
    }
  }
}
