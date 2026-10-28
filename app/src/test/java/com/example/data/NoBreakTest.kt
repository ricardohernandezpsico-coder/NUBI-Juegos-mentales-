package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Test

/** `glueNumbers`: el espacio entre un número y su unidad, y entre «de» y una cifra, es duro (U+00A0); nada más cambia. */
class NoBreakTest {
  @Test
  fun pega_el_numero_a_su_unidad() {
    assertEquals("21${NBSP}días", "21 días".glueNumbers())
    assertEquals("41${NBSP}partidas", "41 partidas".glueNumbers())
    assertEquals("Tu mejor racha: 21${NBSP}días", "Tu mejor racha: 21 días".glueNumbers())
    assertEquals("siguiente: 30${NBSP}días", "siguiente: 30 días".glueNumbers())
    assertEquals("desde el 8${NBSP}de octubre", "desde el 8 de octubre".glueNumbers())
  }

  @Test
  fun pega_el_numero_a_su_unidad_en_las_cantidades_con_de() {
    // «de 15 medallas»: el 15 con «medallas»; «de» puede quedar solo al final de un renglón, pero «15» nunca se separa de «medallas»
    assertEquals("de 15${NBSP}medallas", "de 15 medallas".glueNumbers())
    assertEquals("5${NBSP}de 8", "5 de 8".glueNumbers())
    assertEquals("17${NBSP}de 17", "17 de 17".glueNumbers())
  }

  @Test
  fun pega_de_al_ano_que_le_sigue() {
    assertEquals("desde el 3${NBSP}de octubre de${NBSP}2025", "desde el 3 de octubre de 2025".glueNumbers())
    assertEquals("de${NBSP}1999", "de 1999".glueNumbers())
  }

  @Test
  fun lo_demas_queda_igual() {
    for (s in listOf("probados al menos una vez", "días seguidos", "Tu racha empieza con tu próxima partida", "desde el origen", "siguiente: volver una vez", "Racha: ", "a las 19:00."))
      assertEquals(s, s.glueNumbers())
  }

  @Test
  fun es_idempotente() {
    val once = "Tu mejor racha: 21 días, desde el 8 de octubre".glueNumbers()
    assertEquals(once, once.glueNumbers())
  }
}
