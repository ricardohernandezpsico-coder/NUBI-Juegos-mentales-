package com.example.data

import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.RankTier

/** Dibujo central de la medalla de un logro (ver `ui/components/AchievementMedal.kt`). */
enum class AchievementGlyph { PLAY, FLAME, CALENDAR, COMPASS, HEXAGON, LUCERO, BOLT, CHECK, SHIELD, RETURN }

/**
 * Las tres familias de logros por CONDUCTA (Tarea 124; Fable, `auditoria-fable/spec-avance-juegos.md` §1.10): lo que hace la persona, nunca el puntaje. Cada una tiene niveles; la medalla de la familia muestra el más alto conseguido.
 */
enum class AchievementFamily(val title: String, val blurb: String) {
  CONSTANCIA("Constancia", "Juega seguido"),
  VARIEDAD("Variedad", "Juega en todas las áreas"),
  VOLVER("Volver", "Vuelve a jugar después de unos días sin Nubi")
}

/**
 * Logro: [check] dice si está conseguido con las [AchievementStats] actuales; [progress] da (actual, meta) para
 * mostrar "4/7" mientras está bloqueado. [number] se pinta en la medalla (7 días, 100 partidas...). [tier] solo
 * en los logros de liga (la medalla lleva el escudo de esa liga).
 *
 * [family] y [level] = a qué familia pertenece y su lugar en ella (0 = el primero); `null` en los [Achievements.legacy] (los de desempeño, que ya no se otorgan). [legacy] = «Medallas anteriores».
 */
data class AchievementDef(
  val id: String,
  val title: String,
  val description: String,
  val glyph: AchievementGlyph,
  val number: Int? = null,
  val tier: RankTier? = null,
  val family: AchievementFamily? = null,
  val level: Int = 0,
  val legacy: Boolean = false,
  val check: (AchievementStats) -> Boolean,
  val progress: (AchievementStats) -> Pair<Int, Int>
)

/** Todo lo que miran los logros, calculado desde el historial y los trofeos (ver [computeAchievementStats]). */
data class AchievementStats(
  val totalGames: Int,
  val bestStreak: Int,
  val maxGamesInOneDay: Int,
  val distinctGames: Int,
  val distinctDomains: Int,
  val bestScore: Int,
  val timedGames: Int,
  val bestGameRating: Int,
  val globalRating: Int,
  /** Juegos distintos de los que la app ofrece hoy (sin los retirados): `explorador_17`. */
  val distinctActiveGames: Int = 0,
  /** Cuántas veces volvió a jugar tras 3 o más días sin partida (días con partida cuyo día jugado anterior queda a 4 o más días): los logros de «Volver». */
  val returns: Int = 0
)

/** Racha más larga de días con partida (días locales), con la regla que perdona (`Streak`): un día de descanso por cada 7 no la corta. Siempre ≥ la estricta. */
fun longestStreak(days: Collection<Long>): Int = Streak.longest(days)

/** Cuántas veces volvió a jugar tras una pausa: los días con partida cuyo día jugado anterior queda a [minGapDays] días o más (4 = tres o más días sin partida entre uno y otro). */
fun countReturns(days: Collection<Long>, minGapDays: Int = 4): Int {
  val sorted = days.toSortedSet().toList()
  var n = 0
  for (i in 1 until sorted.size) if (sorted[i] - sorted[i - 1] >= minGapDays) n++
  return n
}

fun computeAchievementStats(history: List<GamePlayResult>, ratings: Map<String, Int>): AchievementStats {
  val days = history.map { Streak.dayOf(it.timestamp) }
  val defs = history.mapNotNull { GameRegistry.getById(it.gameId) }
  return AchievementStats(
    totalGames = history.size,
    bestStreak = longestStreak(days),
    maxGamesInOneDay = days.groupingBy { it }.eachCount().values.maxOrNull() ?: 0,
    distinctGames = defs.map { it.id }.toSet().size,
    distinctDomains = defs.map { it.domain }.toSet().size,
    bestScore = history.maxOfOrNull { it.score } ?: 0,
    timedGames = history.count { it.timed },
    bestGameRating = ratings.filterKeys { !GameRegistry.isRetired(it) }.values.maxOrNull() ?: 0,
    globalRating = GameRegistry.allGames.sumOf { ratings[it.id] ?: 0 } / GameRegistry.allGames.size,
    distinctActiveGames = defs.filter { !GameRegistry.isRetired(it.id) }.map { it.id }.toSet().size,
    returns = countReturns(days)
  )
}

