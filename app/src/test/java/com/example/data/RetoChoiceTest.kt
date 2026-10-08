package com.example.data

import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Sin reloj / Contra el reloj» por juego (ficha de Juegos, 6-oct): la elección que vale, lo que se guarda y cómo se dice. Lógica pura. */
class RetoChoiceTest {
  @Test
  fun `sin eleccion propia vale el ajuste viejo de Ajustes y la eleccion propia gana`() {
    assertFalse(RetoChoice.resolve("radar", null, initial = false))
    assertTrue("migración: quien tenía el Reto activado en Ajustes lo conserva en cada juego", RetoChoice.resolve("radar", null, initial = true))
    assertFalse("lo que se eligió en la ficha pesa más que el ajuste viejo", RetoChoice.resolve("radar", false, initial = true))
    assertTrue(RetoChoice.resolve("radar", true, initial = false))
  }

  @Test
  fun `se guarda por juego y solo las claves reto_ con un si o no se leen`() {
    assertEquals("reto_bodega", RetoChoice.key("bodega"))
    val all: Map<String, Any?> = mapOf("reto_bodega" to true, "reto_radar" to false, "education" to "UNIVERSITARIA", "reto_" to true, "reto_calculo" to "si", "goals" to "A;B")
    assertEquals(mapOf("bodega" to true, "radar" to false), RetoChoice.decode(all))
    assertEquals(emptyMap<String, Boolean>(), RetoChoice.decode(emptyMap<String, Any?>()))
  }

  @Test
  fun `todos los juegos tienen Reto menos La estacion de correo, y cada uno dice su duracion`() {
    assertEquals(setOf("correo"), RetoChoice.WITHOUT_RETO)
    for (g in GameRegistry.allGames) {
      if (g.id in RetoChoice.WITHOUT_RETO) {
        assertFalse("${g.id} no debería ofrecer Reto", RetoChoice.supports(g.id))
        assertNull("${g.id} no tiene duración de Reto", RetoChoice.seconds(g.id))
        continue
      }
      assertTrue("${g.id} debería tener Reto (o estar en WITHOUT_RETO)", RetoChoice.supports(g.id))
      assertNotNull("${g.id} no tiene duración de Reto", RetoChoice.seconds(g.id))
    }
    assertEquals("Constelaciones dura 3 min", 180, RetoChoice.seconds("parejas"))
    assertEquals("Contra el reloj (3 min)", RetoChoice.timedLabel("parejas"))
  }

  @Test
  fun `la duracion se dice en minutos`() {
    assertEquals("1 min", RetoChoice.durationText(60))
    assertEquals("1 min 30 s", RetoChoice.durationText(90))
    assertEquals("2 min", RetoChoice.durationText(120))
    assertEquals("3 min", RetoChoice.durationText(180))
    assertEquals("45 s", RetoChoice.durationText(45))
    assertEquals("Contra el reloj (2 min)", RetoChoice.timedLabel("bodega"))
    assertEquals("Sin reloj", RetoChoice.UNTIMED_LABEL)
  }
}
