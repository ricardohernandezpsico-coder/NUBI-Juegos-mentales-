package com.example.data

import kotlin.math.roundToInt

/**
 * «Finales con sentido» (Etapa 3, aprobados por Ricardo el 10-oct-2026; especificación y textos EXACTOS de los 18 juegos: `docs/finales-con-sentido.md`): la plantilla de Aterrizaje Lunar llevada a los demás juegos.
 * Cada final, en este orden: (1) lo que hiciste, con la medida propia del juego y UN dato tuyo; (2) tu avance, solo contigo (hoy, tu promedio y tu mejor, más una frase); (3) un truco para la próxima (el condicional del juego si aplica a
 * la partida y, si no, el general de la tabla); (4) abajo, en chico, «¿Por qué importa?» con su fuente y la nota común. El detalle que antes se veía (barras, listas, desgloses) va detrás de «Ver el detalle de tu partida», cerrado por defecto.
 * Lógica pura con pruebas. Nunca percentiles, comparación con otras personas ni promesas de salud.
 */
object FinalesConSentido {
  const val PROGRESS_TITLE = "Tu avance"
  const val TRICK_TITLE = "Truco para la próxima"
  const val WHY_TITLE = "¿Por qué importa?"
  const val NOTE = "Medida de esta partida. No es un diagnóstico."
  const val DETAIL_OPEN = "Ver el detalle de tu partida"
  const val DETAIL_CLOSE = "Ocultar el detalle de tu partida"

  /** Cuántas partidas anteriores entran en «Tu promedio» y «Tu mejor» (las últimas). */
  const val PREVIOUS_MAX = 10

  /**
   * Los textos propios de cada juego (de la tabla del documento): [put] = «Lo que pusiste en juego» (con mayúscula inicial), [trick] = el truco GENERAL (el condicional del juego, si aplica, gana), [why] = «Por qué importa»,
   * [source] = la cita corta (el DOI vive en el documento).
   */
  data class Copy(val gameId: String, val put: String, val trick: String, val why: String, val source: String) {
    /**
     * «Pusiste en juego la memoria de lugar: recordar dónde quedó cada cosa. Recordar qué va con qué…» (el «lo que pusiste en juego» con su primera letra en minúscula, seguido de «por qué importa»).
     * Si lo que pusiste en juego EMPIEZA con un verbo en infinitivo («Recordar dónde guardaste cada cosa.») la frase quedaría torcida («Pusiste en juego recordar…»): se arma como «Pusiste en juego lo que usas para recordar dónde guardaste cada cosa.»
     */
    val whyText: String
      get() {
        val lower = put.replaceFirstChar { it.lowercase() }
        return (if (startsWithInfinitive(put)) "Pusiste en juego lo que usas para " else "Pusiste en juego ") + lower + " " + why
      }
    val sourceText: String get() = "Fuente: $source"
  }

