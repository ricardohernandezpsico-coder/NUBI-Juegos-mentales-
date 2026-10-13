package com.example.ui.components

import android.content.Context
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.snap
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Image
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.FilterQuality
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.imageResource
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.IntSize
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.R
import com.example.data.ClayKitSpec
import com.example.data.KitMargins
import com.example.data.KitPiece
import com.example.data.KitSlab
import com.example.ui.theme.Clay
import kotlin.math.roundToInt

/**
 * El interruptor del kit de plasticina (Tarea 89, prueba): solo existe en builds de DEPURACIÓN, encendido por defecto, y se cambia en Ajustes → «Kit plasticina (prueba)» (`DebugTools`). En la versión de tienda siempre está apagado.
 * Mientras esté encendido, los botones grandes de Hoy y de la lámina, las piedritas y la X de la lámina se dibujan con las losas de plasticina renderizadas en Blender; apagado, se ven como antes (arcilla dibujada con borde tinta y sombra dura).
 */
object ClayKitFlag {
  var on by mutableStateOf(false)
    private set

  private const val PREFS = "kit_plasticina"

  /** Lee el interruptor guardado (encendido si nunca se tocó) — solo en depuración. */
  fun init(context: Context, debug: Boolean) {
    on = debug && context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).getBoolean("on", true)
  }

  fun set(context: Context, value: Boolean) {
    context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().putBoolean("on", value).apply()
    on = value
  }

  /** Para las pruebas: lo fija sin tocar las preferencias. */
  internal fun forTest(value: Boolean) { on = value }
}

/** Qué losa: el color (sol, crema o lima) y sus dos estados (normal y presionada). */
enum class SlabColor(@androidx.annotation.DrawableRes val normal: Int, @androidx.annotation.DrawableRes val pressed: Int) {
  SUN(R.drawable.kit_losa_sol, R.drawable.kit_losa_sol_p),
  CREAM(R.drawable.kit_losa_crema, R.drawable.kit_losa_crema_p),
  LIME(R.drawable.kit_losa_lima, R.drawable.kit_losa_lima_p)
}

@Composable
internal fun rememberClayKitSpec(): ClayKitSpec {
  val context = LocalContext.current
  return remember { ClayKitSpec.parse(context.resources.openRawResource(R.raw.kit).bufferedReader().use { it.readText() }) }
}

/** Un trozo del nine-slice: de dónde sale en la imagen y a dónde va en la pantalla (todo en píxeles). */
internal data class NinePatch(val srcX: Int, val srcY: Int, val srcW: Int, val srcH: Int, val dstX: Int, val dstY: Int, val dstW: Int, val dstH: Int)

/**
 * Los nueve trozos de una losa (Tarea 89): las cuatro esquinas (con su sombra) a su tamaño, los cuatro bordes estirados en un solo eje y el centro, parejo, estirado en los dos. [body] es el cuerpo de la losa en la pantalla (px) y [density] los px por dp;
 * la imagen se dibuja alrededor del cuerpo con los márgenes del JSON (ahí cae la sombra de contacto). Si el cuerpo es más chico que dos esquinas, las esquinas se achican parejas. Pura (se prueba sin pantalla).
 */
internal fun nineSlice(slab: KitSlab, pxPerDp: Float, density: Float, body: androidx.compose.ui.geometry.Rect): List<NinePatch> {
  val s = density / pxPerDp                                    // píxeles de pantalla por píxel de la imagen
  val m = slab.margins
  val left = body.left - m.left * density; val top = body.top - m.top * density; val right = body.right + m.right * density; val bottom = body.bottom + m.bottom * density
  var cl = slab.insetLeftPx * s; var cr = slab.insetRightPx * s; var ct = slab.insetTopPx * s; var cb = slab.insetBottomPx * s
  val w = right - left; val h = bottom - top
  if (cl + cr > w) { val k = w / (cl + cr); cl *= k; cr *= k }
  if (ct + cb > h) { val k = h / (ct + cb); ct *= k; cb *= k }
  val xs = intArrayOf(left.roundToInt(), (left + cl).roundToInt(), (right - cr).roundToInt(), right.roundToInt())
  val ys = intArrayOf(top.roundToInt(), (top + ct).roundToInt(), (bottom - cb).roundToInt(), bottom.roundToInt())
  val sxs = intArrayOf(0, slab.insetLeftPx, slab.widthPx - slab.insetRightPx, slab.widthPx)
  val sys = intArrayOf(0, slab.insetTopPx, slab.heightPx - slab.insetBottomPx, slab.heightPx)
  val out = ArrayList<NinePatch>(9)
  for (r in 0 until 3) for (c in 0 until 3) {
    val dw = xs[c + 1] - xs[c]; val dh = ys[r + 1] - ys[r]
    if (dw > 0 && dh > 0) out += NinePatch(sxs[c], sys[r], sxs[c + 1] - sxs[c], sys[r + 1] - sys[r], xs[c], ys[r], dw, dh)
  }
  return out
}

