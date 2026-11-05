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
    // Nunca baja de cero.
    assertEquals(0f, Skill.progress("radar", -0.2f, AgeBand.ADULT), eps)
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
  fun `el encabezado de un juego se dice en puntos en toda la app, sin etapa`() {
    // Tarea 159: UNA escala. El área, de 0 a 1000 («470 de 1000 puntos»); la ficha de un juego, los mismos puntos en proporción («180 de 250 puntos»). Tarea 230: sin «Etapa …» delante ni «Te faltan N puntos para X»
    val g = GamePoints("parejas", 180, 250, 0.72f, true)
    assertEquals("180 de 250 puntos", g.pointsLine)
    assertEquals("90 de 200 puntos", GamePoints("radar", 90, 200, 0.45f, true).pointsLine)
    // un juego mirado a solas: avance × 1000 ÷ N (Memoria tiene 4 juegos; Atención, 5)
    assertEquals(GamePoints("parejas", 180, 250, 0.72f, true).points, GamePoints.of("parejas", 0.72f).points)
    assertEquals(100, GamePoints.of("radar", 0.5f).points); assertEquals(200, GamePoints.of("radar", 1f).max)
    assertEquals(250, GamePoints.of("parejas", 1f).points)
  }

  @Test
  fun `los modos tienen nombre propio, una frase corta y ningun numero de aciertos`() {
    // Tarea 150 (B1.3, B5.4) y 151 (B4.4): «Al límite» no choca con la etapa «Experto»; la ficha no dice «9 de 10 aciertos»
    assertEquals(listOf("Suave", "A tu medida", "Desafío", "Al límite"), PlayMode.entries.map { it.label })
    assertEquals(listOf("Un paso más fácil", "Mide tu avance", "Un paso más difícil", "Dos pasos más"), PlayMode.entries.map { it.short })
    for (m in PlayMode.entries) assertTrue(m.what, !m.what.contains("nivel", ignoreCase = true) && !m.what.contains("aquí", ignoreCase = true) && m.what.none { it.isDigit() })
  }

  @Test
  fun `aciertos esperados`() {
    assertEquals(0.70f, Skill.expectedHits("radar", PlayMode.DESAFIO, AgeBand.ADULT), eps)
    assertEquals(0.85f, Skill.expectedHits("radar", PlayMode.A_TU_MEDIDA, AgeBand.SENIOR), eps)
  }

  @Test
  fun `superado pide los aciertos de a tu medida y suficientes ensayos`() {
    assertTrue(Skill.passed("radar", PlayMode.DESAFIO, AgeBand.ADULT, hits = 10, trials = 12))
    assertFalse(Skill.passed("radar", PlayMode.DESAFIO, AgeBand.ADULT, hits = 9, trials = 12))
    assertFalse(Skill.passed("radar", PlayMode.DESAFIO, AgeBand.ADULT, hits = 11, trials = 11))
    assertFalse(Skill.passed("radar", PlayMode.DESAFIO, AgeBand.SENIOR, hits = 10, trials = 12))
    assertTrue(Skill.passed("satelites", PlayMode.EXPERTO, AgeBand.ADULT, hits = 5, trials = 6))
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
  fun `la etapa guardada sigue siendo quinto del avance aunque la app ya no la nombre`() {
    // Tarea 230: los nombres Inicio…Maestro salieron; `Skill.stage` queda solo por lo que ya se guardó (`stageDates`)
    assertEquals(1, Skill.stage(0.2f)); assertEquals(0, Skill.stage(0.199f)); assertEquals(4, Skill.stage(1f))
  }

  // ---- Escaleras alargadas (Tarea 233, `docs/alargar-dificultad.md` §B.1)

  @Test
  fun `solo Carga exacta tiene la escalera alargada y los demas juegos quedan como estaban`() {
    val carga = Skill.ladder("calculo")
    assertEquals(8, carga.levels); assertEquals(5, carga.pointsLevels); assertEquals("la pendiente se fija a mano: con 8 niveles la fórmula daría 1", 2f, carga.slope, 0f); assertEquals(2, carga.version)
    assertEquals(5f / 8f, carga.ratingScale, 1e-6f)
    assertEquals("la misma razón que la migración de Room y la traducción de lo guardado", Carga.OLD_TO_NEW_RATIO, carga.ratingScale, 1e-6f)
    for ((id, l) in Skill.ladders) if (id != "calculo") {
      assertEquals(id, l.levels, l.pointsLevels); assertEquals(id, 1, l.version); assertEquals(id, 1f, l.ratingScale, 0f)
    }
    assertEquals(mapOf("calculo" to 2), Skill.currentLadderMarks())
    assertEquals("un juego que no existe no se escala", 1f, Skill.ratingScale("juego-que-no-existe"), 0f)
  }

  @Test
  fun `los puntos quedan anclados a los 5 niveles de antes y los niveles nuevos no suman`() {
    for (age in listOf(AgeBand.ADULT, AgeBand.SENIOR, AgeBand.UNDER_18)) {
      val shift = (Skill.logit(Skill.trainingTarget("calculo", age)) - Skill.logit(Skill.READ_AT)) / 2f          // el corrimiento en niveles, con la pendiente de siempre
      // el mismo avance que daba la escalera de 5: rating viejo r5 ↔ rating nuevo r5 × 5/8 (lo que hace la migración de Room)
      for (r5 in listOf(0f, 0.1f, 0.3f, 0.46f, 0.6f, 0.8f, 0.92f, 1f)) {
        val old = (r5 + shift / 5f).coerceIn(0f, 1f)
        assertEquals("rating $r5, $age", old, Skill.progress("calculo", r5 * 5f / 8f, age), 1e-5f)
      }
      // justo en el último nivel de la v1 el avance llega a 1, y nada de lo que viene después lo mueve: los niveles 6 a 8 no suman puntos
      assertEquals(1f, Skill.progress("calculo", (1f - shift / 5f) * 5f / 8f, age), 1e-5f)
      for (r8 in listOf(0.7f, 0.8f, 0.9f, 0.999f, 1f)) assertEquals("rating $r8 ($age) ya pasó los 5 niveles de antes", 1f, Skill.progress("calculo", r8, age), 0f)
    }
  }

  @Test
  fun `el avance de la escalera entera llega a los niveles 6 a 8 y en los demas juegos es igual al de los puntos`() {
    val age = AgeBand.ADULT
    val r = 4.6f / 8f                                                  // alguien que estaba en el 4,6 de 5 y quedó en el 4,6 de 8
    assertEquals(0.575f, Skill.ladderProgress("calculo", r, age), eps)
    assertEquals("sus puntos: los mismos de antes", 0.92f, Skill.progress("calculo", r, age), eps)
    assertEquals("su nivel: el mismo de antes (el 5)", 5, LevelLines.levelOf("calculo", Skill.ladderProgress("calculo", r, age)))
    assertEquals("el rating más alto es el nivel 8", 8, LevelLines.levelOf("calculo", Skill.ladderProgress("calculo", 0.99f, age)))
    assertEquals("y sus puntos siguen en el tope", 1f, Skill.progress("calculo", 0.99f, age), 0f)
    // la trampa que esto evita: con el avance de los PUNTOS (anclado, con tope 1) el nivel 6,6 se leería como el 8
    assertEquals(6, LevelLines.levelOf("calculo", Skill.ladderProgress("calculo", 0.7f, age)))
    assertEquals(8, LevelLines.levelOf("calculo", Skill.progress("calculo", 0.7f, age)))
    for ((id, l) in Skill.ladders) if (l.levels == l.pointsLevels) for (rating in listOf(0f, 0.3f, 0.45f, 0.8f, 1f)) for (a in AgeBand.entries)
      assertEquals("$id $rating $a", Skill.progress(id, rating, a), Skill.ladderProgress(id, rating, a), 0f)
  }

  @Test
  fun `el tope de bajada se mide en puntos y en Carga exacta el rating baja 5 octavos de lo que baja en un juego sin alargar`() {
    val age = AgeBand.ADULT
    // un mal día (la partida termina en 0): el rating no baja más de MAX_DROP × 5/8 y los PUNTOS no bajan más de MAX_DROP, como en los demás juegos
    val next = Skill.nextRating(0.5f, 0f, PlayMode.A_TU_MEDIDA, false, "calculo")
    assertEquals(0.5f - Skill.MAX_DROP * 5f / 8f, next, 1e-5f)
    val dropped = Skill.progress("calculo", 0.5f, age) - Skill.progress("calculo", next, age)
    assertTrue("los puntos bajan $dropped y el tope es ${Skill.MAX_DROP}", dropped <= Skill.MAX_DROP + 1e-4f)
    assertEquals("sin juego, o en uno sin alargar, igual que siempre", 0.5f - Skill.MAX_DROP, Skill.nextRating(0.5f, 0f, PlayMode.A_TU_MEDIDA, false), 1e-5f)
    assertEquals(0.5f - Skill.MAX_DROP, Skill.nextRating(0.5f, 0f, PlayMode.A_TU_MEDIDA, false, "stroop"), 1e-5f)
  }

  @Test
  fun `los modos de Carga exacta suben el mismo numero de niveles que antes con la escalera de 8`() {
    // modeShift ÷ pendiente = niveles que se corre el modo; antes se repartía en 5 niveles y ahora en 8, pero son los mismos niveles
    val r5 = 0.4f; val r8 = r5 * 5f / 8f
    val oldFloorLevels = (Skill.bounds("stroop", r5, PlayMode.DESAFIO, AgeBand.ADULT).floor!! - r5) * 5f
    val newFloorLevels = (Skill.bounds("calculo", r8, PlayMode.DESAFIO, AgeBand.ADULT).floor!! - r8) * 8f
    assertEquals(oldFloorLevels, newFloorLevels, 1e-4f)
    val oldCeilLevels = (Skill.bounds("stroop", r5, PlayMode.SUAVE, AgeBand.ADULT).ceiling!! - r5) * 5f
    val newCeilLevels = (Skill.bounds("calculo", r8, PlayMode.SUAVE, AgeBand.ADULT).ceiling!! - r8) * 8f
    assertEquals(oldCeilLevels, newCeilLevels, 1e-4f)
  }

  @Test
  fun `la marca de la escalera de cada juego se guarda en la preferencia skill y las preferencias viejas se leen igual`() {
    val s = SkillState(measured = setOf("calculo"), expertOpen = setOf("stroop"), stageDates = mapOf("calculo" to mapOf(1 to 5L)), ladderMarks = mapOf("calculo" to 2))
    val back = SkillState.decode(s.encode())
    assertEquals(s, back)
    assertEquals(2, back.ladderMark("calculo")); assertEquals("sin marca = la escalera de siempre", 1, back.ladderMark("stroop"))
    val old = SkillState.decode("m|calculo\nx|stroop\ns|calculo|1|5")
    assertEquals(emptyMap<String, Int>(), old.ladderMarks)
    assertEquals(1, old.ladderMark("calculo"))
    assertEquals("una línea rota se ignora", emptyMap<String, Int>(), SkillState.decode("l|calculo|dos").ladderMarks)
  }

  @Test
  fun `experto se abre al superar un desafio`() {
    assertFalse(Skill.isOpen(PlayMode.EXPERTO, "radar", emptySet()))
    assertTrue(Skill.isOpen(PlayMode.EXPERTO, "radar", setOf("radar")))
    assertTrue(Skill.isOpen(PlayMode.DESAFIO, "radar", emptySet()))
    assertTrue(Skill.isOpen(PlayMode.SUAVE, "radar", emptySet()))
  }

  @Test
  fun `lo medido, experto abierto y la fecha de cada etapa se recuerdan`() {
    var st = SkillState()
    st = st.after("radar", counted = false, passedChallenge = false, progress = 0.9f, now = 10L)
    assertEquals(SkillState(), st) // Suave o Desafío sin superar: nada
    st = st.after("radar", counted = true, passedChallenge = false, progress = 0.45f, now = 20L)
    assertEquals(setOf("radar"), st.measured)
    assertEquals(mapOf(1 to 20L, 2 to 20L), st.stageDates["radar"])
    st = st.after("radar", counted = true, passedChallenge = true, progress = 0.62f, now = 30L)
    assertEquals(setOf("radar"), st.expertOpen)
    assertEquals(mapOf(1 to 20L, 2 to 20L, 3 to 30L), st.stageDates["radar"]) // las fechas viejas no cambian
    assertEquals(st, SkillState.decode(st.encode()))
    assertEquals(SkillState(), SkillState.decode(null))
  }
}
