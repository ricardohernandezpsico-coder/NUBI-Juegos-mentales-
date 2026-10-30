package com.example.diag

import android.app.Application
import android.os.Build
import android.os.StrictMode
import com.example.BuildConfig
import java.io.File

/**
 * StrictMode SOLO en depuración (Tarea 192, parte 3): avisa en el registro de Android («StrictMode») cuando el hilo principal lee o escribe disco o usa la red, y cuando se filtra algo (un cursor o archivo sin cerrar, una Activity). En la versión de la tienda no hace nada (`BuildConfig.DEBUG`).
 *
 * - **Solo `penaltyLog`**: nunca `penaltyDeath`, nunca un cuadro de diálogo; un aviso no debe tumbar la app ni una prueba.
 * - **Solo el proceso principal.** `:unity` es el motor de Unity (código ajeno que lee y escribe disco en su hilo principal por diseño: archivos de arranque, PlayerPrefs, el registro de sondas) y no se puede recorrer en el emulador (Unity es arm64): ahí solo habría ruido que no
 *   se puede arreglar desde la app.
 * - Se arma al principio de `Application.onCreate`, para cubrir también lo que hacen los instaladores de esa misma función.
 * Para ver los avisos: `adb logcat -s StrictMode`; `docs/respaldo-y-diagnostico.md`, «StrictMode».
 */
object StrictModeDebug {
  fun install(app: Application) {
    if (!BuildConfig.DEBUG || !isMainProcess(app)) return
    StrictMode.setThreadPolicy(
      StrictMode.ThreadPolicy.Builder()
        .detectDiskReads()
        .detectDiskWrites()
        .detectNetwork()
        .penaltyLog()
        .build()
    )
    StrictMode.setVmPolicy(
      StrictMode.VmPolicy.Builder()
        .detectLeakedClosableObjects()
        .detectLeakedSqlLiteObjects()
        .detectActivityLeaks()
        .penaltyLog()
        .build()
    )
  }

  private fun isMainProcess(app: Application): Boolean {
    val name = if (Build.VERSION.SDK_INT >= 28) Application.getProcessName()
    else try { File("/proc/self/cmdline").readText().trim('\u0000') } catch (e: Exception) { app.packageName }
    return name == app.packageName
  }
}
