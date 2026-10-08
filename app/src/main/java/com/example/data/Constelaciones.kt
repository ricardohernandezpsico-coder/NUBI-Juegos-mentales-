package com.example.data

/**
 * «Constelaciones» (juego de Memoria, id `parejas` desde la renovación del 7-oct; ver docs/diseno-constelaciones.md): la lectura de lo que manda Unity para la pantalla final y el récord que se guarda. Lógica pura con pruebas.
 *
 * Cada luz del cielo esconde un objeto del espacio; las parejas (o tríos) iguales se unen con una línea: DORADA si fuiste directo a una compañera YA VISTA (de memoria) o CELESTE punteada si la luz era nueva (a la primera
 * vista: suerte). La medida es la «memoria de lugar»: de las veces que había una compañera ya vista, cuántas fuiste directo a ella (aciertos de memoria / oportunidades). Lo encontrado por suerte NO cuenta. Una partida sin
 * oportunidades no tiene medida («—») y no se guarda. La racha de memoria más larga y las parejas de memoria la acompañan; el récord es la «mejor racha» (preferencias `constelaciones_record`, que SÍ van en el respaldo, y se manda a
 * Unity en cada partida). No se habla de enfermedades ni se promete nada de salud: va la nota común «No es un diagnóstico».
 */
object Constelaciones {
  /** Oportunidades mínimas para mostrar la medida en la pantalla final («no sacar conclusiones de pocos casos»). */
  const val MIN_OPPS_FOR_MEASURE = 1
  /** Los 6 grupos de etapas de la pantalla final (3 etapas por grupo): parejas, cielo más lleno, gemelos, tríos, parejas y tríos juntos, cielo grande. */
  const val GROUPS = 6

  private val GROUP_NAMES = listOf("parejas", "cielo más lleno", "gemelos", "tríos", "parejas y tríos juntos", "cielo grande")

  /** «Memoria de lugar»: el porcentaje de aciertos de memoria sobre las oportunidades (0..100). null sin oportunidades. */
  fun percent(hits: Int?, opps: Int?): Int? {
    if (hits == null || opps == null || opps < MIN_OPPS_FOR_MEASURE) return null
    return Math.round(100f * hits.coerceIn(0, opps) / opps)
  }

  /** La marca para ver su evolución: % de memoria de lugar (más alto = mejor). null sin oportunidades: esa partida no se guarda como medida. */
  fun mark(hits: Int?, opps: Int?): Float? {
    if (hits == null || opps == null || opps < MIN_OPPS_FOR_MEASURE) return null
    return 100f * hits.coerceIn(0, opps) / opps
  }

  /** «Memoria de lugar: 70 %» o «—» si no hubo oportunidades. */
  fun headline(hits: Int?, opps: Int?): String = percent(hits, opps)?.let { "Memoria de lugar: $it %" } ?: "Memoria de lugar: —"

  /** «7 de 10 veces fuiste directo a una pareja que ya habías visto»; sin oportunidades, una frase que lo dice sin culpa. */
  fun detailLine(hits: Int?, opps: Int?): String =
    if (hits == null || opps == null || opps < MIN_OPPS_FOR_MEASURE) "No hubo parejas vistas que recordar"
    else "${hits.coerceIn(0, opps)} de $opps veces fuiste directo a una pareja que ya habías visto"

  /** «Parejas de memoria: 4 de 6» (los grupos con al menos una línea dorada sobre los encontrados); null si no hay datos. */
  fun memoryGroupsLine(memGroups: Int?, groups: Int?): String? {
    if (memGroups == null || groups == null || groups <= 0) return null
    return "Parejas de memoria: ${memGroups.coerceIn(0, groups)} de $groups"
  }

  /** «Racha de memoria más larga: 5»; null sin racha. */
  fun streakLine(streak: Int?): String? = if (streak == null || streak <= 0) null else "Racha de memoria más larga: $streak"

  /** «Etapa más alta: 4 de 6 (tríos)»; null si no es un grupo válido (1..6). */
  fun groupLine(group: Int?): String? {
    if (group == null || group !in 1..GROUPS) return null
    return "Etapa más alta: $group de $GROUPS (${GROUP_NAMES[group - 1]})"
  }

  /** «Tu mejor racha: 6»; con «¡Nueva mejor racha!» solo cuando en esta partida se superó la guardada (lo dice Unity: igualarla no cuenta). null sin récord. */
  fun recordLine(best: Int?, isNew: Boolean?): String? {
    if (best == null || best <= 0) return null
    return if (isNew == true) "¡Nueva mejor racha! $best" else "Tu mejor racha: $best"
  }

  /** La nota que explica por qué la suerte no suma. */
  const val LUCK_NOTE = "Lo encontrado por suerte no cuenta."

  /** El consejo (con la marca «Truco: » que lee [ResultAdvice]): cuando alguna vez se te escapó una compañera ya vista. null si todas fueron de memoria o no hubo oportunidades. */
  fun tip(hits: Int?, opps: Int?): String? {
    if (hits == null || opps == null || opps <= 0 || hits >= opps) return null
    return "Truco: da vuelta primero una luz nueva."
  }

  /** El récord que se guarda: el mayor de lo guardado y lo que trae la partida; nunca baja y nunca es negativo. */
  fun mergeRecord(saved: Int, fromGame: Int?): Int = maxOf(saved.coerceAtLeast(0), (fromGame ?: 0).coerceAtLeast(0))

  /**
   * Cuando se renovó el juego (7-oct) la escalera pasó de 10 a 18 etapas, con otras etapas: el rating guardado (0..1) de quienes ya jugaban Parejas Ocultas se lleva a la escala nueva para que partan en una etapa
   * EQUIVALENTE y no en la del mismo «lugar» de la escalera (un 0,9 de la vieja, 12 parejas, caería en el cielo grande de la nueva). Un nivel viejo L (1 a 10) corresponde a la etapa 1 + (L − 1) · 8/9 de la nueva
   * (de 1 a 9: parejas, cielo más lleno y gemelos), y el rating normalizado de cada una es (nivel − 1) / niveles: 0,9 de la vieja (nivel 10 → etapa 9) → 8/18 ≈ 0,44 de la nueva.
   * La misma cuenta, con enteros, está en la migración de Room 12 → 13 (`NeuroVidaDatabase.MIGRATIONS`): ratio = 40/81.
   */
  fun translateOldRating(oldRating: Float): Float = if (oldRating < 0f) oldRating else (oldRating * OLD_TO_NEW_RATIO).coerceIn(0f, 1f)

  const val OLD_TO_NEW_RATIO = 40f / 81f

  /** Para lectores de pantalla. */
  fun spoken(hits: Int?, opps: Int?): String = detailLine(hits, opps)
}
