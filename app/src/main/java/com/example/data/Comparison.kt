package com.example.data

import com.example.Features
import com.example.model.AgeRange
import kotlin.math.min

/**
 * Tu lugar entre personas de tu edad (Tarea 202; `auditoria-fable/nubi-plus-comparacion.md` §1, §4 y §11, opción B), LÓGICA PURA: el percentil desde el histograma SIN suavizar, la frase en «de cada 100», la división con nombre, las reglas de «pocos», el percentil semana a semana,
 * «otras edades» y la forma de la campana; desde la Tarea 203, la cifra protagonista, la escalera de divisiones con su primer acierto, «te faltan», la racha de semanas y la frase gratis. No sabe de pantallas ni de dónde salen las marcas de las otras personas: las pide a una [Source] (hoy los datos de muestra de `SampleComparison`, solo en depuración; el día que haya servidor, sus documentos semanales).
 *
 * Lo que NO hace, a propósito: nunca habla de «capacidad», «edad mental», «ranking» ni «velocidad»; no hay lista de nombres ni «los 10 mejores»; la franja de 13 a 17 años queda fuera mientras [Features.COMPARE_MINORS] sea false; nunca inventa personas (una clase de referencia con menos de [MIN_PEOPLE] se dice «todavía somos pocos»).
 */
object Comparison {
  /** Personas que hacen falta en una clase de referencia (franja, juego y semana) para decir un lugar (§4). */
  const val MIN_PEOPLE = 50

  /** El histograma es de 16 tramos de 5 aciertos: 0 a 4, 5 a 9, …, 70 a 74 y «75 o más». */
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
  const val WEEKS_TITLE = "Tu percentil semanal"
  const val OTHERS_TITLE = "Otras edades"
  const val LADDER_TITLE = "La escalera de tu franja"
  /** Cierra «Otras edades» cuando alguna franja (o todas las edades) todavía no llega a 50 personas: no se muestran filas vacías. */
  const val OTHERS_HIDDEN = "Las demás edades aparecen cuando lleguen a 50 personas."
  /** El rótulo de la raya de 50 en «Tu percentil semanal» (14 sp, como los ejes). */
  const val WEEKS_HALF_LABEL = "la mitad"
  const val WEEKS_AXIS_RIGHT = "esta semana"
  const val BELL_AXIS_LEFT = "0 aciertos"
  const val BELL_AXIS_RIGHT = "75 o más"
  val BELL_AXIS_MIDDLE = listOf(25, 50)
  const val MERGED_LINE = "Juntamos dos franjas porque todavía somos pocos."
  const val FEW_TITLE = "Todavía somos pocos"
  const val FEW_BAR_GOAL = MIN_PEOPLE
  const val HERO_UNIT = "de cada 100"
  const val CLOSES = "La semana cierra el domingo."
  const val BELL_TITLE = "La campana de tu edad"
  /** Sin franja de edad guardada (Tarea 205b): la página dice dónde elegirla. Es la ruta real: Tú › «Más ajustes» › «Juego» › «Rango de edad». */
  const val AGE_HINT = "Para compararte con una referencia estimada para tu edad, elige tu edad en Tú › Más ajustes › Juego › Rango de edad."
  const val BELL_CAPTION = "La línea marca la mitad de las personas."

  // ---- la REFERENCIA ESTIMADA (Tarea 205; `auditoria-fable/referencia-estimada.md` §4 y las decisiones de Opus del 1-nov): siempre dicha; en cada losa solo «referencia estimada»; nunca se llama «personas»
  const val ESTIMATED_BELL_CAPTION = "La línea marca la mitad de la referencia estimada."
  /** Bajo el gráfico de las semanas (con la referencia estimada). */
  const val WEEKS_ESTIMATED_NOTE = "Semanas comparadas con la referencia estimada."

  /** La frase larga, UNA vez por página, bajo el rótulo de la comparación (reemplaza al aviso de muestra): qué es y que no son personas. */
  fun estimatedNotice(range: AgeRange): String =
    "Referencia estimada para ${range.label}: sale de estudios por edad y de un programa que jugó esta misma pista en un computador, no en tu teléfono. No son personas."

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
   * Depende del ancho porque cada acierto mide `widthDp / topMark` dp: en Rescate relámpago (24 tramos de a 1) un acierto de diferencia ya las separa; en Tinta o Palabra, a 360 dp, hacen falta dos o más (~3,4 dp por acierto).
   */
  fun barCoversHalf(mark: Int, d: Distribution, widthDp: Double): Boolean {
    val gapDp = Math.abs(markX(mark, d.topMark) - markX(d.p50, d.topMark)) * widthDp / d.topMark
    return gapDp < BELL_BAR_DP / 2 + BELL_LINE_DP / 2 + BELL_CLEARANCE_DP
  }

