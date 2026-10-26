package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.AreaPoints
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.data.ProgressPoint
import com.example.data.StarMeasures
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.DomainType
import com.example.ui.components.BoardBackground
import com.example.ui.screens.AvanceAreaPage
import com.example.ui.screens.AvanceAreaGrid
import com.example.ui.screens.AvanceBestMarks
import com.example.ui.screens.AvanceWeekLine
import com.example.ui.theme.TitleFamily
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.util.Calendar

/**
 * Avance con PUNTOS por área (Tarea 159; `auditoria-fable/avance-b-puntos.md` §3 y §4; los cuatro cuadros de 2 × 2 y Rubik, Tarea 174), con sus piezas reales y datos fijos: los cuatro cuadros (nombre en Rubik, puntos con «de 1000 puntos» o «Aún sin medir» y una barra del color del área; con letra de 1,3 y 2,0 nada se corta ni se pisa),
 * la línea de la semana, «Tus mejores marcas» y la página de un área (nombre, puntos, etapa, lo que falta, el gráfico de semanas o la frase de cuándo aparece, una tarjeta por juego con su nivel en palabras y su medida, y la nota honesta). Sin «Jugar» en ningún sitio y sin palabras sueltas.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h4000dp-440dpi", sdk = [36])
class AvancePuntosTest {
  @get:Rule val rule = createComposeRule()

  private val day = 86_400_000L
  private val now = Calendar.getInstance().apply { clear(); set(2026, Calendar.OCTOBER, 21, 12, 0, 0) }.timeInMillis
  private val names = DomainType.values().associate { it.name to it.displayName }
  private val areas = listOf(
    PlaceTestData.points("MEMORIA", 470, 12), PlaceTestData.points("ATENCION", 310, 0), PlaceTestData.points("RAZONAMIENTO", 440, -8, best = 520), PlaceTestData.points("LENGUAJE", 0, 0, measured = false)
  )

  private fun show(fontScale: Float = 1f, content: @androidx.compose.runtime.Composable () -> Unit) {
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale)) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(3800.dp)) { BoardBackground(); Column(Modifier.fillMaxSize().padding(horizontal = 22.dp)) { content() } } }
      }
    }
    rule.waitForIdle()
  }

  private fun textOf(tag: String): String = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.Text].joinToString("") { it.text }
  private fun bounds(tag: String): Rect = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode().boundsInRoot
  private fun count(tag: String) = rule.onAllNodes(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNodes().size
  private fun textCount(text: String, substring: Boolean = true) = rule.onAllNodes(hasText(text, substring = substring), useUnmergedTree = true).fetchSemanticsNodes().size

  // ---- La semana en una línea

  @Test
  fun la_semana_es_una_linea_con_las_farolas_y_los_dias_de_la_meta() {
    show { AvanceWeekLine(Week.Lamps(listOf(true, true, false, false, false, false, false), todayIndex = 2), goal = 4, empty = false) }
    assertEquals("2 de 4 días de tu meta", textOf("avance_week_line"))
    rule.onNodeWithTag("avance_lamps", useUnmergedTree = true).assertIsDisplayed()
  }

  @Test
  fun la_frase_de_la_semana_va_al_lado_de_las_farolas_solo_si_cabe_en_una_linea() {
    var scale by androidx.compose.runtime.mutableStateOf(1f)
    var days by androidx.compose.runtime.mutableIntStateOf(0)
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale)) {
        NeuroVidaTheme { Box(Modifier.width(393.dp)) { Column(Modifier.padding(horizontal = 22.dp)) { AvanceWeekLine(Week.Lamps(List(7) { it < days }, todayIndex = 3), goal = 4, empty = false) } } }
      }
    }
    for (s in listOf(1f, 1.3f, 2f)) for (d in listOf(0, 2, 6)) {
      rule.runOnIdle { scale = s; days = d }; rule.waitForIdle()
      val line = bounds("avance_week_line"); val lamps = bounds("avance_lamps")
      val beside = line.left >= lamps.right - 1f
      assertTrue("a $s con $d días la frase queda al lado o debajo de las farolas, nunca encima ni pisándolas", beside || line.top >= lamps.bottom - 1f)
      if (beside) assertTrue("a $s con $d días la frase está al lado de las farolas pero mide ${line.height}: se partió en dos líneas", line.height <= 24f * rule.density.density * s + 1f)
    }
  }

  @Test
  fun sin_ninguna_partida_la_semana_empieza_hoy() {
    show { AvanceWeekLine(Week.Lamps(List(7) { false }, todayIndex = 0), goal = 4, empty = true) }
    assertEquals("Tu semana empieza hoy", textOf("avance_week_line"))
  }

  // ---- Los cuatro cuadros (Tarea 174: antes eran cuatro filas con la etapa, la barra de las 5 etapas y la frase de la semana)

  /** Ninguna palabra del texto de [tag] queda partida entre dos líneas (se mira el diseño real del texto). */
  private fun assertNoWordSplit(tag: String, note: String) {
    val layout = layoutOf(tag)
    val text = layout.layoutInput.text.text
    Regex("\\S+").findAll(text).forEach { m ->
      assertEquals("$note: la palabra «${m.value}» de «$text» quedó partida entre dos líneas", layout.getLineForOffset(m.range.first), layout.getLineForOffset(m.range.last))
    }
  }

  private fun layoutOf(tag: String): androidx.compose.ui.text.TextLayoutResult {
    val node = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode()
    val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
    node.config[androidx.compose.ui.semantics.SemanticsActions.GetTextLayoutResult].action?.invoke(results)
    return results.firstOrNull() ?: error("«$tag» no entregó su diseño de texto")
  }

  private fun layoutOfText(text: String): androidx.compose.ui.text.TextLayoutResult {
    val node = rule.onNode(hasText(text), useUnmergedTree = true).fetchSemanticsNode()
    val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
    node.config[androidx.compose.ui.semantics.SemanticsActions.GetTextLayoutResult].action?.invoke(results)
    return results.firstOrNull() ?: error("«$text» no entregó su diseño de texto")
  }

  private val keys = areas.map { it.area.lowercase() }

  @Test
  fun cada_cuadro_dice_su_nombre_sus_puntos_y_su_barra_y_nada_mas() {
    var opened: String? = null
    show { AvanceAreaGrid(areas, names) { opened = it } }
    assertEquals("Memoria", textOf("area_name_memoria")); assertEquals("Atención", textOf("area_name_atencion")); assertEquals("Razonamiento", textOf("area_name_razonamiento")); assertEquals("Lenguaje", textOf("area_name_lenguaje"))
    assertEquals("470", textOf("area_points_memoria")); assertEquals("310", textOf("area_points_atencion")); assertEquals("440", textOf("area_points_razonamiento"))
    assertEquals("cada cifra lleva su «de cuánto»: «de 1000 puntos» en los tres que ya suman", 3, textCount("de 1000 puntos"))
    assertEquals("un área sin partidas no dice «0 de 1000 puntos»: dice lo mismo que el resto de la app", "Aún sin medir", textOf("area_unmeasured_lenguaje"))
    assertEquals("y no lleva cifra", 0, count("area_points_lenguaje"))
    for (k in keys) rule.onNodeWithTag("area_bar_$k", useUnmergedTree = true).assertExists()
    // lo que se fue de la vista principal (vive en la página del área): la etapa, lo que falta, la frase de la semana y la mejor marca
    for (gone in listOf("Etapa", "Esta semana", "Te faltan", "Tu mejor marca", "Todavía sin puntos", "Hábil", "Aprendiz")) assertEquals("«$gone» ya no va en el cuadro", 0, textCount(gone))
    for (bad in listOf(" de 100 ", "pts", "→", "▲", "▼", "sem.")) assertEquals("«$bad» no va", 0, textCount(bad))
    rule.onNodeWithTag("progress_area_atencion").performClick()
    assertEquals("el cuadro entero se toca y abre su página", "ATENCION", opened)
  }

  @Test
  fun el_lector_de_pantalla_lee_cada_cuadro_entero() {
    show { AvanceAreaGrid(areas, names) {} }
    fun desc(k: String) = rule.onNodeWithTag("progress_area_$k").fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.ContentDescription].joinToString(" ")
    assertEquals("Memoria, 470 de 1000 puntos", desc("memoria"))
    assertEquals("Razonamiento, 440 de 1000 puntos", desc("razonamiento"))
    assertEquals("Lenguaje, aún sin medir", desc("lenguaje"))
  }

  @Test
  fun la_barra_llena_lo_que_dicen_los_puntos_y_un_area_sin_jugar_la_lleva_vacia() {
    show { AvanceAreaGrid(areas, names) {} }
    for ((k, pts) in listOf("memoria" to 470, "atencion" to 310, "razonamiento" to 440)) {
      val bar = bounds("area_bar_$k"); val fill = bounds("area_bar_fill_$k")
      assertEquals("$k: la barra llena ${pts / 10} % del riel", pts / 1000f, fill.width / bar.width, 0.01f)
      assertTrue("$k: el relleno queda dentro del riel", fill.left >= bar.left - 1f && fill.right <= bar.right + 1f && Math.abs(fill.height - bar.height) < 2f)
    }
    assertEquals("un área sin jugar lleva la barra vacía: solo el riel", 0, count("area_bar_fill_lenguaje"))
    assertTrue("el riel mide 16 dp de alto", Math.abs(bounds("area_bar_memoria").height / rule.density.density - 16f) < 1f)
  }

  /** La cuadrícula: dos por fila con letra hasta 1,3, uno por fila con letra mayor; el nombre, los puntos y la barra de cada cuadro, a la misma altura en los de una misma fila y en el mismo orden; nada se parte ni se sale. */
  private fun assertGridFits(fontScale: Float, columns: Int) {
    show(fontScale) { AvanceAreaGrid(areas, names) {} }
    val cards = keys.map { bounds("progress_area_$it") }
    val screen = Rect(0f, 0f, 393f * rule.density.density, 3800f * rule.density.density)
    for ((k, c) in keys.zip(cards)) {
      assertTrue("$k ($fontScale): el cuadro $c cabe en el ancho", c.left >= screen.left && c.right <= screen.right)
      assertTrue("$k ($fontScale): el cuadro mide 48 dp de alto o más para tocarlo", c.height >= 48 * rule.density.density)
      val name = bounds("area_name_$k"); val bar = bounds("area_bar_$k")
      assertTrue("$k ($fontScale): el nombre queda dentro del cuadro", name.left >= c.left - 1f && name.right <= c.right + 1f && name.top >= c.top - 1f)
      assertTrue("$k ($fontScale): la barra queda dentro del cuadro y debajo del nombre", bar.left >= c.left - 1f && bar.right <= c.right + 1f && bar.top >= name.bottom - 1f && bar.bottom <= c.bottom + 1f)
      val nameLayout = layoutOf("area_name_$k")
      assertEquals("$k ($fontScale): el nombre va en UNA línea, entero", 1, nameLayout.lineCount)
      assertTrue("$k ($fontScale): el nombre no se corta (la caja mide ${name.width} px y el texto entero ${nameLayout.getLineRight(0)})", name.width >= nameLayout.getLineRight(0) - 1f)
      assertTrue("$k ($fontScale): el nombre mide 16 sp o más (${nameLayout.layoutInput.style.fontSize})", nameLayout.layoutInput.style.fontSize.value >= 16f)
      assertEquals("$k ($fontScale): el nombre va en Rubik", TitleFamily, nameLayout.layoutInput.style.fontFamily)
      if (count("area_points_$k") > 0) {
        val pts = bounds("area_points_$k"); val label = bounds("area_label_$k")
        assertTrue("$k ($fontScale): nombre, cifra, «de 1000 puntos» y barra, en ese orden", name.bottom <= pts.top + 1f && pts.bottom <= label.top + 1f && label.bottom <= bar.top + 1f)
        assertNoWordSplit("area_label_$k", "cuadro de $k a letra $fontScale")
      }
    }
    assertEquals("los cuatro nombres, del mismo tamaño", 1, keys.map { layoutOf("area_name_$it").layoutInput.style.fontSize.value }.toSet().size)
    // la rejilla: 2 × 2 (Memoria y Atención arriba, Razonamiento y Lenguaje abajo) o una columna
    val m = cards[0]; val a = cards[1]; val r = cards[2]; val l = cards[3]
    if (columns == 2) {
      assertTrue("2 × 2 ($fontScale): Memoria y Atención en la misma fila, Atención a la derecha", Math.abs(m.top - a.top) < 2f && a.left >= m.right - 1f)
      assertTrue("2 × 2 ($fontScale): Razonamiento y Lenguaje en la fila de abajo", Math.abs(r.top - l.top) < 2f && r.top >= m.bottom - 1f && l.left >= r.right - 1f)
      for ((x, y) in listOf(keys[0] to keys[1], keys[2] to keys[3])) {
        assertTrue("2 × 2 ($fontScale): el nombre de $x y el de $y a la misma altura", Math.abs(bounds("area_name_$x").top - bounds("area_name_$y").top) < 2f)
        assertTrue("2 × 2 ($fontScale): la barra de $x y la de $y a la misma altura (${bounds("area_bar_$x").top} y ${bounds("area_bar_$y").top})", Math.abs(bounds("area_bar_$x").top - bounds("area_bar_$y").top) < 2f)
      }
    } else {
      assertTrue("un cuadro por fila ($fontScale): uno debajo de otro, del mismo ancho", m.bottom <= a.top + 1f && a.bottom <= r.top + 1f && r.bottom <= l.top + 1f && Math.abs(m.width - l.width) < 2f)
    }
  }

  @Test fun los_cuadros_caben_dos_por_fila_con_letra_1_0() = assertGridFits(1f, 2)
  @Test fun los_cuadros_caben_dos_por_fila_con_letra_1_3_y_razonamiento_entra_entero() = assertGridFits(1.3f, 2)
  @Test fun con_letra_2_0_va_un_cuadro_por_fila_y_ninguna_palabra_se_parte() = assertGridFits(2f, 1)

  @Test
  fun la_cuadricula_con_letra_1_3_mide_poco_y_deja_el_resto_de_avance_a_la_vista() {
    show(1.3f) { AvanceAreaGrid(areas, names) {} }
    val top = bounds("progress_area_memoria").top; val bottom = bounds("progress_area_lenguaje").bottom
    val h = (bottom - top) / rule.density.density
    assertTrue("las 4 áreas miden $h dp con letra 1,3 (a 393 × 873 caben con el título y la semana sin deslizar si no pasan de ~330)", h <= 330f)
  }

  // ---- Rubik en los títulos de Avance (desde la Tarea 175 lo es de toda la app: `TitulosRubikTest`)

  @Test
  fun el_titulo_de_las_secciones_de_avance_va_en_rubik_y_el_cuerpo_no() {
    show {
      Column {
        AvanceAreaGrid(areas, names) {}
        AvanceBestMarks(emptyList(), now)
      }
    }
    for (t in listOf("Tus áreas", "Tus mejores marcas")) assertEquals("«$t» en Rubik", TitleFamily, layoutOfText(t).layoutInput.style.fontFamily)
    assertTrue("la ayuda bajo el título sigue en el cuerpo (no Rubik)", layoutOfText("Toca un área para ver sus juegos y tus marcas").layoutInput.style.fontFamily != TitleFamily)
    assertTrue("y la cifra de los puntos, que es de Fredoka", layoutOf("area_points_memoria").layoutInput.style.fontFamily != TitleFamily)
  }

  // ---- Tus mejores marcas

  private fun series(key: String, vararg v: Float) = v.mapIndexed { i, x -> MeasurePoint(now - (v.size - i) * day, key, x, 0.5f, true, PlayMode.A_TU_MEDIDA) }

  /** La misma serie pero con la última partida HOY (la anterior, ayer…). */
  private fun seriesToday(key: String, vararg v: Float) = v.mapIndexed { i, x -> MeasurePoint(now - (v.size - 1 - i) * day, key, x, 0.5f, true, PlayMode.A_TU_MEDIDA) }

  @Test
  fun las_mejores_marcas_llevan_unidad_y_fecha_y_solo_con_tres_partidas() {
    val measures = series("glance", 200f, 180f, 160f, 190f) + series("trail", 4f, 5f, 7f, 6f) + series("tracking", 2f, 3f)             // el seguimiento tiene 2 partidas: no entra
    val marks = StarMeasures.bestMarks(measures, max = 3)
    assertEquals(listOf("trail", "glance").toSet(), marks.map { it.def.key }.toSet())
    show { AvanceBestMarks(marks, now) }
    rule.onNodeWithText("Tu vistazo más rápido").assertExists(); rule.onNodeWithText("Tu rastro más largo").assertExists()
    rule.onNodeWithText("0,16 segundos de destello").assertExists()
    rule.onNodeWithText("7 luces seguidas").assertExists()
    assertEquals("nada en milisegundos ni abreviado", 0, textCount(" ms")); assertEquals(0, textCount("°"))
    assertEquals("sin tres partidas no hay nada que decir", 0, StarMeasures.bestMarks(series("glance", 200f, 180f)).size)
  }

  @Test
  fun sin_mejores_marcas_dice_cuando_aparecen() {
    show { AvanceBestMarks(emptyList(), now) }
    rule.onNodeWithText("Cuando juegues tres partidas de un mismo juego, aquí aparece tu mejor marca.").assertExists()
  }

  // ---- La página de un área

  private val memoria = listOf("parejas", "secuencia", "bodega", "correo")

  /** Memoria con sus 4 juegos jugados en distintos días y 9 semanas de historia (más de las 8 del gráfico). */
  private fun logMemoria(): List<ProgressPoint> {
    val l = mutableListOf<ProgressPoint>()
    for (w in 8 downTo 0) {
      val t = Week.weekStart(now, w) + day
      l += ProgressPoint("secuencia", t, 0.30f + (8 - w) * 0.03f)
      if (w <= 6) l += ProgressPoint("parejas", t + 3_600_000L, 0.20f + (6 - w) * 0.04f)
      if (w <= 3) l += ProgressPoint("bodega", t + 7_200_000L, 0.20f + (3 - w) * 0.05f)
    }
    return l
  }

  private fun page(log: List<ProgressPoint>, area: String = "MEMORIA", fontScale: Float = 1f, measures: List<MeasurePoint> = emptyList(), onBack: () -> Unit = {}, played: Map<String, Long> = emptyMap()) {
    val games = AreaPoints.gamesOf(area)
    val p = AreaPoints.compute(area, games, log, now)
    show(fontScale) {
      AvanceAreaPage(p, AreaPoints.weekly(games, log, now), names.getValue(area), played, measures, AppLanguage.SPANISH, now, onBack, Modifier.height(3700.dp))
    }
  }

  @Test
  fun la_pagina_del_area_lleva_todo_lo_de_la_especificacion_y_ningun_jugar() {
    val measures = series("trail", 4f, 5f, 7f, 5f)
    page(logMemoria(), measures = measures, played = mapOf("secuencia" to now, "parejas" to now - 2 * day, "bodega" to now - 3 * day))
    val p = AreaPoints.compute("MEMORIA", memoria, logMemoria(), now)
    assertEquals("Memoria", textOf("page_title"))
    assertEquals("${p.total} puntos de 1000 · Etapa ${p.stageName}", textOf("page_points"))
    rule.onNodeWithTag("page_bar", useUnmergedTree = true).assertExists()
    assertEquals(AreaPoints.toNextSentence(p, "Memoria"), textOf("page_to_next"))
    rule.onNodeWithTag("page_chart", useUnmergedTree = true).assertExists()
    rule.onNodeWithText("Tus puntos en las últimas 8 semanas").assertExists()
    assertEquals("${AreaPoints.weekly(memoria, logMemoria(), now).last()} puntos", textOf("chart_last"))
    assertEquals("hace 7 semanas", textOf("chart_axis_first")); assertEquals("esta semana", textOf("chart_axis_last"))
    rule.onNodeWithText("Tus juegos de Memoria").assertExists()
    rule.onNodeWithText("Cada juego aporta hasta 250 puntos. Suben cuando llegas a niveles más altos.").assertExists()
    for (g in memoria) rule.onNodeWithTag("page_game_$g", useUnmergedTree = true).assertExists()
    rule.onNodeWithText("Qué pones en juego en Memoria").assertExists()
    rule.onNodeWithText(com.example.ui.screens.HONEST_NOTE).assertExists()
    assertEquals("la página mira, Hoy juega: ni un «Jugar»", 0, textCount("Jugar"))
    assertEquals("ni un botón de jugar por ningún lado", 0, count("place_play") + count("btn_discovery_play"))
  }

  /** El texto con su formato: (el texto, si el rótulo del comienzo —hasta los dos puntos o el punto— va en negrita y solo él). */
  private fun annotated(tag: String): androidx.compose.ui.text.AnnotatedString =
    rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.Text].first()

  private fun assertLabelBold(tag: String, label: String) {
    val ann = annotated(tag)
    assertTrue("«$tag»: el rótulo «$label» va en negrita y solo él", ann.spanStyles.any { it.item.fontWeight == androidx.compose.ui.text.font.FontWeight.Bold && it.start == 0 && it.end == label.length })
    assertTrue("«$tag» empieza con su rótulo", ann.text.startsWith(label))
  }

  @Test
  fun cada_tarjeta_tiene_cuatro_filas_fijas_nombre_barra_con_puntos_nivel_y_ultima_partida() {
    page(logMemoria(), measures = seriesToday("trail", 4f, 7f, 6f, 5f), played = mapOf("secuencia" to now, "parejas" to now - 2 * day, "bodega" to now - 3 * day))
    val p = AreaPoints.compute("MEMORIA", memoria, logMemoria(), now)
    val rastro = p.perGame.first { it.gameId == "secuencia" }
    assertEquals("${rastro.points} de 250 puntos", textOf("page_game_points_secuencia"))
    // fila 3: «Tu nivel:» con el nivel en palabras (la «Ya» en minúscula, con su punto); Rastro también la dice (antes se ocultaba)
    assertTrue(textOf("page_game_level_secuencia"), textOf("page_game_level_secuencia").startsWith("Tu nivel: ya repites ") && textOf("page_game_level_secuencia").contains("luces seguidas") && textOf("page_game_level_secuencia").endsWith("."))
    assertTrue(textOf("page_game_level_parejas").startsWith("Tu nivel: ya resuelves cielos de "))
    assertTrue("Bodega: UN solo rasgo extra", textOf("page_game_level_bodega").startsWith("Tu nivel: ya recuerdas "))
    // fila 4: «Tu última partida, hoy:» con la fecha en el rótulo; Rastro, sin «Tu mejor marca» (el nivel ya dice lo que repites seguido)
    assertEquals("Tu última partida, hoy: 5 luces seguidas.", textOf("page_game_last_secuencia"))
    assertEquals("sin medida comparable todavía: el rótulo termina en punto y no en dos puntos", "Tu última partida, hace 2 días.", textOf("page_game_last_parejas"))
    assertEquals("Tu última partida, hace 3 días.", textOf("page_game_last_bodega"))
    assertEquals("un juego sin partidas", "Todavía sin partidas. Este juego aporta hasta 250 puntos.", textOf("page_game_empty_correo"))
    assertEquals("sin partidas no hay fila de última partida", 0, count("page_game_last_correo"))
    // el rótulo va en negrita y solo él; el resto, en normal
    assertLabelBold("page_game_level_secuencia", "Tu nivel:"); assertLabelBold("page_game_last_secuencia", "Tu última partida, hoy:")
    assertLabelBold("page_game_last_parejas", "Tu última partida, hace 2 días."); assertLabelBold("page_game_empty_correo", "Todavía sin partidas.")
    // nada de «Jugado …», ni de «·» uniendo fragmentos, ni de «Jugar»
    assertEquals(0, textCount("Jugado")); assertEquals("y sin «Jugar»", 0, count("page_game_play_correo"))
    for (g in memoria) for (tag in listOf("page_game_level_$g", "page_game_last_$g", "page_game_empty_$g")) if (count(tag) > 0) assertFalse("«$tag» sin «·»", textOf(tag).contains("·"))
  }

  @Test
  fun las_cuatro_filas_van_en_el_mismo_orden_y_las_frases_al_borde_izquierdo() {
    page(logMemoria(), measures = seriesToday("trail", 4f, 7f, 6f, 5f), played = mapOf("secuencia" to now, "parejas" to now - 2 * day, "bodega" to now - 3 * day))
    for (g in listOf("secuencia", "parejas", "bodega")) {
      val name = bounds("page_game_name_$g"); val bar = bounds("page_game_bar_$g"); val pts = bounds("page_game_points_$g"); val level = bounds("page_game_level_$g"); val last = bounds("page_game_last_$g"); val card = bounds("page_game_$g")
      assertTrue("$g: nombre, barra, nivel y última partida, de arriba abajo", name.bottom <= bar.top + 1f && bar.bottom <= level.top + 1f && pts.bottom <= level.top + 1f && level.bottom <= last.top + 1f)
      assertEquals("$g: el nivel y la última partida empiezan en el mismo borde izquierdo", level.left, last.left, 1f)
      assertTrue("$g: ese borde es el de la tarjeta (más su relleno), no el del texto del nombre", level.left < name.left && level.left <= card.left + 16f * rule.density.density)
      assertTrue("$g: todo queda dentro de la tarjeta", listOf(name, bar, pts, level, last).all { it.left >= card.left - 1f && it.right <= card.right + 1f && it.top >= card.top - 1f && it.bottom <= card.bottom + 1f })
    }
  }

  @Test
  fun una_ultima_partida_de_otro_dia_lleva_la_fecha_en_el_rotulo_y_en_palabras() {
    val far = listOf(MeasurePoint(now - 50 * day, "trail", 4f, 0.5f, true, PlayMode.A_TU_MEDIDA), MeasurePoint(now - 48 * day, "trail", 7f, 0.5f, true, PlayMode.A_TU_MEDIDA), MeasurePoint(now - 45 * day, "trail", 5f, 0.5f, true, PlayMode.A_TU_MEDIDA))
    page(logMemoria(), measures = far, played = mapOf("secuencia" to now - 45 * day))
    assertEquals("hace 6 semanas, no «hace 45 días»", "Tu última partida, hace 6 semanas: 5 luces seguidas.", textOf("page_game_last_secuencia"))
  }

  @Test
  fun una_ultima_partida_de_ayer_lo_dice_en_el_rotulo() {
    page(logMemoria(), measures = series("trail", 4f, 5f, 7f, 5f), played = mapOf("secuencia" to now - day))
    assertEquals("ayer", "Tu última partida, ayer: 5 luces seguidas.", textOf("page_game_last_secuencia"))
  }

  @Test
  fun un_juego_jugado_pero_aun_sin_medir_lo_dice_y_su_barra_va_vacia() {
    page(emptyList(), played = mapOf("parejas" to now - 2 * day))
    assertEquals("0 de 250 puntos", textOf("page_game_points_parejas"))
    assertEquals("Aún sin medir: juega a tu medida y empieza a sumar.", textOf("page_game_level_parejas"))
    assertEquals("Tu última partida, hace 2 días.", textOf("page_game_last_parejas"))
    assertLabelBold("page_game_level_parejas", "Aún sin medir:")
    assertEquals("Todavía sin partidas. Este juego aporta hasta 250 puntos.", textOf("page_game_empty_bodega"))
  }

  @Test
  fun atencion_aporta_hasta_200_por_juego_y_freno_dice_su_zona_en_una_oracion_aparte() {
    val log = listOf(ProgressPoint("radar", Week.weekStart(now, 0) + day, 0.5f), ProgressPoint("freno", Week.weekStart(now, 0) + day, 0.6f))
    page(log, "ATENCION", measures = seriesToday("glance", 200f, 180f, 150f, 160f) + series("brake", 270f, 262f, 248f, 255f), played = mapOf("radar" to now, "freno" to now - day))
    rule.onNodeWithText("Cada juego aporta hasta 200 puntos. Suben cuando llegas a niveles más altos.").assertExists()
    assertEquals("100 de 200 puntos", textOf("page_game_points_radar"))
    assertTrue(textOf("page_game_level_radar").startsWith("Tu nivel: ya rescatas "))
    assertEquals("Rescate dice su medida completa, con la mejor marca", "Tu última partida, hoy: 0,16 segundos de destello. Tu mejor marca: 0,15 segundos.", textOf("page_game_last_radar"))
    assertEquals("Tu nivel: ya eliges entre 3 plataformas.", textOf("page_game_level_freno"))
    assertEquals("Freno: su medida es un promedio, no una partida", "Tu última partida, ayer. Tu freno, promedio de tus últimas 4 partidas: zona firme.", textOf("page_game_last_freno"))
    assertLabelBold("page_game_last_freno", "Tu última partida, ayer.")
  }

  @Test
  fun con_menos_de_cuatro_semanas_no_hay_grafico_hay_una_frase() {
    val log = listOf(ProgressPoint("secuencia", Week.weekStart(now, 1) + day, 0.4f), ProgressPoint("secuencia", Week.weekStart(now, 0) + day, 0.5f))
    page(log)
    assertEquals(0, count("page_chart"))
    assertEquals("Llevas 2 semanas en Memoria. El gráfico aparece cuando tengas 4.", textOf("page_weeks_empty"))
  }

  @Test
  fun un_area_sin_partidas_dice_que_todavia_no_hay_puntos_y_que_cada_juego_suma_desde_su_primera_partida() {
    page(emptyList())
    assertEquals("un área sin medir no dice «0 puntos de 1000»: dice solo «Aún sin medir»", "Aún sin medir", textOf("page_points"))
    assertEquals("y no lleva etapa aparte", 0, count("page_stage"))
    assertEquals("Todavía sin puntos en Memoria: cada juego suma desde su primera partida a tu medida.", textOf("page_to_next"))
    assertEquals("Todavía no hay semanas en Memoria. El gráfico aparece cuando tengas 4.", textOf("page_weeks_empty"))
    for (g in memoria) assertEquals("Todavía sin partidas. Este juego aporta hasta 250 puntos.", textOf("page_game_empty_$g"))
  }

  @Test
  fun con_una_bajada_en_el_mes_la_pagina_dice_que_los_puntos_suben_y_bajan_un_poco() {
    val log = logMemoria() + ProgressPoint("secuencia", Week.weekStart(now, 0) + day, 0.35f)             // esta semana bajó
    page(log)
    rule.onNodeWithTag("page_ups_downs", useUnmergedTree = true).assertExists()
    assertEquals(AreaPoints.UPS_AND_DOWNS, textOf("page_ups_downs"))
  }

  @Test
  fun atras_de_la_pagina_vuelve_a_avance() {
    var back = 0
    page(logMemoria(), onBack = { back += 1 })
    rule.onNodeWithTag("area_back", useUnmergedTree = true).performClick()
    assertEquals(1, back)
    assertTrue("«‹ Avance» mide 48 dp o más", bounds("area_back").height >= 48 * rule.density.density - 1f)
  }

  private fun assertPageFits(fontScale: Float) {
    page(logMemoria(), fontScale = fontScale, measures = series("trail", 4f, 5f, 7f, 5f), played = mapOf("secuencia" to now, "parejas" to now - 2 * day, "bodega" to now - 3 * day))
    val width = 393 * rule.density.density
    for (tag in listOf("page_title", "page_points", "page_to_next", "page_what", "page_note", "page_game_level_parejas", "page_game_last_secuencia", "page_game_points_secuencia", "chart_first", "chart_last", "chart_axis_first", "chart_axis_last")) {
      val b = bounds(tag)
      assertTrue("«$tag» ($fontScale) cabe a lo ancho: $b", b.left >= 0f && b.right <= width + 1f)
    }
    for (t in listOf("page_title", "page_points", "page_to_next", "page_what", "page_note", "page_game_name_secuencia", "page_game_name_parejas", "page_game_level_parejas", "page_game_last_secuencia", "page_game_last_parejas", "page_game_empty_correo", "page_game_name_bodega", "page_game_name_correo")) assertNoWordSplit(t, "página a letra $fontScale")
    // los puntos de cada tarjeta van al lado de la barra o DEBAJO, enteros, y a una letra dada TODAS las tarjetas hacen lo mismo
    val inline = memoria.map { g -> val bar = bounds("page_game_bar_$g"); val pts = bounds("page_game_points_$g"); (pts.left >= bar.right - 1f) to (pts.top >= bar.bottom - 1f) }
    for ((beside, below) in inline) assertTrue("($fontScale) los puntos van al lado de la barra o debajo de ella", beside || below)
    assertEquals("($fontScale) todas las tarjetas ponen los puntos en el mismo lugar", 1, inline.map { it.first }.toSet().size)
    for (g in memoria) assertNoWordSplit("page_game_points_$g", "puntos de $g a letra $fontScale")
    val bar = bounds("page_bar"); val names = count("page_stages")
    assertTrue("la barra de etapas se ve", bar.height > 0f && names == 1)
    // las dos cifras del gráfico no se pisan entre sí ni con el eje, a ninguna letra (a 2,0 una cifra de 16 sp mide ~42 dp)
    val last = bounds("chart_last"); val first = bounds("chart_first"); val axisFirst = bounds("chart_axis_first"); val axisLast = bounds("chart_axis_last")
    assertTrue("($fontScale) la cifra de arriba ($last) queda sobre la de abajo ($first)", last.bottom <= first.top + 1f)
    assertTrue("($fontScale) la cifra de abajo ($first) no pisa el eje ($axisFirst)", first.bottom <= axisFirst.top + 1f && first.bottom <= axisLast.top + 1f)
    assertTrue("($fontScale) los dos rótulos del eje no se pisan: $axisFirst y $axisLast", axisFirst.right <= axisLast.left + 1f || axisFirst.bottom <= axisLast.top + 1f)
  }

  @Test fun la_pagina_cabe_con_letra_1_0() = assertPageFits(1f)
  @Test fun la_pagina_cabe_con_letra_1_3() = assertPageFits(1.3f)
  @Test fun la_pagina_cabe_con_letra_2_0() = assertPageFits(2f)

  @Test
  fun con_letra_grande_los_nombres_de_las_etapas_pasan_a_una_frase_y_nada_se_parte() {
    page(logMemoria(), fontScale = 1.3f)
    rule.onNode(hasText("Etapas: Inicio · Aprendiz · Hábil · Experto · Maestro", substring = true), useUnmergedTree = true).assertExists()
    rule.onNode(hasText("Vas en ", substring = true), useUnmergedTree = true).assertExists()                 // dice cuál es la tuya
  }
}
