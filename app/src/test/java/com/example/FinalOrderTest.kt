package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.hasAnyAncestor
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.onAllNodesWithTag
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.dp
import com.example.games.GameResultScreen
import com.example.model.GamePlayResult
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
 * El orden de la pantalla de resultado, en el final «B» (Tarea 146; `auditoria-fable/spec-final-B.md` §1): la placa compacta, la cifra, su unidad, UNA frase de avance, el truco, el botón de seguir, «Jugar de nuevo», «Ver el detalle de tu partida»,
 * «¿Por qué importa?» (solo las 3 primeras partidas del juego) y la nota. Los 17 juegos usan este orden; el truco sale una sola vez, dentro del final.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class FinalOrderTest {
  @get:Rule val composeTestRule = createComposeRule()

  // En la punta de la lengua: la medida («5 de 8 palabras») y un truco. Aterrizaje Lunar y los otros 15 también usan el final con sentido.
  private val punta = GamePlayResult(
    gameId = "anagramas", score = 74, correctAnswers = 6, totalTrials = 8, timed = false, level = 3,
    puntaSolo = 5, puntaPista = 2, puntaLetras = 0, puntaVista = 1
  )

  private fun show(
    result: GamePlayResult = punta, playsBefore: Int = 0, isDailyFlow: Boolean = false, dailyCompletedCount: Int = 0, nextGameId: String? = null,
    didLevelUp: Boolean = false, modeNote: String? = null
  ) {
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).height(915.dp).background(Color(0xFF050823))) {
          GameResultScreen(
            result = result, didLevelUp = didLevelUp, isDailyFlow = isDailyFlow, dailyCompletedCount = dailyCompletedCount, onPlayAgain = {}, onContinue = {},
            modeNote = modeNote, nextGameId = nextGameId, playsBefore = playsBefore
          )
        }
      }
    }
    composeTestRule.mainClock.advanceTimeBy(300)
  }

  private fun yOfTag(tag: String) = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
  private fun countTag(tag: String) = composeTestRule.onAllNodesWithTag(tag).fetchSemanticsNodes().size
  private fun countText(text: String, substring: Boolean = true) = composeTestRule.onAllNodesWithText(text, substring = substring).fetchSemanticsNodes().size

  @Test
  fun el_final_va_en_su_orden_la_placa_la_cifra_la_unidad_el_avance_el_truco_los_botones_el_detalle_y_la_nota() {
    show()
    val order = listOf("result_plaque", "result_headline", "result_unit", "progress_phrase", "next_trick", "btn_result_continue", "btn_result_replay", "detail_toggle", "why_it_matters", "result_note")
    val ys = order.map { it to yOfTag(it) }
    for (i in 0 until ys.size - 1) assertTrue("«${ys[i].first}» (${ys[i].second}) va antes de «${ys[i + 1].first}» (${ys[i + 1].second})", ys[i].second < ys[i + 1].second)
    assertEquals("la medida va suelta: la cifra y su unidad", 1, countText("5 de 8 palabras", substring = false))
    assertEquals(1, countText("por tu cuenta", substring = false))
  }

  @Test
  fun ya_no_hay_cuadros_de_tu_avance_ni_la_fila_de_etapa_y_modo_ni_la_tarjeta_de_la_medida_ni_el_titulo_de_la_caja() {
    show()
    assertEquals("los tres cuadros de «Tu avance» ya no existen", 0, countTag("progress_chips"))
    assertEquals("sin el título «Tu avance»", 0, countText("Tu avance", substring = false))
    assertEquals("sin «etapa» suelto", 0, countText("etapa", substring = false))
    assertEquals("sin «modo» suelto", 0, countText("modo", substring = false))
    assertEquals("la tarjeta crema de la medida ya no está", 0, countTag("meaningful_box"))
    assertEquals("el título de la caja («Tu cielo de palabras») ya no se dibuja", 0, countText("Tu cielo de palabras"))
    assertEquals("sin los discos «Sesión de hoy»", 0, countText("Sesión de hoy"))
  }

  @Test
  fun el_truco_sale_una_sola_vez_dentro_del_final_y_no_hay_bloque_aparte_de_consejo() {
    show()
    assertEquals("el título del bloque viejo ya no sale", 0, countText("Un consejo para la próxima"))
    assertEquals("el truco sale una sola vez, dentro del final", 1, countTag("next_trick"))
    assertEquals(1, countText("Truco para la próxima"))
  }

  @Test
  fun un_juego_sin_truco_condicional_tambien_trae_su_truco_general_una_sola_vez() {
    val brake = GamePlayResult(gameId = "freno", score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 2, brakeMs = 245, stopsOk = 6, stopsTotal = 8)
    show(brake)
    assertEquals(0, countText("Un consejo para la próxima"))
    assertEquals(1, countTag("next_trick"))
  }

  // ------------------------------------------------------------------ «¿Por qué importa?»: solo las 3 primeras partidas del juego

  @Test
  fun con_cero_partidas_antes_el_por_que_importa_esta_a_la_vista_entre_el_detalle_y_la_nota_y_no_se_repite_en_el_detalle() {
    show(playsBefore = 0)
    assertEquals(1, countTag("why_it_matters"))
    assertTrue("después del botón del detalle", yOfTag("why_it_matters") > yOfTag("detail_toggle"))
    assertTrue("antes de la nota", yOfTag("why_it_matters") < yOfTag("result_note"))
    assertEquals(1, countText("¿Por qué importa?", substring = false))
    assertEquals(1, countText("Fuente: Shafto y otros, 2007"))
    composeTestRule.onNodeWithTag("detail_toggle").performClick()
    composeTestRule.mainClock.advanceTimeBy(300)
    assertEquals("abierto el detalle no lo repite", 1, countTag("why_it_matters"))
  }

  @Test
  fun con_la_tercera_partida_del_juego_todavia_se_ve() {
    show(playsBefore = 2)
    assertEquals("la tercera partida del juego (2 antes) todavía lo muestra", 1, countTag("why_it_matters"))
  }

  @Test
  fun desde_la_cuarta_partida_el_por_que_importa_vive_dentro_del_detalle() {
    show(playsBefore = 3)
    assertEquals("a la vista ya no", 0, countTag("why_it_matters"))
    assertEquals(0, countText("Fuente: Shafto y otros, 2007"))
    composeTestRule.onNodeWithTag("detail_toggle").performClick()
    composeTestRule.mainClock.advanceTimeBy(300)
    assertEquals("abierto el detalle sí lo trae", 1, countTag("why_it_matters"))
    assertEquals("y está DENTRO del detalle", 1, composeTestRule.onAllNodes(hasTestTag("why_it_matters") and hasAnyAncestor(hasTestTag("detail_content"))).fetchSemanticsNodes().size)
    assertEquals("con su fuente", 1, countText("Fuente: Shafto y otros, 2007"))
    assertEquals("la nota se queda al pie, siempre", 1, countTag("result_note"))
  }

  private fun assertNoteAtTheBottom(playsBefore: Int) {
    show(playsBefore = playsBefore)
    assertEquals("«Medida de esta partida. No es un diagnóstico.» con $playsBefore partidas antes", 1, countText("Medida de esta partida. No es un diagnóstico.", substring = false))
    assertTrue(yOfTag("result_note") > yOfTag("btn_result_replay"))
  }

  @Test fun la_nota_va_al_pie_en_la_primera_partida() = assertNoteAtTheBottom(0)
  @Test fun la_nota_va_al_pie_desde_la_cuarta_partida() = assertNoteAtTheBottom(5)

  // ------------------------------------------------------------------ el botón de seguir (spec-final-B §1.2)

  @Test
  fun en_la_ruta_de_hoy_el_boton_dice_cual_es_el_juego_que_sigue_y_en_cual_de_los_3_vas() {
    show(isDailyFlow = true, dailyCompletedCount = 1, nextGameId = "calculo")
    composeTestRule.onNodeWithText("Siguiente: Carga exacta · 2 de 3").assertExists()
    assertEquals("el emblema del juego va a la izquierda del texto (en lugar del glifo)", 1, composeTestRule.onAllNodes(hasTestTag("btn_result_continue")).fetchSemanticsNodes().size)
  }

  @Test
  fun en_el_ultimo_juego_de_la_ruta_el_boton_dice_terminar_la_ruta_de_hoy() {
    show(isDailyFlow = true, dailyCompletedCount = 3, nextGameId = null)
    composeTestRule.onNodeWithText("Terminar la ruta de hoy").assertExists()
    assertEquals(0, countText("Siguiente", substring = true))
  }

  @Test
  fun en_una_partida_libre_el_boton_dice_listo() {
    show(isDailyFlow = false)
    composeTestRule.onNodeWithText("Listo").assertExists()
    assertEquals(0, countText("Siguiente", substring = true))
    composeTestRule.onNodeWithText("Jugar de nuevo").assertExists()
  }

  // ------------------------------------------------------------------ los avisos

  @Test
  fun los_avisos_salen_bajo_la_frase_en_este_orden_la_etapa_y_la_nota_del_modo() {
    show(didLevelUp = true, modeNote = "¡Desafío superado! Subes a la etapa siguiente.")
    composeTestRule.mainClock.advanceTimeBy(1500)
    assertEquals(1, countTag("result_notices"))
    assertEquals(2, countTag("result_notice"))
    assertTrue(yOfTag("progress_phrase") < yOfTag("result_notices"))
    assertTrue(yOfTag("result_notices") < yOfTag("next_trick"))
    val subiste = composeTestRule.onNodeWithText("¡Subiste de etapa!").fetchSemanticsNode().positionInRoot
    val modo = composeTestRule.onNodeWithText("¡Desafío superado! Subes a la etapa siguiente.").fetchSemanticsNode().positionInRoot
    assertTrue("la etapa primero, la nota del modo después (en la misma fila o debajo)", subiste.y < modo.y || (subiste.y == modo.y && subiste.x < modo.x))
  }

  @Test
  fun sin_avisos_no_se_dibuja_la_fila() {
    show()
    assertEquals(0, countTag("result_notices"))
  }
}
