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
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.Stable
import androidx.compose.runtime.getValue
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
private val SheetTrack = Color(0xFFE2D6C2)
private val SheetFill = Color(0xFF8E7BD6)
private val SheetNow = Color(0xFFE4DCF7)
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
 * Lo que va ENCIMA de Hoy mientras la lámina está abierta o abriéndose: el velo, la lámina, Nubi volando (o posada) y su globo. Va dentro de la caja de Hoy (no es un `Dialog`), que ocupa toda la pestaña: con la lámina abierta la barra de pestañas se retira y esa
 * caja llega hasta la barra del teléfono, así que el botón «Jugar» queda sobre ella. [top] = dónde empieza la lámina (px desde arriba de la caja); [island] = la caja de la isla en esta misma caja (px).
 */
@Composable
internal fun PlaceLayer(
  fx: PlaceFx,
  place: PlaceSheetUi?,
  layout: IslaLayout,
  island: Rect,
  top: Float,
  onClose: () -> Unit,
  onPlay: (String) -> Unit,
  onGame: (String) -> Unit,
  modifier: Modifier = Modifier
) {
  if (!fx.active || place == null) return
  val density = LocalDensity.current
  // Nubi posada: 0,8 de su tamaño en la isla, a 12 dp del borde izquierdo y asomando 0,26 de su alto por encima de la lámina (como en el boceto).
  val rest = nubiRest(island, layout)
  val perchW = rest.width * 0.8f
  val perchH = rest.height * 0.8f
  val perch = Rect(with(density) { 12.dp.toPx() }, top - perchH * 0.26f, with(density) { 12.dp.toPx() } + perchW, top - perchH * 0.26f + perchH)
  val headerStart = with(density) { perch.right.toDp() } + 6.dp                    // el nombre empieza a la derecha de Nubi

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

    // La lámina sube desde abajo (con «quitar animaciones» aparece en su sitio con un fundido).
    BoxWithConstraints(Modifier.fillMaxSize()) {
      val pageHeight = maxHeight
      val sheetHeight = pageHeight - with(density) { top.toDp() }
      Box(
        Modifier
          .align(Alignment.BottomCenter)
          .fillMaxWidth()
          .height(sheetHeight.coerceAtLeast(0.dp))
          .graphicsLayer {
            val p = fx.sheet.value
            if (fx.moves) translationY = (1f - p) * size.height else alpha = p
          }
      ) {
        PlaceSheetContent(
          place, headerStart = headerStart, nubiDrop = with(density) { (perchH * 0.74f).toDp() }, onClose = onClose, onPlay = onPlay, onGame = onGame,
          footerBottom = pageFooterBottom(pageHeight)
        )
      }
    }

    // Nubi: en vuelo (de la isla a la lámina o de vuelta) o ya posada. Mientras no vuela, la dibuja la isla.
    NubiFlight(fx, layout, island, perch)

    // Su globo, a la derecha de Nubi y apoyado sobre la lámina.
    PlaceGlobe(fx, place.bubble, perch, top)
  }
}

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

/** El globo de Nubi: aparece (320 ms) cuando aterriza y dice dónde estás y qué hay en la lámina. */
@Composable
private fun PlaceGlobe(fx: PlaceFx, text: String, perch: Rect, top: Float) {
  val density = LocalDensity.current
  val gap = with(density) { 6.dp.roundToPx() }
  val lift = with(density) { 7.dp.roundToPx() }
  val edge = with(density) { 14.dp.roundToPx() }
  val minW = with(density) { 120.dp.roundToPx() }
  val maxGlobe = with(density) { 211.dp.roundToPx() }
  androidx.compose.ui.layout.Layout(
    content = {
      Box(
        Modifier
          .graphicsLayer {
            val g = fx.globe.value
            alpha = g
            if (fx.moves) { val sc = 0.8f + 0.2f * g; scaleX = sc; scaleY = sc; transformOrigin = TransformOrigin(0f, 1f) }
          }
          .semantics { liveRegion = LiveRegionMode.Polite }
          .testTag("place_globe")
          .background(GlobeFill, RoundedCornerShape(18.dp))
          .border(3.dp, Clay.Ink, RoundedCornerShape(18.dp))
          .padding(horizontal = 14.dp, vertical = 10.dp)
      ) {
        Text(text, color = Clay.Ink, fontSize = 15.sp, fontWeight = FontWeight.Medium, lineHeight = 20.sp)
      }
    },
    modifier = Modifier.fillMaxSize()
  ) { measurables, constraints ->
    val left = perch.right.toInt() + gap
    val maxW = min(maxGlobe, (constraints.maxWidth - left - edge).coerceAtLeast(minW))
    val p = measurables[0].measure(androidx.compose.ui.unit.Constraints(0, maxW, 0, androidx.compose.ui.unit.Constraints.Infinity))
    // si el globo no cabe entre el borde de arriba y la lámina (letra grande), no se pone: lo que dice ya está en la lámina
    val y = (top - p.height - lift).toInt()
    layout(constraints.maxWidth, constraints.maxHeight) { if (y >= edge / 2) p.place(left, y) else p.place(-100_000, -100_000) }
  }
}

