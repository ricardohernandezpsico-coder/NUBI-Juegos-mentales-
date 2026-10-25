package com.example.data

import android.content.Context
import com.example.model.DomainType
import com.example.model.GameDefinition
import com.example.model.GameRegistry

/** Cómo va el Desafío de hoy: todavía no se jugó, se jugó sin superarlo (se puede repetir) o ya se superó (el día está hecho). */
enum class ChallengeState { PENDIENTE, JUGADO, SUPERADO }

/** El Desafío de hoy: el juego que Nubi eligió para [day] («yyyy-MM-dd») y cómo va. */
data class ChallengeToday(val day: String, val gameId: String, val state: ChallengeState = ChallengeState.PENDIENTE) {
  /** «2026-10-25|stroop|PENDIENTE»: la forma en que se guarda (preferencia pasajera `desafio_hoy`). */
  fun encode(): String = "$day|$gameId|${state.name}"

  companion object {
    fun decode(text: String?): ChallengeToday? {
      val p = text?.split('|') ?: return null
      if (p.size != 3) return null
      val state = ChallengeState.values().firstOrNull { it.name == p[2] } ?: return null
      return ChallengeToday(p[0], p[1], state)
    }
  }
}

/** Lo que se recuerda del Desafío: el de hoy y, por juego, el último día (días desde 1970, hora local) en que fue el Desafío de hoy. */
data class ChallengeLog(val today: ChallengeToday? = null, val lastDay: Map<String, Long> = emptyMap())

/** Dónde se guarda lo del Desafío de hoy. */
interface ChallengeStore {
  fun load(): ChallengeLog
  fun save(log: ChallengeLog)
}

/** En preferencias propias y PASAJERAS (`desafio_hoy`: no entra en `backup_rules.xml`; `BackupRulesTest` lo lista como pasajero): el modo de cada partida no se guarda en la base, así que el día se recuerda aparte; si se pierde, Nubi elige de nuevo. */
class PrefsChallengeStore(context: Context) : ChallengeStore {
  private val prefs = context.applicationContext.getSharedPreferences("desafio_hoy", Context.MODE_PRIVATE)

  override fun load(): ChallengeLog = ChallengeLog(
    today = ChallengeToday.decode(prefs.getString("hoy", null)),
    lastDay = prefs.getStringSet("ultimos", emptySet()).orEmpty().mapNotNull { e -> e.split('=').takeIf { it.size == 2 }?.let { p -> p[1].toLongOrNull()?.let { p[0] to it } } }.toMap()
  )

  override fun save(log: ChallengeLog) {
    prefs.edit().putString("hoy", log.today?.encode()).putStringSet("ultimos", log.lastDay.map { (g, d) -> "$g=$d" }.toSet()).apply()
  }

  companion object { const val FILE = "desafio_hoy" }
}

/**
 * El Desafío de hoy (Tarea 167, 25-oct; `auditoria-fable/hoy-desafio-y-dia-completo.md`, opción 1 «Dos botones, un solo sol a la vez», elegida por Ricardo): UN juego al día, elegido por Nubi con reglas fijas, que se juega en el modo Desafío que ya existe
 * (`PlayMode.DESAFIO`: «un paso más difícil»; si se supera, el avance sube; si no, nunca baja) en una partida de unos 2 minutos. Lógica pura, sin Android más que el almacén, con pruebas (`DailyChallengeTest`).
 *
 * Las seis reglas de la elección ([pick]):
 *  1. Solo juegos ya MEDIDOS ([SkillState.measured]): sin una partida que cuente no hay nivel y «un paso más difícil» no significa nada. Si no hay ninguno, no hay Desafío y Hoy queda sin la losa.
 *  2. NUNCA un juego de la ruta de hoy.
 *  3. Primero el ÁREA que la ruta dejó fuera (la ruta cubre 3 de las 4 áreas): así el día toca las cuatro.
 *  4. Dentro de ese grupo, el que lleva MÁS DÍAS sin ser el Desafío de hoy (el que nunca lo fue, primero); el desempate usa la fecha, no el azar.
 *  5. NO se repite el de ayer.
 *  6. El reloj, como lo tenga elegido la persona en la ficha de ese juego (`RetoChoice`): lo decide quien lo lanza, no [pick].
 *
 * Es gratis y UNO al día, y se puede repetir el mismo día las veces que se quiera si no se superó. **El cobro de Nubi Plus NO se construye**; cuando exista el tope de partidas libres, el Desafío de hoy NO cuenta para él ([COUNTS_FOR_FREE_CAP]) y Plus será el Desafío y
 * Al límite en cualquier juego y a cualquier hora (`docs/modelo-negocio.md`). No compara con otras personas (`Features.COMPARE` sigue apagado).
 */
