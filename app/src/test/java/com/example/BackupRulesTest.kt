package com.example

import java.io.File
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Las reglas de respaldo (qué se copia a la nube o a un teléfono nuevo) tienen que decir lo mismo en las dos versiones
 * de Android y no pueden dejar afuera una preferencia nueva sin que alguien lo decida. Ver backup_rules.xml.
 */
class BackupRulesTest {
  // Estado pasajero: se pierde a propósito al cambiar de teléfono (una partida "fantasma" a medias no sirve).
  private val transient = setOf("paused_game", "game_session", "unity_results", "library_focus")

  private fun xml(name: String) = File("src/main/res/xml/$name").readText()

  private fun includes(text: String, domain: String) =
    Regex("""<include domain="$domain" path="([^"]+)"""").findAll(text).map { it.groupValues[1] }.toSet()

  private fun cloudSection(text: String) = text.substringAfter("<cloud-backup>").substringBefore("</cloud-backup>")
  private fun transferSection(text: String) = text.substringAfter("<device-transfer>").substringBefore("</device-transfer>")

  @Test
  fun `las tres listas de respaldo son iguales`() {
    val old = xml("backup_rules.xml")
    val new = xml("data_extraction_rules.xml")
    val expectedPrefs = includes(old, "sharedpref")
    val expectedDb = includes(old, "database")
    for (section in listOf(cloudSection(new), transferSection(new))) {
      assertEquals(expectedPrefs, includes(section, "sharedpref"))
      assertEquals(expectedDb, includes(section, "database"))
    }
  }

  @Test
  fun `la base de datos y su registro se respaldan juntos`() {
    val db = includes(xml("backup_rules.xml"), "database")
    assertTrue(db.contains("neurovida_database"))
    // Room escribe primero en el registro (-wal): sin él, lo último jugado no llegaría a la copia.
    assertTrue(db.contains("neurovida_database-wal"))
  }

  @Test
  fun `toda preferencia con nombre esta respaldada o marcada como pasajera`() {
    val names = mutableSetOf<String>()
    File("src/main/java").walkTopDown().filter { it.extension == "kt" }.forEach { f ->
      val text = f.readText()
      Regex("""getSharedPreferences\("([a-z_]+)"""").findAll(text).forEach { names += it.groupValues[1] }
      Regex("""PREFS = "([a-z_]+)"""").findAll(text).forEach { names += it.groupValues[1] }
    }
    assertTrue("no encontré ninguna preferencia: ¿cambió la carpeta de trabajo de las pruebas?", names.size >= 8)
    val backedUp = includes(xml("backup_rules.xml"), "sharedpref").map { it.removeSuffix(".xml") }.toSet()
    val undecided = names - backedUp - transient
    assertTrue(
      "Hay preferencias que no están ni respaldadas ni marcadas como pasajeras: $undecided. " +
        "Súmalas a backup_rules.xml y data_extraction_rules.xml, o a la lista `transient` de esta prueba.",
      undecided.isEmpty()
    )
  }

  @Test
  fun `lo pasajero no se respalda`() {
    val backedUp = includes(xml("backup_rules.xml"), "sharedpref").map { it.removeSuffix(".xml") }.toSet()
    assertTrue(backedUp.intersect(transient).isEmpty())
  }
}
