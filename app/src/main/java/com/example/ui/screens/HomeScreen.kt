package com.example.ui.screens

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectHorizontalDragGestures
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.wrapContentHeight
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Whatshot
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.layout.layout
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.data.AreaProgress
import com.example.data.AreaStatus
import com.example.data.MeasurePoint
import com.example.data.Skill
import com.example.data.StarMeasures
import com.example.data.IslaLugar
import com.example.data.Week
import com.example.model.DomainType
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.AreaBar
import com.example.ui.components.AreaBarColor
import com.example.ui.components.CosmosScroll
import com.example.ui.components.LeagueShield
import com.example.ui.components.LockTabSwipe
import com.example.ui.components.MiniPlanet
import com.example.ui.components.NubiBubble
import com.example.ui.components.Island
import com.example.ui.components.TopActions
import com.example.ui.components.NubiPose
import com.example.ui.components.NubiWithHalo
import com.example.ui.components.areaDescription
import com.example.ui.components.domainOf
import com.example.ui.components.overallIndex
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayCard
import com.example.viewmodel.NeuroVidaViewModel
import com.example.viewmodel.TopPanel
import java.util.TimeZone

private const val DAY_MS = 86_400_000L
private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = Color(0xFFC7D0FF)
private val OnNightSoft = Color(0xFF9AA3D6)

private fun dayIndex(ts: Long): Long = (ts + TimeZone.getDefault().getOffset(ts)) / DAY_MS

/**
 * Inicio ("Hoy") con LA ISLA (Tarea 82, fase 1 de `docs/diseno-isla.md` §4.1; aprobado por Ricardo el 11-oct): saludo con la fecha, la semana en siete farolitos, la isla de plasticina con Nubi (≈ 40 % del alto), el globo de Nubi,
 * las tres paradas del camino de hoy y UN solo botón grande. Regla que manda: en 393 × 873 dp con letra normal las tres paradas y el botón se ven SIN deslizar; la isla toma lo que sobra (con letra grande o pantalla chica la isla
 * se achica hasta un mínimo y lo demás se desliza).
 *
 * Qué pasó con lo que Hoy tenía antes: las cuatro barras de área (con su etapa y el cambio de la semana) ya no están sueltas; cada una vive en el DETALLE de su área ([AreaDetail]: etapa, cuánto avanzó, las últimas semanas, sus juegos y «¿Le damos un empujón?»), que se abre TOCANDO
 * el edificio de esa área en la isla (Observatorio = Memoria, Antena = Atención, Hangar = Razonamiento, Biblioteca = Lenguaje; hasta la fase 2, que lo cambia a «Juegos con ese lugar elegido»). La liga y la racha pasaron al lado de los farolitos; el perfil y las opciones siguen arriba a la derecha.
 */
@Composable
fun HomeScreen(
  viewModel: NeuroVidaViewModel,
  onNavigateToGames: () -> Unit,
  modifier: Modifier = Modifier
) {
  val streak by viewModel.currentStreak.collectAsState()
  val dailySession by viewModel.dailySession.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val ranks by viewModel.gameRanks.collectAsState()
  val progress by viewModel.gameProgress.collectAsState()
  val progressLog by viewModel.progressLog.collectAsState()
  val pausedGameId by viewModel.pausedGameId.collectAsState()
  val baseline by viewModel.baseline.collectAsState()
  val measures by viewModel.starMeasures.collectAsState()
  val settings by viewModel.userSettings.collectAsState()
  // Reloj de la semana de cada área: cada 30 s.
  var clock by remember { mutableStateOf(System.currentTimeMillis()) }
  LaunchedEffect(Unit) {
    CosmosScroll.offset = 0f
    while (true) {
      kotlinx.coroutines.delay(30_000L)
      clock = System.currentTimeMillis()
    }
  }
  val lang = LocalAppLanguage.current

  var openArea by rememberSaveable { mutableStateOf<String?>(null) }

  val avg = if (ranks.isEmpty()) 0 else ranks.sumOf { it.rating } / ranks.size
  val tier = RankTier.fromRating(avg)

  val names = remember { DomainType.values().associate { it.name to it.displayName } }
  val statuses = remember(progress, progressLog, clock) {
    AREA_ORDER.map { key ->
      AreaProgress.status(key, GameRegistry.allGames.filter { it.domain.name == key }.map { it.id }, progress, progressLog, clock)
    }
  }

  openArea?.let { key ->
    LockTabSwipe()
    BackHandler { openArea = null }
    AreaDetail(
      areaKey = key,
      statuses = statuses,
      names = names,
      history = history,
      measures = measures,
      now = clock,
      lang = lang,
      onArea = { openArea = it },
      onPlay = { id ->
        openArea = null
        viewModel.launchGame(id)
      },
      onClose = { openArea = null },
      modifier = modifier
    )
    return
  }

  val completed = dailySession.completedCount
  val ui = HomeUi(
    name = settings.name,
    now = clock,
    tier = tier,
    streak = streak,
    weeklyGoal = settings.weeklyGoal,
    lamps = weekLamps(history.map { it.timestamp }, clock),
    stops = homeStops(dailySession.gameIds, completed, lang),
    pausedTitle = pausedGameId?.let { GameRegistry.getById(it) }?.let { getGameTitle(it.id, lang, it.title) },
    flightDay = dailySession.gameIds.isNotEmpty() && completed >= dailySession.gameIds.size && dailySession.scores.isEmpty(),
    hasBaseline = baseline != null
  )
  HomeContent(
    ui = ui,
    onProfile = { viewModel.openTopPanel(TopPanel.PERFIL) },
    onSettings = { viewModel.openTopPanel(TopPanel.AJUSTES) },
    onPlace = { openArea = it },
    onPlay = { viewModel.startDailySession() },
    onResume = { viewModel.resumePausedGame() },
    onBaseline = { viewModel.startBaseline() },
    modifier = modifier
  )
}

