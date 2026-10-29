package com.example.data

/**
 * «En qué se basa Nubi» (Tarea 185, 28-oct; `auditoria-fable/tu-pestana.md` §3): de qué estudio sale la tarea de cada juego, en una línea y con el estudio a un toque. NO es una prueba de que Nubi funcione: Nubi no tiene estudios propios ni promete mejorar nada
 * (`Fuentes.NO_DECIMOS`); es de dónde salen las tareas. Solo autor y año, nuestra línea en español y el enlace: ni el título en inglés ni la revista, porque `VisibleTextsGuardTest` prohíbe «cognitiv…» en cualquier texto del código y varios títulos y revistas
 * lo traen (la página del DOI los trae a un toque). Los 24 DOI se comprobaron contra doi.org y Crossref el 28-oct (autor y año coinciden con la cita; `docs/en-que-se-basa-nubi.md`). Las 17 líneas de «qué tomamos» las APROBÓ Ricardo el 28-oct (con un cambio en la de Constelaciones: «…lo que más cambia con los años», que es lo que dice Old y Naveh-Benjamin, 2008, sobre la memoria asociativa).
 *
 * Desde la Tarea 186 (28-oct) la pantalla suma, además de la fuente principal de cada juego, «Más estudios (N)» bajo cada losa, la sección «Cómo está hecha Nubi» y una «Lo que no decimos» más amplia: eso vive en [FuentesMas].
 *
 * Lo que sale de aquí en una `Intent` de `ACTION_VIEW` hacia el navegador: la app no tiene (ni necesita) permiso de internet.
 */
data class Fuente(
  /** El juego de la app que parte de este estudio (el id de `GameRegistry`). */
  val gameId: String,
  /** «Qué tomamos»: una línea en simple de lo que el juego toma de esa tarea. */
  val taken: String,
  /** La cita corta, «Autor, año» (la misma de «¿Por qué importa?» en `FinalesConSentido`). */
  val cite: String,
  /** El DOI, sin el prefijo de la dirección (`10.xxxx/…`). */
  val doi: String
) {
  val url: String get() = Fuentes.urlOf(doi)
}

/** Un estudio suelto (los «Más estudios» de cada juego, «Cómo está hecha Nubi», lo que no decimos): la cita corta, su DOI y su línea «qué sustenta» en español (Tarea 186; `FuentesMas`). */
data class Estudio(val cite: String, val doi: String, val line: String) {
  val url: String get() = Fuentes.urlOf(doi)
}

object Fuentes {
  fun urlOf(doi: String): String = "https://doi.org/$doi"

  /** La frase del enlace de cada losa (16 sp, tras la cita). */
  const val SEE_STUDY = "Ver el estudio ›"

  const val INTRO = "Cada juego parte de una tarea que la psicología estudia desde hace años. Aquí está de dónde sale cada uno, en una línea, y el estudio para quien quiera leerlo."
  const val PROMISE = "Nubi no promete mejorar nada: es un juego."

