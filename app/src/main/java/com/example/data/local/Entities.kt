package com.example.data.local

import androidx.room.ColumnInfo
import androidx.room.Entity
import androidx.room.PrimaryKey
import com.example.model.AgeBand
import com.example.model.AppLanguage
import com.example.model.DailySessionState
import com.example.model.DifficultyMode
import com.example.model.GamePlayResult
import com.example.model.ThemeMode
import com.example.model.UserSettings

@Entity(tableName = "game_results")
data class GameResultEntity(
  @PrimaryKey val id: String,
  val gameId: String,
  val score: Int,
  val correctAnswers: Int,
  val totalTrials: Int,
  val timed: Boolean,
  val level: Int,
  val timestamp: Long
)

fun GameResultEntity.toDomain(): GamePlayResult = GamePlayResult(
  id = id,
  gameId = gameId,
  score = score,
  correctAnswers = correctAnswers,
  totalTrials = totalTrials,
  timed = timed,
  level = level,
  timestamp = timestamp
)

fun GamePlayResult.toEntity(): GameResultEntity = GameResultEntity(
  id = id,
  gameId = gameId,
  score = score,
  correctAnswers = correctAnswers,
  totalTrials = totalTrials,
  timed = timed,
  level = level,
  timestamp = timestamp
)

@Entity(tableName = "game_progress")
data class GameProgressEntity(
  @PrimaryKey val gameId: String,
  val currentLevel: Int = 1,
  val highestScore: Int = 0,
  val totalGamesPlayed: Int = 0,
  val lastPlayedTimestamp: Long = 0L,
  // Progresión sin techo: currentLevel se tapa en 5 (Experto) a propósito — es la
  // etiqueta visible (Principiante..Experto). masteryStreak sigue subiendo sin límite
  // mientras se siga rindiendo bien en nivel 5, y es lo que de verdad endurece el juego
  // (tiempos, rangos) para que un usuario "Experto" nunca deje de sentir presión.
  val masteryStreak: Int = 0,
  // Ranking tipo ELO (ver RankTier/GameRankInfo en Models.kt): sube o baja con cada
  // partida según el score, independiente de currentLevel/masteryStreak.
  val eloRating: Int = 0,
  // DDA común (Unity, ver docs/DDA-comun.md): rating final de la escalera del juego, normalizado
  // 0..1 y suavizado entre sesiones. -1 = todavía sin dato (el juego arranca según el nivel elegido).
  @ColumnInfo(defaultValue = "-1")
  val ddaRating: Float = -1f
)

@Entity(tableName = "daily_sessions")
data class DailySessionEntity(
  @PrimaryKey val dateKey: String,
  val gameIdsRaw: String, // Comma separated: "calculo,parejas,stroop"
  val completedCount: Int = 0,
  val scoresRaw: String = "" // Comma separated: "85,90"
)

fun DailySessionEntity.toDomain(): DailySessionState {
  val ids = gameIdsRaw.split(",").filter { it.isNotBlank() }
  val scores = if (scoresRaw.isBlank()) emptyList() else scoresRaw.split(",").mapNotNull { it.toIntOrNull() }
  return DailySessionState(
    dateKey = dateKey,
    gameIds = ids,
    completedCount = completedCount,
    scores = scores
  )
}

fun DailySessionState.toEntity(): DailySessionEntity = DailySessionEntity(
  dateKey = dateKey,
  gameIdsRaw = gameIds.joinToString(","),
  completedCount = completedCount,
  scoresRaw = scores.joinToString(",")
)

@Entity(tableName = "domain_mastery")
data class DomainMasteryEntity(
  @PrimaryKey val domain: String, // DomainType.name
  val xp: Int = 0
)

@Entity(tableName = "claimed_weekly_challenges")
data class ClaimedWeeklyChallengeEntity(
  @PrimaryKey val id: String // "<weekKey>|<challengeKey>"
)

