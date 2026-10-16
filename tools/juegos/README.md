# Los juegos en plasticina (tools/juegos)

El arte de los juegos de Unity pasa, juego por juego, de los sprites dibujados por código (`ClayRaster`) a **plasticina renderizada en Blender**, con la misma luz y el mismo material que el kit de la app y la isla. Es un paso de ARTE, a mano: ninguna verificación lo corre y **ni la app ni Unity dependen de Blender** (lo que entra al repo son los atlas ya hechos en `unity/NeuroVidaCore/Assets/Resources/Plasticina/`). Seguimiento de cada juego (inventario, estado, pesos): [`docs/plasticina-juegos.md`](../../docs/plasticina-juegos.md).

## La cadena

```
Blender 4.5 ──► tools/juegos/out/<grupo>/*.png ──► empaqueta.py ──► Assets/Resources/Plasticina/<grupo>/{atlas.png, manifest.json (+ .meta)}
 (radar.py, comun.py)   (fondo transparente)        (reduce, limpia,        │
                                                     atlas, pesos)          ▼
                                                              PlasticArt (C#) → sprite de plasticina, o el procedural de siempre si no existe
```

| Archivo | Para qué |
|---|---|
| `plasticina.py` | El módulo común de Blender. **No copia nada de `tools/isla/isla.py`: lo TOMA** (lee su árbol de sintaxis y ejecuta solo `clay()`, `studio()`, `compositor()`, `setup_render()` y las funciones de las losas del kit), así que si la luz o el material cambian, los juegos cambian con ellos. Pone la cámara ortogonal inclinada del kit (con la compensación 1/cos para que las formas planas salgan como se dibujaron), el suelo que solo recibe sombra, `pieza()` (un contorno levantado con el canto redondo) y el render con fondo transparente. |
| `formas.py` | Las formas con silueta que comparten Rescate y Piloto: `contorno_forma(tipo, extra)` (las mismas SDF de `RadarSprites.ShapeSdf` y `PilotSignalSprites.ShapeSdf`), `recorta_banda` (la franja) y `engorda`. |
| `radar.py` | Rescate relámpago: las 6 cápsulas (256 px) y sus íconos (128 px), la roca, el aro del radar (con consola y antena), la nave con sus 10 asientos y la bolita que los llena. Escribe `medidas.json` (dónde caen los asientos…). |
| `meteoros.py` | Lluvia de meteoros: las 4 rocas (nine-slice, gris claro que el código tiñe, con cráteres), sus 4 presionadas (huella en una esquina) y las 2 estrellas de la constelación (esferas). |
| `meteoros_capas.py` | La brasa del borde que va por delante y las grietas de la roca de polvo (numpy, sobre el mismo lienzo y la misma silueta que la roca: son luz y marca, no plasticina). |
| `mide_meteoros.py` | La roca (cuerpo, margen), la presionada, el color teñido contra el de diseño, el contraste de la palabra sobre la placa crema y las estrellas. |
| `constelaciones.py` | Constelaciones: los 12 objetos y los 4 gemelos (1 unidad del boceto = 1/20 de unidad de Blender: relieves apilados de plasticina; el planeta, una esfera de verdad con su anillo inclinado y sin sombra de contacto), la luz dormida (esfera azul noche con un grano claro), su presionada (aplastada, con la huella lejos del núcleo) y la luz abierta (disco claro). |
| `mide_parejas.py` | Los 16 objetos (silueta, borde de tinta, contraste), los gemelos, la pareja de objetos más parecida en luminancia, las luces y la presionada, y el peso. |
| `hoja_parejas.py` | Arma `docs/previews/plasticina/parejas-objetos.png`: los 16 objetos sobre su luz abierta como los ve el juego, y la luz dormida y la presionada. |
| `bodega.py` | Bodega de carga: los 12 objetos (relieves de plasticina sin borde de tinta), las piezas de la escotilla (marco, aro, tres interiores y la hoja de la puerta, normal y presionada con la huella de un dedo; la hoja GIRA por código: se renderiza sin luz direccional, promediando cuatro renders con la luz girada 0°, 90°, 180° y 270°), el robot (cuerpo, visor, ojo, tallo, bolita), la caja (una cinta), la esclusa y los tres círculos del carro. 1 dp = 1/20 de unidad de Blender. |
| `mide_bodega.py` | Los 12 objetos (tamaño, contraste contra el interior oscuro, pares parecidos en luminancia), la escotilla (la hoja, la presionada y su huella), el robot, la caja, la esclusa, el carro y el peso. |
| `hoja_bodega.py` | Arma `docs/previews/plasticina/bodega-piezas.png`: los 12 objetos dentro de su escotilla, la escotilla cerrada y presionada, el robot armado, la caja, la esclusa y los círculos del carro. |
| `mide_presionados.py` | Los estados presionados de Piloto, Freno, Satélites y Punta contra los normales: tamaño y margen del lienzo, centro, color (luminosidad y ΔE), tinta del detalle y el detalle fino donde cae la huella. |
| `hoja_presionados.py` | Arma `docs/previews/plasticina/presionados.png`: un cuadro normal y uno presionado de lo que se toca en cada juego, con los sprites reales de los atlas. |
| `piloto.py` | Piloto Estelar: las 15 señales (5 formas × punto, anillo y franja, con el detalle de churro de tinta incrustado) y la nave. Las maestras salen a 384 px; `empaqueta.py` saca de la misma maestra la versión de 192 y la chica de 96. |
| `cohete.py` | El cohete de plasticina que comparten Piloto (la nave) y Freno (los de cada carril, con el acento coral, cielo o lima). Solo se importa desde scripts de Blender. |
| `freno.py` | Freno de Emergencia: el botón de lanzamiento (apagado y encendido), los 3 cohetes, la plataforma y la señal de ALTO, cada uno en el mismo lienzo que el sprite procedural al que reemplaza. |
| `calculo.py` | Carga exacta: el aro del reactor (dona de azul noche) y su núcleo (domo de plasticina gris claro que el código tiñe). 1 unidad de Blender = 84 dp (la escena del estudio no admite objetos de cientos de unidades: la cámara los cortaba). |
| `disparate.py` | ¿Verdad o disparate?: la antena parabólica (lienzo ±1,2: de ahí salen los puntos `AntennaTip`/`AntennaDish`/`AntennaFloor`; el plato es una malla de revolución inclinada), los íconos ✓ y onda cortada en dos tramos (churros de tinta) y las barras del medidor (UNA por estado, nine-slice vertical). |
| `satelites.py` | Satélites: el satélite (uno solo), el planeta (una esfera: su silueta es un círculo desde cualquier ángulo, así que NO va dentro de la raíz estirada), el sobre y la nube de polvo (cinco domos achatados). |
| `rastro.py` | Rastro de luz: los 9 luceros (esferas idénticas, del color de `RastroBoard.Colors`, sin sombra de contacto: flotan) y sus 9 estados presionados (`lucero_p_i`: la esfera aplastada, un punto más oscura y con una huella de dedo hundida: malla fina con los vértices desplazados). |
| `stroop.py` | Tinta o Palabra: las 15 losas con el color EXACTO de las 4 tintas, la tarjeta, la cinta y las orillas (con el color de Blender corregido por lo que la luz le hace al tono) y los íconos de la gota y el libro. |
| `punta.py` | En la punta de la lengua: la ficha-letra y la perla del lucero (grises claros, el código las tiñe) y la casilla (cuenco oscuro con aro celeste). |
| `comun.py` | Lo común que NO sale del kit: las casillas «noche» (normal, presionada, acierto, error, apagada), las marcas ✓ y ✗ y el anillo de la cuenta regresiva. También re-renderiza una losa crema de CONTROL para comprobar que este pipeline da el mismo kit que la app. |
| `empaqueta.py` | De los PNG de Blender (y de los WebP del kit de la app y la Nubi 3D) al atlas de cada grupo: reduce con Lanczos y alfa premultiplicado (los renders son la «maestra», a más resolución), limpia la bruma del suelo de sombra, empaca, escribe el manifiesto y los `.meta` (GUID fijo por ruta; un `.meta` que ya existe no se toca: Unity lo reescribe al importar y esa versión es la que va al repo) y **mide cuánto pesa**. |
| `mide_radar.py` | Las cápsulas contra lo que reemplazan (huella) y contra el vidrio del radar (contraste WCAG), y qué tanto se parecen de a pares (escala de grises). |
| `mide_piloto.py` | Las 15 señales y la nave: huella, contraste contra el cielo, tinta contra el cuerpo, qué tanto se distinguen las «parecidas» y lo que miden los detalles a los tamaños reales. |
| `mide_calculo.py` | El reactor de Carga exacta: tamaño y centro, el núcleo claro y el número que rinde sobre sus dos tintes, contraste de las losas de las celdas. |
| `mide_disparate.py` | La antena (inclinación del plato, dónde cae la luz de la punta y el centro del plato, tono contra tono), los íconos contra los botones lima y coral (la onda cortada a 24, 28 y 32 dp) y las barras del medidor. |
| `mide_satelites.py` | Satélites: huella, el satélite sin nada vertical, el planeta oscuro (y cuánto resaltan las luces doradas) y la nube opaca. |
| `mide_rastro.py` | Los 9 luceros: huella, fidelidad del color de diseño (ΔE CIELAB), que los pares cercanos se sigan distinguiendo y contraste contra el cielo; y sus estados presionados (tamaño, color en las mismas reglas de ΔE, cuánto se ve la huella). |
| `mide_stroop.py` | Tinta o Palabra: fidelidad del color de cada tinta (ΔE), separación con daltonismo (CIEDE2000), el nombre sobre cada botón y las 4 tintas sobre el fondo de la tarjeta. |
| `mide_punta.py` | La ficha, la casilla y la perla: tamaño del cuerpo, la letra de tinta sobre cada estado teñido y que los cuatro metales se sigan distinguiendo. |
| `mide_freno.py` | Botón, cohetes, plataforma y ALTO: huella contra el procedural, contraste contra el suelo de la luna y el cielo, flecha contra botón. |
| `huella.py` | Lo que comparten las dos mediciones (la silueta de HOY y el contraste WCAG; solo numpy). |
| `lamina.py` | Una lámina de contacto para MIRAR los renders sobre un fondo liso. |

