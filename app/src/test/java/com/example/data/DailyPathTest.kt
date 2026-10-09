package com.example.data

import com.example.bridge.UnityGameLauncher
import com.example.model.DomainType
import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/** El camino de hoy por avance real y con variedad (`DailyPath`): 3 juegos de 3 áreas, rotación, sin repetir, sin juegos sin tutorial que nunca se jugaron, determinístico. Lógica pura. */
class DailyPathTest {
  private val tutorial = UnityGameLauncher.TUTORIAL_GAMES
  private val level = mapOf(DomainType.MEMORIA to 0.1f, DomainType.ATENCION to 0.3f, DomainType.RAZONAMIENTO to 0.5f, DomainType.LENGUAJE to 0.9f)
  private fun domainOf(id: String) = GameRegistry.getById(id)!!.domain
  private fun areas(path: List<String>) = path.map { domainOf(it) }.toSet()
  private fun allPlayed(day: Long) = GameRegistry.allGames.associate { it.id to day }

  /** Simula [days] días seguidos con las mismas metas y el avance dado; devuelve los caminos en orden. Cada juego jugado queda con su día. */
  private fun simulate(days: Int, goals: Set<DomainType> = emptySet(), levels: Map<DomainType, Float> = level): List<List<String>> {
    val paths = mutableListOf<List<String>>()
    val played = allPlayed(0L).toMutableMap()
    for (d in 1..days) {
      val path = DailyPath.pick(
        dateKey = "2026-10-%02d".format(d), areaLevel = levels, goals = goals,
        yesterday = paths.getOrNull(d - 2), dayBefore = paths.getOrNull(d - 3),
        lastPlayedDay = played, today = d.toLong(), tutorialGames = tutorial
      )
      paths += path
      path.forEach { played[it] = d.toLong() }
    }
    return paths
  }

  @Test
  fun `son 3 juegos de 3 areas distintas`() {
    for (path in simulate(8)) {
      assertEquals(3, path.size)
      assertEquals(3, path.toSet().size)
      assertEquals("tres áreas distintas", 3, areas(path).size)
    }
  }

  @Test
  fun `rotacion en 4 dias el area que quedo fuera ayer entra hoy y ninguna falta dos dias seguidos`() {
    val paths = simulate(4)
    val out = paths.map { p -> DomainType.values().toSet() - areas(p) }
    for (d in 1 until 4) assertTrue("el área fuera de ${d} no puede faltar el día $d+1", out[d - 1].intersect(out[d]).isEmpty())
    for (d in DomainType.values()) assertTrue("$d aparece al menos 2 de 4 días", paths.count { d in areas(it) } >= 2)
    // el área de menor prioridad (la de más avance) es la que más se queda fuera, pero rota
    assertTrue(out.map { it.single() }.toSet().size >= 2)
  }

  @Test
  fun `no repite un juego de ayer ni de anteayer si hay alternativa`() {
    val paths = simulate(7)
    for (d in 2 until paths.size) {
      val recent = paths[d - 1].toSet() + paths[d - 2].toSet()
      for (id in paths[d]) assertFalse("$id repetido el día ${d + 1}", id in recent)
    }
  }

  @Test
  fun `prefiere el juego que hace mas dias que no se juega`() {
    val played = allPlayed(50L).toMutableMap()
    played["radar"] = 10L            // Atención: el que hace más días que no se juega
    val path = DailyPath.pick("2026-10-20", level, setOf(DomainType.ATENCION), null, null, played, 52L, tutorial)
    assertTrue(path.contains("radar"))
  }

  @Test
  fun `un juego sin tutorial que la persona nunca jugo no entra, y si ya lo jugo si es elegible`() {
    // Nadie jugó nada: solo entran juegos con tutorial
    val noTutorial = GameRegistry.allGames.map { it.id }.filter { it !in tutorial }.toSet()
    assertTrue(noTutorial.isNotEmpty())
    for (d in 1..40) {
      val path = DailyPath.pick("2026-11-%02d".format(d % 28 + 1) + d, level, emptySet(), null, null, emptyMap(), d.toLong(), tutorial)
      assertEquals(3, path.size)
      assertTrue("$path trae un juego sin tutorial que nunca se jugó", path.none { it in noTutorial })
    }
    // Piloto ya jugado hace mucho: en Atención, con metas, es el que más días lleva sin jugarse
    val played = mapOf("stroop" to 49L, "freno" to 49L, "satelites" to 49L, "piloto" to 10L)       // (Satélites y Piloto tienen tutorial desde el 9-oct: Satélites se juega reciente para que el más antiguo siga siendo Piloto)
    val path = DailyPath.pick("2026-11-30", level, setOf(DomainType.ATENCION), null, null, played, 50L, tutorial)
    assertTrue("piloto ya se jugó: es elegible y el más antiguo", "piloto" in path)
    // y uno sin tutorial que nunca se jugó sigue excluido aunque sea del mismo área (Satélites ya tiene tutorial desde el 9-oct)
    assertFalse("radar" in path)
  }

