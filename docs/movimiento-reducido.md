# «Quitar animaciones» de verdad — regla, componentes comunes e inventario (fases A y B)

> **Cambio de Chip se retiró el 3-oct (tarea 25):** sus filas de las tablas quedan como historia; la regla se aplica en los 19 juegos que quedan (Engranajes, el 5-oct, nació con la regla) (Comparación Instantánea, Detective de Series, Ruta del Tesoro y Tráfico Estelar también se retiraron, el 4-oct: tareas 31, 33, 34 y 35).

Tarea 18 (3-oct). Fase A: regla, componentes comunes e inventario. Fase B: la regla aplicada dentro de los 23 juegos (columna «Hecho» de las tablas).

## La regla

Con «quitar animaciones» activo:

1. Se QUEDA el movimiento ESENCIAL: el que ES la tarea o la información. Ejemplos: los meteoros que caen, los satélites a seguir, la
   nave que pilotas, las naves de Tráfico, la ficha de En la punta de la lengua que vuela a su casilla, y la chispa que traza la
   constelación (se reemplaza por un fundido de 300 ms, como ya hace Intrusa).
2. Se QUITA todo lo DECORATIVO: titileos, vaivenes, órbitas, estrellas fugaces de fondo, respiración y pulsos en reposo, chispas,
   ondas, sacudidas, tambaleos, rebotes de escala (`EaseOutBack`), cámaras que se mueven solas y conteos animados de números.
3. Lo decorativo que COMUNICA algo (acierto, error, subir de nivel, final) se reemplaza por su estado final con un fundido de
   opacidad de 120-200 ms o un cambio inmediato, nunca por nada. El acierto y el error siguen viéndose (color más forma o texto),
   oyéndose y vibrando según sus ajustes.
4. Sin destellos de pantalla completa: un tinte suave estático (alfa ≤ 0,15, fundido ≤ 200 ms) o nada.
5. En TODOS los modos, también sin «quitar animaciones»: ningún parpadeo de más de 3 por segundo con cambio grande de brillo
   (WCAG 2.3.1). Si el brillo cambia mucho, se baja a ≤ 3 Hz o a una amplitud chica, y se anota en este documento.
6. La mecánica, los tiempos de la tarea, la dificultad y la puntuación NO cambian.

## De dónde sale el dato

- Teléfono → app: `UnityGameLauncher.kt` manda `reduce_motion` cuando `ANIMATOR_DURATION_SCALE == 0`.
- App → Unity: `GameEntryPoint.InitializeGameConfig` lo copia a `GameFeel.ReduceMotion`. **Esa es la única fuente de verdad en Unity.**
- `Games/Shared/Motion.cs` no duplica el flag: `Motion.Decorative => !GameFeel.ReduceMotion` más tres ayudas: `Fade`/`ColorTo` (fundido de
  opacidad o de color, 160 ms por defecto) y `ScaleTo` (escala con rebote, o la escala final de una vez con ReduceMotion).
- En la app (Compose): `ui/components/ReduceMotion.kt` → `rememberReduceMotion()` / `LocalReduceMotion` (ver más abajo).

## Componentes comunes de Unity (hecho en esta fase)

| Componente (`Games/Shared/`) | Con «quitar animaciones» |
|---|---|
| `UiFx.SparkBurst`, `RingBurst` | `yield break` antes de crear nada (cero hijos). |
| `UiFx.Shake` | No mueve; las posiciones de origen no se tocan. |
| `WorldBackdrop` (`WorldAmbient`, `CountdownAmbient`) | Estado de reposo dibujado UNA vez: alfa base, posiciones de casa, sin giro, sin órbitas, sin estrellas fugaces, letras flotantes quietas. Solo se redibuja si cambia el tamaño del área. |
| `StarfieldFx` | Reposo dibujado una vez (estrellas en su lugar de casa, sin estela, alfa base); `Warp` se ignora. |
| `CountdownScreen` | «3, 2, 1, ¡Ya!» con fundido cruzado (160 ms). Sin chispas, estrella en órbita, onda, destello central, rebote, respiración ni hiperespacio. Misma duración total. |
| `Toast` | Fundido de entrada y salida, sin caída ni rebote. |
| `PhasePill` | El texto nuevo entra con fundido (0,35 → 1 en 160 ms), sin rebote. |
| `ProgressDots` | El punto pasa a verde/rojo con fundido de color, sin pop. |
| `GameHud` | `CountUp`: número final de una vez. `LevelUpGlow`: el chip queda resaltado (estático) 0,45 s y vuelve con fundido. Racha: sin pop (y sin chispas, por `UiFx`). |
| `GameControllerBase.Flash` | Tinte con alfa ≤ 0,15 y fundido ≤ 0,2 s. |
| `GameControllerBase.AnimateResult` | Panel con fundido de opacidad y puntaje final de una vez (sin rebote ni cuenta). |
| `FinishCurtain` | «¡Listo!» y subtítulo con fundido, sin chispas, rebote ni hiperespacio; misma duración (≈1,2 s). |
| `PressScale` | No escala; el botón se oscurece 20 % mientras está apretado y vuelve a su color al soltar (la ficha hundida sigue igual). |
| `UiKit.PopRect`, `UiKit.PopIn` | Sin animación: la escala queda en 1. (Los usan casi todos los juegos: cubre muchos «pops» de golpe.) |
| `ExitButton`, `PauseMenu` | (Agregados: también eran comunes.) El botón aparece a su tamaño; el menú entra solo con fundido. |

Además: `GameClock.SimulatedDeltaTime` y `GameClock.RealDeltaTime` (solo para que las pruebas EditMode puedan avanzar las corrutinas).

**Misma duración (Fase B, regla 6).** Con ReduceMotion, `PopIn`, `PopRect`, `Shake`, `SparkBurst`, `RingBurst` y `ScaleTo` dejan el estado final quieto y luego ESPERAN la misma duración con `Motion.Hold(seconds)` (reloj de juego: respeta la pausa). `Flash` espera su duración total aunque el tinte dure ≤ 0,2 s, y `AnimateResult` espera sus 0,9 s. Así un `yield return StartCoroutine(PopIn(...))` dura igual con y sin la opción, y el ritmo entre rondas, los tiempos de reacción y el DDA no cambian. En los juegos, cada corrutina que se reemplaza (entradas, salidas, giros de carta, `CrossfadeState` de Acoplamiento…) espera su tiempo original con `Motion.Hold` o conserva su bucle. Prueba: `PopRect_PopIn_y_Shake_duran_lo_mismo_con_y_sin_ReduceMotion`, `Las_rafagas_con_ReduceMotion_esperan_su_duracion…` y `ScaleTo_con_ReduceMotion_espera…`.

