package com.example.data

import com.example.Features
import com.example.model.AgeRange
import kotlin.math.min

/**
 * Tu lugar entre personas de tu edad (Tarea 202; `auditoria-fable/nubi-plus-comparacion.md` §1, §4 y §11, opción B), LÓGICA PURA: el percentil desde el histograma SIN suavizar, la frase «superas al N %», la división con nombre, las reglas de «pocos», el percentil semana a semana,
 * «otras edades» y la forma de la campana; desde la Tarea 203, la cifra protagonista, la escalera de divisiones con su primer acierto, «te faltan», la racha de semanas y la frase gratis. No sabe de pantallas ni de dónde salen las marcas de las otras personas: las pide a una [Source] (desde la Tarea 205, la REFERENCIA ESTIMADA que lee `EstimatedReference` de los JSON del APK, también en la tienda, y en depuración, con un mundo de muestra, los datos de `SampleComparison`; el día que haya servidor, sus documentos semanales con personas reales, que mandan sobre la referencia en cada franja que llegue a [MIN_PEOPLE]).
 *
 * Lo que NO hace, a propósito: nunca habla de «capacidad», «edad mental», «ranking» ni «velocidad»; no hay lista de nombres ni «los 10 mejores»; las siete franjas se comparan (14 a 17 con la tabla de 18 a 29, Tarea 216); nunca presenta una referencia estimada como personas (con ella dice siempre «referencia estimada», jamás «personas» ni cuenta robots) ni inventa lugar con personas reales que son pocas (una clase con menos de [MIN_PEOPLE] se dice «todavía somos pocos»).
 */
object Comparison {
  /** Personas que hacen falta en una clase de referencia (franja, juego y semana) para decir un lugar (§4). */
  const val MIN_PEOPLE = 50

  /** La forma de la distribución de los DATOS DE MUESTRA y de las clases vacías por omisión: 16 tramos de 5 aciertos (0 a 4, 5 a 9, …, 70 a 74 y «75 o más»). Cada juego de la referencia estimada trae la suya en su JSON (1·2·5·5 y 24·16·16·16): ver [Distribution.binWidth] y [Distribution.bins]. */
  const val BIN_WIDTH = 5
  const val BINS = 16

  /** El borde derecho del dibujo de la campana (el último tramo, «75 o más», se dibuja de 75 a 80). */
  const val TOP_MARK = BIN_WIDTH * BINS

  const val WEEKS = 7
  const val RECENT_WEEKS = 4

  // ------------------------------------------------------------------ los textos de la sección

  const val SECTION_MARKS = "Tus marcas"
  const val SECTION_COMPARE = "Esta semana, comparado con tu edad"
  const val SAMPLE_NOTICE = "Estos son datos de muestra: solo se ven en la versión de prueba."
  const val PLACE_TITLE = "Tu lugar esta semana"
  /** Antes «Tu percentil semanal» (Tarea 209, decisión de Ricardo, 1-nov): «percentil» salió de todo texto visible; los mismos números (el lugar de cada semana) y la raya «la mitad». Primero se llamó «Tu lugar cada semana», pero quedaba casi igual a [PLACE_TITLE] en la misma página (la losa de «sin marca esta semana» y las semanas pasadas); Ricardo eligió «Semana a semana» (Tarea 209b, 1-nov; una línea a 393 y 360 dp con letra 1,0, regla de la Tarea 203b). */
  const val WEEKS_TITLE = "Semana a semana"
  const val OTHERS_TITLE = "Otras edades"
  /** Cierra «Otras edades» cuando alguna franja (o todas las edades) todavía no llega a 50 personas: no se muestran filas vacías. */
  const val OTHERS_HIDDEN = "Las demás edades aparecen cuando lleguen a 50 personas."
  /** El rótulo de la raya de 50 en «Semana a semana» (14 sp, como los ejes). */
  const val WEEKS_HALF_LABEL = "la mitad"
  const val WEEKS_AXIS_RIGHT = "esta semana"
  const val BELL_AXIS_LEFT = "0 aciertos"
  const val BELL_AXIS_RIGHT = "75 o más"
  val BELL_AXIS_MIDDLE = listOf(25, 50)
  const val MERGED_LINE = "Juntamos dos franjas porque todavía somos pocos."
  const val FEW_TITLE = "Todavía somos pocos"
  const val FEW_BAR_GOAL = MIN_PEOPLE
  const val BELL_TITLE = "La campana de tu edad"
  /** Sin franja de edad guardada (Tarea 205b): la página dice dónde elegirla. Es la ruta real: Tú › «Más ajustes» › «Juego» › «Rango de edad». */
  const val AGE_HINT = "Para compararte con una referencia estimada para tu edad, elige tu edad en Tú › Más ajustes › Juego › Rango de edad."
  const val BELL_CAPTION = "La línea marca la mitad de las personas."

  // ---- el lugar aparece desde la tercera partida de cada juego (Tarea 207; decisión de Ricardo, 1-nov)

  /**
   * Desde la tercera partida GUARDADA de un juego se dice el lugar. En la primera semana de las 5 personas de Ricardo, 15 de las 20 primeras partidas quedaron en 0 a 2 de cada 100 (se aprende mucho del día 1 al 3) y la referencia estimada
   * juega 3 partidas: las primeras sirven para conocer el juego y no se comparan. Cuenta POR JUEGO ([gamesPlayed]), con cualquier semana y pista; las de invitado no se guardan, así que no cuentan.
   */
  const val PLACE_FROM_GAME = 3

  /** Lo que dice la página (UNA vez, una línea corrida y sin negrita, donde iría el rótulo de la comparación) mientras el juego tiene menos de [PLACE_FROM_GAME] partidas guardadas. */
  const val EARLY_NOTICE = "Tu lugar aparece desde tu tercera partida: las primeras sirven para conocer el juego."

  /** Cuántas partidas del Contrarreloj tiene guardadas la persona en [game]: de cualquier semana y de cualquier pista (versión), no solo las de hoy. */
  fun gamesPlayed(results: List<TimeTrialResult>, game: String): Int = results.count { it.gameId == game }

  /** ¿Ya jugó las partidas que pide el lugar ([PLACE_FROM_GAME]) en [game]? */
  fun placeUnlocked(results: List<TimeTrialResult>, game: String): Boolean = gamesPlayed(results, game) >= PLACE_FROM_GAME

