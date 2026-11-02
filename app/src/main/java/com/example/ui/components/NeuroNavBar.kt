package com.example.ui.components

import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.snap
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.selection.selectable
import androidx.compose.material3.LocalTextStyle
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
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.rememberTextMeasurer
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.ui.theme.Clay
import com.example.ui.theme.NubiTokens
import com.example.viewmodel.AppTab

/**
 * La barra inferior del kit (Tarea 156, 21-oct; opción C de `auditoria-fable/spec-barra-inferior.md`, elegida por Ricardo; antes, desde la Tarea 86, una bandeja crema con tres cubos y objetos de la isla, `docs/diseno-isla.md` §4.4: quedó en historia).
 * Queja de Ricardo: «los íconos casi no se ven y su base parece un sándwich». Ahora es el lenguaje de la X, la flecha de volver y los íconos de Ajustes: sobre una franja del cielo un tono más claro ([BarTone]) con un filo arriba, DISCOS crema de 48 dp (`ClayDisc`) con un glifo
 * ícono de línea de tinta ([NavIcon], Tarea 158: la isla con su banderín de Hoy, un cronómetro de Contrarreloj —Tarea 215—, tres barras que suben de Avance y una persona de Tú; antes glifos de churro de 32 dp que «no se reconocían») y su rótulo de 16 sp debajo. La pestaña elegida se apoya en una losa SOL chica con el rótulo dentro,
 * en tinta y negrita: la elegida se ve por forma (la losa), por peso (la negrita) y por `selected` para el lector de pantalla; nunca solo por color.
 *
 * Cada pestaña es TODA su columna (todo el alto de la barra y, de ancho, el que dice [navColumnWidths]: ≥ 48 dp en los dos ejes), con `Role.Tab`; al apoyar el dedo el disco se hunde (su imagen presionada). La losa se funde en 150 ms ([PlaceMotion.TAB_FADE_MS]; con «quitar animaciones», de golpe).
 * Las pestañas son Hoy · Contrarreloj · Avance · Tú (Tarea 215; antes Hoy · Avance · Tú, Tarea 158; Juegos se fue). **Las columnas NO son parejas** (`auditoria-fable/idea-pestana-contrarreloj.md` §1.3): «Contrarreloj» en negrita mide 91 dp y «Tú», 19, así que cada columna mide el ancho de su rótulo EN NEGRITA más 12 dp de aire
 * y el sobrante se reparte parejo; como el ancho se mide siempre con la negrita, elegir otra pestaña no mueve nada (igual que `AreaChipGrid` con sus chips). La losa sol mide el ancho de su columna menos 8 dp. **Los rótulos no pasan de la letra 1,3** ([NAV_TEXT_SCALE_CAP]; con 2,0 los cuatro suman 434 dp y no caben en ningún teléfono):
 * es el único costo de accesibilidad de la pestaña nueva; los discos, los glifos y la losa son objetos y no cambian con la letra. Mide 88 dp (7 + 48 + 4 + 21 + 8) con letra normal y crece solo lo que crece el rótulo (95 dp con 1,3, y de ahí no sube). Va sobre la barra del teléfono: el que la pone (`safeDrawingPadding`) ya la deja encima de ella.
 *
 * La guía de Hoy (Tarea 193) ilumina las TRES columnas de la derecha (Contrarreloj, Avance y Tú; Hoy, donde ya está la persona, queda bajo el velo): las junta una fila que publica [GuideTarget.TAB_BAR].
 */
@Composable
fun NeuroNavBar(current: AppTab, onSelect: (AppTab) -> Unit, modifier: Modifier = Modifier) {
  val reduceMotion = rememberReduceMotion()
  val measurer = rememberTextMeasurer()
  val density = LocalDensity.current
  val base = LocalTextStyle.current
  val capped = Density(density.density, minOf(density.fontScale, NAV_TEXT_SCALE_CAP))
  val style = base.merge(TextStyle(fontSize = 16.sp, fontWeight = FontWeight.Bold))
  BoxWithConstraints(
    modifier
      .fillMaxWidth()
      .testTag("bottom_nav_bar")
      .background(BarTone)
      .drawBehind { drawRect(EdgeTone, size = Size(size.width, EDGE_DP.dp.toPx())) }
  ) {
    // El ancho de cada rótulo EN NEGRITA y con la letra topada (el que usan todos los estados de la pestaña): así la columna no cambia al elegirla.
    val labels = TABS.map { with(density) { measurer.measure(it.label, style, softWrap = false, maxLines = 1, density = capped).size.width.toDp().value } }
    val widths = navColumnWidths(labels, maxWidth.value).map { it.dp }
    Row(Modifier.fillMaxWidth()) {
      NavItem(TABS[0], selected = TABS[0].tab == current, reduceMotion = reduceMotion, width = widths[0]) { onSelect(TABS[0].tab) }
      // la guía de Nubi en Hoy ilumina estas tres columnas: el rectángulo que publica esta fila es el hueco (de la columna de Contrarreloj al borde derecho, y de arriba a abajo de la barra)
      Row(Modifier.guideTarget(GuideTarget.TAB_BAR)) {
        for (i in 1 until TABS.size) NavItem(TABS[i], selected = TABS[i].tab == current, reduceMotion = reduceMotion, width = widths[i]) { onSelect(TABS[i].tab) }
      }
    }
  }
}