// ------------------------------------------------------------------ Hoy con la isla

private val HoyCream = Color(0xFFF6EBD3)
private val HoySoft = Color(0xFFD6D1F0)
private val HoyBubbleFill = Color(0xFFFCFAFF)
private val HoyBubbleLine = Color(0xFF4A3896)

/** Lo que Hoy muestra, ya calculado (así `HomeContent` no depende del ViewModel y se prueba y se captura con datos fijos). */
internal data class HomeUi(
  val name: String,
  val now: Long,
  val tier: RankTier,
  val streak: Int,
  val weeklyGoal: Int,
  val lamps: Week.Lamps,
  val stops: List<HomeStop>,
  /** Título del juego en pausa, si hay uno (el botón único pasa a «Seguir tu partida»). */
  val pausedTitle: String?,
  /** El camino de hoy quedó cumplido sin jugarlo: fue el Primer vuelo. */
  val flightDay: Boolean,
  val hasBaseline: Boolean
) {
  val completed: Int get() = stops.count { it.state == StopState.DONE }
}

internal enum class StopState { DONE, NEXT, LATER }

/** Una parada del camino de hoy: el juego, su lugar en la isla (el de su área) y si ya se hizo, es la que sigue o viene después. */
internal data class HomeStop(val gameId: String, val title: String, val area: String, val place: String, val state: StopState)

/** Las tres paradas de hoy en orden: las [completed] primeras están hechas, la siguiente es «la que sigue». */
internal fun homeStops(gameIds: List<String>, completed: Int, lang: com.example.model.AppLanguage): List<HomeStop> =
  gameIds.take(3).mapIndexedNotNull { i, id ->
    GameRegistry.getById(id)?.let { def ->
      HomeStop(
        gameId = id, title = getGameTitle(id, lang, def.title), area = def.domain.name, place = IslaLugar.nameOfArea(def.domain.name).orEmpty(),
        state = when { i < completed -> StopState.DONE; i == completed -> StopState.NEXT; else -> StopState.LATER }
      )
    }
  }

/** Lunes a domingo de la semana de [now]: los días con al menos una partida se encienden. */
internal fun weekLamps(timestamps: List<Long>, now: Long): Week.Lamps {
  val cal = java.util.Calendar.getInstance().apply { timeInMillis = now }
  val mondayOffset = (cal.get(java.util.Calendar.DAY_OF_WEEK) + 5) % 7   // lunes = 0
  return Week.lamps(timestamps, now, ::dayIndex, mondayOffset)
}

/** «Buenos días», «Buenas tardes» o «Buenas noches» según la hora (sin culpa: no habla de lo que falta). */
internal fun greetingWord(now: Long): String {
  val h = java.util.Calendar.getInstance().apply { timeInMillis = now }.get(java.util.Calendar.HOUR_OF_DAY)
  return when { h < 12 -> "Buenos días"; h < 20 -> "Buenas tardes"; else -> "Buenas noches" }
}

