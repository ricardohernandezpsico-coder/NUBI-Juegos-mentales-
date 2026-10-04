package com.example.data

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
      val t = ResultPhrases.feedback("secuencia", score)
      assertFalse(t, t.contains("velocidad", ignoreCase = true))
      assertFalse(t, t.contains("ritmo", ignoreCase = true) && score >= 60)
    }
    assertEquals("¡Muy buena precisión! Sigue así.", ResultPhrases.feedback("secuencia", 74))
    assertEquals("¡Qué precisión! Esta partida merece una celebración.", ResultPhrases.feedback("secuencia", 90))
  }

  @Test
  fun `los juegos donde la rapidez cuenta conservan la frase de siempre`() {
    for (id in listOf("stroop", "calculo", "radar", "freno", "piloto", "trafico", "disparate", "meteoros", "cosecha", "correo", "acoplamiento", "parejas")) {
      assertEquals(id, "¡Muy bien! Buen equilibrio entre precisión y velocidad.", ResultPhrases.feedback(id, 70))
      assertEquals(id, "¡Excelente! Tu precisión y tu ritmo brillaron hoy.", ResultPhrases.feedback(id, 90))
      assertEquals(id, "¡Jugaste! La dificultad se ajusta para que sigas disfrutando.", ResultPhrases.feedback(id, 30))
    }
  }

  @Test
  fun `la lista de juegos sin rapidez es la que se reporta`() {
    assertEquals(setOf("secuencia", "bitacora", "rumbo", "satelites", "aterrizaje", "anagramas", "intrusa"), ResultPhrases.NO_SPEED_GAMES)
    assertTrue(ResultPhrases.feedback("intrusa", 10).contains("ajusta"))
  }

  @Test
  fun `hay un solo tono, calido y sin culpa`() {
    val ids = listOf("secuencia", "aterrizaje", "freno", "meteoros", "parejas")
    for (id in ids) for (score in listOf(10, 50, 70, 90)) {
      val t = ResultPhrases.feedback(id, score)
      listOf("mal", "fall", "culpa", "error", "perdiste", "deber").forEach { assertFalse("$id $score: $t", t.lowercase().contains(it)) }
      assertTrue("$id $score: $t", t.startsWith("¡"))
    }
    assertEquals("¡Muy buena precisión! Sigue así.", ResultPhrases.feedback("secuencia", 74))
  }

  @Test
  fun `Rastro de luz no habla de velocidad ni de ritmo`() {
    for (score in listOf(0, 40, 59, 60, 70, 84, 85, 100)) {
      val t = ResultPhrases.feedback("secuencia", score)
      assertFalse(t, t.contains("velocidad", ignoreCase = true))
      assertFalse(t, t.contains("ritmo", ignoreCase = true) && score >= 60)
    }
  }
}
