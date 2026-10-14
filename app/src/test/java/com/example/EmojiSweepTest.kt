package com.example

import com.example.data.Achievements
import com.example.data.AchievementGlyph
import com.example.data.AvatarIds
import com.example.model.AppLanguage
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.model.WeeklyChallengeRegistry
import com.example.ui.i18n.getTranslations
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * El barrido de emojis (Tarea 91, regla de CLAUDE.md: «Nada de emojis como íconos»): ningún texto, recurso o lista de la app lleva un emoji. Revisa (1) el código de `app/src/main/java` sin comentarios, (2) los recursos (`res/values*`, `res/xml`, `res/raw`, `assets`, el manifiesto) y (3) las listas que la app arma al correr:
 * avatares, idiomas, logros, juegos, desafíos, ligas, áreas y traducciones. Y busca «cerebro», «brain» y el emoji 🧠 en el código y los recursos (el avatar por defecto era un cerebro).
 *
 * [EXCEPCIONES]: la lista de lo que SÍ puede llevar un emoji. Está VACÍA a propósito y debe seguir vacía: si algún día hace falta una excepción, se anota acá con su archivo y su razón (y se la comenta a Ricardo).
 * El rango Unicode es razonable, no exhaustivo: los bloques de emojis y pictogramas (U+1F000–1FAFF), los símbolos varios y dingbats que la mayoría de los teléfonos dibujan como emoji (U+2600–27BF, salvo el visto ✓ ✗ de texto), la estrella ⭐, el círculo ⭕, los cuadrados ⬛⬜, flechas curvas,
 * relojes y reproducir del bloque técnico, y los unidores (selector de variación U+FE0F, unión U+200D, el cuadro de teclas U+20E3).
 */
class EmojiSweepTest {
  /** Lo que puede llevar un emoji sin que el barrido falle: nada. Cada entrada sería «archivo» (o «lista:nombre»). */
  private val EXCEPCIONES: Set<String> = emptySet()

  private fun isEmoji(cp: Int): Boolean =
    cp in 0x1F000..0x1FAFF ||
      (cp in 0x2600..0x27BF && cp !in setOf(0x2713, 0x2717)) ||
      cp in setOf(0x2B50, 0x2B55, 0x2B1B, 0x2B1C, 0x2934, 0x2935, 0x3030, 0x303D, 0x3297, 0x3299, 0x200D, 0xFE0F, 0x20E3, 0x21A9, 0x21AA, 0x231A, 0x231B, 0x23E9, 0x23EA, 0x23F0, 0x23F3, 0x25B6, 0x25C0, 0x25FB, 0x25FC, 0x25FD, 0x25FE)

  private fun emojisIn(text: String): List<String> = text.codePoints().toArray().filter(::isEmoji).map { String(Character.toChars(it)) }

