package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.assertContentDescriptionEquals
import androidx.compose.ui.test.assertCountEquals
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.assertTextEquals
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.ui.components.BoardBackground
import com.example.ui.screens.FirstFlightScreen
import com.example.ui.screens.FlightActions
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * Las farolas de etapa de la cabecera del Primer vuelo (Tarea 132, A4): seis farolas y el paso en palabras, sin «minutos»; con el ancho más angosto que cuidamos (360 dp) y la letra al 200 % todas caben a la vista y el texto del paso se lee.
 */
@RunWith(RobolectricTestRunner::class)
@Config(qualifiers = "w412dp-h915dp", sdk = [36])
class FirstFlightLampsTest {
  @get:Rule val rule = createComposeRule()

  private fun frame(state: FlightState, widthDp: Int = 412, fontScale: Float = 1f) = rule.setContent {
    CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, fontScale)) {
      NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(780.dp)) { BoardBackground(); FirstFlightScreen(state, emptyList(), FlightActions.None) } }
    }
  }

  @Test
  fun la_cabecera_tiene_seis_farolas_y_el_paso_sin_minutos() {
    frame(FlightState(FlightMode.FULL, FlightStep.JUEGO_1, name = "Ana"))
    rule.onNodeWithTag("flight_lamps").assertIsDisplayed().assertContentDescriptionEquals("Paso 1 de 15 · Memoria. 1 de 6 farolas encendidas")
    for (i in 0..5) rule.onNodeWithTag("flight_lamp_$i", useUnmergedTree = true).assertIsDisplayed()
    rule.onNodeWithTag("flight_step_text").assertTextEquals("Paso 1 de 15 · Memoria")
    // ya no se promete cuánto falta (la página del juego sí dice cuánto dura la ronda: eso es del juego, no del recorrido)
    rule.onAllNodesWithText("Faltan unos", substring = true).assertCountEquals(0)
    rule.onAllNodesWithText("Falta 1 minuto", substring = true).assertCountEquals(0)
  }

  @Test
  fun las_pantallas_que_no_son_de_juego_dicen_el_paso_a_secas() {
    // Tarea 143 (A2): la pregunta de la edad no nombra el área del juego anterior («Paso 2 de 6 · Memoria»); el renglón sol ya dice «Para ajustar el ritmo»
    frame(FlightState(FlightMode.FULL, FlightStep.EDAD, name = "Ana", measured = mapOf("secuencia" to 0.5f)))
    rule.onNodeWithTag("flight_step_text").assertTextEquals("Paso 3 de 15")
    rule.onNodeWithTag("flight_lamps").assertContentDescriptionEquals("Paso 3 de 15. 2 de 6 farolas encendidas")
  }

  @Test
  fun las_metas_describen_cada_area_con_las_mismas_palabras_que_Juegos() {
    // Tarea 150 (B1.9): una sola descripción por área (`DomainType.tagline`), no «recordar, retener» aquí y «recordar y ubicar» en Juegos
    frame(FlightState(FlightMode.FULL, FlightStep.METAS, name = "Ana"))
    for (d in com.example.model.DomainType.entries) rule.onAllNodesWithText(d.tagline, substring = true).assertCountEquals(1)
  }

  @Test
  fun en_hola_la_primera_ya_esta_encendida() {
    frame(FlightState(FlightMode.FULL, FlightStep.HOLA))
    rule.onNodeWithTag("flight_step_text").assertTextEquals("Empezamos juntos")
    rule.onNodeWithTag("flight_lamps").assertContentDescriptionEquals("Empezamos juntos. 1 de 6 farolas encendidas")
    // el saludo ya no promete tiempo: «Son 4 juegos cortos.»
    rule.onNodeWithText("Son 4 juegos cortos.", substring = true).assertIsDisplayed()
    rule.onAllNodesWithText("Son unos 5 minutos", substring = true).assertCountEquals(0)
  }

  @Test
  fun los_juegos_no_traen_un_segundo_indicador_de_avance() {
    // Ricardo, 18-oct: dos indicadores apilados confunden a los 60+; las farolas ya dicen en qué juego va
    frame(FlightState(FlightMode.FULL, FlightStep.TARJETA_1, name = "Ana", measured = mapOf("secuencia" to 0.5f)))
    rule.onAllNodes(androidx.compose.ui.test.hasContentDescription("Juego 1 de 4")).assertCountEquals(0)
    rule.onAllNodes(androidx.compose.ui.test.hasContentDescription("1 de 4 juegos hechos")).assertCountEquals(0)
    rule.onNodeWithTag("flight_step_text").assertIsDisplayed().assertTextEquals("Paso 2 de 15 · Memoria")
  }

  @Test
  fun en_360_dp_con_letra_2_0_las_seis_farolas_y_el_texto_caben() {
    frame(FlightState(FlightMode.FULL, FlightStep.PUNTO, name = "Ana", measured = mapOf("secuencia" to 0.5f, "freno" to 0.5f, "aterrizaje" to 0.5f, "meteoros" to 0.5f)), widthDp = 360, fontScale = 2f)
    val rootW = rule.onRoot().fetchSemanticsNode().size.width
    for (i in 0..5) {
      val b = rule.onNodeWithTag("flight_lamp_$i", useUnmergedTree = true).fetchSemanticsNode().boundsInRoot
      assertTrue("la farola $i queda fuera de la pantalla: $b de $rootW px", b.left >= 0f && b.right <= rootW + 0.5f && b.width > 0f)
    }
    // las seis cajas miden lo mismo y no se pisan
    val widths = (0..5).map { rule.onNodeWithTag("flight_lamp_$it", useUnmergedTree = true).fetchSemanticsNode().boundsInRoot.width }
    assertEquals(1, widths.map { Math.round(it) }.toSet().size)
    rule.onNodeWithTag("flight_step_text").assertIsDisplayed().assertTextEquals("Paso 13 de 15")
    rule.onNodeWithTag("flight_lamps").assertContentDescriptionEquals("Paso 13 de 15. 5 de 6 farolas encendidas")
  }

  @Test
  fun la_evaluacion_repetida_tiene_cinco_farolas() {
    frame(FlightState(FlightMode.GAMES, FlightStep.JUEGO_2, measured = mapOf("secuencia" to 0.5f)))
    rule.onNodeWithTag("flight_step_text").assertTextEquals("Juego 2 de 4 · Atención")
    for (i in 0..4) rule.onNodeWithTag("flight_lamp_$i", useUnmergedTree = true).assertIsDisplayed()
    assertEquals("solo cinco farolas (cuatro juegos y «Listo»)", 0, rule.onAllNodes(androidx.compose.ui.test.hasTestTag("flight_lamp_5"), useUnmergedTree = true).fetchSemanticsNodes().size)
  }
}
