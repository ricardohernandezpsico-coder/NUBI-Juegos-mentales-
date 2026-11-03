package com.example.data

import com.example.model.AppLanguage
import com.example.model.GamePlayResult
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.util.Calendar

/**
 * «Tus últimas partidas» (Tarea 221; `auditoria-fable/pestana-juegos-historial.md` §5 y §6): la lógica pura. Qué filas hay, en qué orden, qué dice cada una («cuándo · modo · marca») y lo que dice el lector; y la aritmética del alto de la tarjeta (§10.1).
 */
class HistoryRowsTest {
  private val day = 86_400_000L
  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 26, 16, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis

  private fun play(game: String, agoMs: Long, mode: PlayMode = PlayMode.A_TU_MEDIDA, route: Boolean = false, known: Boolean = true, id: String = "$game-$agoMs") =
    GamePlayResult(id = id, gameId = game, score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 3, timestamp = now - agoMs, playMode = mode, route = route, modeKnown = known)

  private fun trial(game: String, agoMs: Long, hits: Int = 40, errors: Int = 2) =
    TimeTrialResult(id = "tt-$game-$agoMs", gameId = game, day = "2026-10-26", trackVersion = TimeTrialDay.trackVersion(game), hits = hits, errors = errors, level = 4, seconds = 120, timestamp = now - agoMs)

  private fun rows(history: List<GamePlayResult> = emptyList(), trials: List<TimeTrialResult> = emptyList(), measures: List<MeasurePoint> = emptyList(), max: Int = HISTORY_MAX) =
    historyRows(history, trials, measures, now, AppLanguage.SPANISH, max)

  // ---- Qué filas y en qué orden

  @Test
  fun sin_partidas_no_hay_filas_y_Hoy_no_dibuja_la_tarjeta() {
    assertEquals(emptyList<HistoryRow>(), rows())
  }

  @Test
  fun mezcla_las_partidas_de_siempre_y_las_del_Contrarreloj_por_su_fecha_las_mas_recientes_primero() {
    val r = rows(
      history = listOf(play("calculo", 5 * 3_600_000L), play("secuencia", 2 * day)),
      trials = listOf(trial("stroop", 3_600_000L), trial("radar", day + 3_600_000L))
    )
    assertEquals(listOf("stroop", "calculo", "radar", "secuencia"), r.map { it.gameId })
    assertEquals(listOf(true, false, true, false), r.map { it.timeTrial })
    assertEquals("de la más reciente a la más vieja", r.map { it.timestamp }.sortedDescending(), r.map { it.timestamp })
  }

  @Test
  fun lista_diez_como_maximo_y_se_queda_con_las_mas_recientes() {
    val history = (1..14).map { play("calculo", it * 3_600_000L, id = "p$it") }
    val r = rows(history = history)
    assertEquals(HISTORY_MAX, r.size)
    assertEquals(10, HISTORY_MAX)
    assertEquals("las diez más recientes", (1..10).map { now - it * 3_600_000L }, r.map { it.timestamp })
    assertEquals("max = 3 la recorta a tres", 3, rows(history = history, max = 3).size)
    assertEquals("con 11 partidas guardadas, 10 filas", 10, rows(history = history.take(8), trials = listOf(trial("stroop", 1), trial("radar", 2), trial("acoplamiento", 3))).size)
  }

  @Test
  fun no_lista_los_juegos_retirados_ni_los_que_ya_no_estan_en_el_catalogo() {
    val r = rows(history = listOf(play("bitacora", 1_000), play("cosecha", 2_000), play("noexiste", 3_000), play("calculo", 4_000)), trials = listOf(trial("noexiste", 500)))
    assertEquals(listOf("calculo"), r.map { it.gameId })
  }

  // ---- Lo que dice cada fila

