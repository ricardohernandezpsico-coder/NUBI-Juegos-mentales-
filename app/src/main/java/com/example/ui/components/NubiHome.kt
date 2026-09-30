package com.example.ui.components

import android.provider.Settings
import androidx.annotation.DrawableRes
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.Image
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
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
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.R
import com.example.data.AreaStatus
import com.example.data.Skill
import com.example.ui.theme.Clay
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

/**
 * En las imágenes de Nubi (drawable-nodpi/nubi_*.webp, de tools/previews/nubi_recursos.py) el cuerpo mide esta
 * fracción del ancho del lienzo; el resto es su resplandor.
 */
const val NUBI_BODY_FRACTION = 0.65f

enum class NubiPose(@DrawableRes val res: Int) { HOLA(R.drawable.nubi_hola), MIRA(R.drawable.nubi_mira), CELEBRA(R.drawable.nubi_celebra) }

@Composable
fun rememberReduceMotion(): Boolean {
  val context = LocalContext.current
  return remember { Settings.Global.getFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f) == 0f }
}

/**
 * Nubi con su halo detrás (aprobado por Ricardo, `docs/previews/nubi-hoy.png`): resplandor suave, dos aros finos y
 * unas pocas chispas que respiran despacio (quietos con "quitar animaciones"). [size] = ancho del cuerpo de Nubi; el
 * halo ocupa 1,35 veces eso.
 */
@Composable
fun NubiWithHalo(size: Dp, modifier: Modifier = Modifier, pose: NubiPose = NubiPose.HOLA) {
  val reduceMotion = rememberReduceMotion()
  val breath = if (reduceMotion) 0.5f else {
    val t = rememberInfiniteTransition(label = "nubiHalo")
    val v by t.animateFloat(0f, 1f, infiniteRepeatable(tween(3200), RepeatMode.Reverse), label = "nubiHaloV")
    v
  }
  Box(modifier.size(size * 1.35f), contentAlignment = Alignment.Center) {
    Canvas(Modifier.fillMaxSize()) { drawHalo(breath) }
    Image(
      painter = painterResource(pose.res),
      contentDescription = "Nubi",
      modifier = Modifier.requiredSize(size / NUBI_BODY_FRACTION)
    )
  }
}

private val HaloLilac = Color(0xFF968CFF)
private val HaloSky = Color(0xFFBEE1FF)
private val HaloRing = Color(0xFFDCD7FF)

private fun DrawScope.drawHalo(breath: Float) {
  val c = center
  val r = size.minDimension / 2f
  drawCircle(Brush.radialGradient(listOf(HaloLilac.copy(alpha = 0.42f + 0.08f * breath), Color.Transparent), c, r), r, c)
  drawCircle(Brush.radialGradient(listOf(HaloSky.copy(alpha = 0.30f), Color.Transparent), c, r * 0.62f), r * 0.62f, c)
  drawCircle(HaloRing.copy(alpha = 0.28f), r * 0.86f, c, style = Stroke(1.5f * density))
  drawCircle(HaloRing.copy(alpha = 0.14f + 0.06f * breath), r * 0.97f, c, style = Stroke(1.5f * density))
  listOf(-58f to 5f, -128f to 3.5f, 18f to 3f, 152f to 4f).forEachIndexed { i, (ang, s) ->
    val a = ang * PI.toFloat() / 180f
    val p = Offset(c.x + cos(a) * r * 0.9f, c.y + sin(a) * r * 0.9f)
    val k = s * density * (0.85f + 0.3f * if (i % 2 == 0) breath else 1f - breath)
    drawCircle(Brush.radialGradient(listOf(Color.White.copy(alpha = 0.55f), Color.Transparent), p, k * 2.4f), k * 2.4f, p)
    drawPath(sparkle(p, k), Color.White)
  }
}

/** Destello de 4 puntas con lados curvos. */
private fun sparkle(c: Offset, r: Float): Path = Path().apply {
  moveTo(c.x, c.y - r)
  quadraticTo(c.x, c.y, c.x + r, c.y)
  quadraticTo(c.x, c.y, c.x, c.y + r)
  quadraticTo(c.x, c.y, c.x - r, c.y)
  quadraticTo(c.x, c.y, c.x, c.y - r)
  close()
}

// ------------------------------------------------------------------ Hoy: Nubi al centro y sus 4 áreas

private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = Color(0xFFC7D0FF)
/** Un solo color para las barras (el lavanda de Nubi): el NOMBRE dice qué área es, no el color. */
val AreaBarColor = Color(0xFFC4BAFF)
/** Lo que bajó en la semana: lila suave, nunca rojo (sin culpa). */
private val AreaDownColor = Color(0xFFB2A8E8)
private val BubbleFill = Color(0xFFFCFAFF)
private val BubbleLine = Color(0xFF4A3896)
private val BubbleLabel = Color(0xFF605890)

