# Acoplamiento: «muelle de acoplamiento» (renovación, 10-oct-2026)

Boceto aprobado por Ricardo el 10-oct: `docs/previews/acoplamiento-boceto.html` (artifact https://claude.ai/artifact/JjYyTVEr8B5N9UgKkRANvi, versión 2). Mismo id `acoplamiento`, área Razonamiento, carpeta `Games/Acoplamiento/`.

## 1. Por qué se renueva

Es la Etapa 2 de la hoja de ruta. Al nivel de Rescate v4 le faltaba lo mismo que a Rescate v3:
- pieza chica y mucho espacio vacío;
- sin mundo;
- sin una meta durante la partida;
- sin tutorial.

Ricardo probó el boceto: «se entiende rápido, se entiende también cuando es espejo, se ve bien, se acopla bastante bien; el juego engancha».

Sus dos reparos:
- **El crecimiento de la estación** le da un efecto, pero no está seguro de que sea lo que engancha. Se mantiene, sin agrandarlo más.
- **El sonido:** «sonidos breves y un poco más fuertes que no sé si se relacionan a lo que el juego hace». Se corrigió en la versión 2 del boceto (§7).

## 2. La idea

Estás armando una estación espacial. Llegan módulos girados. Si es la misma pieza que el hueco del puerto, **encaja**; si está dada vuelta, es su **espejo**. Cada acierto suma un módulo a tu estación, de las dos formas: si era espejo y lo dijiste, el muelle lo da vuelta y también se acopla.

## 3. Base científica (PubMed, 10-oct)

- **Rotación mental:** el tiempo para decidir crece en línea recta con el ángulo de giro. Shepard y Metzler 1971, doi:10.1126/science.171.3972.701. En el plano: Cooper y Shepard 1973.
- **Las habilidades espaciales mejoran con práctica:** metaanálisis de 217 estudios, g = 0,47, con transferencia a otras tareas espaciales (Uttal et al. 2013, doi:10.1037/a0028446). Es población general, no mayores: **no se promete nada**.
- **Texto permitido:** «girar figuras en tu mente». Nada de salud ni de prevenir.

## 4. Reglas (las de hoy, `DockingContract`, con UN cambio)

- **Sin cambios:**
  - 12 niveles.
  - Bloques: 4 en los niveles 1-3, 5 en 4-6, 6 en 7-9 y 7 en 10-12.
  - Giro máximo: 90° en el nivel 1, 135° en el 2 y 180° desde el 3. Paso de 45° hasta el nivel 4 y de 15° después.
  - Tiempo para decidir: Reto de 6000 → 2500 ms; Precisión sin apuro (12 s).
  - Reto de 120 s; Precisión de 24 módulos.
  - Espejo al 50 %; motor común `stepUp` 0,3, sin modular por tiempo.
- **CAMBIO:** la pieza cabe en 4 × 4 bloques (`RandomChiralShape` rechaza las más largas), para que el módulo y el hueco se vean grandes. Es una prueba de contrato nueva.
- **La respuesta cuenta al tocar.** Sin tiempo: se cuenta como error («Sin tiempo») y se muestra la verdad.

## 5. Pantalla (referencia 360 × 780 dp; 16:9 escala como Rescate)

| Pieza | Valor |
|---|---|
| Marcador | arriba, con `GameHud`: «Acoplamiento», «Módulo N de 24» (Reto: «Módulo N» + barra), «Nivel N»; a la derecha, un módulo chico y el número acoplado, y «Racha ×N» desde 2 |
| Fila de luces | (v3) 8 luces redondas de plasticina de 22 dp cada 30 dp, centradas, en y 100; la medalla lima de los anillos a la derecha (x 321) |
| Avisos | franja en y 118-150 (v3; la v2 iba en y 246-278): NUNCA sobre el módulo, el puerto ni los botones |
| Módulo | centro (180, 265); bloque de **38 dp** (31 en 16:9; la v2, 26); gira en un círculo de 215 dp |
| Barra de tiempo | centro y 385, 200 × 12, con el rótulo «Tiempo» (14 dp) |
| Puerto | centro (180, 506), **245 × 214**; hueco a la MISMA escala (38 dp), 3 dp más arriba que el centro; rótulo «Puerto» (15 dp); 4 chevrones abajo |
| Botones | y 665, 158 × 76 cada uno, separados 12: «Encaja» (lima, ícono de tres bloques que bajan a su soporte) y «Espejo» (uva, ícono de dos figuras espejadas con eje punteado); texto de 22 dp |
| Pista | «Gira la pieza en tu mente y compárala con el hueco» (14 dp, y 723), solo en los primeros 3 módulos |

Todo lo que va bajo la franja se achica parejo en pantallas más bajas (como mínimo 0,6) y lo que sobra en las altas se reparte hacia abajo; ver §13.

- **El módulo (v3):** una almohada de plasticina sin adornos, celeste cálido, con canto de 5 dp hacia abajo y sombra de contacto.
  - **La lámpara queda fija en la pantalla** (brillo arriba a la izquierda, sombra abajo a la derecha): se hornea en la orientación que se ve, así no delata el giro ni el espejo.
- **El hueco del puerto (v3):** una cavidad con cordón de plasticina (verde agua; lima al encajar; celeste al «mostrar»), pared iluminada por la misma lámpara y fondo oscuro (#070818).
- **Nada gira en el fondo** (regla de hoy): cielo y planeta quietos.

## 6. La revelación (siempre se ve la verdad)

1. **Giro (320 ms):** el módulo gira hasta quedar derecho.
2. **Vuelta (300 ms), si era espejo:** se da vuelta como en un espejo (escala x de 1 a −1). Ahora calza con el hueco.
3. **Baja al puerto (380 ms).**
4. **Si acertaste:**
   - golpe hueco («plop», 220 ms: se hunde un 3,5 %), el cordón y los chevrones se ponen lima y el botón tocado muestra ✓;
   - queda a ras del hueco (520 ms), con el visto lima del kit en el panel y una luz de la fila encendida con su halo;
   - cada 8 módulos: una onda lima recorre la fila, «¡Anillo completo!» y una medalla lima cuenta los anillos; la fila empieza de cero.
5. **Si no acertaste:**
   - el botón tocado muestra el aspa diagonal;
   - el borde se pone celeste («mostrar») y el módulo se aleja hacia la derecha mientras se apaga (600 ms), sin castigo.
6. **Avisos:**

| Caso | Aviso |
|---|---|
| Encaja y era igual | «¡Encaja!» (desde la racha 3: «¡Encaja! Racha ×N») |
| Espejo y era espejo | «¡Bien visto! Era su espejo» |
| Respuesta equivocada | «Era su espejo» o «Sí encajaba» |
| Sin tiempo | «Sin tiempo: era su espejo» o «Sin tiempo: sí encajaba» |

- **Llegada de cada módulo:** baja desde arriba en 420 ms.
- **«Quitar animaciones»:** sin llegada, giro, vuelta, plop ni onda de las luces; cada paso aparece de una vez y los chevrones no parpadean.

## 7. Sonido «Madera cálida» (atado a lo que pasa; versión 2 del boceto)

Cada sonido corresponde a una acción visible. Los cortos son los más suaves (Ricardo notó que los «breves y fuertes» no se entendían).

| Momento | Receta | Pico |
|---|---|---|
| El módulo se acerca | soplo de paso bajo 900 → 300 Hz | 0,06 |
| Tocar un botón | marimba apagada de 392 Hz (τ 0,06) | 0,07 |
| Se da vuelta (espejo) | kalimba de 880 Hz y luego de 659 Hz a 0,09 s | 0,16 |
| Encaja | golpe grave de 130 a 60 Hz + marimba do5 + bloque de madera de 700 Hz al 20 % | 0,30 |
| Se suma a la estación | kalimba cuya nota sube con la racha (P[1 + min(racha, 8)]) | 0,26 |
| Anillo completo | marimba do-mi-sol-do + kalimba | 0,36 |
| No era | marimba de 196 y 165 Hz, suave | 0,22 |
| Final | rodado de marimba | 0,40 |

Todo pasa por la reverberación de Madera cálida. Se porta con `Games/Shared/SoundKit.cs` (Tarea 65) y las recetas de `RadarSounds` donde coincidan.

## 8. Medidas (sin cambio)

- **«Tu giro mental»** (grados por segundo) y **«tu curva de giro»** (5 columnas, 0-180°), con los mínimos de hoy: ≥ 8 aciertos, ≥ 3 ángulos y ≥ 70 % de aciertos.
- **Premio, no medida:**
  - módulos acoplados y anillos en la partida;
  - récord de módulos;
  - total entre partidas («Has acoplado N módulos · M anillos»).
  Todo va en prefs `acoplamiento_record`, con respaldo, como `rescate_record`.
- **Final:**
  - «¡Tu estación creció!» y «N módulos acoplados»;
  - Aciertos, Tu giro mental, Anillos completos y Racha mayor;
  - la curva;
  - «Más giro, más tiempo: es lo esperable.» y «Medida de esta partida. No es un diagnóstico.»
- **Telemetría nueva:** `docked`, `rings`.

## 9. Tutorial con Nubi (práctica que no cuenta; «la práctica asegura, nunca alterna»)

1. **Puerto.** Nubi dice «Este es el puerto: el hueco tiene la forma de la pieza» (hueco del foco: el puerto).
2. **Llega un módulo.** Llega uno girado 90° que SÍ encaja. Nubi dice «Gíralo en tu mente: ¿cabe en el hueco?» (foco: el módulo; la v3, con la pieza más grande, deja entre el marcador y el módulo lugar para un globo de 2 líneas y no de 3: antes decía «Llega un módulo girado. Gíralo en tu mente: …», que en las tres formas de pantalla del smoke tapaba el módulo). Después, «Si calza, toca Encaja» (foco: el botón, con toque real).
3. **Demostración de encajar.** El módulo gira, baja y encaja. Nubi dice «¡Encaja! Se enciende una luz» (foco: la fila de luces; en la v2, «Se suma a tu estación» y la estación).
4. **Demostración del espejo.** Llega un módulo espejo (forma en L, girado 45°) y Nubi dice «Este está dado vuelta: es su espejo». Sin tocar, se muestra el giro hasta quedar derecho y luego la vuelta que lo hace calzar. Nubi dice «Si está dado vuelta, toca Espejo».
5. **Práctica del espejo.** Llega otro espejo (90°) y Nubi dice «¿Y este?». Toque real en «Espejo» y luego la revelación.
6. **«¡Listo!».**

- Rumbo y Acoplamiento eran los únicos sin tutorial; con esto, Acoplamiento entra a `TUTORIAL_GAMES`.
- Guardias de estado en el smoke: tras cada toque, la respuesta registrada es la del paso; las dos prácticas terminan acertadas.

## 10. Patentes

Rotación mental «igual o espejo» con poliominós es una tarea de laboratorio genérica (1971). En `docs/nombre-marca-y-riesgos.md` figura como riesgo «Bajo (no revisado a fondo)». Antes de publicar, Fable puede hacer una búsqueda corta. Se mantienen dos reglas:
- nada gira en el fondo;
- nombres propios (nada de «Shape Shifter» ni nombres ajenos).

## 11. Qué se va

- El puerto chico abajo con la pieza flotando en el vacío.
- La franja «Tu estación» de arriba, que se reemplaza por la estación de anillos.
- El aviso «Con calma».
- Las piezas más largas que 4 bloques.

## 12. Cómo quedó hecho (Tarea 68, 10-oct)

**Código** (`Games/Acoplamiento/`):
- `DockingContract.cs`: las reglas de hoy más `FitsInBox` (4 × 4), los tiempos de la revelación, `CompletesRing`, `SlotOf` y todos los textos.
- `DockingLayout.cs`: `DockingPlan(alto, guiada)`, la pantalla en dp como lógica pura probada en varias formas de teléfono.
- `DockingSprites.cs` (arte), `DockingSounds.cs` (sonido).
- `DockingGameController.cs` y sus partes `.Build.cs` (la interfaz), `.Scene.cs` (pose del módulo, puerto, botones, estación, aviso y final), `.Guided.cs` (el tutorial) y `.Shots.cs` (las capturas; solo en el Editor).

**Lo que quedó distinto del boceto o del diseño, y por qué:**
1. **(v2; en la v3 la lámpara se hornea en la pieza, ver §13) La luz fija se hace con una máscara.** El módulo se hornea plano (celeste, borde de tinta, juntas y remaches) y NO lleva luz. La luz de arriba y el brillo son dos imágenes recortadas por la silueta del módulo (un `Mask` sobre su cuerpo) que giran al revés que él, así que quedan quietas en la pantalla. La vuelta del espejo es la escala en x del cuerpo (la luz no se da vuelta). La sombra es la misma silueta, oscura, en un contenedor que no gira: cae siempre hacia abajo.
2. **La barra de «Tiempo» solo se dibuja en Reto.** En Precisión el plazo de 12 s sigue existiendo (un módulo sin contestar cuenta como error), pero no se muestra para que no apure.
3. **(v2; en la v3 no hay estación en la pantalla de juego, ver §13) La estación dibuja como mucho 5 anillos** (los más nuevos); los viejos dejan de dibujarse. Lo que cuenta (módulos, anillos, récord) no depende de lo que se dibuja.
4. **La pantalla final tapa el marcador común** y trae su propia lista (§8). Se quitaron el aviso de subida de nivel y «Con calma»; el smoke `AvisoAcoplamiento` ya no tiene sentido y se quitó.
5. **Sonido.** Los instrumentos son los de `RadarSounds` (kalimba, marimba, bloque de madera, soplo, golpe), que ya son el port 1 a 1 del laboratorio aprobado. Las 8 recetas de la tabla del §7 se prueban contra la salida del propio boceto corrida en Node (`tools/sonido/referencia-acoplamiento.js` → `Tests/DockingSoundReference.json`, tolerancia 1e-4). 15 clips estéreo (la nota de «Se suma» sube con la racha: 8 clips) pesan 10,3 MB y se calculan de a poco durante la cuenta regresiva (92 cuadros; 365 ms de cálculo en total en el PC).
6. **16:9.** `DockingPlan` achica todo lo que va bajo la franja de avisos con una escala K (entre 0,6 y 1; el texto de los botones nunca baja de 14 dp; en la v3 ya no hay estación arriba). Con el tutorial se reservan 96 dp abajo para los controles de Nubi.
7. **Pieza del tutorial:** una L de 4 bloques (siempre quiral). Las dos prácticas que se tocan «aseguran» la respuesta (`EnsureAnswer`) y los pasos esperan el fin de la revelación con una bandera propia (`_revealing`): la fase del juego sigue en «Reveal» hasta que alguien pasa a la siguiente.
8. **El smoke ahora falla si un tutorial no llega a su último aviso («¡Listo!»)** en los 100 s de tope. Antes un tutorial trabado se daba por bueno al llegar al tope (lo descubrió justo este: la primera versión de la práctica se quedaba esperando el fin de la revelación y el smoke decía OK).

**Telemetría nueva** (`StroopTelemetry`): `docked`, `rings`, `dock_best`, `dock_new`; la app manda el récord en `SequenceInitConfig.dock_best`. En la app: `data/Acoplamiento.kt` (líneas del final, récord que nunca baja, totales), preferencias `acoplamiento_record` (claves `best`, `total` y `rings`; en el respaldo y borradas con «Borrar datos») y `GameResultScreen` (módulos acoplados, anillos completos, récord y «Has acoplado N módulos · M anillos» antes de las dos medidas de siempre).

**Pruebas.** Unity: 42 en `Tests/` (`DockingContractTests` 7, `DockingV2Tests` 21: 4 × 4 en 12 niveles × 600 semillas, textos del aviso, tiempos, pantalla de 780 y de 16:9, franja libre en 7 alturas con y sin tutorial, estación y casilleros, arte; `DockingSoundTests` 14: fidelidad de 9 sonidos, nota por racha, niveles de la tabla, memoria y horneado). Smoke: `TutorialAcoplamiento` en 3 formas de pantalla, `Acoplamiento`, `AcoplamientoReto`, `AcoplamientoMudo` (con el sonido apagado nada suena), `PantallaAcoplamiento` y `HowToAcoplamiento`. Kotlin: 17 nuevas (`AcoplamientoTest`, `AcoplamientoRecordTest`, el tutorial, el respaldo y la captura de la pantalla final).

**Capturas reales:** `bash tools/verificar-todo.sh --capturas Acoplamiento` → `docs/previews/capturas/acoplamiento.png`.

## 13. Acoplamiento v3 (Tarea 118, 17-oct): sin estación, plasticina, la pieza almohada con lámpara fija

Ricardo aprobó la hoja [`previews/plasticina/propuesta-acoplamiento-v3.png`](previews/plasticina/propuesta-acoplamiento-v3.png) (Tarea 117; había rechazado las bolitas de la v2: «rellenar por rellenar») y se integró tal cual. **No cambian** las reglas, el DDA, los tiempos (420 ms de llegada, 320 + 300 ms de giro y vuelta, 380 ms de bajada, 220 + 520 + 380 ms después), las medidas, la telemetría (anillos = módulos ÷ 8) ni la app Kotlin. Marca: `estilo 17-oct · acoplamiento v3`.

**Qué se ve ahora**
| Pieza | v3 |
|---|---|
| la **pantalla** | SIN estación: el bloque de la pieza y del hueco sube de 26 a **38 dp** (31 en 16:9), la pieza gira en un círculo de 215 dp y el panel del puerto mide 245 × 214 dp; la franja de avisos sube a y 118-150; los botones siguen en 158 × 76 dp (≥ 56 en las 8 formas de pantalla) y el texto más chico es de 14 dp (`DockingPlan`) |
| el **avance** | una fila de **8 luces redondas de plasticina** (22 dp, cada 30 dp, y 100) junto al marcador; al 8.º módulo una onda lima las recorre (40 ms por luz; con «quitar animaciones», de golpe), el aviso dice «¡Anillo completo!» y una **medalla lima** cuenta los anillos |
| el **encaje** | «plop»: la pieza baja al hueco (380 ms), se hunde un 3,5 % mientras el cordón se pone lima (220 ms) y queda a ras del fondo (sin canto ni sombra, con la sombra de la pared) con el visto lima del kit y los chevrones lima; en ese momento se enciende la luz con su halo y sale «¡Encaja!» en la franja |
| la **pieza** | una **almohada de plasticina sin adornos** (`DockingArt`): la silueta del SDF desenfocada como relieve, canto de 5 dp hacia abajo y sombra de contacto, celeste cálido, grano fino y la **lámpara FIJA** en la pantalla (brillo arriba a la izquierda, sombra abajo a la derecha) |
| el **hueco** | una **cavidad**: cordón de plasticina (verde agua en reposo, lima al encajar, celeste al mostrar; un tubo gris que se tiñe), pared de 6 dp con la misma lámpara (en sombra arriba y a la izquierda, con luz abajo y a la derecha) y fondo oscuro; con el arreglo del alfiler de 2 × 2 |
| el **panel «Puerto»**, **«Encaja»** y **«Espejo»** y los **avisos** | las losas del kit (noche; lima y uva; chica dorada o crema) con el texto liso de tinta; los botones con la receta de VERDAD / DISPARATE (estado presionado y rebote) y su ícono de churro de tinta |
| los **chevrones** y las **luces** | atlas `acoplamiento` (Blender): 4 chevrones de churro, 3 luces (apagada, oro, lima) y los dos íconos de los botones |
| la **pantalla final** | «tu estación» como una **escultura** de las piezas que acopló la persona (hasta 3 anillos de 8 alrededor de un núcleo, pistas lima en los completos); los datos de siempre no cambian |

**Cómo está hecho**
1. **La lámpara se hornea en la orientación que se muestra** (no un shader): `DockingArt.BakeFace` evalúa el SDF ya girado y espejado EN LA PANTALLA y la luz sale de esa silueta, así que durante la decisión no hay nada que delate el espejo (la pieza espejada en pantalla y la de celdas reflejadas dan lo mismo: fuga 0). Por módulo se hornean la pieza MOSTRADA y el hueco (cavidad y cordón) al empezar, y la ALINEADA (0° y sin espejo) y la ASENTADA mientras se decide (a los 2 cuadros) o al empezar la revelación si todavía no están; al responder el sprite pasa a la alineada y gira y se espeja con su transform como antes (la lámpara gira esos 0,6 s con la pieza, pero ya contestó). 96 px por bloque (la pantalla los muestra a 114); el relieve y la lámpara se calculan a un cuarto de resolución y el SDF exacto solo en el borde.
2. **El arreglo del 2 × 2:** `PieceGeo` suma una caja de 2 × 2 por cada 2 × 2 de bloques; sin ella el punto donde se juntan 4 bloques queda en d = 0 y se dibujaba un alfiler translúcido.
3. **Sin estación:** se quitaron `BuildStation`, `RefreshStation`, `SlotTarget`, `AnimateSlotFlash`, las sprites de los anillos y casilleros, la luz y el brillo con máscara del módulo y el vuelo. `_stationShown` sigue siendo la cuenta de módulos acoplados en la sesión (incluye la práctica del tutorial): ahora enciende las luces de la fila; las piezas que acopló la persona (sin la práctica) se guardan en `_dockedShapes` para la escultura.
4. **Tutorial:** el paso 3 dice «¡Encaja! Se enciende una luz» y ilumina la fila de luces (`CoachTexts.Acoplamiento.Docks`; ya no nombra la estación ni los casilleros).
5. **Sin el atlas** (o con `PlasticArt.Disabled`) el panel, los botones, los íconos y los chevrones caen a lo de arcilla de siempre (`DockingSprites`); la pieza, el hueco y la escultura no usan atlas.

**Pruebas** (`DockingV2Tests`, `DockingV3Tests`): el plano v3 (números de la hoja y 8 formas de pantalla, con y sin tutorial: nada se pisa, botones ≥ 56 dp, textos ≥ 14 dp); IoU de la cara contra el SDF del juego ≥ 0,99 (5 piezas, giradas y espejadas); fuga del espejo = 0 (el 8 bits más cercano); lo que se descartó (la luz en el marco con espejo por escala) cae ≥ 8 px del otro lado; el 2 × 2 sin agujero; el contraste del filo oscuro ≥ 3 : 1; el hueco es la silueta de la pieza con el cordón por fuera y la pared de abajo a la derecha más clara que la de arriba a la izquierda; la pieza asentada sin canto ni sombra; la escultura; nada de la estación en la pantalla de juego y 8 luces.
