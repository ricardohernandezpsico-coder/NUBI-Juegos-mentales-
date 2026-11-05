package com.example.data

import com.example.model.AgeBand
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Los puntos por área (Tarea 159; `auditoria-fable/avance-b-puntos.md` §2 y §7): 0 a 1000, cada juego aporta hasta 1000 ÷ N, un juego sin partidas vale 0, la suma de los juegos cuadra exacta con el total,
 * el cambio se mide desde el lunes a las 00:00 y las 8 semanas repiten el valor en las semanas sin partidas.
 * (Tarea 230, 4-nov: las etapas Inicio · Aprendiz · Hábil · Experto · Maestro salieron de la app; quedan los puntos y la barra.)
 */
class AreaPointsTest {
  private val day = 86_400_000L
  private fun at(y: Int, m: Int, d: Int, h: Int = 12): Long = java.util.Calendar.getInstance().apply { clear(); set(y, m, d, h, 0, 0) }.timeInMillis
  /** Un miércoles al mediodía: «esta semana» empezó el lunes 19 a las 00:00. */
  private val now = at(2026, java.util.Calendar.OCTOBER, 21)
  private val monday = at(2026, java.util.Calendar.OCTOBER, 19, 0)

  private val memoria = listOf("parejas", "secuencia", "bodega", "correo")
  private val atencion = listOf("stroop", "piloto", "freno", "satelites", "radar")

  private fun p(id: String, time: Long, progress: Float) = ProgressPoint(id, time, progress)

  @Test
  fun `un juego sin partidas vale 0 y el area vacia da 0 puntos y aun sin medir`() {
    val a = AreaPoints.compute("MEMORIA", memoria, emptyList(), now)
    assertEquals(0, a.total)
    assertFalse(a.measured)
    assertEquals("Aún sin medir", AreaPoints.pointsLine(a))
    assertEquals(listOf(0, 0, 0, 0), a.perGame.map { it.points })
    assertEquals("cada juego aporta hasta 250", listOf(250, 250, 250, 250), a.perGame.map { it.max })
    assertEquals("Atención tiene 5 juegos: hasta 200 cada uno", listOf(200, 200, 200, 200, 200), AreaPoints.compute("ATENCION", atencion, emptyList(), now).perGame.map { it.max })
    // un solo juego en lo más alto de su escalera: 250 puntos y nada más, no 1000
    val one = AreaPoints.compute("MEMORIA", memoria, listOf(p("parejas", now - day, 1f)), now)
    assertEquals(250, one.total); assertEquals("250 de 1000 puntos", AreaPoints.pointsLine(one))
    assertEquals(listOf(250, 0, 0, 0), one.perGame.map { it.points })
    assertEquals(listOf(true, false, false, false), one.perGame.map { it.measured })
  }

  @Test
  fun `probar un juego nuevo solo puede sumar`() {
    val base = listOf(p("secuencia", now - 5 * day, 0.6f))
    val before = AreaPoints.compute("MEMORIA", memoria, base, now)
    val after = AreaPoints.compute("MEMORIA", memoria, base + p("correo", now, 0.2f), now)
    assertTrue("con el promedio de hoy esto bajaba de 60 a 40; con la suma sube", after.total > before.total)
  }

  @Test
  fun `la suma de los puntos de los juegos cuadra EXACTA con el total del area`() {
    val rnd = java.util.Random(159)
    repeat(2000) {
      val games = if (it % 2 == 0) memoria else atencion
      val log = games.map { g -> p(g, now - day, rnd.nextInt(1001) / 1000f) }
      val a = AreaPoints.compute("X", games, log, now)
      assertEquals("juegos ${log.map { l -> (l.progress * 1000).toInt() }}", a.total, a.perGame.sumOf { g -> g.points })
      assertTrue(a.perGame.all { g -> g.points in 0..g.max })
      assertTrue(a.total in 0..1000)
    }
    // el ejemplo de la especificación: 470 = 180 + 120 + 110 + 60
    val ex = AreaPoints.compute("MEMORIA", memoria, listOf(p("parejas", now, 0.72f), p("secuencia", now, 0.48f), p("bodega", now, 0.44f), p("correo", now, 0.24f)), now)
    assertEquals(470, ex.total); assertEquals(listOf(180, 120, 110, 60), ex.perGame.map { it.points })
  }

