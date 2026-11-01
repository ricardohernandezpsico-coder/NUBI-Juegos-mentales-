package com.example.data

/**
 * El interruptor de Nubi Plus (Tarea 199; `docs/modelo-negocio.md`: gratis + Nubi Plus). Hoy es UNA sola fuente de verdad LOCAL: una preferencia (`plus_access`, `plus`) que lee el repositorio y expone el ViewModel como `plus`. Nunca un servidor: los juegos viven en el APK y una cuenta solo sincroniza progreso (regla permanente 3).
 *
 * - **Pasajera, NO va en el respaldo** (`BackupRulesTest`): quién tiene Plus lo dirá Google Play al restaurar, no una copia de la preferencia.
 * - **Las builds de depuración empiezan con Plus ENCENDIDO** (lo que se prueba es la página entera) y un botón «[Debug]» de Ajustes lo apaga o lo enciende; la de la tienda, apagado, hasta que haya compra.
 * - **Lo que cuelga de él:** la página «Contrarreloj» de Avance entera y el atajo «Todas tus marcas ›» del final; sin Plus, la puerta de Avance dice «Con Nubi Plus» y abre una hoja corta que cuenta qué trae. Lo demás del Contrarreloj (jugarlo, el récord de Hoy, el final de siempre) es gratis y no cambia.
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

  const val LOCK_LABEL = "Con Nubi Plus"
  const val SHEET_TITLE = "Tus marcas del Contrarreloj, con Nubi Plus"
  /**
   * La hoja, en TRES líneas (Tarea 203; `auditoria-fable/contrarreloj-rediseno.md` §4). Lo que dice depende de con qué se compara ESTA persona (`Comparison.Kind`, Tarea 205c: NONE si no hay fuente o no tiene franja de edad con que compararse): con la referencia estimada (la de la tienda) la nombra, con personas (datos de muestra de depuración;
   * mañana, personas reales) habla de «personas de tu edad», y sin comparación solo de lo que de verdad hay detrás. Así la hoja nunca promete de más. Las dos primeras constantes son la forma «sin comparación» y la de «personas»; la de la referencia estimada, más abajo, nunca dice «personas».
   */
  const val SHEET_LEAD = "Una página con todas tus marcas, comparadas solo contigo:"
  const val SHEET_LEAD_COMPARED = "Una página con todas tus marcas, comparadas contigo y con personas de tu edad:"
  val SHEET_ITEMS: List<String> = listOf(
    "Tu récord, tu mejor marca de esta semana y tus últimas 10 marcas, en barras.",
    "Tu ritmo en los 2 minutos, medio minuto por medio minuto.",
    "Tu récord de las pistas anteriores."
  )
  val SHEET_ITEMS_COMPARED: List<String> = listOf(
    "Tu lugar entre personas de tu edad: cuántas de cada 100 superas y en qué división estás.",
    "La escalera de divisiones con su primer acierto, y tus semanas seguidas arriba de la mitad.",
    "Tus marcas: el récord, las últimas 10 y tu ritmo en los 2 minutos."
  )

  /**
   * Con la REFERENCIA ESTIMADA (Tarea 205; §4 de Fable y decisión de Opus del 1-nov): la hoja la nombra («referencia estimada», nunca «personas») y NO promete nada más: ningún texto dice que luego se compare con personas reales.
   */
  const val SHEET_LEAD_ESTIMATED = "Una página con todas tus marcas, comparadas contigo y con una referencia estimada para tu edad:"
  val SHEET_ITEMS_ESTIMATED: List<String> = listOf(
    "Tu lugar en una referencia estimada para tu edad: cuántas de cada 100 marcas superas y en qué división estás.",
    "La escalera de divisiones con su primer acierto, y tus semanas seguidas arriba de la mitad.",
    "Tus marcas: el récord, las últimas 10 y tu ritmo en los 2 minutos."
  )

  fun sheetLead(kind: Comparison.Kind): String = when (kind) {
    Comparison.Kind.NONE -> SHEET_LEAD
    Comparison.Kind.ESTIMATED -> SHEET_LEAD_ESTIMATED
    Comparison.Kind.PEOPLE -> SHEET_LEAD_COMPARED
  }

  fun sheetItems(kind: Comparison.Kind): List<String> = when (kind) {
    Comparison.Kind.NONE -> SHEET_ITEMS
    Comparison.Kind.ESTIMATED -> SHEET_ITEMS_ESTIMATED
    Comparison.Kind.PEOPLE -> SHEET_ITEMS_COMPARED
  }

  /** El botón de la hoja (Tarea 205): «Cerrar», no «Entendido»: en un panel de prueba «Entendido» se leyó como «acepto» y alguien creyó que ya había pagado. El precio y la compra llegan con Google Play Billing (Etapa 4). */
  const val SHEET_OK = "Cerrar"
}
