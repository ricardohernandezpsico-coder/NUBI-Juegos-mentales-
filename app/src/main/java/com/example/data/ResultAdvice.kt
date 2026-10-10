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
    // Aterrizaje Lunar (renovado el 10-oct) ya no pasa por aquí: su «Truco para la próxima» viene en su propio final con sentido (`MeaningfulResult`, `NumberLine.trick`).
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
    // En la punta de la lengua: cuando alguna palabra necesitó ayuda o la mostró Nubi.
    result.puntaSolo?.let { solo ->
      out += tipOf(Punta.message(solo, Punta.total(solo, result.puntaPista, result.puntaLetras, result.puntaVista)) ?: "")
    }
    // Carga exacta: cuando alguna carga necesitó pista o quedó para otra vez.
    result.cargaAlone?.let { alone -> out += tipOf(Carga.tip(alone, result.totalTrials, result.level) ?: "") }
    // Engranajes: cuando alguna máquina falló, el truco de seguir el camino desde el motor.
    if (result.engrEtapa != null) out += tipOf(Engranajes.tip(result.correctAnswers, result.totalTrials) ?: "")
    // Bodega de carga: cuando algún objeto no se encontró al primer intento, imaginarlo dentro de su escotilla.
    if (result.bodGroup != null) out += tipOf(Bodega.tip(result.correctAnswers, result.totalTrials) ?: "")
    // Constelaciones: cuando alguna vez se te escapó una compañera ya vista, dar vuelta primero una luz nueva.
    if (result.conGroup != null) out += tipOf(Constelaciones.tip(result.correctAnswers, result.totalTrials) ?: "")
    // Tinta o Palabra: si la palabra o el cambio de orilla te frenaron mucho (≥ 0,3 s).
    if (result.interferenceMs != null || result.switchCostMs != null) {
      out += DosOrillas.tips(result.interferenceMs, result.switchCostMs).map { tipOf(it) }
    }
    // La estación de correo: un truco de intención de implementación según lo que más se escapó (la radio, la hora o las cartas señal).
    if (result.mailGroup != null) out += tipOf(Mail.tip(result.mailEvHits, result.mailEvTotal, result.mailTimeHits, result.mailTimeTotal, result.mailCommissions, result.mailPeeks, result.mailPeeksGood, result.mailGoldMissed, result.mailLazoMissed) ?: "")
    return out.filterNotNull()
  }
}
