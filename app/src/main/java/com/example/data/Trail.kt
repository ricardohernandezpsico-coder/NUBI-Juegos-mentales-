package com.example.data

import kotlin.math.roundToInt

/**
 * La medida de «Rastro de luz» (id «secuencia», juego estrella de Memoria; ver docs/diseno-rastro-de-luz.md §6 y docs/medidas-juegos-estrella.md):
 * Unity manda, por familia (0 el rastro, 1 al revés, 2 el cielo gira, 3 en marcha), el mejor largo repetido bien, las rondas y los aciertos; aquí se
 * convierten en lo que la persona lee. Lógica pura con pruebas.
 *
 * Qué se mide de verdad: cuántas luces SEGUIDAS se repiten bien en el rastro simple (una amplitud visoespacial, como el bloque de Corsi) y cómo le
 * va a la persona en cada giro. Una partida son pocas rondas: cada modo solo se muestra con [MIN_MODE_ROUNDS] o más, y el que menos aciertos tuvo se
 * marca con TEXTO («el que más te costó»), nunca solo con color. Sin percentiles ni comparaciones.
 */
object Trail {
  const val MIN_MODE_ROUNDS = 3
  const val SIMPLE = 0

  /** Nombres de las 4 familias, en el orden de la telemetría. */
  val MODE_NAMES = listOf("El rastro", "Al revés", "El cielo gira", "En marcha")

  /** Los tres modos que se comparan entre sí (el rastro simple es la cifra grande). */
  private val OTHER_MODES = listOf(1, 2, 3)

  // ------------------------------------------------------------------ la cifra grande

  /** Las luces más largas que se repitieron bien en el rastro simple; null si no se repitió ninguna. */
  fun bestTrail(bestLen: List<Int>?): Int? = bestLen?.getOrNull(SIMPLE)?.takeIf { it > 0 }

  /** La marca para ver su evolución (más alto = mejor): el mismo número de [bestTrail]. null sin medida. */
  fun mark(bestLen: List<Int>?): Float? = bestTrail(bestLen)?.toFloat()

  /** «5 luces»; null si no hay medida. */
  fun lights(n: Int?): String? = n?.let { if (it == 1) "1 luz" else "$it luces" }

  /** Qué se dice cuando no hubo rastro simple repetido entero: sin culpa y con un consejo. */
  fun noTrailLine(rounds: List<Int>?): String =
    if (rounds?.getOrNull(SIMPLE)?.let { it > 0 } == true)
      "Esta vez no salió un rastro completo. Mira el camino con calma y toca una luz a la vez: así se arma."
    else "Esta partida no tuvo rastro simple: juega otra para medir tu rastro."

  // ------------------------------------------------------------------ por modo

  /** Un modo con al menos [MIN_MODE_ROUNDS] rondas: rondas, aciertos y si fue el de menor tasa (se marca con TEXTO). */
  data class ModeRow(val mode: Int, val label: String, val rounds: Int, val hits: Int, val lowest: Boolean) {
    val percent: Int get() = if (rounds <= 0) 0 else (100f * hits / rounds).roundToInt()
  }

  /**
   * Al revés, el cielo gira y en marcha, cada uno solo si tuvo [MIN_MODE_ROUNDS] rondas o más, con el de menor tasa marcado (solo si hay dos o más
   * modos y no empatan todos). Vacía si no hay datos o no llegan a 4 familias.
   */
  fun modeRows(rounds: List<Int>?, hits: List<Int>?): List<ModeRow> {
    if (rounds == null || hits == null || rounds.size != 4 || hits.size != 4) return emptyList()
    val valid = OTHER_MODES.filter { rounds[it] >= MIN_MODE_ROUNDS }
    if (valid.isEmpty()) return emptyList()
    val rate = valid.associateWith { hits[it].coerceIn(0, rounds[it]).toFloat() / rounds[it] }
    val low = rate.values.minOrNull() ?: 0f
    val high = rate.values.maxOrNull() ?: 0f
    val markLowest = valid.size >= 2 && high - low > 1e-6f
    val lowest = if (markLowest) valid.minByOrNull { rate.getValue(it) } else null
    return valid.map { ModeRow(it, MODE_NAMES[it], rounds[it], hits[it].coerceIn(0, rounds[it]), it == lowest) }
  }

  // ------------------------------------------------------------------ lectura en palabras (Nubi científica)

  /** Qué es la memoria de trabajo, para quien lo lee por primera vez. */
  const val WORKING_MEMORY =
    "Tu rastro mide tu memoria de trabajo: lo que usas para sostener unas pocas cosas en la cabeza mientras las necesitas, como un número de teléfono hasta marcarlo."

  /** Una línea por modo (el que tuvo rondas suficientes), sin números: qué pide ese modo. */
  fun modeLine(mode: Int): String? = when (mode) {
    1 -> "Al revés pide reordenar en la cabeza: suele costar un poco más."
    2 -> "Con el giro hay que recordar el orden y no el lugar: tus luceros se mueven."
    3 -> "En marcha pide actualizar: guardas lo último y sueltas lo anterior."
    else -> null
  }

  /** Un truco concreto para el modo que más costó (solo para ese). */
  fun trick(mode: Int): String? = when (mode) {
    1 -> "Truco: al mirar, piensa en el camino como una melodía y recita su final primero."
    2 -> "Truco: sigue a un lucero por su color mientras el cielo gira; el orden va con los luceros, no con el lugar."
    3 -> "Truco: no esperes el final: ve guardando solo las últimas luces y suelta las primeras."
    else -> null
  }

  /** El mensaje de modos nuevos de la partida («Desbloqueaste: Al revés»); null si no hubo. [bits]: 1 rastro, 2 al revés, 4 gira, 8 en marcha. */
  fun unlockedLine(bits: Int?): String? {
    val names = (0 until 4).filter { it != SIMPLE && ((bits ?: 0) shr it) and 1 == 1 }.map { MODE_NAMES[it] }
    if (names.isEmpty()) return null
    return if (names.size == 1) "Desbloqueaste un modo nuevo: ${names[0]}" else "Desbloqueaste modos nuevos: ${names.joinToString(", ")}"
  }

  /** Para lectores de pantalla. */
  fun spoken(bestLen: List<Int>?): String = bestTrail(bestLen)?.let { "Tu rastro: ${lights(it)}" } ?: "Sin rastro completo en esta partida"
}
