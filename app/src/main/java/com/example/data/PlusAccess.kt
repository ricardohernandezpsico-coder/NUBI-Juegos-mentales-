package com.example.data

/**
 * El interruptor de Nubilus Plus (Tarea 199; `docs/modelo-negocio.md`: gratis + Nubilus Plus). Hoy es UNA sola fuente de verdad LOCAL: una preferencia (`plus_access`, `plus`) que lee el repositorio y expone el ViewModel como `plus`. Nunca un servidor: los juegos viven en el APK y una cuenta solo sincroniza progreso (regla permanente 3).
 *
 * - **Pasajera, NO va en el respaldo** (`BackupRulesTest`): quién tiene Plus lo dirá Google Play al restaurar, no una copia de la preferencia.
 * - **Las builds de depuración empiezan con Plus ENCENDIDO** (lo que se prueba es la página entera) y un botón «[Debug]» de Ajustes lo apaga o lo enciende; la de la tienda, apagado, hasta que haya compra.
 * - **Lo que cuelga de él:** la pestaña «Contrarreloj» (desde la Tarea 215; antes una página dentro de Avance) en su mitad de abajo —las secciones «tu lugar en detalle» y «todas tus marcas»— y el atajo «Todas tus marcas ›» del final; sin Plus, esas dos mitades van en gris con candado y abren una hoja corta que cuenta qué trae. Lo demás del Contrarreloj (jugarlo, el récord de Hoy, el final de siempre) es gratis y no cambia.
 * - **Lo que FALTA (Etapa 4, Google Play Billing):** el botón de compra de la hoja. Cuando exista, `PlusAccess.resolve` recibirá el estado de la suscripción en vez de la preferencia; el resto de la app solo mira `plus` y no cambia.
 */
object PlusAccess {
  const val PREFS = "plus_access"
  const val KEY = "plus"

  /** El interruptor de depuración de los datos de muestra de la comparación (Tareas 202 y 202b): «muchos» (lo normal), «pocos» o «muy pocos» (los dos mundos de «Todavía somos pocos»); guarda el nombre de `SampleComparison.World`. Vive en el mismo archivo pasajero que Plus: NO va en el respaldo y solo lo cambia el botón «[Debug]». */
  const val COMPARE_WORLD_KEY = "compare_world"

  /** Lo guardado ([stored]; null = nunca se tocó) o, sin nada guardado, lo que pide la build: encendido en depuración, apagado en la de la tienda. */
  fun resolve(stored: Boolean?, debugBuild: Boolean): Boolean = stored ?: debugBuild

  // ------------------------------------------------------------------ lo que dice la hoja del candado (sin Plus)

  const val LOCK_LABEL = "Con Nubilus Plus"

  // ------------------------------------------------------------------ las dos losas grises de la pestaña Contrarreloj (Tarea 215; `auditoria-fable/idea-pestana-contrarreloj.md` §3.1)

  /** Sin Plus, lo de Plus de la pestaña va en DOS losas grises con candado, cada una con su título legible (los rótulos de sus secciones con Plus, más cortos para caber en una línea) y la frase «Con Nubilus Plus: …» en negrita. */
  const val LOCKED_PLACE_TITLE = "Comparado con tu edad"
  const val LOCKED_PLACE = "Con Nubilus Plus: tu lugar en detalle."
  const val LOCKED_MARKS_TITLE = "Tus marcas"
  const val LOCKED_MARKS = "Con Nubilus Plus: todas tus marcas."

