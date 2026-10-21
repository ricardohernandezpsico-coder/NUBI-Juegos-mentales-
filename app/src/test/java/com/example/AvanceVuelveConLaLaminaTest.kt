package com.example

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.ProgressScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.NeuroVidaViewModel
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * Avance vuelve con su lámina abierta (Tarea 155, regla única de vuelta: al terminar una partida se vuelve a la pestaña desde donde se lanzó, con la lámina como se dejó). El pager se retira mientras hay un juego encima, así que lo abierto lo recuerda
 * el ViewModel (`avanceArea`) y `ProgressScreen` lo restaura SIN animar al volver a componerse. Aquí se mira la pantalla real (el recorrido del ViewModel lo prueban `GameFlowTest` y sus tests de Avance).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class AvanceVuelveConLaLaminaTest {
  @get:Rule val rule = createComposeRule()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
  }

  @After
  fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun show(vm: NeuroVidaViewModel) {
    rule.setContent {
      NeuroVidaTheme {
        CompositionLocalProvider(LocalReduceMotion provides true, com.example.ui.components.LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
          Box(Modifier.width(393.dp).height(873.dp)) { ProgressScreen(viewModel = vm) }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun sheets() = rule.onAllNodes(hasTestTag("place_sheet")).fetchSemanticsNodes().size

  @Test
  fun con_una_lamina_recordada_Avance_la_abre_al_componerse() {
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    vm.setTab(com.example.viewmodel.AppTab.PROGRESO)
    vm.openAvanceArea("MEMORIA")                // lo que dejó una partida lanzada desde esa lámina
    show(vm)
    assertEquals("la lámina de Memoria está abierta", 1, sheets())
  }

  @Test
  fun sin_lamina_recordada_Avance_se_ve_con_sus_areas_y_sin_lamina() {
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    vm.setTab(com.example.viewmodel.AppTab.PROGRESO)
    show(vm)
    assertEquals(0, sheets())
  }

  @Test
  fun cerrar_la_lamina_con_la_X_la_olvida_en_el_ViewModel() {
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    vm.setTab(com.example.viewmodel.AppTab.PROGRESO)
    vm.openAvanceArea("MEMORIA")
    show(vm)
    rule.onNodeWithTag("place_close").performClick()
    rule.waitForIdle()
    assertEquals(0, sheets())
    assertNull("ya no se reabre al volver de otra partida", vm.avanceArea.value)
  }
}
