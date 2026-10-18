package com.example.data

import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Los logros por conducta (Tarea 124; Fable, `spec-avance-juegos.md` §1.10): tres familias con niveles (Constancia, Variedad, Volver); los de desempeño (puntaje, Reto y ligas) pasan a `legacy`: ya no se otorgan pero se conservan; la racha de los `racha_N`
 * es la que perdona (`Streak.longest`); nadie pierde una medalla ya ganada.
 */
class AchievementsTest {
  private val day = 24L * 60 * 60 * 1000
  private val base = 1_700_000_000_000L

  private fun game(id: String, dayOffset: Int, score: Int = 70, timed: Boolean = false) =
    GamePlayResult(gameId = id, score = score, correctAnswers = 5, totalTrials = 10, timed = timed, level = 1, timestamp = base + dayOffset * day)

  private val activeIds get() = GameRegistry.allGames.filter { !GameRegistry.isRetired(it.id) }.map { it.id }

  @Test
  fun `racha mas larga cuenta dias seguidos y perdona un descanso`() {
    assertEquals(0, longestStreak(emptyList()))
    assertEquals("1,2,3,(4 perdonado),5,6 son 5 días jugados", 5, longestStreak(listOf(1L, 2L, 3L, 5L, 6L)))
    assertEquals(3, Streak.longestStrict(listOf(1L, 2L, 3L, 5L, 6L)))
    assertEquals(1, longestStreak(listOf(4L, 4L)))
  }

  @Test
  fun `sin partidas no hay logros`() {
    assertTrue(Achievements.unlocked(computeAchievementStats(emptyList(), emptyMap())).isEmpty())
  }

  @Test
  fun `primera partida y racha de 3`() {
    val history = (0 until 3).map { game("secuencia", it) }
    val got = Achievements.unlocked(computeAchievementStats(history, emptyMap()))
    assertTrue("primer_paso" in got)
    assertTrue("racha_3" in got)
    assertFalse("racha_7" in got)
  }

  @Test
  fun `un dia de descanso no corta la racha de los logros`() {
    // 7 días jugados con un descanso en medio (el día 3): con la regla estricta serían 3; con la que perdona, 7
    val history = listOf(0, 1, 2, 4, 5, 6, 7).map { game("secuencia", it) }
    val got = Achievements.unlocked(computeAchievementStats(history, emptyMap()))
    assertTrue("racha_7 con un descanso perdonado", "racha_7" in got)
  }

  @Test
  fun `dia completo y las tres familias`() {
    val history = listOf(game("secuencia", 0), game("parejas", 0), game("stroop", 0))
    val got = Achievements.unlocked(computeAchievementStats(history, emptyMap()))
    assertTrue("dia_completo" in got)
    assertTrue(Achievements.all.all { it.family != null })
    assertEquals(setOf(AchievementFamily.CONSTANCIA, AchievementFamily.VARIEDAD, AchievementFamily.VOLVER), Achievements.all.mapNotNull { it.family }.toSet())
  }

  @Test
  fun `los logros de desempeno ya no se otorgan pero siguen existiendo`() {
    // todo lo que antes daba puntaje, Reto y ligas: 100 puntos, 10 partidas con reloj y liga Platino en un juego
    val history = (0 until 12).map { game("secuencia", it, score = 100, timed = true) }
    val stats = computeAchievementStats(history, mapOf("secuencia" to 900))
    val got = Achievements.unlocked(stats)
    val legacyIds = listOf("brillante", "perfecto", "reto_10", "liga_plata", "liga_oro", "liga_platino", "general_oro")
    for (id in legacyIds) {
      assertFalse("$id ya no se otorga", id in got)
      assertTrue("$id se conserva en el catálogo anterior", Achievements.legacy.any { it.id == id })
      assertNotNull("$id se encuentra por su id (para mostrar la medalla ya ganada)", Achievements.byId(id))
      assertNull("$id ya no está en el catálogo que se otorga", Achievements.all.firstOrNull { it.id == id })
      assertTrue(Achievements.byId(id)!!.legacy)
    }
    // las cifras del desempeño siguen calculándose (por si se vuelven a mirar), solo que no dan logro
    assertEquals(100, stats.bestScore); assertEquals(12, stats.timedGames)
  }

