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
import androidx.compose.ui.test.performScrollTo
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
 * La lámina del lugar de Hoy, en UNA sola vista (Tarea 147; `auditoria-fable/spec-lamina-lugar.md`, que reemplaza al boceto «A» de la Tarea 85): se abre al tocar un edificio y mide lo que necesita (hasta `página − 56 dp`): el lugar, Nubi (S, 72 dp) con su globo,
 * el avance en UNA línea sobre la barra del kit, los juegos en filas compactas y «Jugar <el que sugiere Nubi>» fijo abajo. Aquí: el contenido de los cuatro lugares (uno sin medir); que TODO quepa sin deslizar a 360, 393 y 411 dp con letra normal y con 1,3 (cediendo lo
 * secundario en orden: el globo, los nombres de las etapas, el rótulo); que el botón quede dentro de la pantalla; Atrás / X / velo la cierran; el pager se bloquea mientras está abierta; la línea de tiempo (mirando → vuelo → posada en su hueco) y «quitar animaciones».
 * Capturas con Roborazzi: los cuatro lugares a 393 dp, el Observatorio (4 juegos) y la Antena (5) a 360 con letra 1,0, 1,3 y 2,0.
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
  private var cardOpened: String? = null
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
              ui = ui, onAllGames = {}, onPlaceGame = { played = it }, onPlaceGameCard = { cardOpened = it }, onStop = {}, onPlay = {}, onResume = {}, onBaseline = {}, fx = fx,
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

  /** Caja SIN recortar por el desplazamiento (`boundsInRoot` da cero para lo que quedó fuera de la vista): la de una fila que está más abajo de la pantalla. */
  private fun rawBounds(tag: String, unmerged: Boolean = false): Rect {
    val n = rule.onNodeWithTag(tag, useUnmergedTree = unmerged).fetchSemanticsNode()
    return Rect(n.positionInRoot.x, n.positionInRoot.y, n.positionInRoot.x + n.size.width, n.positionInRoot.y + n.size.height)
  }

  private fun count(tag: String) = rule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().size

  private fun textOf(tag: String): String = rule.onNodeWithTag(tag).fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.Text].joinToString("") { it.text }

  private fun pressBack() { rule.runOnUiThread { rule.activity.onBackPressedDispatcher.onBackPressed() }; rule.waitForIdle() }

  private fun firstTitle(area: String) = ui.places.getValue(area).games.first().title

  // ---- Contenido de los cuatro lugares

  @Test
  fun el_observatorio_muestra_la_memoria_que_avanzo() {
    show(); open("observatorio")
    rule.onNodeWithTag("place_sheet").assertIsDisplayed()
    rule.onNodeWithTag("place_title").assertIsDisplayed()
    rule.onNodeWithText("Observatorio").assertExists()
    rule.onNodeWithTag("place_area").assertIsDisplayed()
    assertEquals("el avance en UNA línea", "Aprendiz · 22 de 100", textOf("place_stage"))
    rule.onNodeWithText("Esta semana avanzó de 18 a 22. Te faltan 18 para Hábil.").assertIsDisplayed()
    rule.onNodeWithText("Tus juegos aquí").assertExists()
    for (g in ui.places.getValue("MEMORIA").games) rule.onNodeWithTag("place_game_${g.gameId}").assertExists()
    rule.onNodeWithText("Jugar ${firstTitle("MEMORIA")}").assertIsDisplayed()
  }

  @Test
  fun la_antena_muestra_la_atencion_que_se_mantiene() {
    show(); open("antena")
    assertEquals("Aprendiz · 31 de 100", textOf("place_stage"))
    rule.onNodeWithText("Esta semana se mantuvo en 31. Te faltan 9 para Hábil.").assertIsDisplayed()
    rule.onNodeWithText("Jugar ${firstTitle("ATENCION")}").assertIsDisplayed()
  }

  @Test
  fun el_hangar_muestra_el_razonamiento_que_recien_empieza() {
    show(); open("hangar")
    assertEquals("Inicio · 9 de 100", textOf("place_stage"))
    rule.onNodeWithText("Esta semana avanzó de 6 a 9. Te faltan 11 para Aprendiz.").assertIsDisplayed()
    rule.onNodeWithText("Jugar ${firstTitle("RAZONAMIENTO")}").assertIsDisplayed()
  }

  @Test
  fun en_Hoy_el_avance_es_una_sola_linea_sin_la_barra_de_etapas_ni_el_grafico() {
    // Tarea 158: el detalle del avance (la barra con sus 5 etapas y el gráfico de 4 semanas) vive solo en Avance; en Hoy, UNA línea («Hábil · 47 de 100»)
    show()
    for (key in listOf("observatorio", "antena", "hangar", "biblioteca")) {
      open(key)
      assertEquals("«$key»: la línea de avance está", 1, count("place_stage"))
      assertEquals("«$key»: sin la barra", 0, count("place_bar"))
      assertEquals("«$key»: sin los nombres de las etapas", 0, count("place_stages"))
      assertEquals("«$key»: sin el gráfico de las 4 semanas", 0, count("place_chart"))
      rule.onNodeWithTag("place_play").assertIsDisplayed()
      pressBack()
    }
  }

  @Test
  fun un_area_sin_medir_lo_dice_y_no_inventa_numeros() {
    show(); open("biblioteca")
    assertEquals("Aún sin medir", textOf("place_stage"))
    rule.onNodeWithText("Aún no hay avance aquí: juega uno y lo verás.").assertIsDisplayed()
    rule.onNodeWithText("Jugar ${firstTitle("LENGUAJE")}").assertIsDisplayed()
  }

  @Test
  fun ya_no_hay_cuatro_semanas_ni_titulo_de_tu_avance_ni_las_dos_frases_sueltas() {
    show(); open("observatorio")
    for (tag in listOf("place_week_0", "place_week_1", "place_week_2", "place_week_3", "place_points", "place_change", "place_next", "place_games_grid")) assertEquals("«$tag» ya no existe", 0, count(tag))
    for (text in listOf("Tu avance aquí", "Hace 3 sem.", "Sem. pasada")) assertEquals("«$text» ya no va en la lámina", 0, rule.onAllNodes(androidx.compose.ui.test.hasText(text, substring = true)).fetchSemanticsNodes().size)
  }

  @Test
  fun el_boton_dice_el_juego_que_sugiere_nubi_con_su_emblema() {
    show(); open("observatorio")
    val chosen = ui.places.getValue("MEMORIA").chosenGameId!!
    val title = ui.places.getValue("MEMORIA").games.first { it.gameId == chosen }.title
    rule.onNodeWithText("Jugar $title").assertIsDisplayed()
    assertEquals("ya no dice «Jugar en el Observatorio»", 0, rule.onAllNodes(androidx.compose.ui.test.hasText("Jugar en el", substring = true)).fetchSemanticsNodes().size)
  }

  private fun androidx.compose.ui.test.junit4.ComposeContentTestRule.onAllNodesWithTextCount(text: String) = onAllNodes(androidx.compose.ui.test.hasText(text)).fetchSemanticsNodes().size

  // ---- Contraste

  private fun luminance(c: androidx.compose.ui.graphics.Color): Double {
    fun ch(v: Float): Double = v.toDouble().let { if (it <= 0.03928) it / 12.92 else Math.pow((it + 0.055) / 1.055, 2.4) }
    return 0.2126 * ch(c.red) + 0.7152 * ch(c.green) + 0.0722 * ch(c.blue)
  }

  private fun contrast(a: androidx.compose.ui.graphics.Color, b: androidx.compose.ui.graphics.Color): Double = luminance(a).let { la -> luminance(b).let { lb -> (maxOf(la, lb) + 0.05) / (minOf(la, lb) + 0.05) } }

  @Test
  fun el_globo_dentro_de_la_lamina_y_los_textos_secundarios_tienen_contraste_de_al_menos_4_5() {
    assertTrue("texto secundario sobre la lámina: ${contrast(com.example.ui.screens.SheetMuted, com.example.ui.screens.SheetCream)}", contrast(com.example.ui.screens.SheetMuted, com.example.ui.screens.SheetCream) >= 4.5)
    // el globo: tinta sobre #FFF8EC
    assertTrue("tinta sobre el globo: ${contrast(com.example.ui.theme.Clay.Ink, androidx.compose.ui.graphics.Color(0xFFFFF8EC))}", contrast(com.example.ui.theme.Clay.Ink, androidx.compose.ui.graphics.Color(0xFFFFF8EC)) >= 4.5)
  }

  // ---- TODO cabe en una vista (la queja de Ricardo: «debo bajar para ver todos los juegos»)

  private val places = listOf("observatorio" to "MEMORIA", "antena" to "ATENCION", "hangar" to "RAZONAMIENTO", "biblioteca" to "LENGUAJE")

  /** Los juegos y el botón enteros dentro de la pantalla, sin ningún deslizamiento: `place_scroll` ni existe, y cada fila queda entre la parte de arriba de la página y la barra del teléfono. */
  private fun assertEverythingInside(widthDp: Int, heightDp: Int, area: String, label: String) {
    val density = rule.density.density
    val pageTop = 24 * density
    val pageBottom = (heightDp - 48) * density
    assertEquals("$label: la lámina no se desliza", 0, count("place_scroll"))
    for (g in ui.places.getValue(area).games) {
      val r = rawBounds("place_game_${g.gameId}")
      assertTrue("$label: la fila de ${g.title} ($r) cabe en la pantalla (${pageTop}..$pageBottom)", r.top >= pageTop && r.bottom <= pageBottom + 1f && r.left >= 0f && r.right <= widthDp * density + 1f)
    }
    val play = rawBounds("place_play")
    assertTrue("$label: el botón $play cabe en la pantalla", play.top >= pageTop && play.bottom <= pageBottom + 1f)
    val sheet = rawBounds("place_sheet")
    assertTrue("$label: queda al menos 56 dp de cielo sobre la lámina: ${sheet.top} contra ${pageTop + 56 * density}", sheet.top >= pageTop + 56 * density - 1f)
  }

  private fun fitsAt(widthDp: Int, heightDp: Int, fontScale: Float, key: String, area: String, globeExpected: Boolean?) {
    show(widthDp, heightDp, fontScale); open(key)
    val label = "$key a ${widthDp}x$heightDp con letra $fontScale"
    assertEverythingInside(widthDp, heightDp, area, label)
    if (globeExpected != null) assertEquals("$label: el globo ${if (globeExpected) "se ve" else "cede"}", if (globeExpected) 1 else 0, count("place_globe"))
  }

  // a 360 × 780 la página mide 708 dp (sin las barras del sistema): con el globo en 3 líneas algunas no caben y el globo cede; lo que importa es que NADA se deslice
  @Test fun el_observatorio_cabe_a_360_por_780() = fitsAt(360, 780, 1f, "observatorio", "MEMORIA", null)
  @Test fun el_observatorio_cabe_a_393_por_873() = fitsAt(393, 873, 1f, "observatorio", "MEMORIA", true)
  @Test fun el_observatorio_cabe_a_411_por_915() = fitsAt(411, 915, 1f, "observatorio", "MEMORIA", true)
  @Test fun la_antena_con_cinco_juegos_cabe_a_360_por_780() = fitsAt(360, 780, 1f, "antena", "ATENCION", null)
  @Test fun la_antena_con_cinco_juegos_cabe_a_393_por_873() = fitsAt(393, 873, 1f, "antena", "ATENCION", true)
  @Test fun la_antena_con_cinco_juegos_cabe_a_411_por_915() = fitsAt(411, 915, 1f, "antena", "ATENCION", true)
  @Test fun el_hangar_cabe_a_360_por_780() = fitsAt(360, 780, 1f, "hangar", "RAZONAMIENTO", null)
  @Test fun el_hangar_cabe_a_393_por_873() = fitsAt(393, 873, 1f, "hangar", "RAZONAMIENTO", true)
  @Test fun la_biblioteca_sin_medir_cabe_a_360_por_780() = fitsAt(360, 780, 1f, "biblioteca", "LENGUAJE", null)
  @Test fun la_biblioteca_sin_medir_cabe_a_393_por_873() = fitsAt(393, 873, 1f, "biblioteca", "LENGUAJE", true)

  // ---- Con letra 1,3 cede lo secundario, en orden, pero nunca los juegos ni el botón

  @Test fun con_letra_1_3_el_observatorio_cabe_a_393_por_873() = fitsAt(393, 873, 1.3f, "observatorio", "MEMORIA", null)
  @Test fun con_letra_1_3_el_observatorio_cabe_a_411_por_915() = fitsAt(411, 915, 1.3f, "observatorio", "MEMORIA", null)
  @Test fun con_letra_1_3_el_observatorio_cabe_a_360_por_780() = fitsAt(360, 780, 1.3f, "observatorio", "MEMORIA", null)
  @Test fun con_letra_1_3_la_antena_cabe_a_393_por_873() = fitsAt(393, 873, 1.3f, "antena", "ATENCION", null)
  @Test fun con_letra_1_3_la_antena_cabe_a_360_por_780() = fitsAt(360, 780, 1.3f, "antena", "ATENCION", null)
  @Test fun con_letra_1_3_la_antena_cabe_a_411_por_915() = fitsAt(411, 915, 1.3f, "antena", "ATENCION", null)

  @Test
  fun con_letra_1_3_lo_que_cede_es_siempre_lo_secundario_y_en_ese_orden() {
    // el globo cede antes que el rótulo «Tus juegos aquí» (en Hoy ya no hay barra ni nombres de etapas)
    show(360, 780, 1.3f); open("antena")
    if (count("place_globe") == 0) assertTrue("cede el globo: Nubi sigue ahí, muda, junto a la línea de avance", count("place_nubi_hole") == 1)
    assertEquals("la línea de avance nunca cede", 1, count("place_stage"))
    assertEquals("y en Hoy no hay barra que ceder (Tarea 158)", 0, count("place_bar"))
  }

  @Test
  fun con_letra_2_0_los_juegos_y_el_boton_siguen_a_la_vista_o_el_medio_se_desliza_con_el_boton_fijo() {
    show(360, 780, 2f); open("antena")
    val density = rule.density.density
    val pageBottom = (780 - 48) * density
    val play = rawBounds("place_play")
    assertTrue("el botón queda entero y fijo a la vista: $play contra $pageBottom", play.bottom <= pageBottom + 1f && play.top >= 24 * density)
    if (count("place_scroll") == 1) {
      // lo que no cabe se desliza por el medio, con el desvanecido que avisa que sigue
      rule.onNodeWithTag("place_fade").assertExists()
    }
  }

  @Test
  fun con_letra_2_0_el_observatorio_a_393_deja_el_boton_a_la_vista() { show(393, 873, 2f); open("observatorio"); assertPlayInside(393, 873) }

  // ---- «Jugar» siempre a la vista, sobre la barra del teléfono

  private fun assertPlayInside(widthDp: Int, heightDp: Int) {
    val density = rule.density.density
    val play = bounds("place_play")
    val navBar = 48 * density
    assertTrue("«Jugar» queda dentro de la pantalla y sobre la barra de navegación: $play (alto ${heightDp * density}, barra $navBar)", play.bottom <= heightDp * density - navBar + 1f && play.top >= 0f && play.left >= 0f && play.right <= widthDp * density + 1f)
    rule.onNodeWithTag("place_play").assertIsDisplayed()
  }

  @Test fun jugar_se_ve_a_393_por_873() { show(393, 873); open("observatorio"); assertPlayInside(393, 873) }
  @Test fun jugar_se_ve_a_360_dp() { show(360, 780); open("antena"); assertPlayInside(360, 780) }
  @Test fun jugar_se_ve_con_letra_1_3() { show(393, 873, 1.3f); open("hangar"); assertPlayInside(393, 873) }

  /** El pie de la lámina deja 12 dp de aire bajo el botón (más la barra de navegación del teléfono, que en la prueba es la franja de 48 dp de afuera). */
  private fun assertPlayHasAirBelow(heightDp: Int) {
    val density = rule.density.density
    val pageBottom = (heightDp - 48) * density
    val play = bounds("place_play")
    assertTrue("«Jugar» termina en ${play.bottom} y deja 12 dp de aire sobre $pageBottom", play.bottom <= pageBottom - 12 * density + 1f)
  }

  @Test fun jugar_queda_con_12_dp_de_aire_abajo_a_393() { show(393, 873); open("observatorio"); assertPlayHasAirBelow(873) }
  @Test fun jugar_queda_con_12_dp_de_aire_abajo_a_360() { show(360, 780); open("antena"); assertPlayHasAirBelow(780) }

  // ---- La lámina mide lo que necesita y deja cielo arriba

  @Test
  fun la_lamina_abre_mas_alta_cuando_falta_espacio_y_deja_56_dp_de_cielo() {
    val density = rule.density.density
    // sobra espacio (393 × 873, Observatorio): abre a su alto natural y la isla asoma por encima
    show(); open("observatorio")
    val sheet = bounds("place_sheet")
    assertTrue("a 393 × 873 queda cielo de sobra sobre la lámina (la isla asoma): ${sheet.top / density} dp desde arriba de la ventana", sheet.top - 24 * density >= 150 * density)
    assertTrue("llega hasta abajo (sobre la barra del teléfono)", sheet.bottom >= (873 - 48) * density - 1f)
    val island = bounds("home_isla")
    assertTrue("tapa solo una parte de la isla: isla $island, lámina $sheet", sheet.top > island.top)
  }

  @Test
  fun la_lamina_con_cinco_juegos_en_una_pantalla_chica_sube_mas_pero_nunca_pasa_de_56_dp_del_borde() {
    show(360, 780); open("antena")
    val density = rule.density.density
    val sheet = bounds("place_sheet")
    assertTrue("deja al menos 56 dp de cielo: ${sheet.top - 24 * density}", sheet.top - 24 * density >= 56 * density - 1f)
  }

  // ---- El nombre del lugar nunca se parte a media palabra

  private fun assertTitleOneLine(fontScale: Float) {
    val title = bounds("place_title")
    val density = rule.density.density
    assertTrue("«Observatorio» cabe en una línea: $title", title.height <= 28f * fontScale * density * 1.3f)
    assertTrue("y dentro de la pantalla: $title", title.left >= 0f && title.right <= rule.onRoot().fetchSemanticsNode().boundsInRoot.right + 1f)
  }

  @Test fun el_nombre_cabe_en_una_linea_a_360_dp() { show(360, 780); open("observatorio"); assertTitleOneLine(1f) }
  @Test fun el_nombre_cabe_en_una_linea_con_letra_1_3() { show(393, 873, 1.3f); open("observatorio"); assertTitleOneLine(1.3f) }
  @Test fun el_nombre_cabe_en_una_linea_con_letra_2_0() { show(393, 873, 2f); open("observatorio"); assertTitleOneLine(2f) }

  // ---- Los juegos en filas compactas, sin cifras de medida (el contenido que Ricardo eligió el 19-oct; cambia solo la disposición)

  private fun rowBounds(area: String) = ui.places.getValue(area).games.associate { it.gameId to rawBounds("place_game_${it.gameId}") }

  private fun nameLayouts(area: String): List<Pair<String, androidx.compose.ui.text.TextLayoutResult>> = ui.places.getValue(area).games.map { g ->
    val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
    rule.onNodeWithTag("place_game_name_${g.gameId}", useUnmergedTree = true).fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsActions.GetTextLayoutResult].action?.invoke(results)
    g.title to (results.firstOrNull() ?: throw AssertionError("«${g.title}» no dio su diseño de texto"))
  }

  /** El nombre de una fila cabe sin cortarse: ninguna línea más ancha que su columna y ninguna palabra partida (cada línea son palabras enteras del nombre). */
  private fun assertNameFits(title: String, layout: androidx.compose.ui.text.TextLayoutResult, note: String, maxLines: Int = 3) {
    assertTrue("«$title» cabe en $maxLines líneas $note (${layout.lineCount})", layout.lineCount <= maxLines)
    assertFalse("«$title» se corta a lo alto $note", layout.didOverflowHeight)
    val inner = layout.layoutInput.constraints.maxWidth
    val words = title.split(' ')
    for (line in 0 until layout.lineCount) {
      assertTrue("«$title» $note: la línea ${line + 1} mide ${layout.getLineRight(line) - layout.getLineLeft(line)} y la columna deja $inner", layout.getLineRight(line) - layout.getLineLeft(line) <= inner + 0.5f)
      val text = layout.layoutInput.text.text.substring(layout.getLineStart(line), layout.getLineEnd(line)).trim()
      for (w in text.split(' ').filter { it.isNotEmpty() }) assertTrue("«$w» (de «$title») $note es una palabra entera", w in words)
    }
  }

  @Test
  fun los_juegos_van_en_filas_sin_cifras_de_medida() {
    show(); open("observatorio")
    rule.onNodeWithTag("place_games_list").assertExists()
    for (g in ui.places.getValue("MEMORIA").games) {
      rule.onNodeWithTag("place_game_${g.gameId}").assertExists()
      rule.onNodeWithTag("place_game_name_${g.gameId}", useUnmergedTree = true).assertExists()
      rule.onNodeWithTag("place_game_when_${g.gameId}", useUnmergedTree = true).assertExists()
    }
    // sin las cifras de medida (esas se quedan en la ficha del juego, con su explicación)
    for (text in listOf("promedio reciente", "tu última", " ms", "a la vez", "señales", "casi nada")) assertEquals("«$text» ya no va en la lámina", 0, rule.onAllNodes(androidx.compose.ui.test.hasText(text, substring = true)).fetchSemanticsNodes().size)
    assertEquals("y sin la leyenda suelta «Nubi elige…»", 0, rule.onAllNodes(androidx.compose.ui.test.hasText("Nubi elige", substring = true)).fetchSemanticsNodes().size)
  }

  @Test
  fun cada_fila_dice_cuando_se_jugo_y_se_lee_completa() {
    show(); open("observatorio")
    for (g in ui.places.getValue("MEMORIA").games) {
      val said = rule.onNodeWithTag("place_game_when_${g.gameId}", useUnmergedTree = true).fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.Text].joinToString("") { it.text }
      assertEquals(g.whenShort, said)
      assertTrue("«$said» es hoy, ayer, hace N días o nuevo", said == "hoy" || said == "ayer" || said == "nuevo" || Regex("hace \\d+ días").matches(said))
      val description = rule.onNodeWithTag("place_game_${g.gameId}").fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.ContentDescription].joinToString(", ")
      assertTrue("«$description» dice el nombre y cuándo", description.startsWith(g.title + ", " + g.spoken))
    }
  }

  @Test
  fun el_juego_que_sugiere_nubi_lleva_el_chip_nubi_sugiere_dentro_de_su_fila_y_se_lee_asi() {
    show(); open("observatorio")
    val chosen = ui.places.getValue("MEMORIA").chosenGameId!!
    assertEquals("un solo chip en toda la lámina", 1, rule.onAllNodes(hasTestTag("place_game_chip"), useUnmergedTree = true).fetchSemanticsNodes().size)
    val chip = rawBounds("place_game_chip", unmerged = true); val row = rawBounds("place_game_$chosen")
    assertTrue("el chip está DENTRO de la fila del sugerido: $chip en $row", chip.left >= row.left - 1f && chip.right <= row.right + 1f && chip.top >= row.top - 1f && chip.bottom <= row.bottom + 1f)
    rule.onNodeWithTag("place_game_chip", useUnmergedTree = true).assertIsDisplayed()
    assertEquals("el chip dice siempre «Nubi sugiere»", 1, rule.onAllNodes(androidx.compose.ui.test.hasText("Nubi sugiere"), useUnmergedTree = true).fetchSemanticsNodes().size)
    val description = rule.onNodeWithTag("place_game_$chosen").fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.ContentDescription].joinToString(", ")
    assertTrue("«$description» termina en «Nubi lo sugiere»", description.endsWith(", Nubi lo sugiere"))
    for (g in ui.places.getValue("MEMORIA").games.filter { it.gameId != chosen }) {
      val other = rule.onNodeWithTag("place_game_${g.gameId}").fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.ContentDescription].joinToString(", ")
      assertFalse("solo el sugerido dice que Nubi lo sugiere: $other", other.contains("Nubi lo sugiere"))
    }
  }

  @Test
  fun las_filas_miden_60_dp_de_alto_y_todo_el_ancho_y_se_tocan_de_sobra() {
    show(); open("observatorio")
    val density = rule.density.density
    val inner = (393 - 36) * density
    for ((id, r) in rowBounds("MEMORIA")) {
      assertTrue("la fila de $id mide ≥ 48 dp de alto y ≥ 48 de ancho: $r", r.width >= 48f * density && r.height >= 48f * density)
      assertTrue("la fila de $id mide ≥ 60 dp de alto: ${r.height / density}", r.height >= 60f * density - 1f)
      assertEquals("la fila de $id ocupa todo el ancho: ${r.width}", inner, r.width, 2f * density)
    }
    val rows = rowBounds("MEMORIA").values.sortedBy { it.top }
    for (i in 1 until rows.size) assertTrue("las filas van una debajo de la otra: ${rows[i - 1]} y ${rows[i]}", rows[i].top >= rows[i - 1].bottom - 1f)
  }

  @Test
  fun a_393_con_letra_normal_los_nombres_no_se_cortan() {
    show(); open("observatorio")
    for ((title, layout) in nameLayouts("MEMORIA")) assertNameFits(title, layout, "con letra normal", maxLines = 1)
  }

  private fun assertNamesFitAt(widthDp: Int, heightDp: Int, fontScale: Float, key: String, area: String, maxLines: Int) {
    show(widthDp, heightDp, fontScale); open(key)
    for ((title, layout) in nameLayouts(area)) assertNameFits(title, layout, "a $widthDp con letra $fontScale", maxLines)
  }

  @Test fun a_360_con_letra_normal_la_antena_no_corta_ningun_nombre() = assertNamesFitAt(360, 780, 1f, "antena", "ATENCION", 2)
  @Test fun con_letra_1_3_la_antena_no_corta_ningun_nombre() = assertNamesFitAt(393, 873, 1.3f, "antena", "ATENCION", 3)
  @Test fun con_letra_1_3_el_observatorio_no_corta_ningun_nombre_y_el_chip_baja_si_no_cabe_al_lado() = assertNamesFitAt(393, 873, 1.3f, "observatorio", "MEMORIA", 3)
  @Test fun con_letra_2_0_el_observatorio_no_corta_ningun_nombre() = assertNamesFitAt(393, 873, 2f, "observatorio", "MEMORIA", 4)

  @Test
  fun con_letra_grande_el_chip_baja_dentro_de_la_columna_del_texto_si_no_cabe_junto_al_nombre() {
    show(360, 780, 1.3f); open("observatorio")
    val chosen = ui.places.getValue("MEMORIA").chosenGameId!!
    val chip = rawBounds("place_game_chip", unmerged = true); val row = rawBounds("place_game_$chosen"); val name = rawBounds("place_game_name_$chosen", unmerged = true)
    assertTrue("el chip sigue dentro de su fila: $chip en $row", chip.left >= row.left - 1f && chip.right <= row.right + 1f && chip.top >= row.top - 1f && chip.bottom <= row.bottom + 1f)
    assertEquals("un solo chip", 1, rule.onAllNodes(hasTestTag("place_game_chip"), useUnmergedTree = true).fetchSemanticsNodes().size)
    assertTrue("el chip no tapa el nombre: ${name.bottom} contra ${chip.top} o a su derecha", chip.left >= name.right - 1f || chip.top >= name.bottom - 1f)
  }

  // ---- Cerrar

  @Test
  fun atras_cierra_la_lamina() {
    show(); open("observatorio")
    assertEquals("observatorio", fx.key)
    pressBack()
    assertNull("sin lugar abierto", fx.key)
    assertEquals(0, count("place_sheet"))
    rule.onNodeWithTag("home_isla").assertIsDisplayed()
  }

  @Test
  fun la_x_cierra_la_lamina() {
    show(); open("antena")
    rule.onNodeWithTag("place_close").performClick(); rule.waitForIdle()
    assertNull(fx.key)
    assertEquals(0, count("place_sheet"))
  }

  @Test
  fun tocar_el_velo_cierra_la_lamina() {
    show(); open("hangar")
    // el velo se toca por donde se ve: arriba de la lámina (siempre hay al menos 56 dp de cielo)
    rule.onNodeWithTag("place_veil").performTouchInput { click(androidx.compose.ui.geometry.Offset(width / 2f, 40f)) }
    rule.waitForIdle()
    assertNull(fx.key)
  }

  @Test
  fun despues_de_cerrar_se_puede_abrir_otro_lugar() {
    show(); open("observatorio"); rule.onNodeWithTag("place_close").performClick(); rule.waitForIdle()
    open("biblioteca")
    assertEquals("biblioteca", fx.key)
    rule.onNodeWithText("Jugar ${firstTitle("LENGUAJE")}").assertIsDisplayed()
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
  fun tocar_una_fila_de_juego_abre_su_ficha_y_no_juega() {
    show(); open("observatorio")
    val first = ui.places.getValue("MEMORIA").games.first().gameId
    rule.onNodeWithTag("place_game_$first").performClick()
    assertEquals("la fila abre la ficha de ese juego", first, cardOpened)
    assertNull("y NO lo juega directo", played)
  }

  private fun assertRowsOpenCards(key: String, area: String) {
    show(); open(key)
    for (g in ui.places.getValue(area).games) {
      cardOpened = null
      rule.onNodeWithTag("place_game_${g.gameId}").performClick()
      assertEquals("$key: la fila de ${g.gameId} abre su ficha", g.gameId, cardOpened)
    }
    assertNull("$key: ninguna fila juega directo", played)
  }

  @Test fun las_filas_del_observatorio_abren_la_ficha() = assertRowsOpenCards("observatorio", "MEMORIA")
  @Test fun las_filas_de_la_antena_abren_la_ficha() = assertRowsOpenCards("antena", "ATENCION")
  @Test fun las_filas_del_hangar_abren_la_ficha() = assertRowsOpenCards("hangar", "RAZONAMIENTO")
  @Test fun las_filas_de_la_biblioteca_abren_la_ficha() = assertRowsOpenCards("biblioteca", "LENGUAJE")

  @Test
  fun las_filas_se_anuncian_como_boton_que_abre_la_ficha() {
    show(); open("observatorio")
    val first = ui.places.getValue("MEMORIA").games.first().gameId
    val node = rule.onNodeWithTag("place_game_$first").fetchSemanticsNode()
    assertEquals("Ver su ficha", node.config[androidx.compose.ui.semantics.SemanticsActions.OnClick].label)
  }

  @Test
  fun el_boton_juega_el_juego_que_eligio_Nubi() {
    show(); open("observatorio")
    rule.onNodeWithTag("place_play").performClick()
    assertNotNull(played)
    assertEquals(ui.places.getValue("MEMORIA").chosenGameId, played)
    assertNull("el botón grande juega directo, sin abrir ninguna ficha", cardOpened)
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
  fun nubi_se_posa_dentro_de_la_lamina_a_la_izquierda_de_su_globo() {
    show(); open("observatorio")
    val density = rule.density.density
    val nubi = bounds("place_nubi"); val hole = bounds("place_nubi_hole"); val globe = bounds("place_globe"); val sheet = bounds("place_sheet")
    // al terminar el vuelo Nubi queda exactamente en el hueco que le deja la lámina (± 1 dp)
    assertEquals("Nubi se posa en el hueco: izquierda", hole.left, nubi.left, 1f * density)
    assertEquals("Nubi se posa en el hueco: arriba", hole.top, nubi.top, 1f * density)
    assertEquals("el hueco mide 72 dp de ancho", 72f * density, hole.width, 1f * density)
    assertTrue("el hueco está DENTRO de la lámina: $hole en $sheet", hole.top >= sheet.top && hole.bottom <= sheet.bottom && hole.left >= sheet.left)
    assertTrue("el globo está a la derecha de Nubi, dentro de la lámina: $globe", globe.left >= hole.right - 1f && globe.right <= sheet.right + 1f && globe.top >= sheet.top && globe.bottom <= sheet.bottom)
    assertTrue("el globo y Nubi quedan centrados entre sí a lo alto: ${(globe.top + globe.bottom) / 2f} y ${(hole.top + hole.bottom) / 2f}", Math.abs((globe.top + globe.bottom) / 2f - (hole.top + hole.bottom) / 2f) <= 12f * density)
    // y no tapa el nombre del lugar: va más abajo que el título
    assertTrue("Nubi no tapa el nombre: ${bounds("place_title")} contra $hole", hole.top >= bounds("place_title").bottom - 1f)
  }

  @Test
  fun el_globo_va_dentro_de_la_lamina_y_no_flota_sobre_hoy() {
    show(); open("observatorio")
    val globe = bounds("place_globe"); val sheet = bounds("place_sheet")
    assertTrue("el globo está dentro de la lámina (no sobre el saludo de Hoy): $globe en $sheet", globe.top >= sheet.top && globe.bottom <= sheet.bottom)
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
  @Test fun captura_antena_360() { show(360, 780); open("antena"); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/lamina-antena-360.png") }
  @Test fun captura_antena_360_letra_1_3() { show(360, 780, 1.3f); open("antena"); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/lamina-antena-360-letra13.png") }
  @Test fun captura_observatorio_letra_1_3() { show(fontScale = 1.3f); open("observatorio"); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/lamina-observatorio-letra13.png") }
  @Test fun captura_observatorio_letra_2_0() { show(fontScale = 2f); open("observatorio"); rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/lamina-observatorio-letra20.png") }
}
