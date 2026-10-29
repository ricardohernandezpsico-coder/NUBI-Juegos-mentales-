package com.example.data

import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * «En qué se basa Nubi» (Tarea 185; `Fuentes`, `NubiContact`): los 17 juegos de la app traen su fuente con un DOI de verdad, la misma cita corta que ya dice «Sobre este juego», una línea en español sin títulos en inglés, y el enlace abre el navegador sin permiso de internet.
 * Los 24 DOI se comprobaron contra doi.org y Crossref el 28-oct (`docs/en-que-se-basa-nubi.md`); aquí se vigila su forma y que no se repitan.
 */
class FuentesTest {
  private val active = GameRegistry.allGames.filter { !GameRegistry.isRetired(it.id) }

  @Test
  fun cada_juego_de_la_app_tiene_su_fuente_y_solo_los_de_la_app() {
    assertEquals(17, active.size)
    assertEquals("una fuente por juego de la app, en su orden", active.map { it.id }.toSet(), Fuentes.byGame.keys)
    for (g in active) assertEquals(g.id, Fuentes.byGame.getValue(g.id).gameId)
  }

  @Test
  fun la_cita_de_cada_juego_es_la_misma_que_dice_Sobre_este_juego() {
    for (g in active) {
      val (_, source) = FinalesConSentido.sheetFor(g.id)!!
      assertEquals("«Fuente: …» de ${g.title}", source, "Fuente: ${Fuentes.byGame.getValue(g.id).cite}")
    }
  }

  /** Los estudios EXTRA que muestra la pantalla (Tarea 186): los «Más estudios» de cada juego, los grupos de «Cómo está hecha Nubi» (con el método de siempre) y los de «Lo que no decimos» (con Simons 2016). */
  private val extras: List<Estudio> = FuentesMas.byGame.values.flatten() + FuentesMas.mechanisms.flatMap { it.studies } + FuentesMas.noDecir

  @Test
  fun todos_los_DOI_tienen_forma_de_DOI_y_ninguno_se_repite() {
    val dois = Fuentes.byGame.values.map { it.doi } + extras.map { it.doi }          // todos los DOI que muestra la pantalla
    assertEquals("17 fuentes principales + 37 «Más estudios» + 32 de «Cómo está hecha Nubi» (3 del método y 29 nuevos) + 4 de «Lo que no decimos»", 90, dois.size)
    assertEquals("ningún DOI repetido", dois.size, dois.toSet().size)
    for (d in dois) assertTrue("«$d» no tiene la forma 10.xxxx/…", Regex("""10\.\d{4,9}/\S+""").matches(d))
    assertTrue("la forma canónica del DOI, sin barra doble", dois.none { "//" in it })
    for (f in Fuentes.byGame.values) assertEquals("https://doi.org/${f.doi}", f.url)
    for (e in extras) assertEquals("https://doi.org/${e.doi}", e.url)
  }

  @Test
  fun cada_cita_es_autor_y_ano_sin_titulo_en_ingles_ni_revista() {
    val cites = Fuentes.byGame.values.map { it.cite } + extras.map { it.cite }
    for (c in cites) {
      assertTrue("«$c» no es «Autor, año»", Regex("""[A-ZÁÉÍÓÚ].*, (19|20)\d\d""").matches(c))
      assertFalse("«$c» trae una palabra en inglés", Regex("""\b(the|of|and|brain|cognitive|journal|psychology)\b""", RegexOption.IGNORE_CASE).containsMatchIn(c))
    }
    // la línea de qué tomamos: una frase corta en español, de punto final, sin títulos en inglés
    for (f in Fuentes.byGame.values) {
      assertTrue("«${f.taken}» no es una frase corta", f.taken.length in 20..90 && f.taken.endsWith("."))
      assertFalse("«${f.taken}» trae inglés", Regex("""\b(the|of|and|training|brain)\b""", RegexOption.IGNORE_CASE).containsMatchIn(f.taken))
    }
  }

