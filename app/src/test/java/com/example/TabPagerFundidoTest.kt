package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.pager.HorizontalPager
import androidx.compose.material3.Text
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.snapshots.Snapshot
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.assertCountEquals
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.assertIsSelected
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.unit.dp
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NubiMotion
import com.example.ui.components.PagerNavBar
import com.example.ui.components.TabPager
import com.example.ui.components.rememberTabPager
import com.example.ui.components.tabFade
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * De Hoy a Tú sin pasar por Avance (Tarea 184, 27-oct, decisión de Ricardo): al tocar «Tú» estando en Hoy (o al revés), el pager deslizaba DOS páginas y se veía pasar Avance. Ahora entre pestañas que NO son vecinas hay un «fade through» de Material: la
 * página de ahora se desvanece (90 ms), se cambia de página de golpe en lo más transparente y entra la nueva con un fundido y una escala de 0,96 a 1 (210 ms), 300 ms en total. Entre vecinas se desliza como siempre, deslizar con el dedo no cambia y con
 * «quitar animaciones» se salta de golpe. Estas pruebas arman el mismo cableado que `MainActivity` (`rememberTabPager` + `HorizontalPager` con `tabFade` + `PagerNavBar`) y REGISTRAN, cuadro por cuadro, qué páginas están visibles, compuestas y asentadas,
 * la opacidad y la escala de las páginas y lo que marca la barra.
 *
 * COSTO: cada prueba que mueve el pager con `animateScrollToPage` (un deslizamiento de verdad) tarda ~170 s en esta máquina, y casi todo es la espera de «Compose quieto» al final de la prueba (medido el 27-oct: los 30 cuadros de la animación tardan 40 ms y el
 * `waitForIdle` posterior 165 s, con o sin `autoAdvance`; las pruebas de `TabPagerBarTest` pagan lo mismo, sin tocarlas esta tarea). Por eso aquí SOLO UNA prueba desliza (Hoy → Avance, la que pide la tarea) y el resto usa el fundido y `scrollToPage`, que no lo pagan.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class TabPagerFundidoTest {
  @get:Rule val rule = createComposeRule()

  private var viewModelTab by mutableStateOf(AppTab.HOY)
  private lateinit var tabPager: TabPager
  private val settledCalls = mutableListOf<AppTab>()
  /** Las páginas compuestas AHORA, y las que alguna vez se compusieron desde que se limpió el registro. */
  private val live = mutableSetOf<Int>()
  private val everComposed = mutableSetOf<Int>()

  private class Frame(
    val ms: Long, val visible: List<Int>, val composed: Set<Int>, val current: Int, val settled: Int, val offset: Float,
    val alpha: Float, val scale: Float, val selected: AppTab
  )

  private fun show(reduce: Boolean = false, startAt: AppTab = AppTab.HOY) {
    viewModelTab = startAt
    rule.mainClock.autoAdvance = false
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides reduce) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(700.dp)) {
            BoardBackground()
            val tp = rememberTabPager(viewModelTab) { viewModelTab = it; settledCalls += it }
            tabPager = tp
            HorizontalPager(state = tp.state, modifier = Modifier.fillMaxSize().testTag("tabs_pager").tabFade(tp)) { page ->
              DisposableEffect(page) { live += page; everComposed += page; onDispose { live -= page } }
              Box(Modifier.fillMaxSize()) { Text("página $page") }
            }
            Box(Modifier.align(Alignment.BottomCenter)) { PagerNavBar(tabPager = tp, onSelect = { viewModelTab = it }) }
          }
        }
      }
    }
    rule.mainClock.advanceTimeBy(100)
    rule.waitForIdle()
    everComposed.clear(); everComposed += live                  // el registro empieza con lo que ya hay en pantalla
  }

  private fun snapshot(ms: Long) = Frame(
    ms = ms, visible = tabPager.state.layoutInfo.visiblePagesInfo.map { it.index }, composed = live.toSet(), current = tabPager.state.currentPage,
    settled = tabPager.state.settledPage, offset = tabPager.state.currentPageOffsetFraction, alpha = tabPager.contentAlpha, scale = tabPager.contentScale, selected = tabPager.selectedTab
  )

  /** Avanza [totalMs] de a un cuadro (16 ms) y devuelve lo que se vio en cada uno. */
  private fun frames(totalMs: Long): List<Frame> {
    val out = mutableListOf<Frame>()
    var t = 0L
    while (t < totalMs) { rule.mainClock.advanceTimeBy(16); t += 16; out += snapshot(t) }
    return out
  }

  /**
   * Lo mismo que tocar la pestaña de la barra (`onSelect = { viewModelTab = it }`), sin `performClick`: con el reloj a mano, el `performClick` espera a que Compose «esté quieto» y con un movimiento real del pager no lo está, así que tarda
   * unos 190 s por prueba (lo mismo que cuesta cada prueba de `TabPagerBarTest` que mueve el pager; el toque de verdad ya lo prueba esa clase). Aquí se cambia la pestaña en el hilo de la interfaz y se avanzan los cuadros.
   */
  private fun tap(tag: String) {
    val tab = when (tag) { "nav_item_hoy" -> AppTab.HOY; "nav_item_progreso" -> AppTab.PROGRESO; else -> AppTab.TU }
    rule.runOnUiThread { viewModelTab = tab; Snapshot.sendApplyNotifications() }          // (sin esto, el cambio de estado no llega a la composición hasta que algo espera a que esté quieta)
  }

  private fun assertNeverSawAvance(frames: List<Frame>) {
    for (f in frames) {
      assertFalse("a los ${f.ms} ms Avance está a la vista: ${f.visible}", 1 in f.visible)
      assertFalse("a los ${f.ms} ms Avance está compuesto: ${f.composed}", 1 in f.composed)
      assertTrue("a los ${f.ms} ms el pager ya está en Avance (actual ${f.current}, asentado ${f.settled})", f.current != 1 && f.settled != 1)
      assertTrue("a los ${f.ms} ms el pager está entre dos páginas (${f.offset})", f.offset == 0f)
      assertTrue("a los ${f.ms} ms la barra marca Avance", f.selected != AppTab.PROGRESO)
    }
    assertFalse("Avance nunca llegó a componerse ($everComposed)", 1 in everComposed)
  }

  @Test
  fun de_Hoy_a_Tu_Avance_nunca_se_dibuja_ni_se_asienta_y_dura_lo_de_una_pestana() {
    show()
    assertEquals(setOf(0), live)
    tap("nav_item_tu")
    val frames = frames(480)
    assertNeverSawAvance(frames)
    // la barra marca el destino de una vez, sin esperar al fundido ni pasar por Avance
    assertEquals("a los 32 ms la barra ya marca Tú", AppTab.TU, frames[1].selected)
    // el fundido: sale la página de ahora (la opacidad baja hasta casi nada)…
    val dip = frames.minBy { it.alpha }
    assertTrue("la opacidad baja casi a cero (${dip.alpha})", dip.alpha <= 0.1f)
    // …el cambio de página se hace en lo más transparente…
    val switched = frames.first { it.current == 2 }
    assertTrue("la página cambia con la opacidad baja (${switched.alpha})", switched.alpha <= 0.35f)
    assertTrue("y no antes de que casi no se vea (a los ${switched.ms} ms)", switched.ms in 64L..160L)
    // …y entra la nueva: la opacidad sube a 1 y la escala de 0,96 a 1
    val minScale = frames.minOf { it.scale }
    assertEquals("entra con la escala mínima de 0,96", NubiMotion.TAB_FADE_MIN_SCALE, minScale, 0.01f)
    assertTrue("a los 150 ms la nueva todavía está entrando (${frames.first { it.ms >= 150 }.alpha})", frames.first { it.ms >= 150 }.alpha < 1f)
    val done = frames.first { it.alpha == 1f && it.scale == 1f && it.ms > switched.ms }
    assertTrue("todo termina unos 300 ms después de que arranca el efecto (a los ${done.ms} ms contando desde el toque, que arranca un par de cuadros antes)", done.ms in 280L..400L)
    val last = frames.last()
    assertEquals(2, last.settled); assertEquals(2, last.current); assertEquals(1f, last.alpha, 0f); assertEquals(1f, last.scale, 0f)
    assertEquals(AppTab.TU, viewModelTab)
    assertTrue("un toque no es un asentamiento por gesto: el aviso no se repite", settledCalls.isEmpty())
    rule.onNodeWithTag("nav_item_tu").assertIsSelected()
  }

  @Test
  fun de_Tu_a_Hoy_tambien_es_un_fundido_sin_pasar_por_Avance() {
    show(startAt = AppTab.TU)
    assertEquals(setOf(2), live)
    tap("nav_item_hoy")
    val frames = frames(480)
    assertNeverSawAvance(frames)
    assertEquals(0, frames.last().settled)
    assertEquals(1f, frames.last().alpha, 0f)
    assertEquals(AppTab.HOY, viewModelTab)
    assertTrue(settledCalls.isEmpty())
  }

  @Test
  fun de_Hoy_a_Avance_sigue_deslizando_sin_fundido() {
    show()
    tap("nav_item_progreso")
    val frames = frames(480)
    assertTrue("hubo un cuadro con Hoy y Avance a la vez (se deslizó)", frames.any { 0 in it.visible && 1 in it.visible })
    assertTrue("el pager estuvo entre dos páginas", frames.any { it.offset != 0f || it.current != it.settled })
    for (f in frames) { assertEquals("sin fundido a los ${f.ms} ms", 1f, f.alpha, 0f); assertEquals(1f, f.scale, 0f) }
    assertEquals(1, frames.last().settled)
    assertEquals(AppTab.PROGRESO, viewModelTab)
    assertTrue(settledCalls.isEmpty())
  }

  @Test
  fun con_quitar_animaciones_de_Hoy_a_Tu_salta_de_golpe_sin_pasar_por_Avance() {
    show(reduce = true)
    tap("nav_item_tu")
    val frames = frames(160)
    assertNeverSawAvance(frames)
    assertEquals("de golpe: a los 48 ms el pager ya está en Tú", 2, frames.first { it.ms >= 48 }.settled)
    for (f in frames) { assertEquals("sin fundido a los ${f.ms} ms", 1f, f.alpha, 0f); assertEquals(1f, f.scale, 0f) }
    assertEquals(AppTab.TU, viewModelTab)
  }

  @Test
  fun el_lector_de_pantalla_queda_en_la_pagina_destino() {
    show()
    tap("nav_item_tu")
    frames(480)
    rule.onNodeWithText("página 2").assertIsDisplayed()
    rule.onAllNodesWithText("página 0").assertCountEquals(0)
    rule.onAllNodesWithText("página 1").assertCountEquals(0)
  }

  @Test
  fun tocar_Hoy_a_mitad_del_fundido_de_salida_vuelve_sin_mover_el_pager_y_sin_dejar_la_pagina_a_medias() {
    show()
    tap("nav_item_tu")
    val salida = frames(48)                                                   // la página de Hoy se está desvaneciendo, el pager todavía no se movió
    assertEquals(0, salida.last().settled)
    assertTrue("a medio fundido (${salida.last().alpha})", salida.last().alpha < 1f)
    tap("nav_item_hoy")                                                      // se arrepiente
    val frames = frames(480)
    assertNeverSawAvance(frames)
    assertTrue("nunca se llegó a Tú", frames.all { it.settled == 0 && it.current == 0 && 2 !in it.composed })
    assertEquals("la página vuelve a su opacidad", 1f, frames.last().alpha, 0f)
    assertEquals(1f, frames.last().scale, 0f)
    assertEquals(AppTab.HOY, frames.last().selected)
    assertEquals(AppTab.HOY, viewModelTab)
    assertTrue(settledCalls.isEmpty())
    // y el pager sigue sirviendo: el siguiente toque funciona
    everComposed.clear(); everComposed += live
    tap("nav_item_tu")
    val otra = frames(480)
    assertNeverSawAvance(otra)
    assertEquals(2, otra.last().settled)
  }
}
