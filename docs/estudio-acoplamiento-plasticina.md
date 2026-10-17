# Estudio de la plasticina de Acoplamiento — Fase A (Tarea 113, 16-oct-2026)

> Solo estudio: el juego no se tocó. Lo escribe `tools/juegos/acoplamiento_estudio.py` (`acoplamiento.py` hace las placas de Blender; `acopla_geo.py`, `acopla_caminos.py`, `acopla_medidas.py` y `acopla_hojas.py` son el arte de hoy en numpy, los dos caminos, las medidas y las hojas). Hojas:
> [`previews/plasticina/acoplamiento-estudio.png`](previews/plasticina/acoplamiento-estudio.png) (las 5 piezas, los acercamientos, la fuga del espejo y los bordes del hueco) y
> [`previews/plasticina/acoplamiento-estudio-pantallas.png`](previews/plasticina/acoplamiento-estudio-pantallas.png) (hoy, camino 1 y camino 2 lado a lado, a tamaño de teléfono sobre el fondo real).

## Resumen y recomendación

**La pregunta:** módulo y hueco salen del MISMO SDF (unión de cajas redondeadas de radio 0,16 bloque; hay 83 pares de siluetas quirales y la pieza gira y se espeja con la escala x −1), así que la plasticina no puede mover el contorno ni delatar el espejo con una luz horneada.
Se compararon **el camino 1** (cada bloque en 4 cuartos; cada cuarto elige una de 4 baldosas de Blender según sus 3 vecinos) y **el camino 2** (el contorno exacto del SDF de hoy; el relieve, de una tabla 1-D medida en un borde recto de Blender), con las 5 piezas reales (L4, F5, P5, C6 y H7).

| | Hoy | Camino 1 (baldosas) | Camino 2 (SDF + perfil) |
|---|---|---|---|
| Silueta (47 u, el bloque más chico): IoU / desvío máx. | 0,995–0,998 / 0,18 px | 0,996–0,998 / 0,18 px (aro del hueco: 0,26 a 47 u y 0,55 a 78 u) | 0,991–0,997 / 0,11 px (con rampa de 1 px: 0,997–0,999 / 0,17 px) |
| En concavidades (máx.) | 0,18 px | 0,12 px (aro del hueco 0,55 a 78 u) | 0,09 px |
| Fuga del espejo: modulación direccional del canto (1.er armónico, niveles de 255; el control con UNA luz sin simetrizar da 1,24) | 0,20 | 0,01 | 0,03 (isótropo POR CONSTRUCCIÓN) |
| «+» y juntas en un 2 × 2 | juntas de 42 niveles y un «+» de −55 | sin juntas (0,24 contra 0,25 de fondo) | sin juntas (0,00); **exige arreglar el SDF** (ver abajo) |
| Peso | — | ≈ 40 KB (12 baldosas) | ≈ 1 KB (3 tablas) |
| Costo por pieza de 7 bloques (Editor de Unity, Mono) | 381 ms | **0,55 ms** | 330 ms (≈ 85 ms ESTIMADOS con el SDF a 1/2 de resolución, 1/4 de los píxeles: IoU igual, desvío ≤ 0,36 px) |
| Relieve contra la verdad en 3D | — | es la verdad | diferencia de luz media 0,5–0,7 niveles de 255; p99 de 11 en esquinas convexas |
| Hueco (aro teal / lima / celeste y fondo) | 2 sprites | baldosas con margen de 12 px y el cuarto cóncavo se pone al final | las dos capas salen del mismo SDF con dos tablas: trivial |

