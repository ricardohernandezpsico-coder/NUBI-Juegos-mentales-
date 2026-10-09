package com.example

import android.app.Application
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.room.Room
import androidx.room.RoomDatabase
import androidx.test.core.app.ApplicationProvider
import com.example.data.NeuroVidaRepository
import com.example.data.local.NeuroVidaDatabase
import java.util.concurrent.Executor
import java.util.concurrent.atomic.AtomicBoolean
import java.util.concurrent.atomic.AtomicInteger
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * El rojo intermitente de la verificación de GitHub (Tarea 57): `RetiredBitacoraTest` fallaba a veces con «UncaughtExceptionsBeforeTest … attempt to re-open an already-closed object: SQLiteDatabase: :memory:».
 *
 * Qué pasaba: al terminar una prueba se cancelaba el trabajo de fondo del repositorio y de los ViewModel, pero NADIE esperaba a que terminara. Una lectura de Room que ya estaba a medias (no se puede interrumpir) seguía
 * corriendo mientras se cerraba la base de esa prueba; al terminar, su `endTransaction` encontraba la base cerrada y el error llegaba, como si fuera suyo, a la prueba siguiente. Dependía de la velocidad de la máquina.
 *
 * Estas pruebas lo reproducen SIEMPRE: la base de la prueba tarda 150 ms en cada consulta (la espera va dentro de la consulta, en el hilo que la hace), así que cuando se suelta el repositorio o el ViewModel hay
 * una lectura a medias seguro. Soltar tiene que ESPERAR a que termine.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class BackgroundWorkShutdownTest {
  private lateinit var app: Application

  /** Cuántas consultas están a medias ahora mismo (dentro de su espera de 150 ms). */
  private val queriesInFlight = AtomicInteger(0)

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
  }

  @After
  fun tearDown() {
    TestSupport.resetDatabase()
  }

  private fun slowDatabase(): NeuroVidaDatabase =
    Room.inMemoryDatabaseBuilder(app, NeuroVidaDatabase::class.java)
      .allowMainThreadQueries()
      .setQueryCallback(RoomDatabase.QueryCallback { _, _ ->
        queriesInFlight.incrementAndGet()
        try { Thread.sleep(150) } finally { queriesInFlight.decrementAndGet() }
      }, Executor { it.run() })        // en el hilo de quien consulta: la espera frena de verdad a esa lectura
      .build()

  private fun awaitAQueryInFlight() {
    val end = System.currentTimeMillis() + 10_000
    while (queriesInFlight.get() == 0 && System.currentTimeMillis() < end) Thread.sleep(2)
    assertTrue("el repositorio nunca empezó a leer la base", queriesInFlight.get() > 0)
  }

  @Test
  fun `cerrar el repositorio espera a la lectura que ya estaba a medias`() {
    val db = slowDatabase()
    val repository = NeuroVidaRepository(app, db)
    awaitAQueryInFlight()
    repository.close()
    assertEquals("al volver de close() no puede quedar ninguna lectura a medias: la base se cierra justo despues", 0, queriesInFlight.get())
    db.close()
  }

  /** Un ViewModel que, al terminar la prueba, todavía tiene trabajo en la base en otro hilo (lo que hace el de la app al arrancar). */
  private class BusyViewModel(val finished: AtomicBoolean, val started: AtomicBoolean) : ViewModel() {
    init {
      viewModelScope.launch {
        withContext(Dispatchers.IO) {
          started.set(true)
          Thread.sleep(300)           // algo que no se puede interrumpir: una lectura de Room a medias
          finished.set(true)
        }
      }
    }
  }

  @Test
  fun `soltar un ViewModel espera al trabajo que tenia en marcha`() {
    val finished = AtomicBoolean(false)
    val started = AtomicBoolean(false)
    val vm = BusyViewModel(finished, started)
    val end = System.currentTimeMillis() + 10_000
    while (!started.get() && System.currentTimeMillis() < end) Thread.sleep(2)
    assertTrue("el trabajo de fondo nunca empezó", started.get())
    TestSupport.release(vm)
    assertTrue("al volver de release() el ViewModel no puede seguir trabajando: la base se cierra justo despues", finished.get())
  }
}
