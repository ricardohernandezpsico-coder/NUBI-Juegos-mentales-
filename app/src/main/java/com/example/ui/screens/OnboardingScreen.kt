package com.example.ui.screens

import android.Manifest
import android.os.Build
import android.provider.Settings
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.activity.compose.BackHandler
import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.slideInHorizontally
import androidx.compose.animation.slideOutHorizontally
import androidx.compose.animation.togetherWith
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Achievements
import com.example.data.BaselinePlan
import com.example.data.Education
import com.example.model.AgeBand
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.AchievementMedal
import com.example.ui.components.CosmosBackground
import com.example.ui.components.GameIcon
import com.example.ui.components.LeagueShield
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.AppFamily
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.roundToInt
import kotlin.math.sin

private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = Color(0xFFB4BFEA)
private const val Pages = 9

/**
 * Primera experiencia (una sola vez, mientras `UserSettings.ageBand == null`): pasos cortos sobre el cielo de la
 * app, sin formularios largos ni recuadros. 1) Bienvenida: los 9 juegos orbitando. 2) Tu nombre (opcional).
 * 3) Rango de edad. 4) Nivel educacional (con "prefiero no decir"; sirve para comparar, no cambia la dificultad).
 * 5) Metas: qué quiere entrenar (hasta 3; el camino diario las prioriza). 6) Cuántos días por semana.
 * 7) Recordatorio diario: hora o ninguno (aquí se pide el permiso de notificaciones de Android 13+).
 * 8) Cómo funciona (camino diario, ligas, logros). 9) "Encontremos tu punto de partida": la evaluación de 3 juegos
 * cortos (al estilo del Fit Test de Lumosity) o "Hacerlo después". Ver data/Baseline.kt.
 * Atrás vuelve al paso anterior. Todo se guarda al final ([onFinish]).
 */
