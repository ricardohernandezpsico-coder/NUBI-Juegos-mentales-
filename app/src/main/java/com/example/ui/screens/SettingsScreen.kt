package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.disabled
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.AgeBand
import com.example.model.AgeRange
import com.example.model.AppLanguage
import com.example.ui.components.ClayChoice
import com.example.ui.components.ClayChoiceRow
import com.example.ui.components.ClayDialog
import com.example.ui.components.ClayPanel
import com.example.ui.components.Glyph
import com.example.ui.components.KitGlyph
import com.example.ui.components.PanelAccent
import com.example.ui.components.WeeklyGoalLamps
import com.example.ui.i18n.strings
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.viewmodel.NeuroVidaViewModel

/**
 * Los Ajustes (Tarea 185, 28-oct; Fable, `auditoria-fable/tu-pestana.md` §2.4): cuatro losas en vez de nueve bloques — **Juego**, **Recordatorio**, **Tus datos** y **Acerca de Nubi**. Una sola puerta por ajuste (regla de la Tarea 152): el sonido, la vibración y el sí o no del
 * recordatorio viven SOLO en Tú («Cómo juegas», [ComoJuegasPanel]); aquí quedan la meta semanal, la edad y los estudios (Juego), la HORA del recordatorio y «Probar el aviso» (Recordatorio) y lo demás. «Enviar informe de errores» se mudó a «Contacto» de Tú (es contacto, no un
 * ajuste). Ya no hay `Toast` de Android: cada cambio se ve en el control mismo y lo único que se explica es lo que no sale como se espera. Las herramientas de prueba salen solo en builds de depuración.
 */