**`ResultMark`** (nuevo, `Games/Shared/ResultMark.cs`): marca ✓/✗ de arcilla estática junto a una opción o carta. Se usa donde el acierto o el error dependían de color + sacudida/pop: Series (✗ en la elegida y ✓ en la correcta) y Parejas (✗ en las dos cartas que no coinciden).

## Parpadeos de más de 3 por segundo (regla 5)

| Dónde | Qué es | Frecuencia | Estado |
|---|---|---|---|
| `CountdownScreen` (línea ~364 antes) | `Sin(elapsed*30+i)` en el alfa de las 26 chispas de «¡Ya!» (±25 %) | 4,8 Hz | **Arreglado**: ahora `elapsed*12` (1,9 Hz). Con ReduceMotion no hay chispas. |
| `Freno/BrakeGameController.cs:539` `Blink` | La baliza de la vía se prende y apaga 3 veces cada 0,08 s | 6,25 Hz (0,5 s) | **Arreglado (todos los modos)**: el brillo oscila solo entre 1 y ≈ 0,67 (antes 1 ↔ 0,18). Con ReduceMotion queda prendida 0,48 s (misma duración) sin parpadeo. |
| `Freno:469` llama del cohete | Tamaño `0,8 + 0,25·Sin(t·60)` | 9,5 Hz | Decisión: cambia tamaño, no brillo, así que SE QUEDA en modo normal; con ReduceMotion el despegue no se dibuja (sin llama). |
| `Aterrizaje/LandingGameController.cs:174` llama | Tamaño `0,85 + 0,15·Sin(t·50)` | 8 Hz | Igual: se queda en normal; con ReduceMotion la llama no titila (fija, sigue creciendo al frenar). |
| `Rumbo/HomingGameController.cs:644` llama | `0,85 + 0,15·Sin(t·40)` | 6,4 Hz | Igual: se queda en normal; con ReduceMotion queda fija (encendida mientras avanza). |
| `Correo/MailGameController.cs:457` golpe | La nave se tambalea ±14° mientras dura el golpe | 6,4 Hz | Es rotación (no brillo): se queda en normal; con ReduceMotion no hay tambaleo (el tinte rojo del golpe sí). |
| `Correo:969` escudo (la nave parpadea mientras se repara) | `0,55 + 0,45·|Sin(t·9)|` | ≈ 2,9 Hz con amplitud grande | **Arreglado (todos los modos)**: `Sin(t·5,5)` ≈ 1,75 Hz. Con ReduceMotion queda semitransparente fija (0,7); el estado se sigue leyendo en el cartel. |
| `Disparate:495` temblor de la tarjeta | `Sin(t·70)·tremble` | 11 Hz | OK: posición (≤ 5 px) y ya se apaga con ReduceMotion. |
| `Piloto:214` aviso «¡A los mandos!» | Tinte ámbar `0,10 + 0,12·|Sin(3πk)|` en 1,1 s | 2,7 Hz | Bajo el límite; con ReduceMotion queda fijo (0,16). |
| `CambioChip/ChipGameController.cs:240` `PulseArenaBorder` (**hallazgo nuevo de la Fase B**) | El borde de la arena late 2 veces hacia blanco (mezcla 0,85) al cambiar de regla, 0,22 s cada pulso | 4,5 Hz | **Arreglado (todos los modos)**: pulsos de 0,34 s (≈ 2,9 Hz) y mezcla 0,6. Con ReduceMotion, tinte fijo sin latido ni escala. |

Revisados y sin problema (≤ 2,2 Hz): `Cosecha:759` (2,2 Hz, solo con palabra rápida), `Correo:824`, `Trafico:711/749` (1,4 Hz), `Bitacora:521`, `Intrusa:1106`, el fondo y las estrellas (≤ 0,5 Hz).
Los destellos de acierto/error de pantalla completa son de UN solo pulso por respuesta (alfa ≤ 0,14: un velo suave); no hacen falta cambios.

## La app (Compose) — qué hice (A4)

- **Prueba hecha:** `ReduceMotionComposeTest` dibuja `CosmosBackground` dos veces (0,5 s y 6 s) y compara las imágenes. Con la escala del sistema
  en 0 puesta en `Settings.Global` **el fondo SIGUE moviéndose en el entorno de pruebas** (la prueba de control con escala normal también se
  mueve, así que la prueba sí detecta movimiento). Como no puedo comprobar en un teléfono real si Compose congela las animaciones infinitas con
  la escala en 0, no confié en eso.
- **Entonces:** creé `ui/components/ReduceMotion.kt` (`LocalReduceMotion`, `systemReduceMotion(context)`, `rememberReduceMotion()`), que lee la
  escala UNA vez. Reemplacé las 6 copias sueltas de esa lectura (`NubiHome`, `BaselineScreen`, `GameLoadingScreen`, `GameResultScreen`,
  `AchievementOverlay`, `LeaguePromotionOverlay`) por `rememberReduceMotion()`. **No toqué ningún ajuste del teléfono de Ricardo.**
- **Lo que faltaba:** `CosmosBackground` (lo usan todas las pantallas) no miraba nada: ahora, con «quitar animaciones», no corre la transición
  infinita (cielo quieto, sin titileo ni olas) y se ignora `CosmosScroll` (el paralaje al deslizar es movimiento de fondo). `OnboardingScreen`:
  el cambio de página pasa de deslizar a solo fundido (160 ms).
- **Ya lo respetaban** (sin cambios de comportamiento): `NubiHome` (halo), `BaselineScreen` (pulso y crecimiento), `GameLoadingScreen`,
  `GameResultScreen`, `AchievementOverlay`, `LeaguePromotionOverlay`.
- Pruebas: `ReduceMotionComposeTest` (3): control con movimiento, fondo quieto con `LocalReduceMotion = true`, y `systemReduceMotion` lee 0 → true.

