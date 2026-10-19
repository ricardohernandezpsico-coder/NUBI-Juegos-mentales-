# Sonido «Madera cálida» para toda la app — inventario, paleta y plan (Tarea 119, Fase A, 17-oct-2026)

Ricardo eligió el 17-oct llevar a todos los juegos los sonidos «Madera cálida» que aprobó el 10-oct en Rescate relámpago (kalimba, marimba y bloque de madera con una sala pequeña y suave: `Games/Radar/RadarSounds.cs`, piezas comunes en `Games/Shared/SoundKit.cs`). El motor «Cohete» de Piloto se queda.
La **Fase A** (inventario, mapa, paleta y página de escucha; 17-oct) no cambió ningún juego; Ricardo escuchó la página y aprobó todo (§9). La **Fase B** (lo común; Tarea 120, 17-oct) ya está hecha (§8); las Fases C a E (los sonidos propios de cada juego) siguen.

## 0. Resumen

- **Hoy conviven cuatro «idiomas» de sonido** (§1): Madera cálida (3 juegos), las «campanas» de la app (12 juegos, copiadas y pegadas), tonos armónicos sueltos (3 juegos) y los comunes de `GameFeel`. No suenan parejos: para el mismo evento hay hasta 12 LU de diferencia entre un juego y otro (§1.4).
- **Hay silencio donde debería haber sonido:** cuenta regresiva, «¡Ya!», pausa, avisos y botones del kit no suenan (§1.2). Y **suenan dos finales seguidos** en todos los juegos (§1.3).
- **La paleta (§2)** tiene 24 sonidos con un evento cada uno: tocar, elegir, quitar, tic, cuenta, acertar, errar, «no se puede», ayuda, nuevo, aviso, destello, hito de racha, subir de nivel, ronda perfecta, «¡Ya!», récord, fin, pausa (2) y 4 texturas. Reusa tal cual lo aprobado en Rescate, Aterrizaje y Acoplamiento. Es código C# nuevo y común, `Games/Shared/WoodSounds.cs`, **que todavía no usa ningún juego**; de ahí salen los WAV de la página.
- **Parejo:** cada sonido cae en una de cuatro familias de nivel (micro, respuesta, hito, cierre) que suben en escalones de 1 a 6 LU (§2.2). La paleta queda 5 a 8 LU más baja que lo común de hoy, igual que Rescate.
- **Sonidos con información (§4):** 14 familias llevan altura, orden o momento como parte del juego (los luceros de Rastro, la nota de cada grupo, la señal de salida de Freno…). Esos **conservan altura, orden e instante; solo cambia el timbre.** Hallazgo: la kalimba de Rescate suena más de 1 s y se encimaría con la nota siguiente de una secuencia: para las notas con información la caída baja a 0,3 s (§4.2).
- **Latencia:** el inicio de los sonidos de la paleta es igual o más rápido que el de hoy (mediana 0,5 ms contra 1,7 ms; el máximo, 19,6 ms, es el soplo de la ayuda, a propósito) (§5).
- **Memoria:** la paleta entera (49 variantes) pesa 12 MB guardada a 22 kHz; lo que carga todo juego (el núcleo común) pesa unos 6 MB (§6). Hoy Rescate sola usa 13 MB.
- **Plan (§8):** Fase B cambia lo común (`GameFeel`, cuenta, pausa, avisos y kit) y con eso suenan Madera todos los juegos en sus eventos básicos; las Fases C a E, los sonidos propios de 4 o 5 juegos por tarea.
- **Lo que necesito de Ricardo (§9):** escuchar la página y decir sí o no a la paleta, a los sonidos nuevos (cuenta, pausa, aviso, botón) y a las señales de Freno y Tinta o Palabra.

## 1. Cómo suena hoy

### 1.1 Los cuatro idiomas

| Idioma | Dónde | Cómo se hace | Memoria |
|---|---|---|---|
| **Madera cálida** | Rescate relámpago, Aterrizaje Lunar, Acoplamiento (y el motor «Cohete» de Piloto, que se queda) | Kalimba, marimba y bloque de madera + sala Freeverb estéreo; clips de 44,1 kHz calculados de a poco durante la cuenta regresiva (`RadarSounds`, `LandingSounds`, `DockingSounds`); volumen de reproducción 0,8 | Rescate: 18 clips, ≈ 13 MB |
| **Campanas de la app** | Constelaciones, Rastro de luz, Bodega, Correo, Piloto, Satélites, Carga exacta, Engranajes, En la punta de la lengua, ¿Verdad o disparate?, La estrella intrusa | Seno con parciales de campana (×2,76 y ×5,4), mono, sin sala; **cada juego tiene su copia** de `Bell`, `Noise`, `Make` y los mismos «Chime», «Thud» y «Finale» (7, 6 y 8 veces) | 10 a 25 clips mono por juego |
| **Tonos armónicos** | Tinta o Palabra, Freno de emergencia, Lluvia de meteoros (`GameControllerBase.PlayTone` → `HarmonicTone`) | Seno con 2.º y 3.er armónico, de 0,03 a 0,1 de amplitud | 3 a 10 tonos |
| **Común (`GameFeel`)** | Tinta o Palabra, Freno, Meteoros, Disparate, y las prácticas de Aterrizaje; el tic, en 15 juegos; el final, en los 17 | Marimba «pling» por la pentatónica (la racha la sube), «bu-bum» de error, arpegio de nivel, tic de madera y campana final | 15 clips mono |

(Retirados, siguen en el código pero la app no los lanza: Bitácora, Rumbo a Casa y Cosecha de palabras; no entran en este plan.)

### 1.2 Lo que hoy no suena

Cuenta regresiva «3 · 2 · 1» y «¡Ya!» (`CountdownScreen.cs`), abrir y cerrar la pausa (`PauseMenu.cs`), los avisos en pantalla (`Toast.cs`: «¡Ráfaga!», «Nivel 2»…) y los botones y fichas del kit de plasticina (`PlasticTouch`, `PlasticPress`): todo en silencio. Cada juego pone a mano algún sonido en sus propios botones, pero el kit no suena.
Además la pausa congela el audio (`AudioListener.pause`), así que el sonido de abrir la pausa tiene que sonar antes de congelarlo.

### 1.3 Lo que suena de más o desparejo

- **Doble final.** Cada juego suena su `Finale` propio al terminar la partida y, justo después, `ExitButton.cs:87` suena `GameFeel.Finish()` al abrir el cierre. Son dos acordes encimados en los 17 juegos. En la Fase B queda uno.
- **Las prácticas del tutorial de Aterrizaje** suenan con `GameFeel.Correct` y `Wrong` (marimba y «bu-bum» viejos) dentro de un juego en Madera (`LandingGameController.Guided.cs:51`, `:57`).
- **Disparate suma dos sonidos a cada respuesta:** `WaveRing` junto al acierto común y `StaticClick` junto al error común.
- **Freno** suma un «rugido» (196 Hz) y un «resoplido» (147 Hz) a su acierto común; **Meteoros**, un tono de 330 Hz al destruir. Quedan para la Fase C (se evalúa quitarlos).

### 1.4 Los niveles no son parejos

Sonoridad (LUFS, ventana de 400 ms; ver §2.2) de los sonidos de hoy, ya con el volumen con que los llama cada juego. Para el mismo tipo de evento:

| Evento (paleta) | Sonidos de hoy | LUFS más bajo | LUFS más alto | Diferencia (LU) |
|---|---|---|---|---|
| Clack | 4 | -37,0 | -25,9 | 11,0 |
| Correct | 5 | -19,7 | -15,5 | 4,2 |
| Cue | 7 | -38,3 | -25,4 | 12,9 |
| Finish | 9 | -17,2 | -14,3 | 2,9 |
| Hint | 2 | -21,2 | -19,5 | 1,6 |
| Hum | 4 | -28,2 | -25,8 | 2,3 |
| Knock | 8 | -35,7 | -23,0 | 12,7 |
| LevelUp | 3 | -15,4 | -15,4 | 0,0 |
| New | 7 | -23,7 | -17,9 | 5,8 |
| Note | 5 | -25,2 | -18,8 | 6,4 |
| Notice | 6 | -29,8 | -21,7 | 8,1 |
| Perfect | 8 | -16,0 | -11,2 | 4,8 |
| Select | 2 | -26,6 | -25,1 | 1,5 |
| Spark | 3 | -32,7 | -24,4 | 8,4 |
| Streak | 3 | -19,9 | -16,7 | 3,3 |
| SweepUp | 6 | -30,6 | -19,3 | 11,3 |
| Tap | 3 | -34,8 | -28,4 | 6,4 |
| Tick | 3 | -33,6 | -26,3 | 7,2 |
| Unselect | 4 | -40,9 | -31,8 | 9,1 |
| Wrong | 14 | -25,5 | -17,5 | 8,0 |
| propio | 15 | -33,8 | -14,8 | 18,9 |

Un error suena entre −25,5 y −17,5 LUFS según el juego; «no se puede», entre −35,7 y −23; el suave de quitar, entre −40,9 y −31,8. La paleta fija **un** valor por evento.

## 2. La paleta «Madera cálida» (`Games/Shared/WoodSounds.cs`)

### 2.1 Cómo está armada

- **Un evento, un sonido**, siempre con el mismo instrumento y la misma altura en cualquier juego. Lo que sube con una cuenta (la racha, la marca, la cuenta regresiva) sube por la **misma escala pentatónica de do** que ya usa toda la app (do5, re5, mi5, sol5, la5, do6…).
- **Reusa lo aprobado.** `Perfect` y `Finish` son, muestra por muestra, los de Rescate (una prueba lo comprueba); `Select`, `Unselect`, `Tap`, `Wrong`, `SweepDown` y `Clack` salen de las recetas de Rescate, Acoplamiento y Aterrizaje (con el nivel ajustado a su familia). Lo nuevo (`Correct`, `Knock`, `Hint`, `New`, `Notice`, `Spark`, `Streak`, `LevelUp`, `Go`, `Count`, `PauseIn`, `PauseOut`, `Record`, `Tick`, `SweepUp`, `Hum`) está hecho con los mismos instrumentos y la misma sala.
- **La sala es la de Rescate** (`RadarSounds.Room`: tamaño 0,7, amortiguación 0,5, apertura 0,8; mezcla 0,18 o menos). La cola se calcula hasta 1,5 s y **se recorta donde deja de oírse** (50 dB bajo el pico o bajo −54 dBFS, con un fundido de 30 ms), así ningún sonido se corta de golpe y los suaves no gastan memoria en una cola inaudible.
- **Se reproducen a 0,8** (`WoodSounds.PlayVolume`), el volumen con el que Ricardo los eligió en el laboratorio.
- **Se guardan a 22,05 kHz** (la kalimba y la marimba no pasan de 10 kHz) y **los micro en mono**: la mitad y la cuarta parte de memoria (§6).

### 2.2 Los niveles: cuatro familias

Cada sonido tiene el pico de su receta (`Recipe.Level`, una constante) y una sonoridad aproximada: LUFS del momento más fuerte, con el filtro K de la recomendación UIT-R BS.1770 y ventanas de 400 ms (`Games/Shared/SoundMeter.cs`; un clip más corto que 400 ms se rellena con silencio, como pasa al oído). No es un medidor certificado: sirve para comparar sonidos entre sí. Los números de la tabla ya incluyen el volumen de reproducción de 0,8 (−1,9 dB).

| Familia | Qué es | Sonoridad objetivo | Pico |
|---|---|---|---|
| **Micro** | Lo que pasa todo el tiempo y casi no se nota: tocar, quitar, tic, cuenta, pausa | −38 a −28 LUFS | −25 a −15 dBFS |
| **Respuesta** | Lo que el juego dice de lo que hiciste: elegir, acertar, errar, «no se puede», ayuda, nuevo, aviso, destello | −27 a −21 LUFS | −19 a −13 dBFS |
| **Hito** | Lo que se celebra: racha, nivel, ronda perfecta, «¡Ya!» | −21 a −19 LUFS | −12,5 a −11 dBFS |
| **Cierre** | El final y el récord | −18 LUFS | −10 dBFS |

Entre un micro y un cierre hay al menos 6 LU; las medianas de las cuatro familias suben en orden (pruebas `TheLevelsFollowTheTiers_SoEverythingSoundsEven`). Las texturas (soplos, zumbido y golpe seco) no tienen banda: cada una va al nivel de su objeto.
**Comparación con hoy:** la paleta queda 5 a 8 LU más baja que lo común de hoy (acierto −21 contra −15; final −18 contra −14; error −23 contra −17,5), igual que Rescate. Si se siente baja basta subir `PlayVolume` (un solo número); el techo hoy es un pico de −10 dBFS.

### 2.3 Los 24 sonidos (medidos)

Pico y LUFS con el volumen de 0,8; «inicio» = ms hasta el 10 % del pico; «dura» = ms hasta 50 dB bajo el pico (con su cola de sala); memoria = como se calcula (estéreo, 44,1 kHz, 32 bits).