  @Test
  fun `el redondeo da el punto que sobra al juego de mayor resto`() {
    // 0,002 + 0,002 + 0,002 + 0,002 → cuatro veces 0,5 punto: el total redondea a 2 y los dos puntos van a los DOS primeros (iguales en resto)
    val a = AreaPoints.compute("MEMORIA", memoria, memoria.map { p(it, now, 0.002f) }, now)
    assertEquals(2, a.total); assertEquals(listOf(1, 1, 0, 0), a.perGame.map { it.points })
    // 0,003 / 0,001 / 0 / 0 → 1 punto en total y se lo lleva el de resto 3
    val b = AreaPoints.compute("MEMORIA", memoria, listOf(p("parejas", now, 0.001f), p("secuencia", now, 0.003f)), now)
    assertEquals(1, b.total); assertEquals(listOf(0, 1, 0, 0), b.perGame.map { it.points })
  }

  @Test
  fun `el area y el juego se dicen en puntos, sin etapa ni tramos`() {
    fun area(total: Int): AreaPoints = AreaPoints(area = "X", total = total, perGame = listOf(GamePoints("parejas", total, 1000, 1f, true)), weekDelta = 0, best = total)
    assertEquals("470 de 1000 puntos", AreaPoints.pointsLine(area(470)))
    assertEquals("1000 de 1000 puntos", AreaPoints.pointsLine(area(1000)))
    assertEquals("la ficha de un juego: los puntos del juego sobre su máximo", "180 de 250 puntos", GamePoints("parejas", 180, 250, 0.72f, true).pointsLine)
    assertEquals("90 de 200 puntos", GamePoints("radar", 90, 200, 0.45f, true).pointsLine)
    for (text in listOf(AreaPoints.pointsLine(area(470)), GamePoints("parejas", 180, 250, 0.72f, true).pointsLine, AreaPoints.pageSentence(area(470), "Memoria"))) {
      for (bad in listOf("Etapa", "etapa", "Inicio", "Aprendiz", "Hábil", "Experto", "Maestro", "Te faltan", "Te falta")) assertFalse("«$text» trae «$bad»", bad in text)
    }
  }

  @Test
  fun `Carga exacta anclada a sus 5 niveles de antes - nadie pierde puntos y los niveles 6 a 8 no suman (Tarea 233)`() {
    val razonamiento = AreaPoints.gamesOf("RAZONAMIENTO")
    assertTrue("calculo" in razonamiento)
    val max = 1000 / razonamiento.size                                  // lo que aporta cada juego del área: 250
    val age = AgeBand.ADULT
    fun total(progress: Float) = AreaPoints.compute("RAZONAMIENTO", razonamiento, listOf(p("calculo", now, progress)), now)
    // la misma persona antes y después de alargar: el 4,6 de 5 (avance 0,92, el que ya estaba en `progress_log`) es el 4,6 de 8 (rating 0,575)
    val before = total(0.92f)
    val after = total(Skill.progress("calculo", 4.6f / 8f, age))
    assertEquals("sus puntos no se mueven", before.total, after.total)
    assertEquals("4,6 de 5 en un juego de 250 puntos", 230, after.total)
    // en el tope de la v1 sigue con TODOS los puntos del juego; los niveles 6, 7 y 8 no suman
    for (rating in listOf(5f / 8f, 6f / 8f, 7f / 8f, 0.999f)) assertEquals("rating $rating", max, total(Skill.progress("calculo", rating, age)).total)
    assertEquals("el juego aporta hasta $max puntos y no más", max, total(Skill.progress("calculo", 0.999f, age)).perGame.first { it.gameId == "calculo" }.max)
    // el nivel sí sube con la escalera entera (Avance lo dice así): el 5 de antes, el 6 al pasar el tope de la v1 y el 8 arriba de todo
    assertEquals(5, LevelLines.levelOf("calculo", Skill.ladderProgress("calculo", 4.6f / 8f, age)))
    assertEquals(6, LevelLines.levelOf("calculo", Skill.ladderProgress("calculo", 5f / 8f, age)))
    assertEquals(8, LevelLines.levelOf("calculo", Skill.ladderProgress("calculo", 0.999f, age)))
  }

