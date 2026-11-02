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
    assertEquals("Con Nubi Plus: además, dónde queda tu marca frente a tu edad y a otras edades.", PlusAccess.LOCK_LABEL_COMPARED)
    assertEquals("sin comparación, el candado corto", PlusAccess.LOCK_LABEL, PlusAccess.lockLabel(Comparison.Kind.NONE))
    assertEquals("con la referencia, el largo", PlusAccess.LOCK_LABEL_COMPARED, PlusAccess.lockLabel(Comparison.Kind.ESTIMATED))
    assertEquals("con personas, también", PlusAccess.LOCK_LABEL_COMPARED, PlusAccess.lockLabel(Comparison.Kind.PEOPLE))
    assertEquals("un solo botón: «Cerrar», no «Entendido» (Tarea 205)", "Cerrar", PlusAccess.SHEET_OK)
    for (kind in Comparison.Kind.entries) {
      val items = PlusAccess.sheetItems(kind)
      assertEquals("tres líneas (Tarea 203), con comparación $kind", 3, items.size)
      val all = (listOf(PlusAccess.SHEET_TITLE, PlusAccess.sheetLead(kind)) + items).joinToString(" ").lowercase()
      for (bad in listOf("nivel", "rendimiento", "capacidad", "puntaje", "edad mental", "cognitiv", "cerebro", "entrenamiento", "precio", "\$", "ranking", "velocidad", "percentil", "entendido")) assertFalse("la hoja no dice «$bad»", bad in all)
      assertTrue("cada línea es una frase completa", items.all { it.endsWith(".") })
      assertTrue("y el encabezado cierra con dos puntos", PlusAccess.sheetLead(kind).endsWith(":"))
    }
    assertEquals("la preferencia es pasajera: otro archivo que el del perfil", "plus_access", PlusAccess.PREFS)
  }

  @Test
  fun `la hoja solo promete la comparacion cuando la build la tiene`() {
    assertEquals("sin comparación (un juego sin tabla), solo lo que hay detrás: contigo", PlusAccess.SHEET_LEAD, PlusAccess.sheetLead(Comparison.Kind.NONE))
    assertTrue("con personas reales (el día que haya servidor) las líneas hablan de personas de tu edad", "personas de tu edad" in PlusAccess.sheetItems(Comparison.Kind.PEOPLE)[0])
    assertFalse("sin ella ninguna línea habla de otras personas", PlusAccess.sheetItems(Comparison.Kind.NONE).any { "personas" in it || "edad" in it || "división" in it || "divisiones" in it })
    assertTrue("con personas sí: el lugar y la escalera", PlusAccess.sheetItems(Comparison.Kind.PEOPLE).let { l -> "lugar" in l[0] && "escalera" in l[1] })
  }

  @Test
  fun `con la referencia estimada la hoja la nombra y nunca la llama personas ni promete pasar a personas reales`() {
    val items = PlusAccess.sheetItems(Comparison.Kind.ESTIMATED)
    assertEquals("Tu lugar en una referencia estimada para tu edad: qué % de ella superas y en qué división estás.", items[0])
    assertEquals("La escalera de las cinco divisiones, con su primer acierto, y tus semanas seguidas arriba de la mitad.", items[1])
    assertEquals("Todas tus marcas: el récord, las últimas 10 y tu ritmo en los 2 minutos.", items[2])
    assertEquals("lo gratis primero y después lo que agrega Plus", "Gratis: tu división de la semana. Con Nubi Plus, además:", PlusAccess.sheetLead(Comparison.Kind.ESTIMATED))
    assertEquals("con personas (futuro), la misma entrada", "Gratis: tu división de la semana. Con Nubi Plus, además:", PlusAccess.sheetLead(Comparison.Kind.PEOPLE))
    assertEquals("la razón bajo las líneas", "La referencia no son personas: Nubi no junta datos de nadie.", PlusAccess.SHEET_NOTE_ESTIMATED)
    assertEquals("de 13 a 17 años, la hoja lo dice", "Una página con todas tus marcas. De 13 a 17 años se comparan solo contigo:", PlusAccess.sheetLead(Comparison.Kind.NONE, minor = true))
    assertEquals("pero con comparación, minor no cambia la entrada", PlusAccess.sheetLead(Comparison.Kind.ESTIMATED), PlusAccess.sheetLead(Comparison.Kind.ESTIMATED, minor = true))
    assertTrue("la escalera y las semanas siguen", "escalera" in items[1])
    val all = (listOf(PlusAccess.sheetLead(Comparison.Kind.ESTIMATED)) + items).joinToString(" ").lowercase()
    for (bad in listOf("personas", "luego", "pronto", "cuando haya", "después", "más adelante", "reales", "servidor")) assertFalse("la hoja de la referencia no dice «$bad»", bad in all)
  }
}
