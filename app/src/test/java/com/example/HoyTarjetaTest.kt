package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.graphics.luminance
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.layout.positionInRoot
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
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.screens.HomeTimeTrial
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
 * Hoy con UNA sola tarjeta y el Contrarreloj de hoy (Tareas 165, 167 y 177; `auditoria-fable/hoy-reestructura.md`, `hoy-desafio-y-dia-completo.md` y `contrarreloj.md`, opción 1 «Dos botones, un solo sol a la vez»): saludo, la semana, UNA tarjeta crema con la ruta de hoy (título, «1 de 3 · unos 4 minutos», los tres juegos como filas con su estado en palabras que NO se tocan, y el botón sol ADENTRO), Nubi posada en
 * su esquina (de noche duerme) y, debajo, «Contrarreloj de hoy» (lila, con el reloj) junto a «Todos los juegos»; con la ruta completa la tarjeta se cierra sola (sin botón ni rótulo) y el sol pasa al Contrarreloj, a todo el ancho. **Un solo sol a la vez.** Ya NO hay isla en Hoy. Capturas a 393 × 873 dp (el teléfono de referencia), a 360 dp de ancho, con letra 1,3 y con letra 2,0,
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

  // el Contrarreloj de hoy: Tinta o Palabra, con un récord de 41 aciertos (la línea «Tu récord» sale solo con el botón ancho)
  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, todayHits = null, record = 41)
  private val stroopJugado = stroop.copy(state = TimeTrialState.JUGADO, todayHits = 38)
  private val stroopMarca = stroop.copy(state = TimeTrialState.MARCA_NUEVA, todayHits = 43, record = 43)

  private fun ui(
    completed: Int = 1, ids: List<String> = listOf("bodega", "calculo", "intrusa"), paused: String? = null, flightDay: Boolean = false, hasBaseline: Boolean = true, name: String = "Ricardo", at: Long = now, timeTrial: HomeTimeTrial? = null
  ) =
    HomeUi(
      name = name, now = at, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
      stops = homeStops(ids, completed, AppLanguage.SPANISH), pausedTitle = paused, flightDay = flightDay, hasBaseline = hasBaseline, timeTrial = timeTrial
    )

  private var played = 0
  private var resumed = 0
  private var trialed = 0

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
              ui = a.ui, onAllGames = { allGamesCallback() }, onPlay = { played++ }, onResume = { resumed++ }, onBaseline = {}, onTimeTrial = { trialed++ },
              modifier = Modifier.padding(top = 24.dp, bottom = 163.dp)
            )
          }
        }
      }
    }
    if (wait) composeTestRule.waitForIdle() else composeTestRule.mainClock.advanceTimeBy(100)
  }

  /** «Contrarreloj de hoy», con el espacio duro de la media fila o con el normal del botón ancho. */
  private val labelMatcher = androidx.compose.ui.test.SemanticsMatcher("el rótulo del Contrarreloj") { n ->
    n.config.getOrNull(SemanticsProperties.Text)?.any { it.text.replace('\u00A0', ' ') == "Contrarreloj de hoy" } == true
  }

  private fun bounds(tag: String): Rect = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot
  /** El borde de abajo de un nodo SIN recortar por la ventana de deslizar, en dp (`boundsInRoot` sí se recorta). */
  private fun unclippedBottomDp(tag: String): Float { val n = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode(); return dp(n.layoutInfo.coordinates.positionInRoot().y + n.layoutInfo.height) }
  private fun exists(tag: String) = composeTestRule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().isNotEmpty()
  /** Los límites SIN recortar por la ventana de deslizar (`boundsInRoot` da 0 de ancho a lo que quedó fuera de la vista) y, con [unmerged], de un nodo que un botón fusionó dentro de sí (el reloj y las casillas). */
  private fun unclipped(tag: String, unmerged: Boolean = false): Rect {
    val n = composeTestRule.onNodeWithTag(tag, useUnmergedTree = unmerged).fetchSemanticsNode()
    val p = n.layoutInfo.coordinates.positionInRoot()
    return Rect(p.x, p.y, p.x + n.layoutInfo.width, p.y + n.layoutInfo.height)
  }
  private fun dp(px: Float) = px / composeTestRule.density.density

  // ---- La regla que manda y las capturas

  /**
   * Cuánto se pasa Hoy de la barra a 393 × 873 (dp; Tarea 180: dos botones a todo el ancho y el título «Modos de juego» sumaron alto; la regla de la tarea es MEDIR e informar, no achicar). Con letra 1,0 solo se pasa «marca nueva con la ruta a medias» (+16); con 1,3, los estados con la ruta a medias (+2) y la marca nueva (+13); «Todos los juegos» mide 48 dp (el mínimo de toque).
   * Lo que no está aquí tiene que caber (la ruta completa y el Contrarreloj jugado, los de todos los días, caben con 1,0 y con 1,3).
   */
  private val scrollsAt393 = mapOf(1f to mapOf("marca nueva con la ruta a medias" to 16f), 1.3f to mapOf("primera hora" to 2f, "a mitad de ruta" to 2f, "marca nueva" to 13f, "marca nueva con la ruta a medias" to 85f, "partida en pausa" to 65f))

  /** Hoy se ve entero sin deslizar, salvo lo que dice [tolerance] (lo medido): lo último, «Todos los juegos» (o la invitación al punto de partida, si toca), termina antes de la barra (a 163 dp del borde de abajo). */
  private fun assertFitsWithoutScrolling(widthDp: Int, heightDp: Int, fontScale: Float, ui: HomeUi, label: String = "", tolerance: Float = 0f) {
    show(ui, widthDp, heightDp, fontScale)
    assertNoWordSplit()
    val viewportBottom = (heightDp - 163) * composeTestRule.density.density
    val pending = ui.stops.any { it.state != com.example.ui.screens.StopState.DONE }
    val tags = mutableListOf("home_greeting", "home_date", "home_week", "home_card", "home_route_title", "home_route_meta", "home_stop_1", "home_stop_2", "home_stop_3", "btn_all_games")
    if (pending || ui.pausedTitle != null) tags += if (ui.pausedTitle != null) "btn_resume_paused" else "btn_home_play"
    if (ui.timeTrial != null) tags += "btn_timetrial"
    if (ui.timeTrial?.state == TimeTrialState.MARCA_NUEVA) tags += "home_timetrial_done"
    if (ui.timeTrial?.record != null && ui.timeTrial.state != TimeTrialState.MARCA_NUEVA && pending.not() && ui.pausedTitle == null) tags += "home_timetrial_record"
    // `boundsInRoot` está RECORTADO por la ventana de deslizar: un botón a medio salir daría «termina justo en el borde». Se mide con la posición SIN recortar; la losa del kit lleva 6 dp de sombra abajo que no cuentan.
    val lastTag = if (ui.baselineInvite) "btn_home_baseline" else "btn_all_games"
    val last = unclippedBottomDp(lastTag) - (if (ui.baselineInvite) 0f else 6f)
    assertTrue("$label: lo último de Hoy termina en $last dp y la barra empieza en ${dp(viewportBottom)} dp: Hoy se desliza (se tolera %.0f dp)".format(tolerance), last <= dp(viewportBottom) + tolerance + 1f)
    for (tag in tags) {
      if (last <= dp(viewportBottom) + 1f) {
        try { composeTestRule.onNodeWithTag(tag).assertIsDisplayed() } catch (e: AssertionError) { val n = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode(); val top = dp(n.layoutInfo.coordinates.positionInRoot().y); throw AssertionError("«$tag» no se ve sin deslizar a $widthDp × $heightDp con letra $fontScale ($label): empieza en %.0f dp, mide %.0f dp y la barra empieza en %.0f dp".format(top, dp(n.layoutInfo.height.toFloat()), (heightDp - 163).toFloat()), e) }
      } else composeTestRule.onNodeWithTag(tag).assertExists()                                  // lo que se pasa se alcanza deslizando (se mide abajo)
    }
    val tt = if (ui.timeTrial != null && exists("btn_timetrial")) (if (Math.abs(bounds("btn_timetrial").top - bounds("btn_all_games").top) < 2f) "media fila" else "apilados") else "sin Contrarreloj"
    println("HOY %d×%d letra %.1f %s: termina en %.0f dp de %.0f (%s)".format(widthDp, heightDp, fontScale, label, last, dp(viewportBottom), tt))
  }

  /** Los estados del día más la pausa (Tareas 167 y 177): la primera hora, a mitad de ruta, la ruta completa con el Contrarreloj por jugar, jugado sin marca nueva y con marca nueva (con la ruta completa y a medias), y una partida en pausa. */
  private val dayStates: List<Pair<String, HomeUi>> by lazy {
    listOf(
      "primera hora" to ui(completed = 0, timeTrial = stroop),
      "a mitad de ruta" to ui(completed = 1, timeTrial = stroop),
      "ruta completa" to ui(completed = 3, timeTrial = stroop),
      "Contrarreloj jugado" to ui(completed = 3, timeTrial = stroopJugado),
      "marca nueva" to ui(completed = 3, timeTrial = stroopMarca),
      "marca nueva con la ruta a medias" to ui(completed = 1, timeTrial = stroopMarca),
      "partida en pausa" to ui(completed = 1, paused = "Carga exacta", timeTrial = stroop),
      "ruta completa sin Contrarreloj" to ui(completed = 3)
    )
  }

  @Test fun hoy_a_393_por_873_con_letra_1_0_todo_cabe_sin_deslizar_en_los_estados_del_dia() { for ((name, u) in dayStates) assertFitsWithoutScrolling(393, 873, 1f, u, name, scrollsAt393.getValue(1f)[name] ?: 0f) }
  @Test fun hoy_a_393_por_873_con_letra_1_3_todo_cabe_sin_deslizar_en_los_estados_del_dia() { for ((name, u) in dayStates) assertFitsWithoutScrolling(393, 873, 1.3f, u, name, scrollsAt393.getValue(1.3f)[name] ?: 0f) }
  /** Tarea 180: cuánto mide Hoy en cada estado del día, a cada tamaño y letra (dp; positivo = se pasa de la barra y se desliza). Solo MIDE (la regla de la tarea: informar los números, no achicar). */
  @Test
  fun tarea_180_medidas() {
    val rows = mutableListOf<String>()
    for ((w, h) in listOf(393 to 873, 360 to 780)) for (fs in listOf(1f, 1.3f, 2f)) {
      val cells = mutableListOf<String>()
      for ((name, u) in dayStates) {
        show(u, w, h, fs)
        val over = unclippedBottomDp("btn_all_games") - 6f - (h - 163)
        cells += "%s %+.0f".format(name, over)
      }
      rows += "MEDIDA %dx%d letra %.1f: %s".format(w, h, fs, cells.joinToString(" | "))
    }
    rows.forEach { println(it) }
  }

  /** Las capturas de la lámina de la Tarea 177 (solo al grabar): Hoy en sus estados del día, la pausa con letra 1,0 y 1,3, la fila apilada con letra 2,0 y el rincón de 360 dp. En `src/test/screenshots/tarea-177/`. */
  @Test
  fun tarea_177_capturas() {
    org.junit.Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    val out = "src/test/screenshots/tarea-177"
    fun shot(name: String, u: HomeUi, w: Int = 393, h: Int = 873, fs: Float = 1f) { show(u, w, h, fs); composeTestRule.onRoot().captureRoboImage(filePath = "$out/$name.png") }
    shot("hoy-a-media-ruta", ui(completed = 1, timeTrial = stroop))
    shot("hoy-ruta-completa", ui(completed = 3, timeTrial = stroop))
    shot("hoy-jugado", ui(completed = 3, timeTrial = stroopJugado))
    shot("hoy-marca-nueva", ui(completed = 3, timeTrial = stroopMarca))
    shot("hoy-pausa-1_0", ui(completed = 1, paused = "Carga exacta", timeTrial = stroop))
    shot("hoy-pausa-1_3", ui(completed = 1, paused = "Carga exacta", timeTrial = stroop), fs = 1.3f)
    shot("hoy-a-media-ruta-1_3", ui(completed = 1, timeTrial = stroop), fs = 1.3f)
    shot("hoy-a-media-ruta-2_0", ui(completed = 1, timeTrial = stroop), fs = 2f)
    shot("hoy-marca-con-ruta-a-medias-360", ui(completed = 1, timeTrial = stroopMarca), w = 360, h = 780)
    show(ui(completed = 1, timeTrial = stroop), 393, 873, 2f)                                   // lo último: deslizar deja la página corrida
    composeTestRule.onNodeWithTag("btn_all_games").performScrollTo(); composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "$out/hoy-a-media-ruta-2_0-abajo.png")
  }

  @Test fun hoy_a_393_por_873_con_letra_1_0_captura() { show(ui(timeTrial = stroop)); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-393.png") }
  @Test fun hoy_a_393_por_873_con_letra_1_3_captura() { show(ui(timeTrial = stroop), fontScale = 1.3f); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-letra13.png") }
  @Test fun hoy_a_393_por_873_con_letra_1_3_y_con_la_invitacion_al_punto_de_partida_en_la_primera_hora() = assertFitsWithoutScrolling(393, 873, 1.3f, ui(completed = 0, timeTrial = stroop).copy(baselineInvite = true), "con la invitación", tolerance = 63f)

  /**
   * A 360 × 780 (el teléfono más chico que se mira) Hoy NO cabe entero en dos de los ocho estados del día (la partida en pausa y la marca nueva con la ruta a medias): se pasa de la barra y se desliza. Cuánto se mide (dp; Tarea 177, con la posición SIN recortar: `boundsInRoot` recorta y antes «termina justo en el borde» no probaba nada) y cuánto se tolera. La ruta completa y el Contrarreloj jugado (los de todos los días) caben desde que se apretó el ritmo vertical de Hoy 16 dp.
   * Lo que no está aquí tiene que caber. A 393 × 873 todos caben con letra 1,0 y 1,3.
   */
  private val scrollsAt360 = mapOf("primera hora" to 74f, "a mitad de ruta" to 74f, "ruta completa" to 22f, "Contrarreloj jugado" to 22f, "marca nueva" to 75f, "marca nueva con la ruta a medias" to 147f, "partida en pausa" to 125f)

  @Test
  fun hoy_a_360_por_780_cuanto_se_pasa_de_la_barra_en_cada_estado() {
    val pasan = mutableListOf<String>()
    for ((name, u) in dayStates) {
      show(u, widthDp = 360, heightDp = 780, fontScale = 1f)
      assertNoWordSplit(); assertNothingCutOrOverlapping(360)
      val over = unclippedBottomDp("btn_all_games") - 6f - (780 - 163)
      println("HOY360 %s: se pasa %.0f dp".format(name, over))
      val limit = scrollsAt360[name] ?: 0f
      if (over > limit + 1f) pasan += "$name: se pasa %.0f dp (se tolera %.0f)".format(over, limit)
    }
    // lo que se pasa, se alcanza deslizando (después de medir todo: deslizar deja la página corrida y movería lo que se mide del estado que sigue)
    for ((name, u) in dayStates) { show(u, widthDp = 360, heightDp = 780, fontScale = 1f); composeTestRule.onNodeWithTag("btn_all_games").performScrollTo().assertIsDisplayed() }
    assertTrue("A 360 × 780 se pasan de la barra: $pasan", pasan.isEmpty())
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-360.png")
  }

  @Test
  fun hoy_con_letra_2_0_se_desliza_pero_nada_se_corta_ni_se_superpone() {
    for ((name, u) in dayStates) { show(u.copy(hasBaseline = false, baselineInvite = true), fontScale = 2f); assertNothingCutOrOverlapping(393); assertNoWordSplit() }
    show(ui(hasBaseline = false, timeTrial = stroop), fontScale = 2f)
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
    check(listOf("home_greeting", "home_date", "home_week", "home_card", "btn_timetrial", "home_timetrial_done", "home_timetrial_record", "home_route_tomorrow", "btn_all_games", "btn_home_baseline"), "afuera de la tarjeta")
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
   * UN SOLO SOL A LA VEZ (Tarea 167; antes, «el único sol»): el sol está en la acción del momento. [sunOf] decide quién lo tiene (la ruta mientras sigue o hay una pausa; el Contrarreloj con la ruta completa y sin jugar hoy; nadie con el Contrarreloj jugado o con marca nueva) y NUNCA son dos. Robolectric no deja leer píxeles
   * aquí (`captureToImage` se cuelga; ver `OnBoardTextTest`), así que además se vigila el CÓDIGO: `HomeScreen.kt` no pide `Clay.Sun`, `SlabColor.SUN` aparece UNA sola vez (el botón ancho del Contrarreloj) y los únicos `ClayButton` (el sol por omisión) son «Seguir tu partida» y la ruta, en ramas distintas.
   */
  @Test
  fun un_solo_sol_a_la_vez() {
    for ((name, u) in dayStates) {
      val sun = sunOf(u)
      val pending = u.stops.any { it.state != com.example.ui.screens.StopState.DONE }
      when {
        u.pausedTitle != null || pending -> assertEquals("$name: mientras la ruta sigue, el sol es de la ruta", Sun.ROUTE, sun)
        u.timeTrial?.state == TimeTrialState.PENDIENTE -> assertEquals("$name: con la ruta completa, el sol pasa al Contrarreloj", Sun.TIME_TRIAL, sun)
        else -> assertEquals("$name: con el Contrarreloj jugado o con marca nueva (o sin Contrarreloj) no queda ningún sol", Sun.NONE, sun)
      }
    }
    val src = java.io.File("src/main/java/com/example/ui/screens/HomeScreen.kt").readText()
    val code = src.lines().filterNot { it.trim().startsWith("*") || it.trim().startsWith("/**") || it.trim().startsWith("//") }.joinToString("\n")
    assertFalse("Hoy no pide el sol con Clay.Sun", code.contains("Clay.Sun"))
    assertEquals("el sol del Contrarreloj sale de un solo lugar (el botón ancho, decidido por sunOf)", 1, Regex("""SlabColor\.SUN""").findAll(code).count())
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
    show(ui(completed = 3, timeTrial = stroop))
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
    show(ui(completed = 3, timeTrial = stroop), fontScale = 1.3f)
    val viewportBottom = (873 - 163) * composeTestRule.density.density
    assertTrue("«Todos los juegos» termina en ${dp(bounds("btn_all_games").bottom)} dp de ${dp(viewportBottom)}", bounds("btn_all_games").bottom <= viewportBottom + 1f)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-completa-letra13.png")
  }

  // ---- El Contrarreloj de hoy (Tarea 177; antes, el Desafío de hoy de las Tareas 167 y 169)

  @Test
  fun mientras_la_ruta_sigue_el_Contrarreloj_va_en_lila_a_todo_el_ancho_bajo_el_titulo_Modos_de_juego_y_sobre_Todos_los_juegos() {
    for (done in 0..2) {
      show(ui(completed = done, timeTrial = stroop))
      composeTestRule.onNodeWithText("Contrarreloj").assertExists()
      val card = bounds("home_card"); val title = bounds("home_modes_title"); val c = bounds("btn_timetrial"); val all = bounds("btn_all_games")
      assertTrue("el título «Modos de juego» va DEBAJO de la tarjeta ($title / $card)", title.top >= card.bottom - 1f)
      assertTrue("el Contrarreloj va bajo el título, a todo el ancho (${dp(c.width)} dp) y «Todos los juegos» bajo él ($c / $all)", c.top >= title.bottom - 1f && dp(c.width) >= 300f && all.top >= c.bottom - 1f)
      assertEquals("los dos botones miden lo mismo de ancho", c.width, all.width, 1f)
      assertEquals("la ruta tiene el sol", Sun.ROUTE, sunOf(ui(completed = done, timeTrial = stroop)))
      assertTrue("y el botón de la ruta sigue dentro de la tarjeta", exists("btn_home_play"))
      assertFalse("mientras la ruta sigue no hay línea de récord (va con la ruta completa)", exists("home_timetrial_record"))
    }
  }

  @Test
  fun el_titulo_Modos_de_juego_va_en_Rubik_y_los_dos_botones_llevan_su_ícono_de_plasticina() {
    show(ui(completed = 1, timeTrial = stroop))
    composeTestRule.onNodeWithTag("home_modes_title").assertExists()
    composeTestRule.onNodeWithText("Modos de juego").assertExists()
    val title = composeTestRule.onNodeWithTag("home_modes_title").fetchSemanticsNode()
    val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
    title.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
    assertEquals("Rubik Bold, como los demás títulos", com.example.ui.theme.TitleFamily, results.first().layoutInput.style.fontFamily)
    val clock = unclipped("icon_reloj_clay", unmerged = true); val btn = unclipped("btn_timetrial")
    assertTrue("el reloj de plasticina mide entre 40 y 44 dp (${dp(clock.height)} dp) y va DENTRO del botón del Contrarreloj", dp(clock.height) in 40f..44f && clock.left >= btn.left && clock.right <= btn.right && clock.top >= btn.top && clock.bottom <= btn.bottom)
    val areas = unclipped("icon_areas", unmerged = true); val all = unclipped("btn_all_games")
    assertEquals("las cuatro casillas miden 28 dp", 28f, dp(areas.height), 0.6f)
    assertTrue("y van dentro de «Todos los juegos»", areas.left >= all.left && areas.right <= all.right && areas.top >= all.top && areas.bottom <= all.bottom)
    assertEquals("las casillas son de los colores de las cuatro áreas, en el orden de la app", listOf(com.example.ui.theme.NubiTokens.Memoria, com.example.ui.theme.NubiTokens.Atencion, com.example.ui.theme.NubiTokens.Razonamiento, com.example.ui.theme.NubiTokens.Lenguaje), com.example.ui.components.AreaSquareColors)
  }

  @Test
  fun el_Contrarreloj_y_Todos_los_juegos_van_a_todo_el_ancho_uno_bajo_el_otro_en_todos_los_estados_y_con_letra_1_0_1_3_y_2_0() {
    for (fs in listOf(1f, 1.3f, 2f)) for ((name, u) in dayStates) {
      if (u.timeTrial == null) continue
      show(u, fontScale = fs)
      val c = unclipped("btn_timetrial"); val all = unclipped("btn_all_games"); val card = unclipped("home_card")
      assertTrue("$name, letra $fs: el Contrarreloj va a todo el ancho (${dp(c.width)} dp) y «Todos los juegos» debajo, también a todo el ancho (${dp(all.width)} dp)", dp(c.width) >= 300f && dp(all.width) >= 300f && all.top >= c.bottom - 1f && c.top >= card.bottom - 1f)
      assertNoWordSplit()
    }
  }

  @Test
  fun la_palabra_Contrarreloj_cabe_en_UNA_linea_con_letra_1_0_1_3_y_2_0_y_el_juego_de_hoy_no_parte_palabras() {
    for (fs in listOf(1f, 1.3f, 2f)) {
      show(ui(completed = 3, timeTrial = stroop), fontScale = fs)
      val node = composeTestRule.onNode(hasText("Contrarreloj"), useUnmergedTree = true).fetchSemanticsNode()
      val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
      node.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
      assertEquals("«Contrarreloj» en una línea con letra $fs", 1, results.first().lineCount)
      composeTestRule.onNodeWithText("Hoy: Tinta o Palabra", useUnmergedTree = true).assertExists()
      assertNoWordSplit()
    }
  }

  @Test
  fun con_letra_1_0_Todos_los_juegos_cabe_en_UNA_linea_y_Contrarreloj_y_su_segunda_linea_tambien() {
    show(ui(completed = 1, timeTrial = stroop))
    fun lines(text: String): Int {
      val node = composeTestRule.onNode(hasText(text), useUnmergedTree = true).fetchSemanticsNode()
      val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
      node.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
      return results.first().lineCount
    }
    assertEquals("«Todos los juegos» cabe en una línea con letra 1,0", 1, lines("Todos los juegos"))
    assertEquals("«Contrarreloj» cabe en una línea con letra 1,0", 1, lines("Contrarreloj"))
    assertEquals("«Hoy: Tinta o Palabra» cabe en una línea con letra 1,0", 1, lines("Hoy: Tinta o Palabra"))
    assertNoWordSplit()
  }

  @Test
  fun tocar_el_Contrarreloj_lo_lanza_y_no_toca_la_ruta() {
    show(ui(completed = 1, timeTrial = stroop))
    composeTestRule.onNodeWithTag("btn_timetrial").performClick()
    assertEquals(1, trialed)
    assertEquals("no es la ruta", 0, played)
  }

  @Test
  fun el_lector_de_pantalla_dice_todo_del_Contrarreloj_aunque_a_la_vista_solo_diga_Contrarreloj_y_Hoy_el_juego() {
    show(ui(completed = 3, timeTrial = stroop))
    composeTestRule.onNodeWithContentDescription("Contrarreloj de hoy, Tinta o Palabra, 2 minutos, tu récord 41 aciertos").assertExists()
    show(ui(completed = 3, timeTrial = stroopJugado))
    composeTestRule.onNodeWithContentDescription("Contrarreloj de hoy, Tinta o Palabra, 2 minutos, hoy 38 aciertos, otra vez, tu récord 41 aciertos").assertExists()
  }

  @Test
  fun con_la_ruta_completa_el_Contrarreloj_va_en_sol_con_Hoy_el_juego_y_el_record_debajo() {
    show(ui(completed = 3, timeTrial = stroop))
    composeTestRule.onNodeWithText("Contrarreloj").assertIsDisplayed()
    composeTestRule.onNodeWithText("Hoy: Tinta o Palabra").assertIsDisplayed()
    composeTestRule.onAllNodes(hasText("2 minutos", substring = true)).assertCountEquals(0)
    composeTestRule.onNodeWithText("Tu récord: 41 aciertos.").assertIsDisplayed()
    val c = bounds("btn_timetrial"); val all = bounds("btn_all_games"); val card = bounds("home_card"); val rec = bounds("home_timetrial_record")
    assertTrue("a todo el ancho (${dp(c.width)} dp) y «Todos los juegos» baja ($all)", dp(c.width) >= 300f && all.top >= c.bottom - 1f && c.top >= card.bottom - 1f)
    assertTrue("el récord va en una línea PLANA entre el botón y «Todos los juegos» ($rec)", rec.top >= c.bottom - 1f && rec.bottom <= all.top + 1f)
    composeTestRule.onNodeWithTag("home_timetrial_record").assertHasNoClickAction()
    assertEquals("el sol pasa al Contrarreloj", Sun.TIME_TRIAL, sunOf(ui(completed = 3, timeTrial = stroop)))
    assertFalse("y la frase de mañana no está todavía", exists("home_route_tomorrow"))
  }

  @Test
  fun la_primera_vez_no_hay_record_y_la_linea_no_se_dibuja() {
    show(ui(completed = 3, timeTrial = stroop.copy(record = null)))
    assertFalse(exists("home_timetrial_record"))
    composeTestRule.onAllNodes(hasText("Tu récord", substring = true)).assertCountEquals(0)
    composeTestRule.onNodeWithContentDescription("Contrarreloj de hoy, Tinta o Palabra, 2 minutos").assertExists()
  }

  @Test
  fun la_segunda_linea_del_boton_dice_Hoy_y_el_juego_con_cualquier_letra() {
    for (fs in listOf(1f, 1.3f, 2f)) {
      show(ui(completed = 3, timeTrial = stroop), fontScale = fs)
      composeTestRule.onNodeWithText("Hoy: Tinta o Palabra", useUnmergedTree = true).assertExists()
      composeTestRule.onAllNodes(hasText("Tinta o Palabra · 2 minutos")).assertCountEquals(0)
    }
  }

  @Test
  fun jugado_sin_marca_nueva_el_Contrarreloj_queda_en_crema_con_Hoy_el_juego_y_ya_no_tiene_el_sol() {
    val u = ui(completed = 3, timeTrial = stroopJugado)
    show(u)
    composeTestRule.onNodeWithText("Hoy: Tinta o Palabra").assertIsDisplayed()
    composeTestRule.onNodeWithText("Tu récord: 41 aciertos.").assertIsDisplayed()
    assertEquals(Sun.NONE, sunOf(u))
    composeTestRule.onNodeWithTag("btn_timetrial").performClick()
    assertEquals("se puede repetir las veces que se quiera", 1, trialed)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-contrarreloj-jugado.png")
  }

  @Test
  fun jugado_con_la_ruta_a_medias_el_Contrarreloj_va_en_crema_y_la_ruta_sigue_con_el_sol() {
    show(ui(completed = 1, timeTrial = stroopJugado))
    composeTestRule.onNodeWithText("Contrarreloj").assertExists()
    composeTestRule.onNodeWithText("Hoy: Tinta o Palabra").assertExists()
    assertEquals("la ruta sigue con el sol", Sun.ROUTE, sunOf(ui(completed = 1, timeTrial = stroopJugado)))
  }

  @Test
  fun con_marca_nueva_no_hay_sol_solo_el_recuadro_con_el_visto_y_Otra_vez_en_crema_a_todo_el_ancho_sobre_Todos_los_juegos() {
    val u = ui(completed = 3, timeTrial = stroopMarca)
    show(u)
    composeTestRule.onNodeWithText("Marca nueva: 43 aciertos en Tinta o Palabra.").assertIsDisplayed()
    composeTestRule.onNodeWithTag("home_timetrial_done").assertIsDisplayed().assertHasNoClickAction()
    composeTestRule.onNodeWithText("Otra vez").assertIsDisplayed()
    assertEquals("ningún sol: el día está hecho", Sun.NONE, sunOf(u))
    val note = bounds("home_timetrial_done"); val again = bounds("btn_timetrial"); val all = bounds("btn_all_games")
    assertTrue("«Otra vez» va a todo el ancho (${dp(again.width)} dp) bajo el recuadro y «Todos los juegos» debajo ($again / $all)", dp(again.width) >= 300f && again.top >= note.bottom - 1f && all.top >= again.bottom - 1f)
    assertFalse("sin la línea del récord (la marca nueva ya lo dice)", exists("home_timetrial_record"))
    composeTestRule.onNodeWithTag("btn_timetrial").performClick()
    assertEquals("«Otra vez» lanza el Contrarreloj de nuevo", 1, trialed)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-contrarreloj-marca-nueva.png")
  }

  @Test
  fun con_marca_nueva_y_la_ruta_a_medias_el_recuadro_va_a_todo_el_ancho_y_la_ruta_sigue_con_el_sol() {
    val u = ui(completed = 1, timeTrial = stroopMarca)
    show(u)
    composeTestRule.onNodeWithText("Marca nueva: 43 aciertos en Tinta o Palabra.").assertIsDisplayed()
    assertEquals("la ruta sigue con el sol", Sun.ROUTE, sunOf(u))
    assertEquals("a todo el ancho, sobre la fila de dos botones", bounds("home_card").width, bounds("home_timetrial_done").width, 40f)
    composeTestRule.onNodeWithTag("home_timetrial_done").assertHasNoClickAction()
  }

  @Test
  fun la_marca_nueva_cabe_en_UNA_linea_con_letra_1_0_y_1_3_y_nunca_parte_una_palabra() {
    for (scale in listOf(1f, 1.3f)) {
      show(ui(completed = 3, timeTrial = stroopMarca), fontScale = scale)
      val node = composeTestRule.onNodeWithText("Marca nueva: 43 aciertos en Tinta o Palabra.", useUnmergedTree = true).fetchSemanticsNode()
      val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
      node.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
      println("MARCA NUEVA letra $scale: ${results.first().lineCount} renglón(es)")
      assertNoWordSplit()
    }
  }

  /** El recuadro ocupa el lugar y el ancho del botón del Contrarreloj con la ruta completa: justo bajo la tarjeta, de lado a lado, y no es tocable. */
  @Test
  fun el_recuadro_de_la_marca_nueva_esta_donde_estaba_el_boton_y_al_mismo_ancho() {
    show(ui(completed = 3, timeTrial = stroop))
    val boton = bounds("btn_timetrial")
    show(ui(completed = 3, timeTrial = stroopMarca))
    val recuadro = bounds("home_timetrial_done")
    assertEquals("a la misma altura que el botón", boton.top, recuadro.top, 1f)
    assertEquals("desde el mismo borde izquierdo", boton.left, recuadro.left, 1f)
    assertEquals("al mismo ancho", boton.width, recuadro.width, 1f)
    composeTestRule.onNodeWithTag("home_timetrial_done").assertHasNoClickAction()
  }

  private fun contrast(a: androidx.compose.ui.graphics.Color, b: androidx.compose.ui.graphics.Color): Double {
    val la = a.luminance().toDouble(); val lb = b.luminance().toDouble()
    return (maxOf(la, lb) + 0.05) / (minOf(la, lb) + 0.05)
  }

  /** El lila claro del recuadro: la tinta y la tinta suave leen ≥ 4,5:1 sobre él, y no es ni el crema de «Todos los juegos» ni el lima de «Hecho». */
  @Test
  fun el_lila_claro_del_recuadro_se_lee_con_la_tinta_y_no_se_confunde_con_el_crema_ni_el_lima() {
    val fill = com.example.ui.screens.TimeTrialDoneFill
    val ink = contrast(com.example.ui.theme.Clay.Ink, fill); val muted = contrast(com.example.ui.theme.Clay.InkMuted, fill)
    println("RECUADRO tinta %.2f:1, tinta suave %.2f:1".format(ink, muted))
    assertTrue("la tinta sobre el lila claro mide $ink:1 (mínimo 4,5)", ink >= 4.5)
    assertTrue("la tinta suave sobre el lila claro mide $muted:1 (mínimo 4,5)", muted >= 4.5)
    assertTrue("es más claro que el lila del botón", fill.luminance() > com.example.ui.theme.Clay.Grape.luminance())
    assertTrue("y se distingue del crema de «Todos los juegos» (el azul domina al rojo)", fill.blue > fill.red + 0.1f)
    assertTrue("y del lima de «Hecho» (el verde no domina)", fill.green < fill.blue && fill.green <= fill.red)
  }

  /** El recuadro es PLANO: su código no pide borde, sombra, losa, botón, reloj ni sol, y su radio es el del kit (22 dp). */
  @Test
  fun el_recuadro_es_plano_en_el_codigo_sin_borde_ni_sombra_ni_boton_ni_reloj() {
    val src = java.io.File("src/main/java/com/example/ui/screens/HomeScreen.kt").readText().replace("\r\n", "\n")
    val i = src.indexOf("private fun TimeTrialDoneNote(")
    assertTrue("no encontré TimeTrialDoneNote", i > 0)
    val body = src.substring(i, src.indexOf("\n}\n", i))
    for (banned in listOf("border(", "shadow", "clickable", "KitButton", "ClayButton", "ClaySlab", "ClayCard", "Glyph.", "SlabColor", "Clay.Sun", "toggleable", "selectable")) {
      assertFalse("el recuadro de la marca nueva no usa `$banned`", body.contains(banned))
    }
    assertTrue("lleva el fondo lila claro", body.contains("background(TimeTrialDoneFill)"))
    assertTrue("con las esquinas del kit", body.contains("RoundedCornerShape(TimeTrialDoneRadius)"))
    assertEquals("22 dp, el radio de los botones y las tarjetas del kit", 22.dp, com.example.ui.screens.TimeTrialDoneRadius)
    assertTrue("lleva el visto de «Hecho»", body.contains("StopMark(StopState.DONE)"))
  }

  @Test
  fun el_boton_lleva_el_reloj_de_plasticina_y_no_el_cometa_ni_el_glifo_de_churro() {
    val src = java.io.File("src/main/java/com/example/ui/screens/HomeScreen.kt").readText().replace("\r\n", "\n")
    assertTrue("el reloj de plasticina del kit", src.contains("ClayClockIcon()"))
    assertFalse("ya no el reloj de churro de 24 dp", src.contains("Glyph.CLOCK"))
    assertFalse("el cometa es del modo Desafío de la ficha, no del Contrarreloj", src.contains("Glyph.COMET"))
    assertFalse("ni queda nada del Desafío de hoy", src.contains("ChallengeState") || src.contains("DailyChallenge") || src.contains("HomeChallenge"))
    assertTrue("«Todos los juegos» lleva las cuatro casillas de las áreas", src.contains("AreaSquares("))
  }

  @Test
  fun con_una_partida_en_pausa_el_Contrarreloj_sigue_en_lila_y_el_sol_es_de_seguir_la_partida() {
    val u = ui(completed = 1, paused = "Carga exacta", timeTrial = stroop)
    show(u)
    assertEquals(Sun.ROUTE, sunOf(u))
    composeTestRule.onNodeWithText("Contrarreloj").assertExists()
    composeTestRule.onNodeWithText("Seguir tu partida").assertIsDisplayed()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-pausa-con-contrarreloj.png")
  }

  @Test
  fun sin_Contrarreloj_Hoy_queda_como_antes_y_con_la_ruta_completa_dice_que_manana_hay_una_ruta_nueva() {
    show(ui(completed = 1))
    assertFalse("sin losa de Contrarreloj", exists("btn_timetrial") || exists("home_timetrial_done"))
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
