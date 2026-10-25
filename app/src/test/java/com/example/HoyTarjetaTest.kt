package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.assertCountEquals
import androidx.compose.ui.test.assertHasNoClickAction
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasAnyAncestor
import androidx.compose.ui.test.hasClickAction
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.hasText
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
import com.example.ui.screens.HomeUi
import com.example.ui.screens.homeStops
import com.example.ui.screens.isNight
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
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
 * Hoy con UNA sola tarjeta (Tarea 165, 24-oct; `auditoria-fable/hoy-reestructura.md`, opción 3, elegida por Ricardo): saludo, la semana, UNA tarjeta crema con la ruta de hoy (título, «1 de 3 · unos 4 minutos», los tres juegos como filas con su estado en palabras que NO se tocan, y el botón sol ADENTRO, el único sol),
 * Nubi posada en su esquina (de noche duerme), y debajo solo «Todos los juegos». Sin isla: solo aparece, dentro de la tarjeta, cuando la ruta queda completa (con Nubi celebrando). Capturas a 393 × 873 dp (el teléfono de referencia), a 360 dp de ancho, con letra 1,3 y con letra 2,0, sobre el fondo en dos capas
 * y con las barras del sistema simuladas por margen (24 dp arriba; abajo 163: la barra de Android y la de pestañas con su aire). La regla que manda: con letra 1,0 y 1,3 TODO Hoy cabe sin deslizar a 393 × 873; con letra más grande se desliza pero nada se corta ni se superpone. Ningún texto que va directo sobre el fondo cae sobre un lucero.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class HoyTarjetaTest {
  @get:Rule val composeTestRule = createComposeRule()

  // Un domingo a las 16:30 (para el saludo «Buenas tardes» y la fecha); la semana: lunes, martes y hoy.
  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 11, 16, 30, 0) }.timeInMillis
  private val night = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 11, 22, 10, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)

  private fun ui(completed: Int = 1, ids: List<String> = listOf("bodega", "calculo", "intrusa"), paused: String? = null, flightDay: Boolean = false, hasBaseline: Boolean = true, name: String = "Ricardo", at: Long = now) =
    HomeUi(
      name = name, now = at, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
      stops = homeStops(ids, completed, AppLanguage.SPANISH), pausedTitle = paused, flightDay = flightDay, hasBaseline = hasBaseline
    )

  private var played = 0
  private var resumed = 0

  private fun show(ui: HomeUi, widthDp: Int = 393, heightDp: Int = 873, fontScale: Float = 1f, onAllGames: () -> Unit = {}, wait: Boolean = true, reduce: Boolean = false) {
    composeTestRule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), com.example.ui.components.LocalReduceMotion provides reduce) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            HomeContent(
              ui = ui, onAllGames = onAllGames, onPlay = { played++ }, onResume = { resumed++ }, onBaseline = {},
              modifier = Modifier.padding(top = 24.dp, bottom = 163.dp)
            )
          }
        }
      }
    }
    if (wait) composeTestRule.waitForIdle() else composeTestRule.mainClock.advanceTimeBy(100)
  }

  private fun bounds(tag: String): Rect = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot
  private fun exists(tag: String) = composeTestRule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().isNotEmpty()
  private fun dp(px: Float) = px / composeTestRule.density.density

  // ---- La regla que manda y las capturas

  /** Todo lo de Hoy se ve entero sin deslizar: lo último, «Todos los juegos», termina antes de la barra (a 163 dp del borde de abajo). */
  private fun assertFitsWithoutScrolling(widthDp: Int, heightDp: Int, fontScale: Float, ui: HomeUi) {
    show(ui, widthDp, heightDp, fontScale)
    val viewportBottom = (heightDp - 163) * composeTestRule.density.density
    for (tag in listOf("home_greeting", "home_date", "home_week", "home_card", "home_route_title", "home_route_meta", "home_stop_1", "home_stop_2", "home_stop_3", "btn_home_play", "btn_all_games")) {
      try { composeTestRule.onNodeWithTag(tag).assertIsDisplayed() } catch (e: AssertionError) { throw AssertionError("«$tag» no se ve sin deslizar a $widthDp × $heightDp con letra $fontScale: ${bounds(tag)}", e) }
    }
    val last = bounds("btn_all_games")
    assertTrue("«Todos los juegos» termina en ${dp(last.bottom)} dp y la barra empieza en ${dp(viewportBottom)} dp: Hoy se desliza", last.bottom <= viewportBottom + 1f)
    println("HOY %d×%d letra %.1f: termina en %.0f dp de %.0f".format(widthDp, heightDp, fontScale, dp(last.bottom), dp(viewportBottom)))
  }

  @Test fun hoy_a_393_por_873_con_letra_1_0_todo_cabe_sin_deslizar() { assertFitsWithoutScrolling(393, 873, 1f, ui()); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-393.png") }
  @Test fun hoy_a_393_por_873_con_letra_1_3_todo_cabe_sin_deslizar() { assertFitsWithoutScrolling(393, 873, 1.3f, ui()); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-letra13.png") }
  @Test fun hoy_a_393_por_873_con_letra_1_3_y_a_mitad_de_ruta_todo_cabe() = assertFitsWithoutScrolling(393, 873, 1.3f, ui(completed = 2))
  @Test fun hoy_a_393_por_873_con_letra_1_3_y_la_primera_hora_todo_cabe() = assertFitsWithoutScrolling(393, 873, 1.3f, ui(completed = 0))

  @Test
  fun hoy_a_360_por_780_con_letra_1_0_todo_cabe_sin_deslizar() {
    assertFitsWithoutScrolling(360, 780, 1f, ui())
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-360.png")
  }

  @Test
  fun hoy_con_letra_2_0_se_desliza_pero_nada_se_corta_ni_se_superpone() {
    show(ui(hasBaseline = false), fontScale = 2f)
    assertNothingCutOrOverlapping(393)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-letra20.png")
  }

  @Test fun a_360_dp_con_letra_1_3_nada_se_corta_ni_se_superpone() { show(ui(), widthDp = 360, heightDp = 780, fontScale = 1.3f); assertNothingCutOrOverlapping(360) }

  /**
   * Ningún texto se corta a lo alto (`TextLayoutResult.didOverflowHeight`). A lo ancho no se mira aquí: dentro de `IntrinsicSize` el último diseño que guarda el texto es el de la medición intrínseca y `didOverflowWidth` da falsos positivos; el ancho lo cubre
   * [assertNothingCutOrOverlapping] y las capturas.
   */
  private fun assertNoTextCut(vararg texts: String) {
    for (text in texts) {
      val node = composeTestRule.onNodeWithText(text).fetchSemanticsNode()
      val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
      node.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
      val r = results.firstOrNull() ?: throw AssertionError("«$text» no dio su diseño de texto")
      assertTrue("«$text» se corta a lo alto", !r.didOverflowHeight)
    }
  }

  @Test
  fun con_letra_2_0_ningun_texto_de_Hoy_se_corta() {
    show(ui(completed = 0), fontScale = 2f)
    assertNoTextCut("Empezar la ruta · 3 juegos", "Tu ruta de hoy", "0 de 3 · unos 6 minutos", "Buenas tardes, Ricardo", "Bodega de carga", "Sigue")
  }

  @Test
  fun con_letra_1_3_ningun_texto_de_Hoy_se_corta() {
    show(ui(completed = 1), fontScale = 1.3f)
    assertNoTextCut("Seguir la ruta · 2 juegos", "Tu ruta de hoy", "Bodega de carga", "Buenas tardes, Ricardo", "Hecho", "Sigue", "Después")
  }

  @Test
  fun a_360_dp_ningun_texto_de_las_filas_se_corta() {
    show(ui(completed = 1), widthDp = 360, heightDp = 780)
    assertNoTextCut("Bodega de carga", "Carga exacta", "La estrella intrusa", "Después", "Sigue", "Hecho")
  }

  /** Cada pieza de Hoy que se mide va dentro del ancho de la pantalla (nada se corta) y ninguna pisa a otra (cajas que no se cruzan): afuera de la tarjeta y, por separado, adentro de ella. */
  private fun assertNothingCutOrOverlapping(widthDp: Int) {
    val width = widthDp * composeTestRule.density.density
    fun check(tags: List<String>, label: String) {
      val present = tags.filter { exists(it) }
      val rects = present.associateWith { bounds(it) }
      for ((tag, r) in rects) assertTrue("«$tag» se sale del ancho: $r (ancho $width)", r.left >= -1f && r.right <= width + 1f)
      val names = rects.keys.toList()
      for (i in names.indices) for (j in i + 1 until names.size) {
        val a = rects.getValue(names[i]); val b = rects.getValue(names[j])
        val w = minOf(a.right, b.right) - maxOf(a.left, b.left); val h = minOf(a.bottom, b.bottom) - maxOf(a.top, b.top)
        assertTrue("$label: «${names[i]}» $a se superpone con «${names[j]}» $b", w <= 1f || h <= 1f)
      }
    }
    check(listOf("home_greeting", "home_date", "home_week", "home_card", "btn_all_games", "btn_home_baseline"), "afuera de la tarjeta")
    check(listOf("home_route_title", "home_route_meta", "home_nubi", "home_stop_1", "home_stop_2", "home_stop_3", "btn_home_play", "home_route_done"), "adentro de la tarjeta")
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
  fun arriba_a_la_derecha_no_hay_botones_y_bajo_la_tarjeta_esta_solo_la_losa_todos_los_juegos() {
    // Tarea 158: el perfil es la pestaña «Tú»; en Hoy, la losa crema «Todos los juegos» (el dibujo de las cuatro casillas, texto de 17 sp o más). Tarea 165: va justo debajo de la tarjeta.
    var opened = 0
    show(ui(), onAllGames = { opened++ })
    composeTestRule.onNodeWithTag("btn_top_profile").assertDoesNotExist()
    composeTestRule.onNodeWithTag("btn_top_settings").assertDoesNotExist()
    composeTestRule.onNodeWithContentDescription("Tu perfil").assertDoesNotExist()
    composeTestRule.onNodeWithTag("btn_all_games").assertIsDisplayed()
    composeTestRule.onNodeWithText("Todos los juegos").assertIsDisplayed()
    val b = bounds("btn_all_games"); val d = composeTestRule.density.density
    assertTrue("la losa es ancha (${b.width / d} dp) y alta (${b.height / d} dp): un botón, no un enlace chico", b.width >= 300f * d && b.height >= 48f * d)
    assertTrue("va DEBAJO de la tarjeta", b.top >= bounds("home_card").bottom - 1f)
    composeTestRule.onNodeWithTag("btn_all_games").performClick()
    assertEquals("tocar la losa abre «Todos los juegos»", 1, opened)
  }

  // ---- La tarjeta

  @Test
  fun la_tarjeta_lleva_el_titulo_la_meta_a_Nubi_las_tres_filas_y_el_boton_adentro() {
    show(ui(completed = 1))
    val card = bounds("home_card")
    for (tag in listOf("home_route_title", "home_route_meta", "home_nubi", "home_stop_1", "home_stop_2", "home_stop_3", "btn_home_play")) {
      val r = bounds(tag)
      assertTrue("«$tag» $r queda dentro de la tarjeta $card", r.left >= card.left - 1f && r.right <= card.right + 1f && r.top >= card.top - 1f && r.bottom <= card.bottom + 1f)
    }
    composeTestRule.onNodeWithText("Tu ruta de hoy").assertIsDisplayed()
    composeTestRule.onNodeWithText("1 de 3 · unos 4 minutos").assertIsDisplayed()
  }

  @Test
  fun Nubi_va_posada_en_la_esquina_de_arriba_a_la_derecha_de_la_tarjeta() {
    show(ui(completed = 1))
    val card = bounds("home_card"); val nubi = bounds("home_nubi")
    assertTrue("a la derecha: $nubi en $card", nubi.center.x > card.center.x && card.right - nubi.right < 24f * composeTestRule.density.density)
    assertTrue("arriba: $nubi en $card", nubi.top - card.top < 24f * composeTestRule.density.density)
    assertEquals("mide 84 dp de ancho", 84f, dp(nubi.width), 1f)
    assertEquals("y es UNA sola Nubi en Hoy", 1, composeTestRule.onAllNodes(hasTestTag("nubi_ojos_abiertos"), useUnmergedTree = true).fetchSemanticsNodes().size)
  }

  @Test
  fun de_noche_es_de_noche_de_las_20_a_las_6_y_Nubi_sigue_en_su_esquina() {
    fun at(h: Int, m: Int = 0) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 11, h, m, 0) }.timeInMillis
    assertTrue(isNight(at(20))); assertTrue(isNight(at(23, 59))); assertTrue(isNight(at(0))); assertTrue(isNight(at(5, 59)))
    assertFalse(isNight(at(6))); assertFalse(isNight(at(12))); assertFalse(isNight(at(19, 59)))
  }

  @Test
  fun de_noche_la_tarjeta_es_la_misma_con_Nubi_dormida() {
    show(ui(completed = 1, at = night))
    composeTestRule.onNodeWithText("Buenas noches, Ricardo").assertIsDisplayed()
    assertTrue(exists("home_nubi") && exists("btn_home_play"))
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-noche.png")
  }

  @Test
  fun las_filas_no_son_botones_ni_se_tocan_y_se_leen_una_a_una_con_su_estado() {
    show(ui(completed = 1))
    for (n in 1..3) {
      val config = composeTestRule.onNodeWithTag("home_stop_$n").fetchSemanticsNode().config
      assertFalse("la fila $n no tiene acción de toque", config.contains(SemanticsActions.OnClick))
      assertNull("ni rol de botón", config.getOrNull(SemanticsProperties.Role))
    }
    composeTestRule.onNodeWithContentDescription("Bodega de carga, hecho").assertExists()
    composeTestRule.onNodeWithContentDescription("Carga exacta, es el que sigue").assertExists()
    composeTestRule.onNodeWithContentDescription("La estrella intrusa, después").assertExists()
  }

  @Test
  fun en_la_tarjeta_el_unico_toque_es_el_boton() {
    show(ui(completed = 1))
    composeTestRule.onAllNodes(hasClickAction() and hasAnyAncestor(hasTestTag("home_card"))).assertCountEquals(1)
    composeTestRule.onNodeWithTag("btn_home_play").performClick()
    assertEquals(1, played)
    for (n in 1..3) composeTestRule.onNodeWithTag("home_stop_$n").performClick()          // tocar una fila no hace nada (ni abre una ficha, ni lanza un juego)
    assertEquals("las filas no lanzan nada", 1, played)
  }

  @Test
  fun las_filas_dicen_su_estado_en_palabras_y_con_marca_y_no_solo_con_color() {
    show(ui(completed = 1))
    composeTestRule.onNodeWithText("Hecho").assertIsDisplayed()
    composeTestRule.onNodeWithText("Sigue").assertIsDisplayed()
    composeTestRule.onNodeWithText("Después").assertIsDisplayed()
    assertEquals("el lugar del juego ya no se repite (no hay isla que lo muestre)", 0, composeTestRule.onAllNodes(hasText("Biblioteca")).fetchSemanticsNodes().size)
  }

  @Test
  fun las_tres_filas_van_una_debajo_de_otra_y_miden_48_dp_o_mas() {
    show(ui(completed = 0))
    val a = bounds("home_stop_1"); val b = bounds("home_stop_2"); val c = bounds("home_stop_3")
    assertTrue("una debajo de otra: $a $b $c", b.top >= a.bottom - 1f && c.top >= b.bottom - 1f && Math.abs(a.left - b.left) < 1f && Math.abs(b.left - c.left) < 1f)
    for (r in listOf(a, b, c)) assertTrue("48 dp o más de alto (${dp(r.height)})", dp(r.height) >= 48f)
  }

  @Test
  fun los_nombres_de_las_filas_van_a_16_sp_o_mas() {
    show(ui(completed = 1))
    for (n in 1..3) {
      val node = composeTestRule.onNodeWithTag("home_stop_title_$n", useUnmergedTree = true).fetchSemanticsNode()
      val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
      node.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
      assertTrue("el nombre de la fila $n va a 16 sp o más", (results.firstOrNull()?.layoutInput?.style?.fontSize?.value ?: 0f) >= 16f)
    }
  }

  /**
   * El botón de la ruta es el ÚNICO sol de la tarjeta (y de Hoy, aparte de la pestaña): Robolectric no deja leer píxeles aquí (`captureToImage` se cuelga; ver `OnBoardTextTest`), así que se vigila el CÓDIGO: `HomeScreen.kt` no pide el sol en ninguna parte
   * (ni `Clay.Sun` ni `SlabColor.SUN`: las filas van en tinta y crema, el estado «Sigue» con un punto de tinta) y los únicos `ClayButton` de la tarjeta —«Seguir tu partida» y la ruta— llevan el color por omisión, el sol, y nunca están los dos a la vez.
   */
  @Test
  fun el_boton_de_la_ruta_es_el_unico_sol_de_la_tarjeta() {
    val src = java.io.File("src/main/java/com/example/ui/screens/HomeScreen.kt").readText()
    val code = src.lines().filterNot { it.trim().startsWith("*") || it.trim().startsWith("/**") || it.trim().startsWith("//") }.joinToString("\n")
    assertFalse("Hoy no pide el sol con Clay.Sun", code.contains("Clay.Sun"))
    assertFalse("ni con SlabColor.SUN", code.contains("SlabColor.SUN"))
    val buttons = Regex("""ClayButton\(([^\n]*)""").findAll(code).map { it.groupValues[1] }.toList()
    assertEquals("dos botones de la tarjeta («Seguir tu partida» y la ruta), en ramas distintas de un `when`", 2, buttons.size)
    for (b in buttons) assertFalse("el botón lleva el color por omisión (el sol): $b", b.contains("color ="))
    assertTrue("y se eligen con un `when`: nunca los dos a la vez", code.contains("paused -> ClayButton") && code.contains("pending > 0 -> {"))
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
  fun al_empezar_dice_0_de_3_y_unos_6_minutos() {
    show(ui(completed = 0))
    composeTestRule.onNodeWithText("0 de 3 · unos 6 minutos").assertIsDisplayed()
  }

  @Test
  fun con_la_ruta_cumplida_la_tarjeta_muestra_la_isla_con_Nubi_y_ruta_completa_y_no_hay_boton_de_jugar() {
    show(ui(completed = 3))
    composeTestRule.onNodeWithTag("home_isla").assertIsDisplayed()
    composeTestRule.onNodeWithTag("home_route_done").assertExists()
    composeTestRule.onNodeWithText("Ruta completa").assertExists()
    composeTestRule.onNodeWithText("3 de 3 · completa").assertExists()
    composeTestRule.onNodeWithText("Mañana te espera una ruta nueva.").assertIsDisplayed()
    composeTestRule.onNodeWithTag("home_route_done").assertHasNoClickAction()                     // «Ruta completa» es un rótulo: no se toca ni parece botón
    composeTestRule.onNodeWithContentDescription("Ruta completa").assertExists()                  // y conserva su descripción
    assertEquals(0, composeTestRule.onAllNodes(hasTestTag("btn_home_play")).fetchSemanticsNodes().size)
    for (n in 1..3) assertFalse("con la isla no se repiten las filas", exists("home_stop_$n"))
    assertFalse("y Nubi no va en la esquina: está en la isla", exists("home_nubi"))
    val card = bounds("home_card"); val isla = bounds("home_isla")
    assertTrue("la isla queda dentro de la tarjeta", isla.left >= card.left - 1f && isla.right <= card.right + 1f && isla.top >= card.top - 1f && isla.bottom <= card.bottom + 1f)
    // los edificios ya no son botones
    for (tag in listOf("isla_observatorio", "isla_antena", "isla_biblioteca", "isla_hangar")) assertFalse("«$tag» no existe", exists(tag))
    composeTestRule.onNodeWithContentDescription("Tu isla: ruta completa, hechos el Observatorio, el Hangar y la Biblioteca").assertExists()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-completa.png")
  }

  @Test
  fun con_la_ruta_cumplida_y_letra_1_3_todo_cabe_sin_deslizar() {
    show(ui(completed = 3), fontScale = 1.3f)
    val viewportBottom = (873 - 163) * composeTestRule.density.density
    assertTrue("«Todos los juegos» termina en ${dp(bounds("btn_all_games").bottom)} dp de ${dp(viewportBottom)}", bounds("btn_all_games").bottom <= viewportBottom + 1f)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-completa-letra13.png")
  }

  @Test
  fun la_isla_chica_lleva_las_piezas_que_ganaste() {
    show(ui(completed = 3).copy(islandPieces = listOf(IslaPieza.C1, IslaPieza.V1, IslaPieza.VO1)))
    for (id in listOf("c1", "v1", "vo1")) assertTrue("pieza $id", exists("isla_pieza_$id"))
    composeTestRule.onNodeWithContentDescription("Tu isla: ruta completa, hechos el Observatorio, el Hangar y la Biblioteca · Tu isla tiene: dos arbustos, banderines y un muelle").assertExists()
  }

  @Test
  fun sin_la_ruta_completa_Hoy_no_lleva_isla_ni_edificios() {
    show(ui(completed = 1))
    assertFalse("no hay isla", exists("home_isla") || exists("isla"))
    for (tag in listOf("isla_observatorio", "isla_antena", "isla_biblioteca", "isla_hangar", "home_island_note")) assertFalse("«$tag» no existe", exists(tag))
  }

  @Test
  fun una_partida_en_pausa_se_retoma_con_el_boton_de_la_tarjeta_y_Hoy_dice_cual() {
    show(ui(paused = "Carga exacta"))
    composeTestRule.onNodeWithText("Seguir tu partida").performClick()
    assertEquals(1, resumed)
    composeTestRule.onNodeWithText("Tienes una partida en pausa: Carga exacta.").assertIsDisplayed()
    composeTestRule.onNodeWithTag("home_caption").assertIsDisplayed()
    composeTestRule.onAllNodes(hasTestTag("btn_home_play")).assertCountEquals(0)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-pausa.png")
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
    composeTestRule.onAllNodes(hasTestTag("btn_home_baseline")).assertCountEquals(0)
    composeTestRule.onAllNodes(hasText("Encuentra tu punto de partida")).assertCountEquals(0)
  }

  @Test
  fun sin_punto_de_partida_la_invitacion_sigue_a_mano_debajo_de_todos_los_juegos() {
    show(ui(hasBaseline = false))
    composeTestRule.onNodeWithTag("btn_home_baseline").assertExists()
    composeTestRule.onNodeWithText("Encuentra tu punto de partida").assertExists()
    assertTrue(bounds("btn_home_baseline").top >= bounds("btn_all_games").bottom - 1f)
  }

  // ---- Los luceros del fondo (~80, fijos detrás de las tres pestañas). Solo el encabezado de Hoy (saludo, fecha con la meta y racha) tiene que quedar sin lucero debajo: es el texto grande que cae siempre en el mismo lugar.
  // El resto de los textos de Hoy que van directo sobre el fondo (el enlace) puede tener un lucero detrás y se lee por su halo navy (`OnBoardText`); aquí solo se IMPRIME dónde cae («ZONAS …»). Lo de la ruta va dentro de la tarjeta crema: no se mira.
  // Se compara la CAJA de cada texto (más grande que sus letras, así que es estricto) con la capa `fondo_luceros` recortada como la pinta `BoardBackground` (ContentScale.Crop, centrado).

  private val headerTexts = listOf("home_greeting", "home_date", "streak_pill")
  private val textsOnBackground = headerTexts + listOf("btn_home_baseline")

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
