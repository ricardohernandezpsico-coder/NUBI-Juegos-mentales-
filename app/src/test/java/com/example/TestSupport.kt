package com.example

import android.os.Looper
import com.example.data.local.NeuroVidaDatabase
import org.junit.Assert.fail
import org.robolectric.Shadows.shadowOf

/** Ayudas para las pruebas con Robolectric que crean el ViewModel de verdad (ver GameFlowTest). */
object TestSupport {
  /**
   * Regla que repite una prueba (hasta 3 veces, soltando la base entre intentos) si falla con «unable to open database file»: la base de la prueba anterior (o su trabajo de fondo) a veces se cruza
   * con la siguiente. Solo para pruebas que ya fallaron así sin que el código tuviera culpa; cualquier otro fallo se informa tal cual.
   */
  fun retryOnDbFlake(times: Int = 3): org.junit.rules.TestRule = org.junit.rules.TestRule { base, _ ->
    object : org.junit.runners.model.Statement() {
      override fun evaluate() {
        var last: Throwable? = null
        repeat(times) {
          try { base.evaluate(); return }
          catch (e: Throwable) {
            var c: Throwable? = e
            var flake = false
            while (c != null) { if (c is android.database.sqlite.SQLiteCantOpenDatabaseException) flake = true; c = c.cause }
            if (!flake) throw e
            last = e
            resetDatabase()
            Thread.sleep(300)
          }
        }
        throw last!!
      }
    }
  }

  /**
   * La base de datos es un singleton que guarda el contexto de la primera prueba; Robolectric borra los archivos entre
   * pruebas, así que hay que soltarlo o la segunda prueba usaría una base ya cerrada.
   */
  fun resetDatabase() {
    val field = NeuroVidaDatabase::class.java.getDeclaredField("INSTANCE")
    field.isAccessible = true
    (field.get(null) as? NeuroVidaDatabase)?.let { runCatching { it.close() } }
    field.set(null, null)
    resetWorkManager()
    // La carpeta de bases de datos de la prueba puede haber desaparecido (Robolectric la limpia entre pruebas): sin ella la base no se puede abrir.
    runCatching { androidx.test.core.app.ApplicationProvider.getApplicationContext<android.content.Context>().getDatabasePath("neurovida_database").parentFile?.mkdirs() }
  }

  /**
   * WorkManager también es un singleton con su propia base: la que inicializó una prueba queda atada a su carpeta temporal (que Robolectric borra) y la prueba siguiente que
   * lo use da «unable to open database file». Se suelta junto con la base de datos de la app.
   */
  fun resetWorkManager() {
    runCatching {
      val impl = Class.forName("androidx.work.impl.WorkManagerImpl")
      val setDelegate = impl.getMethod("setDelegate", impl)
      setDelegate.invoke(null, null)
    }
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

  /**
   * Suelta los ViewModel de una prueba (cancela lo que siguen haciendo en segundo plano) y deja correr el hilo principal, ANTES de soltar la base. Sin esto, un ViewModel que todavía consulta la base
   * cuando termina la prueba recibe «unable to open database file» (los archivos ya no están) y el error le llega a la prueba como si fuera suyo: pasaba sobre todo cuando todo corre rápido.
   */
  fun release(vararg viewModels: androidx.lifecycle.ViewModel) {
    for (vm in viewModels) {
      runCatching {
        val clear = androidx.lifecycle.ViewModel::class.java.getDeclaredMethod("clear")
        clear.isAccessible = true
        clear.invoke(vm)
      }
    }
    runCatching { shadowOf(Looper.getMainLooper()).idle() }
  }
}
