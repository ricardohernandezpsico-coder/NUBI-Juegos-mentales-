package com.example.ui.components

import androidx.annotation.DrawableRes
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.imageResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.R
import com.example.data.FamilyMedalState
import com.example.data.AchievementFamily
import com.example.data.KitAvanceSpec
import com.example.model.RankTier
import com.example.ui.theme.Clay

/**
 * Las piezas del kit para Avance y Juegos (Tarea 125; renders de Blender de la Tarea 124, `tools/isla/kit_avance.py`; `res/raw/kit_avance.json` dice cómo dibujarlas): el canal y el churro de las barras, la bolita del cambio, el emblema de cada juego,
 * la farola, el pedestal de un lugar, la cola del globo de Nubi, las medallas de las tres familias y el escudo de cada liga. El texto nunca va horneado: el número de la medalla y todo lo demás se escriben en Compose.
 */
private object KitAvanceCache { @Volatile var spec: KitAvanceSpec? = null }

@Composable
internal fun rememberKitAvanceSpec(): KitAvanceSpec {
  val context = LocalContext.current
  return remember { KitAvanceCache.spec ?: KitAvanceSpec.parse(context.resources.openRawResource(R.raw.kit_avance).bufferedReader().use { it.readText() }).also { KitAvanceCache.spec = it } }
}

private val EMBLEMS: Map<String, Int> = mapOf(
  "parejas" to R.drawable.kit_emb_parejas, "secuencia" to R.drawable.kit_emb_secuencia, "bodega" to R.drawable.kit_emb_bodega, "correo" to R.drawable.kit_emb_correo,
  "stroop" to R.drawable.kit_emb_stroop, "piloto" to R.drawable.kit_emb_piloto, "freno" to R.drawable.kit_emb_freno, "satelites" to R.drawable.kit_emb_satelites, "radar" to R.drawable.kit_emb_radar,
  "calculo" to R.drawable.kit_emb_calculo, "engranajes" to R.drawable.kit_emb_engranajes, "acoplamiento" to R.drawable.kit_emb_acoplamiento, "aterrizaje" to R.drawable.kit_emb_aterrizaje,
  "anagramas" to R.drawable.kit_emb_anagramas, "meteoros" to R.drawable.kit_emb_meteoros, "disparate" to R.drawable.kit_emb_disparate, "intrusa" to R.drawable.kit_emb_intrusa
)

/** El emblema (la ficha con el objeto del juego) de un juego, o null si es uno retirado. */
internal fun emblemRes(gameId: String): Int? = EMBLEMS[gameId]

/** Los ids de los juegos que tienen emblema (los 17 de la app). */
internal val EMBLEM_GAME_IDS: Set<String> get() = EMBLEMS.keys

private val MEDALS: Map<AchievementFamily, Pair<Int, Int>> = mapOf(
  AchievementFamily.CONSTANCIA to (R.drawable.kit_med_constancia to R.drawable.kit_med_constancia_off),
  AchievementFamily.VARIEDAD to (R.drawable.kit_med_variedad to R.drawable.kit_med_variedad_off),
  AchievementFamily.VOLVER to (R.drawable.kit_med_volver to R.drawable.kit_med_volver_off)
)

private val SHIELDS: Map<RankTier, Int> = mapOf(
  RankTier.BRONCE to R.drawable.kit_escudo_bronce, RankTier.PLATA to R.drawable.kit_escudo_plata, RankTier.ORO to R.drawable.kit_escudo_oro, RankTier.PLATINO to R.drawable.kit_escudo_platino,
  RankTier.ESMERALDA to R.drawable.kit_escudo_esmeralda, RankTier.DIAMANTE to R.drawable.kit_escudo_diamante, RankTier.MAESTRO to R.drawable.kit_escudo_maestro
)

/**
 * Una pieza suelta del kit dibujada para que su CUERPO mida [width] (o [height]) dp; la sombra cae fuera de esa caja, en los márgenes de la imagen. La caja mide lo que el cuerpo: se dibuja con `drawBehind`, sin recortar.
 */
@Composable
internal fun KitPieceImage(key: String, @DrawableRes res: Int, modifier: Modifier = Modifier, width: Dp? = null, height: Dp? = null, alpha: Float = 1f) {
  val spec = rememberKitAvanceSpec()
  val piece = spec.piece(key)
  val image = ImageBitmap.imageResource(res)
  val k = when { width != null -> width.value / piece.bodyWidth; height != null -> height.value / piece.bodyHeight; else -> 1f }
  Box(modifier.size((piece.bodyWidth * k).dp, (piece.bodyHeight * k).dp).drawBehind { drawKitPiece(image, piece.asPiece(), Rect(Offset.Zero, size), alpha, k) })
}

