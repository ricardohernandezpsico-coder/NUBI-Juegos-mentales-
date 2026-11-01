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
  fun `la hoja del candado son tres lineas y habla como la app`() {
    assertEquals("Con Nubi Plus", PlusAccess.LOCK_LABEL)
    assertEquals("un solo botón", "Entendido", PlusAccess.SHEET_OK)
    for (compared in listOf(false, true)) {
      val items = PlusAccess.sheetItems(compared)
      assertEquals("tres líneas (Tarea 203), con comparación $compared", 3, items.size)
      val all = (listOf(PlusAccess.SHEET_TITLE, PlusAccess.sheetLead(compared)) + items).joinToString(" ").lowercase()
      for (bad in listOf("nivel", "rendimiento", "capacidad", "puntaje", "edad mental", "cognitiv", "cerebro", "entrenamiento", "precio", "\$", "gratis", "ranking", "velocidad", "percentil")) assertFalse("la hoja no dice «$bad»", bad in all)
      assertTrue("cada línea es una frase completa", items.all { it.endsWith(".") })
      assertTrue("y el encabezado cierra con dos puntos", PlusAccess.sheetLead(compared).endsWith(":"))
    }
    assertEquals("la preferencia es pasajera: otro archivo que el del perfil", "plus_access", PlusAccess.PREFS)
  }

  @Test
  fun `la hoja solo promete la comparacion cuando la build la tiene`() {
    assertEquals("sin comparación (la tienda), solo lo que hay detrás: contigo", PlusAccess.SHEET_LEAD, PlusAccess.sheetLead(false))
    assertTrue("con comparación habla de personas de tu edad", "personas de tu edad" in PlusAccess.sheetLead(true))
    assertFalse("sin ella ninguna línea habla de otras personas", PlusAccess.sheetItems(false).any { "personas" in it || "edad" in it || "división" in it || "divisiones" in it })
    assertTrue("con ella sí: el lugar y la escalera", PlusAccess.sheetItems(true).let { l -> "lugar" in l[0] && "escalera" in l[1] })
  }
}
