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

  /** Lo guardado ([stored]; null = nunca se tocó) o, sin nada guardado, lo que pide la build: encendido en depuración, apagado en la de la tienda. */
  fun resolve(stored: Boolean?, debugBuild: Boolean): Boolean = stored ?: debugBuild

  // ------------------------------------------------------------------ lo que dice la hoja del candado (sin Plus)

  const val LOCK_LABEL = "Con Nubi Plus"
  const val SHEET_TITLE = "Tus marcas del Contrarreloj, con Nubi Plus"
  const val SHEET_LEAD = "Una página con todas tus marcas, comparadas solo contigo:"
  val SHEET_ITEMS: List<String> = listOf(
    "Tu récord de cada juego, con su fecha.",
    "Tus últimas 10 marcas, en barras.",
    "Esta semana y la pasada, lado a lado.",
    "Tus aciertos y tus errores.",
    "Tu récord de las pistas anteriores."
  )
  const val SHEET_OK = "Entendido"
}
