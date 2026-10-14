package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.ui.components.BoardBackground
import com.example.ui.components.ClayDisc
import com.example.ui.components.ClaySlab
import com.example.ui.components.DiscStyle
import com.example.ui.components.Glyph
import com.example.ui.components.KitBackButton
import com.example.ui.components.KitButton
import com.example.ui.components.KitCloseButton
import com.example.ui.components.KitGlyph
import com.example.ui.components.SlabColor
import com.example.ui.components.SlabSize
import com.example.ui.components.slabImagesOrNull
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
 * Las piezas del kit de plasticina de cerca (Tareas 89 y 90), a 3× (xxhdpi): cada losa en sus colores y radios, normal y presionada (y estirada a un segundo tamaño, para ver el nine-slice), los discos con sus íconos y los íconos de churro tinta,
 * sobre el fondo de la app y sobre la crema de la lámina. Deja `kit-piezas-fondo.png` y `kit-piezas-crema.png` en `src/test/screenshots/` (git-ignorada).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w420dp-h1060dp-xxhdpi", sdk = [36])
class KitPiecesScreenshotTest {
  @get:Rule val rule = createComposeRule()

  @Composable
  private fun Slab(size: SlabSize, color: SlabColor, w: Int, h: Int, pressed: Boolean, label: String) {
    ClaySlab(color, Modifier.width(w.dp).heightIn(min = h.dp), size = size, onClick = {}, forcePressed = pressed) {
      Box(Modifier.width(w.dp).height((h - 6).dp), contentAlignment = Alignment.Center) { Text(label, color = Clay.Ink, fontSize = 14.sp, fontWeight = FontWeight.Bold) }
    }
  }

  @Composable
  private fun Pieces(onCream: Boolean) {
    val ink = if (onCream) Clay.Ink else Color.White
    Column(Modifier.width(420.dp).padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
      for (size in SlabSize.values()) {
        val (w, h) = when (size) { SlabSize.SMALL -> 84 to 48; SlabSize.MEDIUM -> 84 to 62; SlabSize.LARGE -> 96 to 84 }
        for (color in SlabColor.values()) {
          if (slabImagesOrNull(size, color) == null) continue
          Row(horizontalArrangement = Arrangement.spacedBy(10.dp), verticalAlignment = Alignment.CenterVertically) {
            Slab(size, color, w, h, pressed = false, label = "${size.key} ${color.name.lowercase()}")
            Slab(size, color, w, h, pressed = true, label = "presionada")
            Slab(size, color, w + 90, h, pressed = false, label = "ancha")
          }
        }
      }
      Row(horizontalArrangement = Arrangement.spacedBy(14.dp), verticalAlignment = Alignment.CenterVertically) {
        KitCloseButton({}, Modifier)
        KitCloseButton({}, Modifier, forcePressed = true)
        KitBackButton({})
        KitBackButton({}, forcePressed = true)
        ClayDisc(DiscStyle.SKY, {}, "Tu perfil") { Text("R", color = Clay.Ink, fontSize = 20.sp, fontWeight = FontWeight.Bold) }
        ClayDisc(DiscStyle.CREAM, {}, "Opciones") { KitGlyph(Glyph.GEAR, size = 28.dp) }
      }
      Row(horizontalArrangement = Arrangement.spacedBy(14.dp), verticalAlignment = Alignment.CenterVertically) {
        for (g in Glyph.values()) Box(Modifier.background(if (onCream) Color(0x10000000) else Color(0x22FFFFFF))) { KitGlyph(g, size = 24.dp) }
      }
      Text("íconos a 24 dp", color = ink, fontSize = 14.sp)
      KitButton("Empezar la ruta · 3 juegos", {}, glyph = Glyph.PLAY, fontSize = 18.sp)
      KitButton("Compartir", {}, color = SlabColor.CREAM, glyph = Glyph.SHARE, fontSize = 18.sp)
    }
  }

  @Test
  fun piezas_sobre_el_fondo() {
    rule.setContent { NeuroVidaTheme { Box(Modifier.width(420.dp).height(1040.dp).testTag("raiz")) { BoardBackground(); Pieces(false) } } }
    rule.waitForIdle()
    rule.onNodeWithTag("raiz").captureRoboImage(filePath = "src/test/screenshots/kit-piezas-fondo.png")
  }

  @Test
  fun piezas_sobre_la_crema_de_la_lamina() {
    rule.setContent { NeuroVidaTheme { Box(Modifier.width(420.dp).height(1040.dp).testTag("raiz").background(Color(0xFFF5EBDB))) { Pieces(true) } } }
    rule.waitForIdle()
    rule.onNodeWithTag("raiz").captureRoboImage(filePath = "src/test/screenshots/kit-piezas-crema.png")
  }
}