**Recomendación: camino 2**, con tres arreglos que el estudio encontró:
1. **El SDF necesita una caja de 2 × 2 por cada 2 × 2 de bloques.** Con solo cajas y puentes de 1 × 2, el punto donde se juntan 4 bloques queda en d = 0 aunque esté 0,5 bloque adentro de la pieza. En el camino 2 eso dibuja un alfiler translúcido; **en el juego de HOY ya existe**: el fondo del hueco tiene un agujero de 2 × 2 píxeles (alfa 0) en el cruce de todo 2 × 2 y deja ver el borde teal. Lo tienen el 30 % de las siluetas (50 de 166: 2 de 12 de 5 bloques, 8 de 42 de 6 y 40 de 108 de 7). Es un defecto del arte de hoy y se arregla con las mismas 4 líneas.
2. **El costo de hornear.** Hoy `BakePiece` tarda ~381 ms en el Editor (casi todo es el SDF: se evalúa en 384² píxeles contra hasta 19 cajas con búsquedas en un `HashSet`); aquí el camino 2 no mejora (330 ms) y el camino 1 lo elimina (0,55 ms). Para el camino 2, calcular el SDF a media resolución (192²) y subirlo por interpolación lo dejaría en ~1/4 (estimado) sin tocar la fidelidad (medido en numpy: IoU 0,997–0,999, desvío ≤ 0,36 px). No se midió en el teléfono: antes de decidir conviene cronometrar `BakePiece` en el Motorola.
3. **La rampa de suavizado a 1 px** (hoy 1,5 px): el IoU a 47 u pasa de 0,991–0,997 a 0,997–0,999 (el 0,99 que se pide queda muy justo con 1,5 px).

**Por qué no el camino 1:** pasa todos los criterios y es mucho más rápido, pero el módulo y el hueco quedarían de dos fuentes ráster distintas (el aro del hueco llega a 0,55 px de desvío a 78 u: más de tres veces lo de hoy, 0,17) y el aro exige márgenes de baldosa, un orden de pegado y un caso especial en la esquina cóncava; además cualquier cambio del relieve pide volver a Blender (en el camino 2 se sube o baja el relieve en la tabla). **Plan B** si el cronómetro del teléfono dice que el SDF a 1/2 sigue siendo lento: las baldosas para el módulo y el SDF solo para el hueco.

**Lo que el estudio dejó claro sobre la plasticina:** con la luz promediada en 4 giros y espejos el módulo queda casi plano: el canto blando de 0,075 bloque baja solo 14 niveles de 255 (7 %) a 0,02 bloque del borde, y la textura fina del kit casi desaparece (0,35 niveles). Es «plasticina» sin dirección, y justo por eso no hay fuga. Si Ricardo quiere más cuerpo (canto más ancho, oclusión, un grano que se vea) conviene decidirlo en la Fase B: en el camino 2 se sube el relieve en la tabla (la hoja incluye una columna con el relieve ×2,5) y el grano es una textura isótropa que se repite.
Además: el hueco se renderiza con el kit a ≈ (36, 34, 40) y no a #070818 (la plasticina oscura se aclara con el rebote y el SSS); se conserva su sombreado relativo y se lleva al negro de hoy. Y el aro blanco sale con el kit a ≈ (193–230) y no a 255: teñido de teal, lima o celeste queda un 10 a 25 % más oscuro que el de hoy (se compensa con el color base o la exposición).

---

# Las medidas


Las cinco piezas reales (poliominós quirales de 4 a 7 bloques que caben en 4 × 4, como `RandomChiralShape`):

| Pieza | Bloques | Con qué | Cuartos: convexa / borde / cóncava / interior |
|---|---:|---|---|
| L4 | 4 | la L del tutorial | 5 / 10 / 1 / 0 |
| F5 | 5 | concavidades de 1 bloque (una F) | 7 / 10 / 3 / 0 |
| P5 | 5 | un 2 × 2 (una P) | 5 / 10 / 1 / 4 |
| C6 | 6 | un hueco de 1 bloque de ancho (una C) | 7 / 14 / 3 / 0 |
| H7 | 7 | siete bloques, un hueco de 1 bloque y un 2 × 2 | 6 / 16 / 2 / 4 |

**Las siluetas del juego:** 166 (= 83 pares espejo, las «~83»: 4 de 4 bloques, 12 de 5, 42 de 6 y 108 de 7). Sus 4336 cuartos de bloque caen así: 1144 convexos, 2496 de borde, 480 cóncavos, 216 interiores y **0 «pellizcos»** (un cuarto que solo toca a otro por la diagonal): **con 4 baldosas basta**, el quinto caso que se temía no ocurre en ninguna pieza. El interior es solo el 5 % de los cuartos: la pieza es casi todo canto.

## 1. Fidelidad de la silueta contra el SDF (bloque de 47 u, el más chico; y de 78 u, el más grande)

