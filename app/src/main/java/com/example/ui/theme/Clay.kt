package com.example.ui.theme

import androidx.compose.animation.core.animateDpAsState
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.heightIn
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.sp
import com.example.ui.components.ClaySlab
import com.example.ui.components.Glyph
import com.example.ui.components.KitButton
import com.example.ui.components.SlabSize
import com.example.ui.components.slabColorFor

/**
 * Lenguaje visual "noche + arcilla" de NeuroVida (ver NeuroVida/CLAUDE.md): fondo nocturno con estrellas
 * ([com.example.ui.components.CosmosBackground]) y, encima, todo lo tocable en arcilla: bordes gruesos color
 * tinta, sombra dura abajo y colores saturados. Es el mismo lenguaje de los juegos de Unity.
 */
object Clay {
  val Ink = Color(0xFF1A1240)
  val Coral = Color(0xFFFF6B4A)
  val Sun = Color(0xFFFFC93C)
  val Sky = Color(0xFF4CC9F0)
  val Grape = Color(0xFFB8A4FF)
  val Lime = Color(0xFF9BE564)
  val Cream = Color(0xFFFFFFFF)
  val InkSoft = Color(0xFF6B6790)
  /** Texto secundario sobre una losa crema (contraste 7:1). */
  val InkMuted = Color(0xFF514B78)
  val Border = 3.dp
}

/**
 * Tarjeta de plasticina (kit de la Tarea 90): una losa del color más parecido a [color] y del radio de [size] (chica 14 dp para chips y casillas, mediana 22 para botones y tarjetas, grande 30 para láminas y paneles) detrás del contenido; si es clicable,
 * al apoyar el dedo se hunde (imagen presionada). El contenido define el tamaño y el centro de la losa se estira: con letra grande crece sin deformarse.
 */
@Composable
fun ClayCard(
  modifier: Modifier = Modifier,
  color: Color = Clay.Cream,
  size: SlabSize = SlabSize.MEDIUM,
  onClick: (() -> Unit)? = null,
  contentPadding: Dp = 16.dp,
  content: @Composable ColumnScope.() -> Unit
) {
  ClaySlab(slabColorFor(color), modifier, size, onClick) { Column(modifier = Modifier.padding(contentPadding), content = content) }
}

/** Botón de plasticina (acción principal por defecto en sol): losa mediana con texto de Compose y, si hay [glyph], su ícono de churro tinta antes del texto. */
@Composable
fun ClayButton(
  text: String,
  onClick: () -> Unit,
  modifier: Modifier = Modifier,
  color: Color = Clay.Sun,
  textColor: Color = Clay.Ink,
  glyph: Glyph? = null,
  /** El inicio nuevo pide texto de 18 sp o más. */
  fontSize: TextUnit = 17.sp,
  /** false = solo el ancho de su contenido (por defecto ocupa toda la fila). */
  wide: Boolean = true
) {
  KitButton(text, onClick, modifier, slabColorFor(color), glyph, fontSize, textColor, wide)
}

/** Pastilla de plasticina (losa chica de 14 dp; rachas, etiquetas, contadores). No es tocable: solo informa. */
@Composable
fun ClayPill(text: String, modifier: Modifier = Modifier, color: Color = Clay.Sun, textColor: Color = Clay.Ink, icon: ImageVector? = null, compact: Boolean = false) {
  ClaySlab(slabColorFor(color), modifier, SlabSize.SMALL) {
    Row(Modifier.heightIn(min = if (compact) 30.dp else 36.dp).padding(horizontal = if (compact) 10.dp else 14.dp, vertical = if (compact) 2.dp else 6.dp), verticalAlignment = Alignment.CenterVertically) {
      if (icon != null) {
        Icon(icon, contentDescription = null, tint = textColor, modifier = Modifier.size(16.dp))
        Spacer(Modifier.width(4.dp))
      }
      Text(text, color = textColor, fontSize = 14.sp, fontWeight = FontWeight.Bold)
    }
  }
}
