package com.example.data

import kotlin.math.abs
import kotlin.math.atan2
import kotlin.math.hypot

/**
 * Lectura del final de Rumbo a Casa (integración de trayecto: volver al punto de partida sin verlo; tarea de completar
 * el triángulo, Klatzky et al., 1990; Loomis et al., 1993).
 *
 * Cada vuelta llega de Unity como el lugar donde quedó la nave en el marco de la VUELTA JUSTA, en fracciones de la
 * distancia que había hasta casa: la base está en (along = 1, lateral = 0) y el punto donde empezó la vuelta en (0, 0);
 * lateral positivo = a la derecha. De ahí salen, sin suposiciones:
 * - el desvío del rumbo (ángulo de ese punto visto desde el inicio de la vuelta);
 * - la distancia recorrida (qué tan lejos del inicio quedó: 1 = justo, menos = corto);
 * - lo que más la alejó de casa: hacia los costados (rumbo) o antes/después (distancia).
 * En los giros no hay bordes que sesguen el error (a diferencia de la regla de Aterrizaje), así que el signo vale; aun
 * así, con pocos viajes no se nombra ningún patrón (ver los mínimos).
 */
data class HomingTrip(val along: Float, val lateral: Float, val beacon: Boolean) {
  /** Desvío del rumbo en grados, con signo (+ = a la derecha de casa). */
  val angleDeg: Float get() = Math.toDegrees(atan2(lateral.toDouble(), along.toDouble())).toFloat()
  /** Recorrido de vuelta / distancia real a casa. */
  val distanceRatio: Float get() = hypot(along, lateral)
  /** Distancia final a la base / distancia a casa. */
  val errorRatio: Float get() = hypot(along - 1f, lateral)
}

/** Qué alejó más de casa en la partida. */
enum class HomingSource { SIN_DATOS, PAREJO, RUMBO, DISTANCIA }

/** Cómo se calculó la distancia en la partida. */
enum class HomingDistance { SIN_DATOS, JUSTA, CORTO, LARGO, VARIA }

object Homing {
  /** Viajes mínimos para nombrar un patrón (rumbo/distancia). */
  const val MIN_TRIPS = 4
  /** Viajes mínimos con faro y sin faro para compararlos. */
  const val MIN_PER_BEACON = 3
  /** Distancia "justa": dentro de ±10%. */
  const val DISTANCE_OK = 0.10f
  /** Para decir "sueles quedarte corto / pasarte": 70% de los viajes para el mismo lado. */
  const val SAME_SIDE = 0.7f
  /** Para nombrar qué aleja más: una parte al menos 1,5 veces la otra y 5 puntos más. */
  const val SOURCE_RATIO = 1.5f
  const val SOURCE_GAP = 0.05f
  /** Diferencia de grados entre con faro y sin faro para decir que ayudó (o no). */
  const val BEACON_GAP = 8f

  fun trips(along: List<Float>, lateral: List<Float>, beacon: List<Boolean>): List<HomingTrip> {
    val n = minOf(along.size, lateral.size, beacon.size)
    return (0 until n).map { HomingTrip(along[it], lateral[it], beacon[it]) }
  }

  /** Desvío medio del rumbo (grados, sin signo); null sin viajes. */
  fun meanAbsAngle(trips: List<HomingTrip>): Float? =
    trips.takeIf { it.isNotEmpty() }?.map { abs(it.angleDeg) }?.average()?.toFloat()

  fun distance(trips: List<HomingTrip>): HomingDistance {
    if (trips.size < MIN_TRIPS) return HomingDistance.SIN_DATOS
    val diffs = trips.map { it.distanceRatio - 1f }
    val mean = diffs.average().toFloat()
    val short = diffs.count { it < -DISTANCE_OK } / trips.size.toFloat()
    val long = diffs.count { it > DISTANCE_OK } / trips.size.toFloat()
    return when {
      abs(mean) < DISTANCE_OK && short < 0.5f && long < 0.5f -> HomingDistance.JUSTA
      mean <= -DISTANCE_OK && short >= SAME_SIDE -> HomingDistance.CORTO
      mean >= DISTANCE_OK && long >= SAME_SIDE -> HomingDistance.LARGO
      else -> HomingDistance.VARIA
    }
  }

  /** Qué aleja más de casa: el error hacia los costados (rumbo) o antes/después de la base (distancia). */
  fun source(trips: List<HomingTrip>): HomingSource {
    if (trips.size < MIN_TRIPS) return HomingSource.SIN_DATOS
    val side = trips.map { abs(it.lateral) }.average().toFloat()
    val depth = trips.map { abs(it.along - 1f) }.average().toFloat()
    return when {
      side >= SOURCE_RATIO * depth && side - depth >= SOURCE_GAP -> HomingSource.RUMBO
      depth >= SOURCE_RATIO * side && depth - side >= SOURCE_GAP -> HomingSource.DISTANCIA
      else -> HomingSource.PAREJO
    }
  }

  /** Desvío medio del rumbo con faro y sin faro (grados); null si falta alguno de los dos. */
  fun beaconAngles(trips: List<HomingTrip>): Pair<Float, Float>? {
    val with = trips.filter { it.beacon }
    val without = trips.filter { !it.beacon }
    if (with.size < MIN_PER_BEACON || without.size < MIN_PER_BEACON) return null
    return meanAbsAngle(with)!! to meanAbsAngle(without)!!
  }

  fun distanceMessage(d: HomingDistance): String? = when (d) {
    HomingDistance.SIN_DATOS -> null
    HomingDistance.JUSTA -> "Distancia: la calculas bien."
    HomingDistance.CORTO -> "Distancia: sueles quedarte corto (te detienes antes de llegar)."
    HomingDistance.LARGO -> "Distancia: sueles pasarte de largo."
    HomingDistance.VARIA -> "Distancia: a veces te quedas corto y a veces te pasas."
  }

  fun sourceMessage(s: HomingSource): String? = when (s) {
    HomingSource.SIN_DATOS, HomingSource.PAREJO -> null
    HomingSource.RUMBO ->
      "Lo que más te aleja de casa es el rumbo. Truco: en cada giro, fíjate cuánto gira el polvo de estrellas y repite la ruta en voz baja (\"derecho, giro grande a la derecha, giro chico a la izquierda\")."
    HomingSource.DISTANCIA ->
      "Lo que más te aleja de casa es la distancia. Truco: cuenta el tiempo de cada tramo; la nave va siempre a la misma velocidad, así que la vuelta también se puede medir contando."
  }

  fun beaconMessage(withDeg: Float, withoutDeg: Float): String {
    val base = "Con faro te desviaste ${withDeg.toInt()}°; sin faro, ${withoutDeg.toInt()}°."
    return when {
      withoutDeg - withDeg >= BEACON_GAP -> "$base En esta partida el faro te ayudó: cuando haya un punto fijo a lo lejos, fíjate dónde queda al girar."
      withDeg - withoutDeg >= BEACON_GAP -> "$base Esta vez el faro no te ayudó: prueba mirar dónde queda cada vez que giras."
      else -> "$base Con y sin faro te orientaste parecido."
    }
  }
}
