package com.example.data

import com.example.model.DomainType
import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** El Desafío de hoy (`DailyChallenge`, Tarea 167): las seis reglas de la elección, que sea determinista por fecha, el estado del día (pendiente, jugado, superado) y cómo se guarda. Lógica pura. */
class DailyChallengeTest {
  private val route = listOf("bodega", "calculo", "intrusa")                      // Memoria, Razonamiento y Lenguaje: la ruta deja fuera Atención
  private val all = GameRegistry.allGames.map { it.id }.toSet()
  private fun domainOf(id: String) = GameRegistry.getById(id)!!.domain
  private fun pick(measured: Set<String> = all, route: List<String> = this.route, lastDay: Map<String, Long> = emptyMap(), today: Long = 100L, dateKey: String = "2026-10-25") =
    DailyChallenge.pick(dateKey, measured, route, lastDay, today)

  @Test
  fun `sin ningun juego medido no hay Desafio`() {
    assertNull(pick(measured = emptySet()))
  }

  @Test
  fun `solo se elige entre los juegos ya medidos`() {
    val measured = setOf("stroop", "bodega", "calculo", "intrusa")
    assertEquals("stroop", pick(measured = measured))
    for (m in all) assertTrue("el elegido tiene que estar medido", pick(measured = setOf(m, "bodega", "calculo", "intrusa")) in setOf(m, null))
  }

  @Test
  fun `nunca es un juego de la ruta de hoy aunque sea el unico medido`() {
    assertNull(pick(measured = route.toSet()))
    for (r in route) assertFalse(r == pick())
    assertTrue(pick(measured = route.toSet() + "freno") == "freno")
  }

  @Test
  fun `primero el area que la ruta dejo fuera`() {
    // Atención queda fuera: aunque Memoria tenga juegos que llevan más días sin Desafío, sale uno de Atención
    val lastDay = all.filter { domainOf(it) == DomainType.ATENCION }.associateWith { 99L }               // los de Atención tuvieron Desafío anteayer (el de ayer es otro)
    val chosen = pick(lastDay = lastDay.mapValues { 98L })!!
    assertEquals(DomainType.ATENCION, domainOf(chosen))
    // si el área que quedó fuera no tiene candidatos medidos, se pasa a las demás
    val sinAtencion = all.filter { domainOf(it) != DomainType.ATENCION }.toSet()
    val otro = pick(measured = sinAtencion)!!
    assertTrue(otro !in route)
    assertTrue(domainOf(otro) != DomainType.ATENCION)
  }

  @Test
  fun `dentro del area gana el que lleva mas dias sin Desafio y el que nunca lo tuvo va primero`() {
    val atencion = all.filter { domainOf(it) == DomainType.ATENCION }.sorted()
    assertTrue("el catálogo tiene varios juegos de Atención", atencion.size >= 3)
    val lastDay = mapOf(atencion[0] to 90L, atencion[1] to 50L, atencion[2] to 70L)
    val chosen = pick(measured = atencion.take(3).toSet() + route, lastDay = lastDay)
    assertEquals("el de hace 50 días", atencion[1], chosen)
    // los demás de Atención nunca tuvieron: uno de ellos gana al de hace 50 días
    val conNunca = pick(measured = atencion.toSet() + route, lastDay = lastDay)!!
    assertTrue(conNunca !in lastDay.keys)
  }

  @Test
  fun `no se repite el de ayer`() {
    val sinRepetir = pick()!!
    val ayer = mapOf(sinRepetir to 99L)
    assertTrue("hoy no es el mismo que ayer", pick(lastDay = ayer) != sinRepetir)
    // si el de ayer es el único candidato, hoy no hay Desafío
    assertNull(pick(measured = route.toSet() + sinRepetir, lastDay = ayer))
    // anteayer sí se puede repetir
    assertEquals(sinRepetir, pick(measured = route.toSet() + sinRepetir, lastDay = mapOf(sinRepetir to 98L)))
  }

  @Test
  fun `es determinista por fecha y el desempate usa la fecha no el azar`() {
    val a = (1..20).map { pick(dateKey = "2026-10-%02d".format(it)) }
    val b = (1..20).map { pick(dateKey = "2026-10-%02d".format(it)) }
    assertEquals("el mismo día y los mismos datos dan el mismo juego", a, b)
    assertTrue("con todo igual, distintos días reparten entre varios juegos de Atención", a.toSet().size > 1)
  }

  @Test
  fun `a lo largo de los dias el Desafio rota por los juegos del area que la ruta deja fuera`() {
    var log = ChallengeLog()
    val seen = mutableListOf<String>()
    for (d in 1..8) {
      val (today, next) = DailyChallenge.resolve(log, "2026-10-%02d".format(d), d.toLong(), all, route)
      log = next
      seen += today!!.gameId
    }
    assertTrue("nunca el mismo dos días seguidos", seen.zipWithNext().all { (a, b) -> a != b })
    val atencion = all.count { domainOf(it) == DomainType.ATENCION }
    assertEquals("antes de repetir pasa por todos los juegos del área", minOf(atencion, 8), seen.take(minOf(atencion, 8)).toSet().size)
  }

