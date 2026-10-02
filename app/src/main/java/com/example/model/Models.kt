package com.example.model

import androidx.compose.ui.graphics.Color
import com.example.ui.theme.*

enum class DomainType(
  val displayName: String,
  val tagline: String,
  val description: String,
  val color: Color
) {
  MEMORIA("Memoria", "recordar y ubicar", "Retención visual, espacial y secuencial", DomainMemoria),
  ATENCION("Atención", "foco y velocidad", "Foco, control y velocidad de procesamiento", DomainAtencion),
  RAZONAMIENTO("Razonamiento", "lógica y números", "Lógica, espacio y números", DomainRazonamiento),
  LENGUAJE("Lenguaje", "palabras y letras", "Fluidez verbal, léxico y ortografía", DomainLenguaje);

  companion object {
    /**
     * Lee el nombre guardado de un área. Hasta el 29-sep había 6 áreas: Velocidad se unió a Atención y Cálculo a
     * Razonamiento (30-sep); lo guardado con los nombres viejos se sigue entendiendo. Desconocido = null.
     */
    fun fromStored(name: String?): DomainType? = when (val n = name?.trim()) {
      "VELOCIDAD" -> ATENCION
      "CALCULO" -> RAZONAMIENTO
      else -> entries.firstOrNull { it.name == n }
    }
  }
}

/** El nivel 1-5 de cada juego ES la etapa del avance (data/Skill.kt): mismos nombres en toda la app. */
enum class LevelTier(val tierName: String, val levelNumber: Int) {
  PRINCIPIANTE("Inicio", 1),
  INICIADO("Aprendiz", 2),
  INTERMEDIO("Hábil", 3),
  AVANZADO("Experto", 4),
  EXPERTO("Maestro", 5);

  companion object {
    fun fromLevel(lvl: Int): LevelTier {
      return when (lvl.coerceIn(1, 5)) {
        1 -> PRINCIPIANTE
        2 -> INICIADO
        3 -> INTERMEDIO
        4 -> AVANZADO
        else -> EXPERTO
      }
    }
  }
}

data class GameDefinition(
  val id: String,
  val title: String,
  val domain: DomainType,
  val subtitle: String,
  val instruction: String,
  val iconEmoji: String
)

