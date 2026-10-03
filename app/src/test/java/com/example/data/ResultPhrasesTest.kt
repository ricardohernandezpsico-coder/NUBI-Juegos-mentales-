package com.example.data

import com.example.data.CoachTone
import com.example.games.ResultPhrases
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/** La frase de debajo del título del resultado: lo de «velocidad» y «ritmo» solo donde el puntaje se apoya en la rapidez. */
class ResultPhrasesTest {
  @Test
  fun `Rastro de luz no habla de velocidad`() {
    for (score in listOf(0, 40, 59, 60, 70, 84, 85, 100)) {
      val t = ResultPhrases.feedback("secuencia", score, CoachTone.CLARO)
      assertFalse(t, t.contains("velocidad", ignoreCase = true))
      assertFalse(t, t.contains("ritmo", ignoreCase = true) && score >= 60)
    }
    assertEquals("Buena precisión.", ResultPhrases.feedback("secuencia", 74, CoachTone.CLARO))
    assertEquals("Muy buena precisión.", ResultPhrases.feedback("secuencia", 90, CoachTone.CLARO))
  }

  @Test
  fun `los juegos donde la rapidez cuenta conservan la frase de siempre`() {
    for (id in listOf("stroop", "comparacion", "cambiochip", "series", "calculo", "radar", "freno", "piloto", "trafico", "disparate", "meteoros", "cosecha", "correo", "acoplamiento", "parejas")) {
      assertEquals(id, "Buen equilibrio entre precisión y velocidad.", ResultPhrases.feedback(id, 70, CoachTone.CLARO))
      assertEquals(id, "Precisión y ritmo excelentes.", ResultPhrases.feedback(id, 90, CoachTone.CLARO))
      assertEquals(id, "La dificultad se ajusta a tu ritmo en cada partida.", ResultPhrases.feedback(id, 30, CoachTone.CLARO))
    }
  }

  @Test
  fun `la lista de juegos sin rapidez es la que se reporta`() {
    assertEquals(setOf("secuencia", "rutatesoro", "bitacora", "rumbo", "satelites", "aterrizaje", "anagramas", "intrusa"), ResultPhrases.NO_SPEED_GAMES)
    assertTrue(ResultPhrases.feedback("intrusa", 10, CoachTone.CLARO).contains("ajusta"))
  }

  @Test
  fun `el tono cambia la frase y ninguno lleva culpa`() {
    val ids = listOf("secuencia", "aterrizaje", "freno", "meteoros", "parejas")
    for (id in ids) for (score in listOf(10, 50, 70, 90)) {
      val claro = ResultPhrases.feedback(id, score, CoachTone.CLARO)
      val celebrar = ResultPhrases.feedback(id, score, CoachTone.CELEBRAR)
      assertTrue("$id $score: las dos variantes deben ser distintas", claro != celebrar)
      for (t in listOf(claro, celebrar)) {
        listOf("mal", "fall", "culpa", "error", "perdiste", "deber").forEach { assertFalse(t, t.lowercase().contains(it)) }
      }
    }
    assertEquals("¡Muy buena precisión! Sigue así.", ResultPhrases.feedback("secuencia", 74, CoachTone.CELEBRAR))
  }

  @Test
  fun `Rastro de luz no habla de velocidad ni de ritmo en ningun tono`() {
    for (score in listOf(0, 40, 59, 60, 70, 84, 85, 100)) {
      val t = ResultPhrases.feedback("secuencia", score, CoachTone.CELEBRAR)
      assertFalse(t, t.contains("velocidad", ignoreCase = true))
      assertFalse(t, t.contains("ritmo", ignoreCase = true) && score >= 60)
    }
  }
}
