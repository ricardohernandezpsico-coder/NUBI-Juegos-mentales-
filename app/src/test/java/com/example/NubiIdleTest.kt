package com.example

import com.example.ui.components.NubiIdle
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import kotlin.random.Random

/**
 * El reposo de Nubi (Tarea 148, P1): lógica pura de cuánto flota, cuándo parpadea y cuánto se achica su sombra.
 * Lo que se ve (Nubi sube y baja, los ojos se cierran) lo miran `NubiIdleScreenshotTest` y la hoja `docs/previews/tarea-148.png`.
 */
class NubiIdleTest {
  @Test
  fun `flota seis dp hacia arriba y seis hacia abajo en un seno de 4,5 segundos`() {
    assertEquals(6f, NubiIdle.FLOAT_DP, 0f)
    assertEquals(4500L, NubiIdle.FLOAT_MS)
    assertEquals("empieza en reposo", 0f, NubiIdle.floatDp(0), 1e-4f)
    assertEquals("a un cuarto del período, arriba", 6f, NubiIdle.floatDp(1125), 1e-3f)
    assertEquals("a la mitad, de vuelta", 0f, NubiIdle.floatDp(2250), 1e-3f)
    assertEquals("a tres cuartos, abajo", -6f, NubiIdle.floatDp(3375), 1e-3f)
    assertEquals("y el período se repite", NubiIdle.floatDp(700), NubiIdle.floatDp(700 + 4500), 1e-3f)
    for (t in 0L..9000L step 50L) assertTrue("nunca pasa de ±6 dp (t=$t)", NubiIdle.floatDp(t) in -6.001f..6.001f)
  }

  @Test
  fun `el parpadeo es un fundido de 120 ms y espera entre 4 y 7 segundos`() {
    assertEquals(120, NubiIdle.BLINK_MS)
    assertEquals(4000L, NubiIdle.MIN_WAIT_MS)
    assertEquals(7000L, NubiIdle.MAX_WAIT_MS)
    val random = Random(148)
    val waits = List(500) { NubiIdle.waitMs(random) }
    assertTrue("toda espera está entre 4 y 7 s: $waits", waits.all { it in 4000L..7000L })
    assertTrue("no espera siempre lo mismo", waits.toSet().size > 50)
    assertTrue("usa todo el rango, no solo una punta", waits.min() < 4500L && waits.max() > 6500L)
  }

  @Test
  fun `uno de cada cinco parpadeos es doble`() {
    val doubles = (1..20).filter { NubiIdle.isDouble(it) }
    assertEquals(listOf(5, 10, 15, 20), doubles)
    assertFalse("el cero no cuenta", NubiIdle.isDouble(0))
    assertTrue("el segundo parpadeo de uno doble llega pronto, no es otra espera", NubiIdle.DOUBLE_GAP_MS < 300L)
  }

  @Test
  fun `la sombra se achica y se aclara en el punto alto como la de la isla`() {
    assertEquals(0f, NubiIdle.height01(-6f), 1e-4f)
    assertEquals(0.5f, NubiIdle.height01(0f), 1e-4f)
    assertEquals(1f, NubiIdle.height01(6f), 1e-4f)
    assertEquals("fuera de rango se recorta", 1f, NubiIdle.height01(40f), 0f)
    assertEquals("abajo, la sombra entera", 1f, NubiIdle.shadowSize(0f), 1e-4f)
    assertEquals("arriba, al 85 % de su ancho", 0.85f, NubiIdle.shadowSize(1f), 1e-4f)
    assertEquals("abajo, toda su opacidad", 1f, NubiIdle.shadowAlpha(0f), 1e-4f)
    assertEquals("arriba, al 70 % de su opacidad", 0.7f, NubiIdle.shadowAlpha(1f), 1e-4f)
  }

  @Test
  fun `el vaiven se apaga suave cuando Nubi salta o mira un lugar y no se hace con tirones`() {
    assertEquals("un cuarto de segundo para apagarse o volver", 250L, NubiIdle.CALM_MS)
    assertTrue("menos que el período entero: no tarda en volver al reposo", NubiIdle.CALM_MS < NubiIdle.FLOAT_MS / 10)
  }
}