  /**
   * Qué hace la comparación de un juego con una persona, la PRECEDENCIA en un solo lugar (la pestaña Contrarreloj la lee; la puerta de Avance, que la leía, se fue en la Tarea 215):
   * [NONE] no hay nada que decir (sin fuente o sin tabla para ese juego); [NEEDS_AGE] hay con qué comparar pero falta la franja de edad (la página dice dónde elegirla, [AGE_HINT]);
   * [EARLY] hay franja y tabla pero el juego tiene menos de [PLACE_FROM_GAME] partidas guardadas (la página dice [EARLY_NOTICE] y no muestra ningún lugar); [SHOW] todo como siempre.
   */
  enum class Gate { NONE, NEEDS_AGE, EARLY, SHOW }

  fun gate(source: Source?, stored: AgeRange?, game: String, results: List<TimeTrialResult>): Gate {
    if (source == null || !source.covers(game)) return Gate.NONE
    if (rangeFor(stored, source) == null) return Gate.NEEDS_AGE        // sin franja guardada, el aviso de edad (las siete franjas se comparan igual, también la de 14 a 17 con la tabla de 18 a 29: Tarea 216; antes, de 13 a 17 había un `Gate.MINOR`)
    return if (placeUnlocked(results, game)) Gate.SHOW else Gate.EARLY
  }

  /**
   * Con qué se compara ESTA persona en la pestaña Contrarreloj y en la hoja del candado (Tarea 209: antes lo calculaba `ProgressScreen` por su cuenta, para la puerta de Avance, que se fue en la Tarea 215): la comparación de la fuente si la hay y la persona tiene una franja con la que compararse; si no (sin fuente o sin franja), [Kind.NONE].
   */
  fun kindFor(source: Source?, stored: AgeRange?): Kind = if (source != null && rangeFor(stored, source) != null) kindOf(source) else Kind.NONE

  // ---- la REFERENCIA ESTIMADA (Tarea 205; `auditoria-fable/referencia-estimada.md` §4 y las decisiones de Opus del 1-nov): siempre dicha; en cada losa solo «referencia estimada»; nunca se llama «personas»
  const val ESTIMATED_BELL_CAPTION = "La línea marca la mitad de la referencia estimada."
  /** Bajo el gráfico de las semanas (con la referencia estimada). */
  const val WEEKS_ESTIMATED_NOTE = "Semanas comparadas con la referencia estimada."

  /** La fila de 48 dp en la hoja del candado y en la pregunta «¿Con qué me compara el Contrarreloj?»: abre la hoja «De dónde sale la referencia» (el lector dice «…, botón»). */
  const val SOURCE_LINK = "De dónde sale la referencia ›"

  /**
   * La fila de la página «Contrarreloj» (Tarea 213, decisión de Ricardo, 2-nov): DEBAJO de la losa de la cifra y antes de la escalera, abre la misma hoja «De dónde sale la referencia». Reemplaza a la frase larga y a la fila del final de la página (Tarea 211): la explicación vive en la hoja y la página
   * no la repite. El lector dice «Con qué te comparas, botón».
   */
  const val COMPARE_LINK = "Con qué te comparas ›"

  /**
   * La frase de la campana, según sea la referencia estimada o no. El TÍTULO es el mismo («La campana de tu edad»): «La campana estimada de tu edad» partía en dos líneas hasta con letra 1,0 a 393 dp (56,3 dp de alto, `ContrarrelojPageTest`), así que
   * «referencia estimada» va en la frase de abajo (Opus, 1-nov: «si no cabe en UNA línea…»).
   */
  fun bellCaption(estimated: Boolean, onHalf: Boolean = false): String = when {
    onHalf && estimated -> BELL_ON_HALF_ESTIMATED
    onHalf -> BELL_ON_HALF_PEOPLE
    estimated -> ESTIMATED_BELL_CAPTION
    else -> BELL_CAPTION
  }

  // ---- la barra «Tú» sobre la raya de la mitad (Tarea 205b; Opus, 1-nov): la frase nunca habla de una raya que no se ve
  /** Cuando la barra «Tú» tapa la raya de la mitad, la frase de la campana dice esto en vez de «La línea marca la mitad…» (y la raya no se dibuja). */
  const val BELL_ON_HALF_ESTIMATED = "Tu marca está en la mitad de la referencia estimada."
  const val BELL_ON_HALF_PEOPLE = "Tu marca está en la mitad de las personas."
  /** Lo que miden, en dp, la barra «Tú» y la raya de la mitad de la campana, y el aire que debe quedar entre las dos para que se vean separadas. */
  const val BELL_BAR_DP = 6.0
  const val BELL_LINE_DP = 1.5
  const val BELL_CLEARANCE_DP = 1.0

  /**
   * true si, en una campana de [widthDp] dp de ancho interior, la barra «Tú» de [mark] taparía la raya de la mitad de [d] (o la dejaría pegada): la distancia entre sus centros, en dp, es menor que medio ancho de la barra + medio ancho de la raya + 1 dp.
   * Depende del ancho porque cada acierto mide `widthDp / topMark` dp: en Rescate relámpago (16 tramos de a 3, 48 aciertos de ancho: ~6,4 dp por acierto en una campana de 305 dp) un acierto de diferencia ya las separa; en Tinta o Palabra, a 360 dp, hacen falta dos o más (~3,4 dp por acierto).
   */
  fun barCoversHalf(mark: Int, d: Distribution, widthDp: Double): Boolean {
    val gapDp = Math.abs(markX(mark, d.topMark) - markX(d.p50, d.topMark)) * widthDp / d.topMark
    return gapDp < BELL_BAR_DP / 2 + BELL_LINE_DP / 2 + BELL_CLEARANCE_DP
  }

  // ------------------------------------------------------------------ la distribución

