package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
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
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.ChallengeState
import com.example.data.DailyChallenge
import com.example.data.IslaLugar
import com.example.data.Streak
import com.example.data.Week
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.GameEmblem
import com.example.ui.components.Glyph
import com.example.ui.components.KitButton
import com.example.ui.components.KitGlyph
import com.example.ui.components.NubiClay
import com.example.ui.components.OnBoardText
import com.example.ui.components.SlabColor
import com.example.ui.components.SlabSize
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayCard
import com.example.viewmodel.NeuroVidaViewModel

/**
 * Inicio («Hoy»): el saludo, la semana (las farolas, la llama y la frase de la racha) y UNA tarjeta crema con todo lo de la ruta de hoy (Tarea 165, 24-oct; `auditoria-fable/hoy-reestructura.md`, opción 3, elegida por Ricardo después de ver los letreros de la isla de la Tarea 163, que NO se instalaron:
 * «esa pantalla se ve muy sobrecargada, demasiada sobrecarga de estímulos, de botones»): «Tu ruta de hoy», «1 de 3 · unos 4 minutos», los tres juegos con su estado en palabras (no se tocan: informan) y el botón sol ADENTRO, con Nubi posada en su esquina; debajo, «Todos los juegos».
 * Desde la Tarea 167 (25-oct; `auditoria-fable/hoy-desafio-y-dia-completo.md`, opción 1 «Dos botones, un solo sol a la vez», elegida por Ricardo) hay un segundo botón, **«Desafío de hoy»** (un juego al día que Nubi elige, en modo Desafío, con un cometa): lila junto a «Todos los juegos» mientras la ruta sigue y
 * sol a todo el ancho con la ruta completa. **Un solo sol a la vez**: el sol está en la acción del momento (la ruta, después el Desafío) y con el Desafío superado no queda ninguno: el día está hecho. Con la ruta completa la tarjeta se cierra sola (tres «Hecho», «3 de 3 · ruta completa» en negrita, sin botón ni rótulo) y ya NO hay isla en Hoy:
 * la isla vive en Tú. Con letra del sistema de 1,3 todo cabe en 393 × 873 dp sin deslizar.
 */
@Composable
fun HomeScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier
) {
  val dailySession by viewModel.dailySession.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val ranks by viewModel.gameRanks.collectAsState()
  val pausedGameId by viewModel.pausedGameId.collectAsState()
  val baseline by viewModel.baseline.collectAsState()
  val settings by viewModel.userSettings.collectAsState()
  val skill by viewModel.skill.collectAsState()
  val challenge by viewModel.challengeToday.collectAsState()
  // Reloj de Hoy (la fecha, la semana y la racha): cada 30 s.
  var clock by remember { mutableStateOf(System.currentTimeMillis()) }
  LaunchedEffect(Unit) {
    while (true) {
      kotlinx.coroutines.delay(30_000L)
      clock = System.currentTimeMillis()
    }
  }
  // El Desafío de hoy se elige una vez al día, con la ruta de hoy y lo que ya está medido: se mira al abrir Hoy, al cambiar el día y al medirse un juego.
  LaunchedEffect(dailySession.dateKey, skill.measured) { viewModel.refreshChallenge() }
  val lang = LocalAppLanguage.current
  val avg = if (ranks.isEmpty()) 0 else ranks.sumOf { it.rating } / ranks.size
  val tier = RankTier.fromRating(avg)
  val completed = dailySession.completedCount
  // La racha que perdona (Tarea 124): con el día de descanso, si lo hubo esta semana.
  val streakInfo = remember(history, clock) { Streak.fromTimestamps(history.map { it.timestamp }, clock) }
  val ui = HomeUi(
    name = settings.name,
    now = clock,
    tier = tier,
    streak = streakInfo.current,
    streakNote = Streak.forgivenLine(streakInfo) ?: Streak.REST_LINE,
    weeklyGoal = settings.weeklyGoal,
    lamps = Week.lampsFor(history.map { it.timestamp }, clock),
    stops = homeStops(dailySession.gameIds, completed, lang),
    pausedTitle = pausedGameId?.let { GameRegistry.getById(it) }?.let { getGameTitle(it.id, lang, it.title) },
    flightDay = dailySession.gameIds.isNotEmpty() && completed >= dailySession.gameIds.size && dailySession.scores.isEmpty(),
    hasBaseline = baseline != null,
    baselineInvite = com.example.data.BaselinePlan.showHomeInvite(baseline != null, history.size, history.minOfOrNull { it.timestamp }, clock),
    challenge = challenge?.let { c -> GameRegistry.getById(c.gameId)?.let { HomeChallenge(c.gameId, getGameTitle(c.gameId, lang, it.title), c.state) } }
  )
  HomeContent(
    ui = ui,
    onAllGames = { viewModel.openAllGames() },
    onPlay = { viewModel.startDailySession() },
    onResume = { viewModel.resumePausedGame() },
    onBaseline = { viewModel.startBaseline() },
    onChallenge = { viewModel.startDailyChallenge() },
    modifier = modifier
  )
}

