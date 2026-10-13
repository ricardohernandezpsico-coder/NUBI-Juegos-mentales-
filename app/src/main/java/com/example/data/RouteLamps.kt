package com.example.data

import android.content.Context
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/**
 * Qué se celebró HOY en la isla (Tarea 88): las farolas que ya hicieron su pulso al encenderse y si ya hubo el saltito de Nubi de la ruta completa. Es estado PASAJERO (`route_celebrations`, fuera del respaldo): al cambiar de día o de teléfono
 * simplemente no hay nada celebrado y la primera vez que se ve cada farola encendida pulsa de nuevo.
 */
data class RouteCelebrations(val day: String, val lamps: Set<String> = emptySet(), val complete: Boolean = false)

/** Lo que hay que animar al mostrar Hoy: los lugares cuya farola se enciende ahora por primera vez hoy (en el orden de la ruta) y si es la primera vez que la ruta queda completa. */
data class RoutePlan(val pulse: List<String>, val complete: Boolean) {
  val isEmpty: Boolean get() = pulse.isEmpty() && !complete

  companion object { val NONE = RoutePlan(emptyList(), false) }
}

/** Dónde se guarda lo celebrado hoy. */
interface RouteCelebrationStore {
  fun load(): RouteCelebrations?
  fun save(value: RouteCelebrations)
}

/** En preferencias propias y pasajeras (`route_celebrations`: no entra en `backup_rules.xml`; `BackupRulesTest` lo lista como pasajero). */
class PrefsRouteCelebrationStore(context: Context) : RouteCelebrationStore {
  private val prefs = context.applicationContext.getSharedPreferences("route_celebrations", Context.MODE_PRIVATE)

  override fun load(): RouteCelebrations? {
    val day = prefs.getString("day", null) ?: return null
    return RouteCelebrations(day, prefs.getStringSet("lamps", emptySet()).orEmpty().toSet(), prefs.getBoolean("complete", false))
  }

  override fun save(value: RouteCelebrations) {
    prefs.edit().putString("day", value.day).putStringSet("lamps", value.lamps).putBoolean("complete", value.complete).apply()
  }

  companion object { const val FILE = "route_celebrations" }
}

/** La lógica pura de las farolas de la ruta de hoy: qué farolas están encendidas y qué hay que celebrar. Nada de esto mide puntajes: premia la constancia (haber hecho la parada), nunca cómo salió. */
object RouteLamps {
  /** La clave de lugar (`observatorio`, `antena`, `biblioteca`, `hangar`) del área (`MEMORIA`…); null si el área no existe. */
  fun placeKey(area: String): String? = IslaLugar.nameOfArea(area)?.lowercase()

  /** Los índices de las farolas ENCENDIDAS (en [IslaLayout.lamps]) con las áreas de las paradas hechas hoy ([doneAreas]). Una farola de un lugar que no está en la ruta, o aún no se hace, queda apagada. */
  fun litLamps(layout: IslaLayout, doneAreas: Collection<String>): Set<Int> =
    doneAreas.mapNotNull { a -> placeKey(a)?.let { layout.lampIndex(it) } }.toSet()

  /** El día («2026-10-13») con el que se separa lo celebrado. */
  fun dayKey(now: Long): String = SimpleDateFormat("yyyy-MM-dd", Locale.US).format(Date(now))

  /**
   * Qué animar. [doneInOrder] = los lugares de las paradas hechas hoy, en el orden de la ruta; [allDone] = las tres paradas están hechas; [saved] = lo que ya se celebró (de otro día = nada).
   * Cada farola pulsa UNA sola vez por parada y por día; la ruta completa se celebra UNA sola vez al día.
   */
  fun plan(doneInOrder: List<String>, allDone: Boolean, saved: RouteCelebrations?, day: String): RoutePlan {
    val today = saved?.takeIf { it.day == day } ?: RouteCelebrations(day)
    return RoutePlan(doneInOrder.filter { it !in today.lamps }.distinct(), allDone && !today.complete)
  }

  /** Lo celebrado después de aplicar [plan]: lo que ya estaba y lo que se acaba de animar. */
  fun consume(plan: RoutePlan, saved: RouteCelebrations?, day: String): RouteCelebrations {
    val today = saved?.takeIf { it.day == day } ?: RouteCelebrations(day)
    return today.copy(lamps = today.lamps + plan.pulse, complete = today.complete || plan.complete)
  }

  /**
   * El nombre que dice el lector de pantalla para la isla con el estado de la ruta (Tarea 88): «Tu isla: hecho el Observatorio; faltan la Antena y el Hangar». [placesInOrder] = las paradas de hoy, en el orden de la ruta, como (artículo + lugar, hecha).
   * Sin paradas: la isla entera; sin ninguna hecha: lo de antes («hoy la ruta pasa por…»); con todas hechas: «ruta completa».
   */
  fun description(placesInOrder: List<Pair<String, Boolean>>): String {
    if (placesInOrder.isEmpty()) return "Tu isla: el Observatorio, la Antena, la Biblioteca y el Hangar"
    fun list(names: List<String>) = if (names.size > 1) names.dropLast(1).joinToString(", ") + " y " + names.last() else names.joinToString()
    val done = placesInOrder.filter { it.second }.map { it.first }
    val pending = placesInOrder.filter { !it.second }.map { it.first }
    return when {
      done.isEmpty() -> "Tu isla: hoy la ruta pasa por ${list(pending)}"
      pending.isEmpty() -> "Tu isla: ruta completa, hechos ${list(done)}"
      else -> "Tu isla: ${if (done.size == 1) "hecho" else "hechos"} ${list(done)}; ${if (pending.size == 1) "falta" else "faltan"} ${list(pending)}"
    }
  }
}
