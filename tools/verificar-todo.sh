#!/bin/bash
# Verificacion de Nubi en el PC de Ricardo (Windows + Git Bash). Desde cualquier carpeta del repo:
#
#   bash tools/verificar-todo.sh [--instalar] [--juegos Meteoros,Intrusa] [--filtro-tests <regex>] [--solo-app]
#
# SIN flags es la verificacion COMPLETA (la de antes): escena piloto -> pruebas EditMode -> arranque (smoke) de los 23 juegos ->
# REEXPORTAR la libreria Android -> Gradle (pruebas Kotlin + assembleDebug). Esta es la que hay que correr antes de un merge a main
# y cuando se toca codigo compartido (Games/Shared, Bridge, GameResultScreen comun, Room, DDA comun).
#
# Mientras se itera sobre UN juego, lo habitual es:   --juegos <Juego> --filtro-tests <Juego>
#   --juegos A,B       el smoke corre SOLO esos juegos (nombres = sufijo de Run*: Meteoros, Disparate, Cosecha, Intrusa, ...).
#                      Las demas etapas se mantienen (si el cambio es solo de Kotlin, usa --solo-app).
#   --filtro-tests R   EditMode corre solo las pruebas cuyo nombre case con la regex R (se pasa a -testFilter de Unity).
#   --solo-app         salta TODAS las etapas de Unity (escena, EditMode, smoke y export); corre Gradle e instala si va --instalar.
#                      Avisa si hay cambios bajo unity/ posteriores al ultimo export, para no empaquetar una exportacion vieja sin saberlo.
#   --sin-animaciones  smoke de los juegos pedidos (o los 23) con "quitar animaciones" ACTIVO (NUBI_REDUCE_MOTION=1: la config del juego lleva
#                      reduce_motion=true). Corre solo escena piloto + smoke; no reexporta ni toca Gradle ni instala (el APK es el mismo).
#   --instalar         al final instala el APK en el telefono (DEVICE, por defecto el de Ricardo).
#
# El smoke corre en UN solo proceso de Unity (HeadlessPlaymodeSmokeTest.RunList). Si encadenarlos falla en algun juego, ese juego se
# repite aislado (como antes) antes de dar FALLO, para no confundir un artefacto del encadenado con un error real.
#
# Salida: una linea por etapa (OK/FALLO + conteo + segundos). Los logs completos van a unity/test-results/ y solo se leen si algo falla
# (el script imprime las ultimas lineas del log que fallo).
#
# Requisitos ya instalados en ese PC (ver CLAUDE.md, "Toolchain Android"):
#   Unity 6000.0.84f1 con soporte Android, JDK 21 Temurin, Android SDK, adb en el PATH del SDK.
# Solo funciona en el PC de Ricardo: una sesion en la nube no tiene Unity ni el SDK.
set -u

INSTALAR=0; JUEGOS=""; FILTRO=""; SOLO_APP=0; SIN_ANIM=0
while [ $# -gt 0 ]; do
  case "$1" in
    --instalar) INSTALAR=1 ;;
    --juegos) JUEGOS="${2:-}"; shift ;;
    --filtro-tests) FILTRO="${2:-}"; shift ;;
    --solo-app) SOLO_APP=1 ;;
    --sin-animaciones) SIN_ANIM=1 ;;
    *) echo "Opcion desconocida: $1"; sed -n 2,3p "$0"; exit 2 ;;
  esac
  shift
done

  if [ "$SIN_ANIM" = 1 ]; then
  [ "$SOLO_APP" = 1 ] && { echo "--sin-animaciones no se combina con --solo-app (no hay nada de Unity que correr)"; exit 2; }
  export NUBI_REDUCE_MOTION=1   # lo lee HeadlessPlaymodeSmokeTest: la config de cada juego lleva reduce_motion=true
else
  unset NUBI_REDUCE_MOTION
fi

REPO="$(git rev-parse --show-toplevel)"
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.0.84f1/Editor/Unity.exe}"
PROJ="$(cygpath -w "$REPO/unity/NeuroVidaCore")"
RESULTS="$REPO/unity/test-results"
EXPORT_DIR="$REPO/unity/AndroidExport"
export JAVA_HOME="${JAVA_HOME:-/c/Users/RURAL7/AppData/Local/Temurin21/jdk-21.0.12.1+1}"
export PATH="$JAVA_HOME/bin:$PATH:/c/Users/RURAL7/AppData/Local/Android/Sdk/platform-tools"
DEVICE="${DEVICE:-ZY22LPRRWS}"   # telefono de Ricardo (adb devices)
SMOKE_LBL="3 Smoke"; [ "$SIN_ANIM" = 1 ] && SMOKE_LBL="3 Smoke (sin animaciones)"
mkdir -p "$RESULTS"
T0=$SECONDS