/**
 * La lámina de plasticina: arriba el lugar (con espacio a la izquierda para Nubi) y la X; en el medio, que se desliza si no cabe, el AVANCE del área (etapa, barra de cinco etapas, cómo cambió, las últimas 4 semanas) y sus juegos; abajo, FIJO, el botón
 * «Jugar en …» con lo que eligió Nubi. [headerStart] = lo que ocupa Nubi a la izquierda del encabezado y [nubiDrop] cuánto baja Nubi por debajo del borde de la lámina (si el nombre no cabe
 * entre Nubi y la X, ni achicándolo hasta 20 sp, pasa a una línea propia debajo de Nubi).
 */
@Composable
internal fun PlaceSheetContent(place: PlaceSheetUi, headerStart: Dp, nubiDrop: Dp, onClose: () -> Unit, onPlay: (String) -> Unit, onGame: (String) -> Unit, modifier: Modifier = Modifier, footerBottom: Dp = 48.dp) {
  val titleFocus = remember { FocusRequester() }
  LaunchedEffect(place.key) { runCatching { titleFocus.requestFocus() } }
  val color = placeColor(place.area)
  val shape = RoundedCornerShape(topStart = 30.dp, topEnd = 30.dp)
  Column(
    modifier
      .fillMaxSize()
      .drawBehind { drawRoundRect(Color(0x59211C45), topLeft = Offset(0f, -4.dp.toPx()), size = size, cornerRadius = androidx.compose.ui.geometry.CornerRadius(30.dp.toPx())) }
      .clip(shape)
      .background(SheetCream, shape)
      .border(3.dp, Clay.Ink, shape)
      .pointerInput(Unit) { detectTapGestures { } }       // los toques sobre la lámina no llegan al velo
      .semantics { paneTitle = place.name }
      .testTag("place_sheet")
  ) {
    // Encabezado fijo: el lugar, su área y la X.
    PlaceHeader(place, color, headerStart, nubiDrop, titleFocus, onClose)

    // Lo que se desliza: tu avance y tus juegos.
    Column(Modifier.weight(1f).verticalScroll(rememberScrollState()).padding(horizontal = 18.dp, vertical = 8.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
      Text("Tu avance aquí", color = SheetMuted, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(top = 4.dp))
      if (place.measured) {
        Row(verticalAlignment = Alignment.Bottom, horizontalArrangement = Arrangement.spacedBy(10.dp)) {
          Text(place.stageName.orEmpty(), color = Clay.Ink, fontSize = 30.sp, fontWeight = FontWeight.Bold, modifier = Modifier.testTag("place_stage"))
          Text("${place.points} de 100", color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(bottom = 4.dp).testTag("place_points"))
        }
      } else {
        Text("Aún sin medir", color = Clay.Ink, fontSize = 26.sp, fontWeight = FontWeight.Bold, modifier = Modifier.testTag("place_stage"))
      }
      StageBar(place)
      Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
        Text(place.changeLine, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, modifier = Modifier.testTag("place_change"))
        place.toNextLine?.let { Text(it, color = SheetMuted, fontSize = 15.sp, modifier = Modifier.testTag("place_next")) }
      }
      WeeksRow(place)
      Box(Modifier.fillMaxWidth().height(2.dp).background(Color(0x26211C45)))
      Text("Tus juegos aquí", color = SheetMuted, fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
      Column {
        place.games.forEach { g -> GameRow(g, color, onClick = { onGame(g.gameId) }) }
      }
    }

    // Pie fijo: Jugar, sobre la barra del teléfono, y lo que eligió Nubi. [footerBottom] lo deja por encima de la franja donde la caja de Hoy se desvanece (`pageFooterBottom`): la leyenda se lee entera.
    Column(Modifier.fillMaxWidth().navigationBarsPadding().padding(start = 18.dp, end = 18.dp, top = 6.dp, bottom = footerBottom), horizontalAlignment = Alignment.CenterHorizontally) {
      ClayButton(
        text = "Jugar en ${place.article}",
        onClick = { place.chosenGameId?.let(onPlay) },
        glyph = Glyph.PLAY, fontSize = 19.sp,
        modifier = Modifier.testTag("place_play")
      )
      place.chosenLine?.let { Text(it, color = SheetMuted, fontSize = 16.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 6.dp).testTag("place_chosen")) }
    }
  }
}