// ------------------------------------------------------------------ Hoy con una sola tarjeta

private val HoyCream = Color(0xFFF6EBD3)
private val HoySoft = Color(0xFFD6D1F0)
private val HoyMuted = Color(0xFF4A4470)

/** Lo que Hoy muestra, ya calculado (así `HomeContent` no depende del ViewModel y se prueba y se captura con datos fijos). */
internal data class HomeUi(
  val name: String,
  val now: Long,
  val tier: RankTier,
  val streak: Int,
  /** Debajo de la racha: «Descansaste el martes: tu racha sigue.» si hubo un día de descanso perdonado en los últimos 7 días; si no, «Un día de descanso por semana no la corta.» (null: sin línea). */
  val streakNote: String? = null,
  val weeklyGoal: Int,
  val lamps: Week.Lamps,
  val stops: List<HomeStop>,
  /** Título del juego en pausa, si hay uno (el botón único pasa a «Seguir tu partida»). */
  val pausedTitle: String?,
  /** El camino de hoy quedó cumplido sin jugarlo: fue el Primer vuelo. */
  val flightDay: Boolean,
  val hasBaseline: Boolean,
  /** Si Hoy invita a «Encuentra tu punto de partida»: por omisión, mientras no lo haya; Hoy la apaga pasadas 5 partidas o 7 días (Tarea 152, B6.2). */
  val baselineInvite: Boolean = !hasBaseline,
  /** El Desafío de hoy (Tarea 167): el juego que Nubi eligió y cómo va; null = no hay candidato (todavía no hay un juego medido fuera de la ruta): Hoy queda sin la losa. */
  val challenge: HomeChallenge? = null
) {
  val completed: Int get() = stops.count { it.state == StopState.DONE }
}

internal enum class StopState { DONE, NEXT, LATER }

/** El Desafío de hoy tal como lo muestra Hoy: el juego (con su nombre visible) y si falta jugarlo, se jugó sin superarlo («otra vez») o ya se superó. */
internal data class HomeChallenge(val gameId: String, val title: String, val state: ChallengeState)

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

/** «Buenos días», «Buenas tardes» o «Buenas noches» según la hora (sin culpa: no habla de lo que falta). */
internal fun greetingWord(now: Long): String {
  val h = java.util.Calendar.getInstance().apply { timeInMillis = now }.get(java.util.Calendar.HOUR_OF_DAY)
  return when { h < 12 -> "Buenos días"; h < 20 -> "Buenas tardes"; else -> "Buenas noches" }
}

/** ¿Es de noche? (de las 20 a las 6): Nubi duerme en la tarjeta (`asleep`: ojos cerrados) en lugar de mirar de frente. */
internal fun isNight(now: Long): Boolean {
  val h = java.util.Calendar.getInstance().apply { timeInMillis = now }.get(java.util.Calendar.HOUR_OF_DAY)
  return h >= 20 || h < 6
}

/** «Domingo 11 oct.» (con el mes abreviado: a 16 sp, «Miércoles 21 de octubre · 2 de 4 días de tu meta» no cabía en una línea a 393 dp; Tarea 153). */
internal fun dateText(now: Long): String =
  java.text.SimpleDateFormat("EEEE d MMM", java.util.Locale("es")).format(java.util.Date(now)).replaceFirstChar { it.uppercase() }

