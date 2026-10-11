#!/bin/bash
# Renderiza la isla de Hoy (Blender, Cycles) y prepara lo que entra a la app. NO se corre en las verificaciones: es un paso de arte, a mano.
#   BLENDER=/ruta/a/blender bash tools/isla/render.sh [lámina]     (lámina A = lila, B = azul marino, la que usa la app; por defecto B)
# Tarda ~10 min con Cycles solo por CPU (Ryzen 5 5625U): isla completa y capas ~90 s, cada mirada de Nubi ~15 s, el tablero ~3 min.
# Salida: tools/isla/out/ (git-ignorada). Al final se copian a mano (o con las líneas de abajo) a app/src/main/res/.
set -e
cd "$(dirname "$0")"
BLENDER="${BLENDER:-blender}"
LAMINA="${1:-B}"
OUT=out
mkdir -p "$OUT"
run() { "$BLENDER" -b -P isla.py -- --engine CYCLES --samples 112 --width 1400 "$@" 2>&1 | grep "\[isla\]\|Traceback\|Error" || true; }

# 1. Capas (isla completa, base, edificios, Nubi) y isla.json con las coordenadas en píxeles del render.
run --layers --json --out "$OUT"
# 2. Las miradas de Nubi hacia cada edificio (fase 3: todavía no las usa la app) y su perfil.
for m in frente observatorio antena hangar biblioteca; do run --solo-nubi --mirada "$m" --out "$OUT"; done
cp "$OUT/capa-nubi-frente.png" "$OUT/capa-nubi.png"
# 3. El tablero (fondo de la app).
run --tablero --lamina "$LAMINA" --out "$OUT/tablero"

# 4. Recortes, WebP a 2× e isla.json normalizado (0-1) → tools/isla/out/app/ y tools/isla/out/fondo/
python -I componer.py "$OUT" "$OUT/app"
python -I tablero.py "$OUT/tablero" "$OUT/fondo"

cat <<MSG

Listo. Para llevarlo a la app (a drawable-nodpi: NO en carpetas de densidad, ahí Android lo reescala y se ve borroso):
  cp $OUT/app/isla_base.webp $OUT/app/isla_edificios.webp ../../app/src/main/res/drawable-nodpi/
  cp $OUT/app/nubi_hola.webp ../../app/src/main/res/drawable-nodpi/isla_nubi_hola.webp      # el prefijo isla_ evita chocar con el nubi_hola de la app
  cp $OUT/app/isla.json ../../app/src/main/res/raw/isla.json
  cp $OUT/fondo/fondo_tablero.webp ../../app/src/main/res/drawable-nodpi/
Después: correr IslaJsonTest y BoardBackgroundContrastTest (miden que las imágenes y el JSON sigan siendo del mismo render y que el saludo se lea).
MSG
