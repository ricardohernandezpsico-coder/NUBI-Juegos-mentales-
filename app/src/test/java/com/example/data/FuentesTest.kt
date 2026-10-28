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

  @Test
  fun todos_los_DOI_tienen_forma_de_DOI_y_ninguno_se_repite() {
    val dois = Fuentes.byGame.values.map { it.doi } + Fuentes.method.map { it.doi } + Fuentes.simons.doi          // todos los DOI que muestra la pantalla
    assertEquals("17 juegos + 3 del método + Simons", 21, dois.size)
    assertEquals("ningún DOI repetido", dois.size, dois.toSet().size)
    for (d in dois) assertTrue("«$d» no tiene la forma 10.xxxx/…", Regex("""10\.\d{4,9}/\S+""").matches(d))
    for (f in Fuentes.byGame.values) assertEquals("https://doi.org/${f.doi}", f.url)
    for (e in Fuentes.method + Fuentes.simons) assertEquals("https://doi.org/${e.doi}", e.url)
  }

  @Test
  fun cada_cita_es_autor_y_ano_sin_titulo_en_ingles_ni_revista() {
    val cites = Fuentes.byGame.values.map { it.cite } + Fuentes.method.map { it.cite } + Fuentes.simons.cite
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