/** «Domingo 11 de octubre». */
internal fun dateText(now: Long): String =
  java.text.SimpleDateFormat("EEEE d 'de' MMMM", java.util.Locale("es")).format(java.util.Date(now)).replaceFirstChar { it.uppercase() }

/** Lo que dice Nubi en el globo: la ruta de hoy, o que hay una partida en pausa, o que el camino ya está cumplido. */
internal fun nubiLine(ui: HomeUi): String {
  val pending = ui.stops.count { it.state != StopState.DONE }
  return when {
    ui.pausedTitle != null -> "Tienes una partida en pausa: ${ui.pausedTitle}."
    ui.flightDay -> "Tu Primer vuelo fue tu camino de hoy. Mañana empieza tu camino."
    ui.stops.isEmpty() -> "Hoy Nubi te espera en la isla."
    pending == 0 -> "Ya pasaste por la isla hoy. Mañana sigo aquí."
    else -> {
      val places = ui.stops.filter { it.state != StopState.DONE }.mapNotNull { IslaLugar.withArticle(it.area) }
      val list = if (places.size > 1) places.dropLast(1).joinToString(", ") + " y " + places.last() else places.joinToString()
      "Hoy la ruta pasa por $list. Unos ${pending * 2} minutos."
    }
  }
}

/** El nombre que dice el lector de pantalla para la isla: «Tu isla: hoy la ruta pasa por el Observatorio, la Antena y la Biblioteca». */
internal fun islandDescription(ui: HomeUi): String {
  val places = ui.stops.mapNotNull { IslaLugar.withArticle(it.area) }
  return if (places.isEmpty()) "Tu isla: el Observatorio, la Antena, la Biblioteca y el Hangar"
  else "Tu isla: hoy la ruta pasa por " + (if (places.size > 1) places.dropLast(1).joinToString(", ") + " y " + places.last() else places.joinToString())
}

/**
 * El contenido de Hoy sin ViewModel. El `Column` mide al menos lo que mide la pantalla y la isla ocupa lo que SOBRA entre los demás elementos (`weight`), con un mínimo de [ISLAND_MIN_HEIGHT]: si lo demás no cabe (letra grande, pantalla chica),
 * la isla se queda en su mínimo y toda la pantalla se desliza.
 */
@OptIn(ExperimentalLayoutApi::class)
@Composable
internal fun HomeContent(
  ui: HomeUi,
  onProfile: () -> Unit,
  onSettings: () -> Unit,
  onPlace: (area: String) -> Unit,
  onPlay: () -> Unit,
  onResume: () -> Unit,
  onBaseline: () -> Unit,
  modifier: Modifier = Modifier
) {
  BoxWithConstraints(modifier.fillMaxSize()) {
    Column(Modifier.fillMaxWidth().heightIn(min = maxHeight).verticalScroll(rememberScrollState())) {
      // Saludo con la fecha y tu liga; arriba a la derecha tu perfil y las opciones (como en las otras pestañas). Todo el texto va a la IZQUIERDA y desde 24 dp bajo la barra: es la zona del fondo sin luceros (`BoardBackgroundContrastTest`).
      Row(Modifier.fillMaxWidth().padding(start = 20.dp, end = 16.dp, top = 24.dp, bottom = 2.dp), verticalAlignment = Alignment.CenterVertically) {
        Column(Modifier.weight(1f)) {
          Text(
            if (ui.name.isBlank()) greetingWord(ui.now) else "${greetingWord(ui.now)}, ${ui.name.trim()}",
            color = HoyCream, fontSize = 22.sp, fontWeight = FontWeight.Bold, lineHeight = 27.sp, modifier = Modifier.testTag("home_greeting")
          )
          FlowRow(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            Text(dateText(ui.now), color = HoySoft, fontSize = 14.sp, modifier = Modifier.testTag("home_date").then(RowItem))
            Row(RowItem, verticalAlignment = Alignment.CenterVertically) {
              LeagueShield(tier = ui.tier, size = 20.dp)
              Spacer(Modifier.width(5.dp))
              Text(ui.tier.tierName, color = HoyCream, fontSize = 14.sp, fontWeight = FontWeight.SemiBold)
            }
          }
        }
        Spacer(Modifier.width(4.dp))
        TopActions(name = ui.name, onProfile = onProfile, onSettings = onSettings)
      }

      HoyWeek(ui)

      Island(
        description = islandDescription(ui),
        doneAreas = ui.stops.filter { it.state == StopState.DONE }.map { it.area }.toSet(),
        onPlace = onPlace,
        modifier = Modifier.weight(1f).fillMaxWidth().atLeastHeight(ISLAND_MIN_HEIGHT).testTag("home_isla")
      )

      HoyBubble(nubiLine(ui), tailAt = 0.62f, modifier = Modifier.padding(horizontal = 16.dp).testTag(if (ui.flightDay) "home_flight_day" else "nubi_line"))

      if (ui.stops.isNotEmpty()) {
        Column(Modifier.fillMaxWidth().padding(start = 16.dp, end = 16.dp, top = 8.dp), verticalArrangement = Arrangement.spacedBy(2.dp)) {
          ui.stops.forEachIndexed { i, s -> HoyStopRow(s, i + 1) }
        }
      }

      HoyAction(ui, onPlay = onPlay, onResume = onResume, onBaseline = onBaseline)
    }
  }
}

