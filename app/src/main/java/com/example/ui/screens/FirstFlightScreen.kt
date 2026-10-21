package com.example.ui.screens

import android.Manifest
import android.os.Build
import androidx.activity.compose.BackHandler
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.Spring
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
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
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.scale
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.disabled
import androidx.compose.ui.semantics.role
import androidx.compose.ui.semantics.selected
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextDecoration
import androidx.compose.ui.text.withStyle
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.BaselinePlan
import com.example.data.FirstFlight
import com.example.data.FlightCopy
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.model.AgeBand
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.components.GameIcon
import com.example.ui.components.NubiClay
import com.example.ui.components.rememberReduceMotion
import com.example.ui.theme.AppFamily
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = com.example.ui.theme.NubiTokens.OnNightDim
private val RowFill = Color(0xFF0E0C30)
private val RowSelectedFill = Color(0xFF231C48)
private val TrackColor = Color(0xFF221D55)

/** Lo que la pantalla le pide al ViewModel (la lógica vive en `NeuroVidaViewModel` y `data/FirstFlight.kt`). */
class FlightActions(
  val onContinue: () -> Unit,
  val onBack: () -> Unit,
  val onName: (String) -> Unit,
  val onAge: (AgeBand) -> Unit,
  val onGoal: (DomainType) -> Unit,
  val onDays: (Int) -> Unit,
  val onHour: (Int) -> Unit,
  val onPlay: (Int) -> Unit,
  val onSkipGames: () -> Unit,
  val onConfirmReminder: (Int) -> Unit
) {
  companion object {
    /** Acciones vacías (capturas y pruebas de la pantalla). */
    val None = FlightActions({}, {}, {}, {}, {}, {}, {}, {}, {}, {})
  }
}

/**
 * «Primer vuelo con Nubi» (`docs/diseno-inicio.md`, maqueta aprobada `docs/previews/inicio-maqueta.html`): el inicio nuevo y, en su forma corta
 * ([FlightMode.GAMES]), «Hacer la evaluación». Una pregunta por pantalla, sobre el cielo de la app (el fondo lo pinta quien la aloja), sin
 * recuadros para informar: las opciones son filas tocables. Texto de 18 sp o más, toques de 48 dp o más, y «quitar animaciones» se respeta
 * (la barra y los cambios de pantalla pasan a un fundido). Los juegos corren en Unity: acá solo se muestra su paso, y el resultado vuelve
 * por el ViewModel.
 *
 * [todayGames] = los 3 juegos del camino de hoy (para el último paso).
 */
@Composable
fun FirstFlightScreen(
  state: FlightState,
  todayGames: List<String>,
  actions: FlightActions,
  modifier: Modifier = Modifier
) {
  val canGoBack = FirstFlight.back(state) != null
  BackHandler(enabled = canGoBack) { actions.onBack() }
  val reduceMotion = rememberReduceMotion()
  val stations = FirstFlight.stations(state)
  val stepText = FirstFlight.stationText(state)

  Column(
    modifier = modifier
      .fillMaxSize()
      .safeDrawingPadding()
      .imePadding()
      .padding(horizontal = 24.dp)
  ) {
    // Arriba: volver + las farolas de etapa (Tarea 132, A4: en lugar de la barra y de «Faltan unos 5 minutos»), y debajo en qué paso va.
    Row(Modifier.fillMaxWidth().heightIn(min = 52.dp), verticalAlignment = Alignment.CenterVertically) {
      if (canGoBack) {
        com.example.ui.components.KitBackButton(actions.onBack, Modifier.testTag("btn_flight_back"), label = "Atrás")
      } else {
        Spacer(Modifier.size(48.dp))
      }
      Spacer(Modifier.width(8.dp))
      FlightStations(stations, stepText, reduceMotion, Modifier.weight(1f))
    }
    Text(
      stepText,
      color = OnNightDim, fontFamily = AppFamily, fontSize = 18.sp, textAlign = TextAlign.Center,
      modifier = Modifier.fillMaxWidth().padding(top = 4.dp).heightIn(min = 26.dp).testTag("flight_step_text")
    )

    AnimatedContent(
      targetState = state.step,
      transitionSpec = {
        val dir = if (targetState.ordinal >= initialState.ordinal) 1 else -1
        // «Quitar animaciones»: sin deslizamiento, solo un fundido corto.
        if (reduceMotion) fadeIn(tween(com.example.ui.components.NubiMotion.REDUCED_FADE_MS)) togetherWith fadeOut(tween(com.example.ui.components.NubiMotion.REDUCED_FADE_MS))
        else (slideInHorizontally(tween(com.example.ui.components.NubiMotion.TAB_MS, easing = com.example.ui.components.NubiMotion.Emphasized)) { it / 4 * dir } + fadeIn(tween(com.example.ui.components.NubiMotion.TAB_MS, easing = com.example.ui.components.NubiMotion.Emphasized))) togetherWith
          (slideOutHorizontally(tween(com.example.ui.components.NubiMotion.STATE_MS, easing = com.example.ui.components.NubiMotion.Standard)) { -it / 4 * dir } + fadeOut(tween(com.example.ui.components.NubiMotion.STATE_MS, easing = com.example.ui.components.NubiMotion.Standard)))
      },
      modifier = Modifier.weight(1f).fillMaxWidth(),
      label = "flightStep"
    ) { step ->
      FlightPage(step, state, todayGames, actions)
    }
  }
}

