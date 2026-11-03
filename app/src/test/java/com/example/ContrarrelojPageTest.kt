package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.luminance
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsNode
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Comparison
import com.example.data.NBSP
import com.example.data.SampleComparison
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialStats
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
  private fun r(mark: Int, day: Int, errors: Int = 2, game: String = "stroop", version: Int = com.example.data.TimeTrialDay.trackVersion(game)) =
    TimeTrialResult(id = "r-$game-$day-$mark", gameId = game, day = "2026-10-%02d".format(day), trackVersion = version, hits = mark + errors, errors = errors, level = 4, seconds = 120, timestamp = at(day))

  private val now = at(28, 18)

  /**
   * Desde la tercera partida GUARDADA de un juego se dice el lugar (Tarea 207): a las pruebas que miran la comparación con una o dos partidas se les suman dos anteriores y flojas (de la semana pasada, así no tocan la mejor marca de esta semana) hasta completar tres.
   */
  private fun withEarly(game: String, mine: List<TimeTrialResult>): List<TimeTrialResult> =
    if (Comparison.gamesPlayed(mine, game) >= Comparison.PLACE_FROM_GAME) mine else List(Comparison.PLACE_FROM_GAME - Comparison.gamesPlayed(mine, game)) { r(1, 20 + it, game = game) } + mine
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
  private var sourceCalls = 0
  private var playCalls = 0
  private var lockedCalls = 0

  private fun show(widthDp: Int = 393, scale: Float = 1f, results: List<TimeTrialResult> = ten + others, selected: String = "stroop", compare: Comparison.Source? = null, ageRange: AgeRange? = null, plus: Boolean = true, today: String? = "stroop") {
    rule.setContent {
      val base = LocalDensity.current
      density = base.density
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(873.dp)) {
            BoardBackground()
            ContrarrelojPage(results = results, selected = selected, onSelect = { selectedCalls += it }, now = now, lang = com.example.model.AppLanguage.SPANISH, compare = compare, ageRange = ageRange, onOpenSource = { sourceCalls += 1 }, plus = plus, today = today, onPlay = { playCalls += 1 }, onLocked = { lockedCalls += 1 })
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
  private val free = listOf("contrarreloj_free_(record|week)", "contrarreloj_hero_(division|place|emblem_\\w+)", "contrarreloj_ladder_(need|closes)")
  private val chart = listOf("contrarreloj_chart_(title|first)", "contrarreloj_figure_\\d+", "contrarreloj_coin", "contrarreloj_barshape_\\d+", "contrarreloj_axis_(left|right)", "contrarreloj_record_line", "contrarreloj_averages")
  private val notes = listOf("contrarreloj_rule", "contrarreloj_older_text", "contrarreloj_note")
  private val header = listOf("contrarreloj_title", "contrarreloj_subtitle")
  private val rhythm = listOf("contrarreloj_rhythm_(title|legend|sentence|wait)", "contrarreloj_rhythm_(fig|bar)_(hit|err)_\\d", "contrarreloj_rhythm_axis_(\\d|unit)")
  private val cards = listOf("contrarreloj_free", "contrarreloj_marks", "contrarreloj_rhythm", "contrarreloj_notes")
  private val games = listOf("stroop", "disparate", "radar", "acoplamiento")

  private fun groupOf(map: Map<String, Rect>, patterns: List<String>) = map.filterKeys { k -> patterns.any { Regex(it).matches(k) } }

  private fun assertNoClashes(widthDp: Int, scale: Float) {
    show(widthDp, scale)
    val all = boxes()
    val label = "a $widthDp dp con letra $scale"
    val groups = mutableMapOf("la cabecera" to groupOf(all, header), "lo gratis" to groupOf(all, free), "Tus últimas 10 marcas" to groupOf(all, chart), "Tu ritmo" to groupOf(all, rhythm), "las notas" to groupOf(all, notes))
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

  @Test fun a_393_con_letra_1_0_la_cabecera_el_selector_y_lo_gratis_caben_en_la_primera_pantalla() {
    show()
    val b = boxes()
    assertTrue("lo gratis termina en %.0f dp, dentro de los 873 dp de la primera pantalla".format(b.getValue("contrarreloj_free").b), b.getValue("contrarreloj_free").b <= 873f)
    assertTrue("y el botón «Jugar» se ve entero antes de la barra de abajo (88 dp): %.0f dp".format(b.getValue("btn_contrarreloj_play").b), b.getValue("btn_contrarreloj_play").b <= 873f - 88f)
    println("T215 alto de la página a 393 con letra 1,0: %.0f dp; lo gratis termina en %.0f dp, las 10 marcas en %.0f dp".format(pageHeight(), b.getValue("contrarreloj_free").b, b.getValue("contrarreloj_marks").b))
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
  private fun mergedText(tag: String) = rule.onNodeWithTag(tag).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Text)?.joinToString("") { it.text }.orEmpty().replace(NBSP, " ")

  @Test fun lo_gratis_dice_el_record_y_la_semana_en_frases_completas_y_las_tres_cifras_de_la_Tarea_203_ya_no_estan() {
    show()
    assertEquals("Tu récord: 44 aciertos.", n("contrarreloj_free_record"))
    assertEquals("Esta semana jugaste 3 partidas y la mejor fue de 43 aciertos.", n("contrarreloj_free_week"))
    assertEquals("Jugar · 2 minutos", mergedText("btn_contrarreloj_play"))
    assertEquals("para el lector, el botón dice también el juego (Tarea 215b, Fable A3)", "Jugar 2 minutos de Tinta o Palabra", rule.onNodeWithTag("btn_contrarreloj_play").fetchSemanticsNode().config.getOrNull(SemanticsProperties.ContentDescription)?.joinToString("").orEmpty())
    val b = boxes()
    assertNull("las tres cifras salieron: el récord y la semana ya los dice lo gratis", b["contrarreloj_stats"])
    assertEquals(0, b.keys.count { it.startsWith("contrarreloj_stat_") || it.startsWith("contrarreloj_stats_") })
    assertTrue("el botón primero, después el récord y la semana", b.getValue("btn_contrarreloj_play").b <= b.getValue("contrarreloj_free_record").y && b.getValue("contrarreloj_free_record").b <= b.getValue("contrarreloj_free_week").y)
    assertTrue("y todo dentro de la losa gratis", listOf("btn_contrarreloj_play", "contrarreloj_free_record", "contrarreloj_free_week").all { b.getValue(it).y >= b.getValue("contrarreloj_free").y && b.getValue(it).b <= b.getValue("contrarreloj_free").b })
  }

  @Test fun sin_partida_esta_semana_lo_gratis_lo_dice_y_sin_comparacion_no_hay_division() {
    show(results = listOf(r(31, 8)))                       // una sola partida, del 8 de octubre: nada de esta semana
    assertEquals("Esta semana todavía no jugaste.", n("contrarreloj_free_week"))
    assertEquals("pero el récord sí", "Tu récord: 31 aciertos.", n("contrarreloj_free_record"))
    val b = boxes()
    assertNull("sin comparación (la tienda sin franja, o sin tabla) lo gratis no trae la división", b["contrarreloj_hero_division"])
    assertNull(b["contrarreloj_ladder_need"]); assertNull(b["contrarreloj_ladder_closes"])
  }

  /**
   * Tarea 217 (Opus y Fable H3): a 393 dp «Todavía no tienes marcas en este juego.» dejaba «juego.» solo en el segundo renglón. Sin marcas no hay récord: la línea va en la letra del cuerpo (16, tinta) y con «este juego» pegado, así que, cuando pase a dos renglones,
   * el corte nunca deja «juego.» solo. Una composición por prueba.
   */
  private fun sinMarcasSinPalabraSola(width: Int, scale: Float) {
    show(widthDp = width, scale = scale, selected = "radar", results = listOf(r(31, 28)))
    val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
    rule.onNodeWithTag("contrarreloj_free_record", useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(results)
    val layout = results.first()
    val text = layout.layoutInput.text.text
    assertEquals("$width dp con letra $scale: es la frase de siempre", "Todavía no tienes marcas en este juego.", text.replace(NBSP, " "))
    assertEquals("$width dp con letra $scale: en la letra del cuerpo (16), no en la del récord (18 negrita)", 16f, layout.layoutInput.style.fontSize.value, 0.01f)
    assertTrue("$width dp con letra $scale: sin negrita", (layout.layoutInput.style.fontWeight?.weight ?: 400) < 600)
    assertTrue("«este juego» va pegado con espacio duro", "este${NBSP}juego." in text)
    for (line in 0 until layout.lineCount) {
      val seg = text.substring(layout.getLineStart(line), layout.getLineEnd(line)).trim()
      assertTrue("$width dp con letra $scale: el renglón ${line + 1} («$seg») no es «juego.» solo", seg != "juego.")
    }
    if (width == 393 && scale == 1f) assertEquals("a 393 dp con letra 1,0 cabe en UNA línea", 1, layout.lineCount)
  }

  @Test fun sin_marcas_no_deja_juego_solo_a_393_con_letra_1_0() = sinMarcasSinPalabraSola(393, 1f)
  @Test fun sin_marcas_no_deja_juego_solo_a_360_con_letra_1_0() = sinMarcasSinPalabraSola(360, 1f)
  @Test fun sin_marcas_no_deja_juego_solo_a_393_con_letra_1_3() = sinMarcasSinPalabraSola(393, 1.3f)
  @Test fun sin_marcas_no_deja_juego_solo_a_360_con_letra_1_3() = sinMarcasSinPalabraSola(360, 1.3f)
  @Test fun sin_marcas_no_deja_juego_solo_a_393_con_letra_2_0() = sinMarcasSinPalabraSola(393, 2f)
  @Test fun sin_marcas_no_deja_juego_solo_a_360_con_letra_2_0() = sinMarcasSinPalabraSola(360, 2f)

  @Test fun el_boton_Jugar_avisa_y_lo_gratis_nunca_va_en_gris() {
    show(plus = false)
    rule.onNodeWithTag("btn_contrarreloj_play").performClick(); rule.waitForIdle()
    assertEquals(1, playCalls)
    assertEquals("jugar el de hoy no abre la hoja del candado", 0, lockedCalls)
    val b = boxes()
    assertNull("lo gratis no es una losa de las grises", b["contrarreloj_free_lock"])
  }

  @Test fun las_cifras_grandes_y_la_frase_de_la_cifra_no_crecen_mas_alla_de_1_3_de_escala_aunque_la_letra_sea_2_0() {
    show(scale = 2f, compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    val hero = b.getValue("contrarreloj_hero_sentence").h
    // Tarea 213: la frase de la cifra es de 24 sp con línea de 30 sp y el mismo tope: cada renglón mide como mucho 30 × 1,3 = 39 dp (sin tope, con letra 2,0 serían 60 dp por renglón y más de ocho renglones)
    assertTrue("con letra 2,0 la frase de la cifra mide como mucho seis renglones de 39 dp (%.1f dp)".format(hero), hero <= 6 * 39f + 1f)
    assertTrue("y el código la pone dentro de CapScale", Regex("""CapScale\(BIG_TEXT_SCALE_CAP\) \{\s+Text\(Comparison\.heroSentence""").containsMatchIn(src("ui/screens/ContrarrelojPage.kt")))
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
    assertEquals("con una sola partida, en singular; y si fue el récord, sin repetir el número y sin «la mejor» (Tarea 215b, Fable A4 b y M5; Tarea 217, Fable M3)", "Esta semana jugaste 1 partida y fue tu récord.", n("contrarreloj_free_week"))
    assertEquals("Tu récord: 31 aciertos.", n("contrarreloj_free_record"))
    assertTrue("la línea del récord sí está: «${n("contrarreloj_record_line")}»", n("contrarreloj_record_line").let { it.startsWith("Tu récord es del ") && it.endsWith("llegaste al paso 4 de 5 de la pista.") })
  }

  @Test fun un_juego_sin_marcas_dice_que_falta_y_no_muestra_cifras_ni_ritmo() {
    show(selected = "radar", results = listOf(r(31, 28)))
    // Tarea 215b (Fable A4 c): sin marcas, lo gratis dice UNA línea; ni la de la semana ni la losa vacía de antes
    assertEquals("Todavía no tienes marcas en este juego.", n("contrarreloj_free_record"))
    val b = boxes()
    assertNull(b["contrarreloj_free_week"]); assertNull(b["contrarreloj_empty"]); assertNull(b["contrarreloj_record_empty"])
    assertNull(b["contrarreloj_stats"]); assertNull(b["contrarreloj_marks"])
    assertNull("sin marcas en el juego no hay losa de ritmo", b["contrarreloj_rhythm"])
    assertNotNull("la nota de siempre sigue al pie", b["contrarreloj_note"])
    assertNull("y la regla de la marca no, porque no hay marca", b["contrarreloj_rule"])
    assertEquals("sin marca aún", textOf("contrarreloj_game_value_radar"))
  }

  // ---------------------------------------------------------------- orden, líneas, ventanilla noche y «Tu ritmo»

  @Test fun el_orden_de_las_losas_es_selector_gratis_marcas_ritmo_y_notas() {
    show()
    val b = boxes()
    assertTrue("el selector va arriba de lo gratis", b.getValue("contrarreloj_game_radar").b < b.getValue("contrarreloj_free").y)
    for ((a, c) in listOf("contrarreloj_free", "contrarreloj_marks", "contrarreloj_rhythm", "contrarreloj_notes").zipWithNext()) {
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
    assertNotNull("y la página sigue entera: lo gratis y las marcas están", b["contrarreloj_free"]); assertNotNull(b["contrarreloj_marks"])
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
    assertEquals("lo gratis dice la semana sin depender de otras personas", "Esta semana jugaste 3 partidas y la mejor fue de 43 aciertos.", n("contrarreloj_free_week"))
  }

  @Test fun con_datos_de_muestra_la_comparacion_va_primero_y_tus_marcas_al_final_con_su_rotulo() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertEquals("Esta semana, comparado con tu edad", textOf("contrarreloj_section_compare"))
    assertEquals("Tus marcas", textOf("contrarreloj_section_marks"))
    assertEquals("Estos son datos de muestra: solo se ven en la versión de prueba.", textOf("contrarreloj_sample_notice"))
    val order = listOf("contrarreloj_game_radar", "contrarreloj_free", "contrarreloj_section_compare", "contrarreloj_hero", "contrarreloj_ladder", "contrarreloj_bell", "contrarreloj_others", "contrarreloj_percweeks", "contrarreloj_section_marks", "contrarreloj_marks", "contrarreloj_rhythm", "contrarreloj_notes", "contrarreloj_sample_notice")
    for ((a, c) in order.zipWithNext()) assertTrue("«$a» va antes que «$c»", b.getValue(a).b <= b.getValue(c).y + 0.5f)
    assertEquals("24 dp de aire sobre el primer rótulo: 16 de entre losas y 8 propios", 24f, b.getValue("contrarreloj_section_compare").y - b.getValue("contrarreloj_free").b, 1.5f)
    assertEquals("y sobre el segundo", 24f, b.getValue("contrarreloj_section_marks").y - b.getValue("contrarreloj_percweeks").b, 1.5f)
    for (key in listOf("compare", "marks")) {
      val label = b.getValue("contrarreloj_section_$key"); val line = b.getValue("contrarreloj_section_${key}_line")
      assertEquals("la línea mide 1 dp", 1f, line.h, 0.1f)
      assertTrue("y va desde el final del texto hasta el margen derecho (16 dp)", line.x >= label.r + 11.5f && Math.abs(line.r - (393f - 16f)) < 1f)
    }
    // Tarea 211: el aviso de muestra va AL FINAL de la página, después de las notas y no bajo el rótulo de la comparación (que queda solo, con la cifra debajo)
    assertTrue("el aviso de muestra va después de las notas (%.1f dp de aire)".format(b.getValue("contrarreloj_sample_notice").y - b.getValue("contrarreloj_notes").b), b.getValue("contrarreloj_sample_notice").y - b.getValue("contrarreloj_notes").b >= 7.5f)
    assertEquals("y el rótulo de la comparación queda solo sobre la cifra, a 16 dp", 16f, b.getValue("contrarreloj_hero").y - b.getValue("contrarreloj_section_compare").b, 1.5f)
    assertEquals("es lo último de la página", b.values.maxOf { it.b }, b.getValue("contrarreloj_sample_notice").b, 0.5f)
  }

  @Test fun el_rotulo_de_seccion_y_la_nota_de_muestra_van_en_fredoka_18_y_en_crema_de_16_sp_en_el_codigo() {
    val page = src("ui/screens/ContrarrelojPage.kt")
    assertTrue("el rótulo va en Fredoka negrita 18 sp con OnNight", Regex("""Text\(text, color = NubiTokens\.OnNight, fontSize = 18\.sp, fontWeight = FontWeight\.Bold, fontFamily = AppFamily""").containsMatchIn(page))
    assertTrue("la línea del rótulo es OnNight al 35 %", page.contains("NubiTokens.OnNight.copy(alpha = 0.35f)"))
    assertTrue("el aviso de los datos de muestra, en OnNight de 16 sp", Regex("""Text\(Comparison\.SAMPLE_NOTICE\.glueNumbers\(\)\.glueNamesForLetter\(\), color = NubiTokens\.OnNight, fontSize = 16\.sp""").containsMatchIn(page))
    assertTrue("y la fila «Con qué te comparas ›» sale de Comparison y va en SunOnNight", page.contains("label = Comparison.COMPARE_LINK") && page.contains("NubiTokens.SunOnNight, \"contrarreloj_source_link\""))
    assertFalse("la frase larga de la Tarea 211 ya no está en la página", page.contains("estimatedNotice") || page.contains("ReferenceFooter"))
    assertTrue("el recuadro de «Todavía somos pocos» usa el lila de «Marca nueva» (TimeTrialDoneFill), sin copiar el literal", page.contains("background(TimeTrialDoneFill)") && !Regex("0x[Ff][Ff][Dd]9[Cc][Ee][Ff][Ff]").containsMatchIn(page))
  }

  // ---- la cifra protagonista

  /** La frase de la cifra, ENTERA y en una sola pieza (Tarea 213: «Con 43 aciertos, superas al 63 % de …»), dentro de su losa y con letra grande. */
  private fun assertHeroReading(width: Int, scale: Float) {
    show(widthDp = width, scale = scale, compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("a $width dp con letra $scale", "Con 43 aciertos, superas al 63 % de las personas de tu edad que jugaron esta semana.", n("contrarreloj_hero_sentence"))
    val b = boxes(); val sentence = b.getValue("contrarreloj_hero_sentence"); val card = b.getValue("contrarreloj_hero")
    assertTrue("a $width dp con letra $scale la frase entra en la losa con sus 16 dp de relleno (x %.1f a %.1f; losa %.1f a %.1f)".format(sentence.x, sentence.r, card.x, card.r), sentence.x >= card.x + 15.5f && sentence.r <= card.r - 15.5f)
    assertTrue("y mide como mucho siete renglones de 39 dp (%.1f dp)".format(sentence.h), sentence.h <= 7 * 39f + 1f)
    assertNull("ya no hay «de cada 100»: ni cifra suelta ni frase de abajo", b["contrarreloj_hero_figure"] ?: b["contrarreloj_hero_caption"])
  }

  @Test fun la_cifra_es_una_frase_con_los_aciertos_y_el_porcentaje_y_la_division_y_su_lugar_viven_en_lo_gratis() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("Con 43 aciertos, superas al 63 % de las personas de tu edad que jugaron esta semana.", n("contrarreloj_hero_sentence"))
    assertTrue("«63 %» y «43 aciertos» van con espacio duro: no se parten", textOf("contrarreloj_hero_sentence").let { it.contains("63${NBSP}%") && it.contains("43${NBSP}aciertos") })
    assertEquals("En órbita", n("contrarreloj_hero_division"))
    assertEquals("La tercera de cinco divisiones.", n("contrarreloj_hero_place"))
    assertEquals("el lector de pantalla: la frase de Plus, sola (la división la dice lo gratis)", "Con 43 aciertos, superas al 63 % de las personas de tu edad que jugaron esta semana.", descriptionOf("contrarreloj_hero"))
    assertEquals("y lo gratis dice la división y su lugar", "En órbita. La tercera de cinco divisiones.", descriptionOf("contrarreloj_hero_division_line"))
    assertFalse("Tarea 209: «percentil» ya no se dice ni se ve", descriptionOf("contrarreloj_hero").lowercase().contains("percentil"))
    val b = boxes()
    assertNull("la clase normal no dice «Juntamos dos franjas»", b["contrarreloj_hero_scope"])
    val sentence = b.getValue("contrarreloj_hero_sentence"); val div = b.getValue("contrarreloj_hero_division"); val place = b.getValue("contrarreloj_hero_place")
    assertNull("ya no hay cifra suelta ni frase de abajo", b["contrarreloj_hero_figure"]); assertNull(b["contrarreloj_hero_caption"])
    assertNull("ya no hay «Percentil 63» a la derecha", b["contrarreloj_hero_percentile"])
    assertTrue("de arriba abajo: la división (con su emblema) y su lugar, en lo gratis, y después la frase de Plus", div.b <= place.y && place.b <= sentence.y)
    assertTrue("la división es de la losa gratis", div.y >= b.getValue("contrarreloj_free").y && place.b <= b.getValue("contrarreloj_free").b)
    assertEquals("el lugar va justo bajo la división, a 4 dp, alineado a la izquierda", 4f, place.y - div.b, 1.5f)
    assertEquals(div.x, place.x, 0.5f)
    assertEquals("la frase es la primera pieza de la losa, a 16 dp del borde de arriba", b.getValue("contrarreloj_hero").y + 16f, sentence.y, 1.5f)
    assertEquals("la tarjeta llena el ancho de la losa y no tiene título", 393f - 32f, b.getValue("contrarreloj_hero").w, 1f)
    assertNull(b["contrarreloj_hero_title"])
  }

  @Test fun con_la_referencia_estimada_la_frase_de_la_cifra_dice_referencia_estimada_para_tu_edad_y_nunca_personas() {
    show(compare = estWorld, ageRange = AgeRange.R45_54)
    val sentence = n("contrarreloj_hero_sentence")
    assertTrue("«$sentence»", Regex("""Con \d+ aciertos?, superas al \d+ % de la referencia estimada para tu edad\.""").matches(sentence))
    assertFalse("«$sentence» llama personas a la referencia", "persona" in sentence.lowercase())
    assertTrue("el lector la dice también, completa y antes de la división", descriptionOf("contrarreloj_hero").startsWith(sentence))
  }

  @Test fun la_frase_de_la_cifra_se_lee_entera_a_393_con_letra_1_0() = assertHeroReading(393, 1f)
  @Test fun la_frase_de_la_cifra_se_lee_entera_a_393_con_letra_1_3() = assertHeroReading(393, 1.3f)
  @Test fun la_frase_de_la_cifra_se_lee_entera_a_393_con_letra_2_0() = assertHeroReading(393, 2f)
  @Test fun la_frase_de_la_cifra_se_lee_entera_a_360_con_letra_1_0() = assertHeroReading(360, 1f)
  @Test fun la_frase_de_la_cifra_se_lee_entera_a_360_con_letra_1_3() = assertHeroReading(360, 1.3f)
  @Test fun la_frase_de_la_cifra_se_lee_entera_a_360_con_letra_2_0() = assertHeroReading(360, 2f)

  @Test fun con_pocas_personas_en_la_franja_la_cifra_dice_la_clase_que_uso() {
    // 75 o más: 38 personas esta semana → sus 4 semanas
    show(compare = manyWorld, ageRange = AgeRange.R75_PLUS)
    assertTrue("«${n("contrarreloj_hero_sentence")}»", n("contrarreloj_hero_sentence").let { it.startsWith("Con 43 aciertos, superas al ") && it.endsWith(" % de las personas de tu edad que jugaron en las últimas 4 semanas.") })
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

  @Test fun lo_que_falta_su_barra_y_el_domingo_viven_en_lo_gratis_y_la_escalera_sigue_con_las_semanas_seguidas() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("Te faltan 7 aciertos para En la Luna.", n("contrarreloj_ladder_need"))
    assertEquals("La semana cierra el domingo.", n("contrarreloj_ladder_closes"))
    assertEquals("Llevas 3 semanas seguidas En órbita o más arriba.", n("contrarreloj_ladder_streak"))
    assertEquals("Con las 128 personas de tu franja que jugaron esta semana.", n("contrarreloj_ladder_note"))
    val b = boxes()
    val free = listOf("contrarreloj_hero_division", "contrarreloj_hero_place", "contrarreloj_divider_free", "contrarreloj_ladder_need", "contrarreloj_ladder_bar", "contrarreloj_ladder_closes")
    // (la línea fina de lo gratis va ANTES de la división)
    val order = listOf("contrarreloj_divider_free", "contrarreloj_hero_division", "contrarreloj_hero_place", "contrarreloj_ladder_need", "contrarreloj_ladder_bar", "contrarreloj_ladder_closes")
    for ((a, c) in order.zipWithNext()) assertTrue("«$a» va antes que «$c»", b.getValue(a).b <= b.getValue(c).y + 0.5f)
    for (t in free) assertTrue("«$t» es de la losa gratis", b.getValue(t).y >= b.getValue("contrarreloj_free").y && b.getValue(t).b <= b.getValue("contrarreloj_free").b)
    val order2 = listOf("contrarreloj_ladder_row_4", "contrarreloj_divider_ladder", "contrarreloj_ladder_streak", "contrarreloj_ladder_note")
    for ((a, c) in order2.zipWithNext()) assertTrue("«$a» va antes que «$c»", b.getValue(a).b <= b.getValue(c).y + 0.5f)
    for (t in listOf("contrarreloj_ladder_streak", "contrarreloj_ladder_note", "contrarreloj_divider_ladder")) assertTrue("«$t» es de la escalera", b.getValue(t).y >= b.getValue("contrarreloj_ladder").y)
    assertNull("la escalera ya no repite lo que falta ni el domingo: viven una sola vez", b.keys.firstOrNull { it == "contrarreloj_ladder_need" && b.getValue(it).y >= b.getValue("contrarreloj_ladder").y })
    val bar = b.getValue("contrarreloj_ladder_bar"); val fill = b.getValue("contrarreloj_ladder_bar_fill")
    assertEquals("la barra mide 16 dp", 16f, bar.h, 0.6f)
    assertEquals("va llena en 4 de los 11 aciertos del tramo de En órbita a En la Luna", 4f / 11f, fill.w / bar.w, 0.02f)
    assertEquals("la barra llena el ancho útil de la losa gratis", b.getValue("contrarreloj_free").w - 32f, bar.w, 1f)
    assertEquals("la línea de separación mide 1 dp", 1f, b.getValue("contrarreloj_divider_ladder").h, 0.1f)
  }

  @Test fun en_la_division_mas_alta_no_falta_nada_y_no_hay_barra() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54, results = withEarly("stroop", listOf(r(70, 28))))
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

  @Test fun tu_lugar_semana_a_semana_dibuja_las_semanas_con_marca_de_la_mas_vieja_a_esta() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("Semana a semana", textOf("contrarreloj_percweeks_title").replace(NBSP, " "))
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
    assertTrue("el lector de pantalla dice «superas al N %», la misma forma que la cifra (Tarea 213)", descriptionOf("contrarreloj_percweeks_window").let { "superas al " in it && " % de " in it && "deja detrás" !in it && "de cada 100" !in it })
    assertEquals("hace 3 semanas", Comparison.weeksAxisLeft(points))
    assertTrue("el gráfico llena el ancho de la losa y las semanas sin dato no dejan huecos a la izquierda", b.getValue("contrarreloj_percweeks_window").w > 300f && b.getValue("contrarreloj_percweeks_bar_3").x < b.getValue("contrarreloj_percweeks_window").x + 60f)
  }

  @Test fun la_semana_de_lo_gratis_con_comparacion_sigue_siendo_de_la_persona_y_no_de_otras() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("Esta semana jugaste 3 partidas y la mejor fue de 43 aciertos.", n("contrarreloj_free_week"))
    assertEquals("Tu récord: 44 aciertos.", n("contrarreloj_free_record"))
    assertTrue("«3 de 4» ya no sale: la frase de semana a semana lo dice una vez", allTexts().none { "semanas arriba de la mitad" in it })
  }

  @Test fun otras_edades_muestra_solo_las_franjas_con_50_personas_la_propia_en_negrita_y_todas_las_edades() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("Otras edades", textOf("contrarreloj_others_title"))
    assertEquals("Tu mejor marca de esta semana, frente a las personas que jugaron Tinta o Palabra esta semana:", textOf("contrarreloj_others_lead").replace(NBSP, " "))
    val labels = (0..4).map { n("contrarreloj_others_${it}_label") }
    assertEquals(listOf("18 a 29 años", "30 a 44 años", "45 a 54 años (tu franja)", "55 a 64 años", "65 a 74 años"), labels)
    assertTrue("ninguna fila de 14 a 17 años (comparte la tabla de 18 a 29) ni de 75 o más (tiene 38 personas: no hay fila de relleno)", labels.none { "14 a 17" in it || "75" in it })
    assertEquals("Supera al 63 %", n("contrarreloj_others_2_value"))
    assertEquals("Todas las edades", n("contrarreloj_others_all_label"))
    assertEquals("Las demás edades aparecen cuando lleguen a 50 personas.", textOf("contrarreloj_others_hidden"))
    assertTrue("no hay fila de 14 a 17 años ni «todavía somos pocos» en la losa", allTexts().none { "14 a 17" in it || it == "todavía somos pocos" })
    val b = boxes()
    assertNull(b["contrarreloj_others_5"])
    assertTrue("una línea entre las franjas y «Todas las edades»", b.getValue("contrarreloj_divider_others").y > b.getValue("contrarreloj_others_4").b && b.getValue("contrarreloj_divider_others").b < b.getValue("contrarreloj_others_all").y)
    assertTrue("y la línea que cierra va al final", b.getValue("contrarreloj_others_hidden").y > b.getValue("contrarreloj_others_all").b)
    assertEquals("45 a 54 años (tu franja), Supera al 63 %", descriptionOf("contrarreloj_others_2").replace(NBSP, " "))
    assertTrue("el lector de pantalla dice lo mismo: la línea que cierra es texto de la página", allTexts().contains("Las demás edades aparecen cuando lleguen a 50 personas."))
  }

  /** Con letra de 1,3 o menos (Tarea 203b) las franjas, los nombres de división y «Te faltan N aciertos» van pegados con espacios duros. */
  private fun assertGluedNames(scale: Float) {
    show(compare = manyWorld, ageRange = AgeRange.R45_54, scale = scale)
    assertTrue("la franja entera en la fila propia de «Otras edades»", textOf("contrarreloj_others_2_label").contains("45${NBSP}a${NBSP}54${NBSP}años"))
    assertTrue("y «Te faltan 7 aciertos» con «En la Luna» enteros", textOf("contrarreloj_ladder_need").let { it.contains("Te${NBSP}faltan${NBSP}7${NBSP}aciertos") && it.contains("En${NBSP}la${NBSP}Luna") })
    assertTrue("los nombres de las filas de la escalera", textOf("contrarreloj_ladder_row_1_label").contains("En${NBSP}la${NBSP}Luna") && textOf("contrarreloj_ladder_you_name").contains("En${NBSP}órbita"))
    assertTrue("y las franjas de otras edades", textOf("contrarreloj_others_0_label").contains("18${NBSP}a${NBSP}29${NBSP}años"))
    assertEquals("pero lo que lee el lector de pantalla queda con espacios normales", "En órbita, Tú · 43 aciertos", descriptionOf("contrarreloj_ladder_you"))
    assertTrue("la descripción de la cifra no lleva espacios duros", !descriptionOf("contrarreloj_hero").contains(NBSP))
  }

  @Test fun las_franjas_y_las_divisiones_no_se_parten_con_espacios_duros_con_letra_1_0() = assertGluedNames(1f)
  @Test fun las_franjas_y_las_divisiones_no_se_parten_con_espacios_duros_con_letra_1_3() = assertGluedNames(1.3f)

  /** Con letra mayor a 1,3 (Tarea 205c) los nombres de división y «Te faltan» NO se pegan (pegados no caben en el renglón y Android los corta por letras); «7 aciertos» y la franja de edad sí quedan juntos. */
  @Test fun con_letra_2_0_los_nombres_de_division_y_Te_faltan_parten_entre_palabras_pero_la_cifra_con_su_unidad_y_la_franja_siguen_juntas() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54, scale = 2f)
    assertTrue("la franja entera sigue pegada en la fila propia de «Otras edades»", textOf("contrarreloj_others_2_label").contains("45${NBSP}a${NBSP}54${NBSP}años"))
    val need = textOf("contrarreloj_ladder_need")
    assertTrue("«7 aciertos» no se separa: $need", need.contains("7${NBSP}aciertos"))
    assertFalse("«Te faltan» parte entre palabras: $need", need.contains("Te${NBSP}faltan") || need.contains("faltan${NBSP}7"))
    assertFalse("y «En la Luna» también: $need", need.contains("En${NBSP}la") || need.contains("la${NBSP}Luna"))
    assertFalse("los nombres de las filas de la escalera tampoco", textOf("contrarreloj_ladder_row_1_label").contains(NBSP) || textOf("contrarreloj_ladder_you_name").contains("En${NBSP}órbita"))
    assertTrue("las franjas de otras edades siguen pegadas", textOf("contrarreloj_others_0_label").contains("18${NBSP}a${NBSP}29${NBSP}años"))
    assertEquals("lo que lee el lector de pantalla queda igual", "En órbita, Tú · 43 aciertos", descriptionOf("contrarreloj_ladder_you"))
  }

  @Test fun en_el_mundo_de_pocos_no_hay_cifra_ni_escalera_ni_te_faltan_y_dice_la_cuenta_real() {
    show(compare = fewWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertEquals("Tu lugar esta semana", textOf("contrarreloj_place_title"))
    assertEquals("Todavía somos pocos: 23 personas de 45 a 54 años jugaron Tinta o Palabra esta semana. Cuando sean 50, verás aquí tu lugar entre ellas.", textOf("contrarreloj_few_note").replace(NBSP, " "))
    assertEquals("23 de 50 personas", textOf("contrarreloj_few_bar_value").replace(NBSP, " "))
    assertTrue("«Mientras tanto…» con todas las edades de ESTA semana (llegan a 50)", textOf("contrarreloj_few_meanwhile").replace(NBSP, " ").startsWith("Mientras tanto, comparada con todas las edades de esta semana, tu mejor marca, 43 aciertos, supera al "))
    for (tag in listOf("contrarreloj_hero", "contrarreloj_ladder", "contrarreloj_bell", "contrarreloj_bell_window", "contrarreloj_ladder_need", "contrarreloj_percweeks")) assertNull("con pocas personas no existe «$tag» (nada de umbrales ni «te faltan» inventados)", b[tag])
    assertTrue("la barra llena menos de la mitad (23 de 50)", b.getValue("contrarreloj_few_bar_bar").w < 0.6f * b.getValue("contrarreloj_few_bar").w)
    assertTrue("una línea separa la barra de «Mientras tanto»", b.getValue("contrarreloj_divider_few").y > b.getValue("contrarreloj_few_bar").b && b.getValue("contrarreloj_divider_few").b < b.getValue("contrarreloj_few_meanwhile").y)
    assertEquals("de las franjas solo llega a 50 la de 18 a 29 años (70 personas); las demás no tienen fila y la línea que cierra lo dice", listOf("18 a 29 años"), (0..5).mapNotNull { i -> b["contrarreloj_others_${i}_label"]?.let { n("contrarreloj_others_${i}_label") } })
    assertEquals("Las demás edades aparecen cuando lleguen a 50 personas.", textOf("contrarreloj_others_hidden"))
    assertEquals("la fila de todas las edades sí llega a 50", "Todas las edades", n("contrarreloj_others_all_label"))
    assertEquals("sin semanas con 50 personas lo gratis dice igual las partidas de la persona", "Esta semana jugaste 3 partidas y la mejor fue de 43 aciertos.", n("contrarreloj_free_week"))
    assertTrue("y al final de la página siguen «Tus marcas»", b.getValue("contrarreloj_section_marks").y > b.getValue("contrarreloj_others").b)
  }

  @Test fun sin_marca_esta_semana_la_losa_dice_que_hacer_y_no_hay_otras_edades_ni_escalera() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54, selected = "radar", results = withEarly("radar", listOf(r(27, 23, game = "radar"))))
    assertEquals("Todavía no tienes una marca esta semana en Rescate relámpago. Juega su Contrarreloj para ver tu lugar entre personas de tu edad.", textOf("contrarreloj_place_nomark"))
    val b = boxes()
    for (tag in listOf("contrarreloj_others", "contrarreloj_hero", "contrarreloj_ladder", "contrarreloj_bell")) assertNull("sin marca esta semana no existe «$tag»", b[tag])
    assertNotNull("pero las semanas anteriores con marca sí se cuentan (la del 23 de octubre)", b["contrarreloj_percweeks"])
  }

  @Test fun quien_tiene_de_14_a_17_anos_ve_la_comparacion_completa_tambien_con_datos_de_muestra() {
    show(compare = manyWorld, ageRange = AgeRange.R14_17)
    val b = boxes()
    for (tag in listOf("contrarreloj_section_compare", "contrarreloj_hero", "contrarreloj_ladder", "contrarreloj_bell", "contrarreloj_section_marks")) assertNotNull("de 14 a 17 años existe «$tag» (Tarea 216; «Semana a semana» no: la muestra de depuración trae 41 personas de esa franja por semana)", b[tag])
    assertEquals("Otras edades: seis filas, ninguna propia ni de 14 a 17", true, b.keys.none { it.startsWith("contrarreloj_others_") && it.endsWith("_label") && "14 a 17" in n(it) })
  }

  @Test fun sin_franja_elegida_los_datos_de_muestra_usan_45_a_54() {
    show(compare = manyWorld, ageRange = null)
    assertEquals("Con 43 aciertos, superas al 63 % de las personas de tu edad que jugaron esta semana.", n("contrarreloj_hero_sentence"))
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
    assertTrue(meanwhile.startsWith("Mientras tanto, comparada con todas las edades de esta semana, tu mejor marca, 43 aciertos, supera al "))
    val n = Regex("supera al (\\d+) % de las personas\\.").find(meanwhile)!!.groupValues[1]
    assertEquals("la fila «Todas las edades» dice el MISMO número", "Supera al $n %", textOf("contrarreloj_others_all_value").replace(NBSP, " "))
    // y en toda la pantalla, lo que habla de «todas las edades» con un número dice siempre ese
    val numbers = allTexts().filter { "todas las edades" in it.lowercase() }.flatMap { s -> Regex("(\\d+) %").findAll(s).map { it.groupValues[1] }.toList() }
    assertTrue("todos los números de «todas las edades» son $n: $numbers", numbers.size >= 2 && numbers.all { it == n })
  }

  @Test fun en_muy_pocos_mientras_tanto_usa_las_4_semanas_lo_dice_y_la_fila_todas_las_edades_dice_todavia_somos_pocos() {
    show(compare = veryFewWorld, ageRange = AgeRange.R45_54)
    val meanwhile = textOf("contrarreloj_few_meanwhile").replace(NBSP, " ")
    assertTrue("«$meanwhile»", meanwhile.startsWith("Mientras tanto, comparada con todas las edades de las últimas 4 semanas, tu mejor marca de esta semana, 43 aciertos, supera al "))
    assertNull("todas las edades de ESTA semana no llega a 50 y ninguna franja tampoco: «Otras edades» no existe", boxes()["contrarreloj_others"])
    assertTrue("y ninguna fila de todas las edades da otro número", allTexts().none { it.startsWith("Todas las edades,") && " %" in it })
        assertEquals("la frase enmarcada sigue con la cuenta real de la franja", true, textOf("contrarreloj_few_note").replace(NBSP, " ").startsWith("Todavía somos pocos: 23 personas de 45 a 54 años"))
  }

  @Test fun con_la_comparacion_el_subtitulo_y_la_nota_ya_no_dicen_solo_contigo() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    assertEquals("El Contrarreloj define tu lugar de la semana.", textOf("contrarreloj_subtitle"))
    assertEquals("Es el resultado de un juego. No es una medida de capacidad.", textOf("contrarreloj_note"))
    val saysOnlyYou = allTexts().filter { "solo contigo" in it.lowercase() }
    assertTrue("con la comparación ningún texto visible ni leído dice «solo contigo»: $saysOnlyYou", saysOnlyYou.isEmpty())
  }

  @Test fun con_la_comparacion_en_pocos_tampoco_dice_solo_contigo() {
    show(compare = fewWorld, ageRange = AgeRange.R45_54)
    assertEquals("El Contrarreloj define tu lugar de la semana.", textOf("contrarreloj_subtitle"))
    assertTrue(allTexts().none { "solo contigo" in it.lowercase() })
  }

  @Test fun sin_la_comparacion_la_cabecera_y_la_nota_quedan_como_hoy() {
    show(compare = null, ageRange = AgeRange.R45_54)          // la build de la tienda
    assertEquals("Tus marcas, comparadas solo contigo.", textOf("contrarreloj_subtitle"))
    assertEquals("Es el resultado de un juego, comparado solo contigo. No es una medida de capacidad.", textOf("contrarreloj_note"))
  }

  @Test fun de_14_a_17_anos_la_cabecera_y_la_nota_son_las_de_cualquiera_con_comparacion() {
    show(compare = manyWorld, ageRange = AgeRange.R14_17)
    assertEquals("El Contrarreloj define tu lugar de la semana.", textOf("contrarreloj_subtitle"))
    assertEquals("Es el resultado de un juego. No es una medida de capacidad.", textOf("contrarreloj_note"))
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

  private val hero = listOf("contrarreloj_hero_(sentence|scope|division|place)", "contrarreloj_hero_emblem_[A-Z]+")
  private val ladder = listOf("contrarreloj_ladder_(title|need|closes|streak|note)", "contrarreloj_ladder_row_\\d_(label|value)", "contrarreloj_ladder_you_(name|value)", "contrarreloj_ladder_bar", "contrarreloj_ladder_(you_)?emblem(_\\d)?")
  private val bell = listOf("contrarreloj_bell_(title|caption|you|coin|you_bar|median)", "contrarreloj_bell_axis_\\d")
  private val place = listOf("contrarreloj_place_(title|nomark)", "contrarreloj_few_(note|meanwhile)", "contrarreloj_few_bar_(bar|value)")
  private val weeksPercentile = listOf("contrarreloj_percweeks_(title|sentence|half)", "contrarreloj_percweeks_fig_\\d", "contrarreloj_percweeks_bar_\\d", "contrarreloj_percweeks_axis_(left|right)")
  private val othersGroup = listOf("contrarreloj_others_(title|lead|hidden)", "contrarreloj_others_(\\d|all)_(label|value)")
  private val sections = listOf("contrarreloj_section_(marks|compare)", "contrarreloj_section_(marks|compare)_line", "contrarreloj_sample_notice", "contrarreloj_source_link")

  private fun assertCompareNoClashes(widthDp: Int, scale: Float, world: Comparison.Source, label: String) {
    show(widthDp, scale, compare = world, ageRange = AgeRange.R45_54)
    val all = boxes()
    val problems = mutableListOf<String>()
    val groups = mapOf(
      "La cifra" to groupOf(all, hero), "La escalera" to groupOf(all, ladder), "La campana" to groupOf(all, bell), "Tu lugar" to groupOf(all, place), "Tu lugar, semana a semana" to groupOf(all, weeksPercentile),
      "Comparado con otras edades" to groupOf(all, othersGroup), "los rótulos de sección" to groupOf(all, sections), "lo gratis" to groupOf(all, free), "Tus últimas 10 marcas" to groupOf(all, chart), "Tu ritmo" to groupOf(all, rhythm), "las notas" to groupOf(all, notes)
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

  @Test fun a_393_con_letra_1_0_lo_gratis_con_su_division_cabe_en_la_primera_pantalla() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertTrue("lo gratis (con la división) termina en %.0f dp, dentro de los 873 dp de la primera pantalla".format(b.getValue("contrarreloj_free").b), b.getValue("contrarreloj_free").b <= 873f)
    println("T215 con comparación a 393 con letra 1,0: lo gratis termina en %.0f dp, la frase de Plus en %.0f dp y la escalera en %.0f dp".format(b.getValue("contrarreloj_free").b, b.getValue("contrarreloj_hero").b, b.getValue("contrarreloj_ladder").b))
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
    assertEquals("Tinta o Palabra, récord 44 aciertos, el de hoy, elegido", descriptionOf("contrarreloj_game_stroop"))
    assertEquals("¿Verdad o disparate?, récord 31 aciertos", descriptionOf("contrarreloj_game_disparate"))
    assertEquals("Tu récord: 44 aciertos.", n("contrarreloj_free_record"))
    assertEquals("la barra y las cifras sueltas no se leen por separado", 1, rule.onAllNodes(hasTestTag("contrarreloj_bars")).fetchSemanticsNodes().size)
    assertTrue("elegido se anuncia como seleccionado", rule.onNodeWithTag("contrarreloj_game_stroop").fetchSemanticsNode().config.getOrNull(SemanticsProperties.Selected) == true)
    assertFalse(rule.onNodeWithTag("contrarreloj_game_radar").fetchSemanticsNode().config.getOrNull(SemanticsProperties.Selected) == true)
  }

  @Test fun tocar_un_juego_lo_elige_y_el_disco_vuelve() {
    show()
    rule.onNodeWithTag("contrarreloj_game_radar").performClick(); rule.waitForIdle()
    assertEquals(listOf("radar"), selectedCalls)
  }

  @Test fun todo_lo_que_se_toca_mide_48_dp_o_mas() {
    for (scale in listOf(1f)) {
      show(scale = scale)
      val b = boxes()
      for (tag in games.map { "contrarreloj_game_$it" } + "btn_contrarreloj_play") {
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
    // la escala de tamaños: 24 (títulos y, desde la Tarea 213, la frase de la cifra: ya no hay cifra de 40), 18 (Fredoka), 16 y 14 (solo los rótulos de los ejes); el título de la página (26) es el de toda página de Avance
    val sizes = Regex("""fontSize = (\d+(?:\.\d+)?)\.sp""").findAll(page).map { it.groupValues[1] }.toSet()
    assertEquals("la página usa solo estos tamaños", setOf("14", "16", "18", "24", "26"), sizes)
    // letra: solo los rótulos del eje bajan de 16 sp
    val small = Regex("""fontSize = (\d+(?:\.\d+)?)\.sp""").findAll(page).filter { it.groupValues[1].toFloat() < 16f }.map { it.groupValues[1] }.toList()
    assertTrue("solo el rótulo del eje (los dos textos y su medida), a 14 sp", small.isNotEmpty() && small.all { it == "14" })
    val axisFns = listOf("""private fun AxisLabels\(labels[\s\S]*?\n\}""", """private fun RhythmChart\([\s\S]*?\n\}""", """private fun BellAxis\([\s\S]*?\n\}""", """private fun WeeksAxis\([\s\S]*?\n\}""", """private fun PercentileWeeksChart\([\s\S]*?\n\}""")
    assertEquals("y están todos en las funciones de los ejes y rótulos (las marcas, el ritmo, la campana, las semanas y «la mitad»)", small.size, axisFns.sumOf { fn -> Regex(fn).find(page)!!.value.let { Regex("""fontSize = 14\.sp""").findAll(it).count() } })
    // la cifra (Tarea 213: ya no hay cifra de 40 sp sino UNA frase): Rubik Bold 24 con tope de escala 1,3
    assertTrue("la frase de la cifra va en Rubik 24 con tope de escala", Regex("""CapScale\(BIG_TEXT_SCALE_CAP\) \{\s*Text\(Comparison\.heroSentence[^\n]*fontSize = 24\.sp[^\n]*TitleFamily""").containsMatchIn(page))
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

  @Test fun con_la_referencia_estimada_la_pagina_no_trae_la_frase_larga_ni_pie_y_la_fila_va_bajo_lo_gratis_antes_del_detalle_de_Plus() {
    show(compare = estWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertEquals("Esta semana, comparado con tu edad", n("contrarreloj_section_compare"))
    assertNull("ya no es un dato de muestra", b["contrarreloj_sample_notice"])
    // Tarea 213 (decisión de Ricardo, 2-nov): la frase larga y el pie de la Tarea 211 se fueron; la explicación vive en la hoja que abre la fila
    assertNull("la frase larga ya no está en la página", b["contrarreloj_estimated_notice"])
    val said = everythingSaid()
    assertEquals("«No son otras personas…» no se dice en la página (vive en la hoja)", 0, said.count { it.startsWith("No son otras personas:") })
    assertEquals("ni «De dónde sale la referencia ›» (la fila de la página dice otra cosa)", 0, said.count { it.startsWith("De dónde sale la referencia") })
    val free = b.getValue("contrarreloj_free"); val hero = b.getValue("contrarreloj_hero"); val link = b.getValue("contrarreloj_source_link"); val ladder = b.getValue("contrarreloj_ladder"); val notes = b.getValue("contrarreloj_notes")
    assertEquals("una sola fila", 1, b.keys.count { it == "contrarreloj_source_link" })
    assertEquals("a 16 dp bajo la losa gratis (Tarea 215: antes, bajo la losa de la cifra)", 16f, link.y - free.b, 1.5f)
    assertEquals("y a 24 dp sobre el rótulo de la comparación (16 de entre losas y 8 propios)", 24f, b.getValue("contrarreloj_section_compare").y - link.b, 1.5f)
    assertEquals("la última pieza de la página son las notas: no hay pie", b.values.maxOf { it.b }, notes.b, 0.5f)
    assertEquals("el rótulo de la comparación queda solo sobre la frase de Plus, a 16 dp", 16f, hero.y - b.getValue("contrarreloj_section_compare").b, 1.5f)
    // el lector de pantalla: lo gratis, la fila y después la frase de Plus y la escalera, y la fila dice «Con qué te comparas, botón»
    val iHero = said.indexOfFirst { it.startsWith("Con ") && "superas al" in it }; val iLink = said.indexOfFirst { it == "Con qué te comparas" }
    val iLadder = said.indexOfFirst { it == Comparison.LADDER_TITLE }
    assertTrue("el lector encuentra la fila, la frase de Plus y la escalera (frase $iHero, fila $iLink, escalera $iLadder)", iHero >= 0 && iLink >= 0 && iLadder >= 0)
    assertTrue("y los dice en ese orden", iLink < iHero && iHero < iLadder)
    assertEquals("Con qué te comparas", descriptionOf("contrarreloj_source_link"))
    assertTrue("la escalera va después de la frase", hero.b < ladder.y)
  }

  @Test fun sin_marca_esta_semana_la_fila_Con_que_te_comparas_va_bajo_Tu_lugar_esta_semana() {
    show(compare = estWorld, ageRange = AgeRange.R45_54, results = List(3) { r(1, 20 + it) })          // tres partidas, ninguna de esta semana
    val b = boxes()
    assertEquals("Tu lugar esta semana", textOf("contrarreloj_place_title"))
    assertNull("sin cifra", b["contrarreloj_hero"])
    val link = b.getValue("contrarreloj_source_link"); val place = b.getValue("contrarreloj_place")
    assertEquals("la fila va bajo la losa de «Tu lugar esta semana», a 16 dp", 16f, link.y - place.b, 1.5f)
    assertEquals("una sola vez", 1, b.keys.count { it == "contrarreloj_source_link" })
    assertEquals("Con qué te comparas", descriptionOf("contrarreloj_source_link"))
  }

  // ---------------------------------------------------------------- Tarea 212: los emblemas de las cinco divisiones (D3)

  /** Una persona En la plataforma (la división más baja: su nombre es el más largo de las cinco): tres partidas flojas y una marca de 3 esta semana. */
  private val platformResults = List(3) { r(1, 20 + it) } + r(3, 27)

  @Test fun la_cifra_lleva_el_emblema_de_su_division_de_52_dp_a_la_izquierda_del_nombre_y_del_lugar() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    val e = b.getValue("contrarreloj_hero_emblem_ORBIT"); val name = b.getValue("contrarreloj_hero_division"); val place = b.getValue("contrarreloj_hero_place")
    assertEquals("el emblema de En órbita mide 52 dp", 52f, e.w, 0.6f); assertEquals(52f, e.h, 0.6f)
    assertEquals("hay UNO solo en la cifra, el de su división", 1, b.keys.count { it.startsWith("contrarreloj_hero_emblem_") })
    assertEquals("12 dp de aire entre el emblema y el texto", 12f, name.x - e.r, 1f)
    assertEquals("el lugar queda a la misma altura que el nombre", name.x, place.x, 0.6f)
    assertEquals("arriba, a la altura de la primera línea", name.y, e.y, 1f)
    assertTrue("el lugar va debajo del nombre", place.y >= name.b - 0.5f)
    assertEquals("el bloque de la división mide 52 dp con letra 1,0 (el emblema)", 52f, maxOf(e.b, place.b) - e.y, 1.5f)
  }

  @Test fun la_escalera_trae_los_cinco_emblemas_de_28_dp_en_orden_y_las_filas_no_crecen() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    val tags = listOf("contrarreloj_ladder_emblem_0", "contrarreloj_ladder_emblem_1", "contrarreloj_ladder_you_emblem", "contrarreloj_ladder_emblem_3", "contrarreloj_ladder_emblem_4")
    for (t in tags) { assertEquals("«$t» mide 28 dp", 28f, b.getValue(t).w, 0.6f); assertEquals(28f, b.getValue(t).h, 0.6f) }
    for ((a, c) in tags.zipWithNext()) assertTrue("«$a» va arriba de «$c»", b.getValue(a).b <= b.getValue(c).y + 0.5f)
    // a la izquierda de su nombre, con 10 dp de aire
    for ((emblem, label) in listOf(tags[0] to "contrarreloj_ladder_row_0_label", tags[1] to "contrarreloj_ladder_row_1_label", tags[2] to "contrarreloj_ladder_you_name", tags[3] to "contrarreloj_ladder_row_3_label", tags[4] to "contrarreloj_ladder_row_4_label"))
      assertEquals("«$label» queda 10 dp a la derecha de su emblema", 10f, b.getValue(label).x - b.getValue(emblem).r, 1f)
    // las filas no crecen: cada fila apagada sigue ocupando 41 dp de punta a punta (40 de fila y la raya de 1 dp) y la lima mide 44 dp con 4 de aire arriba y abajo (52)
    assertEquals("de la fila 0 a la 1: 41 dp", 41f, b.getValue("contrarreloj_ladder_row_1").y - b.getValue("contrarreloj_ladder_row_0").y, 1.5f)
    assertEquals("la fila lima mide 44 dp (52 con su aire)", 44f, b.getValue("contrarreloj_ladder_you").h, 1.5f)
    assertEquals("de la fila lima a la fila 3: 44 de recuadro, 4 de aire, la raya de 1 y los 6 de arriba de la fila apagada (55 dp)", 55f, b.getValue("contrarreloj_ladder_row_3").y - b.getValue("contrarreloj_ladder_you").y, 1.5f)
    assertTrue("el emblema de la fila lima queda dentro del recuadro, a 12 dp del borde", b.getValue("contrarreloj_ladder_you_emblem").x >= b.getValue("contrarreloj_ladder_you").x + 11.5f)
  }

  @Test fun cada_division_trae_su_propio_emblema_con_su_orden_de_la_escalera() {
    for ((tag, game) in listOf("contrarreloj_hero_emblem_PLATFORM" to platformResults)) {
      show(compare = manyWorld, ageRange = AgeRange.R45_54, results = game + others)
      assertNotNull("En la plataforma: el emblema de la cifra es el de su división (no el de En órbita)", boxes()[tag])
      assertEquals("y solo uno", 1, boxes().keys.count { it.startsWith("contrarreloj_hero_emblem_") })
      assertEquals("la escalera dice En la plataforma en la fila lima", "En la plataforma", n("contrarreloj_ladder_you_name"))
    }
  }

  @Test fun con_el_emblema_la_fila_lima_de_En_la_plataforma_se_apila_a_393_y_nada_se_encima() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54, results = platformResults + others)
    val b = boxes()
    val you = b.getValue("contrarreloj_ladder_you"); val e = b.getValue("contrarreloj_ladder_you_emblem"); val name = b.getValue("contrarreloj_ladder_you_name"); val value = b.getValue("contrarreloj_ladder_you_value")
    assertEquals("En la plataforma: el nombre más largo no cabe en una línea con el emblema y la marca: se apila (+28 dp)", 72f, you.h, 4f)
    assertTrue("la marca va debajo del nombre y del emblema (%.1f)".format(value.y - name.b), value.y >= name.b - 0.5f && value.y >= e.b - 0.5f)
    assertTrue("y a la derecha", value.r >= you.r - 12.5f - 1f)
    assertTrue("el nombre a 10 dp del emblema, sin tocarlo", name.x - e.r >= 9f)
    assertTrue("nada sale del recuadro lima", e.x >= you.x + 11.5f && name.r <= you.r - 11.5f && value.r <= you.r - 11.5f)
  }

  @Test fun el_lector_de_pantalla_dice_lo_mismo_que_antes_y_no_nombra_los_emblemas() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    val said = everythingSaid()
    assertEquals("la fila lima: la división y «Tú»", "En órbita, Tú · 43 aciertos", descriptionOf("contrarreloj_ladder_you").replace(NBSP, " "))
    assertEquals("las demás, su primer acierto", "Entre estrellas, desde 60 aciertos", descriptionOf("contrarreloj_ladder_row_0").replace(NBSP, " "))
    assertEquals("En la Luna, desde 50 aciertos", descriptionOf("contrarreloj_ladder_row_1").replace(NBSP, " "))
    assertEquals("la división se dice una sola vez, con su lugar (en lo gratis)", 1, said.count { it == "En órbita. La tercera de cinco divisiones." })
    assertTrue("y la frase de Plus aparte, sin la división", said.any { it.startsWith("Con 43 aciertos, superas al") && !it.contains("En órbita") })
    assertEquals("la frase de Plus no repite la división en ninguno de los nodos que la dicen", 0, said.count { it.startsWith("Con 43 aciertos, superas al") && it.contains("En órbita") })
    val hero = said.first { it == "En órbita. La tercera de cinco divisiones." }
    assertTrue("sin nada de «emblema», «imagen» ni «icono»: $hero", listOf("emblema", "imagen", "icono", "ícono").none { hero.lowercase().contains(it) })
    assertTrue("ningún nodo dice «emblema»", said.none { it.lowercase().contains("emblema") })
    for (tag in listOf("contrarreloj_hero_emblem_ORBIT", "contrarreloj_ladder_emblem_0", "contrarreloj_ladder_you_emblem")) {
      val node = rule.onNode(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNode()
      assertNull("«$tag» no tiene descripción: es decorativo", node.config.getOrNull(SemanticsProperties.ContentDescription))
    }
  }

  private fun assertEmblemsFit(widthDp: Int, scale: Float, results: List<TimeTrialResult> = ten + others, label: String) {
    show(widthDp, scale, compare = manyWorld, ageRange = AgeRange.R45_54, results = results)
    val b = boxes()
    val hero = b.keys.single { it.startsWith("contrarreloj_hero_emblem_") }
    assertEquals("$label: el de la cifra sigue en 52 dp con la letra a $scale (no escala)", 52f, b.getValue(hero).w, 0.6f)
    val ladder = b.keys.filter { Regex("contrarreloj_ladder_(you_)?emblem(_\\d)?").matches(it) }
    assertEquals("$label: los cinco de la escalera", 5, ladder.size)
    for (t in ladder) assertEquals("$label: «$t» sigue en 28 dp con la letra a $scale", 28f, b.getValue(t).w, 0.6f)
    val problems = mutableListOf<String>()
    fun near(tag: String, others: List<String>, card: String) {
      val e = b.getValue(tag); val c = b.getValue(card)
      if (e.x < c.x + 15.5f || e.r > c.r - 15.5f) problems += "«$tag» no deja los 16 dp de relleno de «$card» (x %.1f a %.1f; losa %.1f a %.1f)".format(e.x, e.r, c.x, c.r)
      for (o in others) b[o]?.let { p -> val s = gap(e, p); if (s < 3.5f) problems += "«$tag» y «$o» a %.1f dp".format(s) }
    }
    near(hero, listOf("contrarreloj_free_record", "contrarreloj_free_week", "contrarreloj_hero_division", "contrarreloj_hero_place", "contrarreloj_ladder_need"), "contrarreloj_free")
    for (t in ladder) near(t, listOf("contrarreloj_ladder_title", "contrarreloj_ladder_need", "contrarreloj_ladder_closes") + (0..4).flatMap { listOf("contrarreloj_ladder_row_${it}_label", "contrarreloj_ladder_row_${it}_value") } + listOf("contrarreloj_ladder_you_name", "contrarreloj_ladder_you_value"), "contrarreloj_ladder")
    for (i in ladder.indices) for (j in i + 1 until ladder.size) { val s = gap(b.getValue(ladder[i]), b.getValue(ladder[j])); if (s < 3.5f) problems += "los emblemas «${ladder[i]}» y «${ladder[j]}» a %.1f dp".format(s) }
    // ninguna palabra de la cifra ni de la escalera se parte: los textos parten por palabras (lo vigila ContrarrelojLetraTest); aquí, que el nombre de la fila lima no salga del recuadro
    b["contrarreloj_ladder_you"]?.let { y -> b["contrarreloj_ladder_you_name"]?.let { n -> if (n.r > y.r - 11.5f || n.x < y.x + 11.5f) problems += "el nombre de la fila lima se sale del recuadro" } }
    assertTrue("$label a $widthDp dp con letra $scale:\n" + problems.joinToString("\n"), problems.isEmpty())
  }

  @Test fun los_emblemas_caben_a_393_con_letra_1_0() = assertEmblemsFit(393, 1f, label = "En órbita")
  @Test fun los_emblemas_caben_a_393_con_letra_1_3() = assertEmblemsFit(393, 1.3f, label = "En órbita")
  @Test fun los_emblemas_caben_a_393_con_letra_2_0() = assertEmblemsFit(393, 2f, label = "En órbita")
  @Test fun los_emblemas_caben_a_360_con_letra_1_0() = assertEmblemsFit(360, 1f, label = "En órbita")
  @Test fun los_emblemas_caben_a_360_con_letra_1_3() = assertEmblemsFit(360, 1.3f, label = "En órbita")
  @Test fun los_emblemas_caben_a_360_con_letra_2_0() = assertEmblemsFit(360, 2f, label = "En órbita")
  @Test fun los_emblemas_caben_a_393_con_letra_1_0_en_la_plataforma() = assertEmblemsFit(393, 1f, platformResults + others, "En la plataforma")
  @Test fun los_emblemas_caben_a_393_con_letra_2_0_en_la_plataforma() = assertEmblemsFit(393, 2f, platformResults + others, "En la plataforma")
  @Test fun los_emblemas_caben_a_360_con_letra_1_0_en_la_plataforma() = assertEmblemsFit(360, 1f, platformResults + others, "En la plataforma")
  @Test fun los_emblemas_caben_a_360_con_letra_2_0_en_la_plataforma() = assertEmblemsFit(360, 2f, platformResults + others, "En la plataforma")

  @Test fun los_cinco_emblemas_son_recursos_distintos_y_decorativos_en_el_codigo() {
    val files = (1..5).map { i -> listOf("plataforma", "despegando", "orbita", "luna", "estrellas")[i - 1] }.map { java.io.File("src/main/res/drawable-nodpi/division_$it.webp") }
    for (f in files) assertTrue("falta ${f.name}", f.exists() && f.length() in 1500..60_000)
    assertEquals("cinco imágenes distintas", 5, files.map { it.readBytes().contentHashCode() }.toSet().size)
    val src = src("ui/components/DivisionEmblem.kt")
    assertTrue("decorativo: contentDescription = null", Regex("""contentDescription = null""").containsMatchIn(src))
    assertTrue("sobre la ventanilla noche", src.contains("NubiTokens.NightWindow"))
    assertTrue("esquina de 0,23 del lado y objeto al 86 %", src.contains("CORNER = 0.23f") && src.contains("OBJECT = 0.86f"))
    val page = src("ui/screens/ContrarrelojPage.kt")
    assertFalse("el emblema NO va en la puerta gratis de Avance ni en el final del Contrarreloj", src("ui/screens/ProgressScreen.kt").contains("DivisionEmblem") || java.io.File("src/main/java/com/example/games/TimeTrialResultScreen.kt").readText().contains("DivisionEmblem"))
    assertEquals("la cifra y la escalera lo usan, y nadie más en la página (2 sitios en la escalera y 1 en la cifra)", 3, Regex("""DivisionEmblem\(""").findAll(page).count())
  }

  // ---------------------------------------------------------------- Tarea 209: «De dónde sale la referencia ›»

  @Test fun con_la_referencia_estimada_hay_una_fila_Con_que_te_comparas_de_48_dp_que_se_toca_entera_y_abre_el_panel() {
    show(compare = estWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    val link = b.getValue("contrarreloj_source_link")
    assertTrue("la fila mide 48 dp o más (%.1f)".format(link.h), link.h >= 47.5f)
    assertEquals("y ocupa todo el ancho entre los márgenes, para tocarla entera", 393f - 32f, link.w, 1f)
    rule.onNode(hasText("Con qué te comparas ›"), useUnmergedTree = true).assertExists()
    assertEquals("el lector dice «Con qué te comparas, botón»: la descripción sin el «›» y el rol de botón", "Con qué te comparas", descriptionOf("contrarreloj_source_link"))
    assertEquals(androidx.compose.ui.semantics.Role.Button, rule.onNode(hasTestTag("contrarreloj_source_link")).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Role))
    rule.onNodeWithTag("contrarreloj_source_link").performScrollTo().performClick(); rule.waitForIdle()
    assertEquals("tocar la fila abre el panel", 1, sourceCalls)
  }

  @Test fun con_los_datos_de_muestra_de_depuracion_no_hay_fila_porque_no_hay_estudios_que_mostrar_y_su_aviso_va_al_final() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertNull(b["contrarreloj_source_link"])
    assertEquals("Estos son datos de muestra: solo se ven en la versión de prueba.", n("contrarreloj_sample_notice"))
    assertEquals("el aviso de muestra sigue siendo lo último de la página", b.values.maxOf { it.b }, b.getValue("contrarreloj_sample_notice").b, 0.5f)
  }

  private fun assertSourceLinkFits(widthDp: Int, scale: Float) {
    show(widthDp = widthDp, scale = scale, compare = estWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    val link = b.getValue("contrarreloj_source_link")
    assertTrue("a $widthDp dp con letra $scale: la fila mide 48 dp o más (%.1f)".format(link.h), link.h >= 47.5f)
    assertTrue("y entre los márgenes de 16 dp (x %.1f a %.1f)".format(link.x, link.r), link.x >= 15.5f && link.r <= widthDp - 15.5f)
    assertTrue("y a 15 dp o más de la losa gratis, que va antes (%.1f)".format(link.y - b.getValue("contrarreloj_free").b), link.y - b.getValue("contrarreloj_free").b >= 15f)
    assertTrue("y a 15 dp o más del rótulo de la comparación, que va después (%.1f)".format(b.getValue("contrarreloj_section_compare").y - link.b), b.getValue("contrarreloj_section_compare").y - link.b >= 15f)
    assertTrue("«Con qué te comparas ›» cabe en una línea o parte por palabras (alto %.1f dp)".format(link.h), link.h <= 100f)
  }

  @Test fun la_fila_Con_que_te_comparas_cabe_a_393_con_letra_1_0() = assertSourceLinkFits(393, 1f)
  @Test fun la_fila_Con_que_te_comparas_cabe_a_393_con_letra_1_3() = assertSourceLinkFits(393, 1.3f)
  @Test fun la_fila_Con_que_te_comparas_cabe_a_393_con_letra_2_0() = assertSourceLinkFits(393, 2f)
  @Test fun la_fila_Con_que_te_comparas_cabe_a_360_con_letra_1_0() = assertSourceLinkFits(360, 1f)
  @Test fun la_fila_Con_que_te_comparas_cabe_a_360_con_letra_1_3() = assertSourceLinkFits(360, 1.3f)
  @Test fun la_fila_Con_que_te_comparas_cabe_a_360_con_letra_2_0() = assertSourceLinkFits(360, 2f)

  private fun assertNeverPeople(scale: Float) {
    show(scale = scale, compare = estWorld, ageRange = AgeRange.R45_54)
    val said = everythingSaid()
    val rest = said          // Tarea 213: la frase larga («No son otras personas…») ya no está en la página: TODO lo que dice la página cumple
    for (text in said) assertFalse("con letra $scale, «$text» dice «percentil» (Tarea 209: salió de todo texto, también del lector)", "percentil" in text.lowercase())
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
    assertTrue("la cifra: «Con N aciertos, superas al N % de la referencia estimada para tu edad.»", n("contrarreloj_hero_sentence").endsWith(" % de la referencia estimada para tu edad.") && said.any { it == n("contrarreloj_hero_sentence") })
    assertTrue("la campana, en su frase", said.any { it == Comparison.ESTIMATED_BELL_CAPTION })
    assertTrue("«Otras edades», en su encabezado", said.any { it == "Tu mejor marca de esta semana, frente a la referencia estimada de cada edad:" })
    assertTrue("las semanas, en su nota", said.any { it == Comparison.WEEKS_ESTIMATED_NOTE })
    assertTrue("y el lugar de la división, no un percentil: «${n("contrarreloj_hero_place")}»", n("contrarreloj_hero_place") in Comparison.Division.entries.map { Comparison.divisionPlaceLine(it) })
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
    show(compare = estWorld, ageRange = AgeRange.R45_54, selected = game, results = withEarly(game, mine))
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
    assertEquals("subtítulo de siempre, solo contigo", com.example.data.TimeTrialMarks.subtitle(compared = false), textOf("contrarreloj_subtitle"))
  }

  @Test fun de_14_a_17_anos_la_referencia_se_muestra_como_la_de_18_a_29() {
    show(compare = estWorld, ageRange = AgeRange.R14_17)
    val young = Comparison.place(estWorld, "stroop", AgeRange.R18_29, 43)
    val b = boxes()
    assertNotNull(b["contrarreloj_hero"]); assertNotNull(b["contrarreloj_section_compare"]); assertNotNull(b["contrarreloj_source_link"])
    assertEquals("la misma frase que de 18 a 29 años con la misma marca", Comparison.heroSentence((young as Comparison.Place.Found).ranked, AgeRange.R14_17), n("contrarreloj_hero_sentence"))
    assertEquals(Comparison.divisionPlaceLine(young.ranked.division), n("contrarreloj_hero_place"))
  }

  @Test fun el_subtitulo_con_comparacion_es_la_frase_de_Ricardo_y_no_habla_de_capacidad_ni_de_personas() {
    show(compare = estWorld, ageRange = AgeRange.R45_54)
    assertEquals("El Contrarreloj define tu lugar de la semana.", textOf("contrarreloj_subtitle"))
    assertEquals("con comparación, la frase de Ricardo (Tarea 215)", com.example.data.TimeTrialMarks.subtitle(compared = true), textOf("contrarreloj_subtitle"))
    val sub = textOf("contrarreloj_subtitle").lowercase()
    assertTrue("ni «personas» ni «capacidad»", "personas" !in sub && "capacidad" !in sub)
    assertTrue("y lo que dice con qué se compara vive en la fila «Con qué te comparas ›»", rule.onAllNodes(hasTestTag("contrarreloj_source_link"), useUnmergedTree = true).fetchSemanticsNodes().isNotEmpty())
  }

  // ---------------------------------------------------------------- Tarea 205b: la raya de la mitad bajo la barra «Tú» y la edad que falta

  private fun halfOf(game: String) = estWorld.distribution(game, listOf(AgeRange.R45_54), 0, 1).p50

  /**
   * La campana de [game] con la marca en [mark]: la frase nunca habla de una raya que no se ve. Si dice «La línea…», la raya existe y sus límites no tocan los de la barra «Tú» (al menos ~1 dp de aire); si dice «Tu marca está en la mitad…», la raya no se dibuja,
   * el lector de pantalla termina con «está en la mitad.» y la distancia entre los centros es la que la regla de `Comparison.barCoversHalf` da con el ancho REAL del dibujo.
   */
  private fun assertBellCaption(game: String, mark: Int, widthDp: Int, expectOnHalf: Boolean) {
    show(widthDp = widthDp, compare = estWorld, ageRange = AgeRange.R45_54, selected = game, results = withEarly(game, listOf(r(mark, 27, game = game))))
    val b = boxes()
    val d = estWorld.distribution(game, listOf(AgeRange.R45_54), 0, 1)
    val caption = textOf("contrarreloj_bell_caption")
    val spoken = rule.onNode(hasTestTag("contrarreloj_bell_window"), useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsProperties.ContentDescription)?.joinToString(". ").orEmpty()
    val area = b.getValue("contrarreloj_bell_area")
    val you = b.getValue("contrarreloj_bell_you_bar")
    val covers = Comparison.barCoversHalf(mark, d, area.w.toDouble())
    assertEquals("$game marca $mark a $widthDp dp: la regla con el ancho real del dibujo (%.1f dp) dice lo mismo que la frase".format(area.w), expectOnHalf, covers)
    if (expectOnHalf) {
      assertEquals("$game marca $mark a $widthDp dp", Comparison.BELL_ON_HALF_ESTIMATED, caption)
      assertNull("$game marca $mark a $widthDp dp: la raya no se dibuja debajo de la barra", b["contrarreloj_bell_median"])
      assertTrue("y el lector de pantalla termina con «está en la mitad.»: $spoken", spoken.endsWith("está en la mitad."))
    } else {
      assertEquals("$game marca $mark a $widthDp dp", Comparison.ESTIMATED_BELL_CAPTION, caption)
      val line = b.getValue("contrarreloj_bell_median")
      assertTrue("$game marca $mark a $widthDp dp: los límites de la raya (%.1f a %.1f) no tocan los de la barra (%.1f a %.1f)".format(line.x, line.r, you.x, you.r), line.x >= you.r + 0.9f || line.r <= you.x - 0.9f)
      assertTrue("y el lector de pantalla termina con «está marcada.»: $spoken", spoken.endsWith("está marcada."))
    }
    assertFalse("la frase de la mitad nunca promete una raya que no está", caption.startsWith("La línea") && b["contrarreloj_bell_median"] == null)
  }

  @Test fun Rescate_con_la_marca_en_la_mitad_dice_que_la_marca_esta_en_la_mitad_a_393() = assertBellCaption("radar", halfOf("radar"), 393, expectOnHalf = true)
  @Test fun Rescate_con_la_marca_en_la_mitad_dice_que_la_marca_esta_en_la_mitad_a_360() = assertBellCaption("radar", halfOf("radar"), 360, expectOnHalf = true)
  @Test fun Rescate_con_un_acierto_mas_ya_ve_la_raya_a_393() = assertBellCaption("radar", halfOf("radar") + 1, 393, expectOnHalf = false)
  @Test fun Rescate_con_un_acierto_menos_ya_ve_la_raya_a_360() = assertBellCaption("radar", halfOf("radar") - 1, 360, expectOnHalf = false)
  @Test fun Tinta_con_la_mitad_mas_1_a_360_dice_que_esta_en_la_mitad() = assertBellCaption("stroop", halfOf("stroop") + 1, 360, expectOnHalf = true)
  @Test fun Tinta_con_la_mitad_menos_1_a_360_dice_que_esta_en_la_mitad() = assertBellCaption("stroop", halfOf("stroop") - 1, 360, expectOnHalf = true)
  @Test fun Tinta_con_la_mitad_mas_2_a_360_ve_la_raya() = assertBellCaption("stroop", halfOf("stroop") + 2, 360, expectOnHalf = false)
  @Test fun Tinta_con_la_mitad_a_393_dice_que_esta_en_la_mitad() = assertBellCaption("stroop", halfOf("stroop"), 393, expectOnHalf = true)
  @Test fun Acoplamiento_con_la_mitad_a_360_dice_que_esta_en_la_mitad() = assertBellCaption("acoplamiento", halfOf("acoplamiento"), 360, expectOnHalf = true)
  @Test fun Acoplamiento_con_la_mitad_mas_2_a_393_ve_la_raya() = assertBellCaption("acoplamiento", halfOf("acoplamiento") + 2, 393, expectOnHalf = false)

  @Test fun con_datos_de_muestra_la_frase_de_siempre_sigue_con_la_raya_a_la_vista() {
    show(compare = manyWorld, ageRange = AgeRange.R45_54)
    val b = boxes()
    assertEquals("La línea marca la mitad de las personas.", textOf("contrarreloj_bell_caption"))
    val line = b.getValue("contrarreloj_bell_median"); val you = b.getValue("contrarreloj_bell_you_bar")
    assertTrue("sin tocarse: la raya (%.1f a %.1f) y la barra (%.1f a %.1f)".format(line.x, line.r, you.x, you.r), line.x >= you.r + 0.9f || line.r <= you.x - 0.9f)
  }

  // ---- sin franja guardada: la página dice dónde elegirla

  @Test fun sin_franja_guardada_la_pagina_dice_donde_elegir_la_edad_en_una_linea_corrida_sin_negrita() {
    show(compare = estWorld, ageRange = null)
    val b = boxes()
    assertEquals("Para compararte con una referencia estimada para tu edad, elige tu edad en Tú › Más ajustes › Juego › Rango de edad.", n("contrarreloj_age_hint"))
    assertEquals(Comparison.AGE_HINT, n("contrarreloj_age_hint"))
    assertNull("sin franja no hay sección de comparación", b["contrarreloj_section_compare"]); assertNull(b["contrarreloj_hero"]); assertNull(b["contrarreloj_estimated_notice"])
    assertEquals("una sola vez en toda la pantalla", 1, everythingSaid().count { it.startsWith("Para compararte con una referencia estimada") })
    val selectorBottom = games.maxOf { b.getValue("contrarreloj_game_$it").b }
    assertTrue("va bajo el selector (%.1f bajo %.1f)".format(b.getValue("contrarreloj_age_hint").y, selectorBottom), b.getValue("contrarreloj_age_hint").y >= selectorBottom + 7.5f)
    val page = src("ui/screens/ContrarrelojPage.kt")
    val line = Regex("""Text\(Comparison\.AGE_HINT[^\n]*""").find(page)!!.value
    assertFalse("es una frase corrida: sin negrita", "fontWeight" in line)
    assertTrue("la ruta es la real: Tú › Más ajustes › Juego › Rango de edad", src("ui/screens/ProfileScreen.kt").contains("\"Más ajustes\"") && src("ui/screens/SettingsScreen.kt").let { it.contains("Text(text = \"Juego\"") && it.contains("Text(text = \"Rango de edad\"") })
  }

  private fun assertNoAgeHint(name: String, source: Comparison.Source?, range: AgeRange?) {
    show(compare = source, ageRange = range)
    assertNull("$name: sin aviso", boxes()["contrarreloj_age_hint"])
  }

  @Test fun el_aviso_de_la_edad_no_sale_de_14_a_17_anos() = assertNoAgeHint("de 14 a 17 años", estWorld, AgeRange.R14_17)
  @Test fun el_aviso_de_la_edad_no_sale_con_franja_guardada() = assertNoAgeHint("con franja", estWorld, AgeRange.R45_54)
  @Test fun el_aviso_de_la_edad_no_sale_sin_comparacion() = assertNoAgeHint("sin comparación", null, null)
  @Test fun el_aviso_de_la_edad_no_sale_con_la_muestra_de_depuracion_que_asume_45_a_54() = assertNoAgeHint("con la muestra de depuración", manyWorld, null)
  @Test fun el_aviso_de_la_edad_no_sale_si_el_juego_no_tiene_tabla() = assertNoAgeHint("sin tabla de este juego", Comparison.ComposedSource(null, com.example.data.EstimatedReference.of(emptyList())), null)

  private fun assertAgeHintFits(widthDp: Int, scale: Float) {
    show(widthDp = widthDp, scale = scale, compare = estWorld, ageRange = null)
    val b = boxes()
    val hint = b.getValue("contrarreloj_age_hint")
    val selectorBottom = games.maxOf { b.getValue("contrarreloj_game_$it").b }
    val next = listOf("contrarreloj_free").firstNotNullOfOrNull { b[it] }
    assertTrue("a $widthDp dp con letra $scale: bajo el selector a 8 dp o más (%.1f)".format(hint.y - selectorBottom), hint.y - selectorBottom >= 7.5f)
    assertTrue("y dentro de los márgenes de 16 dp (x %.1f a %.1f)".format(hint.x, hint.r), hint.x >= 15.5f && hint.r <= widthDp - 15.5f)
    if (next != null) assertTrue("y a 8 dp o más de lo que sigue (%.1f)".format(next.y - hint.b), next.y - hint.b >= 7.5f)
  }

  @Test fun el_aviso_de_la_edad_cabe_a_393_con_letra_1_0() = assertAgeHintFits(393, 1f)
  @Test fun el_aviso_de_la_edad_cabe_a_393_con_letra_2_0() = assertAgeHintFits(393, 2f)
  @Test fun el_aviso_de_la_edad_cabe_a_360_con_letra_1_0() = assertAgeHintFits(360, 1f)
  @Test fun el_aviso_de_la_edad_cabe_a_360_con_letra_2_0() = assertAgeHintFits(360, 2f)

  // ---------------------------------------------------------------- Tarea 207: el lugar aparece desde la tercera partida de cada juego

  /**
   * La página con TODO lo que cambia como estado (un solo `setContent` por prueba): se cambian [results], [selected], [range], [compare], [width] y [scale] y se espera a que recomponga.
   */
  private class Live {
    var results by mutableStateOf(listOf<TimeTrialResult>())
    var selected by mutableStateOf("stroop")
    var range by mutableStateOf<AgeRange?>(null)
    var compare by mutableStateOf<Comparison.Source?>(null)
    var width by mutableStateOf(393)
    var scale by mutableStateOf(1f)
  }

  private fun live(compare: Comparison.Source?, range: AgeRange?): Live {
    val s = Live().also { it.compare = compare; it.range = range }
    rule.setContent {
      val base = LocalDensity.current
      density = base.density
      CompositionLocalProvider(LocalDensity provides Density(base.density, s.scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(s.width.dp).height(873.dp)) {
            BoardBackground()
            ContrarrelojPage(results = s.results, selected = s.selected, onSelect = {}, now = now, lang = com.example.model.AppLanguage.SPANISH, compare = s.compare, ageRange = s.range)
          }
        }
      }
    }
    rule.waitForIdle()
    return s
  }

  /** [n] partidas de [game] con la marca de cada una (la última, de esta semana; las demás, de antes). */
  private fun gamesOf(game: String, n: Int, last: Int = 40): List<TimeTrialResult> = List(n) { i -> if (i == n - 1) r(last, 27, game = game) else r(20 + i, 15 + i, game = game) }

  /** Todo lo de la comparación que NO puede estar con menos de tres partidas. */
  private val placePieces = listOf("contrarreloj_section_compare", "contrarreloj_section_marks", "contrarreloj_source_link", "contrarreloj_hero", "contrarreloj_ladder", "contrarreloj_bell", "contrarreloj_others", "contrarreloj_percweeks", "contrarreloj_place")

  @Test fun con_una_o_dos_partidas_no_hay_lugar_y_la_frase_va_una_vez_bajo_el_selector_en_una_linea_corrida_sin_negrita() {
    val s = live(estWorld, AgeRange.R45_54)
    for (count in 1..2) {
      s.results = gamesOf("stroop", count); rule.waitForIdle()
      val b = boxes()
      assertEquals("con $count partida(s): la frase exacta", "Tu lugar aparece desde tu tercera partida: las primeras sirven para conocer el juego.", n("contrarreloj_early_notice"))
      assertEquals(Comparison.EARLY_NOTICE, n("contrarreloj_early_notice"))
      for (tag in placePieces) assertNull("con $count partida(s) no existe «$tag»", b.keys.firstOrNull { it == tag || it.startsWith(tag + "_") })
      assertNull("sin franja no hay aviso de edad: aquí sí hay franja", b["contrarreloj_age_hint"])
      assertEquals("la frase se dice UNA sola vez en toda la pantalla", 1, everythingSaid().count { it == Comparison.EARLY_NOTICE })
      val selectorBottom = games.maxOf { b.getValue("contrarreloj_game_$it").b }
      assertTrue("va bajo el selector, a 8 dp o más (%.1f)".format(b.getValue("contrarreloj_early_notice").y - selectorBottom), b.getValue("contrarreloj_early_notice").y - selectorBottom >= 7.5f)
      assertTrue("y antes de lo gratis", b.getValue("contrarreloj_early_notice").b <= b.getValue("contrarreloj_free").y - 7.5f)
      assertEquals("la cabecera dice que se compara solo contigo", com.example.data.TimeTrialMarks.SUBTITLE, n("contrarreloj_subtitle"))
      assertTrue("y la nota también", n("contrarreloj_note").contains("comparado solo contigo"))
    }
    val line = Regex("""Text\(Comparison\.EARLY_NOTICE[^\n]*""").find(src("ui/screens/ContrarrelojPage.kt"))!!.value
    assertFalse("es una frase corrida: sin negrita", "fontWeight" in line)
  }

  @Test fun con_cero_partidas_tampoco_hay_lugar_y_la_pagina_dice_que_faltan_marcas() {
    show(compare = estWorld, ageRange = AgeRange.R45_54, results = emptyList())
    val b = boxes()
    assertEquals(Comparison.EARLY_NOTICE, n("contrarreloj_early_notice"))
    assertEquals("y lo gratis dice una sola línea, sin la losa vacía de antes (Tarea 215b)", "Todavía no tienes marcas en este juego.", n("contrarreloj_free_record"))
    assertNull(b["contrarreloj_empty"]); assertNull(b["contrarreloj_free_week"])
    for (tag in placePieces) assertNull("«$tag»", b[tag])
  }

  @Test fun desde_la_tercera_partida_todo_se_ve_como_antes_y_la_frase_ya_no_esta() {
    val s = live(estWorld, AgeRange.R45_54)
    for (count in listOf(3, 4, 10)) {
      s.results = gamesOf("stroop", count); rule.waitForIdle()
      val b = boxes()
      assertNull("con $count partidas ya no hay frase", b["contrarreloj_early_notice"])
      assertEquals("y hay lugar: la cifra, la escalera y la campana", 3, listOf("contrarreloj_hero", "contrarreloj_ladder", "contrarreloj_bell").count { b.containsKey(it) })
      assertNotNull(b["contrarreloj_section_compare"])
      assertEquals(0, everythingSaid().count { it == Comparison.EARLY_NOTICE })
    }
  }

  @Test fun la_cuenta_es_por_juego_y_de_cualquier_semana_y_pista_las_de_otro_juego_no_suman() {
    val s = live(estWorld, AgeRange.R45_54)
    // 2 de Tinta + 5 de Rescate: Tinta sigue sin lugar
    s.results = gamesOf("stroop", 2) + gamesOf("radar", 5, 12); s.selected = "stroop"; rule.waitForIdle()
    assertNotNull("Tinta con 2 partidas: la frase", boxes()["contrarreloj_early_notice"])
    // Rescate, con 5, sí tiene lugar
    s.selected = "radar"; rule.waitForIdle()
    assertNull(boxes()["contrarreloj_early_notice"]); assertNotNull(boxes()["contrarreloj_hero"])
    // una partida de la pista vieja y otra de hace semanas cuentan para llegar a tres, aunque no para la marca de esta semana
    s.results = listOf(r(33, 5, version = 1), r(36, 12), r(40, 27)); s.selected = "stroop"; rule.waitForIdle()
    assertNull("tres partidas de cualquier pista y semana", boxes()["contrarreloj_early_notice"]); assertNotNull(boxes()["contrarreloj_hero"])
  }

  @Test fun el_lector_de_pantalla_no_dice_ningun_lugar_con_menos_de_tres_partidas() {
    show(compare = estWorld, ageRange = AgeRange.R45_54, results = gamesOf("stroop", 2))
    val said = everythingSaid().joinToString(" | ")
    for (word in listOf("de cada 100", "Percentil", "percentil", "En órbita", "En la plataforma", "Despegando", "En la Luna", "Entre estrellas", "Te faltan", "Campana", "referencia estimada", "personas", "robot", "Peldaños", "Otras edades"))
      assertFalse("el lector no dice «$word» con 2 partidas: $said", word in said)
    assertEquals("dice la frase una sola vez", 1, everythingSaid().count { it == Comparison.EARLY_NOTICE })
  }

  // ---- la precedencia: sin franja el aviso de edad, sin tabla nada, la muestra como la referencia; de 14 a 17 años igual que cualquiera (Tarea 216: ya no hay línea de menores)

  @Test fun sin_franja_guardada_manda_el_aviso_de_la_edad_aunque_tenga_menos_de_tres_partidas() {
    val s = live(estWorld, null)
    for (count in 0..3) {
      s.results = gamesOf("stroop", count); rule.waitForIdle()
      assertEquals("con $count partida(s) y sin franja: el aviso de edad", Comparison.AGE_HINT, n("contrarreloj_age_hint"))
      assertNull("y no la frase de la tercera partida", boxes()["contrarreloj_early_notice"])
    }
  }

  @Test fun de_14_a_17_anos_la_pagina_es_la_de_cualquiera_con_la_frase_de_la_tercera_partida_y_despues_el_lugar_y_nunca_una_linea_de_menores() {
    val s = live(estWorld, AgeRange.R14_17)
    for (count in 0..3) {
      s.results = gamesOf("stroop", count); rule.waitForIdle()
      val b = boxes()
      assertNull("con $count: no existe la línea de menores (Tarea 216)", b["contrarreloj_minors_notice"])
      assertNull("sin aviso de edad: ya la dijo", b["contrarreloj_age_hint"])
      if (count < Comparison.PLACE_FROM_GAME) {
        assertEquals("con $count: la frase de la tercera partida, como de 18 a 29", Comparison.EARLY_NOTICE, n("contrarreloj_early_notice")); assertNull(b["contrarreloj_hero"])
      } else {
        assertNull("con $count: ya no hay frase", b["contrarreloj_early_notice"]); assertNotNull("y hay comparación completa", b["contrarreloj_hero"])
        assertNotNull("con su fila «Con qué te comparas ›»", b["contrarreloj_source_link"])
      }
    }
    assertEquals("ninguna frase dice «solo tus propias marcas» ni «13 a 17»", 0, everythingSaid().count { "solo tus propias marcas" in it || "13 a 17" in it })
  }

  /** De 14 a 17 años la pestaña queda igual que de 18 a 29 años: con la comparación completa, a 393 y 360 dp con letra 1,0 y 2,0, sin choques y dentro de los márgenes de 16 dp. */
  private fun assertTeenSameAsYoung(widthDp: Int, scale: Float) {
    val tags = listOf("contrarreloj_hero_sentence", "contrarreloj_hero_division", "contrarreloj_hero_place", "contrarreloj_ladder_need", "contrarreloj_ladder_closes", "contrarreloj_source_link", "contrarreloj_section_compare")
    show(widthDp = widthDp, scale = scale, compare = estWorld, ageRange = AgeRange.R14_17, results = ten)
    val teen = boxes()
    for (tag in tags) assertNotNull("a $widthDp dp con letra $scale, de 14 a 17 existe «$tag»", teen[tag])
    assertNull("sin línea de menores", teen["contrarreloj_minors_notice"])
    for ((tag, b) in teen) if (tag.startsWith("contrarreloj_") && tag !in setOf("contrarreloj_page", "contrarreloj_scroll", "contrarreloj_header")) {
      assertTrue("a $widthDp dp con letra $scale «$tag» no se sale del margen (x %.1f a %.1f)".format(b.x, b.r), b.x >= 15.5f && b.r <= widthDp - 15.5f)
    }
  }

  @Test fun de_14_a_17_anos_la_pestana_es_la_de_18_a_29_a_393_con_letra_1_0() = assertTeenSameAsYoung(393, 1f)
  @Test fun de_14_a_17_anos_la_pestana_es_la_de_18_a_29_a_393_con_letra_2_0() = assertTeenSameAsYoung(393, 2f)
  @Test fun de_14_a_17_anos_la_pestana_es_la_de_18_a_29_a_360_con_letra_1_0() = assertTeenSameAsYoung(360, 1f)
  @Test fun de_14_a_17_anos_la_pestana_es_la_de_18_a_29_a_360_con_letra_2_0() = assertTeenSameAsYoung(360, 2f)

  @Test fun sin_Plus_de_14_a_17_anos_van_las_dos_losas_grises_como_a_los_18() {
    show(compare = estWorld, ageRange = AgeRange.R14_17, results = ten, plus = false)
    val b = boxes()
    assertNotNull("la losa gris de «tu lugar»", b["contrarreloj_locked_place"]); assertNotNull("y la de «todas tus marcas»", b["contrarreloj_locked_marks"])
    assertNull("sin línea de menores", b["contrarreloj_minors_notice"])
    assertNotNull("y lo gratis dice la división", b["contrarreloj_hero_division"])
  }

  @Test fun en_un_juego_sin_tabla_no_sale_ni_la_frase_ni_el_aviso() {
    val sinTabla = object : Comparison.Source {
      override fun distribution(game: String, ranges: List<AgeRange>?, weeksAgo: Int, weeks: Int) = estWorld.distribution(game, ranges, weeksAgo, weeks)
      override fun covers(game: String) = false
    }
    val s = live(sinTabla, AgeRange.R45_54)
    for (range in listOf(AgeRange.R45_54, null)) for (count in 1..3) {
      s.range = range; s.results = gamesOf("stroop", count); rule.waitForIdle()
      val b = boxes()
      assertNull("sin tabla ($range, $count): sin frase", b["contrarreloj_early_notice"]); assertNull(b["contrarreloj_age_hint"]); assertNull(b["contrarreloj_hero"])
    }
  }

  @Test fun con_los_datos_de_muestra_de_depuracion_vale_lo_mismo_que_con_la_referencia() {
    // sin franja guardada la muestra asume 45 a 54: no hay aviso de edad y sí la frase con menos de tres partidas
    val s = live(manyWorld, null)
    s.results = gamesOf("stroop", 2); rule.waitForIdle()
    assertEquals(Comparison.EARLY_NOTICE, n("contrarreloj_early_notice")); assertNull(boxes()["contrarreloj_age_hint"]); assertNull(boxes()["contrarreloj_hero"])
    s.results = gamesOf("stroop", 3); rule.waitForIdle()
    assertNull(boxes()["contrarreloj_early_notice"]); assertNotNull(boxes()["contrarreloj_hero"])
  }

  private fun assertEarlyNoticeFits(widthDp: Int, scale: Float) {
    show(widthDp = widthDp, scale = scale, compare = estWorld, ageRange = AgeRange.R45_54, results = gamesOf("stroop", 2))
    val b = boxes()
    val notice = b.getValue("contrarreloj_early_notice")
    val selectorBottom = games.maxOf { b.getValue("contrarreloj_game_$it").b }
    val next = b.getValue("contrarreloj_free")
    assertTrue("a $widthDp dp con letra $scale: bajo el selector a 8 dp o más (%.1f)".format(notice.y - selectorBottom), notice.y - selectorBottom >= 7.5f)
    assertTrue("y dentro de los márgenes de 16 dp (x %.1f a %.1f)".format(notice.x, notice.r), notice.x >= 15.5f && notice.r <= widthDp - 15.5f)
    assertTrue("y a 8 dp o más de lo que sigue (%.1f)".format(next.y - notice.b), next.y - notice.b >= 7.5f)
  }

  @Test fun la_frase_de_la_tercera_partida_cabe_a_393_con_letra_1_0() = assertEarlyNoticeFits(393, 1f)
  @Test fun la_frase_de_la_tercera_partida_cabe_a_393_con_letra_2_0() = assertEarlyNoticeFits(393, 2f)
  @Test fun la_frase_de_la_tercera_partida_cabe_a_360_con_letra_1_0() = assertEarlyNoticeFits(360, 1f)
  @Test fun la_frase_de_la_tercera_partida_cabe_a_360_con_letra_2_0() = assertEarlyNoticeFits(360, 2f)

  // ---------------------------------------------------------------- Tarea 207: el eje de la campana no pone un rótulo bajo la raya de la mitad ni bajo la barra «Tú»

  /** Los rótulos del eje que se dibujan, la raya y la barra, de la campana de [game] en la franja [range] con la marca [mark]; con el ancho y la letra pedidos. */
  private fun axisOf(s: Live, game: String, range: AgeRange, mark: Int, widthDp: Int, scale: Float = 1f): Triple<Map<Int, Rect>, Rect?, Rect> {
    s.width = widthDp; s.scale = scale; s.selected = game; s.range = range; s.results = gamesOf(game, 3, mark); rule.waitForIdle()
    val b = boxes()
    return Triple((0..3).mapNotNull { i -> b["contrarreloj_bell_axis_$i"]?.let { i to it } }.toMap(), b["contrarreloj_bell_median"], b.getValue("contrarreloj_bell_you_bar"))
  }

  /** Ningún rótulo INTERMEDIO dibujado tiene la raya o la barra dentro de su caja más 2 dp a cada lado (los extremos se dibujan siempre). */
  private fun assertAxisClear(label: String, axis: Triple<Map<Int, Rect>, Rect?, Rect>) {
    val (labels, median, bar) = axis
    assertTrue("$label: los dos extremos se dibujan siempre (${labels.keys})", 0 in labels && 3 in labels)
    for ((i, box) in labels) if (i in 1..2) for ((name, other) in listOf("la raya de la mitad" to median, "la barra «Tú»" to bar)) {
      if (other == null) continue
      assertTrue("$label: el rótulo $i (x %.1f a %.1f) tiene a $name (x %.1f a %.1f) a menos de 2 dp".format(box.x, box.r, other.x, other.r), other.x >= box.r + 1.9f || other.r <= box.x - 1.9f)
    }
  }

  @Test fun Tinta_de_30_a_44_con_la_mitad_en_65_no_dibuja_el_63_ni_a_393_ni_a_360() {
    val s = live(estWorld, AgeRange.R30_44)
    val d = estWorld.distribution("stroop", listOf(AgeRange.R30_44), 0, 1)
    assertEquals("la mitad de Tinta de 30 a 44 años es 65", 65, d.p50); assertEquals(listOf(0, 32, 63, 95), Comparison.axisTicks(d))
    for (w in listOf(393, 360)) {
      val axis = axisOf(s, "stroop", AgeRange.R30_44, 58, w)
      assertEquals("a $w dp el eje queda «0 aciertos · 32 · 95 o más»", setOf(0, 1, 3), axis.first.keys)
      assertTrue("la raya sí se dibuja (la marca 58 no la tapa)", axis.second != null)
      assertAxisClear("Tinta 30 a 44, marca 58, $w dp", axis)
    }
  }

  @Test fun una_marca_justo_sobre_un_rotulo_lo_oculta_y_el_resto_del_eje_sigue() {
    val s = live(estWorld, AgeRange.R30_44)
    // la barra «Tú» de la marca 63 cae sobre el «63» (y a 2 aciertos de la raya, que se sigue viendo)
    for (w in listOf(393, 360)) {
      val axis = axisOf(s, "stroop", AgeRange.R30_44, 63, w)
      assertFalse("a $w dp la barra en 63 oculta el «63»", 2 in axis.first.keys); assertTrue("y el «32» sigue", 1 in axis.first.keys)
      assertAxisClear("Tinta 30 a 44, marca 63, $w dp", axis)
    }
    // la barra de la marca 32 cae sobre el «32»
    for (w in listOf(393, 360)) {
      val axis = axisOf(s, "stroop", AgeRange.R30_44, 32, w)
      assertFalse("a $w dp la barra en 32 oculta el «32»", 1 in axis.first.keys)
      assertAxisClear("Tinta 30 a 44, marca 32, $w dp", axis)
    }
  }

  @Test fun los_extremos_se_dibujan_siempre_aunque_la_barra_caiga_sobre_ellos() {
    val s = live(estWorld, AgeRange.R30_44)
    for (mark in listOf(0, 94)) for (w in listOf(393, 360)) {
      val axis = axisOf(s, "stroop", AgeRange.R30_44, mark, w)
      assertTrue("marca $mark a $w dp: «0 aciertos» y «95 o más» siguen (${axis.first.keys})", 0 in axis.first.keys && 3 in axis.first.keys)
    }
  }

  @Test fun en_todos_los_juegos_y_franjas_ningun_rotulo_intermedio_queda_bajo_la_raya_ni_la_barra_con_letra_1_0_y_2_0() {
    val s = live(estWorld, AgeRange.R30_44)
    val failures = mutableListOf<String>()
    for ((game, marks) in listOf("stroop" to listOf(0, 20, 32, 45, 58, 62, 63, 64, 65, 66, 67, 72, 95), "disparate" to listOf(0, 12, 20, 25, 33, 38, 39, 40, 41, 50, 79), "acoplamiento" to listOf(0, 10, 13, 20, 24, 25, 26, 27, 30, 38), "radar" to listOf(0, 4, 8, 9, 14, 15, 16, 20)))
      for (range in listOf(AgeRange.R30_44, AgeRange.R75_PLUS)) for (w in listOf(393, 360)) for (scale in listOf(1f, 2f)) for (mark in marks) {
        val axis = axisOf(s, game, range, mark, w, scale)
        runCatching { assertAxisClear("$game $range marca $mark a $w dp con letra $scale", axis) }.exceptionOrNull()?.let { failures += it.message.orEmpty() }
      }
    assertTrue("rótulos bajo la raya o la barra:\n" + failures.joinToString("\n"), failures.isEmpty())
  }

  @Test fun el_lector_de_pantalla_no_lee_los_numeros_del_eje_ni_los_de_los_rotulos_omitidos() {
    show(compare = estWorld, ageRange = AgeRange.R30_44, results = gamesOf("stroop", 3, 58))
    val bell = everythingSaid().single { it.startsWith("Campana de la referencia estimada") }
    assertEquals("Campana de la referencia estimada para 30 a 44 años. Tu mejor marca de esta semana, 58 aciertos, está marcada.", bell)
    val axisNodes = listOf("contrarreloj_bell_axis_0", "contrarreloj_bell_axis_1", "contrarreloj_bell_axis_3").flatMap { tag -> rule.onAllNodes(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNodes() }
    assertTrue("los rótulos del eje se ven pero no se leen: no llevan texto en el árbol", axisNodes.size == 3 && axisNodes.all { it.config.getOrNull(SemanticsProperties.Text) == null })
    for (word in listOf(" 32", " 63", "95 o más")) assertFalse("el lector no dice «$word»: $bell", word in bell)
  }

  // ---------------------------------------------------------------- Tarea 208: Rescate relámpago cuenta por cápsula (tramos de a 3, 0 a 47)

  @Test fun en_Rescate_la_escalera_tiene_los_cinco_peldanos_en_las_seis_franjas_y_la_campana_llega_a_45() {
    val s = live(estWorld, AgeRange.R18_29)
    s.selected = "radar"
    for (range in listOf(AgeRange.R18_29, AgeRange.R30_44, AgeRange.R45_54, AgeRange.R55_64, AgeRange.R65_74, AgeRange.R75_PLUS)) {
      val d = estWorld.distribution("radar", listOf(range), 0, 1)
      s.range = range; s.results = gamesOf("radar", 3, d.p50); rule.waitForIdle()
      val b = boxes()
      val rows = b.keys.count { Regex("contrarreloj_ladder_row_\\d").matches(it) } + (if (b.containsKey("contrarreloj_ladder_you")) 1 else 0)
      assertEquals("$range: cinco divisiones en la escalera de Rescate", 5, rows)
      assertEquals("$range: la campana de Rescate va de 0 a 47 (último tramo «45 o más»)", 48, d.topMark)
      assertEquals("$range: el eje termina en «45 o más»", listOf(0, 15, 30, 45), Comparison.axisTicks(d))
      assertTrue("$range: la raya de la mitad y la barra «Tú» están (la marca es la mitad: la barra la tapa y la frase lo dice)", b.containsKey("contrarreloj_bell_you_bar"))
    }
  }

  @Test fun Rescate_con_una_marca_de_la_pista_2_arranca_sin_marcas_y_la_dice_como_pista_anterior() {
    // la pista de Rescate es la 3 (la marca cuenta por cápsula): lo guardado con la regla de rondas no se mezcla con lo nuevo
    show(compare = estWorld, ageRange = AgeRange.R45_54, selected = "radar", results = listOf(r(12, 20, game = "radar", version = 2)))
    val b = boxes()
    assertEquals("Rescate arranca sin marcas: una sola línea, sin losa vacía", "Todavía no tienes marcas en este juego.", n("contrarreloj_free_record"))
    assertNull(b["contrarreloj_empty"])
    assertNull("ni marcas ni ritmo de la pista vieja", b["contrarreloj_marks"])
    assertEquals("Tu récord de la pista 2: 12 aciertos. Las marcas de otra pista no se comparan.", n("contrarreloj_older_text"))
    assertEquals("la partida vieja cuenta como una guardada: falta para la tercera", Comparison.EARLY_NOTICE, n("contrarreloj_early_notice"))
  }
}
