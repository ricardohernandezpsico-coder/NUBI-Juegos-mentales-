# DDA común de Nubi (dificultad adaptativa)

Estado: implementado en Unity el 24-sep-2026 (`Assets/Scripts/Games/AdaptiveDifficulty.cs`), conectado a
Stroop, Ruta del Tesoro, «Carga exacta» (antes Cálculo Sereno; id `calculo`) y «En la punta de la lengua» (antes Anagramas; y Cambio de Chip, Comparación Instantánea y Detective de Series, retirados el 3 y el 4-oct: ver `docs/juegos/descartados.md`). El 3-oct
se sumaron Secuencia Lumínica (hoy «Rastro de luz») y Parejas Ocultas (ver §6): **los 20 juegos usan el motor común** (Piloto y Correo
con dos instancias; Freno solo en la tarea de ir).

> **Alcance y honestidad**: esto es un diseño de ingeniería inspirado en literatura psicométrica y de
> entrenamiento cognitivo. **No está validado clínicamente**, no es una herramienta diagnóstica y sus
> parámetros (pasos, umbrales, objetivos) son valores iniciales razonables que deben calibrarse con datos
> reales de uso. Lumosity, Peak y Elevate no publican sus algoritmos exactos; lo que se toma de ellos es lo
> observable y general (dificultad que se adapta por habilidad, seguimiento por dominio, sesiones cortas
> y diarias, retroalimentación inmediata, puntajes comparables entre juegos).

## 1. Qué problema resuelve

Antes había tres lógicas distintas: `SequenceDDAEngine` (ventana de 3 ensayos, borrado el 26-sep),
`VisualWorkingMemoryDDA` (controlador continuo hacia ~80% con Z-score de tiempo de reacción; borrado el 3-oct) y,
en los otros 7 juegos, contadores
improvisados ("cada N aciertos sube un nivel", "2 errores bajan uno"). Los contadores tienen tres defectos:
no apuntan a una tasa de aciertos concreta, no usan la edad ni el tiempo de reacción, y producen saltos
bruscos. Además el único dato entre sesiones era `masteryStreak` (un entero).

## 2. Fundamentos

| Idea | Fuente | Cómo se usa |
|---|---|---|
| El aprendizaje óptimo ocurre con ~85% de aciertos (error ~15%) | Wilson, Shenhav, Straccia & Cohen (2019), *The Eighty Five Percent Rule for optimal learning*, Nature Communications 10:4646 | Objetivo de aciertos: 0.85 en adultos mayores, 0.80 en el resto (algo más conservador que el 85% teórico para mantener motivación) |
| Métodos adaptativos "up-down" convergen a un porcentaje de aciertos | Levitt (1971), *Transformed up-down methods in psychoacoustics*, JASA 49:467 | Base del control por escalones |
| Up-down **ponderado**: paso de bajada = paso de subida × p/(1−p) converge a la tasa p | Kaernbach (1991), *Simple adaptive testing with the weighted up-down method*, Perception & Psychophysics 49:227 | Regla central: sube δ por acierto, baja δ·p/(1−p) por error (con p=0.8: cuatro veces) |
| Estado de "flow": desafío ajustado a la habilidad | Csikszentmihalyi (1990), *Flow* | Ni aburrimiento (bono cada 6 aciertos seguidos) ni frustración (red anti-frustración) |
| Zona de desarrollo próximo | Vygotsky (1978) | Justifica presentar siempre un nivel apenas por encima de lo dominado |
| Aprendizaje sin error reduce frustración en personas con memoria comprometida o mayores | Baddeley & Wilson (1994), *Neuropsychologia* 32:53 | Objetivo de aciertos más alto y subida más lenta en `Senior`; al fallar se muestra la respuesta correcta |
| Dificultades deseables: algo de error favorece el aprendizaje | Bjork (1994) | Por eso el objetivo no es 100% de aciertos |
| Carga cognitiva | Sweller (1988) | Escalera gradual: cada nivel introduce pocas familias nuevas a la vez |
| Enlentecimiento del procesamiento con la edad | Salthouse (1996), *Psychological Review* 103:403 | El tiempo de reacción pesa menos en `Senior` (0.15) que en `Adult` (0.35) y `Under18` (0.40) |

