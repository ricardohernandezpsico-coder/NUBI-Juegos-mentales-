package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.MutableState
import androidx.compose.runtime.mutableStateOf
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.SemanticsMatcher
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.AchievementFamily
import com.example.data.Achievements
import com.example.data.NBSP
import com.example.data.TuStats
import com.example.data.TuTile
import com.example.data.computeAchievementStats
import com.example.data.familyState
import com.example.ui.screens.TuCuadros
import com.example.ui.screens.medalForTile
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
 * Los cuatro cuadros de Tú (Tarea 185), en la variante «Centrada» que eligió Ricardo el 28-oct: los cuatro de la MISMA altura y con su contenido centrado también en altura (el de medallas, sin línea de apoyo, no queda con un hueco abajo), dos columnas con letra hasta 1,3 y una con letra
 * mayor, ninguna palabra partida a la mitad, el número NUNCA separado de su unidad («21 días», «8 de octubre»: espacio duro), informativos (no se tocan, van planos) y cada uno lo lee TalkBack como una frase.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class TuCuadrosTest {
  @get:Rule val rule = createComposeRule()

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 28, 12, 0, 0) }.timeInMillis
  private val medals = AchievementFamily.entries.map { Achievements.familyState(it, emptySet(), computeAchievementStats(emptyList(), emptyMap())) }
  private val tiles = TuStats.tiles(21, 21, 41, Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 8) }.timeInMillis, now, 9, 15, 15, 17)

  private class Args(val fontScale: Float, val widthDp: Int, val tiles: List<TuTile>)
  private var holder: MutableState<Args>? = null

  /** Muestra los cuadros: la primera vez arma el contenido (un solo `setContent` por prueba); las siguientes cambian la letra o el ancho en el mismo contenido. */
  private fun show(fontScale: Float = 1f, widthDp: Int = 393, theTiles: List<TuTile> = tiles) {
    val args = Args(fontScale, widthDp, theTiles)
    holder?.let { it.value = args; rule.waitForIdle(); return }
    val state = mutableStateOf(args).also { holder = it }
    rule.setContent {
      val a = state.value
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, a.fontScale)) {
        NeuroVidaTheme { Box(Modifier.width(a.widthDp.dp).padding(horizontal = 22.dp)) { TuCuadros(a.tiles, streakLit = true, medal = medalForTile(medals)) } }
      }
    }
    rule.waitForIdle()
  }

  private fun bounds(tag: String) = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode().boundsInRoot
  private val keys = listOf("racha", "partidas", "medallas", "juegos")
  private fun isText(n: androidx.compose.ui.semantics.SemanticsNode) = n.config.getOrNull(SemanticsProperties.Text) != null

  @Test
  fun los_cuatro_cuadros_miden_lo_mismo_de_alto_con_cualquier_letra() {
    for (scale in listOf(1f, 1.3f, 2f)) {
      show(scale)
      val heights = keys.map { bounds("tu_cuadro_$it").height }
      assertTrue("letra $scale: alturas $heights", heights.all { kotlin.math.abs(it - heights[0]) < 1f })
    }
  }

  @Test
  fun el_contenido_va_centrado_tambien_en_altura_y_el_de_medallas_no_deja_un_hueco_abajo() {
    for (scale in listOf(1f, 1.3f)) {
      show(scale)
      for (k in keys) {
        val tile = bounds("tu_cuadro_$k"); val icon = bounds("tu_icono_$k")
        val lastTextBottom = rule.onAllNodes(SemanticsMatcher("texto del cuadro $k") { n ->
          isText(n) && n.boundsInRoot.top >= tile.top - 1f && n.boundsInRoot.bottom <= tile.bottom + 1f && n.boundsInRoot.left >= tile.left - 1f && n.boundsInRoot.right <= tile.right + 1f
        }, useUnmergedTree = true).fetchSemanticsNodes().maxOf { it.boundsInRoot.bottom }
        val above = icon.top - tile.top; val below = tile.bottom - lastTextBottom
        val dp = rule.density.density
        assertTrue("letra $scale, «$k»: el aire de arriba (${above / dp} dp) y el de abajo (${below / dp} dp) deben ser parecidos", kotlin.math.abs(above - below) <= 8f * dp)
      }
    }
  }

  @Test
  fun dos_columnas_con_letra_hasta_1_3_y_una_con_letra_mayor() {
    show(1.3f)
    val racha = bounds("tu_cuadro_racha"); val partidas = bounds("tu_cuadro_partidas"); val medallas = bounds("tu_cuadro_medallas")
    assertTrue("racha y partidas lado a lado", partidas.left > racha.right - 1f && kotlin.math.abs(partidas.top - racha.top) < 1f)
    assertTrue("medallas bajo racha", medallas.top > racha.bottom - 1f && kotlin.math.abs(medallas.left - racha.left) < 1f)
    show(2f)
    val r2 = bounds("tu_cuadro_racha"); val p2 = bounds("tu_cuadro_partidas")
    assertTrue("con letra 2,0, uno bajo el otro", p2.top > r2.bottom - 1f && kotlin.math.abs(p2.left - r2.left) < 1f)
    assertEquals("a todo el ancho", r2.width, p2.width, 1f)
  }

  @Test
  fun cada_cuadro_se_lee_como_una_frase_y_no_se_toca() {
    show()
    for ((k, tile) in keys.zip(tiles)) {
      val config = rule.onNodeWithTag("tu_cuadro_$k").fetchSemanticsNode().config
      assertEquals(listOf(tile.spoken), config.getOrNull(SemanticsProperties.ContentDescription))
      assertFalse("«$k» es informativo: no tiene acción de toque", config.contains(SemanticsActions.OnClick))
      assertTrue("y no se anuncia como botón", config.getOrNull(SemanticsProperties.Role) == null)
      assertFalse("lo que lee el lector no lleva espacios duros", tile.spoken.contains(NBSP))
    }
  }

  @Test
  fun ninguna_palabra_se_parte_y_el_numero_nunca_se_separa_de_su_unidad() {
    val worst = tiles + TuStats.tiles(106, 106, 1_234, now, now, 15, 15, 17, 17)
    for (scale in listOf(1f, 1.3f, 2f)) for (width in listOf(360, 393)) {
      show(scale, width, worst)
      val texts = rule.onAllNodes(SemanticsMatcher("texto de un cuadro") { n -> isText(n) }, useUnmergedTree = true).fetchSemanticsNodes()
      assertTrue("hay textos", texts.isNotEmpty())
      for (n in texts) {
        val results = mutableListOf<TextLayoutResult>()
        n.config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(results)
        val layout = results.firstOrNull() ?: continue
        val text = layout.layoutInput.text.text
        for (line in 0 until layout.lineCount - 1) {
          val end = layout.getLineEnd(line, visibleEnd = false)
          val atWordBoundary = end <= 0 || end >= text.length || text[end - 1] == ' ' || text[end] == ' ' || text[end - 1] == '\n'
          assertTrue("letra $scale ancho $width: «$text» se parte a media palabra en el renglón ${line + 1}", atWordBoundary)
          // el renglón se corta en un espacio normal, nunca en uno duro (el que pega «21» a «días» o «8» a «de»)
          assertFalse("letra $scale ancho $width: «$text» se corta en un espacio duro", end in 1 until text.length && (text[end - 1] == NBSP[0] || text[end] == NBSP[0]))
          val before = text.substring(0, end).trimEnd(); val after = text.substring(end).trimStart()
          assertFalse("letra $scale ancho $width: «$text» separa un número de su unidad (renglón ${line + 1})", before.lastOrNull()?.isDigit() == true && after.firstOrNull()?.isLetter() == true)
        }
      }
    }
  }

  @Test
  fun los_cuadros_son_planos_sin_borde_ni_sombra_de_arcilla_y_ya_no_hay_variantes() {
    // lo tocable es arcilla y lo informativo es plano: el cuadro dibuja su fondo crema en el código y no usa la losa de arcilla (ClayCard / ClaySlab)
    val src = java.io.File("src/main/java/com/example/ui/screens/TuCuadros.kt").readText()
    assertTrue(src.contains("Modifier.background(FlatCream, RoundedCornerShape(22.dp))"))
    assertFalse("sin losa de arcilla", src.contains("ClayCard(") || src.contains("ClaySlab(") || src.contains(".border("))
    assertFalse("la variante «Ícono a la izquierda» se borró (Ricardo eligió la «Centrada»)", src.contains("TileStyle") || src.contains("ICONO_A_LA_IZQUIERDA") || src.contains("TileIconLeft"))
  }

  @Test
  fun el_cuadro_de_juegos_dibuja_las_cuatro_casillas_de_las_areas() {
    show()
    assertEquals(1, rule.onAllNodes(hasTestTag("icon_areas"), useUnmergedTree = true).fetchSemanticsNodes().size)
  }
}
