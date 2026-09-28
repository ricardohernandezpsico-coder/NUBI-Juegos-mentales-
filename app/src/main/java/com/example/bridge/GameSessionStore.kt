package com.example.bridge

import android.content.Context
import com.example.NeuroVidaApplication
import org.json.JSONObject

/**
 * Lo que la app necesita recordar de una partida mientras Unity está al frente, guardado en disco.
 *
 * Por qué: con un juego abierto, la app queda en segundo plano y Android puede cerrar su proceso para liberar memoria
 * (pasa seguido en teléfonos con poca RAM: Unity ocupa mucho). Al volver, el ViewModel es nuevo y no sabía qué
 * partida se estaba jugando: el resultado se guardaba igual, pero sin pantalla de resultado, sin "Siguiente juego"
 * en el camino diario y sin seguir la evaluación inicial. Con esto la app retoma el flujo donde iba.
 *
 * - Partida en curso ([InFlight]): se anota al lanzar Unity con su id de lanzamiento y se borra al procesar el resultado.
 * - Resultado pendiente: si el resultado llega (broadcast de [NativeReceiver]) sin nadie escuchando, se guarda acá y
 *   la app lo procesa al volver a abrirse, en vez de guardarlo "a ciegas".
 * - Evaluación inicial en curso: cuántos juegos van y qué midieron.
 * Solo se usa en el proceso principal.
 */
object GameSessionStore {
  private const val PREFS = "game_session"

  data class InFlight(
    val launchId: String,
    val gameId: String,
    val level: Int,
    val timed: Boolean,
    val daily: Boolean,
    val intensity: Int,
    val assessmentStep: Int,
    val mode: com.example.data.PlayMode = com.example.data.PlayMode.A_TU_MEDIDA
  )

  data class BaselineProgress(val done: Int, val measured: Map<String, Float>, val ageBand: String?)

  private val prefs get() = NeuroVidaApplication.instance.getSharedPreferences(PREFS, Context.MODE_PRIVATE)

  // ------------------------------------------------------------------ partida en curso

  fun saveInFlight(p: InFlight) {
    prefs.edit()
      .putString("launchId", p.launchId)
      .putString("gameId", p.gameId)
      .putInt("level", p.level)
      .putBoolean("timed", p.timed)
      .putBoolean("daily", p.daily)
      .putInt("intensity", p.intensity)
      .putInt("assessmentStep", p.assessmentStep)
      .putString("mode", p.mode.name)
      .apply()
  }

  /** La partida en curso si es la de [launchId] (null si no hay o es otra). */
  fun inFlight(launchId: String?): InFlight? {
    val p = prefs
    val id = p.getString("launchId", null) ?: return null
    if (launchId == null || id != launchId) return null
    val gameId = p.getString("gameId", null) ?: return null
    return InFlight(
      launchId = id,
      gameId = gameId,
      level = p.getInt("level", 1),
      timed = p.getBoolean("timed", false),
      daily = p.getBoolean("daily", false),
      intensity = p.getInt("intensity", 0),
      assessmentStep = p.getInt("assessmentStep", 0),
      mode = runCatching { com.example.data.PlayMode.valueOf(p.getString("mode", null) ?: "") }.getOrDefault(com.example.data.PlayMode.A_TU_MEDIDA)
    )
  }

  fun hasInFlight(launchId: String?): Boolean = inFlight(launchId) != null

  fun clearInFlight() {
    prefs.edit()
      .remove("launchId").remove("gameId").remove("level").remove("timed")
      .remove("daily").remove("intensity").remove("assessmentStep")
      .apply()
  }

  // ------------------------------------------------------------------ resultado pendiente

  fun savePendingResult(launchId: String?, json: String) {
    prefs.edit().putString("pendingLaunchId", launchId).putString("pendingJson", json).apply()
  }

  /** Saca (y borra) el resultado pendiente: (id de lanzamiento, JSON). */
  fun takePendingResult(): Pair<String?, String>? {
    val p = prefs
    val json = p.getString("pendingJson", null) ?: return null
    val id = p.getString("pendingLaunchId", null)
    p.edit().remove("pendingJson").remove("pendingLaunchId").apply()
    return id to json
  }

  // ------------------------------------------------------------------ evaluación inicial en curso

  fun saveBaseline(progress: BaselineProgress?) {
    val e = prefs.edit()
    if (progress == null) {
      e.remove("baselineRun")
    } else {
      val measured = JSONObject().apply { progress.measured.forEach { (k, v) -> put(k, v.toDouble()) } }
      e.putString(
        "baselineRun",
        JSONObject().put("done", progress.done).put("measured", measured).put("age", progress.ageBand ?: "").toString()
      )
    }
    e.apply()
  }

  fun loadBaseline(): BaselineProgress? {
    val raw = prefs.getString("baselineRun", null) ?: return null
    return try {
      val o = JSONObject(raw)
      val m = o.getJSONObject("measured")
      val measured = m.keys().asSequence().associateWith { m.getDouble(it).toFloat() }
      BaselineProgress(o.getInt("done"), measured, o.optString("age").ifEmpty { null })
    } catch (e: Exception) {
      null
    }
  }

  /** "Borrar datos": nada de lo anterior sobrevive. */
  fun clearAll() {
    prefs.edit().clear().apply()
  }
}
