package com.example.data

import android.content.Context
import com.example.data.local.*
import com.example.model.*
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.cancel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.text.SimpleDateFormat
import java.util.*


class NeuroVidaRepository(
  context: Context,
  private val database: NeuroVidaDatabase = NeuroVidaDatabase.getDatabase(context)
) {
  private val repositoryScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

  /**
   * Cancela el trabajo de fondo del repositorio (lo que sigue leyendo la base) Y ESPERA a que termine (hasta 10 s). Solo lo usan las pruebas, al terminar cada una; la app vive con el proceso.
   *
   * Esperar es lo que importa: una lectura de Room que ya está a medias no se puede interrumpir, y si la base se cierra mientras sigue corriendo, su `endTransaction` encuentra la base cerrada («attempt to re-open an already-closed
   * object») y el error le llega a la prueba siguiente (el rojo intermitente de la verificación de GitHub, Tarea 57; reproducido por `BackgroundWorkShutdownTest`).
   */
  @androidx.annotation.VisibleForTesting
  fun close() {
    val job = repositoryScope.coroutineContext[kotlinx.coroutines.Job] ?: return
    job.cancel()
    val limit = System.nanoTime() + 10_000_000_000L
    while (!job.isCompleted && System.nanoTime() < limit) Thread.sleep(2)
  }
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

  // 1. Reactive Settings Flow from Room (el perfil activo; desde el 8-oct ya no se crean, cambian ni borran perfiles: la tabla user_profile y su DAO se quedan)
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
      list.forEach { e -> DomainType.fromStored(e.domain)?.let { map[it] = (map[it] ?: 0) + e.xp } }
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

  // Medidas propias de los juegos estrella, una por partida (ver StarMeasures.kt): su evolución se muestra en Hoy
  // ("descubrimiento del día") y en la ventana de cada zona del planeta. SharedPreferences: solo se agregan.
  private val measurePrefs = context.getSharedPreferences("star_measures", Context.MODE_PRIVATE)
  private val _starMeasures = MutableStateFlow(StarMeasures.decode(measurePrefs.getString("points", null)))
  val starMeasures: StateFlow<List<MeasurePoint>> = _starMeasures.asStateFlow()

  // Colección de palabras raras acertadas en Lluvia de meteoros (en orden de hallazgo). SharedPreferences: solo se agrega.
  private val wordPrefs = context.getSharedPreferences("word_collection", Context.MODE_PRIVATE)
  private val _wordCollection = MutableStateFlow(wordPrefs.getString("words", "").orEmpty().split(",").filter { it.isNotBlank() })
  val wordCollection: StateFlow<List<String>> = _wordCollection.asStateFlow()

  // El atlas de La estrella intrusa: láminas ganadas (con el día), las ganadas con nombre propio y las reglas por repasar
  // (ver Atlas.kt). SharedPreferences: va en el respaldo (es progreso de la persona).
  private val atlasPrefs = context.getSharedPreferences("atlas", Context.MODE_PRIVATE)
  private val _atlas = MutableStateFlow(
    AtlasState(
      Atlas.decodePlates(atlasPrefs.getString("plates", null)),
      Atlas.decodeSet(atlasPrefs.getString("named", null)),
      Atlas.decodeSet(atlasPrefs.getString("review", null))
    )
  )
  val atlas: StateFlow<AtlasState> = _atlas.asStateFlow()

  /** Suma al atlas lo que mandó una partida de La estrella intrusa (láminas nuevas, falladas, repasadas y con nombre propio). */
  private fun recordAtlas(result: GamePlayResult) {
    if (result.gameId != "intrusa" || result.intrSeenType == null) return
    val today = java.time.LocalDate.now().toEpochDay().toInt()
    val next = Atlas.record(
      _atlas.value, result.intrNewPlates.orEmpty(), result.intrNamed.orEmpty(), result.intrReviewNew.orEmpty(), result.intrReviewDone.orEmpty(), today
    )
    if (next == _atlas.value) return
    _atlas.value = next
    atlasPrefs.edit()
      .putString("plates", Atlas.encodePlates(next.plates))
      .putString("named", Atlas.encodeSet(next.named))
      .putString("review", Atlas.encodeSet(next.review))
      .apply()
  }

  // Las palabras azules de «En la punta de la lengua» (las que Nubi tuvo que mostrar): esperan volver en otra partida. Solo hasta Punta.MAX_PENDING.
  // SharedPreferences: va en el respaldo (es progreso de la persona). La app las manda a Unity en cada partida (`punta_pending` de la config del juego).
  private val puntaPrefs = context.getSharedPreferences("punta_words", Context.MODE_PRIVATE)
  private val _puntaPending = MutableStateFlow(Punta.words(puntaPrefs.getString("pending", null)).takeLast(Punta.MAX_PENDING))
  val puntaPending: StateFlow<List<String>> = _puntaPending.asStateFlow()

  /** Suma las azules de una partida de «En la punta de la lengua» y quita las que hoy se encontraron solas o con 1-2 ayudas. */
  private fun recordPunta(result: GamePlayResult) {
    if (result.gameId != "anagramas" || result.puntaSolo == null) return
    val next = Punta.mergePending(_puntaPending.value, result.puntaBlue.orEmpty(), result.puntaCleared.orEmpty())
    if (next == _puntaPending.value) return
    _puntaPending.value = next
    puntaPrefs.edit().putString("pending", Punta.encode(next)).apply()
  }

  // El cohete de «Engranajes»: las luces encendidas (0..9) y los cohetes que ya despegaron. Es progreso de la persona: SharedPreferences que VA en el respaldo
  // (`engranajes_rocket`). La app lo manda a Unity en cada partida (`engr_lights`, `engr_orbit`) y Unity devuelve los nuevos al terminar.
  private val rocketPrefs = context.getSharedPreferences("engranajes_rocket", Context.MODE_PRIVATE)
  // La misión del día de Bitácora de Misión (juego retirado el 5-oct): ya no se lee, pero sigue en el respaldo y «Borrar datos» la limpia.
  private val retiredMissionPrefs = context.getSharedPreferences("mission_log", Context.MODE_PRIVATE)
  private val _engranajesRocket = MutableStateFlow(Engranajes.rocket(rocketPrefs.getInt("lights", 0), rocketPrefs.getInt("orbit", 0)))
  val engranajesRocket: StateFlow<Engranajes.Rocket> = _engranajesRocket.asStateFlow()

  /** Guarda el cohete con que terminó una partida de Engranajes (en cualquier modo: las luces ganadas no se pierden). */
  private fun recordEngranajes(result: GamePlayResult) {
    if (result.gameId != "engranajes") return
    val lights = result.engrLights ?: return
    val orbit = result.engrOrbit ?: return
    val next = Engranajes.rocket(lights, orbit)
    if (next == _engranajesRocket.value) return
    _engranajesRocket.value = next
    rocketPrefs.edit().putInt("lights", next.lights).putInt("orbit", next.orbit).apply()
  }

  // El récord de «Bodega de carga»: la bodega más grande (en objetos) que la persona recordó sin errores. Es progreso: SharedPreferences que VA en el respaldo (`bodega_record`). La app lo manda a Unity en
  // cada partida (`bod_best`) y Unity devuelve el nuevo al terminar; solo sube con pedidos perfectos (lo asegura Unity) y aquí nunca baja.
  private val bodegaPrefs = context.getSharedPreferences("bodega_record", Context.MODE_PRIVATE)
  private val _bodegaRecord = MutableStateFlow(Bodega.mergeRecord(bodegaPrefs.getInt("best", 0), null))
  val bodegaRecord: StateFlow<Int> = _bodegaRecord.asStateFlow()

  /** Guarda el récord con que terminó una partida de Bodega de carga (en cualquier modo). */
  private fun recordBodega(result: GamePlayResult) {
    if (result.gameId != "bodega") return
    val next = Bodega.mergeRecord(_bodegaRecord.value, result.bodBest)
    if (next == _bodegaRecord.value) return
    _bodegaRecord.value = next
    bodegaPrefs.edit().putInt("best", next).apply()
  }

  // El récord de «Constelaciones»: la racha de memoria más larga (parejas encontradas de memoria seguidas) de siempre. Es progreso: SharedPreferences que VA en el respaldo (`constelaciones_record`). La app lo manda a Unity
  // en cada partida (`con_best`) y Unity devuelve el mayor al terminar; aquí nunca baja.
  private val constelacionesPrefs = context.getSharedPreferences("constelaciones_record", Context.MODE_PRIVATE)
  private val _constelacionesRecord = MutableStateFlow(Constelaciones.mergeRecord(constelacionesPrefs.getInt("best", 0), null))
  val constelacionesRecord: StateFlow<Int> = _constelacionesRecord.asStateFlow()

  /** Guarda el récord con que terminó una partida de Constelaciones (en cualquier modo). */
  private fun recordConstelaciones(result: GamePlayResult) {
    if (result.gameId != "parejas") return
    val next = Constelaciones.mergeRecord(_constelacionesRecord.value, result.conBest)
    if (next == _constelacionesRecord.value) return
    _constelacionesRecord.value = next
    constelacionesPrefs.edit().putInt("best", next).apply()
  }

  // El récord de «La estación de correo»: las cartas bien puestas en un día perfecto (todos los encargos cumplidos), el mayor de siempre. Es progreso: SharedPreferences que VA en el respaldo (`correo_record`). La app lo manda a Unity en cada
  // partida (`mail_best`) y Unity devuelve el mayor al terminar; aquí nunca baja.
  private val correoPrefs = context.getSharedPreferences("correo_record", Context.MODE_PRIVATE)
  private val _correoRecord = MutableStateFlow(Mail.mergeRecord(correoPrefs.getInt("best", 0), null))
  val correoRecord: StateFlow<Int> = _correoRecord.asStateFlow()

  /** Guarda el récord con que terminó una partida de «La estación de correo» (en cualquier modo). */
  private fun recordCorreo(result: GamePlayResult) {
    if (result.gameId != "correo") return
    val next = Mail.mergeRecord(_correoRecord.value, result.mailBest)
    if (next == _correoRecord.value) return
    _correoRecord.value = next
    correoPrefs.edit().putInt("best", next).apply()
  }

  // El récord de «Satélites: enciende tu planeta»: las luces encendidas en una partida, el mayor de siempre. Es progreso: SharedPreferences que VA en el respaldo (`satelites_record`). La app lo manda a Unity en cada partida (`sat_best`) y Unity
  // devuelve el mayor al terminar; aquí nunca baja.
  private val satelitesPrefs = context.getSharedPreferences("satelites_record", Context.MODE_PRIVATE)
  private val _satelitesRecord = MutableStateFlow(Satelites.mergeRecord(satelitesPrefs.getInt("best", 0), null))
  val satelitesRecord: StateFlow<Int> = _satelitesRecord.asStateFlow()

  /** Guarda el récord con que terminó una partida de «Satélites» (en cualquier modo). */
  private fun recordSatelites(result: GamePlayResult) {
    if (result.gameId != "satelites") return
    val next = Satelites.mergeRecord(_satelitesRecord.value, result.satBest)
    if (next == _satelitesRecord.value) return
    _satelitesRecord.value = next
    satelitesPrefs.edit().putInt("best", next).apply()
  }

  // El récord de «Rescate relámpago»: las cápsulas rescatadas en una partida, el mayor de siempre. Es progreso: SharedPreferences que VA en el respaldo (`rescate_record`). La app lo manda a Unity en cada partida (`resc_best`) y Unity devuelve el mayor al
  // terminar; aquí nunca baja.
  private val rescatePrefs = context.getSharedPreferences("rescate_record", Context.MODE_PRIVATE)
  private val _rescateRecord = MutableStateFlow(Rescate.mergeRecord(rescatePrefs.getInt("best", 0), null))
  val rescateRecord: StateFlow<Int> = _rescateRecord.asStateFlow()

  // Los totales de toda la vida («En total: 342 cápsulas y 12 viajes a la estación», v4): las cápsulas rescatadas y los viajes a la estación, sumados partida a partida. Van en las mismas preferencias `rescate_record` (claves `total` y `trips`), así que
  // ya están en el respaldo; cada resultado se guarda una sola vez (`UnityResultInbox`), así que sumar no cuenta doble.
  private val _rescateTotals = MutableStateFlow(Rescate.Totals(rescatePrefs.getInt("total", 0).coerceAtLeast(0), rescatePrefs.getInt("trips", 0).coerceAtLeast(0)))
  val rescateTotals: StateFlow<Rescate.Totals> = _rescateTotals.asStateFlow()

  /** Guarda el récord y suma los totales con que terminó una partida de «Rescate relámpago» (en cualquier modo). */
  private fun recordRescate(result: GamePlayResult) {
    if (result.gameId != "radar") return
    val record = Rescate.mergeRecord(_rescateRecord.value, result.rescBest)
    val totals = Rescate.addTotals(_rescateTotals.value, result.rescRescued, result.rescTrips)
    if (record == _rescateRecord.value && totals == _rescateTotals.value) return
    val edit = rescatePrefs.edit()
    if (record != _rescateRecord.value) {
      _rescateRecord.value = record
      edit.putInt("best", record)
    }
    if (totals != _rescateTotals.value) {
      _rescateTotals.value = totals
      edit.putInt("total", totals.rescued).putInt("trips", totals.trips)
    }
    edit.apply()
  }

  // El récord de «Acoplamiento» («muelle de acoplamiento», 10-oct): los módulos acoplados en una partida, el mayor de siempre. Es progreso: SharedPreferences que VA en el respaldo (`acoplamiento_record`). La app lo manda a Unity en cada partida (`dock_best`) y Unity devuelve el
  // mayor al terminar; aquí nunca baja. Los totales de toda la vida («Has acoplado 342 módulos · 12 anillos») van en las mismas preferencias (claves `total` y `rings`); cada resultado se guarda una sola vez (`UnityResultInbox`), así que sumar no cuenta doble.
  private val acoplamientoPrefs = context.getSharedPreferences("acoplamiento_record", Context.MODE_PRIVATE)
  private val _acoplamientoRecord = MutableStateFlow(Acoplamiento.mergeRecord(acoplamientoPrefs.getInt("best", 0), null))
  val acoplamientoRecord: StateFlow<Int> = _acoplamientoRecord.asStateFlow()
  private val _acoplamientoTotals = MutableStateFlow(Acoplamiento.Totals(acoplamientoPrefs.getInt("total", 0).coerceAtLeast(0), acoplamientoPrefs.getInt("rings", 0).coerceAtLeast(0)))
  val acoplamientoTotals: StateFlow<Acoplamiento.Totals> = _acoplamientoTotals.asStateFlow()

  /** Guarda el récord y suma los totales con que terminó una partida de «Acoplamiento» (en cualquier modo). */
  private fun recordAcoplamiento(result: GamePlayResult) {
    if (result.gameId != "acoplamiento") return
    val record = Acoplamiento.mergeRecord(_acoplamientoRecord.value, result.dockBest)
    val totals = Acoplamiento.addTotals(_acoplamientoTotals.value, result.dockDocked, result.dockRings)
    if (record == _acoplamientoRecord.value && totals == _acoplamientoTotals.value) return
    val edit = acoplamientoPrefs.edit()
    if (record != _acoplamientoRecord.value) {
      _acoplamientoRecord.value = record
      edit.putInt("best", record)
    }
    if (totals != _acoplamientoTotals.value) {
      _acoplamientoTotals.value = totals
      edit.putInt("total", totals.modules).putInt("rings", totals.rings)
    }
    edit.apply()
  }

  // Las cúpulas de tu base de «Aterrizaje Lunar» (10-oct): las de toda la vida (`total`) y las de tu mejor partida (`best`). Es progreso: SharedPreferences que VA en el respaldo (`aterrizaje_record`). Cada resultado se guarda una sola vez (`UnityResultInbox`), así que sumar no cuenta doble.
  private val aterrizajePrefs = context.getSharedPreferences("aterrizaje_record", Context.MODE_PRIVATE)
  private val _aterrizajeTotals = MutableStateFlow(Aterrizaje.Totals(aterrizajePrefs.getInt("total", 0).coerceAtLeast(0), aterrizajePrefs.getInt("best", 0).coerceAtLeast(0)))
  val aterrizajeTotals: StateFlow<Aterrizaje.Totals> = _aterrizajeTotals.asStateFlow()

  /** Suma las cúpulas con que terminó una partida de «Aterrizaje Lunar» (en cualquier modo). */
  private fun recordAterrizaje(result: GamePlayResult) {
    if (result.gameId != "aterrizaje") return
    val totals = Aterrizaje.addTotals(_aterrizajeTotals.value, result.landDomes)
    if (totals == _aterrizajeTotals.value) return
    _aterrizajeTotals.value = totals
    aterrizajePrefs.edit().putInt("total", totals.domes).putInt("best", totals.best).apply()
  }

  // Frases de ¿Verdad o disparate? que la persona marcó como "no está clara" (ids; las últimas 300). Van en el informe de
  // errores de Ajustes para que quien dio la app las corrija en el banco (tools/frases/buscar.py las encuentra por id).
  private val unclearPrefs = context.getSharedPreferences("unclear_sentences", Context.MODE_PRIVATE)
  private val _unclearSentences = MutableStateFlow(unclearPrefs.getString("ids", "").orEmpty().split(",").filter { it.isNotBlank() })
  val unclearSentences: StateFlow<List<String>> = _unclearSentences.asStateFlow()

  private fun recordUnclearSentences(result: GamePlayResult) {
    val found = result.svUnclear.orEmpty()
    if (found.isEmpty()) return
    val all = (_unclearSentences.value + found).distinct().takeLast(300)
    if (all == _unclearSentences.value) return
    _unclearSentences.value = all
    unclearPrefs.edit().putString("ids", all.joinToString(",")).apply()
  }

  /** Texto para el informe de errores: las frases marcadas como poco claras (vacío si no hay). */
  fun unclearReport(): String =
    if (_unclearSentences.value.isEmpty()) "" else "Frases marcadas como poco claras (ids): " + _unclearSentences.value.joinToString(", ")

  private fun recordWordCollection(result: GamePlayResult) {
    val found = result.lexRareWords.orEmpty()
    if (found.isEmpty()) return
    val all = (_wordCollection.value + found).distinct()
    if (all.size == _wordCollection.value.size) return
    _wordCollection.value = all
    wordPrefs.edit().putString("words", all.joinToString(",")).apply()
  }

  // Avance (ver Skill.kt): juegos ya medidos, Experto abierto y fecha de cada etapa. SharedPreferences: solo se agrega.
  private val skillPrefs = context.getSharedPreferences("skill", Context.MODE_PRIVATE)
  private val _skill = MutableStateFlow(SkillState.decode(skillPrefs.getString("state", null)))
  val skill: StateFlow<SkillState> = _skill.asStateFlow()

  /**
   * Una sola vez, al actualizar la app: los juegos que ya tenían partidas cuentan como medidos (antes de los modos
   * todas las partidas eran "a tu medida").
   */
  suspend fun seedSkillIfNeeded() = withContext(Dispatchers.IO) {
    if (skillPrefs.contains("state")) return@withContext
    val played = gameResultDao.getAllResultsSync().map { it.gameId }.toSet()
    saveSkill(_skill.value.copy(measured = _skill.value.measured + played))
  }

  private fun saveSkill(state: SkillState) {
    _skill.value = state
    skillPrefs.edit().putString("state", state.encode()).apply()
  }

  // Historia del avance de cada juego (ver AreaProgress.kt): con ella Hoy muestra cuánto se movió cada área en la
  // semana. SharedPreferences: solo se agrega (se guardan ~60 días).
  private val progressPrefs = context.getSharedPreferences("progress_log", Context.MODE_PRIVATE)
  private val _progressLog = MutableStateFlow(AreaProgress.decode(progressPrefs.getString("points", null)))
  val progressLog: StateFlow<List<ProgressPoint>> = _progressLog.asStateFlow()

  private fun logProgress(gameId: String, time: Long, progress: Float) {
    val next = AreaProgress.append(_progressLog.value, ProgressPoint(gameId, time, progress))
    if (next == _progressLog.value) return
    _progressLog.value = next
    progressPrefs.edit().putString("points", AreaProgress.encode(next)).apply()
  }

  // Logros conseguidos (id -> cuándo). Se derivan del historial y los trofeos (ver Achievements.kt); acá solo
  // se recuerda cuáles ya se celebraron.
  private val achievementPrefs = context.getSharedPreferences("achievements", Context.MODE_PRIVATE)
  private val _achievementUnlocks = MutableStateFlow(decodeUnlocks(achievementPrefs.getString("unlocks", null)))
  val achievementUnlocks: StateFlow<Map<String, Long>> = _achievementUnlocks.asStateFlow()

  // "Tu punto de partida" (ver Baseline.kt): nivel educacional, metas y el mapa de la evaluación inicial. En
  // SharedPreferences (como ligas y logros) para no migrar Room por datos que solo se escriben en el onboarding.
  private val profilePrefs = context.getSharedPreferences("profile_extra", Context.MODE_PRIVATE)

  // «Sin reloj / Contra el reloj» por juego (ficha de Juegos, 6-oct): claves `reto_<juego>` en `profile_extra` (van en el respaldo). Ver data/RetoChoice.kt.
  private val _retoChoices = MutableStateFlow(RetoChoice.decode(profilePrefs.all))
  val retoChoices: StateFlow<Map<String, Boolean>> = _retoChoices.asStateFlow()

  /** La elección que vale para [gameId]: la guardada o, si no hay, el ajuste viejo de Ajustes («Modo contra el reloj»), que pasó a ser la elección inicial de cada juego. */
  fun retoFor(gameId: String): Boolean = RetoChoice.resolve(gameId, _retoChoices.value[gameId], userSettings.value.defaultTimed)

  fun setReto(gameId: String, timed: Boolean) {
    if (!RetoChoice.supports(gameId)) return
    _retoChoices.value = _retoChoices.value + (gameId to timed)
    profilePrefs.edit().putBoolean(RetoChoice.key(gameId), timed).apply()
  }
  private val _education = MutableStateFlow(
    profilePrefs.getString("education", null)?.let { n -> Education.values().firstOrNull { it.name == n } }
  )
  val education: StateFlow<Education?> = _education.asStateFlow()
  private val _goals = MutableStateFlow(decodeGoals(profilePrefs.getString("goals", null)))
  val goals: StateFlow<Set<DomainType>> = _goals.asStateFlow()
  private val _baseline = MutableStateFlow(decodeBaseline(profilePrefs.getString("baseline", null)))
  val baseline: StateFlow<Baseline?> = _baseline.asStateFlow()

  // Preferencias del inicio nuevo (`docs/diseno-inicio.md`): qué te sirve más ver primero al terminar un juego y si cuesta distinguir colores. En el mismo archivo
  // `profile_extra` (ya va al respaldo: es configuración de la persona, no estado pasajero). Sin dato = el valor por defecto de cada una.
  /** Lee `result_focus`; si todavía está el valor viejo `coach_tone` lo migra (celebrar → avance, claro → consejo) y borra la clave vieja. */
  private fun loadResultFocus(): ResultFocus {
    val focus = ResultFocus.migrate(profilePrefs.getString("result_focus", null), profilePrefs.getString("coach_tone", null))
    if (profilePrefs.contains("coach_tone")) profilePrefs.edit().putString("result_focus", focus.name).remove("coach_tone").apply()
    return focus
  }

  // `result_focus` (qué te sirve más ver primero al terminar un juego: avance / consejo) reemplazó el 3-oct a `coach_tone` (celebrar / claro): se migra
  // el valor viejo una sola vez y se borra la clave vieja.
  private val _resultFocus = MutableStateFlow(loadResultFocus())
  val resultFocus: StateFlow<ResultFocus> = _resultFocus.asStateFlow()
  private val _colorVision = MutableStateFlow(ColorVision.fromStored(profilePrefs.getString("color_vision", null)) ?: ColorVision.DEFAULT)
  val colorVision: StateFlow<ColorVision> = _colorVision.asStateFlow()

  /** Nivel de estudios: ya no se pregunta en el inicio; queda en Perfil, opcional (null = no respondió). Conserva el valor de quien ya lo tenía. */
  fun saveEducation(education: Education?) {
    _education.value = education
    profilePrefs.edit().putString("education", education?.name).apply()
  }

  /** Metas elegidas (vacías = sin preferencia): el camino diario les da prioridad. */
  fun saveGoals(goals: Set<DomainType>) {
    _goals.value = goals
    profilePrefs.edit().putString("goals", encodeGoals(goals)).apply()
  }

  fun saveResultFocus(focus: ResultFocus) {
    _resultFocus.value = focus
    profilePrefs.edit().putString("result_focus", focus.name).remove("coach_tone").apply()
  }

  fun saveColorVision(vision: ColorVision) {
    _colorVision.value = vision
    profilePrefs.edit().putString("color_vision", vision.name).apply()
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
      gameProgressDao.insertOrUpdate(
        p.copy(
          currentLevel = if (replace) levelFromRating(r) else p.currentLevel,
          ddaRating = if (replace || p.ddaRating < 0f) r.coerceIn(0f, 1f) else p.ddaRating
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

  private fun recordStarMeasures(result: GamePlayResult) {
    val new = starPoints(result)
    if (new.isEmpty()) return
    val all = (_starMeasures.value + new).takeLast(3000)
    _starMeasures.value = all
    measurePrefs.edit().putString("points", StarMeasures.encode(all)).apply()
  }

  /** La medida propia de la partida, si es de un juego estrella y trajo una estimación válida. */
  private fun starPoints(r: GamePlayResult): List<MeasurePoint> {
    fun pct(hits: Int?, total: Int?) = if (hits != null && total != null && total > 0) 100f * hits / total else null
    val (key, value) = when (r.gameId) {
      "radar" -> "glance" to r.glanceMs?.toFloat()
      "freno" -> "brake" to r.brakeMs?.toFloat()
      "satelites" -> "tracking" to r.trackingCapacity
      "aterrizaje" -> "numline" to r.numlineErrorPct
      "disparate" -> "wpm" to Reading.mark(r.svWpm)
      "cosecha" -> "harvest" to Harvest.mark(r.harvCommonFound, r.harvCommonTotal)
      "intrusa" -> "atlas" to Atlas.mark(r.intrSeenType, r.intrHitsType)
      "anagramas" -> "punta" to Punta.mark(r.puntaSolo, Punta.total(r.puntaSolo, r.puntaPista, r.puntaLetras, r.puntaVista))
      "calculo" -> "carga" to Carga.mark(r.cargaAlone, r.totalTrials)
      "engranajes" -> "taller" to (if (r.engrEtapa != null) Engranajes.mark(r.correctAnswers, r.totalTrials) else null)
      "bodega" -> "bodega" to (if (r.bodGroup != null) Bodega.mark(r.correctAnswers, r.totalTrials) else null)
      "parejas" -> "place" to (if (r.conGroups != null) Constelaciones.mark(r.correctAnswers, r.totalTrials) else null)
      "secuencia" -> "trail" to Trail.mark(r.rasBestLen)
      "meteoros" -> "vocab" to Vocabulary.mark(Vocabulary.bandPercents(r.lexBandSeen, r.lexBandHits, r.lexFaSeen, r.lexFaHits), r.lexBandSeen)
      "acoplamiento" -> "rotation" to r.rotationSpeedDps?.toFloat()
      "piloto" -> "mandos" to Piloto.mark(r.pilSignalPct)
      "correo" -> "estacion" to (if (r.mailGroup != null) Mail.mark(r.correctAnswers, r.totalTrials) else null)
      else -> return emptyList()
    }
    return if (value == null || value.isNaN()) emptyList()
    else listOf(MeasurePoint(r.timestamp, key, value, r.endRating?.coerceIn(0f, 1f) ?: -1f, r.timed))
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
      _dailySession.value = withoutRetiredGames(session)
    }
  }

  /**
   * Un camino de hoy guardado ANTES de retirar un juego (data/Models.kt, GameRegistry.retiredDomains) puede nombrarlo: cada juego retirado se cambia por otro de su
   * misma área que no esté ya en el camino (los completados y sus puntajes quedan como están). Sin juegos retirados, devuelve la sesión tal cual.
   */
  private suspend fun withoutRetiredGames(entity: DailySessionEntity): DailySessionState {
    val state = entity.toDomain()
    if (state.gameIds.none { GameRegistry.isRetired(it) }) return state
    val ids = state.gameIds.toMutableList()
    for (i in ids.indices) {
      val domain = GameRegistry.retiredDomains[ids[i]] ?: continue
      val pool = GameRegistry.allGames.filter { it.domain == domain && it.id !in ids }
      ids[i] = (pool.randomOrNull() ?: GameRegistry.allGames.first { it.id !in ids }).id
    }
    val fixed = entity.copy(gameIdsRaw = ids.joinToString(","))
    dailySessionDao.insertOrUpdate(fixed)
    return fixed.toDomain()
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

  /**
   * El camino de hoy (3 juegos de 3 áreas): por AVANCE REAL y con variedad, ver [DailyPath]. El nivel de cada área es el promedio de [Skill.progress] de sus juegos con rating guardado; sin partidas, el del punto de partida.
   * Se lee todo de la base (no de los StateFlow, que al arrancar todavía pueden estar vacíos) y el día se calcula UNA vez: queda guardado en Room.
   */
  private suspend fun pickSessionQueue(history: List<GamePlayResult>): List<String> {
    val profile = userProfileDao.getActiveProfileSync() ?: userProfileDao.getUserProfileSync()
    val age = profile?.toDomain()?.ageBand
    val ratings = gameProgressDao.getAllProgressSync().associate { it.gameId to it.ddaRating }
    val baselineNow = _baseline.value
    val areaLevel = DomainType.values().associateWith { domain ->
      val saved = GameRegistry.allGames.filter { it.domain == domain }
        .mapNotNull { g -> ratings[g.id]?.takeIf { it >= 0f }?.let { Skill.progress(g.id, it, age) } }
      when {
        saved.isNotEmpty() -> saved.average().toFloat()
        baselineNow != null -> baselineNow.domains[domain]?.let { Skill.progress(BaselinePlan.stepFor(domain).gameId, it, age) } ?: 0f
        else -> 0f
      }
    }
    val lastPlayedDay = history.groupBy { it.gameId }.mapValues { (_, rs) -> epochDay(rs.maxOf { it.timestamp }) }
    return DailyPath.pick(
      dateKey = getTodayDateKey(),
      areaLevel = areaLevel,
      goals = _goals.value,
      yesterday = pathOf(-1),
      dayBefore = pathOf(-2),
      lastPlayedDay = lastPlayedDay,
      today = epochDay(System.currentTimeMillis()),
      tutorialGames = com.example.bridge.UnityGameLauncher.TUTORIAL_GAMES
    )
  }

  /** El camino guardado hace [daysAgo] días (negativo: ayer = -1) o null si ese día no hubo. */
  private suspend fun pathOf(daysAgo: Int): List<String>? {
    val cal = java.util.Calendar.getInstance().apply { add(java.util.Calendar.DAY_OF_YEAR, daysAgo) }
    val key = synchronized(dateFormat) { dateFormat.format(cal.time) }
    return dailySessionDao.getDailySessionSync(key)?.toDomain()?.gameIds?.takeIf { it.isNotEmpty() }
  }

  /** Días enteros desde 1970 en la hora local de [millis]. */
  private fun epochDay(millis: Long): Long = (millis + java.util.TimeZone.getDefault().getOffset(millis)) / 86_400_000L

  /**
   * El día que se completa el Primer vuelo, el camino de ese día queda CUMPLIDO con el vuelo: cuenta como día jugado (la racha, la meta semanal y los desafíos de días salen de las partidas del vuelo, que ya se guardaron) sin sumar
   * puntos ni partidas. Un camino cumplido SIN puntajes es, justamente, el del Primer vuelo (Hoy lo dice). Si el camino de hoy ya se completó jugándolo, no se toca.
   */
  suspend fun completeTodayWithFlight() = withContext(Dispatchers.IO) {
    val today = getTodayDateKey()
    val current = dailySessionDao.getDailySessionSync(today)?.toDomain()
      ?: DailySessionState(dateKey = today, gameIds = pickSessionQueue(gameResultDao.getAllResultsSync().map { it.toDomain() }), completedCount = 0)
    if (current.scores.isNotEmpty()) return@withContext        // un camino ya jugado (en parte o completo) no se toca
    val entity = DailySessionEntity(dateKey = today, gameIdsRaw = current.gameIds.joinToString(","), completedCount = current.gameIds.size, scoresRaw = "")
    dailySessionDao.insertOrUpdate(entity)
    _dailySession.value = entity.toDomain()
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
    // El nivel 1-5 se ajusta siempre (el modo de dificultad de Ajustes se quitó el 28-sep).
    val isAdaptive = true

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

    // Avance (Skill.kt): solo lo mueven las partidas a tu medida y los Desafíos o Expertos superados.
    val age = activeProfile?.ageBand?.let { n -> AgeBand.entries.firstOrNull { it.name == n } }
    val modePassed = Skill.passed(result.gameId, result.playMode, age, result.modeHits, result.modeTrials)
    val counted = result.endRating != null && Skill.counts(result.playMode, modePassed)
    val newDdaRating = result.endRating?.let { Skill.nextRating(currentProgress.ddaRating, it, result.playMode, modePassed) }
      ?: currentProgress.ddaRating
    val expertUnlocked = modePassed && result.playMode == PlayMode.DESAFIO && result.gameId !in _skill.value.expertOpen
    if (counted) {
      saveSkill(_skill.value.after(result.gameId, true, modePassed && result.playMode == PlayMode.DESAFIO,
        Skill.progress(result.gameId, newDdaRating, age), result.timestamp))
    }

    // Con rating del DDA, el nivel 1-5 ES la etapa del avance (Inicio…Maestro, data/Skill.kt): una sola escala.
    // Sube solo si la partida contó; "¡Subes!" se celebra al pasar de etapa.
    if (result.endRating != null && counted) {
      val stageLevel = levelFromRating(Skill.progress(result.gameId, newDdaRating, age))
      didLevelUp = stageLevel > currentProgress.currentLevel
      newLevel = stageLevel
      newMastery = currentProgress.masteryStreak
    } else if (result.endRating != null) {
      didLevelUp = false
      newLevel = currentProgress.currentLevel
      newMastery = currentProgress.masteryStreak
    }

    val updatedProgress = currentProgress.copy(
      currentLevel = newLevel,
      masteryStreak = newMastery,
      eloRating = newRating,
      ddaRating = newDdaRating,
      highestScore = maxOf(currentProgress.highestScore, result.score),
      totalGamesPlayed = currentProgress.totalGamesPlayed + 1,
      lastPlayedTimestamp = result.timestamp
    )
    gameProgressDao.insertOrUpdate(updatedProgress)
    // El avance que muestran las cartas (mismo cálculo que NeuroVidaViewModel.gameProgress), para la semana de Hoy.
    if (result.endRating != null && counted) {
      logProgress(result.gameId, result.timestamp, Skill.progress(result.gameId, newDdaRating, age))
    } else if (result.endRating == null && result.gameId in _skill.value.measured) {
      logProgress(result.gameId, result.timestamp, (newLevel - 1) / 5f + 0.1f)
    }
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
      newAchievements = unlockNewAchievements(computeAchievementStats(allResults, ratingsAfter), result.timestamp),
      modePassed = modePassed,
      expertUnlocked = expertUnlocked
    )
    recordLeagueEvents(outcome, result.gameId, result.timestamp)
    // Las marcas se comparan solo entre partidas a tu medida (docs/dificultad-y-avance.md).
    if (result.playMode == PlayMode.A_TU_MEDIDA) recordStarMeasures(result)
    // La colección de palabras raras se llena en cualquier modo.
    recordWordCollection(result)
    recordUnclearSentences(result)
    // El atlas se llena en cualquier modo.
    recordAtlas(result)
    // Las palabras azules también, en cualquier modo.
    recordPunta(result)
    // El cohete de Engranajes también, en cualquier modo.
    recordEngranajes(result)
    // El récord de Bodega de carga también, en cualquier modo.
    recordBodega(result)
    // El récord de Constelaciones también, en cualquier modo.
    recordConstelaciones(result)
    // El récord de La estación de correo también, en cualquier modo.
    recordCorreo(result)
    // El récord de Satélites también, en cualquier modo.
    recordSatelites(result)
    recordRescate(result)
    // El récord de Acoplamiento y sus totales también, en cualquier modo.
    recordAcoplamiento(result)
    // Las cúpulas de Aterrizaje también, en cualquier modo.
    recordAterrizaje(result)
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
    measurePrefs.edit().clear().apply()
    _starMeasures.value = emptyList()
    wordPrefs.edit().clear().apply()
    _wordCollection.value = emptyList()
    atlasPrefs.edit().clear().apply()
    _atlas.value = AtlasState()
    puntaPrefs.edit().clear().apply()
    _puntaPending.value = emptyList()
    rocketPrefs.edit().clear().apply()
    _engranajesRocket.value = Engranajes.rocket(0, 0)
    bodegaPrefs.edit().clear().apply()
    _bodegaRecord.value = 0
    constelacionesPrefs.edit().clear().apply()
    _constelacionesRecord.value = 0
    correoPrefs.edit().clear().apply()
    _correoRecord.value = 0
    satelitesPrefs.edit().clear().apply()
    _satelitesRecord.value = 0
    rescatePrefs.edit().clear().apply()
    _rescateRecord.value = 0
    _rescateTotals.value = Rescate.Totals()
    acoplamientoPrefs.edit().clear().apply()
    _acoplamientoRecord.value = 0
    _acoplamientoTotals.value = Acoplamiento.Totals()
    aterrizajePrefs.edit().clear().apply()
    _aterrizajeTotals.value = Aterrizaje.Totals()
    retiredMissionPrefs.edit().clear().apply()
    unclearPrefs.edit().clear().apply()
    _unclearSentences.value = emptyList()
    skillPrefs.edit().clear().putString("state", "").apply()
    _skill.value = SkillState()
    progressPrefs.edit().clear().apply()
    _progressLog.value = emptyList()
    profilePrefs.edit().clear().apply()
    _retoChoices.value = emptyMap()
    _education.value = null
    _goals.value = emptySet()
    _baseline.value = null
    _resultFocus.value = ResultFocus.DEFAULT
    _colorVision.value = ColorVision.DEFAULT
    initializeDatabaseDefaults()
  }
}
