package com.example.ui.components

import androidx.activity.compose.BackHandler
import androidx.compose.animation.Crossfade
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.animateRectAsState
import androidx.compose.animation.core.snap
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawing
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.windowInsetsPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.RoundRect
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.PathFillType
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.input.pointer.PointerEventPass
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.Layout
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.isTraversalGroup
import androidx.compose.ui.semantics.liveRegion
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.traversalIndex
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.GuideStep
import com.example.data.HoyGuide
import com.example.ui.theme.AppFamily
import com.example.ui.theme.Clay
import com.example.viewmodel.NeuroVidaViewModel
import com.example.viewmodel.StartupStage
import kotlinx.coroutines.delay

/** El velo de la guía: el mismo del coach de los juegos de Unity (`#080A1C` al 72 %). */
internal val GuideVeil = Color(0xFF080A1C).copy(alpha = 0.72f)

/** El aire entre el elemento iluminado y el borde del hueco, y la curva de sus esquinas (el radio del kit). */
internal val GuideHolePadding = 8.dp
internal val GuideHoleRadius = 22.dp

/** El aro lima alrededor del hueco: el mismo que la persona ya conoce como «hoy» en la semana (forma, no solo color). */
private val GuideRingWidth = 2.5.dp

/** Qué elemento real ilumina cada paso. */
internal fun targetOf(step: GuideStep): GuideTarget = when (step) {
  GuideStep.CONTRARRELOJ -> GuideTarget.TIME_TRIAL
  GuideStep.TODOS -> GuideTarget.ALL_GAMES
  GuideStep.BARRA -> GuideTarget.TAB_BAR
}

/**
 * El hueco de un paso, en coordenadas de la raíz y SIN el aire de [GuideHolePadding]: el rectángulo del elemento; en el paso de la barra, solo sus dos tercios de la derecha (Avance y Tú; Hoy, la pestaña donde ya está la persona, queda bajo el velo).
 */
internal fun guideHoleOf(step: GuideStep, rect: Rect): Rect =
  if (step == GuideStep.BARRA) Rect(rect.left + rect.width / 3f, rect.top, rect.right, rect.bottom) else rect

/**
 * Quien aloja la guía (Tarea 193, `HoyGuide`): decide CUÁNDO sale (el día del vuelo, sin la marca, con Hoy a la vista sin nada encima y el arranque terminado, [HoyGuide.STEP_DELAY_MS] después del fundido del arranque) y dibuja el velo del paso en que va el ViewModel.
 * [somethingAbove] es verdadero si Hoy no está a la vista o hay algo encima (una medalla, un resultado, un panel, «Todos los juegos»): si llega algo mientras la guía está abierta, se esconde hasta que Hoy vuelva a estar libre.
 */
@Composable
internal fun HoyGuideHost(viewModel: NeuroVidaViewModel, targets: GuideTargets, somethingAbove: Boolean) {
  val step by viewModel.guideStep.collectAsState()
  val seen by viewModel.hoyGuideSeen.collectAsState()
  val stage by viewModel.startupStage.collectAsState()
  val flightDone by viewModel.firstFlightDone.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val due = step == null && HoyGuide.shouldShow(flightDone, seen, somethingAbove, stage == StartupStage.READY)
  // La espera cuenta desde que todo está libre; si en el camino llega una medalla (o algo) la espera se cancela y vuelve a empezar cuando se cierre.
  LaunchedEffect(due) {
    if (due) {
      delay(HoyGuide.STEP_DELAY_MS + NubiMotion.STATE_MS)
      viewModel.startGuide()
    }
  }
  // Avance muestra «un punto de partida» (las áreas con sus puntos) si hay al menos una partida de un juego que se ve; sin ninguna (el vuelo terminó con «Terminar después» en todo), el paso 3 habla de «cómo vas en cada área».
  val startingPoint = remember(history) { history.any { !com.example.model.GameRegistry.isRetired(it.gameId) } }
  step?.takeIf { !somethingAbove }?.let { HoyGuideOverlay(step = it, targets = targets, onNext = viewModel::guideNext, onSkip = viewModel::guideSkip, startingPoint = startingPoint) }
}

/**
 * La guía de Nubi sobre Hoy, un paso (Tarea 193; `auditoria-fable/guia-primer-ingreso.md` §3.3 y §3.4): un velo sobre TODA la pantalla (también la barra) con un hueco y su aro lima sobre el elemento real, y el globo de Nubi con el texto del paso, «Paso 1 de 3» y el chip lila «Seguir ›» (en el último,
 * «Listo»); arriba a la derecha, «Saltar la guía» en crema. Sin sol (el único sol de Hoy es el Contrarreloj iluminado en el paso 1), sin dedo, sin sonido.
 *
 * - **Tocar lo iluminado = Seguir** y NO abre nada (lanzar el Contrarreloj desde adentro de una guía sería empezar una partida de 2 minutos sin pedirla). **Tocar el velo no hace nada** (con 65+ un toque accidental fuera no debe cerrarla) y tampoco se puede deslizar nada de abajo. **Atrás = saltar.**
 * - El hueco se mueve al elemento nuevo en [NubiMotion.TAB_MS] y el texto cambia con un fundido de [NubiMotion.STATE_MS]; con «quitar animaciones», de golpe.
 * - TalkBack: lo de abajo queda fuera del lector (quien la aloja le pone `hiddenUntil`); el lector recorre solo el globo, el chip y «Saltar la guía», y el texto del globo se anuncia solo con el nombre y el lugar de lo iluminado ([HoyGuide.spoken]).
 * - Letra 1,3: Nubi pasa a S. Letra 2,0: el elemento se trae a la vista antes del paso y, si el texto no cabe, se desliza dentro del globo (nunca se corta, nunca tapa el hueco ni «Saltar la guía»).
 */