@Composable
fun OnboardingScreen(
  onFinish: (
    name: String, band: AgeBand, weeklyGoal: Int, reminderHour: Int?,
    education: Education?, goals: Set<DomainType>, startBaseline: Boolean
  ) -> Unit
) {
  var page by rememberSaveable { mutableIntStateOf(0) }
  var name by rememberSaveable { mutableStateOf("") }
  var band by rememberSaveable { mutableStateOf<AgeBand?>(null) }
  var education by rememberSaveable { mutableStateOf<Education?>(null) }
  var goals by rememberSaveable { mutableStateOf(emptySet<DomainType>()) }
  var goal by rememberSaveable { mutableIntStateOf(4) }
  // Hora del recordatorio (-1 = sin recordatorios). Se guarda recién si Android da el permiso de notificaciones.
  var reminderHour by rememberSaveable { mutableIntStateOf(19) }
  var askedHour by rememberSaveable { mutableIntStateOf(19) }
  var forward by remember { mutableStateOf(true) }
  fun go(to: Int) {
    forward = to > page
    page = to.coerceIn(0, Pages - 1)
  }

  BackHandler(enabled = page > 0) { go(page - 1) }

  // Android 13+: el permiso de notificaciones se pide acá, con contexto ("a la hora que elegiste").
  val notificationPermission = rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
    reminderHour = if (granted) askedHour else -1
    go(7)
  }
  fun pickReminder(hour: Int?) {
    if (hour == null) {
      reminderHour = -1
      go(7)
    } else if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
      askedHour = hour
      notificationPermission.launch(Manifest.permission.POST_NOTIFICATIONS)
    } else {
      reminderHour = hour
      go(7)
    }
  }

  Box(Modifier.fillMaxSize()) {
    CosmosBackground()
    Column(
      modifier = Modifier
        .fillMaxSize()
        .safeDrawingPadding()
        .imePadding()
        .padding(horizontal = 24.dp)
    ) {
      // Arriba: volver + avance en puntos
      Row(
        modifier = Modifier.fillMaxWidth().height(52.dp),
        verticalAlignment = Alignment.CenterVertically
      ) {
        if (page > 0) {
          Box(
            modifier = Modifier
              .size(44.dp)
              .clip(CircleShape)
              .background(Clay.Cream)
              .border(Clay.Border, Clay.Ink, CircleShape)
              .clickable { go(page - 1) },
            contentAlignment = Alignment.Center
          ) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Volver", tint = Clay.Ink) }
        } else {
          Spacer(Modifier.size(44.dp))
        }
        Spacer(Modifier.weight(1f))
        Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
          for (i in 0 until Pages) {
            Box(
              Modifier
                .size(width = if (i == page) 22.dp else 8.dp, height = 8.dp)
                .clip(RoundedCornerShape(4.dp))
                .background(if (i <= page) Clay.Sun else Color.White.copy(alpha = 0.2f))
            )
          }
        }
        Spacer(Modifier.weight(1f))
        Spacer(Modifier.size(44.dp))
      }

      AnimatedContent(
        targetState = page,
        transitionSpec = {
          val dir = if (forward) 1 else -1
          (slideInHorizontally { it / 4 * dir } + fadeIn(tween(260))) togetherWith
            (slideOutHorizontally { -it / 4 * dir } + fadeOut(tween(200)))
        },
        modifier = Modifier.weight(1f).fillMaxWidth(),
        label = "onboardingPage"
      ) { p ->
        when (p) {
          0 -> WelcomePage(onNext = { go(1) })
          1 -> NamePage(name = name, onName = { name = it.take(24) }, onNext = { go(2) })
          2 -> ChoicePage(
            title = "¿En qué rango de edad estás?",
            hint = "Con esto ajustamos el ritmo y el tamaño de los juegos. Puedes cambiarlo cuando quieras en Ajustes.",
            options = AgeBand.entries.map { it.label to null },
            selected = band?.ordinal,
            tagPrefix = "age_band_",
            tags = AgeBand.entries.map { it.name.lowercase() },
            onPick = { i -> band = AgeBand.entries[i]; go(3) }
          )
          3 -> ChoicePage(
            title = "¿Cuál es tu nivel de estudios?",
            hint = "Nos ayuda a compararte con personas parecidas a ti. No cambia la dificultad: eso lo decide cómo juegas.",
            options = Education.entries.map { it.label to null },
            selected = education?.ordinal,
            tagPrefix = "education_",
            tags = Education.entries.map { it.name.lowercase() },
            onPick = { i -> education = Education.entries[i]; go(4) }
          )
          4 -> GoalsPage(
            selected = goals,
            onToggle = { d -> goals = if (d in goals) goals - d else if (goals.size < 3) goals + d else goals },
            onNext = { go(5) }
          )
          5 -> ChoicePage(
            title = "¿Cuántos días por semana quieres entrenar?",
            hint = "Unos 5 minutos por día. Mejor poco y seguido que mucho de una vez.",
            options = listOf("3 días" to "Suave", "4 días" to "Recomendado", "5 días" to "Intenso", "Todos los días" to "Sin pausa"),
            selected = listOf(3, 4, 5, 7).indexOf(goal).takeIf { it >= 0 },
            tagPrefix = "weekly_goal_",
            tags = listOf("3", "4", "5", "7"),
            onPick = { i -> goal = listOf(3, 4, 5, 7)[i]; go(6) }
          )
          6 -> ChoicePage(
            title = "¿Te recordamos cada día?",
            hint = "Un aviso corto a la hora que elijas. Si ya completaste tu camino de hoy, no llega.",
            options = listOf("Por la mañana" to "9:00", "Al mediodía" to "13:00", "Por la tarde" to "19:00", "Por la noche" to "21:00", "Sin recordatorios" to null),
            selected = null,
            tagPrefix = "reminder_",
            tags = listOf("9", "13", "19", "21", "off"),
            onPick = { i -> pickReminder(listOf(9, 13, 19, 21, null)[i]) }
          )
          7 -> HowItWorksPage(onNext = { go(8) })
          else -> StartingPointPage(
            onStart = { onFinish(name, band ?: AgeBand.ADULT, goal, reminderHour.takeIf { it >= 0 }, education, goals, true) },
            onLater = { onFinish(name, band ?: AgeBand.ADULT, goal, reminderHour.takeIf { it >= 0 }, education, goals, false) }
          )
        }
      }
    }
  }
}

