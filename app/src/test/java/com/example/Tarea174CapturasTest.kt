package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Week
import com.example.model.DomainType
import com.example.ui.components.BoardBackground
import com.example.ui.components.OnBoardText
import com.example.ui.screens.AvanceAreaGrid
import com.example.ui.screens.AvanceBestMarks
import com.example.ui.screens.AvanceWeekLine
import com.example.ui.theme.AvanceTitleFamily
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assume
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File

/**
 * Las capturas de «Avance: cuatro cuadros y letra Rubik» (Tarea 174, `docs/previews/tarea-174.png`) en un teléfono de 393 × 873 dp con letra 1,0, 1,3 y 2,0, con los datos fijos de `AvancePuntosTest` (un área sin medir: Lenguaje): el título, la semana, los cuatro cuadros y «Tus mejores marcas», dentro de lo que dejan
 * las barras (24 dp arriba, la del sistema y la de pestañas abajo). Deja en `src/test/screenshots/tarea-174/medidas.txt` lo que mide: hasta dónde llegan los cuatro cuadros y cuánto sobra, el tamaño con que entra el nombre más largo y cuánto ancho le queda.
 * No corre en las pruebas de siempre: solo al grabar capturas (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea174CapturasTest*"`).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class Tarea174CapturasTest {
  @get:Rule val rule = createComposeRule()

  @Before fun solo_al_grabar() { Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true") }

  private val now = 400 * 86_400_000L
  private val names = DomainType.values().associate { it.name to it.displayName }
  private val areas = listOf(PlaceTestData.points("MEMORIA", 668, 52), PlaceTestData.points("ATENCION", 488, 35), PlaceTestData.points("RAZONAMIENTO", 290, 21), PlaceTestData.points("LENGUAJE", 0, 0, measured = false))
  private val out = "src/test/screenshots/tarea-174"

  private fun shot(fontScale: Float, name: String) {
    rule.mainClock.autoAdvance = false
    val barDp = if (fontScale >= 1.3f) 94 else 88                         // la barra de pestañas (Tarea 156: 88 dp, 94 con letra 1,3)
    rule.setContent {
      CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, fontScale)) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(873.dp).testTag("frame")) {
            BoardBackground()
            Column(Modifier.padding(top = 24.dp, bottom = (48 + barDp).dp)) {
              Box(Modifier.padding(start = 20.dp, end = 16.dp, top = 8.dp, bottom = 4.dp)) { OnBoardText("Avance", color = Color(0xFFEAF0FF), fontSize = 26.sp, fontWeight = androidx.compose.ui.text.font.FontWeight.Bold, fontFamily = AvanceTitleFamily) }
              Column(Modifier.padding(horizontal = 20.dp, vertical = 8.dp)) {
                AvanceWeekLine(Week.Lamps(listOf(true, true, true, true, true, false, false), todayIndex = 4), goal = 4, empty = false)
                Spacer(Modifier.height(22.dp))
                Box(Modifier.testTag("grid")) { AvanceAreaGrid(areas, names) {} }
                Spacer(Modifier.height(22.dp))
                AvanceBestMarks(emptyList(), now)
              }
            }
            Box(Modifier.align(Alignment.BottomCenter).fillMaxWidth().height((48 + barDp).dp).background(Color(0xFF1C1B52)))     // las barras de abajo (pestañas y sistema)
          }
        }
      }
    }
    repeat(3) { rule.mainClock.advanceTimeBy(600); rule.waitForIdle() }
    rule.onNodeWithTag("frame").captureRoboImage(filePath = "$out/$name.png")
    val d = rule.density.density
    val last = rule.onNodeWithTag("progress_area_lenguaje").fetchSemanticsNode().boundsInRoot
    val first = rule.onNodeWithTag("progress_area_memoria").fetchSemanticsNode().boundsInRoot
    val raz = rule.onNodeWithTag("area_name_razonamiento", useUnmergedTree = true).fetchSemanticsNode().boundsInRoot
    val card = rule.onNodeWithTag("progress_area_razonamiento").fetchSemanticsNode().boundsInRoot
    val room = (873 - 48 - barDp).toFloat()
    val nameSp = run {
      val res = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
      rule.onNodeWithTag("area_name_razonamiento", useUnmergedTree = true).fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsActions.GetTextLayoutResult].action?.invoke(res)
      res.first().layoutInput.style.fontSize.value
    }
    val line = "$name: los cuatro cuadros van de ${"%.0f".format(first.top / d)} a ${"%.0f".format(last.bottom / d)} dp de los ${"%.0f".format(room)} que dejan las barras (${if (last.bottom / d <= room) "entran todos, sobran ${"%.0f".format(room - last.bottom / d)} dp" else "el último no entra: se desliza"}); " +
      "«Razonamiento» mide ${"%.0f".format(raz.width / d)} dp con ${"%.0f".format(nameSp)} sp en un cuadro de ${"%.0f".format(card.width / d)} dp (le sobran ${"%.0f".format((card.width - raz.width) / d - 16f)} dp de los 8 dp de margen de cada lado); ${"%.0f".format(card.width / d)} × ${"%.0f".format(card.height / d)} dp cada cuadro"
    File("$out/medidas.txt").apply { parentFile.mkdirs(); appendText(line + "\n") }
  }

  @Test fun vista_1_0() = shot(1f, "vista-1_0")
  @Test fun vista_1_3() = shot(1.3f, "vista-1_3")
  @Test fun vista_2_0() = shot(2f, "vista-2_0")
}
