package com.example.bridge

import android.content.Intent
import android.util.Log
import com.example.MainActivity
import com.example.NeuroVidaApplication
import com.example.diag.ErrorLog
import com.example.model.GamePlayResult
import com.squareup.moshi.JsonClass
import com.squareup.moshi.Moshi
import com.unity3d.player.UnityPlayer
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch

/**
 * Receptor Unity -> Nativo, compartido entre todos los juegos migrados (Fase 1: Secuencia
 * Lumínica; Fase 2: Parejas Ocultas -- ver NeuroVida/CLAUDE.md). Unity lo invoca así, desde
 * `unity/NeuroVidaCore/Assets/Scripts/Bridge/NativeBridge.cs`:
 * `AndroidJavaClass("com.example.bridge.NativeReceiver").CallStatic("onGameFinished", json)`
 * -- SIEMPRE el mismo método nativo sea cual sea el juego, así que acá adentro hay que
 * mirar `game_id` para saber qué forma de `session_metrics` esperar antes de parsear en
 * serio (cada juego tiene campos de telemetría propios -- ver `SequenceTelemetry.cs` vs
 * `CardsTelemetry.cs` del lado Unity).
 *
 * Sin `Context` disponible en esa llamada -- de ahí [NeuroVidaApplication], que expone el
 * mismo [com.example.data.NeuroVidaRepository] que usaría
 * `NeuroVidaViewModel.finishActiveGame` si el juego se jugara desde Compose. El mapeo acá
 * es deliberadamente igual de simple que ese método: `recordGameResult` ya hace toda la
 * lógica real (nivel adaptativo, maestría, rank ELO); no hay estado de sesión de la UI
 * (activeGame/lastResult) que replicar porque esta Activity de Unity es standalone, no
 * pasa por el overlay de juego activo del ViewModel.
 */
object NativeReceiver {
  private const val TAG = "NativeReceiver"

  @JsonClass(generateAdapter = true)
  data class GameIdPeekDto(val game_id: String)

  @JsonClass(generateAdapter = true)
  data class SequenceSessionMetricsDto(
    val correct_rounds: Int,
    val total_rounds: Int,
    val calculated_score: Int,
    val average_response_time_ms: Double,
    val final_span_length: Int,
    val level: Int,
    val timed: Boolean,
    // Nivel más alto alcanzado (1..16+); 0 en versiones viejas de Unity. Se guarda como rating 0..1 (memoria).
    val peak_level: Int = 0
  )

  @JsonClass(generateAdapter = true)
  data class SequenceTelemetryDto(
    val user_id: String,
    val game_id: String,
    val session_metrics: SequenceSessionMetricsDto
  )

  @JsonClass(generateAdapter = true)
  data class CardsSessionMetricsDto(
    val matched_pairs: Int,
    val attempts: Int,
    val calculated_score: Int,
    val level: Int,
    val timed: Boolean
  )

  @JsonClass(generateAdapter = true)
  data class CardsTelemetryDto(
    val user_id: String,
    val game_id: String,
    val session_metrics: CardsSessionMetricsDto
  )