  /**
   * Las mejores marcas semanales de UNA clase de referencia (juego, pista, semana o semanas, franja o franjas): cuántas [people] y, en [bins], cuántas cayeron en cada tramo de [binWidth] aciertos (sin suavizar; son 16 tramos de 5 en los datos de muestra y los de cada juego en la referencia estimada); [p25], [p50] y [p75] son los cuartiles en aciertos (la referencia estimada los calcula la app desde los tramos con
   * [markAtPercentile]; el servidor, con las marcas crudas; en los datos de muestra salen de su tabla). Con menos de [MIN_PEOPLE] personas reales no alcanza para decir un lugar; la referencia estimada siempre alcanza ([enough]).
   */
  data class Distribution(
    val people: Int, val bins: List<Int>, val p25: Int, val p50: Int, val p75: Int,
    /** El ancho del tramo en aciertos (Tarea 205: cada juego trae el suyo; 1 en Rescate, 2 en Acoplamiento y 5 en los otros dos). */
    val binWidth: Int = BIN_WIDTH,
    /** true = la referencia ESTIMADA (robots que juegan la pista real, `EstimatedReference`): [people] es la cuenta de robots y NO se dice; nunca se presenta como personas. */
    val estimated: Boolean = false
  ) {
    init {
      require(bins.size >= 2 && binWidth >= 1 && bins.all { it >= 0 } && bins.sum() == people) { "el histograma debe tener 2 tramos o más, de ancho 1 o más, que sumen las $people personas" }
    }

    /**
     * Alcanza para decir un lugar: 50 personas reales o más, o la referencia ESTIMADA (Tarea 205, Opus 1-nov: lo que depende de «cuántos» lee `estimated`, no `people`; los robots que arman la tabla nunca se cuentan ni se muestran ni deciden nada en la página:
     * `EstimatedReference.parse` solo deja entrar tablas con [MIN_PEOPLE] robots o más, y una clase vacía queda con `estimated = false`).
     */
    val enough: Boolean get() = estimated || people >= MIN_PEOPLE

    /** El borde derecho de la campana: el último tramo («75 o más») se dibuja de [lastStart] a [topMark]. */
    val topMark: Int get() = binWidth * bins.size

    /** Donde EMPIEZA el último tramo, el de «o más» (75 en 16 tramos de 5; 95 en 20 de 5; 45 en 16 de 3; 38 en 20 de 2). */
    val lastStart: Int get() = binWidth * (bins.size - 1)

    companion object {
      /** Una clase sin gente (no alcanza para decir un lugar). */
      fun empty(binWidth: Int = BIN_WIDTH, binCount: Int = BINS): Distribution = Distribution(0, List(binCount) { 0 }, 0, 0, 0, binWidth, false)
    }
  }

  /** De dónde salen las marcas de las otras personas (hoy: `SampleComparison`, solo en depuración). [ranges] null = todas las edades; [weeks] semanas que TERMINAN [weeksAgo] semanas atrás (0 = esta semana). */
  interface Source {
    fun distribution(game: String, ranges: List<AgeRange>?, weeksAgo: Int, weeks: Int = 1): Distribution

    /** Si la fuente tiene con qué comparar este juego (la referencia estimada es por juego y versión de la pista: sin su archivo no hay comparación). */
    fun covers(game: String): Boolean = true
  }

  /** Con qué se compara una persona, para los textos que no dependen de un lugar concreto (subtítulo, hoja del candado): nada, la referencia ESTIMADA (la de la tienda) o «personas» (los datos de muestra de depuración; mañana, personas reales). */
  enum class Kind { NONE, ESTIMATED, PEOPLE }

  /** El tipo de comparación de una fuente: null = ninguna; la muestra de depuración = personas; cualquier otra (la compuesta, con la referencia estimada) = estimada. */
  fun kindOf(source: Source?): Kind = when (source) {
    null -> Kind.NONE
    is SampleComparison -> Kind.PEOPLE
    else -> Kind.ESTIMATED
  }

  /** El tipo de comparación de la sección que dibuja la página. */
  fun kindOf(section: Section?): Kind = when {
    section == null -> Kind.NONE
    section.estimated -> Kind.ESTIMATED
    else -> Kind.PEOPLE
  }

  /**
   * La fuente compuesta (Tarea 205): para cada (juego, franja, semana) usa la REAL si tiene [MIN_PEOPLE] personas o más y, si no, la ESTIMADA. Hoy no hay servidor, así que la real es siempre null y la estimada es la que manda; el día que haya servidor, cada franja deja la referencia
   * cuando llega a 50 personas reales. Una clase que ninguna de las dos alcanza (todas las edades, las franjas juntas con la sola referencia) queda vacía: no hay lugar que decir.
   */
  class ComposedSource(private val real: Source?, private val estimated: Source?) : Source {
    override fun covers(game: String): Boolean = real?.covers(game) == true || estimated?.covers(game) == true

    override fun distribution(game: String, ranges: List<AgeRange>?, weeksAgo: Int, weeks: Int): Distribution {
      val r = real?.distribution(game, ranges, weeksAgo, weeks)
      if (r != null && r.enough) return r
      val e = estimated?.distribution(game, ranges, weeksAgo, weeks)
      if (e != null && e.enough) return e
      return r ?: e ?: Distribution.empty()
    }
  }

  /**
   * De dónde sale la comparación en ESTA build: con [Features.COMPARE] encendida sería el servidor, que todavía no existe (devuelve null: no hay sección); con ella apagada, SOLO las builds de depuración usan los datos de muestra ([world]: «muchos», «pocos» o «muy pocos»), y la de la tienda no tiene nada.
   * `ContrarrelojPageTest` y `ComparisonTest` aseguran que en release no hay sección.
   */
  fun sourceFor(debugBuild: Boolean, world: SampleComparison.World, estimated: Source? = null): Source? = when {
    debugBuild && world != SampleComparison.World.REFERENCE -> SampleComparison(world)
    estimated == null -> null
    else -> ComposedSource(realSource(), estimated)
  }

  /**
   * La fuente de personas REALES: hoy ninguna (no hay servidor). [Features.COMPARE] queda EXACTAMENTE para eso: cuando exista la fuente del servidor, se devuelve aquí con la bandera encendida; la referencia estimada no depende de ella (Tarea 205: la tienda ya trae la comparación con la referencia).
   */
  private fun realSource(): Source? = if (Features.COMPARE) null else null

  /**
   * La franja con que se compara una persona: la suya; si no la eligió, en las builds de depuración con datos de muestra la de 45 a 54 años (para poder ver la sección) y en cualquier otra, ninguna (sin franja no hay con quién compararla). Las siete franjas se comparan (Tarea 216, 2-nov: la app es desde los 14 años y la de 14 a 17 usa la tabla de 18 a 29, [EstimatedReference.SHARED_RANGE]).
   */
  fun rangeFor(stored: AgeRange?, sample: Boolean): AgeRange? = stored ?: if (sample) AgeRange.R45_54 else null

  /** Lo mismo, para la fuente que hay en esta build: con datos de muestra ([SampleComparison]) sin franja elegida se usa la de 45 a 54 años (así ninguna pantalla nombra la muestra). */
  fun rangeFor(stored: AgeRange?, source: Source): AgeRange? = rangeFor(stored, sample = source is SampleComparison)

  // ------------------------------------------------------------------ el percentil y la división

  /**
   * Cuántas de cada 100 personas de [d] tienen una marca MENOR que [mark], contando sobre los tramos SIN suavizar (§1.1 y §1.2): los tramos de abajo, enteros, más la parte del propio tramo que queda debajo de la marca (los 5 aciertos de un tramo se reparten parejo: con 3 de 5 por debajo, tres quintos de sus personas).
   * Entero (se redondea hacia abajo: nunca dice más de lo cierto), con tope 99 (nunca «100»: siempre hay alguien igual) y piso 0 (se dice igual, sin culpa). El último tramo («75 o más») no se parte: cuenta solo los de abajo.
   */
  fun percentile(mark: Int, d: Distribution): Int {
    if (d.people <= 0) return 0
    val m = mark.coerceAtLeast(0)
    val w = d.binWidth
    val last = d.bins.size - 1
    val i = min(m / w, last)
    val belowTimesW = d.bins.take(i).sum().toLong() * w
    val withinTimesW = if (i == last) 0L else d.bins[i].toLong() * (m - i * w)
    return ((100L * (belowTimesW + withinTimesW)) / (w.toLong() * d.people)).toInt().coerceIn(0, 99)
  }