/** Lo que dice la derecha de «Tu ruta de hoy»: cuántos juegos van y cuánto falta («0 de 3 · unos 6 minutos»). */
internal fun routeMeta(ui: HomeUi): String {
  val total = ui.stops.size
  val pending = ui.stops.count { it.state != StopState.DONE }
  return if (pending == 0) "$total de $total · ruta completa" else "${total - pending} de $total · ${com.example.data.DailyPath.minutesText(pending)}"
}

/**
 * El contenido de Hoy sin ViewModel (Tareas 165 y 167): saludo, la fecha con la meta de la semana, los siete farolitos con tu racha, UNA tarjeta con la ruta de hoy y, debajo, el Desafío de hoy y «Todos los juegos» (y la invitación al punto de partida, si toca). Si algo no cabe (letra del sistema muy grande),
 * toda la pantalla se desliza; con 1,3 cabe entera en 393 × 873 dp.
 */
@Composable
internal fun HomeContent(
  ui: HomeUi,
  /** La losa «Todos los juegos»: abre la ventana con los 17 y su buscador (Tarea 158). */
  onAllGames: () -> Unit,
  onPlay: () -> Unit,
  onResume: () -> Unit,
  onBaseline: () -> Unit,
  /** «Desafío de hoy»: lanza el juego que Nubi eligió en modo Desafío (Tarea 167). */
  onChallenge: () -> Unit = {},
  modifier: Modifier = Modifier
) {
  // La barra de pestañas es una capa fija (`LocalBarInset`, Tarea 123): Hoy deja esa franja abajo.
  val barInset = com.example.ui.components.LocalBarInset.current
  BoxWithConstraints(modifier.fillMaxSize()) {
    Column(Modifier.fillMaxSize().padding(bottom = barInset).verticalScroll(rememberScrollState())) {
      // Saludo. Todo el texto va a la IZQUIERDA y desde 28 dp bajo la barra: la zona del fondo sin luceros (`ZONAS_HOY` en `tools/isla/isla.py`; la vigila `HoyIslaScreenshotTest`).
      Row(Modifier.fillMaxWidth().padding(start = 20.dp, end = 16.dp, top = 28.dp), verticalAlignment = Alignment.CenterVertically) {
        OnBoardText(
          if (ui.name.isBlank()) greetingWord(ui.now) else "${greetingWord(ui.now)}, ${ui.name.trim()}",
          color = HoyCream, fontSize = 22.sp, fontWeight = FontWeight.Bold, lineHeight = 27.sp, modifier = Modifier.weight(1f).testTag("home_greeting")
        )
      }
      // Una sola línea con la fecha y la meta de la semana.
      OnBoardText(
        "${dateText(ui.now)} · ${Week.caption(ui.lamps.daysPlayed, ui.weeklyGoal)}",
        color = HoySoft, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.padding(start = 20.dp, end = 16.dp, top = 2.dp).testTag("home_date")
      )
      com.example.ui.components.HoyWeek(ui.lamps, ui.streak, ui.streakNote)
      Spacer(Modifier.height(14.dp))
      if (ui.stops.isNotEmpty() || ui.pausedTitle != null) HoyCard(ui, onPlay = onPlay, onResume = onResume, modifier = Modifier.padding(horizontal = 16.dp))
      HoyBelow(ui, onAllGames = onAllGames, onChallenge = onChallenge, onBaseline = onBaseline)
    }
  }
}

