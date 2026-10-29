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
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.FuentesMas
import com.example.data.Streak
import com.example.data.Week
import com.example.ui.components.BoardBackground
import com.example.ui.components.HoyWeek
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.FuentesPanel
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.After
import org.junit.Assume
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import org.robolectric.shadows.ShadowTrace
import java.io.File

/**
 * Las capturas de la lámina de la Tarea 186 («En qué se basa Nubi», ampliada): un juego con «Más estudios» plegado y desplegado, la sección «Cómo está hecha Nubi» al principio y al final, «Lo que no decimos» con las cuatro revisiones (letra 1,0 y 1,3) y las medidas. Después de la aprobación de Opus
 * (28-oct): los grupos de «Cómo está hecha Nubi» van PLEGADOS en «Estudios (N)» (`mecanismos-plegado-*`; los `mecanismos-inicio/final-*` de la lámina son de antes, con los grupos desplegados) y Hoy con la racha en cero (`hoy-racha-cero-*`).
 * Solo corre al grabar capturas; deja los PNG en `src/test/screenshots/tarea-186/propuesta/` y las medidas en `medidas.txt`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h4200dp-440dpi", sdk = [36])
class Tarea186CapturasTest {
  @get:Rule val rule = createComposeRule()

  private val out = "src/test/screenshots/tarea-186/propuesta"
  private val measures = mutableListOf<String>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
  }

  @After fun tearDown() {
    if (measures.isNotEmpty()) File("$out/medidas.txt").apply { parentFile.mkdirs(); appendText(measures.joinToString("\n") + "\n") }
  }

  private var frameH by mutableStateOf(873)
  private var fontScale by mutableStateOf(1f)

  /** El marco de 393 dp con «En qué se basa Nubi» como panel (con las barras del sistema: 24 dp arriba). */
  private fun show(scale: Float = 1f, height: Int = 873) {
    fontScale = scale; frameH = height
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(frameH.dp).testTag("frame")) {
            BoardBackground()
            Box(Modifier.padding(top = 24.dp)) { FuentesPanel(onClose = {}) }
          }
        }
      }
    }
    settle()
  }

  /** Robolectric guarda en `ShadowTrace` cada sección de traza que Compose abre al armar la semántica, y esta pantalla tiene cientos de nodos: sin vaciarla cada tanto se acaba la memoria (java.lang.OutOfMemoryError en `ShadowTrace.endSection`). */
  private fun settle() { repeat(2) { rule.waitForIdle(); ShadowTrace.reset() } }          // el reloj corre solo: con `autoAdvance = false`, `performScrollTo` (un deslizamiento animado) espera para siempre y se acaba la memoria

  private fun shot(name: String) {
    settle()
    rule.onNodeWithTag("frame").captureRoboImage(filePath = "$out/$name.png")
  }

  private fun scrollTo(tag: String) { rule.onNodeWithTag(tag).performScrollTo(); ShadowTrace.reset(); settle() }

  private fun tapMore(id: String) { scrollTo("mas_$id"); rule.onNodeWithTag("mas_$id").performClick(); settle() }

  private fun yOf(tag: String): Float = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y.also { ShadowTrace.reset() } / rule.density.density
  private fun heightOf(tag: String): Float = rule.onNodeWithTag(tag).fetchSemanticsNode().size.height.also { ShadowTrace.reset() } / rule.density.density

  /** Lo que mide la pantalla que se desliza: lo que se ve más lo que falta por deslizar (dp). */
  private fun contentLength(): Float {
    val n = rule.onNodeWithTag("fuentes_screen").fetchSemanticsNode()
    return (n.size.height + n.config[SemanticsProperties.VerticalScrollAxisRange].maxValue()) / rule.density.density
  }

  @Test
  fun mas_estudios_plegado_y_desplegado_con_letra_1_0() {
    show(1f)
    scrollTo("fuente_parejas")
    shot("mas-plegado-1_0")
    tapMore("parejas")
    shot("mas-desplegado-1_0")
  }

  @Test
  fun mas_estudios_plegado_y_desplegado_con_letra_1_3() {
    show(1.3f)
    scrollTo("fuente_parejas")
    shot("mas-plegado-1_3")
    tapMore("parejas")
    shot("mas-desplegado-1_3")
  }

  @Test
  fun como_esta_hecha_nubi_al_principio_y_al_final_con_letra_1_0() {
    show(1f)
    scrollTo("fuentes_mecanismos")
    scrollTo("estudios_tu_eliges")                                  // el título de la sección y los primeros grupos, plegados
    shot("mecanismos-plegado-1_0")
    scrollTo("estudios_elementos_juego")
    scrollTo("fuentes_palabras")                                    // el último grupo y, abajo, donde empieza «Las palabras»
    shot("mecanismos-plegado-final-1_0")
    scrollTo("estudios_dificultad"); rule.onNodeWithTag("estudios_dificultad").performClick(); settle()
    scrollTo("estudio_${FuentesMas.mechanisms.first().studies.last().doi}")
    shot("mecanismos-desplegado-1_0")
  }

  @Test
  fun como_esta_hecha_nubi_al_principio_y_al_final_con_letra_1_3() {
    show(1.3f)
    scrollTo("fuentes_mecanismos")
    scrollTo("estudios_mas_tiempo")
    shot("mecanismos-plegado-1_3")
    scrollTo("estudios_elementos_juego")
    scrollTo("fuentes_palabras")
    shot("mecanismos-plegado-final-1_3")
  }

  @Test
  fun lo_que_no_decimos_con_letra_1_0() {
    show(1f, 1000)
    scrollTo("fuentes_no_decimos")
    scrollTo("fuentes_aviso_salud")
    shot("no-decimos-1_0")
  }

  @Test
  fun lo_que_no_decimos_con_letra_1_3() {
    show(1.3f, 1100)
    scrollTo("fuentes_no_decimos")
    scrollTo("fuentes_aviso_salud")
    shot("no-decimos-1_3")
  }

  /** La semana de Hoy (como la arma `HomeScreen`) con la racha [played] y la mejor [best], en un marco de 393 dp. */
  private fun showHoy(played: Set<Long>, best: Int, scale: Float) {
    val today = 1000L
    val info = Streak.compute(played, today)
    fontScale = scale
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(150.dp).testTag("frame")) {
            BoardBackground()
            Box(Modifier.padding(top = 8.dp)) { HoyWeek(Week.Lamps(List(7) { it in 0..1 }, todayIndex = 3), info.current, Streak.noteFor(info, best), streakText = Streak.counterText(info.current, best)) }
          }
        }
      }
    }
    settle()
  }

  @Test
  fun hoy_con_la_racha_en_cero_y_una_mejor_de_antes() {
    showHoy(setOf(995L, 994L, 993L), best = 12, scale = 1f)
    rule.onNodeWithTag("frame").captureRoboImage(filePath = "$out/hoy-racha-cero-1_0.png")
  }

  @Test
  fun hoy_con_la_racha_en_cero_y_una_mejor_de_antes_con_letra_1_3() {
    showHoy(setOf(995L, 994L, 993L), best = 12, scale = 1.3f)
    rule.onNodeWithTag("frame").captureRoboImage(filePath = "$out/hoy-racha-cero-1_3.png")
  }

  @Test
  fun las_medidas() {
    show(1f, 873)
    val rowTags = FuentesMas.byGame.values.flatten().map { "estudio_${it.doi}" }
    for (scale in listOf(1f, 1.3f, 2f)) {
      rule.runOnIdle { fontScale = scale }
      settle()
      val s = "%.1f".format(scale).replace('.', ',')
      measures += "Letra $s: «En qué se basa Nubi» mide ${"%.0f".format(contentLength())} dp de largo con todo plegado; «Cómo está hecha Nubi» (del título a «Las palabras») mide ${"%.0f".format(yOf("fuentes_palabras") - yOf("fuentes_mecanismos"))} dp; «Más estudios» de Constelaciones mide ${"%.0f".format(heightOf("mas_parejas"))} dp de alto"
    }
    // «Cómo está hecha Nubi» con cada grupo desplegado (Opus pidió plegarlos: así se ve cuánto se evita)
    rule.runOnIdle { fontScale = 1f }
    settle()
    for (m in FuentesMas.mechanisms) { scrollTo("estudios_${m.key}"); rule.onNodeWithTag("estudios_${m.key}").performClick(); settle() }
    measures += "Letra 1,0: con los 10 grupos de «Cómo está hecha Nubi» desplegados la sección mide ${"%.0f".format(yOf("fuentes_palabras") - yOf("fuentes_mecanismos"))} dp (plegados: ver arriba)"
    for (m in FuentesMas.mechanisms) { scrollTo("estudios_${m.key}"); rule.onNodeWithTag("estudios_${m.key}").performClick(); settle() }
    // los 17 «Más estudios» desplegados (con letra 1,0 y después 1,3: quedan abiertos)
    rule.runOnIdle { fontScale = 1f }
    settle()
    for (id in FuentesMas.byGame.keys) { scrollTo("mas_$id"); rule.onNodeWithTag("mas_$id").performClick(); settle() }
    for (scale in listOf(1f, 1.3f)) {
      rule.runOnIdle { fontScale = scale }
      settle()
      val s = "%.1f".format(scale).replace('.', ',')
      val rows = rowTags.map { heightOf(it) }
      measures += "Letra $s: con los 17 «Más estudios» desplegados mide ${"%.0f".format(contentLength())} dp de largo; las filas de estudio miden entre ${"%.0f".format(rows.min())} y ${"%.0f".format(rows.max())} dp de alto (48 dp es el mínimo que se pide)"
    }
  }
}
