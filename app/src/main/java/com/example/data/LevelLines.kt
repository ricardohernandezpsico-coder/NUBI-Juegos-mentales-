package com.example.data

import java.math.BigDecimal
import java.math.MathContext
import java.math.RoundingMode

/**
 * El nivel de cada juego dicho en palabras (Tarea 159; `auditoria-fable/avance-b-puntos.md` §4): una frase que empieza con «Ya …» y dice QUÉ alcanzó la persona («Ya repites 6 luces seguidas», «Ya recuerdas 4 objetos en 8 escotillas»). Sale del avance del juego ([Skill.progress], 0..1): el nivel de su escalera
 * donde se aciertan 8 de 10, `nivel = min(N, 1 + floor(N · avance))` (el motor común, `Games/AdaptiveDifficulty.cs`; N = [Skill.Ladder.levels]). Cada tabla sale del contrato del juego en Unity (solo lectura; archivo y línea en `docs/dificultad-y-avance.md` §13). El nivel real del día puede cambiar: es lo que alcanzó, no una promesa.
 *
 * **Reglas de las frases (Opus, 22-oct, para una persona mayor; `LevelLinesTest` las vigila):** 12 palabras como máximo; una cifra principal y como mucho UN rasgo extra; nada de mecánica interna («4 de cada 10», «en tramos de 4 a 6») ni de condiciones («con reloj»); segundos con a lo más 2 cifras significativas («1,2 segundos», no «1,18»);
 * ni milisegundos ni «dp». Los rasgos que NO suben de un nivel al siguiente (cielos de 12 luces que vuelven a aparecer con gemelos, bodegas de menos escotillas que giran) se dicen en la misma frase para que no parezca que se retrocede.
 * La tarjeta de un juego dice el nivel en su fila «Tu nivel: ya giras piezas de 6 bloques, en giros finos.» ([inRow]); si la medida del juego repite lo mismo en la misma unidad (Rastro y Satélites), la fila de la última partida va sin «Tu mejor marca»: [overlapsMeasure].
 */
object LevelLines {
  /** El nivel 1..N de [gameId] donde está [progress] (0..1): `min(N, 1 + floor(N · progress))`. El épsilon evita que 0,25 caiga en el nivel 3 por el redondeo del flotante. */
  fun levelOf(gameId: String, progress: Float): Int {
    val n = Skill.ladder(gameId).levels
    return (1 + Math.floor(progress.coerceIn(0f, 1f) * n + 1e-4).toInt()).coerceIn(1, n)
  }

  /** La frase de nivel de [gameId] para su avance [progress] (0..1). [timed] = la persona juega este juego con reloj. */
  fun line(gameId: String, progress: Float, timed: Boolean = false): String = lineAt(gameId, levelOf(gameId, progress), timed)

  /** El nivel dentro de la fila «Tu nivel: …» de la tarjeta (Tarea 162): la frase con la «Ya» en minúscula y su punto final, «ya giras piezas de 6 bloques, en giros finos.». Las 17 frases empiezan con «Ya » (`LevelLinesTest` lo vigila). */
  fun inRow(gameId: String, progress: Float, timed: Boolean = false): String = line(gameId, progress, timed).replaceFirstChar { it.lowercase() } + "."

  /**
   * Los juegos cuya frase de nivel habla de lo MISMO, en la misma unidad, que su medida propia (Rastro: «luces seguidas»; Satélites: «satélites a la vez»): en su tarjeta la fila «Tu última partida» va SIN «Tu mejor marca: …» (el nivel de arriba ya dice lo que repites seguido). Rescate relámpago
   * habla de lo mismo pero su medida en segundos es más fina que el destello del nivel, así que la dice completa (Tarea 162; antes, en la Tarea 159, estos juegos ocultaban la frase del nivel).
   */
  fun overlapsMeasure(gameId: String): Boolean = gameId in setOf("secuencia", "satelites")