| Evento | Familia | Instrumento | Altura | Envolvente | Pico (dBFS) | LUFS | Inicio (ms) | Dura (ms) | Memoria (KB) |
|---|---|---|---|---|---|---|---|---|---|
| Tap | Micro | marimba apagada | 392 Hz (sol4) | ataque 3 ms, τ 0,06 s | -25,0 | -38,2 | 0,5 | 1235 | 436 |
| Unselect | Micro | kalimba apagada | 392 Hz | ataque 2 ms, τ 0,07 s | -17,9 | -31,0 | 0,0 | 1112 | 394 |
| Tick | Micro | bloque de madera | 1250 Hz + parcial ×2,71 | ataque 1 ms, τ 35 ms | -15,1 | -30,1 | 0,3 | 1115 | 395 |
| Count(3,2,1) | Micro | marimba | do5, re5, mi5 (un escalón por número) | ataque 3 ms, τ 0,12 s | -19,0 | -28,9 | 0,4 | 1258 | 444 |
| PauseIn | Micro | 2 marimbas, 0,1 s entre una y otra | mi5 → do5 (baja) | τ 0,15 s | -20,4 | -30,2 | 0,5 | 1450 | 510 |
| PauseOut | Micro | 2 marimbas | do5 → mi5 (sube) | τ 0,15 s | -20,4 | -29,0 | 0,5 | 1413 | 497 |
| Select(k) | Respuesta | kalimba | la de la escala: do5 a la6 (k = 0 a 9) | ataque 2 ms, τ 0,45 s | -19,0 | -25,0 | 0,0 | 1716 | 601 |
| Correct(racha) | Respuesta | 2 kalimbas, 90 ms entre una y otra | P[s] y P[s+2]: la pareja sube con la racha (1 a 7) | τ 0,30 s y 0,42 s | -14,3 | -21,3 | 0,0 | 1670 | 586 |
| Wrong | Respuesta | 2 marimbas graves | 196 Hz → 164,8 Hz (baja) | τ 0,25 s, 0,13 s entre notas | -15,1 | -22,8 | 0,6 | 1547 | 543 |
| Knock | Respuesta | golpe de madera hueco | 120 → 70 Hz y marimba de 185 Hz | τ 0,08 s | -13,0 | -25,8 | 0,5 | 1115 | 394 |
| Hint | Respuesta | soplo que sube y kalimba grave | ruido 400 → 1600 Hz; kalimba de 392 Hz a los 0,32 s | τ 0,2 s / 0,5 s | -16,8 | -23,0 | 19,6 | 2044 | 714 |
| New | Respuesta | 2 kalimbas agudas, 0,12 s entre una y otra | do6 → mi6 (sube) | τ 0,4 s / 0,5 s | -13,6 | -21,1 | 0,0 | 1952 | 683 |
| Notice | Respuesta | marimba | do6 | τ 0,2 s | -17,9 | -25,5 | 0,3 | 1207 | 426 |
| Spark(n) | Respuesta | kalimba aguda | do6 a mi7 (sube con n = 1 a 8) | τ 0,25 s | -19,0 | -26,9 | 0,0 | 1289 | 454 |
| Streak | Hito | arpegio de kalimba sobre marimba grave | mi5, la5, do6 sobre do5 | τ 0,35 s | -12,4 | -20,6 | 0,0 | 1520 | 534 |
| LevelUp | Hito | arpegio de kalimba | do5, mi5, sol5, do6 (65 ms entre notas) | τ 0,35 s | -11,8 | -20,4 | 0,4 | 1545 | 543 |
| Perfect | Hito | marimba y 2 kalimbas (la de Rescate) | do-mi-sol-do y do6, mi6 | τ 0,35 s | -11,3 | -19,4 | 1,2 | 1933 | 676 |
| Go | Hito | 2 marimbas y 1 kalimba aguda | do5, sol5 y do6 | τ 0,25 s / 0,4 s | -12,4 | -20,7 | 0,6 | 1574 | 553 |
| Record | Cierre | escalera de kalimba y acorde de marimba | do5 a la6, acorde do4-sol4-do5 | τ 0,35 s / 0,5 s | -9,9 | -18,0 | 0,4 | 2281 | 796 |
| Finish | Cierre | acorde de marimba y kalimba larga (el de Rescate) | 7 notas do4 a do6 y mi6 | τ 0,4 s / 0,6 s | -9,9 | -17,9 | 1,2 | 2427 | 846 |
| SweepUp | Textura | ruido de banda | 500 → 2200 Hz | τ 0,15 s | -20,4 | -31,7 | 2,2 | 1022 | 359 |
| SweepDown | Textura | ruido de paso bajo (el de Aterrizaje) | 1400 → 300 Hz | τ 0,14 s | -20,4 | -35,3 | 0,7 | 1132 | 400 |
| Hum | Textura | 2 marimbas graves | 196 y 294 Hz | τ 0,5 s | -20,4 | -27,4 | 1,1 | 1981 | 693 |
| Clack | Textura | golpe grave y bloque de madera (el de Aterrizaje) | 110 → 55 Hz y 520 Hz | τ 0,09 s | -13,6 | -27,2 | 0,4 | 1287 | 454 |

## 3. El mapa: cada evento de hoy, a su sonido de la paleta

El inventario completo (cada sonido de los 17 juegos y de lo común, dónde se llama y qué receta usa hoy) está en §10, con la columna «Mapa a Madera». Las reglas, en corto:

| Evento | Sonido de la paleta | Quiénes |
|---|---|---|
| Tocar o presionar (botón, ficha) | `Tap` | kit de plasticina, Acoplamiento, Engranajes («Arrancar»), Constelaciones (voltear), Intrusa (latido) |
| Elegir o marcar (la nota sube con la cantidad) | `Select(k)` | Rescate, Carga exacta, Satélites, Engranajes (paso), Punta (casilla) |
| Quitar, deshacer, volver | `Unselect` | Rescate, Satélites, Carga, Punta, Constelaciones (cerrar) |
| Último tramo del reloj | `Tick` | los 15 juegos que lo usan |
| Cuenta regresiva y «¡Ya!» | `Count(3, 2, 1)` y `Go` | los 17 (hoy en silencio) |
| Acertar | `Correct(racha)` | Tinta, Freno, Meteoros, Disparate, Rastro, Carga (paso), Piloto (atrapar), Intrusa |
| Errar | `Wrong` | todos (el «no era» de Rescate, Acoplamiento y Aterrizaje) |
| «Eso no se puede», sin efecto, «aún no» | `Knock` | Constelaciones, Correo, Satélites, Carga, Engranajes, Intrusa, Freno (lento), Piloto (fuera de ruta) |
| Ayuda de Nubi | `Hint` | Carga, Punta |
| «NUEVO», sorpresa, algo que se desbloquea | `New` | los 7 «Chime» copiados + Rastro |
| Aviso en pantalla | `Notice` | Toast, «¡Listo!» de las prácticas, hoja de Correo, pedido de Bodega |
| Destello de racha | `Spark(n)` | Bodega, Constelaciones, Correo, Satélites (luz), Engranajes (luz) |
| Hito de racha | `Streak` | Correo (escalón), Piloto (hiperimpulso), Disparate (aurora) |
| Subir de nivel | `LevelUp` | Tinta, Freno, Meteoros, Disparate, Punta, Intrusa, Correo (cierre del día) |
| Ronda, pedido o figura perfecta | `Perfect` | Constelaciones, Bodega, Correo, Satélites, Carga, Engranajes, Punta, Intrusa, Rescate (ya), Acoplamiento (anillo), Aterrizaje (diana) |
| Récord | `Record` | (hoy en silencio) |
| Fin de la partida | `Finish` | los 17, **uno solo** (hoy suenan dos) |
| Pausa: abrir y seguir | `PauseIn` y `PauseOut` | los 17 (hoy en silencio) |
| Soplos que suben o bajan, zumbido de arranque, golpe seco | `SweepUp`, `SweepDown`, `Hum`, `Clack` | texturas genéricas; los **sonidos propios** de cada objeto (el rayo de Bodega, la turbina de Engranajes, la radio de Correo…) se diseñan en las fases C a E |

## 4. Sonidos que llevan información del juego

Estos **no se pueden cambiar a la ligera**: su altura, su orden o su momento son parte de lo que el jugador percibe o de lo que se mide. Regla: **se conserva la altura (±1 %), el orden, la cantidad y el instante de inicio; solo cambia el timbre.** Cada uno está marcado en el inventario (§10) con la etiqueta `info`, `señal` o `tiempo`.

### 4.1 La lista

| Juego | Sonido | Qué lleva | Qué se conserva | Riesgo al cambiar el timbre |
|---|---|---|---|---|
| Rastro de luz | Las 9 notas de los luceros (392 a 1175 Hz), al mostrar el camino y al tocar | El camino se aprende con el oído y la vista | Altura, orden y momento de cada nota (los fija el juego) | Medio: la kalimba de Rescate suena > 1 s y se encima con la nota siguiente (§4.2) |
| Rastro de luz | Avisos de modo (1, 2 bajando, 3 en arco, 3 subiendo) | La instrucción del modo | Cuántas notas y hacia dónde van | Bajo: se distinguen por el patrón, no por el timbre |
| Constelaciones | La nota de cada grupo (11; la 11.ª, una octava arriba) | Ayuda de memoria: la pareja suena igual | Altura por grupo | Bajo |
| Bodega de carga | La nota de cada escotilla (12) | Ayuda de memoria | Altura por escotilla | Bajo |
| Correo | La nota de cada buzón (do, mi, sol, do) | Identifica el destino | Altura por buzón | Bajo (4 notas muy separadas) |
| En la punta de la lengua | La nota de cada casilla | La casilla k suena la nota k | Altura por casilla | Bajo |
| Satélites | Una campana por satélite con mensaje, cada 0,12 s | **La cantidad** de satélites a seguir | Cantidad y 0,12 s | Medio: las campanas se encíman si la cola es larga |
| Piloto Estelar | Nota de la baliza dentro de la ruta / zumbido fuera de ella; el «blip» IGUAL para todas las señales; las dos notas del cambio de misión a 0,4 s | Dentro o fuera; no delatar la señal de la misión; sincronía con la tarjeta | Que el zumbido siga claramente distinto de las notas; el blip idéntico entre señales; los 0,4 s | Medio |
| **Freno de emergencia** | **Señal de salida** (784 Hz, 60 ms) y **sirena de ALTO** (988-740-988 Hz, 90 ms cada 100 ms) | **Son el estímulo:** el tiempo de reacción se mide desde que suenan | Altura, duración, inicio inmediato y el patrón de la sirena | **Alto:** ver §4.3 |
| Tinta o Palabra | Tono de la regla: 330 Hz = «la tinta», 440 Hz = «la palabra»; 392 Hz al aparecer la regla | Qué regla vale | Altura y duración (0,1 s) | Alto: la altura ES la instrucción |
| Carga exacta | La nota sube con cada paso de la cuenta; las celdas y operaciones tienen su nota | Cuánto avanzó la cuenta | Altura por paso | Bajo |
| Engranajes | La nota sube con la profundidad de cada paso de la cascada | La profundidad | Altura por paso | Bajo |
| La estrella intrusa | La chispa toca una nota por estrella; **más fuerte** si la estrella lleva palabra | Si la estrella tiene palabra | Notas ascendentes y la diferencia de fuerza (≥ 6 dB) | Medio |
| Rescate, Acoplamiento | La marca k, la luz de la racha | Cuántas llevas | Escala ascendente | Ya está en Madera |

Sin información en el sonido: Disparate, Lluvia de meteoros (el tono de 330 Hz al destruir es retroalimentación), Aterrizaje.

### 4.2 Afinación medida: timbre de hoy contra kalimba y marimba

«Claridad de altura» = fracción de la energía de los primeros 300 ms que cae en la frecuencia de la nota (de 0 a 1; `SoundMeter.PitchClarity`). «Caída 20 dB» = ms desde el pico hasta que baja 20 dB.

| Familia | Notas | Claridad de altura: hoy | Kalimba | Marimba | Caída 20 dB: hoy (ms) | Kalimba | Marimba |
|---|---|---|---|---|---|---|---|
| Rastro de luz: las 9 notas de los luceros | 9 | 0,95 | 0,98 | 0,98 | 450 | 697 | 700 |
| Constelaciones: la nota de cada grupo (11) | 11 | 0,96 | 0,97 | 0,98 | 510 | 691 | 695 |
| Bodega de carga: la nota de cada escotilla (12) | 12 | 0,97 | 0,97 | 0,98 | 971 | 682 | 685 |
| Correo: la nota de cada buzón (4) | 4 | 0,96 | 0,97 | 0,97 | 465 | 735 | 735 |
| Piloto: las 4 balizas dentro de la ruta | 4 | 0,91 | 0,98 | 0,98 | 240 | 690 | 698 |
| Piloto: atrapar una señal (la racha sube la nota) | 5 | 0,96 | 0,96 | 0,97 | 580 | 652 | 656 |
| Satélites: las campanas de la presentación (una por satélite) | 5 | 0,97 | 0,97 | 0,98 | 696 | 690 | 690 |
| Satélites: marcar (cada marca sube) | 7 | 0,93 | 0,97 | 0,97 | 280 | 680 | 683 |
| Satélites: una luz que se enciende | 5 | 0,96 | 0,97 | 0,97 | 510 | 662 | 666 |
| Engranajes: la nota de cada paso de la cascada | 10 | 0,93 | 0,98 | 0,98 | 277 | 686 | 689 |
| Engranajes: la luz del cohete que se enciende | 10 | 0,96 | 0,97 | 0,98 | 510 | 689 | 693 |
| En la punta de la lengua: la nota de cada casilla (10) | 10 | 0,96 | 0,97 | 0,98 | 510 | 689 | 693 |
| Intrusa: la chispa traza la figura (estrella sin palabra) | 9 | 0,95 | 0,97 | 0,98 | 330 | 682 | 688 |
| Intrusa: la chispa traza la figura (estrella con palabra: más fuerte) | 9 | 0,79 | 0,97 | 0,98 | 367 | 682 | 688 |
| Señales cortas (Tinta o Palabra, Freno, Piloto, Meteoros): tono armónico de 0,1 s de hoy contra marimba seca de 0,1 s | 10 | 0,91 | — | 0,89 | 84 | — | 87 |

