package com.example

import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.graphicsLayer
import com.example.ui.components.BoardBackground
import com.example.ui.components.NubiMotion
import com.example.ui.components.hiddenUntil
import com.example.ui.components.rememberReduceMotion
import com.example.viewmodel.StartupStage
import kotlinx.coroutines.delay

/** Tope de la espera de los datos al arrancar: pasado este tiempo desde el primer cuadro, entra lo que haya (nunca deja la app sin contenido). */
internal const val STARTUP_CAP_MS = 1000L

/**
 * El arranque en frío de la app (Tarea 192, parte 2). Los `StateFlow` del repositorio nacen vacíos (sin partidas, sin perfil, sin los tiempos del Contrarreloj) y Room los llena unos milisegundos después: el primer cuadro de Hoy salía con la racha y la semana de una persona nueva
 * («Tu racha empieza con tu próxima partida», «0 de 4 días de tu meta») y, de 0,1 a 0,4 s después, con los datos de verdad (medido en el emulador: 4 a 5 cuadros); y, si el perfil tampoco había llegado, la app normal ni siquiera sabía si mostrar Hoy o el Primer vuelo.
 *
 * La primera versión sostenía el primer dibujo con un `OnPreDrawListener` (lo que recomienda Android para alargar la pantalla de inicio). Los cuadros del emulador la desmintieron: en 3 de 7 arranques el sistema quitaba la pantalla de inicio ANTES de que la app tuviera su primer cuadro y quedaban de 3 a 6 cuadros
 * (de 0,2 a 0,4 s) con la ventana vacía, sin ningún color; sin el portón no pasó en 7 de 7. Por eso acá NO se detiene el dibujo: el primer cuadro sale enseguida, con el cielo de siempre ([BoardBackground]: los luceros ya están y la transición desde la pantalla de inicio es la de antes).
 *
 * Tres tiempos ([StartupStage]): mientras el perfil no llega, solo el cielo; con el perfil ([StartupStage.PROFILE]) el contenido (Hoy o el Primer vuelo) se compone, pero INVISIBLE, sin tocar y fuera del lector de pantalla, para que la composición, que es lo que más tarda, corra mientras llegan las partidas y el
 * Contrarreloj; con todo al día ([StartupStage.READY]) entra con un fundido de [NubiMotion.STATE_MS] (con «quitar animaciones», de golpe). Ningún cuadro visible tiene datos a medias.
 *
 * - **Tope de [STARTUP_CAP_MS] desde el primer cuadro:** si la base tarda más (o falla), el contenido se compone y se ve con lo que haya.
 * - **Con la app ya cargada** (la Activity se recrea, p. ej. al cambiar el tema del sistema) la etapa ya es [StartupStage.READY] y el contenido sale en el primer cuadro, sin fundido.
 */
@Composable
internal fun StartupGate(stage: StartupStage, capMs: Long = STARTUP_CAP_MS, content: @Composable () -> Unit) {
  var capped by remember { mutableStateOf(false) }
  LaunchedEffect(Unit) { delay(capMs); capped = true }
  val compose = stage != StartupStage.LOADING || capped
  val visible = stage == StartupStage.READY || capped
  val fade by animateFloatAsState(if (visible) 1f else 0f, NubiMotion.spec(rememberReduceMotion(), NubiMotion.STATE_MS), label = "arranque")
  Box(Modifier.fillMaxSize()) {
    BoardBackground()
    if (compose) Box(Modifier.fillMaxSize().hiddenUntil(visible).graphicsLayer { alpha = fade }) { content() }
  }
}