object GameRegistry {
  val allGames = listOf(
    GameDefinition(
      id = "parejas",
      title = "Parejas Ocultas",
      domain = DomainType.MEMORIA,
      subtitle = "Memoria de trabajo visual",
      instruction = "Memoriza dónde está cada figura antes de que las cartas se den vuelta y encuentra todas las parejas.",
      iconEmoji = "🃏"
    ),
    GameDefinition(
      id = "secuencia",
      title = "Secuencia Lumínica",
      domain = DomainType.MEMORIA,
      subtitle = "Memoria secuencial a corto plazo",
      instruction = "Mira el orden en que se encienden las fichas y repítelo tocándolas en el mismo orden.",
      iconEmoji = "💡"
    ),
    GameDefinition(
      id = "rutatesoro",
      title = "Ruta del Tesoro",
      domain = DomainType.MEMORIA,
      subtitle = "Memoria visoespacial",
      instruction = "Memoriza dónde aparecen los tesoros en el mapa y encuéntralos todos cuando se escondan.",
      iconEmoji = "💎"
    ),
    GameDefinition(
      id = "stroop",
      title = "Tinta o Palabra",
      domain = DomainType.ATENCION,
      subtitle = "Efecto Stroop & Inhibición",
      instruction = "Responde según la regla del cartel: el color de la TINTA o lo que dice la PALABRA. Atento: la regla cambia.",
      iconEmoji = "🎨"
    ),
    GameDefinition(
      id = "cambiochip",
      title = "Cambio de Chip",
      domain = DomainType.ATENCION,
      subtitle = "Flexibilidad cognitiva",
      instruction = "Sigue la regla del cartel: responde hacia dónde apunta la nave (DIRECCIÓN) o en qué borde está (POSICIÓN).",
      iconEmoji = "🔄"
    ),
    GameDefinition(
      id = "series",
      title = "Detective de Series",
      domain = DomainType.RAZONAMIENTO,
      subtitle = "Lógica secuencial",
      instruction = "Descubre la regla que siguen los números y elige el que continúa la serie.",
      iconEmoji = "🔍"
    ),
    GameDefinition(
      id = "acoplamiento",
      title = "Acoplamiento",
      domain = DomainType.RAZONAMIENTO,
      subtitle = "Razonamiento espacial: rotación mental",
      instruction = "Llega un módulo girado: ¿encaja en el puerto o es su reflejo en espejo? Gíralo en tu mente y decide.",
      iconEmoji = "🧩"
    ),
    GameDefinition(
      id = "trafico",
      title = "Tráfico Estelar",
      domain = DomainType.RAZONAMIENTO,
      subtitle = "Planificación y atención dividida",
      instruction = "Del portal salen cápsulas de colores. Toca los desvíos para que cada una llegue al planeta de su color y su símbolo. Anticípate: cada vez llegan más.",
      iconEmoji = "🚦"
    ),
    GameDefinition(
      id = "bitacora",
      title = "Bitácora de Misión",
      domain = DomainType.MEMORIA,
      subtitle = "Memoria de lo vivido: qué, dónde y en qué orden",
      instruction = "Llega una transmisión: una sonda deja hallazgos en planetas. Guárdalos en la bitácora y, más tarde, informa qué había en cada planeta y en qué orden pasó.",
      iconEmoji = "📡"
    ),
    GameDefinition(
      id = "cosecha",
      title = "Cosecha de palabras",
      domain = DomainType.LENGUAJE,
      subtitle = "Forma palabras y haz crecer tu huerto",
      instruction = "Siete letras giran alrededor de un planeta. Toca letras en orden para formar palabras de 3 letras o más y toca «Sembrar»: cada palabra brota como una planta. Si usas las 7 letras, nace un árbol dorado. Si te atascas, Nubi te da una pista.",
      iconEmoji = "🌱"
    ),
    GameDefinition(
      id = "rumbo",
      title = "Rumbo a Casa",
      domain = DomainType.MEMORIA,
      subtitle = "Orientación: volver a casa sin mapa",
      instruction = "Toca las señales para ir de cristal en cristal: el espacio gira a tu alrededor. Al final, apunta hacia tu base y avanza hasta donde creas que está. Recuerda cada giro.",
      iconEmoji = "🧭"
    ),
    GameDefinition(
      id = "correo",
      title = "Correo Estelar",
      domain = DomainType.MEMORIA,
      subtitle = "Memoria para lo pendiente: acordarte a tiempo",
      instruction = "Guía la nave y recoge sobres. Antes de salir recibes encargos: tocar los planetas de un color cuando pasen y avisar por radio cada cierto tiempo. Durante el vuelo nadie te los recuerda.",
      iconEmoji = "✉️"
    ),
    GameDefinition(
      id = "anagramas",
      title = "Anagramas",
      domain = DomainType.LENGUAJE,
      subtitle = "Léxico y procesamiento fonológico",
      instruction = "Ordena las letras para formar la palabra escondida. Si te trabas, puedes pedir una pista.",
      iconEmoji = "🔤"
    ),
    GameDefinition(
      id = "meteoros",
      title = "Lluvia de meteoros",
      domain = DomainType.LENGUAJE,
      subtitle = "Toca solo las palabras que existen",
      instruction = "Caen meteoros con palabras. Toca las que existen y deja pasar las inventadas: no pierdes nada si dudas. Las palabras raras valen más.",
      iconEmoji = "☄️"
    ),
    GameDefinition(
      id = "disparate",
      title = "¿Verdad o disparate?",
      domain = DomainType.LENGUAJE,
      subtitle = "Lee y decide rápido",
      instruction = "Llegan frases cortas desde la radio. Toca VERDAD si es cierta o DISPARATE si no tiene sentido; también puedes deslizar la frase. Si una frase no se entiende, mantenla presionada.",
      iconEmoji = "📡"
    ),
    GameDefinition(
      id = "calculo",
      title = "Cálculo Sereno",
      domain = DomainType.RAZONAMIENTO,
      subtitle = "Aritmética mental",
      instruction = "Resuelve cada cuenta y toca el resultado. En modo Reto, antes de que la burbuja llegue al agua.",
      iconEmoji = "🧮"
    ),
    GameDefinition(
      id = "piloto",
      title = "Piloto Estelar",
      domain = DomainType.ATENCION,
      subtitle = "Multitarea y atención dividida",
      instruction = "Guía la nave por la ruta de luces con un pulgar y, con el otro dedo, atrapa solo las señales de tu misión. Empieza en piloto automático.",
      iconEmoji = "🚀"
    ),
    GameDefinition(
      id = "freno",
      title = "Freno de Emergencia",
      domain = DomainType.ATENCION,
      subtitle = "Control inhibitorio: frenar a tiempo",
      instruction = "Lanza lo más rápido que puedas el cohete que se enciende. Si suena la alarma y aparece ¡ALTO!, no toques: frena a tiempo.",
      iconEmoji = "🛑"
    ),
    GameDefinition(
      id = "satelites",
      title = "Satélites",
      domain = DomainType.ATENCION,
      subtitle = "Seguimiento de varios objetos a la vez",
      instruction = "Algunos satélites encienden su señal y se apagan. Todos se mueven y se cruzan: síguelos con la vista y, cuando se detengan, toca los que brillaban.",
      iconEmoji = "🛰️"
    ),
    GameDefinition(
      id = "radar",
      title = "Radar",
      domain = DomainType.ATENCION,
      subtitle = "Velocidad de procesamiento y visión periférica",
      instruction = "Atento al radar: en un destello aparecen varios astronautas perdidos. Toca todos los lugares donde los viste y pulsa ¡Rescatar! Los robots no se rescatan.",
      iconEmoji = "📡"
    ),
    GameDefinition(
      id = "aterrizaje",
      title = "Aterrizaje Lunar",
      domain = DomainType.RAZONAMIENTO,
      subtitle = "Sentido numérico: estimar en la línea numérica",
      instruction = "Posa la nave justo en el número de la misión. La regla solo tiene marcados los extremos: arrastra para mover la nave y suelta para aterrizar.",
      iconEmoji = "🌙"
    ),
    GameDefinition(
      id = "comparacion",
      title = "Comparación Instantánea",
      domain = DomainType.ATENCION,
      subtitle = "Velocidad perceptiva",
      instruction = "Elige la tarjeta que vale más: la de más puntos, el número mayor o la cuenta con mayor resultado.",
      iconEmoji = "⚡"
    )
  )

