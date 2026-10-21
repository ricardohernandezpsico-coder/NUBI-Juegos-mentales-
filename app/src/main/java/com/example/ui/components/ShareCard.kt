package com.example.ui.components

import android.content.ClipData
import android.content.Context
import android.content.Intent
import android.graphics.Bitmap
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Canvas
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.Shadow
import androidx.compose.ui.graphics.asAndroidBitmap
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.graphics.drawscope.CanvasDrawScope
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.inset
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.text.TextMeasurer
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.drawText
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.font.createFontFamilyResolver
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.unit.sp
import androidx.core.content.FileProvider
import com.example.data.AchievementDef
import com.example.model.RankTier
import com.example.ui.theme.Clay
import com.example.ui.theme.AppFamily
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.File
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin
import kotlin.random.Random

/**
 * Tarjeta para compartir (imagen 1080x1920, formato de historia) con el sello de la app: cielo nocturno con
 * estrellas, el escudo de la liga (o la medalla de un logro) sobre rayos de su color, el titular ("Subí a Plata",
 * "Una semana"), el detalle, hasta dos cifras (trofeos, racha, logros) y la marca. Se dibuja fuera de pantalla con las mismas funciones que la app ([drawLeagueShield], Fredoka), así
 * se ve igual que adentro, y se comparte con la hoja de compartir de Android (WhatsApp, Instagram, etc.).
 */
object ShareCard {
  /** El pie de la tarjeta (Tarea 149: antes «Juega. Avanza. Sube de liga.», y las ligas quedaron fuera de la v1). */
  const val FOOTER = "Juega. Avanza. Cada día un poco."

  /** El archivo que se comparte (antes `neurovida-liga.png`: nombre viejo de la app y de la liga). */
  const val FILE_NAME = "nubi-medalla.png"

  data class Content(
    val headline: String,       // "Subí a Plata" / "Estoy en liga Oro" / "Una semana"
    val subtitle: String,       // "en Secuencia Lumínica" / "Mi liga general" / descripción del logro
    val tier: RankTier? = null, // escudo de liga...
    val achievement: AchievementDef? = null, // ...o medalla de logro
    val stats: List<Pair<String, String>> = emptyList() // cifra -> rótulo ("255" -> "trofeos"), máx. 2
  ) {
    companion object {
      /** Cifras de siempre: trofeos y, si hay, racha. */
      fun trophiesAndStreak(rating: Int, streak: Int): List<Pair<String, String>> = buildList {
        add("$rating" to "trofeos")
        if (streak > 0) add("$streak" to if (streak == 1) "día de racha" else "días de racha")
      }
    }
  }

  private const val W = 1080
  private const val H = 1920

  /** La medalla de plasticina de una familia lista para dibujar en la tarjeta (Tarea 145: la misma de la celebración, de «Tus medallas» y del Perfil). */
  private class MedalArt(val image: ImageBitmap, val piece: com.example.data.KitAvancePiece, val number: Int)

  /** null si el logro no tiene familia (los viejos, de desempeño, que ya no se otorgan): ahí la tarjeta conserva el dibujo de siempre. */
  private fun medalArt(context: Context, def: AchievementDef?): MedalArt? {
    val family = def?.family ?: return null
    val image = android.graphics.BitmapFactory.decodeResource(context.resources, familyMedalRes(family, true))?.asImageBitmap() ?: return null
    return MedalArt(image, loadKitAvanceSpec(context).piece("med_" + family.name.lowercase()), def.number ?: 1)
  }

  /** Dibuja la tarjeta. Puede correr fuera del hilo principal. */
  fun render(context: Context, content: Content): Bitmap {
    val image = ImageBitmap(W, H)
    val density = Density(1f) // 1 unidad = 1 px de la imagen
    val measurer = TextMeasurer(createFontFamilyResolver(context), density, LayoutDirection.Ltr)
    val medal = medalArt(context, content.achievement)
    CanvasDrawScope().draw(density, LayoutDirection.Ltr, Canvas(image), Size(W.toFloat(), H.toFloat())) {
      drawCard(content, measurer, medal)
    }
    return image.asAndroidBitmap()
  }

