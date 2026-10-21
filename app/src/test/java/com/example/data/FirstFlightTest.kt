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
  fun `el recorrido completo tiene los 17 pasos en el orden aprobado`() {
    // Tarea 132 («Juega primero»): el primer juego va antes de la edad y las metas. Tarea 143 (R4): ya no se pregunta por los colores.
    assertEquals(
      listOf(
        "HOLA", "NOMBRE", "JUEGO_1", "TARJETA_1", "EDAD", "METAS", "DATO", "JUEGO_2", "TARJETA_2",
        "JUEGO_3", "TARJETA_3", "DIAS", "JUEGO_4", "TARJETA_4", "PUNTO", "AVISO", "CAMINO"
      ),
      FirstFlight.steps(FlightMode.FULL).map { it.name }
    )
    assertNull("ya no hay pregunta sobre qué ver primero al terminar un juego", FlightStep.entries.firstOrNull { it.name == "ENFOQUE" })
    assertNull("ni la de los colores: hoy no cambia nada y es un paso menos para una persona de 60+", FlightStep.entries.firstOrNull { it.name == "COLOR" })
  }

  @Test
  fun `antes del primer juego solo va el nombre`() {
    val before = FirstFlight.steps(FlightMode.FULL).takeWhile { !it.isGame }
    assertEquals(listOf(FlightStep.HOLA, FlightStep.NOMBRE), before)
    // y el orden del enum es el del recorrido (el sentido del deslizamiento sale de él)
    assertEquals(FirstFlight.steps(FlightMode.FULL), FlightStep.entries.toList())
  }

  @Test
  fun `la edad se pregunta despues de la primera tarjeta y solo si no se sabe`() {
    val card = full(FlightStep.TARJETA_1).copy(measured = mapOf("secuencia" to 0.5f))
    assertEquals("sin edad, después de la tarjeta se pregunta", FlightStep.EDAD, FirstFlight.next(card)!!.step)
    assertEquals("con la edad sabida, se salta", FlightStep.METAS, FirstFlight.next(card.copy(age = AgeBand.ADULT))!!.step)
    assertEquals(FlightStep.METAS, FirstFlight.next(full(FlightStep.EDAD).copy(age = AgeBand.SENIOR))!!.step)
    assertTrue("sin edad el recorrido pasa por EDAD", FlightStep.EDAD in walk(full()))
    assertFalse("con la edad sabida desde el inicio no pasa por EDAD", FlightStep.EDAD in walk(full().copy(age = AgeBand.ADULT)))
    // y «Atrás» desde la pregunta vuelve a la tarjeta
    assertEquals(FlightStep.TARJETA_1, FirstFlight.back(full(FlightStep.EDAD))!!.step)
  }

  @Test
  fun `un recorrido guardado con el orden viejo y la edad respondida no la vuelve a preguntar`() {
    // quien quedó guardado en METAS con la edad ya respondida (el orden viejo la preguntaba antes del juego 1): «Atrás» cae en la tarjeta, no en EDAD
    val old = full(FlightStep.METAS).copy(name = "Ana", age = AgeBand.SENIOR, measured = mapOf("secuencia" to 0.5f))
    assertEquals(FlightStep.TARJETA_1, FirstFlight.back(old)!!.step)
  }

  @Test
  fun `la evaluacion repetida es solo la parte de juegos, sin preguntas`() {
    val steps = FirstFlight.steps(FlightMode.GAMES)
    assertEquals(FlightStep.JUEGO_1, steps.first())
    assertEquals(FlightStep.PUNTO, steps.last())
    val questions = setOf(FlightStep.NOMBRE, FlightStep.EDAD, FlightStep.METAS, FlightStep.DIAS, FlightStep.AVISO)
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
    assertEquals(FlightStep.JUEGO_3, FirstFlight.next(s)!!.step)
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
    assertEquals("antes del primer juego, solo el nombre", FlightStep.NOMBRE, FirstFlight.back(full(FlightStep.TARJETA_1))!!.step)
    assertEquals(FlightStep.EDAD, FirstFlight.back(full(FlightStep.METAS))!!.step)
    assertEquals("con la edad ya respondida se salta", FlightStep.TARJETA_1, FirstFlight.back(full(FlightStep.METAS).copy(age = AgeBand.ADULT))!!.step)
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
    val s = full(FlightStep.JUEGO_3).copy(skipped = setOf("freno"))
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

  // ---- las farolas de etapa (Tarea 132, A4)

  private fun lit(s: FlightState) = FirstFlight.stations(s).map { it.lit }
  private fun text(step: FlightStep) = FirstFlight.stationText(full(step))

  @Test
  fun `la primera farola esta encendida desde el primer paso`() {
    val s = FirstFlight.stations(full())
    assertEquals(listOf("Empezar", "Memoria", "Atención", "Razonamiento", "Lenguaje", "Tu punto de partida"), s.map { it.label })
    assertEquals("abrir Nubi es el primer paso", listOf(true, false, false, false, false, false), s.map { it.lit })
    assertEquals("la actual lleva el aro", listOf(true, false, false, false, false, false), s.map { it.current })
    assertEquals(1, FirstFlight.stations(full(FlightStep.NOMBRE)).count { it.lit })
  }

  @Test
  fun `las farolas avanzan con las etapas y volver atras no apaga ninguna`() {
    // se juegan los 4 juegos en orden; en cada paso se mira lo que enciende «Continuar» y lo que deja «Atrás»
    var s = full()
    var prevLit = lit(s)
    var prevStage = FirstFlight.stationIndex(s)
    val seenStages = mutableSetOf(prevStage)
    while (true) {
      val back = FirstFlight.back(s)
      if (back != null) assertEquals("«Atrás» desde ${s.step} apagó o encendió una farola", lit(s), lit(back))
      s = if (s.step.isGame) FirstFlight.withResult(s, s.gameId(s.step.game!!), 0.5f, null) else FirstFlight.next(s) ?: break
      val now = lit(s)
      for (i in now.indices) assertTrue("en ${s.step} se apagó la farola $i", now[i] || !prevLit[i])
      assertTrue("la etapa retrocedió en ${s.step}", FirstFlight.stationIndex(s) >= prevStage)
      prevLit = now
      prevStage = FirstFlight.stationIndex(s)
      seenStages += prevStage
    }
    assertEquals("pasa por las seis etapas", (0..5).toSet(), seenStages)
    // con el juego 1 medido, la farola de Memoria está encendida; sin él, no
    assertEquals(listOf(true, true, false, false, false, false), lit(full(FlightStep.TARJETA_1).copy(measured = mapOf("secuencia" to 0.5f))))
    assertEquals(listOf(true, false, false, false, false, false), lit(full(FlightStep.JUEGO_1)))
    // la última, cuando el punto de partida se guardó
    assertEquals(listOf(true, true, true, true, true, true), lit(full(FlightStep.AVISO).copy(measured = BaselinePlan.steps.associate { it.gameId to 0.5f }, applied = true)))
  }

  @Test
  fun `un juego dejado para despues enciende su farola igual`() {
    val s = full(FlightStep.JUEGO_3).copy(skipped = setOf("freno", "aterrizaje", "meteoros"), measured = mapOf("secuencia" to 0.5f))
    assertEquals("la etapa se pasó: nada se castiga", listOf(true, true, true, true, true, false), lit(s))
  }

  @Test
  fun `con los 4 juegos para despues las pantallas que siguen dicen Paso 6 de 6 y la etapa nunca salta de 4 a 6`() {
    // «Terminar después» en el juego 1: se saltan los 4 juegos, pero siguen la edad, las metas, el dato, los días, el punto de partida, el aviso y el camino
    var s = FirstFlight.skipRemainingGames(full(FlightStep.JUEGO_1))
    val shown = mutableListOf<String>()
    while (true) {
      s = FirstFlight.next(s) ?: break
      shown += FirstFlight.stationText(s)
      assertEquals("en ${s.step} la etapa actual es «Tu punto de partida»", 5, FirstFlight.stationIndex(s))
      assertTrue("en ${s.step} el aro va en la última farola", FirstFlight.stations(s).last().current)
    }
    assertEquals(listOf(FlightStep.EDAD, FlightStep.METAS, FlightStep.DATO, FlightStep.DIAS, FlightStep.PUNTO, FlightStep.AVISO, FlightStep.CAMINO).size, shown.size)
    assertTrue("pantallas que no son de juego: «Paso 6 de 6» a secas (Tarea 143, A2)", shown.all { it == "Paso 6 de 6" })
    // con el juego 1 medido y los demás para después: Memoria hasta el dato; desde ahí, directo a la última
    var t = FirstFlight.skipRemainingGames(full(FlightStep.JUEGO_2).copy(measured = mapOf("secuencia" to 0.5f), age = AgeBand.ADULT, goals = setOf(DomainType.MEMORIA)))
    assertEquals("Paso 2 de 6", FirstFlight.stationText(full(FlightStep.DATO).copy(skipped = t.skipped, measured = t.measured)))
    for (step in listOf(FlightStep.DIAS, FlightStep.PUNTO)) assertEquals("Paso 6 de 6", FirstFlight.stationText(full(step).copy(skipped = t.skipped, measured = t.measured)))
    // sin juegos saltados nada cambia: el orden de siempre (Memoria, Atención, Razonamiento, Lenguaje)
    assertEquals("Paso 3 de 6 · Atención", FirstFlight.stationText(full(FlightStep.TARJETA_2)))
    // los números de paso que se ven en todo el recorrido nunca bajan
    var w = FirstFlight.skipRemainingGames(full(FlightStep.JUEGO_3).copy(measured = mapOf("secuencia" to 0.5f, "freno" to 0.5f), age = AgeBand.ADULT))
    var last = -1
    while (true) {
      val n = FirstFlight.stationIndex(w)
      assertTrue("la etapa bajó en ${w.step}", n >= last)
      last = n
      w = FirstFlight.next(w) ?: break
    }
  }

  @Test
  fun `el texto dice el paso N de 6 y la etapa`() {
    assertEquals("Empezamos juntos", text(FlightStep.HOLA))
    assertEquals("Empezamos juntos", text(FlightStep.NOMBRE))
    // Un juego o su tarjeta nombran el área; las pantallas que NO son de juego (preguntas, dato, punto de partida, aviso, camino) dicen «Paso N de 6» a secas (Tarea 143, A2)
    for (step in listOf(FlightStep.JUEGO_1, FlightStep.TARJETA_1)) assertEquals("Paso 2 de 6 · Memoria", text(step))
    for (step in listOf(FlightStep.EDAD, FlightStep.METAS, FlightStep.DATO)) assertEquals("Paso 2 de 6", text(step))
    for (step in listOf(FlightStep.JUEGO_2, FlightStep.TARJETA_2)) assertEquals("Paso 3 de 6 · Atención", text(step))
    for (step in listOf(FlightStep.JUEGO_3, FlightStep.TARJETA_3)) assertEquals("Paso 4 de 6 · Razonamiento", text(step))
    assertEquals("Paso 4 de 6", text(FlightStep.DIAS))
    for (step in listOf(FlightStep.JUEGO_4, FlightStep.TARJETA_4)) assertEquals("Paso 5 de 6 · Lenguaje", text(step))
    for (step in listOf(FlightStep.PUNTO, FlightStep.AVISO, FlightStep.CAMINO)) assertEquals("Paso 6 de 6", text(step))
    // nunca promete tiempo
    for (step in FirstFlight.steps(FlightMode.FULL)) assertFalse(text(step), text(step).lowercase().contains("minuto"))
  }

  @Test
  fun `la evaluacion repetida tiene 4 farolas y Listo`() {
    val s = FlightState(FlightMode.GAMES, FlightStep.JUEGO_1)
    assertEquals(listOf("Memoria", "Atención", "Razonamiento", "Lenguaje", "Listo"), FirstFlight.stations(s).map { it.label })
    assertEquals(listOf(false, false, false, false, false), lit(s))
    assertEquals(0, FirstFlight.stationIndex(s))
    assertEquals("Juego 1 de 4 · Memoria", FirstFlight.stationText(s))
    assertEquals("Juego 2 de 4 · Atención", FirstFlight.stationText(s.copy(step = FlightStep.JUEGO_2)))
    assertEquals("Juego 4 de 4 · Lenguaje", FirstFlight.stationText(s.copy(step = FlightStep.TARJETA_4)))
    val punto = s.copy(step = FlightStep.PUNTO, measured = BaselinePlan.steps.associate { it.gameId to 0.5f })
    assertEquals(4, FirstFlight.stationIndex(punto))
    assertEquals("Listo · tu punto de partida", FirstFlight.stationText(punto))
    assertEquals(listOf(true, true, true, true, false), lit(punto))
    assertEquals(listOf(true, true, true, true, true), lit(punto.copy(applied = true)))
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
  fun `la vision de color tiene valor por defecto`() {
    assertEquals(ColorVision.NO_SE, ColorVision.DEFAULT)
    assertNull(ColorVision.fromStored("otra"))
    assertNotNull(ColorVision.fromStored("DIFICULTAD"))
  }
}
