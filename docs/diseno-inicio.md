# Primer vuelo con Nubi — el inicio nuevo (aprobado por Ricardo el 3-oct)

Reemplaza al onboarding actual (`OnboardingScreen`, 9 pantallas de preguntas) y a la evaluación aparte (`BaselineScreen`,
`data/Baseline.kt`: Secuencia, Tinta o Palabra y Comparación, que medían solo 2 de las 4 áreas). Maqueta jugable aprobada:
https://claude.ai/artifact/H2edrC6krMV9mAqLCxuiwa (copia local de la maqueta en `docs/previews/inicio-maqueta.html`).
Ricardo dio libertad para sumar ideas; las que agregué después de la maqueta están marcadas con **(nuevo)**.

## La idea

Se juega desde el primer minuto. Cada juego enseña mientras se juega (la primera ronda es guiada) y mide el punto de
partida de su área; las pocas preguntas necesarias van entre juego y juego, de un toque y con su porqué. Unos 5 minutos.
Inspirado en lo observable del inicio de Lumosity (capturas en `Proyectos/pantallazos Lomosity/`): jugar pronto, tutorial
dentro del juego con «Saltar tutorial», tarjeta «Acabas de usar tu {capacidad}» (antes «X puso a prueba tu Y»: igual a la de Lumosity), preguntas intercaladas y barra de avance. Nada
copiado: nombres, textos, arte y juegos propios.

## Recorrido

1. **Nubi se presenta**: «Vamos a encontrar tu punto de partida jugando. Son unos 5 minutos.»
2. **¿Cómo te llamo?** (opcional).
3. **Rango de edad**: ajusta ritmo y tamaño (no la dificultad).
4. **Juego 1 · Memoria**: Secuencia Lumínica renovada (ver «Juegos del inicio»). Tarjeta de entrada con Nubi maestra →
   ronda guiada → rondas que miden → tarjeta «Acabas de usar tu memoria de trabajo» con el primer dato en
   palabras y una línea de para qué sirve esa capacidad en la vida diaria.
5. **¿Qué te gustaría entrenar?** (hasta 3 áreas).
6. **Nubi: un dato** («poco y seguido rinde más»: práctica distribuida; sin promesas de mejora).
7. **Juego 2 · Atención**: Freno de Emergencia.
8. **Al terminar cada juego, ¿qué te sirve más ver primero?** «Lo que avancé» / «Un consejo para la próxima» (3-oct, tarea 21c; antes
   «¿Cómo prefieres que te anime?», demasiado parecida a la de Lumosity): cambia el ORDEN de la pantalla de resultado, no lo que se muestra. Se cambia en Ajustes.
9. **(nuevo) ¿Te cuesta distinguir algunos colores?** Sí / No / No sé. Con «sí», los juegos que dependen del color usan su
   paleta segura y Tinta o Palabra se marca para revisar (ver «Accesibilidad»). Se cambia en Ajustes.
10. **Juego 3 · Razonamiento**: Aterrizaje Lunar.
11. **¿Cuántos días por semana?** (3 / 4 recomendado / 5 / todos).
12. **Juego 4 · Lenguaje**: Lluvia de meteoros.
13. **Tu punto de partida**: las 4 áreas con su etapa (Inicio…Maestro), «tu fuerte hoy» y «donde más vamos a jugar»;
    «No es un examen: es desde dónde partimos». **(nuevo) Nace tu primera estrella**: Nubi celebra y en el cielo aparece la
    primera estrella (la historia de Nubi: «cada partida hace nacer una estrella»). Se enciende el día 1 de la racha.
14. **¿A qué hora te aviso?** (aquí se pide el permiso de notificaciones de Android 13+).
15. **Tu camino de hoy**: los 3 juegos del día elegidos por las metas y el punto de partida → entra a Hoy.

Salen del inicio: nivel de estudios (pasa a Perfil, opcional: hoy no cambia nada del juego) y «Así funciona» (cada cosa
se explica cuando aparece por primera vez).

## Reglas

- **Jugar en menos de un minuto**: antes del primer juego solo nombre y edad.
- **Barra de avance** arriba en todo el recorrido; en los juegos, además, el indicador de 4 pasos (✓ ✓ ● ○).
  **(nuevo)** Debajo de la barra, «faltan unos N minutos» (estimado, se actualiza).
