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

/** Las 3 pestañas (29-sep): Hoy, Juegos y Avance (la liga vive dentro de Avance). */
enum class AppTab(val title: String) {
  HOY("Hoy"),
  JUEGOS("Juegos"),
  PROGRESO("Avance")
}

/** Lo que se abre con los botones de arriba a la derecha: tu perfil o las opciones (Ajustes). */
enum class TopPanel { PERFIL, AJUSTES }

data class ActiveGameSession(
  val gameDef: GameDefinition,
  val level: Int,
  val timed: Boolean,
  val isDailyFlow: Boolean = false,
  // Progresión sin techo más allá de nivel 5 (Experto) — ver GameProgressEntity.masteryStreak.
  val intensity: Int = 0,
  // Cómo eligió jugar la persona (data/Skill.kt). La sesión diaria y "Jugar" van siempre a tu medida.
  val mode: com.example.data.PlayMode = com.example.data.PlayMode.A_TU_MEDIDA,
  // Partida en pausa que se retoma: se relanza Unity con este id de lanzamiento y la partida sigue donde quedó.
  val resumeLaunchId: String? = null,
  // Evaluación inicial "Tu punto de partida": paso 1..N (0 = partida normal). Ver data/Baseline.kt.
  val assessmentStep: Int = 0,
  // Abre el juego con su tutorial guiado aunque la persona ya lo haya jugado (el inicio completo, `data/FirstFlight.kt`: el tutorial es parte del recorrido).
  val tutorial: Boolean = false,
  // Identidad de esta sesión (no de la partida): la usa la UI para que el estado guardado de una sesión anterior
  // (p. ej. "ya lancé Unity", que Android restaura al recrear la pantalla) no lo herede la siguiente.
  val sessionToken: String = java.util.UUID.randomUUID().toString()
)

class NeuroVidaViewModel(application: Application) : AndroidViewModel(application) {
  // El mismo repositorio que usa el puente con Unity (una sola copia en memoria de ligas, logros y punto de partida).
  private val repository = (application as? com.example.NeuroVidaApplication)?.repository ?: NeuroVidaRepository(application)

  val userSettings = repository.userSettings
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

  /** Punto de partida: educación (opcional, se edita en Perfil), metas, qué ver primero al terminar un juego, visión de color y el mapa guardado (null = no hizo la evaluación). */
  val education = repository.education
  val goals = repository.goals
  val baseline = repository.baseline
  val resultFocus = repository.resultFocus
  val colorVision = repository.colorVision

  fun setEducation(education: com.example.data.Education?) = repository.saveEducation(education)
  fun setResultFocus(focus: com.example.data.ResultFocus) = repository.saveResultFocus(focus)
  fun setColorVision(vision: com.example.data.ColorVision) = repository.saveColorVision(vision)

  // ---------- «Primer vuelo con Nubi»: el inicio nuevo (docs/diseno-inicio.md; lógica pura en data/FirstFlight.kt) ----------

  /**
   * El recorrido en curso (null = ninguno). Se lee de disco al crear el ViewModel: cada paso se guarda al completarse
   * ([FlightStore]), así que si Android cierra la app (también en medio de un juego) vuelve en el paso siguiente.
   * La PRIMERA vez (sin nada guardado todavía y `ageBand == null`) la pantalla muestra el principio del recorrido completo
   * y esto sigue en null hasta la primera respuesta.
   */
  private val _flight = MutableStateFlow<com.example.data.FlightState?>(com.example.data.FlightStore.load(application))
  val flight: StateFlow<com.example.data.FlightState?> = _flight.asStateFlow()

  /** Logros conseguidos durante los juegos del inicio: se celebran al terminar el recorrido, no entre juego y juego. */
  private var flightAchievements: List<String> = emptyList()

  /** El punto de partida se está guardando (evita que un doble toque en «Continuar» lo aplique dos veces). */
  private var flightApplying = false

