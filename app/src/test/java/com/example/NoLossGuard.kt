package com.example

/**
 * La guardia «sin pérdida» (Tarea 131, A7; `auditoria-fable/spec-adopcion-A1-A7.md`): Nubi no usa el miedo a perder (la racha, una oportunidad, una oferta) para que la persona vuelva; la racha «perdona» y espera. Estas listas las comparten
 * `VisibleTextsGuardTest` (todos los literales de la app), `ReminderContentTest` (los recordatorios) y `AvanceLogicTest` (las frases de Nubi en Avance). Todo va en minúsculas: se compara contra el texto en minúsculas.
 *
 * Lo que SÍ puede decirse (y la guardia deja pasar): «no pierdes nada si dudas» (Meteoros: es una tranquilidad, no una amenaza) y «Descansaste el martes: tu racha sigue».
 */
object NoLossGuard {
  /** Para los literales de toda la app. A la lista de la spec se le suman «pierdas tu» y «no pierdas»: sin ellas, «¡No pierdas tu racha!» (el ejemplo de la propia spec) se escapaba, porque la lista traía «pierdes tu» y no «pierdas tu». */
  val literals = listOf(
    "pierdes tu", "pierdas tu", "no pierdas", "perder tu", "perderás", "perderas", "se acaba tu", "última oportunidad", "ultima oportunidad", "solo por hoy", "sólo hoy",
    "no te lo pierdas", "antes de que sea tarde", "te quedas sin", "quedarás sin"
  )

  /** Para los recordatorios y las frases de Nubi (raíces más cortas: ahí cualquier «pierd…», «última» o «rápido» sobra). */
  val reminderRoots = listOf("pierd", "perder", "se acaba", "última", "ultima", "solo hoy", "apúrate", "apurate", "rápido", "te atrasaste", "te olvidaste")

  /** La primera de [list] que aparece en [text] (sin importar mayúsculas), o null. */
  fun hit(text: String, list: List<String>): String? = text.lowercase().let { low -> list.firstOrNull { it in low } }
}