/** Alto mínimo de la isla en Hoy: debajo de esto se ve diminuta, y la pantalla se desliza antes de achicarla más. */
internal val ISLAND_MIN_HEIGHT = 150.dp

/**
 * Como `heightIn(min = …)` pero que SÍ puede pasar de lo que le deja un `weight`: un hijo con `weight` recibe un alto exacto y `heightIn` no puede agrandarlo,
 * así que sin esto la isla se achicaba hasta desaparecer cuando lo demás ocupaba la pantalla (letra grande, pantallas chicas).
 */
private fun Modifier.atLeastHeight(min: Dp): Modifier = layout { measurable, constraints ->
  val available = if (constraints.hasBoundedHeight) constraints.maxHeight else 0
  val h = maxOf(available, min.roundToPx())
  val placeable = measurable.measure(Constraints(constraints.minWidth, constraints.maxWidth, h, h))
  layout(placeable.width, placeable.height) { placeable.place(0, 0) }
}

/** Cada elemento de una fila que puede saltar de línea mide lo mismo (22 dp como mínimo) y se centra en ella: `FlowRow` de esta versión no alinea sus elementos. */
private val RowItem = Modifier.heightIn(min = 22.dp).wrapContentHeight(Alignment.CenterVertically)

/** Los siete farolitos de la semana (los días jugados, encendidos; hoy con un aro lima), con la meta en palabras y tu racha a su lado (a la izquierda, donde el fondo no tiene luceros). */
@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun HoyWeek(ui: HomeUi) {
  Column(Modifier.fillMaxWidth().padding(start = 20.dp, end = 16.dp, top = 2.dp, bottom = 2.dp)) {
    val spoken = Week.spoken(ui.lamps.daysPlayed, ui.weeklyGoal)
    Row(Modifier.testTag("home_week").semantics { contentDescription = spoken }, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
      ui.lamps.played.forEachIndexed { i, on -> WeekLamp(on = on, today = i == ui.lamps.todayIndex) }
    }
    FlowRow(Modifier.padding(top = 3.dp), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
      // El lector de pantalla oye la semana UNA vez (en los farolitos); este texto es solo para la vista.
      Box(RowItem.clearAndSetSemantics { }, contentAlignment = Alignment.CenterStart) {
        Text(Week.caption(ui.lamps.daysPlayed, ui.weeklyGoal), color = HoySoft, fontSize = 14.sp)
      }
      Row(RowItem, verticalAlignment = Alignment.CenterVertically) {
        Icon(Icons.Default.Whatshot, contentDescription = null, tint = Clay.Sun, modifier = Modifier.size(18.dp))
        Text(
          if (ui.streak == 1) "1 día" else "${ui.streak} días", color = Clay.Sun, fontSize = 14.sp, fontWeight = FontWeight.Bold,
          modifier = Modifier.testTag("streak_pill")
        )
      }
    }
  }
}

/** Un farolito: encendido = disco sol con resplandor; apagado = anillo tenue (la forma cambia, no solo el color); hoy lleva además un aro lima. */
@Composable
private fun WeekLamp(on: Boolean, today: Boolean) {
  Canvas(Modifier.size(26.dp)) {
    val c = center
    val r = size.minDimension * 0.34f
    if (on) {
      drawCircle(Brush.radialGradient(listOf(Clay.Sun.copy(alpha = 0.55f), Color.Transparent), c, size.minDimension / 2f), size.minDimension / 2f, c)
      drawCircle(Clay.Sun, r, c)
      drawCircle(Color.White.copy(alpha = 0.7f), r * 0.32f, Offset(c.x - r * 0.3f, c.y - r * 0.3f))
    } else {
      drawCircle(HoyCream.copy(alpha = 0.16f), r, c)
      drawCircle(HoyCream.copy(alpha = 0.5f), r, c, style = Stroke(1.5.dp.toPx()))
    }
    if (today) drawCircle(Clay.Lime, size.minDimension / 2f - 1.5.dp.toPx(), c, style = Stroke(2.5.dp.toPx()))
  }
}