  fun getById(id: String): GameDefinition? = allGames.find { it.id == id }
}

data class GamePlayResult(
  val id: String = java.util.UUID.randomUUID().toString(),
  val gameId: String,
  val score: Int, // 0 to 100
  val correctAnswers: Int,
  val totalTrials: Int,
  val timed: Boolean,
  val level: Int,
  val timestamp: Long = System.currentTimeMillis(),
  // Rating final del DDA común (0..1) informado por los juegos Unity; null en los demás.
  val endRating: Float? = null,
  // Cómo se eligió jugar (ver data/Skill.kt) y ensayos/aciertos después del calentamiento, para decidir si un
  // Desafío o un Experto se superó. No se guardan en Room.
  val playMode: com.example.data.PlayMode = com.example.data.PlayMode.A_TU_MEDIDA,
  val modeTrials: Int = 0,
  val modeHits: Int = 0,
  // Solo Piloto Estelar: costo de multitarea en % de esta partida (se muestra en el resultado; no se guarda en Room).
  val multitaskCost: Int? = null,
  // Solo Radar (Rescate relámpago): "tu vistazo" en ms (destello en el que se asentó la dificultad) y cuántos
  // astronautas había en esas rondas; rescatados / mostrados por dirección (8, 0 = arriba y en sentido horario) y
  // cerca / lejos del centro (2) para "tu radar"; "tu captura" (cuántos de un vistazo, en las lluvias) y robots
  // mostrados / tocados ("tu filtro"). No se guardan en Room.
  val glanceMs: Int? = null,
  val glanceLoad: Float? = null,
  val sectorHits: List<Int>? = null,
  val sectorTrials: List<Int>? = null,
  val ringHits: List<Int>? = null,
  val ringTrials: List<Int>? = null,
  val captureK: Float? = null,
  val robotsShown: Int? = null,
  val robotsTouched: Int? = null,
  // Solo Satélites: cuántos se siguen de verdad a la vez (descontando la suerte) y la velocidad más alta superada
  // completa (múltiplo de la del nivel 1). No se guardan en Room.
  val trackingCapacity: Float? = null,
  /** Satélites: cuántos había que seguir por ronda, en promedio (techo de trackingCapacity en esa partida). */
  val trackingTargets: Float? = null,
  val trackingSpeed: Float? = null,
  // Solo Freno de Emergencia: "tu freno" (tiempo de frenado, ms), altos frenados / totales y el alto más tardío
  // que se frenó (ms). No se guardan en Room.
  val brakeMs: Int? = null,
  val stopsOk: Int? = null,
  val stopsTotal: Int? = null,
  val brakeBestSsdMs: Int? = null,
  // Solo Aterrizaje Lunar: error medio (% del largo de la regla), y por aterrizaje dónde estaba el blanco y dónde se
  // posó (0..1), para dibujar "tu línea". No se guardan en Room.
  val numlineErrorPct: Float? = null,
  val numlineTrue: List<Float>? = null,
  val numlineGiven: List<Float>? = null,
  val numlineBullseyes: Int? = null,
  // Solo Acoplamiento: "tu giro mental" (grados por segundo) y "tu curva de giro" (ms medios a 0/45/90/135/180°;
  // null = sin datos en esa columna). No se guardan en Room.
  val rotationSpeedDps: Int? = null,
  val rotationCurveMs: List<Int?>? = null,
  // Solo Tráfico Estelar: "tu anticipación" (mediana, ms), % de desvíos preparados con tiempo y más cápsulas a la
  // vez. No se guardan en Room.
  val trafficLeadMs: Int? = null,
  val trafficProactivePct: Int? = null,
  val trafficPeakPods: Int? = null,
  // Solo Bitácora de Misión: fase ("" completa, "encode" transmisión, "recall" informe), semilla y nivel de la misión,
  // paradas, aprendidas en el primer repaso (y cuáles, en bits), recordadas en el informe (y cuáles), hallazgos
  // elegidos que no estaban, paradas de la ruta en su lugar y segundos de demora. La app completa la retención y la
  // colección (ver data/MissionLog.kt). No se guardan en Room.
  val memPhase: String? = null,
  val memSeed: Int? = null,
  val memLevel: Int? = null,
  val memItems: Int? = null,
  val memLearned: Int? = null,
  val memLearnedMask: Int? = null,
  val memRecalled: Int? = null,
  val memRecalledMask: Int? = null,
  val memIntrusions: Int? = null,
  val memOrderOk: Int? = null,
  val memDelayS: Int? = null,
  val memRetentionPct: Int? = null,
  val memArchivedTotal: Int? = null,
  // Solo Rumbo a Casa: "tu brújula interna" (a qué distancia de casa quedaste, en % de la distancia que había), dónde
  // quedó cada vuelta en el marco de la vuelta justa (en fracciones de esa distancia: la base en along = 1,
  // lateral = 0; lateral + = a la derecha), si el viaje tenía faro y llegadas perfectas. La lectura está en
  // data/Homing.kt. No se guardan en Room.
  val homingErrorPct: Float? = null,
  val homingAlong: List<Float>? = null,
  val homingLateral: List<Float>? = null,
  val homingBeacon: List<Boolean>? = null,
  val homingPerfect: Int? = null,
  // Solo Correo Estelar: encargos por lugar (planetas entregados de los que pasaron), planetas tocados por error (y de
  // color parecido), encargos por hora (avisos por radio a tiempo, a destiempo, período), cómo se usó el reloj (miradas y
  // cuántas justo antes de la hora), % en la ruta y sobres. La lectura está en data/Mail.kt. No se guardan en Room.
  val mailEventHits: Int? = null,
  val mailEventTotal: Int? = null,
  val mailCommissions: Int? = null,
  val mailLureCommissions: Int? = null,
  val mailRadioHits: Int? = null,
  val mailRadioTotal: Int? = null,
  val mailRadioOfftime: Int? = null,
  val mailRadioPeriodS: Int? = null,
  val mailClockChecks: Int? = null,
  val mailClockLate: Int? = null,
  val mailLanePct: Int? = null,
  val mailEnvelopes: Int? = null,
  val mailAsteroidHits: Int? = null,
  val mailAsteroids: Int? = null,
  val mailHullIntactPct: Int? = null,
  val mailEmergencies: Int? = null,
  // Solo Lluvia de meteoros: palabras reales vistas y tocadas por banda (6, de la común a la rara), inventadas vistas y
  // tocadas por tipo (3: obvia, una letra, letras traspuestas), mediana del tiempo de toque en comunes y raras (ms) y
  // las palabras raras acertadas. No se guardan en Room.
  val lexBandSeen: List<Int>? = null,
  val lexBandHits: List<Int>? = null,
  val lexFaSeen: List<Int>? = null,
  val lexFaHits: List<Int>? = null,
  val lexRtCommonMs: Int? = null,
  val lexRtRareMs: Int? = null,
  val lexRareWords: List<String>? = null,
  // Solo ¿Verdad o disparate?: palabras por minuto leyendo y decidiendo, tiempo medio (ms), aciertos y frases vistas por tipo
  // (6: corta, con complemento, negación, con pausa, todos/algunos/ningún, comparación), disparates evidentes y sutiles,
  // mejor racha y ids de las frases marcadas como "no está clara". No se guardan en Room.
  val svWpm: Int? = null,
  val svRtType: List<Int>? = null,
  val svHitsType: List<Int>? = null,
  val svSeenType: List<Int>? = null,
  val svEvidentHits: Int? = null,
  val svEvidentSeen: Int? = null,
  val svSubtleHits: Int? = null,
  val svSubtleSeen: Int? = null,
  val svBestStreak: Int? = null,
  val svUnclear: List<String>? = null,
  // Solo Cosecha de palabras: palabras de las 3 cosechas (sin las ocultas), de las comunes cuántas se encontraron y cuántas había,
  // % de palabras en racimo (sin dato con menos de 10), palabras en los primeros y en los últimos 20 s (promedio de las cosechas),
  // la palabra estrella encontrada, la más larga o rara, hasta 5 comunes que faltaron y las pistas usadas. No se guardan en Room.
  val harvWords: Int? = null,
  val harvCommonFound: Int? = null,
  val harvCommonTotal: Int? = null,
  val harvClusterPct: Int? = null,
  val harvFirst20: Int? = null,
  val harvLast20: Int? = null,
  val harvStar: String? = null,
  val harvBest: String? = null,
  val harvMissed: List<String>? = null,
  val harvHints: Int? = null
)