## Cómo se corre

```
BLENDER=/c/Users/RURAL7/Tools/blender-4.5.14/blender-4.5.14-windows-x64/blender.exe
$BLENDER -b -P tools/juegos/radar.py -- --samples 112 --out tools/juegos/out/radar        # ~10 min con Cycles por CPU (Ryzen 5 5625U); el aro del radar tarda ~4 de eso
$BLENDER -b -P tools/juegos/piloto.py -- --samples 112 --out tools/juegos/out/piloto      # ~10 min (15 señales a 384 px y la nave)
$BLENDER -b -P tools/juegos/freno.py -- --samples 112 --out tools/juegos/out/freno        # ~6 min (botones, cohetes, plataforma, alto)
$BLENDER -b -P tools/juegos/calculo.py -- --samples 112 --out tools/juegos/out/calculo      # ~6 min (el núcleo, el lienzo grande, tarda ~4)
$BLENDER -b -P tools/juegos/disparate.py -- --samples 112 --out tools/juegos/out/disparate  # ~5 min
$BLENDER -b -P tools/juegos/satelites.py -- --samples 112 --out tools/juegos/out/satelites  # ~5 min
$BLENDER -b -P tools/juegos/rastro.py -- --samples 112 --out tools/juegos/out/rastro        # ~15 min (9 esferas y 9 presionadas a 384 px; --solo p renderiza solo las presionadas)
$BLENDER -b -P tools/juegos/stroop.py -- --samples 112 --out tools/juegos/out/stroop        # ~6 min (15 losas y 2 íconos)
$BLENDER -b -P tools/juegos/punta.py -- --samples 112 --out tools/juegos/out/punta          # ~2 min (ficha, casilla y perla)
$BLENDER -b -P tools/juegos/constelaciones.py -- --samples 112 --out tools/juegos/out/parejas   # ~9 min (16 objetos a 448 px, la luz dormida, su presionada y la abierta a 504); --solo obj|luces|obj_K_V|dormante|dormante_p|abierta
$BLENDER -b -P tools/juegos/bodega.py -- --samples 112 --out tools/juegos/out/bodega      # ~14 min (12 objetos a 384 px, la escotilla —las hojas, ×4 luces—, el robot, la caja, la esclusa y el carro); --solo obj|obj_K|escotilla|robot|resto
$BLENDER -b -P tools/juegos/comun.py -- --samples 112 --out tools/juegos/out/comun        # ~5 min
python -I tools/juegos/empaqueta.py radar piloto freno calculo disparate satelites rastro stroop punta meteoros parejas bodega comun nubi   # atlas + manifiestos + pesos (segundos)
python -I tools/juegos/mide_radar.py                          # huella, contraste y siluetas de las cápsulas
python -I tools/juegos/mide_piloto.py                         # huella, contraste y detalles de las señales y la nave
python -I tools/juegos/mide_calculo.py                        # tamaño del reactor, núcleo claro y losas de las celdas
python -I tools/juegos/mide_disparate.py                      # antena, íconos y barras del medidor
python -I tools/juegos/mide_satelites.py                      # satélite, planeta oscuro, sobre y nube opaca
python -I tools/juegos/mide_rastro.py                         # luceros: huella, color de diseño, pares cercanos y contraste
python -I tools/juegos/mide_stroop.py                         # tintas: ΔE, daltonismo, nombres y fondo de la tarjeta
python -I tools/juegos/mide_punta.py                          # ficha, casilla, perla y metales de los luceros
python -I tools/juegos/mide_freno.py                          # huella, contraste y flecha del botón, cohetes, plataforma y ALTO
python -I tools/juegos/mide_parejas.py                        # objetos (silueta, tinta, gemelos, pares parecidos), luces y la presionada
python -I tools/juegos/mide_meteoros.py                       # roca nine-slice, presionada, color teñido, palabra sobre la placa y estrellas
python -I tools/juegos/hoja_parejas.py                        # la hoja de los 16 objetos sobre su luz
python -I tools/juegos/mide_bodega.py                         # objetos, escotilla (hoja y presionada), robot, caja, esclusa y carro
python -I tools/juegos/hoja_bodega.py                         # la hoja de las piezas de Bodega
python -I tools/juegos/lamina.py out/lam.png '#0A2A3A' 3 1.5 out/radar/capsula_*.png     # para mirarlos
```