@Composable
private fun WelcomePage(onNext: () -> Unit) {
  Column(modifier = Modifier.fillMaxSize(), horizontalAlignment = Alignment.CenterHorizontally) {
    Spacer(Modifier.weight(0.4f))
    GamesOrbit()
    Spacer(Modifier.height(18.dp))
    Text("Nubi", color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 44.sp)
    Text(
      text = "${GameRegistry.allGames.size} juegos cortos para entrenar memoria, atención, razonamiento, lenguaje, cálculo y velocidad.",
      color = OnNightDim, fontFamily = AppFamily, fontSize = 18.sp, lineHeight = 25.sp, textAlign = TextAlign.Center,
      modifier = Modifier.padding(top = 8.dp)
    )
    Spacer(Modifier.weight(0.6f))
    ClayButton(text = "Empezar", onClick = onNext, modifier = Modifier.testTag("btn_onboarding_start"))
    Spacer(Modifier.height(20.dp))
  }
}

/** Los juegos como planetas de arcilla girando despacio alrededor de un sol. */
@Composable
private fun GamesOrbit() {
  val context = LocalContext.current
  val reduceMotion = remember {
    Settings.Global.getFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f) == 0f
  }
  val spin = if (reduceMotion) 0f else {
    val t = rememberInfiniteTransition(label = "orbit")
    val v by t.animateFloat(0f, 360f, infiniteRepeatable(tween(60000, easing = LinearEasing)), label = "orbitSpin")
    v
  }
  val glow = if (reduceMotion) 0.5f else {
    val t = rememberInfiniteTransition(label = "sunGlow")
    val v by t.animateFloat(0f, 1f, infiniteRepeatable(tween(2200), RepeatMode.Reverse), label = "sunGlowV")
    v
  }
  val games = GameRegistry.allGames
  Box(modifier = Modifier.size(300.dp), contentAlignment = Alignment.Center) {
    Canvas(Modifier.fillMaxSize()) {
      val c = center
      val r = size.minDimension * 0.4f
      drawCircle(Color.White.copy(alpha = 0.10f), r, c, style = Stroke(2.dp.toPx()))
      val sunR = size.minDimension * (0.2f + 0.02f * glow)
      drawCircle(Brush.radialGradient(listOf(Clay.Sun.copy(alpha = 0.55f), Color.Transparent), c, sunR * 1.8f), sunR * 1.8f, c)
      // Sol: destello de 4 puntas
      val s = size.minDimension * 0.11f
      val p = Path().apply {
        moveTo(c.x, c.y - s)
        quadraticBezierTo(c.x, c.y, c.x + s, c.y)
        quadraticBezierTo(c.x, c.y, c.x, c.y + s)
        quadraticBezierTo(c.x, c.y, c.x - s, c.y)
        quadraticBezierTo(c.x, c.y, c.x, c.y - s)
        close()
      }
      drawPath(p, Clay.Sun)
    }
    val radiusDp = 120f
    games.forEachIndexed { i, g ->
      val a = (spin + i * 360f / games.size) * PI.toFloat() / 180f
      Box(
        modifier = Modifier
          .offset { IntOffset((cos(a) * radiusDp.dp.toPx()).roundToInt(), (sin(a) * radiusDp.dp.toPx()).roundToInt()) }
          .size(52.dp)
          .clip(CircleShape)
          .background(g.domain.color)
          .border(2.5.dp, Clay.Ink, CircleShape),
        contentAlignment = Alignment.Center
      ) { GameIcon(g.id, size = 34.dp) }
    }
  }
}

