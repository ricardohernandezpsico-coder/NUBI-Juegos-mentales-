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
 * - **El juego elegido** lo guarda el ViewModel (`contrarrelojGame`; null = el de hoy) y no se cierra al cambiar de pestaña. **Sin Nubilus Plus se muestra siempre el de hoy**, aunque el ViewModel guarde otro (el interruptor de depuración, o un Plus que venció): los otros tres se ven en gris.
 * - **«Jugar · 2 minutos»** lanza el juego elegido con la pista de hoy (`startTimeTrial(..., fromTab = true)`): al terminar, el final vuelve aquí y no a Hoy. Sin Plus, la regla de partidas (Tarea 218): 3 al día del juego de hoy, contadas de lo guardado; con partidas, «Te quedan N partidas de hoy.» bajo «Jugar»; sin ellas, «Hoy ya jugaste tus 3 partidas.» en lugar de «Jugar» y una fila gris con candado bajo la losa gratis.
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
  val league by viewModel.league.collectAsState()
  val lang = LocalAppLanguage.current
  var clock by remember { mutableStateOf(System.currentTimeMillis()) }
  LaunchedEffect(Unit) {
    while (true) {
      kotlinx.coroutines.delay(30_000L)
      clock = System.currentTimeMillis()
    }
  }
  val dayKey = remember(clock) { viewModel.todayKey() }
  val today = remember(dayKey) { TimeTrialDay.gameFor(dayKey) }
  // Tarea 218: sin Plus, las partidas de hoy que quedan del juego de hoy, de lo GUARDADO; con Plus, sin tope (null)
  val gamesLeft = if (plus) null else TimeTrialDay.gamesLeft(results, dayKey, today)
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
      gamesLeft = gamesLeft,
      onLocked = { sheet = true },
      tuButton = { m -> com.example.ui.components.TuHeaderButton(viewModel, m) },          // el botón «Tú» en la cabecera de la página (Tarea 225b)
      league = league                                                                      // la liga de la semana (Tarea 229)
    )
  }
  // Con qué se compara ESTA persona, para la hoja (Tarea 205c): sin franja de edad no hay comparación, así que la hoja no le promete una referencia que nunca verá (`LockSheetHost`, que también usan Hoy y la ficha)
  if (sheet) LockSheetHost(viewModel, onDismiss = { sheet = false })
}
