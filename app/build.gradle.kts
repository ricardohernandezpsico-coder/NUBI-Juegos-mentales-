plugins {
  alias(libs.plugins.android.application)
  alias(libs.plugins.kotlin.compose)
  alias(libs.plugins.google.devtools.ksp)
  alias(libs.plugins.roborazzi)
}

android {
  namespace = "com.example"
  compileSdk { version = release(36) { minorApiLevel = 1 } }

  defaultConfig {
    applicationId = "com.nubibraingames.app"
    minSdk = 26
    targetSdk = 36
    versionCode = 1
    versionName = "1.0"

    testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
  }

  signingConfigs {
    create("release") {
      val keystorePath = System.getenv("KEYSTORE_PATH") ?: "${rootDir}/my-upload-key.jks"
      storeFile = file(keystorePath)
      storePassword = System.getenv("STORE_PASSWORD")
      keyAlias = "upload"
      keyPassword = System.getenv("KEY_PASSWORD")
    }
    create("debugConfig") {
      storeFile = file("${rootDir}/debug.keystore")
      storePassword = "android"
      keyAlias = "androiddebugkey"
      keyPassword = "android"
    }
  }

  buildTypes {
    release {
      isCrunchPngs = false
      isMinifyEnabled = false
      proguardFiles(getDefaultProguardFile("proguard-android-optimize.txt"), "proguard-rules.pro")
      signingConfig = signingConfigs.getByName("release")
      // Unity solo viene para arm64-v8a: sin el filtro, el APK trae carpetas vacías de armeabi-v7a/x86/x86_64 (solo una
      // librería de AndroidX) y Google Play ofrecería Nubi a aparatos donde los juegos se caen. Solo en release: el emulador
      // medium_phone (x86_64) sigue sirviendo con debug para la UI Compose.
      ndk { abiFilters += "arm64-v8a" }
    }
    debug { signingConfig = signingConfigs.getByName("debugConfig") }
  }
  compileOptions {
    sourceCompatibility = JavaVersion.VERSION_11
    targetCompatibility = JavaVersion.VERSION_11
  }
  buildFeatures {
    compose = true
    buildConfig = true
  }
  // Las pruebas de capturas de pantallas largas (Tú, Ajustes, «En qué se basa Nubi»: Tarea 185) se quedaban sin memoria con los 512 MB que Gradle da a cada JVM de pruebas por defecto: `maxHeapSize = "2g"`.
  // `DesignMdTest` lee `../docs/DESIGN.md` (fuera de `src/`): se declara como entrada de las pruebas para que un cambio del documento vuelva a correrlas (si no, Gradle las da por hechas).
  testOptions { unitTests { isIncludeAndroidResources = true; all { it.inputs.file(rootProject.file("docs/DESIGN.md")).withPropertyName("designMd"); it.maxHeapSize = "2g" } } }
  // Las pruebas de migración de la base de datos leen los esquemas exportados de Room (schemas/<versión>.json). Robolectric
  // solo ve los assets de la app (no los de la carpeta de pruebas), así que van en la variante de depuración: pesan unos
  // KB, y la versión de tienda (release) no los lleva.
  sourceSets { getByName("debug").assets.srcDir("$projectDir/schemas") }
  dependenciesInfo {
    includeInApk = false
    includeInBundle = true
  }
}

dependencies {
  // Fase 1 del roadmap de migración a Unity (piloto: Secuencia Lumínica) -- ver
  // NeuroVida/CLAUDE.md y unity/README.md.
  implementation(project(":unityLibrary"))
  // unity-classes.jar es "implementation" dentro de unityLibrary y no se ve desde la app: se agrega solo para compilar (UnityPlayer)
  compileOnly(files("../unity/AndroidExport/unityLibrary/libs/unity-classes.jar"))
  implementation(platform(libs.androidx.compose.bom))
  implementation(libs.androidx.activity.compose)
  implementation(libs.androidx.compose.material.icons.core)
  implementation(libs.androidx.compose.material.icons.extended)
  implementation(libs.androidx.compose.material3)
  implementation(libs.androidx.compose.ui)
  implementation(libs.androidx.compose.ui.graphics)
  implementation(libs.androidx.compose.ui.tooling.preview)
  implementation(libs.androidx.core.ktx)
  implementation(libs.androidx.lifecycle.runtime.compose)
  implementation(libs.androidx.lifecycle.runtime.ktx)
  implementation(libs.androidx.lifecycle.viewmodel.compose)
  implementation(libs.androidx.room.ktx)
  implementation(libs.androidx.room.runtime)
  implementation(libs.androidx.work.runtime.ktx)
  implementation(libs.kotlinx.coroutines.android)
  implementation(libs.kotlinx.coroutines.core)
  implementation(libs.moshi.kotlin)
  testImplementation(libs.androidx.compose.ui.test.junit4)
  testImplementation(libs.androidx.core)
  testImplementation(libs.androidx.junit)
  testImplementation(libs.androidx.room.testing)
  testImplementation(libs.junit)
  testImplementation(libs.kotlinx.coroutines.test)
  testImplementation(libs.robolectric)
  testImplementation(libs.roborazzi)
  testImplementation(libs.roborazzi.compose)
  testImplementation(libs.roborazzi.junit.rule)
  androidTestImplementation(platform(libs.androidx.compose.bom))
  androidTestImplementation(libs.androidx.compose.ui.test.junit4)
  androidTestImplementation(libs.androidx.espresso.core)
  androidTestImplementation(libs.androidx.junit)
  androidTestImplementation(libs.androidx.runner)
  debugImplementation(libs.androidx.compose.ui.test.manifest)
  debugImplementation(libs.androidx.compose.ui.tooling)
  androidTestImplementation(libs.androidx.room.testing)
  "ksp"(libs.androidx.room.compiler)
  "ksp"(libs.moshi.kotlin.codegen)
}

ksp {
  // Auditoría (21-sep, fase 2): habilita el historial de esquemas de Room -- sin esto
  // no hay forma de escribir ni de testear una Migration real (Room necesita el JSON
  // del esquema anterior para saber qué cambió entre versiones).
  arg("room.schemaLocation", "$projectDir/schemas")
}