/** La letra de los rótulos de la barra no pasa de 1,3 veces la base: con 2,0 los cuatro rótulos en negrita suman 434 dp (con 1,3, 300) y no caben ni a 411 dp de ancho. */
const val NAV_TEXT_SCALE_CAP = 1.3f

/** Lo mínimo que toca una columna (48 dp) y el aire que lleva cada rótulo (12 dp en total) antes de repartir el sobrante. */
private const val MIN_COLUMN_DP = 48f
private const val LABEL_AIR_DP = 12f
/** Lo que la losa sol mide menos que su columna (4 dp de cada lado). */
private const val SLAB_INSET_DP = 8f

/**
 * El ancho de cada columna de la barra (Tarea 215; pura y probada en `NeuroNavBarTest`): lo que mide su rótulo EN NEGRITA ([labelsDp], en dp) más [LABEL_AIR_DP], nunca menos de [MIN_COLUMN_DP], y el sobrante de [totalDp] repartido parejo entre las columnas.
 * Si no hubiera ni para eso (no pasa con el tope de letra), se achican todas en la misma proporción.
 */
internal fun navColumnWidths(labelsDp: List<Float>, totalDp: Float): List<Float> {
  val need = labelsDp.map { maxOf(it + LABEL_AIR_DP, MIN_COLUMN_DP) }
  val spare = totalDp - need.sum()
  return if (spare >= 0f) need.map { it + spare / need.size } else need.map { it * totalDp / need.sum() }
}

/** El cielo un tono más claro que el de la página (`#1C1B52` abajo): la barra se separa por su tono y su filo, y la página termina sobre ella por `LocalBarInset`. */
private val BarTone = Color(0xFF29276A)
/** El filo de arriba: `OnNight` al 22 %. */
private val EdgeTone = NubiTokens.OnNight.copy(alpha = 0.22f)
private const val EDGE_DP = 2

@Composable
private fun NavItem(t: NavTab, selected: Boolean, reduceMotion: Boolean, width: Dp, onClick: () -> Unit) {
  val on by animateFloatAsState(if (selected) 1f else 0f, if (reduceMotion) snap() else tween(PlaceMotion.TAB_FADE_MS), label = "pestaña")
  val source = remember { MutableInteractionSource() }
  val pressed by source.collectIsPressedAsState()
  Box(
    Modifier
      .width(width)
      .selectable(selected = selected, interactionSource = source, indication = null, role = Role.Tab, onClick = onClick)
      .semantics { contentDescription = t.label }
      .testTag("nav_item_${t.tab.name.lowercase()}"),
    contentAlignment = Alignment.TopCenter
  ) {
    // La losa sol de la elegida, DETRÁS (no mide nada: la columna la mide lo de adelante); se funde con 150 ms. Mide el ancho de SU columna menos 8 dp (Tarea 215: antes fija en 96 dp).
    if (on > 0f) Box(Modifier.matchParentSize().graphicsLayer { alpha = on }.testTag("nav_slab_${t.tab.name.lowercase()}"), contentAlignment = Alignment.TopCenter) {
      ClaySlab(SlabColor.SUN, Modifier.width(width - SLAB_INSET_DP.dp).fillMaxHeight().padding(top = 3.dp).testTag("nav_slab_body_${t.tab.name.lowercase()}"), size = SlabSize.SMALL, onClick = null, role = null) {}
    }
    Column(Modifier.padding(top = 7.dp, bottom = 8.dp), horizontalAlignment = Alignment.CenterHorizontally) {
      ClayDisc(DiscStyle.CREAM, onClick = null, label = t.label, forcePressed = pressed) { NavIconImage(t.icon) }
      Spacer(Modifier.height(4.dp))
      CapScale(NAV_TEXT_SCALE_CAP) {
        Text(
          t.label, color = lerp(NubiTokens.OnNightDim, Clay.Ink, on), fontSize = 16.sp, lineHeight = 21.sp,
          fontWeight = if (selected) FontWeight.Bold else FontWeight.Medium, textAlign = TextAlign.Center, softWrap = false
        )
      }
    }
  }
}

private data class NavTab(val tab: AppTab, val label: String, val icon: NavIcon)

private val TABS = listOf(
  NavTab(AppTab.HOY, "Hoy", NavIcon.ISLA),
  NavTab(AppTab.CONTRARRELOJ, "Contrarreloj", NavIcon.CRONOMETRO),
  NavTab(AppTab.PROGRESO, "Avance", NavIcon.BARRAS),
  NavTab(AppTab.TU, "Tú", NavIcon.PERSONA)
)