/** El globo de Nubi: un recuadro de diálogo con la cola hacia arriba (Nubi está en la isla, encima), en [tailAt] del ancho. */
@Composable
private fun HoyBubble(text: String, tailAt: Float, modifier: Modifier = Modifier) {
  Box(
    modifier
      .fillMaxWidth()
      .drawBehind {
        val tail = 10.dp.toPx()
        val line = 2.dp.toPx()
        val top = tail
        val h = size.height - tail - 3.dp.toPx()
        val r = CornerRadius(18.dp.toPx())
        drawRoundRect(HoyBubbleLine, topLeft = Offset(0f, top + 3.dp.toPx()), size = Size(size.width, h), cornerRadius = r)
        drawRoundRect(HoyBubbleFill, topLeft = Offset(0f, top), size = Size(size.width, h), cornerRadius = r)
        drawRoundRect(HoyBubbleLine, topLeft = Offset(0f, top), size = Size(size.width, h), cornerRadius = r, style = Stroke(line))
        val cx = size.width * tailAt
        val t = 9.dp.toPx()
        val tailPath = Path().apply { moveTo(cx - t, top + line); lineTo(cx, top - tail + line); lineTo(cx + t, top + line); close() }
        drawPath(tailPath, HoyBubbleFill)
        drawLine(HoyBubbleLine, Offset(cx - t, top + line / 2f), Offset(cx, top - tail + line), line)
        drawLine(HoyBubbleLine, Offset(cx + t, top + line / 2f), Offset(cx, top - tail + line), line)
      }
      .padding(start = 16.dp, end = 16.dp, top = 10.dp + 10.dp, bottom = 10.dp + 3.dp)
  ) {
    Text(text, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 20.sp)
  }
}

/** Una parada del camino: su número (o el check si ya está hecha), el juego, su lugar en la isla y si está hecha, es la que sigue o viene después. La que sigue va en sol; las hechas, en lima con check. */
@Composable
private fun HoyStopRow(stop: HomeStop, number: Int) {
  val color = when (stop.state) { StopState.DONE -> Clay.Lime; StopState.NEXT -> Clay.Sun; StopState.LATER -> HoyCream }
  val status = when (stop.state) { StopState.DONE -> "Hecho"; StopState.NEXT -> "Es la que sigue"; StopState.LATER -> "Después" }
  ClayCard(
    modifier = Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = "${stop.title}, ${stop.place}, ${status.replaceFirstChar { it.lowercase() }}" }.testTag("home_stop_$number"),
    color = color, radius = 16.dp, depth = 4.dp, contentPadding = 0.dp
  ) {
    Row(Modifier.fillMaxWidth().heightIn(min = 44.dp).padding(horizontal = 10.dp, vertical = 5.dp), verticalAlignment = Alignment.CenterVertically) {
      Box(
        Modifier.size(26.dp).clip(RoundedCornerShape(50)).background(Color.White).border(2.dp, Clay.Ink, RoundedCornerShape(50)),
        contentAlignment = Alignment.Center
      ) {
        if (stop.state == StopState.DONE) Icon(Icons.Default.Check, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(16.dp))
        else Text("$number", color = Clay.Ink, fontSize = 14.sp, fontWeight = FontWeight.Bold)
      }
      Spacer(Modifier.width(10.dp))
      Column(Modifier.weight(1f)) {
        Text(stop.title, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 19.sp)
        Text(stop.place, color = Clay.Ink, fontSize = 14.sp, lineHeight = 17.sp)
      }
      Spacer(Modifier.width(8.dp))
      Text(status, color = Clay.Ink, fontSize = 14.sp, fontWeight = FontWeight.Bold, lineHeight = 17.sp, textAlign = TextAlign.End, modifier = Modifier.widthIn(max = 112.dp))
    }
  }
}

