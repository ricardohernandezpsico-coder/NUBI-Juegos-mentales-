package com.example.data

import java.util.Random
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * El ritmo de la última partida (`TimeTrialMarks.pace`, Tarea 200; `auditoria-fable/nubi-plus-comparacion.md` §2.3): los aciertos y los errores en cada medio minuto (0 a 30, 30 a 60, 60 a 90 y 90 a 120 s) y la frase que lee el gráfico.
 * «Tu mejor medio minuto fue el de 30 a 60 segundos, con 14 aciertos. Tus 3 errores llegaron en el último minuto, en el paso 4 de la pista.» Lógica pura. Decide y prueba los casos borde: empates, sin errores, errores repartidos, un error, sin aciertos y sin ensayos.
 */
class TimeTrialPaceTest {
  /** Un ensayo: a qué hora (ms), si fue acierto y en qué paso de la pista. */
  private fun trial(ms: Int, ok: Boolean, level: Int = 1) = Triple(ms, ok, level)

  /** Un medio minuto de ejemplo: [hits] aciertos en el paso [hitLevel] y un error en cada paso de [errorLevels], todos dentro del medio minuto [half] (0 a 3), en orden de hora. */
  private fun half(half: Int, hits: Int, errorLevels: List<Int> = emptyList(), hitLevel: Int = 1): List<Triple<Int, Boolean, Int>> {
    val base = half * 30_000
    val ok = List(hits) { trial(base + 500 + it * 400, true, hitLevel) }
    val bad = errorLevels.mapIndexed { i, l -> trial(base + 15_000 + i * 1_000, false, l) }
    return (ok + bad).sortedBy { it.first }
  }

  private fun game(vararg trials: List<Triple<Int, Boolean, Int>>, id: String = "g", gameId: String = "stroop", at: Long = 1_000L, version: Int = 2): TimeTrialResult {
    val all = trials.toList().flatten()
    val tr = TimeTrialTrials(all.map { it.first }, all.map { it.second }, all.map { it.third })
    return TimeTrialResult(id = id, gameId = gameId, day = "2026-10-28", trackVersion = version, hits = tr.hits, errors = tr.errors, level = all.maxOfOrNull { it.third } ?: 1, timestamp = at, trials = tr)
  }

  private fun paceOf(vararg trials: List<Triple<Int, Boolean, Int>>) = TimeTrialMarks.paceOf(game(*trials))!!

  // ------------------------------------------------------------------ la frase de Fable

  @Test
  fun `la frase de la lamina de Fable sale tal cual`() {
    val p = paceOf(half(0, 10), half(1, 14), half(2, 12, listOf(4)), half(3, 11, listOf(4, 4), hitLevel = 4))
    assertEquals(
      "Tu mejor medio minuto fue el de 30 a 60 segundos, con 14 aciertos. Tus 3 errores llegaron en el último minuto, en el paso 4 de la pista.",
      p.sentence
    )
    assertEquals("Tu mejor medio minuto fue el de 30 a 60 segundos, con 14 aciertos.", p.bestLine)
    assertEquals("Tus 3 errores llegaron en el último minuto, en el paso 4 de la pista.", p.errorsLine)
  }

  @Test
  fun `cuenta los aciertos y los errores de cada medio minuto`() {
    val p = paceOf(half(0, 10), half(1, 14), half(2, 12, listOf(4)), half(3, 11, listOf(4, 4), hitLevel = 4))
    assertEquals(listOf(TimeTrialMarks.HalfMinute(10, 0), TimeTrialMarks.HalfMinute(14, 0), TimeTrialMarks.HalfMinute(12, 1), TimeTrialMarks.HalfMinute(11, 2)), p.halves)
    assertEquals(listOf(1), p.bestHalves)
    assertEquals(listOf(2, 3), p.errorHalves)
    assertEquals(listOf(4), p.errorSteps)
    assertEquals("sumas = las de la partida", 47 to 3, p.halves.sumOf { it.hits } to p.halves.sumOf { it.errors })
  }

  @Test
  fun `cada ensayo cae en el medio minuto de su hora y los limites son 30, 60, 90 y 120 segundos`() {
    val t = listOf(0, 29_999, 30_000, 59_999, 60_000, 89_999, 90_000, 119_999, 120_000)
    val p = TimeTrialMarks.paceOf(
      TimeTrialResult(gameId = "stroop", day = "2026-10-28", trackVersion = 2, hits = 9, errors = 0, level = 1, trials = TimeTrialTrials(t, List(9) { true }, List(9) { 1 }))
    )!!
    assertEquals("0 y 29 999 ms son el primero; 30 000 ya es el segundo; 120 000 (el final) cuenta en el último", listOf(2, 2, 2, 3), p.halves.map { it.hits })
  }

