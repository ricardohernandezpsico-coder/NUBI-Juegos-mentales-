package com.example.ui.screens

import androidx.activity.compose.BackHandler
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.layout.wrapContentHeight
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.Stable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.runtime.snapshotFlow
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.focus.focusTarget
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.TransformOrigin
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.layout
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.liveRegion
import androidx.compose.ui.semantics.paneTitle
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.R
import com.example.data.IslaLayout
import com.example.data.PlaceGameRow
import com.example.data.PlaceSheetUi
import com.example.data.PlaceSheets
import com.example.data.Skill
import com.example.ui.components.Glyph
import com.example.ui.components.KitCloseButton
import com.example.ui.components.PlaceMotion
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Job
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.launch
import kotlin.math.min

// ------------------------------------------------------------------ La lámina del lugar (Tarea 85)

internal val SheetCream = Color(0xFFF5EBDB)
internal val SheetMuted = Color(0xFF4A4470)
private val GlobeFill = Color(0xFFFFF8EC)

/** El color de cada área (los del boceto): el punto junto al nombre del área y de cada juego. */
internal fun placeColor(area: String): Color = when (area) {
  "MEMORIA" -> Color(0xFF4F86D9)
  "ATENCION" -> Color(0xFFE8913A)
  "RAZONAMIENTO" -> Color(0xFF8A6FD8)
  else -> Color(0xFF3E9C5C)
}

/**
 * El estado y las animaciones de la lámina que se abre al tocar un edificio de la isla (boceto «A» de Hoy, aprobado por Ricardo el 12-oct): Nubi mira hacia el edificio (fundido de 180 ms, se corre y se ladea), a los ~650 ms cae el velo, sube la lámina y Nubi
 * vuela en arco hasta posarse arriba a la izquierda de ella; al cerrar, la lámina baja y Nubi vuela de vuelta. Cada pieza es un [Animatable] (de 0 a 1) que lee quien la dibuja: nada recompone cuadro a cuadro. Con «quitar animaciones» ([moves] = false) no hay vuelo ni
 * corrimiento: la lámina y Nubi aparecen en su sitio con un fundido corto y la mirada solo cambia la imagen.
 */
@Stable
internal class PlaceFx {
  /** El lugar tocado (`observatorio`…); null = ninguno (la lámina está cerrada del todo). */
  var key by mutableStateOf<String?>(null)
  var closing by mutableStateOf(false)
  /** true = Nubi va hacia la lámina; false = vuelve a la isla. */
  var outbound by mutableStateOf(true)
  var moves by mutableStateOf(true)

  /** 0 = Nubi de frente; 1 = mirando al lugar (fundido, corrimiento y ladeo). */
  val look = Animatable(0f)
  val glow = Animatable(0f)
  val veil = Animatable(0f)
  val sheet = Animatable(0f)
  /** 0 = Nubi en la isla; 1 = posada en la lámina. */
  val flight = Animatable(0f)
  val globe = Animatable(0f)
  val shadow = Animatable(1f)

  private var job: Job? = null

  val active: Boolean get() = key != null

