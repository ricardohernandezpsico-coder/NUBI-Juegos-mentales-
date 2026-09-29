#!/usr/bin/env bash
# Crea un unityLibrary FALSO en unity/AndroidExport/ para poder compilar y probar la app Android SIN Unity (lo usa la
# verificación automática de GitHub, .github/workflows/verificar.yml). Esa carpeta está fuera de git: en el PC de
# Ricardo la llena el export real de Unity, así que este script NO hace nada si ya existe (no pisa el export real).
# Necesita ANDROID_HOME con la plataforma android-36 instalada, y javac/jar (JDK).
# Es la misma idea que el bloque "unityLibrary FALSO" de tools/nube-compilar-app.sh (que además instala el SDK y es
# solo para las sesiones de la nube); está aparte para no tocar ese script.
set -euo pipefail
cd "$(dirname "$0")/.."
: "${ANDROID_HOME:?Falta ANDROID_HOME}"

U=unity/AndroidExport/unityLibrary
if [ -f "$U/libs/unity-classes.jar" ]; then
  echo "Ya hay un unityLibrary (real o falso): no se toca."
  exit 0
fi

echo "unityLibrary FALSO (solo para compilar la app sin Unity)"
S=$(mktemp -d)
mkdir -p "$S/src/com/unity3d/player" "$S/out" "$U/libs" "$U/src/main"
echo 'package com.unity3d.player; public class UnityPlayer { public static android.app.Activity currentActivity; }' > "$S/src/com/unity3d/player/UnityPlayer.java"
echo 'package com.unity3d.player; public class UnityPlayerGameActivity extends android.app.Activity {}' > "$S/src/com/unity3d/player/UnityPlayerGameActivity.java"
javac -d "$S/out" -cp "$ANDROID_HOME/platforms/android-36/android.jar" "$S"/src/com/unity3d/player/*.java
(cd "$S/out" && jar cf "$OLDPWD/$U/libs/unity-classes.jar" com)
cat > "$U/build.gradle" <<'G'
// STUB (no es el export real de Unity): solo para compilar y probar la app sin Unity.
plugins { id 'com.android.library' }
android { namespace = 'com.unity3d.player'; compileSdk = 36; defaultConfig { minSdk = 24 } }
dependencies { implementation files('libs/unity-classes.jar') }
G
echo '<manifest xmlns:android="http://schemas.android.com/apk/res/android" />' > "$U/src/main/AndroidManifest.xml"
echo "Listo: $U"