  /** El primer acierto con que el percentil llega a [p] (null si ni con el borde de la pista se llega): la regla única de [thresholds], de la mitad y de los cuartiles. */
  fun firstMarkReaching(p: Int, d: Distribution): Int? = (0 until d.topMark).firstOrNull { percentile(it, d) >= p }

  /**
   * El acierto donde se cruza el percentil [p] (25, 50 o 75): la mitad de la campana y los cuartiles de la referencia estimada salen de los TRAMOS con la misma regla del percentil y de la escalera (Tarea 205, Opus 1-nov), no de los p25/p50/p75 del JSON, que el robot calcula de otra forma
   * y difieren 1 o 2 aciertos en los bordes (esos campos quedan solo como informe). Si no se alcanza, empieza el último tramo.
   */
  fun markAtPercentile(p: Int, d: Distribution): Int = firstMarkReaching(p, d) ?: d.lastStart

  /**
   * Las DIEZ divisiones de la liga de la semana (Tarea 229; `docs/liga-de-la-semana.md` §1; decisión de Ricardo del 4-nov: una sola división por persona, que sale del Contrarreloj de la semana; antes eran cinco y de cada juego). [from] es la línea del % de la semana: el promedio de lo que la mejor marca de cada juego supera de la referencia estimada (`League`).
   * Los nombres se escriben siempre completos («En la Luna», nunca «Luna») y varios ya traen su preposición («En órbita», «Entre estrellas», «Sobre las nubes»): para armar «te quedarías …» usa [placeIn].
   */
  enum class Division(val label: String, val from: Int) {
    PLATFORM("En la plataforma", 0),
    TAKEOFF("Despegando", 10),
    CLOUDS("Sobre las nubes", 20),
    ORBIT("En órbita", 30),
    MOON("En la Luna", 40),
    MARS("En Marte", 50),
    ASTEROIDS("Entre asteroides", 60),
    JUPITER("En Júpiter", 70),
    STARS("Entre estrellas", 80),
    GALAXY("En otra galaxia", 90);

    /** La de arriba (null en la más alta). */
    val up: Division? get() = entries.getOrNull(ordinal + 1)

    /** La de abajo (null en la plataforma, de donde no se baja). */
    val down: Division? get() = entries.getOrNull(ordinal - 1)
  }

  /** La división de la liga de un % de la semana: la más alta cuya línea el % alcanza. */
  fun division(percent: Int): Division = Division.entries.last { percent >= it.from }

  /** El nombre con su preposición lista para seguir a «te quedarías»: «en Despegando», pero «En la Luna», «Entre estrellas» y «Sobre las nubes» tal cual (nunca «en En la Luna»). */
  fun placeIn(d: Division): String = if (d.label.startsWith("En ") || d.label.startsWith("Entre ") || d.label.startsWith("Sobre ")) d.label else "en ${d.label}"

  private val ORDINALS = listOf("primera", "segunda", "tercera", "cuarta", "quinta", "sexta", "séptima", "octava", "novena", "décima")

  /**
   * El lugar de una división entre las diez, dicho en palabras (Tarea 209, ahora «de diez»): «la primera de diez divisiones», «la segunda…», … «la novena de diez divisiones» y, la más alta, «la décima y más alta de las diez divisiones».
   * Va en minúscula, para seguir al nombre en una frase; la línea suelta lo dice con mayúscula ([divisionPlaceLine]).
   */
  fun divisionPlace(d: Division): String =
    if (d.up == null) "la ${ORDINALS[d.ordinal]} y más alta de las diez divisiones" else "la ${ORDINALS[d.ordinal]} de diez divisiones"

  /** El lugar de la división como frase suelta: «La tercera de diez divisiones.». */
  fun divisionPlaceLine(d: Division): String = divisionPlace(d).replaceFirstChar { it.uppercase() } + "."

  /** Lo que sigue a «Tu división:» en la losa de la liga: «la segunda de diez.» (la más alta, «la décima y más alta.»). */
  fun divisionRank(d: Division): String = if (d.up == null) "la ${ORDINALS[d.ordinal]} y más alta." else "la ${ORDINALS[d.ordinal]} de diez."

  // ------------------------------------------------------------------ las franjas vecinas

  private val GROUPS: List<List<AgeRange>> = listOf(
    listOf(AgeRange.R14_17, AgeRange.R18_29),
    listOf(AgeRange.R30_44, AgeRange.R45_54),
    listOf(AgeRange.R55_64, AgeRange.R65_74),
    listOf(AgeRange.R65_74, AgeRange.R75_PLUS)
  )

  /**
   * Las franjas con que se junta la de [range] cuando todavía somos pocos (§4, paso 3: 14 a 29 · 30 a 54 · 55 a 74 · 65 o más): la primera pareja que la trae. Solo con personas reales (fase 2): con la referencia estimada nunca se juntan.
   */
  fun neighborGroup(range: AgeRange): List<AgeRange> = GROUPS.first { range in it }

  /** «30 a 54 años», «55 a 74 años», «65 años o más» o «14 a 29 años». */
  fun groupLabel(group: List<AgeRange>): String = when (group.last()) {
    AgeRange.R75_PLUS -> "65 años o más"
    AgeRange.R45_54 -> "30 a 54 años"
    AgeRange.R65_74 -> "55 a 74 años"
    else -> "14 a 29 años"
  }

  // ------------------------------------------------------------------ tu lugar esta semana

  /** Con quiénes se compara una marca: la franja esta semana (lo normal), la franja las últimas 4 semanas, las franjas vecinas las últimas 4 semanas, todas las edades ESTA semana o todas las edades las últimas 4 semanas (estas dos, solo en «Mientras tanto»). */
  enum class Scope { OWN_WEEK, OWN_4_WEEKS, NEIGHBORS_4_WEEKS, ALL_WEEK, ALL_4_WEEKS }

  /** Un lugar dicho: la [mark] de esta semana contra [distribution] (de [ranges], null = todas las edades), con su [percentile]. (Desde la Tarea 229 la división ya no es de un juego: es la de la liga de la semana, `League`.) */
  data class Ranked(val mark: Int, val percentile: Int, val scope: Scope, val ranges: List<AgeRange>?, val distribution: Distribution) {
    /** true si la clase de referencia es la ESTIMADA (robots): los textos dicen «referencia estimada», nunca «personas». */
    val estimated: Boolean get() = distribution.estimated
  }

