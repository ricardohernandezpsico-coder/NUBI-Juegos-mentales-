package com.example.viewmodel

import android.app.Application
import android.content.Context
import android.os.Build
import android.os.VibrationEffect
import android.os.Vibrator
import android.os.VibratorManager
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.example.bridge.GameSessionStore
import com.example.data.NeuroVidaRepository
import com.example.model.*
import com.example.notification.CognitiveReminderWorker
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

enum class AppTab(val title: String, val iconName: String) {
  HOY("Hoy", "Today"),
  JUEGOS("Juegos", "SportsEsports"),
  PROGRESO("Progreso", "Insights"),
  AJUSTES("Ajustes", "Settings")
}

data class ActiveGameSession(
  val gameDef: GameDefinition,
  val level: Int,
  val timed: Boolean,
  val isDailyFlow: Boolean = false,
  // Progresión sin techo más allá de nivel 5 (Experto) — ver GameProgressEntity.masteryStreak.
  val intensity: Int = 0,
  // Partida en pausa que se retoma: se relanza Unity con este id de lanzamiento y la partida sigue donde quedó.
  val resumeLaunchId: String? = null,
  // Evaluación inicial "Tu punto de partida": paso 1..N (0 = partida normal). Ver data/Baseline.kt.
  val assessmentStep: Int = 0,
  // Bitácora de Misión: transmisión o informe de la misión del día (null = partida completa o cualquier otro juego).
  val memory: com.example.data.MemoryLaunch? = null,
  // Identidad de esta sesión (no de la partida): la usa la UI para que el estado guardado de una sesión anterior
  // (p. ej. "ya lancé Unity", que Android restaura al recrear la pantalla) no lo herede la siguiente.
  val sessionToken: String = java.util.UUID.randomUUID().toString()
)

/**
 * Estado de la evaluación inicial mientras se juega ([done] juegos terminados de BaselinePlan.steps, ratings
 * medidos por juego) y, al terminar, el mapa ([result]).
 */
data class BaselineRun(
  val done: Int = 0,
  val measured: Map<String, Float> = emptyMap(),
  val result: com.example.data.Baseline? = null
)

class NeuroVidaViewModel(application: Application) : AndroidViewModel(application) {
  // El mismo repositorio que usa el puente con Unity (una sola copia en memoria de ligas, logros y punto de partida).
  private val repository = (application as? com.example.NeuroVidaApplication)?.repository ?: NeuroVidaRepository(application)

  val userSettings = repository.userSettings
  val allProfiles = repository.allProfiles
  val gameHistory = repository.gameHistory
  val gameLevels = repository.gameLevels
  val gameIntensity = repository.gameIntensity
  val weeklyChallengeProgress = repository.weeklyChallengeProgress

  // Meta-progresión por dominio (etapa 4): DomainMasteryInfo deriva tier/label
  // a partir de la XP cruda que guarda el repositorio.
  val domainMasteryInfo: StateFlow<List<DomainMasteryInfo>> = repository.domainMastery.map { xpMap ->
    DomainType.values().map { d -> DomainMasteryInfo(domain = d, xp = xpMap[d] ?: 0) }
  }.stateIn(viewModelScope, SharingStarted.Eagerly, DomainType.values().map { DomainMasteryInfo(it, 0) })

  // Ranking ELO por juego (idea de Ricardo, 20-sep): GameRankInfo deriva tier/división
  // a partir del rating crudo que guarda el repositorio, uno por cada uno de los 9 juegos.
  val gameRanks: StateFlow<List<GameRankInfo>> = repository.gameRanks.map { ratingMap ->
    GameRegistry.allGames.map { g -> GameRankInfo(gameId = g.id, rating = ratingMap[g.id] ?: 0) }
  }.stateIn(viewModelScope, SharingStarted.Eagerly, GameRegistry.allGames.map { GameRankInfo(it.id, 0) })
  val dailySession = repository.dailySession

  // ---------- Bitácora de Misión: la misión del día (transmisión al empezar la sesión, informe al terminarla) ----------

  private val missionStore by lazy { com.example.data.MissionLogStore(getApplication<Application>()) }
  private val _mission = MutableStateFlow(com.example.data.MissionState())
  val mission: StateFlow<com.example.data.MissionState> = _mission.asStateFlow()

  /** En qué punto está la misión de hoy (lo lee Hoy y el flujo de la sesión diaria). */
  fun missionStep(now: Long = System.currentTimeMillis()): com.example.data.MissionStep {
    val session = dailySession.value
    return com.example.data.MissionLog.step(_mission.value, session.dateKey, now, session.completedCount >= session.gameIds.size)
  }

