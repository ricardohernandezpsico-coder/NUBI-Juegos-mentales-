package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/** El atlas de La estrella intrusa: guardado, cifras que se leen, medidas con mínimos y las figuras del asset. */
class AtlasTest {
  // ---- guardado

  @Test
  fun `las laminas se guardan con su dia y vuelven iguales`() {
    val plates = linkedMapOf("fruta" to 100, "sirve para cortar" to 101)
    assertEquals("fruta:100;sirve para cortar:101", Atlas.encodePlates(plates))
    assertEquals(plates, Atlas.decodePlates(Atlas.encodePlates(plates)))
    assertEquals(emptyMap<String, Int>(), Atlas.decodePlates("sin dos puntos;:3;x:no"))
    assertEquals(emptyMap<String, Int>(), Atlas.decodePlates(null))
  }

  @Test
  fun `una partida suma laminas nuevas, nombres, reglas por repasar y repasadas`() {
    val s0 = AtlasState(plates = mapOf("fruta" to 99), review = setOf("pez", "vuela"))
    val s1 = Atlas.record(
      s0, newPlates = listOf("fruta", "pez", "ave"), named = listOf("ave", "fantasma"), reviewNew = listOf("flor"), reviewDone = listOf("pez"), today = 100
    )
    assertEquals(mapOf("fruta" to 99, "pez" to 100, "ave" to 100), s1.plates)      // fruta ya estaba: conserva su día
    assertEquals(setOf("ave"), s1.named)                                          // un nombre sin lámina no cuenta
    assertEquals(setOf("vuela", "flor"), s1.review)                               // pez se repasó
  }

  @Test
  fun `las claves que manda Unity se limpian y no se repiten`() {
    assertEquals(listOf("pez", "sirve para cortar"), Atlas.keys(" pez;;sirve para cortar; pez "))
    assertEquals(emptyList<String>(), Atlas.keys(""))
    assertEquals(emptyList<String>(), Atlas.keys(null))
  }

  // ---- lo que se lee

  @Test
  fun `la linea de cifras omite lo que vale cero`() {
    assertNull(Atlas.summary(AtlasState(), 100))
    val s = AtlasState(plates = (1..14).associate { "r$it" to if (it <= 2) 100 else 90 }, review = setOf("a"))
    assertEquals("14 láminas · 2 nuevas hoy · 1 por repasar", Atlas.summary(s, 100))
    assertEquals("14 láminas · 1 por repasar", Atlas.summary(s, 101))
    assertEquals("1 lámina · 1 nueva hoy", Atlas.summary(AtlasState(plates = mapOf("x" to 5)), 5))
    assertEquals("0 láminas · 2 por repasar", Atlas.summary(AtlasState(review = setOf("a", "b")), 5))
  }

  @Test
  fun `las miniaturas son las ultimas ganadas hoy, hasta tres`() {
    val s = AtlasState(plates = linkedMapOf("a" to 7, "b" to 8, "c" to 8, "d" to 8, "e" to 8))
    assertEquals(listOf("e", "d", "c"), Atlas.todayPlates(s, 8))
    assertEquals(emptyList<String>(), Atlas.todayPlates(s, 9))
    assertEquals(4, Atlas.todayCount(s, 8))
  }

  // ---- medidas

  private val seen = listOf(2, 2, 3, 2, 3, 2)        // categorías: tipo 4, uso 3, lugar 2, trampas 5
  private val hits = listOf(2, 2, 1, 2, 1, 1)        // tipo 4/4, uso 1/3, lugar 2/2, trampas 2/5

  @Test
  fun `las categorias solo con 3 rondas o mas y la mas baja marcada`() {
    val rows = Atlas.categoryRows(seen, hits)
    assertEquals(listOf("Tipo de cosa", "Para qué sirve", "Trampas"), rows.map { it.label })     // «lugar» solo tuvo 2 rondas
    assertEquals(listOf(4 to 4, 3 to 1, 5 to 2), rows.map { it.seen to it.hits }.map { it.first to it.second })
    assertEquals("Para qué sirve", rows.single { it.lowest }.label)        // 1 de 3 (33%) contra trampas 2 de 5 (40%)
    assertEquals(33, rows.single { it.lowest }.percent)
  }

