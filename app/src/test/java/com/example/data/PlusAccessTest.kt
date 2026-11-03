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
    // Tarea 215b (Fable A1): la hoja ya no promete lo que la losa crema de la pestaña da gratis (jugar, el récord, la semana y la división)
    assertEquals("Qué porcentaje de la referencia estimada para tu edad superas, y la escalera de las cinco divisiones.", items[0])
    assertEquals("Tu lugar en otras edades y semana a semana, con tus semanas seguidas arriba de la mitad.", items[1])
    assertEquals("Todas tus marcas: las últimas 10 en barras, tu ritmo en los 2 minutos y el récord de las pistas anteriores.", items[2])
    assertEquals("lo gratis primero y después lo que agrega Plus", "Gratis: jugar, tu récord y tu división de la semana. Con Nubi Plus, además:", PlusAccess.sheetLead(Comparison.Kind.ESTIMATED))
    assertEquals("con personas (futuro), la misma entrada", "Gratis: jugar, tu récord y tu división de la semana. Con Nubi Plus, además:", PlusAccess.sheetLead(Comparison.Kind.PEOPLE))
    assertEquals("sin comparación, lo gratis es jugar, el récord y la semana", "Gratis: jugar, tu récord y tu semana. Con Nubi Plus, además:", PlusAccess.sheetLead(Comparison.Kind.NONE))
    assertEquals("y su primera línea ya no promete el récord ni la mejor de la semana", "Tus últimas 10 marcas en barras, con la fecha y el paso de tu récord.", PlusAccess.sheetItems(Comparison.Kind.NONE)[0])
    for (kind in listOf(Comparison.Kind.ESTIMATED, Comparison.Kind.NONE)) assertFalse("la hoja de $kind no vende lo gratis (la división, «en qué división estás», «el récord» a secas, «una página»)", (PlusAccess.sheetItems(kind) + PlusAccess.sheetLead(kind).substringAfter("Con Nubi Plus")).joinToString(" ").lowercase().let { "en qué división estás" in it || "una página" in it || "tu récord, tu mejor marca" in it })
    assertEquals("la razón bajo las líneas", "La referencia no son personas: Nubi no junta datos de nadie.", PlusAccess.SHEET_NOTE_ESTIMATED)
    // Tarea 216: la hoja no habla de edad; de 14 a 17 años es la de cualquiera (ya no existe la entrada «De 13 a 17 años se comparan solo contigo»)
    for (kind in Comparison.Kind.entries) assertFalse("la hoja de $kind no nombra ninguna edad", Regex("\\d+ a \\d+ años").containsMatchIn(PlusAccess.sheetLead(kind) + PlusAccess.sheetItems(kind).joinToString(" ")))
    assertTrue("la escalera (con el porcentaje) y las semanas siguen", "escalera" in items[0] && "semana a semana" in items[1])
    val all = (listOf(PlusAccess.sheetLead(Comparison.Kind.ESTIMATED)) + items).joinToString(" ").lowercase()
    for (bad in listOf("personas", "luego", "pronto", "cuando haya", "después", "más adelante", "reales", "servidor")) assertFalse("la hoja de la referencia no dice «$bad»", bad in all)
  }
}
