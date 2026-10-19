package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.pager.HorizontalPager
import androidx.compose.foundation.pager.PagerState
import androidx.compose.material3.Text
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.assertIsNotSelected
import androidx.compose.ui.test.assertIsSelected
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.test.down
import androidx.compose.ui.test.moveBy
import androidx.compose.ui.test.up
import androidx.compose.ui.unit.dp
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.PagerNavBar
import com.example.ui.components.rememberTabPager
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La barra de abajo sin «lag» (Tarea 142, 19-oct): Ricardo deslizaba de Hoy a Juegos o a Avance y la página se movía bien, pero los íconos de la barra cambiaban recién DESPUÉS de que terminaba el movimiento: la barra leía la pestaña del ViewModel, que solo se actualiza
 * cuando el pager se asienta (`settledPage`). Ahora la barra lee el pager EN VIVO (`PagerNavBar`: `targetPage`). Estas pruebas arman el mismo cableado que `MainActivity` (`rememberTabPager` + `HorizontalPager` + `PagerNavBar`, con un estado en el lugar del ViewModel) y miran
 * lo que se ve ANTES de que el pager se asiente.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class TabPagerBarTest {
  @get:Rule val rule = createComposeRule()

  private var viewModelTab by mutableStateOf(AppTab.HOY)       // lo que guardaría el ViewModel (`currentTab`): solo cambia al asentarse el pager o al tocar una pestaña
  private lateinit var pager: PagerState
  private val settledCalls = mutableListOf<AppTab>()

  private fun show(reduce: Boolean = false) {
    rule.mainClock.autoAdvance = false
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides reduce) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(700.dp)) {
            BoardBackground()
            val state = rememberTabPager(viewModelTab) { viewModelTab = it; settledCalls += it }
            pager = state
            HorizontalPager(state = state, modifier = Modifier.fillMaxSize().testTag("tabs_pager")) { page -> Box(Modifier.fillMaxSize()) { Text("página $page") } }
            Box(Modifier.align(Alignment.BottomCenter)) { PagerNavBar(pagerState = state, onSelect = { viewModelTab = it }) }
          }
        }
      }
    }
    rule.mainClock.advanceTimeBy(100)
    rule.waitForIdle()
  }

  /** Pone el dedo y arrastra [fraction] del ancho de la página hacia la izquierda (a la derecha si es negativo) SIN soltar. */
  private fun dragWithoutRelease(fraction: Float) {
    rule.onNodeWithTag("tabs_pager").performTouchInput {
      down(center)
      repeat(8) { moveBy(Offset(-width * fraction / 8f, 0f), delayMillis = 16) }
    }
    rule.mainClock.advanceTimeBy(50)
    rule.waitForIdle()
  }

  private fun assertOnlySelected(tag: String) {
    for (t in listOf("nav_item_hoy", "nav_item_juegos", "nav_item_progreso")) {
      if (t == tag) rule.onNodeWithTag(t).assertIsSelected() else rule.onNodeWithTag(t).assertIsNotSelected()
    }
  }

  @Test
  fun arrastrar_el_pager_mas_alla_de_la_mitad_sin_soltar_ya_selecciona_Juegos_antes_de_asentarse() {
    show()
    assertOnlySelected("nav_item_hoy")
    dragWithoutRelease(0.6f)                                                  // más allá de la mitad de la página, el dedo sigue abajo
    assertOnlySelected("nav_item_juegos")                                    // la barra ya cambió…
    assertEquals("…y el pager todavía no se asentó", 0, pager.settledPage)
    assertEquals("…ni el ViewModel (la lógica sigue con settledPage)", AppTab.HOY, viewModelTab)
    assertTrue("no hubo aviso de asentamiento", settledCalls.isEmpty())
    rule.onNodeWithTag("tabs_pager").performTouchInput { up() }              // al soltar, el pager llega y entonces sí se guarda la pestaña
    rule.mainClock.advanceTimeBy(1_000)
    rule.waitForIdle()
    assertEquals(1, pager.settledPage)
    assertEquals(AppTab.JUEGOS, viewModelTab)
    assertEquals(listOf(AppTab.JUEGOS), settledCalls)
    assertOnlySelected("nav_item_juegos")
  }

  @Test
  fun la_barra_cambia_en_cuanto_el_gesto_decide_sin_esperar_a_la_mitad() {
    show()
    dragWithoutRelease(0.25f)                                                // un cuarto de página: pasó el umbral del pager (unos 56 dp), todavía no la mitad
    assertOnlySelected("nav_item_juegos")
    assertEquals(0, pager.settledPage)
  }

  @Test
  fun un_arrastre_corto_no_cambia_la_barra_y_si_se_suelta_vuelve_a_Hoy() {
    show()
    dragWithoutRelease(0.08f)                                                // unos 30 dp: no decide
    assertOnlySelected("nav_item_hoy")
    rule.onNodeWithTag("tabs_pager").performTouchInput { up() }
    rule.mainClock.advanceTimeBy(1_000)
    rule.waitForIdle()
    assertOnlySelected("nav_item_hoy")
    assertEquals(0, pager.settledPage)
    assertTrue(settledCalls.isEmpty())
  }

  @Test
  fun si_se_arrastra_mas_alla_y_se_vuelve_atras_antes_de_soltar_la_barra_se_deshace() {
    show()
    dragWithoutRelease(0.6f)
    assertOnlySelected("nav_item_juegos")
    rule.onNodeWithTag("tabs_pager").performTouchInput { repeat(8) { moveBy(Offset(width * 0.6f / 8f, 0f), delayMillis = 16) } }
    rule.mainClock.advanceTimeBy(50)
    rule.waitForIdle()
    assertOnlySelected("nav_item_hoy")
    rule.onNodeWithTag("tabs_pager").performTouchInput { up() }
  }

  @Test
  fun tocar_una_pestana_la_selecciona_al_instante_sin_esperar_a_que_el_pager_termine() {
    show()
    rule.onNodeWithTag("nav_item_progreso").performClick()
    rule.mainClock.advanceTimeBy(48)                                         // tres cuadros: el pager apenas arrancó (su movimiento dura 300 ms)
    rule.waitForIdle()
    assertOnlySelected("nav_item_progreso")
    assertEquals("el pager todavía no llegó", 0, pager.settledPage)
    assertEquals(AppTab.PROGRESO, viewModelTab)
    rule.mainClock.advanceTimeBy(1_000)
    rule.waitForIdle()
    assertEquals(2, pager.settledPage)
    assertOnlySelected("nav_item_progreso")
    assertTrue("tocar no es un asentamiento por gesto: el aviso no se repite", settledCalls.isEmpty())
  }

  @Test
  fun tocar_otra_pestana_a_mitad_del_movimiento_no_espera_a_que_termine_el_anterior() {
    show()
    rule.onNodeWithTag("nav_item_progreso").performClick()
    rule.mainClock.advanceTimeBy(100)
    assertOnlySelected("nav_item_progreso")
    rule.onNodeWithTag("nav_item_juegos").performClick()                    // cambia de idea a medio camino
    rule.mainClock.advanceTimeBy(48)
    rule.waitForIdle()
    assertOnlySelected("nav_item_juegos")
    rule.mainClock.advanceTimeBy(1_000)
    rule.waitForIdle()
    assertEquals(1, pager.settledPage)
    assertEquals(AppTab.JUEGOS, viewModelTab)
  }

  @Test
  fun con_quitar_animaciones_tocar_una_pestana_salta_de_golpe() {
    show(reduce = true)
    rule.onNodeWithTag("nav_item_juegos").performClick()
    rule.mainClock.advanceTimeBy(32)
    rule.waitForIdle()
    assertOnlySelected("nav_item_juegos")
    assertEquals("de golpe: el pager ya está en Juegos", 1, pager.settledPage)
  }
}
