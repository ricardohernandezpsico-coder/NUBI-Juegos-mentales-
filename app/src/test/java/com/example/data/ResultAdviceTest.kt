package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** El «Truco: …» de una frase de medida, separado de ella: lo que usan los finales con sentido y el detalle de la partida. */
class ResultAdviceTest {
  @Test
  fun `la medida queda sin el truco y el consejo es solo el truco`() {
    val text = "Donde más te alejas del blanco: el centro de la regla. Truco: ubica primero la mitad exacta y corrige desde ahí."
    assertEquals("Donde más te alejas del blanco: el centro de la regla.", ResultAdvice.body(text))
    assertEquals("Ubica primero la mitad exacta y corrige desde ahí.", ResultAdvice.tipOf(text))
    // Sin truco: el texto tal cual y ningún consejo.
    assertEquals("Te posas igual de cerca del blanco en toda la regla.", ResultAdvice.body("Te posas igual de cerca del blanco en toda la regla."))
    assertNull(ResultAdvice.tipOf("Te posas igual de cerca del blanco en toda la regla."))
    // Un texto que empieza con «Truco: » (Rastro de luz) es todo consejo.
    assertEquals("Sigue a un lucero.", ResultAdvice.tipOf("Truco: sigue a un lucero."))
    assertNull(ResultAdvice.tipOf(""))
  }

  @Test
  fun `nada se repite ni se oculta, la medida mas el consejo es el texto original`() {
    val originals = listOf(
      "Donde más te alejas del blanco: el comienzo de la regla. Truco: mide desde el 0 y desde la mitad: el cuarto queda entre los dos.",
      "Donde más te alejas del blanco: el centro de la regla. Truco: ubica primero la mitad exacta y corrige desde ahí.",
      "Donde más te alejas del blanco: el final de la regla. Truco: mide hacia atrás desde el final, no solo desde el 0."
    )
    for (t in originals) {
      val tip = ResultAdvice.tipOf(t)
      assertTrue("sin consejo en: $t", tip != null)
      val body = ResultAdvice.body(t)
      assertFalse("la medida repite el consejo: $body", body.contains("Truco"))
      // el consejo (sin su mayúscula inicial) estaba en el original, y la medida también
      assertTrue(t.contains(tip!!.replaceFirstChar { it.lowercase() }))
      assertTrue(t.startsWith(body))
    }
  }
}
