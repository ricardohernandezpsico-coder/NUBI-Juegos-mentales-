package com.example

import com.example.ui.components.PlaceMotion
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import kotlin.math.abs

/** Los tiempos y la mirada de Nubi cuando se toca un edificio de la isla (Tarea 85; desde la Tarea 163 no hay lámina: se fueron el velo, el vuelo y el globo), sin pantalla. */
class PlaceMotionTest {
  @Test
  fun los_tiempos_son_los_del_boceto() {
    assertEquals(180, PlaceMotion.LOOK_FADE_MS)
    assertEquals(260, PlaceMotion.LOOK_SHIFT_MS)
    assertEquals("Nubi se queda mirando ~650 ms antes de que se abra «Todos los juegos»", 650, PlaceMotion.LOOK_HOLD_MS)
    assertEquals(320, PlaceMotion.GLOW_MS)
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
  fun el_muestreo_pasa_por_los_cuadros_clave_y_no_se_sale_de_rango() {
    val keys = listOf(0f to 0f, 0.5f to 10f, 1f to 4f)                    // cuadros clave: progreso → valor
    assertEquals(0f, PlaceMotion.sample(keys, 0f), 1e-5f)
    assertEquals(10f, PlaceMotion.sample(keys, 0.5f), 1e-5f)
    assertEquals(4f, PlaceMotion.sample(keys, 1f), 1e-5f)
    assertEquals("fuera de rango se acota", 4f, PlaceMotion.sample(keys, 7f), 1e-5f)
    assertEquals(0f, PlaceMotion.sample(keys, -3f), 1e-5f)
    assertTrue("entre dos cuadros queda entre sus valores", PlaceMotion.sample(keys, 0.25f) in 0f..10f)
  }
}