- **La altura queda igual de clara:** kalimba y marimba dan 1,0 en todas las familias (la campana de hoy, entre 0,8 y 1,0). Con la altura no hay riesgo de que dos tonos se parezcan más.
- **La caída sí cambia, y eso sí podría alterar la dificultad.** La campana de hoy cae 20 dB en 0,2 a 0,6 s (Bodega, 1 s); la kalimba de Rescate, en 1 s. En una secuencia rápida (Rastro, Satélites) una nota que suena más de un segundo se encima con las siguientes y las hace más difíciles de separar. **Propuesta: para las notas con información la kalimba cae en 0,3 s (la caída de hoy), igual a la marimba (0,7 s)**; es lo que usa `WoodSounds.MakeNote`. Los marcadores de Rescate se quedan como están (son pocos y lentos).
- **Recomendación:** marimba como primera opción para las familias donde la altura es la información (Rastro, Satélites, Piloto, Tinta o Palabra), y kalimba donde la nota es una ayuda de memoria (Constelaciones, Bodega, Correo, Punta). La página tiene las dos para que Ricardo elija.
- **La señal corta** (Tinta o Palabra, Freno, Piloto, Meteoros) es una marimba seca de la MISMA duración que el tono de hoy (0,06 s la salida de Freno, 0,1 s las reglas) y casi sin sala (`WoodSounds.MakeCue`).

### 4.3 Freno de emergencia: la señal es el estímulo

La salida (784 Hz, 60 ms) suena en el mismo instante en que se enciende la luz, y el tiempo de reacción se mide desde ahí. Cambiar su timbre cambia el estímulo y podría mover los tiempos (los estímulos de audio llegan más rápido a la respuesta que los de luz). **Propuesta:** la señal Madera empieza igual de rápido (inicio 0,3 ms, como la de hoy), dura lo mismo y suena a la misma altura; la sirena de ALTO **se queda** como una alarma reconocible (tres tonos alternados) y NO pasa a madera suave: si se cambia, que sea con la misma altura y ritmo, y se prueba con personas antes de comparar medidas. Decisión de Ricardo en §9.

## 5. Latencia de inicio

«Inicio» = ms desde la primera muestra del clip hasta que llega al 10 % de su pico. Lo que se mide es el clip; el retardo del sistema (buffer de audio de Android) no cambia.

Inicio = ms hasta que el sonido llega al 10 % de su pico (lo que tarda en oírse).

| | Sonidos | Inicio: mediana | Inicio: máximo | Con inicio de 3 ms o menos |
|---|---|---|---|---|
| Hoy | 152 | 1,7 | 280,3 | 107 de 152 |
| Madera (paleta) | 24 | 0,5 | 19,6 | 23 de 24 |

Los de hoy con el inicio más lento: engranajes.engranajes.despegue (280,3 ms), parejas.parejas.racha (181,4 ms), parejas.parejas.zumbido (80,3 ms), correo.correo.bocina (66,3 ms), correo.correo.combo (61,1 ms), correo.correo.dia (59,8 ms).

- Los de hoy más lentos lo son **a propósito**: el despegue de Engranajes sube un rugido durante 0,3 s, el destello de racha de Constelaciones entra 0,18 s después del acorde (retardo dentro del clip) y los zumbidos suben de a poco.
- **El único de la paleta que pasa de 3 ms es la ayuda** (`Hint`, 19,6 ms): empieza con un soplo suave que sube y la nota entra a los 0,32 s. Es una decisión de diseño; la prueba `TheStartOfEverySoundIsImmediate_TheLatencyDoesNotGrow` lo acota en 30 ms y a todos los demás en 3 ms.
- **La sala no atrasa el sonido:** la reverberación (Freeverb) es aditiva; la señal seca sale intacta y la cola viene después.
- **El cálculo sí tarda:** reverberar un clip cuesta decenas de ms. Por eso los clips se calculan **de a poco durante la cuenta regresiva** (`Prewarm`, 24.000 muestras por cuadro, como ya hacen Rescate, Aterrizaje y Acoplamiento), nunca en el momento de sonar. En la Fase B se mide en el teléfono (Motorola) el tiempo del primer sonido de cada juego.

## 6. Memoria

- Paleta completa (49 clips: todas las variantes), como se calcula (estéreo, 44,1 kHz, 32 bits, ya recortada a lo audible): **26.0 MB**.
- Guardada como la guarda el juego (22,05 kHz; los micro en mono): **12.1 MB**.
- El núcleo común que carga TODO juego en la Fase B (25 clips: acierto con sus 8 escalones, error, nivel, tic, fin, cuenta, ya, pausa, aviso, tap, no se puede, nuevo, perfecto, ayuda, quitar): **5.8 MB**.

Cómo llegar ahí (todo está en `WoodSounds`, con pruebas):
1. Se calcula a 44,1 kHz (como los demás) y se **guarda a 22,05 kHz** con un filtro de 10 kHz (`SoundKit.HalveRate`): la mitad.
2. Los **micro van en mono** (llevan casi nada de sala): la mitad de esos.
3. La cola de sala se **recorta donde deja de oírse** (§2.1).
4. Cada juego **carga solo lo que usa**: el núcleo común (todos los juegos) más sus sonidos propios. Las escaleras (racha 1 a 8, marcas, cuenta) cuestan 0,4 a 0,6 MB por escalón; si hiciera falta, se limitan a 6 escalones.
5. Unity queda vivo entre partidas: los clips del núcleo común son estáticos, se calculan **una vez** en la primera partida y se quedan.
Resultado esperado de un juego en Madera: núcleo común (≈ 6 MB) + propios (≈ 3 a 5 MB) ≈ 10 MB, menos que los 13 MB que usa Rescate hoy.

## 7. La página de escucha

`docs/previews/sonido-escucha.html` (una sola página, hecha para el teléfono):
- Por cada evento, el botón azul **«Hoy»** y el amarillo **«Madera»** (y, en las notas con información, un tercero verde con la marimba). Lo común va primero y luego cada juego por área. Abajo de cada botón: pico, sonoridad e inicio ya con el volumen real del juego (los chicos suenan chicos; el interruptor «Igualar volumen» compara solo el timbre).
- **Los WAV salen del MISMO C# que suena en el juego** (script de Editor): «Hoy», de las clases `XSounds` y de `GameFeel` (por reflexión); «Madera», de `WoodSounds`. Nada está recreado en otro lado. Los sonidos propios que aún no tienen versión Madera (la matraca de Bodega, la turbina de Engranajes…) se listan sin botón: se diseñan en la Fase C.
- Pesan poco: mono, 22 kHz, 16 bits, recortados al silencio, con los idénticos compartidos (156 archivos, 8.0 MB). La página carga cada WAV solo cuando se toca.
- **Para regenerar** (después de cambiar un sonido o la paleta): `Unity -batchmode -projectPath unity/NeuroVidaCore -executeMethod NeuroVida.Bridge.EditorTools.SoundListeningExporter.ExportBatch` (o el menú «NeuroVida/Sonido/Exportar página de escucha»; deja `docs/previews/sonido-escucha/*.wav` y `tools/sonido/catalogo.json`) y luego `python tools/sonido/armar_escucha.py`. Las tablas de este documento salen de `python tools/sonido/tablas_plan.py`.
- **Para abrirla en el teléfono como un solo archivo:** `python tools/sonido/armar_escucha.py --incrustar --salida <archivo>` mete los WAV adentro (unos 11 MB); sirve para publicarla como página.

## 8. Plan por fases

**Fase B — lo común (Tarea 120, 17-oct): HECHA.** `GameFeel` suena la paleta y con eso **suenan Madera los 17 juegos en sus eventos básicos**. Qué cambió:
- **`WoodKit`** (`Games/Shared`): los instrumentos (`Kalimba`, `Marimba`, `Woodblock`, `Thump`, `NoiseSweep`, `StaticNoise`), la escala, la sala y el volumen. `RadarSounds` los usa a través de envolturas: mismas muestras, bit por bit (las pruebas de referencia de Rescate, Acoplamiento y Aterrizaje siguen en verde).
- **`GameFeel`** (acierto, error, nivel, tic, fin) suena la paleta a través de **`WoodCore`** (`Games/Shared`): los clips se guardan (22 kHz; micro en mono), se calculan UNA vez y se conservan entre partidas. Lo de la cuenta regresiva (3, 2, 1, «¡Ya!») se calcula de golpe al llegar la configuración (`GameEntryPoint` → `GameFeel.Warm()`, unos 0,2 s); el resto del núcleo (15 clips, ≈ 4,5 MB), de a poco, 5 ms por cuadro. Un sonido del núcleo pedido antes de estar listo NO se calcula en ese momento (tirón): no suena y queda contado (`WoodCore.Skipped`). Con el sonido apagado no se calcula ni se crea nada.
- **Nuevos:** `CountdownScreen` (3, 2, 1 y «¡Ya!»), `PauseMenu` (abrir y seguir; por un altavoz que ignora la pausa del audio: `ignoreListenerPause`), `Toast` (aviso: una marimba solo si nada más suena con él, decidido un cuadro después; `Show(..., sound: false)` lo apaga), `PlasticPress` (toque del kit al apoyar el dedo; máximo uno cada 50 ms; `PlaysTap = false` lo apaga) y `PlasticTouch` (`PlaysTap`, apagado por omisión: el juego suena lo suyo).
- **Nada doble:** Tinta o Palabra apaga el toque de sus botones de respuesta (suenan acierto o error); Disparate apaga el aviso de sus dos avisos que ya suenan algo propio; «¡Listo!» y «Seguir» de Rescate, Intrusa y Correo suenan el toque del kit porque no sonaban nada. Las pruebas `SoundRulesTests` lo vigilan leyendo el código (un aviso cerca de un sonido propio exige `sound: false`; un `PlasticPress` en un juego exige `PlaysTap = false`).
- **UN solo final:** se quitaron los 11 finales propios (Rescate, Acoplamiento, Aterrizaje, Bodega, Carga exacta, Correo, Engranajes, Constelaciones, Piloto, Satélites, En la punta de la lengua); suena el de `ExitButton` (`GameFeel.Finish`). Rescate, Acoplamiento y Aterrizaje ya no calculan el suyo (−1 clip estéreo cada uno). `SoundRulesTests` prohíbe que un juego suene uno propio y el smoke exige a lo más UN final por partida.
- **Las prácticas** de Aterrizaje y Meteoros ya usaban `GameFeel.Correct` y `Wrong`: suenan la paleta. El «¡Listo!» de las prácticas de Tinta, Freno y Meteoros es `GameFeel.Notice()`.
- **El volumen: +3 dB**, en UN solo lugar: `WoodKit.MasterGainDb`. Se aplica a las muestras justo antes de crear el clip (`WoodKit.ApplyMaster`): la paleta y los sonidos propios de Rescate, Acoplamiento y Aterrizaje (y el propulsor de Aterrizaje, para que siga a la misma distancia). Los escalones entre familias no cambian. El pico más alto (el final) queda en −6,9 dBFS ya reproducido (≤ −6 exigido) y dos de los sonidos más fuertes a la vez suman menos de 1,0 (no saturan): pruebas `TheVolume_IsThreeDecibelsHigher_InOnePlace_AndNothingSaturates`.
- **Pruebas nuevas:** `WoodCoreTests` (cuenta lista antes, resto de a poco, núcleo calculado una vez y conservado, «Mudo» sin calcular ni crear nada, el primer sonido no calcula y sale al instante, núcleo < 6 MB, altavoz de interfaz que ignora la pausa, toque limitado, aviso que no se suma, un final contado) y `SoundRulesTests`; en el smoke, corridas «Mudo» de 13 juegos más (ni la cuenta, ni la pausa, ni los avisos, ni el final suenan con el sonido apagado), y cada partida con sonido exige que suenen la cuenta y «¡Ya!» y a lo más un final.
- La página de escucha sigue mostrando «hoy» tal como sonaba antes de esta fase: las recetas viejas de `GameFeel` quedaron congeladas en `Bootstrap/Editor/SoundListeningLegacy.cs`.

**Fase C — notas con ayuda de memoria (5 juegos): HECHA (Tarea 135, 19-oct).** Carga exacta, En la punta de la lengua, Constelaciones, Bodega de carga y La estación de correo suenan Madera cálida (ver §11); la pista de Constelaciones es la nota del grupo de cada luz hermana (opción (a) que eligió Ricardo; Tarea 137, §12). Lo que se pensó: Constelaciones, Bodega de carga, La estación de correo, En la punta de la lengua, Carga exacta. Cada nota conserva su altura; sus sonidos propios (voltear luces, puertas, saco, radio…) pasan a Madera. Prueba nueva por juego: **la altura y el instante de cada sonido con información son los de hoy** (comparar `PitchClarity` y el inicio contra la grabación de hoy).

**Fase D — información fuerte (4 juegos): HECHA (Tarea 138, 19-oct, sin pausa de escucha por decisión de Ricardo: «hazlos con tu criterio»; ver §13).** Rastro de luz, Satélites, Piloto Estelar, La estrella intrusa. Los luceros, la cantidad de campanas, las balizas y las dos notas de la misión a 0,4 s. Se prueba el tiempo (la secuencia suena igual de rápido) y que las notas no se encimen.

