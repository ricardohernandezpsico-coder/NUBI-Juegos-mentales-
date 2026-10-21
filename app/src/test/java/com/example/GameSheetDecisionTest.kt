package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.SemanticsMatcher
import androidx.compose.ui.test.assert
import androidx.compose.ui.test.assertContentDescriptionEquals
import androidx.compose.ui.test.assertIsFocused
import androidx.compose.ui.test.assertIsSelected
import androidx.compose.ui.test.assertTextEquals
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.PlayMode
import com.example.model.AgeBand
import com.example.model.GameRegistry
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.cardData
import com.example.ui.theme.NeuroVidaTheme
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
 * La ficha con UNA decisión visible (Tarea 140, A3.1 y A3.2 de la spec de adopción): plegada por defecto (la línea de resumen, «Cambiar cómo jugar» y «Jugar · …» con lo elegido; las dos secciones ni se componen), desplegada con «Listo», elegir no pliega sola,
 * el foco va a la casilla elegida al desplegar y vuelve al botón al plegar, y el lector de pantalla recibe lo elegido, el estado y la acción del botón y la región viva del título.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class GameSheetDecisionTest {
  @get:Rule val rule = createComposeRule()

  private val day = 86_400_000L
  private val now = 400 * day

  /** La ficha de [gameId] con el estado que la ventana real le da (la elección vive arriba, en quien la usa). */
  private fun setSheet(gameId: String = "radar", widthDp: Int = 393, heightDp: Int = 825, fontScale: Float = 1f, mode: PlayMode = PlayMode.A_TU_MEDIDA, timed: Boolean = true, onPlay: () -> Unit = {}) {
    val g = GameRegistry.getById(gameId)!!
    val data = cardData(g, g.title, now - day, now, 0.55f, emptyList(), playsLast14 = 3)
    rule.setContent {
      NeuroVidaTheme {
        CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, fontScale)) {
          var choice by remember { mutableStateOf(mode) }
          var withClock by remember { mutableStateOf(timed) }
          Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
            Box(Modifier.fillMaxWidth()) {
              GameSheetContent(data, AgeBand.SENIOR, expertOpen = false, choice = choice, onChoose = { choice = it }, onPlay = onPlay, timed = withClock, onChooseTimed = { withClock = it })
            }
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun clickLabelOf(tag: String): String? = rule.onNodeWithTag(tag).fetchSemanticsNode().config[SemanticsActions.OnClick].label

  private fun textsOf(tag: String): List<String> = rule.onNodeWithTag(tag).fetchSemanticsNode().config[SemanticsProperties.Text].map { it.text }

  private val sections = listOf("mode_SUAVE", "mode_A_TU_MEDIDA", "mode_DESAFIO", "mode_EXPERTO", "reto_sin", "reto_con")

  @Test
  fun la_ficha_parte_plegada_con_la_linea_de_resumen_y_las_secciones_ni_se_componen() {
    setSheet()
    rule.onNodeWithTag("sheet_summary").assertTextEquals("A tu medida · con reloj (2 min)")
    rule.onNodeWithTag("sheet_change").assertTextEquals("Cambiar cómo jugar")
    for (tag in sections) rule.onNodeWithTag(tag).assertDoesNotExist()
    rule.onNodeWithText("¿Cómo quieres jugar?").assertDoesNotExist()
    rule.onNodeWithText("¿Con reloj?").assertDoesNotExist()
  }

  @Test
  fun cambiar_como_jugar_dice_al_lector_de_pantalla_lo_elegido_su_estado_y_su_accion() {
    setSheet()
    rule.onNodeWithTag("sheet_change")
      .assert(SemanticsMatcher.expectValue(SemanticsProperties.Role, Role.Button))
      .assertContentDescriptionEquals("Cambiar cómo jugar. Ahora: a tu medida, con reloj")
      .assert(SemanticsMatcher.expectValue(SemanticsProperties.StateDescription, "opciones ocultas"))
    assertEquals("Mostrar opciones", clickLabelOf("sheet_change"))
  }

  @Test
  fun tocar_cambiar_despliega_las_dos_secciones_y_el_mismo_boton_dice_listo() {
    setSheet()
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("sheet_change")
      .assertTextEquals("Listo")
      .assertContentDescriptionEquals("Listo. Ahora: a tu medida, con reloj")
      .assert(SemanticsMatcher.expectValue(SemanticsProperties.StateDescription, "opciones visibles"))
    assertEquals("Ocultar opciones", clickLabelOf("sheet_change"))
    for (tag in sections) rule.onNodeWithTag(tag).assertExists()
    rule.onNodeWithTag("mode_A_TU_MEDIDA").assertIsSelected()
    rule.onNodeWithTag("reto_con").assertIsSelected()
    // el título de la sección es una región viva: el lector anuncia que aparecieron las opciones
    rule.onNodeWithText("¿Cómo quieres jugar?").assert(SemanticsMatcher.expectValue(SemanticsProperties.LiveRegion, LiveRegionMode.Polite))
  }

  @Test
  fun elegir_un_modo_o_el_reloj_no_pliega_sola_y_el_resumen_y_jugar_lo_dicen_hasta_que_la_persona_toca_listo() {
    setSheet()
    rule.onNodeWithTag("sheet_change").performClick()
    rule.onNodeWithTag("mode_DESAFIO").performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("mode_DESAFIO").assertIsSelected()
    for (tag in sections) rule.onNodeWithTag(tag).assertExists()                       // no se plegó sola
    rule.onNodeWithTag("sheet_summary").assertTextEquals("Desafío · con reloj (2 min)")
    rule.onNodeWithTag("sheet_play").assertContentDescriptionEquals("Jugar, en Desafío, con reloj")
    rule.onNodeWithTag("reto_sin").performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("reto_sin").assertIsSelected()
    for (tag in sections) rule.onNodeWithTag(tag).assertExists()
    rule.onNodeWithTag("sheet_summary").assertTextEquals("Desafío · sin reloj")
    rule.onNodeWithTag("sheet_play").assertContentDescriptionEquals("Jugar, en Desafío, sin reloj")
    rule.onNodeWithTag("sheet_change").performClick()                                  // «Listo»: ahora sí pliega
    rule.waitForIdle()
    for (tag in sections) rule.onNodeWithTag(tag).assertDoesNotExist()
    rule.onNodeWithTag("sheet_change").assertTextEquals("Cambiar cómo jugar")
    rule.onNodeWithTag("sheet_summary").assertTextEquals("Desafío · sin reloj")        // y lo elegido se queda
  }

  /** El foco (A3.2). Un `clickable` de Compose solo acepta el foco con teclado o mando, no en modo táctil: la casilla y el botón llevan `focusable()` para que el foco (y con él el lector de pantalla) llegue a donde la ficha lo manda. */
  @Test
  fun al_desplegar_el_foco_va_a_la_casilla_elegida_y_al_plegar_vuelve_al_boton() {
    setSheet(mode = PlayMode.DESAFIO)
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("mode_DESAFIO").assertIsFocused()
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("sheet_change").assertIsFocused()
  }

  /** Ricardo (19-oct): la ficha plegada «un poco más arriba, no tan abajo»: va CENTRADA en el espacio libre entre las barras; desplegada, crece hacia arriba y hacia abajo. */
  @Test
  fun la_tarjeta_plegada_va_centrada_y_al_desplegar_crece_hacia_arriba_y_hacia_abajo() {
    setSheet(heightDp = 1400)                                         // un marco alto: sobra lugar arriba y abajo
    val tolerance = 10f * rule.density.density                        // la losa reserva 6 dp abajo para su sombra: «centrada» con unos dp de margen
    val frame = rule.onNodeWithTag("frame").fetchSemanticsNode().boundsInRoot
    val folded = rule.onNodeWithTag("game_sheet").fetchSemanticsNode().boundsInRoot
    assertTrue("plegada: sobra lugar arriba (${folded.top - frame.top}) y es parecido al de abajo (${frame.bottom - folded.bottom})", folded.top - frame.top > 100f * rule.density.density && Math.abs((folded.top - frame.top) - (frame.bottom - folded.bottom)) <= tolerance)
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    val open = rule.onNodeWithTag("game_sheet").fetchSemanticsNode().boundsInRoot
    assertTrue("desplegada: crece hacia arriba (arriba ${folded.top} → ${open.top}) y hacia abajo (abajo ${folded.bottom} → ${open.bottom})", open.top < folded.top - 1f && open.bottom > folded.bottom + 1f)
    assertTrue("y sigue centrada", Math.abs((open.top - frame.top) - (frame.bottom - open.bottom)) <= tolerance)
  }

  /** El cambio de tamaño es suave: 200 ms (NubiMotion, cambio de estado) y de un salto con «quitar animaciones». */
  @Test
  fun las_secciones_cambian_de_altura_con_nubi_motion_y_de_un_salto_con_quitar_animaciones() {
    val source = java.io.File("src/main/java/com/example/ui/screens/GamesLibraryScreen.kt").readText()
    val sheet = source.substring(source.indexOf("internal fun GameSheetContent("), source.indexOf("private fun SheetPlayButton("))
    assertTrue("entra y sale cambiando de altura", sheet.contains("expandVertically(") && sheet.contains("shrinkVertically("))
    assertTrue("con la duración de NubiMotion (cambio de estado, 200 ms)", sheet.contains("tween(NubiMotion.STATE_MS, easing = NubiMotion.Standard)"))
    assertTrue("y sin animación con «quitar animaciones»", sheet.contains("if (reduceMotion) EnterTransition.None") && sheet.contains("if (reduceMotion) ExitTransition.None"))
    assertEquals(200, com.example.ui.components.NubiMotion.STATE_MS)
  }

  @Test
  fun jugar_llama_a_onPlay_con_lo_elegido_plegada_o_desplegada() {
    var plays = 0
    setSheet(onPlay = { plays++ })
    rule.onNodeWithTag("sheet_play").performClick()
    rule.onNodeWithTag("sheet_change").performClick()
    rule.onNodeWithTag("sheet_play").performClick()
    assertEquals(2, plays)
  }

  @Test
  fun jugar_dice_lo_elegido_en_una_linea_o_en_dos_y_nunca_parte_una_palabra() {
    setSheet()
    // plegada (Tarea 152, C5): el botón dice «Jugar» a secas; la línea de arriba ya dice lo elegido
    assertEquals(listOf("Jugar"), textsOf("sheet_play"))
    rule.onNodeWithTag("sheet_play").assertContentDescriptionEquals("Jugar, a tu medida, con reloj")
    // desplegada, el botón lleva lo elegido (que cambia al tocar): «Jugar · a tu medida · con reloj» entero, o «Jugar» y debajo «a tu medida · con reloj»
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    val normal = textsOf("sheet_play")
    assertTrue("en un teléfono común: «Jugar · a tu medida · con reloj» entero, o «Jugar» y debajo «a tu medida · con reloj»: $normal",
      normal == listOf("Jugar · a tu medida · con reloj") || normal == listOf("Jugar", "a tu medida · con reloj"))
  }

  @Test
  fun con_letra_130_o_en_360_dp_jugar_pasa_a_dos_lineas_y_cada_una_va_entera() {
    setSheet(fontScale = 1.3f)
    assertEquals("plegada: «Jugar» a secas", listOf("Jugar"), textsOf("sheet_play"))
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    assertEquals(listOf("Jugar", "a tu medida · con reloj"), textsOf("sheet_play"))
  }

  @Test
  fun en_un_telefono_de_360_dp_jugar_pasa_a_dos_lineas() {
    setSheet(widthDp = 360, mode = PlayMode.DESAFIO)
    assertEquals("plegada: «Jugar» a secas", listOf("Jugar"), textsOf("sheet_play"))
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    assertEquals(listOf("Jugar", "en Desafío · con reloj"), textsOf("sheet_play"))
  }

  @Test
  fun con_letra_200_jugar_sigue_en_dos_textos_enteros() {
    setSheet(fontScale = 2f, timed = false, mode = PlayMode.SUAVE)
    assertEquals("plegada: «Jugar» a secas", listOf("Jugar"), textsOf("sheet_play"))
    rule.onNodeWithTag("sheet_change").performScrollTo().performClick()           // con letra 2,0 el botón queda bajo el pliegue de la ficha
    rule.waitForIdle()
    assertEquals(listOf("Jugar", "suave · sin reloj"), textsOf("sheet_play"))
  }

  @Test
  fun un_juego_sin_reto_dice_solo_a_tu_medida_y_no_tiene_la_seccion_del_reloj() {
    setSheet(gameId = "correo", timed = true)
    rule.onNodeWithTag("sheet_summary").assertTextEquals("A tu medida")
    rule.onNodeWithTag("sheet_change").assertContentDescriptionEquals("Cambiar cómo jugar. Ahora: a tu medida")
    rule.onNodeWithTag("sheet_play").assertContentDescriptionEquals("Jugar, a tu medida")
    assertEquals("plegada: «Jugar» a secas (Tarea 152, C5)", listOf("Jugar"), textsOf("sheet_play"))
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    val play = textsOf("sheet_play")
    assertTrue("desplegada, Jugar · a tu medida (una línea o dos): $play", play == listOf("Jugar · a tu medida") || play == listOf("Jugar", "a tu medida"))
    rule.onNodeWithTag("mode_A_TU_MEDIDA").assertExists()
    rule.onNodeWithTag("reto_sin").assertDoesNotExist()
    rule.onNodeWithTag("reto_con").assertDoesNotExist()
    rule.onNodeWithText("¿Con reloj?").assertDoesNotExist()
  }
}
