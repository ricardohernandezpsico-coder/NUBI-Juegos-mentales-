package com.example.games

import com.example.data.FinalesConSentido
import com.example.data.MeasurePoint
import com.example.data.PuntaEntry
import com.example.data.PuntaTier
import com.example.model.GamePlayResult
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** Los finales con sentido del grupo 1 (Memoria): lo que hiciste con UN dato tuyo, tu avance con su unidad, el truco (condicional o general) y por qué importa con su fuente. */
class FinalModelsTest {
  private fun result(id: String, block: GamePlayResult.() -> GamePlayResult = { this }) =
    GamePlayResult(gameId = id, score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 3, endRating = 0.5f, timestamp = 1_000L).block()

  private fun points(key: String, vararg values: Float) = values.mapIndexed { i, v -> MeasurePoint(100L * (i + 1), key, v, 0.5f, false) }

  private val constelaciones = result("parejas") { copy(correctAnswers = 7, totalTrials = 9, conGroup = 4, conGroups = 6, conMemGroups = 4, conBestStreak = 5, conBest = 6, conNewRecord = false) }
  private val rastro = result("secuencia") { copy(correctAnswers = 14, totalTrials = 19, rasBestLen = listOf(5, 4, 3, 2), rasRounds = listOf(7, 4, 5, 3), rasHits = listOf(7, 3, 2, 2), rasModesSeen = 15, rasNewModes = 0) }
  private val bodega = result("bodega") { copy(correctAnswers = 9, totalTrials = 12, bodGroup = 3, bodBestStreak = 4, bodBiggest = 5, bodBest = 6, bodNewRecord = false, bodMs = 4500) }
  private val correo = result("correo") { copy(correctAnswers = 8, totalTrials = 11, mailGroup = 2, mailEvHits = 4, mailEvTotal = 4, mailTimeHits = 1, mailTimeTotal = 2, mailCommissions = 0, mailPeeks = 3, mailPeeksGood = 1, mailRight = 6, mailBest = 9, mailNewRecord = false) }

  @Test
  fun `solo los juegos ya llevados al final con sentido lo usan`() {
    for (r in listOf(constelaciones, rastro, bodega, correo)) assertTrue(r.gameId, FinalModels.usesMeaningful(r))
    assertTrue(FinalModels.usesMeaningful(result("aterrizaje") { copy(numlineErrorPct = 8f) }))
    // sin los datos propios del juego (una partida de la versión vieja de Unity) se queda con el final de siempre
    for (id in listOf("parejas", "secuencia", "bodega", "correo", "aterrizaje")) assertTrue(id, !FinalModels.usesMeaningful(result(id)))
    for (id in listOf("stroop", "piloto", "freno", "satelites", "radar", "calculo", "engranajes", "acoplamiento", "anagramas", "meteoros", "disparate", "intrusa")) assertNull(id, FinalModels.forGame(result(id), emptyList()))
    assertNotNull(FinalModels.forGame(constelaciones, emptyList()))
  }

  @Test
  fun `Constelaciones dice el porcentaje, lo que paso y tu racha, y su truco condicional si algo se escapo`() {
    val m = FinalModels.forGame(constelaciones, emptyList())!!
    assertEquals("Tu memoria de lugar", m.boxTitle)
    assertEquals("78 %", m.headline)
    assertEquals("de las parejas vistas, de memoria", m.headlineUnit)
    assertEquals("UN dato tuyo: la racha", "Más seguidos de memoria: 5", m.dataLine)
    assertEquals("se escapó alguna: el truco condicional", "Da vuelta primero una luz nueva.", m.trick)
    // sin nada que se escapara: el truco general de la tabla
    val perfect = FinalModels.forGame(constelaciones.copy(correctAnswers = 9), emptyList())!!
    assertEquals("Da vuelta primero una luz nueva: te muestra más del cielo.", perfect.trick)
    assertEquals("Pusiste en juego la memoria de lugar: recordar dónde quedó cada cosa. Recordar qué va con qué, como una cosa y su lugar, es la parte de la memoria que más cambia con los años.", m.why)
    assertEquals("Fuente: Old y Naveh-Benjamin, 2008", m.source)
    assertEquals("Medida de esta partida. No es un diagnóstico.", m.note)
    // el récord nuevo lo dice el sello de arriba, no la tarjeta (ver `un record se dice UNA vez`)
  }

  @Test
  fun `solo el pie de Rastro de luz dice que el resultado puede variar de una partida a otra`() {
    // Tarea 186 (5b, Opus 28-oct): una partida son pocas rondas y el Corsi en pantalla es poco estable al repetirlo
    assertEquals("Resultado de esta partida. Puede variar de una a otra. No es un diagnóstico.", FinalModels.forGame(rastro, emptyList())!!.note)
    for (r in listOf(constelaciones, bodega, correo)) assertEquals("el pie de ${r.gameId} sigue siendo el de siempre", "Medida de esta partida. No es un diagnóstico.", FinalModels.forGame(r, emptyList())!!.note)
  }

  @Test
  fun `Constelaciones dice lo mismo sobre los años en el final, en la ficha y en En que se basa Nubi`() {
    // Tarea 187 (Opus, 28-oct): el «¿Por qué importa?» decía «…que más atención pide con los años»; Ricardo aprobó «…lo que más cambia con los años» (Old y Naveh-Benjamin, 2008, el déficit asociativo)
    val copy = com.example.data.FinalesConSentido.copy("parejas")!!
    assertTrue(copy.whyText, copy.whyText.endsWith("es la parte de la memoria que más cambia con los años."))
    assertTrue(copy.sheetText, copy.sheetText.endsWith("es la parte de la memoria que más cambia con los años."))
    assertTrue(com.example.data.Fuentes.byGame.getValue("parejas").taken.contains("más cambia con los años"))
    for (id in com.example.model.GameRegistry.allGames.map { it.id }) {
      val c = com.example.data.FinalesConSentido.copy(id) ?: continue
      assertTrue("«más atención pide» ya no se dice de $id", !c.whyText.contains("más atención pide") && !c.sheetText.contains("más atención pide"))
    }
  }

