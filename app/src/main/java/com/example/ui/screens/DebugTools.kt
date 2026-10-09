package com.example.ui.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.example.bridge.UnityGameLauncher
import com.example.model.AgeBand
import com.example.viewmodel.NeuroVidaViewModel

/** Un juego de Unity lanzado suelto desde las herramientas de prueba (sin sesión: la partida solo se guarda). */
private data class DebugGame(
  val id: String, val label: String, val level: Int, val timed: Boolean,
  /** Abre el juego con su tutorial guiado aunque ya se haya jugado (para ver el flujo de «primera vez» sin borrar nada). */
  val tutorial: Boolean = false,
  /** Abre la versión corta del inicio («Tu punto de partida»: `assessment`), con su tutorial si [tutorial]: como la primera vez. */
  val shortVersion: Boolean = false,
  val tag: String = id,
  /** Solo Constelaciones: la etapa (1..18) con que empieza el primer cielo, para ver cada grupo sin jugar hasta él. */
  val conStage: Int = 0,
  /** Solo La estación de correo: la etapa (1..10) con que empieza la partida. */
  val mailStage: Int = 0,
  /** Solo Satélites: el nivel (1..12) con que empieza la partida, y la sorpresa que sale en todas las rondas (1 órbita, 2 nube, 4 rápidas). */
  val satStage: Int = 0,
  val satSurprise: Int = 0,
  /** Solo Piloto Estelar: el nivel (1..9) con que empiezan los dos motores y los segundos por sector (con 5, el sector 2 llega a los 5 s). */
  val pilStage: Int = 0,
  val pilSectorS: Int = 0
)

private val DebugGames = listOf(
  DebugGame("secuencia", "Rastro de luz (Reto 90 s)", level = 1, timed = true),
  DebugGame("secuencia", "Rastro de luz (Precisión, 14 rondas)", level = 1, timed = false, tag = "secuencia_precision"),
  DebugGame("secuencia", "Rastro de luz con tutorial (como la primera vez)", level = 1, timed = false, tutorial = true, tag = "secuencia_tutorial"),
  DebugGame("freno", "Freno de Emergencia con tutorial", level = 1, timed = false, tutorial = true, tag = "freno_tutorial"),
  DebugGame("freno", "Freno de Emergencia: tutorial + versión corta (como el inicio)", level = 1, timed = false, tutorial = true, shortVersion = true, tag = "freno_inicio"),
  DebugGame("aterrizaje", "Aterrizaje Lunar con tutorial", level = 1, timed = false, tutorial = true, tag = "aterrizaje_tutorial"),
  DebugGame("aterrizaje", "Aterrizaje Lunar: tutorial + versión corta (como el inicio)", level = 1, timed = false, tutorial = true, shortVersion = true, tag = "aterrizaje_inicio"),
  DebugGame("meteoros", "Lluvia de meteoros con tutorial", level = 1, timed = false, tutorial = true, tag = "meteoros_tutorial"),
  DebugGame("meteoros", "Lluvia de meteoros: tutorial + versión corta (como el inicio)", level = 1, timed = false, tutorial = true, shortVersion = true, tag = "meteoros_inicio"),
  DebugGame("parejas", "Constelaciones (Reto 180 s)", level = 1, timed = true),
  DebugGame("parejas", "Constelaciones con tutorial", level = 1, timed = false, tutorial = true, tag = "parejas_tutorial"),
  DebugGame("parejas", "Constelaciones etapa 7 (gemelos)", level = 1, timed = false, tag = "parejas_etapa7", conStage = 7),
  DebugGame("parejas", "Constelaciones etapa 10 (tríos)", level = 1, timed = false, tag = "parejas_etapa10", conStage = 10),
  DebugGame("parejas", "Constelaciones etapa 13 (parejas y tríos)", level = 1, timed = false, tag = "parejas_etapa13", conStage = 13),
  DebugGame("parejas", "Constelaciones etapa 16 (cielo grande)", level = 1, timed = false, tag = "parejas_etapa16", conStage = 16),
  DebugGame("stroop", "Tinta o Palabra (Reto 60 s)", level = 4, timed = true), // desde el nivel 4 la regla cambia
  DebugGame("calculo", "Carga exacta (Reto 120 s)", level = 1, timed = true),
  DebugGame("calculo", "Carga exacta con tutorial", level = 1, timed = false, tutorial = true, tag = "calculo_tutorial"),
  DebugGame("engranajes", "Engranajes (Reto 120 s)", level = 1, timed = true),
  DebugGame("engranajes", "Engranajes con tutorial", level = 1, timed = false, tutorial = true, tag = "engranajes_tutorial"),
  DebugGame("bodega", "Bodega de carga (Reto 120 s)", level = 1, timed = true),
  DebugGame("bodega", "Bodega con tutorial", level = 1, timed = false, tutorial = true, tag = "bodega_tutorial"),
  DebugGame("anagramas", "En la punta de la lengua (Reto 120 s)", level = 1, timed = true),
  DebugGame("anagramas", "En la punta de la lengua con tutorial", level = 1, timed = false, tutorial = true, tag = "anagramas_tutorial"),
  DebugGame("piloto", "Piloto Estelar (Reto 90 s)", level = 1, timed = true),
  DebugGame("piloto", "Piloto con tutorial", level = 1, timed = false, tutorial = true, tag = "piloto_tutorial"),
  DebugGame("piloto", "Piloto nivel 3", level = 1, timed = true, tag = "piloto_nivel3", pilStage = 3),
  DebugGame("piloto", "Piloto nivel 6", level = 1, timed = true, tag = "piloto_nivel6", pilStage = 6),
  DebugGame("piloto", "Piloto nivel 9", level = 1, timed = true, tag = "piloto_nivel9", pilStage = 9),
  DebugGame("piloto", "Piloto: cambio de sector (el 2 a los 5 s)", level = 1, timed = true, tag = "piloto_sector", pilSectorS = 5),
  DebugGame("radar", "Radar (Reto 90 s)", level = 1, timed = true),
  DebugGame("satelites", "Satélites (Reto 120 s)", level = 1, timed = true),
  DebugGame("satelites", "Satélites con tutorial", level = 1, timed = false, tutorial = true, tag = "satelites_tutorial"),
  DebugGame("satelites", "Satélites nivel 3", level = 1, timed = false, tag = "satelites_nivel3", satStage = 3),
  DebugGame("satelites", "Satélites nivel 6", level = 1, timed = false, tag = "satelites_nivel6", satStage = 6),
  DebugGame("satelites", "Satélites nivel 9", level = 1, timed = false, tag = "satelites_nivel9", satStage = 9),
  DebugGame("satelites", "Satélites sorpresa: nube", level = 1, timed = false, tag = "satelites_nube", satStage = 5, satSurprise = 2),
  DebugGame("freno", "Freno de Emergencia (Reto 120 s)", level = 1, timed = true),
  DebugGame("aterrizaje", "Aterrizaje Lunar (Reto 120 s)", level = 1, timed = true),
  DebugGame("meteoros", "Lluvia de meteoros (Reto 120 s)", level = 1, timed = true),
  DebugGame("disparate", "¿Verdad o disparate? (Reto 120 s)", level = 1, timed = true),
  DebugGame("cosecha", "Cosecha de palabras (Reto 3 × 60 s)", level = 1, timed = true),
  DebugGame("intrusa", "La estrella intrusa (Reto 120 s)", level = 1, timed = true),
  DebugGame("acoplamiento", "Acoplamiento (Reto 120 s)", level = 1, timed = true),
  DebugGame("rumbo", "Rumbo a Casa (Reto 150 s)", level = 1, timed = true),
  DebugGame("correo", "Correo Estelar con tutorial", level = 1, timed = false, tutorial = true, tag = "correo_tutorial"),
  DebugGame("correo", "Correo Estelar etapa 2 (la hora)", level = 1, timed = false, tag = "correo_etapa2", mailStage = 2),
  DebugGame("correo", "Correo Estelar etapa 4 (lo que cancela la radio)", level = 1, timed = false, tag = "correo_etapa4", mailStage = 4),
  DebugGame("correo", "Correo Estelar etapa 5 (el lazo)", level = 1, timed = false, tag = "correo_etapa5", mailStage = 5),
  DebugGame("correo", "Correo Estelar etapa 8", level = 1, timed = false, tag = "correo_etapa8", mailStage = 8)
)