/** Dibuja la losa [image] como nine-slice sobre [body] (px). */
internal fun DrawScope.drawNineSlice(image: ImageBitmap, slab: KitSlab, pxPerDp: Float, body: androidx.compose.ui.geometry.Rect, alpha: Float = 1f) {
  for (p in nineSlice(slab, pxPerDp, density, body)) {
    drawImage(image, srcOffset = IntOffset(p.srcX, p.srcY), srcSize = IntSize(p.srcW, p.srcH), dstOffset = IntOffset(p.dstX, p.dstY), dstSize = IntSize(p.dstW, p.dstH), alpha = alpha, filterQuality = FilterQuality.High)
  }
}

/** Dibuja una pieza suelta (la X de cerrar, el triángulo de «Jugar») entera, con su cuerpo en [body] y los márgenes alrededor. */
internal fun DrawScope.drawKitPiece(image: ImageBitmap, piece: KitPiece, body: androidx.compose.ui.geometry.Rect, alpha: Float = 1f) {
  val d = density; val m = piece.margins
  drawImage(
    image, dstOffset = IntOffset((body.left - m.left * d).roundToInt(), (body.top - m.top * d).roundToInt()),
    dstSize = IntSize(((piece.widthPx / 3f) * d).roundToInt(), ((piece.heightPx / 3f) * d).roundToInt()), alpha = alpha, filterQuality = FilterQuality.High
  )
}

/**
 * La losa de plasticina como contenedor (Tarea 89): dibuja el nine-slice del [color] detrás de su contenido (el texto de Compose NO va horneado) y, si hay [onClick], al apoyar el dedo cambia a la imagen presionada en ~80 ms (con «quitar animaciones», de golpe)
 * y el contenido baja lo que dice el JSON. Mide lo que mida su contenido: las esquinas quedan fijas y solo se estira el centro, así que con letra grande crece sin deformarse. Reserva 6 dp abajo para el arranque de la sombra (el resto de la sombra es blanda y sale del cuerpo).
 */
@Composable
internal fun ClaySlab(
  color: SlabColor,
  modifier: Modifier = Modifier,
  onClick: (() -> Unit)? = null,
  role: Role? = Role.Button,
  /** Solo para comparaciones y pruebas: la dibuja presionada aunque nadie la toque. */
  forcePressed: Boolean = false,
  content: @Composable BoxScope.() -> Unit
) {
  val spec = rememberClayKitSpec()
  val normal = ImageBitmap.imageResource(color.normal)
  val pressedImg = ImageBitmap.imageResource(color.pressed)
  val source = remember { MutableInteractionSource() }
  val isPressed by source.collectIsPressedAsState()
  val reduce = rememberReduceMotion()
  val p by animateFloatAsState(if ((isPressed && onClick != null) || forcePressed) 1f else 0f, if (reduce) snap() else tween(PRESS_MS), label = "losa")
  val click = if (onClick != null) Modifier.clickable(interactionSource = source, indication = null, role = role, onClick = onClick) else Modifier
  // El contenido define el tamaño; las imágenes se dibujan en una capa que llena al padre (así la losa nunca se agranda por su cuenta) y el toque va en el contenedor, sin contar los 6 dp de abajo.
  Box(modifier.padding(bottom = 6.dp).then(click)) {
    Box(
      Modifier.matchParentSize().drawBehind {
        val body = androidx.compose.ui.geometry.Rect(Offset.Zero, Size(size.width, size.height))
        if (p < 1f) drawNineSlice(normal, spec.slab, spec.pxPerDp, body, alpha = 1f - p)
        if (p > 0f) drawNineSlice(pressedImg, spec.slab, spec.pxPerDp, body, alpha = p)
      }
    )
    Box(Modifier.offset(y = (spec.slab.pressTextDy * p).dp)) { content() }
  }
}

