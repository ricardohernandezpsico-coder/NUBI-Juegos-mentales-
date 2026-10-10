package com.example.data

/**
 * Separa el «Truco: …» de una frase de medida. Los textos de los juegos traen su consejo como «… Truco: …»: [body] es la medida sin él y [tipOf] es solo el truco.
 * Lo usan los finales con sentido (`games/FinalModels.kt`, su «Truco para la próxima» condicional) y el detalle de la partida. Lógica pura con pruebas.
 *
 * Hasta el 10-oct esta pieza también decidía qué consejo mostrar y en qué orden (`ResultFocus`, el ajuste «Lo que avancé / Un consejo para la próxima» y su bloque en la pantalla);
 * desde que los 18 juegos usan el final con sentido, que trae su propio truco, eso quedó sin uso y se quitó.
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
}