  private val ALL = listOf(
    Copy("parejas", "La memoria de lugar: recordar dónde quedó cada cosa.", "Da vuelta primero una luz nueva: te muestra más del cielo.",
      "Recordar qué va con qué, como una cosa y su lugar, es la parte de la memoria que más atención pide con los años.", "Old y Naveh-Benjamin, 2008"),
    Copy("secuencia", "La memoria de trabajo: sostener un orden en la cabeza mientras lo usas.", "Junta las luces de a dos o tres, como cuando dictas un número de teléfono.",
      "La usamos para comprender, razonar y aprender: guarda por un rato lo que necesitamos mientras lo usamos.", "Baddeley, 2003"),
    Copy("bodega", "Recordar dónde guardaste cada cosa.", "Imagina cada objeto dentro de su escotilla y dile su nombre en voz baja.",
      "Recordar dónde están las cosas es esencial en el día a día, y une tres piezas: la cosa, el lugar y el lazo entre las dos.", "Postma, Kessels y van Asselen, 2008"),
    Copy("correo", "La memoria para lo pendiente: acordarte de hacer algo en el momento justo.", "Antes de empezar, dilo en voz baja: «cuando vea…, haré…».",
      "Acordarse de lo que hay que hacer más tarde es clave para manejar el propio día con independencia.", "Hering y otros, 2014"),
    Copy("stroop", "Frenar la lectura automática y cambiar de regla cuando cambia la orilla.", "Mira primero de qué orilla llega y recién después la palabra.",
      "Frenar lo automático y adaptarse cuando cambian las reglas son parte de lo que nos permite pensar antes de actuar.", "Diamond, 2013"),
    Copy("piloto", "Atender dos cosas a la vez: llevar la nave y atrapar las señales de tu misión.", "Al cruzar cada arco, repite por dentro cuál es la señal de tu misión.",
      "En el día a día solemos coordinar dos cosas que vemos a la vez, como caminar mientras miramos alrededor. Con los años, esa coordinación pide más atención.", "Beurskens y Bock, 2012"),
    Copy("freno", "Frenar a tiempo una acción que ya empezaste.", "Lanza sin esperar el ALTO y frena solo si aparece: esperarlo te vuelve más lento.",
      "Detener a tiempo lo que ya no conviene nos permite adaptarnos cuando las cosas cambian de golpe.", "Verbruggen y Logan, 2008"),
    Copy("satelites", "Seguir con la vista varias cosas que se mueven a la vez.", "Une con la mirada los satélites que sigues en una figura, como un triángulo, y sigue la figura.",
      "Seguir varias cosas en movimiento es una capacidad limitada para todos: solo podemos seguir unas pocas a la vez.", "Trick, Perl y Sethi, 2005"),
    Copy("radar", "Captar de un vistazo qué hay, aunque haya cosas que distraen.", "Mira el centro del radar y, apenas se apague, nombra por dentro las formas que viste.",
      "En personas mayores, procesar rápido lo que se ve entre otras cosas se relaciona con las tareas visuales del día a día.", "Owsley, 2013"),
    Copy("calculo", "Hacer cuentas con flexibilidad para llegar a un número.", "Mira primero si multiplicar dos celdas te deja cerca de la carga; después ajusta sumando o restando.",
      "Manejar números con soltura se relaciona con entender riesgos y tomar decisiones con información.", "Reyna y otros, 2009"),
    Copy("engranajes", "Imaginar paso a paso cómo funciona una máquina antes de tocarla.", "Antes de cambiar algo, mira qué piezas quedan después: lo que tocas antes de una rama mueve todo lo que sigue.",
      "Simular en la mente cómo se mueve algo es una forma de razonar que usamos con aparatos y mecanismos, aunque no sepamos cómo se llaman sus partes.", "Hegarty, 2004"),
    Copy("acoplamiento", "Girar formas en la mente para compararlas.", "Elige una parte que destaque, como una punta o un codo, y gira solo esa en tu mente.",
      "Girar formas en la mente es una habilidad espacial. Estas habilidades se relacionan con aprender matemáticas, ciencias y técnica.", "Uttal y otros, 2013"),
    Copy("anagramas", "Encontrar una palabra que conoces y que no sale.", "Si una no sale, piensa en cómo empieza o en otra parecida: suele destrabarla.",
      "Tener una palabra «en la punta de la lengua» es normal, y se vuelve más frecuente con los años.", "Shafto y otros, 2007"),
    Copy("meteoros", "Reconocer al instante qué palabras existen.", "Si dudas, mira el centro de la palabra: ahí suelen esconderse las letras cambiadas.",
      "El vocabulario suele mantenerse, e incluso crecer, con los años.", "Verhaeghen, 2003"),
    Copy("disparate", "Comprender frases rápido y decidir si son ciertas.", "Con un «no», lee la frase sin él y después dale la vuelta.",
      "Las frases con «no» piden más esfuerzo que las afirmativas, a todas las personas. Entenderlas bien es parte de leer con atención.", "Xiang, Kramer y Nordmeyer, 2020"),
    Copy("cosecha", "Sacar palabras de la memoria con rapidez.", "Cuando una palabra funciona, busca sus parientes; si se agotan, salta a otra letra inicial.",
      "Para encontrar palabras usamos dos caminos: agrupar parecidas y saltar a otro grupo cuando se agotan.", "Troyer, Moscovitch y Winocur, 1997"),
    Copy("intrusa", "Ordenar significados sin dejarte llevar por lo que suele ir junto.", "Antes de tocar, pregúntate qué TIPO de cosa es cada una.",
      "Ordenamos lo que sabemos de dos maneras: por tipo de cosa (perro, oso) y por lo que suele ir junto (perro, correa).", "Mirman, Landrigan y Britt, 2017")
  )

  /** ¿El texto empieza con un verbo en infinitivo (Recordar, Frenar, Seguir, Captar, Atender, Hacer, Imaginar, Girar, Encontrar, Reconocer, Comprender, Sacar, Ordenar…)? Una sola palabra en -ar, -er o -ir con mayúscula inicial; los sustantivos («La memoria…») no. */
  fun startsWithInfinitive(text: String): Boolean = Regex("^[A-ZÁÉÍÓÚÑ][a-záéíóúñ]{2,}(ar|er|ir)\\b").containsMatchIn(text.trim())

  private val BY_GAME = ALL.associateBy { it.gameId }

  /** Los textos del juego; null para Aterrizaje Lunar (ya hecho en las Tareas 70 a 73: tiene los suyos en `Aterrizaje.kt`) y para un id que no existe. */
  fun copy(gameId: String): Copy? = BY_GAME[gameId]

  /** Los ids que ya traen sus textos aquí (los 17 de la Etapa 3). */
  val gameIds: List<String> get() = ALL.map { it.gameId }