  @Test
  fun `nadie pierde una medalla ya ganada`() {
    val earned = mapOf("perfecto" to 1L, "liga_oro" to 2L, "racha_7" to 3L, "reto_10" to 4L)
    // lo guardado se lee entero aunque algunos ids ya no estén en el catálogo que se otorga
    assertEquals(earned, decodeUnlocks(encodeUnlocks(earned)))
    assertEquals(setOf("perfecto", "liga_oro", "reto_10"), Achievements.legacyEarned(earned.keys).map { it.id }.toSet())
    assertTrue("«Medallas anteriores» solo muestra las que ganó: quien no ganó ninguna no ve la sección", Achievements.legacyEarned(setOf("racha_7")).isEmpty())
    // cada id viejo sigue teniendo su definición (título, dibujo) para dibujar la medalla
    for (id in earned.keys) assertNotNull(Achievements.byId(id))
  }

  @Test
  fun `volver cuenta las veces que volvio tras tres o mas dias sin partida`() {
    // días: 0, 1, (2, 3, 4 sin partida) 5 → volvió 1 vez; luego 6, (7..10 sin partida) 11 → 2 veces; 12, 13 seguidos no cuentan
    val history = listOf(0, 1, 5, 6, 11, 12, 13).map { game("secuencia", it) }
    val stats = computeAchievementStats(history, emptyMap())
    assertEquals(2, stats.returns)
    val got = Achievements.unlocked(stats)
    assertTrue("vuelta_1" in got)
    assertFalse("vuelta_3" in got)
    assertEquals("dos días sin partida entre uno y otro NO es una pausa", 0, countReturns(listOf(0L, 3L)))
    assertEquals("tres días sin partida sí (de 0 a 4)", 1, countReturns(listOf(0L, 4L)))
    assertEquals("varias partidas el mismo día cuentan una vez", 1, countReturns(listOf(0L, 0L, 9L, 9L)))
  }

  @Test
  fun `los tres niveles de volver`() {
    val ids = Achievements.levelsOf(AchievementFamily.VOLVER).map { it.id }
    assertEquals(listOf("vuelta_1", "vuelta_3", "vuelta_10"), ids)
    assertEquals(listOf(1, 3, 10), Achievements.levelsOf(AchievementFamily.VOLVER).map { it.number })
    assertEquals("vuelta_3", Achievements.reached(AchievementFamily.VOLVER, setOf("vuelta_1", "vuelta_3"))!!.id)
    assertEquals("vuelta_10", Achievements.next(AchievementFamily.VOLVER, setOf("vuelta_1", "vuelta_3"))!!.id)
    assertNull(Achievements.next(AchievementFamily.VOLVER, ids.toSet()))
    assertNull(Achievements.reached(AchievementFamily.VOLVER, emptySet()))
  }

  @Test
  fun `explorador de 17 cuenta los juegos que la app ofrece hoy y no los retirados`() {
    val all = activeIds.mapIndexed { i, id -> game(id, i) }
    assertEquals("la app ofrece 17 juegos", 17, activeIds.size)
    val got = Achievements.unlocked(computeAchievementStats(all, emptyMap()))
    assertTrue("explorador_17" in got)
    assertTrue("explorador" in got)
    val sixteen = activeIds.take(16).mapIndexed { i, id -> game(id, i) }
    assertFalse("con 16 juegos no", "explorador_17" in Achievements.unlocked(computeAchievementStats(sixteen, emptyMap())))
  }

  @Test
  fun `las familias tienen sus niveles en orden y la constancia junta los dias y las partidas`() {
    assertEquals(listOf("primer_paso", "racha_3", "racha_7", "partidas_25", "racha_14", "racha_30", "partidas_100", "racha_100"), Achievements.levelsOf(AchievementFamily.CONSTANCIA).map { it.id })
    assertEquals(listOf("dia_completo", "dominios", "explorador", "explorador_17"), Achievements.levelsOf(AchievementFamily.VARIEDAD).map { it.id })
    for (f in AchievementFamily.entries) assertEquals("los niveles de $f van de a uno", Achievements.levelsOf(f).map { it.level }.sorted(), Achievements.levelsOf(f).map { it.level })
  }

  @Test
  fun `progreso de un logro bloqueado`() {
    val stats = computeAchievementStats((0 until 4).map { game("secuencia", it) }, emptyMap())
    assertEquals(4 to 7, Achievements.byId("racha_7")!!.progress(stats))
  }

  @Test
  fun `registro de logros ida y vuelta`() {
    val unlocks = mapOf("primer_paso" to 10L, "racha_3" to 20L)
    assertEquals(unlocks, decodeUnlocks(encodeUnlocks(unlocks)))
    assertTrue(decodeUnlocks("basura").isEmpty())
  }
}