  private fun setFlight(state: com.example.data.FlightState?) {
    _flight.value = state
    com.example.data.FlightStore.save(getApplication(), state)
  }

  private fun currentFlight(): com.example.data.FlightState = _flight.value ?: com.example.data.FlightState(com.example.data.FlightMode.FULL)

  /** Cambia una respuesta (o lo que sea) del recorrido y lo guarda en disco. */
  private fun updateFlight(block: (com.example.data.FlightState) -> com.example.data.FlightState) = setFlight(block(currentFlight()))

  /** «Hacer la evaluación» (Avance, Hoy): solo la parte de juegos del recorrido, sin preguntas. */
  fun startBaseline() {
    if (_flight.value?.mode == com.example.data.FlightMode.FULL) return // el inicio completo ya incluye los 4 juegos
    flightAchievements = emptyList()
    _lastResult.value = null
    setFlight(com.example.data.FlightState(com.example.data.FlightMode.GAMES))
  }

  /** Solo depuración (Ajustes): repite el inicio completo SIN borrar partidas ni progreso. */
  fun debugRestartOnboarding() {
    flightAchievements = emptyList()
    _lastResult.value = null
    setFlight(com.example.data.FlightState(com.example.data.FlightMode.FULL))
  }

  fun flightSetName(name: String) = updateFlight { it.copy(name = name.trim().take(24)) }

  /** El rango de edad se guarda enseguida en el perfil (los juegos, el avance y el lanzador lo leen de ahí); lo demás, al final. */
  fun flightSetAge(band: AgeBand) {
    updateFlight { it.copy(age = band) }
    viewModelScope.launch { repository.updateSettings(userSettings.value.copy(ageBand = band)) }
  }

  fun flightToggleGoal(domain: DomainType) {
    val s = currentFlight()
    val goals = if (domain in s.goals) s.goals - domain else if (s.goals.size < 3) s.goals + domain else s.goals
    setFlight(s.copy(goals = goals))
    repository.saveGoals(goals)
  }

  fun flightSetFocus(focus: com.example.data.ResultFocus) {
    updateFlight { it.copy(focus = focus) }
    repository.saveResultFocus(focus)
  }

  fun flightSetColorVision(vision: com.example.data.ColorVision) {
    updateFlight { it.copy(color = vision) }
    repository.saveColorVision(vision)
  }

  fun flightSetDays(days: Int) = updateFlight { it.copy(days = days) }

  fun flightSetHour(hour: Int) = updateFlight { it.copy(hour = hour) }

  /** «Continuar»: al paso que sigue. En «Tu punto de partida» antes se guarda el punto de partida; al pasar el último paso termina el recorrido. */
  fun flightContinue() {
    val s = currentFlight()
    if (s.step == com.example.data.FlightStep.PUNTO) {
      applyFlightResults(s)
      return
    }
    val next = com.example.data.FirstFlight.next(s)
    if (next == null) finishFlight() else setFlight(next)
  }

  /** «Atrás»: un paso (nunca a la mitad de un juego). */
  fun flightBack() {
    com.example.data.FirstFlight.back(currentFlight())?.let(::setFlight)
  }

  /**
   * El aviso diario ya se resolvió ([hour] = la hora, o [com.example.data.FirstFlight.NO_REMINDER]; la pantalla pidió antes el permiso de
   * notificaciones de Android 13+ y pasa NO_REMINDER si lo negaron). Se guarda en Ajustes y sigue «Tu camino de hoy».
   */
  fun flightConfirmReminder(hour: Int) {
    val s = currentFlight()
    val on = hour != com.example.data.FirstFlight.NO_REMINDER
    viewModelScope.launch {
      val current = userSettings.value
      repository.updateSettings(
        current.copy(
          notificationsEnabled = on,
          reminderHour = if (on) hour else current.reminderHour,
          reminderMinute = if (on) 0 else current.reminderMinute
        )
      )
      val answered = s.copy(hour = hour)
      setFlight(com.example.data.FirstFlight.next(answered) ?: answered)
    }
  }

