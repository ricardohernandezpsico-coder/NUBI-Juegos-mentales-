# El Contrarreloj, fase 1: «La escalera de 2 minutos» (26-oct-2026)

Un modo para **superarse uno mismo contra el tiempo**, como el Puzzle Rush de chess.com: 2 minutos fijos, la dificultad adaptativa apagada y una pista fija que parte siempre del paso 1 y sube un paso cada N aciertos. La marca es **«N aciertos en 2 minutos»**.
Lo pidió Ricardo (cuando dijo «Desafío» pensaba en esto, no en el «Con reloj»), lo diseñó Fable (`Proyectos/auditoria-fable/contrarreloj.md`, su lámina y el anexo `contrarreloj-cuarto-juego.md`) y Ricardo decidió el 26-oct. Esta es la **fase 1: local, sin servidor**. T176 = Unity (este documento, §2 a §8); T177 = la app (§9, pendiente).

## 1. Las decisiones de Ricardo (26-oct)

1. **Mecánica A, «Escalera de 2 minutos»:** 120 s fijos, dificultad adaptativa APAGADA, una PISTA FIJA con semilla = fecha + versión de la pista, siempre desde el paso 1, y sube un paso cada N aciertos.
2. **El mismo juego para todos cada día**, por rotación de la fecha (como Wordle), y la misma secuencia para todos ese día.
3. **La marca:** «N aciertos en 2 minutos»; el paso alcanzado va en una frase aparte. Se compara solo con tu récord, tus últimas 5 partidas y la mejor de la semana.
4. **Ícono:** el reloj del kit.
5. **Primera tanda:** Tinta o Palabra, ¿Verdad o disparate?, Rescate relámpago y **Acoplamiento** (no Carga exacta: a Ricardo no le gusta). Piloto queda para la fase 3.
6. **Contra el azar:** «pausa que crece al fallar» y un mínimo antes de poder responder en los juegos de dos opciones; prueba obligatoria con bots (§6).
7. En Hoy, el «Contrarreloj de hoy» **reemplaza** al «Desafío de hoy». El modo Desafío de la ficha NO se toca.
8. «¿Con reloj?» de la ficha **convive** en la fase 1 (es otra cosa); se irá en la fase 1b, cuando el Contrarreloj entre a la ficha.
9. Nada de comparar con otras personas ni de franjas nuevas en esta fase (§8).

## 2. La pista de cada juego (`Games/Shared/TimeTrial.cs`)

| Juego (id) | Pasos | Sube cada | El último paso llega al acierto | Versión de la pista | Pausa del error | Antes de responder |
|---|---|---|---|---|---|---|
| Tinta o Palabra (`stroop`) | 5 | 8 aciertos | 32 | 1 | 3 s ×1/×2/×3 | — |
| ¿Verdad o disparate? (`disparate`) | 12 | 3 aciertos | 33 | 1 | 3 s ×1/×2/×3 | 1,5 s (0,25 s por palabra si la frase es larga) |
| Rescate relámpago (`radar`) | 12 | 3 rondas logradas | 33 | 1 | — (la ronda dura lo mismo acierte o no) | — |
| Acoplamiento (`acoplamiento`) | 12 | 2 aciertos | 22 | 1 | 3 s ×1/×2/×3 | el puerto se abre a los 1,2 s |

- **Los pasos son los niveles de cada juego** (`StroopContract.MaxLevel` = 5, los otros 12): el paso 1 es el nivel 1 y la pista no baja nunca. Un error no suma, no resta y no baja de paso: cuesta solo su pausa. En el último paso la pista se queda.
- **Aciertos:** en Tinta, una palabra bien tocada; en ¿Verdad o disparate?, una frase bien decidida; en Acoplamiento, un módulo bien decidido; en Rescate, una **ronda lograda** (`RadarContract.Success`: todas las cápsulas, y con 4 una menos).
- **La pausa que crece:** 3 s al primer error, 6 s al segundo seguido, 9 s desde el tercero; un acierto la devuelve a 3 s. El error CUESTA su pausa en el reloj (en el Reto de siempre la pausa no le gasta tiempo). En Acoplamiento cuenta lo que ya duró la revelación (1,5 a 1,8 s): se espera lo que falta hasta los 3, 6 o 9 s.
- **El mínimo antes de responder** (`TimeTrial.MinThinkSeconds`): en ¿Verdad o disparate? los botones se ven apagados (45 %) hasta que la frase se lee (1,5 s, o 0,25 s por palabra si es más larga: una frase de 10 palabras, 2,5 s); en Acoplamiento, hasta los 1,2 s (el módulo termina de llegar y girar). Un toque antes no cuenta. El plazo de Acoplamiento por módulo (de 6 s a 2,5 s) sigue corriendo desde que llega.
- **Lo que el Contrarreloj apaga:** la dificultad adaptativa (no se llama a `Register`; `end_rating` sale sin tocar y `mode_trials`/`mode_hits` en 0); en ¿Verdad o disparate?, las Ráfagas, la señal que se apaga y el «evitar las frases de las últimas partidas» (ni guarda las suyas en esa lista); en Rescate, las lluvias de cápsulas (el viaje de la nave a la estación sigue sin descontar tiempo); en los dos con récord propio (Rescate y Acoplamiento), el Contrarreloj no toca `resc_best` ni `dock_best`.
- **El marcador:** el chip de la derecha dice «23 aciertos» y la fase de la fila de consigna, el tiempo («1:12»); la barra del reloj de cada juego baja de 120 s. La cuenta regresiva dice «Contrarreloj de 2 minutos».

