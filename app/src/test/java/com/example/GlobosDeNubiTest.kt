package com.example

import com.example.ui.components.EMPTY_AVANCE_LINE
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * La regla para TODOS los globos de Nubi (Opus, 21-oct): como mucho DOS frases y sin repetir lo que la pantalla ya dice (antes, el globo del detalle de un área tenía 6 líneas y repetía la línea de avance que está justo debajo).
 * Aquí se cuentan las frases de cada texto que Nubi dice en un globo: el Avance vacío (desde la Tarea 165 ya no hay globo en Hoy ni lámina del lugar: Nubi va posada en la tarjeta de la ruta, sin globo).
 */
class GlobosDeNubiTest {
  /** Las frases de un texto: se separan donde un punto, un «?» o un «!» van seguidos de un espacio (los dos puntos y las comas no cortan: «Te sugiero X: hace 4 días…» es UNA frase). */
  private fun sentences(text: String): Int = text.trim().split(Regex("(?<=[.!?])\\s+")).count { it.isNotBlank() }

  private fun assertGlobe(text: String, origin: String) {
    assertTrue("$origin: «$text» tiene ${sentences(text)} frases (máximo 2)", sentences(text) in 1..2)
    assertTrue("$origin: «$text» es demasiado larga para un globo (máximo ~100 letras)", text.length <= 100)
  }

  @Test
  fun el_contador_de_frases_cuenta_bien() {
    assertEquals(1, sentences("Te sugiero Atención: hace 4 días que no juegas Constelaciones"))
    assertEquals(2, sentences("¿Le damos un empujón? Te sugiero Constelaciones: sigue en tu ruta de hoy."))
    assertEquals(2, sentences("Te faltan 130 puntos para Experto. Esta semana sumaste 12 puntos."))
    assertEquals(3, sentences("Una. Dos. Tres."))
  }

  @Test
  fun el_avance_vacio_tambien_cumple() {
    assertGlobe(EMPTY_AVANCE_LINE, "Avance vacío")
  }
}