## Pruebas y verificación

- EditMode (`Games/Tests/MotionTests.cs`, 17 pruebas, ReduceMotion = true y se deja en false en el `TearDown`): ráfagas sin hijos, `Shake` quieto,
  `WorldBackdrop` y `StarfieldFx` sin cambios en 240 cuadros, sin estrellas fugaces, la cuenta regresiva termina sin chispas/órbita/onda/rebote,
  `FinishCurtain` termina, `PressScale` oscurece. Hay un control con ReduceMotion = false para probar que el fondo SÍ se mueve.
- Smoke: `NUBI_REDUCE_MOTION=1` (lo lee `HeadlessPlaymodeSmokeTest`, que además comprueba que la bandera llegó al juego).
  `bash tools/verificar-todo.sh --sin-animaciones` corre escena piloto + smoke de los 23 juegos con la bandera (no exporta, no corre Gradle, no instala).
- Sello de la cuenta regresiva: `estilo 3-oct · movimiento B` (Fase B).

## Inventario por juego (Fase B)

Columnas: **Clase** = ESENCIAL / DECORATIVA / DECORATIVA (comunica) / DUDA. **Resp.** = si hoy respeta ReduceMotion («sí (común)» = ya lo
cubre un componente de la sección anterior). El **cambio** es el de la regla. Las líneas son de hoy (3-oct). Lo de `FloatText` (los «+puntos»
que suben) es una función propia en cada juego. Bitácora y Tráfico tienen un `PopInDelayed` propio: no se
benefician de los componentes comunes en eso.

Lo clasifiqué leyendo las líneas clave y el comentario de cada rutina, no cada corrutina completa: donde dudé, la fila dice DUDA.

### Memoria

**Rastro de luz** (antes Secuencia Lumínica) — `Secuencia/RastroGameController.cs` (rehecho el 3-oct; docs/diseno-rastro-de-luz.md §7)

| Qué | Qué hace | Clase | Con «quitar animaciones» |
|---|---|---|---|
| Vuelo de la chispa de lucero en lucero | Muestra el orden del camino | ESENCIAL (es la tarea) | Se queda; en línea recta en vez de curva; misma velocidad y misma espera en cada lucero (`Motion.Hold`) |
| Curva del vuelo, partículas de la chispa y del dedo | Adorno | DECORATIVA | Sin curva ni partículas |
| Rastro de la chispa | Estela que se angosta y se apaga en ~1,5 s | DECORATIVA (comunica el camino) | Tramos rectos entre luceros que aparecen con un fundido de 300 ms |
| Cinta del dedo | Estela de luz que se apaga en 0,7 s | DECORATIVA | Sin cinta libre; quedan los tramos rectos acertados (fundido 300 ms) |
| Florecimiento del lucero | Anillo que se expande 650 ms | DECORATIVA (comunica) | Aro fijo que se apaga (sin crecer) |
| Respiración de los luceros y titileo | Adorno | DECORATIVA | Quietos |
| Vuelo de las chispas al contador | Premio de la ronda | DECORATIVA | El contador sube de una vez |
| **El cielo gira** | El tablero gira 60° a 180° y se repite sobre los mismos luceros | ESENCIAL (es la tarea) | **Se queda**, lineal (sin aceleración) y de 1,5 s |
| Pantalla «¡NUEVO!» | Aviso de un modo nuevo | DECORATIVA (comunica) | Aparece con un fundido corto; misma duración (2,3 s o un toque) |
| Aviso «Ahora: AL REVÉS» (cambio de modo) | Cartel de ~1,5 s con ícono y sonido propio | DECORATIVA (comunica) | Fundido corto; misma duración (o un toque) |
| Rótulo del modo al responder | Texto del color del modo arriba del tablero | Fijo, comunica | Sin cambios (no se mueve) |
| Tutorial (`GuidedTutorial`) | Tarjeta de Nubi y ronda guiada | — | Solo fundidos; el aro de ayuda no gira |

Probado con `verificar-todo.sh --sin-animaciones --juegos Run`.

**Constelaciones** (antes Parejas Ocultas, rehecha el 7-oct) — `Parejas/ConstelacionGameController*.cs` y `ConstelacionMotion.cs`

| Qué | Qué hace | Clase | Cambio con «quitar animaciones» |
|---|---|---|---|
| Las luces aparecen (escalonadas, 45 ms) | Crecen con un pequeño rebote | DECORATIVA | Aparecen de una vez, en el mismo orden y tiempo |
| El volteo de la luz | Resortes: abrir 2,6/0,72 (con rebote), cerrar 3,6/0,95 | DECORATIVA (comunica) | Cambio inmediato dormida ↔ abierta; **el tiempo que la luz queda abierta no cambia** (850 ms o hasta tocar otra) |
| Pulso al tocar | La luz se encoge un poco al presionarla | DECORATIVA | Sin pulso |
| Las luces se van al terminar el cielo | Se encogen | DECORATIVA | Desaparecen de una vez (espera mínima) |
| Líneas doradas / celestes | Se trazan; el brillo de 1,1 s tras 380 ms | DECORATIVA (comunica) | La línea ya está trazada; el brillo y el hilo punteado se muestran igual |
| Rótulos flotantes («¡De memoria!», «¡Falta la tercera!») | Suben mientras se desvanecen | DECORATIVA (comunica) | Sin subir, solo fundido |
| Chispas | Salen de la pareja encontrada | DECORATIVA | No se emiten |
| Tarjeta «NUEVO» | Fundido de 0,35 s; «toca para seguir» parpadea | DECORATIVA (comunica) | Aparece de una vez y sin parpadeo |
| La luz unida se encoge a estrella chica (0,58) y su línea pasa a trazo fino | A los 900 ms, en 500 ms | DECORATIVA | Cambio directo a los 900 ms, sin transición; al completar el cielo las líneas vuelven a brillar de una vez |
| Pausa entre cielos | 2,6 s | — | 1,8 s (sin animación que esperar) |

