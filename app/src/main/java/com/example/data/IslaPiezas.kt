package com.example.data

/** En qué capa de la isla se dibuja una pieza: [SUELO] va sobre la losa y DETRÁS de los edificios (`isla_base` → suelo → `isla_edificios`); [AIRE] va ENCIMA de los edificios (banderines). */
enum class IslaCapa { SUELO, AIRE }

/**
 * «Tu isla crece» (Tarea 136, A1 de `auditoria-fable/spec-adopcion-A1-A7.md`): las diez piezas que la isla gana con las medallas de CONDUCTA ([Achievements.all]; nunca de desempeño ni de liga). Tres familias: **Constancia** (el jardín crece),
 * **Variedad** (los banderines unen los lugares) y **Volver** (el muelle: la nave vuelve al puerto). Cada pieza es una capa transparente de Blender con el mismo recorte que `isla_base` (`res/drawable-nodpi/isla_deco_<id>.webp`) y su imagen chica
 * (`kit_pz_deco_<id>.webp`) para la celebración de la medalla; su ancla y su capa también están en `res/raw/isla.json` (`decoraciones`; `IslaPiezasTest` los compara con este enum).
 *
 * **Todo se DERIVA de las medallas** ([earned]): no hay estado de progreso nuevo, así que al restaurar el respaldo en otro teléfono las piezas vuelven solas. Y como las medallas nunca se revocan (`AchievementsTest`), **nada se quita nunca**.
 * Los niveles 1, 3, 6 y 8 de Constancia y el 1 de Variedad no traen pieza (la medalla basta): las piezas marcan hitos espaciados.
 *
 * El orden del enum es el de DIBUJO dentro de cada capa (las flores sobre los arbustos, los frutos sobre el árbol, la nave y el farol sobre el muelle).
 * [nombre] es lo que dice la celebración («Y en tu isla aparece: un muelle») y [nombreCorto] lo que dice el lector de pantalla en la lista («Tu isla tiene: dos arbustos, banderines y un muelle»).
 */
enum class IslaPieza(val id: String, val family: AchievementFamily, val achievementId: String, val capa: IslaCapa, val nombre: String, val nombreCorto: String) {
  C1("c1", AchievementFamily.CONSTANCIA, "racha_7", IslaCapa.SUELO, "dos arbustos", "dos arbustos"),
  C2("c2", AchievementFamily.CONSTANCIA, "racha_14", IslaCapa.SUELO, "flores en el jardín", "flores"),
  C3("c3", AchievementFamily.CONSTANCIA, "racha_30", IslaCapa.SUELO, "un árbol", "un árbol"),
  C4("c4", AchievementFamily.CONSTANCIA, "racha_100", IslaCapa.SUELO, "frutos en el árbol", "frutos"),
  V1("v1", AchievementFamily.VARIEDAD, "dominios", IslaCapa.AIRE, "banderines entre la Antena y la Biblioteca", "banderines"),
  V2("v2", AchievementFamily.VARIEDAD, "explorador", IslaCapa.AIRE, "banderines sobre el Observatorio", "banderines"),
  V3("v3", AchievementFamily.VARIEDAD, "explorador_17", IslaCapa.AIRE, "un banderín en cada techo", "banderines"),
  VO1("vo1", AchievementFamily.VOLVER, "vuelta_1", IslaCapa.SUELO, "un muelle", "un muelle"),
  VO2("vo2", AchievementFamily.VOLVER, "vuelta_3", IslaCapa.SUELO, "una nave amarrada", "una nave amarrada"),
  VO3("vo3", AchievementFamily.VOLVER, "vuelta_10", IslaCapa.SUELO, "el farol del muelle", "el farol del muelle");
}

/** Lo que hay que revelar al mostrar Hoy: las piezas ganadas que Hoy aún no había mostrado (en el orden de dibujo). */
data class PiezasPlan(val reveal: List<IslaPieza>) {
  val isEmpty: Boolean get() = reveal.isEmpty()

  companion object { val NONE = PiezasPlan(emptyList()) }
}

/** La lógica pura de las piezas de la isla: cuáles hay, qué revelar en Hoy y cómo se dicen. Nada de esto mide puntajes: las piezas premian la constancia, la variedad y el volver, y nunca se quitan. */
object IslaPiezas {
  /** Las piezas que dan las medallas [unlocked] (ids de logro), en el orden de dibujo. Sin medallas, sin piezas. */
  fun earned(unlocked: Collection<String>): List<IslaPieza> = unlocked.toSet().let { u -> IslaPieza.entries.filter { it.achievementId in u } }

  /** La pieza que trae la medalla [achievementId], o null si esa medalla no trae pieza (la celebración queda como era, sin sonido). */
  fun forAchievement(achievementId: String): IslaPieza? = IslaPieza.entries.firstOrNull { it.achievementId == achievementId }

  /**
   * Qué revelar en Hoy: las piezas [earned] que no están en [seen]. Con [seen] en null (la primera vez que Hoy se abre con esta versión, o en un teléfono nuevo) no se revela nada: lo que ya se tiene se SIEMBRA en silencio ([consume]),
   * como `seeded_v2` con las medallas. Así quien ya tenía medallas ve su isla con las piezas puestas y no «llueve» nada.
   */
  fun plan(earned: List<IslaPieza>, seen: Set<String>?): PiezasPlan =
    if (seen == null) PiezasPlan.NONE else PiezasPlan(earned.filter { it.id !in seen })

  /** Lo visto después de aplicar [plan]: lo que ya estaba más las piezas [earned] (las reveladas ahora y, la primera vez, todas las que ya se tenían). */
  fun consume(earned: List<IslaPieza>, seen: Set<String>?): Set<String> = seen.orEmpty() + earned.map { it.id }

  /** «dos arbustos, banderines y un muelle»: los nombres cortos sin repetir ([IslaPieza.nombreCorto]), en el orden de dibujo; vacío si no hay piezas. */
  fun list(pieces: List<IslaPieza>): String {
    val names = pieces.map { it.nombreCorto }.distinct()
    return if (names.size > 1) names.dropLast(1).joinToString(", ") + " y " + names.last() else names.joinToString()
  }

  /** « · Tu isla tiene: dos arbustos, banderines y un muelle» para el final de la descripción de la isla (lector de pantalla); vacío si no hay piezas. */
  fun description(pieces: List<IslaPieza>): String = if (pieces.isEmpty()) "" else " · Tu isla tiene: ${list(pieces)}"

  /** La línea de la celebración de una medalla que trae pieza: «Y en tu isla aparece: un muelle». */
  fun announcement(piece: IslaPieza): String = "Y en tu isla aparece: ${piece.nombre}"
}
