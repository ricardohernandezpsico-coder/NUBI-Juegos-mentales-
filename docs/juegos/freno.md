# Freno de Emergencia (`freno`) — ficha técnica

> Ficha técnica movida TAL CUAL desde `CLAUDE.md` el 2-oct (CLAUDE.md quedó como índice). Lo que manda es el código; esta ficha explica cómo y por qué.

**Freno de Emergencia** (`Games/Freno/`, id `freno`, dominio atención): tarea de señal de alto (Logan y Cowan, 1984;
consenso de Verbruggen et al., eLife 2019). Base de lanzamiento con 2-4 plataformas sobre la luna: se enciende un cohete
(baliza lima + botón lima) y se lanza tocando su carril (cualquier toque en la mitad de abajo; cuenta al PRESIONAR);
en ~30% aparece la señal ¡ALTO! (octágono coral con texto + sirena) un instante después y hay que no tocar.
- Reglas y pruebas: `BrakeContract` / `BrakeContractTests` (7). SSD en escalera (250 ms inicial, ±50, entre 50 y el
  límite − 150). DDA común solo para la tarea de ir: plataformas 2/3/4 (niveles 1-4/5-8/9-12) y tiempo para lanzar
  1400 → 800 ms (Precisión +300). 3 primeros sin alto, nunca más de 2 altos seguidos, espera previa 600-1200 ms
  (tocar antes = "¡Espera la luz!" y se repite sin contar). Si empieza a esperar el alto (últimos 6 lanzamientos 30% más
  lentos que los 6 primeros), aviso "¡No esperes al ALTO!". Reto 120 s; Precisión 40 lanzamientos. 60 cuadros/s.
- Enganche: cada despegue con llama, humo y temblor deja una estrella en el cielo; frenar da más puntos (más cuanto
  más tarde llegó el alto) y empuja el medidor "Límite del freno" (marca sol = récord, aviso "¡Nuevo límite!"); no
  frenar = el cohete da un salto y vuelve (sin choques).
- **Puntaje (Tarea 57, 9-oct)**: `BrakeContract.Score = 100 × (0,75 × goAccuracy + 0,25 × min(1, stopRate / 0,5))`; goAccuracy = lanzamientos
  correctos a tiempo ÷ lanzamientos de ir y stopRate = altos frenados ÷ altos (sin altos en la partida cuenta solo goAccuracy). **Ya no depende del SSRT.**
  Motivo: con 6 a 20 altos por partida el SSRT de UNA partida es ruido (el consenso de Verbruggen et al., 2019 pide 50 o más) y no debe mover el puntaje ni
  el rango. La escalera del SSD lleva el frenado cerca del 50 %, así que quien frena bien saca el cuarto completo y quien ignora el ALTO lo pierde.
- Medida propia: **tu freno** (SSRT por integración con reemplazo de omisiones; -1 con menos de 6 altos o
  p(responder|alto) fuera de 0,25-0,75, el criterio del consenso de Verbruggen et al., 2019). Cada partida lo estima igual y lo guarda en
  `star_measures` (clave `brake`, en ms, interno). Viaja en `brake_ms / stops_ok / stops_total / brake_best_ssd_ms` →
  `GamePlayResult.brakeMs / stopsOk / stopsTotal / brakeBestSsdMs` → `GameResultScreen`. **Se muestra por ZONAS y como PROMEDIO** (`data/Brake.kt`): el promedio de las
  últimas ≤ 5 estimaciones válidas (con la de hoy) en un velocímetro de 3 zonas con nombre: «ágil» (< 230 ms), «firme» (230-300 ms) y «pausado» (> 300 ms), con la aguja en
  el promedio. Con 3 o más: «Tu freno, promedio de tus últimas N partidas: zona firme»; con 1 o 2: «Primera lectura: zona firme. Se afina con más partidas». Sin ms en la
  pantalla (tampoco en Juegos y Hoy: ahí dice «zona firme»). Debajo: «Frenaste N de M altos · récord» (igual que antes) y la nota «Una sola partida trae pocos altos: por eso
  mostramos el promedio de varias». Si esta vez no se pudo estimar, el final lo explica y pide lanzar sin esperar al ALTO (y, si hay partidas anteriores, muestra su promedio).
  El «Límite del freno» (récord de SSD) y lo demás del juego no cambian.
- Arte: `BrakeSprites` (alto, plataforma, botón, cohete; de **plasticina** desde la Tarea 96, ver abajo), `GameWorld.LaunchBase`. Vista previa:
  `python3 tools/art-preview/freno.py <raw>` → `docs/previews/freno.png`. Probado por Ricardo (27-sep): "me gustó,
  funciona muy bien".


## Tutorial guiado, «Cómo se juega» y versión corta del inicio (3-oct, tarea 21a)