@Composable
private fun NamePage(name: String, onName: (String) -> Unit, onNext: () -> Unit) {
  Column(modifier = Modifier.fillMaxSize(), horizontalAlignment = Alignment.CenterHorizontally) {
    Spacer(Modifier.weight(0.35f))
    Text("¿Cómo te llamas?", color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 30.sp, textAlign = TextAlign.Center)
    Text(
      "Para saludarte en tu camino. Es opcional.",
      color = OnNightDim, fontFamily = AppFamily, fontSize = 16.sp, textAlign = TextAlign.Center,
      modifier = Modifier.padding(top = 6.dp, bottom = 22.dp)
    )
    BasicTextField(
      value = name,
      onValueChange = onName,
      singleLine = true,
      textStyle = TextStyle(fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = 24.sp, color = Clay.Ink, textAlign = TextAlign.Center),
      cursorBrush = SolidColor(Clay.Ink),
      keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Words, imeAction = ImeAction.Next),
      keyboardActions = KeyboardActions(onNext = { onNext() }),
      modifier = Modifier.testTag("input_onboarding_name"),
      decorationBox = { inner ->
        Box(
          modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(22.dp))
            .background(Clay.Cream)
            .border(Clay.Border, Clay.Ink, RoundedCornerShape(22.dp))
            .padding(horizontal = 18.dp, vertical = 16.dp),
          contentAlignment = Alignment.Center
        ) {
          if (name.isEmpty()) Text("Tu nombre", color = Clay.InkSoft, fontFamily = AppFamily, fontSize = 24.sp)
          inner()
        }
      }
    )
    Spacer(Modifier.weight(0.65f))
    ClayButton(text = if (name.isBlank()) "Saltar" else "Siguiente", onClick = onNext, modifier = Modifier.testTag("btn_onboarding_name_next"))
    Spacer(Modifier.height(20.dp))
  }
}

/** Una pregunta con opciones de arcilla grandes; tocar una elige y avanza. */
@Composable
private fun ChoicePage(
  title: String,
  hint: String,
  options: List<Pair<String, String?>>,
  selected: Int?,
  tagPrefix: String,
  tags: List<String>,
  onPick: (Int) -> Unit
) {
  Column(modifier = Modifier.fillMaxSize(), horizontalAlignment = Alignment.CenterHorizontally) {
    Spacer(Modifier.weight(0.3f))
    Text(title, color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 28.sp, lineHeight = 32.sp, textAlign = TextAlign.Center)
    Text(
      hint, color = OnNightDim, fontFamily = AppFamily, fontSize = 16.sp, lineHeight = 22.sp, textAlign = TextAlign.Center,
      modifier = Modifier.padding(top = 8.dp, bottom = 26.dp)
    )
    // Con pantallas chicas o letra grande las opciones se desplazan en vez de cortarse.
    Column(Modifier.weight(1f, fill = false).verticalScroll(rememberScrollState())) {
      options.forEachIndexed { i, (label, sub) ->
        val isSel = selected == i
        ClayButton(
          text = if (sub != null) "$label · $sub" else label,
          color = if (isSel) Clay.Sun else Clay.Cream,
          onClick = { onPick(i) },
          modifier = Modifier.testTag("$tagPrefix${tags[i]}")
        )
        Spacer(Modifier.height(14.dp))
      }
    }
    Spacer(Modifier.weight(0.7f))
  }
}

