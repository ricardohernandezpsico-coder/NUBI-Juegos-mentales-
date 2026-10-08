package com.example.data.local

import android.content.Context
import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase
import com.example.BuildConfig

@Database(
  entities = [
    GameResultEntity::class,
    GameProgressEntity::class,
    DailySessionEntity::class,
    UserProfileEntity::class,
    DomainMasteryEntity::class,
    ClaimedWeeklyChallengeEntity::class
  ],
  version = 13,
  exportSchema = true
)
abstract class NeuroVidaDatabase : RoomDatabase() {
  abstract fun gameResultDao(): GameResultDao
  abstract fun gameProgressDao(): GameProgressDao
  abstract fun dailySessionDao(): DailySessionDao
  abstract fun userProfileDao(): UserProfileDao
  abstract fun domainMasteryDao(): DomainMasteryDao
  abstract fun claimedWeeklyChallengeDao(): ClaimedWeeklyChallengeDao

  companion object {
    @Volatile
    private var INSTANCE: NeuroVidaDatabase? = null

    // Auditoría (21-sep, fase 2): antes esto era `.fallbackToDestructiveMigration()`
    // sin condición -- CUALQUIER futuro cambio de esquema (agregar una columna, una
    // tabla, etc.) habría borrado streaks/scores/rangos/historial de todos los
    // usuarios en producción sin avisar. Ahora, si se sube `version` sin agregar la
    // `Migration` real correspondiente en `MIGRATIONS`, un build de RELEASE falla con
    // un crash (`IllegalStateException`) en vez de destruir los datos en silencio --
    // un crash se detecta en QA antes de publicar; una pérdida de datos silenciosa la
    // descubre el usuario después. Solo en DEBUG se mantiene el fallback destructivo,
    // para no trabar la iteración local mientras se define un esquema nuevo.
    // internal (no private) para que la prueba de migración (MigrationTest) use exactamente las mismas.
    internal val MIGRATIONS = arrayOf<androidx.room.migration.Migration>(
      // Cuando se necesite cambiar el esquema: agregar acá un Migration(N, N+1) real
      // con el SQL de la migración, subir `version` arriba, y correr el build una vez
      // para que se genere `schemas/<version>.json` (ya versionado en git desde esta
      // auditoría). Ver NeuroVida/CLAUDE.md para el detalle del proceso.
      // 10 -> 11 (24-sep): rating del DDA común por juego (ver docs/DDA-comun.md).
      object : androidx.room.migration.Migration(10, 11) {
        override fun migrate(db: androidx.sqlite.db.SupportSQLiteDatabase) {
          db.execSQL("ALTER TABLE game_progress ADD COLUMN ddaRating REAL NOT NULL DEFAULT -1")
        }
      },
      // 11 -> 12 (30-sep): de 6 a 4 áreas. Velocidad se une a Atención y Cálculo a Razonamiento: el XP de las áreas
      // viejas se suma al de la nueva (o pasa a ser el de la nueva si esa aún no existía). El esquema no cambia.
      object : androidx.room.migration.Migration(11, 12) {
        override fun migrate(db: androidx.sqlite.db.SupportSQLiteDatabase) {
          for ((old, new) in listOf("VELOCIDAD" to "ATENCION", "CALCULO" to "RAZONAMIENTO")) {
            db.execSQL("UPDATE domain_mastery SET domain='$new' WHERE domain='$old' AND NOT EXISTS (SELECT 1 FROM domain_mastery WHERE domain='$new')")
            db.execSQL("UPDATE domain_mastery SET xp = xp + (SELECT xp FROM domain_mastery WHERE domain='$old') WHERE domain='$new' AND EXISTS (SELECT 1 FROM domain_mastery WHERE domain='$old')")
            db.execSQL("DELETE FROM domain_mastery WHERE domain='$old'")
          }
        }
      },
      // 12 -> 13 (7-oct): Parejas Ocultas se renovó como «Constelaciones» y su escalera pasó de 10 a 18 etapas. El rating guardado (0..1) de quienes ya jugaban se lleva a la escala nueva para que partan en una etapa
      // equivalente (ratio 40/81; la cuenta está en data/Constelaciones.translateOldRating). Sin dato (-1) no se toca. El esquema no cambia.
      object : androidx.room.migration.Migration(12, 13) {
        override fun migrate(db: androidx.sqlite.db.SupportSQLiteDatabase) {
          db.execSQL("UPDATE game_progress SET ddaRating = ddaRating * 40.0 / 81.0 WHERE gameId = 'parejas' AND ddaRating >= 0")
        }
      }
    )

    fun getDatabase(context: Context): NeuroVidaDatabase {
      return INSTANCE ?: synchronized(this) {
        val builder = Room.databaseBuilder(
          context.applicationContext,
          NeuroVidaDatabase::class.java,
          "neurovida_database"
        ).addMigrations(*MIGRATIONS)

        if (BuildConfig.DEBUG) {
          builder.fallbackToDestructiveMigration(dropAllTables = true)
        }

        val instance = builder.build()
        INSTANCE = instance
        instance
      }
    }
  }
}
