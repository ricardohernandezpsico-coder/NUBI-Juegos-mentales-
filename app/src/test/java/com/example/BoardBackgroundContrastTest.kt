package com.example

import android.graphics.BitmapFactory
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import kotlin.math.pow

/**
 * El fondo B en dos capas (Tarea 83): la lámina azul marino SIN luceros (`fondo_lamina`, la que va siempre detrás del pager) y los luceros con su sombra (`fondo_luceros`, que solo se ven en Hoy).
 * Se miden las imágenes que entran a la app: el saludo se lee sobre la lámina con 4,5:1 o más (WCAG AA; medido el 11-oct sobre el B entero: crema 8,3:1 y sol 6,7:1), la lámina no tiene ningún lucero y los luceros no caen en las zonas
 * que `tools/isla/isla.py` (`ZONAS_HOY`) deja libres para el texto que va directo sobre el fondo de Hoy.
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
  fun la_lamina_no_tiene_ningun_lucero() {
    val bmp = BitmapFactory.decodeFile("src/main/res/drawable-nodpi/fondo_lamina.webp")
    var brightest = 0.0
    for (y in 0 until bmp.height step 2) for (x in 0 until bmp.width step 2) brightest = maxOf(brightest, lum(bmp.getPixel(x, y) and 0xFFFFFF))
    assertTrue("el píxel más claro de la lámina debe ser de fondo, no un lucero (luminancia %.2f)".format(brightest), brightest < 0.12)
  }

  // Mismos valores que ZONAS_HOY de tools/isla/isla.py (x0, y0, x1, y1 en fracción de la imagen, y hacia abajo).
  private val zonasHoy = listOf(
    floatArrayOf(0.0f, 0.07f, 1.0f, 0.28f),   // encabezado: saludo, fecha con la meta, farolitos y racha
    floatArrayOf(0.0f, 0.46f, 1.0f, 0.93f)    // «Tu ruta de hoy», los avisos bajo el botón y el enlace del punto de partida
  )

  @Test
  fun los_luceros_no_caen_en_las_zonas_sin_luceros_de_Hoy() {
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
}