@Composable
internal fun HoyGuideOverlay(step: GuideStep, targets: GuideTargets, onNext: () -> Unit, onSkip: () -> Unit, modifier: Modifier = Modifier, startingPoint: Boolean = true) {
  val density = LocalDensity.current
  val reduce = rememberReduceMotion()
  val target = targetOf(step)
  BackHandler(onBack = onSkip)
  // El elemento se trae SIEMPRE a la vista, entero y sobre la barra (Hoy se desliza con letra 1,3 el día del vuelo y con 2,0), y recién cuando terminó de deslizar se dibuja el hueco: mientras tanto queda el del paso anterior.
  var settledStep by remember { mutableStateOf<GuideStep?>(null) }
  LaunchedEffect(step) { targets.bringIntoView(target); settledStep = step }
  // Aparece con un fundido de 200 ms (de golpe con «quitar animaciones»).
  val appear = remember { Animatable(if (reduce) 1f else 0f) }
  LaunchedEffect(Unit) { if (!reduce) appear.animateTo(1f, tween(NubiMotion.STATE_MS)) }

  val raw = targets.rects[target]?.takeIf { settledStep == step }?.let { guideHoleOf(step, it) }
  val pad = with(density) { GuideHolePadding.toPx() }
  // El aire de 8 dp va por los cuatro lados, salvo en la barra: abajo y a la derecha el hueco llega hasta donde llega la barra, para no entrar en la franja de gestos (o los 3 botones) de Android ni salirse de la pantalla.
  val liveHole = raw?.let { if (step == GuideStep.BARRA) Rect(it.left - pad, it.top - pad, it.right, it.bottom) else Rect(it.left - pad, it.top - pad, it.right + pad, it.bottom + pad) }
  var lastHole by remember { mutableStateOf<Rect?>(null) }
  LaunchedEffect(liveHole) { if (liveHole != null) lastHole = liveHole }
  val hole = liveHole ?: lastHole
  val animatedHole by animateRectAsState(hole ?: Rect.Zero, if (reduce) snap() else tween(NubiMotion.TAB_MS, easing = NubiMotion.Emphasized), label = "hueco")
  val shownHole = if (hole == null) null else animatedHole

  val topInset = WindowInsets.safeDrawing.getTop(density)
  val skipBand = with(density) { (8.dp + 48.dp + 8.dp).toPx() }
  val gap = with(density) { 12.dp.toPx() }
  val bigText = density.fontScale >= 1.3f

  Box(
    modifier
      .fillMaxSize()
      .graphicsLayer { alpha = appear.value }
      .testTag("guide_veil")
      .semantics { isTraversalGroup = true }
  ) {
    // El velo con su hueco: se queda con TODOS los toques (nada de abajo se toca ni se desliza).
    Canvas(Modifier.fillMaxSize().pointerInput(Unit) { awaitPointerEventScope { while (true) awaitPointerEvent(PointerEventPass.Initial).changes.forEach { it.consume() } } }.clearAndSetSemantics { }) {
      val whole = Rect(Offset.Zero, size)
      val radius = CornerRadius(GuideHoleRadius.toPx())
      val path = Path().apply {
        fillType = PathFillType.EvenOdd
        addRect(whole)
        if (shownHole != null) addRoundRect(RoundRect(shownHole, radius))
      }
      drawPath(path, GuideVeil)
      if (shownHole != null) drawRoundRect(Clay.Lime, topLeft = shownHole.topLeft, size = Size(shownHole.width, shownHole.height), cornerRadius = radius, style = Stroke(GuideRingWidth.toPx()))
    }
    // Tocar lo iluminado = «Seguir»: un toque sobre el hueco, sin abrir nada.
    if (shownHole != null) {
      val onNextNow by rememberUpdatedState(onNext)
      Box(
        Modifier
          .offset { IntOffset(shownHole.left.toInt(), shownHole.top.toInt()) }
          .size(with(density) { shownHole.width.toDp() }, with(density) { shownHole.height.toDp() })
          .testTag("guide_hole")
          .pointerInput(Unit) { detectTapGestures { onNextNow() } }
      )
    }
    // El globo de Nubi, siempre FUERA del hueco y debajo de «Saltar la guía».
    GuideAnchored(hole = shownHole, topLimit = topInset + skipBand.toInt(), gap = gap, modifier = Modifier.fillMaxSize()) {
      Row(Modifier.fillMaxWidth().padding(horizontal = 12.dp), verticalAlignment = Alignment.Bottom) {
        NubiClay(if (bigText) NubiSize.S else NubiSize.M, Modifier.testTag("guide_nubi").clearAndSetSemantics { })
        Box(Modifier.weight(1f).padding(start = 4.dp)) { GuideGlobe(step, onNext, startingPoint) }
      }
    }
    // «Saltar la guía»: arriba a la derecha, en los tres pasos.
    Box(Modifier.align(Alignment.TopEnd).windowInsetsPadding(WindowInsets.safeDrawing).padding(top = 8.dp, end = 12.dp)) {
      KitButton(
        text = HoyGuide.SKIP_LABEL, onClick = onSkip, color = SlabColor.CREAM, wide = false, minHeight = 48.dp, fontSize = 16.sp,
        modifier = Modifier.semantics { traversalIndex = 2f }.testTag("btn_guide_skip")
      )
    }
  }
}