/**
 * Las farolas de etapa (Tarea 132, A4): seis de 40 dp (la farola del kit, la misma de Hoy, Avance y Ajustes; 28 dp de lámpara en una caja de 36 con el aro lima de «hoy» en la etapa actual), o cinco en la evaluación repetida. En pantallas angostas las cajas
 * se achican parejo (nunca pasan de 40 dp). Una farola se enciende con un fundido de [NubiMotion.STATE_MS] (un salto con «quitar animaciones»); sin sonido ni vibración. Para el lector de pantalla es UN grupo: «Paso 2 de 6 · Memoria. 2 de 6 farolas encendidas».
 */
@Composable
private fun FlightStations(stations: List<FirstFlight.Station>, text: String, reduceMotion: Boolean, modifier: Modifier) {
  androidx.compose.foundation.layout.BoxWithConstraints(
    modifier.semantics(mergeDescendants = true) { contentDescription = "$text. ${stations.count { it.lit }} de ${stations.size} farolas encendidas" }.testTag("flight_lamps")
  ) {
    val cell = minOf(40.dp, maxWidth / stations.size)
    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.Center, verticalAlignment = Alignment.CenterVertically) {
      stations.forEachIndexed { i, st -> FlightLamp(st.lit, st.current, cell, reduceMotion, Modifier.testTag("flight_lamp_$i")) }
    }
  }
}

@Composable
private fun FlightLamp(lit: Boolean, current: Boolean, cell: androidx.compose.ui.unit.Dp, reduceMotion: Boolean, modifier: Modifier) {
  val glow by animateFloatAsState(if (lit) 1f else 0f, com.example.ui.components.NubiMotion.spec(reduceMotion, com.example.ui.components.NubiMotion.STATE_MS), label = "farola_etapa")
  val box = cell - 4.dp
  Box(
    modifier.size(cell).padding(2.dp).drawBehind { if (current) drawCircle(Clay.Lime, size.minDimension / 2f - 1.5.dp.toPx(), center, style = Stroke(2.5.dp.toPx())) },
    contentAlignment = Alignment.Center
  ) {
    com.example.ui.components.FarolaImage(on = false, height = box * 0.78f)                                  // la apagada, siempre debajo; la encendida se funde encima
    com.example.ui.components.FarolaImage(on = true, height = box * 0.78f, modifier = Modifier.alpha(glow))
  }
}