@OptIn(androidx.compose.foundation.layout.ExperimentalLayoutApi::class)
@Composable
fun SettingsScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier,
  /** Abre "Licencias y créditos" (la pantalla la muestra el panel que contiene a Ajustes). */
  onOpenLicenses: () -> Unit = {}
) {
  val userSettings by viewModel.userSettings.collectAsState()

  var weeklyGoal by remember(userSettings.weeklyGoal) { mutableStateOf(userSettings.weeklyGoal) }
  var reminderHour by remember(userSettings.reminderHour) { mutableStateOf(userSettings.reminderHour) }
  var reminderMinute by remember(userSettings.reminderMinute) { mutableStateOf(userSettings.reminderMinute) }
  var appLanguage by remember(userSettings.language) { mutableStateOf(userSettings.language) }
  val ageBand = userSettings.ageBand ?: AgeBand.ADULT                                       // la banda de tres del DDA (solo la usan las herramientas de depuración)
  val ageRange by viewModel.ageRange.collectAsState()                                       // la franja fina (Tarea 195); null = no la eligió: no se marca ninguna

  var showResetDialog by remember { mutableStateOf(false) }

  Column(
    modifier = modifier
      .fillMaxSize()
      .background(MaterialTheme.colorScheme.background)
      .verticalScroll(rememberScrollState())
      .padding(horizontal = 20.dp),
    verticalArrangement = Arrangement.spacedBy(20.dp)
  ) {
    Spacer(modifier = Modifier.height(16.dp))

    // El selector de idioma: solo se muestra si hay más de un idioma TRADUCIDO (`AppLanguage.translated`). Hoy solo el español lo está: la app no se traduce al resto, así que elegirlos dejaba una app a medias.
    if (AppLanguage.selectable.size > 1) ClayPanel(modifier = Modifier.fillMaxWidth().testTag("card_language_selector"), verticalSpacing = 14.dp) {
      Row(verticalAlignment = Alignment.CenterVertically) {
        KitGlyph(Glyph.GLOBE)
        Spacer(modifier = Modifier.width(10.dp))
        Column {
          Text(text = strings.languageCardTitle, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
          Text(text = strings.languageCardSubtitle, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
      }
      ClayChoiceRow {
        AppLanguage.selectable.forEach { lang ->
          val isSelected = appLanguage == lang
          ClayChoice(
            selected = isSelected,
            modifier = Modifier.testTag("chip_lang_${lang.code}"),
            onClick = {
              appLanguage = lang
              viewModel.updateSettings(
                name = userSettings.name, weeklyGoal = weeklyGoal, defaultTimed = userSettings.defaultTimed,
                sound = userSettings.soundEnabled, haptics = userSettings.hapticsEnabled, language = lang
              )
            },
          ) { Text(text = lang.displayName, fontSize = 16.sp, fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Medium) }
        }
      }
    }

    // ---- 1. Juego: la meta semanal, la edad y los estudios
    ClayPanel(modifier = Modifier.fillMaxWidth().testTag("card_juego"), verticalSpacing = 14.dp) {
      Text(text = "Juego", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)

      // Meta semanal (Tarea 130): 7 farolas del kit con su número (1 a 7) en lugar del Slider de Material: la meta se lee como CANTIDAD y es el mismo objeto de Hoy y Avance
      Column(verticalArrangement = Arrangement.spacedBy(6.dp), modifier = Modifier.testTag("settings_weekly_goal")) {
        Text(text = "Meta semanal de juego", style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.SemiBold)
        Text(text = "$weeklyGoal días por semana", style = MaterialTheme.typography.bodyLarge, fontWeight = FontWeight.Black, color = PanelAccent)
        WeeklyGoalLamps(goal = weeklyGoal, onGoal = { k -> weeklyGoal = k; viewModel.setWeeklyGoal(k) })
      }

      HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

      // Rango de edad (piloto de perfiles por edad, 20-sep): chips. Lo que hace cada uno lo dice la nota de abajo (el onboarding prometió «puedes cambiarlo cuando quieras desde Ajustes»: esto cumple esa promesa).
      Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
          KitGlyph(Glyph.PERSON)
          Spacer(modifier = Modifier.width(10.dp))
          Text(text = "Rango de edad", style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.SemiBold)
        }
        ClayChoiceRow {
          AgeRange.entries.forEach { range ->
            ClayChoice(
              selected = ageRange == range,
              onClick = { viewModel.setAgeRange(range) },
              modifier = Modifier.testTag("chip_age_range_${range.name.lowercase()}")
            ) { Text(text = range.label, fontSize = 16.sp, fontWeight = if (ageRange == range) FontWeight.Bold else FontWeight.Medium) }
          }
        }
        Text(text = "Cambia el ritmo y el tamaño de los juegos, no lo difícil: eso lo decide cómo juegas.", fontSize = 16.sp, lineHeight = 21.sp, color = Clay.InkMuted)
      }

      HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

      // Tu nivel de estudios (Tarea 152, B5.2 y B6.5; Tarea 185: una línea con «Cambiar» que despliega las seis opciones, en lugar de seis casillas siempre abiertas —unas 300 dp para un dato opcional que se toca una sola vez—): solo ayuda a calcular el punto de partida si se salta la
      // evaluación; no cambia la dificultad.
      val education by viewModel.education.collectAsState()
      var educationOpen by remember { mutableStateOf(false) }
      Column(verticalArrangement = Arrangement.spacedBy(10.dp), modifier = Modifier.testTag("settings_education")) {
        Row(verticalAlignment = Alignment.CenterVertically) {
          Text(
            text = "Tu nivel de estudios: ${education?.label?.lowercase() ?: "sin elegir"}", style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.SemiBold,
            modifier = Modifier.weight(1f).testTag("education_line")
          )
          Spacer(Modifier.width(8.dp))
          ClayChoice(selected = false, onClick = { educationOpen = !educationOpen }, modifier = Modifier.testTag("btn_education_change")) {
            Text(text = if (educationOpen) "Listo" else "Cambiar", fontSize = 16.sp, fontWeight = FontWeight.Medium)
          }
        }
        if (educationOpen) {
          Text(text = "Opcional. Ayuda a calcular tu punto de partida si saltas la evaluación.", fontSize = 16.sp, lineHeight = 21.sp, color = Clay.InkMuted)
          Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            com.example.data.Education.entries.forEach { e ->
              val on = education == e
              ClayChoice(
                selected = on,
                onClick = { viewModel.setEducation(if (on) null else e) },
                modifier = Modifier.fillMaxWidth().testTag("education_${e.name.lowercase()}")
              ) { Text(text = e.label, fontSize = 16.sp, fontWeight = if (on) FontWeight.Bold else FontWeight.Medium, modifier = Modifier.weight(1f)) }
            }
          }
        }
      }

      HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

      // Las animaciones no son un ajuste de Nubi: la app ya respeta «quitar animaciones» de Android y no debe duplicar el del sistema (Fable, §2.4; la línea pasó de «Sonido y vibración» a «Juego» el 28-oct)
      Text(
        text = "Las animaciones se quitan desde los ajustes de Android y Nubi lo respeta.",
        fontSize = 16.sp, lineHeight = 21.sp, color = Clay.InkMuted, modifier = Modifier.testTag("settings_animations_note")
      )
    }

    // ---- 2. Recordatorio: la HORA y «Probar el aviso» (el sí o no vive en Tú, «Cómo juegas»). Apagado: las horas se ven apagadas y una línea dice dónde encenderlo.
    val reminderOn = userSettings.notificationsEnabled
    var testNote by remember { mutableStateOf<String?>(null) }
    ClayPanel(modifier = Modifier.fillMaxWidth().testTag("card_notifications_workmanager"), verticalSpacing = 14.dp) {
      Row(verticalAlignment = Alignment.CenterVertically) {
        KitGlyph(Glyph.BELL)
        Spacer(modifier = Modifier.width(8.dp))
        Column {
          Text(text = "Recordatorio diario", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
          Text(text = "Un aviso al día", style = MaterialTheme.typography.labelMedium, color = PanelAccent)
        }
      }
      if (reminderOn) {
        Text(
          text = "Un aviso cada día, a la hora que elijas, para acordarte de jugar. Se apaga en Tú, en «Cómo juegas».",
          style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant, modifier = Modifier.testTag("reminder_state_note")
        )
      } else {
        Text(
          text = "El recordatorio está apagado. Enciéndelo en Tú, en «Cómo juegas».",
          style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.SemiBold, modifier = Modifier.testTag("reminder_off_note")
        )
      }

      Text(text = "Hora del recordatorio", style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.SemiBold)

      // Las cuatro horas en una cuadrícula de 2 × 2: la elegida en lima, con su ✓ y quieta. Apagado: se ven a media luz y no se tocan.
      Column(verticalArrangement = Arrangement.spacedBy(2.dp), modifier = Modifier.alpha(if (reminderOn) 1f else 0.5f).testTag("reminder_hours")) {
        val times = listOf(Triple("Mañana", 9, 0), Triple("Tarde", 15, 0), Triple("Al atardecer", 19, 0), Triple("Noche", 21, 0))
        times.chunked(2).forEach { pair -> Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) { pair.forEach { (label, h, m) ->
          val isSelected = reminderHour == h && reminderMinute == m
          ClayChoice(
            selected = isSelected,
            modifier = Modifier.weight(1f).testTag("btn_time_${label.lowercase().replace(" ", "_")}").then(if (reminderOn) Modifier else Modifier.semantics { disabled() }),
            onClick = { if (reminderOn) { reminderHour = h; reminderMinute = m; viewModel.setReminderTime(h, m) } }
          ) {
            Column(Modifier.weight(1f)) {
              Text(text = label, fontSize = 16.sp, fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Medium)
              Text(text = "%02d:%02d".format(h, m), fontSize = 16.sp, color = Clay.InkMuted)
            }
          }
        } } }
      }

      // Probar el aviso: con el recordatorio apagado no se puede (la prueba llega por el mismo permiso). El «enviado» es una línea plana de 2 s, no un Toast.
      Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
        ClayButton(
          text = "Probar el aviso ahora", color = Clay.Cream,
          onClick = { if (reminderOn) { viewModel.triggerTestNotification(); testNote = "Aviso de prueba enviado" } },
          modifier = Modifier.alpha(if (reminderOn) 1f else 0.5f).testTag("btn_test_notification").then(if (reminderOn) Modifier else Modifier.semantics { disabled() })
        )
        TransientNote(testNote, "test_notification_note", onGone = { testNote = null })
      }
    }

    // ---- 3. Tus datos
    ClayPanel(modifier = Modifier.fillMaxWidth().testTag("card_tus_datos"), verticalSpacing = 14.dp) {
      Text(text = "Tus datos", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
      Row(verticalAlignment = Alignment.CenterVertically) {
        KitGlyph(Glyph.SHIELD)
        Spacer(modifier = Modifier.width(12.dp))
        Column {
          Text(text = "Tu progreso es tuyo", style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.Bold, color = PanelAccent)
          Text(
            text = "Sin cuenta, sin internet y sin anuncios. Tu progreso se guarda en tu teléfono y en la copia de seguridad de Android, si la tienes activada.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant
          )
        }
      }
      // Borra datos: va en coral (y su texto lo dice), no solo por el color.
      ClayButton(text = "Restablecer datos y puntuaciones", onClick = { showResetDialog = true }, color = Clay.Coral, modifier = Modifier.testTag("btn_reset_data"))
    }

    // ---- 4. Acerca de Nubi: licencias y el aviso de salud (lo exige Google Play; texto aprobado por Ricardo, Tarea 121: sección con título y letra de lectura, no una nota chica; el mismo texto va en «Licencias y créditos»). La política de privacidad todavía no está (queda pendiente en la hoja de ruta).
    ClayPanel(modifier = Modifier.fillMaxWidth().testTag("card_acerca"), verticalSpacing = 14.dp) {
      Text(text = "Acerca de Nubi", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
      ClayButton(text = "Licencias y créditos", onClick = onOpenLicenses, color = Clay.Cream, modifier = Modifier.testTag("btn_licenses"))
      Column(verticalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.testTag("health_notice")) {
        Row(verticalAlignment = Alignment.CenterVertically) {
          KitGlyph(Glyph.INFO)
          Spacer(modifier = Modifier.width(12.dp))
          Text(text = com.example.data.HealthNotice.TITLE, style = MaterialTheme.typography.titleMedium, color = MaterialTheme.colorScheme.onSurface)
        }
        Text(text = com.example.data.HealthNotice.TEXT, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
      }
    }

    // Herramientas de prueba (solo en builds de depuración), ver DebugTools.kt.
    if (com.example.BuildConfig.DEBUG) {
      DebugTools(viewModel, userSettings.id, ageBand)
      Spacer(modifier = Modifier.height(12.dp))
    }

    Spacer(modifier = Modifier.height(90.dp))
  }

  if (showResetDialog) {
    ClayDialog(
      title = "¿Restablecer datos?",
      text = "Esta acción borrará el historial de partidas y niveles guardados en este dispositivo.",
      confirmText = "Restablecer",
      onConfirm = {
        viewModel.resetData()
        showResetDialog = false
      },
      dismissText = "Cancelar",
      onDismiss = { showResetDialog = false },
      confirmColor = Clay.Coral
    )
  }
}