  @Test
  fun el_cuando_es_hoy_ayer_hace_N_dias_semanas_o_meses() {
    val ago = { days: Int -> rows(history = listOf(play("calculo", days * day + 1_000))).single().whenText }
    assertEquals("hoy", rows(history = listOf(play("calculo", 1_000))).single().whenText)
    assertEquals("ayer", ago(1))
    assertEquals("hace 2 días", ago(2))
    assertEquals("hace 9 días", ago(9))
    assertEquals("hace 2 semanas", ago(15))
    assertEquals("hace 3 meses", ago(95))
  }

  @Test
  fun el_modo_es_En_tu_ruta_o_el_de_la_ficha_y_el_Contrarreloj_se_llama_Contrarreloj() {
    val r = rows(
      history = listOf(
        play("calculo", 1_000, PlayMode.A_TU_MEDIDA, route = true),
        play("freno", 2_000, PlayMode.SUAVE), play("radar", 3_000, PlayMode.A_TU_MEDIDA), play("secuencia", 4_000, PlayMode.DESAFIO), play("parejas", 5_000, PlayMode.EXPERTO)
      ),
      trials = listOf(trial("stroop", 500))
    ).associate { it.gameId to it.mode }
    assertEquals("En tu ruta", r["calculo"])
    assertEquals("Suave", r["freno"]); assertEquals("A tu medida", r["radar"]); assertEquals("Desafío", r["secuencia"]); assertEquals("Al límite", r["parejas"])
    assertEquals("Contrarreloj", r["stroop"])
    for (word in listOf("Reto", "Experto", "partidas libres", "camino", "sesión", "diaria")) assertFalse("«$word» no es un nombre de modo", r.values.any { it?.contains(word, ignoreCase = true) == true })
  }

  @Test
  fun una_partida_de_antes_sin_modo_no_inventa_uno_y_la_linea_pasa_del_cuando_a_la_marca() {
    val row = rows(history = listOf(play("calculo", 9 * day, known = false)), measures = listOf(MeasurePoint(now - 9 * day, "carga", 70f))).single()
    assertNull(row.mode)
    assertEquals("hace 9 días · 70 % de las cargas, sin pista", row.detail)
    assertEquals("Carga exacta, hace 9 días, 70 % de las cargas, sin pista.", row.spoken)
  }

  @Test
  fun la_marca_es_la_frase_de_la_medida_de_ese_juego_cruzada_por_juego_y_fecha_exacta() {
    val ts = now - 1_000
    val measures = listOf(
      MeasurePoint(ts, "carga", 70f),
      MeasurePoint(ts - 5, "carga", 99f),                         // otra partida del mismo juego, otra fecha: no es de esta
      MeasurePoint(ts, "trail", 7f)                              // otro juego a la misma fecha: tampoco
    )
    val row = rows(history = listOf(play("calculo", 1_000, route = true)), measures = measures).single()
    assertEquals("70 % de las cargas, sin pista", row.mark)
    assertEquals("hoy · En tu ruta · 70 % de las cargas, sin pista", row.detail)
    assertEquals("Carga exacta, hoy, en tu ruta, 70 % de las cargas, sin pista.", row.spoken)
    assertNull("sin medida de esa fecha, sin marca", rows(history = listOf(play("calculo", 3_000)), measures = measures).single().mark)
  }

  @Test
  fun Freno_se_dice_en_zonas_nunca_en_milisegundos_y_Tinta_o_Palabra_en_palabras() {
    val zone = { ms: Float -> rows(history = listOf(play("freno", 1_000)), measures = listOf(MeasurePoint(now - 1_000, "brake", ms))).single().mark }
    assertEquals("zona ágil", zone(200f)); assertEquals("zona firme", zone(260f)); assertEquals("zona pausado", zone(400f))
    assertNull("una estimación que no es válida no es una marca", zone(2_000f))
    val stroop = { ms: Float -> rows(history = listOf(play("stroop", 1_000)), measures = listOf(MeasurePoint(now - 1_000, "stroop", ms))).single().mark }
    assertEquals("la palabra casi no te frena", stroop(10f))
    assertEquals("la palabra te frena 0,4 segundos", stroop(400f))
    assertFalse("ni «ms» ni «milisegundos»", listOf(zone(260f), stroop(400f)).joinToString(" ").contains("ms"))
  }

