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
import com.example.ui.i18n.strings
import com.example.ui.theme.EmeraldAccent
import com.example.ui.theme.TealPrimary
import com.example.viewmodel.NeuroVidaViewModel

@Composable
fun SettingsScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier,
  /** Abre "Licencias y créditos" (la pantalla la muestra el panel que contiene a Ajustes). */
  onOpenLicenses: () -> Unit = {}
) {
  val userSettings by viewModel.userSettings.collectAsState()
  val resultFocus by viewModel.resultFocus.collectAsState()
  val colorVision by viewModel.colorVision.collectAsState()

  var nameInput by remember(userSettings.name) { mutableStateOf(userSettings.name) }
  var selectedAvatar by remember(userSettings.avatar) { mutableStateOf(userSettings.avatar) }
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

    Text(
      text = strings.settingsTitle,
      style = MaterialTheme.typography.headlineMedium,
      fontWeight = FontWeight.Black,
      color = MaterialTheme.colorScheme.onBackground
    )

    // Language Selection Card (Internationalization)
    Card(
      modifier = Modifier.fillMaxWidth().testTag("card_language_selector"),
      shape = RoundedCornerShape(22.dp),
      colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
      Column(
        modifier = Modifier.padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(14.dp)
      ) {
        Row(
          verticalAlignment = Alignment.CenterVertically,
          horizontalArrangement = Arrangement.SpaceBetween,
          modifier = Modifier.fillMaxWidth()
        ) {
          Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(Icons.Default.Language, contentDescription = null, tint = TealPrimary)
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

        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
          AppLanguage.entries.forEach { lang ->
            val isSelected = appLanguage == lang
            FilterChip(
              selected = isSelected,
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
              label = {
                Text(
                  text = "${lang.flagEmoji} ${lang.displayName}",
                  fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Normal
                )
              },
              colors = FilterChipDefaults.filterChipColors(
                selectedContainerColor = TealPrimary.copy(alpha = 0.15f),
                selectedLabelColor = TealPrimary
              ),
              modifier = Modifier.testTag("chip_lang_${lang.code}")
            )
          }
        }
      }
    }

    // 1. Tu perfil: el nombre y el avatar propios (8-oct: se quitó la gestión de varios perfiles; la tabla user_profile se queda, sin migración)
    Card(
      modifier = Modifier.fillMaxWidth(),
      shape = RoundedCornerShape(22.dp),
      colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
      Column(modifier = Modifier.padding(20.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
          Icon(Icons.Default.Person, contentDescription = null, tint = TealPrimary)
          Spacer(modifier = Modifier.width(8.dp))
          Text(
            text = "Tu perfil",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold
          )
        }

        Spacer(modifier = Modifier.height(8.dp))

        // Nombre y avatar propios
        Text(
          text = "Tu avatar:",
          style = MaterialTheme.typography.bodySmall,
          color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(modifier = Modifier.height(6.dp))

        val avatarChoices = listOf("🧠", "👵", "👴", "🌟", "🎯", "🦉", "🌿", "💡", "⚡")
        LazyRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
          items(avatarChoices) { av ->
            val isChosen = selectedAvatar == av
            Surface(
              shape = CircleShape,
              color = if (isChosen) TealPrimary.copy(alpha = 0.2f) else MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f),
              border = if (isChosen) BorderStroke(2.dp, TealPrimary) else BorderStroke(1.dp, Color.Transparent),
              modifier = Modifier
                .size(42.dp)
                .clickable { selectedAvatar = av }
            ) {
              Box(contentAlignment = Alignment.Center) {
                Text(text = av, fontSize = 20.sp)
              }
            }
          }
        }

        Spacer(modifier = Modifier.height(12.dp))

        OutlinedTextField(
          value = nameInput,
          onValueChange = { nameInput = it },
          label = { Text("Tu nombre") },
          singleLine = true,
          modifier = Modifier
            .fillMaxWidth()
            .testTag("input_user_name"),
          shape = RoundedCornerShape(14.dp),
          colors = OutlinedTextFieldDefaults.colors(
            focusedBorderColor = TealPrimary,
            focusedLabelColor = TealPrimary
          )
        )

        Spacer(modifier = Modifier.height(12.dp))

        Button(
          onClick = {
            viewModel.updateSettings(
              name = nameInput.trim().ifEmpty { "Usuario" },
              avatar = selectedAvatar,
              weeklyGoal = weeklyGoal,
              defaultTimed = defaultTimed,
              sound = soundEnabled,
              haptics = hapticsEnabled,
              notificationsEnabled = notificationsEnabled,
              reminderHour = reminderHour,
              reminderMinute = reminderMinute,
              difficultyMode = difficultyMode
            )
            Toast.makeText(context, "Perfil guardado", Toast.LENGTH_SHORT).show()
          },
          shape = RoundedCornerShape(12.dp),
          colors = ButtonDefaults.buttonColors(containerColor = TealPrimary),
          modifier = Modifier
            .align(Alignment.End)
            .testTag("btn_save_name")
        ) {
          Icon(Icons.Outlined.Check, contentDescription = null, modifier = Modifier.size(16.dp))
          Spacer(modifier = Modifier.width(6.dp))
          Text("Guardar")
        }
      }
    }

    // 2. Meta semanal de juego
    Card(
      modifier = Modifier.fillMaxWidth(),
      shape = RoundedCornerShape(22.dp),
      colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
      Column(modifier = Modifier.padding(20.dp)) {
        Text(
          text = "Meta semanal de juego",
          style = MaterialTheme.typography.titleMedium,
          fontWeight = FontWeight.Bold
        )
        Text(
          text = "$weeklyGoal días por semana",
          style = MaterialTheme.typography.bodyLarge,
          fontWeight = FontWeight.Black,
          color = TealPrimary,
          modifier = Modifier.padding(top = 4.dp)
        )

        Spacer(modifier = Modifier.height(8.dp))

        Slider(
          value = weeklyGoal.toFloat(),
          onValueChange = {
            weeklyGoal = it.toInt()
            viewModel.updateSettings(
              name = nameInput,
              avatar = selectedAvatar,
              weeklyGoal = weeklyGoal,
              defaultTimed = defaultTimed,
              sound = soundEnabled,
              haptics = hapticsEnabled,
              notificationsEnabled = notificationsEnabled,
              reminderHour = reminderHour,
              reminderMinute = reminderMinute,
              difficultyMode = difficultyMode
            )
          },
          valueRange = 1f..7f,
          steps = 5,
          colors = SliderDefaults.colors(
            thumbColor = TealPrimary,
            activeTrackColor = TealPrimary
          ),
          modifier = Modifier.testTag("slider_weekly_goal")
        )
      }
    }

    // (28-sep) La tarjeta "Dificultad Cognitiva" (modo Principiante/Intermedio/Avanzado, niveles por dominio,
    // asistencia y ritmo) se quitó: no llegaba a los juegos. La dificultad se elige por juego antes de jugar
    // (Suave / A tu medida / Desafío / Experto): ver docs/dificultad-y-avance.md.

    // Game Mode & Audio/Haptic Switches
    Card(
      modifier = Modifier.fillMaxWidth(),
      shape = RoundedCornerShape(22.dp),
      colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
      Column(
        modifier = Modifier.padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
      ) {
        Text(
          text = "Experiencia de Juego",
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
            Icon(Icons.Default.Person, contentDescription = null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(modifier = Modifier.width(10.dp))
            Text(
              text = "Rango de edad",
              style = MaterialTheme.typography.bodyMedium,
              fontWeight = FontWeight.SemiBold
            )
          }
          Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            AgeBand.entries.forEach { band ->
              FilterChip(
                selected = ageBand == band,
                onClick = {
                  ageBand = band
                  viewModel.setAgeBand(band)
                },
                label = { Text(text = band.label) },
                colors = FilterChipDefaults.filterChipColors(
                  selectedContainerColor = TealPrimary.copy(alpha = 0.15f),
                  selectedLabelColor = TealPrimary
                ),
                modifier = Modifier.testTag("chip_age_band_${band.name.lowercase()}")
              )
            }
          }
          Text(
            text = "Por ahora ajusta la dificultad de Constelaciones.",
            style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
          )
        }

        HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

        // Qué te sirve más ver primero al terminar un juego y si cuesta distinguir colores: dos preguntas del inicio, editables acá.
        Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
          Text(text = "Al terminar cada juego, ¿qué te sirve más ver primero?", style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.SemiBold)
          Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            com.example.data.ResultFocus.entries.forEach { focus ->
              FilterChip(
                selected = resultFocus == focus,
                onClick = { viewModel.setResultFocus(focus) },
                label = { Text(text = focus.label) },
                colors = FilterChipDefaults.filterChipColors(
                  selectedContainerColor = TealPrimary.copy(alpha = 0.15f),
                  selectedLabelColor = TealPrimary
                ),
                modifier = Modifier.testTag("chip_result_focus_${focus.name.lowercase()}")
              )
            }
          }
          Text(
            text = "Así ordeno lo que te muestro al final: no se oculta nada, solo cambia qué va primero.",
            style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
          )
        }

        HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

        Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
          Text(text = "¿Te cuesta distinguir algunos colores?", style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.SemiBold)
          Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            com.example.data.ColorVision.entries.forEach { vision ->
              FilterChip(
                selected = colorVision == vision,
                onClick = { viewModel.setColorVision(vision) },
                label = { Text(text = vision.label) },
                colors = FilterChipDefaults.filterChipColors(
                  selectedContainerColor = TealPrimary.copy(alpha = 0.15f),
                  selectedLabelColor = TealPrimary
                ),
                modifier = Modifier.testTag("chip_color_vision_${vision.name.lowercase()}")
              )
            }
          }
          Text(
            text = "Por ahora solo se guarda: los juegos que dependen del color lo usarán más adelante.",
            style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
          )
        }

        HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

        // Sound switch
        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.SpaceBetween,
          verticalAlignment = Alignment.CenterVertically
        ) {
          Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.weight(1f)) {
            Icon(Icons.AutoMirrored.Filled.VolumeUp, contentDescription = null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(modifier = Modifier.width(10.dp))
            Text(
              text = "Efectos de sonido",
              style = MaterialTheme.typography.bodyMedium,
              fontWeight = FontWeight.SemiBold
            )
          }
          Switch(
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
            colors = SwitchDefaults.colors(checkedThumbColor = Color.White, checkedTrackColor = TealPrimary),
            modifier = Modifier.testTag("switch_sound")
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
            Icon(Icons.Default.Vibration, contentDescription = null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(modifier = Modifier.width(10.dp))
            Text(
              text = "Vibración y respuesta háptica",
              style = MaterialTheme.typography.bodyMedium,
              fontWeight = FontWeight.SemiBold
            )
          }
          Switch(
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
            colors = SwitchDefaults.colors(checkedThumbColor = Color.White, checkedTrackColor = TealPrimary),
            modifier = Modifier.testTag("switch_haptics")
          )
        }
      }
    }

    // Programmed Notifications Card (WorkManager)
    Card(
      modifier = Modifier
        .fillMaxWidth()
        .testTag("card_notifications_workmanager"),
      shape = RoundedCornerShape(22.dp),
      colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
      Column(
        modifier = Modifier.padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
      ) {
        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.SpaceBetween,
          verticalAlignment = Alignment.CenterVertically
        ) {
          Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(Icons.Default.NotificationsActive, contentDescription = null, tint = TealPrimary)
            Spacer(modifier = Modifier.width(8.dp))
            Column {
              Text(
                text = "Recordatorio diario",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
              )
              Text(
                text = "Un aviso al día",
                style = MaterialTheme.typography.labelSmall,
                color = TealPrimary
              )
            }
          }

          Switch(
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
            colors = SwitchDefaults.colors(checkedThumbColor = Color.White, checkedTrackColor = TealPrimary),
            modifier = Modifier.testTag("switch_notifications_enabled")
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

          Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
          ) {
            val times = listOf(
              Triple("Mañana", 9, 0),
              Triple("Tarde", 15, 0),
              Triple("Noche", 19, 0),
              Triple("21:00", 21, 0)
            )

            times.forEach { (label, h, m) ->
              val isSelected = reminderHour == h && reminderMinute == m
              OutlinedButton(
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
                },
                modifier = Modifier.weight(1f).testTag("btn_time_${label.lowercase()}"),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.outlinedButtonColors(
                  containerColor = if (isSelected) TealPrimary.copy(alpha = 0.15f) else Color.Transparent
                ),
                border = androidx.compose.foundation.BorderStroke(
                  width = if (isSelected) 2.dp else 1.dp,
                  color = if (isSelected) TealPrimary else MaterialTheme.colorScheme.outlineVariant
                ),
                contentPadding = PaddingValues(vertical = 8.dp, horizontal = 4.dp)
              ) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                  Text(
                    text = label,
                    style = MaterialTheme.typography.labelMedium,
                    fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Normal,
                    color = if (isSelected) TealPrimary else MaterialTheme.colorScheme.onSurface
                  )
                  Text(
                    text = "%02d:%02d".format(h, m),
                    style = MaterialTheme.typography.labelSmall,
                    color = if (isSelected) TealPrimary else MaterialTheme.colorScheme.onSurfaceVariant
                  )
                }
              }
            }
          }

          // Test notification button
          OutlinedButton(
            onClick = {
              viewModel.triggerTestNotification()
              Toast.makeText(context, "Aviso de prueba enviado", Toast.LENGTH_SHORT).show()
            },
            modifier = Modifier
              .fillMaxWidth()
              .testTag("btn_test_notification"),
            shape = RoundedCornerShape(12.dp)
          ) {
            Icon(Icons.Default.Send, contentDescription = null, modifier = Modifier.size(16.dp))
            Spacer(modifier = Modifier.width(8.dp))
            Text("Probar el aviso ahora")
          }
        }
      }
    }

    // Privacy Card
    Surface(
      shape = RoundedCornerShape(18.dp),
      color = TealPrimary.copy(alpha = 0.08f),
      modifier = Modifier.fillMaxWidth()
    ) {
      Row(
        modifier = Modifier.padding(16.dp),
        verticalAlignment = Alignment.CenterVertically
      ) {
        Icon(Icons.Default.Security, contentDescription = null, tint = TealPrimary)
        Spacer(modifier = Modifier.width(12.dp))
        Column {
          Text(
            text = "Tu progreso es tuyo",
            style = MaterialTheme.typography.titleSmall,
            fontWeight = FontWeight.Bold,
            color = TealPrimary
          )
          Text(
            text = "Sin cuenta, sin internet y sin anuncios. Tu progreso se guarda en tu teléfono y en la copia de seguridad de Android, si la tienes activada.",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
          )
        }
      }
    }

    // Medical Disclaimer
    Surface(
      shape = RoundedCornerShape(18.dp),
      color = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.4f),
      modifier = Modifier.fillMaxWidth()
    ) {
      Row(
        modifier = Modifier.padding(16.dp),
        verticalAlignment = Alignment.Top
      ) {
        Icon(Icons.Default.Info, contentDescription = null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
        Spacer(modifier = Modifier.width(12.dp))
        Text(
          text = "Nubi es un juego para mantener la mente activa. No es una herramienta de salud: no diagnostica ni trata nada, y no promete resultados. Si te preocupa tu memoria, habla con un profesional.",
          style = MaterialTheme.typography.bodySmall,
          color = MaterialTheme.colorScheme.onSurfaceVariant,
          lineHeight = 16.sp
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
      OutlinedButton(
        onClick = {
          val send = android.content.Intent(android.content.Intent.ACTION_SEND)
            .setType("text/plain")
            .putExtra(android.content.Intent.EXTRA_SUBJECT, "Informe de errores de Nubi")
            .putExtra(android.content.Intent.EXTRA_TEXT, com.example.diag.ErrorLog.report(context, viewModel.unclearReport()))
          context.startActivity(
            android.content.Intent.createChooser(send, "Enviar informe").addFlags(android.content.Intent.FLAG_ACTIVITY_NEW_TASK)
          )
        },
        shape = RoundedCornerShape(14.dp),
        modifier = Modifier
          .fillMaxWidth()
          .testTag("btn_send_error_report")
      ) {
        Text("Enviar informe de errores")
      }
      Text(
        text = "Si algo falla, comparte este informe con quien te dio la app. No incluye tu nombre; si una partida no se pudo guardar, incluye los datos de esa partida para encontrar el error.",
        style = MaterialTheme.typography.bodyMedium,
        color = MaterialTheme.colorScheme.onSurfaceVariant
      )
    }
    Spacer(modifier = Modifier.height(12.dp))

    OutlinedButton(
      onClick = onOpenLicenses,
      shape = RoundedCornerShape(14.dp),
      modifier = Modifier
        .fillMaxWidth()
        .testTag("btn_licenses")
    ) {
      Text("Licencias y créditos")
    }
    Spacer(modifier = Modifier.height(12.dp))

    // Reset Data button
    OutlinedButton(
      onClick = { showResetDialog = true },
      colors = ButtonDefaults.outlinedButtonColors(contentColor = MaterialTheme.colorScheme.error),
      shape = RoundedCornerShape(14.dp),
      modifier = Modifier
        .fillMaxWidth()
        .testTag("btn_reset_data")
    ) {
      Text("Restablecer datos y puntuaciones")
    }

    Spacer(modifier = Modifier.height(90.dp))
  }

  if (showResetDialog) {
    AlertDialog(
      onDismissRequest = { showResetDialog = false },
      title = { Text("¿Restablecer datos?") },
      text = { Text("Esta acción borrará el historial de partidas y niveles guardados en este dispositivo.") },
      confirmButton = {
        TextButton(
          onClick = {
            viewModel.resetData()
            showResetDialog = false
          }
        ) {
          Text("Restablecer", color = MaterialTheme.colorScheme.error)
        }
      },
      dismissButton = {
        TextButton(onClick = { showResetDialog = false }) {
          Text("Cancelar")
        }
      }
    )
  }


}
