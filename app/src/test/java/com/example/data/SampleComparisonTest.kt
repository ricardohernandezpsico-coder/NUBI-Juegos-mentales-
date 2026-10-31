package com.example.data

import com.example.Features
import com.example.model.AgeRange
import java.io.File
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Los datos de muestra de la comparación (`SampleComparison`, Tarea 202): deterministas, coherentes con la lámina de Fable (128 personas de 45 a 54 años, 43 aciertos supera a 63 de cada 100, la mitad 39 y la cuarta parte más alta 47), con dos mundos («muchos» y «pocos»)
 * y, sobre todo, SOLO en las builds de depuración: ninguna otra parte de la app los nombra.
 */
class SampleComparisonTest {
  private val many = SampleComparison(few = false)
  private val few = SampleComparison(few = true)

  @Test
  fun `la semana de hoy de 45 a 54 anos en Tinta o Palabra es la de la lamina de Fable`() {
    val d = many.distribution("stroop", listOf(AgeRange.R45_54), 0)
    assertEquals(128, d.people)
    assertEquals(SampleComparison.BASE, d.bins)
    assertEquals(listOf(31, 39, 47), listOf(d.p25, d.p50, d.p75))
    assertEquals(63, Comparison.percentile(43, d))
    assertEquals("En órbita", Comparison.division(63).label)
    assertEquals(
      "Tu mejor marca de esta semana, 43 aciertos, supera a 63 de cada 100 personas de 45 a 54 años que jugaron Tinta o Palabra esta semana.",
      Comparison.mainSentence(Comparison.Ranked(43, 63, Comparison.division(63), Comparison.Scope.OWN_WEEK, listOf(AgeRange.R45_54), d), AgeRange.R45_54, "Tinta o Palabra")
    )
  }

  @Test
  fun `son deterministas y la misma pregunta da siempre la misma respuesta`() {
    for (world in listOf(many, SampleComparison(few = false), few, SampleComparison(few = true))) for (game in listOf("stroop", "disparate")) for (w in 0..6)
      assertEquals(world.distribution(game, listOf(AgeRange.R55_64), w), world.distribution(game, listOf(AgeRange.R55_64), w))
    assertEquals(many.distribution("radar", null, 1, 4), SampleComparison(few = false).distribution("radar", null, 1, 4))
  }

  @Test
  fun `en cada clase los tramos suman las personas y hay 16`() {
    for (world in listOf(many, few)) for (game in listOf("stroop", "disparate", "radar", "acoplamiento", "otro")) {
      val rangeSets = AgeRange.entries.map { listOf(it) } + listOf(null, listOf(AgeRange.R30_44, AgeRange.R45_54))
      for (rs in rangeSets) for (ago in 0..6) for (weeks in listOf(1, 4)) {
        val d = world.distribution(game, rs, ago, weeks)
        assertEquals(Comparison.BINS, d.bins.size)
        assertEquals("$game $rs hace $ago semanas ($weeks): los tramos deben sumar las personas", d.people, d.bins.sum())
        assertTrue("los cuartiles van en orden", d.p25 <= d.p50 && d.p50 <= d.p75)
      }
    }
  }

  @Test
  fun `las clases juntadas suman sus partes`() {
    val parts = listOf(AgeRange.R30_44, AgeRange.R45_54).sumOf { r -> (0 until 4).sumOf { many.distribution("stroop", listOf(r), it).people } }
    assertEquals(parts, many.distribution("stroop", listOf(AgeRange.R30_44, AgeRange.R45_54), 0, 4).people)
    val all = AgeRange.entries.filter { it != AgeRange.R13_17 }.sumOf { many.distribution("stroop", listOf(it), 0).people }
    assertEquals("todas las edades no cuenta a los de 13 a 17 años mientras no puedan comparar", all, many.distribution("stroop", null, 0).people)
    assertFalse(Features.COMPARE_MINORS)
  }

  @Test
  fun `en el mundo de muchos solo la franja de 75 o mas tiene menos de 50 personas`() {
    val enough = AgeRange.entries.filter { it != AgeRange.R13_17 }.associateWith { many.distribution("stroop", listOf(it), 0).enough }
    assertEquals(mapOf(AgeRange.R18_29 to true, AgeRange.R30_44 to true, AgeRange.R45_54 to true, AgeRange.R55_64 to true, AgeRange.R65_74 to true, AgeRange.R75_PLUS to false), enough)
    assertTrue("y todas las semanas de la franja de 45 a 54 años llegan a 50", (0..6).all { many.distribution("stroop", listOf(AgeRange.R45_54), it).enough })
  }

  @Test
  fun `las franjas mas jovenes marcan mas y cada juego tiene su propio rango`() {
    val young = many.distribution("stroop", listOf(AgeRange.R18_29), 0)
    val old = many.distribution("stroop", listOf(AgeRange.R65_74), 0)
    assertTrue("la mediana de los jóvenes es más alta que la de los mayores", young.p50 > old.p50)
    assertTrue("la misma marca supera a más personas entre los mayores", Comparison.percentile(43, old) > Comparison.percentile(43, young))
    val stroop = many.distribution("stroop", listOf(AgeRange.R45_54), 0).p50
    val acoplamiento = many.distribution("acoplamiento", listOf(AgeRange.R45_54), 0).p50
    assertTrue("Acoplamiento marca menos que Tinta o Palabra", acoplamiento < stroop)
  }