  @Test
  fun `si todos los juegos tuvieran tutorial la regla queda inerte`() {
    val all = GameRegistry.allGames.map { it.id }.toSet()
    val path = DailyPath.pick("2026-10-05", level, emptySet(), null, null, emptyMap(), 5L, all)
    assertEquals(3, path.size)
    assertEquals(3, areas(path).size)
  }

  @Test
  fun `es deterministico por fecha, con los mismos datos sale el mismo camino`() {
    val played = allPlayed(3L)
    val a = DailyPath.pick("2026-10-07", level, setOf(DomainType.MEMORIA), listOf("calculo", "parejas", "stroop"), null, played, 7L, tutorial)
    val b = DailyPath.pick("2026-10-07", level, setOf(DomainType.MEMORIA), listOf("calculo", "parejas", "stroop"), null, played, 7L, tutorial)
    assertEquals(a, b)
    // sin pasados ni partidas, los empates se deciden por la fecha: tampoco cambia al repetir
    val c = DailyPath.pick("2026-10-07", level, emptySet(), null, null, allPlayed(7L), 7L, tutorial)
    assertEquals(c, DailyPath.pick("2026-10-07", level, emptySet(), null, null, allPlayed(7L), 7L, tutorial))
  }

  @Test
  fun `las metas mantienen su prioridad y entran siempre`() {
    val goals = setOf(DomainType.LENGUAJE, DomainType.RAZONAMIENTO)       // las de más avance: sin metas serían las que quedan fuera
    val paths = simulate(6, goals = goals)
    for (p in paths) assertTrue("las dos metas entran cada día: $p", areas(p).containsAll(goals))
    // la tercera área rota entre las que no son meta (Memoria y Atención)
    val third = paths.map { areas(it) - goals }.map { it.single() }
    assertTrue(third.toSet().size == 2)
    // la meta de menos avance va primero en el camino
    assertEquals(DomainType.RAZONAMIENTO, domainOf(paths[0].first()))
  }

  @Test
  fun `si el area que quedo fuera ayer es la de menos prioridad igual entra hoy`() {
    // ayer: Memoria, Atención y Razonamiento (fuera Lenguaje, la de más avance, que sin esta regla volvería a quedar fuera)
    val yesterday = listOf("parejas", "stroop", "calculo")
    val path = DailyPath.pick("2026-10-09", level, emptySet(), yesterday, null, allPlayed(8L), 9L, tutorial)
    assertTrue(DomainType.LENGUAJE in areas(path))
    assertEquals(3, areas(path).size)
  }

  @Test
  fun `un area sin ningun juego elegible se salta y entra la siguiente`() {
    // Lenguaje: solo juegos sin tutorial que nunca se jugaron (los que tienen tutorial, anagramas y meteoros, se quitan del catálogo)
    val games = GameRegistry.allGames.filterNot { it.domain == DomainType.LENGUAJE && it.id in tutorial }
    val path = DailyPath.pick("2026-10-11", level, setOf(DomainType.LENGUAJE), null, null, emptyMap(), 11L, tutorial, games)
    assertEquals(3, path.size)
    assertTrue("Lenguaje no tiene candidatos: no entra", games.filter { it.id in path }.none { it.domain == DomainType.LENGUAJE })
    assertEquals("las otras tres áreas", 3, games.filter { it.id in path }.map { it.domain }.toSet().size)
    // Dos áreas sin candidatos: se completan los 3 juegos con otros elegibles, nunca con uno sin tutorial que nunca se jugó
    val few = games.filterNot { it.domain == DomainType.MEMORIA && it.id in tutorial }
    val p2 = DailyPath.pick("2026-10-12", level, emptySet(), null, null, emptyMap(), 12L, tutorial, few)
    assertEquals(3, p2.size)
    assertTrue(few.filter { it.id in p2 }.none { it.id !in tutorial })
  }

  @Test
  fun `sin partidas el nivel parte del punto de partida y lo que nunca se jugo no se pierde`() {
    // un área sin dato (0) pasa primero entre las que no son meta
    val levels = mapOf(DomainType.MEMORIA to 0f, DomainType.ATENCION to 0.6f, DomainType.RAZONAMIENTO to 0.7f, DomainType.LENGUAJE to 0.8f)
    val path = DailyPath.pick("2026-10-03", levels, emptySet(), null, null, emptyMap(), 3L, tutorial)
    assertEquals(DomainType.MEMORIA, domainOf(path.first()))
  }
}