**Ruta del Tesoro (RETIRADA el 4-oct; su tabla queda como historia)** — `RutaTesoro/TreasureGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 291 `PopGem` | La gema aparece con rebote | DECORATIVA (comunica) | no | Aparece de una vez | ✓ |
| 309 `HideGems` | Las gemas se encogen hasta desaparecer | DECORATIVA (comunica) | no | Desaparecen de una vez o con fundido | ✓ |
| 350 `CelebrateWave` | Ola de pulsos +10 % | DECORATIVA (comunica) | no | Tinte/color, sin escala | ✓ |
| 372 `WaveIn` / 387 `WaveOut` | Las fichas entran y salen con escala | DECORATIVA | no | Inmediato | ✓ |
| 202-204, 249-263 | Chispas, ondas, sacudida, destellos | DECORATIVA | sí (común) | — | — |

**Bitácora de Misión** (retirada de la app el 5-oct; el código de Unity sigue) — `Bitacora/BitacoraGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 741 `PopInDelayed` (propio) | Aparición con rebote escalonada | DECORATIVA | no | Aparición inmediata | ✓ |
| 521 (`Update`) | El aviso «busca aquí» respira (`Sin(t·7)`, +8 %) | DECORATIVA | no | Quieto | ✓ |
| 640-655 `DropTrail`/`FadeDot` | Puntos de estela de la sonda | DECORATIVA | no | Sin estela | ✓ |
| 682-683 `FlyFindToSlot` | El hallazgo vuela en arco al casillero | DECORATIVA (comunica) | no | Aparece en el casillero con fundido | ✓ |
| 622 `FlyProbe`, 716 `PlaceOnPlanet` | La sonda viaja entre planetas | ESENCIAL (muestra el orden de la ruta) | — | Se queda | — |
| 600 `UpdateComets` | Cometas que cruzan | ESENCIAL (decisión: son la «patrulla» que ocupa la atención durante la espera) | — | Se quedan | — |

**Rumbo a Casa (RETIRADO de la app el 10-oct-2026; el código de Unity se conserva y esta tabla queda como historia)** — `Rumbo/HomingGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 575-591 `ApplyCamera` | La cámara gira y escala siguiendo el rumbo; los íconos contrarrotan | ESENCIAL (decisión: es la tarea de integrar el recorrido) | — | Se queda | — |
| 598 | Brillo de cristales `0,45 ± 0,15` (`Sin(t·4)`) | DECORATIVA | no | Fijo | ✓ |
| 629 | Brillo de la baliza `Sin(t·2,2)` | DECORATIVA | no | Fijo | ✓ |
| 639-640 | Anillo de señal pulsa ±8 % | DECORATIVA | no | Fijo | ✓ |
| 644 | Llama `Sin(t·40)` (6,4 Hz) | DECORATIVA | no | Fija | ✓ |
| 472 `PulseHome` | Resplandor del hogar sube y baja | DECORATIVA (comunica) | no | Tinte fijo | ✓ |
| 675 `UpdateDust` | El polvo de estrellas que envuelve la cámara (flujo óptico) | ESENCIAL (decisión: es la tarea) | — | Se queda | — |
| 835 `FloatText` | «+puntos» suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 363 | La nave del mapa gira con el rumbo | ESENCIAL | — | Se queda | — |
| 257-259, 427-435 | Chispas y ondas | DECORATIVA | sí (común) | — | — |

**Correo Estelar («La estación de correo», rehecho el 8-oct)** — `Correo/MailGameController*.cs` y `MailMotion.cs` (la tabla del vuelo anterior —nave que se inclina, asteroides, escudo— se fue con el código)

| Qué | Qué hace | Clase | Cambio con «quitar animaciones» |
|---|---|---|---|
| La cinta: rodillos y cartas que avanzan | Los rodillos giran; la carta se desliza hasta su lugar | DECORATIVA (la posición de cada carta es ESENCIAL y no cambia) | Rodillos quietos; las cartas siguen en su lugar |
| La carta vuela al buzón o a la caja fuerte | Arco corto con encogimiento | DECORATIVA (comunica) | Llega de una vez (0,001 s) |
| El buzón se sacude con una carta equivocada | Sacudida horizontal que se apaga | DECORATIVA (comunica) | Sin sacudida: el halo del buzón se muestra fijo |
| El buzón y la caja fuerte «respiran» al tocarlos | Pulso de escala 0,96 | DECORATIVA | Sin pulso |
| La caja fuerte brilla al guardar (el dial gira) | Brillo con giro del dial | DECORATIVA (comunica) | Brillo fijo |
| El faro: la luz barre, la nave del correo entra y deja el saco dorado | ~3,4 s de luz que gira y destellos que suben | DECORATIVA (comunica) | Luz fija hacia arriba, la nave quieta sobre la cinta, sin vuelo ni chispas; mismo texto y misma duración |
| «¡Llega un saco!», avisos de la radio | Fundido de entrada y salida | DECORATIVA (comunica) | Aparecen y se van de una vez, con la misma duración |
| Rótulos flotantes («Otro buzón», «¡Racha ×10!») | Suben ~18 dp mientras se desvanecen | DECORATIVA (comunica) | Sin subir, solo fundido |
| Avisos sobre la caja fuerte y el faro («¡Encargo cumplido!», «¡Faro encendido a tiempo!»…) | Salen en el hueco entre los buzones y los botones y solo se desvanecen (no suben, para no tapar los botones) | DECORATIVA (comunica) | Igual: solo fundido |
| Chispas y destellos | Salen del lugar del acierto | DECORATIVA | No se emiten |
| Resplandor del sello dorado | Late (~1 Hz) alrededor del sello | DECORATIVA (refuerza la señal; la forma dentada y el tamaño ya la distinguen) | Resplandor fijo, sin latido |
| Pantallas de hoja, resumen y final; tarjeta NUEVO | Fundido de 0,35 a 0,5 s | DECORATIVA (comunica) | Aparecen de una vez |
| El tiempo del día, los 1,6 s del reloj, la ventana de la hora | — | ESENCIAL (es la tarea) | No cambian |

### Atención

**Tinta o Palabra** — `Stroop/StroopGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 216 `PresentTrial` | La carta entra con escala 0,78 → 1 con rebote | DECORATIVA | no | Aparece (fundido 160 ms; el tiempo de reacción no cambia) | ✓ |
| 255-256 `ResolveTrial` | La carta sube 150 px y crece 8 % al acertar | DECORATIVA (comunica) | no | Queda quieta; el color/forma del acierto se mantiene | ✓ |
| 514-528 `FlipBanner` | El rótulo de la regla se voltea | DECORATIVA | no | Cambio inmediato con fundido | ✓ |
| 241-274 | `Flash`, chispas, ondas, `Shake`, `PopRect` | DECORATIVA | sí (común) | — | — |

