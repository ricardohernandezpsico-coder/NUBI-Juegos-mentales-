package com.example

import androidx.compose.foundation.layout.Column
import androidx.compose.ui.graphics.Canvas
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.CanvasDrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.TextMeasurer
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.createFontFamilyResolver
import androidx.compose.ui.text.drawText
import androidx.compose.ui.text.withStyle
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.LayoutDirection
import androidx.test.core.app.ApplicationProvider
import com.example.ui.components.haloLayoutOf
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.ui.components.OnBoardHalo
import com.example.ui.components.OnBoardText
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
 * `OnBoardText` (Tarea 87): el texto claro que va directo sobre el fondo lleva una orilla navy DENSA (un contorno del color del tablero pintado detrás, con el mismo trazado) para conservar su contraste local si un lucero
 * (una bolita de plasticina) queda detrás de sus letras. El contraste se midió con píxeles reales en el emulador (`unity/test-results/t87_contrast.py`: p5 ≥ 4,5:1 en Hoy, Juegos y Avance a 393 y 360 dp) y en una prueba de laboratorio
 * (una sombra desenfocada de 7 dp deja la letra a 1,0-1,9:1 contra una bolita; el contorno de 3,5 dp, a 4,9-11,5:1); Robolectric no deja leer píxeles aquí (`captureToImage` se cuelga), así que esta prueba fija lo que SÍ se puede:
 * que el contorno no cambia ni la medida ni la semántica del texto, y que su ancho y color son los medidos.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [36])
class OnBoardTextTest {
  @get:Rule val rule = createComposeRule()

  @Test
  fun el_contorno_es_del_azul_del_tablero_y_mide_al_menos_3_dp() {
    assertEquals("el contorno es del color de la lámina «Espacio profundo» en su zona más común (mediana 21, 21, 57; `BoardBackgroundContrastTest` la mide)", androidx.compose.ui.graphics.Color(0xFF151539), OnBoardHalo.Tone)
    assertTrue("el contorno mide al menos 3 dp (con 2,5 dp la orilla no cubre el anillo de 1-2 dp alrededor de la letra y el contraste local baja a 1,9:1)", OnBoardHalo.Outline >= 3.dp)
  }

  @Test
  fun es_UN_solo_nodo_de_texto_con_su_texto_de_siempre() {
    rule.setContent { NeuroVidaTheme { OnBoardText("nivel 18 Aprendiz", color = Color.White, fontSize = 16.sp) } }
    rule.waitForIdle()
    assertEquals("un solo nodo con ese texto (el contorno no duplica la semántica)", 1, rule.onAllNodes(hasText("nivel 18 Aprendiz")).fetchSemanticsNodes().size)
  }

  @Test
  fun mide_y_se_alinea_igual_que_Text() {
    rule.setContent {
      NeuroVidaTheme {
        Column(Modifier.width(300.dp)) {
          Text("Buenas tardes, Ana", modifier = Modifier.fillMaxWidth().testTag("t"), textAlign = TextAlign.Center, fontSize = 22.sp, fontWeight = FontWeight.Bold)
          OnBoardText("Buenas tardes, Ana", modifier = Modifier.fillMaxWidth().testTag("o"), textAlign = TextAlign.Center, fontSize = 22.sp, fontWeight = FontWeight.Bold)
          Text("faltan 245 para Plata y más", modifier = Modifier.testTag("t2"), fontSize = 14.sp, maxLines = 1)
          OnBoardText("faltan 245 para Plata y más", modifier = Modifier.testTag("o2"), fontSize = 14.sp, maxLines = 1)
        }
      }
    }
    rule.waitForIdle()
    for ((a, b) in listOf("t" to "o", "t2" to "o2")) {
      val ra = rule.onNodeWithTag(a).fetchSemanticsNode().boundsInRoot
      val rb = rule.onNodeWithTag(b).fetchSemanticsNode().boundsInRoot
      assertEquals("$b mide lo mismo que $a de ancho", ra.width, rb.width, 0.5f)
      assertEquals("$b mide lo mismo que $a de alto", ra.height, rb.height, 0.5f)
    }
  }

  // ---------------------------------------------------------------- la orilla de un texto con tramos de color (Tarea 194)

  private val sunTone = Color(0xFFFFE6A6)