  /** Recibe la transmisión del día (Unity arma la misión con esta semilla y el nivel que corresponda). */
  fun startMissionTransmission(isDailyFlow: Boolean = false) {
    val seed = kotlin.random.Random.nextInt(1, Int.MAX_VALUE)
    launchMemory(com.example.data.MemoryLaunch(phase = "encode", seed = seed), isDailyFlow)
  }

  /** Abre el informe de la misión pendiente (misma semilla y nivel; Unity sabe cuánto tiempo pasó). */
  fun startMissionReport(isDailyFlow: Boolean = false) {
    val m = _mission.value
    if (m.encodedAt == 0L || m.reported) return
    val elapsed = ((System.currentTimeMillis() - m.encodedAt) / 1000L).coerceIn(0L, Int.MAX_VALUE.toLong()).toInt()
    launchMemory(com.example.data.MemoryLaunch(phase = "recall", seed = m.seed, level = m.level, elapsedS = elapsed), isDailyFlow)
  }

  private fun launchMemory(memory: com.example.data.MemoryLaunch, isDailyFlow: Boolean) {
    val def = GameRegistry.getById("bitacora") ?: return
    pausedGame = null
    _activeGame.value = ActiveGameSession(def, getEffectiveLevelForGame(def.id), false, isDailyFlow, memory = memory)
    _lastResult.value = null
  }

  /**
   * Resultado de Bitácora de Misión: actualiza la misión del día y la colección, y completa la retención (que
   * necesita lo aprendido en la transmisión, guardado acá). Devuelve el resultado completo para mostrar.
   */
  private fun applyMissionResult(result: GamePlayResult): GamePlayResult {
    val now = System.currentTimeMillis()
    val m = _mission.value
    val updated: com.example.data.MissionState
    val shown: GamePlayResult
    when (result.memPhase) {
      "encode" -> {
        updated = com.example.data.MissionLog.onEncoded(
          m, dailySession.value.dateKey, now, result.memSeed ?: 0, result.memLevel ?: 1, result.memItems ?: 0, result.memLearnedMask ?: 0
        )
        shown = result.copy(memArchivedTotal = updated.archivedTotal)
      }
      "recall" -> {
        val recalled = result.memRecalled ?: 0
        updated = com.example.data.MissionLog.onReported(m, recalled)
        shown = result.copy(
          memRetentionPct = com.example.data.MissionLog.retentionPct(m.learnedMask, result.memRecalledMask ?: 0),
          memArchivedTotal = updated.archivedTotal
        )
      }
      else -> {
        val recalled = result.memRecalled ?: 0
        updated = com.example.data.MissionLog.onFullGame(m, recalled)
        shown = result.copy(
          memRetentionPct = com.example.data.MissionLog.retentionPct(result.memLearnedMask ?: 0, result.memRecalledMask ?: 0),
          memArchivedTotal = updated.archivedTotal
        )
      }
    }
    _mission.value = updated
    missionStore.save(updated)
    return shown
  }

  /** Punto de partida: educación, metas y el mapa guardado (null = no hizo la evaluación). */
  val education = repository.education
  val goals = repository.goals
  val baseline = repository.baseline

  /** Evaluación en curso o su resultado por mostrar (null = no se está haciendo). */
  private val _baselineRun = MutableStateFlow<BaselineRun?>(null)
  val baselineRun: StateFlow<BaselineRun?> = _baselineRun.asStateFlow()

  /** Logros conseguidos durante la evaluación: se celebran al cerrar el mapa, no entre juego y juego. */
  private var baselineAchievements: List<String> = emptyList()

  /** Rango de edad con que arranca la evaluación (al salir del onboarding los ajustes todavía no se recargaron). */
  private var baselineAge: AgeBand? = null

  /** Cambia el estado de la evaluación y lo guarda en disco mientras está en curso (sobrevive a que Android cierre
   *  la app durante un juego; ver [GameSessionStore]). Con el mapa ya calculado o sin evaluación, no queda nada guardado. */
  private fun setBaselineRun(run: BaselineRun?) {
    _baselineRun.value = run
    GameSessionStore.saveBaseline(
      if (run == null || run.result != null) null
      else GameSessionStore.BaselineProgress(run.done, run.measured, baselineAge?.name)
    )
  }

  /** Empieza (o vuelve a empezar) la evaluación: 3 juegos cortos, uno tras otro. */
  fun startBaseline(age: AgeBand? = null) {
    baselineAge = age ?: userSettings.value.ageBand
    setBaselineRun(BaselineRun())
    baselineAchievements = emptyList()
    _lastResult.value = null
    launchBaselineStep(0)
  }

  /** Juega el siguiente juego de la evaluación (o repite el que quedó a medias). */
  fun continueBaseline() {
    val run = _baselineRun.value ?: return
    if (run.result == null && run.done < com.example.data.BaselinePlan.steps.size) launchBaselineStep(run.done)
  }

