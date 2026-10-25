package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
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
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.IslaLugar
import com.example.data.IslaPiezas
import com.example.data.PrefsRouteCelebrationStore
import com.example.data.RouteCelebrationStore
import com.example.data.RouteLamps
import com.example.data.RoutePlan
import com.example.data.Streak
import com.example.data.Week
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.GameEmblem
import com.example.ui.components.Glyph
import com.example.ui.components.Island
import com.example.ui.components.KitGlyph
import com.example.ui.components.NubiClay
import com.example.ui.components.OnBoardText
import com.example.ui.components.RouteFx
import com.example.ui.components.RouteMotion
import com.example.ui.components.SlabSize
import com.example.ui.components.rememberIslaLayout
import com.example.ui.components.rememberReduceMotion
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayCard
import com.example.viewmodel.NeuroVidaViewModel

/**
 * Inicio («Hoy») con UNA sola tarjeta (Tarea 165, 24-oct; `auditoria-fable/hoy-reestructura.md`, opción 3, elegida por Ricardo después de ver los letreros de la isla de la Tarea 163, que NO se instalaron: «esa pantalla se ve muy sobrecargada, demasiada sobrecarga de
 * estímulos, de botones»). Hoy tenía 12 toques y once focos para una sola tarea (jugar la ruta): ahora queda el saludo, la semana (las farolas, la llama y la frase de la racha) y UNA tarjeta crema con todo lo de la ruta de hoy —«Tu ruta de hoy», «1 de 3 · unos 4 minutos», los tres
 * juegos con su estado en palabras (no se tocan: informan) y el botón sol ADENTRO, el único sol de Hoy—, con Nubi posada en su esquina; debajo, solo «Todos los juegos». SIN isla: la isla vive en Tú (con las piezas que ganas) y aparece en Hoy UNA vez al día, dentro de la tarjeta,
 * cuando la ruta queda completa, con Nubi celebrando (el premio). Se fueron la isla grande, los edificios como botones, la lámina del edificio, Nubi volando, las piedritas y el globo de la isla. Con letra del sistema de 1,3 todo cabe en 393 × 873 dp sin deslizar.
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
  val unlocks by viewModel.achievementUnlocks.collectAsState()
  val debugPieces by viewModel.debugIslaPieces.collectAsState()
  // Reloj de Hoy (la fecha, la semana y la racha): cada 30 s.
  var clock by remember { mutableStateOf(System.currentTimeMillis()) }
  LaunchedEffect(Unit) {
    while (true) {
      kotlinx.coroutines.delay(30_000L)
      clock = System.currentTimeMillis()
    }
  }
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
    islandPieces = debugPieces ?: IslaPiezas.earned(unlocks.keys)          // la isla chica de la ruta completa lleva las piezas que ganaste (solo el botón [Debug] de Ajustes lo cambia)
  )
  val appContext = androidx.compose.ui.platform.LocalContext.current
  val routeStore = remember { PrefsRouteCelebrationStore(appContext) }
  HomeContent(
    ui = ui,
    onAllGames = { viewModel.openAllGames() },
    onPlay = { viewModel.startDailySession() },
    onResume = { viewModel.resumePausedGame() },
    onBaseline = { viewModel.startBaseline() },
    modifier = modifier,
    routeStore = routeStore
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
  /** Las piezas de la isla que dan las medallas ganadas («Tu isla crece»; `data/IslaPiezas.kt`), en el orden de dibujo: las lleva la isla chica de la ruta completa. */
  val islandPieces: List<com.example.data.IslaPieza> = emptyList()
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

/**
 * El nombre que dice el lector de pantalla para la isla chica de la ruta completa, con el estado de la ruta de hoy (Tarea 88): «Tu isla: ruta completa, hechos el Observatorio, la Antena y el Hangar». Las farolas y su animación son decorativas.
 */
