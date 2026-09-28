package com.example.data

import com.example.model.AgeBand
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class SkillTest {
  private val eps = 0.001f

  @Test
  fun `el avance se lee a 8 de 10 para todas las edades`() {
    // Adulto: el DDA ya busca 80%, sin corrección.
    assertEquals(0.40f, Skill.progress("radar", 0.40f, AgeBand.ADULT), eps)
    assertEquals(0.40f, Skill.progress("radar", 0.40f, AgeBand.UNDER_18), eps)
    // Mayor: busca 85%, su rating queda ~0,35 niveles más abajo; se devuelve (12 niveles).
    assertEquals(0.40f + 0.3483f / 12f, Skill.progress("radar", 0.40f, AgeBand.SENIOR), eps)
    // Ruta del Tesoro busca 70%: su rating queda más arriba que el de 8 de 10.
    assertEquals(0.40f - 0.5390f / 12f, Skill.progress("rutatesoro", 0.40f, AgeBand.ADULT), eps)
    assertEquals(0f, Skill.progress("rutatesoro", 0.01f, AgeBand.ADULT), eps)
  }

  @Test
  fun `los modos ponen techo o piso segun los aciertos esperados y la edad`() {
    assertEquals(ModeBounds(), Skill.bounds("radar", 0.40f, PlayMode.A_TU_MEDIDA, AgeBand.ADULT))
    assertEquals(ModeBounds(), Skill.bounds("radar", -1f, PlayMode.DESAFIO, AgeBand.ADULT))
    val suave = Skill.bounds("radar", 0.40f, PlayMode.SUAVE, AgeBand.ADULT)
    assertNull(suave.floor); assertEquals(0.40f - 0.8109f / 12f, suave.ceiling!!, eps)
    assertEquals(0.40f + 0.5390f / 12f, Skill.bounds("radar", 0.40f, PlayMode.DESAFIO, AgeBand.ADULT).floor!!, eps)
    assertEquals(0.40f + 0.9808f / 12f, Skill.bounds("radar", 0.40f, PlayMode.EXPERTO, AgeBand.ADULT).floor!!, eps)
    // Mayor: parte desde 85%, su Desafío (75%) sube algo más de nivel que el del adulto (70%).
    assertEquals(0.40f + 0.6360f / 12f, Skill.bounds("radar", 0.40f, PlayMode.DESAFIO, AgeBand.SENIOR).floor!!, eps)
    // Escalera corta (5 niveles, pendiente 2): cada nivel es un salto mayor.
    assertEquals(0.40f + 0.5390f / 2f / 5f, Skill.bounds("stroop", 0.40f, PlayMode.DESAFIO, AgeBand.ADULT).floor!!, eps)
  }

  @Test
  fun `aciertos esperados y como se dicen`() {
    assertEquals(0.70f, Skill.expectedHits("radar", PlayMode.DESAFIO, AgeBand.ADULT), eps)
    assertEquals(0.85f, Skill.expectedHits("radar", PlayMode.A_TU_MEDIDA, AgeBand.SENIOR), eps)
    assertEquals(0.5765f, Skill.expectedHits("rutatesoro", PlayMode.DESAFIO, AgeBand.ADULT), eps)
    assertEquals("8 de 10", Skill.hitsText(0.80f))
    assertEquals("8 a 9 de 10", Skill.hitsText(0.85f))
    assertEquals("9 de 10", Skill.hitsText(0.92f))
    assertEquals("7 a 8 de 10", Skill.hitsText(0.75f))
    assertEquals("7 de 10", Skill.hitsText(0.70f))
  }

  @Test
  fun `superado pide los aciertos de a tu medida y suficientes ensayos`() {
    assertTrue(Skill.passed("radar", PlayMode.DESAFIO, AgeBand.ADULT, hits = 10, trials = 12))
    assertFalse(Skill.passed("radar", PlayMode.DESAFIO, AgeBand.ADULT, hits = 9, trials = 12))
    assertFalse(Skill.passed("radar", PlayMode.DESAFIO, AgeBand.ADULT, hits = 11, trials = 11))
    assertFalse(Skill.passed("radar", PlayMode.DESAFIO, AgeBand.SENIOR, hits = 10, trials = 12))
    assertTrue(Skill.passed("rumbo", PlayMode.EXPERTO, AgeBand.ADULT, hits = 5, trials = 6))
    assertFalse(Skill.passed("radar", PlayMode.A_TU_MEDIDA, AgeBand.ADULT, hits = 12, trials = 12))
    assertFalse(Skill.passed("radar", PlayMode.SUAVE, AgeBand.ADULT, hits = 12, trials = 12))
  }

  @Test
  fun `el avance solo cambia con partidas comparables y nunca cae de golpe`() {
    assertEquals(0.30f, Skill.nextRating(-1f, 0.30f, PlayMode.A_TU_MEDIDA, false), eps)
    assertEquals(0.56f, Skill.nextRating(0.50f, 0.60f, PlayMode.A_TU_MEDIDA, false), eps)
    assertEquals(0.45f, Skill.nextRating(0.50f, 0.30f, PlayMode.A_TU_MEDIDA, false), eps) // tope de bajada
    assertEquals(0.50f, Skill.nextRating(0.50f, 0.20f, PlayMode.SUAVE, false), eps)
    assertEquals(0.50f, Skill.nextRating(0.50f, 0.58f, PlayMode.DESAFIO, false), eps)
    assertEquals(0.548f, Skill.nextRating(0.50f, 0.58f, PlayMode.DESAFIO, true), eps)
    assertEquals(0.50f, Skill.nextRating(0.50f, 0.40f, PlayMode.EXPERTO, true), eps) // superado nunca baja
  }

  @Test
  fun `etapas, puntos a la siguiente y avance del area`() {
    assertEquals("Inicio", Skill.stageName(0.05f))
    assertEquals("Hábil", Skill.stageName(0.45f))
    assertEquals("Maestro", Skill.stageName(1f))
    assertEquals(15 to "Experto", Skill.toNextStage(0.45f))
    assertEquals(20 to "Experto", Skill.toNextStage(0.40f))
    assertEquals(1 to "Aprendiz", Skill.toNextStage(0.199f))
    assertNull(Skill.toNextStage(0.85f))
    assertEquals(45, Skill.percent(0.459f))
    val (avg, explored) = Skill.area(listOf("rumbo", "correo", "secuencia"), mapOf("rumbo" to 0.4f, "secuencia" to 0.6f, "radar" to 1f))
    assertEquals(0.5f, avg!!, eps); assertEquals(2, explored)
    assertEquals(null to 0, Skill.area(listOf("correo"), emptyMap()))
  }

  @Test
  fun `experto se abre al superar un desafio y la bitacora solo a tu medida`() {
    assertFalse(Skill.isOpen(PlayMode.EXPERTO, "radar", emptySet()))
    assertTrue(Skill.isOpen(PlayMode.EXPERTO, "radar", setOf("radar")))
    assertTrue(Skill.isOpen(PlayMode.DESAFIO, "radar", emptySet()))
    assertEquals(listOf(PlayMode.A_TU_MEDIDA), Skill.modesFor("bitacora"))
    assertFalse(Skill.isOpen(PlayMode.SUAVE, "bitacora", emptySet()))
  }
}
