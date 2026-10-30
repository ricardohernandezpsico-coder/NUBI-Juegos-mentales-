# La cuenta 3-2-1 y lo que se ve antes de jugar: diagnóstico (Tarea 189, 28-oct-2026)

Solo diagnóstico e inventario: **no se cambió código de la app ni de Unity** (el registrador de cuadros fue temporal; está guardado como parche en [`tools/medicion-cuenta/`](../tools/medicion-cuenta/) para repetir la medición en la T190). Nada se ejecutó en el teléfono de Ricardo.

Dos pedidos de Ricardo: (1) «el 3-2-1 antes de jugar se ve a veces lageado o pegado; la estética no cumple para publicar»; (2) «hay juegos que tienen un tutorial y luego, al iniciar, aparecen nuevamente unas láminas explicando el juego: es tautológico y mal diseñado» (su regla: tutorial solo la primera vez y después nunca más, salvo a pedido).

## 0. En pocas líneas

1. **Lo que pega la cuenta NO es la cuenta**: es el trabajo de armado que corre debajo de ella, en el hilo principal y sin presupuesto. Medido en el Editor (PC, sin gráficos): trozos de armado de sprites de **0,1 a 3,2 s** en un solo cuadro (Correo 3,2 s; Engranajes 0,94 s; Rescate 0,33/0,29/0,14/0,11 s; Aterrizaje 0,27 s; Acoplamiento 0,30 s; Bodega 0,21 s), y la síntesis de sonido que gasta 8 ms por cuadro MÁS pasos que no se pueden cortar de 50 a 92 ms: **cada cuadro de la cuenta dura 17-22 ms (p95: 28-38 ms) en vez de 16,7 ms**, con tirones de 50-60 ms cada 2 o 3 cuadros.
2. El teléfono no se midió: allí todo esto será más lento (cuánto, no se sabe) y se suma lo que el Editor sin gráficos no ve (subir texturas, dibujar el lienzo).
3. **Antes de mostrar el tablero entero hay entre 1,0 y 5,3 s de trabajo en el PC** (tabla de §1.3). Si la T190 mueve todo eso detrás de la pantalla de carga sin reducirlo, la carga durará varios segundos en el teléfono: hay que reducir el trabajo (§3) además de moverlo.
4. **Lo que se explica dos veces** son sobre todo las tarjetas «NUEVO» (Constelaciones, Bodega, Correo, Engranajes) y, en menor medida, la «PALABRA» de Tinta o Palabra y el pre-roll de Freno. Y la pantalla de carga de la app repite la instrucción larga en cada arranque en frío. Detalle en §2.
5. Aparte: **la marca de versión «avisos en la fila 26-oct» se ve en la app publicada** (`ShowStyleStamp = true`); y seis juegos nunca fijan 60 cuadros por segundo (§1.4).

## 1. Parte 1 · Por qué se pega la cuenta

### 1.1 Cómo se midió

Un registrador temporal (`FrameProbe`, `tools/medicion-cuenta/`) anotó, desde que llega la configuración de la partida (`GameEntryPoint.InitializeGameConfig`) hasta 1 s después de la cuenta: el periodo de cada cuadro, la fase (armado, 3, 2, 1, «¡Ya!», partida), lo que gasta el cálculo de sonido de `WoodCore` por cuadro y por paso, las recolecciones de basura, el tiempo de cada trozo de armado de sprites (`Prewarm` de cada juego), de `ClayRaster` y de la carga de atlas. Cuatro corridas del smoke headless de Unity (`HeadlessPlaymodeSmokeTest.RunList`, Unity 6000.0.84f1, Editor sin gráficos, cuenta de 3 pasos):

1. los 17 juegos una vez cada uno (el primero es «frío»);
2. cinco juegos DOS veces seguidas (para separar el costo de compilar al vuelo del Editor del costo real: **los picos se repiten idénticos la segunda vez, así que no son del Editor**);
3. nueve juegos con el tiempo de `ClayRaster`;
4. nueve juegos con el tiempo de cada trozo de armado de sprites.

**Límites.** (a) Es un PC, no el Motorola; (b) sin gráficos: no cuenta subir texturas a la GPU ni dibujar; (c) en el Editor los clips de sonido se destruyen al salir de Play, así que cada corrida «caliente» vuelve a hornear el núcleo completo (en el teléfono el núcleo se hornea UNA vez por proceso de Unity, al abrir el primer juego); (d) la cuenta del smoke es la de 3 pasos (3,3 s); en el teléfono casi siempre es la de 2 (2,6 s).