/**
 * El emblema de un juego: su ficha (64 dp de cuerpo, en el pastel de su área, con el objeto del juego) a [size] dp. Con [done], el sello lima con el ✓ de «jugado hoy» (forma, no solo color). Un juego sin emblema (retirado) cae al planeta de siempre.
 * La caja es cuadrada de [size] para que las filas se alineen.
 */
@Composable
internal fun GameEmblem(gameId: String, size: Dp, modifier: Modifier = Modifier, done: Boolean = false) {
  val res = emblemRes(gameId)
  if (res == null) { MiniPlanet(gameId, size, modifier, done); return }
  Box(modifier.size(size), contentAlignment = Alignment.Center) {
    KitPieceImage("emb_$gameId", res, width = size)
    if (done) {
      Box(
        Modifier.align(Alignment.TopEnd).offset(x = 4.dp, y = (-4).dp).size(size * 0.42f).clip(CircleShape).background(Clay.Lime).border(2.dp, Clay.Ink, CircleShape),
        contentAlignment = Alignment.Center
      ) { KitGlyph(Glyph.CHECK, size = size * 0.3f) }
    }
  }
}

/**
 * La barra de avance de un área con el canal y el churro del kit: el churro (sol sobre la tarjeta crema, lima sobre el cielo) llega hasta [value] y la bolita lima marca dónde estabas hace una semana cuando hubo [change]:
 * dentro del churro si subió y más allá de su punta si bajó (una forma, no solo un color). Sin medir ([value] nulo) solo se ve el canal.
 */
@Composable
internal fun KitBar(value: Float?, change: Float, modifier: Modifier = Modifier, onCard: Boolean = true) {
  val spec = rememberKitAvanceSpec()
  val canal = spec.piece(if (onCard) "canal_crema" else "canal_cielo"); val churro = spec.piece(if (onCard) "churro_sol" else "churro_lima"); val bolita = spec.piece("bolita")
  val canalImg = ImageBitmap.imageResource(if (onCard) R.drawable.kit_canal_crema else R.drawable.kit_canal_cielo)
  val churroImg = ImageBitmap.imageResource(if (onCard) R.drawable.kit_churro_sol else R.drawable.kit_churro_lima)
  val bolitaImg = ImageBitmap.imageResource(R.drawable.kit_bolita)
  Box(
    modifier.fillMaxWidth().height(canal.bodyHeight.dp).drawBehind {
      val d = density
      drawNineSlice(canalImg, canal.asStrip(), spec.pxPerDp, Rect(Offset.Zero, size))
      if (value == null) return@drawBehind
      val v = value.coerceIn(0f, 1f)
      val x0 = 2.5.dp.toPx(); val inner = size.width - 2 * x0
      val w = maxOf(churro.bodyHeight * d, inner * v)                                          // al menos una cápsula entera
      val top = (size.height - churro.bodyHeight * d) / 2f - 0.5.dp.toPx()
      drawNineSlice(churroImg, churro.asStrip(), spec.pxPerDp, Rect(x0, top, x0 + w, top + churro.bodyHeight * d))
      if (change != 0f) {
        val before = (v - change).coerceIn(0f, 1f)
        val cx = x0 + inner * before
        val bw = bolita.bodyWidth * d
        drawKitPiece(bolitaImg, bolita.asPiece(), Rect(cx - bw / 2f, (size.height - bolita.bodyHeight * d) / 2f - 0.5.dp.toPx(), cx + bw / 2f, (size.height + bolita.bodyHeight * d) / 2f - 0.5.dp.toPx()))
      }
    }
  )
}

/** La farola del kit (la de la isla y del ícono de Avance), apagada o encendida, de [height] dp de alto. */
@Composable
internal fun FarolaImage(on: Boolean, height: Dp, modifier: Modifier = Modifier) {
  KitPieceImage(if (on) "farola_on" else "farola_off", if (on) R.drawable.kit_farola_on else R.drawable.kit_farola_off, modifier, height = height)
}

/** El pedestal de un lugar de Juegos: la losa crema de la isla con su base de tierra, de [width] dp de cuerpo. */
@Composable
internal fun PedestalImage(width: Dp, modifier: Modifier = Modifier) {
  KitPieceImage("pedestal", R.drawable.kit_pedestal, modifier, width = width)
}

/** La cola del globo de Nubi (16 × 12 dp): con la punta hacia abajo a la izquierda o a la derecha. */
@Composable
internal fun BubbleTail(left: Boolean, modifier: Modifier = Modifier) {
  if (left) KitPieceImage("cola_izq", R.drawable.kit_cola_izq, modifier) else KitPieceImage("cola_der", R.drawable.kit_cola_der, modifier)
}