## 3. Algoritmo (`AdaptiveDifficulty`)

Estado: `Rating` continuo en `[1, MaxLevel+0.99]` sobre la escalera del juego. `Level = floor(Rating)`.

Por cada ensayo `Register(correct, reactionMs)`:

- **Acierto**: `Rating += δ · calibración · modulación_RT (+ bono)`
  - `δ` = `stepUp` del juego (0.12–0.5 niveles) × factor de edad (Senior ×0.85, Under18 ×1.1).
  - `calibración` = ×1.5 en los primeros 6 ensayos (rating provisorio: converge más rápido al inicio).
  - `modulación_RT` = `1 + w_RT · clamp(−z, −1, 1)`, con `z` = Z-score del tiempo de reacción contra la
    historia del propio usuario (Welford en línea, desvío mínimo 150 ms). Solo en modos con reloj.
  - `bono` = +0.20 cada 6 aciertos seguidos (anti-aburrimiento).
- **Error**: `Rating −= min(δ · p/(1−p), 1.0) · calibración`; si es el 2.º error seguido, −0.15 extra y
  `Struggling = true` (los juegos muestran "Con calma · Ajustamos la dificultad").
- **Calentamiento**: los 2 primeros ensayos se presentan un nivel por debajo (`PresentedLevel`).
- **Fracción** (`Rating − floor`) queda disponible para parámetros continuos (hoy no usada).

Punto de partida: `StartRating(nivelElegido 1-5, MaxLevel, base_intensity)` reparte el nivel elegido por la
app sobre toda la escalera del juego y suma hasta +1.2 niveles según la maestría acumulada.

Parámetros por juego:

| Juego | Escalera | stepUp Reto / Precisión | Objetivo | Usa RT |
|---|---|---|---|---|
| Stroop («Dos orillas», 3-oct: reglas, % que chocan y llegada por nivel en [diseno-tinta-o-palabra.md](diseno-tinta-o-palabra.md) §5) | 5 | 0.12 / 0.20 | edad | Reto |
| Carga exacta (id `calculo`, 4-oct: [diseno-carga-exacta.md](diseno-carga-exacta.md) §5) | 5 | 0.40 | edad | no (con pista cuenta como medio acierto) |
| En la punta de la lengua (id `anagramas`, 3-oct) | 5 | 0.40 | edad | no |
| Ruta del Tesoro | 12 | 0.50 (por ruta) | 0.70 (perder la ruta cuesta una vida) | no |
| Parejas Ocultas | 10 | 0.15 | edad | Reto |
| Rastro de luz (id `secuencia`) | 16 | 0.25 adultos · 0.17 mayores · 0.22 menores de 18 (ver §6) | edad | no |

## 4. Verificación

`AdaptiveDifficultyTests` (13 pruebas) incluye simulaciones con un usuario logístico: con habilidades 3, 5 y
7 la tasa de aciertos converge a 0.72–0.88 (objetivo 0.80) y el nivel de equilibrio queda cerca de
`habilidad − ln(4)/pendiente`; mayor habilidad termina en mayor nivel; `Senior` apunta a más aciertos que
`Adult`; la razón bajada/subida es p/(1−p); el calentamiento, la red anti-frustración, la modulación por
tiempo de reacción y el bono anti-aburrimiento se prueban aisladamente.

## 5. Persistencia entre sesiones (hecho el 24-sep)

`StroopSessionMetrics` (telemetría común de los juegos por ensayos; Secuencia y Parejas tienen la suya con los
mismos campos, ver §6) lleva `end_rating` (0..1) y `peak_level`.
`NativeReceiver` los recoge y `recordGameResult` guarda el rating por juego en Room (columna `ddaRating`,
esquema v11), suavizado 60% partida / 40% anterior (`blendDdaRating`) para que un mal día no tire el progreso.
Al abrir un juego, `UnityGameLauncher` envía el rating guardado y `AdaptiveDifficulty.StartRating(config, max)`
continúa desde ahí; sin dato, se usa el nivel elegido más la maestría (`masteryStreak`), como antes.
Pendiente: mostrar el rating en la pantalla de Progreso.

