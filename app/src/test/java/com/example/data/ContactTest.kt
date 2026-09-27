package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class ContactTest {
  private fun dict(vararg words: NuriWord) = words.associateBy { it.id }

  @Test
  fun `el idioma tiene las mismas 36 palabras que en Unity`() {
    assertEquals(Contact.WORD_COUNT, Contact.WORDS.size)
    assertEquals(Contact.WORD_COUNT, Contact.MEANINGS.size)
    assertEquals(Contact.WORD_COUNT, Contact.WORDS.toSet().size)
    assertEquals("KITU", Contact.label(1))
    assertEquals("cohete", Contact.meaning(1))
    assertEquals("BEL", Contact.label(35))
  }

  @Test
  fun `se repasan hasta 4 de otros dias, las mas olvidadas primero`() {
    val today = 100L
    val d = dict(
      NuriWord(0, 90, 99), NuriWord(1, 90, 95), NuriWord(2, 90, 100), NuriWord(3, 90, 97),
      NuriWord(4, 90, 96), NuriWord(5, 90, 98)
    )
    val launch = Contact.launch(d, today)
    assertEquals(listOf(0, 1, 2, 3, 4, 5), launch.known)
    assertEquals(listOf(1, 4, 3, 5), launch.review)     // la 2 se vio hoy: no se repasa
    assertEquals(listOf(5, 4, 3, 2), launch.reviewDays)
    assertTrue(Contact.launch(dict(NuriWord(7, 100, 100)), today).review.isEmpty())
  }

  @Test
  fun `lo repasado bien se renueva, lo olvidado sale y lo descifrado entra`() {
    val d = dict(NuriWord(0, 90, 95), NuriWord(1, 90, 96))
    val after = Contact.apply(d, 100, decoded = listOf(12, 1), reviewed = listOf(0, 1), reviewOk = listOf(true, false))
    assertEquals(NuriWord(0, 90, 100, reviews = 1), after[0])
    assertEquals(NuriWord(1, 100, 100), after[1])       // se olvidó y se volvió a descifrar hoy
    assertEquals(NuriWord(12, 100, 100), after[12])
    val forgot = Contact.apply(d, 100, decoded = emptyList(), reviewed = listOf(1), reviewOk = listOf(false))
    assertFalse(1 in forgot)
    assertEquals(setOf(0), forgot.keys)
    // Números fuera del idioma no entran.
    assertFalse(99 in Contact.apply(d, 100, listOf(99), emptyList(), emptyList()))
  }

  @Test
  fun `las lecturas no dicen nada con pocos datos y dan un truco cuando cuesta`() {
    assertNull(Contact.exclusionMessage(3, 4))
    assertTrue(Contact.exclusionMessage(8, 9)!!.contains("niños"))
    assertTrue(Contact.exclusionMessage(2, 6)!!.contains("Truco"))
    assertFalse(Contact.hearingsMessage(3.2f).contains("Truco"))
    assertTrue(Contact.hearingsMessage(5.5f).contains("Truco"))
    assertTrue(Contact.hearingsMessage(3.4f).contains("3,4"))
    assertEquals("KITU = cohete · RA = coral", Contact.decodedLine(listOf(1, 12)))
    assertNotNull(Contact.reviewMessage(3, 4, 5))
    assertTrue(Contact.reviewMessage(4, 4, 1).contains("de ayer"))
  }
}
