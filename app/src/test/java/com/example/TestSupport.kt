package com.example

import android.os.Looper
import com.example.data.local.NeuroVidaDatabase
import org.junit.Assert.fail
import org.robolectric.Shadows.shadowOf

/** Ayudas para las pruebas con Robolectric que crean el ViewModel de verdad (ver GameFlowTest). */
object TestSupport {
  /**
   * La base de datos es un singleton que guarda el contexto de la primera prueba; Robolectric borra los archivos entre
   * pruebas, así que hay que soltarlo o la segunda prueba usaría una base ya cerrada.
   */
  fun resetDatabase() {
    val field = NeuroVidaDatabase::class.java.getDeclaredField("INSTANCE")
    field.isAccessible = true
    (field.get(null) as? NeuroVidaDatabase)?.let { runCatching { it.close() } }
    field.set(null, null)
  }

  /**
   * Espera a que se cumpla [condition]. El ViewModel guarda en Room en otro hilo y vuelve al hilo principal para
   * publicar el resultado: hay que ir dejando correr el hilo principal de Robolectric mientras se espera.
   */
  fun awaitUntil(timeoutMs: Long = 10_000, message: String = "Se acabó el tiempo esperando", condition: () -> Boolean) {
    val end = System.currentTimeMillis() + timeoutMs
    while (!condition()) {
      shadowOf(Looper.getMainLooper()).idle()
      if (System.currentTimeMillis() > end) fail(message)
      Thread.sleep(20)
    }
  }
}