  private fun launchBaselineStep(index: Int) {
    val step = com.example.data.BaselinePlan.steps[index]
    val def = GameRegistry.getById(step.gameId) ?: return
    pausedGame = null
    _activeGame.value = ActiveGameSession(
      gameDef = def,
      level = com.example.data.BaselinePlan.startLevel(baselineAge ?: userSettings.value.ageBand),
      timed = step.timed,
      assessmentStep = index + 1
    )
  }

  /** "Terminar después" a mitad de la evaluación: se estima el punto de partida y queda para hacerla desde Perfil. */
  fun skipBaseline() {
    setBaselineRun(null)
    _activeGame.value = null
    _currentTab.value = AppTab.HOY
    viewModelScope.launch {
      repository.applyPriorIfNeeded(userSettings.value.ageBand, education.value)
      flushBaselineAchievements()
    }
  }

  /** Cierra el mapa ("Empezar mi camino"): vuelve a Hoy y celebra los logros que quedaron pendientes. */
  fun finishBaseline() {
    setBaselineRun(null)
    setTab(AppTab.HOY)
    flushBaselineAchievements()
  }

  private fun flushBaselineAchievements() {
    if (baselineAchievements.isNotEmpty()) _achievementQueue.value = _achievementQueue.value + baselineAchievements
    baselineAchievements = emptyList()
  }

  private fun onBaselineResult(session: ActiveGameSession, result: GamePlayResult) {
    val rating = result.endRating ?: (result.score / 100f)
    _activeGame.value = null
    viewModelScope.launch {
      val outcome = repository.recordGameResult(result, countsForDailySession = false)
      baselineAchievements = baselineAchievements + outcome.newAchievements
      val run = _baselineRun.value ?: BaselineRun()
      val measured = run.measured + (session.gameDef.id to rating)
      val done = maxOf(run.done, session.assessmentStep)
      if (done >= com.example.data.BaselinePlan.steps.size) {
        val baseline = com.example.data.buildBaseline(measured)
        repository.applyBaseline(baseline)
        setBaselineRun(BaselineRun(done, measured, baseline))
        triggerHapticFeedback(HapticType.SUCCESS)
      } else {
        setBaselineRun(BaselineRun(done, measured))
        triggerHapticFeedback(HapticType.LIGHT)
      }
    }
  }

  /** Ascensos de liga guardados (para marcarlos en el camino de Hoy). */
  val leagueEvents = repository.leagueEvents

  /** Logros conseguidos (id -> cuándo) y las cifras con que se calculan (para el avance "4/7" de los bloqueados). */
  val achievementUnlocks = repository.achievementUnlocks
  val achievementStats: StateFlow<com.example.data.AchievementStats> = combine(gameHistory, gameRanks) { hist, ranks ->
    com.example.data.computeAchievementStats(hist, ranks.associate { it.gameId to it.rating })
  }.stateIn(viewModelScope, SharingStarted.Eagerly, com.example.data.computeAchievementStats(emptyList(), emptyMap()))

  /** Nivel (0..1) por juego para Progreso: rating del DDA comun; en Secuencia/Parejas (motores propios) se
   *  aproxima con el nivel 1-5 si ya se jugaron; null = sin medir. */
  val gameLevelsForProgress: StateFlow<Map<String, Float?>> = combine(
    repository.gameDdaRating, gameLevels, gameHistory
  ) { dda, levels, hist ->
    val played = hist.map { it.gameId }.toSet()
    GameRegistry.allGames.associate { g ->
      val r = dda[g.id] ?: -1f
      g.id to when {
        r >= 0f -> r
        g.id in played -> ((levels[g.id] ?: 1) - 1) / 5f + 0.1f
        else -> null
      }
    }
  }.stateIn(viewModelScope, SharingStarted.Eagerly, emptyMap())

  private val _currentTab = MutableStateFlow(AppTab.HOY)
  val currentTab: StateFlow<AppTab> = _currentTab.asStateFlow()

  private val _activeGame = MutableStateFlow<ActiveGameSession?>(null)
  val activeGame: StateFlow<ActiveGameSession?> = _activeGame.asStateFlow()

  private val _lastResult = MutableStateFlow<Pair<GamePlayResult, Boolean>?>(null)
  val lastResult: StateFlow<Pair<GamePlayResult, Boolean>?> = _lastResult.asStateFlow()

  /** Si el resultado que se muestra es de un juego del camino diario (ofrece "Siguiente juego"). */
  private val _lastResultDaily = MutableStateFlow(false)
  val lastResultDaily: StateFlow<Boolean> = _lastResultDaily.asStateFlow()

  /** Ascenso de liga de la última partida (se celebra encima de la pantalla de resultado); null = no hubo. */
  private val _promotion = MutableStateFlow<LeaguePromotion?>(null)
  val promotion: StateFlow<LeaguePromotion?> = _promotion.asStateFlow()