### 1.2 Resultados por juego (PC, Editor sin gráficos)

| Juego | 1.er cuadro (Warm + armado) | Pico en la cuenta | Cuadros ≥ 25 ms | Sprites: total (trozo máx) | Sonido: clips · CPU total · paso máx | GC (Gen0) |
|---|---|---|---|---|---|---|
| Rastro de luz (frío) | 134 + 381 = 515 ms | 56 ms | 17 | — | 34 · 1276 ms · 53 ms | 48 |
| Constelaciones | 139 + 200 = 339 ms | 63 ms | 19 | 35 ms (34) | 34 · 1548 ms · 57 ms | 38 |
| Bodega de carga | 123 + 300 = 423 ms | 53 ms | 19 | 235 ms (206) | 46 · 1892 ms · 50 ms | 51 |
| Correo Estelar | 160 + 174 = 334 ms | **3524 ms** | 23 | **3223 ms (3208)** | 39 · 1766 ms · 52 ms | 44 |
| Tinta o Palabra | 131 + 102 = 233 ms | 54 ms | 6 | — | 22 · 762 ms · 51 ms | 25 |
| Piloto Estelar | 124 + 107 = 232 ms | 59 ms | 23 | 1 ms (0) | 28 · 1054 ms · 55 ms | 37 |
| Freno de Emergencia | 128 + 210 = 338 ms | 53 ms | 14 | — | 21 · 748 ms · 50 ms | 29 |
| Satélites | 126 + 130 = 255 ms | 54 ms | 28 | 27 ms (22) | 41 · 1641 ms · 50 ms | 44 |
| Rescate relámpago | 124 + 117 = 241 ms | **306 ms** | 20 | **860 ms (325)** | 19 · 706 ms · 51 ms | 54 |
| Carga exacta | 126 + 128 = 254 ms | 54 ms | 11 | — | 25 · 981 ms · 50 ms | 30 |
| Engranajes | 123 + 306 = 428 ms | **975 ms** | 25 | **1148 ms (936)** | 46 · 1951 ms · **92 ms** | 71 |
| Acoplamiento | 125 + 306 = 431 ms | 63 ms | 23 | 486 ms (299) | 19 · 713 ms · 51 ms | 39 |
| Aterrizaje Lunar | 121 + 106 = 227 ms | **291 ms** | 15 | 355 ms (273) | 19 · 717 ms · 52 ms | 32 |
| En la punta de la lengua | 124 + 158 = 282 ms | 57 ms | 22 | — | 34 · 1406 ms · 54 ms | 38 |
| Lluvia de meteoros | 125 + 335 = 461 ms | 176 ms | 12 | 117 ms (ClayRaster, 1.er cuadro) | 20 · 733 ms · 52 ms | 27 |
| ¿Verdad o disparate? | 118 + 236 = 354 ms | 52 ms | 19 | — | 21 · 794 ms · 49 ms | 28 |
| La estrella intrusa | 123 + 183 = 305 ms | 63 ms | 28 | — | 33 · 1348 ms · 60 ms | 38 |

«Pico en la cuenta» = el cuadro más largo entre el 3 y el 1 (sin contar el primer cuadro). «Warm» = el horneo SÍNCRONO de los 4 clips de la cuenta (`WoodCore.EarlySet`), «armado» = crear el controlador y su interfaz (`StartSession`), ambos en el primer cuadro. Un cuadro normal a 60 fps dura 16,7 ms. Los picos grandes son TROZOS ENTEROS de armado de sprites (`while (sprites.MoveNext()) yield return null;`: un trozo por cuadro, sin presupuesto de tiempo) y se repiten idénticos en cada partida de ese juego.

### 1.3 Causas, por tamaño

