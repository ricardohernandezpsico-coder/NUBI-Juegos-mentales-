package com.example.ui.screens

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.Stable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.layout.boundsInWindow
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.positionInWindow
import com.example.R
import com.example.data.PlaceSheetUi
import com.example.ui.components.LockTabSwipe
import com.example.ui.components.NUBI_ASPECT

/**
 * Dónde está la Nubi de una pestaña (Juegos: la de «¿Qué jugamos hoy?»; Avance: la de «Tu semana»), para que la lámina de un área vuele desde ahí y se pose en ella (Tarea 155). La Nubi de la pestaña se marca con [markNubi]; [bounds] es su caja en la ventana (px) o null si no
 * hay (no se ha dibujado o salió de la vista); mientras vuela hacia la lámina se esconde ([alpha] = 0), como la de la isla en Hoy.
 */
@Stable
internal class NubiAnchor {
  var bounds by mutableStateOf<Rect?>(null)
  /** Cuánto se ve la Nubi de la pestaña (0 mientras vuela a la lámina). Es un ESTADO: el `graphicsLayer` que la lee se vuelve a dibujar cuando la pestaña cambia esta función. */
  var alpha: () -> Float by mutableStateOf({ 1f })
}

/** Marca esta Nubi (un `NubiClay`) como la de la pestaña: guarda su caja y se esconde mientras vuela a la lámina. */
internal fun Modifier.markNubi(anchor: NubiAnchor): Modifier = this
  .onGloballyPositioned { c -> anchor.bounds = if (c.isAttached) c.boundsInWindow() else null }
  .graphicsLayer { alpha = anchor.alpha() }

/**
 * La lámina de un área sobre una pestaña (Juegos o Avance; Tarea 155): [page] es la pestaña y, encima, [PlaceLayer] con el velo, la lámina y Nubi volando desde [anchor]. Mientras está abierta las pestañas no se deslizan (y la barra se retira) y Atrás la cierra.
 * La pestaña es dueña de [fx] (el estado de las animaciones, `PlaceFx(looks = false)`: aquí no hay isla ni edificio que mirar) y de [places] (una lámina por área, [rememberAreaPlaces]); [onClose] = X, velo o Atrás: la pestaña cierra con `fx.close` y limpia lo suyo.
 * [showChart] = el gráfico de las 4 semanas (Avance).
 */
@Composable
internal fun AreaSheetScaffold(
  fx: PlaceFx,
  places: Map<String, PlaceSheetUi>,
  anchor: NubiAnchor,
  showChart: Boolean,
  onClose: () -> Unit,
  onPlay: (String) -> Unit,
  onGame: (String) -> Unit,
  modifier: Modifier = Modifier,
  page: @Composable () -> Unit
) {
  var rootOffset by remember { mutableStateOf(Offset.Zero) }
  var rootHeight by remember { mutableStateOf(0f) }
  if (fx.active) {
    LockTabSwipe()                       // las pestañas no se deslizan con la lámina abierta (y la barra se retira)
    BackHandler(onBack = onClose)        // Atrás la cierra
  }
  Box(modifier.fillMaxSize().onGloballyPositioned { rootOffset = it.positionInWindow(); rootHeight = it.size.height.toFloat() }) {
    page()
    // La caja del CUERPO de Nubi (la imagen que vuela mide ancho × ancho / NUBI_ASPECT) en las coordenadas de esta caja; si no está del todo a la vista, no hay de dónde volar
    val rest = anchor.bounds?.let { b ->
      val left = b.left - rootOffset.x; val top = b.top - rootOffset.y
      Rect(left, top, left + b.width, top + b.width / NUBI_ASPECT)
    }?.takeIf { it.top >= 0f && it.bottom <= rootHeight }
    anchor.alpha = { if (rest != null && fx.flight.value > 0f) 0f else 1f }
    PlaceLayer(
      fx = fx,
      place = fx.key?.let { k -> places.values.firstOrNull { it.key == k } },
      rest = rest,
      onClose = onClose,
      onPlay = onPlay,
      onGame = onGame,
      nubiRes = R.drawable.nubi_3d,
      showChart = showChart
    )
  }
}
