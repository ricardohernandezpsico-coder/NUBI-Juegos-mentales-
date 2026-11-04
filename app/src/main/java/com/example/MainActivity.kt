package com.example

import android.content.Intent
import android.os.Bundle
import androidx.activity.addCallback
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.graphics.Color
import com.example.bridge.NativeReceiver
import com.example.bridge.UnityGameLauncher
import com.example.games.*
import com.example.ui.components.hiddenUntil
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.screens.HomeScreen
import com.example.ui.screens.ProgressScreen
import com.example.ui.screens.TuScreen
import com.example.model.ThemeMode
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.example.viewmodel.NeuroVidaViewModel

class MainActivity : ComponentActivity() {
  private val viewModel: NeuroVidaViewModel by viewModels()

  override fun onCreate(savedInstanceState: Bundle?) {
    super.onCreate(savedInstanceState)
    if (savedInstanceState == null) {
      handleGameReturn(intent)
      handleStartSession(intent)
    }
    // Atrás en la pantalla raíz: la app pasa a segundo plano en vez de cerrarse. Unity queda vivo DEBAJO de esta
    // Activity entre partidas (ver UnityGameHost); cerrarla lo dejaría a la vista. Los BackHandler de Compose
    // (diálogos, Ajustes) se registran después y tienen prioridad.
    onBackPressedDispatcher.addCallback(this) { moveTaskToBack(true) }
    enableEdgeToEdge(
      statusBarStyle = androidx.activity.SystemBarStyle.dark(android.graphics.Color.TRANSPARENT),
      navigationBarStyle = androidx.activity.SystemBarStyle.dark(android.graphics.Color.TRANSPARENT)
    )
    setContent {
      val userSettings by viewModel.userSettings.collectAsState()
      val darkTheme = when (userSettings.themeMode) {
        ThemeMode.LIGHT -> false
        ThemeMode.DARK -> true
        ThemeMode.SYSTEM -> isSystemInDarkTheme()
      }
      CompositionLocalProvider(
        LocalAppLanguage provides userSettings.language
      ) {
        NeuroVidaTheme(darkTheme = darkTheme) {
          // El cielo sale en el primer cuadro, el contenido se compone invisible con el perfil y entra con un fundido cuando la base ya respondió: sin racha ni semana de una persona nueva por un instante (Tarea 192, parte 2; ver StartupGate).
          val startupStage by viewModel.startupStage.collectAsState()
          StartupGate(stage = startupStage) {
            // Primera experiencia («Primer vuelo con Nubi», ver FirstFlightHost): mientras no se resuelva `ageBand`, o haya un
            // recorrido en curso (el inicio retomado tras un cierre de Android, o «Hacer la evaluación»), no se monta la app normal.
            val flight by viewModel.flight.collectAsState()
            if (userSettings.ageBand == null || flight != null) {
              com.example.ui.screens.FirstFlightHost(viewModel)
            } else {
              NeuroVidaApp(viewModel = viewModel)
            }
          }
        }
      }
    }
  }

  /** Unity trae la app al frente al terminar o salir de una partida (ver `NativeReceiver.returnToApp`). */
  override fun onNewIntent(intent: Intent) {
    super.onNewIntent(intent)
    setIntent(intent)
    handleGameReturn(intent)
    handleStartSession(intent)
  }

  /** "Jugar ahora" del recordatorio diario (ver `CognitiveReminderWorker`): arranca la sesión de hoy. */
  private fun handleStartSession(intent: Intent?) {
    if (intent?.getBooleanExtra(EXTRA_START_SESSION, false) != true) return
    intent.removeExtra(EXTRA_START_SESSION)
    viewModel.startDailySessionFromReminder()
  }

  companion object {
    const val EXTRA_START_SESSION = "neurovida_start_session"
  }

  private fun handleGameReturn(intent: Intent?) {
    if (intent?.getBooleanExtra(NativeReceiver.EXTRA_RETURN_FROM_GAME, false) != true) return
    intent.removeExtra(NativeReceiver.EXTRA_RETURN_FROM_GAME)
    viewModel.onReturnedFromGame(
      launchId = intent.getStringExtra(UnityGameLauncher.EXTRA_LAUNCH_ID),
      resultJson = intent.getStringExtra(NativeReceiver.EXTRA_JSON),
      paused = intent.getBooleanExtra(NativeReceiver.EXTRA_PAUSED, false)
    )
  }
}

