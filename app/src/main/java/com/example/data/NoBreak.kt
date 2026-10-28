package com.example.data

/**
 * El número no se separa de su unidad (Tarea 185, Ricardo el 28-oct: «Tu mejor racha: 21 días» se partió entre «21» y «días»). Cambia por un espacio duro (U+00A0, que no deja partir el renglón) el espacio que va entre una cifra y la palabra que la sigue («21 días», «41 partidas», «8 de octubre», «de 15 medallas»)
 * y el que va entre «de» y un año («octubre de 2025»). Entre «de» y otra cifra («de 15») NO: con letra grande y un cuadro angosto «de 15 medallas» no cabe en un renglón, y es mejor dejar «de» solo que partir «15» de «medallas». Solo se usa para lo que SE VE; lo que lee el lector de pantalla y los textos de lógica quedan con espacios normales.
 */
const val NBSP = "\u00A0"

private val DIGIT_THEN_WORD = Regex("""(\d) (?=\p{L})""")
private val DE_THEN_YEAR = Regex("""\bde (?=(?:19|20)\d{2}\b)""")

fun String.glueNumbers(): String = replace(DIGIT_THEN_WORD, "$1$NBSP").replace(DE_THEN_YEAR, "de$NBSP")