/**
 * LA tarjeta de Hoy: una losa crema con «Tu ruta de hoy» y su meta («1 de 3 · unos 4 minutos») a la izquierda y Nubi posada en su esquina de arriba a la derecha (`hola` de día; de noche duerme, con los ojos cerrados); debajo, los tres juegos de la ruta, cada uno con su emblema, su nombre y su estado en
 * palabras con su marca («Hecho» con el visto sobre lima, «Sigue» con un punto de tinta, «Después» con un aro vacío): NO se tocan —informan; un juego concreto se abre desde «Todos los juegos»—, y el botón sol ADENTRO («Empezar la ruta · 3 juegos», «Seguir la ruta · 2 juegos» o «Seguir tu partida»),
 * el único sol mientras la ruta sigue. Con la ruta COMPLETA la tarjeta se cierra sola (Tarea 167): las tres filas en «Hecho», la meta en negrita («3 de 3 · ruta completa») y Nubi saludando, sin botón ni rótulo; el sol pasa al Desafío de hoy, debajo ([HoyBelow]). Las filas se leen una a una
 * («Bodega de carga, hecho»); el botón es el único toque de la tarjeta.
 */
@Composable
private fun HoyCard(ui: HomeUi, onPlay: () -> Unit, onResume: () -> Unit, modifier: Modifier = Modifier) {
  val pending = ui.stops.count { it.state != StopState.DONE }
  val paused = ui.pausedTitle != null
  val complete = ui.stops.isNotEmpty() && pending == 0 && !paused
  val caption = captionLine(ui)
  ClayCard(modifier = modifier.fillMaxWidth().testTag("home_card"), color = Clay.Cream, size = SlabSize.LARGE, contentPadding = 0.dp) {
    Column(Modifier.fillMaxWidth().padding(start = 16.dp, end = 16.dp, top = 12.dp, bottom = 14.dp)) {
      if (ui.stops.isNotEmpty()) {
        Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
          Column(Modifier.weight(1f)) {
            Text("Tu ruta de hoy", color = Clay.Ink, fontSize = 22.sp, fontWeight = FontWeight.Bold, lineHeight = 27.sp, modifier = Modifier.testTag("home_route_title"))
            Text(
              routeMeta(ui), color = if (complete) Clay.Ink else Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp,
              fontWeight = if (complete) FontWeight.Bold else FontWeight.Normal, modifier = Modifier.testTag("home_route_meta")
            )
          }
          Spacer(Modifier.width(8.dp))
          NubiClay(width = 84.dp, asleep = isNight(ui.now), modifier = Modifier.testTag("home_nubi"))
        }
        HoyStops(ui)
      }
      if (paused || pending > 0 || caption != null) Spacer(Modifier.height(10.dp))
      when {
        paused -> ClayButton(text = "Seguir tu partida", onClick = onResume, glyph = Glyph.PLAY, fontSize = 18.sp, modifier = Modifier.testTag("btn_resume_paused"))
        pending > 0 -> {
          val label = if (ui.completed == 0) "Empezar la ruta · ${ui.stops.size} juegos" else "Seguir la ruta · $pending ${if (pending == 1) "juego" else "juegos"}"
          ClayButton(text = label, onClick = onPlay, glyph = Glyph.PLAY, fontSize = 18.sp, modifier = Modifier.testTag("btn_home_play"))
        }
      }
      caption?.let { line ->
        Text(
          line, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, textAlign = TextAlign.Center,
          modifier = Modifier.fillMaxWidth().padding(top = 8.dp).testTag(if (ui.flightDay) "home_flight_day" else "home_caption")
        )
      }
    }
  }
}

/** Lo que dice el lector de pantalla de un juego de la ruta: «Bodega de carga, hecho / es el que sigue / después». */
private fun stopDescription(stop: HomeStop): String =
  "${stop.title}, " + when (stop.state) { StopState.DONE -> "hecho"; StopState.NEXT -> "es el que sigue"; StopState.LATER -> "después" }

/** La palabra de cada estado (la marca sola no basta: acierto y estado nunca solo por forma o color). */
internal fun stopStatusWord(state: StopState): String = when (state) { StopState.DONE -> "Hecho"; StopState.NEXT -> "Sigue"; StopState.LATER -> "Después" }

