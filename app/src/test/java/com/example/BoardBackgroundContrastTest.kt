package com.example

import android.graphics.Bitmap
import android.graphics.BitmapFactory
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.graphics.toArgb
import com.example.model.DomainType
import com.example.ui.components.BoardNavy
import com.example.ui.components.OnBoardHalo
import com.example.ui.theme.Clay
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import kotlin.math.pow

/**
 * El fondo en dos capas: la lámina SIN luceros (`fondo_lamina`) y los luceros con su sombra (`fondo_luceros`), fijos detrás de las tres pestañas. Desde la Tarea 122 es «Espacio profundo» (variante E de `tools/isla/isla.py`, 17-oct: índigo casi negro, nebulosa apagada,
 * viñeta del 25 %, 55 luceros colocados en tres profundidades); antes fue el B azul marino.
 * Se miden las imágenes que entran a la app: el saludo se lee sobre la lámina con 4,5:1 o más (WCAG AA), `OnBoardHalo.Tone` (el contorno de los textos claros) es la MEDIANA medida de la lámina (si no, cada letra sale con un aura), todos los textos claros
 * (crema, blanco, sol, `OnNightDim`, coral y las cuatro áreas aclaradas) pasan de 4,5:1 arriba, en medio y al pie, la lámina no tiene ningún lucero y los luceros no caen en las zonas que `tools/isla/isla.py` (`ZONAS_HOY`) deja libres arriba para el saludo de Hoy y los títulos
 * de Juegos y Avance, y que son entre 40 y 52 (visibles).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [36])
class BoardBackgroundContrastTest {
  private fun lum(rgb: Int): Double {
    fun ch(v: Int): Double { val c = v / 255.0; return if (c <= 0.03928) c / 12.92 else ((c + 0.055) / 1.055).pow(2.4) }
    return 0.2126 * ch((rgb shr 16) and 0xFF) + 0.7152 * ch((rgb shr 8) and 0xFF) + 0.0722 * ch(rgb and 0xFF)
  }

  private fun contrast(a: Int, b: Int): Double {
    val la = lum(a); val lb = lum(b)
    return (maxOf(la, lb) + 0.05) / (minOf(la, lb) + 0.05)
  }

  private val cream = 0xF6EBD3        // el saludo
  private val soft = 0xD6D1F0         // la fecha y los subtítulos
  private val sun = 0xFFC93C          // la racha

  private fun lamina(): Bitmap = BitmapFactory.decodeFile("src/main/res/drawable-nodpi/fondo_lamina.webp")

  private class Band(val name: String, val y0: Double, val y1: Double)
  /** Arriba (donde está el saludo), en medio (donde está casi todo) y el pie (la barra de pestañas y los botones). */
  private val bands = listOf(Band("arriba", 0.0, 0.12), Band("medio", 0.40, 0.60), Band("pie", 0.88, 1.0))

  private fun pixelsOf(bmp: Bitmap, band: Band): IntArray {
    val ys = (bmp.height * band.y0).toInt() until (bmp.height * band.y1).toInt()
    val out = IntArray(ys.count() * bmp.width)
    var i = 0
    for (y in ys) for (x in 0 until bmp.width) out[i++] = bmp.getPixel(x, y) and 0xFFFFFF
    return out
  }

  private fun medianColor(px: IntArray): Int {
    fun channel(shift: Int) = px.map { (it shr shift) and 0xFF }.sorted()[px.size / 2]
    return (channel(16) shl 16) or (channel(8) shl 8) or channel(0)
  }

  /** El píxel más claro de la banda sin contar el 1 % más extremo (el color contra el que peor se lee un texto claro). */
  private fun brightestColor(px: IntArray): Int = px.sortedBy { lum(it) }[(px.size * 0.99).toInt()]

  private fun hex(c: Int) = "#%06X".format(c and 0xFFFFFF)

  @Test
  fun el_saludo_se_lee_sobre_el_fondo_con_4_5_a_1_o_mas() {
    val bmp = BitmapFactory.decodeFile("src/main/res/drawable-nodpi/fondo_lamina.webp")
    assertEquals("el fondo mide 720 × 1200 px a 2×", 720 to 1200, bmp.width to bmp.height)
    val x0 = (bmp.width * 0.04).toInt(); val x1 = (bmp.width * 0.70).toInt()
    val y0 = (bmp.height * 0.08).toInt(); val y1 = (bmp.height * 0.22).toInt()
    var worstCream = 99.0; var worstSoft = 99.0; var worstSun = 99.0
    for (y in y0 until y1 step 2) for (x in x0 until x1 step 2) {
      val px = bmp.getPixel(x, y) and 0xFFFFFF
      worstCream = minOf(worstCream, contrast(cream, px)); worstSoft = minOf(worstSoft, contrast(soft, px)); worstSun = minOf(worstSun, contrast(sun, px))
    }
    assertTrue("crema %.1f:1".format(worstCream), worstCream >= 4.5)
    assertTrue("fecha %.1f:1".format(worstSoft), worstSoft >= 4.5)
    assertTrue("sol %.1f:1".format(worstSun), worstSun >= 4.5)
  }

  @Test
  fun el_contorno_de_los_textos_es_la_mediana_medida_de_la_lamina() {
    val bmp = lamina()
    val all = medianColor(pixelsOf(bmp, Band("toda", 0.0, 1.0)))
    val tone = OnBoardHalo.Tone.toArgb()
    for (shift in listOf(16, 8, 0)) {
      val m = (all shr shift) and 0xFF; val t = (tone shr shift) and 0xFF
      assertTrue("OnBoardHalo.Tone ${hex(tone)} se aparta de la mediana de la lámina ${hex(all)} (canal $shift): cada letra saldría con un aura", kotlin.math.abs(m - t) <= 2)
    }
    assertEquals("BoardNavy es el material del render (#0E0D33)", 0x0E0D33, BoardNavy.toArgb() and 0xFFFFFF)
  }

  @Test
  fun la_lamina_es_mas_oscura_abajo_que_arriba_la_vinieta() {
    val bmp = lamina()
    val top = lum(medianColor(pixelsOf(bmp, bands[0]))); val foot = lum(medianColor(pixelsOf(bmp, bands[2])))
    assertTrue("el pie (%.4f) debe ser más oscuro que arriba (%.4f)".format(foot, top), foot < top * 0.8)
    assertTrue("la lámina es de noche: la mediana de arriba es casi negra (luminancia %.4f)".format(top), top < 0.03)
  }

  /** Los textos claros que van directo sobre el fondo, contra arriba, en medio y al pie (mediana y el 1 % más claro de cada banda). */
  @Test
  fun todos_los_textos_claros_pasan_de_4_5_a_1_arriba_en_medio_y_al_pie() {
    val bmp = lamina()
    fun light(d: DomainType) = lerp(d.color, Color.White, 0.45f).toArgb() and 0xFFFFFF      // `domainTextColor`: el color del área aclarado para el cielo
    val tokens = listOf(
      "crema del saludo" to cream, "blanco (títulos)" to 0xFFFFFF, "sol (racha)" to (Clay.Sun.toArgb() and 0xFFFFFF), "sol claro" to 0xFFCF5A, "subtítulo" to soft,
      "OnNightDim de Avance" to 0xB4BFEA, "OnNightDim de Hoy y Juegos" to 0xC7D0FF, "coral" to (Clay.Coral.toArgb() and 0xFFFFFF),
      "Memoria clara" to light(DomainType.MEMORIA), "Atención clara" to light(DomainType.ATENCION), "Razonamiento claro" to light(DomainType.RAZONAMIENTO), "Lenguaje claro" to light(DomainType.LENGUAJE)
    )
    val failures = mutableListOf<String>()
    for (band in bands) {
      val px = pixelsOf(bmp, band)
      for ((bgName, bg) in listOf("mediana" to medianColor(px), "el 1 % más claro" to brightestColor(px))) {
        for ((name, fg) in tokens) {
          val c = contrast(fg, bg)
          if (c < 4.5) failures += "$name sobre ${band.name} ($bgName ${hex(bg)}): %.1f:1".format(c)
        }
      }
    }
    assertTrue("textos que no llegan a 4,5:1 sobre la lámina nueva:\n" + failures.joinToString("\n"), failures.isEmpty())
  }

  @Test
  fun la_lamina_no_tiene_ningun_lucero() {
    val bmp = BitmapFactory.decodeFile("src/main/res/drawable-nodpi/fondo_lamina.webp")
    var brightest = 0.0
    for (y in 0 until bmp.height step 2) for (x in 0 until bmp.width step 2) brightest = maxOf(brightest, lum(bmp.getPixel(x, y) and 0xFFFFFF))
    assertTrue("el píxel más claro de la lámina debe ser de fondo, no un lucero (luminancia %.2f)".format(brightest), brightest < 0.12)
  }

  // Mismos valores que ZONAS_HOY de tools/isla/isla.py (x0, y0, x1, y1 en fracción de la imagen, y hacia abajo). Tarea 87: una sola zona, arriba: el saludo de Hoy y los títulos de Juegos y Avance.
  private val zonasHoy = listOf(
    floatArrayOf(0.0f, 0.03f, 1.0f, 0.28f)    // saludo de Hoy, fecha con la meta, farolitos y racha; «Tu avance»; «¿Qué jugamos hoy?» y su sugerencia
  )

  @Test
  fun los_luceros_no_caen_en_la_zona_sin_luceros_de_arriba() {
    val bmp = BitmapFactory.decodeFile("src/main/res/drawable-nodpi/fondo_luceros.webp")
    assertEquals("los luceros miden 720 × 1200 px a 2×", 720 to 1200, bmp.width to bmp.height)
    assertTrue("la capa de luceros tiene transparencia", bmp.hasAlpha())
    var total = 0
    for (y in 0 until bmp.height) for (x in 0 until bmp.width) if ((bmp.getPixel(x, y) ushr 24) >= 128) total++
    assertTrue("la capa de luceros no está vacía ($total píxeles opacos)", total > 1000)
    for (z in zonasHoy) {
      var inside = 0
      for (y in (bmp.height * z[1]).toInt() until (bmp.height * z[3]).toInt()) for (x in (bmp.width * z[0]).toInt() until (bmp.width * z[2]).toInt()) if ((bmp.getPixel(x, y) ushr 24) >= 128) inside++
      assertEquals("píxeles de lucero dentro de la zona ${z.toList()}", 0, inside)
    }
  }

  /**
   * Cuenta los luceros de la capa: las manchas conexas de píxeles CLAROS (opacos, α ≥ 128, y de luminancia ≥ 0,30) de al menos 5 píxeles. Con el fondo «Espacio profundo» los luceros chicos miden 1 % del ancho (7 px a 2×) y sus sombras
   * sueltas también salen opacas: la claridad separa a la bolita de su sombra (la sombra es oscura).
   */
  private fun lucerosVisibles(): List<Pair<Float, Float>> {
    val bmp = BitmapFactory.decodeFile("src/main/res/drawable-nodpi/fondo_luceros.webp")
    val w = bmp.width; val h = bmp.height
    val seen = BooleanArray(w * h)
    val out = mutableListOf<Pair<Float, Float>>()
    fun body(x: Int, y: Int): Boolean { val p = bmp.getPixel(x, y); return (p ushr 24) >= 128 && lum(p and 0xFFFFFF) >= 0.30 }
    for (y0 in 0 until h) for (x0 in 0 until w) {
      if (seen[y0 * w + x0] || !body(x0, y0)) continue
      val stack = ArrayDeque<Int>(); stack.add(y0 * w + x0); seen[y0 * w + x0] = true
      var n = 0; var sx = 0L; var sy = 0L
      while (stack.isNotEmpty()) {
        val i = stack.removeLast(); val x = i % w; val y = i / w
        n++; sx += x; sy += y
        for ((dx, dy) in listOf(1 to 0, -1 to 0, 0 to 1, 0 to -1)) {
          val nx = x + dx; val ny = y + dy
          if (nx in 0 until w && ny in 0 until h && !seen[ny * w + nx] && body(nx, ny)) { seen[ny * w + nx] = true; stack.add(ny * w + nx) }
        }
      }
      if (n >= 5) out += (sx.toFloat() / n / w) to (sy.toFloat() / n / h)
    }
    return out
  }

  @Test
  fun la_capa_tiene_entre_40_y_52_luceros_y_ninguno_pegado_a_otro() {
    val luceros = lucerosVisibles()
    assertTrue("luceros en la capa: ${luceros.size} (Tarea 122: 55 colocados, entre 40 y 52 visibles)", luceros.size in 40..52)
    var closest = Float.MAX_VALUE
    for (i in luceros.indices) for (j in i + 1 until luceros.size) {
      val dx = (luceros[i].first - luceros[j].first) * 720; val dy = (luceros[i].second - luceros[j].second) * 1200
      closest = minOf(closest, kotlin.math.sqrt(dx * dx + dy * dy))
    }
    // la colocación exige una separación de 4,5 radios (isla.py): ≥ 14 px a 2× entre dos luceros chicos; ninguno se toca (el más grande mide 8 px de radio)
    assertTrue("el par de luceros más cercano está a %.0f px (a 2×): se amontonan".format(closest), closest >= 14f)
  }
}