  /**
   * Juega el juego [index] (0..3) de [com.example.data.BaselinePlan.steps]. En el recorrido completo SIEMPRE con el tutorial guiado (es el
   * tutorial del juego); en la evaluación repetida, solo si nunca se jugó. La versión corta se pide con `assessmentStep`.
   */
  fun flightPlay(index: Int) {
    val s = currentFlight()
    val step = com.example.data.BaselinePlan.steps.getOrNull(index) ?: return
    val def = GameRegistry.getById(step.gameId) ?: return
    pausedGame = null
    _activeGame.value = ActiveGameSession(
      gameDef = def,
      level = com.example.data.BaselinePlan.startLevel(s.age ?: userSettings.value.ageBand),
      timed = step.timed,
      assessmentStep = index + 1,
      tutorial = s.mode == com.example.data.FlightMode.FULL
    )
    _lastResult.value = null
  }

  /** «Terminar después» en un juego: lo jugado queda guardado y el resto se estima. En la evaluación repetida se cierra todo. */
  fun flightSkipGames() {
    val s = currentFlight()
    if (s.mode == com.example.data.FlightMode.GAMES) {
      viewModelScope.launch { repository.applyPriorIfNeeded(userSettings.value.ageBand, education.value) }
      finishFlight()
      return
    }
    val skipped = com.example.data.FirstFlight.skipRemainingGames(s)
    setFlight(com.example.data.FirstFlight.next(skipped) ?: skipped)
  }

  /**
   * «Tu punto de partida»: se guardan lo que faltaba del perfil (nombre y días por semana), el mapa y los ratings que siembra, y se
   * vuelve a armar el camino de hoy con las metas y el mapa nuevos. Con los 4 juegos medidos se guarda el mapa; con menos (se dejó
   * alguno para después) queda el punto de partida estimado y la evaluación se ofrece de nuevo en Avance.
   */
  private fun applyFlightResults(s: com.example.data.FlightState) {
    if (flightApplying) return
    flightApplying = true
    viewModelScope.launch {
      try {
        if (!s.applied) {
          val age = s.age ?: userSettings.value.ageBand
          if (s.mode == com.example.data.FlightMode.FULL) {
            val current = userSettings.value
            repository.updateSettings(
              current.copy(
                name = s.name.ifEmpty { current.name },
                weeklyGoal = s.days ?: com.example.data.FirstFlight.DEFAULT_DAYS,
                ageBand = age ?: AgeBand.ADULT
              )
            )
          }
          if (s.allMeasured) repository.applyBaseline(com.example.data.buildBaseline(s.measured))
          else repository.applyPriorIfNeeded(age, education.value)
          repository.refreshTodaySession()
        }
        val done = s.copy(applied = true)
        if (done.mode == com.example.data.FlightMode.GAMES) {
          finishFlight()
        } else {
          setFlight(com.example.data.FirstFlight.next(done) ?: done)
        }
      } finally {
        flightApplying = false
      }
    }
  }

  /** Fin del recorrido («Empezar mi camino» o «Listo»): vuelve a Hoy y celebra los logros que quedaron pendientes. */
  fun finishFlight() {
    // El día que se completa el Primer vuelo (el recorrido entero, no «repetir la evaluación»), ese día queda cumplido: el camino de hoy se da por hecho con el vuelo (ver repository.completeTodayWithFlight).
    val flight = _flight.value
    val completesTheDay = flight != null && flight.mode == com.example.data.FlightMode.FULL && flight.applied
    if (completesTheDay) viewModelScope.launch { repository.completeTodayWithFlight() }
    setFlight(null)
    // También se cierra el panel de arriba (Ajustes / Perfil): si el inicio se abrió desde ahí (Debug «Repetir el inicio»), si no seguiría tapando Hoy.
    _topPanel.value = null
    setTab(AppTab.HOY)
    if (flightAchievements.isNotEmpty()) _achievementQueue.value = _achievementQueue.value + flightAchievements
    flightAchievements = emptyList()
  }

