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
 * El saludo de Hoy sobre el fondo B (el tablero azul marino de la isla, Tarea 82): el contraste de cada color de texto contra el PEOR píxel de la zona del saludo (x 4-70 %, y 8-22 % de la imagen, sin luceros por diseño)
 * tiene que ser de 4,5:1 o más (WCAG AA). Se mide sobre la imagen que entra a la app, no sobre la maqueta. Medido el 11-oct: crema 8,3:1 y sol 6,7:1.
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
    val bmp = BitmapFactory.decodeFile("src/main/res/drawable-nodpi/fondo_tablero.webp")
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
  fun la_zona_del_saludo_no_tiene_luceros() {
    val bmp = BitmapFactory.decodeFile("src/main/res/drawable-nodpi/fondo_tablero.webp")
    var brightest = 0.0
    for (y in (bmp.height * 0.08).toInt() until (bmp.height * 0.22).toInt() step 2) for (x in (bmp.width * 0.04).toInt() until (bmp.width * 0.70).toInt() step 2) brightest = maxOf(brightest, lum(bmp.getPixel(x, y) and 0xFFFFFF))
    assertTrue("el píxel más claro de la zona del saludo debe ser de fondo, no un lucero (luminancia %.2f)".format(brightest), brightest < 0.12)
  }
}