  @JsonClass(generateAdapter = true)
  data class StroopSessionMetricsDto(
    val correct_trials: Int,
    val total_trials: Int,
    val calculated_score: Int,
    val average_response_time_ms: Int,
    val level: Int,
    val timed: Boolean,
    // DDA común (Unity): rating final normalizado 0..1 y nivel máximo alcanzado. Opcionales para
    // no romper versiones anteriores; hoy solo se registran (falta persistirlos entre sesiones).
    val end_rating: Double? = null,
    val mode_trials: Int = 0,
    val mode_hits: Int = 0,
    val peak_level: Int = 0,
    // Solo Piloto Estelar: costo de multitarea en % (-1 = no aplica / sin datos).
    val multitask_cost: Int = -1,
    // Solo Radar: vistazo en ms (-1 = no aplica) y astronautas en esas rondas; rescatados/mostrados por dirección (8)
    // y cerca/lejos (2); captura en las lluvias (-1 = sin medida); robots mostrados/tocados.
    val glance_ms: Int = -1,
    val glance_load: Double = -1.0,
    val sector_hits: List<Int>? = null,
    val sector_trials: List<Int>? = null,
    val ring_hits: List<Int>? = null,
    val ring_trials: List<Int>? = null,
    val capture: Double = -1.0,
    val robots_shown: Int = 0,
    val robots_touched: Int = 0,
    // Solo Satélites: seguimiento (satélites a la vez) y velocidad superada (-1 = no aplica).
    val tracking_capacity: Double = -1.0,
    val tracking_targets: Double = -1.0,
    val tracking_speed: Double = -1.0,
    // Solo Freno de Emergencia: tiempo de frenado (ms, -1 = sin estimación), altos frenados / totales, récord.
    val brake_ms: Int = -1,
    val stops_ok: Int = 0,
    val stops_total: Int = 0,
    val brake_best_ssd_ms: Int = -1,
    // Solo Aterrizaje Lunar: error medio (%, -1 = no aplica), blanco y aterrizaje por intento (0..1), dianas.
    val numline_error_pct: Double = -1.0,
    val numline_true: List<Double>? = null,
    val numline_given: List<Double>? = null,
    val numline_bullseyes: Int = 0,
    // Solo Acoplamiento: giro mental (°/s, -1 = sin medida) y curva de giro (5 columnas, -1 = sin datos).
    val rotation_speed_dps: Int = -1,
    val rotation_curve_ms: List<Int>? = null,
    // Solo Tráfico Estelar: anticipación (ms, -1 = sin medida), % proactivo (-1) y más cápsulas a la vez.
    val traffic_lead_ms: Int = -1,
    val traffic_proactive_pct: Int = -1,
    val traffic_peak_pods: Int = 0,
    val mem_phase: String = "",
    val mem_seed: Int = -1,
    val mem_level: Int = -1,
    val mem_items: Int = -1,
    val mem_learned: Int = -1,
    val mem_learned_mask: Int = -1,
    val mem_recalled: Int = -1,
    val mem_recalled_mask: Int = -1,
    val mem_intrusions: Int = -1,
    val mem_order_ok: Int = -1,
    val mem_delay_s: Int = -1,
    // Solo Rumbo a Casa: a qué distancia de casa quedó (% de la distancia que había, -1 = no aplica), dónde quedó cada
    // vuelta (a lo largo y a lo ancho de la vuelta justa, en fracciones de esa distancia), faro (1/0) y perfectas.
    val homing_error_pct: Double = -1.0,
    val homing_along: List<Double>? = null,
    val homing_lateral: List<Double>? = null,
    val homing_beacon: List<Int>? = null,
    val homing_perfect: Int = 0,
    // Solo Correo Estelar (ver StroopTelemetry.cs): encargos por lugar y por hora, errores, reloj, ruta y sobres. -1 = no aplica.
    val mail_event_hits: Int = -1,
    val mail_event_total: Int = -1,
    val mail_commissions: Int = -1,
    val mail_lure_commissions: Int = -1,
    val mail_radio_hits: Int = -1,
    val mail_radio_total: Int = -1,
    val mail_radio_offtime: Int = -1,
    val mail_radio_period_s: Int = -1,
    val mail_clock_checks: Int = -1,
    val mail_clock_late: Int = -1,
    val mail_lane_pct: Int = -1,
    val mail_envelopes: Int = -1,
    val mail_asteroid_hits: Int = -1,
    val mail_asteroids: Int = -1,
    val mail_hull_intact_pct: Int = -1,
    val mail_emergencies: Int = -1
  )

  @JsonClass(generateAdapter = true)
  data class StroopTelemetryDto(
    val user_id: String,
    val game_id: String,
    val session_metrics: StroopSessionMetricsDto
  )

  private val moshi = Moshi.Builder().build()
  private val peekAdapter = moshi.adapter(GameIdPeekDto::class.java)
  private val sequenceAdapter = moshi.adapter(SequenceTelemetryDto::class.java)
  private val cardsAdapter = moshi.adapter(CardsTelemetryDto::class.java)
  private val stroopAdapter = moshi.adapter(StroopTelemetryDto::class.java)

