package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Assume.assumeTrue
import org.junit.Test
import java.io.File

/**
 * «De dónde sale la referencia» (Tarea 209; `ReferenciaFuentes`): los 10 estudios con su DOI comprobado contra doi.org y Crossref el 1-nov, «Autor, año · N personas», una línea en español sin títulos en inglés, el primer párrafo que dice que no son otras personas
 * y el segundo (el de Opus, sin «interpolación» y sin cifras). Nunca el número de robots.
 */
class ReferenciaFuentesTest {
  /** Los DOI de `auditoria-fable/claridad-plus-y-ayuda.md` §1.1.1, comprobados el 1-nov (el de Cerella 1985, el único sin confirmar, resuelve a «Information processing rates in the elderly»). */
  private val expectedDois = listOf(
    "10.1037/0882-7974.21.1.62", "10.1093/geronb/57.2.p101", "10.1016/j.cogpsych.2009.09.001", "10.1037/0033-2909.98.1.67", "10.1037/0033-295x.97.4.475", "10.1037/0033-295x.103.3.403",
    "10.1177/1073191105283427", "10.1080/138255890968187", "10.3389/fpsyg.2013.00012", "10.1111/sjop.12008"
  )

  @Test
  fun son_diez_estudios_con_su_DOI_y_su_enlace_y_ninguno_se_repite() {
    assertEquals(expectedDois, ReferenciaFuentes.studies.map { it.doi })
    assertEquals(expectedDois.size, expectedDois.toSet().size)
    for (e in ReferenciaFuentes.studies) {
      assertTrue("«${e.doi}» no tiene la forma 10.xxxx/…", Regex("""10\.\d{4,9}/\S+""").matches(e.doi))
      assertEquals("https://doi.org/${e.doi}", e.url)
    }
  }

  @Test
  fun los_estudios_van_en_cuatro_grupos_en_este_orden_y_el_ultimo_no_tiene_losas() {
    assertEquals(listOf("La rapidez con la edad", "Tinta o Palabra", "Rescate relámpago", "¿Verdad o disparate? y Acoplamiento"), ReferenciaFuentes.groups.map { it.title })
    assertEquals(listOf(6, 2, 2, 0), ReferenciaFuentes.groups.map { it.studies.size })
    val last = ReferenciaFuentes.groups.last()
    assertEquals("Sus tiempos salen de los mismos estudios de rapidez: la referencia no tiene un estudio propio de cada juego.", last.line)
    assertTrue("los otros tres no traen línea", ReferenciaFuentes.groups.dropLast(1).all { it.line == null })
  }

  @Test
  fun cada_cita_es_autor_ano_y_cuantas_personas_sin_titulo_en_ingles_ni_revista() {
    for (e in ReferenciaFuentes.studies) {
      assertTrue("«${e.cite}» no es «Autor, año · …»", Regex("""[A-ZÁÉÍÓÚ][^·]*, (19|20)\d\d · .+""").matches(e.cite))
      assertFalse("«${e.cite}» trae inglés", Regex("""\b(the|of|and|brain|cognitive|journal|psychology)\b""", RegexOption.IGNORE_CASE).containsMatchIn(e.cite + " " + e.line))
      assertTrue("«${e.line}» es una frase en español de punto final", e.line.endsWith(".") && e.line.length in 20..110)
    }
    // lo que cada estudio dice de cuántas personas tuvo (PubMed, 1-nov)
    val cites = ReferenciaFuentes.studies.map { it.cite }
    assertTrue(cites.contains("Der y Deary, 2006 · 7.130 adultos"))
    assertTrue(cites.contains("Hultsch y otros, 2002 · 862 personas de 17 a 94 años"))
    assertTrue(cites.contains("Van der Elst y otros, 2006 · 1.856 personas de 24 a 81 años"))
    assertTrue(cites.contains("Brockmole y Logie, 2013 · 55.753 personas de 8 a 75 años"))
    assertTrue(cites.contains("Habekost y otros, 2013 · 33 personas de 69 a 87 años"))
    assertEquals("las tres revisiones lo dicen", 3, cites.count { "revisión" in it })
  }

  @Test
  fun los_dos_parrafos_dicen_lo_que_decidieron_Fable_y_Opus_sin_cifras_ni_interpolacion() {
    assertEquals(
      "Te comparas con una referencia estimada para tu edad, no con otras personas. Nubi no junta datos de nadie: tus marcas se quedan en tu teléfono.",
      ReferenciaFuentes.INTRO
    )
    assertEquals(
      "La referencia se arma con estudios publicados que midieron cómo cambian con la edad la rapidez y la exactitud en tareas parecidas, y con un programa que jugó el mismo Contrarreloj en un computador con esos números. Cada estudio dice cuántas personas tuvo.",
      ReferenciaFuentes.INTRO_2
    )
    assertEquals(
      "Los números de cada edad se calcularon a partir de esos estudios; ninguno usó los juegos de Nubi. El ritmo del programa se ajustó con las primeras partidas de quienes probaron Nubi antes de salir.",
      ReferenciaFuentes.ABOUT
    )
    for (t in listOf(ReferenciaFuentes.INTRO, ReferenciaFuentes.INTRO_2, ReferenciaFuentes.ABOUT)) assertFalse("«$t» trae una cifra (ni robots ni cuántos probaron)", t.any { it.isDigit() })
    assertFalse("sin «interpolación»", ReferenciaFuentes.ABOUT.lowercase().contains("interpol"))
    assertEquals("De dónde sale la referencia", ReferenciaFuentes.TITLE)
    assertEquals("De dónde sale la referencia del Contrarreloj ›", ReferenciaFuentes.FUENTES_ROW)
  }

  @Test
  fun nunca_se_dice_el_numero_de_robots_ni_una_promesa_de_pasar_a_personas_ni_una_palabra_que_no_va() {
    val everything = listOf(ReferenciaFuentes.TITLE, ReferenciaFuentes.FUENTES_ROW, ReferenciaFuentes.INTRO, ReferenciaFuentes.INTRO_2, ReferenciaFuentes.ABOUT, ReferenciaFuentes.SHARED_LINE) +
      ReferenciaFuentes.groups.map { it.title } + ReferenciaFuentes.studies.flatMap { listOf(it.cite, it.line) }
    for (t in everything) {
      val low = t.lowercase()
      for (bad in listOf("robot", "2.000", "2000", "nubi plus", "cerebro", "cognitiv", "entren", "capacidad", "edad mental", "ranking", "velocidad", "percentil", "luego", "pronto", "cuando haya", "más adelante", "servidor"))
        assertFalse("«$t» dice «$bad»", bad in low)
    }
  }

  @Test
  fun los_diez_estudios_son_los_que_nombra_AgeReference_y_estan_en_el_documento() {
    val ageReference = File("../unity/NeuroVidaCore/Assets/Scripts/Games/Tests/AgeReference.cs")
    assumeTrue("solo con el repositorio completo", ageReference.exists())
    val src = ageReference.readText()
    for (author in listOf("Cerella 1985", "Myerson 1990", "Salthouse 1996", "Der y Deary 2006", "Hultsch 2002", "Ratcliff 2010", "Van der Elst 2006", "Troyer 2006", "Brockmole y Logie 2013", "Habekost 2013"))
      assertTrue("AgeReference.cs no nombra a $author", src.contains(author))
    val doc = File("../docs/en-que-se-basa-nubi.md")
    assumeTrue(doc.exists())
    val text = doc.readText()
    for (doi in expectedDois) assertTrue("docs/en-que-se-basa-nubi.md no lista $doi", text.contains(doi))
  }
}