  // ------------------------------------------------------------------ la distribución

  /**
   * Las mejores marcas semanales de UNA clase de referencia (juego, pista, semana o semanas, franja o franjas): cuántas [people] y, en [bins], cuántas cayeron en cada tramo de 5 aciertos (16 tramos, sin suavizar); [p25], [p50] y [p75] son los cuartiles en aciertos, dichos tal cual por quien arma la distribución
   * (el servidor los calcula con las marcas crudas; en los datos de muestra salen de su tabla). Con menos de [MIN_PEOPLE] personas no alcanza para decir un lugar ([enough]).
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

    /** Donde EMPIEZA el último tramo, el de «o más» (75 en 16 tramos de 5; 23 en 24 de 1). */
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
   * La franja con que se compara una persona: la suya; si no la eligió, en las builds de depuración con datos de muestra la de 45 a 54 años (para poder ver la sección) y en cualquier otra, ninguna (sin franja no hay con quién compararla). Con de 13 a 17 años, ninguna mientras [Features.COMPARE_MINORS] sea false:
   * esa persona no ve la sección (§1.4 y §3.6).
   */
  fun rangeFor(stored: AgeRange?, sample: Boolean): AgeRange? {
    val range = stored ?: if (sample) AgeRange.R45_54 else null
    return range?.takeIf { it != AgeRange.R13_17 || Features.COMPARE_MINORS }
  }

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

  /** Las cinco divisiones con nombre de la fase 2 (`docs/contrarreloj.md` §8): por tramos fijos del percentil. Van DESPUÉS del número y nunca en su lugar. */
  enum class Division(val label: String, val from: Int) {
    PLATFORM("En la plataforma", 0),
    TAKEOFF("Despegando", 25),
    ORBIT("En órbita", 50),
    MOON("En la Luna", 75),
    STARS("Entre estrellas", 90)
  }

  fun division(percentile: Int): Division = Division.entries.last { percentile >= it.from }

  // ------------------------------------------------------------------ las franjas vecinas

  private val GROUPS: List<List<AgeRange>> = listOf(
    listOf(AgeRange.R13_17, AgeRange.R18_29),
    listOf(AgeRange.R30_44, AgeRange.R45_54),
    listOf(AgeRange.R55_64, AgeRange.R65_74),
    listOf(AgeRange.R65_74, AgeRange.R75_PLUS)
  )

  /**
   * Las franjas con que se junta la de [range] cuando todavía somos pocos (§4, paso 3: 13 a 29 · 30 a 54 · 55 a 74 · 65 o más): la primera pareja que la trae, sin la de 13 a 17 mientras [Features.COMPARE_MINORS] sea false. Si queda sola (18 a 29 sin menores), null: no hay franja vecina con quien juntarla.
   */
  fun neighborGroup(range: AgeRange): List<AgeRange>? =
    GROUPS.first { range in it }.filter { it != AgeRange.R13_17 || Features.COMPARE_MINORS }.takeIf { it.size > 1 }

  /** «30 a 54 años», «55 a 74 años», «65 años o más» o «13 a 29 años». */
  fun groupLabel(group: List<AgeRange>): String = when (group.last()) {
    AgeRange.R75_PLUS -> "65 años o más"
    AgeRange.R45_54 -> "30 a 54 años"
    AgeRange.R65_74 -> "55 a 74 años"
    else -> "13 a 29 años"
  }

  // ------------------------------------------------------------------ tu lugar esta semana

  /** Con quiénes se compara una marca: la franja esta semana (lo normal), la franja las últimas 4 semanas, las franjas vecinas las últimas 4 semanas, todas las edades ESTA semana o todas las edades las últimas 4 semanas (estas dos, solo en «Mientras tanto»). */
  enum class Scope { OWN_WEEK, OWN_4_WEEKS, NEIGHBORS_4_WEEKS, ALL_WEEK, ALL_4_WEEKS }