  @Test
  fun `con una sola categoria o todas iguales no se marca ninguna`() {
    assertTrue(Atlas.categoryRows(listOf(2, 2, 0, 0, 0, 0), listOf(2, 1, 0, 0, 0, 0)).none { it.lowest })
    assertEquals(1, Atlas.categoryRows(listOf(2, 2, 0, 0, 0, 0), listOf(2, 1, 0, 0, 0, 0)).size)
    assertTrue(Atlas.categoryRows(listOf(3, 0, 3, 0, 3, 0), listOf(2, 0, 2, 0, 2, 0)).none { it.lowest })
    assertEquals(emptyList<Atlas.CategoryRow>(), Atlas.categoryRows(listOf(1, 1, 1, 1, 1, 1), listOf(1, 1, 1, 1, 1, 1)))
    assertEquals(emptyList<Atlas.CategoryRow>(), Atlas.categoryRows(null, null))
    assertEquals(emptyList<Atlas.CategoryRow>(), Atlas.categoryRows(listOf(1, 2), listOf(1, 2)))
  }

  @Test
  fun `las dos frases de trampas salen de la misma cuenta`() {
    // 8 trampas, 5 resistidas: la barra «Trampas» dice 5 de 8 y la frase «te engañaron 3 de 8»: la misma cuenta
    val s = listOf(0, 0, 0, 0, 4, 4)
    val h = listOf(0, 0, 0, 0, 2, 3)
    assertEquals("Las trampas te engañaron 3 de 8", Atlas.trapFooledLine(s, h))
    assertTrue(Atlas.trapReading(s, h)!!.contains("TIPO"))
    // la fila de la barra dice lo mismo
    val row = Atlas.categoryRows(s, h).single { it.label == "Trampas" }
    assertEquals(8 to 5, row.seen to row.hits)
    assertEquals("Las trampas te engañaron ${row.seen - row.hits} de ${row.seen}", Atlas.trapFooledLine(s, h))
  }

  @Test
  fun `con menos de 4 trampas no se habla de ellas, y si no engañaron no hay frase de engano`() {
    assertNull(Atlas.trapFooledLine(listOf(0, 0, 0, 0, 2, 1), listOf(0, 0, 0, 0, 1, 1)))
    assertNull(Atlas.trapReading(listOf(0, 0, 0, 0, 2, 1), listOf(0, 0, 0, 0, 1, 1)))
    val s = listOf(0, 0, 0, 0, 3, 2)
    val h = listOf(0, 0, 0, 0, 3, 2)
    assertNull(Atlas.trapFooledLine(s, h))
    assertTrue(Atlas.trapReading(s, h)!!.startsWith("Resististe todas"))
  }

  @Test
  fun `el bonus, los totales, la marca y la rapidez`() {
    assertEquals("Nombraste 3 de 5", Atlas.bonusLine(5, 3))
    assertNull(Atlas.bonusLine(0, 0))
    assertNull(Atlas.bonusLine(null, null))
    assertEquals(9 to 14, Atlas.totals(seen, hits))
    assertEquals(64.29f, Atlas.mark(seen, hits)!!, 0.01f)
    assertNull(Atlas.mark(listOf(0, 0, 0, 0, 0, 0), listOf(0, 0, 0, 0, 0, 0)))
    assertEquals("Mediana al tocar: 1,8 s", Atlas.speedLine(1800))
    assertNull(Atlas.speedLine(null))
    assertEquals("9 de 14 rondas bien", Atlas.spoken(seen, hits))
  }

  // ---- las figuras del asset (lo que escribe tools/intrusa/figuras.py)

  @Test
  fun `el asset trae las 77 figuras con lo necesario para dibujarlas`() {
    val file = listOf("src/main/assets/${FigureBank.ASSET}", "app/src/main/assets/${FigureBank.ASSET}").map { File(it) }.firstOrNull { it.exists() }
    assertNotNull("no se encontró el asset de figuras", file)
    val figs = FigureBank.parse(file!!.readText())
    assertEquals(77, figs.size)
    for ((rule, f) in figs) {
      assertEquals(rule, f.rule)
      assertTrue(rule, f.points.size in 7..12)
      assertTrue(rule, f.edges.isNotEmpty())
      assertEquals(rule, 4, f.anchors.size)
      assertTrue(rule, f.contour.isNotEmpty())
      assertTrue(rule, f.name.isNotBlank())
      assertFalse(rule, f.points.any { it.x !in 0f..1f || it.y !in 0f..1f })
    }
    assertEquals("La Manzana", figs.getValue("fruta").name)
  }

  @Test
  fun `un json roto no tumba la pantalla`() {
    assertEquals(emptyMap<String, Figure>(), FigureBank.parse("esto no es json".let { """{"figuras": 3}""" }))
    assertEquals(emptyMap<String, Figure>(), FigureBank.parse("""{"figuras":[{"regla":"x"}]}"""))
  }
}
