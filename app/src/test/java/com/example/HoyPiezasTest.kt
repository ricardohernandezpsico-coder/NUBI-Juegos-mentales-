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
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithContentDescription
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.dp
import com.example.data.IslaPieza
import com.example.data.IslaPiezasStore
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeUi
import com.example.ui.screens.homeStops
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import java.util.Calendar

/**
 * Las piezas de la isla en Hoy (Tarea 136, «Tu isla crece»), con `HomeContent` y un almacén de prueba: sin almacén las piezas están puestas y no hay revelación; la primera vez (almacén vacío) se siembra en silencio; una pieza nueva se revela UNA vez y se anota;
 * al volver a Hoy no se repite; con «quitar animaciones» se anota igual; mientras una celebración de medalla está encima, la pieza espera; la descripción de la isla las nombra; y diez piezas más no cambian el tamaño de la isla ni de sus toques.
 */
@RunWith(RobolectricTestRunner::class)
@org.robolectric.annotation.GraphicsMode(org.robolectric.annotation.GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class HoyPiezasTest {
  @get:Rule val rule = createComposeRule()

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 19, 16, 30, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  private val ids = listOf("bodega", "calculo", "intrusa")

  private fun ui(pieces: List<IslaPieza>) = HomeUi(
    name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(ids, 0, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true, islandPieces = pieces
  )

  private class FakeStore(var saved: Set<String>? = null) : IslaPiezasStore {
    var saves = 0
    override fun load() = saved
    override fun save(ids: Set<String>) { saved = ids; saves++ }
  }

  private val plans = mutableListOf<List<IslaPieza>>()

  private fun show(ui: HomeUi, store: IslaPiezasStore? = null, reduce: Boolean = false, hold: Boolean = false) {
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides reduce) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(873.dp)) {
            BoardBackground()
            HomeContent(
              ui = ui, onAllGames = {}, onPlaceGame = {}, onPlaceGameCard = {}, onStop = {}, onPlay = {}, onResume = {}, onBaseline = {},
              modifier = Modifier.padding(top = 24.dp, bottom = 163.dp), pieceStore = store, holdPieces = hold, onPiecePlan = { plans += it }
            )
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun drawn(): List<String> = IslaPieza.entries.map { it.id }.filter { rule.onAllNodes(hasTestTag("isla_pieza_$it")).fetchSemanticsNodes().isNotEmpty() }

  // ---- Qué se dibuja

  @Test
  fun sin_piezas_no_se_dibuja_ninguna() {
    show(ui(emptyList()))
    assertTrue(drawn().isEmpty())
  }

  @Test
  fun cada_pieza_ganada_se_dibuja_y_las_demas_no() {
    show(ui(listOf(IslaPieza.C1, IslaPieza.V1, IslaPieza.VO1)))
    assertEquals(listOf("c1", "v1", "vo1"), drawn())
  }

  @Test
  fun las_diez_piezas_se_dibujan() {
    show(ui(IslaPieza.entries))
    assertEquals(IslaPieza.entries.map { it.id }, drawn())
  }

  // ---- Una vez, y en silencio la primera

  @Test
  fun sin_almacen_no_hay_revelacion_y_las_piezas_estan_puestas() {
    show(ui(listOf(IslaPieza.C1, IslaPieza.V1)), store = null)
    assertTrue("sin almacén no hay plan que animar", plans.isEmpty())
    assertEquals(listOf("c1", "v1"), drawn())
  }

  @Test
  fun la_primera_vez_con_el_almacen_vacio_se_siembra_en_silencio() {
    val store = FakeStore(saved = null)
    show(ui(listOf(IslaPieza.C1, IslaPieza.C3, IslaPieza.V1)), store)
    assertTrue("quien ya tenía medallas no ve que «llueva» nada", plans.isEmpty())
    assertEquals(setOf("c1", "c3", "v1"), store.saved)
  }

  @Test
  fun la_primera_vez_sin_piezas_tambien_deja_sembrado_para_que_la_primera_si_se_revele() {
    val store = FakeStore(saved = null)
    show(ui(emptyList()), store)
    assertEquals("sembrado vacío (no null): la próxima pieza sí entra con su animación", emptySet<String>(), store.saved)
  }

  @Test
  fun una_pieza_nueva_se_revela_una_vez_y_se_anota() {
    val store = FakeStore(saved = setOf("c1"))
    show(ui(listOf(IslaPieza.C1, IslaPieza.V1)), store)
    assertEquals(listOf(listOf(IslaPieza.V1)), plans)
    assertEquals(setOf("c1", "v1"), store.saved)
  }

  @Test
  fun las_piezas_nuevas_se_revelan_en_el_orden_de_dibujo() {
    val store = FakeStore(saved = emptySet())
    show(ui(listOf(IslaPieza.C1, IslaPieza.V1, IslaPieza.VO1)), store)
    assertEquals(listOf(listOf(IslaPieza.C1, IslaPieza.V1, IslaPieza.VO1)), plans)
  }

  @Test
  fun al_volver_a_Hoy_no_se_repite() {
    val store = FakeStore(saved = setOf("c1", "v1"))
    show(ui(listOf(IslaPieza.C1, IslaPieza.V1)), store)
    assertTrue("ya estaban vistas: sin plan", plans.isEmpty())
    assertEquals("y no se vuelve a guardar nada", 0, store.saves)
  }

  @Test
  fun con_quitar_animaciones_se_anota_igual_y_no_hay_segunda_vez() {
    val store = FakeStore(saved = emptySet())
    show(ui(listOf(IslaPieza.C1, IslaPieza.VO1)), store, reduce = true)
    assertEquals(listOf(listOf(IslaPieza.C1, IslaPieza.VO1)), plans)
    assertEquals(setOf("c1", "vo1"), store.saved)
    assertEquals("nacen puestas", listOf("c1", "vo1"), drawn())
  }

  @Test
  fun mientras_la_celebracion_de_la_medalla_esta_encima_la_pieza_espera_y_despues_entra_una_vez() {
    val store = FakeStore(saved = setOf("c1"))
    var hold by mutableStateOf(true)
    rule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(873.dp)) {
          BoardBackground()
          HomeContent(
            ui = ui(listOf(IslaPieza.C1, IslaPieza.V1)), onAllGames = {}, onPlaceGame = {}, onPlaceGameCard = {}, onStop = {}, onPlay = {}, onResume = {}, onBaseline = {},
            modifier = Modifier.padding(top = 24.dp, bottom = 163.dp), pieceStore = store, holdPieces = hold, onPiecePlan = { plans += it }
          )
        }
      }
    }
    rule.waitForIdle()
    assertTrue("con la medalla encima, Hoy no revela ni anota", plans.isEmpty() && store.saves == 0)
    hold = false
    rule.waitForIdle()
    assertEquals(listOf(listOf(IslaPieza.V1)), plans)
    assertEquals(setOf("c1", "v1"), store.saved)
  }

  // ---- Lo que dice el lector de pantalla y lo que no se mueve

  @Test
  fun la_descripcion_de_la_isla_nombra_las_piezas() {
    show(ui(listOf(IslaPieza.C1, IslaPieza.V1, IslaPieza.VO1)))
    rule.onNodeWithContentDescription("Tu isla: hoy la ruta pasa por el Observatorio, el Hangar y la Biblioteca · Tu isla tiene: dos arbustos, banderines y un muelle").assertExists()
  }

  @Test
  fun la_descripcion_sin_piezas_es_la_de_siempre() {
    show(ui(emptyList()))
    rule.onNodeWithContentDescription("Tu isla: hoy la ruta pasa por el Observatorio, el Hangar y la Biblioteca").assertExists()
  }

  @Test
  fun diez_piezas_mas_no_cambian_el_tamano_de_la_isla_ni_de_sus_toques() {
    var current by mutableStateOf(ui(emptyList()))
    rule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(873.dp)) {
          BoardBackground()
          HomeContent(ui = current, onAllGames = {}, onPlaceGame = {}, onPlaceGameCard = {}, onStop = {}, onPlay = {}, onResume = {}, onBaseline = {}, modifier = Modifier.padding(top = 24.dp, bottom = 163.dp))
        }
      }
    }
    rule.waitForIdle()
    val tags = listOf("home_isla", "isla_observatorio", "isla_antena", "isla_biblioteca", "isla_hangar")
    val before = tags.associateWith { rule.onNodeWithTag(it).fetchSemanticsNode().boundsInRoot }
    current = ui(IslaPieza.entries)
    rule.waitForIdle()
    assertEquals(IslaPieza.entries.map { it.id }, drawn())
    for (tag in tags) assertEquals("«$tag» no cambia con las piezas", before.getValue(tag), rule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot)
  }
}
