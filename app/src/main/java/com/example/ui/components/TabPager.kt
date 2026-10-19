package com.example.ui.components

import androidx.compose.animation.core.tween
import androidx.compose.foundation.interaction.DragInteraction
import androidx.compose.foundation.pager.PagerState
import androidx.compose.foundation.pager.rememberPagerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.runtime.snapshotFlow
import androidx.compose.ui.Modifier
import kotlinx.coroutines.launch
import com.example.viewmodel.AppTab

/**
 * El pager de las tres pestañas (Hoy, Juegos, Avance), sincronizado con la pestaña que guarda el ViewModel (Tarea 142, 19-oct; antes vivía dentro de `MainActivity`).
 * - Si [currentTab] cambia por fuera (se tocó una pestaña de la barra, se volvió de un juego…), el pager se desplaza hasta ella: 300 ms con la curva enfatizada de `NubiMotion` y, con «quitar animaciones», de golpe.
 * - Cuando el pager TERMINA de asentarse en otra página (el dedo lo soltó y llegó), avisa con [onTabSettled]: es la lógica (`viewModel.setTab`); lo que se VE en la barra ya no espera a esto (ver [PagerNavBar]).
 */
@Composable
fun rememberTabPager(currentTab: AppTab, onTabSettled: (AppTab) -> Unit): PagerState {
  val tabs = AppTab.entries
  val pagerState = rememberPagerState(initialPage = currentTab.ordinal) { tabs.size }
  val reduce = rememberReduceMotion()
  val latestTab by rememberUpdatedState(currentTab)
  val latestSettled by rememberUpdatedState(onTabSettled)
  // A qué página va un movimiento que pidió el código (se tocó una pestaña); -1 = ninguno. Mientras dura, el asentamiento del pager NO es una decisión del dedo y no se guarda como pestaña: si se tocaba otra pestaña a medio camino, el movimiento viejo
  // se cancelaba, el pager «se asentaba» donde estaba (Avance) y ese aviso le deshacía al toque lo que acababa de elegir (Juegos).
  val move = remember { object { var pending = -1 } }
  LaunchedEffect(currentTab) {
    val target = currentTab.ordinal
    // Ya está quieto en esa página (lo normal tras soltar el dedo: el pager llegó y entonces se guardó la pestaña): no hay nada que mover. Si NO está quieto ahí (otra página, o a medio camino hacia otra: se tocó otra pestaña con el
    // movimiento a medias), se va a la nueva sin esperar a que termine el anterior: este efecto se reinicia y cancela el movimiento viejo.
    val atRest = pagerState.settledPage == target && pagerState.currentPage == target && pagerState.currentPageOffsetFraction == 0f && !pagerState.isScrollInProgress
    if (!atRest) {
      move.pending = target
      if (reduce) pagerState.scrollToPage(target)
      else pagerState.animateScrollToPage(target, animationSpec = tween(NubiMotion.TAB_MS, easing = NubiMotion.Emphasized))
      move.pending = -1                                    // (si otro movimiento lo cancela, este renglón no corre: el nuevo fija su propio destino)
    }
  }
  LaunchedEffect(pagerState) {
    // si el dedo toma el pager (aunque sea a medio movimiento), el movimiento del código deja de mandar
    launch { pagerState.interactionSource.interactions.collect { if (it is DragInteraction.Start) move.pending = -1 } }
    snapshotFlow { pagerState.settledPage }.collect { p -> if (move.pending == -1 && p != latestTab.ordinal) latestSettled(tabs[p]) }
  }
  return pagerState
}

/**
 * La barra de pestañas LIGADA AL PAGER EN VIVO (Tarea 142: Ricardo notaba «lag»: la página se movía bien pero los íconos de la barra cambiaban recién DESPUÉS de que terminaba el movimiento, porque la barra leía la pestaña del ViewModel, que solo se
 * actualiza cuando el pager se asienta). Aquí la elegida es `pagerState.targetPage`: la página a la que va el pager. Al arrastrar cambia en cuanto el gesto decide (pasado el umbral de posición del pager, unos 56 dp, sin soltar el dedo) y se
 * deshace si se suelta antes y vuelve; al tocar una pestaña, en cuanto el pager arranca hacia ella (un par de cuadros). Cubo, sombra y etiqueta cambian juntos (el cubo se funde en 150 ms, de golpe con «quitar animaciones»).
 */
@Composable
fun PagerNavBar(pagerState: PagerState, onSelect: (AppTab) -> Unit, modifier: Modifier = Modifier) {
  val tabs = AppTab.entries
  NeuroNavBar(current = tabs[pagerState.targetPage.coerceIn(0, tabs.lastIndex)], onSelect = onSelect, modifier = modifier)
}
