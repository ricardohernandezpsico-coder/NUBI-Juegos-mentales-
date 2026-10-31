package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.luminance
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
import com.example.data.Comparison
import com.example.data.NBSP
import com.example.data.SampleComparison
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialTrials
import com.example.model.AgeRange
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.theme.Clay
import com.example.ui.theme.NubiTokens
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
  /** El registro de una partida: por cada medio minuto, [hits] aciertos y luego [errors] errores, en el paso [levels] de la pista (el mismo de los dos). Las horas crecen dentro de cada medio minuto. */
  private fun paceTrials(hits: List<Int>, errors: List<Int>, levels: List<Int> = listOf(1, 2, 4, 4)): TimeTrialTrials {
    val t = mutableListOf<Int>(); val ok = mutableListOf<Int>(); val lv = mutableListOf<Int>()
    for (h in 0..3) {
      val n = hits[h] + errors[h]
      for (k in 0 until n) { t += h * 30_000 + 500 + k * (28_000 / maxOf(n, 1)); ok += if (k < hits[h]) 1 else 0; lv += levels[h] }
    }
    return TimeTrialTrials.of(t, ok, lv)!!
  }

  /** La partida de hoy (43 de marca) con el ritmo de la lámina de Fable: 11, 14, 12 y 9 aciertos por medio minuto, 1 error en el tercero y 2 en el último, todos en el paso 4 de la pista. */
  private val lastGame = r(43, 28, 3).copy(trials = paceTrials(listOf(11, 14, 12, 9), listOf(0, 0, 1, 2)))
  private val ten = listOf(r(31, 8), r(35, 10), r(39, 13), r(36, 15), r(44, 17), r(38, 19, 3), r(41, 22, 3), r(37, 26, 3), r(42, 27, 3), lastGame)
  private val others = listOf(r(31, 21, game = "disparate"), r(27, 23, game = "radar"), r(19, 24, game = "acoplamiento"), r(38, 12, errors = 0, version = 1))

  private var density = 1f
  private var selectedCalls = mutableListOf<String>()
  private var backCalls = 0

  private fun show(widthDp: Int = 393, scale: Float = 1f, results: List<TimeTrialResult> = ten + others, selected: String = "stroop", compare: Comparison.Source? = null, ageRange: AgeRange? = null) {
    rule.setContent {
      val base = LocalDensity.current
      density = base.density
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(873.dp)) {
            BoardBackground()
            ContrarrelojPage(results = results, selected = selected, onSelect = { selectedCalls += it }, onBack = { backCalls += 1 }, now = now, lang = com.example.model.AppLanguage.SPANISH, compare = compare, ageRange = ageRange)
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
  private val rhythm = listOf("contrarreloj_rhythm_(title|legend|sentence|wait)", "contrarreloj_rhythm_(fig|bar)_(hit|err)_\\d", "contrarreloj_rhythm_axis_(\\d|unit)")
  private val cards = listOf("contrarreloj_record", "contrarreloj_rhythm", "contrarreloj_weeks", "contrarreloj_split", "contrarreloj_older")
  private val games = listOf("stroop", "disparate", "radar", "acoplamiento")

  private fun groupOf(map: Map<String, Rect>, patterns: List<String>) = map.filterKeys { k -> patterns.any { Regex(it).matches(k) } }

  private fun assertNoClashes(widthDp: Int, scale: Float) {
    show(widthDp, scale)
    val all = boxes()
    val label = "a $widthDp dp con letra $scale"
    val groups = mutableMapOf("la cabecera" to groupOf(all, header), "Tu récord" to groupOf(all, record), "Tu ritmo" to groupOf(all, rhythm), "Esta semana y la pasada" to groupOf(all, weeks), "Aciertos y errores" to groupOf(all, split), "Pistas anteriores" to groupOf(all, older))
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
    assertNull("sin marcas en el juego no hay losa de ritmo", b["contrarreloj_rhythm"])
    assertNotNull("la nota de siempre sigue al pie", b["contrarreloj_note"])
    assertEquals("sin marca aún", textOf("contrarreloj_game_value_radar"))
  }

  // ---------------------------------------------------------------- Tarea 201: orden, líneas, ventanilla noche y «Tu ritmo»

  @Test fun el_orden_de_las_losas_es_selector_record_ritmo_semana_aciertos_y_pistas() {
    show()
    val b = boxes()
    assertTrue("el selector va arriba de «Tu récord»", b.getValue("contrarreloj_game_radar").b < b.getValue("contrarreloj_record").y)
    for ((a, c) in listOf("contrarreloj_record", "contrarreloj_rhythm", "contrarreloj_weeks", "contrarreloj_split", "contrarreloj_older").zipWithNext()) {
      assertTrue("«$a» va antes que «$c»", b.getValue(a).b < b.getValue(c).y)
      assertEquals("y 16 dp de aire entre las dos losas", 16f, b.getValue(c).y - b.getValue(a).b, 1f)
    }
    assertNull("todavía no hay rótulos de sección (llegan con la comparación, Tarea 202)", b["contrarreloj_section_marks"])
  }

  @Test fun las_lineas_de_separacion_van_donde_una_losa_junta_dos_ideas_y_en_ningun_otro_lado() {
    show()
    val b = boxes()
    val dividers = b.keys.filter { it.startsWith("contrarreloj_divider_") }.toSet()
    assertEquals(
      "cuatro lugares: Tu récord, las dos semanas y los dos grupos de Aciertos y errores; no en Tu ritmo ni en Pistas anteriores",
      setOf("contrarreloj_divider_record", "contrarreloj_divider_weeks", "contrarreloj_divider_split_record", "contrarreloj_divider_split_recent"), dividers
    )
    for (d in dividers) {
      val line = b.getValue(d)
      assertEquals("«$d» mide 1 dp", 1f, line.h, 0.1f)
      val card = b.getValue(if (d.endsWith("_record") && "split" !in d) "contrarreloj_record" else if ("weeks" in d) "contrarreloj_weeks" else "contrarreloj_split")
      assertEquals("«$d» va de margen a margen de la losa (16 dp de relleno)", card.x + 16f, line.x, 0.6f)
      assertEquals(card.r - 16f, line.r, 0.6f)
    }
    // 16 dp de aire arriba y abajo (medido a las cajas de lo que separa)
    fun air(d: String, above: String, aboveGap: Float, below: String) {
      assertEquals("«$d»: aire sobre la línea", aboveGap, b.getValue(d).y - b.getValue(above).b, 1f)
      assertEquals("«$d»: aire bajo la línea", 16f, b.getValue(below).y - b.getValue(d).b, 1f)
    }
    air("contrarreloj_divider_record", "contrarreloj_record_step", 16f, "contrarreloj_chart_title")
    air("contrarreloj_divider_weeks", "contrarreloj_week_this_1", 12f, "contrarreloj_week_last_title")      // la última fila de la semana ya trae 4 dp de aire propio: 12 + 4 = 16 a la vista
    air("contrarreloj_divider_split_record", "contrarreloj_split_record_errors", 16f, "contrarreloj_split_recent_title")
    air("contrarreloj_divider_split_recent", "contrarreloj_split_recent_errors", 16f, "contrarreloj_rule")
  }

  @Test fun sin_el_segundo_grupo_de_aciertos_y_errores_queda_una_sola_linea_antes_de_la_regla() {
    show(results = listOf(r(31, 28)))
    val b = boxes()
    assertEquals(setOf("contrarreloj_divider_record", "contrarreloj_divider_weeks", "contrarreloj_divider_split_record"), b.keys.filter { it.startsWith("contrarreloj_divider_") }.toSet())
    assertTrue("la línea queda entre el récord y la regla", b.getValue("contrarreloj_divider_split_record").y > b.getValue("contrarreloj_split_record_errors").b && b.getValue("contrarreloj_divider_split_record").b < b.getValue("contrarreloj_rule").y)
  }

  @Test fun sin_marcas_en_el_juego_no_hay_ninguna_linea() {
    show(selected = "radar", results = listOf(r(31, 28)))
    assertEquals(emptyList<String>(), boxes().keys.filter { it.startsWith("contrarreloj_divider_") })
  }

  @Test fun los_graficos_de_barras_verticales_van_en_la_ventanilla_noche_con_12_dp_de_relleno() {
    show()
    val b = boxes()
    val marks = Regex("contrarreloj_(figure_\\d+|barshape_\\d+|coin|axis_(left|right)|baseline)")
    val pace = Regex("contrarreloj_rhythm_((fig|bar)_(hit|err)_\\d|axis_(\\d|unit)|baseline)")
    for ((win, pieces) in listOf("contrarreloj_chart_window" to marks, "contrarreloj_rhythm_window" to pace)) {
      val w = b.getValue(win)
      assertEquals("«$win» llena el ancho de la losa menos su relleno de 16 dp", 393f - 64f, w.w, 1f)
      val inside = b.filterKeys { pieces.matches(it) }
      assertTrue("«$win» tiene piezas adentro", inside.size >= 8)
      for ((tag, p) in inside) assertTrue("«$tag» queda a 12 dp o más del borde de la ventanilla", p.x >= w.x + 11.5f && p.r <= w.r - 11.5f && p.y >= w.y + 11.5f && p.b <= w.b - 11.5f)
    }
    // las barras horizontales de «Aciertos y errores» siguen en crema, fuera de toda ventanilla
    val splitBar = b.getValue("contrarreloj_split_record_hits_bar")
    for (win in listOf("contrarreloj_chart_window", "contrarreloj_rhythm_window")) assertTrue("las barras de Aciertos y errores no están en «$win»", splitBar.y > b.getValue(win).b || splitBar.b < b.getValue(win).y)
  }

  @Test fun tu_ritmo_dice_su_titulo_su_leyenda_y_la_frase_de_la_lamina() {
    show()
    assertEquals("Tu ritmo en la última partida", textOf("contrarreloj_rhythm_title"))
    assertEquals("Barras lila: aciertos. Barras coral: errores.", textOf("contrarreloj_rhythm_legend"))
    assertEquals("Tu mejor medio minuto fue el de 30 a 60 segundos, con 14 aciertos. Tus 3 errores llegaron en el último minuto, en el paso 4 de la pista.", textOf("contrarreloj_rhythm_sentence").replace(NBSP, " "))
    val b = boxes()
    assertTrue("la leyenda va bajo el gráfico y la frase bajo la leyenda", b.getValue("contrarreloj_rhythm_window").b < b.getValue("contrarreloj_rhythm_legend").y && b.getValue("contrarreloj_rhythm_legend").b < b.getValue("contrarreloj_rhythm_sentence").y)
    assertEquals("cuatro rótulos de eje, uno por medio minuto", 4, b.keys.count { Regex("contrarreloj_rhythm_axis_\\d").matches(it) })
    val unit = b.getValue("contrarreloj_rhythm_axis_unit")
    assertTrue("y la unidad, escrita («segundos»), una sola vez bajo los cuatro rótulos", (0..3).all { b.getValue("contrarreloj_rhythm_axis_$it").b <= unit.y } && unit.y > b.getValue("contrarreloj_rhythm_baseline").b)
    assertTrue("la unidad queda al centro de la ventanilla", Math.abs((unit.x + unit.r) / 2 - (b.getValue("contrarreloj_rhythm_window").x + b.getValue("contrarreloj_rhythm_window").r) / 2) < 1f)
  }

  @Test fun el_grafico_del_ritmo_son_cuatro_columnas_con_las_cifras_en_una_fila_y_una_base_comun() {
    show()
    val b = boxes()
    assertEquals("cuatro barras de aciertos", 4, b.keys.count { it.startsWith("contrarreloj_rhythm_bar_hit_") })
    assertEquals("solo dos de errores: un medio minuto sin errores no dibuja esa barra", setOf("contrarreloj_rhythm_bar_err_2", "contrarreloj_rhythm_bar_err_3"), b.keys.filter { it.startsWith("contrarreloj_rhythm_bar_err_") }.toSet())
    assertEquals("cuatro cifras de aciertos y dos de errores (el 0 de los errores no se escribe)", 4 to 2, b.keys.count { it.startsWith("contrarreloj_rhythm_fig_hit_") } to b.keys.count { it.startsWith("contrarreloj_rhythm_fig_err_") })
    val figs = b.filterKeys { it.startsWith("contrarreloj_rhythm_fig_") }.values
    assertTrue("las cifras van en UNA fila", figs.all { Math.abs(it.y - figs.first().y) < 0.5f })
    val bars = b.filterKeys { it.startsWith("contrarreloj_rhythm_bar_") }.values
    assertTrue("las barras parten de una base común", bars.all { Math.abs(it.b - bars.first().b) < 0.5f })
    assertTrue("bajo la base común va el eje, a 4 dp o más", (0..3).all { b.getValue("contrarreloj_rhythm_axis_$it").y >= bars.first().b + 4f })
    // la misma escala para aciertos y errores: 14 aciertos = el alto máximo (96 dp)
    val h = (0..3).map { b.getValue("contrarreloj_rhythm_bar_hit_$it").h }
    assertEquals(96f, h[1], 0.6f)
    assertEquals(96f * 11 / 14, h[0], 0.6f); assertEquals(96f * 12 / 14, h[2], 0.6f); assertEquals(96f * 9 / 14, h[3], 0.6f)
    assertEquals("2 errores a la misma escala", 96f * 2 / 14, b.getValue("contrarreloj_rhythm_bar_err_3").h, 0.6f)
    assertEquals("1 error serían 6,9 dp, pero una barra no baja de 8 dp", 8f, b.getValue("contrarreloj_rhythm_bar_err_2").h, 0.6f)
    // cada medio minuto: aciertos a la izquierda, errores a la derecha y más angostos; los medios minutos van de izquierda a derecha
    for (i in 2..3) assertTrue("el error del medio minuto $i va a la derecha de sus aciertos y es más angosto", b.getValue("contrarreloj_rhythm_bar_err_$i").x >= b.getValue("contrarreloj_rhythm_bar_hit_$i").r && b.getValue("contrarreloj_rhythm_bar_err_$i").w < b.getValue("contrarreloj_rhythm_bar_hit_$i").w)
    for (i in 0..2) assertTrue("los medios minutos van de izquierda a derecha", b.getValue("contrarreloj_rhythm_bar_hit_${i + 1}").x > b.getValue("contrarreloj_rhythm_bar_hit_$i").r)
    // el rótulo del eje va centrado bajo su columna (el par de barras): entre las barras de aciertos de su medio minuto y del siguiente
    for (i in 0..3) {
      val axis = b.getValue("contrarreloj_rhythm_axis_$i"); val center = (axis.x + axis.r) / 2; val hit = b.getValue("contrarreloj_rhythm_bar_hit_$i")
      val nextHit = b["contrarreloj_rhythm_bar_hit_${i + 1}"]
      assertTrue("el rótulo del eje $i queda bajo su columna", center >= hit.x && (nextHit == null || center < nextHit.x) && center - hit.x < 76f)
    }
  }

  @Test fun el_lector_de_pantalla_dice_el_ritmo_medio_minuto_por_medio_minuto() {
    show()
    assertEquals(
      "De 0 a 30 segundos: 11 aciertos y 0 errores. De 30 a 60 segundos: 14 aciertos y 0 errores. De 60 a 90 segundos: 12 aciertos y 1 error. De 90 a 120 segundos: 9 aciertos y 2 errores.",
      descriptionOf("contrarreloj_rhythm_window")
    )
    assertEquals("el gráfico se dice una vez: las barras y las cifras sueltas no se leen aparte", 1, rule.onAllNodes(hasTestTag("contrarreloj_rhythm_window")).fetchSemanticsNodes().size)
  }

  @Test fun con_marcas_pero_sin_ritmo_guardado_la_losa_solo_dice_cuando_aparece() {
    show(results = ten.dropLast(1) + r(43, 28, 3) + others)
    assertEquals("Tu ritmo en la última partida", textOf("contrarreloj_rhythm_title"))
    assertEquals("Tu ritmo aparece desde tu próxima partida del Contrarreloj.", textOf("contrarreloj_rhythm_wait"))
    val b = boxes()
    assertEquals("sin gráfico, sin leyenda y sin frase", 0, b.keys.count { it.startsWith("contrarreloj_rhythm_bar_") || it.startsWith("contrarreloj_rhythm_fig_") || it.startsWith("contrarreloj_rhythm_axis_") })
    assertNull(b["contrarreloj_rhythm_window"]); assertNull(b["contrarreloj_rhythm_legend"]); assertNull(b["contrarreloj_rhythm_sentence"])
    assertNotNull("y la página sigue entera: la semana y los aciertos están", b["contrarreloj_weeks"]); assertNotNull(b["contrarreloj_split"])
  }

  @Test fun el_ritmo_es_el_del_juego_elegido_y_con_cuatro_medios_minutos_parejos_no_resalta_ninguno() {
    val even = r(20, 27, 0, game = "disparate").copy(trials = paceTrials(listOf(5, 5, 5, 5), listOf(0, 0, 0, 0)))
    show(selected = "disparate", results = ten + others + even)
    assertEquals("Los cuatro medios minutos fueron parejos, con 5 aciertos cada uno. Sin errores en esta partida.", textOf("contrarreloj_rhythm_sentence").replace(NBSP, " "))
    val b = boxes()
    assertEquals("sin errores no hay barras coral", 0, b.keys.count { it.startsWith("contrarreloj_rhythm_bar_err_") })
    assertEquals(4, b.keys.count { it.startsWith("contrarreloj_rhythm_bar_hit_") })
    assertTrue("las cuatro barras miden igual", (0..3).all { Math.abs(b.getValue("contrarreloj_rhythm_bar_hit_$it").h - 96f) < 0.6f })
  }

  /** El peor caso de «Tu ritmo»: 18 errores en un medio minuto y 48 aciertos en otro (cifras de dos dígitos en las dos barras de una columna), con 4 dp o más entre cada par de piezas y todo dentro del relleno de la ventanilla. */
  private fun assertRhythmNoClashes(widthDp: Int, scale: Float) {
    val heavy = r(70, 28, 20).copy(trials = paceTrials(listOf(48, 12, 20, 10), listOf(18, 0, 1, 1), levels = listOf(1, 3, 4, 4)))
    show(widthDp = widthDp, scale = scale, results = ten.dropLast(1) + heavy + others)
    val b = boxes()
    val pieces = groupOf(b, rhythm)
    val keys = pieces.keys.toList()
    val problems = mutableListOf<String>()
    for (i in keys.indices) for (j in i + 1 until keys.size) { val s = gap(pieces.getValue(keys[i]), pieces.getValue(keys[j])); if (s < 3.5f) problems += "«${keys[i]}» y «${keys[j]}» a %.1f dp".format(s) }
    val w = b.getValue("contrarreloj_rhythm_window")
    for ((tag, p) in pieces) if (tag.startsWith("contrarreloj_rhythm_fig") || tag.startsWith("contrarreloj_rhythm_bar") || tag.startsWith("contrarreloj_rhythm_axis")) if (p.x < w.x + 11.5f || p.r > w.r - 11.5f) problems += "«$tag» se sale del relleno de la ventanilla"
    assertTrue("a $widthDp dp con letra $scale:\n" + problems.joinToString("\n"), problems.isEmpty())
  }

  @Test fun tu_ritmo_en_el_peor_caso_cero_choques_a_393_con_letra_1_0() = assertRhythmNoClashes(393, 1f)
  @Test fun tu_ritmo_en_el_peor_caso_cero_choques_a_393_con_letra_1_3() = assertRhythmNoClashes(393, 1.3f)
  @Test fun tu_ritmo_en_el_peor_caso_cero_choques_a_393_con_letra_2_0() = assertRhythmNoClashes(393, 2f)
  @Test fun tu_ritmo_en_el_peor_caso_cero_choques_a_360_con_letra_1_0() = assertRhythmNoClashes(360, 1f)
  @Test fun tu_ritmo_en_el_peor_caso_cero_choques_a_360_con_letra_2_0() = assertRhythmNoClashes(360, 2f)

  @Test fun los_textos_sobre_la_ventanilla_noche_pasan_4_5_a_1() {
    fun contrast(a: Color, c: Color): Double { val la = a.luminance().toDouble(); val lc = c.luminance().toDouble(); return (maxOf(la, lc) + 0.05) / (minOf(la, lc) + 0.05) }
    val w = NubiTokens.NightWindow
    for ((name, c) in listOf("las cifras (OnNight)" to NubiTokens.OnNight, "el eje (OnNightDim)" to NubiTokens.OnNightDim, "la cifra resaltada (SunOnNight)" to NubiTokens.SunOnNight, "las cifras de los errores (coral)" to Clay.Coral)) {
      assertTrue("$name sobre la ventanilla: %.1f a 1".format(contrast(c, w)), contrast(c, w) >= 4.5)
    }
    for ((name, c) in listOf("las barras lila" to Clay.Grape, "las barras coral" to Clay.Coral, "la barra del récord (sol)" to Clay.Sun)) {
      assertTrue("$name se separan de la ventanilla (3 a 1 o más): %.1f".format(contrast(c, w)), contrast(c, w) >= 3.0)
    }
    println("T201 contraste sobre la ventanilla: OnNight %.1f · OnNightDim %.1f · SunOnNight %.1f · coral %.1f · lila %.1f".format(contrast(NubiTokens.OnNight, w), contrast(NubiTokens.OnNightDim, w), contrast(NubiTokens.SunOnNight, w), contrast(Clay.Coral, w), contrast(Clay.Grape, w)))
  }

  // ---------------------------------------------------------------- Tarea 202: la comparación con personas de tu edad (datos de muestra, solo en depuración)

  private val manyWorld = SampleComparison(few = false)
  private val fewWorld = SampleComparison(few = true)

  @Test fun sin_comparacion_la_pagina_queda_como_en_la_t201_sin_rotulos_de_seccion() {
    show(compare = null, ageRange = AgeRange.R45_54)          // la build de la tienda: ninguna fuente aunque la persona tenga franja
    val b = boxes()
    for (tag in listOf("contrarreloj_section_marks", "contrarreloj_section_compare", "contrarreloj_sample_notice", "contrarreloj_place", "contrarreloj_percweeks", "contrarreloj_others"))
      assertNull("sin comparación no existe «$tag»", b[tag])
    assertNotNull("y lo de siempre sigue", b["contrarreloj_rhythm"])
  }

  @Test fun con_datos_de_muestra_la_pagina_tiene_dos_secciones_con_su_rotulo_y_el_aviso_de_muestra() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertEquals("Tus marcas", textOf("contrarreloj_section_marks"))
    assertEquals("Comparado con personas de tu edad", textOf("contrarreloj_section_compare"))
    assertEquals("Estos son datos de muestra: solo se ven en la versión de prueba.", textOf("contrarreloj_sample_notice"))
    val order = listOf("contrarreloj_game_radar", "contrarreloj_section_marks", "contrarreloj_record", "contrarreloj_rhythm", "contrarreloj_weeks", "contrarreloj_split", "contrarreloj_older", "contrarreloj_section_compare", "contrarreloj_sample_notice", "contrarreloj_place", "contrarreloj_percweeks", "contrarreloj_others")
    for ((a, c) in order.zipWithNext()) assertTrue("«$a» va antes que «$c»", b.getValue(a).b <= b.getValue(c).y + 0.5f)
    assertEquals("24 dp de aire sobre el rótulo de la sección: 16 de entre losas y 8 propios", 24f, b.getValue("contrarreloj_section_marks").y - b.getValue("contrarreloj_game_radar").b, 1.5f)
    for (key in listOf("marks", "compare")) {
      val label = b.getValue("contrarreloj_section_$key"); val line = b.getValue("contrarreloj_section_${key}_line")
      assertEquals("la línea mide 1 dp", 1f, line.h, 0.1f)
      assertTrue("y va desde el final del texto hasta el margen derecho (16 dp)", line.x >= label.r + 11.5f && Math.abs(line.r - (393f - 16f)) < 1f)
    }
    assertEquals("el aviso de muestra va bajo el rótulo, a 8 dp", 8f, b.getValue("contrarreloj_sample_notice").y - b.getValue("contrarreloj_section_compare").b, 1.5f)
  }

  @Test fun el_rotulo_de_seccion_y_la_nota_de_muestra_van_en_rubik_y_en_crema_de_16_sp_en_el_codigo() {
    val page = src("ui/screens/ContrarrelojPage.kt")
    assertTrue("el rótulo va en Rubik 17 sp con OnNight", Regex("""Text\(text, color = NubiTokens\.OnNight, fontSize = 17\.sp, fontWeight = FontWeight\.Bold, fontFamily = TitleFamily""").containsMatchIn(page))
    assertTrue("la línea del rótulo es OnNight al 35 %", page.contains("NubiTokens.OnNight.copy(alpha = 0.35f)"))
    assertTrue("la nota de muestra, en OnNight de 16 sp", Regex("""Text\(Comparison\.SAMPLE_NOTICE, color = NubiTokens\.OnNight, fontSize = 16\.sp""").containsMatchIn(page))
    assertTrue("el recuadro de la frase usa el lila de «Marca nueva» (TimeTrialDoneFill), sin copiar el literal", page.contains("background(TimeTrialDoneFill)") && !Regex("0x[Ff][Ff][Dd]9[Cc][Ee][Ff][Ff]").containsMatchIn(page))
  }

  @Test fun tu_lugar_dice_la_frase_de_la_lamina_el_percentil_con_su_division_y_las_dos_frases_de_la_campana() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("Tu lugar esta semana", textOf("contrarreloj_place_title"))
    assertEquals("Tu mejor marca de esta semana, 43 aciertos, supera a 63 de cada 100 personas de 45 a 54 años que jugaron Tinta o Palabra esta semana.", textOf("contrarreloj_place_main").replace(NBSP, " "))
    assertEquals("Es el percentil 63: la división En órbita.", textOf("contrarreloj_place_percentile"))
    assertEquals("La mitad de las personas hizo 39 aciertos o menos; la cuarta parte más alta, 47 o más. La línea marca la mitad.", textOf("contrarreloj_place_quartiles"))
    assertEquals("Hecha con las marcas de las 128 personas tal como son, no con una curva teórica.", textOf("contrarreloj_place_madeof"))
    val b = boxes()
    assertNull("la clase normal no dice «Juntamos dos franjas»", b["contrarreloj_place_scope"])
    val main = b.getValue("contrarreloj_place_main")
    assertEquals("el recuadro de la frase tiene 12 dp de relleno (la caja llena el ancho de la losa)", 393f - 64f, main.w, 1f)
    assertTrue("el percentil va debajo de la frase y la línea de separación después del percentil", b.getValue("contrarreloj_place_percentile").y >= main.b + 8f && b.getValue("contrarreloj_divider_place").y > b.getValue("contrarreloj_place_percentile").b)
    assertTrue("la campana va tras la línea, y sus dos frases debajo", b.getValue("contrarreloj_bell_window").y > b.getValue("contrarreloj_divider_place").b && b.getValue("contrarreloj_place_quartiles").y > b.getValue("contrarreloj_bell_window").b && b.getValue("contrarreloj_place_madeof").y > b.getValue("contrarreloj_place_quartiles").b)
    assertTrue("el recuadro mide al menos una línea de 22 dp más sus 12 dp de relleno arriba y abajo", main.h >= 46f)
  }

  @Test fun la_campana_dibuja_tu_barra_sol_hasta_la_curva_con_la_moneda_y_la_palabra_encima_y_la_linea_de_la_mitad() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    val win = b.getValue("contrarreloj_bell_window"); val bar = b.getValue("contrarreloj_bell_you_bar"); val area = b.getValue("contrarreloj_bell_area")
    val median = b.getValue("contrarreloj_bell_median"); val coin = b.getValue("contrarreloj_bell_coin"); val you = b.getValue("contrarreloj_bell_you")
    assertEquals("la ventanilla llena el ancho de la losa", 393f - 64f, win.w, 1f)
    assertEquals("la barra Tú mide 6 dp de ancho", 6f, bar.w, 0.1f)
    assertEquals("y parte de la base de la curva", area.b, bar.b, 0.6f)
    // la barra llega a la curva: la misma función que la dibuja
    val smooth = Comparison.smoothed(SampleComparison.BASE)
    val peak = smooth.max()
    val expected = 84f * (Comparison.bellHeight(smooth, Comparison.markX(43)) / peak).toFloat()
    assertEquals("la barra llega justo a la curva en 43 aciertos", expected, bar.h, 0.8f)
    assertEquals("43,5 de 80 del ancho: el centro de su marca", area.x + area.w * (43.5f / 80f), bar.x + bar.w / 2, 1f)
    assertTrue("la línea de la mitad (39 aciertos) queda a la izquierda de «Tú» (43)", median.x + median.w < bar.x)
    assertEquals("la línea de la mitad mide 1,5 dp (a la rejilla de píxeles, 1,7)", 1.5f, median.w, 0.3f)
    assertEquals("de 39 aciertos", area.x + area.w * (39.5f / 80f), median.x + median.w / 2, 1f)
    assertTrue("la moneda queda sobre la barra, a 4 dp o más", coin.b <= bar.y - 3.5f && Math.abs((coin.x + coin.r) / 2 - (bar.x + bar.r) / 2) < 1f)
    assertTrue("y la palabra «Tú» sobre la moneda", you.b <= coin.y - 3.5f)
    assertTrue("todo dentro de la ventanilla con 12 dp de relleno", listOf(area, bar, median, coin, you).all { it.x >= win.x + 11.5f && it.r <= win.r - 11.5f && it.y >= win.y + 11.5f && it.b <= win.b - 11.5f })
    for (i in 0..3) b["contrarreloj_bell_axis_$i"]?.let { assertTrue("el rótulo $i del eje queda bajo la base", it.y >= area.b + 3.5f && it.b <= win.b - 11.5f) }
    assertNotNull("el eje trae al menos «0 aciertos» y «75 o más»", b["contrarreloj_bell_axis_0"]); assertNotNull(b["contrarreloj_bell_axis_3"])
    assertEquals("la campana se lee una vez, con una descripción corta", "Campana con las marcas de las 128 personas. Tu mejor marca de esta semana, 43 aciertos, está marcada.", descriptionOf("contrarreloj_bell_window"))
  }

  @Test fun el_eje_de_la_campana_omite_un_rotulo_que_no_deja_8_dp_al_anterior() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54, scale = 2f)
    val b = boxes()
    val shown = (0..3).mapNotNull { i -> b["contrarreloj_bell_axis_$i"]?.let { i to it } }
    assertTrue("con letra 2,0 siguen los dos extremos", shown.any { it.first == 0 } && shown.any { it.first == 3 })
    for (i in shown.indices) for (j in i + 1 until shown.size) assertTrue("los rótulos ${shown[i].first} y ${shown[j].first} dejan 8 dp o más", gap(shown[i].second, shown[j].second) >= 7.5f)
  }

  @Test fun tu_percentil_en_las_ultimas_7_semanas_dibuja_las_semanas_con_marca_de_la_mas_vieja_a_esta() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("Tu percentil en las últimas 7 semanas", textOf("contrarreloj_percweeks_title").replace(NBSP, " "))
    val b = boxes()
    val mine = com.example.data.TimeTrialDay.ofGame(ten + others, "stroop")
    val bests = List(7) { Comparison.bestOfWeek(mine, now, it) }
    val points = Comparison.weekPoints(manyWorld, "stroop", AgeRange.R45_54, bests)
    assertEquals("con las marcas de la T199 hay 4 semanas: hace 3, 2, 1 y esta", listOf(3, 2, 1, 0), points.map { it.weeksAgo })
    assertEquals(points.map { "contrarreloj_percweeks_bar_${it.weeksAgo}" }.toSet(), b.keys.filter { it.startsWith("contrarreloj_percweeks_bar_") }.toSet())
    assertEquals(points.map { "contrarreloj_percweeks_fig_${it.weeksAgo}" }.toSet(), b.keys.filter { it.startsWith("contrarreloj_percweeks_fig_") }.toSet())
    val xs = points.map { b.getValue("contrarreloj_percweeks_bar_${it.weeksAgo}").x }
    assertEquals("de la más vieja a esta semana, de izquierda a derecha", xs.sorted(), xs)
    val figs = b.filterKeys { it.startsWith("contrarreloj_percweeks_fig_") }.values
    assertTrue("las cifras van en UNA fila", figs.all { Math.abs(it.y - figs.first().y) < 0.5f })
    val bars = b.filterKeys { it.startsWith("contrarreloj_percweeks_bar_") }.values
    assertTrue("las barras parten de una base común", bars.all { Math.abs(it.b - bars.first().b) < 0.5f })
    for (pt in points) assertEquals("la barra de la semana ${pt.weeksAgo} mide su percentil de 0 a 100", Math.max(96f * pt.percentile / 100f, 8f), b.getValue("contrarreloj_percweeks_bar_${pt.weeksAgo}").h, 0.7f)
    assertEquals("la de esta semana es la última de las siete", true, b.getValue("contrarreloj_percweeks_bar_0").x > b.getValue("contrarreloj_percweeks_bar_1").x)
    assertEquals(Comparison.weeksSentence(points), textOf("contrarreloj_percweeks_sentence"))
    assertEquals(Comparison.weeksSpoken(points), descriptionOf("contrarreloj_percweeks_window"))
    assertNotNull(b["contrarreloj_percweeks_axis_left"]); assertNotNull(b["contrarreloj_percweeks_axis_right"])
  }

  @Test fun comparado_con_otras_edades_trae_seis_franjas_sin_13_a_17_la_propia_en_negrita_y_todas_las_edades() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("Comparado con otras edades", textOf("contrarreloj_others_title"))
    assertEquals("Tu mejor marca de esta semana, 43 aciertos, supera a tantas de cada 100 personas que jugaron Tinta o Palabra esta semana:", textOf("contrarreloj_others_lead").replace(NBSP, " "))
    val labels = (0..5).map { textOf("contrarreloj_others_${it}_label") }
    assertEquals(listOf("18 a 29 años", "30 a 44 años", "45 a 54 años (tu franja)", "55 a 64 años", "65 a 74 años", "75 años o más"), labels)
    assertTrue("ninguna fila de 13 a 17 años", labels.none { "13 a 17" in it })
    assertEquals("63 de cada 100", textOf("contrarreloj_others_2_value").replace(NBSP, " "))
    assertEquals("la de 75 o más tiene menos de 50 personas", "todavía somos pocos", textOf("contrarreloj_others_5_value"))
    assertEquals("Todas las edades", textOf("contrarreloj_others_all_label"))
    assertEquals("De 13 a 17 años no hay comparación por ahora.", textOf("contrarreloj_others_minors"))
    val b = boxes()
    assertTrue("una línea entre las franjas y «Todas las edades»", b.getValue("contrarreloj_divider_others").y > b.getValue("contrarreloj_others_5").b && b.getValue("contrarreloj_divider_others").b < b.getValue("contrarreloj_others_all").y)
    assertTrue("la línea de 13 a 17 años va al final", b.getValue("contrarreloj_others_minors").y > b.getValue("contrarreloj_others_all").b)
    assertEquals("45 a 54 años (tu franja), 63 de cada 100", descriptionOf("contrarreloj_others_2"))
  }

  @Test fun en_el_mundo_de_pocos_sale_todavia_somos_pocos_con_la_cuenta_la_barra_y_mientras_tanto() {
    show(compare = fewWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertEquals("Todavía somos pocos: 23 personas de 45 a 54 años jugaron Tinta o Palabra esta semana. Cuando sean 50, verás aquí tu lugar entre ellas.", textOf("contrarreloj_few_note").replace(NBSP, " "))
    assertEquals("23 de 50 personas", textOf("contrarreloj_few_bar_value").replace(NBSP, " "))
    assertTrue("«Mientras tanto…» con todas las edades de las últimas 4 semanas", textOf("contrarreloj_few_meanwhile").replace(NBSP, " ").startsWith("Mientras tanto, comparada con todas las edades de las últimas 4 semanas, tu mejor marca de esta semana, 43 aciertos, supera a "))
    assertNull("sin campana ni percentil: no hay clase de 50", b["contrarreloj_bell_window"]); assertNull(b["contrarreloj_place_percentile"])
    assertNull("ninguna semana de la franja llega a 50: no hay percentil semana a semana", b["contrarreloj_percweeks"])
    assertTrue("la barra llena menos de la mitad (23 de 50)", b.getValue("contrarreloj_few_bar_bar").w < 0.6f * b.getValue("contrarreloj_few_bar").w)
    assertTrue("una línea separa la barra de «Mientras tanto»", b.getValue("contrarreloj_divider_few").y > b.getValue("contrarreloj_few_bar").b && b.getValue("contrarreloj_divider_few").b < b.getValue("contrarreloj_few_meanwhile").y)
    assertEquals("todavía somos pocos", textOf("contrarreloj_others_2_value"))
  }

  @Test fun sin_marca_esta_semana_la_losa_dice_que_hacer_y_no_hay_otras_edades_ni_semanas_de_hoy() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54, selected = "radar", results = listOf(r(27, 23, game = "radar")))
    assertEquals("Todavía no tienes una marca esta semana en Rescate relámpago. Juega su Contrarreloj para ver tu lugar entre personas de tu edad.", textOf("contrarreloj_place_nomark"))
    val b = boxes()
    assertNull("sin marca esta semana no hay otras edades ni campana", b["contrarreloj_others"]); assertNull(b["contrarreloj_bell_window"])
    assertNotNull("pero las semanas anteriores con marca sí se cuentan (la del 23 de octubre)", b["contrarreloj_percweeks"])
  }

  @Test fun quien_tiene_de_13_a_17_anos_no_ve_la_comparacion_ni_con_datos_de_muestra() {
    show(compare = manyWorld, ageRange = AgeRange.R13_17)
    val b = boxes()
    for (tag in listOf("contrarreloj_section_marks", "contrarreloj_section_compare", "contrarreloj_sample_notice", "contrarreloj_place", "contrarreloj_percweeks", "contrarreloj_others"))
      assertNull("de 13 a 17 años no existe «$tag»", b[tag])
  }

  @Test fun sin_franja_elegida_los_datos_de_muestra_usan_45_a_54_y_con_datos_reales_no_hay_comparacion() {
    show(compare = manyWorld, ageRange = null)
    assertEquals("Tu mejor marca de esta semana, 43 aciertos, supera a 63 de cada 100 personas de 45 a 54 años que jugaron Tinta o Palabra esta semana.", textOf("contrarreloj_place_main").replace(NBSP, " "))
  }

  @Test fun una_franja_con_pocas_personas_se_junta_con_su_vecina_y_lo_dice() {
    // 55 a 64 años (96 personas esta semana: alcanza). 75 o más (38): no alcanza → sus 4 semanas (~160) sí
    show(compare = manyWorld, ageRange = AgeRange.R75_PLUS)
    val main = textOf("contrarreloj_place_main").replace(NBSP, " ")
    assertTrue("75 o más: sus últimas 4 semanas — «$main»", main.contains("personas de 75 años o más que jugaron Tinta o Palabra en las últimas 4 semanas."))
    assertNull("la clase de 4 semanas de la propia franja no necesita decir que se juntaron franjas", boxes()["contrarreloj_place_scope"])
  }

  // ---- la prueba de cajas con la comparación: 4 dp entre cada par de piezas, nada fuera del margen y 8 dp entre losas

  private val place = listOf("contrarreloj_place_(title|main|percentile|scope|quartiles|madeof|nomark)", "contrarreloj_bell_(you|coin|you_bar|median)", "contrarreloj_bell_axis_\\d", "contrarreloj_few_(note|meanwhile)", "contrarreloj_few_bar_(bar|value)")
  private val weeksPercentile = listOf("contrarreloj_percweeks_(title|sentence)", "contrarreloj_percweeks_fig_\\d", "contrarreloj_percweeks_bar_\\d", "contrarreloj_percweeks_axis_(left|right)")
  private val othersGroup = listOf("contrarreloj_others_(title|lead|minors)", "contrarreloj_others_(\\d|all)_(label|value)")
  private val sections = listOf("contrarreloj_section_(marks|compare)", "contrarreloj_section_(marks|compare)_line", "contrarreloj_sample_notice")

  private fun assertCompareNoClashes(widthDp: Int, scale: Float, world: SampleComparison, label: String) {
    show(widthDp, scale, compare = world, ageRange = AgeRange.R45_54)
    val all = boxes()
    val problems = mutableListOf<String>()
    val groups = mapOf("Tu lugar" to groupOf(all, place), "Tu percentil en las últimas 7 semanas" to groupOf(all, weeksPercentile), "Comparado con otras edades" to groupOf(all, othersGroup), "los rótulos de sección" to groupOf(all, sections))
    for ((name, g) in groups) {
      val keys = g.keys.toList()
      for (i in keys.indices) for (j in i + 1 until keys.size) {
        val s = gap(g.getValue(keys[i]), g.getValue(keys[j]))
        if (s < 3.5f) problems += "$name: «${keys[i]}» y «${keys[j]}» a %.1f dp".format(s)
      }
    }
    for ((tag, b) in all) if (tag.startsWith("contrarreloj_") && tag !in listOf("contrarreloj_page", "contrarreloj_scroll", "contrarreloj_header")) {
      if (b.x < 15.5f || b.r > widthDp - 15.5f) problems += "«$tag» se sale del margen de 16 dp (x %.1f a %.1f)".format(b.x, b.r)
    }
    val slabs = (games.map { "contrarreloj_game_$it" } + cards + listOf("contrarreloj_place", "contrarreloj_percweeks", "contrarreloj_others")).filter { it in all }
    for (i in slabs.indices) for (j in i + 1 until slabs.size) { val s = gap(all.getValue(slabs[i]), all.getValue(slabs[j])); if (s < 7.5f) problems += "las losas «${slabs[i]}» y «${slabs[j]}» a %.1f dp".format(s) }
    for ((win, pieces) in listOf("contrarreloj_bell_window" to Regex("contrarreloj_bell_(area|you|you_bar|median|coin|axis_\\d)"), "contrarreloj_percweeks_window" to Regex("contrarreloj_percweeks_(fig_\\d|bar_\\d|baseline|axis_(left|right))")))
      all[win]?.let { w -> for ((tag, p) in all) if (pieces.matches(tag) && (p.x < w.x + 11.5f || p.r > w.r - 11.5f || p.y < w.y + 11.5f || p.b > w.b - 11.5f)) problems += "«$tag» se sale del relleno de «$win»" }
    assertTrue("$label a $widthDp dp con letra $scale:\n" + problems.joinToString("\n"), problems.isEmpty())
  }

  @Test fun comparacion_cero_choques_a_393_con_letra_1_0() = assertCompareNoClashes(393, 1f, manyWorld, "muchos")
  @Test fun comparacion_cero_choques_a_393_con_letra_1_3() = assertCompareNoClashes(393, 1.3f, manyWorld, "muchos")
  @Test fun comparacion_cero_choques_a_393_con_letra_2_0() = assertCompareNoClashes(393, 2f, manyWorld, "muchos")
  @Test fun comparacion_cero_choques_a_360_con_letra_1_0() = assertCompareNoClashes(360, 1f, manyWorld, "muchos")
  @Test fun comparacion_cero_choques_a_360_con_letra_1_3() = assertCompareNoClashes(360, 1.3f, manyWorld, "muchos")
  @Test fun comparacion_cero_choques_a_360_con_letra_2_0() = assertCompareNoClashes(360, 2f, manyWorld, "muchos")
  @Test fun pocos_cero_choques_a_393_con_letra_1_0() = assertCompareNoClashes(393, 1f, fewWorld, "pocos")
  @Test fun pocos_cero_choques_a_393_con_letra_2_0() = assertCompareNoClashes(393, 2f, fewWorld, "pocos")
  @Test fun pocos_cero_choques_a_360_con_letra_1_3() = assertCompareNoClashes(360, 1.3f, fewWorld, "pocos")

  private fun compareHeight(widthDp: Int, scale: Float, world: SampleComparison = manyWorld): String {
    show(widthDp, scale, compare = world, ageRange = AgeRange.R45_54)
    val b = boxes()
    return "T202 alto de la página con comparación (%s) a %d con letra %s: %.0f dp; «Tu lugar» %.0f dp, las 7 semanas %s, otras edades %s".format(
      if (world === manyWorld) "muchos" else "pocos", widthDp, scale, b.getValue("contrarreloj_others").b + 32f, b.getValue("contrarreloj_place").h,
      b["contrarreloj_percweeks"]?.let { "%.0f dp".format(it.h) } ?: "no hay", b["contrarreloj_others"]?.let { "%.0f dp".format(it.h) } ?: "no hay"
    )
  }

  @Test fun el_alto_de_la_pagina_con_la_comparacion_a_393_con_letra_1_0() { println(compareHeight(393, 1f)) }
  @Test fun el_alto_de_la_pagina_con_la_comparacion_a_393_con_letra_1_3() { println(compareHeight(393, 1.3f)) }
  @Test fun el_alto_de_la_pagina_con_la_comparacion_a_393_con_letra_2_0() { println(compareHeight(393, 2f)) }
  @Test fun el_alto_de_la_pagina_con_la_comparacion_a_360_con_letra_1_0() { println(compareHeight(360, 1f)) }
  @Test fun el_alto_de_la_pagina_con_pocos_a_393_con_letra_1_0() { println(compareHeight(393, 1f, fewWorld)) }

  @Test fun el_texto_de_la_frase_pasa_4_5_a_1_sobre_el_lila_claro_de_marca_nueva() {
    fun contrast(a: Color, c: Color): Double { val la = a.luminance().toDouble(); val lc = c.luminance().toDouble(); return (maxOf(la, lc) + 0.05) / (minOf(la, lc) + 0.05) }
    val fill = com.example.ui.screens.TimeTrialDoneFill
    assertEquals(0xD9CEFF, fill.toArgb() and 0xFFFFFF)
    assertTrue("la tinta sobre el lila: %.1f".format(contrast(Clay.Ink, fill)), contrast(Clay.Ink, fill) >= 4.5)
    assertTrue("y la tinta apagada del percentil sobre la crema de la losa (#FFEAD1): %.1f".format(contrast(Clay.InkMuted, Color(0xFFFFEAD1))), contrast(Clay.InkMuted, Color(0xFFFFEAD1)) >= 4.5)
    assertTrue("la nota de muestra, crema sobre el noche plano", contrast(NubiTokens.OnNight, NubiTokens.NightFlat) >= 4.5)
    assertTrue("la campana lila sobre la ventanilla: %.1f".format(contrast(Clay.Grape, NubiTokens.NightWindow)), contrast(Clay.Grape, NubiTokens.NightWindow) >= 3.0)
    assertTrue("y la barra Tú, en sol, sobre la campana lila: %.1f".format(contrast(Clay.Sun, Clay.Grape)), contrast(Clay.Sun, Clay.Grape) >= 1.2)
    println("T202 contraste: tinta sobre #D9CEFF %.1f · tinta apagada sobre la losa %.1f · crema sobre el noche plano %.1f".format(contrast(Clay.Ink, fill), contrast(Clay.InkMuted, Color(0xFFFFEAD1)), contrast(NubiTokens.OnNight, NubiTokens.NightFlat)))
  }

  @Test fun la_hoja_del_candado_suma_solo_tu_ritmo_y_no_promete_la_comparacion_todavia() {
    val items = com.example.data.PlusAccess.SHEET_ITEMS
    assertEquals(6, items.size)
    assertEquals("Tu ritmo en los 2 minutos.", items.last())
    assertTrue("lo que todavía no existe no se promete", items.none { "edad" in it.lowercase() || "personas" in it.lowercase() || "percentil" in it.lowercase() })
    assertEquals("la entrada sigue diciendo «comparadas solo contigo»", "Una página con todas tus marcas, comparadas solo contigo:", com.example.data.PlusAccess.SHEET_LEAD)
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
    val titles = Regex("""Text\(title(?:\.glueNumbers\(\))?, color = Clay\.Ink, fontSize = 20\.sp[^\n]*""").findAll(page).toList()
    assertTrue("el título de cada losa va en Rubik", titles.isNotEmpty() && titles.all { "TitleFamily" in it.value })
    assertTrue("el título de la página, en Rubik", Regex("""OnBoardText\(TimeTrialMarks\.TITLE[^\n]*""").find(page)!!.value.contains("TitleFamily"))
    // letra: solo los dos rótulos del eje bajan de 16 sp
    val small = Regex("""fontSize = (\d+(?:\.\d+)?)\.sp""").findAll(page).filter { it.groupValues[1].toFloat() < 16f }.map { it.groupValues[1] }.toList()
    assertTrue("solo el rótulo del eje (los dos textos y su medida), a 14 sp", small.isNotEmpty() && small.all { it == "14" })
    val axisFns = listOf("""private fun AxisLabels\(labels[\s\S]*?\n\}""", """private fun RhythmChart\([\s\S]*?\n\}""", """private fun BellAxis\([\s\S]*?\n\}""", """private fun WeeksAxis\([\s\S]*?\n\}""")
    assertEquals("y están todos en las funciones de los ejes (las marcas, el ritmo, la campana y las semanas)", small.size, axisFns.sumOf { fn -> Regex(fn).find(page)!!.value.let { Regex("""fontSize = 14\.sp""").findAll(it).count() } })
    assertTrue("la ventanilla noche sale del token (`NubiTokens.NightWindow`), sin copiar el literal", page.contains("NubiTokens.NightWindow") && !Regex("0x[Ff][Ff]100[Ff]3[Aa]").containsMatchIn(page))
    assertEquals("la ventanilla es #100F3A", 0x100F3A, NubiTokens.NightWindow.toArgb() and 0xFFFFFF)
  }
}