Se mide el alfa del sprite de 384 px (80 px por bloque) visto a 47 y a 78 px por bloque con muestreo bilineal sin prefiltro (la GPU), contra el SDF del juego. **IoU** = Σ mín / Σ máx contra la cobertura EXACTA (4 × 4 sub-muestras por píxel). **Desvío máximo del borde** = la mayor distancia del SDF en los puntos donde el alfa cruza 0,5 (interpolado entre píxeles: precisión de sub-píxel), en px de pantalla. «Concavidades» = puntos a menos de 0,45 bloque de un vértice cóncavo. El módulo se compara con d ≤ 0 (el contorno del SDF; hoy el celeste llega a −0,02 y el borde de tinta a +0,115, y «hoy» se compara con +0,115); el aro del hueco con d ≤ 0,115 y su fondo con d ≤ 0 (hoy: −0,023).

| Pieza · camino | IoU a 47 u | Desvío máx. 47 u (px) | máx. en concavidades | IoU a 78 u | Desvío máx. 78 u (px) | máx. en concavidades |
|---|---:|---:|---:|---:|---:|---:|
| L4 · hoy (con tinta) | 0.9964 | 0.13 | 0.12 | 0.9914 | 0.14 | 0.14 |
| L4 · camino 1 | 0.9973 | 0.13 | 0.12 | 0.9964 | 0.12 | 0.06 |
| L4 · camino 2 | 0.9919 | 0.10 | 0.04 | 0.9893 | 0.04 | 0.02 |
| L4 · camino 2, rampa de 1 px | 0.9989 | 0.17 | 0.08 | 0.9980 | 0.10 | 0.03 |
| L4 · camino 2, SDF a 1/2 de resolución (192²) | 0.9989 | 0.17 | 0.16 | 0.9980 | 0.32 | 0.32 |
| L4 · camino 2, SDF a 1/3 de resolución (128²) | 0.9988 | 0.50 | 0.50 | 0.9979 | 0.50 | 0.50 |
| F5 · hoy (con tinta) | 0.9947 | 0.18 | 0.18 | 0.9925 | 0.16 | 0.16 |
| F5 · camino 1 | 0.9972 | 0.13 | 0.12 | 0.9967 | 0.12 | 0.06 |
| F5 · camino 2 | 0.9965 | 0.10 | 0.04 | 0.9900 | 0.04 | 0.04 |
| F5 · camino 2, rampa de 1 px | 0.9972 | 0.17 | 0.08 | 0.9982 | 0.10 | 0.04 |
| F5 · camino 2, SDF a 1/2 de resolución (192²) | 0.9972 | 0.17 | 0.08 | 0.9981 | 0.33 | 0.33 |
| F5 · camino 2, SDF a 1/3 de resolución (128²) | 0.9972 | 0.29 | 0.29 | 0.9980 | 0.54 | 0.54 |
| P5 · hoy (con tinta) | 0.9972 | 0.12 | 0.09 | 0.9927 | 0.15 | 0.15 |
| P5 · camino 1 | 0.9981 | 0.13 | 0.03 | 0.9970 | 0.11 | 0.03 |
| P5 · camino 2 | 0.9936 | 0.10 | 0.07 | 0.9914 | 0.04 | 0.03 |
| P5 · camino 2, rampa de 1 px | 0.9991 | 0.17 | 0.08 | 0.9982 | 0.10 | 0.03 |
| P5 · camino 2, SDF a 1/2 de resolución (192²) | 0.9991 | 0.22 | 0.22 | 0.9982 | 0.30 | 0.30 |
| P5 · camino 2, SDF a 1/3 de resolución (128²) | 0.9990 | 0.49 | 0.49 | 0.9981 | 0.54 | 0.54 |
| C6 · hoy (con tinta) | 0.9978 | 0.13 | 0.11 | 0.9917 | 0.17 | 0.17 |
| C6 · camino 1 | 0.9962 | 0.18 | 0.10 | 0.9973 | 0.13 | 0.08 |
| C6 · camino 2 | 0.9909 | 0.11 | 0.09 | 0.9911 | 0.05 | 0.04 |
| C6 · camino 2, rampa de 1 px | 0.9975 | 0.17 | 0.08 | 0.9982 | 0.11 | 0.04 |
| C6 · camino 2, SDF a 1/2 de resolución (192²) | 0.9974 | 0.17 | 0.15 | 0.9982 | 0.36 | 0.36 |
| C6 · camino 2, SDF a 1/3 de resolución (128²) | 0.9973 | 0.50 | 0.50 | 0.9981 | 0.58 | 0.58 |
| H7 · hoy (con tinta) | 0.9980 | 0.13 | 0.11 | 0.9927 | 0.14 | 0.14 |
| H7 · camino 1 | 0.9960 | 0.18 | 0.06 | 0.9976 | 0.13 | 0.08 |
| H7 · camino 2 | 0.9923 | 0.11 | 0.09 | 0.9926 | 0.05 | 0.04 |
| H7 · camino 2, rampa de 1 px | 0.9972 | 0.17 | 0.08 | 0.9983 | 0.11 | 0.04 |
| H7 · camino 2, SDF a 1/2 de resolución (192²) | 0.9971 | 0.17 | 0.15 | 0.9983 | 0.36 | 0.36 |
| H7 · camino 2, SDF a 1/3 de resolución (128²) | 0.9970 | 0.37 | 0.37 | 0.9982 | 0.50 | 0.50 |

