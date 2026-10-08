package com.example

import com.example.model.AppLanguage
import com.example.model.WeeklyChallengeRegistry
import com.example.ui.i18n.getTranslations
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * «Textos honestos» (Etapa 0, 8-oct): lo que la app muestra no promete salud ni habla de «cognitivo», «entrenamiento», «cerebro» o percentiles, y no nombra detalles técnicos (Room, WorkManager). Se revisa el código fuente: ninguna cadena de
 * `app/src/main/java` trae esas palabras (los comentarios y los nombres de código no cuentan). Y la licencia de las tipografías va completa con sus avisos de copyright.
 */
class VisibleTextsGuardTest {
  private val banned = listOf("cognitiv", "cognitif", "entraîn", "entraine", "entrenamiento", "entrená", "entrenar", "entrena ", "estimulaci", "bienestar", "percentil", "cerebro", "neurona", "mejora tu", "fortalece", "previene", "workmanager", " en room", "base de datos room")

  /** Cadenas técnicas que no se ven (nombres internos): se dejan tal cual. */
  private val internalLiterals = setOf("neurovida_daily_cognitive_reminder")

  private val literal = Regex("\"((?:[^\"\\\\\\n]|\\\\.)*)\"")

  @Test
  fun `ninguna cadena del codigo de la app usa el vocabulario que promete salud o es tecnico`() {
    val found = mutableListOf<String>()
    File("src/main/java").walkTopDown().filter { it.extension == "kt" }.forEach { f ->
      f.readLines().forEachIndexed { i, raw ->
        val line = raw.trim()
        if (line.startsWith("//") || line.startsWith("*") || line.startsWith("/*") || line.startsWith("import ")) return@forEachIndexed
        for (m in literal.findAll(raw)) {
          val text = m.groupValues[1]
          if (text in internalLiterals || Regex("[a-z0-9_]+").matches(text)) continue      // nombres internos y etiquetas de prueba (snake_case): no se ven
          val low = text.lowercase()
          val hit = banned.firstOrNull { it in low } ?: continue
          found += "${f.name}:${i + 1} «$text» (por «$hit»)"
        }
      }
    }
    assertTrue("Textos con vocabulario que no va (usar: juego, partida, camino, tu avance, áreas, mente activa):\n" + found.joinToString("\n"), found.isEmpty())
  }

  @Test
  fun `los desafios de la semana y las traducciones no hablan de dominios cognitivos ni de entrenar`() {
    for (c in WeeklyChallengeRegistry.all) {
      val low = (c.title + " " + c.description).lowercase()
      assertFalse(c.key, banned.any { it in low } || "dominios" in low)
    }
    // tuteo, no voseo
    assertEquals("Juega en 3 áreas distintas esta semana", WeeklyChallengeRegistry.all.first { it.key == "dominios3" }.description)
    assertEquals("Juega 4 días distintos esta semana", WeeklyChallengeRegistry.all.first { it.key == "dias4" }.description)
    for (lang in AppLanguage.entries) {
      val t = getTranslations(lang)
      val all = listOf(t.progressTitle, t.progressSubtitle, t.gamesLibrarySubtitle, t.sessionCompletedTitle, t.startDailySession, t.dailySessionTitle, t.trainAnotherRound).joinToString(" ").lowercase()
      assertFalse(lang.name, listOf("cognit", "training", "trainings", "workout", "brain", "entrenamiento", "treino", "treinos", "entraîn", "22 ").any { it in all })
    }
    assertEquals("19 juegos en 4 áreas", getTranslations(AppLanguage.SPANISH).gamesLibrarySubtitle)
    assertEquals("Tu avance", getTranslations(AppLanguage.SPANISH).progressTitle)
  }

  @Test
  fun `la licencia de las tipografias va completa con los avisos de copyright de cada una`() {
    val text = File("src/main/res/raw/ofl.txt").readText()
    assertTrue(text.contains("SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007"))
    assertTrue(text.contains("PERMISSION & CONDITIONS"))
    assertTrue(text.contains("DISCLAIMER"))
    for (aviso in listOf(
      "Copyright 2016 The Fredoka Project Authors",
      "Copyright 2014 The Nunito Project Authors",
      "Copyright 2020 Braille Institute of America, Inc.",
      "Copyright 2018 The Fraunces Project Authors"
    )) assertTrue(aviso, text.contains(aviso))
    // la pantalla lo lee del recurso, no de un string gigante
    val screen = File("src/main/java/com/example/ui/screens/LicensesScreen.kt").readText()
    assertTrue(screen.contains("R.raw.ofl"))
    for (nombre in listOf("Fredoka", "Nunito", "Atkinson Hyperlegible", "Fraunces")) assertTrue(nombre, screen.contains(nombre))
  }

  @Test
  fun `ya no hay gestion de varios perfiles en Ajustes ni en el ViewModel`() {
    val settings = File("src/main/java/com/example/ui/screens/SettingsScreen.kt").readText()
    val vm = File("src/main/java/com/example/viewmodel/NeuroVidaViewModel.kt").readText()
    val repo = File("src/main/java/com/example/data/NeuroVidaRepository.kt").readText()
    for (gone in listOf("btn_add_profile", "profile_chip_", "viewModel.switchProfile", "viewModel.createNewProfile", "viewModel.deleteProfile")) assertFalse(gone, settings.contains(gone))
    for (gone in listOf("fun createNewProfile", "fun switchProfile", "fun deleteProfile")) assertFalse(gone, vm.contains(gone))
    for (gone in listOf("fun createProfile", "fun switchActiveProfile", "fun deleteProfile")) assertFalse(gone, repo.contains(gone))
    // la tabla y su DAO se quedan (sin migración)
    assertTrue(File("src/main/java/com/example/data/local/Daos.kt").readText().contains("interface UserProfileDao"))
  }
}
