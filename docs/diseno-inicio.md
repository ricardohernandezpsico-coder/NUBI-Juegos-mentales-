# Primer vuelo con Nubi — el inicio nuevo (aprobado por Ricardo el 3-oct)

Reemplaza al onboarding actual (`OnboardingScreen`, 9 pantallas de preguntas) y a la evaluación aparte (`BaselineScreen`,
`data/Baseline.kt`: Secuencia, Tinta o Palabra y Comparación, que medían solo 2 de las 4 áreas). Maqueta jugable aprobada:
https://claude.ai/artifact/H2edrC6krMV9mAqLCxuiwa (copia local de la maqueta en `docs/previews/inicio-maqueta.html`).
Ricardo dio libertad para sumar ideas; las que agregué después de la maqueta están marcadas con **(nuevo)**.

## La idea

Se juega desde el primer minuto. Cada juego enseña mientras se juega (la primera ronda es guiada) y mide el punto de
partida de su área; las pocas preguntas necesarias van entre juego y juego, de un toque y con su porqué. Cuatro juegos cortos.
Inspirado en lo observable del inicio de Lumosity (capturas en `Proyectos/pantallazos Lomosity/`): jugar pronto, tutorial
dentro del juego con «Saltar tutorial», tarjeta «Acabas de usar tu {capacidad}» (antes «X puso a prueba tu Y»: igual a la de Lumosity), preguntas intercaladas y barra de avance. Nada
copiado: nombres, textos, arte y juegos propios.

## Recorrido

1. **Nubi se presenta**: «Vamos a encontrar tu punto de partida jugando. Son 4 juegos cortos.» (desde el 18-oct, Tarea 132: antes decía «Son unos 5 minutos», una promesa de tiempo)
2. **¿Cómo te llamo?** (opcional).
3. **Juego 1 · Memoria**: Secuencia Lumínica renovada (ver «Juegos del inicio»). Tarjeta de entrada con Nubi maestra →
   ronda guiada → rondas que miden → tarjeta «Acabas de usar tu memoria de trabajo» con el primer dato en
   palabras y una línea de para qué sirve esa capacidad en la vida diaria. **(Tarea 132, 18-oct) Va ANTES de la edad y de las metas**: se juega a los pocos segundos de abrir la app.
4. **Rango de edad** (después de la tarjeta del juego 1; se salta si ya se sabe): ajusta ritmo y tamaño (no la dificultad).
5. **¿Qué te gustaría entrenar?** (hasta 3 áreas).
6. **Nubi: un dato** («poco y seguido rinde más»: práctica distribuida; sin promesas de mejora).
7. **Juego 2 · Atención**: Freno de Emergencia.
8. ~~**¿Te cuesta distinguir algunos colores?**~~ **QUITADA el 19-oct (Tarea 143, decisión de Ricardo R4):** se guardaba y no cambiaba nada en los juegos; una pregunta que no hace nada es una promesa a medias y un paso más para una persona de 60+.
   El ajuste sigue en Ajustes («¿Te cuesta distinguir algunos colores?», con la nota «Solo se guarda: hoy no cambia nada en los juegos»). Ver «Accesibilidad de color».
9. **Juego 3 · Razonamiento**: Aterrizaje Lunar.
10. **¿Cuántos días por semana?** (3 / 4 recomendado / 5 / todos).
11. **Juego 4 · Lenguaje**: Lluvia de meteoros.
12. **Tu punto de partida**: las 4 áreas con su etapa (Inicio…Maestro), «tu fuerte hoy» y «donde más vamos a jugar»;
    «No es un examen: es desde dónde partimos». **(nuevo) Nace tu primera estrella**: Nubi celebra y en el cielo aparece la
    primera estrella (la historia de Nubi: «cada partida hace nacer una estrella»). Se enciende el día 1 de la racha.
