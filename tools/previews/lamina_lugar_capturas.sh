#!/usr/bin/env bash
# Regenera las capturas de la hoja `docs/previews/lamina-lugar.png` (Tarea 147): la lámina del lugar del Observatorio (4 juegos) y de la Antena (5), en 393 x 873 y 360 x 780, con letra 1,0 y 1,3.
# La misma prueba (`LaminaLugarCapturesTest`, solo corre al grabar capturas) se graba con el código de HOY (un `git worktree` del commit anterior al rediseño, 630fc96) y con el de ahora;
# deja los PNG y `medidas.txt` (nombre | dónde empieza la lámina en dp | cuánto mide | cuánto hay que deslizar) en `docs/previews/lamina-lugar/{hoy,propuesta}/`.
# Uso (Git Bash, en la carpeta del repo):   bash tools/previews/lamina_lugar_capturas.sh ; python -I tools/previews/lamina_lugar.py
set -e
export JAVA_HOME="${JAVA_HOME:-C:\Users\RURAL7\AppData\Local\Temurin21\jdk-21.0.12.1+1}"
export PATH="$JAVA_HOME/bin:$PATH"
REPO="$(cd "$(dirname "$0")/../.." && pwd)"
BEFORE=630fc96
W="${TMPDIR:-${TEMP:-/tmp}}/nubi-hoy"
OUT="$REPO/docs/previews/lamina-lugar"

# --- PROPUESTA: el código de ahora
cd "$REPO"
rm -rf app/src/test/screenshots/lamina-lugar
./gradlew.bat :app:recordRoborazziDebug --tests "com.example.LaminaLugarCapturesTest"
rm -rf "$OUT/propuesta"; mkdir -p "$OUT/propuesta"; cp app/src/test/screenshots/lamina-lugar/* "$OUT/propuesta/"   # (la hoja los usa a 300 px: se pueden bajar a 600 px de ancho con PIL para que pesen menos en git)

# --- HOY: el commit anterior, en un worktree solo con `app` y `gradle` (más lo que pide Gradle: local.properties, el keystore, el export de Unity y docs/DESIGN.md)
rm -rf "$W"; git worktree prune
git worktree add --no-checkout --detach "$W" "$BEFORE"
( cd "$W" && git sparse-checkout init --cone && git sparse-checkout set app gradle && git checkout "$BEFORE" )
cp local.properties "$W/"; cp app/debug.keystore "$W/app/" 2>/dev/null || true
mkdir -p "$W/unity" "$W/docs"; git show "$BEFORE:docs/DESIGN.md" > "$W/docs/DESIGN.md"
cmd //c mklink //J "$(cygpath -w "$W/unity/AndroidExport")" "$(cygpath -w "$REPO/unity/AndroidExport")"
cp app/src/test/java/com/example/LaminaLugarCapturesTest.kt "$W/app/src/test/java/com/example/"
( cd "$W" && ./gradlew.bat :app:recordRoborazziDebug --tests "com.example.LaminaLugarCapturesTest" )
rm -rf "$OUT/hoy"; mkdir -p "$OUT/hoy"; cp "$W"/app/src/test/screenshots/lamina-lugar/* "$OUT/hoy/"

# --- limpiar el worktree (primero la unión de Unity: borrarla con `rm -rf` borraría el export de verdad)
cmd //c rmdir "$(cygpath -w "$W/unity/AndroidExport")"
git worktree remove --force "$W"