- **Tutorial dentro del juego** (es EL tutorial de cada juego, el mismo que verá quien abra un juego por primera vez):
  - tarjeta de entrada con Nubi maestra y la meta en una frase;
  - ronda guiada: Nubi muestra, una marca indica dónde tocar, si hay error explica la regla y se repite (sin culpa);
  - «Saltar tutorial» siempre visible durante la ronda guiada;
  - la ronda guiada NO cuenta: no suma puntos, no mueve dificultad, avance ni rachas, no se guarda como partida;
  - después de la primera vez, queda en pausa → «Cómo se juega».
- **Cada juego cierra con «Acabas de usar tu…»**: qué entrenó + el primer dato en palabras + una línea de para qué sirve.
  Texto propio, basado en `docs/medidas-juegos-estrella.md`.
- **Una pregunta por pantalla, un toque, con su porqué.**
- **Honesto**: sin promesas de mejora, sin comparar con otras personas mientras no haya datos propios, sin «puntaje
  cerebral». Es «tu punto de partida».
- **Sin cuenta para empezar.** Más adelante (cuando se trabaje Google Play): al final, «Guarda tu avance con Google»,
  opcional.
- **Se puede dejar a medias**: «Terminar después» en cada juego guarda lo jugado y estima el resto (como hoy).
- **(nuevo) Se retoma donde quedó**: Android puede cerrar la app mientras corre Unity (pasa en el Motorola). Cada paso del
  inicio se guarda en disco al completarse (mismo criterio que `bridge/GameSessionStore`); al volver, sigue en el paso
  siguiente, sin repetir juegos ni preguntas.
- **(nuevo) Duración de los juegos del inicio**: versión corta de cada juego (~60 s o un número fijo de rondas), con la
  dificultad inicial según la edad. El resultado siembra el rating guardado de ese juego y la estimación de su área
  (como hace hoy `data/Baseline.kt`), para que la primera partida normal ya empiece a la medida.
- Accesibilidad de siempre: texto ≥ 18 sp en el inicio, contraste ≥ 4,5:1, toque ≥ 48 dp, acierto/error con forma o texto,
  «quitar animaciones» respetado (la barra y los cambios de pantalla con fundido).

## Juegos del inicio (uno por área)

Elegidos por ser los más pulidos y fáciles de entender en segundos:

| Área | Juego | Mide (primer dato) |
|---|---|---|
| Memoria | Secuencia Lumínica **renovada** | cuántas luces recuerdas en orden (amplitud) |
| Atención | Freno de Emergencia | cuántas veces frenaste a tiempo |
| Razonamiento | Aterrizaje Lunar | distancia media al lugar justo de la recta |
| Lenguaje | Lluvia de meteoros | palabras reales reconocidas |

Depende de: (1) Secuencia y Parejas con la dificultad común; (2) Secuencia reestructurada (mundo propio, símbolos
neutros, medida al final). El inicio nuevo se programa después de esas dos tareas.

## Accesibilidad de color (nuevo)

La pregunta 9 guarda una preferencia (`color_vision`: normal / dificultad / no sé). Con «dificultad»: paletas seguras
para daltonismo donde existan y, en el futuro, la opción de ocultar juegos (idea de Peak/Lumosity, `docs/analisis-competencia.md` §9).
Nueva preferencia = decidir su respaldo en `BackupRulesTest` (es progreso de configuración: va al respaldo).

## Ficha técnica (programado el 3-oct, tarea 21b)

**Archivos**
- `data/FirstFlight.kt`: lógica pura. `FlightStep` (los 19 pasos), `FlightState` (dónde va, respuestas, medidas, frases, juegos dejados para después),
  `FirstFlight` (`next`, `back`, `skipRemainingGames`, `withResult`, `minutesLeft`, `progress`, `startingPoint`), `FlightCopy` (textos por juego),
  `CoachTone`, `ColorVision` y `FlightStore` (disco). Dos formas: `FlightMode.FULL` (la primera vez) y `FlightMode.GAMES` («Hacer la evaluación» desde Avance/Hoy:
  solo los 4 juegos, sin preguntas, y al final «Listo»).
- `data/Baseline.kt`: `BaselinePlan.steps` = 4 juegos, uno por área (Rastro de luz, Freno, Aterrizaje, Meteoros). `buildBaseline`/`seedRatings` siembran el rating
  de cada uno y la estimación de su área para los demás juegos. `data/FirstData.kt`: primer dato de los 4 (Rastro: «Repetiste bien un rastro de N luces.»;
  Aterrizaje: «En promedio, aterrizaste a un X % de distancia del lugar justo.», X sin decimales).
