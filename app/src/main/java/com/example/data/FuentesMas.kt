package com.example.data

/**
 * Un grupo de «Cómo está hecha Nubi» (Tarea 186, 28-oct): una frase en simple de lo que hace la app y debajo los estudios en que se apoya esa decisión.
 */
data class Mecanismo(val key: String, val title: String, val text: String, val studies: List<Estudio>)

/**
 * Los estudios EXTRA de «En qué se basa Nubi» (Tarea 186, 28-oct; Ricardo aprobó la propuesta de bibliografía: `docs/bibliografia/` y `docs/en-que-se-basa-nubi.md`):
 *  - [byGame]: «Más estudios (N)» bajo la losa de cada juego (el estudio principal sigue en `Fuentes.byGame`);
 *  - [mechanisms]: «Cómo está hecha Nubi», un grupo por decisión de diseño (dificultad, tiempo desde los 65, partidas cortas, meta y racha, cómo te fue, elegir, compararse solo contigo, una partida, ver y tocar, elementos de juego);
 *  - [noDecir]: los estudios de «Lo que no decimos» (Simons 2016 y tres revisiones más).
 * Solo entran las filas que las tablas de origen marcan «sí» (69 de 101; las «solo interno» NO van en la app). Cada DOI se comprobó contra doi.org y Crossref el 28-oct (autor y año coinciden con la cita).
 * Las líneas «qué sustenta» son BORRADORES (de los subagentes, con revisión): las aprueba Ricardo. Solo autor, año, la línea en español y el enlace: nada de títulos ni revistas (`VisibleTextsGuardTest` prohíbe «cognitiv…», y varios los traen).
 */
object FuentesMas {
  fun moreLabel(count: Int): String = "Más estudios ($count)"

  /** La fila plegada de cada grupo de «Cómo está hecha Nubi» (Opus, 28-oct: «lo simple primero»; a la vista, el título y la frase). */
  fun studiesLabel(count: Int): String = "Estudios ($count)"

  /** El texto de la sección y su título (entre los juegos y «Las palabras»). */
  const val MECHANISMS_TITLE = "Cómo está hecha Nubi"
  const val MECHANISMS_TEXT = "Esto es lo que hace Nubi más allá de los juegos, y de dónde sale."