  fun open(scope: CoroutineScope, place: String, reduceMotion: Boolean) {
    if (key != null) return
    key = place; closing = false; outbound = true; moves = !reduceMotion
    job?.cancel()
    job = scope.launch {
      if (reduceMotion) {
        look.snapTo(1f); flight.snapTo(1f); shadow.snapTo(0f); glow.snapTo(0f)
        coroutineScope {
          launch { veil.animateTo(1f, tween(PlaceMotion.REDUCED_FADE_MS)) }
          launch { sheet.animateTo(1f, tween(PlaceMotion.REDUCED_FADE_MS)) }
          launch { globe.animateTo(1f, tween(PlaceMotion.REDUCED_FADE_MS)) }
        }
      } else {
        val hold = PlaceMotion.LOOK_HOLD_MS
        coroutineScope {
          launch { look.animateTo(1f, tween(PlaceMotion.LOOK_SHIFT_MS)) }
          launch { glow.animateTo(1f, tween(PlaceMotion.GLOW_MS)); glow.animateTo(0f, tween(PlaceMotion.GLOW_MS, delayMillis = hold - 2 * PlaceMotion.GLOW_MS)) }
          launch { veil.animateTo(1f, tween(PlaceMotion.VEIL_MS, delayMillis = hold)) }
          launch { sheet.animateTo(1f, tween(PlaceMotion.SHEET_MS, delayMillis = hold + PlaceMotion.SHEET_DELAY_MS, easing = PlaceMotion.SheetEasing)) }
          launch { flight.animateTo(1f, tween(PlaceMotion.FLIGHT_MS, delayMillis = hold)) }
          launch { shadow.animateTo(0f, tween(PlaceMotion.SHADOW_MS, delayMillis = hold)) }
          launch { globe.animateTo(1f, tween(PlaceMotion.GLOBE_MS, delayMillis = hold + PlaceMotion.GLOBE_DELAY_MS)) }
        }
      }
    }
  }

  fun close(scope: CoroutineScope, reduceMotion: Boolean) {
    if (key == null || closing) return
    closing = true; outbound = false; moves = !reduceMotion
    job?.cancel()
    job = scope.launch {
      globe.snapTo(0f)
      if (reduceMotion) {
        coroutineScope {
          launch { veil.animateTo(0f, tween(PlaceMotion.REDUCED_FADE_MS)) }
          launch { sheet.animateTo(0f, tween(PlaceMotion.REDUCED_FADE_MS)) }
        }
        flight.snapTo(0f); look.snapTo(0f); shadow.snapTo(1f)
      } else {
        coroutineScope {
          launch { veil.animateTo(0f, tween(PlaceMotion.VEIL_MS)) }
          launch { sheet.animateTo(0f, tween(PlaceMotion.SHEET_MS)) }
          launch { flight.animateTo(0f, tween(PlaceMotion.BACK_FLIGHT_MS)) }
          launch { look.animateTo(0f, tween(PlaceMotion.LOOK_SHIFT_MS)) }
          launch { glow.snapTo(0f); shadow.animateTo(1f, tween(PlaceMotion.SHADOW_MS, delayMillis = PlaceMotion.BACK_FLIGHT_MS - PlaceMotion.SHADOW_MS)) }
        }
      }
      key = null; closing = false; outbound = true
    }
  }

  /** Lo deja todo cerrado de golpe (sin animar): cuando Hoy deja de verse con la lámina abierta. */
  fun reset(scope: CoroutineScope) {
    job?.cancel()
    job = scope.launch {
      look.snapTo(0f); glow.snapTo(0f); veil.snapTo(0f); sheet.snapTo(0f); flight.snapTo(0f); globe.snapTo(0f); shadow.snapTo(1f)
      key = null; closing = false; outbound = true
    }
  }
}

/** Dónde está Nubi `hola` en la isla ([island] = la caja de la isla en la misma ventana que la capa), ya corrida y ladeada por la mirada. */
private fun nubiRest(island: Rect, layout: IslaLayout): Rect {
  val b = layout
  return Rect(
    island.left + island.width * b.nubiTopLeft.x, island.top + island.height * b.nubiTopLeft.y,
    island.left + island.width * b.nubiBottomRight.x, island.top + island.height * b.nubiBottomRight.y
  )
}

/**
 * Lo que va ENCIMA de Hoy mientras la lámina está abierta o abriéndose: el velo, la lámina y Nubi volando (o posada en ella). Va dentro de la caja de Hoy (no es un `Dialog`), que ocupa toda la pestaña: con la lámina abierta la barra de pestañas se retira y esa
 * caja llega hasta la barra del teléfono, así que el botón «Jugar» queda sobre ella. [island] = la caja de la isla en esta misma caja (px).
 *
 * La lámina MIDE LO QUE NECESITA (Tarea 147; `auditoria-fable/spec-lamina-lugar.md`): ya no empieza al 22 % fijo. Abre desde abajo con el alto de su contenido y como mucho `página − 56 dp` (siempre queda un poco de cielo arriba); si no cabe, cede lo secundario
 * en este orden: el globo, los nombres de las etapas, el rótulo «Tus juegos aquí» y, solo si aun así no cabe, el medio se desliza. Nubi se posa DENTRO de la lámina, en el hueco que ella le deja ([PlaceSheetContent]).
 */
