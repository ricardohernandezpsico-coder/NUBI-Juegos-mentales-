package com.example

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
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
    assertEquals("el contorno es del color de la lámina en su zona más común (mediana 50, 51, 103)", androidx.compose.ui.graphics.Color(0xFF333369), OnBoardHalo.Tone)
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
}