**Cambio de Chip** — `CambioChip/ChipGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 216 | La ficha aparece con rebote | DECORATIVA | no | Aparece quieta | ✓ |
| 240-247 `PulseArenaBorder` | El borde de la arena pulsa 3 % al cambiar de regla | DECORATIVA (comunica) | no | Cambio de color de borde con fundido | ✓ |
| 281-282 `ResolveTrial` | La ficha avanza y crece 15 % hacia la zona | DECORATIVA (comunica) | no | Desaparece con fundido | ✓ |
| 427-440 `FlipBanner` | Rótulo que se voltea | DECORATIVA | no | Cambio inmediato | ✓ |
| 266-300 | Destellos, chispas, ondas, `Shake`, `PopRect` | DECORATIVA | sí (común) | — | — |

**Comparación Instantánea** — `Comparacion/ComparisonGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 184-198 `PresentTrial` | Las cartas entran deslizando con rebote | DECORATIVA | no | Aparecen en su lugar (fundido); el reloj de reacción no cambia | ✓ |
| 338 `ExitCards` | La elegida crece 10 %, la otra se encoge 6 % | DECORATIVA (comunica) | no | Sin escala | ✓ |
| 278-309 | Destellos, chispas, ondas, `Shake`, `PopRect` | DECORATIVA | sí (común) | — | — |

**Piloto Estelar** — `Piloto/PilotGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 212-214 `TakeControl` | «¡A los mandos!» entra con rebote y el tinte pulsa 2,7 Hz | DECORATIVA (comunica) | no | Texto con fundido y tinte fijo | ✓ |
| 309 | La nave en piloto automático se mece 4 % | DECORATIVA | no | Sin mecer | ✓ |
| 363 | La nave se inclina según su velocidad lateral | DECORATIVA | no | Sin inclinación | ✓ |
| 471 `UpdateSignals` | Las señales aparecen con rebote | DECORATIVA | no | Aparecen de una vez | ✓ |
| 640 `Vanish` | Las señales se encogen al desaparecer | DECORATIVA (comunica) | no | Fundido | ✓ |
| 668 `FloatText` | «+puntos» suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 707 | Brillo bajo la nave `Sin(t·5)` | DECORATIVA | no | Fijo | ✓ |
| 715-728 | Estela del motor | DECORATIVA | no | Sin estela | ✓ |
| 224-300 `Update`, ruta que baja | La nave, la ruta y las señales | ESENCIAL | — | Se queda | — |
| `WorldBackdrop.Hyperspace` | Las estrellas aceleran con la velocidad de vuelo (`Warp`) | DECORATIVA (decisión: el flujo óptico de pantalla completa molesta a quien es sensible al movimiento) | sí (común: `StarfieldFx` queda quieto) | Estrellas quietas; la nave, la ruta y las señales se siguen moviendo | ✓ |

**Freno de Emergencia** — `Freno/BrakeGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 523 `Hop` | El cohete salta con vaivén lateral (`Sin(k·30)`) | DECORATIVA | no | Quieto | ✓ |
| 457-469 `FlyAway` | El cohete despega con llama que titila (9,5 Hz) y se mece | DECORATIVA (comunica) | no | Desaparece con fundido; sin llama que titila | ✓ |
| 495 `Puff` / 508 `Steam` | Humo | DECORATIVA | no | Sin humo | ✓ |
| 539 `Blink` | La baliza parpadea 3 veces (6,25 Hz) | DECORATIVA (comunica) | no | Fija con fundido (y ≤ 3 Hz en todos los modos) | ✓ |
| 534 `PressButton` | El botón se encoge a 0,9 | DECORATIVA | no | Oscurecer (como `PressScale`) | ✓ |
| 609 `FloatText` | «+puntos» suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 290, 305, 354, 335, 401, 554, 570-571, 585 | `Shake`, `RingBurst`, `PopRect`, `PopIn`, chispas | DECORATIVA | sí (común) | — | — |
| 407 | El cartel de ALTO aparece YA (el retraso es la medida) | ESENCIAL | — | Se queda | — |

**Satélites** — `Satelites/SatelliteGameController*.cs`

> **Renovado el 9-oct (tarea 59).** Con «quitar animaciones» (`Motion.Decorative` en false): los satélites SIGUEN girando (es la tarea); el aro de la señal queda fijo (sin pulso); los sobres de los entregados no vuelan (se quedan y se apagan, y la luz aparece al terminar el tiempo
> del vuelo); las luces del planeta quedan fijas (sin pulso ni destello); sin chispas ni «pop» al marcar; el aviso de sorpresa aparece y se va sin fundido; el «¿Aquí se cruzaron?» aparece sin crecer. La tabla de abajo es del juego ANTERIOR y queda como historia.

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 410-411 `Wobble` | Los cuerpos se mecen ±7° | DECORATIVA | no | Quietos | ✓ |
| 197-202 | Los anillos de las señales pulsan (`Sin(t·1,4·2π)`, +8 %) | DECORATIVA (el anillo es la señal; el pulso, adorno) | no | Anillo y resplandor fijos (la señal se ve igual) | ✓ |
| 521 `FloatText` | «+puntos» suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 434 `PlaceAll`, 454 `DrawTrails` | Los satélites en movimiento y sus estelas | ESENCIAL | — | Se queda (estelas: DUDA menor) | — |
| 180, 315, 326, 349, 400 | `PopIn`, `PopRect`, chispas | DECORATIVA | sí (común) | — | — |

**Rescate relámpago (antes Radar)** — `Radar/RadarGameController*.cs` (rehecho el 9-oct, Tarea 62; sección 12 de `docs/diseno-rescate.md`; mundo y viaje a la estación de la v4, Tarea 65: secciones 16.4 y 16.7)