**Fase E — señales y propios largos: HECHA (Tarea 139, 19-oct, sin pausa de escucha; ver §14). CON ELLA SE CIERRA EL PLAN: los 14 juegos del plan suenan «Madera cálida».** Freno de emergencia, Tinta o Palabra, Lluvia de meteoros (las señales de Freno y las reglas de Tinta son estímulos: se prueban con personas), ¿Verdad o disparate? y Engranajes (los propios largos: motor, turbina, despegue).

Cada fase: commit y push, `--instalar`, y Ricardo prueba en el teléfono antes de seguir. **Ninguna fase cambia reglas, DDA, tiempos, medidas ni telemetría.**

## 9. Lo que decidió Ricardo (17-oct, después de escuchar la página)

Ricardo escuchó la página de escucha y **aprobó todo**:
1. **La paleta**, tal cual.
2. **Sonido donde hoy hay silencio:** cuenta regresiva 3-2-1 y «¡Ya!», abrir la pausa y seguir, los avisos y el toque del kit.
3. **Notas con información:** **marimba** en Rastro de luz, Satélites, Piloto Estelar y Tinta o Palabra (la altura ES la información); **kalimba** donde la nota es una ayuda de memoria (Constelaciones, Bodega de carga, La estación de correo, En la punta de la lengua). Se aplica en las Fases C a E.
4. **Freno de emergencia:** la señal de salida pasa a Madera con la misma altura, duración e inicio; la sirena de ALTO se queda como está (Fase E).
5. **UN solo final** por partida.
6. **Subir un poco el volumen:** +3 dB a toda «Madera cálida» (hecho en la Fase B, §8).

## 10. Inventario: cada sonido, dónde se llama y qué receta usa hoy

**Estado al 19-oct (Tarea 139): los cinco juegos de la Fase E (Tinta o Palabra, Lluvia de meteoros, Freno de emergencia, ¿Verdad o disparate? y Engranajes) TAMBIÉN suenan «Madera cálida» y el plan de sonido queda CERRADO (ver §14): la columna «Hoy suena» de las tablas de abajo es el sonido del 17-oct (la página de escucha lo muestra CONGELADO) y lo que suena ahora es lo de §12, §13 y §14.** **Estado al 19-oct (Tarea 138): los cuatro juegos de la Fase D (Rastro de luz, Satélites, Piloto Estelar y La estrella intrusa) TAMBIÉN suenan «Madera cálida» (ver §13); lo dicho abajo de la Fase C vale igual para ellos.** **Estado al 19-oct (Tarea 135): los cinco juegos de la Fase C (Carga exacta, En la punta de la lengua, Constelaciones, Bodega de carga y La estación de correo) YA suenan «Madera cálida»; en sus secciones, las columnas «Hoy suena», «Dónde se llama» y «Pico / LUFS hoy» son el sonido y el código del 17-oct (la página de escucha los muestra CONGELADOS) y «Mapa a Madera» es lo que suena ahora (el mapa completo y sus niveles, en `auditoria-fable/spec-sonido-CDE.md` §2).** Los demás juegos siguen como dice la tabla. Líneas al 17-oct (commit 32e192d). «Pico / LUFS hoy» ya incluye el volumen con que lo llama el juego; «Info»: `info` = altura, orden o momento es información; `señal` = estímulo o instrucción; `tiempo` = sincronizado; `bucle` = suena debajo del juego. Los eventos de la paleta con «NUEVO» hoy no suenan.

### Lo común

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Acertar (la nota sube con la racha: 1 a 8) | GameFeel: «pling» de marimba (seno + parcial ×4, τ 0,11 s) por la pentatónica do5 a mi6, mono, sin sala; pico ≈ 0,5 | GameFeel.cs:41 Correct(racha). Lo llaman Tinta o Palabra (StroopGameController.cs:287), Freno (BrakeGameController.cs:313, :358), Lluvia de meteoros (MeteorGameController.cs:492), ¿Verdad o disparate? (DisparateGameController.cs:586) y las prácticas de Aterrizaje (LandingGameController.Guided.cs:51), Meteoros (:1231) y Tinta (:1162) | -5,6 / -15,8 | Correct(racha): kalimba de 2 notas que suben |  |
| Equivocarse | GameFeel: «bu-bum» de seno grave (220 y 185 Hz con tercer armónico), mono, sin sala; vibración doble | GameFeel.cs:49 Wrong(). Tinta (StroopGameController.cs:307), Freno (BrakeGameController.cs:331, :379), Meteoros (MeteorGameController.cs:499), Disparate (DisparateGameController.cs:621) y las prácticas de Aterrizaje (.Guided.cs:57) y Meteoros (:1239) | -10,5 / -17,5 | Wrong: dos marimbas graves que bajan (la de Rescate) |  |
| Subir de nivel | GameFeel: arpegio de marimba do5-mi5-sol5-do6 cada 65 ms, mono, sin sala; vibración firme | GameFeel.cs:56 LevelUp(). Tinta (StroopGameController.cs:212), Freno (BrakeGameController.cs:288, :323, :368), Meteoros (MeteorGameController.cs:187), Disparate (:292 «¡Ráfaga!», :794), En la punta de la lengua (PuntaGameController.cs:351), La estrella intrusa (IntrusaGameController.cs:1055) | -7,6 / -15,4 | LevelUp: el mismo arpegio, de kalimba |  |
| Tic de los últimos 5 segundos | GameFeel: golpecito de madera (1250 Hz + 2500 Hz, τ 12 ms), mono, sin sala; un tic por segundo | GameFeel.cs:63 Tick(). Acoplamiento (DockingGameController.cs:476), Punta (:1041), Aterrizaje (:473), Bodega (:828), Carga exacta (:788), Engranajes (:635), Freno (:421), Intrusa (:1171), Meteoros (:466), Constelaciones (:539), Rescate (RadarGameController.cs:469), Satélites (:433), Rastro (:717), Tinta (:364), Disparate (:927) | -8,7 / -26,3 | Tick: bloque de madera |  |
| Fin de la partida («¡Listo!») | GameFeel: acorde de campana do5-mi5-sol5-do6 (τ 0,42 s) con brillo a 2093 Hz, 1,1 s, mono, sin sala; vibración firme | ExitButton.cs:87 (GameFeel.Finish) al empezar el cierre, en los 17 juegos | -5,2 / -14,3 | Finish: el final de Rescate (marimba y kalimba larga) |  |
| Cuenta regresiva 3 · 2 · 1 | Silencio (solo se ven los números) | CountdownScreen.cs:186 Play(), en los 17 juegos | — | Count(3), Count(2), Count(1): marimba do, re, mi |  |
| «¡Ya!» | Silencio | CountdownScreen.cs:321 y :447 (con «quitar animaciones») | — | Go: dos marimbas y una kalimba aguda |  |
| Abrir la pausa | Silencio (el audio se congela con AudioListener.pause) | PauseMenu.cs:187 Show() | — | PauseIn: dos marimbas suaves que bajan |  |
| Seguir jugando | Silencio | PauseMenu.cs (botón «Seguir») | — | PauseOut: las mismas dos, que suben |  |
| Aviso en pantalla (Toast, «¡Ráfaga!», «Nivel 2»…) | Silencio (algunos juegos suman su propio sonido) | Toast.cs:178 Show() | — | Notice: una marimba sola |  |
| Botón o ficha presionada (kit de plasticina) | Silencio en el kit; cada juego pone el suyo (campanita, siseo, golpe…) | PlasticTouch.cs:86 Press(); PlasticSlab.cs:214 PlasticPress (pointer down) | — | Tap: marimba apagada de 60 ms (la de Acoplamiento) |  |
| Elegir o marcar (la nota sube con la cantidad) | Cada juego el suyo: campanitas (Carga exacta, Satélites, Engranajes), kalimba (Rescate) | ver cada juego | — | Select(k): kalimba do5, re5, mi5… (la marca de Rescate) |  |
| Quitar o deshacer | Cada juego el suyo: soplo que baja, golpecito, campanita apagada | ver cada juego | — | Unselect: kalimba apagada (la de Rescate) |  |
| «Eso no se puede» / sin efecto | Cada juego el suyo: golpe sordo, tono grave suave, madera sorda | ver cada juego | — | Knock: UN toc de madera grave y hueco |  |
| Ayuda de Nubi | Cada juego el suyo: dos campanas o soplo y campana | ver cada juego | — | Hint: soplo que sube y kalimba grave |  |
| «NUEVO», sorpresa, algo que se desbloquea | Dos campanas (mi y la) copiadas en 7 juegos | ver cada juego | — | New: dos kalimbas agudas que suben |  |
| Destello de racha (sube con cada acierto seguido) | Una campanita aguda por juego (Bodega, Constelaciones, Correo, Satélites) | ver cada juego | — | Spark(n): kalimba aguda que sube |  |
| Hito de racha (cada 5, 10, 20…) | Correo: tres campanas por escalón (×10, ×20, ×30); los demás, nada | MailGameController.Scene.cs:183 | — | Streak: arpegio corto de kalimba |  |
| Ronda perfecta / pedido perfecto / diana | Cada juego el suyo: arpegio de 4 campanas o do-mi-sol-do | ver cada juego | — | Perfect: la de Rescate (marimba do-mi-sol-do y dos kalimbas) |  |
| Récord personal | Silencio (solo se ve) | — | — | Record: escalera de kalimba y acorde de marimba |  |
| Textura: soplo que sube (algo aparece o se abre) | Cada juego el suyo: barridos de ruido | ver cada juego | — | SweepUp: ruido de banda 500 → 2200 Hz |  |
| Textura: soplo que baja (algo se suelta o vuelve) | Cada juego el suyo: barridos de ruido | ver cada juego | — | SweepDown: el de Aterrizaje (paso bajo 1400 → 300 Hz) |  |
| Textura: zumbido que arranca | Bodega, Correo, Constelaciones, Engranajes: senos o triángulos graves | ver cada juego | — | Hum: dos marimbas graves (196 y 294 Hz) |  |
| Textura: golpe seco de un objeto | Satélites «clac», Bodega y Engranajes «clank» | ver cada juego | — | Clack: el golpe de Aterrizaje al posarse |  |

### Constelaciones

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Cada grupo de luces suena SU nota (11: la 11.ª, una octava arriba) | Campana de la pentatónica (τ 0,22 s, 0,9 s), mono, sin sala; pico ≈ 0,3 | ConstelacionGameController.cs:251 (al abrir una luz, vol 0,35), :267 (al fallar, vol 0,3) | ver afinación | Note(hz): kalimba (o marimba) | info |
| Dar vuelta una luz | Siseo agudo que sube (2200 → 3400 Hz), 0,12 s | ConstelacionGameController.cs:250 (vol 0,5) | -21,0 / -34,8 | Tap (más seco) |  |
| Cerrar una luz | Siseo corto que baja, 0,1 s | ConstelacionGameController.cs:247, :468 (vol 0,5) | -23,1 / -40,9 | Unselect |  |
| «No son iguales» | Seno grave y corto que baja 330 → 247 Hz, 0,32 s | ConstelacionGameController.cs:273 (vol 0,5) | -21,0 / -30,8 | Knock |  |
| Encontrar un grupo de memoria | Dos campanas (nota del grupo y su quinta), 1,6 s | ConstelacionGameController.cs:293 (vol 0,8) | -11,4 / -18,2 | Correct (más alto si fue de memoria) |  |
| Encontrar un grupo a la primera vista (suerte) | Dos campanas (nota y tercera), más calladas, 1,4 s | ConstelacionGameController.cs:300 (vol 0,7) | -14,3 / -21,3 | Correct, 3 dB más bajo |  |
| Destello de racha de memoria (1 a 8) | Campanita aguda que sube por la escala | ConstelacionGameController.cs:294 (vol 0,6) | -20,6 / -24,5 | Spark(n) |  |
| Un cielo completo | Arpegio de cuatro campanas, 1,8 s | ConstelacionGameController.cs:224 (vol 0,8) | -8,6 / -14,5 | Perfect |  |
| Empieza el cielo: zumbido de fondo | Dos senos graves (196 y 294 Hz), 1,7 s | ConstelacionGameController.cs:205 (vol 0,5) | -20,0 / -25,8 | Hum |  |
| «NUEVO» | Dos campanas (mi y la) | ConstelacionGameController.cs:419 (vol 0,7), :473 (vol 0,25) | -14,5 / -21,7 | New |  |
| Fin de la partida | Tres campanas que suben, 2 s, con eco | ConstelacionGameController.cs:358 (vol 0,8) | -11,4 / -17,2 | Finish (y se quita el doble final) |  |