  /**
   * La frase del nivel [level] (1..N; se acota) de [gameId]. [timed] solo importa en Freno de Emergencia: el plazo para lanzar solo es cierto CON reloj (sin reloj es 0,3 segundos más largo), así que se dice solo si la persona juega con reloj.
   */
  fun lineAt(gameId: String, level: Int, timed: Boolean = false): String {
    val n = Skill.ladder(gameId).levels
    val l = level.coerceIn(1, n)
    val i = l - 1
    return when (gameId) {
      "parejas" -> "Ya resuelves cielos de ${PAREJAS_LUCES[i]} luces${PAREJAS_RASGO[i]}"
      "secuencia" -> "Ya repites ${RASTRO_LARGO[i]} luces seguidas" + if (RASTRO_REVES[i] > 0) " y ${RASTRO_REVES[i]} al revés" else ""
      "bodega" -> "Ya recuerdas ${BODEGA_OBJETOS[i]} objetos en ${BODEGA_ESCOTILLAS[i]} escotillas" + bodegaCola(BODEGA_CAJAS[i], l >= 8)
      "correo" -> "Ya ordenas cartas en ${CORREO_BUZONES[i]} buzones y cumples ${plural(CORREO_ENCARGOS[i], "encargo", "encargos")} al día"
      "radar" -> "Ya rescatas ${RADAR_CAPSULAS[i]} con un destello de ${seconds(RADAR_SEG[i])}"
      "freno" -> "Ya eliges entre ${FRENO_PLATAFORMAS[i]} plataformas" + if (timed) " y lanzas en ${seconds(FRENO_SEG[i])}" else ""
      "satelites" -> "Ya sigues ${SATELITES_SIGUE[i]} satélites a la vez, entre ${SATELITES_TOTAL[i]}"
      "piloto" -> "Ya atrapas señales ${if (l >= 3) "parecidas " else ""}de ${seconds(PILOTO_SEG[i])} mientras pilotas"
      "stroop" -> STROOP[i]
      "acoplamiento" -> "Ya giras piezas de ${4 + i / 3} bloques, ${if (l >= 5) "en giros finos" else "hasta ${ACOPLAMIENTO_GIRO[i]}"}"
      "calculo" -> "Ya llegas a la carga con ${CARGA[i]}"
      "engranajes" -> "Ya arreglas máquinas de " + if (ENGRANAJES_CORREAS[i] == 0) plural(ENGRANAJES_PIEZAS[i], "pieza", "piezas") else "${plural(ENGRANAJES_PIEZAS[i], "pieza", "piezas")} y ${plural(ENGRANAJES_CORREAS[i], "correa", "correas")}"
      "aterrizaje" -> ATERRIZAJE[i]
      "anagramas" -> if (l >= 5) "Ya encuentras palabras de 8 letras o más" else "Ya encuentras palabras de ${l + 3} o ${l + 4} letras"
      "meteoros" -> "Ya reconoces palabras ${METEOROS[(l - 1) / 2]}"
      "disparate" -> "Ya distingues verdades de disparates en frases ${DISPARATE[(l - 1) / 2]}"
      "intrusa" -> "Ya detectas intrusas ${INTRUSA[(l - 1) / 2]}"
      else -> throw IllegalArgumentException("juego sin frase de nivel: $gameId")
    }
  }

  private fun plural(n: Int, one: String, many: String) = if (n == 1) "1 $one" else "$n $many"

  /** Segundos con a lo más 2 cifras significativas y coma: 1,345 → «1,3 segundos»; 0,964 → «0,96 segundos»; 1,01 → «1 segundo». */
  internal fun seconds(s: Double): String {
    val r = BigDecimal(s.toString()).round(MathContext(2, RoundingMode.HALF_UP)).stripTrailingZeros()
    val text = r.toPlainString().replace('.', ',')
    return if (text == "1") "1 segundo" else "$text segundos"
  }

