package com.example.data

import java.util.Locale

/**
 * «Engranajes: Taller de reparación» (juego de Razonamiento, id `engranajes`; ver docs/diseno-engranajes.md): la lectura de lo que manda Unity para la pantalla final y el cohete que
 * se guarda. Lógica pura con pruebas.
 *
 * La máquina del cohete viene mal armada: cada pieza tiene un cartel con lo que debe hacer y la persona la arregla con una o dos llaves (cambios: tocar el motor o una correa) y arranca.
 * La medida es «cuántas máquinas arreglaste»; la etapa más alta (de 5 grupos), el ritmo (segundos por máquina) y el cohete acompañan. Cada máquina arreglada enciende una de las 10 luces
 * del cohete; esas luces y los cohetes que ya despegaron son progreso de la persona: se guardan (preferencias `engranajes_rocket`, que SÍ van en el respaldo) y se mandan a Unity en cada
 * partida.
 *
 * El 5-oct el juego se rehízo (antes se elegía qué haría una pieza, con 2 respuestas posibles; ahora se arregla la máquina): la medida guardada ya no es comparable, así que cambió de clave
 * (`taller`, ver [StarMeasures]) y el historial viejo (clave `engranajes`) no se muestra ni se mezcla.
 */
object Engranajes {
  /** Las luces de un cohete: con la décima despega (en Unity); aquí nunca se guarda un cohete completo. */
  const val ROCKET_LIGHTS = 10
  /** Máquinas jugadas para mostrar el ritmo («no sacar conclusiones de pocos casos»). */
  const val MIN_FOR_DETAILS = 3
  /** Tiempo máximo creíble por máquina, en ms (fuera de eso no se muestra). */
  private const val MAX_MS = 300_000

  /** Los 5 grupos de etapa de la medida: motor y correas, ramas, carga y compuerta, tres piezas y dos llaves. */
  private val ETAPAS = listOf("motor y correas", "ramas", "carga y compuerta", "tres piezas", "dos llaves")

  /** El cohete de la persona: luces encendidas (0..9) y cohetes que ya despegaron. */
  data class Rocket(val lights: Int = 0, val orbit: Int = 0)

  /** Un cohete bien formado: nunca con las 10 luces (ya habría despegado) ni con números negativos. */
  fun rocket(lights: Int, orbit: Int) = Rocket(lights.coerceIn(0, ROCKET_LIGHTS - 1), orbit.coerceAtLeast(0))

  /** «Arreglaste 7 de 10 máquinas»; null sin máquinas. */
  fun headline(correct: Int?, total: Int?): String? {
    if (correct == null || total == null || total <= 0) return null
    return "Arreglaste ${correct.coerceIn(0, total)} de $total máquinas"
  }

  /** La marca para ver su evolución: % de máquinas arregladas (más alto = mejor). null sin medida. */
  fun mark(correct: Int?, total: Int?): Float? {
    if (correct == null || total == null || total <= 0) return null
    return 100f * correct.coerceIn(0, total) / total
  }

  /** «Etapa más alta: 3 de 5 (carga y compuerta)»; null si no es una etapa válida (1..5). */
  fun etapaLine(etapa: Int?): String? {
    if (etapa == null || etapa !in 1..ETAPAS.size) return null
    return "Etapa más alta: $etapa de ${ETAPAS.size} (${ETAPAS[etapa - 1]})"
  }

  /** «Faltan 4 luces: tu cohete espera en el hangar» (con 1: «Falta 1 luz»); null si no hay luces o ya despegó con todas (0). */
  fun hangarLine(lights: Int?): String? {
    if (lights == null || lights <= 0) return null
    val left = (ROCKET_LIGHTS - lights).coerceAtLeast(1)
    return (if (left == 1) "Falta 1 luz" else "Faltan $left luces") + ": tu cohete espera en el hangar"
  }

  /** Lo que pasó con el cohete en la partida: «¡Despegó tu cohete n.º 3!» (con más de uno: «Despegaron 2 cohetes»); null si ninguno despegó. */
  fun launchLine(launches: Int?, orbit: Int?): String? {
    if (launches == null || launches <= 0 || orbit == null || orbit <= 0) return null
    return if (launches == 1) "¡Despegó tu cohete n.º $orbit!" else "¡Despegaron $launches cohetes!"
  }

  /** «3 cohetes en órbita»; null sin ninguno. */
  fun orbitLine(orbit: Int?): String? {
    if (orbit == null || orbit <= 0) return null
    return if (orbit == 1) "1 cohete en órbita" else "$orbit cohetes en órbita"
  }

  /** «Tu ritmo: 7,5 s por máquina»; solo con [MIN_FOR_DETAILS] o más máquinas. */
  fun paceLine(ms: Int?, total: Int?): String? {
    if (ms == null || ms <= 0 || ms > MAX_MS || total == null || total < MIN_FOR_DETAILS) return null
    return "Tu ritmo: " + String.format(Locale("es"), "%.1f", ms / 1000f) + " s por máquina"
  }

  /** El consejo (con la marca «Truco: » que lee [ResultAdvice]; en pantalla empieza «Antes de cambiar algo…»): cuando alguna máquina falló. null si todas salieron bien. */
  fun tip(correct: Int?, total: Int?): String? {
    if (correct == null || total == null || total <= 0 || correct >= total) return null
    return "Truco: antes de cambiar algo, mira qué piezas quedan después: lo que tocas antes de una rama mueve todo lo que sigue."
  }

  /** Para lectores de pantalla. */
  fun spoken(correct: Int?, total: Int?): String = headline(correct, total) ?: "Sin medida"
}