@Composable
private fun FlightPage(step: FlightStep, s: FlightState, todayGames: List<String>, a: FlightActions) {
  val hi = if (s.name.isNotBlank()) ", ${s.name}" else ""
  when (step) {
    FlightStep.HOLA -> HolaPage(a)
    FlightStep.NOMBRE -> NamePage(s.name, a)
    FlightStep.EDAD -> QuestionPage(
      eyebrow = "Para ajustar el ritmo",
      title = "¿En qué rango de edad estás?",
      options = AgeBand.entries.map { Opt(it.name, it.label) },
      selected = setOf(s.age?.name),
      onPick = { v -> AgeBand.entries.firstOrNull { it.name == v }?.let(a.onAge) },
      hint = "Cambia el ritmo y el tamaño de los juegos, no lo difícil: eso lo decide cómo juegas.",
      canContinue = s.age != null,
      onContinue = a.onContinue
    )
    FlightStep.METAS -> QuestionPage(
      eyebrow = "Para armar tu ruta",
      title = "¿Qué te gustaría jugar más$hi?",
      options = listOf(
        Opt(DomainType.MEMORIA.name, "Memoria", DomainType.MEMORIA.tagline),                    // la misma descripción de cada área que en Juegos (Tarea 150, B1.9)
        Opt(DomainType.ATENCION.name, "Atención", DomainType.ATENCION.tagline),
        Opt(DomainType.RAZONAMIENTO.name, "Razonamiento", DomainType.RAZONAMIENTO.tagline),
        Opt(DomainType.LENGUAJE.name, "Lenguaje", DomainType.LENGUAJE.tagline)
      ),
      selected = s.goals.map { it.name }.toSet(),
      multi = true,
      onPick = { v -> DomainType.entries.firstOrNull { it.name == v }?.let(a.onGoal) },
      hint = "Elige hasta 3. Tu ruta de cada día les dará prioridad.",
      canContinue = s.goals.isNotEmpty(),
      onContinue = a.onContinue
    )
    FlightStep.DIAS -> QuestionPage(
      eyebrow = "Para tu ritmo",
      title = "¿Cuántos días por semana quieres jugar?",
      options = listOf(Opt("3", "3 días", "suave"), Opt("4", "4 días", "recomendado"), Opt("5", "5 días", "intenso"), Opt("7", "Todos los días", "sin pausa")),
      selected = setOf(s.days?.toString()),
      onPick = { v -> v.toIntOrNull()?.let(a.onDays) },
      hint = "Son " + com.example.data.DailyPath.minutesText(com.example.data.DailyPath.SIZE) + " cada vez.",
      canContinue = s.days != null,
      onContinue = a.onContinue
    )
    FlightStep.DATO -> DatoPage(a)
    FlightStep.JUEGO_1, FlightStep.JUEGO_2, FlightStep.JUEGO_3, FlightStep.JUEGO_4 -> GameIntroPage(step.game ?: 0, s, a)
    FlightStep.TARJETA_1, FlightStep.TARJETA_2, FlightStep.TARJETA_3, FlightStep.TARJETA_4 -> GameCardPage(step.game ?: 0, s, a)
    FlightStep.PUNTO -> StartingPointPage(s, hi, a)
    FlightStep.AVISO -> ReminderPage(s, a)
    FlightStep.CAMINO -> TodayPathPage(todayGames, a)
  }
}

// ------------------------------------------------------------------ piezas comunes

private data class Opt(val value: String, val label: String, val sub: String? = null)

/**
 * El marco de una pantalla: el contenido arriba (se desplaza si no cabe: letra grande o pantalla chica) y los botones fijos abajo, siempre
 * a mano. [centered] lo centra (bienvenida y datos de Nubi).
 */
