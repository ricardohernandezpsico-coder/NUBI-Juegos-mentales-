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
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.assertIsNotEnabled
import androidx.compose.ui.test.assertIsSelected
import androidx.compose.ui.test.assertTextEquals
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.FinalesConSentido
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.model.AgeBand
import com.example.model.GameRegistry
import com.example.ui.screens.GameCardData
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.cardData
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La ficha del juego con UNA decisión visible (Tarea 140, A3 de la spec de adopción; sobre la propuesta «A · Compacta» de las Tareas 73 y 74): plegada por defecto, con lo elegido en una línea, el botón «Cambiar cómo jugar» y «Jugar · …» con lo elegido;
 * desplegada, con las dos secciones («¿Cómo quieres jugar?» y «¿Con reloj?») como siempre. Plegada debe caber ENTERA en un teléfono común (393 × 873 dp, ya sin las barras del sistema y con el margen de la ventana) con aire de sobra;
 * desplegada, el contenido hace scroll por dentro si no cabe y «Jugar» sigue a la vista. Se prueba en el peor caso (Rescate con medida, serie y reloj y Experto bloqueado), con la marca más larga (Piloto), sin medida (Tinta o Palabra) y la primera
 * vez («Sin medir aún»). En la ventana real la tarjeta va centrada en el espacio libre entre las barras; aquí se prueba el contenido dentro de un marco del alto disponible.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class GameSheetFitScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private val day = 86_400_000L
  private val now = 400 * day + 3_600_000L

  // Un teléfono común de 393 × 873 dp menos las barras del sistema (unos 24 arriba y 48 abajo): lo que le queda a la ventana de la ficha
  private val commonW = 393
  private val commonH = 873 - 24 - 48

  private fun series(key: String, vararg values: Float) = values.mapIndexed { i, v -> MeasurePoint(now - (values.size - i) * day, key, v, 0.5f, true) }

  private fun rescue(): GameCardData {
    val radar = GameRegistry.getById("radar")!!
    return cardData(radar, radar.title, now - day, now, 0.55f, series("glance", 260f, 240f, 250f, 210f, 190f, 170f), playsLast14 = 6)
  }

  /** [fontScale]: la letra del sistema (1,3 = al 130 %, como la usan muchas personas mayores). [unfold]: toca «Cambiar cómo jugar» antes de la captura (la ficha parte plegada). */
  private fun show(data: GameCardData, widthDp: Int, heightDp: Int, shot: String, timed: Boolean = true, fontScale: Float = 1f, unfold: Boolean = false) {
    composeTestRule.setContent {
      NeuroVidaTheme {
        CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, fontScale)) {
          // lo que hace la ventana de la ficha: un marco del alto disponible (ya sin las barras del sistema), con su margen, y la tarjeta CENTRADA (Tarea 140: Ricardo la quiso más arriba, no pegada abajo)
          Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
            Box(Modifier.fillMaxWidth()) {
              GameSheetContent(data, AgeBand.SENIOR, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = timed, onChooseTimed = {})
            }
          }
        }
      }
    }
    composeTestRule.waitForIdle()
    if (unfold) {
      composeTestRule.onNodeWithTag("sheet_change").performScrollTo().performClick()           // con la letra muy grande el botón puede quedar bajo el pliegue del scroll
      composeTestRule.waitForIdle()
    }
    composeTestRule.onNodeWithTag("frame").captureRoboImage(filePath = "src/test/screenshots/$shot.png")
  }

  /** «Jugar» entero a la vista (sin deslizar) y la tarjeta dentro del marco. */
  private fun assertPlayButtonInsideFrame() {
    composeTestRule.onNodeWithTag("sheet_play").assertIsDisplayed()
    val frame = composeTestRule.onNodeWithTag("frame").fetchSemanticsNode().boundsInRoot
    val play = composeTestRule.onNodeWithTag("sheet_play").fetchSemanticsNode().boundsInRoot
    val card = composeTestRule.onNodeWithTag("game_sheet").fetchSemanticsNode().boundsInRoot
    assertTrue("el botón «Jugar» (abajo en ${play.bottom}) cabe en el marco (abajo en ${frame.bottom})", play.bottom <= frame.bottom + 0.5f)
    assertTrue("la tarjeta (arriba en ${card.top}, abajo en ${card.bottom}) cabe en el marco (${frame.top} a ${frame.bottom})", card.top >= frame.top - 0.5f && card.bottom <= frame.bottom + 0.5f)
  }

  /** La ficha plegada cabe ENTERA sin deslizar: lo último que hay (el botón «Cambiar cómo jugar») está a la vista sin hacer scroll, y las dos secciones ni se componen. */
  private fun assertFoldedFitsWithoutScrolling() {
    assertPlayButtonInsideFrame()
    composeTestRule.onNodeWithTag("sheet_summary").assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_change").assertIsDisplayed()
    assertSectionsNotComposed()
  }

  private fun assertSectionsNotComposed() {
    for (tag in listOf("mode_SUAVE", "mode_A_TU_MEDIDA", "mode_DESAFIO", "mode_EXPERTO", "reto_sin", "reto_con")) composeTestRule.onNodeWithTag(tag).assertDoesNotExist()
  }

  @Test
  fun rescate_plegada_cabe_entera_en_393_por_873_sin_deslizar() {
    show(rescue(), commonW, commonH, "ficha-rescate-393x873")
    assertFoldedFitsWithoutScrolling()
    // lo elegido se lee en la línea de resumen y en el botón: «A tu medida · con reloj (2 minutos)»
    composeTestRule.onNodeWithTag("sheet_summary").assertTextEquals("A tu medida · con reloj (2 minutos)")
  }

  @Test
  fun rescate_plegada_en_360_por_640_cabe_o_hace_scroll_por_dentro_y_el_boton_sigue_a_la_vista() {
    show(rescue(), 360, 640, "ficha-rescate-360x640")
    assertPlayButtonInsideFrame()
    // el resto del contenido sigue ahí, alcanzable con scroll (la última fila: el botón «Cambiar cómo jugar»)
    composeTestRule.onNodeWithTag("sheet_change").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_play").assertIsDisplayed()
    assertSectionsNotComposed()
  }

  @Test
  fun rescate_plegada_en_360_por_780_muestra_solo_la_etapa_actual_y_el_boton_a_la_vista() {
    // el teléfono angosto: los 5 nombres de las etapas no caben cada uno en su casilla y se muestra SOLO el de la etapa actual (nunca «Aprendi / z»); «Jugar» pasa a dos líneas: «Jugar» y debajo lo elegido
    show(rescue(), 360, 780, "ficha-rescate-360x780")
    assertPlayButtonInsideFrame()
    composeTestRule.onNodeWithTag("sheet_change").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_play").assertIsDisplayed()
  }

  @Test
  fun rescate_plegada_con_la_letra_del_sistema_al_130_por_ciento_cabe_y_tiene_el_boton_a_la_vista() {
    // con la letra grande «Jugar» pasa a dos líneas; plegada cabe sin deslizar, o hace scroll por dentro, y «Jugar» sigue a la vista y la tarjeta no se sale del marco
    show(rescue(), commonW, commonH, "ficha-rescate-393x873-letra130", fontScale = 1.3f)
    assertPlayButtonInsideFrame()
    composeTestRule.onNodeWithTag("sheet_change").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_play").assertIsDisplayed()
    assertSectionsNotComposed()
  }

  @Test
  fun piloto_plegada_con_la_marca_larga_cabe_entera_en_393_por_873() {
    val piloto = GameRegistry.getById("piloto")!!
    val data = cardData(piloto, piloto.title, now, now, 0.4f, series("mandos", 41f, 47f, 44f, 52f, 55f, 58f), playsLast14 = 4)
    show(data, commonW, commonH, "ficha-piloto-393x873")
    assertFoldedFitsWithoutScrolling()
  }

  @Test
  fun tinta_o_palabra_plegada_sin_medida_cabe_entera_en_393_por_873() {
    val stroop = GameRegistry.getById("stroop")!!
    val data = cardData(stroop, stroop.title, now - 2 * day, now, 0.62f, emptyList(), playsLast14 = 3)
    show(data, commonW, commonH, "ficha-tinta-393x873", timed = false)
    assertFoldedFitsWithoutScrolling()
    composeTestRule.onNodeWithTag("sheet_summary").assertTextEquals("A tu medida · sin reloj")
  }

  @Test
  fun la_primera_vez_plegada_dice_sin_medir_aun_y_cabe_entera_en_393_por_873() {
    val radar = GameRegistry.getById("radar")!!
    val data = cardData(radar, radar.title, null, now, null, emptyList(), playsLast14 = 0)
    show(data, commonW, commonH, "ficha-primera-393x873", timed = false)
    assertFoldedFitsWithoutScrolling()
  }

  @Test
  fun con_la_letra_del_sistema_al_200_por_ciento_la_ficha_plegada_cabe_y_el_boton_sigue_a_la_vista() {
    // Con letra muy grande ninguna palabra se parte («medid/a», «Desafí/o»): «Jugar» y debajo lo elegido, cada una entera; plegada la ficha cabe (o hace scroll por dentro) y «Jugar» sigue a la vista.
    show(rescue(), commonW, commonH, "ficha-rescate-393x873-letra200", fontScale = 2f)
    assertPlayButtonInsideFrame()
    composeTestRule.onNodeWithTag("sheet_change").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_play").assertIsDisplayed()
  }

  // ---------- «Qué pones en juego» (final «B», Tarea 146) ----------

  @Test
  fun los_17_juegos_muestran_que_pones_en_juego_con_su_texto_en_presente_y_su_fuente() {
    var data by mutableStateOf(rescue())
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(commonW.dp).height(commonH.dp).background(Color(0xFF050823)).padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
          Box(Modifier.fillMaxWidth()) { GameSheetContent(data, AgeBand.SENIOR, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = true, onChooseTimed = {}) }
        }
      }
    }
    assertEquals("los 17 juegos de la app", 17, GameRegistry.allGames.size)
    for (g in GameRegistry.allGames) {
      data = cardData(g, g.title, now - day, now, 0.5f, emptyList(), playsLast14 = 2)
      composeTestRule.waitForIdle()
      val (text, source) = FinalesConSentido.sheetFor(g.id) ?: error("${g.id}: sin texto para la ficha")
      composeTestRule.onNodeWithTag("sheet_why").assertExists()
      assertEquals("${g.id}: «Qué pones en juego» una vez", 1, composeTestRule.onAllNodesWithText("Qué pones en juego").fetchSemanticsNodes().size)
      assertEquals("${g.id}: el texto en presente", 1, composeTestRule.onAllNodesWithText(text).fetchSemanticsNodes().size)
      assertTrue("${g.id}: empieza con «Pones en juego»: $text", text.startsWith("Pones en juego"))
      assertEquals("${g.id}: su fuente", 1, composeTestRule.onAllNodesWithText(source).fetchSemanticsNodes().size)
      assertTrue("${g.id}: «Fuente: …»: $source", source.startsWith("Fuente: "))
    }
  }

  @Test
  fun la_ficha_con_que_pones_en_juego_sigue_cabiendo_o_hace_scroll_y_el_boton_a_la_vista() {
    // el texto largo de Rescate más la medida, a 393 × 873 con letra 1,0 y 1,3: «Jugar» a la vista y la tarjeta dentro del marco
    show(rescue(), commonW, commonH, "ficha-rescate-393x873-que-pones-en-juego")
    assertPlayButtonInsideFrame()
    composeTestRule.onNodeWithTag("sheet_why").performScrollTo().assertIsDisplayed()
  }

  // ---------- desplegada: las dos secciones como siempre ----------

  @Test
  fun rescate_desplegada_en_393_por_873_tiene_el_boton_a_la_vista_y_todo_alcanzable() {
    show(rescue(), commonW, commonH, "ficha-rescate-393x873-desplegada", unfold = true)
    assertPlayButtonInsideFrame()
    // el mismo botón dice «Listo»; la elección se ve y se lee: «A tu medida» y «Con reloj» elegidos, Experto bloqueado y sin poder tocarse
    composeTestRule.onNodeWithTag("sheet_change").assertTextEquals("Listo")
    // al desplegar la ficha se desliza hasta el final: las dos secciones enteras (hasta «¿Con reloj?») quedan a la vista SIN que la persona tenga que deslizar
    composeTestRule.onNodeWithTag("reto_con").assertIsDisplayed()
    composeTestRule.onNodeWithTag("mode_SUAVE").assertIsDisplayed()
    for (tag in listOf("mode_SUAVE", "mode_A_TU_MEDIDA", "mode_DESAFIO", "mode_EXPERTO", "reto_sin", "reto_con")) composeTestRule.onNodeWithTag(tag).performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("mode_A_TU_MEDIDA").assertIsSelected()
    composeTestRule.onNodeWithTag("reto_con").assertIsSelected()
    composeTestRule.onNodeWithTag("mode_EXPERTO").assertIsNotEnabled()
    composeTestRule.onNodeWithTag("sheet_play").assertIsDisplayed()
  }

  @Test
  fun rescate_desplegada_en_360_por_640_hace_scroll_por_dentro_y_el_boton_sigue_a_la_vista() {
    show(rescue(), 360, 640, "ficha-rescate-360x640-desplegada", unfold = true)
    assertPlayButtonInsideFrame()
    composeTestRule.onNodeWithTag("reto_con").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_play").assertIsDisplayed()
  }

  @Test
  fun rescate_desplegada_con_la_letra_del_sistema_al_130_por_ciento_tiene_el_boton_a_la_vista() {
    // con la letra grande hace scroll por dentro, pero «Jugar» sigue a la vista y la tarjeta no se sale del marco
    show(rescue(), commonW, commonH, "ficha-rescate-393x873-letra130-desplegada", fontScale = 1.3f, unfold = true)
    assertPlayButtonInsideFrame()
    composeTestRule.onNodeWithTag("mode_EXPERTO").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("reto_con").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_play").assertIsDisplayed()
  }

  @Test
  fun con_la_letra_del_sistema_al_200_por_ciento_desplegada_las_casillas_van_de_a_una_y_el_boton_sigue_a_la_vista() {
    // Con letra muy grande ninguna palabra se parte («medid/a», «Desafí/o»): los cuatro modos van de a UNO por fila (cada uno ocupa casi todo el ancho de la tarjeta, así que no caben dos lado a lado) y «Jugar» sigue a la vista.
    show(rescue(), commonW, commonH, "ficha-rescate-393x873-letra200-desplegada", fontScale = 2f, unfold = true)
    assertPlayButtonInsideFrame()
    val card = composeTestRule.onNodeWithTag("game_sheet").fetchSemanticsNode().boundsInRoot
    val tags = listOf("mode_SUAVE", "mode_A_TU_MEDIDA", "mode_DESAFIO", "mode_EXPERTO")
    for (tag in tags) composeTestRule.onNodeWithTag(tag).performScrollTo()
    val bounds = tags.map { composeTestRule.onNodeWithTag(it).fetchSemanticsNode().boundsInRoot }
    for ((tag, b) in tags.zip(bounds)) assertTrue("$tag ocupa casi todo el ancho de la tarjeta (${b.width} de ${card.width})", b.width >= card.width * 0.7f)
  }
}