Opciones comunes de los scripts de Blender: `--solo <parte>` (renderiza una parte), `--samples N`, `--out DIR`, `--tilt` (30 por defecto: la inclinación del kit) y `--exp` (−0,42: la exposición del kit).

## Convenciones del arte

- **Cada pieza se modela en la unidad de diseño que usa el código de Unity** (por ejemplo, las unidades de `RadarSprites`: la cápsula mide ~2 de ancho en un lienzo de 2,36), 1 unidad de Blender = 1 unidad de diseño, y se renderiza en el MISMO lienzo y a la MISMA escala que el sprite procedural al que reemplaza: así el código de cada juego no cambia ninguna medida.
- Vista casi cenital (30° de inclinación: se ve un poco el canto de adelante); luz de arriba a la izquierda, sombra de contacto abajo a la derecha. Las posiciones de lo que cuelga de la raíz se corrigen por 1/cos(inclinación) (`raiz()`, `churro_c()`), así los círculos salen redondos.
- Material del kit (`mat_kit`): casi liso, canto blando, sin borde tinta. Un color OSCURO se renderiza con `material_kit(spec=0.05, sheen=0.0)`: con el brillo normal el sol sobre la cara plana le suma un gris de ~45/255 y la casilla «noche» salía lechosa (con poco especular queda a ±8/255 del color de diseño).
- NO usar `bultos` (subdivisión + desplazamiento) en caras grandes y planas: la subdivisión del polígono deja costuras radiales visibles. Sí en piezas irregulares (la roca).
- Una forma que se reconoce por su silueta conserva la huella de hoy (`mide_radar.py`: razón de áreas 0,97-1,02, IoU 0,95-0,98) y contrasta ≥ 3:1 con el fondo REAL del juego (en Rescate, el vidrio del radar: `RadarPlasticTests`).
- Lo que el código tiñe con `Image.color` (anillos, íconos de modo…) se entrega en blanco o gris claro; lo que tiene color propio y no se tiñe (las cápsulas) lleva su color de diseño.

