package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/** El orden de los juegos de un área (Tarea 124, `LibraryOrder.sort`): En tu ruta de hoy / Hace días que no juegas / Nuevo para ti; los grupos vacíos no salen y los empates se rompen por el orden del catálogo. */
class LibraryOrderTest {
  private val catalog = listOf("a", "b", "c", "d", "e", "f")
  private val today = 100L

  private fun sort(
    games: List<String>,
    route: List<String> = emptyList(),
    done: Int = 0,
    last: Map<String, Long> = emptyMap()
  ) = LibraryOrder.sort(games, route, done, last, today, catalog)

  private fun ids(s: LibraryOrder.Section) = s.entries.map { it.gameId }

  @Test
  fun `tres grupos en su orden`() {
    val s = sort(games = listOf("a", "b", "c", "d"), route = listOf("c", "x", "y"), last = mapOf("a" to 90L, "b" to 99L))
    assertEquals(listOf(LibraryOrder.Group.RUTA, LibraryOrder.Group.HACE_DIAS, LibraryOrder.Group.NUEVO), s.map { it.group })
    assertEquals(listOf("c"), ids(s[0]))
    assertEquals("de más días sin jugar a menos", listOf("a", "b"), ids(s[1]))
    assertEquals(listOf("d"), ids(s[2]))
  }

  @Test
  fun `los grupos vacios no se muestran`() {
    assertEquals(listOf(LibraryOrder.Group.NUEVO), sort(listOf("a", "b")).map { it.group })
    assertEquals(listOf(LibraryOrder.Group.HACE_DIAS), sort(listOf("a", "b"), last = mapOf("a" to 1L, "b" to 2L)).map { it.group })
    assertTrue(sort(emptyList()).isEmpty())
  }

  @Test
  fun `la ruta va en el orden de la ruta y solo con los juegos del area`() {
    val s = sort(games = listOf("a", "b", "c", "d"), route = listOf("d", "z", "a"))
    assertEquals("el orden es el de la ruta, no el del catálogo", listOf("d", "a"), ids(s[0]))
    assertEquals("el número de parada es el de la ruta (z es de otra área pero ocupa la parada 2)", listOf(1, 3), s[0].entries.map { it.routeStop })
  }

  @Test
  fun `los ya hechos de la ruta quedan en su grupo marcados`() {
    val s = sort(games = listOf("a", "b", "c"), route = listOf("a", "b", "c"), done = 2)
    assertEquals(listOf(true, true, false), s[0].entries.map { it.doneToday })
    assertEquals(listOf("a", "b", "c"), ids(s[0]))
  }

  @Test
  fun `un juego de la ruta no se repite en los otros grupos`() {
    val s = sort(games = listOf("a", "b"), route = listOf("a"), last = mapOf("a" to 50L, "b" to 60L))
    assertEquals(1, s.flatMap { ids(it) }.count { it == "a" })
    assertEquals(listOf("b"), ids(s[1]))
  }

  @Test
  fun `empates por orden de catalogo`() {
    val s = sort(games = listOf("c", "a", "b"), last = mapOf("a" to 80L, "b" to 80L, "c" to 80L))
    assertEquals("mismo día: el del catálogo primero", listOf("a", "b", "c"), ids(s[0]))
    val n = sort(games = listOf("f", "d", "e"))
    assertEquals("nunca jugados, en orden de catálogo", listOf("d", "e", "f"), ids(n[0]))
  }

  @Test
  fun `los dias desde la ultima partida salen para la tercera linea de la fila`() {
    val s = sort(games = listOf("a", "b"), last = mapOf("a" to 96L, "b" to 100L))
    assertEquals(listOf(4, 0), s[0].entries.map { it.daysSince })
    assertEquals("los nunca jugados no tienen días", null, sort(listOf("a"))[0].entries[0].daysSince)
  }

  @Test
  fun `el juego que abriste por ultima vez ya no reordena`() {
    // la función no recibe el «último abierto»: dos corridas iguales dan lo mismo
    val a = sort(listOf("a", "b", "c"), last = mapOf("a" to 5L, "b" to 7L))
    val b = sort(listOf("a", "b", "c"), last = mapOf("a" to 5L, "b" to 7L))
    assertEquals(a, b)
    assertFalse(a.isEmpty())
  }
}