  fun dismissPromotion() {
    _promotion.value = null
  }

  /** Logros conseguidos en las últimas partidas y todavía no celebrados (se muestran de a uno). */
  private val _achievementQueue = MutableStateFlow<List<String>>(emptyList())
  val achievementQueue: StateFlow<List<String>> = _achievementQueue.asStateFlow()

  fun dismissAchievement() {
    _achievementQueue.value = _achievementQueue.value.drop(1)
  }

  /** Solo depuración (botón en Ajustes): muestra la celebración de un logro. */
  fun debugShowAchievement() {
    _achievementQueue.value = _achievementQueue.value + "racha_7"
  }

  /** Solo depuración (botón en Ajustes): muestra la celebración sin tener que ganar 250 trofeos. */
  fun debugShowPromotion() {
    _promotion.value = LeaguePromotion(RankTier.PLATA, RankTier.BRONCE, gameId = "secuencia", rating = 255)
  }

  // Computed streak
  val currentStreak: StateFlow<Int> = combine(gameHistory) { history ->
    repository.calculateStreak(history[0])
  }.stateIn(viewModelScope, SharingStarted.Eagerly, 0)

  init {
    viewModelScope.launch { com.example.bridge.UnityResultBus.results.collect { onUnityResult(it.result, it.launchId) } }
    viewModelScope.launch {
      userSettings.collect { settings ->
        if (settings.notificationsEnabled) {
          CognitiveReminderWorker.scheduleDailyReminder(
            getApplication(),
            settings.reminderHour,
            settings.reminderMinute
          )
        } else {
          CognitiveReminderWorker.cancelReminder(getApplication())
        }
      }
    }
  }

  fun setTab(tab: AppTab) {
    _currentTab.value = tab
    _activeGame.value = null
    _lastResult.value = null
  }

  fun startDailySession() {
    if (launchMissionIfDue()) return
    val session = dailySession.value
    val nextGameId = if (session.completedCount < session.gameIds.size) {
      session.gameIds[session.completedCount]
    } else {
      session.gameIds.firstOrNull() ?: "calculo"
    }
    launchGame(nextGameId, isDailyFlow = true)
  }

  /**
   * La misión de Bitácora dentro de la sesión diaria: un informe pendiente de otro día va primero; la transmisión de
   * hoy, antes del primer juego; el informe de hoy, después del último. true si lanzó una fase de la misión.
   */
  private fun launchMissionIfDue(): Boolean {
    val session = dailySession.value
    val m = _mission.value
    return when (missionStep()) {
      com.example.data.MissionStep.INFORME ->
        if (m.dateKey != session.dateKey || session.completedCount >= session.gameIds.size) { startMissionReport(isDailyFlow = true); true } else false
      com.example.data.MissionStep.TRANSMISION ->
        if (session.completedCount == 0) { startMissionTransmission(isDailyFlow = true); true } else false
      else -> false
    }
  }

  fun getEffectiveLevelForGame(gameId: String): Int {
    val settings = userSettings.value
    val gameDef = GameRegistry.getById(gameId) ?: return 1
    return when (settings.difficultyMode) {
      DifficultyMode.ADAPTIVE -> gameLevels.value[gameId] ?: 1
      DifficultyMode.PRINCIPIANTE -> 1
      DifficultyMode.INTERMEDIO -> 3
      DifficultyMode.AVANZADO -> 5
      DifficultyMode.CUSTOM -> {
        when (gameDef.domain) {
          DomainType.MEMORIA -> settings.difficultyMemoria
          DomainType.ATENCION -> settings.difficultyAtencion
          DomainType.RAZONAMIENTO -> settings.difficultyRazonamiento
          DomainType.LENGUAJE -> settings.difficultyLenguaje
          DomainType.CALCULO -> settings.difficultyCalculo
          DomainType.VELOCIDAD -> settings.difficultyVelocidad
        }.coerceIn(1, 5)
      }
    }
  }

  // Solo el modo adaptativo acumula masteryStreak (juego real, historial real);
  // los modos de dificultad fija (Principiante/Intermedio/Avanzado/Personalizada)
  // no tienen ese concepto porque el usuario ya eligió congelar el nivel.
  fun getEffectiveIntensityForGame(gameId: String): Int {
    if (userSettings.value.difficultyMode != DifficultyMode.ADAPTIVE) return 0
    return gameIntensity.value[gameId] ?: 0
  }

