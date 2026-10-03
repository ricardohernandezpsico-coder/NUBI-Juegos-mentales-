# «Quitar animaciones» de verdad — regla, componentes comunes e inventario (fases A y B)

> **Cambio de Chip se retiró el 3-oct (tarea 25):** sus filas de las tablas quedan como historia; la regla se aplica en los 22 juegos que quedan.

Tarea 18 (3-oct). Fase A: regla, componentes comunes e inventario. Fase B: la regla aplicada dentro de los 23 juegos (columna «Hecho» de las tablas).

## La regla

Con «quitar animaciones» activo:

1. Se QUEDA el movimiento ESENCIAL: el que ES la tarea o la información. Ejemplos: los meteoros que caen, los satélites a seguir, la
   nave que pilotas, las naves de Tráfico, las burbujas de Anagramas en los niveles 5-7 (son la dificultad), y la chispa que traza la
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
| `LivesHud` | El corazón se apaga con fundido (alfa 1 → 0,2, cambia al corazón vacío, 0,2 → 1), sin salto ni tambaleo. |
| `GameControllerBase.Flash` | Tinte con alfa ≤ 0,15 y fundido ≤ 0,2 s. |
| `GameControllerBase.AnimateResult` | Panel con fundido de opacidad y puntaje final de una vez (sin rebote ni cuenta). |
| `FinishCurtain` | «¡Listo!» y subtítulo con fundido, sin chispas, rebote ni hiperespacio; misma duración (≈1,2 s). |
| `PressScale` | No escala; el botón se oscurece 20 % mientras está apretado y vuelve a su color al soltar (la ficha hundida sigue igual). |
| `UiKit.PopRect`, `UiKit.PopIn` | Sin animación: la escala queda en 1. (Los usan casi todos los juegos: cubre muchos «pops» de golpe.) |
| `ExitButton`, `PauseMenu` | (Agregados: también eran comunes.) El botón aparece a su tamaño; el menú entra solo con fundido. |

Además: `GameClock.SimulatedDeltaTime` y `GameClock.RealDeltaTime` (solo para que las pruebas EditMode puedan avanzar las corrutinas).

**Misma duración (Fase B, regla 6).** Con ReduceMotion, `PopIn`, `PopRect`, `Shake`, `SparkBurst`, `RingBurst` y `ScaleTo` dejan el estado final quieto y luego ESPERAN la misma duración con `Motion.Hold(seconds)` (reloj de juego: respeta la pausa). `Flash` espera su duración total aunque el tinte dure ≤ 0,2 s, y `AnimateResult` espera sus 0,9 s. Así un `yield return StartCoroutine(PopIn(...))` dura igual con y sin la opción, y el ritmo entre rondas, los tiempos de reacción y el DDA no cambian. En los juegos, cada corrutina que se reemplaza (entradas, salidas, giros de carta, `CrossfadeState` de Acoplamiento…) espera su tiempo original con `Motion.Hold` o conserva su bucle. Prueba: `PopRect_PopIn_y_Shake_duran_lo_mismo_con_y_sin_ReduceMotion`, `Las_rafagas_con_ReduceMotion_esperan_su_duracion…` y `ScaleTo_con_ReduceMotion_espera…`.

**`ResultMark`** (nuevo, `Games/Shared/ResultMark.cs`): marca ✓/✗ de arcilla estática junto a una opción o carta. Se usa donde el acierto o el error dependían de color + sacudida/pop: Cálculo (✗ en la elegida y ✓ en la correcta), Series (igual) y Parejas (✗ en las dos cartas que no coinciden).

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

