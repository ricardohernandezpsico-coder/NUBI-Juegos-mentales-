package com.example

import com.example.data.IslaPieza
import com.example.data.PiezasPlan
import com.example.ui.components.NubiMotion
import com.example.ui.components.PieceFx
import kotlinx.coroutines.runBlocking
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * La entrada de las piezas nuevas de la isla (Tarea 136): la matemática del alfa y la escala, que las piezas fuera del plan valen 1 (puestas), que las del plan empiezan en 0 y que con «quitar animaciones» nacen puestas y `run()` no hace nada.
 */
class PieceFxTest {
  private val plan = PiezasPlan(listOf(IslaPieza.C1, IslaPieza.V1))

  @Test
  fun el_alfa_no_pasa_de_1_aunque_el_resorte_si() {
    assertEquals(0f, PieceFx.alpha(0f), 0f)
    assertEquals(1f, PieceFx.alpha(1f), 0f)
    assertEquals("el resorte pasa un poco de 1: el alfa no", 1f, PieceFx.alpha(1.04f), 0f)
    assertEquals(0f, PieceFx.alpha(-0.1f), 0f)
  }

  @Test
  fun la_escala_va_de_0_9_a_1() {
    assertEquals(0.9f, PieceFx.scale(0f), 1e-6f)
    assertEquals(1f, PieceFx.scale(1f), 1e-6f)
    assertTrue("sin saltos bruscos a mitad de camino", PieceFx.scale(0.5f) in 0.9f..1f)
  }

  @Test
  fun las_piezas_del_plan_empiezan_sin_aparecer_y_las_demas_estan_puestas() {
    val fx = PieceFx(plan)
    assertEquals(0f, fx.progress("c1"), 0f)
    assertEquals(0f, fx.progress("v1"), 0f)
    assertEquals("una pieza que no está en el plan ya está puesta", 1f, fx.progress("c3"), 0f)
    assertEquals(1f, PieceFx().progress("c1"), 0f)
  }

  @Test
  fun con_quitar_animaciones_nacen_puestas_y_run_no_hace_nada() {
    val fx = PieceFx(plan, reduceMotion = true)
    assertEquals(1f, fx.progress("c1"), 0f)
    assertEquals(1f, fx.progress("v1"), 0f)
    runBlocking { fx.run() }                                            // vuelve enseguida
    assertEquals(1f, fx.progress("c1"), 0f)
  }

  @Test
  fun un_plan_vacio_no_anima_nada() {
    runBlocking { PieceFx(PiezasPlan.NONE).run() }
  }

  @Test
  fun la_entrada_dura_lo_que_celebrar_y_las_piezas_entran_de_a_una() {
    assertEquals(600, NubiMotion.CELEBRATE_MS)
    assertEquals("la misma separación que las farolas de la ruta", com.example.ui.components.RouteMotion.STAGGER_MS.toLong(), PieceFx.STAGGER_MS)
  }
}
