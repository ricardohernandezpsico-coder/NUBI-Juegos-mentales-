package com.example

import android.graphics.BitmapFactory
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.snap
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.ui.draw.clip
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.FilterQuality
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.graphics.vector.PathParser
import androidx.compose.ui.graphics.vector.rememberVectorPainter
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.ui.components.ClayDisc
import com.example.ui.components.ClaySlab
import com.example.ui.components.DiscStyle
import com.example.ui.components.Glyph
import com.example.ui.components.KitGlyph
import com.example.ui.components.PlaceMotion
import com.example.ui.components.SlabColor
import com.example.ui.components.SlabSize
import com.example.ui.components.rememberReduceMotion
import com.example.ui.theme.Clay
import com.example.ui.theme.NubiTokens
import com.example.viewmodel.AppTab

/*
 * VISTA PREVIA de la Tarea 157 («Íconos de la barra que se reconozcan», 21-oct): solo para la lámina `docs/previews/tarea-157-iconos.png`. Nada de esto está en la app: la barra real (`NeuroNavBar`) queda como estaba.
 * La queja de Ricardo sobre la barra de la Tarea 156: «no se reconoce el cohete ni la farola; se ve algo gris oscuro, con una forma poco delimitada». Aquí se maquetan, con la MISMA barra (tercios, disco de 48 dp, rótulo de 16 sp, losa sol de la elegida), tres estilos de ícono con las mismas metáforas
 * (isla con banderita, cohete, farola): E1 los objetos de plasticina a color (los de la barra de antes de la Tarea 156, renderizados sueltos y a su tamaño con `tools/isla/isla.py --barra-objetos`), E2 línea fina de tinta y E3 dos tonos (contorno de tinta y relleno de color del kit).
 * Los vectores se dibujan en una rejilla de 24 unidades con las mismas rutas para E2 y E3, y se pasan a Compose con `PathParser` (si un estilo se adopta, esas rutas son el `ImageVector` de la app).
 */

/** Los estilos que se comparan: el de hoy (la barra de la Tarea 156) y los tres de la Tarea 157 (E1 con y sin disco, para medir cuál contrasta mejor). */
internal enum class NavStyle(val key: String) {
  ACTUAL("actual"), OBJETOS_SIN_DISCO("e1-sin-disco"), OBJETOS_CON_DISCO("e1-disco"),
  /** E1 sobre una ventanilla noche (regla R4 del kit: lo calibrado sobre oscuro va en su ventanilla noche): la salida que encontré para que los objetos de color se vean también sobre la losa sol de la elegida. */
  OBJETOS_NOCHE("e1-noche"), LINEA("e2-linea"), DOS_TONOS("e3-dos-tonos")
}

/**
 * Un trazo de un ícono vectorial, en la rejilla de 24 unidades. [d] es la ruta (M L H V C Q Z absolutos); [fill] el relleno del color del kit en E3; [solid] = en E2 se rellena de tinta (puntos chicos: ventanilla, banderita, llama);
 * [lineD] = otra ruta para E2 (la misma forma pero ABIERTA, para que el hueco interior no se cierre); [e3Only] = solo existe en E3.
 */
internal data class IconShape(val d: String, val fill: Color? = null, val solid: Boolean = false, val lineD: String? = null)

private fun circle(cx: Float, cy: Float, r: Float): String {
  val k = 0.5523f * r
  return "M${cx + r} $cy C${cx + r} ${cy + k} ${cx + k} ${cy + r} $cx ${cy + r} C${cx - k} ${cy + r} ${cx - r} ${cy + k} ${cx - r} $cy C${cx - r} ${cy - k} ${cx - k} ${cy - r} $cx ${cy - r} C${cx + k} ${cy - r} ${cx + r} ${cy - k} ${cx + r} $cy Z"
}

private fun rrect(x: Float, y: Float, w: Float, h: Float, r: Float): String =
  "M${x + r} $y L${x + w - r} $y Q${x + w} $y ${x + w} ${y + r} L${x + w} ${y + h - r} Q${x + w} ${y + h} ${x + w - r} ${y + h} L${x + r} ${y + h} Q$x ${y + h} $x ${y + h - r} L$x ${y + r} Q$x $y ${x + r} $y Z"

