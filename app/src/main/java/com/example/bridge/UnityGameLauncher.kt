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

  /** Juegos con tutorial guiado (ronda guiada propia) y «Cómo se juega» en la pausa. Rastro de luz fue el primero; Freno, Aterrizaje y Meteoros se sumaron el 3-oct
   *  para el inicio nuevo («Primer vuelo con Nubi»). Para sumar otro juego: ver docs/diseno-rastro-de-luz.md. */
  val TUTORIAL_GAMES = setOf("secuencia", "freno", "aterrizaje", "meteoros", "stroop", "anagramas", "calculo", "engranajes", "bodega", "parejas", "correo", "cosecha", "disparate", "intrusa", "satelites", "piloto", "radar")

  /** La app manda `show_tutorial` si el juego tiene tutorial y la persona NO tiene partidas de él en el historial (la ronda guiada no se guarda). */
  fun shouldShowTutorial(gameId: String, history: List<com.example.model.GamePlayResult>): Boolean =
    gameId in TUTORIAL_GAMES && history.none { it.gameId == gameId }

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
    // Cómo se eligió jugar (data/Skill.kt): techo (Suave) o piso (Desafío, Experto) sobre el rating 0..1; -1 = sin límite.
    // "Quitar animaciones" del teléfono: Unity apaga los adornos que se mueven solos (respiración, destellos).
    val reduce_motion: Boolean = false,
    // Tutorial guiado común (Games/Shared/GuidedTutorial.cs): la persona nunca jugó este juego. Hoy solo Rastro de luz lo usa.
    val show_tutorial: Boolean = false,
    // SOLO depuración (botones [Debug]): Unity dibuja encima del tutorial los rectángulos del foco y el último toque, para mandar una captura de lo que pasa en el teléfono.
    val debug_overlay: Boolean = false,
    // Solo «En la punta de la lengua» (anagramas): las palabras que Nubi mostró en partidas anteriores («las azules»), separadas por «;». Vuelven en otra partida.
    val punta_pending: String = "",
    // Solo «Engranajes»: las luces del cohete que la persona lleva (0..9) y los cohetes que ya despegaron (son progreso: la app los guarda y Unity devuelve los nuevos).
    val engr_lights: Int = 0,
    val engr_orbit: Int = 0,
    // Solo «Bodega de carga»: el récord de la persona (la bodega más grande que recordó sin errores, en objetos): es progreso, la app lo guarda y Unity devuelve el nuevo.
    val bod_best: Int = 0,
    // Solo «Constelaciones» (id parejas): el récord de la persona (su racha de memoria más larga; progreso: la app lo guarda y Unity devuelve el nuevo) y, solo en las herramientas de prueba, la etapa (1..18) con que
    // empieza el primer cielo (0 = la que corresponde).
    val con_best: Int = 0,
    val con_stage: Int = 0,
    // Solo «La estación de correo» (id correo): el récord de la persona (cartas bien puestas en un día perfecto; es progreso: la app lo guarda y Unity devuelve el nuevo) y, solo en las herramientas de prueba, la etapa (1..10) con que
    // empieza la partida (0 = la que corresponde).
    val mail_best: Int = 0,
    val mail_stage: Int = 0,
    // Solo «Satélites: enciende tu planeta» (id satelites): el récord de luces en una partida (progreso: la app lo guarda y Unity devuelve el nuevo) y, solo en las herramientas de prueba, el nivel (1..12) con que empieza la partida (0 = el que
    // corresponde) y la sorpresa que sale en todas las rondas (1 órbita, 2 nube, 4 rápidas; 0 = las de siempre).
    val sat_best: Int = 0,
    val sat_stage: Int = 0,
    val sat_surprise: Int = 0,
    // Solo «Piloto Estelar: la ruta de las balizas» (id piloto), solo en las herramientas de prueba: el nivel (1..9) con que empiezan los dos motores (0 = el que corresponde) y cada cuántos segundos cambia el sector (0 = 30; con 5, el
    // sector 2 llega a los 5 s y el vuelo dura 15).
    val pil_stage: Int = 0,
    val pil_sector_s: Int = 0,
    // Solo «Rescate relámpago» (id radar): el récord de cápsulas rescatadas en una partida (progreso: la app lo guarda y Unity devuelve el nuevo) y, solo en las herramientas de prueba, el nivel (1..12) con que empieza la partida (0 = el que corresponde).
    val resc_best: Int = 0,
    val resc_stage: Int = 0,
    val play_mode: String = "",
    val mode_floor: Float = -1f,
    val mode_ceiling: Float = -1f
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
    mode: com.example.data.PlayMode = com.example.data.PlayMode.A_TU_MEDIDA,
    forceTutorial: Boolean = false, // solo las herramientas de prueba: abre el tutorial aunque la persona ya haya jugado
    debugOverlay: Boolean = false,  // solo las herramientas de prueba: Unity dibuja los rectángulos del tutorial y el último toque
    conStage: Int = 0,              // solo las herramientas de prueba: Constelaciones empieza en esta etapa (0 = normal)
    mailStage: Int = 0,             // solo las herramientas de prueba: La estación de correo empieza en esta etapa (0 = normal)
    satStage: Int = 0,              // solo las herramientas de prueba: Satélites empieza en este nivel (0 = normal)
    satSurprise: Int = 0,           // solo las herramientas de prueba: Satélites trae esta sorpresa en todas las rondas (1 órbita, 2 nube, 4 rápidas; 0 = las de siempre)
    pilStage: Int = 0,              // solo las herramientas de prueba: Piloto empieza en este nivel (0 = normal)
    pilSectorS: Int = 0,            // solo las herramientas de prueba: segundos por sector de Piloto (0 = 30)
    rescStage: Int = 0              // solo las herramientas de prueba: Rescate relámpago empieza en este nivel (0 = normal)
  ): Intent = buildIntent(context, userId, gameId, level, baseIntensity, timed, ageBand, soundEnabled, launchId, assessmentStep, assessmentTotal, mode, forceTutorial, debugOverlay, conStage, mailStage, satStage, satSurprise, pilStage, pilSectorS, rescStage)

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
    mode: com.example.data.PlayMode = com.example.data.PlayMode.A_TU_MEDIDA,
    forceTutorial: Boolean = false,
    debugOverlay: Boolean = false,
    conStage: Int = 0,
    mailStage: Int = 0,
    satStage: Int = 0,
    satSurprise: Int = 0,
    pilStage: Int = 0,
    pilSectorS: Int = 0,
    rescStage: Int = 0
  ): Intent {
    val assessment = assessmentStep > 0
    val savedRating = if (assessment) -1f else com.example.NeuroVidaApplication.instance.repository.gameDdaRating.value[gameId] ?: -1f
    val bounds = if (assessment) com.example.data.ModeBounds() else com.example.data.Skill.bounds(gameId, savedRating, mode, ageBand)
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
        reduce_motion = android.provider.Settings.Global.getFloat(
          context.contentResolver, android.provider.Settings.Global.ANIMATOR_DURATION_SCALE, 1f
        ) == 0f,
        show_tutorial = forceTutorial || shouldShowTutorial(gameId, com.example.NeuroVidaApplication.instance.repository.gameHistory.value),
        debug_overlay = debugOverlay,
        punta_pending = if (gameId == "anagramas") com.example.data.Punta.encode(com.example.NeuroVidaApplication.instance.repository.puntaPending.value) else "",
        engr_lights = if (gameId == "engranajes") com.example.NeuroVidaApplication.instance.repository.engranajesRocket.value.lights else 0,
        engr_orbit = if (gameId == "engranajes") com.example.NeuroVidaApplication.instance.repository.engranajesRocket.value.orbit else 0,
        bod_best = if (gameId == "bodega") com.example.NeuroVidaApplication.instance.repository.bodegaRecord.value else 0,
        con_best = if (gameId == "parejas") com.example.NeuroVidaApplication.instance.repository.constelacionesRecord.value else 0,
        con_stage = if (gameId == "parejas") conStage.coerceIn(0, 18) else 0,
        mail_best = if (gameId == "correo") com.example.NeuroVidaApplication.instance.repository.correoRecord.value else 0,
        mail_stage = if (gameId == "correo") mailStage.coerceIn(0, 10) else 0,
        sat_best = if (gameId == "satelites") com.example.NeuroVidaApplication.instance.repository.satelitesRecord.value else 0,
        sat_stage = if (gameId == "satelites") satStage.coerceIn(0, 12) else 0,
        sat_surprise = if (gameId == "satelites") satSurprise.coerceIn(0, 7) else 0,
        pil_stage = if (gameId == "piloto") pilStage.coerceIn(0, 9) else 0,
        pil_sector_s = if (gameId == "piloto") pilSectorS.coerceIn(0, 30) else 0,
        resc_best = if (gameId == "radar") com.example.NeuroVidaApplication.instance.repository.rescateRecord.value else 0,
        resc_stage = if (gameId == "radar") rescStage.coerceIn(0, 12) else 0,
        play_mode = if (assessment) "" else mode.name,
        mode_floor = bounds.floor ?: -1f,
        mode_ceiling = bounds.ceiling ?: -1f
      )
    )
    val json = adapter.toJson(config)

    return Intent(context, UnityPlayerGameActivity::class.java)
      .addFlags(Intent.FLAG_ACTIVITY_REORDER_TO_FRONT)
      .putExtra(EXTRA_CONFIG_JSON, json)
      .putExtra(EXTRA_LAUNCH_ID, launchId ?: java.util.UUID.randomUUID().toString())
  }
}