/**
 * La medalla de una familia de logros: plasticina cruda si está bloqueada, con color y luz si no. De [height] dp de alto; si cabe (96 dp o más) escribe sobre su placa lisa el número del último nivel conseguido (7 días, 25 partidas…).
 */
@Composable
internal fun FamilyMedal(state: FamilyMedalState, height: Dp, modifier: Modifier = Modifier) {
  val (on, off) = MEDALS.getValue(state.family)
  val key = "med_" + state.family.name.lowercase() + if (state.unlocked) "" else "_off"
  val spec = rememberKitAvanceSpec()
  val piece = spec.piece(key)
  val k = height.value / piece.bodyHeight
  Box(modifier.size((piece.bodyWidth * k).dp, height)) {
    KitPieceImage(key, if (state.unlocked) on else off, height = height)
    val plate = piece.plate
    val number = state.number
    if (plate != null && number != null && height >= 80.dp) {
      val cy = (plate.centerY - piece.margins.top) * k
      Text(
        "$number", color = Clay.Ink, fontSize = (plate.height * k * 0.8f).sp, fontWeight = FontWeight.Bold,
        modifier = Modifier.align(Alignment.TopCenter).offset(y = (cy - plate.height * k * 0.5f).dp)
      )
    }
  }
}

/** El escudo de una liga, de [height] dp de alto (40 en la fila de Avance, 96 en el Perfil, 120 en la celebración). El número de la división va en Compose, no en la imagen. */
@Composable
internal fun ShieldImage(tier: RankTier, height: Dp, modifier: Modifier = Modifier) {
  KitPieceImage("escudo_" + tier.name.lowercase(), SHIELDS.getValue(tier), modifier, height = height)
}

private val BUILDINGS: Map<String, Int> = mapOf(
  "MEMORIA" to R.drawable.kit_edif_memoria, "ATENCION" to R.drawable.kit_edif_atencion, "RAZONAMIENTO" to R.drawable.kit_edif_razonamiento, "LENGUAJE" to R.drawable.kit_edif_lenguaje
)

private fun buildingRes(area: String): Int = BUILDINGS[area.uppercase()] ?: R.drawable.kit_edif_memoria

/** Lo que sobresale el edificio más alto (la antena) sobre el suelo, en dp al tamaño del render: para reservar el alto de [BuildingOnPedestal]. */
private const val BUILDING_ABOVE_DP = 72f
/** Dónde está el centro de la cara de arriba del pedestal, en dp bajo el borde de arriba de su cuerpo (al tamaño del render). */
private const val PEDESTAL_FACE_Y_DP = 21.7f

/**
 * El edificio de un área sobre su pedestal (Tarea 125, la grilla de Juegos): el pedestal de [width] dp de cuerpo y, parado en el centro de su cara, el edificio de la isla (observatorio = Memoria, antena = Atención, hangar = Razonamiento,
 * biblioteca = Lenguaje) a la misma escala. Con [lamp], la farola encendida del lugar en la esquina del pedestal (la «Sugerida»); [overlay] va encima de la caja (el sello de «jugada hoy»).
 */
@Composable
internal fun BuildingOnPedestal(area: String, width: Dp, modifier: Modifier = Modifier, lamp: Boolean = false, overlay: @Composable BoxScope.() -> Unit = {}) {
  val spec = rememberKitAvanceSpec()
  val ped = spec.piece("pedestal"); val b = spec.piece("edif_" + area.lowercase())
  val k = width.value / ped.bodyWidth
  val above = BUILDING_ABOVE_DP * k
  val faceY = above + PEDESTAL_FACE_Y_DP * k
  Box(modifier.size(width, (ped.bodyHeight * k + above).dp)) {
    PedestalImage(width, Modifier.align(Alignment.BottomCenter))
    Image(
      painterResource(buildingRes(area)), contentDescription = null, contentScale = ContentScale.FillBounds,
      modifier = Modifier.size((b.widthPx / spec.pxPerDp * k).dp).offset(x = (ped.bodyWidth / 2f * k - b.footX * k).dp, y = (faceY - b.footY * k).dp)
    )
    if (lamp) FarolaImage(on = true, height = 28.dp, modifier = Modifier.align(Alignment.TopStart).offset(x = (ped.bodyWidth * 0.8f * k - 8f).dp, y = (faceY - 26f).dp))
    overlay()
  }
}

/** Solo el edificio de un área (sin pedestal), de [height] dp de alto de cuerpo: la cabecera de la ventana de un área. La caja es cuadrada de [box] dp para que las cabeceras se alineen. */
@Composable
internal fun BuildingIcon(area: String, height: Dp, box: Dp, modifier: Modifier = Modifier) {
  Box(modifier.size(box), contentAlignment = Alignment.Center) { KitPieceImage("edif_" + area.lowercase(), buildingRes(area), height = height) }
}