  /** El texto del vuelo («Practicar poco y seguido…») con «poco y seguido» en amarillo, medido con el estilo de `Say` (Fredoka SemiBold 20 sp, blanco). */
  private fun layoutWithYellowSpan(measurer: TextMeasurer) = measurer.measure(
    text = buildAnnotatedString { append("Practicar "); withStyle(SpanStyle(color = sunTone)) { append("poco y seguido") }; append(" rinde más") },
    style = TextStyle(color = Color.White, fontSize = 20.sp, fontWeight = FontWeight.SemiBold),
    constraints = Constraints(maxWidth = 640)
  )

  /** Dibuja la orilla (el trazo de 3,5 dp) de [layout] en un bitmap transparente y cuenta los píxeles AMARILLOS (rojo y verde altos, azul medio): con el halo bien hecho no hay ninguno. */
  private fun yellowPixelsOfHalo(layout: androidx.compose.ui.text.TextLayoutResult): Int {
    val w = 700; val h = 120
    val bitmap = ImageBitmap(w, h)
    val density = Density(2.625f)
    CanvasDrawScope().draw(density, LayoutDirection.Ltr, Canvas(bitmap), Size(w.toFloat(), h.toFloat())) {
      drawText(layout, color = OnBoardHalo.Tone, drawStyle = Stroke(width = OnBoardHalo.Outline.toPx(), cap = StrokeCap.Round, join = StrokeJoin.Round))
    }
    val px = IntArray(w * h); bitmap.readPixels(px)
    return px.count { c ->
      val a = (c ushr 24) and 0xFF; val r = (c ushr 16) and 0xFF; val g = (c ushr 8) and 0xFF
      a > 40 && r > 0xC0 && g > 0xA0                                        // el amarillo suave: nada parecido al navy de la orilla (#151539)
    }
  }

  @Test
  fun la_orilla_de_un_tramo_amarillo_se_dibuja_en_navy_y_no_en_amarillo() {
    val measurer = TextMeasurer(createFontFamilyResolver(ApplicationProvider.getApplicationContext()), Density(2.625f), LayoutDirection.Ltr)
    val layout = layoutWithYellowSpan(measurer)
    // control: con el diseño ORIGINAL (el que se usaba) la orilla de «poco y seguido» sale amarilla: la prueba SÍ detecta el defecto
    val before = yellowPixelsOfHalo(layout)
    assertTrue("el control debe encontrar píxeles amarillos en el trazo del diseño original (encontró $before)", before > 200)
    // con el diseño de la orilla (copia sin los colores de los tramos) no hay ninguno
    val halo = measurer.haloLayoutOf(layout)
    assertEquals("la copia mide lo mismo (el trazado coincide letra por letra)", layout.size, halo.size)
    assertEquals(layout.lineCount, halo.lineCount)
    for (i in layout.layoutInput.text.text.indices step 3) assertEquals("la letra $i está en el mismo lugar", layout.getBoundingBox(i), halo.getBoundingBox(i))
    assertEquals("la orilla de un tramo amarillo es navy (OnBoardHalo.Tone): ningún píxel amarillo", 0, yellowPixelsOfHalo(halo))
  }

  @Test
  fun un_texto_con_tramo_amarillo_sigue_siendo_un_solo_nodo_con_su_medida() {
    val annotated = buildAnnotatedString { append("Practicar "); withStyle(SpanStyle(color = sunTone)) { append("poco y seguido") }; append(" rinde más") }
    rule.setContent {
      NeuroVidaTheme {
        Column(Modifier.width(300.dp)) {
          Text(annotated, modifier = Modifier.testTag("t"), color = Color.White, fontSize = 20.sp, fontWeight = FontWeight.SemiBold)
          OnBoardText(annotated, modifier = Modifier.testTag("o"), color = Color.White, fontSize = 20.sp, fontWeight = FontWeight.SemiBold)
        }
      }
    }
    rule.waitForIdle()
    assertEquals("un solo nodo con ese texto", 1, rule.onAllNodes(hasText("Practicar poco y seguido rinde más")).fetchSemanticsNodes().filter { it.config.getOrNull(androidx.compose.ui.semantics.SemanticsProperties.TestTag) == "o" }.size)
    val ra = rule.onNodeWithTag("t").fetchSemanticsNode().boundsInRoot
    val rb = rule.onNodeWithTag("o").fetchSemanticsNode().boundsInRoot
    assertEquals("mide lo mismo de ancho", ra.width, rb.width, 0.5f)
    assertEquals("mide lo mismo de alto", ra.height, rb.height, 0.5f)
  }
}
