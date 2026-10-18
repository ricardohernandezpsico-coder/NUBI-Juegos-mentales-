package com.example.data

import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.RankTier
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** La lógica de Avance y Juegos (Tarea 125): los textos de las medallas, la fila de la liga, la semana, las líneas del cambio, «Lo que descubriste» y el historial en palabras. */
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
    assertFalse(volver.unlocked); assertNull(volver.number); assertEquals("Nivel 0 de 3 · siguiente: volver 1 vez", volver.caption)
    val variedad = Achievements.familyState(AchievementFamily.VARIEDAD, emptySet(), none)
    assertEquals("Nivel 0 de 4 · siguiente: 3 partidas en un día", variedad.caption)
    assertEquals(listOf("Nivel 0 de 4", "siguiente: 3 partidas en un día"), variedad.captionLines)

    val history = (0 until 3).map { game("secuencia", it) }
    val stats = computeAchievementStats(history, emptyMap())
    val got = Achievements.unlocked(stats)
    val constancia = Achievements.familyState(AchievementFamily.CONSTANCIA, got, stats)
    assertTrue(constancia.unlocked)
    assertEquals("racha_3", constancia.reached!!.id); assertEquals(3, constancia.number)
    assertEquals("racha_3 es el segundo nivel de ocho (el primero es el primer paso)", "Nivel 2 de 8 · siguiente: 7 días", constancia.caption)
    assertTrue(constancia.spoken.startsWith("Constancia, nivel 3 días"))
  }

  @Test
  fun `el nivel mas alto de una familia no tiene siguiente`() {
    val all = Achievements.all.filter { it.family == AchievementFamily.VOLVER }.map { it.id }.toSet()
    val st = Achievements.familyState(AchievementFamily.VOLVER, all, computeAchievementStats(emptyList(), emptyMap()))
    assertNull(st.next); assertEquals("Nivel 3 de 3 · completa", st.caption); assertEquals(10, st.number)
    assertEquals("Nivel 2 de 3 · siguiente: volver 10 veces", Achievements.familyState(AchievementFamily.VOLVER, setOf("vuelta_1", "vuelta_3"), computeAchievementStats(emptyList(), emptyMap())).caption)
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

  // ---- semana

  @Test
  fun `la frase de la semana se parte en lo grande y lo demas`() {
    assertEquals("3 de 4 días" to " de tu meta", Week.captionParts(3, 4))
    assertEquals("6 días" to " esta semana: meta cumplida", Week.captionParts(6, 4))
    assertEquals("1 día" to " esta semana", Week.captionParts(1, 0))
    for (d in 0..7) for (g in 0..7) assertEquals(Week.caption(d, g), Week.captionParts(d, g).let { it.first + it.second })
  }

  @Test
  fun `las areas de la semana cuentan desde el lunes`() {
    val monday = 100L * day
    fun dayOf(t: Long) = t / day
    val entries = listOf(monday + 1 to "MEMORIA", monday + 3 * day to "ATENCION", monday - day to "LENGUAJE", monday + 4 * day to null, monday + 3 * day + 5 to "ATENCION")
    assertEquals("miércoles (offset 2): solo Memoria; el domingo anterior no cuenta", 1, Week.areasPlayed(entries, monday + 2 * day, ::dayOf, 2))
    assertEquals("viernes (offset 4): Memoria y Atención; el juego retirado (sin área) no cuenta", 2, Week.areasPlayed(entries, monday + 4 * day, ::dayOf, 4))
  }

  @Test
  fun `el resumen de la semana y sus estados vacios`() {
    val lamps = Week.Lamps(listOf(true, true, false, true, false, false, false), todayIndex = 3)
    val s = weekSummary(lamps, goal = 4, streak = 3, areas = 2, noGamesAtAll = false)
    assertEquals("3 de 4 días", s.big); assertEquals(" de tu meta", s.rest); assertEquals("Racha: 3 días", s.streakPill); assertEquals("Áreas esta semana: 2 de 4", s.areasLine); assertFalse(s.empty)
    assertEquals("Racha: 1 día", weekSummary(lamps, 4, 1, 2, false).streakPill)
    assertNull("sin racha no hay píldora", weekSummary(lamps, 4, 0, 2, false).streakPill)
    val empty = weekSummary(Week.Lamps(List(7) { false }, 0), 4, 0, 0, noGamesAtAll = true)
    assertTrue(empty.empty); assertEquals("Tu semana empieza hoy", empty.big); assertEquals("Juega tu ruta de hoy y se enciende la primera farola.", empty.areasLine); assertNull(empty.streakPill)
  }

  // ---- el cambio de un área

  private fun status(value: Float?, change: Float, weeks: List<Float?>) = AreaStatus("MEMORIA", value, change, weeks)

  @Test
  fun `la linea del cambio de la tarjeta de un area`() {
    val weeks = listOf(0.4f, 0.45f, 0.49f, 0.52f)
    assertEquals("Subió 3 puntos esta semana", AreaProgress.cardLine(status(0.52f, 0.03f, weeks)))
    assertEquals("Subió 1 punto esta semana", AreaProgress.cardLine(status(0.52f, 0.01f, weeks)))
    assertEquals("Igual que la semana pasada", AreaProgress.cardLine(status(0.52f, 0f, weeks)))
    assertEquals("Bajó 2 puntos: es normal que varíe", AreaProgress.cardLine(status(0.52f, -0.02f, weeks)))
    assertNull("la primera semana (un solo valor) no tiene con qué compararse", AreaProgress.cardLine(status(0.52f, 0f, listOf(null, null, null, 0.52f))))
    assertNull("sin medir no hay línea", AreaProgress.cardLine(status(null, 0f, listOf(null, null, null, null))))
  }

  // ---- lo que descubriste

  private fun points(key: String, values: List<Float>, lastDaysAgo: Int, now: Long) =
    values.mapIndexed { i, v -> MeasurePoint(now - (lastDaysAgo + (values.size - 1 - i)) * day, key, v) }

  @Test
  fun `las tarjetas de lo que descubriste`() {
    val now = 1_700_000_000_000L
    assertTrue(StarMeasures.cards(emptyList(), now).isEmpty())
    assertTrue("con menos de 3 partidas no hay tarjeta", StarMeasures.cards(points("glance", listOf(300f, 280f), 0, now), now).isEmpty())
    val pts = points("glance", listOf(300f, 280f, 250f), 0, now) + points("brake", listOf(400f, 390f, 395f), 5, now) + points("tracking", listOf(3f, 3f, 3.1f), 2, now) + points("mandos", listOf(50f, 55f), 1, now)
    val cards = StarMeasures.cards(pts, now)
    assertEquals(3, cards.size)
    assertEquals("la primera es el descubrimiento del día", StarMeasures.discover(pts, now)!!.def.key, cards[0].def.key)
    assertEquals("sin repetir una medida", 3, cards.map { it.def.key }.toSet().size)
    assertEquals("las otras, por su última partida: Satélites (hace 2 días) antes que Freno (hace 5)", listOf("glance", "tracking", "brake"), cards.map { it.def.key })
    assertEquals(1, StarMeasures.cards(points("glance", listOf(300f, 280f, 250f), 0, now), now).size)
    assertEquals(2, StarMeasures.cards(pts, now, max = 2).size)
  }

  // ---- historial

  @Test
  fun `una linea del historial dice el dia, el juego y como se jugo sin puntos`() {
    val cal = java.util.Calendar.getInstance().apply { set(2026, java.util.Calendar.OCTOBER, 14, 12, 0, 0) }
    val ts = cal.timeInMillis
    val r = GamePlayResult(gameId = "radar", score = 80, correctAnswers = 4, totalTrials = 4, timed = false, level = 2, timestamp = ts)
    assertEquals("Mié 14 · Rescate relámpago · a tu medida", historyLine(r, emptyList(), "Rescate relámpago"))
    assertEquals("Mié 14 · Rescate relámpago · con reloj", historyLine(r.copy(timed = true), emptyList(), "Rescate relámpago"))
    val with = historyLine(r, listOf(MeasurePoint(ts, "glance", 84f)), "Rescate relámpago")
    assertEquals("Mié 14 · Rescate relámpago · a tu medida · 84 ms", with)
    assertFalse("nunca «pts»", with.contains("pts"))
    val other = historyLine(r.copy(gameId = "anagramas"), listOf(MeasurePoint(ts, "glance", 84f)), "En la punta de la lengua")
    assertFalse("un punto de otra medida no se pega", other.contains("84"))
    assertTrue(GameRegistry.getById("radar") != null)
  }
}
