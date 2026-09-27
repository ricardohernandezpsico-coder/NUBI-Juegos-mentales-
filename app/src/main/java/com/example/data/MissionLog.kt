package com.example.data

import android.content.Context

/**
 * Parámetros de una partida de Bitácora de Misión que viajan a Unity (ver `SequenceConfigDetails.memory_*`).
 * [phase]: "" = partida completa (transmisión, patrulla e informe), "encode" = solo la transmisión, "recall" = solo el
 * informe. [level] y [elapsedS] solo se usan en el informe (la misión se rearma con la misma semilla y nivel).
 */
data class MemoryLaunch(val phase: String, val seed: Int, val level: Int = 0, val elapsedS: Int = 0)

/** La misión del día (la transmisión recibida) y lo acumulado en la bitácora. */
data class MissionState(
  val dateKey: String = "",
  val seed: Int = 0,
  val level: Int = 0,
  val items: Int = 0,
  val learnedMask: Int = 0,
  val encodedAt: Long = 0L,
  val reported: Boolean = false,
  /** Hallazgos recordados en su lugar, sumando todas las misiones (la "colección" de la bitácora). */
  val archivedTotal: Int = 0,
  val missions: Int = 0
)

/** En qué punto está la misión diaria. */
enum class MissionStep { TRANSMISION, ESPERA, INFORME, AL_DIA }

/**
 * Reglas de la misión diaria de Bitácora de Misión (lógica pura, con pruebas): la transmisión llega al empezar la
 * sesión y el informe se abre al terminarla (o pasados [MIN_DELAY_MS], si la persona juega de a poco). Un informe
 * que quedó pendiente de otro día se puede hacer igual (memoria a un día): hasta hacerlo no llega otra transmisión.
 */
object MissionLog {
  const val MIN_DELAY_MS = 10 * 60 * 1000L

  fun step(s: MissionState, today: String, now: Long, dailyDone: Boolean): MissionStep = when {
    s.encodedAt == 0L -> MissionStep.TRANSMISION
    s.reported -> if (s.dateKey == today) MissionStep.AL_DIA else MissionStep.TRANSMISION
    s.dateKey != today -> MissionStep.INFORME
    dailyDone || now - s.encodedAt >= MIN_DELAY_MS -> MissionStep.INFORME
    else -> MissionStep.ESPERA
  }

  /** Minutos que faltan para que se abra el informe por tiempo (0 si ya se puede). */
  fun minutesLeft(s: MissionState, now: Long): Int {
    val left = MIN_DELAY_MS - (now - s.encodedAt)
    return if (left <= 0) 0 else ((left + 59_999) / 60_000).toInt()
  }

  /** Retención (%): de lo aprendido en el primer repaso, cuánto se recordó en el informe. null si no se aprendió nada. */
  fun retentionPct(learnedMask: Int, recalledMask: Int): Int? {
    val learned = Integer.bitCount(learnedMask)
    if (learned == 0) return null
    val kept = Integer.bitCount(learnedMask and recalledMask)
    return Math.round(100f * kept / learned)
  }

  fun onEncoded(s: MissionState, today: String, now: Long, seed: Int, level: Int, items: Int, learnedMask: Int) =
    s.copy(dateKey = today, seed = seed, level = level, items = items, learnedMask = learnedMask, encodedAt = now, reported = false)

  fun onReported(s: MissionState, recalled: Int) =
    s.copy(reported = true, archivedTotal = s.archivedTotal + recalled.coerceAtLeast(0), missions = s.missions + 1)

  /** Partida completa suelta (desde la biblioteca): suma a la colección, no toca la misión del día. */
  fun onFullGame(s: MissionState, recalled: Int) = s.copy(archivedTotal = s.archivedTotal + recalled.coerceAtLeast(0))

  /** Texto de la demora ("a los 14 minutos", "a las 3 horas", "a los 50 segundos"). */
  fun delayLabel(seconds: Int): String = when {
    seconds < 90 -> "a los $seconds segundos"
    seconds < 2 * 3600 -> (Math.round(seconds / 60f)).let { if (it == 1) "al minuto" else "a los $it minutos" }
    else -> "a las ${Math.round(seconds / 3600f)} horas"
  }
}

/** Guarda la misión en SharedPreferences (se agrega sin migrar Room, como las otras preferencias de la app). */
class MissionLogStore(context: Context) {
  private val prefs = context.getSharedPreferences("mission_log", Context.MODE_PRIVATE)

  fun load(): MissionState = MissionState(
    dateKey = prefs.getString("dateKey", "") ?: "",
    seed = prefs.getInt("seed", 0),
    level = prefs.getInt("level", 0),
    items = prefs.getInt("items", 0),
    learnedMask = prefs.getInt("learnedMask", 0),
    encodedAt = prefs.getLong("encodedAt", 0L),
    reported = prefs.getBoolean("reported", false),
    archivedTotal = prefs.getInt("archivedTotal", 0),
    missions = prefs.getInt("missions", 0)
  )

  fun save(s: MissionState) {
    prefs.edit()
      .putString("dateKey", s.dateKey)
      .putInt("seed", s.seed)
      .putInt("level", s.level)
      .putInt("items", s.items)
      .putInt("learnedMask", s.learnedMask)
      .putLong("encodedAt", s.encodedAt)
      .putBoolean("reported", s.reported)
      .putInt("archivedTotal", s.archivedTotal)
      .putInt("missions", s.missions)
      .apply()
  }

  fun clear() = prefs.edit().clear().apply()
}