@Entity(tableName = "user_profile")
data class UserProfileEntity(
  @PrimaryKey(autoGenerate = true) val id: Long = 0L,
  val name: String = "",
  /** Ya no se muestra (Tarea 93: los avatares se quitaron); la columna se queda con lo que tenga guardado, sin migrar la base. */
  val avatar: String = "",
  val isActive: Boolean = true,
  val weeklyGoal: Int = 4,
  val defaultTimed: Boolean = false,
  val soundEnabled: Boolean = true,
  val hapticsEnabled: Boolean = true,
  val notificationsEnabled: Boolean = true,
  val reminderHour: Int = 19,
  val reminderMinute: Int = 0,
  val difficultyMode: String = "ADAPTIVE",
  val difficultyMemoria: Int = 2,
  val difficultyAtencion: Int = 2,
  val difficultyRazonamiento: Int = 2,
  val difficultyLenguaje: Int = 2,
  val difficultyCalculo: Int = 2,
  val difficultyVelocidad: Int = 2,
  val cognitiveAssistance: Boolean = true,
  val timeScaleFactor: Float = 1.0f,
  val themeMode: String = "SYSTEM",
  val language: String = "es",
  /** `null` = todavía no completó el onboarding de edad -- ver [AgeBand]. */
  val ageBand: String? = null
)

fun UserProfileEntity.toDomain(): UserSettings = UserSettings(
  id = id,
  name = name,
  avatar = avatar,
  isActive = isActive,
  weeklyGoal = weeklyGoal,
  defaultTimed = defaultTimed,
  soundEnabled = soundEnabled,
  hapticsEnabled = hapticsEnabled,
  notificationsEnabled = notificationsEnabled,
  reminderHour = reminderHour,
  reminderMinute = reminderMinute,
  difficultyMode = runCatching { DifficultyMode.valueOf(difficultyMode) }.getOrDefault(DifficultyMode.ADAPTIVE),
  difficultyMemoria = difficultyMemoria,
  difficultyAtencion = difficultyAtencion,
  difficultyRazonamiento = difficultyRazonamiento,
  difficultyLenguaje = difficultyLenguaje,
  difficultyCalculo = difficultyCalculo,
  difficultyVelocidad = difficultyVelocidad,
  cognitiveAssistance = cognitiveAssistance,
  timeScaleFactor = timeScaleFactor,
  themeMode = runCatching { ThemeMode.valueOf(themeMode) }.getOrDefault(ThemeMode.SYSTEM),
  language = AppLanguage.fromCode(language),
  ageBand = ageBand?.let { runCatching { AgeBand.valueOf(it) }.getOrNull() }
)

fun UserSettings.toEntity(): UserProfileEntity = UserProfileEntity(
  id = id,
  name = name,
  avatar = avatar,
  isActive = isActive,
  weeklyGoal = weeklyGoal,
  defaultTimed = defaultTimed,
  soundEnabled = soundEnabled,
  hapticsEnabled = hapticsEnabled,
  notificationsEnabled = notificationsEnabled,
  reminderHour = reminderHour,
  reminderMinute = reminderMinute,
  difficultyMode = difficultyMode.name,
  difficultyMemoria = difficultyMemoria,
  difficultyAtencion = difficultyAtencion,
  difficultyRazonamiento = difficultyRazonamiento,
  difficultyLenguaje = difficultyLenguaje,
  difficultyCalculo = difficultyCalculo,
  difficultyVelocidad = difficultyVelocidad,
  cognitiveAssistance = cognitiveAssistance,
  timeScaleFactor = timeScaleFactor,
  themeMode = themeMode.name,
  language = language.code,
  ageBand = ageBand?.name
)

/**
 * Una partida de Contrarreloj (Tarea 177; `data/TimeTrialDay.kt`): su propia tabla, no `game_results`: el Contrarreloj no mueve el nivel, los puntos ni lo «medido» de la persona (sí cuenta como día jugado para la racha y las farolas). La base va entera en el respaldo.
 * Un día, un juego, la versión de la pista con que se jugó, los aciertos (la marca), los errores, el paso más alto que alcanzó la pista y los segundos.
 */
@Entity(tableName = "time_trial_results")
data class TimeTrialResultEntity(
  @PrimaryKey val id: String,
  val gameId: String,
  val day: String,
  val trackVersion: Int,
  val hits: Int,
  val errors: Int,
  val level: Int,
  val seconds: Int,
  val timestamp: Long
)

fun TimeTrialResultEntity.toDomain(): com.example.data.TimeTrialResult =
  com.example.data.TimeTrialResult(id = id, gameId = gameId, day = day, trackVersion = trackVersion, hits = hits, errors = errors, level = level, seconds = seconds, timestamp = timestamp)

fun com.example.data.TimeTrialResult.toEntity(): TimeTrialResultEntity =
  TimeTrialResultEntity(id = id, gameId = gameId, day = day, trackVersion = trackVersion, hits = hits, errors = errors, level = level, seconds = seconds, timestamp = timestamp)