  @Test
  fun `Constelaciones sin oportunidades no inventa un porcentaje`() {
    val m = FinalModels.forGame(constelaciones.copy(correctAnswers = 0, totalTrials = 0), listOf(MeasurePoint(100L, "place", 60f, 0.5f, false)))!!
    assertEquals("—", m.headline)
    assertEquals("Sin oportunidades de memoria en esta partida.", m.headlineUnit)
    assertEquals("ya no hay cuadros: los dos números van dentro de la frase", emptyList<Any>(), m.chips)
    assertEquals("sin medida hoy no hay frase: la unidad ya dice por qué", "", m.progressPhrase)
  }

  @Test
  fun `Rastro dice las luces mas largas, lo que mas costo y el truco del modo que mas costo`() {
    val m = FinalModels.forGame(rastro, emptyList())!!
    assertEquals("Tu rastro", m.boxTitle)
    assertEquals("5 luces", m.headline)
    assertEquals("seguidas, bien repetidas", m.headlineUnit)
    assertEquals("Con el giro hay que recordar el orden y no el lugar: tus luceros se mueven.", m.dataLine)
    assertEquals("Sigue a un lucero por su color mientras el cielo gira; el orden va con los luceros, no con el lugar.", m.trick)
    // sin modo marcado como el que más costó: el truco general
    val simple = FinalModels.forGame(rastro.copy(rasRounds = listOf(7, 0, 0, 0), rasHits = listOf(7, 0, 0, 0)), emptyList())!!
    assertEquals("Junta las luces de a dos o tres, como cuando dictas un número de teléfono.", simple.trick)
    assertNull(simple.dataLine)
    // un modo nuevo desbloqueado es un AVISO (una píldora), no un dato del detalle; el dato de la partida se queda
    val unlocked = FinalModels.forGame(rastro.copy(rasNewModes = 2), emptyList())!!
    assertEquals(listOf("Modo nuevo: Al revés"), unlocked.notices)
    assertEquals("Con el giro hay que recordar el orden y no el lugar: tus luceros se mueven.", unlocked.dataLine)
    assertEquals(emptyList<String>(), m.notices)
    assertEquals(listOf("Modos nuevos: Al revés, En marcha"), FinalModels.forGame(rastro.copy(rasNewModes = 2 + 8), emptyList())!!.notices)
    assertEquals("Fuente: Baddeley, 2003", m.source)
  }

  @Test
  fun `Rastro sin rastro completo lo dice sin culpa`() {
    val m = FinalModels.forGame(rastro.copy(rasBestLen = listOf(0, 0, 0, 0), rasRounds = listOf(2, 0, 0, 0), rasHits = listOf(0, 0, 0, 0)), emptyList())!!
    assertEquals("—", m.headline)
    assertTrue(m.headlineUnit.isNotBlank())
    assertEquals("", m.progressPhrase)
  }

  @Test
  fun `Bodega dice los objetos al primer intento, tu bodega mas grande y el truco de la escotilla`() {
    val m = FinalModels.forGame(bodega, emptyList())!!
    assertEquals("Tu bodega", m.boxTitle)
    assertEquals("9 de 12 objetos", m.headline)
    assertEquals("al primer intento", m.headlineUnit)
    assertEquals("Tu bodega más grande hoy: 5 objetos sin errores", m.dataLine)
    assertEquals("Imagina cada objeto dentro de su escotilla y dile su nombre en voz baja.", m.trick)
    assertEquals("Fuente: Postma, Kessels y van Asselen, 2008", m.source)
    assertEquals("primera partida: la frase dice que la próxima te comparas contigo", FinalesConSentido.PHRASE_FIRST, m.progressPhrase)
    assertEquals(emptyList<Any>(), m.chips)
  }

  @Test
  fun `Correo dice los encargos cumplidos, la mirada al reloj y el truco segun lo que se escapo`() {
    val m = FinalModels.forGame(correo, emptyList())!!
    assertEquals("Tu memoria para lo pendiente", m.boxTitle)
    assertEquals("8 de 11 encargos", m.headline)
    assertEquals("cumplidos a tiempo", m.headlineUnit)
    // la frase de avance sigue en % (es lo que se guarda)
    assertEquals("Como tu promedio (70 % de los encargos): vas parejo. Tu mejor: 80 % de los encargos.", FinalModels.forGame(correo, points("estacion", 60f, 70f, 80f))!!.progressPhrase)
    assertEquals("Miradas al reloj cerca de la hora: 1 de 3", m.dataLine)
    assertEquals("se escapó una hora y miró poco el reloj", "Mira el reloj cuando se acerque la hora.", m.trick)
    // todo salió bien: el truco general
    val ok = FinalModels.forGame(correo.copy(mailEvHits = 4, mailTimeHits = 2, mailPeeksGood = 3), emptyList())!!
    assertEquals("Antes de empezar, dilo en voz baja: «cuando vea…, haré…».", ok.trick)
    // las cartas señal: el truco nombra la señal que se escapó (etapa 2: sello dorado; etapa 5: lazo)
    val gold = FinalModels.forGame(correo.copy(mailGroup = 1, mailEvHits = 1, mailEvTotal = 2, mailTimeHits = 0, mailTimeTotal = 0, mailCommissions = 0, mailPeeks = 0, mailPeeksGood = 0, mailGoldMissed = 1, mailLazoMissed = 0), emptyList())!!
    assertEquals("Repite «cuando vea un sello dorado, caja fuerte».", gold.trick)
    val lazo = FinalModels.forGame(correo.copy(mailGroup = 3, mailEvHits = 0, mailEvTotal = 2, mailTimeHits = 0, mailTimeTotal = 0, mailCommissions = 0, mailPeeks = 0, mailPeeksGood = 0, mailGoldMissed = 0, mailLazoMissed = 2), emptyList())!!
    assertEquals("Repite «cuando vea un lazo, caja fuerte».", lazo.trick)
    assertEquals("Fuente: Hering y otros, 2014", m.source)
  }

  // ------------------------------------------------------------------ grupo 2 (Atención)