**Parejas Ocultas** — `Parejas/CardsGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 220-247 `AnimateBoardOut` | El tablero se encoge y gira 25° al cambiar de etapa | DECORATIVA | no | Cambio inmediato | ✓ |
| 887-902 `AnimateDealIn` (`EaseOutBack` propio, 561) | Las cartas se reparten con rebote | DECORATIVA | no | Aparecen a escala 1 | ✓ |
| 936 `AnimateMatchPop` | Pulso +16 % al emparejar | DECORATIVA (comunica) | no | Sin pulso; la carta ya cambia de estado | ✓ |
| 965 `AnimateWobble` | Tambaleo ±9° al fallar | DECORATIVA (comunica) | no | Sin giro; las cartas se voltean igual | ✓ |
| 1011-1026 `AnimateFlip` | La carta gira (escala X 1→0→1) | DECORATIVA (la exposición sí es la tarea) | no | Fundido entre dorso y cara con LA MISMA duración (2 × `FlipHalfSeconds`): el tiempo que la carta se ve no cambia | ✓ |
| 210-211 | `RingBurst`, `SparkBurst` | DECORATIVA | sí (común) | — | — |

**Ruta del Tesoro** — `RutaTesoro/TreasureGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 291 `PopGem` | La gema aparece con rebote | DECORATIVA (comunica) | no | Aparece de una vez | ✓ |
| 309 `HideGems` | Las gemas se encogen hasta desaparecer | DECORATIVA (comunica) | no | Desaparecen de una vez o con fundido | ✓ |
| 350 `CelebrateWave` | Ola de pulsos +10 % | DECORATIVA (comunica) | no | Tinte/color, sin escala | ✓ |
| 372 `WaveIn` / 387 `WaveOut` | Las fichas entran y salen con escala | DECORATIVA | no | Inmediato | ✓ |
| 202-204, 249-263 | Chispas, ondas, sacudida, destellos | DECORATIVA | sí (común) | — | — |

**Bitácora de Misión** — `Bitacora/BitacoraGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 741 `PopInDelayed` (propio) | Aparición con rebote escalonada | DECORATIVA | no | Aparición inmediata | ✓ |
| 521 (`Update`) | El aviso «busca aquí» respira (`Sin(t·7)`, +8 %) | DECORATIVA | no | Quieto | ✓ |
| 640-655 `DropTrail`/`FadeDot` | Puntos de estela de la sonda | DECORATIVA | no | Sin estela | ✓ |
| 682-683 `FlyFindToSlot` | El hallazgo vuela en arco al casillero | DECORATIVA (comunica) | no | Aparece en el casillero con fundido | ✓ |
| 622 `FlyProbe`, 716 `PlaceOnPlanet` | La sonda viaja entre planetas | ESENCIAL (muestra el orden de la ruta) | — | Se queda | — |
| 600 `UpdateComets` | Cometas que cruzan | ESENCIAL (decisión: son la «patrulla» que ocupa la atención durante la espera) | — | Se quedan | — |

**Rumbo a Casa** — `Rumbo/HomingGameController.cs`

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

**Correo Estelar** — `Correo/MailGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 457-458 | La nave se inclina con el movimiento y se tambalea ±14° al golpe | DECORATIVA | no | Sin inclinación ni tambaleo | ✓ |
| 572 | Los sobres se mecen ±8° | DECORATIVA | no | Quietos | ✓ |
| 620 | Brillo de planetas `Sin(t·4)` | DECORATIVA | no | Fijo | ✓ |
| 708-710 `FlyPackage` | El paquete vuela en arco, gira 360° y se encoge | DECORATIVA (comunica) | no | Aparece en el planeta con fundido | ✓ |
| 772 `BlinkRadio` | La radio parpadea al vencer el aviso | DECORATIVA (comunica) | no | Tinte fijo | ✓ |
| 777 | Brillo de la radio `Sin(t·3)` | DECORATIVA | no | Fijo | ✓ |
| 824-825 | Brillo bajo la nave `Sin(t·12)` / `Sin(t·5)` | DECORATIVA | no | Fijo | ✓ |
| 846 | Estela de motor | DECORATIVA | no | Sin estela | ✓ |
| 888 | Los asteroides giran | DECORATIVA (el desplazamiento es ESENCIAL) | no | Sin giro | ✓ |
| 969 | Escudo pulsa `|Sin(t·9)|` (≈ 2,9 Hz) | DECORATIVA (comunica) | no | Fijo / ≤ 2 Hz |
| 1058 `FloatText` | Textos que suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 1155 `PulseGo` | Botón «¡Ya!» respira | DECORATIVA | no | Quieto | ✓ |
| 540-570, 880-910 | Sobres/asteroides/planetas que se desplazan | ESENCIAL | — | Se queda | — |

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