  @Test
  fun `el Desafio guardado no cambia durante el dia`() {
    val (first, log) = DailyChallenge.resolve(ChallengeLog(), "2026-10-25", 100L, all, route)
    assertNotNull(first)
    assertEquals(ChallengeState.PENDIENTE, first!!.state)
    // aunque después cambien los medidos o la ruta, el de hoy se queda
    val (again, same) = DailyChallenge.resolve(log, "2026-10-25", 100L, setOf("freno"), listOf("freno", "stroop", "radar"))
    assertEquals(first, again)
    assertEquals(log, same)
    // al día siguiente se elige otro y queda anotado
    val (tomorrow, next) = DailyChallenge.resolve(log, "2026-10-26", 101L, all, route)
    assertEquals("2026-10-26", tomorrow!!.day)
    assertTrue(tomorrow.gameId != first.gameId)
    assertEquals(101L, next.lastDay[tomorrow.gameId])
    assertEquals(100L, next.lastDay[first.gameId])
  }

  @Test
  fun `sin candidato no se guarda nada y se vuelve a mirar`() {
    val (none, log) = DailyChallenge.resolve(ChallengeLog(), "2026-10-25", 100L, emptySet(), route)
    assertNull(none)
    assertNull(log.today)
    assertTrue(log.lastDay.isEmpty())
  }

  @Test
  fun `una partida de Desafio del juego elegido lo pasa a jugado y, si se supera, a superado`() {
    val hoy = ChallengeToday("2026-10-25", "stroop")
    assertEquals(ChallengeState.JUGADO, DailyChallenge.after(hoy, "2026-10-25", "stroop", PlayMode.DESAFIO, passed = false)!!.state)
    assertEquals(ChallengeState.SUPERADO, DailyChallenge.after(hoy, "2026-10-25", "stroop", PlayMode.DESAFIO, passed = true)!!.state)
    // jugado sin superar y otra vez superado
    val jugado = hoy.copy(state = ChallengeState.JUGADO)
    assertEquals(ChallengeState.SUPERADO, DailyChallenge.after(jugado, "2026-10-25", "stroop", PlayMode.DESAFIO, passed = true)!!.state)
    assertNull("repetir jugado no cambia nada", DailyChallenge.after(jugado, "2026-10-25", "stroop", PlayMode.DESAFIO, passed = false))
  }

  @Test
  fun `lo superado no se pierde al repetir`() {
    val hecho = ChallengeToday("2026-10-25", "stroop", ChallengeState.SUPERADO)
    assertNull(DailyChallenge.after(hecho, "2026-10-25", "stroop", PlayMode.DESAFIO, passed = false))
    assertNull(DailyChallenge.after(hecho, "2026-10-25", "stroop", PlayMode.DESAFIO, passed = true))
  }

  @Test
  fun `solo cuenta el juego elegido de hoy y en modo Desafio`() {
    val hoy = ChallengeToday("2026-10-25", "stroop")
    assertNull("otro juego", DailyChallenge.after(hoy, "2026-10-25", "radar", PlayMode.DESAFIO, passed = true))
    for (m in listOf(PlayMode.SUAVE, PlayMode.A_TU_MEDIDA, PlayMode.EXPERTO)) assertNull("otro modo: $m", DailyChallenge.after(hoy, "2026-10-25", "stroop", m, passed = true))
    assertNull("otro día", DailyChallenge.after(hoy, "2026-10-26", "stroop", PlayMode.DESAFIO, passed = true))
    assertNull("sin Desafío", DailyChallenge.after(null, "2026-10-25", "stroop", PlayMode.DESAFIO, passed = true))
  }

  @Test
  fun `el estado del dia se guarda y se lee igual y lo roto se ignora`() {
    for (s in ChallengeState.values()) {
      val c = ChallengeToday("2026-10-25", "stroop", s)
      assertEquals(c, ChallengeToday.decode(c.encode()))
    }
    assertEquals("2026-10-25|stroop|JUGADO", ChallengeToday("2026-10-25", "stroop", ChallengeState.JUGADO).encode())
    assertNull(ChallengeToday.decode(null))
    assertNull(ChallengeToday.decode(""))
    assertNull(ChallengeToday.decode("2026-10-25|stroop"))
    assertNull(ChallengeToday.decode("2026-10-25|stroop|OTRO"))
  }

  @Test
  fun `dura lo que un juego de la ruta, es gratis y no cuenta para el futuro tope de partidas libres`() {
    assertEquals(DailyPath.MINUTES_PER_GAME, DailyChallenge.MINUTES)
    assertEquals("unos 2 minutos", DailyChallenge.minutesText())
    assertFalse("el Desafío de hoy no cuenta para el tope de partidas libres (docs/modelo-negocio.md)", DailyChallenge.COUNTS_FOR_FREE_CAP)
  }
}
