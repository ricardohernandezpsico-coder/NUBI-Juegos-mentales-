package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsNode
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.NBSP
import com.example.data.TimeTrialResult
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.ContrarrelojPage
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File
import java.util.Calendar

/**
 * La página «Contrarreloj» de Avance (Tarea 199) con la pantalla REAL y los datos de la lámina de Fable (§11.3: 31, 35, 39, 36, 44 —el récord, 17 de octubre—, 38, 41, 37, 42 y 43 hoy): qué dibuja con cada letra (diez barras con la moneda del récord; cinco a 2,0, desde la fecha de la sexta y sin moneda
 * si el récord quedó fuera), lo que lee el lector de pantalla, los toques de 48 dp, y la prueba de cajas de Fable (§11.4): 4 dp o más entre cada par de piezas, nada fuera del margen de 16 dp y 8 dp entre losas, con letra 1,0, 1,3 y 2,0 a 393 dp y con 1,0 y 1,3 a 360 dp. Las cajas se leen SIN recortar
 * (`positionInRoot` y `size`: `boundsInRoot` recorta a lo que se ve y daría ceros fuera de la ventana), de UN solo árbol de semántica por prueba.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class ContrarrelojPageTest {
  @get:Rule val rule = createComposeRule()

  @Before fun setUp() { TestSupport.fastFrames() }

  private fun at(day: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, day, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(mark: Int, day: Int, errors: Int = 2, game: String = "stroop", version: Int = 2) =
    TimeTrialResult(id = "r-$game-$day-$mark", gameId = game, day = "2026-10-%02d".format(day), trackVersion = version, hits = mark + errors, errors = errors, level = 4, seconds = 120, timestamp = at(day))

  private val now = at(28, 18)
  private val ten = listOf(r(31, 8), r(35, 10), r(39, 13), r(36, 15), r(44, 17), r(38, 19, 3), r(41, 22, 3), r(37, 26, 3), r(42, 27, 3), r(43, 28, 3))
  private val others = listOf(r(31, 21, game = "disparate"), r(27, 23, game = "radar"), r(19, 24, game = "acoplamiento"), r(38, 12, errors = 0, version = 1))

  private var density = 1f
  private var selectedCalls = mutableListOf<String>()
  private var backCalls = 0

  private fun show(widthDp: Int = 393, scale: Float = 1f, results: List<TimeTrialResult> = ten + others, selected: String = "stroop") {
    rule.setContent {
      val base = LocalDensity.current
      density = base.density
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(873.dp)) {
            BoardBackground()
            ContrarrelojPage(results = results, selected = selected, onSelect = { selectedCalls += it }, onBack = { backCalls += 1 }, now = now, lang = com.example.model.AppLanguage.SPANISH)
          }
        }
      }
    }
    rule.waitForIdle()
  }

  // ---------------------------------------------------------------- cajas

  private class Rect(val x: Float, val y: Float, val w: Float, val h: Float) {
    val r get() = x + w
    val b get() = y + h
  }

  /** Todas las piezas con marca de prueba, en dp y SIN recortar, de UN solo árbol (el sin fusionar: lo que pisa o se sale se ve aunque la losa lo agrupe). */
  private fun boxes(): Map<String, Rect> {
    val out = LinkedHashMap<String, Rect>()
    fun walk(n: SemanticsNode) {
      n.config.getOrNull(SemanticsProperties.TestTag)?.let { tag -> out[tag] = Rect(n.positionInRoot.x / density, n.positionInRoot.y / density, n.size.width / density, n.size.height / density) }
      n.children.forEach(::walk)
    }
    walk(rule.onRoot(useUnmergedTree = true).fetchSemanticsNode())
    return out
  }

  /** La separación entre dos cajas: la mayor de las distancias horizontal y vertical entre bordes (negativa si se tapan). */
  private fun gap(a: Rect, b: Rect): Float = maxOf(a.x - b.r, b.x - a.r, a.y - b.b, b.y - a.b)

  private val record = listOf("contrarreloj_record_(title|figure|when|step|empty)", "contrarreloj_chart_(title|first)", "contrarreloj_figure_\\d+", "contrarreloj_coin", "contrarreloj_barshape_\\d+", "contrarreloj_axis_(left|right)", "contrarreloj_averages")
  private val weeks = listOf("contrarreloj_weeks_title", "contrarreloj_week_(this|last)_title", "contrarreloj_week_(this|last)_\\d_(label|value)")
  private val split = listOf("contrarreloj_split_title", "contrarreloj_split_(record|recent)_title", "contrarreloj_split_(record|recent)_(hits|errors)_(bar|value)", "contrarreloj_rule")
  private val older = listOf("contrarreloj_older_title", "contrarreloj_older_text", "contrarreloj_note")
  private val header = listOf("contrarreloj_back", "contrarreloj_title", "contrarreloj_subtitle")
  private val cards = listOf("contrarreloj_record", "contrarreloj_weeks", "contrarreloj_split", "contrarreloj_older")
  private val games = listOf("stroop", "disparate", "radar", "acoplamiento")

  private fun groupOf(map: Map<String, Rect>, patterns: List<String>) = map.filterKeys { k -> patterns.any { Regex(it).matches(k) } }

  private fun assertNoClashes(widthDp: Int, scale: Float) {
    show(widthDp, scale)
    val all = boxes()
    val label = "a $widthDp dp con letra $scale"
    val groups = mutableMapOf("la cabecera" to groupOf(all, header), "Tu récord" to groupOf(all, record), "Esta semana y la pasada" to groupOf(all, weeks), "Aciertos y errores" to groupOf(all, split), "Pistas anteriores" to groupOf(all, older))
    for (g in games) groups["la losa de $g"] = groupOf(all, listOf("contrarreloj_game_(emblem|name|value)_$g"))
    val problems = mutableListOf<String>()
    for ((name, g) in groups) {
      assertTrue("$label: la prueba no vio piezas en $name", g.size >= 2)
      val keys = g.keys.toList()
      for (i in keys.indices) for (j in i + 1 until keys.size) {
        val s = gap(g.getValue(keys[i]), g.getValue(keys[j]))
        if (s < 3.5f) problems += "$name: «${keys[i]}» y «${keys[j]}» a %.1f dp".format(s)
      }
    }
    // nada fuera del margen de 16 dp
    for ((tag, b) in all) if (tag.startsWith("contrarreloj_") && tag != "contrarreloj_page" && tag != "contrarreloj_scroll" && tag != "contrarreloj_header") {
      if (b.x < 15.5f || b.r > widthDp - 15.5f) problems += "«$tag» se sale del margen de 16 dp (x %.1f a %.1f)".format(b.x, b.r)
    }
    // 8 dp o más entre losas (cuatro juegos y cuatro tarjetas)
    val slabs = (games.map { "contrarreloj_game_$it" } + cards).filter { it in all }
    for (i in slabs.indices) for (j in i + 1 until slabs.size) {
      val s = gap(all.getValue(slabs[i]), all.getValue(slabs[j]))
      if (s < 7.5f) problems += "las losas «${slabs[i]}» y «${slabs[j]}» a %.1f dp".format(s)
    }
    assertTrue("$label:\n" + problems.joinToString("\n"), problems.isEmpty())
  }

  @Test fun cero_choques_a_393_con_letra_1_0() = assertNoClashes(393, 1f)
  @Test fun cero_choques_a_393_con_letra_1_3() = assertNoClashes(393, 1.3f)
  @Test fun cero_choques_a_393_con_letra_2_0() = assertNoClashes(393, 2f)
  @Test fun cero_choques_a_360_con_letra_1_0() = assertNoClashes(360, 1f)
  @Test fun cero_choques_a_360_con_letra_1_3() = assertNoClashes(360, 1.3f)

  /** Lo que mide la página entera (de arriba al borde de abajo de la última losa más sus 32 dp de aire), en dp: lo mide `positionInRoot` + `size`, sin deslizar. */
  private fun pageHeight(): Float = boxes().let { b -> b.getValue("contrarreloj_older").b + 32f }

  @Test fun a_393_con_letra_1_0_la_cabecera_el_selector_y_tu_record_caben_en_la_primera_pantalla() {
    show()
    val b = boxes()
    assertTrue("«Tu récord» entero termina en %.0f dp, dentro de los 873 dp de la primera pantalla".format(b.getValue("contrarreloj_record").b), b.getValue("contrarreloj_record").b <= 873f)
    println("T199 alto de la página a 393 con letra 1,0: %.0f dp; «Tu récord» termina en %.0f dp".format(pageHeight(), b.getValue("contrarreloj_record").b))
  }

  @Test fun el_alto_de_la_pagina_a_393_con_letra_1_3() { show(scale = 1.3f); println("T199 alto de la página a 393 con letra 1,3: %.0f dp".format(pageHeight())) }
  @Test fun el_alto_de_la_pagina_a_393_con_letra_2_0() { show(scale = 2f); println("T199 alto de la página a 393 con letra 2,0: %.0f dp".format(pageHeight())) }
  @Test fun el_alto_de_la_pagina_a_360_con_letra_1_0() { show(widthDp = 360); println("T199 alto de la página a 360 con letra 1,0: %.0f dp".format(pageHeight())) }

  // ---------------------------------------------------------------- lo que dibuja

  private fun count(prefix: String) = boxes().keys.count { it.startsWith(prefix) }

  @Test fun con_letra_1_0_dibuja_diez_barras_con_la_moneda_en_el_record_y_el_eje_desde_la_primera() {
    show()
    val b = boxes()
    assertEquals("diez barras", 10, b.keys.count { it.startsWith("contrarreloj_barshape_") })
    assertEquals("diez cifras", 10, b.keys.count { it.startsWith("contrarreloj_figure_") })
    assertEquals("UNA moneda, la del récord (la quinta)", 1, b.keys.count { it == "contrarreloj_coin" })
    val coin = b.getValue("contrarreloj_coin"); val bar4 = b.getValue("contrarreloj_barshape_4")
    assertTrue("la moneda está sobre la barra del récord", coin.x + coin.w / 2 in bar4.x..bar4.r && coin.b <= b.getValue("contrarreloj_figure_4").y)
    for (i in 0..9) assertTrue("las cifras van en UNA fila (la cifra $i)", Math.abs(b.getValue("contrarreloj_figure_$i").y - b.getValue("contrarreloj_figure_0").y) < 0.5f)
    for (i in 0..9) assertTrue("las barras parten de una base común (la barra $i: ${b.getValue("contrarreloj_barshape_$i").b} contra ${b.getValue("contrarreloj_barshape_0").b})", Math.abs(b.getValue("contrarreloj_barshape_$i").b - b.getValue("contrarreloj_barshape_0").b) < 0.5f)
    assertTrue("el récord es la barra más alta", (0..9).all { b.getValue("contrarreloj_barshape_$it").h <= b.getValue("contrarreloj_barshape_4").h + 0.1f })
    // los rótulos del eje no se leen por separado (el gráfico los dice); su texto sale de `TimeTrialMarks.axisLabels` (probada en `TimeTrialMarksTest`: «8 de octubre» y «hoy»)
    assertNotNull("el rótulo de la izquierda", b["contrarreloj_axis_left"])
    assertNotNull("y «hoy» a la derecha", b["contrarreloj_axis_right"])
    assertTrue("a la izquierda de la primera barra y a la derecha de la última", b.getValue("contrarreloj_axis_left").x < b.getValue("contrarreloj_axis_right").x && b.getValue("contrarreloj_axis_right").r <= b.getValue("contrarreloj_barshape_9").r + 4f)
    assertTrue("bajo la base común, a 4 dp o más", b.getValue("contrarreloj_axis_left").y >= b.getValue("contrarreloj_barshape_0").b + 4f)
  }

  @Test fun con_letra_2_0_dibuja_cinco_barras_desde_la_fecha_de_la_sexta_y_sin_moneda_porque_el_record_quedo_fuera() {
    show(scale = 2f)
    val b = boxes()
    assertEquals(5, b.keys.count { it.startsWith("contrarreloj_barshape_") })
    assertEquals("el récord (la quinta partida) ya no está entre las últimas 5: no hay moneda", 0, b.keys.count { it == "contrarreloj_coin" })
    assertNotNull("el eje sigue con su fecha (la de la sexta partida: «19 de octubre», `TimeTrialMarksTest`)", b["contrarreloj_axis_left"])
    assertEquals("Tus últimas 5 marcas", textOf("contrarreloj_chart_title"))
  }

  @Test fun con_letra_1_3_siguen_las_diez_barras_y_el_selector_pasa_a_una_columna() {
    show(scale = 1.3f)
    val b = boxes()
    assertEquals(10, b.keys.count { it.startsWith("contrarreloj_barshape_") })
    val x = games.map { b.getValue("contrarreloj_game_$it").x }
    assertTrue("una columna: las cuatro losas parten del mismo borde", x.all { Math.abs(it - x[0]) < 0.5f })
    assertTrue("y una bajo otra", games.zipWithNext().all { (a, c) -> b.getValue("contrarreloj_game_$c").y > b.getValue("contrarreloj_game_$a").b })
  }

  @Test fun con_letra_1_0_el_selector_son_dos_filas_parejas_de_dos() {
    show()
    val b = boxes()
    val t = games.map { b.getValue("contrarreloj_game_$it") }
    assertTrue("dos columnas", t[0].x == t[2].x && t[1].x == t[3].x && t[1].x > t[0].r)
    assertEquals("la fila de arriba tiene el mismo alto", t[0].h, t[1].h, 0.5f)
    assertEquals("la de abajo también", t[2].h, t[3].h, 0.5f)
    assertTrue("cada losa mide 72 dp o más", t.all { it.h >= 71.5f })
    assertEquals("el borde derecho de la fila llega al margen", 393f - 16f, t[1].r, 1f)
  }

  @Test fun el_record_dice_su_fecha_su_unidad_y_el_paso_de_la_pista() {
    show()
    assertEquals("44 aciertos", textOf("contrarreloj_record_figure").replace(NBSP, " "))
    assertEquals("el 17 de octubre, en 2 minutos de Contrarreloj", textOf("contrarreloj_record_when").replace(NBSP, " "))
    assertEquals("Llegaste al paso 4 de 5 de la pista.", textOf("contrarreloj_record_step").replace(NBSP, " "))
    assertEquals("Tus últimas 5 marcas promedian 40 aciertos; las 5 anteriores, 37.", textOf("contrarreloj_averages").replace(NBSP, " "))
    assertTrue("el número no se separa de su unidad al partir la línea (espacio duro)", textOf("contrarreloj_record_when").contains("2${NBSP}minutos") && textOf("contrarreloj_record_figure").contains("44${NBSP}aciertos"))
  }

  @Test fun las_tarjetas_de_semana_aciertos_y_pistas_dicen_lo_de_la_lamina() {
    show()
    assertEquals("3 partidas", textOf("contrarreloj_week_this_0_value").replace(NBSP, " "))
    assertEquals("43 aciertos", textOf("contrarreloj_week_this_1_value").replace(NBSP, " "))
    assertEquals("2 partidas", textOf("contrarreloj_week_last_0_value").replace(NBSP, " "))
    assertEquals("41 aciertos", textOf("contrarreloj_week_last_1_value").replace(NBSP, " "))
    assertEquals("46 aciertos", textOf("contrarreloj_split_record_hits_value").replace(NBSP, " "))
    assertEquals("2 errores", textOf("contrarreloj_split_record_errors_value").replace(NBSP, " "))
    assertEquals("43 aciertos", textOf("contrarreloj_split_recent_hits_value").replace(NBSP, " "))
    assertEquals("3 errores", textOf("contrarreloj_split_recent_errors_value").replace(NBSP, " "))
    assertEquals("La marca cuenta los aciertos menos los errores, sin bajar de cero.", textOf("contrarreloj_rule"))
    assertEquals("Pistas anteriores", textOf("contrarreloj_older_title"))
    assertTrue(textOf("contrarreloj_older_text").replace(NBSP, " ").startsWith("Tu récord de la pista 1: 38 aciertos."))
    assertEquals("Es el resultado de un juego, comparado solo contigo. No es una medida de capacidad.", textOf("contrarreloj_note"))
    val b = boxes()
    assertTrue("la barra de aciertos del récord es más larga que la de errores y las dos usan la misma escala", b.getValue("contrarreloj_split_record_hits_bar").w > 5 * b.getValue("contrarreloj_split_record_errors_bar").w)
    assertTrue("el récord (46) pesa más que el promedio de las últimas (43)", b.getValue("contrarreloj_split_record_hits_bar").w > b.getValue("contrarreloj_split_recent_hits_bar").w)
  }

  @Test fun sin_pista_anterior_la_ultima_losa_es_solo_la_nota() {
    show(results = ten)
    assertNull("sin pista anterior, sin título de sección", boxes()["contrarreloj_older_title"])
    assertNull(boxes()["contrarreloj_older_text"])
    assertNotNull(boxes()["contrarreloj_note"])
  }

  @Test fun con_una_sola_marca_dice_que_la_primera_ya_esta_y_no_dibuja_barras() {
    show(results = listOf(r(31, 28)))
    val b = boxes()
    assertEquals("Tu primera marca. Desde la segunda, se dibujan aquí.", textOf("contrarreloj_chart_first"))
    assertEquals(0, b.keys.count { it.startsWith("contrarreloj_barshape_") })
    assertNull("sin dos promedios", b["contrarreloj_averages"])
    assertEquals("la mejor marca de la semana es la única", "31 aciertos", textOf("contrarreloj_week_this_1_value").replace(NBSP, " "))
    assertNull("con una sola partida el récord ya es «las últimas»: no hay segunda fila", b["contrarreloj_split_recent_title"])
  }

  @Test fun un_juego_sin_marcas_dice_que_falta_y_no_muestra_semana_ni_aciertos() {
    show(selected = "radar", results = listOf(r(31, 28)))
    assertEquals("Todavía no tienes marcas en este juego. Cuando juegues su Contrarreloj, aquí aparece tu récord.", textOf("contrarreloj_record_empty"))
    val b = boxes()
    assertNull(b["contrarreloj_weeks"]); assertNull(b["contrarreloj_split"])
    assertNotNull("la nota de siempre sigue al pie", b["contrarreloj_note"])
    assertEquals("sin marca aún", textOf("contrarreloj_game_value_radar"))
  }

  // ---------------------------------------------------------------- lector de pantalla y toques

  private fun textOf(tag: String): String =
    rule.onNode(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Text)?.joinToString("") { it.text } ?: error("«$tag» no tiene texto")

  private fun descriptionOf(tag: String): String =
    rule.onNode(hasTestTag(tag)).fetchSemanticsNode().config.getOrNull(SemanticsProperties.ContentDescription)?.joinToString(". ") ?: error("«$tag» no tiene descripción")

  @Test fun el_lector_de_pantalla_dice_las_barras_el_selector_y_las_filas() {
    show()
    assertEquals("Tus últimas 10 marcas: 31, 35, 39, 36, 44 aciertos, tu récord, 38, 41, 37, 42 y 43 aciertos, la de hoy.", descriptionOf("contrarreloj_bars"))
    assertEquals("Tinta o Palabra, récord 44 aciertos, elegido", descriptionOf("contrarreloj_game_stroop"))
    assertEquals("¿Verdad o disparate?, récord 31 aciertos", descriptionOf("contrarreloj_game_disparate"))
    assertEquals("Jugaste, 3 partidas", descriptionOf("contrarreloj_week_this_0"))
    assertEquals("La mejor marca, 43 aciertos", descriptionOf("contrarreloj_week_this_1"))
    assertEquals("46 aciertos", descriptionOf("contrarreloj_split_record_hits"))
    assertEquals("la barra y las cifras sueltas no se leen por separado", 1, rule.onAllNodes(hasTestTag("contrarreloj_bars")).fetchSemanticsNodes().size)
    assertTrue("elegido se anuncia como seleccionado", rule.onNodeWithTag("contrarreloj_game_stroop").fetchSemanticsNode().config.getOrNull(SemanticsProperties.Selected) == true)
    assertFalse(rule.onNodeWithTag("contrarreloj_game_radar").fetchSemanticsNode().config.getOrNull(SemanticsProperties.Selected) == true)
  }

  @Test fun tocar_un_juego_lo_elige_y_el_disco_vuelve() {
    show()
    rule.onNodeWithTag("contrarreloj_game_radar").performClick(); rule.waitForIdle()
    assertEquals(listOf("radar"), selectedCalls)
    rule.onNodeWithTag("contrarreloj_back", useUnmergedTree = true).assertIsDisplayed().performClick(); rule.waitForIdle()
    assertEquals(1, backCalls)
  }

  @Test fun todo_lo_que_se_toca_mide_48_dp_o_mas() {
    for (scale in listOf(1f)) {
      show(scale = scale)
      val b = boxes()
      for (tag in games.map { "contrarreloj_game_$it" } + "contrarreloj_back") {
        val t = b.getValue(tag)
        assertTrue("«$tag» mide %.1f × %.1f dp".format(t.w, t.h), t.w >= 47.5f && t.h >= 47.5f)
      }
    }
  }

  // ---------------------------------------------------------------- fuentes

  private fun src(path: String) = File("src/main/java/com/example/$path").readText()

  @Test fun la_pagina_usa_el_noche_plano_del_token_el_cielo_solo_en_la_cabecera_y_titulos_en_rubik() {
    val page = src("ui/screens/ContrarrelojPage.kt")
    assertTrue("el fondo plano sale de NubiTokens.NightFlat", page.contains("NubiTokens.NightFlat"))
    assertFalse("y no hay un literal de color copiado (el token es la fuente)", Regex("0x[Ff][Ff]0[5-6]08\\d\\d").containsMatchIn(page))
    assertEquals("el noche plano es #050823", 0x050823, com.example.ui.theme.NubiTokens.NightFlat.toArgb() and 0xFFFFFF)
    // la cabecera va sobre el cielo con OnBoardText; el resto, en losas con Text
    assertTrue(page.contains("OnBoardText(TimeTrialMarks.TITLE") && page.contains("OnBoardText(TimeTrialMarks.SUBTITLE"))
    // los títulos de las losas, en Rubik
    val titles = Regex("""Text\(title, color = Clay\.Ink, fontSize = 20\.sp[^\n]*""").findAll(page).toList()
    assertTrue("el título de cada losa va en Rubik", titles.isNotEmpty() && titles.all { "TitleFamily" in it.value })
    assertTrue("el título de la página, en Rubik", Regex("""OnBoardText\(TimeTrialMarks\.TITLE[^\n]*""").find(page)!!.value.contains("TitleFamily"))
    // letra: solo los dos rótulos del eje bajan de 16 sp
    val small = Regex("""fontSize = (\d+(?:\.\d+)?)\.sp""").findAll(page).filter { it.groupValues[1].toFloat() < 16f }.map { it.groupValues[1] }.toList()
    assertTrue("solo el rótulo del eje (los dos textos y su medida), a 14 sp", small.isNotEmpty() && small.all { it == "14" })
    assertEquals("y están todos en la función del eje", small.size, Regex("""AxisLabels\(labels[\s\S]*?
\}""").find(page)!!.value.let { fn -> Regex("""fontSize = 14\.sp""").findAll(fn).count() })
  }
}
