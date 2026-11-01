package com.example

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.SemanticsMatcher
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.NBSP
import com.example.data.TimeTrialStats
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

  // ---- Rescate relámpago cuenta por cápsula (Tarea 208): la regla también va en el final de invitado

  private var widthState by mutableStateOf(393)
  private var fontState by mutableStateOf(1f)
  private var trialState by mutableStateOf<GuestTrial?>(null)

  /** UNA sola pantalla por prueba (un solo `setContent`); el juego, el ancho y la letra se cambian después. */
  private fun mountFinal(trial: GuestTrial, widthDp: Int = 393, fontScale: Float = 1f) {
    trialState = trial; widthState = widthDp; fontState = fontScale
    rule.setContent {
      val d = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(d.density, fontState)) {
        NeuroVidaTheme { Box(androidx.compose.ui.Modifier.width(widthState.dp).height(873.dp)) { trialState?.let { GuestTrialScreen(trial = it, onAgain = {}, onDone = {}) } } }
      }
    }
    rule.waitForIdle()
  }

  private fun tops(vararg tags: String): List<Float> = tags.map { rule.onNodeWithTag(it, useUnmergedTree = true).fetchSemanticsNode().layoutInfo.coordinates.positionInRoot().y }

  /** NUNCA una palabra partida a la mitad: el renglón de cada texto termina en un espacio o al final. */
  private fun assertNoWordSplit() {
    for (node in rule.onAllNodes(SemanticsMatcher.keyIsDefined(SemanticsActions.GetTextLayoutResult), useUnmergedTree = true).fetchSemanticsNodes()) {
      val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
      node.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
      val l = results.firstOrNull() ?: continue
      val text = l.layoutInput.text.text
      for (line in 0 until l.lineCount - 1) {
        val end = l.getLineEnd(line, visibleEnd = false)
        assertTrue("«$text» parte una palabra a la mitad en el renglón ${line + 1}", end >= text.length || text[end - 1] == ' ' || text[end] == ' ' || text[end - 1] == '\n')
      }
    }
  }

  @Test
  fun el_final_de_invitado_de_rescate_trae_la_regla_justo_despues_de_como_se_cuenta() {
    showFinal(GuestTrial("radar", 16, 2, AgeRange.R65_74))
    assertEquals("la misma frase del detalle, de TimeTrialStats", TimeTrialStats.RADAR_RULE, textOf("guest_trial_radar_rule"))
    val (line, rule_, range, note) = tops("guest_trial_line", "guest_trial_radar_rule", "guest_trial_range", "guest_trial_note")
    assertTrue("después de «cómo se cuenta»", rule_ > line)
    assertTrue("y antes de la edad y del aviso de que no se guardó", rule_ < range && range < note)
  }

  @Test
  fun el_final_de_invitado_de_otros_juegos_no_trae_la_regla_de_rescate() {
    mountFinal(GuestTrial("radar", 16, 2, AgeRange.R45_54))
    assertEquals("Rescate sí la lleva", 1, rule.onAllNodesWithTagStartingWith("guest_trial_radar_rule").size)
    for (game in listOf("stroop", "disparate", "acoplamiento")) {
      trialState = GuestTrial(game, 36, 2, AgeRange.R45_54); rule.waitForIdle()
      assertEquals("«$game» no lleva la regla de Rescate", 0, rule.onAllNodesWithTagStartingWith("guest_trial_radar_rule").size)
      assertTrue("pero sí «cómo se cuenta»", textOf("guest_trial_line").isNotEmpty())
    }
  }

  @Test
  fun la_regla_de_rescate_del_invitado_no_parte_palabras_con_letra_2_0_ni_en_360_dp() {
    mountFinal(GuestTrial("radar", 16, 2, AgeRange.R65_74))
    assertNoWordSplit()
    for ((w, f) in listOf(393 to 1.3f, 393 to 2.0f, 360 to 1.0f, 360 to 2.0f)) {
      widthState = w; fontState = f; rule.waitForIdle()
      assertEquals("la frase entera con ${w} dp y letra $f", TimeTrialStats.RADAR_RULE, textOf("guest_trial_radar_rule"))
      assertNoWordSplit()
    }
  }

  /** Las etiquetas de prueba de TODOS los nodos (también los de un diálogo) que empiezan con [prefix], en orden. */
  private fun androidx.compose.ui.test.junit4.ComposeContentTestRule.onAllNodesWithTagStartingWith(prefix: String): List<String> =
    onAllNodes(SemanticsMatcher("etiqueta $prefix…") { it.config.getOrNull(SemanticsProperties.TestTag)?.startsWith(prefix) == true }).fetchSemanticsNodes().mapNotNull { it.config.getOrNull(SemanticsProperties.TestTag) }
}