@Composable
private fun PageFrame(
  centered: Boolean = false,
  footer: @Composable () -> Unit,
  fill: Boolean = false,
  body: @Composable () -> Unit
) {
  Column(Modifier.fillMaxSize()) {
    Box(Modifier.weight(1f).fillMaxWidth(), contentAlignment = if (centered) Alignment.Center else Alignment.TopCenter) {
      // [fill] = el cuerpo se ocupa de su propio alto (y de deslizarse): el cierre, donde la isla toma lo que sobra
      if (fill) Box(Modifier.fillMaxSize().padding(top = 10.dp)) { body() }
      else Column(
        Modifier.fillMaxWidth().verticalScroll(rememberScrollState()).padding(top = 10.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
        horizontalAlignment = if (centered) Alignment.CenterHorizontally else Alignment.Start
      ) { body() }
    }
    Column(Modifier.fillMaxWidth().padding(top = 10.dp, bottom = 12.dp), horizontalAlignment = Alignment.CenterHorizontally) { footer() }
  }
}

@Composable
private fun Eyebrow(text: String, center: Boolean = false) =
  com.example.ui.components.OnBoardText(
    text, color = Clay.Sun, fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = 18.sp,
    textAlign = if (center) TextAlign.Center else TextAlign.Start, modifier = if (center) Modifier.fillMaxWidth() else Modifier
  )

@Composable
private fun Title(text: String, center: Boolean = false, size: Int = 28) =
  com.example.ui.components.OnBoardText(
    text, color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = size.sp, lineHeight = (size + 5).sp,
    textAlign = if (center) TextAlign.Center else TextAlign.Start, modifier = if (center) Modifier.fillMaxWidth() else Modifier
  )

@Composable
private fun Say(text: String, highlight: String? = null, center: Boolean = false) {
  val annotated = buildAnnotatedString {
    val at = highlight?.let { text.indexOf(it) } ?: -1
    if (highlight == null || at < 0) append(text)
    else {
      append(text.substring(0, at))
      withStyle(SpanStyle(color = Clay.Sun)) { append(highlight) }
      append(text.substring(at + highlight.length))
    }
  }
  com.example.ui.components.OnBoardText(
    annotated, color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = 20.sp, lineHeight = 28.sp,
    textAlign = if (center) TextAlign.Center else TextAlign.Start, modifier = if (center) Modifier.fillMaxWidth() else Modifier
  )
}

@Composable
private fun Sub(text: String, center: Boolean = false) =
  com.example.ui.components.OnBoardText(
    text, color = OnNightDim, fontFamily = AppFamily, fontSize = 18.sp, lineHeight = 25.sp,
    textAlign = if (center) TextAlign.Center else TextAlign.Start, modifier = if (center) Modifier.fillMaxWidth() else Modifier
  )

/** Una opción: casilla de plasticina tocable (no un recuadro que informa). La elegida va en lima, con su ✓ de churro tinta y su texto en negrita (no solo color), y queda quieta. */
@Composable
private fun OptionRow(opt: Opt, selected: Boolean, multi: Boolean, onClick: () -> Unit) {
  com.example.ui.components.ClayChoice(
    selected = selected, onClick = onClick, minHeight = 58.dp,
    role = if (multi) Role.Checkbox else Role.RadioButton,
    modifier = Modifier.fillMaxWidth().padding(bottom = 2.dp).testTag("flight_opt_${opt.value}")
  ) {
    Column(Modifier.weight(1f).padding(vertical = 4.dp)) {
      Text(opt.label, color = Clay.Ink, fontFamily = AppFamily, fontWeight = if (selected) FontWeight.Bold else FontWeight.SemiBold, fontSize = 18.sp, lineHeight = 23.sp)
      if (opt.sub != null) Text(opt.sub, color = Clay.InkMuted, fontFamily = AppFamily, fontSize = 18.sp, lineHeight = 23.sp)
    }
  }
}

/** Marca de hecho dibujada (no un carácter de la fuente). */
@Composable
private fun CheckMark(modifier: Modifier, color: Color) {
  Canvas(modifier) {
    val w = size.width
    val p = Path().apply {
      moveTo(w * 0.12f, w * 0.52f)
      lineTo(w * 0.42f, w * 0.8f)
      lineTo(w * 0.9f, w * 0.22f)
    }
    drawPath(p, color, style = Stroke(w * 0.2f, cap = StrokeCap.Round, join = StrokeJoin.Round))
  }
}

/** El botón principal. Sin respuesta todavía queda atenuado y no hace nada (y lo dice para TalkBack). */
@Composable
private fun MainButton(text: String, enabled: Boolean = true, tag: String = "btn_flight_next", onClick: () -> Unit) {
  if (enabled) {
    ClayButton(text = text, onClick = onClick, fontSize = 19.sp, modifier = Modifier.testTag(tag))
  } else {
    Box(Modifier.alpha(0.45f).semantics { disabled() }.testTag(tag)) { ClayButton(text = text, onClick = {}, fontSize = 19.sp) }
  }
}

/** La acción secundaria («Terminar después»): botón de plasticina crema, debajo del principal. */
@Composable
private fun LinkButton(text: String, tag: String, onClick: () -> Unit) {
  ClayButton(text = text, onClick = onClick, color = Clay.Cream, fontSize = 18.sp, modifier = Modifier.padding(top = 4.dp).testTag(tag))
}

// ------------------------------------------------------------------ las pantallas

@Composable
private fun HolaPage(a: FlightActions) {
  PageFrame(
    centered = true,
    footer = { MainButton("¡Vamos!", onClick = a.onContinue) }
  ) {
    NubiClay(com.example.ui.components.NubiSize.L)
    Title("Hola, soy Nubi", center = true, size = 34)
    Say("Vamos a encontrar tu punto de partida jugando. Son 4 juegos cortos.", highlight = "jugando", center = true)
  }
}

@Composable
private fun NamePage(name: String, a: FlightActions) {
  var text by rememberSaveable { mutableStateOf(name) }
  val go = { a.onName(text); a.onContinue() }
  PageFrame(footer = { MainButton("Continuar", onClick = go) }) {
    Eyebrow("Para saludarte")
    Title("¿Cómo te llamo?")
    // El campo es una losa crema HUNDIDA (la misma del resto de la app): texto tinta de 24 sp, cursor tinta y aro cielo con el foco.
    com.example.ui.components.ClayTextField(
      value = text,
      onValueChange = { text = it.take(24) },
      placeholder = "Tu nombre",
      fontSize = 24.sp,
      keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Words, imeAction = ImeAction.Done),
      keyboardActions = KeyboardActions(onDone = { go() }),
      minHeight = 64.dp,
      modifier = Modifier.testTag("input_flight_name")
    )
    Sub("Es opcional.")
  }
}