## 3. La semilla: la misma pista para todos

- `TimeTrial.Seed(dayKey, gameId)` mezcla (SplitMix64) la **fecha** (`track_seed`, aaaammdd, p. ej. 20261026), la **versión de la pista** del juego y su **id**. Es pura y estable: el mismo día, juego y versión dan la misma pista en cualquier teléfono; mañana, otra.
- **Cada ensayo tiene su propia semilla** (`TimeTrial.TrialRng(seed, número, flujo)`), no un solo flujo que se va gastando: quien falla no desplaza lo que viene. El ensayo 12 usa el mismo azar para todos y solo su contenido depende del paso en que esté la persona (en Rescate y en Acoplamiento los generadores no guardan estado: el mismo paso y el mismo número, el mismo ensayo). En Rescate la espera del relámpago sale de otro flujo del mismo ensayo (la geometría de la pantalla cambia cuántos números gasta el acomodo de las cápsulas y no debe mover la espera).
- **`TrackRandom`**: un `System.Random` con algoritmo propio (SplitMix64), para que la pista no dependa de la implementación de `System.Random` de cada versión de Unity o .NET y un servidor la pueda reproducir (fase 2). Los generadores de los juegos solo usan `Next(n)` y `NextDouble()`. `TimeTrialTests` fija valores concretos (la semilla del 26-oct de Tinta es `2276549384711325806`).
- **Versión de la pista** (`TimeTrial.Track.Version`, hoy 1 en los cuatro): si cambian los pasos, los generadores o las pausas de un juego, se sube la versión: la semilla cambia y **el récord de la persona empieza de nuevo** (el viejo queda en el detalle, «tu récord de la pista 1»). La telemetría trae la versión con que se jugó (`tt_track_version`); la app guarda la partida con ella.

## 4. Lo que Unity recibe y devuelve

- **Entrada** (`SequenceConfigDetails`): `time_trial` (bool) y `track_seed` (la fecha como número). Con `time_trial` en un juego de la primera tanda, `timed` se fuerza a true. Una app vieja no los manda y juega como siempre. (Fable pensaba mandar también `track_version`; no hace falta y es más seguro que la versión la ponga solo Unity, junto al código de la pista: la app la lee de vuelta.)
- **Salida** (`StroopSessionMetrics`): `tt_hits` (la marca), `tt_errors`, `tt_peak_level` (el paso más alto: «Llegaste al paso 4 de 5»), `tt_seconds` (120) y `tt_track_version`; -1 en una partida de siempre. Además `correct_trials`/`total_trials` y la medida propia del juego (por ejemplo `sv_*`) salen como siempre.
- **La app no debe mover el nivel de la persona con un Contrarreloj:** `end_rating` es el de la partida anterior sin tocar y `mode_trials`/`mode_hits` van en 0; los récords de cápsulas y de módulos salen sin cambio (`resc_new`/`dock_new` en 0). T177 guarda el resultado en su propia tabla y no lo mezcla con la dificultad adaptativa ni con los puntos por área.

## 5. Las pruebas

