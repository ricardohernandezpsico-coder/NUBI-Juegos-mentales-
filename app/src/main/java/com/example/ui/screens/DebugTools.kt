package com.example.ui.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
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
  val pilSectorS: Int = 0,
  /** Solo Rescate relámpago: el nivel (1..12) con que empieza la partida. */
  val rescStage: Int = 0
)

private val DebugGames = listOf(
  DebugGame("secuencia", "Rastro de luz (con reloj 90 s)", level = 1, timed = true),
  DebugGame("secuencia", "Rastro de luz (sin reloj, 14 rondas)", level = 1, timed = false, tag = "secuencia_precision"),
  DebugGame("secuencia", "Rastro de luz con tutorial (como la primera vez)", level = 1, timed = false, tutorial = true, tag = "secuencia_tutorial"),
  DebugGame("freno", "Freno de Emergencia con tutorial", level = 1, timed = false, tutorial = true, tag = "freno_tutorial"),
  DebugGame("freno", "Freno de Emergencia: tutorial + versión corta (como el inicio)", level = 1, timed = false, tutorial = true, shortVersion = true, tag = "freno_inicio"),
  DebugGame("aterrizaje", "Aterrizaje Lunar con tutorial", level = 1, timed = false, tutorial = true, tag = "aterrizaje_tutorial"),
  DebugGame("aterrizaje", "Aterrizaje Lunar: tutorial + versión corta (como el inicio)", level = 1, timed = false, tutorial = true, shortVersion = true, tag = "aterrizaje_inicio"),
  DebugGame("meteoros", "Lluvia de meteoros con tutorial", level = 1, timed = false, tutorial = true, tag = "meteoros_tutorial"),
  DebugGame("meteoros", "Lluvia de meteoros: tutorial + versión corta (como el inicio)", level = 1, timed = false, tutorial = true, shortVersion = true, tag = "meteoros_inicio"),
  DebugGame("parejas", "Constelaciones (con reloj 180 s)", level = 1, timed = true),
  DebugGame("parejas", "Constelaciones con tutorial", level = 1, timed = false, tutorial = true, tag = "parejas_tutorial"),
  DebugGame("parejas", "Constelaciones etapa 7 (gemelos)", level = 1, timed = false, tag = "parejas_etapa7", conStage = 7),
  DebugGame("parejas", "Constelaciones etapa 10 (tríos)", level = 1, timed = false, tag = "parejas_etapa10", conStage = 10),
  DebugGame("parejas", "Constelaciones etapa 13 (parejas y tríos)", level = 1, timed = false, tag = "parejas_etapa13", conStage = 13),
  DebugGame("parejas", "Constelaciones etapa 16 (cielo grande)", level = 1, timed = false, tag = "parejas_etapa16", conStage = 16),
  DebugGame("stroop", "Tinta o Palabra (con reloj 60 s)", level = 4, timed = true), // desde el nivel 4 la regla cambia
  DebugGame("calculo", "Carga exacta (con reloj 120 s)", level = 1, timed = true),
  DebugGame("calculo", "Carga exacta con tutorial", level = 1, timed = false, tutorial = true, tag = "calculo_tutorial"),
  DebugGame("engranajes", "Engranajes (con reloj 120 s)", level = 1, timed = true),
  DebugGame("engranajes", "Engranajes con tutorial", level = 1, timed = false, tutorial = true, tag = "engranajes_tutorial"),
  DebugGame("bodega", "Bodega de carga (con reloj 120 s)", level = 1, timed = true),
  DebugGame("bodega", "Bodega con tutorial", level = 1, timed = false, tutorial = true, tag = "bodega_tutorial"),
  DebugGame("anagramas", "En la punta de la lengua (con reloj 120 s)", level = 1, timed = true),
  DebugGame("anagramas", "En la punta de la lengua con tutorial", level = 1, timed = false, tutorial = true, tag = "anagramas_tutorial"),
  DebugGame("piloto", "Piloto Estelar (con reloj 90 s)", level = 1, timed = true),
  DebugGame("piloto", "Piloto con tutorial", level = 1, timed = false, tutorial = true, tag = "piloto_tutorial"),
  DebugGame("piloto", "Piloto nivel 3", level = 1, timed = true, tag = "piloto_nivel3", pilStage = 3),
  DebugGame("piloto", "Piloto nivel 6", level = 1, timed = true, tag = "piloto_nivel6", pilStage = 6),
  DebugGame("piloto", "Piloto nivel 9", level = 1, timed = true, tag = "piloto_nivel9", pilStage = 9),
  DebugGame("piloto", "Piloto: cambio de sector (el 2 a los 5 s)", level = 1, timed = true, tag = "piloto_sector", pilSectorS = 5),
  DebugGame("radar", "Rescate relámpago (con reloj 120 s)", level = 1, timed = true),
  DebugGame("radar", "Rescate relámpago con tutorial", level = 1, timed = false, tutorial = true, tag = "radar_tutorial"),
  DebugGame("radar", "Rescate relámpago nivel 3", level = 1, timed = false, tag = "radar_nivel3", rescStage = 3),
  DebugGame("radar", "Rescate relámpago nivel 6", level = 1, timed = false, tag = "radar_nivel6", rescStage = 6),
  DebugGame("radar", "Rescate relámpago nivel 9", level = 1, timed = false, tag = "radar_nivel9", rescStage = 9),
  DebugGame("satelites", "Satélites (con reloj 120 s)", level = 1, timed = true),
  DebugGame("satelites", "Satélites con tutorial", level = 1, timed = false, tutorial = true, tag = "satelites_tutorial"),
  DebugGame("satelites", "Satélites nivel 3", level = 1, timed = false, tag = "satelites_nivel3", satStage = 3),
  DebugGame("satelites", "Satélites nivel 6", level = 1, timed = false, tag = "satelites_nivel6", satStage = 6),
  DebugGame("satelites", "Satélites nivel 9", level = 1, timed = false, tag = "satelites_nivel9", satStage = 9),
  DebugGame("satelites", "Satélites sorpresa: nube", level = 1, timed = false, tag = "satelites_nube", satStage = 5, satSurprise = 2),
  DebugGame("freno", "Freno de Emergencia (con reloj 120 s)", level = 1, timed = true),
  DebugGame("aterrizaje", "Aterrizaje Lunar (con reloj 120 s)", level = 1, timed = true),
  DebugGame("meteoros", "Lluvia de meteoros (con reloj 120 s)", level = 1, timed = true),
  DebugGame("disparate", "¿Verdad o disparate? (con reloj 120 s)", level = 1, timed = true),
  DebugGame("intrusa", "La estrella intrusa (con reloj 120 s)", level = 1, timed = true),
  DebugGame("acoplamiento", "Acoplamiento (con reloj 120 s)", level = 1, timed = true),
  DebugGame("acoplamiento", "Acoplamiento con tutorial", level = 1, timed = false, tutorial = true, tag = "acoplamiento_tutorial"),
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
  var guestGame by remember { mutableStateOf<String?>(null) }       // el juego del «Contrarreloj de invitado» que espera la edad del invitado
  Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
    DebugButton(if (tutorialOverlay) "[Debug] Rectángulos del tutorial: SÍ (tocar para quitar)" else "[Debug] Rectángulos del tutorial: NO (tocar para ver)", "btn_debug_tutorial_overlay") { tutorialOverlay = !tutorialOverlay }
    if (com.example.Features.LEAGUES) DebugButton("[Debug] Ver celebración de ascenso de liga", "btn_debug_promotion") { viewModel.debugShowPromotion() }
    DebugButton("[Debug] Ver celebración de logro", "btn_debug_achievement") { viewModel.debugShowAchievement() }
    // Nubi Plus (Tarea 199, `PlusAccess`): la compra llega con Google Play Billing (Etapa 4); mientras, este botón enciende o apaga la página «Contrarreloj» de Avance y el atajo del final. Las builds de depuración empiezan con Plus encendido.
    val plus by viewModel.plus.collectAsState()
    DebugButton(if (plus) "[Debug] Nubi Plus: SÍ (tocar para quitar)" else "[Debug] Nubi Plus: NO (tocar para poner)", "btn_debug_plus") { viewModel.setPlus(!plus) }
    // La comparación (Tareas 202, 202b y 205): el valor de fábrica es la «referencia» estimada, lo mismo que ve la tienda; los otros tres son datos de muestra («muchos», «pocos» y «muy pocos», los dos mundos de «Todavía somos pocos»): este botón da la vuelta entre los cuatro.
    val compareWorld by viewModel.compareWorld.collectAsState()
    val worlds = com.example.data.SampleComparison.World.entries
    val nextWorld = worlds[(compareWorld.ordinal + 1) % worlds.size]
    fun worldName(w: com.example.data.SampleComparison.World) = when (w) { com.example.data.SampleComparison.World.REFERENCE -> "referencia"; com.example.data.SampleComparison.World.MANY -> "muchos"; com.example.data.SampleComparison.World.FEW -> "pocos"; com.example.data.SampleComparison.World.VERY_FEW -> "muy pocos" }
    DebugButton("[Debug] Comparación: ${worldName(compareWorld)} (tocar para ${worldName(nextWorld)})", "btn_debug_compare") { viewModel.setCompareWorld(nextWorld) }
    // «Contrarreloj de invitado» (Tarea 205; la edad se pregunta desde la 205c): la pista de hoy del juego elegido; al final muestra la marca y NO guarda nada (ni partida, ni récord, ni racha, ni farolas, ni lo «ya visto»): para anotar las marcas de otras personas en este teléfono sin mezclar las tuyas.
    com.example.data.TimeTrialDay.GAMES.forEach { id ->
      val title = com.example.model.GameRegistry.getById(id)?.title ?: id
      DebugButton("[Debug] Contrarreloj de invitado: $title", "btn_debug_guest_$id") { guestGame = id }
    }
    // el invitado elige su EDAD antes de jugar (Tarea 205c): juega con la banda de esa edad (los de 65 en adelante reciben las tarjetas de Tinta ×1,3 más tarde, como los robots de la referencia), así su marca es comparable con la tabla
    guestGame?.let { id ->
      GuestAgeDialog(
        gameTitle = com.example.model.GameRegistry.getById(id)?.title ?: id,
        onPick = { range -> guestGame = null; viewModel.startGuestTimeTrial(id, range) },
        onCancel = { guestGame = null }
      )
    }
    DebugButton("[Debug] Repetir el inicio (sin borrar datos)", "btn_debug_onboarding") { viewModel.debugRestartOnboarding() }
    // «Jugar como la primera vez» (Tarea 191): tutorial, subida rápida de etapa y tarjetas «NUEVO» como las ve alguien nuevo, sin tocar tu progreso: las marcas de «ya visto» no se leen ni se guardan y el resultado no se guarda.
    com.example.model.GameRegistry.allGames.forEach { g ->
      DebugButton("[Debug] Jugar como la primera vez: ${g.title}", "btn_debug_first_${g.id}") {
        context.startActivity(
          UnityGameLauncher.buildGameIntent(
            context = context,
            gameId = g.id,
            userId = userId.toString(),
            level = 1,
            baseIntensity = 0,
            timed = false,
            ageBand = ageBand,
            debugOverlay = tutorialOverlay,
            freshStart = true
          )
        )
      }
    }
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
            rescStage = g.rescStage,
            assessmentStep = if (g.shortVersion) 1 else 0,
            assessmentTotal = if (g.shortVersion) 4 else 0
          )
        )
      }
    }
  }
}