@Composable
private fun HowItWorksPage(onNext: () -> Unit) {
  Column(modifier = Modifier.fillMaxSize(), horizontalAlignment = Alignment.CenterHorizontally) {
    Spacer(Modifier.weight(0.45f))
    Text("Así funciona", color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 32.sp)
    Spacer(Modifier.height(26.dp))
    HowRow(
      art = { PathNodesArt() },
      title = "Tu camino de cada día",
      text = "3 juegos distintos, unos 5 minutos. La dificultad se ajusta a ti: ni muy fácil, ni imposible."
    )
    HowRow(
      art = { LeagueShield(tier = RankTier.BRONCE, size = 54.dp, pips = 1, glow = true) },
      title = "Sube de liga",
      text = "Cada partida suma trofeos. De Bronce a Maestro, en cada juego y en general."
    )
    HowRow(
      art = { Achievements.byId("racha_3")?.let { AchievementMedal(def = it, unlocked = true, size = 60.dp) } },
      title = "Racha y logros",
      text = "Vuelve cada día para mantener tu racha y desbloquear medallas."
    )
    Spacer(Modifier.weight(0.55f))
    ClayButton(text = "Siguiente", onClick = onNext, modifier = Modifier.testTag("btn_onboarding_how_next"))
    Spacer(Modifier.height(20.dp))
  }
}

/** Metas: hasta 3 dominios que quiere entrenar (el camino diario los prioriza). Se puede seguir sin elegir. */
@Composable
private fun GoalsPage(selected: Set<DomainType>, onToggle: (DomainType) -> Unit, onNext: () -> Unit) {
  Column(modifier = Modifier.fillMaxSize(), horizontalAlignment = Alignment.CenterHorizontally) {
    Spacer(Modifier.weight(0.25f))
    Text("¿Qué quieres entrenar?", color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 28.sp, textAlign = TextAlign.Center)
    Text(
      "Elige hasta 3. Tu camino de cada día les dará prioridad.",
      color = OnNightDim, fontFamily = AppFamily, fontSize = 16.sp, lineHeight = 22.sp, textAlign = TextAlign.Center,
      modifier = Modifier.padding(top = 8.dp, bottom = 22.dp)
    )
    Column(Modifier.weight(1f, fill = false).verticalScroll(rememberScrollState())) {
      DomainType.values().forEach { d ->
        val on = d in selected
        Row(
          modifier = Modifier
            .fillMaxWidth()
            .padding(bottom = 10.dp)
            .clip(RoundedCornerShape(20.dp))
            .background(if (on) Clay.Cream else Color.White.copy(alpha = 0.06f))
            .border(if (on) Clay.Border else 1.5.dp, if (on) Clay.Ink else Color.White.copy(alpha = 0.18f), RoundedCornerShape(20.dp))
            .clickable { onToggle(d) }
            .padding(horizontal = 16.dp, vertical = 12.dp)
            .testTag("goal_${d.name.lowercase()}"),
          verticalAlignment = Alignment.CenterVertically
        ) {
          Box(Modifier.size(18.dp).clip(CircleShape).background(d.color).border(2.dp, Clay.Ink, CircleShape))
          Spacer(Modifier.width(12.dp))
          Column(Modifier.weight(1f)) {
            Text(d.displayName, color = if (on) Clay.Ink else Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 18.sp)
            Text(d.description, color = if (on) Clay.InkSoft else OnNightDim, fontFamily = AppFamily, fontSize = 15.sp)
          }
          // Marca de elegido (forma, no solo color).
          Box(
            Modifier.size(26.dp).clip(CircleShape)
              .background(if (on) Clay.Lime else Color.Transparent)
              .border(2.dp, if (on) Clay.Ink else Color.White.copy(alpha = 0.35f), CircleShape),
            contentAlignment = Alignment.Center
          ) { if (on) Text("✓", color = Clay.Ink, fontWeight = FontWeight.Bold, fontSize = 15.sp) }
        }
      }
    }
    Spacer(Modifier.weight(0.75f))
    ClayButton(text = if (selected.isEmpty()) "Saltar" else "Siguiente", onClick = onNext, modifier = Modifier.testTag("btn_onboarding_goals_next"))
    Spacer(Modifier.height(20.dp))
  }
}

/**
 * Invitación a la evaluación inicial: los 3 juegos como planetas (qué mide cada uno), cuánto dura y que no es un
 * examen. "Empezar" la juega ahora; "Hacerlo después" estima el punto de partida y la deja en Perfil.
 */
