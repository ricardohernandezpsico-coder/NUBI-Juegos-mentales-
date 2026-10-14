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
| `piloto.py` | Piloto Estelar: las 15 señales (5 formas × punto, anillo y franja, con el detalle de churro de tinta incrustado) y la nave. Las maestras salen a 384 px; `empaqueta.py` saca de la misma maestra la versión de 192 y la chica de 96. |
| `cohete.py` | El cohete de plasticina que comparten Piloto (la nave) y Freno (los de cada carril, con el acento coral, cielo o lima). Solo se importa desde scripts de Blender. |
| `freno.py` | Freno de Emergencia: el botón de lanzamiento (apagado y encendido), los 3 cohetes, la plataforma y la señal de ALTO, cada uno en el mismo lienzo que el sprite procedural al que reemplaza. |
| `comun.py` | Lo común que NO sale del kit: las casillas «noche» (normal, presionada, acierto, error, apagada), las marcas ✓ y ✗ y el anillo de la cuenta regresiva. También re-renderiza una losa crema de CONTROL para comprobar que este pipeline da el mismo kit que la app. |
| `empaqueta.py` | De los PNG de Blender (y de los WebP del kit de la app y la Nubi 3D) al atlas de cada grupo: reduce con Lanczos y alfa premultiplicado (los renders son la «maestra», a más resolución), limpia la bruma del suelo de sombra, empaca, escribe el manifiesto y los `.meta` (GUID fijo por ruta; un `.meta` que ya existe no se toca: Unity lo reescribe al importar y esa versión es la que va al repo) y **mide cuánto pesa**. |
| `mide_radar.py` | Las cápsulas contra lo que reemplazan (huella) y contra el vidrio del radar (contraste WCAG), y qué tanto se parecen de a pares (escala de grises). |
| `mide_piloto.py` | Las 15 señales y la nave: huella, contraste contra el cielo, tinta contra el cuerpo, qué tanto se distinguen las «parecidas» y lo que miden los detalles a los tamaños reales. |
| `mide_freno.py` | Botón, cohetes, plataforma y ALTO: huella contra el procedural, contraste contra el suelo de la luna y el cielo, flecha contra botón. |
| `huella.py` | Lo que comparten las dos mediciones (la silueta de HOY y el contraste WCAG; solo numpy). |
| `lamina.py` | Una lámina de contacto para MIRAR los renders sobre un fondo liso. |

## Cómo se corre

```
BLENDER=/c/Users/RURAL7/Tools/blender-4.5.14/blender-4.5.14-windows-x64/blender.exe
$BLENDER -b -P tools/juegos/radar.py -- --samples 112 --out tools/juegos/out/radar        # ~10 min con Cycles por CPU (Ryzen 5 5625U); el aro del radar tarda ~4 de eso
$BLENDER -b -P tools/juegos/piloto.py -- --samples 112 --out tools/juegos/out/piloto      # ~10 min (15 señales a 384 px y la nave)
$BLENDER -b -P tools/juegos/freno.py -- --samples 112 --out tools/juegos/out/freno        # ~6 min (botones, cohetes, plataforma, alto)
$BLENDER -b -P tools/juegos/comun.py -- --samples 112 --out tools/juegos/out/comun        # ~5 min
python -I tools/juegos/empaqueta.py radar piloto freno comun nubi   # atlas + manifiestos + pesos (segundos)
python -I tools/juegos/mide_radar.py                          # huella, contraste y siluetas de las cápsulas
python -I tools/juegos/mide_piloto.py                         # huella, contraste y detalles de las señales y la nave
python -I tools/juegos/mide_freno.py                          # huella, contraste y flecha del botón, cohetes, plataforma y ALTO
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
- Pruebas: `PlasticArtTests` (atlas, tope de peso, nine-slice, contraste del texto, caída al procedural), `RadarPlasticTests` (huella, contraste, siluetas, vidrio), `PilotPlasticTests` (huella, contraste, detalles y color, nave), `BrakePlasticTests` (huella, flecha, ALTO, cohetes, que el cohete quepa entre las torres).

## Peso (lo que suma al APK)

El atlas va SIN compresión de textura (la plasticina tiene degradés suaves que ensucia la compresión): Unity guarda el RGBA32 y el APK lo comprime con deflate. `empaqueta.py` estima ese peso (`deflate_rgba32` del manifiesto) y también lo que pesaría con ASTC 6×6. Tope: ~1,5 MB por juego (`PlasticArtTests.EachGroupWeighsUnderItsBudget`); lo común (`comun`) se paga una sola vez para los 17 juegos. Medidas del 14-oct-2026:

| Grupo | Sprites | Atlas | PNG | En el APK (sin comprimir) | Con ASTC 6×6 |
|---|---:|---|---:|---:|---:|
| `radar` (Rescate relámpago) | 16 | 1024×1776 | 790 KB | 1,11 MB | 0,77 MB |
| `piloto` (Piloto Estelar) | 31 | 1664×492 | 472 KB | 0,58 MB | 0,35 MB |
| `comun` (losas, marcas, anillo) | 28 | 896×2292 | 815 KB | 1,05 MB | 0,87 MB |
| `nubi` (la Nubi 3D de los tutoriales) | 1 | 768×552 | 196 KB | 0,24 MB | 0,18 MB |

Si algún juego se pasa del tope: bajar el ancho final de sus imágenes en `empaqueta.py` (`ANCHOS_<JUEGO>`, la maestra de Blender no se toca) o comprimir su atlas (ASTC 6×6 en el `.meta`/`PlasticinaImporter`).