  private val tinta = result("stroop") { copy(correctAnswers = 20, totalTrials = 24, interferenceMs = 420, switchCostMs = 180) }
  private val piloto = result("piloto") { copy(pilSignalPct = 58, pilHits = 14, pilTargets = 18, pilFalse = 2, pilLanePct = 82, pilSignalLevel = 6, pilBestStreak = 7, pilPoints = 1250) }
  private val freno = result("freno") { copy(brakeMs = 245, stopsOk = 6, stopsTotal = 8, brakeBestSsdMs = 180) }
  private val satelites = result("satelites") { copy(trackingCapacity = 2.6f, trackingTargets = 3.5f, satLights = 12, satPerfect = 5, satBestStreak = 3, satBest = 22, satNewRecord = false, totalTrials = 8) }
  private val rescate = result("radar") { copy(glanceMs = 170, glanceLoad = 3f, rescRescued = 23, rescBest = 31, rescNewRecord = false, captureK = 3.5f, rescPerfect = 5, rescRounds = 12, rescBestStreak = 3, rescShortestMs = 150, rescTrips = 2) }

  @Test
  fun `los juegos del grupo 2 usan el final con sentido solo con sus datos propios`() {
    for (r in listOf(tinta, piloto, freno, satelites, rescate)) assertTrue(r.gameId, FinalModels.usesMeaningful(r))
    for (id in listOf("stroop", "piloto", "freno", "satelites", "radar")) assertTrue(id, !FinalModels.usesMeaningful(result(id)))
    assertTrue("la partida del juego anterior de Satélites (sin luces) también", FinalModels.usesMeaningful(result("satelites") { copy(trackingCapacity = 2.2f) }))
  }

  @Test
  fun `Tinta o Palabra dice cuanto te freno la palabra en palabras, el costo de cambio y el truco condicional`() {
    val m = FinalModels.forGame(tinta, emptyList())!!
    assertEquals("Cuánto te frenó la palabra", m.boxTitle)
    assertEquals("0,4 segundos", m.headline)
    assertEquals("de demora cuando la palabra te frenó", m.headlineUnit)
    assertEquals("Cambiar de orilla te costó: 0,2 segundos más", m.dataLine)
    assertEquals("la diferencia es de 0,3 s o más: el truco condicional", "Mira primero de qué orilla llega y recién después la palabra: la orilla te dice qué tocar.", m.trick)
    val small = FinalModels.forGame(tinta.copy(interferenceMs = 80, switchCostMs = null), emptyList())!!
    assertEquals("0,1 segundos", small.headline)
    assertNull(small.dataLine)
    assertEquals("Mira primero de qué orilla llega y recién después la palabra.", small.trick)
    assertEquals("casi nada", FinalModels.forGame(tinta.copy(interferenceMs = 20), emptyList())!!.headline)
    assertEquals("Pusiste en juego lo que usas para frenar la lectura automática y cambiar de regla cuando cambia la orilla. Frenar lo automático y adaptarse cuando cambian las reglas son parte de lo que nos permite pensar antes de actuar.", m.why)
    assertEquals("Fuente: Diamond, 2013", m.source)
  }

  @Test
  fun `Tinta o Palabra compara en decimas de segundo, menos es mejor y la tolerancia es de 0,1 s`() {
    val prev = points("stroop", 300f, 500f, 400f)         // promedio 400 ms, mejor (más bajo) 300 ms
    val m = FinalModels.forGame(tinta, prev)!!              // hoy 420 ms
    assertEquals("Como tu promedio: vas parejo. Tu mejor: 0,3 segundos más.", m.progressPhrase)
    assertSealed(FinalModels.forGame(tinta.copy(interferenceMs = 250), prev)!!)
    // mejor que el promedio por más de 0,1 s, sin superar tu mejor: antes 250, 500, 600 y 400 ms (promedio 437 ms)
    assertTrue(FinalModels.forGame(tinta.copy(interferenceMs = 300), points("stroop", 250f, 500f, 600f, 400f))!!.progressPhrase.startsWith("Mejor que tu promedio (0,4 segundos más)."))
    assertTrue("a 0,08 s del promedio todavía es parejo", FinalModels.forGame(tinta.copy(interferenceMs = 320), prev)!!.progressPhrase.startsWith("Como tu promedio (0,4 segundos más): vas parejo."))
    assertEquals("Un poco bajo tu promedio (0,4 segundos más): es normal que varíe.", FinalModels.forGame(tinta.copy(interferenceMs = 560), prev)!!.progressPhrase)
  }

  @Test
  fun `Piloto dice las senales a los mandos, la mision y su consejo si lo hay`() {
    val m = FinalModels.forGame(piloto, emptyList())!!
    assertEquals("Tus señales a los mandos", m.boxTitle)
    assertEquals("58 %", m.headline)
    assertEquals("de las señales, a los mandos", m.headlineUnit)
    assertEquals("Señales de tu misión: 14 de 18", m.dataLine)
    assertEquals("Al cruzar cada arco, repite por dentro cuál es la señal de tu misión.", m.trick)
    val off = FinalModels.forGame(piloto.copy(pilLanePct = 60), emptyList())!!
    assertTrue(off.trick, off.trick.startsWith("La nave se salió bastante de la ruta"))
    val few = FinalModels.forGame(piloto.copy(pilSignalPct = null), emptyList())!!
    assertEquals("—", few.headline)
    assertTrue(few.headlineUnit.contains("se necesitan 8"))
    assertEquals(FinalesConSentido.PHRASE_FIRST, m.progressPhrase)
    assertTrue(m.why.startsWith("Pusiste en juego lo que usas para atender dos cosas a la vez"))
  }

  @Test
  fun `Freno se dice en zonas y nunca en milisegundos`() {
    val prev = points("brake", 260f, 250f, 240f, 270f)
    val m = FinalModels.forGame(freno, prev)!!
    assertEquals("Tu freno", m.boxTitle)
    assertEquals("zona firme", m.headline)
    assertEquals("tu freno, en 5 partidas", m.headlineUnit)
    assertEquals("Frenaste 6 de 8 altos", m.dataLine)
    assertEquals("lo que repite la cifra de hoy se omite (Opus, 21-oct)", "Como tu promedio: vas parejo.", m.progressPhrase)
    val all = listOfNotNull(m.headline, m.headlineUnit, m.dataLine, m.progressPhrase, m.trick, m.spoken)
    for (t in all) assertTrue("«$t» dice milisegundos", !Regex("\\d\\s*ms").containsMatchIn(t))
    assertEquals("Lanza sin esperar el ALTO y frena solo si aparece: esperarlo te vuelve más lento.", m.trick)
    val faster = FinalModels.forGame(freno.copy(brakeMs = 200), points("brake", 260f, 250f, 240f, 210f))!!.progressPhrase
    assertEquals("hoy ES la zona ágil de tu mejor: no se repite", "Mejor que tu promedio (zona firme).", faster)
    assertEquals("la primera lectura se dice en palabras", "tu freno, primera lectura", FinalModels.forGame(freno.copy(brakeMs = 340), emptyList())!!.headlineUnit)
    assertEquals("zona pausado", FinalModels.forGame(freno.copy(brakeMs = 340), emptyList())!!.headline)
  }