/** La marca del estado de un juego: «Hecho» = un visto de tinta sobre un disco lima; «Sigue» = un punto crema en un disco de tinta; «Después» = un aro vacío. Crece con la letra del sistema (hasta 1,6). */
@Composable
private fun StopMark(state: StopState) {
  val scale = LocalDensity.current.fontScale.coerceIn(1f, 1.6f)
  val s = 24.dp * scale
  when (state) {
    StopState.DONE -> Box(Modifier.size(s).clip(CircleShape).background(Clay.Lime).border(2.dp, Clay.Ink, CircleShape), contentAlignment = Alignment.Center) { KitGlyph(Glyph.CHECK, size = 15.dp * scale) }
    StopState.NEXT -> Box(Modifier.size(s).clip(CircleShape).background(Clay.Ink), contentAlignment = Alignment.Center) { Box(Modifier.size(s * 0.36f).clip(CircleShape).background(HoyCream)) }
    StopState.LATER -> Box(Modifier.size(s).border(2.dp, HoyMuted, CircleShape))
  }
}

/**
 * Las tres filas de la tarjeta. Si la palabra MÁS LARGA de algún nombre («Constelaciones», «Acoplamiento») no cabe junto a su estado («Después» con su marca), las TRES filas ponen el estado DEBAJO del nombre (con letra 2,0, o 1,3 en un teléfono de 360 dp, el nombre y el estado lado a lado
 * partían las palabras a la mitad): se mide con la letra del sistema, igual que se hacía con las piedritas.
 */
@Composable
private fun HoyStops(ui: HomeUi) {
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  val density = LocalDensity.current
  val scale = density.fontScale.coerceIn(1f, 1.6f)
  BoxWithConstraints(Modifier.fillMaxWidth().padding(top = 4.dp)) {
    val titleStyle = base.merge(androidx.compose.ui.text.TextStyle(fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp))
    val statusStyle = base.merge(androidx.compose.ui.text.TextStyle(fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 20.sp))
    val longestPx = ui.stops.maxOf { s -> s.title.split(' ').maxOf { w -> measurer.measure(w, titleStyle, softWrap = false, maxLines = 1).size.width } }
    val statusPx = ui.stops.maxOf { s -> measurer.measure(stopStatusWord(s.state), statusStyle, softWrap = false, maxLines = 1).size.width } + with(density) { (24.dp * scale + 6.dp).toPx() }
    val availablePx = with(density) { (maxWidth - 36.dp - 10.dp - 8.dp).toPx() }                       // el ancho sin el emblema, su aire y el aire entre el nombre y el estado
    val stacked = longestPx + statusPx > availablePx
    Column(Modifier.fillMaxWidth()) {
      ui.stops.forEachIndexed { i, s ->
        if (i > 0) Box(Modifier.fillMaxWidth().height(1.dp).background(Clay.Ink.copy(alpha = 0.12f)))
        HoyStopRow(s, i + 1, stacked)
      }
    }
  }
}

/**
 * Un juego de la ruta como fila de la tarjeta (48 dp como mínimo): su emblema (36 dp), su nombre y su estado con su marca; el estado va a la derecha o, si no cabe ([HoyStops]), debajo del nombre. NO es un botón (ni `onClick` ni `Role.Button`): informa.
 * El nombre pasa a dos líneas antes que partirse una palabra (`HoyStonesWordsTest`, con letra hasta 2,0); la que sigue va en negrita (el sol es solo del botón de la tarjeta).
 */
@Composable
private fun HoyStopRow(stop: HomeStop, number: Int, stacked: Boolean) {
  @Composable fun Status(modifier: Modifier = Modifier) {
    Row(verticalAlignment = Alignment.CenterVertically, modifier = modifier) {
      StopMark(stop.state)
      Spacer(Modifier.width(6.dp))
      Text(
        stopStatusWord(stop.state), color = if (stop.state == StopState.LATER) HoyMuted else Clay.Ink, fontSize = 16.sp,
        fontWeight = if (stop.state == StopState.LATER) FontWeight.Normal else FontWeight.Bold, lineHeight = 20.sp, softWrap = false, modifier = Modifier.testTag("home_stop_state_$number")
      )
    }
  }
  Row(
    Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = stopDescription(stop) }.testTag("home_stop_$number").heightIn(min = 52.dp).padding(vertical = 4.dp),
    verticalAlignment = Alignment.CenterVertically
  ) {
    GameEmblem(stop.gameId, 36.dp)
    Spacer(Modifier.width(10.dp))
    if (stacked) {
      Column(Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(2.dp)) {
        Text(stop.title, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp, modifier = Modifier.testTag("home_stop_title_$number"))
        Status()
      }
    } else {
      Text(stop.title, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp, modifier = Modifier.weight(1f).testTag("home_stop_title_$number"))
      Spacer(Modifier.width(8.dp))
      Status()
    }
  }
}