  /** Los estudios extra de cada juego (id de `GameRegistry`): los que las tablas marcan «sí», sin repetir el principal. */
  val byGame: Map<String, List<Estudio>> = mapOf(
    "parejas" to listOf(
      Estudio("Naveh-Benjamin, 2000", "10.1037/0278-7393.26.5.1170", "Con la edad, recordar qué iba con qué cuesta más que recordar cada elemento suelto."),
      Estudio("Spencer y Raz, 1995", "10.1037/0882-7974.10.4.527", "En 46 estudios, recordar dónde o cuándo pasó algo cambió más con los años que recordar qué pasó."),
      Estudio("Bender y otros, 2010", "10.1037/a0020595", "En 278 adultos de distintas edades, reconocer parejas de palabras costó más con los años.")
    ),
    "secuencia" to listOf(
      Estudio("Berch y otros, 1998", "10.1006/brcg.1998.1039", "Revisa 25 años de la tarea de Corsi: repetir en orden una secuencia de posiciones."),
      Estudio("Kessels y otros, 2008", "10.1177/1073191108315611", "En 246 adultos de 50 a 92 años, repetir posiciones en orden y repetir números resultaron tareas distintas.")
    ),
    "bodega" to listOf(
      Estudio("Kessels y otros, 2007", "10.1080/00207450600910218", "En una tarea en computador, unir cada objeto con su lugar costó más a mayores que a jóvenes.")
    ),
    "correo" to listOf(
      Estudio("Henry y otros, 2004", "10.1037/0882-7974.19.1.27", "En el laboratorio, acordarse de hacer algo ante una señal cuesta más con los años; en la vida diaria, los mayores rinden mejor."),
      Estudio("Einstein y McDaniel, 1990", "10.1037/0278-7393.16.4.717", "Presentó la tarea de hacer algo cuando aparece una señal y la probó con jóvenes y mayores."),
      Estudio("Kliegel y otros, 2016", "10.1159/000443698", "Resume cómo cambia con la edad recordar hacer algo más tarde, y su peso en la vida diaria.")
    ),
    "stroop" to listOf(
      Estudio("MacLeod, 1991", "10.1037/0033-2909.109.2.163", "Revisa 50 años del efecto Stroop: decir el color de la tinta cuando la palabra dice otro."),
      Estudio("Verhaeghen y De Meersman, 1998", "10.1037/0882-7974.13.1.120", "En 20 estudios, el estorbo de la palabra al nombrar el color fue parecido en jóvenes y mayores, más allá de la rapidez general."),
      Estudio("Wasylyshyn y otros, 2011", "10.1037/a0020912", "Alternar entre dos reglas tiene un costo, y tener las dos reglas en juego pesa más con los años.")
    ),
    "piloto" to listOf(
      Estudio("Verhaeghen y otros, 2003", "10.1037/0882-7974.18.3.443", "Hacer dos tareas a la vez toma más tiempo a los mayores que a los jóvenes; los aciertos cambian poco."),
      Estudio("Pashler, 1994", "10.1037/0033-2909.116.2.220", "Incluso dos tareas simples se estorban al hacerlas juntas, sobre todo al elegir qué responder.")
    ),
    "freno" to listOf(
      Estudio("Rey-Mermet y Gade, 2018", "10.3758/s13423-017-1384-7", "En 176 estudios, la edad cambió poco varias tareas de frenar; en la de la señal de alto, sí."),
      Estudio("Williams y otros, 1999", "10.1037/0012-1649.35.1.205", "En 275 personas de 6 a 81 años, frenar cambió poco en la adultez; responder se hizo más lento.")
    ),
    "satelites" to listOf(
      Estudio("Pylyshyn y Storm, 1988", "10.1163/156856888x00122", "Las personas pueden seguir hasta 5 objetos entre 10 idénticos que se mueven al azar."),
      Estudio("Sekuler y otros, 2008", "10.1068/p5923", "Seguir varios objetos en movimiento costó más a mayores, sobre todo con más velocidad o más tiempo."),
      Estudio("Meyerhoff y otros, 2017", "10.3758/s13414-017-1338-1", "Revisa 30 años de la tarea de seguir varios objetos idénticos y cómo se diseña.")
    ),
    "radar" to listOf(
      Estudio("Waszak y otros, 2008", "10.1007/s00426-008-0139-5", "En 226 personas de 6 a 88 años, reconocer símbolos que aparecen un instante y se tapan sube hasta la juventud y luego baja."),
      Estudio("Enns y Di Lollo, 2000", "10.1016/s1364-6613(00)01520-5", "Una imagen breve puede volverse invisible si otra la sigue, y la atención influye en cuánto se tapa.")
    ),
    "calculo" to listOf(
      Estudio("Duverne y Lemaire, 2004", "10.1093/geronb/59.3.p135", "En 138 adultos de 20 a 80 años, el cálculo de sumas se mantuvo; cambiaron estrategia y rapidez."),
      Estudio("Lipkus y otros, 2001", "10.1177/0272989X0102100105", "Incluso con estudios superiores, a muchos adultos les cuestan las preguntas simples con números.")
    ),
    "engranajes" to listOf(
      Estudio("Hegarty, 1992", "10.1037/0278-7393.18.5.1084", "Para saber cómo se mueve una máquina dibujada, las personas la animan mentalmente pieza por pieza."),
      Estudio("Hegarty y Sims, 1994", "10.3758/bf03200867", "Verificar el movimiento de una pieza cuesta más cuantas más piezas hay que animar en la cadena.")
    ),
    "acoplamiento" to listOf(
      Estudio("Shepard y Metzler, 1971", "10.1126/science.171.3972.701", "Decidir si dos figuras son la misma tarda más cuanto más hay que girarlas."),
      Estudio("Techentin y otros, 2014", "10.1080/0361073X.2014.926773", "En 80 muestras, las tareas espaciales como girar figuras cambiaron con los años, más en el tiempo que en los aciertos.")
    ),
    "aterrizaje" to listOf(
      Estudio("Siegler y Opfer, 2003", "10.1111/1467-9280.02438", "Ubicar números en una recta se vuelve más proporcional con la edad y la experiencia con números.")
    ),
    "anagramas" to listOf(
      Estudio("Brown, 1991", "10.1037/0033-2909.109.2.204", "Tener una palabra en la punta de la lengua le pasa a casi todo el mundo, más o menos cada semana, y es más común con los años."),
      Estudio("Gollan y Brown, 2006", "10.1037/0096-3445.135.3.462", "Con los años aumentan los bloqueos con palabras difíciles, no con las fáciles."),
      Estudio("Facal y otros, 2012", "10.3275/8586", "En 133 adultos, los bloqueos con las palabras aumentaron con los años y se ligaron a la rapidez, no al vocabulario.")
    ),
    "meteoros" to listOf(
      Estudio("Ratcliff y otros, 2004", "10.1037/0882-7974.19.2.278", "Al decidir si algo es una palabra, personas de 60 a 75 años fueron más lentas, pero más precisas."),
      Estudio("Brysbaert y otros, 2016", "10.3389/fpsyg.2016.01116", "Entre los 20 y 60 años, una persona aprende unas 6.000 palabras nuevas (datos en inglés).")
    ),
    "disparate" to listOf(
      Estudio("Nieuwland y Kuperberg, 2008", "10.1111/j.1467-9280.2008.02226.x", "Una frase con «no» que cae bien en su contexto se juzga verdadera o falsa sin trabarse."),
      Estudio("Dale y Duran, 2011", "10.1111/j.1551-6709.2010.01164.x", "Verificar frases con «no» es más trabado, pero un contexto adecuado reduce esa dificultad.")
    ),
    "intrusa" to listOf(
      Estudio("Lin y Murphy, 2001", "10.1037/0096-3445.130.1.3", "Los adultos también agrupan por relaciones de escena (torta y velas), no solo por tipo de cosa."),
      Estudio("Minto-García y otros, 2020", "10.1007/s10936-020-09708-5", "En 120 mayores hispanohablantes, las palabras que se asocian mezclan el tipo de cosa y el contexto (cuna y bebé).")
    )
  )

