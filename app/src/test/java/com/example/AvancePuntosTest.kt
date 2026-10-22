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
import com.example.ui.screens.AvanceAreaRows
import com.example.ui.screens.AvanceBestMarks
import com.example.ui.screens.AvanceWeekLine
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
 * Avance con PUNTOS por área (Tarea 159; `auditoria-fable/avance-b-puntos.md` §3 y §4), con sus piezas reales y datos fijos: las cuatro filas (puntos grandes, «de 1000 puntos», etapa, barra y frase de la semana; alto de 110 dp o más; con letra de 1,3 y 2,0 nada se corta ni se pisa),
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

  // ---- Las cuatro filas

  @Test
  fun cada_fila_dice_sus_puntos_su_etapa_y_su_semana_con_unidad() {
    var opened: String? = null
    show { AvanceAreaRows(areas, names) { opened = it } }
    assertEquals("470", textOf("area_points_memoria")); assertEquals("Etapa Hábil", textOf("area_stage_memoria"))
    assertEquals("Esta semana sumaste 12 puntos.", textOf("area_line_memoria"))
    assertEquals("310", textOf("area_points_atencion")); assertEquals("Etapa Aprendiz", textOf("area_stage_atencion"))
    assertEquals("Esta semana te mantuviste.", textOf("area_line_atencion"))
    assertEquals("Etapa Hábil", textOf("area_stage_razonamiento"))
    assertEquals("la bajada se dice con la mejor marca al lado, sin rojo ni flechas", "Esta semana bajaste 8 puntos. Tu mejor marca sigue en 520 puntos.", textOf("area_line_razonamiento"))
    assertEquals("un área sin partidas", "Aún sin medir", textOf("area_stage_lenguaje")); assertEquals("0", textOf("area_points_lenguaje"))
    assertEquals("Todavía sin puntos: juega un juego de Lenguaje.", textOf("area_line_lenguaje"))
    assertEquals("cada cifra grande lleva su «de cuánto»", 4, textCount("de 1000 puntos"))
    for (bad in listOf(" de 100 ", "pts", "→", "▲", "▼", "sem.")) assertEquals("«$bad» no va", 0, textCount(bad))
    rule.onNodeWithTag("progress_area_atencion").performClick()
    assertEquals("la fila entera se toca y abre su página", "ATENCION", opened)
  }

  @Test
  fun la_fila_mide_110_dp_o_mas_y_se_lee_completa() {
    show { AvanceAreaRows(areas, names) {} }
    for (a in areas) {
      val b = bounds("progress_area_${a.area.lowercase()}")
      assertTrue("${a.area} mide ${b.height / rule.density.density} dp", b.height >= 110 * rule.density.density - 1f)
    }
    val desc = rule.onNodeWithTag("progress_area_memoria").fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.ContentDescription].joinToString(" ")
    assertEquals("Memoria: etapa Hábil, 470 de 1000 puntos. Esta semana sumaste 12 puntos", desc)
  }

  /** Ninguna palabra del texto de [tag] queda partida entre dos líneas (se mira el diseño real del texto). */
  private fun assertNoWordSplit(tag: String, note: String) {
    val node = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode()
    val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
    node.config[androidx.compose.ui.semantics.SemanticsActions.GetTextLayoutResult].action?.invoke(results)
    val layout = results.firstOrNull() ?: error("«$tag» no entregó su diseño de texto: la prueba no estaría mirando nada")
    val text = layout.layoutInput.text.text
    Regex("\\S+").findAll(text).forEach { m ->
      assertEquals("$note: la palabra «${m.value}» de «$text» quedó partida entre dos líneas", layout.getLineForOffset(m.range.first), layout.getLineForOffset(m.range.last))
    }
  }

  private fun assertRowsFit(fontScale: Float) {
    show(fontScale) { AvanceAreaRows(areas, names) {} }
    for (a in areas) {
      val k = a.area.lowercase()
      val card = bounds("progress_area_$k"); val name = bounds("area_name_$k"); val pts = bounds("area_points_$k"); val stage = bounds("area_stage_$k"); val line = bounds("area_line_$k")
      for ((label, b) in listOf("nombre" to name, "puntos" to pts, "etapa" to stage, "frase" to line)) assertTrue("$k ($fontScale): el $label $b queda dentro de la fila $card", b.left >= card.left - 1f && b.right <= card.right + 1f)
      assertTrue("$k ($fontScale): los puntos no pisan el nombre ni la etapa", pts.left >= name.right - 1f && pts.left >= stage.right - 1f || pts.top >= stage.bottom - 1f)
      assertTrue("$k ($fontScale): la frase de la semana va debajo de todo", line.top >= pts.bottom - 1f && line.top >= stage.bottom - 1f)
      for (t in listOf("area_name_$k", "area_stage_$k", "area_line_$k")) assertNoWordSplit(t, "fila de $k a letra $fontScale")
    }
  }

  @Test fun las_filas_caben_con_letra_1_0() = assertRowsFit(1f)
  @Test fun las_filas_caben_con_letra_1_3() = assertRowsFit(1.3f)
  @Test fun las_filas_caben_con_letra_2_0_con_los_puntos_bajo_el_nombre() {
    assertRowsFit(2f)
    val name = bounds("area_name_razonamiento"); val pts = bounds("area_points_razonamiento")
    assertTrue("con letra 2,0 los puntos bajan bajo el nombre y la etapa", pts.top >= name.bottom - 1f)
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

  @Test
  fun cada_tarjeta_de_juego_dice_sus_puntos_su_nivel_en_palabras_su_medida_y_cuando_se_jugo() {
    page(logMemoria(), measures = seriesToday("trail", 4f, 7f, 6f, 5f), played = mapOf("secuencia" to now, "parejas" to now - 2 * day, "bodega" to now - 3 * day))
    val p = AreaPoints.compute("MEMORIA", memoria, logMemoria(), now)
    val rastro = p.perGame.first { it.gameId == "secuencia" }
    assertEquals("${rastro.points} de 250 puntos", textOf("page_game_points_secuencia"))
    assertEquals("Rastro de luz habla de LO MISMO que su medida (luces seguidas): con la medida a la vista, la frase del nivel no repite la cifra", 0, count("page_game_level_secuencia"))
    assertTrue(textOf("page_game_level_parejas").startsWith("Ya resuelves cielos de "))
    assertTrue("Bodega: UN solo rasgo extra", textOf("page_game_level_bodega").startsWith("Ya recuerdas "))
    assertEquals("la última partida fue hoy: «Hoy: …» y la mejor marca al lado", "Hoy: 5 luces seguidas. Tu mejor marca: 7 luces.", textOf("page_game_measure_secuencia"))
    assertEquals("Jugado hoy", textOf("page_game_when_secuencia")); assertEquals("Jugado hace 2 días", textOf("page_game_when_parejas"))
    assertEquals("un juego sin partidas", "Todavía sin partidas · aporta hasta 250 puntos", textOf("page_game_empty_correo"))
    assertEquals("y sin «Jugar»", 0, count("page_game_play_correo"))
  }

  @Test
  fun sin_medida_a_la_vista_rastro_de_luz_si_dice_sus_luces_en_la_frase_del_nivel() {
    page(logMemoria(), played = mapOf("secuencia" to now))
    assertTrue(textOf("page_game_level_secuencia").startsWith("Ya repites ")); assertTrue(textOf("page_game_level_secuencia").contains("luces seguidas"))
    assertEquals(0, count("page_game_measure_secuencia"))
  }

  @Test
  fun si_la_ultima_partida_no_fue_hoy_dice_en_tu_ultima_partida() {
    page(logMemoria(), measures = series("trail", 4f, 5f, 7f, 5f), played = mapOf("secuencia" to now - 4 * day))
    assertEquals("En tu última partida: 5 luces seguidas. Tu mejor marca: 7 luces.", textOf("page_game_measure_secuencia"))
  }

  @Test
  fun atencion_aporta_hasta_200_por_juego() {
    val log = listOf(ProgressPoint("radar", Week.weekStart(now, 0) + day, 0.5f))
    page(log, "ATENCION")
    rule.onNodeWithText("Cada juego aporta hasta 200 puntos. Suben cuando llegas a niveles más altos.").assertExists()
    assertEquals("100 de 200 puntos", textOf("page_game_points_radar"))
    assertTrue(textOf("page_game_level_radar").startsWith("Ya rescatas "))
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
    assertEquals("0 puntos de 1000 · Aún sin medir", textOf("page_points"))
    assertEquals("Todavía sin puntos en Memoria: cada juego suma desde su primera partida a tu medida.", textOf("page_to_next"))
    assertEquals("Todavía no hay semanas en Memoria. El gráfico aparece cuando tengas 4.", textOf("page_weeks_empty"))
    for (g in memoria) assertEquals("Todavía sin partidas · aporta hasta 250 puntos", textOf("page_game_empty_$g"))
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
    for (tag in listOf("page_title", "page_points", "page_to_next", "page_what", "page_note", "page_game_level_parejas", "page_game_measure_secuencia", "page_game_points_secuencia", "chart_first", "chart_last", "chart_axis_first", "chart_axis_last")) {
      val b = bounds(tag)
      assertTrue("«$tag» ($fontScale) cabe a lo ancho: $b", b.left >= 0f && b.right <= width + 1f)
    }
    for (t in listOf("page_title", "page_points", "page_to_next", "page_what", "page_note", "page_game_name_secuencia", "page_game_name_parejas", "page_game_level_parejas", "page_game_measure_secuencia", "page_game_name_bodega", "page_game_name_correo")) assertNoWordSplit(t, "página a letra $fontScale")
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