  @Test
  fun `Freno sin estimacion lo dice sin culpa y su truco hace que la medida funcione`() {
    val m = FinalModels.forGame(freno.copy(brakeMs = null), emptyList())!!
    assertEquals("—", m.headline)
    assertEquals("Sin estimación: hacen falta 6 altos y frenar entre 1 de cada 4 y 3 de cada 4.", m.headlineUnit)
    assertEquals("Frenaste 6 de 8 altos", m.dataLine)
    assertEquals("Lanza apenas se encienda la luz, sin esperar al ALTO: así la medida funciona.", m.trick)
    assertEquals("", m.progressPhrase)
  }

  @Test
  fun `Satelites dice cuantos seguiste de verdad a la vez y las luces como dato tuyo`() {
    val m = FinalModels.forGame(satelites, points("tracking", 2.2f, 2.4f, 2.9f))!!
    assertEquals("Tu seguimiento", m.boxTitle)
    assertEquals("2,6 satélites", m.headline)
    assertEquals("que seguiste a la vez", m.headlineUnit)
    assertEquals("Encendiste 12 luces", m.dataLine)
    assertEquals("Como tu promedio (2,5 satélites): vas parejo. Tu mejor: 2,9 satélites.", m.progressPhrase)
    assertTrue("«seguidos» es de los aciertos en fila, no de los satélites", !m.headlineUnit.contains("seguidos"))
    assertEquals("Une con la mirada los satélites que sigues en una figura, como un triángulo, y sigue la figura.", m.trick)
    assertTrue(m.why.startsWith("Pusiste en juego lo que usas para seguir con la vista varias cosas"))
  }

  @Test
  fun `Rescate dice tu vistazo y las capsulas a salvo, y menos milisegundos es mejor`() {
    val m = FinalModels.forGame(rescate, points("glance", 200f, 190f, 180f))!!
    assertEquals("Tu vistazo", m.boxTitle)
    assertEquals("0,17 segundos", m.headline)
    assertEquals("de vistazo te bastan", m.headlineUnit)
    assertEquals("23 cápsulas a salvo", m.dataLine)
    assertEquals("Como tu promedio (0,19 segundos): vas parejo.", m.progressPhrase)
    assertSealed(m)                                                                                  // más rápido que tu mejor anterior: el sello lo dice, «Tu avance» usa la comparación normal
    assertEquals("Mira el centro del radar y, apenas se apague, nombra por dentro las formas que viste.", m.trick)
    assertEquals("—", FinalModels.forGame(rescate.copy(glanceMs = null), emptyList())!!.headline)
  }

  @Test
  fun `todos los textos de los finales del grupo 2 cumplen las reglas de vocabulario`() {
    for (r in listOf(tinta, piloto, freno, satelites, rescate)) {
      val m = FinalModels.forGame(r, points("x", 60f, 70f))!!
      val all = listOfNotNull(m.boxTitle, m.headline, m.headlineUnit, m.dataLine, m.progressPhrase, m.trick, m.why, m.source, m.note, m.spoken).joinToString(" ").lowercase()
      for (bad in listOf("entren", "cerebro", "cognitiv", "percentil", "otras personas")) assertTrue("${r.gameId}: «$bad»", !all.contains(bad))
    }
  }

  // ------------------------------------------------------------------ grupo 3 (Razonamiento)

  private val carga = result("calculo") { copy(correctAnswers = 5, totalTrials = 8, level = 4, cargaAlone = 5, cargaHinted = 2, cargaShort = 3, cargaMs = 14_000) }
  private val engranajes = result("engranajes") { copy(correctAnswers = 7, totalTrials = 10, engrEtapa = 3, engrLaunches = 1, engrOrbit = 3, engrLights = 0, engrMs = 7500) }
  private val acoplamiento = result("acoplamiento") { copy(rotationSpeedDps = 212, rotationCurveMs = listOf(900, 1100, 1400, 1750, 2100), dockDocked = 24, dockRings = 3, dockBest = 31, dockNewRecord = false) }

  @Test
  fun `los juegos del grupo 3 usan el final con sentido solo con sus datos propios`() {
    for (r in listOf(carga, engranajes, acoplamiento)) assertTrue(r.gameId, FinalModels.usesMeaningful(r))
    for (id in listOf("calculo", "engranajes", "acoplamiento")) assertTrue(id, !FinalModels.usesMeaningful(result(id)))
    assertTrue("sin la medida de giro pero con módulos acoplados también", FinalModels.usesMeaningful(result("acoplamiento") { copy(dockDocked = 5) }))
  }

  @Test
  fun `Carga exacta dice las cargas sin pista, el camino corto y su truco segun el nivel`() {
    val m = FinalModels.forGame(carga, emptyList())!!
    assertEquals("Tu reactor", m.boxTitle)
    assertEquals("5 de 8 cargas", m.headline)
    assertEquals("sin pista", m.headlineUnit)
    assertEquals("Camino corto en 3 de esas 5", m.dataLine)
    assertEquals("Mira primero si multiplicar dos celdas te deja cerca de la carga; después ajusta sumando o restando.", m.trick)
    // nivel bajo y alguna con pista: el truco condicional es otro
    assertEquals("Mira cuánto le falta a la celda más grande para llegar a la carga y busca otra celda que lo complete.", FinalModels.forGame(carga.copy(level = 2), emptyList())!!.trick)
    // sin camino corto (menos de 3 sin pista) el dato es el desglose
    assertEquals("2 sin pista · 3 con pista", FinalModels.forGame(carga.copy(correctAnswers = 2, totalTrials = 5, cargaAlone = 2, cargaHinted = 3, cargaShort = null), emptyList())!!.dataLine)
    assertEquals("Fuente: Reyna y otros, 2009", m.source)
    assertTrue(m.why.startsWith("Pusiste en juego lo que usas para hacer cuentas con flexibilidad para llegar a un número."))
  }