  /** «Cómo está hecha Nubi»: cada grupo con su frase y sus estudios (que en la pantalla van plegados en «Estudios (N)»). «La dificultad se acomoda a ti» se fusiona con el método de siempre (Levitt y Kaernbach); Salthouse 1996 abre «Más tiempo desde los 65». */
  val mechanisms: List<Mecanismo> = listOf(
    Mecanismo("dificultad", "La dificultad se acomoda a ti", Fuentes.METHOD_TEXT,
    Fuentes.method + listOf(
      Estudio("Wilson y otros, 2019", "10.1038/s41467-019-12552-4", "En modelos de aprendizaje, el punto más provechoso está cerca de 85 aciertos de cada 100.")
    )),
    Mecanismo("mas_tiempo", "Más tiempo desde los 65", "Desde los 65, Nubi da más tiempo para ver y responder, y la rapidez pesa menos en el ajuste del nivel.",
    listOf(
      Fuentes.salthouse,
      Estudio("Verhaeghen y Salthouse, 1997", "10.1037/0033-2909.122.3.231", "En 91 estudios, la rapidez para responder baja con la edad, y baja más después de los 50 años."),
      Estudio("Starns y Ratcliff, 2010", "10.1037/a0018022", "Las personas mayores prefieren no equivocarse aunque tarden más; las personas jóvenes equilibran rapidez y aciertos.")
    )),
    Mecanismo("partidas_cortas", "Partidas cortas, varios días", "Cada juego de la ruta dura unos 2 minutos, y la ruta te invita a volver varios días.",
    listOf(
      Estudio("Cepeda y otros, 2006", "10.1037/0033-2909.132.3.354", "Al recordar palabras, repartir la práctica en varios días suele dejar mejor recuerdo que juntarla en un solo día."),
      Estudio("Kornell y otros, 2010", "10.1037/a0017807", "Espaciar la práctica sirvió a jóvenes y a mayores, aunque los participantes creían que juntarla rendía más."),
      Estudio("Simone y otros, 2013", "10.1093/geronb/gbs096", "Repartir la práctica en días distintos también ayudó a personas mayores, aunque algo menos que a jóvenes.")
    )),
    Mecanismo("meta_racha", "Tu meta y tu racha", "Tienes una meta de días por semana, y un día de descanso por semana no corta tu racha.",
    listOf(
      Estudio("Gardner y otros, 2012", "10.3399/bjgp12X659466", "Repetir una acción en el mismo momento y lugar la vuelve más automática con el tiempo."),
      Estudio("Lally y otros, 2011", "10.1080/13548506.2011.555774", "En un estudio pequeño, de 10 personas, los fines de semana y las vacaciones cortaron la rutina, pero después se retomó."),
      Estudio("Singh y otros, 2024", "10.3390/healthcare12232488", "En 20 estudios, un hábito tardó unos dos meses en formarse, con grandes diferencias entre personas."),
      Estudio("Epton y otros, 2017", "10.1037/ccp0000260", "En 141 artículos, ponerse una meta ayudó un poco a cambiar conductas, más si era exigente o compartida."),
      Estudio("Locke y Latham, 2002", "10.1037/0003-066x.57.9.705", "Las metas claras y concretas orientan el esfuerzo mejor que un «haz lo que puedas».")
    )),
    Mecanismo("como_te_fue", "Saber cómo te fue", "Al terminar cada partida, Nubi te cuenta cómo te fue, sin culpa.",
    listOf(
      Estudio("Wisniewski y otros, 2020", "10.3389/fpsyg.2019.03087", "En 435 estudios escolares, recibir información sobre lo hecho ayudó a aprender, sobre todo si decía algo útil de la tarea."),
      Estudio("Cyr y Anderson, 2015", "10.1037/xlm0000073", "Equivocarse no siempre perjudica: en jóvenes y mayores, algunos errores sirvieron de paso hacia la respuesta.")
    )),
    Mecanismo("tu_eliges", "Tú eliges", "Tú eliges qué jugar, en qué modo y a qué hora quieres el recordatorio.",
    listOf(
      Estudio("Ryan y Deci, 2000", "10.1037/0003-066x.55.1.68", "Sentirse capaz, con libertad de elegir y acompañado sostiene las ganas propias de hacer algo."),
      Estudio("Ng y otros, 2012", "10.1177/1745691612447309", "En 184 conjuntos de datos, sentir libertad de elegir se asoció con más ganas propias de mantener un hábito; es una asociación.")
    )),
    Mecanismo("solo_contigo", "Solo contigo", "Tu avance se compara con tus partidas de antes, no con otras personas.",
    listOf(
      Estudio("Lamont y otros, 2015", "10.1037/a0038586", "En 32 artículos, recordar estereotipos negativos sobre la edad empeoró el desempeño de personas mayores en tareas mentales."),
      Estudio("Armstrong y otros, 2017", "10.1093/geront/gnx056", "En 23 artículos, tener presente el estereotipo de la edad bajó el recuerdo de las personas mayores, de forma pequeña a moderada."),
      Estudio("Hess y otros, 2003", "10.1093/geronb/58.1.p3", "Con el estereotipo de la edad presente, las personas mayores recordaron menos, sobre todo quienes valoraban su memoria."),
      Estudio("Wilson y Ross, 2000", "10.1037/0022-3514.78.5.928", "Las personas se comparan con su propio pasado tanto como con otras, y eso suele sentar mejor.")
    )),
    Mecanismo("una_partida", "Una partida es una partida", "Cada partida es un resultado de ese día, no una medida de lo que puedes hacer.",
    listOf(
      Estudio("Calamia y otros, 2012", "10.1080/13854046.2012.680913", "En unos 1.600 resultados, repetir la misma prueba subió el puntaje por pura práctica, algo que hay que tener en cuenta."),
      Estudio("Hedge y otros, 2018", "10.3758/s13428-017-0935-1", "En tareas clásicas de reacción, el puntaje de una misma persona cambia bastante de una vez a otra.")
    )),
    Mecanismo("ver_y_tocar", "Fácil de ver y de tocar", "Letra grande, buen contraste, botones grandes, y Nubi respeta que quites las animaciones en Android.",
    listOf(
      Estudio("Wildenbos y otros, 2018", "10.1016/j.ijmedinf.2018.03.012", "La vista, el tacto, la memoria y la motivación cambian con la edad y afectan el uso de apps."),
      Estudio("Owsley, 2011", "10.1016/j.visres.2010.10.020", "Con la edad baja la capacidad de distinguir contrastes y de ver con poca luz."),
      Estudio("Yeh, 2020", "10.1016/j.heliyon.2020.e04147", "En un estudio con aparatos de salud, con letra de 22 puntos, las personas de 65 años o más usaron la pantalla tan bien como las personas jóvenes."),
      Estudio("Yeh, 2025", "10.1177/00315125251345594", "En 110 personas de 65 años o más, los botones de 16 milímetros rindieron mejor."),
      Estudio("Staab y otros, 2017", "10.3233/VES-170622", "En ciertos mareos crónicos, las imágenes en movimiento o muy cargadas empeoran los síntomas.")
    )),
    Mecanismo("elementos_juego", "Con elementos de juego", "Los puntos, las medallas y la racha son parte del juego; no se presentan como un beneficio.",
    listOf(
      Estudio("Lumsden y otros, 2016", "10.2196/games.5888", "En 33 estudios, los elementos de juego solían dar más ganas de participar; su efecto en el desempeño fue mixto."),
      Estudio("Vermeir y otros, 2020", "10.2196/18644", "En 49 estudios, las tareas con elementos de juego resultaron más motivadoras y más exigentes, sin cambio en el desempeño."),
      Estudio("Koivisto y Malik, 2021", "10.1093/geront/gnaa047", "En personas de 55 años o más, los elementos de juego muestran señales positivas, pero la evidencia todavía es débil.")
    ))
  )

  /** «Lo que no decimos»: Simons 2016 (el de siempre) y las tres revisiones que se suman. */
  val noDecir: List<Estudio> = listOf(
    Fuentes.simons,
    Estudio("Melby-Lervåg y otros, 2016", "10.1177/1745691616635612", "En 87 publicaciones, practicar tareas de memoria hizo mejor esas mismas tareas, pero no otras habilidades."),
    Estudio("Sala y otros, 2018", "10.1037/bul0000139", "En más de 300 resultados, jugar videojuegos no mostró mejoras en habilidades mentales fuera del juego."),
    Estudio("Gates y otros, 2020", "10.1002/14651858.CD012277.pub3", "En 8 ensayos con personas de 65 años o más, los programas de ejercicios en pantalla mostraron beneficios pequeños e inciertos, que no se mantuvieron al año.")
  )
}