internal fun islandDescription(ui: HomeUi): String =
  RouteLamps.description(ui.stops.mapNotNull { s -> IslaLugar.withArticle(s.area)?.let { it to (s.state == StopState.DONE) } }) + IslaPiezas.description(ui.islandPieces)

/** Lo que dice la derecha de «Tu ruta de hoy»: cuántos juegos van y cuánto falta («0 de 3 · unos 6 minutos»). */
internal fun routeMeta(ui: HomeUi): String {
  val total = ui.stops.size
  val pending = ui.stops.count { it.state != StopState.DONE }
  return if (pending == 0) "$total de $total · completa" else "${total - pending} de $total · ${com.example.data.DailyPath.minutesText(pending)}"
}

/**
 * El contenido de Hoy sin ViewModel (Tarea 165): saludo, la fecha con la meta de la semana, los siete farolitos con tu racha, UNA tarjeta con la ruta de hoy y, debajo, «Todos los juegos» (y la invitación al punto de partida, si toca). Si algo no cabe (letra del sistema muy grande),
 * toda la pantalla se desliza; con 1,3 cabe entera en 393 × 873 dp. [routeStore] = dónde se guarda lo celebrado hoy (la ruta completa): con null (pruebas de dibujo) no hay celebración y las farolas de la isla chica ya están encendidas y quietas.
 * [onRoutePlan] avisa qué se va a animar (pruebas): se llama una vez por plan no vacío, justo antes de animar.
 */
@Composable
internal fun HomeContent(
  ui: HomeUi,
  /** La losa «Todos los juegos»: abre la ventana con los 17 y su buscador (Tarea 158). */
  onAllGames: () -> Unit,
  onPlay: () -> Unit,
  onResume: () -> Unit,
  onBaseline: () -> Unit,
  modifier: Modifier = Modifier,
  routeStore: RouteCelebrationStore? = null,
  onRoutePlan: (RoutePlan) -> Unit = {}
) {
  val reduceMotion = rememberReduceMotion()
  val confirm = com.example.ui.components.rememberKitConfirm()       // el «listo» háptico al completar la ruta (KitHaptics)
  // La celebración de la ruta completa (Tarea 88): el plan sale de lo celebrado y se guarda al empezar a animar, así Nubi salta UNA vez por día. La isla chica solo se ve con la ruta completa; mientras no, lo que se enciende no tiene dónde verse y queda anotado igual.
  val doneKeys = ui.stops.filter { it.state == StopState.DONE }.mapNotNull { RouteLamps.placeKey(it.area) }
  val allDone = ui.stops.isNotEmpty() && ui.stops.all { it.state == StopState.DONE }
  val day = RouteLamps.dayKey(ui.now)
  val routeFx = remember(day, doneKeys, allDone, routeStore, reduceMotion) {
    RouteFx(plan = if (routeStore == null) RoutePlan.NONE else RouteLamps.plan(doneKeys, allDone, routeStore.load(), day), reduceMotion = reduceMotion)
  }
  LaunchedEffect(routeFx) {
    if (routeStore != null && !routeFx.plan.isEmpty) {
      routeStore.save(RouteLamps.consume(routeFx.plan, routeStore.load(), day))
      onRoutePlan(routeFx.plan)
      if (routeFx.plan.complete) confirm()
      routeFx.run()
    }
  }
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
      if (ui.stops.isNotEmpty() || ui.pausedTitle != null) HoyCard(ui, routeFx, onPlay = onPlay, onResume = onResume, modifier = Modifier.padding(horizontal = 16.dp))
      HoyBelow(ui, onAllGames = onAllGames, onBaseline = onBaseline)
    }
  }
}

/** Qué se ve de la tarjeta: la ruta con sus tres juegos, o la isla chica con Nubi celebrando (la ruta completa). */
private enum class CardMode { ROUTE, CELEBRATION }