# Una linea por etapa: etapa "texto" segundos
linea() { printf '%-30s %s (%ss)\n' "$1" "$2" "$3"; }
fallo() { # etapa, texto, log
  linea "$1" "FALLO: $2" "$((SECONDS - T))"
  echo "--- ultimas lineas de $3 ---"; tail -n 15 "$3"
  echo "=== FALLO (total $((SECONDS - T0))s) ==="; exit 1
}

if [ "$SOLO_APP" = 0 ]; then
  T=$SECONDS
  "$UNITY" -batchmode -nographics -quit -projectPath "$PROJ" \
    -executeMethod NeuroVida.Bridge.EditorTools.CreatePilotTestScene.Create -logFile "$RESULTS/v-1-scene.log"
  [ $? -eq 0 ] || fallo "1 Escena piloto" "no se pudo recrear" "$RESULTS/v-1-scene.log"
  linea "1 Escena piloto" "OK" "$((SECONDS - T))"

  if [ "$SIN_ANIM" = 0 ]; then   # con --sin-animaciones solo se repite el smoke (EditMode, export y Gradle no cambian)
  T=$SECONDS
  ARGS=(-batchmode -nographics -projectPath "$PROJ" -runTests -testPlatform EditMode -testResults "$RESULTS/v-2-tests.xml" -logFile "$RESULTS/v-2-tests.log")
  [ -n "$FILTRO" ] && ARGS+=(-testFilter "$FILTRO")
  rm -f "$RESULTS/v-2-tests.xml"
  "$UNITY" "${ARGS[@]}" || true
  RES="$(grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' "$RESULTS/v-2-tests.xml" 2>/dev/null | head -1)"
  TOTAL="$(echo "$RES" | sed -n 's/.*total="\([0-9]*\)".*/\1/p')"; FALLIDAS="$(echo "$RES" | sed -n 's/.*failed="\([0-9]*\)".*/\1/p')"
  if [ -z "$RES" ] || [ "${FALLIDAS:-1}" != "0" ] || [ "${TOTAL:-0}" = "0" ]; then
    grep -o 'name="[^"]*" [^>]*result="Failed"' "$RESULTS/v-2-tests.xml" 2>/dev/null | head -5
    fallo "2 EditMode${FILTRO:+ (filtro)}" "${RES:-sin resultados}" "$RESULTS/v-2-tests.log"
  fi
  linea "2 EditMode${FILTRO:+ (filtro)}" "OK $TOTAL pruebas, 0 fallos" "$((SECONDS - T))"
fi

  # --- smoke: UN solo Unity para todos los juegos pedidos (o los 23)
  T=$SECONDS
  NUBI_SMOKE_GAMES="$JUEGOS" "$UNITY" -batchmode -nographics -projectPath "$PROJ" \
    -executeMethod NeuroVida.Bridge.EditorTools.HeadlessPlaymodeSmokeTest.RunList -logFile "$RESULTS/v-3-smoke.log"
  RC=$?
  NOK="$(grep -c '\[SmokeTest\] [A-Za-z]*: OK' "$RESULTS/v-3-smoke.log")"
  FALLADOS="$(grep -o '\[SmokeTest\] [A-Za-z]*: FALL' "$RESULTS/v-3-smoke.log" | sed 's/\[SmokeTest\] \([A-Za-z]*\): FALL/\1/')"
  if [ $RC -ne 0 ] || [ -n "$FALLADOS" ]; then
    # repetir aislados los que fallaron (o todos si el proceso encadenado murio sin dar detalle)
    [ -z "$FALLADOS" ] && FALLADOS="$(echo "${JUEGOS:-Run,Stroop,Calculo,Engranajes,Anagramas,Parejas,Piloto,Radar,Satelites,Freno,Aterrizaje,Acoplamiento,Bitacora,Rumbo,Correo,Meteoros,Disparate,Cosecha,Intrusa}" | tr ',' ' ')"
    MALOS=""
    for G in $FALLADOS; do
      M="Run$G"; [ "$G" = "Run" ] && M="Run"
      case "$G" in
        Tutorial*)
          # los tutoriales se repiten con la misma revision (las 3 formas de pantalla): si no, el reintento "sin revision" taparia un solape real
          NUBI_SMOKE_GAMES="$G" "$UNITY" -batchmode -nographics -projectPath "$PROJ" \
            -executeMethod NeuroVida.Bridge.EditorTools.HeadlessPlaymodeSmokeTest.RunList -logFile "$RESULTS/v-3-$G.log"
          grep -q 'RunList: .* OK' "$RESULTS/v-3-$G.log" || MALOS="$MALOS $G" ;;
        *)
          "$UNITY" -batchmode -nographics -projectPath "$PROJ" \
            -executeMethod NeuroVida.Bridge.EditorTools.HeadlessPlaymodeSmokeTest.$M -logFile "$RESULTS/v-3-$G.log"
          grep -q '\[SmokeTest\] OK' "$RESULTS/v-3-$G.log" || MALOS="$MALOS $G" ;;
      esac
    done
    if [ -n "$MALOS" ]; then
      G1="$(echo $MALOS | cut -d' ' -f1)"
      fallo "$SMOKE_LBL" "arranque fallido en:$MALOS" "$RESULTS/v-3-$G1.log"
    fi
    linea "$SMOKE_LBL" "OK (fallos del encadenado resueltos aislados: $(echo $FALLADOS | tr ' ' ','))" "$((SECONDS - T))"
  else
    linea "$SMOKE_LBL" "OK $NOK juegos" "$((SECONDS - T))"
  fi

  if [ "$SIN_ANIM" = 1 ]; then
    echo "=== TODO OK, sin animaciones (total $((SECONDS - T0))s) ==="; exit 0
  fi

  T=$SECONDS
  "$UNITY" -batchmode -nographics -quit -projectPath "$PROJ" \
    -executeMethod NeuroVida.Bridge.EditorTools.AndroidLibraryExport.Export -logFile "$RESULTS/v-4-export.log"
  grep -q "Resultado: Succeeded" "$RESULTS/v-4-export.log" || fallo "4 Export Android" "sin 'Succeeded'" "$RESULTS/v-4-export.log"
  touch "$EXPORT_DIR/.exportado"
  linea "4 Export Android" "OK $(grep -o 'tamaño: [0-9A-Za-z]*' "$RESULTS/v-4-export.log" | head -1)" "$((SECONDS - T))"