/** El globo: una losa crema con el texto de Nubi a 18 sp, «Paso 1 de 3» y el chip lila. Lo que dice TalkBack es [HoyGuide.spoken] (con el lugar de lo iluminado), no el texto suelto. */
@Composable
private fun GuideGlobe(step: GuideStep, onNext: () -> Unit, startingPoint: Boolean) {
  val reduce = rememberReduceMotion()
  ClaySlab(SlabColor.CREAM, Modifier.fillMaxWidth().testTag("guide_globe_slab"), size = SlabSize.MEDIUM, onClick = null, role = null) {
    Column(Modifier.padding(start = 16.dp, end = 12.dp, top = 12.dp, bottom = 8.dp)) {
      // Si el texto no cabe (letra 2,0), se desliza DENTRO del globo.
      Column(Modifier.weight(1f, fill = false).verticalScroll(rememberScrollState())) {
        Crossfade(
          targetState = step, animationSpec = if (reduce) snap() else tween(NubiMotion.STATE_MS),
          modifier = Modifier
            .testTag("guide_globe")
            .semantics { traversalIndex = 0f }
            .clearAndSetSemantics { contentDescription = HoyGuide.spoken(step, startingPoint); liveRegion = LiveRegionMode.Polite },
          label = "texto de la guía"
        ) { s -> Text(HoyGuide.text(s, startingPoint), color = Clay.Ink, fontSize = 18.sp, lineHeight = 25.sp, fontWeight = FontWeight.SemiBold, fontFamily = AppFamily) }
      }
      val counter: @Composable (Modifier) -> Unit = { m -> Text(HoyGuide.counter(step), color = Clay.Ink, fontSize = 16.sp, lineHeight = 21.sp, fontWeight = FontWeight.Normal, modifier = m.testTag("guide_counter").clearAndSetSemantics { }) }
      val chip: @Composable () -> Unit = {
        KitButton(
          text = HoyGuide.chipLabel(step), onClick = onNext, color = SlabColor.LILAC, wide = false, minHeight = 48.dp, fontSize = 17.sp,
          modifier = Modifier.semantics { contentDescription = HoyGuide.chipSpoken(step); traversalIndex = 1f }.testTag("btn_guide_next")
        )
      }
      if (LocalDensity.current.fontScale >= 1.5f) {
        // con letra muy grande el contador y el chip no caben en una fila: uno bajo el otro (el contador nunca se parte)
        Column(Modifier.fillMaxWidth().padding(top = 6.dp)) { counter(Modifier); Box(Modifier.align(Alignment.End)) { chip() } }
      } else {
        Row(Modifier.fillMaxWidth().padding(top = 6.dp), verticalAlignment = Alignment.CenterVertically) { counter(Modifier.weight(1f)); chip() }
      }
    }
  }
}

/**
 * Coloca el globo de Nubi FUERA del hueco, en la zona oscura más cercana: encima si cabe y, si no, debajo. Nunca sube de [topLimit] (queda debajo de «Saltar la guía»); si el lado elegido es más bajo que el globo, el globo se limita a ese alto y su texto se desliza adentro.
 */
@Composable
private fun GuideAnchored(hole: Rect?, topLimit: Int, gap: Float, modifier: Modifier, content: @Composable () -> Unit) {
  Layout(content = content, modifier = modifier) { measurables, constraints ->
    val width = constraints.maxWidth
    val height = constraints.maxHeight
    val spaceAbove = if (hole == null) 0f else hole.top - gap - topLimit
    val spaceBelow = if (hole == null) 0f else height - (hole.bottom + gap)
    val room = (if (hole == null) (height - topLimit).toFloat() else maxOf(spaceAbove, spaceBelow)).coerceAtLeast(0f).toInt()
    val placeable = measurables.first().measure(constraints.copy(minWidth = 0, minHeight = 0, maxHeight = room))
    val y = when {
      hole == null -> (height - placeable.height) / 2
      placeable.height <= spaceAbove -> (hole.top - gap).toInt() - placeable.height
      else -> (hole.bottom + gap).toInt()
    }.coerceIn(topLimit, (height - placeable.height).coerceAtLeast(topLimit))
    layout(width, height) { placeable.place(0, y) }
  }
}
