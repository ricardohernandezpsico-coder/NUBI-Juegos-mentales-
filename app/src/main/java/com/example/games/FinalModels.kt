package com.example.games

import com.example.data.Bodega
import com.example.data.Constelaciones
import com.example.data.FinalesConSentido
import com.example.data.Mail
import com.example.data.MeasurePoint
import com.example.data.ResultAdvice
import com.example.data.Trail
import com.example.model.GamePlayResult
import com.example.ui.components.MeaningfulResultModel
import com.example.ui.components.ResultChip

/**
 * Lo que dice el «final con sentido» de cada juego (Etapa 3, `docs/finales-con-sentido.md`), armado desde la partida y las medidas guardadas. Lógica pura con pruebas: la pantalla (`GameResultScreen`) solo lo dibuja con `MeaningfulResult`.
 * Grupo 1 (Memoria): Constelaciones, Rastro de luz, Bodega de carga y La estación de correo. (Aterrizaje Lunar, hecho antes, arma el suyo en `GameResultScreen`.)
 *
 * «Lo que hiciste»: la medida propia que el juego ya mostraba, en grande, más UN dato tuyo (el récord si lo superaste hoy; si no, el más útil de los que ya mostraba: ver cada juego). «Tu avance»: Hoy, Tu promedio y Tu mejor en la
 * unidad de la medida ([FinalesConSentido.progress], con su tolerancia de «parejo»). «Truco»: el condicional del juego si aplica a esta partida y, si no, el general de la tabla.
 */
object FinalModels {
  /** Cuántos puntos cuentan como «parejo» alrededor del promedio en las medidas en %: de 8 a 20 ensayos por partida, un solo ensayo pesa de 5 a 12 puntos. */
  const val TOLERANCE_PERCENT = 5f

  /** Para el rastro (luces seguidas, números enteros): media luz. */
  const val TOLERANCE_LIGHTS = 0.5f

  /** ¿Esta partida se muestra con el final con sentido? (Aterrizaje Lunar y los juegos de los grupos ya hechos de la Etapa 3.) */
  fun usesMeaningful(result: GamePlayResult): Boolean = when (result.gameId) {
    "aterrizaje" -> result.numlineErrorPct != null
    "parejas" -> result.conGroup != null
    "secuencia" -> result.rasRounds != null
    "bodega" -> result.bodGroup != null
    "correo" -> result.mailGroup != null
    else -> false
  }

  /** El modelo del final de este juego, o null si todavía no tiene el final con sentido (o la partida no trae su medida). */
  fun forGame(result: GamePlayResult, measures: List<MeasurePoint>): MeaningfulResultModel? = when {
    !usesMeaningful(result) -> null
    result.gameId == "parejas" -> constelaciones(result, measures)
    result.gameId == "secuencia" -> rastro(result, measures)
    result.gameId == "bodega" -> bodega(result, measures)
    result.gameId == "correo" -> correo(result, measures)
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
    conditionalTrick: String?
  ): MeaningfulResultModel {
    // la partida de hoy tal como se guarda como medida: así se compara con las ANTERIORES (la misma marca de tiempo no se cuenta dos veces)
    val today = todayValue?.let { MeasurePoint(result.timestamp, key, it, result.endRating?.coerceIn(0f, 1f) ?: -1f, result.timed) }
    val previous = if (today != null) FinalesConSentido.previousValues(measures, today) else emptyList()
    val progress = FinalesConSentido.progress(todayValue, previous, tolerance)
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