  fun launchGame(gameId: String, customLevel: Int? = null, customTimed: Boolean? = null, isDailyFlow: Boolean = false) {
    // Si ese juego quedó en pausa ("Salir" del menú de pausa), se retoma en vez de empezar de cero. "Jugar de
    // nuevo" (customLevel) siempre es una partida nueva. Abrir otro juego descarta la pausa (Unity recarga).
    val paused = pausedGame
    pausedGame = null
    if (paused != null && paused.first.gameDef.id == gameId && customLevel == null) {
      _activeGame.value = paused.first.copy(
        isDailyFlow = isDailyFlow || paused.first.isDailyFlow,
        resumeLaunchId = paused.second
      )
      _lastResult.value = null
      return
    }
    val def = GameRegistry.getById(gameId) ?: return
    val lvl = customLevel ?: getEffectiveLevelForGame(gameId)
    val timed = customTimed ?: userSettings.value.defaultTimed
    val intensity = if (lvl >= 5) getEffectiveIntensityForGame(gameId) else 0
    _activeGame.value = ActiveGameSession(def, lvl, timed, isDailyFlow, intensity)
    _lastResult.value = null
  }

  /**
   * Resultado de una partida jugada en Unity (llega por [com.example.bridge.UnityResultBus]). Se guarda una sola
   * vez en Room; si coincide con la sesión activa se muestra la pantalla de resultado de la app.
   * Las partidas lanzadas desde los botones de depuración (sin sesión activa) solo se guardan.
   */
  fun onUnityResult(rawResult: GamePlayResult, launchId: String? = null) {
    // La sesión viva, o la anotada en disco si Android cerró la app durante el juego (ViewModel nuevo).
    val live = _activeGame.value?.takeIf { it.gameDef.id == rawResult.gameId }
    val stored = GameSessionStore.inFlight(launchId)?.takeIf { it.gameId == rawResult.gameId }?.let(::sessionFrom)
    if (live != null || stored != null) GameSessionStore.clearInFlight()
    val current = live ?: stored
    if (current != null && current.assessmentStep > 0) {
      onBaselineResult(current, rawResult)
      return
    }
    val result = when {
      rawResult.gameId == "bitacora" && rawResult.memPhase != null -> applyMissionResult(rawResult)
      rawResult.gameId == "contacto" && rawResult.contactLesson != null -> applyContactResult(rawResult)
      else -> rawResult
    }
    if (result.memPhase == "encode") {
      // La transmisión sola no es una partida completa: no suma a la liga ni al historial (el informe sí). Se muestra
      // su pantalla (cuánto se aprendió y cuándo llega el informe) y la sesión sigue.
      if (current != null) {
        _lastResultDaily.value = current.isDailyFlow
        _lastResult.value = Pair(result, false)
        _activeGame.value = null
      }
      return
    }
    viewModelScope.launch {
      val outcome = repository.recordGameResult(result)
      if (current != null) {
        _promotion.value = outcome.promotion(result.gameId)
        _achievementQueue.value = _achievementQueue.value + outcome.newAchievements
        _lastResultDaily.value = current.isDailyFlow
        _lastResult.value = Pair(result, outcome.didLevelUp)
        _activeGame.value = null
        triggerHapticFeedback(if (result.score >= 70) HapticType.SUCCESS else HapticType.LIGHT)
      }
    }
  }

  // ---------- Primer Contacto: el diccionario nuri (crece día a día; al empezar se repasan palabras de otros días) ----------

  private val contactStore by lazy { com.example.data.ContactDictionaryStore(getApplication<Application>()) }

  /** Lo descifrado entra al diccionario; lo repasado bien se renueva y lo olvidado sale (vuelve a aprenderse). */
  private fun applyContactResult(result: GamePlayResult): GamePlayResult {
    val today = com.example.data.Contact.today()
    val before = contactStore.load()
    val reviewed = result.contactReview.orEmpty()
    val maxDays = reviewed.mapNotNull { id -> before[id]?.let { (today - it.lastSeenDay).toInt() } }.maxOrNull()
    val after = com.example.data.Contact.apply(before, today, result.contactDecoded.orEmpty(), reviewed, result.contactReviewOk.orEmpty())
    contactStore.save(after)
    return result.copy(nuriKnown = after.size, contactReviewMaxDays = maxDays)
  }

  private fun sessionFrom(p: GameSessionStore.InFlight): ActiveGameSession? {
    val def = GameRegistry.getById(p.gameId) ?: return null
    return ActiveGameSession(def, p.level, p.timed, p.daily, p.intensity, assessmentStep = p.assessmentStep)
  }

  /** Resultado que llegó mientras la app estaba cerrada (ver [GameSessionStore]): se procesa como si fuera en vivo. */
  private fun processPendingResult(): Boolean {
    val (launchId, json) = GameSessionStore.takePendingResult() ?: return false
    val result = com.example.bridge.NativeReceiver.parse(json) ?: return false
    onUnityResult(result, launchId)
    return true
  }

  /** true entre que se lanza Unity y que vuelve la app (ver [onReturnedFromGame] / [onHostResumed]). */
  private var awaitingUnityReturn = false