  /** Un lugar dicho: la [mark] de esta semana contra [distribution] (de [ranges], null = todas las edades), con su [percentile] y su [division]. */
  data class Ranked(val mark: Int, val percentile: Int, val division: Division, val scope: Scope, val ranges: List<AgeRange>?, val distribution: Distribution) {
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
    fun ranked(scope: Scope, ranges: List<AgeRange>?, d: Distribution) = Ranked(mark, percentile(mark, d), division(percentile(mark, d)), scope, ranges, d)
    val own = source.distribution(game, listOf(range), 0)
    if (own.enough) return Place.Found(ranked(Scope.OWN_WEEK, listOf(range), own))
    source.distribution(game, listOf(range), 0, RECENT_WEEKS).takeIf { it.enough }?.let { return Place.Found(ranked(Scope.OWN_4_WEEKS, listOf(range), it)) }
    neighborGroup(range)?.let { group -> source.distribution(game, group, 0, RECENT_WEEKS).takeIf { it.enough }?.let { return Place.Found(ranked(Scope.NEIGHBORS_4_WEEKS, group, it)) } }
    val allWeek = source.distribution(game, null, 0).takeIf { it.enough }?.let { ranked(Scope.ALL_WEEK, null, it) }
    val meanwhile = allWeek ?: source.distribution(game, null, 0, RECENT_WEEKS).takeIf { it.enough }?.let { ranked(Scope.ALL_4_WEEKS, null, it) }
    return Place.Few(own.people, meanwhile)
  }

  // ------------------------------------------------------------------ la cifra protagonista y la escalera de divisiones (Tarea 203; `auditoria-fable/contrarreloj-rediseno.md` §3, D2)

  /** «63 de cada 100»: la cifra protagonista, siempre con su unidad (nunca un número suelto). */
  fun heroFigure(r: Ranked): String {
    val p = r.percentile
    return "$p $HERO_UNIT"
  }

  /** Quiénes son «las personas» de la cifra: «de 45 a 54 años», «de 30 a 54 años» o «de todas las edades» (la clase de referencia siempre completa). */
  private fun whoText(r: Ranked, range: AgeRange): String = when (r.scope) {
    Scope.OWN_WEEK, Scope.OWN_4_WEEKS -> "de ${range.label}"
    Scope.NEIGHBORS_4_WEEKS -> "de ${groupLabel(r.ranges ?: listOf(range))}"
    Scope.ALL_WEEK, Scope.ALL_4_WEEKS -> "de todas las edades"
  }

  /**
   * La frase que lee la cifra: «personas de 45 a 54 años quedan detrás de tu mejor marca de esta semana en Tinta o Palabra.» (con las 4 semanas o las franjas juntas lo dice: «personas de 30 a 54 años que jugaron Tinta o Palabra en las últimas 4 semanas quedan detrás de tu mejor marca de esta semana.»).
   * Con letra muy grande la unidad pasa a la frase ([withUnit]: «de cada 100 personas de 45 a 54 años quedan detrás…») y la cifra queda sola.
   */
  fun heroCaption(r: Ranked, range: AgeRange, gameTitle: String, withUnit: Boolean = false): String {
    if (r.estimated) {
      val estimatedBody = "marcas de la referencia estimada para ${range.label} quedan detrás de tu mejor marca de esta semana en $gameTitle."
      return if (withUnit) "$HERO_UNIT $estimatedBody" else estimatedBody
    }
    val who = whoText(r, range)
    val body = if (r.scope == Scope.OWN_WEEK || r.scope == Scope.ALL_WEEK) "personas $who quedan detrás de tu mejor marca de esta semana en $gameTitle."
    else "personas $who que jugaron $gameTitle en las últimas $RECENT_WEEKS semanas quedan detrás de tu mejor marca de esta semana."
    return if (withUnit) "$HERO_UNIT $body" else body
  }

  /** «Percentil 63», junto a la división (Ricardo eligió decirlo así el 31-oct: la frase, el percentil y la división). */
  fun percentileLabel(r: Ranked): String {
    val p = r.percentile
    return "Percentil $p"
  }

  /** Lo que lee el lector de pantalla de la tarjeta de la cifra, de corrido: «63 de cada 100 personas de 45 a 54 años quedan detrás de tu mejor marca de esta semana en Tinta o Palabra. En órbita. Percentil 63.» */
  fun heroSpoken(r: Ranked, range: AgeRange, gameTitle: String): String {
    val figure = heroFigure(r)
    val caption = heroCaption(r, range, gameTitle)
    val division = r.division.label
    val label = percentileLabel(r)
    return "$figure $caption $division. $label."
  }

