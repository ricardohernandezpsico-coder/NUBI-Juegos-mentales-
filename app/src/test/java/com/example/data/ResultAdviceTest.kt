package com.example.data

import com.example.model.GamePlayResult
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** El consejo de la pantalla de resultado, separado de la medida (`result_focus`): cada frase sale UNA sola vez, antes o después. */
class ResultAdviceTest {
  private fun result(id: String, block: GamePlayResult.() -> GamePlayResult = { this }) =
    GamePlayResult(gameId = id, score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 1).block()

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
      NumberLine.message(NumberLineReading.INICIO), NumberLine.message(NumberLineReading.CENTRO), NumberLine.message(NumberLineReading.FINAL),
      Homing.sourceMessage(HomingSource.RUMBO)!!, Homing.sourceMessage(HomingSource.DISTANCIA)!!,
      Mail.clockMessage(2, 0, 5)!!, Mail.shipMessage(30, 1)!!
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

  @Test
  fun `En la punta de la lengua da su truco si alguna palabra necesito ayuda o la mostro Nubi`() {
    val some = ResultAdvice.tips(result("anagramas") { copy(puntaSolo = 5, puntaPista = 2, puntaLetras = 0, puntaVista = 1) })
    assertEquals(listOf("Si una no sale, piensa en cómo empieza o en otra parecida: suele destrabarla."), some)
    // todas solas: nada que aconsejar; sin datos del juego, tampoco
    assertTrue(ResultAdvice.tips(result("anagramas") { copy(puntaSolo = 8, puntaPista = 0, puntaLetras = 0, puntaVista = 0) }).isEmpty())
    assertTrue(ResultAdvice.tips(result("anagramas")).isEmpty())
  }

  @Test
  fun `Aterrizaje da su truco solo con una lectura clara`() {
    // primer tercio mucho peor que el resto
    val trues = listOf(0.1f, 0.2f, 0.5f, 0.5f, 0.9f, 0.9f)
    val givens = listOf(0.3f, 0.4f, 0.5f, 0.52f, 0.9f, 0.91f)
    val tips = ResultAdvice.tips(result("aterrizaje") { copy(numlineErrorPct = 8f, numlineTrue = trues, numlineGiven = givens) })
    assertEquals(1, tips.size)
    assertTrue(tips[0], tips[0].startsWith("Mide desde el 0"))
    // sin datos de la regla: ningún consejo
    assertTrue(ResultAdvice.tips(result("aterrizaje") { copy(numlineErrorPct = 8f) }).isEmpty())
  }

  @Test
  fun `Rastro de luz da el truco del modo que mas costo`() {
    val tips = ResultAdvice.tips(result("secuencia") { copy(rasBestLen = listOf(5, 4, 3, 2), rasRounds = listOf(7, 4, 5, 3), rasHits = listOf(7, 3, 2, 2)) })
    assertEquals(1, tips.size)
    assertTrue(tips[0], tips[0].startsWith("Sigue a un lucero"))
    // sin modos con rondas suficientes no hay consejo
    assertTrue(ResultAdvice.tips(result("secuencia") { copy(rasBestLen = listOf(5, 0, 0, 0), rasRounds = listOf(7, 0, 0, 0), rasHits = listOf(7, 0, 0, 0)) }).isEmpty())
  }

  @Test
  fun `Meteoros da su consejo solo si las traspuestas enganan mas`() {
    val seen = listOf(6, 6, 6, 6, 6, 6)
    val hits = listOf(5, 5, 5, 5, 5, 5)
    val tips = ResultAdvice.tips(result("meteoros") { copy(lexBandSeen = seen, lexBandHits = hits, lexFaSeen = listOf(6, 6, 6), lexFaHits = listOf(1, 1, 5)) })
    assertEquals(1, tips.size)
    assertTrue(tips[0].startsWith("Las letras cambiadas de lugar"))
    assertTrue(ResultAdvice.tips(result("meteoros") { copy(lexBandSeen = seen, lexBandHits = hits, lexFaSeen = listOf(6, 6, 6), lexFaHits = listOf(3, 3, 3)) }).isEmpty())
  }

  @Test
  fun `los juegos sin consejo propio no tienen bloque de consejo`() {
    for (id in listOf("freno", "stroop", "parejas", "radar", "satelites", "calculo")) assertTrue(id, ResultAdvice.tips(result(id)).isEmpty())
  }

  @Test
  fun `el orden por defecto es avance y la migracion de coach_tone`() {
    assertEquals(ResultFocus.AVANCE, ResultFocus.DEFAULT)
    assertEquals(ResultFocus.AVANCE, ResultFocus.migrate(null, "CELEBRAR"))
    assertEquals(ResultFocus.CONSEJO, ResultFocus.migrate(null, "CLARO"))
    assertEquals(ResultFocus.AVANCE, ResultFocus.migrate(null, null))
    assertEquals(ResultFocus.AVANCE, ResultFocus.migrate(null, "otra cosa"))
    // lo ya elegido gana sobre el valor viejo
    assertEquals(ResultFocus.CONSEJO, ResultFocus.migrate("CONSEJO", "CELEBRAR"))
    assertEquals(ResultFocus.AVANCE, ResultFocus.migrate("AVANCE", "CLARO"))
  }
}