## 6. Secuencia y Parejas en el motor común (3-oct)

Hasta el 2-oct tenían motor propio (Secuencia: escalera de 16 niveles que subía con 2 aciertos seguidos; Parejas:
`VisualWorkingMemoryDDA`, un controlador continuo D(t)). Hoy los dos usan `AdaptiveDifficulty` como los demás:
objetivo por edad (0,85 mayores, 0,80 resto), peso del tiempo por edad (`DdaUserProfileConfig`), calentamiento de 2
ensayos, techo y piso de los modos Suave / Desafío / Experto (`ConfigureMode`), calibración rápida en la
evaluación, rating guardado entre sesiones y `end_rating` en la telemetría. `VisualWorkingMemoryDDA` y sus pruebas
se borraron. Lo que ve la persona (arte, mecánica, textos, regla «tablero completo a la vez») no cambió, salvo lo
que se dice abajo.

**Parejas Ocultas** (`CardsGameContract`): escalera de **10 niveles** (los mismos 10 tableros de siempre:
2, 3, 4, 5, 6, 7, 8, 9, 10 y 12 parejas; `Skill.kt` ya decía 10).
- Cada nivel fija las parejas, la grilla (`CardsBoardProfile.GridDimensionsFor`) y el banco de símbolos
  (interferencia: niveles 1-3 → banco 0, 4-6 → 1, 7-9 → 2, 10 → 3).
- Ensayo = cada pareja intentada: `Register(acierto, tiempoMs)`, con el tiempo entre la primera y la segunda carta
  (solo pesa en Reto: sin reloj no se usa). Se acabó el tiempo = error (`Register(false)`).
- El nivel del tablero se fija al armarlo con `PresentedLevel` (y el calentamiento): dentro de un tablero no cambia;
  el siguiente toma el nivel que dejó el motor. Antes cada falla de nivel bajaba o repetía el nivel a mano; ahora
  la falla (3 parejas erradas cortan el tablero) solo cuenta para las vidas y el aviso dice «Bajamos de nivel»
  solo si el motor de verdad lo bajó (si no, «Repetimos el nivel»).
- Lo que antes afinaba D(t) dentro del nivel (vista previa de 3 s a 0,5 s según la edad, distractores de fondo y su
  opacidad) usa las mismas fórmulas con el rating continuo (`RatingNormalized`) como índice.
- Fin de partida: 3 tableros cortados seguidos o **10 tableros jugados** (`BoardsPerSession`). Antes terminaba al
  superar el nivel 10; como ahora el motor decide el nivel, hace falta ese límite para que no sea infinita.
- Parte del rating guardado o, sin dato, del nivel elegido (antes siempre empezaba en el tablero de 2 parejas).
- `stepUp` 0,15 por pareja (≈7 aciertos por nivel; la bajada por error es 0,60 con objetivo 0,80).

**Rastro de luz** (id `secuencia`; antes Secuencia Lumínica; `RastroContract` + `RastroLadder`, rehecha el 3-oct, `docs/diseno-rastro-de-luz.md`):
los **16 niveles** de `RastroLadder` son la escalera (qué familias hay, cuántas luces pide cada una, velocidad de la chispa, espera, giro y
cruces; misma escala que la Secuencia anterior, así que el rating guardado de 0 a 1 conserva su sentido).
- Ensayo = cada ronda completa (acierto si repite entero el camino). **Tiempo de reacción: no se usa** (`useReaction: false`): es capacidad de
  memoria, no velocidad.
