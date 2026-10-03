package com.example.data

import com.example.model.GamePlayResult

/**
 * Qué te sirve más ver primero al terminar un juego (`result_focus`, Ajustes y la pregunta del inicio): lo que avanzaste o un consejo para la próxima.
 * Se guarda en `profile_extra` (respaldado). Solo cambia el ORDEN de la pantalla de resultado: ningún dato se repite ni se oculta.
 */
enum class ResultFocus(val label: String) {
  AVANCE("Lo que avancé"),
  CONSEJO("Un consejo para la próxima");

  companion object {
    val DEFAULT = AVANCE
    fun fromStored(name: String?): ResultFocus? = entries.firstOrNull { it.name == name }

    /**
     * Lo guardado: `result_focus` si ya existe; si no, el valor VIEJO `coach_tone` (la pregunta «¿cómo prefieres que te anime?» se cambió el 3-oct por esta):
     * celebrar → avance, claro → consejo. Sin ninguno de los dos, el valor por defecto.
     */
    fun migrate(focus: String?, legacyCoachTone: String?): ResultFocus =
      fromStored(focus) ?: when (legacyCoachTone) {
        "CELEBRAR" -> AVANCE
        "CLARO" -> CONSEJO
        else -> DEFAULT
      }
  }
}

/**
 * El consejo concreto de la pantalla de resultado, separado de la medida para poder mostrarlo antes o después ([ResultFocus]). Los textos de los juegos
 * ya traen su consejo como «… Truco: …»: la medida muestra lo de antes ([body]) y el consejo, lo de después ([tipOf]); así cada frase aparece UNA sola vez.
 * Los juegos sin consejo propio (Freno, Satélites, Radar...) no tienen bloque de consejo: con las dos opciones se ven igual. Lógica pura con pruebas.
 */
object ResultAdvice {
  private const val MARK = "Truco: "

  /** La medida sin su consejo: lo que va antes de «Truco: ». Sin marca, el texto tal cual. */
  fun body(text: String): String = text.substringBefore(" $MARK").trimEnd()

  /** El consejo de un texto «… Truco: …» (con la primera letra en mayúscula); null si no trae. También vale un texto que EMPIEZA con «Truco: ». */
  fun tipOf(text: String): String? {
    val tip = when {
      text.startsWith(MARK) -> text.removePrefix(MARK)
      text.contains(" $MARK") -> text.substringAfter(" $MARK")
      else -> return null
    }.trim()
    return tip.takeIf { it.isNotEmpty() }?.replaceFirstChar { it.uppercase() }
  }

  /** Los consejos de esta partida, en el orden en que la pantalla los mostraba. Vacío = el juego no tiene consejo esta vez. */
  fun tips(result: GamePlayResult): List<String> {
    val out = mutableListOf<String?>()

    // Rastro de luz: el truco del modo que más costó.
    result.rasRounds?.let { rounds ->
      out += Trail.readingLines(Trail.modeRows(rounds, result.rasHits)).drop(1).map { tipOf(it) }
    }
    // Aterrizaje Lunar: el truco del tramo de la regla donde más te alejas.
    val trues = result.numlineTrue
    val givens = result.numlineGiven
    if (result.numlineErrorPct != null && trues != null && givens != null) {
      out += tipOf(NumberLine.message(NumberLine.reading(trues, givens)))
    }
    // Lluvia de meteoros: las letras cambiadas de lugar engañan (solo con datos suficientes).
    if (result.lexBandSeen != null && result.lexBandHits != null) {
      out += Vocabulary.filterAdvice(result.lexFaSeen, result.lexFaHits)
    }
    // ¿Verdad o disparate?: el truco del tipo de frase más lento.
    result.svSeenType?.let {
      val rows = Reading.typeRows(result.svRtType)
      if (rows.size >= 2 && Reading.slowText(rows) != null) out += tipOf(Reading.tip(rows) ?: "")
    }
    // La estrella intrusa: cuando alguna trampa engañó.
    result.intrSeenType?.let { out += tipOf(Atlas.trapReading(it, result.intrHitsType) ?: "") }
    // Rumbo a Casa: de dónde sale lo que te aleja de casa.
    val along = result.homingAlong
    val lateral = result.homingLateral
    val beacon = result.homingBeacon
    if (result.homingErrorPct != null && along != null && lateral != null && beacon != null) {
      Homing.sourceMessage(Homing.source(Homing.trips(along, lateral, beacon)))?.let { out += tipOf(it) }
    }
    // Correo Estelar: el reloj, lugar contra hora y la nave.
    result.mailEventTotal?.let { evTotal ->
      val evHits = result.mailEventHits ?: 0
      val raTotal = result.mailRadioTotal ?: 0
      val raHits = result.mailRadioHits ?: 0
      out += tipOf(Mail.clockMessage(result.mailClockChecks ?: -1, result.mailClockLate ?: 0, raTotal) ?: "")
      out += tipOf(Mail.compareMessage(evHits, evTotal, raHits, raTotal) ?: "")
      out += tipOf(Mail.shipMessage(result.mailHullIntactPct ?: -1, result.mailEmergencies ?: 0) ?: "")
    }
    return out.filterNotNull()
  }
}
