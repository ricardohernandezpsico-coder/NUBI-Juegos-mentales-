package com.example

import com.example.data.HealthNotice
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
  private val banned = listOf("cognitiv", "cognitif", "entraîn", "entraine", "entren", "entrén", "estimulaci", "bienestar", "percentil", "cerebro", "neurona", "mejora tu", "fortalece", "previene", "workmanager", " en room", "base de datos room", "dominio", "domínio") + NoLossGuard.literals      // + «sin pérdida» (A7, Tarea 131)

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
          if (text == HealthNotice.TEXT) continue          // la ÚNICA excepción: el aviso de salud (dice que Nubi NO «previene» nada); ver `el aviso de salud ...` más abajo
          val low = text.lowercase()
          val hit = banned.firstOrNull { it in low } ?: continue
          found += "${f.name}:${i + 1} «$text» (por «$hit»)"
        }
      }
    }
    assertTrue("Textos con vocabulario que no va (usar: juego, partida, ruta, tu avance, áreas, mente activa):\n" + found.joinToString("\n"), found.isEmpty())
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

  /**
   * La guardia «sin pérdida» (A7): atrapa lo que usa el miedo a perder y deja pasar lo que tranquiliza. Se prueba la lista contra frases reales y contra el mismo escaneo del código (un literal dentro de un `Text(...)`).
   */
  @Test
  fun `la guardia sin perdida atrapa el miedo a perder y deja pasar lo que tranquiliza`() {
    for (bad in listOf("¡No pierdas tu racha!", "Última oportunidad", "Solo por hoy", "Si no juegas hoy pierdes tu racha", "No te lo pierdas", "Antes de que sea tarde", "Te quedas sin racha")) {
      assertTrue("la guardia no atrapa «$bad»", NoLossGuard.hit(bad, banned) != null)
      assertTrue("la guardia no atrapa «$bad» dentro de un Text(...)", visibleLiteralsOf("""Text("$bad", fontSize = 20.sp)""").any { NoLossGuard.hit(it, banned) != null })
    }
    for (good in listOf("no pierdes nada si dudas", "Descansaste el martes: tu racha sigue.", "antes de que se acabe", "Un día de descanso por semana no la corta.", "Si un día no puedes, tu racha espera.")) {
      assertTrue("la guardia atrapa «$good» sin motivo (por «${NoLossGuard.hit(good, banned)}»)", NoLossGuard.hit(good, banned) == null)
    }
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
    assertTrue("Textos visibles con «entren…», «cerebro» o «cognitiv…» (usar: juego, partida, ruta, tu avance, áreas, mente activa):\n" + found.joinToString("\n"), found.isEmpty())
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
      val all = listOf(t.progressTitle, t.progressSubtitle, t.gamesLibrarySubtitle, t.trainAnotherRound).joinToString(" ").lowercase()
      assertFalse(lang.name, listOf("cognit", "training", "trainings", "workout", "brain", "entrenamiento", "treino", "treinos", "entraîn", "22 ").any { it in all })
    }
    assertEquals("17 juegos en 4 áreas", getTranslations(AppLanguage.SPANISH).gamesLibrarySubtitle)
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

  /**
   * El aviso de salud que exige Google Play (Tarea 121): el texto aprobado por Ricardo, visible en Ajustes y en «Licencias y créditos». La excepción de la guardia es SOLO para este texto y solo por «previene» (se niega, no se promete).
   */
  @Test
  fun `el aviso de salud es el texto aprobado, va en Ajustes y en Licencias y su unica excepcion es previene`() {
    assertEquals(
      "Nubi es un conjunto de juegos para mantener la mente activa. No es un dispositivo médico: no diagnostica, trata, cura ni previene ninguna condición. Ante cualquier duda sobre tu salud, consulta a un profesional.",
      HealthNotice.TEXT
    )
    assertEquals("Aviso de salud", HealthNotice.TITLE)
    assertEquals("de las palabras prohibidas solo «previene», y para negarla", listOf("previene"), banned.filter { it in HealthNotice.TEXT.lowercase() })
    assertTrue("«no previene» se NIEGA en el aviso", "ni previene ninguna condición" in HealthNotice.TEXT)
    assertTrue("sin nada de lo que NUNCA puede verse", neverVisibleHit(HealthNotice.TEXT) == null && neverVisibleHit(HealthNotice.TITLE) == null)
    val settings = File("src/main/java/com/example/ui/screens/SettingsScreen.kt").readText()
    val licenses = File("src/main/java/com/example/ui/screens/LicensesScreen.kt").readText()
    for ((name, src) in listOf("Ajustes" to settings, "Licencias y créditos" to licenses)) {
      assertTrue("$name muestra el título del aviso", src.contains("HealthNotice.TITLE"))
      assertTrue("$name muestra el texto del aviso", src.contains("HealthNotice.TEXT"))
    }
    assertTrue("el aviso de Ajustes tiene su etiqueta de prueba", settings.contains("health_notice"))
    // el aviso viejo (chico y sin título) ya no está
    assertFalse(settings.contains("No es una herramienta de salud"))
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

  // ---- Una palabra por concepto (Tarea 150, auditoría de coherencia del 20-oct, B1): lo que ya se unificó no puede volver a dividirse

  /** Los nombres que YA NO se dicen: la ruta de hoy es «ruta» (no «camino» ni «sesión»), jugar con tiempo es «Con reloj» (no «Contra el reloj», «Reto» ni «Precisión»). Cada uno con la frase que lo caza, no la palabra suelta: «camino» y «reto» tienen otros sentidos legítimos (el camino de la chispa, un reto de los textos antiguos). */
  private val retiredNames = listOf(
    Regex("\\b(tu|mi|su|el|la) (camino|sesión) (de hoy|de cada día|de juegos|diari[oa])", RegexOption.IGNORE_CASE), Regex("camino diario|sesión diaria|sesión de hoy|sesión completa|empezar sesión|continuar sesión|empezar mi camino|armar tu camino", RegexOption.IGNORE_CASE),
    Regex("\\bsesion(es)?\\b|\\bsesión\\b|\\bsesiones\\b", RegexOption.IGNORE_CASE),
    Regex("contra el reloj", RegexOption.IGNORE_CASE), Regex("\\bReto\\b"), Regex("modo reto", RegexOption.IGNORE_CASE), Regex("\\bPrecisión\\b")
  )

  @Test
  fun `ningun texto visible usa los nombres que se unificaron ruta, con reloj y etapa de un juego`() {
    val found = mutableListOf<String>()
    File("src/main/java").walkTopDown().filter { it.extension == "kt" }.forEach { f ->
      f.readLines().forEachIndexed { i, raw ->
        for (text in visibleLiteralsOf(raw)) {
          if (Regex("[a-z0-9_]+").matches(text)) continue
          val low = text
          retiredNames.firstOrNull { it.containsMatchIn(low) }?.let { found += "${f.path.removePrefix("src/main/java/")}:${i + 1} «$text» (por ${it.pattern})" }
        }
      }
    }
    assertTrue("Textos con un nombre que ya se unificó (ruta · Con reloj / Sin reloj):\n" + found.joinToString("\n"), found.isEmpty())
  }

  @Test
  fun `la guarda de nombres unificados atrapa lo viejo y deja pasar lo permitido`() {
    for (bad in listOf("Tu camino de hoy", "tu camino de cada día", "Empezar mi camino", "Para armar tu camino", "¡Sesión de hoy completa!", "Tu sesión de hoy", "Contra el reloj (2 min)", "Reto", "Modo Reto", "Precisión", "7 sesiones")) {
      assertTrue("no atrapa «$bad»", retiredNames.any { it.containsMatchIn(bad) })
    }
    for (good in listOf("Tu ruta de hoy", "Empezar mi ruta", "Repite el camino de la chispa", "Camino corto en 3 de esas 5", "Con reloj (2 minutos)", "Sin reloj", "Al límite", "Ajustes")) {
      assertTrue("atrapa «$good» sin motivo", retiredNames.none { it.containsMatchIn(good) })
    }
  }

  @Test
  fun `el modo mas dificil ya no se llama Experto, que es una etapa del avance`() {
    assertEquals("Al límite", com.example.data.PlayMode.EXPERTO.label)
    assertTrue("«Experto» queda como la 4.ª etapa (Inicio · Aprendiz · Hábil · Experto · Maestro)", "Experto" in com.example.data.Skill.STAGES)
    assertEquals("al límite", com.example.data.RetoChoice.modeWords(com.example.data.PlayMode.EXPERTO))
    assertTrue("ningún nombre de modo coincide con el de una etapa", com.example.data.PlayMode.entries.none { it.label in com.example.data.Skill.STAGES })
    assertEquals("Con reloj (2 minutos)", com.example.data.RetoChoice.timedLabel("calculo"))
  }

  // ---- Una sola escala de avance (Tarea 159; Ricardo: «sin palabras sueltas», una unidad clara: puntos por área)

  @Test
  fun `ningun texto visible dice el avance en la escala vieja de 100`() {
    val found = mutableListOf<String>()
    File("src/main/java").walkTopDown().filter { it.extension == "kt" }.forEach { f ->
      f.readLines().forEachIndexed { i, raw ->
        for (text in visibleLiteralsOf(raw)) {
          if (Regex("\\bde 100\\b").containsMatchIn(text)) found += "${f.path.removePrefix("src/main/java/")}:${i + 1} «$text»"
        }
      }
    }
    assertTrue("El avance se dice en puntos de 0 a 1000 (áreas) o en puntos del juego («180 de 250 puntos»), nunca «N de 100»:\n" + found.joinToString("\n"), found.isEmpty())
  }

  /** Tarea 159 (Opus, 22-oct): los SEGUNDOS valen en TODA la app; «ms» y «milisegundos» no se ven en ningún texto. `data/StarMeasures.kt` guarda la unidad interna («ms») de cada medida, que no se dice nunca (se dice con `MeasurePhrases`). */
  @Test
  fun `ningun texto visible de la app dice milisegundos`() {
    val found = mutableListOf<String>()
    File("src/main/java").walkTopDown().filter { it.extension == "kt" }.forEach { f ->
      if (f.path.replace('\\', '/').endsWith("com/example/data/StarMeasures.kt")) return@forEach
      f.readLines().forEachIndexed { i, raw ->
        for (text in visibleLiteralsOf(raw)) {
          if (Regex("(^|[\\s\\d}])ms\\b").containsMatchIn(text) || "milisegundo" in text.lowercase()) found += "${f.path.removePrefix("src/main/java/")}:${i + 1} «$text»"
        }
      }
    }
    assertTrue("Los tiempos se dicen en segundos («0,17 segundos»), nunca en ms ni en milisegundos:\n" + found.joinToString("\n"), found.isEmpty())
  }

  @Test
  fun `la guardia de milisegundos atrapa lo que debe y deja pasar lo permitido`() {
    for (bad in listOf("Tu vistazo: 170 ms", "Récord: frenaste con el alto a \$it ms", "280 milisegundos", "ms")) {
      val hit = visibleLiteralsOf("""Text("$bad")""").any { Regex("(^|[\\s\\d}])ms\\b").containsMatchIn(it) || "milisegundo" in it.lowercase() }
      assertTrue("la guardia no atrapa «$bad»", hit)
    }
    for (good in listOf("Tu vistazo: 0,17 segundos", "Cambios de nivel", "Mismo tamaño", "1 segundo")) {
      val hit = visibleLiteralsOf("""Text("$good")""").any { Regex("(^|[\\s\\d}])ms\\b").containsMatchIn(it) || "milisegundo" in it.lowercase() }
      assertFalse("la guardia atrapa «$good» sin motivo", hit)
    }
  }

  // ---- Sin abreviaturas ni signos (Tarea 160; Opus, 22-oct, regla de Ricardo «sin palabras sueltas»)

  private val templateBody = Regex("\\$\\{[^}]*\\}")
  private val templateName = Regex("\\$[A-Za-z_][A-Za-z0-9_]*")
  private val printfSpec = Regex("%[-0-9.,]*[dfsSxX]")

  /** Lo que no puede verse en un texto, con su razón; las plantillas `${…}` y `$x` valen un número (lo que serán en pantalla) y los formatos `%.1f` / `%02d` no cuentan. */
  private val looseRules = listOf(
    "«s» en vez de «segundos»" to Regex("\\d\\s?s(?![\\p{L}])|^\\s+s(?![\\p{L}])"),
    "«min» en vez de «minutos»" to Regex("\\bmins?\\b"),
    "«°» en vez de «grados»" to Regex("[°º]"),
    "«pts»" to Regex("\\bpts\\b"),
    "«sem.», «seg.», «aprox.», «máx.» o «mín.»" to Regex("\\b(sem|seg|aprox|máx|mín)\\."),
    "«n.º» en vez de «número»" to Regex("\\b[nN]\\.º|\\bnº"),
    "una flecha" to Regex("[→←↑↓▲▼►◄↗↘]"),
    "«×»" to Regex("×"),
    "un «+N» o «−N»" to Regex("(^|[^\\p{L}\\d])[+−]\\s?\\d"),
    "un «%» sin su «de qué» al lado" to Regex("%(?!\\s?(de|del)\\b)")
  )

  private fun looseHit(text: String): String? {
    val t = printfSpec.replace(templateName.replace(templateBody.replace(text, "0"), "0"), "")
    return looseRules.firstOrNull { it.second.containsMatchIn(t) }?.first
  }

  /**
   * Excepciones, todas justificadas: `DebugTools.kt` (los botones «[Debug]» solo existen en las compilaciones de depuración, no los ve nadie más) y `StarMeasures.kt` (guarda la unidad INTERNA de cada medida —«°», «%», «ms»—, que
   * nunca se dice: lo que se ve sale de `MeasurePhrases`). Y dos cifras con «%» cuyo «de qué» va pegado: la cifra grande de un final B (`FinalModels.kt`; su `unit` empieza con «de …», lo vigila `FinalModelsTest`) y el valor de cada barra del vocabulario
   * (`GameResultScreen.kt`; la frase de arriba dice «el porcentaje de palabras de ese grupo que reconociste»).
   */
  private fun looseException(file: File, text: String): Boolean {
    val path = file.path.replace('\\', '/')
    val big = path.endsWith("games/FinalModels.kt") && text.trim() in setOf("\$it %", "\${it.roundToInt()} %")
    val bar = path.endsWith("games/GameResultScreen.kt") && text.trim() == "\$percent %"
    return path.endsWith("ui/screens/DebugTools.kt") || path.endsWith("data/StarMeasures.kt") || big || bar
  }

  @Test
  fun `ningun texto visible lleva abreviaturas ni signos sueltos`() {
    val found = mutableListOf<String>()
    File("src/main/java").walkTopDown().filter { it.extension == "kt" }.forEach { f ->
      f.readLines().forEachIndexed { i, raw ->
        for (text in visibleLiteralsOf(raw)) {
          if (looseException(f, text)) continue
          looseHit(text)?.let { found += "${f.path.removePrefix("src/main/java/").replace('\\', '/')}:${i + 1} «$text» (por $it)" }
        }
      }
    }
    assertTrue(
      "Sin palabras sueltas (Ricardo): «segundos», «minutos» y «grados» completos; nada de «×», «+N», «−N» ni flechas; un «%» con su «de qué» («9 % de la regla»):\n" + found.joinToString("\n"),
      found.isEmpty()
    )
  }

  @Test
  fun `la guardia de abreviaturas atrapa lo que debe y deja pasar lo permitido`() {
    val malos = listOf(
      "Tu ritmo: \$x s por objeto", "Tardaste 14 s", " s", "0,4 s más", "Con reloj (2 min)", "1 min 30 s", "212 °/s", "45°", "5 pts", "hace 3 sem.", "n.º 3", "más seguidos ×4",
      "+0,4 s", "+\${words.size} palabras", "\$it %", "Tu mejor: 78 %.", "→ siguiente", "▲ 12"
    )
    for (t in malos) assertTrue("la guardia no atrapa «$t»", looseHit(t) != null)
    val buenos = listOf(
      "Tu ritmo: 1,2 segundos por objeto", "Con reloj (2 minutos)", "1 minuto y 30 segundos", "212 grados por segundo", "9 % de la regla", "70 % de las veces", "a 18 % del blanco", "Recordatorio fijado a las %02d:%02d",
      "%.1f", "más seguidos: 4", "0,4 segundos más", "los 3 sectores", "Ya reconoces palabras raras", "Etapa Hábil · 470 de 1000 puntos", "Juego \${i + 1} de 3", "hace 7 semanas", "0,17 segundos"
    )
    for (t in buenos) assertEquals("la guardia atrapa «$t» sin motivo", null, looseHit(t))
  }

  @Test
  fun `lo que Avance dice de las medidas va en segundos y en palabras, sin milisegundos ni abreviaturas`() {
    val found = mutableListOf<String>()
    val bad = listOf(" ms", "°", " dp", "pts", "sem.", "→", "▲", "▼", " seg.")
    for (name in listOf("ui/screens/AvanceAreas.kt", "ui/screens/AvanceAreaPage.kt", "data/MeasurePhrases.kt", "data/LevelLines.kt", "data/AreaPoints.kt")) {
      File("src/main/java/com/example/$name").readLines().forEachIndexed { i, raw ->
        for (text in visibleLiteralsOf(raw)) bad.firstOrNull { it in text }?.let { found += "$name:${i + 1} «$text» (por «$it»)" }
      }
    }
    assertTrue("Textos de Avance con abreviaturas o cifras internas:\n" + found.joinToString("\n"), found.isEmpty())
  }

  @Test
  fun `los subtitulos de los juegos hablan como los buenos y no en jerga de laboratorio`() {
    // Tarea 150 (B5.5): «Memoria de trabajo visoespacial», «Control inhibitorio», «rotación mental», «Sentido numérico»… no los entiende un 60+ y rozaban el vocabulario prohibido
    val jerga = listOf("visoespacial", "inhibitorio", "rotación mental", "sentido numérico", "seguimiento de varios objetos", "memoria de trabajo", "razonamiento espacial", "memoria para lo pendiente")
    for (g in com.example.model.GameRegistry.allGames) {
      val low = g.subtitle.lowercase()
      assertTrue("${g.id}: «${g.subtitle}»", jerga.none { it in low })
      assertTrue("${g.id}: el subtítulo no pasa de ~40 letras para que entre en la ficha: «${g.subtitle}»", g.subtitle.length <= 45)
    }
    val bySubtitle = com.example.model.GameRegistry.allGames.associate { it.id to it.subtitle }
    assertEquals("Repite el camino de la chispa", bySubtitle["secuencia"])
    assertEquals("Frena cuando suena la alarma", bySubtitle["freno"])
    assertEquals("Gira la pieza en tu mente", bySubtitle["acoplamiento"])
    assertEquals("Posa la nave en el número", bySubtitle["aterrizaje"])
    assertEquals("Sigue varios satélites a la vez", bySubtitle["satelites"])
    assertEquals("Acuérdate a tiempo de cada encargo", bySubtitle["correo"])
  }

  @Test
  fun `las frases del Desafio de hoy son las de la spec, con una sola palabra por concepto`() {
    // Tarea 167 (`auditoria-fable/hoy-desafio-y-dia-completo.md` §6.6): «Desafío» es la palabra (no «reto» ni «modo difícil»), y ningún texto baja de lo dicho
    val c = com.example.ui.screens.HomeChallenge("stroop", "Tinta o Palabra", com.example.data.ChallengeState.PENDIENTE)
    assertEquals("Desafío de hoy", com.example.ui.screens.CHALLENGE_LABEL)
    assertEquals("Tinta o Palabra · unos 2 minutos", com.example.ui.screens.challengeSecondLine(c, wide = true, large = false))
    assertEquals("Tinta o Palabra", com.example.ui.screens.challengeSecondLine(c, wide = true, large = true))
    assertEquals("Tinta o Palabra · otra vez", com.example.ui.screens.challengeSecondLine(c.copy(state = com.example.data.ChallengeState.JUGADO), wide = true, large = false))
    assertEquals("Desafío superado: Tinta o Palabra.", com.example.ui.screens.challengeDoneLine("Tinta o Palabra"))
    assertEquals("Mañana hay ruta y Desafío nuevos.", com.example.ui.screens.CHALLENGE_TOMORROW)
    assertEquals("Mañana te espera una ruta nueva.", com.example.ui.screens.ROUTE_TOMORROW)
    assertEquals("Desafío de hoy, Tinta o Palabra, un paso más difícil, unos 2 minutos", com.example.ui.screens.challengeDescription(c))
    val stops = com.example.ui.screens.homeStops(listOf("bodega", "calculo", "intrusa"), 3, AppLanguage.SPANISH)
    val ui = com.example.ui.screens.HomeUi(name = "R", now = 0L, tier = com.example.model.RankTier.fromRating(0), streak = 0, weeklyGoal = 4, lamps = com.example.data.Week.Lamps(List(7) { false }, 6), stops = stops, pausedTitle = null, flightDay = false, hasBaseline = true)
    assertEquals("3 de 3 · ruta completa", com.example.ui.screens.routeMeta(ui))
    val phrases = listOf(com.example.ui.screens.CHALLENGE_LABEL, com.example.ui.screens.CHALLENGE_TOMORROW, com.example.ui.screens.ROUTE_TOMORROW, com.example.ui.screens.challengeDoneLine("Tinta o Palabra"), com.example.ui.screens.routeMeta(ui))
    for (p in phrases) {
      assertFalse("«$p» usa «reto» o «modo difícil»", Regex("""\bretos?\b|modo dif[ií]cil""", RegexOption.IGNORE_CASE).containsMatchIn(p))
      assertTrue("«$p» no lleva signos sueltos ni flechas", p.none { it in "↑↓→←+−" })
    }
  }

  @Test
  fun `las frases de la ficha portada son las de la spec, una sola palabra por concepto y sin lo que se fue`() {
    // Tarea 171 (`auditoria-fable/ficha-juego.md` §6.5): «Jugar» · «a tu medida · sin reloj» (lo elegido, en la segunda línea) · «Desafío» · «Más opciones» · «¿Cómo quieres jugar?» · «¿Con reloj?» · «Sobre este juego» · «Qué pones en juego» · «Fuente: …» · «Cómo se juega»
    val source = File("src/main/java/com/example/ui/screens/GameSheet.kt").readText()
    val sheet = source.substring(source.indexOf("internal fun GameSheetContent("))
    val texts = sheet.lines().flatMap { visibleLiteralsOf(it) }
    for (phrase in listOf("Jugar", "Desafío", "Más opciones", "Menos opciones", "¿Cómo quieres jugar?", "¿Con reloj?", "Sobre este juego", "Qué pones en juego", "Cómo se juega", "Aún sin medir", "Sin reloj", "Con reloj")) {
      assertTrue("falta «$phrase» en la ficha", phrase in texts)
    }
    assertEquals("Sobre este juego: qué pones en juego y su fuente", texts.first { it.startsWith("Sobre este juego:") })
    // lo que se fue de la ficha no vuelve: el botón y la línea de «Cambiar cómo jugar», el «Listo» de plegarla, la marca con su chip, la constancia y «Te faltan N puntos»
    for (gone in listOf("Cambiar cómo jugar", "Listo", "Tu récord", "Mejorando", "Se mantiene", "Jugaste", "Te faltan", "Etapas:", "Tu vistazo", "partidas en 2 semanas")) {
      assertTrue("«$gone» no va en la ficha", texts.none { gone in it })
    }
    for (t in texts) {
      assertFalse("«$t» usa «reto» o «modo difícil» (la palabra es «Desafío»)", Regex("""retos?|modo dif[ií]cil""", RegexOption.IGNORE_CASE).containsMatchIn(t))
      assertTrue("«$t» no lleva signos sueltos ni flechas", t.none { it in "↑↓→←−" })
    }
    // los textos que arma `RetoChoice` para «Jugar» y su lector de pantalla
    assertEquals("a tu medida · sin reloj", com.example.data.RetoChoice.playDetail(com.example.data.PlayMode.A_TU_MEDIDA, false, true))
    assertEquals("a tu medida", com.example.data.RetoChoice.playDetail(com.example.data.PlayMode.A_TU_MEDIDA, true, false))
    assertEquals("Un paso más difícil", com.example.data.PlayMode.DESAFIO.short)
  }
}