@Composable
internal fun PlaceLayer(
  fx: PlaceFx,
  place: PlaceSheetUi?,
  layout: IslaLayout,
  island: Rect,
  onClose: () -> Unit,
  onPlay: (String) -> Unit,
  onGame: (String) -> Unit,
  modifier: Modifier = Modifier
) {
  if (!fx.active || place == null) return
  val density = LocalDensity.current
  val rest = nubiRest(island, layout)
  val nubiAspect = if (rest.width > 0f) rest.height / rest.width else 0.675f
  // dónde queda el hueco de Nubi dentro de la lámina (px, desde su esquina de arriba a la izquierda) y cuánto mide la lámina: con eso se sabe dónde se posa Nubi aunque la lámina cambie de alto
  var hole by remember(place.key) { mutableStateOf(Offset.Zero) }
  var sheetHeightPx by remember(place.key) { mutableIntStateOf(0) }

  Box(modifier.fillMaxSize()) {
    // El velo (casi negro al 45 %, `PlaceMotion.VEIL`): tocarlo cierra la lámina.
    Box(
      Modifier
        .fillMaxSize()
        .graphicsLayer { alpha = fx.veil.value }
        .background(PlaceMotion.VEIL)
        .clickable(role = Role.Button, onClickLabel = "Cerrar la lámina", onClick = onClose)
        .testTag("place_veil")
    )

    BoxWithConstraints(Modifier.fillMaxSize()) {
      val pageHeight = maxHeight
      val pageHeightPx = with(density) { pageHeight.toPx() }
      val perchW = with(density) { PLACE_NUBI_WIDTH.toPx() }
      val perch = Rect(hole.x, pageHeightPx - sheetHeightPx + hole.y, hole.x + perchW, pageHeightPx - sheetHeightPx + hole.y + perchW * nubiAspect)
      // La lámina sube desde abajo (con «quitar animaciones» aparece en su sitio con un fundido).
      Box(
        Modifier
          .align(Alignment.BottomCenter)
          .fillMaxWidth()
          .onSizeChanged { sheetHeightPx = it.height }
          .graphicsLayer {
            val p = fx.sheet.value
            if (fx.moves) translationY = (1f - p) * size.height else alpha = p
          }
      ) {
        PlaceSheetContent(
          place, nubiAspect = nubiAspect, maxHeight = (pageHeight - PLACE_MIN_SKY).coerceAtLeast(0.dp), onClose = onClose, onPlay = onPlay, onGame = onGame,
          globe = { fx.globe.value }, globeMoves = fx.moves, onHole = { hole = it }
        )
      }
      // Nubi: en vuelo (de la isla a la lámina o de vuelta) o ya posada en su hueco. Mientras no vuela, la dibuja la isla.
      NubiFlight(fx, layout, island, perch)
    }
  }
}

/** Cuánto cielo queda como mínimo sobre la lámina: nunca llega al borde de arriba, así sigue leyéndose como una lámina que sube y no como otra pantalla. */
internal val PLACE_MIN_SKY = 56.dp

/** El ancho de Nubi posada en la lámina: el tamaño S de Nubi (P3: 72 dp), la misma imagen que vuela. */
internal val PLACE_NUBI_WIDTH = 72.dp

