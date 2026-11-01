package com.example.data

import com.example.Features
import com.example.NoLossGuard
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * «Preguntas frecuentes» y «Quién hace Nubi» (Tarea 209; `auditoria-fable/claridad-plus-y-ayuda.md` §1.3 y §1.4): los textos exactos con los ajustes de Opus, el orden, que las dos del cobro queden apagadas con [Features.BILLING], que ninguna diga una palabra que no se dice
 * y que la IA se nombre solo en la pregunta «¿Quién hace Nubi?», nunca en la sección de Tú.
 */
class PreguntasTest {
  private val all = Preguntas.billing + Preguntas.general

  @Test
  fun las_seis_preguntas_de_hoy_van_en_el_orden_de_Fable_y_las_dos_del_cobro_aparte() {
    assertEquals(
      listOf(
        "¿Qué pasa si cambio de teléfono?", "¿Cómo funciona la racha?", "¿Con qué me compara el Contrarreloj?",
        "¿Qué ve alguien de 13 a 17 años?", "¿Quién hace Nubi?", "¿Nubi necesita internet o una cuenta?"
      ),
      Preguntas.general.map { it.question }
    )
    assertEquals(listOf("¿Nubi Plus se renueva solo?", "¿Cómo cancelo Nubi Plus?"), Preguntas.billing.map { it.question })
    assertEquals("las claves no se repiten", all.size, all.map { it.key }.toSet().size)
  }

  @Test
  fun la_bandera_del_cobro_esta_apagada_y_las_dos_del_cobro_no_se_ven_hasta_encenderla() {
    assertFalse("el cobro llega con la Etapa 4", Features.BILLING)
    assertEquals(Preguntas.general, Preguntas.visible())
    assertEquals(Preguntas.general, Preguntas.visible(billingOn = false))
    assertTrue("ni la de renovar ni la de cancelar", Preguntas.visible().none { it.key == "renueva" || it.key == "cancelar" })
    assertEquals("encendida, las del cobro van ARRIBA de la lista", Preguntas.billing + Preguntas.general, Preguntas.visible(billingOn = true))
    assertEquals("y solo la de cancelar trae el botón de Suscripciones", listOf("cancelar"), Preguntas.billing.filter { it.subscriptionsButton }.map { it.key })
    assertTrue("hoy ninguna pregunta visible lo trae", Preguntas.visible().none { it.subscriptionsButton })
  }

  @Test
  fun la_respuesta_del_cambio_de_telefono_dice_que_la_copia_la_hace_Android_y_no_Nubi() {
    val a = Preguntas.general.first { it.key == "telefono" }.answer
    assertTrue("el ajuste de Opus: la última frase", a.endsWith("La copia la hace Android, no Nubi: Nubi no envía tus datos a nadie."))
    assertTrue("y viene después de la copia de Google", a.indexOf("restaura la copia") < a.indexOf("no envía tus datos"))
  }

  @Test
  fun solo_la_pregunta_de_con_que_compara_trae_la_fila_de_donde_sale_la_referencia() {
    assertEquals(listOf("referencia"), all.filter { it.sourceLink }.map { it.key })
    val a = Preguntas.general.first { it.key == "referencia" }.answer
    assertEquals(
      "Con una referencia estimada para tu edad, no con otras personas: Nubi no junta datos de nadie. La referencia sale de estudios por edad y de un programa que jugó esta misma pista en un computador. Gratis ves tu división de la semana; con Nubi Plus, además, dónde queda tu marca frente a tu edad y a otras edades.",
      a
    )
  }

  @Test
  fun la_IA_se_nombra_solo_en_la_pregunta_de_quien_hace_Nubi_y_la_seccion_de_Tu_es_solo_el_psicologo() {
    assertEquals("Quién hace Nubi", Preguntas.WHO_TITLE)
    assertEquals("Un psicólogo chileno decide cada juego y cada texto.", Preguntas.WHO_LINE)
    val low = Preguntas.WHO_LINE.lowercase()
    for (word in listOf("inteligencia", " ia ", "herramienta", "programó", "ricardo", "hernández", "hernandez")) assertFalse("la sección de Tú no dice «$word»", word in low)
    val who = Preguntas.general.first { it.key == "quien" }.answer
    assertEquals("Un psicólogo chileno, que decide cada juego y cada texto. Se programó con ayuda de herramientas de inteligencia artificial, que también revisaron errores. Nubi no promete mejorar nada: es un juego.", who)
    for (p in all.filter { it.key != "quien" }) assertFalse("«${p.key}» no nombra la IA", "inteligencia artificial" in p.answer.lowercase())
    // lo que no va en ningún lado
    for (p in all) for (bad in listOf("agentes de ia", "simul", "simularon", "diferenciador")) assertFalse("«${p.key}» dice «$bad»", bad in (p.question + p.answer).lowercase())
    assertFalse("ningún texto lleva el nombre de la persona", all.any { "ricardo" in (it.question + it.answer).lowercase() || "hernández" in (it.question + it.answer).lowercase() })
  }

