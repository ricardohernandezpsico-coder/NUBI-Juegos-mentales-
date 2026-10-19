#!/bin/bash
# Renderiza la isla de Hoy (Blender, Cycles) y prepara lo que entra a la app. NO se corre en las verificaciones: es un paso de arte, a mano.
#   BLENDER=/ruta/a/blender bash tools/isla/render.sh [lámina]     (lámina A = lila, B = azul marino, E = «Espacio profundo», la que usa la app desde la Tarea 122; por defecto E)
# Tarda ~20 min con Cycles solo por CPU (Ryzen 5 5625U): isla completa y capas ~90 s, cada mirada de Nubi ~15 s, el tablero completo y la lámina ~2 min cada uno y los luceros ~4 min.
# Salida: tools/isla/out/ (git-ignorada). Al final se copian a mano (o con las líneas de abajo) a app/src/main/res/.
set -e
cd "$(dirname "$0")"
BLENDER="${BLENDER:-blender}"
LAMINA="${1:-E}"
OUT=out
mkdir -p "$OUT"
run() { "$BLENDER" -b -P isla.py -- --engine CYCLES --samples 112 --width 1400 "$@" 2>&1 | grep "\[isla\]\|Traceback\|Error" || true; }

# 1. Capas (isla completa, base, edificios, Nubi) y isla.json con las coordenadas en píxeles del render.
run --layers --json --out "$OUT"
# 1b. (Tarea 134) las DIEZ piezas de la isla por medallas (spec-adopcion-A1-A7 §A1): cada una en su capa capa-deco-<id>.png (isla de «shadow catcher», edificios y piezas de abajo de «holdout»; solo se renderiza el rectángulo de la pieza),
#     la isla completa con las diez (isla-completa-deco.png) y los primeros planos de la lámina (lamina-deco-<id>.png). ~12 min. NO entran a la app todavía (tarea T-D2): ver tools/isla/README.md.
run --deco --deco-completa --deco-lamina --out "$OUT"
# 2. Las miradas de Nubi hacia cada edificio (fase 3: todavía no las usa la app) y su perfil.
for m in frente observatorio antena hangar biblioteca; do run --solo-nubi --mirada "$m" --out "$OUT"; done
cp "$OUT/capa-nubi-frente.png" "$OUT/capa-nubi.png"
# 3. El fondo en DOS capas (Tarea 83). La lámina E («Espacio profundo», Tarea 122) es la que usa la app; la B (azul marino, 11-oct) y la A (lila) quedaron atrás.
#    - completo: el B entero con todos sus luceros (--b-aprobado: no quita los de la zona libre de arriba); sirve de referencia para calcular el color de la capa de luceros.
#    - lámina: sola, sin luceros.   - luceros: solo los luceros y su sombra, con la lámina como «shadow catcher» (transparente), SIN los de la zona libre de arriba (ZONAS_HOY en isla.py: el saludo de Hoy y los títulos de Juegos y Avance); son ~80 luceros (N_LUCEROS).
run --tablero --lamina "$LAMINA" --b-aprobado --out "$OUT/tablero-completo"
run --tablero --lamina "$LAMINA" --sin-luceros --out "$OUT/tablero-lamina"
run --tablero --lamina "$LAMINA" --solo-luceros --out "$OUT/tablero-luceros"

# 3b. (Tareas 89 y 90) el kit de plasticina, el estilo de toda la app (~4,5 min): `run --kit --out "$OUT/kit"` y `python -I kit.py "$OUT/kit" "$OUT/kit/app"` (ver el README).
# 3c. (Tarea 92) la Nubi de frente para las pantallas y el ícono (~6,5 min): `run --solo-nubi --mirada frente --width 3000 --out "$OUT/nubi3d"` y `python -I icono.py "$OUT/nubi3d/capa-nubi-frente.png" ../../app/src/main/res` (ver el README).
# 4. Recortes, WebP a 2× e isla.json normalizado (0-1) → tools/isla/out/app/; las dos capas del fondo → tools/isla/out/fondo/ (y la medida de cuánto se parece el apilado al B completo)
python -I componer.py "$OUT" "$OUT/app"
python -I tablero.py "$OUT/tablero-lamina" "$OUT/tablero-luceros" "$OUT/tablero-completo" "$OUT/fondo"

cat <<MSG

Listo. Para llevarlo a la app (a drawable-nodpi: NO en carpetas de densidad, ahí Android lo reescala y se ve borroso):
  cp $OUT/app/isla_base.webp $OUT/app/isla_edificios.webp ../../app/src/main/res/drawable-nodpi/
  cp $OUT/app/nubi_hola.webp ../../app/src/main/res/drawable-nodpi/isla_nubi_hola.webp      # el prefijo isla_ evita chocar con el nubi_hola de la app
  cp $OUT/app/isla.json ../../app/src/main/res/raw/isla.json
  cp $OUT/fondo/fondo_lamina.webp $OUT/fondo/fondo_luceros.webp ../../app/src/main/res/drawable-nodpi/
(Tarea 134) Las piezas de la isla salen en $OUT/app/isla_deco_<id>.webp (10 capas, ≤ 200 KB en total) y en `decoraciones` de isla.json; la lámina y la copia al repo: `python -I ../previews/isla_piezas.py $OUT` (docs/previews/isla-piezas.png). No se copian a la app hasta que Ricardo apruebe la lámina.
Después: correr IslaJsonTest, BoardBackgroundContrastTest y HoyIslaScreenshotTest (miden que las imágenes y el JSON sigan siendo del mismo render, que el saludo se lea y que ningún texto de Hoy caiga sobre un lucero).
MSG