/** El rótulo de la losa que abre la ventana con los 17 juegos. */
internal const val ALL_GAMES_LABEL = "Todos los juegos"

/** Lo que Hoy dice dentro de la tarjeta, bajo el botón, cuando hay algo que decir: la partida en pausa o el Primer vuelo. (Con la ruta completa la tarjeta ya no dice nada: el «Mañana…» pasó al bloque del Desafío, [HoyBelow].) */
internal fun captionLine(ui: HomeUi): String? = when {
  ui.pausedTitle != null -> "Tienes una partida en pausa: ${ui.pausedTitle}."
  ui.flightDay -> "Tu primer vuelo fue tu ruta de hoy. Mañana empieza tu ruta."
  else -> null
}

/** Quién tiene el SOL de Hoy (Tarea 167: «un solo sol a la vez»; antes de la 167 era «el único sol»). */
internal enum class Sun { ROUTE, CHALLENGE, NONE }

/**
 * El sol está en la acción del momento: la RUTA mientras sigue (o hay una partida en pausa que seguir), el DESAFÍO con la ruta completa y todavía sin jugar, y NADIE cuando el día está hecho (el Desafío superado) o el Desafío ya se jugó sin superarlo (crema, «otra vez»);
 * sin Desafío y con la ruta completa tampoco hay sol. Nunca dos a la vez. Robolectric no deja leer píxeles (`captureToImage` se cuelga): esta función es la que decide el color del botón (`ChallengeButton`) y la que vigilan las pruebas.
 */
internal fun sunOf(ui: HomeUi): Sun {
  val pending = ui.stops.count { it.state != StopState.DONE }
  val routeDone = ui.stops.isNotEmpty() && pending == 0 && ui.pausedTitle == null
  return when {
    ui.pausedTitle != null || pending > 0 -> Sun.ROUTE
    routeDone && ui.challenge?.state == ChallengeState.PENDIENTE -> Sun.CHALLENGE
    else -> Sun.NONE
  }
}

/** El rótulo del botón del Desafío de hoy. */
internal const val CHALLENGE_LABEL = "Desafío de hoy"

/** Lo que Hoy dice cuando el día está hecho: con el Desafío superado, o con la ruta completa y sin Desafío (todavía no hay un juego medido fuera de la ruta). */
internal const val CHALLENGE_TOMORROW = "Mañana hay ruta y Desafío nuevos."
internal const val ROUTE_TOMORROW = "Mañana te espera una ruta nueva."

/** «Desafío superado: Tinta o Palabra.» */
internal fun challengeDoneLine(title: String) = "Desafío superado: $title."

/** Lo que dice el lector de pantalla del botón: todo junto, también cuando a la vista solo dice «Desafío de hoy» (media fila). */
internal fun challengeDescription(c: HomeChallenge): String =
  if (c.state == ChallengeState.JUGADO) "$CHALLENGE_LABEL, ${c.title}, un paso más difícil, otra vez" else "$CHALLENGE_LABEL, ${c.title}, un paso más difícil, ${DailyChallenge.minutesText()}"

/**
 * La segunda línea del botón del Desafío cuando toma todo el ancho (la ruta completa): «Tinta o Palabra · unos 2 minutos» o, jugado sin superar, «Tinta o Palabra · otra vez». Con letra del sistema de 1,3 o más ([large]) solo dice el juego (o «otra vez»): la losa no crece de más.
 * En media fila (la ruta sigue) no hay segunda línea salvo «otra vez».
 */
internal fun challengeSecondLine(c: HomeChallenge, wide: Boolean, large: Boolean): String? = when {
  c.state == ChallengeState.JUGADO -> if (wide) "${c.title} · otra vez" else "otra vez"
  !wide -> null
  large -> c.title
  else -> "${c.title} · ${DailyChallenge.minutesText()}"
}

