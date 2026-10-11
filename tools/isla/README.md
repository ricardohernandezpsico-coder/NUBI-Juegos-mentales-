# La isla de plasticina (tools/isla)

Las imágenes de la isla de Hoy no se dibujan a mano: las genera un script de Blender (`isla.py`, Blender 4.5, solo herramientas de Blender, sin texturas externas) y de ahí salen las capas que usa la app. Es un paso de ARTE, a mano: ninguna verificación lo corre y **la app no depende de Blender** (lo que entra al repo son los WebP y `isla.json` ya hechos).

## Qué hay

| Archivo | Para qué |
|---|---|
| `isla.py` | El script `bpy`: isla, edificios (Observatorio, Antena, Biblioteca, Hangar), Nubi, farolas y el tablero del fondo, todo por código. Escribe PNG con fondo transparente, `isla.json` (posiciones en píxeles) y `tiempos.txt`. |
| `componer.py` | Recorta las capas a un encuadre común, las guarda en WebP a 2× y normaliza `isla.json` a coordenadas 0-1. |
| `tablero.py` | Convierte el render del tablero en `fondo_tablero.webp` y mide el contraste del saludo sobre él. |
| `render.sh` | Corre todo en orden (Cycles, 112 muestras, ~10 min por CPU). Al final imprime las líneas para copiar a la app. |
| `out/` | Salida (git-ignorada). |

```
BLENDER=/ruta/a/blender.exe bash tools/isla/render.sh        # lámina B (azul marino)
```

No se probó el `render.sh` completo al subirlo (Tarea 82 no corrió Blender): los pasos y las opciones son los de la cadena con la que se hicieron las imágenes vigentes (ronda 26b, 11-oct-2026).

## Cómo entra a la app

- `isla_base.webp`, `isla_edificios.webp`, `isla_nubi_hola.webp` y `fondo_tablero.webp` van en `app/src/main/res/drawable-nodpi/`, a 2×. **No** en carpetas de densidad: Android las reescala al decodificarlas (un teléfono de 420 dpi las achicaba a 0,66 y se veían borrosas). `nubi_hola` se llama `isla_nubi_hola` porque ya existe un `nubi_hola` en la app.
- `isla.json` va en `app/src/main/res/raw/`. Sus coordenadas son 0-1 sobre el recorte común (930 × 1042 px); `data/IslaLayout.kt` lo lee y `ui/components/Island.kt` dibuja sobre él la sombra de Nubi, el halo de la torre y los de las farolas.
- `IslaJsonTest` comprueba que las imágenes y el JSON son del mismo render (mismo tamaño, Nubi en su caja) y que los edificios, las farolas y la sombra no se pisan. `BoardBackgroundContrastTest` comprueba que el saludo se lea sobre el fondo (≥ 4,5:1) y que su zona no tenga luceros.
- Si se cambia el encuadre o los edificios, hay que volver a correr `componer.py` y copiar LAS TRES cosas juntas (imágenes y JSON): mezclarlas de renders distintos desalinea los toques y los halos.

## Qué falta (fases siguientes de `docs/diseno-isla.md`)

Las miradas de Nubi hacia cada edificio (`nubi_mira_*`, ya salen de `render.sh` pero la app aún no las usa), el movimiento (flotación y sombra), la barra de pestañas en 3D y la cortina de arranque.