/**
 * LA tarjeta de Hoy: una losa crema con «Tu ruta de hoy» y su meta («1 de 3 · unos 4 minutos») a la izquierda y Nubi posada en su esquina de arriba a la derecha (`hola` de día; de noche duerme, con los ojos cerrados); debajo, los tres juegos de la ruta, cada uno con su emblema, su nombre y su estado en
 * palabras con su marca («Hecho» con el visto sobre lima, «Sigue» con un punto de tinta, «Después» con un aro vacío): NO se tocan —informan; un juego concreto se abre desde «Todos los juegos»—, y el botón sol ADENTRO («Empezar la ruta · 3 juegos», «Seguir la ruta · 2 juegos» o «Seguir tu partida»),
 * el único sol de la tarjeta. Con la ruta completa, en lugar de Nubi y de las filas, la isla chica de Tú con Nubi celebrando (el premio) y «Ruta completa» como rótulo en tinta (sin losa: no es un botón). Las filas se leen una a una («Bodega de carga, hecho»); el botón es el único toque de la tarjeta.
 */
@Composable
private fun HoyCard(ui: HomeUi, routeFx: RouteFx, onPlay: () -> Unit, onResume: () -> Unit, modifier: Modifier = Modifier) {
  val pending = ui.stops.count { it.state != StopState.DONE }
  val paused = ui.pausedTitle != null
  val mode = if (ui.stops.isNotEmpty() && pending == 0 && !paused) CardMode.CELEBRATION else CardMode.ROUTE
  ClayCard(modifier = modifier.fillMaxWidth().testTag("home_card"), color = Clay.Cream, size = SlabSize.LARGE, contentPadding = 0.dp) {
    Column(Modifier.fillMaxWidth().padding(start = 16.dp, end = 16.dp, top = 12.dp, bottom = 14.dp)) {
      if (ui.stops.isNotEmpty()) {
        Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
          Column(Modifier.weight(1f)) {
            Text("Tu ruta de hoy", color = Clay.Ink, fontSize = 22.sp, fontWeight = FontWeight.Bold, lineHeight = 27.sp, modifier = Modifier.testTag("home_route_title"))
            Text(routeMeta(ui), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, modifier = Modifier.testTag("home_route_meta"))
          }
          if (mode == CardMode.ROUTE) {
            Spacer(Modifier.width(8.dp))
            NubiClay(width = 84.dp, asleep = isNight(ui.now), modifier = Modifier.testTag("home_nubi"))
          }
        }
        if (mode == CardMode.CELEBRATION) HoyCelebration(ui, routeFx)
        else HoyStops(ui)
      }
      Spacer(Modifier.height(10.dp))
      when {
        paused -> ClayButton(text = "Seguir tu partida", onClick = onResume, glyph = Glyph.PLAY, fontSize = 18.sp, modifier = Modifier.testTag("btn_resume_paused"))
        pending > 0 -> {
          val label = if (ui.completed == 0) "Empezar la ruta · ${ui.stops.size} juegos" else "Seguir la ruta · $pending ${if (pending == 1) "juego" else "juegos"}"
          ClayButton(text = label, onClick = onPlay, glyph = Glyph.PLAY, fontSize = 18.sp, modifier = Modifier.testTag("btn_home_play"))
        }
        // «Ruta completa» es un ROTULO, no un botón: el visto y el texto en tinta, centrados bajo la isla, sin losa, borde ni sombra (la losa lima con borde se leía como un botón que no hace nada)
        else -> Row(
          Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = "Ruta completa" }.testTag("home_route_done").heightIn(min = 40.dp).padding(vertical = 4.dp),
          horizontalArrangement = Arrangement.Center, verticalAlignment = Alignment.CenterVertically
        ) {
          KitGlyph(Glyph.CHECK)
          Spacer(Modifier.width(8.dp))
          Text("Ruta completa", color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold)
        }
      }
      captionLine(ui)?.let { line ->
        Text(
          line, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 21.sp, textAlign = TextAlign.Center,
          modifier = Modifier.fillMaxWidth().padding(top = 8.dp).testTag(if (ui.flightDay) "home_flight_day" else "home_caption")
        )
      }
    }
  }
}

