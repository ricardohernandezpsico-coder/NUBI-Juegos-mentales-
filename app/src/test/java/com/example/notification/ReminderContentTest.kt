package com.example.notification

import com.example.NoLossGuard
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class ReminderContentTest {
  private fun input(
    streak: Int = 0, playedToday: Boolean = false, completed: Int = 0, week: Int = 0, goal: Int = 4,
    next: String? = "Secuencia Lumínica", gap: Int? = 1, name: String = "Ricardo", day: Int = 0
  ) = ReminderInput(name, streak, playedToday, completed, week, goal, next, gap, day)

  @Test
  fun `sesion completa no avisa`() {
    assertNull(buildReminder(input(playedToday = true, completed = 3)))
  }

  @Test
  fun `sesion a medias dice cuanto falta`() {
    val m = buildReminder(input(playedToday = true, completed = 2))!!
    assertEquals("Ricardo, ¡ya casi!", m.title)
    assertTrue(m.text.contains("Te falta 1 juego"))
  }

  @Test
  fun `racha en juego y medalla al llegar a un hito`() {
    val normal = buildReminder(input(streak = 4))!!
    assertEquals("Ricardo, ¡llevas 4 días seguidos!", normal.title)
    assertTrue(normal.text.contains("5 días"))
    val milestone = buildReminder(input(streak = 6))!!
    assertEquals("¡Hoy llegas a 7 días seguidos!", milestone.title)
  }

  @Test
  fun `vuelve sin reproches despues de varios dias`() {
    val m = buildReminder(input(gap = 5))!!
    assertEquals("Ricardo, ¡qué bueno verte por aquí!", m.title)
    val t = (m.title + " " + m.text).lowercase()
    listOf("falt", "dejaste", "abandon", "perdiste", "no jugaste", "hace días").forEach { assertFalse(t, t.contains(it)) }
  }

  @Test
  fun `meta semanal aparece si falta y sin nombre no queda coma`() {
    val m = buildReminder(input(name = "", week = 1, goal = 4, day = 0))!!
    assertEquals("¡Tu ruta de hoy está lista!", m.title)
    assertTrue(m.text.contains("Llevas 1 de 4 días esta semana."))
    val done = buildReminder(input(week = 4, goal = 4, day = 0))!!
    assertFalse(done.text.contains("Llevas"))
  }

  @Test
  fun `los minutos de la ruta salen de una sola cifra`() {
    // Tarea 151 (B4.2): Hoy dice «unos 6 minutos» (2 por juego) y los avisos decían «unos 5»: ahora los dos salen de DailyPath.MINUTES_PER_GAME
    val six = com.example.data.DailyPath.minutesText(com.example.data.DailyPath.SIZE)
    assertEquals("unos 6 minutos", six)
    val invitations = (0..2).map { buildReminder(input(week = 1, goal = 4, day = it))!! } + listOf(buildReminder(input(streak = 3, day = 0))!!)
    assertTrue("alguna invitación dice los minutos de la ruta", invitations.any { it.text.contains(six) })
    for (m in invitations) assertFalse(m.text, m.text.contains("5 minutos"))
  }

  @Test
  fun `hay un solo tono y no depende de ninguna preferencia`() {
    // Nubi vuelve a un solo tono (el 3-oct se quitaron las variantes): el mismo mensaje para la misma situación.
    assertEquals(buildReminder(input(streak = 4)), buildReminder(input(streak = 4)))
  }

  @Test
  fun `sin promesas de salud ni culpa`() {
    val all = (0 until 6).mapNotNull { buildReminder(input(day = it)) } +
      listOfNotNull(buildReminder(input(streak = 3)), buildReminder(input(gap = 9)), buildReminder(input(playedToday = true, completed = 1)))
    all.forEach { m ->
      val t = (m.title + " " + m.text).lowercase()
      listOf("neurona", "cerebro", "reserva cognitiva", "salud", "deterioro", "perdiste", "no jugaste").forEach { assertFalse(t, t.contains(it)) }
    }
  }

  /** La guardia «sin pérdida» (A7, Tarea 131): ningún recordatorio usa el miedo a perder ni apura. Se revisan los 4 casos de `buildReminder` (racha, hito, regreso, sesión a medias) y la semana con y sin meta. */
  @Test
  fun `los recordatorios no usan el miedo a perder ni apuran`() {
    val all = (0 until 6).mapNotNull { buildReminder(input(day = it)) } +
      listOfNotNull(
        buildReminder(input(streak = 3)), buildReminder(input(streak = 6)), buildReminder(input(streak = 4)), buildReminder(input(gap = 9)), buildReminder(input(gap = 5)), buildReminder(input(playedToday = true, completed = 2)),
        buildReminder(input(playedToday = true, completed = 1)), buildReminder(input(week = 1, goal = 4)), buildReminder(input(week = 4, goal = 4)), buildReminder(input(name = ""))
      )
    assertTrue("no se armó ningún recordatorio", all.isNotEmpty())
    all.forEach { m ->
      val t = m.title + " " + m.text
      assertTrue("«$t» usa «${NoLossGuard.hit(t, NoLossGuard.reminderRoots)}»", NoLossGuard.hit(t, NoLossGuard.reminderRoots) == null)
    }
    // la guardia misma: atrapa lo que apura y deja pasar el tono de Nubi
    for (bad in listOf("¡No pierdas tu racha!", "Última oportunidad", "Solo hoy", "¡Apúrate!", "Hazlo rápido", "Te olvidaste de jugar")) assertTrue("no atrapa «$bad»", NoLossGuard.hit(bad, NoLossGuard.reminderRoots) != null)
    for (good in listOf("Tu ruta de hoy está lista", "Descansaste el martes: tu racha sigue.", "Un rato corto hoy también suma.")) assertTrue("atrapa «$good» sin motivo", NoLossGuard.hit(good, NoLossGuard.reminderRoots) == null)
  }
}
