package com.example.data

import kotlin.math.roundToInt

/**
 * Las medidas de «Tinta o Palabra» (modelo «Dos orillas», `docs/diseno-tinta-o-palabra.md` §6; justificación en `docs/medidas-juegos-estrella.md`): Unity manda en milisegundos
 * «cuánto te frenó la palabra» (interferencia de Stroop: tiempo de los aciertos con palabra que choca menos el de los que coinciden) y «cambiar de orilla te costó» (costo de cambio de
 * regla); aquí se convierten en lo que la persona lee. Lógica pura con pruebas. Qué se mide de verdad: dos diferencias de tiempo EN ESTA PARTIDA; no se habla de «control ejecutivo» ni
 * de la concentración en la vida diaria, y con pocos datos (menos de 3 aciertos de cada tipo) no se dice nada.
 */
object DosOrillas {
  /** Por debajo de esto (0,05 s) se dice «casi nada». */
  const val NEGLIGIBLE_MS = 50
  /** Desde esta diferencia (0,3 s) se ofrece un consejo concreto. */
  const val ADVICE_MS = 300

  /** «+0,4 s» o «casi nada». */
  fun cost(ms: Int): String = if (ms < NEGLIGIBLE_MS) "casi nada" else "+" + seconds(ms)

  /** «0,4 s» con coma decimal fija (sin las culturas del teléfono). */
  fun seconds(ms: Int): String = String.format(java.util.Locale.ROOT, "%.1f", (ms / 100f).roundToInt() / 10f).replace('.', ',') + " s"

  const val INTERFERENCE_TITLE = "Cuánto te frenó la palabra"
  const val INTERFERENCE_LINE = "Cuánto más tardaste, en promedio, cuando la palabra decía un color y estaba escrita con otro, comparado con cuando coincidían."
  const val SWITCH_TITLE = "Cambiar de orilla te costó"
  const val SWITCH_LINE = "Cuánto más tardaste, en promedio, justo después de que la palabra cambió de orilla, comparado con cuando seguía llegando por la misma."

  /** El consejo de la partida (en el formato «Truco: …» que [ResultAdvice] separa de la medida); vacío si no hay diferencia grande o faltan datos. */
  fun tips(interferenceMs: Int?, switchCostMs: Int?): List<String> = buildList {
    if (interferenceMs != null && interferenceMs >= ADVICE_MS)
      add("Truco: mira primero de qué orilla llega y recién después la palabra: la orilla te dice qué tocar.")
    if (switchCostMs != null && switchCostMs >= ADVICE_MS)
      add("Truco: cuando cambie la orilla, dite por dentro «ahora, tinta» o «ahora, palabra» antes de tocar.")
  }
}
