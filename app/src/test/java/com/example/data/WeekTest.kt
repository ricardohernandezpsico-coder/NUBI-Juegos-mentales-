package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/** La semana de farolitos de Hoy: lunes a domingo, los días con partida encendidos y el día de hoy marcado. */
class WeekTest {
  private val day = 86_400_000L
  private val dayOf: (Long) -> Long = { it / day }
  // El día 100 es un miércoles (offset 2): el lunes es el 98 y el domingo el 104.
  private val now = 100 * day + 5 * 3_600_000L

  @Test
  fun los_dias_jugados_de_esta_semana_se_encienden_de_lunes_a_domingo() {
    val plays = listOf(98 * day + 1000, 98 * day + 9000, 100 * day + 1000)   // lunes (dos partidas) y hoy
    val w = Week.lamps(plays, now, dayOf, mondayOffset = 2)
    assertEquals(listOf(true, false, true, false, false, false, false), w.played)
    assertEquals(2, w.todayIndex)
    assertEquals("dos partidas el mismo día cuentan UN día", 2, w.daysPlayed)
  }

  @Test
  fun lo_de_otras_semanas_no_cuenta() {
    val w = Week.lamps(listOf(97 * day, 105 * day, 50 * day), now, dayOf, mondayOffset = 2)   // domingo anterior, lunes siguiente y mucho antes
    assertEquals(0, w.daysPlayed)
    assertEquals(List(7) { false }, w.played)
  }

  @Test
  fun el_domingo_es_el_ultimo_farolito_y_el_lunes_el_primero() {
    val sunday = 104 * day + 3000
    assertEquals(6, Week.lamps(listOf(sunday), sunday, dayOf, mondayOffset = 6).todayIndex)
    assertEquals(true, Week.lamps(listOf(sunday), sunday, dayOf, mondayOffset = 6).played[6])
    val monday = 98 * day + 3000
    val w = Week.lamps(listOf(monday), monday, dayOf, mondayOffset = 0)
    assertEquals(0, w.todayIndex)
    assertEquals(true, w.played[0])
  }

  @Test
  fun la_frase_dice_los_dias_de_la_meta_en_palabras() {
    assertEquals("3 de 4 días de tu meta", Week.caption(3, 4))
    assertEquals("0 de 4 días de tu meta", Week.caption(0, 4))
    assertEquals("1 de 1 día de tu meta", Week.caption(1, 1))
    assertEquals("4 de 4 días de tu meta", Week.caption(4, 4))
    assertEquals("6 días esta semana: meta cumplida", Week.caption(6, 4))
    assertEquals("2 días esta semana", Week.caption(2, 0))
    assertEquals("1 día esta semana", Week.caption(1, 0))
    assertEquals("Tu semana: 3 días con partida", Week.lampsSpoken(3))
    assertEquals("Tu semana: 1 día con partida", Week.lampsSpoken(1))
    assertEquals("Tu semana: ningún día con partida todavía", Week.lampsSpoken(0))
  }

  @Test
  fun lampsFor_enciende_el_dia_de_hoy_y_los_de_las_partidas_de_la_semana() {
    val now = System.currentTimeMillis()
    val empty = Week.lampsFor(emptyList(), now)
    assertEquals(7, empty.played.size)
    assertEquals(0, empty.daysPlayed)
    assertTrue("hoy es un día de la semana (0 a 6)", empty.todayIndex in 0..6)
    val today = Week.lampsFor(listOf(now), now)
    assertEquals(1, today.daysPlayed)
    assertTrue("la partida de hoy enciende el farolito de hoy", today.played[today.todayIndex])
  }

  // ---- «esta semana» empieza el lunes a las 00:00 en TODA la app (Tarea 151, B4.1)

  private fun local(y: Int, m: Int, d: Int, h: Int = 0, min: Int = 0): Long = java.util.Calendar.getInstance().apply { clear(); set(y, m, d, h, min, 0) }.timeInMillis

  @Test
  fun la_semana_empieza_el_lunes_a_las_00_00_hora_local() {
    val oct = java.util.Calendar.OCTOBER
    val monday = local(2026, oct, 19)
    assertEquals("un miércoles", monday, Week.weekStart(local(2026, oct, 21, 12)))
    assertEquals("el lunes a las 00:00 en punto", monday, Week.weekStart(monday))
    assertEquals("el lunes a las 23:59", monday, Week.weekStart(local(2026, oct, 19, 23, 59)))
    assertEquals("el domingo a las 23:59 todavía es de esa semana", monday, Week.weekStart(local(2026, oct, 25, 23, 59)))
    assertEquals("el lunes siguiente empieza otra", local(2026, oct, 26), Week.weekStart(local(2026, oct, 26, 0, 1)))
    assertEquals("la semana pasada", local(2026, oct, 12), Week.weekStart(local(2026, oct, 21, 12), weeksBack = 1))
    assertEquals("hace dos semanas", local(2026, oct, 5), Week.weekStart(local(2026, oct, 21, 12), weeksBack = 2))
  }

  @Test
  fun los_minutos_de_la_ruta_son_una_sola_cifra() {
    assertEquals(2, DailyPath.MINUTES_PER_GAME)
    assertEquals(6, DailyPath.routeMinutes(DailyPath.SIZE))
    assertEquals("unos 6 minutos", DailyPath.minutesText(3))
    assertEquals("unos 4 minutos", DailyPath.minutesText(2))
  }
}