@Composable
private fun StartingPointPage(onStart: () -> Unit, onLater: () -> Unit) {
  Column(modifier = Modifier.fillMaxSize(), horizontalAlignment = Alignment.CenterHorizontally) {
    Spacer(Modifier.weight(0.35f))
    Text(
      "Encontremos tu punto de partida",
      color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 30.sp, lineHeight = 34.sp,
      textAlign = TextAlign.Center
    )
    Text(
      "3 juegos cortos, unos 5 minutos. No es un examen: con esto cada juego empieza a tu medida.",
      color = OnNightDim, fontFamily = AppFamily, fontSize = 16.sp, lineHeight = 22.sp, textAlign = TextAlign.Center,
      modifier = Modifier.padding(top = 10.dp, bottom = 30.dp)
    )
    Row(horizontalArrangement = Arrangement.SpaceEvenly, modifier = Modifier.fillMaxWidth()) {
      BaselinePlan.steps.forEachIndexed { i, step ->
        val game = GameRegistry.getById(step.gameId)
        Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.width(96.dp)) {
          Box(
            modifier = Modifier
              .size(76.dp)
              .clip(CircleShape)
              .background(step.domain.color)
              .border(3.dp, Clay.Ink, CircleShape),
            contentAlignment = Alignment.Center
          ) { GameIcon(step.gameId, size = 50.dp) }
          Text(
            "${i + 1}. ${step.domain.displayName}",
            color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 15.sp,
            modifier = Modifier.padding(top = 8.dp)
          )
          Text(
            game?.title ?: "", color = OnNightDim, fontFamily = AppFamily, fontSize = 14.sp, textAlign = TextAlign.Center
          )
        }
      }
    }
    Spacer(Modifier.weight(0.65f))
    ClayButton(text = "Empezar", onClick = onStart, modifier = Modifier.testTag("btn_onboarding_baseline"))
    Text(
      "Hacerlo después",
      color = OnNight, fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = 16.sp,
      modifier = Modifier
        .padding(top = 8.dp)
        .clip(RoundedCornerShape(12.dp))
        .clickable(onClick = onLater)
        .padding(horizontal = 16.dp, vertical = 14.dp) // área de toque >= 48 dp
        .testTag("btn_onboarding_explore")
    )
    Spacer(Modifier.height(8.dp))
  }
}

@Composable
private fun HowRow(art: @Composable () -> Unit, title: String, text: String) {
  Row(modifier = Modifier.fillMaxWidth().padding(bottom = 22.dp), verticalAlignment = Alignment.CenterVertically) {
    Box(modifier = Modifier.width(72.dp), contentAlignment = Alignment.Center) { art() }
    Spacer(Modifier.width(14.dp))
    Column(Modifier.weight(1f)) {
      Text(title, color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 19.sp)
      Text(text, color = OnNightDim, fontFamily = AppFamily, fontSize = 15.sp, lineHeight = 20.sp)
    }
  }
}

/** Tres nodos del camino unidos por puntos (como en Hoy): hecho, hoy, lo que viene. */
@Composable
private fun PathNodesArt() {
  Canvas(Modifier.size(64.dp)) {
    val w = size.width
    val pts = listOf(Offset(w * 0.22f, w * 0.18f), Offset(w * 0.72f, w * 0.48f), Offset(w * 0.3f, w * 0.84f))
    for (i in 0 until pts.size - 1) {
      val a = pts[i]
      val b = pts[i + 1]
      for (k in 1..4) {
        val t = k / 5f
        drawCircle(Color.White.copy(alpha = 0.45f), 2.dp.toPx(), Offset(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t))
      }
    }
    val colors = listOf(Clay.Lime, Clay.Sun, Color.White.copy(alpha = 0.3f))
    val radii = listOf(9f, 12f, 7f)
    pts.forEachIndexed { i, p ->
      val r = radii[i].dp.toPx()
      drawCircle(colors[i], r, p)
      if (i < 2) drawCircle(Clay.Ink, r, p, style = Stroke(2.5.dp.toPx()))
    }
  }
}