  @Test
  fun la_linea_de_Constelaciones_dice_lo_que_dice_el_estudio_y_las_17_estan_aprobadas() {
    // Ricardo, 28-oct: «…lo que más cambia con los años» (Old y Naveh-Benjamin, 2008, trata del déficit de la memoria asociativa con la edad), no «…lo que más atención pide»
    assertEquals("Recordar qué va con qué es lo que más cambia con los años.", Fuentes.byGame.getValue("parejas").taken)
    assertFalse(Fuentes.byGame.values.any { it.taken.contains("atención pide") })
    val src = File("src/main/java/com/example/data/Fuentes.kt").readText()
    assertTrue("el archivo dice que Ricardo aprobó las 17 líneas el 28-oct", src.contains("APROBÓ Ricardo el 28-oct"))
  }

  // ------------------------------------------------------------------ Tarea 186: Más estudios, Cómo está hecha Nubi y Lo que no decimos

  @Test
  fun cada_juego_tiene_sus_Mas_estudios_sin_repetir_el_principal() {
    assertEquals("los 17 juegos de la app", active.map { it.id }.toSet(), FuentesMas.byGame.keys)
    for (g in active) {
      val more = FuentesMas.byGame.getValue(g.id)
      assertTrue("${g.id} tiene al menos un estudio más", more.isNotEmpty())
      assertFalse("${g.id} no repite su estudio principal", more.any { it.doi == Fuentes.byGame.getValue(g.id).doi })
    }
    assertEquals("37 estudios extra en total", 37, FuentesMas.byGame.values.sumOf { it.size })
    assertEquals(
      mapOf("parejas" to 3, "secuencia" to 2, "bodega" to 1, "correo" to 3, "stroop" to 3, "piloto" to 2, "freno" to 2, "satelites" to 3, "radar" to 2, "calculo" to 2, "engranajes" to 2, "acoplamiento" to 2, "aterrizaje" to 1, "anagramas" to 3, "meteoros" to 2, "disparate" to 2, "intrusa" to 2),
      FuentesMas.byGame.mapValues { it.value.size }
    )
    assertEquals("Más estudios (3)", FuentesMas.moreLabel(3))
  }

  @Test
  fun Como_esta_hecha_Nubi_tiene_sus_diez_grupos_con_su_frase_y_sus_fuentes() {
    assertEquals("Cómo está hecha Nubi", FuentesMas.MECHANISMS_TITLE)
    assertEquals(
      listOf("La dificultad se acomoda a ti", "Más tiempo desde los 65", "Partidas cortas, varios días", "Tu meta y tu racha", "Saber cómo te fue", "Tú eliges", "Solo contigo", "Una partida es una partida", "Fácil de ver y de tocar", "Con elementos de juego"),
      FuentesMas.mechanisms.map { it.title }
    )
    assertEquals(listOf(3, 3, 3, 5, 2, 2, 4, 2, 5, 3), FuentesMas.mechanisms.map { it.studies.size })
    assertEquals("32 en total: los 3 de la T185 (Levitt, Kaernbach y Salthouse) y 29 nuevos", 32, FuentesMas.mechanisms.sumOf { it.studies.size })
    assertEquals("Estudios (3)", FuentesMas.studiesLabel(3))
    for (m in FuentesMas.mechanisms) assertTrue("«${m.title}» dice en una frase lo que hace Nubi", m.text.length in 30..140 && m.text.endsWith("."))
    // «Cómo se ajusta la dificultad» se fundió con «La dificultad se acomoda a ti»: el método de siempre (Levitt y Kaernbach) va primero, con Wilson 2019 al final y siempre «en modelos de aprendizaje»
    val dificultad = FuentesMas.mechanisms.first { it.key == "dificultad" }
    assertEquals(Fuentes.method, dificultad.studies.take(2))
    assertEquals(listOf("Levitt, 1971", "Kaernbach, 1991"), Fuentes.method.map { it.cite })
    // Salthouse 1996 pasó a «Más tiempo desde los 65» (Opus, 28-oct) y va primero
    val tiempo = FuentesMas.mechanisms.first { it.key == "mas_tiempo" }
    assertEquals(Fuentes.salthouse, tiempo.studies.first())
    assertEquals("Salthouse, 1996", Fuentes.salthouse.cite)
    assertFalse("Salthouse ya no está en el grupo de la dificultad", dificultad.studies.any { it.doi == Fuentes.salthouse.doi })
    assertEquals("Wilson y otros, 2019", dificultad.studies.last().cite)
    assertTrue(dificultad.studies.last().line.startsWith("En modelos de aprendizaje"))
    assertEquals(Fuentes.METHOD_TEXT, dificultad.text)
    // los títulos hablan como la app
    for (m in FuentesMas.mechanisms) for (w in listOf("cerebro", "cognitiv", "entren")) assertFalse("«${m.title}» dice «$w»", w in m.title.lowercase())
  }