  sealed interface Place {
    /** Alcanzan 50 personas en la clase de referencia (la franja esta semana, o la primera de las más amplias de las reglas 2 y 3 de §4). */
    data class Found(val ranked: Ranked) : Place

    /** «Todavía somos pocos»: [own] personas de la franja jugaron este juego esta semana y ninguna clase de la franja o sus vecinas llegó a 50; [meanwhile] es lo que sí se puede decir con todas las edades (esta semana si llegan a 50; si no, las últimas 4 semanas), si ahí llegan a 50 (paso 4). */
    data class Few(val own: Int, val meanwhile: Ranked?) : Place

    /** Sin marca esta semana en este juego: no hay con qué compararse. */
    data object NoMark : Place
  }

  /**
   * El lugar de [mark] (la mejor marca de esta semana en [game]; null = ninguna) para una persona de [range], con las reglas de §4 EN ORDEN: franja + esta semana con 50 personas o más; si no, la franja las últimas 4 semanas; si no, las franjas vecinas las últimas 4 semanas;
   * si aun así faltan, «Todavía somos pocos» con la cuenta real de la franja y, de «mientras tanto», todas las edades ESTA semana si llegan a 50 (el MISMO número que la fila «Todas las edades» de «Comparado con otras edades»: un grupo nunca muestra dos números en una pantalla) y, solo si esta semana no llegan, todas las edades de las últimas 4 semanas, dicho.
   * Siempre se dice cuál se usó (`Ranked.scope`).
   */
  fun place(source: Source, game: String, range: AgeRange, mark: Int?): Place {
    if (mark == null) return Place.NoMark
    fun ranked(scope: Scope, ranges: List<AgeRange>?, d: Distribution) = Ranked(mark, percentile(mark, d), scope, ranges, d)
    val own = source.distribution(game, listOf(range), 0)
    if (own.enough) return Place.Found(ranked(Scope.OWN_WEEK, listOf(range), own))
    source.distribution(game, listOf(range), 0, RECENT_WEEKS).takeIf { it.enough }?.let { return Place.Found(ranked(Scope.OWN_4_WEEKS, listOf(range), it)) }
    neighborGroup(range).let { group -> source.distribution(game, group, 0, RECENT_WEEKS).takeIf { it.enough }?.let { return Place.Found(ranked(Scope.NEIGHBORS_4_WEEKS, group, it)) } }
    val allWeek = source.distribution(game, null, 0).takeIf { it.enough }?.let { ranked(Scope.ALL_WEEK, null, it) }
    val meanwhile = allWeek ?: source.distribution(game, null, 0, RECENT_WEEKS).takeIf { it.enough }?.let { ranked(Scope.ALL_4_WEEKS, null, it) }
    return Place.Few(own.people, meanwhile)
  }

  // ------------------------------------------------------------------ la cifra protagonista y la escalera de divisiones (Tarea 203; `auditoria-fable/contrarreloj-rediseno.md` §3, D2)

  /** Quiénes son «las personas» de la cifra: «de 45 a 54 años», «de 30 a 54 años» o «de todas las edades» (la clase de referencia siempre completa). */
  private fun whoText(r: Ranked, range: AgeRange): String = when (r.scope) {
    Scope.OWN_WEEK, Scope.OWN_4_WEEKS -> "de ${range.label}"
    Scope.NEIGHBORS_4_WEEKS -> "de ${groupLabel(r.ranges ?: listOf(range))}"
    Scope.ALL_WEEK, Scope.ALL_4_WEEKS -> "de todas las edades"
  }

  /** Con quiénes se compara la persona cuando la clase de referencia son PERSONAS (el futuro con servidor; hoy solo los datos de muestra de depuración): «las personas de tu edad que jugaron esta semana». */
  private fun peopleOf(r: Ranked, range: AgeRange): String = when (r.scope) {
    Scope.OWN_WEEK -> "las personas de tu edad que jugaron esta semana"
    Scope.OWN_4_WEEKS -> "las personas de tu edad que jugaron en las últimas $RECENT_WEEKS semanas"
    Scope.NEIGHBORS_4_WEEKS -> "las personas ${whoText(r, range)} que jugaron en las últimas $RECENT_WEEKS semanas"
    Scope.ALL_WEEK -> "las personas de todas las edades que jugaron esta semana"
    Scope.ALL_4_WEEKS -> "las personas de todas las edades que jugaron en las últimas $RECENT_WEEKS semanas"
  }

  /**
   * La cifra de la página, en UNA sola frase (Tarea 213; decisión de Ricardo, 2-nov: «breve y claro; si explicas mucho se complica»; reemplaza a «63 de cada 100» con su frase de abajo, que seis de cada treinta y cinco personajes de MiroFish leían al revés): «Con 46 aciertos, superas al 5 % de la referencia estimada para tu edad.»
   * El número es el mismo percentil de siempre (los tramos SIN suavizar, hacia abajo, con tope 99). La referencia siempre se dice «estimada» (regla del 1-nov). Con 0 %, sin culpa y sin número: «Con 3 aciertos, todavía no superas ninguna marca de la referencia estimada para tu edad.»
   * Con PERSONAS reales (el futuro con servidor; hoy solo los datos de muestra), la misma frase sobre ellas y con la clase que se usó: «Con 46 aciertos, superas al 5 % de las personas de tu edad que jugaron esta semana.» (con las 4 semanas o las franjas juntas lo dice). La página la pega con espacios duros ([glueNumbers]: «5 %» no se parte);
   * aquí queda con espacios normales, que es lo que lee el lector de pantalla. Una sola frase y un solo estilo: la división y su lugar van debajo, sin cambio.
   */
  fun heroSentence(r: Ranked, range: AgeRange): String {
    val hits = TimeTrialDay.hitsText(r.mark)
    val p = r.percentile
    if (r.estimated) return if (p > 0) "Con $hits, superas al $p % de la referencia estimada para tu edad." else "Con $hits, todavía no superas ninguna marca de la referencia estimada para tu edad."
    val people = peopleOf(r, range)
    return if (p > 0) "Con $hits, superas al $p % de $people." else "Con $hits, todavía no superas a ninguna de $people."
  }

  /** La mitad de la referencia: el umbral de las semanas seguidas (Tarea 229: antes «En órbita o más arriba», el umbral de una división por juego; el 50 es el mismo y ya no usa el nombre de ninguna). */
  const val HALF_LINE = 50