  // No se reutiliza viewModelScope (no hay ViewModel vivo en este camino) -- ídem al
  // repositoryScope interno de NeuroVidaRepository, pero acá hace falta uno propio
  // porque la llamada entra desde fuera del ciclo de vida de cualquier ViewModel/Activity
  // Compose.
  private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

  const val ACTION_GAME_FINISHED = "com.example.action.UNITY_GAME_FINISHED"
  const val EXTRA_JSON = "json"

  /** Extra del Intent de vuelta a la app: la pantalla de juego (Unity) se ocultó y la app vuelve al frente. */
  const val EXTRA_RETURN_FROM_GAME = "neurovida_return_from_game"

  /** Extra del Intent de vuelta: la partida quedó EN PAUSA ("Salir" del menú de pausa), no terminó ni se abandonó. */
  const val EXTRA_PAUSED = "neurovida_game_paused"

  /** Resultado de la partida en curso, guardado en el proceso de Unity hasta que el usuario vuelve a la app. */
  @Volatile private var pendingResultJson: String? = null
  @Volatile private var pendingLaunchId: String? = null

  /** Id del lanzamiento que está jugando Unity (lo pone [UnityGameLauncher] en el Intent). Proceso `:unity`. */
  private fun currentLaunchId(): String? =
    UnityPlayer.currentActivity?.intent?.getStringExtra(UnityGameLauncher.EXTRA_LAUNCH_ID)

  /**
   * Unity ya pintó el juego (proceso `:unity`, hilo de Unity): se quita la pantalla de carga que cubría el
   * arranque en frío ([UnityLoadingOverlay]). Sin capa visible no hace nada.
   */
  @JvmStatic
  fun onGameShown() = UnityLoadingOverlay.hideFromAnyThread()

  /**
   * Punto de entrada desde Unity al terminar la partida (proceso `:unity`). El resultado se guarda para
   * entregarlo junto con la vuelta a la app ([returnToApp]) y además viaja por un broadcast explícito al proceso
   * principal ([UnityResultReceiver] -> [handleFinished]), para no perderlo si el usuario nunca vuelve (Inicio,
   * Unity cerrado en segundo plano). [UnityResultInbox] evita procesarlo dos veces.
   */
  @JvmStatic
  fun onGameFinished(json: String) {
    val launchId = currentLaunchId()
    pendingResultJson = json
    pendingLaunchId = launchId
    val app = NeuroVidaApplication.instance
    app.sendBroadcast(
      Intent(ACTION_GAME_FINISHED).setPackage(app.packageName)
        .putExtra(EXTRA_JSON, json)
        .putExtra(UnityGameLauncher.EXTRA_LAUNCH_ID, launchId)
    )
  }

  /**
   * "Continuar" o Atrás en Unity (proceso `:unity`): trae la app al frente SIN cerrar Unity, que queda en pausa
   * detrás. Así la próxima partida no arranca el motor desde cero (eran 5-8 s por juego). El Intent lleva el
   * resultado si la partida terminó, para que la app lo muestre al instante sin esperar al broadcast.
   * Con [paused] la partida queda en pausa dentro de Unity (menú de pausa, "Salir"): la app la recuerda y, si se
   * vuelve a abrir ese juego, la retoma con el mismo id de lanzamiento.
   * Devuelve false si no pudo (Unity usa entonces el camino viejo: cerrar su Activity).
   */
  @JvmStatic
  @JvmOverloads
  fun returnToApp(paused: Boolean = false): Boolean {
    val activity = UnityPlayer.currentActivity ?: return false
    val launchId = currentLaunchId()
    val intent = Intent(activity, MainActivity::class.java)
      .addFlags(Intent.FLAG_ACTIVITY_REORDER_TO_FRONT)
      .putExtra(EXTRA_RETURN_FROM_GAME, true)
      .putExtra(UnityGameLauncher.EXTRA_LAUNCH_ID, launchId)
      .putExtra(EXTRA_PAUSED, paused)
    if (!paused) {
      val json = pendingResultJson
      if (json != null && pendingLaunchId == launchId) intent.putExtra(EXTRA_JSON, json)
      pendingResultJson = null
      pendingLaunchId = null
    }
    activity.runOnUiThread { activity.startActivity(intent) }
    return true
  }