**A. Armado de sprites procedurales dentro de la cuenta (el «pegado»).** Nueve juegos (Acoplamiento, Aterrizaje, Bodega, Correo, Engranajes, Constelaciones, Piloto, Rescate, Satélites) hornean sus sprites con una corrutina `Prewarm()` durante la cuenta, un trozo por cuadro; un trozo puede juntar varios sprites y el que más tarda es de 0,2 a 3,2 s. Lo hornea `ClayRaster.RenderClay` (un píxel a la vez, con funciones de distancia) y las clases `*Sprites` de cada juego, todo en el hilo principal. Los trozos medidos: Correo 3208 ms (cuadro 21, la cuenta se queda en «3» más de 3 s); Engranajes 936 + 130 + 60 ms; Rescate 325 + 287 + 138 + 108 ms; Aterrizaje 273 + 74 ms; Acoplamiento 299 + 147 + 40 ms; Bodega 206 ms. Meteoros (117 ms) y otros arman sus sprites en el primer cuadro, sin `Prewarm`.

**B. Síntesis de sonido (`WoodCore`) debajo de TODA la cuenta.** El núcleo común (19 clips) y lo extra de cada juego (de 0 a 27) suman **0,7 a 1,95 s de CPU por juego**; el controlador oculto avanza el cálculo 8 ms por cuadro durante la cuenta (5 en la partida), así que cada cuadro de la cuenta dura 17-22 ms (p95 28-38 ms) en vez de 16,7 ms. Y el presupuesto se REBASA: cada paso de cálculo (un clip, un pedazo de su sala o su cierre) no se puede cortar y dura de **10 a 92 ms**; se mide después de cada paso. Con 1,9 s de trabajo (Bodega, Correo, Engranajes) y 8 ms por cuadro hacen falta 240 cuadros (4 s): **la cuenta de 2 pasos (2,6 s) termina antes de que acabe el horneo en estos juegos** y sigue en los primeros segundos de la partida, a 5 ms por cuadro (y si un sonido con información no está listo, se hornea en el momento: 10-30 ms de tirón; el smoke exige que no pase). Cada cambio de juego suelta lo extra del anterior (`ReleaseExtras`): quien juega tres juegos distintos al día vuelve a hornear lo extra de cada uno **en cada cuenta**. Además, durante la cuenta hay una recolección de basura de Gen0 cada 2 o 3 cuadros (25-71 en ~4 s): el horneo reserva arreglos grandes que no se reutilizan.

**C. El primer cuadro: 230-515 ms** (Warm síncrono de ~125-160 ms + armado de 100-380 ms) y, en algunos juegos, un segundo cuadro de 260-330 ms (Rastro, Acoplamiento, Rescate). En el teléfono eso pasa bajo la pantalla de carga… pero la capa de Android se quita **tres cuadros después** de `InitializeGameConfig` (`NotifyShownAfterFirstFrames`), es decir, justo cuando empieza el fundido de la cuenta y cuando caen los trozos de la causa A. La capa se desvanece (280 ms) mientras Unity se atasca.

**D. La pantalla de la cuenta (no medido; por lectura de `CountdownScreen`).** Es un hijo del único `Canvas` del juego (`new CountdownScreen(canvasGo.transform, …)`), con ~140 imágenes que se tocan cada cuadro (90 estrellas, 32 destellos, tres nebulosas de 1730-1950 unidades, un halo de 1150, la onda y el estallido): cada `color`, `localScale` o posición que cambia ensucia el lienzo y lo vuelve a armar entero, con mucho solapamiento de transparencias. El Editor sin gráficos no ve ni el armado de la malla ni el relleno de pantalla; en el teléfono probablemente cuentan. Las cuatro etapas de la cuenta se avanzan con la suma de `Time.unscaledDeltaTime`: un cuadro largo no la alarga, hace que los números «salten» (se ve pegada y luego apurada).

**E. Cuadros por segundo.** Constelaciones, Bodega, Correo, Tinta o Palabra, Carga exacta y Engranajes NO fijan `Application.targetFrameRate` (los otros once, o sus controladores, sí: 60). Según la documentación de Unity, en Android el valor por omisión es de 30 cuadros por segundo (**no verificado en el teléfono**): esos seis juegos, su cuenta y su partida irían a 30. El Editor no sirve para ver esto (ahí corren sin tope).

**F. Texturas.** Los atlas de plasticina están importados en Android como RGBA32 **sin comprimir** (`textureFormat: 4`): `comun` 12 MB, `emblemas` 14 MB, `nubi` 4 MB y el del juego (0,5-9,5 MB) se cargan con `Resources.Load` al armar (20-30 ms cada uno en el Editor, sin contar subirlos a la GPU en el primer dibujo): unos 30-45 MB en la primera partida de un proceso. Es probable que el primer cuadro con el emblema y el anillo cueste en el teléfono; no se pudo medir aquí.

