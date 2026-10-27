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
import androidx.compose.ui.test.onAllNodesWithTag
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
import com.example.ui.screens.GameAboutContent
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.cardData
import com.example.ui.screens.sheetTimeTrialOf
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
 * La ficha del juego como PORTADA (Tarea 171, 25-oct; antes, la ficha con UNA decisión visible de las Tareas 73, 74 y 140): la ventanilla noche con la escena del juego, el nombre y una línea, la etapa con su barra, «Jugar» (sol) con lo elegido en su segunda línea, «Desafío» (lila) y «Más opciones».
 * Plegada debe caber ENTERA en un teléfono común (393 × 873 dp, ya sin las barras del sistema y con el margen de la ventana) sin deslizar, también con la letra del sistema al 130 %; desplegada («Más opciones»), la tarjeta hace scroll por dentro si no cabe y se desliza sola hasta el final.
 * Se prueba con la marca más larga (Piloto), sin medida (Tinta o Palabra) y la primera vez («Aún sin medir»). En la ventana real la tarjeta va centrada en el espacio libre entre las barras; aquí se prueba el contenido dentro de un marco del alto disponible.
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
    return cardData(radar, radar.title, 0.55f)
  }

  /** Un juego con «¿Con reloj?» (Freno de Emergencia): los cuatro con Contrarreloj ya no lo tienen (Tarea 181), así que lo desplegado de siempre se prueba con uno de los otros 13. */
  private fun withReto(): GameCardData {
    val freno = GameRegistry.getById("freno")!!
    return cardData(freno, freno.title, 0.55f)
  }

  /** [fontScale]: la letra del sistema (1,3 = al 130 %, como la usan muchas personas mayores). [unfold]: toca «Más opciones» antes de la captura (la ficha parte plegada). */
  private fun show(data: GameCardData, widthDp: Int, heightDp: Int, shot: String, timed: Boolean = true, fontScale: Float = 1f, unfold: Boolean = false) {
    composeTestRule.setContent {
      NeuroVidaTheme {
        CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, fontScale)) {
          // lo que hace la ventana de la ficha: un marco del alto disponible (ya sin las barras del sistema), con su margen, y la tarjeta CENTRADA (Tarea 140: Ricardo la quiso más arriba, no pegada abajo)
          Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
            Box(Modifier.fillMaxWidth()) {
              GameSheetContent(
                data, AgeBand.SENIOR, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = timed, onChooseTimed = {},
                timeTrial = sheetTimeTrialOf(data.game.id, data.title, null, "2026-10-26"), onTimeTrial = {}                    // el botón «Contrarreloj» de los cuatro juegos que lo tienen (Tarea 181)
              )
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

  /** La ficha plegada cabe ENTERA sin deslizar: lo último que hay («Desafío» y «Más opciones») está a la vista sin hacer scroll, y las dos secciones ni se componen. */
  private fun assertFoldedFitsWithoutScrolling() {
    assertPlayButtonInsideFrame()
    for (tag in listOf("sheet_title", "sheet_stage", "sheet_desafio", "sheet_change", "sheet_about", "sheet_close")) composeTestRule.onNodeWithTag(tag).assertIsDisplayed()
    val frame = composeTestRule.onNodeWithTag("frame").fetchSemanticsNode().boundsInRoot
    val last = if (composeTestRule.onAllNodesWithTag("sheet_timetrial").fetchSemanticsNodes().isNotEmpty()) listOf("sheet_desafio", "sheet_change", "sheet_timetrial") else listOf("sheet_desafio", "sheet_change")
    for (tag in last) {
      val b = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot
      assertTrue("$tag (abajo en ${b.bottom}) queda dentro del marco (abajo en ${frame.bottom}) sin deslizar", b.bottom <= frame.bottom + 0.5f)
    }
    assertSectionsNotComposed()
  }

  private fun assertSectionsNotComposed() {
    for (tag in listOf("mode_SUAVE", "mode_A_TU_MEDIDA", "mode_DESAFIO", "mode_EXPERTO", "reto_sin", "reto_con")) composeTestRule.onNodeWithTag(tag).assertDoesNotExist()
  }

  private fun playTexts(): List<String> = composeTestRule.onNodeWithTag("sheet_play").fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.Text].map { it.text }

  @Test
  fun rescate_plegada_cabe_entera_en_393_por_873_sin_deslizar() {
    show(rescue(), commonW, commonH, "ficha-rescate-393x873")
    assertFoldedFitsWithoutScrolling()
    // lo elegido se lee en la segunda línea de «Jugar»: «a tu medida» (en los cuatro juegos con Contrarreloj ya no hay reloj que elegir: Tarea 181)
    assertEquals(listOf("Jugar", "a tu medida"), playTexts())
  }

  @Test
  fun rescate_plegada_en_360_por_640_cabe_o_hace_scroll_por_dentro_y_jugar_sigue_a_la_vista() {
    show(rescue(), 360, 640, "ficha-rescate-360x640")
    assertPlayButtonInsideFrame()
    // el resto del contenido sigue ahí, alcanzable con scroll (la última fila: «Desafío» y «Más opciones»)
    composeTestRule.onNodeWithTag("sheet_change").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_desafio").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_play").performScrollTo().assertIsDisplayed()
    assertSectionsNotComposed()
  }

  @Test
  fun rescate_plegada_en_360_por_780_cabe_entera_sin_deslizar() {
    // el teléfono angosto: «Desafío» y «Más opciones» van lado a lado mientras cada palabra quepa en su media fila, y apilados si no; ninguna palabra se parte
    show(rescue(), 360, 780, "ficha-rescate-360x780")
    assertFoldedFitsWithoutScrolling()
  }

  @Test
  fun rescate_plegada_con_la_letra_del_sistema_al_130_por_ciento_cabe_entera_sin_deslizar() {
    // la letra al 130 % (como la usan muchas personas mayores): plegada cabe ENTERA, con «Jugar», «Desafío» y «Más opciones» a la vista y la tarjeta dentro del marco
    show(rescue(), commonW, commonH, "ficha-rescate-393x873-letra130", fontScale = 1.3f)
    assertFoldedFitsWithoutScrolling()
  }

  @Test
  fun piloto_plegada_con_la_marca_larga_cabe_entera_en_393_por_873() {
    val piloto = GameRegistry.getById("piloto")!!
    val data = cardData(piloto, piloto.title, 0.4f)
    show(data, commonW, commonH, "ficha-piloto-393x873")
    assertFoldedFitsWithoutScrolling()
  }

  @Test
  fun tinta_o_palabra_plegada_sin_medida_cabe_entera_en_393_por_873() {
    val stroop = GameRegistry.getById("stroop")!!
    val data = cardData(stroop, stroop.title, 0.62f)
    show(data, commonW, commonH, "ficha-tinta-393x873", timed = false)
    assertFoldedFitsWithoutScrolling()
    assertEquals(listOf("Jugar", "a tu medida"), playTexts())
  }

  @Test
  fun la_primera_vez_plegada_dice_sin_medir_aun_y_cabe_entera_en_393_por_873() {
    val radar = GameRegistry.getById("radar")!!
    val data = cardData(radar, radar.title, null)
    show(data, commonW, commonH, "ficha-primera-393x873", timed = false)
    assertFoldedFitsWithoutScrolling()
  }

  @Test
  fun con_la_letra_del_sistema_al_200_por_ciento_la_ficha_plegada_tiene_jugar_a_la_vista_y_lo_demas_con_scroll() {
    // Con letra muy grande ninguna palabra se parte («medid/a», «Desafí/o»): «Jugar» y debajo lo elegido, cada una entera, y «Desafío» y «Más opciones» apilados; «Jugar» sigue a la vista y el resto se alcanza deslizando.
    show(rescue(), commonW, commonH, "ficha-rescate-393x873-letra200", fontScale = 2f)
    assertPlayButtonInsideFrame()
    composeTestRule.onNodeWithTag("sheet_desafio").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_change").performScrollTo().assertIsDisplayed()
  }

  // ---------- «Sobre este juego»: «Qué pones en juego» (final «B», Tarea 146; en la lámina desde la Tarea 171) ----------

  @Test
  fun los_17_juegos_muestran_en_sobre_este_juego_que_pones_en_juego_con_su_texto_en_presente_y_su_fuente() {
    var data by mutableStateOf(rescue())
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(commonW.dp).height(commonH.dp).background(Color(0xFF050823)).padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
          Box(Modifier.fillMaxWidth()) { GameAboutContent(data) }
        }
      }
    }
    assertEquals("los 17 juegos de la app", 17, GameRegistry.allGames.size)
    for (g in GameRegistry.allGames) {
      data = cardData(g, g.title, 0.5f)
      composeTestRule.waitForIdle()
      val (text, source) = FinalesConSentido.sheetFor(g.id) ?: error("${g.id}: sin texto para la lámina")
      composeTestRule.onNodeWithTag("sheet_why").assertExists()
      composeTestRule.onNodeWithTag("about_how").assertExists()
      assertEquals("${g.id}: «Qué pones en juego» una vez", 1, composeTestRule.onAllNodesWithText("Qué pones en juego").fetchSemanticsNodes().size)
      assertEquals("${g.id}: el texto en presente", 1, composeTestRule.onAllNodesWithText(text).fetchSemanticsNodes().size)
      assertTrue("${g.id}: empieza con «Pones en juego»: $text", text.startsWith("Pones en juego"))
      assertEquals("${g.id}: su fuente", 1, composeTestRule.onAllNodesWithText(source).fetchSemanticsNodes().size)
      assertTrue("${g.id}: «Fuente: …»: $source", source.startsWith("Fuente: "))
    }
  }

  @Test
  fun sobre_este_juego_con_el_texto_largo_deja_como_se_juega_fijo_y_a_la_vista_y_la_tarjeta_dentro_del_marco() {
    // el texto largo de Rescate, a 393 × 873: «Cómo se juega» a la vista (fijo abajo, fuera del scroll) y la tarjeta dentro del marco
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(commonW.dp).height(commonH.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
          Box(Modifier.fillMaxWidth()) { GameAboutContent(rescue()) }
        }
      }
    }
    composeTestRule.waitForIdle()
    val frame = composeTestRule.onNodeWithTag("frame").fetchSemanticsNode().boundsInRoot
    val how = composeTestRule.onNodeWithTag("about_how").fetchSemanticsNode().boundsInRoot
    val card = composeTestRule.onNodeWithTag("game_about").fetchSemanticsNode().boundsInRoot
    assertTrue("«Cómo se juega» (abajo en ${how.bottom}) cabe en el marco (${frame.bottom})", how.bottom <= frame.bottom + 0.5f)
    assertTrue("la tarjeta cabe en el marco", card.top >= frame.top - 0.5f && card.bottom <= frame.bottom + 0.5f)
    composeTestRule.onNodeWithTag("about_how").assertIsDisplayed()
  }

  // ---------- desplegada: las dos secciones como siempre ----------

  @Test
  fun freno_desplegada_en_393_por_873_tiene_todo_alcanzable() {
    show(withReto(), commonW, commonH, "ficha-freno-393x873-desplegada", unfold = true)
    val frame = composeTestRule.onNodeWithTag("frame").fetchSemanticsNode().boundsInRoot
    val card = composeTestRule.onNodeWithTag("game_sheet").fetchSemanticsNode().boundsInRoot
    assertTrue("la tarjeta (arriba en ${card.top}, abajo en ${card.bottom}) cabe en el marco (${frame.top} a ${frame.bottom})", card.top >= frame.top - 0.5f && card.bottom <= frame.bottom + 0.5f)
    // el mismo botón dice «Menos opciones»; la elección se ve y se lee: «A tu medida» y «Con reloj» elegidos, Al límite bloqueado y sin poder tocarse
    composeTestRule.onNodeWithTag("sheet_change").assertTextEquals("Menos opciones")
    // al desplegar la ficha se desliza hasta el final: las dos secciones enteras (hasta «¿Con reloj?») quedan a la vista SIN que la persona tenga que deslizar
    composeTestRule.onNodeWithTag("reto_con").assertIsDisplayed()
    composeTestRule.onNodeWithTag("mode_SUAVE").assertIsDisplayed()
    for (tag in listOf("mode_SUAVE", "mode_A_TU_MEDIDA", "mode_DESAFIO", "mode_EXPERTO", "reto_sin", "reto_con")) composeTestRule.onNodeWithTag(tag).performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("mode_A_TU_MEDIDA").assertIsSelected()
    composeTestRule.onNodeWithTag("reto_con").assertIsSelected()
    composeTestRule.onNodeWithTag("mode_EXPERTO").assertIsNotEnabled()
    composeTestRule.onNodeWithTag("sheet_play").performScrollTo().assertIsDisplayed()
  }

  @Test
  fun freno_desplegada_en_360_por_640_hace_scroll_por_dentro_y_todo_se_alcanza() {
    show(withReto(), 360, 640, "ficha-freno-360x640-desplegada", unfold = true)
    val frame = composeTestRule.onNodeWithTag("frame").fetchSemanticsNode().boundsInRoot
    val card = composeTestRule.onNodeWithTag("game_sheet").fetchSemanticsNode().boundsInRoot
    assertTrue("la tarjeta cabe en el marco", card.top >= frame.top - 0.5f && card.bottom <= frame.bottom + 0.5f)
    composeTestRule.onNodeWithTag("reto_con").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_play").performScrollTo().assertIsDisplayed()
  }

  @Test
  fun freno_desplegada_con_la_letra_del_sistema_al_130_por_ciento_hace_scroll_y_todo_se_alcanza() {
    // con la letra grande hace scroll por dentro (se desliza sola al desplegar) y la tarjeta no se sale del marco
    show(withReto(), commonW, commonH, "ficha-freno-393x873-letra130-desplegada", fontScale = 1.3f, unfold = true)
    val frame = composeTestRule.onNodeWithTag("frame").fetchSemanticsNode().boundsInRoot
    val card = composeTestRule.onNodeWithTag("game_sheet").fetchSemanticsNode().boundsInRoot
    assertTrue("la tarjeta cabe en el marco", card.top >= frame.top - 0.5f && card.bottom <= frame.bottom + 0.5f)
    composeTestRule.onNodeWithTag("mode_EXPERTO").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("reto_con").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_play").performScrollTo().assertIsDisplayed()
  }

  @Test
  fun con_la_letra_del_sistema_al_200_por_ciento_desplegada_las_casillas_van_de_a_una() {
    // Con letra muy grande ninguna palabra se parte («medid/a», «Desafí/o»): los cuatro modos van de a UNO por fila (cada uno ocupa casi todo el ancho de la tarjeta, así que no caben dos lado a lado).
    show(rescue(), commonW, commonH, "ficha-rescate-393x873-letra200-desplegada", fontScale = 2f, unfold = true)
    val card = composeTestRule.onNodeWithTag("game_sheet").fetchSemanticsNode().boundsInRoot
    val tags = listOf("mode_SUAVE", "mode_A_TU_MEDIDA", "mode_DESAFIO", "mode_EXPERTO")
    for (tag in tags) composeTestRule.onNodeWithTag(tag).performScrollTo()
    val bounds = tags.map { composeTestRule.onNodeWithTag(it).fetchSemanticsNode().boundsInRoot }
    for ((tag, b) in tags.zip(bounds)) assertTrue("$tag ocupa casi todo el ancho de la tarjeta (${b.width} de ${card.width})", b.width >= card.width * 0.7f)
  }
}
