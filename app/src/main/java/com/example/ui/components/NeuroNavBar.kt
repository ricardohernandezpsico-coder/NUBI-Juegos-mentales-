package com.example.ui.components

import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.snap
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.Image
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.wrapContentSize
import androidx.compose.foundation.selection.selectable
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clipToBounds
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.withTransform
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.R
import com.example.viewmodel.AppTab

/**
 * La barra inferior de plasticina (Tarea 86; aprobada por Ricardo el 11-oct, `docs/diseno-isla.md` §4.4): una bandeja crema con tres cubos blandos, cada uno con su objeto: la isla en miniatura (Hoy), un cohete (Juegos) y el farol de la ruta (Avance).
 * Todo se renderizó en Blender con la luz de la isla (`tools/isla/`, `isla.py --barra`); aquí la bandeja es UNA imagen y cada cubo es una imagen aparte en dos estados (normal y elegido: hundido, con la lámpara encendida y un resplandor caramelo), así que se adapta a 360 y 411 dp
 * sin escalar un compuesto. La sombra de cada cubo sobre la bandeja es un óvalo suave pintado.
 *
 * Al cambiar de pestaña los dos estados de un cubo se funden en 150 ms (el hundimiento ya viene en el render; con «quitar animaciones» el cambio es inmediato). Debajo de cada cubo va su etiqueta (14 sp, negrita la elegida): las tres señales de «elegido» son la forma (hundido y encendido),
 * la negrita y `selected` para el lector de pantalla; ninguna es solo color. Cada pestaña es TODA su columna (cubo y etiqueta, 106 a 143 dp de ancho a 393), con `Role.Tab`.
 *
 * Mide ~92 dp la bandeja con los cubos y ~115 dp con las etiquetas a 393 dp de ancho (se escala con el ancho, entre 0,75 y 1,2); con letra grande crece lo que haga falta para que no se corte nada. Va sobre la barra del teléfono: el que la pone (`Scaffold` con `safeDrawingPadding`) ya la deja encima de ella.
 */
@Composable
fun NeuroNavBar(current: AppTab, onSelect: (AppTab) -> Unit, modifier: Modifier = Modifier) {
  val reduceMotion = rememberReduceMotion()
  BoxWithConstraints(modifier.fillMaxWidth().testTag("bottom_nav_bar")) {
    val scale = (maxWidth.value / BarraGeometry.WIDTH_DP).coerceIn(0.75f, 1.2f)
    val trayHeight = BarraGeometry.dp(BarraGeometry.BOTTOM - BarraGeometry.TOP, scale)
    Box(Modifier.fillMaxWidth()) {
      // La bandeja, los cubos y sus sombras, centrados y recortados a lo que se ve (el lienzo del render es mucho más alto).
      Box(Modifier.align(Alignment.TopCenter).width(BarraGeometry.dp(BarraGeometry.WIDTH_PX, scale)).height(trayHeight).clipToBounds()) {
        Image(
          painterResource(R.drawable.barra_bandeja), contentDescription = null,
          modifier = Modifier.offset(y = -BarraGeometry.dp(BarraGeometry.TOP, scale)).wrapContentSize(Alignment.TopStart, unbounded = true).size(BarraGeometry.dp(BarraGeometry.WIDTH_PX, scale), BarraGeometry.dp(BarraGeometry.HEIGHT_PX, scale)),
          contentScale = ContentScale.FillBounds
        )
        Canvas(Modifier.requiredSize(BarraGeometry.dp(BarraGeometry.WIDTH_PX, scale), trayHeight)) {
          val k = size.width / BarraGeometry.WIDTH_PX
          BarraGeometry.CUBES.forEach { c -> drawCubeShadow(c, selected = c.tab == current, k = k, topPx = BarraGeometry.TOP) }
        }
        BarraGeometry.CUBES.forEach { c -> BarraCube(c, selected = c.tab == current, scale = scale, reduceMotion = reduceMotion) }
      }
      // Las tres pestañas encima: cada una es toda su columna (cubo y etiqueta), sin dibujar nada. Las columnas se parten a medio camino entre cubo y cubo (143, 106 y 143 dp a 393), así el toque nunca queda a menos de 56 dp; la etiqueta va centrada bajo SU cubo.
      Row(Modifier.align(Alignment.TopCenter).width(BarraGeometry.dp(BarraGeometry.WIDTH_PX, scale))) {
        BarraGeometry.CUBES.forEachIndexed { i, c ->
          val selected = c.tab == current
          val left = if (i == 0) 0f else (BarraGeometry.CUBES[i - 1].x + c.x) / 2f
          val right = if (i == BarraGeometry.CUBES.lastIndex) BarraGeometry.WIDTH_PX else (c.x + BarraGeometry.CUBES[i + 1].x) / 2f
          Column(
            Modifier
              .width(BarraGeometry.dp(right - left, scale))
              .selectable(selected = selected, interactionSource = remember { MutableInteractionSource() }, indication = null, role = Role.Tab) { onSelect(c.tab) }
              .semantics { contentDescription = c.label }
              .testTag("nav_item_${c.tab.name.lowercase()}"),
            horizontalAlignment = Alignment.CenterHorizontally
          ) {
            Box(Modifier.height(trayHeight).fillMaxWidth())
            OnBoardText(
              c.label, color = if (selected) LabelOn else LabelOff, fontSize = 14.sp, lineHeight = 18.sp, fontWeight = if (selected) FontWeight.Bold else FontWeight.Medium, textAlign = TextAlign.Center, softWrap = false,
              modifier = Modifier.offset(x = BarraGeometry.dp(c.x - (left + right) / 2f, scale)).padding(top = 1.dp, bottom = 3.dp)
            )
          }
        }
      }
    }
  }
}

