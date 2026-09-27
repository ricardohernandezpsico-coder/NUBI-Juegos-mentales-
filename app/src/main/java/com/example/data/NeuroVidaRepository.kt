package com.example.data

import android.content.Context
import com.example.data.local.*
import com.example.model.*
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.text.SimpleDateFormat
import java.util.*

/** Juegos con motor de dificultad propio (no usan el rating del DDA común guardado en `ddaRating`). */
private val OWN_ENGINE_GAMES = setOf("secuencia", "parejas")

class NeuroVidaRepository(
  context: Context,
  private val database: NeuroVidaDatabase = NeuroVidaDatabase.getDatabase(context)
) {
  private val repositoryScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
  private val dateFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())

  private val gameResultDao = database.gameResultDao()
  private val gameProgressDao = database.gameProgressDao()
  private val dailySessionDao = database.dailySessionDao()
  private val userProfileDao = database.userProfileDao()
  private val domainMasteryDao = database.domainMasteryDao()
  private val claimedWeeklyChallengeDao = database.claimedWeeklyChallengeDao()

  private fun getTodayDateKey(): String = synchronized(dateFormat) {
    dateFormat.format(Date())
  }

  private fun formatDate(timestamp: Long): String = synchronized(dateFormat) {
    dateFormat.format(Date(timestamp))
  }

  // 1. Reactive User Profiles & Settings Flows from Room
  val allProfiles: StateFlow<List<UserSettings>> = userProfileDao.getAllProfiles()
    .map { list -> list.map { it.toDomain() } }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = emptyList()
    )

  val userSettings: StateFlow<UserSettings> = userProfileDao.getActiveProfile()
    .map { entity -> entity?.toDomain() ?: userProfileDao.getUserProfileSync()?.toDomain() ?: UserSettings() }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = UserSettings()
    )

  // 2. Reactive Game History Flow from Room
  val gameHistory: StateFlow<List<GamePlayResult>> = gameResultDao.getAllResults()
    .map { list -> list.map { it.toDomain() } }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = emptyList()
    )

  // 3. Reactive Game Levels Map from Room
  val gameLevels: StateFlow<Map<String, Int>> = gameProgressDao.getAllProgress()
    .map { list ->
      val map = mutableMapOf<String, Int>()
      GameRegistry.allGames.forEach { g -> map[g.id] = 1 }
      list.forEach { p -> map[p.gameId] = p.currentLevel }
      map
    }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = GameRegistry.allGames.associate { it.id to 1 }
    )

  // 3b. Reactive Mastery Streak Map from Room — progresión sin techo más allá de nivel 5
  // (ver comentario en GameProgressEntity.masteryStreak).
  val gameIntensity: StateFlow<Map<String, Int>> = gameProgressDao.getAllProgress()
    .map { list ->
      val map = mutableMapOf<String, Int>()
      GameRegistry.allGames.forEach { g -> map[g.id] = 0 }
      list.forEach { p -> map[p.gameId] = p.masteryStreak }
      map
    }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = GameRegistry.allGames.associate { it.id to 0 }
    )

  // 3b'. Rating del DDA común por juego (0..1; -1 = sin dato), para arrancar los juegos Unity donde
  // quedó el usuario (ver docs/DDA-comun.md).
  val gameDdaRating: StateFlow<Map<String, Float>> = gameProgressDao.getAllProgress()
    .map { list ->
      val map = mutableMapOf<String, Float>()
      GameRegistry.allGames.forEach { g -> map[g.id] = -1f }
      list.forEach { p -> map[p.gameId] = p.ddaRating }
      map
    }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = GameRegistry.allGames.associate { it.id to -1f }
    )

  // 3c. Reactive ELO Rank Map from Room — ranking competitivo por juego (ver
  // RankTier/GameRankInfo en Models.kt), independiente de nivel/masteryStreak.
  val gameRanks: StateFlow<Map<String, Int>> = gameProgressDao.getAllProgress()
    .map { list ->
      val map = mutableMapOf<String, Int>()
      GameRegistry.allGames.forEach { g -> map[g.id] = 0 }
      list.forEach { p -> map[p.gameId] = p.eloRating }
      map
    }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = GameRegistry.allGames.associate { it.id to 0 }
    )

  // 4b. Reactive Domain Mastery XP Map from Room — meta-progresión sin techo,
  // ver DomainMasteryInfo/MasteryTier en Models.kt.
  val domainMastery: StateFlow<Map<DomainType, Int>> = domainMasteryDao.getAll()
    .map { list ->
      val map = DomainType.values().associateWith { 0 }.toMutableMap()
      list.forEach { e -> runCatching { DomainType.valueOf(e.domain) }.getOrNull()?.let { map[it] = e.xp } }
      map
    }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = DomainType.values().associateWith { 0 }
    )

  // 4c. Desafíos semanales: progreso derivado EN VIVO del historial de esta
  // semana (no se persiste un contador aparte) + qué se reclamó ya (para no
  // volver a otorgar el premio si se recalcula).
  private val _claimedChallenges = MutableStateFlow<Set<String>>(emptySet())

  // Ascensos de liga (liga general y de cada juego), para marcarlos en el camino de Hoy. Van en
  // SharedPreferences y no en Room: son pocos, solo se agregan, y así no hace falta una migración.
  private val leaguePrefs = context.getSharedPreferences("league_events", Context.MODE_PRIVATE)
  private val _leagueEvents = MutableStateFlow(decodeLeagueEvents(leaguePrefs.getString("events", null)))
  val leagueEvents: StateFlow<List<LeagueEvent>> = _leagueEvents.asStateFlow()

  // Logros conseguidos (id -> cuándo). Se derivan del historial y los trofeos (ver Achievements.kt); acá solo
  // se recuerda cuáles ya se celebraron.
  private val achievementPrefs = context.getSharedPreferences("achievements", Context.MODE_PRIVATE)
  private val _achievementUnlocks = MutableStateFlow(decodeUnlocks(achievementPrefs.getString("unlocks", null)))
  val achievementUnlocks: StateFlow<Map<String, Long>> = _achievementUnlocks.asStateFlow()

  // "Tu punto de partida" (ver Baseline.kt): nivel educacional, metas y el mapa de la evaluación inicial. En
  // SharedPreferences (como ligas y logros) para no migrar Room por datos que solo se escriben en el onboarding.
  private val profilePrefs = context.getSharedPreferences("profile_extra", Context.MODE_PRIVATE)
  private val _education = MutableStateFlow(
    profilePrefs.getString("education", null)?.let { n -> Education.values().firstOrNull { it.name == n } }
  )
  val education: StateFlow<Education?> = _education.asStateFlow()
  private val _goals = MutableStateFlow(decodeGoals(profilePrefs.getString("goals", null)))
  val goals: StateFlow<Set<DomainType>> = _goals.asStateFlow()
  private val _baseline = MutableStateFlow(decodeBaseline(profilePrefs.getString("baseline", null)))
  val baseline: StateFlow<Baseline?> = _baseline.asStateFlow()

  /** Datos del onboarding para el punto de partida (educación null = no respondió; metas vacías = sin preferencia). */
  fun saveProfileExtras(education: Education?, goals: Set<DomainType>) {
    _education.value = education
    _goals.value = goals
    profilePrefs.edit()
      .putString("education", education?.name)
      .putString("goals", encodeGoals(goals))
      .apply()
  }

  /**
   * Guarda el mapa de la evaluación y siembra el punto de partida de los 9 juegos (cada uno toma el rating de su
   * dominio). La primera evaluación reemplaza el nivel y el rating de todos los juegos que no se jugaron después
   * del punto de partida estimado (si se había saltado); si se repite más adelante, solo completa los juegos del DDA
   * común que todavía no tienen rating (no pisa lo ya jugado). Los juegos medidos ya guardaron su rating al jugarse.
   */
  suspend fun applyBaseline(baseline: Baseline) = withContext(Dispatchers.IO) {
    val firstEvaluation = _baseline.value == null
    val priorAt = profilePrefs.getLong("prior_at", 0L)
    seedStartingPoint(seedRatings(baseline)) { p -> firstEvaluation && (priorAt == 0L || p.lastPlayedTimestamp <= priorAt) }
    _baseline.value = baseline
    profilePrefs.edit().putString("baseline", encodeBaseline(baseline)).apply()
    refreshTodaySession()
  }

  /**
   * Se saltó la evaluación: punto de partida ESTIMADO por edad y educación ([priorRating]), una sola vez y solo
   * donde no hay datos. El DDA lo corrige en la primera partida de cada juego.
   */
  suspend fun applyPriorIfNeeded(age: AgeBand?, education: Education?) = withContext(Dispatchers.IO) {
    if (_baseline.value != null || profilePrefs.getLong("prior_at", 0L) > 0L) return@withContext
    val prior = priorRating(age, education)
    seedStartingPoint(GameRegistry.allGames.associate { it.id to prior }) { true }
    profilePrefs.edit().putLong("prior_at", System.currentTimeMillis()).apply()
    refreshTodaySession()
  }

  /** [overwrite] = si ese juego toma el punto de partida aunque ya tenga nivel/rating; si no, solo completa lo vacío. */
  private suspend fun seedStartingPoint(ratings: Map<String, Float>, overwrite: (GameProgressEntity) -> Boolean) {
    val all = gameProgressDao.getAllProgressSync().associateBy { it.gameId }
    ratings.forEach { (id, r) ->
      val p = all[id] ?: GameProgressEntity(gameId = id, currentLevel = 1)
      val replace = overwrite(p)
      val commonDda = id !in OWN_ENGINE_GAMES
      gameProgressDao.insertOrUpdate(
        p.copy(
          currentLevel = if (replace) levelFromRating(r) else p.currentLevel,
          ddaRating = if (commonDda && (replace || p.ddaRating < 0f)) r.coerceIn(0f, 1f) else p.ddaRating
        )
      )
    }
  }

  /** Si el camino de hoy todavía no empezó, se vuelve a elegir (con las metas y el mapa nuevos). */
  suspend fun refreshTodaySession() = withContext(Dispatchers.IO) {
    val today = getTodayDateKey()
    val current = dailySessionDao.getDailySessionSync(today)
    if (current != null && current.toDomain().completedCount > 0) return@withContext
    val queue = pickSessionQueue(gameResultDao.getAllResultsSync().map { it.toDomain() })
    val entity = DailySessionEntity(dateKey = today, gameIdsRaw = queue.joinToString(","), completedCount = 0, scoresRaw = "")
    dailySessionDao.insertOrUpdate(entity)
    _dailySession.value = entity.toDomain()
  }

  private fun saveUnlocks(unlocks: Map<String, Long>) {
    _achievementUnlocks.value = unlocks
    achievementPrefs.edit().putString("unlocks", encodeUnlocks(unlocks)).apply()
  }

  /**
   * La primera vez (app actualizada con historial previo) lo ya conseguido se registra SIN celebrar: si no, la
   * próxima partida dispararía una lluvia de logros viejos. Debe correr antes de guardar una partida nueva.
   */
  private suspend fun seedAchievementsIfNeeded() {
    if (achievementPrefs.getBoolean("seeded", false)) return
    val stats = computeAchievementStats(
      gameResultDao.getAllResultsSync().map { it.toDomain() },
      gameProgressDao.getAllProgressSync().associate { it.gameId to it.eloRating }
    )
    val now = System.currentTimeMillis()
    saveUnlocks(Achievements.unlocked(stats).associateWith { now } + _achievementUnlocks.value)
    achievementPrefs.edit().putBoolean("seeded", true).apply()
  }

  /** Registra los logros nuevos con estas cifras y los devuelve (en orden de catálogo). */
  private fun unlockNewAchievements(stats: AchievementStats, timestamp: Long): List<String> {
    val known = _achievementUnlocks.value
    val got = Achievements.unlocked(stats)
    val new = Achievements.all.map { it.id }.filter { it in got && it !in known }
    if (new.isNotEmpty()) saveUnlocks(known + new.associateWith { timestamp })
    return new
  }

  private fun recordLeagueEvents(outcome: RecordOutcome, gameId: String, timestamp: Long) {
    val new = listOfNotNull(
      outcome.globalPromotion()?.let { LeagueEvent(timestamp, it.tier, null) },
      outcome.gamePromotion(gameId)?.let { LeagueEvent(timestamp, it.tier, gameId) }
    )
    if (new.isEmpty()) return
    val all = (_leagueEvents.value + new).takeLast(500)
    _leagueEvents.value = all
    leaguePrefs.edit().putString("events", encodeLeagueEvents(all)).apply()
  }

  val weeklyChallengeProgress: StateFlow<List<WeeklyChallengeProgress>> =
    combine(gameHistory, _claimedChallenges) { history, claimed ->
      computeWeeklyProgress(history, claimed, getWeekKey())
    }.stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = WeeklyChallengeRegistry.all.map { WeeklyChallengeProgress(it, 0, false) }
    )

  private fun getWeekKey(timestamp: Long = System.currentTimeMillis()): String {
    val cal = Calendar.getInstance()
    cal.timeInMillis = timestamp
    cal.firstDayOfWeek = Calendar.MONDAY
    cal.minimalDaysInFirstWeek = 4
    return "${cal.get(Calendar.YEAR)}-W${cal.get(Calendar.WEEK_OF_YEAR)}"
  }

  private fun startOfWeekMillis(): Long {
    val cal = Calendar.getInstance()
    cal.firstDayOfWeek = Calendar.MONDAY
    cal.set(Calendar.DAY_OF_WEEK, Calendar.MONDAY)
    cal.set(Calendar.HOUR_OF_DAY, 0)
    cal.set(Calendar.MINUTE, 0)
    cal.set(Calendar.SECOND, 0)
    cal.set(Calendar.MILLISECOND, 0)
    return cal.timeInMillis
  }

  private fun computeWeeklyProgress(
    history: List<GamePlayResult>,
    claimedKeys: Set<String>,
    weekKey: String
  ): List<WeeklyChallengeProgress> {
    val startOfWeek = startOfWeekMillis()
    val thisWeek = history.filter { it.timestamp >= startOfWeek }
    return WeeklyChallengeRegistry.all.map { def ->
      val progress = when (def.key) {
        "dominios3" -> thisWeek.mapNotNull { GameRegistry.getById(it.gameId)?.domain }.toSet().size
        "reto2" -> thisWeek.count { it.timed }
        "precision3" -> thisWeek.count { it.score >= 85 }
        "dias4" -> thisWeek.map { formatDate(it.timestamp) }.toSet().size
        else -> 0
      }
      val claimed = "$weekKey|${def.key}" in claimedKeys
      WeeklyChallengeProgress(def, progress.coerceAtMost(def.target), claimed)
    }
  }

  // 5. Reactive Daily Session State from Room
  private val _dailySession = MutableStateFlow(
    DailySessionState(
      dateKey = getTodayDateKey(),
      gameIds = listOf("calculo", "parejas", "stroop"),
      completedCount = 0
    )
  )
  val dailySession: StateFlow<DailySessionState> = _dailySession.asStateFlow()

  init {
    repositoryScope.launch {
      initializeDatabaseDefaults()
      observeDailySession()
      seedAchievementsIfNeeded()
    }
  }

  /**
   * Instalación nueva (o después de "Borrar datos"): un perfil vacío (sin nombre ni rango de edad, así se abre el
   * onboarding) y los 9 juegos en cero. Nada de historial de ejemplo: todo lo que se ve (logros, ligas, camino de
   * Hoy, mapa) sale de partidas reales.
   */
  private suspend fun initializeDatabaseDefaults() {
    if (userProfileDao.getAllProfilesSync().isEmpty()) {
      userProfileDao.insertOrUpdate(UserProfileEntity(isActive = true, weeklyGoal = 4, difficultyMode = "ADAPTIVE"))
    }

    if (gameProgressDao.getAllProgressSync().isEmpty()) {
      gameProgressDao.insertAll(GameRegistry.allGames.map { GameProgressEntity(gameId = it.id, currentLevel = 1) })
    }

    // 4. Domain mastery: sin filas nuevas que crear (arranca en 0 para los 6,
    // ya cubierto por el valor inicial del StateFlow). Solo cargamos lo reclamado.
    _claimedChallenges.value = claimedWeeklyChallengeDao.getAllClaimedSync().toSet()

    // 5. Daily Session in Room
    val today = getTodayDateKey()
    val session = dailySessionDao.getDailySessionSync(today)
    if (session == null) {
      val allResults = gameResultDao.getAllResultsSync().map { it.toDomain() }
      val newQueue = pickSessionQueue(allResults)
      val newEntity = DailySessionEntity(
        dateKey = today,
        gameIdsRaw = newQueue.joinToString(","),
        completedCount = 0,
        scoresRaw = ""
      )
      dailySessionDao.insertOrUpdate(newEntity)
      _dailySession.value = newEntity.toDomain()
    } else {
      _dailySession.value = session.toDomain()
    }
  }

  private fun observeDailySession() {
    repositoryScope.launch {
      val today = getTodayDateKey()
      dailySessionDao.getDailySession(today).collect { entity ->
        if (entity != null) {
          _dailySession.value = entity.toDomain()
        }
      }
    }
  }

  suspend fun updateSettings(newSettings: UserSettings) = withContext(Dispatchers.IO) {
    userProfileDao.insertOrUpdate(newSettings.toEntity())
  }

  suspend fun createProfile(
    name: String,
    avatar: String = "🧠",
    difficultyMode: DifficultyMode = DifficultyMode.ADAPTIVE,
    weeklyGoal: Int = 4,
    cognitiveAssistance: Boolean = true
  ): Long = withContext(Dispatchers.IO) {
    val newEntity = UserProfileEntity(
      id = 0L,
      name = name.ifBlank { "Nuevo Perfil" },
      avatar = avatar.ifBlank { "🧠" },
      isActive = true,
      weeklyGoal = weeklyGoal,
      difficultyMode = difficultyMode.name,
      cognitiveAssistance = cognitiveAssistance
    )
    val newId = userProfileDao.insertOrUpdate(newEntity)
    userProfileDao.setActiveProfile(newId)
    newId
  }

  suspend fun switchActiveProfile(profileId: Long) = withContext(Dispatchers.IO) {
    userProfileDao.setActiveProfile(profileId)
  }

  suspend fun deleteProfile(profileId: Long) = withContext(Dispatchers.IO) {
    val all = userProfileDao.getAllProfilesSync()
    if (all.size > 1) {
      userProfileDao.deleteProfileById(profileId)
      val active = userProfileDao.getActiveProfileSync()
      if (active == null) {
        val remaining = userProfileDao.getAllProfilesSync().firstOrNull()
        if (remaining != null) {
          userProfileDao.setActiveProfile(remaining.id)
        }
      }
    }
  }

  suspend fun updateDifficultyPreferences(
    profileId: Long,
    mode: DifficultyMode,
    memoria: Int,
    atencion: Int,
    razonamiento: Int,
    lenguaje: Int,
    calculo: Int,
    velocidad: Int,
    assistance: Boolean,
    timeScale: Float
  ) = withContext(Dispatchers.IO) {
    val existing = userProfileDao.getProfileById(profileId) ?: userProfileDao.getActiveProfileSync()
    if (existing != null) {
      val updated = existing.copy(
        difficultyMode = mode.name,
        difficultyMemoria = memoria.coerceIn(1, 5),
        difficultyAtencion = atencion.coerceIn(1, 5),
        difficultyRazonamiento = razonamiento.coerceIn(1, 5),
        difficultyLenguaje = lenguaje.coerceIn(1, 5),
        difficultyCalculo = calculo.coerceIn(1, 5),
        difficultyVelocidad = velocidad.coerceIn(1, 5),
        cognitiveAssistance = assistance,
        timeScaleFactor = timeScale.coerceIn(0.5f, 2.0f)
      )
      userProfileDao.insertOrUpdate(updated)
    }
  }

  private fun pickSessionQueue(history: List<GamePlayResult>): List<String> {
    val domainScores = mutableMapOf<DomainType, MutableList<Int>>()
    DomainType.values().forEach { domainScores[it] = mutableListOf() }
    history.forEach { res ->
      val game = GameRegistry.getById(res.gameId)
      if (game != null) {
        domainScores[game.domain]?.add(res.score)
      }
    }

    // Nivel por dominio: puntaje promedio jugado (0..1) o, sin partidas, el del mapa del punto de partida.
    val baselineNow = _baseline.value
    val domainLevel = DomainType.values().associateWith { domain ->
      val scores = domainScores[domain].orEmpty()
      if (scores.isNotEmpty()) scores.average().toFloat() / 100f else baselineNow?.domains?.get(domain) ?: 0f
    }
    // Primero las metas (la más baja primero) y lo más bajo; el tercer juego sale de un dominio que no es meta,
    // para que el camino no repita siempre los mismos dominios.
    val goalsNow = _goals.value
    val ranked = rankDomainsForSession(goalsNow, domainLevel)
    val sortedDomains = ranked.take(2) + (ranked.drop(2).firstOrNull { it !in goalsNow } ?: ranked[2])

    val selected = mutableListOf<String>()
    sortedDomains.take(3).forEach { domain ->
      val gameInDomain = GameRegistry.allGames.filter { it.domain == domain }.randomOrNull()
      if (gameInDomain != null) {
        selected.add(gameInDomain.id)
      }
    }

    while (selected.size < 3) {
      val candidate = GameRegistry.allGames.random().id
      if (!selected.contains(candidate)) selected.add(candidate)
    }
    return selected
  }

  /** [countsForDailySession] false = no avanza el camino de hoy (juegos de la evaluación inicial). */
  suspend fun recordGameResult(result: GamePlayResult, countsForDailySession: Boolean = true): RecordOutcome = withContext(Dispatchers.IO) {
    seedAchievementsIfNeeded() // antes de guardar: lo que consiga ESTA partida sí se celebra
    // 1. Add to Room game_results table
    gameResultDao.insert(result.toEntity())

    // 2. Adaptive level calculation and progress update in Room
    val currentProgress = gameProgressDao.getProgressForGameSync(result.gameId)
      ?: GameProgressEntity(gameId = result.gameId, currentLevel = 1)
    // Trofeos de los 9 juegos antes de esta partida (liga general = promedio; sin jugar = 0).
    val ratingsBefore = gameProgressDao.getAllProgressSync().associate { it.gameId to it.eloRating }

    val activeProfile = userProfileDao.getActiveProfileSync() ?: userProfileDao.getUserProfileSync()
    val isAdaptive = activeProfile?.difficultyMode == "ADAPTIVE"

    val playedLevel = result.level
    var newLevel = playedLevel
    var newMastery = currentProgress.masteryStreak
    var didLevelUp = false
    if (isAdaptive) {
      if (result.score >= 85) {
        if (playedLevel < 5) {
          newLevel = playedLevel + 1
          didLevelUp = true
        } else {
          // Ya está en el techo de nivel (Experto): seguir rindiendo bien seguirá
          // endureciendo el juego (tiempos, rangos) vía masteryStreak, que no tiene
          // límite superior. didLevelUp se marca igual para celebrar el avance en la UI.
          newMastery += 1
          didLevelUp = true
        }
      } else if (result.score <= 45 && playedLevel > 1) {
        if (playedLevel == 5 && newMastery > 0) {
          // Colchón de gracia: antes de bajar de Experto, primero se consume la
          // racha de maestría acumulada — un mal día no tira por la borda meses
          // de progreso más allá del nivel 5.
          newMastery = (newMastery - 2).coerceAtLeast(0)
        } else {
          newLevel = playedLevel - 1
        }
      }
    }

    val newRating = (currentProgress.eloRating + eloDelta(result.score, currentProgress.eloRating))
      .coerceAtLeast(0)

    val updatedProgress = currentProgress.copy(
      currentLevel = newLevel,
      masteryStreak = newMastery,
      eloRating = newRating,
      ddaRating = result.endRating?.let { blendDdaRating(currentProgress.ddaRating, it) } ?: currentProgress.ddaRating,
      highestScore = maxOf(currentProgress.highestScore, result.score),
      totalGamesPlayed = currentProgress.totalGamesPlayed + 1,
      lastPlayedTimestamp = result.timestamp
    )
    gameProgressDao.insertOrUpdate(updatedProgress)
    val ratingsAfter = ratingsBefore + (result.gameId to newRating)
    val globalOf = { m: Map<String, Int> -> GameRegistry.allGames.sumOf { m[it.id] ?: 0 } / GameRegistry.allGames.size }

    // 3. Advance Daily Session in Room if game matches current queue
    val today = getTodayDateKey()
    val currentSessionEntity = dailySessionDao.getDailySessionSync(today)
    if (currentSessionEntity != null && countsForDailySession) {
      val domainSession = currentSessionEntity.toDomain()
      if (domainSession.completedCount < domainSession.gameIds.size) {
        val expectedGame = domainSession.gameIds[domainSession.completedCount]
        if (expectedGame == result.gameId) {
          val updatedCount = domainSession.completedCount + 1
          val updatedScores = domainSession.scores + result.score
          val updatedSession = domainSession.copy(completedCount = updatedCount, scores = updatedScores)
          dailySessionDao.insertOrUpdate(updatedSession.toEntity())
          _dailySession.value = updatedSession
        }
      }
    }

    val allResults = gameResultDao.getAllResultsSync().map { it.toDomain() }

    // 4. Maestría por dominio (etapa 4): XP sin techo por CUALQUIER partida del
    // dominio, no solo cuando el juego individual mejora de nivel.
    val gameDef = GameRegistry.getById(result.gameId)
    if (gameDef != null) {
      val xpGained = (result.score / 5).coerceAtLeast(1)
      awardDomainXp(gameDef.domain, xpGained)
    }

    // 5. Desafíos semanales: revisa si alguno se completó recién con esta
    // partida y, si no se había reclamado ya, otorga el bono una sola vez.
    val weekKey = getWeekKey()
    val progressNow = computeWeeklyProgress(allResults, _claimedChallenges.value, weekKey)
    progressNow.filter { it.isComplete && !it.claimed }.forEach { wp ->
      val claimId = "$weekKey|${wp.def.key}"
      claimedWeeklyChallengeDao.insert(ClaimedWeeklyChallengeEntity(claimId))
      DomainType.values().forEach { d -> awardDomainXp(d, WeeklyChallengeRegistry.XP_REWARD_PER_DOMAIN) }
      _claimedChallenges.value = _claimedChallenges.value + claimId
    }

    val outcome = RecordOutcome(
      didLevelUp = didLevelUp,
      gameRatingBefore = currentProgress.eloRating,
      gameRatingAfter = newRating,
      globalBefore = globalOf(ratingsBefore),
      globalAfter = globalOf(ratingsAfter),
      newAchievements = unlockNewAchievements(computeAchievementStats(allResults, ratingsAfter), result.timestamp)
    )
    recordLeagueEvents(outcome, result.gameId, result.timestamp)
    outcome
  }

  // Cuánto sube o baja el ELO de un juego tras una partida. A mayor tier, más
  // exigente el umbral para seguir ganando puntos (igual que un ladder real: cuesta
  // más mantenerse arriba que subir desde abajo). Piso en 0 (Bronce 5), sin techo.
  private fun eloDelta(score: Int, currentRating: Int): Int {
    val tier = RankTier.fromRating(currentRating)
    val gainThreshold = 55 + tier.ordinal * 5
    val lossThreshold = 40 + tier.ordinal * 5
    return when {
      score >= gainThreshold + 25 -> 25
      score >= gainThreshold -> 15
      score >= lossThreshold -> 2
      score >= lossThreshold - 20 -> -10
      else -> -20
    }
  }

  private suspend fun awardDomainXp(domain: DomainType, amount: Int) {
    val current = domainMasteryDao.getForDomain(domain.name)?.xp ?: 0
    domainMasteryDao.insertOrUpdate(DomainMasteryEntity(domain = domain.name, xp = current + amount))
  }

  fun calculateStreak(history: List<GamePlayResult>): Int {
    if (history.isEmpty()) return 0
    val daysWithSessions = history.map { formatDate(it.timestamp) }.toSet()
    val cal = Calendar.getInstance()

    var streak = 0
    val todayKey = synchronized(dateFormat) { dateFormat.format(cal.time) }
    val playedToday = daysWithSessions.contains(todayKey)

    if (!playedToday) {
      cal.add(Calendar.DAY_OF_YEAR, -1)
      val yesterdayKey = synchronized(dateFormat) { dateFormat.format(cal.time) }
      if (!daysWithSessions.contains(yesterdayKey)) {
        return 0
      }
      streak = 1
      cal.add(Calendar.DAY_OF_YEAR, -1)
    } else {
      streak = 1
      cal.add(Calendar.DAY_OF_YEAR, -1)
    }

    while (true) {
      val prevKey = synchronized(dateFormat) { dateFormat.format(cal.time) }
      if (daysWithSessions.contains(prevKey)) {
        streak++
        cal.add(Calendar.DAY_OF_YEAR, -1)
      } else {
        break
      }
    }
    return streak
  }

  suspend fun resetData() = withContext(Dispatchers.IO) {
    gameResultDao.deleteAll()
    gameProgressDao.deleteAll()
    dailySessionDao.deleteAll()
    userProfileDao.deleteAll()
    domainMasteryDao.deleteAll()
    claimedWeeklyChallengeDao.deleteAll()
    _claimedChallenges.value = emptySet()
    // Lo guardado fuera de Room también (si no, tras borrar quedaban logros, ascensos y el mapa de antes).
    leaguePrefs.edit().clear().apply()
    _leagueEvents.value = emptyList()
    achievementPrefs.edit().clear().apply()
    _achievementUnlocks.value = emptyMap()
    profilePrefs.edit().clear().apply()
    _education.value = null
    _goals.value = emptySet()
    _baseline.value = null
    initializeDatabaseDefaults()
  }
}