  // Tarea 215b (Fable, `auditoria-fable/contrarreloj-t215-auditoria.md` §3.1 A1): desde la 215 la losa crema de la pestaña, 200 dp más arriba de la losa gris que abre la hoja, ya da gratis jugar, el récord, la semana y la división; la hoja dice «Gratis: …» con eso y lo de Plus
  // empieza donde de verdad empieza Plus (el porcentaje y la escalera, otras edades y semana a semana, las últimas 10 marcas y el ritmo). Cuando llegue la tarea 3 se suma, primera de la lista, «Los cuatro Contrarrelojes, cualquier día, con partidas libres.».
  const val SHEET_TITLE = "Tus marcas del Contrarreloj, con Nubilus Plus"
  /**
   * La hoja, en TRES líneas (Tarea 203; `auditoria-fable/contrarreloj-rediseno.md` §4). Lo que dice depende de con qué se compara ESTA persona (`Comparison.Kind`, Tarea 205c: NONE si no hay fuente o no tiene franja de edad con que compararse): con la referencia estimada (la de la tienda) la nombra, con personas (datos de muestra de depuración;
   * mañana, personas reales) habla de «personas de tu edad», y sin comparación solo de lo que de verdad hay detrás. Así la hoja nunca promete de más. Las dos primeras constantes son la forma «sin comparación» y la de «personas»; la de la referencia estimada, más abajo, nunca dice «personas».
   */
  const val SHEET_LEAD = "Gratis: jugar, tu récord y tu semana. Con Nubilus Plus, además:"
  /** Lo gratis primero, y después lo que agrega Plus (Tarea 209): con personas reales (futuro) y con la referencia estimada empiezan igual. */
  const val SHEET_LEAD_COMPARED = "Gratis: jugar, tu récord y tu división de la semana. Con Nubilus Plus, además:"
  val SHEET_ITEMS: List<String> = listOf(
    "Tus últimas 10 marcas en barras, con la fecha y el paso de tu récord.",
    "Tu ritmo en los 2 minutos, medio minuto por medio minuto.",
    "Tu récord de las pistas anteriores."
  )
  val SHEET_ITEMS_COMPARED: List<String> = listOf(
    "Tu lugar entre personas de tu edad: qué porcentaje de ellas superas y en qué división estás.",
    "La escalera de divisiones con su primer acierto, y tus semanas seguidas arriba de la mitad.",
    "Tus marcas: el récord, las últimas 10 y tu ritmo en los 2 minutos."
  )

  /**
   * Con la REFERENCIA ESTIMADA (Tarea 205; §4 de Fable y decisión de Opus del 1-nov): la hoja la nombra («referencia estimada», nunca «personas») y NO promete nada más: ningún texto dice que luego se compare con personas reales.
   */
  const val SHEET_LEAD_ESTIMATED = SHEET_LEAD_COMPARED
  val SHEET_ITEMS_ESTIMATED: List<String> = listOf(
    "Qué porcentaje de la referencia estimada para tu edad superas, y la escalera de las cinco divisiones.",
    "Tu lugar en otras edades y semana a semana, con tus semanas seguidas arriba de la mitad.",
    "Todas tus marcas: las últimas 10 en barras, tu ritmo en los 2 minutos y el récord de las pistas anteriores."
  )

  /**
   * Bajo las líneas, con la referencia estimada (Tarea 209): la razón primero, en 16 sp `InkMuted`, y debajo la fila [Comparison.SOURCE_LINK] que abre «De dónde sale la referencia». La palabra «personas» solo va en este «no son personas».
   */
  const val SHEET_NOTE_ESTIMATED = "La referencia no son personas: Nubilus no junta datos de nadie."

  /** La entrada de la hoja: según con qué se compara la persona. */
  fun sheetLead(kind: Comparison.Kind): String = when {
    kind == Comparison.Kind.NONE -> SHEET_LEAD
    kind == Comparison.Kind.ESTIMATED -> SHEET_LEAD_ESTIMATED
    else -> SHEET_LEAD_COMPARED
  }

  fun sheetItems(kind: Comparison.Kind): List<String> = when (kind) {
    Comparison.Kind.NONE -> SHEET_ITEMS
    Comparison.Kind.ESTIMATED -> SHEET_ITEMS_ESTIMATED
    Comparison.Kind.PEOPLE -> SHEET_ITEMS_COMPARED
  }

  /** El botón de la hoja (Tarea 205): «Cerrar», no «Entendido»: en un panel de prueba «Entendido» se leyó como «acepto» y alguien creyó que ya había pagado. El precio y la compra llegan con Google Play Billing (Etapa 4). */
  const val SHEET_OK = "Cerrar"
}