**G. Marca de versión.** `CountdownScreen.ShowStyleStamp = true` dibuja «avisos en la fila 26-oct» (12 unidades, crema al 55 %) bajo la cuenta **también en la app publicada**; el propio comentario pide ponerla en `false` antes de la tienda.

### 1.4 Lo que no se pudo medir y qué haría en el teléfono

El Editor sin gráficos en un PC no dice cuánto tarda el Motorola ni cuánto cuesta dibujar. Con el OK de Ricardo (no lo instalo ni lo toco sin su autorización):

1. Un registrador **solo de depuración** (el mismo `FrameProbe`, detrás de una bandera de build) que escribe en `logcat` los cuadros de la entrada. Ricardo juega unas partidas con ese APK y yo leo `adb logcat -d -s Unity` (solo lectura; no lanzo ni capturo nada).
2. Mientras él juega, `adb shell dumpsys SurfaceFlinger --latency <capa de Unity>` da la hora de presentación de cada cuadro (la tasa real y los cuadros perdidos, sin tocar la pantalla).
3. Registrar `Application.targetFrameRate`, `Screen.currentResolution.refreshRateRatio` y el tamaño de los atlas cargados (confirma la causa E y la F).
4. Si hace falta el costo de dibujar: el smoke CON gráficos (el PC tiene tarjeta de video: `ScreenshotRunner` ya corre así) con el mismo registrador.

### 1.5 Trabajo de entrada por juego (para dimensionar la pantalla de carga de la T190)

Lo que hay que hacer en el hilo principal antes de que el tablero esté completo (PC): primer cuadro + trozos de sprites posteriores + todo el cálculo de sonido de arranque.

| Juego | Trabajo de entrada (PC) |
|---|---|
| Tinta o Palabra | 1,0 s (0,23 + 0 + 0,76) |
| Freno de Emergencia | 1,1 s |
| ¿Verdad o disparate? | 1,1 s |
| Carga exacta | 1,2 s |
| Piloto Estelar | 1,3 s |
| Lluvia de meteoros | 1,3 s |
| Aterrizaje Lunar | 1,3 s |
| Acoplamiento | 1,5 s |
| La estrella intrusa | 1,7 s |
| En la punta de la lengua | 1,7 s |
| Rastro de luz | 1,8 s |
| Rescate relámpago | 1,8 s |
| Constelaciones | 1,9 s |
| Satélites | 1,9 s |
| Bodega de carga | 2,3 s |
| Engranajes | 3,4 s |
| Correo Estelar | 5,3 s |

En el teléfono serán más. **Mover todo esto detrás de la carga sin reducirlo daría cargas de varios segundos** (de 3 a más de 10 s en Correo y Engranajes si el teléfono es 2 o 3 veces más lento).

## 2. Parte 2 · Lo que ve la persona antes de jugar

Hecho leyendo el código (un subagente de lectura inventarió los 17 juegos; revisé a mano varias afirmaciones: las tarjetas «NUEVO» de Bodega y sus preferencias, el pre-roll de Freno, la pantalla de carga y el rating sembrado). Nada se ejecutó.

### 2.1 Lo común a todos

