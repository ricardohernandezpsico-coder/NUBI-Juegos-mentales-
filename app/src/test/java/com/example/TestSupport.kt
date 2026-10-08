package com.example

import android.os.Looper
import com.example.data.local.NeuroVidaDatabase
import org.junit.Assert.fail
import org.robolectric.Shadows.shadowOf

/** Ayudas para las pruebas con Robolectric que crean el ViewModel de verdad (ver GameFlowTest). */
object TestSupport {
  /**
   * LA forma de preparar la base en las pruebas (todas las que crean el ViewModel, el repositorio o leen la base la llaman en `@Before` y en `@After`): suelta lo de la prueba anterior y deja una base de Room
   * EN MEMORIA, nueva y vacía, como la que devuelve `NeuroVidaDatabase.getDatabase`. Nada se abre en disco.
   *
   * Por qué: antes la base era el archivo `neurovida_database` en la carpeta temporal de Robolectric, que se borra entre pruebas; el trabajo de fondo del repositorio (un flujo de Room que nunca se cancelaba)
   * o la propia base lo reabrían ya sin carpeta y fallaba, a veces, con «unable to open database file» (sobre todo con la máquina cargada). Una base en memoria no tiene archivo que pueda desaparecer, y el
   * repositorio de la prueba anterior se cancela aquí para que ningún trabajo viejo siga tocando la base nueva.
   */
  fun resetDatabase() {
    closeAppRepository()
    val context = androidx.test.core.app.ApplicationProvider.getApplicationContext<android.content.Context>()
    val db = androidx.room.Room.inMemoryDatabaseBuilder(context, NeuroVidaDatabase::class.java).allowMainThreadQueries().build()
    NeuroVidaDatabase.setInstanceForTesting(db)
    // los recordatorios (WorkManager) no se usan en las pruebas del ViewModel: ver CognitiveReminderWorker.disabledForTests
    com.example.notification.CognitiveReminderWorker.disabledForTests = true
    resetWorkManager()
  }

  /** Cancela el trabajo de fondo del repositorio de la aplicación de la prueba (el que usan los ViewModel), si ya se creó. */
  private fun closeAppRepository() {
    runCatching {
      val app = androidx.test.core.app.ApplicationProvider.getApplicationContext<android.content.Context>() as? com.example.NeuroVidaApplication ?: return
      val field = com.example.NeuroVidaApplication::class.java.getDeclaredField("repository\$delegate")
      field.isAccessible = true
      val lazy = field.get(app) as? Lazy<*> ?: return
      if (lazy.isInitialized()) (lazy.value as? com.example.data.NeuroVidaRepository)?.close()
    }
  }

  /** Cancela el trabajo de fondo de un repositorio que la prueba creó por su cuenta (`NeuroVidaRepository(app)`); va en el `@After`, antes de [resetDatabase]. */
  fun release(repository: com.example.data.NeuroVidaRepository) {
    runCatching { repository.close() }
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