  @Test
  fun las_lineas_de_Ng_y_de_Gates_son_las_que_aprobo_Opus() {
    // Opus, 28-oct: Ng sin la palabra «salud» y Gates sin el «beneficios» ambiguo
    val ng = extras.first { it.cite == "Ng y otros, 2012" }
    assertEquals("En 184 conjuntos de datos, sentir libertad de elegir se asoció con más ganas propias de mantener un hábito; es una asociación.", ng.line)
    assertFalse(ng.line.contains("salud"))
    val gates = FuentesMas.noDecir.first { it.cite == "Gates y otros, 2020" }
    assertEquals("En 8 ensayos con personas de 65 años o más, los programas de ejercicios en pantalla mostraron beneficios pequeños e inciertos, que no se mantuvieron al año.", gates.line)
  }

  @Test
  fun Lo_que_no_decimos_suma_tres_revisiones_a_Simons() {
    assertEquals(listOf("Simons y otros, 2016", "Melby-Lervåg y otros, 2016", "Sala y otros, 2018", "Gates y otros, 2020"), FuentesMas.noDecir.map { it.cite })
    assertEquals(Fuentes.simons, FuentesMas.noDecir.first())
    assertTrue("cada una con su línea", FuentesMas.noDecir.all { it.line.isNotBlank() })
  }

  @Test
  fun cada_linea_extra_es_una_frase_corta_en_espanol_sin_promesas() {
    for (e in extras) {
      assertTrue("«${e.line}» no es una frase corta (30 a 160 letras, de punto final)", e.line.length in 30..160 && e.line.endsWith("."))
      assertFalse("«${e.line}» trae inglés", Regex("""\b(the|of|and|training|brain|cognitive)\b""", RegexOption.IGNORE_CASE).containsMatchIn(e.line))
      val low = e.line.lowercase()
      for (w in listOf("cognitiv", "entren", "cerebro", "neurona", "previene", "fortalece", "mejora tu", "sesión", "sesiones", "bienestar", "estimulaci", "percentil", "garantiz", "protege", "mejorará", "demencia", "alzheimer", "alzhéimer"))
        assertFalse("«${e.line}» dice «$w»", w in low)
      assertTrue("«${e.line}» lleva signos sueltos o flechas", e.line.none { it in "→←↑↓+−×%" })
    }
  }