@Composable
private fun GuestAgeDialog(gameTitle: String, onPick: (com.example.model.AgeRange) -> Unit, onCancel: () -> Unit) {
  androidx.compose.material3.AlertDialog(
    onDismissRequest = onCancel,
    title = { Text("Contrarreloj de invitado: $gameTitle", fontSize = 20.sp) },
    text = {
      Column(verticalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.verticalScroll(rememberScrollState()).testTag("guest_age_dialog")) {
        Text("¿Qué edad tiene la persona? Con ella se juega la partida y se anota junto a su marca; no se guarda nada.", fontSize = 16.sp)
        com.example.model.AgeRange.entries.filter { it != com.example.model.AgeRange.R13_17 }.forEach { range ->
          OutlinedButton(onClick = { onPick(range) }, shape = RoundedCornerShape(14.dp), modifier = Modifier.fillMaxWidth().heightIn(min = 48.dp).testTag("btn_guest_age_${range.name.lowercase()}")) { Text(range.label, fontSize = 16.sp) }
        }
      }
    },
    confirmButton = {},
    dismissButton = { androidx.compose.material3.TextButton(onClick = onCancel, modifier = Modifier.testTag("btn_guest_age_cancel")) { Text("Cancelar", fontSize = 16.sp) } }
  )
}

@Composable
private fun DebugButton(text: String, tag: String, onClick: () -> Unit) {
  OutlinedButton(onClick = onClick, shape = RoundedCornerShape(14.dp), modifier = Modifier.fillMaxWidth().testTag(tag)) {
    Text(text)
  }
}
