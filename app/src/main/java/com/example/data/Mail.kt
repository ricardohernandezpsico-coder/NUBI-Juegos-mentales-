package com.example.data

/**
 * «La estación de correo» (id `correo`, antes el vuelo de Correo Estelar; Unity: `Games/Correo/MailContract.cs`; diseño en docs/diseno-correo-estacion.md): la lectura de lo que manda Unity para la pantalla final y el récord que se guarda.
 * Lógica pura con pruebas.
 *
 * La medida es «tu memoria para lo pendiente» (memoria prospectiva): de todos los encargos de la partida, cuántos cumpliste. Cuentan los encargos POR EVENTO (cartas con sello dorado o con lazo que había que guardar en la caja fuerte), POR HORA
 * (encender el faro a su hora; el reloj va tapado) y los CANCELADOS por la radio (no hacerlos cuenta como cumplido; hacerlos igual es un error de comisión). Acompañan las miradas al reloj cerca de la hora (en el 30 % del día antes de la
 * ventana o dentro de ella), las cartas bien puestas y la etapa más alta. El récord es el de «cartas bien puestas en un día perfecto» (preferencias `correo_record`, que SÍ van en el respaldo; se manda a Unity en cada partida).
 * Una partida sin encargos no tiene medida («—») y no se guarda. No se habla de enfermedades ni se promete nada de salud: va la nota común «No es un diagnóstico».
 */
object Mail {
  /** Los 5 grupos de etapas de la pantalla final (2 etapas por grupo). */
  const val GROUPS = 5
  /** Encargos mínimos para mostrar la medida («no sacar conclusiones de pocos casos»). */
  const val MIN_TODOS_FOR_MEASURE = 1
  /** Miradas al reloj que hacen falta para hablar de cómo se usó. */
  const val MIN_PEEKS_FOR_CLOCK = 1

  /** Encargos cumplidos de la partida: los de evento y de hora, y los cancelados que NO se hicieron. */
  fun done(evHits: Int, timeHits: Int, cancels: Int, commissions: Int): Int =
    evHits.coerceAtLeast(0) + timeHits.coerceAtLeast(0) + (cancels - commissions.coerceIn(0, cancels.coerceAtLeast(0))).coerceAtLeast(0)

  /** Encargos de la partida: por evento, por hora y cancelados. */
  fun total(evTotal: Int, timeTotal: Int, cancels: Int): Int = evTotal.coerceAtLeast(0) + timeTotal.coerceAtLeast(0) + cancels.coerceAtLeast(0)

  /** «Tu memoria para lo pendiente»: el porcentaje de encargos cumplidos (0..100). null sin encargos. */
  fun percent(ok: Int?, all: Int?): Int? {
    if (ok == null || all == null || all < MIN_TODOS_FOR_MEASURE) return null
    return Math.round(100f * ok.coerceIn(0, all) / all)
  }

  /** La marca para ver su evolución: % de encargos cumplidos (más alto = mejor). null sin encargos: esa partida no se guarda como medida. */
  fun mark(ok: Int?, all: Int?): Float? {
    if (ok == null || all == null || all < MIN_TODOS_FOR_MEASURE) return null
    return 100f * ok.coerceIn(0, all) / all
  }

  /** «Tu memoria para lo pendiente: 73 %» o «—» si no hubo encargos. */
  fun headline(ok: Int?, all: Int?): String = percent(ok, all)?.let { "Tu memoria para lo pendiente: $it %" } ?: "Tu memoria para lo pendiente: —"

  /** «8 de 11 encargos cumplidos»; sin encargos, una frase que lo dice sin culpa. */
  fun detailLine(ok: Int?, all: Int?): String =
    if (ok == null || all == null || all < MIN_TODOS_FOR_MEASURE) "No hubo encargos que recordar"
    else "${ok.coerceIn(0, all)} de $all ${if (all == 1) "encargo cumplido" else "encargos cumplidos"}"

  /** «Por evento (cartas señal): 3 de 4»; null si no hubo. */
  fun eventLine(hits: Int?, total: Int?): String? = if (hits == null || total == null || total <= 0) null else "Por evento (cartas señal): ${hits.coerceIn(0, total)} de $total"

  /** «Por hora (faro): 1 de 2»; null si no hubo. */
  fun timeLine(hits: Int?, total: Int?): String? = if (hits == null || total == null || total <= 0) null else "Por hora (faro): ${hits.coerceIn(0, total)} de $total"