  /** Un peldaño de la escalera: la división y el PRIMER acierto con que se llega a ella. */
  data class Rung(val division: Division, val from: Int)

  /**
   * Los peldaños de la escalera de [d], de abajo hacia arriba: el primer acierto con que el percentil de la marca llega a 25, 50, 75 y 90 (la misma regla de [percentile], contando sobre los tramos SIN suavizar; así la escalera y la división de la persona nunca se contradicen), y la plataforma desde 0.
   * Una división a la que no se llega (el percentil no alcanza ni con 75 aciertos o más) o que queda vacía (su primer acierto es el de la siguiente) no tiene peldaño.
   */
  fun thresholds(d: Distribution): List<Rung> {
    val all = Division.entries.mapNotNull { div -> (if (div == Division.PLATFORM) 0 else firstMarkReaching(div.from, d))?.let { Rung(div, it) } }
    return all.filterIndexed { i, rung -> i == all.lastIndex || rung.from < all[i + 1].from }
  }

  /**
   * La escalera de una marca: los [rungs] de ARRIBA hacia abajo, el peldaño de la persona ([current]), el siguiente ([next], null en la división más alta), cuántos aciertos le faltan ([missing], siempre 1 o más) y qué parte del tramo entre los dos lleva recorrida ([progress], de 0 a 1; null en la más alta).
   */
  data class Ladder(val rungs: List<Rung>, val current: Rung?, val next: Rung?, val missing: Int?, val progress: Float?)

  fun ladder(r: Ranked): Ladder {
    val up = thresholds(r.distribution)
    val current = up.firstOrNull { it.division == r.division }
    val next = current?.let { up.getOrNull(up.indexOf(it) + 1) }
    val missing = next?.let { (it.from - r.mark).coerceAtLeast(1) }
    val progress = if (current != null && next != null) ((r.mark - current.from).toFloat() / (next.from - current.from)).coerceIn(0f, 1f) else null
    return Ladder(up.asReversed(), current, next, missing, progress)
  }

  /**
   * Lo que dice cada fila de la escalera: «desde 39 aciertos» (el primer acierto de la división). La de abajo, En la plataforma, empieza en 0 y «desde 0 aciertos» no dice nada: dice hasta dónde llega, «hasta 30 aciertos» (el primer acierto de la división de [above], la de arriba, menos uno; Tarea 203b).
   * Sin división de arriba (la escalera tiene un solo peldaño) queda «desde 0 aciertos».
   */
  fun rungText(rung: Rung, above: Rung?): String =
    if (rung.division == Division.PLATFORM && above != null) "hasta ${TimeTrialDay.hitsText(above.from - 1)}" else "desde ${TimeTrialDay.hitsText(rung.from)}"

  /** «Tú · 43 aciertos»: la fila de la persona, en lima. */
  fun youRow(r: Ranked): String = "Tú · ${TimeTrialDay.hitsText(r.mark)}"

  /** «Te faltan 7 aciertos para En la Luna.» («Te falta 1 acierto…»); en la división más alta, «Estás en la división más alta de tu franja esta semana.». */
  fun needSentence(l: Ladder, r: Ranked): String {
    val next = l.next
    val missing = l.missing
    if (next == null || missing == null) return if (r.scope == Scope.OWN_WEEK) "Estás en la división más alta de tu franja esta semana." else "Estás en la división más alta de tu franja."
    return "${if (missing == 1) "Te falta" else "Te faltan"} ${TimeTrialDay.hitsText(missing)} para ${next.division.label}."
  }

  /**
   * «Con las 128 personas de tu franja que jugaron esta semana.» (la clase de la escalera, dicha una sola vez; la franja exacta y el juego ya están en la cifra de arriba). Con las 4 semanas lo dice: «…que jugaron en las últimas 4 semanas.»;
   * con las franjas juntas o todas las edades dice cuáles («Con las 240 personas de 30 a 54 años que jugaron en las últimas 4 semanas.»).
   */
  fun ladderNote(r: Ranked, range: AgeRange): String {
    if (r.estimated) return "Peldaños de la referencia estimada para ${range.label}; no cambian de semana en semana."
    val whenText = if (r.scope == Scope.OWN_WEEK || r.scope == Scope.ALL_WEEK) "esta semana" else "en las últimas $RECENT_WEEKS semanas"
    val who = if (r.scope == Scope.OWN_WEEK || r.scope == Scope.OWN_4_WEEKS) "de tu franja" else whoText(r, range)
    return "Con las ${r.distribution.people} personas $who que jugaron $whenText."
  }