object DailyChallenge {
  /** El Desafío de hoy NO cuenta para el futuro tope de partidas libres del día (Plus, `docs/modelo-negocio.md`); hoy no hay tope y esto no se lee: queda dicho para quien lo construya. */
  const val COUNTS_FOR_FREE_CAP = false

  /** Lo que dura la partida del Desafío, en minutos: la de un juego de la ruta ([DailyPath.MINUTES_PER_GAME]). */
  const val MINUTES = DailyPath.MINUTES_PER_GAME

  /** «unos 2 minutos». */
  fun minutesText(): String = DailyPath.minutesText(1)

  /**
   * El juego del Desafío de hoy, o null si no hay candidato.
   * @param dateKey día («yyyy-MM-dd»): semilla del desempate.
   * @param measured juegos ya medidos (`SkillState.measured`).
   * @param routeGameIds los juegos de la ruta de hoy.
   * @param lastDay por juego, el último día (días desde 1970) en que fue el Desafío de hoy; sin entrada = nunca lo fue.
   * @param today el día de hoy en esos mismos días ([lastDay] de ayer = `today - 1`).
   */
  fun pick(
    dateKey: String,
    measured: Set<String>,
    routeGameIds: List<String>,
    lastDay: Map<String, Long>,
    today: Long,
    games: List<GameDefinition> = GameRegistry.allGames
  ): String? {
    val route = routeGameIds.toSet()
    val yesterday = lastDay.filterValues { it == today - 1 }.keys
    val candidates = games.filter { it.id in measured && it.id !in route && it.id !in yesterday }
    if (candidates.isEmpty()) return null
    val routeAreas: Set<DomainType> = games.filter { it.id in route }.map { it.domain }.toSet()
    val outside = candidates.filter { it.domain !in routeAreas }
    val pool = outside.ifEmpty { candidates }
    fun daysSince(id: String): Long = lastDay[id]?.let { (today - it).coerceAtLeast(0L) } ?: Long.MAX_VALUE
    return pool.sortedWith(compareByDescending<GameDefinition> { daysSince(it.id) }.thenBy { tieBreak(dateKey, it.id) }).first().id
  }

  /**
   * El Desafío de hoy con lo que se recuerda: si ya hay uno guardado para [dateKey] se queda (el juego no cambia durante el día aunque cambien los datos); si no, se elige con [pick] y se anota como el último día de ese juego.
   * Devuelve el Desafío (null: no hay candidato; no se guarda nada y se vuelve a mirar la próxima vez) y el registro nuevo.
   */
  fun resolve(log: ChallengeLog, dateKey: String, today: Long, measured: Set<String>, routeGameIds: List<String>, games: List<GameDefinition> = GameRegistry.allGames): Pair<ChallengeToday?, ChallengeLog> {
    log.today?.takeIf { it.day == dateKey }?.let { return it to log }
    val id = pick(dateKey, measured, routeGameIds, log.lastDay, today, games) ?: return null to log.copy(today = null)
    val chosen = ChallengeToday(dateKey, id)
    return chosen to ChallengeLog(chosen, log.lastDay + (id to today))
  }

  /**
   * Una partida de [gameId] en [mode] terminó el día [dateKey]: si es el Desafío de hoy en modo Desafío, pasa a «jugado» o, si [passed], a «superado» (que no se pierde: repetir después no lo baja). Null = nada que cambiar.
   * Una partida del mismo juego en otro modo no cuenta como el Desafío.
   */
  fun after(current: ChallengeToday?, dateKey: String, gameId: String, mode: PlayMode, passed: Boolean): ChallengeToday? {
    if (current == null || current.day != dateKey || current.gameId != gameId || mode != PlayMode.DESAFIO) return null
    val next = when { current.state == ChallengeState.SUPERADO -> return null; passed -> ChallengeState.SUPERADO; else -> ChallengeState.JUGADO }
    return current.copy(state = next).takeIf { it != current }
  }

  /** Desempate estable (no depende del azar ni de la corrida): la misma fecha y el mismo juego dan siempre el mismo número. */
  private fun tieBreak(dateKey: String, gameId: String): Int = (dateKey + "|" + gameId).hashCode()
}