  /** Llegó el resultado de un juego del inicio: se guarda como cualquier partida (sin avanzar el camino de hoy), se anota su medida y su primer dato, y se pasa a su tarjeta. */
  private fun onFlightResult(session: ActiveGameSession, result: GamePlayResult) {
    val rating = result.endRating ?: (result.score / 100f)
    _activeGame.value = null
    viewModelScope.launch {
      val outcome = repository.recordGameResult(result, countsForDailySession = false)
      flightAchievements = flightAchievements + outcome.newAchievements
      val s = _flight.value ?: return@launch // sin recorrido (se perdió o fue un botón de prueba): la partida solo se guarda
      setFlight(com.example.data.FirstFlight.withResult(s, session.gameDef.id, rating, com.example.data.FirstData.phrase(result)))
      triggerHapticFeedback(HapticType.LIGHT)
    }
  }

  /** Ascensos de liga guardados (para marcarlos en el camino de Hoy). */
  val leagueEvents = repository.leagueEvents

  /** Medidas propias de los juegos estrella por partida (descubrimiento del día y zonas del planeta en Hoy). */
  val starMeasures = repository.starMeasures
  val atlas = repository.atlas
  val rescateTotals = repository.rescateTotals
  val acoplamientoTotals = repository.acoplamientoTotals
  val aterrizajeTotals = repository.aterrizajeTotals

  /** Las frases de ¿Verdad o disparate? marcadas como poco claras, para el informe de errores de Ajustes. */
  fun unclearReport(): String = repository.unclearReport()

  /** Logros conseguidos (id -> cuándo) y las cifras con que se calculan (para el avance "4/7" de los bloqueados). */
  val achievementUnlocks = repository.achievementUnlocks
  val achievementStats: StateFlow<com.example.data.AchievementStats> = combine(gameHistory, gameRanks) { hist, ranks ->
    com.example.data.computeAchievementStats(hist, ranks.associate { it.gameId to it.rating })
  }.stateIn(viewModelScope, SharingStarted.Eagerly, com.example.data.computeAchievementStats(emptyList(), emptyMap()))

  /** Nivel (0..1) por juego para Progreso: rating del DDA comun; en Secuencia/Parejas (motores propios) se
   *  aproxima con el nivel 1-5 si ya se jugaron; null = sin medir. */
  /** Lo recordado del avance (juegos medidos, Experto abierto, fechas de etapas): ver data/Skill.kt. */
  val skill = repository.skill

  /** Historia del avance de cada juego (ver data/AreaProgress.kt): el cambio de la semana de cada área en Hoy. */
  val progressLog = repository.progressLog

  /**
   * Tu avance (0..1, leído a 8 de 10: data/Skill.kt) de cada juego YA MEDIDO. Los que no están acá dicen
   * "Sin medir aún" aunque tengan un punto de partida estimado. Parejas (motor propio, sin rating) usa su nivel 1-5.
   */
  val gameProgress: StateFlow<Map<String, Float>> = combine(repository.gameDdaRating, gameLevels, repository.skill, userSettings) { dda, levels, sk, settings ->
    sk.measured.mapNotNull { id ->
      val r = dda[id]?.takeIf { it >= 0f }
      when {
        r != null -> id to com.example.data.Skill.progress(id, r, settings.ageBand)
        else -> levels[id]?.let { id to (((it - 1) / 5f) + 0.1f) }
      }
    }.toMap()
  }.stateIn(viewModelScope, SharingStarted.Eagerly, emptyMap())


