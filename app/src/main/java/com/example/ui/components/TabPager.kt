package com.example.ui.components

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.AnimationVector1D
import androidx.compose.animation.core.FastOutLinearInEasing
import androidx.compose.animation.core.LinearOutSlowInEasing
import androidx.compose.animation.core.tween
import androidx.compose.foundation.interaction.DragInteraction
import androidx.compose.foundation.pager.PagerState
import androidx.compose.foundation.pager.rememberPagerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.MutableIntState
import androidx.compose.runtime.Stable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.runtime.snapshotFlow
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.graphicsLayer
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.launch
import com.example.viewmodel.AppTab
import kotlin.math.abs

/**
 * El pager de las tres pestañas (Hoy, Avance, Tú) con su fundido, sincronizado con la pestaña que guarda el ViewModel (Tarea 142, 19-oct; Tarea 184, 27-oct). Lo arma [rememberTabPager].
 * [state] es el estado del `HorizontalPager`; [selectedTab] es lo que marca la barra; [tabFade] es el modificador del fundido, que va en el `HorizontalPager`.
 */
@Stable
class TabPager internal constructor(
  val state: PagerState,
  private val pending: MutableIntState,
  private val alpha: Animatable<Float, AnimationVector1D>,
  private val scale: Animatable<Float, AnimationVector1D>
) {
  /**
   * La pestaña que marca la barra EN VIVO (Tarea 142: Ricardo notaba «lag» cuando los íconos cambiaban recién después de que terminaba el movimiento). Si el código pidió un movimiento (se tocó una pestaña), es el DESTINO desde el primer
   * cuadro, también en el fundido de Hoy ↔ Tú, donde el pager todavía no se movió; si no, es `state.targetPage`: al arrastrar cambia en cuanto el gesto decide (pasado el umbral de posición del pager, unos 56 dp, sin soltar el dedo) y se deshace
   * si se suelta antes y vuelve.
   */
  val selectedTab: AppTab
    get() { val p = pending.intValue; return AppTab.entries[if (p >= 0) p else state.targetPage.coerceIn(0, AppTab.entries.lastIndex)] }

  /** La opacidad de las páginas (1 = normal; baja en el fundido de Hoy ↔ Tú). */
  val contentAlpha: Float get() = alpha.value
  /** La escala de las páginas (1 = normal; entra desde [NubiMotion.TAB_FADE_MIN_SCALE] en el fundido de Hoy ↔ Tú). */
  val contentScale: Float get() = scale.value

  internal fun setPending(page: Int) { pending.intValue = page }
  internal fun clearPending() { pending.intValue = -1 }
  internal suspend fun fadeThrough(target: Int) {
    scale.snapTo(1f)
    alpha.animateTo(0f, tween(NubiMotion.TAB_FADE_OUT_MS, easing = FastOutLinearInEasing))         // sale la página de ahora (acelera)…
    state.scrollToPage(target)                                                                      // …en lo más transparente se cambia de página, de golpe: nunca se ve la del medio…
    scale.snapTo(NubiMotion.TAB_FADE_MIN_SCALE)
    coroutineScope {                                                                                  // …y entra la nueva con un fundido y una escala mínima (frena)
      launch { scale.animateTo(1f, tween(NubiMotion.TAB_FADE_IN_MS, easing = LinearOutSlowInEasing)) }
      alpha.animateTo(1f, tween(NubiMotion.TAB_FADE_IN_MS, easing = LinearOutSlowInEasing))
    }
  }
  /** Devuelve las páginas a su opacidad y escala normales (un fundido a medias que otro toque o el dedo interrumpió): rápido, para que no queden a medias. */
  internal suspend fun settleFade() {
    if (alpha.value == 1f && scale.value == 1f) return
    coroutineScope {
      launch { scale.animateTo(1f, tween(NubiMotion.STATE_FAST_MS)) }
      alpha.animateTo(1f, tween(NubiMotion.STATE_FAST_MS))
    }
  }
  internal suspend fun snapFade() { alpha.snapTo(1f); scale.snapTo(1f) }
}

/** El fundido de las páginas de [tabPager]: opacidad y escala, leídas al dibujar (no recomponen nada). Va en el `HorizontalPager`. */
fun Modifier.tabFade(tabPager: TabPager): Modifier = graphicsLayer {
  val s = tabPager.contentScale
  alpha = tabPager.contentAlpha; scaleX = s; scaleY = s
}

