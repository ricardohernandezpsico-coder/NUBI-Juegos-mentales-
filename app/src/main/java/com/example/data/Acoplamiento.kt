package com.example.data

/**
 * «Acoplamiento: muelle de acoplamiento» (id `acoplamiento`, renovado el 10-oct; Unity: `Games/Acoplamiento/DockingContract.cs`; diseño en docs/diseno-acoplamiento.md): la lectura de lo que manda Unity para la pantalla final y el récord que se guarda. Lógica pura con pruebas.
 *
 * Las MEDIDAS del juego (tu giro mental y tu curva de giro) no cambiaron y se leen en `GameResultScreen` como siempre. El PREMIO, que no es una medida, son los módulos que se acoplaron a la estación, los anillos completos (cada 8 módulos), el récord de módulos en una partida y el total de toda la
 * vida (preferencias `acoplamiento_record`, que SÍ van en el respaldo; el récord se manda a Unity en cada partida como `dock_best`).
 */
object Acoplamiento {
  /** Casilleros de cada anillo de la estación (los mismos que `DockingContract.Slots` en Unity). */
  const val SLOTS = 8

  /** «24 módulos acoplados» (uno solo: «1 módulo acoplado»; ninguno: «Ningún módulo acoplado»). null sin dato. */
  fun dockedLine(docked: Int?): String? = when {
    docked == null || docked < 0 -> null
    docked == 0 -> "Ningún módulo acoplado"
    docked == 1 -> "1 módulo acoplado"
    else -> "$docked módulos acoplados"
  }

  /** «Anillos completos: 3»; null si no se completó ninguno o sin dato. */
  fun ringsLine(rings: Int?): String? = if (rings == null || rings <= 0) null else "Anillos completos: $rings"

  /** «Tu récord: 31 módulos en una partida»; con «¡Récord nuevo: 31 módulos!» solo cuando en esta partida se superó el guardado (lo dice Unity: igualarlo no cuenta). null sin récord. */
  fun recordLine(best: Int?, isNew: Boolean?): String? {
    if (best == null || best <= 0) return null
    val unit = if (best == 1) "módulo" else "módulos"
    return if (isNew == true) "¡Récord nuevo: $best $unit!" else "Tu récord: $best $unit en una partida"
  }

  /** El récord que se guarda: el mayor de lo guardado y lo que trae la partida; nunca baja y nunca es negativo. */
  fun mergeRecord(saved: Int, fromGame: Int?): Int = maxOf(saved.coerceAtLeast(0), (fromGame ?: 0).coerceAtLeast(0))

  /** Lo que se suma partida a partida (preferencias `acoplamiento_record`, que van en el respaldo): los módulos acoplados y los anillos completos de toda la vida. */
  data class Totals(val modules: Int = 0, val rings: Int = 0)

  /** Los totales después de una partida: lo guardado más lo de la partida; nunca bajan ni son negativos (un dato que no vino suma 0). */
  fun addTotals(saved: Totals, docked: Int?, rings: Int?): Totals =
    Totals(saved.modules.coerceAtLeast(0) + (docked ?: 0).coerceAtLeast(0), saved.rings.coerceAtLeast(0) + (rings ?: 0).coerceAtLeast(0))

  /** «Has acoplado 342 módulos · 12 anillos» (en singular cuando es uno; sin anillos: solo los módulos); null si todavía no hay nada. */
  fun totalLine(totals: Totals?): String? {
    if (totals == null || totals.modules <= 0) return null
    val modules = if (totals.modules == 1) "1 módulo" else "${totals.modules} módulos"
    val rings = when {
      totals.rings <= 0 -> ""
      totals.rings == 1 -> " · 1 anillo"
      else -> " · ${totals.rings} anillos"
    }
    return "Has acoplado $modules$rings"
  }

  /** Para lectores de pantalla. */
  fun spoken(docked: Int?, rings: Int?, best: Int?, isNew: Boolean?): String =
    listOfNotNull(dockedLine(docked), ringsLine(rings), recordLine(best, isNew)).joinToString(". ")
}