/**
 * La tarjeta que usan las piedritas (y la ruta completa) de Hoy: con el kit APAGADO es la `ClayCard` de siempre (arcilla dibujada); con el kit ENCENDIDO es una losa de plasticina del color que corresponda (lima, sol o crema).
 * El contenido es el mismo: así se compara antes y después con exactamente lo mismo dentro.
 */
@Composable
internal fun ClayKitCard(
  modifier: Modifier = Modifier,
  color: androidx.compose.ui.graphics.Color,
  radius: androidx.compose.ui.unit.Dp,
  depth: androidx.compose.ui.unit.Dp,
  onClick: (() -> Unit)? = null,
  content: @Composable ColumnScope.() -> Unit
) {
  if (ClayKitFlag.on) {
    val slab = when (color) { Clay.Lime -> SlabColor.LIME; Clay.Sun -> SlabColor.SUN; else -> SlabColor.CREAM }
    ClaySlab(slab, modifier, onClick = onClick) { Column(content = content) }
  } else {
    com.example.ui.theme.ClayCard(modifier = modifier, color = color, radius = radius, depth = depth, onClick = onClick, contentPadding = 0.dp, content = content)
  }
}

/** Lo que tarda la losa en pasar a presionada (y a volver). */
internal const val PRESS_MS = 80

/**
 * El botón grande de plasticina (Tarea 89): losa sol con texto de Compose en tinta (≥ 14 sp, 17 de base) y, si [play], el triángulo de «Jugar» como un churrito tinta antes del texto. 56 dp de alto como mínimo, no fijo: con letra 2,0 el texto pasa a dos líneas y la losa crece.
 */
@Composable
internal fun KitButton(text: String, onClick: () -> Unit, modifier: Modifier = Modifier, play: Boolean = false, fontSize: TextUnit = 17.sp, forcePressed: Boolean = false) {
  ClaySlab(SlabColor.SUN, modifier.fillMaxWidth(), onClick = onClick, forcePressed = forcePressed) {
    Row(
      Modifier.fillMaxWidth().heightIn(min = 56.dp).padding(horizontal = 12.dp, vertical = 8.dp),
      horizontalArrangement = Arrangement.Center, verticalAlignment = Alignment.CenterVertically
    ) {
      if (play) {
        KitPlayGlyph()
        Spacer(Modifier.width(10.dp))
      }
      Text(text, color = Clay.Ink, fontSize = fontSize, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center, modifier = Modifier.weight(1f, fill = false))
    }
  }
}

/** El triángulo de «Jugar» (churrito de plasticina tinta). Es decorativo: el botón ya dice qué hace. */
@Composable
internal fun KitPlayGlyph(modifier: Modifier = Modifier) {
  val spec = rememberClayKitSpec()
  val image = ImageBitmap.imageResource(R.drawable.kit_jugar)
  Box(
    modifier.size(spec.play.bodyWidth.dp, spec.play.bodyHeight.dp).drawBehind {
      drawKitPiece(image, spec.play, androidx.compose.ui.geometry.Rect(Offset.Zero, Size(size.width, size.height)))
    }
  )
}

/** La X de cerrar: un disco de plasticina crema con una X de dos churros tinta (la X va en la imagen). Toca 48 dp; al apoyar el dedo cambia al disco hundido. [label] = lo que lee el lector de pantalla. */
@Composable
internal fun KitCloseButton(onClose: () -> Unit, modifier: Modifier = Modifier, label: String = "Cerrar", forcePressed: Boolean = false) {
  val spec = rememberClayKitSpec()
  val normal = ImageBitmap.imageResource(R.drawable.kit_cerrar)
  val pressedImg = ImageBitmap.imageResource(R.drawable.kit_cerrar_p)
  val source = remember { MutableInteractionSource() }
  val isPressed by source.collectIsPressedAsState()
  val reduce = rememberReduceMotion()
  val p by animateFloatAsState(if (isPressed || forcePressed) 1f else 0f, if (reduce) snap() else tween(PRESS_MS), label = "cerrar")
  Box(
    modifier
      .size(spec.close.bodyWidth.dp, 48.dp)
      .drawBehind {
        val body = androidx.compose.ui.geometry.Rect(Offset.Zero, Size(size.width, size.height))
        if (p < 1f) drawKitPiece(normal, spec.close, body)
        if (p > 0f) drawKitPiece(pressedImg, spec.close, body, alpha = p)
      }
      .clickable(interactionSource = source, indication = null, role = Role.Button, onClickLabel = label, onClick = onClose)
      .semantics { contentDescription = label }
  )
}