  // --- Constelaciones (18): luces y rasgos (ConstelacionContract.cs:88-105). Las luces NO suben siempre: al entrar los gemelos o los tríos bajan, y la frase lo dice.
  private val PAREJAS_LUCES = intArrayOf(6, 8, 10, 12, 14, 16, 12, 14, 16, 12, 15, 15, 14, 16, 19, 20, 22, 24)
  private val PAREJAS_RASGO = arrayOf(
    "", "", "", "", "", "", " con gemelos", " con gemelos", " con gemelos", " con tríos", " con tríos", " con tríos y gemelos",
    " con parejas y tríos", " con gemelos, parejas y tríos", " con gemelos, parejas y tríos", " con gemelos", " con gemelos", " con gemelos, parejas y tríos"
  )

  // --- Rastro de luz (16): el rastro simple y, desde el nivel 5, el rastro al revés (RastroContract.cs:158-177)
  private val RASTRO_LARGO = intArrayOf(2, 3, 3, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 7, 7, 8)
  private val RASTRO_REVES = intArrayOf(0, 0, 0, 0, 3, 4, 4, 4, 4, 5, 5, 5, 6, 6, 6, 7)

  // --- Bodega de carga (12): objetos y escotillas y UN solo rasgo extra, el más nuevo: una caja que cambia (niveles 5 y 7) o, desde el 8, que la bodega gira (BodegaContract.cs:82-96)
  private val BODEGA_OBJETOS = intArrayOf(2, 3, 3, 4, 4, 5, 5, 4, 5, 5, 6, 7)
  private val BODEGA_ESCOTILLAS = intArrayOf(6, 6, 8, 8, 8, 8, 10, 8, 10, 10, 10, 10)
  private val BODEGA_CAJAS = intArrayOf(0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 1, 2)
  private fun bodegaCola(cajas: Int, gira: Boolean): String = when {
    gira -> ", con la bodega girando"
    cajas > 0 -> ", con una caja que cambia"
    else -> ""
  }

  // --- Correo Estelar (10): buzones y encargos del día, lo que la persona HACE (MailContract.cs:116-128; los encargos son los del marcador «Encargos: 1 de 2»: el sello dorado, el lazo y las horas del día)
  private val CORREO_BUZONES = intArrayOf(2, 3, 3, 3, 4, 4, 4, 4, 4, 4)
  private val CORREO_ENCARGOS = intArrayOf(1, 2, 2, 2, 2, 3, 3, 4, 3, 5)

  // --- Rescate relámpago (12): cápsulas y duración del destello en segundos (RadarContract.cs:112-114: 800, 600, 600, 450, 350, 280, 280, 220, 180, 150, 120 y 100 milisegundos)
  private val RADAR_CAPSULAS = arrayOf("las 2 cápsulas", "las 2 cápsulas", "las 3 cápsulas", "las 3 cápsulas", "las 3 cápsulas", "las 3 cápsulas",
    "3 de 4 cápsulas", "3 de 4 cápsulas", "3 de 4 cápsulas", "3 de 4 cápsulas", "3 de 4 cápsulas", "3 de 4 cápsulas")
  private val RADAR_SEG = doubleArrayOf(0.8, 0.6, 0.6, 0.45, 0.35, 0.28, 0.28, 0.22, 0.18, 0.15, 0.12, 0.1)

  // --- Freno de Emergencia (12): plataformas y plazo CON reloj (BrakeContract.cs:69-80: 1400 a 800 milisegundos)
  private val FRENO_PLATAFORMAS = intArrayOf(2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4)
  private val FRENO_SEG = doubleArrayOf(1.4, 1.345, 1.291, 1.236, 1.182, 1.127, 1.073, 1.018, 0.964, 0.909, 0.855, 0.8)

  // --- Satélites (12): los que traen mensaje y los que hay en total (SatelliteContract.cs:66-80)
  private val SATELITES_SIGUE = intArrayOf(2, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5)
  private val SATELITES_TOTAL = intArrayOf(6, 6, 7, 8, 8, 9, 10, 10, 11, 11, 12, 13)