- **El tutorial guiado** (`GuidedTutorial`: la tarjeta de Nubi con título y meta, «Probar una ronda» / «Saltar tutorial», y la ronda guiada propia de cada juego) corre **antes** de la cuenta, en los 17, y SOLO si la app manda `show_tutorial` (`UnityGameLauncher.shouldShowTutorial`: el juego tiene tutorial y la persona **no tiene ninguna partida de él en el historial**). `RunTutorialIfNeeded` lo vuelve a comprobar. El tutorial no cuenta puntos ni se guarda.
- **La cuenta** es de 3 pasos si es la primera vez del juego o la primera partida del día, y de 2 en las demás (`CountdownSteps`); en el Primer vuelo siempre de 3. Su subtítulo es la consigna corta del juego (columna de §2.3), o «Punto de partida · N de 4», o «Contrarreloj de 2 minutos».
- **La pantalla de carga de la app** (`GameLoadingScreen`, vista dentro de la app y, en el arranque en frío, encima de la Activity de Unity por `UnityLoadingOverlay`): el planeta del juego con su luna, el nombre, «Etapa X · Con/Sin reloj» (o «Punto de partida N de 4», o el nombre del Contrarreloj), **«Cómo se juega» seguido de la instrucción larga del juego (2 a 4 frases, `model/Models.kt`)** y «Preparando el juego…». Se quita con un fundido de 280 ms cuando Unity avisa (3 cuadros después de la configuración). Con Unity ya vivo casi no se alcanza a leer (el contenido espera 250 ms); en frío se lee **en cada arranque en frío, no solo la primera vez**.
- **Primer vuelo** (solo Rastro de luz, Freno, Aterrizaje y Meteoros): antes de cada juego, una página de la app con el título, una línea de «cómo se juega» (`FlightCopy.how`) y «Empieza con una ronda de práctica que no cuenta. Dura alrededor de un minuto.»; fuerza el tutorial; cuenta de 3 pasos.
- **A voluntad hoy:** (1) en la **pausa** de Unity, el botón «Cómo se juega» (si `CanShowHowTo`: el juego en marcha, no durante el tutorial, la cuenta ni ya terminado) reabre solo la tarjeta de Nubi y la ronda guiada y luego retoma la partida (la ronda a medias se rejuega entera; el reloj del Reto no corre mientras tanto; no repite la cuenta ni las tarjetas «NUEVO»); (2) en la **ficha** → «Sobre este juego» → «Cómo se juega»: lanza `launchGame(tutorial = true)` = tutorial + cuenta + **una partida real completa que se guarda como cualquier partida** (no es «solo el tutorial»). «Reiniciar» desde la pausa conserva `show_tutorial`.
- Entre «Jugar» y la cuenta no hay diálogos de confirmación; el modo (Suave, A tu medida, Desafío, Al límite) viaja en la configuración; si el juego quedó en pausa con el mismo modo y reloj, «Jugar» lo retoma sin cuenta ni tutorial.

### 2.2 Qué ve alguien, juego por juego

**A** = primera vez que juega ese juego (sin partidas en el historial); **B** = segunda; **C** = cualquier vez posterior. El orden es el temporal. «T» = tutorial guiado; «C(3)» / «C(2)» = cuenta de 3 o de 2 pasos.