  /**
   * Las semanas seguidas, hasta esta, con percentil 50 o más («En órbita o más arriba», el umbral fijo de la división En órbita). Una semana sin marca propia o sin 50 personas en la franja no tiene punto y corta la cuenta: no se sabe cómo fue.
   */
  fun streak(points: List<WeekPoint>): Int {
    val above = points.filter { it.percentile >= Division.ORBIT.from }.map { it.weeksAgo }.toSet()
    var n = 0
    while (n in above) n++
    return n
  }

  /** «Llevas 3 semanas seguidas En órbita o más arriba.»; con una sola, «Esta semana empiezas una racha En órbita o más arriba.»; sin racha no dice nada (nunca «perdiste»: cuando se corta, la página calla). */
  fun streakSentence(n: Int): String? = when {
    n >= 2 -> "Llevas $n semanas seguidas ${Division.ORBIT.label} o más arriba."
    n == 1 -> "Esta semana empiezas una racha ${Division.ORBIT.label} o más arriba."
    else -> null
  }

  /**
   * LO GRATIS (Ricardo, 31-oct): la división y la meta, sin cifra de percentil, sin escalera y sin otras edades: «Esta semana estás En órbita. Te faltan 7 aciertos para En la Luna.» (en la más alta: «Esta semana estás Entre estrellas, la división más alta de tu franja.»).
   * Solo con un lugar dicho ([Place.Found]); con «pocos» o sin marca no hay nada que decir.
   */
  fun freeSentence(place: Place): String? {
    val r = (place as? Place.Found)?.ranked ?: return null
    val l = ladder(r)
    if (r.estimated) {
      if (l.next == null || l.missing == null) return "Esta semana estás ${r.division.label} en la referencia estimada para tu edad, la división más alta."
      return "Esta semana estás ${r.division.label} en la referencia estimada para tu edad. ${needSentence(l, r)}"
    }
    if (l.next == null || l.missing == null) return "Esta semana estás ${r.division.label}, la división más alta de tu franja."
    return "Esta semana estás ${r.division.label}. ${needSentence(l, r)}"
  }

