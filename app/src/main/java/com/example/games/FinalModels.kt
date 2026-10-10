package com.example.games

import com.example.data.Acoplamiento
import com.example.data.Atlas
import com.example.data.Bodega
import com.example.data.Brake
import com.example.data.Carga
import com.example.data.Constelaciones
import com.example.data.DosOrillas
import com.example.data.Engranajes
import com.example.data.FinalesConSentido
import com.example.data.Harvest
import com.example.data.Mail
import com.example.data.MeasurePoint
import com.example.data.Piloto
import com.example.data.Punta
import com.example.data.Reading
import com.example.data.Rescate
import com.example.data.ResultAdvice
import com.example.data.Satelites
import com.example.data.Trail
import com.example.data.Vocabulary
import com.example.model.GamePlayResult
import com.example.ui.components.MeaningfulResultModel
import com.example.ui.components.ResultChip
import kotlin.math.roundToInt

/**
 * Lo que dice el «final con sentido» de cada juego (Etapa 3, `docs/finales-con-sentido.md`), armado desde la partida y las medidas guardadas. Lógica pura con pruebas: la pantalla (`GameResultScreen`) solo lo dibuja con `MeaningfulResult`.
 * Grupo 1 (Memoria): Constelaciones, Rastro de luz, Bodega de carga y La estación de correo. Grupo 2 (Atención): Tinta o Palabra, Piloto Estelar, Freno de Emergencia, Satélites y Rescate relámpago. Grupo 3 (Razonamiento): Carga exacta, Engranajes y Acoplamiento. Grupo 4 (Lenguaje): En la punta de la lengua, Lluvia de meteoros, ¿Verdad o disparate?, Cosecha de palabras y La estrella intrusa.
 * (Aterrizaje Lunar, hecho antes, arma el suyo en `GameResultScreen`.)
 *
 * «Lo que hiciste»: la medida propia que el juego ya mostraba, en grande, más UN dato tuyo (el récord si lo superaste hoy; si no, el más útil de los que ya mostraba: ver cada juego). «Tu avance»: Hoy, Tu promedio y Tu mejor en la
 * unidad de la medida ([FinalesConSentido.progress], con su tolerancia de «parejo»). «Truco»: el condicional del juego si aplica a esta partida y, si no, el general de la tabla.
 */
object FinalModels {
  /** Cuántos puntos cuentan como «parejo» alrededor del promedio en las medidas en %: de 8 a 20 ensayos por partida, un solo ensayo pesa de 5 a 12 puntos. */
  const val TOLERANCE_PERCENT = 5f

  /** Para el rastro (luces seguidas, números enteros): media luz. */
  const val TOLERANCE_LIGHTS = 0.5f

  /** Lluvia de meteoros («tu vocabulario», % ponderado sobre unas 30 a 60 palabras): 5 puntos. */
  const val TOLERANCE_VOCAB = 5f

  /** ¿Verdad o disparate? («tu lectura», palabras por minuto; entre partidas parecidas varía unas 10 ppm): 10 ppm. */
  const val TOLERANCE_WPM = 10f

  /** Cosecha de palabras («las comunes encontradas», % sobre unas 20 a 50 comunes: cada una pesa de 2 a 5 puntos): 5 puntos. */
  const val TOLERANCE_HARVEST = 5f

  /** Carga exacta y Engranajes (% de 6 a 12 ensayos por partida: uno solo pesa de 8 a 17 puntos): 10 puntos. En la punta de la lengua y La estrella intrusa (de 6 a 12 palabras o rondas) también. */
  const val TOLERANCE_PERCENT_SHORT = 10f

  /** Acoplamiento («tu giro mental», grados por segundo; la pendiente del tiempo contra el ángulo cambia de 20 a 40 °/s entre partidas parecidas): 30 °/s. */
  const val TOLERANCE_DPS = 30f

  /** Piloto («tus señales a los mandos», % de 8 a 25 señales de la misión): 5 puntos. */
  const val TOLERANCE_SIGNALS = 5f