| Juego | Otras láminas o tarjetas (qué y cuándo) | A (primera vez) | B y C (después) | ¿Se repite lo mismo? |
|---|---|---|---|---|
| **Constelaciones** | **Tarjetas «NUEVO»** por etapa (Cielo, Gemelos 7, Tríos 10, Mezcla 13, Grande 16; preferencia `con_intros`): antes de CADA cielo si no vistas; **rellenan hacia atrás**; toque tras 0,5 s; salen aunque se haya hecho el tutorial | T → C(3) → tarjeta Cielo (y, con el nivel sembrado ≈ 0,45, también Gemelos) → cielo | C → cielo; tarjeta nueva solo al pisar una etapa nueva | **Sí:** la tarjeta Cielo («Cada luz esconde un objeto… Da vuelta dos…») es la meta de la tarjeta de Nubi casi palabra por palabra |
| **Rastro de luz** | Pantalla de modo: «¡Nuevo!» + nombre la primera vez de cada modo (Al revés, El cielo gira, En marcha; preferencia `rastro_modes`) y «Ahora: <modo>» en CADA cambio de modo | [Primer vuelo + T forzado] T → C(3) → ronda 1 (rastro simple); desde la ronda 2, «¡Nuevo!» | C → rondas; «Ahora: …» en cada cambio | Parcial: el tutorial solo enseña el rastro simple; «mira y repite» sale en la carga, la meta, el coach, el subtítulo y el marcador |
| **Bodega de carga** | **Tarjetas «NUEVO»** (Bodega, Mueve 5…12, Gira 8…12; `bod_intros`): solo la de la etapa del pedido, sin relleno; antes de cada pedido; toque | T → C(3) → tarjeta «Bodega» (+ «Mueve» si el primer pedido es de etapa 5) → pedido | C → pedido; tarjeta solo con mecánica nueva | **Sí:** «Bodega» («La carga entra por la esclusa dorada…») repite la meta y los focos |
| **Correo Estelar** | **5 tarjetas «NUEVO»** (Estación, Hora, Rutina, Cancela, Lazo; `mail_intros`; **rellenan hacia atrás**, en orden, antes de CADA día); **hoja de la mañana** «Encargos de hoy» en CADA día (4 por partida; exige tocar «Empezar el día»); resumen de fin de día | T → C(3) → hasta 5 tarjetas seguidas (con el nivel sembrado, etapa 5) → hoja → día 1 | C → hoja → día → resumen; sin tarjetas | **Sí:** Estación y Hora repiten la meta y los focos del tutorial; «Durante el día no los verás» está en la carga, la meta, el foco, la tarjeta y la hoja real |
| **Tinta o Palabra** | Tarjeta «Ahora: PALABRA» (2,4 s o toque) la primera vez que llega una palabra por esa orilla **en CADA partida** (sin preferencia; solo desde el nivel 3) | T → C(3) → 3 palabras de TINTA → tarjeta PALABRA | igual; la tarjeta vuelve a salir | **Sí:** el tutorial ya enseñó PALABRA; el aviso de nivel 3 y la tarjeta la repiten |
| **Piloto Estelar** | Aviso alto «¡Tu misión!» 2 s al empezar y «¡Nueva misión!» en cada sector (sin toque, sin pausar) | T → C(3) → vuelo | C → vuelo | No (es parte del juego) |
| **Freno de Emergencia** | **Pre-roll de reglas** tras la cuenta cuando NO se jugó el tutorial (≈ 3,4 s: «Lanza el cohete…» y «Si aparece ¡ALTO!…»); «Espera la luz…» en los 2 primeros lanzamientos | [Primer vuelo] T → C(3) → partida (si saltó el tutorial: pre-roll) | C → pre-roll → partida | **Sí, en B y C:** reexplica las dos reglas en TODA partida sin tutorial (el comentario dice «primera vez»; el código usa `!tutorialPlayed`) |
| **Satélites** | Aviso de sorpresa (2,6 s la primera vez de cada una, 1,7 s las demás; sin toque) desde la ronda 2, una de cada dos | T → C(3) → ronda 1 → aviso «Nuevo: …» | C → rondas con aviso «Sorpresa» | No (las sorpresas no se enseñan en el tutorial) |
| **Rescate relámpago** | Sin tarjetas; mensajes en juego sin toque | T → C(3) → partida | C → partida | No |
| **Carga exacta** | Sin tarjetas; la consigna del marcador guía los 3 pasos en CADA carga | T → C(3) → carga | C → carga | La guía de la consigna repite lo enseñado en cada carga |
| **Engranajes** | **Tarjetas «NUEVO»** (Taller 1, Correas 2, Ramas 3, Barras 5, Bien 6, Dos 9; `engr_intros2`): solo la de la etapa de la máquina; toque | T → C(3) → tarjeta de la etapa de partida (con ≈ 0,45, «Barras») → máquina | C → máquina; tarjeta con etapa nueva | **Sí, solo si se parte de la etapa 1:** «Taller» repite el foco de la guía (literal) |
| **Acoplamiento** | Sin tarjetas | T → C(3) → módulos | C → módulos | No |
| **Aterrizaje Lunar** | Sin tarjetas | [Primer vuelo] T → C(3) → aterrizajes | C → aterrizajes | No |
| **En la punta de la lengua** | Sin tarjetas de entrada (la tarjeta crema es el estímulo de cada palabra) | T → C(3) → palabra 1 | C → palabras | No |
| **Lluvia de meteoros** | Sin tarjetas; «¡Lluvia de estrellas!» y «¡Un paso más!» en juego | [Primer vuelo] T → C(3) → meteoros | C → meteoros | No |
| **¿Verdad o disparate?** | Sin tarjetas; «¡Ráfaga!» en juego (no en Contrarreloj) | T → C(3) → frases | C → frases | No |
| **La estrella intrusa** | Sin tarjetas de entrada; la lámina del atlas sale tras CADA ronda (recompensa, no explicación) y, tras un error, una explicación con «Seguir» | T → C(3) → ronda 1 | C → rondas | No |

Todos tienen la ficha → «Sobre este juego» → «Cómo se juega» y la pausa → «Cómo se juega» (salvo en la cuenta, el tutorial y la partida terminada). **Las «láminas» de las que habla Ricardo son, casi seguro, las tarjetas «NUEVO»** (Constelaciones, Bodega, Correo, Engranajes): salen justo después de la cuenta, con la partida a punto de empezar, y la primera de cada juego repite lo que el tutorial acaba de enseñar.