  @Test
  fun `Carga exacta compara en porcentaje con 10 puntos de tolerancia`() {
    val prev = points("carga", 50f, 75f, 62.5f)
    val m = FinalModels.forGame(carga, prev)!!               // hoy 62,5 %
    assertEquals("Como tu promedio: vas parejo. Tu mejor: 75 % de las cargas.", m.progressPhrase)
    assertSealed(FinalModels.forGame(carga.copy(correctAnswers = 7, cargaAlone = 7), prev)!!)
  }

  @Test
  fun `Engranajes dice las maquinas arregladas y lo que paso con el cohete`() {
    val m = FinalModels.forGame(engranajes, emptyList())!!
    assertEquals("Tu cohete", m.boxTitle)
    assertEquals("7 de 10 máquinas", m.headline)
    assertEquals("arregladas", m.headlineUnit)
    assertEquals("el despegue es un AVISO (una píldora), no un dato del detalle", listOf("¡Despegó tu cohete número 3!"), m.notices)
    assertNull(m.dataLine)
    assertEquals("Faltan 4 luces: tu cohete espera en el hangar", FinalModels.forGame(engranajes.copy(engrLaunches = 0, engrLights = 6), emptyList())!!.dataLine)
    assertEquals(emptyList<String>(), FinalModels.forGame(engranajes.copy(engrLaunches = 0, engrLights = 6), emptyList())!!.notices)
    assertEquals("Antes de cambiar algo, mira qué piezas quedan después: lo que tocas antes de una rama mueve todo lo que sigue.", m.trick)
    assertEquals("Fuente: Hegarty, 2004", m.source)
    assertEquals(FinalesConSentido.PHRASE_FIRST, m.progressPhrase)
  }

  @Test
  fun `Acoplamiento dice el giro mental en grados por segundo y los modulos acoplados`() {
    val m = FinalModels.forGame(acoplamiento, points("rotation", 180f, 200f, 260f))!!
    assertEquals("Tu giro mental", m.boxTitle)
    assertEquals("212 grados", m.headline)
    assertEquals("24 módulos acoplados", m.dataLine)
    assertEquals("por segundo, de giro en tu mente", m.headlineUnit)
    assertEquals("Como tu promedio (213 grados por segundo): vas parejo. Tu mejor: 260 grados por segundo.", m.progressPhrase)
    assertEquals("Elige una parte que destaque, como una punta o un codo, y gira solo esa en tu mente.", m.trick)
    assertEquals("Mejor que tu promedio (227 grados por segundo). Tu mejor: 300 grados por segundo.", FinalModels.forGame(acoplamiento.copy(rotationSpeedDps = 270), points("rotation", 180f, 200f, 300f))!!.progressPhrase)
    assertSealed(FinalModels.forGame(acoplamiento.copy(rotationSpeedDps = 320), points("rotation", 180f, 200f, 300f))!!)
    val none = FinalModels.forGame(acoplamiento.copy(rotationSpeedDps = null, rotationCurveMs = null), emptyList())!!
    assertEquals("—", none.headline)
    assertEquals("Se calcula con 8 aciertos en 3 ángulos y 7 de cada 10 bien.", none.headlineUnit)
    assertEquals("24 módulos acoplados", none.dataLine)
  }

  @Test
  fun `todos los textos de los finales del grupo 3 cumplen las reglas de vocabulario`() {
    for (r in listOf(carga, engranajes, acoplamiento)) {
      val m = FinalModels.forGame(r, points("x", 60f, 70f))!!
      val all = listOfNotNull(m.boxTitle, m.headline, m.headlineUnit, m.dataLine, m.progressPhrase, m.trick, m.why, m.source, m.note, m.spoken).joinToString(" ").lowercase()
      for (bad in listOf("entren", "cerebro", "cognitiv", "percentil", "otras personas")) assertTrue("${r.gameId}: «$bad»", !all.contains(bad))
    }
  }

  // ------------------------------------------------------------------ grupo 4 (Lenguaje)

  private val punta = result("anagramas") { copy(puntaSolo = 5, puntaPista = 2, puntaLetras = 0, puntaVista = 1, puntaMs = 6500, puntaWords = listOf(PuntaEntry("luna", PuntaTier.SOLO), PuntaEntry("barco", PuntaTier.PISTA), PuntaEntry("nube", PuntaTier.VISTA))) }
  private val meteoros = result("meteoros") { copy(lexBandSeen = listOf(8, 8, 8, 8, 8, 8), lexBandHits = listOf(8, 8, 7, 6, 5, 3), lexFaSeen = listOf(7, 7, 7), lexFaHits = listOf(0, 1, 2), lexRtCommonMs = 700, lexRtRareMs = 1100, lexRareWords = listOf("alcayata", "badil")) }
  private val disparate = result("disparate") { copy(svSeenType = listOf(8, 8, 8, 8, 8, 8), svRtType = listOf(900, 950, 1300, 1500, 1100, 1000), svWpm = 142, svSubtleHits = 6, svSubtleSeen = 8, svBestStreak = 14) }
  private val intrusa = result("intrusa") { copy(intrSeenType = listOf(3, 3, 2, 2, 2, 2), intrHitsType = listOf(3, 2, 2, 2, 1, 1), intrBonusSeen = 5, intrBonusHits = 3, intrRtMs = 1800) }

  @Test
  fun `los juegos del grupo 4 usan el final con sentido solo con sus datos propios`() {
    for (r in listOf(punta, meteoros, disparate, intrusa)) assertTrue(r.gameId, FinalModels.usesMeaningful(r))
    for (id in listOf("anagramas", "meteoros", "disparate", "intrusa")) assertTrue(id, !FinalModels.usesMeaningful(result(id)))
  }

  @Test
  fun `todos los juegos con medida usan ya el final con sentido`() {
    val ids = listOf(punta, meteoros, disparate, intrusa, tinta, piloto, freno, satelites, rescate, carga, engranajes, acoplamiento, constelaciones, rastro, bodega, correo, result("aterrizaje") { copy(numlineErrorPct = 8f) }).map { it.gameId }
    assertEquals("los 17 juegos de la app", 17, ids.toSet().size)
    assertEquals("los mismos que el registro", com.example.model.GameRegistry.allGames.map { it.id }.toSet(), ids.toSet())
    for (id in ids) assertNotNull(id, com.example.data.StarMeasures.defForGame(id))
  }