| Qué hace | Clase | Resp. | Con «quitar animaciones» |
|---|---|---|---|
| El haz gira y deja estela; los ecos sueltos | DECORATIVA | sí | Sin estela del haz ni ecos |
| Resplandor, onda y luz de pantalla del destello | DECORATIVA | sí | Sin resplandor, sin onda y sin luz en toda la pantalla; el destello se sigue viendo |
| La estática dentro del disco | ESENCIAL (es la máscara de la tarea) | — | **Se mantiene**, pero como imagen quieta, sin parpadeo |
| Polvo de fondo, llamas de la nave, salto de la nave, confeti | DECORATIVA | sí | Polvo quieto, llamas fijas, sin salto y sin confeti |
| Las cápsulas giran al alejarse | DECORATIVA (comunica) | sí | No rotan |
| El rayo tractor sube las rescatadas con chispas | DECORATIVA (comunica) | sí | Cono fijo, sin chispas |
| La baliza de la estación parpadea a 1 Hz, el humo sale del tramo roto, los restos flotan y giran (v4) | DECORATIVA | sí | Baliza fija encendida, sin humo, restos quietos |
| Al entrar una cápsula, su ventana lanza un aro y 6 chispas (v4) | DECORATIVA (comunica) | sí | Sin aro ni chispas; la ventana se enciende de golpe |
| El viaje a la estación: la nave sale llena por la derecha con líneas de velocidad y vuelve vacía por la izquierda (2,0 s; v4) | DECORATIVA (comunica) | sí | La nave no se mueve: a mitad de tiempo cambian las ventanas (10 por las que sobraron), sin líneas de velocidad |

### Razonamiento

**Detective de Series (RETIRADO el 4-oct; su tabla queda como historia)** — `Series/SeriesGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 235-238 `PulseLens` | La lupa pulsa (`PingPong`) y «busca» girando ±8° sin parar | DECORATIVA | no | Quieta | ✓ |
| 337-343 `RowOut` | La fila se encoge | DECORATIVA | no | Inmediato | ✓ |
| 212-223, 258-316, 499 | `PopIn`, `PopRect`, `Flash`, chispas, ondas, `Shake` | DECORATIVA | sí (común) | — | — |

**Carga exacta (id `calculo`, 4-oct; reemplaza a Cálculo Sereno, cuya tabla se borró)** — `Calculo/CalculoGameController.cs`

Escrito ya con la regla: todo movimiento decorativo pasa por `Motion.Decorative`. Con «quitar animaciones»: las celdas aparecen con un fundido corto (sin rebote ni subida desde abajo), la
celda elegida solo cambia de color (no sube ni crece, y no hay halo), al juntar dos celdas la primera se desvanece en su lugar (sin vuelo ni arco de luz) y la segunda cambia de número sin «pop» ni chispas,
el aro de luces del reactor no gira y su resplandor se apaga. SE QUEDAN: el cambio de número, el reactor DORADO al lograr la carga (con el número en tinta), el color dorado de la operación elegida, el contorno
dorado quieto de las dos celdas de la pista y los fundidos cortos de los mensajes. Las esperas de después de lograr la carga (1,7 s) son `Motion.Hold`: duran lo mismo con y sin la opción.

**Engranajes: Taller de reparación (id `engranajes`, 5-oct)** — `Engranajes/EngranajesGameController*.cs`

Escrito ya con la regla: todo movimiento decorativo pasa por `Motion.Decorative`. Con «quitar animaciones»: los engranajes NO giran, no hay pulso de luz, destello de cada engranaje, chispas, llama de la turbina,
aceleración, temblor del cartel que falló, latidos de la pista (el motor o la correa que late en las primeras máquinas, y el dorado de la solución), luz que vuela a la cabecera ni humo del despegue. SE QUEDAN: la flecha del
motor y la correa cambian de estado AL INSTANTE al tocarlas (sin el giro de 0,3 s ni los 0,32 s de la correa que se cruza), la barra de la compuerta y el elevador en su posición final (arriba o abajo), los carteles con
su veredicto (verde con ✓ o coral), lo que se marca al equivocarse (el brillo celeste de lo que movió tu cambio y el dorado de lo que había que tocar, quietos), las ventanillas encendidas, el aviso con su texto y los mismos
tiempos (la cascada, la espera de después —1,9 s si acertó y 4,8 s si no— y el despegue duran lo mismo: son esperas por reloj, `GameClock`, no animaciones). La tarjeta «NUEVO» y los avisos aparecen con un fundido corto; el
despegue, sin vuelo: el cohete simplemente ya no está y el texto «¡Despegue!» queda 1,2 s.

**Bodega de carga (id `bodega`, 5-oct)** — `Bodega/BodegaGameController*.cs`, `BodegaMotion.cs`

Escrito ya con la regla: todo movimiento decorativo pasa por `Motion.Decorative`. El «flow» (resortes amortiguados con el `dt` real) es movimiento decorativo: con «quitar animaciones» NO hay resortes, flotación del
robot, inclinación, antena con retraso, caja que se mece, chispas del haz, destello de la esclusa, brillo de la junta de las puertas, temblor de la escotilla equivocada, rebote del objeto al aparecer, sombra que respira
ni arco de luz: todo llega a su lugar AL INSTANTE (el robot, las puertas, la carga que va de la esclusa a su escotilla, la caja que cambia de lugar y el objeto que sale hacia el carro). SE QUEDAN: las escotillas
se abren y se cierran (el cambio de estado se ve), la caja se ve en su escotilla nueva, la bodega aparece YA girada al terminar el giro, el objeto se ve en su escotilla y en el carro, la escotilla correcta queda con su
brillo dorado quieto (la pista del error) y «vacía» se lee igual. Los tiempos de mirar NO cambian: guardar (0,33 + 0,33 + 0,42 + 0,95 s por objeto), cada caja (≈ 4 s), el giro (1,5 s) y las pausas del error
(0,75 + 0,25 + 0,7 s) son `Motion.Hold`, esperas por reloj (`GameClock`), no animaciones: sin esos tiempos la tarea cambiaría (los mayores los llevan ×1,35 aparte).

**Acoplamiento («muelle de acoplamiento», v2 del 10-oct)** — `Acoplamiento/DockingGameController*.cs` (la tabla de la v1, con `Straighten`/`Flip`/`Dock`/`Bump`/`DriftAway`/`FloatText`, ya no vale)

La revelación es una función del TIEMPO (`RevealT()`: giro 320, vuelta 300, bajada 380, espera 220, vuelo 520, alejarse 600 ms), no una cadena de animaciones: con «quitar animaciones» la pose salta de un paso al siguiente en los MISMOS tiempos (derecho → vuelto → en el puerto → en su casillero), así que la partida dura lo mismo.

| Qué | Clase | Con «quitar animaciones» |
|---|---|---|
| El módulo llega deslizando desde arriba y apareciendo (420 ms) | DECORATIVA | Aparece en su lugar al instante; el reloj de respuesta empieza cuando está en su lugar, con o sin animación |
| El módulo gira hasta quedar derecho, se da vuelta (si era espejo), baja al puerto, espera y vuela a su casillero (o se aleja) | DECORATIVA (comunica: enseña la solución) | Los pasos se ven como imágenes fijas, cada una durante su mismo tiempo; el borde del puerto (lima si encaja) y la insignia ✓ / ✗ del botón dicen el resultado sin movimiento |
| Las 4 luces («chevrones») del puerto parpadean alternadas cuando encaja | DECORATIVA | Quedan encendidas fijas |
| El aro blanco que se abre en el casillero recién ocupado (450 ms) | DECORATIVA | No sale (el casillero se ve ocupado) |
| El confeti de la pantalla final | DECORATIVA | No sale |
| Luz y sombra del módulo, anillos de la estación, botones, aviso de arriba | ESTADO | Igual (no se mueven solos) |

**Tráfico Estelar (RETIRADO el 4-oct; su tabla queda como historia)** — `Trafico/TrafficGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 314 | La baliza respira (`0,35 ± 0,3`, 0,6 Hz) | DECORATIVA | no | Fija | ✓ |
| 360-362 `StationLaunch` | La estación crece y pulsa al lanzar | DECORATIVA | no | Quieta | ✓ |
| 711-716 / 749-755 | Los anillos de las naves y de la cola pulsan (`Sin(t·9)`) | DECORATIVA | no | Fijos | ✓ |
| 590 `PopInDelayed` (propio) | Aparición con rebote escalonada | DECORATIVA | no | Aparición inmediata | ✓ |
| 801 `FloatText` | «+puntos» suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 774 `MarkAt` | La marca de acierto/error aparece creciendo | DECORATIVA (comunica) | no | Aparece quieta | ✓ |
| 670 `UpdateKnobs`, 637 `UpdateFlow`, 723 `DrawPods` | Los desvíos giran y las naves avanzan | ESENCIAL | — | Se queda | — |