Para el inicio nuevo (`docs/diseno-inicio.md`). La pieza común es `Games/Shared/GuidedTutorial` (ficha en `docs/diseno-rastro-de-luz.md`, § Tutorial).

- **Tarjeta de Nubi**: «Freno de Emergencia» · «Lanza el cohete que se enciende. Si aparece ¡ALTO!, no toques.» · «Probar una ronda» / «Saltar tutorial».
- **Ronda guiada** (`BrakeContract.GuidedPlan`: 3 de ir y 1 con alto; entre `// <guided>` y `// </guided>` en `BrakeGameController`): cohetes lentos con un aro sol
  punteado sobre el botón del carril; los pasos de ir esperan sin límite (60 s) y el del alto dura 3,2 s. Avisos de Nubi: «Toca el cohete que se enciende» ·
  «Otra vez: toca el cohete que se enciende» · «Ahora la otra regla: si aparece la señal ¡ALTO!, no toques» · «¡ALTO! Este no: déjalo quieto» · «¡Frenaste a
  tiempo!» · errores sin culpa: «Casi: se toca el cohete que se enciende. Mira otra vez» / «Casi: con el ¡ALTO! el cohete se queda quieto. Probemos otra vez» ·
  cierre «¡Así se juega! Ahora sin ayuda». Un error repite el MISMO paso. No suma puntos, no toca el DDA, las rachas ni el SSRT y no se guarda.
- **«Cómo se juega» desde la pausa** (`PauseMenu`, cuarto botón): descarta el lanzamiento en curso, corre tarjeta + ronda guiada con el reloj andando y vuelve a la partida
  (`HowToSuspend` → `HowToFlow` → `HowToResume`: las anclas de tiempo se corren lo que duró el tutorial; el Reto no pierde segundos).
- **Versión corta** (`Assessment.Active`): `BrakeContract.AssessmentTrials` = **24 lanzamientos** (≈ 70 s, sin reloj), con 8 altos en posiciones fijas
  (`AssessmentStop`: nunca en los 3 primeros, máximo 2 seguidos). Arranque suave (`CreateEngine`: nivel 1 en mayores, 3 en el resto, sin tiempo de reacción).
  `end_rating` normal. **Primer dato** (se lee en la app, `data/FirstData.kt`): «Frenaste a tiempo N de M veces.» (con ≥ 4 altos), de `stops_ok`/`stops_total`.
- Los cohetes llevan ¡ALTO! (octágono): no son «naves».
- Pruebas: `FrenoTutorialTests` (plan, calendario de altos, arranque suave, rating normalizado), `GuidedTutorialTests` (guion, reloj, invariante de la ronda guiada).


## Plasticina (Tarea 96, 14-oct): el tercer juego en pasar al nuevo estilo

Ricardo: todos los juegos en plasticina (el piloto fue Rescate, Tarea 94). Es SOLO arte: el SSD, el DDA, los tiempos, la espera previa, el puntaje y la medida «tu freno» no cambian. Técnica y pesos: [`tools/juegos/README.md`](../../tools/juegos/README.md); seguimiento de los 17 juegos: [`plasticina-juegos.md`](../plasticina-juegos.md). **Fecha del cambio de arte: 14-oct-2026** (marca `estilo 14-oct · plasticina freno`; si algún día hay que separar series de medidas, se hace por fecha: decisión de Opus, no se agrega `art_version`).

**Qué es de plasticina (renders de Blender, atlas `Resources/Plasticina/freno`, `tools/juegos/freno.py`) y qué no**

| De plasticina | Se queda procedural |
|---|---|
| el **botón de lanzamiento**, apagado (azul oscuro, flecha crema) y encendido (lima, flecha de tinta) | la **baliza** (el disco de luz sobre cada cohete, que se tiñe de lima, ámbar o coral y se apaga al 18 %): es luz |
| los **3 cohetes** de los carriles: el cohete de Piloto con el acento coral, cielo o lima | el resplandor del carril, la llama del despegue, el humo y el vapor |
| la **plataforma** (losa con franjas de precaución y dos torres con travesaños) | las estrellas del cielo, el medidor «Límite del freno», el aro de ayuda del tutorial |
| la **señal de ALTO** (octágono coral con el aro crema incrustado; el texto sigue siendo `Text`) | el texto, los avisos (de la fase 0) |

Cada pieza sale en el MISMO lienzo y a la MISMA escala que el sprite procedural al que reemplaza (botón y ALTO ±1,1; plataforma ±1,05): `SetupLanes`, `BuildStopSign` y el tutorial no cambian ninguna medida. Dos ajustes de montaje: (1) el cohete de plasticina viene RECTO (el procedural venía dibujado en diagonal y se giraba 45°, de donde salía su inclinación de ~13°): `BrakeSprites.RocketAngle` da 0° con plasticina y 45° sin ella, y lo usan `BuildLane` y `FlyAway`; (2) el texto «ALTO» sube 26 u con la cara de la señal de plasticina (el canto de 0,3 se ve de lado y la cara queda ~14 u más arriba que el centro del lienzo).

