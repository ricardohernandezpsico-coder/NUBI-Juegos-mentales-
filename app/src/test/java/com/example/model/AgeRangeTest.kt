package com.example.model

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Las siete franjas de edad (Tarea 195, decisión de Ricardo del 30-oct; `docs/contrarreloj.md` §6): 13-17 · 18-29 · 30-44 · 45-54 · 55-64 · 65-74 · 75+. De ellas sale la banda de tres del DDA ([AgeBand]), que NO cambia: 13-17 = menos de 18; 18 a 64 = adulto; 65 en adelante = mayor.
 */
class AgeRangeTest {
  @Test
  fun son_siete_en_orden_y_cada_una_cae_en_su_banda_de_tres() {
    val expected = listOf(
      "R13_17" to AgeBand.UNDER_18, "R18_29" to AgeBand.ADULT, "R30_44" to AgeBand.ADULT, "R45_54" to AgeBand.ADULT,
      "R55_64" to AgeBand.ADULT, "R65_74" to AgeBand.SENIOR, "R75_PLUS" to AgeBand.SENIOR
    )
    assertEquals(expected, AgeRange.entries.map { it.name to it.toBand() })
  }

  @Test
  fun las_tres_bandas_del_DDA_siguen_igual() {
    assertEquals(listOf("UNDER_18", "ADULT", "SENIOR"), AgeBand.entries.map { it.name })
    assertEquals(listOf("Menos de 18", "18 a 64", "65 o más"), AgeBand.entries.map { it.label })
  }

  @Test
  fun cada_banda_tiene_al_menos_una_franja_y_ninguna_queda_sin_banda() {
    for (band in AgeBand.entries) assertTrue("$band sin franja", AgeRange.entries.any { it.toBand() == band })
    assertEquals(setOf(AgeBand.UNDER_18, AgeBand.ADULT, AgeBand.SENIOR), AgeRange.entries.map { it.toBand() }.toSet())
  }

  @Test
  fun cada_numero_va_con_su_unidad_y_no_hay_franja_para_menores_de_13() {
    assertEquals(
      listOf("13 a 17 años", "18 a 29 años", "30 a 44 años", "45 a 54 años", "55 a 64 años", "65 a 74 años", "75 años o más"),
      AgeRange.entries.map { it.label }
    )
    for (r in AgeRange.entries) assertTrue("${r.name}: la etiqueta dice «años»", r.label.contains("años"))
    // política de Play para familias: nada por debajo de 13 años
    for (r in AgeRange.entries) assertFalse("${r.name}: no hay franja para menores de 13", Regex("""^(\d|1[0-2])\D""").containsMatchIn(r.label))
    assertEquals("la más joven empieza en 13", "13 a 17 años", AgeRange.entries.first().label)
  }

  @Test
  fun se_lee_por_nombre_y_lo_que_no_existe_es_nulo() {
    for (r in AgeRange.entries) assertEquals(r, AgeRange.fromStored(r.name))
    assertNull(AgeRange.fromStored(null))
    assertNull(AgeRange.fromStored(""))
    assertNull("un nombre de banda de tres no es una franja", AgeRange.fromStored("ADULT"))
    assertNull(AgeRange.fromStored("R5_9"))
  }
}
