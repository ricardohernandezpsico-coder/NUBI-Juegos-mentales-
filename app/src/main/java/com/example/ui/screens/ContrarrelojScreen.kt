package com.example.ui.screens

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import com.example.data.Comparison
import com.example.data.TimeTrialDay
import com.example.ui.i18n.LocalAppLanguage
import com.example.viewmodel.NeuroVidaViewModel
import com.example.viewmodel.TopPanel

/**
 * La pestaña «Contrarreloj» (Tarea 215): reúne lo que la página necesita del ViewModel y la dibuja ([ContrarrelojPage]); la hoja del candado ([PlusLockSheet]) vive aquí, porque la abre cualquier cosa en gris de la pestaña. Antes la página era un hijo de `ProgressScreen` (Tareas 199 y 203).
 *
 * - **El juego elegido** lo guarda el ViewModel (`contrarrelojGame`; null = el de hoy) y no se cierra al cambiar de pestaña. **Sin Nubi Plus se muestra siempre el de hoy**, aunque el ViewModel guarde otro (el interruptor de depuración, o un Plus que venció): los otros tres se ven en gris.
 * - **«Jugar · 2 minutos»** lanza el juego elegido con la pista de hoy (`startTimeTrial(..., fromTab = true)`): al terminar, el final vuelve aquí y no a Hoy. La cuenta de partidas gratis llega con la Tarea 3 de la pestaña; hoy no hay tope.
 * - **El día de hoy** se vuelve a pedir cada 30 s (el mismo reloj de Hoy): a medianoche el juego marcado «Hoy» cambia solo.
 * - La comparación disponible en ESTA build es la REFERENCIA ESTIMADA del APK (Tarea 205; también en la tienda) o, en depuración, los datos de muestra que elija «[Debug] Comparación»; null si no hay con qué comparar (la referencia la carga el ViewModel fuera del hilo principal: null un instante al arrancar).
 */
@Composable
fun ContrarrelojScreen(viewModel: NeuroVidaViewModel, modifier: Modifier = Modifier) {
  val results by viewModel.timeTrialResults.collectAsState()
  val plus by viewModel.plus.collectAsState()
  val chosen by viewModel.contrarrelojGame.collectAsState()
  val compareWorld by viewModel.compareWorld.collectAsState()
  val estimatedReference by viewModel.estimatedReference.collectAsState()
  val ageRange by viewModel.ageRange.collectAsState()
  val lang = LocalAppLanguage.current
  var clock by remember { mutableStateOf(System.currentTimeMillis()) }
  LaunchedEffect(Unit) {
    while (true) {
      kotlinx.coroutines.delay(30_000L)
      clock = System.currentTimeMillis()
    }
  }
  val today = remember(clock) { TimeTrialDay.gameFor(viewModel.todayKey()) }
  val selected = if (plus) chosen ?: today else today
  val now = remember(results, clock) { System.currentTimeMillis() }
  val compare = remember(compareWorld, estimatedReference) { Comparison.sourceFor(com.example.BuildConfig.DEBUG, compareWorld, estimatedReference) }
  var sheet by remember { mutableStateOf(false) }
  Column(modifier.fillMaxSize().padding(bottom = com.example.ui.components.LocalBarInset.current)) {      // la franja de la barra de pestañas queda libre
    ContrarrelojPage(
      results = results, selected = selected, onSelect = viewModel::selectContrarrelojGame, now = now, lang = lang, compare = compare, ageRange = ageRange,
      onOpenSource = { viewModel.openTopPanel(TopPanel.REFERENCIA) },
      plus = plus, today = today,
      onPlay = { viewModel.startTimeTrial(selected, fromTab = true) },
      onLocked = { sheet = true }
    )
  }
  if (sheet) {
    // Con qué se compara ESTA persona, para la hoja (Tarea 205c): sin franja de edad (o de 13 a 17 años) no hay comparación, así que la hoja no le promete una referencia que nunca verá; de 13 a 17 años lo dice (Tarea 209)
    PlusLockSheet(
      onDismiss = { sheet = false }, kind = Comparison.kindFor(compare, ageRange), minor = Comparison.isMinorLocked(ageRange),
      onSource = { sheet = false; viewModel.openTopPanel(TopPanel.REFERENCIA) }
    )
  }
}
