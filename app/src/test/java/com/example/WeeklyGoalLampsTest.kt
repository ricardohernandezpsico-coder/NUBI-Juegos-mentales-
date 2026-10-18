package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.assertIsSelected
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.WEEKLY_GOAL_MAX
import com.example.ui.components.WEEKLY_GOAL_MIN
import com.example.ui.components.WeeklyGoalLamps
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La meta semanal en 7 farolas (Tarea 130, Ajustes): una farola por día de la meta, de 1 a 7; la tocada es la meta; cada una se lee «k días por semana» y es un botón de radio; entran en un teléfono de 360 dp con letra 1,3 y 2,0 sin
 * salirse de la pantalla, y el toque mide al menos 46 × 48 dp.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp", sdk = [36])
class WeeklyGoalLampsTest {
  @get:Rule val rule = createComposeRule()

  private fun show(goal: Int, widthDp: Int = 393, fontScale: Float = 1f, onGoal: (Int) -> Unit = {}) {
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp)) { var g by remember { mutableIntStateOf(goal) }; WeeklyGoalLamps(goal = g, onGoal = { g = it; onGoal(it) }) } }
      }
    }
    rule.waitForIdle()
  }

  @Test
  fun hay_siete_farolas_y_la_de_la_meta_esta_elegida() {
    show(goal = 4)
    for (k in WEEKLY_GOAL_MIN..WEEKLY_GOAL_MAX) rule.onNodeWithTag("goal_lamp_$k").assertExists()
    rule.onNodeWithTag("goal_lamp_4").assertIsSelected()
    assertEquals(7, WEEKLY_GOAL_MAX)
    assertEquals(1, WEEKLY_GOAL_MIN)
  }

  @Test
  fun tocar_una_farola_cambia_la_meta_y_avisa_una_vez() {
    val got = mutableListOf<Int>()
    show(goal = 4, onGoal = { got += it })
    rule.onNodeWithTag("goal_lamp_6").performClick()
    rule.waitForIdle()
    assertEquals(listOf(6), got)
    rule.onNodeWithTag("goal_lamp_6").assertIsSelected()
    rule.onNodeWithTag("goal_lamp_1").performClick()
    rule.waitForIdle()
    assertEquals(listOf(6, 1), got)
    rule.onNodeWithTag("goal_lamp_1").assertIsSelected()                 // el mínimo es 1
  }

  @Test
  fun cada_farola_se_lee_k_dias_por_semana() {
    show(goal = 4)
    for (k in WEEKLY_GOAL_MIN..WEEKLY_GOAL_MAX) {
      val node = rule.onNodeWithTag("goal_lamp_$k").fetchSemanticsNode()
      assertEquals("$k días por semana", node.config.getOrNull(SemanticsProperties.ContentDescription)?.firstOrNull())
      assertEquals("es un botón de radio", androidx.compose.ui.semantics.Role.RadioButton, node.config.getOrNull(SemanticsProperties.Role))
    }
  }

  private fun fits(widthDp: Int, fontScale: Float) {
    show(goal = 4, widthDp = widthDp, fontScale = fontScale)
    val d = rule.density.density
    for (k in WEEKLY_GOAL_MIN..WEEKLY_GOAL_MAX) {
      val b = rule.onNodeWithTag("goal_lamp_$k").fetchSemanticsNode().boundsInRoot
      assertTrue("la farola $k cabe en $widthDp dp con letra $fontScale (derecha ${b.right / d} dp)", b.left >= -0.5f && b.right / d <= widthDp + 0.5f)
      assertTrue("el toque de la farola $k mide al menos 48 dp de alto (${b.height / d})", b.height / d >= 47.9f)
      assertTrue("y al menos 40 dp de ancho (${b.width / d})", b.width / d >= 40f)
    }
  }

  @Test fun entran_en_393_dp() = fits(393, 1f)
  @Test fun entran_en_360_dp_con_letra_1_3() = fits(360, 1.3f)
  @Test fun entran_en_360_dp_con_letra_2_0() = fits(360, 2f)
  @Test fun en_393_dp_las_cajas_miden_46_dp() {
    show(goal = 4)
    val b = rule.onNodeWithTag("goal_lamp_1").fetchSemanticsNode().boundsInRoot
    assertEquals(46f, b.width / rule.density.density, 0.6f)
  }
}