/**
 * El botón del Desafío de hoy (Tarea 167): la losa del kit con el COMETA. Lila en media fila mientras la ruta sigue (el color de los botones secundarios del kit: el sol es de la ruta); sol a todo el ancho con la ruta completa (el sol pasa a la acción del momento); crema si se jugó sin superar («otra vez»).
 * Siempre justo debajo de la tarjeta: cambia de ancho y de color pero no se mueve de lugar.
 */
@Composable
private fun ChallengeButton(c: HomeChallenge, wide: Boolean, sun: Boolean, onClick: () -> Unit, modifier: Modifier = Modifier, centerContent: Boolean = false) {
  val large = LocalDensity.current.fontScale >= 1.3f
  val color = when { sun -> SlabColor.SUN; c.state == ChallengeState.JUGADO -> SlabColor.CREAM; else -> SlabColor.LILAC }
  KitButton(
    text = CHALLENGE_LABEL, onClick = onClick, color = color, glyph = Glyph.COMET, fontSize = if (wide) 18.sp else 16.sp, minHeight = if (wide) 56.dp else 56.dp,
    secondLine = challengeSecondLine(c, wide, large), secondLineSize = 16.sp, centerContent = centerContent,
    modifier = modifier.semantics(mergeDescendants = true) { contentDescription = challengeDescription(c) }.testTag("btn_challenge")
  )
}

/** El Desafío ya se superó: el visto y «Desafío superado: Tinta o Palabra.» y, si la ruta también está completa (el día está hecho), «Mañana hay ruta y Desafío nuevos.». Ningún sol. */
@Composable
private fun ChallengeDoneNote(c: HomeChallenge, dayDone: Boolean) {
  Column(Modifier.fillMaxWidth().padding(top = 6.dp, bottom = 2.dp).semantics(mergeDescendants = true) { contentDescription = challengeDoneLine(c.title) + if (dayDone) " $CHALLENGE_TOMORROW" else "" }.testTag("home_challenge_done")) {
    Row(verticalAlignment = Alignment.CenterVertically) {
      StopMark(StopState.DONE)
      Spacer(Modifier.width(8.dp))
      OnBoardText(challengeDoneLine(c.title), color = HoyCream, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp, modifier = Modifier.weight(1f))
    }
    if (dayDone) OnBoardText(CHALLENGE_TOMORROW, color = HoySoft, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.padding(top = 2.dp))
  }
}

/** «Todos los juegos»: una losa crema con las cuatro casillas; en media fila (junto al Desafío) va sin ícono, para que el rótulo quepa en UNA línea con letra 1,0. */
@Composable
private fun AllGamesButton(onAllGames: () -> Unit, half: Boolean, modifier: Modifier = Modifier, centerContent: Boolean = false) {
  KitButton(
    text = ALL_GAMES_LABEL, onClick = onAllGames, color = SlabColor.CREAM, fontSize = if (half) 16.sp else 17.sp, minHeight = if (half) 56.dp else 48.dp,
    leading = if (half) null else { { com.example.ui.components.NavIconImage(com.example.ui.components.NavIcon.CASILLAS, size = 32.dp) } }, centerContent = centerContent,
    modifier = modifier.testTag("btn_all_games")
  )
}

/**
 * Bajo la tarjeta (Tarea 167, «un solo sol a la vez»): el Desafío de hoy y «Todos los juegos».
 *  - La ruta sigue (o hay una partida en pausa): una fila con «Desafío de hoy» en LILA y el cometa, junto a «Todos los juegos» en crema; el sol está en la tarjeta.
 *  - La ruta completa: el Desafío toma todo el ancho en SOL («Tinta o Palabra · unos 2 minutos») y «Todos los juegos» baja; jugado sin superar, en crema («Tinta o Palabra · otra vez»).
 *  - Desafío superado: el visto con «Desafío superado: Tinta o Palabra.» (y «Mañana hay ruta y Desafío nuevos.» si la ruta también está completa) y NINGÚN sol: el día está hecho.
 *  - Sin Desafío (todavía no hay un juego medido fuera de la ruta): Hoy queda como antes, solo «Todos los juegos» y, con la ruta completa, «Mañana te espera una ruta nueva.».
 * Y, si todavía no hizo la evaluación, la invitación a hacerla.
 */