  /** Partida que quedó en pausa dentro de Unity (sesión + id de lanzamiento), para retomarla en [launchGame]. */
  //
  // Se guarda en disco (SharedPreferences), no solo en memoria: al salir al escritorio Android puede destruir la
  // app (y este ViewModel) mientras el proceso de Unity, que es aparte, sigue vivo con la partida en pausa. Sin
  // esto, al volver y tocar Play se generaba un id de lanzamiento nuevo y Unity empezaba una partida desde cero.
  private val pausePrefs by lazy {
    getApplication<Application>().getSharedPreferences("paused_game", android.content.Context.MODE_PRIVATE)
  }

  /** Juego que quedó en pausa (para ofrecer "Seguir partida" en Hoy); null = ninguno. */
  private val _pausedGameId = MutableStateFlow<String?>(null)
  val pausedGameId: StateFlow<String?> = _pausedGameId.asStateFlow()

  private var pausedGame: Pair<ActiveGameSession, String>?
    get() {
      val gameId = pausePrefs.getString("gameId", null) ?: return null
      val launchId = pausePrefs.getString("launchId", null) ?: return null
      val def = GameRegistry.getById(gameId) ?: return null
      return ActiveGameSession(
        gameDef = def,
        level = pausePrefs.getInt("level", 1),
        timed = pausePrefs.getBoolean("timed", false),
        isDailyFlow = pausePrefs.getBoolean("daily", false),
        intensity = pausePrefs.getInt("intensity", 0)
      ) to launchId
    }
    set(value) {
      val e = pausePrefs.edit()
      if (value == null) {
        e.clear()
      } else {
        val (session, launchId) = value
        e.putString("gameId", session.gameDef.id)
          .putString("launchId", launchId)
          .putInt("level", session.level)
          .putBoolean("timed", session.timed)
          .putBoolean("daily", session.isDailyFlow)
          .putInt("intensity", session.intensity)
      }
      e.apply()
      _pausedGameId.value = value?.first?.gameDef?.id
    }

  init {
    _mission.value = missionStore.load()
    // Lo que quedó en disco si Android cerró la app durante un juego: la evaluación en curso y un resultado que
    // llegó mientras tanto (se muestra ahora, con su pantalla de resultado y el flujo donde iba).
    _pausedGameId.value = pausedGame?.first?.gameDef?.id
    GameSessionStore.loadBaseline()?.let { saved ->
      baselineAge = saved.ageBand?.let { n -> AgeBand.values().firstOrNull { it.name == n } }
      _baselineRun.value = BaselineRun(saved.done, saved.measured)
    }
    processPendingResult()
  }

  /** Retoma la partida en pausa (botón "Seguir partida" de Hoy). */
  fun resumePausedGame() {
    val (session, _) = pausedGame ?: return
    launchGame(session.gameDef.id, isDailyFlow = session.isDailyFlow)
  }

  /** `UnityGameHost` acaba de traer al frente la pantalla de juego (Unity). */
  fun onUnityLaunched(launchId: String) {
    awaitingUnityReturn = true
    _activeGame.value?.let {
      GameSessionStore.saveInFlight(
        GameSessionStore.InFlight(launchId, it.gameDef.id, it.level, it.timed, it.isDailyFlow, it.intensity, it.assessmentStep)
      )
    }
  }

  /**
   * Unity devolvió la app al frente ("Continuar" o Atrás) -- llega por `MainActivity.onNewIntent`, antes de
   * `onResume`. Si la partida terminó, [resultJson] trae su resultado y se muestra al instante (sin esperar al
   * broadcast de respaldo; [com.example.bridge.UnityResultInbox] evita procesarlo dos veces). Sin resultado =
   * salió a mitad de partida: se cierra la sesión.
   */
  fun onReturnedFromGame(launchId: String?, resultJson: String?, paused: Boolean = false) {
    awaitingUnityReturn = false
    if (paused) {
      // "Salir" del menú de pausa: la partida sigue viva (en pausa) en Unity; se vuelve al menú de la app.
      // En la evaluación inicial no se retoma: ese juego se vuelve a jugar desde el principio ("Seguir").
      val session = _activeGame.value ?: GameSessionStore.inFlight(launchId)?.let(::sessionFrom)
      GameSessionStore.clearInFlight()
      if (session != null && launchId != null && session.assessmentStep == 0) pausedGame = session to launchId
      _activeGame.value = null
      return
    }
    if (resultJson != null && com.example.bridge.UnityResultInbox.claim(launchId)) {
      val result = com.example.bridge.NativeReceiver.parse(resultJson)
      if (result != null) {
        onUnityResult(result, launchId)
        return
      }
    }
    // El resultado pudo llegar antes por el broadcast con la app cerrada: quedó pendiente en disco.
    if (processPendingResult()) return
    if (resultJson == null) GameSessionStore.clearInFlight() // salió sin terminar
    if (_lastResult.value == null) _activeGame.value = null
  }

