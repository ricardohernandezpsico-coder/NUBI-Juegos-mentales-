package com.example.ui.components

import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.Spring
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.PlayCircle
import androidx.compose.material.icons.outlined.Home
import androidx.compose.material.icons.outlined.PlayCircle
import androidx.compose.material3.Icon
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.scale
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.role
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.viewmodel.AppTab

private val BarColor = Color(0xFFFFFFFF)
private val Ink = Color(0xFF1A1240)
private val InkDim = Color(0xFF6B6790)
private val Accent = Color(0xFFFF6B4A)

/** Un destino de la barra; sin ícono de Material = el cerebro dibujado ([BrainIcon]). */
private data class NavItem(val tab: AppTab, val label: String, val on: ImageVector?, val off: ImageVector?)

// Aprobado por Ricardo (29-sep, `docs/previews/navegacion-nubi.png`): Hoy (casa), Juegos (un "player") y Avance (un
// cerebro). Sin el botón "play" del centro: el desafío del día ya se empieza desde Hoy.
private val items = listOf(
  NavItem(AppTab.HOY, "Hoy", Icons.Filled.Home, Icons.Outlined.Home),
  NavItem(AppTab.JUEGOS, "Juegos", Icons.Filled.PlayCircle, Icons.Outlined.PlayCircle),
  NavItem(AppTab.PROGRESO, "Avance", null, null)
)

/** Barra inferior flotante de Nubi: 3 destinos con ícono y etiqueta corta; el elegido se resalta con una pastilla. */
@Composable
fun NeuroNavBar(current: AppTab, onSelect: (AppTab) -> Unit, modifier: Modifier = Modifier) {
  Box(
    modifier = modifier
      .fillMaxWidth()
      .height(84.dp)
      .padding(horizontal = 14.dp)
      .testTag("bottom_nav_bar")
  ) {
    Surface(
      modifier = Modifier
        .align(Alignment.BottomCenter)
        .padding(bottom = 4.dp)
        .fillMaxWidth()
        .height(68.dp)
        .drawBehind {
          drawRoundRect(Ink, topLeft = Offset(0f, 4.dp.toPx()), size = size, cornerRadius = CornerRadius(30.dp.toPx()))
        },
      shape = RoundedCornerShape(30.dp),
      color = BarColor,
      border = BorderStroke(3.dp, Ink),
      shadowElevation = 0.dp
    ) {
      Row(modifier = Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        items.forEach { NavSlot(it, current == it.tab, { onSelect(it.tab) }, Modifier.weight(1f)) }
      }
    }
  }
}

@Composable
private fun NavSlot(item: NavItem, selected: Boolean, onClick: () -> Unit, modifier: Modifier = Modifier) {
  val tint by animateColorAsState(if (selected) Ink else InkDim, label = "navTint")
  val pill by animateColorAsState(if (selected) Accent.copy(alpha = 0.18f) else Color.Transparent, label = "navPill")
  val scale by animateFloatAsState(
    if (selected) 1.08f else 1f,
    spring(dampingRatio = Spring.DampingRatioMediumBouncy, stiffness = Spring.StiffnessMedium),
    label = "navScale"
  )
  Column(
    modifier = modifier
      .height(68.dp)
      .clickable(interactionSource = remember { MutableInteractionSource() }, indication = null, onClick = onClick)
      .semantics { role = Role.Tab; contentDescription = item.label }
      .testTag("nav_item_${item.tab.name.lowercase()}"),
    horizontalAlignment = Alignment.CenterHorizontally,
    verticalArrangement = Arrangement.Center
  ) {
    Box(
      modifier = Modifier
        .clip(RoundedCornerShape(16.dp))
        .background(pill)
        .padding(horizontal = 18.dp, vertical = 4.dp)
        .scale(scale),
      contentAlignment = Alignment.Center
    ) {
      val iconTint = if (selected) Accent else tint
      val icon = if (selected) item.on else item.off
      if (icon != null) Icon(icon, contentDescription = null, tint = iconTint, modifier = Modifier.size(26.dp))
      else BrainIcon(iconTint, 26.dp)
    }
    Text(item.label, fontSize = 13.sp, fontWeight = if (selected) FontWeight.Bold else FontWeight.Medium, color = tint)
  }
}
