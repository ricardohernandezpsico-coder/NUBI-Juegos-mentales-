package com.example.ui.components

import android.view.HapticFeedbackConstants
import android.view.View
import androidx.compose.runtime.Composable
import androidx.compose.runtime.compositionLocalOf
import androidx.compose.runtime.remember
import androidx.compose.ui.platform.LocalView

/**
 * La háptica del kit (Tarea 123; Fable §3.5): un tic al APOYAR el dedo en una losa o un disco del kit, igual que el golpecito de madera del kit de Unity (`GameFeel.Tap`). Lo hacen Duolingo (al acertar) y Headspace (al elegir).
 *
 * Se condiciona a «Vibración y respuesta háptica» de Ajustes: `NeuroVidaApp` fija [LocalHapticsEnabled] con ese ajuste. Se usa `View.performHapticFeedback` (que además respeta el ajuste de «vibrar al tocar» del sistema) con `CONTEXT_CLICK` (un clic
 * suave, desde Android 6; Compose 1.7 todavía no trae ese tipo). Al completar la ruta de hoy, en Android 11 o más, `CONFIRM` (un doble tic de «listo»).
 */
val LocalHapticsEnabled = compositionLocalOf { true }

/** Un tic para quien llama: no hace nada si la vibración de la app está apagada. */
@Composable
fun rememberKitTap(): () -> Unit {
  val view = LocalView.current
  val enabled = LocalHapticsEnabled.current
  return remember(view, enabled) { { if (enabled) KitHaptics.tap(view) } }
}

object KitHaptics {
  /** El tic de apoyar el dedo. */
  fun tap(view: View) {
    view.performHapticFeedback(HapticFeedbackConstants.CONTEXT_CLICK)
  }

}