/** Nubi `hola` entre su sitio en la isla y su sitio en la lámina, con el arco y el balanceo del boceto. Solo se dibuja mientras `flight` > 0. */
@Composable
private fun NubiFlight(fx: PlaceFx, layout: IslaLayout, island: Rect, perch: Rect) {
  val density = LocalDensity.current
  val rest = nubiRest(island, layout)
  val look = PlaceMotion.look(fx.key)
  androidx.compose.ui.layout.Layout(
    content = {
      Image(
        painterResource(R.drawable.isla_nubi_hola), contentDescription = null,
        modifier = Modifier
          .testTag("place_nubi")
          .graphicsLayer {
            val f = fx.flight.value
            val fl = PlaceMotion.flight(f, fx.outbound)
            val e = fl.easedPosition
            alpha = if (f > 0f) 1f else 0f
            val sc = PlaceMotion.lerp(1f, perch.width / rest.width, e)
            scaleX = sc; scaleY = sc
            transformOrigin = TransformOrigin(0f, 0f)
            rotationZ = PlaceMotion.lerp(if (fx.moves) look.tiltDeg * fx.look.value else 0f, 0f, e) + if (fx.moves) fl.tiltDeg else 0f
          },
        contentScale = androidx.compose.ui.layout.ContentScale.FillBounds
      )
    },
    modifier = Modifier.fillMaxSize()
  ) { measurables, constraints ->
    val p = measurables[0].measure(androidx.compose.ui.unit.Constraints.fixed(rest.width.toInt().coerceAtLeast(1), rest.height.toInt().coerceAtLeast(1)))
    layout(constraints.maxWidth, constraints.maxHeight) {
      // El lugar de Nubi cambia cuadro a cuadro: se lee la animación al colocar (no recompone nada).
      val fl = PlaceMotion.flight(fx.flight.value, fx.outbound)
      val e = fl.easedPosition
      val startLeft = rest.left + if (fx.moves) look.shift * island.width * fx.look.value else 0f
      val left = PlaceMotion.lerp(startLeft, perch.left, e)
      val top = PlaceMotion.lerp(rest.top, perch.top, e) + if (fx.moves) with(density) { fl.arcDp.dp.toPx() } else 0f
      p.place(left.toInt(), top.toInt())
    }
  }
}

/** Lo que cede la lámina cuando el contenido no cabe en `página − 56 dp`, en este orden (cada nivel suma al anterior). */
internal object PlaceLevels {
  /** Todo a la vista: Nubi con su globo. */
  const val FULL = 0
  /** Cede el globo: Nubi se queda, muda, junto a la línea de avance. */
  const val NO_GLOBE = 1
  /** Ceden también los nombres de las 5 etapas (queda la barra). */
  const val NO_STAGE_NAMES = 2
  /** Cede también el rótulo «Tus juegos aquí». */
  const val NO_LABEL = 3
  /** Ni así cabe (letra 2,0 con 5 juegos en una pantalla chica): el medio se desliza y el botón queda fijo. */
  const val SCROLL = 4
}

/**
 * La lámina de plasticina en UNA sola vista (Tarea 147; `auditoria-fable/spec-lamina-lugar.md`). De arriba abajo: el lugar con su área y la X; Nubi (S, 72 dp) con su globo, que dice lo que cambió esta semana y lo que falta; el avance en UNA línea
 * («Hábil · 47 de 100») sobre la barra del kit con sus 5 etapas; «Tus juegos aquí» y los juegos en filas compactas (emblema, nombre, cuándo y, en el que sugiere Nubi, el chip «Nubi sugiere»); y, fijo abajo, «Jugar <el que sugiere Nubi>» con su emblema.
 * Mide lo que necesita (hasta [maxHeight]): si no cabe, sube un nivel de [PlaceLevels] hasta que cabe. [nubiAspect] = alto / ancho de Nubi; [onHole] avisa dónde queda su hueco dentro de la lámina (px).
 */