/** Catálogo de logros. Los de [all] son los que se otorgan (en el orden del Perfil: por familia y nivel); los de [legacy] ya no se otorgan pero se conservan. */
object Achievements {
  private fun streak(id: String, title: String, days: Int, level: Int) = AchievementDef(
    id, title, "Juega $days días seguidos", AchievementGlyph.FLAME, number = days, family = AchievementFamily.CONSTANCIA, level = level,
    check = { it.bestStreak >= days }, progress = { it.bestStreak.coerceAtMost(days) to days }
  )

  private fun volume(id: String, title: String, games: Int, level: Int) = AchievementDef(
    id, title, "Juega $games partidas", AchievementGlyph.CHECK, number = games, family = AchievementFamily.CONSTANCIA, level = level,
    check = { it.totalGames >= games }, progress = { it.totalGames.coerceAtMost(games) to games }
  )

  private fun comeBack(id: String, title: String, times: Int, level: Int) = AchievementDef(
    id, title, if (times == 1) "Vuelve a jugar después de unos días sin Nubi" else "Vuelve a jugar $times veces después de unos días sin Nubi",
    AchievementGlyph.RETURN, number = times, family = AchievementFamily.VOLVER, level = level,
    check = { it.returns >= times }, progress = { it.returns.coerceAtMost(times) to times }
  )

  private fun gameTier(id: String, tier: RankTier) = AchievementDef(
    id, tier.tierName, "Llega a liga ${tier.tierName} en un juego", AchievementGlyph.SHIELD, tier = tier, legacy = true,
    check = { it.bestGameRating >= tier.minRating },
    progress = { it.bestGameRating.coerceAtMost(tier.minRating) to tier.minRating }
  )

  /**
   * Los logros que se otorgan. **Constancia** (la farola; el orden de sus niveles es una DECISIÓN por dificultad, porque mezcla días seguidos y partidas): primer paso, 3 días, 7 días, 25 partidas, 14 días, 30 días, 100 partidas, 100 días.
   * **Variedad** (los cuatro lugares): un día completo, las 4 áreas, 9 juegos, los 17. **Volver** (la nave de regreso): 1, 3 y 10 veces.
   */
  val all: List<AchievementDef> = listOf(
    AchievementDef(
      "primer_paso", "Primer paso", "Juega tu primera partida", AchievementGlyph.PLAY, family = AchievementFamily.CONSTANCIA, level = 0,
      check = { it.totalGames >= 1 }, progress = { it.totalGames.coerceAtMost(1) to 1 }
    ),
    streak("racha_3", "En marcha", 3, 1),
    streak("racha_7", "Una semana", 7, 2),
    volume("partidas_25", "Constante", 25, 3),
    streak("racha_14", "Dos semanas", 14, 4),
    streak("racha_30", "Un mes entero", 30, 5),
    volume("partidas_100", "Centenario", 100, 6),
    streak("racha_100", "Imparable", 100, 7),
    AchievementDef(
      "dia_completo", "Día completo", "Juega 3 partidas en un mismo día", AchievementGlyph.CALENDAR, number = 3, family = AchievementFamily.VARIEDAD, level = 1,
      check = { it.maxGamesInOneDay >= 3 }, progress = { it.maxGamesInOneDay.coerceAtMost(3) to 3 }
    ),
    AchievementDef(
      "dominios", "Mente completa", "Juega en las 4 áreas", AchievementGlyph.HEXAGON, number = 4, family = AchievementFamily.VARIEDAD, level = 2,
      check = { it.distinctDomains >= 4 }, progress = { it.distinctDomains.coerceAtMost(4) to 4 }
    ),
    AchievementDef(
      "explorador", "Explorador", "Juega 9 juegos distintos", AchievementGlyph.COMPASS, number = 9, family = AchievementFamily.VARIEDAD, level = 3,
      check = { it.distinctGames >= 9 }, progress = { it.distinctGames.coerceAtMost(9) to 9 }
    ),
    AchievementDef(
      "explorador_17", "Explorador total", "Juega los 17 juegos", AchievementGlyph.COMPASS, number = 17, family = AchievementFamily.VARIEDAD, level = 4,
      check = { it.distinctActiveGames >= 17 }, progress = { it.distinctActiveGames.coerceAtMost(17) to 17 }
    ),
    comeBack("vuelta_1", "Volviste", 1, 1),
    comeBack("vuelta_3", "Siempre vuelves", 3, 2),
    comeBack("vuelta_10", "Vuelves una y otra vez", 10, 3)
  )