data class DailySessionState(
  val dateKey: String,
  val gameIds: List<String>,
  val completedCount: Int = 0,
  val scores: List<Int> = emptyList()
)


enum class DifficultyMode(val label: String, val description: String) {
  ADAPTIVE("Auto-adaptativa", "Ajuste dinámico según tu desempeño en cada sesión"),
  PRINCIPIANTE("Principiante", "Nivel 1 con tiempos generosos y estímulos claros"),
  INTERMEDIO("Intermedio", "Nivel 3 equilibrado para mantener agilidad mental"),
  AVANZADO("Avanzado", "Nivel 5 con máxima exigencia cognitiva y velocidad"),
  CUSTOM("Personalizada", "Nivel asignado a medida por cada dominio cognitivo")
}

enum class ThemeMode(val label: String) {
  LIGHT("Claro"),
  DARK("Oscuro"),
  SYSTEM("Sistema")
}

/**
 * Rango de edad: calibra el DDA de los juegos (Unity lo recibe como `age_band`) y la comparación del punto de
 * partida. `null` en [UserSettings.ageBand] significa "todavía no se preguntó": abre el onboarding.
 */
enum class AgeBand(val label: String) {
  UNDER_18("Menos de 18"),
  ADULT("18 a 64"),
  SENIOR("65 o más")
}