**El hueco**, en dos capas como hoy (el aro debajo, la silueta de d ≤ 0,115; el fondo encima, d ≤ 0):

| Pieza · capa · camino | IoU a 47 u | Desvío máx. 47 u (px) | máx. en concavidades | IoU a 78 u | Desvío máx. 78 u (px) | máx. en concavidades |
|---|---:|---:|---:|---:|---:|---:|
| L4 · aro · hoy | 0.9964 | 0.13 | 0.12 | 0.9914 | 0.14 | 0.14 |
| L4 · fondo · hoy | 0.9933 | 0.13 | 0.13 | 0.9915 | 0.04 | 0.04 |
| L4 · aro · camino 1 | 0.9962 | 0.19 | 0.19 | 0.9926 | 0.51 | 0.51 |
| L4 · fondo · camino 1 | 0.9973 | 0.13 | 0.12 | 0.9964 | 0.12 | 0.06 |
| L4 · aro · camino 2 | 0.9964 | 0.13 | 0.12 | 0.9914 | 0.14 | 0.14 |
| L4 · fondo · camino 2 | 0.9919 | 0.10 | 0.04 | 0.9893 | 0.04 | 0.02 |
| F5 · aro · hoy | 0.9947 | 0.18 | 0.18 | 0.9925 | 0.16 | 0.16 |
| F5 · fondo · hoy | 0.9972 | 0.12 | 0.12 | 0.9941 | 0.05 | 0.05 |
| F5 · aro · camino 1 | 0.9962 | 0.25 | 0.25 | 0.9937 | 0.55 | 0.55 |
| F5 · fondo · camino 1 | 0.9972 | 0.13 | 0.12 | 0.9967 | 0.12 | 0.06 |
| F5 · aro · camino 2 | 0.9947 | 0.18 | 0.18 | 0.9925 | 0.16 | 0.16 |
| F5 · fondo · camino 2 | 0.9965 | 0.10 | 0.04 | 0.9900 | 0.04 | 0.04 |
| P5 · aro · hoy | 0.9972 | 0.12 | 0.09 | 0.9927 | 0.15 | 0.15 |
| P5 · fondo · hoy | 0.9948 | 0.13 | 0.11 | 0.9928 | 0.04 | 0.02 |
| P5 · aro · camino 1 | 0.9973 | 0.26 | 0.26 | 0.9936 | 0.54 | 0.54 |
| P5 · fondo · camino 1 | 0.9981 | 0.13 | 0.03 | 0.9970 | 0.11 | 0.03 |
| P5 · aro · camino 2 | 0.9972 | 0.12 | 0.09 | 0.9927 | 0.15 | 0.15 |
| P5 · fondo · camino 2 | 0.9936 | 0.10 | 0.07 | 0.9914 | 0.04 | 0.03 |
| C6 · aro · hoy | 0.9978 | 0.13 | 0.11 | 0.9917 | 0.17 | 0.17 |
| C6 · fondo · hoy | 0.9914 | 0.17 | 0.17 | 0.9918 | 0.08 | 0.08 |
| C6 · aro · camino 1 | 0.9975 | 0.22 | 0.20 | 0.9927 | 0.51 | 0.51 |
| C6 · fondo · camino 1 | 0.9962 | 0.18 | 0.10 | 0.9973 | 0.13 | 0.08 |
| C6 · aro · camino 2 | 0.9978 | 0.13 | 0.11 | 0.9917 | 0.17 | 0.17 |
| C6 · fondo · camino 2 | 0.9909 | 0.11 | 0.09 | 0.9911 | 0.05 | 0.04 |
| H7 · aro · hoy | 0.9980 | 0.13 | 0.11 | 0.9927 | 0.14 | 0.14 |
| H7 · fondo · hoy | 0.9923 | 0.17 | 0.17 | 0.9925 | 0.08 | 0.08 |
| H7 · aro · camino 1 | 0.9979 | 0.22 | 0.20 | 0.9936 | 0.51 | 0.51 |
| H7 · fondo · camino 1 | 0.9960 | 0.18 | 0.06 | 0.9976 | 0.13 | 0.08 |
| H7 · aro · camino 2 | 0.9980 | 0.13 | 0.11 | 0.9927 | 0.14 | 0.14 |
| H7 · fondo · camino 2 | 0.9923 | 0.11 | 0.09 | 0.9926 | 0.05 | 0.04 |

