package com.example.data

import com.example.NoLossGuard
import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * Las 17 frases «Ya …» (Tarea 159; `auditoria-fable/avance-b-puntos.md` §4): una por juego y por nivel de su escalera, con los números de cada contrato de Unity. Existen, ninguna queda vacía en ningún nivel, ninguna lleva cifras internas
 * (milisegundos, dp, grados con el símbolo) ni culpa, y donde el rasgo principal no sube de un nivel al siguiente (las luces de Constelaciones, los objetos de Bodega) la frase lo dice para que no parezca que se retrocede.
 */
class LevelLinesTest {
  private val games = GameRegistry.allGames.map { it.id }

  @Test
  fun `hay una frase para cada uno de los 17 juegos en cada nivel de su escalera, y ninguna esta vacia`() {
    assertEquals(17, games.size)
    for (g in games) {
      val n = Skill.ladder(g).levels
      for (level in 1..n) {
        val line = Skill.levelLine(g, level)
        assertTrue("$g nivel $level: «$line» no empieza con «Ya »", line.startsWith("Ya ") && line.length > 12)
        assertFalse("$g nivel $level termina con punto o espacio: «$line»", line.endsWith(".") || line.endsWith(" "))
      }
    }
  }

  @Test
  fun `ninguna frase lleva cifras internas, abreviaturas ni culpa`() {
    val bad = listOf(" ms", "dp", "°", "pts", "sem.", "→", "%", "  ", "..", "ms/", "/s", "con reloj", "de cada", "tramos", "cambios", "bordes", "casi siempre", "llave", "lazo", "radio", "sello")
    for (g in games) for (level in 1..Skill.ladder(g).levels) for (timed in listOf(false, true)) {
      val line = Skill.levelLine(g, level, timed)
      for (b in bad) assertFalse("$g nivel $level: «$line» trae «$b»", b in line)
      assertEquals("$g nivel $level: «$line» (palabra prohibida)", null, NoLossGuard.hit(line, NoLossGuard.literals))
    }
  }

  /** Las reglas de Opus (22-oct) para una persona mayor: 12 palabras como máximo y a lo más 2 cifras significativas en los segundos («1,2», no «1,18»). */
  @Test
  fun `las frases son cortas, de a lo mas 12 palabras, y los segundos llevan 2 cifras como maximo`() {
    for (g in games) for (level in 1..Skill.ladder(g).levels) for (timed in listOf(false, true)) {
      val line = Skill.levelLine(g, level, timed)
      val words = line.trim().split(Regex("\\s+")).size
      assertTrue("$g nivel $level: «$line» tiene $words palabras (máximo 12)", words <= 12)
      // «1,18 segundos» o «1,345»: más de 2 cifras significativas (un entero seguido de 2 decimales, o 3 decimales)
      assertFalse("$g nivel $level: «$line» trae más de 2 cifras significativas", Regex("[1-9]\\d*,\\d{2,}|\\d,\\d{3,}").containsMatchIn(line))
    }
  }

  @Test
  fun `el nivel sale de el avance como el motor comun de Unity`() {
    // nivel = min(N, 1 + floor(N × avance)); el épsilon evita que 0,25 caiga en el nivel 3 por el redondeo del flotante
    assertEquals(1, LevelLines.levelOf("radar", 0f)); assertEquals(12, LevelLines.levelOf("radar", 1f))
    assertEquals(4, LevelLines.levelOf("radar", 0.25f)); assertEquals(4, LevelLines.levelOf("radar", 0.2499999f)); assertEquals(3, LevelLines.levelOf("radar", 0.2f))
    assertEquals(1, LevelLines.levelOf("stroop", 0.19f)); assertEquals(2, LevelLines.levelOf("stroop", 0.2f)); assertEquals(5, LevelLines.levelOf("stroop", 0.99f))
    assertEquals("Skill.stage y el nivel de las escaleras de 5 son lo mismo", Skill.stage(0.45f) + 1, LevelLines.levelOf("calculo", 0.45f))
    assertEquals(18, LevelLines.levelOf("parejas", 0.999f)); assertEquals(9, LevelLines.levelOf("piloto", 1f))
    assertEquals(LevelLines.lineAt("secuencia", LevelLines.levelOf("secuencia", 0.5f)), LevelLines.line("secuencia", 0.5f))
  }