### 2.3 Hallazgos

- **H1. La misma regla se dice de 4 a 6 veces la primera vez:** página del Primer vuelo (4 juegos) → pantalla de carga con la instrucción larga → tarjeta de Nubi (la meta) → focos del coach → subtítulo de la cuenta → tarjeta «NUEVO» → consigna del marcador. Con frases casi iguales. El caso extremo es Correo («nadie te los recuerda» / «durante el día no los verás» en la carga, la meta, el foco de la hoja, la tarjeta Estación y la hoja real).
- **H2. Las tarjetas «NUEVO» salen aunque se haya hecho el tutorial** porque dependen de preferencias de Unity (`con_intros`, `bod_intros`, `mail_intros`, `engr_intros2`), no del historial. Constelaciones y Correo **rellenan hacia atrás** (todas las de etapas anteriores aún no vistas); Bodega y Engranajes solo muestran la de la etapa presente.
- **H3. La primera partida real NO empieza en la etapa 1** (por lectura): la app siembra un nivel de 0,45 (`Baseline.DEFAULT_LEVEL`) para todos los juegos, y Unity parte de `1 + nivel × etapas`: Constelaciones ≈ etapa 8, Correo 5, Bodega 5, Engranajes 5, Satélites ≈ 5. El tutorial enseña la etapa 1, la partida real empieza mucho más arriba y las tarjetas de las etapas 1-5 se juntan (Correo: 5 seguidas). Las etapas 1 y 2 («Taller», «Correas») casi nunca se ven. Si el nivel sembrado es otro, cambian los números.
- **H4. Las preferencias de Unity no se respaldan ni se borran** (`backup_rules.xml` lo dice; `resetData` no las toca): con un teléfono restaurado no hay tutorial (el historial sí se respalda) pero vuelven todas las tarjetas «NUEVO»; tras «Borrar datos» vuelve el tutorial y las tarjetas NO.
- **H5. Contrarreloj sin historial (por lectura):** el tutorial se decide por el historial de partidas y las marcas del Contrarreloj van a otra tabla; quien solo juega Contrarreloj de Tinta o Palabra, ¿Verdad o disparate?, Rescate o Acoplamiento recibe el tutorial y la cuenta de 3 pasos en cada Contrarreloj hasta jugar una partida normal.
- **H6. Freno:** el pre-roll sale en TODA partida sin tutorial (el comentario dice «primera vez sin tutorial»); también si se salta el tutorial.
- **H7. «Cómo se juega» de la ficha es una partida real completa** (se guarda), no solo el tutorial; el de la pausa sí es solo tarjeta + ronda guiada.
- **H8. La pantalla de carga repite la instrucción larga** en cada arranque en frío y, en el Primer vuelo, las cuatro frases de `FlightCopy.how` dicen lo mismo con otras palabras.
- **H9. Menores:** la marca de versión en la app publicada (§1.3 G); `tutorialPlayed` de Meteoros se calcula y no se usa; `_hint` de Disparate y Meteoros no recibe texto; el comentario de `GameControllerBase` (línea 16) dice que Rastro y Constelaciones no la heredan y sí; una tarjeta «NUEVO» interrumpida por «Cómo se juega» no queda marcada como vista (reaparece); Satélites marca la sorpresa como vista al mostrarla, no al terminar.

## 3. Recomendaciones técnicas

Para la cuenta (ordenadas por lo que más quita; la T190 de Opus ya decidió quitar el 3-2-1, así que lo de la pantalla de la cuenta (D) deja de importar, pero **A, B y C se mudan con el armado y siguen siendo el problema**):