  /** Los 17 juegos de la app, con la fuente de su tarea (el orden de cada área es el de `GameRegistry`). */
  val byGame: Map<String, Fuente> = listOf(
    // Memoria
    Fuente("parejas", "Recordar qué va con qué es lo que más cambia con los años.", "Old y Naveh-Benjamin, 2008", "10.1037/0882-7974.23.1.104"),
    Fuente("secuencia", "Guardar un orden un rato mientras lo usamos: la memoria de trabajo.", "Baddeley, 2003", "10.1038/nrn1201"),
    Fuente("bodega", "Recordar dónde quedó cada cosa: la cosa, el lugar y lo que las une.", "Postma, Kessels y van Asselen, 2008", "10.1016/j.neubiorev.2008.05.001"),
    Fuente("correo", "Acordarse de hacer algo más tarde sostiene el día a día.", "Hering y otros, 2014", "10.1007/s00426-014-0566-4"),
    // Atención
    Fuente("stroop", "Frenar la lectura automática y cambiar de regla.", "Diamond, 2013", "10.1146/annurev-psych-113011-143750"),
    Fuente("piloto", "Hacer dos cosas a la vez se vuelve más difícil con los años.", "Beurskens y Bock, 2012", "10.1155/2012/131608"),
    Fuente("freno", "Frenar una acción que ya empezó, justo a tiempo.", "Verbruggen y Logan, 2008", "10.1016/j.tics.2008.07.005"),
    Fuente("satelites", "Seguir varios objetos que se mueven a la vez.", "Trick, Perl y Sethi, 2005", "10.1093/geronb/60.2.p102"),
    Fuente("radar", "Qué tan rápido vemos y reconocemos algo que aparece un instante.", "Owsley, 2013", "10.1016/j.visres.2012.11.014"),
    // Razonamiento
    Fuente("calculo", "Entender las cantidades ayuda a comprender riesgos y a decidir.", "Reyna y otros, 2009", "10.1037/a0017327"),
    Fuente("engranajes", "Imaginar cómo se mueven las piezas de una máquina antes de moverlas.", "Hegarty, 2004", "10.1016/j.tics.2004.04.001"),
    Fuente("acoplamiento", "Girar formas en la mente: una habilidad espacial.", "Uttal y otros, 2013", "10.1037/a0028446"),
    Fuente("aterrizaje", "Ubicar números a ojo guía cómo valoramos montos.", "Schley y Peters, 2014", "10.1177/0956797613515485"),
    // Lenguaje
    Fuente("anagramas", "Tener una palabra en la punta de la lengua es normal y crece con los años.", "Shafto y otros, 2007", "10.1162/jocn.2007.19.12.2060"),
    Fuente("meteoros", "El vocabulario se mantiene, y hasta crece, con los años.", "Verhaeghen, 2003", "10.1037/0882-7974.18.2.332"),
    Fuente("disparate", "Las frases con «no» piden más esfuerzo a todas las personas.", "Xiang, Kramer y Nordmeyer, 2020", "10.1037/xlm0000851"),
    Fuente("intrusa", "Ordenamos lo que sabemos por tipo de cosa y por lo que va junto.", "Mirman, Landrigan y Britt, 2017", "10.1037/bul0000092")
  ).associateBy { it.gameId }

  // ---- Cómo se ajusta la dificultad (docs/DDA-comun.md y docs/dificultad-y-avance.md). Desde la Tarea 186 ya no es una sección aparte: es el primer grupo de «Cómo está hecha Nubi» («La dificultad se acomoda a ti», `FuentesMas.mechanisms`).
  const val METHOD_TEXT = "El nivel sube y baja hasta donde aciertas 8 de cada 10. Es un método de los laboratorios de percepción."
  val method: List<Estudio> = listOf(
    Estudio("Levitt, 1971", "10.1121/1.1912375", "La escalera «sube y baja»: el nivel sube con los aciertos y baja con los errores hasta quedar cerca de un nivel de aciertos fijo."),
    Estudio("Kaernbach, 1991", "10.3758/BF03214307", "La escalera con pasos de distinto peso permite quedarse cerca de 8 aciertos de cada 10.")
  )

  /** Salthouse 1996 estaba en el método desde la Tarea 185; desde la 186 (Opus, 28-oct) vive en «Más tiempo desde los 65»: trata de la rapidez con la edad (`FuentesMas.mechanisms`). */
  val salthouse = Estudio("Salthouse, 1996", "10.1037/0033-295x.103.3.403", "Con los años cambia la rapidez para procesar información, y eso explica parte de las diferencias entre edades.")

  // ---- Las palabras (SPALEX y EsPal viven en «Licencias y créditos»)
  const val WORDS_TITLE = "Las palabras"
  const val WORDS_TEXT = "Las listas de palabras vienen de SPALEX y EsPal: están en «Licencias y créditos»."

  // ---- Lo que no decimos
  const val NO_DECIR_TITLE = "Lo que no decimos"
  const val NO_DECIR_TEXT = "Que jugar mejore la memoria o la atención fuera del juego no está demostrado. Por eso Nubi no lo promete."
  /** La revisión que sostiene la frase de arriba (Simons y otros, 2016); desde la Tarea 186 le acompañan otras tres (`FuentesMas.noDecir`). */
  val simons = Estudio("Simons y otros, 2016", "10.1177/1529100616661983", "Revisó los estudios de los programas de juegos mentales: lo practicado mejora, pero no hay evidencia suficiente de que mejore lo demás.")
}
