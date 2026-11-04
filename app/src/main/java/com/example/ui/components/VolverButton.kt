package com.example.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.size
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import com.example.ui.theme.NubiTokens

/** Lo que dice el lector de pantalla del «‹» de volver de Tú. */
const val VOLVER_LABEL = "Volver"

/**
 * El «‹» de volver (Tarea 225): arriba a la izquierda de Tú cuando se abre a pantalla completa. 48 dp que se tocan (el mínimo), con la flecha dibujada de línea (un chevron de 3 dp con las puntas redondas) en `OnNight`, sin recuadro, como las flechas de los paneles.
 * Para el lector es «Volver», un botón; al tocarlo da el tic del kit. Atrás hace lo mismo (lo pone quien lo usa).
 */
@Composable
fun VolverButton(onClick: () -> Unit, modifier: Modifier = Modifier) {
  val tap = rememberKitTap()
  val source = remember { MutableInteractionSource() }
  Canvas(
    modifier
      .size(48.dp)
      .clickable(interactionSource = source, indication = null, role = Role.Button) { tap(); onClick() }
      .semantics { contentDescription = VOLVER_LABEL }
      .testTag("tu_back")
  ) {
    val w = 3.dp.toPx()
    val h = size.height; val cx = size.width / 2f
    val path = Path().apply {
      moveTo(cx + 5.dp.toPx(), h / 2f - 12.dp.toPx())
      lineTo(cx - 5.dp.toPx(), h / 2f)
      lineTo(cx + 5.dp.toPx(), h / 2f + 12.dp.toPx())
    }
    drawPath(path, NubiTokens.OnNight, style = Stroke(w, cap = StrokeCap.Round, join = StrokeJoin.Round))
  }
}
