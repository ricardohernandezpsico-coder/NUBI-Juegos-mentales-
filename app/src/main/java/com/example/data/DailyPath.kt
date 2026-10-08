package com.example.data

import com.example.model.DomainType
import com.example.model.GameDefinition
import com.example.model.GameRegistry

/**
 * El camino de hoy (los 3 juegos de Hoy), elegido por AVANCE REAL y con variedad. Lógica pura, sin Android, con pruebas (`DailyPathTest`).
 *
 * Reglas (Etapa 0, 8-oct; antes se ordenaban las áreas por el promedio del puntaje —que el motor adaptativo deja cerca del 80 %, así que casi no distinguía— y el juego salía al azar):
 *  a) 3 juegos de 3 áreas distintas.
 *  b) Las áreas se ordenan por su avance ([Skill.progress] promedio de sus juegos con rating guardado; sin partidas, el del punto de partida): primero las METAS de la persona (la de menos avance primero), después las demás de menos a más.
 *  c) Rotación: el área que quedó fuera ayer entra hoy (ninguna falta dos días seguidos); queda fuera, entre las demás, la de menor prioridad.
 *  d) Dentro del área no se repite un juego de los caminos de ayer y anteayer si hay alternativa, y se prefiere el que hace más días que no se juega (uno que nunca se jugó cuenta como el más antiguo).
 *  e) Nunca un juego que la persona NUNCA jugó y no tiene tutorial ([tutorialGames]): cuando todos tengan tutorial, la regla queda inerte. Un área sin juegos elegibles se salta y entra la siguiente.
 *  f) Determinístico por fecha: con los mismos datos, el mismo día sale el mismo camino (el desempate usa la fecha, no el azar).
 */
object DailyPath {
  const val SIZE = 3

  /**
   * @param dateKey día del camino («yyyy-MM-dd»): semilla del desempate.
   * @param areaLevel avance 0..1 de cada área (lo calcula el repositorio: promedio de [Skill.progress] o el del punto de partida).
   * @param goals metas de la persona (mantienen su prioridad).
   * @param yesterday camino de ayer (ids) o null si no hubo; [dayBefore] el de anteayer.
   * @param lastPlayedDay por juego, el día (días desde 1970, hora local) de su última partida; sin entrada = nunca lo jugó.
   * @param today el día de hoy en esos mismos días.
   * @param tutorialGames juegos con tutorial (`UnityGameLauncher.TUTORIAL_GAMES`).
   */
  fun pick(
    dateKey: String,
    areaLevel: Map<DomainType, Float>,
    goals: Set<DomainType>,
    yesterday: List<String>?,
    dayBefore: List<String>?,
    lastPlayedDay: Map<String, Long>,
    today: Long,
    tutorialGames: Set<String>,
    games: List<GameDefinition> = GameRegistry.allGames
  ): List<String> {
    val order = rankDomainsForSession(goals, areaLevel)
    val eligible = { g: GameDefinition -> g.id in lastPlayedDay || g.id in tutorialGames }
    val byArea = order.associateWith { d -> games.filter { it.domain == d && eligible(it) } }
    val usable = order.filter { byArea.getValue(it).isNotEmpty() }

    // c) el área que quedó fuera ayer entra hoy
    val yesterdayAreas = yesterday.orEmpty().mapNotNull { id -> games.firstOrNull { it.id == id }?.domain }.toSet()
    val yesterdayOut = if (yesterdayAreas.size == SIZE) order.filter { it !in yesterdayAreas }.toSet() else emptySet()
    val chosenAreas = if (usable.size <= SIZE) usable else {
      val out = usable.lastOrNull { it !in yesterdayOut } ?: usable.last()      // queda fuera la de menor prioridad que no estuvo fuera ayer
      usable.filter { it != out }.take(SIZE)
    }

    // d) un juego por área: no el de ayer ni el de anteayer si hay otro, y el que hace más días que no se juega
    val recent = (yesterday.orEmpty() + dayBefore.orEmpty()).toSet()
    fun daysSince(id: String): Long = lastPlayedDay[id]?.let { (today - it).coerceAtLeast(0L) } ?: Long.MAX_VALUE
    fun best(candidates: List<GameDefinition>): GameDefinition {
      val fresh = candidates.filter { it.id !in recent }.ifEmpty { candidates }
      return fresh.sortedWith(compareByDescending<GameDefinition> { daysSince(it.id) }.thenBy { tieBreak(dateKey, it.id) }).first()
    }
    val picks = chosenAreas.map { best(byArea.getValue(it)) }.toMutableList()

    // Si faltan áreas con juegos elegibles (casi nunca), se completa con otros juegos elegibles y, solo como último recurso, con cualquiera.
    if (picks.size < SIZE) {
      val rest = order.flatMap { byArea.getValue(it) }.filter { g -> picks.none { it.id == g.id } }
      var pool = rest
      while (picks.size < SIZE && pool.isNotEmpty()) {
        val g = best(pool)
        picks += g
        pool = pool.filter { it.id != g.id }
      }
      var any = games.filter { g -> picks.none { it.id == g.id } }
      while (picks.size < SIZE && any.isNotEmpty()) {
        val g = best(any)
        picks += g
        any = any.filter { it.id != g.id }
      }
    }
    return picks.take(SIZE).map { it.id }
  }

  /** Desempate estable (no depende del azar ni de la corrida): la misma fecha y el mismo juego dan siempre el mismo número. */
  private fun tieBreak(dateKey: String, gameId: String): Int = (dateKey + "|" + gameId).hashCode()
}
