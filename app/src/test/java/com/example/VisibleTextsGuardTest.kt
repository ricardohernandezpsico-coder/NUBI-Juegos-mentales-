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
  private val banned = listOf("cognitiv", "cognitif", "entraîn", "entraine", "entren", "entrén", "estimulaci", "bienestar", "percentil", "cerebro", "neurona", "mejora tu", "fortalece", "previene", "workmanager", " en room", "base de datos room", "dominio", "domínio")

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

  /**
   * Las raíces que NUNCA pueden verse (regla de CLAUDE.md: nada de «entrenamiento», «cerebro» ni «cognitivo» en lo visible): «entren…» (entrena, entrenamos, entrenar, entrenamiento…), «cerebro» y «cognitiv…».
   * Antes la lista traía «entrena » (con espacio al final), «entrenar», «entrená» y «entrenamiento», y por eso se escapaban «¿Qué entrenamos hoy?» y «Juega. Entrena. Sube de liga.» (11-oct, hallazgo de Fable).
   */
  private val neverVisible = listOf("entren", "entrén", "cerebro", "cognitiv")

  private fun neverVisibleHit(text: String): String? = neverVisible.firstOrNull { it in text.lowercase() }

  /** Los textos que pueden verse en una línea de código: los literales entre comillas (no comentarios ni imports). */
  private fun visibleLiteralsOf(line: String): List<String> {
    val trimmed = line.trim()
    if (trimmed.startsWith("//") || trimmed.startsWith("*") || trimmed.startsWith("/*") || trimmed.startsWith("import ")) return emptyList()
    return literal.findAll(line).map { it.groupValues[1] }.toList()
  }

  @Test
  fun `la guardia atrapa entrenamos, Entrena, cerebro y cognitivo y deja pasar lo permitido`() {
    for (bad in listOf("¿Qué entrenamos hoy?", "¿Con cuál entrenamos tu memoria?", "Juega. Entrena. Sube de liga.", "Entrenamiento diario", "Tu cerebro", "Una función cognitiva", "Entréname")) {
      assertTrue("la guardia no atrapa «$bad»", neverVisibleHit(bad) != null)
      assertTrue("la guardia no atrapa «$bad» dentro de un Text(...)", visibleLiteralsOf("""Text("$bad", fontSize = 20.sp)""").any { neverVisibleHit(it) != null })
    }
    for (good in listOf("¿Qué jugamos hoy?", "¿Qué juego de Memoria jugamos?", "Juega. Avanza. Sube de liga.", "Tu avance", "Mente activa")) {
      assertTrue("la guardia atrapa «$good» sin motivo", neverVisibleHit(good) == null)
    }
    // los comentarios no cuentan
    assertTrue(visibleLiteralsOf("""// ¿Qué "entrenamos" hoy?""").isEmpty())
    assertTrue(visibleLiteralsOf("""   * "Entrena" en el KDoc""").isEmpty())
  }

  @Test
  fun `ningun texto visible de la app dice entren, cerebro ni cognitiv`() {
    val found = mutableListOf<String>()
    // los literales del código de la app (ui, juegos, datos, avisos…), también los de varias líneas entre comillas triples
    File("src/main/java").walkTopDown().filter { it.extension == "kt" }.forEach { f ->
      f.readLines().forEachIndexed { i, raw ->
        for (text in visibleLiteralsOf(raw)) {
          if (text in internalLiterals || Regex("[a-z0-9_]+").matches(text)) continue
          neverVisibleHit(text)?.let { found += "${f.path.removePrefix("src/main/java/")}:${i + 1} «$text» (por «$it»)" }
        }
      }
      Regex("\"\"\"(.*?)\"\"\"", RegexOption.DOT_MATCHES_ALL).findAll(f.readText()).forEach { m ->
        neverVisibleHit(m.groupValues[1])?.let { found += "${f.name}: texto de varias líneas con «$it»" }
      }
    }
    // los textos de los recursos (strings.xml de cualquier idioma)
    File("src/main/res").walkTopDown().filter { it.isFile && it.name.startsWith("strings") && it.extension == "xml" }.forEach { f ->
      Regex(">([^<]+)<").findAll(f.readText()).forEach { m -> neverVisibleHit(m.groupValues[1])?.let { found += "${f.name}: «${m.groupValues[1].trim()}» (por «$it»)" } }
    }
    assertTrue("Textos visibles con «entren…», «cerebro» o «cognitiv…» (usar: juego, partida, camino, tu avance, áreas, mente activa):\n" + found.joinToString("\n"), found.isEmpty())
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
    assertEquals("18 juegos en 4 áreas", getTranslations(AppLanguage.SPANISH).gamesLibrarySubtitle)
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