### Rastro de luz

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Cada lucero suena SU nota (9 luceros, 392 a 1175 Hz) | Campana: seno + parciales ×2,76 y ×5,4, ataque 8 ms, cae hasta el silencio en 1,3 s; mono, sin sala; pico ≈ 0,3 | RastroGameController.cs:389 (al tocar el lucero, vol 1), :467 (al mostrar el camino, vol 0,88) | ver afinación | Note(hz): kalimba (o marimba) en el MISMO Hz | info |
| Ronda lograda: do-mi-sol-do | Campana: cuatro notas escalonadas 70 ms, 2 s | RastroGameController.cs:489, :562 (vol 1) | -6,9 / -15,5 | Correct (o Perfect): a decidir en la Fase C |  |
| Error: campana grave | Campana de 196 Hz, 0,7 s | RastroGameController.cs:530, :575 (vol 1) | -12,8 / -24,4 | Wrong |  |
| «¡NUEVO!»: modo desbloqueado | Dos campanas (do6 y mi6), 1,7 s | RastroGameController.cs:624 (vol 1) | -10,8 / -17,9 | New |  |
| Aviso de modo: Rastro (una campana) | Campanas de la misma familia, cortas | RastroGameController.cs:317 (vol 0,8), :624 (vol 1) | -14,7 / -25,2 | Note(hz) con el mismo patrón y ritmo | info |
| Aviso de modo: Al revés (dos que bajan) | Campanas de la misma familia, cortas | RastroGameController.cs:317 (vol 0,8), :624 (vol 1) | -12,9 / -21,7 | Note(hz) con el mismo patrón y ritmo | info |
| Aviso de modo: El cielo gira (tres: sube, baja) | Campanas de la misma familia, cortas | RastroGameController.cs:317 (vol 0,8), :624 (vol 1) | -12,9 / -20,6 | Note(hz) con el mismo patrón y ritmo | info |
| Aviso de modo: En marcha (tres que suben rápido) | Campanas de la misma familia, cortas | RastroGameController.cs:317 (vol 0,8), :624 (vol 1) | -12,6 / -20,6 | Note(hz) con el mismo patrón y ritmo | info |
| El cielo gira: soplo de aire | Ruido por un filtro de banda 900 → 2400 Hz, 0,5 s | RastroGameController.cs:406 (vol 1) | -11,1 / -19,3 | SweepUp |  |
| Tic del reloj | Común (GameFeel.Tick) | RastroGameController.cs:717 | -8,7 / -26,3 | Tick |  |

### Bodega de carga

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Cada escotilla suena SU nota (la 11.ª y siguientes, una octava arriba) | Campana de la pentatónica (τ 0,42 s, 1,3 s), mono, sin sala; pico 0,3 | BodegaGameController.cs:222 (al mostrar, vol 0,9), :410 (al abrir, vol 0,9) | ver afinación | Note(hz): kalimba (o marimba) | info |
| El robot arranca: zumbido grave | Triángulo 65 → 90 Hz, 0,85 s | BodegaGameController.cs:190 (vol 0,5) | -20,0 / -28,2 | Hum |  |
| Llega la carga por la esclusa | Soplido que baja y campanita aguda, 0,6 s | BodegaGameController.cs:203 (vol 0,6) | -21,3 / -27,0 | propio (Fase C): SweepDown + una kalimba aguda |  |
| El haz del robot («piu» 420 → 880 Hz) | Seno que sube, 0,34 s | BodegaGameController.cs:214, :254, :284 (vol 0,6) | -18,5 / -27,2 | propio (Fase C): kalimba corta que sube |  |
| El robot vuela con la caja | Soplido de ruido 300 → 1500 Hz, 1,1 s | BodegaGameController.cs:264 (vol 0,6); BodegaGameController.Scene.cs:607 (vol 0,5) | -21,2 / -28,4 | SweepUp (más largo) |  |
| Abrir una puerta | Siseo agudo que baja, 0,2 s | BodegaGameController.cs:217, :385, :408, :699 (vol 0,6) | -20,4 / -33,8 | propio (Fase C): SweepUp corto |  |
| Cerrar una puerta | Golpe grave y suave (ruido 700 → 250 Hz), 0,2 s | BodegaGameController.cs:228, :391, :422, :699 (vol 0,5) | -21,8 / -37,0 | Clack (más suave) |  |
| Tomar o dejar la caja («clank») | Golpe metálico corto y campanita aguda, 0,4 s | BodegaGameController.cs:260, :290 (vol 0,6) | -17,8 / -30,8 | Clack |  |
| La bodega gira: matraca de engranes | Un clic cada 110 ms durante 1,5 s y zumbido grave | BodegaGameController.cs:309 (vol 0,6) | -18,4 / -29,1 | propio (Fase C): una fila de bloques de madera |  |
| La pregunta de un pedido | Campanita (mi agudo), 0,6 s | BodegaGameController.cs:355, :395 (vol 0,6) | -21,3 / -29,8 | Notice |  |
| Error: golpe sordo | Seno grave que baja 220 → 130 Hz, 0,4 s | BodegaGameController.cs:387 (vol 0,7) | -13,7 / -24,1 | Wrong |  |
| Destello de racha (1 a 8) | Campanita aguda que sube de tono | BodegaGameController.cs:413 (vol 0,6) | -19,6 / -24,4 | Spark(n) |  |
| Pedido perfecto | Arpegio de cuatro campanas, 1,8 s | BodegaGameController.cs:472 (vol 0,8) | -8,5 / -14,4 | Perfect |  |
| «NUEVO» | Dos campanas (mi y la) | BodegaGameController.cs:565 (vol 0,7) | -14,5 / -21,7 | New |  |
| Fin de la partida | Tres campanas que suben, 2 s, con eco | BodegaGameController.cs:486 (vol 0,8) | -11,4 / -17,2 | Finish |  |

### La estación de correo

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Cada buzón suena SU nota (do5, mi5, sol5, do6) | Campana suave (τ 0,2 s, 0,7 s), mono, sin sala; pico 0,3 | MailGameController.Scene.cs:158 (al entregar, vol 0,5) | ver afinación | Note(hz): kalimba (o marimba) | info |
| Brillo de la racha (desde ×3) | Campanita aguda que sube por la escala | MailGameController.Scene.cs:159 (vol 0,35) | -26,3 / -32,7 | Spark(n) |  |
| Escalón de racha (×10, ×20, ×30) | Dos a cuatro campanas, 1,4 s | MailGameController.Scene.cs:183 (vol 0,7), :561 (vol 0,5) | -12,2 / -18,2 | Streak |  |
| Carta atrasada que cae / buzón equivocado | Golpe sordo (220 → 130 Hz), 0,34 s | MailGameController.Scene.cs:165, :209, :240, :272 (vol 0,5 a 0,6) | -15,0 / -25,5 | Wrong |  |
| «Esa carta no» / «aún no es la hora» | Tono suave que baja 392 → 330 Hz, 0,28 s | MailGameController.Scene.cs:175, :212, :244, :248, :287 (vol 0,4 a 0,5) | -20,1 / -30,7 | Knock |  |
| Encargo cumplido | Fanfarria de tres campanas (sol, si, re agudos), 1,6 s | MailGameController.Scene.cs:202, :230 (vol 0,8) | -8,0 / -15,3 | Perfect |  |
| Mirar el reloj: tic | Ruido y campanita, 0,4 s | MailGameController.Scene.cs:259 (vol 0,5) | -22,9 / -33,6 | Tick |  |
| La nave del correo: bocina grave de tres notas | Tres triángulos (146, 220, 293 Hz), 2,1 s | MailGameController.Scene.cs:231 (vol 0,6) | -10,5 / -17,9 | propio (Fase C): marimbas graves |  |
| «¡Llega un saco!» | Siseo que sube, golpe y dos campanitas, 0,9 s | MailGameController.Scene.cs:277 (vol 0,7) | -16,1 / -23,3 | propio (Fase C): SweepUp + Clack + dos notas |  |
| La radio | Dos tonos y un siseo, 0,7 s | MailGameController.Scene.cs:282 (vol 0,7) | -14,7 / -23,5 | propio (Fase C) |  |
| Empieza el día: zumbido (sol y re graves) | Dos senos, 1,6 s | MailGameController.cs:183 (vol 0,5) | -20,2 / -26,2 | Hum |  |
| La hoja de la mañana | Campana (sol), 0,9 s | MailGameController.cs:225 (vol 0,7) | -15,5 / -21,7 | Notice |  |
| Cierre del día: tres campanas que suben | Tres campanas, 1,6 s, con eco | MailGameController.cs:198 (vol 0,8) | -9,5 / -15,4 | LevelUp |  |
| «NUEVO» | Dos campanas (mi y la) | MailGameController.cs:298 (vol 0,7) | -14,5 / -21,7 | New |  |
| Fin de la partida | Dos campanas lentas (sol y do agudos), 2 s | MailGameController.cs:262 (vol 0,8) | -10,5 / -17,1 | Finish |  |

### Tinta o Palabra

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Aparece la tarjeta de la regla | PlayTone: 392 Hz, 0,14 s (seno + 2.º y 3.er armónico; ataque 15 ms; amplitud 0,1) | StroopGameController.cs:377 | -19,1 / -27,4 | Cue(392): marimba seca de ~0,1 s | señal |
| La regla pasa a «el color de la tinta» | PlayTone: 330 Hz, 0,1 s | StroopGameController.cs:1034 | -19,1 / -28,4 | Cue(330) | señal |
| La regla pasa a «lo que dice la palabra» | PlayTone: 440 Hz, 0,1 s | StroopGameController.cs:1034 | -19,1 / -28,3 | Cue(440) | señal |
| Práctica: «¡Listo!» | PlayTone: 523 Hz, 0,4 s | StroopGameController.cs:1182 | -21,0 / -28,0 | Notice |  |
| Acierto, error, subir de nivel y tic | Comunes (GameFeel) | StroopGameController.cs:287, :307, :212, :364 | -10,5 / -17,5 | Wrong (ver «Lo común») |  |

### Piloto Estelar

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Baliza dentro de la ruta: una nota de la escala | Campana corta (τ 0,1 s, 0,3 s), mono; pico ≈ 0,3 | PilotGameController.cs:268 (vol 0,6) | ver afinación | Note(hz): kalimba (o marimba) | info |
| Baliza FUERA de la ruta: zumbido grave | Dos senos (130 y 138 Hz) con ruido, 0,22 s | PilotGameController.cs:274 (vol 0,7) | -10,3 / -23,0 | Knock | info |
| Aparece una señal (el MISMO blip para todas) | Seno 880 Hz, τ 35 ms, 0,12 s | PilotGameController.cs:378 (vol 0,5) | -20,1 / -33,8 | Cue(880) | señal |
| Atrapar la señal de la misión (la racha sube la nota) | Campana de la escala (τ 0,25 s, 0,8 s) | PilotGameController.cs:447 (vol 0,8) | ver afinación | Correct(racha) o Note(hz) | info |
| Toque equivocado | Golpe sordo y corto (220 → 130 Hz), 0,3 s | PilotGameController.cs:460 (vol 0,7) | -13,7 / -24,7 | Wrong |  |
| Cruzas el arco de un sector | Soplido que sube 300 → 2400 Hz, 0,8 s | PilotGameController.cs:559 (vol 0,8); PilotGameController.Guided.cs:210 | -16,7 / -23,5 | SweepUp |  |
| La misión cambió: dos notas que suben (con el latido de la tarjeta) | Dos triángulos (mi6 y si6) a 0,4 s uno del otro, 1 s | PilotGameController.Scene.cs:71 (vol 0,85) | -13,1 / -18,8 | Note(hz) × 2 a 0,4 s | tiempo |
| Hiperimpulso | Destello de ruido que se abre y cuatro campanas que suben, 1,6 s | PilotGameController.cs:484 (vol 0,8) | -10,4 / -16,7 | Streak |  |
| Llegada al puerto: fin del vuelo | Dos campanas lentas, 2 s | PilotGameController.cs:604 (vol 0,8) | -10,5 / -17,1 | Finish |  |
| El motor «Cohete» (bucle de 4 s en tres capas) | Ruido café filtrado con turbulencia; ya aprobado por Ricardo (10-oct) | PilotEngine.cs (PilotGameController.Scene.cs:465 Tick) | — | SE QUEDA como está (Ricardo lo aprobó) | bucle |

### Freno de emergencia

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Se enciende un cohete: SEÑAL de salida | PlayTone: 784 Hz, 60 ms (seno + 2.º y 3.er armónico; amplitud 0,06) | BrakeGameController.cs:236, :1183 (práctica) | -23,5 / -34,0 | Cue(784) | señal |
| ¡ALTO!: la sirena de dos tonos (988, 740, 988 Hz) | Tres tonos de 90 ms cada 100 ms | BrakeGameController.cs:444-453 | -21,0 / -25,4 | Cue × 3 (mismas alturas y ritmo) | señal |
| Despegue acertado: rugido grave (con el acierto común) | PlayTone: 196 Hz, 0,25 s | BrakeGameController.cs:315 | -25,1 / -32,8 | propio (Fase C): se evalúa quitarlo (ya suena el Acierto común) |  |
| «¡Más rápido!» | PlayTone: 262 Hz, 0,12 s | BrakeGameController.cs:344 | -25,1 / -34,0 | Knock |  |
| Freno perfecto: resoplido (con el acierto común) | PlayTone: 147 Hz, 0,3 s | BrakeGameController.cs:359 | -25,1 / -32,8 | propio (Fase C): se evalúa quitarlo (ya suena el Acierto común) |  |
| Práctica: «¡Listo!» | PlayTone: 523 Hz, 0,4 s | BrakeGameController.cs:1255 | -21,0 / -28,0 | Notice |  |
| Acierto, error, subir de nivel y tic | Comunes (GameFeel) | BrakeGameController.cs:313/358, :331/379, :288/323/368, :421 | -10,5 / -17,5 | Wrong (ver «Lo común») |  |

