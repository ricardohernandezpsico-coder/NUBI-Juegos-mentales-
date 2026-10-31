package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * El interruptor de Nubi Plus (`PlusAccess`, Tarea 199): una sola fuente local; lo guardado manda y, sin nada guardado, las builds de depuración empiezan con Plus encendido y la de la tienda apagado. Y lo que dice la hoja del candado: breve, con unidades, sin las palabras prohibidas.
 */
class PlusAccessTest {
  @Test
  fun `sin nada guardado depuracion empieza encendido y la tienda apagado`() {
    assertTrue(PlusAccess.resolve(null, debugBuild = true))
    assertFalse(PlusAccess.resolve(null, debugBuild = false))
  }

  @Test
  fun `lo guardado manda sobre la build`() {
    assertFalse("el botón de depuración lo apagó", PlusAccess.resolve(false, debugBuild = true))
    assertTrue("y lo encendió", PlusAccess.resolve(true, debugBuild = false))
  }

  @Test
  fun `la hoja del candado es breve y habla como la app`() {
    assertEquals("Con Nubi Plus", PlusAccess.LOCK_LABEL)
    assertEquals("un solo botón", "Entendido", PlusAccess.SHEET_OK)
    assertTrue("una lista breve", PlusAccess.SHEET_ITEMS.size in 3..6)
    val all = (listOf(PlusAccess.SHEET_TITLE, PlusAccess.SHEET_LEAD) + PlusAccess.SHEET_ITEMS).joinToString(" ").lowercase()
    for (bad in listOf("nivel", "rendimiento", "capacidad", "puntaje", "edad mental", "cognitiv", "cerebro", "entrenamiento", "precio", "\$", "gratis")) assertFalse("la hoja no dice «$bad»", bad in all)
    assertTrue("cada línea es una frase completa", PlusAccess.SHEET_ITEMS.all { it.endsWith(".") })
    assertEquals("la preferencia es pasajera: otro archivo que el del perfil", "plus_access", PlusAccess.PREFS)
  }
}