  // ------------------------------------------------------------------ los aciertos: el mejor medio minuto, con sus empates

  @Test
  fun `cada medio minuto puede ser el mejor y se nombra`() {
    val names = listOf("el de 0 a 30", "el de 30 a 60", "el de 60 a 90", "el de 90 a 120")      // los mismos números del eje: «el segundo» se leería como «un segundo»
    for (best in 0..3) {
      val p = paceOf(*Array(4) { half(it, if (it == best) 9 else 5) })
      assertEquals("Tu mejor medio minuto fue ${names[best]} segundos, con 9 aciertos.", p.bestLine)
    }
  }

  @Test
  fun `con empate en lo mas alto los nombra a todos y dice cada uno`() {
    assertEquals("Tus mejores medios minutos fueron el de 0 a 30 y el de 30 a 60 segundos, con 10 aciertos cada uno.", paceOf(half(0, 10), half(1, 10), half(2, 7), half(3, 4)).bestLine)
    assertEquals("Tus mejores medios minutos fueron el de 60 a 90 y el de 90 a 120 segundos, con 8 aciertos cada uno.", paceOf(half(0, 3), half(1, 5), half(2, 8), half(3, 8)).bestLine)
    assertEquals("Tus mejores medios minutos fueron el de 0 a 30, el de 60 a 90 y el de 90 a 120 segundos, con 9 aciertos cada uno.", paceOf(half(0, 9), half(1, 2), half(2, 9), half(3, 9)).bestLine)
    assertEquals(listOf(0, 2, 3), paceOf(half(0, 9), half(1, 2), half(2, 9), half(3, 9)).bestHalves)
  }

  @Test
  fun `si los cuatro medios minutos empatan dice que fueron parejos`() {
    val p = paceOf(half(0, 12), half(1, 12), half(2, 12), half(3, 12))
    assertEquals("Los cuatro medios minutos fueron parejos, con 12 aciertos cada uno.", p.bestLine)
    assertEquals(listOf(0, 1, 2, 3), p.bestHalves)
  }

  @Test
  fun `un solo acierto va en singular, tambien con empate`() {
    assertEquals("Tu mejor medio minuto fue el de 30 a 60 segundos, con 1 acierto.", paceOf(half(0, 0), half(1, 1)).bestLine)
    assertEquals("Los cuatro medios minutos fueron parejos, con 1 acierto cada uno.", paceOf(half(0, 1), half(1, 1), half(2, 1), half(3, 1)).bestLine)
    assertEquals("Tus mejores medios minutos fueron el de 0 a 30 y el de 90 a 120 segundos, con 1 acierto cada uno.", paceOf(half(0, 1), half(3, 1)).bestLine)
  }

  @Test
  fun `una partida sin aciertos no inventa un mejor medio minuto`() {
    val p = paceOf(half(0, 0, listOf(1)), half(1, 0, listOf(1, 1)))
    assertEquals("Esta partida no tuvo aciertos.", p.bestLine)
    assertTrue(p.bestHalves.isEmpty())
    assertEquals("Esta partida no tuvo aciertos. Tus 3 errores llegaron en el primer minuto, en el paso 1 de la pista.", p.sentence)
  }

  // ------------------------------------------------------------------ los errores: cuándo y en qué paso

  @Test
  fun `sin errores lo dice y nada mas`() {
    val p = paceOf(half(0, 10), half(1, 14), half(2, 12), half(3, 11))
    assertEquals("Sin errores en esta partida.", p.errorsLine)
    assertEquals("Tu mejor medio minuto fue el de 30 a 60 segundos, con 14 aciertos. Sin errores en esta partida.", p.sentence)
    assertTrue(p.errorHalves.isEmpty())
    assertTrue(p.errorSteps.isEmpty())
  }