  /**
   * La app volvió a primer plano. Si se esperaba la vuelta de Unity y no llegó (Unity se cerró o se cayó en
   * vez de devolver la app), se cierra la sesión para no dejar la pantalla de carga colgada.
   */
  fun onHostResumed() {
    if (!awaitingUnityReturn) return
    awaitingUnityReturn = false
    if (_lastResult.value == null) _activeGame.value = null
  }

  fun finishActiveGame(score: Int, correct: Int, total: Int) {
    val current = _activeGame.value ?: return
    val result = GamePlayResult(
      gameId = current.gameDef.id,
      score = score.coerceIn(0, 100),
      correctAnswers = correct,
      totalTrials = total,
      timed = current.timed,
      level = current.level
    )
    viewModelScope.launch {
      val outcome = repository.recordGameResult(result)
      _promotion.value = outcome.promotion(result.gameId)
      _achievementQueue.value = _achievementQueue.value + outcome.newAchievements
      _lastResultDaily.value = current.isDailyFlow
      _lastResult.value = Pair(result, outcome.didLevelUp)
      _activeGame.value = null
    }

    triggerHapticFeedback(if (score >= 70) HapticType.SUCCESS else HapticType.LIGHT)
  }

  fun continueDailyFlow() {
    _lastResult.value = null
    if (launchMissionIfDue()) return
    val session = dailySession.value
    if (session.completedCount < session.gameIds.size) {
      val nextId = session.gameIds[session.completedCount]
      launchGame(nextId, isDailyFlow = true)
    } else {
      // Session fully finished, go home
      setTab(AppTab.HOY)
    }
  }

  fun closeGameOrResult() {
    _activeGame.value = null
    _lastResult.value = null
  }

  fun createNewProfile(
    name: String,
    avatar: String = "🧠",
    difficultyMode: DifficultyMode = DifficultyMode.ADAPTIVE,
    weeklyGoal: Int = 4,
    cognitiveAssistance: Boolean = true
  ) {
    viewModelScope.launch {
      repository.createProfile(name, avatar, difficultyMode, weeklyGoal, cognitiveAssistance)
      triggerHapticFeedback(HapticType.SUCCESS)
    }
  }

  fun switchProfile(profileId: Long) {
    viewModelScope.launch {
      repository.switchActiveProfile(profileId)
      triggerHapticFeedback(HapticType.LIGHT)
    }
  }

  fun deleteProfile(profileId: Long) {
    viewModelScope.launch {
      repository.deleteProfile(profileId)
      triggerHapticFeedback(HapticType.MEDIUM)
    }
  }

  fun updateDifficultyPreferences(
    mode: DifficultyMode,
    memoria: Int,
    atencion: Int,
    razonamiento: Int,
    lenguaje: Int,
    calculo: Int,
    velocidad: Int,
    assistance: Boolean,
    timeScale: Float
  ) {
    viewModelScope.launch {
      val profileId = userSettings.value.id
      repository.updateDifficultyPreferences(
        profileId = profileId,
        mode = mode,
        memoria = memoria,
        atencion = atencion,
        razonamiento = razonamiento,
        lenguaje = lenguaje,
        calculo = calculo,
        velocidad = velocidad,
        assistance = assistance,
        timeScale = timeScale
      )
      triggerHapticFeedback(HapticType.SUCCESS)
    }
  }

  fun updateSettings(
    name: String,
    weeklyGoal: Int,
    defaultTimed: Boolean,
    sound: Boolean,
    haptics: Boolean,
    notificationsEnabled: Boolean = userSettings.value.notificationsEnabled,
    reminderHour: Int = userSettings.value.reminderHour,
    reminderMinute: Int = userSettings.value.reminderMinute,
    avatar: String = userSettings.value.avatar,
    difficultyMode: DifficultyMode = userSettings.value.difficultyMode,
    themeMode: ThemeMode = userSettings.value.themeMode,
    language: AppLanguage = userSettings.value.language
  ) {
    viewModelScope.launch {
      val current = userSettings.value
      val updated = current.copy(
        name = name,
        avatar = avatar,
        weeklyGoal = weeklyGoal,
        defaultTimed = defaultTimed,
        soundEnabled = sound,
        hapticsEnabled = haptics,
        notificationsEnabled = notificationsEnabled,
        reminderHour = reminderHour,
        reminderMinute = reminderMinute,
        difficultyMode = difficultyMode,
        themeMode = themeMode,
        language = language
      )
      repository.updateSettings(updated)

      if (notificationsEnabled) {
        CognitiveReminderWorker.scheduleDailyReminder(
          getApplication(),
          reminderHour,
          reminderMinute
        )
      } else {
        CognitiveReminderWorker.cancelReminder(getApplication())
      }
    }
  }