13. **¿A qué hora te aviso?** (aquí se pide el permiso de notificaciones de Android 13+).
14. **Tu camino de hoy** (el cierre): la ISLA de Hoy (el mismo render, con Nubi; Tarea 145, antes Nubi y dos líneas con la mitad de la pantalla vacía) con «Tu primer vuelo fue tu camino de hoy» y «Mañana empieza tu camino de 3 juegos…» → «Empezar mi camino» entra a Hoy. La isla toma lo que sobra de alto (mínimo 170 dp; con letra muy grande la página se desliza).

Salen del inicio: nivel de estudios (pasa a Perfil, opcional: hoy no cambia nada del juego), «Así funciona» (cada cosa
se explica cuando aparece por primera vez) y, desde el 10-oct (Tarea 78), la pregunta «Al terminar cada juego, ¿qué te sirve más ver primero?» («Lo que avancé» / «Un consejo para la próxima»;
puesta el 3-oct, tarea 21c): con los finales con sentido de los 18 juegos, que tienen UN solo orden y traen su truco dentro, no cambiaba nada de lo que se ve. El recorrido quedó en 18 pasos (la lista tiene 14 puntos porque cada juego va junto con su tarjeta).

## Reglas

- **Jugar en menos de un minuto**: antes del primer juego solo el nombre (opcional). Desde la Tarea 132 (18-oct) la edad se pregunta DESPUÉS de la tarjeta del juego 1, y solo si no se sabe.
- **Farolas de etapa** arriba en todo el recorrido (Tarea 132, en lugar de la barra de avance): **Empezar · Memoria · Atención · Razonamiento · Lenguaje · Tu punto de partida**, con la farola del kit (la de Hoy, Avance y Ajustes); la primera ya
  está encendida (abrir Nubi es el primer paso) y la actual lleva el aro lima. Debajo, en palabras: «Empezamos juntos» (Tarea 145; antes «Ya diste el primer paso», que afirmaba algo que la persona todavía no había hecho) y luego «Paso 2 de 6 · Memoria» … «Paso 6 de 6 · Tu punto de partida». **Ya no se dice «faltan unos N minutos»** (una promesa
  de tiempo que en una persona de 60+ no se cumplía). El indicador de 4 pasos de los juegos (✓ ✓ ● ○) salió el 18-oct (decisión de Ricardo): dos indicadores de avance apilados confunden a los 60+ y las farolas ya dicen en qué juego va (etapas 2 a 5 = juegos 1 a 4;
  en la evaluación repetida, «Juego 2 de 4 · Atención»). El saludo dice «Son 4 juegos cortos.» en vez de «Son unos 5 minutos.».
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
  «quitar animaciones» respetado (las farolas se encienden de una vez y los cambios de pantalla van con fundido).

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

## Accesibilidad de color

**Desde el 19-oct (Tarea 143, R4) el Primer vuelo ya no pregunta por los colores** (el paso `COLOR` se quitó de `FlightStep`; el recorrido tiene 17 pasos). La preferencia `color_vision` (normal / dificultad / no sé) se elige en Ajustes y SOLO se guarda:
hoy no cambia nada en los juegos (los que dependen del color se distinguen por forma, lugar o texto; las tintas de Tinta o Palabra están verificadas para daltonismo para todos). Si algún día hace algo (paletas seguras donde existan, la opción de ocultar juegos,
idea de Peak/Lumosity, `docs/analisis-competencia.md` §9), la pregunta puede volver al recorrido. Nueva preferencia = decidir su respaldo en `BackupRulesTest` (es progreso de configuración: va al respaldo).

## Ficha técnica (programado el 3-oct, tarea 21b)