## 2. Fuga del espejo

El juego muestra el espejo como el MISMO sprite con escala x −1 (`DockingGameController.Scene.cs`): lo que el sprite lleve horneado con dirección (una luz, un relieve) cambia de lado y lo delata. Dos medidas, en niveles de 255: (a) la **asimetría izquierda-derecha de la luz** en una pieza simétrica (una T de 5 bloques, igual a su espejo): |L(S) − L(espejo(S))|. OJO: como el camino 1 arma el lado derecho con las baldosas del izquierdo reflejadas, una pieza simétrica sale simétrica pase lo que pase con la luz (también en el control): por eso esta medida vale 0 en todos y la que delata es la (b); (b) la **modulación direccional del canto** de las 5 piezas reales (la luminosidad de una banda de 0,12 bloque dentro del contorno según el azimut de la normal exterior; el primer armónico es una luz con dirección). Además el alfa del sprite de una pieza espejada y el espejo del sprite de la pieza coinciden (diferencia máxima de alfa 0,000 en los tres caminos). «Control» = el camino 1 con UNA sola luz sin simetrizar: lo que pasaría con un render de luz horneada.

| Camino | (a) Asimetría L-R de la T: máx. | media | (b) Dir. 1.er armónico (peor de las 5) | 2.º armónico |
|---|---:|---:|---:|---:|
| hoy | 0.00 | 0.00 | 0.20 | 0.42 |
| camino 1 | 0.00 | 0.00 | 0.01 | 0.05 |
| camino 2 | 0.00 | 0.00 | 0.03 | 0.04 |
| control (1 luz) | 0.00 | 0.00 | 1.24 | 2.53 |

El módulo es casi plano (la caída del canto es de unos 14 niveles de 255: ver §5), así que incluso la luz horneada de UNA sola dirección delata poco (≈ 1 nivel de modulación y 1 a 3 niveles de asimetría): la fuga existe y el promedio de 4 luces con espejos la deja en ≈ 0, pero el riesgo real es menor que el que se temía; crece si se sube el relieve (con el camino 2, ×2,5 o más, la tabla es isótropa por construcción: no cambia).

## 3. ¿Se ven juntas o un «+» dentro de piezas de 2 × 2?

En las piezas con un 2 × 2 (P5 y H7): el salto de luz (media de |ΔL| entre píxeles a un lado y otro de la línea, en niveles de 255) en las líneas donde se juntan los BLOQUES y donde se juntan los CUARTOS de baldosa, contra el salto «de fondo» (lo que cambia la luz en el interior sin junta alguna); y la luz del centro del «+» (disco de 5 px menos anillo de 9 a 13 px). «camino 2, sin arreglo» = el SDF tal cual está hoy.

| Pieza · camino | Salto en juntas de bloque | Salto en juntas de cuarto | Salto de fondo | «+» en el centro (niveles) |
|---|---:|---:|---:|---:|
| P5 · hoy | 41.80 | 0.00 | 7.00 | -54.77 |
| P5 · camino 1 | 0.24 | 0.27 | 0.25 | +0.31 |
| P5 · camino 2 | 0.00 | 0.03 | 0.04 | +0.03 |
| P5 · camino 2, sin arreglo | 0.00 | 0.03 | 0.04 | -5.27 |
| H7 · hoy | 41.80 | 0.00 | 7.18 | -54.77 |
| H7 · camino 1 | 0.23 | 0.27 | 0.24 | +0.31 |
| H7 · camino 2 | 0.00 | 0.03 | 0.03 | +0.03 |
| H7 · camino 2, sin arreglo | 0.00 | 0.03 | 0.04 | -5.27 |