**Aterrizaje Lunar** — `Aterrizaje/LandingGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 174 | La llama titila (8 Hz, ±15 % de tamaño) | DECORATIVA | no | Llama fija (y ≤ 3 Hz en todos los modos) | ✓ |
| 321 `PlaceLander` | El módulo se inclina | DECORATIVA | no | Sin inclinación | ✓ |
| 354 `PlantFlag` | La bandera crece con rebote | DECORATIVA (comunica) | no | Aparece de una vez | ✓ |
| 394 `Dust` | Polvo al aterrizar | DECORATIVA | no | Sin polvo | ✓ |
| 429 `FloatText` | «+puntos» suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 168-171, 267 | El módulo desciende y sigue el dedo | ESENCIAL | — | Se queda | — |
| 141-249 | `PopRect`, `PopIn`, chispas, ondas | DECORATIVA | sí (común) | — | — |

### Lenguaje

**En la punta de la lengua (antes Anagramas)** — `Anagramas/PuntaGameController.cs` (rehecho el 3-oct: tabla nueva, no la de la Fase A)

| Qué hace | Clase | Cambio |
|---|---|---|
| La ficha vuela a su casilla (resorte; forma corta con «quitar animaciones») | ESENCIAL (es la acción del jugador) | Se queda |
| Los tiempos entre pasos (`Motion.Hold`) | ESENCIAL | Se quedan |
| El texto de la definición que se escribe letra a letra | DECORATIVA | Aparece entero |
| Ondas de la señal, nebulosa antes de las ayudas, flotación de las fichas, rebote al aparecer | DECORATIVA | Se quitan |
| Temblor al fallar, chispas y resplandor de las letras que se encienden | DECORATIVA | Se quitan (la letra igual se enciende en su color) |
| El lucero que vuela al cielo | DECORATIVA | Aparece directo en su lugar, con su campanita |

**Lluvia de meteoros** — `Meteoros/MeteorGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 353 `MoveMeteors`, 600-601 caída | Los meteoros caen | ESENCIAL | — | Se queda | — |
| 332, 354, 600 | Estela corta, meteoro que se mece, sacudida del golpe | DECORATIVA | sí | — | — |
| 515 `RescueEffect` | El meteoro crece 25 % al rescatarlo | DECORATIVA (comunica) | no | Fundido | ✓ |
| 538 `StarFlight` | Estrellitas vuelan a la constelación | DECORATIVA (comunica) | no | La estrella de la constelación se enciende con fundido | ✓ |
| 619 `DissolveEffect` | Se encoge y cae | DECORATIVA (comunica) | no | Fundido | ✓ |
| 637 `MissedEffect` | Cae al pasarse | DECORATIVA (comunica) | no | Fundido | ✓ |
| 657 `MarkPop` | La marca ✓/✗ aparece con rebote | DECORATIVA (comunica) | no | Aparece quieta | ✓ |
| 676 `Dust` | Polvo | DECORATIVA | no | Sin polvo | ✓ |
| 706 `SetAlpha`/`FloatText` | Textos que suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 305, 429-558 | `PopIn`, chispas, ondas | DECORATIVA | sí (común) | — | — |

**¿Verdad o disparate?** — `Disparate/DisparateGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 677 `PlateBeat` | La tarjeta late +7 % al acertar | DECORATIVA (comunica) | no | Sin latido | ✓ |
| 575 | La tarjeta crece de 1,04 a 1,1 | DECORATIVA (comunica) | no | Sin escala | ✓ |
| 801 `FloatText` | «+puntos» suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 269-302, 385-386, 498, 560, 591-600, 636-644, 699, 760, 813-827 | Barrido, temblor, estática, inclinación, chispas, ambiente, transmisión perfecta | DECORATIVA | sí | — | — |
| 495-498 `UpdatePlateMotion` | La tarjeta se mueve con el dedo | ESENCIAL | — | Se queda (el temblor y la inclinación ya se apagan) | — |

**Cosecha de palabras** — `Cosecha/CosechaGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 321 `ReadyBetween` | El título entra con rebote | DECORATIVA | no | Aparece quieto | ✓ |
| 684 `FloatText` | «+puntos» suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 730 `AnimateOrbit` | Las fichas orbitan el planeta | DECORATIVA (decisión) | sí (se detiene) | Ya se detiene con ReduceMotion | — |
| 451, 476, 486-549, 574-594, 761-766 | `Shake`, brillo, siembra, brotes, ambiente | DECORATIVA | sí | — | — |
| 359-444 | `PopRect` | DECORATIVA | sí (común) | — | — |