## Unity

- `Games/Shared/PlasticArt.cs`: el cargador. `PlasticArt.Get("radar", "capsula_gota")` entrega el sprite del atlas (o null); `GetOr(grupo, nombre, fallback)` cae al procedural. `PlasticArt.Disabled = true` lo apaga (pruebas). Cada grupo es UN atlas (`Resources/Plasticina/<grupo>/atlas.png`) y su `manifest.json` (rectángulo, pivote, nine-slice y márgenes de cada sprite, y las medidas que dejó Blender).
- `Games/Shared/PlasticSlab.cs`: las losas del kit como nine-slice de Unity. `PlasticSlab.TryStyle(image, SlabSize, SlabTone)` pone la losa en lugar del fondo de siempre (que queda invisible, solo para los toques) o devuelve null si no hay plasticina: el que llama aplica entonces su estilo de siempre. `PlasticPress` da la presión (la losa presionada y el texto que baja). Tamaños: `Chip` (14 dp), `Button` (22 dp), `Panel` (30 dp). Tonos: `Sun, Cream, Lime, Sky, Coral, Lilac` (las del kit) y `Night, Hit, Miss, Dim` (las casillas oscuras de respuesta).
- `Bootstrap/Editor/PlasticinaImporter.cs`: impone en cada importación los ajustes de textura del atlas (sin compresión, sin mipmaps, sin reescalar), por si un `.meta` se rompe.
- Pruebas: `PlasticArtTests` (atlas, tope de peso, nine-slice, contraste del texto, caída al procedural), `RadarPlasticTests` (huella, contraste, siluetas, vidrio), `PilotPlasticTests` (huella, contraste, detalles y color, nave), `BrakePlasticTests` (huella, flecha, ALTO, cohetes, que el cohete quepa entre las torres), `CalculoPlasticTests` (tamaño del reactor, núcleo claro y teñible, losas de las celdas, interfaz con y sin atlas), `DisparatePlasticTests` (antena y sus puntos, íconos, barras del medidor, interfaz con y sin atlas), `SatellitePlasticTests` (huella, un solo satélite sin nada vertical, planeta oscuro, nube opaca), `RastroPlasticTests` (huella, luceros idénticos y redondos, color de diseño y pares cercanos, contraste), `StroopPlasticTests` (color de cada tinta, separación, nombres, fondo de la tarjeta, losas, íconos), `PuntaPlasticTests` (tamaño del cuerpo, letra sobre cada estado, los cuatro metales, interfaz con y sin atlas) e `IntrusaPlasticTests` (las losas y la interfaz con y sin atlas).