### Satélites

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Se presentan los satélites con mensaje: una campana cada 0,12 s | Campana de la escala (τ 0,3 s, 1 s) | SatelliteGameController.cs:220 (vol 0,7); SatelliteGameController.Guided.cs:49 | ver afinación | Note(hz): kalimba (o marimba) | info |
| Los satélites giran: zumbido en bucle | Dos senos (110 y 165 Hz), bucle exacto de 1 s, volumen 0,12 | SatelliteGameController.cs:513, :619 (AudioSource en bucle) | -24,9 / -28,7 | SE QUEDA (bucle grave y suave); ver Fase C | bucle |
| Se detienen: «clac» | Seno 260 → 150 Hz y chasquido, 0,3 s | SatelliteGameController.cs:258 (vol 0,8) | -12,3 / -25,9 | Clack | tiempo |
| Marcar un satélite (cada marca sube) | Campana de la escala (τ 0,12 s, 0,5 s) | SatelliteGameController.cs:602 (vol 0,8) | ver afinación | Select(k) | info |
| Quitar una marca | Seno que baja 440 → 330 Hz, 0,2 s | SatelliteGameController.cs:586 (vol 0,7) | -19,1 / -31,8 | Unselect |  |
| Ya están todos marcados (un toque que no cuenta) | Seno 300 Hz, 0,2 s | SatelliteGameController.cs:590 (vol 0,6) | -23,0 / -35,7 | Knock |  |
| Acertar todo: arpegio | Cuatro campanas que suben, 1,5 s | SatelliteGameController.cs:355 (vol 0,8) | -8,0 / -15,2 | Perfect |  |
| Faltó alguno: tono suave que baja | Seno 392 → 330 Hz, 0,5 s | SatelliteGameController.cs:360 (vol 0,7) | -17,1 / -25,4 | Wrong |  |
| Una luz se enciende en el planeta | Campanita aguda que sube por la escala (τ 0,22 s) | SatelliteGameController.Scene.cs:229 (vol 0,7) | ver afinación | Spark(n) | info |
| Aviso de sorpresa | Dos campanas (mi y la) | SatelliteGameController.cs:464 (vol 0,7) | -14,5 / -21,7 | New |  |
| Fin de la partida | Dos campanas lentas (sol y do agudos), 2 s | SatelliteGameController.cs:399 (vol 0,8) | -10,5 / -17,1 | Finish |  |

### Rescate relámpago (ya en Madera)

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Una vuelta del haz del radar | Bloque de madera 1250 Hz + sala | RadarGameController.Scene.cs:328 (vol 0,8) | -17,9 / -32,9 | = Tick |  |
| El relámpago (qué cápsulas viste) | Ruido que se cierra y golpe grave | RadarGameController.cs:230 | -9,9 / -23,0 | propio |  |
| La estática | Sonajero suave | RadarGameController.cs:259 | -16,8 / -28,5 | propio |  |
| Marcar la 1.ª, 2.ª, 3.ª y 4.ª cápsula | Kalimba do5, re5, mi5, sol5 | RadarGameController.cs:507 (vol 0,8) | -9,0 / -16,1 | = Select(k) |  |
| Desmarcar | Kalimba apagada (392 Hz) | RadarGameController.cs:502 | -15,1 / -28,2 | = Unselect |  |
| El rayo tractor | Marimba que sube | RadarGameController.Scene.cs:481 | -16,8 / -23,9 | propio |  |
| Entra una cápsula en la nave | Marimba y bloque de madera (cuatro alturas) | RadarGameController.Scene.cs:531 | -11,1 / -19,9 | propio |  |
| Ronda perfecta | Marimba do-mi-sol-do y dos kalimbas | RadarGameController.cs:334 | -11,3 / -19,4 | = Perfect |  |
| Ronda con un error | Dos marimbas graves que bajan | RadarGameController.cs:340 | -14,3 / -22,1 | = Wrong |  |
| Lluvia de cápsulas | Ocho kalimbas que bajan | RadarGameController.cs:214 | -12,4 / -19,5 | propio |  |
| Viaje a la estación | Rayo rápido y una kalimba do6 | RadarGameController.cs:366 | -12,4 / -20,2 | propio |  |
| Fin de la partida | Acorde de marimba y kalimba larga | RadarGameController.cs:395 | -9,9 / -17,9 | = Finish |  |

### Carga exacta

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Elegir una celda | Campanita (la nota 3 de la escala), 0,5 s | CalculoGameController.cs:509, :515 (vol 0,7) | -15,7 / -26,6 | Select(2): mi5, la misma altura |  |
| Elegir una operación | Campanita (la nota 6), 0,5 s | CalculoGameController.cs:530 (vol 0,7) | -14,9 / -25,1 | Select(5): do6, la misma altura |  |
| Juntar dos celdas (la nota sube con cada paso) | Campana de la escala (τ 0,22 s) y un soplo de luz | CalculoGameController.cs:553 (vol 0,8) | -10,5 / -16,1 | Correct(paso) | info |
| Una jugada imposible | Golpe grave y blando, 0,34 s | CalculoGameController.cs:540 (vol 0,8), :571 (vol 0,6) | -12,5 / -23,0 | Knock |  |
| Deshacer o empezar de nuevo | Soplo corto que baja, 0,3 s | CalculoGameController.cs:593, :603 (vol 0,7) | -22,5 / -36,5 | Unselect |  |
| La pista de Nubi | Dos campanas (mi y la), 1,1 s | CalculoGameController.cs:624 (vol 0,8) | -12,3 / -19,5 | Hint |  |
| La carga está exacta: arpegio | Cuatro campanas que suben, 2 s, con eco | CalculoGameController.cs:581 (vol 0,9) | -4,3 / -11,2 | Perfect |  |
| Fin de la partida | Tres campanas que suben, 2 s | CalculoGameController.cs:337 (vol 0,8) | -11,4 / -17,2 | Finish |  |

### Engranajes

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Tocar «Arrancar» | Campanita (la nota 3), 0,4 s | EngranajesGameController.cs:590 (vol 0,6) | -17,1 / -28,4 | Tap |  |
| El motor arranca: zumbido grave | Triángulo 70 → 95 Hz, 0,7 s | EngranajesGameController.cs:184 (vol 0,6) | -18,6 / -26,9 | Hum |  |
| Cada paso de la cascada: clic de dientes y una nota que sube | Chasquido de 50 ms y campanita de la escala | EngranajesGameController.cs:361 (vol 0,9) | ver afinación | Select(k) | info |
| Un cambio (tocar el motor o una correa, o deshacer): «clank» de llave | Golpe metálico corto y campanita aguda | EngranajesGameController.cs:604 (vol 0,7) | -16,3 / -29,0 | Clack |  |
| Un cartel que no cumplió / una jugada que no era | Golpe grave y blando, 0,4 s | EngranajesGameController.cs:369 (vol 0,8), :613 (vol 0,4) | -12,5 / -23,0 | Knock |  |
| La antena: silbido de viento y campana aguda | Ruido 600 → 2600 Hz y campana, 1,2 s | EngranajesGameController.cs:404 (vol 0,8) | -15,2 / -22,1 | propio (Fase C) |  |
| La turbina: rugido que sube | Ruido 180 → 900 Hz con retumbo, 1,3 s | EngranajesGameController.cs:405 (vol 0,8) | -13,1 / -20,5 | propio (Fase C) |  |
| La compuerta y el elevador: motor eléctrico y golpe seco | Diente de sierra suave 90 → 140 Hz y golpe, 1,1 s | EngranajesGameController.cs:406 (vol 0,8) | -13,4 / -18,7 | propio (Fase C) |  |
| Acierto de la máquina: tres campanas que suben | Tres campanas, 2,2 s, con eco | EngranajesGameController.cs:376 (vol 0,8) | -10,5 / -15,5 | Perfect |  |
| La luz del cohete que se enciende (la nota sube con cada luz) | Campanita de la escala (τ 0,22 s) | EngranajesGameController.Scene.cs:808 (vol 0,8) | ver afinación | Spark(n) | info |
| El despegue: rugido que sube y cinco campanas | 3,8 s con eco | EngranajesGameController.cs:422 (vol 0,9) | -6,9 / -14,8 | propio (Fase C): rugido de ruido + Record |  |
| «NUEVO» | Dos campanas (mi y la) | EngranajesGameController.cs:315 (vol 0,7) | -14,5 / -21,7 | New |  |
| Fin de la partida | Tres campanas que suben, 2 s | EngranajesGameController.cs:238 (vol 0,8) | -11,4 / -17,2 | Finish |  |

### Acoplamiento (ya en Madera)

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| El módulo se acerca | Soplo de paso bajo 900 → 300 Hz | DockingGameController.cs:217 | -26,4 / -39,7 | ≈ SweepDown |  |
| Tocar «Encaja» o «Espejo» | Marimba apagada de 60 ms | DockingGameController.cs:503 | -25,0 / -38,2 | = Tap |  |
| El módulo se da vuelta (espejo) | Dos kalimbas que bajan | DockingGameController.cs:298 | -17,9 / -25,9 | propio |  |
| Encaja: golpe hueco con marimba | Thump + marimba + bloque de madera | DockingGameController.cs:304 | -12,4 / -22,6 | propio |  |
| Se enciende una luz (la nota sube con la racha) | Kalimba cuya nota sube | DockingGameController.cs:350 | -12,0 / -19,1 | ≈ Correct(racha) |  |
| Anillo completo | Arpegio de marimba y kalimba | DockingGameController.cs:356 | -10,8 / -18,7 | = Perfect |  |
| No era | Dos marimbas graves que bajan | DockingGameController.cs:312 | -15,1 / -22,8 | = Wrong |  |
| Fin de la partida | Acorde de marimba y kalimba larga | DockingGameController.cs:390 | -9,9 / -17,9 | = Finish |  |

### Aterrizaje Lunar (ya en Madera)

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Soltar la nave | Soplo de paso bajo 1400 → 300 Hz | LandingGameController.cs:259 | -23,9 / -38,8 | = SweepDown |  |
| Posarse | Golpe grave suave y bloque de madera | LandingGameController.cs:291 | -13,6 / -27,2 | = Clack |  |
| Sube la bandera | Kalimba (sol5) | LandingGameController.cs:325; LandingGameController.Guided.cs:69 | -15,9 / -22,0 | propio |  |
| Aterrizaje justo | Dos marimbas (mi5 y la5) | LandingGameController.cs:352 | -13,6 / -21,7 | ≈ Correct |  |
| Diana | Arpegio de marimba y kalimba | LandingGameController.cs:352 | -11,3 / -19,6 | = Perfect |  |
| Quedó lejos | Dos marimbas graves que bajan | LandingGameController.cs:352 | -15,9 / -23,6 | = Wrong |  |
| Se arma una cúpula | Acorde de marimba y kalimba | LandingGameController.cs:361 | -10,8 / -18,9 | ≈ Streak / LevelUp |  |
| Fin de la partida | Acorde de marimba y kalimba larga | LandingGameController.cs:389 | -9,9 / -17,9 | = Finish |  |
| El propulsor mientras baja (bucle de 4 s) | Ruido café pasado por un paso bajo de 420 Hz; ya aprobado | LandingGameController.cs:173; LandingGameController.Scene.cs:416 | — | SE QUEDA como está | bucle |
| La práctica usa los sonidos viejos | GameFeel.Correct y Wrong (marimba y «bu-bum») | LandingGameController.Guided.cs:51, :57 | -10,5 / -17,5 | Wrong |  |

### En la punta de la lengua

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Cada ficha que se coloca suena la nota de su casilla | Campana de la pentatónica (τ 0,22 s, 1,2 s) | PuntaGameController.cs:506 (vol 0,8), :673 (al completar, la siguiente) | ver afinación | Select(k) | info |
| Aparecen las fichas | Barrido de ruido que sube 600 → 2200 Hz, 0,55 s | PuntaGameController.cs:458 (vol 0,6) | -23,0 / -30,6 | SweepUp |  |
| Una ficha vuelve al banco | Soplo corto y bajo, 0,2 s | PuntaGameController.cs:500, :515 (vol 0,5) | -25,5 / -39,7 | Unselect |  |
| «Casi»: golpe grave y blando | Seno 220 → 130 Hz, 0,34 s | PuntaGameController.cs:535 (vol 0,8) | -12,5 / -23,0 | Wrong |  |
| Una ayuda: soplo que sube y campana grave | Ruido 300 → 1800 Hz y campana de 392 Hz, 1 s | PuntaGameController.cs:583 (vol 0,7) | -13,7 / -21,2 | Hint |  |
| La palabra salió: acorde que cierra | Tres campanas que suben, 1,6 s | PuntaGameController.cs:680 (vol 0,8) | -8,7 / -14,0 | Perfect |  |
| El lucero llega al cielo | Dos campanitas, 1,1 s | PuntaGameController.cs:1421, :1451 (vol 0,5) | -16,4 / -23,7 | New |  |
| Fin de la partida | Tres campanas que suben, lentas | PuntaGameController.cs:713 (vol 0,8) | -11,4 / -17,2 | Finish |  |

### Lluvia de meteoros

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Se destruye un meteoro | PlayTone: 330 Hz, 0,12 s, amplitud 0,03 (se suma al acierto común) | MeteorGameController.cs:520, :1250 | -29,5 / -38,3 | Cue(330) |  |
| Práctica: «¡Listo!» | PlayTone: 523 Hz, 0,4 s | MeteorGameController.cs:1212 | -21,0 / -28,0 | Notice |  |
| Acierto, error, subir de nivel y tic | Comunes (GameFeel) | MeteorGameController.cs:492, :499, :187, :466 | -10,5 / -17,5 | Wrong (ver «Lo común») |  |