  /**
   * Las semanas seguidas, hasta esta, con percentil 50 o más (la mitad de arriba de la referencia). Una semana sin marca propia o sin 50 personas en la franja no tiene punto y corta la cuenta: no se sabe cómo fue.
   */
  fun streak(points: List<WeekPoint>): Int {
    val above = points.filter { it.percentile >= HALF_LINE }.map { it.weeksAgo }.toSet()
    var n = 0
    while (n in above) n++
    return n
  }

  /**
   * «Llevas 3 semanas seguidas en la mitad de arriba de la referencia estimada.» (Tarea 229; antes «…En órbita o más arriba»); con una sola, «Esta semana empiezas una racha en la mitad de arriba de la referencia estimada.»; sin racha no dice nada (nunca «perdiste»: cuando se corta, la página calla).
   * Con personas reales (futuro): «…en la mitad de arriba de las personas de tu edad.».
   */
  fun streakSentence(n: Int, estimated: Boolean = true): String? {
    val where = if (estimated) "en la mitad de arriba de la referencia estimada" else "en la mitad de arriba de las personas de tu edad"
    return when {
      n >= 2 -> "Llevas $n semanas seguidas $where."
      n == 1 -> "Esta semana empiezas una racha $where."
      else -> null
    }
  }

  /** El rótulo izquierdo del eje de las semanas: «hace 3 semanas» (la más vieja con punto); null si la más vieja es esta (no hay de qué separar). */
  fun weeksAxisLeft(points: List<WeekPoint>): String? {
    val oldest = points.firstOrNull()?.weeksAgo ?: return null
    return if (oldest == 0) null else weeksAgoText(oldest)
  }

  /** La línea que dice qué clase se usó cuando no es la normal: «Juntamos dos franjas porque todavía somos pocos.» (solo con las franjas vecinas; las otras ya lo dicen en la frase). */
  fun scopeLine(r: Ranked): String? = if (r.scope == Scope.NEIGHBORS_4_WEEKS) MERGED_LINE else null

  /** Lo que lee el lector de pantalla de la campana (el dibujo no habla; las dos frases de abajo son el contenido): «Campana con las marcas de las 128 personas. Tu mejor marca de esta semana, 43 aciertos, está marcada.» */
  fun bellSpoken(r: Ranked, range: AgeRange? = null, onHalf: Boolean = false): String {
    val end = if (onHalf) "está en la mitad." else "está marcada."
    return if (r.estimated) "Campana de la referencia estimada para ${range?.let { EstimatedReference.tableRangeOf(it).label } ?: "tu edad"}. Tu mejor marca de esta semana, ${TimeTrialDay.hitsText(r.mark)}, $end"
    else "Campana con las marcas de las ${r.distribution.people} personas. Tu mejor marca de esta semana, ${TimeTrialDay.hitsText(r.mark)}, $end"
  }

  /** «Todavía somos pocos: 23 personas de 45 a 54 años jugaron Tinta o Palabra esta semana. Cuando sean 50, verás aquí tu lugar entre ellas.» (con una sola: «1 persona … jugó …»). */
  fun fewSentence(few: Place.Few, range: AgeRange, gameTitle: String): String {
    val people = if (few.own == 1) "1 persona" else "${few.own} personas"
    val played = if (few.own == 1) "jugó" else "jugaron"
    return "$FEW_TITLE: $people de ${range.label} $played $gameTitle esta semana. Cuando sean $FEW_BAR_GOAL, verás aquí tu lugar entre ellas."
  }

  /** La barra de «Todavía somos pocos»: «23 de 50 personas». */
  fun fewBarText(own: Int): String = "$own de $FEW_BAR_GOAL personas"

  /**
   * «Mientras tanto, comparada con todas las edades de esta semana, tu mejor marca, 43 aciertos, supera al 51 % de las personas.» (el MISMO número que la fila «Todas las edades» de «Comparado con otras edades»); solo si esta semana no llegan a 50: «Mientras tanto, comparada con todas las edades de las últimas 4 semanas, tu mejor marca de esta semana, 43 aciertos, supera al 53 % de las personas.» y entonces esa fila dice «todavía somos pocos».
   */
  fun meanwhileSentence(r: Ranked): String {
    val n = r.percentile
    if (r.scope == Scope.ALL_WEEK) return "Mientras tanto, comparada con todas las edades de esta semana, tu mejor marca, ${TimeTrialDay.hitsText(r.mark)}, supera al $n % de las personas."
    return "Mientras tanto, comparada con todas las edades de las últimas $RECENT_WEEKS semanas, tu mejor marca de esta semana, ${TimeTrialDay.hitsText(r.mark)}, supera al $n % de las personas."
  }

  /** Sin marca esta semana: qué hacer para ver el lugar (sin números inventados). */
  fun noMarkSentence(gameTitle: String, estimated: Boolean = false): String =
    "Todavía no tienes una marca esta semana en $gameTitle. Juega su Contrarreloj para ver tu lugar " + if (estimated) "en la referencia estimada para tu edad." else "entre personas de tu edad."

  // ------------------------------------------------------------------ tu percentil en las últimas 7 semanas

  /** El percentil de UNA semana ([weeksAgo], 0 = esta) de la persona: solo existe con marca propia esa semana y con 50 personas o más en su franja. */
  data class WeekPoint(val weeksAgo: Int, val mark: Int, val percentile: Int)

  /**
   * Los puntos de las últimas [WEEKS] semanas, de la más vieja a la de esta semana. [bests] = la mejor marca de cada semana (índice = semanas atrás, 0 a 6; null = sin partida esa semana). Una semana sin marca propia o con menos de 50 personas en la franja queda sin punto
   * (nada se rellena): el gráfico deja ese hueco y la frase cuenta solo las que hay.
   */
  fun weekPoints(source: Source, game: String, range: AgeRange, bests: List<Int?>): List<WeekPoint> =
    (WEEKS - 1 downTo 0).mapNotNull { ago ->
      val mark = bests.getOrNull(ago) ?: return@mapNotNull null
      val d = source.distribution(game, listOf(range), ago)
      if (!d.enough) null else WeekPoint(ago, mark, percentile(mark, d))
    }

  /** Una semana está «por encima de la mitad de tu franja» cuando supera al 50 % o más. */
  private fun above(p: WeekPoint) = p.percentile >= 50

  /** «En 6 de las 7 semanas estuviste por encima de la mitad de tu franja.» (todas: «En las 4 semanas …»; ninguna: «En ninguna de las 4 semanas …»; una sola semana: «Tu única semana con marca estuvo …»). */
  fun weeksSentence(points: List<WeekPoint>): String? {
    val n = points.size
    if (n == 0) return null
    val k = points.count(::above)
    return when {
      n == 1 && k == 1 -> "Tu única semana con marca estuvo por encima de la mitad de tu franja."
      n == 1 -> "Tu única semana con marca estuvo en la mitad de abajo de tu franja."
      k == n -> "En las $n semanas estuviste por encima de la mitad de tu franja."
      k == 0 -> "En ninguna de las $n semanas estuviste por encima de la mitad de tu franja."
      else -> "En $k de las $n semanas estuviste por encima de la mitad de tu franja."
    }
  }