/** Los íconos: los tres de las pestañas de hoy (isla con banderita, cohete, farola) y las metáforas que se proponen para Juegos (cuadrícula de fichas, mando) y para Avance (barras que suben, brote). */
internal enum class NavIcono(val key: String, val shapes: List<IconShape>) {
  ISLA(
    "isla", listOf(
      IconShape("M3 16.4 C3 11.6 7.4 8.4 12 8.4 C16.6 8.4 21 11.6 21 16.4 Z", fill = Clay.Lime, lineD = "M3 16.4 C3 11.6 7.4 8.4 12 8.4 C16.6 8.4 21 11.6 21 16.4"),
      IconShape("M12 8.8 L12 2.6"),
      IconShape("M12 2.6 L19 5.2 L12 7.8 Z", fill = Clay.Coral, solid = true),
      IconShape("M2.6 20.6 C5.2 18.8 8 18.8 10.6 20.6 C13.2 22.4 16 22.4 18.6 20.6 L21.4 19")
    )
  ),
  COHETE(
    "cohete", listOf(
      IconShape("M7.6 12.4 L3.8 16.8 L3.8 20.2 L7.6 17.6 Z", fill = Clay.Coral),
      IconShape("M16.4 12.4 L20.2 16.8 L20.2 20.2 L16.4 17.6 Z", fill = Clay.Coral),
      IconShape("M12 2.2 C15.6 4.6 17 8.4 17 12.8 L17 17.8 L7 17.8 L7 12.8 C7 8.4 8.4 4.6 12 2.2 Z", fill = Clay.Grape),
      IconShape(circle(12f, 10.6f, 1.55f), fill = Clay.Sky, solid = true),
      IconShape("M10 20.4 L12 23.2 L14 20.4 Z", fill = Clay.Sun, solid = true)
    )
  ),
  FAROLA(
    "farola", listOf(
      IconShape("M8.2 21.8 L15.8 21.8"),
      IconShape("M12 21.8 L12 15"),
      IconShape("M8.6 6.4 L15.4 6.4 L14.4 15 L9.6 15 Z", fill = Clay.Sun),
      IconShape("M6.8 6.4 C6.8 4 9 2.4 12 2.4 C15 2.4 17.2 4 17.2 6.4 Z", fill = Clay.Grape)
    )
  ),
  FICHAS(
    "fichas", listOf(
      IconShape(rrect(1.5f, 1.5f, 9f, 9f, 2.6f), fill = Clay.Sun), IconShape(rrect(12.5f, 1.5f, 9f, 9f, 2.6f), fill = Clay.Coral),
      IconShape(rrect(1.5f, 12.5f, 9f, 9f, 2.6f), fill = Clay.Sky), IconShape(rrect(12.5f, 12.5f, 9f, 9f, 2.6f), fill = Clay.Lime)
    )
  ),
  MANDO(
    "mando", listOf(
      IconShape("M7.2 6.6 L16.8 6.6 C20 6.6 21.8 10 22.2 14.4 C22.4 17.6 21 19.2 19 19.2 C17.2 19.2 16.4 18 15.4 16.4 L8.6 16.4 C7.6 18 6.8 19.2 5 19.2 C3 19.2 1.6 17.6 1.8 14.4 C2.2 10 4 6.6 7.2 6.6 Z", fill = Clay.Grape),
      IconShape(circle(7.4f, 11.4f, 1.7f), solid = true), IconShape(circle(15.6f, 10.6f, 1.05f), solid = true), IconShape(circle(18.2f, 13.2f, 1.05f), solid = true)
    )
  ),
  BARRAS(
    "barras", listOf(
      IconShape(rrect(1.5f, 14f, 6f, 8f, 1.8f), fill = Clay.Sun), IconShape(rrect(9f, 9f, 6f, 13f, 1.8f), fill = Clay.Lime), IconShape(rrect(16.5f, 3f, 6f, 19f, 1.8f), fill = Clay.Coral)
    )
  ),
  BROTE(
    "brote", listOf(
      IconShape("M12 21.6 L12 11.8"),
      IconShape("M12 13.6 C12 8.8 8.2 5.6 2.4 6 C2.4 10.8 6.4 13.6 12 13.6 Z", fill = Clay.Lime),
      IconShape("M12 11.6 C12 6.4 15.8 3 21.6 3.4 C21.6 8.6 17.6 11.6 12 11.6 Z", fill = Clay.Lime),
      IconShape("M7.6 21.8 L16.4 21.8")
    )
  );

