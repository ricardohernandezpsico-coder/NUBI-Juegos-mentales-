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
import com.example.data.ChallengeState
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.screens.HomeChallenge
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeUi
import com.example.ui.screens.Sun
import com.example.ui.screens.homeStops
import com.example.ui.screens.isNight
import com.example.ui.screens.sunOf
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
 * Hoy con UNA sola tarjeta y el Desafío de hoy (Tareas 165 y 167; `auditoria-fable/hoy-reestructura.md` y `hoy-desafio-y-dia-completo.md`, opción 1 «Dos botones, un solo sol a la vez»): saludo, la semana, UNA tarjeta crema con la ruta de hoy (título, «1 de 3 · unos 4 minutos», los tres juegos como filas con su estado en palabras que NO se tocan, y el botón sol ADENTRO), Nubi posada en
 * su esquina (de noche duerme) y, debajo, «Desafío de hoy» (lila, con el cometa) junto a «Todos los juegos»; con la ruta completa la tarjeta se cierra sola (sin botón ni rótulo) y el sol pasa al Desafío, a todo el ancho. **Un solo sol a la vez.** Ya NO hay isla en Hoy. Capturas a 393 × 873 dp (el teléfono de referencia), a 360 dp de ancho, con letra 1,3 y con letra 2,0,
 * sobre el fondo en dos capas y con las barras del sistema simuladas por margen (24 dp arriba; abajo 163: la barra de Android y la de pestañas con su aire). La regla que manda: con letra 1,0 y 1,3 TODO Hoy cabe sin deslizar a 393 × 873 en los cuatro estados (y la pausa); con letra más grande se desliza pero nada se corta, se superpone ni parte una palabra.
 * Ningún texto que va directo sobre el fondo cae sobre un lucero.
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

  private val stroop = HomeChallenge("stroop", "Tinta o Palabra", ChallengeState.PENDIENTE)

  private fun ui(
    completed: Int = 1, ids: List<String> = listOf("bodega", "calculo", "intrusa"), paused: String? = null, flightDay: Boolean = false, hasBaseline: Boolean = true, name: String = "Ricardo", at: Long = now, challenge: HomeChallenge? = null
  ) =
    HomeUi(
      name = name, now = at, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
      stops = homeStops(ids, completed, AppLanguage.SPANISH), pausedTitle = paused, flightDay = flightDay, hasBaseline = hasBaseline, challenge = challenge
    )

  private var played = 0
  private var resumed = 0
  private var challenged = 0

  private class Shown(val ui: HomeUi, val widthDp: Int, val heightDp: Int, val fontScale: Float, val reduce: Boolean)
  private var shown: androidx.compose.runtime.MutableState<Shown>? = null
  private var allGamesCallback: () -> Unit = {}

  /** Muestra Hoy: la primera vez arma el contenido (un solo `setContent` por prueba); las siguientes cambian lo mostrado (el estado del día, el tamaño o la letra) en el mismo contenido. */
  private fun show(ui: HomeUi, widthDp: Int = 393, heightDp: Int = 873, fontScale: Float = 1f, onAllGames: () -> Unit = {}, wait: Boolean = true, reduce: Boolean = false) {
    allGamesCallback = onAllGames
    val args = Shown(ui, widthDp, heightDp, fontScale, reduce)
    val state = shown
    if (state != null) { state.value = args; composeTestRule.waitForIdle(); return }
    val holder = androidx.compose.runtime.mutableStateOf(args).also { shown = it }
    composeTestRule.setContent {
      val a = holder.value
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, a.fontScale), com.example.ui.components.LocalReduceMotion provides a.reduce) {
        NeuroVidaTheme {
          Box(Modifier.width(a.widthDp.dp).height(a.heightDp.dp)) {
            BoardBackground()
            HomeContent(
              ui = a.ui, onAllGames = { allGamesCallback() }, onPlay = { played++ }, onResume = { resumed++ }, onBaseline = {}, onChallenge = { challenged++ },
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

  /** Todo lo de Hoy se ve entero sin deslizar: lo último, «Todos los juegos» (o la invitación al punto de partida, si toca), termina antes de la barra (a 163 dp del borde de abajo). */
  private fun assertFitsWithoutScrolling(widthDp: Int, heightDp: Int, fontScale: Float, ui: HomeUi, label: String = "") {
    show(ui, widthDp, heightDp, fontScale)
    val viewportBottom = (heightDp - 163) * composeTestRule.density.density
    val pending = ui.stops.any { it.state != com.example.ui.screens.StopState.DONE }
    val tags = mutableListOf("home_greeting", "home_date", "home_week", "home_card", "home_route_title", "home_route_meta", "home_stop_1", "home_stop_2", "home_stop_3", "btn_all_games")
    if (pending || ui.pausedTitle != null) tags += if (ui.pausedTitle != null) "btn_resume_paused" else "btn_home_play"
    if (ui.challenge != null && ui.challenge.state != ChallengeState.SUPERADO) tags += "btn_challenge"
    if (ui.challenge?.state == ChallengeState.SUPERADO) tags += "home_challenge_done"
    for (tag in tags) {
      try { composeTestRule.onNodeWithTag(tag).assertIsDisplayed() } catch (e: AssertionError) { throw AssertionError("«$tag» no se ve sin deslizar a $widthDp × $heightDp con letra $fontScale ($label): ${bounds(tag)}", e) }
    }
    val last = bounds(if (ui.baselineInvite) "btn_home_baseline" else "btn_all_games")
    assertTrue("$label: lo último de Hoy termina en ${dp(last.bottom)} dp y la barra empieza en ${dp(viewportBottom)} dp: Hoy se desliza", last.bottom <= viewportBottom + 1f)
    println("HOY %d×%d letra %.1f %s: termina en %.0f dp de %.0f".format(widthDp, heightDp, fontScale, label, dp(last.bottom), dp(viewportBottom)))
  }

  /** Los cuatro estados del día más la pausa (Tarea 167): la primera hora, a mitad de ruta, la ruta completa con el Desafío por jugar, jugado sin superar y superado, y una partida en pausa. */
  private val dayStates: List<Pair<String, HomeUi>> by lazy {
    listOf(
      "primera hora" to ui(completed = 0, challenge = stroop),
      "a mitad de ruta" to ui(completed = 1, challenge = stroop),
      "ruta completa" to ui(completed = 3, challenge = stroop),
      "Desafío jugado" to ui(completed = 3, challenge = stroop.copy(state = ChallengeState.JUGADO)),
      "Desafío superado" to ui(completed = 3, challenge = stroop.copy(state = ChallengeState.SUPERADO)),
      "partida en pausa" to ui(completed = 1, paused = "Carga exacta", challenge = stroop),
      "ruta completa sin Desafío" to ui(completed = 3)
    )
  }

  @Test fun hoy_a_393_por_873_con_letra_1_0_todo_cabe_sin_deslizar_en_los_estados_del_dia() { for ((name, u) in dayStates) assertFitsWithoutScrolling(393, 873, 1f, u, name) }
  @Test fun hoy_a_393_por_873_con_letra_1_3_todo_cabe_sin_deslizar_en_los_estados_del_dia() { for ((name, u) in dayStates) assertFitsWithoutScrolling(393, 873, 1.3f, u, name) }
  @Test fun hoy_a_393_por_873_con_letra_1_0_captura() { show(ui(challenge = stroop)); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-393.png") }
  @Test fun hoy_a_393_por_873_con_letra_1_3_captura() { show(ui(challenge = stroop), fontScale = 1.3f); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-letra13.png") }
  @Test fun hoy_a_393_por_873_con_letra_1_3_y_con_la_invitacion_al_punto_de_partida_en_la_primera_hora() = assertFitsWithoutScrolling(393, 873, 1.3f, ui(completed = 0, challenge = stroop).copy(baselineInvite = true), "con la invitación")

  @Test
  fun hoy_a_360_por_780_con_letra_1_0_todo_cabe_sin_deslizar() {
    for ((name, u) in dayStates) assertFitsWithoutScrolling(360, 780, 1f, u, name)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-360.png")
  }

  @Test
  fun hoy_con_letra_2_0_se_desliza_pero_nada_se_corta_ni_se_superpone() {
    for ((name, u) in dayStates) { show(u.copy(hasBaseline = false, baselineInvite = true), fontScale = 2f); assertNothingCutOrOverlapping(393); assertNoWordSplit() }
    show(ui(hasBaseline = false, challenge = stroop), fontScale = 2f)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-letra20.png")
  }

  @Test fun a_360_dp_con_letra_1_3_nada_se_corta_ni_se_superpone() { for ((_, u) in dayStates) { show(u, widthDp = 360, heightDp = 780, fontScale = 1.3f); assertNothingCutOrOverlapping(360); assertNoWordSplit() } }

  /**
   * NUNCA una palabra partida a la mitad (regla de Ricardo): en cada texto de Hoy, el final de cada renglón cae en un espacio o al final del texto. Se mira el diseño de cada nodo de texto (`TextLayoutResult`): un renglón que termina dentro de una palabra es una palabra partida.
   * Solo cuenta lo de Hoy que dibuja Compose (el emblema de las filas es una imagen).
   */
  private fun assertNoWordSplit() {
    for (node in composeTestRule.onAllNodes(androidx.compose.ui.test.SemanticsMatcher.keyIsDefined(SemanticsActions.GetTextLayoutResult), useUnmergedTree = true).fetchSemanticsNodes()) {
      val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
      node.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
      val r = results.firstOrNull() ?: continue
      val text = r.layoutInput.text.text
      for (line in 0 until r.lineCount - 1) {
        val end = r.getLineEnd(line, visibleEnd = false)
        assertTrue("«$text» parte una palabra a la mitad en el renglón ${line + 1} (termina en «${text.substring(0, end).takeLast(6)}»)", end >= text.length || text[end - 1] == ' ' || text[end] == ' ' || text[end - 1] == '·' || text[end - 1] == '\n')
      }
    }
  }

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
    check(listOf("home_greeting", "home_date", "home_week", "home_card", "btn_challenge", "home_challenge_done", "home_route_tomorrow", "btn_all_games", "btn_home_baseline"), "afuera de la tarjeta")
    check(listOf("home_route_title", "home_route_meta", "home_nubi", "home_stop_1", "home_stop_2", "home_stop_3", "btn_home_play", "btn_resume_paused"), "adentro de la tarjeta")
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
   * UN SOLO SOL A LA VEZ (Tarea 167; antes, «el único sol»): el sol está en la acción del momento. [sunOf] decide quién lo tiene (la ruta mientras sigue o hay una pausa; el Desafío con la ruta completa y sin jugar; nadie con el Desafío jugado o superado) y NUNCA son dos. Robolectric no deja leer píxeles
   * aquí (`captureToImage` se cuelga; ver `OnBoardTextTest`), así que además se vigila el CÓDIGO: `HomeScreen.kt` no pide `Clay.Sun`, `SlabColor.SUN` aparece UNA sola vez (el botón ancho del Desafío) y los únicos `ClayButton` (el sol por omisión) son «Seguir tu partida» y la ruta, en ramas distintas.
   */
  @Test
  fun un_solo_sol_a_la_vez() {
    for ((name, u) in dayStates) {
      val sun = sunOf(u)
      val pending = u.stops.any { it.state != com.example.ui.screens.StopState.DONE }
      when {
        u.pausedTitle != null || pending -> assertEquals("$name: mientras la ruta sigue, el sol es de la ruta", Sun.ROUTE, sun)
        u.challenge?.state == ChallengeState.PENDIENTE -> assertEquals("$name: con la ruta completa, el sol pasa al Desafío", Sun.CHALLENGE, sun)
        else -> assertEquals("$name: con el Desafío jugado o superado (o sin Desafío) no queda ningún sol", Sun.NONE, sun)
      }
    }
    val src = java.io.File("src/main/java/com/example/ui/screens/HomeScreen.kt").readText()
    val code = src.lines().filterNot { it.trim().startsWith("*") || it.trim().startsWith("/**") || it.trim().startsWith("//") }.joinToString("\n")
    assertFalse("Hoy no pide el sol con Clay.Sun", code.contains("Clay.Sun"))
    assertEquals("el sol del Desafío sale de un solo lugar (el botón ancho, decidido por sunOf)", 1, Regex("""SlabColor\.SUN""").findAll(code).count())
    val buttons = Regex("""ClayButton\(([^\n]*)""").findAll(code).map { it.groupValues[1] }.toList()
    assertEquals("dos botones de la tarjeta («Seguir tu partida» y la ruta), en ramas distintas de un `when`", 2, buttons.size)
    for (b in buttons) assertFalse("el botón de la ruta lleva el color por omisión (el sol): $b", b.contains("color ="))
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
  fun con_la_ruta_completa_la_tarjeta_se_cierra_sola_sin_boton_ni_rotulo_y_sin_isla() {
    show(ui(completed = 3, challenge = stroop))
    for (n in 1..3) composeTestRule.onNodeWithTag("home_stop_$n").assertIsDisplayed()
    composeTestRule.onAllNodes(hasText("Hecho")).assertCountEquals(3)
    composeTestRule.onNodeWithText("3 de 3 · ruta completa").assertIsDisplayed()
    val meta = composeTestRule.onNodeWithTag("home_route_meta").fetchSemanticsNode()
    val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
    meta.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
    assertEquals("la meta va en negrita", androidx.compose.ui.text.font.FontWeight.Bold, results.first().layoutInput.style.fontWeight)
    assertTrue("y Nubi saluda en su esquina", exists("home_nubi"))
    assertFalse("sin botón de la ruta", exists("btn_home_play") || exists("btn_resume_paused"))
    assertFalse("sin el rótulo «Ruta completa» de la Tarea 165b", exists("home_route_done"))
    composeTestRule.onAllNodes(hasText("Ruta completa")).assertCountEquals(0)
    assertFalse("ya no hay isla en Hoy (vive en Tú)", exists("home_isla") || exists("isla"))
    for (tag in listOf("isla_observatorio", "isla_antena", "isla_biblioteca", "isla_hangar", "home_island_note")) assertFalse("«$tag» no existe", exists(tag))
    composeTestRule.onAllNodes(hasClickAction() and hasAnyAncestor(hasTestTag("home_card"))).assertCountEquals(0)          // la tarjeta cerrada no tiene ningún toque
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-completa.png")
  }

  @Test
  fun con_la_ruta_completa_y_letra_1_3_todo_cabe_sin_deslizar() {
    show(ui(completed = 3, challenge = stroop), fontScale = 1.3f)
    val viewportBottom = (873 - 163) * composeTestRule.density.density
    assertTrue("«Todos los juegos» termina en ${dp(bounds("btn_all_games").bottom)} dp de ${dp(viewportBottom)}", bounds("btn_all_games").bottom <= viewportBottom + 1f)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-completa-letra13.png")
  }

  // ---- El Desafío de hoy (Tarea 167)

  @Test
  fun mientras_la_ruta_sigue_el_Desafio_va_en_lila_junto_a_Todos_los_juegos_y_justo_debajo_de_la_tarjeta() {
    for (done in 0..2) {
      show(ui(completed = done, challenge = stroop))
      composeTestRule.onNodeWithText("Desafío de hoy").assertIsDisplayed()
      val card = bounds("home_card"); val c = bounds("btn_challenge"); val all = bounds("btn_all_games")
      assertTrue("va DEBAJO de la tarjeta y junto a «Todos los juegos» ($c / $all)", c.top >= card.bottom - 1f && all.top >= card.bottom - 1f && Math.abs(c.top - all.top) < 2f && c.right <= all.left + 1f)
      assertTrue("en media fila: cada uno ocupa la mitad (${dp(c.width)} y ${dp(all.width)} dp)", dp(c.width) >= 150f && dp(all.width) >= 150f)
      assertEquals("la ruta tiene el sol", Sun.ROUTE, sunOf(ui(completed = done, challenge = stroop)))
      assertTrue("y el botón de la ruta sigue dentro de la tarjeta", exists("btn_home_play"))
    }
  }

  @Test
  fun con_letra_1_0_Todos_los_juegos_y_Desafio_de_hoy_caben_en_UNA_linea_cada_uno() {
    show(ui(completed = 1, challenge = stroop))
    for (text in listOf("Desafío de hoy", "Todos los juegos")) {
      val node = composeTestRule.onNodeWithText(text, useUnmergedTree = true).fetchSemanticsNode()
      val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
      node.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
      assertEquals("«$text» cabe en una línea con letra 1,0", 1, results.first().lineCount)
    }
  }

  @Test
  fun tocar_el_Desafio_lo_lanza_y_no_toca_la_ruta() {
    show(ui(completed = 1, challenge = stroop))
    composeTestRule.onNodeWithTag("btn_challenge").performClick()
    assertEquals(1, challenged)
    assertEquals("no es la ruta", 0, played)
  }

  @Test
  fun el_lector_de_pantalla_dice_todo_del_Desafio_aunque_a_la_vista_solo_diga_Desafio_de_hoy() {
    show(ui(completed = 1, challenge = stroop))
    composeTestRule.onNodeWithContentDescription("Desafío de hoy, Tinta o Palabra, un paso más difícil, unos 2 minutos").assertExists()
    show(ui(completed = 3, challenge = stroop.copy(state = ChallengeState.JUGADO)))
    composeTestRule.onNodeWithContentDescription("Desafío de hoy, Tinta o Palabra, un paso más difícil, otra vez").assertExists()
  }

  @Test
  fun con_la_ruta_completa_el_Desafio_toma_todo_el_ancho_en_sol_con_el_juego_y_los_minutos() {
    show(ui(completed = 3, challenge = stroop))
    composeTestRule.onNodeWithText("Desafío de hoy").assertIsDisplayed()
    composeTestRule.onNodeWithText("Tinta o Palabra · unos 2 minutos").assertIsDisplayed()
    val c = bounds("btn_challenge"); val all = bounds("btn_all_games"); val card = bounds("home_card")
    assertTrue("a todo el ancho (${dp(c.width)} dp) y «Todos los juegos» baja ($all)", dp(c.width) >= 300f && all.top >= c.bottom - 1f && c.top >= card.bottom - 1f)
    assertEquals("el sol pasa al Desafío", Sun.CHALLENGE, sunOf(ui(completed = 3, challenge = stroop)))
    assertFalse("y la frase de mañana no está todavía", exists("home_route_tomorrow") || composeTestRule.onAllNodes(hasText("Mañana hay ruta y Desafío nuevos.")).fetchSemanticsNodes().isNotEmpty())
  }

  @Test
  fun con_letra_1_3_o_mas_la_segunda_linea_del_boton_ancho_dice_solo_el_juego() {
    show(ui(completed = 3, challenge = stroop), fontScale = 1.3f)
    composeTestRule.onNodeWithText("Tinta o Palabra").assertIsDisplayed()
    composeTestRule.onAllNodes(hasText("Tinta o Palabra · unos 2 minutos")).assertCountEquals(0)
  }

  @Test
  fun jugado_sin_superar_el_Desafio_queda_en_crema_con_otra_vez_y_ya_no_tiene_el_sol() {
    val u = ui(completed = 3, challenge = stroop.copy(state = ChallengeState.JUGADO))
    show(u)
    composeTestRule.onNodeWithText("Tinta o Palabra · otra vez").assertIsDisplayed()
    assertEquals(Sun.NONE, sunOf(u))
    composeTestRule.onNodeWithTag("btn_challenge").performClick()
    assertEquals("se puede repetir", 1, challenged)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-desafio-otra-vez.png")
  }

  @Test
  fun superado_no_hay_boton_ni_sol_solo_el_visto_y_las_dos_frases() {
    val u = ui(completed = 3, challenge = stroop.copy(state = ChallengeState.SUPERADO))
    show(u)
    composeTestRule.onNodeWithText("Desafío superado: Tinta o Palabra.").assertIsDisplayed()
    composeTestRule.onNodeWithText("Mañana hay ruta y Desafío nuevos.").assertIsDisplayed()
    assertFalse("ningún botón de Desafío", exists("btn_challenge"))
    assertEquals("ningún sol: el día está hecho", Sun.NONE, sunOf(u))
    composeTestRule.onNodeWithTag("btn_all_games").assertIsDisplayed()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-desafio-superado.png")
  }

  @Test
  fun superado_con_la_ruta_a_medias_dice_solo_el_visto_y_no_anuncia_el_mañana() {
    show(ui(completed = 1, challenge = stroop.copy(state = ChallengeState.SUPERADO)))
    composeTestRule.onNodeWithText("Desafío superado: Tinta o Palabra.").assertIsDisplayed()
    composeTestRule.onAllNodes(hasText("Mañana hay ruta y Desafío nuevos.")).assertCountEquals(0)
    assertEquals("la ruta sigue con el sol", Sun.ROUTE, sunOf(ui(completed = 1, challenge = stroop.copy(state = ChallengeState.SUPERADO))))
  }

  @Test
  fun con_una_partida_en_pausa_el_Desafio_sigue_en_lila_y_el_sol_es_de_seguir_la_partida() {
    val u = ui(completed = 1, paused = "Carga exacta", challenge = stroop)
    show(u)
    assertEquals(Sun.ROUTE, sunOf(u))
    composeTestRule.onNodeWithText("Desafío de hoy").assertIsDisplayed()
    composeTestRule.onNodeWithText("Seguir tu partida").assertIsDisplayed()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-pausa-con-desafio.png")
  }

  @Test
  fun sin_Desafio_Hoy_queda_como_antes_y_con_la_ruta_completa_dice_que_manana_hay_una_ruta_nueva() {
    show(ui(completed = 1))
    assertFalse("sin losa de Desafío", exists("btn_challenge") || exists("home_challenge_done"))
    composeTestRule.onNodeWithTag("btn_all_games").assertIsDisplayed()
    assertTrue("«Todos los juegos» a todo el ancho", dp(bounds("btn_all_games").width) >= 300f)
    show(ui(completed = 3))
    composeTestRule.onNodeWithText("Mañana te espera una ruta nueva.").assertIsDisplayed()
    assertEquals(Sun.NONE, sunOf(ui(completed = 3)))
  }

  @Test
  fun el_primer_vuelo_no_repite_la_frase_de_manana() {
    show(ui(completed = 3, flightDay = true))
    composeTestRule.onAllNodes(hasText("Mañana te espera una ruta nueva.")).assertCountEquals(0)
    composeTestRule.onNodeWithText("Tu primer vuelo fue tu ruta de hoy. Mañana empieza tu ruta.").assertExists()
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