  @Test
  fun `el cambio de la semana usa el ultimo punto anterior al lunes y un juego estrenado suma entero`() {
    val log = listOf(
      p("parejas", monday - 20 * day, 0.30f), p("parejas", monday - 3_600_000L, 0.40f), p("parejas", monday + day, 0.48f),    // el domingo a las 23:00 ya valía 0,40
      p("bodega", monday + day, 0.20f)                                                                                        // estrenado esta semana
    )
    val a = AreaPoints.compute("MEMORIA", memoria, log, now)
    assertEquals("parejas en 0,48 y bodega en 0,20: (480 + 200) ÷ 4", 170, a.total)
    assertEquals("antes del lunes: solo parejas, en 0,40 → 100", 100, a.before)
    assertEquals(70, a.weekDelta)          // lo de parejas (+20) y el estreno de bodega entero (+50)
    // sin partidas esta semana: los puntos no cambiaron (Tarea 230: antes «te mantuviste», que se parece a «te quedas» de la liga de la semana)
    val quiet = AreaPoints.compute("MEMORIA", memoria, log.take(2), now)
    assertEquals(0, quiet.weekDelta)
    assertEquals("Esta semana, tus puntos de Memoria no cambiaron.", AreaPoints.weekLine(quiet, "Memoria"))
    // el resumen de la ruta (Tarea 198): lo que sumó cada juego, o «tus puntos no cambiaron» diciendo el área (nunca la palabra suelta «Hecho»)
    assertEquals("12 puntos en Atención", AreaPoints.gameLine(12, "Atención"))
    assertEquals("1 punto en Memoria", AreaPoints.gameLine(1, "Memoria"))
    assertEquals("Tus puntos de Razonamiento no cambiaron", AreaPoints.gameLine(0, "Razonamiento"))
    assertFalse("«te mantuviste» chocaría con «te quedas» de la liga", "mantuviste" in AreaPoints.gameLine(0, "Razonamiento") + AreaPoints.weekLine(quiet, "Memoria"))
    // el lunes a la mañana, sin partidas todavía: sin cambio
    assertEquals(0, AreaPoints.compute("MEMORIA", memoria, log.take(2), at(2026, java.util.Calendar.OCTOBER, 19, 8)).weekDelta)
    // «desde cuándo»: el resumen de la ruta de hoy mide desde su comienzo
    assertEquals(20, AreaPoints.compute("MEMORIA", memoria, log.take(3), now, weekStart = monday).weekDelta)
  }

  @Test
  fun `con MAX_DROP la bajada de una partida es como mucho 1000 x 0,05 entre N`() {
    for (games in listOf(memoria, atencion)) {
      val n = games.size
      for (start in listOf(0.9f, 0.55f, 0.2f)) {
        // el peor día posible: la partida termina en 0 y el rating baja lo que permite MAX_DROP
        val next = Skill.nextRating(start, 0f, PlayMode.A_TU_MEDIDA, false)
        val dropped = Skill.progress(games[0], start, null) - Skill.progress(games[0], next, null)
        assertTrue("baja como mucho 0,05 de avance (bajó $dropped)", dropped <= Skill.MAX_DROP + 1e-4f)
        val before = AreaPoints.compute("X", games, listOf(p(games[0], now - 2 * day, Skill.progress(games[0], start, null))), now)
        val after = AreaPoints.compute("X", games, listOf(p(games[0], now - 2 * day, Skill.progress(games[0], start, null)), p(games[0], now, Skill.progress(games[0], next, null))), now)
        val limit = Math.ceil(1000.0 * Skill.MAX_DROP / n).toInt()
        assertTrue("baja ${before.total - after.total} y el tope es $limit con $n juegos", before.total - after.total <= limit)
        assertTrue("la mejor marca se conserva", after.best >= before.total)
      }
    }
  }

  @Test
  fun `la bajada se dice sin culpa y con la mejor marca al lado`() {
    val log = listOf(p("parejas", monday - day, 0.80f), p("secuencia", monday - day, 0.60f), p("parejas", monday + day, 0.76f))
    val a = AreaPoints.compute("MEMORIA", memoria, log, now)
    assertEquals(-10, a.weekDelta)
    assertEquals("la mejor marca fue la del lunes (350)", 350, a.best); assertTrue(a.belowBest)
    assertEquals("Esta semana bajaste 10 puntos. Tu mejor marca sigue en 350 puntos.", AreaPoints.weekLine(a, "Memoria"))
    // Tarea 174: la fila de Avance no lleva la frase de la semana; vive en la página del área. Tarea 230: sin «Te faltan N puntos para X» delante, solo la semana, con la mejor marca al lado
    assertEquals("la frase de la página: la de la semana", "Esta semana bajaste 10 puntos. Tu mejor marca sigue en 350 puntos.", AreaPoints.pageSentence(a, "Memoria"))
    val all = AreaPoints.weekLine(a, "Memoria") + AreaPoints.pageSentence(a, "Memoria")
    for (bad in listOf("perdiste", "−", "▼", "↓", "↑", "+")) assertFalse("«$bad» no va", bad in all)
    assertEquals("Esta semana sumaste 1 punto.", AreaPoints.weekLine(a.copy(weekDelta = 1, best = a.total), "Memoria"))
    assertEquals("Todavía sin puntos: juega un juego de Memoria.", AreaPoints.weekLine(AreaPoints.compute("MEMORIA", memoria, emptyList(), now), "Memoria"))
  }