  /** Genera la imagen, la guarda en caché y abre la hoja de compartir con ella y [text]. */
  suspend fun share(context: Context, content: Content, text: String) {
    val uri = withContext(Dispatchers.Default) {
      val bitmap = render(context, content)
      val dir = File(context.cacheDir, "share").apply { mkdirs() }
      val file = File(dir, FILE_NAME)
      file.outputStream().use { bitmap.compress(Bitmap.CompressFormat.PNG, 100, it) }
      FileProvider.getUriForFile(context, "${context.packageName}.fileprovider", file)
    }
    val send = Intent(Intent.ACTION_SEND)
      .setType("image/png")
      .putExtra(Intent.EXTRA_STREAM, uri)
      .putExtra(Intent.EXTRA_TEXT, text)
      .addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
    send.clipData = ClipData.newRawUri("", uri) // para que la app elegida reciba el permiso de lectura
    context.startActivity(Intent.createChooser(send, "Compartir").addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION))
  }

  private fun DrawScope.drawCard(content: Content, measurer: TextMeasurer, medal: MedalArt? = null) {
    val w = size.width
    val h = size.height
    val accentLight = content.tier?.palette()?.light ?: content.achievement?.medalColor()?.let { lerp(it, Color.White, 0.45f) } ?: Color.White
    val accentGlow = content.tier?.palette()?.glow ?: content.achievement?.medalColor() ?: Clay.Sky

    // Cielo nocturno de la app + nebulosas celeste y coral
    drawRect(Brush.verticalGradient(listOf(Color(0xFF1C1B52), Color(0xFF100F3A), Color(0xFF07071F))))     // los tres tonos del fondo «Espacio profundo» (Tarea 122)
    glow(Offset(w * 0.88f, h * 0.10f), 950f, Clay.Sky.copy(alpha = 0.30f))
    glow(Offset(w * 0.08f, h * 0.92f), 950f, Clay.Coral.copy(alpha = 0.26f))
    glow(Offset(w * 0.15f, h * 0.45f), 700f, Clay.Grape.copy(alpha = 0.16f))

    // Estrellas (siempre las mismas: la tarjeta no cambia al volver a generarla)
    val rnd = Random(7)
    repeat(170) { i ->
      val p = Offset(rnd.nextFloat() * w, rnd.nextFloat() * h)
      val r = 1.5f + rnd.nextFloat() * 3.5f
      val c = if (i % 6 == 0) Color(0xFFFFC38A) else Color.White
      drawCircle(c.copy(alpha = 0.3f + rnd.nextFloat() * 0.7f), r, p)
    }

    // Marca arriba
    val brand = TextStyle(fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 64.sp, color = Color.White)
    centered(measurer, "Nubi", brand, 130f)
    sparkle(Offset(w / 2f + 110f, 150f), 22f, Clay.Sun)

    // Escudo sobre rayos del color de la liga
    val shieldW = 470f
    val shieldH = shieldW * 1.12f
    val c = Offset(w / 2f, 720f)
    glow(c, 560f, accentGlow.copy(alpha = 0.55f))
    val rays = 14
    for (i in 0 until rays) {
      val a = (i * 360f / rays - 90f) * PI.toFloat() / 180f
      val half = 0.085f
      val reach = 640f
      val ray = Path().apply {
        moveTo(c.x, c.y)
        lineTo(c.x + cos(a - half) * reach, c.y + sin(a - half) * reach)
        lineTo(c.x + cos(a + half) * reach, c.y + sin(a + half) * reach)
        close()
      }
      drawPath(ray, Brush.radialGradient(listOf(accentLight.copy(alpha = 0.30f), Color.Transparent), c, reach))
    }
    val tier = content.tier
    val achievement = content.achievement
    if (tier != null) {
      inset(c.x - shieldW / 2f, c.y - shieldH / 2f, w - (c.x + shieldW / 2f), h - (c.y + shieldH / 2f)) {
        drawLeagueShield(tier, pips = 1)
      }
    } else if (achievement != null && medal != null) {
      // La medalla de plasticina de su familia, con el número de su nivel en la placa (la misma de la celebración)
      val bodyH = 520f
      val k = bodyH / medal.piece.bodyHeight
      val bodyW = medal.piece.bodyWidth * k
      val left = c.x - bodyW / 2f; val top = c.y - bodyH / 2f
      drawKitPiece(medal.image, medal.piece.asPiece(), androidx.compose.ui.geometry.Rect(left, top, left + bodyW, top + bodyH), 1f, k)
      val plate = medal.piece.plate
      if (plate != null) {
        val layout = measurer.measure("${medal.number}", TextStyle(fontFamily = AppFamily, fontWeight = FontWeight.Bold, color = Clay.Ink, fontSize = (plate.height * k * 0.8f).sp))
        val cy = top + (plate.centerY - medal.piece.margins.top) * k
        drawText(layout, topLeft = Offset(c.x - layout.size.width / 2f, cy - layout.size.height / 2f))
      }
    } else if (achievement != null) {
      val m = 560f
      inset(c.x - m / 2f, c.y - m / 2f, w - (c.x + m / 2f), h - (c.y + m / 2f)) {
        drawAchievementMedal(achievement, unlocked = true, measurer = measurer)
      }
    }

    // Titular, dónde y cifras
    val headline = TextStyle(
      fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 116.sp, lineHeight = 124.sp,
      color = accentLight, textAlign = TextAlign.Center, shadow = Shadow(Clay.Ink, Offset(0f, 10f), 0f)
    )
    var y = centered(measurer, content.headline, headline, 1070f)
    val sub = TextStyle(fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = 54.sp, color = Color.White, textAlign = TextAlign.Center)
    y = centered(measurer, content.subtitle, sub, y + 18f)

    val num = TextStyle(fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 92.sp, color = Clay.Sun, textAlign = TextAlign.Center)
    val lbl = TextStyle(fontFamily = AppFamily, fontSize = 38.sp, color = com.example.ui.theme.NubiTokens.OnNightDim, textAlign = TextAlign.Center)
    val stats = content.stats.take(2)
    val statsTop = y + 110f
    val colW = w / stats.size.coerceAtLeast(1)
    stats.forEachIndexed { i, (n, l) ->
      val left = colW * i
      val nb = measurer.measure(n, num, constraints = Constraints.fixedWidth(colW.toInt()))
      drawText(nb, topLeft = Offset(left, statsTop))
      val lb = measurer.measure(l, lbl, constraints = Constraints.fixedWidth(colW.toInt()))
      drawText(lb, topLeft = Offset(left, statsTop + nb.size.height - 6f))
    }
    if (stats.size == 2) drawCircle(com.example.ui.theme.NubiTokens.OnNightDim.copy(alpha = 0.6f), 7f, Offset(w / 2f, statsTop + 70f))

    // Pie
    val foot = TextStyle(fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = 40.sp, color = Color.White.copy(alpha = 0.7f), textAlign = TextAlign.Center)
    centered(measurer, FOOTER, foot, h - 170f)
  }

  /** Texto centrado a todo el ancho (con márgenes); devuelve la coordenada Y de su borde inferior. */
  private fun DrawScope.centered(measurer: TextMeasurer, text: String, style: TextStyle, top: Float): Float {
    val margin = 70f
    val layout = measurer.measure(text, style.copy(textAlign = TextAlign.Center), constraints = Constraints.fixedWidth((size.width - margin * 2).toInt()))
    drawText(layout, topLeft = Offset(margin, top))
    return top + layout.size.height
  }

  private fun DrawScope.glow(c: Offset, r: Float, color: Color) {
    drawCircle(Brush.radialGradient(listOf(color, Color.Transparent), c, r), radius = r, center = c)
  }

  private fun DrawScope.sparkle(c: Offset, r: Float, color: Color) {
    drawCircle(color.copy(alpha = color.alpha * 0.35f), r, c)
    drawCircle(color, r * 0.5f, c)
  }
}