  // --- Piloto Estelar (9): cuánto dura la señal y, desde el 3, que hay señales parecidas (PilotContract.cs:119-138). El avance es el promedio del pilotaje y las señales.
  private val PILOTO_SEG = doubleArrayOf(1.60, 1.48, 1.36, 1.24, 1.13, 1.01, 0.89, 0.77, 0.65)

  // --- Tinta o Palabra (5): una orilla, dos por tramos, dos al azar (StroopContract.cs:146-153), dicho sin la mecánica
  private val STROOP = arrayOf(
    "Ya respondes por el color de la tinta",
    "Ya respondes por el color de la tinta",
    "Ya alternas entre la tinta y la palabra",
    "Ya alternas entre la tinta y la palabra sin aviso",
    "Ya alternas entre la tinta y la palabra sin aviso"
  )

  // --- Acoplamiento (12): bloques de la pieza (4 a 7) y el giro máximo (90, 135, luego 180); desde el 5 los giros son finos (DockingContract.cs:67-77)
  private val ACOPLAMIENTO_GIRO = arrayOf("un cuarto de vuelta", "casi media vuelta", "media vuelta", "media vuelta")

  // --- Carga exacta (5): las operaciones (CalculoContract.cs:150-157)
  private val CARGA = arrayOf("sumas", "sumas y restas", "sumas, restas y multiplicaciones", "las cuatro operaciones", "las cuatro operaciones y más números")

  // --- Engranajes (12): piezas y correas (EngranajesContract.cs:187-201)
  private val ENGRANAJES_PIEZAS = intArrayOf(1, 1, 2, 2, 2, 2, 3, 3, 2, 3, 3, 3)
  private val ENGRANAJES_CORREAS = intArrayOf(0, 1, 2, 2, 2, 2, 3, 3, 2, 3, 3, 4)

  // --- Lluvia de meteoros (12, de dos en dos): cuán conocidas son las palabras reales; las bandas son prevalencia SPALEX, no frecuencia de uso (MeteorContract.cs:134-154, tools/lexico/meteoros.py)
  private val METEOROS = arrayOf("muy comunes", "comunes", "algo menos comunes", "poco comunes", "que no todos conocen", "raras")

  // --- ¿Verdad o disparate? (12, de dos en dos): cómo es la frase (DisparateContract.cs:71); los segundos de la señal no se dicen: no existen sin reloj y cambian con la edad
  private val DISPARATE = arrayOf("cortas", "un poco más largas", "con «no»", "con una pausa", "con «todos» o «ningún»", "que comparan dos cosas")

  // --- La estrella intrusa (12, de dos en dos): qué tipo de regla une a las otras cuatro (IntrusaContract.cs:165-166; tools/intrusa/grupos.py)
  private val INTRUSA = arrayOf("de otro tipo de cosa", "de un tipo parecido", "por su uso", "por dónde están o qué partes tienen", "que van con otra palabra del grupo", "con trampa en reglas menos obvias")

  // --- Aterrizaje Lunar (12): la regla y el tipo de número cambian de tramo en tramo (LandingContract.cs:94-142); la marca en el medio es el único rasgo extra de los primeros niveles
  private val ATERRIZAJE = arrayOf(
    "Ya ubicas números del 0 al 10, con marca en el medio",
    "Ya ubicas números del 0 al 20, con marca en el medio",
    "Ya ubicas números del 0 al 100, con marca en el medio",
    "Ya ubicas números del 0 al 100, sin marca en el medio",
    "Ya ubicas números del 0 al 1000, con marca en el medio",
    "Ya ubicas números del 0 al 1000, sin marca en el medio",
    "Ya ubicas fracciones entre 0 y 1",
    "Ya ubicas decimales y porcentajes",
    "Ya ubicas números en reglas que no empiezan en cero",
    "Ya resuelves una suma y la ubicas entre 0 y 1000",
    "Ya ubicas fracciones entre 0 y 2",
    "Ya ubicas números negativos entre menos 50 y 50"
  )
}
