package com.example.ui.components

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.FastOutLinearInEasing
import androidx.compose.animation.core.LinearOutSlowInEasing
import androidx.compose.animation.core.tween
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.Stable
import androidx.compose.runtime.compositionLocalOf
import androidx.compose.runtime.getValue
import androidx.compose.runtime.key
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveableStateHolder
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.layout.layout
import androidx.compose.runtime.withFrameNanos
import androidx.compose.ui.semantics.clearAndSetSemantics
import com.example.viewmodel.AppTab
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

/**
 * ¿La pestaña que se está componiendo es la que se VE? Las páginas ya visitadas quedan compuestas y ESCONDIDAS ([TabHost]), así que lo que corre por su cuenta (el vaivén y los parpadeos de Nubi) o lo que escucha el botón Atrás tiene que mirar esto y
 * quedarse quieto cuando es falso (Tarea 224). Por omisión es verdadero: fuera de un [TabHost] (una pantalla probada sola) todo se comporta como siempre.
 */
val LocalTabActive = compositionLocalOf { true }

/** Cuánto espera [TabHost] (con `warmUp`) desde que la app está lista hasta empezar a componer las pestañas que faltan, y entre una y otra: una por vez, para que cada cuadro pesado quede solo. */
const val TAB_WARM_START_MS = 1500L
const val TAB_WARM_GAP_MS = 500L

/** La opacidad y la escala de las páginas durante el fundido entre pestañas (1 y 1 = en reposo). Se leen al dibujar, no recomponen nada. */
@Stable
class TabFade internal constructor() {
  internal val alpha = Animatable(1f)
  internal val scale = Animatable(1f)
  /** La opacidad de las páginas (1 = normal; baja a 0 y vuelve a subir en el fundido). */
  val contentAlpha: Float get() = alpha.value
  /** La escala de las páginas (1 = normal; entra desde [NubiMotion.TAB_FADE_MIN_SCALE] y sube a 1). */
  val contentScale: Float get() = scale.value
}

@Composable
fun rememberTabFade(): TabFade = remember { TabFade() }

/**
 * Las cinco pestañas (Hoy, Juegos, Contrarreloj, Avance, Tú) en UN solo lugar, SIN pager (Tarea 224, 4-nov, idea de Ricardo: «al pasar de una pestaña a otra no se siente prolijo ni suave, como que se pega un poco»; antes, desde el 29-sep, se pasaban
 * también deslizando y las vecinas se deslizaban al tocar la barra, y a una no vecina se iba con un fundido). Ahora la pestaña cambia SOLO al tocar la barra (arrastrar hacia los costados no hace nada: lo que se desliza DENTRO de una página sigue igual) y
 * SIEMPRE con el mismo fundido, el «fade through» de Material, vecina o no (Tarea 184): la página de ahora se desvanece ([NubiMotion.TAB_FADE_OUT_MS], acelera), en lo más transparente se cambia de página de golpe y entra la nueva con un fundido y una escala de
 * [NubiMotion.TAB_FADE_MIN_SCALE] a 1 ([NubiMotion.TAB_FADE_IN_MS], frena): [NubiMotion.TAB_MS] en total. Con «quitar animaciones», de golpe.
 *
 * **Sin tirones.** Una página se compone la PRIMERA vez que se visita y de ahí en adelante se queda compuesta, escondida (no se coloca: ni se dibuja, ni recibe toques, ni la lee el lector de pantalla; `LocalTabActive` es falso para ella): volver a una pestaña ya
 * visitada no recompone nada ni pierde lo que tenía (el deslizado, la búsqueda y el chip de Juegos, el juego elegido en Contrarreloj). La página nueva se compone cuando todo está transparente y el fundido de entrada espera dos cuadros, para que el cuadro pesado
 * (componer y medir) no se coma el principio del movimiento. Lo que guarda `KeepTabsState` (la posición de cada página mientras un juego o un panel sacan las pestañas de la pantalla) sigue igual: cada página guarda su estado con su nombre.
 *
 * **La primera visita también sin tirón** ([warmUp]): componer una página por primera vez cuesta un cuadro largo (en el emulador, de 230 a 900 ms; la de Juegos, con sus 17 emblemas, es la peor). Con [warmUp] verdadero (la app ya está lista y Hoy a la vista),
 * [TabHost] compone las pestañas que faltan, escondidas y de a UNA ([TAB_WARM_GAP_MS] entre cada una, empezando [TAB_WARM_START_MS] después), mientras la persona mira Hoy: cuando toca una, ya está compuesta y el fundido no paga nada.
 *
 * [currentTab] es la pestaña que marca la barra y que guarda el ViewModel; la barra la marca de una vez (no espera al fundido). Si cambia a mitad de un fundido (se tocó otra pestaña), el movimiento sigue desde donde está sin esperar; si vuelve a la que todavía se ve, las
 * páginas vuelven a su opacidad. [page] dibuja la página de una pestaña.
 */