/** Una pregunta por pantalla: su porqué arriba, las opciones como filas tocables y «Continuar» (atenuado hasta responder). */
@Composable
private fun QuestionPage(
  eyebrow: String,
  title: String,
  options: List<Opt>,
  selected: Set<String?>,
  onPick: (String) -> Unit,
  hint: String,
  canContinue: Boolean,
  onContinue: () -> Unit,
  multi: Boolean = false
) {
  PageFrame(footer = { MainButton("Continuar", enabled = canContinue, onClick = onContinue) }) {
    Eyebrow(eyebrow)
    Title(title)
    Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
      options.forEach { o -> OptionRow(o, selected = o.value in selected, multi = multi, onClick = { onPick(o.value) }) }
    }
    Sub(hint)
  }
}

@Composable
private fun DatoPage(a: FlightActions) {
  PageFrame(centered = true, footer = { MainButton("Continuar", onClick = a.onContinue) }) {
    NubiClay(com.example.ui.components.NubiSize.M)
    Eyebrow("¿Sabías que…?", center = true)
    Say("Practicar poco y seguido rinde más que mucho de una vez: es de lo más comprobado sobre cómo aprendemos.", highlight = "poco y seguido", center = true)
  }
}

/** Antes de cada juego: cuál toca y una línea de cómo se juega (el tutorial vive dentro del juego). «Terminar después» deja el resto para estimar. */
@Composable
private fun GameIntroPage(index: Int, s: FlightState, a: FlightActions) {
  val step = BaselinePlan.steps[index]
  val game = GameRegistry.getById(step.gameId)
  PageFrame(
    footer = {
      MainButton("Jugar", tag = "btn_flight_play") { a.onPlay(index) }
      LinkButton("Terminar después", "btn_flight_skip", a.onSkipGames)
    }
  ) {
    Eyebrow(step.domain.displayName, center = true)
    Title(game?.title ?: step.gameId, center = true)
    Box(Modifier.fillMaxWidth(), contentAlignment = Alignment.Center) { NubiClay(com.example.ui.components.NubiSize.M) }
    Say(FlightCopy.of(step.gameId).how, center = true)
    Sub("Empieza con una ronda de práctica que no cuenta. Dura alrededor de un minuto.", center = true)
  }
}

/** Después de cada juego: «Acabas de usar tu {capacidad}», el primer dato en palabras y para qué sirve. */
@Composable
private fun GameCardPage(index: Int, s: FlightState, a: FlightActions) {
  val step = BaselinePlan.steps[index]
  val game = GameRegistry.getById(step.gameId)
  val copy = FlightCopy.of(step.gameId)
  PageFrame(footer = { MainButton("Continuar", onClick = a.onContinue) }) {
    Box(Modifier.fillMaxWidth(), contentAlignment = Alignment.Center) { NubiClay(com.example.ui.components.NubiSize.M) }
    Title("Acabas de usar tu ${copy.short}", center = true, size = 26)
    Say(s.phrases[step.gameId] ?: FlightCopy.NO_DATA_LINE, center = true)
    Sub(copy.why, center = true)
  }
}

/**
 * «Tu punto de partida»: las 4 áreas con su etapa en palabras, «tu fuerte hoy» y «donde más vamos a jugar» (en TEXTO, no solo color), y,
 * la primera vez, el primer lucero de Nubi y el día 1 de la racha. «No es un examen».
 */