/** Margen de abajo del pie de la lámina: la franja que se desvanece (`PAGE_BOTTOM_FADE` del alto de la página) más 12 dp de aire, y nunca menos de 32. */
internal fun pageFooterBottom(@Suppress("UNUSED_PARAMETER") pageHeight: Dp): Dp = PAGE_FOOTER_BOTTOM

/** El aire de abajo de la lámina de un lugar: la lámina cubre la barra de pestañas (capa fija) y termina sobre la barra del sistema; ya no hay franja que se desvanezca (Tarea 123). */
internal val PAGE_FOOTER_BOTTOM = 32.dp

/** El tamaño (en sp, de 26 a 20) con el que [text] cabe en una línea de [availablePx]; null si ni a 20 sp cabe. */
@Composable
private fun rememberTitleSize(text: String, availablePx: Float): Int? {
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  return remember(text, availablePx, base) {
    (26 downTo 20).firstOrNull { sp ->
      measurer.measure(text, base.merge(androidx.compose.ui.text.TextStyle(fontSize = sp.sp, fontWeight = FontWeight.Bold)), softWrap = false, maxLines = 1).size.width <= availablePx
    }
  }
}

@Composable
private fun CloseButton(onClose: () -> Unit) {
  KitCloseButton(onClose, Modifier.testTag("place_close"))
}

/** El nombre del lugar y su área (con un punto del color del área). */
@Composable
private fun PlaceTitle(place: PlaceSheetUi, color: Color, sizeSp: Int, titleFocus: FocusRequester, modifier: Modifier = Modifier) {
  Column(modifier) {
    Text(
      place.name, color = Clay.Ink, fontSize = sizeSp.sp, fontWeight = FontWeight.Bold, lineHeight = (sizeSp + 3).sp, maxLines = 1, softWrap = false,
      modifier = Modifier.focusRequester(titleFocus).focusTarget().semantics { heading() }.testTag("place_title")
    )
    Row(verticalAlignment = Alignment.CenterVertically) {
      Box(Modifier.size(10.dp).clip(CircleShape).background(color).border(2.dp, Clay.Ink, CircleShape))
      Spacer(Modifier.width(6.dp))
      Text(place.areaName, color = SheetMuted, fontSize = 15.sp, fontWeight = FontWeight.Bold, modifier = Modifier.testTag("place_area"))
    }
  }
}

/**
 * El encabezado: a la derecha de Nubi el nombre (de 26 sp, o más chico hasta 20 si no cabe entre Nubi y la X) y, a la derecha, la X. Si ni a 20 sp cabe (pantalla chica o letra grande), la X queda en la primera línea y el nombre baja a una línea propia,
 * debajo de Nubi, con todo el ancho: nunca se parte a media palabra.
 */
@Composable
private fun PlaceHeader(place: PlaceSheetUi, color: Color, headerStart: Dp, nubiDrop: Dp, titleFocus: FocusRequester, onClose: () -> Unit) {
  BoxWithConstraints(Modifier.fillMaxWidth()) {
    val density = LocalDensity.current
    val availablePx = with(density) { (maxWidth - headerStart.coerceAtLeast(18.dp) - 18.dp - 48.dp - 8.dp).toPx() }
    val size = rememberTitleSize(place.name, availablePx)
    if (size != null) {
      Row(Modifier.fillMaxWidth().padding(start = headerStart.coerceAtLeast(18.dp), end = 18.dp, top = 20.dp), verticalAlignment = Alignment.CenterVertically) {
        PlaceTitle(place, color, size, titleFocus, Modifier.weight(1f))
        CloseButton(onClose)
      }
    } else {
      Column(Modifier.fillMaxWidth().padding(start = 18.dp, end = 18.dp, top = 20.dp)) {
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.End) { CloseButton(onClose) }
        // debajo de Nubi, que asoma [nubiDrop] por debajo del borde de la lámina
        Spacer(Modifier.height((nubiDrop - 68.dp).coerceAtLeast(0.dp) + 4.dp))
        Text(
          place.name, color = Clay.Ink, fontSize = 26.sp, fontWeight = FontWeight.Bold, lineHeight = 29.sp,
          modifier = Modifier.focusRequester(titleFocus).focusTarget().semantics { heading() }.testTag("place_title")
        )
        Row(verticalAlignment = Alignment.CenterVertically) {
          Box(Modifier.size(10.dp).clip(CircleShape).background(color).border(2.dp, Clay.Ink, CircleShape))
          Spacer(Modifier.width(6.dp))
          Text(place.areaName, color = SheetMuted, fontSize = 15.sp, fontWeight = FontWeight.Bold, modifier = Modifier.testTag("place_area"))
        }
      }
    }
  }
}

