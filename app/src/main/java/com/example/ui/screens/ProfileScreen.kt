package com.example.ui.screens

import com.example.ui.components.SlabSize
import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.foundation.layout.widthIn
import androidx.compose.runtime.DisposableEffect
import androidx.compose.ui.focus.onFocusChanged
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.style.TextAlign
import com.example.data.AchievementDef
import com.example.data.AchievementStats
import com.example.data.Achievements
import com.example.data.familyState
import com.example.data.Streak
import com.example.model.RankTier
import com.example.ui.components.AchievementMedal
import com.example.ui.components.medalColor
import com.example.ui.components.DomainLegend
import com.example.ui.components.LeagueShield
import com.example.ui.components.SpaceSectionTitle
import com.example.ui.components.levelWord
import com.example.ui.components.overallIndex
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.viewmodel.NeuroVidaViewModel

/**
 * Cabecera de los paneles que se abren encima de una pestaña (los Ajustes): flecha para volver y el título.
 */
@Composable
fun PanelHeader(title: String, onBack: () -> Unit) {
  Row(
    modifier = Modifier
      .fillMaxWidth()
      .padding(horizontal = 12.dp, vertical = 8.dp),
    verticalAlignment = Alignment.CenterVertically
  ) {
    com.example.ui.components.KitBackButton(onBack, Modifier.testTag("btn_panel_back"))
    Spacer(Modifier.width(12.dp))
    com.example.ui.components.OnBoardText(title, color = Color(0xFFEAF0FF), fontSize = 22.sp, fontWeight = FontWeight.Bold)
  }
}

/** Los Ajustes (la losa «Ajustes» de «Tú»), con su flecha de regreso. */
@Composable
fun SettingsPanel(viewModel: NeuroVidaViewModel, onClose: () -> Unit, modifier: Modifier = Modifier) {
  var showLicenses by remember { mutableStateOf(false) }
  if (showLicenses) {
    // "Licencias y créditos" (1-oct): su propia cabecera; Atrás vuelve a Opciones.
    LicensesScreen(onClose = { showLicenses = false }, modifier = modifier)
    return
  }
  BackHandler(onBack = onClose)
  Column(modifier = modifier.fillMaxSize()) {
    PanelHeader("Ajustes", onClose)
    SettingsScreen(viewModel = viewModel, modifier = Modifier.weight(1f), onOpenLicenses = { showLicenses = true })
  }
}

/**
 * Tú (la tercera pestaña, Tarea 158, 21-oct, «La isla es el mapa»; antes el Perfil era un panel que se abría con tu inicial, arriba a la derecha, y estaba escondido): lo tuyo en un solo sitio, con nombre. Tu isla con las piezas que ganaste, tu nombre con «Cambiar», Racha · Partidas · Medallas,
 * tus medallas (y las anteriores) y, al final, una losa «Ajustes» que abre los Ajustes como panel (Licencias sigue adentro). Es una página de pestaña como Hoy y Avance: sin X (se sale con la barra de abajo) y con la franja de la barra libre (`LocalBarInset`).
 * Cuando las ligas vuelvan (`Features.LEAGUES`), «Tu liga» va aquí, donde ya estaba en el perfil; hoy no se ve.
 */