  @Test
  fun `En la punta de la lengua dice las palabras por tu cuenta y su truco condicional`() {
    val m = FinalModels.forGame(punta, emptyList())!!
    assertEquals("Tu cielo de palabras", m.boxTitle)
    assertEquals("5 de 8 palabras", m.headline)
    assertEquals("por tu cuenta", m.headlineUnit)
    assertEquals("Tardaste 6,5 segundos en decir «¡La tengo!» en las que salieron solas", m.dataLine)
    assertEquals("Si una no sale, piensa en cómo empieza o en otra parecida: suele destrabarla.", m.trick)
    assertEquals("las azules vuelven: es el dato cuando no hay tiempo que decir", "Las azules vuelven en otra partida.", FinalModels.forGame(punta.copy(puntaSolo = 1, puntaPista = 2, puntaVista = 1), emptyList())!!.dataLine)
    // todas solas: no hay nada que aconsejar y el truco que sale es el general de la tabla (el mismo texto: siempre hay truco)
    val allAlone = FinalModels.forGame(punta.copy(puntaSolo = 8, puntaPista = 0, puntaLetras = 0, puntaVista = 0), emptyList())!!
    assertEquals("Si una no sale, piensa en cómo empieza o en otra parecida: suele destrabarla.", allAlone.trick)
    assertEquals(FinalesConSentido.PHRASE_FIRST, m.progressPhrase)
    assertEquals("Fuente: Shafto y otros, 2007", m.source)
    assertTrue(m.why.startsWith("Pusiste en juego lo que usas para encontrar una palabra que conoces y que no sale."))
  }

  @Test
  fun `Lluvia de meteoros dice el vocabulario reconocido y el consejo de las letras cambiadas de lugar`() {
    val m = FinalModels.forGame(meteoros, points("vocab", 40f, 50f, 60f))!!
    assertEquals("Tu vocabulario", m.boxTitle)
    assertEquals("51 %", m.headline)
    assertEquals("de las palabras raras, reconocidas", m.headlineUnit)
    assertTrue(m.dataLine!!, m.dataLine!!.startsWith("Reconoces "))
    assertEquals("Las letras cambiadas de lugar engañan a casi todos: leemos la palabra entera. Mira el centro de la palabra para no caer.", m.trick)
    assertEquals("Como tu promedio (50 % de las palabras): vas parejo. Tu mejor: 60 % de las palabras.", m.progressPhrase)
    // sin lo que engaña: el truco general
    assertEquals("Si dudas, mira el centro de la palabra: ahí suelen esconderse las letras cambiadas.", FinalModels.forGame(meteoros.copy(lexFaSeen = listOf(7, 7, 7), lexFaHits = listOf(0, 0, 0)), emptyList())!!.trick)
    val few = FinalModels.forGame(meteoros.copy(lexBandSeen = listOf(2, 2, 2, 2, 2, 2), lexBandHits = listOf(2, 2, 2, 2, 2, 2)), emptyList())!!
    assertEquals("—", few.headline)
    assertNull(few.dataLine)
  }

  @Test
  fun `Verdad o disparate dice las palabras por minuto, lo que te frena y el truco de ese tipo de frase`() {
    val m = FinalModels.forGame(disparate, points("wpm", 130f, 140f, 150f))!!
    assertEquals("Tu lectura con comprensión", m.boxTitle)
    assertEquals("142 palabras", m.headline)
    assertEquals("por minuto, leyendo y decidiendo", m.headlineUnit)
    assertTrue(m.dataLine!!, m.dataLine!!.contains("te toman 0,6 segundos más que las frases simples"))
    assertTrue(m.trick, m.trick.isNotBlank() && m.trick != "Con un «no», lee la frase sin él y después dale la vuelta.")
    assertEquals("Como tu promedio (140 palabras por minuto): vas parejo. Tu mejor: 150 palabras por minuto.", m.progressPhrase)
    // sin tipo lento: el dato es tu mejor racha y el truco es el general
    val even = FinalModels.forGame(disparate.copy(svRtType = listOf(900, 900, 900, 900, 900, 900)), emptyList())!!
    assertEquals("Más seguidos: 14", even.dataLine)
    assertEquals("Con un «no», lee la frase sin él y después dale la vuelta.", even.trick)
    assertEquals("—", FinalModels.forGame(disparate.copy(svWpm = null), emptyList())!!.headline)
  }

  @Test
  fun `La estrella intrusa dice las rondas bien y las trampas que te enganaron`() {
    val m = FinalModels.forGame(intrusa, points("atlas", 70f, 75f, 80f))!!
    assertEquals("Tu red de significados", m.boxTitle)
    assertEquals("11 de 14 rondas", m.headline)
    assertEquals("bien resueltas", m.headlineUnit)
    assertEquals("Las trampas te engañaron 2 de 4", m.dataLine)
    assertEquals("Antes de tocar, pregúntate qué TIPO de cosa es cada una.", m.trick)
    assertEquals("Como tu promedio (75 % de aciertos): vas parejo. Tu mejor: 80 % de aciertos.", m.progressPhrase)
    // las resististe todas: el dato es el bonus
    assertEquals("¿Qué las une? Nombraste 3 de 5", FinalModels.forGame(intrusa.copy(intrHitsType = listOf(3, 3, 2, 2, 2, 2)), emptyList())!!.dataLine)
    assertEquals("Fuente: Mirman, Landrigan y Britt, 2017", m.source)
  }

  @Test
  fun `todos los textos de los finales del grupo 4 cumplen las reglas de vocabulario`() {
    for (r in listOf(punta, meteoros, disparate, intrusa)) {
      val m = FinalModels.forGame(r, points("x", 60f, 70f))!!
      val all = listOfNotNull(m.boxTitle, m.headline, m.headlineUnit, m.dataLine, m.progressPhrase, m.trick, m.why, m.source, m.note, m.spoken).joinToString(" ").lowercase()
      for (bad in listOf("entren", "cerebro", "cognitiv", "percentil", "otras personas")) assertTrue("${r.gameId}: «$bad»", !all.contains(bad))
    }
  }

