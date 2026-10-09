package com.example

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.data.local.NeuroVidaDatabase
import java.io.File
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * Guardián de las pruebas con base de datos (tarea 45, 8-oct): ninguna prueba abre el archivo `neurovida_database` en disco.
 *
 * Antes la base de las pruebas era ese archivo dentro de la carpeta temporal de Robolectric, que se borra entre pruebas, y a veces fallaba con «unable to open database file». Ahora `TestSupport.resetDatabase()`
 * deja una base de Room EN MEMORIA. Estas pruebas hacen que ese cuidado no se pierda sin avisar: una prueba nueva que cree el ViewModel o el repositorio (o lea la base) sin pasar por `TestSupport.resetDatabase()`
 * falla aquí, con el nombre del archivo, en vez de fallar una vez de cada veinte.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class NoDiskDatabaseGuardTest {
  @After
  fun tearDown() {
    TestSupport.resetDatabase()
  }

  @Test
  fun `despues de resetDatabase la base es en memoria y el archivo no existe`() {
    val app = ApplicationProvider.getApplicationContext<Application>()
    TestSupport.resetDatabase()
    val db = NeuroVidaDatabase.getDatabase(app)
    assertNull("una base en memoria no tiene nombre de archivo", db.openHelper.databaseName)
    // y de verdad funciona: se puede leer y escribir
    db.openHelper.writableDatabase.query("SELECT 1").use { assertTrue(it.moveToFirst()) }
    assertFalse("no debe existir el archivo neurovida_database", app.getDatabasePath("neurovida_database").exists())
  }

  @Test
  fun `cada resetDatabase da una base nueva y vacia`() {
    val app = ApplicationProvider.getApplicationContext<Application>()
    TestSupport.resetDatabase()
    val first = NeuroVidaDatabase.getDatabase(app)
    TestSupport.resetDatabase()
    val second = NeuroVidaDatabase.getDatabase(app)
    assertTrue("una instancia nueva por prueba", first !== second)
    assertFalse("la anterior se cerró", first.isOpen)
    assertEquals(0, second.openHelper.writableDatabase.query("SELECT COUNT(*) FROM game_results").use { it.moveToFirst(); it.getInt(0) })
  }

  /** Revisión del código de las pruebas: quien toca la base, el repositorio o el ViewModel de la app tiene que preparar la base con `TestSupport.resetDatabase()` (y el recordatorio de WorkManager queda apagado). */
  @Test
  fun `toda prueba que usa la base, el repositorio o el ViewModel prepara la base con TestSupport`() {
    val root = File("src/test/java")
    assertTrue("no encontré las pruebas en ${root.absolutePath}", root.isDirectory)
    val touches = listOf("NeuroVidaDatabase.getDatabase(", "NeuroVidaRepository(", "NeuroVidaViewModel(")
    val offenders = root.walkTopDown()
      .filter { it.isFile && it.extension == "kt" }
      .filter { it.name != "TestSupport.kt" && it.name != "NoDiskDatabaseGuardTest.kt" }
      .filter { f ->
        val text = f.readText()
        touches.any { it in text } && "TestSupport.resetDatabase()" !in text
      }
      .map { it.name }
      .toList()
    assertTrue("Estas pruebas usan la base/repositorio/ViewModel sin `TestSupport.resetDatabase()` (en @Before y @After): $offenders", offenders.isEmpty())
  }

  /**
   * Y quien crea el ViewModel o el repositorio tiene que SOLTARLO al terminar (`TestSupport.release`, o `close()` del repositorio): sin eso su trabajo de fondo sigue corriendo cuando se cierra la base y el error le llega a la
   * prueba siguiente (el rojo intermitente de GitHub, Tarea 57; ver `BackgroundWorkShutdownTest`).
   */
  @Test
  fun `toda prueba que crea el ViewModel o el repositorio los suelta al terminar`() {
    val root = File("src/test/java")
    val creates = listOf("NeuroVidaRepository(", "NeuroVidaViewModel(")
    val offenders = root.walkTopDown()
      .filter { it.isFile && it.extension == "kt" }
      .filter { it.name != "TestSupport.kt" && it.name != "NoDiskDatabaseGuardTest.kt" }
      .filter { f ->
        val text = f.readText()
        creates.any { it in text } && "TestSupport.release(" !in text && ".close()" !in text
      }
      .map { it.name }
      .toList()
    assertTrue("Estas pruebas crean el ViewModel o el repositorio y no los sueltan (`TestSupport.release(...)` en el @After, antes de `resetDatabase()`): $offenders", offenders.isEmpty())
  }

  @Test
  fun `ninguna prueba crea su propia base en archivo con Room`() {
    val root = File("src/test/java")
    val offenders = root.walkTopDown()
      .filter { it.isFile && it.extension == "kt" && it.name != "NoDiskDatabaseGuardTest.kt" }
      .filter { "Room.databaseBuilder(" in it.readText() }
      .map { it.name }
      .toList()
    assertTrue("Estas pruebas abren una base de Room en archivo (usa Room.inMemoryDatabaseBuilder o TestSupport.resetDatabase()): $offenders", offenders.isEmpty())
  }
}
