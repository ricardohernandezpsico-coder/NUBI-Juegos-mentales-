package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onChildren
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Skill
import com.example.ui.components.StageScale
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

/**
 * La escala de las 5 etapas (Tarea 143, ítems 1, 8 y 12 de la revisión de Fable del 19-oct): UN solo dibujo para la lámina de Hoy, el detalle de un área y la ficha. Con la letra del sistema al 130 % y al 200 %, y en una pantalla angosta, NINGÚN rótulo se corta
 * («Aprendi / z») ni se parte una palabra: o caben los cinco nombres cada uno entero en su casilla, o va la frase «Etapas: Inicio · Aprendiz · Hábil · Experto · Maestro. Vas en Hábil».
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class StageScaleTest {
  @get:Rule val rule = createComposeRule()

  // UNA sola composición por prueba (Compose no deja llamar a setContent dos veces): el ancho, la letra, el fondo y el valor son estado y cada `show` los cambia
  private var widthDp by mutableStateOf(393)
  private var scale by mutableStateOf(1f)
  private var card by mutableStateOf(true)
  private var measure by mutableStateOf<Float?>(0.47f)

  @Before
  fun setUp() {
    rule.setContent {
      CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, scale)) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).background(if (card) Color(0xFFFFF8EC) else Color(0xFF0B0C33)).padding(horizontal = 16.dp)) {
            StageScale(measure, card, change = 0.05f)
          }
        }
      }
    }
  }

  private fun show(widthDp: Int, fontScale: Float, onCard: Boolean, value: Float? = 0.47f) {
    rule.runOnIdle { this.widthDp = widthDp; scale = fontScale; card = onCard; measure = value }
    rule.waitForIdle()
  }

  /** Cada texto de la escala, con su diseño: ninguno se desborda ni a lo ancho ni a lo alto. */
  private fun texts(): List<Pair<String, TextLayoutResult>> =
    rule.onNodeWithTag("stage_names").onChildren().fetchSemanticsNodes().map { node ->
      val text = node.config[SemanticsProperties.Text].joinToString("") { it.text }
      val results = mutableListOf<TextLayoutResult>()
      node.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
      text to (results.firstOrNull() ?: throw AssertionError("«$text» no dio su diseño de texto"))
    }

  private fun assertNothingCut() {
    rule.onNodeWithTag("stage_bar").assertIsDisplayed()
    rule.onNodeWithTag("stage_names").assertIsDisplayed()
    val shown = texts()
    assertTrue("hay rótulos", shown.isNotEmpty())
    for ((text, layout) in shown) {
      assertFalse("«$text» se corta a lo ancho", layout.didOverflowWidth)
      assertFalse("«$text» se corta a lo alto", layout.didOverflowHeight)
    }
    val names = shown.map { it.first }
    if (names.size == Skill.STAGES.size) {
      // las cinco columnas: cada nombre ENTERO y en una línea
      assertEquals(Skill.STAGES, names)
      for ((text, layout) in shown) assertEquals("«$text» ocupa más de una línea", 1, layout.lineCount)
    } else {
      // la frase: trae los cinco nombres enteros (se puede partir entre palabras, nunca a media palabra)
      assertEquals(1, names.size)
      for (stage in Skill.STAGES) assertTrue("la frase trae «$stage» entero: ${names[0]}", names[0].contains(stage))
      assertTrue("y dónde vas: ${names[0]}", names[0].endsWith("Vas en Hábil"))
    }
  }

  @Test
  fun con_letra_normal_en_un_telefono_comun_van_los_cinco_nombres_enteros() {
    for (onCard in listOf(true, false)) {
      show(393 - 40, 1f, onCard)
      assertNothingCut()
      assertEquals("cinco nombres a 14 sp, cada uno entero", Skill.STAGES, texts().map { it.first })
    }
  }

  @Test
  fun con_letra_130_y_200_ningun_rotulo_se_corta_ni_se_parte_una_palabra() {
    for (fontScale in listOf(1.3f, 2f)) for (onCard in listOf(true, false)) for (width in listOf(393, 360)) {
      show(width, fontScale, onCard)
      assertNothingCut()
      assertEquals("con la letra al ${(fontScale * 100).toInt()} % va la frase, no las cinco columnas", 1, texts().size)
    }
  }

  @Test
  fun en_un_telefono_angosto_con_letra_normal_nunca_se_corta_aprendiz() {
    for (width in listOf(320, 340, 360)) for (onCard in listOf(true, false)) {
      show(width, 1f, onCard)
      assertNothingCut()                       // si «Aprendiz» no cabe en su casilla, pasa la frase
    }
  }

  @Test
  fun sin_medir_solo_se_ve_el_canal_y_los_nombres_sin_decir_donde_vas() {
    show(393, 1.3f, onCard = true, value = null)
    rule.onNodeWithTag("stage_bar").assertIsDisplayed()
    val (text, _) = texts().single()
    assertEquals("Etapas: Inicio · Aprendiz · Hábil · Experto · Maestro", text)
  }

  @Test
  fun la_escala_es_la_misma_en_la_lamina_el_detalle_y_la_ficha() {
    // Tarea 143, ítem 12: tres dibujos distintos pasaron a uno solo; desde la Tarea 155 el «detalle» ya no existe (la lámina de un área es la misma de Hoy en las tres pestañas): quedan la lámina y la ficha
    val read = { path: String -> java.io.File("src/main/java/com/example/ui/screens/$path").readText() }
    assertTrue(read("HoyPlace.kt").contains("StageScale("))
    assertTrue(read("GameSheet.kt").contains("StageScale("))
    assertFalse("ya no hay una barra propia en la lámina", read("HoyPlace.kt").contains("private fun StageBar"))
    assertFalse("ya no hay rótulos de 13 sp en el detalle", read("HomeScreen.kt").contains("st, fontSize = 13.sp"))
    assertFalse("ya no hay una línea de puntos en la ficha", read("GameSheet.kt").contains("private fun StageLine"))
  }
}
