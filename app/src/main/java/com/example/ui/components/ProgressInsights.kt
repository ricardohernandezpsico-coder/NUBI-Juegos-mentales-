package com.example.ui.components


/**
 * Datos de la pantalla de Progreso basados en el rating del DDA común (0..1 por juego, ver docs/DDA-comun.md). Es un indicador interno de avance, no una medida clínica validada.
 * (8-oct: se quitaron la campana con el percentil y el radar del «perfil cognitivo»: no se muestran percentiles ni comparaciones con una referencia supuesta.)
 */

/** Rating 0..1 por juego; null = todavía sin dato. */
typealias GameLevels = Map<String, Float?>

/** Índice general 0..100: promedio de los juegos con dato; null si no hay ninguno. */
fun overallIndex(levels: GameLevels): Int? {
  val vals = levels.values.filterNotNull()
  return if (vals.isEmpty()) null else (vals.average() * 100).toInt().coerceIn(0, 100)
}

/** La etapa del avance (Inicio…Maestro, data/Skill.kt): los mismos nombres en toda la app. */
fun levelWord(v: Float): String = com.example.data.Skill.stageName(v)
