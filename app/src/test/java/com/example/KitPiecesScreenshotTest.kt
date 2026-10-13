package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.ui.components.BoardBackground
import com.example.ui.components.ClayKitFlag
import com.example.ui.components.ClaySlab
import com.example.ui.components.KitButton
import com.example.ui.components.KitCloseButton
import com.example.ui.components.SlabColor
import com.example.ui.theme.Clay
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * Las piezas del kit de plasticina de cerca (Tarea 89), a 3× (xxhdpi): cada losa en tres tamaños (para ver el nine-slice estirado) y en sus dos estados (normal y presionada), la X y el botón grande con el triángulo, sobre el fondo de la app y sobre la crema de la lámina.
 * Deja `kit-piezas-fondo.png` y `kit-piezas-crema.png` en `src/test/screenshots/` (git-ignorada).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w420dp-h860dp-xxhdpi", sdk = [36])
class KitPiecesScreenshotTest {
  @get:Rule val rule = createComposeRule()

  @Composable
  private fun Slab(color: SlabColor, w: Int, h: Int, pressed: Boolean, label: String) {
    ClaySlab(color, Modifier.width(w.dp).heightIn(min = h.dp), onClick = {}, forcePressed = pressed) {
      Box(Modifier.width(w.dp).height((h - 6).dp), contentAlignment = androidx.compose.ui.Alignment.Center) { Text(label, color = Clay.Ink, fontSize = 15.sp, fontWeight = FontWeight.Bold) }
    }
  }

  @Composable
  private fun Pieces(onCream: Boolean) {
    Column(Modifier.width(420.dp).padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
      for ((color, name) in listOf(SlabColor.SUN to "Sol", SlabColor.CREAM to "Crema", SlabColor.LIME to "Lima")) {
        Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
          Slab(color, 110, 62, pressed = false, label = "$name")
          Slab(color, 110, 62, pressed = true, label = "$name ↓")
          Slab(color, 150, 98, pressed = false, label = "98 dp")
        }
      }
      Row(horizontalArrangement = Arrangement.spacedBy(14.dp)) {
        KitCloseButton({}, Modifier, "Cerrar")
        KitCloseButton({}, Modifier, "Cerrar", forcePressed = true)
        Text(if (onCream) "← normal · presionada" else "← normal · presionada", color = if (onCream) Clay.Ink else Color.White, fontSize = 14.sp, modifier = Modifier.padding(top = 14.dp))
      }
      KitButton("Empezar la ruta · 3 juegos", {}, play = true, fontSize = 18.sp)
      KitButton("Empezar la ruta · 3 juegos", {}, play = true, fontSize = 18.sp, forcePressed = true)
      Spacer(Modifier.height(4.dp))
    }
  }

  @Test
  fun piezas_sobre_el_fondo() {
    ClayKitFlag.forTest(true)
    rule.setContent { NeuroVidaTheme { Box(Modifier.width(420.dp).height(560.dp).testTag("raiz")) { BoardBackground(); Pieces(false) } } }
    rule.waitForIdle()
    rule.onNodeWithTag("raiz").captureRoboImage(filePath = "src/test/screenshots/kit-piezas-fondo.png")
    ClayKitFlag.forTest(false)
  }

  @Test
  fun piezas_sobre_la_crema_de_la_lamina() {
    ClayKitFlag.forTest(true)
    rule.setContent { NeuroVidaTheme { Box(Modifier.width(420.dp).height(560.dp).testTag("raiz").background(Color(0xFFF5EBDB))) { Pieces(true) } } }
    rule.waitForIdle()
    rule.onNodeWithTag("raiz").captureRoboImage(filePath = "src/test/screenshots/kit-piezas-crema.png")
    ClayKitFlag.forTest(false)
  }
}

