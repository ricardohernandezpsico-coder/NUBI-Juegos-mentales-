package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.graphics.luminance
import androidx.compose.ui.graphics.toArgb
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
import com.example.data.NBSP
import com.example.data.PlusAccess
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.NubiIdle
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeUi
import com.example.ui.screens.Sun
import com.example.ui.screens.homeStops
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
import kotlin.random.Random

/**
 * Hoy con UNA sola tarjeta y el Contrarreloj de hoy (Tareas 165, 167, 177 y 219; `auditoria-fable/hoy-reestructura.md`, `contrarreloj.md` e `idea-pestana-contrarreloj.md`): saludo, la semana, UNA tarjeta crema con la ruta de hoy (título, «1 de 3 · unos 4 minutos», los tres juegos como filas con su estado en palabras que NO se tocan, y el botón sol ADENTRO), Nubi posada en su esquina
 * (siempre despierta, también de noche: Tarea 184) y, debajo (Tarea 219), «Todos los juegos» (crema, 56 dp) y el Contrarreloj de hoy en una losa CELESTE de 80 dp (crema «Otra vez» ya jugado), con su línea de estado, el recuadro celeste claro de la marca nueva o de «sin partidas» y la fila gris; con la ruta completa la tarjeta se cierra sola y el sol NO pasa al Contrarreloj: **el sol vive solo en la ruta.**
 * Ya NO hay isla ni título «Modos de juego». Capturas a 393 × 873 dp (el teléfono de referencia), a 360 dp de ancho, con letra 1,3 y con letra 2,0, sobre el fondo en dos capas y con las barras del sistema simuladas por margen (24 dp arriba; abajo 163: la barra de Android y la de pestañas con su aire). La regla que manda: la acción principal (la ruta o, con la ruta completa, lo primero
 * del bloque del Contrarreloj) se ve entera sin deslizar y lo demás se alcanza deslizando; en ningún tamaño ni letra se corta, se superpone ni parte una palabra. Las medidas (dp que lo último se pasa de la barra) salen de `home_end` y viven en `scrollsAt393` y `scrollsAt360`. Ningún texto que va directo sobre el fondo cae sobre un lucero.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class HoyTarjetaTest {
  @get:Rule val composeTestRule = createComposeRule()

  // Un domingo a las 16:30 (para el saludo «Buenas tardes» y la fecha); la semana: lunes, martes y hoy.
  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 11, 16, 30, 0) }.timeInMillis
  private val night = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 11, 23, 0, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)

  // el Contrarreloj de hoy: Tinta o Palabra, con un récord de 41 aciertos (la línea «Tu récord» sale solo con el botón ancho)
  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, todayHits = null, record = 41)
  private val stroopJugado = stroop.copy(state = TimeTrialState.JUGADO, todayHits = 38)
  private val stroopMarca = stroop.copy(state = TimeTrialState.MARCA_NUEVA, todayHits = 43, record = 43)

  // sin Nubilus Plus (Tareas 218 y 219): quedan N partidas de hoy; sin ninguna, «Hoy ya jugaste tus 3 partidas. Mañana: …»
  private val sinPlus = stroop.copy(gamesLeft = 3)
  private val sinPlusJugado = stroopJugado.copy(gamesLeft = 2)
  private val sinPlusMarca = stroopMarca.copy(gamesLeft = 2)
  private val sinPartidas = stroopJugado.copy(gamesLeft = 0, tomorrowTitle = "¿Verdad o disparate?")
  private val sinPartidasMarca = stroopMarca.copy(gamesLeft = 0, tomorrowTitle = "¿Verdad o disparate?")

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
  private var locked = 0

  private class Shown(val ui: HomeUi, val widthDp: Int, val heightDp: Int, val fontScale: Float, val reduce: Boolean)
  private var shown: androidx.compose.runtime.MutableState<Shown>? = null
  private var lockedCallback: () -> Unit = {}

  /** Muestra Hoy: la primera vez arma el contenido (un solo `setContent` por prueba); las siguientes cambian lo mostrado (el estado del día, el tamaño o la letra) en el mismo contenido. */
  private fun show(ui: HomeUi, widthDp: Int = 393, heightDp: Int = 873, fontScale: Float = 1f, wait: Boolean = true, reduce: Boolean = false, onLocked: () -> Unit = {}) {
    lockedCallback = onLocked
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
              ui = a.ui, onPlay = { played++ }, onResume = { resumed++ }, onBaseline = {}, onTimeTrial = { trialed++ }, onLocked = { lockedCallback() },
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
  private fun unclippedBottomDp(tag: String): Float { val n = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode(); return dp(n.positionInRoot.y + n.size.height) }
  private fun exists(tag: String) = composeTestRule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().isNotEmpty()
  /** Los límites SIN recortar por la ventana de deslizar (`boundsInRoot` da 0 de ancho a lo que quedó fuera de la vista) y, con [unmerged], de un nodo que un botón fusionó dentro de sí (el reloj y las casillas). */
  private fun unclipped(tag: String, unmerged: Boolean = false): Rect {
    val n = composeTestRule.onNodeWithTag(tag, useUnmergedTree = unmerged).fetchSemanticsNode()
    val p = n.positionInRoot                                   // posición y tamaño en el lugar de la cadena de modificadores donde está la etiqueta (`layoutInfo` mezclaba el relleno de antes y de después)
    return Rect(p.x, p.y, p.x + n.size.width, p.y + n.size.height)
  }
  private fun dp(px: Float) = px / composeTestRule.density.density

  // ---- La regla que manda y las capturas

  /** Lo máximo que se pasa de la barra el día del vuelo con 4 filas y letra 1,3 (dp; Tarea 219: Hoy se midió de nuevo con el Contrarreloj bajo «Todos los juegos»; antes, medido el 30-oct: Tarea 193, 90,5; Tarea 194, 78,7). */
  private val FLIGHT_DAY_13_MAX_OVER = 999f

  /** Los dp de sombra dura que cuenta el tamaño de una losa según el lugar de la cadena de modificadores donde está su etiqueta (la losa del Contrarreloj, primer hijo de su bloque, los trae dentro; las demás no): se restan para medir el CUERPO. */
  private fun shadowInSize(tag: String) = if (tag == "btn_timetrial") 6f else 0f

  /**
   * El borde de abajo de lo ÚLTIMO de Hoy (dp, SIN recortar por la ventana de deslizar y sin la sombra de la losa): la marca `home_end`, sin tamaño, está justo donde termina el último elemento, y a eso se le quitan los 6 dp que una losa reserva para su sombra. Desde la Tarea 219 lo último es
   * el Contrarreloj (la losa, su línea de estado, los recuadros o la fila gris), ya no «Todos los juegos»; con la invitación al punto de partida, es la invitación.
   */
  private fun lastBottomDp(): Float {
    val end = composeTestRule.onNodeWithTag("home_end").fetchSemanticsNode()
    val shadow = when {
      exists("btn_home_baseline") -> 0f                                   // el enlace
      exists("home_locked_games") -> 6f                                   // la fila gris es una losa
      exists("home_timetrial_line") || exists("home_timetrial_none") -> 0f   // texto o recuadro plano
      else -> 6f                                                          // una losa: «Otra vez» o el Contrarreloj
    }
    return dp(end.positionInRoot.y) - shadow
  }

  /** La acción principal de Hoy, la que SIEMPRE tiene que verse entera sin deslizar: la ruta (o «Seguir tu partida») mientras sigue; con la ruta completa, lo primero del bloque del Contrarreloj (su losa o, con marca nueva o sin partidas, el recuadro). */
  private fun mainActionTag(ui: HomeUi): String {
    val pending = ui.stops.any { it.state != com.example.ui.screens.StopState.DONE }
    return when {
      ui.pausedTitle != null -> "btn_resume_paused"
      pending -> "btn_home_play"
      ui.timeTrial == null -> "home_card"                              // sin Contrarreloj y con la ruta hecha: la tarjeta cerrada (Tarea 220: «Todos los juegos» ya no está en Hoy)
      exists("home_timetrial_none") -> "home_timetrial_none"          // sin partidas: el recuadro que lo explica
      exists("home_timetrial_done") -> "home_timetrial_done"          // marca nueva: el recuadro es la noticia; «Otra vez» queda debajo, deslizando
      else -> "btn_timetrial"
    }
  }

  /**
   * La acción principal se ve ENTERA sobre la barra; si es un recuadro de información (con marca nueva o sin partidas: no se toca) basta con que se vean sus primeras líneas (40 dp), porque a 360 dp el recuadro de la marca y de lo de mañana ocupa más de 140 dp y lo demás se alcanza deslizando.
   */
  private fun assertMainActionVisible(main: String, widthDp: Int, heightDp: Int, fontScale: Float, label: String) {
    val viewportBottom = (heightDp - 163).toFloat()
    val n = composeTestRule.onNodeWithTag(main).fetchSemanticsNode()
    val top = dp(n.positionInRoot.y)
    val bottom = unclippedBottomDp(main) - shadowInSize(main)
    val needed = if (main == "home_timetrial_none" || main == "home_timetrial_done") top + 40f else bottom
    assertTrue("$label: la acción principal «$main» ($top-$bottom dp) no se ve a $widthDp × $heightDp con letra $fontScale: necesita llegar a %.0f dp y la barra empieza en %.0f dp".format(needed, viewportBottom), needed <= viewportBottom + 1f)
  }

  /**
   * Cuánto se pasa Hoy de la barra, en dp, a 393 × 873 por estado (Tarea 220: sin la losa «Todos los juegos», que medía 70 dp con su sombra y su aire, todo sube 70 dp y casi nada se pasa; la regla de la Tarea 219 era MEDIR e informar. Antes, Tarea 219: el orden nuevo —«Todos los juegos» de 56 dp, la losa celeste de 80 dp y su línea de estado— sumó alto; la regla de la tarea es MEDIR e informar, no achicar). Lo que no está aquí tiene que caber.
   * La regla de siempre: la acción principal ([mainActionTag]) se ve entera sin deslizar y lo demás se alcanza deslizando.
   */
  private val scrollsAt393 = mapOf(
    1f to emptyMap<String, Float>(),                                                                                           // Tarea 220: sin «Todos los juegos» (70 dp: 56 + 6 de sombra + 8 de aire) todos caben con letra 1,0
    1.3f to mapOf("partida en pausa" to 11f, "sin partidas con marca nueva" to 36f, "sin partidas, ruta a medias" to 14f)
  )

  /**
   * Hoy se ve entero sin deslizar, salvo lo que dice [tolerance] (lo medido): lo último —el Contrarreloj— termina antes de la barra (a 163 dp del borde de abajo). Siempre: la acción principal ([mainActionTag]) entera a la vista y el resto, al menos, en el árbol (se alcanza deslizando).
   */
  private fun assertFitsWithoutScrolling(widthDp: Int, heightDp: Int, fontScale: Float, ui: HomeUi, label: String = "", tolerance: Float = 0f) {
    show(ui, widthDp, heightDp, fontScale)
    assertNoWordSplit()
    val viewportBottom = (heightDp - 163).toFloat()
    val last = lastBottomDp()
    println("HOY %d×%d letra %.1f %s: termina en %.0f dp de %.0f (%+.0f)".format(widthDp, heightDp, fontScale, label, last, viewportBottom, last - viewportBottom))
    assertTrue("$label: lo último de Hoy termina en %.0f dp y la barra empieza en %.0f dp: Hoy se desliza %.0f dp (se tolera %.0f)".format(last, viewportBottom, last - viewportBottom, tolerance), last <= viewportBottom + tolerance + 1f)
    val main = mainActionTag(ui)
    assertMainActionVisible(main, widthDp, heightDp, fontScale, label)
    for (tag in listOf("home_greeting", "home_date", "home_week", "home_card", "home_route_title", "home_route_meta", "home_stop_1", "home_stop_2", "home_stop_3", "btn_timetrial", "home_timetrial_done", "home_timetrial_none", "home_timetrial_line", "home_locked_games")) {
      if (exists(tag)) composeTestRule.onNodeWithTag(tag).assertExists()                                  // lo que se pasa se alcanza deslizando (se mide arriba)
    }
  }

  /** Los estados del día más la pausa (Tareas 167, 177 y 219): la primera hora, a mitad de ruta, la ruta completa con el Contrarreloj por jugar, jugado sin marca nueva y con marca nueva (con la ruta completa y a medias), y una partida en pausa. Con Nubilus Plus (sin tope de partidas). */
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

  /** Los estados SIN Nubilus Plus (Tarea 218): quedan partidas (sin jugar, jugado, marca nueva) o no queda ninguna (con y sin marca nueva, y con la ruta a medias). */
  private val noPlusStates: List<Pair<String, HomeUi>> by lazy {
    listOf(
      "sin Plus, ruta a medias" to ui(completed = 1, timeTrial = sinPlus),
      "sin Plus, ruta completa" to ui(completed = 3, timeTrial = sinPlus),
      "sin Plus, jugado" to ui(completed = 3, timeTrial = sinPlusJugado),
      "sin Plus, marca nueva" to ui(completed = 3, timeTrial = sinPlusMarca),
      "sin partidas" to ui(completed = 3, timeTrial = sinPartidas),
      "sin partidas con marca nueva" to ui(completed = 3, timeTrial = sinPartidasMarca),
      "sin partidas, ruta a medias" to ui(completed = 1, timeTrial = sinPartidas)
    )
  }
  private val allStates: List<Pair<String, HomeUi>> get() = dayStates + noPlusStates

  @Test fun hoy_a_393_por_873_con_letra_1_0_la_accion_principal_se_ve_y_lo_demas_se_alcanza_en_todos_los_estados() { for ((name, u) in allStates) assertFitsWithoutScrolling(393, 873, 1f, u, name, scrollsAt393.getValue(1f)[name] ?: 0f) }
  @Test fun hoy_a_393_por_873_con_letra_1_3_la_accion_principal_se_ve_y_lo_demas_se_alcanza_en_todos_los_estados() { for ((name, u) in allStates) assertFitsWithoutScrolling(393, 873, 1.3f, u, name, scrollsAt393.getValue(1.3f)[name] ?: 0f) }

  /** Tarea 219: cuánto mide Hoy en cada estado del día, a cada tamaño y letra (dp; positivo = lo último se pasa de la barra y se desliza). Solo MIDE (la regla de la tarea: informar los números, no achicar). */
  @Test
  fun tarea_219_medidas() {
    val rows = mutableListOf<String>()
    for ((w, h) in listOf(393 to 873, 360 to 780)) for (fs in listOf(1f, 1.3f, 2f)) {
      val cells = mutableListOf<String>()
      for ((name, u) in allStates) {
        show(u, w, h, fs)
        val over = lastBottomDp() - (h - 163)
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
    composeTestRule.onNodeWithTag("btn_timetrial").performScrollTo(); composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "$out/hoy-a-media-ruta-2_0-abajo.png")
  }

  @Test fun hoy_a_393_por_873_con_letra_1_0_captura() { show(ui(timeTrial = stroop)); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-393.png") }
  @Test fun hoy_a_393_por_873_con_letra_1_3_captura() { show(ui(timeTrial = stroop), fontScale = 1.3f); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-letra13.png") }
  @Test fun hoy_a_393_por_873_con_letra_1_3_y_con_la_invitacion_al_punto_de_partida_en_la_primera_hora() = assertFitsWithoutScrolling(393, 873, 1.3f, ui(completed = 0, timeTrial = stroop).copy(baselineInvite = true), "con la invitación", tolerance = 70f)

  /**
   * A 360 × 780 (el teléfono más chico que se mira) Hoy NO cabe entero en dos de los ocho estados del día (la partida en pausa y la marca nueva con la ruta a medias): se pasa de la barra y se desliza. Cuánto se mide (dp; Tarea 177, con la posición SIN recortar: `boundsInRoot` recorta y antes «termina justo en el borde» no probaba nada) y cuánto se tolera. La ruta completa y el Contrarreloj jugado (los de todos los días) caben desde que se apretó el ritmo vertical de Hoy 16 dp.
   * Lo que no está aquí tiene que caber. A 393 × 873 todos caben con letra 1,0 y 1,3.
   */
private val scrollsAt360 = mapOf(
    "primera hora" to 30f, "a mitad de ruta" to 30f, "marca nueva" to 1f, "marca nueva con la ruta a medias" to 73f, "partida en pausa" to 81f,
    "sin Plus, ruta a medias" to 30f, "sin Plus, marca nueva" to 33f, "sin partidas con marca nueva" to 41f, "sin partidas, ruta a medias" to 53f
  )

  @Test
  fun hoy_a_360_por_780_cuanto_se_pasa_de_la_barra_en_cada_estado() {
    val pasan = mutableListOf<String>()
    for ((name, u) in allStates) {
      show(u, widthDp = 360, heightDp = 780, fontScale = 1f)
      assertNoWordSplit(); assertNothingCutOrOverlapping(360)
      assertMainActionVisible(mainActionTag(u), 360, 780, 1f, "HOY360 $name")
      val over = lastBottomDp() - (780 - 163)
      println("HOY360 %s: se pasa %.0f dp".format(name, over))
      val limit = scrollsAt360[name] ?: 0f
      if (over > limit + 1f) pasan += "$name: se pasa %.0f dp (se tolera %.0f)".format(over, limit)
    }
    // lo que se pasa, se alcanza deslizando (después de medir todo: deslizar deja la página corrida y movería lo que se mide del estado que sigue)
    for ((name, u) in allStates) { show(u, widthDp = 360, heightDp = 780, fontScale = 1f); composeTestRule.onNodeWithTag(listOf("btn_home_baseline", "home_locked_games", "home_timetrial_line", "home_timetrial_none", "btn_timetrial", "home_card").first { exists(it) }).performScrollTo().assertIsDisplayed() }
    assertTrue("A 360 × 780 se pasan de la barra: $pasan", pasan.isEmpty())
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-360.png")
  }

  @Test
  fun hoy_con_letra_2_0_se_desliza_pero_nada_se_corta_ni_se_superpone() {
    for ((name, u) in allStates) { show(u.copy(hasBaseline = false, baselineInvite = true), fontScale = 2f); assertNothingCutOrOverlapping(393); assertNoWordSplit() }
    show(ui(hasBaseline = false, timeTrial = stroop), fontScale = 2f)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-letra20.png")
  }

  @Test fun a_360_dp_con_letra_1_3_nada_se_corta_ni_se_superpone() { for ((_, u) in allStates) { show(u, widthDp = 360, heightDp = 780, fontScale = 1.3f); assertNothingCutOrOverlapping(360); assertNoWordSplit() } }

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
    check(listOf("home_greeting", "home_date", "home_week", "home_card", "btn_timetrial", "home_timetrial_done", "home_timetrial_none", "home_timetrial_line", "home_locked_games", "home_route_tomorrow", "btn_home_baseline"), "afuera de la tarjeta")
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
  fun arriba_a_la_derecha_no_hay_botones_y_bajo_la_tarjeta_esta_solo_el_Contrarreloj_sin_la_losa_Todos_los_juegos() {
    // Tarea 158: el perfil es la pestaña «Tú». Tarea 220: «Todos los juegos» salió de Hoy y volvió a ser la pestaña Juegos; bajo la tarjeta va el Contrarreloj y nada más.
    show(ui(timeTrial = stroop))
    composeTestRule.onNodeWithTag("btn_top_profile").assertDoesNotExist()
    composeTestRule.onNodeWithTag("btn_top_settings").assertDoesNotExist()
    composeTestRule.onNodeWithContentDescription("Tu perfil").assertDoesNotExist()
    composeTestRule.onNodeWithTag("btn_all_games").assertDoesNotExist()
    composeTestRule.onAllNodes(hasText("Todos los juegos"), useUnmergedTree = true).assertCountEquals(0)
    composeTestRule.onAllNodes(hasText("Juegos"), useUnmergedTree = true).assertCountEquals(0)          // (la pestaña con ese nombre vive en la barra, no en Hoy)
    val gap = dp(unclipped("btn_timetrial").top - unclipped("home_card").bottom)
    assertTrue("el Contrarreloj va justo bajo la tarjeta, sin la losa de 56 dp en medio: ${gap} dp de aire", gap in 0f..24f)
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

  /** Nubi siempre despierta (Tarea 184, 27-oct): Ricardo la veía «siempre con los ojos cerrados» porque la Tarea 165 la dormía de 20 a 6 y él prueba de noche. A las 23:00 la tarjeta es la misma de siempre y Nubi tiene los ojos abiertos y parpadea como de día. */
  @Test
  fun de_noche_la_tarjeta_es_la_misma_y_Nubi_esta_despierta_con_los_ojos_abiertos_y_parpadea() {
    NubiIdle.seedForTests = 184L
    try {
      composeTestRule.mainClock.autoAdvance = false
      show(ui(completed = 1, at = night), wait = false)
      composeTestRule.onNodeWithText("Buenas noches, Ricardo").assertIsDisplayed()
      assertTrue(exists("home_nubi") && exists("btn_home_play"))
      val idle = NubiIdle.lastForTests ?: throw AssertionError("rememberNubiIdle no armó su estado")
      assertEquals("a las 23:00 los ojos están abiertos (nada los cierra: ya no hay Nubi dormida)", 0f, idle.eyesClosed, 0f)
      composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-noche.png")
      val firstBlink = NubiIdle.waitMs(Random(184L))                      // la hora del primer parpadeo con la semilla fija (entre 4 y 7 s)
      var now = 100L
      var closed = 0f
      while (now < firstBlink + 600) {                                    // por cuadros, para no perderse el cierre completo (dura 120 ms)
        composeTestRule.mainClock.advanceTimeBy(16); now += 16
        closed = maxOf(closed, idle.eyesClosed)
      }
      assertTrue("de noche parpadea igual que de día: los ojos se cierran del todo ($closed)", closed >= 0.9f)
      assertEquals("y vuelven a abrirse", 0f, idle.eyesClosed, 0f)
    } finally {
      NubiIdle.seedForTests = null; NubiIdle.lastForTests = null
    }
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
   * UN SOLO SOL A LA VEZ (Tarea 167; Tarea 219: el sol vive SOLO en la ruta): [sunOf] decide quién lo tiene (la ruta mientras sigue o hay una pausa; nadie con la ruta completa, porque el Contrarreloj tiene su color propio, el celeste) y NUNCA son dos. Robolectric no deja leer píxeles
   * aquí (`captureToImage` se cuelga; ver `OnBoardTextTest`), así que además se vigila el CÓDIGO: `HomeScreen.kt` no pide `Clay.Sun`, `SlabColor.SUN` no aparece (el Contrarreloj es celeste y «Otra vez», crema; ni sol ni lila) y los únicos `ClayButton` (el sol por omisión) son «Seguir tu partida» y la ruta, en ramas distintas.
   */
  @Test
  fun un_solo_sol_a_la_vez_y_vive_solo_en_la_ruta() {
    for ((name, u) in allStates) {
      val sun = sunOf(u)
      val pending = u.stops.any { it.state != com.example.ui.screens.StopState.DONE }
      if (u.pausedTitle != null || pending) assertEquals("$name: mientras la ruta sigue, el sol es de la ruta", Sun.ROUTE, sun)
      else assertEquals("$name: con la ruta completa no queda ningún sol (el Contrarreloj es celeste)", Sun.NONE, sun)
    }
    val src = java.io.File("src/main/java/com/example/ui/screens/HomeScreen.kt").readText()
    val code = src.lines().filterNot { it.trim().startsWith("*") || it.trim().startsWith("/**") || it.trim().startsWith("//") }.joinToString("\n")
    assertFalse("Hoy no pide el sol con Clay.Sun", code.contains("Clay.Sun"))
    assertEquals("el Contrarreloj no es sol: SlabColor.SUN no aparece en Hoy", 0, Regex("""SlabColor\.SUN""").findAll(code).count())
    assertFalse("ni lila (el lila es del Desafío)", code.contains("SlabColor.LILAC"))
    assertTrue("la losa celeste del Contrarreloj", code.contains("SlabColor.SKY"))
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
  fun con_la_ruta_completa_y_letra_1_3_la_losa_del_Contrarreloj_se_ve_entera() {
    show(ui(completed = 3, timeTrial = stroop), fontScale = 1.3f)
    val viewportBottom = (873 - 163).toFloat()
    val losa = unclippedBottomDp("btn_timetrial") - 6f
    assertTrue("la losa del Contrarreloj termina en %.0f dp de %.0f".format(losa, viewportBottom), losa <= viewportBottom + 1f)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-completa-letra13.png")
  }

  // ---- El Contrarreloj de hoy (Tarea 177; Tarea 219: la losa celeste bajo «Todos los juegos», sin título «Modos de juego»)

  private fun textOf(tag: String): String = composeTestRule.onNode(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Text)?.joinToString("") { it.text }.orEmpty().replace(NBSP, " ")
  private fun spokenOf(tag: String): String = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode().config.getOrNull(SemanticsProperties.ContentDescription)?.joinToString(". ").orEmpty().replace(NBSP, " ")
  private fun layoutOf(text: String): androidx.compose.ui.text.TextLayoutResult {
    val node = composeTestRule.onNode(hasText(text), useUnmergedTree = true).fetchSemanticsNode()
    val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
    node.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
    return results.first()
  }

  private fun contrast(a: androidx.compose.ui.graphics.Color, b: androidx.compose.ui.graphics.Color): Double {
    val la = a.luminance().toDouble(); val lb = b.luminance().toDouble()
    return (maxOf(la, lb) + 0.05) / (minOf(la, lb) + 0.05)
  }

  /** Tarea 219 (Ricardo, decisión 1 y 2 de `idea-pestana-contrarreloj.md` §10) y Tarea 220 («Todos los juegos» sale de Hoy): el orden de Hoy es saludo · semana · tarjeta · Contrarreloj, sin la losa «Todos los juegos» ni el título «Modos de juego». */
  @Test
  fun el_orden_de_Hoy_es_saludo_semana_tarjeta_y_Contrarreloj_y_ya_no_hay_losa_Todos_los_juegos_ni_titulo_Modos_de_juego() {
    for (done in 0..3) {
      show(ui(completed = done, timeTrial = stroop))
      val tops = listOf("home_greeting", "home_week", "home_card", "btn_timetrial").map { unclipped(it).top }
      assertEquals("con $done juegos hechos, de arriba abajo: saludo, semana, tarjeta, Contrarreloj", tops.sorted(), tops)
      val card = unclipped("home_card"); val c = unclipped("btn_timetrial")
      assertTrue("el Contrarreloj va DEBAJO de la tarjeta ($c / $card)", c.top >= card.bottom - 1f)
      assertTrue("a todo el ancho (${dp(c.width)} dp)", dp(c.width) >= 300f)
      assertFalse("sin la losa «Todos los juegos» (Tarea 220: es la pestaña Juegos)", exists("btn_all_games"))
      composeTestRule.onAllNodes(hasText("Todos los juegos"), useUnmergedTree = true).assertCountEquals(0)
      assertFalse("sin el título «Modos de juego»", exists("home_modes_title"))
      composeTestRule.onAllNodes(hasText("Modos de juego")).assertCountEquals(0)
    }
  }

  /** La forma B (decisión 2 de Ricardo): UNA losa celeste grande de 80 dp, el reloj de plasticina a 56 dp, «Contrarreloj» a 20 sp y «Hoy: <juego>» debajo. El celeste y la tinta se leen (≥ 4,5:1). */
  @Test
  fun la_losa_del_Contrarreloj_es_celeste_de_80_dp_con_el_reloj_de_56_dp_Contrarreloj_a_20_sp_y_Hoy_el_juego_debajo() {
    show(ui(completed = 3, timeTrial = stroop))
    assertEquals("sin jugar hoy la losa es CELESTE (el color propio del Contrarreloj)", com.example.ui.components.SlabColor.SKY, com.example.ui.screens.timeTrialSlabColor(again = false))
    assertEquals("y ya jugado, «Otra vez» en CREMA", com.example.ui.components.SlabColor.CREAM, com.example.ui.screens.timeTrialSlabColor(again = true))
    val c = unclipped("btn_timetrial"); val clock = unclipped("icon_reloj_clay", unmerged = true)
    println("LOSA CONTRARRELOJ: %.1f dp de alto, reloj %.1f dp".format(dp(c.height), dp(clock.height)))
    assertTrue("la losa mide ${dp(c.height)} dp: 80 de cuerpo más la sombra de la losa", dp(c.height) in 80f..90f)
    assertEquals("el reloj de plasticina mide 56 dp (antes 42)", 56f, dp(clock.height), 0.6f)
    assertEquals("y la constante es la de la tarea", 56.dp, com.example.ui.screens.TimeTrialClockSize)
    assertTrue("el reloj va DENTRO de la losa", clock.left >= c.left && clock.right <= c.right && clock.top >= c.top && clock.bottom <= c.bottom)
    assertEquals("«Contrarreloj» a 20 sp", 20f, layoutOf("Contrarreloj").layoutInput.style.fontSize.value, 0.01f)
    assertEquals("«Hoy: Tinta o Palabra» debajo, a 16 sp", 16f, layoutOf("Hoy: Tinta o Palabra").layoutInput.style.fontSize.value, 0.01f)
    assertTrue("«Hoy: …» va DEBAJO de «Contrarreloj»", composeTestRule.onNode(hasText("Hoy: Tinta o Palabra"), useUnmergedTree = true).fetchSemanticsNode().positionInRoot.y > composeTestRule.onNode(hasText("Contrarreloj"), useUnmergedTree = true).fetchSemanticsNode().positionInRoot.y)
    composeTestRule.onAllNodes(hasText("2 minutos", substring = true)).assertCountEquals(0)
    val ink = contrast(com.example.ui.theme.Clay.Ink, com.example.ui.theme.Clay.Sky)
    println("CELESTE de la losa: la tinta mide %.2f:1".format(ink))
    assertTrue("la tinta sobre el celeste del kit mide $ink:1 (mínimo 4,5)", ink >= 4.5)
    val src = java.io.File("src/main/java/com/example/ui/screens/HomeScreen.kt").readText().replace("\r\n", "\n")
    val body = src.substring(src.indexOf("private fun TimeTrialSlab("), src.indexOf("\n}\n", src.indexOf("private fun TimeTrialSlab(")))
    assertTrue("la losa pide su color a timeTrialSlabColor, el reloj a 56 dp y la altura de 80 dp", body.contains("color = timeTrialSlabColor(again)") && body.contains("ClayClockIcon(size = TimeTrialClockSize)") && body.contains("minHeight = 80.dp") && body.contains("fontSize = 20.sp"))
  }

  /** «Un solo sol a la vez» vive SOLO en la ruta: con la ruta completa la losa sigue celeste y el sol NO pasa al Contrarreloj. */
  @Test
  fun el_sol_vive_solo_en_la_ruta_y_con_la_ruta_completa_la_losa_sigue_celeste_sin_sol() {
    for (done in 0..2) {
      val u = ui(completed = done, timeTrial = stroop)
      show(u)
      assertEquals("con $done juegos hechos el sol es de la ruta", Sun.ROUTE, sunOf(u))
      assertTrue("y el botón de la ruta sigue dentro de la tarjeta", exists("btn_home_play"))
      composeTestRule.onNodeWithText("Contrarreloj").assertExists()
    }
    val done = ui(completed = 3, timeTrial = stroop)
    show(done)
    assertEquals("con la ruta completa no queda ningún sol", Sun.NONE, sunOf(done))
    assertEquals("y la losa es celeste", com.example.ui.components.SlabColor.SKY, com.example.ui.screens.timeTrialSlabColor(again = false))
    assertFalse("sin la frase de mañana mientras hay Contrarreloj", exists("home_route_tomorrow"))
  }

  @Test
  fun el_Contrarreloj_va_a_todo_el_ancho_bajo_la_tarjeta_en_todos_los_estados_y_con_letra_1_0_1_3_y_2_0() {
    for (fs in listOf(1f, 1.3f, 2f)) for ((name, u) in allStates) {
      if (u.timeTrial == null) continue
      show(u, fontScale = fs)
      val first = if (exists("btn_timetrial")) "btn_timetrial" else if (exists("home_timetrial_done")) "home_timetrial_done" else "home_timetrial_none"
      val c = unclipped(first); val card = unclipped("home_card")
      assertTrue("$name, letra $fs: lo del Contrarreloj («$first», ${dp(c.width)} dp) bajo la tarjeta y a todo el ancho", dp(c.width) >= 300f && c.top >= card.bottom - 1f)
      assertNoWordSplit()
    }
  }

  @Test
  fun la_palabra_Contrarreloj_cabe_en_UNA_linea_con_letra_1_0_1_3_y_2_0_y_el_juego_de_hoy_no_parte_palabras() {
    for (fs in listOf(1f, 1.3f, 2f)) {
      show(ui(completed = 3, timeTrial = stroop), fontScale = fs)
      assertEquals("«Contrarreloj» en una línea con letra $fs", 1, layoutOf("Contrarreloj").lineCount)
      composeTestRule.onNodeWithText("Hoy: Tinta o Palabra", useUnmergedTree = true).assertExists()
      assertNoWordSplit()
      show(ui(completed = 3, timeTrial = stroop.copy(title = "¿Verdad o disparate?")), fontScale = fs)
      composeTestRule.onNodeWithText("Hoy: ¿Verdad o disparate?", useUnmergedTree = true).assertExists()          // con 2,0 pasa a dos líneas, por palabras
      assertNoWordSplit()
    }
  }

  @Test
  fun con_letra_1_0_Contrarreloj_y_su_segunda_linea_caben_en_UNA_linea() {
    show(ui(completed = 1, timeTrial = stroop))
    assertEquals("«Contrarreloj» cabe en una línea con letra 1,0", 1, layoutOf("Contrarreloj").lineCount)
    assertEquals("«Hoy: Tinta o Palabra» cabe en una línea con letra 1,0", 1, layoutOf("Hoy: Tinta o Palabra").lineCount)
    assertNoWordSplit()
  }

  @Test
  fun tocar_el_Contrarreloj_lo_lanza_y_no_toca_la_ruta() {
    show(ui(completed = 1, timeTrial = stroop))
    composeTestRule.onNodeWithTag("btn_timetrial").performClick()
    assertEquals(1, trialed)
    assertEquals("no es la ruta", 0, played)
  }

  /** El lector de pantalla dice la losa ENTERA aunque a la vista solo diga «Contrarreloj» y «Hoy: <juego>»: el modo, el juego, los 2 minutos, lo jugado hoy, el récord y las partidas que quedan. */
  @Test
  fun el_lector_de_pantalla_dice_la_losa_entera_del_Contrarreloj() {
    show(ui(completed = 3, timeTrial = stroop))
    assertEquals("Contrarreloj de hoy, Tinta o Palabra, 2 minutos, tu récord 41 aciertos", spokenOf("btn_timetrial"))
    show(ui(completed = 3, timeTrial = stroopJugado))
    assertEquals("Contrarreloj de hoy, Tinta o Palabra, 2 minutos, hoy 38 aciertos, otra vez, tu récord 41 aciertos", spokenOf("btn_timetrial"))
    show(ui(completed = 3, timeTrial = sinPlus))
    assertEquals("sin Plus suma las partidas que quedan", "Contrarreloj de hoy, Tinta o Palabra, 2 minutos, tu récord 41 aciertos, te quedan 3 partidas de hoy", spokenOf("btn_timetrial"))
    show(ui(completed = 3, timeTrial = sinPlusJugado))
    assertEquals("Contrarreloj de hoy, Tinta o Palabra, 2 minutos, hoy 38 aciertos, otra vez, tu récord 41 aciertos, te quedan 2 partidas de hoy", spokenOf("btn_timetrial"))
    show(ui(completed = 3, timeTrial = sinPlus.copy(gamesLeft = 1)))
    assertTrue("con una sola, en singular", spokenOf("btn_timetrial").endsWith("te queda 1 partida de hoy"))
  }

  // ---- Los estados del Contrarreloj de hoy (sin Nubilus Plus / con Plus)

  @Test
  fun sin_jugar_la_losa_es_celeste_y_bajo_ella_el_record_con_Plus_o_las_partidas_sin_Plus_en_texto_sobre_el_cielo() {
    show(ui(completed = 3, timeTrial = stroop))
    assertEquals("con Plus: el récord", "Tu récord: 41 aciertos.", textOf("home_timetrial_line"))
    val c = bounds("btn_timetrial"); val line = bounds("home_timetrial_line")
    assertTrue("la línea va DEBAJO de la losa ($line / $c) y es texto plano", line.top >= c.bottom - 8f)
    composeTestRule.onNodeWithTag("home_timetrial_line").assertHasNoClickAction()
    assertFalse("sin Plus-nada: no hay recuadro ni fila gris", exists("home_timetrial_done") || exists("home_timetrial_none") || exists("home_locked_games"))
    show(ui(completed = 3, timeTrial = sinPlus))
    assertEquals("sin Plus y sin jugar: las partidas, NO el récord", "Te quedan 3 partidas de hoy.", textOf("home_timetrial_line"))
    show(ui(completed = 3, timeTrial = sinPlus.copy(gamesLeft = 1)))
    assertEquals("Te queda 1 partida de hoy.", textOf("home_timetrial_line"))
    show(ui(completed = 3, timeTrial = stroop.copy(record = null)))
    assertFalse("la primera vez no hay récord y la línea no se dibuja", exists("home_timetrial_line"))
    composeTestRule.onAllNodes(hasText("Tu récord", substring = true)).assertCountEquals(0)
  }

  @Test
  fun mientras_la_ruta_sigue_la_losa_y_su_linea_van_igual_bajo_la_tarjeta_y_la_ruta_tiene_el_sol() {
    show(ui(completed = 1, timeTrial = stroop))
    assertEquals("Tu récord: 41 aciertos.", textOf("home_timetrial_line"))
    assertTrue(unclipped("btn_timetrial").top >= unclipped("home_card").bottom - 1f)
    assertEquals(Sun.ROUTE, sunOf(ui(completed = 1, timeTrial = stroop)))
  }

  @Test
  fun jugado_con_partidas_la_losa_pasa_a_crema_Otra_vez_con_Hoy_el_juego_y_la_linea_dice_partidas_y_record() {
    val u = ui(completed = 3, timeTrial = stroopJugado)
    show(u)
    composeTestRule.onNodeWithText("Otra vez", useUnmergedTree = true).assertExists()
    composeTestRule.onNodeWithText("Hoy: Tinta o Palabra", useUnmergedTree = true).assertExists()
    composeTestRule.onAllNodes(hasText("Contrarreloj"), useUnmergedTree = true).assertCountEquals(0)          // «Otra vez» reemplaza al rótulo; el lector aún dice «Contrarreloj de hoy»
    assertEquals("con Plus: el récord", "Tu récord: 41 aciertos.", textOf("home_timetrial_line"))
    assertEquals(Sun.NONE, sunOf(u))
    val src = java.io.File("src/main/java/com/example/ui/screens/HomeScreen.kt").readText().replace("\r\n", "\n")
    assertTrue("«Otra vez» sale cuando el estado es JUGADO", src.contains("TimeTrialSlab(t, again = t.state == TimeTrialState.JUGADO"))
    composeTestRule.onNodeWithTag("btn_timetrial").performClick()
    assertEquals("se puede repetir", 1, trialed)
    show(ui(completed = 3, timeTrial = sinPlusJugado))
    assertEquals("sin Plus: las partidas y el récord, juntos", "Te quedan 2 partidas de hoy. Tu récord: 41 aciertos.", textOf("home_timetrial_line"))
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-contrarreloj-jugado.png")
  }

  @Test
  fun jugado_con_la_ruta_a_medias_la_losa_va_en_crema_y_la_ruta_sigue_con_el_sol() {
    val u = ui(completed = 1, timeTrial = stroopJugado)
    show(u)
    composeTestRule.onNodeWithText("Otra vez", useUnmergedTree = true).assertExists()
    composeTestRule.onNodeWithText("Hoy: Tinta o Palabra", useUnmergedTree = true).assertExists()
    assertEquals("la ruta sigue con el sol", Sun.ROUTE, sunOf(u))
  }

  @Test
  fun con_marca_nueva_el_recuadro_celeste_claro_lleva_el_visto_y_debajo_Otra_vez_en_crema_y_no_hay_sol() {
    val u = ui(completed = 3, timeTrial = stroopMarca)
    show(u)
    composeTestRule.onNodeWithText("Marca nueva: 43 aciertos en Tinta o Palabra.").assertIsDisplayed()
    composeTestRule.onNodeWithTag("home_timetrial_done").assertIsDisplayed().assertHasNoClickAction()
    composeTestRule.onNodeWithText("Otra vez", useUnmergedTree = true).assertExists()
    assertEquals("ningún sol: el día está hecho", Sun.NONE, sunOf(u))
    val note = unclipped("home_timetrial_done"); val again = unclipped("btn_timetrial"); val card = unclipped("home_card")
    assertTrue("«Otra vez» va a todo el ancho (${dp(again.width)} dp) bajo el recuadro, y el recuadro bajo la tarjeta ($note / $again / $card)", dp(again.width) >= 300f && again.top >= note.bottom - 1f && note.top >= card.bottom - 1f)
    assertFalse("sin la línea de estado (la marca nueva ya lo dice)", exists("home_timetrial_line"))
    composeTestRule.onNodeWithTag("btn_timetrial").performClick()
    assertEquals("«Otra vez» lanza el Contrarreloj de nuevo", 1, trialed)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-contrarreloj-marca-nueva.png")
  }

  /** Tarea 219b (auditoría de Fable de la 219, §3): sin Plus, con marca nueva y partidas que quedan, bajo «Otra vez» va «Te quedan N partidas de hoy.» (como en «jugado» pero SIN el récord, que ya está en el recuadro). Con Plus no hay línea; sin partidas tampoco (ya es un solo recuadro). */
  @Test
  fun sin_Plus_con_marca_nueva_y_partidas_bajo_Otra_vez_va_Te_quedan_N_partidas_sin_el_record_y_con_Plus_no_hay_linea() {
    show(ui(completed = 3, timeTrial = sinPlusMarca))
    assertEquals("Te quedan 2 partidas de hoy.", textOf("home_timetrial_line"))
    assertFalse("sin el récord: ya está en el recuadro", textOf("home_timetrial_line").contains("récord"))
    composeTestRule.onNodeWithTag("home_timetrial_line").assertHasNoClickAction()
    val note = unclipped("home_timetrial_done"); val again = unclipped("btn_timetrial"); val line = unclipped("home_timetrial_line")
    assertTrue("el recuadro, «Otra vez» y la línea van en ese orden ($note / $again / $line)", note.bottom <= again.top + 1f && again.bottom <= line.top + 1f)
    assertEquals("el lector dice las partidas como en «jugado»", "Contrarreloj de hoy, Tinta o Palabra, 2 minutos, hoy 43 aciertos, otra vez, tu récord 43 aciertos, te quedan 2 partidas de hoy", spokenOf("btn_timetrial"))
    show(ui(completed = 3, timeTrial = sinPlusMarca.copy(gamesLeft = 1)))
    assertEquals("en singular", "Te queda 1 partida de hoy.", textOf("home_timetrial_line"))
    show(ui(completed = 3, timeTrial = stroopMarca))
    assertFalse("con Plus no hay línea", exists("home_timetrial_line"))
    show(ui(completed = 3, timeTrial = sinPartidasMarca))
    assertFalse("sin partidas tampoco: ya es un solo recuadro", exists("home_timetrial_line"))
    for (fs in listOf(1f, 1.3f, 2f)) { show(ui(completed = 3, timeTrial = sinPlusMarca), fontScale = fs); assertNoWordSplit() }
  }

  @Test
  fun con_marca_nueva_y_la_ruta_a_medias_el_recuadro_va_a_todo_el_ancho_y_la_ruta_sigue_con_el_sol() {
    val u = ui(completed = 1, timeTrial = stroopMarca)
    show(u)
    composeTestRule.onNodeWithText("Marca nueva: 43 aciertos en Tinta o Palabra.").assertExists()
    assertEquals("la ruta sigue con el sol", Sun.ROUTE, sunOf(u))
    assertEquals("a todo el ancho, como la tarjeta", unclipped("home_card").width, unclipped("home_timetrial_done").width, 40f)
    composeTestRule.onNodeWithTag("home_timetrial_done").assertHasNoClickAction()
  }

  @Test
  fun la_marca_nueva_cabe_en_UNA_linea_con_letra_1_0_y_1_3_y_nunca_parte_una_palabra() {
    for (scale in listOf(1f, 1.3f)) {
      show(ui(completed = 3, timeTrial = stroopMarca), fontScale = scale)
      println("MARCA NUEVA letra $scale: ${layoutOf("Marca nueva: 43 aciertos en Tinta o Palabra.").lineCount} renglón(es)")
      assertNoWordSplit()
    }
  }

  /** El recuadro de la marca nueva ocupa el lugar y el ancho de la losa celeste (justo bajo la tarjeta, de lado a lado) y no es tocable; «Otra vez» queda debajo. */
  @Test
  fun el_recuadro_de_la_marca_nueva_esta_donde_estaba_la_losa_y_al_mismo_ancho() {
    show(ui(completed = 3, timeTrial = stroop))
    val losa = unclipped("btn_timetrial")
    show(ui(completed = 3, timeTrial = stroopMarca))
    val recuadro = unclipped("home_timetrial_done")
    assertEquals("a la misma altura que la losa celeste", losa.top, recuadro.top, 1f)
    assertEquals("desde el mismo borde izquierdo", losa.left, recuadro.left, 1f)
    assertEquals("al mismo ancho", losa.width, recuadro.width, 1f)
    composeTestRule.onNodeWithTag("home_timetrial_done").assertHasNoClickAction()
  }

  /** Sin partidas (sin Plus): ni losa ni «Otra vez»; el recuadro plano celeste claro dice «Hoy ya jugaste tus 3 partidas. Mañana: …» y debajo va UNA fila gris con candado que abre la hoja (Tarea 218). */
  @Test
  fun sin_partidas_no_hay_losa_ni_Otra_vez_y_el_recuadro_celeste_claro_dice_Manana_con_la_fila_gris_debajo() {
    show(ui(completed = 3, timeTrial = sinPartidas), onLocked = { locked++ })
    assertFalse("la losa del Contrarreloj no sale", exists("btn_timetrial"))
    composeTestRule.onAllNodes(hasText("Otra vez"), useUnmergedTree = true).assertCountEquals(0)
    assertEquals("Hoy ya jugaste tus 3 partidas. Mañana: ¿Verdad o disparate?", textOf("home_timetrial_tomorrow"))
    assertEquals("lo mismo para el lector", "Hoy ya jugaste tus 3 partidas. Mañana: ¿Verdad o disparate?", spokenOf("home_timetrial_none"))
    composeTestRule.onNodeWithTag("home_timetrial_none").assertHasNoClickAction()
    assertFalse("el visto es solo de la marca nueva", exists("home_timetrial_done"))
    composeTestRule.onAllNodes(hasTestTag("home_timetrial_mark"), useUnmergedTree = true).assertCountEquals(0)
    val card = unclipped("home_card"); val none = unclipped("home_timetrial_none"); val row = unclipped("home_locked_games")
    assertTrue("el recuadro va bajo la tarjeta y la fila gris bajo el recuadro ($card / $none / $row)", none.top >= card.bottom - 1f && row.top >= none.bottom - 1f)
    assertEquals("el recuadro va a todo el ancho", unclipped("home_card").width, none.width, 40f)
    assertEquals("la frase de la fila gris", PlusAccess.LOCKED_GAMES, textOf("home_locked_games_phrase"))
    composeTestRule.onNodeWithTag("home_locked_games").performClick()
    assertEquals("la fila gris abre la hoja del candado", 1, locked)
    assertEquals("y no lanza nada", 0, trialed)
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-sin-partidas.png")
  }

  /** Decisión de Opus (3-nov): marca nueva y sin partidas es UN solo recuadro celeste claro: arriba el visto con «Marca nueva: …» y, tras una línea fina, «Hoy ya jugaste tus 3 partidas. Mañana: …»; después, la fila gris (que sigue GRIS). */
  @Test
  fun sin_partidas_y_con_marca_nueva_es_UN_solo_recuadro_con_la_marca_arriba_la_frase_de_manana_debajo_y_despues_la_fila_gris() {
    show(ui(completed = 3, timeTrial = sinPartidasMarca))
    assertFalse("sin losa ni «Otra vez»", exists("btn_timetrial"))
    assertFalse("y no hay un segundo recuadro: la marca vive dentro del de «sin partidas»", exists("home_timetrial_done"))
    assertEquals("UN solo recuadro", 1, composeTestRule.onAllNodes(hasTestTag("home_timetrial_none")).fetchSemanticsNodes().size)
    assertEquals("Marca nueva: 43 aciertos en Tinta o Palabra.", textOf("home_timetrial_mark"))
    assertEquals("Hoy ya jugaste tus 3 partidas. Mañana: ¿Verdad o disparate?", textOf("home_timetrial_tomorrow"))
    assertEquals("el lector dice las dos de un tirón", "Marca nueva: 43 aciertos en Tinta o Palabra. Hoy ya jugaste tus 3 partidas. Mañana: ¿Verdad o disparate?", spokenOf("home_timetrial_none"))
    val box = unclipped("home_timetrial_none"); val mark = unclipped("home_timetrial_mark", unmerged = true); val tomorrow = unclipped("home_timetrial_tomorrow", unmerged = true); val row = unclipped("home_locked_games")
    assertTrue("la marca arriba y lo de mañana debajo, los dos DENTRO del recuadro ($mark / $tomorrow / $box)", mark.top < tomorrow.top && mark.top >= box.top && tomorrow.bottom <= box.bottom)
    assertTrue("la fila gris queda DEBAJO del recuadro ($row / $box)", row.top >= box.bottom - 1f)
    assertEquals("la frase de mañana se alinea con el texto de la marca, no con el visto", mark.left, tomorrow.left, 1f)
    composeTestRule.onNodeWithTag("home_timetrial_none").assertHasNoClickAction()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-sin-partidas-marca-nueva.png")
  }

  @Test
  fun con_Plus_siempre_hay_Otra_vez_y_nunca_la_fila_gris_ni_el_recuadro_de_sin_partidas() {
    for (t in listOf(stroop, stroopJugado, stroopMarca)) {
      show(ui(completed = 3, timeTrial = t))
      assertTrue("la losa está", exists("btn_timetrial"))
      assertFalse("sin fila gris ni «Hoy ya jugaste…»", exists("home_locked_games") || exists("home_timetrial_none"))
    }
  }

  /** El celeste claro de los recuadros planos (`#A5E4F7`, el del kit mezclado con la mitad de blanco): la tinta y la tinta suave leen ≥ 4,5:1 sobre él y no se confunde con el crema, el lima ni el lila (que es del Desafío). */
  @Test
  fun el_celeste_claro_de_los_recuadros_se_lee_con_la_tinta_y_no_se_confunde_con_el_crema_el_lima_ni_el_lila() {
    val fill = com.example.ui.screens.TimeTrialDoneFill
    val ink = contrast(com.example.ui.theme.Clay.Ink, fill); val muted = contrast(com.example.ui.theme.Clay.InkMuted, fill)
    println("CELESTE CLARO #%06X: tinta %.2f:1, tinta suave %.2f:1".format(fill.toArgb() and 0xFFFFFF, ink, muted))
    assertTrue("la tinta sobre el celeste claro mide $ink:1 (mínimo 4,5)", ink >= 4.5)
    assertTrue("la tinta suave sobre el celeste claro mide $muted:1 (mínimo 4,5)", muted >= 4.5)
    assertTrue("es más claro que el celeste de la losa", fill.luminance() > com.example.ui.theme.Clay.Sky.luminance())
    assertTrue("y se distingue del crema de la tarjeta (el azul domina al rojo)", fill.blue > fill.red + 0.1f)
    assertTrue("del lima de «Hecho» (el azul no baja del verde)", fill.blue >= fill.green)
    assertTrue("y del lila claro de antes `#D9CEFF` (aquí el verde domina al rojo)", fill.green > fill.red + 0.1f)
  }

  /** Los recuadros son PLANOS: su código no pide borde, sombra, losa, botón, reloj ni sol, y su radio es el del kit (22 dp). */
  @Test
  fun los_recuadros_son_planos_en_el_codigo_sin_borde_ni_sombra_ni_boton_ni_reloj() {
    val src = java.io.File("src/main/java/com/example/ui/screens/HomeScreen.kt").readText().replace("\r\n", "\n")
    val i = src.indexOf("private fun TimeTrialNote(")
    assertTrue("no encontré TimeTrialNote", i > 0)
    val body = src.substring(i, src.indexOf("\n}\n", i))
    for (banned in listOf("border(", "shadow", "clickable", "KitButton", "ClayButton", "ClaySlab", "ClayCard", "Glyph.", "SlabColor", "Clay.Sun", "toggleable", "selectable")) {
      assertFalse("el recuadro no usa `$banned`", body.contains(banned))
    }
    assertTrue("lleva el fondo celeste claro", body.contains("background(TimeTrialDoneFill)"))
    assertTrue("con las esquinas del kit", body.contains("RoundedCornerShape(TimeTrialDoneRadius)"))
    assertEquals("22 dp, el radio de los botones y las tarjetas del kit", 22.dp, com.example.ui.screens.TimeTrialDoneRadius)
    assertTrue("la marca nueva lleva el visto de «Hecho»", body.contains("StopMark(StopState.DONE)"))
  }

  @Test
  fun la_losa_lleva_el_reloj_de_plasticina_y_no_el_cometa_ni_el_glifo_de_churro() {
    val src = java.io.File("src/main/java/com/example/ui/screens/HomeScreen.kt").readText().replace("\r\n", "\n")
    assertTrue("el reloj de plasticina del kit, a 56 dp", src.contains("ClayClockIcon(size = TimeTrialClockSize)"))
    assertFalse("ya no el reloj de churro de 24 dp", src.contains("Glyph.CLOCK"))
    assertFalse("el cometa es del modo Desafío de la ficha, no del Contrarreloj", src.contains("Glyph.COMET"))
    assertFalse("ni queda nada del Desafío de hoy", src.contains("ChallengeState") || src.contains("DailyChallenge") || src.contains("HomeChallenge"))
    assertFalse("«Todos los juegos» ya no está en Hoy (Tarea 220: es la pestaña Juegos)", src.contains("AllGamesButton") || src.contains("ALL_GAMES_LABEL") || src.contains("onAllGames"))
  }

  @Test
  fun con_una_partida_en_pausa_el_Contrarreloj_sigue_en_celeste_y_el_sol_es_de_seguir_la_partida() {
    val u = ui(completed = 1, paused = "Carga exacta", timeTrial = stroop)
    show(u)
    assertEquals(Sun.ROUTE, sunOf(u))
    composeTestRule.onNodeWithText("Contrarreloj").assertExists()
    assertEquals(com.example.ui.components.SlabColor.SKY, com.example.ui.screens.timeTrialSlabColor(again = false))
    composeTestRule.onNodeWithText("Seguir tu partida").assertIsDisplayed()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/hoy-tarjeta-pausa-con-contrarreloj.png")
  }

  @Test
  fun sin_Contrarreloj_Hoy_queda_como_antes_y_con_la_ruta_completa_dice_que_manana_hay_una_ruta_nueva() {
    show(ui(completed = 1))
    assertFalse("sin losa de Contrarreloj", exists("btn_timetrial") || exists("home_timetrial_done"))
    composeTestRule.onNodeWithTag("home_card").assertIsDisplayed()
    show(ui(completed = 3))
    composeTestRule.onNodeWithText("Mañana te espera una ruta nueva.").assertIsDisplayed()
    assertEquals(Sun.NONE, sunOf(ui(completed = 3)))
  }

  @Test
  fun el_primer_vuelo_no_repite_la_frase_de_manana() {
    show(ui(completed = 3, flightDay = true))
    composeTestRule.onAllNodes(hasText("Mañana te espera una ruta nueva.")).assertCountEquals(0)
    composeTestRule.onNodeWithText("Tu primer vuelo fue tu ruta de hoy.").assertExists()
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
    composeTestRule.onNodeWithText("Tu primer vuelo fue tu ruta de hoy.").assertExists()
  }

  @Test
  fun pasadas_5_partidas_o_7_dias_Hoy_ya_no_invita_al_punto_de_partida() {
    // Tarea 152 (B6.2): la invitación vive en Hoy solo al principio; después, en Avance («Tu punto de partida»)
    show(ui(hasBaseline = false).copy(baselineInvite = false))
    composeTestRule.onAllNodes(hasTestTag("btn_home_baseline")).assertCountEquals(0)
    composeTestRule.onAllNodes(hasText("Encuentra tu punto de partida")).assertCountEquals(0)
  }

  @Test
  fun sin_punto_de_partida_la_invitacion_sigue_a_mano_debajo_de_la_tarjeta_y_del_Contrarreloj() {
    show(ui(hasBaseline = false))
    composeTestRule.onNodeWithTag("btn_home_baseline").assertExists()
    composeTestRule.onNodeWithText("Encuentra tu punto de partida").assertExists()
    assertTrue(unclipped("btn_home_baseline").top >= unclipped("home_card").bottom - 1f)
    show(ui(hasBaseline = false, timeTrial = stroop))
    assertTrue("con el Contrarreloj, la invitación va debajo de él", unclipped("btn_home_baseline").top >= unclipped("home_timetrial_line").bottom - 1f)
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

  /**
   * Tarea 193 (Ricardo, 30-oct): el día del Primer vuelo la tarjeta muestra con «Hecho» los juegos que de verdad se jugaron en el vuelo: hasta CUATRO filas (la ruta normal trae tres). Con letra 1,0 y 1,3 Hoy cabe entero a 393 × 873 con las cuatro: la tarjeta crece una fila y las demás piezas siguen donde cabían.
   * El encabezado usa la lógica de siempre («4 de 4 · ruta completa»).
   */
  private val flightFour = listOf("secuencia", "freno", "aterrizaje", "meteoros")

  /** Cuánto se pasa el Contrarreloj de donde empieza la barra (dp; negativo = sobra), con la tarjeta del día del vuelo de [ids] y la letra [fontScale]. */
  private fun flightDayOverflow(ids: List<String>, fontScale: Float): Float {
    show(ui(completed = ids.size, ids = ids, flightDay = true, timeTrial = stroop), fontScale = fontScale)
    assertNoWordSplit()
    composeTestRule.onNodeWithText("${ids.size} de ${ids.size} · ruta completa").assertExists()
    composeTestRule.onNodeWithTag("home_flight_day").assertExists()
    for (tag in listOf("home_stop_${ids.size}", "btn_timetrial")) composeTestRule.onNodeWithTag(tag).assertExists()
    val viewportBottom = dp((873 - 163) * composeTestRule.density.density)
    val over = lastBottomDp() - viewportBottom
    println("HOY día del vuelo, %d filas, letra %.1f: el Contrarreloj termina %.1f dp %s de donde empieza la barra".format(ids.size, fontScale, Math.abs(over), if (over > 0) "DESPUÉS" else "ANTES"))
    return over
  }

  /**
   * Tarea 193 (decisión de Ricardo y de Opus, 30-oct): el día del Primer vuelo la tarjeta muestra con «Hecho» los juegos que de verdad se jugaron: hasta CUATRO filas (la ruta normal trae tres), con la línea en UNA frase («Tu primer vuelo fue tu ruta de hoy.»). Con letra 1,0 Hoy cabe entero a 393 × 873 con las
   * cuatro (y con 1, 2 y 3). El encabezado usa la lógica de siempre («4 de 4 · ruta completa»).
   */
  @Test
  fun el_dia_del_vuelo_con_4_filas_cabe_sin_deslizar_con_letra_1_0() {
    for (n in 1..4) {
      val over = flightDayOverflow(flightFour.take(n), 1f)
      assertTrue("el día del vuelo con $n fila(s) a 393 × 873 y letra 1,0: el Contrarreloj termina %.1f dp DESPUÉS de donde empieza la barra: Hoy se desliza".format(over), over <= 1f)
      for (tag in listOf("home_greeting", "home_card", "home_stop_$n", "btn_timetrial", "home_flight_day")) composeTestRule.onNodeWithTag(tag).assertIsDisplayed()
    }
  }

  /**
   * Con letra 1,3 el día del vuelo es el único estado en que Hoy se desliza (la tarjeta de ese día suma la línea del vuelo y, con 4 juegos, una fila más): se MIDE y queda documentado. Todo se alcanza deslizando y nada se corta ni se parte; la guía de Nubi trae cada elemento a la vista antes de
   * iluminarlo. En los estados de siempre sigue valiendo «cabe entero» (`hoy_a_393_por_873_con_letra_1_3_...`).
   */
  @Test
  fun el_dia_del_vuelo_con_letra_1_3_se_mide_y_puede_deslizar() {
    val medidas = (1..4).associateWith { n -> flightDayOverflow(flightFour.take(n), 1.3f) }
    println("HOY día del vuelo con letra 1,3 (dp que se pasa de la barra, por número de filas): $medidas")
    assertTrue("con 4 filas y letra 1,3 se pasa como mucho %.0f dp (lo medido el 30-oct: ver la salida de esta prueba)".format(FLIGHT_DAY_13_MAX_OVER), medidas.getValue(4) <= FLIGHT_DAY_13_MAX_OVER)
  }

  /**
   * Tarea 194 (Ricardo: «se ve desplazado hacia abajo, debe estar más arriba»): cuando la tarjeta NO tiene botón y solo dice «Tu primer vuelo fue tu ruta de hoy.», la línea queda pegada al ritmo de las filas, con UN espacio chico (6 dp) bajo la última (antes 10 + 8 = 18 dp). Con botón, la línea sigue
   * 8 dp bajo él y el botón 10 dp bajo la última fila.
   */
  @Test
  fun la_linea_del_vuelo_sin_boton_queda_pegada_a_la_ultima_fila() {
    assertEquals("el espacio sin botón es chico", 6f, com.example.ui.screens.CAPTION_ONLY_GAP.value, 0.01f)
    var noButton = 0f
    for (n in 1..4) {
      show(ui(completed = n, ids = flightFour.take(n), flightDay = true, timeTrial = stroop))
      noButton = dp(unclipped("home_flight_day").top - unclipped("home_stop_$n").bottom)
      println("HOY día del vuelo con $n fila(s): la línea empieza %.1f dp bajo la caja de la última fila".format(noButton))
      assertTrue("con $n fila(s) la línea empieza %.1f dp bajo la última fila: debe quedar pegada (los 6 dp de CAPTION_ONLY_GAP; antes, con 18 dp de espacio, medía 18)".format(noButton), noButton <= 7f)
    }
    // con botón (la ruta sigue y hay una partida en pausa) el botón sigue separado de la última fila por el espacio de 10 dp (6 de la línea sola: unos 4 dp más)
    show(ui(paused = "Carga exacta", completed = 1))
    val buttonGap = dp(unclipped("btn_resume_paused").top - unclipped("home_stop_3").bottom)
    println("HOY con botón: el botón empieza %.1f dp bajo la caja de la última fila".format(buttonGap))
    assertTrue("con botón, el botón sigue %.1f dp bajo la última fila, más que la línea sola (%.1f)".format(buttonGap, noButton), buttonGap >= noButton + 3f)
  }

  @Test
  fun la_tarjeta_muestra_hasta_cuatro_paradas_y_en_el_orden_que_se_le_da() {
    val four = listOf("secuencia", "freno", "aterrizaje", "meteoros")
    val stops = homeStops(four, 4, AppLanguage.SPANISH)
    assertEquals(four, stops.map { it.gameId })
    assertTrue("las cuatro con «Hecho»", stops.all { it.state == com.example.ui.screens.StopState.DONE })
    assertEquals(4, homeStops(four + "bodega", 0, AppLanguage.SPANISH).size)
  }
}
