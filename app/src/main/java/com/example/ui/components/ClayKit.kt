package com.example.ui.components

import androidx.annotation.DrawableRes
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
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
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.IntSize
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.R
import com.example.data.ClayKitSpec
import com.example.data.KitPiece
import com.example.data.KitSlab
import com.example.ui.theme.Clay
import kotlin.math.roundToInt

/**
 * El kit de plasticina (Tareas 89 y 90): el estilo de TODA la app salvo los juegos de Unity. Son imágenes renderizadas en Blender (`tools/isla/isla.py --kit`, normalizadas por `tools/isla/kit.py`) que se dibujan detrás del texto y los íconos de Compose
 * (el texto nunca va horneado): losas de nine-slice (las esquinas fijas, el centro se estira) en tres radios, discos de 48 dp y íconos de churro tinta. Cada losa y cada disco tiene su imagen presionada (más baja, más oscura, con menos sombra).
 * Los componentes compartidos (`ClayCard`, `ClayButton`, `ClayPill`, los botones redondos) dibujan con esto por dentro.
 */

/** El radio de las esquinas de la losa según el tipo de pieza: chips y casillas chicas (14 dp), botones y tarjetas (22 dp), láminas y paneles grandes (30 dp). */
enum class SlabSize(val key: String) { SMALL("ch"), MEDIUM("md"), LARGE("gr") }

/** El color de la losa. Cada color está renderizado en los radios que se usan (ver [slabImages]). */
enum class SlabColor { SUN, CREAM, LIME, SKY, CORAL, LILAC }

/** Las dos imágenes (normal y presionada) de una losa y el tamaño de las que se encontraron. */
internal data class SlabImages(val size: SlabSize, @DrawableRes val normal: Int, @DrawableRes val pressed: Int)

/**
 * Las imágenes de la losa de [size] y [color]; si ese color no está renderizado en ese radio, las del radio mediano (nunca falla en pantalla; `ClayKitTest` avisa de qué combinaciones existen).
 */
internal fun slabImages(size: SlabSize, color: SlabColor): SlabImages = slabImagesOrNull(size, color) ?: slabImagesOrNull(SlabSize.MEDIUM, color)!!

internal fun slabImagesOrNull(size: SlabSize, color: SlabColor): SlabImages? = when (size) {
  SlabSize.SMALL -> when (color) {
    SlabColor.CREAM -> SlabImages(size, R.drawable.kit_losa_ch_crema, R.drawable.kit_losa_ch_crema_p)
    SlabColor.SUN -> SlabImages(size, R.drawable.kit_losa_ch_sol, R.drawable.kit_losa_ch_sol_p)
    SlabColor.LIME -> SlabImages(size, R.drawable.kit_losa_ch_lima, R.drawable.kit_losa_ch_lima_p)
    else -> null
  }
  SlabSize.MEDIUM -> when (color) {
    SlabColor.SUN -> SlabImages(size, R.drawable.kit_losa_md_sol, R.drawable.kit_losa_md_sol_p)
    SlabColor.CREAM -> SlabImages(size, R.drawable.kit_losa_md_crema, R.drawable.kit_losa_md_crema_p)
    SlabColor.LIME -> SlabImages(size, R.drawable.kit_losa_md_lima, R.drawable.kit_losa_md_lima_p)
    SlabColor.SKY -> SlabImages(size, R.drawable.kit_losa_md_cielo, R.drawable.kit_losa_md_cielo_p)
    SlabColor.CORAL -> SlabImages(size, R.drawable.kit_losa_md_coral, R.drawable.kit_losa_md_coral_p)
    SlabColor.LILAC -> SlabImages(size, R.drawable.kit_losa_md_lila, R.drawable.kit_losa_md_lila_p)
  }
  SlabSize.LARGE -> when (color) {
    SlabColor.CREAM -> SlabImages(size, R.drawable.kit_losa_gr_crema, R.drawable.kit_losa_gr_crema_p)
    else -> null
  }
}

/** El color de la paleta de la app → el de la losa más parecido (lo que usaban `ClayCard` y `ClayButton`); lo desconocido va en crema. */
internal fun slabColorFor(color: Color): SlabColor = when (color) {
  Clay.Sun -> SlabColor.SUN
  Clay.Lime -> SlabColor.LIME
  Clay.Sky -> SlabColor.SKY
  Clay.Coral -> SlabColor.CORAL
  Clay.Grape -> SlabColor.LILAC
  else -> SlabColor.CREAM
}

@Composable
internal fun rememberClayKitSpec(): ClayKitSpec {
  val context = LocalContext.current
  return remember { ClayKitSpec.parse(context.resources.openRawResource(R.raw.kit).bufferedReader().use { it.readText() }) }
}

