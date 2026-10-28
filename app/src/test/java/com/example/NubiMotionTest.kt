package com.example

import com.example.ui.components.NubiMotion
import com.example.ui.components.PlaceMotion
import com.example.ui.components.NubiHop
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * La escala de movimiento ÚNICA (Tarea 123, `NubiMotion`): rápido para lo que responde al dedo, medio para cambiar de lugar y largo solo para celebrar. Esta prueba fija la tabla y que lo que ya existía (el velo y la lámina de un lugar, la barra,
 * el saltito de Nubi) salga de ella, para que nadie vuelva a inventar un tiempo suelto.
 */
class NubiMotionTest {
  @Test
  fun la_tabla_es_la_de_Fable() {
    assertEquals("apretar", 80, NubiMotion.PRESS_MS)
    assertEquals("soltar: la plasticina recupera su forma más lento de lo que se hunde", 140, NubiMotion.RELEASE_MS)
    assertTrue("soltar es más lento que apretar", NubiMotion.RELEASE_MS > NubiMotion.PRESS_MS)
    assertEquals(150, NubiMotion.STATE_FAST_MS)
    assertEquals(200, NubiMotion.STATE_MS)
    assertEquals("pestaña", 300, NubiMotion.TAB_MS)
    assertEquals("lámina", 400, NubiMotion.SHEET_MS)
    assertEquals("pulso de celebración", 600, NubiMotion.CELEBRATE_MS)
    assertEquals("saltito", 900, NubiMotion.CELEBRATE_LONG_MS)
    assertEquals(150, NubiMotion.REDUCED_FADE_MS)
  }

  @Test
  fun el_fundido_entre_pestanas_no_vecinas_suma_el_tiempo_de_una_pestana() {
    assertEquals("sale la página de ahora (Tarea 184)", 90, NubiMotion.TAB_FADE_OUT_MS)
    assertEquals("entra la nueva", 210, NubiMotion.TAB_FADE_IN_MS)
    assertEquals("90 + 210 = lo que dura una pestaña", NubiMotion.TAB_MS, NubiMotion.TAB_FADE_OUT_MS + NubiMotion.TAB_FADE_IN_MS)
    assertEquals("entra con una escala mínima de 0,96", 0.96f, NubiMotion.TAB_FADE_MIN_SCALE, 0f)
  }

  @Test
  fun cada_clase_cae_en_su_rango() {
    assertTrue("estado 150-200", NubiMotion.STATE_FAST_MS in 150..200 && NubiMotion.STATE_MS in 150..200)
    assertTrue("lugar 300-400", NubiMotion.TAB_MS in 300..400 && NubiMotion.SHEET_MS in 300..400)
    assertTrue("celebración 600-900", NubiMotion.CELEBRATE_MS in 600..900 && NubiMotion.CELEBRATE_LONG_MS in 600..900)
  }

  @Test
  fun lo_que_ya_existia_sale_de_la_escala() {
    assertEquals(NubiMotion.STATE_FAST_MS, PlaceMotion.TAB_FADE_MS)
    assertEquals(NubiMotion.REDUCED_FADE_MS, PlaceMotion.REDUCED_FADE_MS)
    assertEquals(NubiMotion.CELEBRATE_LONG_MS, NubiHop.JUMP_MS)
  }

  @Test
  fun la_curva_enfatizada_arranca_rapido_y_frena_suave() {
    val e = NubiMotion.Emphasized
    assertEquals(0f, e.transform(0f), 1e-4f)
    assertEquals(1f, e.transform(1f), 1e-4f)
    assertTrue("a un tercio del tiempo ya recorrió más de la mitad", e.transform(0.33f) > 0.5f)
    assertTrue("y no retrocede", (1..100).map { e.transform(it / 100f) }.zipWithNext().all { (a, b) -> b >= a - 1e-4f })
  }
}
