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
    // DDA común (desde el 3-oct): rating final 0..1 y secuencias/aciertos después del calentamiento. `end_rating` falta
    // en versiones viejas de Unity: entonces se usa el nivel más alto alcanzado (1..16), 0 si tampoco viene.
    val end_rating: Double? = null,
    val mode_trials: Int = 0,
    val mode_hits: Int = 0,
    val peak_level: Int = 0,
    // Solo Rastro de luz (ver SequenceTelemetry.cs): por familia (4) mejor largo, rondas y aciertos; familias vistas y desbloqueadas (bits). -1 = sin dato.
    val ras_best_len: List<Int>? = null,
    val ras_rounds: List<Int>? = null,
    val ras_hits: List<Int>? = null,
    val ras_modes_seen: Int = -1,
    val ras_new_modes: Int = -1,
    // «Confusión de modo» tomada por familia (a lo más 1 por modo y partida): no cuenta como error. Solo se lee; la medida final no cambia.
    val ras_mode_confusions: List<Int>? = null
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
    val timed: Boolean,
    // DDA común (desde el 3-oct): rating final 0..1 de la escalera de 10 niveles, nivel más alto y ensayos/aciertos
    // después del calentamiento. Opcionales: versiones viejas de Unity no los mandan (y entonces no hay rating).
    val end_rating: Double? = null,
    val peak_level: Int = 0,
    val mode_trials: Int = 0,
    val mode_hits: Int = 0
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
    // Solo Tinta o Palabra: interferencia de la palabra y costo de cambiar de orilla (ms, -1 = sin datos suficientes).
    val interference_ms: Int = -1,
    val switch_cost_ms: Int = -1,
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
    val mail_emergencies: Int = -1,
    // Solo Lluvia de meteoros: palabras vistas/tocadas por banda (6), inventadas vistas/tocadas por tipo (3), medianas de
    // reconocimiento en comunes y raras (ms, -1 = pocas muestras) y palabras raras acertadas (separadas por coma).
    val lex_band_seen: List<Int>? = null,
    val lex_band_hits: List<Int>? = null,
    val lex_fa_seen: List<Int>? = null,
    val lex_fa_hits: List<Int>? = null,
    val lex_rt_common_ms: Int = -1,
    val lex_rt_rare_ms: Int = -1,
    val lex_rare_words: String = "",
    // Solo ¿Verdad o disparate?: palabras por minuto (-1 = sin medir), por tipo de frase (6) tiempo medio, aciertos y vistas,
    // disparates evidentes y sutiles, mejor racha y ids de las frases marcadas "no está clara" (separados por coma).
    val sv_wpm: Int = -1,
    val sv_rt_type: List<Int>? = null,
    val sv_hits_type: List<Int>? = null,
    val sv_seen_type: List<Int>? = null,
    val sv_evident_hits: Int = -1,
    val sv_evident_seen: Int = -1,
    val sv_subtle_hits: Int = -1,
    val sv_subtle_seen: Int = -1,
    val sv_best_streak: Int = -1,
    val sv_unclear: String = "",
    // Solo Cosecha de palabras (-1 / "" = sin dato): palabras, comunes encontradas y disponibles, % de racimos, palabras en los
    // primeros y últimos 20 s, palabra estrella, la más larga o rara, comunes que faltaron (separadas por coma) y pistas.
    val harv_words: Int = -1,
    val harv_common_found: Int = -1,
    val harv_common_total: Int = -1,
    val harv_cluster_pct: Int = -1,
    val harv_first20: Int = -1,
    val harv_last20: Int = -1,
    val harv_star: String = "",
    val harv_best: String = "",
    val harv_missed: String = "",
    val harv_hints: Int = -1,
    // Solo La estrella intrusa (-1 / "" = sin dato): rondas y aciertos por tipo de grupo, mediana al tocar, mejor racha, «¿Qué las une?»
    // vistas y acertadas, y las claves de reglas (separadas por ';') de las láminas nuevas, falladas, repasadas y con nombre propio.
    val intr_seen_type: List<Int>? = null,
    val intr_hits_type: List<Int>? = null,
    val intr_rt_ms: Int = -1,
    val intr_best_streak: Int = -1,
    val intr_bonus_seen: Int = -1,
    val intr_bonus_hits: Int = -1,
    val intr_new_plates: String = "",
    val intr_review_new: String = "",
    val intr_review_done: String = "",
    val intr_named: String = "",
    // Solo «En la punta de la lengua» (-1 / "" = sin dato): palabras solas, con 1-2 ayudas, con las letras justas y mostradas por Nubi, tiempo medio hasta «¡La tengo!»
    // de las solas (ms), cada palabra con su lucero («reloj:o;búho:a»), las que Nubi mostró y las azules pendientes que se encontraron (separadas por ';').
    val punta_solo: Int = -1,
    val punta_pista: Int = -1,
    val punta_letras: Int = -1,
    val punta_vista: Int = -1,
    val punta_ms: Int = -1,
    val punta_words: String = "",
    val punta_blue: String = "",
    val punta_cleared: String = "",
    // Solo «Carga exacta» (-1 = sin dato): cargas logradas sin pista, con pista, por el camino más corto y tiempo medio (ms) de las logradas sin pista.
    val carga_alone: Int = -1,
    val carga_hinted: Int = -1,
    val carga_short: Int = -1,
    val carga_ms: Int = -1,
    // Solo «Engranajes» (-1 = sin dato): etapa más alta (1..5), luces del cohete (0..9), cohetes en órbita, despegues de la partida y tiempo medio por máquina (ms).
    val engr_etapa: Int = -1,
    val engr_lights: Int = -1,
    val engr_orbit: Int = -1,
    val engr_launches: Int = -1,
    val engr_ms: Int = -1,
    // Solo «Bodega de carga» (-1 = sin dato): grupo de etapa más alto (1..5), racha más larga, pedido más grande sin errores de la partida, récord de siempre y tiempo medio por objeto (ms).
    val bod_group: Int = -1,
    val bod_best_streak: Int = -1,
    val bod_biggest: Int = -1,
    val bod_best: Int = -1,
    val bod_ms: Int = -1,
    val bod_new: Int = -1
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
   * Desde Unity (proceso `:unity`): anota una línea de diagnóstico en el registro de errores del teléfono (Ajustes → «Enviar informe de errores»).
   * Sirve para ver lo que solo pasa en el teléfono real (hoy, los toques del tutorial). Nunca lanza.
   */
  @JvmStatic
  fun logDiagnostic(tag: String, message: String) = ErrorLog.record(tag, message)

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
      // Carga exacta, Anagramas y Piloto Estelar reusan el mismo esquema de telemetría por ensayos que Stroop.
      "stroop", "calculo", "engranajes", "bodega", "anagramas", "piloto", "radar", "satelites", "freno", "aterrizaje", "acoplamiento", "rumbo", "correo", "meteoros", "disparate", "cosecha", "intrusa" -> parseStroopResult(json)
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
      // El rating del motor común; solo si falta (Unity viejo) se deduce del nivel más alto alcanzado.
      endRating = metrics.end_rating?.toFloat()
        ?: metrics.peak_level.takeIf { it > 0 }?.let { com.example.data.ratingFromSequencePeak(it) },
      modeTrials = metrics.mode_trials,
      modeHits = metrics.mode_hits,
      // Rastro de luz: solo si traen las 4 familias completas (una versión vieja de Unity no manda nada de esto).
      rasBestLen = metrics.ras_best_len?.takeIf { it.size == 4 },
      rasRounds = metrics.ras_rounds?.takeIf { it.size == 4 },
      rasHits = metrics.ras_hits?.takeIf { it.size == 4 },
      rasModesSeen = metrics.ras_modes_seen.takeIf { it >= 0 },
      rasNewModes = metrics.ras_new_modes.takeIf { it >= 0 },
      rasModeConfusions = metrics.ras_mode_confusions?.takeIf { it.size == 4 }
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
      level = metrics.level,
      endRating = metrics.end_rating?.toFloat(),
      modeTrials = metrics.mode_trials,
      modeHits = metrics.mode_hits
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
      interferenceMs = metrics.interference_ms.takeIf { it >= 0 && telemetry.game_id == "stroop" },
      switchCostMs = metrics.switch_cost_ms.takeIf { it >= 0 && telemetry.game_id == "stroop" },
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
      mailEmergencies = metrics.mail_emergencies.takeIf { mail && it >= 0 },
      lexBandSeen = metrics.lex_band_seen?.takeIf { telemetry.game_id == "meteoros" && it.size == 6 },
      lexBandHits = metrics.lex_band_hits?.takeIf { telemetry.game_id == "meteoros" && it.size == 6 },
      lexFaSeen = metrics.lex_fa_seen?.takeIf { telemetry.game_id == "meteoros" && it.size == 3 },
      lexFaHits = metrics.lex_fa_hits?.takeIf { telemetry.game_id == "meteoros" && it.size == 3 },
      lexRtCommonMs = metrics.lex_rt_common_ms.takeIf { telemetry.game_id == "meteoros" && it > 0 },
      lexRtRareMs = metrics.lex_rt_rare_ms.takeIf { telemetry.game_id == "meteoros" && it > 0 },
      lexRareWords = metrics.lex_rare_words.split(",").map { it.trim() }.filter { it.isNotEmpty() }.distinct()
        .takeIf { telemetry.game_id == "meteoros" && it.isNotEmpty() },
      svWpm = metrics.sv_wpm.takeIf { telemetry.game_id == "disparate" && it > 0 },
      svRtType = metrics.sv_rt_type?.takeIf { telemetry.game_id == "disparate" && it.size == 6 },
      svHitsType = metrics.sv_hits_type?.takeIf { telemetry.game_id == "disparate" && it.size == 6 },
      svSeenType = metrics.sv_seen_type?.takeIf { telemetry.game_id == "disparate" && it.size == 6 },
      svEvidentHits = metrics.sv_evident_hits.takeIf { telemetry.game_id == "disparate" && it >= 0 },
      svEvidentSeen = metrics.sv_evident_seen.takeIf { telemetry.game_id == "disparate" && it >= 0 },
      svSubtleHits = metrics.sv_subtle_hits.takeIf { telemetry.game_id == "disparate" && it >= 0 },
      svSubtleSeen = metrics.sv_subtle_seen.takeIf { telemetry.game_id == "disparate" && it >= 0 },
      svBestStreak = metrics.sv_best_streak.takeIf { telemetry.game_id == "disparate" && it >= 0 },
      svUnclear = metrics.sv_unclear.split(",").map { it.trim() }.filter { it.isNotEmpty() }.distinct()
        .takeIf { telemetry.game_id == "disparate" && it.isNotEmpty() },
      harvWords = metrics.harv_words.takeIf { telemetry.game_id == "cosecha" && it >= 0 },
      harvCommonFound = metrics.harv_common_found.takeIf { telemetry.game_id == "cosecha" && it >= 0 },
      harvCommonTotal = metrics.harv_common_total.takeIf { telemetry.game_id == "cosecha" && it > 0 },
      harvClusterPct = metrics.harv_cluster_pct.takeIf { telemetry.game_id == "cosecha" && it in 0..100 },
      harvFirst20 = metrics.harv_first20.takeIf { telemetry.game_id == "cosecha" && it >= 0 },
      harvLast20 = metrics.harv_last20.takeIf { telemetry.game_id == "cosecha" && it >= 0 },
      harvStar = metrics.harv_star.trim().takeIf { telemetry.game_id == "cosecha" && it.isNotEmpty() },
      harvBest = metrics.harv_best.trim().takeIf { telemetry.game_id == "cosecha" && it.isNotEmpty() },
      harvMissed = metrics.harv_missed.split(",").map { it.trim() }.filter { it.isNotEmpty() }.distinct().take(5)
        .takeIf { telemetry.game_id == "cosecha" && it.isNotEmpty() },
      harvHints = metrics.harv_hints.takeIf { telemetry.game_id == "cosecha" && it >= 0 },
      intrSeenType = metrics.intr_seen_type?.takeIf { telemetry.game_id == "intrusa" && it.size == 6 },
      intrHitsType = metrics.intr_hits_type?.takeIf { telemetry.game_id == "intrusa" && it.size == 6 },
      intrRtMs = metrics.intr_rt_ms.takeIf { telemetry.game_id == "intrusa" && it > 0 },
      intrBestStreak = metrics.intr_best_streak.takeIf { telemetry.game_id == "intrusa" && it >= 0 },
      intrBonusSeen = metrics.intr_bonus_seen.takeIf { telemetry.game_id == "intrusa" && it >= 0 },
      intrBonusHits = metrics.intr_bonus_hits.takeIf { telemetry.game_id == "intrusa" && it >= 0 },
      intrNewPlates = com.example.data.Atlas.keys(metrics.intr_new_plates).takeIf { telemetry.game_id == "intrusa" },
      intrReviewNew = com.example.data.Atlas.keys(metrics.intr_review_new).takeIf { telemetry.game_id == "intrusa" },
      intrReviewDone = com.example.data.Atlas.keys(metrics.intr_review_done).takeIf { telemetry.game_id == "intrusa" },
      intrNamed = com.example.data.Atlas.keys(metrics.intr_named).takeIf { telemetry.game_id == "intrusa" },
      puntaSolo = metrics.punta_solo.takeIf { telemetry.game_id == "anagramas" && it >= 0 },
      puntaPista = metrics.punta_pista.takeIf { telemetry.game_id == "anagramas" && it >= 0 },
      puntaLetras = metrics.punta_letras.takeIf { telemetry.game_id == "anagramas" && it >= 0 },
      puntaVista = metrics.punta_vista.takeIf { telemetry.game_id == "anagramas" && it >= 0 },
      puntaMs = metrics.punta_ms.takeIf { telemetry.game_id == "anagramas" && it > 0 },
      puntaWords = com.example.data.Punta.entries(metrics.punta_words).takeIf { telemetry.game_id == "anagramas" && it.isNotEmpty() },
      puntaBlue = com.example.data.Punta.words(metrics.punta_blue).takeIf { telemetry.game_id == "anagramas" },
      puntaCleared = com.example.data.Punta.words(metrics.punta_cleared).takeIf { telemetry.game_id == "anagramas" },
      cargaAlone = metrics.carga_alone.takeIf { telemetry.game_id == "calculo" && it >= 0 },
      cargaHinted = metrics.carga_hinted.takeIf { telemetry.game_id == "calculo" && it >= 0 },
      cargaShort = metrics.carga_short.takeIf { telemetry.game_id == "calculo" && it >= 0 },
      cargaMs = metrics.carga_ms.takeIf { telemetry.game_id == "calculo" && it > 0 },
      engrEtapa = metrics.engr_etapa.takeIf { telemetry.game_id == "engranajes" && it >= 1 },
      engrLights = metrics.engr_lights.takeIf { telemetry.game_id == "engranajes" && it >= 0 },
      engrOrbit = metrics.engr_orbit.takeIf { telemetry.game_id == "engranajes" && it >= 0 },
      engrLaunches = metrics.engr_launches.takeIf { telemetry.game_id == "engranajes" && it >= 0 },
      engrMs = metrics.engr_ms.takeIf { telemetry.game_id == "engranajes" && it > 0 },
      bodGroup = metrics.bod_group.takeIf { telemetry.game_id == "bodega" && it >= 1 },
      bodBestStreak = metrics.bod_best_streak.takeIf { telemetry.game_id == "bodega" && it >= 0 },
      bodBiggest = metrics.bod_biggest.takeIf { telemetry.game_id == "bodega" && it >= 0 },
      bodBest = metrics.bod_best.takeIf { telemetry.game_id == "bodega" && it >= 0 },
      bodMs = metrics.bod_ms.takeIf { telemetry.game_id == "bodega" && it > 0 },
      bodNewRecord = metrics.bod_new.takeIf { telemetry.game_id == "bodega" && it >= 0 }?.let { it == 1 }
    )
  }
}