@Composable
internal fun PlaceSheetContent(
  place: PlaceSheetUi,
  nubiAspect: Float,
  maxHeight: Dp,
  onClose: () -> Unit,
  onPlay: (String) -> Unit,
  onGame: (String) -> Unit,
  modifier: Modifier = Modifier,
  globe: () -> Float = { 1f },
  globeMoves: Boolean = false,
  onHole: (Offset) -> Unit = {}
) {
  val titleFocus = remember { FocusRequester() }
  LaunchedEffect(place.key) { runCatching { titleFocus.requestFocus() } }
  val density = LocalDensity.current
  val color = placeColor(place.area)
  val shape = RoundedCornerShape(topStart = 30.dp, topEnd = 30.dp)
  val maxPx = with(density) { maxHeight.roundToPx() }

  // Cuánto cede: se mide el alto NATURAL del contenido (sin tope) y, si pasa del máximo, se sube un nivel hasta que cabe.
  var level by remember(place.key, density.fontScale, maxPx) { mutableIntStateOf(PlaceLevels.FULL) }
  var natural by remember(place.key, density.fontScale, maxPx) { mutableIntStateOf(0) }
  var measuredLevel by remember(place.key, density.fontScale, maxPx) { mutableIntStateOf(-1) }       // con qué nivel se midió `natural`: así no se sube dos veces por una medida vieja
  LaunchedEffect(natural, measuredLevel, level, maxPx) { if (measuredLevel == level && natural > maxPx && level < PlaceLevels.SCROLL) level += 1 }
  val scrollMode = level >= PlaceLevels.SCROLL

  var sheetCoords by remember { mutableStateOf<androidx.compose.ui.layout.LayoutCoordinates?>(null) }

  @Composable
  fun Hole() {
    Spacer(
      Modifier
        .size(PLACE_NUBI_WIDTH, PLACE_NUBI_WIDTH * nubiAspect)
        .onGloballyPositioned { c -> sheetCoords?.takeIf { it.isAttached }?.let { sheet -> onHole(c.positionInRoot() - sheet.positionInRoot()) } }
        .testTag("place_nubi_hole")
    )
  }

  Box(
    modifier
      .fillMaxWidth()
      .then(if (scrollMode) Modifier.height(maxHeight) else Modifier.heightIn(max = maxHeight))
      .drawBehind { drawRoundRect(Color(0x59211C45), topLeft = Offset(0f, -4.dp.toPx()), size = size, cornerRadius = androidx.compose.ui.geometry.CornerRadius(30.dp.toPx())) }
      .clip(shape)
      .background(SheetCream, shape)
      .border(3.dp, Clay.Ink, shape)
      .pointerInput(Unit) { detectTapGestures { } }       // los toques sobre la lámina no llegan al velo
      .onGloballyPositioned { sheetCoords = it }
      .semantics { paneTitle = place.name }
      .testTag("place_sheet")
  ) {
    Column(
      Modifier
        .fillMaxWidth()
        .then(if (scrollMode) Modifier.fillMaxHeight() else Modifier.wrapContentHeight(align = Alignment.Top, unbounded = true))
        .onGloballyPositioned { natural = it.size.height; measuredLevel = level }
        .padding(horizontal = 18.dp)
    ) {
      Spacer(Modifier.height(14.dp))
      PlaceHeader(place, color, titleFocus, onClose)

      // Con el rótulo ya cedido (los niveles de abajo) todo se junta un poco más: así los 5 juegos de la Antena caben a 360 × 780 con letra 1,3 sin deslizar
      val compact = level >= PlaceLevels.NO_LABEL
      val middle: @Composable () -> Unit = {
        Spacer(Modifier.height(if (compact) 6.dp else 10.dp))
        if (level == PlaceLevels.FULL) {
          // Nubi S a la izquierda y su globo a la derecha, centrados entre sí a lo alto
          Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
            Hole()
            Spacer(Modifier.width(8.dp))
            PlaceGlobe(place.bubble, globe, globeMoves, Modifier.weight(1f))
          }
        }
        Spacer(Modifier.height(12.dp))
        // El avance en UNA línea; si cedió el globo, Nubi (muda) queda a la izquierda de ella
        Row(Modifier.fillMaxWidth().heightIn(min = if (level > PlaceLevels.FULL) PLACE_NUBI_WIDTH * nubiAspect else 0.dp), verticalAlignment = Alignment.CenterVertically) {
          if (level > PlaceLevels.FULL) { Hole(); Spacer(Modifier.width(8.dp)) }
          Text(
            if (place.measured) com.example.data.Skill.stageHeader(place.stageName.orEmpty(), place.points ?: 0) else "Aún sin medir",
            color = Clay.Ink, fontSize = 20.sp, fontWeight = FontWeight.Bold, modifier = Modifier.testTag("place_stage")
          )
        }
        Spacer(Modifier.height(6.dp))
        com.example.ui.components.StageScale(
          if (place.measured) place.fill else null, onCard = true, change = place.change, barTag = "place_bar", namesTag = "place_stages", showNames = level < PlaceLevels.NO_STAGE_NAMES
        )
        if (level < PlaceLevels.NO_LABEL) {
          Spacer(Modifier.height(12.dp))
          Text("Tus juegos aquí", color = SheetMuted, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.testTag("place_games_label"))
        }
        Spacer(Modifier.height(if (compact) 4.dp else 6.dp))
        PlaceGamesList(place, onGame, gap = if (compact) 4.dp else 6.dp)
      }
      if (scrollMode) {
        // Red de seguridad (letra 2,0 con 5 juegos en una pantalla chica): el medio se desliza, con un desvanecido al pie mientras haya más; el botón queda fijo
        val scroll = rememberScrollState()
        Box(Modifier.weight(1f)) {
          Column(Modifier.fillMaxSize().verticalScroll(scroll).testTag("place_scroll")) { middle() }
          if (scroll.canScrollForward) Box(Modifier.align(Alignment.BottomCenter).fillMaxWidth().height(24.dp).background(androidx.compose.ui.graphics.Brush.verticalGradient(listOf(Color.Transparent, SheetCream))).testTag("place_fade"))
        }
      } else {
        middle()
      }

      // Pie fijo: «Jugar <el que sugiere Nubi>» con su emblema, sobre la barra del teléfono (12 dp de aire + la barra de navegación).
      val chosen = place.games.firstOrNull { it.gameId == place.chosenGameId }
      Column(Modifier.fillMaxWidth().navigationBarsPadding().padding(top = 12.dp, bottom = 12.dp), horizontalAlignment = Alignment.CenterHorizontally) {
        ClayButton(
          text = "Jugar " + (chosen?.title ?: place.article),
          onClick = { place.chosenGameId?.let(onPlay) },
          glyph = if (chosen == null) Glyph.PLAY else null,
          leading = chosen?.let { g -> { com.example.ui.components.GameEmblem(g.gameId, 32.dp) } },
          fontSize = 19.sp,
          modifier = Modifier.testTag("place_play")
        )
      }
    }
  }
}

