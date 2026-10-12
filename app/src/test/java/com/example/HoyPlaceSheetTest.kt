package com.example

import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.click
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.LocalTabSwipeLock
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeUi
import com.example.ui.screens.PlaceFx
import com.example.ui.screens.homeStops
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.util.Calendar

/**
 * La lámina del lugar de Hoy (Tarea 85; boceto «A»): se abre al tocar un edificio, con el AVANCE del área, sus juegos y «Jugar en …» fijo abajo. Aquí: contenido de los cuatro lugares (uno sin medir), que el botón quede dentro de la pantalla y sobre el inset de navegación,
 * Atrás / X / velo la cierran, el pager se bloquea mientras está abierta, la línea de tiempo (mirando → vuelo → posada) y «quitar animaciones». Capturas con Roborazzi: los cuatro lugares a 393 dp, y uno a 360, con letra 1,3 y con letra 2,0.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class HoyPlaceSheetTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 12, 10, 0, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(false, false, false, false, false, false, false), todayIndex = 0)
  private val ui = HomeUi(
    name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 0, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(listOf("bodega", "calculo", "intrusa"), 0, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true,
    places = PlaceTestData.places(now)
  )

  private var played: String? = null
  private val fx = PlaceFx()
  private val lock = mutableIntStateOf(0)

  private fun show(widthDp: Int = 393, heightDp: Int = 873, fontScale: Float = 1f, reduce: Boolean = false) {
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalTabSwipeLock provides lock, LocalReduceMotion provides reduce) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            HomeContent(
              ui = ui, onProfile = {}, onSettings = {}, onPlaceGame = { played = it }, onStop = {}, onPlay = {}, onResume = {}, onBaseline = {}, fx = fx,
              modifier = Modifier.padding(top = 24.dp, bottom = 48.dp)       // 24 dp de barra de estado y 48 de barra de navegación (con la lámina abierta la barra de pestañas se retira)
            )
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun open(place: String) {
    rule.onNodeWithTag("isla_$place").performClick()
    rule.waitForIdle()
  }

  private fun bounds(tag: String): Rect = rule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot

  private fun pressBack() { rule.runOnUiThread { rule.activity.onBackPressedDispatcher.onBackPressed() }; rule.waitForIdle() }

  // ---- Contenido de los cuatro lugares

  @Test
  fun el_observatorio_muestra_la_memoria_que_avanzo() {
    show(); open("observatorio")
    rule.onNodeWithTag("place_sheet").assertIsDisplayed()
    rule.onNodeWithTag("place_title").assertIsDisplayed()
    rule.onNodeWithText("Observatorio").assertExists()
    rule.onNodeWithTag("place_area").assertIsDisplayed()
    rule.onNodeWithText("Tu avance aquí").assertIsDisplayed()
    rule.onNodeWithTag("place_stage").assertIsDisplayed()
    assertEquals("[Aprendiz]", rule.onNodeWithTag("place_stage").fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.Text].toString())
    rule.onNodeWithText("22 de 100").assertIsDisplayed()
    rule.onNodeWithText("Esta semana avanzó de 18 a 22").assertIsDisplayed()
    rule.onNodeWithText("Te faltan 18 para Hábil").assertIsDisplayed()
    for (i in 0..3) rule.onNodeWithTag("place_week_$i").assertIsDisplayed()
    for (v in listOf("12", "15", "18")) rule.onNodeWithText(v).assertExists()
    rule.onNodeWithText("Hace 3 sem.").assertExists(); rule.onNodeWithText("Hace 2 sem.").assertExists(); rule.onNodeWithText("Sem. pasada").assertExists(); rule.onNodeWithText("Esta semana").assertExists()
    rule.onNodeWithText("Tus juegos aquí").assertExists()
    for (g in ui.places.getValue("MEMORIA").games) rule.onNodeWithTag("place_game_${g.gameId}").assertExists()
    rule.onNodeWithText("Jugar en el Observatorio").assertIsDisplayed()
    rule.onNodeWithTag("place_chosen").assertIsDisplayed()
    rule.onNodeWithText("En el Observatorio vas en Aprendiz. Aquí tienes tu avance y sus juegos.").assertIsDisplayed()
  }

  @Test
  fun la_antena_muestra_la_atencion_que_se_mantiene() {
    show(); open("antena")
    rule.onNodeWithText("Esta semana se mantuvo en 31").assertIsDisplayed()
    rule.onNodeWithText("31 de 100").assertIsDisplayed()
    rule.onNodeWithText("Jugar en la Antena").assertIsDisplayed()
    rule.onNodeWithText("En la Antena vas en Aprendiz. Aquí tienes tu avance y sus juegos.").assertExists()
  }

  @Test
  fun el_hangar_muestra_el_razonamiento_que_recien_empieza_y_las_semanas_sin_dato() {
    show(); open("hangar")
    rule.onNodeWithText("9 de 100").assertIsDisplayed()
    assertEquals("[Inicio]", rule.onNodeWithTag("place_stage").fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.Text].toString())
    rule.onNodeWithText("—").assertExists()                         // la semana de hace 3 no tiene dato
    rule.onNodeWithText("Jugar en el Hangar").assertIsDisplayed()
  }

  @Test
  fun un_area_sin_medir_lo_dice_y_no_inventa_numeros() {
    show(); open("biblioteca")
    rule.onNodeWithText("Aún sin medir").assertIsDisplayed()
    rule.onNodeWithText("Aún sin medir: tu primera partida lo muestra").assertIsDisplayed()
    rule.onNodeWithText("En la Biblioteca aún no hay avance: juega uno y lo verás.").assertIsDisplayed()
    rule.onNodeWithText("Jugar en la Biblioteca").assertIsDisplayed()
    assertEquals("las cuatro semanas sin dato", 4, rule.onAllNodesWithTextCount("—"))
  }

  private fun androidx.compose.ui.test.junit4.ComposeContentTestRule.onAllNodesWithTextCount(text: String) = onAllNodes(androidx.compose.ui.test.hasText(text)).fetchSemanticsNodes().size

  // ---- «Jugar» siempre a la vista

  private fun assertPlayInside(widthDp: Int, heightDp: Int) {
    val density = rule.density.density
    val play = bounds("place_play")
    val navBar = 48 * density
    assertTrue("«Jugar» queda dentro de la pantalla y sobre la barra de navegación: $play (alto ${heightDp * density}, barra $navBar)", play.bottom <= heightDp * density - navBar + 1f && play.top >= 0f && play.left >= 0f && play.right <= widthDp * density + 1f)
    rule.onNodeWithTag("place_play").assertIsDisplayed()
    rule.onNodeWithTag("place_chosen").assertIsDisplayed()
  }

  @Test fun jugar_se_ve_a_393_por_873() { show(393, 873); open("observatorio"); assertPlayInside(393, 873) }
  @Test fun jugar_se_ve_a_360_dp() { show(360, 780); open("antena"); assertPlayInside(360, 780) }
  @Test fun jugar_se_ve_con_letra_1_3() { show(393, 873, 1.3f); open("hangar"); assertPlayInside(393, 873) }
  @Test fun jugar_se_ve_con_letra_2_0() { show(393, 873, 2f); open("observatorio"); assertPlayInside(393, 873) }

  // ---- El nombre del lugar nunca se parte a media palabra

  private fun assertTitleOneLine(fontScale: Float) {
    val title = bounds("place_title")
    val density = rule.density.density
    assertTrue("«Observatorio» cabe en una línea: $title", title.height <= 29f * fontScale * density * 1.3f)
    assertTrue("y dentro de la pantalla: $title", title.left >= 0f && title.right <= rule.onRoot().fetchSemanticsNode().boundsInRoot.right + 1f)
  }

  @Test fun el_nombre_cabe_en_una_linea_a_360_dp() { show(360, 780); open("observatorio"); assertTitleOneLine(1f) }
  @Test fun el_nombre_cabe_en_una_linea_con_letra_1_3() { show(393, 873, 1.3f); open("observatorio"); assertTitleOneLine(1.3f) }
  @Test fun el_nombre_cabe_en_una_linea_con_letra_2_0() { show(393, 873, 2f); open("observatorio"); assertTitleOneLine(2f) }

  @Test
  fun las_cuatro_semanas_miden_lo_mismo_de_alto() {
    show(); open("observatorio")
    val heights = (0..3).map { bounds("place_week_$it").height }
    assertTrue("alturas iguales: $heights", heights.all { kotlin.math.abs(it - heights[0]) < 1f })
  }

  // ---- Cerrar

  @Test
  fun atras_cierra_la_lamina() {
    show(); open("observatorio")
    assertEquals("observatorio", fx.key)
    pressBack()
    assertNull("sin lugar abierto", fx.key)
    assertEquals(0, rule.onAllNodes(hasTestTag("place_sheet")).fetchSemanticsNodes().size)
    rule.onNodeWithTag("home_isla").assertIsDisplayed()
  }

  @Test
  fun la_x_cierra_la_lamina() {
    show(); open("antena")
    rule.onNodeWithTag("place_close").performClick(); rule.waitForIdle()
    assertNull(fx.key)
    assertEquals(0, rule.onAllNodes(hasTestTag("place_sheet")).fetchSemanticsNodes().size)
  }

  @Test
  fun tocar_el_velo_cierra_la_lamina() {
    show(); open("hangar")
    // el velo se toca por donde se ve: arriba de la lámina (la lámina empieza al 22 % del alto)
    rule.onNodeWithTag("place_veil").performTouchInput { click(androidx.compose.ui.geometry.Offset(width / 2f, 40f)) }
    rule.waitForIdle()
    assertNull(fx.key)
  }

  @Test
  fun despues_de_cerrar_se_puede_abrir_otro_lugar() {
    show(); open("observatorio"); rule.onNodeWithTag("place_close").performClick(); rule.waitForIdle()
    open("biblioteca")
    assertEquals("biblioteca", fx.key)
    rule.onNodeWithText("Jugar en la Biblioteca").assertIsDisplayed()
  }

  // ---- El pager y los juegos

  @Test
  fun con_la_lamina_abierta_el_pager_queda_bloqueado_y_al_cerrar_se_libera() {
    show()
    assertEquals(0, lock.intValue)
    open("observatorio")
    assertTrue("bloqueado al abrir", lock.intValue > 0)
    pressBack()
    assertEquals("liberado al cerrar", 0, lock.intValue)
  }

  @Test
  fun tocar_una_fila_de_juego_lo_abre_a_jugar() {
    show(); open("observatorio")
    val first = ui.places.getValue("MEMORIA").games.first().gameId
    rule.onNodeWithTag("place_game_$first").performClick()
    assertEquals(first, played)
  }

  @Test
  fun el_boton_juega_el_juego_que_eligio_Nubi() {
    show(); open("observatorio")
    rule.onNodeWithTag("place_play").performClick()
    assertNotNull(played)
    assertEquals(ui.places.getValue("MEMORIA").chosenGameId, played)
  }

  // ---- La línea de tiempo: mirando → velo y lámina → vuelo → posada → de vuelta

  @Test
  fun primero_mira_despues_vuela_y_al_final_queda_posada_con_la_sombra_apagada() {
    show()
    rule.mainClock.autoAdvance = false
    rule.onNodeWithTag("isla_observatorio").performClick()
    rule.mainClock.advanceTimeBy(400)
    assertEquals("observatorio", fx.key)
    assertEquals("a los 400 ms ya mira", 1f, fx.look.value, 1e-3f)
    assertEquals("el velo todavía no cae", 0f, fx.veil.value, 0f)
    assertEquals("Nubi todavía no vuela", 0f, fx.flight.value, 0f)
    rule.mainClock.advanceTimeBy(500)         // 900 ms: el velo y el vuelo ya empezaron
    assertTrue("el velo empezó", fx.veil.value > 0f)
    assertTrue("Nubi está en pleno vuelo (${fx.flight.value})", fx.flight.value in 0.05f..0.95f)
    assertTrue("la sombra se apaga", fx.shadow.value < 1f)
    rule.mainClock.advanceTimeBy(1500)
    assertEquals(1f, fx.veil.value, 1e-3f); assertEquals(1f, fx.sheet.value, 1e-3f); assertEquals(1f, fx.flight.value, 1e-3f)
    assertEquals(0f, fx.shadow.value, 1e-3f); assertEquals(1f, fx.globe.value, 1e-3f)
    rule.mainClock.autoAdvance = true
    rule.onNodeWithTag("place_nubi").assertExists()
    pressBack()
    assertEquals(0f, fx.veil.value, 1e-3f); assertEquals(0f, fx.sheet.value, 1e-3f); assertEquals(0f, fx.flight.value, 1e-3f)
    assertEquals("la sombra vuelve", 1f, fx.shadow.value, 1e-3f); assertEquals(0f, fx.look.value, 1e-3f)
  }

  @Test
  fun nubi_posada_no_tapa_el_nombre_del_lugar_y_su_globo_cabe_en_la_pantalla() {
    show(); open("observatorio")
    val nubiBox = bounds("place_nubi"); val title = bounds("place_title"); val globe = bounds("place_globe"); val sheet = bounds("place_sheet")
    val density = rule.density.density
    // `boundsInRoot` no cuenta la escala de la capa gráfica: Nubi posada mide 0,8 de su caja
    val nubiRight = nubiBox.left + nubiBox.width * 0.8f
    val nubiBottom = nubiBox.top + nubiBox.height * 0.8f
    assertTrue("el nombre empieza a la derecha de Nubi: nubi hasta $nubiRight, título $title", title.left >= nubiRight - 2 * density)
    assertTrue("Nubi se posa sobre el borde de la lámina: nubi ${nubiBox.top}-$nubiBottom, lámina ${sheet.top}", nubiBox.top < sheet.top && nubiBottom > sheet.top)
    assertTrue("el globo cabe en la pantalla: $globe", globe.right <= 393 * density && globe.left >= 0f)
    assertTrue("el globo se apoya sobre la lámina: $globe contra ${sheet.top}", globe.bottom <= sheet.top + 1f)
  }

  @Test
  fun la_lamina_empieza_en_el_22_por_ciento_y_tapa_la_isla() {
    show(); open("observatorio")
    val sheet = bounds("place_sheet"); val island = bounds("home_isla")
    val density = rule.density.density
    val viewport = (873 - 24 - 48) * density
    assertEquals("empieza al 22 % del alto de la pestaña", 24 * density + 0.22f * viewport, sheet.top, 3f)
    assertTrue("llega hasta abajo (sobre la barra del teléfono)", sheet.bottom >= (873 - 48) * density - 1f)
    assertTrue("tapa buena parte de la isla: isla $island, lámina $sheet", sheet.top < island.top + island.height * 0.5f)
  }

  // ---- «Quitar animaciones»

  @Test
  fun con_quitar_animaciones_la_lamina_aparece_en_su_sitio_sin_vuelo_ni_corrimiento() {
    show(reduce = true)
    rule.mainClock.autoAdvance = false
    rule.onNodeWithTag("isla_observatorio").performClick()
    rule.mainClock.advanceTimeBy(PlaceMotionReduced + 20L)
    assertFalse("sin movimiento", fx.moves)
    assertEquals("Nubi ya está posada", 1f, fx.flight.value, 0f)
    assertEquals("la sombra ya está apagada", 0f, fx.shadow.value, 0f)
    assertEquals(1f, fx.sheet.value, 1e-3f)
    rule.mainClock.autoAdvance = true
    rule.onNodeWithTag("place_sheet").assertIsDisplayed()
    pressBack()
    assertNull(fx.key)
  }

  // ---- Capturas

  private val PlaceMotionReduced = com.example.ui.components.PlaceMotion.REDUCED_FADE_MS

  @Test fun captura_observatorio_393() { show(); open("observatorio"); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/lamina-observatorio-393.png") }
  @Test fun captura_antena_393() { show(); open("antena"); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/lamina-antena-393.png") }
  @Test fun captura_hangar_393() { show(); open("hangar"); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/lamina-hangar-393.png") }
  @Test fun captura_biblioteca_sin_medir_393() { show(); open("biblioteca"); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/lamina-biblioteca-393.png") }
  @Test fun captura_observatorio_360() { show(360, 780); open("observatorio"); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/lamina-observatorio-360.png") }
  @Test fun captura_observatorio_letra_1_3() { show(fontScale = 1.3f); open("observatorio"); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/lamina-observatorio-letra13.png") }
  @Test fun captura_observatorio_letra_2_0() { show(fontScale = 2f); open("observatorio"); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/lamina-observatorio-letra20.png") }
}