### ¿Verdad o disparate?

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| Llega la frase: sintonía | Barrido de ruido que sube y se aclara, 0,3 s | DisparateGameController.cs:310 (vol 0,5) | -18,6 / -29,7 | SweepUp |  |
| Acierto: onda que sale de la antena (junto al acierto común) | Soplo que sube y campanita, 0,45 s | DisparateGameController.cs:587 (vol 0,45) | -21,2 / -29,8 | propio: se suma al Acierto común; en la Fase C queda solo el Acierto |  |
| Error: chasquido de estática (junto al error común) | Chasquido y golpe grave suave, 0,22 s | DisparateGameController.cs:622 (vol 0,45) | -13,7 / -28,7 | propio: se suma al Error común; en la Fase C queda solo el Error |  |
| Se perdió la señal | Ruido que se apaga y nota grave, 0,7 s | DisparateGameController.cs:668 (vol 0,5) | -15,6 / -24,3 | Wrong |  |
| Transmisión perfecta (10 seguidas) | Cascada de campanas altas, 1,8 s | DisparateGameController.cs:795 (vol 0,55) | -14,9 / -19,9 | Streak |  |
| Gracias por avisar (frase poco clara) | Dos marimbas suaves que suben | DisparateGameController.cs:520 (vol 0,5) | -15,6 / -25,0 | Notice |  |
| Acierto, error, subir de nivel, tic y fin | Comunes (GameFeel); no tiene Finale propio | DisparateGameController.cs:586, :621, :292/:794, :927 | -10,5 / -17,5 | Wrong (ver «Lo común») |  |

### La estrella intrusa

| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |
|---|---|---|---|---|---|
| La chispa pasa por una estrella sin palabra: nota que sube | Marimba suave (pico 0,3) y brillo; 0,4 s | IntrusaGameController.cs:885 (vol 0,55) | ver afinación | Note(hz) | info |
| La chispa pasa por una estrella CON palabra: nota más fuerte y brillante | Marimba fuerte (pico 0,5) con campana una octava arriba; 0,55 s | IntrusaGameController.cs:885 (vol 0,55) | ver afinación | Note(hz) | info |
| Se cierra la figura: acorde de cuatro notas | Cuatro campanas do-mi-sol-do con brillo, 1,3 s | IntrusaGameController.cs:832, :871 (vol 0,35 a 0,65) | -7,5 / -16,0 | Perfect |  |
| La estrella fugaz cae | Soplo brillante que baja y campanita, 0,95 s | IntrusaGameController.cs:758 (vol 0,55) | -20,3 / -26,0 | SweepDown |  |
| Error: cristal suave y apagado | Dos campanas graves que bajan, 0,6 s | IntrusaGameController.cs:486 (vol 0,5) | -14,0 / -21,1 | Wrong |  |
| El grabado: barrido cálido que sube | Ruido y seno 700 → 2500 Hz, 1,2 s | IntrusaGameController.cs:982 (vol 0,4) | -22,7 / -29,1 | SweepUp |  |
| «¿Qué las une?» acertada | Tres campanas que suben, 1 s | IntrusaGameController.cs:438 (vol 0,6) | -13,3 / -19,7 | Correct |  |
| Latido de las cuatro estrellas mientras se piensa | Golpecito grave suave, 0,35 s | IntrusaGameController.cs:397 (vol 0,5) | -18,1 / -28,6 | Tap |  |
| La opción no era: golpecito de madera sordo | Seno 180 Hz, 0,25 s | IntrusaGameController.cs:437 (vol 0,5) | -16,6 / -28,6 | Knock |  |
| Subir de nivel, tic y fin | Comunes (GameFeel); no tiene Finale propio | IntrusaGameController.cs:1055, :1171 | -7,6 / -15,4 | LevelUp (ver «Lo común») |  |

## 11. Fases C a E: lo compartido (paso 0, Tarea 133, 18-oct) y la página de la Fase C

Spec: `auditoria-fable/spec-sonido-CDE.md`. Esta tarea es SOLO infraestructura y la página de la Fase C: **ningún juego suena distinto** (ningún controlador se tocó; el APK suena igual).

**`WoodCore` (Games/Shared):** además del núcleo común, lo EXTRA de cada juego, que se pide ANTES de la cuenta regresiva, en el orden en que suena (lo primero que suena, primero) y se calcula de a poco detrás del núcleo con el mismo controlador oculto:
`Request(WoodSfx, arg)` (una variante de la paleta fuera del núcleo: Select 0-9, Spark 1-8, Perfect, Knock…), `RequestNote(hz, voz, tau)` (una nota con información; clave `note.kalimba.392.00.0.30`), `RequestCue(hz, segundos)` (una señal corta, en mono; `cue.784.00.0.060`) y
`Request(clave, receta, mono)` (un sonido PROPIO del juego, p. ej. `bodega.ratchet`). Para sonar: `Play(WoodSfx, arg, ui, gainDb)`, `PlayNote`, `PlayCue` y `Play(clave, gainDb)`; la ganancia es OPCIONAL y solo puede ser **0, −3, −6 o −9 dB** (otro valor: `ArgumentException`; nunca más fuerte que la receta, así el techo de −6 dBFS no cambia).
Si algo no está listo: una variante de la paleta o un propio **no suena y suma `Skipped`** (nunca un tirón por un adorno); una nota o una señal **sí se calcula en el momento** y suma `LateBakes` (la información del juego no se pierde). **El smoke exige `Skipped` = 0 y `LateBakes` = 0** en toda partida con cuenta regresiva y sonido.
Memoria: lo extra es de UN juego a la vez (`Warm(idDelJuego)` suelta lo del anterior con `ReleaseExtras`; el núcleo sigue estático; `ExtraBytes` ≤ 10 MB por juego). El presupuesto de cálculo sube de 5 a **8 ms por cuadro mientras corre la cuenta regresiva** (`CountdownRunning`, lo avisa `CountdownScreen`) y vuelve a 5 al empezar la partida.

**`WoodSounds`:** `MakeNote(hz, voz, tau = 0,3)` (Rastro necesita 0,2 s y la cascada de Engranajes 0,15 s: con la sala de Rescate, la nota cae a −20 dB a los 0,45 s con τ 0,2 y a 0,35-0,43 s con τ 0,15), `BakeJob(receta, clave, mono)` genérico (mismo clip que `ToClip`), `Recipe` con su **familia** (`WoodTier`); `SoundMeter.OnsetsMs` para las pruebas de los sonidos con varios golpes.
**Patrón por juego** (`XSounds.cs`): cada clase pasa a `partial` y suma `Request()` (lo que pide antes de la cuenta), `All()` (sus recetas propias, con clave y `mono`) y, si tiene propios, su enum y `MakeRecipe`: Bodega (`BodegaSfx`: llega, haz, abrir, volar, matraca) y Correo (`MailSfx`: bocina, saco, radio); Carga, Punta y Constelaciones no tienen propios. El cableado de los controladores (borrar `PlayClip`, las campanas y el altavoz propio) es la tarea de cada juego, con su página aprobada.
**Pruebas:** `WoodCoreTests` (+8), `WoodSoundsTests` (+2), `GameSoundsTierTests` (nueva: cada receta propia finita, con el pico en su nivel, empieza enseguida, dentro de la banda de su familia, la matraca con sus 14 clics a 110 ms, la bocina con sus inicios, `ExtraBytes` ≤ 10 MB y sin `Skipped` ni `LateBakes`), `SoundRulesTests` (el regex de «sonido propio del aviso» cubre `WoodCore`; las reglas (b) un juego cableado no suena por su cuenta, (c) ningún `PlayTone(` salvo la sirena de Freno y (d) las clases de sonidos son una lista de recetas rigen por juego — lista `Migrated`, hoy vacía —; y (e) dos sonidos por una respuesta fallan salvo las parejas permitidas) y el smoke.

**La página por fase** (`python tools/sonido/armar_escucha.py --fase C`): `docs/previews/sonido-escucha-C.html` (liviana, va a git) y, con `--incrustar`, `sonido-escucha-C-incrustado.html` (un solo archivo de unos 7 MB para el teléfono; no va a git). **«Hoy» sale CONGELADO del 17-oct** (`tools/sonido/catalogo-hoy-17-oct.json` es solo lectura; el exportador toma el WAV ya escrito y las medidas con `Frozen(clave)` en vez de renderizarlo, así «hoy» sigue siendo el sonido real aunque se borren las clases `XSounds`)
y «Madera» sale de las MISMAS recetas que van al juego. `Export` ya no borra todos los WAV al empezar. Cada evento del catálogo lleva su fase (`B` lo común, `C`, `D`, `E`).
**Fase C: 62 eventos** de los cinco juegos (Carga 10, Punta 9, Constelaciones 12, Bodega 15, Correo 16): las 37 notas de memoria en kalimba (con la marimba como tercer botón), los 8 sonidos propios nuevos y los tres cambios de criterio que Ricardo decide de oído (quitar el destello de Constelaciones, la bocina de fondo bajo la fanfarria del faro de Correo y la pista de las luces hermanas como «ayuda»).
Niveles medidos de los propios (LUFS con el volumen de reproducción; la spec estimaba ±2): llega −27,8, haz −31,2, abrir −38,1, volar −30,9, matraca −28,7, bocina −25,3, saco −25,0, radio −26,5.

## 12. Fase C cableada (Tarea 135, 19-oct) y la pista de Constelaciones

Los cinco controladores dejaron de sonar campanas propias: **Carga exacta** (celda `Select 2`, operación `Select 5`, juntar `Correct(paso)`, imposible `Knock`, «quedó X» `Knock` −3 dB, deshacer `Unselect`, «empezar de nuevo» armado `Unselect` −6 dB, pista `Hint`, carga exacta `Perfect`), **En la punta de la lengua** (las 10 notas de las casillas en kalimba con `PlayNote`; aparecen las fichas `SweepUp`, volver al banco `Unselect`, «casi» `Wrong`, ayuda `Hint`, palabra salió `Perfect`, el lucero al cielo `New` −3 dB; la cascada final de 7 notas queda como estaba: Ricardo la escuchó y NO se bajó), **Constelaciones** (11 notas de grupo en kalimba, `Tap` al dar vuelta una luz, `Unselect` al cerrarla, `Knock`, `Correct(racha)`, `Correct 1` −3 dB a la primera vista, `Perfect`, `Hum`, `New`; **SIN el destello `Spark`**, decisión de Ricardo), **Bodega de carga** (12 notas de escotilla en kalimba, `Hum`, los cinco propios `arrive`, `beam`, `open`, `fly` y `ratchet`, `Clack` −6 dB al cerrar, `Clack` al tomar o dejar, `Notice`, `Wrong`, `Spark` junto a la nota, `Perfect`, `New`) y **La estación de correo** (4 notas de buzón, `Spark` junto a la nota, `Streak`, `Wrong`, `Knock`, `Perfect`, `Tick`, `Hum`, `Notice`, `LevelUp`, `New`, los propios `horn` —el faro: fanfarria y bocina 8 LU más abajo, como Ricardo lo escuchó—, `sack` y `radio`). Cada clase de sonidos quedó como `Request()` (lo que se pide antes de la cuenta) + `All()` (sus recetas propias) y, en Bodega y Correo, su enum y `MakeRecipe`; las campanas de antes (`Bell`, `Make`, `Prewarm`) y `PlayClip` se borraron.
**La pista de Constelaciones («Las luces hermanas brillan») es la nota del grupo (Tarea 137, 19-oct):** a Ricardo el `Hint` de la paleta no le sonó a ayuda; la Tarea 135 la dejó sonando como hoy (las dos campanitas) y le ofreció tres opciones en `docs/previews/sonido-escucha-pista.html`. **Eligió la (a):** cada luz hermana (las que brillan 380 ms después de errar) suena la nota de SU grupo —la misma que sonó cuando la vio— en kalimba, suave (`ConstelacionSounds.HintGainDb` = −6 dB respecto de la nota normal, no más), UNA por luz en el momento en que brilla: con una pareja es una nota y con un trío son dos, 160 ms aparte (`ConstelacionContract.HintCues` / `HintStaggerMs`; las luces hermanas brillan de a una con ese mismo escalón, así la nota y el brillo coinciden). Es la misma receta de nota de siempre, ya pedida con `RequestNote` (nada se hornea en el momento). Se fue la receta temporal de las campanitas (`HintRecipe`, `HintKey`): Constelaciones ya no tiene sonidos propios. La página `sonido-escucha-pista.html` queda como registro, con la (a) marcada como «elegida»; en `sonido-escucha-C.html` el evento «las luces hermanas» ya muestra la opción (a) como su Madera.
**Las pruebas:** `SoundRulesTests` — los cinco juegos están en la lista de cableados (sin altavoz propio, sin tonos sueltos, clases de sonidos como listas de recetas) y una prueba nueva exige que **todo lo que un controlador cableado le suena a la paleta** esté pedido por adelantado o vaya con el núcleo; `CargaSoundTests` no hace falta (no tiene propios: lo cubre esa regla); `PuntaSoundTests`, `ConstelacionSoundTests`, `BodegaSoundTests` y `MailSoundTests` (las notas afinadas y que empiezan en < 3 ms, el pedido por adelantado, los propios que el controlador suena, y lo particular de cada juego: los retardos de la cascada, el destello fuera, la pista (nota del grupo, kalimba, −6 dB, una por luz hermana), la matraca de `SpinSeconds + 0,3`, la bocina ≥ 1,9 s).
**«Hoy» de la página sale CONGELADO:** `tools/sonido/catalogo-hoy-17-oct.json` (solo lectura; se le sumaron `punta.cascada.hoy`, `correo.faro.hoy` y los nombres de archivo de los siete propios nuevos que el 17-oct no se exportaban); las notas de cada familia salen del mismo archivo (`FrozenHoyNote`). La página `sonido-escucha-C.html` se regeneró desde las recetas cableadas (el grupo de Correo se llama «Correo Estelar»).
**La guardia de 16 dp del smoke** (`ScanTextPlacement`): mide la letra EN REPOSO, no a mitad de una animación de escala. Un flotante que aparece con un «pop» (0,94 → 1) medía 15,0 y 15,1 dp y fallaba a ratos (PantallaSatelites «×2 seguidos», PantallaCorreo «Otro buzón»); ahora el tamaño se revisa cuando la escala no cambió entre dos revisiones. El umbral (16 dp) no se tocó.

