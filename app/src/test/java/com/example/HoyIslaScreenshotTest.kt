package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.assertHeightIsAtLeast
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithContentDescription
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeUi
import com.example.ui.screens.ISLAND_MIN_HEIGHT
import com.example.ui.screens.homeStops
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
import java.util.Calendar

/**
 * Hoy con la isla (Tarea 82, fase 1; `docs/diseno-isla.md` §4.1). Capturas a 393 × 873 dp (el teléfono de referencia), a 360 dp de ancho y con letra 1,3, sobre el fondo nuevo y con las barras del sistema simuladas por margen
 * (24 dp arriba; abajo 48 dp de la barra de Android y 71 dp de la barra de pestañas actual). La regla que manda: con letra normal las TRES paradas y el botón se ven sin deslizar.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class HoyIslaScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  // Un domingo a las 16:30 (para el saludo «Buenas tardes» y la fecha); la semana: lunes, martes y hoy.
  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 11, 16, 30, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)

  private fun ui(completed: Int = 1, ids: List<String> = listOf("bodega", "calculo", "intrusa"), paused: String? = null, flightDay: Boolean = false, hasBaseline: Boolean = true, name: String = "Ricardo") =
    HomeUi(
      name = name, now = now, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
      stops = homeStops(ids, completed, AppLanguage.SPANISH), pausedTitle = paused, flightDay = flightDay, hasBaseline = hasBaseline
    )

  private var placeTapped: String? = null
  private var played = 0
  private var resumed = 0

  private fun show(ui: HomeUi, widthDp: Int = 393, heightDp: Int = 873, fontScale: Float = 1f) {
    composeTestRule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale)) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            HomeContent(
              ui = ui, onProfile = {}, onSettings = {}, onPlace = { placeTapped = it }, onPlay = { played++ }, onResume = { resumed++ }, onBaseline = {},
              modifier = Modifier.padding(top = 24.dp, bottom = 119.dp)
            )
          }
        }
      }
    }
    composeTestRule.waitForIdle()
  }

  @Test
  fun hoy_a_393_por_873_las_tres_paradas_y_el_boton_se_ven_sin_deslizar() {
    show(ui())
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-isla-393.png")
    composeTestRule.onNodeWithTag("home_isla").assertHeightIsAtLeast(220.dp)   // la isla es la protagonista: no se queda en su mínimo cuando la pantalla alcanza
    for (tag in listOf("home_greeting", "home_week", "home_isla", "nubi_line", "home_stop_1", "home_stop_2", "home_stop_3", "btn_home_play")) {
      try { composeTestRule.onNodeWithTag(tag).assertIsDisplayed() } catch (e: AssertionError) { throw AssertionError("«$tag» no se ve sin deslizar a 393 × 873: ${composeTestRule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot}", e) }
    }
  }

  @Test
  fun hoy_a_360_dp_de_ancho_tambien_muestra_las_paradas_y_el_boton() {
    show(ui(), widthDp = 360, heightDp = 780)
    for (tag in listOf("home_stop_1", "home_stop_2", "home_stop_3", "btn_home_play")) composeTestRule.onNodeWithTag(tag).assertIsDisplayed()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-isla-360.png")
  }

  @Test
  fun hoy_con_letra_1_3_se_desliza_pero_la_isla_no_se_pierde() {
    show(ui(), fontScale = 1.3f)
    composeTestRule.onNodeWithTag("home_isla").assertIsDisplayed().assertHeightIsAtLeast(ISLAND_MIN_HEIGHT)   // no se achica más que su mínimo: la pantalla se desliza
    composeTestRule.onNodeWithTag("home_greeting").assertIsDisplayed()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-isla-letra13.png")
  }

  @Test
  fun el_saludo_dice_el_momento_del_dia_el_nombre_y_la_fecha() {
    show(ui())
    composeTestRule.onNodeWithText("Buenas tardes, Ricardo").assertIsDisplayed()
    composeTestRule.onNodeWithText("Domingo 11 de octubre").assertIsDisplayed()
  }

  @Test
  fun sin_nombre_el_saludo_no_inventa_uno() {
    show(ui(name = ""))
    composeTestRule.onNodeWithText("Buenas tardes").assertIsDisplayed()
  }

  @Test
  fun la_semana_se_lee_en_palabras_como_un_solo_grupo() {
    show(ui())
    composeTestRule.onNodeWithText("3 de 4 días de tu meta", useUnmergedTree = true).assertExists()   // el texto se ve, pero el lector de pantalla lo oye UNA vez, en el grupo
    composeTestRule.onNodeWithContentDescription("Tu semana: 3 de 4 días de tu meta").assertExists()
    composeTestRule.onNodeWithTag("streak_pill").assertExists()
  }

  @Test
  fun las_paradas_dicen_su_estado_en_texto_y_no_solo_con_color() {
    show(ui(completed = 1))
    composeTestRule.onNodeWithContentDescription("Bodega de carga, Observatorio, hecho").assertExists()
    composeTestRule.onNodeWithContentDescription("Carga exacta, Hangar, es la que sigue").assertExists()
    composeTestRule.onNodeWithContentDescription("La estrella intrusa, Biblioteca, después").assertExists()
    composeTestRule.onNodeWithText("Es la que sigue").assertExists()
    composeTestRule.onNodeWithText("Hecho").assertExists()
  }

  @Test
  fun el_globo_y_el_boton_siguen_la_ruta_de_hoy() {
    show(ui(completed = 0))
    composeTestRule.onNodeWithText("Hoy la ruta pasa por el Observatorio, el Hangar y la Biblioteca. Unos 6 minutos.").assertExists()
    composeTestRule.onNodeWithText("Empezar la ruta · 3 juegos").performClick()
    assertEquals(1, played)
  }

  @Test
  fun a_mitad_de_camino_el_boton_dice_cuantos_juegos_faltan() {
    show(ui(completed = 2))
    composeTestRule.onNodeWithText("Seguir la ruta · 1 juego").assertExists()
    composeTestRule.onNodeWithText("Hoy la ruta pasa por la Biblioteca. Unos 2 minutos.").assertExists()
  }

  @Test
  fun con_la_ruta_cumplida_hay_un_aviso_de_ruta_completa_y_no_un_boton_de_jugar() {
    show(ui(completed = 3))
    composeTestRule.onNodeWithTag("home_route_done").assertExists()
    composeTestRule.onNodeWithText("Ruta completa").assertExists()
    composeTestRule.onNodeWithText("Ya pasaste por la isla hoy. Mañana sigo aquí.").assertExists()
    assertEquals(0, composeTestRule.onAllNodesWithTagCount("btn_home_play"))
  }

  @Test
  fun una_partida_en_pausa_se_retoma_con_el_mismo_boton_grande() {
    show(ui(paused = "Carga exacta"))
    composeTestRule.onNodeWithText("Seguir tu partida").performClick()
    assertEquals(1, resumed)
    composeTestRule.onNodeWithText("Tienes una partida en pausa: Carga exacta.").assertExists()
  }

  @Test
  fun si_el_camino_fue_el_primer_vuelo_Nubi_lo_dice_y_no_hay_boton_de_jugar() {
    show(ui(completed = 3, flightDay = true))
    composeTestRule.onNodeWithTag("home_flight_day").assertExists()
    composeTestRule.onNodeWithText("Tu Primer vuelo fue tu camino de hoy. Mañana empieza tu camino.").assertExists()
  }

  @Test
  fun sin_punto_de_partida_la_invitacion_sigue_a_mano() {
    show(ui(hasBaseline = false))
    composeTestRule.onNodeWithTag("btn_home_baseline").assertExists()
    composeTestRule.onNodeWithText("¿Aún sin tu punto de partida? Encuéntralo en 5 minutos").assertExists()
  }

  @Test
  fun la_isla_es_decorativa_con_una_descripcion_de_grupo_y_cada_edificio_es_un_boton_con_su_nombre() {
    show(ui(completed = 1))
    composeTestRule.onNodeWithContentDescription("Tu isla: hoy la ruta pasa por el Observatorio, el Hangar y la Biblioteca").assertExists()
    for (name in listOf("Observatorio, Memoria", "Antena, Atención", "Biblioteca, Lenguaje", "Hangar, Razonamiento")) composeTestRule.onNodeWithContentDescription(name).assertExists()
    // las áreas de las paradas hechas dicen «hecho hoy» (Bodega = Memoria = Observatorio)
    composeTestRule.onNodeWithTag("isla_observatorio").assertExists()
    composeTestRule.onNodeWithTag("isla_hangar").performClick()
    assertEquals("RAZONAMIENTO", placeTapped)
    composeTestRule.onNodeWithTag("isla_antena").performClick()
    assertEquals("ATENCION", placeTapped)
  }

  // ---- Los luceros del fondo (Tarea 83): ningún texto de Hoy que va directo sobre el fondo cae sobre un lucero, en cada estado y tamaño.
  // Se compara la CAJA de cada texto (más grande que sus letras, así que es estricto) con la capa `fondo_luceros` recortada como la pinta `BoardBackground` (ContentScale.Crop, centrado).

  private val textsOnBackground = listOf("home_greeting", "home_date", "home_tier", "home_week_caption", "streak_pill", "btn_home_baseline")

  private fun assertNoLucerosUnderTexts(ui: HomeUi, widthDp: Int, heightDp: Int, fontScale: Float = 1f) {
    show(ui, widthDp, heightDp, fontScale)
    val luceros = android.graphics.BitmapFactory.decodeFile("src/main/res/drawable-nodpi/fondo_luceros.webp")
    val root = composeTestRule.onRoot().fetchSemanticsNode().boundsInRoot
    val k = maxOf(root.width / luceros.width, root.height / luceros.height)
    val x0 = (luceros.width * k - root.width) / 2f
    val y0 = (luceros.height * k - root.height) / 2f
    val margin = 0   // la caja de un texto ya es más grande que sus letras (incluye el espacio sobre las mayúsculas): sin margen extra
    val hits = mutableListOf<String>()
    var checked = 0
    for (tag in textsOnBackground) for (node in composeTestRule.onAllNodes(androidx.compose.ui.test.hasTestTag(tag)).fetchSemanticsNodes()) {
      val b = node.boundsInRoot; checked++
      val ix1 = (((b.left + x0) / k).toInt() - margin).coerceIn(0, luceros.width - 1); val ix2 = (((b.right + x0) / k).toInt() + margin).coerceIn(0, luceros.width - 1)
      val iy1 = (((b.top + y0) / k).toInt() - margin).coerceIn(0, luceros.height - 1); val iy2 = (((b.bottom + y0) / k).toInt() + margin).coerceIn(0, luceros.height - 1)
      var n = 0
      for (y in iy1..iy2) for (x in ix1..ix2) if ((luceros.getPixel(x, y) ushr 24) >= 128) n++
      if (n > 0) hits += "«$tag» (caja $b) tiene $n píxeles de lucero debajo"
    }
    assertTrue("hay textos de Hoy sobre luceros a $widthDp × $heightDp, letra $fontScale: $hits", hits.isEmpty())
    assertTrue("se revisaron al menos 5 textos ($checked)", checked >= 5)
  }

  @Test fun sin_luceros_bajo_el_texto_normal_393() = assertNoLucerosUnderTexts(ui(), 393, 873)
  @Test fun sin_luceros_bajo_el_texto_con_el_enlace_393() = assertNoLucerosUnderTexts(ui(hasBaseline = false), 393, 873)
  @Test fun sin_luceros_bajo_el_texto_con_partida_en_pausa_393() = assertNoLucerosUnderTexts(ui(paused = "Carga exacta", hasBaseline = false), 393, 873)
  @Test fun sin_luceros_bajo_el_texto_con_ruta_completa_393() = assertNoLucerosUnderTexts(ui(completed = 3, hasBaseline = false), 393, 873)
  @Test fun sin_luceros_bajo_el_texto_con_el_primer_vuelo_393() = assertNoLucerosUnderTexts(ui(completed = 3, flightDay = true, hasBaseline = false), 393, 873)
  @Test fun sin_luceros_bajo_el_texto_normal_360() = assertNoLucerosUnderTexts(ui(), 360, 780)
  @Test fun sin_luceros_bajo_el_texto_con_el_enlace_360() = assertNoLucerosUnderTexts(ui(hasBaseline = false), 360, 780)
  @Test fun sin_luceros_bajo_el_texto_con_partida_en_pausa_360() = assertNoLucerosUnderTexts(ui(paused = "Carga exacta", hasBaseline = false), 360, 780)
  @Test fun sin_luceros_bajo_el_texto_con_ruta_completa_360() = assertNoLucerosUnderTexts(ui(completed = 3, hasBaseline = false), 360, 780)
  @Test fun sin_luceros_bajo_el_texto_en_el_emulador_411() = assertNoLucerosUnderTexts(ui(hasBaseline = false), 411, 914)

  private fun androidx.compose.ui.test.junit4.ComposeContentTestRule.onAllNodesWithTagCount(tag: String) =
    onAllNodes(androidx.compose.ui.test.hasTestTag(tag)).fetchSemanticsNodes().size
}
