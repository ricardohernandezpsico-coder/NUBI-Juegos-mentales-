package com.example.ui.screens

import android.os.Build
import android.widget.Toast
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.VolumeUp
import androidx.compose.material.icons.filled.*
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.AgeBand
import com.example.model.AppLanguage
import com.example.model.DifficultyMode
import com.example.model.DomainType
import com.example.model.UserSettings
import com.example.ui.components.ClayChoice
import com.example.ui.components.ClayChoiceRow
import com.example.ui.components.ClayDialog
import com.example.ui.components.ClayPanel
import com.example.ui.components.ClaySwitch
import com.example.ui.components.ClayTextField
import com.example.ui.components.Glyph
import com.example.ui.components.KitGlyph
import com.example.ui.components.PanelAccent
import com.example.ui.components.WeeklyGoalLamps
import com.example.ui.i18n.strings
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.NubiTokens
import com.example.viewmodel.NeuroVidaViewModel

@OptIn(androidx.compose.foundation.layout.ExperimentalLayoutApi::class)
@Composable
fun SettingsScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier,
  /** Abre "Licencias y créditos" (la pantalla la muestra el panel que contiene a Ajustes). */
  onOpenLicenses: () -> Unit = {}
) {
  val userSettings by viewModel.userSettings.collectAsState()

  val nameInput = userSettings.name                              // el nombre se edita en el Perfil (Tarea 152): aquí solo se reenvía tal cual
  var weeklyGoal by remember(userSettings.weeklyGoal) { mutableStateOf(userSettings.weeklyGoal) }
  var defaultTimed by remember(userSettings.defaultTimed) { mutableStateOf(userSettings.defaultTimed) }
  var soundEnabled by remember(userSettings.soundEnabled) { mutableStateOf(userSettings.soundEnabled) }
  var hapticsEnabled by remember(userSettings.hapticsEnabled) { mutableStateOf(userSettings.hapticsEnabled) }
  var notificationsEnabled by remember(userSettings.notificationsEnabled) { mutableStateOf(userSettings.notificationsEnabled) }
  var reminderHour by remember(userSettings.reminderHour) { mutableStateOf(userSettings.reminderHour) }
  var reminderMinute by remember(userSettings.reminderMinute) { mutableStateOf(userSettings.reminderMinute) }

  // Granular difficulty states
  var difficultyMode by remember(userSettings.difficultyMode) { mutableStateOf(userSettings.difficultyMode) }
  var cognitiveAssistance by remember(userSettings.cognitiveAssistance) { mutableStateOf(userSettings.cognitiveAssistance) }
  var timeScaleFactor by remember(userSettings.timeScaleFactor) { mutableStateOf(userSettings.timeScaleFactor) }
  // El tema es único (cosmos oscuro, ver Theme.kt): ya no hay selector; se reenvía el valor guardado tal cual.
  val themeMode = userSettings.themeMode
  var appLanguage by remember(userSettings.language) { mutableStateOf(userSettings.language) }
  var ageBand by remember(userSettings.ageBand) { mutableStateOf(userSettings.ageBand ?: AgeBand.ADULT) }

  val context = LocalContext.current
  val notificationPermissionLauncher = rememberLauncherForActivityResult(
    contract = ActivityResultContracts.RequestPermission()
  ) { isGranted ->
    if (isGranted) {
      notificationsEnabled = true
      viewModel.updateSettings(
        name = nameInput,
        weeklyGoal = weeklyGoal,
        defaultTimed = defaultTimed,
        sound = soundEnabled,
        haptics = hapticsEnabled,
        notificationsEnabled = true,
        reminderHour = reminderHour,
        reminderMinute = reminderMinute
      )
      Toast.makeText(context, "Recordatorio diario activado", Toast.LENGTH_SHORT).show()
    } else {
      notificationsEnabled = false
      viewModel.updateSettings(
        name = nameInput,
        weeklyGoal = weeklyGoal,
        defaultTimed = defaultTimed,
        sound = soundEnabled,
        haptics = hapticsEnabled,
        notificationsEnabled = false,
        reminderHour = reminderHour,
        reminderMinute = reminderMinute
      )
      Toast.makeText(context, "Permiso de notificaciones denegado", Toast.LENGTH_SHORT).show()
    }
  }

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

    // (Tarea 143, A3) El título grande «Ajustes y Preferencias» se quitó: la cabecera del panel ya dice «Ajustes» (antes eran dos títulos seguidos, «Opciones» y este).

    // El selector de idioma: solo se muestra si hay más de un idioma TRADUCIDO (`AppLanguage.translated`). Hoy solo el español lo está: la app no se traduce al resto, así que elegirlos dejaba una app a medias.
    if (AppLanguage.selectable.size > 1) ClayPanel(modifier = Modifier.fillMaxWidth().testTag("card_language_selector"), verticalSpacing = 14.dp) {
      run {
        Row(
          verticalAlignment = Alignment.CenterVertically,
          horizontalArrangement = Arrangement.SpaceBetween,
          modifier = Modifier.fillMaxWidth()
        ) {
          Row(verticalAlignment = Alignment.CenterVertically) {
            KitGlyph(Glyph.GLOBE)
            Spacer(modifier = Modifier.width(10.dp))
            Column {
              Text(
                text = strings.languageCardTitle,
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
              )
              Text(
                text = strings.languageCardSubtitle,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
              )
            }
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
                  name = nameInput,
                  weeklyGoal = weeklyGoal,
                  defaultTimed = defaultTimed,
                  sound = soundEnabled,
                  haptics = hapticsEnabled,
                  notificationsEnabled = notificationsEnabled,
                  reminderHour = reminderHour,
                  reminderMinute = reminderMinute,
                  themeMode = themeMode,
                  language = lang
                )
              },
            ) {
              Text(
                text = lang.displayName,
                fontSize = 16.sp,
                fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Medium
              )
            }
          }
        }
      }
    }

    // 2. Meta semanal de juego
    ClayPanel(modifier = Modifier.fillMaxWidth()) {
      run {
        Text(
          text = "Meta semanal de juego",
          style = MaterialTheme.typography.titleMedium,
          fontWeight = FontWeight.Bold
        )
        Text(
          text = "$weeklyGoal días por semana",
          style = MaterialTheme.typography.bodyLarge,
          fontWeight = FontWeight.Black,
          color = PanelAccent,
          modifier = Modifier.padding(top = 4.dp)
        )

        Spacer(modifier = Modifier.height(8.dp))

        // Tarea 130: 7 farolas del kit con su número (1 a 7) en lugar del Slider de Material: la meta se lee como CANTIDAD y es el mismo objeto de Hoy y Avance
        WeeklyGoalLamps(
          goal = weeklyGoal,
          onGoal = { k ->
            weeklyGoal = k
            viewModel.updateSettings(
              name = nameInput,
              weeklyGoal = weeklyGoal,
              defaultTimed = defaultTimed,
              sound = soundEnabled,
              haptics = hapticsEnabled,
              notificationsEnabled = notificationsEnabled,
              reminderHour = reminderHour,
              reminderMinute = reminderMinute,
              difficultyMode = difficultyMode
            )
          }
        )
      }
    }

    // (28-sep) La tarjeta "Dificultad Cognitiva" (modo Principiante/Intermedio/Avanzado, niveles por dominio,
    // asistencia y ritmo) se quitó: no llegaba a los juegos. La dificultad se elige por juego antes de jugar
    // (Suave / A tu medida / Desafío / Experto): ver docs/dificultad-y-avance.md.

    // Game Mode & Audio/Haptic Switches
    ClayPanel(modifier = Modifier.fillMaxWidth(), verticalSpacing = 16.dp) {
      run {
        Text(
          text = "Cómo juegas",
          style = MaterialTheme.typography.titleMedium,
          fontWeight = FontWeight.Bold
        )

        // (6-oct) El interruptor «Modo contra el reloj (Reto)» se quitó: «Sin reloj / Contra el reloj» se elige por juego en su ficha de Juegos, junto a la dificultad (data/RetoChoice.kt).
        // Su último valor quedó como elección inicial de cada juego y sigue guardado en `defaultTimed`.

        HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

        // Rango de edad (piloto de perfiles por edad, 20-sep): chips, mismo patrón visual
        // que el selector de idioma. Cambia el DDA en Constelaciones
        // únicamente por ahora -- el onboarding prometió "podés cambiarlo cuando
        // quieras desde Ajustes", esto cumple esa promesa.
        Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
          Row(verticalAlignment = Alignment.CenterVertically) {
            KitGlyph(Glyph.PERSON)
            Spacer(modifier = Modifier.width(10.dp))
            Text(
              text = "Rango de edad",
              style = MaterialTheme.typography.bodyMedium,
              fontWeight = FontWeight.SemiBold
            )
          }
          ClayChoiceRow {
            AgeBand.entries.forEach { band ->
              ClayChoice(
                selected = ageBand == band,
                onClick = {
                  ageBand = band
                  viewModel.setAgeBand(band)
                },
                modifier = Modifier.testTag("chip_age_band_${band.name.lowercase()}")
              ) { Text(text = band.label, fontSize = 16.sp, fontWeight = if (ageBand == band) FontWeight.Bold else FontWeight.Medium) }
            }
          }
          Text(
            text = "Cambia el ritmo y el tamaño de los juegos, no lo difícil: eso lo decide cómo juegas.",
            fontSize = 16.sp, lineHeight = 21.sp,
            color = com.example.ui.theme.Clay.InkMuted
          )
        }

        HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

        // Tu nivel de estudios (Tarea 152, B5.2 y B6.5: antes huérfano en el Perfil y sin decir para qué sirve): Ajustes es «cómo juega la app». Solo ayuda a calcular el punto de partida si se salta la evaluación; no cambia la dificultad.
        val education by viewModel.education.collectAsState()
        Column(verticalArrangement = Arrangement.spacedBy(10.dp), modifier = Modifier.testTag("settings_education")) {
          Text(text = "Tu nivel de estudios", style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.SemiBold)
          Text(
            text = "Opcional. Ayuda a calcular tu punto de partida si saltas la evaluación.",
            fontSize = 16.sp, lineHeight = 21.sp,
            color = com.example.ui.theme.Clay.InkMuted
          )
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

        HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

        // Sound switch
        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.SpaceBetween,
          verticalAlignment = Alignment.CenterVertically
        ) {
          Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.weight(1f)) {
            KitGlyph(Glyph.SPEAKER)
            Spacer(modifier = Modifier.width(10.dp))
            Text(
              text = "Efectos de sonido",
              style = MaterialTheme.typography.bodyMedium,
              fontWeight = FontWeight.SemiBold
            )
          }
          ClaySwitch(
            checked = soundEnabled,
            onCheckedChange = {
              soundEnabled = it
              viewModel.updateSettings(
                name = nameInput,
                weeklyGoal = weeklyGoal,
                defaultTimed = defaultTimed,
                sound = it,
                haptics = hapticsEnabled,
                notificationsEnabled = notificationsEnabled,
                reminderHour = reminderHour,
                reminderMinute = reminderMinute
              )
            },
            modifier = Modifier.testTag("switch_sound"),
            label = "Efectos de sonido"
          )
        }

        HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

        // Haptics switch
        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.SpaceBetween,
          verticalAlignment = Alignment.CenterVertically
        ) {
          Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.weight(1f)) {
            KitGlyph(Glyph.VIBRATION)
            Spacer(modifier = Modifier.width(10.dp))
            Text(
              text = "Vibración",
              style = MaterialTheme.typography.bodyMedium,
              fontWeight = FontWeight.SemiBold
            )
          }
          ClaySwitch(
            checked = hapticsEnabled,
            onCheckedChange = {
              hapticsEnabled = it
              viewModel.updateSettings(
                name = nameInput,
                weeklyGoal = weeklyGoal,
                defaultTimed = defaultTimed,
                sound = soundEnabled,
                haptics = it,
                notificationsEnabled = notificationsEnabled,
                reminderHour = reminderHour,
                reminderMinute = reminderMinute
              )
            },
            modifier = Modifier.testTag("switch_haptics"),
            label = "Vibración"
          )
        }
      }
    }

    // Programmed Notifications Card (WorkManager)
    ClayPanel(modifier = Modifier.fillMaxWidth().testTag("card_notifications_workmanager"), verticalSpacing = 16.dp) {
      run {
        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.SpaceBetween,
          verticalAlignment = Alignment.CenterVertically
        ) {
          Row(verticalAlignment = Alignment.CenterVertically) {
            KitGlyph(Glyph.BELL)
            Spacer(modifier = Modifier.width(8.dp))
            Column {
              Text(
                text = "Recordatorio diario",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
              )
              Text(
                text = "Un aviso al día",
                style = MaterialTheme.typography.labelMedium,
                color = PanelAccent
              )
            }
          }

          ClaySwitch(
            checked = notificationsEnabled,
            onCheckedChange = { isChecked ->
              if (isChecked) {
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                  notificationPermissionLauncher.launch(android.Manifest.permission.POST_NOTIFICATIONS)
                } else {
                  notificationsEnabled = true
                  viewModel.updateSettings(
                    name = nameInput,
                    weeklyGoal = weeklyGoal,
                    defaultTimed = defaultTimed,
                    sound = soundEnabled,
                    haptics = hapticsEnabled,
                    notificationsEnabled = true,
                    reminderHour = reminderHour,
                    reminderMinute = reminderMinute
                  )
                  Toast.makeText(context, "Recordatorio diario activado", Toast.LENGTH_SHORT).show()
                }
              } else {
                notificationsEnabled = false
                viewModel.updateSettings(
                  name = nameInput,
                  weeklyGoal = weeklyGoal,
                  defaultTimed = defaultTimed,
                  sound = soundEnabled,
                  haptics = hapticsEnabled,
                  notificationsEnabled = false,
                  reminderHour = reminderHour,
                  reminderMinute = reminderMinute
                )
                Toast.makeText(context, "Recordatorio desactivado", Toast.LENGTH_SHORT).show()
              }
            },
            modifier = Modifier.testTag("switch_notifications_enabled"),
            label = "Recordatorio diario"
          )
        }

        Text(
          text = "Un aviso cada día, a la hora que elijas, para acordarte de jugar. Puedes apagarlo cuando quieras.",
          style = MaterialTheme.typography.bodySmall,
          color = MaterialTheme.colorScheme.onSurfaceVariant
        )

        if (notificationsEnabled) {
          HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

          // Schedule time selector
          Text(
            text = "Hora del recordatorio",
            style = MaterialTheme.typography.bodyMedium,
            fontWeight = FontWeight.SemiBold
          )

          // Las cuatro horas en una cuadrícula de 2 × 2: la elegida en lima, con su ✓ y quieta.
          Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
            val times = listOf(
              Triple("Mañana", 9, 0),
              Triple("Tarde", 15, 0),
              Triple("Al atardecer", 19, 0),
              Triple("Noche", 21, 0)
            )

            times.chunked(2).forEach { pair -> Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) { pair.forEach { (label, h, m) ->
              val isSelected = reminderHour == h && reminderMinute == m
              ClayChoice(
                selected = isSelected,
                modifier = Modifier.weight(1f).testTag("btn_time_${label.lowercase().replace(" ", "_")}"),
                onClick = {
                  reminderHour = h
                  reminderMinute = m
                  viewModel.updateSettings(
                    name = nameInput,
                    weeklyGoal = weeklyGoal,
                    defaultTimed = defaultTimed,
                    sound = soundEnabled,
                    haptics = hapticsEnabled,
                    notificationsEnabled = true,
                    reminderHour = h,
                    reminderMinute = m
                  )
                  Toast.makeText(context, "Recordatorio fijado a las %02d:%02d".format(h, m), Toast.LENGTH_SHORT).show()
                }
              ) {
                Column(Modifier.weight(1f)) {
                  Text(text = label, fontSize = 16.sp, fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Medium)
                  Text(text = "%02d:%02d".format(h, m), fontSize = 16.sp, color = Clay.InkMuted)
                }
              }
            } } }
          }

          // Test notification button
          ClayButton(
            text = "Probar el aviso ahora", color = Clay.Cream,
            onClick = {
              viewModel.triggerTestNotification()
              Toast.makeText(context, "Aviso de prueba enviado", Toast.LENGTH_SHORT).show()
            },
            modifier = Modifier.testTag("btn_test_notification")
          )
        }
      }
    }

    // Privacy Card
    ClayPanel(modifier = Modifier.fillMaxWidth(), padding = 16.dp) {
      Row(
        verticalAlignment = Alignment.CenterVertically
      ) {
        KitGlyph(Glyph.SHIELD)
        Spacer(modifier = Modifier.width(12.dp))
        Column {
          Text(
            text = "Tu progreso es tuyo",
            style = MaterialTheme.typography.titleSmall,
            fontWeight = FontWeight.Bold,
            color = PanelAccent
          )
          Text(
            text = "Sin cuenta, sin internet y sin anuncios. Tu progreso se guarda en tu teléfono y en la copia de seguridad de Android, si la tienes activada.",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
          )
        }
      }
    }

    // Aviso de salud (lo exige Google Play; texto aprobado por Ricardo, Tarea 121): sección con título y letra de lectura, no una nota chica. El mismo texto va en «Licencias y créditos».
    ClayPanel(modifier = Modifier.fillMaxWidth().testTag("health_notice"), padding = 16.dp) {
      Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
          KitGlyph(Glyph.INFO)
          Spacer(modifier = Modifier.width(12.dp))
          Text(
            text = com.example.data.HealthNotice.TITLE,
            style = MaterialTheme.typography.titleMedium,
            color = MaterialTheme.colorScheme.onSurface
          )
        }
        Text(
          text = com.example.data.HealthNotice.TEXT,
          style = MaterialTheme.typography.bodyMedium,
          color = MaterialTheme.colorScheme.onSurfaceVariant
        )
      }
    }

    // Herramientas de prueba (solo en builds de depuración), ver DebugTools.kt.
    if (com.example.BuildConfig.DEBUG) {
      DebugTools(viewModel, userSettings.id, ageBand)
      Spacer(modifier = Modifier.height(12.dp))
    }

    // Informe de errores, sin servidores ni cuentas: la persona lo comparte con quien le dio la app (ver diag/ErrorLog).
    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
      ClayButton(
        text = "Enviar informe de errores", color = Clay.Cream,
        onClick = {
          val send = android.content.Intent(android.content.Intent.ACTION_SEND)
            .setType("text/plain")
            .putExtra(android.content.Intent.EXTRA_SUBJECT, "Informe de errores de Nubi")
            .putExtra(android.content.Intent.EXTRA_TEXT, com.example.diag.ErrorLog.report(context, viewModel.unclearReport()))
          context.startActivity(
            android.content.Intent.createChooser(send, "Enviar informe").addFlags(android.content.Intent.FLAG_ACTIVITY_NEW_TASK)
          )
        },
        modifier = Modifier.testTag("btn_send_error_report")
      )
      Text(
        text = "Si algo falla, comparte este informe con quien te dio la app. No incluye tu nombre; si una partida no se pudo guardar, incluye los datos de esa partida para encontrar el error.",
        style = MaterialTheme.typography.bodyMedium,
        color = MaterialTheme.colorScheme.onSurfaceVariant
      )
    }
    Spacer(modifier = Modifier.height(12.dp))

    ClayButton(text = "Licencias y créditos", onClick = onOpenLicenses, color = Clay.Cream, modifier = Modifier.testTag("btn_licenses"))
    Spacer(modifier = Modifier.height(12.dp))

    // Reset Data button
    // Borra datos: va en coral (y su texto lo dice), no solo por el color.
    ClayButton(text = "Restablecer datos y puntuaciones", onClick = { showResetDialog = true }, color = Clay.Coral, modifier = Modifier.testTag("btn_reset_data"))

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
