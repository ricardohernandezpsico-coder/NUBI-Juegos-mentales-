package com.example

import com.example.ui.components.PlaceMotion
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import kotlin.math.abs

/** Los tiempos de lo que queda del toque a un edificio de la isla (Tarea 85; desde la Tarea 163 no hay lámina: se fueron el velo, el vuelo y el globo; la «mirada» de Nubi se borró en la Tarea 182), sin pantalla. */
class PlaceMotionTest {
  @Test
  fun los_tiempos_son_los_del_boceto() {
    assertEquals("Nubi se queda mirando ~650 ms antes de que se abra «Todos los juegos»", 650, PlaceMotion.LOOK_HOLD_MS)
    assertEquals(320, PlaceMotion.GLOW_MS)
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
