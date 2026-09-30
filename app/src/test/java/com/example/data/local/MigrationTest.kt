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
}
