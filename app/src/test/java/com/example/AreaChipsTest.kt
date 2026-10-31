package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.BoardBackground
import com.example.ui.screens.AllGamesContent
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
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
import kotlin.math.abs

/**
 * Los chips de área de «Todos los juegos» (Tarea 196, 30-oct; Ricardo los veía «desordenados» y eligió la variante «B ajustada, con color»): filas parejas con una cuadrícula propia (cuántos entran por fila lo decide el chip MÁS ANCHO; partes iguales; borde derecho recto; relleno de 12 dp),
 * el color del área en un hueco antes del nombre (el punto de 12 dp; «Todas», las cuatro casillas) y la elegida en lima con el visto EN ESE HUECO y en negrita (no en sol). Una fila con un solo chip toma todo el ancho. Ningún texto se corta ni pasa a dos líneas con ninguna letra ni ancho.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class AreaChipsTest {
  @get:Rule val rule = createComposeRule()

  private val now = 400 * 86_400_000L + 3_600_000L
  private val places = PlaceTestData.places(now)
  private var area by mutableStateOf<String?>(null)
  private val selections: List<String?> = listOf<String?>(null) + AREA_ORDER
  private val tags = listOf("all_chip_todas") + AREA_ORDER.map { "all_chip_${it.lowercase()}" }

  @Before fun setUp() { TestSupport.fastFrames() }

  private fun show(widthDp: Int, scale: Float, heightDp: Int = 873) {
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale)) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            AllGamesContent(places = places, area = area, query = "", mark = null, onQueryChange = {}, onArea = {}, onClose = {}, onGame = {}, modifier = Modifier.padding(top = 24.dp))
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun choose(key: String?) { area = key; rule.waitForIdle() }
  private fun dp(px: Float) = px / rule.density.density
  private fun chipBounds(): List<Rect> = tags.map { rule.onNodeWithTag(it).fetchSemanticsNode().boundsInRoot }
  private fun rowsOf(bounds: List<Rect>): List<List<Rect>> = bounds.groupBy { Math.round(it.top) }.toSortedMap().values.toList()
  private fun exists(tag: String, unmerged: Boolean = true) = rule.onAllNodes(hasTestTag(tag), useUnmergedTree = unmerged).fetchSemanticsNodes().isNotEmpty()

  // ------------------------------------------------------------------ filas parejas

  private fun filasParejas(widthDp: Int, scale: Float) {
    show(widthDp, scale)
    for (sel in selections) {
      choose(sel)
      val at = "${widthDp} dp, letra $scale, elegida ${sel ?: "Todas"}"
      val rows = rowsOf(chipBounds())
      assertEquals("$at: los cinco chips están", 5, rows.sumOf { it.size })
      for (row in rows) {
        val sorted = row.sortedBy { it.left }
        val widths = sorted.map { dp(it.width) }
        assertTrue("$at: en una fila los chips miden lo mismo: $widths", widths.max() - widths.min() <= 1f)
        assertEquals("$at: la fila empieza a 16 dp del borde: ${dp(sorted.first().left)}", 16f, dp(sorted.first().left), 0.6f)
        assertEquals("$at: y termina a 16 dp del borde derecho: ${dp(sorted.last().right)}", widthDp - 16f, dp(sorted.last().right), 0.6f)
        for (i in 1 until sorted.size) assertEquals("$at: entre chips hay 8 dp", 8f, dp(sorted[i].left - sorted[i - 1].right), 0.6f)
        assertTrue("$at: cada chip toca 48 dp o más: ${dp(sorted.first().height)}", sorted.all { dp(it.height) >= 48f })
      }
    }
  }

  @Test fun a_393_con_letra_1_0_cada_fila_es_pareja_y_llega_a_16_dp_del_borde() = filasParejas(393, 1f)
  @Test fun a_360_con_letra_1_0_cada_fila_es_pareja_y_llega_a_16_dp_del_borde() = filasParejas(360, 1f)
  @Test fun a_393_con_letra_1_3_cada_fila_es_pareja_y_llega_a_16_dp_del_borde() = filasParejas(393, 1.3f)
  @Test fun a_360_con_letra_1_3_cada_fila_es_pareja_y_llega_a_16_dp_del_borde() = filasParejas(360, 1.3f)
  @Test fun a_393_con_letra_2_0_cada_fila_es_pareja_y_llega_a_16_dp_del_borde() = filasParejas(393, 2f)
  @Test fun a_360_con_letra_2_0_cada_fila_es_pareja_y_llega_a_16_dp_del_borde() = filasParejas(360, 2f)

  /** Elegir un chip (que pasa a negrita, más ancha) no reparte las filas de otra manera: los cinco chips quedan en el mismo lugar y del mismo tamaño con cada uno de los cinco elegido. */
  private fun elegirNoMueveNada(widthDp: Int, scale: Float) {
    show(widthDp, scale)
    var first: List<Rect>? = null
    for (sel in selections) {
      choose(sel)
      val bounds = chipBounds()
      val at = "$widthDp dp, letra $scale, elegida ${sel ?: "Todas"} (filas ${rowsOf(bounds).map { it.size }})"
      if (first == null) first = bounds
      else for ((i, pair) in first.zip(bounds).withIndex()) {
        val (a, b) = pair
        assertTrue("$at: elegir un chip movió o cambió de tamaño al chip ${tags[i]}: antes $a, ahora $b", abs(a.left - b.left) < 1f && abs(a.top - b.top) < 1f && abs(a.width - b.width) < 1f && abs(a.height - b.height) < 1f)
      }
    }
  }

  @Test fun a_393_con_letra_1_0_elegir_un_chip_no_mueve_nada() = elegirNoMueveNada(393, 1f)
  @Test fun a_360_con_letra_1_0_elegir_un_chip_no_mueve_nada() = elegirNoMueveNada(360, 1f)
  @Test fun a_393_con_letra_1_3_elegir_un_chip_no_mueve_nada() = elegirNoMueveNada(393, 1.3f)
  @Test fun a_360_con_letra_1_3_elegir_un_chip_no_mueve_nada() = elegirNoMueveNada(360, 1.3f)
  @Test fun a_393_con_letra_2_0_elegir_un_chip_no_mueve_nada() = elegirNoMueveNada(393, 2f)
  @Test fun a_360_con_letra_2_0_elegir_un_chip_no_mueve_nada() = elegirNoMueveNada(360, 2f)

  @Test
  fun a_393_con_letra_1_0_son_dos_filas_3_mas_2_con_cualquier_chip_elegido_y_elegir_no_mueve_nada() {
    show(393, 1f)
    var first: List<Rect>? = null
    for (sel in selections) {
      choose(sel)
      val at = "elegida ${sel ?: "Todas"}"
      val bounds = chipBounds()
      val rows = rowsOf(bounds)
      assertEquals("$at: dos filas, tres arriba y dos abajo (${rows.map { it.size }})", listOf(3, 2), rows.map { it.size })
      assertTrue("$at: arriba, partes de 115 dp: ${rows[0].map { dp(it.width) }}", rows[0].all { abs(dp(it.width) - 115f) <= 1f })
      assertTrue("$at: abajo, partes de 176 dp: ${rows[1].map { dp(it.width) }}", rows[1].all { abs(dp(it.width) - 176.5f) <= 1f })
      assertEquals("$at: el bloque de los chips mide 104 dp (dos filas de 52, como hoy)", 104f, dp(bounds.maxOf { it.bottom } - bounds.minOf { it.top }), 1.5f)
      if (first == null) first = bounds else for ((a, b) in first.zip(bounds)) {
        assertTrue("$at: elegir un chip no mueve ni cambia de tamaño a ningún chip: $a y $b", abs(a.left - b.left) < 1f && abs(a.top - b.top) < 1f && abs(a.width - b.width) < 1f && abs(a.height - b.height) < 1f)
      }
    }
  }

  @Test
  fun a_360_con_letra_1_0_la_fila_que_no_cabe_baja_a_menos_chips_y_la_suelta_toma_todo_el_ancho() {
    show(360, 1f)
    val bounds = chipBounds()
    val rows = rowsOf(bounds)
    assertEquals("a 360 dp con letra 1,0: 2 + 2 + 1 (la parte de tres sería de 104 dp y «Atención» con su hueco pide 112,6)", listOf(2, 2, 1), rows.map { it.size })
    assertEquals("la fila de un solo chip toma todo el ancho (328 dp)", 328f, dp(rows.last().single().width), 0.6f)
    assertEquals("tres filas: 157 dp", 157f, dp(bounds.maxOf { it.bottom } - bounds.minOf { it.top }), 1.5f)
  }

  // ------------------------------------------------------------------ nada se corta

  private fun nadaSeCorta(widthDp: Int, scale: Float) {
    show(widthDp, scale)
    for (sel in selections) {
      choose(sel)
      for (tag in tags) {
        val at = "$widthDp dp, letra $scale, elegida ${sel ?: "Todas"}, chip $tag"
        val chip = rule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot
        val textNode = rule.onNodeWithTag("${tag}_text", useUnmergedTree = true).fetchSemanticsNode()
        val results = ArrayList<TextLayoutResult>()
        textNode.config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(results)
        assertEquals("$at: el texto va en UNA línea (una palabra no se parte)", 1, results.first().lineCount)
        val text = textNode.boundsInRoot
        assertTrue("$at: el texto queda dentro de su losa: $text en $chip", text.left >= chip.left - 0.5f && text.right <= chip.right + 0.5f)
        // el hueco del color (o del visto) también queda dentro y a la izquierda del texto
        val hole = rule.onAllNodes(hasTestTag("all_chip_check") or hasTestTag("all_chip_dot_${tag.removePrefix("all_chip_")}") or hasTestTag("icon_areas"), useUnmergedTree = true).fetchSemanticsNodes().map { it.boundsInRoot }
          .filter { it.left >= chip.left - 0.5f && it.right <= chip.right + 0.5f && it.top >= chip.top - 0.5f && it.bottom <= chip.bottom + 0.5f }
        assertTrue("$at: el hueco del color o del visto está dentro de la losa", hole.isNotEmpty())
        assertTrue("$at: y no se pisa con el texto", hole.all { it.right <= text.left + 0.5f })
      }
    }
  }

  @Test fun a_393_con_letra_1_0_ningun_texto_se_corta_ni_pasa_a_dos_lineas() = nadaSeCorta(393, 1f)
  @Test fun a_360_con_letra_1_0_ningun_texto_se_corta_ni_pasa_a_dos_lineas() = nadaSeCorta(360, 1f)
  @Test fun a_393_con_letra_1_3_ningun_texto_se_corta_ni_pasa_a_dos_lineas() = nadaSeCorta(393, 1.3f)
  @Test fun a_360_con_letra_1_3_ningun_texto_se_corta_ni_pasa_a_dos_lineas() = nadaSeCorta(360, 1.3f)
  @Test fun a_393_con_letra_2_0_ningun_texto_se_corta_ni_pasa_a_dos_lineas() = nadaSeCorta(393, 2f)
  @Test fun a_360_con_letra_2_0_ningun_texto_se_corta_ni_pasa_a_dos_lineas() = nadaSeCorta(360, 2f)

  // ------------------------------------------------------------------ el color y el visto

  @Test
  fun la_elegida_tiene_el_visto_y_no_tiene_punto_y_las_demas_tienen_punto_o_casillas() {
    show(393, 1f)
    for (sel in selections) {
      choose(sel)
      val at = "elegida ${sel ?: "Todas"}"
      val checks = rule.onAllNodes(hasTestTag("all_chip_check"), useUnmergedTree = true).fetchSemanticsNodes()
      assertEquals("$at: hay UN solo visto", 1, checks.size)
      val chipOfSelected = rule.onNodeWithTag("all_chip_${sel?.lowercase() ?: "todas"}").fetchSemanticsNode().boundsInRoot
      val check = checks.single().boundsInRoot
      assertTrue("$at: el visto está dentro del chip elegido", check.left >= chipOfSelected.left && check.right <= chipOfSelected.right && check.top >= chipOfSelected.top && check.bottom <= chipOfSelected.bottom)
      assertEquals("$at: el visto mide 16 dp (20 dp en «Todas»)", if (sel == null) 20f else 16f, dp(check.width), 0.6f)
      for (key in AREA_ORDER) assertEquals("$at: ${key}: el punto está solo si NO es la elegida", key != sel, exists("all_chip_dot_${key.lowercase()}"))
      assertEquals("$at: las casillas de «Todas» están solo si NO es la elegida", sel != null, exists("icon_areas"))
    }
  }

  @Test
  fun el_lector_dice_el_nombre_y_elegida_y_el_punto_y_las_casillas_no_suman_nada() {
    show(393, 1f)
    for (sel in selections) {
      choose(sel)
      for ((tag, key) in tags.zip(listOf<String?>(null) + AREA_ORDER)) {
        val node = rule.onNodeWithTag(tag).fetchSemanticsNode()
        val label = if (key == null) "Todas" else places[key]!!.areaName
        assertEquals("$tag: lo que dice el lector", if (key == sel) "$label, elegida" else label, node.config[SemanticsProperties.ContentDescription].single())
        assertEquals("$tag: elegido o no", key == sel, node.config[SemanticsProperties.Selected])
      }
    }
  }

  // ------------------------------------------------------------------ el código

  private fun source(): String = File("src/main/java/com/example/ui/screens/AllGamesScreen.kt").readLines().joinToString("\n")

  @Test
  fun AreaChip_ya_no_usa_el_sol_y_no_queda_nada_del_prototipo() {
    val s = source()
    val chip = s.substring(s.indexOf("private fun AreaChip(")).substringBefore("\n}")      // el cuerpo de AreaChip, hasta su llave de cierre
    assertFalse("la elegida ya no va en SlabColor.SUN (el único sol es «Nubi sugiere»)", chip.contains("SlabColor.SUN"))
    assertTrue("va en lima", chip.contains("SlabColor.LIME"))
    assertFalse("sin FlowRow con pesos (reparten sin mirar el contenido y cortaban «Atención»)", s.replace(Regex("/\\*.*?\\*/", RegexOption.DOT_MATCHES_ALL), "").replace(Regex("//.*"), "").contains("FlowRow"))
    assertFalse("sin el interruptor del prototipo", s.contains("AREA_CHIP_VARIANT"))
    assertTrue("la cuadrícula explica su porqué", s.contains("¿Por qué no un `FlowRow` con `Modifier.weight(1f)`?"))
  }
}