/**
 * El globo de Nubi dentro de la lámina (Tarea 147): la losa crema con borde de tinta y su cola hacia Nubi; aparece (fundido y escala de 0,8 a 1) cuando Nubi aterriza. Dice lo que cambió esta semana y lo que falta (`PlaceSheets.build`), en primera persona.
 * [progress] = 0..1 (se lee al dibujar: no recompone cuadro a cuadro).
 */
@Composable
private fun PlaceGlobe(text: String, progress: () -> Float, moves: Boolean, modifier: Modifier = Modifier) {
  Box(modifier.padding(bottom = 8.dp)) {
    Box(
      Modifier
        .fillMaxWidth()
        .graphicsLayer {
          val g = progress()
          alpha = g
          if (moves) { val sc = 0.8f + 0.2f * g; scaleX = sc; scaleY = sc; transformOrigin = TransformOrigin(0f, 1f) }
        }
        .semantics { liveRegion = LiveRegionMode.Polite }
        .testTag("place_globe")
        .background(GlobeFill, RoundedCornerShape(22.dp))
        .border(3.dp, Clay.Ink, RoundedCornerShape(22.dp))
        .padding(horizontal = 14.dp, vertical = 10.dp)
    ) {
      Text(text, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Medium, lineHeight = 21.sp)
    }
    com.example.ui.components.BubbleTail(left = true, Modifier.align(Alignment.BottomStart).offset(x = 26.dp, y = 2.dp).graphicsLayer { alpha = progress() })
  }
}

