package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** La racha que perdona (Tarea 124, `Streak`): ancla hoy o ayer, un perdón por cada 7 días, dos huecos cortan, el perdonado no suma, nadie baja. Lógica pura: los días son números. */
class StreakTest {
  private val today = 1000L

  private fun days(vararg offsets: Int): Set<Long> = offsets.map { today + it }.toSet()

  private fun cur(vararg offsets: Int) = Streak.compute(days(*offsets), today)

  @Test
  fun `sin partidas no hay racha y queda un perdon disponible`() {
    val s = Streak.compute(emptySet(), today)
    assertEquals(0, s.current); assertNull(s.forgivenDay); assertTrue(s.graceAvailable)
  }

  @Test
  fun `la ancla es hoy si ya jugo y ayer si no`() {
    assertEquals(3, cur(0, -1, -2).current)
    assertEquals("hoy todavia no rompe la racha", 3, cur(-1, -2, -3).current)
    assertEquals("solo hoy", 1, cur(0).current)
  }

  @Test
  fun `un dia sin partida se perdona y no suma`() {
    val s = cur(0, -1, -3, -4)        // hoy, ayer, (antier perdonado), hace 3 y 4
    assertEquals("cuenta los 4 días jugados, no el perdonado", 4, s.current)
    assertEquals(today - 2, s.forgivenDay)
    assertFalse("el perdón de esta semana ya se usó", s.graceAvailable)
  }

  @Test
  fun `dos huecos seguidos cortan la racha`() {
    assertEquals(2, cur(0, -1, -4, -5).current)        // faltaron antier y hace 3: la racha es solo hoy y ayer
    assertEquals("ayer faltó y hoy todavía no se juega: un solo hueco, se perdona", 2, cur(-2, -3).current)
    assertEquals(today - 1, cur(-2, -3).forgivenDay)
    assertEquals("faltaron ayer y antier: son dos seguidos", 0, cur(-3, -4).current)
  }

  @Test
  fun `a lo mas un perdon por cada 7 dias corridos`() {
    // huecos en -2 y en -6: están a 4 días → el segundo corta (cuentan 0, -1, -3, -4, -5)
    assertEquals(5, cur(0, -1, -3, -4, -5, -7).current)
    // huecos en -2 y en -9: están a 7 días → los dos se perdonan
    val s = cur(0, -1, -3, -4, -5, -6, -7, -8, -10, -11)
    assertEquals("0, -1, -3 a -8, -10 y -11 = 10 días jugados con dos perdones separados por 7 días", 10, s.current)
    assertEquals(today - 2, s.forgivenDay)
  }

  @Test
  fun `un hueco al final de la racha no es un perdon`() {
    val s = cur(0, -2)               // ayer faltó, antier sí, y antes de eso nada: el perdón de ayer sí alarga (antier se cuenta)
    assertEquals(2, s.current)
    val t = cur(0)                    // ayer faltó pero no hay nada antes: no hay perdón que mostrar
    assertEquals(1, t.current); assertNull(t.forgivenDay); assertTrue(t.graceAvailable)
  }

  @Test
  fun `descansaste el martes`() {
    // el día 0 (1-ene-1970) fue jueves: hoy = día 1000 → jueves + 1000 mod 7
    val todayName = Streak.weekdayName(today)
    val s = cur(0, -1, -3)
    assertEquals(Streak.weekdayName(today - 2), s.forgivenDay?.let { Streak.weekdayName(it) })
    assertEquals("Descansaste el ${Streak.weekdayName(today - 2)}: tu racha sigue.", Streak.forgivenLine(s))
    assertNull("sin perdón no hay frase", Streak.forgivenLine(cur(0, -1, -2)))
    assertEquals("el 1-ene-1970 fue jueves", "jueves", Streak.weekdayName(0))
    assertEquals("el 2-ene-1970 fue viernes", "viernes", Streak.weekdayName(1))
    assertEquals("el 7-ene-1970 fue miércoles", "miércoles", Streak.weekdayName(6))
    assertTrue(todayName.isNotBlank())
  }

  @Test
  fun `el perdon de hace mas de 7 dias ya no se muestra y vuelve a quedar uno`() {
    val s = Streak.compute(setOf(today, today - 1, today - 2, today - 3, today - 4, today - 5, today - 6, today - 7, today - 9), today)      // hueco en -8: hace 8 días
    assertEquals(9, s.current)
    assertNull("el hueco fue hace 8 días: fuera de la ventana de 7", s.forgivenDay)
    assertTrue(s.graceAvailable)
  }

  @Test
  fun `nadie baja la racha perdona siempre es mayor o igual a la estricta`() {
    val rnd = java.util.Random(7)
    repeat(300) {
      val played = (-60..0).filter { rnd.nextInt(100) < 70 }.map { today + it }.toSet()
      val strict = strictCurrent(played, today)
      val forgiving = Streak.compute(played, today).current
      assertTrue("la estricta $strict y la que perdona $forgiving con $played", forgiving >= strict)
      assertTrue(Streak.longest(played) >= Streak.longestStrict(played))
    }
  }

  /** La racha de antes (`calculateStreak`): días seguidos desde hoy, o desde ayer si hoy no se jugó. */
  private fun strictCurrent(played: Set<Long>, today: Long): Int {
    var d = if (today in played) today else today - 1
    var n = 0
    while (d in played) { n++; d-- }
    return n
  }

  @Test
  fun `la racha mas larga usa la misma regla y la estricta queda de referencia`() {
    assertEquals(0, Streak.longest(emptyList()))
    // 1,2,3,(4 perdonado),5,6 → 5 días jugados seguidos con un perdón
    assertEquals(5, Streak.longest(listOf(1L, 2L, 3L, 5L, 6L)))
    assertEquals(3, Streak.longestStrict(listOf(1L, 2L, 3L, 5L, 6L)))
    assertEquals(1, Streak.longest(listOf(4L, 4L)))
  }

  @Test
  fun `con los datos de hoy nadie baja una racha de 20 dias seguidos queda de 20`() {
    val played = (-19..0).map { today + it }.toSet()
    assertEquals(20, Streak.compute(played, today).current)
    assertEquals(20, Streak.longest(played))
    // los datos de ejemplo de la lámina (20 días con dos descansos): la racha que perdona es ≥ la estricta
    val sample = ((-20..-1).filter { it != -9 && it != -4 }).map { today + it }.toSet()
    assertTrue(Streak.compute(sample, today).current >= strictCurrent(sample, today))
  }
}