- `TimeTrialTests` (18): la tabla de pistas y su orden de rotación, los pasos por aciertos (los números de Fable), la semilla (valores fijos), el azar propio, el azar por ensayo, **la misma pista para los cuatro generadores** (con el banco de frases real), la pausa que crece, el mínimo antes de responder, la cuenta de la partida, la config y la telemetría (JSON), los textos («23 aciertos», «1:12»).
- `TimeTrialFairnessTests` (5): ver §6.
- Smoke `Contrarreloj*` (una corrida por juego, en 20:9 y 16:9, con el reloj acortado a 16 s: `TimeTrial.SecondsOverride`, solo en el Editor): el juego se juega solo (acierta y de vez en cuando se equivoca, para pasar por la pausa del error), las guardias de siempre (textos fuera de lugar, nada tapa el marcador ni los botones), el chip dice «N aciertos» y la fase «m:ss», la partida llega a su final y **la telemetría trae la marca** (`tt_*` bien, el DDA sin tocar); en las demás corridas `tt_hits` dice -1.

## 6. La prueba del azar (obligatoria): adivinar no gana

`TimeTrialFairnessTests` juega la **misma pista del día 200 veces** con un bot de **azar** (responde apenas puede, sin mirar; en Rescate marca los tipos justos para lograr la ronda, la mejor jugada a ciegas) y 200 con uno de **cuidado** (acierta el 85 % y responde con el tiempo de una persona atenta: 0,85 s en Tinta, 2,4 s en ¿Verdad o disparate? y Acoplamiento, 2,2 s en Rescate, con dispersión). El tiempo de cada ensayo sale de las mismas constantes que usan los controladores y de la pista real (generadores sembrados, banco de frases real); lo único supuesto es cuánto tarda en responder una persona atenta (no son medidas). Se exige, **en los cuatro**:

- la media del azar menor que el **70 %** de la media del cuidado, y
- el **p90** del azar menor que la media del cuidado.

Resultado (pausa 3 s ×1/×2/×3; mínimo 1,5 s en ¿Verdad o disparate? y 1,2 s en Acoplamiento: los valores del anexo de Fable, sin ajustes):

| Juego | Cuidado (media) | Azar (media) | Azar (p90) | Azar (máx.) | Azar / cuidado |
|---|---|---|---|---|---|
| Tinta o Palabra | 54,8 | 5,5 | 10 | 16 | 10 % |
| ¿Verdad o disparate? | 30,4 | 13,3 | 19 | 28 | 44 % |
| Rescate relámpago | 12,6 | 1,2 | 3 | 6 | 10 % |
| Acoplamiento | 19,2 | 11,3 | 16 | 19 | 59 % |

Sin la pausa ni el mínimo (la pausa de hoy de cada juego; simulación con los mismos bots), adivinar GANA en ¿Verdad o disparate? (≈ 33 contra 33) y en Acoplamiento (≈ 20 contra 20): es lo que cierran las reglas de §2. **Acoplamiento es el margen más corto** (59 % contra el 70 % exigido: con 25 módulos en 2 minutos la suerte pesa); si alguna vez falla, se sube su pausa o su mínimo y se anota aquí. El informe se escribe en `unity/test-results/contrarreloj-azar.txt` cada vez que corre la prueba (y sale en el registro).

## 7. Honestidad y límites

- «Aciertos», «marca», «récord», «pista», «paso», «Contrarreloj»: una palabra por concepto; nunca «nivel» a la vista, «Reto» ni «Desafío» para esto. La nota del final: «Es el resultado de un juego, comparado solo contigo. No es una medida de capacidad.» (T177).
- La marca compara a la persona **con ella misma** (su récord, sus últimas 5 partidas, la mejor de la semana). `Features.COMPARE` sigue apagado.
- La pausa de 6 y 9 s al segundo y tercer error seguido es tiempo muerto: es el precio de que adivinar no gane (§6); un acierto la devuelve a 3 s. «Sin culpa»: no hay vidas ni fin anticipado.
- La prueba del azar usa tiempos de respuesta supuestos; la pista real se verá con personas.

## 8. Fase 2 (NO se construye ahora): las siete franjas de edad

Anotadas para cuando haya servidor, cuentas, consentimiento y abogado (Fable, §6 y §7 de su diseño). **No se cambia Ajustes, `AgeBand` ni la dificultad adaptativa** (sigue con sus tres franjas: Menos de 18 · 18 a 64 · 65 o más):

