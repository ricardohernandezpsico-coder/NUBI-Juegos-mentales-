package com.example

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * Los luceros del fondo van SIEMPRE detrás del contenido y los textos y tarjetas los tapan (Tarea 123; Fable lo vio en Avance y en Licencias). Se mira el código:
 * - el fondo es la PRIMERA capa de `NeuroVidaApp`, antes de las pestañas, la barra y todo lo demás;
 * - la barra de pestañas es una capa fija encima de las páginas (no el `bottomBar` de un `Scaffold`) y ya no hay desvanecido de página;
 * - los textos que van directo sobre el fondo, sin tarjeta (Licencias y créditos, la cabecera de los paneles, el nombre del perfil, el título de Ajustes) usan `OnBoardText`, que les pinta una orilla densa del color de la lámina.
 */
class BoardLayersTest {
  /** El código sin los comentarios (los comentarios hablan del Scaffold y del desvanecido que se quitaron). */
  private fun src(path: String) = File("src/main/java/com/example/$path").readLines().filterNot { it.trimStart().startsWith("//") || it.trimStart().startsWith("*") || it.trimStart().startsWith("/*") }.joinToString("\n")

  @Test
  fun el_fondo_es_la_primera_capa_y_la_barra_va_encima_de_las_paginas() {
    val main = src("MainActivity.kt")
    val bg = main.indexOf("BoardBackground()")
    val pager = main.indexOf("HorizontalPager(")
    val bar = main.indexOf("NeuroNavBar(")
    assertTrue("el fondo está en MainActivity", bg > 0 && pager > 0 && bar > 0)
    assertTrue("el fondo se compone ANTES que las páginas (queda detrás de todo)", bg < pager)
    assertTrue("la barra se compone DESPUÉS de las páginas (capa fija encima)", pager < bar)
    assertFalse("la barra ya no es el bottomBar de un Scaffold", main.contains("Scaffold(") || main.contains("bottomBar"))
    assertFalse("ya no hay desvanecido de página", main.contains("PAGE_BOTTOM_FADE") || main.contains("DstIn"))
    assertTrue("las páginas dejan la franja de la barra con LocalBarInset", src("ui/screens/HomeScreen.kt").contains("LocalBarInset") && src("ui/screens/GamesLibraryScreen.kt").contains("LocalBarInset") && src("ui/screens/ProgressScreen.kt").contains("LocalBarInset"))
  }

  @Test
  fun los_textos_sueltos_sobre_el_fondo_usan_OnBoardText() {
    val plainText = Regex("""(^|[^A-Za-z.])Text\(""", RegexOption.MULTILINE)
    assertFalse("Licencias y créditos va directo sobre el fondo: todo su texto es OnBoardText", plainText.containsMatchIn(src("ui/screens/LicensesScreen.kt")))
    val profile = src("ui/screens/ProfileScreen.kt")
    assertTrue("el título de los paneles y el nombre del perfil", profile.contains("OnBoardText(title,") && profile.contains("OnBoardText(userSettings.name.ifBlank"))
    assertTrue("el título de Ajustes", src("ui/screens/SettingsScreen.kt").contains("OnBoardText(\n      text = strings.settingsTitle") || src("ui/screens/SettingsScreen.kt").contains("OnBoardText(\r\n      text = strings.settingsTitle"))
  }
}