private val LabelOn = Color(0xFFFFFFFF)
private val LabelOff = Color(0xFFD6D1F0)

/** Un cubo: su imagen normal y su imagen elegida, una sobre otra, fundiéndose en 150 ms (de golpe con «quitar animaciones»). */
@Composable
private fun BarraCube(c: BarraGeometry.Cube, selected: Boolean, scale: Float, reduceMotion: Boolean) {
  val on by animateFloatAsState(if (selected) 1f else 0f, if (reduceMotion) snap() else tween(PlaceMotion.CUBE_FADE_MS), label = "cubo")
  @Composable
  fun Layer(res: Int, sizePx: Int, dyPx: Float, alpha: Float) {
    if (alpha <= 0f) return
    val side = BarraGeometry.dp(sizePx * BarraGeometry.CUBE_SCALE, scale)
    Image(
      painterResource(res), contentDescription = null,
      modifier = Modifier
        .offset(
          x = BarraGeometry.dp(c.x - sizePx * BarraGeometry.CUBE_SCALE / 2f, scale),
          y = BarraGeometry.dp(BarraGeometry.ANCHOR_Y + dyPx - sizePx * BarraGeometry.CUBE_SCALE / 2f - BarraGeometry.TOP, scale)
        )
        .size(side)
        .graphicsLayer { this.alpha = alpha },
      contentScale = ContentScale.FillBounds
    )
  }
  Layer(c.normal, c.normalPx, c.normalDy, 1f - on)
  Layer(c.chosen, c.chosenPx, c.chosenDy, on)
}

/** La sombra del cubo sobre la bandeja: un óvalo suave que cae a la derecha (la luz viene de la izquierda); la del cubo hundido es más chica y más pegada. */
private fun androidx.compose.ui.graphics.drawscope.DrawScope.drawCubeShadow(c: BarraGeometry.Cube, selected: Boolean, k: Float, topPx: Float) {
  val cx = (c.x + if (selected) 42f else 58f) * k
  val cy = ((if (selected) 238f else 233f) - topPx) * k
  val rx = (if (selected) 62f else 92f) * k
  val ry = (if (selected) 15f else 22f) * k
  val alpha = if (selected) 0.20f else 0.26f
  withTransform({ translate(cx, cy); scale(1f, ry / rx, Offset.Zero) }) {
    drawCircle(Brush.radialGradient(0f to ShadowTone.copy(alpha = alpha), 0.55f to ShadowTone.copy(alpha = alpha * 0.8f), 1f to Color.Transparent, center = Offset.Zero, radius = rx), radius = rx, center = Offset.Zero)
  }
}

private val ShadowTone = Color(0xFF281E14)

/**
 * Dónde va cada pieza, en píxeles del compuesto de la barra (786 × 420 px = 393 × 210 dp; 1 px = 0,5 dp). Salen de acomodar los renders `cas_*` sobre `bandeja` exactamente como el compuesto aprobado
 * (`tools/isla`): cada render de cubo se dibuja a 0,5 de su tamaño y su centro de render cae en [ANCHOR_Y]; la imagen recortada queda corrida lo que dice cada `…Dy` (el elegido va más abajo: es el hundimiento).
 */
internal object BarraGeometry {
  const val WIDTH_PX = 786f
  const val HEIGHT_PX = 420f
  const val WIDTH_DP = 393f
  /** Lo que se ve de la bandeja y los cubos: de la punta del cohete (y 100) a un pelo bajo la bandeja (y 281). */
  const val TOP = 98f
  const val BOTTOM = 283f
  const val ANCHOR_Y = 190f
  /** Las imágenes de los cubos son renders a 4 px por dp: se dibujan a la mitad de sus píxeles en el compuesto de 2 px por dp. */
  const val CUBE_SCALE = 0.5f

  /** Píxeles del compuesto → dp a la escala [scale] (1 px = 0,5 dp a 393 dp de ancho). */
  fun dp(px: Float, scale: Float): Dp = (px / 2f * scale).dp

  data class Cube(
    val tab: AppTab, val label: String, val x: Float,
    val normal: Int, val normalPx: Int, val normalDy: Float,
    val chosen: Int, val chosenPx: Int, val chosenDy: Float
  )

  val CUBES = listOf(
    Cube(AppTab.HOY, "Hoy", 181f, R.drawable.barra_hoy, 280, -21f, R.drawable.barra_hoy_on, 299, -7.75f),
    Cube(AppTab.JUEGOS, "Juegos", 393f, R.drawable.barra_juegos, 260, -16f, R.drawable.barra_juegos_on, 279, -2.75f),
    Cube(AppTab.PROGRESO, "Avance", 605f, R.drawable.barra_avance, 260, -16f, R.drawable.barra_avance_on, 279, -2.75f)
  )
}
