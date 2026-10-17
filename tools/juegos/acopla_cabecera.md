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