/** Un trozo del nine-slice: de dónde sale en la imagen y a dónde va en la pantalla (todo en píxeles). */
internal data class NinePatch(val srcX: Int, val srcY: Int, val srcW: Int, val srcH: Int, val dstX: Int, val dstY: Int, val dstW: Int, val dstH: Int)

/**
 * Los nueve trozos de una losa: las cuatro esquinas (con su sombra) a su tamaño, los cuatro bordes estirados en un solo eje y el centro, parejo, estirado en los dos. [body] es el cuerpo de la losa en la pantalla (px) y [density] los px por dp;
 * la imagen se dibuja alrededor del cuerpo con los márgenes del JSON (ahí cae la sombra de contacto). Si el cuerpo es más chico que dos esquinas, las esquinas se achican parejas. Pura (se prueba sin pantalla).
 */
internal fun nineSlice(slab: KitSlab, pxPerDp: Float, density: Float, body: Rect): List<NinePatch> {
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
internal fun DrawScope.drawNineSlice(image: ImageBitmap, slab: KitSlab, pxPerDp: Float, body: Rect, alpha: Float = 1f) {
  for (p in nineSlice(slab, pxPerDp, density, body)) {
    drawImage(image, srcOffset = IntOffset(p.srcX, p.srcY), srcSize = IntSize(p.srcW, p.srcH), dstOffset = IntOffset(p.dstX, p.dstY), dstSize = IntSize(p.dstW, p.dstH), alpha = alpha, filterQuality = FilterQuality.High)
  }
}

/** Dibuja una pieza suelta (un disco) entera, con su cuerpo en [body] y los márgenes alrededor; [scale] la achica o agranda entera (la perilla del interruptor es un disco a 0,54×). */
internal fun DrawScope.drawKitPiece(image: ImageBitmap, piece: KitPiece, body: Rect, alpha: Float = 1f, scale: Float = 1f) {
  val d = density * scale; val m = piece.margins
  drawImage(
    image, dstOffset = IntOffset((body.left - m.left * d).roundToInt(), (body.top - m.top * d).roundToInt()),
    dstSize = IntSize(((piece.widthPx / 3f) * d).roundToInt(), ((piece.heightPx / 3f) * d).roundToInt()), alpha = alpha, filterQuality = FilterQuality.High
  )
}

/** Dibuja un ícono de churro tinta con el centro de su cuerpo en el centro del lienzo; [scale] 1 = el tamaño con que se renderizó (24 dp de lienzo de diseño). */
internal fun DrawScope.drawKitGlyph(image: ImageBitmap, piece: KitPiece, scale: Float) {
  val d = density; val m = piece.margins
  val cx = (m.left + piece.bodyWidth / 2f) * scale * d; val cy = (m.top + piece.bodyHeight / 2f) * scale * d
  drawImage(
    image, dstOffset = IntOffset((size.width / 2f - cx).roundToInt(), (size.height / 2f - cy).roundToInt()),
    dstSize = IntSize(((piece.widthPx / 3f) * scale * d).roundToInt(), ((piece.heightPx / 3f) * scale * d).roundToInt()), filterQuality = FilterQuality.High
  )
}

/** Lo que tarda la losa en pasar a presionada (y a volver). */
internal const val PRESS_MS = NubiMotion.PRESS_MS

/**
 * La losa de plasticina como contenedor: dibuja el nine-slice del [color] y [size] detrás de su contenido (el texto de Compose NO va horneado) y, si hay [onClick], al apoyar el dedo cambia a la imagen presionada en ~80 ms (con «quitar animaciones», de golpe)
 * y el contenido baja lo que dice el JSON. Mide lo que mida su contenido: las esquinas quedan fijas y solo se estira el centro, así que con letra grande crece sin deformarse. Reserva 6 dp abajo para el arranque de la sombra (el resto de la sombra es blanda y sale del cuerpo).
 * «Elegido» no es «presionado»: lo elegido lo marca quien llama (su color, un visto y su texto) y la losa queda quieta; la imagen presionada solo se ve mientras el dedo está encima.
 */
@Composable
internal fun ClaySlab(
  color: SlabColor,
  modifier: Modifier = Modifier,
  size: SlabSize = SlabSize.MEDIUM,
  onClick: (() -> Unit)? = null,
  role: Role? = Role.Button,
  /** Solo para comparaciones y pruebas: la dibuja presionada aunque nadie la toque. */
  forcePressed: Boolean = false,
  /** Dónde cae el contenido dentro del cuerpo de la losa (por defecto arriba a la izquierda; las casillas lo centran a lo alto). */
  contentAlignment: Alignment = Alignment.TopStart,
  /** Lo que dice el lector de pantalla que pasa al tocar («Mostrar opciones»); sin valor, solo «tocar dos veces para activar». */
  onClickLabel: String? = null,
  content: @Composable BoxScope.() -> Unit
) {
  val spec = rememberClayKitSpec()
  val images = slabImages(size, color)
  val slab = spec.slab(images.size.key)
  val normal = ImageBitmap.imageResource(images.normal)
  val pressedImg = ImageBitmap.imageResource(images.pressed)
  val source = remember { MutableInteractionSource() }
  val isPressed by source.collectIsPressedAsState()
  val reduce = rememberReduceMotion()
  val pressedNow = (isPressed && onClick != null) || forcePressed
  val p by animateFloatAsState(if (pressedNow) 1f else 0f, NubiMotion.finger(reduce, pressedNow), label = "losa")       // 80 ms al apretar y 140 al soltar (NubiMotion)
  val tick = rememberKitTap()
  androidx.compose.runtime.LaunchedEffect(isPressed) { if (isPressed && onClick != null) tick() }      // el tic háptico al APOYAR el dedo (KitHaptics; solo con la vibración de la app encendida)
  val click = if (onClick != null) Modifier.clickable(interactionSource = source, indication = null, role = role, onClickLabel = onClickLabel, onClick = onClick) else Modifier
  // El contenido define el tamaño; las imágenes se dibujan en una capa que llena al padre (así la losa nunca se agranda por su cuenta) y el toque va en el contenedor, sin contar los 6 dp de abajo.
  Box(modifier.padding(bottom = 6.dp).then(click), contentAlignment = contentAlignment) {
    Box(
      Modifier.matchParentSize().drawBehind {
        val body = Rect(Offset.Zero, Size(this.size.width, this.size.height))
        if (p < 1f) drawNineSlice(normal, slab, spec.pxPerDp, body, alpha = 1f - p)
        if (p > 0f) drawNineSlice(pressedImg, slab, spec.pxPerDp, body, alpha = p)
      }
    )
    Box(Modifier.offset(y = (slab.pressTextDy * p).dp)) { content() }
  }
}

/** Los íconos de churro tinta del kit. Son decorativos: el control que los lleva ya dice qué hace. */
enum class Glyph(val key: String, @DrawableRes val res: Int) {
  BACK("volver", R.drawable.kit_i_volver),
  CHECK("visto", R.drawable.kit_i_visto),
  LEFT("izq", R.drawable.kit_i_izq),
  RIGHT("der", R.drawable.kit_i_der),
  SHARE("compartir", R.drawable.kit_i_compartir),
  LOCK("candado", R.drawable.kit_i_candado),
  GEAR("engranaje", R.drawable.kit_i_engranaje),
  PLAY("jugar", R.drawable.kit_jugar),
  // Tarea 91: los que reemplazan a los íconos de Material dentro de los paneles
  GLOBE("globo", R.drawable.kit_i_globo),
  SPEAKER("altavoz", R.drawable.kit_i_altavoz),
  VIBRATION("vibracion", R.drawable.kit_i_vibracion),
  CLOCK("reloj", R.drawable.kit_i_reloj),
  CLOCK_OFF("sin_reloj", R.drawable.kit_i_sin_reloj),
  SHIELD("escudo", R.drawable.kit_i_escudo),
  INFO("info", R.drawable.kit_i_info),
  BELL("campana", R.drawable.kit_i_campana),
  PERSON("persona", R.drawable.kit_i_persona),
  UP("arriba", R.drawable.kit_i_arriba),
  DOWN("abajo", R.drawable.kit_i_abajo),
  // Tarea 167: el cometa del «Desafío de hoy» (una gota alargada en diagonal con dos estelas; sin puntas: docs/simbolos-neutros.md)
  COMET("cometa", R.drawable.kit_i_cometa)
  // (Tarea 158: los tres glifos de churro de la barra —isla, cohete y farola, de la Tarea 156— se fueron: la barra usa vectores, `NavIcons.kt`)
}

/** Un ícono de churro tinta en una caja de [size] × [size] dp (24 dp = el tamaño con que se renderizó; más grande se amplía de la misma imagen). */
@Composable
internal fun KitGlyph(glyph: Glyph, modifier: Modifier = Modifier, size: Dp = 24.dp) {
  val spec = rememberClayKitSpec()
  val piece = spec.piece(glyph.key)
  val image = ImageBitmap.imageResource(glyph.res)
  Box(modifier.size(size).drawBehind { drawKitGlyph(image, piece, size.toPx() / (24.dp.toPx())) })
}

/**
 * El reloj de plasticina del botón «Contrarreloj» (Tarea 180; `kit_i_reloj_clay`, de Blender: aro de tinta, cara crema, dos manecillas y la corona): un objeto de ~42 dp, no un glifo de churro. Se ve igual sobre lila, sol y crema (el aro es de tinta). Es decorativo: el botón ya dice qué hace.
 * [size] es el alto del cuerpo del reloj; se dibuja de la misma imagen (3 px por dp), así que más grande que ~44 dp se ampliaría.
 */
@Composable
internal fun ClayClockIcon(modifier: Modifier = Modifier, size: Dp = 42.dp) {
  val spec = rememberClayKitSpec()
  val piece = spec.piece("reloj_clay")
  val image = ImageBitmap.imageResource(R.drawable.kit_i_reloj_clay)
  Box(modifier.size(size).drawBehind { drawKitGlyph(image, piece, size.toPx() / (piece.bodyHeight * density)) }.testTag("icon_reloj_clay"))
}

/** Los colores de las cuatro áreas (Memoria, Atención, Razonamiento y Lenguaje) en el orden de la app: arriba a la izquierda, arriba a la derecha, abajo a la izquierda y abajo a la derecha. */
internal val AreaSquareColors = listOf(com.example.ui.theme.NubiTokens.Memoria, com.example.ui.theme.NubiTokens.Atencion, com.example.ui.theme.NubiTokens.Razonamiento, com.example.ui.theme.NubiTokens.Lenguaje)

/**
 * Las cuatro casillas de «Todos los juegos» (Tarea 180): un cuadro de 2 × 2 con el color de cada área, cada casilla con su borde de tinta fino (sobre la losa crema el ámbar y el verde sin borde se pierden). Decorativo: el botón ya dice «Todos los juegos». [size] = el lado del cuadro entero.
 */
@Composable
internal fun AreaSquares(modifier: Modifier = Modifier, size: Dp = 28.dp) {
  val gap = size * 0.12f
  val cell = (size - gap) / 2
  Column(modifier.size(size).testTag("icon_areas"), verticalArrangement = Arrangement.spacedBy(gap)) {
    for (row in 0..1) Row(horizontalArrangement = Arrangement.spacedBy(gap)) {
      for (col in 0..1) Box(Modifier.size(cell).background(AreaSquareColors[row * 2 + col], RoundedCornerShape(cell * 0.28f)).border(1.5.dp, Clay.Ink, RoundedCornerShape(cell * 0.28f)))
    }
  }
}

/** Los discos de plasticina de 48 dp: crema y cielo lisos (el ícono o la inicial va encima, en Compose) y el de cerrar, con la X de dos churros tinta horneada. */
internal enum class DiscStyle(val key: String, @DrawableRes val normal: Int, @DrawableRes val pressed: Int) {
  CLOSE("cerrar", R.drawable.kit_cerrar, R.drawable.kit_cerrar_p),
  CREAM("disco_crema", R.drawable.kit_disco_crema, R.drawable.kit_disco_crema_p),
  SKY("disco_cielo", R.drawable.kit_disco_cielo, R.drawable.kit_disco_cielo_p)
}

/**
 * El botón redondo de plasticina: un disco de 48 dp (tocable entero) con su [content] centrado encima; al apoyar el dedo cambia al disco hundido y el contenido baja. [label] = lo que lee el lector de pantalla.
 * Con [onClick] nulo es solo un adorno (p. ej. la inicial en el perfil): no se toca ni se anuncia como botón.
 */
@Composable
internal fun ClayDisc(
  style: DiscStyle,
  onClick: (() -> Unit)?,
  label: String,
  modifier: Modifier = Modifier,
  forcePressed: Boolean = false,
  content: @Composable BoxScope.() -> Unit = {}
) {
  val spec = rememberClayKitSpec()
  val disc = spec.disc(style.key)
  val normal = ImageBitmap.imageResource(style.normal)
  val pressedImg = ImageBitmap.imageResource(style.pressed)
  val source = remember { MutableInteractionSource() }
  val isPressed by source.collectIsPressedAsState()
  val reduce = rememberReduceMotion()
  val pressedNow = (isPressed && onClick != null) || forcePressed
  val p by animateFloatAsState(if (pressedNow) 1f else 0f, NubiMotion.finger(reduce, pressedNow), label = "disco")
  val tick = rememberKitTap()
  androidx.compose.runtime.LaunchedEffect(isPressed) { if (isPressed && onClick != null) tick() }
  Box(
    modifier
      .size(disc.bodyWidth.dp, disc.bodyHeight.dp)
      .drawBehind {
        val body = Rect(Offset.Zero, Size(size.width, size.height))
        if (p < 1f) drawKitPiece(normal, disc, body, alpha = 1f - p)
        if (p > 0f) drawKitPiece(pressedImg, disc, body, alpha = p)
      }
      .then(if (onClick != null) Modifier.clickable(interactionSource = source, indication = null, role = Role.Button, onClickLabel = label, onClick = onClick) else Modifier)
      .semantics { if (onClick != null) contentDescription = label },
    contentAlignment = Alignment.Center
  ) {
    Box(Modifier.offset(y = (disc.pressTextDy * p).dp), contentAlignment = Alignment.Center) { content() }
  }
}

/** La X de cerrar: un disco de plasticina crema con una X de dos churros tinta (la X va en la imagen). Toca 48 dp. */
@Composable
internal fun KitCloseButton(onClose: () -> Unit, modifier: Modifier = Modifier, label: String = "Cerrar", forcePressed: Boolean = false) {
  ClayDisc(DiscStyle.CLOSE, onClose, label, modifier, forcePressed)
}

/** La flecha de volver: un disco crema con la flecha de churro tinta. Toca 48 dp. */
@Composable
internal fun KitBackButton(onBack: () -> Unit, modifier: Modifier = Modifier, label: String = "Volver", forcePressed: Boolean = false) {
  ClayDisc(DiscStyle.CREAM, onBack, label, modifier, forcePressed) { KitGlyph(Glyph.BACK, size = 28.dp) }
}

/**
 * El botón de plasticina: una losa del [color] con texto de Compose en [textColor] (≥ 14 sp, 17 de base) y, si hay [glyph], su ícono de churro tinta antes del texto. 56 dp de alto como mínimo, no fijo: con letra 2,0 el texto pasa a dos líneas y la losa crece.
 */
@Composable
internal fun KitButton(
  text: String,
  onClick: () -> Unit,
  modifier: Modifier = Modifier,
  color: SlabColor = SlabColor.SUN,
  glyph: Glyph? = null,
  fontSize: TextUnit = 17.sp,
  textColor: Color = Clay.Ink,
  wide: Boolean = true,
  forcePressed: Boolean = false,
  /** Alto mínimo (56 por defecto; en la ficha del juego, «Desafío» y «Más opciones» también miden 56: Tarea 171). */
  minHeight: Dp = 56.dp,
  /** Una segunda línea más chica bajo [text] (el «Jugar» de la ficha con lo elegido, en pantallas angostas o con letra grande): dos textos, cada uno entero, y nunca se parte una palabra. */
  secondLine: String? = null,
  secondLineSize: TextUnit = 16.sp,
  /** Lo que dice el lector de pantalla que pasa al tocar («Mostrar opciones»). */
  onClickLabel: String? = null,
  /** Un dibujo propio a la izquierda del texto (con [glyph] nulo): el emblema del juego que sigue en el final de partida. */
  leading: (@Composable () -> Unit)? = null,
  /** Centra el contenido a lo alto de la losa: cuando quien llama la estira (dos botones de una fila con el alto del más alto, «Desafío de hoy» y «Todos los juegos» de Hoy, Tarea 167). */
  centerContent: Boolean = false
) {
  ClaySlab(color, if (wide) modifier.fillMaxWidth() else modifier, onClick = onClick, forcePressed = forcePressed, onClickLabel = onClickLabel, contentAlignment = if (centerContent) Alignment.Center else Alignment.TopStart) {
    Row(
      (if (wide) Modifier.fillMaxWidth() else Modifier).heightIn(min = minHeight).padding(horizontal = if (wide) 12.dp else 20.dp, vertical = if (minHeight < 56.dp) 4.dp else 8.dp),
      horizontalArrangement = Arrangement.Center, verticalAlignment = Alignment.CenterVertically
    ) {
      if (glyph != null) {
        KitGlyph(glyph)
        Spacer(Modifier.width(10.dp))
      } else if (leading != null) {
        leading()
        Spacer(Modifier.width(10.dp))
      }
      if (secondLine == null) {
        Text(text, color = textColor, fontSize = fontSize, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center, modifier = Modifier.weight(1f, fill = false))
      } else {
        Column(Modifier.weight(1f, fill = false), horizontalAlignment = Alignment.CenterHorizontally) {
          Text(text, color = textColor, fontSize = fontSize, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center)
          Text(secondLine, color = textColor, fontSize = secondLineSize, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center)
        }
      }
    }
  }
}