  /** «Cancelados que no hiciste: 1 de 2» (hacerlos igual es el error de comisión); null si la radio no canceló nada. */
  fun cancelLine(cancels: Int?, commissions: Int?): String? {
    if (cancels == null || cancels <= 0) return null
    return "Cancelados que no hiciste: ${(cancels - (commissions ?: 0).coerceIn(0, cancels))} de $cancels"
  }

  /** «Miradas al reloj cerca de la hora: 2 de 3»; null si no se miró. */
  fun clockLine(good: Int?, peeks: Int?): String? = if (good == null || peeks == null || peeks < MIN_PEEKS_FOR_CLOCK) null else "Miradas al reloj cerca de la hora: ${good.coerceIn(0, peeks)} de $peeks"

  /** «Cartas bien puestas: 20»; null sin dato. */
  fun cardsLine(right: Int?): String? = if (right == null || right < 0) null else "Cartas bien puestas: $right"

  /** «Etapa más alta: 3 de 5»; null si no es un grupo válido (1..5). */
  fun groupLine(group: Int?): String? = if (group == null || group !in 1..GROUPS) null else "Etapa más alta: $group de $GROUPS"

  /** «Tu mejor día: 20 cartas»; con «¡Nuevo récord!» solo cuando en esta partida se superó el guardado (lo dice Unity: igualarlo no cuenta). null sin récord. */
  fun recordLine(best: Int?, isNew: Boolean?): String? {
    if (best == null || best <= 0) return null
    return if (isNew == true) "¡Nuevo récord! $best cartas en un día perfecto" else "Tu mejor día perfecto: $best cartas"
  }

  /**
   * El consejo (con la marca «Truco: » que lee [ResultAdvice]): un truco de intención de implementación («cuando vea X, haré Y», lo que más transfiere a la vida diaria: Henry et al., 2021), según lo que más se escapó.
   * Primero lo cancelado que se hizo igual, luego las horas (con el reloj: mirarlo cuando se acerca la hora, Peper y Ball, 2023), después las cartas señal. null si todo salió bien o no hubo encargos.
   */
  fun tip(evHits: Int?, evTotal: Int?, timeHits: Int?, timeTotal: Int?, commissions: Int?, peeks: Int?, goodPeeks: Int?): String? {
    if ((commissions ?: 0) > 0) return "Truco: cuando la radio cancele algo, dilo en voz baja: «hoy no lo hago»."
    val timeMissed = (timeTotal ?: 0) > 0 && (timeHits ?: 0) < (timeTotal ?: 0)
    if (timeMissed) {
      val p = peeks ?: 0
      val g = goodPeeks ?: 0
      return if (p == 0 || g * 2 < p) "Truco: mira el reloj cuando se acerque la hora." else "Truco: imagínate haciendo el encargo."
    }
    val evMissed = (evTotal ?: 0) > 0 && (evHits ?: 0) < (evTotal ?: 0)
    if (evMissed) return "Truco: repite «cuando vea un lazo, caja fuerte»."
    return null
  }

  /** El récord que se guarda: el mayor de lo guardado y lo que trae la partida; nunca baja y nunca es negativo. */
  fun mergeRecord(saved: Int, fromGame: Int?): Int = maxOf(saved.coerceAtLeast(0), (fromGame ?: 0).coerceAtLeast(0))

  /**
   * Cuando se renovó el juego (8-oct) el vuelo pasó a ser «La estación de correo»: otra tarea, con otras etapas (cada una trae mecánicas nuevas: la hora, lo de todos los días, lo que cancela la radio, el lazo). El rating guardado (0..1)
   * de quienes ya jugaban el vuelo se lleva a la mitad (`ddaRating × 0,5`, la migración de Room 13 → 14) para que nadie parta en una etapa con mecánicas que aún no vio: el mejor jugador del vuelo (0,9) empieza en la etapa 5 y el que
   * estaba en el medio (0,5), en la 3. Sin dato (−1) no se toca.
   */
  fun translateOldRating(oldRating: Float): Float = if (oldRating < 0f) oldRating else (oldRating * OLD_TO_NEW_RATIO).coerceIn(0f, 1f)

  const val OLD_TO_NEW_RATIO = 0.5f

  /** Para lectores de pantalla. */
  fun spoken(ok: Int?, all: Int?): String = detailLine(ok, all)
}