- **`stepUp` por edad** (`RastroContract.StepUp`): adultos **0,25** (con objetivo 0,80 baja 1,0 por error, justo el tope `MaxDropPerError`),
  mayores **0,17** y menores de 18 **0,22**. El motor multiplica el paso por 0,85 en mayores (y 1,1 en menores de 18) y baja `paso · p/(1−p)` por
  error con tope 1,0: con 0,25 en mayores bajaba 1,0 por error (el tope se activaba) y el equilibrio quedaba en ≈0,82; con 0,17 baja 0,82
  y converge a ≈0,85 (simulación con un usuario logístico: mayores 0,82 → 0,84 medido, adultos 0,79; la diferencia con el 0,85 teórico la
  ponen el bono anti-aburrimiento y la bajada extra por dos errores seguidos, que son comunes a todos los juegos). La prueba
  `StepUp_NeverHitsTheDropCap_SoTheTargetIsReached` fija que el tope no se active y `Simulation_Senior_ConvergesTowardTheTarget0_85` lo mide.
- **Vidas**: 3, solo terminan la partida; no deciden la dificultad.
- Arranque: el rating guardado; sin dato (primera vez) y en la **evaluación inicial**, nivel 2 (1 en mayores), solo el rastro simple, hasta
  2 errores o ~75 s; la medida es `end_rating` (lo lee `data/Baseline.kt`).
- La **ronda guiada del tutorial** (`RastroSession.GuidedRound`) no pasa por el motor: no es un ensayo, no mueve el rating ni las vidas ni la
  telemetría (`TheGuidedRound_LeavesNoTraceInTheDdaTheTallyOrTheLives`).
- Telemetría: `SequenceTelemetry` conserva `end_rating`, `peak_level`, `mode_trials` y `mode_hits` y agrega `ras_*` (ver la ficha técnica del documento).

**La app**: `OWN_ENGINE_GAMES` se borró (el repositorio siembra y retoma el rating de los dos como el de todos);
`Skill.kt` quitó `ownTarget` de Secuencia (el objetivo es el de la edad; Ruta del Tesoro queda con 0,70) y la sumó
a los juegos de rondas largas (un Desafío pide 6 rondas, no 12 ensayos). `parseCardsResult` lee `end_rating`,
`peak_level` y los ensayos del modo.

**Datos guardados de antes**: no hace falta convertir nada.
- Secuencia ya guardaba `ddaRating` 0..1 sobre los 16 niveles (`(pico − 1) / 15`); el motor nuevo usa
  `(rating − 1) / 16`: la misma escala, con diferencia de a lo más un nivel y medio en el extremo. Eso sí: el
  valor viejo era el nivel MÁS ALTO alcanzado (por encima del nivel de 8 de 10) y el nuevo es el nivel de equilibrio,
  así que la primera partida lo corrige hacia abajo (el suavizado es 60 % partida / 40 % anterior y el avance no
  baja más de 5 puntos por partida, `Skill.MAX_DROP`).
- Parejas nunca guardó rating (su telemetría no traía `end_rating`; `ddaRating` quedó en −1): la primera partida
  nueva lo crea y, hasta entonces, parte del nivel elegido más la maestría.

**Pruebas**: `CardsDifficultyTests` y `RastroLadderTests` / `RastroSessionTests` (EditMode): escalera y niveles, convergencia
al objetivo con un usuario simulado, techo y piso de modo, partida desde un rating guardado, el error por tiempo
agotado, la evaluación y `end_rating` en la telemetría. Kotlin: `MemoryTelemetryTest` (lectura con y sin
`end_rating`) y `MemoryGamesRatingTest` (guardado, suavizado y retomado del rating de los dos juegos).

## 7. Siguientes pasos sugeridos

1. (Hecho) Persistir el rating entre sesiones. Falta mostrarlo en Progreso.
2. Recoger datos reales (aciertos por nivel, tiempos) y calibrar `stepUp`, objetivos y ratio de calentamiento.
3. Puntaje normativo por percentil y edad (necesita una muestra; es lo que hacen las apps líderes para
   comparar entre juegos).
4. Considerar un modelo tipo Elo / teoría de respuesta al ítem por familia de ítem (no solo por nivel) si se
   quiere elegir cada ítem según su dificultad real medida.