  /**
   * Onboarding de edad (piloto de perfiles por edad, 20-sep) -- función propia en vez de
   * sumarse a `updateSettings` porque es una acción de una sola vez que además decide si
   * se muestra la pantalla de onboarding (`userSettings.ageBand == null`), no una edición
   * más de Ajustes.
   */
  fun setAgeBand(band: AgeBand) {
    viewModelScope.launch {
      repository.updateSettings(userSettings.value.copy(ageBand = band))
    }
  }

  /**
   * Fin del onboarding (primera vez): guarda nombre (si lo escribió), meta de días por semana, recordatorio
   * ([reminderHour] null = sin recordatorios), rango de edad, nivel educacional y metas de una sola vez. Poner
   * `ageBand` es lo que saca al usuario del onboarding (ver `MainActivity`). Con [playBaseline] arranca la
   * evaluación "Tu punto de partida"; si no, se estima el punto de partida (ver data/Baseline.kt).
   */
  fun completeOnboarding(
    name: String,
    band: AgeBand,
    weeklyGoal: Int,
    reminderHour: Int?,
    education: com.example.data.Education?,
    goals: Set<DomainType>,
    playBaseline: Boolean
  ) {
    _currentTab.value = AppTab.HOY
    repository.saveProfileExtras(education, goals)
    viewModelScope.launch {
      val current = userSettings.value
      repository.updateSettings(
        current.copy(
          name = name.trim().ifEmpty { current.name },
          weeklyGoal = weeklyGoal,
          notificationsEnabled = reminderHour != null,
          reminderHour = reminderHour ?: current.reminderHour,
          reminderMinute = if (reminderHour != null) 0 else current.reminderMinute,
          ageBand = band
        )
      )
      if (playBaseline) {
        startBaseline(band)
      } else {
        // "Hacerlo después": punto de partida estimado; la evaluación queda disponible en Perfil.
        repository.applyPriorIfNeeded(band, education)
      }
    }
  }

  /** Solo depuración (botón en Ajustes): vuelve a mostrar el onboarding (sin borrar partidas ni progreso). */
  fun debugRestartOnboarding() {
    viewModelScope.launch { repository.updateSettings(userSettings.value.copy(ageBand = null)) }
  }

  /**
   * "Jugar ahora" desde el recordatorio. Espera un momento a que carguen los ajustes (al abrir en frío todavía
   * valen los de fábrica) y solo arranca si ya pasó el onboarding y no hay otro juego en curso.
   */
  fun startDailySessionFromReminder() {
    viewModelScope.launch {
      kotlinx.coroutines.delay(700)
      if (userSettings.value.ageBand != null && _activeGame.value == null && _lastResult.value == null) startDailySession()
    }
  }

  fun triggerTestNotification() {
    CognitiveReminderWorker.triggerImmediateTestReminder(getApplication())
  }

  fun resetData() {
    viewModelScope.launch {
      repository.resetData()
      _activeGame.value = null
      _lastResult.value = null
      _promotion.value = null
      _achievementQueue.value = emptyList()
      setBaselineRun(null)
      pausedGame = null
      GameSessionStore.clearAll()
      missionStore.clear()
      contactStore.clear()
      _mission.value = com.example.data.MissionState()
      _currentTab.value = AppTab.HOY
    }
  }

  enum class HapticType { LIGHT, MEDIUM, SUCCESS, ERROR }

  fun triggerHapticFeedback(type: HapticType = HapticType.LIGHT) {
    if (!userSettings.value.hapticsEnabled) return
    try {
      val vibrator = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
        val vibratorManager = getApplication<Application>().getSystemService(Context.VIBRATOR_MANAGER_SERVICE) as? VibratorManager
        vibratorManager?.defaultVibrator
      } else {
        @Suppress("DEPRECATION")
        getApplication<Application>().getSystemService(Context.VIBRATOR_SERVICE) as? Vibrator
      }

      vibrator?.let { v ->
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
          val effect = when (type) {
            HapticType.LIGHT -> VibrationEffect.createPredefined(VibrationEffect.EFFECT_TICK)
            HapticType.MEDIUM -> VibrationEffect.createPredefined(VibrationEffect.EFFECT_CLICK)
            HapticType.SUCCESS -> VibrationEffect.createOneShot(100, VibrationEffect.DEFAULT_AMPLITUDE)
            HapticType.ERROR -> VibrationEffect.createWaveform(longArrayOf(0, 50, 50, 50), -1)
          }
          v.vibrate(effect)
        } else {
          @Suppress("DEPRECATION")
          v.vibrate(40)
        }
      }
    } catch (_: Exception) {}
  }
}
