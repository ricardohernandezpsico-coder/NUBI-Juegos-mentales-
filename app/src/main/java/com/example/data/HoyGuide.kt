package com.example.data

import com.example.model.GameRegistry

/** Los tres pasos de la guía de Nubi en la primera entrada a Hoy, en orden (Tarea 193; `auditoria-fable/guia-primer-ingreso.md` §3). Desde la Tarea 219 siguen la pantalla de arriba abajo: «Todos los juegos», el Contrarreloj (debajo de él) y la barra. */
enum class GuideStep { TODOS, CONTRARRELOJ, BARRA }

/**
 * La guía de la primera entrada a Hoy (Tarea 193, propuesta A de Fable, 30-oct): el día del Primer vuelo, Nubi señala TRES cosas que la pantalla no explica sola, con el mismo velo, hueco y globo de los tutoriales de los juegos: «Todos los juegos», el Contrarreloj y la barra de abajo (Contrarreloj, Avance y Tú). Desde la Tarea 219 (Hoy reordenada: ruta, «Todos los juegos», Contrarreloj) el orden es ese, el de la pantalla de arriba abajo; el blanco del paso del Contrarreloj es el BLOQUE entero (la losa o, sin partidas, el recuadro con la fila gris).
 * Lógica pura: qué se dice, cuándo sale y qué sigue. La pantalla (`ui/components/HoyGuideOverlay`) y el ViewModel (`guideStep`) la usan; ver `docs/guia-primer-ingreso.md`.
 *
 * Reglas de los textos (`GlobosDeNubiTest`): 2 frases y 100 letras como máximo, UNA idea, el número con su unidad y lo iluminado con la palabra que se lee en pantalla. No hablan del sol ni de «ahora»: valen en cualquier día.
 */
object HoyGuide {
  /** Cuánto tarda en salir después de que Hoy queda a la vista, sin nada encima (ms). */
  const val STEP_DELAY_MS = 600L

  val steps: List<GuideStep> = GuideStep.entries

  /**
   * El texto del globo (§3.2: exacto). El número de juegos sale del catálogo: si cambia, `HoyGuideTest` falla y el texto se revisa a propósito.
   * [startingPoint] = Avance ya muestra algo medido (al menos una partida: las áreas con sus puntos). Si el Primer vuelo terminó sin jugar ningún juego (Tarea 193b), Avance solo dice «Todavía no hay nada aquí» y la invitación a encontrar el punto de partida: el paso 3 habla de lo que se verá
   * («cada área») y no de un punto de partida que todavía no existe. Desde la Tarea 215 el paso 3 nombra las TRES pestañas de la derecha: la nueva, Contrarreloj, es donde viven las marcas.
   */
  fun text(step: GuideStep, startingPoint: Boolean = true): String = when (step) {
    GuideStep.CONTRARRELOJ -> "El Contrarreloj dura 2 minutos y su juego cambia cada día."
    GuideStep.TODOS -> "Aquí están los ${GameRegistry.allGames.size} juegos: eliges el que quieras."
    GuideStep.BARRA ->
      if (startingPoint) "En Contrarreloj ves tus marcas. En Avance, tu punto de partida; en Tú, tus ajustes."
      else "En Contrarreloj ves tus marcas. En Avance, cada área; en Tú, tus ajustes."
  }

  /** «Paso 1 de 3» (sin palabras sueltas: como el Primer vuelo, `FirstFlight.stationText`). */
  fun counter(step: GuideStep): String = "Paso ${steps.indexOf(step) + 1} de ${steps.size}"

  /** Lo que dice el chip principal: «Seguir ›» y, en el último paso, «Listo» (sin flecha). */
  fun chipLabel(step: GuideStep): String = if (next(step) == null) "Listo" else "Seguir ›"

  /** Lo que el lector de pantalla dice del chip principal (sin la flecha, que leería «comilla angular»). */
  fun chipSpoken(step: GuideStep): String = if (next(step) == null) "Listo" else "Seguir"

  /** «Saltar la guía»: el chip crema de arriba a la derecha, en los tres pasos. */
  const val SKIP_LABEL = "Saltar la guía"

  /** Lo que dice TalkBack al entrar a cada paso (§3.2): el nombre y el lugar de lo iluminado, porque quien no ve el hueco necesita saber qué se señala. */
  fun spoken(step: GuideStep, startingPoint: Boolean = true): String = when (step) {
    GuideStep.TODOS -> "Nubi te muestra Hoy. ${counter(step)}. El botón Todos los juegos, debajo de tu ruta. ${text(step, startingPoint)}"
    GuideStep.CONTRARRELOJ -> "${counter(step)}. El Contrarreloj, debajo de Todos los juegos. ${text(step, startingPoint)}"
    GuideStep.BARRA -> "${counter(step)}. Las pestañas Contrarreloj, Avance y Tú, en la barra de abajo. ${text(step, startingPoint)}"
  }

  /** El paso que sigue, o null después del último (la guía termina). */
  fun next(step: GuideStep): GuideStep? = steps.getOrNull(steps.indexOf(step) + 1)

  /**
   * ¿Toca mostrarla ahora? Solo con TODAS: la persona terminó el Primer vuelo COMPLETO ([firstFlightDone]: la marca `first_flight_done`, la ponen `finishFlight` y el modo completo, jugara 0, 1, 2, 3 o 4 juegos; «Repetir la evaluación» nunca), no la vio ([seen] es la marca `hoy_guide_seen`),
   * Hoy está a la vista y no hay nada encima ([somethingAbove] falso: ni una medalla, ni un resultado, ni un panel) y el arranque ya terminó ([startupReady]: `StartupStage.READY`). Desde la Tarea 193b ya no depende de que sea el día del vuelo: si se cierra a mitad, vuelve la próxima
   * vez que Hoy esté a la vista aunque sea otro día; una vez terminada o saltada, nunca más.
   */
  fun shouldShow(firstFlightDone: Boolean, seen: Boolean, somethingAbove: Boolean, startupReady: Boolean): Boolean =
    firstFlightDone && !seen && !somethingAbove && startupReady
}