1. **Que «listo» sea de verdad.** Unity debe avisar `NotifyGameShown` cuando el tablero esté completo (escena armada, atlas cargados, sprites horneados, sonidos de arranque calculados), no 3 cuadros después de la configuración. Es la pieza que hace que la carga tape los trozos de §1.2.
2. **Reducir el trabajo, no solo moverlo** (§1.5: hasta 5,3 s en el PC):
   - **Sprites (causa A):** (i) pasarlos al arte de plasticina ya renderizado (el plan de `docs/plasticina-juegos.md`: cada juego migrado deja de hornear); (ii) mientras tanto, presupuesto por cuadro dentro de `Prewarm` (cortar `RenderClay` por filas de píxeles) o calcular los arreglos de píxeles en un hilo de trabajo (la matemática de `Color32[]` no toca Unity) y solo crear el `Texture2D` y llamar a `Apply` en el hilo principal. El caso Correo (3,2 s en un trozo) y Engranajes (0,94 s) son los primeros.
   - **Sonido (causa B):** lo más sólido es **pre-renderizar los clips en el build** (la paleta es determinista; el núcleo y lo extra de los 17 juegos son ~34-46 clips por juego de 22 kHz): cero CPU en tiempo de ejecución, cuenta sin ningún horneo y menos basura; cuesta unos MB de APK y un paso del build (ya existe `SoundListeningExporter`). Alternativa: sintetizar en un hilo de trabajo (comprobar que `WoodSounds` no usa estado compartido) y crear el `AudioClip` en el principal; o partir los pasos en rebanadas de ≤ 2-3 ms. En cualquiera: reutilizar los arreglos (la basura de Gen0 cada 2-3 cuadros) y no soltar lo extra del juego anterior si se va a volver a jugar.
   - **Texturas (F):** comprimir los atlas (ASTC o ETC2; hoy RGBA32 de 30-45 MB por partida) y cargarlos detrás de la carga.
3. **Fijar `Application.targetFrameRate` una sola vez** en `GameEntryPoint` para todos los juegos (hoy lo hacen 11 controladores por su cuenta y 6 nunca), y comprobar en el teléfono cuál es el valor por omisión.
4. **La marca de versión (G) solo en depuración**: `ShowStyleStamp` detrás de `Debug.isDebugBuild` (pero el comentario recuerda que el export de Unity es de producción y entonces no se vería: usar una bandera propia de la compilación) o moverla a la pausa solo de depuración.
5. **Medir en el teléfono** (§1.4) antes y después, con el mismo registrador, para dar los picos reales en ms de la T190.

Para las pantallas previas (para Fable y Opus; solo propuestas, nada hecho):

6. **Una sola regla de aparición:** lo que se enseña en el tutorial no se vuelve a enseñar en una tarjeta. Opciones técnicas: (i) marcar como vistas las tarjetas de la primera etapa de cada juego cuando el tutorial termina (o se salta), (ii) quitarlas, (iii) que cada tarjeta «NUEVO» dependa del historial de la app y no de una preferencia de Unity (arregla H4 y H2).
7. **Resolver H3:** o el tutorial enseña la etapa en que de verdad se empieza, o la primera partida empieza en la etapa 1 de cada juego nuevo, o se muestran solo las tarjetas de la etapa actual sin relleno hacia atrás.
8. **Sacar la instrucción larga de la pantalla de carga** (la ficha y la pausa ya la ofrecen); es lo que la T190 pide.
9. **Contrarreloj (H5) y Freno (H6):** decidir si el tutorial también cuenta con las marcas del Contrarreloj, y qué hace el pre-roll de Freno cuando ya se jugó.
10. **«Cómo se juega» de la ficha** (H7): si debe ser solo la ronda guiada, necesita su propio camino (hoy lanza una partida que se guarda).

## 4. Estado tras la Tarea 191 (30-oct)

Detalle en `docs/entrada-al-juego.md` §9. **H1:** el Primer vuelo ya no repite la línea de cómo se juega (H8); la carga no explica desde la T190. **H2:** al terminar o saltar el tutorial quedan vistas las tarjetas que repiten lo que enseñó. **H3:** la primera partida normal de un juego sin historial parte en la etapa 1 y sube un paso por ensayo bien hecho hasta el sembrado; las tarjetas salen de a una, sin relleno hacia atrás, antes del «¡Ya!» (la hoja de Correo, después).
**H4:** el archivo de preferencias de Unity se respalda y «Borrar datos» lo borra (con el proceso `:unity` vivo o no). **H5:** el Contrarreloj cuenta como historial para el tutorial. **H6:** el repaso de Freno solo sale cuando se ofreció el tutorial y se saltó. **H7:** «Cómo se juega» de la ficha se queda como estaba (una partida real que se guarda). **H9:** quitado el código muerto y corregidos los comentarios; una tarjeta cuenta como vista apenas se muestra.