**Archivos**
- `data/FirstFlight.kt`: lógica pura. `FlightStep` (los 18 pasos), `FlightState` (dónde va, respuestas, medidas, frases, juegos dejados para después),
  `FirstFlight` (`next`, `back`, `skipRemainingGames`, `withResult`, `minutesLeft`, `progress`, `startingPoint`), `FlightCopy` (textos por juego),
  `ColorVision` y `FlightStore` (disco). Dos formas: `FlightMode.FULL` (la primera vez) y `FlightMode.GAMES` («Hacer la evaluación» desde Avance/Hoy:
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
- `color_vision`: dentro de `profile_extra`, que YA va al respaldo (es configuración de la persona). Valor por defecto: «no sé». Editable en Ajustes; ya no la fija el Primer vuelo. Solo se guarda.
  `result_focus` (avance / consejo; reemplazó a `coach_tone` el 3-oct, tarea 21c) **se quitó el 10-oct (Tarea 78)**: el ajuste de Opciones, la pregunta del inicio, `ResultFocus` y el bloque
  «Un consejo para la próxima» de `GameResultScreen` ya no existen. Lo que haya quedado guardado en `profile_extra` (`result_focus` o el viejo `coach_tone`) no se borra: simplemente ya no se lee ni se
  migra. De `data/ResultAdvice.kt` queda solo lo que usan los finales con sentido: separar el «Truco: …» de una frase de medida (`body` y `tipOf`). Un recorrido guardado en el paso viejo
  (`ENFOQUE`, o `COLOR` desde el 19-oct) sigue en el paso que viene después de la tarjeta del segundo juego (`JUEGO_3`, o el que siga si ese ya se jugó o se dejó para después) al leerlo (`FlightStore.decode`); la clave `color` guardada se ignora.
  Nubi vuelve a UN solo tono, cálido y sin culpa, en veredictos (`ResultPhrases.feedback`) y recordatorios (`buildReminder`): se quitaron las variantes de tono.
- `education`: ya no se pregunta; se edita en Perfil (opcional) y conserva el valor de quien ya lo tenía.

**Cómo se retoma**: cada cambio de `FlightState` se guarda con `commit()`. Un ViewModel nuevo lee `FlightStore` al crearse; si Android mató la app durante un juego,
el resultado vuelve por la vía de siempre (`GameSessionStore` + `processPendingResult` → `onUnityResult` → `onFlightResult`) y lleva a la tarjeta de ese juego.
Salir de Unity sin terminar deja el paso del juego para volver a tocar «Jugar». El rango de edad se guarda en el perfil al elegirlo (los juegos y el avance lo leen de
ahí; desde la Tarea 195 son siete franjas —13 a 17 · 18 a 29 · 30 a 44 · 45 a 54 · 55 a 64 · 65 a 74 · 75 o más— y se guardan las DOS cosas: la franja fina en `profile_extra` (`age_range`) y la banda de tres del DDA que sale de ella; las opciones miden 52 dp en esa pregunta, `AGE_OPTION_MIN_HEIGHT`); el resto (nombre, días, metas…) en el perfil al pasar «Tu punto de partida». El recorrido termina (y se borra del disco) con «Empezar mi camino».

**Reglas de navegación**: «Atrás» vuelve un paso y nunca cae en un paso de juego (para repetirlo se toca «Jugar»); un juego ya medido no se repite;
«Terminar después» deja ESE juego y los que quedan para estimar (lo medido se conserva), el recorrido sigue por las preguntas que faltan y, si no se midieron
los 4, queda el punto de partida estimado y la evaluación se ofrece de nuevo en Avance.

**Pruebas**: `FirstFlightTest` (lógica), `flow/FirstFlightFlowTest` (recorrido completo, terminar después, muerte del proceso en una pregunta y en el juego 3, resultado
pendiente, evaluación repetida), `BaselineTest`, `FirstDataTest`, `ResultPhrasesTest`, `ReminderContentTest`, `BackupRulesTest`. Capturas Roborazzi:
`FirstFlightScreenshotTest` → `docs/previews/inicio-real-*.png`.

**Probarlo**: Ajustes → herramientas de depuración → «[Debug] Repetir el inicio (sin borrar datos)». Para la muerte del proceso: Opciones de desarrollador →
«No conservar actividades», o cerrar la app desde recientes en medio de un juego.

## El juego primero y las farolas de etapa (Tarea 132, 18-oct)

Pedido de Ricardo (18-oct; spec `auditoria-fable/spec-adopcion-A1-A7.md` §A2 y §A4): jugar antes de contestar y no prometer minutos.

- **Orden nuevo** (`FlightStep`, 18 pasos hasta el 18-oct; 17 desde el 19-oct, sin la pregunta de los colores): Hola · Nombre · **Juego 1 · Tarjeta 1 · Edad** · Metas · Dato · Juego 2 … `FlightStore` guarda y lee el paso por NOMBRE, así que un recorrido guardado con el orden viejo sigue leyéndose. `next()` y `back()` se saltan `EDAD` cuando
  `age != null` (la edad se pregunta una sola vez; después se cambia en Ajustes, «Rango de edad»).
- **El juego 1 sin edad:** corre con la edad que haya en el perfil (`FirstFlightHost`: `state.age ?: settings.ageBand ?: ADULT`), o sea como adulto (objetivo 80 %), con el arranque suave: `BaselinePlan.startLevel(null) = 2` (empezar fácil no daña a nadie; el DDA sube en 2-3 ensayos).
  Al saberse la edad no hay nada que recalcular: lo medido es el rating propio del juego y la edad entra al leer (`Skill.progress`, `FirstFlight.startingPoint`). Nota de serie: `docs/medidas-juegos-estrella.md`, § «Rastro de luz».
- **Si la persona sale a mitad:** de Unity sin terminar el juego 1, sigue en `JUEGO_1` con «Jugar»; si Android cierra la app en el juego 1, el resultado vuelve por `GameSessionStore` → tarjeta 1 → edad; si la cierra en la edad, vuelve en la edad (con el juego 1 ya medido).
- **Farolas** (`FirstFlight.stations` / `stationText`, `FlightStations` en `FirstFlightScreen.kt`): seis de 40 dp (las cajas se achican en pantallas angostas), un fundido de `NubiMotion.STATE_MS` al encender (un salto con «quitar animaciones»), sin sonido ni vibración. Una farola de área se enciende cuando su juego se dio
  (se jugó o se dejó «para después»: la etapa se pasó y nada se castiga). **La etapa que se muestra es la primera que aún no se dio**: con «Terminar después» en el juego 1 (los 4 juegos para después), la edad, las metas, el dato y los días ya dicen «Paso 6 de 6» (a secas: las pantallas que no son de juego no nombran un área, Tarea 143, A2; antes «Paso 6 de 6 · Tu punto de partida»), y la etapa nunca salta de la 4 a la 6 mientras queda una por dar, la última cuando el punto de partida se guardó: como todo sale de lo ya medido, volver atrás nunca apaga una. La evaluación repetida («Hacer la evaluación» desde Avance) lleva cinco: los cuatro juegos y «Listo»
  («Juego 2 de 4 · Atención»). Para el lector de pantalla son un grupo: «Paso 2 de 6 · Memoria. 2 de 6 farolas encendidas» (en un juego o su tarjeta; en una pregunta, «Paso 2 de 6. 2 de 6 farolas encendidas»). `minutesLeft` y `progress` quedan en `FirstFlight.kt` (las usan las pruebas) pero ya no se muestran.
- **Pruebas:** `FirstFlightTest` (orden, edad una sola vez, orden viejo, farolas, textos), `BaselineTest` (arranque suave), `FirstFlightFlowTest` (los recorridos con el ViewModel, también la app cerrada en el juego 1 y en la edad), `FirstFlightLampsTest` (360 dp con letra 2,0, sin «minutos») y la hoja
  `docs/previews/primer-vuelo-orden.png` (`FirstFlightOrdenScreenshotTest`, `tools/previews/primer_vuelo_orden.py`).
