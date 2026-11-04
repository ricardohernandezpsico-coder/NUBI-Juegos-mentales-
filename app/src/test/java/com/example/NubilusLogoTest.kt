package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.MutableState
import androidx.compose.runtime.mutableStateOf
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.hasContentDescription
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.HOY_GREETING_TOP
import com.example.ui.screens.HOY_LOGO_BOX
import com.example.ui.screens.HOY_LOGO_SHIFT
import com.example.ui.screens.HOY_LOGO_TOP
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeUi
import com.example.ui.screens.homeStops
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
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
import kotlin.math.max
import kotlin.math.min

/**
 * El logotipo «Nubilus» de arriba de Hoy (Tarea 223; L1 de la lámina de la 222): 6 dp de aire, 40 dp de caja y 2 dp hasta el saludo (Hoy baja 20 dp), Fredoka Bold de 30 dp que NO crece con la letra del sistema, el lector lo dice UNA vez como
 * encabezado, dentro de la zona sin luceros, y solo en Hoy (Juegos, Contrarreloj, Avance y Tú no lo llevan). El dibujo (crema con contorno de tinta y sombra dura) lo da `Tarea223CapturasTest`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class NubilusLogoTest {
  @get:Rule val rule = createComposeRule()

  @Before fun setUp() { TestSupport.fastFrames() }

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 26, 16, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, record = 41)
  private val ui = HomeUi(
    name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 3, streakNote = "Un día de descanso por semana no la corta.", weeklyGoal = 4, lamps = lamps,
    stops = homeStops(listOf("bodega", "calculo", "intrusa"), 1, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true, timeTrial = stroop
  )

  private class Args(val widthDp: Int, val heightDp: Int, val fontScale: Float)
  private var holder: MutableState<Args>? = null

  private fun show(widthDp: Int = 393, heightDp: Int = 873, fontScale: Float = 1f) {
    val args = Args(widthDp, heightDp, fontScale)
    holder?.let { it.value = args; rule.waitForIdle(); return }
    val state = mutableStateOf(args).also { holder = it }
    rule.setContent {
      val a = state.value
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, a.fontScale), LocalReduceMotion provides true, LocalBarInset provides 88.dp) {
        NeuroVidaTheme { Box(Modifier.width(a.widthDp.dp).height(a.heightDp.dp)) { BoardBackground(); HomeContent(ui = ui, onPlay = {}, onResume = {}, onBaseline = {}) } }
      }
    }
    rule.waitForIdle()
  }

  private val d get() = rule.density.density
  private fun top(tag: String) = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y / d
  private fun bottom(tag: String) = rule.onNodeWithTag(tag).fetchSemanticsNode().let { (it.positionInRoot.y + it.size.height) / d }
  private fun width(tag: String) = rule.onNodeWithTag(tag).fetchSemanticsNode().size.width / d
  private fun height(tag: String) = rule.onNodeWithTag(tag).fetchSemanticsNode().size.height / d

  @Test
  fun el_lector_dice_Nubilus_una_sola_vez_y_como_encabezado() {
    show()
    val nodes = rule.onAllNodes(hasContentDescription("Nubilus")).fetchSemanticsNodes()
    assertEquals("«Nubilus» se dice UNA vez (las cuatro capas de dibujo no se leen)", 1, nodes.size)
    assertTrue("es un encabezado", nodes.single().config.contains(SemanticsProperties.Heading))
    assertEquals("las capas de dibujo no publican su texto en el árbol que lee el lector", 0, rule.onAllNodesWithText("Nubilus").fetchSemanticsNodes().size)
  }

  @Test
  fun no_crece_con_la_letra_del_sistema_y_cabe_en_una_linea() {
    show(fontScale = 1f); val w1 = width("home_logo"); val h1 = height("home_logo")
    for (scale in listOf(1.3f, 2f)) {
      show(fontScale = scale)
      assertEquals("el logotipo mide lo mismo de ancho con letra $scale", w1, width("home_logo"), 1f)
      assertEquals("y lo mismo de alto con letra $scale", h1, height("home_logo"), 1f)
    }
    show(widthDp = 360, heightDp = 780, fontScale = 2f)
    assertTrue("cabe en una línea a 360 dp con letra 2,0 (%.0f × %.0f dp)".format(width("home_logo"), height("home_logo")), height("home_logo") <= HOY_LOGO_BOX.value && width("home_logo") <= 360f - 20f - 16f)
  }

  @Test
  fun mide_4_dp_de_aire_40_de_caja_y_4_hasta_el_saludo_y_baja_Hoy_20_dp() {
    assertEquals(4f, HOY_LOGO_TOP.value, 0f); assertEquals(40f, HOY_LOGO_BOX.value, 0f); assertEquals(4f, HOY_GREETING_TOP.value, 0f)           // Tarea 225: antes 6 + 40 + 2; la suma sigue siendo 48
    assertEquals("Hoy baja lo que mide el logotipo menos los 28 dp de aire de antes", 20f, HOY_LOGO_SHIFT.value, 0f)
    show()
    val scrollTop = top("home_scroll")
    assertEquals("el saludo empieza 4 + 40 + 4 = 48 dp bajo el borde de Hoy (antes 28)", 48f, top("home_greeting") - scrollTop, 0.6f)
    val logoCenter = (top("home_logo") + bottom("home_logo")) / 2f - scrollTop
    assertEquals("el logotipo va centrado en su caja de 40 dp, que empieza a los 4 dp: su centro queda a 24 dp, el del botón «Tú» de arriba a la derecha (Tarea 225)", 4f + 20f, logoCenter, 1f)
    assertEquals("a la izquierda, con el mismo margen del saludo (20 dp)", rule.onNodeWithTag("home_greeting").fetchSemanticsNode().positionInRoot.x, rule.onNodeWithTag("home_logo").fetchSemanticsNode().positionInRoot.x, 1f)
  }

  @Test
  fun el_crema_de_las_letras_pasa_de_4_5_a_1_sobre_el_cielo() {
    fun channel(v: Float): Double = if (v <= 0.03928f) v / 12.92 else Math.pow((v + 0.055) / 1.055, 2.4)
    fun lum(c: Color): Double = 0.2126 * channel(c.red) + 0.7152 * channel(c.green) + 0.0722 * channel(c.blue)
    val cream = Color(0xFFF6EBD3)
    for (sky in listOf(Color(21, 21, 57), Color(7, 7, 31), Color(28, 27, 82))) {
      val ratio = (max(lum(cream), lum(sky)) + 0.05) / (min(lum(cream), lum(sky)) + 0.05)
      assertTrue("las letras crema sobre el cielo ($sky) al menos 4,5:1 (%.1f)".format(ratio), ratio >= 4.5)
    }
  }

  @Test
  fun el_encabezado_de_Hoy_con_el_logotipo_sigue_dentro_de_la_zona_sin_luceros() {
    // `ZONAS_HOY` (`tools/isla/isla.py`): de 3 % a 28 % del alto, a todo el ancho; el contenido de Hoy empieza bajo las barras del sistema (24 dp de barra de estado como mínimo)
    val statusBar = 24f
    val notes = mutableListOf<String>()
    for ((w, h, scale) in listOf(Triple(393, 873, 1f), Triple(393, 873, 1.3f), Triple(360, 780, 1f), Triple(360, 780, 1.3f))) {
      show(widthDp = w, heightDp = h, fontScale = scale)
      assertTrue("el logotipo empieza bajo el 3 %% de la zona (%dx%d letra %.1f)".format(w, h, scale), top("home_logo") + statusBar >= 0.03f * h)
      val week = bottom("home_week") + statusBar
      assertTrue("el saludo, la fecha y la semana (marcas y racha) terminan antes del 28 %% (%.0f dp de %.0f; %dx%d letra %.1f)".format(week, 0.28f * h, w, h, scale), week <= 0.28f * h)
      notes += "%dx%d letra %.1f: la semana termina en el %.1f %%, su nota en el %.1f %%".format(w, h, scale, 100f * week / h, 100f * (bottom("streak_note") + statusBar) / h)
    }
    println("MEDIDA zona sin luceros (28 %%): " + notes.joinToString(" | "))
  }

  @Test
  fun el_logotipo_es_solo_de_Hoy() {
    val users = File("src/main/java").walkTopDown().filter { it.isFile && it.extension == "kt" && "NubilusLogo(" in it.readText() }.map { it.name }.toSet()
    assertEquals("`NubilusLogo` se define en su archivo y se usa solo en `HomeScreen`", setOf("NubilusLogo.kt", "HomeScreen.kt"), users)
    val others = listOf("AllGamesScreen.kt", "ContrarrelojScreen.kt", "ContrarrelojPage.kt", "ProgressScreen.kt", "ProfileScreen.kt").map { File("src/main/java/com/example/ui/screens/$it") }.filter { it.exists() }
    assertEquals("las pestañas Juegos, Contrarreloj, Avance y Tú existen en la prueba", 5, others.size)
    others.forEach { assertTrue("${it.name} no lleva logotipo", "NubilusLogo" !in it.readText()) }
  }

  @Test
  fun nadie_mas_que_el_logotipo_dibuja_el_nombre_con_contorno() {
    // el dibujo del logotipo vive en un solo archivo y no usa el lector (clearAndSetSemantics): «Nubilus» una vez, como encabezado
    val src = File("src/main/java/com/example/ui/components/NubilusLogo.kt").readText()
    assertTrue("`heading()`", "heading()" in src); assertTrue("`clearAndSetSemantics`", "clearAndSetSemantics" in src)
    assertTrue("30 dp, sin sp: no crece con la letra del sistema", "30.dp.toSp()" in src && "30.sp" !in src)
  }
}
