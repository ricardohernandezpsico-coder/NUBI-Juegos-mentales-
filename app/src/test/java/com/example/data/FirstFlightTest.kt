package com.example.data

import com.example.model.AgeBand
import com.example.model.DomainType
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Primer vuelo con Nubi»: los pasos, «Continuar» y «Atrás», lo que falta y el punto de partida (lógica pura). */
class FirstFlightTest {
  private fun full(step: FlightStep = FlightStep.HOLA) = FlightState(FlightMode.FULL, step)

  /** Sigue «Continuar» desde [start] hasta el final (sin jugar: los pasos de juego solo se cruzan con next). */
  private fun walk(start: FlightState): List<FlightStep> {
    val seen = mutableListOf(start.step)
    var s = start
    while (true) {
      s = FirstFlight.next(s) ?: break
      seen += s.step
    }
    return seen
  }

  @Test
  fun `el recorrido completo tiene los 19 pasos en el orden aprobado`() {
    assertEquals(
      listOf(
        "HOLA", "NOMBRE", "EDAD", "JUEGO_1", "TARJETA_1", "METAS", "DATO", "JUEGO_2", "TARJETA_2", "ENFOQUE", "COLOR",
        "JUEGO_3", "TARJETA_3", "DIAS", "JUEGO_4", "TARJETA_4", "PUNTO", "AVISO", "CAMINO"
      ),
      FirstFlight.steps(FlightMode.FULL).map { it.name }
    )
  }

  @Test
  fun `antes del primer juego solo van el nombre y la edad`() {
    val before = FirstFlight.steps(FlightMode.FULL).takeWhile { !it.isGame }
    assertEquals(listOf(FlightStep.HOLA, FlightStep.NOMBRE, FlightStep.EDAD), before)
  }

  @Test
  fun `la evaluacion repetida es solo la parte de juegos, sin preguntas`() {
    val steps = FirstFlight.steps(FlightMode.GAMES)
    assertEquals(FlightStep.JUEGO_1, steps.first())
    assertEquals(FlightStep.PUNTO, steps.last())
    val questions = setOf(FlightStep.NOMBRE, FlightStep.EDAD, FlightStep.METAS, FlightStep.ENFOQUE, FlightStep.COLOR, FlightStep.DIAS, FlightStep.AVISO)
    assertTrue(steps.none { it in questions })
  }

  @Test
  fun `jugando los 4 juegos se recorre todo y el ultimo paso termina`() {
    var s = full()
    val visited = mutableListOf(s.step)
    while (true) {
      s = if (s.step.isGame) FirstFlight.withResult(s, s.gameId(s.step.game!!), 0.5f, "dato") else FirstFlight.next(s) ?: break
      visited += s.step
    }
    assertEquals(FlightStep.CAMINO, s.step)
    assertEquals(FirstFlight.steps(FlightMode.FULL), visited.distinct())
    assertNull(FirstFlight.next(s))
  }

  @Test
  fun `un juego ya medido no se vuelve a jugar`() {
    val s = full(FlightStep.METAS).copy(measured = mapOf("secuencia" to 0.5f, "freno" to 0.4f))
    val dato = FirstFlight.next(s)!!
    assertEquals(FlightStep.DATO, dato.step)
    // El juego 2 ya está medido: de la pregunta se va directo a su tarjeta.
    assertEquals(FlightStep.TARJETA_2, FirstFlight.next(dato)!!.step)
  }

  @Test
  fun `un juego dejado para despues se salta con su tarjeta`() {
    val s = full(FlightStep.DATO).copy(skipped = setOf("freno"))
    assertEquals(FlightStep.ENFOQUE, FirstFlight.next(s)!!.step)
  }

  @Test
  fun `terminar despues deja el resto de los juegos para estimar y conserva lo medido`() {
    val s = full(FlightStep.JUEGO_3).copy(measured = mapOf("secuencia" to 0.5f, "freno" to 0.4f))
    val skipped = FirstFlight.skipRemainingGames(s)
    assertEquals(setOf("aterrizaje", "meteoros"), skipped.skipped)
    assertEquals(setOf("secuencia", "freno"), skipped.measured.keys)
    // El recorrido sigue por las preguntas que faltan, sin juegos ni tarjetas.
    assertEquals(listOf(FlightStep.JUEGO_3, FlightStep.DIAS, FlightStep.PUNTO, FlightStep.AVISO, FlightStep.CAMINO), walk(skipped))
  }

  @Test
  fun `atras vuelve un paso y nunca cae en el medio de un juego`() {
    assertEquals(FlightStep.EDAD, FirstFlight.back(full(FlightStep.TARJETA_1))!!.step)
    assertEquals(FlightStep.TARJETA_1, FirstFlight.back(full(FlightStep.METAS))!!.step)
    assertEquals(FlightStep.DATO, FirstFlight.back(full(FlightStep.JUEGO_2))!!.step)
    assertNull(FirstFlight.back(full(FlightStep.HOLA)))
    assertNull(FirstFlight.back(FlightState(FlightMode.GAMES, FlightStep.JUEGO_1)))
    for (step in FirstFlight.steps(FlightMode.FULL)) {
      val back = FirstFlight.back(full(step)) ?: continue
      assertFalse("de $step no se vuelve a ${back.step}", back.step.isGame)
    }
  }

