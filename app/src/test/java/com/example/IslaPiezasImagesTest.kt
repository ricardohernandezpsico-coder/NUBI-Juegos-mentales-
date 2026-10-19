package com.example

import android.graphics.BitmapFactory
import com.example.data.IslaCapa
import com.example.data.IslaLayout
import com.example.data.IslaPieza
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File
import kotlin.math.hypot

/**
 * Las diez capas de las piezas de la isla (Tarea 136; `tools/isla/piezas_app.py`, las que Ricardo aprobó en `docs/previews/isla-piezas.png`) medidas sobre sus píxeles, con la isla de 390 dp de ancho de un teléfono de 393 dp: ninguna pisa un edificio (las de SUELO van DETRÁS de ellos),
 * una zona tocable de los edificios (48 dp en torno de su centro: las de suelo, nada; los banderines de AIRE nacen de los techos y rozan el borde, a lo más el 15 % de una zona), una farola de la ruta (14 dp en torno de su tope) ni la silueta de Nubi (con 3 dp de margen),
 * y las piezas principales se ven a distancia de teléfono (lado menor mínimo: lección del 16-oct). Las capas son decorativas y van debajo de los toques, así que no bloquean ningún botón.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [36])
class IslaPiezasImagesTest {
  private val dir = "src/main/res/drawable-nodpi/"
  private val layout = IslaLayout.parse(File("src/main/res/raw/isla.json").readText())
  private val w = layout.widthPx
  private val h = layout.heightPx
  private val pxPerDp = w / 390f

  private class Img(val w: Int, val h: Int, val argb: IntArray) { fun a(x: Int, y: Int) = argb[y * w + x] ushr 24 }

  private fun load(name: String): Img {
    val bmp = BitmapFactory.decodeFile("$dir$name.webp") ?: throw AssertionError("no se pudo decodificar $name")
    val px = IntArray(bmp.width * bmp.height)
    bmp.getPixels(px, 0, bmp.width, 0, 0, bmp.width, bmp.height)
    return Img(bmp.width, bmp.height, px)
  }

  private fun mask(img: Img, threshold: Int = 128): BooleanArray = BooleanArray(w * h) { img.argb[it] ushr 24 >= threshold }

  /** La caja (en px del recorte) de lo no transparente de [m]: x0, y0, x1, y1. */
  private fun bounds(m: BooleanArray): IntArray {
    var x0 = w; var y0 = h; var x1 = -1; var y1 = -1
    for (y in 0 until h) for (x in 0 until w) if (m[y * w + x]) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y }
    return intArrayOf(x0, y0, x1 + 1, y1 + 1)
  }

  private val pieces = IslaPieza.entries.associateWith { mask(load("isla_deco_${it.id}")) }
  private val edificios = mask(load("isla_edificios"))

  private fun zone(cx: Float, cy: Float, halfDp: Float) = BooleanArray(w * h).also { z ->
    val r = halfDp * pxPerDp
    for (y in maxOf(0, (cy * h - r).toInt())..minOf(h - 1, (cy * h + r).toInt())) for (x in maxOf(0, (cx * w - r).toInt())..minOf(w - 1, (cx * w + r).toInt())) z[y * w + x] = true
  }

  private fun overlap(a: BooleanArray, b: BooleanArray): Int { var n = 0; for (i in a.indices) if (a[i] && b[i]) n++; return n }

  @Test
  fun las_capas_son_del_mismo_recorte_que_la_base() {
    for (p in IslaPieza.entries) {
      val img = load("isla_deco_${p.id}")
      assertEquals("${p.id} mide lo mismo que isla_base", w to h, img.w to img.h)
      assertTrue("${p.id} no está vacía", pieces.getValue(p).any { it })
    }
  }

  @Test
  fun ninguna_pieza_de_suelo_ni_de_aire_pisa_una_zona_tocable_mas_de_lo_permitido() {
    val zonas = layout.places.associate { it.key to zone(it.center.x, it.center.y, 24f) }
    for ((p, m) in pieces) for ((lugar, z) in zonas) {
      val n = overlap(m, z); val pct = n * 100f / z.count { it }
      if (p.capa == IslaCapa.SUELO) assertEquals("${p.id} (suelo) pisa la zona tocable de $lugar en $n px", 0, n)
      else assertTrue("${p.id} (aire) cubre el %.0f %% de la zona tocable de %s (tope 15 %%)".format(pct, lugar), pct <= 15f)
    }
  }

  @Test
  fun las_piezas_de_suelo_van_detras_de_los_edificios_y_ninguna_tapa_una_farola_de_la_ruta() {
    for ((p, m) in pieces) {
      if (p.capa == IslaCapa.SUELO) assertEquals("${p.id} (suelo) tiene ${overlap(m, edificios)} px sobre un edificio: se vería metida en él", 0, overlap(m, edificios))
      for ((i, l) in layout.lamps.withIndex()) assertEquals("${p.id} pisa la farola $i de la ruta", 0, overlap(m, BooleanArray(w * h).also { z -> disk(z, l.x, l.y, 14f) }))
    }
  }

  /** Pinta en [z] el disco de [radiusDp] en torno de (cx, cy) (0-1 del recorte). */
  private fun disk(z: BooleanArray, cx: Float, cy: Float, radiusDp: Float) {
    val r = radiusDp * pxPerDp
    for (y in maxOf(0, (cy * h - r).toInt())..minOf(h - 1, (cy * h + r).toInt())) for (x in maxOf(0, (cx * w - r).toInt())..minOf(w - 1, (cx * w + r).toInt())) if (hypot(x - cx * w, y - cy * h) <= r) z[y * w + x] = true
  }

  @Test
  fun ninguna_pieza_pisa_la_silueta_de_Nubi_con_3_dp_de_margen() {
    val nubi = load("isla_nubi_hola")
    val x0 = (layout.nubiTopLeft.x * w).toInt(); val y0 = (layout.nubiTopLeft.y * h).toInt()
    val bw = ((layout.nubiBottomRight.x - layout.nubiTopLeft.x) * w).toInt(); val bh = ((layout.nubiBottomRight.y - layout.nubiTopLeft.y) * h).toInt()
    val silueta = BooleanArray(w * h)
    for (y in 0 until bh) for (x in 0 until bw) {
      val gx = x0 + x; val gy = y0 + y
      if (gx !in 0 until w || gy !in 0 until h) continue
      if (nubi.a(minOf(nubi.w - 1, x * nubi.w / bw), minOf(nubi.h - 1, y * nubi.h / bh)) > 16) silueta[gy * w + gx] = true
    }
    val r = (3 * pxPerDp).toInt()
    val gorda = BooleanArray(w * h)
    for (y in 0 until h) for (x in 0 until w) if (silueta[y * w + x]) for (dy in -r..r) for (dx in -r..r) if (dx * dx + dy * dy <= r * r) { val yy = y + dy; val xx = x + dx; if (yy in 0 until h && xx in 0 until w) gorda[yy * w + xx] = true }
    for ((p, m) in pieces) assertEquals("${p.id} pisa la silueta de Nubi (con 3 dp de margen)", 0, overlap(m, gorda))
  }

  @Test
  fun las_piezas_principales_se_ven_a_distancia_de_telefono() {
    // Lado menor mínimo en dp (isla de 390 dp): lo que pide la spec (34 dp para las principales) menos la tolerancia de las medidas de la lámina; los detalles sobre una pieza ya visible (frutos, banderines, el farol) no se miden solos.
    val min = mapOf(IslaPieza.C1 to 34, IslaPieza.C2 to 40, IslaPieza.C3 to 34, IslaPieza.VO1 to 22, IslaPieza.VO2 to 28)
    for ((p, lado) in min) {
      val b = bounds(pieces.getValue(p))
      val dp = minOf(b[2] - b[0], b[3] - b[1]) / pxPerDp
      assertTrue("${p.id} mide %.0f dp de lado menor (mínimo %d)".format(dp, lado), dp >= lado)
    }
  }

  @Test
  fun las_diez_capas_pesan_menos_de_200_KB() {
    val kb = IslaPieza.entries.sumOf { File("${dir}isla_deco_${it.id}.webp").length() } / 1024
    assertTrue("las diez capas pesan $kb KB (tope 200)", kb <= 200)
  }
}
