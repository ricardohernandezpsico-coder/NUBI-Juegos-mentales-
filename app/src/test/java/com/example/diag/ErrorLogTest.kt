package com.example.diag

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.rules.TemporaryFolder

class ErrorLogTest {
  @get:Rule val folder = TemporaryFolder()

  @Before
  fun setUp() {
    ErrorLog.init(folder.newFolder("files"), "app")
  }

  @Test
  fun `sin errores no hay nada que leer`() {
    assertEquals("", ErrorLog.readRecent())
  }

  @Test
  fun `un error queda con su etiqueta, su mensaje y su traza`() {
    ErrorLog.record("RESULTADO", "Una partida no se guardó", IllegalStateException("json roto"))
    val text = ErrorLog.readRecent()
    assertTrue(text.contains("app · RESULTADO: Una partida no se guardó"))
    assertTrue(text.contains("IllegalStateException"))
    assertTrue(text.contains("json roto"))
  }

  @Test
  fun `el proceso de Unity se distingue del de la app`() {
    ErrorLog.init(folder.newFolder("files2"), "juego")
    ErrorLog.record("CIERRE", "se cerró")
    assertTrue(ErrorLog.readRecent().contains("juego · CIERRE"))
  }

  @Test
  fun `un mensaje muy largo se corta para no guardar datos de mas`() {
    ErrorLog.record("RESULTADO", "x".repeat(5000))
    val line = ErrorLog.readRecent().lines().first()
    assertTrue("la línea de un error no debería pasar de unos 400 caracteres: ${line.length}", line.length < 400)
  }

  @Test
  fun `el registro no crece sin limite`() {
    repeat(600) { ErrorLog.record("PRUEBA", "error número $it ".padEnd(300, '.')) }
    val size = java.io.File(folder.root, "files/errores.txt").length()
    assertTrue("el archivo debería quedar bajo ~100 KB y mide $size", size < 110 * 1024)
    // Y lo que queda es lo más reciente.
    assertTrue(ErrorLog.readRecent().contains("error número 599"))
    assertFalse(ErrorLog.readRecent().contains("error número 0 "))
  }

  @Test
  fun `leer solo devuelve lo mas reciente`() {
    repeat(200) { ErrorLog.record("PRUEBA", "error número $it") }
    val recent = ErrorLog.readRecent(maxChars = 500)
    assertTrue(recent.length <= 510)
    assertTrue(recent.contains("error número 199"))
  }

  @Test
  fun `borrar deja el registro vacio`() {
    ErrorLog.record("PRUEBA", "algo")
    ErrorLog.clear()
    assertEquals("", ErrorLog.readRecent())
  }

  @Test
  fun `registrar sin haber iniciado el registro no falla`() {
    ErrorLog.init(java.io.File("/no/existe/para/nada"), "app")
    ErrorLog.record("PRUEBA", "no debe lanzar")
    assertEquals("", ErrorLog.readRecent())
  }
}