/** UN solo botón grande: seguir la partida en pausa, empezar o seguir la ruta de hoy, o «Ruta completa» cuando ya no queda nada; debajo, si todavía no hizo la evaluación, la invitación a hacerla. */
@Composable
private fun HoyAction(ui: HomeUi, onPlay: () -> Unit, onResume: () -> Unit, onBaseline: () -> Unit) {
  val pending = ui.stops.count { it.state != StopState.DONE }
  Column(Modifier.fillMaxWidth().padding(start = 16.dp, end = 16.dp, top = 6.dp, bottom = 6.dp)) {
    when {
      ui.pausedTitle != null ->
        ClayButton(text = "Seguir tu partida", onClick = onResume, icon = Icons.Default.PlayArrow, fontSize = 18.sp, modifier = Modifier.testTag("btn_resume_paused"))
      pending > 0 ->
        ClayButton(
          text = if (ui.completed == 0) "Empezar la ruta · ${ui.stops.size} juegos" else "Seguir la ruta · $pending ${if (pending == 1) "juego" else "juegos"}",
          onClick = onPlay, icon = Icons.Default.PlayArrow, fontSize = 18.sp, modifier = Modifier.testTag("btn_home_play")
        )
      ui.stops.isNotEmpty() ->
        ClayCard(modifier = Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = "Ruta completa" }.testTag("home_route_done"), color = Clay.Lime, radius = 20.dp, depth = 5.dp, contentPadding = 0.dp) {
          Row(Modifier.fillMaxWidth().height(52.dp), horizontalArrangement = Arrangement.Center, verticalAlignment = Alignment.CenterVertically) {
            Icon(Icons.Default.Check, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(22.dp))
            Spacer(Modifier.width(8.dp))
            Text("Ruta completa", color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold)
          }
        }
    }
    if (!ui.hasBaseline) {
      Text(
        text = "¿Aún sin tu punto de partida? Encuéntralo en 5 minutos",
        color = Clay.Sun,
        fontSize = 15.sp,
        fontWeight = FontWeight.SemiBold,
        modifier = Modifier
          .padding(top = 4.dp)
          .clip(RoundedCornerShape(10.dp))
          .clickable(onClick = onBaseline)
          .padding(horizontal = 4.dp, vertical = 6.dp)
          .testTag("btn_home_baseline")
      )
    }
  }
}

/** Lo que dice Nubi arriba del detalle de un área, en palabras. */
private fun detailLine(name: String, s: AreaStatus): String = when {
  s.value == null -> "$name aún sin medir: juega y lo verás"
  s.change > 0f -> "Esta semana $name avanzó"
  s.change < 0f -> "$name bajó un poco: se recupera jugando"
  else -> "$name se mantiene esta semana"
}

/**
 * Detalle de un área (vista "C" aprobada por Ricardo): Nubi explica, la barra grande con las 5 etapas nombradas, de
 * dónde a dónde avanzó en la semana y cuánto falta para la siguiente, las últimas 4 semanas y sus juegos. Abajo, en
 * vez de "Jugar Memoria", la pregunta de Nubi con el estilo del botón y "¡Sí, vamos!" (Nubi elige el juego del área
 * sin jugar o el más olvidado y dice por qué). Flechas (o deslizar sobre Nubi) para pasar a otra área.
 */
