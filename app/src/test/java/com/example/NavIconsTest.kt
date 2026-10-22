package com.example

import com.example.ui.components.NavIcon
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * Los íconos vectoriales de la barra y de la losa «Todos los juegos» (Tarea 158; `NavIcons.kt`): las rutas se leen y arman, el dibujo ENTERO (con su trazo) se ve de 28 dp o más y cabe en el disco crema de 48 dp, y el trazo no pasa de 2,5 dp (la regla de Opus: sin pasar de 2,5 dp
 * y con los huecos interiores de 3 dp o más; los huecos se ven en las capturas de `docs/previews/tarea-158.png`). Con la caja a 32 dp (como en la primera propuesta) el dibujo medía de 21 a 25 dp y no se leía: de ahí los 40 dp.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class NavIconsTest {
  @Test
  fun los_cuatro_vectores_se_arman_con_su_rejilla_de_48() {
    assertEquals(listOf("ISLA", "BARRAS", "PERSONA", "CASILLAS"), NavIcon.values().map { it.name })
    for (icon in NavIcon.values()) {
      val v = icon.vector
      assertEquals("${icon.name}: rejilla de 48", 48f, v.viewportWidth, 0f)
      assertEquals("${icon.name}: rejilla de 48", 48f, v.viewportHeight, 0f)
      assertTrue("${icon.name} tiene trazos", v.root.size >= 1)
    }
  }

  @Test
  fun el_dibujo_entero_de_cada_icono_se_ve_de_28_dp_o_mas_y_cabe_en_el_disco_de_48() {
    for (icon in NavIcon.values()) {
      val (w, h) = icon.visible(NavIcon.BOX_DP)
      assertTrue("${icon.name} mide ${"%.1f".format(w)} × ${"%.1f".format(h)} dp: el lado mayor debe ser ≥ 28 dp", maxOf(w, h) >= 28f)
      val reach = icon.reachDp(NavIcon.BOX_DP)
      assertTrue("${icon.name} cabe en el disco de 48 dp: lo más lejano del centro llega a ${"%.1f".format(reach)} dp (el disco mide 24 de radio; se pide ≤ 21,5 para que no toque su canto)", reach <= 21.5f)
    }
  }

  @Test
  fun el_trazo_mide_2_5_dp_o_menos() {
    val strokeDp = 3f * NavIcon.BOX_DP / 48f
    assertTrue("el trazo de 3 unidades mide $strokeDp dp", strokeDp <= 2.5f + 1e-3f)
  }
}