  /** La frase gratis de la puerta de Avance: la de [freeSentence] con el lugar de esta semana de la persona en [game] (null sin lugar dicho). */
  fun freeSentenceFor(source: Source, game: String, range: AgeRange, results: List<TimeTrialResult>, now: Long): String? =
    if (!source.covers(game)) null else freeSentence(place(source, game, range, bestOfWeek(TimeTrialDay.ofGame(results, game), now, 0)))

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
    return if (r.estimated) "Campana de la referencia estimada para ${range?.label ?: "tu edad"}. Tu mejor marca de esta semana, ${TimeTrialDay.hitsText(r.mark)}, $end"
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
   * «Mientras tanto, comparada con todas las edades de esta semana, tu mejor marca, 43 aciertos, supera a 51 de cada 100 personas.» (el MISMO número que la fila «Todas las edades» de «Comparado con otras edades»); solo si esta semana no llegan a 50: «Mientras tanto, comparada con todas las edades de las últimas 4 semanas, tu mejor marca de esta semana, 43 aciertos, supera a 53 de cada 100 personas.» y entonces esa fila dice «todavía somos pocos».
   */
  fun meanwhileSentence(r: Ranked): String {
    val n = r.percentile
    if (r.scope == Scope.ALL_WEEK) return "Mientras tanto, comparada con todas las edades de esta semana, tu mejor marca, ${TimeTrialDay.hitsText(r.mark)}, deja detrás a $n de cada 100 personas."
    return "Mientras tanto, comparada con todas las edades de las últimas $RECENT_WEEKS semanas, tu mejor marca de esta semana, ${TimeTrialDay.hitsText(r.mark)}, deja detrás a $n de cada 100 personas."
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

  /** Una semana está «por encima de la mitad de tu franja» cuando supera a 50 de cada 100 o más. */
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

  /** Lo que lee el lector de pantalla del gráfico de las semanas, semana por semana: «Tu percentil en las últimas 7 semanas: hace 6 semanas, supera a 52 de cada 100; …; esta semana, supera a 63 de cada 100.» */
  fun weeksSpoken(points: List<WeekPoint>, estimated: Boolean = false): String =
    "$WEEKS_TITLE" + (if (estimated) ", comparado con la referencia estimada" else "") + ": " + points.joinToString("; ") { pt -> val n = pt.percentile; "${weeksAgoText(pt.weeksAgo)}, deja detrás a $n de cada 100" } + "."

  fun weeksAgoText(weeksAgo: Int): String = when (weeksAgo) {
    0 -> "esta semana"
    1 -> "hace 1 semana"
    else -> "hace $weeksAgo semanas"
  }

  // ------------------------------------------------------------------ comparado con otras edades

  /** Una fila de «Otras edades»: [label] (la franja o «Todas las edades») con su [percentile] (solo hay fila con 50 personas o más), [own] = la franja de la persona. */
  data class OtherRow(val label: String, val percentile: Int, val own: Boolean, val people: Int) {
    /** «63 de cada 100». */
    val value: String get() { val p = percentile; return "$p de cada 100" }
  }

  /** Las filas con datos de «Otras edades»; [hidden] = alguna franja (o todas las edades) todavía no llega a 50 personas y la losa cierra con [OTHERS_HIDDEN]. */
  data class Others(val rows: List<OtherRow>, val all: OtherRow?, val hidden: Boolean)

  /**
   * La marca de esta semana contra cada franja (hasta 6 mientras [Features.COMPARE_MINORS] sea false, sin la de 13 a 17) y contra todas las edades, esta semana: solo las clases con 50 personas o más tienen fila, con su «N de cada 100» (Tarea 203b: ya no hay filas de «todavía somos pocos»,
   * que en los primeros meses serían casi todas); la propia lleva «(tu franja)». [Others.hidden] avisa que falta alguna y null = no hay ninguna fila que mostrar (la losa no va).
   */
  fun others(source: Source, game: String, range: AgeRange, mark: Int): Others? {
    var anyEstimated = false
    fun row(label: String, ranges: List<AgeRange>?, own: Boolean): OtherRow? {
      val d = source.distribution(game, ranges, 0)
      if (d.enough && d.estimated) anyEstimated = true
      return if (d.enough) OtherRow(if (own) "$label (tu franja)" else label, percentile(mark, d), own, if (d.estimated) 0 else d.people) else null
    }
    val ages = AgeRange.entries.filter { it != AgeRange.R13_17 || Features.COMPARE_MINORS }
    val rows = ages.mapNotNull { row(it.label, listOf(it), it == range) }
    val all = row("Todas las edades", null, false)
    if (rows.isEmpty() && all == null) return null
    // con la referencia estimada no hay fila de «Todas las edades» (no hay población que ponderar) y no se dice nada de lo que falta: cada franja siempre tiene su fila
    return Others(rows, all, hidden = rows.size < ages.size || (all == null && !anyEstimated))
  }

  /** «De cada 100 personas que jugaron Tinta o Palabra esta semana, tu mejor marca deja detrás a:» (la marca ya está en la cifra de arriba: no se repite). */
  fun othersLead(gameTitle: String, estimated: Boolean = false): String =
    if (estimated) "De cada 100 marcas de la referencia estimada de cada edad, tu mejor marca deja detrás a:" else "De cada 100 personas que jugaron $gameTitle esta semana, tu mejor marca deja detrás a:"

  /** Lo que lee el lector de pantalla de una fila: «45 a 54 años (tu franja), 63 de cada 100». */
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
   * Los rótulos del eje de la campana (Tarea 205): «0 aciertos», dos intermedios y donde EMPIEZA el último tramo («75 o más»), a un tercio y dos tercios de él. Con 16 tramos de 5 son 0 · 25 · 50 · 75 (los de siempre); con 24 de 1 (Rescate), 0 · 8 · 15 · 23; con 16 de 2 (Acoplamiento), 0 · 10 · 20 · 30.
   */
  fun axisTicks(d: Distribution): List<Int> {
    val end = d.lastStart
    return listOf(0, (end / 3.0).let { Math.round(it).toInt() }, (end * 2 / 3.0).let { Math.round(it).toInt() }, end)
  }

  /** El hueco entre rótulos que pide el dibujo antes de omitir uno (14 sp): 8 dp. */
  const val AXIS_GAP_DP = 8
}