  /** Corre en el proceso principal (broadcast): entrega el resultado a la UI o lo guarda, una sola vez. */
  fun handleFinished(json: String, launchId: String?) {
    if (!UnityResultInbox.claim(launchId)) return // ya llegó con la vuelta a la app
    val result = parse(json) ?: return

    // Si la UI está escuchando (ViewModel vivo), ella guarda el resultado y muestra la pantalla de resultado de
    // la app. Si no (Android cerró la app mientras Unity estaba al frente) y la partida era de un flujo de la app
    // (camino diario, evaluación...), queda pendiente para que la app la procese al volver y siga el flujo. Solo las
    // partidas sueltas (herramientas de prueba) se guardan directo.
    if (UnityResultBus.publish(FinishedGame(result, launchId))) {
      Log.i(TAG, "Resultado de Unity entregado a la UI: $result")
    } else if (GameSessionStore.hasInFlight(launchId)) {
      Log.i(TAG, "Resultado de Unity pendiente hasta que vuelva la app: $result")
      GameSessionStore.savePendingResult(launchId, json)
    } else {
      scope.launch {
        Log.i(TAG, "Persistiendo resultado de Unity: $result")
        NeuroVidaApplication.instance.repository.recordGameResult(result)
      }
    }
  }

  /** Telemetría de Unity (JSON) -> [GamePlayResult]; null si no se puede leer. */
  fun parse(json: String): GamePlayResult? {
    val gameId = try {
      peekAdapter.fromJson(json)?.game_id
    } catch (e: Exception) {
      Log.e(TAG, "JSON de telemetría inválido (sin game_id legible): $json", e)
      ErrorLog.record("RESULTADO", "Una partida no se guardó: el resultado no traía juego legible. $json", e)
      null
    } ?: return null

    return when (gameId) {
      "secuencia" -> parseSequenceResult(json)
      "parejas" -> parseCardsResult(json)
      // Comparación, Cambio de Chip, Ruta del Tesoro, Series, Cálculo, Anagramas y Piloto Estelar reusan el mismo esquema de telemetría por ensayos que Stroop.
      "stroop", "comparacion", "cambiochip", "rutatesoro", "series", "calculo", "anagramas", "piloto", "radar", "satelites", "freno", "aterrizaje", "acoplamiento", "trafico", "bitacora", "rumbo", "correo" -> parseStroopResult(json)
      else -> {
        Log.e(TAG, "game_id \"$gameId\" no tiene un parser de telemetría registrado todavía.")
        ErrorLog.record("RESULTADO", "Una partida no se guardó: el juego «$gameId» no tiene lector de resultados en la app.")
        null
      }
    }
  }

  private fun parseSequenceResult(json: String): GamePlayResult? {
    val telemetry = try {
      sequenceAdapter.fromJson(json)
    } catch (e: Exception) {
      Log.e(TAG, "JSON de telemetría de Secuencia Lumínica inválido: $json", e)
      ErrorLog.record("RESULTADO", "Una partida de Secuencia Lumínica no se guardó: resultado ilegible. $json", e)
      null
    } ?: return null

    val metrics = telemetry.session_metrics
    return GamePlayResult(
      gameId = telemetry.game_id,
      score = metrics.calculated_score.coerceIn(0, 100),
      correctAnswers = metrics.correct_rounds,
      totalTrials = metrics.total_rounds,
      // Eco de los valores con que se lanzó la partida (ver UnityGameLauncher /
      // SequenceInitConfig.config) -- mismo criterio que
      // NeuroVidaViewModel.finishActiveGame, que tampoco recalcula nada, solo reusa
      // ActiveGameSession.level/.timed.
      timed = metrics.timed,
      level = metrics.level,
      endRating = metrics.peak_level.takeIf { it > 0 }?.let { com.example.data.ratingFromSequencePeak(it) }
    )
  }