  @Test
  fun `tu avance usa la misma unidad que el juego y se compara solo con partidas anteriores comparables`() {
    // Constelaciones: hoy 78 % (7 de 9); antes 60, 70 y 80 %
    val m = FinalModels.forGame(constelaciones, points("place", 60f, 70f, 80f))!!
    assertEquals("ya no hay tres cuadros: los dos números van DENTRO de la frase", emptyList<Any>(), m.chips)
    assertEquals("Mejor que tu promedio (70 % de las veces). Tu mejor: 80 % de las veces.", m.progressPhrase)
    // Rastro: hoy 5 luces; antes 4, 5, 4, 5, 4 (promedio 4,4 → «4 luces» de promedio, redondeado; tu mejor 5)
    val r = FinalModels.forGame(rastro, points("trail", 4f, 5f, 4f, 5f, 4f))!!
    assertEquals("Mejor que tu promedio (4 luces).", r.progressPhrase)
    // la de hoy ya guardada no se cuenta dos veces
    val again = FinalModels.forGame(constelaciones, points("place", 60f, 70f, 80f) + MeasurePoint(1_000L, "place", 100f * 7 / 9, 0.5f, false))!!
    assertEquals(m.progressPhrase, again.progressPhrase)
    // otro reloj: no se compara
    val otherClock = FinalModels.forGame(constelaciones, listOf(MeasurePoint(100L, "place", 60f, 0.5f, true)))!!
    assertEquals(FinalesConSentido.PHRASE_FIRST, otherClock.progressPhrase)
  }

  @Test
  fun `la primera partida no tiene con que compararse`() {
    val m = FinalModels.forGame(bodega, emptyList())!!
    assertEquals("Primera partida: la próxima te comparas contigo.", m.progressPhrase)
    assertEquals(emptyList<Any>(), m.chips)
  }

  @Test
  fun `todos los textos del final cumplen las reglas de vocabulario`() {
    for (r in listOf(constelaciones, rastro, bodega, correo)) {
      val m = FinalModels.forGame(r, points(if (r.gameId == "parejas") "place" else "x", 60f, 70f))!!
      val all = listOfNotNull(m.boxTitle, m.headline, m.headlineUnit, m.dataLine, m.progressPhrase, m.trick, m.why, m.source, m.note, m.spoken).joinToString(" ").lowercase()
      for (bad in listOf("entren", "cerebro", "cognitiv", "percentil", "otras personas")) assertTrue("${r.gameId}: «$bad»", !all.contains(bad))
    }
  }

  // ------------------------------------------------------------------ el récord, UNA vez por pantalla y nunca en la primera partida (Tarea 127)

  /** Hay sello de récord y «Tu avance» no repite «¡Tu mejor partida hasta ahora!»: usa la comparación normal con el promedio. */
  private fun assertSealed(m: com.example.ui.components.MeaningfulResultModel) {
    assertNotNull("${m.boxTitle}: debería haber sello de récord", m.recordSeal)
    assertTrue("${m.boxTitle}: el sello y «¡Tu mejor hasta ahora!» no se repiten: ${m.progressPhrase}", !m.progressPhrase.startsWith("¡Tu mejor hasta ahora!"))
  }

  @Test
  fun `un record se dice UNA vez`() {
    // (resultado con el récord que Unity avisó, la medida de sus partidas anteriores, el sello con la cifra)
    val cases = listOf(
      Triple(constelaciones.copy(conNewRecord = true), "place", "¡Nuevo récord de seguidos! 6"),
      Triple(bodega.copy(bodNewRecord = true), "bodega", "¡Nuevo récord! 6 objetos"),
      Triple(correo.copy(mailNewRecord = true), "estacion", null),
      Triple(satelites.copy(satNewRecord = true), "tracking", "¡Récord nuevo: 22 luces!"),
      Triple(rescate.copy(rescNewRecord = true), "glance", "¡Récord nuevo: 31 cápsulas!"),
      Triple(acoplamiento.copy(dockNewRecord = true), "rotation", "¡Récord nuevo: 31 módulos!")
    )
    for ((res, key, seal) in cases) {
      // UN sello (con la cifra), y ni la tarjeta ni «Tu avance» lo repiten
      val m = FinalModels.forGame(res, points(key, 70f, 70f))!!
      assertNotNull("${res.gameId}: hay sello", m.recordSeal)
      if (seal != null) assertEquals(seal, m.recordSeal)
      assertFalse("${res.gameId}: la tarjeta no repite el récord", m.dataLine.orEmpty().contains("écord", ignoreCase = true))
      assertTrue("${res.gameId}: la frase de avance no repite la mejor partida: ${m.progressPhrase}", !m.progressPhrase.startsWith("¡Tu mejor hasta ahora!"))
      // sin récord (la primera partida de un juego llega así: `FirstRecord.gate` apaga lo que Unity dice cuando no había marca previa): ni sello ni récord en la tarjeta
      val plain = FinalModels.forGame(res.copy(rescNewRecord = false, dockNewRecord = false, mailNewRecord = false, satNewRecord = false, bodNewRecord = false, conNewRecord = false), emptyList())!!
      assertNull("${res.gameId}: sin récord no hay sello", plain.recordSeal)
      assertEquals("${res.gameId}: sin partidas anteriores", com.example.data.FinalesConSentido.PHRASE_FIRST, plain.progressPhrase)
    }
  }

  @Test
  fun `sin sello el avance conserva su mejor partida y no hay sello sin record`() {
    // mejor que tu mejor anterior, sin récord guardado: hay sello («¡Récord nuevo!», sin cifra) y el avance usa el promedio
    val better = FinalModels.forGame(acoplamiento.copy(rotationSpeedDps = 320, dockNewRecord = false), points("rotation", 180f, 200f, 300f))!!
    assertEquals("¡Récord nuevo!", better.recordSeal)
    // una partida más floja: sin sello
    val worse = FinalModels.forGame(acoplamiento.copy(rotationSpeedDps = 150, dockNewRecord = false), points("rotation", 180f, 200f, 300f))!!
    assertNull(worse.recordSeal)
  }
  // ------------------------------------------------------------------ las «cifra + unidad» del final «B» (spec-final-B §2)