**Satélites** — `Satelites/SatelliteGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 410-411 `Wobble` | Los cuerpos se mecen ±7° | DECORATIVA | no | Quietos | ✓ |
| 197-202 | Los anillos de las señales pulsan (`Sin(t·1,4·2π)`, +8 %) | DECORATIVA (el anillo es la señal; el pulso, adorno) | no | Anillo y resplandor fijos (la señal se ve igual) | ✓ |
| 521 `FloatText` | «+puntos» suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 434 `PlaceAll`, 454 `DrawTrails` | Los satélites en movimiento y sus estelas | ESENCIAL | — | Se queda (estelas: DUDA menor) | — |
| 180, 315, 326, 349, 400 | `PopIn`, `PopRect`, chispas | DECORATIVA | sí (común) | — | — |

**Radar** — `Radar/RadarGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 394-397 | El barrido gira | DECORATIVA | sí | — | — |
| 560-592 `FlyToCrew` | Los rescatados vuelan en arco a la tripulación | DECORATIVA (comunica) | sí | — | — |
| 634 `FloatText` | «+puntos» suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 244, 424, 512, 615 | `PopIn`, `PopRect` | DECORATIVA | sí (común) | — | — |

### Razonamiento

**Detective de Series** — `Series/SeriesGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 235-238 `PulseLens` | La lupa pulsa (`PingPong`) y «busca» girando ±8° sin parar | DECORATIVA | no | Quieta | ✓ |
| 337-343 `RowOut` | La fila se encoge | DECORATIVA | no | Inmediato | ✓ |
| 212-223, 258-316, 499 | `PopIn`, `PopRect`, `Flash`, chispas, ondas, `Shake` | DECORATIVA | sí (común) | — | — |

**Cálculo Sereno** — `Calculo/CalculoGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 149 | La burbuja flota ±10 px (`Sin(t·1,6)`) | DECORATIVA | no | Quieta | ✓ |
| 421 | Las olas del agua se mecen | DECORATIVA | no | Quietas | ✓ |
| 211 | La burbuja entra con rebote | DECORATIVA | no | Aparece quieta | ✓ |
| 317 `SinkBubble` | La burbuja se hunde (y se encoge 25 %) | DECORATIVA (comunica) | no | Fundido en su lugar | ✓ |
| 261-323 | `Flash`, ondas, `Shake`, `PopRect`, `PopIn`, chispas | DECORATIVA | sí (común) | — | — |

