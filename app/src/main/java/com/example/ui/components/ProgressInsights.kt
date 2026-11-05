package com.example.ui.components


/**
 * Datos de la pantalla de Progreso basados en el rating del DDA común (0..1 por juego, ver docs/DDA-comun.md). Es un indicador interno de avance, no una medida clínica validada.
 * (8-oct: se quitaron la campana con el percentil y el radar del «perfil cognitivo»: no se muestran percentiles ni comparaciones con una referencia supuesta.)
 */

/** Rating 0..1 por juego; null = todavía sin dato. */
typealias GameLevels = Map<String, Float?>

// (Tarea 230: `overallIndex` y `levelWord`, el índice general y el nombre de la etapa de «Etapa general», salieron de Tú con las etapas.)
