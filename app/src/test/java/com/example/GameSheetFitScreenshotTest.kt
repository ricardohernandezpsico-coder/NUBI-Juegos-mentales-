package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.assertIsNotEnabled
import androidx.compose.ui.test.assertIsSelected
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
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
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La ficha del juego, propuesta «A · Compacta» (Tareas 73 y 74, 10-oct): debe caber ENTERA en un teléfono común (393 × 873 dp, ya sin las barras del sistema y con el margen de la ventana) sin deslizar, con «Jugar» siempre a la vista;
 * en un teléfono bajo (360 × 640) el contenido hace scroll por dentro y el botón sigue a la vista. Se prueba en el peor caso (Rescate con medida, serie y reloj y Experto bloqueado), con la marca más larga (Piloto), sin medida
 * (Tinta o Palabra) y la primera vez («Sin medir aún»). En la ventana real la tarjeta va anclada abajo; aquí se prueba el contenido dentro de un marco del alto disponible.
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

  /** [fontScale]: la letra del sistema (1,3 = al 130 %, como la usan muchas personas mayores). */
  private fun show(data: GameCardData, widthDp: Int, heightDp: Int, shot: String, timed: Boolean = true, fontScale: Float = 1f) {
    composeTestRule.setContent {
      NeuroVidaTheme {
        CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, fontScale)) {
          // lo que hace la ventana de la ficha: un marco del alto disponible (ya sin las barras del sistema), con su margen, y la tarjeta abajo
          Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.BottomCenter) {
            Box(Modifier.fillMaxWidth()) {
              GameSheetContent(data, AgeBand.SENIOR, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = timed, onChooseTimed = {})
            }
          }
        }
      }
    }
    composeTestRule.waitForIdle()
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

  /** La ficha cabe ENTERA sin deslizar: hasta lo último que hay (los dos cuadros del reloj y los modos) está a la vista sin hacer scroll. */
  private fun assertEverythingVisibleWithoutScrolling(withClock: Boolean = true) {
    assertPlayButtonInsideFrame()
    for (tag in listOf("mode_SUAVE", "mode_A_TU_MEDIDA", "mode_DESAFIO", "mode_EXPERTO")) composeTestRule.onNodeWithTag(tag).assertIsDisplayed()
    if (withClock) { composeTestRule.onNodeWithTag("reto_sin").assertIsDisplayed(); composeTestRule.onNodeWithTag("reto_con").assertIsDisplayed() }
  }

  @Test
  fun rescate_cabe_entera_en_393_por_873_sin_deslizar() {
    show(rescue(), commonW, commonH, "ficha-rescate-393x873")
    assertEverythingVisibleWithoutScrolling()
    // la elección se ve y se lee: «A tu medida» y «Con reloj» elegidos, Experto bloqueado y sin poder tocarse
    composeTestRule.onNodeWithTag("mode_A_TU_MEDIDA").assertIsSelected()
    composeTestRule.onNodeWithTag("reto_con").assertIsSelected()
    composeTestRule.onNodeWithTag("mode_EXPERTO").assertIsNotEnabled()
  }

  @Test
  fun rescate_en_360_por_640_hace_scroll_por_dentro_y_el_boton_sigue_a_la_vista() {
    show(rescue(), 360, 640, "ficha-rescate-360x640")
    assertPlayButtonInsideFrame()
    // el resto del contenido sigue ahí, alcanzable con scroll (la última fila: los cuadros del reloj)
    composeTestRule.onNodeWithTag("reto_con").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_play").assertIsDisplayed()
  }

  @Test
  fun rescate_en_360_por_780_muestra_solo_la_etapa_actual_y_el_boton_a_la_vista() {
    // el teléfono angosto: los 5 nombres de las etapas no caben cada uno en su casilla y se muestra SOLO el de la etapa actual (nunca «Aprendi / z»); el nombre de cada modo pasa a 2 líneas entre palabras
    show(rescue(), 360, 780, "ficha-rescate-360x780")
    assertPlayButtonInsideFrame()
    composeTestRule.onNodeWithTag("reto_con").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_play").assertIsDisplayed()
  }

  @Test
  fun rescate_con_la_letra_del_sistema_al_130_por_ciento_tiene_el_boton_a_la_vista() {
    // con la letra grande puede necesitar scroll, pero «Jugar» sigue a la vista y la tarjeta no se sale del marco
    show(rescue(), commonW, commonH, "ficha-rescate-393x873-letra130", fontScale = 1.3f)
    assertPlayButtonInsideFrame()
    composeTestRule.onNodeWithTag("mode_EXPERTO").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("reto_con").performScrollTo().assertIsDisplayed()
    composeTestRule.onNodeWithTag("sheet_play").assertIsDisplayed()
  }

  @Test
  fun piloto_con_la_marca_larga_cabe_entera_en_393_por_873() {
    val piloto = GameRegistry.getById("piloto")!!
    val data = cardData(piloto, piloto.title, now, now, 0.4f, series("mandos", 41f, 47f, 44f, 52f, 55f, 58f), playsLast14 = 4)
    show(data, commonW, commonH, "ficha-piloto-393x873")
    assertEverythingVisibleWithoutScrolling()
  }

  @Test
  fun tinta_o_palabra_sin_medida_cabe_entera_en_393_por_873() {
    val stroop = GameRegistry.getById("stroop")!!
    val data = cardData(stroop, stroop.title, now - 2 * day, now, 0.62f, emptyList(), playsLast14 = 3)
    show(data, commonW, commonH, "ficha-tinta-393x873", timed = false)
    assertEverythingVisibleWithoutScrolling()
    composeTestRule.onNodeWithTag("reto_sin").assertIsSelected()
  }

  @Test
  fun la_primera_vez_dice_sin_medir_aun_y_cabe_entera_en_393_por_873() {
    val radar = GameRegistry.getById("radar")!!
    val data = cardData(radar, radar.title, null, now, null, emptyList(), playsLast14 = 0)
    show(data, commonW, commonH, "ficha-primera-393x873", timed = false)
    assertEverythingVisibleWithoutScrolling()
  }
}
