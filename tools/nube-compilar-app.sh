#!/usr/bin/env bash
# Compila la app Android y corre sus pruebas Kotlin EN LA NUBE (sin Unity ni teléfono). Idempotente.
#   bash tools/nube-compilar-app.sh            -> :app:testDebugUnitTest (compila todo + pruebas)
#   bash tools/nube-compilar-app.sh fotos      -> además graba las fotos Roborazzi (app/src/test/screenshots/)
# Prepara (solo la primera vez): Android SDK en /root/android-sdk, un unityLibrary FALSO en unity/AndroidExport/ (esa
# carpeta está fuera de git; en el PC de Ricardo la llena el export real de Unity: NO correr esto allá) y el espejo de
# Maven Central de Google en ~/.gradle/init.d (Maven Central a veces responde 429 a la nube).
set -euo pipefail
cd "$(dirname "$0")/.."
export ANDROID_HOME=/root/android-sdk
if [ ! -x "$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" ]; then
  mkdir -p "$ANDROID_HOME/cmdline-tools" && cd "$ANDROID_HOME/cmdline-tools"
  curl -sS -o tools.zip https://dl.google.com/android/repository/commandlinetools-linux-13114758_latest.zip
  unzip -q tools.zip && mv cmdline-tools latest && rm tools.zip && cd - >/dev/null
fi
yes | "$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" --licenses >/dev/null 2>&1 || true
"$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" "platforms;android-36" "platforms;android-36.1" "build-tools;36.1.0" >/dev/null
[ -f local.properties ] || echo "sdk.dir=$ANDROID_HOME" > local.properties

U=unity/AndroidExport/unityLibrary
if [ ! -f "$U/libs/unity-classes.jar" ]; then
  echo "unityLibrary FALSO (solo nube)"
  S=$(mktemp -d); mkdir -p "$S/src/com/unity3d/player" "$S/out" "$U/libs" "$U/src/main"
  echo 'package com.unity3d.player; public class UnityPlayer { public static android.app.Activity currentActivity; }' > "$S/src/com/unity3d/player/UnityPlayer.java"
  echo 'package com.unity3d.player; public class UnityPlayerGameActivity extends android.app.Activity {}' > "$S/src/com/unity3d/player/UnityPlayerGameActivity.java"
  javac -d "$S/out" -cp "$ANDROID_HOME/platforms/android-36/android.jar" "$S"/src/com/unity3d/player/*.java
  (cd "$S/out" && jar cf "$OLDPWD/$U/libs/unity-classes.jar" com)
  cat > "$U/build.gradle" <<'G'
// STUB de la nube (no es el export real de Unity): solo para compilar y probar la app sin Unity.
plugins { id 'com.android.library' }
android { namespace = 'com.unity3d.player'; compileSdk = 36; defaultConfig { minSdk = 24 } }
dependencies { implementation files('libs/unity-classes.jar') }
G
  echo '<manifest xmlns:android="http://schemas.android.com/apk/res/android" />' > "$U/src/main/AndroidManifest.xml"
fi

mkdir -p ~/.gradle/init.d
cat > ~/.gradle/init.d/mirror.gradle <<'G'
// Solo en la nube: espejo de Maven Central de Google, PRIMERO (Maven Central responde 429 a veces).
def mirror = 'https://maven-central.storage-download.googleapis.com/maven2/'
beforeSettings { settings ->
  settings.pluginManagement.repositories.maven { url = mirror }
  settings.dependencyResolutionManagement.repositories.maven { url = mirror }
}
G

bash ./gradlew :app:testDebugUnitTest --console=plain -q
if [ "${1:-}" = "fotos" ]; then bash ./gradlew :app:recordRoborazziDebug --console=plain -q; fi
echo "OK: app compilada y pruebas Kotlin en verde"