/**
 * Maestría por dominio (meta-progresión, etapa 4 de la propuesta de gamificación):
 * a diferencia del nivel de cada juego (tope visible en 5), la XP de dominio no
 * tiene techo — sigue dando sensación de avance aunque todos los juegos de ese
 * dominio ya estén en Experto. Un dominio suma XP jugando CUALQUIER juego suyo.
 */
enum class MasteryTier(val tierName: String, val minXp: Int, val icon: String) {
  BRONCE("Bronce", 0, "🥉"),
  PLATA("Plata", 100, "🥈"),
  ORO("Oro", 300, "🥇"),
  PLATINO("Platino", 600, "💠"),
  DIAMANTE("Diamante", 1000, "💎"),
  MAESTRO("Maestro", 2000, "👑");

  companion object {
    fun fromXp(xp: Int): MasteryTier = entries.lastOrNull { xp >= it.minXp } ?: BRONCE
  }
}

data class DomainMasteryInfo(
  val domain: DomainType,
  val xp: Int
) {
  val tier: MasteryTier get() = MasteryTier.fromXp(xp)
  private val nextTier: MasteryTier? get() = MasteryTier.entries.firstOrNull { it.minXp > xp }
  /** Progreso 0f..1f dentro del tramo actual (para la barra). Si ya es Maestro,
   * sigue avanzando en tramos de 1000 XP para que la barra nunca quede "llena y
   * quieta" — el tope real no existe, solo cambia el tamaño del próximo tramo. */
  val progressInTier: Float get() {
    val next = nextTier
    return if (next != null) {
      ((xp - tier.minXp).toFloat() / (next.minXp - tier.minXp)).coerceIn(0f, 1f)
    } else {
      ((xp - tier.minXp) % 1000) / 1000f
    }
  }
  val xpLabel: String get() {
    val next = nextTier
    return if (next != null) "$xp / ${next.minXp} XP" else "$xp XP"
  }
}