  /** Los logros de desempeño (puntaje, modo Reto y ligas): ya NO se otorgan, pero las medallas ya ganadas se conservan y van al Perfil en «Medallas anteriores». */
  val legacy: List<AchievementDef> = listOf(
    AchievementDef(
      "brillante", "Brillante", "Consigue 90 puntos o más en una partida", AchievementGlyph.LUCERO, number = 90, legacy = true,
      check = { it.bestScore >= 90 }, progress = { it.bestScore.coerceAtMost(90) to 90 }
    ),
    AchievementDef(
      "perfecto", "Perfecto", "Consigue 100 puntos en una partida", AchievementGlyph.LUCERO, number = 100, legacy = true,
      check = { it.bestScore >= 100 }, progress = { it.bestScore.coerceAtMost(100) to 100 }
    ),
    AchievementDef(
      "reto_10", "Contrarreloj", "Termina 10 partidas en modo Reto", AchievementGlyph.BOLT, number = 10, legacy = true,
      check = { it.timedGames >= 10 }, progress = { it.timedGames.coerceAtMost(10) to 10 }
    ),
    gameTier("liga_plata", RankTier.PLATA),
    gameTier("liga_oro", RankTier.ORO),
    gameTier("liga_platino", RankTier.PLATINO),
    AchievementDef(
      "general_oro", "Oro general", "Tu liga general llega a Oro", AchievementGlyph.SHIELD, tier = RankTier.ORO, legacy = true,
      check = { it.globalRating >= RankTier.ORO.minRating },
      progress = { it.globalRating.coerceAtMost(RankTier.ORO.minRating) to RankTier.ORO.minRating }
    )
  )

  /** Un logro por su id: también los de [legacy] (las medallas ya ganadas siguen viéndose). */
  fun byId(id: String): AchievementDef? = all.firstOrNull { it.id == id } ?: legacy.firstOrNull { it.id == id }

  /** Ids de los logros que se OTORGAN con estas cifras (nunca los de [legacy]). */
  fun unlocked(stats: AchievementStats): Set<String> = all.filter { it.check(stats) }.map { it.id }.toSet()

  /** Los niveles de una familia, de menor a mayor. */
  fun levelsOf(family: AchievementFamily): List<AchievementDef> = all.filter { it.family == family }.sortedBy { it.level }

  /** El nivel más alto conseguido de una familia (null si no hay ninguno) con los [unlocked] conseguidos. */
  fun reached(family: AchievementFamily, unlocked: Set<String>): AchievementDef? = levelsOf(family).lastOrNull { it.id in unlocked }

  /** El siguiente nivel de una familia que falta conseguir (null si ya están todos). */
  fun next(family: AchievementFamily, unlocked: Set<String>): AchievementDef? = levelsOf(family).firstOrNull { it.id !in unlocked }

  /** Las medallas anteriores que la persona ganó (en el orden del catálogo): lo que muestra el Perfil en «Medallas anteriores». */
  fun legacyEarned(unlocked: Set<String>): List<AchievementDef> = legacy.filter { it.id in unlocked }
}

/** Logros ya conseguidos y cuándo: una línea `id|timestamp`. */
fun encodeUnlocks(unlocks: Map<String, Long>): String = unlocks.entries.joinToString("\n") { "${it.key}|${it.value}" }

fun decodeUnlocks(text: String?): Map<String, Long> =
  text.orEmpty().lines().mapNotNull { line ->
    val parts = line.split('|')
    if (parts.size != 2) return@mapNotNull null
    val ts = parts[1].toLongOrNull() ?: return@mapNotNull null
    parts[0] to ts
  }.toMap()
