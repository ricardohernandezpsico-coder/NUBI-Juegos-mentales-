package com.example

import androidx.compose.ui.graphics.Color
import com.example.ui.components.PlaceMotion
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import kotlin.math.abs

/** Las curvas y los tiempos de la lámina del lugar (Tarea 85), sin pantalla. */
class PlaceMotionTest {
  @Test
  fun los_tiempos_son_los_del_boceto() {
    assertEquals(180, PlaceMotion.LOOK_FADE_MS)
    assertEquals(650, PlaceMotion.LOOK_HOLD_MS)
    assertEquals("el velo es un cambio de estado (escala única, Tarea 123)", 200, PlaceMotion.VEIL_MS)
    assertEquals("la lámina es un cambio de lugar: 400", 400, PlaceMotion.SHEET_MS)
    assertEquals("y sale al toque, sin espera", 0, PlaceMotion.SHEET_DELAY_MS)
    assertEquals(820, PlaceMotion.FLIGHT_MS)
    assertEquals(620, PlaceMotion.BACK_FLIGHT_MS)
    assertEquals(320, PlaceMotion.GLOBE_MS)
  }

  /** El velo de la lámina (Tarea 122) sobre el cielo «Espacio profundo»: no lo aclara (el de antes, `#211C45` al 50 %, era más claro que el cielo) y sigue apagando la crema de la isla. */
  @Test
  fun el_velo_no_aclara_el_cielo_y_apaga_la_crema_de_la_isla() {
    fun lum(c: Int): Double {
      fun ch(v: Int): Double { val x = v / 255.0; return if (x <= 0.03928) x / 12.92 else Math.pow((x + 0.055) / 1.055, 2.4) }
      return 0.2126 * ch((c shr 16) and 0xFF) + 0.7152 * ch((c shr 8) and 0xFF) + 0.0722 * ch(c and 0xFF)
    }
    fun over(bg: Int): Int {
      val v = PlaceMotion.VEIL; val a = v.alpha
      fun mix(shift: Int, vc: Float) = Math.round(vc * 255f * a + ((bg shr shift) and 0xFF) * (1f - a))
      return (mix(16, v.red) shl 16) or (mix(8, v.green) shl 8) or mix(0, v.blue)
    }
    val sky = 0x151539; val cream = 0xF6EBD3
    assertTrue("el velo cae al ~45 %", PlaceMotion.VEIL.alpha in 0.35f..0.55f)
    assertTrue("el velo no aclara el cielo (%.4f → %.4f)".format(lum(sky), lum(over(sky))), lum(over(sky)) <= lum(sky))
    assertTrue("el velo apaga la crema de la isla (de %.2f a %.2f)".format(lum(cream), lum(over(cream))), lum(over(cream)) <= 0.35 * lum(cream))
  }

  @Test
  fun nubi_se_corre_hacia_el_lugar_y_se_ladea_hacia_el_mismo_lado() {
    for (k in listOf("observatorio", "antena", "hangar")) {
      val l = PlaceMotion.look(k)
      assertTrue("$k a la izquierda", l.shift < 0f && l.tiltDeg < 0f)
    }
    val b = PlaceMotion.look("biblioteca")
    assertTrue(b.shift > 0f && b.tiltDeg > 0f)
    assertTrue("~4 % del ancho, entre 3 y 5 %", PlaceMotion.LOOKS.values.all { abs(it.shift) in 0.03f..0.05f })
    assertTrue("entre 4 y 6 grados", PlaceMotion.LOOKS.values.all { abs(it.tiltDeg) in 4f..6f })
    assertEquals(0f, PlaceMotion.look(null).shift, 0f)
  }

  @Test
  fun el_fundido_de_la_mirada_termina_antes_que_el_corrimiento() {
    assertEquals(0f, PlaceMotion.lookFade(0f), 0f)
    assertEquals(1f, PlaceMotion.lookFade(1f), 0f)
    assertEquals(1f, PlaceMotion.lookFade(180f / 260f), 1e-5f)
    assertTrue(PlaceMotion.lookFade(0.3f) in 0.1f..1f)
  }

  @Test
  fun el_vuelo_de_ida_hace_un_arco_hacia_arriba_y_se_posa_en_su_sitio() {
    val start = PlaceMotion.flight(0f, outbound = true)
    val mid = PlaceMotion.flight(0.35f, outbound = true)
    val end = PlaceMotion.flight(1f, outbound = true)
    assertEquals(0f, start.arcDp, 1e-4f)
    assertEquals(0f, start.tiltDeg, 1e-4f)
    assertEquals(0f, start.easedPosition, 1e-4f)
    assertEquals(-92f, mid.arcDp, 1e-3f)            // sube ~92 dp a un tercio del camino, como el boceto
    assertEquals(-10f, mid.tiltDeg, 1e-3f)
    assertEquals(0f, end.arcDp, 1e-4f)
    assertEquals(0f, end.tiltDeg, 1e-4f)
    assertEquals(1f, end.easedPosition, 1e-3f)
    assertTrue("la ida avanza sin retroceder", (0..20).map { PlaceMotion.flight(it / 20f, true).easedPosition }.zipWithNext().all { (a, b) -> b >= a - 1e-4f })
  }

  @Test
  fun el_vuelo_de_vuelta_tambien_hace_arco_y_termina_en_la_isla() {
    val mid = PlaceMotion.flight(0.6f, outbound = false)   // posición 0,6 de vuelta = tiempo 0,4 del arco
    assertEquals(-70f, mid.arcDp, 1e-3f)
    assertEquals(8f, mid.tiltDeg, 1e-3f)
    val end = PlaceMotion.flight(0f, outbound = false)
    assertEquals(0f, end.arcDp, 1e-4f)
    assertEquals(0f, end.easedPosition, 1e-4f)
    assertEquals(0f, PlaceMotion.flight(1f, outbound = false).arcDp, 1e-4f)
  }

  @Test
  fun el_muestreo_pasa_por_los_cuadros_clave_y_no_se_sale_de_rango() {
    val keys = PlaceMotion.OUT_ARC_DP
    for ((p, v) in keys) assertEquals(v, PlaceMotion.sample(keys, p), 1e-3f)
    assertEquals(0f, PlaceMotion.sample(keys, -1f), 0f)
    assertEquals(0f, PlaceMotion.sample(keys, 2f), 0f)
    assertTrue((0..100).all { PlaceMotion.sample(keys, it / 100f) in -92.01f..6.01f })
  }
}