/**
 * Desafíos semanales (etapa 4): fijos y deterministas — no aleatorios — para dar
 * "variedad forzada con motivo". El progreso se calcula en vivo desde el
 * historial de la semana (no se persiste), solo se guarda si ya se reclamó el
 * premio para no volver a otorgarlo al recalcular.
 */
data class WeeklyChallengeDef(
  val key: String,
  val title: String,
  val description: String,
  val iconEmoji: String,
  val target: Int
)

object WeeklyChallengeRegistry {
  val all = listOf(
    WeeklyChallengeDef("dominios3", "Entrenamiento variado", "Jugá en 3 dominios cognitivos distintos esta semana", "🧭", 3),
    WeeklyChallengeDef("reto2", "Modo Reto", "Completá 2 partidas en modo Reto esta semana", "⚡", 2),
    WeeklyChallengeDef("precision3", "Racha de precisión", "Conseguí 85 puntos o más en 3 partidas esta semana", "🎯", 3),
    WeeklyChallengeDef("dias4", "Constancia", "Entrená en 4 días distintos esta semana", "📅", 4)
  )
  const val XP_REWARD_PER_DOMAIN = 10
}

data class WeeklyChallengeProgress(
  val def: WeeklyChallengeDef,
  val progress: Int,
  val claimed: Boolean
) {
  val isComplete: Boolean get() = progress >= def.target
}

/**
 * Ranking tipo ELO por juego (idea de Ricardo, 20-sep): a diferencia de Maestría por
 * Dominio (XP que solo crece), el rating de un juego sube Y baja según el desempeño
 * de cada partida — da la sensación de "rango" que se puede perder, como en un
 * ladder competitivo. Es puramente un layer de progreso/motivación: no reemplaza
 * ni modifica el nivel de dificultad (1-5) ni masteryStreak, que siguen intactos.
 */
enum class RankTier(val tierName: String, val minRating: Int, val icon: String, val color: Color) {
  BRONCE("Bronce", 0, "🥉", Color(0xFFCD7F32)),
  PLATA("Plata", 250, "🥈", Color(0xFF9CA3AF)),
  ORO("Oro", 500, "🥇", Color(0xFFF59E0B)),
  PLATINO("Platino", 750, "💠", Color(0xFF22D3EE)),
  ESMERALDA("Esmeralda", 1000, "🟢", Color(0xFF17C97A)),
  DIAMANTE("Diamante", 1250, "💎", Color(0xFF60A5FA)),
  MAESTRO("Maestro", 1500, "👑", Color(0xFFA855F7));