  /** Satélites (cuántos seguiste de verdad a la vez, con un decimal; una partida varía de 0,3 a 0,6 de una a otra): 0,3. */
  const val TOLERANCE_TRACKING = 0.3f

  /** Rescate («tu vistazo», ms; la duración de destello varía unos 20 ms entre partidas parecidas): 20 ms. */
  const val TOLERANCE_GLANCE_MS = 20f

  /** Freno («tu freno», ms; una estimación sola trae pocos altos y varía unos 20 ms): 20 ms. Se dice en zonas, nunca en milisegundos. */
  const val TOLERANCE_BRAKE_MS = 20f

  /** Tinta o Palabra («cuánto te frenó la palabra», ms; con pocos aciertos de cada tipo varía 0,1 s o más, que es lo que se ve en pantalla: décimas de segundo): 100 ms. */
  const val TOLERANCE_STROOP_MS = 100f

  /** ¿Esta partida se muestra con el final con sentido? (Aterrizaje Lunar y los juegos de los grupos ya hechos de la Etapa 3.) */
  fun usesMeaningful(result: GamePlayResult): Boolean = when (result.gameId) {
    "aterrizaje" -> result.numlineErrorPct != null
    "parejas" -> result.conGroup != null
    "secuencia" -> result.rasRounds != null
    "bodega" -> result.bodGroup != null
    "correo" -> result.mailGroup != null
    "stroop" -> result.interferenceMs != null || result.switchCostMs != null
    "piloto" -> result.pilLanePct != null
    "freno" -> result.stopsTotal != null
    "satelites" -> result.satLights != null || result.trackingCapacity != null
    "radar" -> result.rescRescued != null
    "calculo" -> result.cargaAlone != null
    "engranajes" -> result.engrEtapa != null
    "acoplamiento" -> result.dockDocked != null || result.rotationSpeedDps != null
    "anagramas" -> result.puntaSolo != null
    "meteoros" -> result.lexBandSeen != null && result.lexBandHits != null
    "disparate" -> result.svSeenType != null
    "cosecha" -> Harvest.words(result.harvWords) != null
    "intrusa" -> result.intrSeenType != null
    else -> false
  }

