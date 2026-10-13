package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithContentDescription
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.dp
import com.example.data.RouteCelebrationStore
import com.example.data.RouteCelebrations
import com.example.data.RouteLamps
import com.example.data.RoutePlan
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeUi
import com.example.ui.screens.homeStops
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.util.Calendar

/**
 * Las farolas de la ruta en Hoy (Tarea 88), con `HomeContent` y un almacén de prueba: la descripción de la isla para el lector de pantalla, que cada farola pulse una sola vez por parada y por día, que la ruta completa se celebre una vez al día y que con «quitar animaciones»
 * se anote igual (la farola nace encendida y no hay segunda vez). Capturas de la isla con 0, 1, 2 y 3 paradas hechas y de dos cuadros del encendido y del saltito.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class HoyLamparasTest {
  @get:Rule val rule = createComposeRule()

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 13, 16, 30, 0) }.timeInMillis
  private val day = RouteLamps.dayKey(now)
  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  private val ids = listOf("bodega", "calculo", "intrusa")          // Observatorio (Memoria), Hangar (Razonamiento), Biblioteca (Lenguaje)

  private fun ui(completed: Int) = HomeUi(
    name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(ids, completed, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true
  )

  private class FakeStore(var saved: RouteCelebrations? = null) : RouteCelebrationStore {
    var saves = 0
    override fun load() = saved
    override fun save(value: RouteCelebrations) { saved = value; saves++ }
  }

  private val plans = mutableListOf<RoutePlan>()

  private fun show(ui: HomeUi, store: RouteCelebrationStore? = null, reduce: Boolean = false) {
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides reduce) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(873.dp)) {
            BoardBackground()
            HomeContent(
              ui = ui, onProfile = {}, onSettings = {}, onPlaceGame = {}, onPlaceGameCard = {}, onStop = {}, onPlay = {}, onResume = {}, onBaseline = {},
              modifier = Modifier.padding(top = 24.dp, bottom = 163.dp), routeStore = store, onRoutePlan = { plans += it }
            )
          }
        }
      }
    }
    rule.waitForIdle()
  }

  // ---- La descripción de la isla (lector de pantalla)

  @Test fun descripcion_con_0_paradas_hechas() { show(ui(0)); rule.onNodeWithContentDescription("Tu isla: hoy la ruta pasa por el Observatorio, el Hangar y la Biblioteca").assertExists() }
  @Test fun descripcion_con_1_parada_hecha() { show(ui(1)); rule.onNodeWithContentDescription("Tu isla: hecho el Observatorio; faltan el Hangar y la Biblioteca").assertExists() }
  @Test fun descripcion_con_2_paradas_hechas() { show(ui(2)); rule.onNodeWithContentDescription("Tu isla: hechos el Observatorio y el Hangar; falta la Biblioteca").assertExists() }
  @Test fun descripcion_con_3_paradas_hechas() { show(ui(3)); rule.onNodeWithContentDescription("Tu isla: ruta completa, hechos el Observatorio, el Hangar y la Biblioteca").assertExists() }

  // ---- Una vez por parada y por día

  @Test
  fun sin_almacen_no_hay_celebracion_y_las_farolas_estan_encendidas() {
    show(ui(2), store = null)
    assertTrue("sin almacén no hay plan que animar", plans.isEmpty())
  }

  @Test
  fun la_primera_vez_pulsa_la_farola_de_la_parada_hecha_y_se_anota() {
    val store = FakeStore()
    show(ui(1), store)
    assertEquals(listOf(RoutePlan(listOf("observatorio"), complete = false)), plans)
    assertEquals(RouteCelebrations(day, setOf("observatorio"), complete = false), store.saved)
  }

  @Test
  fun al_volver_a_Hoy_ese_mismo_dia_ya_no_pulsa() {
    val store = FakeStore(RouteCelebrations(day, setOf("observatorio")))
    show(ui(1), store)
    assertTrue("la farola ya estaba celebrada: sin plan", plans.isEmpty())
    assertEquals("y no se vuelve a guardar nada", 0, store.saves)
  }

  @Test
  fun una_parada_nueva_pulsa_solo_su_farola() {
    val store = FakeStore(RouteCelebrations(day, setOf("observatorio")))
    show(ui(2), store)
    assertEquals(listOf(RoutePlan(listOf("hangar"), complete = false)), plans)
    assertEquals(setOf("observatorio", "hangar"), store.saved!!.lamps)
  }

  @Test
  fun al_completar_la_tercera_pulsa_su_farola_y_Nubi_celebra_una_sola_vez() {
    val store = FakeStore(RouteCelebrations(day, setOf("observatorio", "hangar")))
    show(ui(3), store)
    assertEquals(listOf(RoutePlan(listOf("biblioteca"), complete = true)), plans)
    assertTrue(store.saved!!.complete)
  }

  @Test
  fun la_ruta_completa_ya_celebrada_no_se_repite() {
    val store = FakeStore(RouteCelebrations(day, setOf("observatorio", "hangar", "biblioteca"), complete = true))
    show(ui(3), store)
    assertTrue("nada se mueve al volver a Hoy", plans.isEmpty())
  }

  @Test
  fun al_dia_siguiente_vuelve_a_celebrar() {
    val store = FakeStore(RouteCelebrations("2026-10-12", setOf("observatorio", "hangar", "biblioteca"), complete = true))
    show(ui(1), store)
    assertEquals(listOf(RoutePlan(listOf("observatorio"), complete = false)), plans)
    assertEquals("lo de ayer no se arrastra", RouteCelebrations(day, setOf("observatorio"), complete = false), store.saved)
  }

  @Test
  fun con_quitar_animaciones_se_anota_igual_y_no_hay_segunda_vez() {
    val store = FakeStore()
    show(ui(3), store, reduce = true)
    assertEquals(listOf(RoutePlan(listOf("observatorio", "hangar", "biblioteca"), complete = true)), plans)
    assertTrue(store.saved!!.complete && store.saved!!.lamps.size == 3)
  }

  // ---- Capturas

  @Test fun captura_isla_0_paradas_hechas() { show(ui(0)); rule.onNodeWithTag("home_isla").captureRoboImage(filePath = "src/test/screenshots/lamparas-0.png") }
  @Test fun captura_isla_1_parada_hecha() { show(ui(1)); rule.onNodeWithTag("home_isla").captureRoboImage(filePath = "src/test/screenshots/lamparas-1.png") }
  @Test fun captura_isla_2_paradas_hechas() { show(ui(2)); rule.onNodeWithTag("home_isla").captureRoboImage(filePath = "src/test/screenshots/lamparas-2.png") }
  @Test fun captura_isla_3_paradas_hechas() { show(ui(3)); rule.onNodeWithTag("home_isla").captureRoboImage(filePath = "src/test/screenshots/lamparas-3.png") }

  @Test
  fun capturas_del_encendido_y_del_saltito() {
    rule.mainClock.autoAdvance = false
    val store = FakeStore(RouteCelebrations(day, setOf("observatorio", "hangar")))
    var current by mutableStateOf(ui(3))
    rule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(873.dp)) {
          BoardBackground()
          HomeContent(ui = current, onProfile = {}, onSettings = {}, onPlaceGame = {}, onPlaceGameCard = {}, onStop = {}, onPlay = {}, onResume = {}, onBaseline = {}, modifier = Modifier.padding(top = 24.dp, bottom = 163.dp), routeStore = store, onRoutePlan = { plans += it })
        }
      }
    }
    rule.mainClock.advanceTimeBy(250)
    rule.onNodeWithTag("home_isla").captureRoboImage(filePath = "src/test/screenshots/lamparas-pulso-250ms.png")     // la farola de la Biblioteca creciendo y Nubi mirando hacia ella
    rule.mainClock.advanceTimeBy(1_650 + 225 + 100)                                                                   // pulso y mirada terminaron; el primer salto va por su punto más alto (~225 ms)
    rule.onNodeWithTag("home_isla").captureRoboImage(filePath = "src/test/screenshots/lamparas-salto.png")
    rule.mainClock.advanceTimeBy(150)
    rule.onNodeWithTag("home_isla").captureRoboImage(filePath = "src/test/screenshots/lamparas-chispas.png")
    assertNotNull(store.saved)
  }
}
