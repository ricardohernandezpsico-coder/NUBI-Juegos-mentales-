package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.util.Calendar

/**
 * Lo que el Contrarreloj dice de una partida y de la persona, SOLO contra sí misma (`TimeTrialStats`, Tarea 177): el récord, las últimas 5 partidas, la mejor de la semana, y las frases del final (con marca nueva, sin ella y la primera vez). Lógica pura.
 */
class TimeTrialStatsTest {
  private fun at(day: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, day, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  // por defecto SIN errores: la marca es igual a los aciertos; la pista de hoy es la versión 2 (la marca neta, Tarea 178)
  private fun r(hits: Int, day: Int, game: String = "stroop", version: Int = 2, errors: Int = 0, level: Int = 3, h: Int = 12) =
    TimeTrialResult(id = "r-$game-$day-$h-$hits", gameId = game, day = "2026-10-%02d".format(day), trackVersion = version, hits = hits, errors = errors, level = level, seconds = 120, timestamp = at(day, h))

  // el 26-oct-2026 es lunes: la semana empieza ese día; el 22 y el 23 son de la semana anterior
  private val history = listOf(r(31, 20), r(35, 21), r(41, 23), r(36, 24))

  @Test
  fun `el record y las ultimas cinco son del mismo juego y la pista de hoy`() {
    val all = history + r(50, 22, game = "disparate") + r(60, 25, version = 1)
    assertEquals(41, TimeTrialStats.record(all, "stroop"))
    assertEquals(50, TimeTrialStats.record(all, "disparate"))
    assertNull("nunca jugó Rescate", TimeTrialStats.record(all, "radar"))
    assertEquals("las de una pista anterior no cuentan", listOf(31, 35, 41, 36), TimeTrialStats.last(all, "stroop").map { it.hits })
    val many = (1..8).map { r(10 + it, 10 + it) }
    assertEquals("las últimas 5, de la más vieja a la más nueva", listOf(14, 15, 16, 17, 18), TimeTrialStats.last(many, "stroop").map { it.hits })
  }

  @Test
  fun `la mejor de la semana cuenta desde el lunes`() {
    val now = at(28, 18)
    val all = history + r(38, 26) + r(33, 27)
    assertEquals("lunes 26 y martes 27: 38", 38, TimeTrialStats.bestOfWeek(all, "stroop", now))
    assertNull("esta semana todavía no se jugó Rescate", TimeTrialStats.bestOfWeek(all, "radar", now))
    assertEquals("el domingo 25 todavía es la semana del 19 al 25 (la del 41)", 41, TimeTrialStats.bestOfWeek(history, "stroop", at(25, 20)))
    assertNull("y el lunes 26 la semana empieza de cero", TimeTrialStats.bestOfWeek(history, "stroop", at(26, 9)))
  }

  @Test
  fun `una marca nueva dice el record de antes y que lo supero`() {
    val today = r(43, 26)
    val o = TimeTrialStats.outcome(history + today, today)
    assertTrue(o.isRecord); assertFalse(o.isFirst)
    assertEquals(41, o.previousRecord)
    assertEquals(at(23), o.previousRecordAt)
    assertEquals("las últimas 5 con la de hoy al final", listOf(35, 41, 36, 43), o.last.takeLast(4))
    assertEquals(43, o.bestOfWeek); assertTrue(o.weekBestIsToday)
    assertEquals("Antes, tu mejor marca era 41 aciertos.", TimeTrialStats.phrase(o))
    assertEquals("La mejor de esta semana: 43 aciertos, la de hoy.", TimeTrialStats.weekLine(o))
  }

  @Test
  fun `sin marca nueva dice cuanto es el record y de hace cuantos dias`() {
    val today = r(38, 26)
    val o = TimeTrialStats.outcome(history + today, today)
    assertFalse(o.isRecord); assertFalse(o.isFirst)
    assertEquals("Tu mejor marca sigue en 41 aciertos, de hace 3 días.", TimeTrialStats.phrase(o, now = at(26, 20)))
    assertEquals("La mejor de esta semana: 38 aciertos, la de hoy.", TimeTrialStats.weekLine(o))
    // si la mejor de la semana es de otra partida de la semana
    val again = r(30, 26, h = 18)
    val o2 = TimeTrialStats.outcome(history + today + again, again)
    assertEquals("La mejor de esta semana: 38 aciertos.", TimeTrialStats.weekLine(o2))
  }

  @Test
  fun `igualar el record no es marca nueva`() {
    val today = r(41, 26)
    val o = TimeTrialStats.outcome(history + today, today)
    assertFalse(o.isRecord)
    assertEquals("Tu mejor marca sigue en 41 aciertos, de hace 3 días.", TimeTrialStats.phrase(o, now = at(26, 20)))
  }

  @Test
  fun `la primera vez no tiene record ni sello y dice como seguir`() {
    val first = r(24, 26)
    val o = TimeTrialStats.outcome(listOf(first), first)
    assertTrue(o.isFirst); assertFalse(o.isRecord); assertNull(o.previousRecord)
    assertEquals("Es tu primera marca en el Contrarreloj. La próxima te comparas contigo.", TimeTrialStats.phrase(o))
    assertNull("la primera vez no se dibujan las últimas ni la semana", TimeTrialStats.weekLine(o))
  }

  @Test
  fun `la primera vez de un juego es primera aunque haya jugado otros`() {
    val first = r(24, 26, game = "radar")
    val o = TimeTrialStats.outcome(history + first, first)
    assertTrue(o.isFirst)
  }

  @Test
  fun `los dias de antes se dicen de hoy, de ayer o de hace N dias`() {
    val now = at(26, 20)
    assertEquals("de hoy", TimeTrialStats.daysAgoText(now, at(26, 8)))
    assertEquals("de ayer", TimeTrialStats.daysAgoText(now, at(25, 23)))
    assertEquals("de hace 3 días", TimeTrialStats.daysAgoText(now, at(23)))
    assertEquals("de hace 10 días", TimeTrialStats.daysAgoText(now, at(16)))
  }

  @Test
  fun `el paso de la pista se dice con su largo y nunca pasa del ultimo`() {
    assertEquals("Llegaste al paso 4 de 5 de la pista.", TimeTrialStats.stepLine(4, 5))
    assertEquals("Llegaste al paso 5 de 5 de la pista.", TimeTrialStats.stepLine(9, 5))
    assertEquals("Llegaste al paso 1 de 12 de la pista.", TimeTrialStats.stepLine(0, 12))
  }

  @Test
  fun `el detalle trae aciertos, errores, paso, duracion y el record de una pista anterior`() {
    val today = r(43, 26, errors = 6, level = 4)
    val old = r(50, 20, version = 1)
    val o = TimeTrialStats.outcome(history + old + today, today)
    assertEquals(
      listOf("Hiciste 43 aciertos y 6 errores.", TimeTrialStats.RULE, "Llegaste al paso 4 de 5 de la pista.", "La partida duró 120 segundos.", "Tu récord de la pista 1: 50 aciertos."),
      TimeTrialStats.detailLines(o)
    )
    assertEquals("un solo error", "Hiciste 1 acierto y 1 error.", TimeTrialStats.detailLines(TimeTrialStats.outcome(listOf(r(1, 26, errors = 1)), r(1, 26, errors = 1))).first())
  }

  // ------------------------------------------------------------------ la marca neta (Tarea 178)

  @Test
  fun `la cifra grande es la marca neta y debajo se dice cuantos aciertos y cuantos errores`() {
    val today = r(38, 26, errors = 4)
    val o = TimeTrialStats.outcome(history + today, today)
    assertEquals(34, o.result.mark)
    assertEquals("38 aciertos menos 4 errores, en 2 minutos de Contrarreloj", TimeTrialStats.scoreLine(today))
    assertEquals("sin errores", "43 aciertos sin errores, en 2 minutos de Contrarreloj", TimeTrialStats.scoreLine(r(43, 26)))
    assertEquals("un solo error", "20 aciertos menos 1 error, en 2 minutos de Contrarreloj", TimeTrialStats.scoreLine(r(20, 26, errors = 1)))
    assertEquals("un solo acierto", "1 acierto menos 2 errores, en 2 minutos de Contrarreloj", TimeTrialStats.scoreLine(r(1, 26, errors = 2)))
    val floor = r(3, 26, errors = 5)
    assertEquals("nunca negativa", 0, floor.mark)
    assertEquals("3 aciertos menos 5 errores, en 2 minutos de Contrarreloj", TimeTrialStats.scoreLine(floor))
  }

  @Test
  fun `el record, las ultimas cinco y la semana usan la marca neta`() {
    val a = r(45, 21, errors = 10)                    // 35
    val b = r(40, 23, errors = 0)                     // 40 (el récord, aunque 45 aciertos de a)
    val c = r(44, 24, errors = 6)                     // 38
    val all = listOf(a, b, c)
    assertEquals(40, TimeTrialStats.record(all, "stroop"))
    assertEquals(listOf(35, 40, 38), TimeTrialStats.last(all, "stroop").map { it.mark })
    val today = r(39, 26, errors = 2)                 // 37: no supera 40
    val o = TimeTrialStats.outcome(all + today, today)
    assertFalse("37 no supera 40: manda la marca, no los aciertos (39)", o.isRecord)
    assertEquals(40, o.previousRecord)
    assertEquals(listOf(35, 40, 38, 37), o.last)
    val better = r(48, 26, errors = 3, h = 18)        // 45: sí
    val o2 = TimeTrialStats.outcome(all + today + better, better)
    assertTrue(o2.isRecord)
    assertEquals("Antes, tu mejor marca era 40 aciertos.", TimeTrialStats.phrase(o2))
  }

  @Test
  fun `las partidas de la pista 1 ya no cuentan para el record pero quedan en el detalle con sus aciertos`() {
    val v1 = r(60, 20, version = 1, errors = 30)       // pista 1: contaba solo aciertos
    val today = r(20, 26)
    val o = TimeTrialStats.outcome(listOf(v1, today), today)
    assertTrue("la primera con la pista de hoy", o.isFirst)
    assertEquals(null, TimeTrialStats.record(listOf(v1), "stroop"))
    assertEquals("con la regla de ESA pista: solo los aciertos", 1 to 60, o.olderTrackRecord)
  }

  @Test
  fun `el lector de pantalla lee el juego, la marca, el sello, la frase, el paso y la semana`() {
    val today = r(43, 26, level = 4)
    val o = TimeTrialStats.outcome(history + today, today)
    val spoken = TimeTrialStats.spoken("Tinta o Palabra", o)
    for (part in listOf("Tinta o Palabra, Contrarreloj de hoy", "¡Tu mejor marca hasta hoy!", "43 aciertos sin errores, en 2 minutos de Contrarreloj", "Antes, tu mejor marca era 41 aciertos.", "Llegaste al paso 4 de 5 de la pista.", "La mejor de esta semana: 43 aciertos, la de hoy."))
      assertTrue("«$spoken» no dice «$part»", spoken.contains(part))
  }

  @Test
  fun `los textos son los del diseno y la nota dice que no es una medida de capacidad`() {
    assertEquals("¡Tu mejor marca hasta hoy!", TimeTrialStats.SEAL)
    assertEquals("en 2 minutos de Contrarreloj", TimeTrialStats.UNIT)
    assertEquals("La marca cuenta los aciertos menos los errores, sin bajar de cero.", TimeTrialStats.RULE)
    assertEquals("Tus últimas partidas", TimeTrialStats.LAST_TITLE)
    assertEquals("Otra vez", TimeTrialStats.AGAIN)
    assertEquals("Listo", TimeTrialStats.DONE)
    assertEquals("Ver el detalle de tu partida", TimeTrialStats.DETAIL)
    assertEquals("Es el resultado de un juego, comparado solo contigo. No es una medida de capacidad.", TimeTrialStats.NOTE)
    for (t in listOf(TimeTrialStats.SEAL, TimeTrialStats.UNIT, TimeTrialStats.RULE, TimeTrialStats.NOTE, TimeTrialStats.FIRST_PHRASE, TimeTrialStats.scoreLine(r(38, 26, errors = 4))))
      for (banned in listOf("percentil", "Percentil", "nivel", "Nivel", "cerebro", "cognitiv", "entrenamiento")) assertFalse("«$t» no debe decir «$banned»", t.contains(banned))
  }
}