@Composable
fun NeuroVidaApp(viewModel: NeuroVidaViewModel) {
  val userSettings by viewModel.userSettings.collectAsState()
  val currentTab by viewModel.currentTab.collectAsState()
  val activeGame by viewModel.activeGame.collectAsState()
  val lastResult by viewModel.lastResult.collectAsState()
  val guestTrial by viewModel.guestTrial.collectAsState()           // el final de invitado (Tarea 205, solo [Debug]): la marca, sin guardar nada
  val lastTimeTrial by viewModel.lastTimeTrial.collectAsState()      // el final del Contrarreloj (Tarea 177): con él, `lastResult` es la marca de ese Contrarreloj
  val modeNote by viewModel.modeNote.collectAsState()
  val atlas by viewModel.atlas.collectAsState()
  val starMeasures by viewModel.starMeasures.collectAsState()
  val rescateTotals by viewModel.rescateTotals.collectAsState()
  val acoplamientoTotals by viewModel.acoplamientoTotals.collectAsState()
  val aterrizajeTotals by viewModel.aterrizajeTotals.collectAsState()
  val lastResultDaily by viewModel.lastResultDaily.collectAsState()
  val dailySession by viewModel.dailySession.collectAsState()
  val promotion by viewModel.promotion.collectAsState()
  val streak by viewModel.currentStreak.collectAsState()
  val activity by viewModel.activityTimestamps.collectAsState()                       // cuándo se jugó: de ahí salen las farolas de la semana del resumen de la ruta (Tarea 198)
  val achievementQueue by viewModel.achievementQueue.collectAsState()
  val achievementUnlocks by viewModel.achievementUnlocks.collectAsState()
  val sessionSummary by viewModel.sessionSummary.collectAsState()
  val topPanel by viewModel.topPanel.collectAsState()
  val lang = com.example.ui.i18n.LocalAppLanguage.current
  val startupReady = viewModel.startupStage.collectAsState().value == com.example.viewmodel.StartupStage.READY          // con la app lista, `TabHost` va componiendo las pestañas que faltan mientras se mira Hoy
  // Las pestañas cambian SOLO al tocar la barra (Tarea 224, 4-nov: antes también se pasaban deslizando, y a las vecinas se iba deslizando) y siempre con el mismo fundido: vive en `TabHost`, y la barra marca la pestaña del ViewModel de una vez.
  // Tras la primera celebración de una partida, las siguientes (más logros) aparecen enseguida.
  var celebratedOne by remember(lastResult) { mutableStateOf(false) }
  // La guía de Nubi en la primera entrada a Hoy (Tarea 193, `HoyGuide`): dónde están los elementos que ilumina (los publican Hoy y la barra) y en qué paso va (vive en el ViewModel).
  val guideTargets = remember { com.example.ui.components.GuideTargets() }
  val guideStep by viewModel.guideStep.collectAsState()
  val plus by viewModel.plus.collectAsState()                 // Nubilus Plus (Tarea 199): el atajo «Todas tus marcas ›» del final del Contrarreloj, que desde la Tarea 215 abre la pestaña Contrarreloj

  androidx.compose.runtime.CompositionLocalProvider(com.example.ui.components.LocalGuideTargets provides guideTargets) {
  Box(modifier = Modifier.fillMaxSize()) {
    // El fondo de toda la app: el tablero azul marino de la isla (Tarea 82). Una sola capa detrás de las pestañas: al cambiar de pestaña no salta.
    // La lámina y los luceros, fijos detrás de las tres pestañas (Tarea 87: sin fundido por pestaña).
    com.example.ui.components.BoardBackground()
    // La barra de pestañas es una capa FIJA encima de las páginas (Tarea 123): no es el `bottomBar` de un Scaffold, así que NO se encoge cuando se abre una lámina o una ventana (que la tapan) y las páginas no saltan; tampoco hay
    // desvanecido de página (`PAGE_BOTTOM_FADE` se quitó): cada pestaña deja la franja de la barra libre con `LocalBarInset` y su contenido termina nítido sobre ella. La barra tiene su propio inset (las barras del sistema).
    // Tú (Tarea 225): a pantalla completa, sin la barra, con el mismo fundido de las pestañas; los paneles que se abren desde él van ENCIMA y al cerrarse vuelven a él.
    val tuOpen by viewModel.tuOpen.collectAsState()
    val tuFade = com.example.ui.components.rememberTabFade()
    val tuShown = com.example.ui.components.rememberFadeSwitch(tuOpen, tuFade)
    val barVisible = activeGame == null && lastResult == null && promotion == null && achievementQueue.isEmpty() && sessionSummary == null && topPanel == null && !tuShown
    // La guía solo vive sobre Hoy a la vista y sin nada encima.
    val hoyFree = barVisible && currentTab == AppTab.HOY
    val guideOpen = guideStep != null && hoyFree
    val density = androidx.compose.ui.platform.LocalDensity.current
    // Lo que mide la barra: se recuerda aunque se esconda (la página no cambia de tamaño al abrir o cerrar una ventana).
    var barHeightPx by androidx.compose.runtime.saveable.rememberSaveable { androidx.compose.runtime.mutableIntStateOf(0) }
    // Con la guía abierta, Hoy y la barra quedan fuera del lector de pantalla y sin toques (el velo las tapa): `hiddenUntil`.
    Box(modifier = Modifier.fillMaxSize().safeDrawingPadding().hiddenUntil(!guideOpen)) {
      androidx.compose.runtime.CompositionLocalProvider(
        com.example.ui.components.LocalBarInset provides with(density) { barHeightPx.toDp() },
        com.example.ui.components.LocalHapticsEnabled provides userSettings.hapticsEnabled      // «Vibración y respuesta háptica» de Ajustes: el tic del kit (KitHaptics)
      ) {
      Box(modifier = Modifier.fillMaxSize()) {
        // Las pestañas, el botón «Tú» y Tú mismo comparten el fundido de entrada y salida de Tú (Tarea 225).
        Box(Modifier.fillMaxSize().graphicsLayer { val s = tuFade.contentScale; alpha = tuFade.contentAlpha; scaleX = s; scaleY = s }) {
        // Content based on tab. Mientras hay un juego o un resultado encima no se compone: esas pantallas van
        // sobre el cielo transparente (se vería la pestaña detrás) y no deben dejar pasar toques a ella.
        // `KeepTabsState` (Tarea 209b): al esconder las pestañas guarda el deslizado de cada página y lo devuelve al volver (de «De dónde sale la referencia», de «En qué se basa Nubilus», de un juego...); sin él, la página volvía arriba.
        // Al volver, `TabHost` abre en la pestaña del ViewModel, sin fundido.
        com.example.ui.components.KeepTabsState(show = activeGame == null && lastResult == null && sessionSummary == null && topPanel == null && !tuShown) {
          com.example.ui.components.TabHost(currentTab = currentTab, modifier = Modifier.fillMaxSize(), warmUp = startupReady) { tab ->
            when (tab) {
              AppTab.HOY -> HomeScreen(viewModel = viewModel)
              AppTab.JUEGOS -> com.example.ui.screens.AllGamesScreen(viewModel = viewModel)
              AppTab.CONTRARRELOJ -> com.example.ui.screens.ContrarrelojScreen(viewModel = viewModel)
              AppTab.PROGRESO -> ProgressScreen(viewModel = viewModel)
            }
          }
        }
        // «Tú» (Tarea 225): el botón redondo de arriba a la derecha, UNO solo para las cuatro pestañas (siempre en el mismo lugar), y Tú a pantalla completa, sin la barra: su «‹» y Atrás vuelven a la pestaña de donde se abrió.
        if (barVisible) com.example.ui.components.TuButton(name = userSettings.name, onClick = { viewModel.openTu() }, modifier = Modifier.align(androidx.compose.ui.Alignment.TopEnd).padding(end = com.example.ui.components.TU_BUTTON_END))
        if (tuShown) com.example.ui.components.KeepTabsState(show = activeGame == null && lastResult == null && sessionSummary == null && topPanel == null) {
          androidx.compose.runtime.CompositionLocalProvider(com.example.ui.components.LocalBarInset provides 0.dp) {
            TuScreen(viewModel = viewModel, onBack = { viewModel.closeTu() })
          }
        }
        }

        // Los Ajustes, «En qué se basa Nubilus» y «Preguntas frecuentes», abiertos desde «Tú» (Tarea 158: el perfil es la pestaña «Tú» y los dos botones de arriba a la derecha se fueron; Tarea 185: «En qué se basa Nubilus»; Tarea 209: «Preguntas frecuentes»), y «De dónde sale la referencia», desde la página Contrarreloj de Avance y su hoja.
        if (activeGame == null && lastResult == null && sessionSummary == null) when (topPanel) {
          com.example.viewmodel.TopPanel.AJUSTES ->
            com.example.ui.screens.SettingsPanel(viewModel = viewModel, onClose = { viewModel.openTopPanel(null) })
          com.example.viewmodel.TopPanel.FUENTES ->
            com.example.ui.screens.FuentesPanel(onClose = { viewModel.openTopPanel(null) })
          com.example.viewmodel.TopPanel.PREGUNTAS ->
            com.example.ui.screens.PreguntasPanel(onClose = { viewModel.openTopPanel(null) })
          com.example.viewmodel.TopPanel.REFERENCIA ->
            com.example.ui.screens.ReferenciaPanel(onClose = { viewModel.openTopPanel(null) })
          null -> {}
        }

        // Resumen de la sesión del día recién terminada (sobre Hoy, una vez).
        if (activeGame == null && lastResult == null) sessionSummary?.let { summary ->
          androidx.activity.compose.BackHandler { viewModel.dismissSessionSummary() }
          com.example.ui.screens.SessionSummaryScreen(
            summary = summary,
            streak = streak,
            lamps = androidx.compose.runtime.remember(activity) { com.example.data.Week.lampsFor(activity, System.currentTimeMillis()) },
            lang = lang,
            onClose = { viewModel.dismissSessionSummary() }
          )
        }

        // Active Game Screen Overlay
        activeGame?.let { session ->
          Box(modifier = Modifier.fillMaxSize()) {
            // Los 9 juegos se juegan ahora en Unity (ver docs y UnityGameHost): se lanza la Activity de Unity con
            // esta sesión y el resultado vuelve por UnityResultBus al ViewModel.
            // key: cada sesión es su propio grupo de composición, así no hereda el estado guardado de la anterior.
            androidx.compose.runtime.key(session.sessionToken) {
              com.example.ui.UnityGameHost(
                session = session,
                userId = userSettings.id.toString(),
                ageBand = session.guestRange?.toBand() ?: userSettings.ageBand ?: com.example.model.AgeBand.ADULT,      // el invitado juega con la banda de la edad que eligió
                soundEnabled = userSettings.soundEnabled,
                onLaunched = { id -> viewModel.onUnityLaunched(id) },
                onHostResumed = { viewModel.onHostResumed() }
              )
            }
          }
        }

        // Last Result Screen Overlay
        lastResult?.let { (result, didLevelUp) ->
          // «[Debug] Contrarreloj de invitado» (Tarea 205): solo la marca; no se guardó nada
          val guest = guestTrial
          if (guest != null) {
            com.example.games.GuestTrialScreen(
              trial = guest,
              onAgain = { viewModel.startGuestTimeTrial(guest.gameId, guest.range ?: com.example.model.AgeRange.R45_54) },      // con la MISMA edad (sin ella, por un proceso que murió a mitad, la de 45 a 54)
              onDone = { viewModel.closeGameOrResult() }
            )
            return@let
          }
          val timeTrial = lastTimeTrial?.takeIf { result.isTimeTrial && it.result.gameId == result.gameId }
          val timeTrialResultsNow by viewModel.timeTrialResults.collectAsState()
          if (timeTrial != null) {
            // el Contrarreloj tiene su propio final: la marca, el récord, las últimas 5 y la mejor de la semana; «Otra vez» juega el mismo juego con una pista nueva (cada partida trae la suya, Tarea 210)
            com.example.games.TimeTrialResultScreen(
              outcome = timeTrial,
              onAgain = { viewModel.timeTrialAgain(timeTrial.result.gameId, timeTrial.result.day) },
              onDone = { viewModel.closeGameOrResult() },
              soundEnabled = userSettings.soundEnabled,
              plus = plus,
              onAllMarks = { viewModel.openContrarrelojFromFinal(timeTrial.result.gameId) },
              // sin partidas, «Otra vez» no sale (Tarea 218): la cuenta sale de lo guardado MÁS esta partida (Room puede traer la lista un instante después y «Otra vez» parpadearía: Tarea 219)
              canAgain = com.example.data.TimeTrialDay.canAgainAfter(plus, timeTrial.result, viewModel.todayKey(), timeTrialResultsNow),
              noMoreLine = com.example.data.TimeTrialDay.noMoreLine(timeTrial.result.day, viewModel.todayKey())
            )
            return@let
          }
          // cuántas partidas de este juego hubo antes de esta: «¿Por qué importa?» se ve en el final solo en las 3 primeras (de lo guardado, sin preferencias nuevas)
          val history by viewModel.gameHistory.collectAsState()
          val playsBefore = com.example.data.FinalesConSentido.playsBefore(history, result.gameId, result.timestamp)
          GameResultScreen(
            result = result,
            didLevelUp = didLevelUp,
            isDailyFlow = lastResultDaily,
            dailyCompletedCount = dailySession.completedCount,
            dailyTotalCount = 3,
            nextGameId = dailySession.nextGameId,
            playsBefore = playsBefore,
            modeNote = modeNote,
            atlas = atlas,
            starMeasures = starMeasures,
            rescateTotals = rescateTotals,
            acoplamientoTotals = acoplamientoTotals,
            aterrizajeTotals = aterrizajeTotals,
            soundEnabled = userSettings.soundEnabled,
            onPlayAgain = {
              viewModel.launchGame(result.gameId, customLevel = result.level, customTimed = result.timed, mode = result.playMode)
            },
            onContinue = {
              // En la sesión diaria, "Continuar" sigue el flujo: el siguiente juego o, al terminar, el resumen
              // de la sesión en Hoy (continueDailyFlow).
              if (lastResultDaily) {
                viewModel.continueDailyFlow()
              } else {
                viewModel.closeGameOrResult()
              }
            }
          )
        }

        // Ascenso de liga: se celebra encima del resultado (después de que se vio el puntaje). No depende de que
        // el resultado siga abierto: si el usuario lo cerró antes, la celebración aparece igual sobre la pestaña.
        promotion?.let { p ->
          com.example.ui.components.LeaguePromotionOverlay(
            promotion = p,
            onDismiss = { celebratedOne = true; viewModel.dismissPromotion() },
            streak = streak
          )
        }

        // Logros nuevos: después del ascenso (si lo hubo), de a uno.
        if (promotion == null) achievementQueue.firstOrNull()?.let { id ->
          val def = com.example.data.Achievements.byId(id)
          if (def == null) {
            LaunchedEffect(id) { viewModel.dismissAchievement() }
          } else {
            com.example.ui.components.AchievementOverlay(
              def = def,
              unlockedCount = com.example.data.Achievements.medalCount(achievementUnlocks.keys),          // el mismo número que «Medallas» del Perfil (Tarea 151, C8)
              totalCount = com.example.data.Achievements.all.size,
              streak = streak,
              onDismiss = { celebratedOne = true; viewModel.dismissAchievement() },
              delayMs = if (celebratedOne) 250L else 1700L,
              soundEnabled = userSettings.soundEnabled
            )
          }
        }
      }
      }
      // La barra: fija, encima de todo lo de las pestañas; con una lámina o ventana abierta no está (`barVisible`). Marca la pestaña del ViewModel de una vez, sin esperar al fundido.
      if (barVisible) {
        Box(Modifier.align(androidx.compose.ui.Alignment.BottomCenter).graphicsLayer { alpha = tuFade.contentAlpha }.onSizeChanged { if (it.height > 0) barHeightPx = it.height }) {
          com.example.ui.components.NeuroNavBar(current = currentTab, onSelect = { viewModel.setTab(it) })
        }
      }
    }
    // La guía de Nubi: encima de todo, también de la barra (el paso 3 la ilumina).
    com.example.ui.components.HoyGuideHost(viewModel = viewModel, targets = guideTargets, somethingAbove = !hoyFree)
  }
  }
}
