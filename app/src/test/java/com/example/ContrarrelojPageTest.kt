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
 * La página «Contrarreloj» de Avance con la pantalla REAL y los datos de la lámina de Fable (§11.3 de la Tarea 199: 31, 35, 39, 36, 44 —el récord, 17 de octubre—, 38, 41, 37, 42 y 43 hoy), rediseñada en la Tarea 203 (dirección D2 de `auditoria-fable/contrarreloj-rediseno.md`):
 * sin comparación es solo «Tus marcas» (tres cifras, las 10 marcas, el ritmo y las notas); con ella, primero la comparación (la cifra, la escalera de divisiones, la campana, otras edades y semana a semana) y al final «Tus marcas». Qué dibuja con cada letra (diez barras con la moneda del récord; cinco a 2,0, desde la fecha de la sexta y sin moneda
 * si el récord quedó fuera), lo que lee el lector de pantalla, los toques de 48 dp, y la prueba de cajas de Fable (§11.4): 4 dp o más entre cada par de piezas, nada fuera del margen de 16 dp y 8 dp entre losas, con letra 1,0, 1,3 y 2,0 a 393 dp y con 1,0, 1,3 y 2,0 a 360 dp según el caso. Las cajas se leen SIN recortar
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

  // «Tus marcas» y lo que va en sus losas (sin los contenedores: cada losa o cifra agrupa a sus piezas)
  private val stats = listOf("contrarreloj_stat_\\d_(figure|label)", "contrarreloj_stats_line_\\d")
  private val chart = listOf("contrarreloj_chart_(title|first)", "contrarreloj_figure_\\d+", "contrarreloj_coin", "contrarreloj_barshape_\\d+", "contrarreloj_axis_(left|right)", "contrarreloj_record_line", "contrarreloj_averages")
  private val notes = listOf("contrarreloj_rule", "contrarreloj_older_text", "contrarreloj_note")
  private val header = listOf("contrarreloj_back", "contrarreloj_title", "contrarreloj_subtitle")
  private val rhythm = listOf("contrarreloj_rhythm_(title|legend|sentence|wait)", "contrarreloj_rhythm_(fig|bar)_(hit|err)_\\d", "contrarreloj_rhythm_axis_(\\d|unit)")
  private val cards = listOf("contrarreloj_stats", "contrarreloj_marks", "contrarreloj_rhythm", "contrarreloj_notes")
  private val games = listOf("stroop", "disparate", "radar", "acoplamiento")

  private fun groupOf(map: Map<String, Rect>, patterns: List<String>) = map.filterKeys { k -> patterns.any { Regex(it).matches(k) } }

  private fun assertNoClashes(widthDp: Int, scale: Float) {
    show(widthDp, scale)
    val all = boxes()
    val label = "a $widthDp dp con letra $scale"
    val groups = mutableMapOf("la cabecera" to groupOf(all, header), "las tres cifras" to groupOf(all, stats), "Tus últimas 10 marcas" to groupOf(all, chart), "Tu ritmo" to groupOf(all, rhythm), "las notas" to groupOf(all, notes))
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
  @Test fun cero_choques_a_360_con_letra_2_0() = assertNoClashes(360, 2f)

  /** Lo que mide la página entera (de arriba al borde de abajo de la última losa más sus 32 dp de aire), en dp: lo mide `positionInRoot` + `size`, sin deslizar. */
  private fun pageHeight(): Float = boxes().let { b -> b.getValue("contrarreloj_notes").b + 32f }

  @Test fun a_393_con_letra_1_0_la_cabecera_el_selector_y_las_tres_cifras_caben_en_la_primera_pantalla() {
    show()
    val b = boxes()
    assertTrue("las tres cifras terminan en %.0f dp, dentro de los 873 dp de la primera pantalla".format(b.getValue("contrarreloj_stats").b), b.getValue("contrarreloj_stats").b <= 873f)
    println("T203 alto de la página a 393 con letra 1,0: %.0f dp; las tres cifras terminan en %.0f dp, las 10 marcas en %.0f dp".format(pageHeight(), b.getValue("contrarreloj_stats").b, b.getValue("contrarreloj_marks").b))
  }

  @Test fun el_alto_de_la_pagina_a_393_con_letra_1_3() { show(scale = 1.3f); println("T203 alto de la página a 393 con letra 1,3: %.0f dp".format(pageHeight())) }
  @Test fun el_alto_de_la_pagina_a_393_con_letra_2_0() { show(scale = 2f); println("T203 alto de la página a 393 con letra 2,0: %.0f dp".format(pageHeight())) }
  @Test fun el_alto_de_la_pagina_a_360_con_letra_1_0() { show(widthDp = 360); println("T203 alto de la página a 360 con letra 1,0: %.0f dp".format(pageHeight())) }

  // ---------------------------------------------------------------- lo que dibuja

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

  // ---------------------------------------------------------------- «Tus marcas»: las tres cifras, las 10 marcas y las notas

  private fun n(tag: String) = textOf(tag).replace(NBSP, " ")

  @Test fun las_tres_cifras_llevan_cada_una_su_unidad_y_sin_comparacion_la_tercera_son_las_partidas_de_la_semana() {
    show()
    assertEquals(listOf("44", "43", "3"), (0..2).map { n("contrarreloj_stat_${it}_figure") })
    assertEquals(listOf("aciertos, tu récord", "aciertos esta semana", "partidas esta semana"), (0..2).map { n("contrarreloj_stat_${it}_label") })
    assertEquals("44 aciertos, tu récord", descriptionOf("contrarreloj_stat_0"))
    assertEquals("3 partidas esta semana", descriptionOf("contrarreloj_stat_2"))
    val b = boxes()
    val w = (0..2).map { b.getValue("contrarreloj_stat_$it") }
    assertTrue("tres columnas parejas", w.all { Math.abs(it.w - w[0].w) < 1f } && w.all { Math.abs(it.y - w[0].y) < 0.5f })
    assertEquals("dos líneas finas de 1 dp entre las tres", setOf("contrarreloj_stats_line_1", "contrarreloj_stats_line_2"), b.keys.filter { it.startsWith("contrarreloj_stats_line_") }.toSet())
    assertTrue("cada línea entre dos cifras", (1..2).all { i -> b.getValue("contrarreloj_stats_line_$i").let { l -> Math.abs(l.w - 1f) < 0.1f && l.x >= w[i - 1].r - 0.5f && l.r <= w[i].x + 0.5f } })
  }

  @Test fun con_letra_1_3_las_tres_cifras_pasan_a_una_columna_con_su_rotulo_al_lado() {
    show(scale = 1.3f)
    val b = boxes()
    val rows = (0..2).map { b.getValue("contrarreloj_stat_$it") }
    assertTrue("una bajo otra", rows.zipWithNext().all { (a, c) -> c.y >= a.b })
    assertTrue("cada rótulo a la derecha de su cifra", (0..2).all { i -> b.getValue("contrarreloj_stat_${i}_label").x >= b.getValue("contrarreloj_stat_${i}_figure").r + 11.5f })
    assertEquals("sin las líneas de columnas", 0, b.keys.count { it.startsWith("contrarreloj_stats_line_") })
  }

  @Test fun las_cifras_grandes_no_crecen_mas_alla_de_1_3_de_escala_aunque_la_letra_sea_2_0() {
    show(scale = 2f, compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    val stat = b.getValue("contrarreloj_stat_0_figure").h
    val hero = b.getValue("contrarreloj_hero_figure").h
    assertTrue("con letra 2,0 las tres cifras miden como mucho su línea de 28 sp × 1,3 (%.1f dp)".format(stat), stat <= 28f * 1.3f + 1f)
    assertTrue("y la cifra grande, su línea de 44 sp × 1,3 (%.1f dp)".format(hero), hero <= 44f * 1.3f + 1f)
  }

  @Test fun la_linea_del_record_y_los_promedios_van_bajo_las_barras() {
    show()
    assertEquals("Tu récord es del 17 de octubre; llegaste al paso 4 de 5 de la pista.", n("contrarreloj_record_line"))
    assertEquals("Tus últimas 5 marcas promedian 40 aciertos; las 5 anteriores, 37.", n("contrarreloj_averages"))
    val b = boxes()
    assertTrue("la línea del récord va bajo la ventanilla de las barras y los promedios bajo la línea", b.getValue("contrarreloj_record_line").y >= b.getValue("contrarreloj_chart_window").b + 8f && b.getValue("contrarreloj_averages").y >= b.getValue("contrarreloj_record_line").b)
  }

  @Test fun las_notas_van_juntas_al_final_en_una_losa_sin_titulo_y_dicen_como_se_cuenta_la_marca() {
    show()
    assertEquals("La marca cuenta los aciertos menos los errores, sin bajar de cero.", textOf("contrarreloj_rule"))
    assertTrue(n("contrarreloj_older_text").startsWith("Tu récord de la pista 1: 38 aciertos."))
    assertEquals("Es el resultado de un juego, comparado solo contigo. No es una medida de capacidad.", textOf("contrarreloj_note"))
    val b = boxes()
    assertNull("la losa de notas no lleva título", b["contrarreloj_notes_title"])
    assertTrue("la regla, la pista anterior y la nota, en ese orden", b.getValue("contrarreloj_rule").b <= b.getValue("contrarreloj_older_text").y && b.getValue("contrarreloj_older_text").b <= b.getValue("contrarreloj_note").y)
    assertTrue("y es la última losa de la página", b.getValue("contrarreloj_notes").y >= b.getValue("contrarreloj_rhythm").b + 15.5f)
    assertNull("ya no hay una losa «Pistas anteriores»", b["contrarreloj_older"]); assertNull(b["contrarreloj_older_title"])
  }

  @Test fun sin_pista_anterior_las_notas_son_la_regla_y_la_nota() {
    show(results = ten)
    assertNull(boxes()["contrarreloj_older_text"])
    assertNotNull(boxes()["contrarreloj_rule"]); assertNotNull(boxes()["contrarreloj_note"])
  }

  @Test fun con_una_sola_marca_dice_que_la_primera_ya_esta_y_no_dibuja_barras() {
    show(results = listOf(r(31, 28)))
    val b = boxes()
    assertEquals("Tu primera marca. Desde la segunda, se dibujan aquí.", textOf("contrarreloj_chart_first"))
    assertEquals(0, b.keys.count { it.startsWith("contrarreloj_barshape_") })
    assertNull("sin dos promedios", b["contrarreloj_averages"])
    assertEquals(listOf("31", "31", "1"), (0..2).map { n("contrarreloj_stat_${it}_figure") })
    assertEquals("partida esta semana", n("contrarreloj_stat_2_label"))
    assertTrue("la línea del récord sí está: «${n("contrarreloj_record_line")}»", n("contrarreloj_record_line").let { it.startsWith("Tu récord es del ") && it.endsWith("llegaste al paso 4 de 5 de la pista.") })
  }

  @Test fun un_juego_sin_marcas_dice_que_falta_y_no_muestra_cifras_ni_ritmo() {
    show(selected = "radar", results = listOf(r(31, 28)))
    assertEquals("Todavía no tienes marcas en este juego. Cuando juegues su Contrarreloj, aquí aparece tu récord.", textOf("contrarreloj_record_empty"))
    val b = boxes()
    assertNull(b["contrarreloj_stats"]); assertNull(b["contrarreloj_marks"])
    assertNull("sin marcas en el juego no hay losa de ritmo", b["contrarreloj_rhythm"])
    assertNotNull("la nota de siempre sigue al pie", b["contrarreloj_note"])
    assertNull("y la regla de la marca no, porque no hay marca", b["contrarreloj_rule"])
    assertEquals("sin marca aún", textOf("contrarreloj_game_value_radar"))
  }

  // ---------------------------------------------------------------- orden, líneas, ventanilla noche y «Tu ritmo»

  @Test fun el_orden_de_las_losas_es_selector_cifras_marcas_ritmo_y_notas() {
    show()
    val b = boxes()
    assertTrue("el selector va arriba de las tres cifras", b.getValue("contrarreloj_game_radar").b < b.getValue("contrarreloj_stats").y)
    for ((a, c) in listOf("contrarreloj_stats", "contrarreloj_marks", "contrarreloj_rhythm", "contrarreloj_notes").zipWithNext()) {
      assertTrue("«$a» va antes que «$c»", b.getValue(a).b < b.getValue(c).y)
      assertEquals("y 16 dp de aire entre las dos losas", 16f, b.getValue(c).y - b.getValue(a).b, 1f)
    }
    assertNull("sin comparación no hay rótulos de sección", b["contrarreloj_section_marks"])
    assertEquals("ni ninguna línea de las losas viejas", emptyList<String>(), b.keys.filter { it.startsWith("contrarreloj_divider_") })
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
  }

  @Test fun tu_ritmo_dice_su_titulo_su_leyenda_y_la_frase_de_la_lamina() {
    show()
    assertEquals("Tu ritmo en 2 minutos", n("contrarreloj_rhythm_title"))
    assertEquals("Barras lila: aciertos. Barras coral: errores.", textOf("contrarreloj_rhythm_legend"))
    assertEquals("En tu última partida, tu mejor medio minuto fue el de 30 a 60 segundos, con 14 aciertos. Tus 3 errores llegaron en el último minuto.", textOf("contrarreloj_rhythm_sentence").replace(NBSP, " "))
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
    assertEquals("Tu ritmo en 2 minutos", n("contrarreloj_rhythm_title"))
    assertEquals("Tu ritmo aparece desde tu próxima partida del Contrarreloj.", textOf("contrarreloj_rhythm_wait"))
    val b = boxes()
    assertEquals("sin gráfico, sin leyenda y sin frase", 0, b.keys.count { it.startsWith("contrarreloj_rhythm_bar_") || it.startsWith("contrarreloj_rhythm_fig_") || it.startsWith("contrarreloj_rhythm_axis_") })
    assertNull(b["contrarreloj_rhythm_window"]); assertNull(b["contrarreloj_rhythm_legend"]); assertNull(b["contrarreloj_rhythm_sentence"])
    assertNotNull("y la página sigue entera: las cifras y las marcas están", b["contrarreloj_stats"]); assertNotNull(b["contrarreloj_marks"])
  }

  @Test fun el_ritmo_es_el_del_juego_elegido_y_con_cuatro_medios_minutos_parejos_no_resalta_ninguno() {
    val even = r(20, 27, 0, game = "disparate").copy(trials = paceTrials(listOf(5, 5, 5, 5), listOf(0, 0, 0, 0)))
    show(selected = "disparate", results = ten + others + even)
    assertEquals("En tu última partida, los cuatro medios minutos fueron parejos, con 5 aciertos cada uno. Sin errores.", textOf("contrarreloj_rhythm_sentence").replace(NBSP, " "))
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

  private fun contrast(a: Color, c: Color): Double { val la = a.luminance().toDouble(); val lc = c.luminance().toDouble(); return (maxOf(la, lc) + 0.05) / (minOf(la, lc) + 0.05) }

  @Test fun los_textos_sobre_la_ventanilla_noche_pasan_4_5_a_1() {
    val w = NubiTokens.NightWindow
    for ((name, c) in listOf("las cifras (OnNight)" to NubiTokens.OnNight, "el eje (OnNightDim)" to NubiTokens.OnNightDim, "la cifra resaltada (SunOnNight)" to NubiTokens.SunOnNight, "las cifras de los errores (coral)" to Clay.Coral)) {
      assertTrue("$name sobre la ventanilla: %.1f a 1".format(contrast(c, w)), contrast(c, w) >= 4.5)
    }
    for ((name, c) in listOf("las barras lila" to Clay.Grape, "las barras coral" to Clay.Coral, "la barra del récord (sol)" to Clay.Sun)) {
      assertTrue("$name se separan de la ventanilla (3 a 1 o más): %.1f".format(contrast(c, w)), contrast(c, w) >= 3.0)
    }
    println("T201 contraste sobre la ventanilla: OnNight %.1f · OnNightDim %.1f · SunOnNight %.1f · coral %.1f · lila %.1f".format(contrast(NubiTokens.OnNight, w), contrast(NubiTokens.OnNightDim, w), contrast(NubiTokens.SunOnNight, w), contrast(Clay.Coral, w), contrast(Clay.Grape, w)))
  }

  // ---------------------------------------------------------------- la comparación con personas de tu edad (datos de muestra, solo en depuración)

  private val manyWorld = SampleComparison(few = false)
  private val fewWorld = SampleComparison(few = true)
  private val veryFewWorld = SampleComparison(SampleComparison.World.VERY_FEW)

  private val comparePieces = listOf("contrarreloj_section_marks", "contrarreloj_section_compare", "contrarreloj_sample_notice", "contrarreloj_hero", "contrarreloj_ladder", "contrarreloj_bell", "contrarreloj_place", "contrarreloj_percweeks", "contrarreloj_others")

  @Test fun sin_comparacion_la_pagina_es_solo_tus_marcas_sin_rotulos_de_seccion_ni_comparacion() {
    show(compare = null, ageRange = AgeRange.R45_54)          // la build de la tienda: ninguna fuente aunque la persona tenga franja
    val b = boxes()
    for (tag in comparePieces) assertNull("sin comparación no existe «$tag»", b[tag])
    assertNotNull("y lo de siempre sigue", b["contrarreloj_rhythm"])
    assertEquals("la tercera cifra no depende de otras personas", "partidas esta semana", n("contrarreloj_stat_2_label"))
  }

  @Test fun con_datos_de_muestra_la_comparacion_va_primero_y_tus_marcas_al_final_con_su_rotulo() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertEquals("Esta semana, comparado con tu edad", textOf("contrarreloj_section_compare"))
    assertEquals("Tus marcas", textOf("contrarreloj_section_marks"))
    assertEquals("Estos son datos de muestra: solo se ven en la versión de prueba.", textOf("contrarreloj_sample_notice"))
    val order = listOf("contrarreloj_game_radar", "contrarreloj_section_compare", "contrarreloj_sample_notice", "contrarreloj_hero", "contrarreloj_ladder", "contrarreloj_bell", "contrarreloj_others", "contrarreloj_percweeks", "contrarreloj_section_marks", "contrarreloj_stats", "contrarreloj_marks", "contrarreloj_rhythm", "contrarreloj_notes")
    for ((a, c) in order.zipWithNext()) assertTrue("«$a» va antes que «$c»", b.getValue(a).b <= b.getValue(c).y + 0.5f)
    assertEquals("24 dp de aire sobre el primer rótulo: 16 de entre losas y 8 propios", 24f, b.getValue("contrarreloj_section_compare").y - b.getValue("contrarreloj_game_radar").b, 1.5f)
    assertEquals("y sobre el segundo", 24f, b.getValue("contrarreloj_section_marks").y - b.getValue("contrarreloj_percweeks").b, 1.5f)
    for (key in listOf("compare", "marks")) {
      val label = b.getValue("contrarreloj_section_$key"); val line = b.getValue("contrarreloj_section_${key}_line")
      assertEquals("la línea mide 1 dp", 1f, line.h, 0.1f)
      assertTrue("y va desde el final del texto hasta el margen derecho (16 dp)", line.x >= label.r + 11.5f && Math.abs(line.r - (393f - 16f)) < 1f)
    }
    assertEquals("el aviso de muestra va bajo el rótulo, a 8 dp", 8f, b.getValue("contrarreloj_sample_notice").y - b.getValue("contrarreloj_section_compare").b, 1.5f)
  }

  @Test fun el_rotulo_de_seccion_y_la_nota_de_muestra_van_en_fredoka_18_y_en_crema_de_16_sp_en_el_codigo() {
    val page = src("ui/screens/ContrarrelojPage.kt")
    assertTrue("el rótulo va en Fredoka negrita 18 sp con OnNight", Regex("""Text\(text, color = NubiTokens\.OnNight, fontSize = 18\.sp, fontWeight = FontWeight\.Bold, fontFamily = AppFamily""").containsMatchIn(page))
    assertTrue("la línea del rótulo es OnNight al 35 %", page.contains("NubiTokens.OnNight.copy(alpha = 0.35f)"))
    assertTrue("el aviso (la frase de la referencia estimada o la nota de muestra), en OnNight de 16 sp", Regex("""Text\(notice\.first\.glueNumbers\(\)\.glueNames\(\), color = NubiTokens\.OnNight, fontSize = 16\.sp""").containsMatchIn(page))
    assertTrue("y los dos textos salen de Comparison", page.contains("Comparison.estimatedNotice(s.range)") && page.contains("Comparison.SAMPLE_NOTICE"))
    assertTrue("el recuadro de «Todavía somos pocos» usa el lila de «Marca nueva» (TimeTrialDoneFill), sin copiar el literal", page.contains("background(TimeTrialDoneFill)") && !Regex("0x[Ff][Ff][Dd]9[Cc][Ee][Ff][Ff]").containsMatchIn(page))
  }

  // ---- la cifra protagonista

  /** La cifra con su frase juntas, como las lee una persona: «63 de cada 100 personas de 45 a 54 años quedan detrás de…» aunque con letra grande «de cada 100» pase de la cifra a la frase. */
  private fun assertHeroReading(width: Int, scale: Float) {
    show(widthDp = width, scale = scale, compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("a $width dp con letra $scale", "63 de cada 100 personas de 45 a 54 años quedan detrás de tu mejor marca de esta semana en Tinta o Palabra.", n("contrarreloj_hero_figure") + " " + n("contrarreloj_hero_caption"))
    assertTrue("la cifra sola nunca es un número suelto: lleva «de cada 100» en ella o en la frase de abajo", "de cada 100" in n("contrarreloj_hero_figure") || n("contrarreloj_hero_caption").startsWith("de cada 100 "))
  }

  @Test fun la_cifra_dice_63_de_cada_100_con_su_frase_su_division_y_su_percentil() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("63 de cada 100", n("contrarreloj_hero_figure"))
    assertEquals("personas de 45 a 54 años quedan detrás de tu mejor marca de esta semana en Tinta o Palabra.", n("contrarreloj_hero_caption"))
    assertEquals("En órbita", n("contrarreloj_hero_division"))
    assertEquals("Percentil 63", n("contrarreloj_hero_percentile"))
    assertEquals(
      "63 de cada 100 personas de 45 a 54 años quedan detrás de tu mejor marca de esta semana en Tinta o Palabra. En órbita. Percentil 63.",
      descriptionOf("contrarreloj_hero")
    )
    val b = boxes()
    assertNull("la clase normal no dice «Juntamos dos franjas»", b["contrarreloj_hero_scope"])
    val fig = b.getValue("contrarreloj_hero_figure"); val cap = b.getValue("contrarreloj_hero_caption"); val div = b.getValue("contrarreloj_hero_division"); val pct = b.getValue("contrarreloj_hero_percentile")
    assertTrue("de arriba abajo: la cifra, su frase y la división", fig.b <= cap.y && cap.b <= div.y)
    assertTrue("la división a la izquierda y el percentil a la derecha, en la misma fila", pct.x >= div.x + 11.5f && Math.abs((pct.y + pct.b) / 2 - (div.y + div.b) / 2) < 6f)
    assertTrue("la cifra mide 40 sp: más alta que cualquier otro texto de la tarjeta (%.1f dp)".format(fig.h), fig.h >= 40f && fig.h > div.h + 10f)
    assertEquals("la tarjeta llena el ancho de la losa y no tiene título", 393f - 32f, b.getValue("contrarreloj_hero").w, 1f)
    assertNull(b["contrarreloj_hero_title"])
  }

  @Test fun la_cifra_nunca_queda_sin_su_unidad_a_393_con_letra_1_0() = assertHeroReading(393, 1f)
  @Test fun la_cifra_nunca_queda_sin_su_unidad_a_393_con_letra_1_3() = assertHeroReading(393, 1.3f)
  @Test fun la_cifra_nunca_queda_sin_su_unidad_a_393_con_letra_2_0() = assertHeroReading(393, 2f)
  @Test fun la_cifra_nunca_queda_sin_su_unidad_a_360_con_letra_1_0() = assertHeroReading(360, 1f)
  @Test fun la_cifra_nunca_queda_sin_su_unidad_a_360_con_letra_2_0() = assertHeroReading(360, 2f)

  @Test fun con_pocas_personas_en_la_franja_la_cifra_dice_la_clase_que_uso() {
    // 75 o más: 38 personas esta semana → sus 4 semanas
    show(compare = manyWorld, ageRange = AgeRange.R75_PLUS)
    assertTrue("«${n("contrarreloj_hero_caption")}»", n("contrarreloj_hero_caption").endsWith("personas de 75 años o más que jugaron Tinta o Palabra en las últimas 4 semanas quedan detrás de tu mejor marca de esta semana."))
    assertEquals("el título es el mismo; la nota de abajo dice la clase", "La escalera de tu franja", textOf("contrarreloj_ladder_title"))
    assertTrue("«${n("contrarreloj_ladder_note")}»", n("contrarreloj_ladder_note").let { it.startsWith("Con las ") && it.endsWith("personas de tu franja que jugaron en las últimas 4 semanas.") })
    assertNull("la clase de 4 semanas de la propia franja no necesita decir que se juntaron franjas", boxes()["contrarreloj_hero_scope"])
  }

  // ---- la escalera

  @Test fun la_escalera_son_cinco_divisiones_de_arriba_abajo_cada_una_con_su_primer_acierto_y_tu_fila_en_lima() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("La escalera de tu franja", textOf("contrarreloj_ladder_title"))
    assertEquals("Entre estrellas", n("contrarreloj_ladder_row_0_label")); assertEquals("desde 60 aciertos", n("contrarreloj_ladder_row_0_value"))
    assertEquals("En la Luna", n("contrarreloj_ladder_row_1_label")); assertEquals("desde 50 aciertos", n("contrarreloj_ladder_row_1_value"))
    assertEquals("En órbita", n("contrarreloj_ladder_you_name")); assertEquals("Tú · 43 aciertos", n("contrarreloj_ladder_you_value"))
    assertEquals("Despegando", n("contrarreloj_ladder_row_3_label")); assertEquals("desde 31 aciertos", n("contrarreloj_ladder_row_3_value"))
    assertEquals("En la plataforma", n("contrarreloj_ladder_row_4_label")); assertEquals("de 0 a 30: llega hasta 30 aciertos", "hasta 30 aciertos", n("contrarreloj_ladder_row_4_value"))
    assertEquals("la fila tuya no se dice solo por el color: lleva la división y «Tú»", "En órbita, Tú · 43 aciertos", descriptionOf("contrarreloj_ladder_you").replace(NBSP, " "))
    assertEquals("la de otra división se lee con su primer acierto", "Entre estrellas, desde 60 aciertos", descriptionOf("contrarreloj_ladder_row_0").replace(NBSP, " "))
    val b = boxes()
    val order = listOf("contrarreloj_ladder_row_0", "contrarreloj_ladder_row_1", "contrarreloj_ladder_you", "contrarreloj_ladder_row_3", "contrarreloj_ladder_row_4")
    for ((a, c) in order.zipWithNext()) assertTrue("«$a» va antes que «$c»", b.getValue(a).b <= b.getValue(c).y + 0.5f)
    val card = b.getValue("contrarreloj_ladder"); val you = b.getValue("contrarreloj_ladder_you")
    assertEquals("la fila tuya llena el ancho útil de la losa (relleno de 16 dp)", card.w - 32f, you.w, 1f)
    assertTrue("y mide 40 dp o más de alto (una línea de 24 dp y 10 dp de relleno arriba y abajo)", you.h >= 39.5f)
  }

  @Test fun al_pie_de_la_escalera_dice_te_faltan_con_su_barra_cuando_cierra_la_semana_y_las_semanas_seguidas() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("Te faltan 7 aciertos para En la Luna.", n("contrarreloj_ladder_need"))
    assertEquals("La semana cierra el domingo.", n("contrarreloj_ladder_closes"))
    assertEquals("Llevas 3 semanas seguidas En órbita o más arriba.", n("contrarreloj_ladder_streak"))
    assertEquals("Con las 128 personas de tu franja que jugaron esta semana.", n("contrarreloj_ladder_note"))
    val b = boxes()
    val order = listOf("contrarreloj_ladder_row_4", "contrarreloj_divider_ladder", "contrarreloj_ladder_need", "contrarreloj_ladder_bar", "contrarreloj_ladder_closes", "contrarreloj_ladder_streak", "contrarreloj_ladder_note")
    for ((a, c) in order.zipWithNext()) assertTrue("«$a» va antes que «$c»", b.getValue(a).b <= b.getValue(c).y + 0.5f)
    val bar = b.getValue("contrarreloj_ladder_bar"); val fill = b.getValue("contrarreloj_ladder_bar_fill")
    assertEquals("la barra mide 16 dp", 16f, bar.h, 0.6f)
    assertEquals("va llena en 4 de los 11 aciertos del tramo de En órbita a En la Luna", 4f / 11f, fill.w / bar.w, 0.02f)
    assertEquals("la barra llena el ancho útil de la losa", b.getValue("contrarreloj_ladder").w - 32f, bar.w, 1f)
    assertEquals("la línea de separación mide 1 dp", 1f, b.getValue("contrarreloj_divider_ladder").h, 0.1f)
  }

  @Test fun en_la_division_mas_alta_no_falta_nada_y_no_hay_barra() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54, results = listOf(r(70, 28)))
    assertEquals("Estás en la división más alta de tu franja esta semana.", n("contrarreloj_ladder_need"))
    assertEquals("Entre estrellas", n("contrarreloj_ladder_you_name")); assertEquals("Tú · 70 aciertos", n("contrarreloj_ladder_you_value"))
    val b = boxes()
    assertNull("sin siguiente división no hay barra", b["contrarreloj_ladder_bar"])
    assertTrue("tú vas en la fila de arriba", b.getValue("contrarreloj_ladder_you").y < b.getValue("contrarreloj_ladder_row_1").y)
    assertEquals("quedan cuatro divisiones más, sin «Entre estrellas»", 4, b.keys.count { Regex("contrarreloj_ladder_row_\\d").matches(it) })
    assertEquals("La semana cierra el domingo.", n("contrarreloj_ladder_closes"))
  }

  @Test fun la_pagina_no_dice_nada_de_semanas_seguidas_cuando_la_cuenta_es_cero() {
    // esta semana por debajo de la mitad: marca 20 → percentil bajo
    show(compare = manyWorld, ageRange = AgeRange.R45_54, results = listOf(r(20, 28), r(45, 22)))
    assertNull("sin racha, la escalera calla (nunca «perdiste»)", boxes()["contrarreloj_ladder_streak"])
    assertTrue(allTexts().none { "perdiste" in it.lowercase() || "racha" in it.lowercase() && "perd" in it.lowercase() })
  }

  // ---- la campana, su losa propia

  @Test fun la_campana_tiene_su_losa_con_titulo_y_una_frase_sin_numeros_de_la_escalera() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("La campana de tu edad", textOf("contrarreloj_bell_title"))
    assertEquals("La línea marca la mitad de las personas.", textOf("contrarreloj_bell_caption"))
    assertTrue("la frase de la campana no repite ningún número de la escalera", textOf("contrarreloj_bell_caption").none { it.isDigit() })
    val b = boxes()
    assertTrue("la campana va tras la escalera, en su propia losa, y su frase debajo de la ventanilla", b.getValue("contrarreloj_bell").y >= b.getValue("contrarreloj_ladder").b + 15.5f && b.getValue("contrarreloj_bell_caption").y >= b.getValue("contrarreloj_bell_window").b)
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

  // ---- semana a semana, otras edades, pocos y sin marca

  @Test fun tu_percentil_semana_a_semana_dibuja_las_semanas_con_marca_de_la_mas_vieja_a_esta() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("Tu percentil semanal", textOf("contrarreloj_percweeks_title").replace(NBSP, " "))
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
    assertEquals(Comparison.weeksSentence(points), textOf("contrarreloj_percweeks_sentence"))
    assertEquals(Comparison.weeksSpoken(points), descriptionOf("contrarreloj_percweeks_window"))
    assertNotNull("a la izquierda «hace 3 semanas»", b["contrarreloj_percweeks_axis_left"]); assertNotNull("a la derecha «esta semana»", b["contrarreloj_percweeks_axis_right"])
    // la raya de 50: a la mitad exacta del alto de las barras (48 de 96 dp), con su rótulo «la mitad» en un carril a la derecha del gráfico
    val half = b.getValue("contrarreloj_percweeks_half_line"); val label = b.getValue("contrarreloj_percweeks_half"); val base = b.getValue("contrarreloj_percweeks_baseline")
    assertEquals("la raya queda a 48 dp sobre la base de las barras", base.y - 48f, half.y + half.h / 2, 1f)
    assertEquals("y mide 1 dp", 1f, half.h, 0.1f)
    assertEquals("el rótulo, centrado sobre la raya", half.y + half.h / 2, label.y + label.h / 2, 1.5f)
    assertTrue("a la derecha de la última barra, a 8 dp o más, y no más allá del relleno de la ventanilla", label.x >= b.getValue("contrarreloj_percweeks_bar_0").r + 7.5f && label.r <= b.getValue("contrarreloj_percweeks_window").r - 11.5f)
    assertTrue("la raya cruza todo el gráfico y no entra al carril del rótulo", half.r <= label.x - 7.5f && half.x <= b.getValue("contrarreloj_percweeks_bar_3").x)
    assertTrue("el lector de pantalla dice «deja detrás a», igual que la cifra y «Otras edades»", descriptionOf("contrarreloj_percweeks_window").let { "deja detrás a" in it && "supera" !in it })
    assertEquals("hace 3 semanas", Comparison.weeksAxisLeft(points))
    assertTrue("el gráfico llena el ancho de la losa y las semanas sin dato no dejan huecos a la izquierda", b.getValue("contrarreloj_percweeks_window").w > 300f && b.getValue("contrarreloj_percweeks_bar_3").x < b.getValue("contrarreloj_percweeks_window").x + 60f)
  }

  @Test fun la_tercera_cifra_con_comparacion_sigue_siendo_las_partidas_de_la_persona() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("3", n("contrarreloj_stat_2_figure"))
    assertEquals("partidas esta semana", n("contrarreloj_stat_2_label"))
    assertEquals("3 partidas esta semana", descriptionOf("contrarreloj_stat_2"))
    assertEquals("las otras dos no cambian", listOf("44", "43"), (0..1).map { n("contrarreloj_stat_${it}_figure") })
    assertTrue("«3 de 4» ya no sale: la frase de semana a semana lo dice una vez", allTexts().none { "semanas arriba de la mitad" in it })
  }

  @Test fun otras_edades_muestra_solo_las_franjas_con_50_personas_la_propia_en_negrita_y_todas_las_edades() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("Otras edades", textOf("contrarreloj_others_title"))
    assertEquals("De cada 100 personas que jugaron Tinta o Palabra esta semana, tu mejor marca deja detrás a:", textOf("contrarreloj_others_lead").replace(NBSP, " "))
    val labels = (0..4).map { n("contrarreloj_others_${it}_label") }
    assertEquals(listOf("18 a 29 años", "30 a 44 años", "45 a 54 años (tu franja)", "55 a 64 años", "65 a 74 años"), labels)
    assertTrue("ninguna fila de 13 a 17 años ni de 75 o más (tiene 38 personas: no hay fila de relleno)", labels.none { "13 a 17" in it || "75" in it })
    assertEquals("63 de cada 100", n("contrarreloj_others_2_value"))
    assertEquals("Todas las edades", n("contrarreloj_others_all_label"))
    assertEquals("Las demás edades aparecen cuando lleguen a 50 personas.", textOf("contrarreloj_others_hidden"))
    assertTrue("ya no hay línea de 13 a 17 años ni «todavía somos pocos» en la losa", allTexts().none { "13 a 17" in it || it == "todavía somos pocos" })
    val b = boxes()
    assertNull(b["contrarreloj_others_5"])
    assertTrue("una línea entre las franjas y «Todas las edades»", b.getValue("contrarreloj_divider_others").y > b.getValue("contrarreloj_others_4").b && b.getValue("contrarreloj_divider_others").b < b.getValue("contrarreloj_others_all").y)
    assertTrue("y la línea que cierra va al final", b.getValue("contrarreloj_others_hidden").y > b.getValue("contrarreloj_others_all").b)
    assertEquals("45 a 54 años (tu franja), 63 de cada 100", descriptionOf("contrarreloj_others_2").replace(NBSP, " "))
    assertTrue("el lector de pantalla dice lo mismo: la línea que cierra es texto de la página", allTexts().contains("Las demás edades aparecen cuando lleguen a 50 personas."))
  }

  @Test fun las_franjas_y_las_divisiones_no_se_parten_con_espacios_duros() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54, scale = 2f)
    assertTrue("la franja entera en la frase de la cifra", textOf("contrarreloj_hero_caption").contains("45${NBSP}a${NBSP}54${NBSP}años"))
    assertTrue("y «Te faltan 7 aciertos» con «En la Luna» enteros", textOf("contrarreloj_ladder_need").let { it.contains("Te${NBSP}faltan${NBSP}7${NBSP}aciertos") && it.contains("En${NBSP}la${NBSP}Luna") })
    assertTrue("los nombres de las filas de la escalera", textOf("contrarreloj_ladder_row_1_label").contains("En${NBSP}la${NBSP}Luna") && textOf("contrarreloj_ladder_you_name").contains("En${NBSP}órbita"))
    assertTrue("y las franjas de otras edades", textOf("contrarreloj_others_0_label").contains("18${NBSP}a${NBSP}29${NBSP}años"))
    assertEquals("pero lo que lee el lector de pantalla queda con espacios normales", "En órbita, Tú · 43 aciertos", descriptionOf("contrarreloj_ladder_you"))
    assertTrue("la descripción de la cifra no lleva espacios duros", !descriptionOf("contrarreloj_hero").contains(NBSP))
  }

  @Test fun en_el_mundo_de_pocos_no_hay_cifra_ni_escalera_ni_te_faltan_y_dice_la_cuenta_real() {
    show(compare = fewWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertEquals("Tu lugar esta semana", textOf("contrarreloj_place_title"))
    assertEquals("Todavía somos pocos: 23 personas de 45 a 54 años jugaron Tinta o Palabra esta semana. Cuando sean 50, verás aquí tu lugar entre ellas.", textOf("contrarreloj_few_note").replace(NBSP, " "))
    assertEquals("23 de 50 personas", textOf("contrarreloj_few_bar_value").replace(NBSP, " "))
    assertTrue("«Mientras tanto…» con todas las edades de ESTA semana (llegan a 50)", textOf("contrarreloj_few_meanwhile").replace(NBSP, " ").startsWith("Mientras tanto, comparada con todas las edades de esta semana, tu mejor marca, 43 aciertos, deja detrás a "))
    for (tag in listOf("contrarreloj_hero", "contrarreloj_ladder", "contrarreloj_bell", "contrarreloj_bell_window", "contrarreloj_ladder_need", "contrarreloj_percweeks")) assertNull("con pocas personas no existe «$tag» (nada de umbrales ni «te faltan» inventados)", b[tag])
    assertTrue("la barra llena menos de la mitad (23 de 50)", b.getValue("contrarreloj_few_bar_bar").w < 0.6f * b.getValue("contrarreloj_few_bar").w)
    assertTrue("una línea separa la barra de «Mientras tanto»", b.getValue("contrarreloj_divider_few").y > b.getValue("contrarreloj_few_bar").b && b.getValue("contrarreloj_divider_few").b < b.getValue("contrarreloj_few_meanwhile").y)
    assertEquals("de las franjas solo llega a 50 la de 18 a 29 años (70 personas); las demás no tienen fila y la línea que cierra lo dice", listOf("18 a 29 años"), (0..5).mapNotNull { i -> b["contrarreloj_others_${i}_label"]?.let { n("contrarreloj_others_${i}_label") } })
    assertEquals("Las demás edades aparecen cuando lleguen a 50 personas.", textOf("contrarreloj_others_hidden"))
    assertEquals("la fila de todas las edades sí llega a 50", "Todas las edades", n("contrarreloj_others_all_label"))
    assertEquals("sin semanas con 50 personas la tercera cifra son las partidas", "partidas esta semana", n("contrarreloj_stat_2_label"))
    assertTrue("y al final de la página siguen «Tus marcas»", b.getValue("contrarreloj_section_marks").y > b.getValue("contrarreloj_others").b)
  }

  @Test fun sin_marca_esta_semana_la_losa_dice_que_hacer_y_no_hay_otras_edades_ni_escalera() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54, selected = "radar", results = listOf(r(27, 23, game = "radar")))
    assertEquals("Todavía no tienes una marca esta semana en Rescate relámpago. Juega su Contrarreloj para ver tu lugar entre personas de tu edad.", textOf("contrarreloj_place_nomark"))
    val b = boxes()
    for (tag in listOf("contrarreloj_others", "contrarreloj_hero", "contrarreloj_ladder", "contrarreloj_bell")) assertNull("sin marca esta semana no existe «$tag»", b[tag])
    assertNotNull("pero las semanas anteriores con marca sí se cuentan (la del 23 de octubre)", b["contrarreloj_percweeks"])
  }

  @Test fun quien_tiene_de_13_a_17_anos_no_ve_la_comparacion_ni_con_datos_de_muestra() {
    show(compare = manyWorld, ageRange = AgeRange.R13_17)
    val b = boxes()
    for (tag in comparePieces) assertNull("de 13 a 17 años no existe «$tag»", b[tag])
  }

  @Test fun sin_franja_elegida_los_datos_de_muestra_usan_45_a_54() {
    show(compare = manyWorld, ageRange = null)
    assertEquals("personas de 45 a 54 años quedan detrás de tu mejor marca de esta semana en Tinta o Palabra.", n("contrarreloj_hero_caption"))
  }

  // ---- el mismo grupo nunca muestra dos números en una pantalla, y la cabecera y la nota no dicen «solo contigo» con la comparación

  /** Todo lo que la página dice o lee, de UN solo árbol sin fusionar: los textos y las descripciones del lector de pantalla (sin los espacios duros). */
  private fun allTexts(): List<String> {
    val out = mutableListOf<String>()
    fun walk(n: SemanticsNode) {
      n.config.getOrNull(SemanticsProperties.Text)?.let { out += it.joinToString("") { s -> s.text }.replace(NBSP, " ") }
      n.config.getOrNull(SemanticsProperties.ContentDescription)?.let { out += it.joinToString(". ").replace(NBSP, " ") }
      n.children.forEach(::walk)
    }
    walk(rule.onRoot(useUnmergedTree = true).fetchSemanticsNode())
    return out
  }

  @Test fun en_pocos_el_numero_de_mientras_tanto_es_el_mismo_de_la_fila_todas_las_edades() {
    show(compare = fewWorld, ageRange = AgeRange.R45_54)
    val meanwhile = textOf("contrarreloj_few_meanwhile").replace(NBSP, " ")
    assertTrue(meanwhile.startsWith("Mientras tanto, comparada con todas las edades de esta semana, tu mejor marca, 43 aciertos, deja detrás a "))
    val n = Regex("deja detrás a (\\d+) de cada 100 personas\\.").find(meanwhile)!!.groupValues[1]
    assertEquals("la fila «Todas las edades» dice el MISMO número", "$n de cada 100", textOf("contrarreloj_others_all_value").replace(NBSP, " "))
    // y en toda la pantalla, lo que habla de «todas las edades» con un número dice siempre ese
    val numbers = allTexts().filter { "todas las edades" in it.lowercase() }.flatMap { s -> Regex("(\\d+) de cada 100").findAll(s).map { it.groupValues[1] }.toList() }
    assertTrue("todos los números de «todas las edades» son $n: $numbers", numbers.size >= 2 && numbers.all { it == n })
  }

  @Test fun en_muy_pocos_mientras_tanto_usa_las_4_semanas_lo_dice_y_la_fila_todas_las_edades_dice_todavia_somos_pocos() {
    show(compare = veryFewWorld, ageRange = AgeRange.R45_54)
    val meanwhile = textOf("contrarreloj_few_meanwhile").replace(NBSP, " ")
    assertTrue("«$meanwhile»", meanwhile.startsWith("Mientras tanto, comparada con todas las edades de las últimas 4 semanas, tu mejor marca de esta semana, 43 aciertos, deja detrás a "))
    assertNull("todas las edades de ESTA semana no llega a 50 y ninguna franja tampoco: «Otras edades» no existe", boxes()["contrarreloj_others"])
    assertTrue("y ninguna fila de todas las edades da otro número", allTexts().none { it.startsWith("Todas las edades,") && "de cada 100" in it })
        assertEquals("la frase enmarcada sigue con la cuenta real de la franja", true, textOf("contrarreloj_few_note").replace(NBSP, " ").startsWith("Todavía somos pocos: 23 personas de 45 a 54 años"))
  }

  @Test fun con_la_comparacion_el_subtitulo_y_la_nota_ya_no_dicen_solo_contigo() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("Tus marcas, comparadas contigo y con personas de tu edad.", textOf("contrarreloj_subtitle"))
    assertEquals("Es el resultado de un juego. No es una medida de capacidad.", textOf("contrarreloj_note"))
    val saysOnlyYou = allTexts().filter { "solo contigo" in it.lowercase() }
    assertTrue("con la comparación ningún texto visible ni leído dice «solo contigo»: $saysOnlyYou", saysOnlyYou.isEmpty())
  }

  @Test fun con_la_comparacion_en_pocos_tampoco_dice_solo_contigo() {
    show(compare = fewWorld, ageRange = AgeRange.R45_54)
    assertEquals("Tus marcas, comparadas contigo y con personas de tu edad.", textOf("contrarreloj_subtitle"))
    assertTrue(allTexts().none { "solo contigo" in it.lowercase() })
  }

  @Test fun sin_la_comparacion_la_cabecera_y_la_nota_quedan_como_hoy() {
    show(compare = null, ageRange = AgeRange.R45_54)          // la build de la tienda
    assertEquals("Tus marcas, comparadas solo contigo.", textOf("contrarreloj_subtitle"))
    assertEquals("Es el resultado de un juego, comparado solo contigo. No es una medida de capacidad.", textOf("contrarreloj_note"))
  }

  @Test fun de_13_a_17_anos_la_cabecera_y_la_nota_quedan_como_hoy() {
    show(compare = manyWorld, ageRange = AgeRange.R13_17)
    assertEquals("Tus marcas, comparadas solo contigo.", textOf("contrarreloj_subtitle"))
    assertEquals("Es el resultado de un juego, comparado solo contigo. No es una medida de capacidad.", textOf("contrarreloj_note"))
  }


  // ---------------------------------------------------------------- Tarea 203b: títulos de una línea, rótulo sin raya suelta, negrita y siete semanas

  private val titleTags = listOf("contrarreloj_ladder_title", "contrarreloj_bell_title", "contrarreloj_others_title", "contrarreloj_percweeks_title", "contrarreloj_chart_title", "contrarreloj_rhythm_title")

  private fun assertTitlesInOneLine(widthDp: Int) {
    show(widthDp = widthDp, compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    for (tag in titleTags) assertTrue("«${textOf(tag)}» a $widthDp dp con letra 1,0 mide %.1f dp de alto (una línea de Rubik 24 mide 28)".format(b.getValue(tag).h), b.getValue(tag).h <= 28.5f)
  }

  @Test fun los_seis_titulos_de_losa_caben_en_una_linea_a_393_con_letra_1_0() = assertTitlesInOneLine(393)
  @Test fun los_seis_titulos_de_losa_caben_en_una_linea_a_360_con_letra_1_0() = assertTitlesInOneLine(360)

  @Test fun el_rotulo_de_seccion_lleva_su_raya_con_letra_1_0() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    for (key in listOf("compare", "marks")) {
      val label = b.getValue("contrarreloj_section_$key"); val line = b.getValue("contrarreloj_section_${key}_line")
      assertTrue("la raya de «$key» mide 24 dp o más (%.1f)".format(line.w), line.w >= 23.5f)
      assertTrue("y el rótulo está en UNA línea (%.1f dp)".format(label.h), label.h <= 24.5f)
    }
  }

  @Test fun cuando_el_rotulo_de_seccion_parte_en_varias_lineas_la_raya_no_va() {
    show(scale = 2f, compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertTrue("«Esta semana, comparado con tu edad» parte con letra 2,0 (%.1f dp de alto)".format(b.getValue("contrarreloj_section_compare").h), b.getValue("contrarreloj_section_compare").h > 30f)
    assertNull("y entonces no queda un guion suelto junto a la última palabra", b["contrarreloj_section_compare_line"])
    assertEquals("el rótulo ocupa el ancho entero, de margen a margen", 393f - 32f, b.getValue("contrarreloj_section_compare").w, 1f)
  }

  @Test fun en_el_texto_corrido_solo_van_en_negrita_te_faltan_y_la_fila_lima() {
    val page = src("ui/screens/ContrarrelojPage.kt")
    for (tag in listOf("contrarreloj_percweeks_sentence", "contrarreloj_rhythm_sentence", "contrarreloj_ladder_closes", "contrarreloj_ladder_streak", "contrarreloj_ladder_note", "contrarreloj_bell_caption", "contrarreloj_others_lead", "contrarreloj_others_hidden")) {
      val line = page.lines().first { "testTag(\"$tag\")" in it }
      assertFalse("«$tag» va en peso normal", "FontWeight.Bold" in line || "FontWeight.SemiBold" in line)
    }
    assertTrue("«Te faltan…» sí va en negrita", page.lines().first { "testTag(\"contrarreloj_ladder_need\")" in it }.contains("FontWeight.Bold"))
    assertTrue("y la fila lima también", page.lines().filter { "contrarreloj_ladder_you_" in it && "Text(" in it }.all { "FontWeight.Bold" in it })
  }

  @Test fun con_siete_semanas_a_360_con_letra_2_0_las_cifras_no_se_tocan() {
    // una marca en cada una de las últimas 7 semanas (las tres de septiembre se suman a las de octubre)
    val sept = listOf(r(34, 1), r(36, -5), r(38, -12))
    show(widthDp = 360, scale = 2f, compare = manyWorld, ageRange = AgeRange.R45_54, results = ten + others + sept)
    val b = boxes()
    val figs = b.filterKeys { it.startsWith("contrarreloj_percweeks_fig_") }
    assertEquals("siete cifras", 7, figs.size)
    val problems = mutableListOf<String>()
    val keys = (6 downTo 0).map { "contrarreloj_percweeks_fig_$it" }
    for ((a, c) in keys.zipWithNext()) { val s = gap(b.getValue(a), b.getValue(c)); if (s < 3.5f) problems += "«$a» y «$c» a %.1f dp".format(s) }
    val win = b.getValue("contrarreloj_percweeks_window")
    for ((tag, p) in b) if (tag.startsWith("contrarreloj_percweeks_") && tag != "contrarreloj_percweeks" && tag != "contrarreloj_percweeks_window" && tag != "contrarreloj_percweeks_title" && tag != "contrarreloj_percweeks_sentence") {
      if (p.x < win.x + 11.5f || p.r > win.r - 11.5f) problems += "«$tag» se sale del relleno de la ventanilla"
    }
    assertTrue("siete semanas a 360 dp con letra 2,0:\n" + problems.joinToString("\n"), problems.isEmpty())
  }

  // ---- la prueba de cajas con la comparación: 4 dp entre cada par de piezas, nada fuera del margen y 8 dp entre losas

  private val hero = listOf("contrarreloj_hero_(figure|caption|scope|division|percentile)")
  private val ladder = listOf("contrarreloj_ladder_(title|need|closes|streak|note)", "contrarreloj_ladder_row_\\d_(label|value)", "contrarreloj_ladder_you_(name|value)", "contrarreloj_ladder_bar")
  private val bell = listOf("contrarreloj_bell_(title|caption|you|coin|you_bar|median)", "contrarreloj_bell_axis_\\d")
  private val place = listOf("contrarreloj_place_(title|nomark)", "contrarreloj_few_(note|meanwhile)", "contrarreloj_few_bar_(bar|value)")
  private val weeksPercentile = listOf("contrarreloj_percweeks_(title|sentence|half)", "contrarreloj_percweeks_fig_\\d", "contrarreloj_percweeks_bar_\\d", "contrarreloj_percweeks_axis_(left|right)")
  private val othersGroup = listOf("contrarreloj_others_(title|lead|hidden)", "contrarreloj_others_(\\d|all)_(label|value)")
  private val sections = listOf("contrarreloj_section_(marks|compare)", "contrarreloj_section_(marks|compare)_line", "contrarreloj_(sample|estimated)_notice")

  private fun assertCompareNoClashes(widthDp: Int, scale: Float, world: Comparison.Source, label: String) {
    show(widthDp, scale, compare = world, ageRange = AgeRange.R45_54)
    val all = boxes()
    val problems = mutableListOf<String>()
    val groups = mapOf(
      "La cifra" to groupOf(all, hero), "La escalera" to groupOf(all, ladder), "La campana" to groupOf(all, bell), "Tu lugar" to groupOf(all, place), "Tu percentil, semana a semana" to groupOf(all, weeksPercentile),
      "Comparado con otras edades" to groupOf(all, othersGroup), "los rótulos de sección" to groupOf(all, sections), "las tres cifras" to groupOf(all, stats), "Tus últimas 10 marcas" to groupOf(all, chart), "Tu ritmo" to groupOf(all, rhythm), "las notas" to groupOf(all, notes)
    )
    for ((name, g) in groups) {
      val keys = g.keys.toList()
      for (i in keys.indices) for (j in i + 1 until keys.size) {
        val s = gap(g.getValue(keys[i]), g.getValue(keys[j]))
        if (s < 3.5f) problems += "$name: «${keys[i]}» y «${keys[j]}» a %.1f dp".format(s)
      }
    }
    assertTrue("$label: la prueba no vio ni la cifra ni «Tu lugar»", groups.getValue("La cifra").isNotEmpty() || groups.getValue("Tu lugar").isNotEmpty())
    for ((tag, b) in all) if (tag.startsWith("contrarreloj_") && tag !in listOf("contrarreloj_page", "contrarreloj_scroll", "contrarreloj_header")) {
      if (b.x < 15.5f || b.r > widthDp - 15.5f) problems += "«$tag» se sale del margen de 16 dp (x %.1f a %.1f)".format(b.x, b.r)
    }
    val slabs = (games.map { "contrarreloj_game_$it" } + cards + listOf("contrarreloj_hero", "contrarreloj_ladder", "contrarreloj_bell", "contrarreloj_place", "contrarreloj_percweeks", "contrarreloj_others")).filter { it in all }
    for (i in slabs.indices) for (j in i + 1 until slabs.size) { val s = gap(all.getValue(slabs[i]), all.getValue(slabs[j])); if (s < 7.5f) problems += "las losas «${slabs[i]}» y «${slabs[j]}» a %.1f dp".format(s) }
    for ((win, pieces) in listOf("contrarreloj_bell_window" to Regex("contrarreloj_bell_(area|you|you_bar|median|coin|axis_\\d)"), "contrarreloj_percweeks_window" to Regex("contrarreloj_percweeks_(fig_\\d|bar_\\d|baseline|half|half_line|axis_(left|right))")))
      all[win]?.let { w -> for ((tag, p) in all) if (pieces.matches(tag) && (p.x < w.x + 11.5f || p.r > w.r - 11.5f || p.y < w.y + 11.5f || p.b > w.b - 11.5f)) problems += "«$tag» se sale del relleno de «$win»" }
    // dentro de la fila lima y de la tarjeta de la cifra: nada pisa el borde con su relleno de 12 dp
    all["contrarreloj_ladder_you"]?.let { y -> for (tag in listOf("contrarreloj_ladder_you_name", "contrarreloj_ladder_you_value")) all[tag]?.let { p -> if (p.x < y.x + 11.5f || p.r > y.r - 11.5f) problems += "«$tag» no deja 12 dp en la fila lima" } }
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
  @Test fun muy_pocos_cero_choques_a_393_con_letra_1_0() = assertCompareNoClashes(393, 1f, veryFewWorld, "muy pocos")
  @Test fun muy_pocos_cero_choques_a_360_con_letra_2_0() = assertCompareNoClashes(360, 2f, veryFewWorld, "muy pocos")

  private fun compareHeight(widthDp: Int, scale: Float, world: SampleComparison = manyWorld): String {
    show(widthDp, scale, compare = world, ageRange = AgeRange.R45_54)
    val b = boxes()
    return "T203 alto de la página con comparación (%s) a %d con letra %s: %.0f dp; la cifra termina en %s dp, la escalera mide %s dp, la campana %s dp".format(
      if (world === manyWorld) "muchos" else "pocos", widthDp, scale, b.getValue("contrarreloj_notes").b + 32f,
      (b["contrarreloj_hero"] ?: b["contrarreloj_place"])?.let { "%.0f".format(it.b) } ?: "?", b["contrarreloj_ladder"]?.let { "%.0f".format(it.h) } ?: "no hay", b["contrarreloj_bell"]?.let { "%.0f".format(it.h) } ?: "no hay"
    )
  }

  @Test fun el_alto_de_la_pagina_con_la_comparacion_a_393_con_letra_1_0() { println(compareHeight(393, 1f)) }
  @Test fun el_alto_de_la_pagina_con_la_comparacion_a_393_con_letra_1_3() { println(compareHeight(393, 1.3f)) }
  @Test fun el_alto_de_la_pagina_con_la_comparacion_a_393_con_letra_2_0() { println(compareHeight(393, 2f)) }
  @Test fun el_alto_de_la_pagina_con_la_comparacion_a_360_con_letra_1_0() { println(compareHeight(360, 1f)) }
  @Test fun el_alto_de_la_pagina_con_pocos_a_393_con_letra_1_0() { println(compareHeight(393, 1f, fewWorld)) }

  @Test fun a_393_con_letra_1_0_la_cifra_y_su_division_caben_en_la_primera_pantalla() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertTrue("la tarjeta de la cifra (con su división y su percentil) termina en %.0f dp, dentro de los 873 dp de la primera pantalla".format(b.getValue("contrarreloj_hero").b), b.getValue("contrarreloj_hero").b <= 873f)
    println("T203 con comparación a 393 con letra 1,0: la tarjeta de la cifra termina en %.0f dp y la escalera en %.0f dp".format(b.getValue("contrarreloj_hero").b, b.getValue("contrarreloj_ladder").b))
  }

  @Test fun los_textos_de_la_comparacion_pasan_4_5_a_1_sobre_su_fondo() {
    val cream = Color(0xFFFFEAD1)
    val fill = com.example.ui.screens.TimeTrialDoneFill
    assertEquals(0xD9CEFF, fill.toArgb() and 0xFFFFFF)
    assertTrue("la tinta sobre el lila de «Todavía somos pocos»: %.1f".format(contrast(Clay.Ink, fill)), contrast(Clay.Ink, fill) >= 4.5)
    assertTrue("la tinta apagada (el percentil, las notas) sobre la crema de la losa (#FFEAD1): %.1f".format(contrast(Clay.InkMuted, cream)), contrast(Clay.InkMuted, cream) >= 4.5)
    assertTrue("la tinta de la cifra sobre la crema: %.1f".format(contrast(Clay.Ink, cream)), contrast(Clay.Ink, cream) >= 4.5)
    assertTrue("la tinta sobre el lima de tu fila: %.1f".format(contrast(Clay.Ink, Clay.Lime)), contrast(Clay.Ink, Clay.Lime) >= 4.5)
    assertTrue("los rótulos de sección y la nota de muestra, crema sobre el noche plano", contrast(NubiTokens.OnNight, NubiTokens.NightFlat) >= 4.5)
    assertTrue("la campana lila sobre la ventanilla: %.1f".format(contrast(Clay.Grape, NubiTokens.NightWindow)), contrast(Clay.Grape, NubiTokens.NightWindow) >= 3.0)
    assertTrue("y la barra Tú, en sol, sobre la campana lila: %.1f".format(contrast(Clay.Sun, Clay.Grape)), contrast(Clay.Sun, Clay.Grape) >= 1.2)
    assertTrue("la barra de lo que falta (lima con borde de tinta) se separa de su carril sobre la crema: %.1f".format(contrast(Clay.Lime, cream)), contrast(Clay.Ink, cream) >= 3.0)
    println("T203 contraste: tinta sobre #D9CEFF %.1f · tinta apagada sobre la losa %.1f · tinta sobre lima %.1f · crema sobre el noche plano %.1f".format(contrast(Clay.Ink, fill), contrast(Clay.InkMuted, cream), contrast(Clay.Ink, Clay.Lime), contrast(NubiTokens.OnNight, NubiTokens.NightFlat)))
  }

  // ---------------------------------------------------------------- lector de pantalla y toques

  private fun textOf(tag: String): String =
    rule.onNode(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Text)?.joinToString("") { it.text } ?: error("«$tag» no tiene texto")

  private fun descriptionOf(tag: String): String =
    rule.onNode(hasTestTag(tag)).fetchSemanticsNode().config.getOrNull(SemanticsProperties.ContentDescription)?.joinToString(". ") ?: error("«$tag» no tiene descripción")

  @Test fun el_lector_de_pantalla_dice_las_barras_el_selector_y_las_cifras() {
    show()
    assertEquals("Tus últimas 10 marcas: 31, 35, 39, 36, 44 aciertos, tu récord, 38, 41, 37, 42 y 43 aciertos, la de hoy.", descriptionOf("contrarreloj_bars"))
    assertEquals("Tinta o Palabra, récord 44 aciertos, elegido", descriptionOf("contrarreloj_game_stroop"))
    assertEquals("¿Verdad o disparate?, récord 31 aciertos", descriptionOf("contrarreloj_game_disparate"))
    assertEquals("43 aciertos esta semana", descriptionOf("contrarreloj_stat_1"))
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
    assertTrue(page.contains("OnBoardText(TimeTrialMarks.TITLE") && page.contains("OnBoardText(TimeTrialMarks.subtitle("))
    // los títulos de las losas, en Rubik 24 (Tarea 203: la escala de cinco tamaños)
    val titles = Regex("""Text\(title(?:\.glueNumbers\(\))?, color = Clay\.Ink, fontSize = 24\.sp[^\n]*""").findAll(page).toList()
    assertTrue("el título de cada losa va en Rubik 24", titles.isNotEmpty() && titles.all { "TitleFamily" in it.value })
    assertTrue("el título de la página, en Rubik", Regex("""OnBoardText\(TimeTrialMarks\.TITLE[^\n]*""").find(page)!!.value.contains("TitleFamily"))
    // la escala de tamaños: 40 (la cifra), 24 (títulos), 18 (Fredoka), 16 y 14 (solo los rótulos de los ejes); el título de la página (26) es el de toda página de Avance
    val sizes = Regex("""fontSize = (\d+(?:\.\d+)?)\.sp""").findAll(page).map { it.groupValues[1] }.toSet()
    assertEquals("la página usa solo estos tamaños", setOf("14", "16", "18", "24", "26", "40"), sizes)
    // letra: solo los rótulos del eje bajan de 16 sp
    val small = Regex("""fontSize = (\d+(?:\.\d+)?)\.sp""").findAll(page).filter { it.groupValues[1].toFloat() < 16f }.map { it.groupValues[1] }.toList()
    assertTrue("solo el rótulo del eje (los dos textos y su medida), a 14 sp", small.isNotEmpty() && small.all { it == "14" })
    val axisFns = listOf("""private fun AxisLabels\(labels[\s\S]*?\n\}""", """private fun RhythmChart\([\s\S]*?\n\}""", """private fun BellAxis\([\s\S]*?\n\}""", """private fun WeeksAxis\([\s\S]*?\n\}""", """private fun PercentileWeeksChart\([\s\S]*?\n\}""")
    assertEquals("y están todos en las funciones de los ejes y rótulos (las marcas, el ritmo, la campana, las semanas y «la mitad»)", small.size, axisFns.sumOf { fn -> Regex(fn).find(page)!!.value.let { Regex("""fontSize = 14\.sp""").findAll(it).count() } })
    // la cifra grande: Rubik 40 con tope de escala 1,3
    assertTrue("la cifra grande va en Rubik 40 con tope de escala", Regex("""CapScale\(BIG_TEXT_SCALE_CAP\) \{\s*Text\([^\n]*fontSize = 40\.sp[^\n]*TitleFamily""").containsMatchIn(page))
    assertTrue("la ventanilla noche sale del token (`NubiTokens.NightWindow`), sin copiar el literal", page.contains("NubiTokens.NightWindow") && !Regex("0x[Ff][Ff]100[Ff]3[Aa]").containsMatchIn(page))
    assertEquals("la ventanilla es #100F3A", 0x100F3A, NubiTokens.NightWindow.toArgb() and 0xFFFFFF)
  }

  @Test fun la_pagina_no_usa_fuentes_nuevas_ni_colores_copiados() {
    val page = src("ui/screens/ContrarrelojPage.kt")
    val families = Regex("""fontFamily = (\w+)""").findAll(page).map { it.groupValues[1] }.toSet()
    assertTrue("solo las familias de hoy (Rubik y Fredoka): $families", families.all { it == "TitleFamily" || it == "AppFamily" })
    assertFalse("sin fuentes nuevas", Regex("""FontFamily\(|Font\(R\.font""").containsMatchIn(page))
    assertFalse("sin colores copiados: la lima, la tinta y la crema salen de Clay", Regex("""Color\(0x""").containsMatchIn(page))
  }

  // ---------------------------------------------------------------- Tarea 205: la referencia estimada (la que trae el APK y ve la tienda)

  /** La referencia estimada REAL del APK (los 4 JSON de `assets/referencia/`), como la ve la tienda: la fuente compuesta sin personas reales. */
  private val estWorld: Comparison.Source by lazy {
    val dir = File("src/main/assets/${com.example.data.EstimatedReference.ASSET_DIR}")
    Comparison.ComposedSource(null, com.example.data.EstimatedReference.of(dir.listFiles { f -> f.extension == "json" }.orEmpty().sortedBy { it.name }.map { it.readText(Charsets.UTF_8) }))
  }

  /** TODO lo que dice la pantalla, escrito y hablado: cada texto y cada descripción del árbol SIN fusionar, con los espacios duros como espacios. */
  private fun everythingSaid(): List<String> {
    val out = mutableListOf<String>()
    fun walk(node: SemanticsNode) {
      node.config.getOrNull(SemanticsProperties.Text)?.let { out += it.joinToString("") { s -> s.text } }
      node.config.getOrNull(SemanticsProperties.ContentDescription)?.let { out += it.joinToString(". ") }
      node.children.forEach(::walk)
    }
    walk(rule.onRoot(useUnmergedTree = true).fetchSemanticsNode())
    return out.map { it.replace(NBSP, " ") }
  }

  @Test fun con_la_referencia_estimada_la_frase_larga_va_una_sola_vez_bajo_el_rotulo_y_el_aviso_de_muestra_no_va() {
    show(compare = estWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertEquals("Esta semana, comparado con tu edad", n("contrarreloj_section_compare"))
    assertEquals("Referencia estimada para 45 a 54 años: sale de estudios por edad y de un programa que jugó esta misma pista en un computador, no en tu teléfono. No son personas.", n("contrarreloj_estimated_notice"))
    assertNull("ya no es un dato de muestra", b["contrarreloj_sample_notice"])
    assertEquals("la frase larga va bajo el rótulo, a 8 dp", 8f, b.getValue("contrarreloj_estimated_notice").y - b.getValue("contrarreloj_section_compare").b, 1.5f)
    assertEquals("y UNA sola vez en toda la pantalla", 1, everythingSaid().count { it.startsWith("Referencia estimada para") })
    assertEquals("las demás se dicen igual con otra franja", "Referencia estimada para 75 años o más: sale de estudios por edad y de un programa que jugó esta misma pista en un computador, no en tu teléfono. No son personas.", Comparison.estimatedNotice(AgeRange.R75_PLUS))
  }

  private fun assertNeverPeople(scale: Float) {
    show(scale = scale, compare = estWorld, ageRange = AgeRange.R45_54)
    val said = everythingSaid()
    val notice = Comparison.estimatedNotice(AgeRange.R45_54)
    val rest = said.filter { it != notice && !it.contains("Referencia estimada para") }
    for (text in rest) {
      val low = text.lowercase()
      assertFalse("con letra $scale, «$text» llama personas a la referencia", "personas" in low || "persona " in low || " gente" in low)
      for (gone in listOf("todas las edades", "todavía somos pocos", "mientras tanto", "las demás edades aparecen", "datos de muestra", "cuando lleguen", "personas reales")) assertFalse("con letra $scale, «$text» dice «$gone»", gone in low)
      // los robots que arman la tabla nunca se nombran ni se cuentan (Opus, 1-nov): ni «robot» ni «2.000»
      for (count in listOf("robot", "2000", "2.000", "2 000")) assertFalse("con letra $scale, «$text» dice «$count»", count in low)
    }
    assertTrue("la pantalla sí dice «referencia estimada»", said.any { "referencia estimada" in it.lowercase() })
    assertTrue("y dijo bastante (la prueba vio la página)", said.size > 40)
  }

  @Test fun la_referencia_estimada_nunca_se_llama_personas_ni_en_lo_escrito_ni_en_lo_hablado_con_letra_1_0() = assertNeverPeople(1f)
  @Test fun la_referencia_estimada_nunca_se_llama_personas_ni_en_lo_escrito_ni_en_lo_hablado_con_letra_2_0() = assertNeverPeople(2f)

  @Test fun en_cada_losa_la_referencia_se_dice_solo_referencia_estimada() {
    show(compare = estWorld, ageRange = AgeRange.R45_54)
    val said = everythingSaid()
    assertTrue("la cifra: «marcas de la referencia estimada para 45 a 54 años quedan detrás…»", said.any { it == "marcas de la referencia estimada para 45 a 54 años quedan detrás de tu mejor marca de esta semana en Tinta o Palabra." })
    assertTrue("la campana, en su frase", said.any { it == Comparison.ESTIMATED_BELL_CAPTION })
    assertTrue("«Otras edades», en su encabezado", said.any { it == "De cada 100 marcas de la referencia estimada de cada edad, tu mejor marca deja detrás a:" })
    assertTrue("las semanas, en su nota", said.any { it == Comparison.WEEKS_ESTIMATED_NOTE })
    assertEquals("y el percentil se dice como siempre", "Percentil ${Regex("""\d+""").find(n("contrarreloj_hero_percentile"))!!.value}", n("contrarreloj_hero_percentile"))
  }

  @Test fun con_la_referencia_estimada_no_hay_todas_las_edades_ni_la_linea_de_las_demas_edades() {
    show(compare = estWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertEquals("Otras edades", textOf("contrarreloj_others_title"))
    assertNull("sin fila «Todas las edades»", b["contrarreloj_others_all_label"])
    assertNull("sin la línea que dice que faltan", b["contrarreloj_others_hidden"])
    assertEquals("las seis franjas de 18 en adelante, cada una con su fila", 6, (0..5).count { b.containsKey("contrarreloj_others_${it}_label") })
    assertNull("sin «Todavía somos pocos»", b["contrarreloj_place_title"])
  }

  /** Los seis títulos con la referencia: el de la campana es el que decide el nombre («La campana estimada de tu edad» o «La campana de tu edad»). */
  private fun assertEstimatedTitlesInOneLine(widthDp: Int) {
    show(widthDp = widthDp, compare = estWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    for (tag in titleTags) assertTrue("«${textOf(tag)}» a $widthDp dp con letra 1,0 mide %.1f dp de alto (una línea de Rubik 24 mide 28)".format(b.getValue(tag).h), b.getValue(tag).h <= 28.5f)
  }

  @Test fun con_la_referencia_los_seis_titulos_de_losa_caben_en_una_linea_a_393_con_letra_1_0() = assertEstimatedTitlesInOneLine(393)
  @Test fun con_la_referencia_los_seis_titulos_de_losa_caben_en_una_linea_a_360_con_letra_1_0() = assertEstimatedTitlesInOneLine(360)

  @Test fun el_titulo_de_la_campana_con_la_referencia_es_el_de_siempre_y_su_frase_dice_referencia_estimada() {
    show(compare = estWorld, ageRange = AgeRange.R45_54)
    assertEquals("La campana de tu edad", textOf("contrarreloj_bell_title"))
    assertEquals("La línea marca la mitad de la referencia estimada.", textOf("contrarreloj_bell_caption"))
    assertFalse("«La campana estimada de tu edad» partía en dos líneas: no hay un texto así", Regex("\"La campana estimada").containsMatchIn(src("data/Comparison.kt")))
  }

  @Test fun comparacion_estimada_cero_choques_a_393_con_letra_1_0() = assertCompareNoClashes(393, 1f, estWorld, "referencia estimada")
  @Test fun comparacion_estimada_cero_choques_a_393_con_letra_1_3() = assertCompareNoClashes(393, 1.3f, estWorld, "referencia estimada")
  @Test fun comparacion_estimada_cero_choques_a_393_con_letra_2_0() = assertCompareNoClashes(393, 2f, estWorld, "referencia estimada")
  @Test fun comparacion_estimada_cero_choques_a_360_con_letra_1_0() = assertCompareNoClashes(360, 1f, estWorld, "referencia estimada")
  @Test fun comparacion_estimada_cero_choques_a_360_con_letra_1_3() = assertCompareNoClashes(360, 1.3f, estWorld, "referencia estimada")
  @Test fun comparacion_estimada_cero_choques_a_360_con_letra_2_0() = assertCompareNoClashes(360, 2f, estWorld, "referencia estimada")

  /** El eje y la marca «Tú» de la campana de [game] con la referencia: los rótulos caen donde `axisTicks` los pone, en el ancho de SU pista, y la barra «Tú» en el centro de su tramo. */
  private fun assertBellOfGame(game: String, mine: List<TimeTrialResult>, mark: Int) {
    show(compare = estWorld, ageRange = AgeRange.R45_54, selected = game, results = mine)
    val b = boxes()
    val d = estWorld.distribution(game, listOf(AgeRange.R45_54), 0, 1)
    val ticks = Comparison.axisTicks(d)
    val area = b.getValue("contrarreloj_bell_area")
    val shown = (0..3).filter { b.containsKey("contrarreloj_bell_axis_$it") }
    assertTrue("$game: el eje trae al menos «0 aciertos» y el último rótulo ($shown)", 0 in shown && 3 in shown)
    for (i in shown) {
      val label = b.getValue("contrarreloj_bell_axis_$i")
      val center = label.x + label.w / 2 - area.x
      val want = area.w * ticks[i] / d.topMark
      when (i) {
        0 -> assertEquals("$game: «0 aciertos» al borde izquierdo", 0f, label.x - area.x, 1.5f)
        3 -> assertEquals("$game: el último, al borde derecho", area.w, label.r - area.x, 1.5f)
        else -> assertEquals("$game: el rótulo $i (${ticks[i]}) sobre su lugar", want, center, 2.5f)
      }
    }
    val you = b.getValue("contrarreloj_bell_you_bar")
    assertEquals("$game: la barra «Tú» en el centro del tramo de la marca $mark", (area.w * Comparison.markX(mark, d.topMark) / d.topMark).toFloat(), you.x + you.w / 2 - area.x, 1.5f)
    assertTrue("$game: tiene su escalera", b.containsKey("contrarreloj_ladder_you") && b.keys.any { it.startsWith("contrarreloj_ladder_row_") })
  }

  @Test fun el_eje_y_la_barra_de_Tinta_o_Palabra_son_de_5_en_5_hasta_75() = assertBellOfGame("stroop", ten, 43)
  @Test fun el_eje_y_la_barra_de_Verdad_o_disparate_son_de_5_en_5_hasta_75() = assertBellOfGame("disparate", listOf(r(31, 27, game = "disparate")), 31)
  @Test fun el_eje_y_la_barra_de_Rescate_son_de_a_1_acierto_hasta_23() = assertBellOfGame("radar", listOf(r(14, 27, game = "radar")), 14)
  @Test fun el_eje_y_la_barra_de_Acoplamiento_son_de_a_2_aciertos_hasta_30() = assertBellOfGame("acoplamiento", listOf(r(24, 27, game = "acoplamiento")), 24)

  @Test fun un_juego_sin_tabla_de_su_pista_queda_sin_comparacion_como_la_pagina_de_antes() {
    val none = Comparison.ComposedSource(null, com.example.data.EstimatedReference.of(emptyList()))
    show(compare = none, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertNull(b["contrarreloj_section_compare"]); assertNull(b["contrarreloj_estimated_notice"]); assertNull(b["contrarreloj_hero"]); assertNull(b["contrarreloj_section_marks"])
    assertEquals("subtítulo de siempre, solo contigo", com.example.data.TimeTrialMarks.subtitle(Comparison.Kind.NONE), textOf("contrarreloj_subtitle"))
  }

  @Test fun de_13_a_17_anos_la_referencia_tampoco_se_muestra() {
    show(compare = estWorld, ageRange = AgeRange.R13_17)
    val b = boxes()
    assertNull(b["contrarreloj_estimated_notice"]); assertNull(b["contrarreloj_hero"]); assertNull(b["contrarreloj_section_compare"])
  }

  @Test fun el_subtitulo_con_la_referencia_dice_referencia_estimada_y_la_nota_no_habla_de_capacidad_ni_de_personas() {
    show(compare = estWorld, ageRange = AgeRange.R45_54)
    assertEquals(com.example.data.TimeTrialMarks.subtitle(Comparison.Kind.ESTIMATED), textOf("contrarreloj_subtitle"))
    val sub = textOf("contrarreloj_subtitle").lowercase()
    assertTrue("referencia estimada" in sub && "personas" !in sub)
  }
}
