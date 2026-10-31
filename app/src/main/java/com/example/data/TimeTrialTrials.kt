package com.example.data

import com.squareup.moshi.JsonClass
import com.squareup.moshi.Moshi

/**
 * El RITMO de una partida de Contrarreloj (Tarea 200; `auditoria-fable/nubi-plus-comparacion.md` §2.2): un registro por ensayo resuelto, en el orden en que se resolvieron, con tres listas PARALELAS (una posición = un ensayo) que Unity anota en `TimeTrial.Run.Register` y manda como `tt_t_ms`, `tt_ok` y `tt_level`:
 *  - [tMs]: milisegundos desde el «¡Ya!» hasta que se resolvió el ensayo (el reloj de la partida: una pausa, o «Cómo se juega», no los corre; nunca decrecen ni pasan de los 120 s);
 *  - [ok]: si el ensayo fue un acierto (`true`) o un error (`false`);
 *  - [level]: el paso de la pista en que se JUGÓ el ensayo (el de antes de registrarlo).
 * Se guarda en la columna `trials` de `time_trial_results` como texto JSON ([toJson]: `{"t_ms":[…],"ok":[1,0,…],"level":[…]}`; unos 1,5 KB por partida) y solo sirve para contar el ritmo (`TimeTrialMarks.pace`): nunca se muestra un tiempo por ensayo como «tu velocidad», ni se compara con otras personas.
 * Una partida vieja, o de una versión de Unity sin el registro, queda con `trials` en null. Lógica pura, con pruebas (`TimeTrialTrialsTest`).
 */
data class TimeTrialTrials(val tMs: List<Int>, val ok: List<Boolean>, val level: List<Int>) {
  /** Cuántos ensayos hay anotados. */
  val size: Int get() = tMs.size

  /** Los aciertos y los errores anotados (con el registro completo, son los de la partida). */
  val hits: Int get() = ok.count { it }
  val errors: Int get() = ok.count { !it }

  /** El texto que va a la columna `trials`: las tres listas, con 1 por acierto y 0 por error (como las manda Unity). */
  fun toJson(): String = adapter.toJson(TrialsJson(tMs, ok.map { if (it) 1 else 0 }, level))

  companion object {
    /** Tope de ensayos que Unity anota por partida (`TimeTrial.Run.MaxLoggedTrials`); un registro más largo no viene de Unity y no se guarda. */
    const val MAX = 200

    /** Lo máximo que puede valer un tiempo: lo que dura la partida (ms). */
    const val MAX_MS = TimeTrialDay.SECONDS * 1000

    private val adapter by lazy { Moshi.Builder().build().adapter(TrialsJson::class.java) }

    /**
     * Las tres listas de Unity (`tt_t_ms`, `tt_ok`, `tt_level`) hechas registro, o null si no hay ritmo que guardar: faltan, vienen vacías (una partida de siempre las manda así), miden distinto, pasan del tope [MAX] o traen un dato imposible
     * (`ok` que no es 0 o 1, un tiempo negativo, que decrece o que pasa de [MAX_MS], un paso menor que 1 o que baja). Todo o nada: un registro a medias no se guarda, la partida sí.
     */
    fun of(tMs: List<Int>?, ok: List<Int>?, level: List<Int>?): TimeTrialTrials? {
      if (tMs == null || ok == null || level == null) return null
      val n = tMs.size
      if (n == 0 || n > MAX || ok.size != n || level.size != n) return null
      if (ok.any { it != 0 && it != 1 }) return null
      if (tMs.first() < 0 || tMs.last() > MAX_MS || tMs.zipWithNext().any { (a, b) -> b < a }) return null
      if (level.first() < 1 || level.zipWithNext().any { (a, b) -> b < a }) return null
      return TimeTrialTrials(tMs, ok.map { it == 1 }, level)
    }

    /** El registro de la columna `trials`; null si no hay texto o no se puede leer (un texto dañado nunca rompe la lista de partidas: esa partida queda sin ritmo). */
    fun fromJson(json: String?): TimeTrialTrials? {
      if (json.isNullOrBlank()) return null
      val dto = try { adapter.fromJson(json) } catch (e: Exception) { null } ?: return null
      return of(dto.t_ms, dto.ok, dto.level)
    }
  }

  /** La forma del texto guardado: los mismos nombres que usa Unity, sin el prefijo `tt_`. */
  @JsonClass(generateAdapter = true)
  internal data class TrialsJson(val t_ms: List<Int>, val ok: List<Int>, val level: List<Int>)
}
