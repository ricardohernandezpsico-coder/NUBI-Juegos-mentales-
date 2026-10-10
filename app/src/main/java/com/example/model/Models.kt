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
      // «Constelaciones» (7-oct): el juego renovado de Parejas Ocultas; el id «parejas» se mantiene (avance, marcas e historial).
      title = "Constelaciones",
      domain = DomainType.MEMORIA,
      subtitle = "Memoria de lugar",
      instruction = "Toca una luz para ver qué esconde y busca su pareja: las que son iguales se unen con una línea. Si te acuerdas de dónde estaba, ve directo a ella.",
      iconEmoji = "🌌"
    ),
    GameDefinition(
      // El id «secuencia» se mantiene (avance, marcas e historial); el nombre visible es el del juego rehecho (3-oct).
      id = "secuencia",
      title = "Rastro de luz",
      domain = DomainType.MEMORIA,
      subtitle = "Memoria de trabajo visoespacial",
      instruction = "Mira el camino que hace la chispa entre los luceros y repítelo con el dedo, en el mismo orden. Cada lucero suena con su nota.",
      iconEmoji = "🔮"
    ),
    GameDefinition(
      id = "bodega",
      title = "Bodega de carga",
      domain = DomainType.MEMORIA,
      subtitle = "Recuerda dónde guardó cada cosa el robot",
      instruction = "La carga entra por la esclusa y el robot la guarda en las escotillas de una bodega redonda. Mira bien dónde queda cada cosa: después te las piden una por una y tocas la escotilla que la tiene. A veces el robot cambia una caja de lugar y a veces la bodega gira.",
      iconEmoji = "📦"
    ),
    GameDefinition(
      id = "stroop",
      title = "Tinta o Palabra",
      domain = DomainType.ATENCION,
      subtitle = "Dos orillas, dos reglas",
      instruction = "La palabra llega por una orilla. Si viene de la orilla de la TINTA, toca el color con que está escrita; si viene de la orilla de la PALABRA, toca lo que dice. A veces cambia de orilla.",
      iconEmoji = "🎨"
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
      id = "cosecha",
      title = "Cosecha de palabras",
      domain = DomainType.LENGUAJE,
      subtitle = "Forma palabras y haz crecer tu huerto",
      instruction = "Siete letras giran alrededor de un planeta. Toca letras en orden para formar palabras de 3 letras o más y toca «Sembrar»: cada palabra brota como una planta. Si usas las 7 letras, nace un árbol dorado. Si te atascas, Nubi te da una pista.",
      iconEmoji = "🌱"
    ),
    GameDefinition(
      id = "intrusa",
      title = "La estrella intrusa",
      domain = DomainType.LENGUAJE,
      subtitle = "Cinco palabras, una no pertenece",
      instruction = "Cinco estrellas con una palabra: cuatro comparten algo y una no. Toca la intrusa. Después, una chispa dibuja la figura de lo que las une y la guardas en tu atlas. Cuidado: a veces la intrusa va muy bien con una de las otras.",
      iconEmoji = "⭐"
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
      // «La estación de correo» (8-oct): el vuelo se rehízo; el nombre y el id se mantienen (avance, marcas e historial).
      title = "Correo Estelar",
      domain = DomainType.MEMORIA,
      subtitle = "Memoria para lo pendiente: acordarte a tiempo",
      instruction = "Trabajas en la estación de correo: toca el buzón del sello de cada carta. Cada mañana recibes encargos (una carta con sello dorado o con lazo a la caja fuerte; encender el faro a una hora) y durante el día nadie te los recuerda.",
      iconEmoji = "✉️"
    ),
    GameDefinition(
      id = "anagramas",
      title = "En la punta de la lengua",
      domain = DomainType.LENGUAJE,
      subtitle = "Encuentra la palabra que ya sabes",
      instruction = "Nubi capta la definición de una palabra. Búscala en tu memoria; si la tienes, ordénala con las letras. Si no sale, pide una ayuda: siempre hay una, y nunca te quedas trabado.",
      iconEmoji = "💬"
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
      title = "Carga exacta",
      domain = DomainType.RAZONAMIENTO,
      subtitle = "Combina las celdas de energía",
      instruction = "El reactor pide una carga exacta. Toca una celda, una operación y otra celda para juntarlas; sigue hasta que una celda valga la carga. Cualquier camino sirve, y puedes deshacer sin costo.",
      iconEmoji = "🔋"
    ),
    GameDefinition(
      id = "engranajes",
      title = "Engranajes",
      domain = DomainType.RAZONAMIENTO,
      subtitle = "Arregla la máquina del cohete",
      instruction = "La máquina del cohete viene mal armada: cada pieza tiene un cartel con lo que debe hacer. Con una llave (un cambio) toca el motor o una correa para cambiar su giro, y después toca Arrancar. Cada máquina arreglada enciende una luz; con diez, tu cohete despega.",
      iconEmoji = "⚙️"
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
      instruction = "Algunos satélites traen un mensaje: se apagan y todos giran alrededor de tu planeta. Síguelos con la vista y, cuando se detengan, toca los que lo traían: cada mensaje enciende una luz.",
      iconEmoji = "🛰️"
    ),
    GameDefinition(
      id = "radar",
      title = "Rescate relámpago",
      domain = DomainType.ATENCION,
      subtitle = "Qué ves de un vistazo",
      instruction = "Atento al radar: un relámpago muestra unas cápsulas. Elige en el tablero cuáles viste y toca ¡Rescatar! Las rocas grises no se rescatan.",
      iconEmoji = "📡"
    ),
    GameDefinition(
      id = "aterrizaje",
      title = "Aterrizaje Lunar",
      domain = DomainType.RAZONAMIENTO,
      subtitle = "Sentido numérico: estimar en la línea numérica",
      instruction = "Posa la nave justo en el número de la misión. La regla solo tiene marcados los extremos: arrastra para mover la nave y suelta para aterrizar.",
      iconEmoji = "🌙"
    )
  )

  fun getById(id: String): GameDefinition? = allGames.find { it.id == id }

  /**
   * Juegos RETIRADOS: su id queda reservado (no se reutiliza) y se guarda de qué área eran. Quien los jugó antes conserva sus partidas y su progreso
   * en la base de datos y en las preferencias (NO se borra nada ni hay migración): simplemente no se muestran ni cuentan en ningún lado, porque todo lo
   * que se ve sale de [allGames] o de [getById] (null para un id retirado). La racha y el total de partidas sí cuentan esos días jugados.
   *  - `cambiochip` (Cambio de Chip, Atención): retirado el 3-oct-2026 (docs/juegos/descartados.md).
   *  - `comparacion` (Comparación Instantánea, Atención): retirado el 4-oct-2026 (docs/juegos/descartados.md).
   *  - `series` (Detective de Series, Razonamiento): retirado el 4-oct-2026 (docs/juegos/descartados.md).
   *  - `rutatesoro` (Ruta del Tesoro, Memoria): retirado el 4-oct-2026 (docs/juegos/descartados.md).
   *  - `trafico` (Tráfico Estelar, Razonamiento): retirado el 4-oct-2026 (docs/juegos/descartados.md).
   *  - `bitacora` (Bitácora de Misión, Memoria): retirado el 5-oct-2026 (docs/juegos/descartados.md). Su misión del día vivía en las preferencias `mission_log` (siguen en el respaldo; ya no se leen) y
   *    sus medidas guardadas (clave `recall`) quedan sin leerse.
   */
  val retiredDomains: Map<String, DomainType> = mapOf(
    "cambiochip" to DomainType.ATENCION, "comparacion" to DomainType.ATENCION, "series" to DomainType.RAZONAMIENTO, "rutatesoro" to DomainType.MEMORIA,
    "trafico" to DomainType.RAZONAMIENTO, "bitacora" to DomainType.MEMORIA
  )

  fun isRetired(id: String): Boolean = id in retiredDomains
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
  // Solo Piloto Estelar («la ruta de las balizas», 9-oct): todo se mide con las dos tareas a la vez. % del vuelo dentro de la ruta; señales de la misión atrapadas y resueltas; toques equivocados; «tus señales a los mandos»
  // (% = (aciertos − toques equivocados) / señales de la misión; null con menos de 8); nivel de señales y de pilotaje (1..9); racha mayor; puntos; hiperimpulsos. No se guardan en Room.
  val pilLanePct: Int? = null,
  val pilHits: Int? = null,
  val pilTargets: Int? = null,
  val pilFalse: Int? = null,
  val pilSignalPct: Int? = null,
  val pilSignalLevel: Int? = null,
  val pilBestStreak: Int? = null,
  val pilPoints: Int? = null,
  val pilHyper: Int? = null,
  // Solo Rescate relámpago («qué cápsulas viste», id radar, renovado el 9-oct): "tu vistazo" en ms (destello en el que se asentó la dificultad, medido en duraciones REALES) y cuántas cápsulas había en esas rondas; "tu captura" (cuántas se nombran bien de un
  // vistazo, de 4, en las lluvias); cápsulas rescatadas, rondas perfectas, rondas jugadas (con lluvias), racha mayor, el destello más corto resuelto (ms), el récord de cápsulas en una partida (el guardado o el de esta partida, el mayor) y si esta partida lo
  // superó. El récord va en prefs «rescate_record» (y en el respaldo); la lectura está en data/Rescate.kt. Ya no hay medidas por lugar ni robots. No se guardan en Room.
  val glanceMs: Int? = null,
  val glanceLoad: Float? = null,
  val captureK: Float? = null,
  val rescRescued: Int? = null,
  val rescPerfect: Int? = null,
  val rescRounds: Int? = null,
  val rescBestStreak: Int? = null,
  val rescShortestMs: Int? = null,
  val rescBest: Int? = null,
  val rescNewRecord: Boolean? = null,
  /** Rescate relámpago (v4): los viajes a la estación de esta partida (la nave se llena con 10 cápsulas a bordo, viaja y vuelve vacía). */
  val rescTrips: Int? = null,
  // Solo Satélites: cuántos se siguen de verdad a la vez (descontando la suerte) y la velocidad más alta superada
  // completa (múltiplo de la del nivel 1). No se guardan en Room.
  val trackingCapacity: Float? = null,
  /** Satélites: cuántos había que seguir por ronda, en promedio (techo de trackingCapacity en esa partida). */
  val trackingTargets: Float? = null,
  val trackingSpeed: Float? = null,
  // Solo Tinta o Palabra («Dos orillas»): «Cuánto te frenó la palabra» (interferencia, ms) y «Cambiar de orilla te costó» (costo de cambio, ms);
  // null = sin datos (menos de 3 aciertos de cada tipo). Lectura en data/DosOrillas.kt. No se guardan en Room.
  val interferenceMs: Int? = null,
  val switchCostMs: Int? = null,
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
  // Acoplamiento («muelle de acoplamiento», 10-oct): el premio, que no es una medida. Módulos acoplados y anillos completos de la partida, el récord de módulos (el guardado o el de esta partida, el mayor) y si esta partida lo superó. El récord y los totales van
  // en prefs «acoplamiento_record» (y en el respaldo); la lectura está en data/Acoplamiento.kt. No se guardan en Room.
  val dockDocked: Int? = null,
  val dockRings: Int? = null,
  val dockBest: Int? = null,
  val dockNewRecord: Boolean? = null,
  // Solo Rumbo a Casa: "tu brújula interna" (a qué distancia de casa quedaste, en % de la distancia que había), dónde
  // quedó cada vuelta en el marco de la vuelta justa (en fracciones de esa distancia: la base en along = 1,
  // lateral = 0; lateral + = a la derecha), si el viaje tenía faro y llegadas perfectas. La lectura está en
  // data/Homing.kt. No se guardan en Room.
  val homingErrorPct: Float? = null,
  val homingAlong: List<Float>? = null,
  val homingLateral: List<Float>? = null,
  val homingBeacon: List<Boolean>? = null,
  val homingPerfect: Int? = null,
  // Solo «La estación de correo» (id correo): encargos por evento (cartas con sello dorado o lazo: cumplidos y total), por hora (faro a tiempo y total, sin contar los cancelados), cancelados por la radio y cuántas veces se hicieron igual
  // (comisión), faros antes de hora, miradas al reloj (y cuántas cerca de la hora), cartas bien puestas y clasificadas, mejor racha, días con todos los encargos, el grupo de etapa más alto (1..5), el récord (cartas en un día perfecto, que se guarda) y
  // si esta partida lo superó. Encargos cumplidos = correctAnswers, encargos = totalTrials. El récord va en prefs «correo_record» (y en el respaldo); la medida va a StarMeasures. La lectura está en data/Mail.kt. No se guardan en Room.
  val mailEvHits: Int? = null,
  val mailEvTotal: Int? = null,
  val mailTimeHits: Int? = null,
  val mailTimeTotal: Int? = null,
  val mailCancels: Int? = null,
  val mailCommissions: Int? = null,
  val mailEarly: Int? = null,
  val mailPeeks: Int? = null,
  val mailPeeksGood: Int? = null,
  val mailRight: Int? = null,
  val mailSorted: Int? = null,
  val mailBestCombo: Int? = null,
  val mailDaysPerfect: Int? = null,
  /** La Estación de correo: cartas señal que se escaparon, por tipo (sello dorado / lazo). */
  val mailGoldMissed: Int? = null,
  val mailLazoMissed: Int? = null,
  val mailGroup: Int? = null,
  val mailBest: Int? = null,
  val mailNewRecord: Boolean? = null,
  // Solo «Satélites: enciende tu planeta» (id satelites, renovado el 9-oct): luces encendidas (= mensajes entregados), rondas perfectas (rondas = totalTrials), racha mayor de rondas perfectas, el récord de luces en una partida (el guardado o el de
  // esta partida, el mayor) y si esta partida lo superó. «Tu seguimiento» sigue en trackingCapacity / trackingTargets / trackingSpeed. El récord va en prefs «satelites_record» (y en el respaldo); la lectura está en data/Satelites.kt. No se guarda en Room.
  val satLights: Int? = null,
  val satPerfect: Int? = null,
  val satBestStreak: Int? = null,
  val satBest: Int? = null,
  val satNewRecord: Boolean? = null,
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
  val harvHints: Int? = null,
  // Solo La estrella intrusa: rondas vistas y aciertos por tipo de grupo (6: amplia, vecina, uso, material/lugar/parte, trampa,
  // regla + trampa), mediana al tocar (ms), mejor racha, «¿Qué las une?» vistas y acertadas, y las reglas (claves) de las láminas
  // nuevas, las falladas, las repasadas y las ganadas con nombre propio. No se guardan en Room: el atlas va en Atlas.kt.
  val intrSeenType: List<Int>? = null,
  val intrHitsType: List<Int>? = null,
  val intrRtMs: Int? = null,
  val intrBestStreak: Int? = null,
  val intrBonusSeen: Int? = null,
  val intrBonusHits: Int? = null,
  val intrNewPlates: List<String>? = null,
  val intrReviewNew: List<String>? = null,
  val intrReviewDone: List<String>? = null,
  val intrNamed: List<String>? = null,
  // Solo «En la punta de la lengua» (id anagramas): palabras encontradas solas, con 1-2 ayudas, con las letras justas y mostradas por Nubi, tiempo medio
  // hasta «¡La tengo!» de las que salieron solas (ms), cada palabra con su lucero (data/Punta.kt), las que Nubi mostró (las azules, que vuelven) y las azules
  // pendientes que esta partida encontró sola o con 1-2 ayudas. No se guardan en Room: las azules van en Punta.kt / prefs «punta_words».
  val puntaSolo: Int? = null,
  val puntaPista: Int? = null,
  val puntaLetras: Int? = null,
  val puntaVista: Int? = null,
  val puntaMs: Int? = null,
  val puntaWords: List<com.example.data.PuntaEntry>? = null,
  val puntaBlue: List<String>? = null,
  val puntaCleared: List<String>? = null,
  // Solo «Carga exacta» (id calculo): cargas logradas sin pista, logradas con la pista de Nubi, logradas por el camino más corto (sin pista) y tiempo medio (ms) de las
  // logradas sin pista. Cargas jugadas = totalTrials. No se guardan en Room (la medida va a StarMeasures). La lectura está en data/Carga.kt.
  val cargaAlone: Int? = null,
  val cargaHinted: Int? = null,
  val cargaShort: Int? = null,
  val cargaMs: Int? = null,
  // Solo «Engranajes» (id engranajes): la etapa más alta jugada (1..5), las luces del cohete con que termina la partida (0..9), los cohetes en órbita, cuántos despegaron en la
  // partida y el tiempo medio por máquina (ms). Máquinas jugadas = totalTrials, acertadas = correctAnswers. Las luces y los cohetes son progreso: se guardan en prefs
  // «engranajes_rocket» (y van en el respaldo); la medida va a StarMeasures. La lectura está en data/Engranajes.kt.
  val engrEtapa: Int? = null,
  val engrLights: Int? = null,
  val engrOrbit: Int? = null,
  val engrLaunches: Int? = null,
  val engrMs: Int? = null,
  // Solo «Bodega de carga» (id bodega): el grupo de etapa más alto jugado (1..5), la racha más larga, el pedido más grande sin errores de la partida, el récord (la bodega más grande de siempre, que se guarda)
  // y el tiempo medio por objeto (ms). Objetos al primer intento = correctAnswers, objetos encontrados = totalTrials. El récord va en prefs «bodega_record» (y en el respaldo); la medida va a StarMeasures.
  // La lectura está en data/Bodega.kt.
  val bodGroup: Int? = null,
  val bodBestStreak: Int? = null,
  val bodBiggest: Int? = null,
  val bodBest: Int? = null,
  val bodMs: Int? = null,
  val bodNewRecord: Boolean? = null,
  // Solo «Constelaciones» (id parejas): aciertos de memoria (= correctAnswers) y oportunidades (= totalTrials), parejas/tríos encontrados y cuántos de memoria, la racha de memoria más larga, los toques que no servían, los turnos,
  // el grupo de etapa más alto (1..6), el récord (mejor racha, que se guarda) y si esta partida lo superó. El récord va en prefs «constelaciones_record» (y en el respaldo); la medida va a StarMeasures. Lectura en data/Constelaciones.kt.
  val conGroups: Int? = null,
  val conMemGroups: Int? = null,
  val conBestStreak: Int? = null,
  val conUseless: Int? = null,
  val conTurns: Int? = null,
  val conGroup: Int? = null,
  val conBest: Int? = null,
  val conNewRecord: Boolean? = null,
  // Solo Rastro de luz (id «secuencia»): por familia (4: el rastro, al revés, el cielo gira, en marcha) el mejor largo repetido bien (0 = ninguno),
  // las rondas y los aciertos (la ronda guiada del tutorial no cuenta), qué familias aparecieron y cuáles se desbloquearon por primera vez en
  // la partida (bits: 1 rastro, 2 al revés, 4 gira, 8 en marcha). La lectura está en data/Trail.kt. No se guardan en Room.
  val rasBestLen: List<Int>? = null,
  val rasRounds: List<Int>? = null,
  val rasHits: List<Int>? = null,
  val rasModesSeen: Int? = null,
  val rasNewModes: Int? = null,
  // «Confusión de modo» tomada por familia (a lo más 1 por modo y partida; no cuenta como error). Se lee, no se muestra.
  val rasModeConfusions: List<Int>? = null
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
  AVANZADO("Avanzado", "Nivel 5 con máxima exigencia y velocidad"),
  CUSTOM("Personalizada", "Nivel asignado a medida por cada área")
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
    WeeklyChallengeDef("dominios3", "Variedad", "Juega en 3 áreas distintas esta semana", "🧭", 3),
    WeeklyChallengeDef("reto2", "Modo Reto", "Completa 2 partidas en modo Reto esta semana", "⚡", 2),
    WeeklyChallengeDef("precision3", "Racha de precisión", "Consigue 85 puntos o más en 3 partidas esta semana", "🎯", 3),
    WeeklyChallengeDef("dias4", "Constancia", "Juega 4 días distintos esta semana", "📅", 4)
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