  @Test
  fun `el cuadro de un area en Avance se lee con su nombre y sus puntos, o aun sin medir`() {
    val a = AreaPoints.compute("MEMORIA", memoria, listOf(p("parejas", now, 0.72f), p("secuencia", now, 0.48f), p("bodega", now, 0.44f), p("correo", now, 0.24f)), now)
    assertEquals("Memoria, 470 de 1000 puntos", AreaPoints.cardSpoken(a, "Memoria"))
    assertEquals("Memoria, aún sin medir", AreaPoints.cardSpoken(AreaPoints.compute("MEMORIA", memoria, emptyList(), now), "Memoria"))
  }

  @Test
  fun `la frase de la pagina del area dice la semana y los puntos, sin lo que falta`() {
    val a = AreaPoints.compute("MEMORIA", memoria, listOf(p("parejas", now, 0.72f), p("secuencia", now, 0.48f), p("bodega", now, 0.44f), p("correo", now, 0.24f)), now)
    assertEquals("Esta semana sumaste 470 puntos.", AreaPoints.pageSentence(a, "Memoria"))
    assertEquals("470 de 1000 puntos", AreaPoints.pointsLine(a))
    val top = AreaPoints.compute("MEMORIA", memoria, memoria.map { p(it, now - 9 * day, 1f) }, now)
    assertEquals(1000, top.total); assertEquals("Esta semana, tus puntos de Memoria no cambiaron.", AreaPoints.pageSentence(top, "Memoria"))
    assertEquals("Aún sin medir", AreaPoints.pointsLine(AreaPoints.compute("MEMORIA", memoria, emptyList(), now)))
    assertEquals("Todavía sin puntos en Memoria: cada juego suma desde su primera partida a tu medida.", AreaPoints.pageSentence(AreaPoints.compute("MEMORIA", memoria, emptyList(), now), "Memoria"))
  }

  @Test
  fun `las 8 semanas repiten el valor en las semanas sin partidas y son menos con menos de 4 semanas`() {
    // 9 semanas atrás: 0,40 en un juego; una partida hace 3 semanas (0,60); nada más
    val log = listOf(p("parejas", Week.weekStart(now, 8) + day, 0.40f), p("parejas", Week.weekStart(now, 3) + day, 0.60f))
    val w = AreaPoints.weekly(memoria, log, now)
    assertEquals("8 valores: de hace 7 semanas a esta semana", 8, w.size)
    assertEquals(listOf(100, 100, 100, 100, 150, 150, 150, 150), w)
    // solo 2 semanas de datos: 2 valores (y la frase vacía)
    val short = listOf(p("parejas", Week.weekStart(now, 1) + day, 0.40f))
    assertEquals(listOf(100, 100), AreaPoints.weekly(memoria, short, now))
    assertEquals("Llevas 2 semanas en Memoria. El gráfico aparece cuando tengas 4.", AreaPoints.weeklyLine(2, "Memoria"))
    assertEquals("Llevas 1 semana en Memoria. El gráfico aparece cuando tengas 4.", AreaPoints.weeklyLine(1, "Memoria"))
    // esta semana apenas: 1 valor; sin partidas: ninguno
    assertEquals(listOf(100), AreaPoints.weekly(memoria, listOf(p("parejas", monday + 1000L, 0.40f)), now))
    assertTrue(AreaPoints.weekly(memoria, emptyList(), now).isEmpty())
    // el último valor es SIEMPRE el total de hoy
    assertEquals(AreaPoints.compute("MEMORIA", memoria, log, now).total, w.last())
    // una partida de otra área no cuenta
    assertTrue(AreaPoints.weekly(memoria, listOf(p("radar", now - day, 0.9f)), now).isEmpty())
  }

  @Test
  fun `la fila y la pagina dicen el avance con unidad y sin palabras sueltas`() {
    val a = AreaPoints.compute("MEMORIA", memoria, listOf(p("parejas", now - 8 * day, 0.40f), p("parejas", now, 0.60f)), now)
    for (text in listOf(AreaPoints.weekLine(a, "Memoria"), AreaPoints.pageSentence(a, "Memoria"), AreaPoints.pointsLine(a), AreaPoints.gamesHint(250), AreaPoints.UPS_AND_DOWNS)) {
      assertFalse("«$text» tiene «de 100»", " de 100" in text && " de 1000" !in text)
      for (bad in listOf("pts", "sem.", "→", "▲", "▼", "+")) assertFalse("«$text» trae «$bad»", bad in text)
    }
    assertEquals("Cada juego aporta hasta 200 puntos. Suben cuando llegas a niveles más altos.", AreaPoints.gamesHint(200))
  }
}
