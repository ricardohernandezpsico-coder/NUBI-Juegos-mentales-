package com.example.data

import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Engranajes: Taller de reparación» (docs/diseno-engranajes.md §9): la lectura de la pantalla final y el cohete que se guarda. */
class EngranajesTest {
  @Test
  fun `la cabecera dice cuantas maquinas arreglaste y no inventa sin maquinas`() {
    assertEquals("Arreglaste 7 de 10 máquinas", Engranajes.headline(7, 10))
    assertEquals("Arreglaste 10 de 10 máquinas", Engranajes.headline(12, 10))   // nunca más que las jugadas
    assertNull(Engranajes.headline(null, 10))
    assertNull(Engranajes.headline(3, 0))
  }

  @Test
  fun `la marca es el porcentaje de maquinas arregladas`() {
    assertEquals(70f, Engranajes.mark(7, 10)!!, 1e-4f)
    assertEquals(100f, Engranajes.mark(12, 10)!!, 1e-4f)
    assertNull(Engranajes.mark(null, 10))
    assertNull(Engranajes.mark(3, 0))
  }

  @Test
  fun `la etapa mas alta se nombra con sus cinco grupos`() {
    assertEquals("Etapa más alta: 1 de 5 (motor y correas)", Engranajes.etapaLine(1))
    assertEquals("Etapa más alta: 2 de 5 (ramas)", Engranajes.etapaLine(2))
    assertEquals("Etapa más alta: 3 de 5 (carga y compuerta)", Engranajes.etapaLine(3))
    assertEquals("Etapa más alta: 4 de 5 (tres piezas)", Engranajes.etapaLine(4))
    assertEquals("Etapa más alta: 5 de 5 (dos llaves)", Engranajes.etapaLine(5))
    assertNull(Engranajes.etapaLine(0))
    assertNull(Engranajes.etapaLine(6))
    assertNull(Engranajes.etapaLine(null))
  }

  @Test
  fun `el cohete nunca despega incompleto y el final lo cuenta`() {
    assertEquals("Faltan 4 luces: tu cohete espera en el hangar", Engranajes.hangarLine(6))
    assertEquals("Falta 1 luz: tu cohete espera en el hangar", Engranajes.hangarLine(9))
    assertNull(Engranajes.hangarLine(0))
    assertNull(Engranajes.hangarLine(null))
    assertEquals("¡Despegó tu cohete n.º 3!", Engranajes.launchLine(1, 3))
    assertEquals("¡Despegaron 2 cohetes!", Engranajes.launchLine(2, 5))
    assertNull(Engranajes.launchLine(0, 3))
    assertEquals("1 cohete en órbita", Engranajes.orbitLine(1))
    assertEquals("4 cohetes en órbita", Engranajes.orbitLine(4))
    assertNull(Engranajes.orbitLine(0))
  }

  @Test
  fun `el cohete guardado se acota a 0 a 9 luces y no hay cohetes negativos`() {
    assertEquals(Engranajes.Rocket(0, 0), Engranajes.rocket(-3, -1))
    assertEquals(Engranajes.Rocket(9, 2), Engranajes.rocket(10, 2))     // 10 luces ya despegó: no se guarda completo
    assertEquals(Engranajes.Rocket(4, 7), Engranajes.rocket(4, 7))
  }

  @Test
  fun `el ritmo solo con tres o mas maquinas y el consejo solo si alguna fallo`() {
    assertEquals("Tu ritmo: 7,5 s por máquina", Engranajes.paceLine(7500, 10))
    assertNull(Engranajes.paceLine(7500, 2))
    assertNull(Engranajes.paceLine(0, 10))
    assertNull(Engranajes.paceLine(999_999, 10))
    val tip = Engranajes.tip(7, 10)!!
    assertTrue(tip.startsWith("Truco: antes de cambiar algo, mira qué piezas quedan después"))
    assertTrue(tip.contains("lo que tocas antes de una rama mueve todo lo que sigue"))
    assertNull(Engranajes.tip(10, 10))
    assertNull(Engranajes.tip(null, 10))
  }

  @Test
  fun `el juego esta registrado en Razonamiento, con su escalera de 12 y su medida`() {
    val g = GameRegistry.getById("engranajes")
    assertNotNull(g)
    assertEquals("Engranajes", g!!.title)
    assertEquals("RAZONAMIENTO", g.domain.name)
    assertEquals(12, Skill.ladder("engranajes").levels)
    assertNotNull(StarMeasures.defForGame("engranajes"))
    assertEquals("Engranajes", StarMeasures.gameNames["engranajes"])
    assertEquals(setOf("calculo", "acoplamiento", "aterrizaje", "engranajes"), GameRegistry.allGames.filter { it.domain.name == "RAZONAMIENTO" }.map { it.id }.toSet())
    assertEquals(19, GameRegistry.allGames.size)
  }

  @Test
  fun `el consejo entra en los de la pantalla final`() {
    val r = com.example.model.GamePlayResult(gameId = "engranajes", score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 1, engrEtapa = 3)
    val tips = ResultAdvice.tips(r)
    assertTrue(tips.any { it.startsWith("Antes de cambiar algo, mira qué piezas quedan después") && it.contains("lo que tocas antes de una rama mueve todo lo que sigue") })
    val perfect = r.copy(correctAnswers = 10, score = 100)
    assertTrue(ResultAdvice.tips(perfect).none { it.contains("lo que tocas antes de una rama") })
  }

  @Test
  fun `la medida del taller es nueva y el historial viejo de Engranajes no se lee ni se mezcla`() {
    assertEquals("taller", StarMeasures.defForGame("engranajes")!!.key)
    assertNull("la clave vieja ya no es una medida", StarMeasures.def("engranajes"))
    val now = 10L * 86_400_000L
    val old = (0 until 4).map { MeasurePoint(now - (4 - it) * 1000L, "engranajes", 50f + it) }
    assertNull("sin puntos del taller no se dice nada aunque haya historial viejo", StarMeasures.discover(old, now))
    assertNull(StarMeasures.latest(old, "engranajes"))
    val fresh = (0 until 3).map { MeasurePoint(now - (3 - it) * 1000L, "taller", 60f + it * 10f) }
    assertNotNull(StarMeasures.discover(old + fresh, now))
    assertEquals(80f, StarMeasures.latest(old + fresh, "engranajes")!!.second, 1e-4f)
  }
}