  private fun parseCardsResult(json: String): GamePlayResult? {
    val telemetry = try {
      cardsAdapter.fromJson(json)
    } catch (e: Exception) {
      Log.e(TAG, "JSON de telemetría de Parejas Ocultas inválido: $json", e)
      ErrorLog.record("RESULTADO", "Una partida de Parejas Ocultas no se guardó: resultado ilegible. $json", e)
      null
    } ?: return null

    val metrics = telemetry.session_metrics
    return GamePlayResult(
      gameId = telemetry.game_id,
      score = metrics.calculated_score.coerceIn(0, 100),
      correctAnswers = metrics.matched_pairs,
      totalTrials = metrics.attempts,
      timed = metrics.timed,
      level = metrics.level
    )
  }

  private fun parseStroopResult(json: String): GamePlayResult? {
    val telemetry = try {
      stroopAdapter.fromJson(json)
    } catch (e: Exception) {
      Log.e(TAG, "JSON de telemetría de Tinta o Palabra inválido: $json", e)
      ErrorLog.record("RESULTADO", "Una partida no se guardó: el resultado (juegos con la telemetría común) es ilegible. $json", e)
      null
    } ?: return null

    val metrics = telemetry.session_metrics
    Log.i(TAG, "DDA ${telemetry.game_id}: end_rating=${metrics.end_rating} peak_level=${metrics.peak_level}")
    // Rumbo a Casa: una terna por viaje (dónde quedó a lo largo y a lo ancho de la vuelta justa, y si había faro);
    // solo si las tres listas vienen completas y del mismo largo.
    // Correo Estelar: sus campos solo valen si el juego es Correo y trajo encargos por lugar.
    val mail = telemetry.game_id == "correo" && metrics.mail_event_total >= 0
    val homing = run {
      val along = metrics.homing_along
      val lateral = metrics.homing_lateral
      val beacon = metrics.homing_beacon
      if (telemetry.game_id != "rumbo" || along.isNullOrEmpty() || lateral == null || beacon == null ||
        lateral.size != along.size || beacon.size != along.size
      ) null
      else along.indices.map { i -> Triple(along[i].toFloat(), lateral[i].toFloat(), beacon[i] == 1) }
    }
    return GamePlayResult(
      gameId = telemetry.game_id,
      score = metrics.calculated_score.coerceIn(0, 100),
      correctAnswers = metrics.correct_trials,
      totalTrials = metrics.total_trials,
      timed = metrics.timed,
      level = metrics.level,
      endRating = metrics.end_rating?.toFloat(),
      modeTrials = metrics.mode_trials,
      modeHits = metrics.mode_hits,
      multitaskCost = metrics.multitask_cost.takeIf { it >= 0 },
      glanceMs = metrics.glance_ms.takeIf { it > 0 },
      glanceLoad = metrics.glance_load.takeIf { it > 0.0 }?.toFloat(),
      sectorHits = metrics.sector_hits?.takeIf { it.size == 8 },
      sectorTrials = metrics.sector_trials?.takeIf { it.size == 8 },
      ringHits = metrics.ring_hits?.takeIf { it.size == 2 },
      ringTrials = metrics.ring_trials?.takeIf { it.size == 2 },
      captureK = metrics.capture.takeIf { it >= 0.0 }?.toFloat(),
      robotsShown = metrics.robots_shown.takeIf { it > 0 && telemetry.game_id == "radar" },
      robotsTouched = metrics.robots_touched.takeIf { metrics.robots_shown > 0 && telemetry.game_id == "radar" },
      trackingCapacity = metrics.tracking_capacity.takeIf { it >= 0.0 }?.toFloat(),
      trackingTargets = metrics.tracking_targets.takeIf { it > 0.0 }?.toFloat(),
      trackingSpeed = metrics.tracking_speed.takeIf { it > 0.0 }?.toFloat(),
      brakeMs = metrics.brake_ms.takeIf { it > 0 },
      stopsOk = metrics.stops_ok.takeIf { metrics.stops_total > 0 },
      stopsTotal = metrics.stops_total.takeIf { it > 0 },
      brakeBestSsdMs = metrics.brake_best_ssd_ms.takeIf { it > 0 },
      numlineErrorPct = metrics.numline_error_pct.takeIf { it >= 0.0 }?.toFloat(),
      numlineTrue = metrics.numline_true?.map { it.toFloat() }?.takeIf { it.isNotEmpty() },
      numlineGiven = metrics.numline_given?.map { it.toFloat() }?.takeIf { it.isNotEmpty() },
      numlineBullseyes = metrics.numline_bullseyes.takeIf { metrics.numline_error_pct >= 0.0 },
      rotationSpeedDps = metrics.rotation_speed_dps.takeIf { it > 0 },
      rotationCurveMs = metrics.rotation_curve_ms?.takeIf { it.size == 5 && it.any { v -> v > 0 } }?.map { v -> v.takeIf { it > 0 } },
      trafficLeadMs = metrics.traffic_lead_ms.takeIf { it >= 0 },
      trafficProactivePct = metrics.traffic_proactive_pct.takeIf { it >= 0 },
      trafficPeakPods = metrics.traffic_peak_pods.takeIf { it > 0 && telemetry.game_id == "trafico" },
      memPhase = metrics.mem_phase.takeIf { telemetry.game_id == "bitacora" },
      memSeed = metrics.mem_seed.takeIf { it >= 0 && telemetry.game_id == "bitacora" },
      memLevel = metrics.mem_level.takeIf { it > 0 },
      memItems = metrics.mem_items.takeIf { it > 0 },
      memLearned = metrics.mem_learned.takeIf { it >= 0 },
      memLearnedMask = metrics.mem_learned_mask.takeIf { it >= 0 },
      memRecalled = metrics.mem_recalled.takeIf { it >= 0 },
      memRecalledMask = metrics.mem_recalled_mask.takeIf { it >= 0 },
      memIntrusions = metrics.mem_intrusions.takeIf { it >= 0 },
      memOrderOk = metrics.mem_order_ok.takeIf { it >= 0 },
      memDelayS = metrics.mem_delay_s.takeIf { it >= 0 },
      homingErrorPct = metrics.homing_error_pct.takeIf { it >= 0.0 }?.toFloat(),
      homingAlong = homing?.let { h -> h.map { it.first } },
      homingLateral = homing?.let { h -> h.map { it.second } },
      homingBeacon = homing?.let { h -> h.map { it.third } },
      homingPerfect = metrics.homing_perfect.takeIf { metrics.homing_error_pct >= 0.0 },
      mailEventHits = metrics.mail_event_hits.takeIf { mail },
      mailEventTotal = metrics.mail_event_total.takeIf { mail },
      mailCommissions = metrics.mail_commissions.takeIf { mail },
      mailLureCommissions = metrics.mail_lure_commissions.takeIf { mail },
      mailRadioHits = metrics.mail_radio_hits.takeIf { mail },
      mailRadioTotal = metrics.mail_radio_total.takeIf { mail },
      mailRadioOfftime = metrics.mail_radio_offtime.takeIf { mail },
      mailRadioPeriodS = metrics.mail_radio_period_s.takeIf { mail },
      mailClockChecks = metrics.mail_clock_checks.takeIf { mail },
      mailClockLate = metrics.mail_clock_late.takeIf { mail },
      mailLanePct = metrics.mail_lane_pct.takeIf { mail },
      mailEnvelopes = metrics.mail_envelopes.takeIf { mail },
      mailAsteroidHits = metrics.mail_asteroid_hits.takeIf { mail && it >= 0 },
      mailAsteroids = metrics.mail_asteroids.takeIf { mail && it >= 0 },
      mailHullIntactPct = metrics.mail_hull_intact_pct.takeIf { mail && it >= 0 },
      mailEmergencies = metrics.mail_emergencies.takeIf { mail && it >= 0 }
    )
  }
}