  @Test
  fun `un solo error va en singular y dice en que medio minuto`() {
    assertEquals("Tu único error llegó en el primer medio minuto, en el paso 1 de la pista.", paceOf(half(0, 5, listOf(1))).errorsLine)
    assertEquals("Tu único error llegó en el segundo medio minuto, en el paso 2 de la pista.", paceOf(half(0, 5), half(1, 5, listOf(2), hitLevel = 2)).errorsLine)
    assertEquals("Tu único error llegó en el tercer medio minuto, en el paso 3 de la pista.", paceOf(half(0, 5), half(2, 5, listOf(3), hitLevel = 3)).errorsLine)
    assertEquals("Tu único error llegó en el último medio minuto, en el paso 5 de la pista.", paceOf(half(0, 5), half(3, 5, listOf(5), hitLevel = 5)).errorsLine)
  }

  @Test
  fun `varios errores en el mismo medio minuto lo dicen en plural`() {
    assertEquals("Tus 2 errores llegaron en el segundo medio minuto, en el paso 2 de la pista.", paceOf(half(1, 8, listOf(2, 2))).errorsLine)
    assertEquals("Tus 4 errores llegaron en el último medio minuto, en el paso 3 de la pista.", paceOf(half(3, 8, listOf(3, 3, 3, 3))).errorsLine)
  }

  @Test
  fun `dos medios minutos seguidos se dicen como un minuto o como el medio de la partida`() {
    assertEquals("Tus 2 errores llegaron en el primer minuto, en el paso 1 de la pista.", paceOf(half(0, 5, listOf(1)), half(1, 5, listOf(1))).errorsLine)
    assertEquals("Tus 3 errores llegaron en el último minuto, en el paso 4 de la pista.", paceOf(half(2, 5, listOf(4)), half(3, 5, listOf(4, 4))).errorsLine)
    assertEquals("Tus 2 errores llegaron en el medio de la partida, en el paso 2 de la pista.", paceOf(half(1, 5, listOf(2)), half(2, 5, listOf(2))).errorsLine)
  }

  @Test
  fun `errores repartidos lo dicen con otro verbo`() {
    assertEquals("Tus 2 errores se repartieron durante la partida, en el paso 2 de la pista.", paceOf(half(0, 5, listOf(2)), half(2, 5, listOf(2))).errorsLine)
    assertEquals("Tus 2 errores se repartieron durante la partida, en el paso 3 de la pista.", paceOf(half(0, 5, listOf(3)), half(3, 5, listOf(3))).errorsLine)
    assertEquals("tres medios minutos seguidos tampoco son «el medio»", "Tus 3 errores se repartieron durante la partida, en el paso 1 de la pista.", paceOf(half(0, 5, listOf(1)), half(1, 5, listOf(1)), half(2, 5, listOf(1))).errorsLine)
    assertEquals("Tus 4 errores se repartieron durante la partida, en el paso 1 de la pista.", paceOf(half(0, 5, listOf(1)), half(1, 5, listOf(1)), half(2, 5, listOf(1)), half(3, 5, listOf(1))).errorsLine)
  }

  @Test
  fun `el paso de los errores puede ser uno, dos o varios`() {
    assertEquals("Tus 3 errores llegaron en el último medio minuto, en los pasos 3 y 4 de la pista.", paceOf(half(3, 8, listOf(3, 4, 4))).errorsLine)
    assertEquals("Tus 4 errores llegaron en el último medio minuto, entre el paso 2 y el 4 de la pista.", paceOf(half(3, 8, listOf(2, 3, 4, 4))).errorsLine)
    assertEquals("tres pasos no seguidos: de menor a mayor", "Tus 3 errores llegaron en el último medio minuto, entre el paso 1 y el 5 de la pista.", paceOf(half(3, 8, listOf(5, 1, 3))).errorsLine)
    assertEquals("un paso repetido cuenta una vez", listOf(2, 3), paceOf(half(3, 8, listOf(3, 2, 3, 2))).errorSteps)
  }

  @Test
  fun `el paso es el de antes de registrar el ensayo, no el que abre`() {
    // el error llega justo tras el acierto que sube de paso: Unity lo anota en el paso donde se jugó
    val p = paceOf(listOf(trial(1000, true, 1), trial(2000, true, 2), trial(3000, false, 2)))
    assertEquals(listOf(2), p.errorSteps)
  }

  // ------------------------------------------------------------------ qué partida y cuándo no hay ritmo

  @Test
  fun `una partida sin ensayos no tiene ritmo`() {
    val old = TimeTrialResult(gameId = "stroop", day = "2026-10-28", trackVersion = 2, hits = 30, errors = 2, level = 3)
    assertNull(TimeTrialMarks.paceOf(old))
    assertNull(TimeTrialMarks.pace(listOf(old), "stroop"))
    assertNull("sin ninguna partida", TimeTrialMarks.pace(emptyList(), "stroop"))
  }

