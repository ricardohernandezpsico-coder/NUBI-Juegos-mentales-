package com.example.data

import com.example.model.RankTier
import java.util.Locale

/**
 * Lo que sostiene las medallas de «Tú» y la fila de la liga (Tarea 125; `auditoria-fable/spec-avance-juegos.md` §1 y §2): textos y estados armados con lógica pura, con pruebas (`AvanceLogicTest`). Los puntos por área de Avance viven en `AreaPoints` (Tarea 159).
 */

/** Lo que cuenta la meta de un nivel de logro, con su unidad: «3 días», «25 partidas», «4 áreas», «9 juegos», «1 regreso». */
fun AchievementDef.goalText(): String {
  val n = number ?: 1
  val unit = when {
    id.startsWith("racha_") -> if (n == 1) "día" else "días"
    id == "primer_paso" || id.startsWith("partidas_") -> if (n == 1) "partida" else "partidas"
    id == "dia_completo" -> "partidas en un día"
    id == "dominios" -> "áreas"
    id.startsWith("explorador") -> "juegos"
    id.startsWith("vuelta_") -> if (n == 1) "regreso" else "regresos"
    else -> ""
  }
  return "$n $unit".trim()
}

/**
 * Una familia de medallas en un momento: el último nivel conseguido ([reached], null = ninguno todavía), el siguiente ([next], null = ya está en el más alto) y cuánto lleva hacia el siguiente ([progress]).
 * La medalla del kit muestra [number] (la meta del último nivel conseguido) sobre su placa; si no hay ninguno, va bloqueada (plasticina cruda) con «x/y» debajo.
 */
data class FamilyMedalState(val family: AchievementFamily, val reached: AchievementDef?, val next: AchievementDef?, val progress: Pair<Int, Int>?) {
  val unlocked: Boolean get() = reached != null
  val number: Int? get() = reached?.let { it.number ?: 1 }

  /** Cuántos niveles tiene la familia (Constancia 8, Variedad 4, Volver 3). */
  val totalLevels: Int get() = Achievements.levelsOf(family).size

  /** El nivel en que está: 0 si todavía no consiguió ninguno; si no, su lugar en la familia (1 = el primero). */
  val level: Int get() = reached?.let { r -> Achievements.levelsOf(family).indexOfFirst { it.id == r.id } + 1 } ?: 0

  /** Lo que falta, dicho como lo diría una persona: «14 días», «25 partidas», «volver 1 vez» (Volver). */
  private fun ask(def: AchievementDef): String =
    if (family == AchievementFamily.VOLVER) { val n = def.number ?: 1; "volver $n ${if (n == 1) "vez" else "veces"}" } else def.goalText()

  /**
   * Bajo el título de la familia, UN solo formato (Tarea 126, a pedido de Opus; sin la palabra «Nivel» desde la Tarea 150, B1.11: la app dice «etapa»): «2 de 8 · siguiente: 7 días», «0 de 3 · siguiente: volver 1 vez» y, en el último, «8 de 8 · completa». Se entiende solo, sin «0/1».
   */
  val caption: String
    get() = "$level de $totalLevels · " + (next?.let { "siguiente: ${ask(it)}" } ?: "completa")

  /** El texto de [caption] en dos líneas para la casilla de la medalla: «2 de 8» y «siguiente: 7 días». */
  val captionLines: List<String> get() = caption.split(" · ")

  /** Lo que lee el lector de pantalla. */
  val spoken: String
    get() = if (reached == null) "${family.title}, bloqueada. $caption" else "${family.title}, ${reached.goalText()}. $caption"
}

fun Achievements.familyState(family: AchievementFamily, unlocked: Set<String>, stats: AchievementStats): FamilyMedalState {
  val next = next(family, unlocked)
  return FamilyMedalState(family, reached(family, unlocked), next, next?.progress?.invoke(stats))
}

/** Los números con punto de miles, como se lee en español («1.240»). */
fun thousands(n: Int): String = String.format(Locale.GERMANY, "%,d", n)

/** La fila de la liga de Avance (§1.5): «Liga Plata · 1.240 trofeos» y, si hay siguiente liga y ya hay trofeos, «260 para Oro». */
data class LeagueRow(val tier: RankTier, val title: String, val toNext: String?)

fun leagueRow(avg: Int): LeagueRow {
  val tier = RankTier.fromRating(avg)
  val next = RankTier.entries.getOrNull(tier.ordinal + 1)
  return LeagueRow(
    tier, "Liga ${tier.tierName} · ${thousands(avg)} ${if (avg == 1) "trofeo" else "trofeos"}",
    if (next == null || avg <= 0) null else "${thousands((next.minRating - avg).coerceAtLeast(0))} para ${next.tierName}"
  )
}