/** El tamaño (en sp, de 24 a 20) con el que [text] cabe en una línea de [availablePx]; null si ni a 20 sp cabe. */
@Composable
private fun rememberTitleSize(text: String, availablePx: Float): Int? {
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  return remember(text, availablePx, base) {
    (24 downTo 20).firstOrNull { sp ->
      measurer.measure(text, base.merge(androidx.compose.ui.text.TextStyle(fontSize = sp.sp, fontWeight = FontWeight.Bold)), softWrap = false, maxLines = 1).size.width <= availablePx
    }
  }
}

@Composable
private fun CloseButton(onClose: () -> Unit) {
  KitCloseButton(onClose, Modifier.testTag("place_close"))
}

/** El nombre del lugar y su área (con un punto del color del área, a 16 sp). */
@Composable
private fun PlaceTitle(place: PlaceSheetUi, color: Color, sizeSp: Int, titleFocus: FocusRequester, modifier: Modifier = Modifier) {
  Column(modifier) {
    Text(
      place.name, color = Clay.Ink, fontSize = sizeSp.sp, fontWeight = FontWeight.Bold, lineHeight = (sizeSp + 4).sp, maxLines = 1, softWrap = false,
      modifier = Modifier.focusRequester(titleFocus).focusTarget().semantics { heading() }.testTag("place_title")
    )
    Row(verticalAlignment = Alignment.CenterVertically) {
      Box(Modifier.size(10.dp).clip(CircleShape).background(color).border(2.dp, Clay.Ink, CircleShape))
      Spacer(Modifier.width(6.dp))
      Text(place.areaName, color = SheetMuted, fontSize = 16.sp, fontWeight = FontWeight.Bold, modifier = Modifier.testTag("place_area"))
    }
  }
}

/**
 * El encabezado: el nombre (de 24 sp, o más chico hasta 20 si no cabe junto a la X) con su área debajo y, a la derecha, la X arriba. Si ni a 20 sp cabe (letra muy grande), la X queda sola en la primera línea y el nombre baja a una línea propia con todo el ancho:
 * nunca se parte a media palabra.
 */
@Composable
private fun PlaceHeader(place: PlaceSheetUi, color: Color, titleFocus: FocusRequester, onClose: () -> Unit) {
  BoxWithConstraints(Modifier.fillMaxWidth()) {
    val density = LocalDensity.current
    val availablePx = with(density) { (maxWidth - 48.dp - 8.dp).toPx() }
    val size = rememberTitleSize(place.name, availablePx)
    if (size != null) {
      Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.Top) {
        PlaceTitle(place, color, size, titleFocus, Modifier.weight(1f))
        CloseButton(onClose)
      }
    } else {
      Column(Modifier.fillMaxWidth()) {
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.End) { CloseButton(onClose) }
        Text(
          place.name, color = Clay.Ink, fontSize = 24.sp, fontWeight = FontWeight.Bold, lineHeight = 28.sp,
          modifier = Modifier.focusRequester(titleFocus).focusTarget().semantics { heading() }.testTag("place_title")
        )
        Row(verticalAlignment = Alignment.CenterVertically) {
          Box(Modifier.size(10.dp).clip(CircleShape).background(color).border(2.dp, Clay.Ink, CircleShape))
          Spacer(Modifier.width(6.dp))
          Text(place.areaName, color = SheetMuted, fontSize = 16.sp, fontWeight = FontWeight.Bold, modifier = Modifier.testTag("place_area"))
        }
      }
    }
  }
}

/**
 * «Tus juegos aquí» como FILAS compactas (Tarea 147; antes una cuadrícula de fichas, Tarea 143, que con 5 juegos y con letra grande no cabía en una vista): el mismo contenido que Ricardo eligió el 19-oct —emblema, nombre, cuándo se jugó y, en el que sugiere Nubi, el chip
 * «Nubi sugiere»; SIN cifras de medida— con otra disposición. Cada fila ([PlaceGameRowItem]) mide 60 dp (emblema de 48) y se toca entera para abrir la ficha del juego.
 */