  /** Avance de cada juego para Hoy, Liga y Perfil: el mismo de las cartas ([gameProgress]); null = sin medir aún. */
  val gameLevelsForProgress: StateFlow<Map<String, Float?>> = gameProgress.map { p ->
    GameRegistry.allGames.associate { g -> g.id to p[g.id] }
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
    viewModelScope.launch { repository.seedSkillIfNeeded() }
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

  private val _topPanel = MutableStateFlow<TopPanel?>(null)
  val topPanel: StateFlow<TopPanel?> = _topPanel.asStateFlow()

  fun openTopPanel(panel: TopPanel?) { _topPanel.value = panel }

  /**
   * Cambia de pestaña (barra de abajo, deslizar, «Ir a Juegos» desde Hoy). Juegos siempre se muestra con las 4 áreas:
   * al entrar, al tocarla estando en ella y al salir se cierra la ventana del área. La única forma de volver a un área
   * abierta es cerrar el resultado de un juego lanzado desde Juegos ([closeGameOrResult], que no pasa por aquí).
   */
  fun setTab(tab: AppTab) {
    if (tab == AppTab.JUEGOS || _currentTab.value == AppTab.JUEGOS) {
      _libraryFocus.value = _libraryFocus.value.copy(open = false)
      focusPrefs.edit().putBoolean("return", false).apply()
    }
    _currentTab.value = tab
    _activeGame.value = null
    _lastResult.value = null
  }

  fun startDailySession() {
    val session = dailySession.value
    val nextGameId = if (session.completedCount < session.gameIds.size) {
      session.gameIds[session.completedCount]
    } else {
      session.gameIds.firstOrNull() ?: "calculo"
    }
    launchGame(nextGameId, isDailyFlow = true)
  }

  /**
   * Nivel 1-5 con que se lanza un juego (solo pesa en los que todavía no tienen rating guardado: Unity continúa
   * desde el rating). El modo de dificultad de Ajustes se quitó el 28-sep: ahora se elige el modo por juego.
   */
  fun getEffectiveLevelForGame(gameId: String): Int = gameLevels.value[gameId] ?: 1

  fun getEffectiveIntensityForGame(gameId: String): Int = gameIntensity.value[gameId] ?: 0

  fun launchGame(
    gameId: String,
    customLevel: Int? = null,
    customTimed: Boolean? = null,
    isDailyFlow: Boolean = false,
    mode: com.example.data.PlayMode = com.example.data.PlayMode.A_TU_MEDIDA
  ) {
    // Si ese juego quedó en pausa ("Salir" del menú de pausa), se retoma en vez de empezar de cero. "Jugar de
    // nuevo" (customLevel) siempre es una partida nueva. Abrir otro juego descarta la pausa (Unity recarga).
    // un juego de la sesión de Hoy nunca vuelve a Juegos, aunque quedara «volver» de una partida anterior lanzada desde Juegos
    if (isDailyFlow) focusPrefs.edit().putBoolean("return", false).apply()
    val paused = pausedGame
    pausedGame = null
    if (paused != null && paused.first.gameDef.id == gameId && customLevel == null && paused.first.mode == mode && (customTimed == null || paused.first.timed == customTimed)) {
      _activeGame.value = paused.first.copy(
        isDailyFlow = isDailyFlow || paused.first.isDailyFlow,
        resumeLaunchId = paused.second
      )
      _lastResult.value = null
      return
    }
    val def = GameRegistry.getById(gameId) ?: return
    val lvl = customLevel ?: getEffectiveLevelForGame(gameId)
    // El camino diario de Hoy va siempre sin reloj; en Juegos vale la elección de la ficha de cada juego (data/RetoChoice.kt).
    val timed = if (isDailyFlow) false else customTimed ?: repository.retoFor(gameId)
    val intensity = if (lvl >= 5) getEffectiveIntensityForGame(gameId) else 0
    _activeGame.value = ActiveGameSession(def, lvl, timed, isDailyFlow, intensity, mode = if (isDailyFlow) com.example.data.PlayMode.A_TU_MEDIDA else mode)
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
      onFlightResult(current, rawResult)
      return
    }
    val withMode = if (current != null) rawResult.copy(playMode = current.mode) else rawResult
    val result = withMode
    viewModelScope.launch {
      val outcome = repository.recordGameResult(result)
      if (current != null) {
        _modeNote.value = modeNote(result.playMode, outcome)
        _promotion.value = outcome.promotion(result.gameId)
        _achievementQueue.value = _achievementQueue.value + outcome.newAchievements
        _lastResultDaily.value = current.isDailyFlow
        _lastResult.value = Pair(result, outcome.didLevelUp)
        _activeGame.value = null
        triggerHapticFeedback(if (result.score >= 70) HapticType.SUCCESS else HapticType.LIGHT)
      }
    }
  }