@Composable
internal fun AreaDetail(
  areaKey: String,
  statuses: List<AreaStatus>,
  names: Map<String, String>,
  history: List<GamePlayResult>,
  measures: List<MeasurePoint>,
  now: Long,
  lang: com.example.model.AppLanguage,
  onArea: (String) -> Unit,
  onPlay: (String) -> Unit,
  onClose: () -> Unit,
  modifier: Modifier = Modifier
) {
  val idx = AREA_ORDER.indexOf(areaKey).coerceAtLeast(0)
  val s = statuses.getOrNull(idx) ?: AreaStatus(areaKey, null, 0f, emptyList())
  val name = names[areaKey] ?: areaKey
  val domain = domainOf(areaKey) ?: return
  val games = GameRegistry.allGames.filter { it.domain == domain }
  val lastPlayed = games.associate { g -> g.id to history.filter { it.gameId == g.id }.maxOfOrNull { it.timestamp } }
  val suggested = games.minByOrNull { lastPlayed[it.id] ?: Long.MIN_VALUE }
  fun go(step: Int) = onArea(AREA_ORDER[(idx + step + AREA_ORDER.size) % AREA_ORDER.size])

  Column(modifier.fillMaxSize().testTag("area_detail")) {
    Row(
      verticalAlignment = Alignment.CenterVertically,
      modifier = Modifier
        .padding(start = 8.dp, top = 4.dp)
        .clip(RoundedCornerShape(12.dp))
        .clickable(onClick = onClose)
        .padding(horizontal = 8.dp, vertical = 8.dp)
        .testTag("area_back")
    ) {
      Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = null, tint = OnNight, modifier = Modifier.size(22.dp))
      Spacer(Modifier.width(8.dp))
      Text("Hoy", color = OnNight, fontSize = 17.sp, fontWeight = FontWeight.SemiBold)
    }
    Column(
      Modifier.weight(1f).verticalScroll(rememberScrollState()).padding(horizontal = 20.dp),
      horizontalAlignment = Alignment.CenterHorizontally
    ) {
      NubiBubble("Nubi", detailLine(name, s))
      var drag by remember(areaKey) { mutableStateOf(0f) }
      Row(
        verticalAlignment = Alignment.CenterVertically,
        modifier = Modifier.fillMaxWidth().pointerInput(areaKey) {
          detectHorizontalDragGestures(
            onDragEnd = { if (drag > 60.dp.toPx()) go(-1) else if (drag < -60.dp.toPx()) go(1); drag = 0f },
            onDragCancel = { drag = 0f }
          ) { _, dx -> drag += dx }
        }
      ) {
        AreaArrow(left = true, onClick = { go(-1) })
        Spacer(Modifier.weight(1f))
        NubiWithHalo(size = 118.dp, pose = NubiPose.MIRA)
        Spacer(Modifier.weight(1f))
        AreaArrow(left = false, onClick = { go(1) })
      }
      Text(name, color = Color.White, fontSize = 28.sp, fontWeight = FontWeight.Bold)
      Text(
        s.value?.let { "Etapa ${Skill.stageName(it)} · ${s.points} de 100" } ?: "Aún sin medir",
        color = OnNightDim, fontSize = 16.sp
      )
      Spacer(Modifier.height(12.dp))
      AreaBar(s.value, s.change, height = 14.dp, modifier = Modifier.semantics { contentDescription = areaDescription(name, s) })
      Row(Modifier.fillMaxWidth().padding(top = 6.dp)) {
        Skill.STAGES.forEachIndexed { i, st ->
          val here = s.value != null && Skill.stage(s.value) == i
          Text(
            st, fontSize = 13.sp, textAlign = TextAlign.Center, maxLines = 1,
            color = if (here) Color.White else OnNightSoft, fontWeight = if (here) FontWeight.Bold else FontWeight.Normal,
            modifier = Modifier.weight(1f)
          )
        }
      }
      Spacer(Modifier.height(14.dp))
      Text(AreaProgress.changeLine(s), color = Color.White, fontSize = 17.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center)
      AreaProgress.toNextLine(s)?.let { Text(it, color = OnNightDim, fontSize = 15.sp) }
      if (s.value != null && s.weeks.count { it != null } >= 2) WeeksChart(s.weeks)
      Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.padding(vertical = 12.dp)) {
        AREA_ORDER.forEachIndexed { i, _ ->
          Box(
            Modifier.size(if (i == idx) 10.dp else 8.dp).clip(RoundedCornerShape(50))
              .background(if (i == idx) Color.White else Color.White.copy(alpha = 0.25f))
          )
        }
      }
      Text(
        "Tus juegos de ${name.lowercase()}", color = Clay.Sun, fontSize = 14.sp, fontWeight = FontWeight.Bold,
        modifier = Modifier.fillMaxWidth().padding(top = 4.dp, bottom = 2.dp)
      )
      games.forEach { g ->
        val m = StarMeasures.latest(measures, g.id)
        val last = lastPlayed[g.id]
        val detail = when {
          m != null -> StarMeasures.zoneText(m.first, measures)?.let { "$it · promedio reciente" } ?: "${m.first.format(m.second)} ${m.first.unit} · tu última"
          last == null -> "Sin jugar aún"
          else -> playedAgo(now, last)
        }
        Row(
          verticalAlignment = Alignment.CenterVertically,
          modifier = Modifier.fillMaxWidth().clip(RoundedCornerShape(12.dp)).clickable { onPlay(g.id) }.padding(vertical = 6.dp)
        ) {
          MiniPlanet(g.id, 34.dp)
          Spacer(Modifier.width(12.dp))
          Column {
            Text(getGameTitle(g.id, lang, g.title), color = Color.White, fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
            Text(detail, color = OnNightDim, fontSize = 14.sp)
          }
        }
      }
      Spacer(Modifier.height(8.dp))
    }
    if (suggested != null) {
      // La invitación: la pregunta con el mismo estilo que el botón, para que se lean juntas.
      Column(Modifier.fillMaxWidth().padding(start = 20.dp, end = 20.dp, top = 8.dp, bottom = 10.dp), horizontalAlignment = Alignment.CenterHorizontally) {
        Text(
          "¿Le damos un empujón a ${AreaProgress.your(name)}?", color = Clay.Sun, fontSize = 20.sp,
          fontWeight = FontWeight.Bold, textAlign = TextAlign.Center, modifier = Modifier.padding(bottom = 8.dp)
        )
        ClayButton(text = "¡Sí, vamos!", onClick = { onPlay(suggested.id) }, modifier = Modifier.testTag("area_play"))
        val title = getGameTitle(suggested.id, lang, suggested.title)
        val last = lastPlayed[suggested.id]
        Text(
          when {
            last == null -> "Nubi eligió $title: aún no la juegas"
            else -> "Nubi eligió $title: ${playedAgo(now, last).replaceFirstChar { it.lowercase() }}"
          },
          color = OnNightDim, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 8.dp)
        )
      }
    }
  }
}