  /**
   * Lo que lee el lector de pantalla del gráfico de las semanas, semana por semana (Tarea 213: la misma forma que la cifra, «superas al N %»): «Semana a semana, comparado con la referencia estimada: hace 6 semanas, superas al 52 % de ella; …; esta semana, superas al 63 % de ella.»
   * Con personas reales (futuro): «…, superas al 52 % de las personas de tu edad».
   */
  fun weeksSpoken(points: List<WeekPoint>, estimated: Boolean = false): String =
    "$WEEKS_TITLE" + (if (estimated) ", comparado con la referencia estimada" else "") + ": " + points.joinToString("; ") { pt ->
      val place = weeksAgoText(pt.weeksAgo); val n = pt.percentile
      if (estimated) "$place, superas al $n % de ella" else "$place, superas al $n % de las personas de tu edad"
    } + "."

  fun weeksAgoText(weeksAgo: Int): String = when (weeksAgo) {
    0 -> "esta semana"
    1 -> "hace 1 semana"
    else -> "hace $weeksAgo semanas"
  }

  // ------------------------------------------------------------------ comparado con otras edades

  /** Una fila de «Otras edades»: [label] (la franja o «Todas las edades») con su [percentile] (solo hay fila con 50 personas o más), [own] = la franja de la persona. */
  data class OtherRow(val label: String, val percentile: Int, val own: Boolean, val people: Int) {
    /** «Supera al 63 %» (Tarea 213: antes «63 de cada 100»): la forma corta de la cifra; de qué dice la frase de entrada ([othersLead]). */
    val value: String get() { val p = percentile; return "Supera al $p %" }
  }

  /** Las filas con datos de «Otras edades»; [hidden] = alguna franja (o todas las edades) todavía no llega a 50 personas y la losa cierra con [OTHERS_HIDDEN]. */
  data class Others(val rows: List<OtherRow>, val all: OtherRow?, val hidden: Boolean)

  /**
   * La marca de esta semana contra cada franja (las seis de 18 a 29 a 75 o más; la de 14 a 17 no lleva fila: comparte la tabla de 18 a 29 y sería la de 18 a 29 repetida) y contra todas las edades, esta semana: solo las clases con 50 personas o más tienen fila, con su «Supera al N %» (Tarea 203b: ya no hay filas de «todavía somos pocos»,
   * que en los primeros meses serían casi todas); la propia lleva «(tu franja)». [Others.hidden] avisa que falta alguna y null = no hay ninguna fila que mostrar (la losa no va).
   */
  fun others(source: Source, game: String, range: AgeRange, mark: Int): Others? {
    var anyEstimated = false
    fun row(label: String, ranges: List<AgeRange>?, own: Boolean): OtherRow? {
      val d = source.distribution(game, ranges, 0)
      if (d.enough && d.estimated) anyEstimated = true
      return if (d.enough) OtherRow(if (own) "$label (tu franja)" else label, percentile(mark, d), own, if (d.estimated) 0 else d.people) else null
    }
    // 14 a 17 comparte la tabla de 18 a 29: no lleva fila; y a quien tiene 14 a 17 tampoco se le muestra la de 18 a 29, que es SU referencia y repetiría el número de la cifra (Tarea 217, Fable M1)
    val ages = AgeRange.entries.filter { it != AgeRange.R14_17 && !(range == AgeRange.R14_17 && it == EstimatedReference.tableRangeOf(range)) }
    val rows = ages.mapNotNull { row(it.label, listOf(it), it == range) }
    val all = row("Todas las edades", null, false)
    if (rows.isEmpty() && all == null) return null
    // con la referencia estimada no hay fila de «Todas las edades» (no hay población que ponderar) y no se dice nada de lo que falta: cada franja siempre tiene su fila
    return Others(rows, all, hidden = rows.size < ages.size || (all == null && !anyEstimated))
  }

  /**
   * La frase de entrada de «Otras edades» (Tarea 213: antes «De cada 100 marcas de la referencia estimada de cada edad, tu mejor marca deja detrás a:»): dice de qué es cada «Supera al N %» de las filas: «Tu mejor marca de esta semana, frente a la referencia estimada de cada edad:»
   * (con personas: «…, frente a las personas que jugaron Tinta o Palabra esta semana:»). La marca ya está en la cifra de arriba: no se repite.
   */
  fun othersLead(gameTitle: String, estimated: Boolean = false): String =
    if (estimated) "Tu mejor marca de esta semana, frente a la referencia estimada de cada edad:" else "Tu mejor marca de esta semana, frente a las personas que jugaron $gameTitle esta semana:"

  /** Lo que lee el lector de pantalla de una fila: «45 a 54 años (tu franja), Supera al 63 %». */
  fun rowSpoken(row: OtherRow): String = "${row.label}, ${row.value}"

  // ------------------------------------------------------------------ la sección entera

  /** Todo lo que dibuja la sección «Comparado con personas de tu edad» de la página, ya calculado. [others] es null cuando no hay marca esta semana o no hay con quién. */
  data class Section(
    val range: AgeRange,
    val gameTitle: String,
    val mark: Int?,
    val place: Place,
    val weeks: List<WeekPoint>,
    val others: Others?,
    /** La referencia con que se compara es la ESTIMADA (robots que juegan la pista real). */
    val estimated: Boolean = false,
    /** Son los datos de muestra de depuración (`SampleComparison`): la página lo dice en vez de la frase de la referencia estimada. */
    val sample: Boolean = false
  )

  /** La mejor marca de [results] (ya de UN juego y de la pista de hoy) en la semana que empezó [weeksAgo] semanas atrás; null si no hubo partida. */
  fun bestOfWeek(results: List<TimeTrialResult>, now: Long, weeksAgo: Int): Int? {
    val from = Week.weekStart(now, weeksAgo)
    val to = if (weeksAgo == 0) Long.MAX_VALUE else Week.weekStart(now, weeksAgo - 1)
    return results.filter { it.timestamp in from until to }.maxOfOrNull { it.mark }
  }