@Composable
private fun StartingPointPage(s: FlightState, hi: String, a: FlightActions) {
  val rows = FirstFlight.startingPoint(s, s.age)
  val first = s.mode == FlightMode.FULL
  val played = s.playedCount > 0
  PageFrame(footer = { MainButton(if (first) "Continuar" else "Listo", tag = "btn_flight_point", onClick = a.onContinue) }) {
    if (first && played) {
      FirstStar()
    }
    Eyebrow(if (first) "Listo$hi" else "Tu punto de partida de hoy")
    Title("Tu punto de partida")
    Column(verticalArrangement = Arrangement.spacedBy(12.dp)) { rows.forEach { AreaLine(it) } }
    Sub("No es un examen: es desde dónde partimos. Se afina en cada partida.")
    if (first) {
      if (played) {
        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(12.dp)) {
          Box(
            Modifier.size(44.dp).clip(CircleShape).background(Color(0xFF3A1E10)).border(3.dp, Color(0xFFFF9A3C), CircleShape),
            contentAlignment = Alignment.Center
          ) { Text("1", color = Color(0xFFFFB45C), fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 18.sp) }
          Text("¡Empezó tu racha! Vuelve mañana para el día 2.", color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = 18.sp, lineHeight = 24.sp, modifier = Modifier.weight(1f))
        }
      } else {
        Sub("Cada partida hace nacer un lucero en el cielo de Nubi. Tu primera te espera en Hoy.")
      }
    }
  }
}

@Composable
private fun AreaLine(r: FirstFlight.AreaRow) {
  val label = buildString {
    append(r.stage)
    if (!r.measured) append(" · calculado con lo que jugaste")
    if (r.strong) append(" · tu fuerte hoy")
    if (r.focus) append(" · donde más vamos a jugar")
  }
  Column(Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = "${r.domain.displayName}: $label" }, verticalArrangement = Arrangement.spacedBy(4.dp)) {
    Text(r.domain.displayName, color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = 18.sp)
    // La barra del kit (canal y churro), la misma de Juegos y Avance (Tarea 143, ítem 13); un área estimada (sin medir) se ve más tenue
    com.example.ui.components.KitBar(r.progress.coerceIn(0.04f, 1f), change = 0f, modifier = Modifier.alpha(if (r.measured) 1f else 0.55f), onCard = false)
    Text(label, color = OnNightDim, fontFamily = AppFamily, fontSize = 18.sp, lineHeight = 23.sp)
  }
}

/**
 * «Nace tu primer lucero»: Nubi celebra y el primer lucero aparece en su cielo (un rebote corto). Con «quitar animaciones» solo
 * se funde y queda quieta.
 */
@Composable
private fun FirstStar() {
  val still = rememberReduceMotion()
  val grow = remember { Animatable(if (still) 1f else 0f) }
  val fade = remember { Animatable(if (still) 1f else 0f) }
  LaunchedEffect(Unit) {
    if (!still) {
      fade.animateTo(1f, tween(300))
      grow.animateTo(1f, spring(dampingRatio = Spring.DampingRatioMediumBouncy, stiffness = Spring.StiffnessLow))
    }
  }
  Column(Modifier.fillMaxWidth().testTag("flight_first_star"), horizontalAlignment = Alignment.CenterHorizontally) {
    Box(contentAlignment = Alignment.Center) {
      NubiClay(com.example.ui.components.NubiSize.M, celebrate = true)
      Canvas(
        Modifier
          .align(Alignment.TopEnd)
          .offset(x = 34.dp, y = (-4).dp)
          .size(64.dp)
          .scale(grow.value.coerceAtLeast(0.01f))
          .alpha(fade.value)
          .semantics { contentDescription = "Tu primer lucero" }
      ) {
        val c = center
        val r = size.minDimension * 0.34f
        drawCircle(Clay.Sun.copy(alpha = 0.28f), r * 1.35f, c)
        drawCircle(Clay.Ink, r + 5f, c)
        drawCircle(Clay.Sun, r, c)
        drawCircle(Color.White.copy(alpha = 0.55f), r * 0.22f, Offset(c.x - r * 0.35f, c.y - r * 0.38f))
      }
    }
    Text("¡Nace tu primer lucero!", color = Clay.Sun, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 20.sp, textAlign = TextAlign.Center)
    Text("Cada partida hace nacer un lucero en el cielo de Nubi.", color = OnNightDim, fontFamily = AppFamily, fontSize = 18.sp, lineHeight = 24.sp, textAlign = TextAlign.Center)
  }
}