  /** El modelo del final de este juego, o null si todavía no tiene el final con sentido (o la partida no trae su medida). */
  fun forGame(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel? = when {
    !usesMeaningful(result) -> null
    result.gameId == "parejas" -> constelaciones(result, measures)
    result.gameId == "secuencia" -> rastro(result, measures)
    result.gameId == "bodega" -> bodega(result, measures)
    result.gameId == "correo" -> correo(result, measures)
    result.gameId == "stroop" -> tinta(result, measures)
    result.gameId == "piloto" -> piloto(result, measures)
    result.gameId == "freno" -> freno(result, measures)
    result.gameId == "satelites" -> satelites(result, measures)
    result.gameId == "radar" -> rescate(result, measures)
    result.gameId == "calculo" -> carga(result, measures)
    result.gameId == "engranajes" -> engranajes(result, measures)
    result.gameId == "acoplamiento" -> acoplamiento(result, measures)
    result.gameId == "anagramas" -> punta(result, measures)
    result.gameId == "meteoros" -> meteoros(result, measures)
    result.gameId == "disparate" -> disparate(result, measures)
    result.gameId == "cosecha" -> cosecha(result, measures)
    result.gameId == "intrusa" -> intrusa(result, measures)
    else -> null
  }

  // ------------------------------------------------------------------ Constelaciones («Tu memoria de lugar»)

  fun constelaciones(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val con = Constelaciones
    val hits = result.correctAnswers
    val opps = result.totalTrials
    val record = con.recordLine(result.conBest, result.conNewRecord)
    return build(
      result, measures, key = "place", copy = FinalesConSentido.copy("parejas")!!, tolerance = TOLERANCE_PERCENT, format = FinalesConSentido::percentText,
      todayValue = con.mark(hits, opps),
      boxTitle = "Tu memoria de lugar",
      headline = con.percent(hits, opps)?.let { "$it %" } ?: "—",
      unit = con.detailLine(hits, opps),
      dataLine = if (result.conNewRecord == true) record else con.streakLine(result.conBestStreak) ?: record,
      conditionalTrick = con.tip(hits, opps)?.let { ResultAdvice.tipOf(it) }
    )
  }

  // ------------------------------------------------------------------ Rastro de luz («Tu rastro»)

  fun rastro(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val trail = Trail
    val rounds = result.rasRounds
    val best = trail.bestTrail(result.rasBestLen)
    val rows = trail.modeRows(rounds, result.rasHits)
    val reading = trail.readingLines(rows)                                   // [la línea del modo que más costó, su truco] o nada
    return build(
      result, measures, key = "trail", copy = FinalesConSentido.copy("secuencia")!!, tolerance = TOLERANCE_LIGHTS, format = FinalesConSentido::lightsText,
      todayValue = trail.mark(result.rasBestLen),
      boxTitle = "Tu rastro",
      headline = best?.let { trail.lights(it) } ?: "—",
      unit = if (best != null) "las más largas que repetiste bien en el rastro simple" else trail.noTrailLine(rounds),
      dataLine = trail.unlockedLine(result.rasNewModes) ?: reading.firstOrNull(),
      conditionalTrick = reading.getOrNull(1)?.let { ResultAdvice.tipOf(it) }
    )
  }

  // ------------------------------------------------------------------ Bodega de carga («Tu bodega»)

  fun bodega(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val bod = Bodega
    val total = result.totalTrials
    val firstTry = result.correctAnswers
    val record = bod.recordLine(result.bodBest, result.bodNewRecord)
    return build(
      result, measures, key = "bodega", copy = FinalesConSentido.copy("bodega")!!, tolerance = TOLERANCE_PERCENT, format = FinalesConSentido::percentText,
      todayValue = bod.mark(firstTry, total),
      boxTitle = "Tu bodega",
      headline = if (total > 0) "${firstTry.coerceIn(0, total)} de $total objetos" else "—",
      unit = "al primer intento",
      dataLine = if (result.bodNewRecord == true) record else bod.biggestLine(result.bodBiggest) ?: bod.streakLine(result.bodBestStreak, total) ?: record,
      conditionalTrick = bod.tip(firstTry, total)?.let { ResultAdvice.tipOf(it) }
    )
  }

  // ------------------------------------------------------------------ La estación de correo («Tu memoria para lo pendiente»)

  fun correo(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val mail = Mail
    val ok = result.correctAnswers
    val all = result.totalTrials
    val record = mail.recordLine(result.mailBest, result.mailNewRecord)
    val tip = mail.tip(
      result.mailEvHits, result.mailEvTotal, result.mailTimeHits, result.mailTimeTotal, result.mailCommissions, result.mailPeeks, result.mailPeeksGood,
      result.mailGoldMissed, result.mailLazoMissed
    )
    return build(
      result, measures, key = "estacion", copy = FinalesConSentido.copy("correo")!!, tolerance = TOLERANCE_PERCENT, format = FinalesConSentido::percentText,
      todayValue = mail.mark(ok, all),
      boxTitle = "Tu memoria para lo pendiente",
      headline = mail.percent(ok, all)?.let { "$it %" } ?: "—",
      unit = mail.detailLine(ok, all),
      dataLine = if (result.mailNewRecord == true) record else mail.clockLine(result.mailPeeksGood, result.mailPeeks) ?: mail.cancelLine(result.mailCancels, result.mailCommissions) ?: mail.cardsLine(result.mailRight) ?: record,
      conditionalTrick = tip?.let { ResultAdvice.tipOf(it) }
    )
  }

  // ------------------------------------------------------------------ Grupo 2 (Atención)

  /** Tinta o Palabra («Cuánto te frenó la palabra»): menos es mejor; se dice en palabras («casi nada», «+0,4 s»). */
  fun tinta(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val dos = DosOrillas
    val ms = result.interferenceMs
    val switch = result.switchCostMs
    return build(
      result, measures, key = "stroop", copy = FinalesConSentido.copy("stroop")!!, tolerance = TOLERANCE_STROOP_MS, lowerIsBetter = true,
      format = { v -> if (v == null) "—" else dos.cost(v.roundToInt()) },
      todayValue = ms?.toFloat(),
      boxTitle = dos.INTERFERENCE_TITLE,
      headline = ms?.let { dos.cost(it) } ?: "—",
      unit = if (ms != null) "Lo que tardaste de más cuando la palabra y su tinta no coincidían." else "Esta vez no hubo aciertos suficientes para medirlo.",
      dataLine = switch?.let { dos.SWITCH_TITLE + ": " + dos.cost(it) },
      conditionalTrick = dos.tips(ms, switch).firstOrNull()?.let { ResultAdvice.tipOf(it) }
    )
  }

  /** Piloto Estelar («Tus señales a los mandos»): las dos tareas siempre juntas; sin 8 señales de la misión no se dice una proporción. */
  fun piloto(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val pct = result.pilSignalPct?.takeIf { it in 0..100 }
    return build(
      result, measures, key = "mandos", copy = FinalesConSentido.copy("piloto")!!, tolerance = TOLERANCE_SIGNALS, format = FinalesConSentido::percentText,
      todayValue = Piloto.mark(pct),
      boxTitle = "Tus señales a los mandos",
      headline = pct?.let { "$it %" } ?: "—",
      unit = if (pct != null) "de las señales de tu misión, a los mandos" else Piloto.TOO_FEW_LINE,
      dataLine = Piloto.missionLine(result.pilHits, result.pilTargets),
      conditionalTrick = Piloto.advice(result.pilHits, result.pilTargets, result.pilFalse, result.pilLanePct)
    )
  }

  /** Freno de Emergencia («Tu freno»): en ZONAS (ágil, firme, pausado), nunca en milisegundos; el recuadro dice el promedio de las últimas partidas. */
  fun freno(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val reading = Brake.reading(measures, result.brakeMs, result.timestamp)
    val today = result.brakeMs?.toFloat()?.takeIf { Brake.isValid(it) }
    val altos = "Frenaste ${result.stopsOk ?: 0} de ${result.stopsTotal ?: 0} altos"
    return build(
      result, measures, key = Brake.KEY, copy = FinalesConSentido.copy("freno")!!, tolerance = TOLERANCE_BRAKE_MS, lowerIsBetter = true,
      format = { v -> if (v == null) "—" else "zona " + Brake.zone(v).label },
      todayValue = today,
      boxTitle = "Tu freno",
      headline = reading?.let { "zona " + it.zone.label } ?: "—",
      unit = when {
        reading == null -> "Esta vez no se pudo estimar tu freno: hacen falta al menos 6 altos y haber frenado entre 1 de cada 4 y 3 de cada 4."
        reading.isFirstReading -> "primera lectura: se afina con más partidas"
        else -> "promedio de tus últimas ${reading.count} partidas"
      },
      dataLine = altos,
      // sin estimación el truco condicional es el que hace que la medida funcione; con ella, el general de la tabla
      conditionalTrick = if (reading == null) "Lanza apenas se encienda la luz, sin esperar al ALTO: así la medida funciona." else null
    )
  }

  /** Satélites («Tu seguimiento»): cuántos seguiste de verdad a la vez; las luces encendidas son el premio (el planeta va en el detalle). */
  fun satelites(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val cap = result.trackingCapacity
    val record = Satelites.recordLine(result.satBest, result.satNewRecord)
    return build(
      result, measures, key = "tracking", copy = FinalesConSentido.copy("satelites")!!, tolerance = TOLERANCE_TRACKING, format = FinalesConSentido::atOnceText,
      todayValue = cap,
      boxTitle = "Tu seguimiento",
      headline = cap?.let { FinalesConSentido.atOnceText(it) } ?: "—",
      unit = if (cap != null) "cuántos seguiste de verdad al mismo tiempo, sin contar la suerte" else "Esta vez no hubo rondas suficientes para medir tu seguimiento.",
      dataLine = if (result.satNewRecord == true) record else Satelites.lightsLine(result.satLights) ?: Satelites.perfectLine(result.satPerfect, result.totalTrials, result.satBestStreak),
      conditionalTrick = null
    )
  }

  /** Rescate relámpago («Tu vistazo»): el destello más breve con el que rescatas casi todas las cápsulas; menos milisegundos es mejor. */
  fun rescate(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val ms = result.glanceMs?.takeIf { it > 0 }
    val record = Rescate.recordLine(result.rescBest, result.rescNewRecord)
    return build(
      result, measures, key = "glance", copy = FinalesConSentido.copy("radar")!!, tolerance = TOLERANCE_GLANCE_MS, lowerIsBetter = true, format = FinalesConSentido::msText,
      todayValue = ms?.toFloat(),
      boxTitle = "Tu vistazo",
      headline = ms?.let { "$it ms" } ?: "—",
      unit = if (ms != null) "el destello más breve con el que rescatas casi todas las cápsulas" else "Esta vez no hubo rondas suficientes para medir tu vistazo.",
      dataLine = if (result.rescNewRecord == true) record else Rescate.rescuedLine(result.rescRescued) ?: record,
      conditionalTrick = null
    )
  }

  // ------------------------------------------------------------------ Grupo 3 (Razonamiento)

  /** Carga exacta («Tu reactor»): las cargas logradas sin pista; los cuadros van en % porque cada partida trae un total de cargas distinto. */
  fun carga(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val carga = Carga
    val alone = result.cargaAlone
    val total = result.totalTrials
    return build(
      result, measures, key = "carga", copy = FinalesConSentido.copy("calculo")!!, tolerance = TOLERANCE_PERCENT_SHORT, format = FinalesConSentido::percentText,
      todayValue = carga.mark(alone, total),
      boxTitle = "Tu reactor",
      headline = if (alone != null && total > 0) "${alone.coerceIn(0, total)} de $total" else "—",
      unit = "cargas logradas sin pista",
      dataLine = carga.shortLine(result.cargaShort, alone) ?: carga.breakdown(alone, result.cargaHinted, total),
      conditionalTrick = carga.tip(alone, total, result.level)?.let { ResultAdvice.tipOf(it) }
    )
  }

  /** Engranajes («Tu cohete»): las máquinas arregladas; lo que pasó con el cohete (despegó, o cuántas luces faltan) es el dato tuyo. */
  fun engranajes(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val eng = Engranajes
    val total = result.totalTrials
    val ok = result.correctAnswers
    return build(
      result, measures, key = "taller", copy = FinalesConSentido.copy("engranajes")!!, tolerance = TOLERANCE_PERCENT_SHORT, format = FinalesConSentido::percentText,
      todayValue = eng.mark(ok, total),
      boxTitle = "Tu cohete",
      headline = if (total > 0) "${ok.coerceIn(0, total)} de $total máquinas" else "—",
      unit = "arregladas",
      dataLine = eng.launchLine(result.engrLaunches, result.engrOrbit) ?: eng.hangarLine(result.engrLights),
      conditionalTrick = eng.tip(ok, total)?.let { ResultAdvice.tipOf(it) }
    )
  }

  /** Acoplamiento («Tu giro mental»): grados por segundo, más es mejor; los módulos acoplados (el premio) son el dato tuyo. Sin medida de giro lo dice sin culpa. */
  fun acoplamiento(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val dps = result.rotationSpeedDps
    val record = Acoplamiento.recordLine(result.dockBest, result.dockNewRecord)
    return build(
      result, measures, key = "rotation", copy = FinalesConSentido.copy("acoplamiento")!!, tolerance = TOLERANCE_DPS, format = FinalesConSentido::degreesText,
      todayValue = dps?.toFloat(),
      boxTitle = "Tu giro mental",
      headline = dps?.let { "$it° por segundo" } ?: "—",
      unit = if (dps != null) "los grados que giras la pieza en tu mente cada segundo" else "Se calcula con al menos 8 aciertos en 3 ángulos distintos y 7 de cada 10 respuestas bien: con más partidas lo verás.",
      dataLine = if (result.dockNewRecord == true) record else Acoplamiento.dockedLine(result.dockDocked) ?: record,
      conditionalTrick = null
    )
  }

  // ------------------------------------------------------------------ Grupo 4 (Lenguaje)

  /** En la punta de la lengua («Tu cielo de palabras»): las palabras que encontraste por tu cuenta; los cuadros van en %. */
  fun punta(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val punta = Punta
    val solo = result.puntaSolo
    val total = punta.total(solo, result.puntaPista, result.puntaLetras, result.puntaVista)
    return build(
      result, measures, key = "punta", copy = FinalesConSentido.copy("anagramas")!!, tolerance = TOLERANCE_PERCENT_SHORT, format = FinalesConSentido::percentText,
      todayValue = punta.mark(solo, total),
      boxTitle = "Tu cielo de palabras",
      headline = if (solo != null && total > 0) "${solo.coerceIn(0, total)} de $total" else "—",
      unit = "palabras encontradas por tu cuenta",
      dataLine = punta.speedLine(result.puntaMs, solo) ?: punta.blueNote(result.puntaVista) ?: punta.breakdown(solo, result.puntaPista, result.puntaLetras, result.puntaVista),
      conditionalTrick = punta.message(solo, total)?.let { ResultAdvice.tipOf(it) }
    )
  }

  /** Lluvia de meteoros («Tu vocabulario»): el % reconocido de las palabras menos comunes (menos las inventadas tocadas). */
  fun meteoros(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val vocab = Vocabulary
    val bands = vocab.bandPercents(result.lexBandSeen, result.lexBandHits, result.lexFaSeen, result.lexFaHits)
    val groups = vocab.groupPercents(result.lexBandSeen, result.lexBandHits, result.lexFaSeen, result.lexFaHits)
    val mark = vocab.mark(bands, result.lexBandSeen)
    return build(
      result, measures, key = "vocab", copy = FinalesConSentido.copy("meteoros")!!, tolerance = TOLERANCE_VOCAB, format = FinalesConSentido::percentText,
      todayValue = mark,
      boxTitle = "Tu vocabulario",
      headline = mark?.let { "${it.roundToInt()} %" } ?: "—",
      unit = if (mark != null) "de las palabras menos comunes, reconocidas" else "Esta vez no hubo palabras suficientes para medir tu vocabulario.",
      dataLine = if (mark != null) vocab.phraseFor(groups) else null,
      conditionalTrick = vocab.filterAdvice(result.lexFaSeen, result.lexFaHits)
    )
  }

  /** ¿Verdad o disparate? («Tu lectura con comprensión»): palabras por minuto, leyendo y decidiendo; más es mejor. */
  fun disparate(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val reading = Reading
    val wpm = reading.wpm(result.svWpm)
    val rows = reading.typeRows(result.svRtType)
    return build(
      result, measures, key = "wpm", copy = FinalesConSentido.copy("disparate")!!, tolerance = TOLERANCE_WPM, format = FinalesConSentido::ppmText,
      todayValue = reading.mark(wpm),
      boxTitle = "Tu lectura con comprensión",
      headline = wpm?.let { "$it ppm" } ?: "—",
      unit = if (wpm != null) "palabras por minuto, leyendo y decidiendo, en las frases que acertaste" else "Juega un poco más para medir tu lectura: hacen falta unas 10 respuestas bien.",
      dataLine = reading.slowText(rows) ?: reading.streakLine(result.svBestStreak)?.let { "Tu mejor racha: $it" },
      conditionalTrick = reading.tip(rows)?.let { ResultAdvice.tipOf(it) }
    )
  }

  /** Cosecha de palabras («Tu cosecha»): las palabras comunes que había y encontraste; tu palabra estrella es el dato tuyo. */
  fun cosecha(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val harv = Harvest
    val words = harv.words(result.harvWords) ?: 0
    val common = harv.common(result.harvCommonFound, result.harvCommonTotal)
    val star = harv.starTitle(result.harvStar, result.harvBest)
    val search = harv.searchLine(result.harvClusterPct)
    val pct = harv.clusterPct(result.harvClusterPct)
    return build(
      result, measures, key = "harvest", copy = FinalesConSentido.copy("cosecha")!!, tolerance = TOLERANCE_HARVEST, format = FinalesConSentido::percentText,
      todayValue = harv.mark(result.harvCommonFound, result.harvCommonTotal),
      boxTitle = "Tu cosecha",
      headline = if (common != null) "${common.first} de ${common.second}" else "$words",
      unit = if (common != null) "palabras comunes" else harv.wordsLabel(words),
      dataLine = star?.let { it.first + ": " + it.second } ?: harv.rhythmLine(result.harvFirst20, result.harvLast20),
      // la frase de «tu manera de buscar» trae su consejo cuando la búsqueda se inclina mucho a un lado; si no, el general de la tabla
      conditionalTrick = if (pct != null && (pct >= Harvest.MANY_CLUSTERS || pct <= Harvest.MANY_JUMPS)) search else null
    )
  }

  /** La estrella intrusa («Tu red de significados»): las rondas bien; las trampas que te engañaron son el dato tuyo. */
  fun intrusa(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel {
    val atlas = Atlas
    val totals = atlas.totals(result.intrSeenType, result.intrHitsType)
    return build(
      result, measures, key = "atlas", copy = FinalesConSentido.copy("intrusa")!!, tolerance = TOLERANCE_PERCENT_SHORT, format = FinalesConSentido::percentText,
      todayValue = atlas.mark(result.intrSeenType, result.intrHitsType),
      boxTitle = "Tu red de significados",
      headline = totals?.let { "${it.first} de ${it.second}" } ?: "—",
      unit = "rondas bien",
      dataLine = atlas.trapFooledLine(result.intrSeenType, result.intrHitsType) ?: atlas.bonusLine(result.intrBonusSeen, result.intrBonusHits)?.let { "¿Qué las une? " + it } ?: atlas.speedLine(result.intrRtMs),
      conditionalTrick = atlas.trapReading(result.intrSeenType, result.intrHitsType)?.let { ResultAdvice.tipOf(it) }
    )
  }

  // ------------------------------------------------------------------ común

  private fun build(
    result: GamePlayResult,
    measures: List<MeasurePoint>,
    key: String,
    copy: FinalesConSentido.Copy,
    tolerance: Float,
    format: (Float?) -> String,
    todayValue: Float?,
    boxTitle: String,
    headline: String,
    unit: String,
    dataLine: String?,
    conditionalTrick: String?,
    lowerIsBetter: Boolean = false
  ): MeaningfulResultModel {
    // la partida de hoy tal como se guarda como medida: así se compara con las ANTERIORES (la misma marca de tiempo no se cuenta dos veces)
    val today = todayValue?.let { MeasurePoint(result.timestamp, key, it, result.endRating?.coerceIn(0f, 1f) ?: -1f, result.timed) }
    val previous = if (today != null) FinalesConSentido.previousValues(measures, today) else emptyList()
    val progress = FinalesConSentido.progress(todayValue, previous, tolerance, lowerIsBetter)
    return MeaningfulResultModel(
      title = "",
      boxTitle = boxTitle,
      headline = headline,
      headlineUnit = unit,
      dataLine = dataLine,
      summaryLine = null,
      progressTitle = FinalesConSentido.PROGRESS_TITLE,
      chips = listOf(
        ResultChip("Hoy", format(progress.today), highlight = true),
        ResultChip("Tu promedio", format(progress.average)),
        ResultChip("Tu mejor", format(progress.best))
      ),
      progressPhrase = progress.phrase,
      trickTitle = FinalesConSentido.TRICK_TITLE,
      trick = conditionalTrick ?: copy.trick,
      whyTitle = FinalesConSentido.WHY_TITLE,
      why = copy.whyText,
      source = copy.sourceText,
      note = FinalesConSentido.NOTE,
      spoken = FinalesConSentido.spoken(boxTitle, headline, unit, dataLine, progress.phrase)
    )
  }
}