  @Test
  fun la_app_muestra_exactamente_las_filas_si_de_las_tablas_de_bibliografia_y_ninguna_solo_interno() {
    // docs/bibliografia: copia de las dos tablas que Ricardo aprobó el 28-oct (101 referencias, confirmadas en PubMed). Entran las filas cuya última columna empieza con «sí» (también «sí, con matiz»); las «solo interno» NO. Los 17 estudios principales, el método y Simons ya estaban desde la Tarea 185.
    class Row(val cite: String, val doi: String, val yes: Boolean)
    val rows = listOf("biblio_juegos.md", "biblio_mecanismos.md").flatMap { name ->
      File("../docs/bibliografia/$name").readLines().map { it.trim() }.filter { it.startsWith("|") && !it.startsWith("|---") && !it.startsWith("| Cita corta") }.map { line ->
        val cells = line.trim('|').split("|").map { it.trim() }
        assertEquals("«$line» no tiene 8 columnas", 8, cells.size)
        val doi = Regex("""\]\(https://doi\.org/(\S+)\)$""").find(cells[4])!!.groupValues[1].replace("//", "/")
        Row(cells[0].replace(Regex("""\s*\(.*\)$"""), ""), doi, cells[7].replace("*", "").trim().lowercase().startsWith("sí"))
      }
    }
    assertEquals("las dos tablas traen 101 referencias", 101, rows.size)
    val already = (Fuentes.byGame.values.map { it.doi } + Fuentes.method.map { it.doi } + Fuentes.salthouse.doi + Fuentes.simons.doi).toSet()
    val yes = rows.filter { it.yes }
    assertEquals("69 marcadas «sí»", 69, yes.size)
    assertEquals("la pantalla suma EXACTAMENTE las 69 filas «sí»", yes.map { it.doi }.toSet(), extras.map { it.doi }.toSet() - already)
    val no = rows.filter { !it.yes }.map { it.doi }.toSet()
    assertTrue("ninguna «solo interno» está en la pantalla (salvo las que ya estaban desde la Tarea 185)", (extras.map { it.doi }.toSet() - already).none { it in no })
    // y la cita de la pantalla es la de la tabla (sin «en línea» ni «vol.»)
    val appCite = extras.associate { it.doi to it.cite }
    for (r in yes) assertEquals("la cita de ${r.doi}", r.cite, appCite[r.doi])
  }

  @Test
  fun el_encuadre_no_promete_nada() {
    assertEquals("Nubi no promete mejorar nada: es un juego.", Fuentes.PROMISE)
    assertTrue(Fuentes.NO_DECIR_TEXT.contains("no está demostrado") && Fuentes.NO_DECIR_TEXT.contains("Nubi no lo promete"))
    assertEquals("Simons y otros, 2016", Fuentes.simons.cite)
    // el nombre de la sección es el que eligió Ricardo, no «La ciencia de Nubi»
    val screen = File("src/main/java/com/example/ui/screens/FuentesScreen.kt").readLines().filter { l -> l.trim().let { !it.startsWith("//") && !it.startsWith("*") && !it.startsWith("/*") } }.joinToString("\n")
    assertTrue(screen.contains("\"En qué se basa Nubi\"") && !screen.contains("La ciencia"))
  }

  @Test
  fun el_enlace_abre_el_navegador_y_la_app_sigue_sin_permiso_de_internet() {
    val screen = File("src/main/java/com/example/ui/screens/FuentesScreen.kt").readText()
    assertTrue("ACTION_VIEW hacia el navegador, atrapando la falta de uno", screen.contains("Intent.ACTION_VIEW") && screen.contains("ActivityNotFoundException"))
    val manifest = File("src/main/AndroidManifest.xml").readText()
    assertFalse("la app no pide internet (abrir un enlace lo hace el navegador)", manifest.contains("android.permission.INTERNET"))
  }

  @Test
  fun el_contacto_no_tiene_correo_todavia_y_la_losa_lo_dice() {
    assertNull("Ricardo todavía no crea el correo de Nubi", NubiContact.EMAIL)
    assertEquals("Correo por definir", NubiContact.secondLine())
    assertFalse(NubiContact.canWrite())
    assertEquals("hola@ejemplo.org", NubiContact.secondLine("hola@ejemplo.org"))
    assertTrue(NubiContact.canWrite("hola@ejemplo.org"))
    assertFalse("un correo en blanco no cuenta", NubiContact.canWrite("  "))
  }
}