  @Test
  fun `las 16 cifras y sus unidades son las del final B (Aterrizaje no se toca)`() {
    val table = listOf(
      Triple(constelaciones, "78 %", "de las parejas vistas, de memoria"),
      Triple(rastro, "5 luces", "seguidas, bien repetidas"),
      Triple(bodega, "9 de 12 objetos", "al primer intento"),
      Triple(correo, "8 de 11 encargos", "cumplidos a tiempo"),
      Triple(tinta, "0,4 segundos", "de demora cuando la palabra te frenó"),
      Triple(tinta.copy(interferenceMs = 20), "casi nada", "te frenó la palabra"),
      Triple(piloto, "58 %", "de las señales, a los mandos"),
      Triple(freno, "zona firme", "tu freno, primera lectura"),
      Triple(satelites, "2,6 satélites", "que seguiste a la vez"),
      Triple(rescate, "0,17 segundos", "de vistazo te bastan"),
      Triple(carga, "5 de 8 cargas", "sin pista"),
      Triple(engranajes, "7 de 10 máquinas", "arregladas"),
      Triple(acoplamiento, "212 grados", "por segundo, de giro en tu mente"),
      Triple(punta, "5 de 8 palabras", "por tu cuenta"),
      Triple(meteoros, "51 %", "de las palabras raras, reconocidas"),
      Triple(disparate, "142 palabras", "por minuto, leyendo y decidiendo"),
      Triple(intrusa, "11 de 14 rondas", "bien resueltas")
    )
    for ((r, headline, unit) in table) {
      val m = FinalModels.forGame(r, emptyList())!!
      assertEquals("${r.gameId}: la cifra", headline, m.headline)
      assertEquals("${r.gameId}: la unidad", unit, m.headlineUnit)
      assertEquals("${r.gameId}: sin cuadros", emptyList<Any>(), m.chips)
    }
    assertEquals("las 16 de FinalModels (Aterrizaje arma la suya)", 16, table.map { it.first.gameId }.toSet().size)
  }

  @Test
  fun `sin medida la cifra es raya y la unidad dice por que, sin frase de avance`() {
    val table = listOf(
      constelaciones.copy(correctAnswers = 0, totalTrials = 0) to "Sin oportunidades de memoria en esta partida.",
      correo.copy(correctAnswers = 0, totalTrials = 0) to "Sin encargos en esta partida.",
      tinta.copy(interferenceMs = null) to "Esta vez no hubo aciertos suficientes para medirlo.",
      piloto.copy(pilSignalPct = null) to com.example.data.Piloto.TOO_FEW_LINE,
      freno.copy(brakeMs = null) to "Sin estimación: hacen falta 6 altos y frenar entre 1 de cada 4 y 3 de cada 4.",
      satelites.copy(trackingCapacity = null) to "Sin rondas suficientes para medir tu seguimiento.",
      rescate.copy(glanceMs = null) to "Sin rondas suficientes para medir tu vistazo.",
      acoplamiento.copy(rotationSpeedDps = null, rotationCurveMs = null) to "Se calcula con 8 aciertos en 3 ángulos y 7 de cada 10 bien.",
      meteoros.copy(lexBandSeen = listOf(2, 2, 2, 2, 2, 2), lexBandHits = listOf(2, 2, 2, 2, 2, 2)) to "Sin palabras suficientes para medir tu vocabulario.",
      disparate.copy(svWpm = null) to "Faltan unas 10 respuestas bien para medir tu lectura.",
      rastro.copy(rasBestLen = listOf(0, 0, 0, 0), rasRounds = listOf(2, 0, 0, 0), rasHits = listOf(0, 0, 0, 0)) to com.example.data.Trail.noTrailLine(listOf(2, 0, 0, 0))
    )
    for ((r, unit) in table) {
      val m = FinalModels.forGame(r, points("x", 60f, 70f))!!
      assertEquals("${r.gameId}: sin medida", "—", m.headline)
      assertEquals("${r.gameId}: la unidad dice por qué", unit, m.headlineUnit)
      assertEquals("${r.gameId}: sin frase de avance (no decir dos veces lo mismo)", "", m.progressPhrase)
      assertEquals(emptyList<Any>(), m.chips)
    }
  }

  @Test
  fun `las unidades, las frases y las sin medida cumplen el vocabulario y la guardia de culpa`() {
    val results = listOf(constelaciones, rastro, bodega, correo, tinta, piloto, freno, satelites, rescate, carga, engranajes, acoplamiento, punta, meteoros, disparate, intrusa)
    val histories = listOf(emptyList<MeasurePoint>(), points("place", 60f, 70f), points("trail", 4f, 5f), points("brake", 260f, 250f), points("tracking", 2.2f, 2.9f), points("wpm", 130f, 150f), points("vocab", 40f, 60f), points("atlas", 70f, 80f))
    for (r in results) for (h in histories) {
      val m = FinalModels.forGame(r, h)!!
      for (t in listOfNotNull(m.headline, m.headlineUnit, m.progressPhrase) + m.notices) {      // el truco no cambió (ya pasa sus guardias)
        assertEquals("${r.gameId}: «$t»", null, com.example.NoLossGuard.hit(t, com.example.NoLossGuard.literals))
        assertEquals("${r.gameId}: «$t» (sin excepciones: ya no hay ninguna unidad con la raíz «perder»)", null, com.example.NoLossGuard.hit(t, com.example.NoLossGuard.reminderRoots))
        for (bad in listOf("entren", "cerebro", "cognitiv", "percentil", "otras personas", "racha")) assertTrue("${r.gameId}: «$t» dice «$bad»", !t.lowercase().contains(bad))
      }
    }
  }

  @Test
  fun `el lector de pantalla dice el juego, la cifra con su unidad, la frase, los avisos y el truco`() {
    val m = FinalModels.forGame(rastro.copy(rasNewModes = 8), points("trail", 4f, 5f, 4f, 5f, 4f))!!
    assertEquals(
      "Rastro de luz, Memoria. 5 luces seguidas, bien repetidas. Mejor que tu promedio (4 luces). Modo nuevo: En marcha. Truco para la próxima: ${m.trick.trimEnd('.')}.",
      m.spoken
    )
  }
}