@Composable
private fun PlaceGamesList(place: PlaceSheetUi, onGame: (String) -> Unit, gap: Dp = 6.dp) {
  Column(Modifier.fillMaxWidth().testTag("place_games_list"), verticalArrangement = Arrangement.spacedBy(gap)) {
    place.games.forEach { g -> PlaceGameRowItem(g, suggested = g.gameId == place.suggestedGameId, onClick = { onGame(g.gameId) }) }
  }
}

/**
 * Una fila de juego: losa crema del kit con su estado presionado; a la izquierda el emblema (48 dp, con el sello «jugado hoy»), a su derecha el nombre (16 sp Bold) y cuándo se jugó (16 sp); el sugerido por Nubi lleva el chip sol «Nubi sugiere» pegado al borde derecho.
 * Si la palabra más larga del nombre no cabe junto al chip (letra grande), el chip baja DENTRO de la columna de texto, bajo el «cuándo» (la fila crece). Se lee «Satélites, sin jugar aún, Nubi lo sugiere».
 */
@Composable
private fun PlaceGameRowItem(row: PlaceGameRow, suggested: Boolean, onClick: () -> Unit) {
  val density = LocalDensity.current
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  com.example.ui.components.ClaySlab(
    color = com.example.ui.components.SlabColor.CREAM,
    size = com.example.ui.components.SlabSize.SMALL,
    onClick = onClick,
    role = Role.Button,
    onClickLabel = "Ver su ficha",
    contentAlignment = Alignment.CenterStart,
    modifier = Modifier
      .fillMaxWidth()
      .heightIn(min = 60.dp)
      .semantics(mergeDescendants = true) { contentDescription = row.title + ", " + row.spoken + if (suggested) ", Nubi lo sugiere" else "" }
      .testTag("place_game_${row.gameId}")
  ) {
    BoxWithConstraints(Modifier.fillMaxWidth()) {
      val bold = base.merge(androidx.compose.ui.text.TextStyle(fontSize = 16.sp, fontWeight = FontWeight.Bold))
      val longestWordPx = row.title.split(' ').maxOfOrNull { measurer.measure(it, bold, softWrap = false, maxLines = 1).size.width } ?: 0
      val chipPx = measurer.measure(com.example.data.Suggestion.CHIP, bold, softWrap = false, maxLines = 1).size.width + with(density) { 20.dp.toPx() }
      val sidePx = with(density) { (20.dp + 48.dp + 10.dp + 8.dp).toPx() }                      // padding de la fila + emblema + aire + aire del chip
      val chipBeside = suggested && constraints.maxWidth - sidePx - chipPx >= longestWordPx
      Row(Modifier.fillMaxWidth().padding(horizontal = 10.dp, vertical = 6.dp), verticalAlignment = Alignment.CenterVertically) {
        com.example.ui.components.GameEmblem(row.gameId, 48.dp, done = row.daysAgo == 0)
        Spacer(Modifier.width(10.dp))
        Column(Modifier.weight(1f)) {
          Text(row.title, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 20.sp, modifier = Modifier.testTag("place_game_name_${row.gameId}"))
          Text(row.whenShort, color = SheetMuted, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.testTag("place_game_when_${row.gameId}"))
          if (suggested && !chipBeside) com.example.ui.theme.ClayPill(com.example.data.Suggestion.CHIP, Modifier.padding(top = 4.dp).testTag("place_game_chip"), color = Clay.Sun, compact = true)
        }
        if (suggested && chipBeside) {
          Spacer(Modifier.width(8.dp))
          com.example.ui.theme.ClayPill(com.example.data.Suggestion.CHIP, Modifier.testTag("place_game_chip"), color = Clay.Sun, compact = true)
        }
      }
    }
  }
}
