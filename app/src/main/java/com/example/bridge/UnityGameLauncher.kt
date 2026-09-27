package com.example.bridge

import android.content.Context
import android.content.Intent
import com.example.model.AgeBand
import com.squareup.moshi.JsonClass
import com.squareup.moshi.Moshi
import com.unity3d.player.UnityPlayerGameActivity

/**
 * Arma el Intent que abre un juego en Unity. La configuración (juego, nivel, modo, edad, rating guardado, evaluación)
 * viaja como JSON en un extra del Intent; Unity la lee al arrancar la partida
 * (`Assets/Scripts/Bootstrap/LaunchIntentConfigReader.cs`, vía `UnityPlayer.currentActivity.getIntent()`).
 */
object UnityGameLauncher {
  const val EXTRA_CONFIG_JSON = "neurovida_game_config_json"

  /** Id único de cada partida lanzada. Unity queda vivo entre partidas: con este id detecta que llegó una nueva
   *  (Intent nuevo con REORDER_TO_FRONT) y la app sabe a qué partida pertenece el resultado que vuelve. */
  const val EXTRA_LAUNCH_ID = "neurovida_launch_id"

  @JsonClass(generateAdapter = true)
  data class ConfigDetailsDto(
    val level: Int,
    val base_intensity: Int,
    val timed: Boolean,
    val age_band: String,
    val sound_enabled: Boolean,
    // Vibración corta en aciertos importantes, errores y fin de partida (Ajustes > Vibración). Unity la
    // da por activada si falta (versiones viejas de la app).
    val haptics_enabled: Boolean = true,
    // DDA común: rating guardado del juego (0..1). `has_dda_rating` evita confundir "sin dato" con 0.
    val has_dda_rating: Boolean = false,
    val dda_rating: Double = 0.0,
    // Evaluación inicial "Tu punto de partida" (ver data/Baseline.kt): partida corta que busca el nivel; paso N
    // de M se muestra en la cuenta regresiva. En la evaluación NO se manda el rating guardado (se mide de cero).
    val assessment: Boolean = false,
    val assessment_step: Int = 0,
    val assessment_total: Int = 0,
    // Solo Bitácora de Misión (ver data/MissionLog.kt): fase, semilla, nivel de la misión y segundos desde la transmisión.
    val memory_phase: String = "",
    val memory_seed: Int = 0,
    val memory_level: Int = 0,
    val memory_elapsed_s: Int = 0
  )

  @JsonClass(generateAdapter = true)
  data class InitConfigDto(
    val user_id: String,
    val game_id: String,
    val config: ConfigDetailsDto
  )

  private val moshi = Moshi.Builder().build()
  private val adapter = moshi.adapter(InitConfigDto::class.java)

  /**
   * Intent para jugar cualquiera de los 9 juegos en Unity desde el menú principal / sesión diaria. Trae al
   * frente la Activity de Unity si ya está viva (FLAG_ACTIVITY_REORDER_TO_FRONT: sin volver a arrancar el motor)
   * con un [EXTRA_LAUNCH_ID] nuevo. La vuelta a la app llega a `MainActivity.onNewIntent` (ver
   * `NativeReceiver.returnToApp`).
   */
  fun buildGameIntent(
    context: Context,
    gameId: String,
    userId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean = true,
    launchId: String? = null, // una partida en pausa se retoma con SU id (Unity no la reinicia)
    assessmentStep: Int = 0,  // 1..assessmentTotal en la evaluación inicial; 0 = partida normal
    assessmentTotal: Int = 0,
    memory: com.example.data.MemoryLaunch? = null // Bitácora de Misión: transmisión o informe de la misión del día
  ): Intent = buildIntent(context, userId, gameId, level, baseIntensity, timed, ageBand, soundEnabled, launchId, assessmentStep, assessmentTotal, memory)

  private fun buildIntent(
    context: Context,
    userId: String,
    gameId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean,
    launchId: String? = null,
    assessmentStep: Int = 0,
    assessmentTotal: Int = 0,
    memory: com.example.data.MemoryLaunch? = null
  ): Intent {
    val assessment = assessmentStep > 0
    val savedRating = if (assessment) -1f else com.example.NeuroVidaApplication.instance.repository.gameDdaRating.value[gameId] ?: -1f
    val config = InitConfigDto(
      user_id = userId,
      game_id = gameId, // mismo id de texto que GameRegistry (Models.kt), no un int
      config = ConfigDetailsDto(
        level = level,
        base_intensity = baseIntensity,
        timed = timed,
        age_band = ageBand.name, // "SENIOR"/"ADULT"/"UNDER_18" -- coincide 1:1 con DdaUserProfileConfig.ParseAgeBand en C#
        sound_enabled = soundEnabled,
        haptics_enabled = com.example.NeuroVidaApplication.instance.repository.userSettings.value.hapticsEnabled,
        has_dda_rating = savedRating >= 0f,
        dda_rating = savedRating.coerceAtLeast(0f).toDouble(),
        assessment = assessment,
        assessment_step = assessmentStep,
        assessment_total = assessmentTotal,
        memory_phase = memory?.phase ?: "",
        memory_seed = memory?.seed ?: 0,
        memory_level = memory?.level ?: 0,
        memory_elapsed_s = memory?.elapsedS ?: 0
      )
    )
    val json = adapter.toJson(config)

    return Intent(context, UnityPlayerGameActivity::class.java)
      .addFlags(Intent.FLAG_ACTIVITY_REORDER_TO_FRONT)
      .putExtra(EXTRA_CONFIG_JSON, json)
      .putExtra(EXTRA_LAUNCH_ID, launchId ?: java.util.UUID.randomUUID().toString())
  }
}