/** Orden de las áreas (30-sep, 4 áreas): en Hoy, índices pares a la izquierda y impares a la derecha. */
val AREA_ORDER = listOf("MEMORIA", "ATENCION", "RAZONAMIENTO", "LENGUAJE")

/**
 * Barra de avance de un área (0-100; 4 marcas finas = 5 etapas) con el cambio de la semana dibujado EN la barra, sin
 * números (Ricardo: los "▲ +4" molestaban a la vista): tramo sol al final = lo avanzado; tramo lila tenue con borde
 * punteado = lo que bajó. Forma distinta para cada caso, así no depende solo del color.
 */
@Composable
fun AreaBar(value: Float?, change: Float, modifier: Modifier = Modifier, height: Dp = 8.dp) {
  Canvas(modifier.fillMaxWidth().height(height)) {
    val w = size.width; val h = size.height; val rad = CornerRadius(h / 2f, h / 2f)
    drawRoundRect(Color.White.copy(alpha = 0.15f), cornerRadius = rad)
    val v = (value ?: 0f).coerceIn(0f, 1f)
    val before = if (change > 0f) (v - change).coerceIn(0f, 1f) else v
    if (value != null && before > 0.005f) drawRoundRect(AreaBarColor, size = Size(maxOf(h, w * before), h), cornerRadius = rad)
    if (value != null && change > 0f) {
      val x0 = maxOf(0f, w * before - h / 2f); val x1 = w * v
      drawCircle(Brush.radialGradient(listOf(Clay.Sun.copy(alpha = 0.55f), Color.Transparent), Offset(x1, h / 2f), h * 1.6f), h * 1.6f, Offset(x1, h / 2f))
      drawRoundRect(Clay.Sun, topLeft = Offset(x0, 0f), size = Size(maxOf(h, x1 - x0), h), cornerRadius = rad)
      drawCircle(Color.White, h * 0.22f, Offset(x1 - h * 0.1f, h / 2f))
    } else if (value != null && change < 0f) {
      val x0 = w * v; val x1 = w * (v - change).coerceIn(0f, 1f)
      drawRoundRect(AreaDownColor.copy(alpha = 0.28f), topLeft = Offset(x0 - h / 2f, 0f), size = Size(x1 - x0 + h / 2f, h), cornerRadius = rad)
      val step = 3f * density
      var x = x0
      while (x <= x1 + 0.1f) {
        drawCircle(AreaDownColor, 0.9f * density, Offset(x, 0f)); drawCircle(AreaDownColor, 0.9f * density, Offset(x, h))
        x += step
      }
    }
    for (k in 1..4) {
      val x = w * k / 5f
      drawLine(Color(0xAA0A0A28), Offset(x, -2f * density), Offset(x, h + 2f * density), 1.4f * density)
    }
  }
}

/** Qué dice la barra, para lectores de pantalla. */
fun areaDescription(name: String, s: AreaStatus): String {
  val v = s.points ?: return "$name: aún sin medir"
  val ch = when {
    s.change > 0f -> "avanzó esta semana"
    s.change < 0f -> "bajó un poco esta semana"
    else -> "igual que la semana pasada"
  }
  return "$name: ${Skill.stageName(s.value!!)}, $v de 100, $ch"
}

/**
 * Globo de texto de Nubi (un diálogo de Nubi: el único recuadro de la pantalla), con la cola hacia abajo o, con
 * [tailLeft], hacia la izquierda (Nubi al lado del globo, como en la ventana de un área de Juegos).
 */
@Composable
fun NubiBubble(title: String, text: String, modifier: Modifier = Modifier, tailLeft: Boolean = false) {
  Column(
    modifier
      .drawBehind {
        val tl = 12.dp.toPx()
        val x0 = if (tailLeft) tl else 0f
        val w = size.width - x0
        val h = if (tailLeft) size.height - 3.dp.toPx() else size.height - 12.dp.toPx()
        val r = CornerRadius(18.dp.toPx())
        val line = 2.dp.toPx()
        drawRoundRect(BubbleLine, topLeft = Offset(x0, 3.dp.toPx()), size = Size(w, h), cornerRadius = r)
        drawRoundRect(BubbleFill, topLeft = Offset(x0, 0f), size = Size(w, h), cornerRadius = r)
        drawRoundRect(BubbleLine, topLeft = Offset(x0, 0f), size = Size(w, h), cornerRadius = r, style = Stroke(line))
        val t = 9.dp.toPx()
        if (tailLeft) {
          val cy = h * 0.5f
          val tail = Path().apply { moveTo(x0 + line, cy - t); lineTo(x0 - tl + line, cy); lineTo(x0 + line, cy + t); close() }
          drawPath(tail, BubbleFill)
          drawLine(BubbleLine, Offset(x0 + line / 2f, cy - t), Offset(x0 - tl + line, cy), line)
          drawLine(BubbleLine, Offset(x0 + line / 2f, cy + t), Offset(x0 - tl + line, cy), line)
        } else {
          val cx = size.width / 2f
          val tail = Path().apply { moveTo(cx - t, h - line); lineTo(cx, h + 12.dp.toPx() - line); lineTo(cx + t, h - line); close() }
          drawPath(tail, BubbleFill)
          drawLine(BubbleLine, Offset(cx - t, h - line / 2f), Offset(cx, h + 12.dp.toPx() - line), line)
          drawLine(BubbleLine, Offset(cx + t, h - line / 2f), Offset(cx, h + 12.dp.toPx() - line), line)
        }
      }
      .padding(
        start = if (tailLeft) 28.dp else 16.dp, end = 16.dp, top = 8.dp,
        bottom = if (tailLeft) 14.dp else 20.dp
      )
  ) {
    Text(title, color = BubbleLabel, fontSize = 14.sp)
    Text(text, color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold)
  }
}