**El cohete ya no es el de Constelaciones.** Antes era `SymbolSprite.Rocket` (que usan también las cartas de Constelaciones); ahora es `BrakeSprites.Rocket(carril)`: con plasticina, el de Piloto con el acento del carril (coral, cielo, lima); sin ella, el ícono de siempre. Las cartas de Constelaciones no cambiaron (le toca a ese juego, fila 6 de `plasticina-juegos.md`). El casco crema domina, y el lima («ir») y el coral («alto») quedan solo en la punta y las aletas: el cohete deja de confundirse con la baliza y la señal.

**Lo que se midió** (`tools/juegos/mide_freno.py`, sobre el atlas ya empaquetado; fondo = el suelo de la luna #595890 medido en la captura y el cielo #070A28):
- **Misma huella y mismo tamaño:** razón de áreas 0,98 a 1,07 contra el sprite de hoy (con su borde de tinta) e IoU 0,89 a 0,98 (la plataforma, la más baja: sus torres salen algo más altas por el canto). El cohete llena el 92 % del alto del lienzo (el de hoy, ~89 %).
- **Contraste:** el casco crema rinde 6,1:1 contra el suelo y 18:1 contra el cielo; el botón encendido, 4,0:1 y 11,8:1; el ALTO, 7,5:1 contra el cielo. **La flecha contra el botón: 10,0:1 (crema sobre el apagado) y 8,9:1 (tinta sobre el encendido).** Encendido y apagado se distinguen por luminosidad (4,0:1 entre sí contra el suelo; el apagado es el oscuro): el estímulo «ir» sigue leyéndose sin color.
- **Lo más flojo, a juzgar en el teléfono:** el botón apagado rinde 1,9:1 contra el suelo de la luna (el de hoy, con su borde de tinta, 2,6:1; sin borde lo separa la sombra de contacto, y se oscureció de #2A3590 a #222B78 para ayudar); el aro crema del ALTO rinde 2,1:1 contra el coral (es adorno: la señal es forma, texto y sirena); los acentos coral y cielo de los cohetes rinden 2,3 y 3,4:1 contra el suelo (el casco es lo que se lee).
- **Pesa** 0,50 MB en el APK (369 KB de PNG; 0,33 MB con ASTC 6×6), bajo el tope de ~1,5 MB. 7 imágenes: botón ×2 (256 px), cohete ×3 (256 px), plataforma (320 px) y ALTO (400 px).

**Pruebas:** `BrakePlasticTests` (huella, flecha contra botón y luminosidad apagado/encendido, ALTO, casco común y acento por carril, que el cohete quepa entre las torres, ángulo, caída al procedural sin el atlas). Las del resto de Freno no cambiaron (`BrakeContractTests`, `FrenoTutorialTests`).

### Estado presionado (Tarea 104, 14-oct)

Regla del 14-oct: todo objeto de plasticina que se toca tiene su estado presionado, con textura (la pieza aplastada, un punto más oscura y con la huella de un dedo; mismo lienzo y escala; sin «saltar»). Detalle, medidas y cómo funciona la ayuda común `PlasticTouch`: [`plasticina-juegos.md`](../plasticina-juegos.md) § «Estado presionado».
- **Freno:** el botón de cada carril pasa a `boton_encendido_p` / `boton_apagado_p` (aplastado, un punto más oscuro, con la huella de un dedo en el espacio libre de al lado de la flecha: la flecha es lo que se lee y no se pisa) al menos 0,1 s y vuelve con un rebote chico; sin el atlas, el 90 % de escala de siempre (o oscurecer con «quitar animaciones»). En el botón apagado (azul oscuro) la huella se ve poco.

## Cambio de versión del 18-oct-2026 (Tarea 128): el ALTO más grande

**Qué cambió:** la señal ¡ALTO! pasa de ~133 a **160 dp** (de 400 a 480 unidades del lienzo; su letra, de 120 a 144) y «Tu freno» y su valor van a 16 dp. **Por qué:** la mitad de la pantalla estaba vacía y el ALTO es lo que no hay que tocar: verlo grande ayuda a una persona de 60+. **Qué NO cambió:** ningún tiempo (el límite del freno, el momento en que suena la alarma, la ventana de toque), la regla ni las cantidades.
**Comparabilidad:** una señal más grande se detecta un poco antes: el tiempo de reacción al ALTO de antes y después del 18-oct puede no ser comparable. El juego no guarda una versión de reglas: basta esta nota.
