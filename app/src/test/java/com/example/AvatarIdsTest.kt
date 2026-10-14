package com.example

import com.example.data.AvatarIds
import com.example.data.local.UserProfileEntity
import com.example.data.local.toDomain
import com.example.data.local.toEntity
import com.example.model.AppLanguage
import com.example.model.UserSettings
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Los avatares de plasticina (Tarea 91) y su migración: ocho ids, ningún emoji, y todo lo que haya quedado guardado (un emoji de antes, un id nuevo, nada o algo raro) se resuelve a un avatar que existe: nadie queda sin avatar ni con uno que ya no está.
 * También el idioma: hoy solo el español está traducido, así que un idioma guardado que no lo es vuelve al español.
 */
class AvatarIdsTest {
  /** Los emojis que se ofrecían hasta la Tarea 90 (el 🧠 era el de por defecto). */
  private val legacy = listOf("🧠", "👵", "👴", "🌟", "🎯", "🦉", "🌿", "💡", "⚡")

  @Test
  fun `son ocho avatares con id y nombre distintos y el de por defecto es Nubi`() {
    assertEquals(8, AvatarIds.ALL.size)
    assertEquals(8, AvatarIds.ALL.toSet().size)
    assertEquals("nubi", AvatarIds.DEFAULT)
    assertTrue(AvatarIds.DEFAULT in AvatarIds.ALL)
    assertEquals("todos tienen su nombre para el lector de pantalla", AvatarIds.ALL.toSet(), AvatarIds.LABELS.keys)
    assertEquals(8, AvatarIds.LABELS.values.toSet().size)
    assertEquals(listOf("nubi", "cohete", "farol", "isla", "engranaje", "planeta", "lucero", "gota"), AvatarIds.ALL)
  }

  @Test
  fun `un id nuevo se queda como esta`() {
    for (id in AvatarIds.ALL) assertEquals(id, AvatarIds.resolve(id))
  }

  @Test
  fun `cada emoji de antes pasa a un avatar nuevo y el cerebro pasa a Nubi`() {
    assertEquals("nubi", AvatarIds.resolve("🧠"))
    for (old in legacy) assertTrue("«$old» debe pasar a un avatar que existe", AvatarIds.resolve(old) in AvatarIds.ALL)
    assertEquals("lucero", AvatarIds.resolve("🌟"))
    assertEquals("lucero", AvatarIds.resolve("⭐"))
    assertEquals("con el selector de variación (el que agregan algunos teléfonos)", "lucero", AvatarIds.resolve("⭐️"))
    assertEquals("planeta", AvatarIds.resolve("🎯"))
    assertEquals("isla", AvatarIds.resolve("🦉"))
    assertEquals("gota", AvatarIds.resolve("🌿"))
    assertEquals("farol", AvatarIds.resolve("💡"))
    assertEquals("engranaje", AvatarIds.resolve("⚡"))
    assertEquals("las caras de personas pasan a Nubi", "nubi", AvatarIds.resolve("👵"))
    assertEquals("nubi", AvatarIds.resolve("👴"))
  }

  @Test
  fun `nada, vacio o algo desconocido es el avatar por defecto`() {
    for (weird in listOf(null, "", "   ", "xyz", "🐙", "Nubi ", "NUBI2", "avatar_1")) {
      val r = AvatarIds.resolve(weird)
      assertTrue("«$weird» → «$r» debe existir", r in AvatarIds.ALL)
    }
    assertEquals("nubi", AvatarIds.resolve(null)); assertEquals("nubi", AvatarIds.resolve("")); assertEquals("nubi", AvatarIds.resolve("🐙"))
    assertEquals("un id con espacios alrededor se limpia", "cohete", AvatarIds.resolve(" cohete "))
  }

  @Test
  fun `el perfil guardado con un emoji viejo se lee con el avatar nuevo y al guardarlo queda el id`() {
    val viejo = UserProfileEntity(name = "Ana", avatar = "🧠")
    assertEquals("nubi", viejo.toDomain().avatar)
    assertEquals("nubi", UserProfileEntity(name = "Ana", avatar = "👴").toDomain().avatar)
    assertEquals("planeta", UserProfileEntity(avatar = "🎯").toDomain().avatar)
    assertEquals("cohete", UserProfileEntity(avatar = "cohete").toDomain().avatar)
    assertEquals("nubi", UserProfileEntity(avatar = "").toDomain().avatar)
    // ida y vuelta: lo que se guarda es el id
    val guardado = viejo.toDomain().toEntity()
    assertEquals("nubi", guardado.avatar)
    assertEquals("gota", UserSettings(avatar = "gota").toEntity().avatar)
    // los perfiles nuevos nacen con Nubi, no con un cerebro
    assertEquals("nubi", UserProfileEntity().avatar)
    assertEquals("nubi", UserSettings().avatar)
  }

  // ---- idiomas (Tarea 91, punto 2)

  @Test
  fun `solo el espanol esta traducido y es lo unico que se ofrece`() {
    assertEquals(listOf(AppLanguage.SPANISH), AppLanguage.selectable)
    assertTrue(AppLanguage.SPANISH.translated)
    for (l in AppLanguage.entries.filter { it != AppLanguage.SPANISH }) assertFalse("${l.displayName} no está traducido", l.translated)
    assertEquals("Español", AppLanguage.SPANISH.displayName)
    assertEquals("Français", AppLanguage.FRENCH.displayName)
    assertEquals("el nombre va en su lengua: Deutsch y Português", listOf("Deutsch", "Português"), listOf(AppLanguage.GERMAN.displayName, AppLanguage.PORTUGUESE.displayName))
  }

  @Test
  fun `un idioma guardado que no esta traducido vuelve al espanol`() {
    assertEquals(AppLanguage.SPANISH, AppLanguage.fromCode("es"))
    for (code in listOf("en", "fr", "de", "pt", "EN", "xx", "")) assertEquals("«$code»", AppLanguage.SPANISH, AppLanguage.fromCode(code))
    assertEquals(AppLanguage.SPANISH, UserProfileEntity(language = "de").toDomain().language)
    assertEquals(AppLanguage.SPANISH, UserProfileEntity(language = "en").toDomain().language)
  }
}
