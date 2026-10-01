package com.example.ui.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.example.bridge.UnityGameLauncher
import com.example.model.AgeBand
import com.example.viewmodel.NeuroVidaViewModel

/** Un juego de Unity lanzado suelto desde las herramientas de prueba (sin sesión: la partida solo se guarda). */
private data class DebugGame(val id: String, val label: String, val level: Int, val timed: Boolean)

private val DebugGames = listOf(
  DebugGame("secuencia", "Secuencia Lumínica", level = 1, timed = false),
  DebugGame("parejas", "Parejas Ocultas", level = 1, timed = false),
  DebugGame("stroop", "Tinta o Palabra (Reto 60 s)", level = 4, timed = true), // desde el nivel 4 la regla cambia
  DebugGame("comparacion", "Comparación (Reto 60 s)", level = 2, timed = true),
  DebugGame("cambiochip", "Cambio de Chip (Reto 60 s)", level = 2, timed = true),
  DebugGame("rutatesoro", "Ruta del Tesoro (con reloj)", level = 1, timed = true),
  DebugGame("series", "Detective de Series (Reto 120 s)", level = 1, timed = true),
  DebugGame("calculo", "Cálculo Sereno (Reto 90 s)", level = 1, timed = true),
  DebugGame("anagramas", "Anagramas (Reto 120 s)", level = 1, timed = true),
  DebugGame("piloto", "Piloto Estelar (Reto 90 s)", level = 1, timed = true),
  DebugGame("radar", "Radar (Reto 90 s)", level = 1, timed = true),
  DebugGame("satelites", "Satélites (Reto 120 s)", level = 1, timed = true),
  DebugGame("freno", "Freno de Emergencia (Reto 120 s)", level = 1, timed = true),
  DebugGame("aterrizaje", "Aterrizaje Lunar (Reto 120 s)", level = 1, timed = true),
  DebugGame("meteoros", "Lluvia de meteoros (Reto 120 s)", level = 1, timed = true),
  DebugGame("disparate", "¿Verdad o disparate? (Reto 120 s)", level = 1, timed = true),
  DebugGame("acoplamiento", "Acoplamiento (Reto 120 s)", level = 1, timed = true),
  DebugGame("trafico", "Tráfico Estelar (Reto 120 s)", level = 1, timed = true),
  DebugGame("bitacora", "Bitácora de Misión (completa, con patrulla)", level = 1, timed = false),
  DebugGame("rumbo", "Rumbo a Casa (Reto 150 s)", level = 1, timed = true),
  DebugGame("correo", "Correo Estelar (vuelo de 150 s)", level = 2, timed = true)
)

/**
 * Herramientas de prueba al final de Ajustes. Solo se muestran en builds de depuración (`BuildConfig.DEBUG`):
 * ver celebraciones y el onboarding sin tener que provocarlos, y abrir cada juego de Unity directo.
 */
@Composable
fun DebugTools(viewModel: NeuroVidaViewModel, userId: Long, ageBand: AgeBand) {
  val context = LocalContext.current
  Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
    DebugButton("[Debug] Ver celebración de ascenso de liga", "btn_debug_promotion") { viewModel.debugShowPromotion() }
    DebugButton("[Debug] Ver celebración de logro", "btn_debug_achievement") { viewModel.debugShowAchievement() }
    DebugButton("[Debug] Ver el onboarding otra vez", "btn_debug_onboarding") { viewModel.debugRestartOnboarding() }
    // La misión del día de Bitácora, fuera de la sesión: la transmisión y, sin esperar los 10 minutos, el informe.
    DebugButton("[Debug] Bitácora: recibir transmisión", "btn_debug_mission_encode") { viewModel.startMissionTransmission() }
    DebugButton("[Debug] Bitácora: informe ya (sin esperar)", "btn_debug_mission_recall") { viewModel.startMissionReport() }
    DebugGames.forEach { g ->
      DebugButton("[Debug] Probar ${g.label}", "btn_debug_unity_${g.id}") {
        context.startActivity(
          UnityGameLauncher.buildGameIntent(
            context = context,
            gameId = g.id,
            userId = userId.toString(),
            level = g.level,
            baseIntensity = 0,
            timed = g.timed,
            ageBand = ageBand
          )
        )
      }
    }
  }
}

@Composable
private fun DebugButton(text: String, tag: String, onClick: () -> Unit) {
  OutlinedButton(onClick = onClick, shape = RoundedCornerShape(14.dp), modifier = Modifier.fillMaxWidth().testTag(tag)) {
    Text(text)
  }
}