- **Para comparar, siete franjas:** 13 a 17 · 18 a 29 · 30 a 44 · 45 a 54 · 55 a 64 · 65 a 74 · 75 o más. Se pregunta la franja, no la fecha de nacimiento; una franja se muestra solo con 50 personas o más en ese juego esa semana. Menos de 13, fuera.
- El servidor volvería a jugar la pista de cada partida (semilla, versión y cada ensayo con su respuesta y su tiempo) para recalcular los aciertos: por eso `TrackRandom` es propio. Descartaría respuestas imposibles (menos de 150 ms seguidas) y limitaría las partidas que cuentan por día.
- Se dice como una de cinco divisiones con nombre («En la plataforma», «Despegando», «En órbita», «En la Luna», «Entre estrellas»), nunca «percentil 62».

## 9. La app (Tarea 177, 26-oct; fase 1, sin servidor)

**Lo construido** (detalle de pantallas en [`arquitectura-app.md`](arquitectura-app.md) § «El Contrarreloj de hoy»):
- `data/TimeTrialDay.kt`: el juego del día por rotación de la FECHA sobre los cuatro de la primera tanda (`GAMES[floorMod(epochDay, 4)]`: el mismo para todas las personas), la tabla de pistas (`TRACKS`: versión, paso y largo de cada juego; una prueba la compara con `TimeTrial.cs` y los contratos de Unity), la semilla (`track_seed` = la fecha local como número, aaaammdd), el estado de Hoy (`stateOf`: PENDIENTE · JUGADO · MARCA_NUEVA, DERIVADO de las partidas guardadas) y las frases de §9.8 con «Acoplamiento» en lugar de «Carga exacta».
- `data/TimeTrialStats.kt`: el récord, las últimas 5 partidas, la mejor de la semana (desde el lunes) y las frases del final, todo SOLO contra la misma persona y la pista de hoy (las de una pista anterior no cuentan). `Outcome` = la partida, si es la primera, si es marca nueva, el récord de antes.
- Room 16: tabla `time_trial_results` (`id`, `gameId`, `day`, `trackVersion`, `hits`, `errors`, `level`, `seconds`, `timestamp`), `Migration(15, 16)`, `MigrationTest`; va en el respaldo con la base entera. **No hay preferencia pasajera del día** (`desafio_hoy` y `DailyChallenge` se retiraron; el archivo viejo, si existe, queda sin uso: `BackupRulesTest`).
- Puente: la config lleva `time_trial` y `track_seed` (solo si el juego es de la primera tanda); Unity devuelve `tt_hits`, `tt_errors`, `tt_peak_level`, `tt_seconds` y `tt_track_version` (-1 = sin dato; una partida sin `tt_hits` no es un Contrarreloj). `GamePlayResult.isTimeTrial`. El ViewModel (`startTimeTrial`, `lastTimeTrial`, `timeTrialResults`) lo guarda en SU tabla y NO en `game_results`: no mueve el avance, los niveles, lo medido ni la ruta; sí cuenta para la racha y las farolas (por la fecha de la partida). Si Android cierra la app durante la partida, `GameSessionStore.InFlight.timeTrialDay` recuerda el día de la pista.
- Hoy y el final del Contrarreloj: ver la arquitectura. El final no lleva «¿Por qué importa?» y dice, en una nota, «Es el resultado de un juego, comparado solo contigo. No es una medida de capacidad.».

**Verificación de la fase 1** (lámina `docs/previews/tarea-177.png`, `tools/previews/tarea_177.py`): la prueba del azar de la Tarea 176 con sus números (stroop 10 %, disparate 44 %, radar 10 %, acoplamiento 59 %: el margen justo, el máximo es 70 %); Hoy en sus estados con letra 1,0, 1,3 y 2,0 (Roborazzi y emulador); los cuatro juegos corriendo como Contrarreloj en Unity (`NUBI_SHOTS_TT=1 NUBI_SHOTS_GAMES=stroop,disparate,radar,acoplamiento`, `ScreenshotRunner.TimeTrialShots`, solo Editor); el final en sus tres estados.

**Lo que se cambió del diseño al tocar el código real:** (1) la palabra «Contrarreloj» no cabe en media fila con letra 1,3 ni 2,0: el botón de Hoy tiene tres formas (con reloj, sin reloj y apilado con «Todos los juegos»); (2) la prueba de ajuste de Hoy medía con un borde recortado y daba por buenos botones a medio salir: ahora mide sin recortar (a 393 × 873 caben todos los estados con letra 1,0 y 1,3; a 360 × 780 con 1,0 caben 6 de 8, tras apretar 16 dp el ritmo vertical de Hoy; se deslizan la pausa, 31 dp, y la marca nueva con la ruta a medias, 45 dp).