private fun playedAgo(now: Long, last: Long): String = when (val days = (dayIndex(now) - dayIndex(last)).toInt()) {
  0 -> "Jugado hoy"
  1 -> "Jugado ayer"
  else -> "Hace $days días que no lo juegas"
}

@Composable
private fun AreaArrow(left: Boolean, onClick: () -> Unit) {
  Box(
    Modifier.size(48.dp).clip(RoundedCornerShape(50)).background(Color.White.copy(alpha = 0.12f)).clickable(onClick = onClick),
    contentAlignment = Alignment.Center
  ) {
    Icon(
      if (left) Icons.AutoMirrored.Filled.KeyboardArrowLeft else Icons.AutoMirrored.Filled.KeyboardArrowRight,
      contentDescription = if (left) "Área anterior" else "Área siguiente", tint = Color.White, modifier = Modifier.size(28.dp)
    )
  }
}

/** Las últimas 4 semanas en la misma escala 0-100: una línea lavanda, la de esta semana en sol, con su número. */
@Composable
private fun WeeksChart(weeks: List<Float?>) {
  val vals = weeks.map { it?.let { v -> (v * 100f) } }
  val known = vals.filterNotNull()
  val lo = (known.minOrNull() ?: 0f) - 4f
  val hi = (known.maxOrNull() ?: 0f) + 4f
  Column(Modifier.fillMaxWidth().padding(top = 16.dp)) {
    Row(Modifier.fillMaxWidth()) {
      vals.forEachIndexed { i, v ->
        Text(
          v?.let { "${it.toInt()}" } ?: "", fontSize = 13.sp, textAlign = TextAlign.Center, modifier = Modifier.weight(1f),
          color = if (i == vals.lastIndex) Color.White else OnNightSoft, fontWeight = if (i == vals.lastIndex) FontWeight.Bold else FontWeight.Normal
        )
      }
    }
    Canvas(Modifier.fillMaxWidth().height(56.dp).padding(vertical = 6.dp)) {
      fun p(i: Int, v: Float) = Offset(size.width * (i + 0.5f) / vals.size, size.height * (1f - (v - lo) / (hi - lo)))
      val pts = vals.mapIndexedNotNull { i, v -> v?.let { p(i, it) } }
      val path = Path().apply { pts.forEachIndexed { i, o -> if (i == 0) moveTo(o.x, o.y) else lineTo(o.x, o.y) } }
      drawPath(path, AreaBarColor, style = Stroke(2.5.dp.toPx(), cap = StrokeCap.Round, join = StrokeJoin.Round))
      pts.forEachIndexed { i, o ->
        val last = i == pts.lastIndex
        drawCircle(Clay.Ink, (if (last) 6 else 5).dp.toPx(), o)
        drawCircle(if (last) Clay.Sun else AreaBarColor, (if (last) 4.5f else 3.5f).dp.toPx(), o)
      }
    }
    Row(Modifier.fillMaxWidth()) {
      listOf("hace 3", "hace 2", "pasada", "esta").forEachIndexed { i, t ->
        Text(t, fontSize = 13.sp, textAlign = TextAlign.Center, color = if (i == 3) Color.White else OnNightSoft, modifier = Modifier.weight(1f))
      }
    }
  }
}
