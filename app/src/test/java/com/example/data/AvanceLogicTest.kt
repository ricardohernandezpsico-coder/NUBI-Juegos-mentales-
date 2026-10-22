package com.example.data

import com.example.model.GamePlayResult
import com.example.model.RankTier
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** La lógica de las medallas y la liga (Tarea 125) y la frase de la semana. Los puntos por área (Tarea 159) se prueban en `AreaPointsTest`. */
class AvanceLogicTest {
  private val day = 24L * 60 * 60 * 1000
  private fun game(id: String, dayOffset: Int) =
    GamePlayResult(gameId = id, score = 70, correctAnswers = 5, totalTrials = 10, timed = false, level = 1, timestamp = 1_700_000_000_000L + dayOffset * day)

  // ---- medallas

  @Test
  fun `la meta de un nivel dice su unidad`() {
    fun goal(id: String) = Achievements.byId(id)!!.goalText()
    assertEquals("1 partida", goal("primer_paso")); assertEquals("3 días", goal("racha_3")); assertEquals("25 partidas", goal("partidas_25")); assertEquals("100 días", goal("racha_100"))
    assertEquals("3 partidas en un día", goal("dia_completo")); assertEquals("4 áreas", goal("dominios")); assertEquals("9 juegos", goal("explorador")); assertEquals("17 juegos", goal("explorador_17"))
    assertEquals("1 regreso", goal("vuelta_1")); assertEquals("3 regresos", goal("vuelta_3")); assertEquals("10 regresos", goal("vuelta_10"))
  }

  @Test
  fun `una familia bloqueada muestra cuanto lleva y la conseguida cual es la siguiente`() {
    val none = computeAchievementStats(emptyList(), emptyMap())
    val volver = Achievements.familyState(AchievementFamily.VOLVER, emptySet(), none)
    assertFalse(volver.unlocked); assertNull(volver.number); assertEquals("0 de 3 · siguiente: volver 1 vez", volver.caption)
    val variedad = Achievements.familyState(AchievementFamily.VARIEDAD, emptySet(), none)
    assertEquals("0 de 4 · siguiente: 3 partidas en un día", variedad.caption)
    assertEquals(listOf("0 de 4", "siguiente: 3 partidas en un día"), variedad.captionLines)

    val history = (0 until 3).map { game("secuencia", it) }
    val stats = computeAchievementStats(history, emptyMap())
    val got = Achievements.unlocked(stats)
    val constancia = Achievements.familyState(AchievementFamily.CONSTANCIA, got, stats)
    assertTrue(constancia.unlocked)
    assertEquals("racha_3", constancia.reached!!.id); assertEquals(3, constancia.number)
    assertEquals("racha_3 es el segundo nivel de ocho (el primero es el primer paso)", "2 de 8 · siguiente: 7 días", constancia.caption)
    assertTrue(constancia.spoken, constancia.spoken.startsWith("Constancia, 3 días"))
  }

  @Test
  fun `el nivel mas alto de una familia no tiene siguiente`() {
    val all = Achievements.all.filter { it.family == AchievementFamily.VOLVER }.map { it.id }.toSet()
    val st = Achievements.familyState(AchievementFamily.VOLVER, all, computeAchievementStats(emptyList(), emptyMap()))
    assertNull(st.next); assertEquals("3 de 3 · completa", st.caption); assertEquals(10, st.number)
    assertEquals("2 de 3 · siguiente: volver 10 veces", Achievements.familyState(AchievementFamily.VOLVER, setOf("vuelta_1", "vuelta_3"), computeAchievementStats(emptyList(), emptyMap())).caption)
  }

  // ---- liga

  @Test
  fun `la fila de la liga`() {
    val r = leagueRow(1240)
    assertEquals(RankTier.ESMERALDA, r.tier); assertEquals("Liga Esmeralda · 1.240 trofeos", r.title); assertEquals("10 para Diamante", r.toNext)
    val zero = leagueRow(0)
    assertEquals("Liga Bronce · 0 trofeos", zero.title); assertNull("sin trofeos no hay «para Plata»", zero.toNext)
    assertEquals("Liga Bronce · 1 trofeo", leagueRow(1).title)
    assertNull("Maestro es la última", leagueRow(1600).toNext)
    assertEquals("1.240", thousands(1240)); assertEquals("999", thousands(999))
  }

  // ---- semana: la frase corta (Avance y Hoy)

  @Test
  fun `la frase de la semana se parte en lo grande y lo demas`() {
    assertEquals("3 de 4 días" to " de tu meta", Week.captionParts(3, 4))
    assertEquals("6 días" to " esta semana: meta cumplida", Week.captionParts(6, 4))
    assertEquals("1 día" to " esta semana", Week.captionParts(1, 0))
    for (d in 0..7) for (g in 0..7) assertEquals(Week.caption(d, g), Week.captionParts(d, g).let { it.first + it.second })
  }

  // ---- una sola cuenta de medallas (Tarea 151, C8)

  @Test
  fun `la celebracion y el perfil cuentan las mismas medallas y los logros anteriores no suman`() {
    val total = AchievementFamily.entries.sumOf { Achievements.levelsOf(it).size }
    assertEquals("8 de constancia + 4 de variedad + 3 de volver", 15, total)
    assertEquals(total, Achievements.all.size)
    val keys = setOf("primer_paso", "racha_3", "vuelta_1", "brillante", "liga_plata")      // dos medallas de conducta de una familia, una de otra y dos logros anteriores
    val perfil = AchievementFamily.entries.sumOf { f -> Achievements.familyState(f, keys, computeAchievementStats(emptyList(), emptyMap())).level }
    assertEquals("la celebración dice lo mismo que el Perfil", perfil, Achievements.medalCount(keys))
    assertEquals(3, Achievements.medalCount(keys))
    assertEquals(0, Achievements.medalCount(emptySet()))
    assertEquals("todas las medallas", 15, Achievements.medalCount(Achievements.all.map { it.id }.toSet()))
  }
}