## Peso (lo que suma al APK)

El atlas va SIN compresión de textura (la plasticina tiene degradés suaves que ensucia la compresión): Unity guarda el RGBA32 y el APK lo comprime con deflate. `empaqueta.py` estima ese peso (`deflate_rgba32` del manifiesto) y también lo que pesaría con ASTC 6×6. Tope: ~1,5 MB por juego (`PlasticArtTests.EachGroupWeighsUnderItsBudget`); lo común (`comun`) se paga una sola vez para los 17 juegos. Medidas del 14-oct-2026:

| Grupo | Sprites | Atlas | PNG | En el APK (sin comprimir) | Con ASTC 6×6 |
|---|---:|---|---:|---:|---:|
| `radar` (Rescate relámpago) | 16 | 1024×1776 | 790 KB | 1,11 MB | 0,77 MB |
| `piloto` (Piloto Estelar) | 46 | 1664×884 | 912 KB | 1,21 MB | 0,64 MB |
| `freno` (Freno de Emergencia) | 9 | 1408×664 | 473 KB | 0,66 MB | 0,41 MB |
| `calculo` (Carga exacta: aro y núcleo del reactor) | 2 | 1152×556 | 242 KB | 0,32 MB | 0,27 MB |
| `disparate` (¿Verdad o disparate?: antena, íconos, barras del medidor) | 5 | 512×574 | 125 KB | 0,16 MB | 0,13 MB |
| `satelites` (Satélites: satélite, planeta, sobre, nube) | 5 | 1024×712 | 204 KB | 0,25 MB | 0,32 MB |
| `rastro` (Rastro de luz: 9 luceros + 9 presionados) | 18 | 1792×392 | 444 KB | 0,83 MB | 0,31 MB |
| `stroop` (Tinta o Palabra: losas de color exacto, gota y libro) | 17 | 1536×708 | 547 KB | 0,77 MB | 0,46 MB |
| `punta` (En la punta de la lengua: ficha, casilla, perla) | 4 | 640×196 | 75 KB | 0,10 MB | 0,06 MB |
| `meteoros` (Lluvia de meteoros: rocas nine-slice, presionadas, brasa, grietas, estrellas) | 18 | 640×1956 | 386 KB | 0,52 MB | 0,55 MB |
| `parejas` (Constelaciones: 12 objetos + 4 gemelos, luz dormida, presionada y abierta) | 19 | 1152×940 | 431 KB | 0,55 MB | 0,46 MB |
| `bodega` (Bodega de carga: 12 objetos, escotilla, robot, caja, esclusa, carro) | 30 | 1024×1384 | 684 KB | 0,89 MB | 0,60 MB |
| `comun` (losas, marcas, anillo) | 29 | 896×2472 | 849 KB | 1,11 MB | 0,97 MB |
| `nubi` (la Nubi 3D de los tutoriales) | 1 | 768×552 | 196 KB | 0,24 MB | 0,18 MB |

Si algún juego se pasa del tope: bajar el ancho final de sus imágenes en `empaqueta.py` (`ANCHOS_<JUEGO>`, la maestra de Blender no se toca) o comprimir su atlas (ASTC 6×6 en el `.meta`/`PlasticinaImporter`).