  @Test
  fun ninguna_pregunta_ni_respuesta_dice_una_palabra_que_no_se_dice_ni_un_numero_de_robots() {
    for (p in all) {
      val low = (p.question + " " + p.answer).lowercase()
      for (bad in listOf("cerebro", "cognitiv", "entren", "entrén", "capacidad", "edad mental", "ranking", "velocidad", "percentil", "robot", "bienestar", "estimulaci", "mejora tu", "fortalece", "previene", "neurona"))
        assertFalse("«${p.key}» dice «$bad»", bad in low)
      NoLossGuard.hit(p.answer, NoLossGuard.literals)?.let { error("«${p.key}» dice «$it»") }
      assertTrue("«${p.question}» es una pregunta", p.question.startsWith("¿") && p.question.endsWith("?"))
      assertTrue("«${p.key}»: la respuesta es una frase completa", p.answer.endsWith("."))
      assertFalse("sin emojis", Regex("[\\u2600-\\u27BF\\uD83C-\\uDBFF\\uDC00-\\uDFFF]").containsMatchIn(p.question + p.answer))
    }
    assertEquals("«personas» solo aparece en lo que el diseño escribe: una sola vez (en «no con otras personas»)", 1, all.sumOf { Regex("personas").findAll(it.answer).count() })
  }

  @Test
  fun lo_que_se_dice_del_cobro_cumple_la_politica_de_Play() {
    val renews = Preguntas.billing.first { it.key == "renueva" }.answer
    assertTrue("dice qué se renueva solo y qué no", "mensual y el anual se renuevan solos" in renews && "pago único" in renews && "no se renueva" in renews)
    assertFalse("nunca «de por vida» ni «para siempre»", all.any { "de por vida" in it.answer.lowercase() || "para siempre" in it.answer.lowercase() })
    val cancel = Preguntas.billing.first { it.key == "cancelar" }.answer
    assertTrue("dice cómo cancelar y que desinstalar no cancela", "Suscripciones" in cancel && "Desinstalar la app no cancela la suscripción." in cancel)
    assertEquals("Abrir Suscripciones en Google Play", Preguntas.SUBSCRIPTIONS_BUTTON)
  }

  @Test
  fun el_enlace_a_Suscripciones_lleva_el_paquete_real_y_el_sku_solo_cuando_exista() {
    assertEquals("el paquete es el applicationId de la app", com.example.BuildConfig.APPLICATION_ID, Preguntas.PACKAGE)
    assertEquals("https://play.google.com/store/account/subscriptions", Preguntas.SUBSCRIPTIONS_URL)
    assertEquals("https://play.google.com/store/account/subscriptions?package=com.nubibraingames.app", Preguntas.subscriptionsUrl())
    assertEquals("https://play.google.com/store/account/subscriptions?sku=nubi_plus_anual&package=com.nubibraingames.app", Preguntas.subscriptionsUrl("nubi_plus_anual"))
  }

  @Test
  fun el_texto_corrido_de_la_ficha_de_Play_cabe_en_2000_caracteres_con_las_ocho_y_pone_las_del_cobro_primero() {
    val with = Preguntas.playListing(billingOn = true)
    assertTrue("%d caracteres con las ocho".format(with.length), with.length < 2000)
    val order = (Preguntas.billing + Preguntas.general).map { with.indexOf(it.question) }
    assertEquals("las ocho, en orden, las del cobro primero", order.sorted(), order)
    assertTrue(order.all { it >= 0 })
    val without = Preguntas.playListing(billingOn = false)
    assertFalse("sin el cobro, sin sus preguntas", "se renueva solo" in without || "cancelo" in without)
    assertNotEquals(with, without)
    assertFalse("sin botones", "Abrir Suscripciones" in with)
  }

  @Test
  fun la_ficha_de_Play_sin_valor_explicito_sigue_la_bandera_del_cobro_y_no_trae_sus_preguntas_mientras_este_apagado() {
    assertEquals("por defecto, lo mismo que con la bandera", Preguntas.playListing(billingOn = Features.BILLING), Preguntas.playListing())
    if (!Features.BILLING) assertFalse("con el cobro apagado, la ficha no habla de renovar ni de cancelar", "se renueva solo" in Preguntas.playListing() || "cancelo" in Preguntas.playListing())
  }
}
