package com.example.data

/**
 * «De dónde sale la referencia» (Tarea 209, 1-nov; `auditoria-fable/claridad-plus-y-ayuda.md` §1.1.1): los estudios reales que fijan los números de la referencia estimada del Contrarreloj (los que nombra `AgeReference.cs`, Tarea 204; el §2.2 de `auditoria-fable/referencia-estimada.md`), agrupados,
 * con «Autor, año · N personas», una línea en español y el estudio a un toque, en el mismo molde de «En qué se basa Nubi» (`Estudio`, `StudyRow`). Se abre desde la página «Contrarreloj», desde la hoja del candado, desde la pregunta «¿Con qué me compara el Contrarreloj?» y desde una fila de «En qué se basa Nubi».
 *
 * Lo que NO dice, a propósito: cuántos robots son ni qué escala se calibró (el número de robots nunca se muestra), «Nubi Plus» (no es una venta), títulos en inglés (la guardia de «cognitiv…») ni ninguna promesa de que la referencia pase a ser de personas (regla del 1-nov: nada lo promete mientras no haya servidor).
 * El segundo párrafo lo redactó Opus el 1-nov (decisión de Ricardo): dice con honestidad que los números se calcularon a partir de los estudios y cómo se ajustó el ritmo del programa, sin cifras y sin la palabra «interpolación». Los 10 DOI se comprobaron contra doi.org y Crossref el 1-nov
 * (autor y año coinciden con la cita; el de Cerella 1985, el único sin confirmar en el diseño, resuelve a «Information processing rates in the elderly», 1985; Habekost y otros salió en línea en 2012 y en papel en abril de 2013: se cita 2013, como `AgeReference.cs`). Brinley (la recta de las lentitudes) es un método sin DOI y no va.
 */
object ReferenciaFuentes {
  const val TITLE = "De dónde sale la referencia"

  /** La fila de «En qué se basa Nubi», entre «Cómo está hecha Nubi» y «Las palabras»: abre esta hoja (el lector dice «…, botón»). */
  const val FUENTES_ROW = "De dónde sale la referencia del Contrarreloj ›"

  /** El primer párrafo: primero la razón (no son otras personas) y luego qué es la referencia. */
  const val INTRO = "No son otras personas: Nubi no junta datos de nadie. La referencia se arma con estudios publicados que midieron cómo cambian con la edad la rapidez y la exactitud en tareas parecidas, y con un programa que jugó esta misma pista en un computador con esos números. Cada estudio dice cuántas personas tuvo."

  /** El segundo párrafo, en `OnNightDim` (la decisión de Ricardo: es lo honesto; sin cifras). */
  const val ABOUT = "Los números de cada edad se calcularon a partir de esos estudios; ninguno usó los juegos de Nubi. El ritmo del programa se ajustó con las primeras partidas de quienes probaron Nubi antes de salir."

  /** El grupo que no tiene losas: ¿Verdad o disparate? y Acoplamiento salen de los mismos estudios de rapidez. */
  const val SHARED_LINE = "Sus tiempos salen de los mismos estudios de rapidez: la referencia no tiene un estudio propio de cada juego."

  /** Un grupo de la hoja: su título, sus estudios (puede no tener) y, en lugar de losas, una línea ([line]). */
  data class Group(val key: String, val title: String, val studies: List<Estudio>, val line: String? = null)

  val groups: List<Group> = listOf(
    Group(
      "rapidez", "La rapidez con la edad", listOf(
        Estudio("Der y Deary, 2006 · 7.130 adultos", "10.1037/0882-7974.21.1.62", "El tiempo de reacción se alarga con la edad y varía más entre personas."),
        Estudio("Hultsch y otros, 2002 · 862 personas de 17 a 94 años", "10.1093/geronb/57.2.p101", "Las personas mayores varían más de un ensayo a otro."),
        Estudio("Ratcliff y otros, 2010 · 130 personas en tres grupos de edad", "10.1016/j.cogpsych.2009.09.001", "La edad alarga el tiempo de decidir, no la calidad de lo que se ve."),
        Estudio("Cerella, 1985 · revisión de muchos estudios", "10.1037/0033-2909.98.1.67", "Con la edad todo se hace un poco más lento, y parejo en tareas distintas."),
        Estudio("Myerson y otros, 1990 · revisión", "10.1037/0033-295x.97.4.475", "Un modelo de cuánto se alarga cada paso con la edad."),
        Estudio("Salthouse, 1996 · revisión", "10.1037/0033-295x.103.3.403", "La rapidez explica buena parte de las diferencias entre edades.")
      )
    ),
    Group(
      "stroop", "Tinta o Palabra", listOf(
        Estudio("Van der Elst y otros, 2006 · 1.856 personas de 24 a 81 años", "10.1177/1073191105283427", "La edad afecta sobre todo los tiempos y poco los errores."),
        Estudio("Troyer y otros, 2006 · 272 personas de 18 a 94 años", "10.1080/138255890968187", "Los errores en la parte difícil también suben con la edad.")
      )
    ),
    Group(
      "radar", "Rescate relámpago", listOf(
        Estudio("Brockmole y Logie, 2013 · 55.753 personas de 8 a 75 años", "10.3389/fpsyg.2013.00012", "Lo que se retiene de un vistazo baja desde los 20 años."),
        Estudio("Habekost y otros, 2013 · 33 personas de 69 a 87 años", "10.1111/sjop.12008", "La rapidez para reconocer algo breve se reduce a la mitad entre los 70 y los 85.")
      )
    ),
    Group("otros", "¿Verdad o disparate? y Acoplamiento", emptyList(), SHARED_LINE)
  )

  /** Todos los estudios de la hoja, en el orden en que se ven. */
  val studies: List<Estudio> = groups.flatMap { it.studies }
}