/** El alto de la isla chica de la ruta completa (dp): el de la miniatura de Tú. */
private val HOY_CELEBRATION_ISLAND_HEIGHT = 150.dp

/**
 * El premio de la ruta completa: la isla chica (la de Tú, con las piezas que ganaste) con Nubi celebrando, en su ventanilla noche (la isla se calibró sobre el cielo oscuro) dentro de la tarjeta crema. Las farolas, el saltito de Nubi y las chispas son las de la Tarea 88: una sola vez al día,
 * y con «quitar animaciones» la isla está quieta.
 */
@Composable
private fun HoyCelebration(ui: HomeUi, routeFx: RouteFx) {
  val layout = rememberIslaLayout()
  Box(
    Modifier.fillMaxWidth().padding(top = 8.dp).height(HOY_CELEBRATION_ISLAND_HEIGHT).clip(RoundedCornerShape(18.dp))
      .background(Brush.verticalGradient(listOf(Color(0xFF100F3A), Color(0xFF1C1B52)))),
    contentAlignment = Alignment.Center
  ) {
    Island(
      description = islandDescription(ui),
      layout = layout,
      doneAreas = ui.stops.filter { it.state == StopState.DONE }.map { it.area }.toSet(),
      lookKey = routeFx.lookKey,
      lookAmount = { routeFx.look.value },
      lookMoves = routeFx.moves,
      lampProgress = { routeFx.pulseProgress(it) },
      sparks = { routeFx.sparks.value },
      nubiLiftDp = { RouteMotion.jumpLiftDp(routeFx.jump.value) },
      nubiTiltDeg = { RouteMotion.jumpTiltDeg(routeFx.jump.value) },
      pieces = ui.islandPieces,
      modifier = Modifier.fillMaxSize().testTag("home_isla")
    )
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

/** Lo que Hoy dice dentro de la tarjeta, bajo el botón, cuando hay algo que decir: la partida en pausa, la ruta ya cumplida o el Primer vuelo. */
internal fun captionLine(ui: HomeUi): String? {
  val pending = ui.stops.count { it.state != StopState.DONE }
  return when {
    ui.pausedTitle != null -> "Tienes una partida en pausa: ${ui.pausedTitle}."
    ui.flightDay -> "Tu primer vuelo fue tu ruta de hoy. Mañana empieza tu ruta."
    ui.stops.isNotEmpty() && pending == 0 -> "Mañana te espera una ruta nueva."
    else -> null
  }
}

/** Bajo la tarjeta, solo «Todos los juegos» (una losa crema, visible y ancha, con las cuatro casillas: Tarea 158) y, si todavía no hizo la evaluación, la invitación a hacerla. Es crema para que el único sol de Hoy sea el botón de la tarjeta. */
@Composable
private fun HoyBelow(ui: HomeUi, onAllGames: () -> Unit, onBaseline: () -> Unit) {
  Column(Modifier.fillMaxWidth().padding(start = 16.dp, end = 16.dp, top = 6.dp, bottom = 4.dp)) {
    com.example.ui.components.KitButton(
      text = ALL_GAMES_LABEL, onClick = onAllGames, color = com.example.ui.components.SlabColor.CREAM, fontSize = 17.sp, minHeight = 48.dp,
      leading = { com.example.ui.components.NavIconImage(com.example.ui.components.NavIcon.CASILLAS, size = 32.dp) },
      modifier = Modifier.padding(top = 8.dp).testTag("btn_all_games")
    )
    if (ui.baselineInvite) {
      // El enlace sigue siendo un enlace pero en crema tenue (`OnNightDim`) con su chevrón, para que no compita con el botón de la tarjeta, que lleva el único sol
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