  companion object {
    /** La caja del vector (dp) para que el dibujo ENTERO se vea de 28 dp o más (E2: caja de 32 dp y trazo de 2,5 dp; E3: caja de 34 dp y contorno de 2 dp). */
    const val BOX_E2 = 32f
    const val STROKE_E2 = 2.5f
    const val BOX_E3 = 34f
    const val STROKE_E3 = 2f
  }
}

/** El ícono como `ImageVector` en el estilo pedido (solo E2 y E3). */
internal fun NavIcono.vector(style: NavStyle): ImageVector {
  val dos = style == NavStyle.DOS_TONOS
  val box = if (dos) NavIcono.BOX_E3 else NavIcono.BOX_E2
  val stroke = (if (dos) NavIcono.STROKE_E3 else NavIcono.STROKE_E2) * 24f / box        // el trazo en unidades de la rejilla
  val b = ImageVector.Builder("nav_${key}_${style.key}", box.dp, box.dp, 24f, 24f)
  for (s in shapes) {
    val d = if (!dos && s.lineD != null) s.lineD else s.d
    val fill = if (dos) (s.fill ?: if (s.solid) Clay.Ink else null) else if (s.solid) Clay.Ink else null
    b.addPath(
      PathParser().parsePathString(d).toNodes(), fill = fill?.let { SolidColor(it) }, stroke = SolidColor(Clay.Ink),
      strokeLineWidth = stroke, strokeLineCap = StrokeCap.Round, strokeLineJoin = StrokeJoin.Round
    )
  }
  return b.build()
}

/** Lo que mide el dibujo ENTERO (con su trazo), en dp, en el estilo pedido: para comprobar que se ve de 28 dp o más. */
internal fun NavIcono.visibleDp(style: NavStyle): Pair<Float, Float> {
  val dos = style == NavStyle.DOS_TONOS
  val box = if (dos) NavIcono.BOX_E3 else NavIcono.BOX_E2
  val strokeDp = if (dos) NavIcono.STROKE_E3 else NavIcono.STROKE_E2
  var r: Rect? = null
  for (s in shapes) {
    val d = if (!dos && s.lineD != null) s.lineD else s.d
    val b = PathParser().parsePathString(d).toPath().getBounds()
    r = r?.let { Rect(minOf(it.left, b.left), minOf(it.top, b.top), maxOf(it.right, b.right), maxOf(it.bottom, b.bottom)) } ?: b
  }
  val k = box / 24f
  return (r!!.width * k + strokeDp) to (r.height * k + strokeDp)
}

private fun objeto(nombre: String): ImageBitmap =
  BitmapFactory.decodeStream(NavIcono::class.java.classLoader!!.getResourceAsStream("nav_iconos/$nombre.png")).asImageBitmap()

/** Una pestaña de la maqueta: lo que dibuja la barra real, con el ícono del estilo elegido. */
private data class PreviewTab(val tab: AppTab, val label: String, val glyph: Glyph, val icono: NavIcono, val objeto: String)

/** (perezoso: las entradas de [NavIcono] usan funciones de este archivo, y crear la lista al cargar el archivo las encontraría todavía sin armar) */
private val PREVIEW_TABS by lazy {
  listOf(
    PreviewTab(AppTab.HOY, "Hoy", Glyph.ISLA, NavIcono.ISLA, "obj_hoy"),
    PreviewTab(AppTab.JUEGOS, "Juegos", Glyph.COHETE, NavIcono.COHETE, "obj_juegos"),
    PreviewTab(AppTab.PROGRESO, "Avance", Glyph.FAROLA, NavIcono.FAROLA, "obj_avance")
  )
}

