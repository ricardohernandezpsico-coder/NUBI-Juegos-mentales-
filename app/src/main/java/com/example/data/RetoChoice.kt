package com.example.data

/**
 * «Sin reloj» o «Con reloj» por juego (6-oct, pedido de Ricardo: la elección vive en la ficha del juego, junto a la dificultad, no escondida en Ajustes). Lógica pura con pruebas.
 *
 * - La elección se recuerda POR JUEGO (preferencias `profile_extra`, clave `reto_<juego>`, que van en el respaldo).
 * - Mientras un juego no tenga elección propia, vale el ajuste que la persona tenía en Ajustes («Modo contra el reloj (Reto)»; `UserSettings.defaultTimed`): así nadie pierde lo que tenía. El
 *   interruptor de Ajustes se quitó; su último valor queda como elección inicial.
 * - El camino diario de Hoy va siempre sin reloj.
 * - Si algún juego no tuviera Reto, se lista en [WITHOUT_RETO] y su ficha no muestra la elección.
 */
object RetoChoice {
  private const val PREFIX = "reto_"

  /** Los juegos sin Reto: «La estación de correo» (8-oct) son 4 días de 50 s con su hoja y su resumen: no hay una versión sin reloj ni una contra el reloj (el día YA tiene su ritmo), así que su ficha no muestra la elección. Los demás tienen Reto (cada uno con su duración). */
  val WITHOUT_RETO: Set<String> = setOf("correo")

  /** Duración del Reto de cada juego, en segundos (la de Unity, de cada juego en `docs/juegos`). Constelaciones: 180 s en total (6 cielos). */
  private val SECONDS = mapOf(
    "secuencia" to 90, "stroop" to 60, "calculo" to 120, "engranajes" to 120, "bodega" to 120, "parejas" to 180, "anagramas" to 120, "piloto" to 90, "radar" to 120,
    "satelites" to 120, "freno" to 120, "aterrizaje" to 120, "meteoros" to 120, "disparate" to 120, "intrusa" to 120,
    "acoplamiento" to 120
  )

  fun key(gameId: String): String = PREFIX + gameId

  /**
   * ¿La ficha ofrece «¿Con reloj?» en este juego? No en los sin Reto (Correo) ni, desde la Tarea 181 (26-oct), en los cuatro que tienen Contrarreloj propio ([TimeTrialDay.GAMES]): ahí el tiempo es el botón «Contrarreloj» de la ficha, no una elección. El modo Reto sigue en el código de Unity y en su smoke;
   * lo que cambia es que esas cuatro fichas ya no lo lanzan. Un `reto_<juego>` guardado de uno de esos cuatro se ignora (se juega sin reloj) y NO se borra (va en el respaldo).
   */
  fun supports(gameId: String): Boolean = gameId !in WITHOUT_RETO && !TimeTrialDay.supports(gameId)

  /** La elección que vale: la guardada de ese juego o, si no hay, la inicial (el ajuste viejo de Ajustes). Un juego sin «¿Con reloj?» siempre va sin reloj. */
  fun resolve(gameId: String, saved: Boolean?, initial: Boolean): Boolean = supports(gameId) && (saved ?: initial)

  /** Las elecciones guardadas, desde todas las preferencias (`clave → valor`): solo las claves `reto_<juego>` con un valor sí/no. */
  fun decode(all: Map<String, *>): Map<String, Boolean> =
    all.mapNotNull { (k, v) -> if (k.startsWith(PREFIX) && k.length > PREFIX.length && v is Boolean) k.removePrefix(PREFIX) to v else null }.toMap()

  fun seconds(gameId: String): Int? = SECONDS[gameId]

  /** «1 minuto», «1 minuto y 30 segundos», «2 minutos», «45 segundos»: cómo se dice la duración en la ficha (Tarea 160: antes «2 min» y «45 s»). */
  fun durationText(seconds: Int): String {
    val m = seconds / 60
    val s = seconds % 60
    val minutes = if (m == 1) "1 minuto" else "$m minutos"
    val secs = if (s == 1) "1 segundo" else "$s segundos"
    return when {
      m == 0 -> secs
      s == 0 -> minutes
      else -> "$minutes y $secs"
    }
  }

  /** Lo que dice la opción: «Con reloj (2 minutos)»; sin duración fija, solo «Con reloj». Un solo nombre en toda la app (Tarea 150, B1.2): ya no «Contra el reloj», «Reto» ni «Precisión». */
  fun timedLabel(gameId: String): String = seconds(gameId)?.let { "Con reloj (${durationText(it)})" } ?: "Con reloj"

  const val UNTIMED_LABEL = "Sin reloj"

  // ---- Lo elegido, dicho en palabras (Tarea 140: la ficha con UNA decisión visible; funciones puras con pruebas) ----

  /** El modo como suena en el botón «Jugar · …»: «a tu medida», «suave», «en Desafío», «al límite». */
  fun modeWords(mode: PlayMode): String = when (mode) {
    PlayMode.SUAVE -> "suave"
    PlayMode.A_TU_MEDIDA -> "a tu medida"
    PlayMode.DESAFIO -> "en Desafío"
    PlayMode.EXPERTO -> "al límite"
  }

  /** «con reloj» o «sin reloj»; nada en un juego sin Reto (Correo): ahí no hay nada que decir del reloj. [timed] sin valor cuenta como «sin reloj». */
  private fun clockWords(timed: Boolean?, supportsReto: Boolean): String? = if (!supportsReto) null else if (timed == true) "con reloj" else "sin reloj"

  /** Lo elegido en dos o tres palabras: «a tu medida · con reloj», «suave · sin reloj», «en Desafío · con reloj»; en un juego sin Reto, solo «a tu medida». */
  fun playDetail(mode: PlayMode, timed: Boolean?, supportsReto: Boolean): String =
    listOfNotNull(modeWords(mode), clockWords(timed, supportsReto)).joinToString(" · ")

  /** El texto del botón grande de la ficha: «Jugar · a tu medida · con reloj», «Jugar · suave · sin reloj», «Jugar · al límite · sin reloj»; en un juego sin Reto, «Jugar · a tu medida». */
  fun playLabel(mode: PlayMode, timed: Boolean?, supportsReto: Boolean): String = "Jugar · " + playDetail(mode, timed, supportsReto)

  /** Lo mismo para el lector de pantalla, con comas en vez de puntos medios: «a tu medida, con reloj». */
  fun playSpoken(mode: PlayMode, timed: Boolean?, supportsReto: Boolean): String =
    listOfNotNull(modeWords(mode), clockWords(timed, supportsReto)).joinToString(", ")

  /** La línea de resumen de la ficha plegada: «A tu medida · con reloj (2 min)», «Suave · sin reloj», «Desafío · con reloj (2 min)»; en un juego sin Reto, solo «A tu medida». [seconds] = la duración del Reto de ese juego. */
  fun summaryLabel(mode: PlayMode, timed: Boolean?, supportsReto: Boolean, seconds: Int? = null): String {
    val clock = clockWords(timed, supportsReto)?.let { if (timed == true && seconds != null) "$it (${durationText(seconds)})" else it }
    return listOfNotNull(mode.label, clock).joinToString(" · ")
  }

}
