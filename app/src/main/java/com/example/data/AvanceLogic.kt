package com.example.data

import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.RankTier
import java.util.Locale
import kotlin.math.roundToInt

/**
 * Lo que sostiene las pantallas de Avance y Juegos (Tarea 125; `auditoria-fable/spec-avance-juegos.md` §1 y §2): textos y estados armados con lógica pura, con pruebas (`AvanceLogicTest`).
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

private val SHORT_DAYS = arrayOf("Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb")

/** «Lun 14 · Rescate relámpago · a tu medida» (con reloj: «con reloj»); si el juego tiene medida y hay un punto con la misma marca de tiempo, se agrega su valor en palabras: «· 4 de 4 cápsulas». Sin puntos («pts»). */
fun historyLine(r: GamePlayResult, measures: List<MeasurePoint>, title: String): String {
  val cal = java.util.Calendar.getInstance().apply { timeInMillis = r.timestamp }
  val day = "${SHORT_DAYS[cal.get(java.util.Calendar.DAY_OF_WEEK) - 1]} ${cal.get(java.util.Calendar.DAY_OF_MONTH)}"
  val def = StarMeasures.defForGame(r.gameId)
  val point = def?.let { d -> measures.firstOrNull { it.key == d.key && it.timestamp == r.timestamp } }
  val value = if (def != null && point != null) when (def.key) {
    "stroop" -> DosOrillas.cost(point.value.roundToInt())                    // en palabras, como en su final
    Brake.KEY -> StarMeasures.zoneText(def, measures)                        // Freno: la zona, nunca milisegundos
    else -> measureText(def, point.value)
  } else null
  return listOfNotNull(day, title, if (r.timed) "con reloj" else "a tu medida", value).joinToString(" · ")
}

/** El estado de «Tu semana» (§1.1): los textos y si hay algo que celebrar o invitar. */
data class WeekSummary(val big: String, val rest: String, val streakPill: String?, val areasLine: String, val empty: Boolean)

fun weekSummary(lamps: Week.Lamps, goal: Int, streak: Int, areas: Int, noGamesAtAll: Boolean): WeekSummary {
  if (noGamesAtAll) return WeekSummary("Tu semana empieza hoy", "", null, "Juega tu ruta de hoy y se enciende la primera farola.", true)
  val (big, rest) = Week.captionParts(lamps.daysPlayed, goal)
  return WeekSummary(big, rest, if (streak > 0) "Racha: ${if (streak == 1) "1 día" else "$streak días"}" else null, "Áreas esta semana: $areas de ${com.example.model.DomainType.values().size}", false)
}

/**
 * Lo que Nubi comenta bajo «Tu semana» (Tarea 131, A6; `auditoria-fable/spec-adopcion-A1-A7.md`): UNA frase, la primera que se cumple de estas seis (en este orden de prioridad), siempre en positivo: nunca «no jugaste», «perdiste» ni «te atrasaste»
 * (`NoLossGuard` lo vigila en las pruebas).
 *  1. sin partidas: invita a la primera ruta;
 *  2. día perdonado esta semana (`streak.forgivenDay`): lo dice como constancia, con el día en español;
 *  3. meta cumplida (`daysPlayed >= goal`): lo de más es regalo;
 *  4. las 4 áreas: lo celebra;
 *  5. falta 1 día: «Cualquier ruta cuenta»;
 *  6. el resto: cuánto lleva de la meta y que un rato corto suma.
 * Sin meta (`goal <= 0`) no hay lo de la meta: cae en cuántos días lleva.
 */
fun nubiWeekLine(lamps: Week.Lamps, goal: Int, streak: Streak.StreakInfo, areas: Int, noGamesAtAll: Boolean): String {
  val played = lamps.daysPlayed
  return when {
    noGamesAtAll -> "Cuando juegues tu primera ruta, aquí te cuento cómo va tu semana."
    streak.forgivenDay != null -> Streak.forgivenLine(streak)!!                      // una sola frase del descanso en toda la app: la de Hoy (Tarea 150, B1.8)
    goal > 0 && played >= goal -> "Meta cumplida esta semana. Lo que juegues de más es regalo."
    areas >= com.example.model.DomainType.values().size -> "Esta semana pasaste por las ${com.example.model.DomainType.values().size} áreas."
    goal > 0 && goal - played == 1 -> "Te falta 1 día para tu meta. Cualquier ruta cuenta."
    goal > 0 -> "Llevas $played de $goal. Un rato corto hoy también suma."
    else -> "Llevas $played ${if (played == 1) "día" else "días"} esta semana. Un rato corto hoy también suma."
  }
}

/** El nombre del área de un juego (null si es uno retirado): para contar las áreas de la semana. */
fun areaNameOf(gameId: String): String? = GameRegistry.getById(gameId)?.domain?.name
