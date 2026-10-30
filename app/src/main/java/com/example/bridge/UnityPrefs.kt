package com.example.bridge

import android.app.ActivityManager
import android.content.Context
import java.io.File

/**
 * Las preferencias de Unity (`PlayerPrefs`; Tarea 191, H4 del diagnóstico): las marcas de «ya visto» de las tarjetas «NUEVO», los modos de Rastro de luz y la rotación y el repaso de palabras. Unity las guarda en
 * `shared_prefs/<applicationId>.v2.playerprefs.xml` y las tiene EN MEMORIA en su proceso (`:unity`), que queda VIVO detrás de la app entre partidas: borrar el archivo desde la app no basta, porque Unity volvería a escribir lo que tiene
 * en memoria. Son PROGRESO (`intrusa_review` e `intrusa_plates` son avance de verdad): se respaldan con el archivo entero (`backup_rules.xml`) y «Borrar datos» las borra enteras.
 *
 * «Borrar datos» ([clear]) pide el borrado a Unity: la app anota «borrar» ([requestReset]) y la PRÓXIMA configuración que le manda a Unity lleva `reset_unity_prefs` (se borra DENTRO del proceso de Unity, esté vivo o no, antes de que lea una sola
 * marca). Si Unity no está vivo, además se borran los archivos en el acto.
 */
object UnityPrefs {
  private const val SUFFIX = ".playerprefs.xml"
  private const val RESET_FILE = "unity_reset"
  private const val PENDING = "pending"

  /** El archivo de preferencias de Unity de esta instalación (el que respaldan `backup_rules.xml` y `data_extraction_rules.xml`). */
  fun fileName(context: Context): String = context.packageName + ".v2.playerprefs.xml"

  /** Los archivos de preferencias de Unity de la app. */
  fun files(context: Context): List<File> =
    File(context.applicationInfo.dataDir, "shared_prefs").listFiles { f -> f.name.endsWith(SUFFIX) }?.toList().orEmpty()

  /** El proceso de Unity (`:unity`) está vivo: tiene las preferencias en memoria. */
  fun unityProcessAlive(context: Context): Boolean {
    val manager = context.getSystemService(Context.ACTIVITY_SERVICE) as? ActivityManager ?: return false
    return manager.runningAppProcesses.orEmpty().any { it.processName.endsWith(":unity") }
  }

  /** La próxima configuración que reciba Unity le pide borrar sus preferencias. */
  fun requestReset(context: Context) {
    context.getSharedPreferences(RESET_FILE, Context.MODE_PRIVATE).edit().putBoolean(PENDING, true).commit()
  }

  fun resetPending(context: Context): Boolean = context.getSharedPreferences(RESET_FILE, Context.MODE_PRIVATE).getBoolean(PENDING, false)

  /** Unity ya recibió la configuración con el pedido de borrar (la app lo llama al lanzar la partida). */
  fun clearPending(context: Context) {
    if (resetPending(context)) context.getSharedPreferences(RESET_FILE, Context.MODE_PRIVATE).edit().remove(PENDING).commit()
  }

  /**
   * «Borrar datos»: Unity las borra al empezar la próxima partida (con su proceso vivo o no) y, si no hay proceso de Unity (nadie las tiene en memoria), los archivos se borran ya. Las preferencias de la APP (progreso, récords) las borra el repositorio.
   */
  fun clear(context: Context) {
    requestReset(context)
    if (!unityProcessAlive(context)) files(context).forEach { f ->
      context.deleteSharedPreferences(f.name.removeSuffix(".xml"))
      f.delete()
    }
  }
}