  companion object {
    const val DIVISION_SIZE = 50
    fun fromRating(rating: Int): RankTier = entries.lastOrNull { rating >= it.minRating } ?: BRONCE
  }
}

/**
 * Lo que dejó guardar una partida ([com.example.data.NeuroVidaRepository.recordGameResult]): si subió de nivel y
 * los trofeos antes/después, del juego y de la liga general (promedio de todos los juegos, sin jugar = 0).
 */
data class RecordOutcome(
  val didLevelUp: Boolean,
  val gameRatingBefore: Int,
  val gameRatingAfter: Int,
  val globalBefore: Int,
  val globalAfter: Int,
  /** Logros conseguidos con esta partida (ids de [com.example.data.Achievements]), en orden de catálogo. */
  val newAchievements: List<String> = emptyList(),
  /** Desafío o Experto superado en esta partida (ver data/Skill.kt): el avance subió. */
  val modePassed: Boolean = false,
  /** Primer Desafío superado en ese juego: se abrió Experto. */
  val expertUnlocked: Boolean = false
) {
  /** Ascenso de liga para celebrar: primero la liga general (más rara y más importante), si no la del juego. */
  fun promotion(gameId: String): LeaguePromotion? = globalPromotion() ?: gamePromotion(gameId)

  fun globalPromotion(): LeaguePromotion? {
    val before = RankTier.fromRating(globalBefore)
    val after = RankTier.fromRating(globalAfter)
    return if (after.ordinal > before.ordinal) LeaguePromotion(after, before, gameId = null, rating = globalAfter) else null
  }

  fun gamePromotion(gameId: String): LeaguePromotion? {
    val before = RankTier.fromRating(gameRatingBefore)
    val after = RankTier.fromRating(gameRatingAfter)
    return if (after.ordinal > before.ordinal) LeaguePromotion(after, before, gameId = gameId, rating = gameRatingAfter) else null
  }
}

/** Subida a una liga nueva: [gameId] = liga de ese juego; null = liga general. */
data class LeaguePromotion(val tier: RankTier, val previous: RankTier, val gameId: String?, val rating: Int)

data class GameRankInfo(
  val gameId: String,
  val rating: Int
) {
  val tier: RankTier get() = RankTier.fromRating(rating)
  private val intoTier: Int get() = rating - tier.minRating
  /** 1 (a punto de ascender) a 5 (recién ascendido) — como en un ladder competitivo.
   * Maestro no tiene divisiones, sigue subiendo sin techo (mismo criterio que
   * MasteryTier/masteryStreak: el ladder nunca "se llena y queda quieto"). */
  val division: Int? get() =
    if (tier == RankTier.MAESTRO) null
    else (5 - (intoTier / RankTier.DIVISION_SIZE)).coerceIn(1, 5)
  val label: String get() =
    if (tier == RankTier.MAESTRO) "${tier.tierName} · $rating" else "${tier.tierName} $division"
}

data class UserSettings(
  val id: Long = 1L,
  val name: String = "",
  val avatar: String = "🧠",
  val isActive: Boolean = true,
  val weeklyGoal: Int = 4, // days per week
  val defaultTimed: Boolean = false, // false: Precisión, true: Reto
  val soundEnabled: Boolean = true,
  val hapticsEnabled: Boolean = true,
  val notificationsEnabled: Boolean = true,
  val reminderHour: Int = 19, // 19:00 (7 PM default)
  val reminderMinute: Int = 0,
  // Difficulty and cognitive customization
  val difficultyMode: DifficultyMode = DifficultyMode.ADAPTIVE,
  val difficultyMemoria: Int = 2,
  val difficultyAtencion: Int = 2,
  val difficultyRazonamiento: Int = 2,
  val difficultyLenguaje: Int = 2,
  val difficultyCalculo: Int = 2,
  val difficultyVelocidad: Int = 2,
  val cognitiveAssistance: Boolean = true,
  val timeScaleFactor: Float = 1.0f,
  val themeMode: ThemeMode = ThemeMode.SYSTEM,
  val language: AppLanguage = AppLanguage.SPANISH,
  /** `null` = todavía no completó el onboarding de edad -- ver [AgeBand]. */
  val ageBand: AgeBand? = null
)
