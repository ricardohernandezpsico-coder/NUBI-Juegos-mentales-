package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.test.swipeUp
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.AreaPoints
import com.example.data.SessionGame
import com.example.data.SessionSummary
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NubiHop
import com.example.ui.screens.NUBI_AIR
import com.example.ui.screens.SessionSummaryScreen
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File

/**
 * «¡Ruta de hoy completa!» como UNA losa crema (Tarea 198, 31-oct; Ricardo: «se ve poco clara y desordenada»; opción B de Fable, `auditoria-fable/resumen-del-dia.md`): Nubi con 20 dp de aire arriba (con 8 dp y un salto de 12 se cortaba), el título y la frase sobre el cielo, la losa con «Tus juegos» y «Tu avance»
 * (cada número UNA vez; la barra de los cuadros de Avance con lo de hoy a media tinta), la semana de Hoy debajo y el pie fijo con el único sol. Con los datos de Ricardo: Bodega, Freno y Aterrizaje, racha de 1 día, y un juego que no sumó puntos.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class SessionSummaryScreenTest {
  @get:Rule val rule = createComposeRule()

  @Before fun setUp() { TestSupport.fastFrames() }

  private val summary = SessionSummary(
    games = listOf(SessionGame("bodega", "MEMORIA", played = true, gained = 9), SessionGame("freno", "ATENCION", played = true, gained = 12), SessionGame("aterrizaje", "RAZONAMIENTO", played = true, gained = 0)),
    areas = listOf(PlaceTestData.points("MEMORIA", 330, 9), PlaceTestData.points("ATENCION", 410, 12), PlaceTestData.points("RAZONAMIENTO", 90, 0))
  )
  /** Jueves y viernes (hoy = viernes, índice 4): solo hoy encendida, racha de 1 día. */
  private val lamps = Week.Lamps(listOf(false, false, false, false, true, false, false), 4)
  private var closed = 0

  private fun show(widthDp: Int = 393, heightDp: Int = 873, scale: Float = 1f, reduceMotion: Boolean = false, summary: SessionSummary = this.summary) {
    rule.mainClock.autoAdvance = false
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides reduceMotion) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            SessionSummaryScreen(summary, streak = 1, lamps = lamps, lang = AppLanguage.SPANISH, onClose = { closed++ })
          }
        }
      }
    }
    rule.mainClock.advanceTimeBy(1_200)                                  // el saltito de Nubi (900 ms) ya terminó
    rule.waitForIdle()
    refresh()
  }

  /**
   * UNA foto del árbol de semántica (sin fusionar) por paso, no una consulta por dato: cada `onNodeWithTag` arma el árbol entero y Robolectric guarda una traza por cada uno (con unas 100 consultas por prueba el montón de la JVM se agotaba:
   * `OutOfMemoryError` en `ShadowTrace`). Se refresca al empezar, después de cada avance del reloj y después de deslizar.
   */
  private var tree: Map<String, androidx.compose.ui.semantics.SemanticsNode> = emptyMap()
  private var texts: List<String> = emptyList()
  private fun refresh() {
    val found = HashMap<String, androidx.compose.ui.semantics.SemanticsNode>()
    val all = ArrayList<String>()
    fun walk(n: androidx.compose.ui.semantics.SemanticsNode) {
      n.config.getOrNull(SemanticsProperties.TestTag)?.let { found.putIfAbsent(it, n) }
      n.config.getOrNull(SemanticsProperties.Text)?.let { list -> all += list.map { it.text } }
      n.children.forEach { walk(it) }
    }
    walk(rule.onRoot(useUnmergedTree = true).fetchSemanticsNode())
    tree = found
    texts = all
  }

  /**
   * El rectángulo SIN recortar de un nodo (posición en la raíz y tamaño): `boundsInRoot` devuelve solo lo que se ve dentro del área que se desliza (lo que queda fuera sale en ceros) y un Nubi cortado por arriba saldría «dentro». La posición incluye la capa gráfica
   * del saltito (si no la incluyera, la prueba del salto vería a Nubi quieta y fallaría).
   */
  private fun bounds(tag: String): Rect {
    val n = tree[tag] ?: error("no está «$tag» en el árbol: ${tree.keys.sorted()}")
    val p = n.positionInRoot
    return Rect(p.x, p.y, p.x + n.size.width, p.y + n.size.height)
  }
  private fun dp(px: Float) = px / rule.density.density
  private fun exists(tag: String) = tree.containsKey(tag)
  private fun text(tag: String): String = (tree[tag] ?: error("no está «$tag»")).config[SemanticsProperties.Text].joinToString("") { it.text }
  private fun lines(tag: String): Int {
    val results = ArrayList<TextLayoutResult>()
    (tree[tag] ?: error("no está «$tag»")).config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(results)
    return results.first().lineCount
  }

  // ------------------------------------------------------------------ el orden y lo que dice

  @Test
  fun va_de_arriba_a_abajo_Nubi_titulo_frase_losa_semana_y_el_pie_fijo() {
    show()
    val order = listOf("summary_nubi", "summary_title", "summary_sentence", "summary_card", "summary_week", "summary_footer_line", "btn_summary_close")
    val tops = order.map { bounds(it).top }
    assertEquals("el orden de arriba abajo: ${order.zip(tops)}", tops.sorted(), tops)
    val card = bounds("summary_card")
    assertTrue("la semana va DEBAJO de la losa, no dentro: ${bounds("summary_week").top} y ${card.bottom}", bounds("summary_week").top >= card.bottom)
    for (tag in listOf("summary_games_title", "summary_progress_title", "summary_divider") + listOf("bodega", "freno", "aterrizaje").map { "summary_row_$it" } + listOf("memoria", "atencion", "razonamiento").map { "summary_area_$it" }) {
      val b = bounds(tag)
      assertTrue("«$tag» está dentro de la losa: $b en $card", b.left >= card.left && b.right <= card.right && b.top >= card.top && b.bottom <= card.bottom)
    }
    assertTrue("«Tus juegos» va antes que la línea y «Tu avance» después", bounds("summary_games_title").bottom <= bounds("summary_divider").top && bounds("summary_divider").bottom <= bounds("summary_progress_title").top)
    rule.onNodeWithTag("btn_summary_close").assertIsDisplayed()
  }

  @Test
  fun dice_lo_que_sumo_cada_juego_y_los_puntos_de_cada_area_una_sola_vez() {
    show()
    assertEquals("9 puntos en Memoria", text("summary_game_bodega"))
    assertEquals("12 puntos en Atención", text("summary_game_freno"))
    assertEquals("un juego que no sumó puntos dice la palabra que la app ya tiene para un área que no cambió («te mantuviste»), en paralelo con «12 puntos en Atención» y diciendo en qué área fue: ya no la palabra suelta «Hecho»", "Te mantuviste en Razonamiento", text("summary_game_aterrizaje"))
    assertEquals("330 de ${AreaPoints.SCALE} puntos", text("summary_area_points_memoria"))
    assertEquals("410 de ${AreaPoints.SCALE} puntos", text("summary_area_points_atencion"))
    assertEquals("90 de ${AreaPoints.SCALE} puntos", text("summary_area_points_razonamiento"))
    assertEquals("Memoria", text("summary_area_name_memoria"))
    assertEquals("Trabajaste memoria, atención y razonamiento", text("summary_sentence"))
    // ya no están: la línea que repetía el número, la racha suelta y los títulos en mayúsculas
    val all = texts
    for (gone in listOf("Hoy sumaste", "Hoy te mantuviste", "Racha:", "TUS JUEGOS", "TU AVANCE", "Etapa ", "Hecho")) assertTrue("«$gone» ya no está en el resumen", all.none { it.contains(gone) })
    assertTrue("ninguna palabra de 4 letras o más va en mayúsculas sostenidas", all.none { t -> Regex("""[A-ZÁÉÍÓÚÑ]{4,}""").containsMatchIn(t) })
  }

  @Test
  fun la_semana_de_Hoy_trae_su_racha_junto_a_las_farolas() {
    show()
    assertEquals("la racha va en la semana, con su unidad: «1 día»", "1 día", text("streak_pill"))
    assertEquals(Week.lampsSpoken(1), tree.getValue("home_week").config[SemanticsProperties.ContentDescription].single())
    assertTrue("la semana está dentro de «summary_week»", bounds("home_week").top >= bounds("summary_week").top - 1f)
  }

  @Test
  fun un_area_sin_medir_lo_dice_y_su_barra_queda_vacia() {
    show(summary = SessionSummary(summary.games, listOf(PlaceTestData.points("MEMORIA", 0, 0, measured = false), summary.areas[1], summary.areas[2])))
    assertEquals("Aún sin medir", text("summary_area_points_memoria"))
    assertTrue("y su barra (el riel) está", exists("area_bar_summary_memoria"))
    assertFalse("sin relleno", exists("area_bar_fill_summary_memoria"))
  }

  // ------------------------------------------------------------------ Nubi no se corta

  @Test
  fun Nubi_queda_entera_dentro_del_area_visible_en_lo_alto_de_sus_dos_saltos() {
    rule.mainClock.autoAdvance = false
    rule.setContent { NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp)) { BoardBackground(); SessionSummaryScreen(summary, 1, lamps, AppLanguage.SPANISH, {}) } } }
    rule.waitForIdle()
    refresh()
    val scroll = bounds("summary_scroll")
    val rest = bounds("nubi_ojos_abiertos").top
    var lowest = Float.MAX_VALUE
    // el saltito son dos arcos de |sin|: lo más alto es a la cuarta parte y a las tres cuartas partes de su duración
    val quarter = (NubiHop.JUMP_MS * 0.25f).toLong()
    for (p in listOf(0.25f, 0.75f)) {
      rule.mainClock.advanceTimeBy(if (p == 0.25f) quarter else 2 * quarter)
      rule.waitForIdle()
      refresh()
      val top = bounds("nubi_ojos_abiertos").top
      println("SALTO p=$p: Nubi empieza en ${dp(top)} dp (en reposo, ${dp(rest)} dp) y el área visible, en ${dp(scroll.top)} dp")
      lowest = minOf(lowest, top)
    }
    assertTrue("la medida VE el salto: Nubi sube al menos 10 dp (${dp(rest - lowest)} dp)", dp(rest - lowest) >= 10f)
    assertTrue("Nubi queda entera dentro del área visible en lo alto del salto: su borde de arriba en ${dp(lowest)} dp y el del área en ${dp(scroll.top)} dp", lowest >= scroll.top)
    assertEquals("con el aire de 20 dp (12 del salto, 6 de la flotación y 2 de margen)", 20f, NUBI_AIR.value, 0f)
  }

  @Test
  fun con_quitar_animaciones_Nubi_no_salta() {
    rule.mainClock.autoAdvance = false
    rule.setContent { CompositionLocalProvider(LocalReduceMotion provides true) { NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp)) { BoardBackground(); SessionSummaryScreen(summary, 1, lamps, AppLanguage.SPANISH, {}) } } } }
    rule.waitForIdle()
    refresh()
    val at0 = bounds("nubi_ojos_abiertos").top
    rule.mainClock.advanceTimeBy((NubiHop.JUMP_MS * 0.25f).toLong())
    rule.waitForIdle()
    refresh()
    assertEquals("sin animaciones, Nubi se queda donde está", at0, bounds("nubi_ojos_abiertos").top, 0.5f)
  }

  // ------------------------------------------------------------------ nada se pisa ni se corta

  private fun nadaSePisa(widthDp: Int, heightDp: Int, scale: Float) {
    show(widthDp, heightDp, scale)
    val at = "$widthDp × $heightDp, letra $scale"
    val card = bounds("summary_card")
    fun gap(above: Rect, below: Rect) = dp(below.top - above.bottom)
    // las tres filas de juegos: planetita, nombre y línea, una bajo otra, sin pisarse; y las filas separadas ≥ 4 dp
    var previousRow: Rect? = null
    for (id in listOf("bodega", "freno", "aterrizaje")) {
      val row = bounds("summary_row_$id"); val name = bounds("summary_name_$id"); val line = bounds("summary_game_$id")
      assertTrue("$at: $id: el nombre y su línea no se pisan (${gap(name, line)} dp)", gap(name, line) >= -0.5f)
      assertTrue("$at: $id: nombre y línea dentro de la losa y de su fila", name.left >= row.left && line.right <= row.right + 0.5f && line.right <= card.right && name.right <= card.right)
      previousRow?.let { assertTrue("$at: entre las filas hay 4 dp o más (${gap(it, row)} dp)", gap(it, row) >= 4f) }
      previousRow = row
    }
    // las tres áreas: el nombre y los puntos (en una línea o uno bajo el otro), y la barra debajo de los dos
    for (key in listOf("memoria", "atencion", "razonamiento")) {
      val name = bounds("summary_area_name_$key"); val points = bounds("summary_area_points_$key"); val bar = bounds("area_bar_summary_$key")
      val sameLine = points.top < name.bottom - 1f
      if (sameLine) assertTrue("$at: $key: nombre y puntos en la misma línea con 4 dp o más entre los dos (${dp(points.left - name.right)} dp)", dp(points.left - name.right) >= 4f)
      else assertTrue("$at: $key: los puntos bajan debajo del nombre sin pisarlo (${gap(name, points)} dp)", gap(name, points) >= -0.5f)
      assertTrue("$at: $key: la barra queda 4 dp o más debajo de lo de arriba: nombre ${dp(name.top)}-${dp(name.bottom)}, puntos ${dp(points.top)}-${dp(points.bottom)}, barra ${dp(bar.top)}-${dp(bar.bottom)} (misma línea: $sameLine)", gap(points, bar) >= 4f && gap(name, bar) >= 4f)
      assertTrue("$at: $key: los puntos caben dentro de la losa (${dp(points.right)} dp)", points.right <= card.right - 8f)
      assertEquals("$at: $key: «${text("summary_area_name_$key")}» va en UNA línea (una palabra no se parte)", 1, lines("summary_area_name_$key"))
    }
    // la semana no pisa la losa y se alcanza deslizando; el botón del pie queda a la vista y fuera del área que se desliza
    rule.mainClock.autoAdvance = true                                    // deslizar espera fotogramas: con el reloj detenido giraba sin fin y agotaba la memoria
    rule.onNodeWithTag("summary_week").performScrollTo()
    rule.waitForIdle()
    refresh()
    assertTrue("$at: la semana queda 4 dp o más debajo de la losa", gap(bounds("summary_card"), bounds("summary_week")) >= 4f)
    assertTrue("$at: la semana termina antes del pie", bounds("summary_week").bottom <= bounds("summary_footer_line").top)
    rule.onNodeWithTag("btn_summary_close").assertIsDisplayed()
    assertTrue("$at: entre el final del área que se desliza y la frase del pie hay 16 dp o más (${dp(bounds("summary_footer_line").top - bounds("summary_scroll").bottom)} dp)", dp(bounds("summary_footer_line").top - bounds("summary_scroll").bottom) >= 16f - 0.5f)
    println("RESUMEN $at: la losa mide ${dp(card.height)} dp; el contenido llega hasta ${dp(bounds("summary_week").bottom)} dp de ${dp(bounds("summary_scroll").bottom)} dp visibles")
  }

  @Test fun a_393_por_873_con_letra_1_0_nada_se_pisa_ni_se_corta() = nadaSePisa(393, 873, 1f)
  @Test fun a_393_por_873_con_letra_1_3_nada_se_pisa_ni_se_corta() = nadaSePisa(393, 873, 1.3f)
  @Test fun a_393_por_873_con_letra_2_0_nada_se_pisa_ni_se_corta() = nadaSePisa(393, 873, 2f)
  @Test fun a_360_por_780_con_letra_1_0_nada_se_pisa_ni_se_corta() = nadaSePisa(360, 780, 1f)

  // ------------------------------------------------------------------ el desvanecido del borde de abajo (Tarea 198)

  @Test
  fun el_desvanecido_esta_solo_mientras_queda_contenido_por_ver() {
    show()
    assertFalse("con letra 1,0 a 393 todo cabe: no hay desvanecido (nada cambia a la vista)", exists("summary_fade"))
  }

  private fun desvanecido(widthDp: Int, heightDp: Int, scale: Float) {
    show(widthDp, heightDp, scale)
    val at = "$widthDp × $heightDp, letra $scale"
    assertTrue("$at: mientras queda contenido por ver, el borde de abajo se desvanece (marca «summary_fade»)", exists("summary_fade"))
    val scroll = bounds("summary_scroll"); val fade = bounds("summary_fade")
    assertEquals("$at: el desvanecido mide 24 dp", 24f, dp(fade.height), 0.6f)
    assertEquals("$at: y está pegado al borde de abajo del área que se desliza", scroll.bottom, fade.bottom, 1f)
    // (Robolectric no deja leer píxeles aquí: `captureToImage` se cuelga, como dice `OnBoardTextTest`. El dibujo se mira en la captura de Roborazzi de la lámina y su condición en el código: abajo)
    rule.mainClock.autoAdvance = true
    // al llegar al final ya no queda nada por ver: el desvanecido se va
    rule.onNodeWithTag("summary_scroll").performTouchInput { swipeUp(startY = height * 0.95f, endY = 0f, durationMillis = 200) }
    rule.onNodeWithTag("summary_scroll").performTouchInput { swipeUp(startY = height * 0.95f, endY = 0f, durationMillis = 200) }
    rule.waitForIdle()
    refresh()
    assertFalse("$at: con todo a la vista no hay desvanecido", exists("summary_fade"))
  }

  @Test
  fun el_desvanecido_se_dibuja_con_DstIn_hacia_lo_que_hay_detras_y_solo_si_queda_algo_por_ver() {
    val src = File("src/main/java/com/example/ui/screens/SessionSummaryScreen.kt").readText()
    assertTrue("el desvanecido borra hacia lo que hay detrás (BlendMode.DstIn sobre una capa aparte), no pinta un color de cielo que podría no coincidir", src.contains("BlendMode.DstIn") && src.contains("CompositingStrategy.Offscreen"))
    assertTrue("solo mientras queda contenido por ver", Regex("""if \(scroll\.canScrollForward\) \{\s*val fade""").containsMatchIn(src))
    assertTrue("mide 24 dp (BOTTOM_FADE)", com.example.ui.screens.BOTTOM_FADE.value == 24f)
    assertTrue("y se dibuja FUERA del verticalScroll (sobre el área visible, no sobre lo que se desliza)", src.indexOf(".drawWithContent") in 1 until src.indexOf(".verticalScroll(scroll)"))
  }

  @Test fun a_393_por_873_con_letra_1_3_el_borde_de_abajo_se_desvanece_mientras_queda_algo_por_ver() = desvanecido(393, 873, 1.3f)
  @Test fun a_393_por_873_con_letra_2_0_el_borde_de_abajo_se_desvanece_mientras_queda_algo_por_ver() = desvanecido(393, 873, 2f)
  @Test fun a_360_por_780_con_letra_1_0_el_borde_de_abajo_se_desvanece_mientras_queda_algo_por_ver() = desvanecido(360, 780, 1f)

  @Test
  fun con_letra_1_0_a_393_por_873_todo_cabe_sin_deslizar() {
    show()
    assertTrue("la semana termina antes del pie sin deslizar: ${dp(bounds("summary_week").bottom)} dp y el área visible llega a ${dp(bounds("summary_scroll").bottom)} dp", bounds("summary_week").bottom <= bounds("summary_scroll").bottom)
  }

  @Test
  fun el_boton_vuelve_a_Hoy() {
    show()
    rule.onNodeWithTag("btn_summary_close").performClick()
    assertEquals(1, closed)
  }

  // ------------------------------------------------------------------ el código

  @Test
  fun el_unico_sol_es_el_boton_y_no_hay_texto_amarillo() {
    val src = File("src/main/java/com/example/ui/screens/SessionSummaryScreen.kt").readText()
    assertFalse("el resumen ya no usa el amarillo de texto (era la racha y los dos títulos)", src.contains("SunOnNight"))
    assertFalse("ni el sol saturado: el único sol es el botón «Volver a Hoy» (ClayButton)", src.contains("Clay.Sun"))
    assertFalse("ni `AreaBar` de la vieja barra con brillo (components): la barra es la de los cuadros de Avance", src.contains("import com.example.ui.components.AreaBar"))
    assertTrue("la barra es la de los cuadros de Avance, con lo de hoy a media tinta", src.contains("AreaBar(") && src.contains("change = "))
  }
}
