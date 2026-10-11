package com.example

import androidx.compose.foundation.pager.PagerState
import com.example.ui.components.drawsLuceros
import com.example.ui.components.lucerosAlpha
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * Los luceros del fondo solo se ven en Hoy (Tarea 83): su opacidad sigue la posición del pager (1 en la página 0, 0 en las páginas 1 y 2, lineal entre medias) y, con opacidad 0, la capa no se dibuja.
 * Es lo mismo con «quitar animaciones», porque sigue al dedo y no es una animación. (El dibujo real no se puede capturar en Robolectric sin Roborazzi; se prueba la regla y se mira en el emulador.)
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [36])
class LucerosAlphaTest {
  private fun pager(page: Int, offset: Float = 0f) = PagerState(currentPage = page, currentPageOffsetFraction = offset) { 3 }

  @Test
  fun los_luceros_valen_1_en_Hoy_y_0_en_Juegos_y_Avance() {
    assertEquals("Hoy (página 0)", 1f, lucerosAlpha(pager(0)), 0f)
    assertEquals("Juegos (página 1)", 0f, lucerosAlpha(pager(1)), 0f)
    assertEquals("Avance (página 2)", 0f, lucerosAlpha(pager(2)), 0f)
  }

  @Test
  fun entre_Hoy_y_Juegos_siguen_al_dedo_en_linea_recta() {
    assertEquals("a un cuarto de camino hacia Juegos", 0.75f, lucerosAlpha(pager(0, 0.25f)), 1e-6f)
    assertEquals("a la mitad", 0.5f, lucerosAlpha(pager(0, 0.5f)), 1e-6f)
    assertEquals("pasada la mitad, la página actual ya es Juegos", 0.4f, lucerosAlpha(pager(1, -0.4f)), 1e-6f)
    assertEquals("de Juegos a Avance no vuelven", 0f, lucerosAlpha(pager(1, 0.4f)), 0f)
    assertEquals("tirar de Hoy hacia atrás (elástico) no pasa de 1", 1f, lucerosAlpha(pager(0, -0.3f)), 0f)
  }

  @Test
  fun con_opacidad_0_la_capa_de_luceros_no_se_dibuja() {
    // En las páginas 1 y 2 la opacidad es 0 y `lucerosLayer` solo llama a drawContent() si `drawsLuceros` da true.
    for (page in 1..2) assertFalse("página $page", drawsLuceros(lucerosAlpha(pager(page))))
    assertFalse("opacidad negativa (elástico)", drawsLuceros(-0.2f))
  }

  @Test
  fun con_opacidad_mayor_que_0_la_capa_de_luceros_se_dibuja() {
    assertTrue("Hoy", drawsLuceros(lucerosAlpha(pager(0))))
    assertTrue("a medio camino", drawsLuceros(lucerosAlpha(pager(0, 0.5f))))
    assertTrue("apenas visibles", drawsLuceros(0.01f))
  }
}