  /** El código sin comentarios (de `//` al final de la línea y `/* … */`, incluidos los KDoc), para no contar lo que solo explica. Respeta las comillas. */
  private fun withoutComments(src: String): String {
    val out = StringBuilder(); var i = 0; var inStr = false; var raw = false
    while (i < src.length) {
      if (!inStr && src.startsWith("//", i)) { val j = src.indexOf('\n', i); i = if (j < 0) src.length else j; continue }
      if (!inStr && src.startsWith("/*", i)) { val j = src.indexOf("*/", i + 2); i = if (j < 0) src.length else j + 2; continue }
      if (src.startsWith("\"\"\"", i)) { raw = !raw; inStr = raw; out.append("\"\"\""); i += 3; continue }
      if (!raw && src[i] == '"' && (i == 0 || src[i - 1] != '\\')) inStr = !inStr
      out.append(src[i]); i++
    }
    return out.toString()
  }

  private fun main(path: String) = File("src/main/$path")

  /** Todos los archivos de código de la app, ya sin comentarios: nombre → texto. */
  private fun kotlinSources(): Map<String, String> =
    main("java").walkTopDown().filter { it.isFile && it.extension == "kt" }.associate { it.path.replace('\\', '/') to withoutComments(it.readText()) }

  /** Los recursos de texto: valores, xml, raw y assets, más el manifiesto. */
  private fun resourceFiles(): List<File> {
    val dirs = main("res").listFiles { f -> f.isDirectory && (f.name.startsWith("values") || f.name == "xml" || f.name == "raw") }.orEmpty().toList() + main("assets")
    return dirs.filter { it.exists() }.flatMap { it.walkTopDown().filter { f -> f.isFile && f.extension.lowercase() in setOf("xml", "json", "txt", "html", "csv", "properties") }.toList() } + listOfNotNull(main("AndroidManifest.xml").takeIf { it.exists() })
  }

  // ---- el detector mismo

  @Test
  fun `el detector atrapa emojis, banderas y cosas con selector y deja pasar el texto normal`() {
    for (bad in listOf("🧠", "🇪🇸", "⭐", "✉️", "☄️", "Hola 👋", "⚡", "🌟")) assertTrue("debía detectar «$bad»", emojisIn(bad).isNotEmpty())
    for (good in listOf("Español", "Français", "¡Hola, Ana!", "✓ Hecho", "→", "·", "—", "1/10", "Ñandú", "%d días", "a 18% de casa")) assertTrue("no debía detectar «$good»", emojisIn(good).isEmpty())
  }

  // ---- el código y los recursos

  @Test
  fun `ningun archivo de codigo de la app lleva un emoji (sin contar comentarios)`() {
    val hits = kotlinSources().filterKeys { it !in EXCEPCIONES }.flatMap { (f, t) -> t.lines().mapIndexedNotNull { i, l -> emojisIn(l).takeIf { it.isNotEmpty() }?.let { "$f:${i + 1} ${it.joinToString("")} | ${l.trim().take(80)}" } } }
    assertTrue("Emojis en el código de la app (usar los íconos de plasticina del kit, `docs/simbolos-neutros.md`):\n" + hits.joinToString("\n"), hits.isEmpty())
  }

  @Test
  fun `ningun recurso de la app (textos, xml, raw, assets, manifiesto) lleva un emoji`() {
    val hits = resourceFiles().filter { it.path.replace('\\', '/') !in EXCEPCIONES }.flatMap { f -> f.readText().lines().mapIndexedNotNull { i, l -> emojisIn(l).takeIf { it.isNotEmpty() }?.let { "${f.path}:${i + 1} ${it.joinToString("")}" } } }
    assertTrue("Emojis en los recursos:\n" + hits.joinToString("\n"), hits.isEmpty())
  }

  @Test
  fun `ni el codigo ni los recursos hablan de cerebro, brain ni usan el emoji del cerebro`() {
    val words = listOf("cerebro", "brain", "🧠")
    val code = kotlinSources().flatMap { (f, t) -> t.lines().mapIndexedNotNull { i, l -> words.firstOrNull { it in l.lowercase() }?.let { "$f:${i + 1} «$it»" } } }
    val res = resourceFiles().flatMap { f -> f.readText().lines().mapIndexedNotNull { i, l -> words.firstOrNull { it in l.lowercase() }?.let { "${f.path}:${i + 1} «$it»" } } }
    assertTrue("«cerebro»/«brain» en el código o los recursos:\n" + (code + res).joinToString("\n"), (code + res).isEmpty())
  }

  // ---- lo que la app arma al correr

  private fun assertNoEmoji(name: String, texts: Collection<String>) {
    if ("lista:$name" in EXCEPCIONES) return
    val bad = texts.filter { emojisIn(it).isNotEmpty() }
    assertTrue("La lista «$name» trae emojis: $bad", bad.isEmpty())
  }

  @Test
  fun `las listas de la app no traen emojis`() {
    assertNoEmoji("avatares", AvatarIds.ALL + AvatarIds.LABELS.values + AvatarIds.LABELS.keys)
    assertNoEmoji("idiomas", AppLanguage.entries.flatMap { listOf(it.code, it.displayName) })
    assertNoEmoji("logros", Achievements.all.flatMap { listOf(it.id, it.title, it.description) })
    assertNoEmoji("juegos", GameRegistry.allGames.flatMap { listOf(it.id, it.title, it.subtitle, it.instruction) })
    assertNoEmoji("desafios semanales", WeeklyChallengeRegistry.all.flatMap { listOf(it.key, it.title, it.description) })
    assertNoEmoji("ligas", RankTier.entries.map { it.tierName })
    assertNoEmoji("areas", DomainType.entries.flatMap { listOf(it.displayName, it.tagline) })
    for (lang in AppLanguage.entries) {
      val t = getTranslations(lang)
      assertNoEmoji("traducciones ${lang.code}", listOf(t.greetingFormat("Ana"), t.settingsTitle, t.languageCardTitle, t.languageCardSubtitle, t.tabToday, t.tabGames, t.tabProgress, t.tabSettings))
    }
  }

  @Test
  fun `los logros no usan una estrella de puntas, una media luna ni una cruz`() {
    // docs/simbolos-neutros.md: luceros redondos, hexágonos, gotas y olas sí; estrellas de puntas, lunas y cruces no.
    val prohibidas = listOf("STAR", "MOON", "CRESCENT", "CROSS", "PLUS")
    assertTrue(AchievementGlyph.values().none { g -> prohibidas.any { it in g.name } })
    assertTrue("Brillante y Perfecto llevan el lucero redondo", listOf("brillante", "perfecto").all { id -> Achievements.byId(id)?.glyph == AchievementGlyph.LUCERO })
  }

  @Test
  fun `la lista de excepciones esta vacia`() {
    assertEquals("Si hace falta una excepción, se anota en EXCEPCIONES con su razón y se le cuenta a Ricardo; mientras tanto, vacía.", emptySet<String>(), EXCEPCIONES)
    assertFalse(EXCEPCIONES.isNotEmpty())
  }
}