Hallazgo del camino 2: con el SDF TAL COMO ESTÁ (cajas y puentes de 1 × 2), el punto donde se juntan 4 bloques de un 2 × 2 queda justo en el borde de los cuatro puentes (d = 0 aunque esté 0,5 bloque adentro de la pieza): ningún píxel cae exactamente ahí, pero el vecino más cercano da d ≈ −0,006 y se dibuja un ALFILER translúcido (alfa 0,83) del color del canto. Hoy no se ve porque la junta de tinta lo tapa. Se arregla sumando al SDF una caja de 2 × 2 por cada 2 × 2 de bloques (4 líneas, sin costo de importancia); con ese arreglo el «+» desaparece.

## 4. Cuánto se parece el relieve del camino 2 a las baldosas de Blender (el camino 1 es la verdad en 3D)

Diferencia de luminosidad (niveles de 255) entre el camino 2 y el camino 1 en la banda de 0,14 bloque dentro del contorno, por clase de cuarto (los cuartos de cada pieza se clasifican por sus 3 vecinos). Es el error del perfil 1-D (el borde recto) cuando se lo aplica a esquinas.

| Clase de cuarto | Píxeles | Diferencia de luz media | p99 | máx. |
|---|---:|---:|---:|---:|
| convexa | 19620 | 0.68 | 10.96 | 15.03 |
| borde | 24000 | 0.53 | 2.95 | 3.40 |
| concava | 1200 | 0.66 | 7.08 | 7.08 |
| interior | 0 | — (no hay banda de canto en un cuarto interior) | — | — |

## 5. Cuánto relieve hay (la «plasticina» que se ve)

- Perfil del canto del módulo (luminosidad en niveles de 255; promedio de 4 luces con espejos, a lo largo de un borde recto): interior 198.6; mínimo 184.9 a 0.022 bloque del borde. La caída máxima es de 13.7 niveles (6.9 %): el canto blando de 0,075 bloque (≈ 6 px a 80 por bloque) se nota como un velo apenas más oscuro. Con la luz promediada en 4 giros el relieve queda SIN dirección y por eso es tan suave; si hace falta más «plasticina» se sube el canto o se le suma oclusión (se ve igual en los dos caminos).
- Textura fina del kit en el interior (placa lisa de 4,8 bloques, luz promediada): desviación 0.35 niveles de 255. El grano propio de la plasticina casi no sobrevive al promedio de 4 luces y al reductor de ruido; si se quiere un grano que se vea, se le suma una textura isótropa de periodo corto: lo admiten los dos caminos por igual (camino 1: ya horneada en las baldosas; camino 2: una textura de 128 × 128 que se repite).

## 6. Peso y costo

- **Camino 1:** 12 baldosas (4 del módulo de 40 × 40 px, 4 del aro de 40 a 52 px y 4 del fondo de 40 × 40): 85.5 KB en RGBA32, ≈ 14.2 KB en PNG; en un atlas con su relleno ≈ 40 KB (a 6 px de canto, 8 baldosas por capa si se quisiera más variedad).
- **Camino 2:** tres tablas 1-D de color (módulo, aro y fondo; 128 entradas de RGB = 1,1 KB en total) y, solo si se quiere grano, una textura isótropa de 128 × 128 (16 KB en gris). Sin baldosas.

Tiempo por pieza de 7 bloques, solo en numpy (el orden de magnitud, no el del teléfono): hoy 322 ms · camino 1 10 ms · camino 2 266 ms · camino 2 con el SDF a 1/2 de resolución (solo el módulo) 46 ms.

**Medido en Unity (Editor con Mono, esta PC; una pieza de 7 bloques, promedio de 20 horneos; NO medido en el teléfono: el número absoluto allá será otro, la proporción es la que importa):**

- hoy BakePiece (3 capas, 3 texturas, 384^2): 380,8 ms
- camino 2 prototipo (SDF una vez por pixel + tabla, modulo y hueco, sin texturas): 329,7 ms
- camino 1 prototipo (28 cuartos x 2 capas, pegado de baldosas, sin texturas): 0,55 ms
- 2 texturas 384^2 (crear + SetPixels32 + Apply, lo comun a los tres): 0,1 ms

