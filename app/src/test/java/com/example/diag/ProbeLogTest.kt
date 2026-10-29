package com.example.diag

import com.example.bridge.LoadingFrameProbe
import java.io.File
import kotlin.io.path.createTempDirectory
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test

/**
 * El archivo de las sondas de entrada (Tarea 190b): agrega con la hora, respeta el tope de 256 KB quedándose con lo último, no escribe en release y NO entra en el respaldo; y la línea de la sonda de la animación de la carga.
 */
class ProbeLogTest {
  private lateinit var dir: File

  @Before fun setUp() {
    dir = createTempDirectory("sonda").toFile()
    ProbeLog.install(dir)
  }

  @After fun tearDown() {
    dir.deleteRecursively()
  }

  private val file get() = File(dir, ProbeLog.FILE)

  @Test fun `agrega al final con la hora y no pisa lo anterior`() {
    ProbeLog.write("[sonda-entrada] juego=stroop listo=0,44 s", debug = true)
    ProbeLog.write("[sonda-carga] juego=stroop", debug = true)
    val lines = file.readLines()
    assertEquals(2, lines.size)
    assertTrue(lines[0].matches(Regex("""\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} \[sonda-entrada] juego=stroop listo=0,44 s""")))
    assertTrue(lines[1].endsWith("[sonda-carga] juego=stroop"))
  }

  @Test fun `en release no escribe nada`() {
    ProbeLog.write("no tiene que quedar", debug = false)
    assertFalse(file.exists())
  }

  @Test fun `pasado el tope se queda con lo ultimo y en lineas enteras`() {
    val long = "x".repeat(900)
    repeat(400) { ProbeLog.write("linea $it $long", debug = true) }              // ~360 KB: pasa el tope de 256 KB
    assertTrue("el archivo (${file.length()} bytes) no pasa mucho del tope", file.length() <= ProbeLog.MAX_BYTES + 2000)
    val lines = file.readLines()
    assertTrue("sigue la última línea", lines.last().contains("linea 399"))
    assertFalse("lo más viejo se fue", lines.any { it.contains("linea 0 ") })
    assertTrue("todas las líneas están enteras", lines.all { it.contains(" linea ") && it.endsWith(long) })
  }

  @Test fun `el archivo de sondas no entra en el respaldo`() {
    for (name in listOf("backup_rules.xml", "data_extraction_rules.xml")) {
      val xml = File("src/main/res/xml/$name").readText()
      assertFalse("$name no puede incluir el dominio de archivos", Regex("""domain="file"""").containsMatchIn(xml))
      assertFalse("$name no puede mencionar la sonda", xml.contains("sonda-entrada"))
    }
  }

  @Test fun `la linea de la sonda de la carga trae los cuadros, los percentiles, los nucleos y la pantalla`() {
    val gaps = List(97) { 16.7f } + listOf(40f, 60f, 20f)
    val line = LoadingFrameProbe.format("engranajes", warm = true, gapsMs = gaps, visibleSeconds = 1.234, cores = 8, refreshHz = 60f)
    assertTrue(line, line.startsWith("[sonda-carga] juego=engranajes en caliente"))
    assertTrue(line, line.contains("100 cuadros"))
    assertTrue(line, line.contains("máx 60.0 ms"))
    assertTrue(line, line.contains(">34 ms: 2"))
    assertTrue(line, line.contains("visible 1.23 s"))
    assertTrue(line, line.contains("núcleos 8, pantalla 60 Hz"))
    assertTrue(LoadingFrameProbe.format("stroop", warm = false, gapsMs = emptyList(), visibleSeconds = 0.0, cores = 4, refreshHz = 0f).contains("en frío"))
  }
}
