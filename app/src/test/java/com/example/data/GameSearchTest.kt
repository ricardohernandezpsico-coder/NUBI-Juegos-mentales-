package com.example.data

import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/** La búsqueda de «Todos los juegos» (Tarea 158): por el nombre, sin tildes ni mayúsculas. */
class GameSearchTest {
  @Test
  fun fold_quita_tildes_mayusculas_y_espacios_de_mas() {
    assertEquals("rescate relampago", GameSearch.fold("  Rescate   Relámpago "))
    assertEquals("la enie se conserva: año no es ano", "año", GameSearch.fold("AÑO"))
    assertFalse(GameSearch.fold("año") == GameSearch.fold("ano"))
  }

  @Test
  fun rescate_relampago_encuentra_rescate_relampago() {
    assertTrue(GameSearch.matches("Rescate relámpago", "rescate relampago"))
    assertTrue(GameSearch.matches("Rescate relámpago", "RESCATE RELÁMPAGO"))
    assertTrue("una parte del nombre", GameSearch.matches("Rescate relámpago", "relamp"))
  }

  @Test
  fun varias_palabras_deben_estar_todas_en_cualquier_orden() {
    assertTrue(GameSearch.matches("Tinta o Palabra", "palabra tinta"))
    assertFalse(GameSearch.matches("Tinta o Palabra", "tinta rescate"))
  }

  @Test
  fun una_busqueda_vacia_o_de_espacios_encuentra_todo() {
    for (g in GameRegistry.allGames) { assertTrue(GameSearch.matches(g.title, "")); assertTrue(GameSearch.matches(g.title, "   ")) }
  }

  @Test
  fun escribir_el_nombre_de_cualquier_juego_lo_encuentra_con_y_sin_tildes() {
    for (g in GameRegistry.allGames) {
      assertTrue("«${g.title}» se encuentra por su nombre", GameSearch.matches(g.title, g.title))
      assertTrue("«${g.title}» se encuentra sin tildes ni mayúsculas", GameSearch.matches(g.title, GameSearch.fold(g.title)))
    }
  }

  @Test
  fun lo_que_no_esta_en_ningun_nombre_no_encuentra_nada() {
    assertEquals(0, GameRegistry.allGames.count { GameSearch.matches(it.title, "zzzz") })
  }
}