/** ¿A qué hora te aviso? Aquí se pide el permiso de notificaciones de Android 13+ («a la hora que elegiste»). */
@Composable
private fun ReminderPage(s: FlightState, a: FlightActions) {
  var asked by rememberSaveable { mutableIntStateOf(19) }
  val permission = rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
    a.onConfirmReminder(if (granted) asked else FirstFlight.NO_REMINDER)
  }
  val onContinue = {
    val hour = s.hour
    if (hour != null) {
      if (hour == FirstFlight.NO_REMINDER) a.onConfirmReminder(hour)
      else if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) { asked = hour; permission.launch(Manifest.permission.POST_NOTIFICATIONS) }
      else a.onConfirmReminder(hour)
    }
  }
  QuestionPage(
    eyebrow = "Para no olvidarlo",
    title = "¿A qué hora te aviso?",
    options = listOf(
      Opt("9", "Por la mañana", "9:00"), Opt("13", "Al mediodía", "13:00"), Opt("19", "Por la tarde", "19:00"),
      Opt("21", "Por la noche", "21:00"), Opt(FirstFlight.NO_REMINDER.toString(), "Sin aviso")
    ),
    selected = setOf(s.hour?.toString()),
    onPick = { v -> v.toIntOrNull()?.let(a.onHour) },
    hint = "Un aviso corto. Si ya jugaste ese día, no llega.",
    canContinue = s.hour != null,
    onContinue = onContinue
  )
}

/**
 * El último paso: los 3 juegos de hoy, elegidos por las metas y el punto de partida. «Empezar mi ruta» entra a Hoy.
 * Tarea 145 (ítem 23 de la revisión del 19-oct): antes Nubi y dos líneas con la mitad de la pantalla vacía; ahora la ISLA, la misma que se ve mañana en Hoy (el mismo render, con Nubi encima), a un tamaño que entra sin desplazar, y las dos líneas de siempre (sin texto nuevo que prometa algo).
 * La isla se queda con lo que sobra de alto después de las dos líneas (se miden con la letra del sistema); con letra muy grande, donde ni así entra, la página se desliza como las demás.
 */
@Composable
private fun TodayPathPage(todayGames: List<String>, a: FlightActions) {
  // El día del Primer vuelo, la ruta de hoy ya quedó cumplida con el vuelo (repository.completeTodayWithFlight): no se juegan 7 juegos. Mañana empieza la ruta de 3 juegos.
  val title = "Tu primer vuelo fue tu ruta de hoy"
  val sub = "Mañana empieza tu ruta de 3 juegos, elegida por tus metas y por lo que más conviene reforzar."
  PageFrame(footer = { MainButton("Empezar mi ruta", tag = "btn_flight_finish", onClick = a.onContinue) }, fill = true) {
    androidx.compose.foundation.layout.BoxWithConstraints(Modifier.fillMaxSize()) {
      val measurer = androidx.compose.ui.text.rememberTextMeasurer()
      val density = androidx.compose.ui.platform.LocalDensity.current
      val textHeight = remember(maxWidth, density.fontScale) {
        with(density) {
          val width = maxWidth.roundToPx()
          val t = measurer.measure(title, TextStyle(fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 28.sp, lineHeight = 33.sp, textAlign = TextAlign.Center), constraints = androidx.compose.ui.unit.Constraints(maxWidth = width))
          val s = measurer.measure(sub, TextStyle(fontFamily = AppFamily, fontSize = 18.sp, lineHeight = 25.sp, textAlign = TextAlign.Center), constraints = androidx.compose.ui.unit.Constraints(maxWidth = width))
          (t.size.height + s.size.height).toDp() + 12.dp * 2
        }
      }
      val islandHeight = (maxHeight - textHeight).coerceAtLeast(MIN_ISLAND_HEIGHT)
      Column(Modifier.fillMaxWidth().verticalScroll(rememberScrollState()), verticalArrangement = Arrangement.spacedBy(12.dp), horizontalAlignment = Alignment.CenterHorizontally) {
        com.example.ui.components.Island(
          description = "Tu isla, la que verás mañana en Hoy",
          modifier = Modifier.fillMaxWidth().height(islandHeight).testTag("flight_island")
        )
        Title(title, center = true)
        Sub(sub, center = true)
      }
    }
  }
}

/** Lo menos que mide la isla del cierre (dp): con letra muy grande la página se desliza antes que dejarla más chica. */
private val MIN_ISLAND_HEIGHT = 170.dp