@Composable
fun TuScreen(viewModel: NeuroVidaViewModel, modifier: Modifier = Modifier) {
  val userSettings by viewModel.userSettings.collectAsState()
  val streak by viewModel.currentStreak.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val ranks by viewModel.gameRanks.collectAsState()
  val levels by viewModel.gameLevelsForProgress.collectAsState()
  val unlocks by viewModel.achievementUnlocks.collectAsState()
  val achStats by viewModel.achievementStats.collectAsState()
  var achievementDetail by remember { mutableStateOf<AchievementDef?>(null) }
  var familyOpen by remember { mutableStateOf<com.example.data.AchievementFamily?>(null) }
  val medals = remember(unlocks, achStats) { com.example.data.AchievementFamily.entries.map { Achievements.familyState(it, unlocks.keys, achStats) } }

  val avg = if (ranks.isEmpty()) 0 else ranks.sumOf { it.rating } / ranks.size
  val tier = RankTier.fromRating(avg)
  val index = overallIndex(levels)
  val medalCount = medals.sumOf { it.level }                    // los niveles ganados en las tres familias: el mismo número de «Tus medallas» (Tarea 145; antes «Mejor», el puntaje más alto, que no dice mejor en qué con 17 juegos de escalas distintas)

  val prog = if (tier == RankTier.MAESTRO) ((avg - tier.minRating) % 250) / 250f else (avg - tier.minRating) / 250f

  Column(modifier.fillMaxSize().padding(bottom = com.example.ui.components.LocalBarInset.current)) {      // la franja de la barra de pestañas queda libre
  Row(Modifier.fillMaxWidth().padding(start = 20.dp, end = 16.dp, top = 8.dp, bottom = 4.dp), verticalAlignment = Alignment.CenterVertically) {
    com.example.ui.components.OnBoardText("Tú", color = Color(0xFFEAF0FF), fontSize = 26.sp, fontWeight = FontWeight.Bold, modifier = Modifier.semantics { heading() }.testTag("tu_title"))
  }
  LazyColumn(
    modifier = Modifier
      .weight(1f)
      .padding(horizontal = 22.dp),
    contentPadding = PaddingValues(top = 8.dp, bottom = 28.dp),
    verticalArrangement = Arrangement.spacedBy(22.dp)
  ) {
    // Nubi y tu nombre, sin recuadros (Tarea 143, R2 b: mientras las ligas esperan la decisión de negocio, el héroe del perfil es Nubi y el escudo, un dibujo «de antes», queda chico en «Tu liga»)
    item {
      Column(modifier = Modifier.fillMaxWidth(), horizontalAlignment = Alignment.CenterHorizontally) {
        // La isla en miniatura con las piezas que la persona ya ganó (Tarea 147, P3: antes Nubi sobre el nombre, que parecía su avatar)
        com.example.ui.components.Island(
          description = "Tu isla" + com.example.data.IslaPiezas.description(com.example.data.IslaPiezas.earned(unlocks.keys)),
          modifier = Modifier.fillMaxWidth().height(150.dp).testTag("profile_island"),
          pieces = com.example.data.IslaPiezas.earned(unlocks.keys)
        )
        // El nombre en GRANDE, como siempre, y un botón chico del kit «Cambiar» (Opus, 21-oct: un campo siempre abierto bajo la isla se leía como un formulario, sin rótulo, y le quitaba el protagonismo al nombre; es el mismo patrón de «Cambiar cómo jugar» de la ficha).
        // Al tocarlo, el nombre pasa a un campo con el rótulo «Tu nombre» y el teclado abierto; se guarda con «Listo» del teclado, al salir del campo o al salir del Perfil, y vuelve a texto (Tarea 153; antes de la 152 estaba en Ajustes con un botón «Guardar»).
        val focusManager = androidx.compose.ui.platform.LocalFocusManager.current
        var editing by remember { mutableStateOf(false) }
        var nameDraft by remember(userSettings.name) { mutableStateOf(userSettings.name) }
        var hadFocus by remember { mutableStateOf(false) }
        val nameFocus = remember { androidx.compose.ui.focus.FocusRequester() }
        val saveName = { if (nameDraft.trim() != userSettings.name) viewModel.setName(nameDraft) }
        val latestSave by androidx.compose.runtime.rememberUpdatedState(saveName)
        DisposableEffect(Unit) { onDispose { latestSave() } }                                  // si se sale del perfil con el campo abierto, también se guarda
        if (!editing) {
          val shown = userSettings.name.ifBlank { "Sin nombre" }
          Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.padding(top = 6.dp)) {
            com.example.ui.components.OnBoardText(
              shown, color = Color(0xFFEAF0FF), fontSize = 30.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center,
              modifier = Modifier.testTag("profile_name").clearAndSetSemantics { }                // lo dice el botón de abajo: «Tu nombre, Ricardo, botón Cambiar»
            )
            com.example.ui.components.KitButton(
              text = "Cambiar", onClick = { editing = true; hadFocus = false }, color = com.example.ui.components.SlabColor.CREAM, fontSize = 16.sp, wide = false, minHeight = 48.dp,
              modifier = Modifier.padding(top = 4.dp).testTag("btn_change_name").semantics { contentDescription = "Tu nombre, $shown, botón Cambiar" }
            )
          }
        } else {
          Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.widthIn(max = 340.dp).padding(top = 6.dp)) {
            com.example.ui.components.OnBoardText("Tu nombre", color = com.example.ui.theme.NubiTokens.OnNightDim, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(bottom = 6.dp).testTag("name_label"))
            com.example.ui.components.ClayTextField(
              value = nameDraft, onValueChange = { nameDraft = it.take(30) },
              placeholder = "Tu nombre", fontSize = 22.sp,
              modifier = Modifier.focusRequester(nameFocus).testTag("input_user_name")
                .onFocusChanged { st -> if (st.isFocused) hadFocus = true else if (hadFocus) { saveName(); editing = false; hadFocus = false } },
              keyboardOptions = androidx.compose.foundation.text.KeyboardOptions(capitalization = androidx.compose.ui.text.input.KeyboardCapitalization.Words, imeAction = androidx.compose.ui.text.input.ImeAction.Done),
              keyboardActions = androidx.compose.foundation.text.KeyboardActions(onDone = { focusManager.clearFocus() })
            )
          }
          LaunchedEffect(Unit) { runCatching { nameFocus.requestFocus() } }                    // el teclado se abre solo
        }
        index?.let {
          com.example.ui.components.OnBoardText(
            text = "Etapa general: ${levelWord(it / 100f)}",                 // (ítem 17) «etapa», como en la lámina, el detalle y el resultado; antes «nivel hábil»
            color = com.example.ui.theme.NubiTokens.OnNightDim,
            fontSize = 16.sp,
            modifier = Modifier.testTag("profile_stage")
          )
        }
      }
    }

    // Tres cifras como texto grande, separadas por lineas finas
    item {
      Row(modifier = Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        StatText("Racha", "$streak", com.example.ui.theme.NubiTokens.OnNight, Modifier.weight(1f), fire = true)
        Box(Modifier.width(1.dp).height(44.dp).background(Color.White.copy(alpha = 0.14f)))
        StatText("Partidas", "${history.size}", com.example.ui.theme.NubiTokens.OnNight, Modifier.weight(1f))
        Box(Modifier.width(1.dp).height(44.dp).background(Color.White.copy(alpha = 0.14f)))
        StatText("Medallas", "$medalCount", com.example.ui.theme.NubiTokens.OnNight, Modifier.weight(1f).testTag("profile_medal_count"))
      }
    }
    // (Tarea 158: aquí NO va la frase de la racha ni la del descanso: vive solo en Hoy; quedan las cifras)

    // Tu liga: el escudo chico y la liga (el escudo grande queda para cuando se decidan las ligas). Fuera de la v1 (Tarea 149): se ve solo con `Features.LEAGUES`.
    if (com.example.Features.LEAGUES) item {
      Column(Modifier.testTag("profile_league")) {
        SpaceSectionTitle("Tu liga")
        Spacer(Modifier.height(10.dp))
        Row(verticalAlignment = Alignment.CenterVertically) {
          com.example.ui.components.ShieldImage(tier, 40.dp)
          Spacer(Modifier.width(12.dp))
          Column {
            com.example.ui.components.OnBoardText("Liga ${tier.tierName}", color = Color(0xFFEAF0FF), fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp)
            com.example.ui.components.OnBoardText("Cada partida suma", color = com.example.ui.theme.NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 21.sp)
          }
        }
      }
    }

    // Tus medallas: las tres familias por conducta (Constancia, Variedad y Volver), con su último nivel y lo que falta; tocar una abre sus niveles. (Ítem 17: «medallas», como en Avance; antes «Logros».)
    item {
      Column {
        SpaceSectionTitle("Tus medallas", hint = "Por lo que haces, no por el puntaje")
        Spacer(Modifier.height(14.dp))
        FamilyMedalCards(medals) { familyOpen = it }
      }
    }

    // Los logros de desempeño (puntaje, Reto y ligas) ya no se otorgan; los que ya ganó se conservan aquí (Tarea 124). Quien no ganó ninguno no ve esta sección.
    val oldMedals = Achievements.legacyEarned(unlocks.keys)
    if (oldMedals.isNotEmpty()) item {
      Column {
        SpaceSectionTitle("Medallas anteriores", hint = "Las que ganaste antes de que cambiaran las medallas")
        Spacer(Modifier.height(14.dp))
        AchievementGrid(oldMedals, unlocks.keys, achStats) { achievementDetail = it }
      }
    }

    // Los Ajustes (antes el engranaje de arriba a la derecha): una losa del kit al final de lo tuyo; abre el panel de Ajustes.
    item {
      com.example.ui.components.KitButton(
        text = "Ajustes", onClick = { viewModel.openTopPanel(com.example.viewmodel.TopPanel.AJUSTES) }, color = com.example.ui.components.SlabColor.CREAM,
        glyph = com.example.ui.components.Glyph.GEAR, fontSize = 18.sp, modifier = Modifier.testTag("btn_open_settings")
      )
    }
  }
  }

  achievementDetail?.let { def ->
    AchievementDetailDialog(def, unlocks[def.id], achStats) { achievementDetail = null }
  }
  familyOpen?.let { f -> medals.firstOrNull { it.family == f }?.let { FamilyMedalDialog(it, unlocks, achStats) { familyOpen = null } } }
}