  @Test
  fun la_marca_del_Contrarreloj_es_la_marca_en_aciertos_con_su_unidad() {
    val one = rows(trials = listOf(trial("stroop", 1_000, hits = 3, errors = 2))).single()
    assertEquals("1 acierto", one.mark)
    assertEquals("hoy · Contrarreloj · 1 acierto", one.detail)
    val many = rows(trials = listOf(trial("stroop", 1_000, hits = 40, errors = 2))).single()
    assertEquals("38 aciertos", many.mark)
    assertEquals("Tinta o Palabra, hoy, Contrarreloj, 38 aciertos.", many.spoken)
  }

  @Test
  fun el_lector_dice_la_fila_entera_con_el_modo_en_minuscula_y_sin_la_palabra_boton() {
    val r = rows(history = listOf(play("secuencia", day + 1_000, PlayMode.A_TU_MEDIDA), play("freno", 2 * day + 1_000, PlayMode.DESAFIO)))
    assertEquals("Rastro de luz, ayer, a tu medida.", r[0].spoken)
    assertEquals("Freno de Emergencia, hace 2 días, desafío.", r[1].spoken)
    for (row in r) assertFalse(row.spoken.contains("botón", ignoreCase = true))
  }

  @Test
  fun los_textos_cumplen_las_reglas_de_siempre() {
    val ts = now - 1_000
    val all = rows(
      history = listOf(play("calculo", 1_000, route = true), play("stroop", 2_000), play("freno", 3_000, PlayMode.DESAFIO), play("secuencia", 4_000)),
      trials = listOf(trial("radar", 500)),
      measures = listOf(MeasurePoint(ts, "carga", 70f), MeasurePoint(now - 2_000, "stroop", 400f), MeasurePoint(now - 3_000, "brake", 260f), MeasurePoint(now - 4_000, "trail", 5f))
    )
    for (row in all) {
      val text = row.detail + " " + row.spoken
      for (word in listOf("cerebro", "cognitiv", "entrenamiento", "capacidad", "edad mental", "ranking", "velocidad", "percentil", "puntaje", "nivel")) assertFalse("«$word» en «$text»", text.lowercase().contains(word))
      assertFalse("sin flechas ni «+N»: «$text»", Regex("[→↑↓›]|\\+\\d").containsMatchIn(text))
      assertFalse("«%» siempre seguido de «de»: «$text»", Regex("%(?! de )").containsMatchIn(text))
    }
  }

  @Test
  fun el_numero_queda_pegado_a_su_unidad_con_un_espacio_duro_solo_al_dibujar_y_el_lector_lee_espacios_normales() {
    val nbsp = '\u00A0'
    val row = rows(history = listOf(play("calculo", 2 * day + 1_000, route = true)), measures = listOf(MeasurePoint(now - 2 * day - 1_000, "carga", 70f)), trials = listOf(trial("stroop", 3 * day, hits = 40, errors = 2))).first { !it.timeTrial }
    assertEquals("hace 2${nbsp}días${nbsp}· En tu ruta${nbsp}· 70${nbsp}% de las cargas, sin pista", row.shownDetail)
    assertEquals("sin cambiar lo que dice el lector ni el texto de siempre", "hace 2 días · En tu ruta · 70 % de las cargas, sin pista", row.detail)
    assertFalse(row.spoken.contains(nbsp))
    val trialRow = rows(trials = listOf(trial("stroop", 1_000, hits = 40, errors = 2))).single()
    assertEquals("hoy${nbsp}· Contrarreloj${nbsp}· 38${nbsp}aciertos", trialRow.shownDetail)
    assertEquals("un número solo, sin unidad ni letra detrás, no se toca", "hoy · 3 · x", "hoy · 3 · x".glueNumbers())
  }

  // ---- El alto de la tarjeta (§10.1)