/**
 * El centro de Hoy (30-sep, 4 áreas, maqueta `docs/previews/cuatro-areas.png` "Hoy · B final"): Nubi grande con su halo
 * al centro; Memoria y Razonamiento a la izquierda, Atención y Lenguaje a la derecha (arriba y abajo), cada una con su
 * planeta, nombre, subtítulo, barra y etapa. Nubi crece hasta llenar el alto que sobra entre las dos filas.
 * [statuses] en el orden de [AREA_ORDER]; [names] = nombre visible de cada área. Tocar un área abre su detalle.
 */
@Composable
fun NubiHome(
  statuses: List<AreaStatus>,
  names: Map<String, String>,
  line: String,
  onArea: (String) -> Unit,
  modifier: Modifier = Modifier
) {
  Column(modifier, horizontalAlignment = Alignment.CenterHorizontally) {
    NubiBubble("Nubi", line, Modifier.padding(horizontal = 24.dp).testTag("nubi_line"))
    BoxWithConstraints(Modifier.fillMaxWidth().weight(1f)) {
      // Alto de un área (planeta + nombre + subtítulo + barra + etapa) y de Nubi: lo que sobra entre las dos filas.
      val blockHeight = 164.dp
      val nubiBody = ((maxHeight - blockHeight * 2 - 8.dp) / 1.35f).coerceIn(108.dp, 176.dp)
      NubiWithHalo(size = nubiBody, modifier = Modifier.align(Alignment.Center))
      val byArea = statuses.associateBy { it.area }
      listOf(true, false).forEach { left ->
        Column(
          Modifier
            .align(if (left) Alignment.CenterStart else Alignment.CenterEnd)
            .fillMaxHeight()
            .width(156.dp),
          verticalArrangement = Arrangement.SpaceBetween
        ) {
          AREA_ORDER.filterIndexed { i, _ -> (i % 2 == 0) == left }.forEach { key ->
            val s = byArea[key] ?: AreaStatus(key, null, 0f, emptyList())
            AreaBlock(names[key] ?: key, s, alignStart = left, onClick = { onArea(key) })
          }
        }
      }
    }
  }
}

@Composable
private fun AreaBlock(name: String, s: AreaStatus, alignStart: Boolean, onClick: () -> Unit) {
  val align = if (alignStart) Alignment.Start else Alignment.End
  val textAlign = if (alignStart) TextAlign.Start else TextAlign.End
  val tagline = com.example.model.DomainType.fromStored(s.area)?.tagline.orEmpty()
  Column(
    horizontalAlignment = align,
    modifier = Modifier
      .fillMaxWidth()
      .clip(RoundedCornerShape(12.dp))
      .clickable(onClick = onClick)
      .semantics { contentDescription = areaDescription(name, s) }
      .padding(start = if (alignStart) 16.dp else 0.dp, end = if (alignStart) 0.dp else 16.dp, top = 4.dp, bottom = 4.dp)
      .testTag("area_${s.area}")
  ) {
    // El planeta: su esfera mide AREA_BODY_FRACTION de la imagen; se corre para que el borde de la esfera quede
    // alineado con el texto.
    Image(
      painterResource(areaPlanetRes(s.area)), contentDescription = null,
      modifier = Modifier.size(58.dp).offset(x = if (alignStart) (-11).dp else 11.dp)
    )
    Text(name, color = OnNight, fontSize = 20.sp, fontWeight = FontWeight.SemiBold, textAlign = textAlign, maxLines = 1)
    Text(tagline, color = OnNightDim, fontSize = 15.sp, textAlign = textAlign, maxLines = 1)
    AreaBar(s.value, s.change, Modifier.padding(top = 6.dp, bottom = 6.dp), height = 9.dp)
    Text(
      s.value?.let { Skill.stageName(it) } ?: "Sin medir",
      color = OnNightDim, fontSize = 15.sp, textAlign = textAlign, maxLines = 1
    )
  }
}