/**
 * El pager de las tres pestañas, sincronizado con la pestaña que guarda el ViewModel.
 * - Si [currentTab] cambia por fuera (se tocó una pestaña de la barra, se volvió de un juego…), el pager va hasta ella:
 *   - a una pestaña VECINA, se desliza (300 ms con la curva enfatizada de `NubiMotion`);
 *   - a una que NO es vecina (Hoy ↔ Tú), un «fade through» de Material en vez de deslizar: Avance, que queda en el medio, no se ve pasar (la página de ahora se desvanece en [NubiMotion.TAB_FADE_OUT_MS] ms y entra la nueva en
 *     [NubiMotion.TAB_FADE_IN_MS], con una escala de [NubiMotion.TAB_FADE_MIN_SCALE] a 1: 300 ms en total, los de «pestaña»);
 *   - con «quitar animaciones», de golpe, a cualquiera.
 *   Deslizar con el dedo no cambia: lo decide el pager.
 * - Cuando el pager TERMINA de asentarse en otra página (el dedo lo soltó y llegó), avisa con [onTabSettled]: es la lógica (`viewModel.setTab`); lo que se VE en la barra ya no espera a esto (ver [PagerNavBar]).
 */
@Composable
fun rememberTabPager(currentTab: AppTab, onTabSettled: (AppTab) -> Unit): TabPager {
  val tabs = AppTab.entries
  val pagerState = rememberPagerState(initialPage = currentTab.ordinal) { tabs.size }
  val reduce = rememberReduceMotion()
  val latestTab by rememberUpdatedState(currentTab)
  val latestSettled by rememberUpdatedState(onTabSettled)
  // A qué página va un movimiento que pidió el código (se tocó una pestaña); -1 = ninguno. Mientras dura, el asentamiento del pager NO es una decisión del dedo y no se guarda como pestaña: si se tocaba otra pestaña a medio camino, el movimiento viejo
  // se cancelaba, el pager «se asentaba» donde estaba (Avance) y ese aviso le deshacía al toque lo que acababa de elegir (Juegos). Es un estado para que la barra lo lea (marca el destino desde el primer cuadro).
  val pending = remember { mutableIntStateOf(-1) }
  val tabPager = remember(pagerState) { TabPager(pagerState, pending, Animatable(1f), Animatable(1f)) }
  LaunchedEffect(currentTab) {
    val target = currentTab.ordinal
    // Ya está quieto en esa página (lo normal tras soltar el dedo: el pager llegó y entonces se guardó la pestaña): no hay nada que mover. Si NO está quieto ahí (otra página, o a medio camino hacia otra: se tocó otra pestaña con el
    // movimiento a medias), se va a la nueva sin esperar a que termine el anterior: este efecto se reinicia y cancela el movimiento viejo.
    val atRest = pagerState.settledPage == target && pagerState.currentPage == target && pagerState.currentPageOffsetFraction == 0f && !pagerState.isScrollInProgress
    if (atRest) {
      // También cuando un fundido a medias (Hoy → Tú) se cancela con un toque que vuelve a la página donde el pager todavía estaba: no hay movimiento, pero el destino viejo y la opacidad a medias se limpian.
      tabPager.clearPending()
      tabPager.settleFade()
    } else {
      tabPager.setPending(target)
      val skipsAMiddlePage = abs(target - pagerState.currentPage) >= 2          // Hoy ↔ Tú: deslizar pasaría por Avance
      when {
        reduce -> { tabPager.snapFade(); pagerState.scrollToPage(target) }
        skipsAMiddlePage -> tabPager.fadeThrough(target)
        else -> coroutineScope {
          launch { tabPager.settleFade() }                                          // si venía un fundido a medias, vuelve a lo normal mientras se desliza
          pagerState.animateScrollToPage(target, animationSpec = tween(NubiMotion.TAB_MS, easing = NubiMotion.Emphasized))
        }
      }
      tabPager.clearPending()                                  // (si otro movimiento lo cancela, este renglón no corre: el nuevo fija su propio destino)
    }
  }
  LaunchedEffect(pagerState) {
    // si el dedo toma el pager (aunque sea a medio movimiento), el movimiento del código deja de mandar y las páginas vuelven a su opacidad
    launch { pagerState.interactionSource.interactions.collect { if (it is DragInteraction.Start) { tabPager.clearPending(); tabPager.snapFade() } } }
    snapshotFlow { pagerState.settledPage }.collect { p -> if (pending.intValue == -1 && p != latestTab.ordinal) latestSettled(tabs[p]) }
  }
  return tabPager
}

/**
 * La barra de pestañas LIGADA AL PAGER EN VIVO (Tarea 142): lo que marca es [TabPager.selectedTab], no la pestaña del ViewModel, que solo se actualiza cuando el pager se asienta. La losa sol y el rótulo cambian juntos (la losa se funde en 150 ms,
 * de golpe con «quitar animaciones»).
 */
@Composable
fun PagerNavBar(tabPager: TabPager, onSelect: (AppTab) -> Unit, modifier: Modifier = Modifier) {
  NeuroNavBar(current = tabPager.selectedTab, onSelect = onSelect, modifier = modifier)
}