  @Test
  fun `el mundo de pocos tiene 23 personas de 45 a 54 anos, no alcanza con 4 semanas ni con la franja vecina pero si con todas las edades`() {
    assertEquals(23, few.distribution("stroop", listOf(AgeRange.R45_54), 0).people)
    assertTrue(few.distribution("stroop", listOf(AgeRange.R45_54), 0, 4).people in 24..49)
    assertTrue("la franja vecina (30 a 54) sigue por debajo de 50", few.distribution("stroop", listOf(AgeRange.R30_44, AgeRange.R45_54), 0, 4).people < 50)
    assertTrue("todas las edades de las últimas 4 semanas sí llegan", few.distribution("stroop", null, 0, 4).enough)
    val place = Comparison.place(few, "stroop", AgeRange.R45_54, 43) as Comparison.Place.Few
    assertEquals(23, place.own)
    assertEquals("todas las edades llegan a 50 ESTA semana: «Mientras tanto» usa esa semana", Comparison.Scope.ALL_WEEK, place.meanwhile?.scope)
    // y en el mundo de muchos, lo normal
    assertEquals(Comparison.Scope.OWN_WEEK, ((Comparison.place(many, "stroop", AgeRange.R45_54, 43)) as Comparison.Place.Found).ranked.scope)
  }

  @Test
  fun `el mundo de muy pocos tiene 23 personas de 45 a 54 anos y ni todas las edades de esta semana llegan a 50 pero si las ultimas 4 semanas`() {
    val veryFew = SampleComparison(SampleComparison.World.VERY_FEW)
    assertEquals(23, veryFew.distribution("stroop", listOf(AgeRange.R45_54), 0).people)
    assertTrue("la franja las últimas 4 semanas sigue por debajo de 50", veryFew.distribution("stroop", listOf(AgeRange.R45_54), 0, 4).people < 50)
    assertTrue("y la franja vecina (30 a 54) también", veryFew.distribution("stroop", listOf(AgeRange.R30_44, AgeRange.R45_54), 0, 4).people < 50)
    assertTrue("todas las edades ESTA semana no llegan a 50", veryFew.distribution("stroop", null, 0).people < 50)
    assertTrue("las últimas 4 semanas sí", veryFew.distribution("stroop", null, 0, 4).enough)
    val place = Comparison.place(veryFew, "stroop", AgeRange.R45_54, 43) as Comparison.Place.Few
    assertEquals(23, place.own)
    assertEquals(Comparison.Scope.ALL_4_WEEKS, place.meanwhile?.scope)
  }

  @Test
  fun `en el mundo de pocos ninguna semana de la franja llega a 50 y no hay percentil semana a semana`() {
    assertTrue((0..6).none { few.distribution("stroop", listOf(AgeRange.R45_54), it).enough })
    assertTrue(Comparison.weekPoints(few, "stroop", AgeRange.R45_54, List(7) { 40 }).isEmpty())
    assertEquals("en el de muchos sí hay las 7 semanas", 7, Comparison.weekPoints(many, "stroop", AgeRange.R45_54, List(7) { 40 }).size)
  }

  // ------------------------------------------------------------------ las piezas de fabricación

  @Test
  fun `fit reparte las personas en la silueta y suma justo el total`() {
    assertEquals("si ya suma el total, queda igual", SampleComparison.BASE, SampleComparison.fit(SampleComparison.BASE, 128))
    for (n in listOf(1, 7, 23, 50, 99, 211, 1000)) assertEquals(n, SampleComparison.fit(SampleComparison.BASE, n).sum())
    assertTrue(SampleComparison.fit(SampleComparison.BASE, 211).all { it >= 0 })
  }

  @Test
  fun `shift corre los tramos y junta en el borde lo que se sale`() {
    assertEquals("a la derecha: lo que se sale por el borde se junta en el último tramo", listOf(0, 1, 2, 3), SampleComparison.shift(listOf(1, 2, 3, 0), 1))
    assertEquals("a la izquierda: se junta en el primero", listOf(3, 0, 0, 0), SampleComparison.shift(listOf(1, 2, 0, 0), -1))
    assertEquals(SampleComparison.BASE.sum(), SampleComparison.shift(SampleComparison.BASE, 5).sum())
    assertEquals(SampleComparison.BASE.sum(), SampleComparison.shift(SampleComparison.BASE, -9).sum())
  }

  // ------------------------------------------------------------------ SOLO depuración

  @Test
  fun `ninguna parte de la app salvo la fuente de la comparacion arma ni usa los datos de muestra`() {
    val builders = mutableListOf<String>()
    val others = mutableListOf<String>()
    File("src/main/java").walkTopDown().filter { it.extension == "kt" && it.name != "SampleComparison.kt" }.forEach { f ->
      val text = f.readText()
      if (Regex("""SampleComparison\(""").containsMatchIn(text)) builders += f.name
      // fuera de esos dos, solo se nombra su enum de mundos (el botón «[Debug]» y su preferencia): nunca la fuente
      if (Regex("""SampleComparison(?!\.World)""").containsMatchIn(text) && f.name !in listOf("Comparison.kt", "ContrarrelojPage.kt")) others += f.name
    }
    // `Comparison.kt` la arma solo si la build es de depuración; la página la reconoce para elegir la franja de la muestra
    assertEquals(listOf("Comparison.kt"), builders.sorted())
    assertEquals("solo el enum de mundos se nombra fuera: $others", emptyList<String>(), others)
    val source = File("src/main/java/com/example/data/Comparison.kt").readText()
    assertTrue("sourceFor solo arma la muestra con debugBuild", Regex("""debugBuild -> SampleComparison\(world\)""").containsMatchIn(source))
    val screen = File("src/main/java/com/example/ui/screens/ProgressScreen.kt").readText()
    assertTrue("la pantalla pide la fuente con BuildConfig.DEBUG", screen.contains("Comparison.sourceFor(com.example.BuildConfig.DEBUG"))
  }
}
