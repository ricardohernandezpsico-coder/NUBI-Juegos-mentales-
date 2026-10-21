package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.assertHeightIsAtLeast
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.assertCountEquals
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithContentDescription
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.IslaPieza
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeStop
import com.example.ui.screens.HomeUi
import com.example.ui.screens.ISLAND_MIN_HEIGHT
import com.example.ui.screens.homeStops
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.util.Calendar

/**
 * Hoy con la isla protagonista y «Tu ruta de hoy» (Tarea 84, distribución «A» de `docs/diseno-isla.md`). Capturas a 393 × 873 dp (el teléfono de referencia), a 360 dp de ancho, con letra 1,3 y con letra 2,0, sobre el fondo en dos capas y con las barras
 * del sistema simuladas por margen (24 dp arriba; abajo 48 dp de la barra de Android y 71 dp de la barra de pestañas actual). La regla que manda: con letra normal las TRES piedritas y el botón se ven sin deslizar y la isla toma el resto;
 * con letra grande se desliza, pero nada se corta ni se superpone. Y ningún texto que va directo sobre el fondo cae sobre un lucero.
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
      stops = homeStops(ids, completed, AppLanguage.SPANISH), pausedTitle = paused, flightDay = flightDay, hasBaseline = hasBaseline,
      places = PlaceTestData.places(now)
    )

  private var playedFromSheet: String? = null
  private var stopTapped: HomeStop? = null
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
              ui = ui, onProfile = {}, onSettings = {}, onPlaceGame = { playedFromSheet = it }, onPlaceGameCard = {}, onStop = { stopTapped = it }, onPlay = { played++ }, onResume = { resumed++ }, onBaseline = {},
              modifier = Modifier.padding(top = 24.dp, bottom = 163.dp)
            )
          }
        }
      }
    }
    composeTestRule.waitForIdle()
  }

  private fun bounds(tag: String): Rect = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot

  /** Imprime el alto de la isla en dp (para los informes: «ISLA 393×873 …»). */
  private fun printIsland(label: String) = println("ISLA %s: %.0f dp de alto, %.0f dp de ancho".format(label, bounds("home_isla").height / composeTestRule.density.density, bounds("home_isla").width / composeTestRule.density.density))

  // ---- La regla que manda y las capturas

  @Test
  fun hoy_a_393_por_873_las_tres_piedritas_y_el_boton_se_ven_sin_deslizar() {
    show(ui())
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-isla-393.png")
    for (tag in listOf("home_greeting", "home_date", "home_week", "home_isla", "home_route_title", "home_route_meta", "home_stop_1", "home_stop_2", "home_stop_3", "btn_home_play")) {
      try { composeTestRule.onNodeWithTag(tag).assertIsDisplayed() } catch (e: AssertionError) { throw AssertionError("«$tag» no se ve sin deslizar a 393 × 873: ${bounds(tag)}", e) }
    }
  }

  @Test
  fun la_isla_es_la_protagonista_y_toma_lo_que_sobra() {
    show(ui())
    printIsland("393×873 sin el enlace")
    composeTestRule.onNodeWithTag("home_isla").assertHeightIsAtLeast(340.dp)   // Tarea 123 (letra de 16 sp): 344 dp, 16 menos que con la letra de 14 (antes ≥ 360). A 393 × 873 con letra 1,0 y sin el enlace: más de 360 dp con la barra nueva de plasticina (371 en la Tarea 86; con la barra vieja eran ~417 y en la Tarea 83 ~240)
  }

  @Test
  fun con_el_enlace_del_punto_de_partida_la_isla_cede_ese_alto_pero_sigue_siendo_grande() {
    show(ui(hasBaseline = false))
    printIsland("393×873 con el enlace")
    composeTestRule.onNodeWithTag("home_isla").assertHeightIsAtLeast(300.dp)   // Tarea 123 (letra de 16 sp): 304 dp (antes ≥ 320); con el enlace en UNA línea (Tarea 87): 331 dp a 393 × 873 (con la frase de dos líneas eran 307)
    composeTestRule.onNodeWithTag("btn_home_baseline").assertIsDisplayed()
    composeTestRule.onNodeWithTag("btn_home_play").assertIsDisplayed()
  }

  @Test
  fun hoy_a_360_dp_de_ancho_tambien_muestra_las_piedritas_y_el_boton() {
    show(ui(), widthDp = 360, heightDp = 780)
    for (tag in listOf("home_stop_1", "home_stop_2", "home_stop_3", "btn_home_play")) composeTestRule.onNodeWithTag(tag).assertIsDisplayed()
    printIsland("360×780 sin el enlace")
    composeTestRule.onNodeWithTag("home_isla").assertHeightIsAtLeast(ISLAND_MIN_HEIGHT)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-isla-360.png")
  }

  @Test
  fun hoy_con_letra_1_3_se_desliza_pero_la_isla_no_se_pierde_y_nada_se_superpone() {
    show(ui(), fontScale = 1.3f)
    composeTestRule.onNodeWithTag("home_isla").assertIsDisplayed().assertHeightIsAtLeast(ISLAND_MIN_HEIGHT)
    composeTestRule.onNodeWithTag("home_greeting").assertIsDisplayed()
    assertNothingCutOrOverlapping(393)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-isla-letra13.png")
  }

  @Test
  fun hoy_con_letra_1_3_el_boton_principal_entra_en_pantalla_sin_deslizar_y_va_antes_de_la_lista() {
    // Tarea 143, ítem 3: con letra grande lo principal va primero («Seguir la ruta» antes de las paradas, que son la explicación y pueden quedar bajo el pliegue)
    show(ui(), fontScale = 1.3f)
    val viewportBottom = (873 - 163) * composeTestRule.density.density            // el alto de la pantalla menos la barra de pestañas y la del sistema
    val button = bounds("btn_home_play")
    assertTrue("«Seguir la ruta» se ve entero sin deslizar: $button (la pantalla acaba en $viewportBottom)", button.bottom <= viewportBottom + 1f)
    assertTrue("y va antes de la lista de paradas", button.top < bounds("home_stop_1").top)
  }

  @Test
  fun hoy_con_letra_normal_las_paradas_van_antes_del_boton() {
    show(ui())
    assertTrue("con letra normal el orden de siempre", bounds("home_stop_1").top < bounds("btn_home_play").top)
  }

  @Test
  fun la_tercera_parada_dice_despues_y_no_el_lugar() {
    show(ui(completed = 1))
    composeTestRule.onNodeWithText("Después").assertExists()
    assertEquals("el lugar ya no se repite en la piedrita (está en la isla)", 0, composeTestRule.onAllNodes(androidx.compose.ui.test.hasText("Biblioteca")).fetchSemanticsNodes().size)
  }

  @Test
  fun hoy_con_letra_2_0_se_desliza_y_nada_se_corta_ni_se_superpone() {
    show(ui(hasBaseline = false), fontScale = 2f)
    composeTestRule.onNodeWithTag("home_isla").assertHeightIsAtLeast(ISLAND_MIN_HEIGHT)
    assertNothingCutOrOverlapping(393)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-isla-letra20.png")
  }

  @Test
  fun a_360_dp_con_letra_1_3_nada_se_corta_ni_se_superpone() {
    show(ui(), widthDp = 360, heightDp = 780, fontScale = 1.3f)
    assertNothingCutOrOverlapping(360)
  }

  /**
   * Ningún texto se corta a lo alto (`TextLayoutResult.didOverflowHeight`): es lo que le pasaba al botón grande con letra 2,0 (alto fijo de 52 dp). A lo ancho no se mira aquí: dentro de `IntrinsicSize` el último diseño que guarda el texto es el de la medición
   * intrínseca y `didOverflowWidth` da falsos positivos; el ancho lo cubre [assertNothingCutOrOverlapping] y las capturas.
   */
  private fun assertNoTextCut(vararg texts: String) {
    for (text in texts) {
      val node = composeTestRule.onNodeWithText(text).fetchSemanticsNode()
      val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
      node.config[androidx.compose.ui.semantics.SemanticsActions.GetTextLayoutResult].action?.invoke(results)
      val r = results.firstOrNull() ?: throw AssertionError("«$text» no dio su diseño de texto")
      assertTrue("«$text» se corta a lo alto", !r.didOverflowHeight)
    }
  }

  @Test
  fun con_letra_2_0_ningun_texto_de_Hoy_se_corta() {
    show(ui(completed = 0), fontScale = 2f)
    // Las filas de las paradas no se miden aquí: su `TextLayoutResult` da falsos positivos dentro de `ClayCard`; se vieron enteras en el emulador con letra 2,0.
    assertNoTextCut("Empezar la ruta · 3 juegos", "Tu ruta de hoy", "0 de 3 · unos 6 minutos", "Buenas tardes, Ricardo")
  }

  @Test
  fun con_letra_1_3_ningun_texto_de_Hoy_se_corta() {
    show(ui(completed = 1), fontScale = 1.3f)
    assertNoTextCut("Seguir la ruta · 2 juegos", "Tu ruta de hoy", "Bodega de carga", "Buenas tardes, Ricardo")
  }

  @Test
  fun a_360_dp_ningun_texto_de_las_piedritas_se_corta() {
    show(ui(completed = 1), widthDp = 360, heightDp = 780)
    assertNoTextCut("Bodega de carga", "Carga exacta", "La estrella intrusa", "Después", "Sigue", "Hecho")
  }

  /** Cada pieza de Hoy que se mide va dentro del ancho de la pantalla (nada se corta) y ninguna pisa a otra (cajas que no se cruzan). */
  private fun assertNothingCutOrOverlapping(widthDp: Int) {
    val density = composeTestRule.density.density
    val width = widthDp * density
    val tags = listOf("home_greeting", "home_date", "home_week", "home_isla", "home_route_title", "home_route_meta", "home_stop_1", "home_stop_2", "home_stop_3", "btn_home_play", "btn_home_baseline")
      .filter { composeTestRule.onAllNodes(hasTestTag(it)).fetchSemanticsNodes().isNotEmpty() }
    val rects = tags.associateWith { bounds(it) }
    for ((tag, r) in rects) assertTrue("«$tag» se sale del ancho: $r (ancho $width)", r.left >= -1f && r.right <= width + 1f)
    val names = rects.keys.toList()
    for (i in names.indices) for (j in i + 1 until names.size) {
      val a = rects.getValue(names[i]); val b = rects.getValue(names[j])
      val w = minOf(a.right, b.right) - maxOf(a.left, b.left); val h = minOf(a.bottom, b.bottom) - maxOf(a.top, b.top)
      assertTrue("«${names[i]}» $a se superpone con «${names[j]}» $b", w <= 1f || h <= 1f)
    }
  }

  // ---- Encabezado

  @Test
  fun el_saludo_dice_el_momento_del_dia_el_nombre_y_la_fecha_con_la_meta_en_una_linea() {
    val u = ui()
    show(u)
    composeTestRule.onNodeWithText("Buenas tardes, Ricardo").assertIsDisplayed()
    composeTestRule.onNodeWithText(com.example.ui.screens.dateText(u.now) + " · 3 de 4 días de tu meta").assertIsDisplayed()          // «Domingo 11 oct. · 3 de 4 días de tu meta»: una línea a 16 sp (Tarea 153)
  }

  @Test
  fun sin_nombre_el_saludo_no_inventa_uno() {
    show(ui(name = ""))
    composeTestRule.onNodeWithText("Buenas tardes").assertIsDisplayed()
  }

  @Test
  fun la_semana_se_lee_en_palabras_como_un_solo_grupo_y_la_racha_se_ve() {
    show(ui())
    composeTestRule.onNodeWithContentDescription("Tu semana: 3 días con partida").assertExists()
    composeTestRule.onNodeWithTag("streak_pill").assertExists()
    composeTestRule.onNodeWithText("3 días").assertIsDisplayed()
  }

  @Test
  fun el_avatar_va_solo_con_su_inicial_porque_las_ligas_estan_fuera_de_la_v1() {
    // Tarea 149: sin ligas en la v1 el avatar ya no lleva el escudo y el lector dice solo «Tu perfil»
    show(ui())
    composeTestRule.onNodeWithContentDescription("Tu perfil").assertExists()
    composeTestRule.onNodeWithContentDescription("Tu perfil, liga Bronce").assertDoesNotExist()
    composeTestRule.onNodeWithTag("btn_top_profile").assertIsDisplayed()
  }

  // ---- «Tu ruta de hoy»

  @Test
  fun la_ruta_dice_cuantos_juegos_van_y_cuanto_falta() {
    show(ui(completed = 1))
    composeTestRule.onNodeWithText("Tu ruta de hoy").assertIsDisplayed()
    composeTestRule.onNodeWithText("1 de 3 · unos 4 minutos").assertIsDisplayed()
  }

  @Test
  fun al_empezar_dice_0_de_3_y_unos_6_minutos() {
    show(ui(completed = 0))
    composeTestRule.onNodeWithText("0 de 3 · unos 6 minutos").assertIsDisplayed()
  }

  @Test
  fun las_piedritas_dicen_su_estado_en_texto_y_con_forma_y_no_solo_con_color() {
    show(ui(completed = 1))
    composeTestRule.onNodeWithContentDescription("Bodega de carga, Observatorio, hecho").assertExists()
    composeTestRule.onNodeWithContentDescription("Carga exacta, Hangar, es la que sigue").assertExists()
    composeTestRule.onNodeWithContentDescription("La estrella intrusa, Biblioteca, después").assertExists()
    composeTestRule.onNodeWithText("Hecho").assertIsDisplayed()          // la hecha: visto + «Hecho»
    composeTestRule.onNodeWithText("Sigue").assertIsDisplayed()          // la que sigue: «Sigue» en lugar del lugar
    composeTestRule.onNodeWithText("Después").assertIsDisplayed()        // la que viene después: «Después» (Tarea 143, ítem 18; antes su lugar, «Biblioteca», que ya se ve en la isla)
  }

  @Test
  fun las_tres_piedritas_van_en_fila_y_miden_lo_mismo() {
    show(ui(completed = 0))
    val a = bounds("home_stop_1"); val b = bounds("home_stop_2"); val c = bounds("home_stop_3")
    assertTrue("en fila: $a $b $c", a.right <= b.left + 1f && b.right <= c.left + 1f && Math.abs(a.top - b.top) < 1f && Math.abs(b.top - c.top) < 1f)
    assertEquals("mismo alto", a.height, b.height, 1f)
    assertEquals("mismo alto", b.height, c.height, 1f)
    assertTrue("88 dp como mínimo (84 + la sombra)", a.height / composeTestRule.density.density >= 84f)
  }

  @Test
  fun tocar_una_piedrita_abre_la_ficha_de_ese_juego() {
    show(ui(completed = 1))
    composeTestRule.onNodeWithTag("home_stop_2").performClick()
    assertEquals("calculo", stopTapped?.gameId)
    composeTestRule.onNodeWithTag("home_stop_3").performClick()
    assertEquals("intrusa", stopTapped?.gameId)
    composeTestRule.onNodeWithTag("home_stop_1").performClick()      // también las hechas (para repasar su ficha)
    assertEquals("bodega", stopTapped?.gameId)
  }

  @Test
  fun con_letra_grande_las_piedritas_son_filas_y_tambien_se_tocan() {
    show(ui(completed = 1), fontScale = 1.3f)
    val a = bounds("home_stop_1"); val b = bounds("home_stop_2")
    assertTrue("una debajo de otra: $a $b", b.top >= a.bottom - 1f && Math.abs(a.left - b.left) < 1f)
    composeTestRule.onNodeWithTag("home_stop_2").performClick()
    assertNotNull(stopTapped)
    assertEquals("calculo", stopTapped?.gameId)
  }

  // ---- El botón y lo que se dice debajo

  @Test
  fun el_boton_empieza_la_ruta_o_la_sigue() {
    show(ui(completed = 0))
    composeTestRule.onNodeWithText("Empezar la ruta · 3 juegos").performClick()
    assertEquals(1, played)
  }

  @Test
  fun a_mitad_de_camino_el_boton_dice_cuantos_juegos_faltan() {
    show(ui(completed = 2))
    composeTestRule.onNodeWithText("Seguir la ruta · 1 juego").assertExists()
    composeTestRule.onNodeWithText("2 de 3 · unos 2 minutos").assertExists()
  }

  @Test
  fun con_la_ruta_cumplida_hay_un_aviso_de_ruta_completa_y_no_un_boton_de_jugar() {
    show(ui(completed = 3))
    composeTestRule.onNodeWithTag("home_route_done").assertExists()
    composeTestRule.onNodeWithText("Ruta completa").assertExists()
    composeTestRule.onNodeWithText("3 de 3 · completa").assertExists()
    composeTestRule.onNodeWithText("Ya pasaste por la isla hoy. Mañana sigo aquí.").assertIsDisplayed()
    assertEquals(0, composeTestRule.onAllNodes(hasTestTag("btn_home_play")).fetchSemanticsNodes().size)
  }

  @Test
  fun una_partida_en_pausa_se_retoma_con_el_mismo_boton_grande_y_Hoy_dice_cual() {
    show(ui(paused = "Carga exacta"))
    composeTestRule.onNodeWithText("Seguir tu partida").performClick()
    assertEquals(1, resumed)
    composeTestRule.onNodeWithText("Tienes una partida en pausa: Carga exacta.").assertIsDisplayed()
  }

  @Test
  fun si_el_camino_fue_el_primer_vuelo_Hoy_lo_dice_y_no_hay_boton_de_jugar() {
    show(ui(completed = 3, flightDay = true))
    composeTestRule.onNodeWithTag("home_flight_day").assertIsDisplayed()
    composeTestRule.onNodeWithText("Tu primer vuelo fue tu ruta de hoy. Mañana empieza tu ruta.").assertExists()
  }

  @Test
  fun pasadas_5_partidas_o_7_dias_Hoy_ya_no_invita_al_punto_de_partida() {
    // Tarea 152 (B6.2): la invitación vive en Hoy solo al principio; después, en Avance («Tu punto de partida»)
    show(ui(hasBaseline = false).copy(baselineInvite = false))
    composeTestRule.onAllNodes(androidx.compose.ui.test.hasTestTag("btn_home_baseline")).assertCountEquals(0)
    composeTestRule.onAllNodes(androidx.compose.ui.test.hasText("Encuentra tu punto de partida")).assertCountEquals(0)
  }

  @Test
  fun sin_punto_de_partida_la_invitacion_sigue_a_mano() {
    show(ui(hasBaseline = false))
    composeTestRule.onNodeWithTag("btn_home_baseline").assertExists()
    composeTestRule.onNodeWithText("Encuentra tu punto de partida").assertExists()
  }

  // ---- Las piezas de la isla por medallas (Tarea 136, «Tu isla crece»): 0, 3 (arbustos, banderines y muelle: las primeras) y las 10, a 393 × 873 y a 360 × 780. Que no pisen edificios, zonas tocables, farolas ni a Nubi lo mide `IslaPiezasImagesTest` sobre los píxeles.

  private val tresPiezas = listOf(IslaPieza.C1, IslaPieza.V1, IslaPieza.VO1)

  private fun piezas(pieces: List<IslaPieza>, widthDp: Int, heightDp: Int, file: String) {
    show(ui().copy(islandPieces = pieces), widthDp, heightDp)
    assertEquals(pieces.map { it.id }, IslaPieza.entries.map { it.id }.filter { composeTestRule.onAllNodes(hasTestTag("isla_pieza_$it")).fetchSemanticsNodes().isNotEmpty() })
    for (tag in listOf("home_isla", "home_stop_1", "home_stop_2", "home_stop_3", "btn_home_play")) composeTestRule.onNodeWithTag(tag).assertIsDisplayed()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/$file")
  }

  @Test fun hoy_393_con_0_piezas() = piezas(emptyList(), 393, 873, "hoy-isla-393-piezas-0.png")
  @Test fun hoy_393_con_3_piezas() = piezas(tresPiezas, 393, 873, "hoy-isla-393-piezas-3.png")
  @Test fun hoy_393_con_las_10_piezas() = piezas(IslaPieza.entries, 393, 873, "hoy-isla-393-piezas-10.png")
  @Test fun hoy_360_con_0_piezas() = piezas(emptyList(), 360, 780, "hoy-isla-360-piezas-0.png")
  @Test fun hoy_360_con_3_piezas() = piezas(tresPiezas, 360, 780, "hoy-isla-360-piezas-3.png")
  @Test fun hoy_360_con_las_10_piezas() = piezas(IslaPieza.entries, 360, 780, "hoy-isla-360-piezas-10.png")

  @Test
  fun con_las_10_piezas_los_cuatro_edificios_siguen_siendo_botones_con_su_nombre() {
    show(ui(completed = 1).copy(islandPieces = IslaPieza.entries))
    for (name in listOf("Observatorio, Memoria", "Antena, Atención", "Biblioteca, Lenguaje", "Hangar, Razonamiento")) composeTestRule.onNodeWithContentDescription(name).assertExists()
  }

  // ---- La isla

  @Test
  fun la_isla_es_decorativa_con_una_descripcion_de_grupo_y_cada_edificio_es_un_boton_con_su_nombre() {
    show(ui(completed = 1))
    composeTestRule.onNodeWithContentDescription("Tu isla: hecho el Observatorio; faltan el Hangar y la Biblioteca").assertExists()   // con una parada hecha (Tarea 88: la descripción incluye el estado de la ruta)
    for (name in listOf("Observatorio, Memoria", "Antena, Atención", "Biblioteca, Lenguaje", "Hangar, Razonamiento")) composeTestRule.onNodeWithContentDescription(name).assertExists()
    composeTestRule.onNodeWithTag("isla_observatorio").assertExists()
    composeTestRule.onNodeWithTag("isla_hangar").assertExists()
  }

  // ---- Los luceros del fondo (Tarea 87: ahora hay ~80, fijos detrás de las tres pestañas). Solo el encabezado de Hoy (saludo, fecha con la meta y racha) tiene que quedar sin lucero debajo: es el texto grande que cae siempre en el mismo lugar.
  // El resto de los textos de Hoy que van directo sobre el fondo (la ruta, los avisos y el enlace) puede tener un lucero detrás y se lee por su halo navy (`OnBoardText`); aquí solo se IMPRIME dónde cae cada uno («ZONAS …»).
  // Se compara la CAJA de cada texto (más grande que sus letras, así que es estricto) con la capa `fondo_luceros` recortada como la pinta `BoardBackground` (ContentScale.Crop, centrado).

  private val headerTexts = listOf("home_greeting", "home_date", "streak_pill")
  private val textsOnBackground = headerTexts + listOf("home_route_title", "home_route_meta", "home_caption", "home_flight_day", "btn_home_baseline")

  private fun assertNoLucerosUnderTexts(ui: HomeUi, widthDp: Int, heightDp: Int, fontScale: Float = 1f) {
    show(ui, widthDp, heightDp, fontScale)
    val luceros = android.graphics.BitmapFactory.decodeFile("src/main/res/drawable-nodpi/fondo_luceros.webp")
    val root = composeTestRule.onRoot().fetchSemanticsNode().boundsInRoot
    val k = maxOf(root.width / luceros.width, root.height / luceros.height)
    val x0 = (luceros.width * k - root.width) / 2f
    val y0 = (luceros.height * k - root.height) / 2f
    val hits = mutableListOf<String>()
    var checked = 0
    for (tag in textsOnBackground) for (node in composeTestRule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes()) {
      val b = node.boundsInRoot
      if (tag in headerTexts) checked++
      val ix1 = (((b.left + x0) / k).toInt()).coerceIn(0, luceros.width - 1); val ix2 = (((b.right + x0) / k).toInt()).coerceIn(0, luceros.width - 1)
      val iy1 = (((b.top + y0) / k).toInt()).coerceIn(0, luceros.height - 1); val iy2 = (((b.bottom + y0) / k).toInt()).coerceIn(0, luceros.height - 1)
      var n = 0
      for (y in iy1..iy2) for (x in ix1..ix2) if ((luceros.getPixel(x, y) ushr 24) >= 128) n++
      println("ZONAS %d×%d letra %.1f: %s y %.3f–%.3f x %.3f–%.3f de la imagen, %d píxeles de lucero debajo".format(widthDp, heightDp, fontScale, tag, iy1.toFloat() / luceros.height, iy2.toFloat() / luceros.height, ix1.toFloat() / luceros.width, ix2.toFloat() / luceros.width, n))
      if (n > 0 && tag in headerTexts) hits += "«$tag» (caja $b) tiene $n píxeles de lucero debajo"
    }
    assertTrue("hay luceros bajo el encabezado de Hoy a $widthDp × $heightDp, letra $fontScale: $hits", hits.isEmpty())
    assertTrue("se revisaron el saludo, la fecha y la racha ($checked)", checked >= 2)
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
  @Test fun sin_luceros_bajo_el_texto_en_el_emulador_411_con_partida_en_pausa() = assertNoLucerosUnderTexts(ui(paused = "Carga exacta", hasBaseline = false), 411, 914)
}