  // Qué pasó con el modo elegido en la última partida (se muestra en el resultado).
  private val _modeNote = MutableStateFlow<String?>(null)
  val modeNote: StateFlow<String?> = _modeNote.asStateFlow()

  private fun modeNote(mode: com.example.data.PlayMode, outcome: com.example.model.RecordOutcome): String? = when {
    mode == com.example.data.PlayMode.A_TU_MEDIDA -> null
    outcome.expertUnlocked -> "¡Desafío superado! Tu avance subió y se abrió Experto"
    outcome.modePassed -> "¡${mode.label} superado! Tu avance subió"
    mode == com.example.data.PlayMode.SUAVE -> "Partida suave: tu avance se mantiene"
    else -> "${mode.label}: esta vez tu avance se mantiene"
  }

  private fun sessionFrom(p: GameSessionStore.InFlight): ActiveGameSession? {
    val def = GameRegistry.getById(p.gameId) ?: return null
    return ActiveGameSession(def, p.level, p.timed, p.daily, p.intensity, assessmentStep = p.assessmentStep, mode = p.mode)
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
        intensity = pausePrefs.getInt("intensity", 0),
        mode = runCatching { com.example.data.PlayMode.valueOf(pausePrefs.getString("mode", null) ?: "") }
          .getOrDefault(com.example.data.PlayMode.A_TU_MEDIDA)
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
          .putString("mode", session.mode.name)
      }
      e.apply()
      _pausedGameId.value = value?.first?.gameDef?.id
    }

  init {
    // Lo que quedó en disco si Android cerró la app durante un juego: el recorrido del inicio en curso (ya cargado en
    // [_flight]) y un resultado que llegó mientras tanto (se muestra ahora, con su pantalla de resultado y el flujo donde iba).
    _pausedGameId.value = pausedGame?.first?.gameDef?.id
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
        GameSessionStore.InFlight(launchId, it.gameDef.id, it.level, it.timed, it.isDailyFlow, it.intensity, it.assessmentStep, it.mode)
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

  /**
   * "Continuar" del resultado en la sesión diaria: el siguiente de los 3 juegos o, al terminar, el resumen de la
   * sesión ([sessionSummary]).
   */
  fun continueDailyFlow() {
    _lastResult.value = null
    val session = dailySession.value
    if (session.completedCount < session.gameIds.size) {
      val nextId = session.gameIds[session.completedCount]
      launchGame(nextId, isDailyFlow = true)
    } else {
      setTab(AppTab.HOY)
      _sessionSummary.value = buildSessionSummary()
    }
  }

  private val _sessionSummary = MutableStateFlow<com.example.data.SessionSummary?>(null)
  /** El resumen de la sesión de hoy recién terminada (se muestra una vez, sobre Hoy). */
  val sessionSummary: StateFlow<com.example.data.SessionSummary?> = _sessionSummary.asStateFlow()

  fun dismissSessionSummary() { _sessionSummary.value = null }

  private fun buildSessionSummary(): com.example.data.SessionSummary =
    com.example.data.SessionSummary.build(
      gameIds = dailySession.value.gameIds,
      history = gameHistory.value,
      progress = gameProgress.value,
      points = repository.progressLog.value,
      now = System.currentTimeMillis()
    )

  fun closeGameOrResult() {
    // Partida lanzada desde Juegos: al cerrar el resultado se vuelve a Juegos, al área y casilla de ese juego
    // (también si Android cerró la app durante el juego y el ViewModel es nuevo).
    val gameId = _lastResult.value?.first?.gameId ?: _activeGame.value?.gameDef?.id
    if (gameId != null && focusPrefs.getBoolean("return", false) && _libraryFocus.value.gameId == gameId) {
      _currentTab.value = AppTab.JUEGOS
    }
    focusPrefs.edit().putBoolean("return", false).apply()
    _activeGame.value = null
    _lastResult.value = null
  }

  // Dónde quedó la pestaña Juegos (área y casilla), en disco: al volver de un juego se abre en el mismo lugar.
  // [open] = la ventana del área está abierta (desde el 29-sep Juegos muestra las 4 áreas y cada una abre su ventana);
  // al arrancar la app solo sigue abierta si se vuelve de un juego lanzado desde ella.
  data class LibraryFocus(val domain: DomainType? = null, val gameId: String? = null, val open: Boolean = false)

  private val focusPrefs by lazy { getApplication<Application>().getSharedPreferences("library_focus", android.content.Context.MODE_PRIVATE) }
  private val _libraryFocus = MutableStateFlow(
    LibraryFocus(
      focusPrefs.getString("domain", null)?.let { n -> DomainType.fromStored(n) },
      focusPrefs.getString("gameId", null),
      open = focusPrefs.getBoolean("return", false)
    )
  )
  val libraryFocus: StateFlow<LibraryFocus> = _libraryFocus.asStateFlow()

  /** Abre (o deja abierta) la ventana de [domain] en Juegos, recordando la casilla [gameId]. */
  fun setLibraryFocus(domain: DomainType, gameId: String?) {
    _libraryFocus.value = LibraryFocus(domain, gameId, open = true)
    focusPrefs.edit().putString("domain", domain.name).putString("gameId", gameId).apply()
  }

  /** La X (o Atrás) de la ventana de un área: vuelve a las 4 áreas. */
  fun closeLibraryArea() {
    _libraryFocus.value = _libraryFocus.value.copy(open = false)
  }

  /** "Jugar" en la ficha de Juegos: recuerda la casilla para volver a ella y lanza en el modo elegido. */
  fun playFromLibrary(gameId: String, mode: com.example.data.PlayMode, timed: Boolean? = null) {
    GameRegistry.getById(gameId)?.let { setLibraryFocus(it.domain, gameId) }
    focusPrefs.edit().putBoolean("return", true).apply()
    if (timed != null) repository.setReto(gameId, timed)       // se recuerda por juego
    launchGame(gameId, mode = mode, customTimed = timed?.let { it && com.example.data.RetoChoice.supports(gameId) })
  }

  /** La elección «Sin reloj / Contra el reloj» que vale hoy para [gameId] (la ficha de Juegos la muestra elegida). */
  fun retoFor(gameId: String): Boolean = repository.retoFor(gameId)

  val retoChoices: StateFlow<Map<String, Boolean>> get() = repository.retoChoices

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
   * "Jugar ahora" desde el recordatorio. Espera un momento a que carguen los ajustes (al abrir en frío todavía
   * valen los de fábrica) y solo arranca si ya pasó el onboarding y no hay otro juego en curso.
   */
  fun startDailySessionFromReminder() {
    viewModelScope.launch {
      kotlinx.coroutines.delay(700)
      // Con el inicio nuevo en curso no se salta a la sesión de hoy: primero termina su recorrido.
      if (userSettings.value.ageBand != null && _flight.value == null && _activeGame.value == null && _lastResult.value == null) startDailySession()
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
      flightAchievements = emptyList()
      _topPanel.value = null
      setFlight(null)
      pausedGame = null
      GameSessionStore.clearAll()
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
    } catch (e: Exception) {
      // La vibración es un extra: si falla, la partida sigue igual, pero queda anotado.
      com.example.diag.ErrorLog.record("VIBRACION", "No se pudo vibrar.", e)
    }
  }
}