- `ui/screens/FirstFlightScreen.kt` (pantallas), `FirstFlightHost.kt` (cielo + pantalla + `UnityGameHost` cuando toca jugar). `MainActivity` lo muestra mientras
  `ageBand == null` **o hay un recorrido guardado** (`flight != null`). Borrados: `OnboardingScreen`, `BaselineScreen` y lo de la evaluación en `GameSessionStore`.
- `NeuroVidaViewModel`: `flight`, `flightContinue/Back/Play/SkipGames/ConfirmReminder/Set*`, `startBaseline()` (evaluación repetida), `finishFlight()`.
  `ActiveGameSession.tutorial` → `forceTutorial` del lanzador: el inicio completo SIEMPRE abre el tutorial guiado (con `assessment` = versión corta).

**Preferencias**
- `first_flight` (nueva, `FlightStore`): el recorrido en curso como JSON. Estado PASAJERO: va a `transient` en `BackupRulesTest` (no al respaldo).
- `result_focus` (avance / consejo) y `color_vision`: dentro de `profile_extra`, que YA va al respaldo (son configuración de la persona). Valores por defecto: avance y «no sé».
  Editables en Ajustes. `result_focus` reemplazó a `coach_tone` el 3-oct (tarea 21c): se MIGRA una sola vez (celebrar → avance, claro → consejo) y la clave vieja se borra
  (`ResultFocus.migrate`, `NeuroVidaRepository.loadResultFocus`). Cambia el orden de `GameResultScreen`: el bloque «Un consejo para la próxima» (`data/ResultAdvice.kt`: el «Truco: …» de
  cada medida, que ya no se repite dentro de ella) va antes o después de las medidas y de lo que subió; los juegos sin consejo propio (Freno, Satélites, Radar…) se ven igual con las dos
  opciones. Con consejo hoy: Rastro de luz, Aterrizaje, Meteoros, ¿Verdad o disparate?, La estrella intrusa, Rumbo a Casa y Correo Estelar (Bitácora y Cosecha lo llevan dentro de su texto).
  Nubi vuelve a UN solo tono, cálido y sin culpa, en veredictos (`ResultPhrases.feedback`) y recordatorios (`buildReminder`): se quitaron las variantes de tono.
  `color_vision` por ahora solo se guarda.
- `education`: ya no se pregunta; se edita en Perfil (opcional) y conserva el valor de quien ya lo tenía.

**Cómo se retoma**: cada cambio de `FlightState` se guarda con `commit()`. Un ViewModel nuevo lee `FlightStore` al crearse; si Android mató la app durante un juego,
el resultado vuelve por la vía de siempre (`GameSessionStore` + `processPendingResult` → `onUnityResult` → `onFlightResult`) y lleva a la tarjeta de ese juego.
Salir de Unity sin terminar deja el paso del juego para volver a tocar «Jugar». El rango de edad se guarda en el perfil al elegirlo (los juegos y el avance lo leen de
ahí); el resto (nombre, días, metas…) en el perfil al pasar «Tu punto de partida». El recorrido termina (y se borra del disco) con «Empezar mi camino».

**Reglas de navegación**: «Atrás» vuelve un paso y nunca cae en un paso de juego (para repetirlo se toca «Jugar»); un juego ya medido no se repite;
«Terminar después» deja ESE juego y los que quedan para estimar (lo medido se conserva), el recorrido sigue por las preguntas que faltan y, si no se midieron
los 4, queda el punto de partida estimado y la evaluación se ofrece de nuevo en Avance.

**Pruebas**: `FirstFlightTest` (lógica), `flow/FirstFlightFlowTest` (recorrido completo, terminar después, muerte del proceso en una pregunta y en el juego 3, resultado
pendiente, evaluación repetida), `BaselineTest`, `FirstDataTest`, `ResultPhrasesTest`, `ReminderContentTest`, `BackupRulesTest`. Capturas Roborazzi:
`FirstFlightScreenshotTest` → `docs/previews/inicio-real-*.png`.

**Probarlo**: Ajustes → herramientas de depuración → «[Debug] Repetir el inicio (sin borrar datos)». Para la muerte del proceso: Opciones de desarrollador →
«No conservar actividades», o cerrar la app desde recientes en medio de un juego.