else
  # --solo-app: avisar si lo de Unity cambio desde el ultimo export
  if [ -d "$EXPORT_DIR" ]; then
    REF="$EXPORT_DIR/.exportado"
    [ -f "$REF" ] || REF="$(find "$EXPORT_DIR" -type f -printf '%T@ %p\n' 2>/dev/null | sort -n | tail -1 | cut -d' ' -f2-)"
    CAMBIOS="$(find "$REPO/unity/NeuroVidaCore/Assets" "$REPO/unity/NeuroVidaCore/Packages" -type f -newer "$REF" \
      ! -name 'SecuenciaPilotoTest.unity' 2>/dev/null | head -50)"
    if [ -n "$CAMBIOS" ]; then
      echo "*** AVISO: hay $(echo "$CAMBIOS" | wc -l) archivo(s) de unity/ mas nuevos que el ultimo export (unity/AndroidExport): el APK llevaria una"
      echo "*** exportacion VIEJA. Si tocaste Unity, no uses --solo-app. Ejemplos:"
      echo "$CAMBIOS" | head -5 | sed "s|$REPO/||; s/^/***   /"
    else
      echo "(unity/ sin cambios desde el ultimo export: --solo-app es seguro)"
    fi
  else
    echo "*** AVISO: no existe unity/AndroidExport: el APK saldria sin juegos. Corre la verificacion sin --solo-app."
  fi
fi

T=$SECONDS
cd "$REPO"
./gradlew.bat testDebugUnitTest assembleDebug --console=plain > "$RESULTS/v-5-gradle.log" 2>&1 || fallo "5 Gradle" "compilacion o pruebas Kotlin" "$RESULTS/v-5-gradle.log"
KT="$(cat app/build/test-results/testDebugUnitTest/*.xml 2>/dev/null | grep -o '<testsuite [^>]*' | sed -n 's/.* tests="\([0-9]*\)".* failures="\([0-9]*\)".*/\1 \2/p' | awk '{t+=$1; f+=$2} END {print t+0" "f+0}')"
linea "5 Gradle" "OK ${KT% *} pruebas Kotlin, ${KT#* } fallos, APK compilado" "$((SECONDS - T))"

if [ "$INSTALAR" = 1 ]; then
  T=$SECONDS
  adb -s "$DEVICE" install -r app/build/outputs/apk/debug/app-debug.apk > "$RESULTS/v-6-install.log" 2>&1 \
    && linea "6 Instalar ($DEVICE)" "OK" "$((SECONDS - T))" || fallo "6 Instalar ($DEVICE)" "adb install" "$RESULTS/v-6-install.log"
fi
echo "=== TODO OK (total $((SECONDS - T0))s) ==="
