# La isla de plasticina (tools/isla)

Las imágenes de la isla de Hoy no se dibujan a mano: las genera un script de Blender (`isla.py`, Blender 4.5, solo herramientas de Blender, sin texturas externas) y de ahí salen las capas que usa la app. Es un paso de ARTE, a mano: ninguna verificación lo corre y **la app no depende de Blender** (lo que entra al repo son los WebP y `isla.json` ya hechos).

## Qué hay

| Archivo | Para qué |
|---|---|
| `isla.py` | El script `bpy`: isla, edificios (Observatorio, Antena, Biblioteca, Hangar), Nubi, farolas y el tablero del fondo, todo por código. Escribe PNG con fondo transparente, `isla.json` (posiciones en píxeles) y `tiempos.txt`. |
| `componer.py` | Recorta las capas a un encuadre común, las guarda en WebP a 2× y normaliza `isla.json` a coordenadas 0-1. |
| `tablero.py` | Convierte los renders del tablero en las DOS capas del fondo (`fondo_lamina.webp` y `fondo_luceros.webp`), mide cuánto se parece su apilado al B completo y el contraste del saludo sobre la lámina. |
| `render.sh` | Corre todo en orden (Cycles, 112 muestras, ~20 min por CPU). Al final imprime las líneas para copiar a la app. |
| `out/` | Salida (git-ignorada). |

```
BLENDER=/ruta/a/blender.exe bash tools/isla/render.sh        # lámina B (azul marino)
```

El paso del tablero (lámina, luceros y `tablero.py`) se corrió de verdad en la Tarea 83. Los pasos de la isla y de Nubi (1 y 2 de `render.sh`) son los de la cadena con la que se hicieron las imágenes vigentes (ronda 26b, 11-oct-2026) pero no se volvieron a correr desde el repo.

## El fondo en dos capas (Tarea 83)

El fondo B (tablero azul marino con luceros de plasticina) son DOS imágenes apiladas: `fondo_lamina` (la lámina con su nebulosa y su textura, sin luceros) y `fondo_luceros` (las bolitas con su sombra de contacto, transparente, encima). Desde la Tarea 87 van FIJAS detrás de Hoy, Juegos y Avance (~80 luceros visibles; antes solo se veían en Hoy, con 17). En la app: `ui/components/BoardBackground.kt` (sin fundido por pestaña).

Opciones de `isla.py` para el tablero (se usan siempre con `--tablero --lamina B`):

| Opción | Qué renderiza | Archivo |
|---|---|---|
| (ninguna) | el B entero (lámina y luceros) SIN los luceros de la zona libre de arriba | `fondo_tablero.png` |
| `--sin-luceros` | solo la lámina, opaca | `fondo_lamina.png` |
| `--solo-luceros` | solo los luceros y su sombra, transparente (la lámina es «shadow catcher» de Cycles: no se ve, solo recibe la sombra y el rebote) | `fondo_luceros.png` |
| `--b-aprobado` | no quita los luceros de la zona libre de arriba (la secuencia llega a `N_LUCEROS`; los primeros 58 son los del B aprobado de la ronda 26b) | (se suma a cualquiera de las anteriores) |

Los luceros salen de la MISMA semilla de colocación (`random.Random(5)`) que el B aprobado: los primeros 58 quedan donde estaban y la secuencia sigue hasta `N_LUCEROS` = 95 colocados (~80 visibles: los que caen en la zona libre de arriba o en la banda de la barra se calculan y NO se crean). La única zona libre (`ZONAS_HOY` en `isla.py`, x 0-100 %, y 3-28 %) es el encabezado: el saludo de Hoy y los títulos de Juegos y Avance, que siempre caen en el mismo lugar.
El texto de Hoy medido en las pruebas llega a y 22,5 % (encabezado); «Tu avance» está en y 5-8 % y «¿Qué jugamos hoy?» con su sugerencia en y 10-20 %. Bajo botones, piedritas y tarjetas no se excluye nada (son opacas); los demás textos claros directo sobre el fondo se leen por su contorno navy (`OnBoardText`).
Si se agrega un título grande directo sobre el fondo que caiga siempre en el mismo lugar, hay que ampliar la zona y volver a correr `render.sh`; `HoyIslaScreenshotTest` falla si el saludo, la fecha o la racha de Hoy caen sobre un lucero (e imprime en qué fracción de la imagen cae cada texto: líneas «ZONAS …»), y `BoardBackgroundContrastTest` cuenta los luceros (70-90) y que ninguno se pegue a otro.

**Apilados dan el B.** `tablero.py` mide el apilado (los WebP que entran a la app) contra el B completo: media 0,85/255, p99 3, y solo el 0,23 % de los píxeles difieren en más de 8 (máximo 27); es el mismo nivel de ruido que hay entre dos renders del MISMO B (media 0,85, p99 3). Detalle: el «shadow catcher» solo da bien la máscara (dónde hay luceros y sombras); su sombra sale gris por el canal alfa y la real es más cálida (rebote del lucero) y menos azul, así que dentro de esa máscara el color y la opacidad de la capa se calculan para que `lámina · (1-α) + color · α` dé el píxel del B completo.

## Cómo entra a la app

- `isla_base.webp`, `isla_edificios.webp`, `isla_nubi_hola.webp`, `fondo_lamina.webp` y `fondo_luceros.webp` van en `app/src/main/res/drawable-nodpi/`, a 2×. **No** en carpetas de densidad: Android las reescala al decodificarlas (un teléfono de 420 dpi las achicaba a 0,66 y se veían borrosas). `nubi_hola` se llama `isla_nubi_hola` porque ya existe un `nubi_hola` en la app.
- `isla.json` va en `app/src/main/res/raw/`. Sus coordenadas son 0-1 sobre el recorte común (930 × 1042 px); `data/IslaLayout.kt` lo lee y `ui/components/Island.kt` dibuja sobre él la sombra de Nubi, el halo de la torre y los de las farolas.
- `IslaJsonTest` comprueba que las imágenes y el JSON son del mismo render (mismo tamaño, Nubi en su caja) y que los edificios, las farolas y la sombra no se pisan. `BoardBackgroundContrastTest` comprueba que el saludo se lea sobre la lámina (≥ 4,5:1), que la lámina no tenga luceros y que la capa de luceros no tenga ninguno en las zonas libres de Hoy.
- Si se cambia el encuadre o los edificios, hay que volver a correr `componer.py` y copiar LAS TRES cosas juntas (imágenes y JSON): mezclarlas de renders distintos desalinea los toques y los halos.

## Qué falta (fases siguientes de `docs/diseno-isla.md`)

Las miradas de Nubi hacia cada edificio (`nubi_mira_*`, ya salen de `render.sh` pero la app aún no las usa), el movimiento (flotación y sombra), la barra de pestañas en 3D y la cortina de arranque.
