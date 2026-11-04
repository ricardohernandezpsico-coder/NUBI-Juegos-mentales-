package com.example.data

/**
 * El número no se separa de su unidad (Tarea 185, Ricardo el 28-oct: «Tu mejor racha: 21 días» se partió entre «21» y «días»). Cambia por un espacio duro (U+00A0, que no deja partir el renglón) el espacio que va entre una cifra y la palabra que la sigue («21 días», «41 partidas», «8 de octubre», «de 15 medallas»)
 * y el que va entre «de» y un año («octubre de 2025»). Entre «de» y otra cifra («de 15») NO: con letra grande y un cuadro angosto «de 15 medallas» no cabe en un renglón, y es mejor dejar «de» solo que partir «15» de «medallas». Solo se usa para lo que SE VE; lo que lee el lector de pantalla y los textos de lógica quedan con espacios normales.
 */
const val NBSP = "\u00A0"

private val DIGIT_THEN_WORD = Regex("""(\d) (?=\p{L})""")
private val DIGIT_THEN_PERCENT = Regex("""(\d) (?=%)""")
private val DE_THEN_YEAR = Regex("""\bde (?=(?:19|20)\d{2}\b)""")

fun String.glueNumbers(): String = replace(DIGIT_THEN_WORD, "$1$NBSP").replace(DIGIT_THEN_PERCENT, "$1$NBSP").replace(DE_THEN_YEAR, "de$NBSP")

/**
 * La regla de partidas (Tarea 218): «Te quedan 2 partidas de hoy.» y «Hoy ya jugaste tus 3 partidas.» no dejan «hoy.» ni «partidas.» solas en el segundo renglón con letra grande: la cifra con su unidad ([glueNumbers]) y «de hoy» pegados. Solo para lo que SE VE.
 */
fun String.glueGames(): String = glueNumbers().replace(" de hoy", "${NBSP}de${NBSP}hoy")

/** Los nombres de las divisiones de la página «Contrarreloj» (Tarea 203b): «En la Luna» no se parte entre «En la» y «Luna». */
private val DIVISION_NAMES = listOf("En la plataforma", "Sobre las nubes", "En la Luna", "En Marte", "Entre asteroides", "En Júpiter", "Entre estrellas", "En otra galaxia", "En órbita")
// el espacio entre la cifra y su unidad puede venir ya duro ([glueNumbers] corre antes): se acepta cualquiera de los dos
private val AGE_RANGE = Regex("""\b(\d{2})[ \u00A0]a[ \u00A0](\d{2})[ \u00A0]años\b""")
private val AGE_OR_MORE = Regex("""\b(\d{2})[ \u00A0]años[ \u00A0]o[ \u00A0]más\b""")
private val WE_NEED = Regex("""\b(Te) (falta(?:n)?) (\d+)[ \u00A0](aciertos?)\b""")

/**
 * Pega con espacios duros lo que se lee de un golpe en la página «Contrarreloj» (Tarea 203b; Tarea 205c: solo con [fontScale] de 1,3 o menos, ver abajo; con letra 2,0 se leía «45 a / 54 años» y, a 360 dp, «para En la / Luna.»): el nombre de una división («En la Luna»), la franja de edad entera con su unidad («45 a 54 años», «75 años o más») y «Te faltan 7 aciertos».
 * Como [glueNumbers], solo para lo que SE VE: lo que lee el lector de pantalla y los textos de lógica quedan con espacios normales. Se usa DESPUÉS de [glueNumbers] o en lugar de él cuando el texto ya trae las cifras pegadas.
 */
fun String.glueNames(fontScale: Float = 1f): String {
  var s = this
  // Tarea 205c: con letra mayor a 1,3 los nombres de división y «Te faltan N aciertos» NO se pegan: pegados miden más que el ancho de su renglón (a 360 dp, 16 sp negrita y letra 2,0, «Te faltan 12 aciertos» mide ~300 dp en ~246) y Android los corta por letras
  // («acier / tos»). Mejor partir entre palabras que por dentro de una. La cifra con su unidad («12 aciertos») sí queda pegada siempre ([glueNumbers]).
  if (fontScale <= GLUE_NAMES_MAX_FONT_SCALE) {
    for (name in DIVISION_NAMES) s = s.replace(name, name.replace(" ", NBSP))
    s = s.replace(WE_NEED, "$1$NBSP$2$NBSP$3$NBSP$4")
  }
  return s.replace(AGE_RANGE, "$1${NBSP}a$NBSP$2${NBSP}años").replace(AGE_OR_MORE, "$1${NBSP}años${NBSP}o${NBSP}más")
}

/** La letra más grande con la que se pegan los nombres de división y «Te faltan N aciertos» (Tarea 205c): arriba de esto un tramo pegado puede no caber en el renglón. */
const val GLUE_NAMES_MAX_FONT_SCALE = 1.3f
