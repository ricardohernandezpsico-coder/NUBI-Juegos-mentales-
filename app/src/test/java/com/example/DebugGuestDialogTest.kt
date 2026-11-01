package com.example

import android.app.Application
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.SemanticsMatcher
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.test.core.app.ApplicationProvider
import com.example.data.NBSP
import com.example.games.GuestTrial
import com.example.games.GuestTrialScreen
import com.example.model.AgeBand
import com.example.model.AgeRange
import com.example.ui.screens.DebugTools
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.NeuroVidaViewModel
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
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
 * «[Debug] Contrarreloj de invitado» pregunta la EDAD antes de jugar (Tarea 205c): las seis franjas de 18 en adelante (nunca 13 a 17), «Cancelar» no lanza nada, y el final de invitado dice la edad elegida para que Ricardo la anote junto a la marca.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class DebugGuestDialogTest {
  @get:Rule val rule = createComposeRule()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    TestSupport.fastFrames()
  }

  @After fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun show(): NeuroVidaViewModel {
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    rule.setContent { NeuroVidaTheme { Column(androidx.compose.ui.Modifier.verticalScroll(rememberScrollState())) { DebugTools(viewModel = vm, userId = 1L, ageBand = AgeBand.ADULT) } } }
    rule.waitForIdle()
    return vm
  }

  private val ageButtons = SemanticsMatcher("un botón de edad") { it.config.getOrNull(SemanticsProperties.TestTag)?.startsWith("btn_guest_age_r") == true }

  @Test
  fun tocar_un_juego_de_invitado_pregunta_las_seis_edades_de_adulto_y_no_lanza_nada_todavia() {
    val vm = show()
    rule.onNodeWithTag("btn_debug_guest_radar").performScrollTo().performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("guest_age_dialog").assertIsDisplayed()
    val tags = rule.onAllNodesWithTagStartingWith("btn_guest_age_r")
    assertEquals("las seis franjas de 18 en adelante", listOf("btn_guest_age_r18_29", "btn_guest_age_r30_44", "btn_guest_age_r45_54", "btn_guest_age_r55_64", "btn_guest_age_r65_74", "btn_guest_age_r75_plus"), tags)
    assertFalse("nunca la de 13 a 17", tags.any { "r13_17" in it })
    assertNull("preguntar no lanza la partida", vm.activeGame.value)
  }

  @Test
  fun elegir_una_edad_lanza_la_partida_de_invitado_con_esa_edad() {
    val vm = show()
    rule.onNodeWithTag("btn_debug_guest_stroop").performScrollTo().performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("btn_guest_age_r65_74").performClick()
    rule.waitForIdle()
    assertEquals("stroop", vm.activeGame.value?.gameDef?.id)
    assertEquals(AgeRange.R65_74, vm.activeGame.value?.guestRange)
    assertEquals("la pista de hoy", vm.todayKey(), vm.activeGame.value?.timeTrialDay)
    assertEquals("y la pregunta se cierra", 0, rule.onAllNodesWithTagStartingWith("guest_age_dialog").size)
  }

  @Test
  fun cancelar_la_pregunta_no_lanza_nada() {
    val vm = show()
    rule.onNodeWithTag("btn_debug_guest_acoplamiento").performScrollTo().performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("btn_guest_age_cancel").performClick()
    rule.waitForIdle()
    assertNull(vm.activeGame.value)
    assertEquals(0, rule.onAllNodesWithTagStartingWith("guest_age_dialog").size)
  }

  // ---- el final de invitado

  private fun showFinal(trial: GuestTrial) {
    rule.setContent { NeuroVidaTheme { GuestTrialScreen(trial = trial, onAgain = {}, onDone = {}) } }
    rule.waitForIdle()
  }

  private fun textOf(tag: String): String =
    rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Text)?.joinToString("") { it.text }.orEmpty().replace(NBSP, " ")

  @Test
  fun el_final_de_invitado_dice_la_marca_y_la_edad_elegida() {
    showFinal(GuestTrial("stroop", 36, 2, AgeRange.R45_54))
    assertEquals("34 aciertos", textOf("guest_trial_mark"))
    assertEquals("Edad elegida: 45 a 54 años.", textOf("guest_trial_range"))
    assertTrue("y dice que no se guardó", "no se guardó" in textOf("guest_trial_note"))
  }

  @Test
  fun si_la_sesion_se_perdio_el_final_calla_la_edad_en_vez_de_inventarla() {
    showFinal(GuestTrial("stroop", 36, 2, null))
    assertEquals(0, rule.onAllNodesWithTagStartingWith("guest_trial_range").size)
    assertEquals("34 aciertos", textOf("guest_trial_mark"))
  }

  /** Las etiquetas de prueba de TODOS los nodos (también los de un diálogo) que empiezan con [prefix], en orden. */
  private fun androidx.compose.ui.test.junit4.ComposeContentTestRule.onAllNodesWithTagStartingWith(prefix: String): List<String> =
    onAllNodes(SemanticsMatcher("etiqueta $prefix…") { it.config.getOrNull(SemanticsProperties.TestTag)?.startsWith(prefix) == true }).fetchSemanticsNodes().mapNotNull { it.config.getOrNull(SemanticsProperties.TestTag) }
}