@Composable
fun TabHost(currentTab: AppTab, modifier: Modifier = Modifier, fade: TabFade = rememberTabFade(), warmUp: Boolean = false, page: @Composable (AppTab) -> Unit) {
  val reduce = rememberReduceMotion()
  val saveable = rememberSaveableStateHolder()
  var shown by remember { mutableStateOf(currentTab) }                     // la pestaña que se ve (sigue a currentTab, pero en lo más transparente del fundido)
  val visited = remember { mutableStateListOf(currentTab) }                // las que ya se compusieron: quedan compuestas
  LaunchedEffect(currentTab) {
    if (currentTab == shown) {
      // ya se ve (lo normal al abrir), o se tocó otra y se volvió a esta a mitad del fundido de salida: las páginas vuelven a su opacidad y escala, rápido
      if (fade.alpha.value != 1f || fade.scale.value != 1f) coroutineScope {
        launch { fade.scale.animateTo(1f, tween(NubiMotion.STATE_FAST_MS)) }
        fade.alpha.animateTo(1f, tween(NubiMotion.STATE_FAST_MS))
      }
      return@LaunchedEffect
    }
    if (reduce) {
      fade.alpha.snapTo(1f); fade.scale.snapTo(1f)
      if (currentTab !in visited) visited.add(currentTab)
      shown = currentTab
      return@LaunchedEffect
    }
    fade.scale.snapTo(1f)
    fade.alpha.animateTo(0f, tween(NubiMotion.TAB_FADE_OUT_MS, easing = FastOutLinearInEasing))     // sale la página de ahora (acelera)…
    if (currentTab !in visited) visited.add(currentTab)
    shown = currentTab                                                                              // …en lo más transparente se cambia de página, de golpe: nunca se ve pasar la del medio…
    withFrameNanos { }; withFrameNanos { }                                                          // …el cuadro pesado (componer y medir la página nueva) pasa con todo transparente…
    fade.scale.snapTo(NubiMotion.TAB_FADE_MIN_SCALE)
    coroutineScope {                                                                                // …y entra la nueva con un fundido y una escala mínima (frena)
      launch { fade.scale.animateTo(1f, tween(NubiMotion.TAB_FADE_IN_MS, easing = LinearOutSlowInEasing)) }
      fade.alpha.animateTo(1f, tween(NubiMotion.TAB_FADE_IN_MS, easing = LinearOutSlowInEasing))
    }
  }
  LaunchedEffect(warmUp) {
    if (!warmUp) return@LaunchedEffect
    delay(TAB_WARM_START_MS)
    for (tab in AppTab.entries) {
      if (tab !in visited) { withFrameNanos { }; if (tab !in visited) visited.add(tab) }          // un cuadro de calma y se compone la siguiente, escondida
      delay(TAB_WARM_GAP_MS)
    }
  }
  Box(modifier.fillMaxSize().graphicsLayer {
    val s = fade.scale.value
    alpha = fade.alpha.value; scaleX = s; scaleY = s
  }) {
    for (tab in visited.toList()) key(tab) {
      saveable.SaveableStateProvider(tab.name) {
        CompositionLocalProvider(LocalTabActive provides (tab == shown)) {
          Box(Modifier.fillMaxSize().parked(tab != shown)) { page(tab) }
        }
      }
    }
  }
}

/**
 * Una página compuesta que no se ve: se mide (para estar lista) pero no se coloca, así que no se dibuja ni recibe toques, y fuera del lector de pantalla. Cambiar de pestaña no la recompone ni la recrea: solo la coloca.
 */
private fun Modifier.parked(parked: Boolean): Modifier =
  if (!parked) this
  else this
    .layout { measurable, constraints ->
      val placeable = measurable.measure(constraints)
      layout(placeable.width, placeable.height) { }
    }
    .clearAndSetSemantics { }