  @Test
  fun `los ejemplos de cada juego dicen lo que se ve en pantalla`() {
    assertEquals("Ya repites 2 luces seguidas", Skill.levelLine("secuencia", 1))
    assertEquals("Ya repites 6 luces seguidas y 5 al revés", Skill.levelLine("secuencia", 11))
    assertEquals("Ya repites 8 luces seguidas y 7 al revés", Skill.levelLine("secuencia", 16))
    assertEquals("Ya resuelves cielos de 6 luces", Skill.levelLine("parejas", 1))
    assertEquals("pasar de 16 luces a 12 por los gemelos se dice, no se esconde", "Ya resuelves cielos de 12 luces con gemelos", Skill.levelLine("parejas", 7))
    assertEquals("Ya resuelves cielos de 24 luces con gemelos, parejas y tríos", Skill.levelLine("parejas", 18))
    assertEquals("Ya recuerdas 2 objetos en 6 escotillas", Skill.levelLine("bodega", 1))
    assertEquals("UN solo rasgo extra: la caja", "Ya recuerdas 4 objetos en 8 escotillas, con una caja que cambia", Skill.levelLine("bodega", 5))
    assertEquals("UN solo rasgo extra: el giro (el más nuevo)", "Ya recuerdas 4 objetos en 8 escotillas, con la bodega girando", Skill.levelLine("bodega", 8))
    assertEquals("Ya recuerdas 7 objetos en 10 escotillas, con la bodega girando", Skill.levelLine("bodega", 12))
    assertEquals("lo que la persona HACE: ordena cartas y cumple encargos", "Ya ordenas cartas en 2 buzones y cumples 1 encargo al día", Skill.levelLine("correo", 1))
    assertEquals("Ya ordenas cartas en 3 buzones y cumples 2 encargos al día", Skill.levelLine("correo", 3))
    assertEquals("Ya ordenas cartas en 4 buzones y cumples 5 encargos al día", Skill.levelLine("correo", 10))
    assertEquals("segundos y no milisegundos", "Ya rescatas las 2 cápsulas con un destello de 0,8 segundos", Skill.levelLine("radar", 1))
    assertEquals("Ya rescatas 3 de 4 cápsulas con un destello de 0,22 segundos", Skill.levelLine("radar", 8))
    assertEquals("Ya rescatas 3 de 4 cápsulas con un destello de 0,1 segundos", Skill.levelLine("radar", 12))
    assertEquals("Freno sin reloj: sin plazo", "Ya eliges entre 3 plataformas", Skill.levelLine("freno", 5))
    assertEquals("Freno con reloj: «y lanzas en 1,2 segundos» (1,18 con 2 cifras)", "Ya eliges entre 3 plataformas y lanzas en 1,2 segundos", Skill.levelLine("freno", 5, timed = true))
    assertEquals("Ya eliges entre 2 plataformas y lanzas en 1,4 segundos", Skill.levelLine("freno", 1, timed = true))
    assertEquals("Ya eliges entre 4 plataformas y lanzas en 0,86 segundos", Skill.levelLine("freno", 11, timed = true))
    assertEquals("Ya sigues 2 satélites a la vez, entre 6", Skill.levelLine("satelites", 1))
    assertEquals("Ya atrapas señales de 1,6 segundos mientras pilotas", Skill.levelLine("piloto", 1))
    assertEquals("Ya atrapas señales de 1,5 segundos mientras pilotas", Skill.levelLine("piloto", 2))
    assertEquals("desde el nivel 3, señales parecidas; sin «en los bordes»", "Ya atrapas señales parecidas de 1,4 segundos mientras pilotas", Skill.levelLine("piloto", 3))
    assertEquals("Ya atrapas señales parecidas de 1 segundo mientras pilotas", Skill.levelLine("piloto", 6))
    assertEquals("Ya atrapas señales parecidas de 0,65 segundos mientras pilotas", Skill.levelLine("piloto", 9))
    assertEquals("Ya respondes por el color de la tinta", Skill.levelLine("stroop", 1))
    assertEquals("sin «casi siempre» en el nivel 2", "Ya respondes por el color de la tinta", Skill.levelLine("stroop", 2))
    assertEquals("Ya alternas entre la tinta y la palabra", Skill.levelLine("stroop", 3))
    assertEquals("Ya alternas entre la tinta y la palabra sin aviso", Skill.levelLine("stroop", 4))
    assertEquals("Ya alternas entre la tinta y la palabra sin aviso", Skill.levelLine("stroop", 5))
    assertEquals("Ya giras piezas de 4 bloques, hasta un cuarto de vuelta", Skill.levelLine("acoplamiento", 1))
    assertEquals("Ya giras piezas de 4 bloques, hasta casi media vuelta", Skill.levelLine("acoplamiento", 2))
    assertEquals("Ya giras piezas de 5 bloques, hasta media vuelta", Skill.levelLine("acoplamiento", 4))
    assertEquals("Ya giras piezas de 5 bloques, en giros finos", Skill.levelLine("acoplamiento", 5))
    assertEquals("Ya giras piezas de 7 bloques, en giros finos", Skill.levelLine("acoplamiento", 12))
    assertEquals("Ya llegas a la carga con sumas", Skill.levelLine("calculo", 1))
    assertEquals("Ya llegas a la carga con las cuatro operaciones y más números", Skill.levelLine("calculo", 5))
    assertEquals("Ya arreglas máquinas de 1 pieza", Skill.levelLine("engranajes", 1))
    assertEquals("Ya arreglas máquinas de 1 pieza y 1 correa", Skill.levelLine("engranajes", 2))
    assertEquals("sin «hasta dos cambios»", "Ya arreglas máquinas de 3 piezas y 4 correas", Skill.levelLine("engranajes", 12))
    assertEquals("Ya ubicas números del 0 al 10, con marca en el medio", Skill.levelLine("aterrizaje", 1))
    assertEquals("Ya ubicas números negativos entre menos 50 y 50", Skill.levelLine("aterrizaje", 12))
    assertEquals("Ya encuentras palabras de 4 o 5 letras", Skill.levelLine("anagramas", 1))
    assertEquals("Ya encuentras palabras de 8 letras o más", Skill.levelLine("anagramas", 5))
    assertEquals("Ya reconoces palabras muy comunes", Skill.levelLine("meteoros", 1))
    assertEquals("Ya reconoces palabras raras", Skill.levelLine("meteoros", 12))
    assertEquals("Ya distingues verdades de disparates en frases cortas", Skill.levelLine("disparate", 1))
    assertEquals("Ya distingues verdades de disparates en frases que comparan dos cosas", Skill.levelLine("disparate", 12))
    assertEquals("Ya detectas intrusas de otro tipo de cosa", Skill.levelLine("intrusa", 1))
    assertEquals("Ya detectas intrusas con trampa en reglas menos obvias", Skill.levelLine("intrusa", 12))
  }

