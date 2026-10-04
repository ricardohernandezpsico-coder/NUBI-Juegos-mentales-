package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «En la punta de la lengua»: lo que manda Unity, las palabras azules que vuelven y lo que se lee al final. */
class PuntaTest {
  // ---- lo que manda Unity

  @Test
  fun `las palabras llegan limpias y sin repetir`() {
    assertEquals(listOf("búho", "faro"), Punta.words(" búho;;faro ; búho "))
    assertEquals(emptyList<String>(), Punta.words(""))
    assertEquals(emptyList<String>(), Punta.words(null))
  }

  @Test
  fun `cada palabra llega con su lucero, en el orden jugado`() {
    val e = Punta.entries("reloj:o;búho:a;faro:p;nido:c")
    assertEquals(listOf("reloj", "búho", "faro", "nido"), e.map { it.word })
    assertEquals(listOf(PuntaTier.SOLO, PuntaTier.VISTA, PuntaTier.PISTA, PuntaTier.LETRAS), e.map { it.tier })
  }

  @Test
  fun `lo que no se entiende se salta`() {
    val e = Punta.entries("reloj:o;sinlucero;:o;faro:x;mesa:oo;luna:p")
    assertEquals(listOf("reloj", "luna"), e.map { it.word })
    assertEquals(emptyList<PuntaEntry>(), Punta.entries(null))
  }

  @Test
  fun `los luceros tienen su nombre en texto`() {
    assertEquals("sola", PuntaTier.SOLO.label)
    assertEquals("con pista", PuntaTier.PISTA.label)
    assertEquals("con las letras", PuntaTier.LETRAS.label)
    assertEquals("te la mostró Nubi", PuntaTier.VISTA.label)
  }

  // ---- las azules que vuelven

  @Test
  fun `las azules de hoy se suman a las de antes y las encontradas dejan de esperar`() {
    val next = Punta.mergePending(pending = listOf("faro", "nido"), blue = listOf("búho", "mesa"), cleared = listOf("nido"))
    assertEquals(listOf("faro", "búho", "mesa"), next)
  }

  @Test
  fun `una azul mostrada otra vez no se repite y pasa al final`() {
    assertEquals(listOf("nido", "faro"), Punta.mergePending(listOf("faro", "nido"), listOf("faro"), emptyList()))
  }

  @Test
  fun `una pendiente encontrada solo con las letras sigue esperando`() {
    // «cleared» solo trae las encontradas solas o con 1-2 ayudas: la de las letras no está ahí
    assertEquals(listOf("faro"), Punta.mergePending(listOf("faro"), emptyList(), emptyList()))
  }

  @Test
  fun `se guardan 20 como mucho y se sueltan las mas viejas`() {
    val old = (1..18).map { "v$it" }
    val next = Punta.mergePending(old, listOf("a", "b", "c", "d"), emptyList())
    assertEquals(Punta.MAX_PENDING, next.size)
    assertEquals(listOf("v3", "v4"), next.take(2))
    assertEquals(listOf("a", "b", "c", "d"), next.takeLast(4))
    assertFalse(next.contains("v1"))
  }

  @Test
  fun `lo guardado vuelve igual y se manda a Unity separado por punto y coma`() {
    val words = listOf("búho", "faro")
    assertEquals("búho;faro", Punta.encode(words))
    assertEquals(words, Punta.words(Punta.encode(words)))
    assertEquals("", Punta.encode(emptyList()))
  }

  // ---- lo que se lee

  @Test
  fun `la cifra es cuantas encontraste por tu cuenta`() {
    assertEquals("Encontraste 5 de 8 por tu cuenta", Punta.headline(5, 8))
    assertEquals("Encontraste 8 de 8 por tu cuenta", Punta.headline(12, 8))     // nunca más que el total
    assertNull(Punta.headline(null, 8))
    assertNull(Punta.headline(3, 0))
    assertEquals(62.5f, Punta.mark(5, 8)!!, 1e-4f)
    assertNull(Punta.mark(null, 8))
    assertNull(Punta.mark(3, 0))
    assertEquals(8, Punta.total(5, 1, 1, 1))
    assertEquals(0, Punta.total(null, null, null, null))
  }

  @Test
  fun `el desglose omite lo que vale cero`() {
    assertEquals("5 solas · 2 con ayuda · 1 mostrada", Punta.breakdown(5, 1, 1, 1))
    assertEquals("1 sola", Punta.breakdown(1, 0, 0, 0))
    assertEquals("3 con ayuda", Punta.breakdown(0, 3, 0, 0))
    assertEquals("2 mostradas", Punta.breakdown(0, 0, 0, 2))
    assertNull(Punta.breakdown(0, 0, 0, 0))
    assertNull(Punta.breakdown(null, null, null, null))
  }

  @Test
  fun `el tiempo solo se dice con tres o mas encontradas solas`() {
    assertEquals("Tardaste 4,2 s en decir «¡La tengo!» en las que salieron solas", Punta.speedLine(4200, 3))
    assertNull(Punta.speedLine(4200, 2))
    assertNull(Punta.speedLine(null, 5))
    assertNull(Punta.speedLine(0, 5))
    assertNull(Punta.speedLine(500_000, 5))
  }

  @Test
  fun `el consejo aparece solo si alguna palabra necesito ayuda, y es un truco`() {
    assertNull(Punta.message(8, 8))
    assertNull(Punta.message(null, 8))
    val text = Punta.message(5, 8)!!
    assertEquals("Si una no sale, piensa en cómo empieza o en otra parecida: suele destrabarla.", ResultAdvice.tipOf(text))
    assertFalse(ResultAdvice.body(text).contains("Truco"))
    assertTrue(ResultAdvice.body(text).startsWith("Que algunas palabras se escondan es normal"))
  }

  @Test
  fun `la nota de las azules solo sale si hubo azules`() {
    assertEquals("Las azules vuelven en otra partida.", Punta.blueNote(2))
    assertNull(Punta.blueNote(0))
    assertNull(Punta.blueNote(null))
  }

  @Test
  fun `el texto para lectores de pantalla es el mismo de la cifra`() {
    assertEquals("Encontraste 5 de 8 por tu cuenta", Punta.spoken(5, 8))
    assertEquals("Sin medida", Punta.spoken(null, 8))
  }

  @Test
  fun `la medida del juego se llama por su nombre nuevo y vive en la lista de medidas`() {
    val def = StarMeasures.defForGame("anagramas")!!
    assertEquals("punta", def.key)
    assertEquals("En la punta de la lengua", StarMeasures.gameNames["anagramas"])
    assertFalse(def.lowerIsBetter)
    assertEquals("63%", def.format(62.5f))
  }
}
