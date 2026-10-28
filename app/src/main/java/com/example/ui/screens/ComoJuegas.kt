package com.example.ui.screens

import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.width
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.ui.components.ClayPanel
import com.example.ui.components.ClaySwitch
import com.example.ui.components.Glyph
import com.example.ui.components.KitGlyph
import com.example.ui.theme.Clay
import com.example.viewmodel.NeuroVidaViewModel
import kotlinx.coroutines.delay

/**
 * «Cómo juegas» en Tú (Tarea 185; Fable, `auditoria-fable/tu-pestana.md` §2.2 B): los tres ajustes de uso diario a mano, sin abrir nada: efectos de sonido, vibración y recordatorio diario (sí o no). **Una sola puerta por ajuste** (regla de la Tarea 152, que Opus
 * repitió el 28-oct): estos tres interruptores viven SOLO aquí; en Ajustes quedan la hora del recordatorio y «Probar el aviso». Cada cambio se ve en el interruptor mismo (ya no hay un `Toast` de Android que lo repita); lo único que se explica es lo que NO sale
 * bien: si Android no deja activar los avisos, una línea plana lo dice y se queda hasta el siguiente intento.
 */
@Composable
fun ComoJuegasPanel(viewModel: NeuroVidaViewModel, modifier: Modifier = Modifier) {
  val settings by viewModel.userSettings.collectAsState()
  var sound by remember(settings.soundEnabled) { mutableStateOf(settings.soundEnabled) }
  var haptics by remember(settings.hapticsEnabled) { mutableStateOf(settings.hapticsEnabled) }
  var reminder by remember(settings.notificationsEnabled) { mutableStateOf(settings.notificationsEnabled) }
  var permissionDenied by remember { mutableStateOf(false) }
  // Android 13 y siguientes piden permiso para avisar; antes, no. El resultado decide el interruptor.
  val launcher = rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
    permissionDenied = !granted
    reminder = granted
    viewModel.setReminderEnabled(granted)
  }

  ClayPanel(modifier = modifier.fillMaxWidth().testTag("card_como_juegas"), verticalSpacing = 12.dp) {
    Text(text = "Cómo juegas", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
    SettingSwitchRow(Glyph.SPEAKER, "Efectos de sonido", sound, "switch_sound") { sound = it; viewModel.setSoundEnabled(it) }
    HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
    SettingSwitchRow(Glyph.VIBRATION, "Vibración", haptics, "switch_haptics") { haptics = it; viewModel.setHapticsEnabled(it) }
    HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
    SettingSwitchRow(Glyph.BELL, "Recordatorio diario", reminder, "switch_notifications_enabled") { on ->
      if (!on) {
        permissionDenied = false
        reminder = false
        viewModel.setReminderEnabled(false)
      } else if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
        launcher.launch(android.Manifest.permission.POST_NOTIFICATIONS)
      } else {
        permissionDenied = false
        reminder = true
        viewModel.setReminderEnabled(true)
      }
    }
    if (reminder) {
      Text(
        text = "Un aviso al día, a las ${"%02d:%02d".format(settings.reminderHour, settings.reminderMinute)}. La hora se cambia en Ajustes.",
        fontSize = 16.sp, lineHeight = 21.sp, color = Clay.InkMuted, modifier = Modifier.testTag("reminder_hint")
      )
    }
    if (permissionDenied) {
      Text(
        text = "Android no dejó activar los avisos. Puedes permitirlos en los ajustes de tu teléfono.",
        fontSize = 16.sp, lineHeight = 21.sp, color = Clay.InkMuted, modifier = Modifier.testTag("reminder_denied")
      )
    }
  }
}

/** Una fila de ajuste: su ícono del kit, su nombre y el interruptor de plasticina (el nombre también es lo que lee el lector de pantalla). */
@Composable
internal fun SettingSwitchRow(glyph: Glyph, label: String, checked: Boolean, tag: String, onChange: (Boolean) -> Unit) {
  Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
    Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.weight(1f)) {
      KitGlyph(glyph)
      Spacer(modifier = Modifier.width(10.dp))
      Text(text = label, style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.SemiBold)
    }
    ClaySwitch(checked = checked, onCheckedChange = onChange, modifier = Modifier.testTag(tag), label = label)
  }
}

/**
 * Un aviso chico de texto plano junto a un control que dura [ms] ms (por defecto 2 s) y se va solo: reemplaza a los `Toast` de Android de Ajustes (Tarea 185). null = no hay nada que decir. Un texto distinto empieza su propio tiempo.
 */
@Composable
internal fun TransientNote(text: String?, tag: String, ms: Long = 2_000L, modifier: Modifier = Modifier, onGone: () -> Unit = {}) {
  var shown by remember(text) { mutableStateOf(text) }
  LaunchedEffect(text) {
    if (text != null) { delay(ms); shown = null; onGone() }
  }
  shown?.let { Text(text = it, fontSize = 16.sp, lineHeight = 21.sp, color = Clay.InkMuted, modifier = modifier.testTag(tag)) }
}