  @Test
  fun `donde el numero principal baja o se repite entre niveles vecinos la frase se distingue`() {
    // Constelaciones (luces que bajan al entrar los gemelos o los tríos) y Rescate: ninguna frase se repite en toda su escalera
    for (g in listOf("parejas", "radar")) {
      val lines = (1..Skill.ladder(g).levels).map { Skill.levelLine(g, it) }
      assertEquals("$g: frases repetidas ${lines.groupBy { it }.filter { it.value.size > 1 }.keys}", lines.size, lines.toSet().size)
    }
    // Freno con reloj: con 2 cifras significativas (regla de Opus) dos pares de niveles vecinos dicen lo mismo (1,3 y 1,1 segundos); las demás se distinguen
    val freno = (1..12).map { Skill.levelLine("freno", it, timed = true) }
    assertEquals(10, freno.toSet().size)
    // Bodega: con UN solo rasgo extra, los niveles 9 y 10 dicen lo mismo (5 objetos en 10 escotillas, la bodega gira); los demás se distinguen
    val bodega = (1..12).map { Skill.levelLine("bodega", it) }
    assertEquals(11, bodega.toSet().size); assertEquals(bodega[8], bodega[9])
    // la escalera de 12 de Meteoros, Disparate e Intrusa se lee de a pares (6 frases distintas)
    for (g in listOf("meteoros", "disparate", "intrusa")) assertEquals(g, 6, (1..12).map { Skill.levelLine(g, it) }.toSet().size)
  }

  @Test
  fun `la tarjeta no repite la cifra que ya dice la medida del juego`() {
    // Rastro (luces seguidas), Satélites (a la vez) y Rescate (el destello) hablan de LO MISMO en la misma unidad que su medida: si la medida está a la vista, solo va la medida
    assertEquals(setOf("secuencia", "satelites", "radar"), games.filter { LevelLines.overlapsMeasure(it) }.toSet())
    for (g in games.filter { LevelLines.overlapsMeasure(it) }) assertTrue(g, StarMeasures.defForGame(g) != null)
  }

  @Test
  fun `un nivel fuera de la escalera se acota y un juego desconocido avisa`() {
    assertEquals(Skill.levelLine("radar", 1), Skill.levelLine("radar", 0))
    assertEquals(Skill.levelLine("radar", 12), Skill.levelLine("radar", 99))
    val r = runCatching { LevelLines.lineAt("noexiste", 1) }
    assertTrue("un id que no es un juego no devuelve una frase inventada", r.isFailure)
  }

  /** Escribe las frases en `build/frases-ya.tsv` para la lámina de la tarea (`tools/previews/tarea_159.py`): «id \t nivel \t frase». */
  @Test
  fun `deja las frases en un archivo para la lamina de la tarea`() {
    val out = File("build/frases-ya.tsv")
    out.parentFile?.mkdirs()
    // Freno de Emergencia dice su plazo solo si la persona juega con reloj: va dos veces (id «freno» y «freno_reloj»)
    val rows = games.flatMap { g -> (1..Skill.ladder(g).levels).map { l -> "$g\t$l\t${Skill.levelLine(g, l)}" } } + (1..Skill.ladder("freno").levels).map { l -> "freno_reloj\t$l\t${Skill.levelLine("freno", l, timed = true)}" }
    out.writeText(rows.joinToString("\n"), Charsets.UTF_8)
    assertTrue(out.length() > 1000)
  }
}