**La estrella intrusa** — `Intrusa/IntrusaGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 381-393 `Bonus` | Las opciones aparecen con escala `EaseOutBack` | DECORATIVA | no | Aparecen quietas | ✓ |
| 1042 `FloatText` | «+puntos» suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 518, 671-693, 721, 782, 942, 1096-1110 | Pulso del aro, aparición, caída, traza, ambiente | DECORATIVA | sí | — | — |
| 538 `ShowTrapLine` | Los puntos recorren la línea de la trampa | ESENCIAL | — | Se queda | — |
| 782 `Trace` | La chispa que traza la constelación | ESENCIAL → fundido 300 ms | sí | — | — |
| 414, 848 | `PopRect` | DECORATIVA | sí (común) | — | — |

## Decisiones de la Fase B (Ricardo/Opus) y casos que quedaron conservadores

Decisiones aplicadas: Rumbo (cámara y polvo de estrellas) ESENCIAL; Acoplamiento (v1) `Straighten`/`Flip` = decorativo que comunica (fundido cruzado, misma duración; en la v2 del 10-oct la revelación se calcula por tiempo: ver su tabla);
Piloto `Warp` DECORATIVO (estrellas quietas; el aviso de tramo existente da los cambios de velocidad por texto); Parejas, giro de carta = fundido entre
dorso y cara con la misma duración; Cosecha, órbita decorativa (ya se detiene); Bitácora, cometas ESENCIALES; paralaje de la app quieto; «+puntos» quietos con fundido.

Criterio para el resto: ¿sin ese movimiento la persona puede hacer la tarea y entender qué pasó? Si sí, decorativo. Casos dudosos que dejé conservadores (se quedan en movimiento):
- **Carga exacta (antes Cálculo, modo con reloj):** ya no hay burbuja que caiga ni indicador animado de una carga: lo único con reloj es la barra de tiempo del Reto (un indicador de tiempo, se queda).
- **En la punta de la lengua (antes Anagramas):** las fichas que se deslizan a su lugar (la acción de la persona). Las burbujas se borraron.
- **Satélites:** (juego anterior) las estelas de los satélites (ayudaban a seguirlos). El juego nuevo no tiene estelas.
- **Tráfico:** el giro de los desvíos y el avance de las naves; el aro urgente queda fijo pero sigue distinguiéndose por forma.
- **Bitácora:** los puntos de la estela de la sonda quedan (opacidad, sin encogerse): muestran la ruta recorrida.
- **Comparación/Stroop/Cambio de Chip:** el fundido de entrada de la tarjeta/ficha (opacidad, ≤ 0,24 s) se queda.
- **Piloto/Rumbo/Aterrizaje/Freno:** el movimiento de la nave, las rutas, los asteroides, los obstáculos y el descenso. (Correo ya no vuela, 8-oct: sus cartas avanzan por la cinta, lo esencial.)
- **Acierto y error por forma o texto:** revisados los 23. Ya tenían texto (cartel, banner, aviso) o marca ✓/✗: Stroop, Comparación, Cambio de Chip, Secuencia, Ruta del Tesoro,
  Anagramas, Acoplamiento, Aterrizaje, Cosecha, Piloto, Radar, Satélites, Rumbo, Bitácora, Disparate, Freno, Tráfico, Correo, Meteoros, Intrusa. **Sí dependían de color + animación y
  recibieron una marca ✓/✗ estática (`ResultMark`): Series y Parejas (Cálculo Sereno también, hasta que se reemplazó por Carga exacta).**

## Resumen (Fase B)

Las 102 filas DECORATIVAS con «Resp. = no» de la Fase A quedaron aplicadas (columna «Hecho» = ✓), salvo una por decisión: el polvo de estrellas de Rumbo pasó a ESENCIAL y no se tocó
(la cámara de Rumbo, también esencial, tampoco). Además se aplicaron las dudas que pasaron a decorativas (Parejas giro, Acoplamiento `Straighten`/`Flip`, Satélites anillo, Piloto `Warp`).
Decorativas sin respetar que quedan en los juegos: **0**.

## Tutorial guiado y «Cómo se juega» (3-oct, tarea 21a)

Rastro de luz, Freno de Emergencia, Aterrizaje Lunar y Lluvia de meteoros tienen tarjeta de Nubi + ronda guiada (`Games/Shared/GuidedTutorial`). Con «quitar animaciones»: la tarjeta entra y sale con fundido
(`Motion.FadeSeconds`), el aro sol punteado queda QUIETO (sin girar, `GuidedTutorial.SpinHint` mira `Motion.Decorative`) y los avisos de Nubi y la franja de Aterrizaje aparecen de una vez.
Las esperas del guion usan `Motion.Hold` (reloj de juego). Verificado con `--sin-animaciones` en los cuatro juegos.

## Nubi entrenadora (`Games/Shared/NubiCoach.cs`, 4-oct)

Ficha del componente de los tutoriales (ver `docs/diseno-rastro-de-luz.md`, punto 6).

| Qué hace | Clase | Con «quitar animaciones» |
|---|---|---|
| El velo gris con hueco y el aro celeste | ESENCIAL (dice qué tocar) | Aparece con un fundido de 160 ms |
| El aro que late | DECORATIVA | Quieto |
| Nubi que entra deslizándose por el costado | DECORATIVA | Aparece con un fundido |
| El globo que salta y el dedo que señala cuando insiste (a los 5 s) | DECORATIVA | El globo no salta y el dedo queda quieto |
| El juego congelado mientras hay un foco de «tocar» (`GameClock`) | ESENCIAL | Igual |
