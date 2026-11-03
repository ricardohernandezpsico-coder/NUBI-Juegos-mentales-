package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.util.Calendar

/**
 * Lo que dice la página «Contrarreloj» de Avance (`TimeTrialMarks`, Tarea 199), solo contigo y de lo guardado en `time_trial_results`: las últimas 10 marcas con el récord, esta semana y la pasada, los dos promedios, los aciertos y errores, la pista anterior y los textos que lee el lector de pantalla. Lógica pura.
 * Los datos son los de la lámina de Fable (§11.3), con aciertos y errores que SÍ restan a la marca.
 */
class TimeTrialMarksTest {
  private fun at(day: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, day, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(mark: Int, day: Int, game: String = "stroop", version: Int = TimeTrialDay.trackVersion(game), errors: Int = 2, level: Int = 4, h: Int = 12) =
    TimeTrialResult(id = "r-$game-$day-$h-$mark", gameId = game, day = "2026-10-%02d".format(day), trackVersion = version, hits = mark + errors, errors = errors, level = level, seconds = 120, timestamp = at(day, h))

  /** Las diez marcas de la lámina: 31, 35, 39, 36, 44 (el récord, 17 de octubre; 46 aciertos y 2 errores), 38, 41, 37, 42 y 43 (hoy, miércoles 28); las últimas cinco con 3 errores cada una. */
  private val ten = listOf(
    r(31, 8), r(35, 10), r(39, 13), r(36, 15), r(44, 17), r(38, 19, errors = 3), r(41, 22, errors = 3), r(37, 26, errors = 3), r(42, 27, errors = 3), r(43, 28, errors = 3)
  )
  private val now = at(28, 18)

  @Test
  fun `las ultimas diez marcas llevan el record en su lugar`() {
    val p = TimeTrialMarks.page(ten, "stroop", now)
    assertEquals(listOf(31, 35, 39, 36, 44, 38, 41, 37, 42, 43), p.marks.map { it.mark })
    assertEquals(10, p.total)
    assertEquals(44, p.recordMark)
    assertEquals("el 17 de octubre", at(17), p.record?.timestamp)
    assertEquals("el récord es la quinta barra", 4, p.recordIndex)
    assertEquals(46, p.record?.hits); assertEquals(2, p.record?.errors)
  }

  @Test
  fun `con mas de diez partidas se ven las ultimas diez y un record viejo ya no tiene lugar`() {
    val older = listOf(r(60, 1), r(20, 2)) + ten
    val p = TimeTrialMarks.page(older, "stroop", now)
    assertEquals(10, p.marks.size)
    assertEquals("las dos primeras ya no se ven: quedan las diez de la lámina", listOf(31, 35, 39, 36, 44, 38, 41, 37, 42, 43), p.marks.map { it.mark })
    assertEquals(12, p.total)
    assertEquals(60, p.recordMark)
    assertNull("el récord de 60 quedó fuera de las diez: no hay moneda", p.recordIndex)
  }

  @Test
  fun `con una marca repetida el record es la primera vez que se hizo`() {
    val p = TimeTrialMarks.page(listOf(r(30, 20), r(44, 21), r(44, 22), r(40, 23)), "stroop", now)
    assertEquals(at(21), p.record?.timestamp)
    assertEquals(1, p.recordIndex)
  }

  @Test
  fun `esta semana cuenta desde el lunes`() {
    val p = TimeTrialMarks.page(ten, "stroop", now)
    assertEquals("lunes 26, martes 27 y miércoles 28", TimeTrialMarks.WeekSummary(3, 43), p.thisWeek)
    val quiet = TimeTrialMarks.page(ten, "stroop", at(30, 9) + 3 * 86_400_000L)
    assertEquals("una semana más tarde no hubo partidas esta semana", TimeTrialMarks.WeekSummary(0, null), quiet.thisWeek)
  }

  @Test
  fun `los dos promedios existen solo con diez partidas`() {
    val p = TimeTrialMarks.page(ten, "stroop", now)
    assertEquals(40, p.lastFiveAverage)
    assertEquals(37, p.previousFiveAverage)
    assertEquals("Tus últimas 5 marcas promedian 40 aciertos; las 5 anteriores, 37.", TimeTrialMarks.averagesSentence(p))
    val nine = TimeTrialMarks.page(ten.drop(1), "stroop", now)
    assertNull(nine.lastFiveAverage); assertNull(nine.previousFiveAverage)
    assertNull("con nueve partidas no hay frase de promedios", TimeTrialMarks.averagesSentence(nine))
  }

  @Test
  fun `lo gratis dice el record y la semana en frases completas, cada numero con su unidad`() {
    val p = TimeTrialMarks.page(ten, "stroop", now)
    assertEquals("Tu récord: 44 aciertos.", TimeTrialMarks.freeRecordLine(p.recordMark))
    assertEquals("Tu récord: 1 acierto.", TimeTrialMarks.freeRecordLine(1))
    // Tarea 215b (Fable A4 c): sin marcas en el juego, UNA línea; ni «récord» ni la semana
    assertEquals("Todavía no tienes marcas en este juego.", TimeTrialMarks.freeRecordLine(null))
    assertEquals(TimeTrialMarks.NO_MARKS_FREE, TimeTrialMarks.freeRecordLine(null))
    // Tarea 215b (Fable M5 y A4 b): «y la mejor fue de N aciertos» y, si la mejor ES el récord, «y la mejor fue tu récord» sin número
    assertEquals("Esta semana jugaste 3 partidas y la mejor fue de 43 aciertos.", TimeTrialMarks.weekLine(p.thisWeek, p.recordMark))
    assertEquals("sin récord que comparar, con el número", "Esta semana jugaste 3 partidas y la mejor fue de 43 aciertos.", TimeTrialMarks.weekLine(p.thisWeek))
    assertEquals("Esta semana jugaste 3 partidas y la mejor fue tu récord.", TimeTrialMarks.weekLine(TimeTrialMarks.WeekSummary(3, 44), 44))
    // Tarea 217 (Fable M3): con UNA sola partida «la mejor» sobra
    assertEquals("Esta semana jugaste 1 partida y fue de 1 acierto.", TimeTrialMarks.weekLine(TimeTrialMarks.WeekSummary(1, 1), 7))
    assertEquals("con una sola, «fue tu récord»", "Esta semana jugaste 1 partida y fue tu récord.", TimeTrialMarks.weekLine(TimeTrialMarks.WeekSummary(1, 31), 31))
    assertEquals("la mejor de esta semana nunca pasa del récord: con una marca mayor no es «tu récord»", "Esta semana jugaste 2 partidas y la mejor fue de 30 aciertos.", TimeTrialMarks.weekLine(TimeTrialMarks.WeekSummary(2, 30), 44))
    // Tarea 215b (Fable A3): el lector dice también el juego
    assertEquals("Jugar 2 minutos de Acoplamiento", TimeTrialMarks.playSpoken("Acoplamiento"))
    assertEquals("Esta semana todavía no jugaste.", TimeTrialMarks.weekLine(TimeTrialMarks.WeekSummary(0, null)))
    assertEquals("sin partida esta semana (los días siguientes)", "Esta semana todavía no jugaste.", TimeTrialMarks.weekLine(TimeTrialMarks.page(ten, "stroop", at(30, 9) + 3 * 86_400_000L).thisWeek))
    assertEquals("Jugar · 2 minutos", TimeTrialMarks.PLAY)
    for (t in listOf(TimeTrialMarks.freeRecordLine(44), TimeTrialMarks.weekLine(p.thisWeek), TimeTrialMarks.weekLine(p.thisWeek, p.recordMark), TimeTrialMarks.NO_MARKS_FREE, TimeTrialMarks.playSpoken("Acoplamiento"), TimeTrialMarks.PLAY, TimeTrialMarks.SUBTITLE_PLACE, TimeTrialMarks.SUBTITLE))
      for (banned in listOf("intento", "bloquead", "desbloque", "percentil", "Percentil")) assertFalse("«$t» no debe decir «$banned»", t.contains(banned))
  }

  @Test
  fun `la linea del record dice la fecha y el paso, y no repite lo que dice la cifra`() {
    val p = TimeTrialMarks.page(ten, "stroop", now)
    assertEquals("Tu récord es del 17 de octubre; llegaste al paso 4 de 5 de la pista.", TimeTrialMarks.recordLine(p.record!!, now, p.maxLevel))
    assertEquals("el paso nunca pasa del máximo de la pista", "Tu récord es del 17 de octubre; llegaste al paso 5 de 5 de la pista.", TimeTrialMarks.recordLine(p.record!!.copy(level = 9), now, p.maxLevel))
  }

  @Test
  fun `solo cuentan las partidas del juego y de la pista de hoy y la pista anterior queda aparte`() {
    val all = ten + r(50, 20, game = "disparate") + r(38, 12, version = 1, errors = 0) + r(33, 14, version = 1, errors = 0)
    val p = TimeTrialMarks.page(all, "stroop", now)
    assertEquals(10, p.total)
    assertEquals(44, p.recordMark)
    assertEquals("la mayor marca de la pista 1", 1 to 38, p.olderTrack)
    assertEquals("Tu récord de la pista 1: 38 aciertos. Las marcas de otra pista no se comparan.", TimeTrialMarks.olderSentence(p.olderTrack!!))
    assertNull("sin pista anterior no hay sección", TimeTrialMarks.page(ten, "stroop", now).olderTrack)
    val none = TimeTrialMarks.page(all, "radar", now)
    assertFalse(none.hasMarks); assertNull(none.record); assertNull(none.recordIndex)
    assertEquals(TimeTrialMarks.WeekSummary(0, null), none.thisWeek)
  }

  @Test
  fun `el eje dice la fecha de la primera barra que se ve y hoy a la derecha`() {
    val p = TimeTrialMarks.page(ten, "stroop", now)
    assertEquals("8 de octubre" to "hoy", TimeTrialMarks.axisLabels(p.marks, now))
    assertEquals("con las últimas cinco, la fecha de la sexta partida, no la de la primera", "19 de octubre" to "hoy", TimeTrialMarks.axisLabels(p.marks.takeLast(5), now))
    assertEquals("si la última no es de hoy dice su fecha", "8 de octubre" to "28 de octubre", TimeTrialMarks.axisLabels(p.marks, at(30, 10)))
    assertEquals("un solo día: solo la izquierda", "hoy" to null, TimeTrialMarks.axisLabels(listOf(r(30, 28, h = 9), r(31, 28, h = 10)), now))
    assertNull(TimeTrialMarks.axisLabels(emptyList(), now))
  }

  @Test
  fun `el lector de pantalla dice las barras con el record y la de hoy`() {
    val p = TimeTrialMarks.page(ten, "stroop", now)
    assertEquals(
      "Tus últimas 10 marcas: 31, 35, 39, 36, 44 aciertos, tu récord, 38, 41, 37, 42 y 43 aciertos, la de hoy.",
      TimeTrialMarks.barsSpoken(p.marks, p.recordIndex, now)
    )
    assertEquals("la última de ayer", "Tus últimas 10 marcas: 31, 35, 39, 36, 44 aciertos, tu récord, 38, 41, 37, 42 y 43 aciertos, la de ayer.", TimeTrialMarks.barsSpoken(p.marks, p.recordIndex, at(29, 10)))
    assertEquals("Tus últimas 5 marcas: 38, 41, 37, 42 y 43 aciertos, la de hoy.", TimeTrialMarks.barsSpoken(p.marks.takeLast(5), null, now))
    assertEquals("Tu primera marca: 31 aciertos.", TimeTrialMarks.barsSpoken(listOf(r(31, 28)), 0, now))
    assertEquals("dos marcas: «a y b»", "Tus últimas 2 marcas: 30 y 31 aciertos, la de hoy.", TimeTrialMarks.barsSpoken(listOf(r(30, 27), r(31, 28)), null, now))
  }

  @Test
  fun `las frases dicen cada numero con su unidad`() {
    val p = TimeTrialMarks.page(ten, "stroop", now)
    assertEquals("Llegaste al paso 4 de 5 de la pista.", TimeTrialStats.stepLine(p.record!!.level, p.maxLevel))
    assertEquals("1 error", TimeTrialMarks.errorsText(1)); assertEquals("2 errores", TimeTrialMarks.errorsText(2)); assertEquals("0 errores", TimeTrialMarks.errorsText(0))
    assertEquals("Tus últimas 10 marcas", TimeTrialMarks.barsTitle(10)); assertEquals("Tus últimas 5 marcas", TimeTrialMarks.barsTitle(5)); assertEquals("Tus últimas marcas", TimeTrialMarks.barsTitle(1))
  }

  @Test
  fun `el selector dice el record, cual es el de hoy y cuales piden Nubilus Plus`() {
    assertEquals("44 aciertos", TimeTrialMarks.tileValue(44)); assertEquals("sin marca aún", TimeTrialMarks.tileValue(null))
    assertEquals("Tinta o Palabra, récord 44 aciertos, elegido", TimeTrialMarks.tileSpoken("Tinta o Palabra", 44, true))
    assertEquals("Acoplamiento, sin marca aún", TimeTrialMarks.tileSpoken("Acoplamiento", null, false))
    // Tarea 215: el de hoy lo dice; en gris, «con Nubilus Plus» (el candado y el gris no se leen)
    assertEquals("Tinta o Palabra, récord 44 aciertos, el de hoy, elegido", TimeTrialMarks.tileSpoken("Tinta o Palabra", 44, selected = true, today = true))
    assertEquals("Rescate relámpago, récord 12 aciertos, con Nubilus Plus", TimeTrialMarks.tileSpoken("Rescate relámpago", 12, selected = false, locked = true))
    assertEquals("Acoplamiento, sin marca aún, con Nubilus Plus", TimeTrialMarks.tileSpoken("Acoplamiento", null, selected = false, locked = true))
    assertEquals("Hoy", TimeTrialMarks.TODAY_TAG)
  }

  // ------------------------------------------------------------------ Rescate relámpago por cápsula (Tarea 208): varias cápsulas en el mismo instante

  @Test
  fun `el ritmo con tiempos repetidos cuenta las capsulas de una ronda en el mismo medio minuto`() {
    // dos rondas: la primera a los 40 s con 2 bien y 1 de más (tres cápsulas en el mismo ms), la segunda a los 100 s con 3 bien
    val trials = TimeTrialTrials.of(listOf(40_000, 40_000, 40_000, 100_000, 100_000, 100_000), listOf(1, 1, 0, 1, 1, 1), listOf(1, 1, 1, 1, 1, 1))!!
    val result = TimeTrialResult(gameId = "radar", day = "2026-10-26", trackVersion = TimeTrialDay.trackVersion("radar"), hits = 5, errors = 1, level = 1, timestamp = 1_000L, trials = trials)
    assertTrue("el ritmo está completo: tantos elementos como aciertos más errores", result.hasPace)
    val pace = TimeTrialMarks.paceOf(result)!!
    assertEquals("0 a 30: nada", Pair(0, 0), pace.halves[0].let { it.hits to it.errors })
    assertEquals("30 a 60: 2 aciertos y 1 error de la misma ronda", Pair(2, 1), pace.halves[1].let { it.hits to it.errors })
    assertEquals("60 a 90: nada", Pair(0, 0), pace.halves[2].let { it.hits to it.errors })
    assertEquals("90 a 120: 3 aciertos", Pair(3, 0), pace.halves[3].let { it.hits to it.errors })
    assertEquals("el mejor medio minuto es el último", listOf(3), pace.bestHalves)
  }
}
