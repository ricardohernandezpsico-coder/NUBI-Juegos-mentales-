package com.example.data.local

import androidx.test.platform.app.InstrumentationRegistry
import androidx.room.testing.MigrationTestHelper
import java.io.File
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * Una actualización de la app NO puede borrar el progreso de nadie. En una versión de tienda, si falta la migración de
 * un cambio de esquema, la app se detiene en vez de borrar (ver NeuroVidaDatabase); estas pruebas hacen que ese error
 * se vea aquí, antes de publicar, y no en el teléfono de una persona.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class MigrationTest {
  @get:Rule
  val helper = MigrationTestHelper(InstrumentationRegistry.getInstrumentation(), NeuroVidaDatabase::class.java)

  @Test
  fun `de la version 10 a la 11 se conserva el avance y el rating parte sin dato`() {
    helper.createDatabase("migracion", 10).apply {
      execSQL(
        "INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating) " +
          "VALUES ('calculo', 4, 92, 17, 1700000000000, 3, 1200)"
      )
      close()
    }
    val db = helper.runMigrationsAndValidate("migracion", 11, true, *NeuroVidaDatabase.MIGRATIONS)
    db.query("SELECT currentLevel, highestScore, totalGamesPlayed, masteryStreak, ddaRating FROM game_progress WHERE gameId = 'calculo'").use { c ->
      assertTrue("la fila del juego desapareció con la migración", c.moveToFirst())
      assertEquals(4, c.getInt(0))
      assertEquals(92, c.getInt(1))
      assertEquals(17, c.getInt(2))
      assertEquals(3, c.getInt(3))
      // -1 = "todavía sin rating" (así lo lee el DDA común).
      assertEquals(-1.0, c.getDouble(4), 0.0)
    }
    db.close()
  }

  @Test
  fun `de la version 11 a la 12 Velocidad se une a Atencion y Calculo a Razonamiento`() {
    helper.createDatabase("migracion12", 11).apply {
      execSQL("INSERT INTO domain_mastery (domain, xp) VALUES ('ATENCION', 10)")
      execSQL("INSERT INTO domain_mastery (domain, xp) VALUES ('VELOCIDAD', 5)")
      execSQL("INSERT INTO domain_mastery (domain, xp) VALUES ('CALCULO', 7)")
      execSQL("INSERT INTO domain_mastery (domain, xp) VALUES ('MEMORIA', 3)")
      close()
    }
    val db = helper.runMigrationsAndValidate("migracion12", 12, true, *NeuroVidaDatabase.MIGRATIONS)
    val xp = mutableMapOf<String, Int>()
    db.query("SELECT domain, xp FROM domain_mastery").use { c -> while (c.moveToNext()) xp[c.getString(0)] = c.getInt(1) }
    // Atención 10 + Velocidad 5; Razonamiento no existía, hereda el XP de Cálculo; Memoria no se toca.
    assertEquals(mapOf("ATENCION" to 15, "RAZONAMIENTO" to 7, "MEMORIA" to 3), xp)
    db.close()
  }

  @Test
  fun `de la version 12 a la 13 el rating de Parejas pasa a la escalera nueva de 18 etapas y los demas juegos no se tocan`() {
    helper.createDatabase("migracion13", 12).apply {
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('parejas', 3, 80, 9, 1700000000000, 1, 1100, 0.9)")
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('secuencia', 3, 80, 9, 1700000000000, 1, 1100, 0.9)")
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('rumbo', 1, 0, 0, 0, 0, 0, -1)")
      close()
    }
    val db = helper.runMigrationsAndValidate("migracion13", 13, true, *NeuroVidaDatabase.MIGRATIONS)
    val rating = mutableMapOf<String, Double>()
    db.query("SELECT gameId, ddaRating, totalGamesPlayed FROM game_progress").use { c ->
      while (c.moveToNext()) rating[c.getString(0)] = c.getDouble(1)
    }
    // 0,9 de la escalera de 10 (nivel 10) = etapa 9 de la de 18 (8/18); el mismo número que calcula la app (Constelaciones.translateOldRating).
    assertEquals(com.example.data.Constelaciones.translateOldRating(0.9f).toDouble(), rating["parejas"]!!, 1e-4)
    assertEquals(8.0 / 18.0, rating["parejas"]!!, 1e-3)
    assertEquals(0.9, rating["secuencia"]!!, 1e-6)
    assertEquals(-1.0, rating["rumbo"]!!, 0.0)
    db.query("SELECT totalGamesPlayed FROM game_progress WHERE gameId = 'parejas'").use { c ->
      assertTrue(c.moveToFirst())
      assertEquals(9, c.getInt(0))
    }
    db.close()
  }

  @Test
  fun `de la version 13 a la 14 el rating de Correo se lleva a la mitad y los demas juegos no se tocan`() {
    helper.createDatabase("migracion14", 13).apply {
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('correo', 3, 80, 9, 1700000000000, 1, 1100, 0.9)")
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('secuencia', 3, 80, 9, 1700000000000, 1, 1100, 0.9)")
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('rumbo', 1, 0, 0, 0, 0, 0, -1)")
      close()
    }
    val db = helper.runMigrationsAndValidate("migracion14", 14, true, *NeuroVidaDatabase.MIGRATIONS)
    val rating = mutableMapOf<String, Double>()
    db.query("SELECT gameId, ddaRating FROM game_progress").use { c ->
      while (c.moveToNext()) rating[c.getString(0)] = c.getDouble(1)
    }
    // 0,9 del vuelo viejo parte en 0,45 (etapa 5 de 10); el mismo número que calcula la app (Mail.translateOldRating).
    assertEquals(com.example.data.Mail.translateOldRating(0.9f).toDouble(), rating["correo"]!!, 1e-4)
    assertEquals(0.45, rating["correo"]!!, 1e-4)
    assertEquals(0.9, rating["secuencia"]!!, 1e-6)
    assertEquals(-1.0, rating["rumbo"]!!, 0.0)
    db.query("SELECT totalGamesPlayed FROM game_progress WHERE gameId = 'correo'").use { c ->
      assertTrue(c.moveToFirst())
      assertEquals(9, c.getInt(0))
    }
    db.close()
  }

  @Test
  fun `de la version 14 a la 15 el rating de Satelites se lleva a las tres cuartas partes y los demas juegos no se tocan`() {
    helper.createDatabase("migracion15", 14).apply {
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('satelites', 3, 80, 9, 1700000000000, 1, 1100, 0.9)")
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('secuencia', 3, 80, 9, 1700000000000, 1, 1100, 0.9)")
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('radar', 1, 0, 0, 0, 0, 0, -1)")
      close()
    }
    val db = helper.runMigrationsAndValidate("migracion15", 15, true, *NeuroVidaDatabase.MIGRATIONS)
    val rating = mutableMapOf<String, Double>()
    db.query("SELECT gameId, ddaRating FROM game_progress").use { c ->
      while (c.moveToNext()) rating[c.getString(0)] = c.getDouble(1)
    }
    // 0,9 del juego viejo parte en 0,675 (nivel 9 de 12); el mismo número que calcula la app (Satelites.translateOldRating).
    assertEquals(com.example.data.Satelites.translateOldRating(0.9f).toDouble(), rating["satelites"]!!, 1e-4)
    assertEquals(0.675, rating["satelites"]!!, 1e-4)
    assertEquals(0.9, rating["secuencia"]!!, 1e-6)
    assertEquals(-1.0, rating["radar"]!!, 0.0)
    db.query("SELECT totalGamesPlayed FROM game_progress WHERE gameId = 'satelites'").use { c ->
      assertTrue(c.moveToFirst())
      assertEquals(9, c.getInt(0))
    }
    db.close()
  }

  @Test
  fun `de la version 15 a la 16 aparece la tabla del Contrarreloj y todo lo que habia se conserva`() {
    helper.createDatabase("migracion16", 15).apply {
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('stroop', 3, 80, 9, 1700000000000, 1, 1100, 0.6)")
      execSQL("INSERT INTO game_results (id, gameId, score, correctAnswers, totalTrials, timed, level, timestamp) VALUES ('a', 'stroop', 80, 10, 12, 1, 3, 1700000000000)")
      close()
    }
    val db = helper.runMigrationsAndValidate("migracion16", 16, true, *NeuroVidaDatabase.MIGRATIONS)
    db.query("SELECT ddaRating, totalGamesPlayed FROM game_progress WHERE gameId = 'stroop'").use { c ->
      assertTrue("la fila del juego desapareció con la migración", c.moveToFirst())
      assertEquals(0.6, c.getDouble(0), 1e-6)
      assertEquals(9, c.getInt(1))
    }
    db.query("SELECT COUNT(*) FROM game_results").use { c -> assertTrue(c.moveToFirst()); assertEquals(1, c.getInt(0)) }
    db.query("SELECT COUNT(*) FROM time_trial_results").use { c -> assertTrue(c.moveToFirst()); assertEquals("la tabla nace vacía", 0, c.getInt(0)) }
    db.execSQL("INSERT INTO time_trial_results (id, gameId, day, trackVersion, hits, errors, level, seconds, timestamp) VALUES ('x', 'stroop', '2026-10-26', 1, 38, 6, 4, 120, 1700000000000)")
    db.query("SELECT hits, level FROM time_trial_results WHERE id = 'x'").use { c -> assertTrue(c.moveToFirst()); assertEquals(38, c.getInt(0)); assertEquals(4, c.getInt(1)) }
    db.close()
  }

  @Test
  fun `cada version exportada tiene su migracion a la siguiente`() {
    val versions = File("schemas").walkTopDown().filter { it.extension == "json" }.map { it.nameWithoutExtension.toInt() }.toList().sorted()
    assertTrue("no encontré los esquemas exportados en app/schemas", versions.isNotEmpty())
    val declared = NeuroVidaDatabase.MIGRATIONS.map { it.startVersion to it.endVersion }.toSet()
    val missing = (versions.first() until versions.last()).filter { (it to it + 1) !in declared }
    assertTrue(
      "Falta la migración de la versión $missing a la siguiente. Subir `version` sin agregar la Migration real haría " +
        "que la app se detenga (o, en depuración, borre los datos) al actualizarse.",
      missing.isEmpty()
    )
    // La versión real de la base (la que dice @Database) tiene que ser la última exportada: si se sube `version` sin
    // compilar, falta el esquema; si se compiló, la comprobación de arriba ya exige su migración.
    val context = androidx.test.core.app.ApplicationProvider.getApplicationContext<android.content.Context>()
    val db = androidx.room.Room.inMemoryDatabaseBuilder(context, NeuroVidaDatabase::class.java).allowMainThreadQueries().build()
    val current = db.openHelper.readableDatabase.version
    db.close()
    assertEquals("la última versión exportada debe ser la de la base de datos", versions.last(), current)
  }
  @Test
  fun `de la version 16 a la 17 el Contrarreloj gana la columna del ritmo, las partidas de antes quedan sin ritmo y todo lo demas se conserva`() {
    helper.createDatabase("migracion17", 16).apply {
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('stroop', 3, 80, 9, 1700000000000, 1, 1100, 0.6)")
      execSQL("INSERT INTO game_results (id, gameId, score, correctAnswers, totalTrials, timed, level, timestamp) VALUES ('a', 'stroop', 80, 10, 12, 1, 3, 1700000000000)")
      execSQL("INSERT INTO time_trial_results (id, gameId, day, trackVersion, hits, errors, level, seconds, timestamp) VALUES ('viejo', 'stroop', '2026-10-30', 2, 38, 4, 4, 120, 1700000000000)")
      close()
    }
    val db = helper.runMigrationsAndValidate("migracion17", 17, true, *NeuroVidaDatabase.MIGRATIONS)
    db.query("SELECT hits, errors, level, seconds, trials FROM time_trial_results WHERE id = 'viejo'").use { c ->
      assertTrue("la partida de antes desapareció con la migración", c.moveToFirst())
      assertEquals(38, c.getInt(0)); assertEquals(4, c.getInt(1)); assertEquals(4, c.getInt(2)); assertEquals(120, c.getInt(3))
      assertTrue("sin ritmo: la columna queda en NULL", c.isNull(4))
    }
    db.query("SELECT ddaRating, totalGamesPlayed FROM game_progress WHERE gameId = 'stroop'").use { c ->
      assertTrue("la fila del juego desapareció con la migración", c.moveToFirst())
      assertEquals(0.6, c.getDouble(0), 1e-6)
      assertEquals(9, c.getInt(1))
    }
    db.query("SELECT COUNT(*) FROM game_results").use { c -> assertTrue(c.moveToFirst()); assertEquals(1, c.getInt(0)) }
    // una partida nueva guarda su ritmo como texto; y una sin ritmo sigue valiendo (columna que admite NULL, sin valor por defecto)
    db.execSQL("INSERT INTO time_trial_results (id, gameId, day, trackVersion, hits, errors, level, seconds, timestamp, trials) VALUES ('nuevo', 'stroop', '2026-10-31', 2, 2, 1, 2, 120, 1700000100000, '{\"t_ms\":[1200,3400,5000],\"ok\":[1,0,1],\"level\":[1,1,2]}')")
    db.execSQL("INSERT INTO time_trial_results (id, gameId, day, trackVersion, hits, errors, level, seconds, timestamp) VALUES ('sin-ritmo', 'radar', '2026-10-31', 2, 20, 3, 3, 120, 1700000200000)")
    db.query("SELECT trials FROM time_trial_results WHERE id = 'nuevo'").use { c ->
      assertTrue(c.moveToFirst())
      assertEquals(com.example.data.TimeTrialTrials.of(listOf(1200, 3400, 5000), listOf(1, 0, 1), listOf(1, 1, 2)), com.example.data.TimeTrialTrials.fromJson(c.getString(0)))
    }
    db.query("SELECT COUNT(*) FROM time_trial_results WHERE trials IS NULL").use { c -> assertTrue(c.moveToFirst()); assertEquals("la de antes y la sin ritmo", 2, c.getInt(0)) }
    db.close()
  }

  /**
   * Tarea 221: «Tus últimas partidas» de Hoy guarda con qué modo se jugó cada partida y si fue de la ruta de hoy. Las de antes quedan SIN modo (NULL: no se inventa) y fuera de la ruta (`route` = 0); nada se borra ni se reescribe.
   */
  @Test
  fun `de la version 17 a la 18 las partidas ganan el modo y la ruta, las de antes quedan sin modo y todo lo demas se conserva`() {
    helper.createDatabase("migracion18", 17).apply {
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('calculo', 4, 92, 17, 1700000000000, 3, 1200, 0.5)")
      execSQL("INSERT INTO game_results (id, gameId, score, correctAnswers, totalTrials, timed, level, timestamp) VALUES ('vieja', 'calculo', 80, 10, 12, 1, 3, 1700000000000)")
      execSQL("INSERT INTO time_trial_results (id, gameId, day, trackVersion, hits, errors, level, seconds, timestamp) VALUES ('tt', 'stroop', '2026-10-30', 2, 38, 4, 4, 120, 1700000050000)")
      close()
    }
    val db = helper.runMigrationsAndValidate("migracion18", 18, true, *NeuroVidaDatabase.MIGRATIONS)
    db.query("SELECT score, correctAnswers, totalTrials, timed, level, timestamp, mode, route FROM game_results WHERE id = 'vieja'").use { c ->
      assertTrue("la partida de antes desapareció con la migración", c.moveToFirst())
      assertEquals(80, c.getInt(0)); assertEquals(10, c.getInt(1)); assertEquals(12, c.getInt(2)); assertEquals(1, c.getInt(3)); assertEquals(3, c.getInt(4)); assertEquals(1700000000000L, c.getLong(5))
      assertTrue("sin modo: la columna queda en NULL", c.isNull(6))
      assertEquals("y no cuenta como de la ruta", 0, c.getInt(7))
    }
    // una partida nueva guarda su modo y si fue de la ruta; una sin la ruta sigue valiendo (valor por defecto 0)
    db.execSQL("INSERT INTO game_results (id, gameId, score, correctAnswers, totalTrials, timed, level, timestamp, mode, route) VALUES ('nueva', 'calculo', 90, 11, 12, 1, 3, 1700000100000, 'DESAFIO', 1)")
    db.execSQL("INSERT INTO game_results (id, gameId, score, correctAnswers, totalTrials, timed, level, timestamp) VALUES ('sin-ruta', 'calculo', 70, 9, 12, 1, 3, 1700000200000)")
    db.query("SELECT mode, route FROM game_results WHERE id = 'nueva'").use { c -> assertTrue(c.moveToFirst()); assertEquals("DESAFIO", c.getString(0)); assertEquals(1, c.getInt(1)) }
    db.query("SELECT COUNT(*) FROM game_results WHERE mode IS NULL AND route = 0").use { c -> assertTrue(c.moveToFirst()); assertEquals("la de antes y la sin ruta", 2, c.getInt(0)) }
    db.query("SELECT ddaRating, totalGamesPlayed FROM game_progress WHERE gameId = 'calculo'").use { c ->
      assertTrue("la fila del juego desapareció con la migración", c.moveToFirst())
      assertEquals(0.5, c.getDouble(0), 1e-6); assertEquals(17, c.getInt(1))
    }
    db.query("SELECT hits, errors FROM time_trial_results WHERE id = 'tt'").use { c -> assertTrue("el Contrarreloj se conserva", c.moveToFirst()); assertEquals(38, c.getInt(0)); assertEquals(4, c.getInt(1)) }
    db.close()
  }

  @Test
  fun `de la version 18 a la 19 el rating de Carga se lleva a cinco octavos, el nivel y los puntos no cambian y los demas juegos no se tocan`() {
    helper.createDatabase("migracion19", 18).apply {
      // alguien en el 4,6 de 5 (rating 0,92), otro en el 2 de 5 (0,4), alguien sin dato (-1), y juegos que NO se alargaron con el mismo rating
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('calculo', 5, 95, 21, 1700000000000, 2, 1300, 0.92)")
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('stroop', 5, 90, 9, 1700000000000, 1, 1100, 0.92)")
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('anagramas', 2, 60, 4, 1700000000000, 0, 1000, 0.4)")
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('radar', 1, 0, 0, 0, 0, 0, -1)")
      execSQL("INSERT INTO game_results (id, gameId, score, correctAnswers, totalTrials, timed, level, timestamp) VALUES ('vieja', 'calculo', 80, 6, 8, 0, 4, 1700000000000)")
      close()
    }
    val db = helper.runMigrationsAndValidate("migracion19", 19, true, *NeuroVidaDatabase.MIGRATIONS)
    val rating = mutableMapOf<String, Double>()
    db.query("SELECT gameId, ddaRating FROM game_progress").use { c -> while (c.moveToNext()) rating[c.getString(0)] = c.getDouble(1) }
    // 4,6 de 5 queda en el 4,6 de 8 (0,575); el mismo número que calcula la app (Carga.translateOldRating) y la razón de la escalera
    assertEquals(0.575, rating["calculo"]!!, 1e-4)
    assertEquals(com.example.data.Carga.translateOldRating(0.92f).toDouble(), rating["calculo"]!!, 1e-4)
    assertEquals(com.example.data.Skill.ladder("calculo").ratingScale * 0.92, rating["calculo"]!!, 1e-4)
    assertEquals(0.92, rating["stroop"]!!, 1e-6); assertEquals(0.4, rating["anagramas"]!!, 1e-6); assertEquals(-1.0, rating["radar"]!!, 0.0)
    // sin perder nada: los puntos (avance anclado a los 5 niveles de antes) y el nivel son los mismos que antes de la migración
    val age = com.example.model.AgeBand.ADULT
    val r = rating["calculo"]!!.toFloat()
    assertEquals("sus puntos no cambian", 0.92f, com.example.data.Skill.progress("calculo", r, age), 1e-4f)
    assertEquals("su nivel tampoco: el 5", 5, com.example.data.LevelLines.levelOf("calculo", com.example.data.Skill.ladderProgress("calculo", r, age)))
    db.query("SELECT totalGamesPlayed, highestScore, currentLevel FROM game_progress WHERE gameId = 'calculo'").use { c ->
      assertTrue(c.moveToFirst()); assertEquals(21, c.getInt(0)); assertEquals(95, c.getInt(1)); assertEquals(5, c.getInt(2))
    }
    db.query("SELECT score FROM game_results WHERE id = 'vieja'").use { c -> assertTrue("las partidas se conservan", c.moveToFirst()); assertEquals(80, c.getInt(0)) }
    db.close()
  }

  @Test
  fun `de la version 18 a la 19 Carga sin rating guardado sigue sin rating`() {
    helper.createDatabase("migracion19b", 18).apply {
      execSQL("INSERT INTO game_progress (gameId, currentLevel, highestScore, totalGamesPlayed, lastPlayedTimestamp, masteryStreak, eloRating, ddaRating) VALUES ('calculo', 1, 0, 0, 0, 0, 0, -1)")
      close()
    }
    val db = helper.runMigrationsAndValidate("migracion19b", 19, true, *NeuroVidaDatabase.MIGRATIONS)
    db.query("SELECT ddaRating FROM game_progress WHERE gameId = 'calculo'").use { c -> assertTrue(c.moveToFirst()); assertEquals(-1.0, c.getDouble(0), 0.0) }
    db.close()
  }
}