  @Test
  fun una_fila_mide_52_dp_como_minimo_y_crece_con_su_texto_mas_8_dp_de_aire() {
    assertEquals("la marca en una línea (21 + 20 dp de texto)", 52f, HistoryCard.rowHeight(41f), 0f)
    assertEquals("la marca en dos líneas (21 + 40 dp de texto)", 69f, HistoryCard.rowHeight(61f), 0f)
    assertEquals("con letra 1,3 (27,3 + 2 × 26 dp)", 87.3f, HistoryCard.rowHeight(79.3f), 0.01f)
    assertEquals("nunca menos que el emblema de 36 dp y su aire", 52f, HistoryCard.rowHeight(10f), 0f)
  }

  @Test
  fun con_cuatro_o_mas_filas_la_ventana_es_tres_filas_enteras_con_sus_lineas_y_la_mitad_de_la_cuarta() {
    // las filas de la lámina de Fable con letra 1,0: las dos primeras marcas en dos líneas
    val h = listOf(69f, 69f, 52f, 52f, 52f, 52f, 52f, 52f, 52f, 52f)
    assertEquals("la tarjeta mide 259 dp a 393 con letra 1,0 (12 + 22 + 6 de arriba, 69 + 1 + 69 + 1 + 52 + 1 y la mitad de 52)", 259f, HistoryCard.height(h, 22f), 0f)
    assertEquals("225 con las cuatro en una línea", 225f, HistoryCard.height(List(10) { 52f }, 22f), 0f)
    // lo visible de la cuarta es la mitad
    assertEquals(26f, HistoryCard.windowHeight(h) - (69f + 1f + 69f + 1f + 52f + 1f), 0f)
    // 4 filas: se desliza; 3: no
    assertTrue(HistoryCard.scrolls(4)); assertFalse(HistoryCard.scrolls(3)); assertFalse(HistoryCard.scrolls(1))
  }

  @Test
  fun con_una_a_tres_filas_la_tarjeta_tiene_su_alto_natural_sin_recorte_ni_deslizado() {
    assertEquals(0f, HistoryCard.height(emptyList(), 22f), 0f)
    assertEquals("una fila: el aire de arriba, el título, el aire con su línea, la fila y el aire del final", 12f + 22f + 6f + 52f + 10f, HistoryCard.height(listOf(52f), 22f), 0f)
    assertEquals("tres filas: sus alturas, las dos líneas de entre ellas y el aire del final", 12f + 22f + 6f + (69f + 52f + 52f) + 2f + 10f, HistoryCard.height(listOf(69f, 52f, 52f), 22f), 0f)
    assertEquals("sin deslizado por dentro", 0f, HistoryCard.innerTravel(listOf(69f, 52f, 52f)), 0f)
  }

  @Test
  fun el_recorrido_por_dentro_es_el_contenido_menos_la_ventana_y_deja_la_decima_fila_entera_con_su_aire() {
    val h = List(10) { 52f }
    val content = 52f * 10 + 9 + HistoryCard.END_AIR_DP
    assertEquals(content - HistoryCard.windowHeight(h), HistoryCard.innerTravel(h), 0.001f)
    assertTrue("con 10 filas la lista recorre más de lo que muestra a la vez", HistoryCard.innerTravel(h) > HistoryCard.windowHeight(h))
    assertEquals("con 4 filas recorre poco: la mitad de la cuarta y el aire", 52f / 2f + HistoryCard.END_AIR_DP, HistoryCard.innerTravel(List(4) { 52f }), 0.001f)
  }

  @Test
  fun el_alto_crece_con_la_letra_pero_siempre_son_tres_filas_y_media() {
    for (scale in listOf(1f, 1.3f, 2f)) {
      val textDp = 21f * scale + 40f * scale
      val h = List(10) { HistoryCard.rowHeight(textDp) }
      val window = HistoryCard.windowHeight(h)
      assertEquals("con letra $scale: tres filas, tres líneas y media fila", 3f * h[0] + 3f + h[3] / 2f, window, 0.01f)
    }
  }
}