  @Test
  fun `atras se salta la tarjeta de un juego que se dejo para despues`() {
    val s = full(FlightStep.ENFOQUE).copy(skipped = setOf("freno"))
    assertEquals(FlightStep.DATO, FirstFlight.back(s)!!.step)
  }

  @Test
  fun `el resultado de un juego anota su medida y su dato y pasa a su tarjeta`() {
    val s = FirstFlight.withResult(full(FlightStep.JUEGO_2), "freno", 0.7f, "Frenaste a tiempo 6 de 8 veces.")
    assertEquals(FlightStep.TARJETA_2, s.step)
    assertEquals(0.7f, s.measured["freno"]!!, 1e-4f)
    assertEquals("Frenaste a tiempo 6 de 8 veces.", s.phrases["freno"])
    val none = FirstFlight.withResult(full(FlightStep.JUEGO_2), "freno", 0.7f, null)
    assertNull(none.phrases["freno"])
    // Un juego que no es de la evaluación no mueve nada.
    assertEquals(full(FlightStep.JUEGO_2), FirstFlight.withResult(full(FlightStep.JUEGO_2), "radar", 0.7f, null))
  }

  @Test
  fun `la barra avanza y los minutos bajan hasta el final`() {
    var s = full()
    var lastProgress = 0f
    var lastMinutes = Int.MAX_VALUE
    while (true) {
      val p = FirstFlight.progress(s)
      val m = FirstFlight.minutesLeft(s)
      assertTrue(p > lastProgress - 1e-6f)
      assertTrue("$m > $lastMinutes en ${s.step}", m <= lastMinutes)
      lastProgress = p
      lastMinutes = m
      s = if (s.step.isGame) FirstFlight.withResult(s, s.gameId(s.step.game!!), 0.5f, null) else FirstFlight.next(s) ?: break
    }
    assertEquals(1f, lastProgress, 1e-4f)
    assertEquals(1, lastMinutes)
    // Al empezar, «unos 5 minutos» como dice Nubi.
    assertEquals(5, FirstFlight.minutesLeft(full()))
  }

  @Test
  fun `los minutos no cuentan los juegos que se dejaron para despues`() {
    val all = FirstFlight.minutesLeft(full())
    val skipped = FirstFlight.minutesLeft(full().copy(skipped = BaselinePlan.steps.map { it.gameId }.toSet()))
    assertTrue(skipped < all)
    assertEquals("Faltan unos 5 minutos", FirstFlight.minutesLeftText(5))
    assertEquals("Falta 1 minuto", FirstFlight.minutesLeftText(1))
  }

  @Test
  fun `el punto de partida da etapa a las 4 areas y marca tu fuerte y donde mas se juega`() {
    val s = full(FlightStep.PUNTO).copy(
      goals = setOf(DomainType.RAZONAMIENTO),
      measured = mapOf("secuencia" to 0.9f, "freno" to 0.5f, "aterrizaje" to 0.2f, "meteoros" to 0.6f)
    )
    val rows = FirstFlight.startingPoint(s, AgeBand.ADULT)
    assertEquals(4, rows.size)
    assertTrue(rows.all { it.measured })
    assertTrue(rows.all { it.stage in Skill.STAGES })
    assertEquals(1, rows.count { it.strong })
    assertEquals(DomainType.MEMORIA, rows.first { it.strong }.domain)
    // Donde más vamos a jugar: la primera del camino de hoy (las metas primero).
    assertEquals(DomainType.RAZONAMIENTO, rows.first { it.focus }.domain)
    assertEquals(1, rows.count { it.focus })
  }

  @Test
  fun `un area dejada para despues sale estimada y sin etiquetas si no hay dos medidas`() {
    val one = FirstFlight.startingPoint(full(FlightStep.PUNTO).copy(measured = mapOf("secuencia" to 0.9f)), AgeBand.ADULT)
    assertEquals(1, one.count { it.measured })
    assertTrue(one.none { it.strong })
    val none = FirstFlight.startingPoint(full(FlightStep.PUNTO), AgeBand.ADULT)
    assertTrue(none.none { it.measured || it.strong || it.focus })
    assertEquals(4, none.size)
  }

  @Test
  fun `cada juego del inicio tiene sus textos sin promesas de salud`() {
    for (step in BaselinePlan.steps) {
      val c = FlightCopy.of(step.gameId)
      assertTrue(c.how.isNotBlank() && c.short.isNotBlank() && c.why.isNotBlank())
      val all = (c.how + c.short + c.why).lowercase()
      listOf("neurona", "cerebro", "salud", "mejora", "octágono").forEach { assertFalse(all, all.contains(it)) }
    }
    assertEquals("memoria de trabajo", FlightCopy.of("secuencia").short)
  }

  @Test
  fun `el enfoque del resultado y la vision de color tienen valor por defecto`() {
    assertEquals(ResultFocus.AVANCE, ResultFocus.DEFAULT)
    assertEquals(ColorVision.NO_SE, ColorVision.DEFAULT)
    assertNull(ResultFocus.fromStored(null))
    assertNull(ColorVision.fromStored("otra"))
    assertEquals(ResultFocus.CONSEJO, ResultFocus.fromStored("CONSEJO"))
    assertNotNull(ColorVision.fromStored("DIFICULTAD"))
  }

  @Test
  fun `la pregunta del enfoque no se parece a la de animo de Lumosity`() {
    assertEquals(listOf("Lo que avancé", "Un consejo para la próxima"), ResultFocus.entries.map { it.label })
  }
}