## 13. Fase D cableada (Tarea 138, 19-oct)

**Sin pausa de escucha** (decisión de Ricardo: «no me des la página, hazlos con tu criterio»): los cuatro juegos se cablearon directo con la spec (`auditoria-fable/spec-sonido-CDE.md` §3) y las decisiones de §8 ya tomadas; la página `docs/previews/sonido-escucha-D.html` (y su versión de un solo archivo `…-D-incrustado.html`, que no va a git) queda como REGISTRO de lo que suena. Una carpeta de juego por commit; lo que lleva información no cambió de altura, de orden ni de momento (las pruebas lo fijan).

**Rastro de luz** (`RastroSounds.cs`): cada lucero suena su nota en MARIMBA con τ 0,2 s (la campana de hoy caía 20 dB en 450 ms; con τ 0,3 las notas de un camino rápido se encimarían más) y la MISMA altura (`RastroBoard.Notes`, 392 a 1175 Hz), igual cuando la chispa llega y cuando el jugador toca (un solo nivel; antes 1,0 y 0,88); la ronda lograda es `Correct` que SUBE con el largo del camino (`RoundStep(asked) = clamp(asked − 2, 1, 7)`; decisión de Ricardo: una respuesta, no un `Perfect` en cada ronda), el error `Wrong`, «¡NUEVO!» `New` y el soplo del cielo `SweepUp`; cuatro propios `rastro.cue0 a cue3` (el aviso de cada modo: marimbas de τ 0,2 con las alturas y los instantes de hoy, nivel 0,20, familia «respuesta»). Pruebas: `RastroSoundTests` (alturas, afinación, inicio < 3 ms, caída 20 dB entre 370 y 560 ms según la nota, cada nota de cada aviso en su instante ±3 ms, `Request`, el `Correct` que sube).
**Satélites** (`SatelliteSounds.cs`): una campana de marimba por cada satélite con mensaje, cada `SatelliteContract.CueBellGap` = 0,12 s (el juego y el tutorial usan la misma constante; el tutorial las sonaba todas de golpe y ahora van una por una) con la altura de hoy (`Penta[2..6]`, τ 0,3 como hoy); marcar es `Select` (kalimba, el de Rescate) con la altura de hoy (`Select(n + 1)`: mi5 con la primera); quitar `Unselect`, «ya están todos» `Knock`, acertar todo `Perfect`, «faltó alguno» `Wrong`, el aviso de sorpresa `New`, el «clac» `Clack` y la luz que se enciende `Spark` en la MISMA escalera de cinco notas que sube y vuelve a empezar. Sin sonidos propios; el zumbido en bucle se queda (`_humSource`) y durante el seguimiento no suena nada más. Pruebas: `SatelliteSoundTests`.
**Piloto Estelar** (`PilotSounds.cs`): una nota de marimba por baliza dentro de la ruta (`Penta[1 + i % 4]`, la de hoy) y un golpe hueco (`Knock`) para la que se pasa fuera; el blip de las señales es el MISMO para todas por construcción (`PlayCue(880, 0,12, gainDb −6)`: un solo clip); atrapar la señal de la misión `Correct(racha)`, el toque equivocado `Wrong`, el arco de un sector `SweepUp`, el hiperimpulso `Streak`; un propio, `piloto.mission` (dos marimbas, mi6 y si6, separadas EXACTAMENTE `PulseBeatSeconds` = 0,4 s dentro de un solo clip). **El motor «Cohete» no se tocó** (`PilotSounds` conserva `EngineLoop`/`MakeEngineLoop`/`PrewarmEngine`; `PilotEngineTests` solo compara ahora el motor con el acierto y el aviso de la paleta: el motor queda ≈ 10 dB abajo, así que no hizo falta bajarlo; falta oírlo en el teléfono a velocidad alta). Pruebas: `PilotSoundTests`.
**La estrella intrusa** (`IntrusaSounds.cs`): las 9 notas de la chispa en marimba con la altura de hoy; la estrella CON palabra suena la misma nota 6 dB más fuerte que la que no la tiene (`PlayNote(step, strong)`: gainDb 0 contra −6, una sola receta por nota; decisión de Ricardo); al cerrar la figura `Perfect` (−6 dB en la variante tranquila), el error `Wrong`, «¿Qué las une?» acertada `Correct 3`, la opción que no era `Knock` y el latido mientras se piensa `Hum` a −3 dB (decisión de Ricardo; el plan decía `Tap`); dos propios: `intrusa.engrave` (el grabado) e `intrusa.fall` (la estrella fugaz, ver abajo). Pruebas: `IntrusaSoundTests`.

**Decisiones de Sonnet (criterio de Opus: un momento con sentido propio del juego no se queda en un evento genérico si queda casi inaudible):** (1) la estrella fugaz que cae en Intrusa es un propio (`intrusa.fall`: un soplo que baja de 4200 a 500 Hz y una kalimba sol6, nivel 0,14 ≈ −28 LUFS) y no `SweepDown`, que quedaba 9 LU más bajo que hoy (≈ −35 LUFS); es la alternativa que traía la spec. (2) En Satélites la luz que se enciende conserva el ciclo de cinco notas (`Spark(1 + luces % 5)`) en vez de `Spark(luces + 1)` con tope en 8, que a partir de la 8.ª luz sonaría siempre igual. (3) La marca de Satélites usa `Select(n + 1)` y no `Select(n)`: así la primera marca es mi5, la altura de hoy (la tabla de la spec decía «n = 1 → re5, la misma altura que hoy», que no coincide). (4) En Rastro y en las pruebas de avisos se mide cada nota en su instante (el detector de inicios puede marcar de más el batido de dos notas); los rangos de caída salen de lo medido (370 a 560 ms, media 473 ms contra los 450 de la campana). Lo demás es la spec tal cual.

**Pruebas compartidas:** `SoundRulesTests` — `Migrated` pasa a nueve juegos, la prueba de cobertura suma Rastro, Satélites, Piloto e Intrusa, y los archivos que se quedan con su altavoz (los tres bucles aprobados: `SatelliteSounds.cs`, `SatelliteGameController.Build.cs`, `PilotSounds.cs`, además de los que ya estaban) y `HarmonicTone.cs` (el generador de tonos de la sirena de Freno, que vive en la carpeta de Rastro) quedan exentos de la regla (b); `GameSoundsTierTests` recorre también los propios de Rastro (4), Piloto (1) e Intrusa (2). **La página** sale de `ExportBatch` + `python tools/sonido/armar_escucha.py --fase D`: «hoy» congelado del 17-oct (`catalogo-hoy-17-oct.json`) y «Madera» de las mismas recetas del juego; `Family_` acepta la caída (τ) y `FamilyEvent` la voz del juego como «Madera» (marimba en la Fase D, con la kalimba como tercer botón).

## 14. Fase E cableada y cierre del plan (Tarea 139, 19-oct)

**Sin pausa de escucha** (decisión de Ricardo: «hazlos con tu criterio»), igual que la Fase D: los cinco juegos se cablearon directo con la spec (`auditoria-fable/spec-sonido-CDE.md` §4) y las decisiones de §8 ya tomadas; la página `docs/previews/sonido-escucha-E.html` (y su versión de un solo archivo `…-E-incrustado.html`, que no va a git) queda como REGISTRO. Una carpeta de juego por commit.

**Tinta o Palabra** (`StroopSounds.cs`, nuevo): las tres señales (la tarjeta de la regla, 392 Hz y 0,14 s; el cambio de regla, 330 Hz «tinta» y 440 Hz «palabra», 0,1 s) pasan de `PlayTone` a `PlayCue` (marimba seca, casi sin sala) con la MISMA altura (±1 %) y la misma duración; **la altura ES la regla** y el inicio llega antes (0,5 ms contra 15 ms de ataque del tono viejo). Lo demás es `GameFeel`. **Lluvia de meteoros** (`MeteorSounds.cs`): el 330 Hz de «dejar pasar una inventada» se queda como `PlayCue(330, 0,12, gainDb −9)`: no está doblado con el acierto común (es el único sonido de ese acierto). **Freno de emergencia** (`BrakeSounds.cs`): la SEÑAL de salida (el estímulo del tiempo de reacción) es `PlayCue(784, 0,06, gainDb −6)`, misma altura, duración y nivel, con el inicio más rápido, y el juego la pide PRIMERO antes de la cuenta (nunca espera a calcularse); «¡Más rápido!» `Knock`; se QUITAN el rugido del despegue y el resoplido del freno perfecto (en la práctica suena el `Correct` de la paleta); **la sirena de «¡ALTO!» se queda tal cual con `PlayTone`** (988-740-988 Hz): es el único uso vivo de `PlayTone`/`HarmonicTone`, y `SoundRulesTests` (c) exige que en `BrakeGameController.cs` solo queden esos tres, dentro de `ShowStop`/`SirenTail`. **¿Verdad o disparate?**: `DisparateSounds.cs` desaparece entera; la sintonía es `SweepUp` a −3 dB, «se perdió la señal» `Wrong`, la transmisión perfecta SOLO `Streak` (se quita el `LevelUp` que sonaba encima), «gracias por avisar» el `Notice` del propio aviso (ya no va mudo) y se QUITAN la onda de la antena y la estática (acierto y error quedan solo con los comunes); lo único que no va con el núcleo (`SweepUp`, `Streak`) se pide desde `DisparateGameController.RequestSounds`. **Engranajes** (`EngranajesSounds.cs`): 14 propios: los diez pasos de la cascada (un clic de dientes y la nota de marimba de τ 0,15 con la altura de hoy, `Penta[profundidad + 1]`; el reloj de la cascada y `StepSeconds` no se tocan), la antena, la turbina, la compuerta y el elevador (tres golpes graves a 0,27 s, el motor, y un golpe seco al parar) y el despegue (rugido de 2,6 s, cinco kalimbas desde 1,0 s cada 0,14 s y un acorde de marimba: un HITO, no el `Record`); el resto es de la paleta (`Hum`, `Clack`, `Knock` y `Knock` a −6 dB, `Perfect`, `Spark` 1 a 8, `New`) y «Arrancar» es el toque del kit (`PlaysTap`), sin nota propia. Lo que pide Engranajes antes de la cuenta (27 extras) cabe en los 10 MB.

**Medidas de Freno y de Tinta:** la fecha del cambio de timbre de la señal de salida y de la regla (19-oct-2026) quedó anotada en `docs/medidas-juegos-estrella.md` (un tiempo de reacción antes y después podría moverse unos milisegundos; no se cambió ninguna regla ni ningún tiempo).

**Decisiones de Sonnet:** (1) En la práctica de Freno, el acierto que sonaba con el rugido y el resoplido ahora suena el `Correct(1)` de la paleta (el mismo del juego): quitar el sonido propio sin poner nada dejaba mudo el acierto de la práctica. (2) «Arrancar» sin plasticina (la losa del kit no disponible) suena `GameFeel.Tap()` a mano. (3) Los «hoy» de Disparate (onda y estática) y de Engranajes (antena, turbina, compuerta y despegue) no tenían WAV en la página del 17-oct (eran eventos sin Madera); se exportaron ANTES de borrar el código viejo y se anotaron en el catálogo congelado.

**CIERRE del plan (spec §7):** `SoundRulesTests` — `Migrated` tiene los 14 juegos del plan (Carga, Punta, Constelaciones, Bodega, Correo, Rastro, Satélites, Piloto, Intrusa, Tinta, Meteoros, Freno, Disparate y Engranajes), la regla (c) (ningún `PlayTone` suelto) rige para los 14 con la sirena de Freno como única excepción, una prueba nueva (`ThePlanIsClosed_…`) exige que ninguno tenga ya un `PlayClip` propio ni `DisparateSounds.cs`, y la de las clases de sonidos suma las de la Fase E. `PlayTone` solo vive en `GameControllerBase` por la sirena. Los tres juegos que ya estaban en Madera (Rescate, Acoplamiento y Aterrizaje, con `RadarSounds`) y los retirados de la app (Cosecha, Bitácora, Rumbo, con sus sonidos de antes) no entran en el plan. StyleStamp «estilo 19-oct · sonido E». **Pendiente anotado (no es barato, no es de sonido):** el código muerto del final propio de Rescate y de Satélites (`ShowEnd`, el confeti, `_endLayer` y sus etiquetas: nadie los llama desde la Tarea 129; ≈ 170 líneas entre `Build` y `Scene` de los dos juegos) se puede borrar en una tarea aparte con su verificación.
