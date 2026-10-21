package com.example.ui.components

import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.snap
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.selection.selectable
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.ui.theme.Clay
import com.example.ui.theme.NubiTokens
import com.example.viewmodel.AppTab

/**
 * La barra inferior del kit (Tarea 156, 21-oct; opción C de `auditoria-fable/spec-barra-inferior.md`, elegida por Ricardo; antes, desde la Tarea 86, una bandeja crema con tres cubos y objetos de la isla, `docs/diseno-isla.md` §4.4: quedó en historia).
 * Queja de Ricardo: «los íconos casi no se ven y su base parece un sándwich». Ahora es el lenguaje de la X, la flecha de volver y los íconos de Ajustes: sobre una franja del cielo un tono más claro ([BarTone]) con un filo arriba, tres DISCOS crema de 48 dp (`ClayDisc`) con un glifo
 * de churro tinta de 32 dp (la isla de Hoy, el cohete de Juegos, la farola de Avance) y su rótulo de 16 sp debajo. La pestaña elegida se apoya en una losa SOL chica con el rótulo dentro, en tinta y negrita: la elegida se ve por forma (la losa), por peso (la negrita) y por `selected`
 * para el lector de pantalla; nunca solo por color.
 *
 * Cada pestaña es TODA su columna (un tercio del ancho × todo el alto de la barra: ≥ 48 dp en los dos ejes), con `Role.Tab`; al apoyar el dedo el disco se hunde (su imagen presionada). La losa se funde en 150 ms ([PlaceMotion.TAB_FADE_MS]; con «quitar animaciones», de golpe).
 * Mide 89 dp (8 + 48 + 4 + 21 + 8) con letra normal y crece solo lo que crece el rótulo (95 dp con letra 1,3): los discos, glifos y la losa son objetos, no cambian con la letra. Va sobre la barra del teléfono: el que la pone (`safeDrawingPadding`) ya la deja encima de ella.
 */
@Composable
fun NeuroNavBar(current: AppTab, onSelect: (AppTab) -> Unit, modifier: Modifier = Modifier) {
  val reduceMotion = rememberReduceMotion()
  Row(
    modifier
      .fillMaxWidth()
      .testTag("bottom_nav_bar")
      .background(BarTone)
      .drawBehind { drawRect(EdgeTone, size = Size(size.width, EDGE_DP.dp.toPx())) }
  ) {
    for (t in TABS) NavItem(t, selected = t.tab == current, reduceMotion = reduceMotion, modifier = Modifier.weight(1f)) { onSelect(t.tab) }
  }
}

/** El cielo un tono más claro que el de la página (`#1C1B52` abajo): la barra se separa por su tono y su filo, y la página termina sobre ella por `LocalBarInset`. */
private val BarTone = Color(0xFF29276A)
/** El filo de arriba: `OnNight` al 22 %. */
private val EdgeTone = NubiTokens.OnNight.copy(alpha = 0.22f)
private const val EDGE_DP = 2
/** El cuerpo de la losa sol de la elegida (ancho; el alto lo da la columna). */
private const val SLAB_WIDTH_DP = 96
/** El glifo de churro tinta dentro del disco de 48 dp: 32 dp (la flecha de volver usa 28; en la barra el ícono es lo primero que se reconoce a distancia de teléfono, así que va un poco más grande). */
private const val GLYPH_DP = 32

@Composable
private fun NavItem(t: NavTab, selected: Boolean, reduceMotion: Boolean, modifier: Modifier, onClick: () -> Unit) {
  val on by animateFloatAsState(if (selected) 1f else 0f, if (reduceMotion) snap() else tween(PlaceMotion.TAB_FADE_MS), label = "pestaña")
  val source = remember { MutableInteractionSource() }
  val pressed by source.collectIsPressedAsState()
  Box(
    modifier
      .selectable(selected = selected, interactionSource = source, indication = null, role = Role.Tab, onClick = onClick)
      .semantics { contentDescription = t.label }
      .testTag("nav_item_${t.tab.name.lowercase()}"),
    contentAlignment = Alignment.TopCenter
  ) {
    // La losa sol de la elegida, DETRÁS (no mide nada: la columna la mide lo de adelante); se funde con 150 ms
    if (on > 0f) Box(Modifier.matchParentSize().graphicsLayer { alpha = on }.testTag("nav_slab_${t.tab.name.lowercase()}"), contentAlignment = Alignment.TopCenter) {
      ClaySlab(SlabColor.SUN, Modifier.width(SLAB_WIDTH_DP.dp).fillMaxHeight().padding(top = 3.dp), size = SlabSize.SMALL, onClick = null, role = null) {}
    }
    Column(Modifier.padding(top = 7.dp, bottom = 8.dp), horizontalAlignment = Alignment.CenterHorizontally) {
      ClayDisc(DiscStyle.CREAM, onClick = null, label = t.label, forcePressed = pressed) { KitGlyph(t.glyph, size = GLYPH_DP.dp) }
      Spacer(Modifier.height(4.dp))
      Text(
        t.label, color = lerp(NubiTokens.OnNightDim, Clay.Ink, on), fontSize = 16.sp, lineHeight = 21.sp,
        fontWeight = if (selected) FontWeight.Bold else FontWeight.Medium, textAlign = TextAlign.Center, softWrap = false
      )
    }
  }
}

private data class NavTab(val tab: AppTab, val label: String, val glyph: Glyph)

private val TABS = listOf(
  NavTab(AppTab.HOY, "Hoy", Glyph.ISLA),
  NavTab(AppTab.JUEGOS, "Juegos", Glyph.COHETE),
  NavTab(AppTab.PROGRESO, "Avance", Glyph.FAROLA)
)