private val BarTone = Color(0xFF29276A)
/** La ventanilla noche: el tono más oscuro del cielo de la app (`#07071F`/`#100F3A`/`#1C1B52`). */
private val NocheTone = Color(0xFF100F3A)
private val EdgeTone = NubiTokens.OnNight.copy(alpha = 0.22f)

/** La barra de la lámina: COPIA exacta de la disposición de `NeuroNavBar` (tercios, losa sol de la elegida, disco de 48 dp, rótulo de 16 sp), con el ícono del [style]. */
@Composable
internal fun PreviewNavBar(style: NavStyle, current: AppTab, modifier: Modifier = Modifier) {
  Row(
    modifier
      .fillMaxWidth()
      .testTag("bottom_nav_bar")
      .background(BarTone)
      .drawBehind { drawRect(EdgeTone, size = Size(size.width, 2.dp.toPx())) }
  ) {
    for (t in PREVIEW_TABS) PreviewNavItem(style, t.label, t.glyph, t.icono, t.objeto, selected = t.tab == current, modifier = Modifier.weight(1f).testTag("nav_item_${t.tab.name.lowercase()}"))
  }
}

/** Una pestaña suelta (la columna de un tercio): sirve también para mostrar las metáforas nuevas (cuadrícula, mando, barras, brote) con el mismo marco. */
@Composable
internal fun PreviewNavItem(style: NavStyle, label: String, glyph: Glyph, icono: NavIcono, objeto: String, selected: Boolean, modifier: Modifier = Modifier) {
  val reduceMotion = rememberReduceMotion()
  val on by animateFloatAsState(if (selected) 1f else 0f, if (reduceMotion) snap() else tween(PlaceMotion.TAB_FADE_MS), label = "pestaña")
  Box(modifier, contentAlignment = Alignment.TopCenter) {
    if (on > 0f) Box(Modifier.matchParentSize().graphicsLayer { alpha = on }, contentAlignment = Alignment.TopCenter) {
      ClaySlab(SlabColor.SUN, Modifier.width(96.dp).fillMaxHeight().padding(top = 3.dp), size = SlabSize.SMALL, onClick = null, role = null) {}
    }
    Column(Modifier.padding(top = 7.dp, bottom = 8.dp), horizontalAlignment = Alignment.CenterHorizontally) {
      when (style) {
        NavStyle.ACTUAL -> ClayDisc(DiscStyle.CREAM, onClick = null, label = label) { KitGlyph(glyph, size = 32.dp) }
        NavStyle.OBJETOS_SIN_DISCO -> Box(Modifier.size(48.dp), contentAlignment = Alignment.Center) { ObjetoImagen(objeto, selected) }
        NavStyle.OBJETOS_CON_DISCO -> ClayDisc(DiscStyle.CREAM, onClick = null, label = label) { ObjetoImagen(objeto, selected) }
        NavStyle.OBJETOS_NOCHE -> Box(Modifier.size(48.dp).clip(CircleShape).background(NocheTone), contentAlignment = Alignment.Center) { ObjetoImagen(objeto, selected) }
        NavStyle.LINEA, NavStyle.DOS_TONOS -> ClayDisc(DiscStyle.CREAM, onClick = null, label = label) {
          Image(rememberVectorPainter(remember(icono, style) { icono.vector(style) }), contentDescription = null)
        }
      }
      Spacer(Modifier.height(4.dp))
      Text(
        label, color = lerp(NubiTokens.OnNightDim, Clay.Ink, on), fontSize = 16.sp, lineHeight = 21.sp,
        fontWeight = if (selected) FontWeight.Bold else FontWeight.Medium, textAlign = TextAlign.Center, softWrap = false
      )
    }
  }
}

/** El objeto de plasticina renderizado a 3 px por dp en un cuadro de 48 dp (nada se amplía: con 2,75 px por dp se reduce un poco). Elegida = encendido (ventanilla, lucero, llama). */
@Composable
private fun ObjetoImagen(nombre: String, encendido: Boolean) {
  val bmp = remember(nombre, encendido) { objeto(if (encendido) nombre + "_on" else nombre) }
  Image(bmp, contentDescription = null, modifier = Modifier.size(48.dp), filterQuality = FilterQuality.High)
}
