package com.example.data

import com.example.Features
import com.example.model.AgeRange
import kotlin.math.min

/**
 * Tu lugar entre personas de tu edad (Tarea 202; `auditoria-fable/nubi-plus-comparacion.md` §1, §4 y §11, opción B), LÓGICA PURA: el percentil desde el histograma SIN suavizar, la frase en «de cada 100», la división con nombre, las reglas de «pocos», el percentil semana a semana,
 * «otras edades» y la forma de la campana. No sabe de pantallas ni de dónde salen las marcas de las otras personas: las pide a una [Source] (hoy los datos de muestra de `SampleComparison`, solo en depuración; el día que haya servidor, sus documentos semanales).
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
  const val SECTION_COMPARE = "Comparado con personas de tu edad"
  const val SAMPLE_NOTICE = "Estos son datos de muestra: solo se ven en la versión de prueba."
  const val PLACE_TITLE = "Tu lugar esta semana"
  const val WEEKS_TITLE = "Tu percentil en las últimas 7 semanas"
  const val OTHERS_TITLE = "Comparado con otras edades"
  const val NO_MINORS = "De 13 a 17 años no hay comparación por ahora."
  const val WEEKS_AXIS_LEFT = "hace 7 semanas"
  const val WEEKS_AXIS_RIGHT = "esta semana"
  const val BELL_AXIS_LEFT = "0 aciertos"
  const val BELL_AXIS_RIGHT = "75 o más"
  val BELL_AXIS_MIDDLE = listOf(25, 50)
  const val MERGED_LINE = "Juntamos dos franjas porque todavía somos pocos."
  const val FEW_TITLE = "Todavía somos pocos"
  const val FEW_BAR_GOAL = MIN_PEOPLE
  const val NOT_ENOUGH_PEOPLE = "todavía somos pocos"

  // ------------------------------------------------------------------ la distribución

  /**
   * Las mejores marcas semanales de UNA clase de referencia (juego, pista, semana o semanas, franja o franjas): cuántas [people] y, en [bins], cuántas cayeron en cada tramo de 5 aciertos (16 tramos, sin suavizar); [p25], [p50] y [p75] son los cuartiles en aciertos, dichos tal cual por quien arma la distribución
   * (el servidor los calcula con las marcas crudas; en los datos de muestra salen de su tabla). Con menos de [MIN_PEOPLE] personas no alcanza para decir un lugar ([enough]).
   */
  data class Distribution(val people: Int, val bins: List<Int>, val p25: Int, val p50: Int, val p75: Int) {
    init {
      require(bins.size == BINS && bins.all { it >= 0 } && bins.sum() == people) { "el histograma debe tener $BINS tramos que sumen las $people personas" }
    }

    val enough: Boolean get() = people >= MIN_PEOPLE
  }

  /** De dónde salen las marcas de las otras personas (hoy: `SampleComparison`, solo en depuración). [ranges] null = todas las edades; [weeks] semanas que TERMINAN [weeksAgo] semanas atrás (0 = esta semana). */
  interface Source {
    fun distribution(game: String, ranges: List<AgeRange>?, weeksAgo: Int, weeks: Int = 1): Distribution
  }

  /**
   * De dónde sale la comparación en ESTA build: con [Features.COMPARE] encendida sería el servidor, que todavía no existe (devuelve null: no hay sección); con ella apagada, SOLO las builds de depuración usan los datos de muestra ([few] = el mundo de «pocos»), y la de la tienda no tiene nada.
   * `ContrarrelojPageTest` y `ComparisonTest` aseguran que en release no hay sección.
   */
  fun sourceFor(debugBuild: Boolean, few: Boolean): Source? = when {
    Features.COMPARE -> null
    debugBuild -> SampleComparison(few)
    else -> null
  }

  /**
   * La franja con que se compara una persona: la suya; si no la eligió, en las builds de depuración con datos de muestra la de 45 a 54 años (para poder ver la sección) y en cualquier otra, ninguna (sin franja no hay con quién compararla). Con de 13 a 17 años, ninguna mientras [Features.COMPARE_MINORS] sea false:
   * esa persona no ve la sección (§1.4 y §3.6).
   */
  fun rangeFor(stored: AgeRange?, sample: Boolean): AgeRange? {
    val range = stored ?: if (sample) AgeRange.R45_54 else null
    return range?.takeIf { it != AgeRange.R13_17 || Features.COMPARE_MINORS }
  }

  // ------------------------------------------------------------------ el percentil y la división

  /**
   * Cuántas de cada 100 personas de [d] tienen una marca MENOR que [mark], contando sobre los tramos SIN suavizar (§1.1 y §1.2): los tramos de abajo, enteros, más la parte del propio tramo que queda debajo de la marca (los 5 aciertos de un tramo se reparten parejo: con 3 de 5 por debajo, tres quintos de sus personas).
   * Entero (se redondea hacia abajo: nunca dice más de lo cierto), con tope 99 (nunca «100»: siempre hay alguien igual) y piso 0 (se dice igual, sin culpa). El último tramo («75 o más») no se parte: cuenta solo los de abajo.
   */
  fun percentile(mark: Int, d: Distribution): Int {
    if (d.people <= 0) return 0
    val m = mark.coerceAtLeast(0)
    val i = min(m / BIN_WIDTH, BINS - 1)
    val belowTimes5 = d.bins.take(i).sum().toLong() * BIN_WIDTH
    val withinTimes5 = if (i == BINS - 1) 0L else d.bins[i].toLong() * (m - i * BIN_WIDTH)
    return ((100L * (belowTimes5 + withinTimes5)) / (BIN_WIDTH.toLong() * d.people)).toInt().coerceIn(0, 99)
  }

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

  /** Con quiénes se compara una marca: la franja esta semana (lo normal), la franja las últimas 4 semanas, las franjas vecinas las últimas 4 semanas o todas las edades las últimas 4 semanas. */
  enum class Scope { OWN_WEEK, OWN_4_WEEKS, NEIGHBORS_4_WEEKS, ALL_4_WEEKS }

  /** Un lugar dicho: la [mark] de esta semana contra [distribution] (de [ranges], null = todas las edades), con su [percentile] y su [division]. */
  data class Ranked(val mark: Int, val percentile: Int, val division: Division, val scope: Scope, val ranges: List<AgeRange>?, val distribution: Distribution)

  sealed interface Place {
    /** Alcanzan 50 personas en la clase de referencia (la franja esta semana, o la primera de las más amplias de las reglas 2 y 3 de §4). */
    data class Found(val ranked: Ranked) : Place

    /** «Todavía somos pocos»: [own] personas de la franja jugaron este juego esta semana y ninguna clase de la franja o sus vecinas llegó a 50; [meanwhile] es lo que sí se puede decir con todas las edades de las últimas 4 semanas, si ahí llegan a 50 (paso 4). */
    data class Few(val own: Int, val meanwhile: Ranked?) : Place

    /** Sin marca esta semana en este juego: no hay con qué compararse. */
    data object NoMark : Place
  }

  /**
   * El lugar de [mark] (la mejor marca de esta semana en [game]; null = ninguna) para una persona de [range], con las reglas de §4 EN ORDEN: franja + esta semana con 50 personas o más; si no, la franja las últimas 4 semanas; si no, las franjas vecinas las últimas 4 semanas;
   * si aun así faltan, «Todavía somos pocos» con la cuenta real de la franja y, de «mientras tanto», todas las edades las últimas 4 semanas si ahí hay 50 o más. Siempre se dice cuál se usó (`Ranked.scope`).
   */
  fun place(source: Source, game: String, range: AgeRange, mark: Int?): Place {
    if (mark == null) return Place.NoMark
    fun ranked(scope: Scope, ranges: List<AgeRange>?, d: Distribution) = Ranked(mark, percentile(mark, d), division(percentile(mark, d)), scope, ranges, d)
    val own = source.distribution(game, listOf(range), 0)
    if (own.enough) return Place.Found(ranked(Scope.OWN_WEEK, listOf(range), own))
    source.distribution(game, listOf(range), 0, RECENT_WEEKS).takeIf { it.enough }?.let { return Place.Found(ranked(Scope.OWN_4_WEEKS, listOf(range), it)) }
    neighborGroup(range)?.let { group -> source.distribution(game, group, 0, RECENT_WEEKS).takeIf { it.enough }?.let { return Place.Found(ranked(Scope.NEIGHBORS_4_WEEKS, group, it)) } }
    val all = source.distribution(game, null, 0, RECENT_WEEKS).takeIf { it.enough }?.let { ranked(Scope.ALL_4_WEEKS, null, it) }
    return Place.Few(own.people, all)
  }

  /** «Tu mejor marca de esta semana, 43 aciertos, supera a 63 de cada 100 personas de 45 a 54 años que jugaron Tinta o Palabra esta semana.» (con las otras clases: «… en las últimas 4 semanas», «de 30 a 54 años», «de todas las edades»). */
  fun mainSentence(r: Ranked, range: AgeRange, gameTitle: String): String {
    val who = when (r.scope) {
      Scope.OWN_WEEK, Scope.OWN_4_WEEKS -> "de ${range.label}"
      Scope.NEIGHBORS_4_WEEKS -> "de ${groupLabel(r.ranges ?: listOf(range))}"
      Scope.ALL_4_WEEKS -> "de todas las edades"
    }
    val whenText = if (r.scope == Scope.OWN_WEEK) "esta semana" else "en las últimas $RECENT_WEEKS semanas"
    val n = r.percentile
    return "Tu mejor marca de esta semana, ${TimeTrialDay.hitsText(r.mark)}, supera a $n de cada 100 personas $who que jugaron $gameTitle $whenText."
  }

  /** «Es el percentil 63: la división En órbita.» */
  fun percentileLine(r: Ranked): String {
    val p = r.percentile
    return "Es el percentil $p: la división ${r.division.label}."
  }

  /** La línea que dice qué clase se usó cuando no es la normal: «Juntamos dos franjas porque todavía somos pocos.» (solo con las franjas vecinas; las otras ya lo dicen en la frase). */
  fun scopeLine(r: Ranked): String? = if (r.scope == Scope.NEIGHBORS_4_WEEKS) MERGED_LINE else null

  /** Lo que lee el lector de pantalla de la campana (el dibujo no habla; las dos frases de abajo son el contenido): «Campana con las marcas de las 128 personas. Tu mejor marca de esta semana, 43 aciertos, está marcada.» */
  fun bellSpoken(r: Ranked): String = "Campana con las marcas de las ${r.distribution.people} personas. Tu mejor marca de esta semana, ${TimeTrialDay.hitsText(r.mark)}, está marcada."

  /** «La mitad de las personas hizo 39 aciertos o menos; la cuarta parte más alta, 47 o más. La línea marca la mitad.» */
  fun quartilesSentence(d: Distribution): String =
    "La mitad de las personas hizo ${TimeTrialDay.hitsText(d.p50)} o menos; la cuarta parte más alta, ${d.p75} o más. La línea marca la mitad."

  /** «Hecha con las marcas de las 128 personas tal como son, no con una curva teórica.» */
  fun madeOfSentence(d: Distribution): String = "Hecha con las marcas de las ${d.people} personas tal como son, no con una curva teórica."

  /** «Todavía somos pocos: 23 personas de 45 a 54 años jugaron Tinta o Palabra esta semana. Cuando sean 50, verás aquí tu lugar entre ellas.» (con una sola: «1 persona … jugó …»). */
  fun fewSentence(few: Place.Few, range: AgeRange, gameTitle: String): String {
    val people = if (few.own == 1) "1 persona" else "${few.own} personas"
    val played = if (few.own == 1) "jugó" else "jugaron"
    return "$FEW_TITLE: $people de ${range.label} $played $gameTitle esta semana. Cuando sean $FEW_BAR_GOAL, verás aquí tu lugar entre ellas."
  }

  /** La barra de «Todavía somos pocos»: «23 de 50 personas». */
  fun fewBarText(own: Int): String = "$own de $FEW_BAR_GOAL personas"

  /** «Mientras tanto, comparada con todas las edades de las últimas 4 semanas, tu mejor marca de esta semana, 43 aciertos, supera a 58 de cada 100 personas.» */
  fun meanwhileSentence(r: Ranked): String {
    val n = r.percentile
    return "Mientras tanto, comparada con todas las edades de las últimas $RECENT_WEEKS semanas, tu mejor marca de esta semana, ${TimeTrialDay.hitsText(r.mark)}, supera a $n de cada 100 personas."
  }

  /** Sin marca esta semana: qué hacer para ver el lugar (sin números inventados). */
  fun noMarkSentence(gameTitle: String): String = "Todavía no tienes una marca esta semana en $gameTitle. Juega su Contrarreloj para ver tu lugar entre personas de tu edad."

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
  fun weeksSpoken(points: List<WeekPoint>): String =
    "$WEEKS_TITLE: " + points.joinToString("; ") { pt -> val n = pt.percentile; "${weeksAgoText(pt.weeksAgo)}, supera a $n de cada 100" } + "."

  fun weeksAgoText(weeksAgo: Int): String = when (weeksAgo) {
    0 -> "esta semana"
    1 -> "hace 1 semana"
    else -> "hace $weeksAgo semanas"
  }

  // ------------------------------------------------------------------ comparado con otras edades

  /** Una fila de «Comparado con otras edades»: [label] (la franja o «Todas las edades»), [percentile] (null = todavía somos pocos), [own] = la franja de la persona. */
  data class OtherRow(val label: String, val percentile: Int?, val own: Boolean, val people: Int) {
    /** «63 de cada 100» o «todavía somos pocos». */
    val value: String get() = percentile?.let { "$it de cada 100" } ?: NOT_ENOUGH_PEOPLE
  }

  data class Others(val rows: List<OtherRow>, val all: OtherRow)

  /**
   * La marca de esta semana contra cada franja (6 mientras [Features.COMPARE_MINORS] sea false, sin la de 13 a 17) y contra todas las edades, esta semana: con 50 personas o más, su «N de cada 100»; con menos, «todavía somos pocos». La propia lleva «(tu franja)».
   */
  fun others(source: Source, game: String, range: AgeRange, mark: Int): Others {
    fun row(label: String, ranges: List<AgeRange>?, own: Boolean): OtherRow {
      val d = source.distribution(game, ranges, 0)
      return OtherRow(if (own) "$label (tu franja)" else label, if (d.enough) percentile(mark, d) else null, own, d.people)
    }
    val rows = AgeRange.entries.filter { it != AgeRange.R13_17 || Features.COMPARE_MINORS }.map { row(it.label, listOf(it), it == range) }
    return Others(rows, row("Todas las edades", null, false))
  }

  /** «Tu mejor marca de esta semana, 43 aciertos, supera a tantas de cada 100 personas que jugaron Tinta o Palabra esta semana:» */
  fun othersLead(mark: Int, gameTitle: String): String =
    "Tu mejor marca de esta semana, ${TimeTrialDay.hitsText(mark)}, supera a tantas de cada 100 personas que jugaron $gameTitle esta semana:"

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
    val others: Others?
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
    return Section(range, gameTitle, mark, place, weekPoints(source, game, range, bests), others)
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
  fun bellHeight(smoothed: List<Double>, x: Double): Double {
    val u = x / BIN_WIDTH - 0.5                                // posición entre centros: el tramo i tiene su centro en u = i
    val i = Math.floor(u).toInt().coerceIn(0, smoothed.size - 2)
    val t = (u - i).coerceIn(0.0, 1.0)
    fun at(k: Int) = smoothed[k.coerceIn(0, smoothed.lastIndex)]
    val p0 = at(i - 1); val p1 = at(i); val p2 = at(i + 1); val p3 = at(i + 2)
    val y = 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t)
    return y.coerceAtLeast(0.0)
  }

  /** Dónde cae [mark] en el eje de la campana (de 0 a [TOP_MARK]): al centro de su marca; el último tramo («75 o más») se topa en el borde derecho. */
  fun markX(mark: Int): Double = (mark.coerceIn(0, TOP_MARK - 1) + 0.5).coerceAtMost(TOP_MARK.toDouble())

  /** El hueco entre rótulos que pide el dibujo antes de omitir uno (14 sp): 8 dp. */
  const val AXIS_GAP_DP = 8
}