/** La barra de 5 tramos con las etapas nombradas debajo (la tuya, en negrita). Con letra grande los nombres no caben en cinco columnas y pasan a una línea. */
@Composable
private fun StageBar(place: PlaceSheetUi) {
  Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
    Box(
      Modifier.fillMaxWidth().height(16.dp).clip(RoundedCornerShape(50)).background(SheetTrack).border(2.dp, Clay.Ink, RoundedCornerShape(50)).testTag("place_bar")
    ) {
      Box(Modifier.fillMaxWidth(place.fill.coerceIn(0f, 1f)).fillMaxSize().background(SheetFill))
      Row(Modifier.fillMaxSize()) {
        repeat(5) { i -> Box(Modifier.weight(1f).fillMaxSize().then(if (i < 4) Modifier.drawBehind { drawLine(Color(0x59211C45), Offset(size.width, 0f), Offset(size.width, size.height), 2.dp.toPx()) } else Modifier)) }
      }
    }
    if (LocalDensity.current.fontScale > 1.15f) {
      Text(
        "Etapas: " + Skill.STAGES.joinToString(" · ") + (place.stageName?.let { ". Vas en $it" } ?: ""), color = SheetMuted, fontSize = 14.sp,      // la escala de las 5 etapas: rótulo de gráfico, 14 sp
        modifier = Modifier.testTag("place_stages")
      )
    } else {
      Row(Modifier.fillMaxWidth().testTag("place_stages")) {
        Skill.STAGES.forEachIndexed { i, name ->
          val here = place.stageIndex == i
          Text(name, color = if (here) Clay.Ink else SheetMuted, fontSize = 14.sp, fontWeight = if (here) FontWeight.ExtraBold else FontWeight.Normal, textAlign = TextAlign.Center, maxLines = 1, modifier = Modifier.weight(1f))
        }
      }
    }
  }
}

/** Las últimas 4 semanas: cuatro casillas con el número y su rótulo; la de esta semana, destacada. Sin dato, «—». Con letra grande, dos y dos. */
@Composable
private fun WeeksRow(place: PlaceSheetUi) {
  @Composable
  fun Cell(i: Int, modifier: Modifier) {
    val now = i == 3
    Column(
      modifier
        .clip(RoundedCornerShape(14.dp))
        .background(if (now) SheetNow else Color.Transparent)
        .border(2.dp, if (now) Clay.Ink else Color(0x40211C45), RoundedCornerShape(14.dp))
        .padding(horizontal = 1.dp, vertical = 6.dp)
        .testTag("place_week_$i"),
      horizontalAlignment = Alignment.CenterHorizontally
    ) {
      Text(place.weeks.getOrNull(i)?.toString() ?: "—", color = Clay.Ink, fontSize = 19.sp, fontWeight = FontWeight.Bold)
      Text(PlaceSheets.WEEK_LABELS[i], color = SheetMuted, fontSize = 16.sp, textAlign = TextAlign.Center, fontWeight = if (now) FontWeight.Bold else FontWeight.Normal)
    }
  }
  if (LocalDensity.current.fontScale > 1.15f) {
    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
      for (r in 0..1) Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) { for (c in 0..1) Cell(r * 2 + c, Modifier.weight(1f)) }
    }
  } else {
    Row(Modifier.fillMaxWidth().height(IntrinsicSize.Max), horizontalArrangement = Arrangement.spacedBy(6.dp)) { for (i in 0..3) Cell(i, Modifier.weight(1f).fillMaxHeight()) }
  }
}

/** Una fila de «Tus juegos aquí»: el punto del área, el nombre del juego y su dato; tocarla abre la FICHA de ese juego encima de la lámina (solo el botón grande «Jugar en…» juega directo; Tarea 87). */
@Composable
private fun GameRow(row: PlaceGameRow, color: Color, onClick: () -> Unit) {
  Row(
    Modifier
      .fillMaxWidth()
      .heightIn(min = 44.dp)
      .clip(RoundedCornerShape(12.dp))
      .clickable(onClickLabel = "Ver su ficha", role = Role.Button, onClick = onClick)
      .semantics(mergeDescendants = true) { contentDescription = "${row.title}, ${row.detail}" }
      .padding(vertical = 4.dp)
      .testTag("place_game_${row.gameId}"),
    verticalAlignment = Alignment.CenterVertically,
    horizontalArrangement = Arrangement.spacedBy(10.dp)
  ) {
    Box(Modifier.size(12.dp).clip(CircleShape).background(color).border(2.dp, Clay.Ink, CircleShape))
    Text(row.title, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, modifier = Modifier.weight(1f))
    Text(row.detail, color = SheetMuted, fontSize = 16.sp, textAlign = TextAlign.End, modifier = Modifier.widthIn(max = 150.dp))
  }
}