@Composable
private fun HoyBelow(ui: HomeUi, onAllGames: () -> Unit, onChallenge: () -> Unit, onBaseline: () -> Unit) {
  val pending = ui.stops.count { it.state != StopState.DONE }
  val routeDone = ui.stops.isNotEmpty() && pending == 0 && ui.pausedTitle == null
  val c = ui.challenge
  Column(Modifier.fillMaxWidth().padding(start = 16.dp, end = 16.dp, top = 6.dp, bottom = 4.dp)) {
    when {
      c == null -> {
        if (routeDone && !ui.flightDay) OnBoardText(ROUTE_TOMORROW, color = HoyCream, fontSize = 17.sp, fontWeight = FontWeight.Bold, lineHeight = 21.sp, modifier = Modifier.fillMaxWidth().padding(top = 8.dp).testTag("home_route_tomorrow"), textAlign = TextAlign.Center)
        AllGamesButton(onAllGames, half = false, modifier = Modifier.padding(top = 8.dp))
      }
      c.state == ChallengeState.SUPERADO -> {
        ChallengeDoneNote(c, dayDone = routeDone)
        AllGamesButton(onAllGames, half = false, modifier = Modifier.padding(top = 8.dp))
      }
      routeDone -> {
        ChallengeButton(c, wide = true, sun = sunOf(ui) == Sun.CHALLENGE, onClick = onChallenge, modifier = Modifier.padding(top = 8.dp))
        AllGamesButton(onAllGames, half = false, modifier = Modifier.padding(top = 8.dp))
      }
      // Los dos botones de la fila miden lo mismo (con letra grande «Desafío de hoy» pasa a dos líneas): el alto del más alto, con el texto centrado
      else -> Row(Modifier.fillMaxWidth().height(IntrinsicSize.Min).padding(top = 8.dp), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
        ChallengeButton(c, wide = false, sun = false, onClick = onChallenge, modifier = Modifier.weight(1f).fillMaxHeight(), centerContent = true)
        AllGamesButton(onAllGames, half = true, modifier = Modifier.weight(1f).fillMaxHeight(), centerContent = true)
      }
    }
    if (ui.baselineInvite) {
      // El enlace sigue siendo un enlace pero en crema tenue (`OnNightDim`) con su chevrón, para que no compita con el sol de la ruta o del Desafío
      Row(
        Modifier
          .padding(top = 4.dp)
          .clip(RoundedCornerShape(10.dp))
          .clickable(role = androidx.compose.ui.semantics.Role.Button, onClick = onBaseline)
          .heightIn(min = 48.dp)
          .padding(horizontal = 4.dp)
          .testTag("btn_home_baseline"),
        verticalAlignment = Alignment.CenterVertically
      ) {
        OnBoardText(
          text = "Encuentra tu punto de partida",                 // una línea, con su flecha (Opus, 21-oct; antes «… · 4 juegos cortos», que bajaba a 2 líneas)
          color = com.example.ui.theme.NubiTokens.OnNightDim,
          fontSize = 16.sp,
          fontWeight = FontWeight.SemiBold,
          modifier = Modifier.weight(1f, fill = false)
        )
        Spacer(Modifier.width(6.dp))
        androidx.compose.foundation.Canvas(Modifier.size(width = 10.dp, height = 16.dp)) {
          val p = androidx.compose.ui.graphics.Path().apply { moveTo(size.width * 0.15f, size.height * 0.1f); lineTo(size.width * 0.85f, size.height * 0.5f); lineTo(size.width * 0.15f, size.height * 0.9f) }
          drawPath(p, com.example.ui.theme.NubiTokens.OnNightDim, style = androidx.compose.ui.graphics.drawscope.Stroke(2.dp.toPx(), cap = androidx.compose.ui.graphics.StrokeCap.Round, join = androidx.compose.ui.graphics.StrokeJoin.Round))
        }
      }
    }
  }
}
