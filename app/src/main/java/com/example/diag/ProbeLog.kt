package com.example.diag

import com.example.BuildConfig
import java.io.File
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/**
 * El archivo de las sondas de entrada al juego (Tarea 190b), SOLO en las builds de depuración: `files/sonda-entrada.txt`, que se lee con
 * `adb shell run-as com.nubibraingames.app cat files/sonda-entrada.txt` (el logcat de un teléfono real se rota en minutos y pierde las partidas anteriores).
 * Lo escriben Unity (`EntryProbe`, por `NativeReceiver.appendProbe`) y la pantalla de carga (`UnityLoadingOverlay`), los dos en el proceso `:unity`.
 *
 * Agrega al final, una línea con la hora; pasados [MAX_BYTES] se queda con la segunda mitad (cortada en una línea entera). NO va en el respaldo: ni `backup_rules.xml` ni
 * `data_extraction_rules.xml` incluyen el dominio de archivos (solo la base y las preferencias de progreso; la prueba `ProbeLogTest` lo vigila). En release no escribe nada.
 */
object ProbeLog {
  const val FILE = "sonda-entrada.txt"
  const val MAX_BYTES = 256 * 1024

  private var dir: File? = null

  /** Se llama una vez desde `Application.onCreate` (corre en el proceso de la app y en `:unity`). */
  fun install(directory: File) {
    dir = directory
  }

  /** Añade una línea (con la hora). Solo en depuración; nunca lanza: medir no puede romper nada. */
  @Synchronized
  fun append(line: String) = write(line, BuildConfig.DEBUG)

  /** Separado de [append] para probarlo con la bandera en los dos valores. */
  @Synchronized
  internal fun write(line: String, debug: Boolean) {
    if (!debug) return
    val d = dir ?: return
    try {
      val file = File(d, FILE)
      if (file.length() > MAX_BYTES) trim(file)
      val stamp = SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSS", Locale.US).format(Date())
      file.appendText("$stamp $line\n")
    } catch (_: Exception) {
      // nada: medir no puede ser lo que rompa la app
    }
  }

  private fun trim(file: File) {
    val bytes = file.readBytes()
    val from = bytes.size - MAX_BYTES / 2
    var start = from
    while (start < bytes.size && bytes[start] != '\n'.code.toByte()) start++            // al corte de la primera línea entera
    file.writeBytes(bytes.copyOfRange(minOf(start + 1, bytes.size), bytes.size))
  }
}