  /**
   * La sección de comparación para el juego [game] de una persona de [range]: su lugar esta semana, su percentil en las últimas 7 semanas y las otras edades. [results] = TODAS las partidas del Contrarreloj; se usan las del juego con la pista de hoy (`TimeTrialDay.ofGame`), igual que el resto de la página.
   */
  fun section(source: Source, game: String, gameTitle: String, range: AgeRange, results: List<TimeTrialResult>, now: Long): Section {
    val mine = TimeTrialDay.ofGame(results, game)
    val bests = List(WEEKS) { bestOfWeek(mine, now, it) }
    val mark = bests[0]
    val place = place(source, game, range, mark)
    val others = if (mark != null) others(source, game, range, mark) else null
    val estimated = source.distribution(game, listOf(range), 0).estimated
    return Section(range, gameTitle, mark, place, weekPoints(source, game, range, bests), others, estimated, sample = source is SampleComparison)
  }

  // ------------------------------------------------------------------ la forma de la campana

  /**
   * El histograma suavizado SOLO para dibujar (1-2-1 entre 4: cada tramo pesa la mitad y sus vecinos un cuarto; en los bordes el tramo se repite): la silueta sin escalones. El percentil NO sale de aquí, sino de los tramos sin suavizar ([percentile]).
   */
  fun smoothed(bins: List<Int>): List<Double> = bins.indices.map { i ->
    val left = bins[maxOf(i - 1, 0)]
    val right = bins[min(i + 1, bins.lastIndex)]
    (left + 2.0 * bins[i] + right) / 4.0
  }

  /**
   * La altura de la campana en [x] (en aciertos, de 0 a [TOP_MARK]): una curva suave (Catmull-Rom) por los centros de los tramos suavizados, nunca por debajo de 0. El dibujo y la barra «Tú» usan la MISMA función, así la barra llega justo a la curva.
   */
  fun bellHeight(smoothed: List<Double>, x: Double, binWidth: Int = BIN_WIDTH): Double {
    val u = x / binWidth - 0.5                                // posición entre centros: el tramo i tiene su centro en u = i
    val i = Math.floor(u).toInt().coerceIn(0, smoothed.size - 2)
    val t = (u - i).coerceIn(0.0, 1.0)
    fun at(k: Int) = smoothed[k.coerceIn(0, smoothed.lastIndex)]
    val p0 = at(i - 1); val p1 = at(i); val p2 = at(i + 1); val p3 = at(i + 2)
    val y = 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t)
    return y.coerceAtLeast(0.0)
  }

  /** Dónde cae [mark] en el eje de la campana (de 0 a [TOP_MARK]): al centro de su marca; el último tramo («75 o más») se topa en el borde derecho. */
  fun markX(mark: Int, topMark: Int = TOP_MARK): Double = (mark.coerceIn(0, topMark - 1) + 0.5).coerceAtMost(topMark.toDouble())

  /**
   * Los rótulos del eje de la campana (Tarea 205): «0 aciertos», dos intermedios y donde EMPIEZA el último tramo («75 o más»), a un tercio y dos tercios de él. Con 16 tramos de 5 (¿Verdad o disparate?) son 0 · 25 · 50 · 75; con 20 de 5 (Tinta o Palabra, desde la Tarea 206), 0 · 32 · 63 · 95; con 16 de 3 (Rescate, desde la Tarea 208), 0 · 15 · 30 · 45; con 20 de 2 (Acoplamiento), 0 · 13 · 25 · 38.
   */
  fun axisTicks(d: Distribution): List<Int> {
    val end = d.lastStart
    return listOf(0, (end / 3.0).let { Math.round(it).toInt() }, (end * 2 / 3.0).let { Math.round(it).toInt() }, end)
  }

  /** El hueco entre rótulos que pide el dibujo antes de omitir uno (14 sp): 8 dp. */
  const val AXIS_GAP_DP = 8

  /** El aire a cada lado de la caja de un rótulo del eje en que no se deja caer la raya de la mitad ni la barra «Tú» (Tarea 207): 2 dp. */
  const val AXIS_OBSTACLE_MARGIN_DP = 2.0

  /**
   * Qué rótulos del eje de la campana se dibujan (Tarea 207; Opus, 1-nov). Hay cuatro, en este orden: 0 = «0 aciertos» (izquierda), 1 y 2 = los dos intermedios (a un tercio y a dos tercios del último tramo) y 3 = «N o más» (derecha).
   * Los dos de los EXTREMOS se dibujan siempre. Un INTERMEDIO se omite si no queda [gap] entre él y uno ya dibujado (la regla de siempre: con letra 2,0 o poco ancho el del medio no cabe) o si algún estorbo de [obstacles] —la raya de la mitad y la barra «Tú»,
   * cada uno como `(centro, ancho)`— cae dentro de su caja de texto más [margin] a cada lado: un «63» bajo una raya que está en 65 se lee «la mitad es 63», y eso es falso. Prioridad: los extremos, después el intermedio de la derecha y al final el de la izquierda.
   * Todo en las MISMAS unidades (dp o píxeles) y medido con el ancho real del dibujo, como [barCoversHalf]; si la raya no se dibuja porque la tapa la barra, quien llama solo pasa la barra. Devuelve los índices dibujados, de izquierda a derecha.
   */
  fun axisLabelsShown(starts: List<Double>, widths: List<Double>, gap: Double, obstacles: List<Pair<Double, Double>>, margin: Double): List<Int> {
    val shown = mutableListOf(0, 3)
    for (i in listOf(2, 1)) {
      val left = starts[i]
      val right = left + widths[i]
      val fits = shown.all { j -> left >= starts[j] + widths[j] + gap || starts[j] >= right + gap }
      val clear = obstacles.none { (center, width) -> center + width / 2 > left - margin && center - width / 2 < right + margin }
      if (fits && clear) shown += i
    }
    return shown.sorted()
  }

  /**
   * Lo mismo con lo que sabe la campana: los rótulos de [ticks] ([axisTicks]) y su ancho medido, en dp, el [widthDp] interior del dibujo, la marca [mark] y si la barra «Tú» tapa la raya ([onHalf], [barCoversHalf]; entonces cuenta solo la barra).
   * [labelWidthsDp] = el ancho de «0 aciertos», de cada intermedio y de «N o más».
   */
  fun axisLabelsFor(d: Distribution, mark: Int, onHalf: Boolean, widthDp: Double, labelWidthsDp: List<Double>): List<Int> {
    val ticks = axisTicks(d)
    val top = d.topMark
    val starts = listOf(0.0, widthDp * ticks[1] / top - labelWidthsDp[1] / 2, widthDp * ticks[2] / top - labelWidthsDp[2] / 2, widthDp - labelWidthsDp[3])
    val obstacles = buildList {
      add(widthDp * markX(mark, top) / top to BELL_BAR_DP)
      if (!onHalf) add(widthDp * markX(d.p50, top) / top to BELL_LINE_DP)
    }
    return axisLabelsShown(starts, labelWidthsDp, AXIS_GAP_DP.toDouble(), obstacles, AXIS_OBSTACLE_MARGIN_DP)
  }
}
