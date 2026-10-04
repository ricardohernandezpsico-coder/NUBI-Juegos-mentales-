package com.example.data

import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Carga exacta»: lo que se lee al final de la partida (docs/diseno-carga-exacta.md §6). */
class CargaTest {
  @Test
  fun `el titular dice cuantas cargas salieron sin pista`() {
    assertEquals("Lograste 5 de 8 sin pista", Carga.headline(5, 8))
    assertEquals("Lograste 8 de 8 sin pista", Carga.headline(12, 8))   // nunca pasa del total
    assertNull(Carga.headline(null, 8))
    assertNull(Carga.headline(3, 0))
  }

  @Test
  fun `la marca es el porcentaje de cargas sin pista`() {
    assertEquals(62.5f, Carga.mark(5, 8)!!, 1e-4f)
    assertEquals(100f, Carga.mark(8, 8)!!, 1e-4f)
    assertEquals(0f, Carga.mark(0, 4)!!, 1e-4f)
    assertNull(Carga.mark(null, 8))
    assertNull(Carga.mark(2, 0))
  }

  @Test
  fun `el desglose omite lo que vale cero`() {
    assertEquals("5 sin pista · 2 con pista · 1 para otra vez", Carga.breakdown(5, 2, 8))
    assertEquals("8 sin pista", Carga.breakdown(8, 0, 8))
    assertEquals("1 sin pista · 1 con pista", Carga.breakdown(1, 1, 2))
    assertEquals("3 para otra vez", Carga.breakdown(0, 0, 3))
    assertNull(Carga.breakdown(null, 1, 8))
  }

  @Test
  fun `el tiempo y los caminos cortos solo se muestran con 3 cargas sin pista o mas`() {
    assertEquals("Tardaste 14 s por carga, en promedio, en las que lograste sin pista", Carga.speedLine(14_200, 4))
    assertNull(Carga.speedLine(14_200, 2))
    assertNull(Carga.speedLine(-1, 5))
    assertNull(Carga.speedLine(900_000, 5))
    assertEquals("Camino corto en 3 de esas 5", Carga.shortLine(3, 5))
    assertEquals("Camino corto en 5 de esas 5", Carga.shortLine(9, 5))
    assertNull(Carga.shortLine(1, 2))
    assertNull(Carga.shortLine(null, 5))
  }

  @Test
  fun `el consejo aparece solo si alguna carga necesito pista o quedo para otra vez`() {
    assertNull(Carga.tip(8, 8, 3))
    assertNull(Carga.tip(null, 8, 3))
    val tip = ResultAdvice.tipOf(Carga.tip(5, 8, 3)!!)
    assertEquals("Mira primero si multiplicar dos celdas te deja cerca de la carga; después ajusta sumando o restando.", tip)
    // sin multiplicaciones todavía: otro truco, que no las nombra
    val low = ResultAdvice.tipOf(Carga.tip(5, 8, 1)!!)!!
    assertTrue(!low.contains("multiplicar"))
  }

  @Test
  fun `el juego se llama Carga exacta y conserva el id calculo`() {
    val g = GameRegistry.getById("calculo")
    assertNotNull(g)
    assertEquals("Carga exacta", g!!.title)
    assertTrue(GameRegistry.allGames.none { it.title.contains("Cálculo Sereno") || it.instruction.contains("burbuja") })
    assertEquals(5, Skill.ladder("calculo").levels)
    assertNotNull(StarMeasures.defForGame("calculo"))
    assertEquals("Carga exacta", StarMeasures.gameNames["calculo"])
  }
}