/**
 * Herramientas de prueba al final de Ajustes. Solo se muestran en builds de depuración (`BuildConfig.DEBUG`):
 * ver celebraciones y el onboarding sin tener que provocarlos, y abrir cada juego de Unity directo.
 */
@Composable
fun DebugTools(viewModel: NeuroVidaViewModel, userId: Long, ageBand: AgeBand) {
  val context = LocalContext.current
  // Dibuja encima del tutorial los rectángulos del foco y el último toque (se pide a Unity con `debug_overlay`): para mandar una captura si un tutorial se traba.
  var tutorialOverlay by remember { mutableStateOf(false) }
  Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
    DebugButton(if (tutorialOverlay) "[Debug] Rectángulos del tutorial: SÍ (tocar para quitar)" else "[Debug] Rectángulos del tutorial: NO (tocar para ver)", "btn_debug_tutorial_overlay") { tutorialOverlay = !tutorialOverlay }
    DebugButton("[Debug] Ver celebración de ascenso de liga", "btn_debug_promotion") { viewModel.debugShowPromotion() }
    DebugButton("[Debug] Ver celebración de logro", "btn_debug_achievement") { viewModel.debugShowAchievement() }
    DebugButton("[Debug] Repetir el inicio (sin borrar datos)", "btn_debug_onboarding") { viewModel.debugRestartOnboarding() }
    DebugGames.forEach { g ->
      DebugButton("[Debug] Probar ${g.label}", "btn_debug_unity_${g.tag}") {
        context.startActivity(
          UnityGameLauncher.buildGameIntent(
            context = context,
            gameId = g.id,
            userId = userId.toString(),
            level = g.level,
            baseIntensity = 0,
            timed = g.timed,
            ageBand = ageBand,
            forceTutorial = g.tutorial,
            debugOverlay = tutorialOverlay,
            conStage = g.conStage,
            mailStage = g.mailStage,
            satStage = g.satStage,
            satSurprise = g.satSurprise,
            pilStage = g.pilStage,
            pilSectorS = g.pilSectorS,
            assessmentStep = if (g.shortVersion) 1 else 0,
            assessmentTotal = if (g.shortVersion) 4 else 0
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
