package com.example.diag

import android.app.Application
import android.content.Context
import android.os.Build
import androidx.core.content.pm.PackageInfoCompat
import java.io.File
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/**
 * Registro de errores EN EL TELÉFONO (sin servidores ni cuentas: esas decisiones quedan para el final). Guarda los
 * cierres inesperados de la app y de Unity y los avisos que hasta ahora solo iban al logcat (por ejemplo, un resultado
 * de un juego que no se pudo leer y por eso no se guardó). Desde Ajustes → "Enviar informe de errores" la persona
 * comparte el texto con quien le dio la app.
 *
 * Qué NO guarda: nombre, edad ni resultados de las partidas. Solo el tipo de error, la traza y un trozo corto del
 * dato que falló (como mucho [MAX_DETAIL] caracteres).
 *
 * Los dos procesos (la app y `:unity`) escriben en el mismo archivo; cada línea de cabecera dice de cuál viene.
 * Un cierre nativo de Unity (fallo del motor, no de Java) no pasa por aquí: Android lo informa por su lado.
 */
object ErrorLog {
  private const val FILE = "errores.txt"
  private const val MAX_BYTES = 96 * 1024
  private const val KEEP_BYTES = 48 * 1024
  private const val MAX_DETAIL = 300
  private const val MAX_TRACE = 4000

  private var dir: File? = null
  private var process = "app"

  /** Se llama una vez desde `Application.onCreate` (corre en el proceso de la app y en `:unity`). */
  fun install(app: Application) {
    val name = if (Build.VERSION.SDK_INT >= 28) Application.getProcessName()
    else runCatching { File("/proc/self/cmdline").readText().trim('\u0000') }.getOrNull()
    init(app.filesDir, if (name != null && name.endsWith(":unity")) "juego" else "app")
    val previous = Thread.getDefaultUncaughtExceptionHandler()
    Thread.setDefaultUncaughtExceptionHandler { thread, error ->
      record("CIERRE", "El proceso «$process» se cerró por un error en el hilo ${thread.name}", error)
      previous?.uncaughtException(thread, error)
    }
  }

  /** Separado de [install] para poder probarlo sin Android. */
  internal fun init(directory: File, processLabel: String) {
    dir = directory
    process = processLabel
  }

  /** Anota un error. Nunca lanza: registrar no puede ser lo que rompa la app. */
  @Synchronized
  fun record(tag: String, message: String, error: Throwable? = null) {
    val d = dir ?: return
    try {
      val file = File(d, FILE)
      if (file.length() > MAX_BYTES) trim(file)
      val stamp = SimpleDateFormat("yyyy-MM-dd HH:mm:ss", Locale.US).format(Date())
      val sb = StringBuilder()
      sb.append("[").append(stamp).append("] ").append(process).append(" · ").append(tag).append(": ")
        .append(message.take(MAX_DETAIL)).append('\n')
      if (error != null) sb.append(error.stackTraceToString().take(MAX_TRACE)).append('\n')
      sb.append('\n')
      file.appendText(sb.toString())
    } catch (_: Throwable) {
      // Sin espacio o sin permiso: no hay más que hacer.
    }
  }

  /** Lo más reciente del registro, hasta [maxChars] caracteres (vacío si no hay nada). */
  @Synchronized
  fun readRecent(maxChars: Int = 12_000): String {
    val file = File(dir ?: return "", FILE)
    if (!file.exists()) return ""
    return try {
      val text = file.readText()
      if (text.length <= maxChars) text else "…\n" + text.takeLast(maxChars)
    } catch (_: Throwable) {
      ""
    }
  }

  @Synchronized
  fun clear() {
    runCatching { File(dir ?: return, FILE).delete() }
  }

  /** El texto que se comparte desde Ajustes: versión y teléfono arriba, y después lo registrado. */
  fun report(context: Context): String {
    val info = runCatching { context.packageManager.getPackageInfo(context.packageName, 0) }.getOrNull()
    val version = info?.let { "${it.versionName} (${PackageInfoCompat.getLongVersionCode(it)})" } ?: "?"
    val header = "Nubi $version · Android ${Build.VERSION.RELEASE} (API ${Build.VERSION.SDK_INT}) · ${Build.MANUFACTURER} ${Build.MODEL}"
    val body = readRecent()
    return if (body.isBlank()) "$header\n\nNo hay errores registrados." else "$header\n\n$body"
  }

  /** Para que la primera línea de un archivo cortado no quede a medias. */
  private fun trim(file: File) {
    val tail = file.readText().takeLast(KEEP_BYTES)
    val firstBreak = tail.indexOf("\n\n")
    file.writeText(if (firstBreak in 0 until tail.length - 2) tail.substring(firstBreak + 2) else tail)
  }
}
