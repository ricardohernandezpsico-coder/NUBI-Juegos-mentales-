package com.example.ui.screens

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.key
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import com.example.data.FirstFlight
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.model.AgeBand
import com.example.ui.UnityGameHost
import com.example.ui.components.CosmosBackground
import com.example.viewmodel.NeuroVidaViewModel

/**
 * Aloja «Primer vuelo con Nubi» (`data/FirstFlight.kt`): el cielo de la app, la pantalla del paso y, cuando toca jugar, el puente con Unity
 * ([UnityGameHost], el mismo de siempre). Se muestra mientras `ageBand == null` (la primera vez) o haya un recorrido en curso, y reemplaza
 * al onboarding de 9 pantallas y a la evaluación aparte.
 */
@Composable
fun FirstFlightHost(viewModel: NeuroVidaViewModel) {
  val flight by viewModel.flight.collectAsState()
  val activeGame by viewModel.activeGame.collectAsState()
  val settings by viewModel.userSettings.collectAsState()
  val dailySession by viewModel.dailySession.collectAsState()
  // Sin nada guardado todavía (la primera vez): el principio del recorrido completo; no se guarda hasta la primera respuesta.
  val state = flight ?: remember { FlightState(FlightMode.FULL) }

  val actions = remember(viewModel) {
    FlightActions(
      onContinue = viewModel::flightContinue,
      onBack = viewModel::flightBack,
      onName = viewModel::flightSetName,
      onAge = viewModel::flightSetAge,
      onGoal = viewModel::flightToggleGoal,
      onFocus = viewModel::flightSetFocus,
      onColor = viewModel::flightSetColorVision,
      onDays = viewModel::flightSetDays,
      onHour = viewModel::flightSetHour,
      onPlay = viewModel::flightPlay,
      onSkipGames = viewModel::flightSkipGames,
      onConfirmReminder = viewModel::flightConfirmReminder
    )
  }

  Box(Modifier.fillMaxSize()) {
    CosmosBackground()
    val session = activeGame
    if (session != null) {
      Box(Modifier.fillMaxSize().safeDrawingPadding()) {
        // key: cada sesión es su propio grupo de composición (no hereda el estado guardado de la anterior).
        key(session.sessionToken) {
          UnityGameHost(
            session = session,
            userId = settings.id.toString(),
            ageBand = state.age ?: settings.ageBand ?: AgeBand.ADULT,
            soundEnabled = settings.soundEnabled,
            onLaunched = { id -> viewModel.onUnityLaunched(id) },
            onHostResumed = { viewModel.onHostResumed() }
          )
        }
      }
    } else {
      FirstFlightScreen(state = state, todayGames = dailySession.gameIds, actions = actions)
    }
  }
}
