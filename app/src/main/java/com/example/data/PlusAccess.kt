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
   * La hoja, en TRES líneas (Tarea 203; `auditoria-fable/contrarreloj-rediseno.md` §4). Lo que dice depende de si en esta build hay comparación con personas de tu edad (`Comparison.sourceFor`: hoy solo la de depuración, con datos de muestra; el día que haya servidor, también la de la tienda):
   * con ella habla del lugar, la escalera y tus marcas; sin ella (la tienda), solo de lo que de verdad hay detrás. Así la hoja nunca promete de más.
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

  fun sheetLead(compared: Boolean): String = if (compared) SHEET_LEAD_COMPARED else SHEET_LEAD
  fun sheetItems(compared: Boolean): List<String> = if (compared) SHEET_ITEMS_COMPARED else SHEET_ITEMS
  const val SHEET_OK = "Entendido"
}