  // ------------------------------------------------------------------ tu avance, solo contigo

  /** Los tres cuadros («Hoy», «Tu promedio», «Tu mejor») y su frase. Los valores son de la medida guardada del juego (`StarMeasures`); null = «—». */
  data class Progress(val today: Float?, val average: Float?, val best: Float?, val phrase: String)

  const val PHRASE_FIRST = "Juega otra vez para ver tu avance."
  const val PHRASE_BEST = "¡Tu mejor partida hasta ahora!"
  const val PHRASE_BETTER = "Hoy te fue mejor que tu promedio."
  const val PHRASE_BELOW = "Un poco por debajo de tu promedio: es normal que varíe."
  const val PHRASE_EVEN = "Igual que tu promedio: vas parejo."

  /**
   * Las partidas ANTERIORES de este juego con las que se compara hoy: las medidas guardadas de [today] (la misma clave) sin la de esta partida (la misma marca de tiempo: funciona igual si todavía no se guardó o ya sí), las que se
   * pueden comparar con ella (el mismo reloj y un nivel parecido: [StarMeasures.comparable]) y las últimas [PREVIOUS_MAX].
   */
  fun previousValues(points: List<MeasurePoint>, today: MeasurePoint): List<Float> {
    val series = points.filter { it.key == today.key && it.timestamp != today.timestamp } + today
    return StarMeasures.comparable(series).filter { it.timestamp != today.timestamp }.takeLast(PREVIOUS_MAX).map { it.value }
  }

  /**
   * «Hoy / Tu promedio / Tu mejor» y su frase, con la misma medida y la misma unidad que muestra el juego (el formato lo pone quien llama). El promedio y el mejor salen de las partidas ANTERIORES; «Tu mejor» incluye hoy.
   * [lowerIsBetter] respeta el sentido de la medida (menos es mejor: distancias, tiempos). [tolerance] (en la unidad de la medida) es lo que cuenta como «parejo» alrededor del promedio; por medida:
   * - Constelaciones, Bodega y Correo (porcentajes de 8 a 20 ensayos, donde un solo ensayo pesa de 5 a 12 puntos): 5 puntos.
   * - Rastro de luz (luces seguidas, números enteros): media luz.
   * Frases: sin partidas anteriores (o sin medida hoy) «Juega otra vez…»; mejor que tu mejor anterior (por poco que sea) «¡Tu mejor partida hasta ahora!»; más que el promedio por sobre la tolerancia «Hoy te fue mejor…»; menos, «Un poco por
   * debajo…»; si no, «Igual que tu promedio…».
   */
  fun progress(today: Float?, previous: List<Float>, tolerance: Float, lowerIsBetter: Boolean = false): Progress {
    if (today == null || today.isNaN() || previous.isEmpty()) return Progress(today?.takeIf { !it.isNaN() }, null, null, PHRASE_FIRST)
    val mean = previous.average().toFloat()
    val prevBest = if (lowerIsBetter) previous.min() else previous.max()
    val signed = if (lowerIsBetter) -(today - mean) else today - mean                       // > 0: hoy mejor que el promedio
    val beatsBest = if (lowerIsBetter) today < prevBest else today > prevBest
    val phrase = when {
      beatsBest -> PHRASE_BEST
      signed > tolerance -> PHRASE_BETTER
      signed < -tolerance -> PHRASE_BELOW
      else -> PHRASE_EVEN
    }
    val best = if (lowerIsBetter) minOf(prevBest, today) else maxOf(prevBest, today)
    return Progress(today, mean, best, phrase)
  }

  /** «78 %» (redondeado); «—» sin dato. */
  fun percentText(value: Float?): String = if (value == null || value.isNaN()) "—" else "${value.roundToInt()} %"

  /** «170 ms» (redondeado); «—» sin dato. */
  fun msText(value: Float?): String = if (value == null || value.isNaN()) "—" else "${value.roundToInt()} ms"

  /** «212 °/s» (grados por segundo, redondeado); «—» sin dato. */
  fun degreesText(value: Float?): String = if (value == null || value.isNaN()) "—" else "${value.roundToInt()} °/s"

  /** «3,2 a la vez» (coma decimal); «—» sin dato. */
  fun atOnceText(value: Float?): String = if (value == null || value.isNaN()) "—" else String.format(java.util.Locale("es"), "%.1f", value) + " a la vez"

  /** «6 luces» (en singular «1 luz»; el promedio se redondea a un número entero de luces); «—» sin dato. */
  fun lightsText(value: Float?): String = if (value == null || value.isNaN()) "—" else value.roundToInt().let { if (it == 1) "1 luz" else "$it luces" }

  /** Para lectores de pantalla: los textos del recuadro principal y la frase de tu avance, de corrido. */
  fun spoken(vararg parts: String?): String = parts.filterNotNull().filter { it.isNotBlank() }.joinToString(". ")
}