/** Detalle de un logro: medalla grande, qué hay que hacer, cuándo se consiguió o cuánto falta. */
@Composable
private fun AchievementDetailDialog(def: AchievementDef, unlockedAt: Long?, stats: AchievementStats, onDismiss: () -> Unit) {
  androidx.compose.ui.window.Dialog(onDismissRequest = onDismiss) {
    com.example.ui.theme.ClayCard(
      modifier = Modifier.fillMaxWidth().padding(8.dp),
      color = Clay.Cream,
      size = SlabSize.LARGE,
      contentPadding = 22.dp
    ) {
      Column(modifier = Modifier.fillMaxWidth(), horizontalAlignment = Alignment.CenterHorizontally) {
        Box(
          modifier = Modifier
            .clip(CircleShape)
            .background(Color(0xFF0A0F33))
            .padding(10.dp)
        ) { AchievementMedal(def = def, unlocked = unlockedAt != null, size = 110.dp) }
        Text(def.title, color = Clay.Ink, fontSize = 24.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 10.dp))
        Text(def.description, color = Clay.InkSoft, fontSize = 16.sp, textAlign = TextAlign.Center)
        Spacer(Modifier.height(10.dp))
        if (unlockedAt != null) {
          val f = java.text.SimpleDateFormat("d 'de' MMMM yyyy", java.util.Locale("es"))
          Text("Conseguido el ${f.format(java.util.Date(unlockedAt))}", color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
        } else {
          val (cur, goal) = def.progress(stats)
          Text("Llevas $cur de $goal", color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
          Box(
            modifier = Modifier
              .padding(top = 8.dp)
              .fillMaxWidth()
              .height(10.dp)
              .clip(RoundedCornerShape(5.dp))
              .background(Clay.Ink.copy(alpha = 0.12f))
          ) {
            Box(
              modifier = Modifier
                .fillMaxWidth((cur.toFloat() / goal).coerceIn(0.03f, 1f))
                .height(10.dp)
                .background(def.medalColor())
            )
          }
        }
      }
    }
  }
}

@Composable
private fun StatText(label: String, value: String, color: Color, modifier: Modifier, fire: Boolean = false) {
  Column(modifier = modifier, horizontalAlignment = Alignment.CenterHorizontally) {
    Row(verticalAlignment = Alignment.CenterVertically) {
      if (fire) {
        com.example.ui.components.KitLlama(lit = (value.toIntOrNull() ?: 0) > 0, size = 22.dp)
        Spacer(Modifier.width(6.dp))
      }
      com.example.ui.components.OnBoardText(value, color = color, fontSize = 34.sp, fontWeight = FontWeight.Bold)
    }
    com.example.ui.components.OnBoardText(label, color = com.example.ui.theme.NubiTokens.OnNightDim, fontSize = 16.sp)
  }
}

/** Una rejilla de medallas (casillas de plasticina tocables): las bloqueadas muestran cuánto falta. Cuántas por fila lo decide la palabra más larga: nunca se parte una. */
@Composable
private fun AchievementGrid(defs: List<AchievementDef>, unlocked: Set<String>, achStats: AchievementStats, onTap: (AchievementDef) -> Unit) {
        androidx.compose.foundation.layout.BoxWithConstraints(Modifier.fillMaxWidth()) {
        val needed = 90.dp * androidx.compose.ui.platform.LocalDensity.current.fontScale
        val cols = (4 downTo 1).firstOrNull { maxWidth / it - 8.dp >= needed } ?: 1
        Column {
        defs.chunked(cols).forEach { row ->
          Row(modifier = Modifier.fillMaxWidth().height(IntrinsicSize.Min).padding(bottom = 4.dp)) {
            row.forEach { def ->
              val got = def.id in unlocked
              // Cada medalla es una casilla de plasticina tocable (losa chica crema): tocarla abre su detalle.
              com.example.ui.theme.ClayCard(
                modifier = Modifier.weight(1f).fillMaxHeight().padding(horizontal = 2.dp).testTag("achievement_${def.id}"),
                size = SlabSize.SMALL, onClick = { onTap(def) }, contentPadding = 0.dp
              ) {
                Column(Modifier.fillMaxWidth().padding(horizontal = 2.dp, vertical = 6.dp), horizontalAlignment = Alignment.CenterHorizontally) {
                  AchievementMedal(def = def, unlocked = got, size = 56.dp)
                  Text(
                    text = def.title,
                    color = if (got) Clay.Ink else Clay.InkMuted,
                    fontSize = 16.sp,
                    fontWeight = if (got) FontWeight.Bold else FontWeight.Normal,
                    textAlign = TextAlign.Center,
                    maxLines = 2,
                    lineHeight = 20.sp
                  )
                  if (!got) {
                    val (cur, goal) = def.progress(achStats)
                    Text("$cur/$goal", color = Clay.Ink, fontSize = 16.sp)
                  }
                }
              }
            }
            repeat(cols - row.size) { Spacer(Modifier.weight(1f)) }
          }
        }
        }
        }
}