  @Test
  fun `un registro incompleto no se usa porque los numeros no serian los del final`() {
    val full = game(half(0, 10), half(1, 14))
    val cut = full.copy(hits = full.hits + 20)                // Unity anotó hasta 200 ensayos: la partida tuvo más
    assertFalse(cut.hasPace)
    assertNull(TimeTrialMarks.paceOf(cut))
    assertNull(TimeTrialMarks.pace(listOf(cut), "stroop"))
  }

  @Test
  fun `toma la ultima partida del juego que tenga ritmo`() {
    val older = game(half(0, 5), id = "a", at = 1_000L)
    val newer = game(half(1, 9), half(2, 3, listOf(2)), id = "b", at = 2_000L)
    val noTrials = TimeTrialResult(id = "c", gameId = "stroop", day = "2026-10-28", trackVersion = 2, hits = 20, errors = 1, level = 2, timestamp = 3_000L)
    assertEquals("la más nueva con ritmo", "b", TimeTrialMarks.pace(listOf(older, newer), "stroop")!!.result.id)
    assertEquals("sin importar el orden de la lista", "b", TimeTrialMarks.pace(listOf(newer, older), "stroop")!!.result.id)
    assertEquals("una partida más nueva SIN ritmo (de una Unity vieja) no tapa a la anterior", "b", TimeTrialMarks.pace(listOf(older, newer, noTrials), "stroop")!!.result.id)
  }

  @Test
  fun `solo cuenta el juego pedido y la pista de hoy`() {
    val stroop = game(half(0, 5), id = "s", gameId = "stroop", at = 1_000L)
    val radar = game(half(0, 7), id = "r", gameId = "radar", at = 5_000L)
    val oldTrack = game(half(0, 9), id = "v1", gameId = "stroop", at = 9_000L, version = 1)
    assertEquals("s", TimeTrialMarks.pace(listOf(stroop, radar, oldTrack), "stroop")!!.result.id)
    assertEquals("r", TimeTrialMarks.pace(listOf(stroop, radar, oldTrack), "radar")!!.result.id)
    assertNull("Acoplamiento no jugó", TimeTrialMarks.pace(listOf(stroop, radar, oldTrack), "acoplamiento"))
    assertNull("la pista vieja no se compara: sin otra partida, no hay ritmo", TimeTrialMarks.pace(listOf(oldTrack), "stroop"))
  }

  // ------------------------------------------------------------------ las palabras y los numeros

  @Test
  fun `cada numero va con su unidad y no aparece ninguna palabra que mida a la persona`() {
    val rng = Random(200)
    val seen = mutableSetOf<String>()
    repeat(400) {
      val halves = (0..3).map { h -> half(h, rng.nextInt(16), List(if (rng.nextInt(3) == 0) rng.nextInt(4) else 0) { 1 + rng.nextInt(5) }.sorted()) }
      val trials = halves.flatten().sortedBy { it.first }
      if (trials.isEmpty()) return@repeat
      val tr = TimeTrialTrials(trials.map { it.first }, trials.map { it.second }, trials.map { it.third }.runningFold(1) { a, b -> maxOf(a, b) }.drop(1))
      val p = TimeTrialMarks.paceOf(TimeTrialResult(gameId = "stroop", day = "2026-10-28", trackVersion = 2, hits = tr.hits, errors = tr.errors, level = 3, trials = tr))!!
      assertEquals("aciertos y errores de los 4 medios minutos suman los de la partida", tr.hits to tr.errors, p.halves.sumOf { it.hits } to p.halves.sumOf { it.errors })
      assertEquals(4, p.halves.size)
      for (line in listOf(p.bestLine, p.errorsLine, p.sentence)) {
        seen += line
        assertTrue("«$line» termina en punto", line.endsWith("."))
        val low = line.lowercase()
        for (bad in listOf("velocidad", "capacidad", "rendimiento", "rápido", "rapidez", "nivel", "puntaje", "edad mental", "ranking", "percentil", "cerebro", "cognitiv", "entren"))
          assertFalse("«$line» dice «$bad»", bad in low)
        // quitadas las cifras con su unidad («14 aciertos», «3 errores», «paso 4», «pasos 3 y 4», «entre el paso 2 y el 4 de la pista», «el de 30 a 60 segundos»), no queda ningún número suelto
        val rest = line.replace(Regex("""\d+ (aciertos?|errores)"""), "").replace(Regex("""pasos? \d+"""), "").replace(Regex("""y (el )?\d+ de la pista"""), "").replace(Regex("""el de \d+ a \d+"""), "")
        assertFalse("«$line» trae un número sin su unidad: «$rest»", Regex("""\d""").containsMatchIn(rest))
      }
    }
    assertTrue("el ejercicio recorrió variantes distintas de la frase (${seen.size})", seen.size > 40)
  }

