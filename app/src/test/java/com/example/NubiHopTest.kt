package com.example

import com.example.ui.components.NubiHop
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * El saltito de Nubi (Tarea 88; `NubiClay(celebrate = true)`): dos arcos de 12 dp en 900 ms con un giro leve; Nubi vuelve a su sitio. Matemática pura. (El pulso de las farolas de la ruta, las chispas y `RouteFx` se retiraron en la Tarea 168.)
 */
class NubiHopTest {
  @Test
  fun el_saltito_son_dos_arcos_de_12_dp_en_900_ms_con_un_giro_leve() {
    assertEquals(900, NubiHop.JUMP_MS)
    assertEquals(12f, NubiHop.JUMP_DP, 0f)
    assertEquals("en su sitio al empezar", 0f, NubiHop.jumpLiftDp(0f), 1e-4f)
    assertEquals("primer salto: arriba", 12f, NubiHop.jumpLiftDp(0.25f), 1e-3f)
    assertEquals("a la mitad vuelve a pisar", 0f, NubiHop.jumpLiftDp(0.5f), 1e-3f)
    assertEquals("segundo salto: arriba", 12f, NubiHop.jumpLiftDp(0.75f), 1e-3f)
    assertEquals("en su sitio al terminar", 0f, NubiHop.jumpLiftDp(1f), 1e-3f)
    assertTrue("nunca pasa de 12 dp ni baja del suelo", (0..200).all { NubiHop.jumpLiftDp(it / 200f) in 0f..12.0001f })
    assertTrue("gira a un lado en el primer salto y al otro en el segundo", NubiHop.jumpTiltDeg(0.25f) > 0f && NubiHop.jumpTiltDeg(0.75f) < 0f)
    assertTrue("leve: menos de 8 grados", (0..200).all { Math.abs(NubiHop.jumpTiltDeg(it / 200f)) < 8f })
    assertEquals("recto al empezar y al terminar", 0f, NubiHop.jumpTiltDeg(1f), 1e-3f)
  }

  @Test
  fun fuera_de_rango_se_acota() {
    assertEquals(0f, NubiHop.jumpLiftDp(-3f), 1e-4f)
    assertEquals(0f, NubiHop.jumpLiftDp(7f), 1e-3f)
    assertEquals(0f, NubiHop.jumpTiltDeg(7f), 1e-3f)
  }
}