**Acoplamiento** — `Acoplamiento/DockingGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 165 | La pieza entra deslizando | DECORATIVA | no | Aparece (fundido) | ✓ |
| 182 | La pieza flota ±6 px (`Sin(t·3)`) | DECORATIVA | no | Quieta | ✓ |
| 315 `Straighten` / 331 `Flip` | Tras responder, la pieza se endereza o se da vuelta como espejo | DECORATIVA (comunica: enseña la solución) | no | Fundido cruzado a la pieza ya alineada / reflejada, misma duración total (`CrossfadeState`) | ✓ |
| 348 `Dock` | La pieza entra al puerto | DECORATIVA (comunica) | no | Aparece acoplada | ✓ |
| 375-386 `Bump` | Rebote al fallar | DECORATIVA (comunica) | no | Sin rebote; marca de error | ✓ |
| 400 `DriftAway` | La pieza se aleja flotando al fallar | DECORATIVA (comunica) | no | Fundido | ✓ |
| 490 `FloatText` | «+puntos» suben | DECORATIVA (comunica) | no | Quietos con fundido | ✓ |
| 355-361, 414, 436 | `Shake`, chispas, ondas, `PopIn`, `PopRect` | DECORATIVA | sí (común) | — | — |

**Tráfico Estelar** — `Trafico/TrafficGameController.cs`

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

**Anagramas** — `Anagramas/AnagramGameController.cs`

| Línea | Qué hace | Clase | Resp. | Cambio | Hecho |
|---|---|---|---|---|---|
| 126-147 `Update` | Las fichas se deslizan al lugar que les toca | ESENCIAL (es la acción del jugador) | — | Se queda | — |
| `BubbleField` (129, 133) | Las burbujas de los niveles 5-7 | ESENCIAL (dificultad) | — | Se queda | — |
| 149-151, 162-166, 267 | Respiración de la ficha, latido del resplandor, destellos | DECORATIVA | sí | — | — |
| 517-518 `FadeAll` | Las fichas y casillas se encogen al salir | DECORATIVA | no | Fundido | ✓ |
| 260-261, 303-339, 370-495 | `PopIn`, `Flash`, chispas, ondas, `Shake`, `PopRect`, `JumpWave` | DECORATIVA | sí (común) | — | — |

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

Decisiones aplicadas: Rumbo (cámara y polvo de estrellas) ESENCIAL; Acoplamiento `Straighten`/`Flip` = decorativo que comunica (fundido cruzado, misma duración);
Piloto `Warp` DECORATIVO (estrellas quietas; el aviso de tramo existente da los cambios de velocidad por texto); Parejas, giro de carta = fundido entre
dorso y cara con la misma duración; Cosecha, órbita decorativa (ya se detiene); Bitácora, cometas ESENCIALES; paralaje de la app quieto; «+puntos» quietos con fundido.

Criterio para el resto: ¿sin ese movimiento la persona puede hacer la tarea y entender qué pasó? Si sí, decorativo. Casos dudosos que dejé conservadores (se quedan en movimiento):
- **Cálculo, modo con reloj:** la burbuja que baja hacia el agua mientras corre el tiempo es el indicador del tiempo (se queda). Solo se quitó la flotación sin reloj, las olas y el hundimiento de después de responder.
- **Anagramas:** las fichas que se deslizan a su lugar (la acción de la persona) y las burbujas de los niveles 5-7.
- **Satélites:** las estelas de los satélites (ayudan a seguirlos; derivan del movimiento esencial).
- **Tráfico:** el giro de los desvíos y el avance de las naves; el aro urgente queda fijo pero sigue distinguiéndose por forma.
- **Bitácora:** los puntos de la estela de la sonda quedan (opacidad, sin encogerse): muestran la ruta recorrida.
- **Comparación/Stroop/Cambio de Chip:** el fundido de entrada de la tarjeta/ficha (opacidad, ≤ 0,24 s) se queda.
- **Piloto/Correo/Rumbo/Aterrizaje/Freno:** el movimiento de la nave, las rutas, los asteroides, los obstáculos y el descenso.
- **Acierto y error por forma o texto:** revisados los 23. Ya tenían texto (cartel, banner, aviso) o marca ✓/✗: Stroop, Comparación, Cambio de Chip, Secuencia, Ruta del Tesoro,
  Anagramas, Acoplamiento, Aterrizaje, Cosecha, Piloto, Radar, Satélites, Rumbo, Bitácora, Disparate, Freno, Tráfico, Correo, Meteoros, Intrusa. **Sí dependían de color + animación y
  recibieron una marca ✓/✗ estática (`ResultMark`): Cálculo, Series y Parejas.**

## Resumen (Fase B)

Las 102 filas DECORATIVAS con «Resp. = no» de la Fase A quedaron aplicadas (columna «Hecho» = ✓), salvo una por decisión: el polvo de estrellas de Rumbo pasó a ESENCIAL y no se tocó
(la cámara de Rumbo, también esencial, tampoco). Además se aplicaron las dudas que pasaron a decorativas (Parejas giro, Acoplamiento `Straighten`/`Flip`, Satélites anillo, Piloto `Warp`).
Decorativas sin respetar que quedan en los juegos: **0**.

## Tutorial guiado y «Cómo se juega» (3-oct, tarea 21a)

Rastro de luz, Freno de Emergencia, Aterrizaje Lunar y Lluvia de meteoros tienen tarjeta de Nubi + ronda guiada (`Games/Shared/GuidedTutorial`). Con «quitar animaciones»: la tarjeta entra y sale con fundido
(`Motion.FadeSeconds`), el aro sol punteado queda QUIETO (sin girar, `GuidedTutorial.SpinHint` mira `Motion.Decorative`) y los avisos de Nubi y la franja de Aterrizaje aparecen de una vez.
Las esperas del guion usan `Motion.Hold` (reloj de juego). Verificado con `--sin-animaciones` en los cuatro juegos.