  @Test
  fun `el ritmo cuenta lo mismo que el final de la partida`() {
    val g = game(half(0, 10), half(1, 14), half(2, 12, listOf(4)), half(3, 11, listOf(4, 4), hitLevel = 4))
    val p = TimeTrialMarks.pace(listOf(g), "stroop")!!
    assertEquals("los aciertos del gráfico son los de la partida", g.hits, p.halves.sumOf { it.hits })
    assertEquals("y los errores", g.errors, p.halves.sumOf { it.errors })
    assertNotNull(p.result.trials)
  }

  // ------------------------------------------------------------------ la losa «Tu ritmo en la última partida» (Tarea 201)

  @Test
  fun `los textos de la losa son los de la lamina de Fable`() {
    assertEquals("Tu ritmo en la última partida", TimeTrialMarks.PACE_TITLE)
    assertEquals("Tu ritmo aparece desde tu próxima partida del Contrarreloj.", TimeTrialMarks.PACE_WAIT)
    assertEquals("Barras lila: aciertos. Barras coral: errores.", TimeTrialMarks.PACE_LEGEND)
    assertEquals("un rótulo por medio minuto, sin abreviaturas (la regla «sin palabras sueltas» no deja «s» por «segundos»)", listOf("0 a 30", "30 a 60", "60 a 90", "90 a 120"), TimeTrialMarks.PACE_AXIS)
    assertEquals("y la unidad, escrita completa, una sola vez", "segundos", TimeTrialMarks.PACE_AXIS_UNIT)
    assertEquals(TimeTrialMarks.HALF_MINUTES, TimeTrialMarks.PACE_AXIS.size)
  }

  @Test
  fun `el lector de pantalla dice cada medio minuto con sus unidades`() {
    val p = paceOf(half(0, 11), half(1, 14), half(2, 12, listOf(4)), half(3, 9, listOf(4, 4), hitLevel = 4))
    assertEquals(
      "De 0 a 30 segundos: 11 aciertos y 0 errores. De 30 a 60 segundos: 14 aciertos y 0 errores. De 60 a 90 segundos: 12 aciertos y 1 error. De 90 a 120 segundos: 9 aciertos y 2 errores.",
      TimeTrialMarks.paceSpoken(p)
    )
    val one = paceOf(half(0, 1), half(1, 0), half(2, 0), half(3, 0))
    assertTrue("un acierto en singular y cero con su unidad", TimeTrialMarks.paceSpoken(one).startsWith("De 0 a 30 segundos: 1 acierto y 0 errores. De 30 a 60 segundos: 0 aciertos y 0 errores."))
    val low = TimeTrialMarks.paceSpoken(p).lowercase()
    for (bad in listOf("velocidad", "capacidad", "rendimiento", "nivel", "puntaje", "edad mental", "ranking", "cerebro", "cognitiv")) assertFalse("lo que se lee dice «$bad»", bad in low)
  }

  @Test
  fun `se resalta el mejor medio minuto, los empatados en lo mas alto, y ninguno si fueron parejos o no hubo aciertos`() {
    assertEquals(listOf(1), TimeTrialMarks.paceHighlight(paceOf(half(0, 10), half(1, 14), half(2, 12), half(3, 11))))
    assertEquals("los empatados en lo más alto", listOf(0, 3), TimeTrialMarks.paceHighlight(paceOf(half(0, 14), half(1, 10), half(2, 12), half(3, 14))))
    assertEquals("los cuatro parejos: no hay un «mejor» que destacar", emptyList<Int>(), TimeTrialMarks.paceHighlight(paceOf(half(0, 5), half(1, 5), half(2, 5), half(3, 5))))
    assertEquals("sin aciertos no se destaca nada", emptyList<Int>(), TimeTrialMarks.paceHighlight(paceOf(half(0, 0, listOf(1)), half(1, 0), half(2, 0), half(3, 0))))
  }
}
