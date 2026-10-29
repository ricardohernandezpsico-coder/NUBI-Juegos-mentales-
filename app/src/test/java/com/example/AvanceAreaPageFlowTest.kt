package com.example

import android.app.Application
import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.assertIsNotDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.hasScrollAction
import androidx.compose.ui.test.performScrollToNode
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.GamePlayResult
import com.example.model.UserSettings
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.ProgressScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.example.viewmodel.NeuroVidaViewModel
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * Avance con puntos, con la pantalla REAL y su ViewModel (Tarea 159): con partidas se ven las cuatro filas y «Tus mejores marcas»; tocar una fila abre la página del área SIN cambiar de pestaña; Atrás (la tecla del teléfono) vuelve a la lista en el mismo lugar; la página no tiene
 * ningún «Jugar» (Avance mira, Hoy juega); y sin partidas, Nubi con su línea y nada de filas vacías.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class AvanceAreaPageFlowTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()
  private val day = 24L * 60 * 60 * 1000

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    TestSupport.fastFrames()          // sin esto, `al_volver_de_la_pagina_la_lista_esta_donde_estaba` (deslizar la lista) tardaba ~140 s: ver `TestSupport.fastFrames`
  }

  @After
  fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  /** Unas semanas de uso de siete juegos de las cuatro áreas, cada uno con su avance subiendo. */
  private fun seed(): Int = runBlocking {
    val db = NeuroVidaDatabase.getDatabase(app)
    val repo = (app as NeuroVidaApplication).repository
    val now = System.currentTimeMillis()
    db.userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity())
    val games = listOf("secuencia", "parejas", "bodega", "radar", "stroop", "calculo", "anagramas")
    var n = 0
    for ((k, id) in games.withIndex()) for (round in 0 until 4) {
      repo.recordGameResult(
        GamePlayResult(
          gameId = id, score = 70, correctAnswers = 8, totalTrials = 12, timed = false, level = 2, timestamp = now - (28 - 7 * round) * day + k * 3_600_000L, endRating = 0.25f + 0.08f * round + 0.01f * k,
          modeTrials = 10, modeHits = 8
        ),
        countsForDailySession = false
      )
      n += 1
    }
    n
  }

  private fun withData(): NeuroVidaViewModel {
    val n = seed()
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(30_000) { vm.gameHistory.value.size >= n }
    vm.setTab(AppTab.PROGRESO)
    return vm
  }

  private fun show(vm: NeuroVidaViewModel, heightDp: Int = 873) {
    rule.setContent {
      NeuroVidaTheme {
        CompositionLocalProvider(LocalReduceMotion provides true, com.example.ui.components.LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
          Box(Modifier.width(393.dp).height(heightDp.dp)) { ProgressScreen(viewModel = vm) }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun count(tag: String) = rule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().size
  private fun textCount(text: String) = rule.onAllNodes(hasText(text, substring = true)).fetchSemanticsNodes().size
  /** El nodo no está a la vista: o no está compuesto (la lista perezosa lo soltó) o está fuera de la pantalla. */
  private fun assertOffscreen(tag: String) { val n = rule.onAllNodes(hasTestTag(tag)); if (n.fetchSemanticsNodes().isNotEmpty()) n[0].assertIsNotDisplayed() }
  private fun pressBack() { rule.runOnUiThread { rule.activity.onBackPressedDispatcher.onBackPressed() }; rule.waitForIdle() }

  @Test
  fun con_partidas_Avance_muestra_su_titulo_las_cuatro_filas_y_las_mejores_marcas() {
    show(withData())
    rule.onNodeWithTag("avance_title").assertIsDisplayed()
    rule.onNodeWithTag("avance_week").assertIsDisplayed()
    for (a in listOf("memoria", "atencion", "razonamiento", "lenguaje")) rule.onNodeWithTag("progress_area_$a").assertExists()
    assertEquals("las cuatro áreas dicen sus puntos: «de 1000 puntos» las que ya suman y «Aún sin medir» las que no", 4, textCount("de 1000 puntos") + textCount("Aún sin medir"))
    assertEquals("ya no hay «Tus medidas», «Tus últimas partidas» ni el enlace a las medallas", 0, textCount("Tus medidas") + textCount("Tus últimas partidas") + textCount("ver en Tú") + textCount("Ver más"))
    assertEquals(0, count("btn_discovery_play"))
  }

  @Test
  fun tocar_una_fila_abre_la_pagina_del_area_sin_cambiar_de_pestana_y_Atras_vuelve_a_la_lista() {
    val vm = withData()
    show(vm)
    rule.onNodeWithTag("progress_area_memoria").performClick(); rule.waitForIdle()
    assertEquals("MEMORIA", vm.avanceArea.value)
    assertEquals("la barra sigue en Avance", AppTab.PROGRESO, vm.currentTab.value)
    rule.onNodeWithTag("avance_area_page_memoria").assertIsDisplayed()
    rule.onNodeWithTag("page_title").assertIsDisplayed()
    assertEquals("la lista de áreas no se ve mientras la página está abierta", 0, count("progress_area_memoria"))
    pressBack()
    assertNull("Atrás cierra la página", vm.avanceArea.value)
    rule.onNodeWithTag("progress_area_memoria").assertExists()
    assertEquals(0, count("avance_area_page_memoria"))
  }

  @Test
  fun la_flecha_de_la_pagina_tambien_vuelve_y_la_pagina_no_tiene_jugar() {
    val vm = withData()
    show(vm)
    rule.onNodeWithTag("progress_area_atencion").performClick(); rule.waitForIdle()
    rule.onNodeWithTag("page_title").assertIsDisplayed()
    assertEquals("Atención", rule.onNodeWithTag("page_title", useUnmergedTree = true).fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.Text].joinToString(""))
    assertEquals("ningún «Jugar» ni botón de jugar en la página", 0, textCount("Jugar") + count("place_play") + count("btn_discovery_play"))
    rule.onNodeWithTag("area_back", useUnmergedTree = true).performClick(); rule.waitForIdle()
    assertNull(vm.avanceArea.value)
    rule.onNodeWithTag("progress_area_atencion").assertExists()
  }

  @Test
  fun al_volver_de_la_pagina_la_lista_esta_donde_estaba() {
    val vm = withData()
    show(vm, heightDp = 520)                                          // con los cuatro cuadros de 2 × 2 (Tarea 174) toda la lista cabe en 873 dp: una ventana baja para que haya algo que deslizar
    rule.onNode(hasScrollAction()).performScrollToNode(hasTestTag("btn_profile_baseline")); rule.waitForIdle()
    assertOffscreen("avance_week")                                    // la línea de la semana quedó arriba, fuera de la vista
    rule.onNodeWithTag("progress_area_lenguaje").performClick(); rule.waitForIdle()
    assertEquals("LENGUAJE", vm.avanceArea.value)
    pressBack()
    rule.onNodeWithTag("progress_area_lenguaje").assertIsDisplayed()
    assertOffscreen("avance_week")                                    // y sigue fuera de la vista: no volvió arriba del todo
    assertTrue(vm.avanceArea.value == null)
  }

  @Test
  fun sin_partidas_Avance_dice_la_frase_de_Nubi_y_no_dibuja_filas_vacias() {
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    vm.setTab(AppTab.PROGRESO)
    show(vm)
    rule.onNodeWithTag("nubi_empty").assertIsDisplayed()
    assertEquals(0, count("progress_area_memoria"))
    assertEquals("Tu semana empieza hoy", rule.onNodeWithTag("avance_week_line", useUnmergedTree = true).fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.Text].joinToString(""))
  }
}
